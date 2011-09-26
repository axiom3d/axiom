using System;
using System.IO;
using System.IO.IsolatedStorage;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Xml;
using System.Xml.Linq;
using Axiom.Configuration;
using Axiom.Core;
using Axiom.Graphics;
using Axiom.RenderSystems.Xna;

namespace Axiom.Demos.Browser.Silverlight
{
    public partial class MainPage : UserControl
    {
        //private static readonly HeadingInfo _headingInfo = new HeadingInfo( "Axiom Samples", "0.8" );

        private sealed class Options
        {
            //[Option( "s", "sample", Required = false, HelpText = "Initial sample to run." )] 
            public readonly string Sample = String.Empty;

            //[HelpOption( HelpText = "Display this help screen." )]
            public string GetUsage()
            {
                //var help = new HelpText( Program._headingInfo );
                //help.AdditionalNewLineAfterOption = true;
                //help.Copyright = new CopyrightInfo( "Axiom Engine Team", 2003, 2010 );
                //help.AddPreOptionsLine( "This is free software. You may redistribute copies of it under the terms of" );
                //help.AddPreOptionsLine( "the LGPL License <http://www.opensource.org/licenses/lgpl-license.php>." );
                //help.AddPreOptionsLine( "Usage: SampleApp -sCompositor " );
                //help.AddOptions( this );

                //return help;
                return "";
            }
        }

        protected const string CONFIG_FILE = @"EngineConfig.xml";

        private Root engine;
        private DemoConfigDialog dlg;
        private XElement config;

        private readonly string DemoAssembly = @"Axiom.Demos.dll";
        //Path.GetDirectoryName( Assembly.GetExecutingAssembly().Location ) + Path.DirectorySeparatorChar +

        private void Choosed(Options options)
        {
            try
            {
                Type demoType = null;
                if (!String.IsNullOrEmpty(options.Sample))
                {
#if SILVERLIGHT
                    var demos = Assembly.Load(DemoAssembly);
#else
                                            var demos = Assembly.LoadFrom( DemoAssembly );
#endif
                    var demoTypes = demos.GetTypes();
                    demoType = demos.GetType("Axiom.Demos." + options.Sample);
                }
                else
                {
                    demoType = dlg.Demo;
                }

                if (demoType != null)
                {
                    demo = (TechDemo)Activator.CreateInstance(demoType);
                    demo.SetupResources();
                    demo.Start(); //show and start rendering
                }
            }
            catch (Exception caughtException)
            {
                LogManager.Instance.Write(BuildExceptionString(caughtException));
            }
        }

        private bool _configure( Options options )
        {
            // instantiate the Root singleton
            engine = new Root( "AxiomDemos.log" );

            _setupResources();

            dlg = new DemoConfigDialog();
            dlg.cd.LoadRenderSystemConfig += LoadRenderSystemConfiguration;
            dlg.cd.SaveRenderSystemConfig += SaveRenderSystemConfiguration;
            dlg.LoadDemos( DemoAssembly );

            if (String.IsNullOrEmpty(options.Sample))
            {
                dlg.Show();
                dlg.Closed += ( s, e ) => Choosed( options );                            
                //TODO: Wait?
                var result = dlg.DialogResult;

                if (result == null)
                {
                    //engine.RenderSystem = engine.RenderSystems[0];
                    //LoadRenderSystemConfiguration(this, engine.RenderSystems[0]);
                    return false;
                }

                if (result == false)
                {
                    Root.Instance.Dispose();
                    engine = null;
                    return false;
                }
            }
            else
            {
                engine.RenderSystem = engine.RenderSystems[ 0 ];
                LoadRenderSystemConfiguration( this, engine.RenderSystems[ 0 ] );
            }

            return true;
        }

        public XElement FindByNameRenderSystem( string Name, string RenderSystem )
        {
            var Rows = config.Elements( "ConfigOption".X() );
            foreach ( var row in Rows )
            {
                if ( row.Attr( "Name" ) == Name && row.Attr( "RenderSystem" ) == RenderSystem )
                {
                    return row;
                }
            }
            return null;
        }

        private void SaveRenderSystemConfiguration( object sender, RenderSystem rs )
        {
            var renderSystemId = rs.GetType().FullName;

            foreach ( ConfigOption opt in rs.ConfigOptions )
            {
                var coRow = FindByNameRenderSystem( opt.Name, renderSystemId );
                if ( coRow == null )
                {
                    coRow = new XElement( "ConfigOption".X() );
                    coRow.Attr( "RenderSystem", renderSystemId );
                    coRow.Attr( "Name", opt.Name );
                    config.Add( coRow );
                }
                coRow.Value = opt.Value;
            }

            var isolatedStorage = IsolatedStorageFile.GetUserStoreForApplication();
            using ( var stream = isolatedStorage.OpenFile( CONFIG_FILE, FileMode.Create ) )
            {
                using ( var writer = XmlWriter.Create( stream ) )
                {
                    config.WriteTo( writer );
                }
            }
        }

