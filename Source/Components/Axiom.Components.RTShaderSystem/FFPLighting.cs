﻿using System.Collections.Generic;
using Axiom.Core;
using Axiom.Graphics;
using Axiom.Math;

namespace Axiom.Components.RTShaderSystem
{
	public class FFPLighting : SubRenderState
	{
		private class LightParams
		{
			public LightType Type;
			public UniformParameter Position;
			public UniformParameter Direction;
			public UniformParameter AttenuatParams;
			public UniformParameter SpotParams;
			public UniformParameter DiffuseColor;
			public UniformParameter SpecularColor;
		}

		public static string FFPType = "FFP_Lighting";
		private TrackVertexColor trackVertexColorType;
		private bool specularEnable;
		private List<LightParams> lightParamsList;
		private UniformParameter worldViewMatrix, worldViewITMatrix;
		private Parameter vsInPosition, vsInNormal, vsDiffuse;
		private Parameter vsOutDiffuse, vsOutSpecular;
		private UniformParameter derivedSceneColor, lightAmbientColor, derivedAmbientLightColor;

		private UniformParameter surfaceAmbientColor,
		                         surfaceDiffuseColor,
		                         surfaceSpecularColor,
		                         surfaceEmissiveCoilor;

		private UniformParameter surfaceShininess;
		private readonly Light blankLight;

		public FFPLighting()
		{
			trackVertexColorType = TrackVertexColor.None;
			specularEnable = false;

			blankLight = new Light();
			blankLight.Diffuse = ColorEx.Black;
			blankLight.Specular = ColorEx.Black;
			blankLight.SetAttenuation( 0, 1, 0, 0 );
		}

		public override void UpdateGpuProgramsParams( Graphics.IRenderable rend, Graphics.Pass pass,
		                                              Graphics.AutoParamDataSource source,
		                                              Core.Collections.LightList lightList )
		{
			if ( lightParamsList.Count == 0 )
			{
				return;
			}

			Matrix4 matView = source.ViewMatrix;
			LightType curLightType = LightType.Directional;
			int curSearchLightIndex = 0;

			//Update per light parameters
			for ( int i = 0; i < lightParamsList.Count; i++ )
			{
				LightParams curParams = lightParamsList[ i ];

				if ( curLightType != curParams.Type )
				{
					curLightType = curParams.Type;
					curSearchLightIndex = 0;
				}

				Light srcLight = null;
				Vector4 vParameter;
				ColorEx color;

				//Search a matching light from the current sorted lights of the given renderable
				for ( int j = curSearchLightIndex; j < lightList.Count; j++ )
				{
					if ( lightList[ j ].Type == curLightType )
					{
						srcLight = lightList[ j ];
						curSearchLightIndex = j + 1;
						break;
					}
				}

				//No matching light found -> use a blank dummy light for parameter update
				if ( srcLight == null )
				{
					srcLight = blankLight;
				}

				switch ( curParams.Type )
				{
					case LightType.Directional:
						//Update light direction
						vParameter = matView.TransformAffine( srcLight.GetAs4DVector( true ) );
						curParams.Direction.SetGpuParameter( vParameter );
						break;
					case LightType.Point:
						//Update light position
						vParameter = matView.TransformAffine( srcLight.GetAs4DVector( true ) );
						curParams.Position.SetGpuParameter( vParameter );

						//Update light attenuation paramters
						vParameter.x = srcLight.AttenuationRange;
						vParameter.y = srcLight.AttenuationConstant;
						vParameter.z = srcLight.AttenuationLinear;
						vParameter.w = srcLight.AttenuationQuadratic;
						curParams.AttenuatParams.SetGpuParameter( vParameter );
						break;
					case LightType.Spotlight:
					{
						Vector3 vec3;
						Matrix3 matViewIT;

						source.InverseTransposeViewMatrix.Extract3x3Matrix( out matViewIT );

						//Update light position
						vParameter = matView.TransformAffine( srcLight.GetAs4DVector( true ) );
						curParams.Position.SetGpuParameter( vParameter );

						vec3 = matViewIT*srcLight.DerivedDirection;
						vec3.Normalize();

						vParameter.x = -vec3.x;
						vParameter.y = -vec3.y;
						vParameter.z = -vec3.z;
						vParameter.w = 0.0f;

						curParams.Direction.SetGpuParameter( vParameter );

						//Update light attenuation parameters
						vParameter.x = srcLight.AttenuationRange;
						vParameter.y = srcLight.AttenuationConstant;
						vParameter.z = srcLight.AttenuationLinear;
						vParameter.w = srcLight.AttenuationQuadratic;
						curParams.AttenuatParams.SetGpuParameter( vParameter );

						//Update spotlight parameters
						Real phi = Axiom.Math.Utility.Cos( srcLight.SpotlightOuterAngle*0.5 );
						Real theta = Axiom.Math.Utility.Cos( srcLight.SpotlightInnerAngle*0.5 );

						vec3.x = theta;
						vec3.y = phi;
						vec3.z = srcLight.SpotlightFalloff;

						curParams.SpotParams.SetGpuParameter( vec3 );
					}
						break;
				}

				//Update diffuse color
				if ( ( trackVertexColorType & TrackVertexColor.Diffuse ) == 0 )
				{
					color = srcLight.Diffuse*pass.Diffuse;
					curParams.DiffuseColor.SetGpuParameter( color );
				}
				else
				{
					color = srcLight.Diffuse;
					curParams.DiffuseColor.SetGpuParameter( color );
				}

				//Update specular color if need to
				if ( specularEnable )
				{
					//Update diffuse color
					if ( ( trackVertexColorType & TrackVertexColor.Specular ) == 0 )
					{
						color = srcLight.Specular*pass.Specular;
						curParams.SpecularColor.SetGpuParameter( color );
					}
					else
					{
						color = srcLight.Specular;
						curParams.SpecularColor.SetGpuParameter( color );
					}
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

			if ( srcPass.Shininess > 0 && srcPass.Specular != ColorEx.Black )
			{
				SpecularEnabled = true;
			}
			else
			{
				SpecularEnabled = false;
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
						"Using iterative lighting method with RT Shader System requires specifying explicit light type" );
				}
			}

			SetLightCount( lightCount );

			return true;
		}

