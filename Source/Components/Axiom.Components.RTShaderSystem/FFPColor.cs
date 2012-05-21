using Axiom.Core;

namespace Axiom.Components.RTShaderSystem
{
	public class FFPColor : SubRenderState
	{
		public enum StageFlags
		{
			VsInputDiffuse = 1 << 1,
			VsInputSpecular = 1 << 2,
			VsOutputdiffuse = 1 << 3,
			VsOutputSpecular = 1 << 4,
			PsInputDiffuse = 1 << 5,
			PsInputSpecular = 1 << 6,
			PsOutputDiffuse = 1 << 7,
			PsOutputSpecular = 1 << 8
		}

		public static string FFPType = "FFP_Color";
		private int resolveStageFlags;
		private Parameter vsInputDiffuse;
		private Parameter vsInputSpecular;
		private Parameter vsOutputDiffuse;
		private Parameter vsOutputSpecular;
		private Parameter psInputDiffuse;
		private Parameter psInputSpecular;
		private Parameter psOutputDiffuse;
		private Parameter psOutputSpecular;

		public FFPColor()
		{
			this.resolveStageFlags = (int)StageFlags.PsOutputDiffuse;
		}

		public void AddResolveStageMask( int mask )
		{
			this.resolveStageFlags |= mask;
		}

		public void RemoveResolveStageMask( int mask )
		{
			this.resolveStageFlags &= ~mask;
		}

		public override bool PreAddToRenderState( TargetRenderState targetRenderState, Graphics.Pass srcPass,
		                                          Graphics.Pass dstPass )
		{
			TrackVertexColor trackColor = srcPass.VertexColorTracking;

			if ( trackColor != null )
			{
				AddResolveStageMask( (int)StageFlags.VsInputDiffuse );
			}

			return true;
		}

		protected override bool ResolveParameters( ProgramSet programSet )
		{
			Program vsProgram = programSet.CpuVertexProgram;
			Program psProgram = programSet.CpuFragmentProgram;
			Function vsMain = vsProgram.EntryPointFunction;
			Function psMain = psProgram.EntryPointFunction;

			bool success = ( this.resolveStageFlags & (int)StageFlags.VsInputDiffuse ) == 1;
			if ( success )
			{
				this.vsInputDiffuse = vsMain.ResolveInputParameter( Parameter.SemanticType.Color, 0,
				                                                    Parameter.ContentType.ColorDiffuse,
				                                                    Graphics.GpuProgramParameters.GpuConstantType.Float4 );
			}
			success = ( this.resolveStageFlags & (int)StageFlags.VsInputSpecular ) == 1;
			if ( success )
			{
				this.vsInputSpecular = vsMain.ResolveInputParameter( Parameter.SemanticType.Color, 1,
				                                                     Parameter.ContentType.ColorSpecular,
				                                                     Graphics.GpuProgramParameters.GpuConstantType.Float4 );
			}

			//Resolve VS color outputs if have inputs from vertex stream
			if ( this.vsInputDiffuse != null || ( this.resolveStageFlags & (int)StageFlags.VsOutputdiffuse ) == 1 )
			{
				this.vsOutputDiffuse = vsMain.ResolveOutputParameter( Parameter.SemanticType.Color, 0,
				                                                      Parameter.ContentType.ColorDiffuse,
				                                                      Graphics.GpuProgramParameters.GpuConstantType.Float4 );
			}

			if ( this.vsInputSpecular != null || ( this.resolveStageFlags & (int)StageFlags.VsOutputSpecular ) == 1 )
			{
				this.vsOutputSpecular = vsMain.ResolveOutputParameter( Parameter.SemanticType.Color, 1,
				                                                       Parameter.ContentType.ColorSpecular,
				                                                       Graphics.GpuProgramParameters.GpuConstantType.Float4 );
			}

			//Resolve PS color inputs if we have inputs from vertex shader.
			if ( this.vsOutputDiffuse != null || ( this.resolveStageFlags & (int)StageFlags.PsInputDiffuse ) == 1 )
			{
				this.psInputDiffuse = psMain.ResolveInputParameter( Parameter.SemanticType.Color, 0,
				                                                    Parameter.ContentType.ColorDiffuse,
				                                                    Graphics.GpuProgramParameters.GpuConstantType.Float4 );
			}

			if ( this.vsOutputSpecular != null || ( this.resolveStageFlags & (int)StageFlags.PsInputSpecular ) == 1 )
			{
				this.psInputDiffuse = psMain.ResolveInputParameter( Parameter.SemanticType.Color, 1,
				                                                    Parameter.ContentType.ColorSpecular,
				                                                    Graphics.GpuProgramParameters.GpuConstantType.Float4 );
			}

			//Resolve PS output diffuse color
			if ( ( this.resolveStageFlags & (int)StageFlags.PsOutputDiffuse ) == 1 )
			{
				this.psOutputDiffuse = psMain.ResolveOutputParameter( Parameter.SemanticType.Color, 0,
				                                                      Parameter.ContentType.ColorDiffuse,
				                                                      Graphics.GpuProgramParameters.GpuConstantType.Float4 );
				if ( this.psOutputDiffuse == null )
				{
					return false;
				}
			}

			//Resolve PS output specular color
			if ( ( this.resolveStageFlags & (int)StageFlags.PsOutputSpecular ) == 1 )
			{
				this.psOutputSpecular = psMain.ResolveOutputParameter( Parameter.SemanticType.Color, 1,
				                                                       Parameter.ContentType.ColorSpecular,
				                                                       Graphics.GpuProgramParameters.GpuConstantType.Float4 );
				if ( this.psOutputSpecular == null )
				{
					return false;
				}
			}

			return true;
		}

