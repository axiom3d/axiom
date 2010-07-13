#region Namespace Declarations

using System;
using System.Diagnostics;
using Axiom.Core;
using Axiom.Graphics;
using Axiom.Input;
using Axiom.Math;
using Axiom.Overlays;

#endregion Namespace Declarations

namespace Axiom.Demos
{
	/// <summary>
	///     Demonstrates dotproduct blending operation and normalization cube map
	///     usage for achieving bump mapping effect.
	/// </summary>
	public class Dot3Bump : TechDemo
	{
		#region Fields

		const int NUM_ENTITIES = 3;
		const int NUM_MATERIALS = 6;
		const int NUM_LIGHTS = 3;

		float timeDelay = 0.0f;

		Entity[] entities = new Entity[ NUM_LIGHTS ];
		string[] entityMeshes = new string[] { "athene.mesh", "knot.mesh", "ogrehead.mesh" };
		Light[] lights = new Light[ NUM_LIGHTS ];
		BillboardSet[] lightFlareSets = new BillboardSet[ NUM_LIGHTS ];
		Billboard[] lightFlares = new Billboard[ NUM_LIGHTS ];
		Vector3[] lightPositions = new Vector3[] {
                                                     new Vector3(300, 0, 0),
                                                     new Vector3(-200, 50, 0),
                                                     new Vector3(0, -300, -100)
                                                 };

		float[] lightRotationAngles = new float[] { 0, 30, 75 };

		Vector3[] lightRotationAxes = new Vector3[] {
                                                        Vector3.UnitX,
                                                        Vector3.UnitZ,
                                                        Vector3.UnitY
                                                    };

		float[] lightSpeeds = new float[] { 30, 10, 50 };

		ColorEx[] diffuseLightColors = new ColorEx[] {
                                                         new ColorEx(1, 1, 1, 1),
                                                         new ColorEx(1, 1, 0, 0),
                                                         new ColorEx(1, 1, 1, 0.5f)
                                                     };

		ColorEx[] specularLightColors = new ColorEx[] {
                                                          new ColorEx(1, 1, 1, 1),
                                                          new ColorEx(1, 0, 0.8f, 0.8f),
                                                          new ColorEx(1, 1, 1, 0.8f)
                                                      };

		bool[] lightState = new bool[] { true, true, false };

		string[ , ] materialNames = new string[ , ] {
													// athene
													{
														"Examples/Athene/NormalMapped",
														"Examples/Athene/NormalMappedSpecular",
														"Examples/Athene/NormalMapped",
														"Examples/ShowUV",
														"Examples/ShowNormals",
														"Examples/ShowTangents"
													},
													// knot
													{
														"Examples/BumpMapping/MultiLight",
														"Examples/BumpMapping/MultiLightSpecular",
														"Examples/OffsetMapping/Specular",
														"Examples/ShowUV",
														"Examples/ShowNormals",
														"Examples/ShowTangents"
													},
													// ogre head
													{
														"Examples/BumpMapping/MultiLight",
														"Examples/BumpMapping/MultiLightSpecular",
														"Examples/OffsetMapping/Specular",
														"Examples/ShowUV",
														"Examples/ShowNormals",
														"Examples/ShowTangents"
													}
												  };

		int currentMaterial = 0;
		int currentEntity = 0;

		SceneNode mainNode;
		SceneNode[] lightNodes = new SceneNode[ NUM_LIGHTS ];
		SceneNode[] lightPivots = new SceneNode[ NUM_LIGHTS ];

		OverlayElement objectInfo, materialInfo, info;

		#endregion Fields

