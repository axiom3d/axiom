using System;
using Axiom.Core;
using Axiom.Gui;

namespace Plugin_GuiElements
{
	/// <summary>
	/// 	Summary description for BorderPanelFactory.
	/// </summary>
	public class BorderPanelFactory : IGuiElementFactory
	{
        #region IGuiElementFactory Members

        public GuiElement Create(string name) {
            return new BorderPanel(name);
        }

        public string Type {
            get {
                return "BorderPanel";
            }
        }

        #endregion
    }
}
