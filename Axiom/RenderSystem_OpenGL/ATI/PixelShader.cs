/**
	A number of invaluable references were used to put together this ps.1.x compiler for ATI_fragment_shader execution

	References:
		1. MSDN: DirectX 8.1 Reference
		2. Wolfgang F. Engel "Fundamentals of Pixel Shaders - Introduction to Shader Programming Part III" on gamedev.net
		3. Martin Ecker - XEngine
		4. Shawn Kirst - ps14toATIfs
		5. Jason L. Mitchell "Real-Time 3D Graphics With Pixel Shaders" 
		6. Jason L. Mitchell "1.4 Pixel Shaders"
		7. Jason L. Mitchell and Evan Hart "Hardware Shading with EXT_vertex_shader and ATI_fragment_shader"
		6. ATI 8500 SDK
		7. GL_ATI_fragment_shader extension reference
*/

using System;
using Tao.OpenGl;

namespace Axiom.RenderSystems.OpenGL.ATI {
	/// <summary>
    ///     Subclasses Compiler2Pass to provide a ps_1_x compiler that takes DirectX pixel shader assembly
    ///     and converts it to a form that can be used by ATI_fragment_shader OpenGL API.
	/// </summary>
	/// <remarks>
	///     All ps_1_1, ps_1_2, ps_1_3, ps_1_4 assembly instructions are recognized but not all are passed
	///     on to ATI_fragment_shader.	ATI_fragment_shader does not have an equivelant directive for
	///     texkill or texdepth instructions.
	///     <p/>
	///     The user must provide the GL binding interfaces.
	///     <p/>
	///     A Test method is provided to verify the basic operation of the compiler which outputs the test
	///     results to a file.
	/// </remarks>
	public class PixelShader : Compiler2Pass {
        #region Static Fields

        static bool libInitialized = false;

        const int ALPHA_BIT = 0x08;
        const int RGB_BITS = 0x07;

