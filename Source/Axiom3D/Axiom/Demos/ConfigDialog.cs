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

#region Namespace Declarations

using System;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using SWF = System.Windows.Forms;
using System.Xml;

using Axiom;
using Axiom.Core;

#endregion Namespace Declarations

namespace Axiom.Demos
{
    public class ConfigDialog : Form
    {
        private Container components = null;
        private PictureBox picLogo;
        private GroupBox grpVideoOptions;
        private Label lblRenderer;
        private Label lblOption;
        private ComboBox cboOptionValues;
        private ListBox lstOptions;
        private Button cmdCancel;
        private Button cmdOk;
        private SWF.Panel pnlBackground;
        private ComboBox cboRenderSystems;

        public ConfigDialog()
        {
            this.SetStyle( ControlStyles.DoubleBuffer, true );
            InitializeComponent();

            try
            {
                Stream image = ResourceManager.FindCommonResourceData( "AxiomLogoSmall.png" );
                Stream icon = ResourceManager.FindCommonResourceData( "AxiomIcon.ico" );

                if ( image != null )
                {
                    this.picLogo.Image = System.Drawing.Image.FromStream( image, true );
                }

                if ( icon != null )
                {
                    this.Icon = new Icon( icon );
                }
            }
            catch ( Exception )
            {
            }
        }

        protected override void Dispose( bool disposing )
        {
            if ( disposing )
            {
                if ( components != null )
                {
                    components.Dispose();
                }
            }
            base.Dispose( disposing );
        }

