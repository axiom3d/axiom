using System;
using CeGuiSharp;
using CeGuiSharp.Widgets;

namespace CeGuiSharp.WidgetSets.WindowsLook {

	/// <summary>
	/// 
	/// </summary>
	public class WLSliderThumb : Thumb
	{
		#region Constants

		// Image names
		/// <summary>
		/// 
		/// </summary>
		protected const string	ImagesetName			= "WindowsLook";
		/// <summary>
		/// 
		/// </summary>
		protected const string	NormalImageName		= "SliderThumbNormal";
		/// <summary>
		/// 
		/// </summary>
		protected const string	HighlightImageName	= "SliderThumbHover";
		/// <summary>
		/// 
		/// </summary>
		protected const string	MouseCursorImageName	= "MouseEsWeCursor";

		#endregion

		#region Fields


		/// <summary>
		/// 
		/// </summary>
		protected Image normalImage;
		/// <summary>
		/// 
		/// </summary>
		protected Image highlightImage;

		#endregion

		#region Constructor

		/// <summary>
		/// 
		/// </summary>
		/// <param name="name"></param>
		public WLSliderThumb( string type, string name ) : base(type, name) {
			Imageset imageSet = ImagesetManager.Instance.GetImageset(ImagesetName);

			normalImage = imageSet.GetImage(NormalImageName);
			highlightImage = imageSet.GetImage(HighlightImageName);

			SetMouseCursor(imageSet.GetImage(MouseCursorImageName));
		}

		#endregion

		#region PushbuttonMembers

		/// <summary>
		/// 
		/// </summary>
		/// <param name="z"></param>
		protected override void DrawNormal(float z) {
			Rect clipper = PixelRect;

			// do nothing if the widget is totally clipped.
			if(clipper.Width == 0) {
				return;
			}

			// get the destination screen rect for this window
			Rect absRect = UnclippedPixelRect;

			// calculate the colors to use.
			ColorRect colors = new ColorRect(new Color(EffectiveAlpha, 1.0f, 1.0f, 1.0f));

			// draw the image
			normalImage.Draw(absRect, z, clipper, colors);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="z"></param>
		protected override void DrawHover(float z) {
			Rect clipper = PixelRect;

			// do nothing if the widget is totally clipped.
			if(clipper.Width == 0) {
				return;
			}

			// get the destination screen rect for this window
			Rect absRect = UnclippedPixelRect;

			// calculate the colors to use.
			ColorRect colors = new ColorRect(new Color(EffectiveAlpha, 1.0f, 1.0f, 1.0f));

			// draw the image
			highlightImage.Draw(absRect, z, clipper, colors);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="z"></param>
		protected override void DrawDisabled(float z) {
			Rect clipper = PixelRect;

			// do nothing if the widget is totally clipped.
			if(clipper.Width == 0) {
				return;
			}

			// get the destination screen rect for this window
			Rect absRect = UnclippedPixelRect;

			// calculate the colors to use.
			ColorRect colors = new ColorRect(new Color(0x7F7F7F));
			colors.SetAlpha(EffectiveAlpha);

			// draw the image
			normalImage.Draw(absRect, z, clipper, colors);
		}

		#endregion
	}
}
