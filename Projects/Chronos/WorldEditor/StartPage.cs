using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Windows.Forms;
using System.Xml;
using System.Xml.XPath;
using Chronos.Core;

namespace Chronos
{
	/// <summary>
	/// Summary description for StartPage.
	/// </summary>
	public class StartPage : System.Windows.Forms.UserControl
	{
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.HelpProvider helpProvider1;
		private System.Windows.Forms.ImageList imageList1;
		private System.Windows.Forms.ImageList imageList2;
		private System.Windows.Forms.Panel panel1;
		private System.Windows.Forms.ListView listView1;
		private System.Windows.Forms.ColumnHeader columnHeader1;
		private System.Windows.Forms.ColumnHeader columnHeader2;
		private TD.SandBar.ToolBar toolBar1;
		private TD.SandBar.ButtonItem buttonItem1;
		private TD.SandBar.DropDownMenuItem dropDownMenuItem1;
		private TD.SandBar.MenuButtonItem menuButtonItem1;
		private TD.SandBar.MenuButtonItem menuButtonItem2;
		private TD.SandBar.MenuButtonItem menuButtonItem3;
		private TD.SandBar.MenuButtonItem menuButtonItem4;
		private System.ComponentModel.IContainer components;
		private TD.SandBar.MenuButtonItem checkedItem;

		public StartPage()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

			// TODO: Add any initialization after the InitializeComponent call

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