        static SymbolDef[] PS_1_4_SymbolTypeLib = {
            new SymbolDef( Symbol.PS_1_4 , Gl.GL_NONE , ContextKeyPattern.PS_BASE , ContextKeyPattern.PS_1_4),
            new SymbolDef( Symbol.PS_1_1 , Gl.GL_NONE , ContextKeyPattern.PS_BASE , ContextKeyPattern.PS_1_1),
            new SymbolDef( Symbol.PS_1_2 , Gl.GL_NONE , ContextKeyPattern.PS_BASE , (uint)ContextKeyPattern.PS_1_2 + (uint)ContextKeyPattern.PS_1_1),
            new SymbolDef( Symbol.PS_1_3 , Gl.GL_NONE , ContextKeyPattern.PS_BASE , (uint)ContextKeyPattern.PS_1_3 + (uint)ContextKeyPattern.PS_1_2 + (uint)ContextKeyPattern.PS_1_1),
            new SymbolDef( Symbol.C0 , Gl.GL_CON_0_ATI , ContextKeyPattern.PS_BASE),
            new SymbolDef( Symbol.C1 , Gl.GL_CON_1_ATI , ContextKeyPattern.PS_BASE),
            new SymbolDef( Symbol.C2 , Gl.GL_CON_2_ATI , ContextKeyPattern.PS_BASE),
            new SymbolDef( Symbol.C3 , Gl.GL_CON_3_ATI , ContextKeyPattern.PS_BASE),
            new SymbolDef( Symbol.C4 , Gl.GL_CON_4_ATI , ContextKeyPattern.PS_BASE),
            new SymbolDef( Symbol.C5 , Gl.GL_CON_5_ATI , ContextKeyPattern.PS_BASE),
            new SymbolDef( Symbol.C6 , Gl.GL_CON_6_ATI , ContextKeyPattern.PS_BASE),
            new SymbolDef( Symbol.C7 , Gl.GL_CON_7_ATI , ContextKeyPattern.PS_BASE),
            new SymbolDef( Symbol.V0 , Gl.GL_PRIMARY_COLOR_ARB  , ContextKeyPattern.PS_BASE),
            new SymbolDef( Symbol.V1 , Gl.GL_SECONDARY_INTERPOLATOR_ATI  , ContextKeyPattern.PS_BASE),
            new SymbolDef( Symbol.ADD , Gl.GL_ADD_ATI , ContextKeyPattern.PS_BASE),
            new SymbolDef( Symbol.SUB , Gl.GL_SUB_ATI , ContextKeyPattern.PS_BASE),
            new SymbolDef( Symbol.MUL , Gl.GL_MUL_ATI , ContextKeyPattern.PS_BASE),
            new SymbolDef( Symbol.MAD , Gl.GL_MAD_ATI , ContextKeyPattern.PS_BASE),
            new SymbolDef( Symbol.LRP , Gl.GL_LERP_ATI , ContextKeyPattern.PS_BASE),
            new SymbolDef( Symbol.MOV , Gl.GL_MOV_ATI , ContextKeyPattern.PS_BASE),
            new SymbolDef( Symbol.CMP , Gl.GL_CND0_ATI , ContextKeyPattern.PS_BASE),
            new SymbolDef( Symbol.CND , Gl.GL_CND_ATI , ContextKeyPattern.PS_BASE),
            new SymbolDef( Symbol.DP3 , Gl.GL_DOT3_ATI , ContextKeyPattern.PS_BASE),
            new SymbolDef( Symbol.DP4 , Gl.GL_DOT4_ATI , ContextKeyPattern.PS_BASE),
            new SymbolDef( Symbol.DEF , Gl.GL_NONE , ContextKeyPattern.PS_BASE),
            new SymbolDef( Symbol.R , Gl.GL_RED_BIT_ATI , ContextKeyPattern.PS_1_4),
            new SymbolDef( Symbol.RA , Gl.GL_RED_BIT_ATI | ALPHA_BIT , ContextKeyPattern.PS_1_4),
            new SymbolDef( Symbol.G , Gl.GL_GREEN_BIT_ATI , ContextKeyPattern.PS_1_4),
            new SymbolDef( Symbol.GA , Gl.GL_GREEN_BIT_ATI | ALPHA_BIT , ContextKeyPattern.PS_1_4),
            new SymbolDef( Symbol.B , Gl.GL_BLUE_BIT_ATI , ContextKeyPattern.PS_1_4),
            new SymbolDef( Symbol.BA , Gl.GL_BLUE_BIT_ATI | ALPHA_BIT , ContextKeyPattern.PS_1_4),
            new SymbolDef( Symbol.A, ALPHA_BIT , ContextKeyPattern.PS_BASE),
            new SymbolDef( Symbol.RGBA, RGB_BITS | ALPHA_BIT , ContextKeyPattern.PS_BASE),
            new SymbolDef( Symbol.RGB, RGB_BITS  , ContextKeyPattern.PS_BASE),
            new SymbolDef( Symbol.RG , Gl.GL_RED_BIT_ATI | Gl.GL_GREEN_BIT_ATI , ContextKeyPattern.PS_1_4),
            new SymbolDef( Symbol.RGA , Gl.GL_RED_BIT_ATI | Gl.GL_GREEN_BIT_ATI | ALPHA_BIT , ContextKeyPattern.PS_1_4),
            new SymbolDef( Symbol.RB , Gl.GL_RED_BIT_ATI | Gl.GL_BLUE_BIT_ATI , ContextKeyPattern.PS_1_4),
            new SymbolDef( Symbol.RBA , Gl.GL_RED_BIT_ATI | Gl.GL_BLUE_BIT_ATI | ALPHA_BIT , ContextKeyPattern.PS_1_4),
            new SymbolDef( Symbol.GB , Gl.GL_GREEN_BIT_ATI | Gl.GL_BLUE_BIT_ATI , ContextKeyPattern.PS_1_4),
            new SymbolDef( Symbol.GBA , Gl.GL_GREEN_BIT_ATI | Gl.GL_BLUE_BIT_ATI | ALPHA_BIT , ContextKeyPattern.PS_1_4),
            new SymbolDef( Symbol.RRRR , Gl.GL_RED , ContextKeyPattern.PS_1_4),
            new SymbolDef( Symbol.GGGG , Gl.GL_GREEN , ContextKeyPattern.PS_1_4),
            new SymbolDef( Symbol.BBBB , Gl.GL_BLUE , ContextKeyPattern.PS_BASE),
            new SymbolDef( Symbol.AAAA , Gl.GL_ALPHA , ContextKeyPattern.PS_BASE),
            new SymbolDef( Symbol.X2 , Gl.GL_2X_BIT_ATI , ContextKeyPattern.PS_BASE),
            new SymbolDef(Symbol.X4 , Gl.GL_4X_BIT_ATI , ContextKeyPattern.PS_BASE),
            new SymbolDef( Symbol.D2 , Gl.GL_HALF_BIT_ATI , ContextKeyPattern.PS_BASE),
            new SymbolDef(Symbol.SAT , Gl.GL_SATURATE_BIT_ATI , ContextKeyPattern.PS_BASE),
            new SymbolDef( Symbol.BIAS , Gl.GL_BIAS_BIT_ATI , ContextKeyPattern.PS_BASE),
            new SymbolDef(Symbol.INVERT , Gl.GL_COMP_BIT_ATI , ContextKeyPattern.PS_BASE),
            new SymbolDef( Symbol.NEGATE , Gl.GL_NEGATE_BIT_ATI , ContextKeyPattern.PS_BASE),
            new SymbolDef(Symbol.BX2 , Gl.GL_2X_BIT_ATI | Gl.GL_BIAS_BIT_ATI , ContextKeyPattern.PS_BASE),
            new SymbolDef( Symbol.COMMA , Gl.GL_NONE , ContextKeyPattern.PS_BASE),
            new SymbolDef(Symbol.VALUE , Gl.GL_NONE , ContextKeyPattern.PS_BASE),
            new SymbolDef( Symbol.R0 , Gl.GL_REG_0_ATI , ContextKeyPattern.PS_1_4),
            new SymbolDef(Symbol.R1 , Gl.GL_REG_1_ATI , ContextKeyPattern.PS_1_4),
            new SymbolDef(Symbol.R2 , Gl.GL_REG_2_ATI , ContextKeyPattern.PS_1_4),
            new SymbolDef( Symbol.R3 , Gl.GL_REG_3_ATI , ContextKeyPattern.PS_1_4),
            new SymbolDef(Symbol.R4 , Gl.GL_REG_4_ATI , ContextKeyPattern.PS_1_4),
            new SymbolDef(Symbol.R5 , Gl.GL_REG_5_ATI , ContextKeyPattern.PS_1_4),
            new SymbolDef( Symbol.T0 , Gl.GL_TEXTURE0_ARB , ContextKeyPattern.PS_1_4),
            new SymbolDef(Symbol.T1 , Gl.GL_TEXTURE1_ARB , ContextKeyPattern.PS_1_4),
            new SymbolDef(Symbol.T2 , Gl.GL_TEXTURE2_ARB , ContextKeyPattern.PS_1_4),
            new SymbolDef( Symbol.T3 , Gl.GL_TEXTURE3_ARB , ContextKeyPattern.PS_1_4),
            new SymbolDef(Symbol.T4 , Gl.GL_TEXTURE4_ARB , ContextKeyPattern.PS_1_4),
            new SymbolDef(Symbol.T5 , Gl.GL_TEXTURE5_ARB , ContextKeyPattern.PS_1_4),
            new SymbolDef( Symbol.DP2ADD , Gl.GL_DOT2_ADD_ATI , ContextKeyPattern.PS_1_4),
            new SymbolDef( Symbol.X8 , Gl.GL_8X_BIT_ATI , ContextKeyPattern.PS_1_4),
            new SymbolDef(Symbol.D8 , Gl.GL_EIGHTH_BIT_ATI , ContextKeyPattern.PS_1_4),
            new SymbolDef(Symbol.D4 , Gl.GL_QUARTER_BIT_ATI , ContextKeyPattern.PS_1_4),
            new SymbolDef( Symbol.TEXCRD , Gl.GL_NONE , ContextKeyPattern.PS_1_4),
            new SymbolDef(Symbol.TEXLD , Gl.GL_NONE , ContextKeyPattern.PS_1_4),
            new SymbolDef( Symbol.STR , Gl.GL_SWIZZLE_STR_ATI - Gl.GL_SWIZZLE_STR_ATI , ContextKeyPattern.PS_1_4),
            new SymbolDef( Symbol.STQ , Gl.GL_SWIZZLE_STQ_ATI - Gl.GL_SWIZZLE_STR_ATI , ContextKeyPattern.PS_1_4),
            new SymbolDef( Symbol.STRDR , Gl.GL_SWIZZLE_STR_DR_ATI - Gl.GL_SWIZZLE_STR_ATI , ContextKeyPattern.PS_1_4),
            new SymbolDef( Symbol.STQDQ , Gl.GL_SWIZZLE_STQ_DQ_ATI - Gl.GL_SWIZZLE_STR_ATI , ContextKeyPattern.PS_1_4),
            new SymbolDef(  Symbol.BEM , Gl.GL_NONE , ContextKeyPattern.PS_1_4),
            new SymbolDef( Symbol.PHASE , Gl.GL_NONE , ContextKeyPattern.PS_1_4),
            new SymbolDef( Symbol.R0_1 , Gl.GL_REG_4_ATI , ContextKeyPattern.PS_1_1),
            new SymbolDef(Symbol.R1_1 , Gl.GL_REG_5_ATI , ContextKeyPattern.PS_1_1),
            new SymbolDef( Symbol.T0_1 , Gl.GL_REG_0_ATI , ContextKeyPattern.PS_1_1),
            new SymbolDef( Symbol.T1_1 , Gl.GL_REG_1_ATI , ContextKeyPattern.PS_1_1),
            new SymbolDef(Symbol.T2_1, Gl.GL_REG_2_ATI , ContextKeyPattern.PS_1_1),
            new SymbolDef(Symbol.T3_1 , Gl.GL_REG_3_ATI , ContextKeyPattern.PS_1_1),
            new SymbolDef( Symbol.TEX , Gl.GL_NONE , ContextKeyPattern.PS_1_1),
            new SymbolDef(Symbol.TEXCOORD , Gl.GL_NONE , ContextKeyPattern.PS_1_1),
            new SymbolDef(Symbol.TEXM3X2PAD , Gl.GL_NONE , ContextKeyPattern.PS_1_1),
            new SymbolDef( Symbol.TEXM3X2TEX , Gl.GL_NONE , ContextKeyPattern.PS_1_1),
            new SymbolDef(Symbol.TEXM3X3PAD , Gl.GL_NONE , ContextKeyPattern.PS_1_1),
            new SymbolDef(Symbol.TEXM3X3TEX , Gl.GL_NONE , ContextKeyPattern.PS_1_1),
            new SymbolDef(Symbol.TEXM3X3SPEC , Gl.GL_NONE , ContextKeyPattern.PS_1_1),
            new SymbolDef(Symbol.TEXM3X3VSPEC , Gl.GL_NONE , ContextKeyPattern.PS_1_1),
            new SymbolDef(Symbol.TEXREG2AR , Gl.GL_NONE , ContextKeyPattern.PS_1_2),
            new SymbolDef( Symbol.TEXREG2GB , Gl.GL_NONE , ContextKeyPattern.PS_1_2),
            new SymbolDef( Symbol.TEXREG2RGB , Gl.GL_NONE , ContextKeyPattern.PS_1_2),
            new SymbolDef(Symbol.TEXDP3 , Gl.GL_NONE , ContextKeyPattern.PS_1_2),
            new SymbolDef(Symbol.TEXDP3TEX , Gl.GL_NONE , ContextKeyPattern.PS_1_2),
            new SymbolDef( Symbol.SKIP , Gl.GL_NONE , ContextKeyPattern.PS_BASE),
            new SymbolDef(Symbol.PLUS , Gl.GL_NONE , ContextKeyPattern.PS_BASE),
            new SymbolDef( Symbol.PROGRAM , Gl.GL_NONE , ContextKeyPattern.PS_BASE),
            new SymbolDef( Symbol.PROGRAMTYPE , Gl.GL_NONE , ContextKeyPattern.PS_BASE),
            new SymbolDef( Symbol.DECLCONSTS , Gl.GL_NONE , ContextKeyPattern.PS_BASE),
            new SymbolDef( Symbol.DEFCONST , Gl.GL_NONE , ContextKeyPattern.PS_BASE),
            new SymbolDef( Symbol.CONSTANT , Gl.GL_NONE , ContextKeyPattern.PS_BASE),
            new SymbolDef( Symbol.COLOR , Gl.GL_NONE , ContextKeyPattern.PS_BASE),
            new SymbolDef(  Symbol.TEXSWIZZLE , Gl.GL_NONE , ContextKeyPattern.PS_BASE),
            new SymbolDef( Symbol.UNARYOP , Gl.GL_NONE , ContextKeyPattern.PS_BASE),
            new SymbolDef( Symbol.NUMVAL , Gl.GL_NONE , ContextKeyPattern.PS_BASE),
            new SymbolDef( Symbol.SEPERATOR , Gl.GL_NONE , ContextKeyPattern.PS_BASE),
            new SymbolDef( Symbol.ALUOPS , Gl.GL_NONE , ContextKeyPattern.PS_BASE),
            new SymbolDef( Symbol.TEXMASK , Gl.GL_NONE , ContextKeyPattern.PS_BASE),
            new SymbolDef( Symbol.TEXOP_PS1_1_3 , Gl.GL_NONE , ContextKeyPattern.PS_1_1),
            new SymbolDef(  Symbol.TEXOP_PS1_4 , Gl.GL_NONE , ContextKeyPattern.PS_1_4),
            new SymbolDef( Symbol.ALU_STATEMENT , Gl.GL_NONE , ContextKeyPattern.PS_BASE),
            new SymbolDef( Symbol.DSTMODSAT , Gl.GL_NONE , ContextKeyPattern.PS_BASE),
            new SymbolDef( Symbol.UNARYOP_ARGS , Gl.GL_NONE , ContextKeyPattern.PS_BASE),
            new SymbolDef( Symbol.REG_PS1_4 , Gl.GL_NONE , ContextKeyPattern.PS_1_4),
            new SymbolDef( Symbol.TEX_PS1_4 , Gl.GL_NONE , ContextKeyPattern.PS_1_4),
            new SymbolDef( Symbol.REG_PS1_1_3 , Gl.GL_NONE , ContextKeyPattern.PS_1_1),
            new SymbolDef( Symbol.TEX_PS1_1_3 , Gl.GL_NONE , ContextKeyPattern.PS_1_1),
            new SymbolDef( Symbol.DSTINFO , Gl.GL_NONE , ContextKeyPattern.PS_BASE),
            new SymbolDef( Symbol.SRCINFO , Gl.GL_NONE , ContextKeyPattern.PS_BASE),
            new SymbolDef( Symbol.BINARYOP_ARGS , Gl.GL_NONE , ContextKeyPattern.PS_BASE),
            new SymbolDef( Symbol.TERNARYOP_ARGS , Gl.GL_NONE , ContextKeyPattern.PS_BASE),
            new SymbolDef( Symbol.TEMPREG , Gl.GL_NONE , ContextKeyPattern.PS_BASE),
            new SymbolDef( Symbol.DSTMASK , Gl.GL_NONE , ContextKeyPattern.PS_BASE),
            new SymbolDef( Symbol.PRESRCMOD , Gl.GL_NONE , ContextKeyPattern.PS_BASE),
            new SymbolDef( Symbol.SRCNAME , Gl.GL_NONE , ContextKeyPattern.PS_BASE),
            new SymbolDef( Symbol.SRCREP , Gl.GL_NONE , ContextKeyPattern.PS_BASE),
            new SymbolDef( Symbol.POSTSRCMOD , Gl.GL_NONE , ContextKeyPattern.PS_BASE),
            new SymbolDef( Symbol.DSTMOD , Gl.GL_NONE , ContextKeyPattern.PS_BASE),
            new SymbolDef( Symbol.DSTSAT , Gl.GL_NONE , ContextKeyPattern.PS_BASE),
            new SymbolDef( Symbol.BINARYOP , Gl.GL_NONE , ContextKeyPattern.PS_BASE),
            new SymbolDef( Symbol.TERNARYOP , Gl.GL_NONE , ContextKeyPattern.PS_BASE),
            new SymbolDef( Symbol.TEXOPS_PHASE1 , Gl.GL_NONE , ContextKeyPattern.PS_BASE),
            new SymbolDef( Symbol.COISSUE , Gl.GL_NONE , ContextKeyPattern.PS_BASE),
            new SymbolDef( Symbol.PHASEMARKER , Gl.GL_NONE , ContextKeyPattern.PS_1_4),
            new SymbolDef( Symbol.TEXOPS_PHASE2 , Gl.GL_NONE , ContextKeyPattern.PS_1_4),
            new SymbolDef( Symbol.TEXREG_PS1_4 , Gl.GL_NONE , ContextKeyPattern.PS_1_4),
            new SymbolDef( Symbol.TEXOPS_PS1_4 , Gl.GL_NONE , ContextKeyPattern.PS_1_4),
            new SymbolDef( Symbol.TEXOPS_PS1_1_3 , Gl.GL_NONE , ContextKeyPattern.PS_1_1),
            new SymbolDef( Symbol.TEXCISCOP_PS1_1_3 , Gl.GL_NONE , ContextKeyPattern.PS_1_1)                                     
        };

