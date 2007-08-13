#region LGPL License
/*
Axiom Graphics Engine Library
Copyright (C) 2003-2006  Axiom Project Team

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
using System.Text;
using System.IO;

using Axiom.Graphics;
using Axiom.Math;
using Axiom.Collections;

#endregion Namespace Declarations
			
namespace Axiom.Core
{
	public partial class StaticGeometry
	{

		/// <summary>
		/// A MaterialBucket is a collection of smaller buckets with the same 
		/// Material (and implicitly the same LOD).
		/// </summary>
		public class MaterialBucket : IDisposable
		{
			#region Fields and Properties

			protected LODBucket parent;
			protected string materialName;
			protected Material material;
			protected Technique technique;

			protected List<GeometryBucket> geometryBucketList;
			protected Dictionary<string, GeometryBucket> currentGeometryMap;

			public string MaterialName
			{
				get
				{
					return materialName;
				}
			}

			public LODBucket Parent
			{
				get
				{
					return parent;
				}
			}

			public Material Material
			{
				get
				{
					return material;
				}
			}

			public Technique CurrentTechnique
			{
				get
				{
					return technique;
				}
			}

			public List<GeometryBucket> GeometryBucketList
			{
				get
				{
					return geometryBucketList;
				}
			}

			#endregion

			#region Constructors

			public MaterialBucket( LODBucket parent, string materialName )
			{
				this.parent = parent;
				this.materialName = materialName;
				geometryBucketList = new List<GeometryBucket>();
				currentGeometryMap = new Dictionary<string, GeometryBucket>();
			}

			#endregion

			#region Proteced Methods
			protected string GetGeometryFormatString( SubMeshLodGeometryLink geom )
			{
				// Formulate an identifying string for the geometry format
				// Must take into account the vertex declaration and the index type
				// Format is (all lines separated by '|'):
				// Index type
				// Vertex element (repeating)
				//   source
				//   semantic
				//   type
				string str = string.Format( "{0}|", geom.indexData.indexBuffer.Type );

				for ( int i = 0; i < geom.vertexData.vertexDeclaration.ElementCount; ++i )
				{
					VertexElement elem = geom.vertexData.vertexDeclaration.GetElement( i );
					str += string.Format( "{0}|{0}|{1}|{2}|", elem.Source, elem.Semantic, elem.Type );
				}
				return str;
			}

			#endregion

			#region Public Methods
			public void Assign( QueuedGeometry qgeom )
			{
				// Look up any current geometry
				string formatString = GetGeometryFormatString( qgeom.geometry );
				bool newBucket = true;
				if ( currentGeometryMap.ContainsKey( formatString ) )
				{
					GeometryBucket gbucket = currentGeometryMap[ formatString ];
					// Found existing geometry, try to assign
					newBucket = !gbucket.Assign( qgeom );
					// Note that this bucket will be replaced as the 'current'
					// for this format string below since it's out of space
				}
				// Do we need to create a new one?
				if ( newBucket )
				{
					GeometryBucket gbucket = new GeometryBucket( this, formatString, qgeom.geometry.vertexData, qgeom.geometry.indexData );
					// Add to main list
					geometryBucketList.Add( gbucket );
					// Also index in 'current' list
					currentGeometryMap[ formatString ] = gbucket;
					if ( !gbucket.Assign( qgeom ) )
					{
						throw new AxiomException( "Somehow we couldn't fit the requested geometry even in a " +
							"brand new GeometryBucket!! Must be a bug, please report." );
					}
				}
			}

			public void Build( bool stencilShadows, int logLevel )
			{
				if ( logLevel <= 1 )
					LogManager.Instance.Write( "MaterialBucket.Build: Building material {0}", materialName );
				material = (Material)MaterialManager.Instance[ materialName ];
				if ( null == material )
					throw new AxiomException( "Material '{0}' not found.", materialName );
				material.Load();
				// tell the geometry buckets to build
				foreach ( GeometryBucket gbucket in geometryBucketList )
					gbucket.Build( stencilShadows, logLevel );
			}

			public void AddRenderables( RenderQueue queue, RenderQueueGroupID group, float camDistanceSquared )
			{
				// determine the current material technique
				technique = material.GetBestTechnique( material.GetLodIndexSquaredDepth( camDistanceSquared ) );
				foreach ( GeometryBucket gbucket in geometryBucketList )
					queue.AddRenderable( gbucket, RenderQueue.DEFAULT_PRIORITY, group );
			}

			public void Dump()
			{
				LogManager.Instance.Write( "Material Bucket {0}", materialName );
				LogManager.Instance.Write( "--------------------------------------------------" );
				LogManager.Instance.Write( "Geometry buckets: {0}", geometryBucketList.Count );
				foreach ( GeometryBucket gbucket in geometryBucketList )
					gbucket.Dump();
				LogManager.Instance.Write( "--------------------------------------------------" );
			}

			/// <summary>
			///     Dispose the geometry buckets
			/// </summary>
			public virtual void Dispose()
			{
				foreach ( GeometryBucket gbucket in geometryBucketList )
					gbucket.Dispose();
			}

			#endregion
		}
	}
}
