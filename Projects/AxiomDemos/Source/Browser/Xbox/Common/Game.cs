#region Namespace Declarations

using System;
using System.Reflection;
using Axiom.Core;
using Axiom.Input;
using Axiom.RenderSystems.Xna;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Input;

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

        string nextGame = String.Empty;
        private InputReader _input;

#if WINDOWS_PHONE
        private Axiom.Graphics.RenderWindow _window;
        private Microsoft.Xna.Framework.Graphics.GraphicsDevice _xnaDevice;

        public Game( Microsoft.Xna.Framework.Graphics.GraphicsDevice device )
        {
            _xnaDevice = device;
        }
#else
        public Game(Microsoft.Xna.Framework.Graphics.GraphicsDevice device = null)
        {
        }
#endif

        partial void _setDefaultNextGame();

        partial void _setupResources();

        partial void _loadPlugins();

        private bool _configure()
        {
            new XnaResourceGroupManager();

            // instantiate the Root singleton

            engine = new Root( "AxiomDemos.log" );

#if (XBOX || XBOX360) || WINDOWS_PHONE
            ( new Axiom.RenderSystems.Xna.Plugin() ).Initialize();
#endif
            Root.Instance.RenderSystem = Root.Instance.RenderSystems[ "Xna" ];

            Root.Instance.RenderSystem.ConfigOptions[ "Use Content Pipeline" ].Value = "Yes";
            Root.Instance.RenderSystem.ConfigOptions[ "Video Mode" ].Value = "1280 x 720 @ 32-bit color";

#if WINDOWS_PHONE
            engine.Initialize( false, "Axiom Demos" );
            var parms = new Collections.NamedParameterList();
            parms.Add( "xnaGraphicsDevice", _xnaDevice );
            _window = engine.CreateRenderWindow( "Axiom Demos", 480, 800, true, parms );
#endif

            _setupResources();

            engine.FrameStarted += engine_FrameStarted;

            return true;
        }

        void engine_FrameStarted( object source, FrameEventArgs e )
        {
            if ( _input.IsKeyPressed( KeyCodes.G ) && !Guide.IsVisible )
            {
                Guide.ShowSignIn( 1, false );
            }
            if ( GamePad.GetState( 0 ).IsButtonDown( Buttons.Back ) )
            {
                e.StopRendering = true;
            }
        }

        public void Run()
        {
            try
            {
                if ( _configure() )
                {
#if !WINDOWS_PHONE
                    Assembly demos = Assembly.LoadFrom( "Axiom.Demos.dll" );
#endif
                    _setDefaultNextGame();

                    Type type;

                    type = Assembly.GetExecutingAssembly().GetType( "Axiom.Demos.Browser.Xna." + nextGame );

                    if ( type == null )
                    {
#if !WINDOWS_PHONE
                        type = demos.GetType( "Axiom.Demos." + nextGame );
#else
                        //TODO
                        type = typeof( Axiom.Demos.CameraTrack );
#endif
                    }

                    if ( type != null )
                    {
                        using ( TechDemo demo = (TechDemo)Activator.CreateInstance( type ) )
                        {
#if WINDOWS_PHONE
                            demo.Window = _window;
#endif
                            demo.SetupInput = new TechDemo.ConfigureInput( _setupInput );
                            demo.Start();//show and start rendering
                        }//dispose of it when done
                    }
                }
            }
            catch ( Exception caughtException )
            {
                LogManager.Instance.Write( BuildExceptionString( caughtException ) );
                throw;
            }
        }

        private InputReader _setupInput()
        {
            _input = new XBoxInput();
            return _input;
        }

        #region Main

#if !WINDOWS_PHONE

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
#endif

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

        #endregion IDisposable Members
    }
}