        static TokenRule[] PS_1_x_RulePath = {
            new TokenRule(OperationType.Rule,  Symbol.PROGRAM, "Program"),
            new TokenRule(OperationType.And,  Symbol.PROGRAMTYPE),
            new TokenRule(OperationType.Optional,  Symbol.DECLCONSTS),
            new TokenRule(OperationType.Optional,  Symbol.TEXOPS_PHASE1),
            new TokenRule(OperationType.Optional,  Symbol.ALUOPS ),
            new TokenRule(OperationType.Optional,  Symbol.PHASEMARKER),
            new TokenRule(OperationType.Optional,  Symbol.TEXOPS_PHASE2),
            new TokenRule(OperationType.Optional,  Symbol.ALUOPS),
            new TokenRule(OperationType.End ),
            new TokenRule(OperationType.Rule,  Symbol.PROGRAMTYPE, "<ProgramType>"),
            new TokenRule(OperationType.And,  Symbol.PS_1_4, "ps.1.4"),
            new TokenRule(OperationType.Or,  Symbol.PS_1_1, "ps.1.1"),
            new TokenRule(OperationType.Or,  Symbol.PS_1_2, "ps.1.2"),
            new TokenRule(OperationType.Or,  Symbol.PS_1_3, "ps.1.3"),
            new TokenRule(OperationType.End),
            new TokenRule(OperationType.Rule,  Symbol.PHASEMARKER, "<PhaseMarker>"),
            new TokenRule(OperationType.And,  Symbol.PHASE, "phase"),
            new TokenRule(OperationType.End),
            new TokenRule(OperationType.Rule,  Symbol.DECLCONSTS, "<DeclareConstants>"),
            new TokenRule(OperationType.Repeat,  Symbol.DEFCONST),
            new TokenRule(OperationType.End),
            new TokenRule(OperationType.Rule,  Symbol.TEXOPS_PHASE1, "<TexOps_Phase1>"),
            new TokenRule(OperationType.And,  Symbol.TEXOPS_PS1_1_3),
            new TokenRule(OperationType.Or,  Symbol.TEXOPS_PS1_4),
            new TokenRule(OperationType.End),
            new TokenRule(OperationType.Rule,  Symbol.TEXOPS_PHASE2, "<TexOps_Phase2>"),
            new TokenRule(OperationType.And,  Symbol.TEXOPS_PS1_4),
            new TokenRule(OperationType.End),
            new TokenRule(OperationType.Rule,  Symbol.NUMVAL, "<NumVal>"),
            new TokenRule(OperationType.And,  Symbol.VALUE, "Float Value"),
            new TokenRule(OperationType.End),
            new TokenRule(OperationType.Rule,  Symbol.TEXOPS_PS1_1_3, "<TexOps_PS1_1_3>"),
            new TokenRule(OperationType.Repeat,  Symbol.TEXOP_PS1_1_3),
            new TokenRule(OperationType.End),
            new TokenRule(OperationType.Rule,  Symbol.TEXOPS_PS1_4, "<TexOps_PS1_4>"),
            new TokenRule(OperationType.Repeat,  Symbol.TEXOP_PS1_4),
            new TokenRule(OperationType.End),
            new TokenRule(OperationType.Rule,  Symbol.TEXOP_PS1_1_3, "<TexOp_PS1_1_3>"),
            new TokenRule(OperationType.And,   Symbol.TEXCISCOP_PS1_1_3),
            new TokenRule(OperationType.And,  Symbol.TEX_PS1_1_3),
            new TokenRule(OperationType.And,  Symbol.SEPERATOR),
            new TokenRule(OperationType.And,  Symbol.TEX_PS1_1_3),
            new TokenRule(OperationType.Or,   Symbol.TEXCOORD, "texcoord"),
            new TokenRule(OperationType.And,  Symbol.TEX_PS1_1_3),
            new TokenRule(OperationType.Or,   Symbol.TEX, "tex"),
            new TokenRule(OperationType.And,  Symbol.TEX_PS1_1_3),
            new TokenRule(OperationType.End),
            new TokenRule(OperationType.Rule,  Symbol.TEXOP_PS1_4, "<TexOp_PS1_4>"),
            new TokenRule(OperationType.And,   Symbol.TEXCRD, "texcrd"),
            new TokenRule(OperationType.And,  Symbol.REG_PS1_4),
            new TokenRule(OperationType.Optional,  Symbol.TEXMASK),
            new TokenRule(OperationType.And,  Symbol.SEPERATOR),
            new TokenRule(OperationType.And,  Symbol.TEXREG_PS1_4),
            new TokenRule(OperationType.Or,   Symbol.TEXLD, "texld"),
            new TokenRule(OperationType.And,  Symbol.REG_PS1_4),
            new TokenRule(OperationType.Optional,  Symbol.TEXMASK),
            new TokenRule(OperationType.And,  Symbol.SEPERATOR),
            new TokenRule(OperationType.And,  Symbol.TEXREG_PS1_4 ),
            new TokenRule(OperationType.End),
            new TokenRule(OperationType.Rule,  Symbol.ALUOPS, "<ALUOps>"),
            new TokenRule(OperationType.Repeat,  Symbol.ALU_STATEMENT),
            new TokenRule(OperationType.End),
            new TokenRule(OperationType.Rule,  Symbol.ALU_STATEMENT, "<ALUStatement>"),
            new TokenRule(OperationType.And,  Symbol.COISSUE),
            new TokenRule(OperationType.And,  Symbol.UNARYOP),
            new TokenRule(OperationType.Optional,  Symbol.DSTMODSAT),
            new TokenRule(OperationType.And,  Symbol.UNARYOP_ARGS ),
            new TokenRule(OperationType.Or,  Symbol.COISSUE),
            new TokenRule(OperationType.And,  Symbol.BINARYOP),
            new TokenRule(OperationType.Optional,  Symbol.DSTMODSAT),
            new TokenRule(OperationType.And,  Symbol.BINARYOP_ARGS),
            new TokenRule(OperationType.Or,  Symbol.COISSUE),
            new TokenRule(OperationType.And,  Symbol.TERNARYOP),
            new TokenRule(OperationType.Optional,  Symbol.DSTMODSAT),
            new TokenRule(OperationType.And,  Symbol.TERNARYOP_ARGS ),
            new TokenRule(OperationType.End),
            new TokenRule(OperationType.Rule,  Symbol.TEXREG_PS1_4, "<TexReg_PS1_4>"),
            new TokenRule(OperationType.And,  Symbol.TEX_PS1_4  ),
            new TokenRule(OperationType.Optional,  Symbol.TEXSWIZZLE),
            new TokenRule(OperationType.Or,  Symbol.REG_PS1_4  ),
            new TokenRule(OperationType.Optional,  Symbol.TEXSWIZZLE),
            new TokenRule(OperationType.End),
            new TokenRule(OperationType.Rule,  Symbol.UNARYOP_ARGS, "<UnaryOpArgs>"),
            new TokenRule(OperationType.And,   Symbol.DSTINFO),
            new TokenRule(OperationType.And,  Symbol.SRCINFO),
            new TokenRule(OperationType.End),
            new TokenRule(OperationType.Rule,  Symbol.BINARYOP_ARGS, "<BinaryOpArgs>"),
            new TokenRule(OperationType.And,   Symbol.DSTINFO),
            new TokenRule(OperationType.And,  Symbol.SRCINFO),
            new TokenRule(OperationType.And,  Symbol.SRCINFO),
            new TokenRule(OperationType.End),
            new TokenRule(OperationType.Rule,  Symbol.TERNARYOP_ARGS, "<TernaryOpArgs>"),
            new TokenRule(OperationType.And,   Symbol.DSTINFO),
            new TokenRule(OperationType.And,  Symbol.SRCINFO),
            new TokenRule(OperationType.And,  Symbol.SRCINFO),
            new TokenRule(OperationType.And,  Symbol.SRCINFO),
            new TokenRule(OperationType.End),
            new TokenRule(OperationType.Rule,  Symbol.DSTINFO, "<DstInfo>"),
            new TokenRule(OperationType.And,  Symbol.TEMPREG),
            new TokenRule(OperationType.Optional,  Symbol.DSTMASK),
            new TokenRule(OperationType.End),
            new TokenRule(OperationType.Rule,  Symbol.SRCINFO, "<SrcInfo>"),
            new TokenRule(OperationType.And,  Symbol.SEPERATOR),
            new TokenRule(OperationType.Optional,  Symbol.PRESRCMOD),
            new TokenRule(OperationType.And,  Symbol.SRCNAME),
            new TokenRule(OperationType.Optional,  Symbol.POSTSRCMOD),
            new TokenRule(OperationType.Optional,  Symbol.SRCREP),
            new TokenRule(OperationType.End),
            new TokenRule(OperationType.Rule,  Symbol.SRCNAME, "<SrcName>"),
            new TokenRule(OperationType.And,  Symbol.TEMPREG),
            new TokenRule(OperationType.Or,  Symbol.CONSTANT),
            new TokenRule(OperationType.Or,  Symbol.COLOR),
            new TokenRule(OperationType.End),
            new TokenRule(OperationType.Rule,  Symbol.DEFCONST, "<DefineConstant>"),
            new TokenRule(OperationType.And,  Symbol.DEF, "def"),
            new TokenRule(OperationType.And,  Symbol.CONSTANT),
            new TokenRule(OperationType.And,  Symbol.SEPERATOR),
            new TokenRule(OperationType.And,  Symbol.NUMVAL),
            new TokenRule(OperationType.And,  Symbol.SEPERATOR),
            new TokenRule(OperationType.And,  Symbol.NUMVAL),
            new TokenRule(OperationType.And,  Symbol.SEPERATOR),
            new TokenRule(OperationType.And,  Symbol.NUMVAL),
            new TokenRule(OperationType.And,  Symbol.SEPERATOR),
            new TokenRule(OperationType.And,  Symbol.NUMVAL),
            new TokenRule(OperationType.End),
            new TokenRule(OperationType.Rule,  Symbol.CONSTANT, "<Constant>"),
            new TokenRule(OperationType.And,  Symbol.C0, "c0"),
            new TokenRule(OperationType.Or,  Symbol.C1, "c1"),
            new TokenRule(OperationType.Or,  Symbol.C2, "c2"),
            new TokenRule(OperationType.Or,  Symbol.C3, "c3"),
            new TokenRule(OperationType.Or,  Symbol.C4, "c4"),
            new TokenRule(OperationType.Or,  Symbol.C5, "c5"),
            new TokenRule(OperationType.Or,  Symbol.C6, "c6"),
            new TokenRule(OperationType.Or,  Symbol.C7, "c7"),
            new TokenRule(OperationType.End),
            new TokenRule(OperationType.Rule,  Symbol.TEXCISCOP_PS1_1_3, "<TexCISCOp_PS1_1_3>"),
            new TokenRule(OperationType.And,  Symbol.TEXDP3TEX,"texdp3tex"),
            new TokenRule(OperationType.Or,  Symbol.TEXDP3,"texdp3"),
            new TokenRule(OperationType.Or,  Symbol.TEXM3X2PAD,"texm3x2pad"),
            new TokenRule(OperationType.Or,  Symbol.TEXM3X2TEX,"texm3x2tex"),
            new TokenRule(OperationType.Or,  Symbol.TEXM3X3PAD,"texm3x3pad"),
            new TokenRule(OperationType.Or,  Symbol.TEXM3X3TEX,"texm3x3tex"),
            new TokenRule(OperationType.Or,  Symbol.TEXM3X3SPEC,"texm3x3spec"),
            new TokenRule(OperationType.Or,  Symbol.TEXM3X3VSPEC,"texm3x3vspec"),
            new TokenRule(OperationType.Or,  Symbol.TEXREG2RGB,"texreg2rgb"),
            new TokenRule(OperationType.Or,  Symbol.TEXREG2AR,"texreg2ar"),
            new TokenRule(OperationType.Or,  Symbol.TEXREG2GB,"texreg2gb"),
            new TokenRule(OperationType.End),
            new TokenRule(OperationType.Rule,  Symbol.TEXSWIZZLE, "<TexSwizzle>"),
            new TokenRule(OperationType.And,  Symbol.STQDQ,"_dw.xyw"),
            new TokenRule(OperationType.Or,  Symbol.STQDQ,"_dw"),
            new TokenRule(OperationType.Or,  Symbol.STQDQ,"_da.rga"),
            new TokenRule(OperationType.Or,  Symbol.STQDQ,"_da"),
            new TokenRule(OperationType.Or,  Symbol.STRDR,"_dz.xyz"),
            new TokenRule(OperationType.Or,  Symbol.STRDR,"_dz"),
            new TokenRule(OperationType.Or,  Symbol.STRDR,"_db.rgb"),
            new TokenRule(OperationType.Or,  Symbol.STRDR,"_db"),
            new TokenRule(OperationType.Or,  Symbol.STR,".xyz"),
            new TokenRule(OperationType.Or,  Symbol.STR,".rgb"),
            new TokenRule(OperationType.Or,  Symbol.STQ,".xyw"),
            new TokenRule(OperationType.Or,  Symbol.STQ,".rga"),
            new TokenRule(OperationType.End ),
            new TokenRule(OperationType.Rule,  Symbol.TEXMASK, "<TexMask>"),
            new TokenRule(OperationType.And,  Symbol.RGB,".rgb"),
            new TokenRule(OperationType.Or,  Symbol.RGB,".xyz"),
            new TokenRule(OperationType.Or,  Symbol.RG,".rg"),
            new TokenRule(OperationType.Or,  Symbol.RG,".xy"),
            new TokenRule(OperationType.End),
            new TokenRule(OperationType.Rule,  Symbol.SEPERATOR, "<Seperator>"),
            new TokenRule(OperationType.And,  Symbol.COMMA, ","),
            new TokenRule(OperationType.End),
            new TokenRule(OperationType.Rule,  Symbol.REG_PS1_4, "<Reg_PS1_4>"),
            new TokenRule(OperationType.And,  Symbol.R0, "r0"),
            new TokenRule(OperationType.Or,  Symbol.R1, "r1"),
            new TokenRule(OperationType.Or,  Symbol.R2, "r2"),
            new TokenRule(OperationType.Or,  Symbol.R3, "r3"),
            new TokenRule(OperationType.Or,  Symbol.R4, "r4"),
            new TokenRule(OperationType.Or,  Symbol.R5, "r5"),
            new TokenRule(OperationType.End),
            new TokenRule(OperationType.Rule,  Symbol.TEX_PS1_4, "<Tex_PS1_4>"),
            new TokenRule(OperationType.And,  Symbol.T0, "t0"),
            new TokenRule(OperationType.Or,  Symbol.T1, "t1"),
            new TokenRule(OperationType.Or,  Symbol.T2, "t2"),
            new TokenRule(OperationType.Or,  Symbol.T3, "t3"),
            new TokenRule(OperationType.Or,  Symbol.T4, "t4"),
            new TokenRule(OperationType.Or,  Symbol.T5, "t5"),
            new TokenRule(OperationType.End),
            new TokenRule(OperationType.Rule,  Symbol.REG_PS1_1_3, "<Reg_PS1_1_3>"),
            new TokenRule(OperationType.And,  Symbol.R0_1, "r0"),
            new TokenRule(OperationType.Or,  Symbol.R1_1, "r1"),
            new TokenRule(OperationType.End),
            new TokenRule(OperationType.Rule,  Symbol.TEX_PS1_1_3, "<Tex_PS1_1_3>"),
            new TokenRule(OperationType.And,  Symbol.T0_1, "t0"),
            new TokenRule(OperationType.Or,  Symbol.T1_1, "t1"),
            new TokenRule(OperationType.Or,  Symbol.T2_1, "t2"),
            new TokenRule(OperationType.Or,  Symbol.T3_1, "t3"),
            new TokenRule(OperationType.End),
            new TokenRule(OperationType.Rule,  Symbol.COLOR, "<Color>"),
            new TokenRule(OperationType.And,  Symbol.V0, "v0"),
            new TokenRule(OperationType.Or,  Symbol.V1, "v1"),
            new TokenRule(OperationType.End),
            new TokenRule(OperationType.Rule,  Symbol.TEMPREG, "<TempReg>"),
            new TokenRule(OperationType.And,  Symbol.REG_PS1_4),
            new TokenRule(OperationType.Or,  Symbol.REG_PS1_1_3),
            new TokenRule(OperationType.Or,  Symbol.TEX_PS1_1_3),
            new TokenRule(OperationType.End),
            new TokenRule(OperationType.Rule,  Symbol.DSTMODSAT, "<DstModSat>"),
            new TokenRule(OperationType.Optional,  Symbol.DSTMOD),
            new TokenRule(OperationType.Optional,  Symbol.DSTSAT),
            new TokenRule(OperationType.End),
            new TokenRule(OperationType.Rule,   Symbol.UNARYOP, "<UnaryOp>"),
            new TokenRule(OperationType.And,  Symbol.MOV, "mov"),
            new TokenRule(OperationType.End),
            new TokenRule(OperationType.Rule,  Symbol.BINARYOP, "<BinaryOP>"),
            new TokenRule(OperationType.And,  Symbol.ADD, "add"),
            new TokenRule(OperationType.Or,  Symbol.MUL, "mul"),
            new TokenRule(OperationType.Or,  Symbol.SUB, "sub"),
            new TokenRule(OperationType.Or,  Symbol.DP3, "dp3"),
            new TokenRule(OperationType.Or,  Symbol.DP4, "dp4"),
            new TokenRule(OperationType.Or,  Symbol.BEM, "bem"),
            new TokenRule(OperationType.End),
            new TokenRule(OperationType.Rule,  Symbol.TERNARYOP, "<TernaryOp>"),
            new TokenRule(OperationType.And,  Symbol.MAD, "mad"),
            new TokenRule(OperationType.Or,  Symbol.LRP, "lrp"),
            new TokenRule(OperationType.Or,  Symbol.CND, "cnd"),
            new TokenRule(OperationType.Or,  Symbol.CMP, "cmp"),
            new TokenRule(OperationType.End),
            new TokenRule(OperationType.Rule,  Symbol.DSTMASK, "<DstMask>"),
            new TokenRule(OperationType.And,  Symbol.RGBA,".rgba"),
            new TokenRule(OperationType.Or,  Symbol.RGBA,".xyzw"),
            new TokenRule(OperationType.Or,  Symbol.RGB,".rgb"),
            new TokenRule(OperationType.Or,  Symbol.RGB,".xyz"),
            new TokenRule(OperationType.Or,  Symbol.RGA,".xyw"),
            new TokenRule(OperationType.Or,  Symbol.RGA,".rga"),
            new TokenRule(OperationType.Or,  Symbol.RBA,".rba"),
            new TokenRule(OperationType.Or,  Symbol.RBA,".xzw"),
            new TokenRule(OperationType.Or,  Symbol.GBA,".gba"),
            new TokenRule(OperationType.Or,  Symbol.GBA,".yzw"),
            new TokenRule(OperationType.Or,  Symbol.RG,".rg"),
            new TokenRule(OperationType.Or,  Symbol.RG,".xy"),
            new TokenRule(OperationType.Or,  Symbol.RB,".xz"),
            new TokenRule(OperationType.Or,  Symbol.RB,".rb"),
            new TokenRule(OperationType.Or,  Symbol.RA,".xw"),
            new TokenRule(OperationType.Or,  Symbol.RA,".ra"),
            new TokenRule(OperationType.Or,  Symbol.GB,".gb"),
            new TokenRule(OperationType.Or,  Symbol.GB,".yz"),
            new TokenRule(OperationType.Or,  Symbol.GA,".yw"),
            new TokenRule(OperationType.Or,  Symbol.GA,".ga"),
            new TokenRule(OperationType.Or,  Symbol.BA,".zw"),
            new TokenRule(OperationType.Or,  Symbol.BA,".ba"),
            new TokenRule(OperationType.Or,  Symbol.R,".r"),
            new TokenRule(OperationType.Or,  Symbol.R,".x"),
            new TokenRule(OperationType.Or,  Symbol.G,".g"),
            new TokenRule(OperationType.Or,  Symbol.G,".y"),
            new TokenRule(OperationType.Or,  Symbol.B,".b"),
            new TokenRule(OperationType.Or,  Symbol.B,".z"),
            new TokenRule(OperationType.Or,  Symbol.A,".a"),
            new TokenRule(OperationType.Or,  Symbol.A,".w"),
            new TokenRule(OperationType.End),
            new TokenRule(OperationType.Rule,  Symbol.SRCREP, "<SrcRep>"),
            new TokenRule(OperationType.And,  Symbol.RRRR, ".r"),
            new TokenRule(OperationType.Or,  Symbol.RRRR, ".x"),
            new TokenRule(OperationType.Or,  Symbol.GGGG, ".g"),
            new TokenRule(OperationType.Or,  Symbol.GGGG, ".y"),
            new TokenRule(OperationType.Or,  Symbol.BBBB, ".b"),
            new TokenRule(OperationType.Or,  Symbol.BBBB, ".z"),
            new TokenRule(OperationType.Or,  Symbol.AAAA, ".a"),
            new TokenRule(OperationType.Or,  Symbol.AAAA, ".w"),
            new TokenRule(OperationType.End),
            new TokenRule(OperationType.Rule,  Symbol.PRESRCMOD, "<PreSrcMod>"),
            new TokenRule(OperationType.And,  Symbol.INVERT, "1-"),
            new TokenRule(OperationType.Or,  Symbol.INVERT, "1 -"),
            new TokenRule(OperationType.Or,  Symbol.NEGATE, "-"),
            new TokenRule(OperationType.End),
            new TokenRule(OperationType.Rule,  Symbol.POSTSRCMOD, "<PostSrcMod>"),
            new TokenRule(OperationType.And,  Symbol.BX2, "_bx2"),
            new TokenRule(OperationType.Or,  Symbol.X2, "_x2"),
            new TokenRule(OperationType.Or,  Symbol.BIAS, "_bias"),
            new TokenRule(OperationType.End),
            new TokenRule(OperationType.Rule,  Symbol.DSTMOD, "<DstMod>"),
            new TokenRule(OperationType.And,  Symbol.X2, "_x2"),
            new TokenRule(OperationType.Or,  Symbol.X4, "_x4"),
            new TokenRule(OperationType.Or,  Symbol.D2, "_d2"),
            new TokenRule(OperationType.Or,  Symbol.X8, "_x8"),
            new TokenRule(OperationType.Or,  Symbol.D4, "_d4"),
            new TokenRule(OperationType.Or,  Symbol.D8, "_d8"),
            new TokenRule(OperationType.End),
            new TokenRule(OperationType.Rule,  Symbol.DSTSAT, "<DstSat>"),
            new TokenRule(OperationType.And,  Symbol.SAT, "_sat"),
            new TokenRule(OperationType.End),
            new TokenRule(OperationType.Rule,  Symbol.COISSUE, "<CoIssue>"),
            new TokenRule(OperationType.Optional,  Symbol.PLUS, "+"),
            new TokenRule(OperationType.End)
        };

