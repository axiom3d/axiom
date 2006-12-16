using System;
using System.Collections;
using System.Windows.Forms;
using System.Xml;
using Axiom.Core;
using Axiom.Graphics;
using Axiom.Math.Collections;

using Chronos.Core;
namespace BackgroundPlugin
{
	/// <summary>
	/// Summary description for BackgroundPlugin.
	/// </summary>
	public class BackgroundPlugin : Chronos.Core.IEditorPlugin
	{
		private AdjustScene adjustSceneFrm;

        #region Singleton Implementation

        private static BackgroundPlugin _Instance;

        /// <summary>
        /// The private constructor is called from PluginManager.
        /// </summary>
        private BackgroundPlugin() 
        {
            _Instance = this;
        }

        /// <summary>
        /// Instance allows access to the Root, SceneManager, and SceneGraph from
        /// within the plugin.
        /// </summary>
        public static BackgroundPlugin Instance {
            get {
                if(_Instance == null) {
					// This error message is a little more programmer friendly, I think.
					// Users should never see it.
                    string message = "Singleton instance not initialized. Please call the plugin constructor first.";
                    throw new InvalidOperationException(message);
                }
                return _Instance;
            }
        }

        #endregion

		#region EditorPluginBase members
	
		public void Start()
		{
			adjustSceneFrm = new AdjustScene();
			GuiManager.Instance.CreateDockingWindow(adjustSceneFrm);
		}

		public void Stop() 
		{
		}

		#endregion
	}
}
