using System;
using System.Collections;
using Axiom.Core;
using Axiom.Graphics;
using Axiom.MathLib;
using Axiom.Media;

namespace Axiom.SceneManagers.Octree {
	/// <summary>
	/// Summary description for TerrainSceneManager.
	/// </summary>
	public class TerrainSceneManager : OctreeSceneManager {

        #region Fields

        protected TerrainRenderable[,] tiles;
        protected int tileSize;
        protected int numTiles;
        protected Vector3 scale;
        protected Material terrainMaterial;
        protected SceneNode terrainRoot;

        #endregion Fields
        
        public TerrainSceneManager() {
		}

        #region SceneManager members

        public override void LoadWorldGeometry(string fileName) {
            TerrainOptions options = new TerrainOptions();

            options.maxMipmap = 5;
            options.detailTile = 3;
            options.maxPixelError = 8;
            options.scalex = 1;
            options.scaley = 0.2f;
            options.scalez = 1;
            options.size = 17;

            string terrainFileName = "terrain.png";
            string detailTexture = "terrain_detail.jpg";
            string worldTexture = "tettain_texture.jpg";

            scale = new Vector3(options.scalex, options.scaley, options.scalez);
            tileSize = options.size;

            // load the heightmap
            Image image = Image.FromFile(terrainFileName);

            // TODO: Check terrain size for 2^n + 1

            // get the data from the heightmap
            options.data = image.Data;

            options.worldSize = image.Width;

            float maxx = options.scalex * options.worldSize;
            float maxy = 255 * options.scaley;
            float maxz = options.scalez * options.worldSize;

            Resize(new AxisAlignedBox(Vector3.Zero, new Vector3(maxx, maxy, maxz)));

            terrainMaterial = CreateMaterial("Terrain");

            if(worldTexture != "") {
                terrainMaterial.GetTechnique(0).GetPass(0).CreateTextureUnitState(worldTexture, 0);
            }

            if(detailTexture != "") {
                terrainMaterial.GetTechnique(0).GetPass(0).CreateTextureUnitState(detailTexture, 1);
            }

            terrainMaterial.Lighting = options.isLit;
            terrainMaterial.Load();

            terrainRoot = (SceneNode)RootSceneNode.CreateChild("TerrainRoot");

            numTiles = (options.worldSize - 1) / (options.size - 1);

            tiles = new TerrainRenderable[numTiles, numTiles]; 


        }

        /// <summary>
        ///     Updates all the TerrainRenderables LOD.
        /// </summary>
        /// <param name="camera"></param>
        protected override void UpdateSceneGraph(Camera camera) {
            base.UpdateSceneGraph (camera);
        }

        /// <summary>
        ///     Aligns TerrainRenderable neighbors, and renders them.
        /// </summary>
        protected override void RenderVisibleObjects() {
            base.RenderVisibleObjects ();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="camera"></param>
        public override void FindVisibleObjects(Camera camera) {
            base.FindVisibleObjects (camera);
        }

        /// <summary>
        ///     Returns the TerrainRenderable that contains the given pt.
        //      If no tile exists at the point, it returns 0
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public TerrainRenderable GetTerrainTile(Vector3 point) {
            return null;
        }

        #endregion SceneManager members
	}
}
