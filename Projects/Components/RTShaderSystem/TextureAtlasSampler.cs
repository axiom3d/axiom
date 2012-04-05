using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Axiom.Graphics;
using Axiom.Math;

namespace Axiom.Components.RTShaderSystem
{
    class TextureAtlasMap : Dictionary<string, List<TextureAtlasRecord>>
    { }
    public struct TextureAtlasRecord
    {
        public float posU, posV, width, height;
        public string originalTextureName;
        public string atlasTextureName;
        public int indexInAtlas;

        public TextureAtlasRecord(string texOriginalName, string texAtlasName, float texPosU, float texPosV, float texWidth, float texHeight, int texIndexInAtlas)
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
        static int MaxTextures = 4;
        public static int MaxSafeAtlasedTextuers = 250;
        Parameter vsInpTextureTableIndex;
        Axiom.Graphics.UVWAddressing[] textureAddressings = new Axiom.Graphics.UVWAddressing[TextureAtlasSampler.MaxTextures];
        Parameter[] vsOutTextureDatas = new Parameter[TextureAtlasSampler.MaxTextures];
        Parameter[] psInpTextureData = new Parameter[TextureAtlasSampler.MaxTextures];
        
        UniformParameter[] psTextureSizes = new UniformParameter[TextureAtlasSampler.MaxTextures];
        UniformParameter[] vsTextureTable = new UniformParameter[TextureAtlasSampler.MaxTextures];

        int atlasTexcoordPos;
        List<List<TextureAtlasRecord>> atlasTableDatas = new List<List<TextureAtlasRecord>>();
        bool[] isAtlasTextureUnits = new bool[TextureAtlasSampler.MaxTextures];
        bool isTableDataUpdated;
        bool autoAdjustPollPosition;

        public static string SGXType = "SGX_TextureAtlasSampler";

        static string SGXLibTextureAtlas = "SGXLib_TextureAtlas";

        static string SGXFuncAtlasSampleAutoAdjust = "SGX_Atlas_Sample_Auto_Adjust";
        static string SGXFuncAtlasSampleNormal = "SGX_Atlas_Sample_Normal";

        static string SGXFuncAtlasWrap = "SGX_Atlas_Wrap";
        static string SGXFuncAtlasMirror = "SGX_Atlas_Mirror";
        static string SGXFuncAtlasClamp = "SGX_Atlas_Clamp";
        static string SGXFuncAtlasBorder = "SGX_Atlas_Border";


        List<TextureAtlasRecord> blankAtlasTable;
        string paramTexel = "texel_";
        string RTAtlasKey = "RTAtlas"; 
        #endregion

        #region C'Tor
        public TextureAtlasSampler()
        {
            atlasTexcoordPos = 0;
            isTableDataUpdated = false;
            autoAdjustPollPosition = true;

            for (int i = 0; i < TextureAtlasSampler.MaxTextures; i++)
            {
                isAtlasTextureUnits[i] = false;
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
                //Return FFP_TEXTUREING + 25
                throw new NotImplementedException();
            }
        } 
        #endregion

