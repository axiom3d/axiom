using System;
using System.Data;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using Axiom.Utility;

namespace Demos {
    /// <summary>
    /// Summary description for DemoBrowser.
    /// </summary>
    public class DemoBrowser : Form {
        private IContainer components = null;
        private Label lblShowTypes;
        private Panel pnlDemoTypes;
        private CheckBox chkDemos;
        private CheckBox chkTutorials;
        private CheckBox chkTools;
        private LinkLabel lnkWebsite;
        private DataView demoView;
        private DataTable demoTable = new DataTable("Demos");
        private ListView lstDemos;
        private ImageList demoImageList = new ImageList();
        private Label lblInfo;

        public DemoBrowser() {
            InitializeComponent();
        }

        protected override void Dispose(bool disposing) {
            if(disposing) {
                if(components != null) {
                    components.Dispose();
                }
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent() {
            System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(DemoBrowser));
            this.lstDemos = new System.Windows.Forms.ListView();
            this.lblInfo = new System.Windows.Forms.Label();
            this.chkTools = new System.Windows.Forms.CheckBox();
            this.chkTutorials = new System.Windows.Forms.CheckBox();
            this.chkDemos = new System.Windows.Forms.CheckBox();
            this.demoView = new System.Data.DataView();
            this.lblShowTypes = new System.Windows.Forms.Label();
            this.pnlDemoTypes = new System.Windows.Forms.Panel();
            this.lnkWebsite = new System.Windows.Forms.LinkLabel();
            ((System.ComponentModel.ISupportInitialize)(this.demoView)).BeginInit();
            this.pnlDemoTypes.SuspendLayout();
            this.SuspendLayout();
            // 
            // lstDemos
            // 
            this.lstDemos.Activation = System.Windows.Forms.ItemActivation.OneClick;
            this.lstDemos.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
                | System.Windows.Forms.AnchorStyles.Right)));
            this.lstDemos.BackColor = System.Drawing.Color.FromArgb(((System.Byte)(25)), ((System.Byte)(35)), ((System.Byte)(75)));
            this.lstDemos.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.lstDemos.Font = new System.Drawing.Font("Palatino Linotype", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
            this.lstDemos.ForeColor = System.Drawing.Color.White;
            this.lstDemos.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
            this.lstDemos.Location = new System.Drawing.Point(104, 8);
            this.lstDemos.MultiSelect = false;
            this.lstDemos.Name = "lstDemos";
            this.lstDemos.Size = new System.Drawing.Size(584, 392);
            this.lstDemos.TabIndex = 3;
            this.lstDemos.Click += new System.EventHandler(this.lstDemos_Click);
            this.lstDemos.MouseMove += new System.Windows.Forms.MouseEventHandler(this.lstDemos_MouseMove);
            // 
            // lblInfo
            // 
            this.lblInfo.BackColor = System.Drawing.Color.FromArgb(((System.Byte)(25)), ((System.Byte)(35)), ((System.Byte)(75)));
            this.lblInfo.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.lblInfo.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.lblInfo.Font = new System.Drawing.Font("Palatino Linotype", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
            this.lblInfo.ForeColor = System.Drawing.Color.White;
            this.lblInfo.Location = new System.Drawing.Point(0, 408);
            this.lblInfo.Name = "lblInfo";
            this.lblInfo.Size = new System.Drawing.Size(694, 40);
            this.lblInfo.TabIndex = 4;
            this.lblInfo.Text = "Select a demo to run.  The description of each demo will appear here as you hover" +
                " over them with the mouse.";
            this.lblInfo.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // chkTools
            // 
            this.chkTools.Cursor = System.Windows.Forms.Cursors.Hand;
            this.chkTools.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.chkTools.Location = new System.Drawing.Point(8, 56);
            this.chkTools.Name = "chkTools";
            this.chkTools.Size = new System.Drawing.Size(80, 24);
            this.chkTools.TabIndex = 2;
            this.chkTools.Text = "Tools";
            this.chkTools.CheckedChanged += new System.EventHandler(this.typeGroup_CheckedChanged);
            // 
            // chkTutorials
            // 
            this.chkTutorials.Checked = true;
            this.chkTutorials.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkTutorials.Cursor = System.Windows.Forms.Cursors.Hand;
            this.chkTutorials.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.chkTutorials.Location = new System.Drawing.Point(8, 32);
            this.chkTutorials.Name = "chkTutorials";
            this.chkTutorials.Size = new System.Drawing.Size(80, 24);
            this.chkTutorials.TabIndex = 1;
            this.chkTutorials.Text = "Tutorials";
            this.chkTutorials.CheckedChanged += new System.EventHandler(this.typeGroup_CheckedChanged);
            // 
            // chkDemos
            // 
            this.chkDemos.Checked = true;
            this.chkDemos.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkDemos.Cursor = System.Windows.Forms.Cursors.Hand;
            this.chkDemos.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.chkDemos.Location = new System.Drawing.Point(8, 8);
            this.chkDemos.Name = "chkDemos";
            this.chkDemos.Size = new System.Drawing.Size(64, 24);
            this.chkDemos.TabIndex = 0;
            this.chkDemos.Text = "Demos";
            this.chkDemos.CheckedChanged += new System.EventHandler(this.typeGroup_CheckedChanged);
            // 
            // lblShowTypes
            // 
            this.lblShowTypes.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.lblShowTypes.Location = new System.Drawing.Point(8, 8);
            this.lblShowTypes.Name = "lblShowTypes";
            this.lblShowTypes.Size = new System.Drawing.Size(80, 23);
            this.lblShowTypes.TabIndex = 5;
            this.lblShowTypes.Text = "Show Types:";
            // 
            // pnlDemoTypes
            // 
            this.pnlDemoTypes.Controls.Add(this.chkTools);
            this.pnlDemoTypes.Controls.Add(this.chkTutorials);
            this.pnlDemoTypes.Controls.Add(this.chkDemos);
            this.pnlDemoTypes.Location = new System.Drawing.Point(16, 24);
            this.pnlDemoTypes.Name = "pnlDemoTypes";
            this.pnlDemoTypes.Size = new System.Drawing.Size(80, 88);
            this.pnlDemoTypes.TabIndex = 6;
            // 
            // lnkWebsite
            // 
            this.lnkWebsite.ActiveLinkColor = System.Drawing.Color.White;
            this.lnkWebsite.LinkColor = System.Drawing.Color.White;
            this.lnkWebsite.Location = new System.Drawing.Point(8, 360);
            this.lnkWebsite.Name = "lnkWebsite";
            this.lnkWebsite.Size = new System.Drawing.Size(88, 23);
            this.lnkWebsite.TabIndex = 7;
            this.lnkWebsite.TabStop = true;
            this.lnkWebsite.Text = "Axiom Website";
            this.lnkWebsite.VisitedLinkColor = System.Drawing.Color.White;
            this.lnkWebsite.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.lnkWebsite_LinkClicked);
            // 
            // DemoBrowser
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 15);
            this.BackColor = System.Drawing.Color.FromArgb(((System.Byte)(25)), ((System.Byte)(35)), ((System.Byte)(75)));
            this.ClientSize = new System.Drawing.Size(694, 448);
            this.Controls.Add(this.lnkWebsite);
            this.Controls.Add(this.pnlDemoTypes);
            this.Controls.Add(this.lblInfo);
            this.Controls.Add(this.lstDemos);
            this.Controls.Add(this.lblShowTypes);
            this.Font = new System.Drawing.Font("Palatino Linotype", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
            this.ForeColor = System.Drawing.SystemColors.ControlLightLight;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "DemoBrowser";
            this.Text = "Axiom Engine - Demo Browser v1.0.0.0";
            this.Load += new System.EventHandler(this.DemoBrowser_Load);
            ((System.ComponentModel.ISupportInitialize)(this.demoView)).EndInit();
            this.pnlDemoTypes.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        private void lstDemos_Click(object sender, EventArgs e) {
            if(lstDemos.SelectedItems.Count > 0) {
                // get the first one, we only do single select for the demos
                ListViewItem item = lstDemos.SelectedItems[0];

                if(item.Tag != null) {
                    // find the class name of the demo
                    string demoClassName = item.Tag.ToString();

                    if(demoClassName.Length != 0) {
                        // get the type of the demo class
                        Type type = Type.GetType(demoClassName);

                        // create an instance of the demo class and start it up
                        using(TechDemo demo = (TechDemo) Activator.CreateInstance(type)) {
                            demo.Start();
                        }
                    }
                }
            }
        }

        private void lstDemos_MouseMove(object sender, MouseEventArgs e) {
            ListViewItem item = lstDemos.GetItemAt(e.X, e.Y);

            if(item != null) {
                if(item.SubItems.Count > 1) {
                    lblInfo.Text = item.SubItems[1].Text;
                }
            }
        }

        private void LoadDemoItems(DataView demoView) {
            // make sure the listview is empty
            lstDemos.Items.Clear();

            foreach(DataRowView demo in demoView) {

                ListViewItem item = new ListViewItem();
                item.Tag = (string)demo["ClassName"];
                item.Text = (string)demo["Name"];
                item.SubItems.Add((string)demo["Description"]);
                item.ImageIndex = (int)demo["ImageIndex"];

                lstDemos.Items.Add(item);
            }
        }

        private void DemoBrowser_Load(object sender, EventArgs e) {
            demoTable.Columns.Add("ImageIndex", typeof(int));
            demoTable.Columns.Add("Name");
            demoTable.Columns.Add("ClassName");
            demoTable.Columns.Add("ImageFile");
            demoTable.Columns.Add("Description");
            demoTable.Columns.Add("Type");

            demoTable.Rows.Add(new object[] {0, "Camera Track", "Demos.CameraTrack", "CameraTrack.jpg", "Watch the camera follow a defined spline path while maintaining focus on an object in the scene.", "Demos"});
            demoTable.Rows.Add(new object[] {1, "TextureFX", "Demos.TextureFX", "TextureFX.jpg", "Demonstrates the usage of various texture effects, including scrolling and rotating.", "Demos"});
            demoTable.Rows.Add(new object[] {2, "Transparency", "Demos.Transparency", "Transparency.jpg", "A high poly scene with transparent entities showing how scene blending works.", "Demos"});
            demoTable.Rows.Add(new object[] {3, "Environment Mapping", "Demos.EnvMapping", "EnvMapping.jpg", "Small example of an environment mapped entity.", "Demos"});
            demoTable.Rows.Add(new object[] {4, "Sky Plane", "Demos.SkyPlane", "SkyPlane.jpg", "Example of creating a scene with a skyplane.", "Demos"});
            demoTable.Rows.Add(new object[] {5, "Sky Box", "Demos.SkyBox", "SkyBox.jpg", "Example of creating a scene with a skybox.", "Demos"});
            demoTable.Rows.Add(new object[] {6, "Lights", "Demos.Lights", "Lights.jpg", "Example of creating a scene with lights and billboards.", "Demos"});
            demoTable.Rows.Add(new object[] {7, "ParticleFX", "Demos.ParticleFX", "ParticleFX.jpg", "Demonstrates the various type of particle systems that the engine supports.", "Demos"});
            demoTable.Rows.Add(new object[] {8, "Skeletal Animation", "Demos.SkeletalAnimation", "SkeletalAnimation.jpg", "Demonstrates skeletal animation techniques.", "Demos"});
            demoTable.Rows.Add(new object[] {9, "Physics", "Demos.Physics", "Physics.jpg", "Demonstrates collidable objects with real time physics.", "Demos"});
            demoTable.Rows.Add(new object[] {10, "Tutorial 1", "Demos.Tutorial1", "Tutorial1.jpg", "Demonstrates the typical spinning triangle demo using the engine.", "Tutorials"});

            demoView = new DataView(demoTable);

            // load the images on form load once
            foreach(DataRowView demo in demoView) {
                // add the image
                demoImageList.Images.Add(Bitmap.FromFile("BrowserImages/" + (string)demo["ImageFile"]));
            }

            // set the properties of the image list
            demoImageList.ColorDepth = ColorDepth.Depth16Bit;
            demoImageList.ImageSize = new Size(133, 100);

            // set the listview to use the new imagelist for images
            lstDemos.SmallImageList = demoImageList;
            lstDemos.LargeImageList = demoImageList;
            lstDemos.StateImageList = demoImageList;

            // load the items
            LoadDemoItems(demoView);
        }

        private void typeGroup_CheckedChanged(object sender, EventArgs e) {
            string filter = "";
            bool firstOne = true;

            // loop through each type and filter the shown items
            foreach(CheckBox type in pnlDemoTypes.Controls) {
                if(type.Checked) {
                    if(firstOne) {
                        firstOne = false;
                    }
                    else {
                        filter += " OR ";
                    }
                    filter += string.Format("Type = '{0}'", type.Text);
                }
            }

            // set the filter on the dataview
            if(filter.Length == 0) {
                demoView.RowFilter = "Type = 'None'";
            }
            else {
                demoView.RowFilter = filter;
            }

            // reload the demo items
            LoadDemoItems(demoView);
        }

        private void lnkWebsite_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e) {
            lnkWebsite.LinkVisited = true;
            System.Diagnostics.Process.Start("http://axiomengine.sourceforge.net/");
        }
    }
}
