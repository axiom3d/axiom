using System;
using System.Collections.Generic;
using System.Text;
using Axiom.Core;
using Axiom.Graphics;
using Axiom.Math;
using System.IO;
using Axiom.SceneManagers.PortalConnected;

namespace OctreeZone
{
    public class TerrainZonePageConstructedEventArgs : EventArgs
    {
        public int PageX;
        public int PageZ;

        public Real[] HeightData;

        public TerrainZonePageConstructedEventArgs(int pagex, int pagez, Real[] heightData)
        {
            PageX = pagex;
            PageZ = pagez;
            HeightData = heightData;
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


        public virtual void Initialize(TerrainZone tz, 
            int tileSize, int pageSize, bool asyncLoading, 
            TerrainZonePageSourceOptionList optionList)
        {
            mTerrainZone = tz;
            mTileSize = tileSize;
            mPageSize = pageSize;
            mAsyncLoading = asyncLoading;
        }


        public virtual TerrainZonePage BuildPage(Real[] heightData, Material pMaterial)
        {
            string name;

            // Create a TerrainZone Page
            TerrainZonePage page = new TerrainZonePage((ushort)((mPageSize - 1) / (mTileSize - 1)));
            // Create a node for all tiles to be attached to
            // Note we sequentially name since page can be attached at different points
            // so page x/z is not appropriate
            int pageIndex = mTerrainZone.PageCount;
            name = mTerrainZone.Name + "_page[";
            name += pageIndex + "]_Node";
            if (mTerrainZone.mPCZSM.HasSceneNode(name))
            {
                page.PageSceneNode = mTerrainZone.mPCZSM.GetSceneNode(name);
                // set the home zone of the scene node to the terrainzone
                ((PCZSceneNode)(page.PageSceneNode)).AnchorToHomeZone(mTerrainZone);
                // EXPERIMENTAL - prevent terrain zone pages from visiting other zones
                ((PCZSceneNode)(page.PageSceneNode)).AllowToVisit = false;
            }
            else
            {
                page.PageSceneNode = mTerrainZone.TerrainRootNode.CreateChildSceneNode(name);
                // set the home zone of the scene node to the terrainzone
                ((PCZSceneNode)(page.PageSceneNode)).AnchorToHomeZone(mTerrainZone);
                // EXPERIMENTAL - prevent terrain zone pages from visiting other zones
                ((PCZSceneNode)(page.PageSceneNode)).AllowToVisit = false;

            }

            int q = 0;
            for (int j = 0; j < mPageSize - 1; j += (mTileSize - 1))
            {
                int p = 0;

                for (int i = 0; i < mPageSize - 1; i += (mTileSize - 1))
                {
                    // Create scene node for the tile and the TerrainZoneRenderable
                    name = mTerrainZone.Name + "_tile[" + pageIndex + "][" + p + "," + q + "]_Node";

                    SceneNode c;
                    if (mTerrainZone.mPCZSM.HasSceneNode(name))
                    {
                        c = mTerrainZone.mPCZSM.GetSceneNode(name);
                        if (c.Parent != page.PageSceneNode)
                            page.PageSceneNode.AddChild(c);
                        // set the home zone of the scene node to the terrainzone
                        ((PCZSceneNode)c).AnchorToHomeZone(mTerrainZone);
                        // EXPERIMENTAL - prevent terrain zone pages from visiting other zones
                        ((PCZSceneNode)c).AllowToVisit = false;
                    }
                    else
                    {
                        c = page.PageSceneNode.CreateChildSceneNode(name);
                        // set the home zone of the scene node to the terrainzone
                        ((PCZSceneNode)c).AnchorToHomeZone(mTerrainZone);
                        // EXPERIMENTAL - prevent terrain zone pages from visiting other zones
                        ((PCZSceneNode)c).AllowToVisit = false;
                    }

                    TerrainZoneRenderable tile = new TerrainZoneRenderable(name, mTerrainZone);
                    // set queue
                    tile.RenderQueueGroup = mTerrainZone.mPCZSM.WorldGeometryRenderQueueId;
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

            if (mTerrainZone.Options.lit)
            {
                q = 0;
                for (int j = 0; j < mPageSize - 1; j += (mTileSize - 1))
                {
                    int p = 0;

                    for (int i = 0; i < mPageSize - 1; i += (mTileSize - 1))
                    {
                        page.tiles[p][q].CalculateNormals();
                        p++;
                    }
                    q++;
                }
            }

            return page;
        }

        public virtual void Shutdown() { }
        public virtual void RequestPage(ushort x, ushort z) { }

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
