using System.Collections.Generic;
using Axiom.Core;
using Axiom.Graphics;
using Axiom.Math;

namespace Axiom.Components.RTShaderSystem
{
	public class PerPixelLighting : SubRenderState
	{
		public class LightParams
		{
			public LightType Type;
			public UniformParameter Position;
			public UniformParameter Direction;
			public UniformParameter AttenuatParams;
			public UniformParameter SpotParams;
			public UniformParameter DiffuseColor;
			public UniformParameter SpecularColor;
		}

		public static string SGXType = "SGX_PerPixelLighting";
		private static string SGXLibPerPixelLighting = "SGXLib_PerPixelLighting";
		private static string SGXFuncTransformNormal = "SGX_TransformNormal";
		private static string SGXFuncTransformPosition = "SGX_TransformPosition";
		private static string SGXFuncLightDirectionalDiffuse = "SGX_Light_Directional_Diffuse";
		private static string SGXFuncLightDirectionalDiffuseSpecular = "SGX_Light_Directional_DiffuseSpecular";
		private static string SGXFuncLightPointDiffuse = "SGX_Light_Point_Diffuse";
		private static string SGXFuncLightPointDiffuseSpecular = "SGX_Light_Point_DiffuseSpecular";
		private static string SGXFuncLightSpotDiffuse = "SGX_Light_Spot_Diffuse";
		private static string SGXFuncLightSpotDiffuseSpecular = "SGX_Light_Spot_DiffuseSpecular";
		private TrackVertexColor trackVertexColorType;
		private bool specularEnable;
		private List<LightParams> lightParamsList;
		private UniformParameter worldViewMatrix, worldViewITMatrix;
		private Parameter vsInPosition;
		private Parameter vsOutViewPos;
		private Parameter psInViewPos;
		private Parameter vsInNormal;
		private Parameter vsOutNormal;
		private Parameter psInNormal;
		private Parameter psTempDiffuseColor;
		private Parameter psTempSpecularColor;
		private Parameter psDiffuse;
		private Parameter psSpecular;
		private Parameter psOutSpecular;
		private Parameter psOutDiffuse;
		private UniformParameter derivedSceneColor;
		private UniformParameter lightAmbientColor;
		private UniformParameter derivedAmbientLightColor;
		private UniformParameter surfaceAmbientColor;
		private UniformParameter surfaceDiffuseColor;
		private UniformParameter surfaceSpecularColor;
		private UniformParameter surfaceEmissiveColor;
		private UniformParameter surfaceShininess;
		private readonly Light blankLight;

		public PerPixelLighting()
		{
			this.trackVertexColorType = TrackVertexColor.None;
			this.specularEnable = false;
			this.blankLight = new Light();
			this.blankLight.Diffuse = ColorEx.Black;
			this.blankLight.Specular = ColorEx.Black;
			this.blankLight.SetAttenuation( 0, 1, 0, 0 );
		}

		public override void UpdateGpuProgramsParams( Graphics.IRenderable rend, Graphics.Pass pass,
		                                              Graphics.AutoParamDataSource source,
		                                              Core.Collections.LightList lightList )
		{
			if ( this.lightParamsList.Count == 0 )
			{
				return;
			}

			Matrix4 matView = source.ViewMatrix;
			LightType curLightType = LightType.Directional;
			int curSearchLightIndex = 0;

			//Update per light parameters.
			for ( int i = 0; i < this.lightParamsList.Count; i++ )
			{
				PerPixelLighting.LightParams curParams = this.lightParamsList[ i ];

				if ( curLightType != curParams.Type )
				{
					curLightType = curParams.Type;
					curSearchLightIndex = 0;
				}
				Light srcLight = null;
				Vector4 vParamter;
				ColorEx color;

				//Search a matching light from the current sorted light of the given renderble
				for ( int j = curSearchLightIndex; j < lightList.Count; j++ )
				{
					if ( lightList[ j ].Type == curLightType )
					{
						srcLight = lightList[ j ];
						curSearchLightIndex = j + 1;
						break;
					}
				}
				//no matching light found -> use a blnak dummy light for parameter update.
				if ( srcLight == null )
				{
					srcLight = this.blankLight;
				}

				switch ( curParams.Type )
				{
					case LightType.Directional:
						//Update light direction
						vParamter = matView.TransformAffine( srcLight.GetAs4DVector( true ) );
						curParams.Direction.SetGpuParameter( vParamter );
						break;
					case LightType.Point:
						//Update light position.
						vParamter = matView.TransformAffine( srcLight.GetAs4DVector( true ) );
						curParams.Position.SetGpuParameter( vParamter );

						//Update light attenuation paramters.
						vParamter.x = srcLight.AttenuationRange;
						vParamter.y = srcLight.AttenuationConstant;
						vParamter.z = srcLight.AttenuationLinear;
						vParamter.w = srcLight.AttenuationQuadratic;
						curParams.AttenuatParams.SetGpuParameter( vParamter );
						break;
					case LightType.Spotlight:
					{
						Vector3 vec3;
						Matrix3 matViewIT;

						//Update light position
						vParamter = matView.TransformAffine( srcLight.GetAs4DVector( true ) );
						curParams.Position.SetGpuParameter( vParamter );

						//Update light direction
						source.InverseTransposeViewMatrix.Extract3x3Matrix( out matViewIT );
						vec3 = matViewIT*srcLight.DerivedDirection;
						vec3.Normalize();

						vParamter.x = -vec3.x;
						vParamter.y = -vec3.y;
						vParamter.z = -vec3.z;
						vParamter.w = 0.0;
						curParams.Direction.SetGpuParameter( vParamter );

						//Update spotlight parameters
						Real phi = Axiom.Math.Utility.Cos( srcLight.SpotlightOuterAngle );
						Real theta = Axiom.Math.Utility.Cos( srcLight.SpotlightInnerAngle );

						vec3.x = theta;
						vec3.y = phi;
						vec3.z = srcLight.SpotlightFalloff;

						curParams.SpotParams.SetGpuParameter( vec3 );
					}
						break;
				}

				//update diffuse color
				if ( ( this.trackVertexColorType & TrackVertexColor.Diffuse ) == 0 )
				{
					color = srcLight.Diffuse*pass.Diffuse;
					curParams.DiffuseColor.SetGpuParameter( color );
				}
				else
				{
					color = srcLight.Diffuse;
					curParams.DiffuseColor.SetGpuParameter( color );
				}
			}
		}

