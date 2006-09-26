using System;
using CeGuiSharp;
using CeGuiSharp.Widgets;

namespace CeGuiSharp.WidgetSets.WindowsLook {
	/// <summary>
	/// Summary description for TLStaticText.
	/// </summary>
	public class WLStaticText : StaticText {
		#region Constructor

		/// <summary>
		///		Constructor.
		/// </summary>
		/// <param name="name">Name of this widget.</param>
		public WLStaticText(string type, string name) : base(type, name) {
		}

		/// <summary>
		///		Init this widget.
		/// </summary>
		public override void Initialize() {
			base.Initialize ();

			Imageset imageset = ImagesetManager.Instance.GetImageset("WindowsLook");

			SetFrameImages(
				imageset.GetImage("StaticFrameTopLeft"),
				imageset.GetImage("StaticFrameTopRight"),
				imageset.GetImage("StaticFrameBottomLeft"),
				imageset.GetImage("StaticFrameBottomRight"),
				imageset.GetImage("StaticFrameLeft"),
				imageset.GetImage("StaticFrameTop"),
				imageset.GetImage("StaticFrameRight"),
				imageset.GetImage("StaticFrameBottom"));

			SetBackgroundImage(imageset.GetImage("Background"));
			SetBackgroundColors(new ColorRect(new Color(0xFFDFDFDF)));

			FrameEnabled = true;
			BackgroundEnabled = true;
		}


		#endregion Constructor
	}
}
