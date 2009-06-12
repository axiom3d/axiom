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
//     <id value="$Id:"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;

using Axiom.Core;
using Axiom.Graphics;
using Tao.Sdl;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.OpenGL
{
	/// <summary>
	/// Abstractio layer for control of OpenGL through the SDL API
	/// </summary>
	class SdlDevice
	{
		#region Fields and Properties

		static public bool DoubleBuffer
		{
			get
			{
				int value;
				Sdl.SDL_GL_GetAttribute( Sdl.SDL_GL_DOUBLEBUFFER, out value );
				return (value == 1);
			}
			set
			{
				Sdl.SDL_GL_SetAttribute( Sdl.SDL_GL_DOUBLEBUFFER, value ? 1 : 0 );
			}
		}


		static public int StencilSize
		{
			get
			{
				int value;
				Sdl.SDL_GL_GetAttribute( Sdl.SDL_GL_STENCIL_SIZE, out value );
				return value;
			}
			set
			{
				Sdl.SDL_GL_SetAttribute( Sdl.SDL_GL_STENCIL_SIZE, value );
			}
		}

		static public int MultiSampleBuffers
		{
			get
			{
				int value;
				Sdl.SDL_GL_GetAttribute( Sdl.SDL_GL_MULTISAMPLEBUFFERS, out value );
				return value;
			}
			set
			{
				Sdl.SDL_GL_SetAttribute( Sdl.SDL_GL_MULTISAMPLEBUFFERS, value );
			}
		}
		static public int MultiSampleSamples
		{
			get
			{
				int value;
				Sdl.SDL_GL_GetAttribute( Sdl.SDL_GL_MULTISAMPLESAMPLES, out value );
				return value;
			}
			set
			{
				Sdl.SDL_GL_SetAttribute( Sdl.SDL_GL_MULTISAMPLESAMPLES, value );
			}
		}
										
		#endregion Fields and PRoperties

		#region Construction and Destruction

		public SdlDevice()
		{

		}

		#endregion Construction and Destruction

		#region Methods

		static public void SwapBuffers()
		{
			Sdl.SDL_GL_SwapBuffers();
		}

		#endregion Methods

	}
}