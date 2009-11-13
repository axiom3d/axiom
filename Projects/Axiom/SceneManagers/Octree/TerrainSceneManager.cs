#region LGPL License
/*
Axiom Graphics Engine Library
Copyright (C) 2003-2006 Axiom Project Team

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
//     <license see="http://axiomengine.sf.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;
//using System.Data;
using System.Collections;
using System.Threading;

using Axiom.Core;
using Axiom.Graphics;
using Axiom.Math;
using Axiom.Media;
using System.Data;


#endregion Namespace Declarations

namespace Axiom.SceneManagers.Octree
{

    /// <summary>
    /// Summary description for TerrainSceneManager.
    /// </summary>
    public class TerrainSceneManager : OctreeSceneManager
    {
        #region Fields

        protected TerrainRenderable[ , ] tiles;
        protected int tileSize;
        protected int numTiles;
        protected Vector3 scale;
        protected Material terrainMaterial;
        protected SceneNode terrainRoot;
        protected TerrainOptions options; //needed for get HeightAt

        #endregion Fields

		public TerrainSceneManager( string name )
			: base( name )
		{
		}

        #region SceneManager members

		public override string TypeName
		{
			get { return "TerrainSceneManager"; }
		}

        public override void ClearScene()
        {
            base.ClearScene();

            tiles = null;
            terrainMaterial = null;
            terrainRoot = null;
        }

        public override void LoadWorldGeometry( string fileName )
        {
            options = new TerrainOptions();

            DataSet optionData = new DataSet();
            optionData.ReadXml( ResourceGroupManager.Instance.OpenResource( fileName ) );
            DataTable table = optionData.Tables[0];
            DataRow row = table.Rows[0];

            string terrainFileName = "";
            string detailTexture = "";
            string worldTexture = "";
            string materialName = "";

            if (table.Columns["Terrain"] != null)
            {
                terrainFileName = (string)row["Terrain"];
            }

            if (table.Columns["DetailTexture"] != null)
            {
                detailTexture = (string)row["DetailTexture"];
            }

            if (table.Columns["MaterialName"] != null)
            {
                materialName = (string)row["MaterialName"];
            }

            if (table.Columns["WorldTexture"] != null)
            {
                worldTexture = (string)row["WorldTexture"];
            }

            if (table.Columns["MaxMipMapLevel"] != null)
            {
                options.maxMipmap = Convert.ToInt32(row["MaxMipMapLevel"]);
            }

            if (table.Columns["DetailTile"] != null)
            {
                options.detailTile = Convert.ToInt32(row["DetailTile"]);
            }

            if (table.Columns["MaxPixelError"] != null)
            {
                options.maxPixelError = Convert.ToInt32(row["MaxPixelError"]);
            }

            if (table.Columns["TileSize"] != null)
            {
                options.size = Convert.ToInt32(row["TileSize"]);
            }

            if (table.Columns["ScaleX"] != null)
            {
                options.scalex = StringConverter.ParseFloat((string)row["ScaleX"]);
            }

            if (table.Columns["ScaleY"] != null)
            {
                options.scaley = StringConverter.ParseFloat((string)row["ScaleY"]);
            }

            if (table.Columns["ScaleZ"] != null)
            {
                options.scalez = StringConverter.ParseFloat((string)row["ScaleZ"]);
            }

            if (table.Columns["VertexNormals"] != null)
            {
                options.isLit = ((string)row["VertexNormals"]) == "yes" ? true : false;
            }

            scale = new Vector3(options.scalex, options.scaley, options.scalez);
            tileSize = options.size;
            // load the heightmap
            {
                Image image = Image.FromFile( terrainFileName );
                worldSize = options.worldSize = image.Width;
                Real[] dest = new Real[(int)worldSize * (int)worldSize];
                byte[] src = image.Data;
                Real invScale;

                if ( image.Format != PixelFormat.L8 && image.Format != PixelFormat.L16 )
                    throw new AxiomException( "Heightmap is not a grey scale image!" );

                bool is16bit = ( image.Format == PixelFormat.L16 );

                // Determine mapping from fixed to floating
                ulong rowSize;
                if ( is16bit )
                {
                    invScale = 1.0f / 65535.0f;
                    rowSize = (ulong)worldSize * 2;
                }
                else
                {
                    invScale = 1.0f;// / 255.0f;
                    rowSize = (ulong)worldSize;
                }
                // Read the data
                int srcIndex = 0;
                int dstIndex = 0;
                for ( ulong j = 0; j < (ulong)worldSize; ++j )
                {
                    for (ulong i = 0; i < (ulong)worldSize; ++i)
                    {
                        if ( is16bit )
                        {
#if OGRE_ENDIAN_BIG
                            ushort val = (ushort)(src[srcIndex++] << 8);
                            val += src[srcIndex++];
#else
                            ushort val = src[srcIndex++];
                            val += (ushort)(src[srcIndex++] << 8);
#endif
                            dest[dstIndex++] = new Real(val) * invScale;
                        }
                        else
                        {
                            dest[ dstIndex++ ] = new Real( src[ srcIndex++ ] );// *invScale;
                        }
                    }
                }

                // get the data from the heightmap
                options.data = dest;
            }

            float maxx = options.scalex * options.worldSize;
            float maxy = 255 * options.scaley;
            float maxz = options.scalez * options.worldSize;

            Resize(new AxisAlignedBox(Vector3.Zero, new Vector3(maxx, maxy, maxz)));

            terrainMaterial = (Material)(MaterialManager.Instance.CreateOrRetrieve(!String.IsNullOrEmpty(materialName) ? materialName : "Terrain", ResourceGroupManager.Instance.WorldResourceGroupName).First);

            if (worldTexture != "")
            {
                terrainMaterial.GetTechnique(0).GetPass(0).CreateTextureUnitState(worldTexture, 0);
            }

            if (detailTexture != "")
            {
                terrainMaterial.GetTechnique(0).GetPass(0).CreateTextureUnitState(detailTexture, 1);
            }

            terrainMaterial.Lighting = options.isLit;
            terrainMaterial.Load();

            terrainRoot = (SceneNode)RootSceneNode.CreateChild("TerrainRoot");

            numTiles = (options.worldSize - 1) / (options.size - 1);

            tiles = new TerrainRenderable[numTiles, numTiles];

            int p = 0, q = 0;

            for (int j = 0; j < options.worldSize - 1; j += (options.size - 1))
            {
                p = 0;

                for (int i = 0; i < options.worldSize - 1; i += (options.size - 1))
                {
                    options.startx = i;
                    options.startz = j;

                    string name = string.Format("Tile[{0},{1}]", p, q);

                    SceneNode node = (SceneNode)terrainRoot.CreateChild(name);
                    TerrainRenderable tile = new TerrainRenderable();
                    tile.Name = name;

                    tile.RenderQueueGroup = this.WorldGeometryRenderQueueId;

                    tile.SetMaterial(terrainMaterial);
                    tile.Init(options);

                    tiles[p, q] = tile;

                    node.AttachObject(tile);

                    p++;
                }

                q++;
            }

            int size1 = tiles.GetLength(0);
            int size2 = tiles.GetLength(1);

            for (int j = 0; j < size1; j++)
            {
                for (int i = 0; i < size2; i++)
                {
                    if (j != size1 - 1)
                    {
                        tiles[ i, j ].SetNeighbor( Neighbor.South, tiles[ i, j + 1 ] );
                        tiles[ i, j + 1 ].SetNeighbor( Neighbor.North, tiles[ i, j ] );
                    }

                    if (i != size2 - 1)
                    {
                        tiles[ i, j ].SetNeighbor( Neighbor.East, tiles[ i + 1, j ] );
                        tiles[ i + 1, j ].SetNeighbor( Neighbor.West, tiles[ i, j ] );
                    }
                }
            }

            if (options.isLit)
            {
                for (int j = 0; j < size1; j++)
                {
                    for (int i = 0; i < size2; i++)
                    {
                        tiles[i, j].CalculateNormals();
                    }
                }
            }
        }

        /// <summary>
        ///     Updates all the TerrainRenderables LOD.
        /// </summary>
        /// <param name="camera"></param>
        protected override void UpdateSceneGraph( Camera camera )
        {
            base.UpdateSceneGraph( camera );
        }

        /// <summary>
        ///     Aligns TerrainRenderable neighbors, and renders them.
        /// </summary>
        protected override void RenderVisibleObjects()
        {
            if ( tiles == null )
                return;
            for ( int i = 0; i < tiles.GetLength( 0 ); i++ )
            {
                for ( int j = 0; j < tiles.GetLength( 1 ); j++ )
                {
                    tiles[ i, j ].AlignNeighbors();
                }
            }

            base.RenderVisibleObjects();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="camera"></param>
        public override void FindVisibleObjects( Camera camera, bool onlyShadowCasters )
        {
            base.FindVisibleObjects( camera, onlyShadowCasters );
        }
        /// <summary>
        ///    Creates a query to return objects found along the ray.
        /// </summary>
        /// <returns>A specialized implementation of RaySceneQuery for this scene manager.</returns>
        public virtual RaySceneQuery CreateRayQuery()
        {
            return this.CreateRayQuery(new Ray(), 0xffffffff);
        }

        /// <summary>
        ///    Creates a query to return objects found along the ray.
        /// </summary>
        /// <param name="ray">Ray to use for the intersection query.</param>
        /// <returns>A specialized implementation of RaySceneQuery for this scene manager.</returns>
        public virtual RaySceneQuery CreateRayQuery(Ray ray)
        {
            return this.CreateRayQuery(ray, 0xffffffff);
        }

        /// <summary>
        ///    Creates a query to return objects found along the ray. 
        /// </summary>
        /// <param name="ray">Ray to use for the intersection query.</param>
        /// <returns>A specialized implementation of RaySceneQuery for this scene manager.</returns>
        public override RaySceneQuery CreateRayQuery(Ray ray, uint mask)
        {
            TerrainRaySceneQuery query = new TerrainRaySceneQuery(this);
            query.Ray = ray;
            query.QueryMask = mask;
            return query;
        }

        public Vector3 IntersectSegment(Vector3 start, Vector3 end)
        {
            TerrainRenderable t = GetTerrainTile(start);
            if (t == null)
            {
                return new Vector3(-1, -1, -1);
            }
            return t.IntersectSegment(start, end);
        }

        /// <summary>
        /// Get the height of a a point on the terrain under/over a givin 3d point. This is
        /// very useful terrain collision testing, since you can simply select 
        /// a few locations you would like to test and see if the y value matches the one returned
        /// by this function.
        /// 
        /// Just to clarify this does not return the altitude of a generic xyz point, 
        /// rather it returns the y value (height) of a point with the same x and z values 
        /// as thoes passed in, that is on the surface of the terrain. 
        /// To get the Altitude you would do something like 
        /// float altitude = thePoint.y - GetHeightAt(thePoint, 0);
        /// 
        /// This has code merged into it from GetTerrainTile() b/c it gives us about 60 fps 
        /// when testing 1000+ points, to inline it here rather than going through the extra function calls
        /// </summary>
        /// <param name="point">The point you would like to know the y value of the terrain at</param>
        /// <param name="defaultheight">value to return if the point is not over/under the terrain</param>
        /// <returns></returns>
        public float GetHeightAt( Vector3 point, float defaultheight )
        {
            if ( options == null || tiles == null )
                return defaultheight;
            float worldsize = options.worldSize;
            float scalex = options.scalex;
            float scalez = options.scalez;

            int xdim = tiles.GetLength( 0 );
            int zdim = tiles.GetLength( 1 );

            float maxx = scalex * worldsize;
            int xCoordIndex = (int)( ( point.x * ( xdim / maxx ) ) );

            float maxz = scalez * worldsize;
            int zCoordIndex = (int)( ( point.z * zdim / maxx ) );

            if ( xCoordIndex >= xdim || zCoordIndex >= zdim || xCoordIndex < 0 || zCoordIndex < 0 )
                return defaultheight; //point is not over a tile			
            return tiles[ xCoordIndex, zCoordIndex ].GetHeightAt( point.x, point.z );
        }

        /// <summary>
        ///     Returns the TerrainRenderable that contains the given pt.
        //      If no tile exists at the point, it returns 0
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public TerrainRenderable GetTerrainTile( Vector3 point )
        {

            if ( options == null || tiles == null )
                return null;
            float worldsize = options.worldSize;
            float scalex = options.scalex;
            float scalez = options.scalez;

            int xdim = tiles.GetLength( 0 );
            int zdim = tiles.GetLength( 1 );

            float maxx = scalex * worldsize;
            int xCoordIndex = (int)( ( point.x * ( xdim / maxx ) ) );

            float maxz = scalez * worldsize;
            int zCoordIndex = (int)( ( point.z * zdim / maxx ) );

            if ( xCoordIndex >= xdim || zCoordIndex >= zdim || xCoordIndex < 0 || zCoordIndex < 0 )
                return null; //point is not over a tile			
            else
                return tiles[ xCoordIndex, zCoordIndex ];
        }

        #endregion SceneManager members
    }

	/// <summary>
	///		Factory for <see cref="TerrainSceneManager"/>.
	/// </summary>
	class TerrainSceneManagerFactory : SceneManagerFactory
	{
		public TerrainSceneManagerFactory()
		{

		}

		#region Methods
		
		protected override void InitMetaData()
		{
			metaData.sceneTypeMask = SceneType.ExteriorClose;
			metaData.typeName = "TerrainSceneManager";
			metaData.worldGeometrySupported = true;
			metaData.description = "Scene manager which generally organises the scene on " +
			"the basis of an octree, but also supports terrain world geometry. ";

		}

		public override SceneManager CreateInstance( string name )
		{
			return new TerrainSceneManager( name );
		}

		public override void DestroyInstance( SceneManager instance )
		{
			instance.ClearScene();
		}
		
		#endregion
	}    
    
}
