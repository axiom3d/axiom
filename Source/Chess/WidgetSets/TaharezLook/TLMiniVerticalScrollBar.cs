using System;
using System.Drawing;
using CeGuiSharp;
using CeGuiSharp.Widgets;

namespace CeGuiSharp.WidgetSets.TaharezLook 
{
	/// <summary>
	/// Mini vertical scrollbar widget for the Taharez Gui Scheme.
	/// </summary>
	public class TLMiniVerticalScrollbar : Scrollbar
	{
		#region Constants
		/// <summary>
		///		Name of the Imageset to use for rendering.
		/// </summary>
		const string ImagesetName = "TaharezLook";
		/// <summary>
		///		Name of image to use for the main body of the scroll bar
		/// </summary>
		const string ScrollbarBodyImageName = "MiniVertScrollBarSegment";
		/// <summary>
		///		Name of image to use for the up button in normal state.
		/// </summary>
		const string UpButtonNormalImageName = "MiniVertScrollUpNormal";
		/// <summary>
		///		Name of image to use for the up button in highlighted state.
		/// </summary>
		const string UpButtonHighlightImageName = "MiniVertScrollUpHover";
		/// <summary>
		///		Name of image to use for the down button in normal state.
		/// </summary>
		const string DownButtonNormalImageName = "MiniVertScrollDownNormal";
		/// <summary>
		///		Name of image to use for the down button in the highlighted state.
		/// </summary>
		const string DownButtonHighlightImageName = "MiniVertScrollDownHover";

		/// <summary>
		/// Relative X co-ordinate for the thumb.
		/// </summary>
		const float ThumbPositionX = 0.15f;
		/// <summary>
		/// Relative width of the thumb.
		/// </summary>
		const float ThumbWidth = 0.7f;
		/// <summary>
		/// Relative X co-ordinate for the body imagery.
		/// </summary>
		const float BodyPositionX = 0.3f;
		/// <summary>
		/// Relative width for the body imagery.
		/// </summary>
		const float BodyWidth = 0.4f;
		
		
		/// <summary>
		///		Type of the thumb to create.
		/// </summary>
		const string ThumbType			= "TaharezLook.TLMiniVerticalScrollbarThumb";
		/// <summary>
		///		Type of the buttons to create.
		/// </summary>
		const string ButtonType			= "TaharezLook.TLButton";

		#endregion

		#region Fields
		/// <summary>
		/// Image for body segment.
		/// </summary>
		protected Image	bodyImage;

		#endregion

		#region Constructor
		/// <summary>
		///		Constructor.
		/// </summary>
		/// <param name="name"></param>
		public TLMiniVerticalScrollbar(string type, string name) : base(type, name) 
		{
			bodyImage = ImagesetManager.Instance.GetImageset(ImagesetName).GetImage(ScrollbarBodyImageName);
		}

		#endregion

		#region Scrollbar Methods

		/// <summary>
		/// create a PushButton based widget to use as the increaseButton button for this scroll bar.
		/// </summary>
		/// <returns>A reference to a PushButton widget for use in the scrollbar</returns>
		protected override PushButton CreateIncreaseButton()
		{
			TLButton newButton = (TLButton)WindowManager.Instance.CreateWindow(ButtonType, name + "_auto_IncreaseButton");

			newButton.StandardImageryEnabled = false;
			newButton.CustomImageryAutoSized = true;
			newButton.AlwaysOnTop = true;

			Imageset imageSet = ImagesetManager.Instance.GetImageset(ImagesetName);
			RenderableImage image = new RenderableImage();
			image.HorizontalFormat = HorizontalImageFormat.Stretched;
			image.VerticalFormat = VerticalImageFormat.Stretched;
			image.Image = imageSet.GetImage(DownButtonNormalImageName);
			newButton.SetNormalImage(image);
			newButton.SetDisabledImage(image);

			image = new RenderableImage();
			image.HorizontalFormat = HorizontalImageFormat.Stretched;
			image.VerticalFormat = VerticalImageFormat.Stretched;
			image.Image = imageSet.GetImage(DownButtonHighlightImageName);
			newButton.SetHoverImage(image);
			newButton.SetPushedImage(image);

			return newButton;
		}

		/// <summary>
		/// create a PushButton based widget to use as the decreaseButton button for this scroll bar.
		/// </summary>
		/// <returns>A reference to a PushButton widget for use in the scrollbar</returns>
		protected override PushButton CreateDecreaseButton()
		{
			TLButton newButton = (TLButton)WindowManager.Instance.CreateWindow(ButtonType, name + "_auto_DecreaseButton");

			newButton.StandardImageryEnabled = false;
			newButton.CustomImageryAutoSized = true;
			newButton.AlwaysOnTop = true;

			Imageset imageSet = ImagesetManager.Instance.GetImageset(ImagesetName);
			RenderableImage image = new RenderableImage();
			image.HorizontalFormat = HorizontalImageFormat.Stretched;
			image.VerticalFormat = VerticalImageFormat.Stretched;
			image.Image = imageSet.GetImage(UpButtonNormalImageName);
			newButton.SetNormalImage(image);
			newButton.SetDisabledImage(image);

			image = new RenderableImage();
			image.HorizontalFormat = HorizontalImageFormat.Stretched;
			image.VerticalFormat = VerticalImageFormat.Stretched;
			image.Image = imageSet.GetImage(UpButtonHighlightImageName);
			newButton.SetHoverImage(image);
			newButton.SetPushedImage(image);

			return newButton;
		}

