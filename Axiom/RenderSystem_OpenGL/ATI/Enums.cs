using System;
using Tao.OpenGl;

namespace RenderSystem_OpenGL.atifs
{
    /// <summary>
    ///    Rule symbol group types for ASM Pixel Shader 1.x instructions.
    /// </summary>
    [Flags]
    public enum RuleSymbol {
        Any = 0xffff,
        Register = 0x1,
        Constant = 0x2,
        Color = 0x4,
        Texture = 0x8,
        OpInstruction = 0x10,
        Mask = 0x20,
        TexSwizzle = 0x40,
        DestMod = 0x80,
        ArgMod = 0x100,
        NumVal = 0x200,
        Seperator = 0x400,
        TexRegister = 0x800,
        // Combined semantic rules to make bit fields used for semantic checks on each Token instruction
        ParameterLeft = OpInstruction | Seperator | DestMod | Mask | ArgMod,
        ParameterRight = Seperator | ArgMod | Mask | OpInstruction | DestMod,
        Argument = Register | Constant | Color | TexRegister,
        MaskRepLeft = Argument | ArgMod,
        MaskRepRight = ArgMod | Seperator | OpInstruction | DestMod,
        OpLeft = Argument | DestMod | Mask | ArgMod | NumVal | TexSwizzle | Texture,
        TempRegister = Register | TexRegister
    }

    /// <summary>
    /// 
    /// </summary>
    public enum Bits {
        RGB = 0x7,
        Alpha = 0x8
    }

    /// <summary>
    /// 
    /// </summary>
    [Flags]
    public enum ContextKeyPattern {
        Base,
        PS_1_1,
        PS_1_2,
        PS_1_3,
        PS_1_4,
        PS_1_4_Base = Base + PS_1_4
    }

    public enum RWAflags {
        None = 0, 
        Read = 1, 
        Write= 2
    }

    public enum MachineInstruction {
        ColorOp1, 
        ColorOp2, 
        ColorOp3, 
        AlphaOp1, 
        AlphaOp2,
        AlphaOp3, 
        SetConstants, 
        PassTexCoord, 
        SampleMap, 
        Tex,
        TexCoord, 
        TexReg2RGB, 
        Nop
    }

    public enum Symbol {
        // DirectX pixel shader source formats 
        PS_1_4, PS_1_1, PS_1_2, PS_1_3,
					
        // Base
        C0, C1, C2, C3, C4, C5, C6, C7,
        V0, V1,
        ADD, SUB, MUL, MAD, LRP, MOV, CMP, CND,
        DP3, DP4, DEF,
        R, RA, G, GA, B, BA, A, RGBA, RGB,
        RG, RGA, RB, RBA, GB, GBA,
        X2, X4, D2, SAT,
        BIAS, INVERT, NEGATE, BX2,
        COMMA, VALUE,

        //PS_1_4
        R0, R1, R2, R3, R4, R5,
        T0, T1, T2, T3, T4, T5,
        DP2ADD,
        X8, D8, D4,
        TEXCRD, TEXLD,
        STRDR, STQDQ,
        PHASE,

        //PS_1_1
        R0_1, R1_1, T0_1, T1_1, T2_1, T3_1,
        TEX, TEXCOORD, TEXM3X2PAD,
        TEXM3X2TEX, TEXM3X3PAD, TEXM3X3TEX, TEXM3X3SPEC, TEXM3X3VSPEC,
        TEXREG2AR, TEXREG2GB,
		
        // PS_1_2
        TEXREG2RGB, TEXDP3, TEXDP3TEX,

        // common
        Skip,
        Invalid = 999,

        // Base
        BASE_R = (R0 - Gl.GL_REG_0_ATI),
        BASE_C = (C0 - Gl.GL_CON_0_ATI),
        BASE_T = (T0_1 - Gl.GL_REG_0_ATI)
    }

    /// <summary>
    ///    There are 2 phases with 2 subphases each.
    /// </summary>
    public enum PhaseType {
        PHASE1TEX, 
        PHASE1ALU, 
        PHASE2TEX, 
        PHASE2ALU 
    }
}
