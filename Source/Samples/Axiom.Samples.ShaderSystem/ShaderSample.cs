using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Axiom.Core;
using Axiom.Math;
using Axiom.Core.Collections;
using Axiom.Components.RTShaderSystem;
using Axiom.Graphics;

namespace Axiom.Samples.ShaderSystem
{
	public class ShaderSample : SdkSample
	{
		private enum ShaderSystemLightingModel
		{
			PerVertexLighting,
			PerPixelLighting,
			NormalMapLightingTangentSpace,
			NormalMapLightingObjectSpace
		}

		#region Fields

		private EntityList targetEntities;
		private ShaderSystemLightingModel curLightingModel;
		private SelectMenu lightingModelMenu;
		private SelectMenu fogModeMenu;
		private SelectMenu languageMenu;
		private SelectMenu shadowMenu;
		private bool perPixelFogEnable;
		private bool specularEnable;
		private SubRenderStateFactory reflectionMapFactory;
		private SubRenderState instancedViewportsSubRenderState;
		private bool instancedViewportsEnable;
		private InfiniteFrustum infiniteFrustum;
		private BillboardSet bbsFlare;
		private bool addedLotsOfModels;
		private List<Entity> lotsOfModelsEntitites;
		private List<SceneNode> lotsOfModelsNodes;
		private int numberOfModelsAdded;
		private SubRenderStateFactory instancedViewportsFactory;

		private SubRenderState reflectionMapSubRS;
		private LayeredBlending layerBlendSubRS;
		private Label layerBlendLabel;
		private Slider reflectionPowerSlider;
		private bool reflectionMapEnable;
		private Slider modifierValueSlider;
		private Entity layeredBlendingEntity;
		private SceneNode pointLightNode;
		private SceneNode directionalLightNode;
		private RaySceneQuery rayQuery;
		private MovableObject targetObj;
		private Label targetObjMatName;
		private Label targetObjVS;
		private Label targetObjFS;
		private CheckBox dirLightCheckBox;
		private CheckBox pointLightCheckBox;
		private CheckBox spotLightCheckBox;
		private string exportMaterialPath;
		private CheckBox instancedViewportsCheckBox;
		private CheckBox addLotsOfModels;

		private const string DirectionalLightName = "DirectionalLight";
		private const string PointLightName = "PointLight";
		private const string InstancedViewportsName = "InstancedViewports";
		private const string AddLotsOfModelsName = "AddLotsOfModels";
		private const string SpotLightName = "SpotLight";
		private const string PerPixelFogBox = "PerPixelFog";
		private const string AtlasAutoBorderMode = "AutoBorderAtlasing";
		private const string MainEntityMesh = "MainEntity";
		private const string SpecularBox = "SpecularBox";
		private const string ReflectionMapBox = "ReflectionMapBox";
		private const string ReflectionMapPowerSlider = "ReflectionPowerSlider";
		private const string MainEntityName = "MainEntity";
		private const string ExportButtonName = "ExportMaterial";
		private const string FlushButtonName = "FlushShaderCache";
		private const string LayerblendButtonName = "ChangeLayerBlendType";
		private const string ModifierValueSlider = "ModifierValueSlider";
		private const string SampleMaterialGroup = "RTShaderSystemMaterialsGroup";

		private readonly string[] meshArray = new string[2]
		                                      {
		                                      	MainEntityMesh, "knot.mesh"
		                                      };

		#endregion

		public ShaderSample()
		{
			this.layeredBlendingEntity = null;
			Metadata[ "Title" ] = "Shader System";
			Metadata[ "Description" ] = "Demonstrate the capabilities of the RT Shader System component." +
			                            "1. Fixed Function Pipeline emulation." +
			                            "2. On the fly shader generation based on existing material." +
			                            "3. On the fly shader synchronization with scene state (Lights, Fog)." +
			                            "4. Built in lighting models: Per vertex, Per pixel, Normal map tangent and object space." +
			                            "5. Pluggable custom shaders extensions." +
			                            "6. Built in material script parsing that includes extended attributes." +
			                            "7. Built in material script serialization.";
			Metadata[ "Thumbnail" ] = "thumb_shadersystem.png";
			Metadata[ "Category" ] = "Lighting";
			Metadata[ "Help" ] = "F2 Toggle Shader System globally. " +
			                     "F3 Toggles Global Lighting Model. " +
			                     "Modify target model attributes and scene settings and observe the generated shaders count. " +
			                     "Press the export button in order to export current target model material. " +
			                     "The model above the target will import this material next time the sample reloads. " +
			                     "Right click on object to see the shaders it currently uses. ";
			this.pointLightNode = null;
			this.reflectionMapFactory = null;
			this.instancedViewportsEnable = false;
			this.instancedViewportsSubRenderState = null;
			this.instancedViewportsFactory = null;
			this.bbsFlare = null;
			this.addedLotsOfModels = false;
			this.numberOfModelsAdded = 0;
		}

		public override void Shutdown()
		{
			DestroyInstancedViewports();
			base.Shutdown();
		}

		private void ItemSelected( SelectMenu menu )
		{
			if ( menu == this.lightingModelMenu )
			{
				int curModelIndex = menu.SelectionIndex;

				if ( curModelIndex >= (int)ShaderSystemLightingModel.PerVertexLighting &&
				     curModelIndex <= (int)ShaderSystemLightingModel.NormalMapLightingObjectSpace )
				{
					CurrentLightingModel = (ShaderSystemLightingModel)curModelIndex;
				}
			}
			else if ( menu == this.fogModeMenu )
			{
				int curModeIndex = menu.SelectionIndex;

				if ( curModeIndex >= (int)FogMode.None && curModeIndex <= (int)FogMode.Linear )
				{
					SceneManager.SetFog( (FogMode)curModeIndex, new ColorEx( 1, 1, 1, 0 ), 0.0015f, 350.0f, 1500.0f );
				}
			}
			else if ( menu == this.shadowMenu )
			{
				int curShadowTypeIndex = menu.SelectionIndex;

				ApplyShadowType( curShadowTypeIndex );
			}
			else if ( menu == this.languageMenu )
			{
				ShaderGenerator.Instance.TargetLangauge = menu.SelectedItem;
			}
		}

		private void CheckBoxToggled( CheckBox box )
		{
			string cbName = box.Name;

			if ( cbName == SpecularBox )
			{
				SpecularEnable = box.IsChecked;
			}
			else if ( cbName == ReflectionMapBox )
			{
				this.reflectionMapEnable = box.IsChecked;
			}
			else if ( cbName == DirectionalLightName )
			{
				UpdateLightState( cbName, box.IsChecked );
			}
			else if ( cbName == PointLightName )
			{
				UpdateLightState( cbName, box.IsChecked );
			}
			else if ( cbName == InstancedViewportsName )
			{
				UpdateLightState( cbName, box.IsChecked );
			}
			else if ( cbName == AddLotsOfModelsName )
			{
				UpdateAddLotsOfModels( box.IsChecked );
			}
			else if ( cbName == SpotLightName )
			{
				UpdateLightState( cbName, box.IsChecked );
			}
			else if ( cbName == PerPixelFogBox )
			{
				PerPixelFogEnable = box.IsChecked;
			}
			else if ( cbName == AtlasAutoBorderMode )
			{
				SetAtlasBorderMode( box.IsChecked );
			}
		}

		public void ButtonHit( Button b )
		{
			//Case the current material of the main entity should be exported
			if ( b.Name == ExportButtonName )
			{
				string materialName = SceneManager.GetEntity( MainEntityName ).GetSubEntity( 0 ).MaterialName;

				ExportRTShaderSystemMaterail( this.exportMaterialPath + "ShaderSystemExport.material", materialName );
			}
				//Case the shader cache should be flushed
			else if ( b.Name == FlushButtonName )
			{
				ShaderGenerator.Instance.FlushShaderCache();
			}

				//Case the blend layer type modified.
			else if ( b.Name == LayerblendButtonName && this.layerBlendSubRS != null )
			{
				ChangeTextureLayerBlendMode();
			}
		}

