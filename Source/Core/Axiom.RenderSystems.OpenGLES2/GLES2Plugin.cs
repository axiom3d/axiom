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
using Axiom.Core;
using Axiom.Graphics;

namespace Axiom.RenderSystems.OpenGLES2
{
    class GLES2Plugin : IPlugin
    {
        private RenderSystem _renderSystem;

        public void Initialize()
        {
            _renderSystem = new GLES2RenderSystem();

            Root.Instance.RenderSystems.Add(_renderSystem);
        }

        public void Shutdown()
        {
            _renderSystem.Dispose();
            _renderSystem = null;
        }

    }
}