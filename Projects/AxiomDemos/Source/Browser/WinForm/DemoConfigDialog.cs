
using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using SWF = System.Windows.Forms;

using Axiom;
using System.Collections.Generic;
using System.Reflection;
using Axiom.Core;

namespace Axiom.Demos
{
	public class DemoConfigDialog : ConfigDialog
	{
		protected ListBox lstDemos;
		protected GroupBox grpAvailableDemos;
		protected PictureBox picPreview;
		protected SWF.Timer tmrRotator;

		private struct DemoItem
		{
			public DemoItem( string name, Type demo )
			{
				Name = name;
				Demo = demo;
			}

			public string Name;
			public Type Demo;
			public override string ToString()
			{
				return Name;
			}
		}

		public DemoConfigDialog() : base()
		{
			InitializeComponent();
		}

        private void InitializeComponent()
        {
			this.lstDemos = new ListBox();
			this.grpAvailableDemos = new GroupBox();
			this.picPreview = new PictureBox();
			this.tmrRotator = new System.Windows.Forms.Timer();
			///
			/// lstDemos
			this.lstDemos.ItemHeight = 14;
			this.lstDemos.Location = new System.Drawing.Point( 7, 20 );
			this.lstDemos.Name = "lstDemos";
			this.lstDemos.Size = new System.Drawing.Size( 200, 130 );
			this.lstDemos.TabIndex = 0;
			this.lstDemos.SelectedIndexChanged += new EventHandler( lstDemos_SelectedIndexChanged );
			this.lstDemos.DoubleClick += new EventHandler( cmdOk_Click );

			//
			// picPreview
			// 
			this.picPreview.BackColor = System.Drawing.Color.White;
			this.picPreview.Location = new System.Drawing.Point( 205, 20 );
			this.picPreview.Name = "picPreview";
			this.picPreview.Size = new System.Drawing.Size( 200, 130 );
			this.picPreview.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
			this.picPreview.TabIndex = 3;
			this.picPreview.TabStop = false;
			this.picPreview.Click += new EventHandler( picPreview_Click );
			this.picPreview.DoubleClick += new EventHandler( cmdOk_Click );

			// 
			// grpAvailableDemos
			// 
			this.grpAvailableDemos.Controls.Add( this.lstDemos );
			this.grpAvailableDemos.Controls.Add( this.picPreview );
			this.grpAvailableDemos.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.grpAvailableDemos.Font = new System.Drawing.Font( "Tahoma", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ( (byte)( 0 ) ) );
			this.grpAvailableDemos.ForeColor = System.Drawing.Color.FromArgb( ( (int)( ( (byte)( 25 ) ) ) ), ( (int)( ( (byte)( 35 ) ) ) ), ( (int)( ( (byte)( 75 ) ) ) ) );
			this.grpAvailableDemos.Location = new System.Drawing.Point( 12, 408 );
			this.grpAvailableDemos.Name = "grpAvailableDemos";
			this.grpAvailableDemos.Size = new System.Drawing.Size( 420, 157 );
			this.grpAvailableDemos.TabIndex = 7;
			this.grpAvailableDemos.TabStop = false;
			this.grpAvailableDemos.Text = "Available Demos";

			///
			/// tmrRotator
			/// 
			this.tmrRotator.Interval = 1000;
			this.tmrRotator.Tick += new EventHandler( tmrRotator_Tick );
			///
			/// DemoConfigDialog
			this.ClientSize = new System.Drawing.Size( 442, 595 );
			this.Controls.Add( grpAvailableDemos );

		}

		private void picPreview_Click( object sender, EventArgs e )
		{
			this.tmrRotator.Stop();
		}

		private void tmrRotator_Tick( object sender, EventArgs e )
		{
			lstDemos.SelectedIndex = ( lstDemos.SelectedIndex + 1 ) % ( lstDemos.Items.Count);
			this.tmrRotator.Start();
		}

		private void lstDemos_SelectedIndexChanged( object sender, EventArgs e )
		{
			Stream image = null;

			//Stop the rotator
			this.tmrRotator.Stop();
			try
			{
				image = ResourceGroupManager.Instance.OpenResource( ( (DemoItem)lstDemos.SelectedItem ).Name + ".jpg", ResourceGroupManager.DefaultResourceGroupName );
			}
			catch ( Exception )
			{
				image = ResourceGroupManager.Instance.OpenResource( "ImageNotAvailable.jpg", ResourceGroupManager.DefaultResourceGroupName );
			}

			if ( image != null )
			{
				this.picPreview.Image = System.Drawing.Image.FromStream( image, true );
			}

			image.Close();
		}

		public Type Demo
		{
			get
			{
				return lstDemos.SelectedIndex != -1 ? ((DemoItem)lstDemos.SelectedItem).Demo : null;
			}
		}

		public void LoadDemos( string DemoAssembly )
		{
			SortedList<string, DemoItem> demoList = new SortedList<string, DemoItem>();

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

			foreach ( KeyValuePair<string, DemoItem> typeName in demoList )
			{
				lstDemos.Items.Add( typeName.Value );
			}

			if ( lstDemos.Items.Count > 0 )
				lstDemos.SelectedIndex = 0;

			this.tmrRotator.Start();

		}
	}
}
