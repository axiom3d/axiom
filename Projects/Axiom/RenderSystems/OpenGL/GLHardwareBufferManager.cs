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
	/// 	Summary description for GLHardwareBufferManager.
	/// </summary>
	public class GLHardwareBufferManagerBase : HardwareBufferManagerBase
	{
		#region Member variables

		#endregion

		#region Constructors

		public GLHardwareBufferManagerBase()
		{
		}

		#endregion

		#region Methods

		/// <summary>
		/// 
		/// </summary>
		/// <param name="type"></param>
		/// <param name="numIndices"></param>
		/// <param name="usage"></param>
		/// <returns></returns>
		public override HardwareIndexBuffer CreateIndexBuffer( IndexType type, int numIndices, BufferUsage usage )
		{
			return CreateIndexBuffer( type, numIndices, usage, false );
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="type"></param>
		/// <param name="numIndices"></param>
		/// <param name="usage"></param>
		/// <param name="useShadowBuffer"></param>
		/// <returns></returns>
		public override HardwareIndexBuffer CreateIndexBuffer( IndexType type, int numIndices, BufferUsage usage, bool useShadowBuffer )
		{
			GLHardwareIndexBuffer buffer = new GLHardwareIndexBuffer( this, type, numIndices, usage, useShadowBuffer );
			indexBuffers.Add( buffer );
			return buffer;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="vertexSize"></param>
		/// <param name="numVerts"></param>
		/// <param name="usage"></param>
		/// <returns></returns>
		public override HardwareVertexBuffer CreateVertexBuffer( VertexDeclaration vertexDeclaration, int numVerts, BufferUsage usage )
		{
			return CreateVertexBuffer( vertexDeclaration, numVerts, usage, false );
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="vertexSize"></param>
		/// <param name="numVerts"></param>
		/// <param name="usage"></param>
		/// <param name="useShadowBuffer"></param>
		/// <returns></returns>
		public override HardwareVertexBuffer CreateVertexBuffer( VertexDeclaration vertexDeclaration, int numVerts, BufferUsage usage, bool useShadowBuffer )
		{
			GLHardwareVertexBuffer buffer = new GLHardwareVertexBuffer( this, vertexDeclaration, numVerts, usage, useShadowBuffer );
			vertexBuffers.Add( buffer );
			return buffer;
		}


		#endregion
	}

	public class GLHardwareBufferManager : HardwareBufferManager
	{
		public GLHardwareBufferManager()
			: base( new GLHardwareBufferManagerBase() )
		{
		}

		protected override void dispose( bool disposeManagedResources )
		{
			if ( disposeManagedResources )
			{
				_baseInstance.Dispose();
				_baseInstance = null;
			}
			base.dispose( disposeManagedResources );
		}

	    public static int GetGLType( VertexElementType type )
	    {
            switch (type)
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
                default:
                    return 0;
            };
	    }
	}

	public class GLSoftwareBufferManager : DefaultHardwareBufferManager
	{
	}
}
