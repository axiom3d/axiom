using System;
using Axiom.Graphics;
using SlimDX.Direct3D9;

// ReSharper disable InconsistentNaming

namespace Axiom.RenderSystems.DirectX9
{
    [OgreVersion(1, 7, 2790)]
    public class D3D9DepthBuffer : DepthBuffer
    {
        #region depthBuffer

        [OgreVersion(1, 7, 2790)]

        protected Surface depthBuffer;

        #endregion

        #region creator

        [OgreVersion(1, 7, 2790)]
        protected Device creator;

        #endregion

        #region multiSampleQuality

        [OgreVersion(1, 7, 2790)]
        protected int multiSampleQuality;

        #endregion

        #region d3dFormat

        [OgreVersion(1, 7, 2790)]
        protected Format d3dFormat;

        #endregion

        #region renderSystem

        [OgreVersion(1, 7, 2790)]
        protected D3DRenderSystem renderSystem;

        #endregion

        #region DeviceCreator

        [OgreVersion(1, 7, 2790)]
        public Device DeviceCreator
        {
            get
            {
                return creator;
            }
        }

        #endregion

        #region DepthBufferSurface

        [OgreVersion(1, 7, 2790)]
        public Surface DepthBufferSurface
        {
            get
            {
                return depthBuffer;
            }
        }

        #endregion

        #region Constructor

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

        #endregion

        #region Dispose

        protected override void dispose(bool disposeManagedResources)
        {
            if (!manual)
                depthBuffer.Dispose();
            depthBuffer = null;

            creator = null;

            base.dispose(disposeManagedResources);
        }

        #endregion

        #region IsCompatible

        [OgreVersion(1, 7, 2790)]
        public override bool IsCompatible(RenderTarget renderTarget)
        {

            var pBack = (Surface[])renderTarget[ "DDBACKBUFFER" ];
            if ( pBack[ 0 ] == null )
                return false;

            var srfDesc = pBack[ 0 ].Description;

            //RenderSystem will determine if bitdepths match (i.e. 32 bit RT don't like 16 bit Depth)
            //This is the same function used to create them. Note results are usually cached so this should
            //be quick
            var fmt = renderSystem.GetDepthStencilFormatFor( srfDesc.Format );
            var activeDevice = D3DRenderSystem.ActiveD3D9Device;

            return creator == activeDevice &&
                   fmt == d3dFormat &&
                   fsaa == (int)srfDesc.MultisampleType &&
                   multiSampleQuality == srfDesc.MultisampleQuality &&
                   Width >= renderTarget.Width &&
                   Height >= renderTarget.Height;
        }

        #endregion
    }
}

// ReSharper restore InconsistentNaming