		public override void CreateScene()
		{
			// Check prerequisites first
			RenderSystemCapabilities caps = Root.Instance.RenderSystem.HardwareCapabilities;
			if ( !caps.HasCapability( Capabilities.VertexPrograms ) || !caps.HasCapability( Capabilities.FragmentPrograms ) )
			{
				throw new AxiomException( "Your card does not support vertex and fragment programs, so cannot run this demo. Sorry!" );
			}
			else
			{
				if ( !GpuProgramManager.Instance.IsSyntaxSupported( "arbfp1" ) &&
					!GpuProgramManager.Instance.IsSyntaxSupported( "ps_2_0" ) )
				{
					throw new AxiomException( "Your card does not support shader model 2, so cannot run this demo. Sorry!" );
				}
			}

			scene.AmbientLight = ColorEx.Black;

			// create scene node
			mainNode = scene.RootSceneNode.CreateChildSceneNode();

			// Load the meshes with non-default HBU options
			for ( int mn = 0; mn < NUM_ENTITIES; mn++ )
			{
				Mesh mesh = MeshManager.Instance.Load( entityMeshes[ mn ], ResourceGroupManager.DefaultResourceGroupName,
					BufferUsage.DynamicWriteOnly,
					BufferUsage.StaticWriteOnly,
					true, true, 1 ); //so we can still read it

				// Build tangent vectors, all our meshes use only 1 texture coordset
				short srcIdx, destIdx;

				if ( !mesh.SuggestTangentVectorBuildParams( out srcIdx, out destIdx ) )
				{
					mesh.BuildTangentVectors( srcIdx, destIdx );
				}

				// Create entity
				entities[ mn ] = scene.CreateEntity( "Ent" + mn.ToString(), entityMeshes[ mn ] );

				// Attach to child of root node
				mainNode.AttachObject( entities[ mn ] );

				// Make invisible, except for index 0
				if ( mn == 0 )
				{
					entities[ mn ].MaterialName = materialNames[ currentEntity, currentMaterial ];
				}
				else
				{
					entities[ mn ].IsVisible = false;
				}
			}

			for ( int i = 0; i < NUM_LIGHTS; i++ )
			{
				lightPivots[ i ] = scene.RootSceneNode.CreateChildSceneNode();
				lightPivots[ i ].Rotate( lightRotationAxes[ i ], lightRotationAngles[ i ] );

				// Create a light, use default parameters
				lights[ i ] = scene.CreateLight( "Light" + i.ToString() );
				lights[ i ].Position = lightPositions[ i ];
				lights[ i ].Diffuse = diffuseLightColors[ i ];
				lights[ i ].Specular = specularLightColors[ i ];
				lights[ i ].IsVisible = lightState[ i ];

				// Attach light
				lightPivots[ i ].AttachObject( lights[ i ] );

				// Create billboard for light
				lightFlareSets[ i ] = scene.CreateBillboardSet( "Flare" + i.ToString() );
				lightFlareSets[ i ].MaterialName = "Particles/Flare";
				lightPivots[ i ].AttachObject( lightFlareSets[ i ] );
				lightFlares[ i ] = lightFlareSets[ i ].CreateBillboard( lightPositions[ i ] );
				lightFlares[ i ].Color = diffuseLightColors[ i ];
				lightFlareSets[ i ].IsVisible = lightState[ i ];
			}
			// move the camera a bit right and make it look at the knot
			camera.MoveRelative( new Vector3( 50, 0, 20 ) );
			camera.LookAt( new Vector3( 0, 0, 0 ) );

			// show overlay
			Overlay pOver = OverlayManager.Instance.GetByName( "Example/DP3Overlay" );
			objectInfo = OverlayManager.Instance.Elements.GetElement( "Example/DP3/ObjectInfo" );
			materialInfo = OverlayManager.Instance.Elements.GetElement( "Example/DP3/MaterialInfo" );
			info = OverlayManager.Instance.Elements.GetElement( "Example/DP3/Info" );

			objectInfo.Text = "Current: " + entityMeshes[ currentEntity ];
			materialInfo.Text = "Current: " + materialNames[ currentEntity, currentMaterial ];
			if ( !caps.HasCapability( Capabilities.FragmentPrograms ) )
			{
				info.Text = "NOTE: Light colours and specular highlights are not supported by your card.";
			}
			pOver.Show();
		}

		protected override void OnFrameStarted( object source, FrameEventArgs e )
		{
			base.OnFrameStarted( source, e );

			timeDelay -= e.TimeSinceLastFrame;

			IfKeyPressed( KeyCodes.O, delegate()
			{
				entities[ currentEntity ].IsVisible = false;
				currentEntity = ( ++currentEntity ) % NUM_ENTITIES;
				entities[ currentEntity ].IsVisible = true;
				entities[ currentEntity ].MaterialName = materialNames[ currentEntity, currentMaterial ];

				objectInfo.Text = "Current: " + entityMeshes[ currentEntity ];
				materialInfo.Text = "Current: " + materialNames[ currentEntity, currentMaterial ];
			} );

			IfKeyPressed( KeyCodes.M, delegate()
			{
				currentMaterial = ( ++currentMaterial ) % NUM_MATERIALS;
				entities[ currentEntity ].MaterialName = materialNames[ currentEntity, currentMaterial ];

				materialInfo.Text = "Current: " + materialNames[ currentEntity, currentMaterial ];
			} );

			IfKeyPressed( KeyCodes.D1, delegate()
			{
				FlipLightState( 0 );
			} );

			IfKeyPressed( KeyCodes.D2, delegate()
			{
				FlipLightState( 1 );
			} );

			IfKeyPressed( KeyCodes.D3, delegate()
			{
				FlipLightState( 2 );
			} );

			// animate the lights
			for ( int i = 0; i < NUM_LIGHTS; i++ )
			{
				lightPivots[ i ].Rotate( Vector3.UnitZ, lightSpeeds[ i ] * e.TimeSinceLastFrame );
			}
		}

		/// <summary>
		///    Flips the light states for the light at the specified index.
		/// </summary>
		/// <param name="index"></param>
		void FlipLightState( int index )
		{
			lightState[ index ] = !lightState[ index ];
			lights[ index ].IsVisible = lightState[ index ];
			lightFlareSets[ index ].IsVisible = lightState[ index ];
		}
	}
}