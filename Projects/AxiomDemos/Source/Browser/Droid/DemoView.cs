using System;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.ES11;
using OpenTK.Platform;
using OpenTK.Platform.Android;

using Android.Views;
using Android.Content;
using Axiom.Core;

namespace Droid
{
	class DemoView : AndroidGameView
	{

		protected Root engine;

		public DemoView( IntPtr handle )
			: base( handle )
		{
		}

		// This gets called when the drawing surface is ready
		protected override void OnLoad( EventArgs e )
		{
			base.OnLoad( e );

			this.GLContextVersion = GLContextVersion.Gles2_0;

			try
			{
				//new AndroidResourceGroupManager();

				// instantiate the Root singleton
				engine = new Root( "AxiomDemos.log" );

				( new Axiom.RenderSystems.OpenGLES.GLESPlugin() ).Initialize();

				Root.Instance.RenderSystem = Root.Instance.RenderSystems[ "OpenGLES" ];

				_loadPlugins();

				_setupResources();

				Axiom.Demos.TechDemo demo = new Axiom.Demos.Tutorial1();

				demo.Setup();
			}
			catch ( Exception ex )
			{
				Console.WriteLine( "An exception has occurred. See below for details:" );
				Console.WriteLine( BuildExceptionString( ex ) );
			}
			// UpdateFrame and RenderFrame are called
			// by the render loop. This is takes effect
			// when we use 'Run ()', like below
			UpdateFrame += this.Update;

			RenderFrame += delegate
			{
				engine.RenderOneFrame();
			};

			// Run the render loop
			Run();
		}

		protected virtual void Update( object sender, OpenTK.FrameEventArgs e )
		{
		}

		private void _setupResources()
		{
		}

		private void _loadPlugins()
		{
		}

		private static string BuildExceptionString( Exception exception )
		{
			string errMessage = string.Empty;

			errMessage += exception.Message + Environment.NewLine + exception.StackTrace;

			while ( exception.InnerException != null )
			{
				errMessage += BuildInnerExceptionString( exception.InnerException );
				exception = exception.InnerException;
			}

			return errMessage;
		}

		private static string BuildInnerExceptionString( Exception innerException )
		{
			string errMessage = string.Empty;

			errMessage += Environment.NewLine + " InnerException ";
			errMessage += Environment.NewLine + innerException.Message + Environment.NewLine + innerException.StackTrace;

			return errMessage;
		}
	}
}
