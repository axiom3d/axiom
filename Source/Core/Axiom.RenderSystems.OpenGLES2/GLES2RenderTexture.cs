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
    class GLES2SurfaceDesc
    {
        public GLES2HardwarePixelBuffer buffer = null;
        public int zoffset = 0;
        public int numSamples = 0;

        public GLES2SurfaceDesc(GLES2HardwarePixelBuffer buffer, int zoffset, int numSamples)
        {
            this.buffer = buffer;
            this.zoffset = zoffset;
            this.numSamples = numSamples;
        }

    }
    
    class GLES2RenderTexture : RenderTexture
    {

        public GLES2RenderTexture(string name, GLES2SurfaceDesc target, bool writeGamma, int fsaa)
            : base(target.buffer, target.zoffset)
        {
            base.name = name;
            this.hwGamma = writeGamma;
            base.fsaa = fsaa;
        }
        public override object this[string attribute]
        {
            get
            {
                if (attribute == "TARGET")
                {
                    GLES2SurfaceDesc target = new GLES2SurfaceDesc((pixelBuffer as GLES2HardwarePixelBuffer), target.zoffset, 0);
                    return target;
                }
                return base[attribute];
            }
        }
        public override bool RequiresTextureFlipping
        {
            get { return true; }
        }
    }
}