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
//     <id value="$Id$"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Axiom.Graphics;
using Axiom.Math;

#endregion Namespace Declarations

namespace Axiom.Core
{
	/// <summary>
	///     Pre-transforms and batches up meshes for efficient use as static geometry in a scene.
	/// </summary>
	/// <remarks>
	/// Modern graphics cards (GPUs) prefer to receive geometry in large
	/// batches. It is orders of magnitude faster to render 10 batches
	/// of 10,000 triangles than it is to render 10,000 batches of 10
	/// triangles, even though both result in the same number of on-screen
	/// triangles.
	/// <br/>
	/// Therefore it is important when you are rendering a lot of geometry to
	/// batch things up into as few rendering calls as possible. This
	/// class allows you to build a batched object from a series of entities
	/// in order to benefit from this behaviour.
	/// Batching has implications of it's own though:
	/// <ul>
	/// <li> Batched geometry cannot be subdivided; that means that the whole
	/// 	group will be displayed, or none of it will. This obivously has
	/// 	culling issues.</li>l
	/// <li> A single world transform must apply to the entire batch. Therefore
	/// 	once you have batched things, you can't move them around relative to
	/// 	each other. That's why this class is most useful when dealing with
	/// 	static geometry (hence the name). In addition, geometry is
	/// 	effectively duplicated, so if you add 3 entities based on the same
	/// 	mesh in different positions, they will use 3 times the geometry
	/// 	space than the movable version (which re-uses the same geometry).
	/// 	So you trade memory	and flexibility of movement for pure speed when
	/// 	using this class.</li>l
	/// <li> A single material must apply for each batch. In fact this class
	/// 	allows you to use multiple materials, but you should be aware that
	/// 	internally this means that there is one batch per material.
	/// 	Therefore you won't gain as much benefit from the batching if you
	/// 	use many different materials; try to keep the number down.</li>l
	/// </ul>
	/// <br/>
	/// In order to retain some sort of culling, this class will batch up
	/// meshes in localised regions. The size and shape of these blocks is
	/// controlled by the SceneManager which constructs this object, since it
	/// makes sense to batch things up in the most appropriate way given the
	/// existing partitioning of the scene.
	/// <br/>
	/// The LOD settings of both the Mesh and the Materials used in
	/// constructing this static geometry will be respected. This means that
	/// if you use meshes/materials which have LOD, batches in the distance
	/// will have a lower polygon count or material detail to those in the
	/// foreground. Since each mesh might have different LOD distances, during
	/// build the furthest distance at each LOD level from all meshes
	/// in that region is used. This means all the LOD levels change at the
	/// same time, but at the furthest distance of any of them (so quality is
	/// not degraded). Be aware that using Mesh LOD in this class will
	/// further increase the memory required. Only generated LOD
	/// is supported for meshes.
	/// <br/>
	/// There are 2 ways you can add geometry to this class; you can add
	/// Entity objects directly with predetermined positions, scales and
	/// orientations, or you can add an entire SceneNode and it's subtree,
	/// including all the objects attached to it. Once you've added everthing
	/// you need to, you have to call build() the fix the geometry in place.
	/// <br/>
	/// This class is not a replacement for world geometry (see
	/// SceneManager.WorldGeometry). The single most efficient way to
	/// render large amounts of static geometry is to use a SceneManager which
	/// is specialised for dealing with that particular world structure.
	/// However, this class does provide you with a good 'halfway house'
	/// between generalised movable geometry (Entity) which works with all
	/// SceneManagers but isn't efficient when using very large numbers, and
	/// highly specialised world geometry which is extremely fast but not
	/// generic and typically requires custom world editors.
	/// <br/>
	/// You should not construct instances of this class directly; instead, call
	/// SceneManager.CreateStaticGeometry, which gives the SceneManager the
	/// option of providing you with a specialised version of this class if it
	/// wishes, and also handles the memory management for you like other
	/// classes.
	/// </remarks>
	/// Port started by jwace81
	/// OGRE Source File: http://cvs.sourceforge.net/viewcvs.py/ogre/ogrenew/OgreMain/src/OgreStaticGeometry.cpp?rev=1.22&amp;view=auto
	/// OGRE Header File: http://cvs.sourceforge.net/viewcvs.py/ogre/ogrenew/OgreMain/include/OgreStaticGeometry.h?rev=1.14&amp;view=auto
	public partial class StaticGeometry
	{
		#region Structs