		public override void SetLightCount( int[] currLightCount )
		{
			for ( int type = 0; type < 3; type++ )
			{
				for ( int i = 0; i < currLightCount[ type ]; i++ )
				{
					var curParam = new LightParams();
					if ( type == 0 )
					{
						curParam.Type = LightType.Point;
					}
					else if ( type == 1 )
					{
						curParam.Type = LightType.Directional;
					}
					else if ( type == 2 )
					{
						curParam.Type = LightType.Spotlight;
					}

					lightParamsList.Add( curParam );
				}
			}
		}

		public override void GetLightCount( out int[] lightCount )
		{
			lightCount = new int[3]
			             {
			             	0, 0, 0
			             };

			for ( int i = 0; i < lightParamsList.Count; i++ )
			{
				LightParams curParams = lightParamsList[ i ];

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

		protected override bool ResolveParameters( ProgramSet programSet )
		{
			Program vsProgram = programSet.CpuVertexProgram;
			Function vsMain = vsProgram.EntryPointFunction;

			//REsolve world view IT matrix
			worldViewITMatrix =
				vsProgram.ResolveAutoParameterInt(
					GpuProgramParameters.AutoConstantType.InverseTransposeWorldViewMatrix, 0 );
			if ( worldViewITMatrix == null )
			{
				return false;
			}

			//Get surface ambient color if need to
			if ( ( trackVertexColorType & TrackVertexColor.Ambient ) == 0 )
			{
				derivedAmbientLightColor =
					vsProgram.ResolveAutoParameterInt( GpuProgramParameters.AutoConstantType.DerivedAmbientLightColor, 0 );
				if ( derivedAmbientLightColor == null )
				{
					return false;
				}
			}
			else
			{
				lightAmbientColor =
					vsProgram.ResolveAutoParameterInt( GpuProgramParameters.AutoConstantType.AmbientLightColor, 0 );
				if ( lightAmbientColor == null )
				{
					return false;
				}

				surfaceAmbientColor =
					vsProgram.ResolveAutoParameterInt( GpuProgramParameters.AutoConstantType.SurfaceAmbientColor, 0 );
				if ( surfaceAmbientColor == null )
				{
					return false;
				}
			}

			//Get surface diffuse color if need to.
			if ( ( trackVertexColorType & TrackVertexColor.Diffuse ) == 0 )
			{
				surfaceDiffuseColor =
					vsProgram.ResolveAutoParameterInt( GpuProgramParameters.AutoConstantType.SurfaceDiffuseColor, 0 );
				if ( surfaceDiffuseColor == null )
				{
					return false;
				}
			}

			//Get surface specular color if need to
			if ( ( trackVertexColorType & TrackVertexColor.Specular ) == 0 )
			{
				surfaceSpecularColor =
					vsProgram.ResolveAutoParameterInt( GpuProgramParameters.AutoConstantType.SurfaceSpecularColor, 0 );
				if ( surfaceSpecularColor == null )
				{
					return false;
				}
			}

			//Get surface emissive color if need to.
			if ( ( trackVertexColorType & TrackVertexColor.Emissive ) == 0 )
			{
				surfaceEmissiveCoilor =
					vsProgram.ResolveAutoParameterInt( GpuProgramParameters.AutoConstantType.SurfaceEmissiveColor, 0 );
				if ( surfaceEmissiveCoilor == null )
				{
					return false;
				}
			}

			//Get derived scene color
			derivedSceneColor =
				vsProgram.ResolveAutoParameterInt( GpuProgramParameters.AutoConstantType.DerivedSceneColor, 0 );
			if ( derivedSceneColor == null )
			{
				return false;
			}

			//get surface shininess
			surfaceShininess = vsProgram.ResolveAutoParameterInt(
				GpuProgramParameters.AutoConstantType.SurfaceShininess, 0 );
			if ( surfaceShininess == null )
			{
				return false;
			}

			//Resolve input vertex shader normal
			vsInNormal = vsMain.ResolveInputParameter( Parameter.SemanticType.Normal, 0,
			                                           Parameter.ContentType.NormalObjectSpace,
			                                           GpuProgramParameters.GpuConstantType.Float3 );
			if ( vsInNormal == null )
			{
				return false;
			}

			if ( trackVertexColorType != 0 )
			{
				vsDiffuse = vsMain.ResolveInputParameter( Parameter.SemanticType.Color, 0,
				                                          Parameter.ContentType.ColorDiffuse,
				                                          GpuProgramParameters.GpuConstantType.Float4 );
				if ( vsDiffuse == null )
				{
					return false;
				}
			}

			//Resolve output vertex shader diffuse color.
			vsOutDiffuse = vsMain.ResolveOutputParameter( Parameter.SemanticType.Color, 0,
			                                              Parameter.ContentType.ColorDiffuse,
			                                              GpuProgramParameters.GpuConstantType.Float4 );
			if ( vsOutDiffuse == null )
			{
				return false;
			}

			//Resolve per light parameters
			for ( int i = 0; i < lightParamsList.Count; i++ )
			{
				switch ( lightParamsList[ i ].Type )
				{
					case LightType.Directional:
						lightParamsList[ i ].Direction =
							vsProgram.ResolveParameter( GpuProgramParameters.GpuConstantType.Float4, -1,
							                            GpuProgramParameters.GpuParamVariability.Lights,
							                            "light_position_view_space" );
						if ( lightParamsList[ i ].Direction == null )
						{
							return false;
						}
						break;
					case LightType.Point:
						worldViewMatrix =
							vsProgram.ResolveAutoParameterInt( GpuProgramParameters.AutoConstantType.WorldViewMatrix, 0 );
						if ( worldViewMatrix == null )
						{
							return false;
						}

						vsInPosition = vsMain.ResolveInputParameter( Parameter.SemanticType.Position, 0,
						                                             Parameter.ContentType.PositionObjectSpace,
						                                             GpuProgramParameters.GpuConstantType.Float4 );
						if ( vsInPosition == null )
						{
							return false;
						}

						lightParamsList[ i ].Position =
							vsProgram.ResolveParameter( GpuProgramParameters.GpuConstantType.Float4, -1,
							                            GpuProgramParameters.GpuParamVariability.Lights,
							                            "light_position_view_space" );
						if ( lightParamsList[ i ].Position == null )
						{
							return false;
						}

						lightParamsList[ i ].AttenuatParams =
							vsProgram.ResolveParameter( GpuProgramParameters.GpuConstantType.Float4, -1,
							                            GpuProgramParameters.GpuParamVariability.Lights,
							                            "light_attenuation" );
						if ( lightParamsList[ i ].AttenuatParams == null )
						{
							return false;
						}
						break;
					case LightType.Spotlight:
						worldViewMatrix =
							vsProgram.ResolveAutoParameterInt( GpuProgramParameters.AutoConstantType.WorldViewMatrix, 0 );
						if ( worldViewMatrix == null )
						{
							return false;
						}

						vsInPosition = vsMain.ResolveInputParameter( Parameter.SemanticType.Position, 0,
						                                             Parameter.ContentType.PositionObjectSpace,
						                                             GpuProgramParameters.GpuConstantType.Float4 );
						if ( vsInPosition == null )
						{
							return false;
						}

						lightParamsList[ i ].Position =
							vsProgram.ResolveParameter( GpuProgramParameters.GpuConstantType.Float4, -1,
							                            GpuProgramParameters.GpuParamVariability.Lights,
							                            "light_position_view_space" );
						if ( lightParamsList[ i ].Position == null )
						{
							return false;
						}


						lightParamsList[ i ].Direction =
							vsProgram.ResolveParameter( GpuProgramParameters.GpuConstantType.Float4, -1,
							                            GpuProgramParameters.GpuParamVariability.Lights,
							                            "light_direction_view_space" );
						if ( lightParamsList[ i ].Direction == null )
						{
							return false;
						}

						lightParamsList[ i ].AttenuatParams =
							vsProgram.ResolveParameter( GpuProgramParameters.GpuConstantType.Float4, -1,
							                            GpuProgramParameters.GpuParamVariability.Lights,
							                            "light_attenuation" );
						if ( lightParamsList[ i ].AttenuatParams == null )
						{
							return false;
						}

						lightParamsList[ i ].SpotParams =
							vsProgram.ResolveParameter( GpuProgramParameters.GpuConstantType.Float3, -1,
							                            GpuProgramParameters.GpuParamVariability.Lights,
							                            "spotlight_params" );
						if ( lightParamsList[ i ].SpotParams == null )
						{
							return false;
						}

						break;
				}

				//Resolve diffuse color
				if ( ( trackVertexColorType & TrackVertexColor.Diffuse ) == 0 )
				{
					lightParamsList[ i ].DiffuseColor =
						vsProgram.ResolveParameter( GpuProgramParameters.GpuConstantType.Float4, -1,
						                            GpuProgramParameters.GpuParamVariability.Global |
						                            GpuProgramParameters.GpuParamVariability.Lights,
						                            "derived_light_diffuse" );
					if ( lightParamsList[ i ].DiffuseColor == null )
					{
						return false;
					}
				}
				else
				{
					lightParamsList[ i ].DiffuseColor =
						vsProgram.ResolveParameter( GpuProgramParameters.GpuConstantType.Float4, -1,
						                            GpuProgramParameters.GpuParamVariability.Lights, "light_diffuse" );
					if ( lightParamsList[ i ].DiffuseColor == null )
					{
						return false;
					}
				}

				if ( specularEnable )
				{
					//Resolve specular color
					if ( ( trackVertexColorType & TrackVertexColor.Specular ) == 0 )
					{
						lightParamsList[ i ].SpecularColor =
							vsProgram.ResolveParameter( GpuProgramParameters.GpuConstantType.Float4, -1,
							                            GpuProgramParameters.GpuParamVariability.Global |
							                            GpuProgramParameters.GpuParamVariability.Lights,
							                            "derived_light_specular" );
						if ( lightParamsList[ i ].SpecularColor == null )
						{
							return false;
						}
					}
					else
					{
						lightParamsList[ i ].SpecularColor =
							vsProgram.ResolveParameter( GpuProgramParameters.GpuConstantType.Float4, -1,
							                            GpuProgramParameters.GpuParamVariability.Lights,
							                            "light_specular" );
						if ( lightParamsList[ i ].SpecularColor == null )
						{
							return false;
						}
					}

					if ( vsOutSpecular == null )
					{
						vsOutSpecular = vsMain.ResolveOutputParameter( Parameter.SemanticType.Color, 1,
						                                               Parameter.ContentType.ColorSpecular,
						                                               GpuProgramParameters.GpuConstantType.Float4 );
						if ( vsOutSpecular == null )
						{
							return false;
						}
					}

					if ( vsInPosition == null )
					{
						vsInPosition = vsMain.ResolveInputParameter( Parameter.SemanticType.Position, 0,
						                                             Parameter.ContentType.PositionObjectSpace,
						                                             GpuProgramParameters.GpuConstantType.Float4 );
						if ( vsInPosition == null )
						{
							return false;
						}
					}

					if ( worldViewMatrix == null )
					{
						worldViewMatrix =
							vsProgram.ResolveAutoParameterInt( GpuProgramParameters.AutoConstantType.WorldViewMatrix, 0 );
						if ( worldViewMatrix == null )
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
			vsProgram.AddDependency( FFPRenderState.FFPLibCommon );
			vsProgram.AddDependency( FFPRenderState.FFPLibCommon );

			return true;
		}

		protected override bool AddFunctionInvocations( ProgramSet programSet )
		{
			Program vsProgram = programSet.CpuVertexProgram;
			Function vsMain = vsProgram.EntryPointFunction;

			int internalCounter = 0;

			//Add the global illumination functions
			if (
				!AddGlobalIlluminationInvocation( vsMain, (int)FFPRenderState.FFPVertexShaderStage.VSLighting,
				                                  ref internalCounter ) )
			{
				return false;
			}

			//Add per light funcitons
			for ( int i = 0; i < lightParamsList.Count; i++ )
			{
				if (
					!AddIlluminationInvocation( lightParamsList[ i ], vsMain,
					                            (int)FFPRenderState.FFPVertexShaderStage.VSLighting, ref internalCounter ) )
				{
					return false;
				}
			}

			return true;
		}

		private bool AddGlobalIlluminationInvocation( Function vsMain, int groupOrder, ref int internalCounter )
		{
			FunctionInvocation curFuncInvocation = null;
			if ( ( trackVertexColorType & TrackVertexColor.Ambient ) == 0 &&
			     ( trackVertexColorType & TrackVertexColor.Emissive ) == 0 )
			{
				curFuncInvocation = new FunctionInvocation( FFPRenderState.FFPFuncAssign, groupOrder, internalCounter++ );
				curFuncInvocation.PushOperand( derivedSceneColor, Operand.OpSemantic.In );
				curFuncInvocation.PushOperand( vsOutDiffuse, Operand.OpSemantic.Out );
				vsMain.AddAtomInstance( curFuncInvocation );
			}
			else
			{
				if ( ( trackVertexColorType & TrackVertexColor.Ambient ) == TrackVertexColor.Ambient )
				{
					curFuncInvocation = new FunctionInvocation( FFPRenderState.FFPFuncModulate, groupOrder,
					                                            internalCounter++ );
					curFuncInvocation.PushOperand( lightAmbientColor, Operand.OpSemantic.In );
					curFuncInvocation.PushOperand( vsDiffuse, Operand.OpSemantic.In );
					curFuncInvocation.PushOperand( vsOutDiffuse, Operand.OpSemantic.Out );
					vsMain.AddAtomInstance( curFuncInvocation );
				}
				else
				{
					curFuncInvocation = new FunctionInvocation( FFPRenderState.FFPFuncAssign, groupOrder,
					                                            internalCounter++ );
					curFuncInvocation.PushOperand( derivedAmbientLightColor, Operand.OpSemantic.In,
					                               ( Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z ) );
					curFuncInvocation.PushOperand( vsOutDiffuse, Operand.OpSemantic.Out,
					                               ( Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z ) );
					vsMain.AddAtomInstance( curFuncInvocation );
				}

				if ( ( trackVertexColorType & TrackVertexColor.Emissive ) == TrackVertexColor.Emissive )
				{
					curFuncInvocation = new FunctionInvocation( FFPRenderState.FFPFuncAssign, groupOrder,
					                                            internalCounter++ );
					curFuncInvocation.PushOperand( surfaceEmissiveCoilor, Operand.OpSemantic.In );
					curFuncInvocation.PushOperand( vsOutDiffuse, Operand.OpSemantic.In );
					curFuncInvocation.PushOperand( vsOutDiffuse, Operand.OpSemantic.Out );
					vsMain.AddAtomInstance( curFuncInvocation );
				}
				else
				{
					curFuncInvocation = new FunctionInvocation( FFPRenderState.FFPFuncAdd, groupOrder, internalCounter++ );
					curFuncInvocation.PushOperand( surfaceEmissiveCoilor, Operand.OpSemantic.In );
					curFuncInvocation.PushOperand( vsOutDiffuse, Operand.OpSemantic.In );
					curFuncInvocation.PushOperand( vsOutDiffuse, Operand.OpSemantic.Out );
					vsMain.AddAtomInstance( curFuncInvocation );
				}
			}
			return true;
		}

		private bool AddIlluminationInvocation( LightParams curLightParams, Function vsMain, int groupOrder,
		                                        ref int internalCounter )
		{
			FunctionInvocation curFuncInvocation = null;


			//Merge dffuse color with vertex color if need to
			if ( ( trackVertexColorType & TrackVertexColor.Diffuse ) == TrackVertexColor.Diffuse )
			{
				curFuncInvocation = new FunctionInvocation( FFPRenderState.FFPFuncModulate, groupOrder,
				                                            internalCounter++ );
				curFuncInvocation.PushOperand( vsDiffuse, Operand.OpSemantic.In,
				                               ( Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z ) );
				curFuncInvocation.PushOperand( curLightParams.DiffuseColor, Operand.OpSemantic.In,
				                               ( Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z ) );
				curFuncInvocation.PushOperand( curLightParams.DiffuseColor, Operand.OpSemantic.Out,
				                               ( Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z ) );
				vsMain.AddAtomInstance( curFuncInvocation );
			}

			//Merge specular color with vertex color if need to
			if ( specularEnable && ( trackVertexColorType & TrackVertexColor.Specular ) == TrackVertexColor.Specular )
			{
				curFuncInvocation = new FunctionInvocation( FFPRenderState.FFPFuncModulate, groupOrder,
				                                            internalCounter++ );
				curFuncInvocation.PushOperand( vsDiffuse, Operand.OpSemantic.In,
				                               ( Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z ) );
				curFuncInvocation.PushOperand( curLightParams.SpecularColor, Operand.OpSemantic.In,
				                               ( Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z ) );
				curFuncInvocation.PushOperand( curLightParams.SpecularColor, Operand.OpSemantic.Out,
				                               ( Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z ) );
				vsMain.AddAtomInstance( curFuncInvocation );
			}

			switch ( curLightParams.Type )
			{
				case LightType.Directional:
					if ( specularEnable )
					{
						curFuncInvocation = new FunctionInvocation(
							FFPRenderState.FFPFuncLightDirectionDiffuseSpecular, groupOrder, internalCounter++ );
						curFuncInvocation.PushOperand( worldViewMatrix, Operand.OpSemantic.In );
						curFuncInvocation.PushOperand( vsInPosition, Operand.OpSemantic.In );
						curFuncInvocation.PushOperand( worldViewITMatrix, Operand.OpSemantic.In );
						curFuncInvocation.PushOperand( vsInNormal, Operand.OpSemantic.In );
						curFuncInvocation.PushOperand( curLightParams.Direction, Operand.OpSemantic.In,
						                               ( Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z ) );
						curFuncInvocation.PushOperand( curLightParams.DiffuseColor, Operand.OpSemantic.In,
						                               ( Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z ) );
						curFuncInvocation.PushOperand( curLightParams.SpecularColor, Operand.OpSemantic.In,
						                               ( Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z ) );
						curFuncInvocation.PushOperand( surfaceShininess, Operand.OpSemantic.In );
						curFuncInvocation.PushOperand( vsOutDiffuse, Operand.OpSemantic.In,
						                               ( Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z ) );
						curFuncInvocation.PushOperand( vsOutSpecular, Operand.OpSemantic.In,
						                               ( Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z ) );
						curFuncInvocation.PushOperand( vsOutDiffuse, Operand.OpSemantic.Out,
						                               ( Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z ) );
						curFuncInvocation.PushOperand( vsOutSpecular, Operand.OpSemantic.Out,
						                               ( Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z ) );
						vsMain.AddAtomInstance( curFuncInvocation );
					}
					else
					{
						curFuncInvocation = new FunctionInvocation( FFPRenderState.FFPFuncLightDirectionDiffuse,
						                                            groupOrder, internalCounter++ );
						curFuncInvocation.PushOperand( worldViewITMatrix, Operand.OpSemantic.In );
						curFuncInvocation.PushOperand( vsInNormal, Operand.OpSemantic.In );
						curFuncInvocation.PushOperand( curLightParams.Direction, Operand.OpSemantic.In,
						                               ( Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z ) );
						curFuncInvocation.PushOperand( curLightParams.DiffuseColor, Operand.OpSemantic.In,
						                               ( Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z ) );
						curFuncInvocation.PushOperand( vsOutDiffuse, Operand.OpSemantic.In,
						                               ( Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z ) );
						curFuncInvocation.PushOperand( vsOutDiffuse, Operand.OpSemantic.Out,
						                               ( Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z ) );
						vsMain.AddAtomInstance( curFuncInvocation );
					}
					break;
				case LightType.Point:
					if ( specularEnable )
					{
						curFuncInvocation = new FunctionInvocation( FFPRenderState.FFPFuncLightPointDiffuseSpecular,
						                                            groupOrder, internalCounter++ );
						curFuncInvocation.PushOperand( worldViewMatrix, Operand.OpSemantic.In );
						curFuncInvocation.PushOperand( vsInPosition, Operand.OpSemantic.In );
						curFuncInvocation.PushOperand( worldViewITMatrix, Operand.OpSemantic.In );
						curFuncInvocation.PushOperand( vsInNormal, Operand.OpSemantic.In );
						curFuncInvocation.PushOperand( curLightParams.Position, Operand.OpSemantic.In,
						                               ( Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z ) );
						curFuncInvocation.PushOperand( curLightParams.AttenuatParams, Operand.OpSemantic.In );
						curFuncInvocation.PushOperand( curLightParams.DiffuseColor, Operand.OpSemantic.In,
						                               ( Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z ) );
						curFuncInvocation.PushOperand( curLightParams.SpecularColor, Operand.OpSemantic.In,
						                               ( Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z ) );
						curFuncInvocation.PushOperand( surfaceShininess, Operand.OpSemantic.In );
						curFuncInvocation.PushOperand( vsOutDiffuse, Operand.OpSemantic.In,
						                               ( Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z ) );
						curFuncInvocation.PushOperand( vsOutSpecular, Operand.OpSemantic.In,
						                               ( Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z ) );
						curFuncInvocation.PushOperand( vsOutDiffuse, Operand.OpSemantic.Out,
						                               ( Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z ) );
						curFuncInvocation.PushOperand( vsOutSpecular, Operand.OpSemantic.Out,
						                               ( Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z ) );
						vsMain.AddAtomInstance( curFuncInvocation );
					}
					else
					{
						curFuncInvocation = new FunctionInvocation( FFPRenderState.FFPFuncLightPointDiffuse, groupOrder,
						                                            internalCounter++ );
						curFuncInvocation.PushOperand( worldViewMatrix, Operand.OpSemantic.In );
						curFuncInvocation.PushOperand( vsInPosition, Operand.OpSemantic.In );
						curFuncInvocation.PushOperand( worldViewITMatrix, Operand.OpSemantic.In );
						curFuncInvocation.PushOperand( vsInNormal, Operand.OpSemantic.In );
						curFuncInvocation.PushOperand( curLightParams.Position, Operand.OpSemantic.In,
						                               ( Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z ) );
						curFuncInvocation.PushOperand( curLightParams.AttenuatParams, Operand.OpSemantic.In );
						curFuncInvocation.PushOperand( curLightParams.DiffuseColor, Operand.OpSemantic.In,
						                               ( Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z ) );
						curFuncInvocation.PushOperand( vsOutDiffuse, Operand.OpSemantic.In,
						                               ( Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z ) );
						curFuncInvocation.PushOperand( vsOutDiffuse, Operand.OpSemantic.Out,
						                               ( Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z ) );
						vsMain.AddAtomInstance( curFuncInvocation );
					}
					break;
				case LightType.Spotlight:
					if ( specularEnable )
					{
						curFuncInvocation = new FunctionInvocation( FFPRenderState.FFPFuncLightSpotDiffuseSpecular,
						                                            groupOrder, internalCounter++ );
						curFuncInvocation.PushOperand( worldViewITMatrix, Operand.OpSemantic.In );
						curFuncInvocation.PushOperand( vsInPosition, Operand.OpSemantic.In );
						curFuncInvocation.PushOperand( worldViewITMatrix, Operand.OpSemantic.In );
						curFuncInvocation.PushOperand( vsInNormal, Operand.OpSemantic.In );
						curFuncInvocation.PushOperand( curLightParams.Position, Operand.OpSemantic.In,
						                               ( Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z ) );
						curFuncInvocation.PushOperand( curLightParams.Direction, Operand.OpSemantic.In,
						                               ( Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z ) );
						curFuncInvocation.PushOperand( curLightParams.AttenuatParams, Operand.OpSemantic.In );
						curFuncInvocation.PushOperand( curLightParams.SpotParams, Operand.OpSemantic.In );
						curFuncInvocation.PushOperand( curLightParams.DiffuseColor, Operand.OpSemantic.In,
						                               ( Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z ) );
						curFuncInvocation.PushOperand( curLightParams.SpecularColor, Operand.OpSemantic.In,
						                               ( Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z ) );
						curFuncInvocation.PushOperand( surfaceShininess, Operand.OpSemantic.In );
						curFuncInvocation.PushOperand( vsOutDiffuse, Operand.OpSemantic.In,
						                               ( Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z ) );
						curFuncInvocation.PushOperand( vsOutSpecular, Operand.OpSemantic.In,
						                               ( Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z ) );
						curFuncInvocation.PushOperand( vsOutDiffuse, Operand.OpSemantic.Out,
						                               ( Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z ) );
						curFuncInvocation.PushOperand( vsOutSpecular, Operand.OpSemantic.Out,
						                               ( Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z ) );
						vsMain.AddAtomInstance( curFuncInvocation );
					}
					else
					{
						curFuncInvocation = new FunctionInvocation( FFPRenderState.FFPFuncLightSpotDiffuse, groupOrder,
						                                            internalCounter++ );
						curFuncInvocation.PushOperand( worldViewMatrix, Operand.OpSemantic.In );
						curFuncInvocation.PushOperand( vsInPosition, Operand.OpSemantic.In );
						curFuncInvocation.PushOperand( worldViewITMatrix, Operand.OpSemantic.In );
						curFuncInvocation.PushOperand( vsInNormal, Operand.OpSemantic.In );
						curFuncInvocation.PushOperand( curLightParams.Position, Operand.OpSemantic.In,
						                               ( Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z ) );
						curFuncInvocation.PushOperand( curLightParams.DiffuseColor, Operand.OpSemantic.In,
						                               ( Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z ) );
						curFuncInvocation.PushOperand( curLightParams.AttenuatParams, Operand.OpSemantic.In );
						curFuncInvocation.PushOperand( curLightParams.SpotParams, Operand.OpSemantic.In );
						curFuncInvocation.PushOperand( curLightParams.DiffuseColor, Operand.OpSemantic.In,
						                               ( Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z ) );
						curFuncInvocation.PushOperand( vsOutDiffuse, Operand.OpSemantic.In,
						                               ( Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z ) );
						curFuncInvocation.PushOperand( vsOutDiffuse, Operand.OpSemantic.Out,
						                               ( Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z ) );
						vsMain.AddAtomInstance( curFuncInvocation );
					}
					break;
			}
			return true;
		}

		private bool SpecularEnabled
		{
			get
			{
				return specularEnable;
			}
			set
			{
				specularEnable = value;
			}
		}

		private TrackVertexColor TrackVertexColorType
		{
			get
			{
				return trackVertexColorType;
			}
			set
			{
				trackVertexColorType = value;
			}
		}

		public override string Type
		{
			get
			{
				return FFPLighting.FFPType;
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