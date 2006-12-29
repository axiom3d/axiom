using System;
using System.Collections;

using Axiom.Core;

namespace Chronos.Core
{
	/// <summary>
	/// NodeStore streamlines the process of replacing children of the RootSceneNode for rendering.
	/// </summary>
	public class NodeStore
	{
		private SceneNode node;
		private SceneNode root;
		private ArrayList list;

		public NodeStore()
		{
			this.node = null;
			this.list = new ArrayList();
		}

		public bool HasCapture()
		{
			return this.node != null;
		}

		public void Capture(SceneNode nodeToCapture, SceneNode newRoot)
		{
			if(HasCapture())
				throw new Exception("This instance has a capture, call restore to flush the capture.");

			if(nodeToCapture == null)
				throw new ArgumentNullException("nodeToCapture");

			if(newRoot == null)
				throw new ArgumentNullException("newRoot");

			node = nodeToCapture;

			list.Clear();
			while(node.ChildCount > 0)
				list.Add(node.RemoveChild(node.ChildCount - 1));

			node.AddChild(root = newRoot);
		}

		public void Restore()
		{
			if(!HasCapture())
				throw new Exception("This instance does not have a capture, call capture to get one.");

			node.RemoveChild(root);

			for(int i = list.Count - 1; i >= 0; i--)
				node.AddChild(list[i] as Node);

			node = null;
		}
	}
}
