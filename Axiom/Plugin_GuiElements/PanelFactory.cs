using System;
using Axiom.Core;
using Axiom.Gui;

namespace Plugin_GuiElements
{
    /// <summary>
    /// 	Summary description for BorderPanelFactory.
    /// </summary>
    public class PanelFactory : IGuiElementFactory {
        #region IGuiElementFactory Members

        public GuiElement Create(string name) {
            return new Panel(name);
        }

        public string Type {
            get {
                return "Panel";
            }
        }

        #endregion
    }
}
