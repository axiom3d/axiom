using System;
using System.Drawing;
using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows.Forms;
using System.IO;
using Axiom.Core;
using Axiom.Graphics;
using Axiom.Math;
using Axiom.Math.Collections;
using Chronos.Core;

namespace MaterialLibraryPlugin {

	/// <summary>
	/// Summary description for MaterialLibrary.
	/// </summary>
	public class MaterialLibrary : System.Windows.Forms.Form {

		private System.Windows.Forms.Splitter splitter1;
		private System.Windows.Forms.ContextMenu materialMenu;
		private System.Windows.Forms.ImageList imageList1;
		private System.Windows.Forms.MenuItem menuItem1;
		private System.Windows.Forms.MenuItem editEntry;
		private System.Windows.Forms.MenuItem delEntry;
		private System.Windows.Forms.MenuItem addMaterial;
		private System.Windows.Forms.MenuItem addVS;
		private System.Windows.Forms.MenuItem addFS;
		private System.ComponentModel.IContainer components;

		private Axiom.Graphics.RenderWindow renderWindow;
		private Axiom.Core.Viewport viewport;
		private Axiom.Core.Camera camera;
		private Entity previewEntity;
		private SceneNode previewNode, previewRotator, lightRotator1, lightRotator2;
		private Light previewLight1, previewLight2;
		private System.Windows.Forms.ToolBar toolBar1;
		private System.Windows.Forms.ToolBarButton addButton;
		private System.Windows.Forms.ToolBarButton editSelButton;
		private System.Windows.Forms.ToolBarButton delSelButton;
		private System.Windows.Forms.Panel panel3;
		private System.Windows.Forms.Panel panel2;
		private System.Windows.Forms.Panel panel1;
		private System.Windows.Forms.ToolBarButton rotateMesh;
		private System.Windows.Forms.ToolBarButton light1Active;
		private System.Windows.Forms.ToolBarButton light2Active;
		private System.Windows.Forms.ToolBarButton light3Active;
		private System.Windows.Forms.PictureBox materialPreview;
		private System.Windows.Forms.ToolBar previewToolbar;
		private System.Windows.Forms.ImageList imageList2;
		private System.Windows.Forms.MenuItem menuItem2;
		private System.Windows.Forms.MainMenu mainMenu1;
		private System.Windows.Forms.ToolBarButton toolBarButton1;
		private System.Windows.Forms.ListBox listBox1;
		private System.Windows.Forms.Panel panel4;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Panel panel5;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.ComboBox comboBox2;
		private System.Windows.Forms.ComboBox comboBox1;
		private TD.SandBar.ToolBar toolBar2;
		private TD.SandBar.ButtonItem buttonItem1;
		private TD.SandBar.ButtonItem buttonItem2;
		private ColorEx ambientLight;

