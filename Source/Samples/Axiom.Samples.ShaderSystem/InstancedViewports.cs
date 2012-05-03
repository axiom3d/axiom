using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Axiom.Components.RTShaderSystem;
using Axiom.Math;

namespace Axiom.Samples.ShaderSystem
{
	internal class InstancedViewports : SubRenderState
	{
		public static string SGXType = "SGX_InstancedViewports";
		private static string SGXLibInstancedViewports = "SampleLib_InstancedViewports";
		private static string SGXFuncInstancedViewportsTransform = "SGX_InstancedViewportsTransform";
		private static string SGXFuncInstancedViewportsDiscardOutOfBounds = "SGX_InstancedViewportsDiscardOutOfBounds";
		private Parameter vsInPosition;
		private Parameter vsOriginalOutPositionProjectiveSpace;
		private Parameter vsOutPositionProjectiveSpace;
		private Parameter psInPositionProjectiveSpace;
		private UniformParameter vsInMonitorsCount;
		private UniformParameter psInMonitorsCount;
		private Parameter vsInMonitorIndex;
		private Parameter vsOutMonitorIndex;
		private Parameter psInMonitorIndex;

		private Parameter vsInViewportOffsetMatrixR0;
		private Parameter vsInViewportOffsetMatrixR1;
		private Parameter vsInViewportOffsetMatrixR2;
		private Parameter vsInViewportOffsetMatrixR3;

		private UniformParameter worldViewMatrix;
		private UniformParameter projectionMatrix;

		private Vector2 monitorsCount;
		private bool monitorsCountChanged;

		public InstancedViewports()
		{
			monitorsCount = new Vector2( 1.0f, 1.0f );
			monitorsCountChanged = true;
		}

		public override bool PreAddToRenderState( TargetRenderState targetRenderState, Graphics.Pass srcPass,
		                                          Graphics.Pass dstPass )
		{
			return base.PreAddToRenderState( targetRenderState, srcPass, dstPass );
		}

		public override void UpdateGpuProgramsParams( Graphics.IRenderable rend, Graphics.Pass pass,
		                                              Graphics.AutoParamDataSource source,
		                                              Core.Collections.LightList lightList )
		{
			if ( monitorsCountChanged )
			{
				vsInMonitorsCount.SetGpuParameter( monitorsCount + new Vector2( 0.0001f, 0.0001f ) );
				psInMonitorsCount.SetGpuParameter( monitorsCount + new Vector2( 0.0001f, 0.0001f ) );

				monitorsCountChanged = false;
			}
		}

