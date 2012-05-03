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

using System.Collections.Generic;
using Axiom.Core;
using Axiom.Graphics;

#endregion Namespace Declarations

namespace Axiom.Components.Terrain
{
	/// <summary>
	/// Interface used to by the Terrain instance to allocate GPU buffers.
	/// </summary>
	/// <remarks>
	/// This class exists to make it easier to re-use buffers between
	/// multiple instances of terrain.
	/// </remarks>
	public abstract class GpuBufferAllocator : DisposableObject
	{
		/// <summary>
		/// Allocate (or reuse) vertex buffers for a terrain LOD. 
		/// </summary>
		/// <param name="numVertices">The total number of vertices</param>
		/// <param name="destPos">Pointer to a vertex buffer for positions, to be bound</param>
		/// <param name="destDelta">Pointer to a vertex buffer for deltas, to be bound</param>
		public abstract void AllocateVertexBuffers( Terrain forTerrain, int numVertices, out HardwareVertexBuffer destPos,
		                                            out HardwareVertexBuffer destDelta );

		/// <summary>
		/// Free (or return to the pool) vertex buffers for terrain. 
		/// </summary>
		public abstract void FreeVertexBuffers( HardwareVertexBuffer posbuf, HardwareVertexBuffer deltabuf );

		/// <summary>
		/// Get a shared index buffer for a given number of settings.
		/// </summary>
		/// <remarks>
		/// Since all index structures are the same at the same LOD level and
		/// relative position, we can share index buffers. Therefore the 
		/// buffer returned from this method does not need to be 'freed' like
		/// the vertex buffers since it is never owned.
		/// </remarks>
		/// <param name="batchSize">The batch size along one edge</param>
		/// <param name="vdatasize">The size of the referenced vertex data along one edge</param>
		/// <param name="vertexIncrement">The number of vertices to increment for each new indexed row / column</param>
		/// <param name="xoffset">The x offset from the start of vdatasize, at that resolution</param>
		/// <param name="yoffset">The y offset from the start of vdatasize, at that resolution</param>
		/// <param name="numSkirtRowsCols">Number of rows and columns of skirts</param>
		/// <param name="skirtRowColSkip">The number of rows / cols to skip in between skirts</param>
		/// <returns></returns>
		public abstract HardwareIndexBuffer GetSharedIndexBuffer( ushort batchSize, ushort vdatasize, int vertexIncrement,
		                                                          ushort xoffset, ushort yoffset, ushort numSkirtRowsCols,
		                                                          ushort skirtRowColSkip );

		/// <summary>
		/// Free any buffers we're holding
		/// </summary>
		public abstract void FreeAllBuffers();
	};

	/// <summary>
	///  Standard implementation of a buffer allocator which re-uses buffers
	/// </summary>
	public class DefaultGpuBufferAllocator : GpuBufferAllocator
	{
		protected List<HardwareVertexBuffer> FreePosBufList = new List<HardwareVertexBuffer>();
		protected List<HardwareVertexBuffer> FreeDeltaBufList = new List<HardwareVertexBuffer>();
		protected Dictionary<int, HardwareIndexBuffer> SharedIBufMap = new Dictionary<int, HardwareIndexBuffer>();

		[OgreVersion( 1, 7, 2 )]
		public override void AllocateVertexBuffers( Terrain forTerrain, int numVertices, out HardwareVertexBuffer destPos,
		                                            out HardwareVertexBuffer destDelta )
		{
			//destPos = this.GetVertexBuffer( ref FreePosBufList, forTerrain.PositionBufVertexSize, numVertices );
			//destDelta = this.GetVertexBuffer( ref FreeDeltaBufList, forTerrain.DeltaBufVertexSize, numVertices );

			destPos = GetVertexBuffer( ref FreePosBufList, forTerrain.PositionVertexDecl, numVertices );
			destDelta = GetVertexBuffer( ref FreeDeltaBufList, forTerrain.DeltaVertexDecl, numVertices );
		}

