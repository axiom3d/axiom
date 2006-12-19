using System;
using System.Drawing;
using CeGuiSharp;
using CeGuiSharp.Widgets;

namespace CeGuiSharp.WidgetSets.TaharezLook {
	/// <summary>
	///		Single line editbox.
	/// </summary>
	public class TLEditBox : EditBox {
		#region Constants

		/// <summary>
		///		Name of the Imageset containing the imagery to use.
		/// </summary>
		const string	ImagesetName				= "TaharezLook";
		/// <summary>
		///		Name of the image to use for the left end of the container.
		/// </summary>
		const string	ContainerLeftImageName		= "EditBoxLeft";
		/// <summary>
		///		Name of the image to use for the middle of the container.
		/// </summary>
		const string	ContainerMiddleImageName	= "EditBoxMiddle";
		/// <summary>
		///		Name of the image to use for the right end of the container.
		/// </summary>
		const string	ContainerRightImageName		= "EditBoxRight";
		/// <summary>
		///		Name of the image to use for the carat.
		/// </summary>
		const string	CaratImageName				= "EditBoxCarat";
		/// <summary>
		///		Name of the image to use for the selection brush.
		/// </summary>
		const string	SelectionBrushImageName		= "TextSelectionBrush";
		/// <summary>
		///		Name of the image used for the mouse cursor.
		/// </summary>
		const string	MouseCursorImageName		= "MouseTextBar";
		/// <summary>
		///		Used to generate padding distance for text.
		/// </summary>
		const float	TextPaddingRatio				= 0.5f;

		const int SelectionLayer					= 1;
		const int TextLayer							= 2;
		const int CaratLayer						= 3;

		#endregion Constants

		#region Fields

		/// <summary>
		/// 
		/// </summary>
		protected Image left;
		/// <summary>
		/// 
		/// </summary>
		protected Image middle;
		/// <summary>
		/// 
		/// </summary>
		protected Image right;
		/// <summary>
		/// 
		/// </summary>
		protected Image carat;
		/// <summary>
		/// 
		/// </summary>
		protected Image selection;

		/// <summary>
		/// 
		/// </summary>
		protected float lastTextOffset;

		#endregion Fields

		#region Constructor

		/// <summary>
		///		Constructor.
		/// </summary>
		/// <param name="name">Name of this widget.</param>
		public TLEditBox(string type, string name) : base(type, name) {
		}

		#endregion Constructor

		#region Editbox Members

		/// <summary>
		/// 
		/// </summary>
		/// <param name="point"></param>
		/// <returns></returns>
		protected override int GetTextIndexFromPosition(Point point) {
			//
			// calculate final window position to be checked
			//
			float wndx = ScreenToWindowX(point.x);

			if (this.MetricsMode == MetricsMode.Relative) {
				wndx = RelativeToAbsoluteX(wndx);
			}

			wndx -= lastTextOffset;

			//
			// Return the proper index
			//
			if (TextMasked) {
				return this.Font.GetCharAtPixel("".PadRight(text.Length, MaskCodePoint), 0, wndx);
			}
			else {
				return this.Font.GetCharAtPixel(text, 0, wndx);
			}
		}

