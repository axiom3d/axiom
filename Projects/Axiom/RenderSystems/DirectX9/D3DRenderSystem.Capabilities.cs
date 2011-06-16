using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Axiom.Configuration;
using Axiom.Graphics;

namespace Axiom.RenderSystems.DirectX9
{
    public partial class D3DRenderSystem
    {
        [OgreVersion(0, 0, "implement activeD3DDriver")]
        private void UpdateRenderSystemCapabilities()
        {
            var rsc = realCapabilities ?? new RenderSystemCapabilities();

            rsc.SetCategoryRelevant(CapabilitiesCategory.D3D9, true);
		    rsc.DriverVersion = driverVersion;
		    //rsc.DeviceName = activeD3DDriver->DriverDescription());
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
            }

            throw new NotImplementedException();
        }
    }
}
