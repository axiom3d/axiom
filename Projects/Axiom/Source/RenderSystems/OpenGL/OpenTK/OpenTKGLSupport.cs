#region Namespace Declarations

using System;
using System.Collections.Generic;

using Axiom.Collections;
using Axiom.Configuration;
using Axiom.Core;
using Axiom.Graphics;

using OpenTK.Graphics;


#endregion Namespace Declarations

namespace Axiom.RenderSystems.OpenGL
{
    /// <summary>
    ///		Summary description for OpenTKGLSupport.
    /// </summary>
    internal class GLSupport : BaseGLSupport
    {
        private List<int> _fsaaLevels = new List<int>();

        public GLSupport()
            : base()
        {
        }

        #region BaseGLSupport Members

        public override void Start()
        {
            LogManager.Instance.Write("*** Starting OpenTKGL Subsystem ***");
        }

        public override void Stop()
        {
            LogManager.Instance.Write("*** Stopping OpenTKGL Subsystem ***");
        }

        /// <summary>
        ///		Returns the pointer to the specified extension function in the GL driver.
        /// </summary>
        /// <param name="extension"></param>
        /// <returns></returns>
        public override IntPtr GetProcAddress(string extension)
        {
            //return GL.GetAddress(extension);
            return IntPtr.Zero;
        }

        /// <summary>
        ///		
        /// </summary>
        public override void AddConfig()
        {
            ConfigOption optFullScreen = new ConfigOption( "Full Screen", "No", false );
            ConfigOption optVideoMode = new ConfigOption( "Video Mode", "800 x 600", false );
            ConfigOption optDisplayFrequency = new ConfigOption( "Display Frequency", "", false );
            ConfigOption optColorDepth = new ConfigOption( "Color Depth", "", false );
            ConfigOption optFSAA = new ConfigOption( "FSAA", "0", false );
            ConfigOption optVSync = new ConfigOption( "VSync", "No", false );
            ConfigOption optRTTMode = new ConfigOption( "RTT Preferred Mode", "FBO", false );

            // Full Screen
            optFullScreen.PossibleValues.Add( 0, "Yes" );
            optFullScreen.PossibleValues.Add( 1, "No" );

            // Video Mode
            #region Video Modes

            // get the available OpenGL resolutions
            DisplayDevice dev = DisplayDevice.Default;
            DisplayResolution[] res = dev.AvailableResolutions;

            optVideoMode = new ConfigOption( "Video Mode", "800 x 600 @ 32-bit colour", false );

            // add the resolutions to the config
            for ( int q = 0; q < res.Length; q++ )
            {
                if ( res[ q ].BitsPerPixel >= 16 )
                {
                    int width = res[ q ].Width;
                    int height = res[ q ].Height;

                    // filter out the lower resolutions and dupe frequencies
                    if ( width >= 640 && height >= 480 )
                    {
                        string query = string.Format( "{0} x {1} @ {2}-bit colour", width, height, res[ q ].BitsPerPixel );

                        if ( !optVideoMode.PossibleValues.Values.Contains( query ) )
                        {
                            // add a new row to the display settings table
                            optVideoMode.PossibleValues.Add( optVideoMode.PossibleValues.Count, query );
                        }
                        if ( optVideoMode.PossibleValues.Count == 1 && String.IsNullOrEmpty( optVideoMode.Value ) )
                        {
                            optVideoMode.Value = query;
                        }
                    }
                }
            }

            #endregion Video Modes

            // FSAA
            foreach ( int level in _fsaaLevels )
            {
                optFSAA.PossibleValues.Add( level, level.ToString() );
            }


            // VSync
            optVSync.PossibleValues.Add( 0, "Yes" );
            optVSync.PossibleValues.Add( 1, "No" );

            // RTTMode
            optRTTMode.PossibleValues.Add( 0, "FBO" );
            optRTTMode.PossibleValues.Add( 1, "PBuffer" );
            optRTTMode.PossibleValues.Add( 2, "Copy" );

            optFullScreen.ConfigValueChanged += new ConfigOption<string>.ValueChanged( _configOptionChanged );
            optVideoMode.ConfigValueChanged += new ConfigOption<string>.ValueChanged( _configOptionChanged );
            optDisplayFrequency.ConfigValueChanged += new ConfigOption<string>.ValueChanged( _configOptionChanged );
            optFSAA.ConfigValueChanged += new ConfigOption<string>.ValueChanged( _configOptionChanged );
            optVSync.ConfigValueChanged += new ConfigOption<string>.ValueChanged( _configOptionChanged );
            optColorDepth.ConfigValueChanged += new ConfigOption<string>.ValueChanged( _configOptionChanged );
            optRTTMode.ConfigValueChanged += new ConfigOption<string>.ValueChanged( _configOptionChanged );

            ConfigOptions.Add( optVideoMode );
            ConfigOptions.Add( optColorDepth );
            ConfigOptions.Add( optDisplayFrequency );
            ConfigOptions.Add( optFullScreen );
            ConfigOptions.Add( optFSAA );
            ConfigOptions.Add( optVSync );
            ConfigOptions.Add( optRTTMode );

            _refreshConfig();
        }

