using System.Collections.Generic;
using Axiom.Core;
using Axiom.Graphics;
using Axiom.Math;

namespace Axiom.Components.RTShaderSystem
{
    public class FFPTexturing : SubRenderState
    {
        public class TextureUnitParams
        {
            public TextureUnitState TextureUnitState;
            public Frustum TextureProjector;
            public int TextureSamplerIndex;
            public GpuProgramParameters.GpuConstantType TextureSamplerType;
            public GpuProgramParameters.GpuConstantType VSInTextureCoordinateType;
            public GpuProgramParameters.GpuConstantType VSOutTextureCoordinateType;
            public TexCoordCalcMethod TexCoordCalcMethod;
            public UniformParameter TextureMatrix;
            public UniformParameter TextureViewProjImageMatrix;
            public UniformParameter TextureSampler;
            public Parameter VSInputTexCoord;
            public Parameter VSOutputTexCoord;
            public Parameter PSInputTexCoord;
        }

        public static string FFPType = "FFP_Texturing";
        private List<TextureUnitParams> textureUnitParamsList;
        private UniformParameter worldMatrix;
        private UniformParameter worldITMatrix;
        private UniformParameter viewMatrix;
        private Parameter vsInputNormal;
        private Parameter vsInputPos;
        protected Parameter psOutDiffuse;
        private Parameter psDiffuse;
        private Parameter psSpecular;


        public FFPTexturing()
        {
        }

        public override void UpdateGpuProgramsParams(IRenderable rend, Pass pass, AutoParamDataSource source,
                                                      Core.Collections.LightList lightList)
        {
            for (int i = 0; i < this.textureUnitParamsList.Count; i++)
            {
                TextureUnitParams curParams = this.textureUnitParamsList[i];

                if (curParams.TextureProjector != null && curParams.TextureViewProjImageMatrix != null)
                {
                    Matrix4 matTexViewProjImage;

                    matTexViewProjImage = Matrix4.ClipSpace2DToImageSpace *
                                          curParams.TextureProjector.ProjectionMatrixRSDepth *
                                          curParams.TextureProjector.ViewMatrix;

                    curParams.TextureViewProjImageMatrix.SetGpuParameter(matTexViewProjImage);
                }
            }
        }

        public override bool PreAddToRenderState(TargetRenderState targetRenderState, Pass srcPass, Pass dstPass)
        {
            //count the number of texture units we need to process
            int validTexUnits = 0;
            for (int i = 0; i < srcPass.TextureUnitStatesCount; i++)
            {
                if (IsProcessingNeeded(srcPass.GetTextureUnitState(i)))
                {
                    validTexUnits++;
                }
            }

            SetTextureUnitCount(validTexUnits);

            //Build texture stage sub states
            for (int i = 0; i < srcPass.TextureUnitStatesCount; i++)
            {
                TextureUnitState texUnitState = srcPass.GetTextureUnitState(i);
                if (IsProcessingNeeded(texUnitState))
                {
                    SetTextureUnit(i, texUnitState);
                }
            }

            return true;
        }

        private void SetTextureUnitCount(int validTexUnits)
        {
            for (int i = 0; i < validTexUnits; i++)
            {
                TextureUnitParams curParams = this.textureUnitParamsList[i];

                curParams.TextureUnitState = null;
                curParams.TextureProjector = null;
                curParams.TextureSamplerIndex = 0;
                curParams.TextureSamplerType = GpuProgramParameters.GpuConstantType.Sampler2D;
                curParams.VSInTextureCoordinateType = GpuProgramParameters.GpuConstantType.Float2;
                curParams.VSOutTextureCoordinateType = GpuProgramParameters.GpuConstantType.Float2;
            }
        }

        private void SetTextureUnit(int index, TextureUnitState textureUnitState)
        {
            if (index >= this.textureUnitParamsList.Count)
            {
                throw new AxiomException("FFPTexturing unit index out of bounds !!!");
            }
            if (textureUnitState.BindingType == TextureBindingType.Vertex)
            {
                throw new AxiomException("FFP Texture unit does not support vertex texture fetch !!!");
            }

            TextureUnitParams curParams = this.textureUnitParamsList[index];

            curParams.TextureSamplerIndex = index;
            curParams.TextureUnitState = textureUnitState;

            switch (curParams.TextureUnitState.TextureType)
            {
                case TextureType.CubeMap:
                    curParams.TextureSamplerType = GpuProgramParameters.GpuConstantType.SamplerCube;
                    curParams.VSInTextureCoordinateType = GpuProgramParameters.GpuConstantType.Float3;
                    break;
                case TextureType.OneD:
                    curParams.TextureSamplerType = GpuProgramParameters.GpuConstantType.Sampler1D;
                    curParams.VSInTextureCoordinateType = GpuProgramParameters.GpuConstantType.Float1;
                    break;
                case TextureType.ThreeD:
                    curParams.TextureSamplerType = GpuProgramParameters.GpuConstantType.Sampler3D;
                    curParams.VSInTextureCoordinateType = GpuProgramParameters.GpuConstantType.Float3;
                    break;
                case TextureType.TwoD:
                    curParams.TextureSamplerType = GpuProgramParameters.GpuConstantType.Sampler2D;
                    curParams.VSInTextureCoordinateType = GpuProgramParameters.GpuConstantType.Float2;
                    break;
            }

            curParams.VSOutTextureCoordinateType = curParams.VSInTextureCoordinateType;
            curParams.TexCoordCalcMethod = GetCalcMethod(curParams.TextureUnitState);

            if (curParams.TexCoordCalcMethod == TexCoordCalcMethod.ProjectiveTexture)
            {
                curParams.VSOutTextureCoordinateType = GpuProgramParameters.GpuConstantType.Float3;
            }
        }

        protected override bool ResolveParameters(ProgramSet programSet)
        {
            for (int i = 0; i < this.textureUnitParamsList.Count; i++)
            {
                TextureUnitParams curParams = this.textureUnitParamsList[i];

                if (!ResolveUniformParams(curParams, programSet))
                {
                    return false;
                }
                if (!ResolveFunctionsParams(curParams, programSet))
                {
                    return false;
                }
            }

            return true;
        }

