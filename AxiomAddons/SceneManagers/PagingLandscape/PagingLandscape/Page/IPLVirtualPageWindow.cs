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

#endregion;


namespace Axiom.SceneManagers.IPLSceneManager.Page
{
	/// <summary>
	/// This is a virtual window page manager for organizing Pages into a visible landscape.
	/// </summary>
	public class IPLVirtualPageWindow
	{

		#region Fields

		private float	worldScaleX;
		private float	worldScaleZ;
		private int		pagePixelSize;
		private float	boundryExtent;

		/// <summary>
		///		number of pages that make up the window along the x axis
		/// </summary>
		private int		windowPageCountX;		

		/// <summary>
		///		number of pages that make up the window along the z axis
		/// </summary>
		private int		windowPageCountZ;		

		/// <summary>
		///		number of pages that make up the map along x axis
		/// </summary>
		private int		worldMap_PageCountX;	

		/// <summary>
		///		number of pages that make up the map along z axis
		/// </summary>
		private int		worldMap_PageCountZ;	 

		/// <summary>
		///		position of window in absolute page index from origin on x axis
		/// </summary>
		private int		absolutePageOffsetX;   

		/// <summary>
		///		position of window in absolute page index from origin on z axis
		/// </summary>
		private int		absolutePageOffsetZ;   

		/// <summary>
		///		relative Map page index for bottom left corner of Window on x axis
		/// </summary>
		private int		mapPageOffsetX;	

		/// <summary>
		///		relative Map page index for bottom left corner of Window on z axis
		/// </summary>
		private int		mapPageOffsetZ;		

		// center area boundary
		private float	boundryMinX; 
		private float	boundryMinZ;
		private float	boundryMaxX;
		private float	boundryMaxZ;

		private IPLSceneManager sceneManager;
		private SceneNode rootSceneNode;

		private ArrayList activeWindow;
		private ArrayList oldWindow;

		#endregion Fields

		#region Constructor

		/// <summary>
		///		Constructor
		/// </summary>
		/// <param name="sm">SceneManager that created this VirtualPageManager</param>
		public IPLVirtualPageWindow(IPLSceneManager creator )
		{
			// initialize members to default settings

			boundryExtent = 0.10f;
			sceneManager = creator;
			rootSceneNode = creator.RootSceneNode;
			this.SetWorldScale( IPLOptions.Instance.scale.x, IPLOptions.Instance.scale.z );
			this.SetWorldMapSize( IPLOptions.Instance.world_height, IPLOptions.Instance.world_width, IPLOptions.Instance.PageSize );
			this.SetPageWindowSize( IPLOptions.Instance.virtual_window_width, IPLOptions.Instance.virtual_window_height );

			preLoad( );

			// force update view;
			boundryMaxX = -1.0F;
			this.UpdateViewPosition( 0, 0 );
		}

		#endregion Constructor

		#region Properties

		/// <summary>
		///		returns SceneManager that owns this VirtualPageManager
		/// </summary>
		public IPLSceneManager SceneManager
		{
			get 
			{
				return this.sceneManager;
			}
		}

		/// <summary>
		///		returns root scene node
		/// </summary>
		public SceneNode RootSceneNode
		{
			get
			{
				return this.rootSceneNode;
			}
		}

		#endregion Properties

		#region Methods

		/// <summary>
		///		setup page windows by initializing both page vector containers
		///		to the required size and fill with pages in active window and NULL 
		///		in old window
		/// </summary>
		/// <param name="xcount">number of pages in window on x axis</param>
		/// <param name="zcount">number of pages in window on z axis</param>
		public void SetPageWindowSize(  int xcount,  int zcount )
		{
			// check window is getting bigger or smaller
			// would be nice to be able to resize at any time
			// there is a lot more to this so stay simple for now

			// for now assume no size to window
			// resize both mWindowPages1 and mWindowPages2 at the same time
			windowPageCountX = xcount; 
			windowPageCountZ = zcount;

			activeWindow = new ArrayList(zcount);
			oldWindow = new ArrayList(zcount);

			for ( int z = 0; z < zcount; z++ )
			{
				activeWindow.Add( new ArrayList( xcount) );
				oldWindow.Add( new ArrayList( xcount ) );

				for (int x = 0; x < xcount; x++ )
				{
					// populate Active Window with pages
					( (ArrayList) activeWindow[z]).Add( new IPLPage( this ) );
					// populate old window with NULLs
					( (ArrayList) oldWindow[z]).Add( null );

					// pages will be seeded later
				}
			}		
		}