		public void SliderMoved( object sender, Slider slider )
		{
			if ( slider.Name == ReflectionMapPowerSlider )
			{
				Real reflectionPower = slider.Value;

				if ( this.reflectionMapSubRS != null )
				{
					var reflectMapSubRS = this.reflectionMapSubRS as ReflectionMap;

					// Since RTSS export caps based on the template sub render states we have to update the template reflection sub render state.
					reflectMapSubRS.ReflectionPower = reflectionPower;

					// Grab the instances set and update them with the new reflection power value.
					// The instances are the actual sub render states that have been assembled to create the final shaders.
					// Every time that the shaders have to be re-generated (light changes, fog changes etc..) a new set of sub render states 
					// based on the template sub render states assembled for each pass.
					// From that set of instances a CPU program is generated and afterward a GPU program finally generated.

					foreach ( var it in this.reflectionMapSubRS.TemplateSubRenderStateList )
					{
						var reflectionMapInstance = it as ReflectionMap;
						reflectionMapInstance.ReflectionPower = reflectionPower;
					}
				}
			}

			if ( slider.Name == ModifierValueSlider )
			{
				if ( this.layeredBlendingEntity != null )
				{
					Real val = this.modifierValueSlider.Value;
					this.layeredBlendingEntity.GetSubEntity( 0 ).SetCustomParameter( 2, new Vector4( val, val, val, 0 ) );
				}
			}
		}

		public override void TestCapabilities( Graphics.RenderSystemCapabilities capabilities )
		{
			if ( !capabilities.HasCapability( Capabilities.VertexPrograms ) ||
			     !( capabilities.HasCapability( Capabilities.FragmentPrograms ) ) )
			{
				throw new AxiomException(
					"Your graphics card does not support vertex and fragment programs, so you cannot run this sample. Sorry!" );
			}

			//Check if D3D10 shader is supported - is so - then we are OK.
			if ( GpuProgramManager.Instance.IsSyntaxSupported( "ps_4_0" ) )
			{
				return;
			}

			//Check if GLSL type shaders are supported - is so - then we are OK.
			if ( GpuProgramManager.Instance.IsSyntaxSupported( "glsles" ) ||
			     GpuProgramManager.Instance.IsSyntaxSupported( "glsl" ) )
			{
				return;
			}

			if ( !GpuProgramManager.Instance.IsSyntaxSupported( "arbfp1" ) &&
			     !GpuProgramManager.Instance.IsSyntaxSupported( "ps_2_0" ) )
			{
				throw new AxiomException( "Your card does not support shader model 2, so you cannot run this sample. Sorry!" );
			}
		}

		public override bool FrameRenderingQueued( FrameEventArgs evt )
		{
			Light light = SceneManager.GetLight( SpotLightName );

			light.Position = Camera.DerivedPosition + Camera.DerivedUp*20.0f;
			light.Direction = Camera.DerivedDirection;

			if ( this.pointLightNode != null )
			{
				Real totalTime = 0.0;

				totalTime += evt.TimeSinceLastFrame;
				this.pointLightNode.Yaw( (float)( new Degree( new Real( evt.TimeSinceLastFrame*15 ) ) ) );
				this.pointLightNode.Position = new Vector3( 0, Axiom.Math.Utility.Sin( totalTime )*30.0, 0 );
			}

			UpdateTargetObjInfo();
			return base.FrameRenderingQueued( evt );
		}

		public void UpdateAddLotsOfModels( bool addThem )
		{
			if ( this.addedLotsOfModels != addThem )
			{
				this.addedLotsOfModels = addThem;

				if ( this.numberOfModelsAdded == 0 )
				{
					AddModelToScene( "Barrel.mesh" );
					AddModelToScene( "facial.mesh" );
					AddModelToScene( "fish.mesh" );
					AddModelToScene( "ninja.mesh" );
					AddModelToScene( "penguin.mesh" );
					AddModelToScene( "razor.mesh" );
					AddModelToScene( "RZR-002.mesh" );
					AddModelToScene( "tudorhouse.mesh" );
					AddModelToScene( "WoodPallet.mesh" );
				}
				for ( int i = 0; i < this.lotsOfModelsNodes.Count; i++ )
				{
					this.lotsOfModelsNodes[ i ].IsVisible = this.addedLotsOfModels;
				}
			}
		}

		public void UpdateTargetObjInfo()
		{
			if ( this.targetObj == null )
			{
				return;
			}

			string targetObjMaterialName = string.Empty;

			if ( this.targetObj.MovableType == "Entity" )
			{
				var targetEnt = this.targetObj as Entity;
				targetObjMaterialName = targetEnt.GetSubEntity( 0 ).MaterialName;
			}

			this.targetObjMatName.Caption = targetObjMaterialName;
			if ( Viewport.MaterialScheme == ShaderGenerator.DefaultSchemeName )
			{
				var matMainEnt = (Material)MaterialManager.Instance.GetByName( targetObjMaterialName );

				if ( matMainEnt != null )
				{
					Technique shaderGeneratedTech = null;

					for ( int i = 0; i < matMainEnt.TechniqueCount; i++ )
					{
						Technique curTech = matMainEnt.GetTechnique( i );

						if ( curTech.SchemeName == ShaderGenerator.DefaultSchemeName )
						{
							shaderGeneratedTech = curTech;
							break;
						}
					}

					if ( shaderGeneratedTech != null )
					{
						this.targetObjVS.Caption = "VS: " + shaderGeneratedTech.GetPass( 0 ).VertexProgramName;
						this.targetObjFS.Caption = "FS: " + shaderGeneratedTech.GetPass( 0 ).FragmentProgramName;
					}
				}
			}
			else
			{
				this.targetObjVS.Caption = "VS: N/A";
				this.targetObjFS.Caption = "FS: N/A";
			}
		}

		private void CreateDirectionalLight()
		{
			Light light;
			Vector3 dir;

			light = SceneManager.CreateLight( DirectionalLightName );
			light.Type = LightType.Directional;
			light.CastShadows = true;
			dir = new Vector3( 0.5f, -1.0f, 0.3f );
			dir.Normalize();
			light.Direction = dir;
			light.Diffuse = new ColorEx( 0.65f, 0.15f, 0.15f );
			light.Specular = new ColorEx( 0.5f, 0.5f, 0.5f );

			//Create pivot node
			this.directionalLightNode = SceneManager.RootSceneNode.CreateChildSceneNode();

			//Create billboard set
			this.bbsFlare = SceneManager.CreateBillboardSet();
			this.bbsFlare.MaterialName = "Examples/Flare3";
			this.bbsFlare.CreateBillboard( -dir*500 ).Color = light.Diffuse;
			this.bbsFlare.CastShadows = false;

			this.directionalLightNode.AttachObject( this.bbsFlare );
			this.directionalLightNode.AttachObject( light );
		}

		private void CreatePointLight()
		{
			Light light;
			Vector3 dir;

			light = SceneManager.CreateLight( PointLightName );
			light.Type = LightType.Point;
			light.CastShadows = false;

			dir = new Vector3( 0.5, 0.0, 0.0f );
			dir.Normalize();

			light.Direction = dir;
			light.Diffuse = new ColorEx( 0.15f, 0.65f, 0.15f );
			light.Specular = new ColorEx( 0.5f, 0.5f, 0.5f );
			light.SetAttenuation( 200, 1, 0.0005f, 0.0f );
			//Create pivot point
			this.pointLightNode = SceneManager.RootSceneNode.CreateChildSceneNode();

			BillboardSet bbs;

			bbs = SceneManager.CreateBillboardSet();
			bbs.MaterialName = "Examples/Flare3";
			bbs.CreateBillboard( new Vector3( 200, 100, 0 ) ).Color = light.Diffuse;
			bbs.CastShadows = false;

			this.pointLightNode.AttachObject( bbs );
			this.pointLightNode.CreateChildSceneNode( new Vector3( 200, 100, 0 ) ).AttachObject( light );
		}