        private void InitializeComponent()
        {
            this.picLogo = new System.Windows.Forms.PictureBox();
            this.grpVideoOptions = new System.Windows.Forms.GroupBox();
            this.lblOption = new System.Windows.Forms.Label();
            this.cboOptionValues = new System.Windows.Forms.ComboBox();
            this.lstOptions = new System.Windows.Forms.ListBox();
            this.lblRenderer = new System.Windows.Forms.Label();
            this.cboRenderSystems = new System.Windows.Forms.ComboBox();
            this.cmdCancel = new System.Windows.Forms.Button();
            this.cmdOk = new System.Windows.Forms.Button();
            this.pnlBackground = new System.Windows.Forms.Panel();
            ( (System.ComponentModel.ISupportInitialize)( this.picLogo ) ).BeginInit();
            this.grpVideoOptions.SuspendLayout();
            this.SuspendLayout();
            // 
            // picLogo
            // 
            this.picLogo.BackColor = System.Drawing.Color.White;
            this.picLogo.Location = new System.Drawing.Point( 12, 3 );
            this.picLogo.Name = "picLogo";
            this.picLogo.Size = new System.Drawing.Size( 420, 174 );
            this.picLogo.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.picLogo.TabIndex = 3;
            this.picLogo.TabStop = false;
            // 
            // grpVideoOptions
            // 
            this.grpVideoOptions.Controls.Add( this.lblOption );
            this.grpVideoOptions.Controls.Add( this.cboOptionValues );
            this.grpVideoOptions.Controls.Add( this.lstOptions );
            this.grpVideoOptions.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.grpVideoOptions.Font = new System.Drawing.Font( "Tahoma", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ( (byte)( 0 ) ) );
            this.grpVideoOptions.ForeColor = System.Drawing.Color.FromArgb( ( (int)( ( (byte)( 25 ) ) ) ), ( (int)( ( (byte)( 35 ) ) ) ), ( (int)( ( (byte)( 75 ) ) ) ) );
            this.grpVideoOptions.Location = new System.Drawing.Point( 12, 215 );
            this.grpVideoOptions.Name = "grpVideoOptions";
            this.grpVideoOptions.Size = new System.Drawing.Size( 420, 187 );
            this.grpVideoOptions.TabIndex = 6;
            this.grpVideoOptions.TabStop = false;
            this.grpVideoOptions.Text = "Rendering System Options";
            // 
            // lblOption
            // 
            this.lblOption.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.lblOption.Font = new System.Drawing.Font( "Tahoma", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ( (byte)( 0 ) ) );
            this.lblOption.ForeColor = System.Drawing.Color.FromArgb( ( (int)( ( (byte)( 25 ) ) ) ), ( (int)( ( (byte)( 35 ) ) ) ), ( (int)( ( (byte)( 75 ) ) ) ) );
            this.lblOption.Location = new System.Drawing.Point( 104, 160 );
            this.lblOption.Name = "lblOption";
            this.lblOption.Size = new System.Drawing.Size( 128, 22 );
            this.lblOption.TabIndex = 9;
            this.lblOption.Text = "Option Name:";
            this.lblOption.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.lblOption.Visible = false;
            // 
            // cboOptionValues
            // 
            this.cboOptionValues.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboOptionValues.Font = new System.Drawing.Font( "Palatino Linotype", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ( (byte)( 0 ) ) );
            this.cboOptionValues.ForeColor = System.Drawing.Color.FromArgb( ( (int)( ( (byte)( 25 ) ) ) ), ( (int)( ( (byte)( 35 ) ) ) ), ( (int)( ( (byte)( 75 ) ) ) ) );
            this.cboOptionValues.Location = new System.Drawing.Point( 238, 158 );
            this.cboOptionValues.Name = "cboOptionValues";
            this.cboOptionValues.Size = new System.Drawing.Size( 176, 24 );
            this.cboOptionValues.TabIndex = 8;
            this.cboOptionValues.Visible = false;
            this.cboOptionValues.SelectedIndexChanged += new System.EventHandler( this.cboOptionValues_SelectedIndexChanged );
            // 
            // lstOptions
            // 
            this.lstOptions.ItemHeight = 14;
            this.lstOptions.Location = new System.Drawing.Point( 7, 22 );
            this.lstOptions.Name = "lstOptions";
            this.lstOptions.Size = new System.Drawing.Size( 407, 130 );
            this.lstOptions.TabIndex = 0;
            this.lstOptions.SelectedIndexChanged += new System.EventHandler( this.lstOptions_SelectedIndexChanged );
            // 
            // lblRenderer
            // 
            this.lblRenderer.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.lblRenderer.Font = new System.Drawing.Font( "Tahoma", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ( (byte)( 0 ) ) );
            this.lblRenderer.ForeColor = System.Drawing.Color.FromArgb( ( (int)( ( (byte)( 25 ) ) ) ), ( (int)( ( (byte)( 35 ) ) ) ), ( (int)( ( (byte)( 75 ) ) ) ) );
            this.lblRenderer.Location = new System.Drawing.Point( 10, 185 );
            this.lblRenderer.Name = "lblRenderer";
            this.lblRenderer.Size = new System.Drawing.Size( 128, 24 );
            this.lblRenderer.TabIndex = 9;
            this.lblRenderer.Text = "Rendering Subsystem:";
            this.lblRenderer.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // cboRenderSystems
            // 
            this.cboRenderSystems.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboRenderSystems.Font = new System.Drawing.Font( "Palatino Linotype", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ( (byte)( 0 ) ) );
            this.cboRenderSystems.ForeColor = System.Drawing.Color.FromArgb( ( (int)( ( (byte)( 25 ) ) ) ), ( (int)( ( (byte)( 35 ) ) ) ), ( (int)( ( (byte)( 75 ) ) ) ) );
            this.cboRenderSystems.Location = new System.Drawing.Point( 145, 185 );
            this.cboRenderSystems.Name = "cboRenderSystems";
            this.cboRenderSystems.Size = new System.Drawing.Size( 285, 24 );
            this.cboRenderSystems.TabIndex = 8;
            this.cboRenderSystems.SelectedIndexChanged += new System.EventHandler( this.RenderSystems_SelectedIndexChanged );
            // 
            // cmdCancel
            // 
            this.cmdCancel.Location = new System.Drawing.Point( 355, 408 );
            this.cmdCancel.Name = "cmdCancel";
            this.cmdCancel.Size = new System.Drawing.Size( 75, 23 );
            this.cmdCancel.TabIndex = 10;
            this.cmdCancel.Text = "Cancel";
            this.cmdCancel.Click += new System.EventHandler( this.cmdCancel_Click );
            // 
            // cmdOk
            // 
            this.cmdOk.Location = new System.Drawing.Point( 261, 408 );
            this.cmdOk.Name = "cmdOk";
            this.cmdOk.Size = new System.Drawing.Size( 75, 23 );
            this.cmdOk.TabIndex = 11;
            this.cmdOk.Text = "Ok";
            this.cmdOk.Click += new System.EventHandler( this.cmdOk_Click );
            // 
            // pnlBackground
            // 
            this.pnlBackground.BackColor = System.Drawing.Color.White;
            this.pnlBackground.Location = new System.Drawing.Point( -2, 3 );
            this.pnlBackground.Name = "pnlBackground";
            this.pnlBackground.Size = new System.Drawing.Size( 446, 174 );
            this.pnlBackground.TabIndex = 12;
            // 
            // ConfigDialog
            // 
            this.ClientSize = new System.Drawing.Size( 442, 436 );
            this.ControlBox = false;
            this.Controls.Add( this.cmdOk );
            this.Controls.Add( this.cmdCancel );
            this.Controls.Add( this.lblRenderer );
            this.Controls.Add( this.grpVideoOptions );
            this.Controls.Add( this.cboRenderSystems );
            this.Controls.Add( this.picLogo );
            this.Controls.Add( this.pnlBackground );
            this.Font = new System.Drawing.Font( "Tahoma", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ( (byte)( 0 ) ) );
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ConfigDialog";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Axiom Rendering Engine Setup";
            this.Load += new System.EventHandler( this.ConfigDialog_Load );
            ( (System.ComponentModel.ISupportInitialize)( this.picLogo ) ).EndInit();
            this.grpVideoOptions.ResumeLayout( false );
            this.ResumeLayout( false );

        }