		///<summary>
		///    Struct holding geometry optimised per SubMesh / LOD level, ready
		///    for copying to instances.
		///</summary>
		///<remarks>
		///    Since we're going to be duplicating geometry lots of times, it's
		///    far more important that we don't have redundant vertex data. If a
		///    SubMesh uses shared geometry, or we're looking at a lower LOD, not
		///    all the vertices are being referenced by faces on that submesh.
		///    Therefore to duplicate them, potentially hundreds or even thousands
		///    of times, would be extremely wasteful. Therefore, if a SubMesh at
		///    a given LOD has wastage, we create an optimised version of it's
		///    geometry which is ready for copying with no wastage.
		///
		///    The hierarchy is:
		///        StaticGeometry
		///        Region
		///        LODBucket
		///        MaterialBucket
		///        GeometryBucket
		///
		///    GeometryBucket is the layer at which different meshes that
		///    share the same material are combined.
		///</remarks>
		public class OptimisedSubMeshGeometry
		{
			public VertexData vertexData;
			public IndexData indexData;
		}

		///<summary>
		///    Saved link between SubMesh at a LOD and vertex/index data
		///    May point to original or optimised geometry
		///</summary>
		public class SubMeshLodGeometryLink
		{
			public VertexData vertexData;
			public IndexData indexData;
		}

		///<summary>
		///    Structure recording a queued submesh for the build
		///</summary>
		public class QueuedSubMesh
		{
			public SubMesh submesh;

			/// Link to LOD list of geometry, potentially optimised
			public List<SubMeshLodGeometryLink> geometryLodList;

			public string materialName;
			public Vector3 position;
			public Quaternion orientation;
			public Vector3 scale;
			// Pre-transformed world AABB
			public AxisAlignedBox worldBounds;
		}

		///<summary>
		///    Structure recording a queued geometry for low level builds
		///</summary>
		public struct QueuedGeometry
		{
			public SubMeshLodGeometryLink geometry;
			public Vector3 position;
			public Quaternion orientation;
			public Vector3 scale;
		}

		#endregion

		#region Fields and Properties

		protected SceneManager owner;
		protected string name;
		protected int logLevel;
		protected bool built;
		protected float upperDistance;
		protected float squaredUpperDistance;
		protected bool castShadows;
		protected Vector3 regionDimensions;
		protected Vector3 halfRegionDimensions;
		protected Vector3 origin;
		protected bool visible;
		protected RenderQueueGroupID renderQueueID;
		protected bool renderQueueIDSet;
		protected int buildCount;
		protected List<QueuedSubMesh> queuedSubMeshes;
		protected List<OptimisedSubMeshGeometry> optimisedSubMeshGeometryList;
		protected Dictionary<SubMesh, List<SubMeshLodGeometryLink>> subMeshGeometryLookup;
		protected Dictionary<uint, Region> regionMap;

		protected static int regionRange = 1024;
		protected static int regionHalfRange = 512;
		protected static int regionMaxIndex = 511;
		protected static int regionMinIndex = -512;

		/// <summary>
		///     This is the size of each dimension of a region; it can
		///     be set via a public property.  By default, set to 100 meters
		/// </summary>
		protected static int regionSize = 1000000;

		public string Name
		{
			get
			{
				return this.name;
			}
		}

