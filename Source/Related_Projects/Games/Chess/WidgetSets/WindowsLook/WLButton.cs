using System;
using System.Drawing;
using CeGuiSharp;
using CeGuiSharp.Widgets;

namespace CeGuiSharp.WidgetSets.WindowsLook {

	/// <summary>
	/// 
	/// </summary>
	public class WLButton : PushButton {
		#region Constants

		/// <summary>
		///		Name of the imageset to use for rendering.
		/// </summary>
		protected const string	ImagesetName					= "WindowsLook";
		/// <summary>
		/// 
		/// </summary>
		protected const string	BackgroundImageName			= "Background";
		
		/// <summary>
		/// 
		/// </summary>
		protected const string	NormalLeftImageName			= "ButtonNormalLeft";
		/// <summary>
		/// 
		/// </summary>
		protected const string	NormalRightImageName			= "ButtonNormalRight";
		/// <summary>
		/// 
		/// </summary>
		protected const string	NormalTopImageName			= "ButtonNormalTop";
		/// <summary>
		/// 
		/// </summary>
		protected const string	NormalBottomImageName		= "ButtonNormalBottom";
		/// <summary>
		/// 
		/// </summary>
		protected const string	NormalTopLeftImageName		= "ButtonNormalTopLeft";
		/// <summary>
		/// 
		/// </summary>
		protected const string	NormalTopRightImageName		= "ButtonNormalTopRight";
		/// <summary>
		/// 
		/// </summary>
		protected const string	NormalBottomLeftImageName	= "ButtonNormalBottomLeft";
		/// <summary>
		/// 
		/// </summary>
		protected const string	NormalBottomRightImageName	= "ButtonNormalBottomRight";
		
		/// <summary>
		/// 
		/// </summary>
		protected const string	HoverLeftImageName			= "ButtonHoverLeft";
		/// <summary>
		/// 
		/// </summary>
		protected const string	HoverRightImageName			= "ButtonHoverRight";
		/// <summary>
		/// 
		/// </summary>
		protected const string	HoverTopImageName				= "ButtonHoverTop";
		/// <summary>
		/// 
		/// </summary>
		protected const string	HoverBottomImageName			= "ButtonHoverBottom";
		/// <summary>
		/// 
		/// </summary>
		protected const string	HoverTopLeftImageName		= "ButtonHoverTopLeft";
		/// <summary>
		/// 
		/// </summary>
		protected const string	HoverTopRightImageName		= "ButtonHoverTopRight";
		/// <summary>
		/// 
		/// </summary>
		protected const string	HoverBottomLeftImageName	= "ButtonHoverBottomLeft";
		/// <summary>
		/// 
		/// </summary>
		protected const string	HoverBottomRightImageName	= "ButtonHoverBottomRight";

		/// <summary>
		/// 
		/// </summary>
		protected const string	PushedLeftImageName			= "ButtonPushedLeft";
		/// <summary>
		/// 
		/// </summary>
		protected const string	PushedRightImageName		= "ButtonPushedRight";
		/// <summary>
		/// 
		/// </summary>
		protected const string	PushedTopImageName			= "ButtonPushedTop";
		/// <summary>
		/// 
		/// </summary>
		protected const string	PushedBottomImageName		= "ButtonPushedBottom";
		/// <summary>
		/// 
		/// </summary>
		protected const string	PushedTopLeftImageName		= "ButtonPushedTopLeft";
		/// <summary>
		/// 
		/// </summary>
		protected const string	PushedTopRightImageName		= "ButtonPushedTopRight";
		/// <summary>
		/// 
		/// </summary>
		protected const string	PushedBottomLeftImageName	= "ButtonPushedBottomLeft";
		/// <summary>
		/// 
		/// </summary>
		protected const string	PushedBottomRightImageName	= "ButtonPushedBottomRight";

		/// <summary>
		/// 
		/// </summary>
		protected const string	MouseCursorImageName			= "MouseArrow";