		protected override bool ResolveDependencies( ProgramSet programSet )
		{
			Program vsProgram = programSet.CpuVertexProgram;
			Program psProgram = programSet.CpuFragmentProgram;
			vsProgram.AddDependency( FFPRenderState.FFPLibCommon );
			psProgram.AddDependency( FFPRenderState.FFPLibCommon );

			return true;
		}

		protected override bool AddFunctionInvocations( ProgramSet programSet )
		{
			Program vsProgram = programSet.CpuVertexProgram;
			Program psProgram = programSet.CpuFragmentProgram;
			Function vsMain = vsProgram.EntryPointFunction;
			Function psMain = psProgram.EntryPointFunction;
			FunctionInvocation curFuncInvocation = null;
			int internalCounter;

			//Create vertex shader color invocations
			Parameter vsDiffuse, vsSpecular;
			internalCounter = 0;
			if ( this.vsInputDiffuse != null )
			{
				vsDiffuse = this.vsInputDiffuse;
			}
			else
			{
				vsDiffuse = vsMain.ResolveLocalParameter( Parameter.SemanticType.Color, 0,
				                                          Parameter.ContentType.ColorDiffuse,
				                                          Graphics.GpuProgramParameters.GpuConstantType.Float4 );
				curFuncInvocation = new FunctionInvocation( FFPRenderState.FFPFuncConstruct,
				                                            (int)FFPRenderState.FFPVertexShaderStage.VSColor,
				                                            internalCounter++ );
				curFuncInvocation.PushOperand( ParameterFactory.CreateConstParamFloat( 1.0f ), Operand.OpSemantic.In );
				curFuncInvocation.PushOperand( ParameterFactory.CreateConstParamFloat( 1.0f ), Operand.OpSemantic.In );
				curFuncInvocation.PushOperand( ParameterFactory.CreateConstParamFloat( 1.0f ), Operand.OpSemantic.In );
				curFuncInvocation.PushOperand( ParameterFactory.CreateConstParamFloat( 1.0f ), Operand.OpSemantic.In );
				curFuncInvocation.PushOperand( vsDiffuse, Operand.OpSemantic.Out );
				vsMain.AddAtomInstance( curFuncInvocation );
			}

			if ( this.vsOutputDiffuse != null )
			{
				curFuncInvocation = new FunctionInvocation( FFPRenderState.FFPFuncAssign,
				                                            (int)FFPRenderState.FFPVertexShaderStage.VSColor,
				                                            internalCounter++ );
				curFuncInvocation.PushOperand( vsDiffuse, Operand.OpSemantic.In );
				curFuncInvocation.PushOperand( this.vsOutputDiffuse, Operand.OpSemantic.Out );
				vsMain.AddAtomInstance( curFuncInvocation );
			}

			if ( this.vsInputSpecular != null )
			{
				vsSpecular = this.vsInputSpecular;
			}
			else
			{
				vsSpecular = vsMain.ResolveLocalParameter( Parameter.SemanticType.Color, 1,
				                                           Parameter.ContentType.ColorSpecular,
				                                           Graphics.GpuProgramParameters.GpuConstantType.Float4 );
				curFuncInvocation = new FunctionInvocation( FFPRenderState.FFPFuncConstruct,
				                                            (int)FFPRenderState.FFPVertexShaderStage.VSColor,
				                                            internalCounter++ );
				curFuncInvocation.PushOperand( ParameterFactory.CreateConstParamFloat( 0.0f ), Operand.OpSemantic.In );
				curFuncInvocation.PushOperand( ParameterFactory.CreateConstParamFloat( 0.0f ), Operand.OpSemantic.In );
				curFuncInvocation.PushOperand( ParameterFactory.CreateConstParamFloat( 0.0f ), Operand.OpSemantic.In );
				curFuncInvocation.PushOperand( ParameterFactory.CreateConstParamFloat( 0.0f ), Operand.OpSemantic.In );
				curFuncInvocation.PushOperand( vsSpecular, Operand.OpSemantic.Out );
				vsMain.AddAtomInstance( curFuncInvocation );
			}

			if ( this.vsOutputSpecular != null )
			{
				curFuncInvocation = new FunctionInvocation( FFPRenderState.FFPFuncAssign,
				                                            (int)FFPRenderState.FFPVertexShaderStage.VSColor,
				                                            internalCounter++ );
				curFuncInvocation.PushOperand( vsSpecular, Operand.OpSemantic.In );
				curFuncInvocation.PushOperand( this.vsOutputSpecular, Operand.OpSemantic.Out );
				vsMain.AddAtomInstance( curFuncInvocation );
			}

			//Create fragment shader color invocations
			Parameter psDiffuse, psSpecular;
			internalCounter = 0;

			//Handle diffuse color
			if ( this.psInputDiffuse != null )
			{
				psDiffuse = this.psInputDiffuse;
			}
			else
			{
				psDiffuse = psMain.ResolveLocalParameter( Parameter.SemanticType.Color, 0,
				                                          Parameter.ContentType.ColorDiffuse,
				                                          Graphics.GpuProgramParameters.GpuConstantType.Float4 );
				curFuncInvocation = new FunctionInvocation( FFPRenderState.FFPFuncConstruct,
				                                            (int)FFPRenderState.FFPFragmentShaderStage.PSColorBegin,
				                                            internalCounter++ );
				curFuncInvocation.PushOperand( ParameterFactory.CreateConstParamFloat( 1.0f ), Operand.OpSemantic.In );
				curFuncInvocation.PushOperand( ParameterFactory.CreateConstParamFloat( 1.0f ), Operand.OpSemantic.In );
				curFuncInvocation.PushOperand( ParameterFactory.CreateConstParamFloat( 1.0f ), Operand.OpSemantic.In );
				curFuncInvocation.PushOperand( ParameterFactory.CreateConstParamFloat( 1.0f ), Operand.OpSemantic.In );
				curFuncInvocation.PushOperand( psDiffuse, Operand.OpSemantic.Out );
				psMain.AddAtomInstance( curFuncInvocation );
			}

			//Handle specular color
			if ( this.psInputSpecular != null )
			{
				psSpecular = this.psInputSpecular;
			}
			else
			{
				psSpecular = psMain.ResolveLocalParameter( Parameter.SemanticType.Color, 1,
				                                           Parameter.ContentType.ColorSpecular,
				                                           Graphics.GpuProgramParameters.GpuConstantType.Float4 );
				curFuncInvocation = new FunctionInvocation( FFPRenderState.FFPFuncConstruct,
				                                            (int)FFPRenderState.FFPFragmentShaderStage.PSColorBegin,
				                                            internalCounter++ );
				curFuncInvocation.PushOperand( ParameterFactory.CreateConstParamFloat( 0.0f ), Operand.OpSemantic.In );
				curFuncInvocation.PushOperand( ParameterFactory.CreateConstParamFloat( 0.0f ), Operand.OpSemantic.In );
				curFuncInvocation.PushOperand( ParameterFactory.CreateConstParamFloat( 0.0f ), Operand.OpSemantic.In );
				curFuncInvocation.PushOperand( ParameterFactory.CreateConstParamFloat( 0.0f ), Operand.OpSemantic.In );
				curFuncInvocation.PushOperand( psSpecular, Operand.OpSemantic.Out );
				psMain.AddAtomInstance( curFuncInvocation );
			}

			//Assign diffuse color
			if ( this.psOutputDiffuse != null )
			{
				curFuncInvocation = new FunctionInvocation( FFPRenderState.FFPFuncAssign,
				                                            (int)FFPRenderState.FFPFragmentShaderStage.PSColorBegin,
				                                            internalCounter++ );
				curFuncInvocation.PushOperand( psDiffuse, Operand.OpSemantic.In );
				curFuncInvocation.PushOperand( this.psOutputDiffuse, Operand.OpSemantic.Out );
				psMain.AddAtomInstance( curFuncInvocation );
			}

			//Assign specular color
			if ( this.psOutputSpecular != null )
			{
				curFuncInvocation = new FunctionInvocation( FFPRenderState.FFPFuncAssign,
				                                            (int)FFPRenderState.FFPFragmentShaderStage.PSColorBegin,
				                                            internalCounter++ );
				curFuncInvocation.PushOperand( psSpecular, Operand.OpSemantic.In );
				curFuncInvocation.PushOperand( this.psOutputSpecular, Operand.OpSemantic.Out );
				psMain.AddAtomInstance( curFuncInvocation );
			}

			//Add specular to out color
			internalCounter = 0;
			if ( this.psOutputDiffuse != null && psSpecular != null )
			{
				curFuncInvocation = new FunctionInvocation( FFPRenderState.FFPFuncAdd,
				                                            (int)FFPRenderState.FFPFragmentShaderStage.PSColorEnd,
				                                            internalCounter++ );
				curFuncInvocation.PushOperand( this.psOutputDiffuse, Operand.OpSemantic.In,
				                               ( Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z ) );
				curFuncInvocation.PushOperand( psSpecular, Operand.OpSemantic.In,
				                               ( Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z ) );
				curFuncInvocation.PushOperand( this.psOutputDiffuse, Operand.OpSemantic.Out,
				                               ( Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z ) );
				psMain.AddAtomInstance( curFuncInvocation );
			}

			return true;
		}

		/// <summary>
		///   Gets/Sets the resolve stage flags that this sub render state will produce. I.E. - If one want to specify that the vertex shader program needs to get a diffuse component and the pixel shader should output diffuse component he should pass VsInputDiffuse | PsOutputdiffuse
		/// </summary>
		public int ResolveStageFlags
		{
			get
			{
				return this.resolveStageFlags;
			}
			set
			{
				this.resolveStageFlags = value;
			}
		}

		public override int ExecutionOrder
		{
			get
			{
				return (int)FFPRenderState.FFPShaderStage.Color;
			}
		}

		public override string Type
		{
			get
			{
				return FFPColor.FFPType;
			}
		}
	}
}