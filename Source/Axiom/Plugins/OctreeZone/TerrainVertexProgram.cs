﻿#region LGPL License

/*
Axiom Graphics Engine Library
Copyright © 2003-2011 Axiom Project Team

The overall design, and a majority of the core engine and rendering code
contained within this library is a derivative of the open source Object Oriented
Graphics Engine OGRE, which can be found at http://ogre.sourceforge.net.
Many thanks to the OGRE team for maintaining such a high quality project.

This library is free software; you can redistribute it and/or
modify it under the terms of the GNU Lesser General Public
License as published by the Free Software Foundation; either
version 2.1 of the License, or (at your option) any later version.

This library is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
Lesser General Public License for more details.

You should have received a copy of the GNU Lesser General Public
License along with this library; if not, write to the Free Software
Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
*/

#endregion

#region SVN Version Information

// <file>
//     <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
//     <id value="$Id:$"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections.Generic;
using System.Text;

using Axiom.Graphics;

#endregion Namespace Declarations

namespace OctreeZone
{
	internal class TerrainVertexProgram
	{
		private const string mNoFogArbvp1 = "!!ARBvp1.0\n" + "PARAM c5 = { 1, 1, 1, 1 };\n" + "#var float4x4 worldViewProj :  : c[0], 4 : 8 : 1\n" + "#var float morphFactor :  : c[4] : 9 : 1\n" + "TEMP R0;\n" + "ATTRIB v17 = vertex.weight;\n" + "ATTRIB v25 = vertex.texcoord[1];\n" + "ATTRIB v24 = vertex.texcoord[0];\n" + "ATTRIB v16 = vertex.position;\n" + "PARAM c0[4] = { program.local[0..3] };\n" + "PARAM c4 = program.local[4];\n" + "	MOV result.texcoord[0], v24;\n" + "	MOV result.texcoord[1], v25;\n" + "	MOV R0, v16;\n" + "	MAD R0.y, v17.x, c4.x, R0.y;\n" + "	DP4 result.position.x, c0[0], R0;\n" + "	DP4 result.position.y, c0[1], R0;\n" + "	DP4 result.position.z, c0[2], R0;\n" + "	DP4 result.position.w, c0[3], R0;\n" + "	MOV result.color.front.primary, c5.x;\n" + "END\n";