		public float SquaredRenderingDistance
		{
			get
			{
				return this.squaredUpperDistance;
			}
		}

		public float RenderingDistance
		{
			get
			{
				return this.upperDistance;
			}
			set
			{
				this.upperDistance = value;
				this.squaredUpperDistance = this.upperDistance*this.upperDistance;
			}
		}

		public bool Visible
		{
			get
			{
				return this.visible;
			}
			set
			{
				this.visible = value;
				// tell any existing regions
				foreach ( var region in this.regionMap.Values )
				{
					region.IsVisible = value;
				}
			}
		}

		public bool CastShadows
		{
			get
			{
				return this.castShadows;
			}
			set
			{
				this.castShadows = value;
				// tell any existing regions
				foreach ( var region in this.regionMap.Values )
				{
					region.CastShadows = value;
				}
			}
		}

		public Vector3 RegionDimensions
		{
			get
			{
				return this.regionDimensions;
			}
			set
			{
				this.regionDimensions = value;
				this.halfRegionDimensions = value*0.5f;
			}
		}

		public int RegionSize
		{
			get
			{
				return regionSize;
			}
			set
			{
				regionSize = value;
			}
		}

		public Vector3 Origin
		{
			get
			{
				return this.origin;
			}
			set
			{
				this.origin = value;
			}
		}

		public RenderQueueGroupID RenderQueueID
		{
			get
			{
				return this.renderQueueID;
			}
			set
			{
				this.renderQueueIDSet = true;
				this.renderQueueID = value;
				// tell any existing regions
				foreach ( var region in this.regionMap.Values )
				{
					region.RenderQueueGroup = value;
				}
			}
		}

		public Dictionary<uint, Region> RegionMap
		{
			get
			{
				return this.regionMap;
			}
		}

		#endregion Fields and Properties

		#region Constructor

		public StaticGeometry( SceneManager owner, string name, int logLevel )
		{
			this.owner = owner;
			this.name = name;
			this.logLevel = logLevel;
			this.built = false;
			this.upperDistance = 0.0f;
			this.squaredUpperDistance = 0.0f;
			this.castShadows = false;
			this.regionDimensions = new Vector3( regionSize, regionSize, regionSize );
			this.halfRegionDimensions = this.regionDimensions*0.5f;
			this.origin = Vector3.Zero;
			this.visible = true;
			this.renderQueueID = RenderQueueGroupID.Main;
			this.renderQueueIDSet = false;
			this.buildCount = 0;
			this.subMeshGeometryLookup = new Dictionary<SubMesh, List<SubMeshLodGeometryLink>>();
			this.queuedSubMeshes = new List<QueuedSubMesh>();
			this.regionMap = new Dictionary<uint, Region>();
			this.optimisedSubMeshGeometryList = new List<OptimisedSubMeshGeometry>();
		}

		#endregion Constructor

		#region Protected Members

		protected Region GetRegion( AxisAlignedBox bounds, bool autoCreate )
		{
			if ( bounds == null )
			{
				return null;
			}

			// Get the region which has the largest overlapping volume
			var min = bounds.Minimum;
			var max = bounds.Maximum;

			// Get the min and max region indexes
			ushort minx, miny, minz;
			ushort maxx, maxy, maxz;
			GetRegionIndexes( min, out minx, out miny, out minz );
			GetRegionIndexes( max, out maxx, out maxy, out maxz );
			var maxVolume = 0.0f;
			ushort finalx = 0;
			ushort finaly = 0;
			ushort finalz = 0;
			for ( var x = minx; x <= maxx; ++x )
			{
				for ( var y = miny; y <= maxy; ++y )
				{
					for ( var z = minz; z <= maxz; ++z )
					{
						var vol = GetVolumeIntersection( bounds, x, y, z );
						if ( vol > maxVolume )
						{
							maxVolume = vol;
							finalx = x;
							finaly = y;
							finalz = z;
						}
					}
				}
			}
			Debug.Assert( maxVolume > 0.0f, "Static geometry: Problem determining closest volume match!" );
			return GetRegion( finalx, finaly, finalz, autoCreate );
		}

