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
//     <id value="$Id:$"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System.Collections.Generic;
using Axiom.Core;
using Axiom.Math;

#endregion Namespace Declarations

/// <summary>
/// List Of TerrainZoneRenderable
/// </summary>
public class TerrainZoneRow : List<TerrainZoneRenderable>
{
}
/// <summary>
/// List of TerrainZoneRow
/// </summary>
public class TerrainZone2D : List<TerrainZoneRow>
{
}

/// <summary>
/// Terrain Zone Page
/// </summary>
public class TerrainZonePage
{
    private TerrainZone2D _tiles = new TerrainZone2D();
    /// <summary>
    /// 2-dimensional vector of tiles, pre-allocated to the correct size
    /// </summary>
    public TerrainZone2D Tiles
    {
        get { return _tiles; }
        set { _tiles = value; }
    }

    private ushort _tilesPerPage = 1;

    /// <summary>
    /// The number of tiles across a page
    /// </summary>
    public ushort TilesPerPage
    {
        get { return _tilesPerPage; }
        set { _tilesPerPage = value; }
    }

    protected SceneNode _pageSceneNode = null;
    /// <summary>
    /// The scene node to which all the tiles for this page are attached
    /// </summary>
    public SceneNode PageSceneNode
    {
        get { return _pageSceneNode; }
        set { _pageSceneNode = value; }
    }

    /// <summary>
    /// Terrain Zone Page Constructor
    /// </summary>
    /// <param name="numTiles">ushort</param>
    public TerrainZonePage(ushort numTiles)
    {
        TilesPerPage = numTiles;
        // Set up an empty array of TerrainZoneRenderable pointers
        int i, j;
        for (i = 0; i < TilesPerPage; i++)
        {
            _tiles.Add(new TerrainZoneRow());

            for (j = 0; j < TilesPerPage; j++)
            {
                _tiles[i].Add(null);
            }
        }
        _pageSceneNode = null;
    }

    /// <summary>
    /// Link Neighbours
    /// </summary>
    public void LinkNeighbours()
    {
        //setup the neighbor links.

        for (int j = 0; j < TilesPerPage; j++)
        {
            for (int i = 0; i < TilesPerPage; i++)
            {
                if (j != TilesPerPage - 1)
                {
                    _tiles[i][j].SetNeighbor(Neighbor.South, _tiles[i][j + 1]);
                    _tiles[i][j + 1].SetNeighbor(Neighbor.North, _tiles[i][j]);
                }

                if (i != TilesPerPage - 1)
                {
                    _tiles[i][j].SetNeighbor(Neighbor.East, _tiles[i + 1][j]);
                    _tiles[i + 1][j].SetNeighbor(Neighbor.West, _tiles[i][j]);
                }

            }
        }
    }

    /// <summary>
    /// GetTerrainZoneTile
    /// </summary>
    /// <param name="pt">Vector3</param>
    /// <returns>TerrainZoneRenderable</returns>
    public TerrainZoneRenderable GetTerrainZoneTile(Vector3 pt)
    {
        /* Since we don't know if the terrain is square, or has holes, we use a line trace
        to find the containing tile...
        */

        TerrainZoneRenderable tile = _tiles[0][0];

        while (null != tile)
        {
            AxisAlignedBox b = tile.BoundingBox;

            if (pt.x < b.Minimum.x)
                tile = tile.GetNeighbor(Neighbor.West);
            else if (pt.x > b.Maximum.x)
                tile = tile.GetNeighbor(Neighbor.East);
            else if (pt.z < b.Minimum.z)
                tile = tile.GetNeighbor(Neighbor.North);
            else if (pt.z > b.Maximum.z)
                tile = tile.GetNeighbor(Neighbor.South);
            else
                return tile;
        }

        return null;
    }

    /// <summary>
    /// Returns the TerrainZoneRenderable Tile with given index
    /// </summary>
    /// <param name="x">ushort</param>
    /// <param name="z">ushort</param>
    /// <returns>TerrainZoneRenderable</returns>
    public TerrainZoneRenderable GetTerrainZoneTile(ushort x, ushort z)
    {
        /* Todo: error checking!
        */
        //TerrainZoneRenderable * tile = tiles[ 0 ][ 0 ];
        return _tiles[x][z];
    }

    /// <summary>
    /// SetRenderQueue
    /// </summary>
    /// <param name="qid">RenderQueueGroupID</param>
    public void SetRenderQueue(RenderQueueGroupID qid)
    {
        for (int j = 0; j < TilesPerPage; j++)
        {
            for (int i = 0; i < TilesPerPage; i++)
            {
                if (j != TilesPerPage - 1)
                {
                    _tiles[i][j].RenderQueueGroup = qid;
                }
            }
        }
    }

}