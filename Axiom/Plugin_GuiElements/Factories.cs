using System;
using Axiom.Core;
using Axiom.Gui;
namespace Plugin_GuiElements
{
    /// <summary>
    /// 	Summary description for BorderPanelFactory.
    /// </summary>
    public class BorderPanelFactory : IGuiElementFactory {
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

    /// <summary>
    /// 	Summary description for PanelFactory.
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

    /// <summary>
    /// 	Summary description for TextAreaFactory.
    /// </summary>
    public class TextAreaFactory : IGuiElementFactory {
        #region IGuiElementFactory Members

        public GuiElement Create(string name) {
            return new TextArea(name);
        }

        public string Type {
            get {
                return "TextArea";
            }
        }

        #endregion
    }
}
