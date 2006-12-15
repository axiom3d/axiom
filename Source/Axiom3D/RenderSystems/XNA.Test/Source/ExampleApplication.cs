// This file was created from ExampleApplication.h (Ogle v1.2.3) by trejs
// for use with Axiom's tutorials.
//
// It still has some flaws. See the "// TODO" lines to begin with.
// But better that than no tutorials at all :)

#region Namespace Declarations
using System;
using System.Data;
using System.Collections.Generic;

using Axiom;
using Axiom.Core;
using Axiom.Graphics;
using Axiom.Configuration;
using Axiom.Math;
using Axiom.Overlays;
using Axiom.Input;
#endregion Namespace Declarations

namespace ExampleApplication
{
    public class ExampleApplication
    {
        #region ConfigureConsole
        // Code originally from the command-line demo launcher. Mostly rewritten.
        //
        // If you only want the configuration menu, just copy this class into your application.
        // See how it's used in ExampleApplication.Configure().
        private class ConfigureConsole
        {
            #region Types
            public enum DialogResult
            {
                Continue,
                Run,
                Exit
            }
            #endregion

            #region Private fields
            // Currently selected system
            private RenderSystem m_currentSystem;

            // Holds the list of possible rendersystems
            private ConfigOption m_renderSystems;

            // Menu items
            private List<ConfigOption> m_currentMenuItems = new List<ConfigOption>();

            // Currently selected item
            private ConfigOption m_currentOption;

            // Options for the currently selected system
            private List<ConfigOption> m_currentSystemOptions = new List<ConfigOption>();
            #endregion

            #region Properties
            /// <summary>
            /// Gets the selected rendersystem (and all the values of options set for it)
            /// </summary>
            public RenderSystem RenderSystem
            {
                get
                {
                    return m_currentSystem;
                }
            }
            #endregion

            #region Constructor
            public ConfigureConsole()
            {
                // Set current RenderSystem to first in list
                m_currentSystem = Root.Instance.RenderSystems[ 0 ];

                // Create rendersystem option and get possible values
                m_renderSystems = new ConfigOption( "Render System", m_currentSystem.Name, false );
                foreach ( RenderSystem r in Root.Instance.RenderSystems )
                    m_renderSystems.PossibleValues.Add( r );

                // Build list of options for the currently selected rendersystem
                BuildCurrentSystemOptions();
            }
            #endregion

            #region Public methods
            public DialogResult Show()
            {
                while ( true )
                {
                    // Clear menu buffer
                    m_currentMenuItems.Clear();

                    // Build menu
                    if ( m_currentOption.Name == null ) // Main-menu
                    {

                        // Build menu from m_currentOptions
                        foreach ( ConfigOption c in m_currentSystemOptions )
                            m_currentMenuItems.Add( c );
                    }
                    else // Option menu 
                    {
                        // Add possible values for this option
                        foreach ( object value in m_currentOption.PossibleValues )
                            m_currentMenuItems.Add( new ConfigOption( value.ToString(), "", false ) );
                    }

                    // Display menu
                    DisplayOptions();

                    // Handle next keypress (waits for user input)
                    DialogResult result = HandleNextKeyPress();

                    // Check if we're exiting the configure console
                    if ( result != DialogResult.Continue )
                    {
                        Console.Clear();
                        return result;
                    }
                }
            }
            #endregion

            #region Private methods
            private void BuildCurrentSystemOptions()
            {
                // Make shure it's empty
                m_currentSystemOptions.Clear();

                // Add rendersystem options to the list
                m_currentSystemOptions.Add( m_renderSystems );

                // Browse and add options of current rendersystem
                foreach ( ConfigOption c in m_currentSystem.ConfigOptions )
                    m_currentSystemOptions.Add( c );
            }

            private bool IsDigit( ref int digit, ConsoleKey key )
            {
                string str = key.ToString();
                if ( str.Length == 2 && char.IsDigit( str[ 1 ] ) )
                {
                    digit = int.Parse( str.Substring( 1 ) ); // Numbers are returned like "D5"
                    return true;
                }
                else
                    return false;
            }

