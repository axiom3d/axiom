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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.IO;

using Axiom.Graphics;
using Axiom.Math;
using Axiom.Collections;
using Axiom.Graphics.Collections;
using Axiom.Core.Collections;

#endregion Namespace Declarations

namespace Axiom.Core
{
	public partial class StaticGeometry
	{
		/// <summary>
		/// The details of a topological region which is the highest level of
		/// partitioning for this class.
		/// </summary>
		/// <remarks>
		/// The size & shape of regions entirely depends on the SceneManager
		/// specific implementation. It is a MovableObject since it will be
		/// attached to a node based on the local centre - in practice it
		/// won't actually move (although in theory it could).
		/// </remarks>
		public class Region : MovableObject, IDisposable
		{
			#region Inner Classes

			public class RegionShadowRenderable : ShadowRenderable
			{
				protected Region parent;
				protected HardwareVertexBuffer positionBuffer;
				protected HardwareVertexBuffer wBuffer;

				public RegionShadowRenderable( Region parent, HardwareIndexBuffer indexBuffer, VertexData vertexData, bool createSeparateLightCap, bool isLightCap )
				{
					throw new NotImplementedException();
				}

				public RegionShadowRenderable( Region parent, HardwareIndexBuffer indexBuffer, VertexData vertexData, bool createSeparateLightCap )
					: this( parent, indexBuffer, vertexData, createSeparateLightCap, false )
				{
				}

				public HardwareVertexBuffer PositionBuffer
				{
					get
					{
						return positionBuffer;
					}
				}

				public HardwareVertexBuffer WBuffer
				{
					get
					{
						return wBuffer;
					}
				}

				public override void GetWorldTransforms( Matrix4[] matrices )
				{
					matrices[ 0 ] = parent.ParentNodeFullTransform;
				}

				public override Quaternion WorldOrientation
				{
					get
					{
						return parent.ParentNode.DerivedOrientation;
					}
				}

				public override Vector3 WorldPosition
				{
					get
					{
						return parent.Center;
					}
				}
			}

			#endregion

			#region Fields and Properties

			protected StaticGeometry parent;
			protected SceneManager sceneMgr;
			protected SceneNode node;
			protected List<QueuedSubMesh> queuedSubMeshes;
			protected UInt32 regionID;
			protected Vector3 center;
			protected LodValueList lodValues;
			protected LodStrategy lodStrategy;
			protected AxisAlignedBox aabb;
			protected float boundingRadius;
			protected ushort currentLod;
			protected float camDistanceSquared;
			protected List<LODBucket> lodBucketList;
			protected EdgeData edgeList;
			protected ShadowRenderableList shadowRenderables;
			protected bool vertexProgramInUse;

			public StaticGeometry Parent
			{
				get
				{
					return parent;
				}
			}

			public UInt32 ID
			{
				get
				{
					return regionID;
				}
			}

			public Vector3 Center
			{
				get
				{
					return center;
				}
			}

			public override AxisAlignedBox BoundingBox
			{
				get
				{
					return aabb;
				}
			}

			// TODO: Is this right?
			public ushort NumWorldTransforms
			{
				get
				{
					return 1;
				}
			}

			public override float BoundingRadius
			{
				get
				{
					return boundingRadius;
				}
			}

			public LightList Lights
			{
				get
				{
					// Make sure we only update this once per frame no matter how many
					// times we're asked
					ulong frame = Root.Instance.CurrentFrameCount;
					if ( frame > lightListUpdated )
					{
						lightList = node.FindLights( boundingRadius );
						lightListUpdated = frame;
					}
					return lightList;
				}
			}

			public EdgeData EdgeList
			{
				get
				{
					return edgeList;
				}
			}

			public List<LODBucket> LodBucketList
			{
				get
				{
					return lodBucketList;
				}
			}

			#endregion

			#region Constructors

			public Region( StaticGeometry parent, string name, SceneManager mgr, UInt32 regionID, Vector3 center )
				: base( name )
			{
				this.MovableType = "StaticGeometry";
				this.parent = parent;
				this.sceneMgr = mgr;
				this.regionID = regionID;
				this.center = center;
				queuedSubMeshes = new List<QueuedSubMesh>();
				lodValues = new LodValueList();
				aabb = new AxisAlignedBox();
				lodBucketList = new List<LODBucket>();
				shadowRenderables = new ShadowRenderableList();
			}

			#endregion

			#region Public Methods