        private bool ResolveFunctionsParams(TextureUnitParams curParams, ProgramSet programSet)
        {
            Program vsProgram = programSet.CpuVertexProgram;
            Program psProgram = programSet.CpuFragmentProgram;
            Function vsMain = vsProgram.EntryPointFunction;
            Function psMain = psProgram.EntryPointFunction;
            Parameter.ContentType texCoordContent = Parameter.ContentType.Unknown;

            switch (curParams.TexCoordCalcMethod)
            {
                case TexCoordCalcMethod.None:
                    //Resolve explicit vs input texture coordinates

                    if (curParams.TextureMatrix == null)
                    {
                        switch (curParams.TextureUnitState.TextureCoordSet)
                        {
                            case 0:
                                texCoordContent = Parameter.ContentType.TextureCoordinate0;
                                break;
                            case 1:
                                texCoordContent = Parameter.ContentType.TextureCoordinate1;
                                break;
                            case 2:
                                texCoordContent = Parameter.ContentType.TextureCoordinate2;
                                break;
                            case 3:
                                texCoordContent = Parameter.ContentType.TextureCoordinate3;
                                break;
                            case 4:
                                texCoordContent = Parameter.ContentType.TextureCoordinate4;
                                break;
                            case 5:
                                texCoordContent = Parameter.ContentType.TextureCoordinate5;
                                break;
                            case 6:
                                texCoordContent = Parameter.ContentType.TextureCoordinate6;
                                break;
                            case 7:
                                texCoordContent = Parameter.ContentType.TextureCoordinate7;
                                break;
                        }
                    }
                    Parameter.ContentType texCoordToUse = Parameter.ContentType.TextureCoordinate0;
                    switch (curParams.TextureUnitState.TextureCoordSet)
                    {
                        case 0:
                            texCoordToUse = Parameter.ContentType.TextureCoordinate0;
                            break;
                        case 1:
                            texCoordToUse = Parameter.ContentType.TextureCoordinate1;
                            break;
                        case 2:
                            texCoordToUse = Parameter.ContentType.TextureCoordinate2;
                            break;
                        case 3:
                            texCoordToUse = Parameter.ContentType.TextureCoordinate3;
                            break;
                        case 4:
                            texCoordToUse = Parameter.ContentType.TextureCoordinate4;
                            break;
                        case 5:
                            texCoordToUse = Parameter.ContentType.TextureCoordinate5;
                            break;
                        case 6:
                            texCoordToUse = Parameter.ContentType.TextureCoordinate6;
                            break;
                        case 7:
                            texCoordToUse = Parameter.ContentType.TextureCoordinate7;
                            break;
                    }

                    curParams.VSInputTexCoord = vsMain.ResolveInputParameter(
                        Parameter.SemanticType.TextureCoordinates, curParams.TextureUnitState.TextureCoordSet,
                        texCoordToUse, curParams.VSInTextureCoordinateType);
                    if (curParams.VSInputTexCoord == null)
                    {
                        return false;
                    }
                    break;
                case TexCoordCalcMethod.EnvironmentMap:
                case TexCoordCalcMethod.EnvironmentMapNormal:
                case TexCoordCalcMethod.EnvironmentMapPlanar:
                    //Resolve vertex normal
                    this.vsInputNormal = vsMain.ResolveInputParameter(Parameter.SemanticType.Normal, 0,
                                                                       Parameter.ContentType.NormalObjectSpace,
                                                                       GpuProgramParameters.GpuConstantType.Float3);
                    if (this.vsInputNormal == null)
                    {
                        return false;
                    }
                    break;
                case TexCoordCalcMethod.EnvironmentMapReflection:
                    //Resolve vertex normal
                    this.vsInputNormal = vsMain.ResolveInputParameter(Parameter.SemanticType.Normal, 0,
                                                                       Parameter.ContentType.NormalObjectSpace,
                                                                       GpuProgramParameters.GpuConstantType.Float3);
                    if (this.vsInputNormal == null)
                    {
                        return false;
                    }

                    //Resovle vertex position
                    this.vsInputPos = vsMain.ResolveInputParameter(Parameter.SemanticType.Position, 0,
                                                                    Parameter.ContentType.PositionObjectSpace,
                                                                    GpuProgramParameters.GpuConstantType.Float4);
                    if (this.vsInputPos == null)
                    {
                        return false;
                    }
                    break;

                case TexCoordCalcMethod.ProjectiveTexture:
                    //Resolve vertex position
                    this.vsInputPos = vsMain.ResolveInputParameter(Parameter.SemanticType.Position, 0,
                                                                    Parameter.ContentType.PositionObjectSpace,
                                                                    GpuProgramParameters.GpuConstantType.Float4);
                    if (this.vsInputPos == null)
                    {
                        return false;
                    }
                    break;
            }

            //Resolve vs output texture coordinates
            curParams.PSInputTexCoord = psMain.ResolveInputParameter(Parameter.SemanticType.TextureCoordinates,
                                                                      curParams.VSOutputTexCoord.Index,
                                                                      curParams.VSOutputTexCoord.Content,
                                                                      curParams.VSOutTextureCoordinateType);

            if (curParams.PSInputTexCoord == null)
            {
                return false;
            }

            var inputParams = psMain.InputParameters;
            var localParams = psMain.LocalParameters;

            this.psDiffuse = Function.GetParameterByContent(inputParams, Parameter.ContentType.ColorDiffuse,
                                                             GpuProgramParameters.GpuConstantType.Float4);
            if (this.psDiffuse == null)
            {
                this.psDiffuse = Function.GetParameterByContent(localParams, Parameter.ContentType.ColorDiffuse,
                                                                 GpuProgramParameters.GpuConstantType.Float4);
                if (this.psDiffuse == null)
                {
                    return false;
                }
            }

            this.psSpecular = Function.GetParameterByContent(inputParams, Parameter.ContentType.ColorSpecular,
                                                              GpuProgramParameters.GpuConstantType.Float4);
            if (this.psSpecular == null)
            {
                this.psSpecular = Function.GetParameterByContent(localParams, Parameter.ContentType.ColorSpecular,
                                                                  GpuProgramParameters.GpuConstantType.Float4);
                if (this.psSpecular == null)
                {
                    return false;
                }
            }

            this.psOutDiffuse = psMain.ResolveOutputParameter(Parameter.SemanticType.Color, 0,
                                                               Parameter.ContentType.ColorDiffuse,
                                                               GpuProgramParameters.GpuConstantType.Float4);
            if (this.psOutDiffuse == null)
            {
                return false;
            }

            return true;
        }

