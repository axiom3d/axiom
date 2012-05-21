using Axiom.Core;
using Axiom.Graphics;
using Axiom.Math;

namespace Axiom.Components.RTShaderSystem
{
	public class FFPFog : SubRenderState
	{
		public enum CalcMode
		{
			PerVertex = 1,
			PerPixel = 2
		}

		public static string FFPType = "FFP_Fog";
		private CalcMode calcMode;
		private FogMode fogMode;
		private ColorEx fogColorValue;
		private Vector4 fogParamsValue;
		private bool passOverrideParams;

		private UniformParameter worldViewProjMatrix;
		private UniformParameter fogColor;
		private UniformParameter fogParams;
		private Parameter vsInPos;
		private Parameter vsOutFogFactor;
		private Parameter psInFogFactor;
		private Parameter vsOutDepth;
		private Parameter psInDepth;
		private Parameter psOutDiffuse;

		public FFPFog()
		{
			this.fogMode = FogMode.None;
			this.calcMode = FFPFog.CalcMode.PerVertex;
			this.passOverrideParams = false;
		}

		public override void UpdateGpuProgramsParams( IRenderable rend, Pass pass, AutoParamDataSource source,
		                                              Core.Collections.LightList lightList )
		{
			if ( this.fogMode == FogMode.None )
			{
				return;
			}

			FogMode fMode;
			ColorEx newFogColor;
			Real newFogStart, newFogEnd, newFogDensity;

			if ( this.passOverrideParams )
			{
				fMode = pass.FogMode;
				newFogColor = pass.FogColor;
				newFogStart = pass.FogStart;
				newFogEnd = pass.FogEnd;
				newFogDensity = pass.FogDensity;
			}
			else
			{
				var sceneMgr = ShaderGenerator.Instance.ActiveSceneManager;

				fMode = sceneMgr.FogMode;
				newFogColor = sceneMgr.FogColor;
				newFogStart = sceneMgr.FogStart;
				newFogEnd = sceneMgr.FogEnd;
				newFogDensity = sceneMgr.FogDensity;
			}

			SetFogProperties( fMode, newFogColor, newFogStart, newFogEnd, newFogDensity );

			//Per pixel fog
			if ( this.calcMode == CalcMode.PerPixel )
			{
				this.fogParams.SetGpuParameter( this.fogParamsValue );
			}

				//per vertex fog
			else
			{
				this.fogParams.SetGpuParameter( this.fogParamsValue );
			}

			this.fogColor.SetGpuParameter( this.fogColorValue );
		}

		public override bool PreAddToRenderState( TargetRenderState targetRenderState, Pass srcPass, Pass dstPass )
		{
			FogMode fMode;
			ColorEx newFogColor;
			Real newFogStart, newFogEnd, newFogDensity;

			if ( srcPass.FogOverride )
			{
				fMode = srcPass.FogMode;
				newFogColor = srcPass.FogColor;
				newFogStart = srcPass.FogStart;
				newFogEnd = srcPass.FogEnd;
				newFogDensity = srcPass.FogDensity;
			}
			else
			{
				var sceneMgr = ShaderGenerator.Instance.ActiveSceneManager;

				if ( sceneMgr == null )
				{
					fMode = FogMode.None;
					newFogColor = ColorEx.White;
					newFogStart = 0.0f;
					newFogEnd = 0.0f;
					newFogDensity = 0.0f;
				}
				else
				{
					fMode = sceneMgr.FogMode;
					newFogColor = sceneMgr.FogColor;
					newFogStart = sceneMgr.FogStart;
					newFogEnd = sceneMgr.FogEnd;
					newFogDensity = sceneMgr.FogDensity;
				}

				this.passOverrideParams = false;
			}
			//Set fog properties
			SetFogProperties( fMode, newFogColor, newFogStart, newFogEnd, newFogDensity );

			//Override scene fog since it will happen in shader
			dstPass.SetFog( true, FogMode.None, newFogColor, newFogDensity, newFogStart, newFogEnd );
			return true;
		}

		public void SetFogProperties( FogMode fogMode, ColorEx fogColor, float fogStart, float fogEnd, float fogDensity )
		{
			this.fogMode = fogMode;
			this.fogColorValue = fogColor;
			this.fogParamsValue = new Vector4( fogDensity, fogStart, fogEnd, fogEnd != fogStart ? 1/( fogEnd - fogStart ) : 0 );
		}


