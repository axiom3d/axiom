#region LGPL License
/*
Chronos World Editor
Copyright (C) 2004 Chris "Antiarc" Heald [antiarc@antiarc.net]

This application is free software; you can redistribute it and/or
modify it under the terms of the GNU Lesser General Public
License as published by the Free Software Foundation; either
version 2.1 of the License, or (at your option) any later version.

This application is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
Lesser General Public License for more details.

You should have received a copy of the GNU Lesser General Public
License along with this library; if not, write to the Free Software
Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
*/
#endregion

using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using Axiom.Core;
using Axiom.Math;
using Axiom.Math.Collections;
using Axiom.Graphics;

namespace Chronos.Core {
	/// <summary>
	/// Summary description for SceneGraph.
	/// </summary>

	public class SceneGraph : Form {
		private bool isSetup = false;

		#region Singleton implementation

		protected static SceneGraph instance;

		public static SceneGraph Instance {
			get { 
				return instance; 
			}
		}

		public static void Init() {
			if (instance != null) {
				throw new ApplicationException("SceneGraph.Instance is null!");
			}
			instance = new SceneGraph();
			Chronos.GarbageManager.Instance.Add(instance);
		}

		new public void Dispose() {
			if (instance == this) {
				instance = null;
			}
		}
		
		#endregion

		// Custom event delegates
		public delegate void SelectedObjectChangedDelegate(object sender, EditorNode node);
		public event SelectedObjectChangedDelegate SelectedObjectChanged;

		//public delegate void SelectedNodeChangedDelegate(object sender, TreeNode n);
		//public event SelectedNodeChangedDelegate SelectedNodeChanged;

		private System.Windows.Forms.ToolBar toolBar1;
		private PropertyEditorForm propEditor = new PropertyEditorForm();
		private TreeNode dragNode;

		private Chronos.Core.TrappedTreeView SceneGraphView;

		internal PropertyEditorForm propform {
			get { return propEditor; }
		}

		internal PropertyGrid PropertyEditor {
			get { return propEditor.PropertyEditor; }
		}

		#region Constructors and destructors

