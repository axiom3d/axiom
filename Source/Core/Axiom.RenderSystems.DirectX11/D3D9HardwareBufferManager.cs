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

using Axiom.Core;
using Axiom.Graphics;
using Axiom.Utilities;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.DirectX9
{
	/// <summary>
	/// Should we ask D3D to manage vertex/index buffers automatically?
	/// Doing so avoids lost devices, but also has a performance impact
	/// which is unacceptably bad when using very large buffers
	/// </summary>
	/// AXIOM_D3D_MANAGE_BUFFERS
	/// <summary>
	/// Implementation of HardwareBufferManager for D3D9.
	/// </summary>
	public class D3D9HardwareBufferManagerBase : HardwareBufferManagerBase
	{
		#region Constructors

		[OgreVersion( 1, 7, 2 )]
		public D3D9HardwareBufferManagerBase()
			: base()
		{
		}

		[OgreVersion( 1, 7, 2, "~D3D9HardwareBufferManagerBase" )]
		protected override void dispose( bool disposeManagedResources )
		{
			if ( !IsDisposed )
			{
				if ( disposeManagedResources )
				{
					DestroyAllDeclarations();
					DestroyAllBindings();
				}
			}

			base.dispose( disposeManagedResources );
		}

		#endregion Constructors

		#region Methods

		/// <see cref="Axiom.Graphics.HardwareBufferManagerBase.CreateVertexBuffer(VertexDeclaration, int, BufferUsage, bool)"/>
		[OgreVersion( 1, 7, 2 )]
		public override HardwareVertexBuffer CreateVertexBuffer( VertexDeclaration vertexDeclaration, int numVerts,
		                                                         BufferUsage usage, bool useShadowBuffer )
		{
			Contract.Requires( numVerts > 0 );

#if AXIOM_D3D_MANAGE_BUFFERS
	// Override shadow buffer setting; managed buffers are automatically
	// backed by system memory
	// Don't override shadow buffer if discardable, since then we use
	// unmanaged buffers for speed (avoids write-through overhead)
			if ( useShadowBuffer && ( usage & BufferUsage.Discardable ) == 0 )
			{
				useShadowBuffer = false;
				// Also drop any WRITE_ONLY so we can read direct
				if ( usage == BufferUsage.DynamicWriteOnly )
				{
					usage = BufferUsage.Dynamic;
				}

				else if ( usage == BufferUsage.StaticWriteOnly )
				{
					usage = BufferUsage.Static;
				}
			}
#endif
			var vbuf = new D3D9HardwareVertexBuffer( this, vertexDeclaration, numVerts, usage, false, useShadowBuffer );
			lock ( VertexBuffersMutex )
			{
				vertexBuffers.Add( vbuf );
			}

			return vbuf;
		}

		/// <see cref="Axiom.Graphics.HardwareBufferManagerBase.CreateIndexBuffer(IndexType, int, BufferUsage, bool)"/>
		[OgreVersion( 1, 7, 2 )]
		public override HardwareIndexBuffer CreateIndexBuffer( IndexType type, int numIndices, BufferUsage usage,
		                                                       bool useShadowBuffer )
		{
			Contract.Requires( numIndices > 0 );

#if AXIOM_D3D_MANAGE_BUFFERS
	// Override shadow buffer setting; managed buffers are automatically
	// backed by system memory
			if ( useShadowBuffer )
			{
				useShadowBuffer = false;
				// Also drop any WRITE_ONLY so we can read direct
				if ( usage == BufferUsage.DynamicWriteOnly )
				{
					usage = BufferUsage.Dynamic;
				}

				else if ( usage == BufferUsage.StaticWriteOnly )
				{
					usage = BufferUsage.Static;
				}
			}
#endif
			var idxBuf = new D3D9HardwareIndexBuffer( this, type, numIndices, usage, false, useShadowBuffer );
			lock ( IndexBuffersMutex )
			{
				indexBuffers.Add( idxBuf );
			}

			return idxBuf;
		}

		//TODO
		//public override RenderToVertexBuffer CreateRenderToVertexBuffer()
		//{
		//    throw new AxiomException( "Direct3D9 does not support render to vertex buffer objects" );
		//}

		[OgreVersion( 1, 7, 2 )]
		protected override VertexDeclaration CreateVertexDeclarationImpl()
		{
			return new D3D9VertexDeclaration();
		}

		[OgreVersion( 1, 7, 2 )]
		protected override void DestroyVertexDeclarationImpl( VertexDeclaration decl )
		{
			decl.SafeDispose();
		}

		#endregion Methods
	};

	/// <summary>
	/// D3D9HardwareBufferManagerBase as a Singleton
	/// </summary>
	public class D3D9HardwareBufferManager : HardwareBufferManager
	{
		[OgreVersion( 1, 7, 2 )]
		public D3D9HardwareBufferManager()
			: base( new D3D9HardwareBufferManagerBase() )
		{
		}

		[OgreVersion( 1, 7, 2, "~D3D9HardwareBufferManager" )]
		protected override void dispose( bool disposeManagedResources )
		{
			if ( !IsDisposed )
			{
				if ( disposeManagedResources )
				{
					_baseInstance.SafeDispose();
				}
			}

			base.dispose( disposeManagedResources );
		}
	};
}