            private DialogResult HandleNextKeyPress()
            {
                // Wait for key and then read it without echo to the console
                ConsoleKey key = Console.ReadKey( true ).Key;

                // If the currentOption.Name is null, then we're in the main-menu
                if ( m_currentOption.Name == null )
                {
                    // Escape exits, enter runs
                    if ( key == ConsoleKey.Escape )
                        return DialogResult.Exit;
                    else if ( key == ConsoleKey.Enter )
                    {
                        // Save options for current system
                        for ( int i = 0; i < m_currentSystemOptions.Count; i++ )
                        {
                            ConfigOption opt = m_currentSystemOptions[ i ];
                            m_currentSystem.ConfigOptions[ opt.Name ] = opt;
                        }

                        return DialogResult.Run;
                    }
                    else
                    {
                        // Check for selection
                        int selection = 0;
                        if ( IsDigit( ref selection, key ) && selection < m_currentMenuItems.Count )
                            m_currentOption = (ConfigOption)m_currentMenuItems[ selection ];
                    }
                }
                else
                {
                    // Esc: Return to main-menu
                    if ( key == ConsoleKey.Escape )
                        m_currentOption.Name = null;

                    // Check if the current key is a digit, and if that digit is within the possible values
                    int selection = 0;
                    if ( IsDigit( ref selection, key ) && selection < m_currentOption.PossibleValues.Count )
                    {
                        // Check if we're about to change render system
                        if ( m_currentOption.Name == "Render System" )
                        {
                            m_currentSystem = (RenderSystem)m_currentOption.PossibleValues[ selection ];
                            m_renderSystems = m_currentOption;
                            BuildCurrentSystemOptions();

                            m_currentOption.Name = null; // Reset current option (to show main-menu)
                        }
                        else
                        {
                            m_currentOption.Value = (string)m_currentOption.PossibleValues[ selection ];

                            // Set the selected value
                            for ( int i = 0; i < m_currentSystemOptions.Count; i++ )
                                if ( m_currentSystemOptions[ i ].Name == m_currentOption.Name )
                                    m_currentSystemOptions[ i ] = m_currentOption;

                            m_currentOption.Name = null; // Reset current option (to show main-menu)
                        }
                    }
                }

                return DialogResult.Continue;
            }

            private void DisplayOptions()
            {
                Console.Clear();
                Console.WriteLine( "Axiom Engine Configuration" );
                Console.WriteLine( "==========================" );

                if ( m_currentOption.Name != null )
                {
                    Console.WriteLine( "Available settings for {0}.\n", m_currentOption.Name );
                }

                // Load Render Subsystem Options
                int i = 0;
                foreach ( object o in m_currentMenuItems )
                {
                    // If this is a possible-value of an option (and not a list of options in the main-menu)
                    // we need to display it's name but not the current value (it doesn't have any).
                    if ( m_currentOption.Name == null )
                        Console.WriteLine( "{0}      | {1}", i++, o );
                    else
                        Console.WriteLine( "{0}      | {1}", i++, ( (ConfigOption)o ).Name );
                }

                if ( m_currentOption.Name == null )
                {
                    Console.WriteLine();
                    Console.WriteLine( "Enter  | Saves changes." );
                    Console.WriteLine( "ESC    | Exits." );
                }

                Console.Write( "\nSelect option: " );
            }
            #endregion
        }
        #endregion

        #region Private fields
        private string m_configFile = "EngineConfig.xml";
        private string m_logFile = "AxiomExample.log";
        private long m_lastOverlayUpdate = -1000;
        private float m_moveScale = 0, m_rotScale = 0, m_moveSpeed = 100, m_rotateSpeed = 36;
        private Vector2 m_rotateVector = new Vector2( 0, 0 );
        private Vector3 m_translateVector = new Vector3( 0, 0, 0 );
        private Root m_root;
        private Camera m_camera;
        private SceneManager m_sceneManager;
        private RenderWindow m_renderWindow;
        private InputReader m_inputReader;
        #endregion Private fields

