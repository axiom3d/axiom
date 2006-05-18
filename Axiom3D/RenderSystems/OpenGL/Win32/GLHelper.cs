using System;
using System.Windows.Forms;
using Axiom;
using Tao.Platform.Windows;

namespace Axiom.RenderSystems.OpenGL
{
    /// <summary>
    /// Summary description for GLSupport.
    /// </summary>
    public class GLSupport : BaseGLSupport
    {

        public GLSupport()
            : base()
        {
        }

        /// <summary>
        ///		Uses Wgl to return the procedure address for an extension function.
        /// </summary>
        /// <param name="extension"></param>
        /// <returns></returns>
        public override IntPtr GetProcAddress( string extension )
        {
            return Wgl.wglGetProcAddress( extension );
        }

        /// <summary>
        ///		Query the display modes and deal with any other config options.
        /// </summary>
        public override void AddConfig()
        {
            Gdi.DEVMODE setting;
            int i = 0;
            int width, height, bpp, freq;
            ConfigOption option;

            // Full Screen
            option = new ConfigOption( "Full Screen", "Yes", false );
            option.PossibleValues.Add( "Yes" );
            option.PossibleValues.Add( "No" );
            ConfigOptions.Add( option );

            // Video Mode
            // get the available OpenGL resolutions
            bool more = User.EnumDisplaySettings( null, i++, out setting );

            option = new ConfigOption( "Video Mode", "", false );
            // add the resolutions to the config
            while ( more )
            {
                width = setting.dmPelsWidth;
                height = setting.dmPelsHeight;
                bpp = setting.dmBitsPerPel;
                freq = setting.dmDisplayFrequency;

                // filter out the lower resolutions and dupe frequencies
                if ( width >= 640 && height >= 480 && bpp >= 16 )
                {
                    string query = string.Format( "{0} x {1} @ {2}-bit colour", width, height, bpp );

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
                // grab the current display settings
                more = User.EnumDisplaySettings( null, i++, out setting );
            }
            ConfigOptions.Add( option );

            option = new ConfigOption( "FSAA", "0", false );
            option.PossibleValues.Add( "0" );
            option.PossibleValues.Add( "2" );
            option.PossibleValues.Add( "4" );
            option.PossibleValues.Add( "6" );
            ConfigOptions.Add( option );

        }

        public override Axiom.RenderWindow CreateWindow( bool autoCreateWindow, GLRenderSystem renderSystem, string windowTitle )
        {
            RenderWindow autoWindow = null;

            if ( autoCreateWindow )
            {
                int width = 640;
                int height = 480;
                int bpp = 32;
                bool fullscreen = false;

                ConfigOption optVM = ConfigOptions[ "Video Mode" ];
                string vm = optVM.Value;
                width = int.Parse( vm.Substring( 0, vm.IndexOf( "x" ) ) );
                height = int.Parse( vm.Substring( vm.IndexOf( "x" ) + 1, vm.IndexOf( "@" ) - ( vm.IndexOf( "x" ) + 1 ) ) );
                bpp = int.Parse( vm.Substring( vm.IndexOf( "@" ) + 1, vm.IndexOf( "-" ) - ( vm.IndexOf( "@" ) + 1 ) ) );

                fullscreen = ( ConfigOptions[ "Full Screen" ].Value == "Yes" );


                // create a default form to use for a rendering target
                DefaultForm form = CreateDefaultForm( windowTitle, 0, 0, width, height, fullscreen );

                // create the window with the default form as the target
                autoWindow = renderSystem.CreateRenderWindow( windowTitle, width, height, bpp, fullscreen, 0, 0, true, false, form.Target );

                // set the default form's renderwindow so it can access it internally
                form.RenderWindow = autoWindow;

                // show the window
                form.Show();
            }

            return autoWindow;
        }

        public override Axiom.RenderWindow NewWindow( string name, int width, int height, int colorDepth, bool fullScreen, int left, int top, bool depthBuffer, bool vsync, object target )
        {
            Win32Window window = new Win32Window();

            window.Handle = target;

            window.Create( name, width, height, colorDepth, fullScreen, left, top, depthBuffer, vsync );

            return window;
        }

        /// <summary>
        ///		Creates a default form to use for a rendering target.
        /// </summary>
        /// <remarks>
        ///		This is used internally whenever <see cref="Initialize"/> is called and autoCreateWindow is set to true.
        /// </remarks>
        /// <param name="windowTitle">Title of the window.</param>
        /// <param name="top">Top position of the window.</param>
        /// <param name="left">Left position of the window.</param>
        /// <param name="width">Width of the window.</param>
        /// <param name="height">Height of the window</param>
        /// <param name="fullScreen">Prepare the form for fullscreen mode?</param>
        /// <returns>A form suitable for using as a rendering target.</returns>
        private DefaultForm CreateDefaultForm( string windowTitle, int top, int left, int width, int height, bool fullScreen )
        {
            DefaultForm form = new DefaultForm();

            form.ClientSize = new System.Drawing.Size( width, height );
            form.MaximizeBox = false;
            form.MinimizeBox = false;
            form.StartPosition = FormStartPosition.CenterScreen;

            if ( fullScreen )
            {
                form.Top = 0;
                form.Left = 0;
                form.FormBorderStyle = FormBorderStyle.None;
                form.WindowState = FormWindowState.Maximized;
                form.TopMost = true;
                form.TopLevel = true;
            }
            else
            {
                form.Top = top;
                form.Left = left;
                form.FormBorderStyle = FormBorderStyle.FixedSingle;
                form.WindowState = FormWindowState.Normal;
                form.Text = windowTitle;
            }

            return form;
        }

        public override void SetConfigOption( string name, string value )
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
