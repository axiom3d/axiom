using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Axiom.Graphics;
using Axiom.Core;
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
        CalcMode calcMode;
        FogMode fogMode;
        ColorEx fogColorValue;
        Vector4 fogParamsValue;
        bool passOverrideParams;

        UniformParameter worldViewProjMatrix;
        UniformParameter fogColor;
        UniformParameter fogParams;
        Parameter vsInPos;
        Parameter vsOutFogFactor;
        Parameter psInFogFactor;
        Parameter vsOutDepth;
        Parameter psInDepth;
        Parameter psOutDiffuse;

        public FFPFog()
        {
            fogMode = FogMode.None;
            calcMode = FFPFog.CalcMode.PerVertex;
            passOverrideParams = false;
        }

        public override void UpdateGpuProgramsParams(IRenderable rend, Pass pass, AutoParamDataSource source, Core.Collections.LightList lightList)
        {
            if (fogMode == FogMode.None)
                return;

            FogMode fMode;
            ColorEx newFogColor;
            Real newFogStart, newFogEnd, newFogDensity;

            if (passOverrideParams)
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

            SetFogProperties(fMode, newFogColor, newFogStart, newFogEnd, newFogDensity);

            //Per pixel fog
            if (calcMode == CalcMode.PerPixel)
            {
                fogParams.SetGpuParameter(fogParamsValue);
            }

            //per vertex fog
            else
            {
                fogParams.SetGpuParameter(fogParamsValue);
            }

            fogColor.SetGpuParameter(fogColorValue);
        }
        public override bool PreAddToRenderState(TargetRenderState targetRenderState, Pass srcPass, Pass dstPass)
        {
            FogMode fMode;
            ColorEx newFogColor;
            Real newFogStart, newFogEnd, newFogDensity;

            if (srcPass.FogOverride)
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

                if (sceneMgr == null)
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

                passOverrideParams = false;
            }
            //Set fog properties
            SetFogProperties(fMode, newFogColor, newFogStart, newFogEnd, newFogDensity);

            //Override scene fog since it will happen in shader
            dstPass.SetFog(true, FogMode.None, newFogColor, newFogDensity, newFogStart, newFogEnd);
            return true;
        }
        public void SetFogProperties(FogMode fogMode, ColorEx fogColor, float fogStart, float fogEnd, float fogDensity)
        {
            this.fogMode = fogMode;
            this.fogColorValue = fogColor;
            this.fogParamsValue = new Vector4(fogDensity, fogStart, fogEnd, fogEnd != fogStart ? 1 / (fogEnd - fogStart) : 0);

        }


        internal override bool ResolveParameters(ProgramSet programSet)
        {
            if (fogMode == FogMode.None)
                return true;

            Program vsProgram = programSet.CpuVertexProgram;
            Program psProgram = programSet.CpuFragmentProgram;
            Function vsMain = vsProgram.EntryPointFunction;
            Function psMain = psProgram.EntryPointFunction;

            //Resolve world view matrix.
            worldViewProjMatrix = vsProgram.ResolveAutoParameterInt(GpuProgramParameters.AutoConstantType.WorldViewProjMatrix, 0);
            if (worldViewProjMatrix == null)
                return false;

            //Resolve vertex shader input position
            vsInPos = vsMain.ResolveInputParameter(Parameter.SemanticType.Position, 0, Parameter.ContentType.PositionObjectSpace, GpuProgramParameters.GpuConstantType.Float4);
            if (vsInPos == null)
                return false;

            //Resolve fog color
            fogColor = psProgram.ResolveParameter(GpuProgramParameters.GpuConstantType.Float4, -1, GpuProgramParameters.GpuParamVariability.Global, "gFogColor");
            if (fogColor == null)
                return false;

            //Resolve pixel shader output diffuse color
            psOutDiffuse = psMain.ResolveOutputParameter(Parameter.SemanticType.Color, 0, Parameter.ContentType.ColorDiffuse, GpuProgramParameters.GpuConstantType.Float4);
            if (psOutDiffuse == null)
                return false;

            //Per pixel fog
            if (calcMode == CalcMode.PerPixel)
            {
                //Resolve fog params
                fogParams = psProgram.ResolveParameter(GpuProgramParameters.GpuConstantType.Float4, -1, GpuProgramParameters.GpuParamVariability.Global, "gFogParams");
                if (fogParams == null)
                    return false;

                //Resolve vertex shader output depth
                vsOutDepth = vsMain.ResolveOutputParameter(Parameter.SemanticType.TextureCoordinates, -1, Parameter.ContentType.DepthViewSpace, GpuProgramParameters.GpuConstantType.Float1);
                if (vsOutDepth == null)
                    return false;

                //Resolve pixel shader input depth.
                psInDepth = psMain.ResolveInputParameter(Parameter.SemanticType.TextureCoordinates, vsOutDepth.Index, vsOutDepth.Content, GpuProgramParameters.GpuConstantType.Float1);
                if (psInDepth == null)
                    return false;
            }
                //per vertex fog
            else
            {
                fogParams = vsProgram.ResolveParameter(GpuProgramParameters.GpuConstantType.Float4, -1, GpuProgramParameters.GpuParamVariability.Global, "gFogParams");
                if (fogParams == null)
                    return false;

                //Resolve vertex shader output fog factor
                vsOutFogFactor = vsMain.ResolveOutputParameter(Parameter.SemanticType.TextureCoordinates, -1, Parameter.ContentType.Unknown, GpuProgramParameters.GpuConstantType.Float1);
                if (vsOutFogFactor == null)
                    return false;

                //Resolve pixel shader input fog factor
                psInFogFactor = psMain.ResolveInputParameter(Parameter.SemanticType.TextureCoordinates, vsOutFogFactor.Index, vsOutFogFactor.Content, GpuProgramParameters.GpuConstantType.Float1);
                if (psInFogFactor == null)
                    return false;
            }


            return true;
        }
        internal override bool ResolveDependencies(ProgramSet programSet)
        {
            if (fogMode == FogMode.None)
                return true;

            Program vsProgram = programSet.CpuVertexProgram;
            Program psProgram = programSet.CpuFragmentProgram;

            vsProgram.AddDependency(FFPRenderState.FFPLibFog);
            psProgram.AddDependency(FFPRenderState.FFPLibCommon);
            //Per pixel fog.
            if (calcMode == CalcMode.PerPixel)
            {
                psProgram.AddDependency(FFPRenderState.FFPLibFog);
            }

            return true;
        }
        internal override bool AddFunctionInvocations(ProgramSet programSet)
        {
            if (fogMode == FogMode.None)
                return true;

            Program vsProgram = programSet.CpuVertexProgram;
            Program psProgram = programSet.CpuFragmentProgram;
            Function vsMain = vsProgram.EntryPointFunction;
            Function psMain = psProgram.EntryPointFunction;
            FunctionInvocation curFuncInvocation = null;
            int internalCounter = 0;

            //Per pixel fog
            if (calcMode == CalcMode.PerPixel)
            {
                curFuncInvocation = new FunctionInvocation(FFPRenderState.FFPFuncPixelFogDepth, (int)FFPRenderState.FFPVertexShaderStage.VSFog, internalCounter++);
                curFuncInvocation.PushOperand(worldViewProjMatrix, Operand.OpSemantic.In);
                curFuncInvocation.PushOperand(vsInPos, Operand.OpSemantic.In);
                curFuncInvocation.PushOperand(vsOutDepth, Operand.OpSemantic.Out);
                vsMain.AddAtomInstance(curFuncInvocation);

                internalCounter = 0;
                switch (fogMode)
                {
                    case FogMode.Exp:
                        curFuncInvocation = new FunctionInvocation(FFPRenderState.FFPFuncPixelFogLinear, (int)FFPRenderState.FFPFragmentShaderStage.PSFog, internalCounter++);
                        break;
                    case FogMode.Exp2:
                        curFuncInvocation = new FunctionInvocation(FFPRenderState.FFPFuncPixelFogExp, (int)FFPRenderState.FFPFragmentShaderStage.PSFog, internalCounter++);
                        break;
                    case FogMode.Linear:
                        curFuncInvocation = new FunctionInvocation(FFPRenderState.FFPFuncPixelFogExp2, (int)FFPRenderState.FFPFragmentShaderStage.PSFog, internalCounter++);
                        break;
                    default:
                        break;
                }

                curFuncInvocation.PushOperand(psInDepth, Operand.OpSemantic.In);
                curFuncInvocation.PushOperand(fogParams, Operand.OpSemantic.In);
                curFuncInvocation.PushOperand(fogColor, Operand.OpSemantic.In);
                curFuncInvocation.PushOperand(psOutDiffuse, Operand.OpSemantic.In);
                curFuncInvocation.PushOperand(psOutDiffuse, Operand.OpSemantic.Out);
                psMain.AddAtomInstance(curFuncInvocation);
            }
            else //Per vertex fog
            {
                internalCounter = 0;
                switch (fogMode)
                {
                    case FogMode.Exp:
                        curFuncInvocation = new FunctionInvocation(FFPRenderState.FFPFuncVertexFogLinear, (int)FFPRenderState.FFPVertexShaderStage.VSFog, internalCounter++);
                        break;
                    case FogMode.Exp2:
                        curFuncInvocation = new FunctionInvocation(FFPRenderState.FFPFuncVertexFogExp, (int)FFPRenderState.FFPVertexShaderStage.VSFog, internalCounter++);
                        break;
                    case FogMode.Linear:
                        curFuncInvocation = new FunctionInvocation(FFPRenderState.FFPFuncVertexFogExp2, (int)FFPRenderState.FFPVertexShaderStage.VSFog, internalCounter++);
                        break;
                    default:
                        break;
                }

                curFuncInvocation.PushOperand(worldViewProjMatrix, Operand.OpSemantic.In);
                curFuncInvocation.PushOperand(vsInPos, Operand.OpSemantic.In);
                curFuncInvocation.PushOperand(fogParams, Operand.OpSemantic.In);
                curFuncInvocation.PushOperand(vsOutFogFactor, Operand.OpSemantic.Out);
                vsMain.AddAtomInstance(curFuncInvocation);

                internalCounter = 0;

                curFuncInvocation = new FunctionInvocation(FFPRenderState.FFPFuncLerp, (int)FFPRenderState.FFPFragmentShaderStage.PSFog, internalCounter++);
                curFuncInvocation.PushOperand(fogColor, Operand.OpSemantic.In);
                curFuncInvocation.PushOperand(psOutDiffuse, Operand.OpSemantic.In);
                curFuncInvocation.PushOperand(psInFogFactor, Operand.OpSemantic.In);
                curFuncInvocation.PushOperand(psOutDiffuse, Operand.OpSemantic.Out);
                psMain.AddAtomInstance(curFuncInvocation);
            }



            return true;
        }
        public CalcMode CalculationMode
        {
            get { return calcMode; }
            set { calcMode = value; }
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
