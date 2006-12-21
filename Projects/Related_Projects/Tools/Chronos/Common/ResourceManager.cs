using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Configuration;
using System.Xml;
using System.IO;
using System.Text;
using Axiom.Graphics;
using Chronos.Core;

namespace Chronos.Core
{
	/// <summary>
	/// Summary description for ImportMaterial.
	/// </summary>
	public class ResourceManagerForm : System.Windows.Forms.Form
	{
		#region Fields
		private System.Windows.Forms.OpenFileDialog openFileDialog1;
		private const int LVM_EDIT = 0x1000 + 23;

		private string mediaPath;
		private TD.SandBar.MenuBar menuBar1;
		private TD.SandBar.MenuBarItem menuBarItem1;
		private TD.SandBar.MenuButtonItem menuButtonItem1;
		private TD.SandBar.MenuButtonItem menuButtonItem2;
		private TD.SandBar.ToolBar toolBar1;
		private TD.SandBar.ButtonItem buttonItem1;
		private TD.SandBar.ButtonItem buttonItem2;
		private System.Windows.Forms.ImageList imageList1;
		private System.Windows.Forms.ImageList imageList2;
		private System.Windows.Forms.ImageList imageList3;
		private TD.SandBar.MenuButtonItem menuButtonItem3;
		private TD.SandBar.MenuButtonItem menuButtonItem4;
		private TD.SandBar.MenuButtonItem menuButtonItem5;
		private TD.SandBar.MenuButtonItem menuButtonItem6;
		private System.Windows.Forms.ContextMenu contextMenu1;
		private System.Windows.Forms.MenuItem menuItem1;
		private System.Windows.Forms.MenuItem menuItem2;
		private System.Windows.Forms.MenuItem menuItem3;
		private System.Windows.Forms.MenuItem menuItem4;
		private TD.SandBar.DropDownMenuItem dropDownMenuItem1;
		private TD.SandBar.MenuButtonItem menuButtonItem7;
		private TD.SandBar.MenuButtonItem menuButtonItem8;
		private TD.SandBar.MenuButtonItem menuButtonItem9;
		private System.Windows.Forms.TreeView treeView1;
		private System.Windows.Forms.Splitter splitter1;
		private DocumentManager.DocumentManager documentManager1;
		private System.ComponentModel.IContainer components;
		private System.Windows.Forms.Panel panel1;
		private TD.SandBar.MenuBarItem menuBarItem2;
		private int listCount = 0;
		private Hashtable friendlyNames = new Hashtable();
		#endregion

		private struct ResourceHandlerInfo {
			public string filter;
			public string name;
		}

		[System.Runtime.InteropServices.DllImport("user32.dll",EntryPoint="SendMessage")]
		private static extern int SendMessage(int _WindowHandler, int _WM_USER, int _data, int _id);

		#region Singleton implementation

		protected static ResourceManagerForm instance;

		public static ResourceManagerForm Instance {
			get { 
				return instance; 
			}
		}

		public static void Init() {
			if (instance != null) {
				throw new ApplicationException("ResourceManagerForm.Instance is null!");
			}
			instance = new ResourceManagerForm();
			Chronos.GarbageManager.Instance.Add(instance);
		}

		protected ResourceManagerForm() {
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();
			AddResourceHandler("All Resources", "*.*", "Resource");
			GuiManager.Instance.AdoptForm(this);

			string fileName = ConfigurationSettings.AppSettings["editorSettingsFile"];

			if(!File.Exists(fileName)) {
				string message = "The file, " + fileName + ", does not exist.";
				System.Diagnostics.Trace.Write(message);
				return;
			}
		
			// Load config from XML
			XmlTextReader reader = null;
			try {
				reader = new XmlTextReader(fileName);
				while(reader.Read()) {
					if(reader.NodeType == XmlNodeType.Element) {
						if(reader.LocalName.ToLower().Equals("media")) {
							mediaPath = reader.GetAttribute("path");
						}
					}
				}
			} finally {
				if(reader != null)
					reader.Close();
			}
			if(mediaPath == null) {
				throw new System.Exception("Common plugin: Unable to find media node in editor settings file.");
			} else {
				updateClasses();
			}
		}
		#endregion

