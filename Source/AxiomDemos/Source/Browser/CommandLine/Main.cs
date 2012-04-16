#region Namespace Declarations

using System;
using System.IO;
using System.Globalization;
using System.Threading;

using Axiom.Collections;
using Axiom.Demos;
using Axiom.Core;
using Axiom.Graphics;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

#endregion Namespace Declarations

namespace Axiom.Demos.Browser.CommandLine
{
    using Configuration;

    /// <summary>
    /// Demo command line browser entry point.
    /// </summary>
    /// <remarks>
    /// This demo browser is implemented using a commandline interface. 
    /// </remarks>
    public class Program : IDisposable
    {
        private struct DemoItem
        {
            public DemoItem(string name, Type demo)
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

        protected const string CONFIG_FILE = @"EngineConfig.xml";

        private Root engine;

#if !(XBOX || XBOX360 || SILVERLIGHT)

        private bool _configure( )
        {
            // instantiate the Root singleton
            engine = new Root( "AxiomDemos.log" );

            _setupResources();

            // HACK: Temporary
            ConfigDialog dlg = new ConfigDialog();
            DialogResult result = dlg.ShowDialog();
			if ( result == DialogResult.Cancel )
			{
				Root.Instance.Dispose();
				engine = null;
				return false;
			}

            return true;
        }
#endif

        /// <summary>
        ///		Loads default resource configuration if one exists.
        /// </summary>
        private void _setupResources()
        {
            string resourceConfigPath = Path.GetFullPath( CONFIG_FILE );

            if ( File.Exists( resourceConfigPath ) )
            {
                EngineConfig config = new EngineConfig();

                // load the config file
                // relative from the location of debug and releases executables
                config.ReadXml( CONFIG_FILE );

                // interrogate the available resource paths
                foreach ( EngineConfig.FilePathRow row in config.FilePath )
                {
                    ResourceGroupManager.Instance.AddResourceLocation( Path.GetFullPath( row.src ), row.type, row.group );
                }
            }
        }

        public void Run( )
        {
#if !(XBOX || XBOX360 || SILVERLIGHT)
            AxiomSortedCollection<string, DemoItem> demoList = new AxiomSortedCollection<string, DemoItem>();
#endif
            try
            {
#if !(XBOX || XBOX360 || SILVERLIGHT)
                if (_configure())
                {

                    Assembly demos = Assembly.LoadFrom("Axiom.Demos.dll");
                    Type[] demoTypes = demos.GetTypes();
                    Type techDemo = demos.GetType("Axiom.Demos.TechDemo");

                    foreach (Type demoType in demoTypes)
                    {
                        if (demoType.IsSubclassOf(techDemo))
                        {
                            demoList.Add(demoType.Name, new DemoItem(demoType.Name, demoType));
                        }
                    }

                    {
                        Type demoType;
                        int i = 1;
                        foreach (KeyValuePair<string, DemoItem> typeName in demoList)
                        {
                            Console.WriteLine("{0}) {1}", i++, typeName.Key);
                        }
                        Console.WriteLine("Enter the number of the demo that you want to run and press enter.");
                        while (true)
                        {
                            string line = Console.ReadLine();
                            int number = -1;
                            if (line != string.Empty)
                            {
                                number = int.Parse(line.Trim());
                            }
                            if (number < 1 || number > demoList.Count)
                                Console.WriteLine("The number of the demo game must be between 1 and {0}, the number of demos games available.", demoList.Count);
                            else
                            {
                                demoType = demoList.Values[number - 1].Demo;
                                break;
                            }
                        }

                        if (demoType != null)
                        {
                            using (TechDemo demo = (TechDemo)Activator.CreateInstance(demoType))
                            {
                                demo.SetupResources();
                                demo.Start();//show and start rendering
                            }//dispose of it when done
                        }
                    }

                }
#else
                            using ( TechDemo demo = (TechDemo)(new Axiom.Demos.SkyPlane()))
                            {
                                demo.Start();//show and start rendering
                            }//dispose of it when done
#endif
            }
            catch ( Exception caughtException )
            {
                LogManager.Instance.Write( BuildExceptionString( caughtException ) );
            }
        }

        #region Main

        [STAThread]
        private static void Main( string[] args )
        {
            try
            {
                using ( Program main = new Program() )
                {
                    main.Run();//show and start rendering
                }//dispose of it when done
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
            string errMessage = string.Empty;

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
            string errMessage = string.Empty;

            errMessage += Environment.NewLine + " InnerException ";
            errMessage += Environment.NewLine + innerException.Message + Environment.NewLine + innerException.StackTrace;

            return errMessage;
        }

        #endregion Main

        #region IDisposable Members

        public void Dispose()
        {
            //throw new Exception( "The method or operation is not implemented." );
        }

        #endregion
    }
}