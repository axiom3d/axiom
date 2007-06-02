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

using Axiom.Core;
using Axiom.Graphics;

using Tao.OpenGl;
using Tao.Sdl;
using System.Collections.Generic;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.OpenGL
{
    /// <summary>
    /// Summary description for SdlWindow.
    /// </summary>
    public class SdlWindow : RenderWindow
    {
        #region Fields

        private Sdl.SDL_Surface surface;
		private IntPtr _window;
		private SdlContext _glContext;

        #endregion Fields

        public SdlWindow()
        {
        }

        #region RenderWindow Implementation

		public override object this[ string attribute ]
		{
			get
			{
				switch ( attribute.ToLower() )
				{
					case "glcontext":
						return _glContext;
					case "window":
						System.Windows.Forms.Control ctrl = System.Windows.Forms.Control.FromChildHandle( _hWindow );
						return ctrl;
					default:
						return base[ attribute ];
				}
			}
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
        /// <param name="miscParams"></param>
		public override void Create( string name, int width, int height, bool fullScreen, Axiom.Collections.NamedParameterList miscParams )
		{
			string title = name;
			int fsaa;

			
			this.Name = name;
            this.Width = width;
            this.Height = height;
            this.ColorDepth = 32;

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
								Sdl.SDL_GL_SetAttribute( Sdl.SDL_GL_MULTISAMPLEBUFFERS, 1 );
								Sdl.SDL_GL_SetAttribute( Sdl.SDL_GL_MULTISAMPLESAMPLES, fsaa );
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

            int flags = Sdl.SDL_OPENGL | Sdl.SDL_HWPALETTE | Sdl.SDL_RESIZABLE;

            // we want double buffering
            Sdl.SDL_GL_SetAttribute( Sdl.SDL_GL_DOUBLEBUFFER, 1 );

            // request good stencil size if 32-bit color
            if ( ColorDepth == 32 && isDepthBuffered )
            {
                Sdl.SDL_GL_SetAttribute( Sdl.SDL_GL_STENCIL_SIZE, 8 );
            }

            // full screen?
            if ( fullScreen )
            {
                flags |= Sdl.SDL_FULLSCREEN;
            }


            // set the video mode (and create the surface)
            // TODO: Grab return val once changed to the right type
            _window = Sdl.SDL_SetVideoMode( width, height, ColorDepth, flags );

			// lets get active!
            IsActive = true;

            // set the window text for windowed mode
            if ( !fullScreen )
            {
                Sdl.SDL_WM_SetCaption( name, null );
            }
        }

		public override bool IsClosed
		{
			get
			{
				return false;
			}
		}
        public void Destroy()
        {
            //Sdl.SDL_FreeSurface(
        }

        public override void Reposition( int left, int right )
        {

        }

        public override void Resize( int width, int height )
        {

        }

        public void SaveToFile( string fileName )
        {

        }

        /// <summary>
        ///		Update the render window.
        /// </summary>
        /// <param name="waitForVSync"></param>
        public override void SwapBuffers( bool waitForVSync )
        {
            Sdl.SDL_GL_SwapBuffers();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="stream"></param>
        public override void Save( System.IO.Stream stream )
        {
        }



        #endregion RenderWindow Implementation
    }
}
