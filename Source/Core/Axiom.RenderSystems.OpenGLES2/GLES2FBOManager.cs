using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Glenum = OpenTK.Graphics.ES20.All;

namespace Axiom.RenderSystems.OpenGLES2
{
    class GLES2FBOManager : GLES2RTTManager
    {
        #region NestedTypes
		 struct FormatProperties
        {
            public bool valid; //This format can be used as RTT (FBO)
            public struct Mode
            {
                public int depth;
                public int stencil;
            }
            public List<Mode> modes;
        }
        class RBFormat
        {
            public Glenum format;
            public int width;
            public int height;
            public int samples;

            public RBFormat(Glenum format, int width, int height, int samples)
            {
                this.format = format;
                this.width = width;
                this.height = height;
                this.samples = samples;
            }
            private bool LessThan(RBFormat other)
            {
                if(format < other.format)
                    return true;
                else if(format == other.format)
                {
                    if(width < other.width)
                        return true;
                    else if(width == other.width)
                    {
                        if(height < other.height)
                            return true;
                        else if(height == other.height)
                        {
                            if(samples < other.samples)
                                return true;
                        }
                    }
                }

                return false;
            }
            public bool operator <(RBFormat other)
            {
                return LessThan(other);
            }
        }
        struct RBRef
        {
            public GLES2RenderBuffer buffer;
            int refCount;

            public RBRef(GLES2RenderBuffer buffer, int refCount)
            {
                this.buffer = buffer;
                this.refCount = refCount;
            }
            public RBRef(GLES2RenderBuffer buffer)
            {
                this.buffer = buffer;
                this.refCount = 1;
            }
        } 
	#endregion

        Dictionary<RBFormat, RBRef> renderBufferMap;
        int tempFBO;

        public GLES2FBOManager()
        {
        }

        public override void  Bind(Graphics.RenderTarget target)
        {
 	         base.Bind(target);
        }
        public override void  Unbind(Graphics.RenderTarget target)
        {
 	         base.Unbind(target);
        }
        public override Graphics.RenderTexture  CreateRenderTexture(string name, GLES2SurfaceDesc target, bool writeGamme, int fsaa)
        {
 	         return base.CreateRenderTexture(name, target, writeGamme, fsaa);
        }
        public override void GetBestDepthStencil(OpenTK.Graphics.ColorFormat internalColorFormat, ref OpenTK.Graphics.ES20.All depthFormat, ref OpenTK.Graphics.ES20.All stencilFormat)
        {
            base.GetBestDepthStencil(internalColorFormat, ref depthFormat, ref stencilFormat);
        }
        public override bool  CheckFormat(Media.PixelFormat format)
        {
 	         return base.CheckFormat(format);
        }
        public GLES2SurfaceDesc RequestRenderBuffer(Glenum format, int width, int height, int fsaa)
        {
        }
        public void RequestRenderBuffer(out GLES2SurfaceDesc surface)
        {}
        public void ReleaseRenderBuffer(GLES2SurfaceDesc surface)
        {}
        private void DetectFBOFormats()
        {}
        private int TryFormat(Glenum depthFormat, Glenum stencilFormat)
        {}
        private bool TryPackedFormat(Glenum packedFormat)
        {}

        public int TemporaryFBO
        {
            get{return this.tempFBO;}
        }
    }
}
