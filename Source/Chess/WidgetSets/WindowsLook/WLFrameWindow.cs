using System;
using System.Drawing;
using CeGuiSharp;
using CeGuiSharp.Widgets;

namespace CeGuiSharp.WidgetSets.WindowsLook {

	/// <summary>
	/// 
	/// </summary>
	public class WLFrameWindow : FrameWindow
	{
		#region Constants

		/// <summary>
		/// 
		/// </summary>
		protected const string	ImagesetName					= "WindowsLook";
		/// <summary>
		/// 
		/// </summary>
		protected const string	TopLeftFrameImageName			= "WindowFrameTopLeft";
		/// <summary>
		/// 
		/// </summary>
		protected const string	TopRightFrameImageName			= "WindowFrameTopRight";
		/// <summary>
		/// 
		/// </summary>
		protected const string	BottomLeftFrameImageName		= "WindowFrameBottomLeft";
		/// <summary>
		/// 
		/// </summary>
		protected const string	BottomRightFrameImageName		= "WindowFrameBottomRight";
		/// <summary>
		/// 
		/// </summary>
		protected const string	LeftFrameImageName				= "WindowFrameLeft";
		/// <summary>
		/// 
		/// </summary>
		protected const string	RightFrameImageName				= "WindowFrameRight";
		/// <summary>
		/// 
		/// </summary>
		protected const string	TopFrameImageName				= "WindowFrameTop";
		/// <summary>
		/// 
		/// </summary>
		protected const string	BottomFrameImageName			= "WindowFrameBottom";
		/// <summary>
		/// 
		/// </summary>
		protected const string	ClientBrushImageName			= "Background";

		/// <summary>
		/// 
		/// </summary>
		protected const string	CloseButtonNormalImageName		= "CloseButtonNormal";
		/// <summary>
		/// 
		/// </summary>
		protected const string	CloseButtonHoverImageName		= "CloseButtonHover";
		/// <summary>
		/// 
		/// </summary>
		protected const string	CloseButtonPushedImageName		= "CloseButtonPushed";

		// cursor images
		/// <summary>
		/// 
		/// </summary>
		protected const string	NormalCursorImageName			= "MouseArrow";
		/// <summary>
		/// 
		/// </summary>
		protected const string	NorthSouthCursorImageName		= "MouseNoSoCursor";
		/// <summary>
		/// 
		/// </summary>
		protected const string	EastWestCursorImageName			= "MouseEsWeCursor";
		/// <summary>
		/// 
		/// </summary>
		protected const string	NWestSEastCursorImageName		= "MouseNwSeCursor";
		/// <summary>
		/// 
		/// </summary>
		protected const string	NEastSWestCursorImageName		= "MouseNeSwCursor";

		// window type stuff
		/// <summary>
		/// 
		/// </summary>
		protected const string	TitlebarType		= "WindowsLook/Titlebar";
		/// <summary>
		/// 
		/// </summary>
		protected const string	CloseButtonType		= "WindowsLook/CloseButton";

		// layout constants
		/// <summary>
		/// 
		/// </summary>
		protected const float	TitlebarXOffset			= 0;
		/// <summary>
		/// 
		/// </summary>
		protected const float	TitlebarYOffset			= 0;
		/// <summary>
		/// 
		/// </summary>
		protected const float	TitlebarTextPadding		= 12;

		/// <summary>
		/// 
		/// </summary>
		protected static Color ActiveColor = new Color( 0xFFA7C7FF );
		/// <summary>
		/// 
		/// </summary>
		protected static Color InactiveColor = new Color( 0xFFEFEFEF );
		/// <summary>
		/// 
		/// </summary>
		protected static Color ClientTopLeftColor = new Color(0xFFDFDFF5);
		/// <summary>
		/// 
		/// </summary>
		protected static Color ClientTopRightColor = new Color(0xFFDFEFF5);
		/// <summary>
		/// 
		/// </summary>
		protected static Color ClientBottomLeftColor = new Color(0xFFF4F3F5);
		/// <summary>
		/// 
		/// </summary>
		protected static Color ClientBottomRightColor = new Color(0xFFF0F0F5);

		#endregion

		#region Fields

		/// <summary>
		///		Handles the frame for the window.
		/// </summary>
		protected RenderableFrame frame = new RenderableFrame();
		/// <summary>
		///		Handles the client clearing brush for the window.
		/// </summary>
		protected RenderableImage clientBrush = new RenderableImage();

		/// <summary>
		///		Width of the left frame edge in pixels.
		/// </summary>
		protected float frameLeftSize;
		/// <summary>
		///		Width of the right frame edge in pixels.
		/// </summary>
		protected float frameRightSize;
		/// <summary>
		///		Height of the top frame edge in pixels.
		/// </summary>
		protected float frameTopSize;
		/// <summary>
		///		Height of the bottom frame edge in pixels.
		/// </summary>
		protected float frameBottomSize;

