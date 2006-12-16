using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;

namespace Chronos
{
	/// <summary>
	/// Summary description for Toolbox.
	/// </summary>
	public class Toolbox : System.Windows.Forms.Form
	{
		public OutlookBar.OutlookBar outlookBar1;

		public Toolbox()
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
			base.Dispose( disposing );
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.outlookBar1 = new OutlookBar.OutlookBar();
			this.SuspendLayout();
			// 
			// outlookBar1
			// 
			this.outlookBar1.AutoScroll = true;
			this.outlookBar1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.outlookBar1.ImageList = null;
			this.outlookBar1.Location = new System.Drawing.Point(0, 0);
			this.outlookBar1.Name = "outlookBar1";
			this.outlookBar1.SelectedCategory = null;
			this.outlookBar1.Size = new System.Drawing.Size(292, 266);
			this.outlookBar1.TabIndex = 1;
			// 
			// Toolbox
			// 
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.ClientSize = new System.Drawing.Size(292, 266);
			this.Controls.Add(this.outlookBar1);
			this.Name = "Toolbox";
			this.Text = "Toolbox";
			this.ResumeLayout(false);

		}
		#endregion
	}
}
