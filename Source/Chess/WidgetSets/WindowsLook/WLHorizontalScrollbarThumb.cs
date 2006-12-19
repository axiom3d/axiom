using System;
using CeGuiSharp;
using CeGuiSharp.Widgets;

namespace CeGuiSharp.WidgetSets.WindowsLook {

	/// <summary>
	/// 
	/// </summary>
	public class WLHorizontalScrollbarThumb : Thumb
	{
		#region Constants

		/// <summary>
		///		Name of the Imageset to use for rendering.
		/// </summary>
		protected const string	ImagesetName					= "WindowsLook";
		/// <summary>
		/// 
		/// </summary>
		protected const string	BackgroundImageName				= "Background";
		/// <summary>
		/// 
		/// </summary>
		protected const string	NormalLeftImageName				= "ButtonNormalLeft";
		/// <summary>
		/// 
		/// </summary>
		protected const string	NormalRightImageName			= "ButtonNormalRight";
		/// <summary>
		/// 
		/// </summary>
		protected const string	NormalTopImageName				= "ButtonNormalTop";
		/// <summary>
		/// 
		/// </summary>
		protected const string	NormalBottomImageName			= "ButtonNormalBottom";
		/// <summary>
		/// 
		/// </summary>
		protected const string	NormalTopLeftImageName			= "ButtonNormalTopLeft";
		/// <summary>
		/// 
		/// </summary>
		protected const string	NormalTopRightImageName			= "ButtonNormalTopRight";
		/// <summary>
		/// 
		/// </summary>
		protected const string	NormalBottomLeftImageName		= "ButtonNormalBottomLeft";
		/// <summary>
		/// 
		/// </summary>
		protected const string	NormalBottomRightImageName		= "ButtonNormalBottomRight";
		/// <summary>
		/// 
		/// </summary>
		protected const string	HoverLeftImageName				= "ButtonHoverLeft";
		/// <summary>
		/// 
		/// </summary>
		protected const string	HoverRightImageName				= "ButtonHoverRight";
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
		protected const string	HoverTopLeftImageName			= "ButtonHoverTopLeft";
		/// <summary>
		/// 
		/// </summary>
		protected const string	HoverTopRightImageName			= "ButtonHoverTopRight";
		/// <summary>
		/// 
		/// </summary>
		protected const string	HoverBottomLeftImageName		= "ButtonHoverBottomLeft";
		/// <summary>
		/// 
		/// </summary>
		protected const string	HoverBottomRightImageName		= "ButtonHoverBottomRight";
		/// <summary>
		/// 
		/// </summary>
		protected const string	PushedLeftImageName				= "ButtonPushedLeft";
		/// <summary>
		/// 
		/// </summary>
		protected const string	PushedRightImageName			= "ButtonPushedRight";
		/// <summary>
		/// 
		/// </summary>
		protected const string	PushedTopImageName				= "ButtonPushedTop";
		/// <summary>
		/// 
		/// </summary>
		protected const string	PushedBottomImageName			= "ButtonPushedBottom";
		/// <summary>
		/// 
		/// </summary>
		protected const string	PushedTopLeftImageName			= "ButtonPushedTopLeft";
		/// <summary>
		/// 
		/// </summary>
		protected const string	PushedTopRightImageName			= "ButtonPushedTopRight";
		/// <summary>
		/// 
		/// </summary>
		protected const string	PushedBottomLeftImageName		= "ButtonPushedBottomLeft";
		/// <summary>
		/// 
		/// </summary>
		protected const string	PushedBottomRightImageName		= "ButtonPushedBottomRight";
		/// <summary>
		/// 
		/// </summary>
		protected const string	GripperImageName				= "HorzScrollbarGrip";
		/// <summary>
		/// 
		/// </summary>
		protected const string	MouseCursorImageName			= "MouseArrow";

		// colours
		/// <summary>
		/// 
		/// </summary>
		protected static Color NormalPrimaryColor		= new Color(0xAFAFAF);
		/// <summary>
		/// 
		/// </summary>
		protected static Color NormalSecondaryColor	= new Color(0xFFFFFF);
		/// <summary>
		/// 
		/// </summary>
		protected static Color HoverPrimaryColor		= new Color(0xCFD9CF);
		/// <summary>
		/// 
		/// </summary>
		protected static Color HoverSecondaryColor		= new Color(0xF2FFF2);
		/// <summary>
		/// 
		/// </summary>
		protected static Color PushedPrimaryColor		= new Color(0xAFAFAF);
		/// <summary>
		/// 
		/// </summary>
		protected static Color PushedSecondaryColor	= new Color(0xFFFFFF);
		/// <summary>
		/// 
		/// </summary>
		protected static Color DisabledPrimaryColor	= new Color(0x999999);
		/// <summary>
		/// 
		/// </summary>
		protected static Color DisabledSecondaryColor	= new Color(0x999999);