		protected float GetVolumeIntersection( AxisAlignedBox box, ushort x, ushort y, ushort z )
		{
			// Get bounds of indexed region
			var regionBounds = GetRegionBounds( x, y, z );
			var intersectBox = regionBounds.Intersection( box );
			// return a 'volume' which ignores zero dimensions
			// since we only use this for relative comparisons of the same bounds
			// this will still be internally consistent
			var boxdiff = box.Maximum - box.Minimum;
			var intersectDiff = intersectBox.Maximum - intersectBox.Minimum;

			return ( boxdiff.x == 0 ? 1 : intersectDiff.x )*( boxdiff.y == 0 ? 1 : intersectDiff.y )*
			       ( boxdiff.z == 0 ? 1 : intersectDiff.z );
		}

		protected AxisAlignedBox GetRegionBounds( ushort x, ushort y, ushort z )
		{
			var min = new Vector3( ( (float)x - regionHalfRange )*this.regionDimensions.x + this.origin.x,
			                       ( (float)y - regionHalfRange )*this.regionDimensions.y + this.origin.y,
			                       ( (float)z - regionHalfRange )*this.regionDimensions.z + this.origin.z );
			var max = min + this.regionDimensions;
			return new AxisAlignedBox( min, max );
		}

		protected Vector3 GetRegionCenter( ushort x, ushort y, ushort z )
		{
			return
				new Vector3( ( (float)x - regionHalfRange )*this.regionDimensions.x + this.origin.x + this.halfRegionDimensions.x,
				             ( (float)y - regionHalfRange )*this.regionDimensions.y + this.origin.y + this.halfRegionDimensions.y,
				             ( (float)z - regionHalfRange )*this.regionDimensions.z + this.origin.z + this.halfRegionDimensions.z );
		}

		protected Region GetRegion( ushort x, ushort y, ushort z, bool autoCreate )
		{
			var index = PackIndex( x, y, z );
			var region = GetRegion( index );
			if ( region == null && autoCreate )
			{
				// Make a name
				var str = string.Format( "{0}:{1}", this.name, index );
				// Calculate the region center
				var center = GetRegionCenter( x, y, z );
				region = new Region( this, str, this.owner, index, center );
				this.owner.InjectMovableObject( region );
				region.IsVisible = this.visible;
				region.CastShadows = this.castShadows;
				if ( this.renderQueueIDSet )
				{
					region.RenderQueueGroup = this.renderQueueID;
				}
				this.regionMap[ index ] = region;
			}
			return region;
		}

		protected Region GetRegion( uint index )
		{
			if ( this.regionMap.ContainsKey( index ) )
			{
				return this.regionMap[ index ];
			}
			else
			{
				return null;
			}
		}

		protected void GetRegionIndexes( Vector3 point, out ushort x, out ushort y, out ushort z )
		{
			// Scale the point into multiples of region and adjust for origin
			var scaledPoint = ( point - this.origin )/this.regionDimensions;

			// Round down to 'bottom left' point which represents the cell index
			var ix = (int)System.Math.Floor( scaledPoint.x );
			var iy = (int)System.Math.Floor( scaledPoint.y );
			var iz = (int)System.Math.Floor( scaledPoint.z );

			// Check bounds
			if ( ix < regionMinIndex || ix > regionMaxIndex || iy < regionMinIndex || iy > regionMaxIndex || iz < regionMinIndex ||
			     iz > regionMaxIndex )
			{
				throw new Exception( "Point out of bounds in StaticGeometry.GetRegionIndexes" );
			}
			// Adjust for the fact that we use unsigned values for simplicity
			// (requires less faffing about for negatives give 10-bit packing
			x = (ushort)( ix + regionHalfRange );
			y = (ushort)( iy + regionHalfRange );
			z = (ushort)( iz + regionHalfRange );
		}