		public override bool PreAddToRenderState( TargetRenderState targetRenderState, Graphics.Pass srcPass,
		                                          Graphics.Pass dstPass )
		{
			if ( srcPass.LightingEnabled == false )
			{
				return false;
			}

			var lightCount = new int[3];
			targetRenderState.GetLightCount( out lightCount );

			TrackVertexColorType = srcPass.VertexColorTracking;

			if ( srcPass.Shininess > 0.0 && srcPass.Specular != ColorEx.Black )
			{
				SpecularEnable = true;
			}
			else
			{
				SpecularEnable = false;
			}

			//Case this pass should run once per light(s) -> override the light policy.
			if ( srcPass.IteratePerLight )
			{
				//This is the preferred case -> only one type of light is handled
				if ( srcPass.RunOnlyOncePerLightType )
				{
					if ( srcPass.OnlyLightType == LightType.Point )
					{
						lightCount[ 0 ] = srcPass.LightsPerIteration;
						lightCount[ 1 ] = 0;
						lightCount[ 2 ] = 0;
					}
					else if ( srcPass.OnlyLightType == LightType.Directional )
					{
						lightCount[ 0 ] = 0;
						lightCount[ 1 ] = srcPass.LightsPerIteration;
						lightCount[ 2 ] = 0;
					}
					else if ( srcPass.OnlyLightType == LightType.Spotlight )
					{
						lightCount[ 0 ] = 0;
						lightCount[ 1 ] = 0;
						lightCount[ 2 ] = srcPass.LightsPerIteration;
					}
				}
				else
				{
					throw new AxiomException(
						"Using iterative lighting method with RT Shader System requires specifying explicit light type." );
				}
			}

			SetLightCount( lightCount );

			return true;
		}

		public override void GetLightCount( out int[] lightCount )
		{
			lightCount = new int[3]
			             {
			             	0, 0, 0
			             };

			for ( int i = 0; i < this.lightParamsList.Count; i++ )
			{
				LightParams curParams = this.lightParamsList[ i ];

				if ( curParams.Type == LightType.Point )
				{
					lightCount[ 0 ]++;
				}
				else if ( curParams.Type == LightType.Directional )
				{
					lightCount[ 1 ]++;
				}
				else if ( curParams.Type == LightType.Spotlight )
				{
					lightCount[ 2 ]++;
				}
			}
		}

		public override void SetLightCount( int[] currLightCount )
		{
			for ( int type = 0; type < 3; type++ )
			{
				for ( int i = 0; i < currLightCount[ type ]; i++ )
				{
					var curParams = new LightParams();

					if ( type == 0 )
					{
						curParams.Type = LightType.Point;
					}
					else if ( type == 1 )
					{
						curParams.Type = LightType.Directional;
					}
					else if ( type == 2 )
					{
						curParams.Type = LightType.Spotlight;
					}

					this.lightParamsList.Add( curParams );
				}
			}
		}

		protected override bool ResolveParameters( ProgramSet programSet )
		{
			if ( ResolveGlobalParameters( programSet ) == false )
			{
				return false;
			}
			if ( ResolveParameters( programSet ) == false )
			{
				return false;
			}

			return true;
		}

