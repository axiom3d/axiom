using System;
using System.Drawing;
using CeGuiSharp;
using CeGuiSharp.Widgets;

namespace CeGuiSharp.WidgetSets.WindowsLook {

	/// <summary>
	/// 
	/// </summary>
	public class WLVerticalScrollbar : Scrollbar
	{
		#region Constants

		// image name constants
		/// <summary>
		/// 
		/// </summary>
		protected const string	ImagesetName					= "WindowsLook";
		/// <summary>
		/// 
		/// </summary>
		protected const string	TopLeftFrameImageName		= "StaticFrameTopLeft";
		/// <summary>
		/// 
		/// </summary>
		protected const string	TopRightFrameImageName		= "StaticFrameTopRight";
		/// <summary>
		/// 
		/// </summary>
		protected const string	BottomLeftFrameImageName	= "StaticFrameBottomLeft";
		/// <summary>
		/// 
		/// </summary>
		protected const string	BottomRightFrameImageName	= "StaticFrameBottomRight";
		/// <summary>
		/// 
		/// </summary>
		protected const string	LeftFrameImageName			= "StaticFrameLeft";
		/// <summary>
		/// 
		/// </summary>
		protected const string	RightFrameImageName			= "StaticFrameRight";
		/// <summary>
		/// 
		/// </summary>
		protected const string	TopFrameImageName				= "StaticFrameTop";
		/// <summary>
		/// 
		/// </summary>
		protected const string	BottomFrameImageName			= "StaticFrameBottom";
		/// <summary>
		/// 
		/// </summary>
		protected const string	BackgroundImageName			= "Background";
		/// <summary>
		/// 
		/// </summary>
		protected const string	UpButtonNormalImageName		= "LargeUpArrow";
		/// <summary>
		/// 
		/// </summary>
		protected const string	UpButtonHighlightImageName	= "LargeUpArrow";
		/// <summary>
		/// 
		/// </summary>
		protected const string	DownButtonNormalImageName	= "LargeDownArrow";
		/// <summary>
		/// 
		/// </summary>
		protected const string	DownButtonHighlightImageName	= "LargeDownArrow";
		/// <summary>
		/// 
		/// </summary>
		protected const string	MouseCursorImageName				= "MouseArrow";

		// Colours
		/// <summary>
		/// 
		/// </summary>
		protected static Color BackgroundColor		= new Color(0xDFDFDF);

		// some layout stuff
		/// <summary>
		/// 
		/// </summary>
		protected const float	MinThumbHeight			= 10.0f;

		// type names for the component widgets
		/// <summary>
		/// 
		/// </summary>
		protected const string	ThumbWidgetType			= "WindowsLook.WLVerticalScrollbarThumb";
		/// <summary>
		/// 
		/// </summary>
		protected const string	IncreaseButtonWidgetType	= "WindowsLook.WLButton";
		/// <summary>
		/// 
		/// </summary>
		protected const string	DecreaseButtonWidgetType	= "WindowsLook.WLButton";

		#endregion

		#region Fields

		/// <summary>
		/// 
		/// </summary>
		protected RenderableFrame frame = new RenderableFrame();

		/// <summary>
		/// 
		/// </summary>
		protected Image background;

		// frame image spacing
		/// <summary>
		/// 
		/// </summary>
		protected float frameLeftSize;
		/// <summary>
		/// 
		/// </summary>
		protected float frameTopSize;
		/// <summary>
		/// 
		/// </summary>
		protected float frameRightSize;
		/// <summary>
		/// 
		/// </summary>
		protected float frameBottomSize;

		#endregion

		#region Constructor