		/// <summary>
		/// 
		/// </summary>
		protected static Color	NormalPrimaryColor		= new Color( 0xAFAFAF );
		/// <summary>
		/// 
		/// </summary>
		protected static Color	NormalSecondaryColor	= new Color( 0xFFFFFF );
		/// <summary>
		/// 
		/// </summary>
		protected static Color	HoverPrimaryColor		= new Color( 0xCFD9CF );
		/// <summary>
		/// 
		/// </summary>
		protected static Color	HoverSecondaryColor		= new Color( 0xF2FFF2 );
		/// <summary>
		/// 
		/// </summary>
		protected static Color	PushedPrimaryColor		= new Color( 0xAFAFAF );
		/// <summary>
		/// 
		/// </summary>
		protected static Color	PushedSecondaryColor	= new Color( 0xFFFFFF );
		/// <summary>
		/// 
		/// </summary>
		protected static Color	DisabledPrimaryColor	= new Color( 0x999999 );
		/// <summary>
		/// 
		/// </summary>
		protected static Color	DisabledSecondaryColor	= new Color( 0x999999 );
		/// <summary>
		/// 
		/// </summary>
		protected static Color	EnabledTextLabelColor	= new Color( 0x000000 );
		/// <summary>
		/// 
		/// </summary>
		protected static Color	DisabledTextLabelColor	= new Color( 0x888888 );

		/// <summary>
		/// 
		/// </summary>
		protected const int CustomImageLayer = 1;
		/// <summary>
		/// 
		/// </summary>
		protected const int LabelLayer = 2;

		#endregion

		#region Fields

		/// <summary>
		/// 
		/// </summary>
		protected bool autoscaleImages;
		/// <summary>
		/// 
		/// </summary>
		protected bool useStandardImagery;
		/// <summary>
		/// 
		/// </summary>
		protected bool useNormalImage;
		/// <summary>
		/// 
		/// </summary>
		protected bool useHoverImage;
		/// <summary>
		/// 
		/// </summary>
		protected bool usePushedImage;
		/// <summary>
		/// 
		/// </summary>
		protected bool useDisabledImage;

		/// <summary>
		/// 
		/// </summary>
		protected RenderableImage	normalImage = new RenderableImage();
		/// <summary>
		/// 
		/// </summary>
		protected RenderableImage	hoverImage = new RenderableImage();
		/// <summary>
		/// 
		/// </summary>
		protected RenderableImage	pushedImage = new RenderableImage();
		/// <summary>
		/// 
		/// </summary>
		protected RenderableImage	disabledImage = new RenderableImage();

		/// <summary>
		/// 
		/// </summary>
		protected RenderableFrame	normalFrame = new RenderableFrame();
		/// <summary>
		/// 
		/// </summary>
		protected RenderableFrame	hoverFrame = new RenderableFrame();
		/// <summary>
		/// 
		/// </summary>
		protected RenderableFrame	pushedFrame = new RenderableFrame();

		/// <summary>
		/// 
		/// </summary>
		protected float	frameLeftSize;
		/// <summary>
		/// 
		/// </summary>
		protected float	frameTopSize;
		/// <summary>
		/// 
		/// </summary>
		protected float	frameRightSize;
		/// <summary>
		/// 
		/// </summary>
		protected float	frameBottomSize;

		/// <summary>
		/// 
		/// </summary>
		protected Image		background;

		#endregion

		#region Constructor

		/// <summary>
		/// 
		/// </summary>
		/// <param name="name"></param>
		public WLButton( string type, string name ) : base(type, name)
		{
			autoscaleImages = true;
			useStandardImagery = true;
			useNormalImage = false;
			useHoverImage = false;
			usePushedImage = false;
			useDisabledImage = false;

			StoreFrameSizes();

			// setup images and frames
			Imageset imageSet = ImagesetManager.Instance.GetImageset( ImagesetName );

			normalFrame.SetImages(
				imageSet.GetImage(NormalTopLeftImageName),
				imageSet.GetImage(NormalTopRightImageName),
				imageSet.GetImage(NormalBottomLeftImageName),
				imageSet.GetImage(NormalBottomRightImageName),
				imageSet.GetImage(NormalLeftImageName),
				imageSet.GetImage(NormalTopImageName),
				imageSet.GetImage(NormalRightImageName),
				imageSet.GetImage(NormalBottomImageName) );

			hoverFrame.SetImages(
				imageSet.GetImage(HoverTopLeftImageName),
				imageSet.GetImage(HoverTopRightImageName),
				imageSet.GetImage(HoverBottomLeftImageName),
				imageSet.GetImage(HoverBottomRightImageName),
				imageSet.GetImage(HoverLeftImageName),
				imageSet.GetImage(HoverTopImageName),
				imageSet.GetImage(HoverRightImageName),
				imageSet.GetImage(HoverBottomImageName) );

			pushedFrame.SetImages(
				imageSet.GetImage(PushedTopLeftImageName),
				imageSet.GetImage(PushedTopRightImageName),
				imageSet.GetImage(PushedBottomLeftImageName),
				imageSet.GetImage(PushedBottomRightImageName),
				imageSet.GetImage(PushedLeftImageName),
				imageSet.GetImage(PushedTopImageName),
				imageSet.GetImage(PushedRightImageName),
				imageSet.GetImage(PushedBottomImageName) );

			background = imageSet.GetImage(BackgroundImageName);

			// set mouse for this widget
			SetMouseCursor( imageSet.GetImage(MouseCursorImageName) );

			// set the default colors for text
			normalColor = EnabledTextLabelColor;
			hoverColor = EnabledTextLabelColor;
			pushedColor = EnabledTextLabelColor;
			disabledColor = DisabledTextLabelColor;
		}

