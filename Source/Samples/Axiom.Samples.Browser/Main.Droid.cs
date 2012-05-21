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
		protected override void OnCreate( Bundle bundle )
		{
			base.OnCreate( bundle );
			Axiom.Platform.Android.Plugin platform = new Plugin();
			Axiom.RenderSystems.OpenGLES2.GLES2Plugin renderSystem = new GLES2Plugin();

            // Inflate our UI from its XML layout description
            // - should match filename res/layout/main.xml ?
            SetContentView(Resource.Layout.Main);

            // Load the view
            FindViewById(Resource.Id.samplebrowserview);
        }

		protected override void OnPause()
		{
			base.OnPause();
            var view = FindViewById<SampleBrowserView>(Resource.Id.samplebrowserview);
            view.Pause();
		}

		protected override void OnResume()
		{
			base.OnResume();
            var view = FindViewById<SampleBrowserView>(Resource.Id.samplebrowserview);
            view.Resume();
        }

	}
}