		private void CreateSpotLight()
		{
			Light light;
			Vector3 dir;

			light = SceneManager.CreateLight( SpotLightName );
			light.Type = LightType.Spotlight;
			light.CastShadows = false;
			dir = new Vector3( 0, 0, -1.0 );
			dir.Normalize();
			light.SetSpotlightRange( 20, 25, 0.95f );
			light.Diffuse = new ColorEx( 0.15f, 0.15f, 0.65f );
			light.Specular = new ColorEx( 0.5f, 0.5f, 0.5f );
			light.SetAttenuation( 1000.0f, 1.0f, 0.0005f, 0.0f );
		}

		private void UpdateInstancedViewports( bool enabled )
		{
			if ( this.instancedViewportsEnable != enabled )
			{
				this.instancedViewportsEnable = enabled;

				if ( this.instancedViewportsEnable )
				{
					Camera.CullFrustum = new InfiniteFrustum();

					// having problems with bb...
					this.directionalLightNode.DetachObject( this.bbsFlare );
				}
				else
				{
					Camera.CullFrustum = null;
					this.directionalLightNode.AttachObject( this.bbsFlare );
				}


				if ( this.instancedViewportsEnable )
				{
					CreateInstancedViewports();
				}
				else
				{
					DestroyInstancedViewports();
				}
			}
		}

		private void UpdateLightState( string lightName, bool visible )
		{
			if ( lightName == PointLightName )
			{
				if ( visible )
				{
					if ( this.pointLightNode.IsInSceneGraph == false )
					{
						SceneManager.RootSceneNode.AddChild( this.pointLightNode );
					}
				}
				else
				{
					if ( this.pointLightNode.IsInSceneGraph == true )
					{
						SceneManager.RootSceneNode.RemoveChild( this.pointLightNode );
					}
				}
				SceneManager.GetLight( lightName ).IsVisible = visible;
			}

				//Case it is the direction light
				//Toggle its visibility ad billboard set visiblity
			else if ( lightName == DirectionalLightName )
			{
				foreach ( var it in this.directionalLightNode.Objects )
				{
					it.IsVisible = visible;
				}
			}

				//spot light has no scene node representation.
			else
			{
				SceneManager.GetLight( lightName ).IsVisible = visible;
			}

			RenderState schemeRenderState = ShaderGenerator.Instance.GetRenderState( ShaderGenerator.DefaultSchemeName );

			var lightCount = new int[3]
			                 {
			                 	0, 0, 0
			                 };

			//Update point light count.
			if ( SceneManager.GetLight( PointLightName ).IsVisible )
			{
				lightCount[ 0 ] = 1;
			}
			//Update directional light count
			if ( SceneManager.GetLight( DirectionalLightName ).IsVisible )
			{
				lightCount[ 1 ] = 1;
			}
			//Update spot light count
			if ( SceneManager.GetLight( SpotLightName ).IsVisible )
			{
				lightCount[ 2 ] = 1;
			}

			//Update the scheme light count
			schemeRenderState.SetLightCount( lightCount );

			//Invalidate the scheme in order to regen all shaders based technique related to this scheme.
			ShaderGenerator.Instance.InvalidateScheme( ShaderGenerator.DefaultSchemeName );
		}

		private void UpdateSystemShaders()
		{
			foreach ( var it in this.targetEntities )
			{
				GenerateShaders( it );
			}
		}

		private void ExportRTShaderSystemMaterail( string fileName, string materialName )
		{
			var material = (Material)MaterialManager.Instance.GetByName( materialName );

			//Create shader based technique.
			bool success = ShaderGenerator.Instance.CreateShaderBasedTechnique( materialName, MaterialManager.DefaultSchemeName,
			                                                                    ShaderGenerator.DefaultSchemeName, false );

			if ( success )
			{
				//Force shader generation of the given material
				ShaderGenerator.Instance.ValidateMaterial( ShaderGenerator.DefaultSchemeName, materialName );

				//Grab the RTSS material serializer listener
				//TODO
			}
		}

		private void AddModelToScene( string modelName )
		{
			this.numberOfModelsAdded++;
			for ( int i = 0; i < 8; i++ )
			{
				float scaleFactor = 30;
				Entity entity;
				SceneNode childNode;
				entity = SceneManager.CreateEntity( "createdEnts" + this.numberOfModelsAdded.ToString(), modelName );
				this.lotsOfModelsEntitites.Add( entity );
				childNode = SceneManager.RootSceneNode.CreateChildSceneNode();
				this.lotsOfModelsNodes.Add( childNode );
				childNode.Position = new Vector3( this.numberOfModelsAdded*scaleFactor, 15, i*scaleFactor );
				childNode.AttachObject( entity );
				var modelMesh = (Mesh)MeshManager.Instance.GetByName( modelName );
				Vector3 modelSize = modelMesh.BoundingBox.Size;
				childNode.ScaleBy( new Vector3( 1/modelSize.x*scaleFactor,
				                                1/modelSize.y*scaleFactor,
				                                1/modelSize.z*scaleFactor ) );
			}
		}