        //***************************** MACROs for PS1_1 , PS1_2, PS1_3 CISC instructions **************************************

        /// <summary>
        ///     Macro token expansion for ps_1_2 instruction: texreg2ar
        /// </summary>
        static TokenInstruction[] texreg2ar = {
            // mov r(x).r, r(y).a
            new TokenInstruction(Symbol.UNARYOP, Symbol.MOV),
            new TokenInstruction(Symbol.REG_PS1_4, Symbol.R1),
            new TokenInstruction(Symbol.DSTMASK, Symbol.R),
            new TokenInstruction(Symbol.SEPERATOR, Symbol.COMMA),
            new TokenInstruction(Symbol.REG_PS1_4, Symbol.R0),
            new TokenInstruction(Symbol.SRCREP, Symbol.AAAA),
            // mov r(x).g, r(y).r
            new TokenInstruction(Symbol.UNARYOP, Symbol.MOV),
            new TokenInstruction(Symbol.REG_PS1_4, Symbol.R1),
            new TokenInstruction(Symbol.DSTMASK, Symbol.G),
            new TokenInstruction(Symbol.SEPERATOR, Symbol.COMMA),
            new TokenInstruction(Symbol.REG_PS1_4, Symbol.R0),
            new TokenInstruction(Symbol.SRCREP, Symbol.RRRR),
            // texld r(x), r(x)
            new TokenInstruction(Symbol.TEXOP_PS1_4, Symbol.TEXLD),
            new TokenInstruction(Symbol.REG_PS1_4, Symbol.R1),
            new TokenInstruction(Symbol.SEPERATOR, Symbol.COMMA),
            new TokenInstruction(Symbol.REG_PS1_4, Symbol.R1)
        };