		// layout related constants
		/// <summary>
		/// 
		/// </summary>
		protected const float	MinimumWidthWithGripRatio	= 2.0f;

		#endregion

		#region Fields

		/// <summary>
		/// 
		/// </summary>
		protected RenderableFrame normalFrame = new RenderableFrame();
		/// <summary>
		/// 
		/// </summary>
		protected RenderableFrame hoverFrame = new RenderableFrame();
		/// <summary>
		/// 
		/// </summary>
		protected RenderableFrame pushedFrame = new RenderableFrame();

		/// <summary>
		/// 
		/// </summary>
		protected float frameLeftSize;
		/// <summary>
		/// 
		/// </summary>
		protected float frameRightSize;
		/// <summary>
		/// 
		/// </summary>
		protected float frameTopSize;
		/// <summary>
		/// 
		/// </summary>
		protected float frameBottomSize;

		/// <summary>
		/// 
		/// </summary>
		protected Image backgroundImage;
		/// <summary>
		/// 
		/// </summary>
		protected Image gripperImage;

		#endregion

		#region Constructor

		/// <summary>
		/// 
		/// </summary>
		/// <param name="name"></param>
		public WLHorizontalScrollbarThumb( string type, string name ) : base(type, name) {
			StoreFrameSizes();

			Imageset imageSet = ImagesetManager.Instance.GetImageset(ImagesetName);

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

			backgroundImage = imageSet.GetImage(BackgroundImageName);
			gripperImage = imageSet.GetImage(GripperImageName);

			SetMouseCursor(imageSet.GetImage(MouseCursorImageName));
		}

		#endregion

		#region Methods

		/// <summary>
		/// 
		/// </summary>
		protected void StoreFrameSizes() {
			Imageset imageSet = ImagesetManager.Instance.GetImageset(ImagesetName);

			frameLeftSize = imageSet.GetImage(NormalLeftImageName).Width;
			frameRightSize = imageSet.GetImage(NormalRightImageName).Width;
			frameTopSize = imageSet.GetImage(NormalTopImageName).Height;
			frameBottomSize = imageSet.GetImage(NormalBottomImageName).Height;
		}

		#endregion

		#region Pushbutton Members

