using Axiom.Core;
using Axiom.Graphics;
using Axiom.Math;

namespace OctreeZone
{
    public class TerrainZoneOptions
    {
        public TerrainZoneOptions()
        {
            pageSize = 0;
            tileSize = 0;
            tilesPerPage = 0;
            maxGeoMipMapLevel = 0;
            scale = Vector3.UnitScale;
            maxPixelError = 4;
            detailTile = 1;
            lit = false;
            coloured = false;
            lodMorph = false;
            lodMorphStart = 0.5;
            useTriStrips = false;
            primaryCamera = null;
            terrainMaterial = null;
        }
        /// The size of one edge of a terrain page, in vertices
        public int pageSize;
        /// The size of one edge of a terrain tile, in vertices
        public int tileSize; 
        /// Precalculated number of tiles per page
        public int tilesPerPage;
        /// The primary camera, used for error metric calculation and page choice
        public Camera primaryCamera;
        /// The maximum terrain geo-mipmap level
        public int maxGeoMipMapLevel;
        /// The scale factor to apply to the terrain (each vertex is 1 unscaled unit
        /// away from the next, and height is from 0 to 1)
        public Vector3 scale;
        /// The maximum pixel error allowed
        public int maxPixelError;
        /// Whether we should use triangle strips
        public bool useTriStrips;
        /// The number of times to repeat a detail texture over a tile
        public int detailTile;
        /// Whether LOD morphing is enabled
        public bool lodMorph;
        /// At what point (parametric) should LOD morphing start
        public Real lodMorphStart;
        /// Whether dynamic lighting is enabled
        public bool lit;
        /// Whether vertex colours are enabled
        public bool coloured;
        /// Pointer to the material to use to render the terrain
        public Material terrainMaterial;

    }
    
}