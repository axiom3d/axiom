using System;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.ES20;
using OpenTK.Platform;
using OpenTK.Platform.Android;

using Android.Views;
using Android.Content;

using Axiom.Core;
using SIS = SharpInputSystem;
using Axiom.Collections;

namespace Axiom.Samples.Browser
{
	public class SampleBrowser : Axiom.Samples.SampleBrowser
	{
        private IGraphicsContext GLGraphicsContext;
        private IWindowInfo GlWindowInfo;

        public SampleBrowser(IGraphicsContext iGraphicsContext, IWindowInfo iWindowInfo)
        {
            // TODO: Complete member initialization
            this.GLGraphicsContext = iGraphicsContext;
            this.GlWindowInfo = iWindowInfo;
        }
		
		protected override bool OneTimeConfig()
		{
			( new Axiom.RenderSystems.OpenGLES2.GLES2Plugin() ).Initialize();

			Root.Instance.RenderSystem = Root.Instance.RenderSystems[ "OpenGLES2" ];

			return true;
		}

        protected override void CreateWindow()
        {
            Root.Initialize(false, "Axiom Sample Browser");
            var miscParams = new NamedParameterList();
            var width = 800;
            var height = 600;
            miscParams.Add("externalWindowInfo", this.GlWindowInfo);
            miscParams.Add("externalGLContext", GLGraphicsContext);
            this.RenderWindow = Root.CreateRenderWindow("AndroidMainWindow", width, height, true, miscParams);
        }
		/// <summary>
		/// Sets up SIS input.
		/// </summary>
		protected override void SetupInput()
		{
			SIS.ParameterList pl = new SIS.ParameterList();
			pl.Add( new SIS.Parameter( "WINDOW", RenderWindow[ "WINDOW" ] ) );
#if !(XBOX || XBOX360 || WINDOWS_PHONE )
			pl.Add( new SIS.Parameter( "w32_mouse", "CLF_BACKGROUND" ) );
			pl.Add( new SIS.Parameter( "w32_mouse", "CLF_NONEXCLUSIVE" ) );
#endif
			this.InputManager = SIS.InputManager.CreateInputSystem( pl );

			CreateInputDevices();      // create the specific input devices

			this.WindowResized( RenderWindow );    // do an initial adjustment of mouse area
		}

#if WINDOWS_PHONE
		protected override void CreateWindow()
		{
			base.Root.Initialize( false, "Axiom Sample Browser" );
			var parms = new Collections.NamedParameterList();
			parms.Add( "xnaGraphicsDevice", Graphics );
			base.RenderWindow = base.Root.CreateRenderWindow( "Axiom Sample Browser", 480, 800, true, parms );

		}
#endif
	}
}