        private bool ResolveUniformParams(TextureUnitParams textureUnitParams, ProgramSet programSet)
        {
            Program vsProgram = programSet.CpuVertexProgram;
            Program psProgram = programSet.CpuFragmentProgram;

            //Resolve texture sampler parameter.
            textureUnitParams.TextureSampler = psProgram.ResolveParameter(textureUnitParams.TextureSamplerType,
                                                                           textureUnitParams.TextureSamplerIndex,
                                                                           GpuProgramParameters.GpuParamVariability.
                                                                               Global, "gTextureSampler");
            if (textureUnitParams.TextureSampler == null)
            {
                return false;
            }

            //Resolve texture matrix parameter
            if (NeedsTextureMatrix(textureUnitParams.TextureUnitState))
            {
                textureUnitParams.TextureMatrix =
                    vsProgram.ResolveAutoParameterInt(GpuProgramParameters.AutoConstantType.TextureMatrix,
                                                       textureUnitParams.TextureSamplerIndex);
                if (textureUnitParams.TextureMatrix == null)
                {
                    return false;
                }
            }

            switch (textureUnitParams.TexCoordCalcMethod)
            {
                case TexCoordCalcMethod.None:
                    break;
                case TexCoordCalcMethod.EnvironmentMap:
                case TexCoordCalcMethod.EnvironmentMapNormal:
                case TexCoordCalcMethod.EnvironmentMapPlanar:
                    this.worldITMatrix =
                        vsProgram.ResolveAutoParameterInt(
                            GpuProgramParameters.AutoConstantType.InverseTransposeWorldMatrix, 0);
                    if (this.worldITMatrix == null)
                    {
                        return false;
                    }

                    this.viewMatrix = vsProgram.ResolveAutoParameterInt(GpuProgramParameters.AutoConstantType.ViewMatrix, 0);
                    if (this.viewMatrix == null)
                    {
                        return false;
                    }
                    break;
                case TexCoordCalcMethod.EnvironmentMapReflection:
                    this.worldMatrix = vsProgram.ResolveAutoParameterInt(GpuProgramParameters.AutoConstantType.WorldMatrix,
                                                                          0);
                    if (this.worldMatrix == null)
                    {
                        return false;
                    }

                    this.worldITMatrix =
                        vsProgram.ResolveAutoParameterInt(
                            GpuProgramParameters.AutoConstantType.InverseTransposeWorldMatrix, 0);
                    if (this.worldITMatrix == null)
                    {
                        return false;
                    }

                    this.viewMatrix = vsProgram.ResolveAutoParameterInt(GpuProgramParameters.AutoConstantType.ViewMatrix, 0);
                    if (this.viewMatrix == null)
                    {
                        return false;
                    }
                    break;
                case TexCoordCalcMethod.ProjectiveTexture:
                    this.worldMatrix = vsProgram.ResolveAutoParameterInt(GpuProgramParameters.AutoConstantType.WorldMatrix,
                                                                          0);
                    if (this.worldMatrix == null)
                    {
                        return false;
                    }

                    textureUnitParams.TextureViewProjImageMatrix =
                        vsProgram.ResolveParameter(GpuProgramParameters.GpuConstantType.Matrix_4X4, -1,
                                                    GpuProgramParameters.GpuParamVariability.Lights,
                                                    "gTexViewProjImageMatrix");
                    if (textureUnitParams.TextureViewProjImageMatrix == null)
                    {
                        return false;
                    }

                    var effects = new List<TextureEffect>();
                    for (int i = 0; i < textureUnitParams.TextureUnitState.NumEffects; i++)
                    {
                        var curEffect = textureUnitParams.TextureUnitState.GetEffect(i);
                        effects.Add(curEffect);
                    }

                    foreach (var effi in effects)
                    {
                        if (effi.type == TextureEffectType.ProjectiveTexture)
                        {
                            textureUnitParams.TextureProjector = effi.frustum;
                            break;
                        }
                    }

                    if (textureUnitParams.TextureProjector == null)
                    {
                        return false;
                    }
                    break;
            }
            return true;
        }

        protected override bool ResolveDependencies(ProgramSet programSet)
        {
            Program vsProgram = programSet.CpuVertexProgram;
            Program psProgram = programSet.CpuFragmentProgram;

            vsProgram.AddDependency(FFPRenderState.FFPLibCommon);
            vsProgram.AddDependency(FFPRenderState.FFPLibTexturing);

            psProgram.AddDependency(FFPRenderState.FFPLibCommon);
            psProgram.AddDependency(FFPRenderState.FFPLibTexturing);

            return true;
        }

        protected override bool AddFunctionInvocations(ProgramSet programSet)
        {
            Program vsProgram = programSet.CpuVertexProgram;
            Program psProgram = programSet.CpuFragmentProgram;
            Function vsMain = vsProgram.EntryPointFunction;
            Function psMain = psProgram.EntryPointFunction;

            int internalCounter = 0;
            for (int i = 0; i < this.textureUnitParamsList.Count; i++)
            {
                TextureUnitParams curParams = this.textureUnitParamsList[i];

                if (!AddVSFunctionInvocations(curParams, vsMain))
                {
                    return false;
                }
                if (!AddPSFunctionInvocations(curParams, psMain, ref internalCounter))
                {
                    return false;
                }
            }

            return true;
        }