		#endregion

		#region Constructor

		/// <summary>
		/// 
		/// </summary>
		/// <param name="name"></param>
		public WLFrameWindow( string type, string name ) : base(type, name)
		{
			Imageset imageSet = ImagesetManager.Instance.GetImageset( ImagesetName );

			// setup frame images
			frame.SetImages(
				imageSet.GetImage(TopLeftFrameImageName),
				imageSet.GetImage(TopRightFrameImageName),
				imageSet.GetImage(BottomLeftFrameImageName),
				imageSet.GetImage(BottomRightFrameImageName),
				imageSet.GetImage(LeftFrameImageName),
				imageSet.GetImage(TopFrameImageName),
				imageSet.GetImage(RightFrameImageName),
				imageSet.GetImage(BottomFrameImageName) );

			StoreFrameSizes();

			// setup client area clearing brush
			clientBrush.Image = imageSet.GetImage(ClientBrushImageName);
			clientBrush.Position = new Point(frameLeftSize, frameTopSize);
			clientBrush.HorizontalFormat = HorizontalImageFormat.Stretched;
			clientBrush.VerticalFormat = VerticalImageFormat.Stretched;
			clientBrush.SetColors( ClientTopLeftColor, ClientTopRightColor,
										  ClientBottomLeftColor, ClientBottomRightColor );

			// setup cursor images for this window
			SetMouseCursor(imageSet.GetImage(NormalCursorImageName));
			sizingCursorNS = imageSet.GetImage(NorthSouthCursorImageName);
			sizingCursorEW = imageSet.GetImage(EastWestCursorImageName);
			sizingCursorNWSE = imageSet.GetImage(NWestSEastCursorImageName);
			sizingCursorNESW = imageSet.GetImage(NEastSWestCursorImageName);
		}

		#endregion

		#region Methods

		/// <summary>
		///		Store the sizes for the frame edges.
		/// </summary>
		protected void StoreFrameSizes() {
			Imageset imageSet = ImagesetManager.Instance.GetImageset(ImagesetName);

			frameLeftSize	= imageSet.GetImage(LeftFrameImageName).Width;
			frameRightSize	= imageSet.GetImage(RightFrameImageName).Width;
			frameTopSize	= imageSet.GetImage(TopFrameImageName).Height;
			frameBottomSize = imageSet.GetImage(BottomFrameImageName).Height;
		}

		/// <summary>
		/// 
		/// </summary>
		protected void UpdateFrameColors() {
			Color color = IsActive ? ActiveColor : InactiveColor;
			frame.SetColors( new ColorRect( color ) );
			frame.Colors.SetAlpha( EffectiveAlpha );
		}

		#endregion

		#region Base Members

		#region Properties
		#endregion

		#region Methods

