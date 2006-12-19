using System;
using System.Drawing;
using CeGuiSharp;
using CeGuiSharp.Widgets;

namespace CeGuiSharp.WidgetSets.TaharezLook {
	/// <summary>
	/// Standard Listbox widget for the Taharez Gui Scheme.
	/// </summary>
	public class TLListbox : Listbox {
		#region Constants
		/// <summary>
		///		Name of the Imageset to use for rendering.
		/// </summary>
		const string ImagesetName = "TaharezLook";
		/// <summary>
		/// Name of the image to use for the top-left corner of the box.
		/// </summary>
		const string TopLeftImageName = "ListboxTopLeft";
		/// <summary>
		/// Name of the image to use for the top-right corner of the box.
		/// </summary>
		const string TopRightImageName = "ListboxTopRight";
		/// <summary>
		/// Name of the image to use for the bottom left corner of the box.
		/// </summary>
		const string BottomLeftImageName = "ListboxBottomLeft";
		/// <summary>
		/// Name of the image to use for the bottom right corner of the box.
		/// </summary>
		const string BottomRightImageName = "ListboxBottomRight";
		/// <summary>
		/// Name of the image to use for the left edge of the box.
		/// </summary>
		const string LeftEdgeImageName = "ListboxLeft";
		/// <summary>
		/// Name of the image to use for the right edge of the box.
		/// </summary>
		const string RightEdgeImageName = "ListboxRight";
		/// <summary>
		/// Name of the image to use for the top edge of the box.
		/// </summary>
		const string TopEdgeImageName = "ListboxTop";
		/// <summary>
		/// Name of the image to use for the bottom edge of the box.
		/// </summary>
		const string BottomEdgeImageName = "ListboxBottom";
		/// <summary>
		/// Name of the image to use for the box background.
		/// </summary>
		const string BackgroundImageName = "ListboxBackdrop";
		/// <summary>
		/// Name of the image to use for the selection highlight brush.
		/// </summary>
		const string SelectionBrushImageName = "ListboxSelectionBrush";
		/// <summary>
		/// Name of the image to use for the mouse cursor.
		/// </summary>
		const string MouseCursorImageName = "MouseTarget";
		/// <summary>
		/// Type name of widget to be created as horizontal scroll bar.
		/// </summary>
		const string HorzScrollbarTypeName = "TaharezLook.TLMiniHorizontalScrollbar";
		/// <summary>
		/// Type name of widget to be created as vertical scroll bar.
		/// </summary>
		const string VertScrollbarTypeName = "TaharezLook.TLMiniVerticalScrollbar";

		#endregion

		#region Fields
		/// <summary>
		/// Used for the frame of the list box.
		/// </summary>
		protected RenderableFrame	frame = new RenderableFrame();
		/// <summary>
		/// Used for the background area of the list box.
		/// </summary>
		protected RenderableImage	background = new RenderableImage();
		/// <summary>
		/// Width of the left frame edge in pixels.
		/// </summary>
		protected float leftFrameSize;
		/// <summary>
		/// Width of the right frame edge in pixels.
		/// </summary>
		protected float rightFrameSize;
		/// <summary>
		/// Height of the top frame edge in pixels.
		/// </summary>
		protected float topFrameSize;
		/// <summary>
		/// Height of the bottom frame edge in pixels.
		/// </summary>
		protected float bottomFrameSize;
		#endregion

		#region Constructor
		/// <summary>
		///		Constructor.
		/// </summary>
		/// <param name="name"></param>
		public TLListbox(string type, string name) : base(type, name) {
			Imageset imageSet = ImagesetManager.Instance.GetImageset(ImagesetName);

			StoreFrameSizes();

			// set up frame images
			frame.SetImages(
				imageSet.GetImage(TopLeftImageName),		imageSet.GetImage(TopRightImageName),
				imageSet.GetImage(BottomLeftImageName),		imageSet.GetImage(BottomRightImageName),
				imageSet.GetImage(LeftEdgeImageName),		imageSet.GetImage(TopEdgeImageName),
				imageSet.GetImage(RightEdgeImageName),		imageSet.GetImage(BottomEdgeImageName)
				);

			// set up background brush
			background.Image = imageSet.GetImage(BackgroundImageName);
			background.Position = new Point(leftFrameSize, topFrameSize);
			background.HorizontalFormat = HorizontalImageFormat.Stretched;
			background.VerticalFormat = VerticalImageFormat.Stretched;

			// set mouse cursor for this Listbox
			SetMouseCursor(imageSet.GetImage(MouseCursorImageName));
		}

		#endregion

		#region Members
		/// <summary>
		/// Store sizes for the frame edges.
		/// </summary>
		protected void StoreFrameSizes() {
			Imageset imageSet = ImagesetManager.Instance.GetImageset(ImagesetName);
			
			leftFrameSize	= imageSet.GetImage(LeftEdgeImageName).Width;
			rightFrameSize	= imageSet.GetImage(RightEdgeImageName).Width;
			topFrameSize	= imageSet.GetImage(TopEdgeImageName).Height;
			bottomFrameSize	= imageSet.GetImage(BottomEdgeImageName).Height;
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

				tmp.Left = leftFrameSize;
				tmp.Top	 = topFrameSize;
				tmp.Size = new SizeF(AbsoluteWidth - leftFrameSize, AbsoluteHeight - topFrameSize);

				if (vertScrollbar.Visible) {
					tmp.Right -= vertScrollbar.AbsoluteWidth;
				}
				else {
					tmp.Right -= rightFrameSize;
				}

				if (horzScrollbar.Visible) {
					tmp.Bottom -= horzScrollbar.AbsoluteHeight;
				}
				else {
					tmp.Bottom -= bottomFrameSize;
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
			newSize.Width	-= (leftFrameSize + rightFrameSize);
			newSize.Height	-= (topFrameSize + bottomFrameSize);

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
