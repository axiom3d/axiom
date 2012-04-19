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

namespace Axiom.RenderSystems.OpenGLES2
{
    static class GLES2InternalShaders
    {
        public static enum InternalShader
        {
            ES2_LightingShader
        }
        public static byte[] LightingShader
        {
            get
            {
                List<byte> retVal = new List<byte>();
                retVal.Add(byte.Parse("attribute vec4 a_position;   \n"));
                retVal.Add(byte.Parse("void main()                  \n"));
                retVal.Add(byte.Parse("{                            \n"));
                retVal.Add(byte.Parse(" gl_Position = a_position;   \n"));
                retVal.Add(byte.Parse("}                            \n"));
                return retVal.ToArray();
            }
        }
    }
}