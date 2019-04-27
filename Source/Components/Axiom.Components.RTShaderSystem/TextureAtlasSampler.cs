using System;
using System.Collections.Generic;
using Axiom.Graphics;
using Axiom.Math;

namespace Axiom.Components.RTShaderSystem
{
    internal class TextureAtlasMap : Dictionary<string, List<TextureAtlasRecord>>
    {
    }

    public struct TextureAtlasRecord
    {
        public float posU, posV, width, height;
        public string originalTextureName;
        public string atlasTextureName;
        public int indexInAtlas;

        public TextureAtlasRecord(string texOriginalName, string texAtlasName, float texPosU, float texPosV,
                                   float texWidth, float texHeight, int texIndexInAtlas)
        {
            this.originalTextureName = texOriginalName;
            this.atlasTextureName = texAtlasName;
            this.posU = texPosU;
            this.posV = texPosV;
            this.width = texWidth;
            this.height = texHeight;
            this.indexInAtlas = texIndexInAtlas;
        }
    }

    public class TextureAtlasSampler : SubRenderState
    {
        #region Fields

        private static int MaxTextures = 4;
        public static int MaxSafeAtlasedTextuers = 250;
        private Parameter vsInpTextureTableIndex;

        private readonly Axiom.Graphics.UVWAddressing[] textureAddressings =
            new Axiom.Graphics.UVWAddressing[TextureAtlasSampler.MaxTextures];

        private readonly Parameter[] vsOutTextureDatas = new Parameter[TextureAtlasSampler.MaxTextures];
        private readonly Parameter[] psInpTextureData = new Parameter[TextureAtlasSampler.MaxTextures];

        private readonly UniformParameter[] psTextureSizes = new UniformParameter[TextureAtlasSampler.MaxTextures];
        private readonly UniformParameter[] vsTextureTable = new UniformParameter[TextureAtlasSampler.MaxTextures];

        private int atlasTexcoordPos;
        private readonly List<List<TextureAtlasRecord>> atlasTableDatas = new List<List<TextureAtlasRecord>>();
        private readonly bool[] isAtlasTextureUnits = new bool[TextureAtlasSampler.MaxTextures];
        private bool isTableDataUpdated;
        private bool autoAdjustPollPosition;

        public static string SGXType = "SGX_TextureAtlasSampler";

        private static string SGXLibTextureAtlas = "SGXLib_TextureAtlas";

        private static string SGXFuncAtlasSampleAutoAdjust = "SGX_Atlas_Sample_Auto_Adjust";
        private static string SGXFuncAtlasSampleNormal = "SGX_Atlas_Sample_Normal";

        private static string SGXFuncAtlasWrap = "SGX_Atlas_Wrap";
        private static string SGXFuncAtlasMirror = "SGX_Atlas_Mirror";
        private static string SGXFuncAtlasClamp = "SGX_Atlas_Clamp";
        private static string SGXFuncAtlasBorder = "SGX_Atlas_Border";


        private List<TextureAtlasRecord> blankAtlasTable;
        private string paramTexel = "texel_";
        private string RTAtlasKey = "RTAtlas";

        #endregion

        #region C'Tor

        public TextureAtlasSampler()
        {
            this.atlasTexcoordPos = 0;
            this.isTableDataUpdated = false;
            this.autoAdjustPollPosition = true;

            for (int i = 0; i < TextureAtlasSampler.MaxTextures; i++)
            {
                this.isAtlasTextureUnits[i] = false;
            }
        }

        #endregion

        #region Properties

        public override string Type
        {
            get
            {
                return "SGX_TextureAtlasSampler";
            }
        }

        public override int ExecutionOrder
        {
            get
            {
                return (int)FFPRenderState.FFPShaderStage.Texturing + 25;
            }
        }

        #endregion

        #region Methods

