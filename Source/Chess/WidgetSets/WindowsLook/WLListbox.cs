using System;
using System.Drawing;
using CeGuiSharp;
using CeGuiSharp.Widgets;

namespace CeGuiSharp.WidgetSets.WindowsLook {

	/// <summary>
	/// 
	/// </summary>
	public class WLListbox : Listbox
	{
		#region Constants

		// image / imageset related
		/// <summary>
		///		Name of the Imageset to use for rendering.
		/// </summary>
		const string	ImagesetName				= "WindowsLook";
		const string	TopLeftImageName			= "StaticFrameTopLeft";
		const string	TopRightImageName			= "StaticFrameTopRight";
		const string	BottomLeftImageName		= "StaticFrameBottomLeft";
		const string	BottomRightImageName		= "StaticFrameBottomRight";
		const string	LeftEdgeImageName			= "StaticFrameLeft";
		const string	RightEdgeImageName			= "StaticFrameRight";
		const string	TopEdgeImageName			= "StaticFrameTop";
		const string	BottomEdgeImageName		= "StaticFrameBottom";
		const string	BackgroundImageName		= "Background";
		const string	SelectionBrushImageName	= "Background";
		const string	MouseCursorImageName		= "MouseArrow";

		// component widget type names
		const string	HorzScrollbarTypeName		= "WindowsLook.WLHorizontalScrollbar";
		const string	VertScrollbarTypeName		= "WindowsLook.WLVerticalScrollbar";

		#endregion

		#region Fields

		RenderableFrame frame = new RenderableFrame();
		RenderableImage background = new RenderableImage();

		// sizes of frame edges
		float frameLeftSize;
		float frameRightSize;
		float frameTopSize;
		float frameBottomSize;

		#endregion

		#region Constructor

		/// <summary>
		/// 
		/// </summary>
		/// <param name="name"></param>
		public WLListbox( string type, string name ) : base(type, name) {
			Imageset imageSet = ImagesetManager.Instance.GetImageset(ImagesetName);

			StoreFrameSizes();

			// setup frame images
			frame.SetImages(
				imageSet.GetImage(TopLeftImageName),
				imageSet.GetImage(TopRightImageName),
				imageSet.GetImage(BottomLeftImageName),
				imageSet.GetImage(BottomRightImageName),
				imageSet.GetImage(LeftEdgeImageName),
				imageSet.GetImage(TopEdgeImageName),
				imageSet.GetImage(RightEdgeImageName),
				imageSet.GetImage(BottomEdgeImageName));

			// setup the background brush
			background.Image = imageSet.GetImage(BackgroundImageName);
			background.Position = new Point(frameLeftSize, frameTopSize);
			background.HorizontalFormat = HorizontalImageFormat.Stretched;
			background.VerticalFormat = VerticalImageFormat.Stretched;

			// set mouse cursor for this Listbox
			SetMouseCursor(imageSet.GetImage(MouseCursorImageName));
		}

		#endregion

		#region Methods

		/// <summary>
		///		Store the sizes for the frame edges.
		/// </summary>
		protected void StoreFrameSizes() {
			Imageset imageSet = ImagesetManager.Instance.GetImageset(ImagesetName);

			frameLeftSize	= imageSet.GetImage(LeftEdgeImageName).Width;
			frameRightSize	= imageSet.GetImage(RightEdgeImageName).Width;
			frameTopSize	= imageSet.GetImage(TopEdgeImageName).Height;
			frameBottomSize = imageSet.GetImage(BottomEdgeImageName).Height;
		}

		#endregion

		#region Listbox Members

		#region Properties

		/// <summary>
		/// Get the Rect that describes, in un-clipped pixels, the window relative area
		/// that is to be used for rendering list items.
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

		#endregion

		#region Methods
		
		/// <summary>
		/// Create a Widget to be used as the vertical scrollbar in this Listbox.
		/// </summary>
		/// <returns>A Scrollbar based object.</returns>
		protected override Scrollbar CreateVerticalScrollbar() {
			Scrollbar sbar = (Scrollbar)WindowManager.Instance.CreateWindow(VertScrollbarTypeName, Name + "_auto_VScrollbar");

			// set min/max sizes
			sbar.MinimumSize =  new SizeF(0.0125f, 0.0f);
			sbar.MaximumSize =  new SizeF(0.0125f, 1.0f);

			return sbar;
		}

