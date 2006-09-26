using System;
using CeGuiSharp;
using CeGuiSharp.Widgets;

namespace CeGuiSharp.WidgetSets.WindowsLook {

	/// <summary>
	/// 
	/// </summary>
	public class WLProgressBar : ProgressBar
	{
		#region Constants

		/// <summary>
		/// 
		/// </summary>
		protected const string	ImagesetName				= "WindowsLook";
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
		protected const string	RightFrameImageName		= "StaticFrameRight";
		/// <summary>
		/// 
		/// </summary>
		protected const string	TopFrameImageName			= "StaticFrameTop";
		/// <summary>
		/// 
		/// </summary>
		protected const string	BottomFrameImageName		= "StaticFrameBottom";
		/// <summary>
		/// 
		/// </summary>
		protected const string	BackgroundImageName		= "Background";
		/// <summary>
		/// 
		/// </summary>
		protected const string	MouseCursorImageName		= "MouseArrow";

		// colours
		/// <summary>
		/// 
		/// </summary>
		protected Color ContainerBackgroundColor		= new Color(0xDFDFDF);
		/// <summary>
		/// 
		/// </summary>
		protected Color DefaultProgressColor			= new Color(0x2222FF);

		// rendering layers
		/// <summary>
		/// 
		/// </summary>
		protected const int	ContainerLayer	= 0;
		/// <summary>
		/// 
		/// </summary>
		protected const int	ProgressLayer	= 1;

		#endregion

		#region Fields

		/// <summary>
		/// 
		/// </summary>
		protected Image background;
		/// <summary>
		/// 
		/// </summary>
		protected RenderableFrame frame = new RenderableFrame();

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

		/// <summary>
		/// 
		/// </summary>
		protected Color progressColor;

		#endregion

		#region Constructor

		/// <summary>
		/// 
		/// </summary>
		/// <param name="name"></param>
		public WLProgressBar(string type, string name ) : base(type, name) {
			Imageset imageSet = ImagesetManager.Instance.GetImageset(ImagesetName);

			frame.SetImages(
				imageSet.GetImage(TopLeftFrameImageName),
				imageSet.GetImage(TopRightFrameImageName),
				imageSet.GetImage(BottomLeftFrameImageName),
				imageSet.GetImage(BottomRightFrameImageName),
				imageSet.GetImage(LeftFrameImageName),
				imageSet.GetImage(TopFrameImageName),
				imageSet.GetImage(RightFrameImageName),
				imageSet.GetImage(BottomFrameImageName));

			background = imageSet.GetImage(BackgroundImageName);

			SetMouseCursor(imageSet.GetImage(MouseCursorImageName));

			StoreFrameSizes();

			progressColor = DefaultProgressColor;
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

		#region Window Members

		/// <summary>
		/// 
		/// </summary>
		/// <param name="z"></param>
		protected override void DrawSelf(float z) {
			Rect clipper = PixelRect;

			// do nothing if the widget is totally clipped
			if(clipper.Width == 0) {
				return;
			}

			// rendering layers
			float containerZ = GuiSystem.Instance.Renderer.GetZLayer(ContainerLayer);
			float progressZ = GuiSystem.Instance.Renderer.GetZLayer(ProgressLayer);

			// get the destination screen rect for this window
			Rect absRect = UnclippedPixelRect;

			// render the container
			Vector3 pos = new Vector3(absRect.Left, absRect.Top, containerZ);
			frame.Draw(pos, clipper);

			// calculate inner area rect considering frame
			absRect.Left += frameLeftSize;
			absRect.Top += frameTopSize;
			absRect.Right -= frameRightSize;
			absRect.Bottom -= frameBottomSize;

			// calculate colors to use
			ColorRect colors = new ColorRect(ContainerBackgroundColor);
			colors.SetAlpha(EffectiveAlpha);

			// draw background image
			background.Draw(absRect, pos.z, clipper, colors);

			// adjust rect area according to current progress
			absRect.Width = absRect.Width * Progress;

			// calculate colors for progress bar itself
			colors = new ColorRect(progressColor);
			colors.SetAlpha(EffectiveAlpha);
			background.Draw(absRect, progressZ, clipper, colors);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="e"></param>
		protected override void OnSized(GuiEventArgs e) {
			base.OnSized(e);

			// update frame size.
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