		protected SceneGraph() {
			InitializeComponent();
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing ) {
			base.Dispose( disposing );
		}
		#endregion

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent() {
			System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(SceneGraph));
			this.toolBar1 = new System.Windows.Forms.ToolBar();
			this.SceneGraphView = new Chronos.Core.TrappedTreeView();
			this.SuspendLayout();
			// 
			// toolBar1
			// 
			this.toolBar1.ButtonSize = new System.Drawing.Size(24, 24);
			this.toolBar1.DropDownArrows = true;
			this.toolBar1.Location = new System.Drawing.Point(0, 0);
			this.toolBar1.Name = "toolBar1";
			this.toolBar1.ShowToolTips = true;
			this.toolBar1.Size = new System.Drawing.Size(264, 30);
			this.toolBar1.TabIndex = 0;
			// 
			// SceneGraphView
			// 
			this.SceneGraphView.AllowDrop = true;
			this.SceneGraphView.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.SceneGraphView.Dock = System.Windows.Forms.DockStyle.Fill;
			this.SceneGraphView.HideSelection = false;
			this.SceneGraphView.ImageIndex = -1;
			this.SceneGraphView.Location = new System.Drawing.Point(0, 30);
			this.SceneGraphView.Name = "SceneGraphView";
			this.SceneGraphView.SelectedImageIndex = -1;
			this.SceneGraphView.Size = new System.Drawing.Size(264, 301);
			this.SceneGraphView.TabIndex = 1;
			/*this.SceneGraphView.MouseDown += new System.Windows.Forms.MouseEventHandler(this.SceneGraphView_MouseDown);
			this.SceneGraphView.DragOver += new System.Windows.Forms.DragEventHandler(this.SceneGraphView_DragOver);
			this.SceneGraphView.DragDrop += new System.Windows.Forms.DragEventHandler(this.SceneGraphView_DragDrop);*/
			this.SceneGraphView.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.SceneGraphView_AfterSelect);
			/*this.SceneGraphView.ItemDrag += new System.Windows.Forms.ItemDragEventHandler(this.SceneGraphView_ItemDrag);
			this.SceneGraphView.CmdKeyPressed += new Chronos.Core.TrappedTreeView.CmdKeyDelegate(this.SceneGraphView_CmdKeyPressed);*/
			// 
			// SceneGraph
			// 
			this.AccessibleRole = System.Windows.Forms.AccessibleRole.None;
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.ClientSize = new System.Drawing.Size(264, 331);
			this.Controls.Add(this.SceneGraphView);
			this.Controls.Add(this.toolBar1);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.KeyPreview = true;
			this.Name = "SceneGraph";
			this.Text = "Scene Graph";
			this.ResumeLayout(false);

		}
		#endregion

		public void Setup() {
			if(!isSetup) {
				isSetup = true;
				 ViewportPlugin.Instance.ObjectPicked += new Chronos.Core.ViewportPlugin.ObjectPickedDelegate(Instance_ObjectPicked);
			}
		}
		private void SceneGraphView_AfterSelect(object sender, System.Windows.Forms.TreeViewEventArgs e) {
			if(SelectedObjectChanged != null)
				SelectedObjectChanged(sender, e.Node.Tag as EditorNode);

			MovableObject tag = (e.Node.Tag as EditorNode).GetObject(0);
			if(PropertyEditor.SelectedObject != null &&
				PropertyEditor.SelectedObject is IPropertiesWrapper)
				(PropertyEditor.SelectedObject as IPropertiesWrapper).Dispose();
			if(tag == null) {
				PropertyEditor.SelectedObject = null;
			} else {
				PropertyEditor.SelectedObject = (e.Node.Tag as EditorNode).GetOwner().GetPropertiesObject(tag);
			}
		}

		private void deleteNode(TreeNode node) {
			if(node.Parent == null) return;
			foreach(TreeNode n in node.Nodes)
				deleteNode(n);
			Root.Instance.SceneManager.DestroySceneNode((node.Tag as EditorNode).Name);
			node.Remove();
		}

		private void SceneGraphView_ItemDrag(object sender, System.Windows.Forms.ItemDragEventArgs e) {
			if(e.Item is TreeNode) {
				dragNode = e.Item as TreeNode;
				DoDragDrop(dragNode, DragDropEffects.Move);
			}
		}

		private void SceneGraphView_DragDrop(object sender, System.Windows.Forms.DragEventArgs e) {
			
			TreeView tv = (sender as TreeView);
			TreeNode n = tv.GetNodeAt(tv.PointToClient(new Point(e.X, e.Y)));
			if(n != null && isValidDropTarget(dragNode, n)) {
				dragNode.Remove();
				n.Nodes.Add(dragNode);
				(dragNode.Tag as EditorNode).ReParent(n.Tag as EditorNode);
			}
		}

		private void SceneGraphView_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e) {
			TreeView tv = sender as TreeView;
			TreeNode tn = tv.GetNodeAt(e.X, e.Y);
			if(tn != null)
				tv.SelectedNode = tn;
		}

		private void SceneGraphView_DragOver(object sender, System.Windows.Forms.DragEventArgs e) {
			TreeView tv = (sender as TreeView);
			TreeNode n = tv.GetNodeAt(tv.PointToClient(new Point(e.X, e.Y)));
			if(n != null && n.Parent != null) {
				if(isValidDropTarget(dragNode, n))
					e.Effect = DragDropEffects.Move;
			}
		}

		private bool isValidDropTarget(TreeNode node, TreeNode target) {
			while(target != null) {
				if(target.Equals(node)) return false;
				target = target.Parent;
			}
			return true;
		}

		private void SceneGraphView_CmdKeyPressed(Keys key) {
			if(key == Keys.Delete) {
				deleteNode(this.SceneGraphView.SelectedNode);
			}
		}

		internal void SynchToEditorScene(EditorScene scene) {
			this.SceneGraphView.Nodes.Clear();
			SynchToEditorNode(scene.GetRoot(), null);
		}

		private void SynchToEditorNode(EditorNode node, TreeNode parent) {
			TreeNode n = new TreeNode(node.DisplayName);
			n.Tag = node;
			node.GraphTag = n;
			node.NodeAdded += new Chronos.Core.EditorNode.EditorNodeOperation(node_NodeAdded);
			node.NodeRemoved += new Chronos.Core.EditorNode.EditorNodeOperation(node_NodeRemoved);
			node.DisplayNameChanged += new Chronos.Core.EditorNode.EditorNodeChangeDisplayName(node_DisplayNameChanged);
			if(parent == null) {
				this.SceneGraphView.Nodes.Add(n);
			} else {
				parent.Nodes.Add(n);
			}
			foreach(EditorNode ed in node.Children) {
				SynchToEditorNode(ed, n);
			}
		}

		private void node_NodeAdded(EditorNode node, object sender) {
			if(node.Parent != null) {
				this.SynchToEditorNode(node, (node.Parent as EditorNode).GraphTag as TreeNode);
				((node.Parent as EditorNode).GraphTag as TreeNode).Expand();
			}
		}

		private void node_NodeRemoved(EditorNode node, object sender) {
			(node.GraphTag as TreeNode).Remove();
			node.NodeAdded -= new Chronos.Core.EditorNode.EditorNodeOperation(node_NodeAdded);
			node.NodeRemoved -= new Chronos.Core.EditorNode.EditorNodeOperation(node_NodeRemoved);
		}

		private void Instance_ObjectPicked(object sender, MovableObject obj) {
			if(obj == null) {
				SceneGraphView.SelectedNode = SceneGraphView.Nodes[0];
			} else {
				SceneNode parent = (SceneNode)obj.ParentNode;
				while(!(parent is EditorNode) && parent != null){
					parent = (SceneNode)parent.Parent;
				}
				if(parent != null) {
					this.SceneGraphView.SelectedNode = (parent as EditorNode).GraphTag as TreeNode;
				}
			}
		}

		private void node_DisplayNameChanged(string newname, object sender) {
			object t = (sender as EditorNode).GraphTag;
			if(t != null && t is TreeNode)
				(t as TreeNode).Text = newname;
		}
	}

	public class TrappedTreeView : TreeView {
		public delegate void CmdKeyDelegate(Keys key);
		public event CmdKeyDelegate CmdKeyPressed;

		protected override bool ProcessCmdKey(ref Message msg, Keys keyData) {
			if (keyData == Keys.Delete) {
				if(CmdKeyPressed != null)
					CmdKeyPressed(keyData);
			}
			return base.ProcessCmdKey (ref msg, keyData);
		}

	}
}
