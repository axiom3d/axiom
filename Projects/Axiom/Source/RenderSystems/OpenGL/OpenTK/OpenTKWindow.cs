#region LGPL License

/*
Axiom Graphics Engine Library
Copyright (C) 2003-2009 Axiom Project Team

The overall design, and a majority of the core engine and rendering code 
contained within this library is a derivative of the open source Object Oriented 
Graphics Engine OGRE, which can be found at http://ogre.sourceforge.net.  
Many thanks to the OGRE team for maintaining such a high quality project.

This library is free software; you can redistribute it and/or
modify it under the terms of the GNU Lesser General Public
License as published by the Free Software Foundation; either
version 2.1 of the License, or (at your option) any later version.

This library is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
Lesser General Public License for more details.

You should have received a copy of the GNU Lesser General Public
License along with this library; if not, write to the Free Software
Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
*/

#endregion

#region SVN Version Information

// <file>
//     <license see="http://axiomengine.sf.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;

using Axiom.Graphics;

using OpenTK;
using OpenTK.Graphics;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.OpenGL
{
    /// <summary>
    /// Summary description for OpenTKWindow.
    /// </summary>
    public class OpenTKWindow : RenderWindow
    {
        #region Fields

        public GameWindow OTKGameWindow;

        private bool destroyed;
        private bool fullScreen;
        private DisplayDevice displayDevice = null;

        #endregion Fields

        public OpenTKWindow()
        {
        }

        #region RenderWindow Members

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
        /// <param name="miscParams"></param>
        public override void Create( string name,
                                     int width,
                                     int height,
                                     int colorDepth,
                                     bool fullScreen,
                                     int left,
                                     int top,
                                     bool depthBuffer,
                                     params object[] miscParams )
        {
            this.name = name;
            this.width = width;
            this.height = height;
            this.colorDepth = colorDepth;
            this.fullScreen = fullScreen;
            displayDevice = DisplayDevice.Default;

            // create window
            OTKGameWindow = new GameWindow( width, height, GraphicsMode.Default, name );

            // full screen?
            if ( fullScreen )
            {
                displayDevice.ChangeResolution( displayDevice.SelectResolution( width, height, colorDepth, 60f ) );
                OTKGameWindow.WindowState = WindowState.Fullscreen;
            }
            else
            {
                OTKGameWindow.WindowState = WindowState.Normal;
            }

            GL.Clear( ClearBufferMask.ColorBufferBit );
            OTKGameWindow.ProcessEvents();
            OTKGameWindow.SwapBuffers();

            // lets get active!
            isActive = true;
        }

        public override void Dispose()
        {
            Destroy();
            base.Dispose();
        }

        public void Destroy()
        {
            if ( !destroyed )
            {
                if ( fullScreen )
                {
                    displayDevice.RestoreResolution();
                }
                OTKGameWindow.Context.Dispose();
                OTKGameWindow.Exit();
                OTKGameWindow = null;
                destroyed = true;
            }
        }

        public override void Reposition( int left, int right )
        {
        }

        public override void Resize( int width, int height )
        {
            OTKGameWindow.Width = width;
            OTKGameWindow.Height = height;
        }

        public void SaveToFile( string fileName )
        {
        }

        public override void Update()
        {
            base.Update();
            if ( OTKGameWindow != null )
            {
                OTKGameWindow.ProcessEvents();
            }
        }

        private bool _isSet = false;

        /// <summary>
        ///		Update the render window.
        /// </summary>
        /// <param name="waitForVSync"></param>
        public override void SwapBuffers( bool waitForVSync )
        {
            if ( !_isSet )
            {
                OTKGameWindow.VSync = waitForVSync ? VSyncMode.On : VSyncMode.Off;
                _isSet = true;
            }

            if ( OTKGameWindow != null )
            {
                OTKGameWindow.SwapBuffers();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="stream"></param>
        public override void Save( System.IO.Stream stream )
        {
        }

        #endregion RenderWindow Members
    }
}