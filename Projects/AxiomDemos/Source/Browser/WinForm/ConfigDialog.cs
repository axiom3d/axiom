using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

using Axiom.Configuration;
using Axiom.Core;
using Axiom.Graphics;

using SWF = System.Windows.Forms;

namespace Axiom.Demos
{
	public class ConfigDialog : Form, IMessageFilter
	{
		// A delegate type for hooking up loaded notifications.

		#region Delegates

		public delegate void LoadRenderSystemConfigEventHandler( object sender, RenderSystem rs );

		// A delegate type for hooking up save notifications.
		public delegate void SaveRenderSystemConfigEventHandler( object sender, RenderSystem rs );

		#endregion

		// An event that clients can use to be notified whenever a
		// RenderSystem is loaded.

		private const int WM_KEYDOWN = 0x100;
		private string _iconResourceName = "AxiomIcon.ico";
		private string _logoResourceName = "AxiomLogo.png";
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

		public ConfigDialog()
		{
			SetStyle( ControlStyles.DoubleBuffer, true );
			InitializeComponent();

			try
			{
				Stream image = ResourceGroupManager.Instance.OpenResource( this._logoResourceName, ResourceGroupManager.DefaultResourceGroupName );
				Stream icon = ResourceGroupManager.Instance.OpenResource( this._iconResourceName, ResourceGroupManager.DefaultResourceGroupName );

				if ( image != null )
				{
					this.picLogo.Image = Image.FromStream( image, true );
				}

				if ( icon != null )
				{
					Icon = new Icon( icon );
				}

				image.Close();
				icon.Close();
			}
			catch ( Exception ) { }
			//cboRenderSystems.Enabled = false;
		}

		public string LogoResourceName
		{
			get
			{
				return this._logoResourceName;
			}
			set
			{
				this._logoResourceName = value;
			}
		}

		public string IconResourceName
		{
			get
			{
				return this._iconResourceName;
			}
			set
			{
				this._iconResourceName = value;
			}
		}

		#region IMessageFilter Members