		/// <summary>
		///		update the window view based on the new position
		/// </summary>
		/// <param name="x"></param>
		/// <param name="z"></param>
		/// <remarks>
		///		the window manager will shift the window view if
		///		the view position is outside the view boundaries
		///		if the view position is still in the view boundaries then nothing is done
		/// </remarks>
		public void UpdateViewPosition(  float x,  float z )
		{
			// if the new view is outside the center area of the window then update
			if ( isViewCentered( x, z ) == false )
			{
				// calculate new center area in world coordinates for the window
				// figure out the new page for the 0,0 position in the window

				// figure out shift of pages in window by (old page - new page) at 0,0 window position
				int oldMapPageOffsetX = mapPageOffsetX;
				int oldMapPageOffsetZ = mapPageOffsetZ;

				setCenterAreaBoundries( x, z );

				int firstShiftX = oldMapPageOffsetX - mapPageOffsetX;
				int firstShiftZ = oldMapPageOffsetZ - mapPageOffsetZ;

				// need to find the smallest shift so that we minimize the pages that must reload

				// calculate second shift for X
				// if first shift is less than 0 then add X page count
				// else subtract X page count
				int secondShiftX = firstShiftX + ( ( firstShiftX < 0 ) ?  worldMap_PageCountX : -worldMap_PageCountX );
				// chose smallest absolute shift
				firstShiftX = ( Math.Abs( firstShiftX ) < Math.Abs( secondShiftX ) ) ? firstShiftX : secondShiftX;

				// calculate second shift for Z
				// if first shift is less than 0 then add Z page count
				// else subtract Z page count
				int secondShiftZ = firstShiftZ + ( ( firstShiftZ < 0 ) ?  worldMap_PageCountZ : -worldMap_PageCountZ );
				// chose smallest absolute shift
				firstShiftZ = ( Math.Abs( firstShiftZ ) < Math.Abs( secondShiftZ ) ) ? firstShiftZ : secondShiftZ;

				shufflePages( firstShiftX, firstShiftZ );
			}		
		}


		/// <summary>
		///		change the parameters that define how big the world map is
		/// </summary>
		/// <param name="PageXCount">number of pages that are along the X axis in the Map</param>
		/// <param name="PageZCount">number of pages that are along the Z axis in the Map</param>
		/// <param name="PagePixelSize">is the width and height in pixel of the height map data that defines the page</param>
		public void SetWorldMapSize(  int PageXCount,  int PageZCount,  int PagePixelSize )
		{
			worldMap_PageCountX = PageXCount;
			worldMap_PageCountZ = PageZCount;
			pagePixelSize = PagePixelSize;
			// need to update window position
		}


		/// <summary>
		///		change the world scale - values must be > 0
		/// </summary>
		/// <param name="x">x scale</param>
		/// <param name="z">z scale</param>
		public void SetWorldScale(  float x,  float z )
		{
			worldScaleX = x;
			worldScaleZ = z;
		}


