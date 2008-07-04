#region LGPL License
/*
Axiom Graphics Engine Library
Copyright (C) 2003-2006 Axiom Project Team

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
//     <license see="http://axiomengine.sf.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;

using Axiom.Graphics;
using Axiom.RenderSystems.Xna.HLSL;

using XNA = Microsoft.Xna.Framework;
using XFG = Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.Xna.FixedFunctionEmulation
{
	class HLSLShaderGenerator : ShaderGenerator
	{
		#region Construction and Destruction

		public HLSLShaderGenerator()
		{
			name = "hlsl";
			languageName = "hlsl";
			vpTarget = "vs_3_0";
			fpTarget = "ps_3_0";
		}

		#endregion Construction and Destruction

		#region ShaderGenerator Implentation

		public override string GetShaderSource( string vertexProgramName, string fragmentProgramName, VertexBufferDeclaration vertexBufferDeclaration, FixedFunctionState fixedFunctionState )
		{
			bool bHasColor = vertexBufferDeclaration.HasColor;
			bool bHasTexcoord = vertexBufferDeclaration.HasTexCoord;

			String shaderSource = "";

			shaderSource = shaderSource + "struct VS_INPUT { ";

			ushort[] semanticCount = new ushort[ 100 ];

			IEnumerable<VertexBufferElement> vertexBufferElements = vertexBufferDeclaration.VertexBufferElements;
			foreach ( VertexBufferElement element in vertexBufferElements )
			{
				VertexElementSemantic semantic = element.VertexElementSemantic;
				VertexElementType type = element.VertexElementType;

				String thisElementSemanticCount = semanticCount[ (int)semantic ].ToString();
				semanticCount[ (int)semantic ]++;
				String parameterType = "";
				String parameterName = "";
				String parameterShaderTypeName = "";

				switch ( type )
				{
					case VertexElementType.Float1:
						parameterType = "float";
						break;
					case VertexElementType.Float2:
						parameterType = "float2";
						break;
					case VertexElementType.Float3:
						parameterType = "float3";
						break;
					case VertexElementType.Float4:
						parameterType = "float4";
						break;
					case VertexElementType.Color:
					case VertexElementType.Color_ABGR:
					case VertexElementType.Color_ARGB:
                        parameterType = "int"; //"unsigned int";//unsigned not recognized
						break;
					case VertexElementType.Short1:
						parameterType = "short";
						break;
					case VertexElementType.Short2:
						parameterType = "short2";
						break;
					case VertexElementType.Short3:
						parameterType = "short3";
						break;
					case VertexElementType.Short4:
						parameterType = "short4";
						break;
					case VertexElementType.UByte4:
                        parameterType = "float4";//char4";//char4 not supported ??
						break;

				}
				switch ( semantic )
				{
					case VertexElementSemantic.Position:
						parameterName = "Position";
						parameterShaderTypeName = "POSITION";
						//parameterType = "float4"; // position must be float4 (and not float3 like in the buffer)
						break;
					case VertexElementSemantic.BlendWeights:
						parameterName = "BlendWeight";
						parameterShaderTypeName = "BLENDWEIGHT";
						break;
					case VertexElementSemantic.BlendIndices:
						parameterName = "BlendIndices";
						parameterShaderTypeName = "BLENDINDICES";
						break;
					case VertexElementSemantic.Normal:
						parameterName = "Normal";
						parameterShaderTypeName = "NORMAL";
						break;
					case VertexElementSemantic.Diffuse:
						parameterName = "DiffuseColor";
						parameterShaderTypeName = "COLOR";
						break;
					case VertexElementSemantic.Specular:
						parameterName = "SpecularColor";
						parameterShaderTypeName = "COLOR";
						thisElementSemanticCount = semanticCount[ (int)VertexElementSemantic.Diffuse ].ToString(); // Diffuse is the "COLOR" count...
						semanticCount[ (int)VertexElementSemantic.Diffuse ]++;
						break;
					case VertexElementSemantic.TexCoords:
						parameterName = "Texcoord";
						parameterShaderTypeName = "TEXCOORD";
						break;
					case VertexElementSemantic.Binormal:
						parameterName = "Binormal";
						parameterShaderTypeName = "BINORMAL";
						break;
					case VertexElementSemantic.Tangent:
						parameterName = "Tangent";
						parameterShaderTypeName = "TANGENT";
						break;
				}



				shaderSource = shaderSource + parameterType + " " + parameterName + thisElementSemanticCount + " : " + parameterShaderTypeName + thisElementSemanticCount + ";\n";
			}

			shaderSource = shaderSource + " };";



			shaderSource = shaderSource + "float4x4  World;\n";
			shaderSource = shaderSource + "float4x4  View;\n";
			shaderSource = shaderSource + "float4x4  Projection;\n";
			shaderSource = shaderSource + "float4x4  ViewIT;\n";
			shaderSource = shaderSource + "float4x4  WorldViewIT;\n";

			switch ( fixedFunctionState.GeneralFixedFunctionState.FogMode )
			{
				case FogMode.None:
					break;
				case FogMode.Exp:
				case FogMode.Exp2:
					shaderSource = shaderSource + "float FogDensity;\n";
					break;
				case FogMode.Linear:
					shaderSource = shaderSource + "float FogStart;\n";
					shaderSource = shaderSource + "float FogEnd;\n";
					break;
			}

			if ( fixedFunctionState.GeneralFixedFunctionState.EnableLighting )
			{
				shaderSource = shaderSource + "float4 BaseLightAmbient;\n";

				for ( int i = 0; i < fixedFunctionState.Lights.Count; i++ )
				{
					String prefix = "Light" + i.ToString() + "_";
					switch ( fixedFunctionState.Lights[ i ] )
					{
						case LightType.Point:
							shaderSource = shaderSource + "float3 " + prefix + "Position;\n";
							shaderSource = shaderSource + "float4 " + prefix + "Ambient;\n";
							shaderSource = shaderSource + "float4 " + prefix + "Diffuse;\n";
							shaderSource = shaderSource + "float4 " + prefix + "Specular;\n";
							shaderSource = shaderSource + "float  " + prefix + "Range;\n";
							shaderSource = shaderSource + "float3 " + prefix + "Attenuation;\n";
							break;
						case LightType.Directional:
							shaderSource = shaderSource + "float3 " + prefix + "Direction;\n";
							shaderSource = shaderSource + "float4 " + prefix + "Ambient;\n";
							shaderSource = shaderSource + "float4 " + prefix + "Diffuse;\n";
							shaderSource = shaderSource + "float4 " + prefix + "Specular;\n";
							break;
						case LightType.Spotlight:
							shaderSource = shaderSource + "float3 " + prefix + "Direction;\n";
							shaderSource = shaderSource + "float3 " + prefix + "Position;\n";
							shaderSource = shaderSource + "float4 " + prefix + "Ambient;\n";
							shaderSource = shaderSource + "float4 " + prefix + "Diffuse;\n";
							shaderSource = shaderSource + "float4 " + prefix + "Specular;\n";
							shaderSource = shaderSource + "float3 " + prefix + "Attenuation;\n";
							shaderSource = shaderSource + "float3 " + prefix + "Spot;\n";
							break;
					}
				}

			}



			shaderSource = shaderSource + "struct VS_OUTPUT\n";
			shaderSource = shaderSource + "{\n";
            shaderSource = shaderSource + "float4 Pos : POSITION;\n";//"float4 Pos : SV_POSITION;\n"; //SV not recognised
			if ( bHasTexcoord )
			{
				shaderSource = shaderSource + "float2 tCord : TEXCOORD;\n";
			}

			shaderSource = shaderSource + "float4 Color : COLOR0;\n";
			shaderSource = shaderSource + "float4 ColorSpec : COLOR1;\n";

			if ( fixedFunctionState.GeneralFixedFunctionState.FogMode != FogMode.None )
			{
                shaderSource = shaderSource + "float fogDist;\n"; //"float fogDist : FOGDISTANCE;\n";
			}

			shaderSource = shaderSource + "};\n";

			shaderSource = shaderSource + "VS_OUTPUT " + vertexProgramName + "( VS_INPUT input )\n";
			shaderSource = shaderSource + "{\n";
			shaderSource = shaderSource + "VS_OUTPUT output = (VS_OUTPUT)0;\n";
			shaderSource = shaderSource + "float4 worldPos = mul( World, float4( input.Position0 , 1 ));\n";
			shaderSource = shaderSource + "float4 cameraPos = mul( View, worldPos );\n";
			shaderSource = shaderSource + "output.Pos = mul( Projection, cameraPos );\n";


			if ( bHasTexcoord )
			{
				shaderSource = shaderSource + "output.tCord = input.Texcoord0;\n";
			}

			shaderSource = shaderSource + "output.ColorSpec = float4(0.0, 0.0, 0.0, 0.0);\n";


			if ( fixedFunctionState.GeneralFixedFunctionState.EnableLighting && fixedFunctionState.Lights.Count > 0 )
			{
				shaderSource = shaderSource + "output.Color = BaseLightAmbient;\n";
				if ( bHasColor )
				{
					shaderSource = shaderSource + "output.Color.x = ((input.DiffuseColor0 >> 24) & 0xFF) / 255.0f;\n";
					shaderSource = shaderSource + "output.Color.y = ((input.DiffuseColor0 >> 16) & 0xFF) / 255.0f;\n";
					shaderSource = shaderSource + "output.Color.z = ((input.DiffuseColor0 >> 8) & 0xFF) / 255.0f;\n";
					shaderSource = shaderSource + "output.Color.w = (input.DiffuseColor0 & 0xFF) / 255.0f;\n";
				}


				shaderSource = shaderSource + "float3 N = mul((float3x3)WorldViewIT, input.Normal0);\n";
				shaderSource = shaderSource + "float3 V = -normalize(cameraPos);\n";

				shaderSource = shaderSource + "#define fMaterialPower 16.f\n";

				for ( int i = 0; i < fixedFunctionState.Lights.Count; i++ )
				{
					String prefix = "Light" + i.ToString() + "_";
					switch ( fixedFunctionState.Lights[ i ] )
					{
						case LightType.Point:
							shaderSource = shaderSource + "{\n";
							shaderSource = shaderSource + "  float3 PosDiff = " + prefix + "Position-(float3)mul(World,input.Position0);\n";
							shaderSource = shaderSource + "  float3 L = mul((float3x3)ViewIT, normalize((PosDiff)));\n";
							shaderSource = shaderSource + "  float NdotL = dot(N, L);\n";
							shaderSource = shaderSource + "  float4 Color = " + prefix + "Ambient;\n";
							shaderSource = shaderSource + "  float4 ColorSpec = 0;\n";
							shaderSource = shaderSource + "  float fAtten = 1.f;\n";
							shaderSource = shaderSource + "  if(NdotL >= 0.f)\n";
							shaderSource = shaderSource + "  {\n";
							shaderSource = shaderSource + "    //compute diffuse color\n";
							shaderSource = shaderSource + "    Color += NdotL * " + prefix + "Diffuse;\n";
							shaderSource = shaderSource + "    //add specular component\n";
							shaderSource = shaderSource + "    float3 H = normalize(L + V);   //half vector\n";
							shaderSource = shaderSource + "    ColorSpec = pow(max(0, dot(H, N)), fMaterialPower) * " + prefix + "Specular;\n";
							shaderSource = shaderSource + "    float LD = length(PosDiff);\n";
							shaderSource = shaderSource + "    if(LD > " + prefix + "Range)\n";
							shaderSource = shaderSource + "    {\n";
							shaderSource = shaderSource + "      fAtten = 0.f;\n";
							shaderSource = shaderSource + "    }\n";
							shaderSource = shaderSource + "    else\n";
							shaderSource = shaderSource + "    {\n";
							shaderSource = shaderSource + "      fAtten *= 1.f/(" + prefix + "Attenuation.x + " + prefix + "Attenuation.y*LD + " + prefix + "Attenuation.z*LD*LD);\n";
							shaderSource = shaderSource + "    }\n";
							shaderSource = shaderSource + "    Color *= fAtten;\n";
							shaderSource = shaderSource + "    ColorSpec *= fAtten;\n";
							shaderSource = shaderSource + "    output.Color += Color;\n";
							shaderSource = shaderSource + "    output.ColorSpec += ColorSpec;\n";
							shaderSource = shaderSource + "  }\n";
							shaderSource = shaderSource + "}\n";

							break;
						case LightType.Directional:
							shaderSource = shaderSource + "{\n";
							shaderSource = shaderSource + "  float3 L = mul((float3x3)ViewIT, -normalize(" + prefix + "Direction));\n";
							shaderSource = shaderSource + "  float NdotL = dot(N, L);\n";
							shaderSource = shaderSource + "  float4 Color = " + prefix + "Ambient;\n";
							shaderSource = shaderSource + "  float4 ColorSpec = 0;\n";
							shaderSource = shaderSource + "  if(NdotL > 0.f)\n";
							shaderSource = shaderSource + "  {\n";
							shaderSource = shaderSource + "    //compute diffuse color\n";
							shaderSource = shaderSource + "    Color += NdotL * " + prefix + "Diffuse;\n";
							shaderSource = shaderSource + "    //add specular component\n";
							shaderSource = shaderSource + "    float3 H = normalize(L + V);   //half vector\n";
							shaderSource = shaderSource + "    ColorSpec = pow(max(0, dot(H, N)), fMaterialPower) * " + prefix + "Specular;\n";
							shaderSource = shaderSource + "    output.Color += Color;\n";
							shaderSource = shaderSource + "    output.ColorSpec += ColorSpec;\n";
							shaderSource = shaderSource + "  }\n";
							shaderSource = shaderSource + "}\n";
							break;
						case LightType.Spotlight:
							shaderSource = shaderSource + "{\n";
							shaderSource = shaderSource + "  float3 PosDiff = " + prefix + "Position-(float3)mul(World,input.Position0);\n";
							shaderSource = shaderSource + "   float3 L = mul((float3x3)ViewIT, normalize((PosDiff)));\n";
							shaderSource = shaderSource + "   float NdotL = dot(N, L);\n";
							shaderSource = shaderSource + "   Out.Color = " + prefix + "Ambient;\n";
							shaderSource = shaderSource + "   Out.ColorSpec = 0;\n";
							shaderSource = shaderSource + "   float fAttenSpot = 1.f;\n";
							shaderSource = shaderSource + "   if(NdotL >= 0.f)\n";
							shaderSource = shaderSource + "   {\n";
							shaderSource = shaderSource + "      //compute diffuse color\n";
							shaderSource = shaderSource + "      Out.Color += NdotL * " + prefix + "Diffuse;\n";
							shaderSource = shaderSource + "      //add specular component\n";
							shaderSource = shaderSource + "       float3 H = normalize(L + V);   //half vector\n";
							shaderSource = shaderSource + "       Out.ColorSpec = pow(max(0, dot(H, N)), fMaterialPower) * " + prefix + "Specular;\n";
							shaderSource = shaderSource + "      float LD = length(PosDiff);\n";
							shaderSource = shaderSource + "      if(LD > lights[i].fRange)\n";
							shaderSource = shaderSource + "      {\n";
							shaderSource = shaderSource + "         fAttenSpot = 0.f;\n";
							shaderSource = shaderSource + "      }\n";
							shaderSource = shaderSource + "      else\n";
							shaderSource = shaderSource + "      {\n";
							shaderSource = shaderSource + "         fAttenSpot *= 1.f/(" + prefix + "Attenuation.x + " + prefix + "Attenuation.y*LD + " + prefix + "Attenuation.z*LD*LD);\n";
							shaderSource = shaderSource + "      }\n";
							shaderSource = shaderSource + "      //spot cone computation\n";
							shaderSource = shaderSource + "      float3 L2 = mul((float3x3)ViewIT, -normalize(" + prefix + "Direction));\n";
							shaderSource = shaderSource + "      float rho = dot(L, L2);\n";
							shaderSource = shaderSource + "      fAttenSpot *= pow(saturate((rho - " + prefix + "Spot.y)/(" + prefix + "Spot.x - " + prefix + "Spot.y)), " + prefix + "Spot.z);\n";
							shaderSource = shaderSource + "		Color *= fAttenSpot;\n";
							shaderSource = shaderSource + "		ColorSpec *= fAttenSpot;\n";
							shaderSource = shaderSource + "    output.Color += Color;\n";
							shaderSource = shaderSource + "    output.ColorSpec += ColorSpec;\n";
							shaderSource = shaderSource + "   }\n";
							shaderSource = shaderSource + "}\n";
							break;
					}
				}

			}
			else
			{
				if ( bHasColor )
				{
                    shaderSource = shaderSource + "output.Color = ((input.DiffuseColor0)) / 255.0f;\n";
					/*shaderSource = shaderSource + "output.Color.x = ((input.DiffuseColor0 >> 24) & 0xFF) / 255.0f;\n";
					shaderSource = shaderSource + "output.Color.y = ((input.DiffuseColor0 >> 16) & 0xFF) / 255.0f;\n";
					shaderSource = shaderSource + "output.Color.z = ((input.DiffuseColor0 >> 8) & 0xFF) / 255.0f;\n";
					shaderSource = shaderSource + "output.Color.w = (input.DiffuseColor0 & 0xFF) / 255.0f;\n";*/
				}
				else
				{
					shaderSource = shaderSource + "output.Color = float4(1.0, 1.0, 1.0, 1.0);\n";
				}
			}

			switch ( fixedFunctionState.GeneralFixedFunctionState.FogMode )
			{
				case FogMode.None:
					break;
				case FogMode.Exp:
				case FogMode.Exp2:
				case FogMode.Linear:
					shaderSource = shaderSource + "output.fogDist = length(cameraPos.xyz);\n";
					break;
			}

			shaderSource = shaderSource + "return output;}\n";

			// here starts the fragment shader

			shaderSource = shaderSource + "float4x4  TextureMatrix;\n";
			shaderSource = shaderSource + "float4  FogColor;\n";



			if ( bHasTexcoord )
			{
				shaderSource = shaderSource + "sampler tex0 : register(s0);\n";
                shaderSource = shaderSource + "float4 " + fragmentProgramName + "( VS_OUTPUT input ) : COLOR\n";// "( VS_OUTPUT input ) : SV_Target\n";
				shaderSource = shaderSource + "{\n";
				shaderSource = shaderSource + "float4 texCordWithMatrix = float4(input.tCord.x, input.tCord.y, 0, 1);\n";
				shaderSource = shaderSource + "texCordWithMatrix = mul( texCordWithMatrix, TextureMatrix );\n";
				shaderSource = shaderSource + "\n";
				shaderSource = shaderSource + "float4 finalColor = tex2D(tex0,texCordWithMatrix.xy)  * input.Color + input.ColorSpec;\n";
			}
			else
			{
				shaderSource = shaderSource + "float4 " + fragmentProgramName + "( VS_OUTPUT input ) : SV_Target\n";
				shaderSource = shaderSource + "{\n";
                shaderSource = shaderSource + "float4 finalColor = input.Color + input.ColorSpec;\n"; //"float4 finalColor = input.colorD + input.colorS;\n";
			}

			switch ( fixedFunctionState.GeneralFixedFunctionState.FogMode )
			{
				case FogMode.None:
					break;
				case FogMode.Exp:
					shaderSource = shaderSource + "#define E 2.71828\n";
					shaderSource = shaderSource + "input.fogDist = 1.0 / pow( E, input.fogDist*FogDensity );\n";
					shaderSource = shaderSource + "input.fogDist = clamp( input.fogDist, 0, 1 );\n";
					break;
				case FogMode.Exp2:
					shaderSource = shaderSource + "#define E 2.71828\n";
					shaderSource = shaderSource + "input.fogDist = 1.0 / pow( E, input.fogDist*input.fogDist*FogDensity*FogDensity );\n";
					shaderSource = shaderSource + "input.fogDist = clamp( input.fogDist, 0, 1 );\n";
					break;
				case FogMode.Linear:
					shaderSource = shaderSource + "input.fogDist = (FogEnd - input.fogDist)/(FogEnd - FogStart);\n";
					shaderSource = shaderSource + "input.fogDist = clamp( input.fogDist, 0, 1 );\n";
					break;
			}

			if ( fixedFunctionState.GeneralFixedFunctionState.FogMode != FogMode.None )
			{

				shaderSource = shaderSource + "finalColor.xyz = input.fogDist * finalColor.xyz + (1.0 - input.fogDist)*FogColor.xyz;\n";

			}

			shaderSource = shaderSource + "return finalColor;\n}";
			return shaderSource;
		}

		#endregion ShaderGenerator Implementation
	}
}