        #region Protected properties
        /// <summary>
        /// Root (Axiom.Core.Root)
        /// </summary>
        protected Root Root
        {
            get
            {
                return m_root;
            }
            set
            {
                m_root = value;
            }
        }

        /// <summary>
        /// Camera (Axiom.Core.Camera)
        /// </summary>
        protected Camera Camera
        {
            get
            {
                return m_camera;
            }
            set
            {
                m_camera = value;
            }
        }

        /// <summary>
        /// SceneManager (Axiom.Core.SceneManager)
        /// </summary>
        protected SceneManager SceneManager
        {
            get
            {
                return m_sceneManager;
            }
            set
            {
                m_sceneManager = value;
            }
        }


        /// <summary>
        /// RenderWindow (Axiom.Graphics.RenderWindow)
        /// </summary>
        protected RenderWindow RenderWindow
        {
            get
            {
                return m_renderWindow;
            }
            set
            {
                m_renderWindow = value;
            }
        }

        /// <summary>
        /// InputReader (Axiom.Input.InputReader)
        /// </summary>
        protected InputReader InputReader
        {
            get
            {
                return m_inputReader;
            }
            set
            {
                m_inputReader = value;
            }
        }
        #endregion Protected fields

        #region Public properties
        /// <summary>
        /// Gets or set the config file name and path
        /// </summary>
        public string ConfigFile
        {
            get
            {
                return m_configFile;
            }
            set
            {
                m_configFile = value;
            }
        }

        /// <summary>
        /// Gets or sets the config file name and path
        /// </summary>
        public string LogFile
        {
            get
            {
                return m_logFile;
            }
            set
            {
                m_logFile = value;
            }
        }
        #endregion

        #region Init methods
        /// <summary>
        /// Starts the example
        /// </summary>
        public virtual void Run()
        {
            try
            {
                if ( Setup() )
                    m_root.StartRendering();
            }
            catch ( System.Reflection.ReflectionTypeLoadException ex )
            {
                // This catches directx missing (or too old) to log :)
                for ( int i = 0; i < ex.LoaderExceptions.Length; i++ )
                    if ( LogManager.Instance != null )
                        LogManager.Instance.Write( ex.LoaderExceptions[ i ].Message );
            }
            catch ( Exception ex )
            {
                if ( LogManager.Instance != null )
                    LogManager.Instance.Write( ex.ToString() );
            }

            // TODO: Memory cleanup here..
        }

        /// <summary>
        /// Initalizes the application
        /// </summary>
        /// <returns>True if successfull, False to exit the application</returns>
        protected virtual bool Setup()
        {
            m_root = new Root( ConfigFile, LogFile );

            SetupResources();

            // Run config utility, exit program if it returns false
            if ( !Configure() )
                return false;
            else
                m_renderWindow = Root.Instance.Initialize( true , "Axoim Xna Render Window");

            // Initalize input
            m_inputReader = PlatformManager.Instance.CreateInputReader();
            m_inputReader.Initialize( m_renderWindow, true, true, false, true );

            ChooseSceneManager();
            CreateCamera();
            CreateViewports();

            // Set default mipmap level (NB some APIs ignore this)
            TextureManager.Instance.DefaultNumMipMaps = 5;

            // Create any resource listeners (for loading screens)
            CreateResourceListener();

            // Add some event handlers
            RegisterEventHandlers();

            // Lastly, create the scene
            CreateScene();

            return true;
        }

        /// <summary>
        /// Adds the searchpaths from "EngineConfig.xml" to resources
        /// </summary>
        protected virtual void SetupResources()
        {
            EngineConfig config = new EngineConfig();

            config.ReadXml( ConfigFile );

            ResourceManager.AddCommonArchive( "../../../Media", "Folder" );

            foreach ( EngineConfig.FilePathRow row in config.FilePath )
                ResourceManager.AddCommonArchive( row.src, row.type );
        }

        /// <summary>
        /// Configures the application
        /// </summary>
        /// <returns>True if successfull, False to exit the application</returns>
        protected virtual bool Configure()
        {
            ConfigureConsole cc = new ConfigureConsole();
            if ( cc.Show() == ConfigureConsole.DialogResult.Exit )
                return false;

            // Set selected rendersystem
            m_root.RenderSystem = cc.RenderSystem;

            return true;
        }

