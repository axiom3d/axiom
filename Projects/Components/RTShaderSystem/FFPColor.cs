using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        int resolveStageFlags;
        Parameter vsInputDiffuse;
        Parameter vsInputSpecular;
        Parameter vsOutputDiffuse;
        Parameter vsOutputSpecular;
        Parameter psInputDiffuse;
        Parameter psInputSpecular;
        Parameter psOutputDiffuse;
        Parameter psOutputSpecular;

        public FFPColor()
        {
            resolveStageFlags = (int)StageFlags.PsOutputDiffuse;
        }
       
        public void AddResolveStageMask(int mask)
        {
            resolveStageFlags |= mask;
        }
        public void RemoveResolveStageMask(int mask)
        {
            resolveStageFlags &= ~mask;
        }

        public override bool PreAddToRenderState(TargetRenderState targetRenderState, Graphics.Pass srcPass, Graphics.Pass dstPass)
        {
            TrackVertexColor trackColor = srcPass.VertexColorTracking;

            if (trackColor != null)
            {
                AddResolveStageMask((int)StageFlags.VsInputDiffuse);
            }

            return true;
        }
        internal override bool ResolveParameters(ProgramSet programSet)
        {
            Program vsProgram = programSet.CpuVertexProgram;
            Program psProgram = programSet.CpuFragmentProgram;
            Function vsMain = vsProgram.EntryPointFunction;
            Function psMain = psProgram.EntryPointFunction;

            bool success = (resolveStageFlags & (int)StageFlags.VsInputDiffuse) == 1;
            if (success)
            {
                vsInputDiffuse = vsMain.ResolveInputParameter(Parameter.SemanticType.Color, 0, Parameter.ContentType.ColorDiffuse, Graphics.GpuProgramParameters.GpuConstantType.Float4);
            }
            success = (resolveStageFlags & (int)StageFlags.VsInputSpecular) == 1;
            if (success)
            {
                vsInputSpecular = vsMain.ResolveInputParameter(Parameter.SemanticType.Color, 1, Parameter.ContentType.ColorSpecular, Graphics.GpuProgramParameters.GpuConstantType.Float4);
            }

            //Resolve VS color outputs if have inputs from vertex stream
            if (vsInputDiffuse != null || (resolveStageFlags & (int)StageFlags.VsOutputdiffuse) == 1)
                vsOutputDiffuse = vsMain.ResolveOutputParameter(Parameter.SemanticType.Color, 0, Parameter.ContentType.ColorDiffuse, Graphics.GpuProgramParameters.GpuConstantType.Float4);

            if (vsInputSpecular != null || (resolveStageFlags & (int)StageFlags.VsOutputSpecular) == 1)
                vsOutputSpecular = vsMain.ResolveOutputParameter(Parameter.SemanticType.Color, 1, Parameter.ContentType.ColorSpecular, Graphics.GpuProgramParameters.GpuConstantType.Float4);

            //Resolve PS color inputs if we have inputs from vertex shader.
            if (vsOutputDiffuse != null || (resolveStageFlags & (int)StageFlags.PsInputDiffuse) == 1)
                psInputDiffuse = psMain.ResolveInputParameter(Parameter.SemanticType.Color, 0, Parameter.ContentType.ColorDiffuse, Graphics.GpuProgramParameters.GpuConstantType.Float4);

            if(vsOutputSpecular != null || (resolveStageFlags & (int)StageFlags.PsInputSpecular) == 1)
                psInputDiffuse = psMain.ResolveInputParameter(Parameter.SemanticType.Color, 1, Parameter.ContentType.ColorSpecular, Graphics.GpuProgramParameters.GpuConstantType.Float4);

            //Resolve PS output diffuse color
            if((resolveStageFlags & (int)StageFlags.PsOutputDiffuse) == 1)
            {
                psOutputDiffuse = psMain.ResolveOutputParameter(Parameter.SemanticType.Color, 0, Parameter.ContentType.ColorDiffuse, Graphics.GpuProgramParameters.GpuConstantType.Float4);
                if(psOutputDiffuse == null)
                    return false;
            }

            //Resolve PS output specular color
            if((resolveStageFlags & (int)StageFlags.PsOutputSpecular) == 1)
            {
                psOutputSpecular = psMain.ResolveOutputParameter(Parameter.SemanticType.Color, 1, Parameter.ContentType.ColorSpecular, Graphics.GpuProgramParameters.GpuConstantType.Float4);
                if(psOutputSpecular == null)
                    return false;
            }

            return true;
        }
        internal override bool ResolveDependencies(ProgramSet programSet)
        {
            Program vsProgram = programSet.CpuVertexProgram;
            Program psProgram = programSet.CpuFragmentProgram;
            vsProgram.AddDependency(FFPRenderState.FFPLibCommon);
            psProgram.AddDependency(FFPRenderState.FFPLibCommon);

            return true;
        }
        internal override bool AddFunctionInvocations(ProgramSet programSet)
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
            if (vsInputDiffuse != null)
            {
                vsDiffuse = vsInputDiffuse;
            }
            else
            {
                vsDiffuse = vsMain.ResolveLocalParameter(Parameter.SemanticType.Color, 0, Parameter.ContentType.ColorDiffuse, Graphics.GpuProgramParameters.GpuConstantType.Float4);
                curFuncInvocation = new FunctionInvocation(FFPRenderState.FFPFuncConstruct, (int)FFPRenderState.FFPVertexShaderStage.VSColor, internalCounter++);
                curFuncInvocation.PushOperand(ParameterFactory.CreateConstParamFloat(1.0f), Operand.OpSemantic.In);
                curFuncInvocation.PushOperand(ParameterFactory.CreateConstParamFloat(1.0f), Operand.OpSemantic.In);
                curFuncInvocation.PushOperand(ParameterFactory.CreateConstParamFloat(1.0f), Operand.OpSemantic.In);
                curFuncInvocation.PushOperand(ParameterFactory.CreateConstParamFloat(1.0f), Operand.OpSemantic.In);
                curFuncInvocation.PushOperand(vsDiffuse, Operand.OpSemantic.Out);
                vsMain.AddAtomInstance(curFuncInvocation);
            }

            if (vsOutputDiffuse != null)
            {
                curFuncInvocation = new FunctionInvocation(FFPRenderState.FFPFuncAssign, (int)FFPRenderState.FFPVertexShaderStage.VSColor, internalCounter++);
                curFuncInvocation.PushOperand(vsDiffuse, Operand.OpSemantic.In);
                curFuncInvocation.PushOperand(vsOutputDiffuse, Operand.OpSemantic.Out);
                vsMain.AddAtomInstance(curFuncInvocation);
            }

            if (vsInputSpecular != null)
            {
                vsSpecular = vsInputSpecular;
            }
            else 
            {
                vsSpecular = vsMain.ResolveLocalParameter(Parameter.SemanticType.Color, 1, Parameter.ContentType.ColorSpecular, Graphics.GpuProgramParameters.GpuConstantType.Float4);
                curFuncInvocation = new FunctionInvocation(FFPRenderState.FFPFuncConstruct, (int)FFPRenderState.FFPVertexShaderStage.VSColor, internalCounter++);
                curFuncInvocation.PushOperand(ParameterFactory.CreateConstParamFloat(0.0f), Operand.OpSemantic.In);
                curFuncInvocation.PushOperand(ParameterFactory.CreateConstParamFloat(0.0f), Operand.OpSemantic.In);
                curFuncInvocation.PushOperand(ParameterFactory.CreateConstParamFloat(0.0f), Operand.OpSemantic.In);
                curFuncInvocation.PushOperand(ParameterFactory.CreateConstParamFloat(0.0f), Operand.OpSemantic.In);
                curFuncInvocation.PushOperand(vsSpecular, Operand.OpSemantic.Out);
                vsMain.AddAtomInstance(curFuncInvocation);
            }

            if (vsOutputSpecular != null)
            {
                curFuncInvocation = new FunctionInvocation(FFPRenderState.FFPFuncAssign, (int)FFPRenderState.FFPVertexShaderStage.VSColor, internalCounter++);
                curFuncInvocation.PushOperand(vsSpecular, Operand.OpSemantic.In);
                curFuncInvocation.PushOperand(vsOutputSpecular, Operand.OpSemantic.Out);
                vsMain.AddAtomInstance(curFuncInvocation);
            }

            //Create fragment shader color invocations
            Parameter psDiffuse, psSpecular;
            internalCounter = 0;

            //Handle diffuse color
            if (psInputDiffuse != null)
            {
                psDiffuse = psInputDiffuse;
            }
            else
            {
                psDiffuse = psMain.ResolveLocalParameter(Parameter.SemanticType.Color, 0, Parameter.ContentType.ColorDiffuse, Graphics.GpuProgramParameters.GpuConstantType.Float4);
                curFuncInvocation = new FunctionInvocation(FFPRenderState.FFPFuncConstruct, (int)FFPRenderState.FFPFragmentShaderStage.PSColorBegin, internalCounter++);
                curFuncInvocation.PushOperand(ParameterFactory.CreateConstParamFloat(1.0f), Operand.OpSemantic.In);
                curFuncInvocation.PushOperand(ParameterFactory.CreateConstParamFloat(1.0f), Operand.OpSemantic.In);
                curFuncInvocation.PushOperand(ParameterFactory.CreateConstParamFloat(1.0f), Operand.OpSemantic.In);
                curFuncInvocation.PushOperand(ParameterFactory.CreateConstParamFloat(1.0f), Operand.OpSemantic.In);
                curFuncInvocation.PushOperand(psDiffuse, Operand.OpSemantic.Out);
                psMain.AddAtomInstance(curFuncInvocation);
            }

            //Handle specular color
            if (psInputSpecular != null)
            {
                psSpecular = psInputSpecular;
            }
            else
            {
                psSpecular = psMain.ResolveLocalParameter(Parameter.SemanticType.Color, 1, Parameter.ContentType.ColorSpecular, Graphics.GpuProgramParameters.GpuConstantType.Float4);
                curFuncInvocation = new FunctionInvocation(FFPRenderState.FFPFuncConstruct, (int)FFPRenderState.FFPFragmentShaderStage.PSColorBegin, internalCounter++);
                curFuncInvocation.PushOperand(ParameterFactory.CreateConstParamFloat(0.0f), Operand.OpSemantic.In);
                curFuncInvocation.PushOperand(ParameterFactory.CreateConstParamFloat(0.0f), Operand.OpSemantic.In);
                curFuncInvocation.PushOperand(ParameterFactory.CreateConstParamFloat(0.0f), Operand.OpSemantic.In);
                curFuncInvocation.PushOperand(ParameterFactory.CreateConstParamFloat(0.0f), Operand.OpSemantic.In);
                curFuncInvocation.PushOperand(psSpecular, Operand.OpSemantic.Out);
                psMain.AddAtomInstance(curFuncInvocation);
            }

            //Assign diffuse color
            if (psOutputDiffuse != null)
            {
                curFuncInvocation = new FunctionInvocation(FFPRenderState.FFPFuncAssign, (int)FFPRenderState.FFPFragmentShaderStage.PSColorBegin, internalCounter++);
                curFuncInvocation.PushOperand(psDiffuse, Operand.OpSemantic.In);
                curFuncInvocation.PushOperand(psOutputDiffuse, Operand.OpSemantic.Out);
                psMain.AddAtomInstance(curFuncInvocation);
            }

            //Assign specular color
            if (psOutputSpecular != null)
            {
                curFuncInvocation = new FunctionInvocation(FFPRenderState.FFPFuncAssign, (int)FFPRenderState.FFPFragmentShaderStage.PSColorBegin, internalCounter++);
                curFuncInvocation.PushOperand(psSpecular, Operand.OpSemantic.In);
                curFuncInvocation.PushOperand(psOutputSpecular, Operand.OpSemantic.Out);
                psMain.AddAtomInstance(curFuncInvocation);
            }

            //Add specular to out color
            internalCounter = 0;
            if (psOutputDiffuse != null && psSpecular != null)
            {
                curFuncInvocation = new FunctionInvocation(FFPRenderState.FFPFuncAdd, (int)FFPRenderState.FFPFragmentShaderStage.PSColorEnd, internalCounter++);
                curFuncInvocation.PushOperand(psOutputDiffuse, Operand.OpSemantic.In, (Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z));
                curFuncInvocation.PushOperand(psSpecular, Operand.OpSemantic.In, (Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z));
                curFuncInvocation.PushOperand(psOutputDiffuse, Operand.OpSemantic.Out, (Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z));
                psMain.AddAtomInstance(curFuncInvocation);
            }

            return true;
        }
        
        /// <summary>
        /// Gets/Sets the resolve stage flags that this sub render state will produce.
        /// I.E. - If one want to specify that the vertex shader program needs to get a diffuse component
        /// and the pixel shader should output diffuse component he should pass VsInputDiffuse | PsOutputdiffuse
        /// </summary>
        public int ResolveStageFlags
        {
            get { return resolveStageFlags; }
            set { resolveStageFlags = value; }
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