        static RegModOffset[] texreg2xx_RegMods = {
            new RegModOffset(1, Symbol.R_BASE, 0),
            new RegModOffset(7, Symbol.R_BASE, 0),
            new RegModOffset(13, Symbol.R_BASE, 0),
            new RegModOffset(15, Symbol.R_BASE, 0),
            new RegModOffset(4, Symbol.R_BASE, 1),
            new RegModOffset(10, Symbol.R_BASE, 1),
        };

        static MacroRegModify texreg2ar_MacroMods = 
            new MacroRegModify(texreg2ar, texreg2xx_RegMods);

        /// <summary>
        ///     Macro token expansion for ps_1_2 instruction: texreg2gb
        /// </summary>
        static TokenInstruction[] texreg2gb = {
            new TokenInstruction(Symbol.UNARYOP,Symbol.MOV),
            new TokenInstruction(Symbol.REG_PS1_4,Symbol.R1),
            new TokenInstruction(Symbol.DSTMASK,Symbol.R),
            new TokenInstruction(Symbol.SEPERATOR,Symbol.COMMA),
            new TokenInstruction(Symbol.REG_PS1_4,Symbol.R0),
            new TokenInstruction(Symbol.SRCREP,Symbol.GGGG),
            // mov r(x).g, r(y).b
            new TokenInstruction(Symbol.UNARYOP,Symbol.MOV),
            new TokenInstruction(Symbol.REG_PS1_4,Symbol.R1),
            new TokenInstruction(Symbol.DSTMASK,Symbol.G),
            new TokenInstruction(Symbol.SEPERATOR,Symbol.COMMA),
            new TokenInstruction(Symbol.REG_PS1_4,Symbol.R0),
            new TokenInstruction(Symbol.SRCREP,Symbol.BBBB),
            // texld r(x), r(x)
            new TokenInstruction(Symbol.TEXOP_PS1_4, Symbol.TEXLD),
            new TokenInstruction(Symbol.REG_PS1_4,Symbol.R1),
            new TokenInstruction(Symbol.SEPERATOR,Symbol.COMMA),
            new TokenInstruction(Symbol.REG_PS1_4,Symbol.R1)
        };

