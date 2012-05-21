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
using Axiom.Graphics;
using Axiom.Math;

#endregion Namespace Declarations

namespace Axiom.Core
{
	public partial class StaticGeometry
	{
		/// <summary>
		/// A LODBucket is a collection of smaller buckets with the same LOD.
		/// </summary>
		/// <remarks>
		/// LOD refers to Mesh LOD here. Material LOD can change separately
		/// at the next bucket down from this.
		/// </remarks>
		public class LODBucket : DisposableObject
		{
			#region Fields and Properties

			protected Region parent;
			protected ushort lod;
			protected Real squaredDistance;
			protected Dictionary<string, MaterialBucket> materialBucketMap;
			protected List<QueuedGeometry> queuedGeometryList;

			public Region Parent
			{
				get
				{
					return this.parent;
				}
			}

			public ushort Lod
			{
				get
				{
					return this.lod;
				}
			}

			public Real SquaredDistance
			{
				get
				{
					return this.squaredDistance;
				}
			}

			public Dictionary<string, MaterialBucket> MaterialBucketMap
			{
				get
				{
					return this.materialBucketMap;
				}
			}

			#endregion

			#region Constructors

			public LODBucket( Region parent, ushort lod, float lodDist )
				: base()
			{
				this.parent = parent;
				this.lod = lod;
				this.squaredDistance = lodDist;
				this.materialBucketMap = new Dictionary<string, MaterialBucket>();
				this.queuedGeometryList = new List<QueuedGeometry>();
			}

			#endregion

			#region Public Methods

			public void Assign( QueuedSubMesh qsm, ushort atlod )
			{
				var q = new QueuedGeometry();
				this.queuedGeometryList.Add( q );
				q.position = qsm.position;
				q.orientation = qsm.orientation;
				q.scale = qsm.scale;
				if ( qsm.geometryLodList.Count > atlod )
				{
					// This submesh has enough lods, use the right one
					q.geometry = (SubMeshLodGeometryLink)qsm.geometryLodList[ atlod ];
				}
				else
				{
					// Not enough lods, use the lowest one we have
					q.geometry = (SubMeshLodGeometryLink)qsm.geometryLodList[ qsm.geometryLodList.Count - 1 ];
				}
				// Locate a material bucket
				MaterialBucket mbucket;
				if ( this.materialBucketMap.ContainsKey( qsm.materialName ) )
				{
					mbucket = this.materialBucketMap[ qsm.materialName ];
				}
				else
				{
					mbucket = new MaterialBucket( this, qsm.materialName );
					this.materialBucketMap.Add( qsm.materialName, mbucket );
				}
				mbucket.Assign( q );
			}

			public void Build( bool stencilShadows, int logLevel )
			{
				// Just pass this on to child buckets
				foreach ( var mbucket in this.materialBucketMap.Values )
				{
					mbucket.Build( stencilShadows, logLevel );
				}
			}

			public void AddRenderables( RenderQueue queue, RenderQueueGroupID group, float camSquaredDistance )
			{
				// Just pass this on to child buckets
				foreach ( var mbucket in this.materialBucketMap.Values )
				{
					mbucket.AddRenderables( queue, group, camSquaredDistance );
				}
			}

			public void Dump()
			{
				LogManager.Instance.Write( "LOD Bucket {0}", this.lod );
				LogManager.Instance.Write( "------------------" );
				LogManager.Instance.Write( "Distance: {0}", Utility.Sqrt( this.squaredDistance ) );
				LogManager.Instance.Write( "Number of Materials: {0}", this.materialBucketMap.Count );
				foreach ( var mbucket in this.materialBucketMap.Values )
				{
					mbucket.Dump();
				}
				LogManager.Instance.Write( "------------------" );
			}

			/// <summary>
			///     Dispose the material buckets
			/// </summary>
			protected override void dispose( bool disposeManagedResources )
			{
				if ( !IsDisposed )
				{
					if ( disposeManagedResources )
					{
						if ( this.materialBucketMap != null )
						{
							foreach ( var mbucket in this.materialBucketMap.Values )
							{
								if ( !mbucket.IsDisposed )
								{
									mbucket.Dispose();
								}
							}
							this.materialBucketMap.Clear();
							this.materialBucketMap = null;
						}
					}
				}

				base.dispose( disposeManagedResources );
			}

			#endregion
		}
	}
}