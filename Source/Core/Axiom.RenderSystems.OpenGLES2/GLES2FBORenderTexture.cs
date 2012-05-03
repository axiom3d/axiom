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
using GLenum = OpenTK.Graphics.ES20.All;

namespace Axiom.RenderSystems.OpenGLES2
{
    class GLES2FBORenderTexture : GLES2RenderTexture
    {
        private GLES2FrameBufferObject fb;
        
        public GLES2FBORenderTexture(GLES2FBOManager manager, string name, GLES2SurfaceDesc target, bool writeGamma, int fsaa)
            :base(name, target, writeGamma, fsaa)
        {
            fb.BindSurface(0, target);

            width = fb.Width;
            height = fb.Height;
        }
        public override object this[string attribute]
        {
            get
            {
                
                if (attribute == "FBO")
                {
                    return fb;
                }
                return base[attribute];
            }
        }
        public override void SwapBuffers(bool waitForVSync)
        {
            fb.SwapBuffers();
        }
        public override bool AttachDepthBuffer(DepthBuffer ndepthBuffer)
        {
            bool result;
            result = base.AttachDepthBuffer(ndepthBuffer);
            if (result)
            {
                fb.AttachDepthBuffer(ndepthBuffer);
            }

            return result;
        }
        public override void DetachDepthBuffer()
        {
            fb.DetachDepthBuffer();
        }
        public override void _DetachDepthBuffer()
        {
            fb.DetachDepthBuffer();
            base._DetachDepthBuffer();
        }
    }
}