		/// <summary>
		/// 
		/// </summary>
		/// <param name="name"></param>
		public WLVerticalScrollbar( string type, string name ) : base(type, name) {
			Imageset imageSet = ImagesetManager.Instance.GetImageset(ImagesetName);

			frame.SetImages(
				imageSet.GetImage(TopLeftFrameImageName),
				imageSet.GetImage(TopRightFrameImageName),
				imageSet.GetImage(BottomLeftFrameImageName),
				imageSet.GetImage(BottomRightFrameImageName),
				imageSet.GetImage(LeftFrameImageName),
				imageSet.GetImage(TopFrameImageName),
				imageSet.GetImage(RightFrameImageName),
				imageSet.GetImage(BottomFrameImageName) );

			background = imageSet.GetImage(BackgroundImageName);

			SetMouseCursor(imageSet.GetImage(MouseCursorImageName));

			StoreFrameSizes();
		}

		#endregion

		#region Methods

		/// <summary>
		/// 
		/// </summary>
		protected void StoreFrameSizes() {
			Imageset imageSet = ImagesetManager.Instance.GetImageset(ImagesetName);

			frameLeftSize = imageSet.GetImage(LeftFrameImageName).Width;
			frameRightSize = imageSet.GetImage(RightFrameImageName).Width;
			frameTopSize = imageSet.GetImage(TopFrameImageName).Height;
			frameBottomSize = imageSet.GetImage(BottomFrameImageName).Height;
		}

		#endregion