		protected uint PackIndex( ushort x, ushort y, ushort z )
		{
			return (uint)( x + ( y << 10 ) + ( z << 20 ) );
		}

		protected Region GetRegion( Vector3 point, bool autoCreate )
		{
			ushort x, y, z;
			GetRegionIndexes( point, out x, out y, out z );
			return GetRegion( x, y, z, autoCreate );
		}

		protected AxisAlignedBox CalculateBounds( VertexData vertexData, Vector3 position, Quaternion orientation,
		                                          Vector3 scale )
		{
#if !AXIOM_SAFE_ONLY
			unsafe
#endif
			{
				var posElem = vertexData.vertexDeclaration.FindElementBySemantic( VertexElementSemantic.Position );
				var vbuf = vertexData.vertexBufferBinding.GetBuffer( posElem.Source );
				var src = vbuf.Lock( BufferLocking.ReadOnly );
				var vertex = src;

				var min = Vector3.Zero;
				var max = Vector3.Zero;
				var first = true;

				for ( var j = 0; j < vertexData.vertexCount; ++j, vertex += vbuf.VertexSize )
				{
					var pFloat = ( vertex + posElem.Offset ).ToFloatPointer();
					var pt = new Vector3( pFloat[ 0 ], pFloat[ 1 ], pFloat[ 2 ] );
					// Transform to world (scale, rotate, translate)
					pt = ( orientation*( pt*scale ) ) + position;
					if ( first )
					{
						min = max = pt;
						first = false;
					}
					else
					{
						min.Floor( pt );
						max.Ceil( pt );
					}
				}
				vbuf.Unlock();
				return new AxisAlignedBox( min, max );
			}
		}

		protected List<SubMeshLodGeometryLink> DetermineGeometry( SubMesh sm )
		{
			// First, determine if we've already seen this submesh before
			List<SubMeshLodGeometryLink> lodList;
			if ( this.subMeshGeometryLookup.TryGetValue( sm, out lodList ) )
			{
				return lodList;
			}
			// Otherwise, we have to create a new one
			lodList = new List<SubMeshLodGeometryLink>();
			this.subMeshGeometryLookup[ sm ] = lodList;
			var numLods = sm.Parent.IsLodManual ? 1 : sm.Parent.LodLevelCount;
			for ( var lod = 0; lod < numLods; ++lod )
			{
				var geomLink = new SubMeshLodGeometryLink();
				lodList.Add( geomLink );
				var lodIndexData = lod == 0 ? sm.indexData : sm.LodFaceList[ lod - 1 ];
				// Can use the original mesh geometry?
				if ( sm.useSharedVertices )
				{
					if ( sm.Parent.SubMeshCount == 1 )
					{
						// Ok, this is actually our own anyway
						geomLink.vertexData = sm.Parent.SharedVertexData;
						geomLink.indexData = lodIndexData;
					}
					else
					{
						// We have to split it
						SplitGeometry( sm.Parent.SharedVertexData, lodIndexData, geomLink );
					}
				}
				else
				{
					if ( lod == 0 )
					{
						// Ok, we can use the existing geometry; should be in full
						// use by just this SubMesh
						geomLink.vertexData = sm.VertexData;
						geomLink.indexData = sm.IndexData;
					}
					else
					{
						// We have to split it
						SplitGeometry( sm.VertexData, lodIndexData, geomLink );
					}
				}
				Debug.Assert( geomLink.vertexData.vertexStart == 0,
				              "Cannot use vertexStart > 0 on indexed geometry due to " +
				              "rendersystem incompatibilities - see the docs!" );
			}
			return lodList;
		}