        #region Methods
        public override void UpdateGpuProgramsParams(IRenderable rend, Pass pass, AutoParamDataSource source, Core.Collections.LightList lightList)
        {
            if (isTableDataUpdated == false)
            {
                isTableDataUpdated = true;
                for (int j = 0; j < TextureAtlasSampler.MaxTextures; j++)
                {
                    if (isAtlasTextureUnits[j] == true)
                    {
                        //Update the information of the size of the atlas textures
                        //TODO: Replace -1, -1 with actual dimensions
                        Axiom.Math.Tuple<int, int> texSizeInt = new Math.Tuple<int, int>(-1, -1);// = pass.GetTextureUnitState(j).Dimensions;
                        Vector2 texSize = new Vector2(texSizeInt.First, texSizeInt.Second);
                        psTextureSizes[j].SetGpuParameter(texSize);

                        //Update the information of which texture exists where in the atlas
                        GpuProgramParameters vsGpuParams = pass.VertexProgramParameters;
                        List<float> buffer = new List<float>(atlasTableDatas[j].Count * 4);
                        for (int i = 0; i < atlasTableDatas[j].Count; i++)
                        {
                            buffer[i * 4] = atlasTableDatas[j][i].posU;
                            buffer[i * 4 + 1] = atlasTableDatas[j][i].posV;
                            buffer[i * 4 + 2] = (float)Axiom.Math.Utility.Log2((int)atlasTableDatas[j][i].width * (int)texSize.x);
                            buffer[i * 4 + 3] = (float)Axiom.Math.Utility.Log2((int)atlasTableDatas[j][i].height * (int)texSize.y);
                        }
                        vsGpuParams.SetNamedConstant(vsTextureTable[j].Name, buffer.ToArray(), atlasTableDatas[j].Count);

                    }
                }
            }
        }
        public override bool PreAddToRenderState(TargetRenderState targetRenderState, Pass srcPass, Pass dstPass)
        {
            atlasTexcoordPos = 0;
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
                        Axiom.Core.LogManager.Instance.Write("Warning: Compiling atlas texture has too many internally defined textures. Shader may fail to compile.");

                    }
                    if (i > TextureAtlasSampler.MaxTextures)
                    {
                        throw new Axiom.Core.AxiomException("Texture atlas sub-render does not support more than TextureAtlasSampler.MaxTextures {0} atlas textures", TextureAtlasSampler.MaxTextures);
                    }
                    if (pState.TextureType != TextureType.TwoD)
                    {
                        throw new Axiom.Core.AxiomException("Texture atlas sub-render state only supports 2d textures.");
                    }