        /// <summary>
        /// Chooses scene manager (SceneType.Generic)
        /// </summary>
        protected virtual void ChooseSceneManager()
        {
            // Create a generic scene manager
            m_sceneManager = m_root.SceneManagers.GetSceneManager( SceneType.Generic );
        }

        /// <summary>
        /// Creates a camera at 500 in the Z direction that looks at -300 in the Z direction
        /// </summary>
        protected virtual void CreateCamera()
        {
            // Create the camera
            m_camera = m_sceneManager.CreateCamera( "PlayerCam" );

            // Position it at 500 in the Z direction
            m_camera.Position = new Vector3( 0, 0, 500 );

            // Look back along -Z
            m_camera.LookAt( new Vector3( 0, 0, -300 ) );
            m_camera.Near = 5;
        }

        /// <summary>
        /// Creates a viewport using mCamera
        /// </summary>
        protected virtual void CreateViewports()
        {
            // Create one viewport, entire window
            Viewport vp = m_renderWindow.AddViewport( m_camera );
            vp.BackgroundColor = ColorEx.Black;

            // Alter the camera aspect ratio to match the viewport
            m_camera.AspectRatio = vp.ActualWidth / vp.ActualHeight;
        }

        /// <summary>
        /// Optional override method where you can create resource listeners (e.g. for loading screens)
        /// </summary>
        protected virtual void CreateResourceListener()
        {

        }

        /// <summary>
        /// Registers event handlers and calls InitOverlay()
        /// </summary>
        protected virtual void RegisterEventHandlers()
        {
            m_root.FrameStarted += UpdateInput;
            m_root.FrameStarted += UpdateOverlay;
            m_root.FrameStarted += FrameStarted;
            m_root.FrameEnded += FrameEnded;

            // Create debug overlay
            InitOverlay();
        }

        /// <summary>
        /// Initalizes the debug overlay (fps, etc..)
        /// </summary>
        protected virtual void InitOverlay()
        {
            Overlay o = OverlayManager.Instance.GetByName( "Core/DebugOverlay" );
            if ( o == null )
                throw new Exception( "Could not find overlay named 'Core/DebugOverlay'." );
            o.Show();
        }

        /// <summary>
        /// Creates the scene
        /// </summary>
        protected virtual void CreateScene()
        {

        }
        #endregion Init methods

        #region Event handlers
        /// <summary>
        /// This is run before each frame
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        protected virtual void FrameStarted( object source, FrameEventArgs e )
        {

        }

        /// <summary>
        /// This is run after each frame
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        protected virtual void FrameEnded( object source, FrameEventArgs e )
        {

        }

