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

#endregion LGPL License

#region SVN Version Information

// <file>
//     <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;
	//using System.Data;
using System.Xml.Serialization;
using Axiom.Core;
using Axiom.Graphics;
using Axiom.Math;
using Axiom.Media;

#if !(XBOX || XBOX360 || SILVERLIGHT)

#endif

#endregion Namespace Declarations

namespace Axiom.SceneManagers.Octree
{
	/// <summary>
	/// Summary description for TerrainSceneManager.
	/// </summary>
	public class TerrainSceneManager : OctreeSceneManager
	{
		#region Fields

		protected TerrainRenderable[,] tiles;
		protected int tileSize;
		protected int numTiles;
		protected Vector3 scale;
		protected Material terrainMaterial;
		protected SceneNode terrainRoot;

		protected TerrainOptions options; //needed for get HeightAt

		public TerrainOptions TerrainOptions
		{
			get
			{
				return this.options;
			}
		}

		#endregion Fields

		public TerrainSceneManager( string name )
			: base( name )
		{
		}

		#region SceneManager members

		public override string TypeName
		{
			get
			{
				return "TerrainSceneManager";
			}
		}

		public override void ClearScene()
		{
			base.ClearScene();

			this.tiles = null;
			this.terrainMaterial = null;
			this.terrainRoot = null;
		}

