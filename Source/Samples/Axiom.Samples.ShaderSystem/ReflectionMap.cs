using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Axiom.Components.RTShaderSystem;
using Axiom.Math;
using Axiom.Graphics;

namespace Axiom.Samples.ShaderSystem
{
    internal class ReflectionMap : SubRenderState
    {
        public static string SGXType = "SGX_ReflectionMap";
        private static string SGXLibReflectionMap = "SampleLib_ReflectionMap";
        private static string SGXFuncApplyReflectionMap = "SGX_ApplyReflectionMap";
        private string reflectionMapTextureName;
        private string maskMapTextureName;
        private int maskMapSamplerIndex;
        private int reflectionMapSamplerIndex;
        private Real reflectionPowerValue;
        private bool reflectionPowerChanged;
        private TextureType reflectionMapType;
        private UniformParameter maskMapSampler;
        private UniformParameter reflectionMapSampler;
        private UniformParameter reflectionPower;
        private Parameter vsInMaskTexcoord;
        private Parameter vsOutMaskTexcoord;
        private Parameter vsOutReflectionTexcoord;
        private Parameter psInMaskTexcoord;
        private Parameter psInReflectionTexcoord;

        private UniformParameter worldMatrix,
                                 worldITMatrix,
                                 viewMatrix;

        private Parameter vsInputNormal;
        private Parameter vsInputPos;
        private Parameter psOutDiffuse;

        public ReflectionMap()
        {
            this.maskMapSamplerIndex = 0;
            this.reflectionMapSamplerIndex = 0;
            this.reflectionMapType = TextureType.TwoD;
            this.reflectionPowerChanged = true;
            this.reflectionPowerValue = 0.05f;
        }

        public override bool PreAddToRenderState(TargetRenderState targetRenderState, Pass srcPass, Pass dstPass)
        {
            TextureUnitState textureUnit;

            //Create the mask texture unit.
            textureUnit = dstPass.CreateTextureUnitState();
            textureUnit.SetTextureName(this.maskMapTextureName);
            this.maskMapSamplerIndex = dstPass.TextureUnitStatesCount - 1;

            //Create the reflection texture unit.
            textureUnit = dstPass.CreateTextureUnitState();

            if (this.reflectionMapType == TextureType.TwoD)
            {
                textureUnit.SetTextureName(this.reflectionMapTextureName);
            }
            else
            {
                textureUnit.SetCubicTextureName(this.reflectionMapTextureName, true);
            }
            this.reflectionMapSamplerIndex = dstPass.TextureUnitStatesCount - 1;

            return true;
        }

        public override void
            UpdateGpuProgramsParams(IRenderable rend, Pass pass, AutoParamDataSource source,
                                     Core.Collections.LightList lightList)
        {
            if (this.reflectionPowerChanged)
            {
                GpuProgramParameters fsParams = pass.FragmentProgramParameters;

                this.reflectionPower.SetGpuParameter(this.reflectionPowerValue);
                this.reflectionPowerChanged = false;
            }
        }

