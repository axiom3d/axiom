using System;
using Axiom.Core;
using Axiom.Gui;

namespace Plugin_GuiElements
{
	/// <summary>
	/// Summary description for GuiElements.
	/// </summary>
	public class GuiElements : IPlugin
	{
        #region IPlugin Members

        public void Start() {
            GuiManager.Instance.AddElementFactory(new PanelFactory());
            GuiManager.Instance.AddElementFactory(new BorderPanelFactory());
        }

        public void Stop() {
            // TODO:  Add GuiElements.Stop implementation
        }

        #endregion
    }
}