        public override void UpdateGpuProgramsParams(IRenderable rend, Pass pass, AutoParamDataSource source,
                                                      Core.Collections.LightList lightList)
        {
            if (this.isTableDataUpdated == false)
            {
                this.isTableDataUpdated = true;
                for (int j = 0; j < TextureAtlasSampler.MaxTextures; j++)
                {
                    if (this.isAtlasTextureUnits[j] == true)
                    {
                        //Update the information of the size of the atlas textures
                        //TODO: Replace -1, -1 with actual dimensions
                        var texSizeInt = new Math.Tuple<int, int>(-1, -1);
                        // = pass.GetTextureUnitState(j).Dimensions;
                        var texSize = new Vector2(texSizeInt.First, texSizeInt.Second);
                        this.psTextureSizes[j].SetGpuParameter(texSize);

                        //Update the information of which texture exists where in the atlas
                        GpuProgramParameters vsGpuParams = pass.VertexProgramParameters;
                        var buffer = new List<float>(this.atlasTableDatas[j].Count * 4);
                        for (int i = 0; i < this.atlasTableDatas[j].Count; i++)
                        {
                            buffer[i * 4] = this.atlasTableDatas[j][i].posU;
                            buffer[i * 4 + 1] = this.atlasTableDatas[j][i].posV;
                            buffer[i * 4 + 2] =
                                (float)Axiom.Math.Utility.Log2((int)this.atlasTableDatas[j][i].width * (int)texSize.x);
                            buffer[i * 4 + 3] =
                                (float)Axiom.Math.Utility.Log2((int)this.atlasTableDatas[j][i].height * (int)texSize.y);
                        }
                        vsGpuParams.SetNamedConstant(this.vsTextureTable[j].Name, buffer.ToArray(),
                                                      this.atlasTableDatas[j].Count);
                    }
                }
            }
        }

        public override bool PreAddToRenderState(TargetRenderState targetRenderState, Pass srcPass, Pass dstPass)
        {
            this.atlasTexcoordPos = 0;
            TextureAtlasSamplerFactory factory = TextureAtlasSamplerFactory.Instance;

            bool hasAtlas = false;
            int texCount = srcPass.TextureUnitStatesCount;
            for (int i = 0; i < texCount; i++)
            {
                TextureUnitState pState = srcPass.GetTextureUnitState(i);

                var table = factory.GetTextureAtlasTable(pState.TextureName);
                if (table != null)
                {
                    if (table.Count > TextureAtlasSampler.MaxSafeAtlasedTextuers)
                    {
                        Axiom.Core.LogManager.Instance.Write(
                            "Warning: Compiling atlas texture has too many internally defined textures. Shader may fail to compile.");
                    }
                    if (i > TextureAtlasSampler.MaxTextures)
                    {
                        throw new Axiom.Core.AxiomException(
                            "Texture atlas sub-render does not support more than TextureAtlasSampler.MaxTextures {0} atlas textures",
                            TextureAtlasSampler.MaxTextures);
                    }
                    if (pState.TextureType != TextureType.TwoD)
                    {
                        throw new Axiom.Core.AxiomException("Texture atlas sub-render state only supports 2d textures.");
                    }

                    this.atlasTableDatas[i] = table;
                    this.textureAddressings[i] = pState.TextureAddressingMode;
                    this.isAtlasTextureUnits[i] = true;
                    hasAtlas = true;
                }
            }

            //Gather the materials atlas processing attributes
            //and calculate the position of the indexes
            TextureAtlasAttib attrib;
            factory.HasMaterialAtlasingAttributes(srcPass.Parent.Parent, out attrib);

            this.autoAdjustPollPosition = attrib.autoBorderAdjust;
            this.atlasTexcoordPos = attrib.positionOffset;
            if (attrib.positionMode == IndexPositionMode.Relative)
            {
                this.atlasTexcoordPos += texCount - 1;
            }

            return hasAtlas;
        }