        private bool AddVSFunctionInvocations(TextureUnitParams textureUnitParams, Function vsMain)
        {
            FunctionInvocation texCoordCalcFunc = null;

            switch (textureUnitParams.TexCoordCalcMethod)
            {
                case TexCoordCalcMethod.None:
                    if (textureUnitParams.TextureMatrix == null)
                    {
                        texCoordCalcFunc = new FunctionInvocation(FFPRenderState.FFPFuncAssign,
                                                                   (int)FFPRenderState.FFPVertexShaderStage.VSTexturing,
                                                                   textureUnitParams.TextureSamplerIndex);

                        texCoordCalcFunc.PushOperand(textureUnitParams.VSInputTexCoord, Operand.OpSemantic.In);
                        texCoordCalcFunc.PushOperand(textureUnitParams.VSOutputTexCoord, Operand.OpSemantic.Out);
                    }
                    else
                    {
                        texCoordCalcFunc = new FunctionInvocation(FFPRenderState.FFPFuncTransformTexCoord,
                                                                   (int)FFPRenderState.FFPVertexShaderStage.VSTexturing,
                                                                   textureUnitParams.TextureSamplerIndex);
                        texCoordCalcFunc.PushOperand(textureUnitParams.TextureMatrix, Operand.OpSemantic.In);
                        texCoordCalcFunc.PushOperand(textureUnitParams.VSInputTexCoord, Operand.OpSemantic.In);
                        texCoordCalcFunc.PushOperand(textureUnitParams.VSOutputTexCoord, Operand.OpSemantic.Out);
                    }
                    break;
                case TexCoordCalcMethod.EnvironmentMap:
                case TexCoordCalcMethod.EnvironmentMapPlanar:
                    if (textureUnitParams.TextureMatrix == null)
                    {
                        texCoordCalcFunc = new FunctionInvocation(FFPRenderState.FFPFunGenerateTexcoordEnvSphere,
                                                                   (int)FFPRenderState.FFPVertexShaderStage.VSTexturing,
                                                                   textureUnitParams.TextureSamplerIndex);
                        texCoordCalcFunc.PushOperand(this.worldITMatrix, Operand.OpSemantic.In);
                        texCoordCalcFunc.PushOperand(this.viewMatrix, Operand.OpSemantic.In);
                        texCoordCalcFunc.PushOperand(this.vsInputNormal, Operand.OpSemantic.In);
                        texCoordCalcFunc.PushOperand(textureUnitParams.VSOutputTexCoord, Operand.OpSemantic.Out);
                    }
                    else
                    {
                        texCoordCalcFunc = new FunctionInvocation(FFPRenderState.FFPFunGenerateTexcoordEnvSphere,
                                                                   (int)FFPRenderState.FFPVertexShaderStage.VSTexturing,
                                                                   textureUnitParams.TextureSamplerIndex);

                        texCoordCalcFunc.PushOperand(this.worldITMatrix, Operand.OpSemantic.In);
                        texCoordCalcFunc.PushOperand(this.viewMatrix, Operand.OpSemantic.In);
                        texCoordCalcFunc.PushOperand(textureUnitParams.TextureMatrix, Operand.OpSemantic.In);
                        texCoordCalcFunc.PushOperand(this.vsInputNormal, Operand.OpSemantic.In);
                        texCoordCalcFunc.PushOperand(textureUnitParams.VSOutputTexCoord, Operand.OpSemantic.Out);
                    }
                    break;

                case TexCoordCalcMethod.EnvironmentMapReflection:
                    if (textureUnitParams.TextureMatrix == null)
                    {
                        texCoordCalcFunc = new FunctionInvocation(FFPRenderState.FFPFuncGenerateTexCoordEnvReflect,
                                                                   (int)FFPRenderState.FFPVertexShaderStage.VSTexturing,
                                                                   textureUnitParams.TextureSamplerIndex);

                        texCoordCalcFunc.PushOperand(this.worldMatrix, Operand.OpSemantic.In);
                        texCoordCalcFunc.PushOperand(this.worldITMatrix, Operand.OpSemantic.In);
                        texCoordCalcFunc.PushOperand(this.viewMatrix, Operand.OpSemantic.In);
                        texCoordCalcFunc.PushOperand(this.vsInputNormal, Operand.OpSemantic.In);
                        texCoordCalcFunc.PushOperand(this.vsInputPos, Operand.OpSemantic.In);
                        texCoordCalcFunc.PushOperand(textureUnitParams.VSOutputTexCoord, Operand.OpSemantic.Out);
                    }
                    else
                    {
                        texCoordCalcFunc = new FunctionInvocation(FFPRenderState.FFPFuncGenerateTexCoordEnvReflect,
                                                                   (int)FFPRenderState.FFPVertexShaderStage.VSTexturing,
                                                                   textureUnitParams.TextureSamplerIndex);
                        texCoordCalcFunc.PushOperand(this.worldMatrix, Operand.OpSemantic.In);
                        texCoordCalcFunc.PushOperand(this.worldITMatrix, Operand.OpSemantic.In);
                        texCoordCalcFunc.PushOperand(this.viewMatrix, Operand.OpSemantic.In);
                        texCoordCalcFunc.PushOperand(textureUnitParams.TextureMatrix, Operand.OpSemantic.In);
                        texCoordCalcFunc.PushOperand(this.vsInputNormal, Operand.OpSemantic.In);
                        texCoordCalcFunc.PushOperand(this.vsInputPos, Operand.OpSemantic.In);
                        texCoordCalcFunc.PushOperand(textureUnitParams.VSOutputTexCoord, Operand.OpSemantic.Out);
                    }
                    break;
                case TexCoordCalcMethod.EnvironmentMapNormal:
                    if (textureUnitParams.TextureMatrix == null)
                    {
                        texCoordCalcFunc = new FunctionInvocation(FFPRenderState.FFPFuncGenerateTexcoordEnvNormal,
                                                                   (int)FFPRenderState.FFPVertexShaderStage.VSTexturing,
                                                                   textureUnitParams.TextureSamplerIndex);

                        texCoordCalcFunc.PushOperand(this.worldITMatrix, Operand.OpSemantic.In);
                        texCoordCalcFunc.PushOperand(this.viewMatrix, Operand.OpSemantic.In);
                        texCoordCalcFunc.PushOperand(this.vsInputPos, Operand.OpSemantic.In);
                        texCoordCalcFunc.PushOperand(textureUnitParams.VSOutputTexCoord, Operand.OpSemantic.Out);
                    }
                    else
                    {
                        texCoordCalcFunc = new FunctionInvocation(FFPRenderState.FFPFuncGenerateTexcoordEnvNormal,
                                                                   (int)FFPRenderState.FFPVertexShaderStage.VSTexturing,
                                                                   textureUnitParams.TextureSamplerIndex);

                        texCoordCalcFunc.PushOperand(this.worldITMatrix, Operand.OpSemantic.In);
                        texCoordCalcFunc.PushOperand(this.viewMatrix, Operand.OpSemantic.In);
                        texCoordCalcFunc.PushOperand(textureUnitParams.TextureMatrix, Operand.OpSemantic.In);
                        texCoordCalcFunc.PushOperand(this.vsInputNormal, Operand.OpSemantic.In);
                        texCoordCalcFunc.PushOperand(textureUnitParams.VSOutputTexCoord, Operand.OpSemantic.Out);
                    }
                    break;
                case TexCoordCalcMethod.ProjectiveTexture:
                    texCoordCalcFunc = new FunctionInvocation(FFPRenderState.FFPFuncGenerateTexCoordProjection,
                                                               (int)FFPRenderState.FFPVertexShaderStage.VSTexturing,
                                                               textureUnitParams.TextureSamplerIndex);

                    texCoordCalcFunc.PushOperand(this.worldMatrix, Operand.OpSemantic.In);
                    texCoordCalcFunc.PushOperand(textureUnitParams.TextureViewProjImageMatrix, Operand.OpSemantic.In);
                    texCoordCalcFunc.PushOperand(this.vsInputPos, Operand.OpSemantic.In);
                    texCoordCalcFunc.PushOperand(textureUnitParams.VSOutputTexCoord, Operand.OpSemantic.Out);

                    break;
            }

            if (texCoordCalcFunc != null)
            {
                vsMain.AddAtomInstance(texCoordCalcFunc);
            }

            return true;
        }

