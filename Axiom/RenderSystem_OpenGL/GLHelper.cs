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
		private static StringCollection extensionList;
		private static string glVersion;
		private static string vendor;

		/// <summary>
		/// 
		/// </summary>
		public static StringCollection Extensions
		{
			get 
			{
				return extensionList; 
			}
		}

		/// <summary>
		///		Handy check to see if the current GL version is at least what is supplied.
		/// </summary>
		/// <param name="version">What you want to check for, i.e. "1.3" </param>
		/// <returns></returns>
		public static bool CheckMinVersion(string version)
		{
			return glVersion.StartsWith(version);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="extention"></param>
		/// <returns></returns>
		public static bool SupportsExtension(string extention)
		{
			// check if the extension is supported
			return extensionList.Contains(extention);
		}

		/// <summary>
		/// 
		/// </summary>
		public static void InitializeExtensions()
		{
			if(extensionList == null)
			{
				// get the OpenGL version string and vendor name
				glVersion = Gl.glGetString(Gl.GL_VERSION);
				vendor = Gl.glGetString(Gl.GL_VENDOR);
				vendor = vendor.Substring(0, vendor.IndexOf(" "));

				// create a new extension list
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
					return (int)Gl.GL_STATIC_DRAW_ARB;

				case BufferUsage.Dynamic:
				case BufferUsage.DynamicWriteOnly:
				default:
					return (int)Gl.GL_DYNAMIC_DRAW_ARB;
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

		/// <summary>
		///		Find the GL int value for the CompareFunction enum.
		/// </summary>
		/// <param name="func"></param>
		/// <returns></returns>
		public static int ConvertEnum(CompareFunction func)
		{
			switch(func)
			{
				case CompareFunction.AlwaysFail:
					return Gl.GL_NEVER;
				case CompareFunction.AlwaysPass:
					return Gl.GL_ALWAYS;
				case CompareFunction.Less:
					return Gl.GL_LESS;
				case CompareFunction.LessEqual:
					return Gl.GL_LEQUAL;
				case CompareFunction.Equal:
					return Gl.GL_EQUAL;
				case CompareFunction.NotEqual:
					return Gl.GL_NOTEQUAL;
				case CompareFunction.GreaterEqual:
					return Gl.GL_GEQUAL;
				case CompareFunction.Greater:
					return Gl.GL_GREATER;
			} // switch

			// make the compiler happy
			return 0;
		}

		/// <summary>
		///		Find the GL int value for the StencilOperation enum.
		/// </summary>
		/// <param name="op"></param>
		/// <returns></returns>
		public static int ConvertEnum(StencilOperation op)
		{
			switch(op)
			{
				case StencilOperation.Keep:
					return Gl.GL_KEEP;
				case StencilOperation.Zero:
					return Gl.GL_ZERO;
				case StencilOperation.Replace:
					return Gl.GL_REPLACE;
				case StencilOperation.Increment:
					return Gl.GL_INCR;
				case StencilOperation.Decrement:
					return Gl.GL_DECR;
				case StencilOperation.Invert:
					return Gl.GL_INVERT;
			}

			// make the compiler happy
			return 0;
		}

		public static string Vendor
		{
			get { return vendor; }
		}

	}
}
