using System;
using CeGuiSharp;
using CeGuiSharp.Widgets;

namespace CeGuiSharp.WidgetSets.TaharezLook {
	/// <summary>
	/// Summary description for TLSliderThumb.
	/// </summary>
	public class TLSliderThumb : Thumb {
		#region Constants

		/// <summary>
		///		Name of the imageset to use for rendering.
		/// </summary>
		public const string ImagesetName = "TaharezLook";
		/// <summary>
		///		Name of the image to use for normal rendering.
		/// </summary>
		public const string NormalImageName = "VertSliderThumbNormal";
		/// <summary>
		///		Name of the image to use for hover / highlighted rendering.
		/// </summary>
		public const string HighlightImageName = "VertSliderThumbHover";

		#endregion Constants

		#region Fields

		/// <summary>
		///		Image to render in normal state.
		/// </summary>
		protected Image normalImage;
		/// <summary>
		///		Image to render in highlighted state.
		/// </summary>
		protected Image highlightImage;

		#endregion Fields

		#region Constructor

		/// <summary>
		///		Constructor.
		/// </summary>
		/// <param name="name"></param>
		public TLSliderThumb(string type, string name) : base(type, name) {
			// load the images for the set
			Imageset imageSet = ImagesetManager.Instance.GetImageset(ImagesetName);

			normalImage = imageSet.GetImage(NormalImageName);
			highlightImage = imageSet.GetImage(HighlightImageName);
		}

		#endregion Constructor

		#region PushButton Members

		/// <summary>
		///		Render thumb in the normal state.
		/// </summary>
		/// <param name="z">Z value for rendering.</param>
		protected override void DrawNormal(float z) {
			Rect clipper = PixelRect;

			// do nothing if the widget is totally clipped.
			if (clipper.Width == 0) {
				return;
			}

			// get the destination screen rect for this window
			Rect absRect = UnclippedPixelRect;

			// calculate colours to use.
			Color colorVal = new Color(EffectiveAlpha, 1, 1, 1);
			ColorRect colors = new ColorRect(colorVal, colorVal, colorVal, colorVal);

			// draw the image
			normalImage.Draw(absRect, z, clipper, colors);
		}

		/// <summary>
		///		Render thumb in the hover state.
		/// </summary>
		/// <param name="z">Z value for rendering.</param>
		protected override void DrawHover(float z) {
			Rect clipper = PixelRect;

			// do nothing if the widget is totally clipped.
			if (clipper.Width == 0) {
				return;
			}

			// get the destination screen rect for this window
			Rect absRect = UnclippedPixelRect;

			// calculate colours to use.
			Color colorVal = new Color(EffectiveAlpha, 1, 1, 1);
			ColorRect colors = new ColorRect(colorVal, colorVal, colorVal, colorVal);

			// draw the image
			highlightImage.Draw(absRect, z, clipper, colors);
		}

		/// <summary>
		///		Render thumb in the pushed state.
		/// </summary>
		/// <param name="z">Z value for rendering.</param>
		protected override void DrawPushed(float z) {
			DrawHover(z);
		}

		#endregion PushButton Members
	}
}
