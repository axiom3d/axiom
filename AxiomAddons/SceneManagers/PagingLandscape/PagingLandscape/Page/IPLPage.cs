#region LGPL License
/*
Axiom Game Engine Library
Copyright (C) 2003  Axiom Project Team

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

#region Using Statements

using System;
using System.Collections;

using Axiom.Core;
using Axiom.MathLib;
using Axiom.Graphics;

using Axiom.SceneManagers.IPLSceneManager.Texture;

#endregion;

namespace Axiom.SceneManagers.IPLSceneManager.Page
{
	/// <summary>
	/// Summary description for IPLPage.
	/// </summary>
	public class IPLPage
	{
		#region Fields

		private SceneNode				pageNode;
		private IPLData2D				data;
		private IPLTexture				texture;

		private IPLVirtualPageWindow*	virtualWindow; // the virtual window taking care of the page

		private int						x;				// Position of this Terrain Page in the Terrain Page Array
		private int						z;

		private float					iniX;			// real world location on x axis
		private float					iniZ;			// real world location on z axis
		private uint					id;			// ID key used by patches to tell them if page changed
		
		// container holding the tiles that make up the page
		private ArrayList				tileContainer;

		//
		static int mPageCount = 0;

		#endregion Fields

		#region Constructor

		/// <summary>
		///		Constructor
		/// </summary>
		/// <param name="creator"></param>
		public	IPLPage( IPLVirtualPageWindow* creator )
		{
			// keep track of how many pages are created
			// used for unique name generation
			this.pageCount++;

			// default position
			this.x = -1;
			this.z = -1;
			this.pageNode = NULL;

			this.virtualWindow = creator;

			loadData2D( );

			loadTexture( );

			// setup page with scene node and tiles if not already done	
			// only happens once during lifetime of page
			this.pageNode = this.virtualWindow.RootSceneNode.CreateChildSceneNode( );

			// build tiles
			int tile_size = 1 << IPLOptions.Instance.mTileLOD;
			int num_tiles = ( IPLOptions.Instance.PageSize ) / tile_size;

			// start off with mID at a low value of one
			// assume patches will start their ID at 0 so that they must update
			this.id = 1;

			// create all the tiles within the page
			this.tileContainer = new ArrayList( num_tiles );

			for ( int i = 0; i < ( int )num_tiles; i++ )
			{
				this.tileContainer.Add( new ArrayList(num_tiles) );

				for ( int j = 0; j < ( int )num_tiles; j++ )
				{
					( (ArrayList) this.tileContainer[ i ]).Add( new IPLTile( this, i, j ) );
				}
			}
		}
    
		#endregion Contructor

		#region Methods

		///TODO: Implement Dispose Method
		
		/// <summary>
		///		loads the landscape data and base texture from the cache 
		/// </summary>
		public void Load( )
		{
			// load the height data
			this.data.Load( this.x, this.z );

			this.texture.Load( this.x, this.z );
		}


		/// <summary>
		///		Unloads the landscape data, and destroys the landscape tiles within the page. 
		/// </summary>
		public void Unload( )
		{
			if ( this.pageNode != null )
			{
				// Unload the nodes
				// detach the tile from the scene node
				this.pageNode.DetachAllObjects( );
				// get rid of child scene nodes
				this.pageNode.Clear();

				(SceneNode)( this.pageNode.Parent ).RemoveChild( this.pageNode.Name );
				this.pageNode = null;
			}

			if ( this.data != 0 )
			{
				this.data.Dispose();;
				this.data = null;
			}

			if ( this.texture != 0 )
			{
				this.texture.Dispose();
				this.texture = null;
			}

			int size = ( int )this.tileContainer.Count;
			for ( int i = 0; i < size; i++ )
			{
				for ( int j = 0; j < size; j++ )
				{
					if ( (ArrayList)(this.tileContainer[ i ])[ j ] != null )
					{
						((IPLTile)((ArrayList)(mTileContainer[ i ])[ j ])).Dispose();
					}
				}
			}
		}


		/// <summary>
		///		set the real world position of the page
		/// </summary>
		/// <param name="startx">position in world on x axis that top left corner of page sits</param>
		/// <param name="startz">position in world on z axis that top left corner of page sits</param>
		/// <remarks>
		///		the page will modify the position of its scene node
		///		page indexes are not modified so all that is happening is
		///		the terrain will shift position in the world
		/// </remarks>
		public void SetWorldPosition( float startx, float startz )
		{
			this.iniX = startx;
			this.iniZ = startz;

			this.pageNode.Position = new Vector3( this.iniX , 0.0, this.iniZ );
		}


		/// <summary>
		///		set the page indexes within the map
		/// </summary>
		/// <param name="x">index into Map on x axis for page</param>
		/// <param name="z">index into Map on z axis for page</param>
		/// <remarks>
		///		the page will reload itself with the proper height data
		///		if the new indexes are different than the current
		/// </remarks>
		public void SetPageMapIndexes( int x, int z )
		{
			// if map indexes are changing then unload the page and reload
			if ( ( x != this.x ) || ( z != this.z ) )
			{
				// x and z should never be negative
				//assert( x >= 0 );
				//assert( z >= 0 );

				this.x = x;
				this.z = z;

				// load the page data from the cache
				this.Load( );
				// change the page ID to force patches to update when they become visible
				this.id++;
			}
		}


		public int GetHeight( int x, int z )
		{
			return this.data.GetHeight( x, z );
		}


		public float GetWorldHeight( int x, int z )
		{
			return this.data.GetHeight( ( Math.Abs( x ) - Math.Abs( this.iniX ) ) / IPLOptions.Instance.scale.x, ( Math.Abs( z ) - Math.Abs( this.iniZ ) ) / IPLOptions.Instance.scale.z ) * IPLOptions.Instance.scale.y;
		}


		/// <summary>
		///		Get the real world height at a particular position within the page
		/// </summary>
		/// <param name="x">x position relative to page and in page scale</param>
		/// <param name="z">z position relative to page and in page scale</param>
		/// <returns>
		///		the float returned is the real world height based on the scale of the world.  If the height could
		///		not be determined then -1 is returned and this would only occur if the page was not preloaded or loaded
		/// </returns>
		/// <remarks>
		///		Method is used to get the terrain height at a position based on x and z that is local to the page.
		///		This method just figures out what tile the position is on and then asks the tile node
		///		to do the dirty work of getting the height. Again passing the buck.
		/// </remarks>
		public float GetRealWorldHeight( float x, float z )
		{
			return this.data.GetInterpolatedHeight( x, z ) * IPLOptions.Instance.scale.y;
		}


		#endregion Methods

		#region Properties

		public Material Material		
		{
			get
			{
				return null;
			}
		}

		public SceneNode PageNode		
		{
			get
			{
				return this.pageNode;
			}
		}

		public IPLData2D HeightData		
		{
			get
			{
				this.data;
			}
		}

		public IPLTexture Texture		
		{
			get
			{
				return this.texture;
			}
		}

		public uint ID
		{
			get
			{
				return this.id;
			}
		}

		#endregion Properties

		#region Operations

		private	void loadData2D( )
		{
			// set the specialized to the data loader
			if ( IPLOptions.Instance.mData2DFormat == "HeightField" )
			{
				this.data = new IPLData2D_HeightField( );
			}
			else if ( IPLOptions.Instance.mData2DFormat == "HeightFieldTC" )
			{
				this.data = new IPLData2D_HeightFieldTC( );
			}
			else if ( IPLOptions.Instance.mData2DFormat == "SplineField" )
			{
				this.data = new IPLData2D_Spline( );
			}
			else
			{
				this.data = null;
				throw new Exception("IPLData2D not supplied!");
			}

		}

		private void loadTexture( )
		{
			// set the specialized texture loader
			if ( IPLOptions.Instance.mTextureFormat == "Image" )
			{
				this.texture = new IPLTexture_Image( this );
			}
			else if ( IPLOptions.Instance.mTextureFormat == "BaseTexture" )
			{
				this.texture = new IPLTexture_BaseTexture( this );
			}
			else if ( IPLOptions.Instance.mTextureFormat == "BaseTexture2" )
			{
				this.texture = new IPLTexture_BaseTexture2( this );
			}
			else if ( IPLOptions.Instance.mTextureFormat == "Splatting1" )
			{
				this.texture = new IPLTexture_Splatting1( this );
			}
			else
			{
				this.texture = 0;
				throw new Exception("IPLTexture not supplied!");
			}
			this.texture.setupMaterial( this.pageCount, this.virtualWindow );
		}

		#endregion Operations

	}
}
