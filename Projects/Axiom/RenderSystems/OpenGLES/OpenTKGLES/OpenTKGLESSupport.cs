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
    public class OpenTKGLESSupport : GLESSupport
    {
        public override void AddConfig()
        {
            throw new NotImplementedException();
        }
        public override GLESPBuffer CreatePBuffer(Media.PixelComponentType format, int width, int height)
        {
            throw new NotImplementedException();
        }
        public override Graphics.RenderWindow CreateWindow(bool autoCreateWindow, GLESRenderSystem renderSystem, string windowTitle)
        {
            throw new NotImplementedException();
        }
        public override void GetProcAddress(string procname)
        {
            throw new NotImplementedException();
        }
        public override Graphics.RenderWindow NewWindow(string name, int width, int height, bool fullScreen, Collections.NameValuePairList miscParams = null)
        {
            throw new NotImplementedException();
        }
        public override void Start()
        {
            throw new NotImplementedException();
        }
        public override void Stop()
        {
            throw new NotImplementedException();
        }
    }
}