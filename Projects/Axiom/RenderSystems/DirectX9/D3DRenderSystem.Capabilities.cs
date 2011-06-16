using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Axiom.Configuration;
using Axiom.Core;
using Axiom.Graphics;
using Axiom.Math;
using Axiom.Media;
using SlimDX.Direct3D9;

namespace Axiom.RenderSystems.DirectX9
{
    public partial class D3DRenderSystem
    {

        [OgreVersion(1, 7)]
        private RenderSystemCapabilities UpdateRenderSystemCapabilities(D3DRenderWindow renderWindow)
        {
            var rsc = realCapabilities ?? new RenderSystemCapabilities();

            rsc.SetCategoryRelevant(CapabilitiesCategory.D3D9, true);
		    rsc.DriverVersion = driverVersion;
		    rsc.DeviceName = _activeDriver.DriverDescription;
            rsc.RendersystemName = Name;

            // Init caps to maximum.		
            rsc.TextureUnitCount = 1024;
            rsc.SetCapability(Graphics.Capabilities.AnisotropicFiltering);
            rsc.SetCapability(Graphics.Capabilities.HardwareMipMaps);
            rsc.SetCapability(Graphics.Capabilities.Dot3);
            rsc.SetCapability(Graphics.Capabilities.CubeMapping);
            rsc.SetCapability(Graphics.Capabilities.ScissorTest);
            rsc.SetCapability(Graphics.Capabilities.TwoSidedStencil);
            rsc.SetCapability(Graphics.Capabilities.StencilWrap);
            rsc.SetCapability(Graphics.Capabilities.HardwareOcculusion);
            rsc.SetCapability(Graphics.Capabilities.UserClipPlanes);
            rsc.SetCapability(Graphics.Capabilities.VertexFormatUByte4);
            rsc.SetCapability(Graphics.Capabilities.Texture3D);
            rsc.SetCapability(Graphics.Capabilities.NonPowerOf2Textures);
            rsc.NonPOW2TexturesLimited = false;
            rsc.MultiRenderTargetCount = Config.MaxMultipleRenderTargets;
            rsc.SetCapability(Graphics.Capabilities.MRTDifferentBitDepths);
            rsc.SetCapability(Graphics.Capabilities.PointSprites);
            rsc.SetCapability(Graphics.Capabilities.PointExtendedParameters);
            rsc.MaxPointSize = 2.19902e+012f;
            rsc.SetCapability(Graphics.Capabilities.MipmapLODBias);
            rsc.SetCapability(Graphics.Capabilities.PerStageConstant);
            rsc.SetCapability(Graphics.Capabilities.StencilBuffer);
            rsc.StencilBufferBitCount = 8;
            rsc.SetCapability(Graphics.Capabilities.AdvancedBlendOperations);
            rsc.SetCapability(Graphics.Capabilities.RTTSerperateDepthBuffer);
            rsc.SetCapability(Graphics.Capabilities.RTTMainDepthbufferAttachable);
            rsc.SetCapability(Graphics.Capabilities.RTTDepthbufferResolutionLessEqual);
            rsc.SetCapability(Graphics.Capabilities.VertexBufferInstanceData);
            rsc.SetCapability(Graphics.Capabilities.CanGetCompiledShaderBuffer);

            foreach (var device in _deviceManager)
            {
                var d3d9Device = device.D3DDevice;

                // Check for hardware stencil support
                var pSurf = d3d9Device.DepthStencilSurface;
                if (pSurf != null)
                {
                    var surfDesc = pSurf.Description;

                    if (surfDesc.Format != Format.D15S1 &&
                        surfDesc.Format != Format.D24S8 &&
                        surfDesc.Format != Format.D24X4S4 &&
                        surfDesc.Format != Format.D24SingleS8)
                        rsc.UnsetCapability( Graphics.Capabilities.StencilBuffer );
                }

                // Check for hardware occlusion support
                //HRESULT hr = d3d9Device->CreateQuery(D3DQUERYTYPE_OCCLUSION, NULL);
                try
                {
                    new Query(d3d9Device, QueryType.Occlusion);
                }
                catch (Direct3D9Exception)
                {
                    rsc.UnsetCapability( Graphics.Capabilities.HardwareOcculusion );
                }
            }

            // Update RS caps using the minimum value found in adapter list.
            foreach (var pCurDriver in _driverList)
		    {
			    var rkCurCaps    = pCurDriver.D3D9DeviceCaps;

			    if (rkCurCaps.MaxSimultaneousTextures < rsc.TextureUnitCount)
			    {
				    rsc.TextureUnitCount = rkCurCaps.MaxSimultaneousTextures;
			    }

			    // Check for Anisotropy.
			    if (rkCurCaps.MaxAnisotropy <= 1)
			        rsc.UnsetCapability( Graphics.Capabilities.AnisotropicFiltering );

			    // Check automatic mipmap generation.
			    if ((rkCurCaps.Caps2 & Caps2.CanAutoGenerateMipMap) == 0)
			        rsc.UnsetCapability( Graphics.Capabilities.HardwareMipMaps );

			    // Check Dot product 3.
			    if ((rkCurCaps.TextureOperationCaps & TextureOperationCaps.DotProduct3) == 0)
			        rsc.UnsetCapability( Graphics.Capabilities.Dot3 );

			    // Check cube map support.
			    if ((rkCurCaps.TextureCaps & TextureCaps.CubeMap) == 0)
				    rsc.UnsetCapability(Graphics.Capabilities.CubeMapping);
			
			    // Scissor test
			    if ((rkCurCaps.RasterCaps & RasterCaps.ScissorTest) == 0)
				    rsc.UnsetCapability(Graphics.Capabilities.ScissorTest);


			    // Two-sided stencil
			    if ((rkCurCaps.StencilCaps & StencilCaps.TwoSided) == 0)
				    rsc.UnsetCapability(Graphics.Capabilities.TwoSidedStencil);

			    // stencil wrap
			    if ((rkCurCaps.StencilCaps & StencilCaps.Increment) == 0 ||
				    (rkCurCaps.StencilCaps & StencilCaps.Decrement) == 0)
				    rsc.UnsetCapability(Graphics.Capabilities.StencilWrap);

			    // User clip planes
			    if (rkCurCaps.MaxUserClipPlanes == 0)			
				    rsc.UnsetCapability(Graphics.Capabilities.UserClipPlanes);			

			    // UBYTE4 type?
			    if ((rkCurCaps.DeclarationTypes & DeclarationTypeCaps.UByte4) == 0)			
				    rsc.UnsetCapability(Graphics.Capabilities.VertexFormatUByte4);	

			    // 3D textures?
			    if ((rkCurCaps.TextureCaps & TextureCaps.VolumeMap) == 0)			
				    rsc.UnsetCapability(Graphics.Capabilities.Texture3D);			

			    if ((rkCurCaps.TextureCaps & TextureCaps.Pow2) != 0)
			    {
				    // Conditional support for non POW2
				    if ((rkCurCaps.TextureCaps & TextureCaps.NonPow2Conditional) != 0)
					    rsc.NonPOW2TexturesLimited = true;				

				    // Only power of 2 supported.
				    else					
					    rsc.UnsetCapability(Graphics.Capabilities.NonPowerOf2Textures);				
			    }	

			    // Number of render targets
			    if (rkCurCaps.SimultaneousRTCount < rsc.MultiRenderTargetCount)
			    {
			        rsc.MultiRenderTargetCount = Utility.Min( rkCurCaps.SimultaneousRTCount, Config.MaxMultipleRenderTargets );
			    }	

			    if((rkCurCaps.PrimitiveMiscCaps & PrimitiveMiscCaps.MrtIndependentBitDepths) == 0)
			    {
				    rsc.UnsetCapability(Graphics.Capabilities.MRTDifferentBitDepths);
			    }

			    // Point sprites 
			    if (rkCurCaps.MaxPointSize <= 1.0f)
			    {
				    rsc.UnsetCapability(Graphics.Capabilities.PointSprites);
				    // sprites and extended parameters go together in D3D
				    rsc.UnsetCapability(Graphics.Capabilities.PointExtendedParameters);				
			    }
			
			    // Take the minimum point size.
			    if (rkCurCaps.MaxPointSize < rsc.MaxPointSize)
				    rsc.MaxPointSize = rkCurCaps.MaxPointSize;	

			    // Mipmap LOD biasing?
			    if ((rkCurCaps.RasterCaps & RasterCaps.MipMapLodBias) == 0)			
				    rsc.UnsetCapability(Graphics.Capabilities.MipmapLODBias);			


			    // Do we support per-stage src_manual constants?
			    // HACK - ATI drivers seem to be buggy and don't support per-stage constants properly?
			    // TODO: move this to RSC
			    if((rkCurCaps.PrimitiveMiscCaps & PrimitiveMiscCaps.PerStageConstant) == 0)
				    rsc.UnsetCapability(Graphics.Capabilities.PerStageConstant);

			    // Advanced blend operations? min max subtract rev 
                if ((rkCurCaps.PrimitiveMiscCaps & PrimitiveMiscCaps.BlendOperation) == 0)
				    rsc.UnsetCapability(Graphics.Capabilities.AdvancedBlendOperations);
		    }

            // Blending between stages supported
            rsc.SetCapability(Graphics.Capabilities.Blending);


            // We always support compression, D3DX will decompress if device does not support
            rsc.SetCapability(Graphics.Capabilities.TextureCompression);
            rsc.SetCapability(Graphics.Capabilities.TextureCompressionDXT);

            // We always support VBOs
            rsc.SetCapability(Graphics.Capabilities.VertexBuffer);


            ConvertVertexShaderCaps(rsc);
            ConvertPixelShaderCaps(rsc);

            // Adapter details
            var adapterId = _activeDriver.AdapterIdentifier;

		    // determine vendor
		    // Full list of vendors here: http://www.pcidatabase.com/vendors.php?sort=id
            switch (adapterId.VendorId)
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
            if (rsc.HasCapability(Graphics.Capabilities.VertexPrograms))
            {
                // GeForce4 Ti (and presumably GeForce3) does not
                // render infinite projection properly, even though it does in GL
                // So exclude all cards prior to the FX range from doing infinite
                if (rsc.Vendor != GPUVendor.Nvidia || // not nVidia
                    !((adapterId.DeviceId >= 0x200 && adapterId.DeviceId <= 0x20F) || //gf3
                    (adapterId.DeviceId >= 0x250 && adapterId.DeviceId <= 0x25F) || //gf4ti
                    (adapterId.DeviceId >= 0x280 && adapterId.DeviceId <= 0x28F) || //gf4ti
                    (adapterId.DeviceId >= 0x170 && adapterId.DeviceId <= 0x18F) || //gf4 go
                    (adapterId.DeviceId >= 0x280 && adapterId.DeviceId <= 0x28F)))  //gf4ti go
                {
                    rsc.SetCapability( Graphics.Capabilities.InfiniteFarPlane );
                }
            }

            // We always support rendertextures bigger than the frame buffer
            rsc.SetCapability( Graphics.Capabilities.HardwareRenderToTexture );

		    // Determine if any floating point texture format is supported
		    
            var floatFormats = new[] {Format.R16F, Format.G16R16F, 
			    Format.A16B16G16R16F, Format.R32F, Format.G32R32F, 
			    Format.A32B32G32R32F};
            

            var bbSurf = (Surface)renderWindow[ "DDBACKBUFFER" ];
            var bbSurfDesc = bbSurf.Description;

            for (var i = 0; i < 6; ++i)
            {
                if (manager.CheckDeviceFormat(_activeDriver.AdapterNumber,
                    DeviceType.Hardware, bbSurfDesc.Format, 0, ResourceType.Texture, floatFormats[i]))
                {
                    rsc.SetCapability( Graphics.Capabilities.TextureFloat );
                    break;
                }
            }

            // TODO: make convertVertex/Fragment fill in rsc
		    // TODO: update the below line to use rsc
		    // Vertex textures
		    if (rsc.IsShaderProfileSupported("vs_3_0"))
		    {
			    // Run through all the texture formats looking for any which support
			    // vertex texture fetching. Must have at least one!
			    // All ATI Radeon up to X1n00 say they support vs_3_0, 
			    // but they support no texture formats for vertex texture fetch (cheaters!)
			    if (CheckVertexTextureFormats(renderWindow))
			    {
			        rsc.SetCapability( Graphics.Capabilities.VertexTextureFetch );
				    // always 4 vertex texture units in vs_3_0, and never shared
			        rsc.TextureUnitCount = 4;
				    rsc.VertexTextureUnitsShared = false;
			    }
		    }	
		    else
		    {
			    //True HW Instancing is supported since Shader model 3.0 ATI has a nasty
			    //hack for enabling it in their SM 2.0 cards, but we don't (and won't) support it
			    rsc.UnsetCapability( Graphics.Capabilities.VertexBufferInstanceData );
		    }

            // Check alpha to coverage support
		    // this varies per vendor! But at least SM3 is required
		    if (rsc.IsShaderProfileSupported("ps_3_0"))
		    {
			    // NVIDIA needs a separate check
			    if (rsc.Vendor == GPUVendor.Nvidia)
			    {
                    if (manager.CheckDeviceFormat(0, DeviceType.Hardware, Format.X8R8G8B8, 0, ResourceType.Surface,
                        (Format)( 'A' | ( 'T' ) << 8 | ( 'O' ) << 16 | ( 'C' ) << 24 ) ))
                    {
                        rsc.SetCapability( Graphics.Capabilities.AlphaToCoverage );
                    }

			    }
			    else if (rsc.Vendor == GPUVendor.Ati)
			    {
				    // There is no check on ATI, we have to assume SM3 == support
			        rsc.SetCapability( Graphics.Capabilities.AlphaToCoverage );
			    }

			    // no other cards have Dx9 hacks for alpha to coverage, as far as I know
		    }

            if (realCapabilities == null)
		    {		
			    realCapabilities = rsc;
			    realCapabilities.AddShaderProfile("hlsl");

			    // if we are using custom capabilities, then 
			    // mCurrentCapabilities has already been loaded
			    if(!useCustomCapabilities)
				    currentCapabilities = realCapabilities;

                FireEvent("RenderSystemCapabilitiesCreated");

			    InitializeFromRenderSystemCapabilities(currentCapabilities, renderWindow);
		    }

		    return rsc;
        }