		/// <summary>
		/// 
		/// </summary>
		/// <param name="z"></param>
		protected override void DrawNormal(float z) {
			Rect clipper = PixelRect;

			// do nothing if the widget is totally clipped
			if(clipper.Width == 0) {
				return;
			}

			// get the destination screen rect for this window
			Rect absRect = UnclippedPixelRect;

			// calculate the colors to use
			ColorRect colors = new ColorRect(NormalPrimaryColor, NormalSecondaryColor, NormalSecondaryColor, NormalPrimaryColor);
			colors.SetAlpha(EffectiveAlpha);

			// draw background image
			Rect bkRect = absRect;
			bkRect.Left += frameLeftSize;
			bkRect.Right -= frameRightSize;
			bkRect.Top += frameTopSize;
			bkRect.Bottom -= frameBottomSize;
			backgroundImage.Draw(bkRect, z, clipper, colors);

			// draw frame
			normalFrame.Draw(new Vector3(absRect.Left, absRect.Top, z), clipper);

			// draw gripper if needed
			if(absRect.Width >= gripperImage.Height * MinimumWidthWithGripRatio) {
				Vector3 gripPos = new Vector3(
					absRect.Left + ((absRect.Width - gripperImage.Width) / 2),
					absRect.Top + ((absRect.Height - gripperImage.Height) / 2),
					z );

				gripperImage.Draw(gripPos, clipper, colors);
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="z"></param>
		protected override void DrawHover(float z) {
			Rect clipper = PixelRect;

			// do nothing if the widget is totally clipped
			if(clipper.Width == 0) {
				return;
			}

			// get the destination screen rect for this window
			Rect absRect = UnclippedPixelRect;

			// calculate the colors to use
			ColorRect colors = new ColorRect(HoverPrimaryColor, HoverSecondaryColor, HoverSecondaryColor, HoverPrimaryColor);
			colors.SetAlpha(EffectiveAlpha);

			// draw background image
			Rect bkRect = absRect;
			bkRect.Left += frameLeftSize;
			bkRect.Right -= frameRightSize;
			bkRect.Top += frameTopSize;
			bkRect.Bottom -= frameBottomSize;
			backgroundImage.Draw(bkRect, z, clipper, colors);

			// draw frame
			hoverFrame.Draw(new Vector3(absRect.Left, absRect.Top, z), clipper);

			// draw gripper if needed
			if(absRect.Width >= gripperImage.Height * MinimumWidthWithGripRatio) {
				Vector3 gripPos = new Vector3(
					absRect.Left + ((absRect.Width - gripperImage.Width) / 2),
					absRect.Top + ((absRect.Height - gripperImage.Height) / 2),
					z );

				gripperImage.Draw(gripPos, clipper, colors);
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="z"></param>
		protected override void DrawPushed(float z) {
			Rect clipper = PixelRect;

			// do nothing if the widget is totally clipped
			if(clipper.Width == 0) {
				return;
			}

			// get the destination screen rect for this window
			Rect absRect = UnclippedPixelRect;

			// calculate the colors to use
			ColorRect colors = new ColorRect(PushedPrimaryColor, PushedSecondaryColor, PushedSecondaryColor, PushedPrimaryColor);
			colors.SetAlpha(EffectiveAlpha);

			// draw background image
			Rect bkRect = absRect;
			bkRect.Left += frameLeftSize;
			bkRect.Right -= frameRightSize;
			bkRect.Top += frameTopSize;
			bkRect.Bottom -= frameBottomSize;
			backgroundImage.Draw(bkRect, z, clipper, colors);

			// draw frame
			pushedFrame.Draw(new Vector3(absRect.Left, absRect.Top, z), clipper);

			// draw gripper if needed
			if(absRect.Width >= gripperImage.Height * MinimumWidthWithGripRatio) {
				Vector3 gripPos = new Vector3(
					absRect.Left + ((absRect.Width - gripperImage.Width) / 2),
					absRect.Top + ((absRect.Height - gripperImage.Height) / 2),
					z );

				gripperImage.Draw(gripPos, clipper, colors);
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="z"></param>
		protected override void DrawDisabled(float z) {
			Rect clipper = PixelRect;

			// do nothing if the widget is totally clipped
			if(clipper.Width == 0) {
				return;
			}

			// get the destination screen rect for this window
			Rect absRect = UnclippedPixelRect;

			// calculate the colors to use
			ColorRect colors = new ColorRect(DisabledPrimaryColor, DisabledSecondaryColor, DisabledSecondaryColor, DisabledPrimaryColor);
			colors.SetAlpha(EffectiveAlpha);

			// draw background image
			Rect bkRect = absRect;
			bkRect.Left += frameLeftSize;
			bkRect.Right -= frameRightSize;
			bkRect.Top += frameTopSize;
			bkRect.Bottom -= frameBottomSize;
			backgroundImage.Draw(bkRect, z, clipper, colors);

			// draw frame
			normalFrame.Draw(new Vector3(absRect.Left, absRect.Top, z), clipper);

			// draw gripper if needed
			if(absRect.Width >= gripperImage.Height * MinimumWidthWithGripRatio) {
				Vector3 gripPos = new Vector3(
					absRect.Left + ((absRect.Width - gripperImage.Width) / 2),
					absRect.Top + ((absRect.Height - gripperImage.Height) / 2),
					z );

				gripperImage.Draw(gripPos, clipper, colors);
			}
		}

		#endregion

		#region Window Members

		/// <summary>
		/// 
		/// </summary>
		/// <param name="e"></param>
		protected override void OnSized(GuiEventArgs e) {
			base.OnSized(e);

			// update frame size.
			normalFrame.Size = AbsoluteSize;
			hoverFrame.Size = AbsoluteSize;
			pushedFrame.Size = AbsoluteSize;

			e.Handled = true;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="e"></param>
		protected override void OnAlphaChanged(GuiEventArgs e) {
			base.OnAlphaChanged(e);

			normalFrame.Colors.SetAlpha(EffectiveAlpha);
			hoverFrame.Colors.SetAlpha(EffectiveAlpha);
			pushedFrame.Colors.SetAlpha(EffectiveAlpha);
		}

		#endregion
	}
}