		public MaterialLibrary() {
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			renderWindow = Root.Instance.CreateRenderWindow(
				"__materialLibRenderWindow", Target.Width, Target.Height,
				32, false, 0, 0, true, false, Target
				);
			this.Target.Visible = true;

			camera = Root.Instance.SceneManager.CreateCamera("__matpreview");
			if(renderWindow == null)
				throw new Exception("Attempting to use a null RenderWindow for material preview.");
			// create a new viewport and set it's background color
			viewport = renderWindow.AddViewport(camera, 0, 0, 100, 100, 99);
			viewport.BackgroundColor = ColorEx.Black;
			//viewport.OverlaysEnabled = false;

			SceneManager sm = Root.Instance.SceneManager;

			previewEntity = sm.CreateEntity("__materialPreview", "Editor/sphere.mesh");
			previewEntity.IsVisible = false;
			previewEntity.CastShadows = false;
			previewNode = sm.CreateSceneNode();
			//sm.RootSceneNode.AddChild(previewNode);
			
			float l = (previewEntity.BoundingBox.Maximum - previewEntity.BoundingBox.Minimum).Length;

			previewRotator = previewNode.CreateChildSceneNode();
			previewRotator.AttachObject(previewEntity);
			previewRotator.Rotate(new Vector3(1,0,0), -90);

			lightRotator1 = previewNode.CreateChildSceneNode();
			lightRotator2 = previewNode.CreateChildSceneNode();

			SceneNode lightNode1 = lightRotator1.CreateChildSceneNode(new Vector3(l, 0, 0));
			SceneNode lightNode2 = lightRotator2.CreateChildSceneNode(new Vector3(0, l, 0));
			previewLight1 = sm.CreateLight("__matpreviewlight1");
			previewLight2 = sm.CreateLight("__matpreviewlight2");
			previewLight1.CastShadows = false;
			previewLight2.CastShadows = false;
			lightNode1.AttachObject(previewLight1);
			lightNode2.AttachObject(previewLight2);

			SceneNode cameraNode = previewNode.CreateChildSceneNode(new Vector3(0,0,l));
			cameraNode.AttachObject(camera);
			cameraNode.SetAutoTracking(true, previewNode);
			
			renderWindow.BeforeViewportUpdate += new ViewportUpdateEventHandler(renderWindow_BeforeViewportUpdate);
			renderWindow.AfterViewportUpdate += new ViewportUpdateEventHandler(renderWindow_AfterViewportUpdate);
			Root.Instance.FrameStarted +=new FrameEvent(Root_FrameStarted);
		}

