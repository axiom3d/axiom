using System;
using System.Collections;
using System.Collections.Specialized;
using System.Windows.Forms;
using System.IO;
using System.Xml;
using Axiom.Core;
using Axiom.Graphics;
using Axiom.Math.Collections;
using Chronos.Core;
using Chronos;

namespace MaterialLibraryPlugin
{
	/// <summary>
	/// Summary description for MaterialLibraryPlugin.
	/// </summary>
	public class MaterialLibraryPlugin : Chronos.Core.IEditorPlugin
	{
		private MaterialLibrary matLibFrm;

		public delegate void ApplyMaterialDelegate(object sender, Material material);
		public event ApplyMaterialDelegate MaterialChanged;

		public void FireMaterialChanged(object sender, Material material) {
			if(MaterialChanged != null)
				MaterialChanged(sender, material);
		}

		#region Singleton Implementation

        private static MaterialLibraryPlugin _Instance;

        /// <summary>
        /// The private constructor is called from PluginManager.
        /// </summary>
        private MaterialLibraryPlugin() 
        {
            _Instance = this;
        }

        /// <summary>
        /// Instance allows access to the Root, SceneManager, and SceneGraph from
        /// within the plugin.
        /// </summary>
        public static MaterialLibraryPlugin Instance {
            get {
                if(_Instance == null) {
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
			MaterialEntryManager.Init();
			matLibFrm = new MaterialLibrary();

			string path = EditorResourceManager.Instance.MediaPath;
			StringCollection materialScripts = MaterialManager.GetAllCommonNamesLike("", "*.material");
			foreach(string script in materialScripts) {
				string[] bits = script.Split("\\/".ToCharArray());
				MaterialEntryManager.Instance.ParseScript(
					EditorResourceManager.Instance.MediaPath + Path.DirectorySeparatorChar + script,
					bits[0], Path.GetFileNameWithoutExtension(script));
			}
			matLibFrm.UpdateDisplay();

			ResourceManagerForm.Instance.AddResourceHandler("Materials", "*.material", "Material Pack");

			GuiManager.Instance.CreateDockingWindow(matLibFrm);
		}

		public void Stop() {}

		public void ImportMaterial() {
			Chronos.Core.ResourceManagerForm.Instance.Show();
		}

		#endregion
	}
}