        protected override bool ResolveParameters(ProgramSet programSet)
        {
            Program vsProgram = programSet.CpuVertexProgram;
            Program psProgram = programSet.CpuFragmentProgram;
            Function vsMain = vsProgram.EntryPointFunction;
            Function psMain = psProgram.EntryPointFunction;

            //Define vertex shader parameters used to find the positon of the textures in the atlas
            var indexContent =
                (Parameter.ContentType)((int)Parameter.ContentType.TextureCoordinate0 + this.atlasTexcoordPos);
            Axiom.Graphics.GpuProgramParameters.GpuConstantType indexType = GpuProgramParameters.GpuConstantType.Float4;

            this.vsInpTextureTableIndex = vsMain.ResolveInputParameter(Parameter.SemanticType.TextureCoordinates,
                                                                        this.atlasTexcoordPos, indexContent, indexType);

            //Define parameters to carry the information on the location of the texture from the vertex to the pixel shader
            for (int i = 0; i < TextureAtlasSampler.MaxTextures; i++)
            {
                if (this.isAtlasTextureUnits[i] == true)
                {
                    this.vsTextureTable[i] = vsProgram.ResolveParameter(GpuProgramParameters.GpuConstantType.Float4, -1,
                                                                           GpuProgramParameters.GpuParamVariability.Global,
                                                                           "AtlasData", this.atlasTableDatas[i].Count);
                    this.vsOutTextureDatas[i] = vsMain.ResolveOutputParameter(Parameter.SemanticType.TextureCoordinates,
                                                                                 -1, Parameter.ContentType.Unknown,
                                                                                 GpuProgramParameters.GpuConstantType.Float4);
                    this.psInpTextureData[i] = psMain.ResolveInputParameter(Parameter.SemanticType.TextureCoordinates,
                                                                               this.vsOutTextureDatas[i].Index,
                                                                               Parameter.ContentType.Unknown,
                                                                               GpuProgramParameters.GpuConstantType.Float4);
                    this.psTextureSizes[i] = psProgram.ResolveParameter(GpuProgramParameters.GpuConstantType.Float2, -1,
                                                                           GpuProgramParameters.GpuParamVariability.PerObject,
                                                                           "AtlasSize");
                }
            }

            return true;
        }

        protected override bool ResolveDependencies(ProgramSet programSet)
        {
            Program vsProgram = programSet.CpuVertexProgram;
            Program psProgram = programSet.CpuFragmentProgram;
            vsProgram.AddDependency(FFPRenderState.FFPLibCommon);
            psProgram.AddDependency(SGXLibTextureAtlas);
            return true;
        }

