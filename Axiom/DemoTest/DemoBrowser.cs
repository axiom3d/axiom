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
    public class DemoBrowser : System.Windows.Forms.Form {
        private System.ComponentModel.IContainer components;
        private System.Windows.Forms.Label lblInfo;
        private System.Windows.Forms.CheckBox checkBox1;
        private System.Windows.Forms.CheckBox checkBox2;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.ListView demoListView;
        private System.Data.DataView demoView;
        private System.Windows.Forms.GroupBox typeGroup;
        private System.Windows.Forms.CheckBox checkBox3;

        private DataTable demoTable = new DataTable("Demos");
        private ImageList demoImageList = new ImageList();

        public DemoBrowser() {
            //
            // Required for Windows Form Designer support
            //
            InitializeComponent();

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
            System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(DemoBrowser));
            this.demoListView = new System.Windows.Forms.ListView();
            this.lblInfo = new System.Windows.Forms.Label();
            this.typeGroup = new System.Windows.Forms.GroupBox();
            this.checkBox2 = new System.Windows.Forms.CheckBox();
            this.checkBox1 = new System.Windows.Forms.CheckBox();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.demoView = new System.Data.DataView();
            this.checkBox3 = new System.Windows.Forms.CheckBox();
            this.typeGroup.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.demoView)).BeginInit();
            this.SuspendLayout();
            // 
            // demoListView
            // 
            this.demoListView.Activation = System.Windows.Forms.ItemActivation.OneClick;
            this.demoListView.BackColor = System.Drawing.Color.Black;
            this.demoListView.Font = new System.Drawing.Font("Palatino Linotype", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
            this.demoListView.ForeColor = System.Drawing.Color.White;
            this.demoListView.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
            this.demoListView.Location = new System.Drawing.Point(128, 56);
            this.demoListView.MultiSelect = false;
            this.demoListView.Name = "demoListView";
            this.demoListView.Size = new System.Drawing.Size(600, 288);
            this.demoListView.TabIndex = 0;
            this.demoListView.Click += new System.EventHandler(this.demoListView_Click);
            this.demoListView.MouseMove += new System.Windows.Forms.MouseEventHandler(this.demoListView_MouseMove);
            // 
            // lblInfo
            // 
            this.lblInfo.BackColor = System.Drawing.Color.White;
            this.lblInfo.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblInfo.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.lblInfo.ForeColor = System.Drawing.Color.Navy;
            this.lblInfo.Location = new System.Drawing.Point(128, 0);
            this.lblInfo.Name = "lblInfo";
            this.lblInfo.Size = new System.Drawing.Size(600, 48);
            this.lblInfo.TabIndex = 2;
            this.lblInfo.Text = "Select a demo to run.  The description of each demo will appear here as you hover" +
                " over them with the mouse.";
            // 
            // typeGroup
            // 
            this.typeGroup.Controls.Add(this.checkBox3);
            this.typeGroup.Controls.Add(this.checkBox2);
            this.typeGroup.Controls.Add(this.checkBox1);
            this.typeGroup.Location = new System.Drawing.Point(8, 56);
            this.typeGroup.Name = "typeGroup";
            this.typeGroup.Size = new System.Drawing.Size(112, 112);
            this.typeGroup.TabIndex = 3;
            this.typeGroup.TabStop = false;
            this.typeGroup.Text = "Show";
            // 
            // checkBox2
            // 
            this.checkBox2.Checked = true;
            this.checkBox2.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBox2.Location = new System.Drawing.Point(16, 48);
            this.checkBox2.Name = "checkBox2";
            this.checkBox2.Size = new System.Drawing.Size(80, 24);
            this.checkBox2.TabIndex = 1;
            this.checkBox2.Text = "Tutorials";
            this.checkBox2.CheckedChanged += new System.EventHandler(this.typeGroup_CheckedChanged);
            // 
            // checkBox1
            // 
            this.checkBox1.Checked = true;
            this.checkBox1.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBox1.Location = new System.Drawing.Point(16, 24);
            this.checkBox1.Name = "checkBox1";
            this.checkBox1.Size = new System.Drawing.Size(64, 24);
            this.checkBox1.TabIndex = 0;
            this.checkBox1.Text = "Demos";
            this.checkBox1.CheckedChanged += new System.EventHandler(this.typeGroup_CheckedChanged);
            // 
            // pictureBox1
            // 
            this.pictureBox1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pictureBox1.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox1.Image")));
            this.pictureBox1.Location = new System.Drawing.Point(8, 0);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(112, 48);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pictureBox1.TabIndex = 4;
            this.pictureBox1.TabStop = false;
            // 
            // checkBox3
            // 
            this.checkBox3.Location = new System.Drawing.Point(16, 72);
            this.checkBox3.Name = "checkBox3";
            this.checkBox3.Size = new System.Drawing.Size(80, 24);
            this.checkBox3.TabIndex = 2;
            this.checkBox3.Text = "Tools";
            this.checkBox3.CheckedChanged += new System.EventHandler(this.typeGroup_CheckedChanged);
            // 
            // DemoBrowser
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 15);
            this.BackColor = System.Drawing.Color.Black;
            this.ClientSize = new System.Drawing.Size(730, 346);
            this.Controls.Add(this.pictureBox1);
            this.Controls.Add(this.typeGroup);
            this.Controls.Add(this.lblInfo);
            this.Controls.Add(this.demoListView);
            this.Font = new System.Drawing.Font("Palatino Linotype", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
            this.ForeColor = System.Drawing.SystemColors.ControlLightLight;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Name = "DemoBrowser";
            this.Text = "Axiom Engine - Demo Browser v1.0.0.0";
            this.Load += new System.EventHandler(this.DemoBrowser_Load);
            this.typeGroup.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.demoView)).EndInit();
            this.ResumeLayout(false);

        }
        #endregion

        private void demoListView_Click(object sender, System.EventArgs e) {
            if(demoListView.SelectedItems.Count > 0) {
                // get the first one, we only do single select for the demos
                ListViewItem item = demoListView.SelectedItems[0];

                if(item.Tag != null) {
                    // find the class name of the demo
                    string demoClassName = item.Tag.ToString();

                    if(demoClassName.Length != 0) {
                        // get the type of the demo class
                        Type type = Type.GetType(demoClassName);

                        // create an instance of the demo class and start it up
                        using(TechDemo demo = (TechDemo)Activator.CreateInstance(type)) {
                            demo.Start();
                        }
                    }
                }
            }
        }

        private void demoListView_MouseMove(object sender, System.Windows.Forms.MouseEventArgs e) {
            ListViewItem item = demoListView.GetItemAt(e.X, e.Y);

            if(item != null) {			
                if(item.SubItems.Count > 1) {
                    lblInfo.Text = item.SubItems[1].Text;
                }
            }
        }

        private void LoadDemoItems(DataView demoView) {

            // make sure the listview is empty
            demoListView.Items.Clear();

            foreach(DataRowView demo in demoView) {
                // add the image
                //demoImageList.Images.Add(Bitmap.FromFile("../../Demos/images/" + (string)demo["ImageFile"]));

                ListViewItem item = new ListViewItem();
                item.Tag = (string)demo["ClassName"];
                item.Text = (string)demo["Name"];
                item.SubItems.Add((string)demo["Description"]);
                item.ImageIndex = (int)demo["ImageIndex"];

                demoListView.Items.Add(item);
            }
        }

        private void DemoBrowser_Load(object sender, System.EventArgs e) {
            // TODO: load data from xml file or something

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
            demoTable.Rows.Add(new object[] {8, "Tutorial 1", "Demos.Tutorial1", "Tutorial1.jpg", "Demonstrates the typical spinning triangle demo using the engine.", "Tutorials"});

            demoView = new DataView(demoTable);

            // load the images on form load once
            foreach(DataRowView demo in demoView) {
                // add the image
                demoImageList.Images.Add(Bitmap.FromFile("../../Demos/images/" + (string)demo["ImageFile"]));
            }

            // set the properties of the image list
            demoImageList.ColorDepth = ColorDepth.Depth16Bit;
            demoImageList.ImageSize = new Size(133, 100);

            // set the listview to use the new imagelist for images
            demoListView.SmallImageList = demoImageList;
            demoListView.LargeImageList = demoImageList;
            demoListView.StateImageList = demoImageList;

            // load the items
            LoadDemoItems(demoView);
        }

        private void typeGroup_CheckedChanged(object sender, System.EventArgs e) {
            string filter = "";
            bool firstOne = true;

            // loop through each type and filter the shown items
            foreach(CheckBox type in typeGroup.Controls) {
                if(type.Checked) {
                    if(firstOne)
                        firstOne = false;
                    else
                        filter += " OR ";

                    filter += string.Format("Type = '{0}'", type.Text);
                }
            }

            // set the filter on the dataview
            if(filter.Length == 0)
                demoView.RowFilter = "Type = 'None'";
            else
                demoView.RowFilter = filter;

            // reload the demo items
            LoadDemoItems(demoView);
        }
    }
}
