using System;
using CeGuiSharp;
using CeGuiSharp.Widgets;

namespace CeGuiSharp.WidgetSets.TaharezLook {
	/// <summary>
	/// Summary description for TLCheckbox.
	/// </summary>
	public class TLCheckbox : Checkbox {
		#region Constants

		/// <summary>
		///		Name of the imageset to use for rendering.
		/// </summary>
		const string ImagesetName			= "TaharezLook";
		/// <summary>
		///		Name of the image to use for the normal state.
		/// </summary>
		const string NormalImageName		= "CheckboxNormal";
		/// <summary>
		///		Name of the image to use for the highlighted state.
		/// </summary>
		const string HighlightImageName		= "CheckboxHover";
		/// <summary>
		///		Name of the image to use for the check / selected mark.
		/// </summary>
		const string CheckMarkImageName		= "CheckboxMark";

		/// <summary>
		///		Pixel padding value for text label (space between image and text label).
		/// </summary>
		const float	LabelPadding			= 4.0f;

		#endregion Constants

		#region Fields

		/// <summary>
		///		Image to use when rendering in normal state.
		/// </summary>
		protected Image	normalImage;
		/// <summary>
		///		Image to use when rendering in hover / highlighted state.
		/// </summary>
		protected Image	hoverImage;
		/// <summary>
		///		Image to use when rendering the check-mark.
		/// </summary>
		protected Image	checkMarkImage;

		#endregion Fields

		#region Constructor

		/// <summary>
		///		Constructor.
		/// </summary>
		/// <param name="name"></param>
		public TLCheckbox(string type, string name) : base(type, name) {
			Imageset imageSet = ImagesetManager.Instance.GetImageset(ImagesetName);

			// setup cache of image pointers
			normalImage		= imageSet.GetImage(NormalImageName);
			hoverImage		= imageSet.GetImage(HighlightImageName);
			checkMarkImage	= imageSet.GetImage(CheckMarkImageName);
		}

		#endregion Constructor

		#region Methods

		/// <summary>
		///		Helper method for common rendering code for the checkmark.
		/// </summary>
		/// <param name="image">Image to use.</param>
		/// <param name="z">Z value to use.</param>
		/// <param name="drawDisabled">Will use white if false, or greyed if true.</param>
		protected void DrawCheckbox(Image image, float z, bool drawDisabled) {
			Rect clipper = PixelRect;

			// do nothing if the widget is totally clipped.
			if (clipper.Width == 0) {
				return;
			}

			// get the destination screen rect for this window
			Rect absRect = UnclippedPixelRect;

			Color color;

			if(drawDisabled) {
				color = DefaultDisabledLabelColor;
			}
			else {
				color = DefaultNormalLabelColor;
			}

			color.a = EffectiveAlpha;

			// calculate colors to use.
			ColorRect colors = new ColorRect(color, color, color, color);

			// draw the images
			Vector3 pos = new Vector3(absRect.Left, absRect.Top + (absRect.Height - image.Height) * 0.5f, z);

			image.Draw(pos, clipper, colors);

			// draw the check if need be
			if (isChecked) {
				// HACK: Find out why I need this and Paul doesn't
				pos.z = GuiSystem.Instance.Renderer.GetZLayer(1);

				checkMarkImage.Draw(pos, clipper, colors);
			}

			// Draw label text
			absRect.Top		+= (absRect.Height - this.Font.LineSpacing) * 0.5f;
			// TODO: Added padding, verify
			absRect.Left	+= (image.Width + 3);
			this.Font.DrawText(
				this.Text, absRect, 
				GuiSystem.Instance.Renderer.GetZLayer(1), clipper, HorizontalTextFormat.Left, colors);
		}

		#endregion Methods

		#region Window Members

		/// <summary>
		///		Render the checkbox in the 'normal' state.
		/// </summary>
		/// <param name="z">Z value for rendering.</param>
		protected override void DrawNormal(float z) {
			DrawCheckbox(normalImage, z, false);
		}
		
		/// <summary>
		///		Render the checkbox in the 'pushed' state.
		/// </summary>
		/// <param name="z">Z value for rendering.</param>
		protected override void DrawPushed(float z) {
			DrawCheckbox(normalImage, z, false);
		}

		/// <summary>
		///		Render the checkbox in the 'hover' state.
		/// </summary>
		/// <param name="z">Z value for rendering.</param>
		protected override void DrawHover(float z) {
			DrawCheckbox(hoverImage, z, false);
		}


		#endregion Window Members
	}
}
