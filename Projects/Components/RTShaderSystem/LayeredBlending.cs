using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Axiom.Components.RTShaderSystem
{
    class LayeredBlending : FFPTexturing
    {
        #region TypeDefs
        public enum BlendMode
        {
            Invalid = -1,
            FFPBlend,
            Normal,
            Lighten,
            Darken,
            Multiply,
            Average,
            Add,
            Subtract,
            Difference,
            Negation,
            Exclusion,
            Screen,
            Overlay,
            SoftLight,
            HardLight,
            ColorDodge,
            ColorBurn,
            LinearDodge,
            LinearBurn,
            LinearLight,
            VividLight,
            PinLight,
            HardMix,
            Reflect,
            Glow,
            Phoenix,
            Saturation,
            Color,
            Luminosity,
            MaxBlendModes
        }

       public enum SourceModifier
        {
            Invalid = -1,
            None,
            Source1Modulate,
            Source2Modulate,
            Source1InvModulate,
            Source2InvModulate,
            MaxSourceModifiers
        }

        class TextureBlend
        {
            public BlendMode BlendMode = BlendMode.Invalid;
            public SourceModifier SourceModifier = SourceModifier.Invalid;
            public int CustomNum = 0;
            public Parameter ModControlParam = null;

            public TextureBlend()
            { }
            public TextureBlend(BlendMode blendMode, SourceModifier sourceModifier, int customNum)
            {
                this.BlendMode = blendMode;
                this.SourceModifier = sourceModifier;
                this.CustomNum = customNum;

            }
        }
        public struct BlendModeDescription
        {
            public BlendMode Type;
            public string Name;
            public string FuncName;
        }
        public struct SourceModifierDescription
        {
            public SourceModifier Type;
            public string Name;
            public string FuncName;
        }
        #endregion
        
        public static string LBType = "LayeredBlendRTSSEx";
        static string SGXLibLayeredBlending = "SGXLib_LayeredBlending";
        List<TextureBlend> textureBlends;
        public static BlendModeDescription[] blendModes = new BlendModeDescription[] 
        {
            new BlendModeDescription() { Type = BlendMode.FFPBlend, Name = "default", FuncName = ""},
            new BlendModeDescription() { Type = BlendMode.Normal, Name = "normal", FuncName = "SGX_blend_normal"},
            new BlendModeDescription() {Type = BlendMode.Lighten, Name = "lighten", FuncName = "SGX_blend_lighten"},
            new BlendModeDescription() {Type = BlendMode.Darken, Name = "darken", FuncName = "SGX_blend_darken"},
            new BlendModeDescription() {Type = BlendMode.Multiply, Name = "multiply", FuncName = "SGX_blend_multiply"},
            new BlendModeDescription() {Type = BlendMode.Average, Name = "average", FuncName = "SGX_blend_average"},
            new BlendModeDescription() {Type = BlendMode.Add, Name = "add", FuncName = "SGX_blend_add"},
            new BlendModeDescription() {Type = BlendMode.Subtract, Name ="subtract", FuncName = "SGX_blend_subtract"},
            new BlendModeDescription() {Type = BlendMode.Difference, Name = "difference", FuncName = "SGX_blend_difference"},
            new BlendModeDescription() {Type = BlendMode.Negation, Name = "negation", FuncName = "SGX_blend_negation"},
            new BlendModeDescription() {Type = BlendMode.Exclusion, Name = "exclusion", FuncName = "SGX_blend_exclusion"},
            new BlendModeDescription() {Type = BlendMode.Screen, Name = "screen", FuncName = "SGX_blend_screen"},
            new BlendModeDescription() {Type = BlendMode.Overlay, Name = "overlay", FuncName = "SGX_blend_overlay"},
            new BlendModeDescription() {Type = BlendMode.HardLight, Name = "hard_light", FuncName = "SGX_blend_hardLight"},
            new BlendModeDescription() {Type = BlendMode.SoftLight, Name = "soft_light", FuncName = "SGX_blend_softLight"},
            new BlendModeDescription() {Type = BlendMode.ColorDodge, Name = "color_dodge", FuncName = "SGX_blend_colorDodge"},
            new BlendModeDescription() {Type = BlendMode.ColorBurn, Name = "color_burn", FuncName = "SGX_blend_colorBurn"},
            new BlendModeDescription() {Type = BlendMode.LinearDodge, Name = "linear_dodge", FuncName = "SGX_blend_linearDodge"},
            new BlendModeDescription() {Type = BlendMode.LinearBurn, Name = "linear_burn", FuncName = "SGX_blend_linearBurn"},
            new BlendModeDescription() {Type = BlendMode.LinearLight, Name = "linear_light", FuncName = "SGX_blend_linearLight"},
            new BlendModeDescription() {Type = BlendMode.VividLight, Name = "vivid_light", FuncName = "SGX_blend_vividLight"},
            new BlendModeDescription() {Type = BlendMode.PinLight, Name = "pin_light", FuncName = "SGX_blend_pinLight"},
            new BlendModeDescription() {Type = BlendMode.HardMix, Name = "hard_mix", FuncName = "SGX_blend_hardMix"},
            new BlendModeDescription() {Type = BlendMode.Reflect, Name = "reflect", FuncName = "SGX_blend_reflect"},
            new BlendModeDescription() {Type = BlendMode.Glow, Name = "glow", FuncName = "SGX_blend_glow"},
            new BlendModeDescription() {Type = BlendMode.Phoenix, Name = "phoenix", FuncName = "SGX_blend_phoenix"},
            new BlendModeDescription() {Type = BlendMode.Saturation, Name = "saturation", FuncName = "SGX_blend_saturation"},
            new BlendModeDescription() {Type = BlendMode.Color, Name = "color", FuncName = "SGX_blend_color"},
            new BlendModeDescription() {Type = BlendMode.Luminosity, Name = "luminosity", FuncName = "SGX_blend_luminosity"},

        };

        public static SourceModifierDescription[] sourceModifiers = new SourceModifierDescription[]
        {
            new SourceModifierDescription() { Type = SourceModifier.None, Name = string.Empty, FuncName = string.Empty},
            new SourceModifierDescription() {Type = SourceModifier.Source1Modulate, Name = "src1_modulate", FuncName = string.Empty},
            new SourceModifierDescription() { Type = SourceModifier.Source2Modulate, Name = "src2_modulate", FuncName = string.Empty},
            new SourceModifierDescription() { Type = SourceModifier.Source1InvModulate, Name = "src1_inverse_modulate", FuncName = string.Empty},
            new SourceModifierDescription() { Type = SourceModifier.Source2InvModulate, Name = "src2_inverse__modulate", FuncName = string.Empty},
        };
        public LayeredBlending()
        { }

        public void SetBlendMode(int index, BlendMode mode)
        {
            textureBlends[index].BlendMode = mode;
        }
        public BlendMode GetBlendMode(int index)
        {
            if (index < textureBlends.Count)
                return textureBlends[index].BlendMode;

            return BlendMode.Invalid;
        }
        public void SetSourceModifier(int index, SourceModifier modType, int customNum)
        {
            if (index >= textureBlends.Count)
            {
                textureBlends.Add(new TextureBlend());
            }

            textureBlends[index].SourceModifier = modType;
            textureBlends[index].CustomNum = customNum;
        }
        public bool GetSourceModifier(int index, out SourceModifier sourceMod, out int customNum)
        {
            sourceMod = SourceModifier.Invalid;
            customNum = 0;
            if (index < textureBlends.Count)
            {
                sourceMod = textureBlends[index].SourceModifier;
                customNum = textureBlends[index].CustomNum;
            }

            return (sourceMod != SourceModifier.Invalid);
        }

        internal override bool ResolveParameters(ProgramSet programSet)
        {
            //resolve parameter for normal texturing procedures
            bool isSuccess = base.ResolveParameters(programSet);

            if (isSuccess)
            {
                //resolve source modification parameters
                Program psProgram = programSet.CpuFragmentProgram;

                for (int i = textureBlends.Count - 1; i >= 0; i--)
                {
                    TextureBlend texBlend = textureBlends[i];
                    if ((texBlend.SourceModifier != SourceModifier.Invalid) &&
                        (texBlend.SourceModifier != SourceModifier.None))
                    {
                        texBlend.ModControlParam = psProgram.ResolveAutoParameterInt(Graphics.GpuProgramParameters.AutoConstantType.Custom, texBlend.CustomNum);
                        if (texBlend.ModControlParam == null)
                        {
                            isSuccess = false;
                            break;
                        }
                    }
                }
            }

            return isSuccess;
        }
        internal override bool ResolveDependencies(ProgramSet programSet)
        {
           base.ResolveDependencies(programSet);
           Program psProgram = programSet.CpuFragmentProgram;
           psProgram.AddDependency(SGXLibLayeredBlending);
           return true;
        }
        protected override void AddPSBlendInvocations(Function psMain, Parameter arg1, Parameter arg2, Parameter texel, int samplerIndex, Graphics.LayerBlendModeEx blendMode, int groupOrder, ref int internalCounter, int targetChannels)
        {
            //Add the modifier invocation

            AddPSModifierInvocation(psMain, samplerIndex, arg1, arg2, groupOrder, ref internalCounter, targetChannels);

            //Add the blending fucntion invocations
            BlendMode mode = GetBlendMode(samplerIndex);

            if ((mode == BlendMode.FFPBlend) || (mode == BlendMode.Invalid))
            {
                base.AddPSBlendInvocations(psMain, arg1, arg2, texel, samplerIndex, blendMode, groupOrder, ref internalCounter, targetChannels);
            }
            else
            {
                //find the function name for the blend mode
                string funcName = string.Empty;

                for (int i = 0; i < blendModes.Length; i++)
                {
                    if (blendModes[i].Type == mode)
                    {
                        funcName = blendModes[i].FuncName;
                        break;
                    }
                }

                if (funcName != string.Empty)
                {
                    FunctionInvocation curFuncInvocation = new FunctionInvocation(funcName, groupOrder, internalCounter++);
                    curFuncInvocation.PushOperand(arg1, Operand.OpSemantic.In, targetChannels);
                    curFuncInvocation.PushOperand(arg2, Operand.OpSemantic.In, targetChannels);
                    curFuncInvocation.PushOperand(psOutDiffuse, Operand.OpSemantic.Out, targetChannels);
                    psMain.AddAtomInstance(curFuncInvocation);
                }
            }
        }
        private void AddPSModifierInvocation(Function psMain, int samplerIndex, Parameter arg1, Parameter arg2, int groupOrder, ref int internalCounter, int targetChanells)
        {
            SourceModifier modType;
            int customNum;
            if (GetSourceModifier(samplerIndex, out modType, out customNum) == true)
            {
                Parameter modifiedParam = null;
                string funcName = string.Empty;
                switch (modType)
                {
                    case SourceModifier.Source1Modulate:
                        funcName = "SGX_src_mod_modulate";
                        modifiedParam = arg1;
                        break;
                    case SourceModifier.Source2Modulate:
                        funcName = "SGX_src_mod_modulate";
                        modifiedParam = arg2;
                        break;
                    case SourceModifier.Source1InvModulate:
                        funcName = "SGX_src_mod_inv_modulate";
                        modifiedParam = arg1;
                        break;
                    case SourceModifier.Source2InvModulate:
                        funcName = "SGX_src_mod_inv_modulate";
                        modifiedParam = arg2;
                        break;
                    default:
                        break;
                }

                //add the function of the blend mode
                if (funcName != string.Empty)
                {
                    Parameter controlParam = textureBlends[samplerIndex].ModControlParam;

                    FunctionInvocation curFuncInvocation = new FunctionInvocation(funcName, groupOrder, internalCounter++);
                    curFuncInvocation.PushOperand(modifiedParam, Operand.OpSemantic.In, targetChanells);
                    curFuncInvocation.PushOperand(controlParam, Operand.OpSemantic.In, targetChanells);
                    curFuncInvocation.PushOperand(modifiedParam, Operand.OpSemantic.Out, targetChanells);
                    psMain.AddAtomInstance(curFuncInvocation);
                }
            }
        }
        public override string Type
        {
            get
            {
                return LayeredBlending.LBType;
            }
        }
    }
}