        private void LoadRenderSystemConfiguration( object sender, RenderSystem rs )
        {
            var renderSystemId = rs.GetType().FullName;

            foreach ( var row in config.Elements( "ConfigOption".X() ) )
            {
                if ( row.Attr( "RenderSystem" ) == renderSystemId )
                {
                    var name = row.Attr( "Name" );
                    if ( rs.ConfigOptions.ContainsKey( name ) )
                    {
                        rs.ConfigOptions[ name ].Value = row.Attr( "Value" );
                    }
                }
            }
        }

        /// <summary>
        ///		Loads default resource configuration if one exists.
        /// </summary>
        private void _setupResources()
        {
            var resourceConfigPath = new Uri( "./" + CONFIG_FILE, UriKind.Relative ); //Path.GetFullPath( 

            //if ( File.Exists( resourceConfigPath ) )            
            {
                // load the config file
                // relative from the location of debug and releases executables
                config = XElement.Load( CONFIG_FILE );

                // interrogate the available resource paths
                foreach ( var row in config.Elements( "FilePath".X() ) )
                {
                    ResourceGroupManager.Instance.AddResourceLocation(
                        row.Attr( "src" ), row.Attr( "type" ),
                        row.Attr( "group" ) ?? ResourceGroupManager.DefaultResourceGroupName, false, true );
                }
            }
        }

        TechDemo demo;

        private void Run( Options options )
        {
            try
            {
                if (_configure(options))
                    Choosed( options );
            }
            catch ( Exception caughtException )
            {
                LogManager.Instance.Write( BuildExceptionString( caughtException ) );
            }
        }

        private void StreamCopy(Stream dst, Stream src)
        {
            int read;
            var buffer = new byte[4096];
            while ( ( read = src.Read( buffer, 0, buffer.Length ) ) > 0 )
                dst.Write( buffer, 0, read );
        }

        private void Debug()
        {
            var isolatedStorage = IsolatedStorageFile.GetUserStoreForApplication();            
            var xapName = Application.Current.Host.Source.AbsolutePath;
            xapName = xapName.Substring( xapName.LastIndexOf( "/" ) + 1 );
            var xapCopy = isolatedStorage.CreateFile(xapName);

            if (App.Current.IsRunningOutOfBrowser)
            {
                Stream xapOriginal;
                if ( App.Current.HasElevatedPermissions )
                    xapOriginal = File.OpenRead( Application.Current.Host.Source.AbsolutePath );
                else
                    xapOriginal = File.OpenRead( Application.Current.Host.Source.AbsolutePath );
                var excess = xapOriginal.Length - isolatedStorage.Quota;
                if (excess > 0)
                    isolatedStorage.IncreaseQuotaTo( isolatedStorage.Quota + excess + 4096 );
                StreamCopy( xapCopy, xapOriginal );
            }
            else
            {
                xapName = Application.Current.Host.Source.AbsoluteUri.Replace(xapName, "OutXap.txt");
                var wait = new AutoResetEvent(false);
                var wc = new WebClient();
                wc.OpenReadCompleted += (s, o) =>
                {
                    int read;
                    var buffer = new byte[4096];
                    while ((read = o.Result.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        xapCopy.Write(buffer, 0, read);
                    }
                    wait.Set();
                };
                var uri = new Uri(xapName);
                wc.OpenReadAsync(uri);
                wait.WaitOne();
            }
        }

        public MainPage()
        {
            InitializeComponent();
        }

        private void ExecuteCoreTask( Options options )
        {
            try
            {
                Run( options ); //show and start rendering
            }
            catch ( Exception ex )
            {
                Console.WriteLine( BuildExceptionString( ex ) );
                Console.WriteLine( "An exception has occurred.  Press enter to continue..." );
                Console.ReadLine();
            }
        }

        private static string BuildExceptionString( Exception exception )
        {
            var errMessage = string.Empty;

            errMessage += exception.Message + Environment.NewLine + exception.StackTrace;

            while ( exception.InnerException != null )
            {
                errMessage += BuildInnerExceptionString( exception.InnerException );
                exception = exception.InnerException;
            }

            return errMessage;
        }

        private static string BuildInnerExceptionString( Exception innerException )
        {
            var errMessage = string.Empty;

            errMessage += Environment.NewLine + " InnerException ";
            errMessage += Environment.NewLine + innerException.Message + Environment.NewLine + innerException.StackTrace;

            return errMessage;
        }

        private void DrawSurface_Draw(object sender, DrawEventArgs e)
        {

        }

        private void DrawSurface_Loaded(object sender, RoutedEventArgs e)
        {
            //Debug();
            XnaRenderWindow.DrawingSurface = DrawSurface;

            var options = new Options();
            //var parser = new CommandLineParser( new CommandLineParserSettings( Console.Error ) );
            //if ( !parser.ParseArguments( args, options ) )
            //{
            //    Environment.Exit( 1 );
            //}

            ExecuteCoreTask(options);
        }
    }

    public static class XElementExtensions
    {
        public static XName X( this string name )
        {
            return XName.Get( name, "http://tempuri.org/EngineConfig.xsd" );
        }

        public static string Attr( this XElement element, string name )
        {
            var a = element.Attribute( XName.Get( name ) );
            return a != null ? a.Value : null;
        }

        public static void Attr( this XElement element, string name, string value )
        {
            var a = element.Attribute( XName.Get( name ) );
            if ( a != null )
            {
                a.SetValue( value );
            }
        }
    }
}