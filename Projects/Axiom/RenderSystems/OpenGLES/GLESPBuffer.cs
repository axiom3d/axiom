#region LGPL License
/*
Axiom Graphics Engine Library
Copyright (C) 2003-2010 Axiom Project Team
This file is part of Axiom.RenderSystems.OpenGLES
C# version developed by bostich.

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
#endregion LGPL License

#region SVN Version Information
// <file>
//     <license see="http://axiomengine.sf.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations
using System;
using Axiom.Media;
#endregion Namespace Declarations

namespace Axiom.RenderSystems.OpenGLES
{
	/// <summary>
	/// An off-screen rendering context. These contexts are always RGBA for simplicity, speed and
	/// convience, but the component format is configurable.
	/// </summary>
	public class GLESPBuffer
	{
		/// <summary>
		/// Format of the PBuffer
		/// </summary>
		public PixelComponentType Format
		{
			get;
			protected set;
		}
		/// <summary>
		/// Get's the width of the PBuffer
		/// </summary>
		public int Width
		{
			get;
			protected set;
		}
		/// <summary>
		/// Get's the height of the PBuffer
		/// </summary>
		public int Height
		{
			get;
			protected set;
		}

		/// <summary>
		/// Default ctor.
		/// </summary>
		/// <param name="format"></param>
		/// <param name="width"></param>
		/// <param name="height"></param>
		public GLESPBuffer(PixelComponentType format, int width, int height)
		{
			Format = format;
			Width = width;
			Height = height;
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="fmt"></param>
		/// <returns></returns>
		public static PixelComponentType GetPixelComponentType(PixelFormat fmt)
		{
			throw new NotImplementedException();
			
		}
	}
}

