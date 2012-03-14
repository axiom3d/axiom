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
using System.Drawing;
using System.Windows.Forms;

using Axiom.Configuration;
using Axiom.Core;
using Axiom.Framework.Properties;

using SWF = System.Windows.Forms;


namespace Axiom.Framework.Configuration
{
	/// <summary>
	/// 
	/// </summary>
	public class WinFormConfigurationDialog : Form, IMessageFilter, IConfigurationDialog
	{
		#region Fields and Properties

		private const string _logoResourceNameDefault = "AxiomLogo.png";
		private const string _iconResourceNameDefault = "AxiomIcon.ico";
		protected ComboBox cboOptionValues;
		protected ComboBox cboRenderSystems;
		protected Button cmdCancel;
		protected Button cmdOk;
		protected Container components;
		protected GroupBox grpVideoOptions;
		protected Label lblOption;
		protected Label lblRenderer;
		protected ListBox lstOptions;
		protected PictureBox picLogo;
		protected Panel pnlBackground;
		public string LogoResourceName { get; set; }

		public string IconResourceName { get; set; }

		public Root Engine { get; set; }

		public ResourceGroupManager ResourceManager { get; set; }

		#endregion Fields and Properties

		public WinFormConfigurationDialog( Root engine, ResourceGroupManager resourceManager )
		{
			Engine = engine;
			ResourceManager = resourceManager;

			SetStyle( ControlStyles.DoubleBuffer, true );
			InitializeComponent();

			LogoResourceName = _logoResourceNameDefault;
			IconResourceName = _iconResourceNameDefault;
		}

