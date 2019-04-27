using System.Collections.Generic;
using Axiom.Graphics;
using Axiom.Math;

namespace Axiom.Components.RTShaderSystem
{
    public class IntegratedPSSM3 : SubRenderState
    {
        private class ShadowTextureParams
        {
            public Real MaxRange;
            public int TextureSamplerIndex;
            public UniformParameter TextureSampler;
            public UniformParameter InvTextureSize;
            public UniformParameter WorldViewProjMatrix;
            public Parameter VSOutLightPosition;
            public Parameter PSInLightPosition;
        }

        public static string SGXType = "SGX_IntegratedPSSM3";
        private static string SGXLibIntegratedPSSM = "SGXLib_IntegratedPSSM";
        private static string SGXFuncComputeShadowColor3 = "SGX_ComputeShadowFactor_PSSM3";
        private static string SGXFuncApplyShadowFactorDiffuse = "SGX_ApplyShadowFactor_Diffuse";
        private static string SGXFuncModulateScalar = "SGX_ModulateScalar";

        private List<ShadowTextureParams> shadowTextureParamsList;
        private UniformParameter psSplitPoints;
        private Parameter vsInPos;
        private Parameter vsOutPos;
        private Parameter vsOutDepth;
        private Parameter psInDepth;
        private Parameter psLocalShadowFactor;
        private Parameter psDiffuse;
        private Parameter psOutDiffuse;
        private Parameter psSpecular;
        private UniformParameter psDerivedSceneColor;

        public IntegratedPSSM3()
        {
        }

        public override void UpdateGpuProgramsParams(Graphics.IRenderable rend, Graphics.Pass pass,
                                                      Graphics.AutoParamDataSource source,
                                                      Core.Collections.LightList lightList)
        {
            int shadowIndex = 0;

            foreach (var it in this.shadowTextureParamsList)
            {
                it.WorldViewProjMatrix.SetGpuParameter(source.GetTextureWorldViewProjMatrix(shadowIndex));
                it.InvTextureSize.SetGpuParameter(source.GetInverseTextureSize(it.TextureSamplerIndex));

                shadowIndex++;
            }

            var splitPoints = new Vector4();

            splitPoints.x = this.shadowTextureParamsList[0].MaxRange;
            splitPoints.y = this.shadowTextureParamsList[1].MaxRange;
            splitPoints.z = 0.0;
            splitPoints.w = 0.0;

            this.psSplitPoints.SetGpuParameter(splitPoints);
        }

        public override bool PreAddToRenderState(TargetRenderState targetRenderState, Graphics.Pass srcPass,
                                                  Graphics.Pass dstPass)
        {
            if (srcPass.LightingEnabled == false || srcPass.Parent.Parent.ReceiveShadows == false)
            {
                return false;
            }

            foreach (var it in this.shadowTextureParamsList)
            {
                TextureUnitState curShadowTexture = dstPass.CreateTextureUnitState();

                //TODO
                //curShadowTexture.ContentType = TextureUnitState.ContentShadow;
                curShadowTexture.SetTextureAddressingMode(TextureAddressing.Border);
                curShadowTexture.TextureBorderColor = Core.ColorEx.White;
                it.TextureSamplerIndex = dstPass.TextureUnitStatesCount - 1;
            }

            return true;
        }

        public void SetSplitPoints(List<Real> newSplitPoints)
        {
            if (newSplitPoints.Count != 4)
            {
                throw new Core.AxiomException("IntegratedPSSM3 sub render state supports only 4 split points");
            }

            for (int i = 1; i < newSplitPoints.Count; i++)
            {
                ShadowTextureParams curParams = this.shadowTextureParamsList[i - 1];
                curParams.MaxRange = newSplitPoints[i];
            }
        }