		/// <summary>
		///		get the height of the terrain at the world coordinates x,z
		/// </summary>
		/// <param name="worldx">real world position on x axis</param>
		/// <param name="worldz">real world position on z axis</param>
		/// <returns>
		///		if the world coordinates are within the virtual page window then the actual world height
		///		is returned.  If outside the virtual window then -1 is returned
		/// </returns>
		public float GetWorldHeight( float worldx, float worldz )
		{
			int pageX;
			int pageZ;
			// determine the page within the virtual page window that the world coordinates refer to
			// calculate the absolute page indexes
			this.getWorldPageIndices( worldx, worldz, out pageX, out pageZ );

			// scale down world coordinates to 1::1 scale
			worldx /= worldScaleX;
			worldz /= worldScaleZ;

			// subtract page offsets to get point within page
			// make sure not below zero - negative numbers don't work good for indexes
			worldx -= ( float )( pageX * ( pagePixelSize - 1.0f ) );
			if ( worldx < 0 )
			{
				worldx += ( float )( pagePixelSize - 1.0f );
				// when negative must compensate page index offset
				pageX--;
			}

			// make sure not below zero
			worldz -= ( float )( pageZ * ( pagePixelSize - 1.0f ) );
			if ( worldz < 0 )
			{
				worldz += ( float )( pagePixelSize - 1.0f );
				// when negative must compensate page index offset
				pageZ--;
			}

			// make page index relative to virtual window
			pageX -= absolutePageOffsetX;
			pageZ -= absolutePageOffsetZ;

			// if page indices are not int the window then height is -1
			if ( pageX < 0 )
			{
				return -1;
			}
			if ( pageX >= windowPageCountX )
			{
				return -1;
			}
			if ( pageZ < 0 )
			{
				return -1;
			}
			if ( pageZ >= windowPageCountZ )
			{
				return -1;
			}

			// no value should be below zero
			//assert( ( pageZ >= 0 ) && ( pageX >= 0 ) );
			//assert( ( worldx >= 0 ) && ( worldz >= 0 ) );

			// get the page to calculate the height
			return ((IPLPage) ((ArrayList) activeWindow[ pageZ ])[ pageX ]).GetRealWorldHeight( worldx, worldz );
		}


		#endregion Methods

		#region Operations

		/// <summary>
		/// 
		/// </summary>
		private void preLoad(  )
		{
			// if cache enabled load all the height maps and textures
			// - this is to dirty the cache to make page loading faster since the data is now paged by the OS
			// this is not the best way to implement a cache but its simple, cheap and works in windows XP
			if ( IPLOptions.Instance.mPreloadCache == true )
			{
				// Preload the height fields
				if ( IPLOptions.Instance.mData2DFormat == "HeightField" )
				{
					IPLData2D_HeightField.preLoad( );
				}
				else if ( IPLOptions.Instance.mData2DFormat == "HeightFieldTC" )
				{
					IPLData2D_HeightFieldTC.preLoad( );
				}
				else if ( IPLOptions.Instance.mData2DFormat == "SplineField" )
				{
					IPLData2D_Spline.preLoad( );
				}
				// Preload the textures
				if ( IPLOptions.Instance.mTextureFormat == "Image" )
				{
					IPLTexture_Image.preLoad( );
				}
			}
		}

		/// <summary>
		///		swap ActiveWindow and OldWindow.  
		///		Normally done after the pages have been shuffled in the window.
		/// </summary>
		private void switchWindow(  ) // TODO: Not implemented
		{
		}

		/// <summary>
		///		shift the pages in the active window into the Old window
		///		after the shift each page is informed of its new map index and new position for the scene node
		///		Swap Active window with old window so the shuffled windows become the active view
		/// </summary>
		/// <param name="ShiftX">number of pages to shift on the x axis</param>
		/// <param name="ShiftZ">number of pages to shift on the z axis</param>
		private void shufflePages(  int ShiftX,  int ShiftZ )
		{
			// shuffle pages in active window to old window using ShiftX and ShiftZ as offsets
			// loop through window pages on z axis
			int newPos_x;
			int newPos_z;
			// flip old and active window references
			ArrayList tmp = new ArrayList( activeWindow );
			activeWindow = oldWindow;
			oldWindow = tmp;

			// if shift is greater than window size than make shift 0
			if ( ( ShiftX >= windowPageCountX ) || ( ShiftX <= -windowPageCountX ) )
			{
				ShiftX = 0;
			}
			if ( ( ShiftZ >= windowPageCountZ ) || ( ShiftZ <= -windowPageCountZ ) )
			{
				ShiftZ = 0;
			}

			for ( int z = 0; z < windowPageCountZ; z++ )
			{

				newPos_z = z + ShiftZ;
				if ( newPos_z < 0 )
				{
					newPos_z += windowPageCountZ;
				}
				else if ( newPos_z >=  windowPageCountZ )
				{
					newPos_z -= windowPageCountZ;
				}
				//assert( newPos_z >= 0 );
				//assert( newPos_z < windowPageCountZ );

				// loop through window pages on x axis
				for( int x = 0; x < windowPageCountX; x++ )
				{
					// calculate new window pane position
					newPos_x = x + ShiftX;
					if ( newPos_x < 0 )
					{
						newPos_x += windowPageCountX;
					}
					else if ( newPos_x >=  windowPageCountX )
					{
						newPos_x -= windowPageCountX;
					}

					//assert( newPos_x >= 0 );
					//assert( newPos_x < windowPageCountX );

					// transfer page pointer from old window to active window
					((ArrayList) activeWindow[ newPos_z ])[ newPos_x ] = ((ArrayList) oldWindow[ z ])[ x ];

					// tell page about page index and position change
					((IPLPage) ((ArrayList) activeWindow[ newPos_z ])[ newPos_x ]).SetWorldPosition( ( absolutePageOffsetX + newPos_x ) * ( pagePixelSize - 1 ) * worldScaleX,
						( absolutePageOffsetX + newPos_z ) * ( pagePixelSize - 1 ) * worldScaleZ );

					((IPLPage) ((ArrayList) activeWindow[ newPos_z ])[ newPos_x ]).SetPageMapIndexes( ( mapPageOffsetX + newPos_x ) % worldMap_PageCountX,
						( mapPageOffsetZ + newPos_z ) % worldMap_PageCountZ );
				}
			}
		}


