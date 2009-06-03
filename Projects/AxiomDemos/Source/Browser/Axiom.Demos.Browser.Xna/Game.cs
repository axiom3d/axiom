#region Namespace Declarations

using System;
using System.IO;
using System.Globalization;
using System.Threading;

using Axiom.Demos;
using Axiom.Core;
using Axiom.Graphics;
using Axiom.Configuration;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using Axiom.Input;

#endregion Namespace Declarations

namespace Axiom.Demos.Browser.Xna
{
    /// <summary>
    /// Demo command line browser entry point.
    /// </summary>
    /// <remarks>
    /// This demo browser is implemented using a commandline interface. 
    /// </remarks>
    public partial class Game : IDisposable
    {
        protected const string CONFIG_FILE = @"EngineConfig.xml";

        private Root engine;

        string nextGame = "";

        partial void _setDefaultNextGame();

        private bool _configure()
        {
            // instantiate the Root singleton
            engine = new Root( CONFIG_FILE, "AxiomDemos.log" );

#if (XBOX || XBOX360 )
            ( new Axiom.RenderSystems.Xna.Plugin() ).Start();
#endif
            Root.Instance.RenderSystem = Root.Instance.RenderSystems[ 0 ];
            _setupResources();

            engine.FrameStarted += new FrameEvent( engine_FrameStarted );

            return true;
        }

        void engine_FrameStarted( object source, FrameEventArgs e )
        {
            Axiom.Overlays.OverlayManager.Instance.GetByName( "Core/XnaOverlay" ).Show();
            engine.FrameStarted -= new FrameEvent( engine_FrameStarted );
        }

        /// <summary>
        ///		Loads default resource configuration if one exists.
        /// </summary>
        private void _setupResources()
        {
            ResourceManager.AddCommonArchive( "Content\\Fonts", "Folder" );
#if !( XBOX || XBOX360 )
            ResourceManager.AddCommonArchive( "Content\\Icons", "Folder" );
            ResourceManager.AddCommonArchive( "Content\\BrowserImages", "Folder" );
            ResourceManager.AddCommonArchive( "Content\\XNA.Materials\\x86\\scripts", "Folder" );
            ResourceManager.AddCommonArchive( "Content\\XNA.Materials\\x86\\programs", "Folder" );
            //ResourceManager.AddCommonArchive( "Content\\XNA.Materials\\x86\\textures", "Folder" );

            ResourceManager.AddCommonArchive( "Content\\XNA.Materials\\x86\\Fresnel.zip", "ZipFile" );
#else
            //ResourceManager.AddCommonArchive( "Content\\XNA.Materials\\XBox", "Folder" );
            ResourceManager.AddCommonArchive("Content\\XNA.Materials\\XBox\\scripts", "Folder");
            ResourceManager.AddCommonArchive("Content\\XNA.Materials\\XBox\\programs", "Folder");
            ResourceManager.AddCommonArchive("Content\\XNA.Materials\\XBox\\Textures", "Folder");
#endif
            ResourceManager.AddCommonArchive( "Content\\Meshes", "Folder" );
            ResourceManager.AddCommonArchive( "Content\\Overlays", "Folder" );
            ResourceManager.AddCommonArchive( "Content\\Skeletons", "Folder" );
            ResourceManager.AddCommonArchive( "Content\\Textures", "Folder" );
#if !( XBOX || XBOX360 )
            ResourceManager.AddCommonArchive( "Content\\Textures\\Skyboxes.zip", "ZipFile" );
            ResourceManager.AddCommonArchive( "Content\\Archives\\chiropteraDM.zip", "ZipFile" );
            ResourceManager.AddCommonArchive("Content\\Archives\\Water.zip", "ZipFile");
#endif

        }

        public void Run()
        {
            try
            {
                if ( _configure() )
                {
                    Assembly demos = Assembly.LoadFrom( "Axiom.Demos.dll" );

                    _setDefaultNextGame();

#if !(XBOX || XBOX360 || SILVERLIGHT)
                    Type[] demoTypes = demos.GetTypes();
                    Type techDemo = demos.GetType("Axiom.Demos.TechDemo");
                    List<string> demoList=new List<string>();
                    foreach (Type demoType in demoTypes)
                    {
                        if (demoType.IsSubclassOf(techDemo))
                        {
                            demoList.Add(demoType.Name);
                        }
                    }
                    demoList.Sort();

                    int i = 1;
                    foreach (string typeName in demoList)
                    {
                        Console.WriteLine("{0}) {1}", i++, typeName);
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
                            nextGame = (string)demoList[number - 1];
                            break;
                        }
                    }
#endif

                    Type type = demos.GetType( "Axiom.Demos." + nextGame );

                    if ( type != null )
                    {
                        using ( TechDemo demo = (TechDemo)Activator.CreateInstance( type ) )
                        {
                            demo.SetupInput += new TechDemo.ConfigureInput( _setupInput );
                            demo.Start();//show and start rendering
                        }//dispose of it when done
                    }
                }
                
            }
            catch ( Exception caughtException )
            {
                LogManager.Instance.Write( BuildExceptionString( caughtException ) );
            }
        }

        static InputReader _setupInput()
        {
            return new XBoxInput();
        }

        #region Main

#if !(XBOX || XBOX360)
        [STAThread]
#endif
        private static void Main( string[] args )
        {
            try
            {
                using ( Game main = new Game() )
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