		protected bool ResolveGlobalParameters( ProgramSet programSet )
		{
			Program vsProgram = programSet.CpuVertexProgram;
			Program psProgram = programSet.CpuFragmentProgram;
			Function vsMain = vsProgram.EntryPointFunction;
			Function psMain = psProgram.EntryPointFunction;

			//Resolve world view IT matrix
			this.worldViewITMatrix =
				vsProgram.ResolveAutoParameterInt(
					GpuProgramParameters.AutoConstantType.InverseTransposeWorldViewMatrix, 0 );
			if ( this.worldViewITMatrix == null )
			{
				return false;
			}

			//Get surface ambient color if need to
			if ( ( this.trackVertexColorType & TrackVertexColor.Ambient ) == 0 )
			{
				this.derivedAmbientLightColor =
					psProgram.ResolveAutoParameterInt( GpuProgramParameters.AutoConstantType.DerivedAmbientLightColor, 0 );
				if ( this.derivedAmbientLightColor == null )
				{
					return false;
				}
			}
			else
			{
				this.lightAmbientColor =
					psProgram.ResolveAutoParameterInt( GpuProgramParameters.AutoConstantType.AmbientLightColor, 0 );
				if ( this.lightAmbientColor == null )
				{
					return false;
				}

				this.surfaceAmbientColor =
					psProgram.ResolveAutoParameterInt( GpuProgramParameters.AutoConstantType.SurfaceAmbientColor, 0 );
				if ( this.surfaceAmbientColor == null )
				{
					return false;
				}
			}

			//Get surface diffuse color if need to
			if ( ( this.trackVertexColorType & TrackVertexColor.Diffuse ) == 0 )
			{
				this.surfaceDiffuseColor =
					psProgram.ResolveAutoParameterInt( GpuProgramParameters.AutoConstantType.SurfaceDiffuseColor, 0 );
				if ( this.surfaceDiffuseColor == null )
				{
					return false;
				}
			}

			//Get surface emissive color if need to
			if ( ( this.trackVertexColorType & TrackVertexColor.Emissive ) == 0 )
			{
				this.surfaceEmissiveColor =
					psProgram.ResolveAutoParameterInt( GpuProgramParameters.AutoConstantType.SurfaceEmissiveColor, 0 );
				if ( this.surfaceEmissiveColor == null )
				{
					return false;
				}
			}

			//Get derived scene color
			this.derivedSceneColor =
				psProgram.ResolveAutoParameterInt( GpuProgramParameters.AutoConstantType.DerivedSceneColor, 0 );
			if ( this.derivedSceneColor == null )
			{
				return false;
			}

			//Get surface shininess
			this.surfaceShininess = psProgram.ResolveAutoParameterInt(
				GpuProgramParameters.AutoConstantType.SurfaceShininess, 0 );
			if ( this.surfaceShininess == null )
			{
				return false;
			}

			//Resolve input vertex shader normal
			this.vsInNormal = vsMain.ResolveInputParameter( Parameter.SemanticType.Normal, 0,
			                                                Parameter.ContentType.NormalObjectSpace,
			                                                GpuProgramParameters.GpuConstantType.Float3 );
			if ( this.vsInNormal == null )
			{
				return false;
			}

			this.vsOutNormal = vsMain.ResolveOutputParameter( Parameter.SemanticType.TextureCoordinates, -1,
			                                                  Parameter.ContentType.NormalViewSpace,
			                                                  GpuProgramParameters.GpuConstantType.Float3 );
			if ( this.vsOutNormal == null )
			{
				return false;
			}

			//Resolve input pixel shader normal.
			this.psInNormal = psMain.ResolveInputParameter( Parameter.SemanticType.TextureCoordinates, this.vsOutNormal.Index,
			                                                this.vsOutNormal.Content, GpuProgramParameters.GpuConstantType.Float3 );
			if ( this.psInNormal == null )
			{
				return false;
			}

			var inputParams = psMain.InputParameters;
			var localParams = psMain.LocalParameters;

			this.psDiffuse = Function.GetParameterByContent( inputParams, Parameter.ContentType.ColorDiffuse,
			                                                 GpuProgramParameters.GpuConstantType.Float4 );
			if ( this.psDiffuse == null )
			{
				this.psDiffuse = Function.GetParameterByContent( localParams, Parameter.ContentType.ColorDiffuse,
				                                                 GpuProgramParameters.GpuConstantType.Float4 );
				if ( this.psDiffuse == null )
				{
					return false;
				}
			}

			this.psOutDiffuse = psMain.ResolveOutputParameter( Parameter.SemanticType.Color, 0,
			                                                   Parameter.ContentType.ColorDiffuse,
			                                                   GpuProgramParameters.GpuConstantType.Float4 );
			if ( this.psOutDiffuse == null )
			{
				return false;
			}

			this.psTempDiffuseColor = psMain.ResolveLocalParameter( Parameter.SemanticType.Unknown, 0, "lPerPixelDiffuse",
			                                                        GpuProgramParameters.GpuConstantType.Float4 );
			if ( this.psTempDiffuseColor == null )
			{
				return false;
			}

			if ( this.specularEnable )
			{
				this.psSpecular = Function.GetParameterByContent( inputParams, Parameter.ContentType.ColorSpecular,
				                                                  GpuProgramParameters.GpuConstantType.Float4 );
				if ( this.psSpecular == null )
				{
					this.psSpecular = Function.GetParameterByContent( localParams, Parameter.ContentType.ColorSpecular,
					                                                  GpuProgramParameters.GpuConstantType.Float4 );
					if ( this.psSpecular == null )
					{
						return false;
					}
				}

				this.psTempSpecularColor = psMain.ResolveLocalParameter( Parameter.SemanticType.Unknown, 0,
				                                                         "lPerPixelSpecular",
				                                                         GpuProgramParameters.GpuConstantType.Float4 );
				if ( this.psTempSpecularColor == null )
				{
					return false;
				}

				this.vsInPosition = vsMain.ResolveInputParameter( Parameter.SemanticType.Position, 0,
				                                                  Parameter.ContentType.PositionObjectSpace,
				                                                  GpuProgramParameters.GpuConstantType.Float4 );
				if ( this.vsInPosition == null )
				{
					return false;
				}

				this.vsOutViewPos = vsMain.ResolveOutputParameter( Parameter.SemanticType.TextureCoordinates, -1,
				                                                   Parameter.ContentType.PositionViewSpace,
				                                                   GpuProgramParameters.GpuConstantType.Float3 );
				if ( this.vsOutViewPos == null )
				{
					return false;
				}

				this.psInViewPos = psMain.ResolveInputParameter( Parameter.SemanticType.TextureCoordinates,
				                                                 this.vsOutViewPos.Index, this.vsOutViewPos.Content,
				                                                 GpuProgramParameters.GpuConstantType.Float3 );
				if ( this.psInViewPos == null )
				{
					return false;
				}

				this.worldViewMatrix =
					vsProgram.ResolveAutoParameterInt( GpuProgramParameters.AutoConstantType.WorldViewMatrix, 0 );
				if ( this.worldViewMatrix == null )
				{
					return false;
				}
			}

			return true;
		}

