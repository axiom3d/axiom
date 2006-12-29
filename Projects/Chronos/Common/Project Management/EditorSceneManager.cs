using System;
using System.IO;
using Chronos;
using Chronos.Core;
using System.Windows.Forms;

namespace Chronos.Core
{
	/// <summary>
	/// Summary description for EditorSceneManager.
	/// </summary>
	public class EditorSceneManager {
		#region Singleton Implementation

		private static EditorSceneManager _Instance;

		/// <summary>
		/// The private constructor is called from PluginManager.
		/// </summary>
		private EditorSceneManager() {
			_Instance = this;
		}

		/// <summary>
		/// Instance allows access to the Root, SceneManager, and SceneGraph from
		/// within the plugin.
		/// </summary>
		public static EditorSceneManager Instance {
			get {
				if(_Instance == null) {
					string message = "Singleton instance not initialized. Please call the plugin constructor first.";
					throw new InvalidOperationException(message);
				}
				return _Instance;
			}
		}

		public static void Init() {
			if(_Instance != null) {
				string message = "Singleton instance not initialized. Please call the plugin constructor first.";
				throw new InvalidOperationException(message);
			}
			_Instance = new EditorSceneManager();
		}

		#endregion

		private EditorScene activeScene;

		/// <summary>
		/// Returns the currently active scene. There can only be one active
		/// scene.
		/// </summary>
		public EditorScene ActiveScene {
			get { return activeScene; }
		}

		/// <summary>
		/// Causes the existing scene to be cleared and a new one to be created.
		/// </summary>
		public void NewScene() {
			if(!CloseScene()) return;
			activeScene = new EditorScene();
			activeScene.Attach();
			DocumentEventHook doc = Chronos.Core.GuiManager.Instance.CreateDocument(ViewportPlugin.Instance.CreateNewRenderWindow(), "View Window");
			activeScene.AddDocHook(doc);
			Chronos.Core.SceneGraph.Instance.SynchToEditorScene(activeScene);
		}

		/// <summary>
		/// Loads a scene from a stream. The stream should contain a .chronos file
		/// </summary>
		/// <param name="filestream"></param>
		public void LoadScene(Stream filestream) {
			if(!CloseScene()) return;
			// TODO: Load scene
		}

		/// <summary>
		/// Loads a scene from a .chronos file
		/// </summary>
		/// <param name="path"></param>
		public void LoadScene(string path) {
			if(!CloseScene()) return;
			// TODO: Load scene
		}

		/// <summary>
		/// Closes a scene and destroys and associated viewport documents. Not
		/// generally expected to be used by plugins, but there are instances where
		/// it may be helpful to do so.
		/// </summary>
		/// <returns></returns>
		public bool CloseScene() {
			if(activeScene != null) {
				if(activeScene.IsDirty) {
					DialogResult d = MessageBox.Show("Do you wish to save the current scene?", "Closing Scene", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
					if(d == DialogResult.Yes) {
						activeScene.Save();
					} else if(d == DialogResult.Cancel) {
						return false;
					}
				}
				activeScene.DestroyHookedDocuments();
				activeScene.Detach();
			}
			return true;
		}
	}
}

