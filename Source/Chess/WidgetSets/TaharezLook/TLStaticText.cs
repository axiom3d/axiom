using System;
using CeGuiSharp;
using CeGuiSharp.Widgets;

namespace CeGuiSharp.WidgetSets.TaharezLook {
	/// <summary>
	/// Summary description for TLStaticText.
	/// </summary>
	public class TLStaticText : StaticText {
		#region Constructor

		/// <summary>
		///		Constructor.
		/// </summary>
		/// <param name="name">Name of this widget.</param>
		public TLStaticText(string type, string name) : base(type, name) {
		}

		/// <summary>
		///		Init this widget.
		/// </summary>
		public override void Initialize() {
			base.Initialize ();

			Imageset imageset = ImagesetManager.Instance.GetImageset("TaharezLook");

			SetFrameImages(
				imageset.GetImage("StaticTopLeft"),
				imageset.GetImage("StaticTopRight"),
				imageset.GetImage("StaticBottomLeft"),
				imageset.GetImage("StaticBottomRight"),
				imageset.GetImage("StaticLeft"),
				imageset.GetImage("StaticTop"),
				imageset.GetImage("StaticRight"),
				imageset.GetImage("StaticBottom"));

			SetBackgroundImage(imageset.GetImage("StaticBackdrop"));

			FrameEnabled = true;
			BackgroundEnabled = true;
		}


		#endregion Constructor
	}
}