		protected bool ResolvePerLightParameters( ProgramSet programSet )
		{
			Program vsProgram = programSet.CpuVertexProgram;
			Program psProgram = programSet.CpuFragmentProgram;
			Function vsMain = vsProgram.EntryPointFunction;
			Function psMain = psProgram.EntryPointFunction;

			//Resolve per light parameters
			for ( int i = 0; i < this.lightParamsList.Count; i++ )
			{
				switch ( this.lightParamsList[ i ].Type )
				{
					case LightType.Directional:
						this.lightParamsList[ i ].Direction =
							psProgram.ResolveParameter( GpuProgramParameters.GpuConstantType.Float4, -1,
							                            GpuProgramParameters.GpuParamVariability.Lights,
							                            "light_direction_view_space" );
						if ( this.lightParamsList[ i ].Direction == null )
						{
							return false;
						}
						break;
					case LightType.Point:
						this.worldViewMatrix =
							vsProgram.ResolveAutoParameterInt( GpuProgramParameters.AutoConstantType.WorldViewMatrix, 0 );
						if ( this.worldViewMatrix == null )
						{
							return false;
						}

						this.vsInPosition = vsMain.ResolveInputParameter( Parameter.SemanticType.Position, 0,
						                                                  Parameter.ContentType.PositionObjectSpace,
						                                                  GpuProgramParameters.GpuConstantType.Float4 );
						if ( this.vsInPosition == null )
						{
							return false;
						}

						this.lightParamsList[ i ].Position =
							psProgram.ResolveParameter( GpuProgramParameters.GpuConstantType.Float4, -1,
							                            GpuProgramParameters.GpuParamVariability.Lights,
							                            "light_position_view_space" );
						if ( this.lightParamsList[ i ].Position == null )
						{
							return false;
						}

						this.lightParamsList[ i ].AttenuatParams =
							psProgram.ResolveParameter( GpuProgramParameters.GpuConstantType.Float4, -1,
							                            GpuProgramParameters.GpuParamVariability.Lights,
							                            "light_attenuation" );
						if ( this.lightParamsList[ i ].AttenuatParams == null )
						{
							return false;
						}

						if ( this.vsOutViewPos == null )
						{
							this.vsOutViewPos = vsMain.ResolveOutputParameter( Parameter.SemanticType.TextureCoordinates, -1,
							                                                   Parameter.ContentType.PositionViewSpace,
							                                                   GpuProgramParameters.GpuConstantType.Float3 );
							if ( this.vsOutViewPos == null )
							{
								return false;
							}

							this.psInViewPos = psMain.ResolveInputParameter( Parameter.SemanticType.TextureCoordinates,
							                                                 this.vsOutViewPos.Index, this.vsOutViewPos.Content,
							                                                 GpuProgramParameters.GpuConstantType.Float3 );
							if ( this.psInViewPos == null )
							{
								return false;
							}
						}
						break;
					case LightType.Spotlight:
						this.worldViewMatrix =
							vsProgram.ResolveAutoParameterInt( GpuProgramParameters.AutoConstantType.WorldViewMatrix, 0 );
						if ( this.worldViewMatrix == null )
						{
							return false;
						}

						this.vsInPosition = vsMain.ResolveInputParameter( Parameter.SemanticType.Position, 0,
						                                                  Parameter.ContentType.PositionObjectSpace,
						                                                  GpuProgramParameters.GpuConstantType.Float4 );
						if ( this.vsInPosition == null )
						{
							return false;
						}

						this.lightParamsList[ i ].Position =
							psProgram.ResolveParameter( GpuProgramParameters.GpuConstantType.Float4, -1,
							                            GpuProgramParameters.GpuParamVariability.Lights,
							                            "light_position_view_space" );
						if ( this.lightParamsList[ i ].Position == null )
						{
							return false;
						}

						this.lightParamsList[ i ].Direction =
							psProgram.ResolveParameter( GpuProgramParameters.GpuConstantType.Float4, -1,
							                            GpuProgramParameters.GpuParamVariability.Lights,
							                            "light_direction_view_space" );
						if ( this.lightParamsList[ i ].Direction == null )
						{
							return false;
						}

						this.lightParamsList[ i ].AttenuatParams =
							psProgram.ResolveParameter( GpuProgramParameters.GpuConstantType.Float4, -1,
							                            GpuProgramParameters.GpuParamVariability.Lights,
							                            "light_attenuation" );
						if ( this.lightParamsList[ i ].AttenuatParams == null )
						{
							return false;
						}

						this.lightParamsList[ i ].SpotParams =
							psProgram.ResolveParameter( GpuProgramParameters.GpuConstantType.Float3, -1,
							                            GpuProgramParameters.GpuParamVariability.Lights,
							                            "spotlight_params" );
						if ( this.lightParamsList[ i ].SpotParams == null )
						{
							return false;
						}

						if ( this.vsOutViewPos == null )
						{
							this.vsOutViewPos = vsMain.ResolveOutputParameter( Parameter.SemanticType.TextureCoordinates, -1,
							                                                   Parameter.ContentType.PositionViewSpace,
							                                                   GpuProgramParameters.GpuConstantType.Float3 );
							if ( this.vsOutViewPos == null )
							{
								return false;
							}

							this.psInViewPos = psMain.ResolveInputParameter( Parameter.SemanticType.TextureCoordinates,
							                                                 this.vsOutViewPos.Index,
							                                                 this.vsOutViewPos.Content,
							                                                 GpuProgramParameters.GpuConstantType.Float3 );

							if ( this.psInViewPos == null )
							{
								return false;
							}
						}
						break;
				}

				//Resolve diffuse color
				if ( ( this.trackVertexColorType & TrackVertexColor.Diffuse ) == 0 )
				{
					this.lightParamsList[ i ].DiffuseColor =
						psProgram.ResolveParameter( GpuProgramParameters.GpuConstantType.Float4, -1,
						                            GpuProgramParameters.GpuParamVariability.Lights |
						                            GpuProgramParameters.GpuParamVariability.Global,
						                            "derived_light_diffuse" );
					if ( this.lightParamsList[ i ].DiffuseColor == null )
					{
						return false;
					}
				}
				else
				{
					this.lightParamsList[ i ].DiffuseColor =
						psProgram.ResolveParameter( GpuProgramParameters.GpuConstantType.Float4, -1,
						                            GpuProgramParameters.GpuParamVariability.Lights, "light_diffuse" );
					if ( this.lightParamsList[ i ].DiffuseColor == null )
					{
						return false;
					}
				}

				if ( this.specularEnable )
				{
					//Resolve specular color
					if ( ( this.trackVertexColorType & TrackVertexColor.Specular ) == 0 )
					{
						this.lightParamsList[ i ].SpecularColor =
							psProgram.ResolveParameter( GpuProgramParameters.GpuConstantType.Float4, -1,
							                            GpuProgramParameters.GpuParamVariability.Lights |
							                            GpuProgramParameters.GpuParamVariability.Global,
							                            "derived_light_specular" );
						if ( this.lightParamsList[ i ].SpecularColor == null )
						{
							return false;
						}
					}
					else
					{
						this.lightParamsList[ i ].SpecularColor =
							psProgram.ResolveParameter( GpuProgramParameters.GpuConstantType.Float4, -1,
							                            GpuProgramParameters.GpuParamVariability.Lights,
							                            "light_specular" );
						if ( this.lightParamsList[ i ].SpecularColor == null )
						{
							return false;
						}
					}
				}
			}
			return true;
		}

