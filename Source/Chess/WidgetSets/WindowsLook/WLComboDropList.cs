using System;
using System.Drawing;
using CeGuiSharp;
using CeGuiSharp.Widgets;

namespace CeGuiSharp.WidgetSets.WindowsLook {

	/// <summary>
	/// 
	/// </summary>
	public class WLComboDropList : ComboDropList
	{
		#region Constants

		/// <summary>
		///		Name of the imageset to use for rendering.
		/// </summary>
		const string	ImagesetName = "WindowsLook";
		/// <summary>
		///		Name of the image to use for the top-left corner of the box.
		/// </summary>
		const string	TopLeftImageName = "StaticFrameTopLeft";
		/// <summary>
		///		Name of the image to use for the top-right corner of the box.
		/// </summary>
		const string	TopRightImageName = "StaticFrameTopRight";
		/// <summary>
		///		Name of the image to use for the bottom left corner of the box.
		/// </summary>
		const string	BottomLeftImageName = "StaticFrameBottomLeft";
		/// <summary>
		///		Name of the image to use for the bottom right corner of the box.
		/// </summary>
		const string	BottomRightImageName = "StaticFrameBottomRight";
		/// <summary>
		///		Name of the image to use for the left edge of the box.
		/// </summary>
		const string	LeftEdgeImageName = "StaticFrameLeft";
		/// <summary>
		///		Name of the image to use for the right edge of the box.
		/// </summary>
		const string	RightEdgeImageName = "StaticFrameRight";
		/// <summary>
		///		Name of the image to use for the top edge of the box.
		/// </summary>
		const string	TopEdgeImageName = "StaticFrameTop";
		/// <summary>
		///		Name of the image to use for the bottom edge of the box.
		/// </summary>
		const string	BottomEdgeImageName = "StaticFrameBottom";
		/// <summary>
		///		Name of the image to use for the box background.
		/// </summary>
		const string	BackgroundImageName = "Background";
		/// <summary>
		///		Name of the image to use for the selection highlight brush.
		/// </summary>
		const string	SelectionBrushImageName = "Background";
		/// <summary>
		///		Name of the image to use for the mouse cursor.
		/// </summary>
		const string	MouseCursorImageName = "MouseArrow";

		// component widget type names
		/// <summary>
		///		Type name of widget to be created as horizontal scroll bar.
		/// </summary>
		const string	HorzScrollbarTypeName = "WindowsLook.WLHorizontalScrollbar";
		/// <summary>
		///		Type name of widget to be created as vertical scroll bar.
		/// </summary>
		const string	VertScrollbarTypeName = "WindowsLook.WLVerticalScrollbar";

		#endregion Constants

		#region Fields

		/// <summary>
		///		Used for the frame of the drop-list box.
		/// </summary>
		protected RenderableFrame frame = new RenderableFrame();
		/// <summary>
		/// `Used for the background area of the drop-list box.
		/// </summary>
		protected RenderableImage background = new RenderableImage();

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

		#endregion Fields

		#region Constructor

		/// <summary>
		/// 
		/// </summary>
		/// <param name="name"></param>
		public WLComboDropList(string type, string name) : base(type, name) {
		}

		#endregion Constructor

		#region Window Members

		/// <summary>
		/// 
		/// </summary>
		public override void Initialize() {
			base.Initialize();

			StoreFrameSizes();

			// setup frame images
			Imageset iset = ImagesetManager.Instance.GetImageset(ImagesetName);
			frame.SetImages(
				iset.GetImage(TopLeftImageName), iset.GetImage(TopRightImageName),
				iset.GetImage(BottomLeftImageName), iset.GetImage(BottomRightImageName),
				iset.GetImage(LeftEdgeImageName), iset.GetImage(TopEdgeImageName),
				iset.GetImage(RightEdgeImageName), iset.GetImage(BottomEdgeImageName)
				);

			// setup background brush
			background.Image = iset.GetImage(BackgroundImageName);
			background.Position = new Point(frameLeftSize, frameTopSize);
			background.HorizontalFormat = HorizontalImageFormat.Stretched;
			background.VerticalFormat = VerticalImageFormat.Stretched;

			// set cursor for this window
			SetMouseCursor(iset.GetImage(MouseCursorImageName));
		}

		#endregion Window Members

		#region Listbox Members

		#region Properties

