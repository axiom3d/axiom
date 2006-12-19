using System;
using CeGuiSharp;
using CeGuiSharp.Widgets;

namespace CeGuiSharp.WidgetSets.WindowsLook {

	/// <summary>
	/// ListHeaderSegment widget for the Windows Gui Scheme.
	/// </summary>
	public class WLListHeaderSegment : ListHeaderSegment
	{
		#region Constants

		/// <summary>
		/// Name of the imageset to use for rendering.
		/// </summary>
		const string	ImagesetName				= "WindowsLook";
		/// <summary>
		/// Image to use for segment backdrop in normal state.
		/// </summary>
		const string	BackdropMainImageName	= "HeaderMainBrush";
		/// <summary>
		/// Image to use for segment backdrop edge in normal state.
		/// </summary>
		const string	BackdropEdgeImageName	= "HeaderBottomEdge";
		/// <summary>
		/// Image to use for splitter / sizing bar in normal state.
		/// </summary>
		const string	SplitterImageName			= "HeaderSplitter";
		/// <summary>
		/// Image to use for 'sort ascending' indicator.
		/// </summary>
		const string	SortUpImageName			= "SmallUpArrow";
		/// <summary>
		/// Image to use for 'sort descending' indicator.
		/// </summary>
		const string	SortDownImageName			= "SmallDownArrow";
		/// <summary>
		/// Image to use for mouse when not sizing.
		/// </summary>
		const string	NormalMouseCursor			= "MouseArrow";
		/// <summary>
		/// Image to use for mouse when sizing.
		/// </summary>
		const string	SizingMouseCursor			= "MouseEsWeCursor";
		/// <summary>
		/// Image to use for mouse when moving.
		/// </summary>
		const string	MovingMouseCursor			= "MouseMoveCursor";

		/// <summary>
		/// Backdrop color in normal mode.
		/// </summary>
		static readonly Color BackdropNormalColor		= new Color(0xDDDDDD);
		/// <summary>
		/// Backdrop color in highlight mode.
		/// </summary>
		static readonly Color BackdropHighlightColor	= new Color(0xEFEFEF);
		/// <summary>
		/// Splitter color in normal mode.
		/// </summary>
		static readonly Color SplitterNormalColor		= new Color(0xDDDDDD);
		/// <summary>
		/// Splitter color in highlight mode.
		/// </summary>
		static readonly Color SplitterHighlightColor	= new Color(0xEFEFEF);

		#endregion

		#region Fields

		/// <summary>
		/// image for backdrop.
		/// </summary>
		protected Image backdropMainImage;
		/// <summary>
		/// image for backdrop edge.
		/// </summary>
		protected Image backdropEdgeImage;
		/// <summary>
		/// image for splitter.
		/// </summary>
		protected Image splitterImage;
		/// <summary>
		/// image for 'sort ascending' icon.
		/// </summary>
		protected Image sortAscendImage;
		/// <summary>
		/// image for 'sort descending' icon.
		/// </summary>
		protected Image sortDescendImage;

		#endregion

		#region Constructor

		/// <summary>
		/// 
		/// </summary>
		/// <param name="name"></param>
		public WLListHeaderSegment(string type, string name ) : base(type, name) {
			Imageset imageSet = ImagesetManager.Instance.GetImageset(ImagesetName);

			backdropMainImage = imageSet.GetImage(BackdropMainImageName);
			backdropEdgeImage = imageSet.GetImage(BackdropEdgeImageName);
			splitterImage = imageSet.GetImage(SplitterImageName);
			sortAscendImage = imageSet.GetImage(SortUpImageName);
			sortDescendImage = imageSet.GetImage(SortDownImageName);
			normalMouseCursor = imageSet.GetImage(NormalMouseCursor);
			sizingMouseCursor = imageSet.GetImage(SizingMouseCursor);
			movingMouseCursor = imageSet.GetImage(MovingMouseCursor);

			SetMouseCursor(imageSet.GetImage(NormalMouseCursor));
		}

		#endregion

		#region ListHeaderSegment Members

		/// <summary>
		///		Perform rendering for this widget.
		/// </summary>
		/// <param name="z">Z value to use for rendering.</param>
		protected override void DrawSelf(float z) {
			// get the destination screen rect for this widget
			Rect absRect = UnclippedPixelRect;

			// get the clipping rect for window
			Rect clipper = PixelRect;

			Vector3 pos = new Vector3(absRect.Left, absRect.Top, z);

			// if widget is not totally clipped
			if(clipper.Width != 0) {
				RenderSegmentImagery(pos, EffectiveAlpha, clipper);
			}

			// always draw the ghost if the segment is being dragged
			if(dragMoving) {
				clipper = GuiSystem.Instance.Renderer.Rect;
				pos.x = absRect.Left + dragPosition.x;
				pos.y = absRect.Top + dragPosition.y;
				pos.z = 0.0f;
				RenderSegmentImagery(pos, EffectiveAlpha * 0.5f, clipper);
			}
		}

		/// <summary>
		/// Method to draw a copy of the widget.
		/// </summary>
		/// <param name="position">Screen position to draw at</param>
		/// <param name="alpha">Alpha (transparency) value to use</param>
		/// <param name="clipper">Clipping rectangle to use when drawing</param>
		protected void RenderSegmentImagery(Vector3 position, float alpha, Rect clipper) {
			Rect absRect = new Rect(position.x, position.y, position.x + AbsoluteWidth, position.y + AbsoluteHeight);
			Rect destRect = absRect;

			// calculate colors to use
			ColorRect colors;
			if((segmentHover != segmentPushed) && !splitterHover && !Clickable) {
				colors = new ColorRect(BackdropHighlightColor);
			}
			else {
				colors = new ColorRect(BackdropNormalColor);
			}
			colors.SetAlpha(alpha);

			// draw the main area
			destRect.Right -= splitterImage.Width;
			destRect.Bottom -= backdropEdgeImage.Height;
			backdropMainImage.Draw(destRect, position.z, clipper, colors);

			// draw bottom edge
			destRect.Top = destRect.Bottom;
			destRect.Bottom = absRect.Bottom;
			backdropEdgeImage.Draw(destRect, position.z, clipper, colors);

			// draw splitter
			destRect.Top = absRect.Top;
			destRect.Left = destRect.Right;
			destRect.Right = absRect.Right;

			if(splitterHover) {
				colors = new ColorRect(SplitterHighlightColor);
			}
			else {
				colors = new ColorRect(SplitterNormalColor);
			}
			colors.SetAlpha(alpha);
			splitterImage.Draw(destRect, position.z, clipper, colors);

			// adjust clipper to be inside the splitter bar imagery
			destRect.Right = destRect.Left;

			// pad left by half the width of the 'sort' icon
			destRect.Left = absRect.Left + sortAscendImage.Width * 0.5f;
			Rect inner_clip = destRect.GetIntersection(clipper);

			// Render 'sort' icon as needed
			colors = new ColorRect(new Color(alpha, 1.0f, 1.0f, 1.0f));

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

			colors = new ColorRect(new Color(alpha, 0.0f, 0.0f, 0.0f));

			// draw text centred text vertically
			destRect.Top += ((destRect.Height - font.LineSpacing) * 0.5f);
			font.DrawText(Text, destRect, position.z, inner_clip, HorizontalTextFormat.Left, colors);
		}

		#endregion
	}
}
