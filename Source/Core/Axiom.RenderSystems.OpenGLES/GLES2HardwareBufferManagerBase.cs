#region MIT/X11 License

//Copyright © 2003-2012 Axiom 3D Rendering Engine Project
//
//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:
//
//The above copyright notice and this permission notice shall be included in
//all copies or substantial portions of the Software.
//
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//THE SOFTWARE.

#endregion License

#region SVN Version Information

// <file>
//     <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Axiom.Core;
using Axiom.Graphics;

using GL = OpenTK.Graphics.ES11.GL;
using GLenum = OpenTK.Graphics.ES11.All;
using All = OpenTK.Graphics.ES11.All;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.OpenGLES
{
	internal class GLESHardwareBufferManagerBase : HardwareBufferManagerBase
	{
		private const int DefaultMapBufferThreshold = ( 1024 * 32 );


		private byte[] _scratchBufferPool;
		private object _scratchMutex;

		public GLESHardwareBufferManagerBase() {}

		public override HardwareVertexBuffer CreateVertexBuffer( VertexDeclaration vertexDeclaration, int numVerts, BufferUsage usage, bool useShadowBuffer )
		{
			var vertexBuffer = new GLESHardwareVertexBuffer( this, vertexDeclaration, numVerts, usage, true );
			lock ( VertexBuffersMutex )
			{
				vertexBuffers.Add( vertexBuffer );
			}
			return vertexBuffer;
		}

		public override HardwareIndexBuffer CreateIndexBuffer( IndexType type, int numIndices, BufferUsage usage, bool useShadowBuffer )
		{
			var indexBuffer = new GLESHardwareIndexBuffer( this, type, numIndices, usage, true );
			lock ( IndexBuffersMutex )
			{
				indexBuffers.Add( indexBuffer );
			}
			return indexBuffer;
		}

		//public override RenderToVertexBuffer CreateRenderToVertexBuffer( )
		//{
		//    throw new NotImplementedException();
		//}

		public static All GetGLUsage( BufferUsage usage )
		{
			switch ( usage )
			{
				case BufferUsage.Static:
				case BufferUsage.StaticWriteOnly:
					return All.StaticDraw;
				case BufferUsage.Dynamic:
				case BufferUsage.DynamicWriteOnly:
					return All.DynamicDraw;
				case BufferUsage.DynamicWriteOnlyDiscardable:
				default:
					return All.DynamicDraw;
			}
		}

		public static All GetGLType( VertexElementType type )
		{
			switch ( type )
			{
				case VertexElementType.Float1:
				case VertexElementType.Float2:
				case VertexElementType.Float3:
				case VertexElementType.Float4:
					return All.Float;
				case VertexElementType.Short1:
				case VertexElementType.Short2:
				case VertexElementType.Short3:
				case VertexElementType.Short4:
					return All.Short;
				case VertexElementType.Color:
				case VertexElementType.Color_ARGB:
				case VertexElementType.Color_ABGR:
				case VertexElementType.UByte4:
					return All.UnsignedByte;
				default:
					return 0;
			}
		}

		public BufferBase AllocateScratch( int size )
		{
			return null;
		}

		public void DeallocateScratch( BufferBase ptr ) {}

		public int MapBufferThreshold { get; set; }
	}
}