		/// <summary>
		///		Return a Rect object describing, in un-clipped pixels, the window
		///		relative area that is to be used for rendering list items.
		/// </summary>
		protected override Rect ListRenderArea {
			get {
				Rect tmp = new Rect();

				tmp.Left = frameLeftSize;
				tmp.Top = frameTopSize;
				tmp.Size = new SizeF(AbsoluteWidth - frameLeftSize, AbsoluteHeight - frameTopSize);

				if(vertScrollbar.Visible) {
					tmp.Right -= vertScrollbar.AbsoluteWidth;
				}
				else {
					tmp.Right -= frameRightSize;
				}

				if(horzScrollbar.Visible) {
					tmp.Bottom -= horzScrollbar.AbsoluteHeight;
				}
				else {
					tmp.Bottom -= frameBottomSize;
				}

				return tmp;
			}
		}

		#endregion Properties

		#region Methods

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		protected override Scrollbar CreateHorizontalScrollbar() {
			Scrollbar sbar = (Scrollbar)WindowManager.Instance.CreateWindow(HorzScrollbarTypeName, name + "__auto_hscrollbar");
			
			// set the min/max sizes
			sbar.MinimumSize = new SizeF(0.0f, 0.016667f);
			sbar.MaximumSize = new SizeF(1.0f, 0.016667f);

			return sbar;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		protected override Scrollbar CreateVerticalScrollbar() {
			Scrollbar sbar = (Scrollbar)WindowManager.Instance.CreateWindow(VertScrollbarTypeName, name + "__auto_vscrollbar");
			
			// set the min/max sizes
			sbar.MinimumSize = new SizeF(0.0125f, 0);
			sbar.MaximumSize = new SizeF(0.0125f, 1.0f);

			return sbar;
		}

		/// <summary>
		/// 
		/// </summary>
		protected override void LayoutComponentWidgets() {
			// set desired size for vertical scroll-bar
			SizeF vSize = new SizeF(0.05f, 1.0f);
			vertScrollbar.Size = vSize;

			// get the actual size used for vertical scroll bar.
			vSize = AbsoluteToRelative(vertScrollbar.AbsoluteSize);

			// set desired size for horizontal scroll-bar
			SizeF hSize = new SizeF(1.0f, 0.0f);

			if (absArea.Height != 0.0f) {
				hSize.Height = (absArea.Width * vSize.Width) / absArea.Height;
			}

			// adjust length to consider width of vertical scroll bar if that is visible
			if (vertScrollbar.Visible) {
				hSize.Width -= vSize.Width;
			}

			horzScrollbar.Size = hSize;

			// get actual size used
			hSize = AbsoluteToRelative(horzScrollbar.AbsoluteSize);

			// position vertical scroll bar
			vertScrollbar.Position = new Point(1.0f - vSize.Width, 0.0f);

			// position horizontal scroll bar
			horzScrollbar.Position = new Point(0.0f, 1.0f - hSize.Height);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="z"></param>
		protected override void RenderListboxBaseImagery(float z) {
			Rect clipper = PixelRect;

			// do nothing if the widget is totally clipped.
			if (clipper.Width == 0) {
				return;
			}

			// get the destination screen rect for this window
			Rect absrect = UnclippedPixelRect;

			// draw the box elements
			Vector3 pos = new Vector3(absrect.Left, absrect.Top, z);
			background.Draw(pos, clipper);
			frame.Draw(pos, clipper);
		}

		#endregion Methods

		#endregion Listbox Members

		#region Base Members

		/// <summary>
		///		Store the sizes for the frame edges.
		/// </summary>
		protected void StoreFrameSizes() {
			Imageset iset = ImagesetManager.Instance.GetImageset(ImagesetName);

			frameLeftSize		= iset.GetImage(LeftEdgeImageName).Width;
			frameRightSize		= iset.GetImage(RightEdgeImageName).Width;
			frameTopSize		= iset.GetImage(TopEdgeImageName).Height;
			frameBottomSize		= iset.GetImage(BottomEdgeImageName).Height;
		}

		#endregion Base Members

		#region Events

		#region Overridden Trigger Methods

		/// <summary>
		/// 
		/// </summary>
		/// <param name="e"></param>
		protected override void OnSized(GuiEventArgs e) {
			// base class processing
			base.OnSized(e);

			// update size of frame
			SizeF newsize = AbsoluteSize;
			frame.Size = newsize;

			// update size of background image
			newsize.Width		-= (frameLeftSize + frameRightSize);
			newsize.Height		-= (frameTopSize + frameBottomSize);

			background.Size = newsize;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="e"></param>
		protected override void OnAlphaChanged(GuiEventArgs e) {
			// base class processing
			base.OnAlphaChanged(e);

			// update alpha values for the frame and background brush
			float alpha = EffectiveAlpha;

			ColorRect cr = frame.Colors;
			cr.SetAlpha(alpha);

			cr = background.Colors;
			cr.SetAlpha(alpha);
		}

		#endregion Overridden Trigger Methods

		#endregion Events
	}
}
