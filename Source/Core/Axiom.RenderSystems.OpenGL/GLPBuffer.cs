#region LGPL License

/*
Axiom Graphics Engine Library
Copyright © 2003-2011 Axiom Project Team

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
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <id value="$Id: GLPBuffer.cs 1085 2007-08-13 20:37:24Z borrillis $"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections.Generic;
using System.Text;

using Axiom.Media;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.OpenGL
{
	/// <summary>
	/// An off-screen rendering context. These contexts are always RGBA for simplicity, speed and
	/// convenience, but the component format is configurable.
	/// </summary>
	internal abstract class GLPBuffer
	{
		#region Fields and Properties

		#region Format Property

		private PixelComponentType _format;

		public PixelComponentType Format
		{
			get
			{
				return _format;
			}
			protected set
			{
				_format = value;
			}
		}

		#endregion Format Property

		#region Width Property

		private int _width;

		public int Width
		{
			get
			{
				return _width;
			}
			protected set
			{
				_width = value;
			}
		}

		#endregion Width Property

		#region Height Property

		private int _height;

		public int Height
		{
			get
			{
				return _height;
			}
			protected set
			{
				_height = value;
			}
		}

		#endregion Height Property

		#region GLContext Property

		/// <summary>
		/// Get the GL context that needs to be active to render to this PBuffer.
		/// </summary>
		/// <returns></returns>
		public abstract GLContext Context { get; }

		#endregion GLContext Property

		#endregion Fields and Properties

		#region Construction and Destruction

		/// <summary>
		/// 
		/// </summary>
		/// <param name="format"></param>
		/// <param name="width"></param>
		/// <param name="height"></param>
		public GLPBuffer( PixelComponentType format, int width, int height ) {}

		#endregion Construction and Destruction

		#region Methods

		/// <summary>
		/// Get PBuffer component format for an OGRE pixel format.
		/// </summary>
		/// <param name="fmt"></param>
		/// <returns></returns>
		public static PixelComponentType GetPixelComponentType( PixelFormat fmt )
		{
			return PixelUtil.GetComponentType( fmt );
		}

		#endregion Methods
	}
}