		#region Component Designer generated code
		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(StartPage));
			System.Windows.Forms.ListViewItem listViewItem1 = new System.Windows.Forms.ListViewItem("Daikatana 2: Medieval Boogaloo", 0);
			System.Windows.Forms.ListViewItem listViewItem2 = new System.Windows.Forms.ListViewItem("Halo 3", 0);
			System.Windows.Forms.ListViewItem listViewItem3 = new System.Windows.Forms.ListViewItem("Leisure Suit Larry: Bump Mapping", 0);
			System.Windows.Forms.ListViewItem listViewItem4 = new System.Windows.Forms.ListViewItem("Test Project 1", 0);
			System.Windows.Forms.ListViewItem listViewItem5 = new System.Windows.Forms.ListViewItem("The Most Awesome 1-Man MMORPG Ever", 0);
			this.label1 = new System.Windows.Forms.Label();
			this.imageList1 = new System.Windows.Forms.ImageList(this.components);
			this.imageList2 = new System.Windows.Forms.ImageList(this.components);
			this.helpProvider1 = new System.Windows.Forms.HelpProvider();
			this.panel1 = new System.Windows.Forms.Panel();
			this.listView1 = new System.Windows.Forms.ListView();
			this.columnHeader1 = new System.Windows.Forms.ColumnHeader();
			this.columnHeader2 = new System.Windows.Forms.ColumnHeader();
			this.toolBar1 = new TD.SandBar.ToolBar();
			this.buttonItem1 = new TD.SandBar.ButtonItem();
			this.dropDownMenuItem1 = new TD.SandBar.DropDownMenuItem();
			this.menuButtonItem1 = new TD.SandBar.MenuButtonItem();
			this.menuButtonItem2 = new TD.SandBar.MenuButtonItem();
			this.menuButtonItem3 = new TD.SandBar.MenuButtonItem();
			this.menuButtonItem4 = new TD.SandBar.MenuButtonItem();
			this.panel1.SuspendLayout();
			this.SuspendLayout();
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.BackColor = System.Drawing.Color.Transparent;
			this.label1.Font = new System.Drawing.Font("Arial Black", 20.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.label1.Location = new System.Drawing.Point(32, 0);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(209, 42);
			this.label1.TabIndex = 1;
			this.label1.Text = "P r o j e c t s";
			// 
			// imageList1
			// 
			this.imageList1.ColorDepth = System.Windows.Forms.ColorDepth.Depth16Bit;
			this.imageList1.ImageSize = new System.Drawing.Size(32, 32);
			//this.imageList1.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageList1.ImageStream")));
			this.imageList1.TransparentColor = System.Drawing.Color.Transparent;
			// 
			// imageList2
			// 
			this.imageList2.ColorDepth = System.Windows.Forms.ColorDepth.Depth24Bit;
			this.imageList2.ImageSize = new System.Drawing.Size(16, 16);
			//this.imageList2.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageList2.ImageStream")));
			this.imageList2.TransparentColor = System.Drawing.Color.Transparent;
			// 
			// panel1
			// 
			this.panel1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
				| System.Windows.Forms.AnchorStyles.Left) 
				| System.Windows.Forms.AnchorStyles.Right)));
			this.panel1.Controls.Add(this.listView1);
			this.panel1.Controls.Add(this.toolBar1);
			this.panel1.Location = new System.Drawing.Point(8, 40);
			this.panel1.Name = "panel1";
			this.panel1.Size = new System.Drawing.Size(520, 344);
			this.panel1.TabIndex = 9;
			// 
			// listView1
			// 
			this.listView1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.listView1.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
																						this.columnHeader1,
																						this.columnHeader2});
			this.listView1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.listView1.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
			this.listView1.Items.AddRange(new System.Windows.Forms.ListViewItem[] {
																					  listViewItem1,
																					  listViewItem2,
																					  listViewItem3,
																					  listViewItem4,
																					  listViewItem5});
			this.listView1.LargeImageList = this.imageList1;
			this.listView1.Location = new System.Drawing.Point(0, 26);
			this.listView1.MultiSelect = false;
			this.listView1.Name = "listView1";
			this.listView1.Size = new System.Drawing.Size(520, 318);
			this.listView1.SmallImageList = this.imageList2;
			this.listView1.TabIndex = 10;
			this.listView1.DoubleClick += new System.EventHandler(this.listView1_DoubleClick);
			// 
			// columnHeader1
			// 
			this.columnHeader1.Text = "Name";
			this.columnHeader1.Width = 309;
			// 
			// columnHeader2
			// 
			this.columnHeader2.Text = "Last Modified";
			this.columnHeader2.Width = 205;
			// 
			// toolBar1
			// 
			this.toolBar1.Buttons.AddRange(new TD.SandBar.ToolbarItemBase[] {
																				this.buttonItem1,
																				this.dropDownMenuItem1});
			this.toolBar1.Guid = new System.Guid("5ee9812b-e681-4d7f-add0-2078f3634ce2");
			this.toolBar1.IsOpen = true;
			this.toolBar1.Location = new System.Drawing.Point(0, 0);
			this.toolBar1.Name = "toolBar1";
			this.toolBar1.Size = new System.Drawing.Size(520, 26);
			this.toolBar1.TabIndex = 11;
			this.toolBar1.Text = "toolBar1";
			// 
			// buttonItem1
			// 
			this.buttonItem1.Image = ((System.Drawing.Image)(resources.GetObject("buttonItem1.Image")));
			this.buttonItem1.ToolTipText = "New Project";
			this.buttonItem1.Activate += new System.EventHandler(this.buttonItem1_Activate);
			// 
			// dropDownMenuItem1
			// 
			this.dropDownMenuItem1.Image = ((System.Drawing.Image)(resources.GetObject("dropDownMenuItem1.Image")));
			this.dropDownMenuItem1.MenuItems.AddRange(new TD.SandBar.MenuButtonItem[] {
																						  this.menuButtonItem1,
																						  this.menuButtonItem2,
																						  this.menuButtonItem3,
																						  this.menuButtonItem4});
			this.dropDownMenuItem1.Text = "";
			this.dropDownMenuItem1.ToolTipText = "Change Project View";
			// 
			// menuButtonItem1
			// 
			this.menuButtonItem1.BeginGroup = true;
			this.menuButtonItem1.Text = "Large Icons";
			this.menuButtonItem1.Activate += new System.EventHandler(this.menuButtonItem1_Activate);
			// 
			// menuButtonItem2
			// 
			this.menuButtonItem2.Text = "Small Icons";
			this.menuButtonItem2.Activate += new System.EventHandler(this.menuButtonItem2_Activate);
			// 
			// menuButtonItem3
			// 
			this.menuButtonItem3.Text = "List View";
			this.menuButtonItem3.Activate += new System.EventHandler(this.menuButtonItem3_Activate);
			// 
			// menuButtonItem4
			// 
			this.menuButtonItem4.Text = "Details";
			this.menuButtonItem4.Activate += new System.EventHandler(this.menuButtonItem4_Activate);
			// 
			// StartPage
			// 
			this.BackColor = System.Drawing.Color.White;
			this.Controls.Add(this.panel1);
			this.Controls.Add(this.label1);
			this.ForeColor = System.Drawing.Color.Black;
			this.Name = "StartPage";
			this.Size = new System.Drawing.Size(536, 392);
			this.Load += new System.EventHandler(this.StartPage_Load);
			this.panel1.ResumeLayout(false);
			this.ResumeLayout(false);

		}
		#endregion

		private void menuButtonItem1_Activate(object sender, System.EventArgs e) {
			listView1.View = View.LargeIcon;
			if(checkedItem != null)
				checkedItem.Checked = false;
			checkedItem = (sender as TD.SandBar.MenuButtonItem);
			checkedItem.Checked = true;
		}

		private void menuButtonItem2_Activate(object sender, System.EventArgs e) {
			listView1.View = View.SmallIcon;
			if(checkedItem != null)
				checkedItem.Checked = false;
			checkedItem = (sender as TD.SandBar.MenuButtonItem);
			checkedItem.Checked = true;
		}

		private void menuButtonItem3_Activate(object sender, System.EventArgs e) {
			listView1.View = View.List;
			if(checkedItem != null)
				checkedItem.Checked = false;
			checkedItem = (sender as TD.SandBar.MenuButtonItem);
			checkedItem.Checked = true;
		}

		private void menuButtonItem4_Activate(object sender, System.EventArgs e) {
			listView1.View = View.Details;
			if(checkedItem != null)
				checkedItem.Checked = false;
			checkedItem = (sender as TD.SandBar.MenuButtonItem);
			checkedItem.Checked = true;
		}

		private void InitProjectList() {
			this.listView1.Items.Clear();
			XPathNodeIterator iter = new XPathDocument("editorSettings.xml").CreateNavigator().Select("/document/recent/scene");
			while (iter.MoveNext()) {
				string path = iter.Current.GetAttribute("path", string.Empty);
				string name = iter.Current.GetAttribute("name", string.Empty);
				string date = iter.Current.GetAttribute("lastmodified", string.Empty);
				string[] items = new string[] {name, date, path};
				ListViewItem v = new ListViewItem(items, 0);
				listView1.Items.Add(v);
			}
		}

		private void StartPage_Load(object sender, System.EventArgs e) {
			InitProjectList();
			if(listView1.View == View.LargeIcon)
				menuButtonItem1_Activate(menuButtonItem1, EventArgs.Empty);
			else if(listView1.View == View.SmallIcon)
				menuButtonItem2_Activate(menuButtonItem2, EventArgs.Empty);
			else if(listView1.View == View.List)
				menuButtonItem3_Activate(menuButtonItem3, EventArgs.Empty);
			else if(listView1.View == View.Details)
				menuButtonItem4_Activate(menuButtonItem4, EventArgs.Empty);
		}

		private void buttonItem1_Activate(object sender, System.EventArgs e) {
			EditorSceneManager.Instance.NewScene();
		}

		private void listView1_DoubleClick(object sender, System.EventArgs e) {
			EditorSceneManager.Instance.LoadScene((sender as ListView).SelectedItems[0].SubItems[2].ToString());
		}
	}
}

