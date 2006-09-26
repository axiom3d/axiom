using System;
using CeGuiSharp;
using CeGuiSharp.Widgets;

namespace CeGuiSharp.WidgetSets.TaharezLook 
{
	/// <summary>
	/// ListHeaderSegment widget for the Taharez Gui Scheme.
	/// </summary>
	public class TLListHeaderSegment : ListHeaderSegment
	{
		#region Constants
		/// <summary>
		/// Name of the imageset to use for rendering.
		/// </summary>
		const string	ImagesetName = "TaharezLook";

		/// <summary>
		/// Image to use for segment backdrop in normal state.
		/// </summary>
		const string	BackdropNormalImageName = "HeaderBarBackdropNormal";		

		/// <summary>
		/// Image to use for segment backdrop when mouse is hovering.
		/// </summary>
		const string	BackdropHoverImageName = "HeaderBarBackdropHover";

		/// <summary>
		/// Image to use for splitter / sizing bar in normal state.
		/// </summary>
		const string	SplitterNormalImageName = "HeaderBarSplitterNormal";

		/// <summary>
		/// Image to use for splitter / sizing bar in hovering state.
		/// </summary>
		const string	SplitterHoverImageName = "HeaderBarSplitterHover";

		/// <summary>
		/// Image to use for 'sort ascending' indicator.
		/// </summary>
		const string	SortUpImageName = "HeaderBarSortUp";

		/// <summary>
		/// Image to use for 'sort descending' indicator.
		/// </summary>
		const string	SortDownImageName = "HeaderBarSortDown";

		/// <summary>
		/// Image to use for mouse when not sizing.
		/// </summary>
		const string	NormalMouseCursorImageName = "MouseArrow";

		/// <summary>
		/// Image to use for mouse when sizing.
		/// </summary>
		const string	SizingMouseCursorImageName = "MouseEsWeCursor";

		/// <summary>
		/// Image to use for mouse when moving.
		/// </summary>
		const string	MovingMouseCursorImageName = "MouseMoveCursor";

		#endregion

		#region Fields

		/// <summary>
		/// image for normal backdrop.
		/// </summary>
		Image	backNormalImage;

		/// <summary>
		/// image for hover backdrop.
		/// </summary>
		Image	backHoverImage;

		/// <summary>
		/// image for normal splitter.
		/// </summary>
		Image	splitterNormalImage;

		/// <summary>
		/// image for hover splitter.
		/// </summary>
		Image	splitterHoverImage;

		/// <summary>
		/// image for 'sort ascending' icon.
		/// </summary>
		Image	sortAscendImage;

		/// <summary>
		/// image for 'sort descending' icon.
		/// </summary>
		Image	sortDescendImage;
		#endregion

		#region Constructor
		/// <summary>
		///		Constructor.
		/// </summary>
		/// <param name="name"></param>
		public TLListHeaderSegment(string type, string name) : base(type, name) 
		{
			// initialise cache of images used by this widget
			Imageset imageSet = ImagesetManager.Instance.GetImageset(ImagesetName);

			backNormalImage		= imageSet.GetImage(BackdropNormalImageName);
			backHoverImage		= imageSet.GetImage(BackdropHoverImageName);
			splitterNormalImage	= imageSet.GetImage(SplitterNormalImageName);
			splitterHoverImage	= imageSet.GetImage(SplitterHoverImageName);
			sortAscendImage		= imageSet.GetImage(SortUpImageName);
			sortDescendImage	= imageSet.GetImage(SortDownImageName);

			normalMouseCursor	= imageSet.GetImage(NormalMouseCursorImageName);
			sizingMouseCursor	= imageSet.GetImage(SizingMouseCursorImageName);
			movingMouseCursor	= imageSet.GetImage(MovingMouseCursorImageName);
		}
		#endregion

		#region Members