		protected override bool ResolveDependencies( ProgramSet programSet )
		{
			Program vsProgram = programSet.CpuVertexProgram;
			Program psProgram = programSet.CpuFragmentProgram;

			vsProgram.AddDependency( FFPRenderState.FFPLibCommon );
			vsProgram.AddDependency( SGXLibPerPixelLighting );

			psProgram.AddDependency( FFPRenderState.FFPLibCommon );
			psProgram.AddDependency( SGXLibPerPixelLighting );

			return true;
		}

		protected override bool AddFunctionInvocations( ProgramSet programSet )
		{
			Program vsProgram = programSet.CpuVertexProgram;
			Program psProgram = programSet.CpuFragmentProgram;
			Function vsMain = vsProgram.EntryPointFunction;
			Function psMain = psProgram.EntryPointFunction;

			int internalCounter = 0;
			//Add the global illumination functions.
			if ( AddVSInvocation( vsMain, (int)FFPRenderState.FFPVertexShaderStage.VSLighting, ref internalCounter ) ==
			     false )
			{
				return false;
			}

			internalCounter = 0;

			//Add the global illumination fuctnions
			if (
				AddPSGlobalIlluminationInvocation( psMain, (int)FFPRenderState.FFPFragmentShaderStage.PSColorBegin + 1,
				                                   ref internalCounter ) == false )
			{
				return false;
			}

			//Add per light functions
			for ( int i = 0; i < this.lightParamsList.Count; i++ )
			{
				if ( AddPSIlluminationInvocation( this.lightParamsList[ i ], psMain, -1, ref internalCounter ) == false )
				{
					return false;
				}
			}

			//Assign back temporary variables to the ps diffuse and specular components.
			if (
				AddPSFinalAssignmentInvocation( psMain, (int)FFPRenderState.FFPFragmentShaderStage.PSColorBegin + 1,
				                                ref internalCounter ) == false )
			{
				return false;
			}

			return true;
		}