        static MacroRegModify texreg2gb_MacroMods = 
            new MacroRegModify(texreg2gb, texreg2xx_RegMods);

        /// <summary>
        ///     Macro token expansion for ps_1_1 instruction: texdp3
        /// </summary>
        static TokenInstruction[] texdp3 = {
            // texcoord t(x)
            new TokenInstruction(Symbol.TEXOP_PS1_1_3,  Symbol.TEXCOORD),
            new TokenInstruction(Symbol.TEX_PS1_1_3,  Symbol.T1_1),
            // dp3 r(x), r(x), r(y)
            new TokenInstruction(Symbol.BINARYOP, Symbol.DP3),
            new TokenInstruction(Symbol.REG_PS1_4, Symbol.R1),
            new TokenInstruction(Symbol.SEPERATOR, Symbol.COMMA),
            new TokenInstruction(Symbol.REG_PS1_4, Symbol.R1),
            new TokenInstruction(Symbol.SEPERATOR, Symbol.COMMA),
            new TokenInstruction(Symbol.REG_PS1_4, Symbol.R0)
        };

        static RegModOffset[] texdp3_RegMods = {
            new RegModOffset(1, Symbol.T_BASE, 0),
            new RegModOffset(3, Symbol.R_BASE, 0),
            new RegModOffset(5, Symbol.R_BASE, 0),
            new RegModOffset(7, Symbol.R_BASE, 1)
        };

