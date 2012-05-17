using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.ES20;
using OpenTK.Platform;
using OpenTK.Platform.Android;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;

namespace Axiom.Samples.Browser
{
    public class SampleBrowserView : AndroidGameView
    {
        SampleBrowser browser;

        public SampleBrowserView(Context context, IAttributeSet attrs) 
            : base(context, attrs)
        {
            Initialize();
        }

        private void Initialize()
        {
        }

        // This method is called everytime the context needs
        // to be recreated. Use it to set any egl-specific settings
        // prior to context creation
        protected override void CreateFrameBuffer()
        {
            GLContextVersion = GLContextVersion.Gles2_0;

            // the default GraphicsMode that is set consists of (16, 16, 0, 0, 2, false)
            try
            {
                Log.Verbose("GLCube", "Loading with default settings");

                // if you don't call this, the context won't be created
                base.CreateFrameBuffer();
                return;
            }
            catch (Exception ex)
            {
                Log.Verbose("GLCube", "{0}", ex);
            }

            // this is a graphics setting that sets everything to the lowest mode possible so
            // the device returns a reliable graphics setting.
            try
            {
                Log.Verbose("GLCube", "Loading with custom Android settings (low mode)");
                GraphicsMode = new AndroidGraphicsMode(0, 0, 0, 0, 0, false);

                // if you don't call this, the context won't be created
                base.CreateFrameBuffer();
                return;
            }
            catch (Exception ex)
            {
                Log.Verbose("GLCube", "{0}", ex);
            }
            throw new Exception("Can't load egl, aborting");
        }

        // This gets called when the drawing surface is ready
        protected override void OnLoad(EventArgs e)
        {
            // this call is optional, and meant to raise delegates
            // in case any are registered
            base.OnLoad(e);

            this.browser = new SampleBrowser( this.GraphicsContext, this.WindowInfo );
            
            // Run the render loop
            browser.Go();           
        }

        // this occurs mostly on rotation.
        protected override void OnResize(EventArgs e)
        {
        }

        public override void Resume()
        {
            base.Resume();
            if ( this.browser != null )
                this.browser.UnpauseCurrentSample();
        }

        public override void Pause()
        {
            base.Pause();
            if (this.browser != null)
                this.browser.PauseCurrentSample();
        }
    }
}