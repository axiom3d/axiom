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
//     <id value="$Id:"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using Axiom.Graphics;

using XNA = Microsoft.Xna.Framework;
using XFG = Microsoft.Xna.Framework.Graphics;
using Axiom.RenderSystems.Xna.HLSL;
using System;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.Xna
{
    /// <summary>
    ///	Xna Fixed Function Emulation
    /// </summary>
    internal class XnaFixedFunctionEmulation
	{
		#region Constants

		private const string _vsSource =		"float4x4  World;\n" +
												"float4x4  View;\n" +
												"float4x4  Projection;\n" +
												"struct VS_OUTPUT\n" +
												"{\n" +
												"float4 Pos : POSITION0;\n" +
												"float4 col : COLOR0;\n" +
												"};\n" +
												"VS_OUTPUT VS( VS_INPUT input )\n" +
												"{\n" +
												"VS_OUTPUT output = (VS_OUTPUT)0;\n" +
												"output.Pos = mul( input.Pos, World );\n" +
												"output.Pos = mul( output.Pos, View );\n" +
												"output.Pos = mul( output.Pos, Projection );\n" +
												"output.col = 0;\n" +
												"return output;}\n";

		private const string _vsSourceTexture = "float4x4  World;\n" + 
												"float4x4  View;\n" +
												"float4x4  Projection;\n" +
												"struct VS_OUTPUT\n" +
												"{\n" +
												"float4 Pos : POSITION0;\n" +
												"float2 tCord : TEXCOORD0;\n" +
												"};\n" +
												"VS_OUTPUT VS( VS_INPUT input )\n" +
												"{\n" +
												"VS_OUTPUT output = (VS_OUTPUT)0;\n" +
												"output.Pos = mul( input.Pos, World );\n" +
												"output.Pos = mul( output.Pos, View );\n" +
												"output.Pos = mul( output.Pos, Projection );\n" +
												"output.tCord = input.tCord;\n" +
												"return output;}\n";

		private const string _vsSourceColor =	"float4x4  World;\n" +
												"float4x4  View;\n" +
												"float4x4  Projection;\n" +
												"struct VS_OUTPUT\n" +
												"{\n" +
												"float4 Pos : POSITION0;\n" +
												"float4 col : COLOR0;\n" +
												"};\n" +
												"VS_OUTPUT VS( VS_INPUT input )\n" +
												"{\n" +
												"VS_OUTPUT output = (VS_OUTPUT)0;\n" +
												"output.Pos = mul( input.Pos, World );\n" +
												"output.Pos = mul( output.Pos, View );\n" +
												"output.Pos = mul( output.Pos, Projection );\n" +
												"output.col = input.col;\n" +
												"return output;}\n";

		private const string _vsSourceTextureColor =
												"float4x4  World;\n" +
												"float4x4  View;\n" +
												"float4x4  Projection;\n" +
												"struct VS_OUTPUT\n" +
												"{\n" +
												"float4 Pos : POSITION0;\n" +
												"float2 tCord : TEXCOORD0;\n" +
												"float4 col : COLOR0;\n" +
												"};\n" +
												"VS_OUTPUT VS( VS_INPUT input )\n" +
												"{\n" +
												"VS_OUTPUT output = (VS_OUTPUT)0;\n" +
												"output.Pos = mul( input.Pos, World );\n" +
												"output.Pos = mul( output.Pos, View );\n" +
												"output.Pos = mul( output.Pos, Projection );\n" +
												"output.tCord = input.tCord;\n" +
												"output.col = input.col;\n" +
												"return output;}\n";

		#endregion Constants

		#region Fields & Properties

		private static XnaRenderSystem _rs;
		private static XFG.GraphicsDevice _device;

		private static HLSLProgram _defaultVSPosition;
		private static HLSLProgram _defaultVSPositionTexture;
		private static HLSLProgram _defaultVSPositionColor;
		private static HLSLProgram _defaultVSPositionTextureColor;
		private static HLSLProgram _defaultVSPositionNormalTexture;
		private static HLSLProgram _defaultVSPositionNormalColor;
		private static HLSLProgram _defaultFPTextureColor;
		private static HLSLProgram _defaultFPTexture;
		private static HLSLProgram _defaultFPColor;

		private static GpuProgramParameters _defaultVPParamters;

		private static bool _unBindVS;
		private static bool _unBindFS;

		private static bool _initialized;

		#endregion Fields & Properties
			
		internal XnaFixedFunctionEmulation( XnaRenderSystem rs, XFG.GraphicsDevice device )
		{
			_initialize( rs, device );
		}

		private void _initialize( XnaRenderSystem rs, XFG.GraphicsDevice device )
		{
			lock ( this )
			{
				if ( !_initialized )
				{
					_rs = rs;
					_device = device;

					#region defaultVSPosition
					_defaultVSPosition = (HLSLProgram)HighLevelGpuProgramManager.Instance.CreateProgram( "_defaultVSPosition", "hlsl", GpuProgramType.Vertex );
					_defaultVSPosition.Source = "struct VS_INPUT { float4 Pos : POSITION0; };" +
													   _vsSource;
					_defaultVSPosition.SetParam( "entry_point", "VS" );
					_defaultVSPosition.Load();
					#endregion defaultPosition

					#region defaultVSPositionTexture
					_defaultVSPositionTexture = (HLSLProgram)HighLevelGpuProgramManager.Instance.CreateProgram( "_defaultVSPositionTexture", "hlsl", GpuProgramType.Vertex );
					_defaultVSPositionTexture.Source = "struct VS_INPUT { float4 Pos : POSITION0; float2 tCord : TEXCOORD0 ;};" +
													   _vsSourceTexture;
					_defaultVSPositionTexture.SetParam( "entry_point", "VS" );
					_defaultVSPositionTexture.Load();
					#endregion defaultPositionTexture

					#region defaultVSPositionColor
					_defaultVSPositionColor = (HLSLProgram)HighLevelGpuProgramManager.Instance.CreateProgram( "_defaultVSPositionColor", "hlsl", GpuProgramType.Vertex );
					_defaultVSPositionColor.Source = "struct VS_INPUT { float4 Pos : POSITION0; float4 col : COLOR0 ;};" +
													   _vsSourceColor;
					_defaultVSPositionColor.SetParam( "entry_point", "VS" );
					_defaultVSPositionColor.Load();
					#endregion defaultPositionColor

					#region defaultVSPositionTextureColor
					_defaultVSPositionTextureColor = (HLSLProgram)HighLevelGpuProgramManager.Instance.CreateProgram( "_defaultVSPositionTextureColor", "hlsl", GpuProgramType.Vertex );
					_defaultVSPositionTextureColor.Source = "struct VS_INPUT { float4 Pos : POSITION0; float2 tCord : TEXCOORD0; float4 col : COLOR0 ;};" +
													        _vsSourceTextureColor;
					_defaultVSPositionTextureColor.SetParam( "entry_point", "VS" );
					_defaultVSPositionTextureColor.Load();
					#endregion defaultVSPositionTextureColor

					#region defaultVSPositionNormalTexture
					_defaultVSPositionNormalTexture = (HLSLProgram)HighLevelGpuProgramManager.Instance.CreateProgram( "_defaultVSPositionNormalTexture", "hlsl", GpuProgramType.Vertex );
					_defaultVSPositionNormalTexture.Source = "struct VS_INPUT { float4 Pos : POSITION0; float3 Normal : NORMAL; float2 tCord : TEXCOORD0; };" +
													         _vsSourceTexture;
					_defaultVSPositionNormalTexture.SetParam( "entry_point", "VS" );
					_defaultVSPositionNormalTexture.Load();
					#endregion defaultPositionNormalTexture

					#region defaultVSPositionNormalColor
					_defaultVSPositionNormalColor = (HLSLProgram)HighLevelGpuProgramManager.Instance.CreateProgram( "_defaultVSPositionNormalColor", "hlsl", GpuProgramType.Vertex );
					_defaultVSPositionNormalColor.Source = "struct VS_INPUT { float4 Pos : POSITION0; float3 norm : NORMAL0; float4 col : COLOR0 ;};" +
														   _vsSourceColor;
					_defaultVSPositionNormalColor.SetParam( "entry_point", "VS" );
					_defaultVSPositionNormalColor.Load();
					#endregion defaultPositionNormalColor

					#region defaultFPTexture
					_defaultFPTexture = (HLSLProgram)HighLevelGpuProgramManager.Instance.CreateProgram( "_defaultFPTexture", "hlsl", GpuProgramType.Fragment );
					_defaultFPTexture.Source = "sampler tex0 : register(s0); float4 PS( float4 Pos : POSITION0, float2 tCord : TEXCOORD0 ) : COLOR0 {	return tex2D(tex0,tCord); }";
					_defaultFPTexture.SetParam( "entry_point", "PS" );
					_defaultFPTexture.Load();
					#endregion defaultFPTexture

					#region defaultFPColor
					_defaultFPColor = (HLSLProgram)HighLevelGpuProgramManager.Instance.CreateProgram( "_defaultFPColor", "hlsl", GpuProgramType.Fragment );
					_defaultFPColor.Source = "sampler tex0 : register(s0); float4 PS( float4 Pos : POSITION0, float4 col : COLOR0) : COLOR0 {	return col; }";
					_defaultFPColor.SetParam( "entry_point", "PS" );
					_defaultFPColor.Load();
					#endregion defaultFPColor

					#region defaultFPTextureColor
					_defaultFPTextureColor = (HLSLProgram)HighLevelGpuProgramManager.Instance.CreateProgram( "_defaultFPTextureColor", "hlsl", GpuProgramType.Fragment );
					_defaultFPTextureColor.Source = "sampler tex0 : register(s0); float4 PS( float4 Pos : POSITION0, float2 tCord : TEXCOORD0, float4 col : COLOR0 ) : COLOR0 {	return tex2D(tex0,tCord) * col; }";
					_defaultFPTextureColor.SetParam( "entry_point", "PS" );
					_defaultFPTextureColor.Load();
					#endregion defaultFPTextureColor

					_defaultVPParamters = _defaultVSPositionNormalColor.CreateParameters();
					_defaultVPParamters.TransposeMatrices = true;
					_defaultVPParamters.AutoAddParamName = true;

					_initialized = true;
				}
			}
		}

		public bool BeginEmulation( RenderOperation op )
		{
			// Need both Begin Emulate will return true if either are not present
			bool emulationNeeded = ( _device.VertexShader == null || _device.PixelShader == null );

			bool bindTextureDefault = true;
			bool bindTextureAndColorDefault = false;

			if ( _device.VertexShader == null )
			{
				if ( ( op.vertexData.vertexDeclaration.ElementCount == 1 ) &&
					 ( op.vertexData.vertexDeclaration[ 0 ].Semantic == VertexElementSemantic.Position ) )
				{
					_rs.BindGpuProgram( _defaultVSPosition.BindingDelegate );
					_unBindVS = true;
				}
				else if ( ( op.vertexData.vertexDeclaration.ElementCount == 2 ) && 
					 ( op.vertexData.vertexDeclaration[ 0 ].Semantic == VertexElementSemantic.Position )  )
				{
					if ( ( op.vertexData.vertexDeclaration[ 1 ].Semantic == VertexElementSemantic.TexCoords ) )
					{
						_rs.BindGpuProgram( _defaultVSPositionTexture.BindingDelegate );
						_unBindVS = true;
					}
					else if ( ( op.vertexData.vertexDeclaration[ 1 ].Semantic == VertexElementSemantic.Diffuse ) )
					{
						bindTextureDefault = false;
						_rs.BindGpuProgram( _defaultVSPositionColor.BindingDelegate );
						_unBindVS = true;
					}
				}
				else if ( ( op.vertexData.vertexDeclaration.ElementCount == 3 ) &&
						  ( op.vertexData.vertexDeclaration[ 0 ].Semantic == VertexElementSemantic.Position ) )
				{
					if ( ( op.vertexData.vertexDeclaration[ 1 ].Semantic == VertexElementSemantic.Diffuse ) &&
						 ( op.vertexData.vertexDeclaration[ 2 ].Semantic == VertexElementSemantic.TexCoords ) )
					{
						bindTextureAndColorDefault = true;
						_rs.BindGpuProgram( _defaultVSPositionTexture.BindingDelegate );
						_unBindVS = true;
					}

					if ( ( op.vertexData.vertexDeclaration[ 1 ].Semantic == VertexElementSemantic.TexCoords ) &&
						 ( op.vertexData.vertexDeclaration[ 2 ].Semantic == VertexElementSemantic.Diffuse ) )
					{
						bindTextureAndColorDefault = true;
						_rs.BindGpuProgram( _defaultVSPositionTextureColor.BindingDelegate );
						_unBindVS = true;
					}

					if ( ( op.vertexData.vertexDeclaration[ 1 ].Semantic == VertexElementSemantic.Normal ) &&
						 ( op.vertexData.vertexDeclaration[ 2 ].Semantic == VertexElementSemantic.TexCoords ) )
					{
						_rs.BindGpuProgram( _defaultVSPositionNormalTexture.BindingDelegate );
						_unBindVS = true;
					}

					if ( ( op.vertexData.vertexDeclaration[ 1 ].Semantic == VertexElementSemantic.Normal ) &&
						 ( op.vertexData.vertexDeclaration[ 2 ].Semantic == VertexElementSemantic.Diffuse ) )
					{
						_rs.BindGpuProgram( _defaultVSPositionNormalColor.BindingDelegate );
						_unBindVS = true;
					}

				}
				else if ( ( op.vertexData.vertexDeclaration.ElementCount == 4 ) &&
						  ( op.vertexData.vertexDeclaration[ 0 ].Semantic == VertexElementSemantic.Position ) )
				{
					if ( ( op.vertexData.vertexDeclaration[ 1 ].Semantic == VertexElementSemantic.Normal ) &&
						 ( op.vertexData.vertexDeclaration[ 2 ].Semantic == VertexElementSemantic.TexCoords ) )
					{
						_rs.BindGpuProgram( _defaultVSPositionNormalTexture.BindingDelegate );
						_unBindVS = true;
					}
				}

				if ( ( _device.VertexShader == null ) && ( _unBindVS == false ) )
					throw new Exception( "Xna Render System requires a vertex shader, and can't find a suitable default." );

				// Set Common VP paramters
				_defaultVPParamters.SetNamedConstant( "World", _rs.WorldMatrix );
				_defaultVPParamters.SetNamedConstant( "View", _rs.ViewMatrix );
				_defaultVPParamters.SetNamedConstant( "Projection", _rs.ProjectionMatrix );

				_rs.BindGpuProgramParameters( GpuProgramType.Vertex, _defaultVPParamters );

			}

			if ( _device.PixelShader == null )
			{
				if ( bindTextureAndColorDefault )
				{
					_rs.BindGpuProgram( _defaultFPTextureColor.BindingDelegate );
				}
				else if ( bindTextureDefault )
				{
					_rs.BindGpuProgram( _defaultFPTexture.BindingDelegate );
				}
				else
				{
					_rs.BindGpuProgram( _defaultFPColor.BindingDelegate );
				}
				_unBindFS = true;

				if ( ( _device.PixelShader == null ) && ( _unBindFS == false ) )
					throw new Exception( "Xna Render System requires a pixel shader, and can't find a suitable default." );
			}

			return emulationNeeded;
		}

		public void EndEmulation()
		{
			if ( _unBindVS )
			{
				_rs.UnbindGpuProgram( GpuProgramType.Vertex );
				_unBindVS = false;
			}

			if ( _unBindFS )
			{
				_rs.UnbindGpuProgram( GpuProgramType.Fragment );
				_unBindFS = false;
			}
		}
	}
}
