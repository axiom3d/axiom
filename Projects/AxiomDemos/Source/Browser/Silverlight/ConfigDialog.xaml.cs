using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Browser;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Axiom.Configuration;
using Axiom.Core;
using Axiom.Graphics;

namespace Axiom.Demos.Browser.Silverlight
{
    public partial class ConfigDialog : ChildWindow
    {
        // A delegate type for hooking up loaded notifications.
        public delegate void LoadRenderSystemConfigEventHandler( object sender, RenderSystem rs );

        // A delegate type for hooking up save notifications.
        public delegate void SaveRenderSystemConfigEventHandler( object sender, RenderSystem rs );

        // An event that clients can use to be notified whenever a
        // RenderSystem is loaded.
        public event LoadRenderSystemConfigEventHandler LoadRenderSystemConfig;

        // An event that clients can use to be notified whenever a
        // RenderSystem Configuration needs to be saved.
        public event SaveRenderSystemConfigEventHandler SaveRenderSystemConfig;

        private string _logoResourceName = "AxiomLogo.png";

        public string LogoResourceName
        {
            get
            {
                return _logoResourceName;
            }
            set
            {
                _logoResourceName = value;
            }
        }

        private string _iconResourceName = "AxiomIcon.ico";

        public string IconResourceName
        {
            get
            {
                return _iconResourceName;
            }
            set
            {
                _iconResourceName = value;
            }
        }

        public ConfigDialog()
        {
            InitializeComponent();

            try
            {
                var image = ResourceGroupManager.Instance.OpenResource( _logoResourceName, ResourceGroupManager.DefaultResourceGroupName );

                if ( image != null )
                {
                    var bi = new BitmapImage();
                    bi.SetSource( image );
                    picLogo.Source = bi;
                }                

                image.Close();
            }
            catch ( Exception )
            {
            }

            cboRenderSystems.SelectionChanged += cboRenderSystems_SelectionChanged;
            lstOptions.SelectionChanged += lstOptions_SelectionChanged;
            cboOptionValues.Loaded += cboOptionValues_Loaded;
            cboOptionValues.SelectionChanged += cboOptionValues_SelectionChanged;
            cmdOk.Click += cmdOk_Click;
            cmdCancel.Click += cmdCancel_Click;
        }

        internal void cmdOk_Click( object sender, RoutedEventArgs e )
        {
            Root.Instance.RenderSystem = (RenderSystem)cboRenderSystems.SelectedItem;

            var system = Root.Instance.RenderSystem;

            foreach ( ConfigOption opt in lstOptions.Items )
            {
                system.ConfigOptions[ opt.Name ] = opt;
            }

            SaveRenderSystemConfig( this, system );

            DialogResult = true;
            if ( sender is ChildWindow )
            {
                ( sender as ChildWindow ).Close();
            }
        }

        private void cmdCancel_Click( object sender, RoutedEventArgs e )
        {
            DialogResult = false;
            if ( sender is ChildWindow )
            {
                ( sender as ChildWindow ).Close();
            }
        }

        private void cboOptionValues_Loaded( object sender, RoutedEventArgs e )
        {
            // Register [Enter] and [Esc] keys for Default buttons
            //Application.AddMessageFilter(this);
            //cmdOk.NotifyDefault(true);
            try
            {
                if ( !DesignerProperties.GetIsInDesignMode( Application.Current.RootVisual ) )
                {
                    foreach ( RenderSystem renderSystem in Root.Instance.RenderSystems )
                    {
                        LoadRenderSystemConfig( this, renderSystem );
                        cboRenderSystems.Items.Add( renderSystem );
                    }
                }
            }
            catch ( Exception ex )
            {
                LogManager.Instance.Write( LogManager.BuildExceptionString( ex ) );
                throw;
            }

            if ( cboRenderSystems.Items.Count > 0 )
            {
                cboRenderSystems.SelectedIndex = 0;
            }
        }

        private void cboRenderSystems_SelectionChanged( object sender, SelectionChangedEventArgs e )
        {
            lstOptions.Items.Clear();
            cboOptionValues.Items.Clear();
            var system = (RenderSystem)cboRenderSystems.SelectedItem;
            ConfigOption optVideoMode;

            // Load Render Subsystem Options
            foreach ( var option in system.ConfigOptions.Values )
            {
                lstOptions.Items.Add( option );
            }
        }

        private void lstOptions_SelectionChanged( object sender, SelectionChangedEventArgs e )
        {
            cboOptionValues.SelectionChanged -= cboOptionValues_SelectionChanged;

            var system = (RenderSystem)cboRenderSystems.SelectedItem;
            var opt = (ConfigOption)lstOptions.SelectedItem;

            cboOptionValues.Items.Clear();
            foreach ( var value in opt.PossibleValues.Values )
            {
                cboOptionValues.Items.Add( value );
            }

            if ( cboOptionValues.Items.Count == 0 )
            {
                cboOptionValues.Items.Add( opt.Value );
            }
            cboOptionValues.SelectedIndex = cboOptionValues.Items.IndexOf( opt.Value );

            lblOption.Content = opt.Name;
            lblOption.Visibility = Visibility.Visible;
            cboOptionValues.Visibility = Visibility.Visible;
            cboOptionValues.IsEnabled = ( !opt.Immutable );

            cboOptionValues.SelectionChanged += cboOptionValues_SelectionChanged;
        }

        private void cboOptionValues_SelectionChanged( object sender, SelectionChangedEventArgs e )
        {
            var opt = (ConfigOption)lstOptions.SelectedItem;
            var value = (string)cboOptionValues.SelectedItem;

            opt.Value = value;

            lstOptions.SelectionChanged -= lstOptions_SelectionChanged;
            for ( var index = 0; index < lstOptions.Items.Count; index++ )
            {
                lstOptions.Items[ index ] = lstOptions.Items[ index ];
            }
            lstOptions.SelectionChanged += lstOptions_SelectionChanged;
        }
    }
}