#region MIT/X11 License

//Copyright © 2003-2012 Axiom 3D Rendering Engine Project
//
//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:
//
//The above copyright notice and this permission notice shall be included in
//all copies or substantial portions of the Software.
//
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//THE SOFTWARE.

#endregion License

using System;
using System.ComponentModel;
using Axiom.Configuration;
using Axiom.Core;
using SWF = System.Windows.Forms;


namespace Axiom.Framework.Configuration
{
	/// <summary>
	/// 
	/// </summary>
	public class WinFormConfigurationDialog : SWF.Form, SWF.IMessageFilter, IConfigurationDialog
	{
		#region Fields and Properties

		protected Container components = null;
		protected SWF.PictureBox picLogo;
		protected SWF.GroupBox grpVideoOptions;
		protected SWF.Label lblRenderer;
		protected SWF.Label lblOption;
		protected SWF.ComboBox cboOptionValues;
		protected SWF.ListBox lstOptions;
		protected SWF.Button cmdCancel;
		protected SWF.Button cmdOk;
		protected SWF.Panel pnlBackground;
		protected SWF.ComboBox cboRenderSystems;

		private const string _logoResourceNameDefault = "AxiomLogo.png";
		public string LogoResourceName { get; set; }

		private const string _iconResourceNameDefault = "AxiomIcon.ico";
		public string IconResourceName { get; set; }

		public Root Engine { get; set; }

		public ResourceGroupManager ResourceManager { get; set; }

		#endregion Fields and Properties

		public WinFormConfigurationDialog( Root engine, ResourceGroupManager resourceManager )
		{
			Engine = engine;
			ResourceManager = resourceManager;

			SetStyle( SWF.ControlStyles.DoubleBuffer, true );
			InitializeComponent();

			LogoResourceName = _logoResourceNameDefault;
			IconResourceName = _iconResourceNameDefault;
		}

