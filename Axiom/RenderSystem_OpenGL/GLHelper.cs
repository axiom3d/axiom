#region LGPL License
/*
Axiom Game Engine Library
Copyright (C) 2003  Axiom Project Team

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

using System;
using System.Collections.Specialized;
using Axiom.SubSystems.Rendering;
//using Gl = CsGL.OpenGL.GL;
using Gl = Tao.OpenGl.Gl;

namespace RenderSystem_OpenGL
{
	/// <summary>
	/// Summary description for GLHelper.
	/// </summary>
	public class GLHelper
	{
		const uint GL_STATIC_DRAW_ARB = 0x88E4; 
		const uint GL_ARRAY_BUFFER_ARB = 0x8892;
		const uint GL_READ_ONLY_ARB = 0x88B8;
		const uint GL_ELEMENT_ARRAY_BUFFER_ARB = 0x8893;
		const uint GL_STATIC_COPY_ARB = 0x88E6; 
		const uint GL_DYNAMIC_DRAW_ARB = 0x88E8;
		const uint GL_WRITE_ONLY_ARB = 0x88B9;
		const uint GL_READ_WRITE_ARB = 0x88BA;

		static StringCollection extensionList;

		/// <summary>
		/// 
		/// </summary>
		public static StringCollection Extensions
		{
			get 
			{
				// lazy load, first time load the extensions
				LoadExtensionList();

				return extensionList; 
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="extention"></param>
		/// <returns></returns>
		public static bool SupportsExtension(string extention)
		{
			// lazy load, first time load the extensions
			LoadExtensionList();

			// check if the extension is supported
			return extensionList.Contains(extention);
		}

		/// <summary>
		/// 
		/// </summary>
		private static void LoadExtensionList()
		{
			if(extensionList == null)
			{
				extensionList = new StringCollection();

				string allExt = Gl.glGetString(Gl.GL_EXTENSIONS);
				string[] splitExt = allExt.Split(Char.Parse(" "));

				// store the parsed extension list
				for(int i = 0; i < splitExt.Length; i++)
					extensionList.Add(splitExt[i]);
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="usage"></param>
		/// <returns></returns>
		public static int ConvertEnum(BufferUsage usage)
		{
			switch(usage)
			{
				case BufferUsage.Static:
				//case BufferUsage.StaticWriteOnly:
					return (int)GL_STATIC_DRAW_ARB;

				case BufferUsage.Dynamic:
				case BufferUsage.DynamicWriteOnly:
				default:
					return (int)GL_DYNAMIC_DRAW_ARB;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public static int ConvertEnum(VertexElementType type)
		{
			switch(type)
			{
				case VertexElementType.Float1:
				case VertexElementType.Float2:
				case VertexElementType.Float3:
				case VertexElementType.Float4:
					return Gl.GL_FLOAT;

				case VertexElementType.Short1:
				case VertexElementType.Short2:
				case VertexElementType.Short3:
				case VertexElementType.Short4:
					return Gl.GL_SHORT;

				case VertexElementType.Color:
					return Gl.GL_UNSIGNED_BYTE;
			}

			// should never reach this
			return 0;
		}

	}
}