		[OgreVersion( 1, 7, 2 )]
		public override void FreeVertexBuffers( HardwareVertexBuffer posbuf, HardwareVertexBuffer deltabuf )
		{
			FreePosBufList.Add( posbuf );
			FreeDeltaBufList.Add( deltabuf );
		}

		[OgreVersion( 1, 7, 2 )]
		public override HardwareIndexBuffer GetSharedIndexBuffer( ushort batchSize, ushort vdatasize, int vertexIncrement,
		                                                          ushort xoffset, ushort yoffset, ushort numSkirtRowsCols,
		                                                          ushort skirtRowColSkip )
		{
			int hsh = HashIndexBuffer( batchSize, vdatasize, vertexIncrement, xoffset, yoffset, numSkirtRowsCols, skirtRowColSkip );

			if ( !SharedIBufMap.ContainsKey( hsh ) )
			{
				// create new
				int indexCount = Terrain.GetNumIndexesForBatchSize( batchSize );
				HardwareIndexBuffer ret = HardwareBufferManager.Instance.CreateIndexBuffer( IndexType.Size16, indexCount,
				                                                                            BufferUsage.StaticWriteOnly );
				var pI = ret.Lock( BufferLocking.Discard );
				Terrain.PopulateIndexBuffer( pI, batchSize, vdatasize, vertexIncrement, xoffset, yoffset, numSkirtRowsCols,
				                             skirtRowColSkip );
				ret.Unlock();

				SharedIBufMap.Add( hsh, ret );
				return ret;
			}
			else
			{
				return SharedIBufMap[ hsh ];
			}
		}

		[OgreVersion( 1, 7, 2 )]
		public override void FreeAllBuffers()
		{
			FreePosBufList.Clear();
			FreeDeltaBufList.Clear();
			SharedIBufMap.Clear();
		}

		/// <summary>
		/// 'Warm start' the allocator based on needing x instances of 
		/// terrain with the given configuration.
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public void WarmStart( int numInstances, ushort terrainSize, ushort maxBatchSize, ushort minBatchSize )
		{
			// TODO
		}

		[OgreVersion( 1, 7, 2, "~DefaultGpuBufferAllocator" )]
		protected override void dispose( bool disposeManagedResources )
		{
			if ( !IsDisposed )
			{
				if ( disposeManagedResources )
				{
					FreeAllBuffers();
				}
			}

			base.dispose( disposeManagedResources );
		}

		[OgreVersion( 1, 7, 2 )]
		protected int HashIndexBuffer( ushort batchSize, ushort vdatasize, int vertexIncrement, ushort xoffset, ushort yoffset,
		                               ushort numSkirtRowsCols, ushort skirtRowColSkip )
		{
			int ret = batchSize.GetHashCode();
			ret ^= vdatasize.GetHashCode();
			ret ^= vertexIncrement.GetHashCode();
			ret ^= xoffset.GetHashCode();
			ret ^= yoffset.GetHashCode();
			ret ^= numSkirtRowsCols.GetHashCode();
			ret ^= skirtRowColSkip.GetHashCode();
			return ret;
		}

		[OgreVersion( 1, 7, 2 )]
		//protected HardwareVertexBuffer GetVertexBuffer( ref List<HardwareVertexBuffer> list, int vertexSize, int numVertices )
		protected HardwareVertexBuffer GetVertexBuffer( ref List<HardwareVertexBuffer> list, VertexDeclaration decl,
		                                                int numVertices )
		{
			int sz = decl.GetVertexSize()*numVertices; // vertexSize* numVertices;
			foreach ( var i in list )
			{
				if ( i.Size == sz )
				{
					HardwareVertexBuffer ret = i;
					list.Remove( i );
					return ret;
				}
			}

			// Didn't find one?
			return HardwareBufferManager.Instance.CreateVertexBuffer( decl, numVertices, BufferUsage.StaticWriteOnly );

			//TODO It should looks like this
			//return HardwareBufferManager.Instance.CreateVertexBuffer( vertexSize, numVertices, BufferUsage.StaticWriteOnly );
		}
	};
}