        private bool AddPSFunctionInvocations(TextureUnitParams textureUnitParams, Function psMain,
                                               ref int internalCounter)
        {
            LayerBlendModeEx colorBlend = textureUnitParams.TextureUnitState.ColorBlendMode;
            LayerBlendModeEx alphaBlend = textureUnitParams.TextureUnitState.AlphaBlendMode;
            Parameter source1;
            Parameter source2;
            var groupOrder = (int)FFPRenderState.FFPFragmentShaderStage.PSTexturing;

            //Add texture sampling code
            Parameter texel = psMain.ResolveLocalParameter(Parameter.SemanticType.Unknown, 0,
                                                            "texel_" + textureUnitParams.TextureSamplerIndex.ToString(),
                                                            GpuProgramParameters.GpuConstantType.Float4);
            AddPSSampleTexelInvocation(textureUnitParams, psMain, texel, groupOrder, ref internalCounter);

            //Build color argument for source1
            source1 = psMain.ResolveLocalParameter(Parameter.SemanticType.Unknown, 0, "source1",
                                                    GpuProgramParameters.GpuConstantType.Float4);

            AddPSArgumentInvocations(psMain, source1, texel, textureUnitParams.TextureSamplerIndex, colorBlend.source1,
                                      colorBlend.colorArg1, colorBlend.alphaArg1, false, groupOrder, ref internalCounter);

            //build color argument for source2
            source2 = psMain.ResolveLocalParameter(Parameter.SemanticType.Unknown, 0, "source2",
                                                    GpuProgramParameters.GpuConstantType.Float4);

            AddPSArgumentInvocations(psMain, source2, texel, textureUnitParams.TextureSamplerIndex, colorBlend.source2,
                                      colorBlend.colorArg2, colorBlend.alphaArg2, false, groupOrder, ref internalCounter);

            bool needDifferentAlphaBlend = false;
            if (alphaBlend.operation != colorBlend.operation ||
                 alphaBlend.source1 != colorBlend.source1 ||
                 alphaBlend.source2 != colorBlend.source2 ||
                 colorBlend.source1 == LayerBlendSource.Manual ||
                 colorBlend.source2 == LayerBlendSource.Manual ||
                 alphaBlend.source1 == LayerBlendSource.Manual ||
                 alphaBlend.source2 == LayerBlendSource.Manual)
            {
                needDifferentAlphaBlend = true;
            }

            //Build colors blend
            AddPSBlendInvocations(psMain, source1, source2, texel, textureUnitParams.TextureSamplerIndex, colorBlend,
                                   groupOrder, ref internalCounter,
                                   needDifferentAlphaBlend
                                       ? (int)(Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z)
                                       : (int)(Operand.OpMask.All));

            //Case we need different alpha channel code
            if (needDifferentAlphaBlend)
            {
                //build alpha argument for source1
                AddPSArgumentInvocations(psMain, source1, texel, textureUnitParams.TextureSamplerIndex,
                                          alphaBlend.source1, alphaBlend.colorArg1, alphaBlend.alphaArg1, true,
                                          groupOrder, ref internalCounter);

                //Build alpha argument for source2
                AddPSArgumentInvocations(psMain, source2, texel, textureUnitParams.TextureSamplerIndex,
                                          alphaBlend.source2, alphaBlend.colorArg2, alphaBlend.alphaArg2, true,
                                          groupOrder, ref internalCounter);

                //Build alpha blend
                AddPSBlendInvocations(psMain, source1, source2, texel, textureUnitParams.TextureSamplerIndex,
                                       alphaBlend, groupOrder, ref internalCounter, (int)Operand.OpMask.W);
            }

            return true;
        }