		protected override bool ResolveParameters( ProgramSet programSet )
		{
			if ( this.fogMode == FogMode.None )
			{
				return true;
			}

			Program vsProgram = programSet.CpuVertexProgram;
			Program psProgram = programSet.CpuFragmentProgram;
			Function vsMain = vsProgram.EntryPointFunction;
			Function psMain = psProgram.EntryPointFunction;

			//Resolve world view matrix.
			this.worldViewProjMatrix =
				vsProgram.ResolveAutoParameterInt( GpuProgramParameters.AutoConstantType.WorldViewProjMatrix, 0 );
			if ( this.worldViewProjMatrix == null )
			{
				return false;
			}

			//Resolve vertex shader input position
			this.vsInPos = vsMain.ResolveInputParameter( Parameter.SemanticType.Position, 0,
			                                             Parameter.ContentType.PositionObjectSpace,
			                                             GpuProgramParameters.GpuConstantType.Float4 );
			if ( this.vsInPos == null )
			{
				return false;
			}

			//Resolve fog color
			this.fogColor = psProgram.ResolveParameter( GpuProgramParameters.GpuConstantType.Float4, -1,
			                                            GpuProgramParameters.GpuParamVariability.Global, "gFogColor" );
			if ( this.fogColor == null )
			{
				return false;
			}

			//Resolve pixel shader output diffuse color
			this.psOutDiffuse = psMain.ResolveOutputParameter( Parameter.SemanticType.Color, 0,
			                                                   Parameter.ContentType.ColorDiffuse,
			                                                   GpuProgramParameters.GpuConstantType.Float4 );
			if ( this.psOutDiffuse == null )
			{
				return false;
			}

			//Per pixel fog
			if ( this.calcMode == CalcMode.PerPixel )
			{
				//Resolve fog params
				this.fogParams = psProgram.ResolveParameter( GpuProgramParameters.GpuConstantType.Float4, -1,
				                                             GpuProgramParameters.GpuParamVariability.Global, "gFogParams" );
				if ( this.fogParams == null )
				{
					return false;
				}

				//Resolve vertex shader output depth
				this.vsOutDepth = vsMain.ResolveOutputParameter( Parameter.SemanticType.TextureCoordinates, -1,
				                                                 Parameter.ContentType.DepthViewSpace,
				                                                 GpuProgramParameters.GpuConstantType.Float1 );
				if ( this.vsOutDepth == null )
				{
					return false;
				}

				//Resolve pixel shader input depth.
				this.psInDepth = psMain.ResolveInputParameter( Parameter.SemanticType.TextureCoordinates, this.vsOutDepth.Index,
				                                               this.vsOutDepth.Content,
				                                               GpuProgramParameters.GpuConstantType.Float1 );
				if ( this.psInDepth == null )
				{
					return false;
				}
			}
				//per vertex fog
			else
			{
				this.fogParams = vsProgram.ResolveParameter( GpuProgramParameters.GpuConstantType.Float4, -1,
				                                             GpuProgramParameters.GpuParamVariability.Global, "gFogParams" );
				if ( this.fogParams == null )
				{
					return false;
				}

				//Resolve vertex shader output fog factor
				this.vsOutFogFactor = vsMain.ResolveOutputParameter( Parameter.SemanticType.TextureCoordinates, -1,
				                                                     Parameter.ContentType.Unknown,
				                                                     GpuProgramParameters.GpuConstantType.Float1 );
				if ( this.vsOutFogFactor == null )
				{
					return false;
				}

				//Resolve pixel shader input fog factor
				this.psInFogFactor = psMain.ResolveInputParameter( Parameter.SemanticType.TextureCoordinates,
				                                                   this.vsOutFogFactor.Index, this.vsOutFogFactor.Content,
				                                                   GpuProgramParameters.GpuConstantType.Float1 );
				if ( this.psInFogFactor == null )
				{
					return false;
				}
			}


			return true;
		}

		protected override bool ResolveDependencies( ProgramSet programSet )
		{
			if ( this.fogMode == FogMode.None )
			{
				return true;
			}

			Program vsProgram = programSet.CpuVertexProgram;
			Program psProgram = programSet.CpuFragmentProgram;

			vsProgram.AddDependency( FFPRenderState.FFPLibFog );
			psProgram.AddDependency( FFPRenderState.FFPLibCommon );
			//Per pixel fog.
			if ( this.calcMode == CalcMode.PerPixel )
			{
				psProgram.AddDependency( FFPRenderState.FFPLibFog );
			}

			return true;
		}

