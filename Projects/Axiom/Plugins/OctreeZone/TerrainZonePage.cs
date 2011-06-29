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

public class TerrainZoneRow : List<TerrainZoneRenderable>
{
}
public class TerrainZone2D : List<TerrainZoneRow>
{
}

public class TerrainZonePage
{
    /// 2-dimensional vector of tiles, pre-allocated to the correct size
    protected internal TerrainZone2D tiles = new TerrainZone2D();
    /// The number of tiles across a page
    ushort tilesPerPage;
    /// The scene node to which all the tiles for this page are attached
    protected SceneNode pageSceneNode;

    public TerrainZonePage(ushort numTiles)
    {
        tilesPerPage = numTiles;
        // Set up an empty array of TerrainZoneRenderable pointers
        int i, j;
        for (i = 0; i < tilesPerPage; i++)
        {
            tiles.Add(new TerrainZoneRow());

            for (j = 0; j < tilesPerPage; j++)
            {
                tiles[i].Add(null);
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

        for (int j = 0; j < tilesPerPage; j++)
        {
            for (int i = 0; i < tilesPerPage; i++)
            {
                if (j != tilesPerPage - 1)
                {
                    tiles[i][j].SetNeighbor(Neighbor.SOUTH, tiles[i][j + 1]);
                    tiles[i][j + 1].SetNeighbor(Neighbor.NORTH, tiles[i][j]);
                }

                if (i != tilesPerPage - 1)
                {
                    tiles[i][j].SetNeighbor(Neighbor.EAST, tiles[i + 1][j]);
                    tiles[i + 1][j].SetNeighbor(Neighbor.WEST, tiles[i][j]);
                }

            }
        }
    }

    public TerrainZoneRenderable GetTerrainZoneTile(Vector3 pt)
    {
        /* Since we don't know if the terrain is square, or has holes, we use a line trace
        to find the containing tile...
        */

        TerrainZoneRenderable tile = tiles[0][0];

        while (null != tile)
        {
            AxisAlignedBox b = tile.BoundingBox;

            if (pt.x < b.Minimum.x)
                tile = tile.GetNeighbor(Neighbor.WEST);
            else if (pt.x > b.Maximum.x)
                tile = tile.GetNeighbor(Neighbor.EAST);
            else if (pt.z < b.Minimum.z)
                tile = tile.GetNeighbor(Neighbor.NORTH);
            else if (pt.z > b.Maximum.z)
                tile = tile.GetNeighbor(Neighbor.SOUTH);
            else
                return tile;
        }

        return null;
    }



    /** Returns the TerrainZoneRenderable Tile with given index
    */
    public TerrainZoneRenderable GetTerrainZoneTile(ushort x, ushort z)
    {
        /* Todo: error checking!
        */
        //TerrainZoneRenderable * tile = tiles[ 0 ][ 0 ];
        return tiles[x][z];
    }

    public void SetRenderQueue(RenderQueueGroupID qid)
    {
        for (int j = 0; j < tilesPerPage; j++)
        {
            for (int i = 0; i < tilesPerPage; i++)
            {
                if (j != tilesPerPage - 1)
                {
                    tiles[i][j].RenderQueueGroup = qid;
                }
            }
        }
    }

}