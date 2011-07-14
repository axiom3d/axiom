#region LGPL License
/*
Axiom Graphics Engine Library
Copyright � 2003-2011 Axiom Project Team

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
//     <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;

using Axiom.Graphics;

using Tao.OpenGl;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.OpenGL
{
	/// <summary>
	/// Summary description for GLHelper.
	/// </summary>
	public sealed class GLHelper
	{
		/// <summary>
		///
		/// </summary>
		/// <param name="usage"></param>
		/// <returns></returns>
		public static int ConvertEnum( BufferUsage usage )
		{
			switch ( usage )
			{
				case BufferUsage.Static:
				case BufferUsage.StaticWriteOnly:
					return (int)Gl.GL_STATIC_DRAW_ARB;

				case BufferUsage.Dynamic:
				case BufferUsage.DynamicWriteOnly:
					return (int)Gl.GL_DYNAMIC_DRAW_ARB;

				case BufferUsage.DynamicWriteOnlyDiscardable:
					return (int)Gl.GL_STREAM_DRAW_ARB;

				default:
					return (int)Gl.GL_DYNAMIC_DRAW_ARB;
			}
		}

		public static int ConvertEnum( SceneBlendFactor blend )
		{
			switch ( blend )
			{
				case SceneBlendFactor.One:
					return Gl.GL_ONE;
				case SceneBlendFactor.Zero:
					return Gl.GL_ZERO;
				case SceneBlendFactor.DestColor:
					return Gl.GL_DST_COLOR;
				case SceneBlendFactor.SourceColor:
					return Gl.GL_SRC_COLOR;
				case SceneBlendFactor.OneMinusDestColor:
					return Gl.GL_ONE_MINUS_DST_COLOR;
				case SceneBlendFactor.OneMinusSourceColor:
					return Gl.GL_ONE_MINUS_SRC_COLOR;
				case SceneBlendFactor.DestAlpha:
					return Gl.GL_DST_ALPHA;
				case SceneBlendFactor.SourceAlpha:
					return Gl.GL_SRC_ALPHA;
				case SceneBlendFactor.OneMinusDestAlpha:
					return Gl.GL_ONE_MINUS_DST_ALPHA;
				case SceneBlendFactor.OneMinusSourceAlpha:
					return Gl.GL_ONE_MINUS_SRC_ALPHA;
			}
			;
			// to keep compiler happy
			return Gl.GL_ONE;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public static int ConvertEnum( VertexElementType type )
		{
			switch ( type )
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
				case VertexElementType.Color_ABGR:
				case VertexElementType.Color_ARGB:
				case VertexElementType.UByte4:
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
		public static int ConvertEnum( CompareFunction func )
		{
			switch ( func )
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

		public static int ConvertEnum( StencilOperation op )
		{
			return ConvertEnum( op, false );
		}

		/// <summary>
		///		Find the GL int value for the StencilOperation enum.
		/// </summary>
		/// <param name="op"></param>
		/// <returns></returns>
		public static int ConvertEnum( StencilOperation op, bool invert )
		{
			switch ( op )
			{
				case StencilOperation.Keep:
					return Gl.GL_KEEP;

				case StencilOperation.Zero:
					return Gl.GL_ZERO;

				case StencilOperation.Replace:
					return Gl.GL_REPLACE;

				case StencilOperation.Increment:
					return invert ? Gl.GL_DECR : Gl.GL_INCR;

				case StencilOperation.Decrement:
					return invert ? Gl.GL_INCR : Gl.GL_DECR;

				case StencilOperation.IncrementWrap:
					return invert ? Gl.GL_DECR_WRAP_EXT : Gl.GL_INCR_WRAP_EXT;

				case StencilOperation.DecrementWrap:
					return invert ? Gl.GL_INCR_WRAP_EXT : Gl.GL_DECR_WRAP_EXT;

				case StencilOperation.Invert:
					return Gl.GL_INVERT;
			}

			// make the compiler happy
			return Gl.GL_KEEP;
		}

		public static int ConvertEnum( GpuProgramType type )
		{
			switch ( type )
			{
				case GpuProgramType.Vertex:
					return Gl.GL_VERTEX_PROGRAM_ARB;

				case GpuProgramType.Fragment:
					return Gl.GL_FRAGMENT_PROGRAM_ARB;
			}

			// make the compiler happy
			return 0;
		}

	    public static int ConvertEnum( TextureAddressing tam )
        {
            var type = 0;

            switch ( tam )
            {
                case TextureAddressing.Wrap:
                    type = Gl.GL_REPEAT;
                    break;

                case TextureAddressing.Mirror:
                    type = Gl.GL_MIRRORED_REPEAT;
                    break;

                case TextureAddressing.Clamp:
                    type = Gl.GL_CLAMP_TO_EDGE;
                    break;

                case TextureAddressing.Border:
                    type = Gl.GL_CLAMP_TO_BORDER;
                    break;
            }

            return type;
	    }
	}
}