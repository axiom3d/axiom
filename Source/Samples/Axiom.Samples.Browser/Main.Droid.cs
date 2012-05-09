#region Namespace Declarations

using System;

using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;

using Axiom.Platform.Android;
using Axiom.RenderSystems.OpenGLES2;

#endregion Namespace Declarations
			
namespace Axiom.Samples.Browser
{
	[Activity( Label = "Axiom.Samples.Browser", MainLauncher = true, Icon = "@drawable/GameThumbnail" )]
	public class Main : Activity
	{
		SampleBrowser browser = new SampleBrowser();

		protected override void OnCreate( Bundle bundle )
		{
			base.OnCreate( bundle );
			Axiom.Platform.Android.Plugin platform = new Plugin();
			Axiom.RenderSystems.OpenGLES2.GLES2Plugin renderSystem = new GLES2Plugin();

			browser.Go();
		}

		protected override void OnPause()
		{
			base.OnPause();
			browser.PauseCurrentSample();
		}

		protected override void OnResume()
		{
			base.OnResume();
			browser.UnpauseCurrentSample();
		}

	}
}