        protected override bool AddFunctionInvocations(ProgramSet programSet)
        {
            Program vsProgram = programSet.CpuVertexProgram;
            Program psProgram = programSet.CpuFragmentProgram;
            Function psMain = psProgram.EntryPointFunction;
            Function vsMain = vsProgram.EntryPointFunction;
            FunctionInvocation curFuncInvocation = null;
            //Calculate the position and size of the texture in the atlas in the vertex shader
            int groupOrder = ((int)FFPRenderState.FFPVertexShaderStage.VSTexturing -
                               (int)FFPRenderState.FFPVertexShaderStage.VSLighting) / 2;
            int internalCounter = 0;

            for (int i = 0; i < TextureAtlasSampler.MaxTextures; i++)
            {
                if (this.isAtlasTextureUnits[i] == true)
                {
                    Operand.OpMask textureIndexMask = Operand.OpMask.X;
                    switch (i)
                    {
                        case 1:
                            textureIndexMask = Operand.OpMask.Y;
                            break;
                        case 2:
                            textureIndexMask = Operand.OpMask.Z;
                            break;
                        case 3:
                            textureIndexMask = Operand.OpMask.W;
                            break;
                    }
                    curFuncInvocation = new FunctionInvocation(FFPRenderState.FFPFuncAssign, groupOrder,
                                                                internalCounter++);
                    curFuncInvocation.PushOperand(this.vsTextureTable[i], Operand.OpSemantic.In);
                    curFuncInvocation.PushOperand(this.vsInpTextureTableIndex, Operand.OpSemantic.In, (int)textureIndexMask,
                                                   1);
                    curFuncInvocation.PushOperand(this.vsOutTextureDatas[i], Operand.OpSemantic.Out);
                    vsMain.AddAtomInstance(curFuncInvocation);
                }
            }

            //sample the texture in the fragment shader given the extracted data in the pixel shader
            // groupOrder = (FFP_PS_SAMPLING + FFP_PS_TEXTURING) / 2;
            internalCounter = 0;

            var inpParams = psMain.InputParameters;
            var localParams = psMain.LocalParameters;

            Parameter psAtlasTextureCoord = psMain.ResolveLocalParameter(Parameter.SemanticType.Unknown, -1,
                                                                          "atlasCoord",
                                                                          GpuProgramParameters.GpuConstantType.Float2);

            for (int j = 0; j < TextureAtlasSampler.MaxTextures; j++)
            {
                if (this.isAtlasTextureUnits[j] == true)
                {
                    //Find the texture coordinates texel and sampler from the original FFPTexturing
                    Parameter texcoord = Function.GetParameterByContent(inpParams,
                                                                         (Parameter.ContentType)
                                                                         ((int)Parameter.ContentType.TextureCoordinate0 +
                                                                           j),
                                                                         GpuProgramParameters.GpuConstantType.Float2);
                    Parameter texel = Function.GetParameterByName(localParams, this.paramTexel + j.ToString());
                    UniformParameter sampler =
                        psProgram.GetParameterByType(GpuProgramParameters.GpuConstantType.Sampler2D, j);

                    //TODO
                    string addressUFuncName = GetAddressingFunctionName(this.textureAddressings[j].U);
                    string addressVFuncName = GetAddressingFunctionName(this.textureAddressings[j].V);

                    //Create a function which will replace the texel with the texture texel
                    if ((texcoord != null) && (texel != null) && (sampler != null) &&
                         (addressUFuncName != null) && (addressVFuncName != null))
                    {
                        //calculate the U value due to addressing mode
                        curFuncInvocation = new FunctionInvocation(addressUFuncName, groupOrder, internalCounter++);
                        curFuncInvocation.PushOperand(texcoord, Operand.OpSemantic.In, Operand.OpMask.X);
                        curFuncInvocation.PushOperand(psAtlasTextureCoord, Operand.OpSemantic.Out, Operand.OpMask.X);
                        psMain.AddAtomInstance(curFuncInvocation);

                        //calculate the V value due to addressing mode
                        curFuncInvocation = new FunctionInvocation(addressVFuncName, groupOrder, internalCounter++);
                        curFuncInvocation.PushOperand(texcoord, Operand.OpSemantic.In, Operand.OpMask.Y);
                        curFuncInvocation.PushOperand(psAtlasTextureCoord, Operand.OpSemantic.Out, Operand.OpMask.Y);
                        psMain.AddAtomInstance(curFuncInvocation);

                        //sample the texel color
                        curFuncInvocation =
                            new FunctionInvocation(this.autoAdjustPollPosition ? SGXFuncAtlasSampleAutoAdjust : SGXFuncAtlasSampleNormal,
                                                    groupOrder, internalCounter++);
                        curFuncInvocation.PushOperand(sampler, Operand.OpSemantic.In);
                        curFuncInvocation.PushOperand(texcoord, Operand.OpSemantic.In, (int)(Operand.OpMask.X | Operand.OpMask.Y));
                        curFuncInvocation.PushOperand(psAtlasTextureCoord, Operand.OpSemantic.In);
                        curFuncInvocation.PushOperand(this.psInpTextureData[j], Operand.OpSemantic.In);
                        curFuncInvocation.PushOperand(this.psTextureSizes[j], Operand.OpSemantic.In);
                        curFuncInvocation.PushOperand(texel, Operand.OpSemantic.Out);
                        psMain.AddAtomInstance(curFuncInvocation);
                    }
                }
            }

            return true;
        }

        protected string GetAddressingFunctionName(TextureAddressing mode)
        {
            switch (mode)
            {
                case TextureAddressing.Border:
                    return TextureAtlasSampler.SGXFuncAtlasBorder;
                case TextureAddressing.Clamp:
                    return TextureAtlasSampler.SGXFuncAtlasClamp;
                case TextureAddressing.Mirror:
                    return TextureAtlasSampler.SGXFuncAtlasMirror;
                case TextureAddressing.Wrap:
                    return TextureAtlasSampler.SGXFuncAtlasWrap;
                default:
                    return null;
            }
        }

        #endregion
    }
}