		#endregion

		#region Methods

		/// <summary>
		/// 
		/// </summary>
		protected void StoreFrameSizes()
		{
			Imageset imageSet = ImagesetManager.Instance.GetImageset( ImagesetName );

			frameLeftSize = imageSet.GetImage( NormalLeftImageName ).Width;
			frameRightSize = imageSet.GetImage( NormalRightImageName ).Width;
			frameTopSize = imageSet.GetImage( NormalTopImageName ).Height;
			frameBottomSize = imageSet.GetImage( NormalBottomImageName ).Height;
		}

		#endregion

		#region Base Members

		#region Properties

		/// <summary>
		///		Get/Set whether of not custom button image areas are auto-scaled to the size of the button.
		/// </summary>
		/// <value>
		///		true if client specified custom image areas are re-sized when the button size changes.  
		///		false if image areas will remain unchanged when the button is sized.
		/// </value>
		public bool CustomImageryAutoSized {
			get {
				return autoscaleImages;
			}
			set {
				// if we are enabling auto-sizing, scale images for current size
				if ((value == true) && (value != autoscaleImages)) {
					Rect area = new Rect(0, 0, absArea.Width, absArea.Height);

					normalImage.Rect = area;
					hoverImage.Rect = area;
					pushedImage.Rect = area;
					disabledImage.Rect = area;

					RequestRedraw();
				}

				autoscaleImages = value;
			}
		}

		/// <summary>
		///		Get/Set whether or not rendering of the standard imagery is enabled.
		/// </summary>
		/// <value>true if the standard button imagery will be rendered, false if no standard rendering will be performed.</value>
		public bool StandardImageryEnabled {
			get {
				return useStandardImagery;
			}
			set {
				if(useStandardImagery != value) {
					useStandardImagery = value;
					RequestRedraw();
				}
			}
		}

		#endregion

		#region Methods

		/// <summary>
		///		Set the details of the image to render for the button in the normal state.
		/// </summary>
		/// <param name="image">
		///		RenderableImage object with all the details for the image.  Note that an internal copy of the Renderable image is made and
		///		ownership of <paramref name="image"/> remains with client code.  If this parameter is NULL, rendering of an image for this 
		///		button state is disabled.
		/// </param>
		public void SetNormalImage(RenderableImage image) {
			if(image == null) {
				useNormalImage = false;
			}
			else {
				useNormalImage = true;
				normalImage = image;
			}
		}

		/// <summary>
		///		Set the details of the image to render for the button in the hover state.
		/// </summary>
		/// <param name="image">
		///		RenderableImage object with all the details for the image.  Note that an internal copy of the Renderable image is made and
		///		ownership of <paramref name="image"/> remains with client code.  If this parameter is NULL, rendering of an image for this 
		///		button state is disabled.
		/// </param>
		public void SetHoverImage(RenderableImage image) {
			if(image == null) {
				useHoverImage = false;
			}
			else {
				useHoverImage = true;
				hoverImage = image;
			}
		}

		/// <summary>
		///		Set the details of the image to render for the button in the pushed state.
		/// </summary>
		/// <param name="image">
		///		RenderableImage object with all the details for the image.  Note that an internal copy of the Renderable image is made and
		///		ownership of <paramref name="image"/> remains with client code.  If this parameter is NULL, rendering of an image for this 
		///		button state is disabled.
		/// </param>
		public void SetPushedImage(RenderableImage image) {
			if(image == null) {
				usePushedImage = false;
			}
			else {
				usePushedImage = true;
				pushedImage = image;
			}
		}