        /// <summary>
        /// Checks for input and handles it
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        protected virtual void UpdateInput( object source, FrameEventArgs e )
        {
            m_inputReader.Capture();

            #region Camera movement
            // Reset vectors
            m_rotateVector.x = m_translateVector.x = 0;
            m_rotateVector.y = m_translateVector.y = 0;
            m_translateVector.z = 0;

            // Move
            m_moveScale = m_moveSpeed * e.TimeSinceLastFrame;

            // Rotate
            m_rotScale = m_rotateSpeed * e.TimeSinceLastFrame;

            // Move forward and back
            if ( m_inputReader.IsKeyPressed( KeyCodes.W ) || m_inputReader.IsKeyPressed( KeyCodes.Up ) )
                m_translateVector.z = -m_moveScale;
            else if ( m_inputReader.IsKeyPressed( KeyCodes.S ) || m_inputReader.IsKeyPressed( KeyCodes.Down ) )
                m_translateVector.z = m_moveScale;

            // Move left and right
            if ( m_inputReader.IsKeyPressed( KeyCodes.A ) )
                m_translateVector.x = -m_moveScale;
            else if ( m_inputReader.IsKeyPressed( KeyCodes.D ) )
                m_translateVector.x = m_moveScale;

            // Move up and down
            if ( m_inputReader.IsKeyPressed( KeyCodes.PageUp ) )
                m_translateVector.y = m_moveScale;
            else if ( m_inputReader.IsKeyPressed( KeyCodes.PageDown ) )
                m_translateVector.y = -m_moveScale;

            // Rotate left and right
            if ( m_inputReader.IsKeyPressed( KeyCodes.Left ) )
                m_rotateVector.x = -m_rotScale;
            else if ( m_inputReader.IsKeyPressed( KeyCodes.Right ) )
                m_rotateVector.x = m_rotScale;

            // Right mouse button pressed
            if ( m_inputReader.IsMousePressed( MouseButtons.Right ) )
            {
                // Translate
                m_translateVector.x += m_inputReader.RelativeMouseX * 0.13f;
                m_translateVector.y -= m_inputReader.RelativeMouseY * 0.13f;
            }
            else
            {
                // Apply mouse rotation
                m_rotateVector.x += m_inputReader.RelativeMouseX * 0.13f;
                m_rotateVector.y += m_inputReader.RelativeMouseY * 0.13f;
            }

            // Apply changes
            m_camera.Yaw( -m_rotateVector.x );
            m_camera.Pitch( -m_rotateVector.y );
            m_camera.MoveRelative( m_translateVector );
            #endregion Camera movement

            // TODO: what about window-closing-event?
            if ( m_inputReader.IsKeyPressed( KeyCodes.Escape ) )
            {
                Root.Instance.QueueEndRendering();

                // TODO: Find a better way
                if ( m_root != null )
                {
                    // remove event handlers
                    m_root.FrameStarted -= UpdateInput;
                    m_root.FrameStarted -= UpdateOverlay;
                    m_root.FrameStarted -= FrameStarted;
                    m_root.FrameEnded -= FrameEnded;

                    m_root.Dispose();
                }

                m_sceneManager.RemoveAllCameras();
                m_sceneManager.RemoveCamera( m_camera );
                m_camera = null;
                Root.Instance.RenderSystem.DetachRenderTarget( m_renderWindow );
                m_renderWindow.Dispose();
                return;
            }

        }

        /// <summary>
        /// Updates the debug overlay
        /// </summary>
        protected virtual void UpdateOverlay( object source, FrameEventArgs e )
        {
            if ( Root.Instance.Timer.Milliseconds - m_lastOverlayUpdate >= 1000 )
            {
                m_lastOverlayUpdate = Root.Instance.Timer.Milliseconds;

                OverlayElement element = OverlayElementManager.Instance.GetElement( "Core/DebugText" );
                element.Text = m_renderWindow.DebugText;

                element = OverlayElementManager.Instance.GetElement( "Core/CurrFps" );
                element.Text = string.Format( "Current FPS: {0:#.00}", Root.Instance.CurrentFPS );

                element = OverlayElementManager.Instance.GetElement( "Core/BestFps" );
                element.Text = string.Format( "Best FPS: {0:#.00}", Root.Instance.BestFPS );

                element = OverlayElementManager.Instance.GetElement( "Core/WorstFps" );
                element.Text = string.Format( "Worst FPS: {0:#.00}", Root.Instance.WorstFPS );

                element = OverlayElementManager.Instance.GetElement( "Core/AverageFps" );
                element.Text = string.Format( "Average FPS: {0:#.00}", Root.Instance.AverageFPS );

                element = OverlayElementManager.Instance.GetElement( "Core/NumTris" );
                element.Text = string.Format( "Triangle Count: {0}", m_sceneManager.TargetRenderSystem.FacesRendered );
                LogManager.Instance.Write( "Engine Statistics: Count: {5}  FPS <C,B,W,A>: {0:#.00} {1:#.00} {2:#.00} {3:#.00}  Trias: {4} ", Root.Instance.CurrentFPS, Root.Instance.BestFPS, Root.Instance.WorstFPS, Root.Instance.AverageFPS, m_sceneManager.TargetRenderSystem.FacesRendered, Root.Instance.CurrentFrameCount );
            }
        }
        #endregion Events
    }
}
