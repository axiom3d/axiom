using System;
using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.IO;
using System.Windows.Forms;
using Axiom.Core;
using Axiom.Graphics;

namespace MaterialLibraryPlugin
{
	/// <summary>
	/// Summary description for TextureBrowser.
	/// </summary>
	public class TextureBrowser : System.Windows.Forms.Form
	{
		/// <summary> 
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;
		private Panel selectedPanel;
		private System.Windows.Forms.ComboBox comboBox1;
		private System.Windows.Forms.Panel panel1;
		private string selectedTexture;

		public TextureBrowser()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

			string[] extensions = {"*.jpg","*.jpeg","*.png","*.gif","*.bmp"};

			this.comboBox1.Items.Clear();

			ArrayList itemList = new ArrayList();
			foreach(string ext in extensions) {
				StringCollection items = MaterialManager.GetAllCommonNamesLike("", ext);
				foreach(string item in items) {
					itemList.Add(item);
				}
			}
			
			itemList.Sort();
			int top = 10, left = 10;
			this.SuspendLayout();
			foreach(string item in itemList) {
				Panel p = new Panel();
				Stream s = MaterialManager.FindCommonResourceData(item);

				Image i = Bitmap.FromStream(s);
				Label l = new Label();
				Label l2 = new Label();
				l.Height = 32;
				l2.Height = 16;
				l.Text = item;
				l2.Text = "[" + i.Width.ToString() + "x" + i.Height.ToString() + "]";
				l.Width = 100;
				l2.Width = 100;
				l.Location = new System.Drawing.Point(5, 105);
				l2.Location = new System.Drawing.Point(5, 137);
				l.TextAlign = ContentAlignment.MiddleCenter;
				l2.TextAlign = ContentAlignment.MiddleCenter;
				l.Click += new EventHandler(l_Click);
				l2.Click += new EventHandler(l_Click);

				PictureBox picBox = new PictureBox();
				picBox.Image = i;
				picBox.Size = new System.Drawing.Size(100,100);
				picBox.Location = new System.Drawing.Point(5, 5);
				if(i.Width > 100 || i.Height > 100)
					picBox.SizeMode = PictureBoxSizeMode.StretchImage;
				else
					picBox.SizeMode = PictureBoxSizeMode.CenterImage;
				picBox.Click +=new EventHandler(picBox_Click);

				p.Location = new System.Drawing.Point(left, top);
				p.Width = 112;
				p.Height = 153;
				p.BorderStyle = BorderStyle.FixedSingle;
				p.Click += new EventHandler(p_Click);
				p.BackColor = Color.DarkGray;
				left += p.Width + 10;
				if(left + p.Width + 10 > this.Width) {
					top += 165;
					left = 10;
				}
				p.Controls.Add(l);
				p.Controls.Add(l2);
				p.Controls.Add(picBox);
				panel1.Controls.Add(p);
			}
			this.ResumeLayout();

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
			this.comboBox1 = new System.Windows.Forms.ComboBox();
			this.panel1 = new System.Windows.Forms.Panel();
			this.SuspendLayout();
			// 
			// comboBox1
			// 
			this.comboBox1.Dock = System.Windows.Forms.DockStyle.Top;
			this.comboBox1.Location = new System.Drawing.Point(0, 0);
			this.comboBox1.Name = "comboBox1";
			this.comboBox1.Size = new System.Drawing.Size(672, 21);
			this.comboBox1.TabIndex = 0;
			this.comboBox1.Text = "comboBox1";
			// 
			// panel1
			// 
			this.panel1.AutoScroll = true;
			this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.panel1.Location = new System.Drawing.Point(0, 21);
			this.panel1.Name = "panel1";
			this.panel1.Size = new System.Drawing.Size(672, 377);
			this.panel1.TabIndex = 1;
			this.panel1.Resize += new System.EventHandler(this.panel1_Resize);
			// 
			// TextureBrowser
			// 
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.BackColor = System.Drawing.SystemColors.Control;
			this.ClientSize = new System.Drawing.Size(672, 398);
			this.Controls.Add(this.panel1);
			this.Controls.Add(this.comboBox1);
			this.Name = "TextureBrowser";
			this.Text = "Texture Browser";
			this.ResumeLayout(false);

		}
		#endregion

		private void picBox_MouseHover(object sender, EventArgs e) {
			(sender as PictureBox).BorderStyle = BorderStyle.FixedSingle;
		}

		public void SelectPicture(Panel p) {
			if(selectedPanel != null) {
				this.selectedPanel.BackColor = Color.DarkGray;
				foreach(Control c in selectedPanel.Controls)
					if(c is Label) {
						c.ForeColor = Color.Black;
					}
			}
			p.BackColor = Color.Navy;
			selectedPanel = p;
			selectedTexture = null;
			foreach(Control c in p.Controls)
				if(c is Label) {
					c.ForeColor = Color.White;
					if(selectedTexture == null)
						selectedTexture = c.Text;
				}
		}

		public void SelectPicture(string texture) {
			foreach(Control c in this.Controls) {
				if(c is Panel) {
					foreach(Control pc in c.Controls) {
						if(pc is Label && pc.Text.ToLower() == texture.ToLower()) {
							SelectPicture(c as Panel);
						}
					}
				}
			}
		}

		private void p_Click(object sender, EventArgs e) {
			SelectPicture(sender as Panel);
			this.Close();
		}

		private void l_Click(object sender, EventArgs e) {
			SelectPicture((sender as Label).Parent as Panel);
			this.Close();
		}

		private void picBox_Click(object sender, EventArgs e) {
			SelectPicture((sender as PictureBox).Parent as Panel);
			this.Close();
		}

		private void panel1_Resize(object sender, System.EventArgs e) {
			int left = 10, top = 10;
			foreach(Control c in (sender as Panel).Controls) {
				if(left + c.Width + 10 > (sender as Panel).Width) {
					left = 10;
					top += c.Height + 10;
					c.Left = left;
					c.Top = top;
				} else {
					c.Left = left;
					c.Top = top;
					left += c.Width + 10;
				}
			}
		}

		public string SelectedTexture {
			get { return selectedTexture; }
		}
	}
}