		private void InitializeComponent()
		{
			var resources = new System.ComponentModel.ComponentResourceManager( typeof ( WinFormConfigurationDialog ) );
			picLogo = new System.Windows.Forms.PictureBox();
			grpVideoOptions = new System.Windows.Forms.GroupBox();
			lblOption = new System.Windows.Forms.Label();
			cboOptionValues = new System.Windows.Forms.ComboBox();
			lstOptions = new System.Windows.Forms.ListBox();
			lblRenderer = new System.Windows.Forms.Label();
			cboRenderSystems = new System.Windows.Forms.ComboBox();
			cmdCancel = new System.Windows.Forms.Button();
			cmdOk = new System.Windows.Forms.Button();
			pnlBackground = new System.Windows.Forms.Panel();
			( (System.ComponentModel.ISupportInitialize)( picLogo ) ).BeginInit();
			grpVideoOptions.SuspendLayout();
			SuspendLayout();
			// 
			// picLogo
			// 
			picLogo.BackColor = System.Drawing.Color.White;
			picLogo.Image = global::Axiom.Framework.Properties.Resources.AxiomLogo;
			picLogo.Location = new System.Drawing.Point( 12, 3 );
			picLogo.Name = "picLogo";
			picLogo.Size = new System.Drawing.Size( 420, 174 );
			picLogo.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
			picLogo.TabIndex = 3;
			picLogo.TabStop = false;
			// 
			// grpVideoOptions
			// 
			grpVideoOptions.Controls.Add( lblOption );
			grpVideoOptions.Controls.Add( cboOptionValues );
			grpVideoOptions.Controls.Add( lstOptions );
			grpVideoOptions.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			grpVideoOptions.Font = new System.Drawing.Font( "Tahoma", 9F, System.Drawing.FontStyle.Regular,
			                                                System.Drawing.GraphicsUnit.Point, ( (byte)( 0 ) ) );
			grpVideoOptions.ForeColor = System.Drawing.Color.FromArgb( ( (int)( ( (byte)( 25 ) ) ) ),
			                                                           ( (int)( ( (byte)( 35 ) ) ) ),
			                                                           ( (int)( ( (byte)( 75 ) ) ) ) );
			grpVideoOptions.Location = new System.Drawing.Point( 12, 215 );
			grpVideoOptions.Name = "grpVideoOptions";
			grpVideoOptions.Size = new System.Drawing.Size( 420, 187 );
			grpVideoOptions.TabIndex = 6;
			grpVideoOptions.TabStop = false;
			grpVideoOptions.Text = "Rendering System Options";
			// 
			// lblOption
			// 
			lblOption.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			lblOption.Font = new System.Drawing.Font( "Tahoma", 9F, System.Drawing.FontStyle.Regular,
			                                          System.Drawing.GraphicsUnit.Point, ( (byte)( 0 ) ) );
			lblOption.ForeColor = System.Drawing.Color.FromArgb( ( (int)( ( (byte)( 25 ) ) ) ), ( (int)( ( (byte)( 35 ) ) ) ),
			                                                     ( (int)( ( (byte)( 75 ) ) ) ) );
			lblOption.Location = new System.Drawing.Point( 32, 153 );
			lblOption.Name = "lblOption";
			lblOption.Size = new System.Drawing.Size( 200, 22 );
			lblOption.TabIndex = 9;
			lblOption.Text = "Option Name:";
			lblOption.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			lblOption.Visible = false;
			// 
			// cboOptionValues
			// 
			cboOptionValues.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			cboOptionValues.Font = new System.Drawing.Font( "Tahoma", 9F, System.Drawing.FontStyle.Regular,
			                                                System.Drawing.GraphicsUnit.Point, ( (byte)( 0 ) ) );
			cboOptionValues.ForeColor = System.Drawing.Color.FromArgb( ( (int)( ( (byte)( 25 ) ) ) ),
			                                                           ( (int)( ( (byte)( 35 ) ) ) ),
			                                                           ( (int)( ( (byte)( 75 ) ) ) ) );
			cboOptionValues.Location = new System.Drawing.Point( 238, 158 );
			cboOptionValues.Name = "cboOptionValues";
			cboOptionValues.Size = new System.Drawing.Size( 176, 22 );
			cboOptionValues.TabIndex = 8;
			cboOptionValues.Visible = false;
			cboOptionValues.SelectedIndexChanged += new System.EventHandler( cboOptionValues_SelectedIndexChanged );
			// 
			// lstOptions
			// 
			lstOptions.ItemHeight = 14;
			lstOptions.Location = new System.Drawing.Point( 7, 22 );
			lstOptions.Name = "lstOptions";
			lstOptions.Size = new System.Drawing.Size( 407, 130 );
			lstOptions.TabIndex = 0;
			lstOptions.SelectedIndexChanged += new System.EventHandler( lstOptions_SelectedIndexChanged );
			// 
			// lblRenderer
			// 
			lblRenderer.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			lblRenderer.Font = new System.Drawing.Font( "Tahoma", 9F, System.Drawing.FontStyle.Regular,
			                                            System.Drawing.GraphicsUnit.Point, ( (byte)( 0 ) ) );
			lblRenderer.ForeColor = System.Drawing.Color.FromArgb( ( (int)( ( (byte)( 25 ) ) ) ), ( (int)( ( (byte)( 35 ) ) ) ),
			                                                       ( (int)( ( (byte)( 75 ) ) ) ) );
			lblRenderer.Location = new System.Drawing.Point( 10, 185 );
			lblRenderer.Name = "lblRenderer";
			lblRenderer.Size = new System.Drawing.Size( 128, 24 );
			lblRenderer.TabIndex = 9;
			lblRenderer.Text = "Rendering Subsystem:";
			lblRenderer.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			// 
			// cboRenderSystems
			// 
			cboRenderSystems.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			cboRenderSystems.Font = new System.Drawing.Font( "Tahoma", 9F, System.Drawing.FontStyle.Regular,
			                                                 System.Drawing.GraphicsUnit.Point, ( (byte)( 0 ) ) );
			cboRenderSystems.ForeColor = System.Drawing.Color.FromArgb( ( (int)( ( (byte)( 25 ) ) ) ),
			                                                            ( (int)( ( (byte)( 35 ) ) ) ),
			                                                            ( (int)( ( (byte)( 75 ) ) ) ) );
			cboRenderSystems.Location = new System.Drawing.Point( 145, 185 );
			cboRenderSystems.Name = "cboRenderSystems";
			cboRenderSystems.Size = new System.Drawing.Size( 285, 22 );
			cboRenderSystems.TabIndex = 8;
			cboRenderSystems.SelectedIndexChanged += new System.EventHandler( RenderSystems_SelectedIndexChanged );
			// 
			// cmdCancel
			// 
			cmdCancel.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
			cmdCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			cmdCancel.Location = new System.Drawing.Point( 355, 408 );
			cmdCancel.Name = "cmdCancel";
			cmdCancel.Size = new System.Drawing.Size( 75, 23 );
			cmdCancel.TabIndex = 10;
			cmdCancel.Text = "Cancel";
			cmdCancel.Click += new System.EventHandler( cmdCancel_Click );
			// 
			// cmdOk
			// 
			cmdOk.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
			cmdOk.DialogResult = System.Windows.Forms.DialogResult.OK;
			cmdOk.Location = new System.Drawing.Point( 261, 408 );
			cmdOk.Name = "cmdOk";
			cmdOk.Size = new System.Drawing.Size( 75, 23 );
			cmdOk.TabIndex = 11;
			cmdOk.Text = "Ok";
			cmdOk.Click += new System.EventHandler( cmdOk_Click );
			// 
			// pnlBackground
			// 
			pnlBackground.BackColor = System.Drawing.Color.White;
			pnlBackground.Location = new System.Drawing.Point( -2, 3 );
			pnlBackground.Name = "pnlBackground";
			pnlBackground.Size = new System.Drawing.Size( 446, 174 );
			pnlBackground.TabIndex = 12;
			// 
			// WinFormConfigurationDialog
			// 
			//SWF.Application.AddMessageFilter( this );
			ClientSize = new System.Drawing.Size( 442, 436 );
			ControlBox = false;
			Controls.Add( cmdOk );
			Controls.Add( cmdCancel );
			Controls.Add( lblRenderer );
			Controls.Add( grpVideoOptions );
			Controls.Add( cboRenderSystems );
			Controls.Add( picLogo );
			Controls.Add( pnlBackground );
			Font = new System.Drawing.Font( "Tahoma", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point,
			                                ( (byte)( 0 ) ) );
			FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
			MaximizeBox = false;
			MinimizeBox = false;
			Name = "WinFormConfigurationDialog";
			StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			Text = "Axiom Rendering Engine Setup";
			Load += new System.EventHandler( WinFormConfigurationDialog_Load );
			( (System.ComponentModel.ISupportInitialize)( picLogo ) ).EndInit();
			grpVideoOptions.ResumeLayout( false );
			ResumeLayout( false );
		}