		protected void SplitGeometry( VertexData vd, IndexData id, SubMeshLodGeometryLink targetGeomLink )
		{
#if !AXIOM_SAFE_ONLY
			unsafe
#endif
			{
				if ( this.logLevel <= 1 )
				{
					LogManager.Instance.Write( "StaticGeometry.SplitGeometry called" );
				}
				// Firstly we need to scan to see how many vertices are being used
				// and while we're at it, build the remap we can use later
				var use32bitIndexes = id.indexBuffer.Type == IndexType.Size32;
				var indexRemap = new Dictionary<int, int>();
				var src = id.indexBuffer.Lock( BufferLocking.ReadOnly );
				indexRemap.Clear();
				if ( use32bitIndexes )
				{
					var p32 = src.ToIntPointer();
					for ( var i = 0; i < id.indexCount; ++i )
					{
						indexRemap[ p32[ i ] ] = indexRemap.Count;
					}
				}
				else
				{
					var p16 = src.ToShortPointer();
					for ( var i = 0; i < id.indexCount; ++i )
					{
						indexRemap[ p16[ i ] ] = indexRemap.Count;
					}
				}
				id.indexBuffer.Unlock();
				if ( indexRemap.Count == vd.vertexCount )
				{
					// ha, complete usage after all
					targetGeomLink.vertexData = vd;
					targetGeomLink.indexData = id;
					return;
				}

				// Create the new vertex data records
				targetGeomLink.vertexData = vd.Clone( false );
				// Convenience
				var newvd = targetGeomLink.vertexData;
				//IndexData newid = targetGeomLink.IndexData;
				// Update the vertex count
				newvd.vertexCount = indexRemap.Count;

				var numvbufs = vd.vertexBufferBinding.BindingCount;
				// Copy buffers from old to new
				for ( short b = 0; b < numvbufs; ++b )
				{
					// Lock old buffer
					var oldBuf = vd.vertexBufferBinding.GetBuffer( b );
					// Create new buffer
					var newBuf = HardwareBufferManager.Instance.CreateVertexBuffer( oldBuf.VertexDeclaration, indexRemap.Count,
					                                                                BufferUsage.Static );
					// rebind
					newvd.vertexBufferBinding.SetBinding( b, newBuf );

					// Copy all the elements of the buffer across, by iterating over
					// the IndexRemap which describes how to move the old vertices
					// to the new ones. By nature of the map the remap is in order of
					// indexes in the old buffer, but note that we're not guaranteed to
					// address every vertex (which is kinda why we're here)
					var vdSrc = oldBuf.Lock( BufferLocking.ReadOnly );
					var pSrcBase = vdSrc;
					var vdDest = newBuf.Lock( BufferLocking.Discard );
					var pDstBase = vdDest;
					var vertexSize = oldBuf.VertexSize;
					// Buffers should be the same size
					Debug.Assert( vertexSize == newBuf.VertexSize );

					foreach ( var pair in indexRemap )
					{
						Debug.Assert( pair.Key < oldBuf.VertexCount );
						Debug.Assert( pair.Value < newBuf.VertexCount );

						var pSrc = ( pSrcBase + pair.Key*vertexSize ).ToBytePointer();
						var pDst = ( pDstBase + pair.Value*vertexSize ).ToBytePointer();
						for ( var i = 0; i < vertexSize; i++ )
						{
							pDst[ i ] = pSrc[ i ];
						}
					}
					// unlock
					oldBuf.Unlock();
					newBuf.Unlock();
				}

				// Now create a new index buffer
				var ibuf = HardwareBufferManager.Instance.CreateIndexBuffer( id.indexBuffer.Type, id.indexCount, BufferUsage.Static );

				var idSrc = id.indexBuffer.Lock( BufferLocking.ReadOnly );
				var idDest = ibuf.Lock( BufferLocking.Discard );
				if ( use32bitIndexes )
				{
					var pSrc32 = idSrc.ToIntPointer();
					var pDst32 = idDest.ToIntPointer();
					for ( var i = 0; i < id.indexCount; ++i )
					{
						pDst32[ i ] = indexRemap[ pSrc32[ i ] ];
					}
				}
				else
				{
					var pSrc16 = idSrc.ToUShortPointer();
					var pDst16 = idDest.ToUShortPointer();
					for ( var i = 0; i < id.indexCount; ++i )
					{
						pDst16[ i ] = (ushort)indexRemap[ pSrc16[ i ] ];
					}
				}
				id.indexBuffer.Unlock();
				ibuf.Unlock();

				targetGeomLink.indexData = new IndexData();
				targetGeomLink.indexData.indexStart = 0;
				targetGeomLink.indexData.indexCount = id.indexCount;
				targetGeomLink.indexData.indexBuffer = ibuf;

				// Store optimised geometry for deallocation later
				var optGeom = new OptimisedSubMeshGeometry();
				optGeom.indexData = targetGeomLink.indexData;
				optGeom.vertexData = targetGeomLink.vertexData;
				this.optimisedSubMeshGeometryList.Add( optGeom );
			}
		}

