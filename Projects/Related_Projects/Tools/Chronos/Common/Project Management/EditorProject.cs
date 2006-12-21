using System;
using System.Collections;
using System.IO;
using System.Windows.Forms;
using System.Xml.Serialization;
using Axiom.Core;

namespace Chronos
{
	/// <summary>
	/// Summary description for EditorScene.
	/// </summary>
	[Serializable]
	public class EditorProject {
		// Open by basedir
		private ArrayList classes = new ArrayList();
		private Hashtable directories = new Hashtable();
		private Hashtable scenes = new Hashtable();
		private string projectFilename = String.Empty;
		private EditorScene activeScene = null;
		private bool isDirty = false;
		private string name;

		public EditorProject() {
		}

		public void ImportResource(string filename, string rscClass) {
			// Copy file to data/{class}
			string file = System.IO.Path.GetFileName(filename);
			string dir = directories["data"].ToString() + Path.PathSeparator + rscClass;
			if(!System.IO.Path.IsPathRooted(filename)) {
				dir = System.IO.Path.GetDirectoryName(projectFilename) + Path.PathSeparator + dir;
			}
			System.IO.File.Copy(filename, dir + Path.PathSeparator + file);
		}

		public void SetDirectory(string key, string value) {
			directories[key] = value;
		}

		public void AddClass(string name) {
			classes.Add(name);
		}

		public void AddScene(EditorScene scene) {
			scenes[scene.Name] = scene;
		}

		public EditorScene GetScene(string name) {
			if(scenes.ContainsKey(name))
				return scenes[name] as EditorScene;
			else
				return null;
		}

		public string Filename {
			get { return projectFilename; }
			set { projectFilename = value; }
		}

		public bool IsDirty {
			get { 
				if(isDirty) return true;
				foreach(EditorScene scene in scenes) {
					if(scene.IsDirty) return true;
				}
				return false;
			}
			set {
				isDirty = value;
			}
		}

		public EditorScene ActiveScene {
			get { return activeScene; }
			set { activeScene = value; }
		}

		public string Name {
			get { return name; }
			set { name = value; }
		}

		public void Save() {
			XmlSerializer xs = new XmlSerializer(this.GetType());
			FileStream fs = new FileStream(projectFilename, FileMode.OpenOrCreate, FileAccess.Write);
			xs.Serialize(fs, this);
			fs.Close();
		}

		public void SaveTo(string filename) {
			projectFilename = filename;
			Save();
		}

		public void NewScene(string sceneName, Axiom.Core.SceneType sceneType) {
			if(activeScene != null && activeScene.IsDirty) {
				DialogResult d = MessageBox.Show("Do you wish to save the current scene?", "Closing Scene", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
				if(d == DialogResult.Yes) {
					activeScene.Save();
				} else if(d == DialogResult.Cancel) {
					return;
				}
			}
			EditorScene scene = new EditorScene();
			scene.Filename = sceneName;
			scene.Name = sceneName;
			scene.SceneType = sceneType;
			AddScene(scene);
			activeScene = scene;
			ProjectBrowser.Instance.NewScene(scene);
			Chronos.Core.GuiManager.Instance.CreateDocument(ViewportPlugin.Instance.CreateNewRenderWindow(), "Render Window");
			// TODO: Clear, attach
		}
	}

}
