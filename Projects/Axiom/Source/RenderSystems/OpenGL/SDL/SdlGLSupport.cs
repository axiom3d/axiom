#region LGPL License
/*
Axiom Graphics Engine Library
Copyright (C) 2003-2006 Axiom Project Team

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

using Axiom.Configuration;
using Axiom.Core;
using Axiom.Graphics;

using Tao.OpenGl;
using Tao.Sdl;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.OpenGL
{
    /// <summary>
    ///		Summary description for SdlGLSupport.
    /// </summary>
    internal class GLSupport : BaseGLSupport
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
            option = new ConfigOption( "Full Screen", "No", false );
            option.PossibleValues.Add( "Yes" );
            option.PossibleValues.Add( "No" );
            ConfigOptions.Add( option );

            // Video Mode
            // get the available OpenGL resolutions
            Sdl.SDL_Rect[] modes = Sdl.SDL_ListModes( IntPtr.Zero, Sdl.SDL_FULLSCREEN | Sdl.SDL_OPENGL );

            option = new ConfigOption( "Video Mode", "800 x 600 @ 32-bit colour", false );
            // add the resolutions to the config
            foreach ( Sdl.SDL_Rect mode in modes )
            {
                int width = mode.w;
                int height = mode.h;

                // filter out the lower resolutions and dupe frequencies
                if ( width >= 640 && height >= 480 )
                {
                    string query = string.Format( "{0} x {1} @ {2}-bit colour", width, height, 32 );

                    if ( !option.PossibleValues.Contains( query ) )
                    {
                        // add a new row to the display settings table
                        option.PossibleValues.Add( query );
                    }
                    if ( option.PossibleValues.Count == 1 && String.IsNullOrEmpty( option.Value ) )
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
        public override RenderWindow NewWindow( string name, int width, int height, int colorDepth, bool fullScreen, int left, int top, bool depthBuffer, bool vsync, IntPtr target )
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
                autoWindow = renderSystem.CreateRenderWindow( windowTitle, width, height, 32, fullScreen, 0, 0, true, false, IntPtr.Zero );
            }

            return autoWindow;
        }

        #endregion BaseGLSupport Members
    }
}