        protected virtual void AddPSSampleTexelInvocation(TextureUnitParams textureUnitParams, Function psMain,
                                                           Parameter texel, int groupOrder, ref int internalCounter)
        {
            FunctionInvocation curFuncInvocation = null;
            if (textureUnitParams.TexCoordCalcMethod == TexCoordCalcMethod.ProjectiveTexture)
            {
                curFuncInvocation = new FunctionInvocation(FFPRenderState.FFPFuncSamplerTextureProj, groupOrder,
                                                            internalCounter++);
            }
            else
            {
                curFuncInvocation = new FunctionInvocation(FFPRenderState.FFPFuncSampleTexture, groupOrder,
                                                            internalCounter++);
            }

            curFuncInvocation.PushOperand(textureUnitParams.TextureSampler, Operand.OpSemantic.In);
            curFuncInvocation.PushOperand(textureUnitParams.PSInputTexCoord, Operand.OpSemantic.In);
            curFuncInvocation.PushOperand(texel, Operand.OpSemantic.Out);
            psMain.AddAtomInstance(curFuncInvocation);
        }

        protected virtual void AddPSArgumentInvocations(Function psMain, Parameter arg, Parameter texel,
                                                         int samplerIndex, LayerBlendSource blendSrc, ColorEx colorValue,
                                                         Real alphaValue, bool isAlphaArgument, int groupOrder,
                                                         ref int internalCounter)
        {
            FunctionInvocation curFuncInvocation = null;

            switch (blendSrc)
            {
                case LayerBlendSource.Current:
                    curFuncInvocation = new FunctionInvocation(FFPRenderState.FFPFuncAssign, groupOrder, internalCounter++);
                    if (samplerIndex == 0)
                    {
                        curFuncInvocation.PushOperand(this.psDiffuse, Operand.OpSemantic.In);
                    }
                    else
                    {
                        curFuncInvocation.PushOperand(this.psOutDiffuse, Operand.OpSemantic.In);
                    }
                    curFuncInvocation.PushOperand(arg, Operand.OpSemantic.Out);
                    psMain.AddAtomInstance(curFuncInvocation);
                    break;

                case LayerBlendSource.Texture:
                    curFuncInvocation = new FunctionInvocation(FFPRenderState.FFPFuncAssign, groupOrder,
                                                                internalCounter++);
                    curFuncInvocation.PushOperand(texel, Operand.OpSemantic.In);
                    curFuncInvocation.PushOperand(arg, Operand.OpSemantic.Out);
                    psMain.AddAtomInstance(curFuncInvocation);
                    break;

                case LayerBlendSource.Specular:
                    curFuncInvocation = new FunctionInvocation(FFPRenderState.FFPFuncAssign, groupOrder,
                                                                internalCounter++);
                    curFuncInvocation.PushOperand(this.psSpecular, Operand.OpSemantic.In);
                    curFuncInvocation.PushOperand(arg, Operand.OpSemantic.Out);
                    psMain.AddAtomInstance(curFuncInvocation);
                    break;

                case LayerBlendSource.Diffuse:
                    curFuncInvocation = new FunctionInvocation(FFPRenderState.FFPFuncAssign, groupOrder,
                                                                internalCounter++);
                    curFuncInvocation.PushOperand(this.psDiffuse, Operand.OpSemantic.In);
                    curFuncInvocation.PushOperand(arg, Operand.OpSemantic.Out);
                    psMain.AddAtomInstance(curFuncInvocation);
                    break;
                case LayerBlendSource.Manual:
                    curFuncInvocation = new FunctionInvocation(FFPRenderState.FFPFuncConstruct, groupOrder,
                                                                internalCounter++);

                    if (isAlphaArgument)
                    {
                        curFuncInvocation.PushOperand(ParameterFactory.CreateConstParamFloat(alphaValue),
                                                       Operand.OpSemantic.In);
                    }
                    else
                    {
                        curFuncInvocation.PushOperand(ParameterFactory.CreateConstParamFloat(colorValue.r),
                                                       Operand.OpSemantic.In);
                        curFuncInvocation.PushOperand(ParameterFactory.CreateConstParamFloat(colorValue.g),
                                                       Operand.OpSemantic.In);
                        curFuncInvocation.PushOperand(ParameterFactory.CreateConstParamFloat(colorValue.b),
                                                       Operand.OpSemantic.In);
                        curFuncInvocation.PushOperand(ParameterFactory.CreateConstParamFloat(colorValue.a),
                                                       Operand.OpSemantic.In);
                    }

                    curFuncInvocation.PushOperand(arg, Operand.OpSemantic.In);
                    psMain.AddAtomInstance(curFuncInvocation);
                    break;
            }
        }

