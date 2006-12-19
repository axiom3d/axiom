using System;
using System.Drawing;
using CeGuiSharp;
using CeGuiSharp.Widgets;

namespace CeGuiSharp.WidgetSets.TaharezLook 
{
	/// <summary>
	/// Mini horizontal scrollbar widget for the Taharez Gui Scheme.
	/// </summary>
	public class TLMiniHorizontalScrollbar : Scrollbar
	{
		#region Constants
		/// <summary>
		///		Name of the Imageset to use for rendering.
		/// </summary>
		const string ImagesetName = "TaharezLook";
		/// <summary>
		///		Name of image to use for the main body of the scroll bar
		/// </summary>
		const string ScrollbarBodyImageName = "MiniHorzScrollBarSegment";
		/// <summary>
		///		Name of image to use for the left button in normal state.
		/// </summary>
		const string LeftButtonNormalImageName = "MiniHorzScrollLeftNormal";
		/// <summary>
		///		Name of image to use for the left button in highlighted state.
		/// </summary>
		const string LeftButtonHighlightImageName = "MiniHorzScrollLeftHover";
		/// <summary>
		///		Name of image to use for the right button in normal state.
		/// </summary>
		const string RightButtonNormalImageName = "MiniHorzScrollRightNormal";
		/// <summary>
		///		Name of image to use for the right button in the highlighted state.
		/// </summary>
		const string RightButtonHighlightImageName = "MiniHorzScrollRightHover";

		/// <summary>
		/// Relative Y co-ordinate for the thumb.
		/// </summary>
		const float ThumbPositionY = 0.15f;
		/// <summary>
		/// Relative height of the thumb.
		/// </summary>
		const float ThumbHeight = 0.7f;
		/// <summary>
		/// Relative Y co-ordinate for the body imagery.
		/// </summary>
		const float BodyPositionY = 0.3f;
		/// <summary>
		/// Relative height for the body imagery.
		/// </summary>
		const float BodyHeight = 0.4f;
		
		
		/// <summary>
		///		Type of the thumb to create.
		/// </summary>
		const string ThumbType			= "TaharezLook.TLMiniHorizontalScrollbarThumb";
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
		public TLMiniHorizontalScrollbar(string type, string name) : base(type, name) 
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
			image.Image = imageSet.GetImage(RightButtonNormalImageName);
			newButton.SetNormalImage(image);
			newButton.SetDisabledImage(image);

			image = new RenderableImage();
			image.HorizontalFormat = HorizontalImageFormat.Stretched;
			image.VerticalFormat = VerticalImageFormat.Stretched;
			image.Image = imageSet.GetImage(RightButtonHighlightImageName);
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
			image.Image = imageSet.GetImage(LeftButtonNormalImageName);
			newButton.SetNormalImage(image);
			newButton.SetDisabledImage(image);

			image = new RenderableImage();
			image.HorizontalFormat = HorizontalImageFormat.Stretched;
			image.VerticalFormat = VerticalImageFormat.Stretched;
			image.Image = imageSet.GetImage(LeftButtonHighlightImageName);
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

			newThumb.Horizontal = true;
			newThumb.Y = ThumbPositionY;
			newThumb.Height = ThumbHeight;

			return newThumb;
		}

		/// <summary>
		/// Layout the scrollbar component widgets.
		/// </summary>
		protected override void LayoutComponentWidgets()
		{
			// button sizes are the height of the scrollbar and square.
			SizeF bsz = new SizeF(AbsoluteToRelativeX(absArea.Height), AbsoluteToRelativeY(absArea.Height));
			
			// install sizes into buttons
			increaseButton.Size = bsz;
			decreaseButton.Size = bsz;
			
			// position buttons
			Point pos = new Point(0.0f, 0.0f);
			decreaseButton.Position = pos;

			pos.x = AbsoluteToRelativeX(absArea.Width) - bsz.Width;
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
			float slideTrackXPadding = decreaseButton.AbsoluteWidth;

			// calculate maximum extents for thumb positioning.
			float posExtent		= documentSize - pageSize;
			float slideExtent	= Math.Max(0.0f, absArea.Width - (2 * slideTrackXPadding) - thumb.AbsoluteWidth);

			// Thumb does not change size with document length, we just need to update position and range
			thumb.SetHorizontalRange(AbsoluteToRelativeX(slideTrackXPadding), AbsoluteToRelativeX(slideTrackXPadding + slideExtent));
			thumb.X = AbsoluteToRelativeX(slideTrackXPadding + (position * (slideExtent / posExtent)));
		}

		/// <summary>
		/// Return value that best represents current scroll position given the current location of the thumb.
		/// </summary>
		/// <returns>float value that, given the thumb widget position, best represents the current scroll position for the scrollbar.</returns>
		protected override float GetPositionFromThumb()
		{
			// calculate padding value to use to account for buttons.
			float slideTrackXPadding = decreaseButton.AbsoluteWidth;

			// calculate maximum extents for thumb positioning.
			float posExtent		= documentSize - pageSize;
			float slideExtent	= absArea.Width - (2 * slideTrackXPadding) - thumb.AbsoluteWidth;

			return	(thumb.AbsoluteX - slideTrackXPadding) / (slideExtent / posExtent);
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

			if (point.x < absRect.Left) {
				return -1;
			}
			else if (point.x > absRect.Right) {
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
			float buttonWidth = decreaseButton.AbsoluteWidth;

			Vector3 pos = new Vector3(absRect.Left + buttonWidth, absRect.Top + (absRect.Height * BodyPositionY), z);
			SizeF sz = new SizeF(absRect.Width - (buttonWidth * 0.5f), absRect.Height * BodyHeight);

			bodyImage.Draw(pos, sz, clipper, colors);
		}

		#endregion
	}
}