			public void Assign( QueuedSubMesh qsm )
			{
				queuedSubMeshes.Add( qsm );

				// update lod distances
				Mesh mesh = qsm.submesh.Parent;
				LodStrategy lodStrategy = mesh.LodStrategy;
				if ( this.lodStrategy == null )
				{
					this.lodStrategy = lodStrategy;
					// First LOD mandatory, and always from base lod value
					this.lodValues.Add( this.lodStrategy.BaseValue );
				}
				else
				{
					if ( this.lodStrategy != lodStrategy )
						throw new AxiomException( "Lod strategies do not match." );
				}

				int lodLevels = mesh.LodLevelCount;
				if ( qsm.geometryLodList.Count != lodLevels )
				{
					string msg = string.Format( "QueuedSubMesh '{0}' lod count of {1} does not match parent count of {2}", qsm.submesh.Name, qsm.geometryLodList.Count, lodLevels );
					throw new AxiomException( msg );
				}

				while ( lodValues.Count < lodLevels )
				{
					lodValues.Add( 0.0f );
				}
				// Make sure LOD levels are max of all at the requested level
				for ( ushort lod = 1; lod < lodLevels; ++lod )
				{
					MeshLodUsage meshLod = qsm.submesh.Parent.GetLodLevel( lod );
					lodValues[ lod ] = Utility.Max( (float)lodValues[ lod ], meshLod.Value );
				}

				// update bounds
				// Transform world bounds relative to our center
				AxisAlignedBox localBounds = new AxisAlignedBox( qsm.worldBounds.Minimum - center, qsm.worldBounds.Maximum - center );
				aabb.Merge( localBounds );
				foreach ( Vector3 corner in localBounds.Corners )
				{
					boundingRadius = Utility.Max( boundingRadius, corner.Length );
				}
			}

			public void Build( bool stencilShadows, int logLevel )
			{
				// Create a node
				node = sceneMgr.RootSceneNode.CreateChildSceneNode( name, center );
				node.AttachObject( this );
				// We need to create enough LOD buckets to deal with the highest LOD
				// we encountered in all the meshes queued
				for ( ushort lod = 0; lod < lodValues.Count; ++lod )
				{
					LODBucket lodBucket = new LODBucket( this, lod, (float)lodValues[ lod ] );
					lodBucketList.Add( lodBucket );
					// Now iterate over the meshes and assign to LODs
					// LOD bucket will pick the right LOD to use
					IEnumerator iter = queuedSubMeshes.GetEnumerator();
					while ( iter.MoveNext() )
					{
						QueuedSubMesh qsm = (QueuedSubMesh)iter.Current;
						lodBucket.Assign( qsm, lod );
					}
					// now build
					lodBucket.Build( stencilShadows, logLevel );
				}

				// Do we need to build an edge list?
				if ( stencilShadows )
				{
					EdgeListBuilder eb = new EdgeListBuilder();
					int vertexSet = 0;
					foreach ( LODBucket lod in lodBucketList )
					{
						foreach ( MaterialBucket mat in lod.MaterialBucketMap.Values )
						{
							// Check if we have vertex programs here
							Technique t = mat.Material.GetBestTechnique();
							if ( null != t )
							{
								Pass p = t.GetPass( 0 );
								if ( null != p )
								{
									if ( p.HasVertexProgram )
									{
										vertexProgramInUse = true;
									}
								}
							}

							foreach ( GeometryBucket geom in mat.GeometryBucketList )
							{
								// Check we're dealing with 16-bit indexes here
								// Since stencil shadows can only deal with 16-bit
								// More than that and stencil is probably too CPU-heavy
								// in any case
								if ( geom.IndexData.indexBuffer.Type != IndexType.Size16 )
								{
									throw new AxiomException( "Only 16-bit indexes allowed when using stencil shadows" );
								}
								eb.AddVertexData( geom.VertexData );
								eb.AddIndexData( geom.IndexData );
							}
						}
					}
					edgeList = eb.Build();
				}
			}