		/// <summary>
		///		notify pages in window of what map indexes and scene node position they should have
		///		this method would be called after calling shufflePages
		///		It is up to the pages to decide if they need to unload and reload.  Moving the scene node is upto them.
		/// </summary>
		private void updatePageMapIndexes(  ) // TODO: Not implemented
		{
		}


		/// <summary>
		///		checks if the world coordinates are within the window center region
		///		returns true if the view is within the boundaries
		/// </summary>
		/// <param name="worldx">position on x axis in the world</param>
		/// <param name="worldz">position on z axis in the world</param>
		/// <returns></returns>
		private bool isViewCentered(  float worldx,  float worldz ) 
		{
			// check if world x is within x boundaries
			// check if world z is within z boundaries
			if( ((worldx <= boundryMaxX) && (worldx >= boundryMinX)) &&
				((worldz <= boundryMaxZ) && (worldz >= boundryMinZ)))
			{
				return true;
			}
			else
			{
				return false;
			}
		}


		/// <summary>
		///		using the world coordinates as the new center view position calculate
		///		min x,max x, min v and max v that define the center boundaries.  These boundaries are
		///		used by isVeiwCentered to determine if the view position is still within
		///		the center boundaries
		/// </summary>
		/// <param name="worldx">world position on world x axis</param>
		/// <param name="worldz">world position on world y axis</param>
		private void setCenterAreaBoundries(  float worldx,  float worldz )
		{
			/*
				// center area must be slightly larger than the page size
				// this is so that when the window shifts the world view position will
				// be within in the center boundaries
				// if the boundaries are smaller than a page then a race condition develops
				// and the window keep shifting to try and position the view in the center area

				// use page coordinate in
				// given a world position need to center window based on page size
				// get window as close as page size possible
				// not sure how to do this
				// what formula is required
				// a page offset to position 0,0 of window is a start
				// once 0,0 calculated then rest is easy
				// world position is within a center area
				// center areas always the same position
				// so figure out root center position and go from there
				// so x page = worldposX / page size
				// this would give the absolute page count
				// to get the relative page index in the map use x page % map page count
				// to get map page index to window 0,0 position it would be to the left and down from the center
				// know window size 
				// divide by 2 to get the middle then take whole number ie (3-1)/2 = 1; (5-1)/2 = 2; (6-1)/2 = 2

			*/
			// calculate new center area in world coordinates for the window

			float WorldHalfPosition_x = (worldx * 2.0f) / (pagePixelSize - 1) / worldScaleX;
			float WorldHalfPosition_z = (worldz * 2.0f) / (pagePixelSize - 1) / worldScaleZ;

			// calculate the bottom left corner of the window position in absolute page indexes

			absolutePageOffsetX = (int)Math.Floor((WorldHalfPosition_x - (windowPageCountX - 1.0f))/2.0f);
			absolutePageOffsetZ = (int)Math.Floor((WorldHalfPosition_z - (windowPageCountZ - 1.0f))/2.0f);

			// calculate the center area boundaries
			boundryMinX = (((float)absolutePageOffsetX  + (float)windowPageCountX / 2.0f) - (0.5f + boundryExtent)) * pagePixelSize * worldScaleX;
			boundryMaxX = boundryMinX + (2.0f * (0.5f + boundryExtent)) * pagePixelSize * worldScaleX;

			boundryMinZ = (((float)absolutePageOffsetZ  + (float)windowPageCountZ / 2.0f) - (0.5f + boundryExtent)) * pagePixelSize * worldScaleZ;
			boundryMaxZ = boundryMinZ + (2.0f * (0.5f + boundryExtent)) * pagePixelSize * worldScaleZ;

			// set up map page offset indexes for bottom left corner of window at 0,0 within the map
			getMapPageOffset(absolutePageOffsetX, absolutePageOffsetZ, out mapPageOffsetX, out mapPageOffsetZ);
		}


