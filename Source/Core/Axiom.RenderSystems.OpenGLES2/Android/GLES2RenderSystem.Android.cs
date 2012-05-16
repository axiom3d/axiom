using System;
using System.Collections.Generic;
using System.Text;

using Axiom.Graphics;

namespace Axiom.RenderSystems.OpenGLES2
{
    public partial class GLES2RenderSystem 
    {
        partial void CreateGlSupport()
        {
            this.glSupport = new Android.AndroidSupport();
        }
    }
}