		#endregion Protected Members

		#region Public Members

		/// <summary>
		///     Adds an Entity to the static geometry.
		/// </summary>
		/// <remarks>
		///     This method takes an existing Entity and adds its details to the
		///     list of	elements to include when building. Note that the Entity
		///     itself is not copied or referenced in this method; an Entity is
		///     passed simply so that you can change the materials of attached
		///     SubEntity objects if you want. You can add the same Entity
		///     instance multiple times with different material settings
		///     completely safely, and destroy the Entity before destroying
		///     this StaticGeometry if you like. The Entity passed in is simply
		///     used as a definition.
		///
		///     Note: Must be called before 'build'.
		/// </remarks>
		/// <param name="ent">The Entity to use as a definition (the Mesh and Materials</param>
		/// <param name="position">The world position at which to add this Entity</param>
		/// <param name="orientation">The world orientation at which to add this Entity</param>
		/// <param name="scale">The scale at which to add this entity</param>
		public void AddEntity( Entity ent, Vector3 position, Quaternion orientation, Vector3 scale )
		{
			var msh = ent.Mesh;
			// Validate
			if ( msh.IsLodManual )
			{
				LogManager.Instance.Write( "WARNING (StaticGeometry): Manual LOD is not supported. " +
				                           "Using only highest LOD level for mesh " + msh.Name );
			}
			// queue this entities submeshes and choice of material
			// also build the lists of geometry to be used for the source of lods
			foreach ( SubEntity se in ent.SubEntities )
			{
				var q = new QueuedSubMesh();

				// Get the geometry for this SubMesh
				q.submesh = se.SubMesh;
				q.geometryLodList = DetermineGeometry( q.submesh );
				q.materialName = se.MaterialName;
				q.orientation = orientation;
				q.position = position;
				q.scale = scale;
				// Determine the bounds based on the highest LOD
				q.worldBounds = CalculateBounds( q.geometryLodList[ 0 ].vertexData, position, orientation, scale );
				this.queuedSubMeshes.Add( q );
			}
		}

		/// <summary>
		///     Adds all the Entity objects attached to a SceneNode and all it's
		///     children to the static geometry.
		/// </summary>
		/// <remarks>
		///     This method performs just like addEntity, except it adds all the
		///     entities attached to an entire sub-tree to the geometry.
		///     The position / orientation / scale parameters are taken from the
		///     node structure instead of being specified manually.
		/// </remarks>
		/// <note>
		///     The SceneNode you pass in will not be automatically detached from
		///     it's parent, so if you have this node already attached to the scene
		///     graph, you will need to remove it if you wish to avoid the overhead
		///     of rendering <i>both</i> the original objects and their new static
		///     versions! We don't do this for you incase you are preparing this
		///     in advance and so don't want the originals detached yet.
		/// </note>
		/// <param name="node">Pointer to the node to use to provide a set of Entity templates</param>
		public void AddSceneNode( SceneNode node )
		{
			foreach ( var mobj in node.Objects )
			{
				if ( mobj is Entity )
				{
					AddEntity( (Entity)mobj, node.DerivedPosition, node.DerivedOrientation, node.DerivedScale );
				}
			}
			// recursively add the child-nodes
			foreach ( SceneNode child in node.Children )
			{
				AddSceneNode( child );
			}
		}