        protected virtual void AddPSBlendInvocations(Function psMain, Parameter arg1, Parameter arg2, Parameter texel,
                                                      int samplerIndex, LayerBlendModeEx blendMode,
                                                      int groupOrder, ref int internalCounter, int targetChannels)
        {
            FunctionInvocation curFuncInvocation = null;

            switch (blendMode.operation)
            {
                case LayerBlendOperationEx.Add:
                    curFuncInvocation = new FunctionInvocation(FFPRenderState.FFPFuncAdd, groupOrder, internalCounter++);
                    curFuncInvocation.PushOperand(arg1, Operand.OpSemantic.In, targetChannels);
                    curFuncInvocation.PushOperand(arg2, Operand.OpSemantic.In, targetChannels);
                    curFuncInvocation.PushOperand(this.psOutDiffuse, Operand.OpSemantic.Out, targetChannels);
                    psMain.AddAtomInstance(curFuncInvocation);
                    break;
                case LayerBlendOperationEx.AddSigned:
                    curFuncInvocation = new FunctionInvocation(FFPRenderState.FFPFuncAddSigned, groupOrder,
                                                                internalCounter++);
                    curFuncInvocation.PushOperand(arg1, Operand.OpSemantic.In, targetChannels);
                    curFuncInvocation.PushOperand(arg2, Operand.OpSemantic.In, targetChannels);
                    curFuncInvocation.PushOperand(this.psOutDiffuse, Operand.OpSemantic.Out, targetChannels);
                    psMain.AddAtomInstance(curFuncInvocation);
                    break;
                case LayerBlendOperationEx.AddSmooth:
                    curFuncInvocation = new FunctionInvocation(FFPRenderState.FFPFuncAddSmooth, groupOrder,
                                                                internalCounter++);
                    curFuncInvocation.PushOperand(arg1, Operand.OpSemantic.In, targetChannels);
                    curFuncInvocation.PushOperand(arg2, Operand.OpSemantic.In, targetChannels);
                    curFuncInvocation.PushOperand(this.psOutDiffuse, Operand.OpSemantic.Out, targetChannels);
                    psMain.AddAtomInstance(curFuncInvocation);
                    break;
                case LayerBlendOperationEx.BlendCurrentAlpha:
                    curFuncInvocation = new FunctionInvocation(FFPRenderState.FFPFuncLerp, groupOrder,
                                                                internalCounter++);
                    curFuncInvocation.PushOperand(arg2, Operand.OpSemantic.In, targetChannels);
                    curFuncInvocation.PushOperand(arg1, Operand.OpSemantic.In, targetChannels);

                    if (samplerIndex == 0)
                    {
                        curFuncInvocation.PushOperand(this.psDiffuse, Operand.OpSemantic.In, Operand.OpMask.W);
                    }
                    else
                    {
                        curFuncInvocation.PushOperand(this.psOutDiffuse, Operand.OpSemantic.In, Operand.OpMask.W);
                    }
                    curFuncInvocation.PushOperand(this.psOutDiffuse, Operand.OpSemantic.Out, targetChannels);
                    psMain.AddAtomInstance(curFuncInvocation);
                    break;
                case LayerBlendOperationEx.BlendDiffuseAlpha:
                    curFuncInvocation = new FunctionInvocation(FFPRenderState.FFPFuncSubtract, groupOrder,
                                                                internalCounter++);
                    curFuncInvocation.PushOperand(arg1, Operand.OpSemantic.In, targetChannels);
                    curFuncInvocation.PushOperand(arg2, Operand.OpSemantic.In, targetChannels);
                    curFuncInvocation.PushOperand(this.psDiffuse, Operand.OpSemantic.In, Operand.OpMask.W);
                    curFuncInvocation.PushOperand(this.psOutDiffuse, Operand.OpSemantic.Out, targetChannels);
                    psMain.AddAtomInstance(curFuncInvocation);
                    break;
                case LayerBlendOperationEx.BlendDiffuseColor:
                    curFuncInvocation = new FunctionInvocation(FFPRenderState.FFPFuncLerp, groupOrder,
                                                                internalCounter++);
                    curFuncInvocation.PushOperand(arg2, Operand.OpSemantic.In, targetChannels);
                    curFuncInvocation.PushOperand(arg1, Operand.OpSemantic.In, targetChannels);
                    curFuncInvocation.PushOperand(this.psDiffuse, Operand.OpSemantic.In);
                    curFuncInvocation.PushOperand(this.psOutDiffuse, Operand.OpSemantic.Out, targetChannels);
                    psMain.AddAtomInstance(curFuncInvocation);
                    break;
                case LayerBlendOperationEx.BlendManual:
                    curFuncInvocation = new FunctionInvocation(FFPRenderState.FFPFuncLerp, groupOrder, internalCounter);
                    curFuncInvocation.PushOperand(arg2, Operand.OpSemantic.In, targetChannels);
                    curFuncInvocation.PushOperand(arg1, Operand.OpSemantic.In, targetChannels);
                    curFuncInvocation.PushOperand(ParameterFactory.CreateConstParamFloat(blendMode.blendFactor),
                                                   Operand.OpSemantic.In);
                    curFuncInvocation.PushOperand(this.psOutDiffuse, Operand.OpSemantic.Out, targetChannels);
                    psMain.AddAtomInstance(curFuncInvocation);
                    break;
                case LayerBlendOperationEx.BlendTextureAlpha:
                    curFuncInvocation = new FunctionInvocation(FFPRenderState.FFPFuncLerp, groupOrder,
                                                                internalCounter++);
                    curFuncInvocation.PushOperand(arg1, Operand.OpSemantic.In, targetChannels);
                    curFuncInvocation.PushOperand(arg2, Operand.OpSemantic.In, targetChannels);
                    curFuncInvocation.PushOperand(texel, Operand.OpSemantic.In, Operand.OpMask.W);
                    curFuncInvocation.PushOperand(this.psOutDiffuse, Operand.OpSemantic.Out, targetChannels);
                    psMain.AddAtomInstance(curFuncInvocation);
                    break;
                case LayerBlendOperationEx.DotProduct:
                    curFuncInvocation = new FunctionInvocation(FFPRenderState.FFPFuncDotProduct, groupOrder,
                                                                internalCounter++);
                    curFuncInvocation.PushOperand(arg2, Operand.OpSemantic.In, targetChannels);
                    curFuncInvocation.PushOperand(arg1, Operand.OpSemantic.In, targetChannels);
                    curFuncInvocation.PushOperand(this.psOutDiffuse, Operand.OpSemantic.Out, targetChannels);
                    psMain.AddAtomInstance(curFuncInvocation);
                    break;
                case LayerBlendOperationEx.Modulate:
                    curFuncInvocation = new FunctionInvocation(FFPRenderState.FFPFuncModulate, groupOrder,
                                                                internalCounter++);
                    curFuncInvocation.PushOperand(arg1, Operand.OpSemantic.In, targetChannels);
                    curFuncInvocation.PushOperand(arg2, Operand.OpSemantic.In, targetChannels);
                    curFuncInvocation.PushOperand(this.psOutDiffuse, Operand.OpSemantic.Out, targetChannels);
                    psMain.AddAtomInstance(curFuncInvocation);
                    break;
                case LayerBlendOperationEx.ModulateX2:
                    curFuncInvocation = new FunctionInvocation(FFPRenderState.FFPFuncModulateX2, groupOrder,
                                                                internalCounter);
                    curFuncInvocation.PushOperand(arg1, Operand.OpSemantic.In, targetChannels);
                    curFuncInvocation.PushOperand(arg2, Operand.OpSemantic.In, targetChannels);
                    curFuncInvocation.PushOperand(this.psOutDiffuse, Operand.OpSemantic.Out, targetChannels);
                    psMain.AddAtomInstance(curFuncInvocation);
                    break;
                case LayerBlendOperationEx.ModulateX4:
                    curFuncInvocation = new FunctionInvocation(FFPRenderState.FFPFuncModulateX4, groupOrder,
                                                                internalCounter);
                    curFuncInvocation.PushOperand(arg1, Operand.OpSemantic.In, targetChannels);
                    curFuncInvocation.PushOperand(arg2, Operand.OpSemantic.In, targetChannels);
                    curFuncInvocation.PushOperand(this.psOutDiffuse, Operand.OpSemantic.Out, targetChannels);
                    psMain.AddAtomInstance(curFuncInvocation);
                    break;
                case LayerBlendOperationEx.Source1:
                    curFuncInvocation = new FunctionInvocation(FFPRenderState.FFPFuncAssign, groupOrder,
                                                                internalCounter++);
                    curFuncInvocation.PushOperand(arg1, Operand.OpSemantic.In, targetChannels);
                    curFuncInvocation.PushOperand(this.psOutDiffuse, Operand.OpSemantic.Out, targetChannels);
                    psMain.AddAtomInstance(curFuncInvocation);
                    break;
                case LayerBlendOperationEx.Source2:
                    curFuncInvocation = new FunctionInvocation(FFPRenderState.FFPFuncAssign, groupOrder,
                                                                internalCounter++);
                    curFuncInvocation.PushOperand(arg2, Operand.OpSemantic.In, targetChannels);
                    curFuncInvocation.PushOperand(this.psOutDiffuse, Operand.OpSemantic.Out, targetChannels);
                    psMain.AddAtomInstance(curFuncInvocation);
                    break;
                case LayerBlendOperationEx.Subtract:
                    curFuncInvocation = new FunctionInvocation(FFPRenderState.FFPFuncSubtract, groupOrder,
                                                                internalCounter++);
                    curFuncInvocation.PushOperand(arg1, Operand.OpSemantic.In, targetChannels);
                    curFuncInvocation.PushOperand(arg2, Operand.OpSemantic.In, targetChannels);
                    curFuncInvocation.PushOperand(this.psOutDiffuse, Operand.OpSemantic.Out, targetChannels);
                    psMain.AddAtomInstance(curFuncInvocation);
                    break;
            }
        }

