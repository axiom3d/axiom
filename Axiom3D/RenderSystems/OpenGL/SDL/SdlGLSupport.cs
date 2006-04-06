using System;
using System.Data;

using Axiom;

using Tao.OpenGl;
using Tao.Sdl;

namespace Axiom.RenderSystems.OpenGL
{
    /// <summary>
    ///		Summary description for SdlGLSupport.
    /// </summary>
    public class GLSupport : BaseGLSupport
    {
        public GLSupport()
            : base()
        {
            Sdl.SDL_Init( Sdl.SDL_INIT_VIDEO );
        }

        #region BaseGLSupport Members

        /// <summary>
        ///		Returns the pointer to the specified extension function in the GL driver.
        /// </summary>
        /// <param name="extension"></param>
        /// <returns></returns>
        public override IntPtr GetProcAddress( string extension )
        {
            return Sdl.SDL_GL_GetProcAddress( extension );
        }

        /// <summary>
        ///		
        /// </summary>
        public override void AddConfig()
        {
            ConfigOption option;
            
            // Full Screen
            option = new ConfigOption( "Full Screen", "Yes", false );
            option.PossibleValues.Add( "Yes" );
            option.PossibleValues.Add( "No" );
            ConfigOptions.Add( option );
            
            // Video Mode
            // get the available OpenGL resolutions
            Sdl.SDL_Rect[] modes = Sdl.SDL_ListModes( IntPtr.Zero, Sdl.SDL_FULLSCREEN | Sdl.SDL_OPENGL );

            option = new ConfigOption( "Video Mode", "", false );
            // add the resolutions to the config
            foreach ( Sdl.SDL_Rect mode in modes )
            {
                int width = mode.w;
                int height = mode.h;
                
                // filter out the lower resolutions and dupe frequencies
                if ( width >= 640 && height >= 480)
                {
                    string query = string.Format( "{0} x {1} @ {2}-bit colour", width, height, 32 );

                    if ( !option.PossibleValues.Contains( query ) )
                    {
                        // add a new row to the display settings table
                        option.PossibleValues.Add( query );
                    }
                    if ( option.PossibleValues.Count == 1 )
                    {
                        option.Value = query;
                    }
                }
            }
            ConfigOptions.Add( option );

            option = new ConfigOption( "FSAA", "0", false );
            option.PossibleValues.Add( "0" );
            option.PossibleValues.Add( "2" );
            option.PossibleValues.Add( "4" );
            option.PossibleValues.Add( "6" );
            ConfigOptions.Add( option );
            
        }

        /// <summary>
        ///		
        /// </summary>
        /// <param name="name"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="colorDepth"></param>
        /// <param name="fullScreen"></param>
        /// <param name="left"></param>
        /// <param name="top"></param>
        /// <param name="depthBuffer"></param>
        /// <param name="vsync"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public override RenderWindow NewWindow( string name, int width, int height, int colorDepth, bool fullScreen, int left, int top, bool depthBuffer, bool vsync, object target )
        {
            SdlWindow window = new SdlWindow();
            window.Create( name, width, height, colorDepth, fullScreen, left, top, depthBuffer, vsync );
            return window;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="autoCreateWindow"></param>
        /// <param name="renderSystem"></param>
        /// <param name="windowTitle"></param>
        /// <returns></returns>
        public override RenderWindow CreateWindow( bool autoCreateWindow, GLRenderSystem renderSystem, string windowTitle )
        {
            RenderWindow autoWindow = null;

            if ( autoCreateWindow )
            {
                // MONO: Could not cast result of Select to strongly typed data row
                //DataRow[] modes =
                //    (DataRow[])engineConfig.DisplayMode.Select( "Selected = true" );

                //DataRow mode = modes[0];

                //int width = (int)mode["Width"];
                //int height = (int)mode["Height"];
                //int bpp = (int)mode["Bpp"];
                //bool fullscreen = (bool)mode["FullScreen"];

                int width = 640;
                int height = 480;
                int bpp = 32;
                bool fullScreen = false;

                ConfigOption optVM = ConfigOptions[ "Video Mode" ];
                string vm = optVM.Value;
                width = int.Parse( vm.Substring( 0, vm.IndexOf( "x" ) ) );
                height = int.Parse( vm.Substring( vm.IndexOf( "x" ) + 1, vm.IndexOf( "@" ) - ( vm.IndexOf( "x" ) + 1 ) ) );
                bpp = int.Parse( vm.Substring( vm.IndexOf( "@" ) + 1, vm.IndexOf( "-" ) - ( vm.IndexOf( "@" ) + 1 ) ) );

                fullScreen = ( ConfigOptions[ "Full Screen" ].Value == "Yes" );

                // create the window with the default form as the target
                autoWindow = renderSystem.CreateRenderWindow( windowTitle, width, height, 32, fullScreen, 0, 0, true, false, null );
            }

            return autoWindow;
        }

        #endregion BaseGLSupport Members

        public override void SetConfigOption(string name, string value)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Make sure all the extra options are valid
        /// </summary>
        /// <returns>string with error message</returns>
        public override string ValidateConfig()
        {
            throw new NotImplementedException();
        }
    }
}
