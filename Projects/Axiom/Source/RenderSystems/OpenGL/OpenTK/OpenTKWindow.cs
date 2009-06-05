#region Namespace Declarations

using System;

using Axiom.Graphics;

using OpenTK;
using OpenTK.Graphics;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.OpenGL
{
    using System.Collections.Generic;

    using Collections;

    using Core;

    using Media;

    /// <summary>
    /// Summary description for OpenTKWindow.
    /// </summary>
    public class OpenTKWindow : RenderWindow
    {
        public class AxiomOTKGameWindow : GameWindow
        {
            public AxiomOTKGameWindow(int width, int height, GraphicsMode gm, string title) : base(width, height, gm, title) { }
            public override void OnRenderFrame(RenderFrameEventArgs e)
            {
                if ( this.IsExiting == false )
                    Exit();
            }
        }

        #region Fields

        public AxiomOTKGameWindow OTKGameWindow;

        private bool destroyed;
        private bool fullScreen;
        private DisplayDevice displayDevice = null;
        private bool lastVSyncModeSet = false;

        #endregion Fields

        public OpenTKWindow()
        {
        }

        #region RenderWindow Members

        public override object this[string attribute]
        {
            get
            {
                switch (attribute.ToLower())
                {
                    case "glcontext":
                        return null; //	_glContext;
                    case "window":
                        return OTKGameWindow;
                    // Retrieve the Handle to the SDL Window
                    //System.Windows.Forms.Control ctrl = System.Windows.Forms.Control.FromHandle( sdlWindowHandle );
                    //return ctrl;
                    default:
                        return null;
                }
            }
        }

        public void Destroy()
        {
            if ( !destroyed )
            {
                if ( fullScreen )
                    displayDevice.RestoreResolution();
                OTKGameWindow.Context.Dispose();
                OTKGameWindow.Exit();
                OTKGameWindow = null;
                destroyed = true;
            }
        }

        public override void Reposition(int left, int right)
        {
        }

        /// <summary>
        /// Indicates whether the window has been closed by the user.
        /// </summary>
        /// <returns></returns>
        public override bool IsClosed
        {
            get { return false; }
        }

        /// <summary>
        ///		Creates & displays the new window.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="width">The width of the window in pixels.</param>
        /// <param name="height">The height of the window in pixels.</param>
        /// <param name="fullScreen">If true, the window fills the screen, with no title bar or border.</param>
        /// <param name="miscParams">A variable number of platform-specific arguments. 
        /// The actual requirements must be defined by the implementing subclasses.</param>
        public override void Create(string name, int width, int height, bool fullScreen, NamedParameterList miscParams)
        {
            string title = name;
            int fsaa;

            this.Name = name;
            this.Width = width;
            this.Height = height;
            this.ColorDepth = 32;
            this.fullScreen = fullScreen;
            displayDevice = DisplayDevice.Default;

            #region Parameter Handling

            if ( miscParams != null )
            {
                foreach ( KeyValuePair<string, object> entry in miscParams )
                {
                    switch ( entry.Key )
                    {
                        case "title":
                            title = entry.Value.ToString();
                            break;
                        case "left":
                            left = Int32.Parse( entry.Value.ToString() );
                            break;
                        case "top":
                            top = Int32.Parse( entry.Value.ToString() );
                            break;
                        case "fsaa":
                            fsaa = Int32.Parse( entry.Value.ToString() );
                            if ( fsaa > 1 )
                            {
                                // If FSAA is enabled in the parameters, enable the MULTISAMPLEBUFFERS
                                // and set the number of samples before the render window is created.
                                //displayDevice.MultiSampleBuffers = 1;
                                //SdlDevice.MultiSampleSamples = fsaa;
                            }
                            break;
                        case "colourDepth":
                        case "colorDepth":
                            ColorDepth = Int32.Parse( entry.Value.ToString() );
                            break;
                        default:
                            break;
                    }
                }
            }

            #endregion Parameter Handling

            // create window
            OTKGameWindow = new AxiomOTKGameWindow( width, height, GraphicsMode.Default, name );

            // full screen?
            if ( fullScreen )
            {
                displayDevice.ChangeResolution( displayDevice.SelectResolution( width, height, ColorDepth, 60f ) );
                OTKGameWindow.WindowState = WindowState.Fullscreen;
            }
            else
            {
                OTKGameWindow.WindowState = WindowState.Normal;
                OTKGameWindow.WindowBorder = WindowBorder.Fixed;
            }

            OTKGameWindow.Title = title;

            WindowEventMonitor.Instance.RegisterWindow( this );

            // lets get active!
            IsActive = true;

            GL.Clear( ClearBufferMask.ColorBufferBit );
            SwapBuffers( false );
        }

        public override void Resize(int width, int height)
        {
            if ( destroyed )
                return;

            OTKGameWindow.Width = width;
            OTKGameWindow.Height = height;
        }

        public void SaveToFile(string fileName)
        {

        }

        public override void Update()
        {
            if ( destroyed )
                return;

            base.Update();
            OTKGameWindow.ProcessEvents();
        }

        public override void CopyContentsToMemory( PixelBox pb, FrameBuffer buffer )
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///		Update the render window.
        /// </summary>
        /// <param name="waitForVSync"></param>
        public override void SwapBuffers(bool waitForVSync)
        {
            if ( destroyed || OTKGameWindow.WindowState == WindowState.Minimized )
                return;

            if ( lastVSyncModeSet != waitForVSync )
            {
                OTKGameWindow.VSync = waitForVSync ? VSyncMode.On : VSyncMode.Off;
                lastVSyncModeSet = waitForVSync;
            }

            OTKGameWindow.SwapBuffers();
        }

        #endregion RenderWindow Members
    }
}