		private bool AddPSGlobalIlluminationInvocation( Function psMain, int groupOrder, ref int internalCounter )
		{
			FunctionInvocation curFuncInvocation = null;
			if ( ( this.trackVertexColorType & TrackVertexColor.Ambient ) == 0 &&
			     ( this.trackVertexColorType & TrackVertexColor.Emissive ) == 0 )
			{
				curFuncInvocation = new FunctionInvocation( FFPRenderState.FFPFuncAssign, groupOrder, internalCounter++ );
				curFuncInvocation.PushOperand( this.derivedSceneColor, Operand.OpSemantic.In );
				curFuncInvocation.PushOperand( this.psTempDiffuseColor, Operand.OpSemantic.Out );
				psMain.AddAtomInstance( curFuncInvocation );
			}
			else
			{
				if ( ( this.trackVertexColorType & TrackVertexColor.Ambient ) != 0 )
				{
					curFuncInvocation = new FunctionInvocation( FFPRenderState.FFPFuncModulate, groupOrder,
					                                            internalCounter++ );
					curFuncInvocation.PushOperand( this.lightAmbientColor, Operand.OpSemantic.In );
					curFuncInvocation.PushOperand( this.psDiffuse, Operand.OpSemantic.In );
					curFuncInvocation.PushOperand( this.psTempDiffuseColor, Operand.OpSemantic.Out );
					psMain.AddAtomInstance( curFuncInvocation );
				}
				else
				{
					curFuncInvocation = new FunctionInvocation( FFPRenderState.FFPFuncAssign, groupOrder,
					                                            internalCounter++ );
					curFuncInvocation.PushOperand( this.derivedAmbientLightColor, Operand.OpSemantic.In,
					                               (int)( Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z ) );
					curFuncInvocation.PushOperand( this.psTempDiffuseColor, Operand.OpSemantic.Out,
					                               (int)( Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z ) );
					psMain.AddAtomInstance( curFuncInvocation );
				}

				if ( ( this.trackVertexColorType & TrackVertexColor.Emissive ) != 0 )
				{
					curFuncInvocation = new FunctionInvocation( FFPRenderState.FFPFuncAdd, groupOrder, internalCounter++ );
					curFuncInvocation.PushOperand( this.psDiffuse, Operand.OpSemantic.In );
					curFuncInvocation.PushOperand( this.psTempDiffuseColor, Operand.OpSemantic.In );
					curFuncInvocation.PushOperand( this.psTempDiffuseColor, Operand.OpSemantic.Out );
					psMain.AddAtomInstance( curFuncInvocation );
				}
				else
				{
					curFuncInvocation = new FunctionInvocation( FFPRenderState.FFPFuncAdd, groupOrder, internalCounter++ );
					curFuncInvocation.PushOperand( this.surfaceEmissiveColor, Operand.OpSemantic.In );
					curFuncInvocation.PushOperand( this.psTempDiffuseColor, Operand.OpSemantic.In );
					curFuncInvocation.PushOperand( this.psTempDiffuseColor, Operand.OpSemantic.Out );
					psMain.AddAtomInstance( curFuncInvocation );
				}
			}

			if ( this.specularEnable )
			{
				curFuncInvocation = new FunctionInvocation( FFPRenderState.FFPFuncAssign, groupOrder, internalCounter++ );
				curFuncInvocation.PushOperand( this.psSpecular, Operand.OpSemantic.In );
				curFuncInvocation.PushOperand( this.psTempSpecularColor, Operand.OpSemantic.Out );
				psMain.AddAtomInstance( curFuncInvocation );
			}

			return true;
		}

		protected bool AddVSInvocation( Function vsMain, int groupOrder, ref int internalCounter )
		{
			FunctionInvocation curFuncInvocation = null;

			//transform normal in view space
			curFuncInvocation = new FunctionInvocation( SGXFuncTransformNormal, groupOrder, internalCounter++ );
			curFuncInvocation.PushOperand( this.worldViewITMatrix, Operand.OpSemantic.In );
			curFuncInvocation.PushOperand( this.vsInNormal, Operand.OpSemantic.In );
			curFuncInvocation.PushOperand( this.vsOutNormal, Operand.OpSemantic.Out );
			vsMain.AddAtomInstance( curFuncInvocation );

			//Transform view space position if need to
			if ( this.vsOutViewPos != null )
			{
				curFuncInvocation = new FunctionInvocation( SGXFuncTransformPosition, groupOrder, internalCounter++ );
				curFuncInvocation.PushOperand( this.worldViewMatrix, Operand.OpSemantic.In );
				curFuncInvocation.PushOperand( this.vsInPosition, Operand.OpSemantic.In );
				curFuncInvocation.PushOperand( this.vsOutViewPos, Operand.OpSemantic.Out );
				vsMain.AddAtomInstance( curFuncInvocation );
			}

			return true;
		}

