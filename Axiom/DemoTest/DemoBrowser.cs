using System;
using System.Data;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using Axiom.Utility;

namespace Demos
{
	/// <summary>
	/// Summary description for DemoBrowser.
	/// </summary>
	public class DemoBrowser : System.Windows.Forms.Form
	{
		private System.ComponentModel.IContainer components;
		private System.Windows.Forms.Label lblInfo;
		private System.Windows.Forms.ListView listView1;

		public DemoBrowser()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

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
		private void InitializeComponent() {
			this.listView1 = new System.Windows.Forms.ListView();
			this.lblInfo = new System.Windows.Forms.Label();
			this.SuspendLayout();
			// 
			// listView1
			// 
			this.listView1.Activation = System.Windows.Forms.ItemActivation.OneClick;
			this.listView1.BackColor = System.Drawing.Color.Black;
			this.listView1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.listView1.Font = new System.Drawing.Font("Palatino Linotype", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.listView1.ForeColor = System.Drawing.Color.White;
			this.listView1.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
			this.listView1.Location = new System.Drawing.Point(8, 56);
			this.listView1.MultiSelect = false;
			this.listView1.Name = "listView1";
			this.listView1.Size = new System.Drawing.Size(600, 280);
			this.listView1.TabIndex = 0;
			this.listView1.Click += new System.EventHandler(this.listView1_Click);
			this.listView1.MouseMove += new System.Windows.Forms.MouseEventHandler(this.listView1_MouseMove);
			// 
			// lblInfo
			// 
			this.lblInfo.BackColor = System.Drawing.Color.White;
			this.lblInfo.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.lblInfo.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.lblInfo.ForeColor = System.Drawing.Color.Navy;
			this.lblInfo.Location = new System.Drawing.Point(8, 8);
			this.lblInfo.Name = "lblInfo";
			this.lblInfo.Size = new System.Drawing.Size(528, 40);
			this.lblInfo.TabIndex = 2;
			this.lblInfo.Text = "Select a demo to run.  The description of each demo will appear here as you hover" +
				" over them with the mouse.";
			// 
			// DemoBrowser
			// 
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.BackColor = System.Drawing.Color.LightSteelBlue;
			this.ClientSize = new System.Drawing.Size(616, 346);
			this.Controls.Add(this.lblInfo);
			this.Controls.Add(this.listView1);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.Name = "DemoBrowser";
			this.Text = "Axiom Engine - Demo Browser v0.12";
			this.Load += new System.EventHandler(this.DemoBrowser_Load);
			this.ResumeLayout(false);

		}
		#endregion

		private void listView1_Click(object sender, System.EventArgs e)
		{
			if(listView1.SelectedItems.Count > 0)
			{
				// get the first one, we only do single select for the demos
				ListViewItem item = listView1.SelectedItems[0];

				if(item.Tag != null)
				{
					// find the class name of the demo
					string demoClassName = item.Tag.ToString();

					if(demoClassName.Length != 0)
					{
						// get the type of the demo class
						Type type = Type.GetType(demoClassName);

						// create an instance of the demo class and start it up
						using(TechDemo demo = (TechDemo)Activator.CreateInstance(type))
						{
							demo.Start();
						}

						// quite the application
						// TODO: Fix so that multiple demos can be run without reinitialization issues
						Application.Exit();
					}
				}
			}
		}

		private void listView1_MouseMove(object sender, System.Windows.Forms.MouseEventArgs e)
		{
			ListViewItem item = listView1.GetItemAt(e.X, e.Y);

			if(item != null)
			{			
				if(item.SubItems.Count > 1)
				{
					lblInfo.Text = item.SubItems[1].Text;
				}
			}
		}

		private void LoadDemoItems(DataTable demoTable)
		{
			ImageList demoImageList = new ImageList();
			demoImageList.ColorDepth = ColorDepth.Depth16Bit;
			demoImageList.ImageSize = new Size(133, 100);

			for(int i = 0; i < demoTable.Rows.Count; i++)
			{
				DataRow demo = demoTable.Rows[i];

				// add the image
				demoImageList.Images.Add(Bitmap.FromFile("../../Demos/images/" + (string)demo["ImageFile"]));

				ListViewItem item = new ListViewItem();
				item.Tag = (string)demo["ClassName"];
				item.Text = (string)demo["Name"];
				item.SubItems.Add((string)demo["Description"]);
				item.ImageIndex = i;

				listView1.Items.Add(item);

				listView1.SmallImageList = demoImageList;
				listView1.LargeImageList = demoImageList;
				listView1.StateImageList = demoImageList;
			}
		}

		private void DemoBrowser_Load(object sender, System.EventArgs e)
		{
			// TODO: load data from xml file or something
			DataTable demoTable = new DataTable("Demos");
			demoTable.Columns.Add("Name");
			demoTable.Columns.Add("ClassName");
			demoTable.Columns.Add("ImageFile");
			demoTable.Columns.Add("Description");

			demoTable.Rows.Add(new object[] {"Camera Track", "Demos.CameraTrack", "CameraTrack.jpg", "Watch the camera follow a defined spline path while maintaining focus on an object in the scene."});
			demoTable.Rows.Add(new object[] {"TextureFX", "Demos.TextureFX", "TextureFX.jpg", "Demonstrates the usage of various texture effects, including scrolling and rotating."});
			demoTable.Rows.Add(new object[] {"Transparency", "Demos.Transparency", "Transparency.jpg", "A high poly scene with transparent entities showing how scene blending works."});
			demoTable.Rows.Add(new object[] {"Environment Mapping", "Demos.EnvMapping", "EnvMapping.jpg", "Small example of an environment mapped entity."});
			demoTable.Rows.Add(new object[] {"Sky Plane", "Demos.SkyPlane", "SkyPlane.jpg", "Example of creating a scene with a skyplane."});
			demoTable.Rows.Add(new object[] {"Sky Box", "Demos.SkyBox", "SkyBox.jpg", "Example of creating a scene with a skybox."});
			demoTable.Rows.Add(new object[] {"Lights", "Demos.Lights", "Lights.jpg", "Example of creating a scene with lights and billboards."});

			// load the items
			LoadDemoItems(demoTable);
		}
	}
}