		#region Scrollbar Members

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		protected override PushButton CreateIncreaseButton() {
			// create the widget
			WLButton button = (WLButton)WindowManager.Instance.CreateWindow(IncreaseButtonWidgetType, Name + "__auto_incbtn__");

			// perform some initialization
			button.StandardImageryEnabled = true;
			button.CustomImageryAutoSized = true;
			button.AlwaysOnTop = true;

			Imageset imageSet = ImagesetManager.Instance.GetImageset(ImagesetName);
			RenderableImage image = new RenderableImage();
			image.HorizontalFormat = HorizontalImageFormat.Centered;
			image.VerticalFormat = VerticalImageFormat.Centered;
			image.Image = imageSet.GetImage(DownButtonNormalImageName);
			button.SetNormalImage(image);
			button.SetDisabledImage(image);

			image = new RenderableImage();
			image.HorizontalFormat = HorizontalImageFormat.Centered;
			image.VerticalFormat = VerticalImageFormat.Centered;
			image.Image = imageSet.GetImage(DownButtonHighlightImageName);
			button.SetHoverImage(image);
			button.SetPushedImage(image);

			return button;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		protected override PushButton CreateDecreaseButton() {
			// create the widget
			WLButton button = (WLButton)WindowManager.Instance.CreateWindow(DecreaseButtonWidgetType, Name + "__auto_decbtn__");

			// perform some initialization
			button.StandardImageryEnabled = true;
			button.CustomImageryAutoSized = true;
			button.AlwaysOnTop = true;

			Imageset imageSet = ImagesetManager.Instance.GetImageset(ImagesetName);
			RenderableImage image = new RenderableImage();
			image.HorizontalFormat = HorizontalImageFormat.Centered;
			image.VerticalFormat = VerticalImageFormat.Centered;
			image.Image = imageSet.GetImage(UpButtonNormalImageName);
			button.SetNormalImage(image);
			button.SetDisabledImage(image);

			image = new RenderableImage();
			image.HorizontalFormat = HorizontalImageFormat.Centered;
			image.VerticalFormat = VerticalImageFormat.Centered;
			image.Image = imageSet.GetImage(UpButtonHighlightImageName);
			button.SetHoverImage(image);
			button.SetPushedImage(image);

			return button;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		protected override Thumb CreateThumb() {
			// create the widget
			Thumb thumb = (Thumb)WindowManager.Instance.CreateWindow(ThumbWidgetType, Name + "__auto_thumb__");

			// perform initialization
			thumb.Vertical = true;

			return thumb;
		}

		/// <summary>
		/// 
		/// </summary>
		protected override void LayoutComponentWidgets() {
			// button sizes are the height of the scrollbar and square.
			SizeF bsz = new SizeF();
			bsz.Width = absArea.Width - (frameLeftSize - frameRightSize);
			bsz.Height = bsz.Width;

			// install button sizes
			increaseButton.Size = AbsoluteToRelative(bsz);
			decreaseButton.Size = AbsoluteToRelative(bsz);
			
			// position buttons
			Point pos = new Point(frameLeftSize, frameTopSize);
			decreaseButton.Position = AbsoluteToRelative(pos);

			pos.y = absArea.Height - bsz.Height - frameBottomSize;
			increaseButton.Position = AbsoluteToRelative(pos);

			// this will configure the thumb widget appropriately
			UpdateThumb();
		}

		/// <summary>
		/// 
		/// </summary>
		protected override void UpdateThumb() {
			// calculate actual padding values to use
			float slideTrackYPadding = decreaseButton.AbsoluteHeight + frameTopSize;

			// calculate maximum extents for thumb positioning.
			float posExtent		= documentSize - pageSize;
			float slideExtent = Math.Max(0.0f, absArea.Height  - (2 * slideTrackYPadding));
			float thumbHeight = (documentSize <= pageSize) ? slideExtent : slideExtent * pageSize / documentSize;
			slideExtent -= thumbHeight;

			// make sure thumb is not too small
			if(thumbHeight < MinThumbHeight) {
				thumbHeight = MinThumbHeight;
			}

			//thumb.AbsoluteSize = new Size(increaseButton.AbsoluteWidth, thumb.Height);
			thumb.Size = AbsoluteToRelative(new SizeF(increaseButton.AbsoluteWidth, thumbHeight));
			thumb.SetVerticalRange(AbsoluteToRelativeY(slideTrackYPadding), AbsoluteToRelativeY(slideTrackYPadding + slideExtent));
			thumb.X =  AbsoluteToRelativeX(frameLeftSize);
			thumb.Y =  AbsoluteToRelativeY(slideTrackYPadding + (position * (slideExtent / posExtent)));
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		protected override float GetPositionFromThumb() {
			// calculate actual padding values to use.
			float slideTrackYPadding = decreaseButton.AbsoluteHeight + frameTopSize;

			//calculate maximum extents for thumb positioning.
			float posExtent = documentSize - pageSize;
			float sliderExtent = Math.Max(0.0f, absArea.Height - (2 * slideTrackYPadding) - thumb.AbsoluteHeight);

			return (thumb.AbsoluteY - slideTrackYPadding) / (sliderExtent / posExtent);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="point"></param>
		/// <returns></returns>
		protected override float GetAdjustDirectionFromPoint(Point point) {
			Rect absRect = UnclippedPixelRect;

			if(point.y < absRect.Top) {
				return -1.0f;
			}
			else if(point.y > absRect.Bottom) {
				return 1.0f;
			}
			else {
				return 0.0f;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="z"></param>
		protected override void DrawSelf(float z) 
		{
			Rect clipper = PixelRect;

			// do nothing if the widget is totally clipped.
			if (clipper.Width == 0) {
				return;
			}

			// get the destination screen rect for this window
			Rect absRect = UnclippedPixelRect;

			// calculate colors to use
			ColorRect colors = new ColorRect(BackgroundColor);
			colors.SetAlpha(EffectiveAlpha);

			// draw background image
			background.Draw(absRect, z, clipper, colors);

			// draw container
			Vector3 pos = new Vector3(absRect.Left, absRect.Top, z);
			frame.Draw(pos, clipper);
		}

		#endregion

		#region Window Members

		/// <summary>
		/// 
		/// </summary>
		/// <param name="e"></param>
		protected override void OnSized(GuiEventArgs e) {
			base.OnSized(e);

			frame.Size = AbsoluteSize;

			e.Handled = true;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="e"></param>
		protected override void OnAlphaChanged(GuiEventArgs e) {
			base.OnAlphaChanged(e);

			frame.Colors.SetAlpha(EffectiveAlpha);
		}

		#endregion
	}
}