			public override void NotifyCurrentCamera( Camera cam )
			{
				// Determine active lod
				Vector3 diff = cam.DerivedPosition - center;
				// Distance from the edge of the bounding sphere
				camDistanceSquared = diff.LengthSquared - boundingRadius * boundingRadius;
				// Clamp to 0
				camDistanceSquared = Utility.Max( 0.0f, camDistanceSquared );

				float maxDist = parent.SquaredRenderingDistance;
				if ( parent.RenderingDistance > 0 && camDistanceSquared > maxDist && cam.UseRenderingDistance )
				{
					beyondFarDistance = true;
				}
				else
				{
					beyondFarDistance = false;

					currentLod = (ushort)( lodValues.Count - 1 );
					for ( ushort i = 0; i < lodValues.Count; ++i )
					{
						if ( (float)lodValues[ i ] > camDistanceSquared )
						{
							currentLod = (ushort)( i - 1 );
							break;
						}
					}
				}
			}

			public override void UpdateRenderQueue( RenderQueue queue )
			{
				LODBucket lodBucket = lodBucketList[ currentLod ];
				lodBucket.AddRenderables( queue, renderQueueID, camDistanceSquared );
			}

			public override bool IsVisible
			{
				get
				{
					return isVisible && !beyondFarDistance;
				}
			}

			public IEnumerator GetShadowVolumeRenderableIterator( ShadowTechnique shadowTechnique, Light light, HardwareIndexBuffer indexBuffer,
																  bool extrudeVertices, float extrusionDistance, ulong flags )
			{
				Debug.Assert( indexBuffer != null, "Only external index buffers are supported right now" );
				Debug.Assert( indexBuffer.Type == IndexType.Size16, "Only 16-bit indexes supported for now" );

				// Calculate the object space light details
				Vector4 lightPos = light.GetAs4DVector();
				Matrix4 world2Obj = parentNode.FullTransform.Inverse();
				lightPos = world2Obj * lightPos;

				// We need to search the edge list for silhouette edges
				if ( edgeList == null )
				{
					throw new Exception( "You enabled stencil shadows after the buid process!  In " +
										 "Region.GetShadowVolumeRenderableIterator" );
				}

				// Init shadow renderable list if required
				bool init = shadowRenderables.Count == 0;

				RegionShadowRenderable esr = null;
				//bool updatedSharedGeomNormals = false;
				for ( int i = 0; i < edgeList.EdgeGroups.Count; i++ )
				{
					EdgeData.EdgeGroup group = (EdgeData.EdgeGroup)edgeList.EdgeGroups[ i ];
					if ( init )
					{
						// Create a new renderable, create a separate light cap if
						// we're using a vertex program (either for this model, or
						// for extruding the shadow volume) since otherwise we can
						// get depth-fighting on the light cap
						esr = new RegionShadowRenderable( this, indexBuffer, group.vertexData, vertexProgramInUse || !extrudeVertices );
						shadowRenderables.Add( esr );
					}
					else
					{
						esr = (RegionShadowRenderable)shadowRenderables[ i ];
					}
					// Extrude vertices in software if required
					if ( extrudeVertices )
					{
						ExtrudeVertices( esr.PositionBuffer, group.vertexData.vertexCount, lightPos, extrusionDistance );
					}
				}
				return (IEnumerator)shadowRenderables;
			}

			public void Dump()
			{
				LogManager.Instance.Write( "Region {0}", regionID );
				LogManager.Instance.Write( "--------------------------" );
				LogManager.Instance.Write( "Center: {0}", center );
				LogManager.Instance.Write( "Local AABB: {0}", aabb );
				LogManager.Instance.Write( "Bounding radius: {0}", boundingRadius );
				LogManager.Instance.Write( "Number of LODs: {0}", lodBucketList.Count );
				foreach ( LODBucket lodBucket in lodBucketList )
				{
					lodBucket.Dump();
				}
				LogManager.Instance.Write( "--------------------------" );
			}

			#endregion

			/// <summary>
			///     Remove the region from the scene graph
			/// </summary>
			protected override void dispose( bool disposeManagedResources )
			{
				if ( !IsDisposed )
				{
					if ( disposeManagedResources )
					{
						if ( node != null )
						{
							node.RemoveFromParent();
							sceneMgr.DestroySceneNode( node );
							node = null;
						}

						foreach ( LODBucket lodBucket in lodBucketList )
						{
							lodBucket.Dispose();
						}
						lodBucketList.Clear();
					}
				}
			}
			#region MovableObject Implementation

			/// <summary>
			/// Get the 'type flags' for this <see cref="Region"/>.
			/// </summary>
			/// <seealso cref="MovableObject.TypeFlags"/>
			public override uint TypeFlags
			{
				get
				{
					return (uint)SceneQueryTypeMask.StaticGeometry;
				}
			}

			#endregion MovableObject Implementation
		}
	}
}