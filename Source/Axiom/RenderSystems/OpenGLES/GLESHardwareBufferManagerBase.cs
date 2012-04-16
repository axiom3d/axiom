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
using Axiom.Graphics;
using Axiom.Core;
using OpenTK.Graphics.ES11;
#endregion Namespace Declarations

namespace Axiom.RenderSystems.OpenGLES
{
	/// <summary>
	/// Implementation of HardwareBufferManager for OpenGL ES.
	/// </summary>
	public class GLESHardwareBufferManagerBase : HardwareBufferManagerBase
	{
		protected byte[] _scratchBufferPool;
		protected IntPtr _scratchBufferPoolPtr;
		protected readonly object _scratchLock = new object();
		public const int ScratchPoolSize = 1 * 1024 * 1024;
		public const int ScratchAlignment = 32;
		private readonly object _vertexBufferLock = new object();
		private readonly object _indexBufferLock = new object();

		/// <summary>
		/// Scratch pool management (32 bit structure)
		/// </summary>
		public struct GLESScratchBufferAlloc
		{
			public int Size;
			public int Free;
		}

		/// <summary>
		/// 
		/// </summary>
		public GLESHardwareBufferManagerBase()
		{
			_scratchBufferPool = new byte[ ScratchPoolSize ];
			unsafe
			{
				_scratchBufferPoolPtr = Memory.PinObject( _scratchBufferPool );
				GLESScratchBufferAlloc* ptrAlloc = (GLESScratchBufferAlloc*)_scratchBufferPoolPtr;
				ptrAlloc->Size = ScratchPoolSize - sizeof( GLESScratchBufferAlloc );
				ptrAlloc->Free = 1;
			}
		}

