#region LGPL License
/*
Axiom Game Engine Library
Copyright (C) 2003  Axiom Project Team

The overall design, and a majority of the core engine and rendering code 
contained within this library is a derivative of the open source Object Oriented 
Graphics Engine OGRE, which can be found at http://ogre.sourceforge.net.  
Many thanks to the OGRE team for maintaining such a high quality project.

This library is free software; you can redistribute it and/or
modify it under the terms of the GNU Lesser General Public
License as published by the Free Software Foundation; either
version 2.1 of the License, or (at your option) any later version.

This library is distributed in the hope that it will be useful,
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
using Axiom.Exceptions;
using Axiom.Graphics;

namespace Axiom.Utility {
    public class ConfigDialog : Form {
        private Container components = null;
        private PictureBox picLogo;
        private GroupBox grpVideoOptions;
        private Label lblDriver;
        private ComboBox cboRenderSystems;
        private Label lblResolution;
        private ComboBox cboResolution;
        private CheckBox chkFullscreen;
        private Button btnOk;
        private Button btnCancel;

        public ConfigDialog() {
            this.SetStyle(ControlStyles.DoubleBuffer, true);
            InitializeComponent();
            this.Icon = new Icon(ResourceManager.FindCommonResourceData("AxiomIcon.ico"));
        }

        protected override void Dispose(bool disposing) {
            if(disposing) {
                if(components != null) {
                    components.Dispose();
                }
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent() {
            this.lblResolution = new Label();
            this.btnCancel = new Button();
            this.picLogo = new PictureBox();
            this.chkFullscreen = new CheckBox();
            this.btnOk = new Button();
            this.grpVideoOptions = new GroupBox();
            this.lblDriver = new Label();
            this.cboRenderSystems = new ComboBox();
            this.cboResolution = new ComboBox();
            this.grpVideoOptions.SuspendLayout();
            this.SuspendLayout();
            // 
            // lblResolution
            // 
            this.lblResolution.FlatStyle = FlatStyle.Flat;
            this.lblResolution.ForeColor = Color.FromArgb(((Byte)(25)), ((Byte)(35)), ((Byte)(75)));
            this.lblResolution.Location = new Point(16, 70);
            this.lblResolution.Name = "lblResolution";
            this.lblResolution.Size = new Size(128, 16);
            this.lblResolution.TabIndex = 7;
            this.lblResolution.Text = "Resolution:";
            this.lblResolution.TextAlign = ContentAlignment.MiddleRight;
            // 
            // btnCancel
            // 
            this.btnCancel.BackColor = Color.White;
            this.btnCancel.Cursor = Cursors.Hand;
            this.btnCancel.DialogResult = DialogResult.Cancel;
            this.btnCancel.FlatStyle = FlatStyle.Flat;
            this.btnCancel.Font = new Font("Palatino Linotype", 12F, FontStyle.Regular, GraphicsUnit.Point, ((Byte)(0)));
            this.btnCancel.ForeColor = Color.FromArgb(((Byte)(25)), ((Byte)(35)), ((Byte)(75)));
            this.btnCancel.Location = new Point(237, 287);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new Size(96, 24);
            this.btnCancel.TabIndex = 1;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.Click += new EventHandler(Cancel_Click);
            // 
            // picLogo
            // 
            this.picLogo.Image = Bitmap.FromStream(ResourceManager.FindCommonResourceData("AxiomLogo.png"), true);
            this.picLogo.Location = new Point(94, -14);
            this.picLogo.Name = "picLogo";
            this.picLogo.Size = new Size(256, 128);
            this.picLogo.SizeMode = PictureBoxSizeMode.AutoSize;
            this.picLogo.TabIndex = 3;
            this.picLogo.TabStop = false;
            // 
            // chkFullscreen
            // 
            this.chkFullscreen.BackColor = Color.White;
            this.chkFullscreen.Cursor = Cursors.Hand;
            this.chkFullscreen.FlatStyle = FlatStyle.Flat;
            this.chkFullscreen.Font = new Font("Palatino Linotype", 12F, FontStyle.Regular, GraphicsUnit.Point, ((Byte)(0)));
            this.chkFullscreen.ForeColor = Color.FromArgb(((Byte)(25)), ((Byte)(35)), ((Byte)(75)));
            this.chkFullscreen.Location = new Point(143, 102);
            this.chkFullscreen.Name = "chkFullscreen";
            this.chkFullscreen.Size = new Size(144, 24);
            this.chkFullscreen.TabIndex = 0;
            this.chkFullscreen.Text = "Fullscreen?";
            // 
            // btnOk
            // 
            this.btnOk.BackColor = Color.White;
            this.btnOk.Cursor = Cursors.Hand;
            this.btnOk.DialogResult = DialogResult.OK;
            this.btnOk.FlatStyle = FlatStyle.Flat;
            this.btnOk.Font = new Font("Palatino Linotype", 12F, FontStyle.Regular, GraphicsUnit.Point, ((Byte)(0)));
            this.btnOk.ForeColor = Color.FromArgb(((Byte)(25)), ((Byte)(35)), ((Byte)(75)));
            this.btnOk.Location = new Point(109, 287);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new Size(96, 24);
            this.btnOk.TabIndex = 0;
            this.btnOk.Text = "Start";
            this.btnOk.Click += new EventHandler(Ok_Click);
            // 
            // grpVideoOptions
            // 
            this.grpVideoOptions.Controls.Add(this.lblDriver);
            this.grpVideoOptions.Controls.Add(this.cboRenderSystems);
            this.grpVideoOptions.Controls.Add(this.lblResolution);
            this.grpVideoOptions.Controls.Add(this.cboResolution);
            this.grpVideoOptions.Controls.Add(this.chkFullscreen);
            this.grpVideoOptions.FlatStyle = FlatStyle.Flat;
            this.grpVideoOptions.Font = new Font("Palatino Linotype", 12F, FontStyle.Regular, GraphicsUnit.Point, ((Byte)(0)));
            this.grpVideoOptions.ForeColor = Color.FromArgb(((Byte)(25)), ((Byte)(35)), ((Byte)(75)));
            this.grpVideoOptions.Location = new Point(24, 111);
            this.grpVideoOptions.Name = "grpVideoOptions";
            this.grpVideoOptions.Size = new Size(392, 144);
            this.grpVideoOptions.TabIndex = 6;
            this.grpVideoOptions.TabStop = false;
            this.grpVideoOptions.Text = "Video Options";
            // 
            // lblDriver
            // 
            this.lblDriver.FlatStyle = FlatStyle.Flat;
            this.lblDriver.ForeColor = Color.FromArgb(((Byte)(25)), ((Byte)(35)), ((Byte)(75)));
            this.lblDriver.Location = new Point(71, 37);
            this.lblDriver.Name = "lblDriver";
            this.lblDriver.Size = new Size(72, 16);
            this.lblDriver.TabIndex = 9;
            this.lblDriver.Text = "Driver:";
            this.lblDriver.TextAlign = ContentAlignment.MiddleRight;
            // 
            // cboRenderSystems
            // 
            this.cboRenderSystems.DropDownStyle = ComboBoxStyle.DropDownList;
            this.cboRenderSystems.Font = new Font("Palatino Linotype", 8F, FontStyle.Regular, GraphicsUnit.Point, ((Byte)(0)));
            this.cboRenderSystems.ForeColor = Color.FromArgb(((Byte)(25)), ((Byte)(35)), ((Byte)(75)));
            this.cboRenderSystems.Location = new Point(144, 32);
            this.cboRenderSystems.Name = "cboRenderSystems";
            this.cboRenderSystems.Size = new Size(176, 24);
            this.cboRenderSystems.TabIndex = 8;
            this.cboRenderSystems.SelectedIndexChanged += new EventHandler(RenderSystems_SelectedIndexChanged);
            // 
            // cboResolution
            // 
            this.cboResolution.DropDownStyle = ComboBoxStyle.DropDownList;
            this.cboResolution.Font = new Font("Palatino Linotype", 8F, FontStyle.Regular, GraphicsUnit.Point, ((Byte)(0)));
            this.cboResolution.ForeColor = Color.FromArgb(((Byte)(25)), ((Byte)(35)), ((Byte)(75)));
            this.cboResolution.Location = new Point(144, 64);
            this.cboResolution.Name = "cboResolution";
            this.cboResolution.Size = new Size(176, 24);
            this.cboResolution.TabIndex = 6;
            // 
            // ConfigDialog
            // 
            this.AcceptButton = this.btnOk;
            this.AutoScaleBaseSize = new Size(6, 17);
            this.BackColor = Color.White;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new Size(442, 344);
            this.ControlBox = false;
            this.Controls.Add(this.grpVideoOptions);
            this.Controls.Add(this.picLogo);
            this.Controls.Add(this.btnOk);
            this.Controls.Add(this.btnCancel);
            this.Font = new Font("Palatino Linotype", 9F, FontStyle.Bold, GraphicsUnit.Point, ((Byte)(0)));
            this.FormBorderStyle = FormBorderStyle.FixedToolWindow;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ConfigDialog";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Text = "Axiom Game Engine";
            this.Load += new EventHandler(this.ConfigDialog_Load);
            this.grpVideoOptions.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        private void Ok_Click(object sender, EventArgs e) {
            Engine.Instance.RenderSystem = (RenderSystem) cboRenderSystems.SelectedItem;
            EngineConfig.DisplayModeRow mode = Engine.Instance.RenderSystem.ConfigOptions.DisplayMode[cboResolution.SelectedIndex];
            mode.FullScreen = chkFullscreen.Checked;
            mode.Selected = true;
            SaveDisplaySettings(cboRenderSystems.SelectedIndex.ToString(), cboResolution.SelectedIndex.ToString(), chkFullscreen.Checked.ToString());
            this.Close();
        }

        private void Cancel_Click(object sender, EventArgs e) {
            this.Dispose();
        }

        private void ConfigDialog_Load(object sender, System.EventArgs e) {
            foreach(RenderSystem renderSystem in Engine.Instance.RenderSystems) {
                cboRenderSystems.Items.Add(renderSystem);
            }

            XmlTextReader settingsReader = null;

            try {
                string temp = string.Empty;
                
                settingsReader = new XmlTextReader("DisplayConfig.xml");

                while(settingsReader.Read()) {
                    if(settingsReader.NodeType == XmlNodeType.Element) {
                        if(settingsReader.LocalName.Equals("RenderSystem")) {
                            temp = settingsReader.ReadString();
                            if(cboRenderSystems.Items.Count > int.Parse(temp)) {
                                cboRenderSystems.SelectedIndex = int.Parse(temp);
                            }
                            else {
                                cboRenderSystems.SelectedIndex = 1;
                            }
                        }

                        if(settingsReader.LocalName.Equals("Resolution")) {
                            temp = settingsReader.ReadString();
                            if(cboResolution.Items.Count > int.Parse(temp)) {
                                cboResolution.SelectedIndex = int.Parse(temp);
                            }
                            else {
                                cboResolution.SelectedIndex = (cboResolution.Items.Count - 1);
                            }
                        }

                        if(settingsReader.LocalName.Equals("FullScreen")) {
                            if(settingsReader.ReadString()== "True") {
                                chkFullscreen.Checked = true;
                            }
                            else {
                                chkFullscreen.Checked = false;
                            }
                        }
                    }
                }
            }
            catch(System.IO.FileNotFoundException) {
                // HACK: Trying to force Tao.OpenGl to be listed first.
                if(cboRenderSystems.Items.Count > 1) {
                    cboRenderSystems.SelectedIndex = 1;
                }
                else {
                    cboRenderSystems.SelectedIndex = 0;
                }

                cboResolution.SelectedIndex = 0;
            }
            finally {
                if(settingsReader != null) {
                    settingsReader.Close();
                }
            }
        }

        private void RenderSystems_SelectedIndexChanged(object sender, EventArgs e) {
            cboResolution.Items.Clear();
            RenderSystem system = (RenderSystem) cboRenderSystems.SelectedItem;

            foreach(EngineConfig.DisplayModeRow mode in system.ConfigOptions.DisplayMode)
                cboResolution.Items.Add(string.Format("{0} x {1} @ {2}bpp", mode.Width, mode.Height, mode.Bpp));

            cboResolution.SelectedIndex = 0;
        }

        public void SaveDisplaySettings(string renderSystem, string resolution, string fullScreen ) {
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