		/// <summary>
		///		Perform rendering for this widget.
		/// </summary>
		/// <param name="z">Z value to use for rendering.</param>
		protected override void DrawSelf(float z) 
		{
			// get the destination screen area for this window
			Rect absRect = UnclippedPixelRect;

			// get clipping area for window
			Rect clipper = PixelRect;

			Vector3 position = new Vector3(absRect.Left, absRect.Top, z);

			// if widget is not totally clipped
			if (clipper.Width != 0) {
				// draw 'real' copy of widget
				RenderSegmentImagery(position, EffectiveAlpha, clipper);
			}

			// draw ghost copy of segment if being dragged (ignoring clipping)
			if (dragMoving) {
				clipper = GuiSystem.Instance.Renderer.Rect;

				position.x = absRect.Left + dragPosition.x;
				position.y = absRect.Top + dragPosition.y;

				// render ghost infront of everything else.
				position.z = 0.0f;

				// TODO: Magic number removal?
				RenderSegmentImagery(position, EffectiveAlpha * 0.5f, clipper);
			}
		}

		/// <summary>
		/// Method to draw a copy of the widget.
		/// </summary>
		/// <param name="position">Screen position to draw at</param>
		/// <param name="alpha">Alpha (transparency) value to use</param>
		/// <param name="clipper">Clipping rectangle to use when drawing</param>
		protected void RenderSegmentImagery(Vector3 position, float alpha, Rect clipper)
		{
			Rect absRect = new Rect(position.x, position.y, position.x + AbsoluteWidth, position.y + AbsoluteHeight);
			Rect destRect = absRect;

			// calculate colors to use.
			ColorRect colors = new ColorRect(new Color(alpha, 1, 1, 1));

			// draw the main images
			destRect.Right -= splitterNormalImage.Width;

			if ((segmentHover != segmentPushed) && !splitterHover && Clickable) {
				backHoverImage.Draw(destRect, position.z, clipper, colors);
			}
			else {
				backNormalImage.Draw(destRect, position.z, clipper, colors);
			}

			// draw splitter
			destRect.Left = destRect.Right;
			destRect.Right = absRect.Right;

			if (splitterHover) {
				splitterHoverImage.Draw(destRect, position.z, clipper, colors);
			}
			else {
				splitterNormalImage.Draw(destRect, position.z, clipper, colors);
			}

			//
			// adjust clipper to be inside the splitter bar imagery
			//
			destRect.Right = destRect.Left;

			// pad left by half the width of the 'sort' icon.
			destRect.Left = absRect.Left + sortAscendImage.Width * 0.5f;
			Rect inner_clip = destRect.GetIntersection(clipper);

			// TODO: Remove this hack once we have a stable sort up and running in the renderer
			position.z = GuiSystem.Instance.Renderer.GetZLayer(1);

			//
			// Render 'sort' icon as needed
			//
			if (sortingDirection == SortDirection.Ascending) {
				sortAscendImage.Draw(new Vector3(destRect.Left, destRect.Top + sortAscendImage.Height * 0.5f, position.z), inner_clip, colors);
				destRect.Left += sortAscendImage.Width * 1.5f;
			}
			else if (sortingDirection == SortDirection.Descending) {
				sortDescendImage.Draw(new Vector3(destRect.Left, destRect.Top + sortDescendImage.Height * 0.5f, position.z), inner_clip, colors);
				destRect.Left += sortDescendImage.Width * 1.5f;
			}

			//
			// Render the text
			//
			// find a font to be used.
			Font font;
			if (Font != null) {
				font = Font;
			}
			else if (Parent != null) {
				font = Parent.Font;
			}
			else {
				font = GuiSystem.Instance.DefaultFont;
			}

			// draw text centred text vertically
			destRect.Top += ((destRect.Height - font.LineSpacing) * 0.5f);
			font.DrawText(Text, destRect, position.z, inner_clip, HorizontalTextFormat.Left, colors);
		}

		#endregion

	}

}
