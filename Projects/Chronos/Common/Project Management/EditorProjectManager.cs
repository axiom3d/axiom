using System;
using System.Collections;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using System.Windows.Forms;
using Chronos.Diagnostics;
using Chronos;
using Axiom.Core;

namespace Chronos
{
	/// <summary>
	/// The project manager is defunct for the time being.
	/// </summary>
	public class EditorProjectManager
	{
		EditorProject activeProject;

		#region Singleton Implementation

		private static EditorProjectManager _Instance;

		/// <summary>
		/// The private constructor is called from PluginManager.
		/// </summary>
		private EditorProjectManager() {
			_Instance = this;
		}

		/// <summary>
		/// Instance allows access to the Engine, SceneManager, and SceneGraph from
		/// within the plugin.
		/// </summary>
		public static EditorProjectManager Instance {
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
			_Instance = new EditorProjectManager();
		}

		#endregion

		// Takes a .cep file
		public EditorProject LoadProject(string file) {
			EditorProject ep = null;
			XmlSerializer xs = new XmlSerializer(typeof(EditorProject));
			FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read);
			ep = xs.Deserialize(fs) as EditorProject;
			fs.Close();
			activeProject = ep;
			if(activeProject != null && activeProject.ActiveScene != null) {
				activeProject.ActiveScene.Attach();
				Chronos.Core.SceneGraph.Instance.SynchToEditorScene(activeProject.ActiveScene);
			}
			return ep;
		}

		public EditorProject ActiveProject {
			get { return activeProject; }
		}

		public EditorProject NewProject(string projectName, string projectFile, SceneType sceneType) {
			if(activeProject != null && activeProject.ActiveScene != null) {
				activeProject.ActiveScene.Detach();
			}
			activeProject = new EditorProject();
			activeProject.Filename = projectFile;
			activeProject.Name = projectName;
			ProjectBrowser.Instance.NewProject(projectName);
			activeProject.NewScene("scene1.scene", sceneType);
			activeProject.ActiveScene.Attach();
			Chronos.Core.SceneGraph.Instance.SynchToEditorScene(activeProject.ActiveScene);
			return activeProject;
		}

		public EditorProject NewProject() {
			if(CloseActiveProject()) {
				NewProjectForm np = new NewProjectForm();
				if(np.ShowDialog() == DialogResult.OK) {
					Array values = Enum.GetValues(typeof(SceneType));
					SceneType selType = SceneType.Generic;
					foreach(SceneType t in values) {
						if(t.ToString() == np.SceneType.SelectedItem.ToString()) {
							selType = t;
							break;
						}
					}
					activeProject = new EditorProject();
					activeProject.Filename = np.projDest.Text;
					activeProject.NewScene("scene1.scene", selType);
					activeProject.ActiveScene.Attach();
					Chronos.Core.SceneGraph.Instance.SynchToEditorScene(activeProject.ActiveScene);
				}
				return activeProject;
			} else {
				return null;
			}
		}

		public bool CloseActiveProject() {
			if(activeProject.IsDirty) {
				DialogResult d = MessageBox.Show("Do you wish to save the current project?", "Closing Project", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
				if(d == DialogResult.Yes) {
					activeProject.Save();
				} else if(d == DialogResult.Cancel) {
					return false;
				}
			}
			activeProject = null;
			return true;
		}
	}
}
