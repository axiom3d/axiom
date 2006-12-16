#region LGPL License
/*
Chronos World Editor
Copyright (C) 2004 Chris "Antiarc" Heald [antiarc@antiarc.net]

This application is free software; you can redistribute it and/or
modify it under the terms of the GNU Lesser General Public
License as published by the Free Software Foundation; either
version 2.1 of the License, or (at your option) any later version.

This application is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
Lesser General Public License for more details.

You should have received a copy of the GNU Lesser General Public
License along with this library; if not, write to the Free Software
Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
*/
#endregion

using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.IO;
using System.Text;
using ICSharpCode.TextEditor.Document;
using Axiom.Graphics;

namespace MaterialLibraryPlugin
{
	/// <summary>
	/// Summary description for EditorForm.
	/// </summary>
	public class EditorForm : System.Windows.Forms.UserControl
	{
		private System.Windows.Forms.Panel panel1;
		private ICSharpCode.TextEditor.TextEditorControl textEditorControl1;
		private TD.SandBar.ToolBar toolBar1;
		private TD.SandBar.ButtonItem buttonItem1;
		private string filename;

		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		public EditorForm()
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
			System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(EditorForm));
			this.panel1 = new System.Windows.Forms.Panel();
			this.textEditorControl1 = new ICSharpCode.TextEditor.TextEditorControl();
			this.toolBar1 = new TD.SandBar.ToolBar();
			this.buttonItem1 = new TD.SandBar.ButtonItem();
			this.SuspendLayout();
			// 
			// panel1
			// 
			this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
			this.panel1.Location = new System.Drawing.Point(0, 24);
			this.panel1.Name = "panel1";
			this.panel1.Size = new System.Drawing.Size(726, 4);
			this.panel1.TabIndex = 3;
			// 
			// textEditorControl1
			// 
			this.textEditorControl1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.textEditorControl1.Encoding = ((System.Text.Encoding)(resources.GetObject("textEditorControl1.Encoding")));
			this.textEditorControl1.Location = new System.Drawing.Point(0, 28);
			this.textEditorControl1.Name = "textEditorControl1";
			this.textEditorControl1.ShowEOLMarkers = true;
			this.textEditorControl1.ShowSpaces = true;
			this.textEditorControl1.ShowTabs = true;
			this.textEditorControl1.ShowVRuler = true;
			this.textEditorControl1.Size = new System.Drawing.Size(726, 477);
			this.textEditorControl1.TabIndex = 4;
			// 
			// toolBar1
			// 
			this.toolBar1.Buttons.AddRange(new TD.SandBar.ToolbarItemBase[] {
																				this.buttonItem1});
			this.toolBar1.Guid = new System.Guid("1b0287ac-1781-4821-b7b0-d0343bfe1649");
			this.toolBar1.IsOpen = true;
			this.toolBar1.Location = new System.Drawing.Point(0, 0);
			this.toolBar1.Name = "toolBar1";
			this.toolBar1.Size = new System.Drawing.Size(726, 24);
			this.toolBar1.TabIndex = 5;
			this.toolBar1.Text = "toolBar1";
			// 
			// buttonItem1
			// 
			this.buttonItem1.Text = "Save changes";
			this.buttonItem1.Activate += new System.EventHandler(this.buttonItem1_Activate);
			// 
			// EditorForm
			// 
			this.Controls.Add(this.textEditorControl1);
			this.Controls.Add(this.panel1);
			this.Controls.Add(this.toolBar1);
			this.Name = "EditorForm";
			this.Size = new System.Drawing.Size(726, 505);
			this.ResumeLayout(false);

		}
		#endregion

		public string Filename {
			get { return filename; }
			set { filename = value; }
		}

		public void LoadStream(Stream s, string tempfile) 
		{
			// TODO: Tempfile = ugh. Find a way to circumvent this?
			try 
			{
				File.Delete(tempfile);
				FileStream outstream = File.OpenWrite(tempfile);
				byte[] b = new byte[4096];
				int size = 1;
				while(size > 0) 
				{
					size = s.Read(b,0,4096);
					outstream.Write(b,0, size);
				}
				outstream.Close();
			} 
			catch 
			{
				MessageBox.Show(this, "Unable to open temporary file '" + tempfile + "' for writing!", this.Text, MessageBoxButtons.OK);
			}

			HighlightingManager.Manager.AddSyntaxModeFileProvider(new FileReadHighlighter());
			try 
			{
				this.textEditorControl1.LoadFile(tempfile, true, true);
			}
			catch (Exception e)
			{
				MessageBox.Show(this, "Unable to parse syntax highlighting file!\n" + e.Message, this.Text, MessageBoxButtons.OK);
			}
			File.Delete(tempfile);
		 }

		private void buttonItem1_Activate(object sender, System.EventArgs e) {
			ParseMaterial(this.textEditorControl1.Text, filename);
		}

		public void ParseMaterial(string script, string filename) {
			FileStream file = File.OpenWrite(filename);
			for(int i=0; i<script.Length; i++) {
				file.WriteByte((byte)script[i]);
			}
			file.Close();
			FileStream fStream = File.OpenRead(filename);
			(new Axiom.Serialization.MaterialSerializer()).ParseScript(fStream,filename);
			fStream.Close();
		}
	}
}