		private void GenerateShaders( Entity entity )
		{
			for ( int i = 0; i < entity.SubEntityCount; i++ )
			{
				SubEntity curSubEntity = entity.GetSubEntity( i );
				string curMaterialName = curSubEntity.MaterialName;
				bool success;

				//Create the shader based technique of this material.
				success = ShaderGenerator.Instance.CreateShaderBasedTechnique( curMaterialName, MaterialManager.DefaultSchemeName,
				                                                               ShaderGenerator.DefaultSchemeName, false );

				//Setup custmo shader sub render states according to current setup.
				if ( success )
				{
					var curMaterial = (Material)MaterialManager.Instance.GetByName( curMaterialName );
					Pass curPass = curMaterial.GetTechnique( 0 ).GetPass( 0 );

					if ( this.specularEnable )
					{
						curPass.Specular = ColorEx.White;
						curPass.Shininess = 32;
					}
					else
					{
						curPass.Specular = ColorEx.Beige;
						curPass.Shininess = 0;
					}
					// Grab the first pass render state. 
					// NOTE: For more complicated samples iterate over the passes and build each one of them as desired.
					RenderState renderState = ShaderGenerator.Instance.GetRenderState( ShaderGenerator.DefaultSchemeName,
					                                                                   curMaterialName, 0 );

					//Remove all sub render states
					renderState.Reset();

					if ( this.curLightingModel == ShaderSystemLightingModel.PerVertexLighting )
					{
						SubRenderState perPerVertexLightModel = ShaderGenerator.Instance.CreateSubRenderState( FFPLighting.FFPType );
						renderState.AddTemplateSubRenderState( perPerVertexLightModel );
					}
					else if ( this.curLightingModel == ShaderSystemLightingModel.PerVertexLighting )
					{
						SubRenderState perPixelLightModel = ShaderGenerator.Instance.CreateSubRenderState( PerPixelLighting.SGXType );
						renderState.AddTemplateSubRenderState( perPixelLightModel );
					}
					else if ( this.curLightingModel == ShaderSystemLightingModel.NormalMapLightingTangentSpace )
					{
						//Apply normal map only on main entity.
						if ( entity.Name == MainEntityName )
						{
							SubRenderState subRenderState = ShaderGenerator.Instance.CreateSubRenderState( NormalMapLighting.SGXType );
							var normalMapSubRS = subRenderState as NormalMapLighting;

							normalMapSubRS.NormalMapSpace = NormalMapSpace.Tangent;
							normalMapSubRS.NormalMapTextureName = "Panels_Normal_Tangent.png";
							renderState.AddTemplateSubRenderState( normalMapSubRS );
						}
							//It is secondary entity -> use simple per pixel lighting
						else
						{
							SubRenderState perPixelLightModel = ShaderGenerator.Instance.CreateSubRenderState( PerPixelLighting.SGXType );
							renderState.AddTemplateSubRenderState( perPixelLightModel );
						}
					}
					else if ( this.curLightingModel == ShaderSystemLightingModel.NormalMapLightingObjectSpace )
					{
						//Apply normal map only on main entity
						if ( entity.Name == MainEntityName )
						{
							SubRenderState subRenderState = ShaderGenerator.Instance.CreateSubRenderState( NormalMapLighting.SGXType );
							var normalMapSubRS = subRenderState as NormalMapLighting;

							normalMapSubRS.NormalMapSpace = NormalMapSpace.Object;
							normalMapSubRS.NormalMapTextureName = "Panels_Normal_Obj.png";

							renderState.AddTemplateSubRenderState( normalMapSubRS );
						}

							//It is secondary entity -> use simple per pixel lighting.
						else
						{
							SubRenderState perPixelLightModel = ShaderGenerator.Instance.CreateSubRenderState( PerPixelLighting.SGXType );
							renderState.AddTemplateSubRenderState( perPixelLightModel );
						}
					}

					if ( this.reflectionMapEnable )
					{
						SubRenderState subRenderState = ShaderGenerator.Instance.CreateSubRenderState( ReflectionMap.SGXType );
						var reflectMapSubRs = subRenderState as ReflectionMap;

						reflectMapSubRs.ReflectionMapType = TextureType.CubeMap;
						reflectMapSubRs.ReflectionPower = this.reflectionPowerSlider.Value;

						//Setup the textures needed by the reflection effect
						reflectMapSubRs.MaskMapTextureName = "Panels_refmask.png";
						reflectMapSubRs.ReflectionMapTextureName = "cubescene.jpg";

						renderState.AddTemplateSubRenderState( subRenderState );
						this.reflectionMapSubRS = subRenderState;
					}
					else
					{
						this.reflectionMapSubRS = null;
					}
					//Invalidate this material in order to regen its shaders
					ShaderGenerator.Instance.InvalidateMaterial( ShaderGenerator.DefaultSchemeName, curMaterialName );
				}
			}
		}

		protected override void SetupView()
		{
			//Setup default viewport layout and camera
			Camera = SceneManager.CreateCamera( "MainCamera" );
			Viewport = Window.AddViewport( Camera );
			Camera.AspectRatio = Viewport.ActualWidth/Viewport.ActualHeight;
			Camera.Near = 5;

			CameraManager = new SdkCameraManager( Camera ); // create a default camera controller
		}

		protected override void SetupContent()
		{
			//Setup default effects values.
			this.curLightingModel = ShaderSystemLightingModel.PerVertexLighting;
			this.perPixelFogEnable = false;
			this.specularEnable = false;
			this.reflectionMapEnable = false;
			this.reflectionMapSubRS = null;
			this.layerBlendSubRS = null;

			this.rayQuery = SceneManager.CreateRayQuery( new Ray() );
			this.targetObj = null;

			//Set ambient
			SceneManager.AmbientLight = new ColorEx( 0.2f, 0.2f, 0.2f );

			SceneManager.SetSkyBox( true, "Examples/SceneCubMap2", 10000 );

			MeshManager.Instance.CreatePlane( "Myplane", ResourceGroupManager.DefaultResourceGroupName,
			                                  new Plane( Vector3.UnitY, 0 ),
			                                  1500, 1500, 25, 25, true, 1, 60, 60, Vector3.UnitZ );

			Entity planeEnt = SceneManager.CreateEntity( "plane", "Myplane" );
			planeEnt.MaterialName = "Examples/Rockwall";
			planeEnt.CastShadows = false;
			SceneManager.RootSceneNode.CreateChildSceneNode( Vector3.Zero ).AttachObject( planeEnt );

			//Load sample meshes an generate tangent vectors.
			for ( int i = 0; i < this.meshArray.Length; i++ )
			{
				string curMeshName = this.meshArray[ i ];

				Mesh mesh = MeshManager.Instance.Load( curMeshName, ResourceGroupManager.DefaultResourceGroupName,
				                                       BufferUsage.DynamicWriteOnly, BufferUsage.StaticWriteOnly );

				//Build tangent vectors, all our meshes use only 1 texture coordset
				//Note we can build into ves_tangent now (sm2+)
				short src, dst;
				if ( !mesh.SuggestTangentVectorBuildParams( out src, out dst ) )
				{
					mesh.BuildTangentVectors( src, dst );
				}
			}

			Entity entity;
			SceneNode childNode;

			//Create the main entity and mark it as the current target object
			entity = SceneManager.CreateEntity( MainEntityName, MainEntityMesh );
			this.targetEntities.Add( entity );
			childNode = SceneManager.RootSceneNode.CreateChildSceneNode();
			childNode.AttachObject( entity );
			this.targetObj = entity;
			childNode.ShowBoundingBox = true;

			//Create reflection entity that will show the exported material.
			string mainExportedMaterial = SceneManager.GetEntity( MainEntityName ).GetSubEntity( 0 ).MaterialName +
			                              "_RTSS_Export";
			var matMainEnt = (Material)MaterialManager.Instance.GetByName( mainExportedMaterial );

			entity = SceneManager.CreateEntity( "ExportedMaterialEntity", MainEntityMesh );
			entity.GetSubEntity( 0 ).Material = matMainEnt;
			childNode = SceneManager.RootSceneNode.CreateChildSceneNode();
			childNode.Position = new Vector3( 0, 200, -200 );
			childNode.AttachObject( entity );

			//Create texture layer blending demonstration entity
			this.layeredBlendingEntity = SceneManager.CreateEntity( "LayeredBlendingMaterialEntity", MainEntityMesh );
			this.layeredBlendingEntity.MaterialName = "RTSS/LayeredBlending";
			this.layeredBlendingEntity.GetSubEntity( 0 ).SetCustomParameter( 2, Vector4.Zero );
			childNode = SceneManager.RootSceneNode.CreateChildSceneNode();
			childNode.Position = new Vector3( 300, 200, -200 );
			childNode.AttachObject( this.layeredBlendingEntity );

			//Grab the render state of the material
			RenderState renderState = ShaderGenerator.Instance.GetRenderState( ShaderGenerator.DefaultSchemeName, "RTSS/LayeredBlending", 0 );

			if ( renderState != null )
			{
				var subRenderStateList = renderState.TemplateSubRenderStateList;

				for ( int i = 0; i < subRenderStateList.Count; i++ )
				{
					SubRenderState curSubRenderState = subRenderStateList[ i ];

					if ( curSubRenderState.Type == FFPTexturing.FFPType )
					{
						this.layerBlendSubRS = curSubRenderState as LayeredBlending;
						break;
					}
				}
			}
			//Create per pixel lighting demo entity
			entity = SceneManager.CreateEntity( "PerPixelEntity", "knot.mesh" );
			entity.MaterialName = "RTSS/PerPixel_SinglePass";
			childNode = SceneManager.RootSceneNode.CreateChildSceneNode();
			childNode.Position = new Vector3( 300, 100, -100 );
			childNode.AttachObject( entity );

			//Create normal map lighting demo entity
			entity = SceneManager.CreateEntity( "NormalMapEntity", "knot.mesh" );
			entity.MaterialName = "RTSS/NormalMapping_SinglePass";
			childNode = SceneManager.RootSceneNode.CreateChildSceneNode();
			childNode.Position = new Vector3( -300, 100, -100 );
			childNode.AttachObject( entity );

			//OpenGL ES 2.0 does not support texture atlases
			if ( !Root.Instance.RenderSystem.Name.Contains( "OpenGL ES 2" ) )
			{
				RenderState mainRenderState =
					ShaderGenerator.Instance.CreateOrRetrieveRenderState( ShaderGenerator.DefaultSchemeName ).Item1;
				mainRenderState.AddTemplateSubRenderState(
					ShaderGenerator.Instance.CreateSubRenderState( TextureAtlasSampler.SGXType ) );

				//Create texture atlas object and node
				ManualObject atlasObject = CreateTextureAtlasObject();
				childNode = SceneManager.RootSceneNode.CreateChildSceneNode();
				childNode.AttachObject( atlasObject );
			}

			CreateDirectionalLight();
			CreatePointLight();
			CreateSpotLight();

			RenderState schemeRenderState = ShaderGenerator.Instance.GetRenderState( ShaderGenerator.DefaultSchemeName );

			//take responsibility for updating the light count manually
			schemeRenderState.LightCountAutoUpdate = false;

			SetupUI();

			Camera.Position = new Vector3( 0, 300, 450 );

			Viewport.MaterialScheme = ShaderGenerator.DefaultSchemeName;

			//Mark system as on
			DetailsPanel.SetParamValue( 11, "On" );

			// a friendly reminder
			var names = new List<string>();
			names.Add( "Help" );
			TrayManager.CreateParamsPanel( TrayLocation.TopLeft, "Help", 100, names ).SetParamValue( 0, "H/F1" );
			UpdateSystemShaders();
		}

