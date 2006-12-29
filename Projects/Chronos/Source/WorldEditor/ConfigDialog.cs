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
using System.Xml;
using Axiom.Configuration;
using Axiom.Core;
using Axiom.Graphics;

namespace Chronos
{
	public class ConfigDialog : Form 
	{
		private GroupBox grpVideoOptions;
		private Label lblDriver;
		private ComboBox cboRenderSystems;
		private Label lblResolution;
		private ComboBox cboResolution;
		private Button btnOk;
		private Button btnCancel;

		public ConfigDialog() 
		{
			this.SetStyle(ControlStyles.DoubleBuffer, true);
			InitializeComponent();
			//this.Icon = new Icon("Media/Icons/AxiomIcon.ico");
		}

		private void InitializeComponent() 
		{
			this.lblResolution = new System.Windows.Forms.Label();
			this.btnCancel = new System.Windows.Forms.Button();
			this.btnOk = new System.Windows.Forms.Button();
			this.grpVideoOptions = new System.Windows.Forms.GroupBox();
			this.lblDriver = new System.Windows.Forms.Label();
			this.cboRenderSystems = new System.Windows.Forms.ComboBox();
			this.cboResolution = new System.Windows.Forms.ComboBox();
			this.grpVideoOptions.SuspendLayout();
			this.SuspendLayout();
			// 
			// lblResolution
			// 
			this.lblResolution.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.lblResolution.ForeColor = System.Drawing.Color.Black;
			this.lblResolution.Location = new System.Drawing.Point(16, 70);
			this.lblResolution.Name = "lblResolution";
			this.lblResolution.Size = new System.Drawing.Size(128, 16);
			this.lblResolution.TabIndex = 7;
			this.lblResolution.Text = "Resolution:";
			this.lblResolution.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			// 
			// btnCancel
			// 
			this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.btnCancel.BackColor = System.Drawing.Color.White;
			this.btnCancel.Cursor = System.Windows.Forms.Cursors.Hand;
			this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.btnCancel.Font = new System.Drawing.Font("Arial", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.btnCancel.ForeColor = System.Drawing.Color.Black;
			this.btnCancel.Location = new System.Drawing.Point(216, 120);
			this.btnCancel.Name = "btnCancel";
			this.btnCancel.Size = new System.Drawing.Size(120, 24);
			this.btnCancel.TabIndex = 1;
			this.btnCancel.Text = "Cancel";
			this.btnCancel.Click += new System.EventHandler(this.Cancel_Click);
			// 
			// btnOk
			// 
			this.btnOk.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.btnOk.BackColor = System.Drawing.Color.White;
			this.btnOk.Cursor = System.Windows.Forms.Cursors.Hand;
			this.btnOk.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.btnOk.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.btnOk.Font = new System.Drawing.Font("Arial", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.btnOk.ForeColor = System.Drawing.Color.Black;
			this.btnOk.Location = new System.Drawing.Point(16, 120);
			this.btnOk.Name = "btnOk";
			this.btnOk.Size = new System.Drawing.Size(120, 24);
			this.btnOk.TabIndex = 0;
			this.btnOk.Text = "Start";
			this.btnOk.Click += new System.EventHandler(this.Ok_Click);
			// 
			// grpVideoOptions
			// 
			this.grpVideoOptions.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
				| System.Windows.Forms.AnchorStyles.Left) 
				| System.Windows.Forms.AnchorStyles.Right)));
			this.grpVideoOptions.Controls.Add(this.lblDriver);
			this.grpVideoOptions.Controls.Add(this.cboRenderSystems);
			this.grpVideoOptions.Controls.Add(this.lblResolution);
			this.grpVideoOptions.Controls.Add(this.cboResolution);
			this.grpVideoOptions.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.grpVideoOptions.Font = new System.Drawing.Font("Arial", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.grpVideoOptions.ForeColor = System.Drawing.Color.Black;
			this.grpVideoOptions.Location = new System.Drawing.Point(8, 8);
			this.grpVideoOptions.Name = "grpVideoOptions";
			this.grpVideoOptions.Size = new System.Drawing.Size(336, 104);
			this.grpVideoOptions.TabIndex = 6;
			this.grpVideoOptions.TabStop = false;
			this.grpVideoOptions.Text = "Video Options";
			// 
			// lblDriver
			// 
			this.lblDriver.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.lblDriver.ForeColor = System.Drawing.Color.Black;
			this.lblDriver.Location = new System.Drawing.Point(71, 37);
			this.lblDriver.Name = "lblDriver";
			this.lblDriver.Size = new System.Drawing.Size(72, 16);
			this.lblDriver.TabIndex = 9;
			this.lblDriver.Text = "Driver:";
			this.lblDriver.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			// 
			// cboRenderSystems
			// 
			this.cboRenderSystems.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
				| System.Windows.Forms.AnchorStyles.Right)));
			this.cboRenderSystems.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.cboRenderSystems.Font = new System.Drawing.Font("Palatino Linotype", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.cboRenderSystems.ForeColor = System.Drawing.Color.FromArgb(((System.Byte)(25)), ((System.Byte)(35)), ((System.Byte)(75)));
			this.cboRenderSystems.Location = new System.Drawing.Point(144, 32);
			this.cboRenderSystems.Name = "cboRenderSystems";
			this.cboRenderSystems.Size = new System.Drawing.Size(176, 24);
			this.cboRenderSystems.TabIndex = 8;
			this.cboRenderSystems.SelectedIndexChanged += new System.EventHandler(this.RenderSystems_SelectedIndexChanged);
			// 
			// cboResolution
			// 
			this.cboResolution.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
				| System.Windows.Forms.AnchorStyles.Right)));
			this.cboResolution.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.cboResolution.Font = new System.Drawing.Font("Palatino Linotype", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.cboResolution.ForeColor = System.Drawing.Color.FromArgb(((System.Byte)(25)), ((System.Byte)(35)), ((System.Byte)(75)));
			this.cboResolution.Location = new System.Drawing.Point(144, 64);
			this.cboResolution.Name = "cboResolution";
			this.cboResolution.Size = new System.Drawing.Size(176, 24);
			this.cboResolution.TabIndex = 6;
			// 
			// ConfigDialog
			// 
			this.AcceptButton = this.btnOk;
			this.AutoScaleBaseSize = new System.Drawing.Size(6, 17);
			this.BackColor = System.Drawing.SystemColors.Control;
			this.CancelButton = this.btnCancel;
			this.ClientSize = new System.Drawing.Size(346, 154);
			this.ControlBox = false;
			this.Controls.Add(this.grpVideoOptions);
			this.Controls.Add(this.btnOk);
			this.Controls.Add(this.btnCancel);
			this.Font = new System.Drawing.Font("Palatino Linotype", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "ConfigDialog";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Load += new System.EventHandler(this.ConfigDialog_Load);
			this.grpVideoOptions.ResumeLayout(false);
			this.ResumeLayout(false);

		}

		private void Ok_Click(object sender, EventArgs e) 
		{
            //Root.Instance.RenderSystem = (RenderSystem) cboRenderSystems.SelectedItem;
            //EngineConfig.DisplayModeRow mode = Root.Instance.RenderSystem.ConfigOptions["DisplayMode"];//[cboResolution.SelectedIndex];
            //mode.FullScreen = false;
            //mode.Selected = true;
            //SaveDisplaySettings(cboRenderSystems.SelectedIndex.ToString(), cboResolution.SelectedIndex.ToString(), false.ToString());
			this.Dispose();
		}

		private void Cancel_Click(object sender, EventArgs e) 
		{
			this.Dispose();
		}

		private void ConfigDialog_Load(object sender, System.EventArgs e) 
		{
			foreach(RenderSystem renderSystem in Root.Instance.RenderSystems) 
			{
				cboRenderSystems.Items.Add(renderSystem);
			}

			XmlTextReader settingsReader = null;

			try 
			{
				string temp = string.Empty;
                
				settingsReader = new XmlTextReader("DisplayConfig.xml");

				while(settingsReader.Read()) 
				{
					if(settingsReader.NodeType == XmlNodeType.Element) 
					{
						if(settingsReader.LocalName.Equals("RenderSystem")) 
						{
							temp = settingsReader.ReadString();
							if(cboRenderSystems.Items.Count > int.Parse(temp)) 
							{
								cboRenderSystems.SelectedIndex = int.Parse(temp);
							}
							else 
							{
								cboRenderSystems.SelectedIndex = 1;
							}
						}

						if(settingsReader.LocalName.Equals("Resolution")) 
						{
							temp = settingsReader.ReadString();
							if(cboResolution.Items.Count > int.Parse(temp)) 
							{
								cboResolution.SelectedIndex = int.Parse(temp);
							}
							else 
							{
								cboResolution.SelectedIndex = (cboResolution.Items.Count - 1);
							}
						}
					}
				}
			}
			catch 
			{
				// HACK: Trying to force Tao.OpenGl to be listed first.
				if(cboRenderSystems.Items.Count > 1) 
				{
					cboRenderSystems.SelectedIndex = 1;
				}
				else 
				{
					cboRenderSystems.SelectedIndex = 0;
				}

				cboResolution.SelectedIndex = 0;
			}
			finally 
			{
				if(settingsReader != null) 
				{
					settingsReader.Close();
				}
			}
		}

		private void RenderSystems_SelectedIndexChanged(object sender, EventArgs e) 
		{
			cboResolution.Items.Clear();
			RenderSystem system = (RenderSystem) cboRenderSystems.SelectedItem;

			//foreach(RootConfig.DisplayModeRow mode in system.ConfigOptions.DisplayMode)
			//	cboResolution.Items.Add(string.Format("{0} x {1} @ {2}bpp", mode.Width, mode.Height, mode.Bpp));

			cboResolution.SelectedIndex = 0;
		}

		public void SaveDisplaySettings(string renderSystem, string resolution, string fullScreen ) 
		{
			XmlTextWriter settingsWriter = new XmlTextWriter("DisplayConfig.xml", null);
			settingsWriter.Formatting = Formatting.Indented;
			settingsWriter.Indentation = 6;
			settingsWriter.Namespaces = false;

			settingsWriter.WriteStartDocument();
			settingsWriter.WriteStartElement("", "Settings", "");
			settingsWriter.WriteStartElement("", "RenderSystem", "");
			settingsWriter.WriteString(renderSystem);
			settingsWriter.WriteEndElement();

			settingsWriter.WriteStartElement("", "Resolution", "");
			settingsWriter.WriteString(resolution);
			settingsWriter.WriteEndElement();

			settingsWriter.WriteStartElement("", "FullScreen", "");
			settingsWriter.WriteString(fullScreen);
			settingsWriter.WriteEndElement();
			settingsWriter.WriteEndElement();
			settingsWriter.WriteEndDocument();
			settingsWriter.Flush();
		}
	}
}
