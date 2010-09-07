using System;
using OpenGL = OpenTK.Graphics.ES11.GL;

namespace Axiom.RenderSystems.OpenGLES
{
    public static class GLESConfig
    {
        public static void GlCheckError(object caller)
        {
            int e = (int)OpenGL.GetError();
            if (e != 0)
            {
                throw new Exception("OpenGL error " + caller.ToString());
            }
        }
    }
}