        protected override bool ResolveParameters(ProgramSet programSet)
        {
            Program vsProgram = programSet.CpuVertexProgram;
            Program psProgram = programSet.CpuFragmentProgram;
            Function vsMain = vsProgram.EntryPointFunction;
            Function psMain = psProgram.EntryPointFunction;

            //Get input position parameter
            this.vsInPos = Function.GetParameterBySemantic(vsMain.InputParameters, Parameter.SemanticType.Position, 0);
            if (this.vsInPos == null)
            {
                return false;
            }

            //Get output position parameter
            this.vsOutPos = Function.GetParameterBySemantic(vsMain.OutputParameters, Parameter.SemanticType.Position, 0);
            if (this.vsOutPos == null)
            {
                return false;
            }

            //Resolve vertex shader output depth.
            this.vsOutDepth = vsMain.ResolveOutputParameter(Parameter.SemanticType.TextureCoordinates, -1,
                                                             Parameter.ContentType.DepthViewSpace,
                                                             GpuProgramParameters.GpuConstantType.Float1);
            if (this.vsOutDepth == null)
            {
                return false;
            }

            //Resolve input depth parameter.
            this.psInDepth = psMain.ResolveInputParameter(Parameter.SemanticType.TextureCoordinates, this.vsOutDepth.Index,
                                                           this.vsOutDepth.Content, GpuProgramParameters.GpuConstantType.Float1);
            if (this.psInDepth == null)
            {
                return false;
            }

            //Get in/local specular paramter
            this.psSpecular = Function.GetParameterBySemantic(psMain.InputParameters, Parameter.SemanticType.Color, 1);
            if (this.psSpecular == null)
            {
                this.psSpecular = Function.GetParameterBySemantic(psMain.LocalParameters, Parameter.SemanticType.Color, 1);
                if (this.psSpecular == null)
                {
                    return false;
                }
            }
            //Resolve computed local shadow color parameter.
            this.psLocalShadowFactor = psMain.ResolveLocalParameter(Parameter.SemanticType.Unknown, 0, "lShadowFactor",
                                                                     GpuProgramParameters.GpuConstantType.Float1);
            if (this.psLocalShadowFactor == null)
            {
                return false;
            }

            //Resolve computed local shadow color parameter
            this.psSplitPoints = psProgram.ResolveParameter(GpuProgramParameters.GpuConstantType.Float4, -1,
                                                             GpuProgramParameters.GpuParamVariability.Global,
                                                             "pssm_split_points");
            if (this.psSplitPoints == null)
            {
                return false;
            }

            //Get derived scene color
            this.psDerivedSceneColor =
                psProgram.ResolveAutoParameterInt(GpuProgramParameters.AutoConstantType.DerivedSceneColor, 0);
            if (this.psDerivedSceneColor == null)
            {
                return false;
            }

            int lightIndex = 0;

            foreach (var it in this.shadowTextureParamsList)
            {
                it.WorldViewProjMatrix = vsProgram.ResolveParameter(GpuProgramParameters.GpuConstantType.Matrix_4X4, -1,
                                                                     GpuProgramParameters.GpuParamVariability.PerObject,
                                                                     "world_texture_view_proj");
                if (it.WorldViewProjMatrix == null)
                {
                    return false;
                }

                Parameter.ContentType lightSpace = Parameter.ContentType.PositionLightSpace0;

                switch (lightIndex)
                {
                    case 1:
                        lightSpace = Parameter.ContentType.PositionLightSpace1;
                        break;
                    case 2:
                        lightSpace = Parameter.ContentType.PositionLightSpace2;
                        break;
                    case 3:
                        lightSpace = Parameter.ContentType.PositionLightSpace3;
                        break;
                    case 4:
                        lightSpace = Parameter.ContentType.PositionLightSpace4;
                        break;
                    case 5:
                        lightSpace = Parameter.ContentType.PositionLightSpace5;
                        break;
                    case 6:
                        lightSpace = Parameter.ContentType.PositionLightSpace6;
                        break;
                    case 7:
                        lightSpace = Parameter.ContentType.PositionLightSpace7;
                        break;
                }

                it.VSOutLightPosition = vsMain.ResolveOutputParameter(Parameter.SemanticType.TextureCoordinates, -1,
                                                                       lightSpace,
                                                                       GpuProgramParameters.GpuConstantType.Float4);
                if (it.VSOutLightPosition == null)
                {
                    return false;
                }

                it.PSInLightPosition = psMain.ResolveInputParameter(Parameter.SemanticType.TextureCoordinates,
                                                                     it.VSOutLightPosition.Index,
                                                                     it.VSOutLightPosition.Content,
                                                                     GpuProgramParameters.GpuConstantType.Float4);
                if (it.PSInLightPosition == null)
                {
                    return false;
                }

                it.TextureSampler = psProgram.ResolveParameter(GpuProgramParameters.GpuConstantType.Sampler2D,
                                                                it.TextureSamplerIndex,
                                                                GpuProgramParameters.GpuParamVariability.Global,
                                                                "shadow_map");
                if (it.TextureSampler == null)
                {
                    return false;
                }

                it.InvTextureSize = psProgram.ResolveParameter(GpuProgramParameters.GpuConstantType.Float4, -1,
                                                                GpuProgramParameters.GpuParamVariability.Global,
                                                                "inv_shadow_texture_size");
                if (it.InvTextureSize == null)
                {
                    return false;
                }

                lightIndex++;
            }

            return true;
        }