		/// <summary>
		///	create a Thumb based widget to use as the thumb for this scrollbar.
		/// </summary>
		/// <returns>A reference to a thumb widget for use in this scrollbar.</returns>

		protected override Thumb CreateThumb()
		{
			Thumb newThumb = (Thumb)WindowManager.Instance.CreateWindow(ThumbType, name + "_auto_Thumb");

			newThumb.Vertical = true;
			newThumb.X = ThumbPositionX;
			newThumb.Width = ThumbWidth;

			return newThumb;
		}

		/// <summary>
		/// Layout the scrollbar component widgets.
		/// </summary>
		protected override void LayoutComponentWidgets()
		{
			// button sizes are the width of the scrollbar and square.
			SizeF bsz = new SizeF(AbsoluteToRelativeX(absArea.Width), AbsoluteToRelativeY(absArea.Width));
			
			// install sizes into buttons
			increaseButton.Size = bsz;
			decreaseButton.Size = bsz;
			
			// position buttons
			Point pos = new Point(0.0f, 0.0f);
			decreaseButton.Position = pos;

			pos.y = AbsoluteToRelativeY(absArea.Height) - bsz.Height;
			increaseButton.Position = pos;

			// configure the thumb
			UpdateThumb();
		}

		/// <summary>
		/// Update the size and position of the thumb to best represent the current state of the scrollbar.
		/// </summary>
		protected override void UpdateThumb()
		{
			// calculate padding value to use to account for buttons.
			float slideTrackYPadding = decreaseButton.AbsoluteHeight;

			// calculate maximum extents for thumb positioning.
			float posExtent		= documentSize - pageSize;
			float slideExtent	= Math.Max(0.0f, absArea.Height - (2 * slideTrackYPadding) - thumb.AbsoluteHeight);

			// Thumb does not change size with document length, we just need to update position and range
			thumb.SetVerticalRange(AbsoluteToRelativeY(slideTrackYPadding), AbsoluteToRelativeY(slideTrackYPadding + slideExtent));
			thumb.Y = AbsoluteToRelativeY(slideTrackYPadding + (position * (slideExtent / posExtent)));
		}

		/// <summary>
		/// Return value that best represents current scroll position given the current location of the thumb.
		/// </summary>
		/// <returns>float value that, given the thumb widget position, best represents the current scroll position for the scrollbar.</returns>
		protected override float GetPositionFromThumb()
		{
			// calculate padding value to use to account for buttons.
			float slideTrackYPadding = decreaseButton.AbsoluteHeight;

			// calculate maximum extents for thumb positioning.
			float posExtent		= documentSize - pageSize;
			float slideExtent	= Math.Max(0.0f, absArea.Height - (2 * slideTrackYPadding) - thumb.AbsoluteHeight);

			return	(thumb.AbsoluteY - slideTrackYPadding) / (slideExtent / posExtent);
		}
	
		/// <summary>
		///	Given window location <paramref name="point"/>, return a value indicating what change should be 
		///	made to the scroll position.
		/// </summary>
		/// <param name="point">Point describing a pixel position in window space.</param>
		/// <returns>
		///	- -1 to indicate scroll position should be moved to a lower setting.
		///	-  0 to indicate scroll position should not be moved.
		///	- +1 to indicate scroll position should be moved to a higher setting.
		/// </returns>
		protected override float GetAdjustDirectionFromPoint(Point point)
		{
			Rect absRect = thumb.UnclippedPixelRect;

			if (point.y < absRect.Top) {
				return -1;
			}
			else if (point.y > absRect.Bottom) {
				return 1;
			}
			else {
				return 0;
			}
		}

		/// <summary>
		///		Perform the actual rendering for this Scrollbar widget.
		/// </summary>
		/// <param name="z">float value specifying the base Z co-ordinate that should be used when rendering.</param>
		protected override void DrawSelf(float z) 
		{
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

			//
			// Render bar body
			//
			float buttonHeight = decreaseButton.AbsoluteHeight;

			Vector3 pos = new Vector3(absRect.Left + (absRect.Width * BodyPositionX), absRect.Top + buttonHeight, z);
			SizeF sz = new SizeF(absRect.Width * BodyWidth, absRect.Height - (buttonHeight * 0.5f));

			bodyImage.Draw(pos, sz, clipper, colors);
		}

		#endregion
	}
}
