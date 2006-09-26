using System;
using System.Drawing;
using CeGuiSharp;
using CeGuiSharp.Widgets;

namespace CeGuiSharp.WidgetSets.TaharezLook {
	/// <summary>
	/// 
	/// </summary>
	public class TLTitleBar : TitleBar {
		#region Constants

		/// <summary>
		///		Name of the imageset to use for rendering.
		/// </summary>
		internal const string ImagesetName = "TaharezLook";
		/// <summary>
		///		Name of the image to use for the left section of the title bar.
		/// </summary>
		internal const string LeftEndSectionImageName = "NewTitlebarLeft";
		/// <summary>
		///		Name of the image to use for the middle section of the title bar.
		/// </summary>
		internal const string MiddleSectionImageName = "NewTitlebarMiddle";
		/// <summary>
		///		Name of the image to use for the right section of the title bar.
		/// </summary>
		internal const string RightEndSectionImageName = "NewTitlebarRight";
		/// <summary>
		///		Name of the image to use for the middle section of the sys buttons area.
		/// </summary>
		internal const string SysAreaMiddleImageName = "SysAreaMiddle";
		/// <summary>
		///		Name of the image to use for the right section of the sys buttons area.
		/// </summary>
		internal const string SysAreaRightImageName = "SysAreaRight";
		/// <summary>
		///		Name of the image to use as the mouse cursor for this window.
		/// </summary>
		internal const string NormalCursorImageName = "MouseMoveCursor";

		#endregion Constants

		#region Fields

		/// <summary>
		///		Image object used for the left edge of the title bar.
		/// </summary>
		protected Image leftImage;
		/// <summary>
		///		Image object used for the middle section of the title bar.
		/// </summary>
		protected Image middleImage;
		/// <summary>
		///		Image object used for the right edge of the title bar.
		/// </summary>
		protected Image rightImage;
		/// <summary>
		///		Image object used for the system area (middle section).
		/// </summary>
		protected Image sysMidImage;
		/// <summary>
		///		Image object used for the system area (right end section).
		/// </summary>
		protected Image sysRightImage;

		#endregion Fields

		/// <summary>
		/// 
		/// </summary>
		/// <param name="name"></param>
		public TLTitleBar(string type, string name) : base(type, name) {
			Imageset imageSet = ImagesetManager.Instance.GetImageset(ImagesetName);

			// get images
			leftImage = imageSet.GetImage(LeftEndSectionImageName);
			middleImage = imageSet.GetImage(MiddleSectionImageName);
			rightImage = imageSet.GetImage(RightEndSectionImageName);
			sysMidImage = imageSet.GetImage(SysAreaMiddleImageName);
			sysRightImage = imageSet.GetImage(SysAreaRightImageName);

			// set cursor
			SetMouseCursor(imageSet.GetImage(NormalCursorImageName));

			AlwaysOnTop = false;
		}

		#region Window Members

		/// <summary>
		/// 
		/// </summary>
		public override Rect PixelRect {
			get {
				// clip to screen if we have no grand-parent
				if(parent == null || parent.Parent == null) {
					return GuiSystem.Instance.Renderer.Rect.GetIntersection(UnclippedPixelRect);
				}
					// clip to grand parent
				else {
					return parent.Parent.InnerRect.GetIntersection(UnclippedPixelRect);
				}
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="z"></param>
		protected override void DrawSelf(float z) {
			Rect clipper = PixelRect;

			// do nothing if the widget is totally clipped
			if(clipper.Width == 0) {
				return;
			}

			// get the destination screen rect for this window
			Rect absRect = UnclippedPixelRect;

			// calc colors to use
			Color colorVal = new Color(EffectiveAlpha, 1, 1, 1);
			ColorRect colors = new ColorRect(colorVal, colorVal, colorVal, colorVal);

			float leftWidth = leftImage.Width;
			float rightWidth = rightImage.Width;
			float sysRightWidth = sysRightImage.Width;

			FrameWindow parentWindow = (FrameWindow)this.Parent;

			float sysMidWidth = ((parentWindow != null) && parentWindow.CloseButtonEnabled) ? sysMidImage.Width : 0;

			float midWidth = absRect.Width - leftWidth - rightWidth - sysRightWidth - sysMidWidth;

			// draw the title var images
			CeGuiSharp.Vector3 pos =
				new CeGuiSharp.Vector3 (absRect.Left, absRect.Top, z);

			SizeF size = new SizeF(leftWidth, absRect.Height);
			leftImage.Draw(pos, size, clipper, colors);
			pos.x += size.Width;

			size.Width = midWidth;
			middleImage.Draw(pos, size, clipper, colors);
			pos.x += size.Width;

			size.Width = rightWidth;
			rightImage.Draw(pos, size, clipper, colors);
			pos.x += size.Width;

			size.Width = sysMidWidth;
			sysMidImage.Draw(pos, size, clipper, colors);
			pos.x += size.Width;

			size.Width = sysRightWidth;
			sysRightImage.Draw(pos, size, clipper, colors);

			// Draw label text
			pos.x = absRect.Left + 10.0f;
			pos.y = absRect.Top + ((absRect.Height - this.Font.LineSpacing) / 2);
			pos.z = GuiSystem.Instance.Renderer.GetZLayer(1);

			this.Font.DrawText(parent.Text, pos, clipper, colors);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="e"></param>
		protected override void OnMouseMove(MouseEventArgs e) {
			base.OnMouseMove (e);

			e.Handled = true;
		}

		#endregion Window Members
	}
}