        protected override bool ResolveDependencies(ProgramSet programSet)
        {
            Program vsProgram = programSet.CpuVertexProgram;
            Program psProgram = programSet.CpuFragmentProgram;

            vsProgram.AddDependency(FFPRenderState.FFPLibCommon);
            vsProgram.AddDependency(FFPRenderState.FFPLibCommon);
            vsProgram.AddDependency(SGXLibIntegratedPSSM);
            return true;
        }

        protected override bool AddFunctionInvocations(ProgramSet programSet)
        {
            Program vsProgram = programSet.CpuVertexProgram;
            Program psProgram = programSet.CpuFragmentProgram;
            Function vsMain = vsProgram.EntryPointFunction;
            int internalCounter = 0;

            if (
                AddVSInvocation(vsMain, (int)FFPRenderState.FFPVertexShaderStage.VSTexturing + 1, ref internalCounter) ==
                false)
            {
                return false;
            }

            internalCounter = 0;
            if (
                !AddPSInvocation(psProgram, (int)FFPRenderState.FFPFragmentShaderStage.PSColorBegin + 2,
                                  ref internalCounter))
            {
                return false;
            }

            return true;
        }

        private bool AddVSInvocation(Function vsMain, int groupOrder, ref int internalCounter)
        {
            FunctionInvocation curFuncInvocation;

            //Output the vertex depth in camera space
            curFuncInvocation = new FunctionInvocation(FFPRenderState.FFPFuncAssign, groupOrder, internalCounter++);
            curFuncInvocation.PushOperand(this.vsOutPos, Operand.OpSemantic.In, Operand.OpMask.Z);
            curFuncInvocation.PushOperand(this.vsOutDepth, Operand.OpSemantic.Out);
            vsMain.AddAtomInstance(curFuncInvocation);

            //compute world space position
            foreach (var it in this.shadowTextureParamsList)
            {
                curFuncInvocation = new FunctionInvocation(FFPRenderState.FFPFuncTransform, groupOrder,
                                                            internalCounter++);
                curFuncInvocation.PushOperand(it.WorldViewProjMatrix, Operand.OpSemantic.In);
                curFuncInvocation.PushOperand(this.vsInPos, Operand.OpSemantic.In);
                curFuncInvocation.PushOperand(it.VSOutLightPosition, Operand.OpSemantic.Out);
                vsMain.AddAtomInstance(curFuncInvocation);
            }
            return true;
        }

