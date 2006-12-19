using System;
using CeGuiSharp;
using CeGuiSharp.Widgets;

namespace CeGuiSharp.WidgetSets.WindowsLook {

	/// <summary>
	///		Close button widget for the Windows Gui Scheme.
	/// </summary>
	public class WLCloseButton : WLButton
	{
		/// <summary>
		/// 
		/// </summary>
		/// <param name="name"></param>
		public WLCloseButton( string type, string name ) : base(type, name) {}

		/// <summary>
		/// 
		/// </summary>
		public override Rect PixelRect {
			get {
				// clip to screen if we have no grand-parent
				if ((parent == null) || (parent.Parent == null)) {
					return GuiSystem.Instance.Renderer.Rect.GetIntersection(UnclippedPixelRect);
				}
					// else clip to grand-parent
				else {
					return parent.Parent.InnerRect.GetIntersection(UnclippedPixelRect);
				}
			}
		}

	}
}
