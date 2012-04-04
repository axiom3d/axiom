using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Axiom.Components.RTShaderSystem
{
    class FFPTransform : SubRenderState
    {
        public static string FFPType = "FFP_Transform";

        public override string Type
        {
            get
            {
                return FFPTransform.FFPType;
            }
           
        }
        public override int ExecutionOrder
        {
            get
            {
                return (int)FFPRenderState.FFPShaderStage.Transform;
            }
        }
        internal override bool CreateCpuSubPrograms(ProgramSet programSet)
        {
            Program vsProgram = programSet.CpuVertexProgram;

            //Resolve world view proj matrix
            UniformParameter wvpMatrix = vsProgram.ResolveAutoParameterInt(Graphics.GpuProgramParameters.AutoConstantType.WorldViewProjMatrix, 0);
            if (wvpMatrix == null)
                return false;

            Function vsEntry = vsProgram.EntryPointFunction;

            //Resolve input position parameter
            Parameter positionIn = vsEntry.ResolveInputParameter(Parameter.SemanticType.Position, 0, Parameter.ContentType.PositionObjectSpace, Graphics.GpuProgramParameters.GpuConstantType.Float4);
            if (positionIn == null)
                return false;

            //Resolve output position parameter
            Parameter positionOut = vsEntry.ResolveOutputParameter(Parameter.SemanticType.Position, 0, Parameter.ContentType.PositionProjectiveSpace, Graphics.GpuProgramParameters.GpuConstantType.Float4);
            if (positionOut == null)
                return false;

            //Add dependency
            vsProgram.AddDependency(FFPRenderState.FFPLibTransform);

            FunctionInvocation transformFunc = new FunctionInvocation(FFPRenderState.FFPFuncTransform, -1, 0);
            transformFunc.PushOperand(wvpMatrix, Operand.OpSemantic.In);
            transformFunc.PushOperand(positionIn, Operand.OpSemantic.In);
            transformFunc.PushOperand(positionOut, Operand.OpSemantic.Out);

            vsEntry.AddAtomInstance(transformFunc);

            return true;
        }
    }
}
