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
using Axiom.Demos;
using Axiom.Collections;
using OpenTK.Graphics;

namespace Droid.Demos
{
	class Tutorial1 : Axiom.Demos.Tutorial1
	{
		public override void SetupResources()
		{
		}

		public bool Setup( IGraphicsContext glContext, int width, int height )
		{
			// instantiate the Root singleton
			engine = Root.Instance;

			// add event handlers for frame events
			engine.FrameStarted += OnFrameStarted;
			engine.FrameRenderingQueued += OnFrameRenderingQueued;
			engine.FrameEnded += OnFrameEnded;

			Root.Instance.Initialize( false, "Axiom Engine Demo Window" );

			NamedParameterList miscParams = new NamedParameterList();
			miscParams.Add( "externalWindowInfo", glContext );
			miscParams.Add( "externalWindow", true );
			window = Root.Instance.CreateRenderWindow( "Droid.Demo", width, height, true, miscParams );

			TechDemoListener rwl = new TechDemoListener( window );
			WindowEventMonitor.Instance.RegisterListener( window, rwl );

			SetupResources();

			ChooseSceneManager();
			CreateCamera();
			CreateViewports();

			this.viewport.BackgroundColor = ColorEx.SteelBlue;

			// set default mipmap level
			TextureManager.Instance.DefaultMipmapCount = 5;

			// Create any resource listeners (for loading screens)
			this.CreateResourceListener();
			// Load resources
			this.LoadResources();

			ShowDebugOverlay( true );

			//CreateGUI();


			//input = SetupInput();

			// call the overridden CreateScene method
			CreateScene();
			return true;
		}

		protected override void OnFrameRenderingQueued( object source, FrameEventArgs evt )
		{			
			if ( evt.StopRendering )
				return;

			base.OnFrameRenderingQueued( source, evt );

			evt.StopRendering = true;
		}
	}
}