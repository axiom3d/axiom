// created on 8/4/2003 at 1:30 AM
using System;
using System.Windows.Forms;

namespace MyForm {
	public class CreatedForm : System.Windows.Forms.Form
	{
		public CreatedForm()
		{
			InitializeComponent();
		}
		
		// THIS METHOD IS MAINTAINED BY THE FORM DESIGNER
		// DO NOT EDIT IT MANUALLY! YOUR CHANGES ARE LIKELY TO BE LOST
		void CreatedFormLoad(object sender, System.EventArgs e)
		{
			this.Controls.Add(new PictureBox());
		}
		
		void InitializeComponent() {
			this.SuspendLayout();
			// 
			// CreatedForm
			// 
			this.ClientSize = new System.Drawing.Size(292, 266);
			this.Load += new System.EventHandler(this.CreatedFormLoad);
			this.ResumeLayout(false);
		}
	}
}
