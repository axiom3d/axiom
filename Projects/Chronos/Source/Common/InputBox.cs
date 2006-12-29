using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;

namespace Chronos.Core
{
	/// <summary>
	/// Summary description for InputBox.
	/// </summary>
	public class InputBox : System.Windows.Forms.Form {
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.TextBox textBox1;
		private System.Windows.Forms.Button button1;
		private System.Windows.Forms.Button button2;

		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		private InputBox() {
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			//
			// TODO: Add any constructor code after InitializeComponent call
			//
		}

		public static string Show(string title, string prompt, string defaultValue) {
			InputBox ib = new InputBox();
			ib.Text = title;
			ib.label1.Text = prompt;
			ib.textBox1.Text = defaultValue;
			if(ib.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
				return ib.textBox1.Text;
			} else {
				return null;
			}
		}

		public static string Show(string title, string prompt) {
			return Show(title, prompt, "");
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
			this.label1 = new System.Windows.Forms.Label();
			this.textBox1 = new System.Windows.Forms.TextBox();
			this.button1 = new System.Windows.Forms.Button();
			this.button2 = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// label1
			// 
			this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
				| System.Windows.Forms.AnchorStyles.Left) 
				| System.Windows.Forms.AnchorStyles.Right)));
			this.label1.Location = new System.Drawing.Point(8, 8);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(384, 32);
			this.label1.TabIndex = 0;
			this.label1.Text = "label1";
			// 
			// textBox1
			// 
			this.textBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
				| System.Windows.Forms.AnchorStyles.Right)));
			this.textBox1.Location = new System.Drawing.Point(8, 48);
			this.textBox1.Name = "textBox1";
			this.textBox1.Size = new System.Drawing.Size(384, 20);
			this.textBox1.TabIndex = 1;
			this.textBox1.Text = "";
			// 
			// button1
			// 
			this.button1.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
			this.button1.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.button1.Location = new System.Drawing.Point(104, 80);
			this.button1.Name = "button1";
			this.button1.Size = new System.Drawing.Size(80, 24);
			this.button1.TabIndex = 2;
			this.button1.Text = "&Ok";
			this.button1.Click += new System.EventHandler(this.button1_Click);
			// 
			// button2
			// 
			this.button2.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
			this.button2.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.button2.Location = new System.Drawing.Point(200, 80);
			this.button2.Name = "button2";
			this.button2.Size = new System.Drawing.Size(80, 24);
			this.button2.TabIndex = 3;
			this.button2.Text = "&Cancel";
			this.button2.Click += new System.EventHandler(this.button2_Click);
			// 
			// InputBox
			// 
			this.AcceptButton = this.button1;
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.CancelButton = this.button2;
			this.ClientSize = new System.Drawing.Size(400, 115);
			this.ControlBox = false;
			this.Controls.Add(this.button2);
			this.Controls.Add(this.button1);
			this.Controls.Add(this.textBox1);
			this.Controls.Add(this.label1);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.Name = "InputBox";
			this.ShowInTaskbar = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "InputBox";
			this.ResumeLayout(false);

		}
		#endregion

		private void button2_Click(object sender, System.EventArgs e) {
		
		}

		private void button1_Click(object sender, System.EventArgs e) {
		
		}
	}
}
