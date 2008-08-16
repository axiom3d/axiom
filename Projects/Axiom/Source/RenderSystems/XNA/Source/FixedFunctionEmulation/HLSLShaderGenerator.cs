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
        internal class TextureCoordType : Dictionary<int, VertexElementType>
        {}
        protected TextureCoordType texCordVecType = new TextureCoordType();
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
            bool bHasNormal = false;
			bool bHasTexcoord = vertexBufferDeclaration.HasTexCoord;
            uint texcoordCount = vertexBufferDeclaration.TexCoordCount;
            
            
			String shaderSource = "";

			shaderSource = shaderSource + "struct VS_INPUT\n{\n";
            
			ushort[] semanticCount = new ushort[ 100 ];

			IEnumerable<VertexBufferElement> vertexBufferElements = vertexBufferDeclaration.VertexBufferElements;
			foreach ( VertexBufferElement element in vertexBufferElements )
			{
				VertexElementSemantic semantic = element.VertexElementSemantic;
				VertexElementType type = element.VertexElementType;

				String thisElementSemanticCount = semanticCount[ (int)semantic ].ToString();
                semanticCount[(int)semantic]++;
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
                        parameterType = "float4";
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
                        bHasNormal = true;
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
                        if (!texCordVecType.ContainsKey((int)semanticCount[(int)semantic] - 1))
                            texCordVecType.Add((int)semanticCount[(int)semantic] - 1, type);//[semanticCount[(int)semantic] - 1] = type;
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

                shaderSource = shaderSource + "\t"+ parameterType + " " + parameterName + thisElementSemanticCount + " : " + parameterShaderTypeName + thisElementSemanticCount + ";\n";
			}

			shaderSource = shaderSource + "};\n\n";



            shaderSource = shaderSource + "float4x4  World;\n";
            shaderSource = shaderSource + "float4x4  View;\n";
            shaderSource = shaderSource + "float4x4  Projection;\n";
            shaderSource = shaderSource + "float4x4  ViewIT;\n";
            shaderSource = shaderSource + "float4x4  WorldViewIT;\n";


            
            for(uint i = 0 ; i < fixedFunctionState.TextureLayerStates.Count; i++)
    		{
	    		String layerCounter = Convert.ToString(i);
                shaderSource = shaderSource + "float4x4  TextureMatrix" + layerCounter + ";\n";
		    }

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

			shaderSource = shaderSource + "\nstruct VS_OUTPUT\n";
			shaderSource = shaderSource + "{\n";
            shaderSource = shaderSource + "\tfloat4 Pos : POSITION;\n";//"float4 Pos : SV_POSITION;\n"; //SV not recognised

            for (int i = 0; i < fixedFunctionState.TextureLayerStates.Count; i++)
		    {
			    String layerCounter = Convert.ToString(i);

               // TextureLayerState curTextureLayerState = (TextureLayerState)fixedFunctionState.TextureLayerStates[i];
                
                if(texCordVecType.ContainsKey(i))
                switch (texCordVecType[i])
			    {
			    case VertexElementType.Float1:
                    shaderSource = shaderSource + "\tfloat1 Texcoord" + layerCounter + " : TEXCOORD" + layerCounter + ";\n";
				    break;
                case VertexElementType.Float2:
                    shaderSource = shaderSource + "\tfloat2 Texcoord" + layerCounter + " : TEXCOORD" + layerCounter + ";\n";
				    break;
                case VertexElementType.Float3:
                    shaderSource = shaderSource + "\tfloat3 Texcoord" + layerCounter + " : TEXCOORD" + layerCounter + ";\n";
				    break;
			    }

		    }

            shaderSource = shaderSource + "\tfloat4 Color : COLOR0;\n";
            shaderSource = shaderSource + "\tfloat4 ColorSpec : COLOR1;\n";

			if ( fixedFunctionState.GeneralFixedFunctionState.FogMode != FogMode.None )
			{
                shaderSource = shaderSource + "\tfloat fogDist : COLOR2;\n"; //"float fogDist : FOGDISTANCE;\n";
			}

			shaderSource = shaderSource + "};\n";

			shaderSource = shaderSource + "\nVS_OUTPUT " + vertexProgramName + "( VS_INPUT input )\n";
			shaderSource = shaderSource + "{\n";
            shaderSource = shaderSource + "\tVS_OUTPUT output = (VS_OUTPUT)0;\n";
            shaderSource = shaderSource + "\tfloat4 worldPos = mul( World, float4( input.Position0 , 1 ));\n";
            shaderSource = shaderSource + "\tfloat4 cameraPos = mul( View, worldPos );\n";
            shaderSource = shaderSource + "\toutput.Pos = mul( Projection, cameraPos );\n";


            if (bHasNormal)
            {
                shaderSource = shaderSource + "\tfloat3 Normal = input.Normal0;\n";
            }
            else
            {
                shaderSource = shaderSource + "\tfloat3 Normal = float3(0.0, 0.0, 0.0);\n";
            }
		

            for(int i = 0 ; i < fixedFunctionState.TextureLayerStates.Count; i++)
            {
                TextureLayerState curTextureLayerState = fixedFunctionState.TextureLayerStates[i];
			    String layerCounter = Convert.ToString(i);
			    String coordIdx = Convert.ToString(curTextureLayerState.CoordIndex);

			    shaderSource = shaderSource + "\t{\n";

                switch (curTextureLayerState.TexCoordCalcMethod)
                {
                    case TexCoordCalcMethod.None:
                        if (curTextureLayerState.CoordIndex < texcoordCount)
                        {
                            shaderSource = shaderSource + "TextureMatrix" + layerCounter + "=float4x4(1.0,0.0,0.0,0.0, 0.0,1.0,0.0,0.0, 0.0,0.0,1.0,0.0, 0.0,0.0,0.0,1.0);\n";
                            if (texCordVecType.ContainsKey(i))
                                switch (texCordVecType[i])
                                {

                                    case VertexElementType.Float1:
                                        shaderSource = shaderSource + "\t\toutput.Texcoord" + layerCounter + " = input.Texcoord" + coordIdx + ";\n";
                                        break;
                                    case VertexElementType.Float2:
                                        shaderSource = shaderSource + "\t\tfloat4 texCordWithMatrix = float4(input.Texcoord" + coordIdx + ".x, input.Texcoord" + coordIdx + ".y, 0, 1);\n";
                                        shaderSource = shaderSource + "\t\ttexCordWithMatrix = mul(texCordWithMatrix, TextureMatrix" + layerCounter + " );\n";
                                        shaderSource = shaderSource + "\t\toutput.Texcoord" + layerCounter + " = texCordWithMatrix.xy;\n";
                                        break;
                                    case VertexElementType.Float3:
                                        shaderSource = shaderSource + "\t\toutput.Texcoord" + layerCounter + " = input.Texcoord" + coordIdx + ";\n";
                                        break;
                                }

                        }
                        else
                        {
                            if (texCordVecType.ContainsKey(i))
                                switch (texCordVecType[i])
                                {
                                    case VertexElementType.Float1:
                                        shaderSource = shaderSource + "\t\toutput.Texcoord" + layerCounter + " = 0.0;\n"; // so no error
                                        break;
                                    case VertexElementType.Float2:
                                        shaderSource = shaderSource + "\t\toutput.Texcoord" + layerCounter + " = float2(0.0, 0.0);\n"; // so no error
                                        break;
                                    case VertexElementType.Float3:
                                        shaderSource = shaderSource + "\t\toutput.Texcoord" + layerCounter + " = float3(0.0, 0.0, 0.0);\n"; // so no error
                                        break;
                                }
                        }
                        break;

                    case TexCoordCalcMethod.EnvironmentMap:
                        //shaderSource = shaderSource + "float3 ecPosition3 = cameraPos.xyz/cameraPos.w;\n";
                        shaderSource = shaderSource + "\t\tfloat3 u = normalize(cameraPos.xyz);\n";
                        shaderSource = shaderSource + "\t\tfloat3 r = reflect(u, Normal);\n";
                        shaderSource = shaderSource + "\t\tfloat  m = 2.0 * sqrt(r.x * r.x + r.y * r.y + (r.z + 1.0) * (r.z + 1.0));\n";
                        shaderSource = shaderSource + "\t\toutput.Texcoord" + layerCounter + " = float2 (r.x / m + 0.5, r.y / m + 0.5);\n";
                        break;
                    case TexCoordCalcMethod.EnvironmentMapPlanar:
                        break;
                    case TexCoordCalcMethod.EnvironmentMapReflection:
                        //assert(curTextureLayerState.getTextureType() == TEX_TYPE_CUBE_MAP);
                        shaderSource = shaderSource + "\t{\n";
                        shaderSource = shaderSource + "\t\tfloat4 worldNorm = mul(float4(Normal, 0), World);\n";
                        shaderSource = shaderSource + "\t\tfloat4 viewNorm = mul(worldNorm, View);\n";
                        shaderSource = shaderSource + "\t\tviewNorm = normalize(viewNorm);\n";
                        shaderSource = shaderSource + "\t\toutput.Texcoord" + layerCounter + " = reflect(viewNorm.xyz, float3(0.0,0.0,-1.0));\n";
                        shaderSource = shaderSource + "\t}\n";
                        break;
                    case TexCoordCalcMethod.EnvironmentMapNormal:
                        break;
                    case TexCoordCalcMethod.ProjectiveTexture:
                        if (texCordVecType.ContainsKey(i))
                            switch (texCordVecType[i])
                            {
                                case VertexElementType.Float1:
                                    shaderSource = shaderSource + "\t{\n";
                                    shaderSource = shaderSource + "\t\tfloat4 cameraPosNorm = normalize(cameraPos);\n";
                                    shaderSource = shaderSource + "\t\toutput.Texcoord" + layerCounter + ".x = 0.5 + cameraPosNorm.x;\n";
                                    shaderSource = shaderSource + "\t}\n";
                                    break;
                                case VertexElementType.Float2:
                                case VertexElementType.Float3:
                                    shaderSource = shaderSource + "\t{\n";
                                    shaderSource = shaderSource + "\t\tfloat4 cameraPosNorm = normalize(cameraPos);\n";
                                    shaderSource = shaderSource + "\t\toutput.Texcoord" + layerCounter + ".x = 0.5 + cameraPosNorm.x;\n";
                                    shaderSource = shaderSource + "\t\toutput.Texcoord" + layerCounter + ".y = 0.5 - cameraPosNorm.y;\n";
                                    shaderSource = shaderSource + "\t}\n";
                                    break;
                            }
                        break;
                }
                shaderSource = shaderSource + "\t}\n";
            }


			
            shaderSource = shaderSource + "\toutput.ColorSpec = float4(0.0, 0.0, 0.0, 0.0);\n";


			if ( fixedFunctionState.GeneralFixedFunctionState.EnableLighting && fixedFunctionState.Lights.Count > 0 )
			{
				shaderSource = shaderSource + "\t\toutput.Color = BaseLightAmbient;\n";
				if ( bHasColor )
				{
					shaderSource = shaderSource + "\t\toutput.Color.x = ((input.DiffuseColor0 >> 24) & 0xFF) / 255.0f;\n";
					shaderSource = shaderSource + "\t\toutput.Color.y = ((input.DiffuseColor0 >> 16) & 0xFF) / 255.0f;\n";
					shaderSource = shaderSource + "\t\toutput.Color.z = ((input.DiffuseColor0 >> 8) & 0xFF) / 255.0f;\n";
					shaderSource = shaderSource + "\t\toutput.Color.w = (input.DiffuseColor0 & 0xFF) / 255.0f;\n";
				}


				shaderSource = shaderSource + "\t\tfloat3 N = mul((float3x3)WorldViewIT, input.Normal0);\n";
				shaderSource = shaderSource + "\t\tfloat3 V = -normalize(cameraPos);\n";

				shaderSource = shaderSource + "\t\t#define fMaterialPower 16.f\n";

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
                    shaderSource = shaderSource + "\toutput.Color = ((input.DiffuseColor0)) / 255.0f;\n";
					/*shaderSource = shaderSource + "output.Color.x = ((input.DiffuseColor0 >> 24) & 0xFF) / 255.0f;\n";
					shaderSource = shaderSource + "output.Color.y = ((input.DiffuseColor0 >> 16) & 0xFF) / 255.0f;\n";
					shaderSource = shaderSource + "output.Color.z = ((input.DiffuseColor0 >> 8) & 0xFF) / 255.0f;\n";
					shaderSource = shaderSource + "output.Color.w = (input.DiffuseColor0 & 0xFF) / 255.0f;\n";*/
				}
				else
				{
					shaderSource = shaderSource + "\toutput.Color = float4(1.0, 1.0, 1.0, 1.0);\n";
				}
			}

            switch (fixedFunctionState.GeneralFixedFunctionState.FogMode)
            {
                case FogMode.None:
                    break;
                case FogMode.Exp:
                    shaderSource = shaderSource + "\t#define E 2.71828\n";
                    shaderSource = shaderSource + "\toutput.fogDist = 1.0 / pow( E, output.fogDist*FogDensity );\n";
                    shaderSource = shaderSource + "\toutput.fogDist = clamp( output.fogDist, 0, 1 );\n";
                    break;
                case FogMode.Exp2:
                    shaderSource = shaderSource + "\t#define E 2.71828\n";
                    shaderSource = shaderSource + "\toutput.fogDist = 1.0 / pow( E, output.fogDist*output.fogDist*FogDensity*FogDensity );\n";
                    shaderSource = shaderSource + "\toutput.fogDist = clamp( output.fogDist, 0, 1 );\n";
                    break;
                case FogMode.Linear:
                    shaderSource = shaderSource + "\toutput.fogDist = (FogEnd - output.fogDist)/(FogEnd - FogStart);\n";
                    shaderSource = shaderSource + "\toutput.fogDist = clamp( output.fogDist, 0, 1 );\n";
                    break;
            }
			shaderSource = shaderSource + "\treturn output;\n}\n\n";











            /////////////////////////////////////
			// here starts the fragment shader
            /////////////////////////////////////

            for(int i = 0 ; i < fixedFunctionState.TextureLayerStates.Count; i++)
		    {
                String layerCounter = Convert.ToString(i);
			    shaderSource = shaderSource + "sampler Texture" + layerCounter + " : register(s" + layerCounter + ");\n";
		    }
			shaderSource = shaderSource + "float4  FogColor;\n";


            shaderSource = shaderSource + "\nfloat4 " + fragmentProgramName + "( VS_OUTPUT input ) : COLOR\n";// SV_Target\n";
		    shaderSource = shaderSource + "{\n";

		    shaderSource = shaderSource + "\tfloat4 finalColor= input.Color + input.ColorSpec;\n";

           
            for (int i = 0; i < fixedFunctionState.TextureLayerStates.Count; i++)
            {
                shaderSource = shaderSource + "\t{\n\tfloat4 texColor=float4(1.0,1.0,1.0,1.0);\n";
                TextureLayerState curTextureLayerState = fixedFunctionState.TextureLayerStates[i];
                String layerCounter = Convert.ToString(i);
               
                switch (curTextureLayerState.TextureType)
                {
                    case TextureType.OneD:
                        {
                            if (texCordVecType.ContainsKey(i))
                                switch (texCordVecType[i])
                                {
                                    case VertexElementType.Float1:
                                        shaderSource = shaderSource + "\t\texColor = tex1D(Texture" + layerCounter + ", input.Texcoord" + layerCounter + ");\n";
                                        break;
                                    case VertexElementType.Float2:
                                        shaderSource = shaderSource + "\t\ttexColor = tex1D(Texture" + layerCounter + ", input.Texcoord" + layerCounter + ".x);\n";
                                        break;
                                }
                        }
                        break;
                    case TextureType.TwoD:
                        {
                            if (texCordVecType.ContainsKey(i))
                                switch (texCordVecType[i])
                                {
                                    case VertexElementType.Float1:
                                        shaderSource = shaderSource + "\t\ttexColor  = tex2D(Texture" + layerCounter + ", float2(input.Texcoord" + layerCounter + ", 0.0));\n";
                                        break;
                                    case VertexElementType.Float2:
                                        shaderSource = shaderSource + "\t\ttexColor  = tex2D(Texture" + layerCounter + ", input.Texcoord" + layerCounter + ");\n";
                                        break;
                                }
                        }

                        break;
                    case TextureType.CubeMap:
                    case TextureType.ThreeD:
                        if (texCordVecType.ContainsKey(i))
                            switch (texCordVecType[i])
                            {
                                case VertexElementType.Float1:
                                    shaderSource = shaderSource + "\t\ttexColor  = tex3D(Texture" + layerCounter + ", float3(input.Texcoord" + layerCounter + ", 0.0, 0.0));\n";
                                    break;
                                case VertexElementType.Float2:
                                    shaderSource = shaderSource + "\t\ttexColor  = tex3D(Texture" + layerCounter + ", float3(input.Texcoord" + layerCounter + ".x, input.Texcoord" + layerCounter + ".y, 0.0));\n";
                                    break;
                                case VertexElementType.Float3:
                                    shaderSource = shaderSource + "\t\ttexColor  = tex3D(Texture" + layerCounter + ", input.Texcoord" + layerCounter + ");\n";
                                    break;
                            }
                        break;
                }


               
                LayerBlendModeEx blend = curTextureLayerState.LayerBlendModeEx;
                switch (blend.source1)
                {
                    case LayerBlendSource.Current:
                        shaderSource = shaderSource + "\t\tfloat4 source1 = finalColor;\n";
                        break;
                    case LayerBlendSource.Texture:
                        shaderSource = shaderSource + "\t\tfloat4 source1 = texColor;\n";
                        break;
                    case LayerBlendSource.Diffuse:
                        shaderSource = shaderSource + "\t\tfloat4 source1 = input.Color;\n";
                        break;
                    case LayerBlendSource.Specular:
                        shaderSource = shaderSource + "\t\tfloat4 source1 = input.ColorSpec;\n";
                        break;
                    case LayerBlendSource.Manual:
                        shaderSource = shaderSource + "\t\tfloat4 source1 = Texture" + layerCounter + "_colourArg1;\n";
                        break;
                }
                switch (blend.source2)
                {
                    case LayerBlendSource.Current:
                        shaderSource = shaderSource + "\t\tfloat4 source2 = finalColor;\n";
                        break;
                    case LayerBlendSource.Texture:
                        shaderSource = shaderSource + "\t\tfloat4 source2 = texColor;\n";
                        break;
                    case LayerBlendSource.Diffuse:
                        shaderSource = shaderSource + "\t\tfloat4 source2 = input.Color;\n";
                        break;
                    case LayerBlendSource.Specular:
                        shaderSource = shaderSource + "\t\tfloat4 source2 = input.ColorSpec;\n";
                        break;
                    case LayerBlendSource.Manual:
                        shaderSource = shaderSource + "\t\tfloat4 source2 = Texture" + layerCounter + "_colourArg2;\n";
                        break;
                }

                switch (blend.operation)
                {
                    case LayerBlendOperationEx.Source1:
                        shaderSource = shaderSource + "\t\tfinalColor = source1;\n";
                        break;
                    case LayerBlendOperationEx.Source2:
                        shaderSource = shaderSource + "\t\tfinalColor = source2;\n";
                        break;
                    case LayerBlendOperationEx.Modulate:
                        shaderSource = shaderSource + "\t\tfinalColor = source1 * source2;\n";
                        break;
                    case LayerBlendOperationEx.ModulateX2:
                        shaderSource = shaderSource + "\t\tfinalColor = source1 * source2 * 2.0;\n";
                        break;
                    case LayerBlendOperationEx.ModulateX4:
                        shaderSource = shaderSource + "\t\tfinalColor = source1 * source2 * 4.0;\n";
                        break;
                    case LayerBlendOperationEx.Add:
                        shaderSource = shaderSource + "\t\tfinalColor = source1 + source2;\n";
                        break;
                    case LayerBlendOperationEx.AddSigned:
                        shaderSource = shaderSource + "\t\tfinalColor = source1 + source2 - 0.5;\n";
                        break;
                    case LayerBlendOperationEx.AddSmooth:
                        shaderSource = shaderSource + "\t\tfinalColor = source1 + source2 - (source1 * source2);\n";
                        break;
                    case LayerBlendOperationEx.Subtract:
                        shaderSource = shaderSource + "\t\tfinalColor = source1 - source2;\n";
                        break;
                    case LayerBlendOperationEx.BlendDiffuseAlpha:
                        shaderSource = shaderSource + "\t\tfinalColor = source1 * input.Color.w + source2 * (1.0 - input.Color.w);\n";
                        break;
                    case LayerBlendOperationEx.BlendTextureAlpha:
                        shaderSource = shaderSource + "\t\tfinalColor = source1 * texColor.w + source2 * (1.0 - texColor.w);\n";
                        break;
                    case LayerBlendOperationEx.BlendCurrentAlpha:
                        shaderSource = shaderSource + "\t\tfinalColor = source1 * finalColor.w + source2 * (1.0 - finalColor.w);\n";
                        break;
                    case LayerBlendOperationEx.BlendManual:
                        shaderSource = shaderSource + "\t\tfinalColor = source1 * " + Convert.ToString(blend.blendFactor) +
                                                        " + source2 * (1.0 - " + Convert.ToString(blend.blendFactor) + ");\n";
                        break;
                    case LayerBlendOperationEx.DotProduct:
                        shaderSource = shaderSource + "\t\tfinalColor = product(source1,source2);\n";
                        break;
                    //case LayerBlendOperationEx.. LBX_BLEND_DIFFUSE_COLOUR:
                    //  shaderSource = shaderSource + "finalColor = source1 * input.Color + source2 * (float4(1.0,1.0,1.0,1.0) - input.Color);\n";
                    //break;
                }
                shaderSource = shaderSource + "finalColor=finalColor*texColor;\n";

                shaderSource = shaderSource + "\t}\n";
            }
       
            if ( fixedFunctionState.GeneralFixedFunctionState.FogMode != FogMode.None )
			{
                //just for testing for now...
                shaderSource = shaderSource + "\tinput.fogDist=1.0;\n";
                shaderSource = shaderSource + "\tFogColor=float4(1.0,1.0,1.0,1.0);\n";
                
                shaderSource = shaderSource + "\tfinalColor = input.fogDist * finalColor + (1.0 - input.fogDist)*FogColor;\n";
			}

			shaderSource = shaderSource + "\treturn finalColor;\n}\n";

            
            
            
            return shaderSource;
		}

        public override FixedFunctionPrograms CreateFixedFunctionPrograms()
        {
            return new HLSLFixedFunctionProgram();
        }

		#endregion ShaderGenerator Implementation
    }
}
