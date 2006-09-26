using System;
using System.Drawing;
using CeGuiSharp;
using CeGuiSharp.Widgets;

namespace CeGuiSharp.WidgetSets.WindowsLook {

	/// <summary>
	/// 
	/// </summary>
	public class WLEditBox : EditBox
	{
		#region Constants

		// image name constants
		/// <summary>
		/// 
		/// </summary>
		protected const string	ImagesetName				= "WindowsLook";
		/// <summary>
		/// 
		/// </summary>
		protected const string	TopLeftFrameImageName		= "EditFrameTopLeft";
		/// <summary>
		/// 
		/// </summary>
		protected const string	TopRightFrameImageName		= "EditFrameTopRight";
		/// <summary>
		/// 
		/// </summary>
		protected const string	BottomLeftFrameImageName	= "EditFrameBottomLeft";
		/// <summary>
		/// 
		/// </summary>
		protected const string	BottomRightFrameImageName	= "EditFrameBottomRight";
		/// <summary>
		/// 
		/// </summary>
		protected const string	LeftFrameImageName			= "EditFrameLeft";
		/// <summary>
		/// 
		/// </summary>
		protected const string	RightFrameImageName		= "EditFrameRight";
		/// <summary>
		/// 
		/// </summary>
		protected const string	TopFrameImageName			= "EditFrameTop";
		/// <summary>
		/// 
		/// </summary>
		protected const string	BottomFrameImageName		= "EditFrameBottom";
		/// <summary>
		/// 
		/// </summary>
		protected const string	BackgroundImageName		= "Background";
		/// <summary>
		/// 
		/// </summary>
		protected const string	CaratImageName				= "EditBoxCarat";
		/// <summary>
		/// 
		/// </summary>
		protected const string	SelectionBrushImageName	= "Background";
		/// <summary>
		/// 
		/// </summary>
		protected const string	MouseCursorImageName		= "MouseTextBar";

		// layout values
		/// <summary>
		/// 
		/// </summary>
		protected const float	TextPaddingRatio		= 1.0f;

		// colours
		/// <summary>
		/// 
		/// </summary>
		protected static Color ReadWriteBackgroundColor	= new Color(0xFFFFFF);
		/// <summary>
		/// 
		/// </summary>
		protected static Color ReadOnlyBackgroundColor	= new Color(0xDFDFDF);
		/// <summary>
		/// 
		/// </summary>
		protected static Color DefaultNormalTextColor			= new Color(0x000000);
		/// <summary>
		/// 
		/// </summary>
		protected static Color DefaultSelectedTextColor			= new Color(0xFFFFFF);
		/// <summary>
		/// 
		/// </summary>
		protected static Color NormalSelectionColor		= new Color(0x607FFF);
		/// <summary>
		/// 
		/// </summary>
		protected static Color InactiveSelectionColor		= new Color(0x808080);

		// implementation constants
		/// <summary>
		/// 
		/// </summary>
		protected const int	ContainerLayer	= 1;
		/// <summary>
		/// 
		/// </summary>
		protected const int	SelectionLayer	= 2;
		/// <summary>
		/// 
		/// </summary>
		protected const int	TextLayer		= 3;
		/// <summary>
		///
		/// </summary>
		protected const int	CaratLayer		= 4;

		#endregion

		#region Fields

		/// <summary>
		/// 
		/// </summary>
		protected RenderableFrame frame = new RenderableFrame();

		// images
		/// <summary>
		/// 
		/// </summary>
		protected Image background;
		/// <summary>
		/// 
		/// </summary>
		protected Image carat;
		/// <summary>
		/// 
		/// </summary>
		protected Image selection;

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

		// rendering internal vars
		/// <summary>
		/// 
		/// </summary>
		protected float lastTextOffset;

		#endregion

		#region Constructor