        private bool AddPSInvocation(Program psProgram, int groupOrder, ref int internalCounter)
        {
            Function psMain = psProgram.EntryPointFunction;
            FunctionInvocation curFuncInvocation = null;

            ShadowTextureParams splitParams0 = this.shadowTextureParamsList[0];
            ShadowTextureParams splitParams1 = this.shadowTextureParamsList[1];
            ShadowTextureParams splitParams2 = this.shadowTextureParamsList[2];

            //Compute shadow factor
            curFuncInvocation = new FunctionInvocation(SGXFuncComputeShadowColor3, groupOrder, internalCounter++);
            curFuncInvocation.PushOperand(this.psInDepth, Operand.OpSemantic.In);
            curFuncInvocation.PushOperand(this.psSplitPoints, Operand.OpSemantic.In);
            curFuncInvocation.PushOperand(splitParams0.PSInLightPosition, Operand.OpSemantic.In);
            curFuncInvocation.PushOperand(splitParams1.PSInLightPosition, Operand.OpSemantic.In);
            curFuncInvocation.PushOperand(splitParams2.PSInLightPosition, Operand.OpSemantic.In);
            curFuncInvocation.PushOperand(splitParams0.TextureSampler, Operand.OpSemantic.In);
            curFuncInvocation.PushOperand(splitParams1.TextureSampler, Operand.OpSemantic.In);
            curFuncInvocation.PushOperand(splitParams2.TextureSampler, Operand.OpSemantic.In);
            curFuncInvocation.PushOperand(splitParams0.InvTextureSize, Operand.OpSemantic.In);
            curFuncInvocation.PushOperand(splitParams1.InvTextureSize, Operand.OpSemantic.In);
            curFuncInvocation.PushOperand(splitParams2.InvTextureSize, Operand.OpSemantic.In);
            curFuncInvocation.PushOperand(this.psLocalShadowFactor, Operand.OpSemantic.Out);
            psMain.AddAtomInstance(curFuncInvocation);

            //Apply shadow factor on diffuse color
            curFuncInvocation = new FunctionInvocation(SGXFuncApplyShadowFactorDiffuse, groupOrder, internalCounter++);
            curFuncInvocation.PushOperand(this.psDerivedSceneColor, Operand.OpSemantic.In);
            curFuncInvocation.PushOperand(this.psDiffuse, Operand.OpSemantic.In);
            curFuncInvocation.PushOperand(this.psLocalShadowFactor, Operand.OpSemantic.In);
            curFuncInvocation.PushOperand(this.psLocalShadowFactor, Operand.OpSemantic.Out);
            psMain.AddAtomInstance(curFuncInvocation);

            //Apply shadow factor on specular color
            curFuncInvocation = new FunctionInvocation(SGXFuncModulateScalar, groupOrder, internalCounter++);
            curFuncInvocation.PushOperand(this.psLocalShadowFactor, Operand.OpSemantic.In);
            curFuncInvocation.PushOperand(this.psSpecular, Operand.OpSemantic.In);
            curFuncInvocation.PushOperand(this.psSpecular, Operand.OpSemantic.Out);
            psMain.AddAtomInstance(curFuncInvocation);

            //Assign the local diffuse to output diffuse
            curFuncInvocation = new FunctionInvocation(FFPRenderState.FFPFuncAssign, groupOrder, internalCounter++);
            curFuncInvocation.PushOperand(this.psDiffuse, Operand.OpSemantic.In);
            curFuncInvocation.PushOperand(this.psOutDiffuse, Operand.OpSemantic.Out);
            psMain.AddAtomInstance(curFuncInvocation);

            return true;
        }

        public override string Type
        {
            get
            {
                return IntegratedPSSM3.SGXType;
            }
        }

        public override int ExecutionOrder
        {
            get
            {
                return (int)FFPRenderState.FFPShaderStage.Texturing + 1;
            }
        }
    }
}