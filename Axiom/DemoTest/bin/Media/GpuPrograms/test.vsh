!!ARBvp1.0
# ARB_vertex_program generated by NVIDIA Cg compiler
# cgc version 1.1.0003, build date Mar  4 2003  12:32:10
# command line args: -profile arbvp1
# nv30vp backend compiling 'main' program
PARAM c1 = { 0, 1, 0, 0 };
#vendor NVIDIA Corporation
#version 1.0.02
#profile arbvp1
#program main
#semantic main.color
#var float2 position : $vin.POSITION : POSITION : 0 : 1
#var float2 texCoord : $vin.TEXCOORD0 : TEXCOORD0 : 1 : 1
#var float4 color :  : c[0] : 2 : 1
#var float4 position : $vout.POSITION : POSITION : -1 : 1
#var float4 color : $vout.COLOR : COLOR : -1 : 1
#var float2 texCoord : $vout.TEXCOORD0 : TEXCOORD0 : -1 : 1
ATTRIB v24 = vertex.texcoord[0];
ATTRIB v16 = vertex.position;
PARAM c0 = program.local[0];
	MOV result.texcoord[0].xy, v24;
	MOV result.position.xy, v16.xyyy;
	MOV result.position.zw, c1.yyxy;
	MOV result.color.front.primary, c0;
END
# 4 instructions
# 0 temp registers
# End of program