		/// <summary>
		///		convert world coordinates into page map index
		///		assumes the map repeats in x and z direction in the world
		///		works for negative coordinates also
		///		normally used to find the center page in the window
		/// </summary>
		/// <param name="x">coordinate on x axis</param>
		/// <param name="z">coordinate on z axis</param>
		/// <param name="PageX">reference to page index on x axis that will receive converted value</param>
		/// <param name="PageZ">reference to page index on z axis that will receive converted value</param>
		private void getPageMapIdxFromWorldPosition( float x, float z, out int  PageX, out int  PageZ )
		{
			getWorldPageIndices( x, z,out PageX, out PageZ);
			getMapPageOffset( PageX, PageZ, out PageX, out PageZ );
		}


		/// <summary>
		///		get the Map Page offsets using absolute page indexes
		/// </summary>
		/// <param name="AbsolutePage_x">page index on x axis</param>
		/// <param name="AbsolutePage_z">page index on z axis</param>
		/// <param name="MapPageOffsetX">reference that will receive the page offset in relation to the map on the x axis</param>
		/// <param name="MapPageOffsetZ">reference that will receive the page offset in relation to the map on the z axis</param>
		/// <remarks>
		///		the method assumes that the map is repeating in both x and z coordinates and that the map
		///		has a size equal or greater to one or both axis
		/// </remarks>
		private void getMapPageOffset(  int AbsolutePage_x,  int AbsolutePage_z, out int  MapPageOffsetX, out int MapPageOffsetZ )
		{
			// calculate remainder of absolute page index / map size to get the page offset within the map
			MapPageOffsetX = AbsolutePage_x % worldMap_PageCountX ;
			// if the remainder is less than zero then add the map size to get the relative page index into the map
			if (MapPageOffsetX < 0)
			{
				MapPageOffsetX += worldMap_PageCountX;
			}
			MapPageOffsetZ = AbsolutePage_z % worldMap_PageCountZ ;
			if (MapPageOffsetZ < 0)
			{
				MapPageOffsetZ += worldMap_PageCountZ;
			}
		}


		/// <summary>
		///		Get the Page absolute indices from a world position vector
		/// </summary>
		/// <param name="worldx">position on x axis</param>
		/// <param name="worldz">position on z axis</param>
		/// <param name="PageX">result placed in reference to the x index of the page</param>
		/// <param name="PageZ">result placed in reference to the z index of the page</param>
		/// <remarks>
		///		Method is used to find the Page indices using a world position.
		///		Beats having to iterate through the Page list to find a page at a particular
		///		position in the world.
		///		The page indices returned are not clamped to the map page count.
		/// </remarks>
		private void getWorldPageIndices(  float worldx,  float worldz, out int PageX, out int PageZ )
		{
			PageX = (int)(worldx / worldScaleX / (pagePixelSize - 1.0 ) );
			PageZ = (int)(worldz / worldScaleZ / (pagePixelSize - 1.0 ) );
		}

		#endregion Operations

	};

}


