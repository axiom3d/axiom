using System;
using Axiom.Core;
using Axiom.Graphics;

namespace Axiom.SceneManagers.Octree {
	/// <summary>
	/// Summary description for TerrainRenderable.
	/// </summary>
    public class TerrainRenderable { // : SceneObject, IRenderable {
        /// <summary>
        ///     Default constructor.
        /// </summary>
        public TerrainRenderable() {
        }

        #region SceneObject Members

        

        #endregion SceneObject Members

        #region IRenderable Members

        public float GetSquaredViewDepth(Camera camera) {
            // TODO:  Add TerrainRenderable.GetSquaredViewDepth implementation
            return 0;
        }

        public bool UseIdentityView {
            get {
                // TODO:  Add TerrainRenderable.UseIdentityView getter implementation
                return false;
            }
        }

        public bool UseIdentityProjection {
            get {
                // TODO:  Add TerrainRenderable.UseIdentityProjection getter implementation
                return false;
            }
        }

        public Axiom.MathLib.Vector3 WorldPosition {
            get {
                // TODO:  Add TerrainRenderable.WorldPosition getter implementation
                return new Axiom.MathLib.Vector3 ();
            }
        }

        public void GetRenderOperation(RenderOperation op) {
            // TODO:  Add TerrainRenderable.GetRenderOperation implementation
        }

        public void GetWorldTransforms(Axiom.MathLib.Matrix4[] matrices) {
            // TODO:  Add TerrainRenderable.GetWorldTransforms implementation
        }

        public Axiom.MathLib.Quaternion WorldOrientation {
            get {
                // TODO:  Add TerrainRenderable.WorldOrientation getter implementation
                return new Axiom.MathLib.Quaternion ();
            }
        }

        public Axiom.Graphics.SceneDetailLevel RenderDetail {
            get {
                // TODO:  Add TerrainRenderable.RenderDetail getter implementation
                return new Axiom.Graphics.SceneDetailLevel ();
            }
        }

        public Material Material {
            get {
                // TODO:  Add TerrainRenderable.Material getter implementation
                return null;
            }
        }

        public Axiom.Collections.LightList Lights {
            get {
                // TODO:  Add TerrainRenderable.Lights getter implementation
                return null;
            }
        }

        public Technique Technique {
            get {
                // TODO:  Add TerrainRenderable.Technique getter implementation
                return null;
            }
        }

        public bool NormalizeNormals {
            get {
                // TODO:  Add TerrainRenderable.NormalizeNormals getter implementation
                return false;
            }
        }

        public ushort NumWorldTransforms {
            get {
                // TODO:  Add TerrainRenderable.NumWorldTransforms getter implementation
                return 0;
            }
        }

        #endregion
    }

    class TerrainOptions {
        public TerrainOptions() {
            size = 0;
            worldSize = 0;
            startx = 0;
            startz = 0;
            maxMipmap = 0;
            scalex = 1;
            scaley = 1;
            scalez = 1;
            maxPixelError = 4;
            vertRes = 768;
            topCoord = 1;
            nearPlane = 1;
            detailTile = 1;
            isLit = false;
            isColored = false;
        }


        public int GetWorldHeight(int x, int z) {
            return data[((z * worldSize) + x)];
        }

        public byte[] data;     //pointer to the world 2D data.
        public int size;         //size of this square block
        public int worldSize;   //size of the world.
        public int startx;
        public int startz; //starting coords of this block.
        public int maxMipmap;  //max mip_map level
        public float scalex, scaley, scalez;

        public int maxPixelError;
        public int nearPlane;
        public int vertRes;
        public int detailTile;
        public float topCoord;

        public bool isLit;
        public bool isColored;

    }

}
