using System;
using CeGuiSharp;
using CeGuiSharp.Widgets;

namespace CeGuiSharp.WidgetSets.TaharezLook {
	/// <summary>
	///		
	/// </summary>
	public class TLCloseButton : TLButton {
		/// <summary>
		/// 
		/// </summary>
		/// <param name="name"></param>
		public TLCloseButton(string type, string name) : base(type, name) {}

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