		private void SetupUI()
		{
			//Create language selection
			this.languageMenu = TrayManager.CreateLongSelectMenu( TrayLocation.TopLeft, "LangMode", "Language", 220, 120, 10 );

			//Use GLSL ES in case of OpenGL ES 2 render system
			if ( Root.RenderSystem.Name.Contains( "OpenGL ES 2" ) )
			{
				this.languageMenu.AddItem( "glsles" );
				ShaderGenerator.Instance.TargetLangauge = "glsles";
			}
			else if ( Root.RenderSystem.Name.Contains( "OpenGL" ) )
			{
				this.languageMenu.AddItem( "glsl" );
				ShaderGenerator.Instance.TargetLangauge = "glsl";
			}
			else if ( Root.RenderSystem.Name.Contains( "Direct3D9" ) )
			{
				this.languageMenu.AddItem( "hlsl" );
				ShaderGenerator.Instance.TargetLangauge = "hlsl";
			}
			this.languageMenu.AddItem( "cg" );

			//Create check boxes to toggle lights.
			this.dirLightCheckBox = TrayManager.CreateCheckBox( TrayLocation.TopLeft, DirectionalLightName, "Directional Light",
			                                                    220 );
			this.pointLightCheckBox = TrayManager.CreateCheckBox( TrayLocation.TopLeft, PointLightName, "Point Light", 220 );
			this.spotLightCheckBox = TrayManager.CreateCheckBox( TrayLocation.TopLeft, SpotLightName, "Spot Light", 220 );
			this.instancedViewportsCheckBox = TrayManager.CreateCheckBox( TrayLocation.TopLeft, InstancedViewportsName,
			                                                              "Instanced Viewports", 220 );
			this.addLotsOfModels = TrayManager.CreateCheckBox( TrayLocation.TopLeft, InstancedViewportsName, "Add Lots of Models",
			                                                   220 );

			this.dirLightCheckBox.IsChecked = true;
			this.dirLightCheckBox.CheckChanged += new CheckChangedHandler( CheckBoxToggled );
			this.pointLightCheckBox.IsChecked = true;
			this.pointLightCheckBox.CheckChanged += new CheckChangedHandler( CheckBoxToggled );
			this.spotLightCheckBox.IsChecked = false;
			this.spotLightCheckBox.CheckChanged += new CheckChangedHandler( CheckBoxToggled );
			this.instancedViewportsCheckBox.IsChecked = false;
			this.instancedViewportsCheckBox.CheckChanged += new CheckChangedHandler( CheckBoxToggled );
			this.addLotsOfModels.IsChecked = false;
			this.addLotsOfModels.CheckChanged += new CheckChangedHandler( CheckBoxToggled );
			CheckBox cb;
			cb = TrayManager.CreateCheckBox( TrayLocation.TopLeft, PerPixelFogBox, "Per Pixel Fog", 220 );
			cb.IsChecked = this.perPixelFogEnable;
			cb.CheckChanged += new CheckChangedHandler( CheckBoxToggled );
			cb = TrayManager.CreateCheckBox( TrayLocation.TopLeft, AtlasAutoBorderMode, "Atlas auto border", 220 );
			cb.IsChecked = true;
			cb.CheckChanged += new CheckChangedHandler( CheckBoxToggled );


			//Create fog widgets.
			this.fogModeMenu = TrayManager.CreateLongSelectMenu( TrayLocation.TopLeft, "FogMode", "Fog Mode", 220, 120, 10 );
			this.fogModeMenu.AddItem( "None" );
			this.fogModeMenu.AddItem( "Exp" );
			this.fogModeMenu.AddItem( "Exp2" );
			this.fogModeMenu.AddItem( "Linear" );

			//Create shadow menu
			this.shadowMenu = TrayManager.CreateLongSelectMenu( TrayLocation.TopLeft, "ShadowType", "Shadow", 220, 120, 10 );
			this.shadowMenu.AddItem( "None" );

			//shadowMenu.AddItem("PSSM 3");

			//Flush shader cache button.
			var b = TrayManager.CreateButton( TrayLocation.TopLeft, FlushButtonName, "Flush Shader Cache", 220 );

			//Create target model widgets
			this.targetObjMatName = TrayManager.CreateLabel( TrayLocation.TopLeft, "TargetObjMatName", string.Empty, 220 );
			this.targetObjVS = TrayManager.CreateLabel( TrayLocation.TopLeft, "TargetObjVS", string.Empty, 220 );
			this.targetObjFS = TrayManager.CreateLabel( TrayLocation.TopLeft, "TargetObjFS", string.Empty, 220 );

			//Create main entity widgets.
			TrayManager.CreateLabel( TrayLocation.Bottom, "MainEntityLable", "Main Entity Settings", 240 );
			var scb = TrayManager.CreateCheckBox( TrayLocation.Bottom, SpecularBox, "Specular", 240 );
			scb.IsChecked = this.specularEnable;
			scb.CheckChanged += new CheckChangedHandler( CheckBoxToggled );

			//Allow reflection map only on PS3 and above since with all lights on + specular + bump we
			//exceed the instruction count limits of PS2
			if ( GpuProgramManager.Instance.IsSyntaxSupported( "ps_3_0" ) ||
			     GpuProgramManager.Instance.IsSyntaxSupported( "glsles" ) ||
			     GpuProgramManager.Instance.IsSyntaxSupported( "fp30" ) )
			{
				var rcb = TrayManager.CreateCheckBox( TrayLocation.Bottom, ReflectionMapBox, "Reflection Map", 240 );
				rcb.IsChecked = true;
				rcb.CheckChanged += new CheckChangedHandler( CheckBoxToggled );

				this.reflectionPowerSlider = TrayManager.CreateThickSlider( TrayLocation.Bottom, ReflectionMapPowerSlider,
				                                                            "Reflection Power", 240, 80, 0, 1, 100 );
				this.reflectionPowerSlider.SetValue( 0.5f, false );
				this.reflectionPowerSlider.SliderMoved += new SliderMovedHandler( SliderMoved );
			}

			this.lightingModelMenu = TrayManager.CreateLongSelectMenu( TrayLocation.Bottom, "targetModelLighting", string.Empty,
			                                                           240,
			                                                           230, 10 );
			this.lightingModelMenu.AddItem( "Per Vertex" );
			this.lightingModelMenu.AddItem( "Per Pixel" );
			this.lightingModelMenu.AddItem( "Normal Map - Tangent Space" );
			this.lightingModelMenu.AddItem( "Normal Map - Object Space" );

			TrayManager.CreateButton( TrayLocation.Bottom, ExportButtonName, "Export Material", 240 );
			this.layerBlendLabel = TrayManager.CreateLabel( TrayLocation.Right, "Blend Type", "Blend Type", 240 );
			TrayManager.CreateButton( TrayLocation.Right, LayerblendButtonName, "Change Blend Type", 220 );
			this.modifierValueSlider = TrayManager.CreateThickSlider( TrayLocation.Right, ModifierValueSlider, "Modifier", 240,
			                                                          80, 0,
			                                                          1, 100 );
			this.modifierValueSlider.SetValue( 0.0f, false );
			this.modifierValueSlider.SliderMoved += new SliderMovedHandler( SliderMoved );

			UpdateLayerBlendingCaption( this.layerBlendSubRS.GetBlendMode( 1 ) );

			TrayManager.ShowCursor();
		}