        [OgreVersion(1, 7)]
        private bool CheckVertexTextureFormats(D3DRenderWindow renderWindow)
        {
            var anySupported = false;

            var bbSurf = (Surface)renderWindow[ "DDBACKBUFFER" ];
            var bbSurfDesc = bbSurf.Description;

            for ( var pf = PixelFormat.L8; pf < PixelFormat.Count; pf++ )
            {
                var fmt = D3DHelper.ConvertEnum( pf );
                if ( manager.CheckDeviceFormat( _activeDriver.AdapterNumber, DeviceType.Hardware, bbSurfDesc.Format,
                                                Usage.QueryVertexTexture, ResourceType.Texture, fmt ) )
                {
                    // cool, at least one supported
                    anySupported = true;
                    LogManager.Instance.Write( "D3D9: Vertex texture format supported - {0}",
                                               PixelUtil.GetFormatName( pf ) );
                }
            }
            return anySupported;
        }

        [OgreVersion(1, 7)]
        private void ConvertPixelShaderCaps( RenderSystemCapabilities rsc )
        {
            var major = 0xFF;
		    var minor = 0xFF;
		    SlimDX.Direct3D9.Capabilities minPsCaps = null;

            // Find the device with the lowest vertex shader caps.
            foreach (var driver in _driverList)
            {
                var rkCurCaps = driver.D3D9DeviceCaps;
                var currMajor = rkCurCaps.PixelShaderVersion.Major;
                var currMinor = rkCurCaps.PixelShaderVersion.Minor;

                if (currMajor < major)
                {
                    major = currMajor;
                    minor = currMinor;
                    minPsCaps = rkCurCaps;
                }
                else if (currMajor == major && currMinor < minor)
                {
                    minor = currMinor;
                    minPsCaps = rkCurCaps;
                }
            }
		
		    var ps2a = false;
            var ps2b = false;
            var ps2x = false;

		    // Special case detection for ps_2_x/a/b support
		    if (major >= 2)
		    {
			    if ((minPsCaps.PS20Caps.Caps & PixelShaderCaps.NoTextureInstructionLimit) != 0 &&
				    (minPsCaps.PS20Caps.TempCount >= 32))
			    {
				    ps2b = true;
			    }

                if ((minPsCaps.PS20Caps.Caps & PixelShaderCaps.NoTextureInstructionLimit) != 0 &&
                    (minPsCaps.PS20Caps.Caps & PixelShaderCaps.NoDependentReadLimit) != 0 &&
                    (minPsCaps.PS20Caps.Caps & PixelShaderCaps.ArbitrarySwizzle) != 0 &&
                    (minPsCaps.PS20Caps.Caps & PixelShaderCaps.GradientInstructions) != 0 &&
                    (minPsCaps.PS20Caps.Caps & PixelShaderCaps.Predication) != 0 &&
				    (minPsCaps.PS20Caps.TempCount >= 22))
			    {
				    ps2a = true;
			    }

			    // Does this enough?
			    if (ps2a || ps2b)
			    {
				    ps2x = true;
			    }
		    }

            switch (major)
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
            switch (major)
            {
                case 3:
                    if ( minor > 0 )
                        rsc.AddShaderProfile( "ps_3_x" );

                    rsc.AddShaderProfile( "ps_3_0" );
                    goto case 2;
                case 2:
                    if ( ps2x )
                        rsc.AddShaderProfile( "ps_2_x" );
                    if ( ps2a )
                        rsc.AddShaderProfile( "ps_2_a" );
                    if ( ps2b )
                        rsc.AddShaderProfile( "ps_2_b" );

                    rsc.AddShaderProfile( "ps_2_0" );
                    goto case 1;
                case 1:
                    if ( major > 1 || minor >= 4 )
                        rsc.AddShaderProfile( "ps_1_4" );
                    if ( major > 1 || minor >= 3 )
                        rsc.AddShaderProfile( "ps_1_3" );
                    if ( major > 1 || minor >= 2 )
                        rsc.AddShaderProfile( "ps_1_2" );

                    rsc.AddShaderProfile( "ps_1_1" );
                    rsc.SetCapability( Graphics.Capabilities.FragmentPrograms );
                    break;
            }

        }