		/// <summary>
		///     Build the geometry.
		/// </summary>
		/// <remarks>
		///     Based on all the entities which have been added, and the batching
		///     options which have been set, this method constructs	the batched
		///     geometry structures required. The batches are added to the scene
		///     and will be rendered unless you specifically hide them.
		/// </remarks>
		/// <note>
		///     Once you have called this method, you can no longer add any more
		///     entities.
		/// </note>
		public void Build()
		{
			if ( this.logLevel <= 1 )
			{
				LogManager.Instance.Write( "Building new static geometry {0}", this.name );
			}

			this.buildCount++;

			// Make sure there's nothing from previous builds
			Destroy();

			// Firstly allocate meshes to regions
			foreach ( var qsm in this.queuedSubMeshes )
			{
				var region = GetRegion( qsm.worldBounds, true );
				region.Assign( qsm );
			}
			var stencilShadows = false;
			if ( this.castShadows && this.owner.IsShadowTechniqueStencilBased )
			{
				stencilShadows = true;
			}

			// Now tell each region to build itself
			foreach ( var region in this.regionMap.Values )
			{
				region.Build( stencilShadows, this.logLevel );
			}

			if ( this.logLevel <= 1 )
			{
				LogManager.Instance.Write( "Finished building new static geometry {0}", this.name );
				Dump();
			}
		}

		/// <summary>
		///     Destroys all the built geometry state (reverse of build).
		/// </summary>
		/// <remarks>
		///     You can call build() again after this and it will pick up all the
		///     same entities / nodes you queued last time.
		/// </remarks>
		public void Destroy()
		{
			foreach ( var region in this.regionMap.Values )
			{
				this.owner.ExtractMovableObject( region );
				region.Dispose();
			}
			this.regionMap.Clear();
		}

		/// <summary>
		///     Clears any of the entities / nodes added to this geometry and
		///     destroys anything which has already been built.
		/// </summary>
		public void Reset()
		{
			Destroy();
			this.queuedSubMeshes.Clear();
			this.subMeshGeometryLookup.Clear();
			var bm = HardwareBufferManager.Instance;
			foreach ( var smg in this.optimisedSubMeshGeometryList )
			{
				bm.DestroyVertexBufferBinding( smg.vertexData.vertexBufferBinding );
				bm.DestroyVertexDeclaration( smg.vertexData.vertexDeclaration );
			}
			this.optimisedSubMeshGeometryList.Clear();
		}

		public void Dump()
		{
			LogManager.Instance.Write( "Static Geometry Report for {0}", this.name );
			LogManager.Instance.Write( "-------------------------------------------------" );
			LogManager.Instance.Write( "Build Count: {0}", this.buildCount );
			LogManager.Instance.Write( "Number of queued submeshes: {0}", this.queuedSubMeshes.Count );
			LogManager.Instance.Write( "Number of regions: {0}", this.regionMap.Count );
			LogManager.Instance.Write( "Region dimensions: {0}", this.regionDimensions );
			LogManager.Instance.Write( "Origin: {0}", this.origin );
			LogManager.Instance.Write( "Max distance: {0}", this.upperDistance );
			LogManager.Instance.Write( "Casts shadows?: {0}", this.castShadows );
			foreach ( var region in this.regionMap.Values )
			{
				region.Dump();
			}
			LogManager.Instance.Write( "-------------------------------------------------" );
		}

		#endregion Public Members
	}
}