using System;
using System.Diagnostics;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;

namespace Axiom.Utility
{
	/// <summary>
	/// Summary description for DebugWindow.
	/// </summary>
	public class OutputWindow : System.Windows.Forms.Form
	{
        internal System.Windows.Forms.RichTextBox debugText;
        private System.Windows.Forms.Button button1;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		public OutputWindow()
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
            this.debugText = new System.Windows.Forms.RichTextBox();
            this.button1 = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // debugText
            // 
            this.debugText.Font = new System.Drawing.Font("Palatino Linotype", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
            this.debugText.Location = new System.Drawing.Point(0, 0);
            this.debugText.Name = "debugText";
            this.debugText.Size = new System.Drawing.Size(400, 264);
            this.debugText.TabIndex = 0;
            this.debugText.Text = "";
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(145, 272);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(112, 23);
            this.button1.TabIndex = 1;
            this.button1.Text = "Copy To Clipboard";
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // OutputWindow
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.ClientSize = new System.Drawing.Size(402, 304);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.debugText);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "OutputWindow";
            this.Text = "Axiom Debug Window";
            this.ResumeLayout(false);

        }
		#endregion

        private void button1_Click(object sender, System.EventArgs e) {
            Clipboard.SetDataObject(debugText.Text, true);
        }
	}

    public class DebugWindow : TraceListener {

        private OutputWindow output;
        private RichTextBox debugText;

        public DebugWindow() {
            output = new OutputWindow();
            debugText = output.debugText;
        }

        public void Show() {
            output.Show();
        }

        public override void Write(string message) {
            if(!debugText.IsDisposed) {
                debugText.Text += message;
            }
        }

        public override void WriteLine(string message) {
            if(!debugText.IsDisposed) {
                debugText.Text += "\r\n" + message;
                debugText.SelectionStart = debugText.Text.Length;
            }
            //debugText.Focus();
        }
    }
}
