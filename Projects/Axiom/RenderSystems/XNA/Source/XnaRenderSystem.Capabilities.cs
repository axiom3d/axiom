#region LGPL License
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
#endregion LGPL License

#region SVN Version Information
// <file>
//     <id value="$Id$"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using Axiom.Core;
using Axiom.Graphics;
using XFG = Microsoft.Xna.Framework.Graphics;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.Xna
{
	public partial class XnaRenderSystem
	{
		/// <summary>
		///	Helper method to go through and interrogate hardware capabilities.
		/// </summary>
		private RenderSystemCapabilities _checkHardwareCapabilities( XFG.GraphicsProfile profile )
		{
			var rsc = realCapabilities ?? new RenderSystemCapabilities();

			_setCapabilitiesForAllProfiles( ref rsc );

			if ( profile == XFG.GraphicsProfile.HiDef )
				_setCapabilitiesForHiDefProfile( ref rsc );

			else if ( profile == XFG.GraphicsProfile.Reach )
				_setCapabilitiesForReachProfile( ref rsc );

			else
				throw new AxiomException( "Not a valid profile!" );

			return rsc;
		}

		private void _setCapabilitiesForAllProfiles( ref RenderSystemCapabilities rsc )
		{
			//TODO Should we add an XNA capabilities category?
			//rsc.SetCategoryRelevant( CapabilitiesCategory.D3D9, true );
			rsc.DriverVersion = driverVersion;
			rsc.DeviceName = _device.Adapter.Description;
			rsc.RendersystemName = Name;

			// determine vendor
			// Full list of vendors here: http://www.pcidatabase.com/vendors.php?sort=id
			switch ( _device.Adapter.VendorId )
			{
				case 0x10DE:
					rsc.Vendor = GPUVendor.Nvidia;
					break;

				case 0x1002:
					rsc.Vendor = GPUVendor.Ati;
					break;

				case 0x163C:
				case 0x8086:
					rsc.Vendor = GPUVendor.Intel;
					break;

				case 0x5333:
					rsc.Vendor = GPUVendor.S3;
					break;

				case 0x3D3D:
					rsc.Vendor = GPUVendor._3DLabs;
					break;

				case 0x102B:
					rsc.Vendor = GPUVendor.Matrox;
					break;

				case 0x1039:
					rsc.Vendor = GPUVendor.Sis;
					break;

				default:
					rsc.Vendor = GPUVendor.Unknown;
					break;
			}

			// Texture Compression
			// We always support compression, Xna will decompress if device does not support
			rsc.SetCapability( Graphics.Capabilities.TextureCompression );
			rsc.SetCapability( Graphics.Capabilities.TextureCompressionDXT );

			// Xna uses vertex buffers for everything
			rsc.SetCapability( Graphics.Capabilities.VertexBuffer );
		}

		private void _setCapabilitiesForHiDefProfile( ref RenderSystemCapabilities rsc )
		{
			// Fill in the HiDef profile requirements.
			rsc.SetCapability( Graphics.Capabilities.HardwareOcculusion );

			//VertexShaderVersion = 0x300;
			rsc.SetCapability( Graphics.Capabilities.VertexPrograms );
			rsc.MaxVertexProgramVersion = "vs_3_0";
			rsc.VertexProgramConstantIntCount = 16 * 4;
			rsc.VertexProgramConstantFloatCount = 256;
			rsc.AddShaderProfile( "vs_1_1" );
			rsc.AddShaderProfile( "vs_2_0" );
			rsc.AddShaderProfile( "vs_2_x" );
			rsc.AddShaderProfile( "vs_3_0" );

			//PixelShaderVersion = 0x300;
			rsc.SetCapability( Graphics.Capabilities.FragmentPrograms );
			rsc.MaxFragmentProgramVersion = "ps_3_0";
			rsc.FragmentProgramConstantIntCount = 16;
			rsc.FragmentProgramConstantFloatCount = 224;
			rsc.AddShaderProfile( "ps_1_1" );
			rsc.AddShaderProfile( "ps_1_2" );
			rsc.AddShaderProfile( "ps_1_3" );
			rsc.AddShaderProfile( "ps_1_4" );
			rsc.AddShaderProfile( "ps_2_0" );
			rsc.AddShaderProfile( "ps_3_0" );

			//SeparateAlphaBlend = true;
			rsc.SetCapability( Graphics.Capabilities.AdvancedBlendOperations );
			//DestBlendSrcAlphaSat = true;

			//MaxPrimitiveCount = 1048575;
			//IndexElementSize32 = true;
			//MaxVertexStreams = 16;
			//MaxStreamStride = 255;

			//MaxTextureSize = 4096;
			//MaxCubeSize = 4096;
			//MaxVolumeExtent = 256;
			//MaxTextureAspectRatio = 2048;
			//MaxVertexSamplers = 4;
			//MaxRenderTargets = 4;
			rsc.TextureUnitCount = 16;
			rsc.MultiRenderTargetCount = 4;

			//NonPow2Unconditional = true;
			//NonPow2Cube = true;
			//NonPow2Volume = true;

			//ValidTextureFormats       = MakeList(STANDARD_TEXTURE_FORMATS, COMPRESSED_TEXTURE_FORMATS, SIGNED_TEXTURE_FORMATS, HIDEF_TEXTURE_FORMATS, FLOAT_TEXTURE_FORMATS);
			//ValidCubeFormats          = MakeList(STANDARD_TEXTURE_FORMATS, COMPRESSED_TEXTURE_FORMATS, HIDEF_TEXTURE_FORMATS, FLOAT_TEXTURE_FORMATS);
			//ValidVolumeFormats        = MakeList(STANDARD_TEXTURE_FORMATS, HIDEF_TEXTURE_FORMATS, FLOAT_TEXTURE_FORMATS);
			//ValidVertexTextureFormats = MakeList(FLOAT_TEXTURE_FORMATS);
			//InvalidFilterFormats      = MakeList(FLOAT_TEXTURE_FORMATS);
			//InvalidBlendFormats       = MakeList(STANDARD_FLOAT_TEXTURE_FORMATS);
			//ValidVertexFormats        = MakeList(STANDARD_VERTEX_FORMATS, HIDEF_VERTEX_FORMATS);
		}

		private void _setCapabilitiesForReachProfile( ref RenderSystemCapabilities rsc )
		{
			// Fill in the Reach profile requirements.
			// Texture Compression
			// We always support compression, Xna will decompress if device does not support
			rsc.SetCapability( Graphics.Capabilities.TextureCompression );
			rsc.SetCapability( Graphics.Capabilities.TextureCompressionDXT );

			// Xna uses vertex buffers for everything
			rsc.SetCapability( Graphics.Capabilities.VertexBuffer );

			//VertexShaderVersion = 0x200;
			rsc.SetCapability( Graphics.Capabilities.VertexPrograms );
			rsc.MaxVertexProgramVersion = "vs_2_0";
			rsc.VertexProgramConstantIntCount = 16 * 4;
			rsc.VertexProgramConstantFloatCount = 256;
			rsc.AddShaderProfile( "vs_1_1" );
			rsc.AddShaderProfile( "vs_2_0" );

			//PixelShaderVersion = 0x200;
			rsc.SetCapability( Graphics.Capabilities.FragmentPrograms );
			rsc.MaxFragmentProgramVersion = "ps_2_0";
			rsc.FragmentProgramConstantIntCount = 0;
			rsc.FragmentProgramConstantFloatCount = 32;
			rsc.AddShaderProfile( "ps_1_1" );
			rsc.AddShaderProfile( "ps_1_2" );
			rsc.AddShaderProfile( "ps_1_3" );
			rsc.AddShaderProfile( "ps_1_4" );
			rsc.AddShaderProfile( "ps_2_0" );

			//SeparateAlphaBlend = false;
			//DestBlendSrcAlphaSat = false;

			//MaxPrimitiveCount = 65535;
			//IndexElementSize32 = false;
			//MaxVertexStreams = 16;
			//MaxStreamStride = 255;

			//MaxTextureSize = 2048;
			//MaxCubeSize = 512;
			//MaxVolumeExtent = 0;
			//MaxTextureAspectRatio = 2048;
			//MaxVertexSamplers = 0;
			//MaxRenderTargets = 1;
			rsc.MultiRenderTargetCount = 1;

			//NonPow2Unconditional = false;
			//NonPow2Cube = false;
			//NonPow2Volume = false;

			//ValidTextureFormats       = MakeList(STANDARD_TEXTURE_FORMATS, COMPRESSED_TEXTURE_FORMATS, SIGNED_TEXTURE_FORMATS);
			//ValidCubeFormats          = MakeList(STANDARD_TEXTURE_FORMATS, COMPRESSED_TEXTURE_FORMATS);
			//ValidVolumeFormats        = MakeList<SurfaceFormat>();
			//ValidVertexTextureFormats = MakeList<SurfaceFormat>();
			//InvalidFilterFormats      = MakeList<SurfaceFormat>();
			//InvalidBlendFormats       = MakeList<SurfaceFormat>();
			//ValidVertexFormats        = MakeList(STANDARD_VERTEX_FORMATS);
		}
	}
}