        protected override bool ResolveParameters(ProgramSet programSet)
        {
            Program vsProgram = programSet.CpuVertexProgram;
            Program psProgram = programSet.CpuFragmentProgram;
            Function vsMain = vsProgram.EntryPointFunction;
            Function psMain = psProgram.EntryPointFunction;

            // Resolve vs input mask texture coordinates.
            // NOTE: We use the first texture coordinate hard coded here
            // You may want to parametrize this as well - just remember to add it to hash and copy methods. 
            this.vsInMaskTexcoord = vsMain.ResolveInputParameter(Parameter.SemanticType.TextureCoordinates, 0,
                                                                  Parameter.ContentType.TextureCoordinate0,
                                                                  GpuProgramParameters.GpuConstantType.Float2);
            if (this.vsInMaskTexcoord == null)
            {
                return false;
            }

            //Resolve vs output mask texture coordinates
            this.vsInMaskTexcoord = vsMain.ResolveInputParameter(Parameter.SemanticType.TextureCoordinates, 0,
                                                                  Parameter.ContentType.TextureCoordinate0,
                                                                  GpuProgramParameters.GpuConstantType.Float2);
            if (this.vsInMaskTexcoord == null)
            {
                return false;
            }

            //Resolve vs output mask texture coordinates.
            this.vsOutMaskTexcoord = vsMain.ResolveOutputParameter(Parameter.SemanticType.TextureCoordinates, -1,
                                                                    this.vsInMaskTexcoord.Content,
                                                                    GpuProgramParameters.GpuConstantType.Float2);
            if (this.vsInMaskTexcoord == null)
            {
                return false;
            }

            //Resolve ps input mask texture coordinates.
            this.psInMaskTexcoord = psMain.ResolveInputParameter(Parameter.SemanticType.TextureCoordinates,
                                                                  this.vsOutMaskTexcoord.Index,
                                                                  this.vsOutMaskTexcoord.Content,
                                                                  GpuProgramParameters.GpuConstantType.Float2);

            //Resolve vs output reflection texture coordinates
            this.vsOutReflectionTexcoord = vsMain.ResolveOutputParameter(Parameter.SemanticType.TextureCoordinates, -1,
                                                                          Parameter.ContentType.Unknown,
                                                                          (this.reflectionMapType == TextureType.TwoD)
                                                                              ? GpuProgramParameters.GpuConstantType.Float2
                                                                              : GpuProgramParameters.GpuConstantType.Float3);
            if (this.vsOutReflectionTexcoord == null)
            {
                return false;
            }

            //Resolve ps input reflection texture coordinates.
            this.psInReflectionTexcoord = psMain.ResolveInputParameter(Parameter.SemanticType.TextureCoordinates,
                                                                        this.vsOutReflectionTexcoord.Index,
                                                                        this.vsOutReflectionTexcoord.Content,
                                                                        this.vsOutReflectionTexcoord.Type);

            //Resolve world matrix.
            this.worldMatrix = vsProgram.ResolveAutoParameterInt(GpuProgramParameters.AutoConstantType.WorldMatrix, 0);
            if (this.worldMatrix == null)
            {
                return false;
            }

            this.worldITMatrix = vsProgram.ResolveAutoParameterInt(
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

            this.vsInputPos = vsMain.ResolveInputParameter(Parameter.SemanticType.Position, 0,
                                                            Parameter.ContentType.PositionObjectSpace,
                                                            GpuProgramParameters.GpuConstantType.Float4);
            if (this.vsInputPos == null)
            {
                return false;
            }

            this.vsInputNormal = vsMain.ResolveInputParameter(Parameter.SemanticType.Normal, 0,
                                                               Parameter.ContentType.NormalObjectSpace,
                                                               GpuProgramParameters.GpuConstantType.Float3);
            if (this.vsInputNormal == null)
            {
                return false;
            }

            this.maskMapSampler = psProgram.ResolveParameter(GpuProgramParameters.GpuConstantType.Sampler2D,
                                                              this.maskMapSamplerIndex,
                                                              GpuProgramParameters.GpuParamVariability.Global, "mask_sampler");
            if (this.maskMapSampler == null)
            {
                return false;
            }

            this.reflectionMapSampler =
                psProgram.ResolveParameter(
                    (this.reflectionMapType == TextureType.TwoD)
                        ? GpuProgramParameters.GpuConstantType.Sampler2D
                        : GpuProgramParameters.GpuConstantType.SamplerCube,
                    this.reflectionMapSamplerIndex, GpuProgramParameters.GpuParamVariability.Global, "reflection_texture");
            if (this.reflectionMapSampler == null)
            {
                return false;
            }

            this.reflectionPower = psProgram.ResolveParameter(GpuProgramParameters.GpuConstantType.Float1, -1,
                                                               GpuProgramParameters.GpuParamVariability.Global,
                                                               "reflection_power");
            if (this.reflectionPower == null)
            {
                return false;
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

        protected override bool ResolveDependencies(ProgramSet programSet)
        {
            Program vsProgram = programSet.CpuVertexProgram;
            Program psProgram = programSet.CpuFragmentProgram;

            vsProgram.AddDependency(FFPRenderState.FFPLibCommon);
            vsProgram.AddDependency(FFPRenderState.FFPLibTexturing);

            psProgram.AddDependency(FFPRenderState.FFPLibCommon);
            psProgram.AddDependency(FFPRenderState.FFPLibTexturing);
            psProgram.AddDependency(SGXLibReflectionMap);

            return true;
        }

        protected override bool AddFunctionInvocations(ProgramSet programSet)
        {
            Program vsProgram = programSet.CpuVertexProgram;
            Function vsMain = vsProgram.EntryPointFunction;
            Program psProgram = programSet.CpuFragmentProgram;
            Function psMain = psProgram.EntryPointFunction;

            //Add vertex shader invocations.
            if (!AddVsInvocations(vsMain, (int)FFPRenderState.FFPVertexShaderStage.VSTexturing + 1))
            {
                return false;
            }

            if (!AddPsInvocations(psMain, (int)FFPRenderState.FFPFragmentShaderStage.PSTexturing + 1))
            {
                return false;
            }

            return true;
        }

        private bool AddVsInvocations(Function vsMain, int groupOrder)
        {
            FunctionInvocation funcInvocation = null;
            int internalCounter = 0;

            //Output mask texgture coordinates
            funcInvocation = new FunctionInvocation(FFPRenderState.FFPFuncAssign, groupOrder, internalCounter++);
            funcInvocation.PushOperand(this.vsInMaskTexcoord, Operand.OpSemantic.In);
            funcInvocation.PushOperand(this.vsOutMaskTexcoord, Operand.OpSemantic.Out);
            vsMain.AddAtomInstance(funcInvocation);

            //Output reflection texture coordinates.
            if (this.reflectionMapType == TextureType.TwoD)
            {
                funcInvocation = new FunctionInvocation(FFPRenderState.FFPFunGenerateTexcoordEnvSphere, groupOrder,
                                                         internalCounter++);
                funcInvocation.PushOperand(this.worldITMatrix, Operand.OpSemantic.In);
                funcInvocation.PushOperand(this.viewMatrix, Operand.OpSemantic.In);
                funcInvocation.PushOperand(this.vsInputNormal, Operand.OpSemantic.In);
                funcInvocation.PushOperand(this.vsOutReflectionTexcoord, Operand.OpSemantic.Out);
                vsMain.AddAtomInstance(funcInvocation);
            }
            else
            {
                funcInvocation = new FunctionInvocation(FFPRenderState.FFPFuncGenerateTexCoordEnvReflect, groupOrder,
                                                         internalCounter++);
                funcInvocation.PushOperand(this.worldMatrix, Operand.OpSemantic.In);
                funcInvocation.PushOperand(this.worldITMatrix, Operand.OpSemantic.In);
                funcInvocation.PushOperand(this.viewMatrix, Operand.OpSemantic.In);
                funcInvocation.PushOperand(this.vsInputNormal, Operand.OpSemantic.In);
                funcInvocation.PushOperand(this.vsInputPos, Operand.OpSemantic.In);
                funcInvocation.PushOperand(this.vsOutReflectionTexcoord, Operand.OpSemantic.Out);
                vsMain.AddAtomInstance(funcInvocation);
            }

            return true;
        }

        private bool AddPsInvocations(Function psMain, int groupOrder)
        {
            FunctionInvocation funcInvocation = null;
            int internalCounter = 0;
            funcInvocation = new FunctionInvocation(SGXFuncApplyReflectionMap, groupOrder, internalCounter++);
            funcInvocation.PushOperand(this.maskMapSampler, Operand.OpSemantic.In);
            funcInvocation.PushOperand(this.psInMaskTexcoord, Operand.OpSemantic.In);
            funcInvocation.PushOperand(this.reflectionMapSampler, Operand.OpSemantic.In);
            funcInvocation.PushOperand(this.psInReflectionTexcoord, Operand.OpSemantic.In);
            funcInvocation.PushOperand(this.psOutDiffuse, Operand.OpSemantic.In,
                                        (int)(Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z));
            funcInvocation.PushOperand(this.reflectionPower, Operand.OpSemantic.In);
            funcInvocation.PushOperand(this.psOutDiffuse, Operand.OpSemantic.Out,
                                        (int)(Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z));

            psMain.AddAtomInstance(funcInvocation);

            return true;
        }

        public TextureType ReflectionMapType
        {
            get
            {
                return this.reflectionMapType;
            }
            set
            {
                this.reflectionMapType = value;
            }
        }

        public Real ReflectionPower
        {
            get
            {
                return this.reflectionPowerValue;
            }
            set
            {
                this.reflectionPowerValue = value;
                this.reflectionPowerChanged = true;
            }
        }

        public string ReflectionMapTextureName
        {
            get
            {
                return this.reflectionMapTextureName;
            }
            set
            {
                this.reflectionMapTextureName = value;
            }
        }

        public string MaskMapTextureName
        {
            get
            {
                return this.maskMapTextureName;
            }
            set
            {
                this.maskMapTextureName = value;
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
                return (int)FFPRenderState.FFPShaderStage.Texturing + 1;
            }
        }
    }

    internal class ReflectionMapFactory : SubRenderStateFactory
    {
        public override string Type
        {
            get
            {
                return ReflectionMap.SGXType;
            }
        }

        public override SubRenderState CreateInstance(Scripting.Compiler.ScriptCompiler compiler,
                                                       Scripting.Compiler.AST.PropertyAbstractNode prop, Pass pass,
                                                       ScriptTranslator stranslator)
        {
            if (prop.Name == "rtss_ext_reflection_map")
            {
                if (prop.Values.Count >= 2)
                {
                    string strValue;
                    int it = 0;

                    //Read reflection map type
                    if (!ScriptTranslator.GetString(prop.Values[it], out strValue))
                    {
                        //compiler.AddError(...)
                        return null;
                    }
                    it++;

                    SubRenderState subRenderState = CreateInstance();
                    var reflectionMapSubRenderState = subRenderState as ReflectionMap;

                    //Reflection map is cubic texture.
                    if (strValue == "cube_map")
                    {
                        reflectionMapSubRenderState.ReflectionMapType = TextureType.CubeMap;
                    }
                    else if (strValue == "2d_map")
                    {
                        reflectionMapSubRenderState.ReflectionMapType = TextureType.TwoD;
                    }

                    if (!ScriptTranslator.GetString(prop.Values[it], out strValue))
                    {
                        //compiler.AddError(...)
                        return null;
                    }
                    reflectionMapSubRenderState.MaskMapTextureName = strValue;
                    it++;

                    //read reflection texture
                    if (!ScriptTranslator.GetString(prop.Values[it], out strValue))
                    {
                        //compiler.AddError(...);
                        return null;
                    }
                    reflectionMapSubRenderState.ReflectionMapTextureName = strValue;
                    it++;

                    //Read reflection power value
                    Real reflectionPower = 0.5;
                    if (!ScriptTranslator.GetReal(prop.Values[it], out reflectionPower))
                    {
                        //compiler.AddError(...)
                        return null;
                    }
                    reflectionMapSubRenderState.ReflectionPower = reflectionPower;

                    return subRenderState;
                }
            }

            return null;
        }

        public override void WriteInstance(Serialization.MaterialSerializer ser, SubRenderState subRenderState, Pass srcPass,
                                            Pass dstPass)
        {
            //TODO
            //ser.WriteAttribute(4, "rtss_ext_reflection_map");

            //ReflectionMap reflectionMapSubRenderState = subRenderState as ReflectionMap;
            //if (reflectionMapSubRenderState.ReflectionMapType == TextureType.CubeMap)
            //{
            //    ser.WriteValue("cube_map");
            //}
            //else if (reflectionMapSubRenderState.ReflectionMapType == TextureType.TwoD)
            //{
            //    ser.WriteValue("2d_map");
            //}

            //ser.WriteValue(reflectionMapSubRenderState.MaskMapTextureName);
            //ser.WriteValue(reflectionMapSubRenderState.ReflectionMapTextureName);
            //ser.WriteValue(reflectionMapSubRenderState.ReflectionPower.ToString());
        }

        protected override SubRenderState CreateInstanceImpl()
        {
            return new ReflectionMap();
        }
    }
}