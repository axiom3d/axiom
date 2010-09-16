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
	public class AxiomDemoActivity : Activity
	{
		public AxiomDemoActivity( IntPtr handle )
			: base( handle )
		{
		}

		protected override void OnCreate( Bundle bundle )
		{
			base.OnCreate( bundle );

			DemoView view = new DemoView(this);
			SetContentView(view);
			//// Inflate our UI from its XML layout description
			//SetContentView( R.layout.demo );

			//// Load the view
			//FindViewById( R.id.paintingview );
		}
	}
}