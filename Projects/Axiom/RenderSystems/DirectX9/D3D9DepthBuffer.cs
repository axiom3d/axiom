using System;
using Axiom.Graphics;
using SlimDX.Direct3D9;

namespace Axiom.RenderSystems.DirectX9
{
    public class D3D9DepthBuffer : DepthBuffer
    {
        [OgreVersion(1, 7)]
        protected Surface depthBuffer;

        [OgreVersion(1, 7)]
        protected Device creator;

        [OgreVersion(1, 7)]
        protected int multiSampleQuality;

        [OgreVersion(1, 7)]
        protected Format d3dFormat;

        [OgreVersion(1, 7)]
        protected D3DRenderSystem renderSystem;

        [OgreVersion(1, 7)]
        public Device DeviceCreator
        {
            get
            {
                return creator;
            }
        }

        [OgreVersion(1, 7)]
        public Surface DepthBufferSurface
        {
            get
            {
                return depthBuffer;
            }
        }

        public D3D9DepthBuffer( PoolId poolId, D3DRenderSystem renderSystem,
            Device creator, Surface depthBufferSurf,
            Format fmt, int width, int height,
            MultisampleType fsaa, int multiSampleQuality,
            bool isManual) :
            base(poolId, 0, width, height, (int)fsaa, "", isManual)
        {
            depthBuffer = depthBufferSurf;
            this.creator = creator;
            this.multiSampleQuality = multiSampleQuality;
			d3dFormat = fmt;
            this.renderSystem = renderSystem;

            switch (fmt)
            {
                case Format.D16Lockable:
                case Format.D15S1:
                case Format.D16:
                    bitDepth = 16;
                    break;
                case Format.D32:
                case Format.D24S8:
                case Format.D24X8:
                case Format.D24X4S4:
                case Format.D32Lockable:
                case Format.D24SingleS8:
                    bitDepth = 32;
                    break;
            }
        }
    }
}