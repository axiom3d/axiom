#region LGPL License
/*
Axiom Game Engine Library
Copyright (C) 2003  Axiom Project Team

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

#region Namespace Declarations

using System;
using System.Collections;
using System.Diagnostics;

using Axiom.Core;
using Axiom;
using Axiom.MathLib;

using Axiom.SceneManagers.PagingLandscape.Collections;
using Axiom.SceneManagers.PagingLandscape.Tile;

#endregion Using Directives

namespace Axiom.SceneManagers.PagingLandscape.Data2D
{

	/// <summary>
	/// Summary description for Data2D.
	/// </summary>
	public abstract class Data2D : IDisposable
	{

		#region Fields

		/// <summary>
		/// computed Height Data  (scaled)
		/// </summary>
		protected float[] heightData;

		/// <summary>
		/// maximum position in Array
		/// </summary>
		protected long maxArrayPos;

		/// <summary>
		/// data side maximum size
		/// </summary>
		protected long size;

		/// <summary>
		/// image data  maximum size
		/// </summary>
		protected long max;

		/// <summary>
		/// maximum page/data2d height. (scaled)
		/// </summary>
		protected float maxheight;

		/// <summary>
		/// if data loaded or not
		/// </summary>
		protected bool isLoaded;

		/// <summary>
		/// 
		/// </summary>
		protected ArrayList newHeight;

		/// <summary>
		/// 
		/// </summary>
		protected bool dynamic;


		#endregion Fields

		/// <summary>
		/// 
		/// </summary>
		public Data2D() 
		{
			dynamic = false;
			isLoaded = false;
			heightData = null;
			newHeight = new ArrayList();
		}

		/// <summary>
		/// 
		/// </summary>
		public bool Dynamic 
		{
			get 
			{
				return dynamic;
			}
		}

		#region IDisposable Members



		public virtual void Dispose()

		{

			if (heightData != null)
				heightData = null;
			newHeight.Clear();
			isLoaded = false;		

		}



		#endregion

		/// <summary>
		/// 
		/// </summary>
		/// <param name="X"></param>
		/// <param name="Z"></param>
		public virtual void Load(  float X,  float Z)
		{
			isLoaded = true;
			load(X, Z);
		}

		/// <summary>
		/// 
		/// </summary>
		public virtual void Load()
		{
			isLoaded = true;
			load();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="NewHeightmap"></param>
		public virtual void Load(Image NewHeightmap)

		{
			isLoaded = true;
			load(NewHeightmap);
		}

		/// <summary>
		/// 
		/// </summary>
		public virtual void Unload()
		{
			isLoaded = false;
			unload();
		}

		/// <summary>
		/// Method that deform Height Data of the terrain.
		/// </summary>
		/// <param name="deformationPoint">Where modification is, in world coordinates</param>
		/// <param name="modificationHeight">What modification do to terrain</param>
		/// <param name="info">Give some info on tile to help coordinate system change</param>
		/// <returns></returns>
		public float DeformHeight( Vector3 deformationPoint, float modificationHeight, TileInfo info)
		{
			if ( heightData != null )
			{
				int pSize = (int)Options.Instance.PageSize;

				// adjust x and z to be local to page
				int x = (int) ((deformationPoint.x ) 
						- ((info.PageX - Options.Instance.World_Width * 0.5f) * (pSize)));
				int z = (int) ((deformationPoint.z ) 
						- ((info.PageZ - Options.Instance.World_Height * 0.5f) * (pSize)));

				long arraypos = z * size + x;

				if (arraypos < maxArrayPos)
				{
					if ((heightData[arraypos] - modificationHeight) > 0.0f)
						heightData[arraypos] -= modificationHeight;
					else
						heightData[arraypos] = 0.0f;
					return heightData[arraypos];
				}
			}
			return 0.0f;
		}

		/// <summary>
		/// Method that deform Height Data of the terrain.
		/// </summary>
		/// <param name="x">x Position on 2d height grid</param>
		/// <param name="z">z Position on 2d height grid</param>
		/// <param name="modificationHeight">What modification do to terrain</param>
		/// <returns></returns>
		public float DeformHeight(long x, long z, float modificationHeight)
		{
			if ( heightData != null)
			{
				long arraypos = z * size + x;

				if (arraypos < maxArrayPos)
				{
					if ((heightData[arraypos] - modificationHeight) > 0.0f)
						heightData[arraypos] -= modificationHeight;
					else
						heightData[arraypos] = 0.0f;
					return heightData[arraypos];
				}
			}
			return 0.0f;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="mX"></param>
		/// <param name="mZ"></param>
		/// <returns></returns>
		public virtual Vector3 GetNormalAt (float mX, float mZ)

		{

			return Vector3.UnitY;

		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="mX"></param>
		/// <param name="mZ"></param>
		/// <returns></returns>
		public virtual ColorEx GetBase (float mX, float mZ)
		{
			return ColorEx.White;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="mX"></param>
		/// <param name="mZ"></param>
		/// <returns></returns>
		public virtual ColorEx GetCoverage (float mX, float mZ)
		{
			return ColorEx.White;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="x"></param>
		/// <param name="z"></param>
		/// <param name="info"></param>
		/// <returns></returns>
		public float GetHeightAbsolute(float x, float z, TileInfo info)
		{
			if ( heightData != null)
			{
				int pSize = (int)Options.Instance.PageSize;
				Vector3 scale = Options.Instance.Scale;

				// adjust x and z to be local to page
				int i_x = ((int)(x / scale.x) 
					- ((int)(info.PageX - Options.Instance.World_Width * 0.5f) * (pSize)));
				int i_z = ((int)(z / scale.z) 
					- ((int)(info.PageZ - Options.Instance.World_Height * 0.5f) * (pSize)));

				long arraypos = (long)(i_z * size + i_x); 
				if (arraypos < maxArrayPos)
					return heightData[arraypos];
			}
			return 0.0f;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="x"></param>
		/// <param name="z"></param>
		/// <returns></returns>
		public virtual float GetHeight( float x, float z )
		{
			if ( heightData != null )
			{
				long Pos = (long) (( z * size )+ x);
				if ( maxArrayPos > Pos )
					return heightData[ Pos ];
			}
			return 0.0f;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="x"></param>
		/// <param name="z"></param>
		/// <returns></returns>
		public virtual float GetHeight(  long x, long z )
		{
			if ( heightData != null)
			{
				long Pos = z * size + x;
				if ( maxArrayPos > Pos )
					return heightData[ Pos ];
			}
			return 0.0f;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="x"></param>
		/// <param name="z"></param>
		/// <returns></returns>
		public virtual float GetHeight( int x, int z )
		{
			if ( heightData != null )
			{
				int Pos = z * (int)size + x;
				if ( maxArrayPos > (long) Pos )
					return heightData[ Pos ];
			}
			return 0.0f;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="x"></param>
		/// <param name="z"></param>
		/// <param name="h"></param>
		public void SetHeight( long x, long z, float h )
		{
			if ( heightData != null )
			{
				long Pos = ( z * size ) + x;
				if ( maxArrayPos > Pos )
					heightData[ Pos ] = h;
			}
		}
	    
		/// <summary>
		/// 
		/// </summary>
		public float MaxHeight
		{
			get
			{
				return maxheight;
			}
			set
			{
				maxheight = value;
			}
		}


		/// <summary>
		/// 
		/// </summary>
		public float[] HeightData
		{
			get
			{
				return heightData;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		public bool IsLoaded
		{
			get
			{
				return isLoaded;
			}
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="NewHeight"></param>
		/// <returns></returns>
		public bool AddNewHeight(Sphere NewHeight)
		{
			//std::vector<Sphere>::iterator cur, end = mNewHeight.end();
			//for( cur = mNewHeight.begin(); cur < end; cur++ )
			for (int index= 0; index < newHeight.Count; index ++)
			{
				if( ((Sphere)newHeight[index]).Intersects( NewHeight ) == true )
				{
					// We don´t allow to heights to intersect
					return false;
				}
			}
			newHeight.Add(NewHeight);
			return true;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="oldHeight"></param>
		/// <returns></returns>
		public bool RemoveNewHeight(Sphere oldHeight)
		{
			//std::vector<Sphere>::iterator cur, end = mNewHeight.end();
			//for( cur = mNewHeight.begin(); cur < end; cur++ )
			for (int index= 0; index < newHeight.Count; index ++)
			{
				if( ((Sphere)newHeight[index]).Intersects( oldHeight ) == true )
				{
					// Since we don´t allow to heights to intersect we can delete this one
					newHeight.RemoveAt(index);
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="X"></param>
		/// <param name="Z"></param>
		protected abstract void load(float X, float Z);
		/// <summary>
		/// 
		/// </summary>
		/// <param name="NewHeightmap"></param>
		protected abstract void load(Image NewHeightmap);
		/// <summary>
		/// 
		/// </summary>
		protected abstract void load();
		/// <summary>
		/// 
		/// </summary>
		protected abstract void unload();

		/// <summary>
		/// 
		/// </summary>
		/// <param name="s"></param>
		/// <returns></returns>
		protected bool checkSize( int s )
		{
			for ( int i = 0; i < 256; i++ ) 
			{
				//printf( "Checking...%d\n", ( 1 << i ) + 1 );
				if ( s == ( 1 << i ) + 1 ) 
				{
					return true;
				}
			}
			return false;
		}

	}

}