		public bool PreFilterMessage( ref Message msg )
		{
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

		public event LoadRenderSystemConfigEventHandler LoadRenderSystemConfig;

		// An event that clients can use to be notified whenever a
		// RenderSystem Configuration needs to be saved.
		public event SaveRenderSystemConfigEventHandler SaveRenderSystemConfig;

		protected override void Dispose( bool disposing )
		{
			if ( disposing )
			{
				if ( this.components != null )
				{
					this.components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		private void InitializeComponent()
		{
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
			this.grpVideoOptions.Font = new Font( "Arial", 9F, FontStyle.Regular, GraphicsUnit.Point, ( ( 0 ) ) );
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
			this.lblOption.Font = new Font( "Arial", 9F, FontStyle.Regular, GraphicsUnit.Point, ( ( 0 ) ) );
			this.lblOption.ForeColor = Color.FromArgb( ( ( ( ( 25 ) ) ) ), ( ( ( ( 35 ) ) ) ), ( ( ( ( 75 ) ) ) ) );
			this.lblOption.Location = new Point( 104, 160 );
			this.lblOption.Name = "lblOption";
			this.lblOption.Size = new Size( 128, 22 );
			this.lblOption.TabIndex = 9;
			this.lblOption.Text = "Option Name:";
			this.lblOption.TextAlign = ContentAlignment.MiddleRight;
			this.lblOption.Visible = false;
			//
			// cboOptionValues
			//
			this.cboOptionValues.DropDownStyle = ComboBoxStyle.DropDownList;
			this.cboOptionValues.Font = new Font( "Arial", 8F, FontStyle.Regular, GraphicsUnit.Point, ( ( 0 ) ) );
			this.cboOptionValues.ForeColor = Color.FromArgb( ( ( ( ( 25 ) ) ) ), ( ( ( ( 35 ) ) ) ), ( ( ( ( 75 ) ) ) ) );
			this.cboOptionValues.Location = new Point( 238, 158 );
			this.cboOptionValues.Name = "cboOptionValues";
			this.cboOptionValues.Size = new Size( 176, 24 );
			this.cboOptionValues.TabIndex = 8;
			this.cboOptionValues.Visible = false;
			this.cboOptionValues.SelectedIndexChanged += cboOptionValues_SelectedIndexChanged;
			//
			// lstOptions
			//
			this.lstOptions.ItemHeight = 14;
			this.lstOptions.Location = new Point( 7, 22 );
			this.lstOptions.Name = "lstOptions";
			this.lstOptions.Size = new Size( 407, 165 );
			this.lstOptions.TabIndex = 0;
			this.lstOptions.SelectedIndexChanged += lstOptions_SelectedIndexChanged;
			//
			// lblRenderer
			//
			this.lblRenderer.FlatStyle = FlatStyle.Flat;
			this.lblRenderer.Font = new Font( "Arial", 9F, FontStyle.Regular, GraphicsUnit.Point, ( ( 0 ) ) );
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
			this.cboRenderSystems.Font = new Font( "Arial", 9F, FontStyle.Regular, GraphicsUnit.Point, ( ( 0 ) ) );
			this.cboRenderSystems.ForeColor = Color.FromArgb( ( ( ( ( 25 ) ) ) ), ( ( ( ( 35 ) ) ) ), ( ( ( ( 75 ) ) ) ) );
			this.cboRenderSystems.Location = new Point( 145, 185 );
			this.cboRenderSystems.Name = "cboRenderSystems";
			this.cboRenderSystems.Size = new Size( 285, 24 );
			this.cboRenderSystems.TabIndex = 8;
			this.cboRenderSystems.SelectedIndexChanged += RenderSystems_SelectedIndexChanged;
			//
			// cmdCancel
			//
			this.cmdCancel.Anchor = AnchorStyles.Bottom;
			this.cmdCancel.DialogResult = DialogResult.Cancel;
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
			this.cmdOk.DialogResult = DialogResult.OK;
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
			// ConfigDialog
			//
			ClientSize = new Size( 442, 436 );
			ControlBox = false;
			Controls.Add( this.cmdOk );
			Controls.Add( this.cmdCancel );
			Controls.Add( this.lblRenderer );
			Controls.Add( this.grpVideoOptions );
			Controls.Add( this.cboRenderSystems );
			Controls.Add( this.picLogo );
			Controls.Add( this.pnlBackground );
			Font = new Font( "Arial", 9F, FontStyle.Bold, GraphicsUnit.Point, ( ( 0 ) ) );
			FormBorderStyle = FormBorderStyle.FixedToolWindow;
			MaximizeBox = false;
			MinimizeBox = false;
			Name = "ConfigDialog";
			StartPosition = FormStartPosition.CenterScreen;
			Text = "Axiom Rendering Engine Setup";
			Load += ConfigDialog_Load;
			( (ISupportInitialize)( this.picLogo ) ).EndInit();
			this.grpVideoOptions.ResumeLayout( false );
			ResumeLayout( false );
		}

		protected void cmdOk_Click( object sender, EventArgs e )
		{
			Root.Instance.RenderSystem = (RenderSystem)this.cboRenderSystems.SelectedItem;

			RenderSystem system = Root.Instance.RenderSystem;

			foreach ( ConfigOption opt in this.lstOptions.Items )
			{
				system.ConfigOptions[ opt.Name ] = opt;
			}

			SaveRenderSystemConfig( this, system );

			DialogResult = DialogResult.OK;
			Close();
		}

		protected void cmdCancel_Click( object sender, EventArgs e )
		{
			DialogResult = DialogResult.Cancel;
			Dispose();
		}

		private void ConfigDialog_Load( object sender, EventArgs e )
		{
			// Register [Enter] and [Esc] keys for Default buttons
			Application.AddMessageFilter( this );
			this.cmdOk.NotifyDefault( true );
			try
			{
				if ( !DesignMode )
				{
					foreach ( RenderSystem renderSystem in Root.Instance.RenderSystems )
					{
						LoadRenderSystemConfig( this, renderSystem );
						this.cboRenderSystems.Items.Add( renderSystem );
					}
				}
			}
			catch ( Exception ex )
			{
				LogManager.Instance.Write( LogManager.BuildExceptionString( ex ) );
				throw;
			}

			if ( this.cboRenderSystems.Items.Count > 0 )
			{
				this.cboRenderSystems.SelectedIndex = 0;
			}
		}

		private void RenderSystems_SelectedIndexChanged( object sender, EventArgs e )
		{
			this.lstOptions.Items.Clear();
			this.cboOptionValues.Items.Clear();
			var system = (RenderSystem)this.cboRenderSystems.SelectedItem;
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

			var system = (RenderSystem)this.cboRenderSystems.SelectedItem;
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
	}
}