		/// <summary>
		/// 
		/// </summary>
		public override void Initialize() {
			base.Initialize ();

			Imageset imageSet = ImagesetManager.Instance.GetImageset(ImagesetName);

			// cache images to use
			left = imageSet.GetImage(ContainerLeftImageName);
			middle = imageSet.GetImage(ContainerMiddleImageName);
			right = imageSet.GetImage(ContainerRightImageName);
			carat = imageSet.GetImage(CaratImageName);
			selection = imageSet.GetImage(SelectionBrushImageName);

			SetMouseCursor(imageSet.GetImage(MouseCursorImageName));
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		protected float GetTextPaddingPixels() {
			return left.Width * TextPaddingRatio;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="z"></param>
		protected override void DrawSelf(float z) {
			Rect clipper = PixelRect;

			// do nothing if the widget is totally clipped.
			if (clipper.Width == 0) {
				return;
			}

			Font fnt = this.Font;
			Renderer renderer = GuiSystem.Instance.Renderer;

			// get the destination screen rect for this window
			Rect absrect = UnclippedPixelRect;

			// calculate colours to use.
			Color color = new Color(EffectiveAlpha, 1, 1, 1);
			ColorRect colors = new ColorRect(color, color, color, color);

			bool hasFocus = HasInputFocus;

			//
			// render container
			//
			// calculate widths for container segments
			float leftWidth		= left.Width;
			float rightWidth	= right.Width;
			float midWidth		= absrect.Width - leftWidth - rightWidth;

			Vector3 pos = new Vector3(absrect.Left, absrect.Top, z);
			SizeF sz = new SizeF(leftWidth, absrect.Height);

			// left end
			left.Draw(pos, sz, clipper, colors);

			// stretchy middle segment
			pos.x += sz.Width;
			sz.Width = midWidth;
			middle.Draw(pos, sz, clipper, colors);

			// right end
			pos.x += sz.Width;
			sz.Width = rightWidth;
			right.Draw(pos, sz, clipper, colors);

			//
			// Required preliminary work for main rendering operations
			//
			// Create a 'masked' version of the string if needed.
			string editText = "";

			if (TextMasked) {
				editText = editText.PadRight(text.Length, MaskCodePoint);
			}
			else {
				editText = text;
			}

			// calculate new area rect considering text padding value.
			float textpadding = GetTextPaddingPixels();

			absrect.Left		+= textpadding;
			absrect.Top			+= textpadding;
			absrect.Right		-= textpadding;
			absrect.Bottom		-= textpadding;

			// calculate best position to render text to ensure carat is always visible
			float textOffset = 0;
			float extentToCarat = fnt.GetTextExtent(editText.Substring(0, CaratIndex));

			// if box is inactive
			if (!hasFocus) {
				textOffset = lastTextOffset;
			}
			// if carat is to the left of the box
			else if ((lastTextOffset + extentToCarat) < 0) {
				textOffset = -extentToCarat;
			}
				// if carat is off to the right.
			else if ((lastTextOffset + extentToCarat) >= (absrect.Width - carat.Width)) {
				textOffset = absrect.Width - extentToCarat - carat.Width;
			}
				// else carat is already within the box
			else {
				textOffset = lastTextOffset;
			}

			// adjust clipper for new target area
			clipper = absrect.GetIntersection(clipper);

			//
			// Render carat
			//
			if (!ReadOnly && hasFocus) {
				Vector3 pos2 = new Vector3(absrect.Left + textOffset + extentToCarat, absrect.Top, renderer.GetZLayer(CaratLayer));
				SizeF sz2 = new SizeF(carat.Width, absrect.Height);
				carat.Draw(pos2, sz2, clipper, colors);
			}

			//
			// Draw label text
			//
			// setup initial rect for text formatting
			Rect textRect = absrect;
			textRect.Top += (textRect.Height - this.Font.LineSpacing) * 0.5f;
			textRect.Left += textOffset;

			// draw pre-highlight text
			string sect = editText.Substring(0, SelectionStartIndex);
			Color tmp = normalTextColor;
			tmp.a = EffectiveAlpha;
			colors = new ColorRect(tmp, tmp, tmp, tmp);
			fnt.DrawText(sect, textRect, renderer.GetZLayer(TextLayer), clipper, HorizontalTextFormat.Left, colors);
			textRect.Left += fnt.GetTextExtent(sect);

			// draw highlight text
			sect = editText.Substring(SelectionStartIndex, SelectionLength);
			tmp = selectTextColor;
			tmp.a = EffectiveAlpha;
			colors = new ColorRect(tmp, tmp, tmp, tmp);
			fnt.DrawText(sect, textRect, renderer.GetZLayer(TextLayer), clipper, HorizontalTextFormat.Left, colors);

			textRect.Left += fnt.GetTextExtent(sect);

			// draw post-highlight text
			sect = editText.Substring(SelectionEndIndex);
			tmp = normalTextColor;
			tmp.a = EffectiveAlpha;
			colors = new ColorRect(tmp, tmp, tmp, tmp);
			fnt.DrawText(sect, textRect, renderer.GetZLayer(TextLayer), clipper, HorizontalTextFormat.Left, colors);

			//
			// Render selection brush
			//
			if (SelectionLength != 0) {
				// calculate required start and end offsets
				float selStartOffset	= fnt.GetTextExtent(editText.Substring(0, SelectionStartIndex));
				float selEndOffset		= fnt.GetTextExtent(editText.Substring(0, SelectionEndIndex));

				// setup colours
				if (hasFocus && (!ReadOnly)) {
					tmp = selectBrushColor;
					tmp.a = EffectiveAlpha;
				}
				else {
					tmp = inactiveSelectBrushColor;
					tmp.a = EffectiveAlpha;
				}

				colors = new ColorRect(tmp, tmp, tmp, tmp);

				// calculate highlight area
				Rect hlarea		= new Rect();
				hlarea.Left		= absrect.Left + textOffset + selStartOffset;
				hlarea.Right	= absrect.Left + textOffset + selEndOffset;
				hlarea.Top		= textRect.Top;
				hlarea.Bottom	= hlarea.Top + fnt.LineSpacing;

				// render the highlight
				selection.Draw(hlarea, renderer.GetZLayer(SelectionLayer), clipper, colors);
			}

			lastTextOffset = textOffset;
		}

		#endregion Editbox Members
	}
}
