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
using System.Xml;
using System.Windows.Forms;
using Axiom.Configuration;
using Axiom.Core;
using Axiom.Exceptions;
using Axiom.SubSystems.Rendering;

namespace Axiom.Utility {
    /// <summary>
    /// Summary description for ConfigDialog.
    /// </summary>
    public class ConfigDialog : System.Windows.Forms.Form {
        private System.Windows.Forms.ComboBox cboRenderSystems;
        private System.Windows.Forms.ComboBox cboResolution;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.CheckBox chkFullScreen;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Label lblResolutions;
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.Container components = null;

        public ConfigDialog() {
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
            System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(ConfigDialog));
            this.lblResolutions = new System.Windows.Forms.Label();
            this.btnCancel = new System.Windows.Forms.Button();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.chkFullScreen = new System.Windows.Forms.CheckBox();
            this.btnOK = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.label1 = new System.Windows.Forms.Label();
            this.cboRenderSystems = new System.Windows.Forms.ComboBox();
            this.cboResolution = new System.Windows.Forms.ComboBox();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // lblResolutions
            // 
            this.lblResolutions.ForeColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.lblResolutions.Location = new System.Drawing.Point(16, 70);
            this.lblResolutions.Name = "lblResolutions";
            this.lblResolutions.Size = new System.Drawing.Size(128, 16);
            this.lblResolutions.TabIndex = 7;
            this.lblResolutions.Text = "Resolution:";
            this.lblResolutions.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // btnCancel
            // 
            this.btnCancel.BackColor = System.Drawing.SystemColors.Control;
            this.btnCancel.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCancel.Font = new System.Drawing.Font("Palatino Linotype", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
            this.btnCancel.ForeColor = System.Drawing.Color.Black;
            this.btnCancel.Location = new System.Drawing.Point(237, 296);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(96, 24);
            this.btnCancel.TabIndex = 1;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // pictureBox1
            // 
            this.pictureBox1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pictureBox1.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox1.Image")));
            this.pictureBox1.Location = new System.Drawing.Point(126, 15);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(200, 80);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
            this.pictureBox1.TabIndex = 3;
            this.pictureBox1.TabStop = false;
            // 
            // chkFullScreen
            // 
            this.chkFullScreen.BackColor = System.Drawing.SystemColors.Control;
            this.chkFullScreen.Cursor = System.Windows.Forms.Cursors.Hand;
            this.chkFullScreen.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.chkFullScreen.Font = new System.Drawing.Font("Palatino Linotype", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
            this.chkFullScreen.ForeColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.chkFullScreen.Location = new System.Drawing.Point(143, 102);
            this.chkFullScreen.Name = "chkFullScreen";
            this.chkFullScreen.Size = new System.Drawing.Size(144, 24);
            this.chkFullScreen.TabIndex = 0;
            this.chkFullScreen.Text = "Fullscreen?";
            // 
            // btnOK
            // 
            this.btnOK.BackColor = System.Drawing.SystemColors.Control;
            this.btnOK.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnOK.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnOK.Font = new System.Drawing.Font("Palatino Linotype", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
            this.btnOK.ForeColor = System.Drawing.Color.Black;
            this.btnOK.Location = new System.Drawing.Point(109, 296);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(96, 24);
            this.btnOK.TabIndex = 0;
            this.btnOK.Text = "Start";
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.cboRenderSystems);
            this.groupBox1.Controls.Add(this.lblResolutions);
            this.groupBox1.Controls.Add(this.cboResolution);
            this.groupBox1.Controls.Add(this.chkFullScreen);
            this.groupBox1.Font = new System.Drawing.Font("Palatino Linotype", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
            this.groupBox1.ForeColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.groupBox1.Location = new System.Drawing.Point(24, 136);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(392, 144);
            this.groupBox1.TabIndex = 6;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Video Options";
            // 
            // label1
            // 
            this.label1.ForeColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.label1.Location = new System.Drawing.Point(71, 37);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(72, 16);
            this.label1.TabIndex = 9;
            this.label1.Text = "Driver:";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // cboRenderSystems
            // 
            this.cboRenderSystems.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboRenderSystems.Font = new System.Drawing.Font("Palatino Linotype", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
            this.cboRenderSystems.Location = new System.Drawing.Point(144, 32);
            this.cboRenderSystems.Name = "cboRenderSystems";
            this.cboRenderSystems.Size = new System.Drawing.Size(176, 24);
            this.cboRenderSystems.TabIndex = 8;
            this.cboRenderSystems.SelectedIndexChanged += new System.EventHandler(this.cboRenderSystems_SelectedIndexChanged);
            // 
            // cboResolution
            // 
            this.cboResolution.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboResolution.Font = new System.Drawing.Font("Palatino Linotype", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
            this.cboResolution.Location = new System.Drawing.Point(144, 64);
            this.cboResolution.Name = "cboResolution";
            this.cboResolution.Size = new System.Drawing.Size(176, 24);
            this.cboResolution.TabIndex = 6;
            // 
            // ConfigDialog
            // 
            this.AcceptButton = this.btnOK;
            this.AutoScaleBaseSize = new System.Drawing.Size(6, 17);
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(442, 344);
            this.ControlBox = false;
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.pictureBox1);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.btnCancel);
            this.Font = new System.Drawing.Font("Palatino Linotype", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ConfigDialog";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Axiom Game Engine";
            this.Load += new System.EventHandler(this.ConfigDialog_Load);
            this.groupBox1.ResumeLayout(false);
            this.ResumeLayout(false);

        }
        #endregion

        private void btnOK_Click(object sender, System.EventArgs e) { 
            Engine.Instance.RenderSystem = (RenderSystem)cboRenderSystems.SelectedItem; 

            EngineConfig.DisplayModeRow mode = 
                Engine.Instance.RenderSystem.ConfigOptions.DisplayMode 
                [cboResolution.SelectedIndex]; 

            mode.FullScreen = chkFullScreen.Checked; 
            mode.Selected = true; 

            // Hobbes: Save those settings for next time! 
            SaveDisplaySettings(cboRenderSystems.SelectedIndex.ToString() , cboResolution.SelectedIndex.ToString(), chkFullScreen.Checked.ToString()); 
            // END Hobbes          
            this.Close(); 
        } 

        private void btnCancel_Click(object sender, System.EventArgs e) { 
            this.Dispose(); 
        } 

        private void ConfigDialog_Load(object sender, System.EventArgs e) { 
            string temp; 
            // add the available render systems to the driver dropdown 
            foreach(RenderSystem renderSystem in Engine.Instance.RenderSystems) { 
                cboRenderSystems.Items.Add(renderSystem); 
            } 
    
            // Hobbes: Check for a config file and load settings 
            try { 
                XmlTextReader settingsReader = new XmlTextReader("DisplayConfig.xml"); 
          
                while(settingsReader.Read()) { 
                    if(settingsReader.NodeType == XmlNodeType.Element) { 
                        if(settingsReader.LocalName.Equals("RenderSystem")) { 
                            temp = settingsReader.ReadString(); 
                            if(cboRenderSystems.Items.Count > int.Parse(temp)) 
                                cboRenderSystems.SelectedIndex = int.Parse(temp); 
                            else 
                                cboRenderSystems.SelectedIndex = 1; 
                        } 
                      
                        if(settingsReader.LocalName.Equals("Resolution")) { 
                            temp = settingsReader.ReadString(); 
                            if(cboResolution.Items.Count > int.Parse(temp)) 
                                cboResolution.SelectedIndex = int.Parse(temp); 
                            else 
                                cboResolution.SelectedIndex = (cboResolution.Items.Count - 1); 
                        } 
                      
                        if(settingsReader.LocalName.Equals("FullScreen")) { 
                            if(settingsReader.ReadString()== "True") 
                                chkFullScreen.Checked = true; 
                            else 
                                chkFullScreen.Checked = false; 
                        } 
                    } 
                } 
            } 
            catch { 
                // If the DisplayConfig.xml file is missing, or corrupt, hack it. 
                // HACK: forcing OpenGL as default, since it kicks ass <g/>. 
                if(cboRenderSystems.Items.Count > 1) 
                    cboRenderSystems.SelectedIndex = 1; 
                else 
                    cboRenderSystems.SelectedIndex = 0; 
               
                // Hobbes: HACK: Forcing highest resolution 
                if(cboResolution.Items.Count > 1) 
                    cboResolution.SelectedIndex = (cboResolution.Items.Count - 1); 
                else 
                    cboResolution.SelectedIndex = 0; 
            } 
        } 
        // END Hobbes 

        private void cboRenderSystems_SelectedIndexChanged(object sender, System.EventArgs e) {
            cboResolution.Items.Clear();
				
            RenderSystem system = (RenderSystem)cboRenderSystems.SelectedItem;

            foreach(EngineConfig.DisplayModeRow mode in system.ConfigOptions.DisplayMode)
                cboResolution.Items.Add(String.Format("{0} x {1} @{2}bpp", mode.Width, mode.Height, mode.Bpp));

            cboResolution.SelectedIndex = 0;
        }

        // Hobbes: Save the Render System, Resolution and Full Screen options 
        public void SaveDisplaySettings(string renderSystem, string resolution, string fullScreen ) { 
            XmlTextWriter settingsWriter = new XmlTextWriter("DisplayConfig.xml", null); 
          
            try { 
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
             
                settingsWriter.Flush(); 
            } 
            finally { 
                if(settingsWriter != null) { 
                    settingsWriter.Close(); 
                } 
            } 
        }

    }
}
