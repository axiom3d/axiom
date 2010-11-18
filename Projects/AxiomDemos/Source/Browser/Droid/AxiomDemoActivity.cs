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

namespace Droid
{
	[Activity( MainLauncher = true, Label = "Axiom Demo Browser" )]
	public class AxiomDemoActivity : Activity
	{
		DemoView view;

		protected override void OnCreate( Bundle bundle )
		{
			base.OnCreate( bundle );

			view = new DemoView( this );
			SetContentView( view );
		}

		protected override void OnDestroy()
		{
			view.Close();
			base.OnDestroy();
		}

		protected override void OnPause()
		{
			view.Close();
			base.OnPause();
		}
	}
}