		public void AddResourceHandler(string name, string filter, string friendlyName) {
			string[] bits = filter.Split(';');
			foreach(string bit in bits) {
				friendlyNames.Add(bit.Trim(), friendlyName);
			}
			
			ResourceHandlerInfo info = new ResourceHandlerInfo();
			info.filter = filter;
			info.name = name;

			// Set up the toolbar

			// Set up the menu bar

			// Set up the document
			DocumentManager.Document doc = new DocumentManager.Document(getNewList(), name);
			doc.Tag = filter;
			documentManager1.AddDocument(doc);
		}

		private ListView getNewList() {
			// 
			// listView1
			// 
			ListView newList = new ListView();
			newList.AllowColumnReorder = true;
			newList.AllowDrop = true;
			newList.LabelEdit = true;
			newList.LargeImageList = this.imageList2;
			newList.Name = "listView" + (listCount++).ToString();
			newList.Size = new System.Drawing.Size(112, 104);
			newList.SmallImageList = this.imageList3;
			//newList.Dock = DockStyle.Fill;

			newList.KeyDown += new KeyEventHandler(listView1_KeyDown);
			newList.AfterLabelEdit += new LabelEditEventHandler(listView1_AfterLabelEdit);

			return newList;
		}

		private ListView getActiveListView() {
			return getActiveDocument().Control as ListView;
		}

		private DocumentManager.Document getActiveDocument() {
			return documentManager1.TabStrips[0].SelectedDocument;
		}


		public void updateClasses() {
			string path = Application.StartupPath + Path.DirectorySeparatorChar + mediaPath;
			treeView1.Nodes.Clear();
			updateTreeRecursive(treeView1.Nodes, path, 0);
		}

		private void updateTreeRecursive(TreeNodeCollection nodeParent, string path, int count) {
			string[] dirs = Directory.GetDirectories(path);
			foreach(string dir in dirs) {
				string[] pieces = dir.Split("\\/".ToCharArray());
				if(!pieces[pieces.Length-1].StartsWith(".")) {
					TreeNode node = new TreeNode(pieces[pieces.Length-1],0,0);
					node.Tag = dir;
					nodeParent.Add(node);
					updateTreeRecursive(node.Nodes, dir, count+1);
				}
			}
		}