		public void UpdateDisplay() {
			Hashtable classList = MaterialEntryManager.Instance.ClassList;
			comboBox1.Items.Clear();
			foreach(string keyName in classList.Keys)
				comboBox1.Items.Add(keyName);
			comboBox1.SelectedIndex = 0;
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing ) {
			if( disposing ) {
				if(components != null) {
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
		private void InitializeComponent() {
			this.components = new System.ComponentModel.Container();
			System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(MaterialLibrary));
			this.imageList1 = new System.Windows.Forms.ImageList(this.components);
			this.materialMenu = new System.Windows.Forms.ContextMenu();
			this.menuItem1 = new System.Windows.Forms.MenuItem();
			this.addMaterial = new System.Windows.Forms.MenuItem();
			this.addVS = new System.Windows.Forms.MenuItem();
			this.addFS = new System.Windows.Forms.MenuItem();
			this.editEntry = new System.Windows.Forms.MenuItem();
			this.menuItem2 = new System.Windows.Forms.MenuItem();
			this.delEntry = new System.Windows.Forms.MenuItem();
			this.splitter1 = new System.Windows.Forms.Splitter();
			this.toolBar1 = new System.Windows.Forms.ToolBar();
			this.addButton = new System.Windows.Forms.ToolBarButton();
			this.editSelButton = new System.Windows.Forms.ToolBarButton();
			this.delSelButton = new System.Windows.Forms.ToolBarButton();
			this.toolBarButton1 = new System.Windows.Forms.ToolBarButton();
			this.panel3 = new System.Windows.Forms.Panel();
			this.listBox1 = new System.Windows.Forms.ListBox();
			this.panel5 = new System.Windows.Forms.Panel();
			this.comboBox2 = new System.Windows.Forms.ComboBox();
			this.label2 = new System.Windows.Forms.Label();
			this.panel2 = new System.Windows.Forms.Panel();
			this.panel1 = new System.Windows.Forms.Panel();
			this.materialPreview = new System.Windows.Forms.PictureBox();
			this.previewToolbar = new System.Windows.Forms.ToolBar();
			this.rotateMesh = new System.Windows.Forms.ToolBarButton();
			this.light1Active = new System.Windows.Forms.ToolBarButton();
			this.light2Active = new System.Windows.Forms.ToolBarButton();
			this.imageList2 = new System.Windows.Forms.ImageList(this.components);
			this.light3Active = new System.Windows.Forms.ToolBarButton();
			this.mainMenu1 = new System.Windows.Forms.MainMenu();
			this.panel4 = new System.Windows.Forms.Panel();
			this.comboBox1 = new System.Windows.Forms.ComboBox();
			this.label1 = new System.Windows.Forms.Label();
			this.toolBar2 = new TD.SandBar.ToolBar();
			this.buttonItem1 = new TD.SandBar.ButtonItem();
			this.buttonItem2 = new TD.SandBar.ButtonItem();
			this.panel3.SuspendLayout();
			this.panel5.SuspendLayout();
			this.panel2.SuspendLayout();
			this.panel1.SuspendLayout();
			this.panel4.SuspendLayout();
			this.SuspendLayout();
			// 
			// imageList1
			// 
			this.imageList1.ImageSize = new System.Drawing.Size(21, 21);
			this.imageList1.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageList1.ImageStream")));
			this.imageList1.TransparentColor = System.Drawing.Color.Transparent;
			// 
			// materialMenu
			// 
			this.materialMenu.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
																						 this.menuItem1,
																						 this.editEntry,
																						 this.menuItem2,
																						 this.delEntry});
			// 
			// menuItem1
			// 
			this.menuItem1.Index = 0;
			this.menuItem1.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
																					  this.addMaterial,
																					  this.addVS,
																					  this.addFS});
			this.menuItem1.Text = "Add &New...";
			// 
			// addMaterial
			// 
			this.addMaterial.Index = 0;
			this.addMaterial.Text = "&Material";
			// 
			// addVS
			// 
			this.addVS.Index = 1;
			this.addVS.Text = "&Vertex Shader";
			// 
			// addFS
			// 
			this.addFS.Index = 2;
			this.addFS.Text = "&Fragment Shader";
			// 
			// editEntry
			// 
			this.editEntry.Index = 1;
			this.editEntry.Text = "Edit &Script";
			this.editEntry.Click += new System.EventHandler(this.editMaterialItem_Click);
			// 
			// menuItem2
			// 
			this.menuItem2.Index = 2;
			this.menuItem2.Text = "Edit &Material";
			this.menuItem2.Click += new System.EventHandler(this.menuItem2_Click);
			// 
			// delEntry
			// 
			this.delEntry.Index = 3;
			this.delEntry.Text = "&Delete Entry";
			// 
			// splitter1
			// 
			this.splitter1.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.splitter1.Location = new System.Drawing.Point(0, 273);
			this.splitter1.Name = "splitter1";
			this.splitter1.Size = new System.Drawing.Size(224, 2);
			this.splitter1.TabIndex = 4;
			this.splitter1.TabStop = false;
			// 
			// toolBar1
			// 
			this.toolBar1.Appearance = System.Windows.Forms.ToolBarAppearance.Flat;
			this.toolBar1.Buttons.AddRange(new System.Windows.Forms.ToolBarButton[] {
																						this.addButton,
																						this.editSelButton,
																						this.delSelButton,
																						this.toolBarButton1});
			this.toolBar1.ButtonSize = new System.Drawing.Size(21, 21);
			this.toolBar1.DropDownArrows = true;
			this.toolBar1.ImageList = this.imageList1;
			this.toolBar1.Location = new System.Drawing.Point(0, 0);
			this.toolBar1.Name = "toolBar1";
			this.toolBar1.ShowToolTips = true;
			this.toolBar1.Size = new System.Drawing.Size(224, 33);
			this.toolBar1.TabIndex = 10;
			this.toolBar1.ButtonClick += new System.Windows.Forms.ToolBarButtonClickEventHandler(this.toolBar1_ButtonClick_1);
			// 
			// addButton
			// 
			this.addButton.Enabled = false;
			this.addButton.ImageIndex = 0;
			// 
			// editSelButton
			// 
			this.editSelButton.Enabled = false;
			this.editSelButton.ImageIndex = 1;
			// 
			// delSelButton
			// 
			this.delSelButton.Enabled = false;
			this.delSelButton.ImageIndex = 2;
			// 
			// toolBarButton1
			// 
			this.toolBarButton1.ImageIndex = 3;
			// 
			// panel3
			// 
			this.panel3.Controls.Add(this.listBox1);
			this.panel3.Controls.Add(this.panel5);
			this.panel3.Controls.Add(this.toolBar1);
			this.panel3.Dock = System.Windows.Forms.DockStyle.Fill;
			this.panel3.Location = new System.Drawing.Point(0, 31);
			this.panel3.Name = "panel3";
			this.panel3.Size = new System.Drawing.Size(224, 242);
			this.panel3.TabIndex = 9;
			// 
			// listBox1
			// 
			this.listBox1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.listBox1.Location = new System.Drawing.Point(0, 65);
			this.listBox1.Name = "listBox1";
			this.listBox1.Size = new System.Drawing.Size(224, 173);
			this.listBox1.TabIndex = 11;
			this.listBox1.DoubleClick += new System.EventHandler(this.listBox1_DoubleClick);
			this.listBox1.SelectedIndexChanged += new System.EventHandler(this.listBox1_SelectedIndexChanged);
			// 
			// panel5
			// 
			this.panel5.Controls.Add(this.comboBox2);
			this.panel5.Controls.Add(this.label2);
			this.panel5.Dock = System.Windows.Forms.DockStyle.Top;
			this.panel5.Location = new System.Drawing.Point(0, 33);
			this.panel5.Name = "panel5";
			this.panel5.Size = new System.Drawing.Size(224, 32);
			this.panel5.TabIndex = 13;
			// 
			// comboBox2
			// 
			this.comboBox2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
				| System.Windows.Forms.AnchorStyles.Right)));
			this.comboBox2.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBox2.Location = new System.Drawing.Point(48, 6);
			this.comboBox2.Name = "comboBox2";
			this.comboBox2.Size = new System.Drawing.Size(176, 21);
			this.comboBox2.TabIndex = 15;
			this.comboBox2.SelectedIndexChanged += new System.EventHandler(this.comboBox2_SelectedIndexChanged);
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(8, 8);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(29, 16);
			this.label2.TabIndex = 14;
			this.label2.Text = "Pack";
			// 
			// panel2
			// 
			this.panel2.Controls.Add(this.panel1);
			this.panel2.Controls.Add(this.previewToolbar);
			this.panel2.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.panel2.Location = new System.Drawing.Point(0, 275);
			this.panel2.Name = "panel2";
			this.panel2.Size = new System.Drawing.Size(224, 184);
			this.panel2.TabIndex = 8;
			// 
			// panel1
			// 
			this.panel1.BackColor = System.Drawing.Color.Black;
			this.panel1.Controls.Add(this.materialPreview);
			this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.panel1.Location = new System.Drawing.Point(0, 28);
			this.panel1.Name = "panel1";
			this.panel1.Size = new System.Drawing.Size(224, 156);
			this.panel1.TabIndex = 15;
			this.panel1.Resize += new System.EventHandler(this.panel1_Resize);
			// 
			// materialPreview
			// 
			this.materialPreview.BackColor = System.Drawing.Color.Black;
			this.materialPreview.Location = new System.Drawing.Point(64, 64);
			this.materialPreview.Name = "materialPreview";
			this.materialPreview.Size = new System.Drawing.Size(88, 40);
			this.materialPreview.TabIndex = 18;
			this.materialPreview.TabStop = false;
			// 
			// previewToolbar
			// 
			this.previewToolbar.Appearance = System.Windows.Forms.ToolBarAppearance.Flat;
			this.previewToolbar.Buttons.AddRange(new System.Windows.Forms.ToolBarButton[] {
																							  this.rotateMesh,
																							  this.light1Active,
																							  this.light2Active});
			this.previewToolbar.ButtonSize = new System.Drawing.Size(16, 16);
			this.previewToolbar.DropDownArrows = true;
			this.previewToolbar.ImageList = this.imageList2;
			this.previewToolbar.Location = new System.Drawing.Point(0, 0);
			this.previewToolbar.Name = "previewToolbar";
			this.previewToolbar.ShowToolTips = true;
			this.previewToolbar.Size = new System.Drawing.Size(224, 28);
			this.previewToolbar.TabIndex = 16;
			// 
			// rotateMesh
			// 
			this.rotateMesh.ImageIndex = 0;
			this.rotateMesh.Pushed = true;
			this.rotateMesh.Style = System.Windows.Forms.ToolBarButtonStyle.ToggleButton;
			this.rotateMesh.ToolTipText = "Toggle mesh rotation";
			// 
			// light1Active
			// 
			this.light1Active.ImageIndex = 1;
			this.light1Active.Pushed = true;
			this.light1Active.Style = System.Windows.Forms.ToolBarButtonStyle.ToggleButton;
			this.light1Active.ToolTipText = "Toggle light #1";
			// 
			// light2Active
			// 
			this.light2Active.ImageIndex = 2;
			this.light2Active.Style = System.Windows.Forms.ToolBarButtonStyle.ToggleButton;
			this.light2Active.ToolTipText = "Toggle light #2";
			// 
			// imageList2
			// 
			this.imageList2.ColorDepth = System.Windows.Forms.ColorDepth.Depth16Bit;
			this.imageList2.ImageSize = new System.Drawing.Size(16, 16);
			this.imageList2.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageList2.ImageStream")));
			this.imageList2.TransparentColor = System.Drawing.Color.Transparent;
			// 
			// light3Active
			// 
			this.light3Active.Style = System.Windows.Forms.ToolBarButtonStyle.ToggleButton;
			// 
			// panel4
			// 
			this.panel4.Controls.Add(this.comboBox1);
			this.panel4.Controls.Add(this.label1);
			this.panel4.Dock = System.Windows.Forms.DockStyle.Top;
			this.panel4.Location = new System.Drawing.Point(0, 31);
			this.panel4.Name = "panel4";
			this.panel4.Size = new System.Drawing.Size(224, 32);
			this.panel4.TabIndex = 16;
			// 
			// comboBox1
			// 
			this.comboBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
				| System.Windows.Forms.AnchorStyles.Right)));
			this.comboBox1.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBox1.Location = new System.Drawing.Point(48, 8);
			this.comboBox1.Name = "comboBox1";
			this.comboBox1.Size = new System.Drawing.Size(176, 21);
			this.comboBox1.TabIndex = 15;
			this.comboBox1.SelectedIndexChanged += new System.EventHandler(this.comboBox1_SelectedIndexChanged);
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(8, 8);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(33, 16);
			this.label1.TabIndex = 14;
			this.label1.Text = "Class";
			// 
			// toolBar2
			// 
			this.toolBar2.Buttons.AddRange(new TD.SandBar.ToolbarItemBase[] {
																				this.buttonItem1,
																				this.buttonItem2});
			this.toolBar2.Guid = new System.Guid("b9bc20a5-c120-402c-bd14-4f89aaf4c7ad");
			this.toolBar2.ImageList = this.imageList1;
			this.toolBar2.IsOpen = true;
			this.toolBar2.Location = new System.Drawing.Point(0, 0);
			this.toolBar2.Name = "toolBar2";
			this.toolBar2.Size = new System.Drawing.Size(224, 31);
			this.toolBar2.TabIndex = 17;
			this.toolBar2.Text = "toolBar2";
			// 
			// buttonItem1
			// 
			this.buttonItem1.ImageIndex = 3;
			this.buttonItem1.Activate += new System.EventHandler(this.buttonItem1_Activate);
			// 
			// MaterialLibrary
			// 
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.ClientSize = new System.Drawing.Size(224, 459);
			this.Controls.Add(this.panel4);
			this.Controls.Add(this.panel3);
			this.Controls.Add(this.splitter1);
			this.Controls.Add(this.panel2);
			this.Controls.Add(this.toolBar2);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Menu = this.mainMenu1;
			this.Name = "MaterialLibrary";
			this.Text = "Material Library";
			this.panel3.ResumeLayout(false);
			this.panel5.ResumeLayout(false);
			this.panel2.ResumeLayout(false);
			this.panel1.ResumeLayout(false);
			this.panel4.ResumeLayout(false);
			this.ResumeLayout(false);

		}
		#endregion

		public PictureBox Target {
			get { return materialPreview; }
		}

		public Axiom.Graphics.RenderWindow RenderWindow {
			get { return renderWindow; }
			set { renderWindow = value; }
		}

		private void treeView1_AfterSelect(object sender, System.Windows.Forms.TreeViewEventArgs e) {
			//propertyGrid1.SelectedObject = e.Node.Tag;
			TreeView t = sender as TreeView;
			this.addButton.Enabled = false;
			this.editSelButton.Enabled = false;
			this.delSelButton.Enabled = false;
			TreeNode tn = t.SelectedNode;
			previewEntity.IsVisible = false;
			if(tn != null) {
				while(tn.Parent != null) {
					if(tn.Tag is Material || t.SelectedNode.Tag is GpuProgram) {
						this.addButton.Enabled = true;
						this.editSelButton.Enabled = true;
						this.delSelButton.Enabled = true;
						if(tn.Tag is Material) {
							previewEntity.MaterialName = tn.Text;
							previewEntity.IsVisible = true;
						}
						break;
					}
					tn = tn.Parent;
				}
			}
		}

		private void toolBar1_ButtonClick(object sender, System.Windows.Forms.ToolBarButtonClickEventArgs e) {
		}

		private void editMaterialItem_Click(object sender, System.EventArgs e) {
			/*
			EditorForm f = new EditorForm();
			f.Text = treeView1.SelectedNode.Text;
			TreeNode s = treeView1.SelectedNode;
			if(s == null) return;
			while(s.Parent != null && s.Parent.Parent != null)
				s = s.Parent;
			StringCollection file = ResourceManager.GetAllCommonNamesLike("", s.Text);
			try {
				Stream fs = MaterialManager.Instance.FindResourceData(file[0].ToString());
				f.LoadStream(fs, "_editor.material");
				f.Filename = file[0].ToString();
				fs.Close();
			}
			catch(Exception except) {
				System.Diagnostics.Trace.Write(String.Format(
					"Could not load material {0}: {1}",
					s.Text,
					except.Message));
			}

			DocumentEventHook d = GuiManager.Instance.CreateDocument(f, s.Text);
			d.Closing += new Chronos.Core.DocumentEventHook.DocumentEventArgsDelegate(d_Closing);
			*/
		}

		private void treeView1_MouseUp(object sender, System.Windows.Forms.MouseEventArgs e) {
			if(e.Button != MouseButtons.Right) return;
			TreeNode n = (sender as TreeView).GetNodeAt(e.X, e.Y);
			(sender as TreeView).SelectedNode = n;
			materialMenu.Show(sender as TreeView, new Point(e.X, e.Y));
		}

		private void renderWindow_BeforeViewportUpdate(object sender, ViewportUpdateEventArgs e) {
			SceneManager sm = Root.Instance.SceneManager;
			//previewNode.Visible = true;
			sm.OverrideRootSceneNode(previewNode);

			ambientLight = sm.AmbientLight;
			sm.AmbientLight = ColorEx.FromColor(Color.FromArgb(100,100,100));
		}

		private void renderWindow_AfterViewportUpdate(object sender, ViewportUpdateEventArgs e) {
			SceneManager sm = Root.Instance.SceneManager;
			//previewNode.Visible = false;
			sm.RestoreRootSceneNode();

			if(ambientLight != null)
				sm.AmbientLight = ambientLight;
		}

		private void Root_FrameStarted(object source, FrameEventArgs e) {
			if(this.rotateMesh.Pushed)
				previewRotator.Rotate(new Vector3(0,0,1), 36 * e.TimeSinceLastFrame);
			lightRotator1.Rotate(new Vector3(0,1,0), 30 * -e.TimeSinceLastFrame);
			lightRotator2.Rotate(new Vector3(1,0,0), 40 * -e.TimeSinceLastFrame);
			previewLight1.IsVisible = this.light1Active.Pushed;
			previewLight2.IsVisible = this.light2Active.Pushed;
		}

		private void panel1_Resize(object sender, System.EventArgs e) {
			int w = (sender as Panel).Width;
			int h = (sender as Panel).Height;
			if(w > h * 1.25) {
				materialPreview.Height = h;
				materialPreview.Width = (int)((double)h * 1.25);
				materialPreview.Top = 0;
				materialPreview.Left = (w-materialPreview.Width)/2;
			} 
			else {
				materialPreview.Width = w;
				materialPreview.Height =(int)((double)w * 0.75);
				materialPreview.Top = (h-materialPreview.Height)/2;
				materialPreview.Left = 0;
			}
			renderWindow.Resize(materialPreview.Width, materialPreview.Height);
			(viewport as Viewport).SetDimensions(0, 0, 100, 100);
		}

		private void treeView1_DoubleClick(object sender, System.EventArgs e) {
			TreeNode n = (sender as TreeView).SelectedNode;
			if(n != null && n.Tag is Material)
				MaterialLibraryPlugin.Instance.FireMaterialChanged(sender, n.Tag as Material);
		}

		private void menuItem2_Click(object sender, System.EventArgs e) {
			/*VisualEditor v = new VisualEditor();
			v.Material = MaterialManager.Instance.GetByName(treeView1.SelectedNode.Text);
			DocumentEventHook d = GuiManager.Instance.CreateDocument(v, treeView1.SelectedNode.Text);
			d.Closing += new Chronos.Core.DocumentEventHook.DocumentEventArgsDelegate(d_Closing);
			*/
		}

		private void d_Closing(object sender, ref DocumentEventArgs e) {
			DialogResult d = MessageBox.Show("Do you wish to save? (Currently unimplemented.)", "Chronos Alert", MessageBoxButtons.YesNoCancel);
			if(d == DialogResult.Yes) {
				e.Cancel = false;
			} else if(d == DialogResult.No) {
				e.Cancel = false;
			}else {
				e.Cancel = true;
			}
		}

		private void toolBar1_ButtonClick_1(object sender, System.Windows.Forms.ToolBarButtonClickEventArgs e) {
			MaterialLibraryPlugin.Instance.ImportMaterial();
		}

		private void comboBox2_SelectedIndexChanged(object sender, System.EventArgs e) {
			listBox1.Items.Clear();
			Hashtable t = MaterialEntryManager.Instance.ClassList[comboBox1.SelectedItem.ToString()] as Hashtable;
			if(t == null) return;
			ArrayList materialList = t[comboBox2.SelectedItem.ToString()] as ArrayList;
			if(materialList == null) return;
			listBox1.Items.AddRange(materialList.ToArray());
			byte[] b = new byte[1024];
		}

		private void comboBox1_SelectedIndexChanged(object sender, System.EventArgs e) {
			Hashtable t = MaterialEntryManager.Instance.ClassList[(sender as ComboBox).SelectedItem.ToString()] as Hashtable;
			comboBox2.Items.Clear();
			if(t == null) return;
			foreach(string s in t.Keys) {
				comboBox2.Items.Add(s);
			}
			if(comboBox2.Items.Count > 0)
				comboBox2.SelectedIndex = 0;
		}

		private void listBox1_SelectedIndexChanged(object sender, System.EventArgs e) {
			previewNode.Visible = true;
			previewEntity.IsVisible = true;
			previewEntity.MaterialName = ((sender as ListBox).SelectedItem as Block).FullName;
		}

		private void listBox1_DoubleClick(object sender, System.EventArgs e) {
			Material mat = MaterialManager.Instance.GetByName((listBox1.SelectedItem as Block).FullName);
			if(mat != null)
				MaterialLibraryPlugin.Instance.FireMaterialChanged(sender, mat);
		}

		private void buttonItem1_Activate(object sender, System.EventArgs e) {
			MaterialLibraryPlugin.Instance.ImportMaterial();
		}
	}
}
