using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Axiom.Graphics;

namespace Axiom.RenderSystems.OpenGLES2
{
    /// <summary>
    /// MultiRenderTarget for GL ES 2.x.
    /// </summary>
    class GLES2FBOMultiRenderTarget : MultiRenderTarget
    {
        private GLES2FrameBufferObject fbo;

        public GLES2FBOMultiRenderTarget(GLES2FBOManager manager, string name)
            : base(name)
        {
            fbo = new GLES2FrameBufferObject(manager, 0); //Ogre TODO: multisampling on MRTs?
        }

        public override object this[string attribute]
        {
            get
            {
                if (name == "FBO")
                {
                    return base[attribute] as GLES2FrameBufferObject;
                }
                return base[attribute];
            }
        }

        public override bool RequiresTextureFlipping
        {
            get { return true; }
        }
        public override bool AttachDepthBuffer(DepthBuffer ndepthBuffer)
        {
            bool result = false;

                if((result = base.AttachDepthBuffer(depthBuffer)))
                    fbo.AttachDepthBuffer(depthBuffer);

            return result;
        }
        public override void DetachDepthBuffer()
        {
            fbo.DetachDepthBuffer();
            base.DetachDepthBuffer();
        }
        public override void _DetachDepthBuffer()
        {
            fbo.DetachDepthBuffer();
            base._DetachDepthBuffer();
        }
        protected override void BindSurfaceImpl(int attachment, RenderTexture target)
        {
            //Check if the render target is in the rendertarget.FBO map
            GLES2FrameBufferObject fboojb = null;
            fboojb = (GLES2FrameBufferObject)target["FBO"];
            fbo.BindSurface(attachment, fboojb.GetSurface(0));

            this.width = fbo.Width;
            this.height = fbo.Height;
        }
        protected override void UnbindSurfaceImpl(int attachment)
        {
            fbo.UnbindSurface(attachment);

            //Set width and height
            this.width = fbo.Width;
            this.height = fbo.Height;
        }


    }
}