		public override void LoadWorldGeometry( string fileName )
		{
			var serializer = new XmlSerializer( typeof ( TerrainOptions ) );
			this.options = (TerrainOptions)serializer.Deserialize( ResourceGroupManager.Instance.OpenResource( fileName ) );

			this.scale = new Vector3( this.options.scalex, this.options.scaley, this.options.scalez );
			this.tileSize = this.options.size;
			// load the heightmap
			{
				Image image = Image.FromStream( ResourceGroupManager.Instance.OpenResource( this.options.Terrain ),
				                                this.options.Terrain.Split( '.' )[ 1 ] );
				worldSize = this.options.worldSize = image.Width;
				var dest = new Real[(int)worldSize*(int)worldSize];
				byte[] src = image.Data;
				Real invScale;

				//if ( image.Format != PixelFormat.L8 && image.Format != PixelFormat.L16 )
				//    throw new AxiomException( "Heightmap is not a grey scale image!" );

				bool is16bit = ( image.Format == PixelFormat.L16 );

				// Determine mapping from fixed to floating
				ulong rowSize;
				if ( is16bit )
				{
					invScale = 1.0f/65535.0f;
					rowSize = (ulong)worldSize*2;
				}
				else
				{
					invScale = 1.0f; // / 255.0f;
					rowSize = (ulong)worldSize;
				}
				// Read the data
				int srcIndex = 0;
				int dstIndex = 0;
				for ( ulong j = 0; j < (ulong)worldSize; ++j )
				{
					for ( ulong i = 0; i < (ulong)worldSize; ++i )
					{
						if ( is16bit )
						{
#if AXIOM_BIG_ENDIAN
							ushort val = (ushort)(src[srcIndex++] << 8);
							val += src[srcIndex++];
#else
							ushort val = src[ srcIndex++ ];
							val += (ushort)( src[ srcIndex++ ] << 8 );
#endif
							dest[ dstIndex++ ] = new Real( val )*invScale;
						}
						else
						{
							dest[ dstIndex++ ] = new Real( src[ srcIndex++ ] ); // *invScale;
#if (XBOX || XBOX360 )
							srcIndex += 3;
#endif
						}
					}
				}

				// get the data from the heightmap
				this.options.data = dest;
			}

			float maxx = this.options.scalex*this.options.worldSize;
			float maxy = 255*this.options.scaley;
			float maxz = this.options.scalez*this.options.worldSize;

			Resize( new AxisAlignedBox( Vector3.Zero, new Vector3( maxx, maxy, maxz ) ) );

			this.terrainMaterial =
				(Material)
				( MaterialManager.Instance.CreateOrRetrieve(
					!String.IsNullOrEmpty( this.options.MaterialName ) ? this.options.MaterialName : "Terrain",
					ResourceGroupManager.Instance.WorldResourceGroupName ).First );

			if ( this.options.WorldTexture != "" )
			{
				this.terrainMaterial.GetTechnique( 0 ).GetPass( 0 ).CreateTextureUnitState( this.options.WorldTexture, 0 );
			}

			if ( this.options.DetailTexture != "" )
			{
				this.terrainMaterial.GetTechnique( 0 ).GetPass( 0 ).CreateTextureUnitState( this.options.DetailTexture, 1 );
			}

			this.terrainMaterial.Lighting = this.options.isLit;
			this.terrainMaterial.Load();

			this.terrainRoot = (SceneNode)RootSceneNode.CreateChild( "TerrainRoot" );

			this.numTiles = ( this.options.worldSize - 1 )/( this.options.size - 1 );

			this.tiles = new TerrainRenderable[this.numTiles,this.numTiles];

			int p = 0, q = 0;

			for ( int j = 0; j < this.options.worldSize - 1; j += ( this.options.size - 1 ) )
			{
				p = 0;

				for ( int i = 0; i < this.options.worldSize - 1; i += ( this.options.size - 1 ) )
				{
					this.options.startx = i;
					this.options.startz = j;

					string name = string.Format( "Tile[{0},{1}]", p, q );

					var node = (SceneNode)this.terrainRoot.CreateChild( name );
					var tile = new TerrainRenderable();
					tile.Name = name;

					tile.RenderQueueGroup = WorldGeometryRenderQueueId;

					tile.SetMaterial( this.terrainMaterial );
					tile.Init( this.options );

					this.tiles[ p, q ] = tile;

					node.AttachObject( tile );

					p++;
				}

				q++;
			}

			int size1 = this.tiles.GetLength( 0 );
			int size2 = this.tiles.GetLength( 1 );

			for ( int j = 0; j < size1; j++ )
			{
				for ( int i = 0; i < size2; i++ )
				{
					if ( j != size1 - 1 )
					{
						this.tiles[ i, j ].SetNeighbor( Neighbor.South, this.tiles[ i, j + 1 ] );
						this.tiles[ i, j + 1 ].SetNeighbor( Neighbor.North, this.tiles[ i, j ] );
					}

					if ( i != size2 - 1 )
					{
						this.tiles[ i, j ].SetNeighbor( Neighbor.East, this.tiles[ i + 1, j ] );
						this.tiles[ i + 1, j ].SetNeighbor( Neighbor.West, this.tiles[ i, j ] );
					}
				}
			}

			if ( this.options.isLit )
			{
				for ( int j = 0; j < size1; j++ )
				{
					for ( int i = 0; i < size2; i++ )
					{
						this.tiles[ i, j ].CalculateNormals();
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
			if ( this.tiles == null )
			{
				return;
			}
			for ( int i = 0; i < this.tiles.GetLength( 0 ); i++ )
			{
				for ( int j = 0; j < this.tiles.GetLength( 1 ); j++ )
				{
					this.tiles[ i, j ].AlignNeighbors();
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
		public override RaySceneQuery CreateRayQuery()
		{
			return CreateRayQuery( new Ray(), 0xffffffff );
		}

		/// <summary>
		///    Creates a query to return objects found along the ray.
		/// </summary>
		/// <param name="ray">Ray to use for the intersection query.</param>
		/// <returns>A specialized implementation of RaySceneQuery for this scene manager.</returns>
		public override RaySceneQuery CreateRayQuery( Ray ray )
		{
			return CreateRayQuery( ray, 0xffffffff );
		}

		/// <summary>
		///    Creates a query to return objects found along the ray.
		/// </summary>
		/// <param name="ray">Ray to use for the intersection query.</param>
		/// <returns>A specialized implementation of RaySceneQuery for this scene manager.</returns>
		public override RaySceneQuery CreateRayQuery( Ray ray, uint mask )
		{
			var query = new TerrainRaySceneQuery( this );
			query.Ray = ray;
			query.QueryMask = mask;
			return query;
		}

		public Vector3 IntersectSegment( Vector3 start, Vector3 end )
		{
			TerrainRenderable t = GetTerrainTile( start );
			if ( t == null )
			{
				return new Vector3( -1, -1, -1 );
			}
			return t.IntersectSegment( start, end );
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
			if ( this.options == null || this.tiles == null )
			{
				return defaultheight;
			}
			float worldsize = this.options.worldSize;
			float scalex = this.options.scalex;
			float scalez = this.options.scalez;

			int xdim = this.tiles.GetLength( 0 );
			int zdim = this.tiles.GetLength( 1 );

			float maxx = scalex*worldsize;
			var xCoordIndex = (int)( ( point.x*( xdim/maxx ) ) );

			float maxz = scalez*worldsize;
			var zCoordIndex = (int)( ( point.z*zdim/maxx ) );

			if ( xCoordIndex >= xdim || zCoordIndex >= zdim || xCoordIndex < 0 || zCoordIndex < 0 )
			{
				return defaultheight; //point is not over a tile
			}
			return this.tiles[ xCoordIndex, zCoordIndex ].GetHeightAt( point.x, point.z );
		}

		/// <summary>
		///     Returns the TerrainRenderable that contains the given pt.
		//      If no tile exists at the point, it returns 0
		/// </summary>
		/// <param name="point"></param>
		/// <returns></returns>
		public TerrainRenderable GetTerrainTile( Vector3 point )
		{
			if ( this.options == null || this.tiles == null )
			{
				return null;
			}
			float worldsize = this.options.worldSize;
			float scalex = this.options.scalex;
			float scalez = this.options.scalez;

			int xdim = this.tiles.GetLength( 0 );
			int zdim = this.tiles.GetLength( 1 );

			float maxx = scalex*worldsize;
			var xCoordIndex = (int)( ( point.x*( xdim/maxx ) ) );

			float maxz = scalez*worldsize;
			var zCoordIndex = (int)( ( point.z*zdim/maxx ) );

			if ( xCoordIndex >= xdim || zCoordIndex >= zdim || xCoordIndex < 0 || zCoordIndex < 0 )
			{
				return null; //point is not over a tile
			}
			else
			{
				return this.tiles[ xCoordIndex, zCoordIndex ];
			}
		}

		#endregion SceneManager members
	}

	/// <summary>
	///		Factory for <see cref="TerrainSceneManager"/>.
	/// </summary>
	internal class TerrainSceneManagerFactory : SceneManagerFactory
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

		#endregion Methods
	}
}