		private const string mShadowReceiverArbvp1 = "!!ARBvp1.0\n" + "PARAM c[14] = { program.local[0..12], { 1 } };\n" + "TEMP R0;\n" + "TEMP R1;\n" + "TEMP R2;\n" + "MOV result.color, c[13].x;\n" + "MUL R0.x, vertex.weight, c[12];\n" + "ADD R1.y, R0.x, vertex.position;\n" + "MOV R1.xzw, vertex.position;\n" + "DP4 result.position.w, R1, c[3];\n" + "DP4 result.position.z, R1, c[2];\n" + "DP4 R0.w, R1, c[7];\n" + "DP4 R0.z, R1, c[6];\n" + "DP4 R0.y, R1, c[5];\n" + "DP4 R0.x, R1, c[4];\n" + "DP4 result.position.y, R1, c[1];\n" + "DP4 R2.y, R0, c[9];\n" + "DP4 R2.z, R0, c[11];\n" + "DP4 R2.x, R0, c[8];\n" + "RCP R0.x, R2.z;\n" + "DP4 result.position.x, R1, c[0];\n" + "MUL result.texcoord[0].xy, R2, R0.x;\n" + "END\n";
		private const string mLinearFogArbvp1 = "!!ARBvp1.0\n" + "PARAM c5 = { 1, 1, 1, 1 };\n" + "#var float4x4 worldViewProj :  : c[0], 4 : 9 : 1\n" + "#var float morphFactor :  : c[4] : 10 : 1\n" + "TEMP R0, R1;\n" + "ATTRIB v17 = vertex.weight;\n" + "ATTRIB v25 = vertex.texcoord[1];\n" + "ATTRIB v24 = vertex.texcoord[0];\n" + "ATTRIB v16 = vertex.position;\n" + "PARAM c0[4] = { program.local[0..3] };\n" + "PARAM c4 = program.local[4];\n" + "	MOV result.texcoord[0], v24;\n" + "	MOV result.texcoord[1], v25;\n" + "	MOV R1, v16;\n" + "	MAD R1.y, v17.x, c4.x, R1.y;\n" + "	DP4 R0.x, c0[0], R1;\n" + "	DP4 R0.y, c0[1], R1;\n" + "	DP4 R0.z, c0[2], R1;\n" + "	DP4 R0.w, c0[3], R1;\n" + "	MOV result.fogcoord.x, R0.z;\n" + "	MOV result.position, R0;\n" + "	MOV result.color.front.primary, c5.x;\n" + "END\n";
		private const string mExpFogArbvp1 = "!!ARBvp1.0\n" + "PARAM c6 = { 1, 1, 1, 1 };\n" + "PARAM c7 = { 2.71828, 0, 0, 0 };\n" + "#var float4x4 worldViewProj :  : c[0], 4 : 9 : 1\n" + "#var float morphFactor :  : c[4] : 10 : 1\n" + "#var float fogDensity :  : c[5] : 11 : 1\n" + "TEMP R0, R1;\n" + "ATTRIB v17 = vertex.weight;\n" + "ATTRIB v25 = vertex.texcoord[1];\n" + "ATTRIB v24 = vertex.texcoord[0];\n" + "ATTRIB v16 = vertex.position;\n" + "PARAM c5 = program.local[5];\n" + "PARAM c0[4] = { program.local[0..3] };\n" + "PARAM c4 = program.local[4];\n" + "	MOV result.texcoord[0], v24;\n" + "	MOV result.texcoord[1], v25;\n" + "	MOV R1, v16;\n" + "	MAD R1.y, v17.x, c4.x, R1.y;\n" + "	DP4 R0.x, c0[0], R1;\n" + "	DP4 R0.y, c0[1], R1;\n" + "	DP4 R0.z, c0[2], R1;\n" + "	DP4 R0.w, c0[3], R1;\n" + "	MOV result.position, R0;\n" + "	MOV result.color.front.primary, c6.x;\n" + "	MUL R0.zw, R0.z, c5.x;\n" + "	MOV R0.xy, c7.x;\n" + "	LIT R0.z, R0;\n" + "	RCP result.fogcoord.x, R0.z;\n" + "END\n";
		private const string mExp2FogArbvp1 = "!!ARBvp1.0\n" + "PARAM c6 = { 1, 1, 1, 1 };\n" + "PARAM c7 = { 0.002, 2.71828, 0, 0 };\n" + "#var float4x4 worldViewProj :  : c[0], 4 : 9 : 1\n" + "#var float morphFactor :  : c[4] : 10 : 1\n" + "#var float fogDensity :  : c[5] : 11 : 1\n" + "TEMP R0, R1;\n" + "ATTRIB v17 = vertex.weight;\n" + "ATTRIB v25 = vertex.texcoord[1];\n" + "ATTRIB v24 = vertex.texcoord[0];\n" + "ATTRIB v16 = vertex.position;\n" + "PARAM c0[4] = { program.local[0..3] };\n" + "PARAM c4 = program.local[4];\n" + "	MOV result.texcoord[0], v24;\n" + "	MOV result.texcoord[1], v25;\n" + "	MOV R1, v16;\n" + "	MAD R1.y, v17.x, c4.x, R1.y;\n" + "	DP4 R0.x, c0[0], R1;\n" + "	DP4 R0.y, c0[1], R1;\n" + "	DP4 R0.z, c0[2], R1;\n" + "	DP4 R0.w, c0[3], R1;\n" + "	MOV result.position, R0;\n" + "	MOV result.color.front.primary, c6.x;\n" + "	MUL R0.x, R0.z, c7.x;\n" + "	MUL R0.zw, R0.x, R0.x;\n" + "	MOV R0.xy, c7.y;\n" + "	LIT R0.z, R0;\n" + "	RCP result.fogcoord.x, R0.z;\n" + "END\n";

		private const string mNoFogVs_1_1 = "vs_1_1\n" + "def c5, 1, 1, 1, 1\n" + "//var float4x4 worldViewProj :  : c[0], 4 : 8 : 1\n" + "//var float morphFactor :  : c[4] : 9 : 1\n" + "dcl_blendweight v1\n" + "dcl_texcoord1 v8\n" + "dcl_texcoord0 v7\n" + "dcl_position v0\n" + "	mov oT0.xy, v7\n" + "	mov oT1.xy, v8\n" + "	mov r0, v0\n" + "	mad r0.y, v1.x, c4.x, r0.y\n" + "	dp4 oPos.x, c0, r0\n" + "	dp4 oPos.y, c1, r0\n" + "	dp4 oPos.z, c2, r0\n" + "	dp4 oPos.w, c3, r0\n" + "	mov oD0, c5.x\n";
		private const string mLinearFogVs_1_1 = "vs_1_1\n" + "def c5, 1, 1, 1, 1\n" + "//var float4x4 worldViewProj :  : c[0], 4 : 9 : 1\n" + "//var float morphFactor :  : c[4] : 10 : 1\n" + "dcl_blendweight v1\n" + "dcl_texcoord1 v8\n" + "dcl_texcoord0 v7\n" + "dcl_position v0\n" + "	mov oT0.xy, v7\n" + "	mov oT1.xy, v8\n" + "	mov r1, v0\n" + "	mad r1.y, v1.x, c4.x, r1.y\n" + "	dp4 r0.x, c0, r1\n" + "	dp4 r0.y, c1, r1\n" + "	dp4 r0.z, c2, r1\n" + "	dp4 r0.w, c3, r1\n" + "	mov oFog, r0.z\n" + "	mov oPos, r0\n" + "	mov oD0, c5.x\n";