		/// <summary>
		///		Return a Rect object that describes, unclipped, the inner rectangle for this window.	
		/// </summary>
		public override Rect UnclippedInnerRect {
			get {
				Rect tempRect = UnclippedPixelRect;
				
				if(FrameEnabled) {
					Point pos = frame.Position;

					tempRect.Left += pos.x + frameLeftSize;
					tempRect.Right -= frameRightSize;
					tempRect.Top += pos.y + frameTopSize;
					tempRect.Bottom -= frameBottomSize;
				}

				return tempRect;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public override PushButton CreateCloseButton() {
			WLCloseButton button = (WLCloseButton)WindowManager.Instance.CreateWindow(
				"WindowsLook.WLCloseButton", name + "_auto_PushButton");

			button.StandardImageryEnabled = false;
			button.CustomImageryAutoSized = true;

			Imageset imageSet = ImagesetManager.Instance.GetImageset( ImagesetName );

			// setup close button imagery
			RenderableImage image = new RenderableImage();
			image.HorizontalFormat = HorizontalImageFormat.Stretched;
			image.VerticalFormat = VerticalImageFormat.Stretched;
			image.Image = imageSet.GetImage( CloseButtonNormalImageName );
			image.SetColors( new ColorRect( new Color(0xFFFFFFFF) ) );
			button.SetNormalImage( image );

			image = new RenderableImage();
			image.HorizontalFormat = HorizontalImageFormat.Stretched;
			image.VerticalFormat = VerticalImageFormat.Stretched;
			image.Image = imageSet.GetImage( CloseButtonNormalImageName );
			image.SetColors( new ColorRect( new Color(0x7F3FAFAF) ) );
			button.SetDisabledImage( image );

			image = new RenderableImage();
			image.HorizontalFormat = HorizontalImageFormat.Stretched;
			image.VerticalFormat = VerticalImageFormat.Stretched;
			image.Image = imageSet.GetImage( CloseButtonHoverImageName );
			image.SetColors( new ColorRect( new Color(0xFFFFFFFF) ) );
			button.SetHoverImage( image );

			image = new RenderableImage();
			image.HorizontalFormat = HorizontalImageFormat.Stretched;
			image.VerticalFormat = VerticalImageFormat.Stretched;
			image.Image = imageSet.GetImage( CloseButtonPushedImageName );
			image.SetColors( new ColorRect( new Color(0xFFFFFFFF) ) );
			button.SetPushedImage( image );

			button.Alpha = 0.5f;
			button.MetricsMode = MetricsMode.Absolute;
			button.AlwaysOnTop = true;

			return button;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public override TitleBar CreateTitleBar() {
			// create a titlebar to use for this frame window
			TitleBar window = (TitleBar)WindowManager.Instance.CreateWindow(
				"WindowsLook.WLTitleBar", name + "_auto_Titlebar");

			window.MetricsMode = MetricsMode.Absolute;
			window.Position = new Point(TitlebarXOffset, TitlebarYOffset);
			
			return window;
		}

		/// <summary>
		/// 
		/// </summary>
		public override void LayoutComponentWidgets() {
			// set the size of the titlebar
			SizeF titleSize = new SizeF();
			titleSize.Height = titleBar.Font.LineSpacing + TitlebarTextPadding;
			titleSize.Width = this.IsRolledUp ? absOpenSize.Width : absArea.Width;

			titleBar.Size = titleSize;

			// set the size of the close button
			float closeSize = titleSize.Height * 0.75f;
			closeButton.Size = new SizeF(closeSize, closeSize);

			float closeX = titleSize.Width - closeSize - titleSize.Height * 0.125f;
			float closeY = TitlebarYOffset + ((titleSize.Height - closeSize) / 2);
			closeButton.Position = new Point(closeX, closeY);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="z"></param>
		protected override void DrawSelf(float z) {
			// get the destination screen rect for this window
			Rect absRect = UnclippedPixelRect;

			CeGuiSharp.Vector3 pos =
                new CeGuiSharp.Vector3(absRect.Left, absRect.Top, z);
			
			clientBrush.Draw(pos, InnerRect);

			if(FrameEnabled) {
				frame.Draw(pos, PixelRect);
			}
		}

		#region Overriden Event Trigger Methods
		
		/// <summary>
		/// 
		/// </summary>
		/// <param name="e"></param>
		protected override void OnSized(GuiEventArgs e) {
			// MUST call base class handler no matter what.  This is now required 100%
			base.OnSized (e);

			Rect area = UnclippedPixelRect;
			SizeF newSize = new SizeF(area.Width, area.Height);

			// adjust frame and client area rendering objects so that the title bar and close button appear half in and half-out of the frame.
			float frameOffset = 0;

			// if title bar is active, close button is the same height.
			if(TitleBarEnabled) {
				frameOffset = titleBar.UnclippedPixelRect.Height;
			}// if no title bar, measure the close button instead.
			else if(CloseButtonEnabled) {
				frameOffset = closeButton.UnclippedPixelRect.Height;
			}

			// move frame into position
			Point pos = new Point(0, frameOffset);
			frame.Position = pos;

			// adjust the size of the frame
			newSize.Height -= frameOffset;
			frame.Size = newSize;

			// adjust position for client brush
			pos.y += (TitleBarEnabled || FrameEnabled) ? 0 : frameTopSize;

			// modify size of client so it is within the frame
			if( FrameEnabled ) {
				pos.x += frameLeftSize;
				newSize.Width -= (frameLeftSize + frameRightSize);
				newSize.Height -= frameBottomSize;

				if( TitleBarEnabled ) {
					newSize.Height -= frameTopSize;
				}
			}

			clientBrush.Size = newSize;
			clientBrush.Position = pos;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="e"></param>
		protected override void OnAlphaChanged(GuiEventArgs e) {
			base.OnAlphaChanged(e);

			frame.Colors.SetAlpha( EffectiveAlpha );
			clientBrush.Colors.SetAlpha( EffectiveAlpha );
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="e"></param>
		protected override void OnActivated(WindowEventArgs e) {
			base.OnActivated(e);
			UpdateFrameColors();
			closeButton.Alpha = 1.0f;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="e"></param>
		protected override void OnDeactivated(WindowEventArgs e) {
			base.OnDeactivated(e);
			UpdateFrameColors();
			closeButton.Alpha = 0.5f;
		}

		#endregion

		#endregion

		#endregion

	}

}