		/// <summary>
		/// Create a widget to be used as the horizontal scrollbar in this Listbox.
		/// </summary>
		/// <returns>A Scrollbar based object.</returns>
		protected override Scrollbar CreateHorizontalScrollbar() {
			Scrollbar sbar = (Scrollbar)WindowManager.Instance.CreateWindow(HorzScrollbarTypeName, Name + "_auto_HScrollbar");

			// set min/max sizes
			sbar.MinimumSize =  new SizeF(0.0f, 0.016667f);
			sbar.MaximumSize =  new SizeF(1.0f, 0.016667f);

			return sbar;
		}

		/// <summary>
		/// Layout the component widgets of this Listbox.
		/// </summary>
		protected override void	LayoutComponentWidgets() {
			// set desired size for vertical scroll-bar
			SizeF verticalSize = new SizeF(0.05f, 1.0f);
			vertScrollbar.Size = verticalSize;

			// get the actual size used for vertical scroll bar.
			verticalSize = AbsoluteToRelative(vertScrollbar.AbsoluteSize);


			// set desired size for horizontal scroll-bar
			SizeF horizontalSize = new SizeF(1.0f, 0.0f);

			if (absArea.Height != 0.0f) {
				horizontalSize.Height = (absArea.Width * verticalSize.Width) / absArea.Height;
			}

			// adjust length to consider width of vertical scroll bar if that is visible
			if (vertScrollbar.Visible) {
				horizontalSize.Width -= verticalSize.Width;
			}

			horzScrollbar.Size = horizontalSize;

			// get actual size used
			horizontalSize = AbsoluteToRelative(horzScrollbar.AbsoluteSize);


			// position vertical scroll bar
			vertScrollbar.Position = new Point(1.0f - verticalSize.Width, 0.0f);

			// position horizontal scroll bar
			horzScrollbar.Position = new Point(0.0f, 1.0f - horizontalSize.Height);
		}

		/// <summary>
		/// Perform rendering of the widget control frame and other 'static' areas.  This
		/// method should not render the actual items.  Note that the items are typically
		/// rendered to layer 3, other layers can be used for rendering imagery behind and
		/// infront of the items.
		/// </summary>
		/// <param name="z">base z co-ordinate (layer 0)</param>
		protected override void RenderListboxBaseImagery(float z) {
			Rect clipper = PixelRect;

			// do nothing if the widget is totally clipped.
			if (clipper.Width == 0) {
				return;
			}

			// get the destination screen rect for this window
			Rect absRect = UnclippedPixelRect;

			// draw the box elements
			Vector3 position = new Vector3(absRect.Left, absRect.Top, z);
			background.Draw(position, clipper);
			frame.Draw(position, clipper);
		}

		#endregion

		#endregion

		#region Window Members
		
		/// <summary>
		///      Event trigger method for the <see cref="CEGUI.Window.Sized"/> event.
		/// </summary>
		/// <param name="e">Event information.</param>
		protected override void OnSized(GuiEventArgs e) {
			// base class processing
			base.OnSized(e);

			// update size of frame
			SizeF newSize = AbsoluteSize;
			frame.Size = newSize;

			// update size of background image
			newSize.Width	-= (frameLeftSize + frameRightSize);
			newSize.Height	-= (frameTopSize + frameBottomSize);

			background.Size = newSize;
		}

		/// <summary>
		///      Event trigger method for the <see cref="CEGUI.Window.AlphaChanged"/> event.
		/// </summary>
		/// <param name="e">Event information.</param>
		protected override void OnAlphaChanged(GuiEventArgs e) {
			base.OnAlphaChanged(e);

			// update alpha values for the frame and background brush
			float alpha = EffectiveAlpha;

			ColorRect cr = frame.Colors;
			cr.SetAlpha(alpha);
			frame.SetColors(cr);

			cr = background.Colors;
			cr.SetAlpha(alpha);
			background.SetColors(cr);
		}

		#endregion
	}
}
