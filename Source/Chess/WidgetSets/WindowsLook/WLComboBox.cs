using System;
using System.Drawing;
using CeGuiSharp;
using CeGuiSharp.Widgets;

namespace CeGuiSharp.WidgetSets.WindowsLook {
	/// <summary>
	/// Summary description for TLComboBox.
	/// </summary>
	public class WLComboBox : ComboBox {
		#region Constants

		const string	ImagesetName				= "WindowsLook";
		const string	ButtonNormalImageName		= "LargeDownArrow";
		const string	ButtonHighlightedImageName	= "LargeDownArrow";

		// component widget type names
		const string	EditboxTypeName				= "WindowsLook.WLEditBox";
		const string	DropListTypeName			= "WindowsLook.WLComboDropList";
		const string	ButtonTypeName				= "WindowsLook.WLButton";

		#endregion Costants

		/// <summary>
		/// 
		/// </summary>
		/// <param name="name"></param>
		public WLComboBox(string type, string name) : base(type, name) {
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="z"></param>
		protected override void DrawSelf(float z) {
			// do nothing, this is based off nothing but sub-widgets
		}

		/// <summary>
		/// 
		/// </summary>
		protected override void LayoutComponentWidgets() {
			Point	pos = new Point();
			SizeF	sz = new SizeF();

			float ebheight = this.Font.LineSpacing * 1.5f;

			// set the button size
			sz.Height = sz.Width = ebheight;
			button.Size = sz;

			// set-up edit box
			pos.x = pos.y = 0;
			editBox.Position = pos;

			sz.Width = AbsoluteWidth - ebheight;
			editBox.Size = sz;

			// set button position
			pos.x = sz.Width;
			button.Position = pos;

			// set list position and size (relative)
			pos.x = 0;
			pos.y = (AbsoluteHeight == 0.0f) ? 0.0f : (ebheight / AbsoluteHeight);
			dropList.Position = pos;

			sz.Width	= 1.0f;
			sz.Height	= 1.0f - pos.y;
			dropList.Size = sz;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public override PushButton CreatePushButton() {
			WLButton btn = (WLButton)WindowManager.Instance.CreateWindow(ButtonTypeName, name + "__auto_button__");
			btn.MetricsMode = MetricsMode.Absolute;

			// Set up imagery
			btn.StandardImageryEnabled = true;
			btn.CustomImageryAutoSized = true;
			btn.AlwaysOnTop = true;

			RenderableImage img = new RenderableImage();
			img.HorizontalFormat = HorizontalImageFormat.Centered;
			img.VerticalFormat = VerticalImageFormat.Centered;

			img.Image = ImagesetManager.Instance.GetImageset(ImagesetName).GetImage(ButtonNormalImageName);
			btn.SetNormalImage(img);
			btn.SetDisabledImage(img);

			img.Image = ImagesetManager.Instance.GetImageset(ImagesetName).GetImage(ButtonHighlightedImageName);
			btn.SetHoverImage(img);
			btn.SetPushedImage(img);

			return btn;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public override EditBox CreateEditBox() {
			EditBox box = (EditBox)WindowManager.Instance.CreateWindow(EditboxTypeName, name + "__auto_edtibox__");
			box.MetricsMode = MetricsMode.Absolute;
			return box;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public override ComboDropList CreateDropList() {
			return (ComboDropList)WindowManager.Instance.CreateWindow(DropListTypeName, name + "__auto_droplist__");
		}



	}
}