		protected override bool ResolveParameters( Axiom.Components.RTShaderSystem.ProgramSet programSet )
		{
			Program vsProgram = programSet.CpuVertexProgram;
			Program psProgram = programSet.CpuFragmentProgram;

			Function vsMain = vsProgram.EntryPointFunction;
			Function psMain = psProgram.EntryPointFunction;

			//Resolve vertex shader output position in projective space.

			vsInPosition = vsMain.ResolveInputParameter( Parameter.SemanticType.Position, 0,
			                                             Parameter.ContentType.PositionObjectSpace,
			                                             Graphics.GpuProgramParameters.GpuConstantType.Float4 );
			if ( vsInPosition == null )
			{
				return false;
			}

			vsOriginalOutPositionProjectiveSpace = vsMain.ResolveOutputParameter( Parameter.SemanticType.Position, 0,
			                                                                      Parameter.ContentType.PositionProjectiveSpace,
			                                                                      Graphics.GpuProgramParameters.GpuConstantType.
			                                                                      	Float4 );
			if ( vsOriginalOutPositionProjectiveSpace == null )
			{
				return false;
			}

			var positionProjectiveSpaceAsTexcoord = (Parameter.ContentType)( Parameter.ContentType.CustomContentBegin + 1 );
			vsOutPositionProjectiveSpace = vsMain.ResolveOutputParameter( Parameter.SemanticType.TextureCoordinates, -1,
			                                                              positionProjectiveSpaceAsTexcoord,
			                                                              Graphics.GpuProgramParameters.GpuConstantType.Float4 );
			if ( vsOutPositionProjectiveSpace == null )
			{
				return false;
			}

			//Resolve ps input position in projective space
			psInPositionProjectiveSpace = psMain.ResolveInputParameter( Parameter.SemanticType.TextureCoordinates,
			                                                            vsOutPositionProjectiveSpace.Index,
			                                                            vsOutPositionProjectiveSpace.Content,
			                                                            Graphics.GpuProgramParameters.GpuConstantType.Float4 );
			if ( psInPositionProjectiveSpace == null )
			{
				return false;
			}

			//Resolve vertex shader uniform monitors count
			vsInMonitorsCount = vsProgram.ResolveParameter( Graphics.GpuProgramParameters.GpuConstantType.Float2, -1,
			                                                Graphics.GpuProgramParameters.GpuParamVariability.Global,
			                                                "monitorsCount" );
			if ( vsInMonitorsCount == null )
			{
				return false;
			}

			//Resolve pixel shader uniform monitors count
			psInMonitorsCount = psProgram.ResolveParameter( Graphics.GpuProgramParameters.GpuConstantType.Float2, -1,
			                                                Graphics.GpuProgramParameters.GpuParamVariability.Global,
			                                                "monitorsCount" );
			if ( psInMonitorsCount == null )
			{
				return false;
			}

			//Resolve the current world & view matrices concatenated
			worldViewMatrix = vsProgram.ResolveAutoParameterInt( Graphics.GpuProgramParameters.AutoConstantType.WorldViewMatrix,
			                                                     0 );
			if ( worldViewMatrix == null )
			{
				return false;
			}

			//Resolve the current projection matrix
			projectionMatrix = vsProgram.ResolveAutoParameterInt(
				Graphics.GpuProgramParameters.AutoConstantType.ProjectionMatrix, 0 );
			if ( projectionMatrix == null )
			{
				return false;
			}

			var monitorIndex = Parameter.ContentType.TextureCoordinate3;
			//Resolve vertex shader monitor index
			vsInMonitorIndex = vsMain.ResolveInputParameter( Parameter.SemanticType.TextureCoordinates, 3, monitorIndex,
			                                                 Graphics.GpuProgramParameters.GpuConstantType.Float4 );
			if ( vsInMonitorIndex == null )
			{
				return false;
			}

			Parameter.ContentType matrixR0 = Parameter.ContentType.TextureCoordinate4;
			Parameter.ContentType matrixR1 = Parameter.ContentType.TextureCoordinate5;
			Parameter.ContentType matrixR2 = Parameter.ContentType.TextureCoordinate6;
			Parameter.ContentType matrixR3 = Parameter.ContentType.TextureCoordinate7;

			//Resolve vertex shader viewport offset matrix
			vsInViewportOffsetMatrixR0 = vsMain.ResolveInputParameter( Parameter.SemanticType.TextureCoordinates, 4, matrixR0,
			                                                           Graphics.GpuProgramParameters.GpuConstantType.Float4 );
			if ( vsInViewportOffsetMatrixR0 == null )
			{
				return false;
			}
			vsInViewportOffsetMatrixR1 = vsMain.ResolveInputParameter( Parameter.SemanticType.TextureCoordinates, 4, matrixR1,
			                                                           Graphics.GpuProgramParameters.GpuConstantType.Float4 );
			if ( vsInViewportOffsetMatrixR1 == null )
			{
				return false;
			}
			vsInViewportOffsetMatrixR2 = vsMain.ResolveInputParameter( Parameter.SemanticType.TextureCoordinates, 4, matrixR2,
			                                                           Graphics.GpuProgramParameters.GpuConstantType.Float4 );
			if ( vsInViewportOffsetMatrixR2 == null )
			{
				return false;
			}
			vsInViewportOffsetMatrixR3 = vsMain.ResolveInputParameter( Parameter.SemanticType.TextureCoordinates, 4, matrixR3,
			                                                           Graphics.GpuProgramParameters.GpuConstantType.Float4 );
			if ( vsInViewportOffsetMatrixR3 == null )
			{
				return false;
			}

			vsOutMonitorIndex = vsMain.ResolveOutputParameter( Parameter.SemanticType.TextureCoordinates, -1, monitorIndex,
			                                                   Graphics.GpuProgramParameters.GpuConstantType.Float4 );
			if ( vsOutMonitorIndex == null )
			{
				return false;
			}

			//Resolve ps input monitor index
			psInMonitorIndex = psMain.ResolveInputParameter( Parameter.SemanticType.TextureCoordinates,
			                                                 vsOutMonitorIndex.Index,
			                                                 vsOutMonitorIndex.Content,
			                                                 Graphics.GpuProgramParameters.GpuConstantType.Float4 );
			if ( psInMonitorIndex == null )
			{
				return false;
			}

			return true;
		}

		protected override bool ResolveDependencies( Axiom.Components.RTShaderSystem.ProgramSet programSet )
		{
			Program vsProgram = programSet.CpuVertexProgram;
			Program psProgram = programSet.CpuFragmentProgram;

			vsProgram.AddDependency( FFPRenderState.FFPLibCommon );
			vsProgram.AddDependency( SGXLibInstancedViewports );

			psProgram.AddDependency( FFPRenderState.FFPLibCommon );
			psProgram.AddDependency( SGXLibInstancedViewports );

			return true;
		}