		protected override bool AddFunctionInvocations( ProgramSet programSet )
		{
			if ( this.fogMode == FogMode.None )
			{
				return true;
			}

			Program vsProgram = programSet.CpuVertexProgram;
			Program psProgram = programSet.CpuFragmentProgram;
			Function vsMain = vsProgram.EntryPointFunction;
			Function psMain = psProgram.EntryPointFunction;
			FunctionInvocation curFuncInvocation = null;
			int internalCounter = 0;

			//Per pixel fog
			if ( this.calcMode == CalcMode.PerPixel )
			{
				curFuncInvocation = new FunctionInvocation( FFPRenderState.FFPFuncPixelFogDepth,
				                                            (int)FFPRenderState.FFPVertexShaderStage.VSFog,
				                                            internalCounter++ );
				curFuncInvocation.PushOperand( this.worldViewProjMatrix, Operand.OpSemantic.In );
				curFuncInvocation.PushOperand( this.vsInPos, Operand.OpSemantic.In );
				curFuncInvocation.PushOperand( this.vsOutDepth, Operand.OpSemantic.Out );
				vsMain.AddAtomInstance( curFuncInvocation );

				internalCounter = 0;
				switch ( this.fogMode )
				{
					case FogMode.Exp:
						curFuncInvocation = new FunctionInvocation( FFPRenderState.FFPFuncPixelFogLinear,
						                                            (int)FFPRenderState.FFPFragmentShaderStage.PSFog,
						                                            internalCounter++ );
						break;
					case FogMode.Exp2:
						curFuncInvocation = new FunctionInvocation( FFPRenderState.FFPFuncPixelFogExp,
						                                            (int)FFPRenderState.FFPFragmentShaderStage.PSFog,
						                                            internalCounter++ );
						break;
					case FogMode.Linear:
						curFuncInvocation = new FunctionInvocation( FFPRenderState.FFPFuncPixelFogExp2,
						                                            (int)FFPRenderState.FFPFragmentShaderStage.PSFog,
						                                            internalCounter++ );
						break;
					default:
						break;
				}

				curFuncInvocation.PushOperand( this.psInDepth, Operand.OpSemantic.In );
				curFuncInvocation.PushOperand( this.fogParams, Operand.OpSemantic.In );
				curFuncInvocation.PushOperand( this.fogColor, Operand.OpSemantic.In );
				curFuncInvocation.PushOperand( this.psOutDiffuse, Operand.OpSemantic.In );
				curFuncInvocation.PushOperand( this.psOutDiffuse, Operand.OpSemantic.Out );
				psMain.AddAtomInstance( curFuncInvocation );
			}
			else //Per vertex fog
			{
				internalCounter = 0;
				switch ( this.fogMode )
				{
					case FogMode.Exp:
						curFuncInvocation = new FunctionInvocation( FFPRenderState.FFPFuncVertexFogLinear,
						                                            (int)FFPRenderState.FFPVertexShaderStage.VSFog,
						                                            internalCounter++ );
						break;
					case FogMode.Exp2:
						curFuncInvocation = new FunctionInvocation( FFPRenderState.FFPFuncVertexFogExp,
						                                            (int)FFPRenderState.FFPVertexShaderStage.VSFog,
						                                            internalCounter++ );
						break;
					case FogMode.Linear:
						curFuncInvocation = new FunctionInvocation( FFPRenderState.FFPFuncVertexFogExp2,
						                                            (int)FFPRenderState.FFPVertexShaderStage.VSFog,
						                                            internalCounter++ );
						break;
					default:
						break;
				}

				curFuncInvocation.PushOperand( this.worldViewProjMatrix, Operand.OpSemantic.In );
				curFuncInvocation.PushOperand( this.vsInPos, Operand.OpSemantic.In );
				curFuncInvocation.PushOperand( this.fogParams, Operand.OpSemantic.In );
				curFuncInvocation.PushOperand( this.vsOutFogFactor, Operand.OpSemantic.Out );
				vsMain.AddAtomInstance( curFuncInvocation );

				internalCounter = 0;

				curFuncInvocation = new FunctionInvocation( FFPRenderState.FFPFuncLerp,
				                                            (int)FFPRenderState.FFPFragmentShaderStage.PSFog,
				                                            internalCounter++ );
				curFuncInvocation.PushOperand( this.fogColor, Operand.OpSemantic.In );
				curFuncInvocation.PushOperand( this.psOutDiffuse, Operand.OpSemantic.In );
				curFuncInvocation.PushOperand( this.psInFogFactor, Operand.OpSemantic.In );
				curFuncInvocation.PushOperand( this.psOutDiffuse, Operand.OpSemantic.Out );
				psMain.AddAtomInstance( curFuncInvocation );
			}


			return true;
		}

		public CalcMode CalculationMode
		{
			get
			{
				return this.calcMode;
			}
			set
			{
				this.calcMode = value;
			}
		}

		public override string Type
		{
			get
			{
				return FFPFog.FFPType;
			}
		}

		public override int ExecutionOrder
		{
			get
			{
				return (int)FFPRenderState.FFPShaderStage.Fog;
			}
		}
	}
}