		protected override void CleanupContent()
		{
			//Unload sample meshes and generate tangent vectors.
			for ( int i = 0; i < this.meshArray.Length; i++ )
			{
				string curMeshName = this.meshArray[ i ];
				MeshManager.Instance.Unload( curMeshName );
			}

			MeshManager.Instance.Remove( MainEntityMesh );
			this.targetEntities.Clear();

			MeshManager.Instance.Remove( "Myplane" );
		}

		protected override void LoadResources()
		{
			//Create and add the custom refledcdtion map shader extension factory to the shader generator.
			this.reflectionMapFactory = new ReflectionMapFactory();
			ShaderGenerator.Instance.AddSubRenderStateFactory( this.reflectionMapFactory );

			CreatePrivateResourceGroup();
		}

		private void CreatePrivateResourceGroup()
		{
			//Create the resource group of the RT Shader System Sample.
			ResourceGroupManager rgm = ResourceGroupManager.Instance;

			this.exportMaterialPath = "C:/";

			rgm.CreateResourceGroup( SampleMaterialGroup );
			rgm.AddResourceLocation( this.exportMaterialPath, "FileSystem", SampleMaterialGroup );
			rgm.InitializeResourceGroup( SampleMaterialGroup );
			rgm.LoadResourceGroup( SampleMaterialGroup, true, false );
		}

		protected override void UnloadResources()
		{
			DestroyPrivateResourceGroup();
			ShaderGenerator.Instance.RemoveAllShaderBasedTechniques( "Panels" );
			ShaderGenerator.Instance.RemoveAllShaderBasedTechniques( "Panels_RTSS_Export" );

			if ( this.reflectionMapFactory != null )
			{
				ShaderGenerator.Instance.RemoveSubRenderStateFactory( this.reflectionMapFactory );
				this.reflectionMapFactory = null;
			}
		}

		private void CreateInstancedViewports()
		{
			if ( this.instancedViewportsFactory == null )
			{
				this.instancedViewportsFactory = null; // = new ShaderExInStancedViewportsFactory();
				ShaderGenerator.Instance.AddSubRenderStateFactory( this.instancedViewportsFactory );
			}

			var monitorCount = new Vector2( 2.0f, 2.0f );
			this.instancedViewportsSubRenderState = ShaderGenerator.Instance.CreateSubRenderState( InstancedViewports.SGXType );
			var shaderExInstancedViewports = this.instancedViewportsSubRenderState as InstancedViewports;
			shaderExInstancedViewports.MonitorsCount = monitorCount;
			RenderState renderState = ShaderGenerator.Instance.GetRenderState( ShaderGenerator.DefaultSchemeName );
			renderState.AddTemplateSubRenderState( this.instancedViewportsSubRenderState );


			VertexDeclaration vertexDeclaration = HardwareBufferManager.Instance.CreateVertexDeclaration();
			int offset = 0;
			offset = vertexDeclaration.GetVertexSize( 0 );
			vertexDeclaration.AddElement( 0, offset, VertexElementType.Float4, VertexElementSemantic.TexCoords, 3 );
			offset = vertexDeclaration.GetVertexSize( 0 );
			vertexDeclaration.AddElement( 0, offset, VertexElementType.Float4, VertexElementSemantic.TexCoords, 4 );
			offset = vertexDeclaration.GetVertexSize( 0 );
			vertexDeclaration.AddElement( 0, offset, VertexElementType.Float4, VertexElementSemantic.TexCoords, 5 );
			offset = vertexDeclaration.GetVertexSize( 0 );
			vertexDeclaration.AddElement( 0, offset, VertexElementType.Float4, VertexElementSemantic.TexCoords, 6 );
			offset = vertexDeclaration.GetVertexSize( 0 );
			vertexDeclaration.AddElement( 0, offset, VertexElementType.Float4, VertexElementSemantic.TexCoords, 7 );


			//todo
			// vbuf.Unlock();

			// Root.RenderSystem.GlobalInstanceVertexBuffer = vbuf;
			Root.RenderSystem.GlobalInstanceVertexBufferVertexDeclaration = vertexDeclaration;
			Root.RenderSystem.GlobalNumberOfInstances = (int)( monitorCount.x*monitorCount.y );

			//Invalidate the scheme
			ShaderGenerator.Instance.InvalidateScheme( ShaderGenerator.DefaultSchemeName );
			ShaderGenerator.Instance.ValidateScheme( ShaderGenerator.DefaultSchemeName );
		}

		private void DestroyInstancedViewports()
		{
			if ( this.instancedViewportsSubRenderState != null )
			{
				RenderState renderState = ShaderGenerator.Instance.GetRenderState( ShaderGenerator.DefaultSchemeName );
				renderState.RemoveTemplateSubRenderState( this.instancedViewportsSubRenderState );
				this.instancedViewportsSubRenderState = null;
			}

			if ( Root.RenderSystem.GlobalInstanceVertexBufferVertexDeclaration != null )
			{
				//todo
				//HardwareBufferManager.Instance.DestroyVertexBufferBinding(Root.RenderSystem.GlobalInstanceVertexBufferVertexDeclaration);
				Root.RenderSystem.GlobalInstanceVertexBufferVertexDeclaration = null;
			}
			Root.RenderSystem.GlobalNumberOfInstances = 1;
			//todo
			//Root.RenderSystem.GlobalInstanceVertexBuffer = new HardwareVertexBuffer();

			ShaderGenerator.Instance.InvalidateScheme( ShaderGenerator.DefaultSchemeName );
			ShaderGenerator.Instance.ValidateScheme( ShaderGenerator.DefaultSchemeName );

			DestroyInstancedViewports();
		}

		private void DestroyInstancedViewportsFactory()
		{
			if ( this.instancedViewportsFactory != null )
			{
				this.instancedViewportsFactory.DestroyAllInstances();
				ShaderGenerator.Instance.RemoveSubRenderStateFactory( this.instancedViewportsFactory );
				this.instancedViewportsFactory = null;
			}
		}

		private void DestroyPrivateResourceGroup()
		{
			ResourceGroupManager.Instance.DestroyResourceGroup( SampleMaterialGroup );
		}

		private void PickTargetObject( SharpInputSystem.MouseEventArgs evt )
		{
			int xPos = evt.State.X.Absolute;
			int yPos = evt.State.Y.Absolute;
			int width = evt.State.Width;
			int height = evt.State.Height;

			Ray mouseRay = Camera.GetCameraToViewportRay( xPos/(float)width, yPos/(float)height );
			this.rayQuery.Ray = mouseRay;

			var result = this.rayQuery.Execute();

			foreach ( var curEntry in result )
			{
				if ( this.targetObj != null )
				{
					this.targetObj.ParentSceneNode.ShowBoundingBox = false;
				}

				this.targetObj = curEntry.SceneObject;
				this.targetObj.ParentSceneNode.ShowBoundingBox = true;
			}
		}