		protected override bool AddFunctionInvocations( Axiom.Components.RTShaderSystem.ProgramSet programSet )
		{
			Program vsProgram = programSet.CpuVertexProgram;
			Function vsMain = vsProgram.EntryPointFunction;
			Program psProgram = programSet.CpuFragmentProgram;
			Function psMain = psProgram.EntryPointFunction;

			//Add vertex shader invocations
			if ( !AddVsInvocations( vsMain, (int)FFPRenderState.FFPVertexShaderStage.VSTransform + 1 ) )
			{
				return false;
			}

			//Add pixel shader invocations
			if ( !AddPsInvocations( psMain, (int)FFPRenderState.FFPFragmentShaderStage.PSPreProcess + 1 ) )
			{
				return false;
			}

			return true;
		}

		private bool AddVsInvocations( Function vsMain, int groupOrder )
		{
			FunctionInvocation funcInvocation = null;
			int internalCounter = 0;

			funcInvocation = new FunctionInvocation( SGXFuncInstancedViewportsTransform, groupOrder, internalCounter++ );
			funcInvocation.PushOperand( vsInPosition, Operand.OpSemantic.In );
			funcInvocation.PushOperand( worldViewMatrix, Operand.OpSemantic.In );
			funcInvocation.PushOperand( projectionMatrix, Operand.OpSemantic.In );
			funcInvocation.PushOperand( vsInViewportOffsetMatrixR0, Operand.OpSemantic.In );
			funcInvocation.PushOperand( vsInViewportOffsetMatrixR1, Operand.OpSemantic.In );
			funcInvocation.PushOperand( vsInViewportOffsetMatrixR2, Operand.OpSemantic.In );
			funcInvocation.PushOperand( vsInViewportOffsetMatrixR3, Operand.OpSemantic.In );
			funcInvocation.PushOperand( vsInMonitorsCount, Operand.OpSemantic.In );
			funcInvocation.PushOperand( vsInMonitorIndex, Operand.OpSemantic.In );
			funcInvocation.PushOperand( vsOriginalOutPositionProjectiveSpace, Operand.OpSemantic.Out );
			vsMain.AddAtomInstance( funcInvocation );

			//Output position in projective space
			funcInvocation = new FunctionInvocation( FFPRenderState.FFPFuncAssign, groupOrder, internalCounter++ );
			funcInvocation.PushOperand( vsOriginalOutPositionProjectiveSpace, Operand.OpSemantic.In );
			funcInvocation.PushOperand( vsOutPositionProjectiveSpace, Operand.OpSemantic.Out );
			vsMain.AddAtomInstance( funcInvocation );

			//Output monitor index
			funcInvocation = new FunctionInvocation( FFPRenderState.FFPFuncAssign, groupOrder, internalCounter++ );
			funcInvocation.PushOperand( vsInMonitorIndex, Operand.OpSemantic.In );
			funcInvocation.PushOperand( vsOutMonitorIndex, Operand.OpSemantic.Out );
			vsMain.AddAtomInstance( funcInvocation );

			return true;
		}

		private bool AddPsInvocations( Function psMain, int groupOrder )
		{
			FunctionInvocation funcInvocation = null;
			int internalCounter = 0;
			funcInvocation = new FunctionInvocation( SGXFuncInstancedViewportsDiscardOutOfBounds, groupOrder, internalCounter++ );
			funcInvocation.PushOperand( psInMonitorsCount, Operand.OpSemantic.In );
			funcInvocation.PushOperand( psInMonitorIndex, Operand.OpSemantic.In );
			funcInvocation.PushOperand( psInPositionProjectiveSpace, Operand.OpSemantic.In );

			psMain.AddAtomInstance( funcInvocation );

			return true;
		}

		public Vector2 MonitorsCount
		{
			get
			{
				return monitorsCount;
			}
			set
			{
				monitorsCount = value;
				monitorsCountChanged = true;
			}
		}

		public override string Type
		{
			get
			{
				return SGXType;
			}
		}

		public override int ExecutionOrder
		{
			get
			{
				return (int)FFPRenderState.FFPShaderStage.PostProcess + 1;
			}
		}
	}

	internal class InstancedViewportsFactory : SubRenderStateFactory
	{
		public override string Type
		{
			get
			{
				return InstancedViewports.SGXType;
			}
		}

		public override SubRenderState CreateInstance( Scripting.Compiler.ScriptCompiler compiler,
		                                               Scripting.Compiler.AST.PropertyAbstractNode prop, Graphics.Pass pass,
		                                               SGScriptTranslator stranslator )
		{
			SubRenderState subRenderState = CreateInstance();
			return subRenderState;
		}

		public override void WriteInstance( Serialization.MaterialSerializer ser, SubRenderState subRenderState,
		                                    Graphics.Pass srcPass, Graphics.Pass dstPass )
		{
		}

		protected override SubRenderState CreateInstanceImpl()
		{
			return new InstancedViewports();
		}
	}
}