        private void cmdOk_Click( object sender, EventArgs e )
        {
            Root.Instance.RenderSystem = (RenderSystem)cboRenderSystems.SelectedItem;

            RenderSystem system = Root.Instance.RenderSystem;

            foreach ( ConfigOption opt in lstOptions.Items )
            {
               system.ConfigOptions[ opt.Name ] = opt;
            }

            //TODO: Use ConfigurationSectionHandler to save config out to config file.

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void cmdCancel_Click( object sender, EventArgs e )
        {
            this.DialogResult = DialogResult.Cancel;
            this.Dispose();
        }

        private void ConfigDialog_Load( object sender, EventArgs e )
        {
            foreach ( RenderSystem renderSystem in Root.Instance.RenderSystems )
            {
                cboRenderSystems.Items.Add( renderSystem );
            }

            if ( cboRenderSystems.Items.Count > 0 )
                cboRenderSystems.SelectedIndex = 0;

            //TODO: Read configuration Settings from config file using ConfigurationSectionHandler
        }

        private void RenderSystems_SelectedIndexChanged( object sender, EventArgs e )
        {
            lstOptions.Items.Clear();
            cboOptionValues.Items.Clear();
            RenderSystem system = (RenderSystem)cboRenderSystems.SelectedItem;
            ConfigOption optVideoMode;

            // Load Render Subsystem Options
            foreach ( ConfigOption option in system.ConfigOptions.Values )
            {
                lstOptions.Items.Add( option );
            }

        }

        private void lstOptions_SelectedIndexChanged( object sender, EventArgs e )
        {
     
            RenderSystem system = (RenderSystem)cboRenderSystems.SelectedItem;
            ConfigOption opt = (ConfigOption)lstOptions.SelectedItem;

            cboOptionValues.Items.Clear();
            foreach ( string value in opt.PossibleValues )
                cboOptionValues.Items.Add( value );

            if ( cboOptionValues.Items.Count == 0 )
            {
                cboOptionValues.Items.Add( opt.Value );
            }
            cboOptionValues.SelectedIndex = cboOptionValues.Items.IndexOf( opt.Value );

            this.lblOption.Text = opt.Name;
            this.lblOption.Visible = true;
            this.cboOptionValues.Visible = true;
            this.cboOptionValues.Enabled = ( !opt.Immutable );
        }

        private void cboOptionValues_SelectedIndexChanged( object sender, EventArgs e )
        {
            ConfigOption opt = (ConfigOption)lstOptions.SelectedItem;
            string value = (string)cboOptionValues.SelectedItem;

            opt.Value = value;

            int index = lstOptions.SelectedIndex;
            this.lstOptions.SelectedIndexChanged -= new System.EventHandler( this.lstOptions_SelectedIndexChanged );
            lstOptions.Items[ index ] = opt;
            this.lstOptions.SelectedIndexChanged += new System.EventHandler( this.lstOptions_SelectedIndexChanged );
        }        

    }
}
