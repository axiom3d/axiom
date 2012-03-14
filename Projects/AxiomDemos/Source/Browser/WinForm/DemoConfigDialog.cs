using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

using Axiom.Collections;
using Axiom.Core;

using SWF = System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace Axiom.Demos
{
	public class DemoConfigDialog : ConfigDialog
	{
		protected GroupBox grpAvailableDemos;
		private Stream image;
		protected ListBox lstDemos;
		protected PictureBox picPreview;
		protected Timer tmrRotator;

		public DemoConfigDialog()
		{
			InitializeComponent();
		}

		public Type Demo
		{
			get
			{
				return this.lstDemos.SelectedIndex != -1 ? ( (DemoItem)this.lstDemos.SelectedItem ).Demo : null;
			}
		}

		protected override void Dispose( bool disposing )
		{
			if ( this.image != null )
			{
				this.image.Close();
			}
		}

		private void InitializeComponent()
		{
			this.lstDemos = new ListBox();
			this.grpAvailableDemos = new GroupBox();
			this.picPreview = new PictureBox();
			this.tmrRotator = new Timer();

			///
			/// lstDemos
			this.lstDemos.ItemHeight = 14;
			this.lstDemos.Location = new Point( 7, 20 );
			this.lstDemos.Name = "lstDemos";
			this.lstDemos.Size = new Size( 200, 135 );
			this.lstDemos.TabIndex = 0;

			//
			// picPreview
			//
			this.picPreview.BackColor = Color.White;
			this.picPreview.Location = new Point( 210, 20 );
			this.picPreview.Name = "picPreview";
			this.picPreview.Size = new Size( 200, 123 );
			this.picPreview.SizeMode = PictureBoxSizeMode.StretchImage;
			this.picPreview.TabIndex = 3;
			this.picPreview.TabStop = false;
			this.picPreview.Click += picPreview_Click;
			this.picPreview.DoubleClick += cmdOk_Click;

			//
			// grpAvailableDemos
			//
			this.grpAvailableDemos.Controls.Add( this.lstDemos );
			this.grpAvailableDemos.Controls.Add( this.picPreview );
			this.grpAvailableDemos.FlatStyle = FlatStyle.Flat;
			this.grpAvailableDemos.Font = new Font( "Arial", 9F, FontStyle.Regular, GraphicsUnit.Point, ( ( 0 ) ) );
			this.grpAvailableDemos.ForeColor = Color.FromArgb( ( ( ( ( 25 ) ) ) ), ( ( ( ( 35 ) ) ) ), ( ( ( ( 75 ) ) ) ) );
			this.grpAvailableDemos.Location = new Point( 12, 408 );
			this.grpAvailableDemos.Name = "grpAvailableDemos";
			this.grpAvailableDemos.Size = new Size( 420, 155 );
			this.grpAvailableDemos.TabIndex = 7;
			this.grpAvailableDemos.TabStop = false;
			this.grpAvailableDemos.Text = "Available Demos";

			///
			/// tmrRotator
			///
			this.tmrRotator.Interval = 1000;
			this.tmrRotator.Tick += tmrRotator_Tick;

			///
			/// DemoConfigDialog
			ClientSize = new Size( 442, 595 );
			Controls.Add( this.grpAvailableDemos );
		}

		private void picPreview_Click( object sender, EventArgs e )
		{
			this.tmrRotator.Stop();
		}

		private void tmrRotator_Tick( object sender, EventArgs e )
		{
			this.lstDemos.SelectedIndex = ( this.lstDemos.SelectedIndex + 1 ) % ( this.lstDemos.Items.Count );
			this.tmrRotator.Start();
		}

		private void lstDemos_SelectedIndexChanged( object sender, EventArgs e )
		{
			//Stop the rotator
			this.tmrRotator.Stop();

			if ( this.image != null )
			{
				this.image.Close();
			}

			try
			{
				this.image = ResourceGroupManager.Instance.OpenResource( ( (DemoItem)this.lstDemos.SelectedItem ).Name + ".jpg", ResourceGroupManager.DefaultResourceGroupName );
			}
			catch ( Exception )
			{
				this.image = ResourceGroupManager.Instance.OpenResource( "ImageNotAvailable.jpg", ResourceGroupManager.DefaultResourceGroupName );
			}

			if ( this.image != null )
			{
				this.picPreview.Image = Image.FromStream( this.image, true );
			}
		}

		public void LoadDemos( string DemoAssembly )
		{
			var demoList = new AxiomSortedCollection<string, DemoItem>();

			Assembly demos = Assembly.LoadFrom( DemoAssembly );
			Type[] demoTypes = demos.GetTypes();
			Type techDemo = demos.GetType( "Axiom.Demos.TechDemo" );

			foreach ( Type demoType in demoTypes )
			{
				if ( demoType.IsSubclassOf( techDemo ) )
				{
					demoList.Add( demoType.Name, new DemoItem( demoType.Name, demoType ) );
				}
			}

			foreach ( var demoItem in demoList )
			{
				this.lstDemos.Items.Add( demoItem.Value );
			}

			this.lstDemos.SelectedIndexChanged += lstDemos_SelectedIndexChanged;
			this.lstDemos.DoubleClick += cmdOk_Click;

			if ( this.lstDemos.Items.Count > 0 )
			{
				this.lstDemos.SelectedIndex = 0;
			}

			this.tmrRotator.Start();
		}

		#region Nested type: DemoItem

		private struct DemoItem
		{
			public readonly Type Demo;
			public readonly string Name;

			public DemoItem( string name, Type demo )
			{
				this.Name = name;
				this.Demo = demo;
			}

			public override string ToString()
			{
				return this.Name;
			}
		}

		#endregion
	}
}
