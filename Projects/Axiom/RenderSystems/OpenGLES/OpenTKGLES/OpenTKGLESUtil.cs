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

namespace Axiom.RenderSystems.OpenGLES.OpenTKGLES
{
    public class OpenTKGLESUtil
    {
        /// <summary>
        /// 
        /// </summary>
        public static OpenTKGLESSupport GLESSupport
        {
            get { return new OpenTKGLESSupport(); }
        }
    }
}