		private void InitializeComponent()
		{
			var resources = new ComponentResourceManager( typeof( WinFormConfigurationDialog ) );
			this.picLogo = new PictureBox();
			this.grpVideoOptions = new GroupBox();
			this.lblOption = new Label();
			this.cboOptionValues = new ComboBox();
			this.lstOptions = new ListBox();
			this.lblRenderer = new Label();
			this.cboRenderSystems = new ComboBox();
			this.cmdCancel = new Button();
			this.cmdOk = new Button();
			this.pnlBackground = new Panel();
			( (ISupportInitialize)( this.picLogo ) ).BeginInit();
			this.grpVideoOptions.SuspendLayout();
			SuspendLayout();
			// 
			// picLogo
			// 
			this.picLogo.BackColor = Color.White;
			this.picLogo.Image = Resources.AxiomLogo;
			this.picLogo.Location = new Point( 12, 3 );
			this.picLogo.Name = "picLogo";
			this.picLogo.Size = new Size( 420, 174 );
			this.picLogo.SizeMode = PictureBoxSizeMode.StretchImage;
			this.picLogo.TabIndex = 3;
			this.picLogo.TabStop = false;
			// 
			// grpVideoOptions
			// 
			this.grpVideoOptions.Controls.Add( this.lblOption );
			this.grpVideoOptions.Controls.Add( this.cboOptionValues );
			this.grpVideoOptions.Controls.Add( this.lstOptions );
			this.grpVideoOptions.FlatStyle = FlatStyle.Flat;
			this.grpVideoOptions.Font = new Font( "Tahoma", 9F, FontStyle.Regular, GraphicsUnit.Point, ( ( 0 ) ) );
			this.grpVideoOptions.ForeColor = Color.FromArgb( ( ( ( ( 25 ) ) ) ), ( ( ( ( 35 ) ) ) ), ( ( ( ( 75 ) ) ) ) );
			this.grpVideoOptions.Location = new Point( 12, 215 );
			this.grpVideoOptions.Name = "grpVideoOptions";
			this.grpVideoOptions.Size = new Size( 420, 187 );
			this.grpVideoOptions.TabIndex = 6;
			this.grpVideoOptions.TabStop = false;
			this.grpVideoOptions.Text = "Rendering System Options";
			// 
			// lblOption
			// 
			this.lblOption.FlatStyle = FlatStyle.Flat;
			this.lblOption.Font = new Font( "Tahoma", 9F, FontStyle.Regular, GraphicsUnit.Point, ( ( 0 ) ) );
			this.lblOption.ForeColor = Color.FromArgb( ( ( ( ( 25 ) ) ) ), ( ( ( ( 35 ) ) ) ), ( ( ( ( 75 ) ) ) ) );
			this.lblOption.Location = new Point( 32, 153 );
			this.lblOption.Name = "lblOption";
			this.lblOption.Size = new Size( 200, 22 );
			this.lblOption.TabIndex = 9;
			this.lblOption.Text = "Option Name:";
			this.lblOption.TextAlign = ContentAlignment.MiddleRight;
			this.lblOption.Visible = false;
			// 
			// cboOptionValues
			// 
			this.cboOptionValues.DropDownStyle = ComboBoxStyle.DropDownList;
			this.cboOptionValues.Font = new Font( "Tahoma", 9F, FontStyle.Regular, GraphicsUnit.Point, ( ( 0 ) ) );
			this.cboOptionValues.ForeColor = Color.FromArgb( ( ( ( ( 25 ) ) ) ), ( ( ( ( 35 ) ) ) ), ( ( ( ( 75 ) ) ) ) );
			this.cboOptionValues.Location = new Point( 238, 158 );
			this.cboOptionValues.Name = "cboOptionValues";
			this.cboOptionValues.Size = new Size( 176, 22 );
			this.cboOptionValues.TabIndex = 8;
			this.cboOptionValues.Visible = false;
			this.cboOptionValues.SelectedIndexChanged += cboOptionValues_SelectedIndexChanged;
			// 
			// lstOptions
			// 
			this.lstOptions.ItemHeight = 14;
			this.lstOptions.Location = new Point( 7, 22 );
			this.lstOptions.Name = "lstOptions";
			this.lstOptions.Size = new Size( 407, 130 );
			this.lstOptions.TabIndex = 0;
			this.lstOptions.SelectedIndexChanged += lstOptions_SelectedIndexChanged;
			// 
			// lblRenderer
			// 
			this.lblRenderer.FlatStyle = FlatStyle.Flat;
			this.lblRenderer.Font = new Font( "Tahoma", 9F, FontStyle.Regular, GraphicsUnit.Point, ( ( 0 ) ) );
			this.lblRenderer.ForeColor = Color.FromArgb( ( ( ( ( 25 ) ) ) ), ( ( ( ( 35 ) ) ) ), ( ( ( ( 75 ) ) ) ) );
			this.lblRenderer.Location = new Point( 10, 185 );
			this.lblRenderer.Name = "lblRenderer";
			this.lblRenderer.Size = new Size( 128, 24 );
			this.lblRenderer.TabIndex = 9;
			this.lblRenderer.Text = "Rendering Subsystem:";
			this.lblRenderer.TextAlign = ContentAlignment.MiddleRight;
			// 
			// cboRenderSystems
			// 
			this.cboRenderSystems.DropDownStyle = ComboBoxStyle.DropDownList;
			this.cboRenderSystems.Font = new Font( "Tahoma", 9F, FontStyle.Regular, GraphicsUnit.Point, ( ( 0 ) ) );
			this.cboRenderSystems.ForeColor = Color.FromArgb( ( ( ( ( 25 ) ) ) ), ( ( ( ( 35 ) ) ) ), ( ( ( ( 75 ) ) ) ) );
			this.cboRenderSystems.Location = new Point( 145, 185 );
			this.cboRenderSystems.Name = "cboRenderSystems";
			this.cboRenderSystems.Size = new Size( 285, 22 );
			this.cboRenderSystems.TabIndex = 8;
			this.cboRenderSystems.SelectedIndexChanged += RenderSystems_SelectedIndexChanged;
			// 
			// cmdCancel
			// 
			this.cmdCancel.Anchor = AnchorStyles.Bottom;
			this.cmdCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.cmdCancel.Location = new Point( 355, 408 );
			this.cmdCancel.Name = "cmdCancel";
			this.cmdCancel.Size = new Size( 75, 23 );
			this.cmdCancel.TabIndex = 10;
			this.cmdCancel.Text = "Cancel";
			this.cmdCancel.Click += cmdCancel_Click;
			// 
			// cmdOk
			// 
			this.cmdOk.Anchor = AnchorStyles.Bottom;
			this.cmdOk.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.cmdOk.Location = new Point( 261, 408 );
			this.cmdOk.Name = "cmdOk";
			this.cmdOk.Size = new Size( 75, 23 );
			this.cmdOk.TabIndex = 11;
			this.cmdOk.Text = "Ok";
			this.cmdOk.Click += cmdOk_Click;
			// 
			// pnlBackground
			// 
			this.pnlBackground.BackColor = Color.White;
			this.pnlBackground.Location = new Point( -2, 3 );
			this.pnlBackground.Name = "pnlBackground";
			this.pnlBackground.Size = new Size( 446, 174 );
			this.pnlBackground.TabIndex = 12;
			// 
			// WinFormConfigurationDialog
			// 
			//SWF.Application.AddMessageFilter( this );
			ClientSize = new Size( 442, 436 );
			ControlBox = false;
			Controls.Add( this.cmdOk );
			Controls.Add( this.cmdCancel );
			Controls.Add( this.lblRenderer );
			Controls.Add( this.grpVideoOptions );
			Controls.Add( this.cboRenderSystems );
			Controls.Add( this.picLogo );
			Controls.Add( this.pnlBackground );
			Font = new Font( "Tahoma", 9F, FontStyle.Regular, GraphicsUnit.Point, ( ( 0 ) ) );
			FormBorderStyle = FormBorderStyle.FixedToolWindow;
			MaximizeBox = false;
			MinimizeBox = false;
			Name = "WinFormConfigurationDialog";
			StartPosition = FormStartPosition.CenterScreen;
			Text = "Axiom Rendering Engine Setup";
			Load += WinFormConfigurationDialog_Load;
			( (ISupportInitialize)( this.picLogo ) ).EndInit();
			this.grpVideoOptions.ResumeLayout( false );
			ResumeLayout( false );
		}

		#region Event Handlers