		/// <summary>
		/// Utility function to get the correct GL usage based on BU's
		/// </summary>
		/// <param name="usage"></param>
		/// <returns></returns>
		public static All GetGLUsage( BufferUsage usage )
		{
			switch ( usage )
			{
				case BufferUsage.Static:
				case BufferUsage.StaticWriteOnly:
					return All.StaticDraw;
				case BufferUsage.Dynamic:
				case BufferUsage.DynamicWriteOnly:
				case BufferUsage.DynamicWriteOnlyDiscardable:
				default:
					return All.DynamicDraw;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
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
				case VertexElementType.Color_ABGR:
				case VertexElementType.Color_ARGB:
				case VertexElementType.UByte4:
					return All.UnsignedByte;
				default:
					return 0;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="type"></param>
		/// <param name="numIndices"></param>
		/// <param name="usage"></param>
		/// <returns></returns>
		public override HardwareIndexBuffer CreateIndexBuffer( IndexType type, int numIndices, BufferUsage usage )
		{
			return CreateIndexBuffer( type, numIndices, usage, true );
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
			// always use shadowBuffer
			GLESHardwareIndexBuffer buf = new GLESHardwareIndexBuffer( this, type, numIndices, usage, true );
			lock ( _indexBufferLock )
			{
				indexBuffers.Add( buf );
			}
			return buf;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="vertexSize"></param>
		/// <param name="numVerts"></param>
		/// <param name="usage"></param>
		/// <returns></returns>
		public override HardwareVertexBuffer CreateVertexBuffer( VertexDeclaration declaration, int numVerts, BufferUsage usage )
		{
			return CreateVertexBuffer( declaration, numVerts, usage, true );
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="vertexSize"></param>
		/// <param name="numVerts"></param>
		/// <param name="usage"></param>
		/// <param name="useShadowBuffer"></param>
		/// <returns></returns>
		public override HardwareVertexBuffer CreateVertexBuffer( VertexDeclaration declaration, int numVerts, BufferUsage usage, bool useShadowBuffer )
		{
			// always use shadowBuffer
			GLESHardwareVertexBuffer buf = new GLESHardwareVertexBuffer( this, declaration, numVerts, usage, true );
			lock ( _vertexBufferLock )
			{
				vertexBuffers.Add( buf );
			}
			return buf;
		}

		/// <summary>
		/// Allocator method to allow us to use a pool of memory as a scratch
		/// area for hardware buffers. This is because glMapBuffer is incredibly
		/// inefficient, seemingly no matter what options we give it. So for the
		/// period of lock/unlock, we will instead allocate a section of a local
		/// memory pool, and use glBufferSubDataARB / glGetBufferSubDataARB
		/// instead.
		/// </summary>
		/// <param name="size"></param>
		/// <returns></returns>
		public IntPtr AllocateScratch( int size )
		{
			unsafe
			{
				LogManager.Instance.Write( "Allocate Scratch : " + size.ToString() );
				// simple forward link search based on alloc sizes
				// not that fast but the list should never get that long since not many
				// locks at once (hopefully)
				lock ( _scratchLock )
				{
					// Alignment - round up the size to 32 bits
					// control blocks are 32 bits too so this packs nicely
					if ( size % 4 != 0 )
					{
						size += 4 - ( size % 4 );
					}
					int bufferPos = 0;
					byte* dataPtr = (byte*)_scratchBufferPoolPtr;
					while ( bufferPos < ScratchPoolSize )
					{
						LogManager.Instance.Write( "Bufferpos " + bufferPos );
						GLESScratchBufferAlloc* pNext = (GLESScratchBufferAlloc*)( dataPtr + bufferPos );
						// Big enough?
						if ( pNext->Free != 0 && pNext->Size >= size )
						{
							LogManager.Instance.Write( "Was big enough!" );
							// split? And enough space for control block
							if ( pNext->Size > size + sizeof( GLESScratchBufferAlloc ) )
							{
								LogManager.Instance.Write( "Split! " + pNext->Size.ToString() );
								int offset = sizeof( GLESScratchBufferAlloc ) + size;
								GLESScratchBufferAlloc* pSplitAlloc = (GLESScratchBufferAlloc*)( dataPtr + bufferPos + offset );
								pSplitAlloc->Free = 1;
								// split size is remainder minus new control block
								pSplitAlloc->Size = pNext->Size - size - sizeof( GLESScratchBufferAlloc );
								// New size of current
								pNext->Size = size;
							}
							// allocate and return
							pNext->Free = 0;

							// return pointer just after this control block (++ will do that for us)
							return (IntPtr)( ++pNext );
						}

						bufferPos += sizeof( GLESScratchBufferAlloc ) + pNext->Size;
					}//end while
					return IntPtr.Zero;
				}//end lock
			}//end unsafe
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="ptr"></param>
		public void DeallocateScratch( IntPtr ptr )
		{
			unsafe
			{
				lock ( _scratchLock )
				{
					// Simple linear search dealloc
					int bufferPos = 0;
					GLESScratchBufferAlloc* pLast = (GLESScratchBufferAlloc*)IntPtr.Zero;
					byte* dataPtr = (byte*)_scratchBufferPoolPtr;
					GLESScratchBufferAlloc* pToDelete = (GLESScratchBufferAlloc*)ptr;
					while ( bufferPos < ScratchPoolSize )
					{
						GLESScratchBufferAlloc* pCurrent = (GLESScratchBufferAlloc*)( dataPtr + bufferPos );
						// Pointers match?
						if ( ( dataPtr + bufferPos + sizeof( GLESScratchBufferAlloc ) ) == pToDelete )
						{
							// dealloc
							pCurrent->Free = 1;
							// merge with previous
							if ( pLast != (GLESScratchBufferAlloc*)IntPtr.Zero && pLast->Free != 0 )
							{
								// adjust buffer pos
								bufferPos -= ( pLast->Size + sizeof( GLESScratchBufferAlloc ) );
								// merge free space
								pLast->Size += pCurrent->Size + sizeof( GLESScratchBufferAlloc );
								pCurrent = pLast;
							}
							// merge with next
							int offset = bufferPos + pCurrent->Size + sizeof( GLESScratchBufferAlloc );
							if ( offset < ScratchPoolSize )
							{
								GLESScratchBufferAlloc* pNext = (GLESScratchBufferAlloc*)( dataPtr + offset );
								if ( pNext->Free != 0 )
								{
									pCurrent->Size += pNext->Size + sizeof( GLESScratchBufferAlloc );
								}
							}
							//done
							return;
						}

						bufferPos += sizeof( GLESScratchBufferAlloc ) + pCurrent->Size;
						pLast = pCurrent;
					}//end while

				}//end lock
			}//end unsafe
			// Should never get here unless there's a corruption
			Utilities.Contract.Requires( false, "Memory deallocation error" );
		}
	}
}