		private void SetAtlasBorderMode( bool enable )
		{
			TextureAtlasSamplerFactory.Instance.SetDefaultAtlasingAttributes( IndexPositionMode.Relative, 1, enable );
			ShaderGenerator.Instance.InvalidateScheme( ShaderGenerator.DefaultSchemeName );
		}

		private void ApplyShadowType( int menuIndex )
		{
			RenderState schemeRenderState = ShaderGenerator.Instance.GetRenderState( ShaderGenerator.DefaultSchemeName );

			//No shadow
			if ( menuIndex == 0 )
			{
				SceneManager.ShadowTechnique = ShadowTechnique.None;

				var subRenderStateList = schemeRenderState.TemplateSubRenderStateList;

				foreach ( var curSubRenderState in subRenderStateList )
				{
					//this is the pssm3 sub render state -> remove it
					if ( curSubRenderState.Type == IntegratedPSSM3.SGXType )
					{
						schemeRenderState.RemoveTemplateSubRenderState( curSubRenderState );
						break;
					}
				}

				TrayManager.MoveWidgetToTray( this.dirLightCheckBox, TrayLocation.TopLeft, 1 );
				TrayManager.MoveWidgetToTray( this.pointLightCheckBox, TrayLocation.TopLeft, 2 );
				TrayManager.MoveWidgetToTray( this.spotLightCheckBox, TrayLocation.TopLeft, 3 );

				this.dirLightCheckBox.Show();
				this.pointLightCheckBox.Show();
				this.spotLightCheckBox.Show();
			}
			else if ( menuIndex == 1 )
			{
				SceneManager.ShadowTechnique = ShadowTechnique.TextureModulative;

				// 3 textures per directional light
				//SceneManager.setShadowTextureCountPerLightType(LightType.Directional, 3);
				SceneManager.SetShadowTextureSettings( 512, 3, Media.PixelFormat.FLOAT32_R );
				SceneManager.ShadowTextureSelfShadow = true;

				//Leave only directional light
				this.dirLightCheckBox.IsChecked = true;
				this.pointLightCheckBox.IsChecked = false;
				this.spotLightCheckBox.IsChecked = false;

				TrayManager.RemoveWidgetFromTray( this.dirLightCheckBox );
				TrayManager.RemoveWidgetFromTray( this.pointLightCheckBox );
				TrayManager.RemoveWidgetFromTray( this.spotLightCheckBox );
				this.dirLightCheckBox.Hide();
				this.pointLightCheckBox.Hide();
				this.spotLightCheckBox.Hide();

				//Set up caster material - this is just a standard depth/shadow map caster
				SceneManager.ShadowTextureCasterMaterial = "PSSM/shadow_caster";

				//Disable fog on the caster pass.
				var passCasterMaterial = (Material)MaterialManager.Instance.GetByName( "PSSM/shadow_caster" );
				Pass pssmCasterPass = passCasterMaterial.GetTechnique( 0 ).GetPass( 0 );
				pssmCasterPass.SetFog( true );

				//Shadow camera settup
				//TODO
				//PSSMShadowCameraSetup

				SubRenderState subRenderState = ShaderGenerator.Instance.CreateSubRenderState( IntegratedPSSM3.SGXType );
				var pssm3SubRenderState = subRenderState as IntegratedPSSM3;
				//todo
			}

			//Invalidate the scheme
			ShaderGenerator.Instance.InvalidateScheme( ShaderGenerator.DefaultSchemeName );
		}

		private void ChangeTextureLayerBlendMode()
		{
			LayeredBlending.BlendMode curBlendMode = this.layerBlendSubRS.GetBlendMode( 1 );
			LayeredBlending.BlendMode nextBlendMode;

			//Update the next blend layer mode.
			if ( curBlendMode == LayeredBlending.BlendMode.Luminosity )
			{
				nextBlendMode = LayeredBlending.BlendMode.FFPBlend;
			}
			else
			{
				nextBlendMode = (LayeredBlending.BlendMode)( curBlendMode + 1 );
			}

			this.layerBlendSubRS.SetBlendMode( 1, nextBlendMode );
			ShaderGenerator.Instance.InvalidateMaterial( ShaderGenerator.DefaultSchemeName, "RTSS/LayeredBlending" );

			//Update the caption.
			UpdateLayerBlendingCaption( nextBlendMode );
		}

		private void UpdateLayerBlendingCaption( LayeredBlending.BlendMode nextBlendMode )
		{
			this.layerBlendLabel.Caption = nextBlendMode.ToString();
		}

		private ManualObject CreateTextureAtlasObject()
		{
			var textureAtlasSamplerFactory =
				ShaderGenerator.Instance.GetSubRenderStateFactory( TextureAtlasSampler.SGXType ) as TextureAtlasSamplerFactory;
			var textureAtlasTable = new List<TextureAtlasRecord>();

			var taiFile = ResourceGroupManager.Instance.OpenResource( "TextureAtlasSamplerWrap.tai" );

			textureAtlasSamplerFactory.AddTextureAtlasDefinition( new System.IO.StreamReader( taiFile ), textureAtlasTable );

			//Generate the geometry that will seed the particle system
			ManualObject textureAtlasObject = SceneManager.CreateManualObject( "TextureAtlasObject" );

			int sliceSize = 30;
			int wrapSize = 5;

			string curMatName = string.Empty;

			//Create original texture geometry
			for ( int i = 0; i < textureAtlasTable.Count; i++ )
			{
				bool changeMat = ( curMatName != textureAtlasTable[ i ].atlasTextureName );

				if ( changeMat )
				{
					if ( curMatName != string.Empty )
					{
						textureAtlasObject.End();
					}
					curMatName = textureAtlasTable[ i ].originalTextureName;
					CreateMaterialForTexture( curMatName, false );
					textureAtlasObject.Begin( curMatName, OperationType.TriangleList );
				}

				//triangle 0
				textureAtlasObject.Position( new Vector3( i*sliceSize, 0, 0 ) );
				textureAtlasObject.TextureCoord( 0, 0 );

				textureAtlasObject.Position( new Vector3( i*sliceSize, 0, sliceSize ) );
				textureAtlasObject.TextureCoord( 0, wrapSize );

				textureAtlasObject.Position( new Vector3( ( i + 1 )*sliceSize, 0, sliceSize ) );
				textureAtlasObject.TextureCoord( wrapSize, wrapSize );

				//triangle 1
				textureAtlasObject.Position( i*sliceSize, 0, 0 );
				textureAtlasObject.TextureCoord( 0, 0 );

				textureAtlasObject.Position( ( i + 1 )*sliceSize, 0, 0 );
				textureAtlasObject.TextureCoord( wrapSize, wrapSize );

				textureAtlasObject.Position( ( i + 1 )*sliceSize, 0, 0 );
				textureAtlasObject.TextureCoord( wrapSize, 0 );
			}

			//create texture atlas geometry
			for ( int i = 0; i < textureAtlasTable.Count; i++ )
			{
				bool changeMat = ( curMatName != textureAtlasTable[ i ].atlasTextureName );

				if ( changeMat )
				{
					if ( curMatName != string.Empty )
					{
						textureAtlasObject.End();
					}

					curMatName = textureAtlasTable[ i ].atlasTextureName;
					CreateMaterialForTexture( curMatName, true );
					textureAtlasObject.Begin( curMatName, OperationType.TriangleList );
				}
				//triangle 0
				textureAtlasObject.Position( i*sliceSize, 0, sliceSize ); //Position
				textureAtlasObject.TextureCoord( 0, 0 ); //UV
				textureAtlasObject.TextureCoord( textureAtlasTable[ i ].indexInAtlas );

				textureAtlasObject.Position( i*sliceSize, 0, sliceSize*2 );
				textureAtlasObject.TextureCoord( 0, 0 );
				textureAtlasObject.TextureCoord( textureAtlasTable[ i ].indexInAtlas ); //Texture ID

				textureAtlasObject.Position( ( i + 1 )*sliceSize, 0, sliceSize*2 );
				textureAtlasObject.TextureCoord( wrapSize, wrapSize );
				textureAtlasObject.TextureCoord( textureAtlasTable[ i ].indexInAtlas );

				//Triangle 1
				textureAtlasObject.Position( i*sliceSize, 0, sliceSize );
				textureAtlasObject.TextureCoord( 0, 0 );
				textureAtlasObject.TextureCoord( textureAtlasTable[ i ].indexInAtlas );

				textureAtlasObject.Position( ( i + 1 )*sliceSize, 0, sliceSize*2 );
				textureAtlasObject.TextureCoord( wrapSize, wrapSize );
				textureAtlasObject.TextureCoord( textureAtlasTable[ i ].indexInAtlas );

				textureAtlasObject.Position( ( i + 1 )*sliceSize, 0, sliceSize*2 );
				textureAtlasObject.TextureCoord( wrapSize, 0 );
				textureAtlasObject.TextureCoord( textureAtlasTable[ i ].indexInAtlas );
			}
			textureAtlasObject.End();

			return textureAtlasObject;
		}