		protected void cmdOk_Click( object sender, EventArgs e )
		{
			if ( this.cboRenderSystems.SelectedItem == null )
			{
				MessageBox.Show( "Please select a rendering system.", "Axiom", MessageBoxButtons.OK, MessageBoxIcon.Exclamation );
				DialogResult = System.Windows.Forms.DialogResult.Cancel;
				return;
			}

			var system = (Axiom.Graphics.RenderSystem)this.cboRenderSystems.SelectedItem;

			string errorMsg = system.ValidateConfigOptions();
			if ( !String.IsNullOrEmpty( errorMsg ) )
			{
				MessageBox.Show( errorMsg, "Axiom", MessageBoxButtons.OK, MessageBoxIcon.Exclamation );
				DialogResult = System.Windows.Forms.DialogResult.Cancel;
				return;
			}

			Engine.RenderSystem = system;
			DialogResult = System.Windows.Forms.DialogResult.OK;
			Close();
		}

		protected void cmdCancel_Click( object sender, EventArgs e )
		{
			DialogResult = System.Windows.Forms.DialogResult.Cancel;
			Dispose();
		}

		private void WinFormConfigurationDialog_Load( object sender, EventArgs e )
		{
			foreach ( Axiom.Graphics.RenderSystem renderSystem in Engine.RenderSystems )
			{
				this.cboRenderSystems.Items.Add( renderSystem );
			}

			// Set the default if it's already configured
			if ( Engine.RenderSystem != null )
			{
				if ( this.cboRenderSystems.Items.Contains( Engine.RenderSystem ) )
				{
					this.cboRenderSystems.SelectedItem = Engine.RenderSystem;
				}
			}

			// Register [Enter] and [Esc] keys for Default buttons
			//SWF.Application.AddMessageFilter( this );
			this.cmdOk.NotifyDefault( true );
		}

		private void RenderSystems_SelectedIndexChanged( object sender, EventArgs e )
		{
			this.lstOptions.Items.Clear();
			this.cboOptionValues.Items.Clear();
			var system = (Axiom.Graphics.RenderSystem)this.cboRenderSystems.SelectedItem;
			ConfigOption optVideoMode;

			// Load Render Subsystem Options
			foreach ( ConfigOption option in system.ConfigOptions.Values )
			{
				this.lstOptions.Items.Add( option );
			}
		}

		private void lstOptions_SelectedIndexChanged( object sender, EventArgs e )
		{
			this.cboOptionValues.SelectedIndexChanged -= cboOptionValues_SelectedIndexChanged;

			var system = (Axiom.Graphics.RenderSystem)this.cboRenderSystems.SelectedItem;
			var opt = (ConfigOption)this.lstOptions.SelectedItem;

			this.cboOptionValues.Items.Clear();
			foreach ( string value in opt.PossibleValues.Values )
			{
				this.cboOptionValues.Items.Add( value );
			}

			if ( this.cboOptionValues.Items.Count == 0 )
			{
				this.cboOptionValues.Items.Add( opt.Value );
			}
			this.cboOptionValues.SelectedIndex = this.cboOptionValues.Items.IndexOf( opt.Value );

			this.lblOption.Text = opt.Name;
			this.lblOption.Visible = true;
			this.cboOptionValues.Visible = true;
			this.cboOptionValues.Enabled = ( !opt.Immutable );

			this.cboOptionValues.SelectedIndexChanged += cboOptionValues_SelectedIndexChanged;
		}

		private void cboOptionValues_SelectedIndexChanged( object sender, EventArgs e )
		{
			var opt = (ConfigOption)this.lstOptions.SelectedItem;
			var value = (string)this.cboOptionValues.SelectedItem;

			opt.Value = value;

			this.lstOptions.SelectedIndexChanged -= lstOptions_SelectedIndexChanged;
			for ( int index = 0; index < this.lstOptions.Items.Count; index++ )
			{
				this.lstOptions.Items[ index ] = this.lstOptions.Items[ index ];
			}
			this.lstOptions.SelectedIndexChanged += lstOptions_SelectedIndexChanged;
		}

		#endregion Event Handlers

		#region IConfigurationDialog Implementation

		public Axiom.Graphics.RenderSystem RenderSystem
		{
			get
			{
				return this.cboRenderSystems.SelectedItem as Axiom.Graphics.RenderSystem;
			}
		}

		public DialogResult Show()
		{
			this.cmdOk.Select();
			return ShowDialog() == System.Windows.Forms.DialogResult.OK ? Configuration.DialogResult.Ok : Configuration.DialogResult.Cancel;
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
		bool IMessageFilter.PreFilterMessage( ref Message msg )
		{
			const int WM_KEYDOWN = 0x100;

			Keys keyCode = (Keys)(int)msg.WParam & Keys.KeyCode;
			if ( msg.Msg == WM_KEYDOWN && keyCode == Keys.Return )
			{
				cmdOk_Click( this, null );
				return true;
			}
			if ( msg.Msg == WM_KEYDOWN && keyCode == Keys.Escape )
			{
				cmdCancel_Click( this, null );
				return true;
			}
			return false;
		}

		#endregion
	}
}
