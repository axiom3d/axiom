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
using System.Data;

using Axiom.Configuration;
using Axiom.Core;
using Axiom.Graphics;

using Tao.OpenGl;
using Tao.Sdl;
using Axiom.Collections;

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

		public override void Start()
		{
			LogManager.Instance.Write( "*** Starting SDLGL Subsystem ***" );
        }

		public override void Stop()
		{
			LogManager.Instance.Write( "*** Stopping SDLGL Subsystem ***" );
		}

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
            option.PossibleValues.Add(0, "Yes" );
            option.PossibleValues.Add(1, "No" );
            ConfigOptions.Add( option );

            // Video Mode
            // get the available OpenGL resolutions
            Sdl.SDL_Rect[] modes = Sdl.SDL_ListModes( IntPtr.Zero, Sdl.SDL_FULLSCREEN | Sdl.SDL_OPENGL );

            option = new ConfigOption( "Video Mode", "800 x 600", false );
            // add the resolutions to the config
            foreach ( Sdl.SDL_Rect mode in modes )
            {
                int width = mode.w;
                int height = mode.h;

                // filter out the lower resolutions and dupe frequencies
                if ( width >= 640 && height >= 480 )
                {
                    string query = string.Format( "{0} x {1}", width, height);

                    if ( !option.PossibleValues.Values.Contains( query ) )
                    {
                        // add a new row to the display settings table
                        option.PossibleValues.Add( option.PossibleValues.Count, query );
                    }
                    if ( option.PossibleValues.Count == 1 )
                    {
                        option.Value = query;
                    }
                }
            }
            ConfigOptions.Add( option );

            option = new ConfigOption( "FSAA", "0", false );
            option.PossibleValues.Add(0, "0" );
            option.PossibleValues.Add(1, "2" );
            option.PossibleValues.Add(2, "4" );
            option.PossibleValues.Add(3, "6" );
            ConfigOptions.Add( option );
        }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="name"></param>
		/// <param name="width"></param>
		/// <param name="height"></param>
		/// <param name="fullScreen"></param>
		/// <param name="miscParams"></param>
		/// <returns></returns>
		public override RenderWindow NewWindow( string name, int width, int height, bool fullScreen, Axiom.Collections.NamedParameterList miscParams )
		{
            SdlRenderWindow window = new SdlRenderWindow();
            window.Create( name, width, height, fullScreen, miscParams);
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
				if ( opt != null )
					miscParams.Add( "colorDepth", opt.Value );

				opt = ConfigOptions[ "VSync" ];
				if ( opt != null )
				{
					miscParams.Add( "vsync", opt.Value );
					//TODO : renderSystem.WaitForVerticalBlank = (bool)opt.Value;
				}

				opt = ConfigOptions[ "FSAA" ];
				if ( opt != null )
					miscParams.Add( "fsaa", opt.Value );

                // create the window with the default form as the target
                autoWindow = renderSystem.CreateRenderWindow( windowTitle, width, height, fullScreen, miscParams );
            }

            return autoWindow;
        }

        #endregion BaseGLSupport Members
    }
}
