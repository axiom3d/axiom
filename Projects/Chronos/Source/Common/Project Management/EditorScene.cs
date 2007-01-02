using System;
using System.Collections;
using Axiom.Core;
using Chronos.Core;

namespace Chronos.Core
{
	/// <summary>
	/// Summary description for EditorScene.
	/// </summary>
	public class EditorScene {
		private string name;
		private string file;
		private SceneType sceneManager;
		private bool isDirty = false;
		private EditorNode rootNode;
		private ArrayList docHooks = new ArrayList();

		public EditorScene() {
			rootNode = new EditorNode(Root.Instance.SceneManager);
			rootNode.DisplayName = "[Root]";
		}

		public string Name {
			get { return name; }
			set { name = value; }
		}

		public string Filename {
			get { return file; }
			set { file = value; }
		}

		public SceneType SceneType {
			get { return sceneManager; }
			set { sceneManager = value; }
		}

		public bool IsDirty {
			get { return isDirty; }
			set { isDirty = true; }
		}

		public EditorNode GetRoot() {
			return rootNode;
		}

		public EditorNode AddObject(string text, MovableObject obj, IMovableObjectPlugin ownerObj) {
			EditorNode ed = rootNode.CreateChildEditorNode();
			ed.DisplayName = text;
			ed.AttachObject(obj, ownerObj);
			return ed;
		}

		public void Attach() {
			Root.Instance.SceneManager.RootSceneNode.AddChild(this.rootNode);
		}

		public void Detach() {
			Root.Instance.SceneManager.RootSceneNode.RemoveChild(rootNode);
		}

		public void Save() {
			// TODO: Stubbed
		}

		public void AddDocHook(DocumentEventHook d) {
			docHooks.Add(d);
		}

		public void DestroyHookedDocuments() {
			foreach(DocumentEventHook d in docHooks) {
				d.FireClose(this);
			}
		}
	}
}
