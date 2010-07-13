using System.Collections.Generic;
using Axiom.Core;
using Axiom.Math;

namespace OctreeZone
{
	public class TerrainZoneRow : List<TerrainZoneRenderable> { }
	public class TerrainZone2D : List<TerrainZoneRow> { }

	public class TerrainZonePage
	{
		/// 2-dimensional vector of tiles, pre-allocated to the correct size
		protected internal TerrainZone2D tiles = new TerrainZone2D();
		/// The number of tiles across a page
		ushort tilesPerPage;
		/// The scene node to which all the tiles for this page are attached
		protected SceneNode pageSceneNode;

		public TerrainZonePage( ushort numTiles )
		{
			tilesPerPage = numTiles;
			// Set up an empty array of TerrainZoneRenderable pointers
			int i, j;
			for ( i = 0; i < tilesPerPage; i++ )
			{
				tiles.Add( new TerrainZoneRow() );

				for ( j = 0; j < tilesPerPage; j++ )
				{
					tiles[ i ].Add( null );
				}
			}
			pageSceneNode = null;
		}

		public SceneNode PageSceneNode
		{
			get
			{
				return pageSceneNode;
			}
			set
			{
				pageSceneNode = value;
			}
		}

		public void LinkNeighbours()
		{
			//setup the neighbor links.

			for ( int j = 0; j < tilesPerPage; j++ )
			{
				for ( int i = 0; i < tilesPerPage; i++ )
				{
					if ( j != tilesPerPage - 1 )
					{
						tiles[ i ][ j ].SetNeighbor( Neighbor.SOUTH, tiles[ i ][ j + 1 ] );
						tiles[ i ][ j + 1 ].SetNeighbor( Neighbor.NORTH, tiles[ i ][ j ] );
					}

					if ( i != tilesPerPage - 1 )
					{
						tiles[ i ][ j ].SetNeighbor( Neighbor.EAST, tiles[ i + 1 ][ j ] );
						tiles[ i + 1 ][ j ].SetNeighbor( Neighbor.WEST, tiles[ i ][ j ] );
					}

				}
			}
		}

		public TerrainZoneRenderable GetTerrainZoneTile( Vector3 pt )
		{
			/* Since we don't know if the terrain is square, or has holes, we use a line trace
			to find the containing tile...
			*/

			TerrainZoneRenderable tile = tiles[ 0 ][ 0 ];

			while ( null != tile )
			{
				AxisAlignedBox b = tile.BoundingBox;

				if ( pt.x < b.Minimum.x )
					tile = tile.GetNeighbor( Neighbor.WEST );
				else if ( pt.x > b.Maximum.x )
					tile = tile.GetNeighbor( Neighbor.EAST );
				else if ( pt.z < b.Minimum.z )
					tile = tile.GetNeighbor( Neighbor.NORTH );
				else if ( pt.z > b.Maximum.z )
					tile = tile.GetNeighbor( Neighbor.SOUTH );
				else
					return tile;
			}

			return null;
		}



		/** Returns the TerrainZoneRenderable Tile with given index
		*/
		public TerrainZoneRenderable GetTerrainZoneTile( ushort x, ushort z )
		{
			/* Todo: error checking!
			*/
			//TerrainZoneRenderable * tile = tiles[ 0 ][ 0 ];
			return tiles[ x ][ z ];
		}

		public void SetRenderQueue( int qid )
		{
			for ( int j = 0; j < tilesPerPage; j++ )
			{
				for ( int i = 0; i < tilesPerPage; i++ )
				{
					if ( j != tilesPerPage - 1 )
					{
						tiles[ i ][ j ].RenderQueueGroup = (RenderQueueGroupID)qid;
					}
				}
			}
		}

	}
}