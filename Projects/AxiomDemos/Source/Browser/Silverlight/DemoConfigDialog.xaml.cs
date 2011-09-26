using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Axiom.Collections;
using Axiom.Core;

namespace Axiom.Demos.Browser.Silverlight
{
    public partial class DemoConfigDialog : ChildWindow
    {
        protected DispatcherTimer tmrRotator = new DispatcherTimer();

        private Stream image;

        private struct DemoItem
        {
            public DemoItem( string name, Type demo )
            {
                Name = name;
                Demo = demo;
            }

            public readonly string Name;
            public readonly Type Demo;

            public override string ToString()
            {
                return Name;
            }
        }

        public DemoConfigDialog()
        {
            InitializeComponent();

            tmrRotator.Interval = new TimeSpan( 0, 0, 0, 0, 1000 );
            tmrRotator.Tick += tmrRotator_Tick;
            cd.cmdOk.Click += (s, o) => Close();
            cd.cmdCancel.Click += (s, o) => Close();
        }

        private void picPreview_MouseLeftButtonDown( object sender, MouseButtonEventArgs e )
        {
            tmrRotator.Stop();
        }

        private void tmrRotator_Tick( object sender, EventArgs e )
        {
            lstDemos.SelectedIndex = ( lstDemos.SelectedIndex + 1 )%( lstDemos.Items.Count );
            tmrRotator.Start();
        }

        private void lstDemos_SelectionChanged( object sender, SelectionChangedEventArgs e )
        {
            //Stop the rotator
            tmrRotator.Stop();

            if ( image != null )
            {
                image.Close();
            }

            try
            {
                image = ResourceGroupManager.Instance.OpenResource( ( (DemoItem)lstDemos.SelectedItem ).Name + ".jpg",
                                                                    ResourceGroupManager.DefaultResourceGroupName );
            }
            catch ( Exception )
            {
                image = ResourceGroupManager.Instance.OpenResource( "ImageNotAvailable.jpg",
                                                                    ResourceGroupManager.DefaultResourceGroupName );
            }

            if ( image != null )
            {
                var bi = new BitmapImage();
                bi.SetSource( image );
                picPreview.Source = bi;
            }
        }

        public Type Demo
        {
            get
            {
                return lstDemos.SelectedIndex != -1 ? ( (DemoItem)lstDemos.SelectedItem ).Demo : null;
            }
        }

        [ImportMany(typeof(TechDemo))]
        public IEnumerable<TechDemo> techdemos { private get; set; }

        public void LoadDemos(string DemoAssembly)
        {
            var demoList = new AxiomSortedCollection<string, DemoItem>();

            /**/
            this.SatisfyImports();
            foreach ( var techdemo in techdemos )
            {
                var demoType = techdemo.GetType();
                demoList.Add( demoType.Name, new DemoItem( demoType.Name, demoType ) );
            }
            /*/
            var demos = Assembly.LoadFrom( DemoAssembly );
            var demoTypes = demos.GetTypes();
            var techDemo = demos.GetType( "Axiom.Demos.TechDemo" );

            foreach ( var demoType in demoTypes )
            {
                if ( demoType.IsSubclassOf( techDemo ) )
                {
                    demoList.Add( demoType.Name, new DemoItem( demoType.Name, demoType ) );
                }
            }
            /**/

            foreach ( var demoItem in demoList )
            {
                lstDemos.Items.Add( demoItem.Value );
            }

            lstDemos.SelectionChanged += lstDemos_SelectionChanged;

            if ( lstDemos.Items.Count > 0 )
                lstDemos.SelectedIndex = 0;

            tmrRotator.Start();
        }

        private void lstDemos_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                cd.cmdOk_Click(this, null);
        }

        private void lstDemos_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount > 1)
                cd.cmdOk_Click(this, null);
        }
    }
}