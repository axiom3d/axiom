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
//     <id value="$Id$"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections.Generic;
using System.Text;
using Axiom.Core;
using Axiom.Graphics;
using Axiom.Math;
using System.IO;
using Axiom.SceneManagers.PortalConnected;

#endregion Namespace Declarations

namespace OctreeZone
{
    public class TerrainZonePageConstructedEventArgs : EventArgs
    {
        public int PageX;
        public int PageZ;

        public Real[] HeightData;

        public TerrainZonePageConstructedEventArgs(int pagex, int pagez, Real[] heightData)
        {
            this.PageX = pagex;
            this.PageZ = pagez;
            this.HeightData = heightData;
        }
    }


    public class TerrainZonePageSource
    {
        public delegate void TerrainZonePageConstructedEventHandler(object sender, TerrainZonePageConstructedEventArgs te);

        public event TerrainZonePageConstructedEventHandler PageConstructed;

        /// Link back to parent manager
        protected TerrainZone mTerrainZone;

        /// Has asynchronous loading been requested?
        protected bool mAsyncLoading;

        /// The expected size of the page in number of vertices
        protected int mPageSize;

        /// The expected size of a tile in number of vertices
        protected int mTileSize;


        public virtual void Initialize(TerrainZone tz, int tileSize, int pageSize, bool asyncLoading,
                                        TerrainZonePageSourceOptionList optionList)
        {
            this.mTerrainZone = tz;
            this.mTileSize = tileSize;
            this.mPageSize = pageSize;
            this.mAsyncLoading = asyncLoading;
        }


        public virtual TerrainZonePage BuildPage(Real[] heightData, Material pMaterial)
        {
            string name;

            // Create a TerrainZone Page
            var page = new TerrainZonePage((ushort)((this.mPageSize - 1) / (this.mTileSize - 1)));
            // Create a node for all tiles to be attached to
            // Note we sequentially name since page can be attached at different points
            // so page x/z is not appropriate
            int pageIndex = this.mTerrainZone.PageCount;
            name = this.mTerrainZone.Name + "_page[";
            name += pageIndex + "]_Node";
            if (this.mTerrainZone.mPCZSM.HasSceneNode(name))
            {
                page.PageSceneNode = this.mTerrainZone.mPCZSM.GetSceneNode(name);
                // set the home zone of the scene node to the terrainzone
                ((PCZSceneNode)(page.PageSceneNode)).AnchorToHomeZone(this.mTerrainZone);
                // EXPERIMENTAL - prevent terrain zone pages from visiting other zones
                ((PCZSceneNode)(page.PageSceneNode)).AllowToVisit = false;
            }
            else
            {
                page.PageSceneNode = this.mTerrainZone.TerrainRootNode.CreateChildSceneNode(name);
                // set the home zone of the scene node to the terrainzone
                ((PCZSceneNode)(page.PageSceneNode)).AnchorToHomeZone(this.mTerrainZone);
                // EXPERIMENTAL - prevent terrain zone pages from visiting other zones
                ((PCZSceneNode)(page.PageSceneNode)).AllowToVisit = false;
            }

            int q = 0;
            for (int j = 0; j < this.mPageSize - 1; j += (this.mTileSize - 1))
            {
                int p = 0;

                for (int i = 0; i < this.mPageSize - 1; i += (this.mTileSize - 1))
                {
                    // Create scene node for the tile and the TerrainZoneRenderable
                    name = this.mTerrainZone.Name + "_tile[" + pageIndex + "][" + p + "," + q + "]_Node";

                    SceneNode c;
                    if (this.mTerrainZone.mPCZSM.HasSceneNode(name))
                    {
                        c = this.mTerrainZone.mPCZSM.GetSceneNode(name);
                        if (c.Parent != page.PageSceneNode)
                        {
                            page.PageSceneNode.AddChild(c);
                        }
                        // set the home zone of the scene node to the terrainzone
                        ((PCZSceneNode)c).AnchorToHomeZone(this.mTerrainZone);
                        // EXPERIMENTAL - prevent terrain zone pages from visiting other zones
                        ((PCZSceneNode)c).AllowToVisit = false;
                    }
                    else
                    {
                        c = page.PageSceneNode.CreateChildSceneNode(name);
                        // set the home zone of the scene node to the terrainzone
                        ((PCZSceneNode)c).AnchorToHomeZone(this.mTerrainZone);
                        // EXPERIMENTAL - prevent terrain zone pages from visiting other zones
                        ((PCZSceneNode)c).AllowToVisit = false;
                    }

                    var tile = new TerrainZoneRenderable(name, this.mTerrainZone);
                    // set queue
                    tile.RenderQueueGroup = this.mTerrainZone.mPCZSM.WorldGeometryRenderQueueId;
                    // Initialise the tile
                    tile.Material = pMaterial;
                    tile.Initialize(i, j, heightData);
                    // Attach it to the page
                    page.tiles[p][q] = tile;
                    // Attach it to the node
                    c.AttachObject(tile);
                    p++;
                }

                q++;
            }

            pageIndex++;

            // calculate neighbours for page
            page.LinkNeighbours();

            if (this.mTerrainZone.Options.lit)
            {
                q = 0;
                for (int j = 0; j < this.mPageSize - 1; j += (this.mTileSize - 1))
                {
                    int p = 0;

                    for (int i = 0; i < this.mPageSize - 1; i += (this.mTileSize - 1))
                    {
                        page.tiles[p][q].CalculateNormals();
                        p++;
                    }
                    q++;
                }
            }

            return page;
        }

        public virtual void Shutdown()
        {
        }

        public virtual void RequestPage(ushort x, ushort z)
        {
        }

        protected void OnPageConstructed(int x, int z, Real[] data)
        {
            object o = PageConstructed;
            if (null != o)
            {
                PageConstructed(this, new TerrainZonePageConstructedEventArgs(x, z, data));
            }
        }
    }
}