		internal bool AddPSIlluminationInvocation( LightParams curLightParams, Function psMain, int groupOrder,
		                                           ref int internalCounter )
		{
			FunctionInvocation curFuncInvocation = null;

			//merge diffuse color with vertex color if need to
			if ( ( this.trackVertexColorType & TrackVertexColor.Diffuse ) != 0 )
			{
				curFuncInvocation = new FunctionInvocation( FFPRenderState.FFPFuncModulate, groupOrder,
				                                            internalCounter++ );
				curFuncInvocation.PushOperand( this.psDiffuse, Operand.OpSemantic.In,
				                               (int)( Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z ) );
				curFuncInvocation.PushOperand( curLightParams.DiffuseColor, Operand.OpSemantic.In,
				                               (int)( Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z ) );
				curFuncInvocation.PushOperand( curLightParams.DiffuseColor, Operand.OpSemantic.Out,
				                               (int)( Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z ) );
				psMain.AddAtomInstance( curFuncInvocation );
			}
			//merge specular color with vertex color if need to
			if ( this.specularEnable && ( this.trackVertexColorType & TrackVertexColor.Specular ) != 0 )
			{
				curFuncInvocation = new FunctionInvocation( FFPRenderState.FFPFuncModulate, groupOrder,
				                                            internalCounter++ );
				curFuncInvocation.PushOperand( this.psDiffuse, Operand.OpSemantic.In,
				                               (int)( Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z ) );
				curFuncInvocation.PushOperand( curLightParams.SpecularColor, Operand.OpSemantic.In,
				                               (int)( Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z ) );
				curFuncInvocation.PushOperand( curLightParams.SpecularColor, Operand.OpSemantic.Out,
				                               (int)( Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z ) );
				psMain.AddAtomInstance( curFuncInvocation );
			}

			switch ( curLightParams.Type )
			{
				case LightType.Directional:
					if ( this.specularEnable )
					{
						curFuncInvocation = new FunctionInvocation( SGXFuncLightDirectionalDiffuseSpecular, groupOrder,
						                                            internalCounter++ );
						curFuncInvocation.PushOperand( this.psInNormal, Operand.OpSemantic.In );
						curFuncInvocation.PushOperand( this.psInViewPos, Operand.OpSemantic.In );
						curFuncInvocation.PushOperand( curLightParams.Direction, Operand.OpSemantic.In,
						                               (int)( Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z ) );
						curFuncInvocation.PushOperand( curLightParams.DiffuseColor, Operand.OpSemantic.In,
						                               (int)( Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z ) );
						curFuncInvocation.PushOperand( curLightParams.SpecularColor, Operand.OpSemantic.In,
						                               (int)( Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z ) );
						curFuncInvocation.PushOperand( this.surfaceShininess, Operand.OpSemantic.In );
						curFuncInvocation.PushOperand( this.psTempDiffuseColor, Operand.OpSemantic.In,
						                               (int)( Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z ) );
						curFuncInvocation.PushOperand( this.psTempSpecularColor, Operand.OpSemantic.In,
						                               (int)( Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z ) );
						curFuncInvocation.PushOperand( this.psTempDiffuseColor, Operand.OpSemantic.Out,
						                               (int)( Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z ) );
						curFuncInvocation.PushOperand( this.psTempSpecularColor, Operand.OpSemantic.Out,
						                               (int)( Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z ) );
						psMain.AddAtomInstance( curFuncInvocation );
					}

					else
					{
						curFuncInvocation = new FunctionInvocation( SGXFuncLightDirectionalDiffuse, groupOrder,
						                                            internalCounter++ );
						curFuncInvocation.PushOperand( this.psInNormal, Operand.OpSemantic.In );
						curFuncInvocation.PushOperand( curLightParams.Direction, Operand.OpSemantic.In,
						                               (int)( Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z ) );
						curFuncInvocation.PushOperand( curLightParams.DiffuseColor, Operand.OpSemantic.In,
						                               (int)( Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z ) );
						curFuncInvocation.PushOperand( this.psTempDiffuseColor, Operand.OpSemantic.In,
						                               (int)( Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z ) );
						curFuncInvocation.PushOperand( this.psTempDiffuseColor, Operand.OpSemantic.Out,
						                               (int)( Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z ) );
						psMain.AddAtomInstance( curFuncInvocation );
					}
					break;
				case LightType.Point:
					if ( this.specularEnable )
					{
						curFuncInvocation = new FunctionInvocation( SGXFuncLightPointDiffuseSpecular, groupOrder,
						                                            internalCounter++ );
						curFuncInvocation.PushOperand( this.psInNormal, Operand.OpSemantic.In );
						curFuncInvocation.PushOperand( this.psInViewPos, Operand.OpSemantic.In );
						curFuncInvocation.PushOperand( curLightParams.Position, Operand.OpSemantic.In,
						                               (int)( Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z ) );
						curFuncInvocation.PushOperand( curLightParams.AttenuatParams, Operand.OpSemantic.In );
						curFuncInvocation.PushOperand( curLightParams.DiffuseColor, Operand.OpSemantic.In,
						                               (int)( Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z ) );
						curFuncInvocation.PushOperand( curLightParams.SpecularColor, Operand.OpSemantic.In,
						                               (int)( Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z ) );
						curFuncInvocation.PushOperand( this.surfaceShininess, Operand.OpSemantic.In,
						                               (int)( Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z ) );
						curFuncInvocation.PushOperand( this.psTempDiffuseColor, Operand.OpSemantic.In,
						                               (int)( Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z ) );
						curFuncInvocation.PushOperand( this.psTempSpecularColor, Operand.OpSemantic.In,
						                               (int)( Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z ) );
						curFuncInvocation.PushOperand( this.psTempDiffuseColor, Operand.OpSemantic.Out,
						                               (int)( Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z ) );
						curFuncInvocation.PushOperand( this.psTempSpecularColor, Operand.OpSemantic.Out,
						                               (int)( Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z ) );
						psMain.AddAtomInstance( curFuncInvocation );
					}
					else
					{
						curFuncInvocation = new FunctionInvocation( SGXFuncLightPointDiffuse, groupOrder,
						                                            internalCounter++ );
						curFuncInvocation.PushOperand( this.psInNormal, Operand.OpSemantic.In );
						curFuncInvocation.PushOperand( this.psInViewPos, Operand.OpSemantic.In );
						curFuncInvocation.PushOperand( curLightParams.Position, Operand.OpSemantic.In,
						                               (int)( Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z ) );
						curFuncInvocation.PushOperand( curLightParams.AttenuatParams, Operand.OpSemantic.In );
						curFuncInvocation.PushOperand( curLightParams.DiffuseColor, Operand.OpSemantic.In,
						                               (int)( Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z ) );
						curFuncInvocation.PushOperand( this.psTempDiffuseColor, Operand.OpSemantic.In,
						                               (int)( Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z ) );
						curFuncInvocation.PushOperand( this.psTempDiffuseColor, Operand.OpSemantic.Out,
						                               (int)( Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z ) );
						psMain.AddAtomInstance( curFuncInvocation );
					}
					break;
				case LightType.Spotlight:
					if ( this.specularEnable )
					{
						curFuncInvocation = new FunctionInvocation( SGXFuncLightSpotDiffuseSpecular, groupOrder,
						                                            internalCounter++ );
						curFuncInvocation.PushOperand( this.psInNormal, Operand.OpSemantic.In );
						curFuncInvocation.PushOperand( this.psInViewPos, Operand.OpSemantic.In );
						curFuncInvocation.PushOperand( curLightParams.Position, Operand.OpSemantic.In,
						                               (int)( Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z ) );
						curFuncInvocation.PushOperand( curLightParams.Direction, Operand.OpSemantic.In,
						                               (int)( Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z ) );
						curFuncInvocation.PushOperand( curLightParams.AttenuatParams, Operand.OpSemantic.In );
						curFuncInvocation.PushOperand( curLightParams.SpotParams, Operand.OpSemantic.In );
						curFuncInvocation.PushOperand( curLightParams.DiffuseColor, Operand.OpSemantic.In,
						                               (int)( Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z ) );
						curFuncInvocation.PushOperand( curLightParams.SpecularColor, Operand.OpSemantic.In,
						                               (int)( Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z ) );
						curFuncInvocation.PushOperand( this.surfaceShininess, Operand.OpSemantic.In );
						curFuncInvocation.PushOperand( this.psTempDiffuseColor, Operand.OpSemantic.In,
						                               (int)( Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z ) );
						curFuncInvocation.PushOperand( this.psTempSpecularColor, Operand.OpSemantic.In,
						                               (int)( Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z ) );
						curFuncInvocation.PushOperand( this.psTempDiffuseColor, Operand.OpSemantic.Out,
						                               (int)( Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z ) );
						curFuncInvocation.PushOperand( this.psTempSpecularColor, Operand.OpSemantic.Out,
						                               (int)( Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z ) );
					}
					else
					{
						curFuncInvocation = new FunctionInvocation( SGXFuncLightSpotDiffuse, groupOrder,
						                                            internalCounter++ );
						curFuncInvocation.PushOperand( this.psInNormal, Operand.OpSemantic.In );
						curFuncInvocation.PushOperand( this.psInViewPos, Operand.OpSemantic.In );
						curFuncInvocation.PushOperand( curLightParams.Position, Operand.OpSemantic.In,
						                               (int)( Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z ) );
						curFuncInvocation.PushOperand( curLightParams.Direction, Operand.OpSemantic.In,
						                               (int)( Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z ) );
						curFuncInvocation.PushOperand( curLightParams.AttenuatParams, Operand.OpSemantic.In );
						curFuncInvocation.PushOperand( curLightParams.SpotParams, Operand.OpSemantic.In );
						curFuncInvocation.PushOperand( curLightParams.DiffuseColor, Operand.OpSemantic.In,
						                               (int)( Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z ) );
						curFuncInvocation.PushOperand( this.psTempDiffuseColor, Operand.OpSemantic.In,
						                               (int)( Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z ) );
						curFuncInvocation.PushOperand( this.psTempDiffuseColor, Operand.OpSemantic.Out,
						                               (int)( Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z ) );
						psMain.AddAtomInstance( curFuncInvocation );
					}
					break;
			}

			return true;
		}