		#region Event Handlers

		protected void cmdOk_Click( object sender, EventArgs e )
		{
			if ( cboRenderSystems.SelectedItem == null )
			{
				SWF.MessageBox.Show( "Please select a rendering system.", "Axiom", SWF.MessageBoxButtons.OK,
				                     SWF.MessageBoxIcon.Exclamation );
				DialogResult = SWF.DialogResult.Cancel;
				return;
			}

			var system = (Axiom.Graphics.RenderSystem)cboRenderSystems.SelectedItem;

			string errorMsg = system.ValidateConfigOptions();
			if ( !String.IsNullOrEmpty( errorMsg ) )
			{
				SWF.MessageBox.Show( errorMsg, "Axiom", SWF.MessageBoxButtons.OK, SWF.MessageBoxIcon.Exclamation );
				DialogResult = SWF.DialogResult.Cancel;
				return;
			}

			Engine.RenderSystem = system;
			DialogResult = SWF.DialogResult.OK;
			Close();
		}

		protected void cmdCancel_Click( object sender, EventArgs e )
		{
			DialogResult = SWF.DialogResult.Cancel;
			Dispose();
		}

		private void WinFormConfigurationDialog_Load( object sender, EventArgs e )
		{
			foreach ( Axiom.Graphics.RenderSystem renderSystem in Engine.RenderSystems )
			{
				cboRenderSystems.Items.Add( renderSystem );
			}

			// Set the default if it's already configured
			if ( Engine.RenderSystem != null )
			{
				if ( cboRenderSystems.Items.Contains( Engine.RenderSystem ) )
				{
					cboRenderSystems.SelectedItem = Engine.RenderSystem;
				}
			}

			// Register [Enter] and [Esc] keys for Default buttons
			//SWF.Application.AddMessageFilter( this );
			cmdOk.NotifyDefault( true );
		}