		private void buttonItem1_Activate(object sender, System.EventArgs e) {
			string def = String.Empty;
			while(true) {
				string cls = InputBox.Show("New Class", "Enter the new class name:", def);
				if(cls == null) break;
				if(cls.IndexOfAny("\\/?<>*:\"|".ToCharArray()) != -1) {
					MessageBox.Show(this, "Class name contains invalid characters ( \\/?<>*:\"| )",
						"Invalid Class Name", MessageBoxButtons.OK, MessageBoxIcon.Error);
					def = cls;
					continue;
				}
				if(Directory.Exists(mediaPath + cls)) {
					MessageBox.Show(this, "A class with this name already exists!",
						"Invalid Class Name", MessageBoxButtons.OK, MessageBoxIcon.Error);
					def = cls;
					continue;
				}
				Directory.CreateDirectory(mediaPath + cls);
				updateClasses();
				break;
			}
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
			this.components = new System.ComponentModel.Container();
			System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(ResourceManagerForm));
			this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
			this.menuBar1 = new TD.SandBar.MenuBar();
			this.menuBarItem1 = new TD.SandBar.MenuBarItem();
			this.menuButtonItem7 = new TD.SandBar.MenuButtonItem();
			this.menuButtonItem8 = new TD.SandBar.MenuButtonItem();
			this.menuButtonItem9 = new TD.SandBar.MenuButtonItem();
			this.menuButtonItem1 = new TD.SandBar.MenuButtonItem();
			this.menuButtonItem2 = new TD.SandBar.MenuButtonItem();
			this.imageList2 = new System.Windows.Forms.ImageList(this.components);
			this.imageList3 = new System.Windows.Forms.ImageList(this.components);
			this.imageList1 = new System.Windows.Forms.ImageList(this.components);
			this.toolBar1 = new TD.SandBar.ToolBar();
			this.buttonItem1 = new TD.SandBar.ButtonItem();
			this.buttonItem2 = new TD.SandBar.ButtonItem();
			this.dropDownMenuItem1 = new TD.SandBar.DropDownMenuItem();
			this.menuButtonItem3 = new TD.SandBar.MenuButtonItem();
			this.menuButtonItem4 = new TD.SandBar.MenuButtonItem();
			this.menuButtonItem5 = new TD.SandBar.MenuButtonItem();
			this.menuButtonItem6 = new TD.SandBar.MenuButtonItem();
			this.contextMenu1 = new System.Windows.Forms.ContextMenu();
			this.menuItem1 = new System.Windows.Forms.MenuItem();
			this.menuItem2 = new System.Windows.Forms.MenuItem();
			this.menuItem3 = new System.Windows.Forms.MenuItem();
			this.menuItem4 = new System.Windows.Forms.MenuItem();
			this.treeView1 = new System.Windows.Forms.TreeView();
			this.splitter1 = new System.Windows.Forms.Splitter();
			this.documentManager1 = new DocumentManager.DocumentManager();
			this.panel1 = new System.Windows.Forms.Panel();
			this.menuBarItem2 = new TD.SandBar.MenuBarItem();
			this.SuspendLayout();
			// 
			// openFileDialog1
			// 
			this.openFileDialog1.DefaultExt = "material";
			this.openFileDialog1.Filter = "Material files (*.material)|*.material|All Files (*.*)|*.*";
			this.openFileDialog1.Multiselect = true;
			// 
			// menuBar1
			// 
			this.menuBar1.Buttons.AddRange(new TD.SandBar.ToolbarItemBase[] {
																				this.menuBarItem1,
																				this.menuBarItem2});
			this.menuBar1.Guid = new System.Guid("47f3e460-6a5a-4b34-bba1-dafaede1adac");
			this.menuBar1.IsOpen = true;
			this.menuBar1.Location = new System.Drawing.Point(0, 0);
			this.menuBar1.Name = "menuBar1";
			this.menuBar1.Size = new System.Drawing.Size(672, 24);
			this.menuBar1.TabIndex = 11;
			this.menuBar1.Text = "menuBar1";
			// 
			// menuBarItem1
			// 
			this.menuBarItem1.MenuItems.AddRange(new TD.SandBar.MenuButtonItem[] {
																					 this.menuButtonItem2});
			this.menuBarItem1.Text = "&File";
			// 
			// menuButtonItem7
			// 
			this.menuButtonItem7.MenuItems.AddRange(new TD.SandBar.MenuButtonItem[] {
																						this.menuButtonItem8,
																						this.menuButtonItem9});
			this.menuButtonItem7.Shortcut = System.Windows.Forms.Shortcut.F2;
			this.menuButtonItem7.Text = "&New";
			// 
			// menuButtonItem8
			// 
			this.menuButtonItem8.Text = "Resource &Class...";
			// 
			// menuButtonItem9
			// 
			this.menuButtonItem9.Text = "Material &Pack...";
			// 
			// menuButtonItem1
			// 
			this.menuButtonItem1.Shortcut = System.Windows.Forms.Shortcut.F4;
			this.menuButtonItem1.Text = "&Import Script...";
			// 
			// menuButtonItem2
			// 
			this.menuButtonItem2.BeginGroup = true;
			this.menuButtonItem2.Shortcut = System.Windows.Forms.Shortcut.CtrlW;
			this.menuButtonItem2.Text = "&Close";
			// 
			// imageList2
			// 
			this.imageList2.ColorDepth = System.Windows.Forms.ColorDepth.Depth24Bit;
			this.imageList2.ImageSize = new System.Drawing.Size(48, 48);
			//this.imageList2.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageList2.ImageStream")));
			this.imageList2.TransparentColor = System.Drawing.Color.Transparent;
			// 
			// imageList3
			// 
			this.imageList3.ColorDepth = System.Windows.Forms.ColorDepth.Depth24Bit;
			this.imageList3.ImageSize = new System.Drawing.Size(16, 16);
			//this.imageList3.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageList3.ImageStream")));
			this.imageList3.TransparentColor = System.Drawing.Color.Transparent;
			// 
			// imageList1
			// 
			this.imageList1.ImageSize = new System.Drawing.Size(16, 16);
			//this.imageList1.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageList1.ImageStream")));
			this.imageList1.TransparentColor = System.Drawing.Color.Transparent;
			// 
			// toolBar1
			// 
			this.toolBar1.Buttons.AddRange(new TD.SandBar.ToolbarItemBase[] {
																				this.buttonItem1,
																				this.buttonItem2,
																				this.dropDownMenuItem1});
			this.toolBar1.Guid = new System.Guid("218c7539-6071-4276-8196-7b6dcbf1b40a");
			this.toolBar1.IsOpen = true;
			this.toolBar1.Location = new System.Drawing.Point(0, 24);
			this.toolBar1.Name = "toolBar1";
			this.toolBar1.Size = new System.Drawing.Size(672, 30);
			this.toolBar1.TabIndex = 13;
			this.toolBar1.Text = "toolBar1";
			// 
			// buttonItem1
			// 
			this.buttonItem1.Image = ((System.Drawing.Image)(resources.GetObject("buttonItem1.Image")));
			this.buttonItem1.ToolTipText = "Create a new resource class";
			this.buttonItem1.Activate += new System.EventHandler(this.buttonItem1_Activate);
			// 
			// buttonItem2
			// 
			this.buttonItem2.Image = ((System.Drawing.Image)(resources.GetObject("buttonItem2.Image")));
			this.buttonItem2.ToolTipText = "Create a new material pack";
			this.buttonItem2.Activate += new System.EventHandler(this.buttonItem2_Activate);
			// 
			// dropDownMenuItem1
			// 
			this.dropDownMenuItem1.BeginGroup = true;
			this.dropDownMenuItem1.Image = ((System.Drawing.Image)(resources.GetObject("dropDownMenuItem1.Image")));
			this.dropDownMenuItem1.MenuItems.AddRange(new TD.SandBar.MenuButtonItem[] {
																						  this.menuButtonItem3,
																						  this.menuButtonItem4,
																						  this.menuButtonItem5,
																						  this.menuButtonItem6});
			this.dropDownMenuItem1.Text = "";
			// 
			// menuButtonItem3
			// 
			this.menuButtonItem3.Text = "&Large Icons";
			// 
			// menuButtonItem4
			// 
			this.menuButtonItem4.Text = "&Small Icons";
			// 
			// menuButtonItem5
			// 
			this.menuButtonItem5.Text = "Lis&t";
			// 
			// menuButtonItem6
			// 
			this.menuButtonItem6.Text = "&Detail";
			// 
			// contextMenu1
			// 
			this.contextMenu1.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
																						 this.menuItem1,
																						 this.menuItem2,
																						 this.menuItem3,
																						 this.menuItem4});
			// 
			// menuItem1
			// 
			this.menuItem1.Index = 0;
			this.menuItem1.Text = "Large Icons";
			// 
			// menuItem2
			// 
			this.menuItem2.Index = 1;
			this.menuItem2.Text = "Small Icons";
			// 
			// menuItem3
			// 
			this.menuItem3.Index = 2;
			this.menuItem3.Text = "List View";
			// 
			// menuItem4
			// 
			this.menuItem4.Index = 3;
			this.menuItem4.Text = "Detail View";
			// 
			// treeView1
			// 
			this.treeView1.Dock = System.Windows.Forms.DockStyle.Left;
			this.treeView1.HideSelection = false;
			this.treeView1.ImageList = this.imageList1;
			this.treeView1.LabelEdit = true;
			this.treeView1.Location = new System.Drawing.Point(0, 54);
			this.treeView1.Name = "treeView1";
			this.treeView1.Nodes.AddRange(new System.Windows.Forms.TreeNode[] {
																				  new System.Windows.Forms.TreeNode("Node0", new System.Windows.Forms.TreeNode[] {
																																									 new System.Windows.Forms.TreeNode("Node1", 1, 1)})});
			this.treeView1.Size = new System.Drawing.Size(160, 373);
			this.treeView1.TabIndex = 15;
			this.treeView1.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.treeView1_AfterSelect);
			this.treeView1.AfterLabelEdit += new System.Windows.Forms.NodeLabelEditEventHandler(this.treeView1_AfterLabelEdit);
			// 
			// splitter1
			// 
			this.splitter1.Location = new System.Drawing.Point(160, 54);
			this.splitter1.Name = "splitter1";
			this.splitter1.Size = new System.Drawing.Size(8, 373);
			this.splitter1.TabIndex = 16;
			this.splitter1.TabStop = false;
			// 
			// documentManager1
			// 
			this.documentManager1.AllowDrop = true;
			this.documentManager1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.documentManager1.Location = new System.Drawing.Point(168, 54);
			this.documentManager1.Name = "documentManager1";
			this.documentManager1.Size = new System.Drawing.Size(504, 373);
			this.documentManager1.TabIndex = 20;
			this.documentManager1.TabStripBackColor = System.Drawing.SystemColors.Control;
			this.documentManager1.UseCustomTabStripBackColor = true;
			this.documentManager1.FocusedDocumentChanged += new System.EventHandler(this.documentManager1_FocusedDocumentChanged);
			// 
			// panel1
			// 
			this.panel1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.panel1.Location = new System.Drawing.Point(656, 56);
			this.panel1.Name = "panel1";
			this.panel1.Size = new System.Drawing.Size(16, 16);
			this.panel1.TabIndex = 21;
			// 
			// menuBarItem2
			// 
			this.menuBarItem2.MenuItems.AddRange(new TD.SandBar.MenuButtonItem[] {
																					 this.menuButtonItem7,
																					 this.menuButtonItem1});
			this.menuBarItem2.Text = "[name]";
			// 
			// ResourceManagerForm
			// 
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.ClientSize = new System.Drawing.Size(672, 427);
			this.Controls.Add(this.panel1);
			this.Controls.Add(this.documentManager1);
			this.Controls.Add(this.splitter1);
			this.Controls.Add(this.treeView1);
			this.Controls.Add(this.toolBar1);
			this.Controls.Add(this.menuBar1);
			this.Name = "ResourceManagerForm";
			this.ShowInTaskbar = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "Resource Browser";
			this.Load += new System.EventHandler(this.ImportMaterial_Load);
			this.ResumeLayout(false);

		}
		#endregion

		private void button1_Click(object sender, System.EventArgs e) {
			/*DialogResult dr = openFileDialog1.ShowDialog();
			if(dr == DialogResult.OK) {
				listView3.Items.AddRange(openFileDialog1.FileNames);
			}*/
		}

		public void ImportMaterialFile(string cls, string pack, string s) {
//			MaterialEntryManager.Instance.ParseScript(s, cls, pack);
		}

		private string readBlock(StreamReader script) {
			string line = script.ReadLine();
			bool hitFirstBrace = false;
			string buffer = String.Empty;
			int braceCount = 0;
			while(line != null) {
				buffer += line + "\n";
				if(line.StartsWith("{")) {
					braceCount++;
					hitFirstBrace = true;
				} else if(line.StartsWith("}")) {
					braceCount--;
				}
				if(hitFirstBrace && braceCount == 0)
					break;
				line = script.ReadLine();
			}
			return buffer;
		}

		private void buttonItem2_Activate(object sender, System.EventArgs e) {
			string def = String.Empty;
			while(true) {
				string cls = InputBox.Show("New Material Pack", "Enter the new pack name:", def);
				if(cls == null) break;
				if(cls.IndexOfAny(" \\/?<>*:\"|".ToCharArray()) != -1) {
					MessageBox.Show(this, "Pack name contains invalid characters ( \\/?<>*:\"|<space> )",
						"Invalid Pack Name", MessageBoxButtons.OK, MessageBoxIcon.Error);
					def = cls;
					continue;
				}
				string file = mediaPath + getActiveListView().SelectedItems[0].ToString() + Path.DirectorySeparatorChar + cls + ".material";
				if(File.Exists(file)) {
					MessageBox.Show(this, "A pack with this name already exists!",
						"Invalid Class Name", MessageBoxButtons.OK, MessageBoxIcon.Error);
					def = cls;
					continue;
				}
				File.Create(file);
				updateClasses();
				break;
			}
		}

		private void button3_Click(object sender, System.EventArgs e) {
			/*if(listView1.SelectedItem == null) {
				MessageBox.Show(this, "Please select a valid resource class.",
					"Invalid Resource Class", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}
			if(listView2.SelectedItem == null) {
				MessageBox.Show(this, "Please select a valid material pack.",
					"Invalid Resource Pack", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}
			foreach(string s in listView3.Items) {
				this.ImportMaterialFile(listView1.SelectedItem.ToString(),
					listView2.SelectedItem.ToString(), s);
			}*/
		}

		private void ImportMaterial_Load(object sender, System.EventArgs e) {
		
		}

		private void treeView1_AfterSelect(object sender, System.Windows.Forms.TreeViewEventArgs e) {
			TreeNode sNode = (sender as TreeView).SelectedNode;
			string path = sNode.Tag.ToString();
			updateListview(path);
		}

		private void updateListview(string path) {
			getActiveListView().Clear();

			string[] dirs = Directory.GetDirectories(path);
			foreach(string dir in dirs) {
				if(Path.GetFileName(dir).StartsWith(".")) continue;
				ListViewItem i = new ListViewItem(Path.GetFileName(dir), 1);
				i.SubItems.Add(Path.GetFileName(dir));
				i.Tag = dir;
				getActiveListView().Items.Add(i);
			}

			string[] files = Directory.GetFiles(path, getActiveDocument().Tag.ToString());
			foreach(string file in files) {
				string text = Path.GetFileName(file);
				ListViewItem i = new ListViewItem(text, 0);
				i.SubItems.Add(text);
				i.Tag = file;
				getActiveListView().Items.Add(i);
			}
		}

		private void listView1_AfterLabelEdit(object sender, System.Windows.Forms.LabelEditEventArgs e) {
			if(e.Label.Length == 0) {
				e.CancelEdit = true;
				return;
			}
			string path = (sender as ListView).Items[e.Item].Tag.ToString();
			string newPath = string.Empty;
			if(System.IO.File.Exists(path)) {
				string[] bits = path.Split("\\/".ToCharArray());
				for(int i=0;i<bits.Length-1;i++)
					newPath += bits[i] + Path.DirectorySeparatorChar;
				newPath += e.Label + ".material";
				File.Move(path, newPath);
			} else if(System.IO.Directory.Exists(path)) {
				string[] bits = path.Split("\\/".ToCharArray());
				for(int i=0;i<bits.Length-1;i++)
					newPath += bits[i] + Path.DirectorySeparatorChar;
				newPath += e.Label;
				Directory.Move(path, newPath);
			}
		}

		private void listView1_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e) {
			if((sender as ListView).SelectedItems.Count == 1) {
				if(e.KeyCode == Keys.F2) {
					SendMessage((sender as ListView).Handle.ToInt32(), LVM_EDIT, 0, 0);
				}
			}
		}

		private void treeView1_AfterLabelEdit(object sender, System.Windows.Forms.NodeLabelEditEventArgs e) {
			if(e.Label.Length == 0) {
				e.CancelEdit = true;
				return;
			}
			string[] bits = e.Node.Tag.ToString().Split("\\/".ToCharArray());
			string newPath = string.Empty;
			for(int i=0;i<bits.Length-1; i++) {
				newPath += bits[i] + Path.DirectorySeparatorChar;
			}
			newPath += e.Label;
			Directory.Move(e.Node.Tag.ToString(), newPath);
			this.updateClasses();
		}

		private void treeView1_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e) {
			if((sender as TreeView).SelectedNode != null) {
				if(e.KeyCode == Keys.F2) {
					SendMessage((sender as TreeView).Handle.ToInt32(), LVM_EDIT, 0, 0);
				}
			}
		}

		private void documentManager1_FocusedDocumentChanged(object sender, EventArgs e) {
			if(treeView1.SelectedNode != null)
				updateListview(treeView1.SelectedNode.Tag.ToString());
		}
	}
}
