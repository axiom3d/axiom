using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using Axiom.Core;
using Axiom.Graphics;

namespace MaterialLibraryPlugin
{
	/// <summary>
	/// Summary description for VisualEditor.
	/// </summary>
	public class VisualEditor : System.Windows.Forms.UserControl
	{
		private System.Windows.Forms.TreeView treeView1;
		private TD.SandBar.ToolBar toolBar1;
		private TD.SandBar.ButtonItem buttonItem1;
		private TD.SandBar.ButtonItem buttonItem2;
		private TD.SandBar.ButtonItem buttonItem3;
		private TD.SandBar.ButtonItem buttonItem4;
		private TD.SandBar.ButtonItem buttonItem5;
		private System.Windows.Forms.Panel panel1;
		private System.Windows.Forms.PropertyGrid propertyGrid1;
		private System.Windows.Forms.Panel panel2;
		private System.Windows.Forms.Splitter splitter2;
		private Material material;
		private System.Windows.Forms.ContextMenu techniqueMenu;
		private System.Windows.Forms.MenuItem menuItem5;
		private System.Windows.Forms.MenuItem menuItem1;
		private System.Windows.Forms.ContextMenu passMenu;
		private System.Windows.Forms.ContextMenu texUnitMenu;
		private System.Windows.Forms.MenuItem menuItem2;
		private System.Windows.Forms.MenuItem menuItem3;
		private System.Windows.Forms.MenuItem menuItem4;
		private System.Windows.Forms.MenuItem menuItem6;
		private System.Windows.Forms.MenuItem menuItem7;
		private System.Windows.Forms.MenuItem menuItem8;
		private System.Windows.Forms.MenuItem menuItem9;
		private System.Windows.Forms.MenuItem menuItem10;
		private System.Windows.Forms.MenuItem menuItem11;

		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		public VisualEditor()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			//
			// TODO: Add any constructor code after InitializeComponent call
			//
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if(components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.treeView1 = new System.Windows.Forms.TreeView();
			this.toolBar1 = new TD.SandBar.ToolBar();
			this.buttonItem1 = new TD.SandBar.ButtonItem();
			this.buttonItem2 = new TD.SandBar.ButtonItem();
			this.buttonItem3 = new TD.SandBar.ButtonItem();
			this.buttonItem4 = new TD.SandBar.ButtonItem();
			this.buttonItem5 = new TD.SandBar.ButtonItem();
			this.panel1 = new System.Windows.Forms.Panel();
			this.propertyGrid1 = new System.Windows.Forms.PropertyGrid();
			this.panel2 = new System.Windows.Forms.Panel();
			this.splitter2 = new System.Windows.Forms.Splitter();
			this.techniqueMenu = new System.Windows.Forms.ContextMenu();
			this.menuItem1 = new System.Windows.Forms.MenuItem();
			this.menuItem5 = new System.Windows.Forms.MenuItem();
			this.passMenu = new System.Windows.Forms.ContextMenu();
			this.menuItem2 = new System.Windows.Forms.MenuItem();
			this.menuItem3 = new System.Windows.Forms.MenuItem();
			this.menuItem7 = new System.Windows.Forms.MenuItem();
			this.menuItem8 = new System.Windows.Forms.MenuItem();
			this.menuItem9 = new System.Windows.Forms.MenuItem();
			this.menuItem10 = new System.Windows.Forms.MenuItem();
			this.menuItem11 = new System.Windows.Forms.MenuItem();
			this.texUnitMenu = new System.Windows.Forms.ContextMenu();
			this.menuItem4 = new System.Windows.Forms.MenuItem();
			this.menuItem6 = new System.Windows.Forms.MenuItem();
			this.panel1.SuspendLayout();
			this.panel2.SuspendLayout();
			this.SuspendLayout();
			// 
			// treeView1
			// 
			this.treeView1.Dock = System.Windows.Forms.DockStyle.Left;
			this.treeView1.HideSelection = false;
			this.treeView1.ImageIndex = -1;
			this.treeView1.Location = new System.Drawing.Point(0, 0);
			this.treeView1.Name = "treeView1";
			this.treeView1.SelectedImageIndex = -1;
			this.treeView1.Size = new System.Drawing.Size(160, 467);
			this.treeView1.TabIndex = 0;
			this.treeView1.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.treeView1_AfterSelect);
			// 
			// toolBar1
			// 
			this.toolBar1.Buttons.AddRange(new TD.SandBar.ToolbarItemBase[] {
																				this.buttonItem1,
																				this.buttonItem2,
																				this.buttonItem3,
																				this.buttonItem4,
																				this.buttonItem5});
			this.toolBar1.Guid = new System.Guid("4a99ed51-9593-4823-b667-26c0cee66eed");
			this.toolBar1.IsOpen = true;
			this.toolBar1.Location = new System.Drawing.Point(0, 0);
			this.toolBar1.Name = "toolBar1";
			this.toolBar1.Size = new System.Drawing.Size(656, 24);
			this.toolBar1.TabIndex = 1;
			this.toolBar1.Text = "toolBar1";
			// 
			// buttonItem1
			// 
			this.buttonItem1.Text = "New Technique";
			// 
			// buttonItem2
			// 
			this.buttonItem2.Text = "New Pass";
			this.buttonItem2.Activate += new System.EventHandler(this.buttonItem2_Activate);
			// 
			// buttonItem3
			// 
			this.buttonItem3.Text = "New Tex Unit";
			this.buttonItem3.Activate += new System.EventHandler(this.buttonItem3_Activate);
			// 
			// buttonItem4
			// 
			this.buttonItem4.Text = "Vertex Program";
			// 
			// buttonItem5
			// 
			this.buttonItem5.Text = "Fragment Program";
			// 
			// panel1
			// 
			this.panel1.Controls.Add(this.propertyGrid1);
			this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.panel1.Location = new System.Drawing.Point(168, 0);
			this.panel1.Name = "panel1";
			this.panel1.Size = new System.Drawing.Size(488, 467);
			this.panel1.TabIndex = 3;
			// 
			// propertyGrid1
			// 
			this.propertyGrid1.CommandsVisibleIfAvailable = true;
			this.propertyGrid1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.propertyGrid1.LargeButtons = false;
			this.propertyGrid1.LineColor = System.Drawing.SystemColors.ScrollBar;
			this.propertyGrid1.Location = new System.Drawing.Point(0, 0);
			this.propertyGrid1.Name = "propertyGrid1";
			this.propertyGrid1.Size = new System.Drawing.Size(488, 467);
			this.propertyGrid1.TabIndex = 3;
			this.propertyGrid1.Text = "propertyGrid1";
			this.propertyGrid1.ViewBackColor = System.Drawing.SystemColors.Window;
			this.propertyGrid1.ViewForeColor = System.Drawing.SystemColors.WindowText;
			// 
			// panel2
			// 
			this.panel2.Controls.Add(this.panel1);
			this.panel2.Controls.Add(this.splitter2);
			this.panel2.Controls.Add(this.treeView1);
			this.panel2.Dock = System.Windows.Forms.DockStyle.Fill;
			this.panel2.Location = new System.Drawing.Point(0, 24);
			this.panel2.Name = "panel2";
			this.panel2.Size = new System.Drawing.Size(656, 467);
			this.panel2.TabIndex = 4;
			// 
			// splitter2
			// 
			this.splitter2.Location = new System.Drawing.Point(160, 0);
			this.splitter2.Name = "splitter2";
			this.splitter2.Size = new System.Drawing.Size(8, 467);
			this.splitter2.TabIndex = 1;
			this.splitter2.TabStop = false;
			// 
			// techniqueMenu
			// 
			this.techniqueMenu.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
																						  this.menuItem1,
																						  this.menuItem5});
			// 
			// menuItem1
			// 
			this.menuItem1.Index = 0;
			this.menuItem1.Text = "&New Technique";
			// 
			// menuItem5
			// 
			this.menuItem5.Index = 1;
			this.menuItem5.Text = "&Delete Technique";
			// 
			// passMenu
			// 
			this.passMenu.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
																					 this.menuItem2,
																					 this.menuItem3,
																					 this.menuItem7,
																					 this.menuItem8,
																					 this.menuItem9,
																					 this.menuItem10,
																					 this.menuItem11});
			// 
			// menuItem2
			// 
			this.menuItem2.Index = 0;
			this.menuItem2.Text = "&New Pass";
			// 
			// menuItem3
			// 
			this.menuItem3.Index = 1;
			this.menuItem3.Text = "&Delete Pass";
			// 
			// menuItem7
			// 
			this.menuItem7.Index = 2;
			this.menuItem7.Text = "-";
			// 
			// menuItem8
			// 
			this.menuItem8.Index = 3;
			this.menuItem8.Text = "Add &Vertex Shader Reference";
			// 
			// menuItem9
			// 
			this.menuItem9.Index = 4;
			this.menuItem9.Text = "Add &Pixel Shader Reference";
			// 
			// menuItem10
			// 
			this.menuItem10.Index = 5;
			this.menuItem10.Text = "Delete V&ertex Shader Reference";
			// 
			// menuItem11
			// 
			this.menuItem11.Index = 6;
			this.menuItem11.Text = "Delete P&ixel Shader Reference";
			// 
			// texUnitMenu
			// 
			this.texUnitMenu.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
																						this.menuItem4,
																						this.menuItem6});
			// 
			// menuItem4
			// 
			this.menuItem4.Index = 0;
			this.menuItem4.Text = "&New Texture Unit";
			// 
			// menuItem6
			// 
			this.menuItem6.Index = 1;
			this.menuItem6.Text = "&Delete Texture Unit";
			// 
			// VisualEditor
			// 
			this.Controls.Add(this.panel2);
			this.Controls.Add(this.toolBar1);
			this.Name = "VisualEditor";
			this.Size = new System.Drawing.Size(656, 491);
			this.Load += new System.EventHandler(this.VisualEditor_Load);
			this.panel1.ResumeLayout(false);
			this.panel2.ResumeLayout(false);
			this.ResumeLayout(false);

		}
		#endregion

		private void VisualEditor_Load(object sender, System.EventArgs e) {
		
		}

		private void treeView1_AfterSelect(object sender, System.Windows.Forms.TreeViewEventArgs e) {
			if(e.Node.Tag is Material)
				this.propertyGrid1.SelectedObject = new MaterialWrapper(e.Node.Tag as Material);
			else if(e.Node.Tag is Technique)
				this.propertyGrid1.SelectedObject = new TechniqueWrapper(e.Node.Tag as Technique);
			else if(e.Node.Tag is Pass)
				this.propertyGrid1.SelectedObject = new PassWrapper(e.Node.Tag as Pass);
			else if(e.Node.Tag is TextureUnitState)
				this.propertyGrid1.SelectedObject = new TextureUnitStateWrapper(e.Node.Tag as TextureUnitState);
		}

		private void buttonItem2_Activate(object sender, System.EventArgs e) {
			if(this.treeView1.SelectedNode.Tag is Technique) {
				(treeView1.SelectedNode.Tag as Technique).CreatePass();
				parseMaterialIntoTree();
			}
		}

		private void buttonItem3_Activate(object sender, System.EventArgs e) {
			if(this.treeView1.SelectedNode.Tag is Pass) {
				(treeView1.SelectedNode.Tag as Pass).CreateTextureUnitState();
				parseMaterialIntoTree();
			}
		}

		public Material Material {
			set {
				this.treeView1.Nodes.Clear();
				material = value;
				parseMaterialIntoTree();
			}
		}

		public void parseMaterialIntoTree() {
			this.treeView1.Nodes.Clear();
			for(int i=0; i<material.NumTechniques;i++) {
				Technique t = material.GetTechnique(i);
				TreeNode n = new TreeNode(t.Name != null && t.Name.Length > 0 ? t.Name : "Technique " + i.ToString());
				n.Tag = t;
				for(int j=0; j<t.NumPasses; j++) {
					Pass p = t.GetPass(j);
					TreeNode nn = new TreeNode("Pass " + j.ToString());
					nn.Tag = p;
					for(int k=0; k < p.NumTextureUnitStages; k++) {
						TextureUnitState tx = p.GetTextureUnitState(k);
						TreeNode nnn = new TreeNode("Texture Unit " + k.ToString());
						nnn.Tag = p.GetTextureUnitState(k);
						nn.Nodes.Add(nnn);
					}
					n.Nodes.Add(nn);
				}
				this.treeView1.Nodes.Add(n);
				this.treeView1.ExpandAll();
			}
		}
	}
}