		private void RenderSystems_SelectedIndexChanged( object sender, EventArgs e )
		{
			lstOptions.Items.Clear();
			cboOptionValues.Items.Clear();
			var system = (Axiom.Graphics.RenderSystem)cboRenderSystems.SelectedItem;
			ConfigOption optVideoMode;

			// Load Render Subsystem Options
			foreach ( ConfigOption option in system.ConfigOptions.Values )
			{
				lstOptions.Items.Add( option );
			}
		}

		private void lstOptions_SelectedIndexChanged( object sender, EventArgs e )
		{
			cboOptionValues.SelectedIndexChanged -= new System.EventHandler( cboOptionValues_SelectedIndexChanged );

			var system = (Axiom.Graphics.RenderSystem)cboRenderSystems.SelectedItem;
			var opt = (ConfigOption)lstOptions.SelectedItem;

			cboOptionValues.Items.Clear();
			foreach ( string value in opt.PossibleValues.Values )
			{
				cboOptionValues.Items.Add( value );
			}

			if ( cboOptionValues.Items.Count == 0 )
			{
				cboOptionValues.Items.Add( opt.Value );
			}
			cboOptionValues.SelectedIndex = cboOptionValues.Items.IndexOf( opt.Value );

			lblOption.Text = opt.Name;
			lblOption.Visible = true;
			cboOptionValues.Visible = true;
			cboOptionValues.Enabled = ( !opt.Immutable );

			cboOptionValues.SelectedIndexChanged += new System.EventHandler( cboOptionValues_SelectedIndexChanged );
		}

		private void cboOptionValues_SelectedIndexChanged( object sender, EventArgs e )
		{
			var opt = (ConfigOption)lstOptions.SelectedItem;
			var value = (string)cboOptionValues.SelectedItem;

			opt.Value = value;

			lstOptions.SelectedIndexChanged -= new System.EventHandler( lstOptions_SelectedIndexChanged );
			for ( int index = 0; index < lstOptions.Items.Count; index++ )
			{
				lstOptions.Items[ index ] = lstOptions.Items[ index ];
			}
			lstOptions.SelectedIndexChanged += new System.EventHandler( lstOptions_SelectedIndexChanged );
		}

		#endregion Event Handlers

		#region IConfigurationDialog Implementation

		public Axiom.Graphics.RenderSystem RenderSystem
		{
			get
			{
				return cboRenderSystems.SelectedItem as Axiom.Graphics.RenderSystem;
			}
		}

		public DialogResult Show()
		{
			cmdOk.Select();
			return ShowDialog() == SWF.DialogResult.OK ? Configuration.DialogResult.Ok : Configuration.DialogResult.Cancel;
		}

		#endregion IConfigurationDialog Implementation

		#region Implementation of IMessageFilter

		/// <summary>
		/// Filters out a message before it is dispatched.
		/// </summary>
		/// <returns>
		/// true to filter the message and stop it from being dispatched; false to allow the message to continue to the next filter or control.
		/// </returns>
		/// <param name="msg">The message to be dispatched. You cannot modify this message. 
		/// </param><filterpriority>1</filterpriority>
		bool SWF.IMessageFilter.PreFilterMessage( ref SWF.Message msg )
		{
			const int WM_KEYDOWN = 0x100;

			SWF.Keys keyCode = (SWF.Keys)(int)msg.WParam & SWF.Keys.KeyCode;
			if ( msg.Msg == WM_KEYDOWN && keyCode == SWF.Keys.Return )
			{
				cmdOk_Click( this, null );
				return true;
			}
			if ( msg.Msg == WM_KEYDOWN && keyCode == SWF.Keys.Escape )
			{
				cmdCancel_Click( this, null );
				return true;
			}
			return false;
		}

		#endregion
	}
}