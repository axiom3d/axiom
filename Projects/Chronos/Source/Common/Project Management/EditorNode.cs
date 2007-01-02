using System;
using System.Collections;
using Axiom.Core;
using Chronos.Core;

namespace Chronos.Core
{
	/// <summary>
	/// Summary description for EditorNode.
	/// </summary>
	public class EditorNode : SceneNode {
		public object GraphTag;
		private IMovableObjectPlugin objectOwner;
		private string displayName;

		public delegate void EditorNodeOperation(EditorNode node, object sender);
		public delegate void EditorNodeChangeDisplayName(string newname, object sender);
		public event EditorNodeOperation NodeAdded;
		public event EditorNodeOperation NodeRemoved;
		public event EditorNodeChangeDisplayName DisplayNameChanged;

		public EditorNode(SceneManager creator) : base(creator) {}
		public EditorNode(SceneManager creator, string name) : base(creator, name) {}
		
		public void AddChild(EditorNode node) {
			base.AddChild(node);
			if(this.NodeAdded != null)
				NodeAdded(node, this);
		}

		public override Node RemoveChild(int index) {
			if(NodeRemoved != null)
				NodeRemoved(base.GetChild(index) as EditorNode, this);
			return base.RemoveChild (index);
		}

		public override Node RemoveChild(string name) {
			if(NodeRemoved != null)
				NodeRemoved(base.GetChild(name) as EditorNode, this);
			return base.RemoveChild (name);
		}

		public void AttachObject(MovableObject obj, IMovableObjectPlugin owner) {
			foreach(MovableObject o in objectList)
				if(o is Entity)
					throw new Exception("Error: Cannot add more than one entity to an EditorNode.");
			base.AttachObject(obj);
			objectOwner = owner;
		}

		new public void DetachObject(MovableObject obj) {
			base.DetachObject(obj);
			if(objectList.Count == 0)
				objectOwner = null;
		}

		public IMovableObjectPlugin GetOwner() {
			return objectOwner;
		}

		public EditorNode CreateChildEditorNode() {
			EditorNode child = new EditorNode(this.creator);
			this.AddChild(child);
			return child;
		}

		public void Serialize() {
			// TODO: Stubbed
		}

		public Axiom.Collections.NodeCollection Children {
			get { return childNodes; }
		}

		public Axiom.Collections.MovableObjectCollection AttachedObjects {
			get { return base.objectList; }
		}

		public void ReParent(SceneNode newParent) {
			this.Parent.RemoveChild(this);
			newParent.AddChild(this);
		}

		new public MovableObject GetObject(int index) {
			if(this.objectList.Count > index)
				return objectList[index];
			return null;
		}

		public string DisplayName {
			get { return displayName; }
			set {
				displayName = value;
				if(DisplayNameChanged != null)
					this.DisplayNameChanged(value, this);
			}
		}

		public void HighlightNode() {
			ShowBoundingBox = true;
		}

		public void UnhighlightNode() {
			ShowBoundingBox = false;
		}
	}
}