        [OgreVersion(1, 7)]
        private void ConvertVertexShaderCaps( RenderSystemCapabilities rsc )
        {
            var major = 0xFF;
		    var minor = 0xFF;
		    SlimDX.Direct3D9.Capabilities minVsCaps = null;

		    // Find the device with the lowest vertex shader caps.
		    foreach (var driver in _driverList)
		    {
                var rkCurCaps = driver.D3D9DeviceCaps;
		        var currMajor = rkCurCaps.VertexShaderVersion.Major;
                var currMinor = rkCurCaps.VertexShaderVersion.Minor;

			    if (currMajor < major)	
			    {
				    major = currMajor;
				    minor = currMinor;
				    minVsCaps = rkCurCaps;
			    }
			    else if (currMajor == major && currMinor < minor)
			    {
				    minor = currMinor;
				    minVsCaps = rkCurCaps;
			    }			
		    }

		
		    var vs2x = false;
		    var vs2a = false;

		    // Special case detection for vs_2_x/a support
		    if (major >= 2)
		    {
			    if ((minVsCaps.VS20Caps.Caps & VertexShaderCaps.Predication) != 0 &&
				    (minVsCaps.VS20Caps.DynamicFlowControlDepth > 0) &&
				    (minVsCaps.VS20Caps.TempCount >= 12))
			    {
				    vs2x = true;
			    }

                if ((minVsCaps.VS20Caps.Caps & VertexShaderCaps.Predication) != 0 &&
				    (minVsCaps.VS20Caps.DynamicFlowControlDepth > 0) &&
				    (minVsCaps.VS20Caps.TempCount >= 13))
			    {
				    vs2a = true;
			    }
		    }

		    // Populate max param count
            switch (major)
            {
                case 1:
                    // No boolean params allowed
                    rsc.VertexProgramConstantBoolCount = 0;
                    // No integer params allowed
                    rsc.VertexProgramConstantIntCount = 0;
                    // float params, always 4D
                    rsc.VertexProgramConstantFloatCount = minVsCaps.MaxVertexShaderConstants;
                    break;
                case 2:
                    // 16 boolean params allowed
                    rsc.VertexProgramConstantBoolCount = 16;
                    // 16 integer params allowed, 4D
                    rsc.VertexProgramConstantIntCount = 16;
                    // float params, always 4D
                    rsc.VertexProgramConstantFloatCount = minVsCaps.MaxVertexShaderConstants;
                    break;
                case 3:
                    // 16 boolean params allowed
                    rsc.VertexProgramConstantBoolCount = 16;
                    // 16 integer params allowed, 4D
                    rsc.VertexProgramConstantIntCount = 16;
                    // float params, always 4D
                    rsc.VertexProgramConstantFloatCount = minVsCaps.MaxVertexShaderConstants;
                    break;
            }

            // populate syntax codes in program manager (no breaks in this one so it falls through)
            switch (major)
            {
                case 3:
                    rsc.AddShaderProfile( "vs_3_0" );
                    goto case 2;
                case 2:
                    if ( vs2x )
                        rsc.AddShaderProfile( "vs_2_x" );
                    if ( vs2a )
                        rsc.AddShaderProfile( "vs_2_a" );

                    rsc.AddShaderProfile( "vs_2_0" );
                    goto case 1;
                case 1:
                    rsc.AddShaderProfile( "vs_1_1" );
                    rsc.SetCapability( Graphics.Capabilities.VertexPrograms );
                    break;
            }
        }
    }
}