		/// <summary>
		/// 
		/// </summary>
		/// <param name="name"></param>
		public WLEditBox( string type, string name ) : base(type, name) {
			Imageset imageSet = ImagesetManager.Instance.GetImageset(ImagesetName);

			frame.SetImages(
				imageSet.GetImage(TopLeftFrameImageName),
				imageSet.GetImage(TopRightFrameImageName),
				imageSet.GetImage(BottomLeftFrameImageName),
				imageSet.GetImage(BottomFrameImageName),
				imageSet.GetImage(LeftFrameImageName),
				imageSet.GetImage(TopFrameImageName),
				imageSet.GetImage(RightFrameImageName),
				imageSet.GetImage(BottomFrameImageName));

			background = imageSet.GetImage(BackgroundImageName);
			carat = imageSet.GetImage(CaratImageName);
			selection = imageSet.GetImage(SelectionBrushImageName);

			SetMouseCursor(imageSet.GetImage(MouseCursorImageName));

			StoreFrameSizes();

			// set up colors
			normalTextColor = DefaultNormalTextColor;
			selectTextColor = DefaultSelectedTextColor;
			selectBrushColor = NormalSelectionColor;
			inactiveSelectBrushColor = InactiveSelectionColor;
		}

		#endregion

		#region Methods

		/// <summary>
		/// 
		/// </summary>
		protected void StoreFrameSizes() {
			Imageset imageSet = ImagesetManager.Instance.GetImageset(ImagesetName);

			frameLeftSize	= imageSet.GetImage(TopLeftFrameImageName).Width;
			frameRightSize	= imageSet.GetImage(TopRightFrameImageName).Width;
			frameTopSize	= imageSet.GetImage(TopFrameImageName).Height;
			frameBottomSize = imageSet.GetImage(BottomFrameImageName).Height;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		protected float GetTextPaddingPixels() {
			return frameLeftSize * TextPaddingRatio;
		}

		#endregion

		#region EditBox Methods

		/// <summary>
		/// 
		/// </summary>
		/// <param name="point"></param>
		/// <returns></returns>
		protected override int GetTextIndexFromPosition(Point point) {
			// calculate final window position to be checked
			float wndx = ScreenToWindowX(point.x);

			if(MetricsMode == MetricsMode.Relative) {
				wndx = RelativeToAbsoluteX(wndx);
			}

			wndx -= lastTextOffset;

			// return the proper index
			if(TextMasked) {
				return Font.GetCharAtPixel("".PadRight(text.Length, MaskCodePoint), 0, wndx);
			}
			else {
				return Font.GetCharAtPixel(text, 0, wndx);
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="z"></param>
		protected override void DrawSelf(float z) 
		{
			Rect clipper = PixelRect;

			// do nothing if the widget is totally clipped
			if(clipper.Width == 0) {
				return;
			}

			Font fnt = Font;
			Renderer renderer = GuiSystem.Instance.Renderer;
			bool hasFocus = HasInputFocus;

			// get the destination screen rect for this window
			Rect absRect = UnclippedPixelRect;

			// calculate colors to use
			ColorRect colors = new ColorRect(ReadOnly ? ReadOnlyBackgroundColor : ReadWriteBackgroundColor);
			colors.SetAlpha(EffectiveAlpha);

			// render container
			Vector3 pos = new Vector3(absRect.Left, absRect.Top, renderer.GetZLayer(ContainerLayer));
			frame.Draw(pos, clipper);

			// calculate inner area rect considering frame
			absRect.Left += frameLeftSize;
			absRect.Top += frameTopSize;
			absRect.Right -= frameRightSize;
			absRect.Bottom -= frameBottomSize;

			// draw background image
			background.Draw(absRect, pos.z, clipper, colors);

			// Required preliminary work for main rendering operations
			//
			// Create a 'masked' version of the string if needed.
			string editText;

			if(TextMasked) {
				editText = new string(MaskCodePoint, text.Length);
			}
			else {
				editText = text;
			}

			// calculate best position to render text to ensure carat is always visible
			float textOffset;
			float extentToCarat = fnt.GetTextExtent(editText.Substring(0, CaratIndex));

			// if box is inactive
			if(!hasFocus) {
				textOffset = lastTextOffset;
			}
			// if carat is to the left of the box
			else if((lastTextOffset + extentToCarat) < 0) {
				textOffset = -extentToCarat;
			}
			// if carat is off to the right.
			else if((lastTextOffset + extentToCarat) >= (absRect.Width - carat.Width)) {
				textOffset = absRect.Width - extentToCarat - carat.Width;
			}
			// else carat is already within the box
			else {
				textOffset = lastTextOffset;
			}

			// adjust clipper for new target area
			clipper = absRect.GetIntersection(clipper);

			// render carat
			if((!ReadOnly) && hasFocus) {
				pos = new Vector3(absRect.Left + textOffset + extentToCarat, absRect.Top, renderer.GetZLayer(CaratLayer));
				SizeF sz = new SizeF(carat.Width, absRect.Height);
				carat.Draw(pos, sz, clipper, colors);
			}

			// draw label text
			//
			// setup initial rect for text formatting
			Rect textRect = absRect;
			textRect.Top += (textRect.Height - Font.LineSpacing) * 0.5f;
			textRect.Left += textOffset;

			// draw pre-highlight text
			String sect = editText.Substring(0, SelectionStartIndex);
			colors = new ColorRect(normalTextColor);
			colors.SetAlpha(EffectiveAlpha);
			fnt.DrawText(sect, textRect, renderer.GetZLayer(TextLayer), clipper, HorizontalTextFormat.Left, colors);

			textRect.Left += fnt.GetTextExtent(sect);

			// draw highlight text
			sect = editText.Substring(SelectionStartIndex, SelectionLength);
			colors = new ColorRect(selectTextColor);
			colors.SetAlpha(EffectiveAlpha);
			fnt.DrawText(sect, textRect, renderer.GetZLayer(TextLayer), clipper, HorizontalTextFormat.Left, colors);

			textRect.Left += fnt.GetTextExtent(sect);

			// draw post-highlight text
			sect = editText.Substring(SelectionEndIndex);
			colors = new ColorRect(normalTextColor);
			colors.SetAlpha(EffectiveAlpha);
			fnt.DrawText(sect, textRect, renderer.GetZLayer(TextLayer), clipper, HorizontalTextFormat.Left, colors);

			// render selection brush
			if(SelectionLength > 0) {
				// calculate required start and end offsets
				float selStartOffset = fnt.GetTextExtent(editText.Substring(0, SelectionStartIndex));
				float selEndOffset = fnt.GetTextExtent(editText.Substring(0, SelectionEndIndex));

				// setup colors
				if(hasFocus && !ReadOnly) {
					colors = new ColorRect(selectBrushColor);
				}
				else {
					colors = new ColorRect(inactiveSelectBrushColor);
				}
				colors.SetAlpha(EffectiveAlpha);

				// calculate highlight area
				Rect hlarea = new Rect();
				hlarea.Left = absRect.Left + textOffset + selStartOffset;
				hlarea.Right = absRect.Left + textOffset + selEndOffset;
				hlarea.Top = textRect.Top;
				hlarea.Bottom = hlarea.Top + fnt.LineSpacing;

				// render the highlight
				selection.Draw(hlarea, renderer.GetZLayer(SelectionLayer), clipper, colors);
			}

			lastTextOffset = textOffset;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="e"></param>
		protected override void OnSized(GuiEventArgs e) 
		{
			base.OnSized(e);

			// update frame size
			frame.Size = AbsoluteSize;

			e.Handled = true;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="e"></param>
		protected override void OnAlphaChanged(GuiEventArgs e) 
		{
			base.OnAlphaChanged(e);

			frame.Colors.SetAlpha(EffectiveAlpha);
		}

		#endregion
	}
}
