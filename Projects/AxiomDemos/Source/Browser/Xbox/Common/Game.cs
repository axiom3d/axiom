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
using Microsoft.Xna.Framework.Storage;

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
        private Root engine;

        string nextGame = "";
        string titleLocation;

        partial void _setDefaultNextGame();
        partial void _setupResources();
        partial void _loadPlugins();

        private bool _configure()
        {

            // instantiate the Root singleton
            engine = new Root( titleLocation + "AxiomDemos.log" );

            ( new Axiom.RenderSystems.Xna.Plugin() ).Initialize();

            Root.Instance.RenderSystem = Root.Instance.RenderSystems[ "Xna" ];
            Root.Instance.RenderSystem.ConfigOptions[ "Use Content Pipeline" ].Value = "Yes";
            Root.Instance.RenderSystem.ConfigOptions[ "Video Mode" ].Value = "1280 x 720 @ 32-bit color";

            _loadPlugins();

            _setupResources();

            engine.FrameStarted += engine_FrameStarted;

            return true;
        }

        void engine_FrameStarted( object source, FrameEventArgs e )
        {
            //Axiom.Overlays.OverlayManager.Instance.GetByName( "Core/XnaOverlay" ).Show();
            engine.FrameStarted -= engine_FrameStarted;
        }


        public void Run()
        {
#if !( XBOX || XBOX360 )
            titleLocation = String.Empty;
#else
            titleLocation = StorageContainer.TitleLocation + "\\";
#endif

            try
            {
                if ( _configure() )
                {
                    Assembly demos = Assembly.LoadFrom( titleLocation + "Axiom.Demos.dll" );

                    _setDefaultNextGame();

                    Type type;

                    type = Assembly.GetExecutingAssembly().GetType( "Axiom.Demos.Browser.Xna." + nextGame );

                    if ( type == null )
                    {
                        type = demos.GetType( "Axiom.Demos." + nextGame );
                    }

                    if ( type != null )
                    {
                        using ( TechDemo demo = (TechDemo)Activator.CreateInstance( type ) )
                        {
                            demo.SetupInput = new TechDemo.ConfigureInput( _setupInput );
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