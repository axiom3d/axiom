#region MIT/X11 License

//Copyright © 2003-2012 Axiom 3D Rendering Engine Project
//
//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:
//
//The above copyright notice and this permission notice shall be included in
//all copies or substantial portions of the Software.
//
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//THE SOFTWARE.

#endregion License

#region SVN Version Information

// <file>
//     <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;
using Axiom.Collections;
using Axiom.Configuration;
using Axiom.Core;
using Axiom.Graphics;
using Axiom.Math;
using Axiom.Media;
using Axiom.RenderSystems.DirectX9.HLSL;
using Axiom.Utilities;
using D3D9 = SharpDX.Direct3D9;
using DX = SharpDX;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.DirectX9
{
	public partial class D3D9RenderSystem
	{
		[OgreVersion( 1, 7, 2790 )]
		private RenderSystemCapabilities _updateRenderSystemCapabilities( D3D9RenderWindow renderWindow )
		{
			var rsc = realCapabilities ?? new RenderSystemCapabilities();

			rsc.SetCategoryRelevant( CapabilitiesCategory.D3D9, true );
			rsc.DriverVersion = driverVersion;
			rsc.DeviceName = this._activeD3DDriver.DriverDescription;
			rsc.RendersystemName = Name;

			// Supports fixed-function
			rsc.SetCapability( Graphics.Capabilities.FixedFunction );

			// Init caps to maximum.        
			rsc.TextureUnitCount = 1024;
			rsc.SetCapability( Graphics.Capabilities.AnisotropicFiltering );
			rsc.SetCapability( Graphics.Capabilities.HardwareMipMaps );
			rsc.SetCapability( Graphics.Capabilities.Dot3 );
			rsc.SetCapability( Graphics.Capabilities.CubeMapping );
			rsc.SetCapability( Graphics.Capabilities.ScissorTest );
			rsc.SetCapability( Graphics.Capabilities.TwoSidedStencil );
			rsc.SetCapability( Graphics.Capabilities.StencilWrap );
			rsc.SetCapability( Graphics.Capabilities.HardwareOcculusion );
			rsc.SetCapability( Graphics.Capabilities.UserClipPlanes );
			rsc.SetCapability( Graphics.Capabilities.VertexFormatUByte4 );
			rsc.SetCapability( Graphics.Capabilities.Texture3D );
			rsc.SetCapability( Graphics.Capabilities.NonPowerOf2Textures );
			rsc.NonPOW2TexturesLimited = false;
			rsc.MultiRenderTargetCount = Config.MaxMultipleRenderTargets;
			rsc.SetCapability( Graphics.Capabilities.MRTDifferentBitDepths );
			rsc.SetCapability( Graphics.Capabilities.PointSprites );
			rsc.SetCapability( Graphics.Capabilities.PointExtendedParameters );
			rsc.MaxPointSize = 10.0f;
			rsc.SetCapability( Graphics.Capabilities.MipmapLODBias );
			rsc.SetCapability( Graphics.Capabilities.PerStageConstant );
			rsc.SetCapability( Graphics.Capabilities.StencilBuffer );
			rsc.StencilBufferBitCount = 8;
			rsc.SetCapability( Graphics.Capabilities.AdvancedBlendOperations );
			//rsc.SetCapability( Graphics.Capabilities.RTTSerperateDepthBuffer );
			//rsc.SetCapability( Graphics.Capabilities.RTTMainDepthbufferAttachable );
			//rsc.SetCapability( Graphics.Capabilities.RTTDepthbufferResolutionLessEqual );
			//rsc.SetCapability( Graphics.Capabilities.VertexBufferInstanceData );
			//rsc.SetCapability( Graphics.Capabilities.CanGetCompiledShaderBuffer );

			foreach ( var device in this._deviceManager )
			{
				var d3D9Device = device.D3DDevice;

				// Check for hardware stencil support
				var pSurf = d3D9Device.DepthStencilSurface;
				if ( pSurf != null )
				{
					var surfDesc = pSurf.Description;
					//TODO
					//pSurf.Release();

					if ( surfDesc.Format != D3D9.Format.D15S1 && surfDesc.Format != D3D9.Format.D24S8 &&
					     surfDesc.Format != D3D9.Format.D24X4S4 && surfDesc.Format != D3D9.Format.D24SingleS8 )
					{
						rsc.UnsetCapability( Graphics.Capabilities.StencilBuffer );
					}
				}

				// Check for hardware occlusion support
				try
				{
					new D3D9.Query( d3D9Device, D3D9.QueryType.Occlusion );
				}
				catch ( DX.SharpDXException )
				{
					rsc.UnsetCapability( Graphics.Capabilities.HardwareOcculusion );
				}
			}

			// Update RS caps using the minimum value found in adapter list.
			foreach ( var pCurDriver in this._driverList )
			{
				var rkCurCaps = pCurDriver.D3D9DeviceCaps;

				if ( rkCurCaps.MaxSimultaneousTextures < rsc.TextureUnitCount )
				{
					rsc.TextureUnitCount = rkCurCaps.MaxSimultaneousTextures;
				}

				// Check for Anisotropy.
				if ( rkCurCaps.MaxAnisotropy <= 1 )
				{
					rsc.UnsetCapability( Graphics.Capabilities.AnisotropicFiltering );
				}

				// Check automatic mipmap generation.
				if ( ( rkCurCaps.Caps2 & D3D9.Caps2.CanAutoGenerateMipMap ) == 0 )
				{
					rsc.UnsetCapability( Graphics.Capabilities.HardwareMipMaps );
				}

				// Check Dot product 3.
				if ( ( rkCurCaps.TextureOperationCaps & D3D9.TextureOperationCaps.DotProduct3 ) == 0 )
				{
					rsc.UnsetCapability( Graphics.Capabilities.Dot3 );
				}

				// Check cube map support.
				if ( ( rkCurCaps.TextureCaps & D3D9.TextureCaps.CubeMap ) == 0 )
				{
					rsc.UnsetCapability( Graphics.Capabilities.CubeMapping );
				}

				// Scissor test
				if ( ( rkCurCaps.RasterCaps & D3D9.RasterCaps.ScissorTest ) == 0 )
				{
					rsc.UnsetCapability( Graphics.Capabilities.ScissorTest );
				}

				// Two-sided stencil
				if ( ( rkCurCaps.StencilCaps & D3D9.StencilCaps.TwoSided ) == 0 )
				{
					rsc.UnsetCapability( Graphics.Capabilities.TwoSidedStencil );
				}

				// stencil wrap
				if ( ( rkCurCaps.StencilCaps & D3D9.StencilCaps.Increment ) == 0 ||
				     ( rkCurCaps.StencilCaps & D3D9.StencilCaps.Decrement ) == 0 )
				{
					rsc.UnsetCapability( Graphics.Capabilities.StencilWrap );
				}

				// User clip planes
				if ( rkCurCaps.MaxUserClipPlanes == 0 )
				{
					rsc.UnsetCapability( Graphics.Capabilities.UserClipPlanes );
				}

				// UBYTE4 type?
				if ( ( rkCurCaps.DeclarationTypes & D3D9.DeclarationTypeCaps.UByte4 ) == 0 )
				{
					rsc.UnsetCapability( Graphics.Capabilities.VertexFormatUByte4 );
				}

				// 3D textures?
				if ( ( rkCurCaps.TextureCaps & D3D9.TextureCaps.VolumeMap ) == 0 )
				{
					rsc.UnsetCapability( Graphics.Capabilities.Texture3D );
				}

				if ( ( rkCurCaps.TextureCaps & D3D9.TextureCaps.Pow2 ) != 0 )
				{
					// Conditional support for non POW2
					if ( ( rkCurCaps.TextureCaps & D3D9.TextureCaps.NonPow2Conditional ) != 0 )
					{
						rsc.NonPOW2TexturesLimited = true;
					}

						// Only power of 2 supported.
					else
					{
						rsc.UnsetCapability( Graphics.Capabilities.NonPowerOf2Textures );
					}
				}

				// Number of render targets
				if ( rkCurCaps.SimultaneousRTCount < rsc.MultiRenderTargetCount )
				{
					rsc.MultiRenderTargetCount = Utility.Min( rkCurCaps.SimultaneousRTCount, Config.MaxMultipleRenderTargets );
				}

				if ( ( rkCurCaps.PrimitiveMiscCaps & D3D9.PrimitiveMiscCaps.MrtIndependentBitDepths ) == 0 )
				{
					rsc.UnsetCapability( Graphics.Capabilities.MRTDifferentBitDepths );
				}

				// Point sprites 
				if ( rkCurCaps.MaxPointSize <= 1.0f )
				{
					rsc.UnsetCapability( Graphics.Capabilities.PointSprites );
					// sprites and extended parameters go together in D3D
					rsc.UnsetCapability( Graphics.Capabilities.PointExtendedParameters );
				}

				// Take the minimum point size.
				if ( rkCurCaps.MaxPointSize < rsc.MaxPointSize )
				{
					rsc.MaxPointSize = rkCurCaps.MaxPointSize;
				}

				// Mipmap LOD biasing?
				if ( ( rkCurCaps.RasterCaps & D3D9.RasterCaps.MipMapLodBias ) == 0 )
				{
					rsc.UnsetCapability( Graphics.Capabilities.MipmapLODBias );
				}


				// Do we support per-stage src_manual constants?
				// HACK - ATI drivers seem to be buggy and don't support per-stage constants properly?
				// TODO: move this to RSC
				if ( ( rkCurCaps.PrimitiveMiscCaps & D3D9.PrimitiveMiscCaps.PerStageConstant ) == 0 )
				{
					rsc.UnsetCapability( Graphics.Capabilities.PerStageConstant );
				}

				// Advanced blend operations? min max subtract rev 
				if ( ( rkCurCaps.PrimitiveMiscCaps & D3D9.PrimitiveMiscCaps.BlendOperation ) == 0 )
				{
					rsc.UnsetCapability( Graphics.Capabilities.AdvancedBlendOperations );
				}
			}

			// Blending between stages supported
			rsc.SetCapability( Graphics.Capabilities.Blending );

			// We always support compression, D3DX will decompress if device does not support
			rsc.SetCapability( Graphics.Capabilities.TextureCompression );
			rsc.SetCapability( Graphics.Capabilities.TextureCompressionDXT );

			// We always support VBOs
			rsc.SetCapability( Graphics.Capabilities.VertexBuffer );

			_convertVertexShaderCaps( rsc );
			_convertPixelShaderCaps( rsc );

			// Adapter details
			var adapterId = this._activeD3DDriver.AdapterIdentifier;

			// determine vendor
			// Full list of vendors here: http://www.pcidatabase.com/vendors.php?sort=id
			switch ( adapterId.VendorId )
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

			// Infinite projection?
			// We have no capability for this, so we have to base this on our
			// experience and reports from users
			// Non-vertex program capable hardware does not appear to support it
			if ( rsc.HasCapability( Graphics.Capabilities.VertexPrograms ) )
			{
				// GeForce4 Ti (and presumably GeForce3) does not
				// render infinite projection properly, even though it does in GL
				// So exclude all cards prior to the FX range from doing infinite
				if ( rsc.Vendor != GPUVendor.Nvidia || // not nVidia
				     !( ( adapterId.DeviceId >= 0x200 && adapterId.DeviceId <= 0x20F ) || //gf3
				        ( adapterId.DeviceId >= 0x250 && adapterId.DeviceId <= 0x25F ) || //gf4ti
				        ( adapterId.DeviceId >= 0x280 && adapterId.DeviceId <= 0x28F ) || //gf4ti
				        ( adapterId.DeviceId >= 0x170 && adapterId.DeviceId <= 0x18F ) || //gf4 go
				        ( adapterId.DeviceId >= 0x280 && adapterId.DeviceId <= 0x28F ) ) ) //gf4ti go
				{
					rsc.SetCapability( Graphics.Capabilities.InfiniteFarPlane );
				}
			}

			// We always support rendertextures bigger than the frame buffer
			rsc.SetCapability( Graphics.Capabilities.HardwareRenderToTexture );

			// Determine if any floating point texture format is supported
			var floatFormats = new[]
			                   {
			                   	D3D9.Format.R16F, D3D9.Format.G16R16F, D3D9.Format.A16B16G16R16F, D3D9.Format.R32F,
			                   	D3D9.Format.G32R32F, D3D9.Format.A32B32G32R32F
			                   };

			var bbSurf = (D3D9.Surface[])renderWindow[ "DDBACKBUFFER" ];
			var bbSurfDesc = bbSurf[ 0 ].Description;

			for ( var i = 0; i < 6; ++i )
			{
				if (
					!this._pD3D.CheckDeviceFormat( this._activeD3DDriver.AdapterNumber, D3D9.DeviceType.Hardware, bbSurfDesc.Format, 0,
					                               D3D9.ResourceType.Texture, floatFormats[ i ] ) )
				{
					continue;
				}
				rsc.SetCapability( Graphics.Capabilities.TextureFloat );
				break;
			}

			// TODO: make convertVertex/Fragment fill in rsc
			// TODO: update the below line to use rsc
			// Vertex textures
			if ( rsc.IsShaderProfileSupported( "vs_3_0" ) )
			{
				// Run through all the texture formats looking for any which support
				// vertex texture fetching. Must have at least one!
				// All ATI Radeon up to X1n00 say they support vs_3_0, 
				// but they support no texture formats for vertex texture fetch (cheaters!)
				if ( _checkVertexTextureFormats( renderWindow ) )
				{
					rsc.SetCapability( Graphics.Capabilities.VertexTextureFetch );
					// always 4 vertex texture units in vs_3_0, and never shared
					rsc.VertexTextureUnitCount = 4;
					rsc.VertexTextureUnitsShared = false;
				}
			}

			// Check alpha to coverage support
			// this varies per vendor! But at least SM3 is required
			if ( rsc.IsShaderProfileSupported( "ps_3_0" ) )
			{
				// NVIDIA needs a separate check
				switch ( rsc.Vendor )
				{
					case GPUVendor.Nvidia:
						if ( this._pD3D.CheckDeviceFormat( 0, D3D9.DeviceType.Hardware, D3D9.Format.X8R8G8B8, 0, D3D9.ResourceType.Surface,
						                                   (D3D9.Format)( 'A' | ( 'T' ) << 8 | ( 'O' ) << 16 | ( 'C' ) << 24 ) ) )
						{
							rsc.SetCapability( Graphics.Capabilities.AlphaToCoverage );
						}
						break;

					case GPUVendor.Ati:
						// There is no check on ATI, we have to assume SM3 == support
						rsc.SetCapability( Graphics.Capabilities.AlphaToCoverage );
						break;
				}

				// no other cards have Dx9 hacks for alpha to coverage, as far as I know
			}

			if ( realCapabilities == null )
			{
				realCapabilities = rsc;
				realCapabilities.AddShaderProfile( "hlsl" );

				// if we are using custom capabilities, then 
				// mCurrentCapabilities has already been loaded
				if ( !useCustomCapabilities )
				{
					currentCapabilities = realCapabilities;
				}

				InitializeFromRenderSystemCapabilities( currentCapabilities, renderWindow );
			}

			return rsc;
		}

		[OgreVersion( 1, 7, 2790 )]
		private bool _checkVertexTextureFormats( D3D9RenderWindow renderWindow )
		{
			var anySupported = false;

			var bbSurf = (D3D9.Surface[])renderWindow[ "DDBACKBUFFER" ];
			var bbSurfDesc = bbSurf[ 0 ].Description;

			for ( var pf = PixelFormat.L8; pf < PixelFormat.Count; ++pf )
			{
				var fmt = D3D9Helper.ConvertEnum( D3D9Helper.GetClosestSupported( pf ) );
				if (
					!this._pD3D.CheckDeviceFormat( this._activeD3DDriver.AdapterNumber, D3D9.DeviceType.Hardware, bbSurfDesc.Format,
					                               D3D9.Usage.QueryVertexTexture, D3D9.ResourceType.Texture, fmt ) )
				{
					continue;
				}

				// cool, at least one supported
				anySupported = true;
				LogManager.Instance.Write( "D3D9: Vertex texture format supported - {0}", PixelUtil.GetFormatName( pf ) );
			}

			return anySupported;
		}

		[OgreVersion( 1, 7, 2790 )]
		private void _convertPixelShaderCaps( RenderSystemCapabilities rsc )
		{
			var major = 0xFF;
			var minor = 0xFF;
			var minPsCaps = new D3D9.Capabilities();

			// Find the device with the lowest vertex shader caps.
			foreach ( var pCurDriver in this._driverList )
			{
				var currCaps = pCurDriver.D3D9DeviceCaps;
				var currMajor = currCaps.PixelShaderVersion.Major;
				var currMinor = currCaps.PixelShaderVersion.Minor;

				if ( currMajor < major )
				{
					major = currMajor;
					minor = currMinor;
					minPsCaps = currCaps;
				}
				else if ( currMajor == major && currMinor < minor )
				{
					minor = currMinor;
					minPsCaps = currCaps;
				}
			}

			var ps2a = false;
			var ps2b = false;
			var ps2x = false;

			// Special case detection for ps_2_x/a/b support
			if ( major >= 2 )
			{
				if ( ( minPsCaps.PS20Caps.Caps & D3D9.PixelShaderCaps.NoTextureInstructionLimit ) != 0 &&
				     ( minPsCaps.PS20Caps.TempCount >= 32 ) )
				{
					ps2b = true;
				}

				if ( ( minPsCaps.PS20Caps.Caps & D3D9.PixelShaderCaps.NoTextureInstructionLimit ) != 0 &&
				     ( minPsCaps.PS20Caps.Caps & D3D9.PixelShaderCaps.NoDependentReadLimit ) != 0 &&
				     ( minPsCaps.PS20Caps.Caps & D3D9.PixelShaderCaps.ArbitrarySwizzle ) != 0 &&
				     ( minPsCaps.PS20Caps.Caps & D3D9.PixelShaderCaps.GradientInstructions ) != 0 &&
				     ( minPsCaps.PS20Caps.Caps & D3D9.PixelShaderCaps.Predication ) != 0 && ( minPsCaps.PS20Caps.TempCount >= 22 ) )
				{
					ps2a = true;
				}

				// Does this enough?
				if ( ps2a || ps2b )
				{
					ps2x = true;
				}
			}

			switch ( major )
			{
				case 1:
					// no boolean params allowed
					rsc.FragmentProgramConstantBoolCount = 0;
					// no integer params allowed
					rsc.FragmentProgramConstantIntCount = 0;
					// float params, always 4D
					// NB in ps_1_x these are actually stored as fixed point values,
					// but they are entered as floats
					rsc.FragmentProgramConstantFloatCount = 8;
					break;

				case 2:
					// 16 boolean params allowed
					rsc.FragmentProgramConstantBoolCount = 16;
					// 16 integer params allowed, 4D
					rsc.FragmentProgramConstantIntCount = 16;
					// float params, always 4D
					rsc.FragmentProgramConstantFloatCount = 32;
					break;

				case 3:
					// 16 boolean params allowed
					rsc.FragmentProgramConstantBoolCount = 16;
					// 16 integer params allowed, 4D
					rsc.FragmentProgramConstantIntCount = 16;
					// float params, always 4D
					rsc.FragmentProgramConstantFloatCount = 224;
					break;
			}

			// populate syntax codes in program manager (no breaks in this one so it falls through)
			switch ( major )
			{
				case 3:
					if ( minor > 0 )
					{
						rsc.AddShaderProfile( "ps_3_x" );
					}

					rsc.AddShaderProfile( "ps_3_0" );
					goto case 2;

				case 2:
					if ( ps2x )
					{
						rsc.AddShaderProfile( "ps_2_x" );
					}
					if ( ps2a )
					{
						rsc.AddShaderProfile( "ps_2_a" );
					}
					if ( ps2b )
					{
						rsc.AddShaderProfile( "ps_2_b" );
					}

					rsc.AddShaderProfile( "ps_2_0" );
					goto case 1;

				case 1:
					if ( major > 1 || minor >= 4 )
					{
						rsc.AddShaderProfile( "ps_1_4" );
					}
					if ( major > 1 || minor >= 3 )
					{
						rsc.AddShaderProfile( "ps_1_3" );
					}
					if ( major > 1 || minor >= 2 )
					{
						rsc.AddShaderProfile( "ps_1_2" );
					}

					rsc.AddShaderProfile( "ps_1_1" );
					rsc.SetCapability( Graphics.Capabilities.FragmentPrograms );
					break;
			}
		}

		[OgreVersion( 1, 7, 2790 )]
		private void _convertVertexShaderCaps( RenderSystemCapabilities rsc )
		{
			var major = 0xFF;
			var minor = 0xFF;
			var minVsCaps = new D3D9.Capabilities();

			// Find the device with the lowest vertex shader caps.
			foreach ( var pCurDriver in this._driverList )
			{
				var rkCurCaps = pCurDriver.D3D9DeviceCaps;
				var currMajor = rkCurCaps.VertexShaderVersion.Major;
				var currMinor = rkCurCaps.VertexShaderVersion.Minor;

				if ( currMajor < major )
				{
					major = currMajor;
					minor = currMinor;
					minVsCaps = rkCurCaps;
				}
				else if ( currMajor == major && currMinor < minor )
				{
					minor = currMinor;
					minVsCaps = rkCurCaps;
				}
			}

			var vs2x = false;
			var vs2a = false;

			// Special case detection for vs_2_x/a support
			if ( major >= 2 )
			{
				if ( ( minVsCaps.VS20Caps.Caps & D3D9.VertexShaderCaps.Predication ) != 0 &&
				     ( minVsCaps.VS20Caps.DynamicFlowControlDepth > 0 ) && ( minVsCaps.VS20Caps.TempCount >= 12 ) )
				{
					vs2x = true;
				}

				if ( ( minVsCaps.VS20Caps.Caps & D3D9.VertexShaderCaps.Predication ) != 0 &&
				     ( minVsCaps.VS20Caps.DynamicFlowControlDepth > 0 ) && ( minVsCaps.VS20Caps.TempCount >= 13 ) )
				{
					vs2a = true;
				}
			}

			// Populate max param count
			switch ( major )
			{
				case 1:
					// No boolean params allowed
					rsc.VertexProgramConstantBoolCount = 0;
					// No integer params allowed
					rsc.VertexProgramConstantIntCount = 0;
					// float params, always 4D
					rsc.VertexProgramConstantFloatCount = minVsCaps.MaxVertexShaderConst;
					break;

				case 2:
					// 16 boolean params allowed
					rsc.VertexProgramConstantBoolCount = 16;
					// 16 integer params allowed, 4D
					rsc.VertexProgramConstantIntCount = 16;
					// float params, always 4D
					rsc.VertexProgramConstantFloatCount = minVsCaps.MaxVertexShaderConst;
					break;

				case 3:
					// 16 boolean params allowed
					rsc.VertexProgramConstantBoolCount = 16;
					// 16 integer params allowed, 4D
					rsc.VertexProgramConstantIntCount = 16;
					// float params, always 4D
					rsc.VertexProgramConstantFloatCount = minVsCaps.MaxVertexShaderConst;
					break;
			}

			// populate syntax codes in program manager (no breaks in this one so it falls through)
			switch ( major )
			{
				case 3:
					rsc.AddShaderProfile( "vs_3_0" );
					goto case 2;

				case 2:
					if ( vs2x )
					{
						rsc.AddShaderProfile( "vs_2_x" );
					}
					if ( vs2a )
					{
						rsc.AddShaderProfile( "vs_2_a" );
					}

					rsc.AddShaderProfile( "vs_2_0" );
					goto case 1;

				case 1:
					rsc.AddShaderProfile( "vs_1_1" );
					rsc.SetCapability( Graphics.Capabilities.VertexPrograms );
					break;
			}
		}

		[OgreVersion( 1, 7, 2790 )]
		public override RenderWindow Initialize( bool autoCreateWindow, string windowTitle )
		{
			LogManager.Instance.Write( "[D3D9] : Subsystem Initializing" );

			// Axiom specific
			WindowEventMonitor.Instance.MessagePump = Win32MessageHandling.MessagePump;

			// Init using current settings
			this._activeD3DDriver = this._driverList[ ConfigOptions[ "Rendering Device" ].Value ];
			if ( this._activeD3DDriver == null )
			{
				throw new ArgumentException( "Problems finding requested Direct3D driver!" );
			}

			driverVersion.Major = this._activeD3DDriver.AdapterIdentifier.DriverVersion.Major;
			driverVersion.Minor = this._activeD3DDriver.AdapterIdentifier.DriverVersion.Minor;
			driverVersion.Release = this._activeD3DDriver.AdapterIdentifier.DriverVersion.MajorRevision;
			driverVersion.Build = this._activeD3DDriver.AdapterIdentifier.DriverVersion.MinorRevision;

			// Create the device manager.
			this._deviceManager = new D3D9DeviceManager();

			// Create the texture manager for use by others        
			textureManager = new D3D9TextureManager();

			// Also create hardware buffer manager
			this._hardwareBufferManager = new D3D9HardwareBufferManager();

			// Create the GPU program manager    
			this._gpuProgramManager = new D3D9GpuProgramManager();

			// Create & register HLSL factory
			this._hlslProgramFactory = new D3D9HLSLProgramFactory();

			RenderWindow autoWindow = null;

			if ( autoCreateWindow )
			{
				var fullScreen = ( ConfigOptions[ "Full Screen" ].Value == "Yes" );

				D3D9VideoMode videoMode = null;

				var vm = ConfigOptions[ "Video Mode" ].Value;
				var width = int.Parse( vm.Substring( 0, vm.IndexOf( "x" ) ) );
				var height = int.Parse( vm.Substring( vm.IndexOf( "x" ) + 1, vm.IndexOf( "@" ) - ( vm.IndexOf( "x" ) + 1 ) ) );
				var bpp = int.Parse( vm.Substring( vm.IndexOf( "@" ) + 1, vm.IndexOf( "-" ) - ( vm.IndexOf( "@" ) + 1 ) ) );

				foreach ( var currVideoMode in this._activeD3DDriver.VideoModeList )
				{
					var temp = currVideoMode.Description;
					var colorDepth =
						int.Parse( temp.Substring( temp.IndexOf( "@" ) + 1, temp.IndexOf( "-" ) - ( temp.IndexOf( "@" ) + 1 ) ) );

					// In full screen we only want to allow supported resolutions, so temp and opt->second.currentValue need to 
					// match exactly, but in windowed mode we can allow for arbitrary window sized, so we only need
					// to match the colour values
					if ( fullScreen && ( temp == vm ) || !fullScreen && ( colorDepth == bpp ) )
					{
						videoMode = currVideoMode;
						break;
					}
				}

				if ( videoMode == null )
				{
					throw new AxiomException( "Can't find requested video mode." );
				}

				// sRGB window option
				ConfigOption opt;
				if ( !ConfigOptions.TryGetValue( "sRGB Gamma Conversion", out opt ) )
				{
					throw new AxiomException( "Can't find sRGB option!" );
				}

				var hwGamma = opt.Value == "Yes";

				var miscParams = new NamedParameterList();
				miscParams.Add( "title", windowTitle ); // Axiom only?
				miscParams.Add( "colorDepth", bpp );
				miscParams.Add( "FSAA", this._fsaaSamples );
				miscParams.Add( "FSAAHint", this._fsaaHint );
				miscParams.Add( "vsync", vSync );
				miscParams.Add( "vsyncInterval", vSyncInterval );
				miscParams.Add( "useNVPerfHUD", this._useNVPerfHUD );
				miscParams.Add( "gamma", hwGamma );
				miscParams.Add( "monitorIndex", this._activeD3DDriver.AdapterNumber );

				// create the render window
				autoWindow = CreateRenderWindow( windowTitle, width, height, fullScreen, miscParams );

				// If we have 16bit depth buffer enable w-buffering.
				Contract.Requires( autoWindow != null );
				wBuffer = ( autoWindow.ColorDepth == 16 );
			}

			LogManager.Instance.Write( "***************************************" );
			LogManager.Instance.Write( "*** D3D9 : Subsystem Initialized OK ***" );
			LogManager.Instance.Write( "***************************************" );

			// call superclass method
			base.Initialize( autoCreateWindow, windowTitle );

			// Configure SharpDX
			DX.Configuration.ThrowOnShaderCompileError = true;

#if DEBUG
			DX.Configuration.EnableObjectTracking = true;
#endif
			return autoWindow;
		}

		[OgreVersion( 1, 7, 2790 )]
		public override void InitializeFromRenderSystemCapabilities( RenderSystemCapabilities caps, RenderTarget primary )
		{
			if ( caps.RendersystemName != Name )
			{
				throw new AxiomException(
					"Trying to initialize D3D9RenderSystem from RenderSystemCapabilities that do not support Direct3D9" );
			}

			if ( caps.IsShaderProfileSupported( "hlsl" ) )
			{
				HighLevelGpuProgramManager.Instance.AddFactory( this._hlslProgramFactory );
			}

			var defaultLog = LogManager.Instance.DefaultLog;

			if ( defaultLog != null )
			{
				caps.Log( defaultLog );
			}
		}
	};
}