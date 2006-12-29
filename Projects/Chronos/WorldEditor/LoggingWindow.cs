using System;
using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.IO;
using System.Windows.Forms;
using System.Diagnostics;

using Chronos.Diagnostics;

namespace Chronos
{
	/// <summary>
	/// OutputControl displays various catagorized outputs.
	/// </summary>
	public class LoggingWindow : System.Windows.Forms.Form
	{
		UberMulticastLogListener listener;

		private System.Windows.Forms.RichTextBox richTextBox1;
		private System.Windows.Forms.ComboBox comboBox1;

		/// <summary> 
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		public LoggingWindow()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

			// Fill control.
			this.Dock = DockStyle.Fill;

			// Setup the comboBox.
			comboBox1.Items.Clear();
			comboBox1.Items.Add(Logs.Axiom);
			comboBox1.Items.Add(Logs.Error);
			comboBox1.Items.Add(Logs.General);
			comboBox1.Items.Add(Logs.Notice);
			comboBox1.Items.Add(Logs.Status);
			comboBox1.Items.Add(Logs.Trace);
			comboBox1.Items.Add(Logs.Warning);
			comboBox1.SelectedItem = comboBox1.Items[1];

			// Eavesdrop on the logging infrastructure.
			listener = new UberMulticastLogListener();
			listener.TextChanged +=new TextChangedEventHandler(listener_TextChanged);

			Log.Listeners(Logs.Axiom).Add(listener);
			Log.Listeners(Logs.Error).Add(listener);
			Log.Listeners(Logs.General).Add(listener);
			Log.Listeners(Logs.Notice).Add(listener);
			Log.Listeners(Logs.Status).Add(listener);
			Log.Listeners(Logs.Trace).Add(listener);
			Log.Listeners(Logs.Warning).Add(listener);
		}

		#region Event Handlers

		private void comboBox1_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			if(listener != null) 
			{
				richTextBox1.Text = listener.Text((Logs) comboBox1.SelectedItem);
			}
		}

		private void listener_TextChanged(object sender, Logs catagory)
		{
			if(((Logs) comboBox1.SelectedItem) == catagory) 
			{
				richTextBox1.Text = listener.Text(catagory);
			}
		}

		#endregion

		#region Component Designer generated code
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

		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.richTextBox1 = new System.Windows.Forms.RichTextBox();
			this.comboBox1 = new System.Windows.Forms.ComboBox();
			this.SuspendLayout();
			// 
			// richTextBox1
			// 
			this.richTextBox1.AutoSize = true;
			this.richTextBox1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.richTextBox1.Font = new System.Drawing.Font("Courier New", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.richTextBox1.Location = new System.Drawing.Point(0, 21);
			this.richTextBox1.Name = "richTextBox1";
			this.richTextBox1.ReadOnly = true;
			this.richTextBox1.Size = new System.Drawing.Size(320, 197);
			this.richTextBox1.TabIndex = 0;
			this.richTextBox1.Text = "";
			this.richTextBox1.WordWrap = false;
			// 
			// comboBox1
			// 
			this.comboBox1.Dock = System.Windows.Forms.DockStyle.Top;
			this.comboBox1.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBox1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.comboBox1.Location = new System.Drawing.Point(0, 0);
			this.comboBox1.Name = "comboBox1";
			this.comboBox1.Size = new System.Drawing.Size(320, 21);
			this.comboBox1.TabIndex = 1;
			this.comboBox1.SelectedIndexChanged += new System.EventHandler(this.comboBox1_SelectedIndexChanged);
			// 
			// LoggingWindow
			// 
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.ClientSize = new System.Drawing.Size(320, 218);
			this.Controls.Add(this.richTextBox1);
			this.Controls.Add(this.comboBox1);
			this.Name = "LoggingWindow";
			this.ResumeLayout(false);

		}
		#endregion
	}

}