		private const string mShadowReceiverVs_1_1 = "vs_1_1\n" + "def c13, 1, 1, 1, 1\n" + "dcl_blendweight v1\n" + "dcl_texcoord1 v8\n" + "dcl_texcoord0 v7\n" + "dcl_position v0\n" + "mov r1, v0\n" + "mad r1.y, v1.x, c12.x, r1.y\n" + "dp4 oPos.x, c0, r1\n" + "dp4 oPos.y, c1, r1\n" + "dp4 oPos.z, c2, r1\n" + "dp4 oPos.w, c3, r1\n" + "dp4 r0.x, c4, r1\n" + "dp4 r0.y, c5, r1\n" + "dp4 r0.z, c6, r1\n" + "dp4 r0.w, c7, r1\n" + "dp4 r1.x, c8, r0\n" + "dp4 r1.y, c9, r0\n" + "dp4 r1.w, c11, r0\n" + "rcp r0.x, r1.w\n" + "mul oT0.xy, r1.xy, r0.x\n" + "mov oD0, c13\n";
		private const string mExpFogVs_1_1 = "vs_1_1\n" + "def c6, 1, 1, 1, 1\n" + "def c7, 2.71828, 0, 0, 0\n" + "//var float4x4 worldViewProj :  : c[0], 4 : 9 : 1\n" + "//var float morphFactor :  : c[4] : 10 : 1\n" + "//var float fogDensity :  : c[5] : 11 : 1\n" + "dcl_blendweight v1\n" + "dcl_texcoord1 v8\n" + "dcl_texcoord0 v7\n" + "dcl_position v0\n" + "	mov oT0.xy, v7\n" + "	mov oT1.xy, v8\n" + "	mov r1, v0\n" + "	mad r1.y, v1.x, c4.x, r1.y\n" + "	dp4 r0.x, c0, r1\n" + "	dp4 r0.y, c1, r1\n" + "	dp4 r0.z, c2, r1\n" + "	dp4 r0.w, c3, r1\n" + "	mov oPos, r0\n" + "	mov oD0, c6.x\n" + "	mul r0.zw, r0.z, c5.x\n" + "	mov r0.xy, c7.x\n" + "	lit r0.z, r0\n" + "	rcp oFog, r0.z\n";
		private const string mExp2FogVs_1_1 = "vs_1_1\n" + "def c6, 1, 1, 1, 1\n" + "def c7, 0.002, 2.71828, 0, 0\n" + "//var float4x4 worldViewProj :  : c[0], 4 : 9 : 1\n" + "//var float morphFactor :  : c[4] : 10 : 1\n" + "//var float fogDensity :  : c[5] : 11 : 1\n" + "dcl_blendweight v1\n" + "dcl_texcoord1 v8\n" + "dcl_texcoord0 v7\n" + "dcl_position v0\n" + "	mov oT0.xy, v7\n" + "	mov oT1.xy, v8\n" + "	mov r1, v0\n" + "	mad r1.y, v1.x, c4.x, r1.y\n" + "	dp4 r0.x, c0, r1\n" + "	dp4 r0.y, c1, r1\n" + "	dp4 r0.z, c2, r1\n" + "	dp4 r0.w, c3, r1\n" + "	mov oPos, r0\n" + "	mov oD0, c6.x\n" + "	mul r0.x, r0.z, c7.x\n" + "	mul r0.zw, r0.x, r0.x\n" + "	mov r0.xy, c7.y\n" + "	lit r0.z, r0\n" + "	rcp oFog, r0.z\n";

		public string getProgramSource( FogMode fogMode, string syntax, bool shadowReceiver )
		{
			if ( shadowReceiver )
			{
				if ( syntax == "arbvp1" )
				{
					return mShadowReceiverArbvp1;
				}
				else
				{
					return mShadowReceiverVs_1_1;
				}
			}
			else
			{
				switch ( fogMode )
				{
					case FogMode.None:
						if ( syntax == "arbvp1" )
						{
							return mNoFogArbvp1;
						}
						else
						{
							return mNoFogVs_1_1;
						}
						break;
					case FogMode.Linear:
						if ( syntax == "arbvp1" )
						{
							return mLinearFogArbvp1;
						}
						else
						{
							return mLinearFogVs_1_1;
						}
						break;
					case FogMode.Exp:
						if ( syntax == "arbvp1" )
						{
							return mExpFogArbvp1;
						}
						else
						{
							return mExpFogVs_1_1;
						}
						break;
					case FogMode.Exp2:
						if ( syntax == "arbvp1" )
						{
							return mExp2FogArbvp1;
						}
						else
						{
							return mExp2FogVs_1_1;
						}
						break;
				}
				;
			}
			// default
			return string.Empty;
		}
	}
}