		/// <summary>
		///		Set the details of the image to render for the button in the disabled state.
		/// </summary>
		/// <param name="image">
		///		RenderableImage object with all the details for the image.  Note that an internal copy of the Renderable image is made and
		///		ownership of <paramref name="image"/> remains with client code.  If this parameter is NULL, rendering of an image for this 
		///		button state is disabled.
		/// </param>
		public void SetDisabledImage(RenderableImage image) {
			if(image == null) {
				useDisabledImage = false;
			}
			else {
				useDisabledImage = true;
				disabledImage = image;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="z"></param>
		protected override void DrawNormal(float z) {
			Rect clipper = PixelRect;

			// do nothing if the widget is totally clipped
			if( clipper.Width == 0 ) {
				return;
			}

			// get the destination screen rect for this widget
			Rect absRect = UnclippedPixelRect;
			
			// calculate the colors to use
			ColorRect colors = new ColorRect( NormalPrimaryColor,
														 NormalSecondaryColor,
														 NormalSecondaryColor,
														 NormalPrimaryColor );
			colors.SetAlpha( EffectiveAlpha );

			// render standard button imagery if required.
			if( useStandardImagery ) {
				// draw background image
				Rect bkRect = absRect;
				bkRect.Left += frameLeftSize;
				bkRect.Right -= frameRightSize;
				bkRect.Top += frameTopSize;
				bkRect.Bottom -= frameBottomSize;
				background.Draw( bkRect, z, clipper, colors );

				// draw frame
				normalFrame.Draw( new Vector3(absRect.Left, absRect.Top, z), clipper );
			}

			// render clients custom image if that is required.
			if( useNormalImage ) {
				normalImage.Colors.SetAlpha( EffectiveAlpha );
				Vector3 imgPos = new Vector3( absRect.Left, absRect.Top, GuiSystem.Instance.Renderer.GetZLayer(CustomImageLayer) );
				normalImage.Draw( imgPos, clipper );
			}

			// draw label text
			absRect.Top += (absRect.Height - Font.LineSpacing) / 2;
			colors = new ColorRect( normalColor );
			colors.SetAlpha( EffectiveAlpha );
			Font.DrawText( Text, absRect, GuiSystem.Instance.Renderer.GetZLayer(LabelLayer), clipper, HorizontalTextFormat.Center, colors );
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="z"></param>
		protected override void DrawHover(float z) {
			Rect clipper = PixelRect;

			// do nothing if the widget is totally clipped
			if( clipper.Width == 0 ) {
				return;
			}

			// get the destination screen rect for this widget
			Rect absRect = UnclippedPixelRect;
			
			// calculate the colors to use
			ColorRect colors = new ColorRect( HoverPrimaryColor,
														 HoverSecondaryColor,
														 HoverSecondaryColor,
														 HoverPrimaryColor );
			colors.SetAlpha( EffectiveAlpha );

			// render standard button imagery if required.
			if( useStandardImagery ) {
				// draw background image
				Rect bkRect = absRect;
				bkRect.Left += frameLeftSize;
				bkRect.Right -= frameRightSize;
				bkRect.Top += frameTopSize;
				bkRect.Bottom -= frameBottomSize;
				background.Draw( bkRect, z, clipper, colors );

				// draw frame
				hoverFrame.Draw( new Vector3(absRect.Left, absRect.Top, z), clipper );
			}

			// render clients custom image if that is required.
			if( useHoverImage ) {
				hoverImage.Colors.SetAlpha( EffectiveAlpha );
				Vector3 imgPos = new Vector3( absRect.Left, absRect.Top, GuiSystem.Instance.Renderer.GetZLayer(CustomImageLayer) );
				hoverImage.Draw( imgPos, clipper );
			}

			// draw label text
			absRect.Top += (absRect.Height - Font.LineSpacing) / 2;
			colors = new ColorRect( hoverColor );
			colors.SetAlpha( EffectiveAlpha );
			Font.DrawText( Text, absRect, GuiSystem.Instance.Renderer.GetZLayer(LabelLayer), clipper, HorizontalTextFormat.Center, colors );
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="z"></param>
		protected override void DrawPushed(float z) {
			Rect clipper = PixelRect;

			// do nothing if the widget is totally clipped
			if( clipper.Width == 0 ) {
				return;
			}

			// get the destination screen rect for this widget
			Rect absRect = UnclippedPixelRect;
			
			// calculate the colors to use
			ColorRect colors = new ColorRect( PushedPrimaryColor,
														 PushedSecondaryColor,
														 PushedSecondaryColor,
														 PushedPrimaryColor );
			colors.SetAlpha( EffectiveAlpha );

			// render standard button imagery if required.
			if( useStandardImagery ) {
				// draw background image
				Rect bkRect = absRect;
				bkRect.Left += frameLeftSize;
				bkRect.Right -= frameRightSize;
				bkRect.Top += frameTopSize;
				bkRect.Bottom -= frameBottomSize;
				background.Draw( bkRect, z, clipper, colors );

				// draw frame
				pushedFrame.Draw( new Vector3(absRect.Left, absRect.Top, z), clipper );
			}

			// render clients custom image if that is required.
			if( usePushedImage ) {
				pushedImage.Colors.SetAlpha( EffectiveAlpha );
				Vector3 imgPos = new Vector3( absRect.Left, absRect.Top, GuiSystem.Instance.Renderer.GetZLayer(CustomImageLayer) );
				pushedImage.Draw( imgPos, clipper );
			}

			// draw label text
			absRect.Top += (absRect.Height - Font.LineSpacing) / 2;
			colors = new ColorRect( pushedColor );
			colors.SetAlpha( EffectiveAlpha );
			Font.DrawText( Text, absRect, GuiSystem.Instance.Renderer.GetZLayer(LabelLayer), clipper, HorizontalTextFormat.Center, colors );
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="z"></param>
		protected override void DrawDisabled(float z) 
		{
			Rect clipper = PixelRect;

			// do nothing if the widget is totally clipped
			if( clipper.Width == 0 ) {
				return;
			}

			// get the destination screen rect for this widget
			Rect absRect = UnclippedPixelRect;
			
			// calculate the colors to use
			ColorRect colors = new ColorRect( DisabledPrimaryColor,
														 DisabledSecondaryColor,
														 DisabledSecondaryColor,
														 DisabledPrimaryColor );
			colors.SetAlpha( EffectiveAlpha );

			// render standard button imagery if required.
			if( useStandardImagery ) {
				// draw background image
				Rect bkRect = absRect;
				bkRect.Left += frameLeftSize;
				bkRect.Right -= frameRightSize;
				bkRect.Top += frameTopSize;
				bkRect.Bottom -= frameBottomSize;
				background.Draw( bkRect, z, clipper, colors );

				// draw frame
				normalFrame.Draw( new Vector3(absRect.Left, absRect.Top, z), clipper );
			}

			// render clients custom image if that is required.
			if( useDisabledImage ) {
				disabledImage.Colors.SetAlpha( EffectiveAlpha );
				Vector3 imgPos = new Vector3( absRect.Left, absRect.Top, GuiSystem.Instance.Renderer.GetZLayer(CustomImageLayer) );
				disabledImage.Draw( imgPos, clipper );
			}

			// draw label text
			absRect.Top += (absRect.Height - Font.LineSpacing) / 2;
			colors = new ColorRect( disabledColor );
			colors.SetAlpha( EffectiveAlpha );
			Font.DrawText( Text, absRect, GuiSystem.Instance.Renderer.GetZLayer(LabelLayer), clipper, HorizontalTextFormat.Center, colors );
		}

		#endregion

		#endregion

		#region Window Members

		/// <summary>
		/// 
		/// </summary>
		/// <param name="e"></param>
		protected override void OnSized(GuiEventArgs e) {
			// default processing
			base.OnSized(e);

			SizeF absSize = AbsoluteSize;

			// update frame size
			normalFrame.Size = absSize;
			hoverFrame.Size = absSize;
			pushedFrame.Size = absSize;

			// scale user images if required
			if( autoscaleImages )
			{
				Rect area = new Rect( 0, 0, absSize.Width, absSize.Height );

				normalImage.Rect = area;
				hoverImage.Rect = area;
				pushedImage.Rect = area;
				disabledImage.Rect = area;
			}

			e.Handled = true;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="e"></param>
		protected override void OnAlphaChanged(GuiEventArgs e) {
			base.OnAlphaChanged(e);

			normalFrame.Colors.SetAlpha( EffectiveAlpha );
			hoverFrame.Colors.SetAlpha( EffectiveAlpha );
			pushedFrame.Colors.SetAlpha( EffectiveAlpha );
		}

		#endregion
	}

}
