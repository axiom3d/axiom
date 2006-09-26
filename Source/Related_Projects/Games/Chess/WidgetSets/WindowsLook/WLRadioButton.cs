using System;
using CeGuiSharp;
using CeGuiSharp.Widgets;

namespace CeGuiSharp.WidgetSets.WindowsLook {

	/// <summary>
	/// 
	/// </summary>
	public class WLRadioButton : RadioButton
	{
		#region Constants

		/// <summary>
		/// 
		/// </summary>
		protected const string	ImagesetName					= "WindowsLook";
		/// <summary>
		/// 
		/// </summary>
		protected const string	NormalImageName		= "RadioButtonNormal";
		/// <summary>
		/// 
		/// </summary>
		protected const string	HighlightImageName	= "RadioButtonHover";
		/// <summary>
		/// 
		/// </summary>
		protected const string	SelectMarkImageName	= "RadioButtonMark";

		/// <summary>
		/// 
		/// </summary>
		protected const float	LabelPadding				= 4.0f;

		// colours
		/// <summary>
		/// 
		/// </summary>
		protected static Color EnabledTextLabelColor  = new Color(0x000000);
		/// <summary>
		/// 
		/// </summary>
		protected static Color DisabledTextLabelColor = new Color(0x888888);

		const int CheckLayer = 1;
		const int TextLayer = 2;

		#endregion

		#region Fields

		/// <summary>
		/// 
		/// </summary>
		protected Image normalImage;
		/// <summary>
		/// 
		/// </summary>
		protected Image hoverImage;
		/// <summary>
		/// 
		/// </summary>
		protected Image selectMarkImage;

		#endregion

		#region Constructor

		/// <summary>
		/// 
		/// </summary>
		/// <param name="name"></param>
		public WLRadioButton( string type, string name ) : base(type, name) {
			Imageset imageSet = ImagesetManager.Instance.GetImageset(ImagesetName);

			// setup images
			normalImage = imageSet.GetImage(NormalImageName);
			hoverImage = imageSet.GetImage(HighlightImageName);
			selectMarkImage = imageSet.GetImage(SelectMarkImageName);

			// set default colors for text
			normalColor = EnabledTextLabelColor;
			hoverColor = EnabledTextLabelColor;
			pushedColor = EnabledTextLabelColor;
			disabledColor = DisabledTextLabelColor;
		}

		#endregion

		#region Base Members

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

			// get the destination screen rect for this window
			Rect absRect = UnclippedPixelRect;

			// calculate colors to use
			ColorRect colors = new ColorRect(new Color(EffectiveAlpha, 1.0f, 1.0f, 1.0f));

			// draw the images
			Vector3 pos = new Vector3(absRect.Left, absRect.Top + ((absRect.Height - normalImage.Height) / 2), z);
			normalImage.Draw(pos, clipper, colors);

			if(Checked) {
				pos.z = GuiSystem.Instance.Renderer.GetZLayer(CheckLayer);
				selectMarkImage.Draw(pos, clipper, colors);
			}

			// draw label text
			absRect.Top += (absRect.Height - Font.LineSpacing) * 0.5f;
			absRect.Left += normalImage.Width;
			colors = new ColorRect(normalColor);
			colors.SetAlpha(EffectiveAlpha);
			Font.DrawText(Text, absRect, GuiSystem.Instance.Renderer.GetZLayer(TextLayer), clipper, HorizontalTextFormat.Left, colors);
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

			// get the destination screen rect for this window
			Rect absRect = UnclippedPixelRect;

			// calculate colors to use
			ColorRect colors = new ColorRect(new Color(EffectiveAlpha, 1.0f, 1.0f, 1.0f));

			// draw the images
			Vector3 pos = new Vector3(absRect.Left, absRect.Top + ((absRect.Height - hoverImage.Height) / 2), z);
			hoverImage.Draw(pos, clipper, colors);

			if(Checked) {
				pos.z = GuiSystem.Instance.Renderer.GetZLayer(CheckLayer);
				selectMarkImage.Draw(pos, clipper, colors);
			}

			// draw label text
			absRect.Top += (absRect.Height - Font.LineSpacing) * 0.5f;
			absRect.Left += hoverImage.Width;
			colors = new ColorRect(hoverColor);
			colors.SetAlpha(EffectiveAlpha);
			Font.DrawText(Text, absRect, GuiSystem.Instance.Renderer.GetZLayer(TextLayer), clipper, HorizontalTextFormat.Left, colors);
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

			// get the destination screen rect for this window
			Rect absRect = UnclippedPixelRect;

			// calculate colors to use
			ColorRect colors = new ColorRect(new Color(EffectiveAlpha, 1.0f, 1.0f, 1.0f));

			// draw the images
			Vector3 pos = new Vector3(absRect.Left, absRect.Top + ((absRect.Height - normalImage.Height) / 2), z);
			normalImage.Draw(pos, clipper, colors);

			if(Checked) {
				pos.z = GuiSystem.Instance.Renderer.GetZLayer(CheckLayer);
				selectMarkImage.Draw(pos, clipper, colors);
			}

			// draw label text
			absRect.Top += (absRect.Height - Font.LineSpacing) * 0.5f;
			absRect.Left += normalImage.Width;
			colors = new ColorRect(pushedColor);
			colors.SetAlpha(EffectiveAlpha);
			Font.DrawText(Text, absRect, GuiSystem.Instance.Renderer.GetZLayer(TextLayer), clipper, HorizontalTextFormat.Left, colors);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="z"></param>
		protected override void DrawDisabled(float z) {
			Rect clipper = PixelRect;

			// do nothing if the widget is totally clipped
			if( clipper.Width == 0 ) {
				return;
			}

			// get the destination screen rect for this window
			Rect absRect = UnclippedPixelRect;

			// calculate colors to use
			ColorRect colors = new ColorRect(new Color(EffectiveAlpha, 1.0f, 1.0f, 1.0f));

			// draw the images
			Vector3 pos = new Vector3(absRect.Left, absRect.Top + ((absRect.Height - normalImage.Height) / 2), z);
			normalImage.Draw(pos, clipper, colors);

			if(Checked) {
				pos.z = GuiSystem.Instance.Renderer.GetZLayer(CheckLayer);
				selectMarkImage.Draw(pos, clipper, colors);
			}

			// draw label text
			absRect.Top += (absRect.Height - Font.LineSpacing) * 0.5f;
			absRect.Left += normalImage.Width;
			colors = new ColorRect(disabledColor);
			colors.SetAlpha(EffectiveAlpha);
			Font.DrawText(Text, absRect, GuiSystem.Instance.Renderer.GetZLayer(TextLayer), clipper, HorizontalTextFormat.Left, colors);
		}

		#endregion
	}

}
