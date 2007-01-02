using System;
using Chronos.Core;
using Axiom.Core;
using System.Windows.Forms;

namespace Chronos.Core
{
	/// <summary>
	/// Summary description for ViewportPlugin.
	/// </summary>
	/// 
	public class ViewportPlugin : IEditorPlugin
	{
		public delegate void ObjectPickedDelegate(object sender, MovableObject obj);
		public event ObjectPickedDelegate ObjectPicked;

		public void FireObjectPicked(object sender, MovableObject obj) {
			if(ObjectPicked != null) {
				ObjectPicked(sender, obj);
			}
		}

		public UserControl CreateNewRenderWindow() {
			return new RenderingWindow();
		}

		#region Singleton Implementation

		private static ViewportPlugin _Instance;

		/// <summary>
		/// The private constructor is called from PluginManager.
		/// </summary>
		private ViewportPlugin() {
			_Instance = this;
		}

		public static void Init() {
			if(_Instance == null)
				_Instance = new ViewportPlugin();
		}

		/// <summary>
		/// Instance allows access to the Root, SceneManager, and SceneGraph from
		/// within the plugin.
		/// </summary>
		public static ViewportPlugin Instance {
			get {
				if(_Instance == null) {
					string message = "Singleton instance not initialized. Please call the plugin constructor first.";
					throw new InvalidOperationException(message);
				}
				return _Instance;
			}
		}

		#endregion

		#region IEditorPlugin Members

		public void Start()
		{
		}

		public void Stop()
		{
			// TODO:  Add ViewportPlugin.Stop implementation
		}

		#endregion
	}
}