		private void CreateMaterialForTexture( string texName, bool isTextureAtlasTexture )
		{
			MaterialManager matMgr = MaterialManager.Instance;
			if ( matMgr.ResourceExists( texName ) == false )
			{
				var newMat = (Material)matMgr.Create( texName, ResourceGroupManager.DefaultResourceGroupName );
				newMat.GetTechnique( 0 ).GetPass( 0 ).LightingEnabled = false;
				TextureUnitState state = newMat.GetTechnique( 0 ).GetPass( 0 ).CreateTextureUnitState( texName );
				if ( isTextureAtlasTexture )
				{
					// to solve wrap edge bleed
					state.SetTextureFiltering( TextureFiltering.Trilinear );
				}
			}
		}

		private ShaderSystemLightingModel CurrentLightingModel
		{
			get
			{
				return this.curLightingModel;
			}
			set
			{
				if ( this.curLightingModel != value )
				{
					this.curLightingModel = value;

					foreach ( var it in this.targetEntities )
					{
						GenerateShaders( it );
					}
				}
			}
		}

		private bool SpecularEnable
		{
			get
			{
				return this.specularEnable;
			}
			set
			{
				this.specularEnable = value;
				UpdateSystemShaders();
			}
		}

		private bool ReflectionMapEnable
		{
			get
			{
				return this.reflectionMapEnable;
			}
			set
			{
				this.reflectionMapEnable = value;
			}
		}

		private bool PerPixelFogEnable
		{
			get
			{
				return this.perPixelFogEnable;
			}
			set
			{
				if ( this.perPixelFogEnable != value )
				{
					this.perPixelFogEnable = value;

					//Grab the scheme render state.
					RenderState schemeRenderState = ShaderGenerator.Instance.GetRenderState( ShaderGenerator.DefaultSchemeName );
					var subRenderStateList = schemeRenderState.TemplateSubRenderStateList;

					FFPFog fogSubRenderState = null;

					//Search for the fog sub state
					foreach ( var curSubRenderState in subRenderStateList )
					{
						if ( curSubRenderState.Type == FFPFog.FFPType )
						{
							fogSubRenderState = curSubRenderState as FFPFog;
							break;
						}
					}

					//Create the fog sub render state if need to
					if ( fogSubRenderState == null )
					{
						SubRenderState subRenderState = ShaderGenerator.Instance.CreateSubRenderState( FFPFog.FFPType );

						fogSubRenderState = subRenderState as FFPFog;
						schemeRenderState.AddTemplateSubRenderState( fogSubRenderState );
					}

					//Select the desired fog calculation mode.
					if ( this.perPixelFogEnable )
					{
						fogSubRenderState.CalculationMode = FFPFog.CalcMode.PerPixel;
					}
					else
					{
						fogSubRenderState.CalculationMode = FFPFog.CalcMode.PerVertex;
					}

					//Invalidate the scheme in order to regen all shaders based technique related to this scheme
					ShaderGenerator.Instance.InvalidateScheme( ShaderGenerator.DefaultSchemeName );
				}
			}
		}

		private bool InstancedViewportsEnable
		{
			get
			{
				return this.instancedViewportsEnable;
			}
			set
			{
				this.instancedViewportsEnable = value;
			}
		}

		public override IList<string> RequiredPlugins
		{
			get
			{
				var names = new List<string>();
				if ( !GpuProgramManager.Instance.IsSyntaxSupported( "glsles" ) &&
				     !GpuProgramManager.Instance.IsSyntaxSupported( "glsl" ) )
				{
					names.Add( "Cg Program Manager" );
				}

				return names;
			}
		}
	}

	/// <summary>
	/// A hack class to get infinite frustum - needed by instanced viewports demo
	/// A better solution will be to check the frustums of all the viewports in a similar class
	/// </summary>
	internal class InfiniteFrustum : Frustum
	{
		private readonly Dictionary<FrustumPlane, Plane> frustumPlanes = new Dictionary<FrustumPlane, Plane>();

		public InfiniteFrustum()
		{
			var p = new Plane();
			p.Normal = Vector3.NegativeUnitX;
			p.D = 9999999999999999999.0f;
			this.frustumPlanes.Add( FrustumPlane.Left, p );

			p = new Plane();
			p.Normal = Vector3.UnitX;
			p.D = 9999999999999999999.0f;
			this.frustumPlanes.Add( FrustumPlane.Right, p );

			p = new Plane();
			p.Normal = Vector3.NegativeUnitY;
			p.D = 9999999999999999999.0f;
			this.frustumPlanes.Add( FrustumPlane.Top, p );

			p = new Plane();
			p.Normal = Vector3.UnitY;
			p.D = 9999999999999999999.0f;
			this.frustumPlanes.Add( FrustumPlane.Bottom, p );

			p = new Plane();
			p.Normal = Vector3.NegativeUnitZ;
			p.D = 9999999999999999999.0f;
			this.frustumPlanes.Add( FrustumPlane.Near, p );

			p = new Plane();
			p.Normal = Vector3.UnitZ;
			p.D = 9999999999999999999.0f;
			this.frustumPlanes.Add( FrustumPlane.Far, p );
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="bound"></param>
		/// <param name="culledBy">Default is Near</param>
		/// <returns></returns>
		public new virtual bool IsVisible( AxisAlignedBox bound, FrustumPlane culledBy )
		{
			return true;
		}

		public new virtual bool IsVisible( Sphere bound, FrustumPlane culledBy )
		{
			return true;
		}

		public new virtual bool IsVisible( Vector3 vert, FrustumPlane culledBy )
		{
			return true;
		}

		public bool ProjectSphere( Sphere sphere, Real left, Real top, Real right, Real bottom )
		{
			left = bottom = -1.0f;
			right = top = 1.0f;
			return true;
		}

		/// <summary>
		/// Gets NearClipDistance
		/// </summary>
		public Real NearClipDistance
		{
			get
			{
				return 1.0;
			}
		}

		/// <summary>
		/// Gets FarClipDistance
		/// </summary>
		public Real FarClipDistance
		{
			get
			{
				return 9999999999999999999.0;
			}
		}

		public Plane GetFrustumPlane( FrustumPlane plane )
		{
			return this.frustumPlanes[ plane ];
		}
	}
}