        static MacroRegModify texdp3_MacroMods = 
            new MacroRegModify(texdp3, texdp3_RegMods);

        /// <summary>
        ///     Macro token expansion for ps_1_1 instruction: texdp3
        /// </summary>
        static TokenInstruction[] texdp3tex = {
            // texcoord t(x)
            new TokenInstruction(Symbol.TEXOP_PS1_1_3,  Symbol.TEXCOORD),
            new TokenInstruction(Symbol.TEX_PS1_1_3,  Symbol.T1_1),
	        // dp3 r1, r(x), r(y)
            new TokenInstruction(Symbol.BINARYOP, Symbol.DP3),
            new TokenInstruction(Symbol.REG_PS1_4, Symbol.R1),
            new TokenInstruction(Symbol.SEPERATOR, Symbol.COMMA),
            new TokenInstruction(Symbol.REG_PS1_4, Symbol.R1),
            new TokenInstruction(Symbol.SEPERATOR, Symbol.COMMA),
            new TokenInstruction(Symbol.REG_PS1_4, Symbol.R0),
            // texld r(x), r(x)
            new TokenInstruction(Symbol.TEXOP_PS1_4, Symbol.TEXLD),
            new TokenInstruction(Symbol.REG_PS1_4, Symbol.R1),
            new TokenInstruction(Symbol.SEPERATOR, Symbol.COMMA),
            new TokenInstruction(Symbol.REG_PS1_4, Symbol.R1)
        };

        static RegModOffset[] texdp3tex_RegMods = {
            new RegModOffset(1, Symbol.T_BASE, 0),
            new RegModOffset(3, Symbol.R_BASE, 0),
            new RegModOffset(5, Symbol.R_BASE, 0),
            new RegModOffset(7, Symbol.R_BASE, 1),
            new RegModOffset(9, Symbol.R_BASE, 1),
            new RegModOffset(11, Symbol.R_BASE, 1)
        };

        static MacroRegModify texdp3tex_MacroMods =
            new MacroRegModify(texdp3tex, texdp3tex_RegMods);

        static TokenInstruction[] texm3x2pad = {
            // texcoord t(x)
            new TokenInstruction(Symbol.TEXOP_PS1_1_3,  Symbol.TEXCOORD),
            new TokenInstruction(Symbol.TEX_PS1_1_3,  Symbol.T0_1),
            // dp3 r4.r,  r(x),  r(y)
            new TokenInstruction(Symbol.BINARYOP, Symbol.DP3),
            new TokenInstruction(Symbol.REG_PS1_4, Symbol.R4),
            new TokenInstruction(Symbol.DSTMASK, Symbol.R),
            new TokenInstruction(Symbol.SEPERATOR, Symbol.COMMA),
            new TokenInstruction(Symbol.REG_PS1_4, Symbol.R1),
            new TokenInstruction(Symbol.SEPERATOR, Symbol.COMMA),
            new TokenInstruction(Symbol.REG_PS1_4, Symbol.R0)
        };

        static RegModOffset[] texm3xxpad_RegMods = {
            new RegModOffset(1, Symbol.T_BASE, 0),
            new RegModOffset(6, Symbol.R_BASE, 0),
            new RegModOffset(8, Symbol.R_BASE, 1)
        };

        static MacroRegModify texm3x2pad_MacroMods =
            new MacroRegModify(texm3x2pad, texm3xxpad_RegMods);

        /// <summary>
        ///     Macro token expansion for ps_1_1 instruction: texm3x2tex
        /// </summary>
        static TokenInstruction[] texm3x2tex = {
            // texcoord t(x)
            new TokenInstruction(Symbol.TEXOP_PS1_1_3, Symbol.TEXCOORD),
            new TokenInstruction(Symbol.TEX_PS1_1_3, Symbol.T1_1),
            // dp3 r4.g, r(x), r(y)
            new TokenInstruction(Symbol.BINARYOP,	Symbol.DP3),
            new TokenInstruction(Symbol.REG_PS1_4,	Symbol.R4),
            new TokenInstruction(Symbol.DSTMASK,	Symbol.G),
            new TokenInstruction(Symbol.SEPERATOR,	Symbol.COMMA),
            new TokenInstruction(Symbol.REG_PS1_4,	Symbol.R1),
            new TokenInstruction(Symbol.SEPERATOR,	Symbol.COMMA),
            new TokenInstruction(Symbol.REG_PS1_4,	Symbol.R0),
            // texld r(x), r4
            new TokenInstruction(Symbol.TEXOP_PS1_4, Symbol.TEXLD),
            new TokenInstruction(Symbol.REG_PS1_4,	Symbol.R1),
            new TokenInstruction(Symbol.SEPERATOR,	Symbol.COMMA),
            new TokenInstruction(Symbol.REG_PS1_4,	Symbol.R4)
        };

        static RegModOffset[] texm3xxtex_RegMods = {
            new RegModOffset(1, Symbol.T_BASE, 0),
            new RegModOffset(6, Symbol.R_BASE, 0),
            new RegModOffset(8, Symbol.R_BASE, 1),
            new RegModOffset(10, Symbol.R_BASE, 0)
        };

        static MacroRegModify texm3x2tex_MacroMods = 
            new MacroRegModify(texm3x2tex, texm3xxtex_RegMods);