        /// <summary>
        ///		
        /// </summary>
        /// <param name="name"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="fullScreen"></param>
        /// <param name="left"></param>
        /// <param name="top"></param>
        /// <param name="depthBuffer"></param>
        /// <param name="vsync"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public override RenderWindow NewWindow(string name, int width, int height, bool fullScreen, NamedParameterList miscParams)
        {
            OpenTKWindow window = new OpenTKWindow();
            window.Create( name, width, height, fullScreen, miscParams );
            return window;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="autoCreateWindow"></param>
        /// <param name="renderSystem"></param>
        /// <param name="windowTitle"></param>
        /// <returns></returns>
        public override RenderWindow CreateWindow(bool autoCreateWindow, GLRenderSystem renderSystem, string windowTitle)
        {
            RenderWindow autoWindow = null;

            if ( autoCreateWindow )
            {
                int width = 800;
                int height = 600;
                int bpp = 32;
                bool fullScreen = false;

				ConfigOption optVM = ConfigOptions[ "Video Mode" ];
				string vm = optVM.Value;
				int pos = vm.IndexOf( 'x' );
				if ( pos == -1 )
					throw new Exception( "Invalid Video Mode provided" );
				width = int.Parse( vm.Substring( 0, vm.IndexOf( "x" ) ) );
				height = int.Parse( vm.Substring( vm.IndexOf( "x" ) + 1 ) );

				fullScreen = ( ConfigOptions[ "Full Screen" ].Value == "Yes" );

				NamedParameterList miscParams = new NamedParameterList();
				ConfigOption opt;

				opt = ConfigOptions[ "Color Depth" ];
				if ( opt != null && opt.Value != null && opt.Value.Length > 0 )
					miscParams.Add( "colorDepth", opt.Value );

				opt = ConfigOptions[ "VSync" ];
                if ( opt != null && opt.Value != null && opt.Value.Length > 0 )
				{
					miscParams.Add( "vsync", opt.Value );
					//TODO : renderSystem.WaitForVerticalBlank = (bool)opt.Value;
				}

				opt = ConfigOptions[ "FSAA" ];
                if ( opt != null && opt.Value != null && opt.Value.Length > 0 )
					miscParams.Add( "fsaa", opt.Value );

                miscParams.Add( "title", windowTitle );

                // create the window with the default form as the target
                autoWindow = renderSystem.CreateRenderWindow( windowTitle, width, height, fullScreen, miscParams );
            }

            return autoWindow;
        }

        #endregion BaseGLSupport Members

        #region Methods

        private void _configOptionChanged(string name, string value)
        {
            LogManager.Instance.Write( "OpenGL : RenderSystem Option: {0} = {1}", name, value );

            if ( name == "Video Mode" )
                _refreshConfig();

            if ( name == "Full Screen" )
            {
                ConfigOption opt = ConfigOptions[ "Display Frequency" ];
                if ( value == "No" )
                {
                    opt.Value = "N/A";
                    opt.Immutable = true;
                }
                else
                {
                    opt.Immutable = false;
                    opt.Value = opt.PossibleValues.Values[ opt.PossibleValues.Count - 1 ];
                }
            }
        }

        private void _refreshConfig()
        {

            ConfigOption optVideoMode = ConfigOptions[ "Video Mode" ];
            ConfigOption optColorDepth = ConfigOptions[ "Color Depth" ];
            ConfigOption optDisplayFrequency = ConfigOptions[ "Display Frequency" ];
            ConfigOption optFullScreen = ConfigOptions[ "Full Screen" ];

            string val = optVideoMode.Value;

            int pos = val.IndexOf( 'x' );
            if ( pos == -1 )
                throw new Exception( "Invalid Video Mode provided" );
            int width = Int32.Parse( val.Substring( 0, pos ) );
            int height = Int32.Parse( val.Substring( pos + 1 ) );

            //DisplayDevice device = DisplayDevice.Default;
            //device.AvailableResolutions.
            //optColorDepth.PossibleValues.Clear();
            //IntPtr videoInfoPtr = Sdl.SDL_GetVideoInfo();
            //Sdl.SDL_VideoInfo videoInfo = (Sdl.SDL_VideoInfo)Marshal.PtrToStructure( videoInfoPtr, typeof ( Sdl.SDL_VideoInfo ) );
            //IntPtr pixelFormatPtr = videoInfo.vfmt;
            //Sdl.SDL_PixelFormat pixelFormat = (Sdl.SDL_PixelFormat)Marshal.PtrToStructure( pixelFormatPtr, typeof ( Sdl.SDL_PixelFormat ) );
            //for ( int bpp = pixelFormat.BitsPerPixel, index = 0; bpp > 0; bpp -= 8, index++ )
            //{
            //    if ( Sdl.SDL_VideoModeOK( width, height, bpp, 0 ) != 0 )
            //        optColorDepth.PossibleValues.Add( index, bpp.ToString() );
            //}

            if ( optFullScreen.Value == "No" )
            {
                optDisplayFrequency.Value = "N/A";
                optDisplayFrequency.Immutable = true;
            }
            else
            {
                optDisplayFrequency.Immutable = false;
                optDisplayFrequency.Value = optDisplayFrequency.PossibleValues.Values[ optDisplayFrequency.PossibleValues.Count - 1 ];
            }
            if ( optColorDepth.PossibleValues.Values.Count > 0 )
                optColorDepth.Value = optColorDepth.PossibleValues.Values[ optColorDepth.PossibleValues.Values.Count - 1 ];
            if ( optDisplayFrequency.Value != "N/A" )
                optDisplayFrequency.Value = optDisplayFrequency.PossibleValues.Values[ optDisplayFrequency.PossibleValues.Count - 1 ];
        }

        #endregion Methods

    }
}