		protected bool AddPSFinalAssignmentInvocation( Function psMain, int groupOrder, ref int internalCounter )
		{
			var curFuncInvocation = new FunctionInvocation( FFPRenderState.FFPFuncAssign,
			                                                (int)FFPRenderState.FFPFragmentShaderStage.PSColorBegin + 1,
			                                                internalCounter++ );
			curFuncInvocation.PushOperand( this.psTempDiffuseColor, Operand.OpSemantic.In );
			curFuncInvocation.PushOperand( this.psDiffuse, Operand.OpSemantic.Out );
			psMain.AddAtomInstance( curFuncInvocation );

			curFuncInvocation = new FunctionInvocation( FFPRenderState.FFPFuncAssign,
			                                            (int)FFPRenderState.FFPFragmentShaderStage.PSColorBegin + 1,
			                                            internalCounter++ );
			curFuncInvocation.PushOperand( this.psDiffuse, Operand.OpSemantic.In );
			curFuncInvocation.PushOperand( this.psOutDiffuse, Operand.OpSemantic.Out );
			psMain.AddAtomInstance( curFuncInvocation );

			if ( this.specularEnable )
			{
				curFuncInvocation = new FunctionInvocation( FFPRenderState.FFPFuncAssign,
				                                            (int)FFPRenderState.FFPFragmentShaderStage.PSColorBegin + 1,
				                                            internalCounter++ );
				curFuncInvocation.PushOperand( this.psTempSpecularColor, Operand.OpSemantic.In );
				curFuncInvocation.PushOperand( this.psSpecular, Operand.OpSemantic.Out );
				psMain.AddAtomInstance( curFuncInvocation );
			}
			return true;
		}

		private TrackVertexColor TrackVertexColorType
		{
			get
			{
				return this.trackVertexColorType;
			}
			set
			{
				this.trackVertexColorType = value;
			}
		}

		public bool SpecularEnable
		{
			get
			{
				return this.specularEnable;
			}
			set
			{
				this.specularEnable = value;
			}
		}

		public override string Type
		{
			get
			{
				return PerPixelLighting.SGXType;
			}
		}

		public override int ExecutionOrder
		{
			get
			{
				return (int)FFPRenderState.FFPShaderStage.Lighting;
			}
		}
	}
}