        private TexCoordCalcMethod GetCalcMethod(TextureUnitState textureUnitState)
        {
            TexCoordCalcMethod texCoordCalcMethod = TexCoordCalcMethod.None;
            var effectMap = new List<TextureEffect>();
            for (int i = 0; i < textureUnitState.NumEffects; i++)
            {
                effectMap.Add(textureUnitState.GetEffect(i));
            }

            foreach (var effi in effectMap)
            {
                switch (effi.type)
                {
                    case TextureEffectType.EnvironmentMap:
                        //TODO
                        //if (effi.subtype == Curved)
                        //{
                        //    texCoordCalcMethod = TexCoordCalcMethod.EnvironmentMap;
                        //}
                        //else if (effi.subtype == Planar)
                        //{
                        //    texCoordCalcMethod = TexCoordCalcMethod.EnvironmentMapPlanar;
                        //}
                        //else if (effi.subtype == Reflection)
                        //{
                        //    texCoordCalcMethod = TexCoordCalcMethod.EnvironmentMapReflection;
                        //}
                        //else if (effi.subtype == Normal)
                        //{
                        //    texCoordCalcMethod = TexCoordCalcMethod.EnvironmentMapNormal;
                        //}

                        break;
                    case TextureEffectType.ProjectiveTexture:
                        texCoordCalcMethod = TexCoordCalcMethod.ProjectiveTexture;
                        break;
                    case TextureEffectType.Rotate:
                    case TextureEffectType.Transform:
                    case TextureEffectType.UScroll:
                    case TextureEffectType.UVScroll:
                    case TextureEffectType.VScroll:
                        break;
                }
            }

            return texCoordCalcMethod;
        }

        private bool NeedsTextureMatrix(TextureUnitState textureUnitState)
        {
            for (int i = 0; i < textureUnitState.NumEffects; i++)
            {
                TextureEffect effi = textureUnitState.GetEffect(i);

                switch (effi.type)
                {
                    case TextureEffectType.EnvironmentMap:
                    case TextureEffectType.ProjectiveTexture:
                    case TextureEffectType.Rotate:
                    case TextureEffectType.Transform:
                    case TextureEffectType.UScroll:
                    case TextureEffectType.UVScroll:
                    case TextureEffectType.VScroll:
                        return true;
                }
            }
            //TODO
            var matTexture = new Matrix4(); //textureUnitState.getTextureTransform();

            //Resolve texture matrix parameter
            if (matTexture != Matrix4.Identity)
            {
                return true;
            }

            return false;
        }

        protected virtual bool IsProcessingNeeded(TextureUnitState texUnitState)
        {
            return texUnitState.BindingType == TextureBindingType.Fragment;
        }

        private int TextureUnitCount
        {
            get
            {
                return this.textureUnitParamsList.Count;
            }
        }

        public override string Type
        {
            get
            {
                return FFPTexturing.FFPType;
            }
        }

        public override int ExecutionOrder
        {
            get
            {
                return (int)FFPRenderState.FFPShaderStage.Texturing;
            }
        }
    }
}