        /// <summary>
        ///     Macro token expansion for ps_1_1 instruction: texm3x3tex
        /// </summary>
        static TokenInstruction[] texm3x3pad = {
            // texcoord t(x)
            new TokenInstruction(Symbol.TEXOP_PS1_1_3,  Symbol.TEXCOORD),
            new TokenInstruction(Symbol.TEX_PS1_1_3,  Symbol.T0_1),
            // dp3 r4.b, r(x), r(y)
            new TokenInstruction(Symbol.BINARYOP, Symbol.DP3),
            new TokenInstruction(Symbol.REG_PS1_4, Symbol.R4),
            new TokenInstruction(Symbol.DSTMASK, Symbol.B),
            new TokenInstruction(Symbol.SEPERATOR, Symbol.COMMA),
            new TokenInstruction(Symbol.REG_PS1_4, Symbol.R1),
            new TokenInstruction(Symbol.SEPERATOR, Symbol.COMMA),
            new TokenInstruction(Symbol.REG_PS1_4, Symbol.R0)
        };

        static MacroRegModify texm3x3pad_MacroMods =
            new MacroRegModify(texm3x3pad, texm3xxpad_RegMods);

        /// <summary>
        ///     Macro token expansion for ps_1_1 instruction: texm3x3pad
        /// </summary>
        static TokenInstruction[] texm3x3tex = {
            // texcoord t(x)
            new TokenInstruction(Symbol.TEXOP_PS1_1_3, Symbol.TEXCOORD),
            new TokenInstruction(Symbol.TEX_PS1_1_3, Symbol.T1_1),
            // dp3 r4.b, r(x), r(y)
            new TokenInstruction(Symbol.BINARYOP,	Symbol.DP3),
            new TokenInstruction(Symbol.REG_PS1_4,	Symbol.R4),
            new TokenInstruction(Symbol.DSTMASK,	Symbol.B),
            new TokenInstruction(Symbol.SEPERATOR,	Symbol.COMMA),
            new TokenInstruction(Symbol.REG_PS1_4,	Symbol.R1),
            new TokenInstruction(Symbol.SEPERATOR,	Symbol.COMMA),
            new TokenInstruction(Symbol.REG_PS1_4,	Symbol.R0),
            // texld r1, r4
            new TokenInstruction(Symbol.TEXOP_PS1_4, Symbol.TEXLD),
            new TokenInstruction(Symbol.REG_PS1_4,	Symbol.R1),
            new TokenInstruction(Symbol.SEPERATOR,	Symbol.COMMA),
            new TokenInstruction(Symbol.REG_PS1_4,	Symbol.R4)
        };

        static MacroRegModify texm3x3tex_MacroMods =
            new MacroRegModify(texm3x3tex, texm3xxtex_RegMods);

        /// <summary>
        ///     Macro token expansion for ps_1_1 instruction: texm3x3spec
        /// </summary>
        static TokenInstruction[] texm3x3spec = {
            new TokenInstruction(Symbol.TEXOP_PS1_1_3, Symbol.TEXCOORD),
            new TokenInstruction(Symbol.TEX_PS1_1_3, Symbol.T3_1),
            // dp3 r4.b, r3, r(x)
            new TokenInstruction(Symbol.BINARYOP, Symbol.DP3),
            new TokenInstruction(Symbol.REG_PS1_4, Symbol.R4),
            new TokenInstruction(Symbol.DSTMASK, Symbol.B),
            new TokenInstruction(Symbol.SEPERATOR, Symbol.COMMA),
            new TokenInstruction(Symbol.REG_PS1_4, Symbol.R3),
            new TokenInstruction(Symbol.SEPERATOR, Symbol.COMMA),
            new TokenInstruction(Symbol.REG_PS1_4, Symbol.R0),
            // dp3_x2 r3, r4, c(x)
            new TokenInstruction(Symbol.BINARYOP, Symbol.DP3),
            new TokenInstruction(Symbol.DSTMOD, 	Symbol.X2),
            new TokenInstruction(Symbol.REG_PS1_4, Symbol.R3),
            new TokenInstruction(Symbol.SEPERATOR, Symbol.COMMA),
            new TokenInstruction(Symbol.REG_PS1_4, Symbol.R4),
            new TokenInstruction(Symbol.SEPERATOR, Symbol.COMMA),
            new TokenInstruction(Symbol.CONSTANT, Symbol.C0),
            // mul r3, r3, c(x)
            new TokenInstruction(Symbol.UNARYOP, Symbol.MUL),
            new TokenInstruction(Symbol.REG_PS1_4, Symbol.R3),
            new TokenInstruction(Symbol.SEPERATOR, Symbol.COMMA),
            new TokenInstruction(Symbol.REG_PS1_4, Symbol.R3),
            new TokenInstruction(Symbol.SEPERATOR, Symbol.COMMA),
            new TokenInstruction(Symbol.CONSTANT, Symbol.C0),
            // dp3 r2, r4, r4
            new TokenInstruction(Symbol.BINARYOP, Symbol.DP3),
            new TokenInstruction(Symbol.REG_PS1_4, Symbol.R2),
            new TokenInstruction(Symbol.SEPERATOR, Symbol.COMMA),
            new TokenInstruction(Symbol.REG_PS1_4, Symbol.R4),
            new TokenInstruction(Symbol.SEPERATOR, Symbol.COMMA),
            new TokenInstruction(Symbol.REG_PS1_4, Symbol.R4),
            // mad r4.rgb, 1-c(x), r2, r3
            new TokenInstruction(Symbol.TERNARYOP, Symbol.MAD),
            new TokenInstruction(Symbol.REG_PS1_4, Symbol.R4),
            new TokenInstruction(Symbol.DSTMASK, Symbol.RGB),
            new TokenInstruction(Symbol.SEPERATOR, Symbol.COMMA),
            new TokenInstruction(Symbol.PRESRCMOD, Symbol.INVERT),
            new TokenInstruction(Symbol.CONSTANT, Symbol.C0),
            new TokenInstruction(Symbol.SEPERATOR, Symbol.COMMA),
            new TokenInstruction(Symbol.REG_PS1_4, Symbol.R2),
            new TokenInstruction(Symbol.SEPERATOR, Symbol.COMMA),
            new TokenInstruction(Symbol.REG_PS1_4, Symbol.R3),
            // + mov r4.a, r2.r
            new TokenInstruction(Symbol.UNARYOP, Symbol.MOV),
            new TokenInstruction(Symbol.REG_PS1_4, Symbol.R4),
            new TokenInstruction(Symbol.DSTMASK, Symbol.A),
            new TokenInstruction(Symbol.SEPERATOR, Symbol.COMMA),
            new TokenInstruction(Symbol.REG_PS1_4, Symbol.R2),
            new TokenInstruction(Symbol.SRCREP, 	Symbol.RRRR),
            // texld r3, r4.xyz_dz
            new TokenInstruction(Symbol.TEXOP_PS1_4, Symbol.TEXLD),
            new TokenInstruction(Symbol.REG_PS1_4, Symbol.R3),
            new TokenInstruction(Symbol.SEPERATOR, Symbol.COMMA),
            new TokenInstruction(Symbol.REG_PS1_4, Symbol.R4),
            new TokenInstruction(Symbol.TEXSWIZZLE, Symbol.STRDR)
        };

        static RegModOffset[] texm3x3spec_RegMods = {
            new RegModOffset(8, Symbol.R_BASE, 1),
            new RegModOffset(15, Symbol.R_BASE, 2),
            new RegModOffset(21, Symbol.C_BASE, 2),
            new RegModOffset(33, Symbol.C_BASE, 2)
        };

        static MacroRegModify texm3x3tex_MacroMods =
            new MacroRegModify(texm3x3tex, texm3xxtex_RegMods);

        #endregion Static Fields

        #region Constructor

		public PixelShader() {
		}

        #endregion Constructor

        #region Compiler2Pass Members

        protected override bool DoPass2() {
            return false;
        }

        #endregion Compiler2Pass Members
	}
}