                    atlasTableDatas[i] = table;
                    textureAddressings[i] = pState.TextureAddressingMode;
                    isAtlasTextureUnits[i] = true;
                    hasAtlas = true;

                }
            }

            //Gather the materials atlas processing attributes
            //and calculate the position of the indexes
            TextureAtlasAttib attrib;
            factory.HasMaterialAtlasingAttributes(srcPass.Parent.Parent, out attrib);

            autoAdjustPollPosition = attrib.autoBorderAdjust;
            atlasTexcoordPos = attrib.positionOffset;
            if(attrib.positionMode == IndexPositionMode.Relative)
            {
                atlasTexcoordPos += texCount -1;
            }

            return hasAtlas;
        }

        internal override bool ResolveParameters(ProgramSet programSet)
        {
            Program vsProgram = programSet.CpuVertexProgram;
            Program psProgram = programSet.CpuFragmentProgram;
            Function vsMain = vsProgram.EntryPointFunction;
            Function psMain = psProgram.EntryPointFunction;

            //Define vertex shader parameters used to find the positon of the textures in the atlas
            Parameter.ContentType indexContent = (Parameter.ContentType)((int)Parameter.ContentType.TextureCoordinate0 + atlasTexcoordPos);
            Axiom.Graphics.GpuProgramParameters.GpuConstantType indexType = GpuProgramParameters.GpuConstantType.Float4;

            vsInpTextureTableIndex = vsMain.ResolveInputParameter(Parameter.SemanticType.TextureCoordinates, atlasTexcoordPos, indexContent, indexType);

            //Define parameters to carry the information on the location of the texture from the vertex to the pixel shader
            for (int i = 0; i < TextureAtlasSampler.MaxTextures; i++)
            {
                if (isAtlasTextureUnits[i] == true)
                {
                    vsTextureTable[i] = vsProgram.ResolveParameter(GpuProgramParameters.GpuConstantType.Float4, -1, GpuProgramParameters.GpuParamVariability.Global, "AtlasData", atlasTableDatas[i].Count);
                    vsOutTextureDatas[i] = vsMain.ResolveOutputParameter(Parameter.SemanticType.TextureCoordinates, -1, Parameter.ContentType.Unknown, GpuProgramParameters.GpuConstantType.Float4);
                    psInpTextureData[i] = psMain.ResolveInputParameter(Parameter.SemanticType.TextureCoordinates, vsOutTextureDatas[i].Index, Parameter.ContentType.Unknown, GpuProgramParameters.GpuConstantType.Float4);
                    psTextureSizes[i] = psProgram.ResolveParameter(GpuProgramParameters.GpuConstantType.Float2, -1, GpuProgramParameters.GpuParamVariability.PerObject, "AtlasSize");

                }
            }

            return true;
        }
        internal override bool ResolveDependencies(ProgramSet programSet)
        {
            Program vsProgram = programSet.CpuVertexProgram;
            Program psProgram = programSet.CpuFragmentProgram;
            vsProgram.AddDependency(FFPRenderState.FFPLibCommon);
            psProgram.AddDependency(SGXLibTextureAtlas);
            return true;
        }
        internal override bool AddFunctionInvocations(ProgramSet programSet)
        {
            Program vsProgram = programSet.CpuVertexProgram;
            Program psProgram = programSet.CpuFragmentProgram;
            Function psMain = psProgram.EntryPointFunction;
            Function vsMain = vsProgram.EntryPointFunction;
            FunctionInvocation curFuncInvocation = null;
            //Calculate the position and size of the texture in the atlas in the vertex shader
            int groupOrder = ((int)FFPRenderState.FFPVertexShaderStage.VSTexturing - (int)FFPRenderState.FFPVertexShaderStage.VSLighting) / 2;
            int internalCounter = 0;

            for (int i = 0; i < TextureAtlasSampler.MaxTextures; i++)
            {
                if (isAtlasTextureUnits[i] == true)
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
                        case 3: textureIndexMask = Operand.OpMask.W;
                            break;
                    }
                    curFuncInvocation = new FunctionInvocation(FFPRenderState.FFPFuncAssign, groupOrder, internalCounter++);
                    curFuncInvocation.PushOperand(vsTextureTable[i], Operand.OpSemantic.In);
                    curFuncInvocation.PushOperand(vsInpTextureTableIndex, Operand.OpSemantic.In, (int)textureIndexMask, 1);
                    curFuncInvocation.PushOperand(vsOutTextureDatas[i], Operand.OpSemantic.Out);
                    vsMain.AddAtomInstance(curFuncInvocation);

                }
            }

            //sample the texture in the fragment shader given the extracted data in the pixel shader
            // groupOrder = (FFP_PS_SAMPLING + FFP_PS_TEXTURING) / 2;
            internalCounter = 0;

            var inpParams = psMain.InputParameters;
            var localParams = psMain.LocalParameters;

            Parameter psAtlasTextureCoord = psMain.ResolveLocalParameter(Parameter.SemanticType.Unknown, -1, "atlasCoord", GpuProgramParameters.GpuConstantType.Float2);

            for (int j = 0; j < TextureAtlasSampler.MaxTextures; j++)
            {
                if (isAtlasTextureUnits[j] == true)
                {
                    //Find the texture coordinates texel and sampler from the original FFPTexturing
                    Parameter texcoord = Function.GetParameterByContent(inpParams, (Parameter.ContentType)((int)Parameter.ContentType.TextureCoordinate0 + j), GpuProgramParameters.GpuConstantType.Float2);
                    Parameter texel = Function.GetParameterByName(localParams, paramTexel + j.ToString());
                    UniformParameter sampler = psProgram.GetParameterByType(GpuProgramParameters.GpuConstantType.Sampler2D, j);

                    //TODO
                    char addressUFuncName = ' ';// = GetAddressingFunctionName(textureAddressings[j].u);
                    char addressVFuncName = ' ';// = GetAddressingFunctionName(textureAddressings[j].v);

                    //Create a function which will replace the texel with the texture texel
                    if ((texcoord != null) && (texel != null) && (sampler != null) && (addressUFuncName != null) && (addressVFuncName != null))
                    {
                        throw new NotImplementedException();
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
