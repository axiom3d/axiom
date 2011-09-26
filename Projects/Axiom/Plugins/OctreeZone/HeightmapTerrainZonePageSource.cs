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
//     <id value="$Id:$"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Axiom.Math;
using Axiom.Media;
using Axiom.Core;

#endregion Namespace Declarations

namespace OctreeZone
{
	public class HeightmapTerrainZonePageSource : TerrainZonePageSource
	{

		/// Is this input RAW?
		protected bool mIsRaw;
		/// Should we flip terrain vertically?
		protected bool mFlipTerrainZone;
		/// Image containing the source heightmap if loaded from non-RAW
		protected Image mImage;
		/// Arbitrary data loaded from RAW
		protected MemoryStream mRawData;
		/// The (single) terrain page this source will provide
		protected TerrainZonePage mPage;
		/// Source file name
		protected string mSource;
		/// Manual size if source is RAW
		protected int mRawSize;
		/// Manual bpp if source is RAW
		protected byte mRawBpp;


		//-------------------------------------------------------------------------
		public HeightmapTerrainZonePageSource()
		{
			mIsRaw = false;
			mFlipTerrainZone = false;
			mPage = null;
		}
		//-------------------------------------------------------------------------
		~HeightmapTerrainZonePageSource()
		{
			Shutdown();
		}
		//-------------------------------------------------------------------------
		public override void Shutdown()
		{
			if ( null != mImage )
			{
				mImage.Dispose();
			}
			mPage = null;
		}
		//-------------------------------------------------------------------------
		public void LoadHeightmap()
		{
			int imgSize;
			// Special-case RAW format
			if ( mIsRaw )
			{
				// Image size comes from setting (since RAW is not self-describing)
				imgSize = mRawSize;

				// Load data
				mRawData = null;
				Stream stream = ResourceGroupManager.Instance.OpenResource( mSource, ResourceGroupManager.Instance.WorldResourceGroupName );
				byte[] buffer = new byte[ stream.Length ];
				stream.Read( buffer, 0, (int)stream.Length );
				mRawData = new MemoryStream( buffer );

				// Validate size
				int numBytes = imgSize * imgSize * mRawBpp;
				if ( mRawData.Length != numBytes )
				{
					Shutdown();
					throw new AxiomException( "RAW size (" + mRawData.Length +
											 ") does not agree with configuration settings. HeightmapTerrainZonePageSource.LoadHeightmap" );
				}
			}
			else
			{
				mImage =
					Image.FromStream( ResourceGroupManager.Instance.OpenResource( mSource,
																				ResourceGroupManager.Instance.
																					WorldResourceGroupName ), mSource.Split( '.' )[ 1 ] );

				// Must be square (dimensions checked later)
				if ( mImage.Width != mImage.Height )
				{
					Shutdown();
					throw new AxiomException( "Heightmap must be square. HeightmapTerrainZonePageSource.LoadHeightmap" );
				}
				imgSize = mImage.Width;
			}
			//check to make sure it's the expected size
			if ( imgSize != mPageSize )
			{
				Shutdown();
				throw new AxiomException( "Error: Invalid heightmap size : " + imgSize + ". Should be " + mPageSize + "HeightmapTerrainZonePageSource.LoadHeightmap" );
			}

		}
		//-------------------------------------------------------------------------
		public override void Initialize( TerrainZone tsm, int tileSize, int pageSize, bool asyncLoading,
			TerrainZonePageSourceOptionList optionList )
		{
			// Shutdown to clear any previous data
			Shutdown();

			base.Initialize( tsm, tileSize, pageSize, asyncLoading, optionList );

			// Get source image

			bool imageFound = false;
			mIsRaw = false;
			bool rawSizeFound = false;
			bool rawBppFound = false;
			foreach ( KeyValuePair<string, string> opt in optionList )
			{
				string key = opt.Key;
				key = key.Trim();
				if ( key.StartsWith( "HeightmapImage".ToLower(), StringComparison.InvariantCultureIgnoreCase ) )
				{
					mSource = opt.Value;
					imageFound = true;
					// is it a raw?
					if ( mSource.ToLowerInvariant().Trim().EndsWith( "raw" ) )
					{
						mIsRaw = true;
					}
				}
				else if ( key.StartsWith( "Heightmap.raw.size", StringComparison.InvariantCultureIgnoreCase ) )
				{
					mRawSize = Convert.ToInt32( opt.Value );
					rawSizeFound = true;
				}
				else if ( key.StartsWith( "Heightmap.raw.bpp", StringComparison.InvariantCultureIgnoreCase ) )
				{
					mRawBpp = Convert.ToByte( opt.Value );
					if ( mRawBpp < 1 || mRawBpp > 2 )
					{
						throw new AxiomException(
							"Invalid value for 'Heightmap.raw.bpp', must be 1 or 2. HeightmapTerrainZonePageSource.Initialise" );
					}
					rawBppFound = true;
				}
				else if ( key.StartsWith( "Heightmap.flip", StringComparison.InvariantCultureIgnoreCase ) )
				{
					mFlipTerrainZone = Convert.ToBoolean( opt.Value );
				}
				else
				{
					LogManager.Instance.Write( "Warning: ignoring unknown Heightmap option '"
						+ key + "'" );
				}
			}
			if ( !imageFound )
			{
				throw new AxiomException( "Missing option 'HeightmapImage'. HeightmapTerrainZonePageSource.Initialise" );
			}
			if ( mIsRaw &&
				( !rawSizeFound || !rawBppFound ) )
			{
				throw new AxiomException(
					"Options 'Heightmap.raw.size' and 'Heightmap.raw.bpp' must be specified for RAW heightmap sources. HeightmapTerrainZonePageSource.Initialise" );
			}
			// Load it!
			LoadHeightmap();
		}
		//-------------------------------------------------------------------------
		public override void RequestPage( ushort x, ushort y )
		{
			// Only 1 page provided
			if ( x == 0 && y == 0 && mPage == null )
			{
				// Convert the image data to unscaled floats
				ulong totalPageSize = (ulong)( mPageSize * mPageSize );
				Real[] heightData = new Real[ totalPageSize ];
				byte[] pOrigSrc, pSrc;
				Real[] pDest = heightData;
				Real invScale;
				bool is16bit = false;

				if ( mIsRaw )
				{
					pOrigSrc = mRawData.GetBuffer();
					is16bit = ( mRawBpp == 2 );
				}
				else
				{
					PixelFormat pf = mImage.Format;
					if ( pf != PixelFormat.L8 && pf != PixelFormat.L16 )
					{
						throw new AxiomException( "Error: Image is not a grayscale image. HeightmapTerrainZonePageSource.RequestPage" );
					}

					pOrigSrc = mImage.Data;
					is16bit = ( pf == PixelFormat.L16 );
				}
				// Determine mapping from fixed to floating
				ulong rowSize;
				if ( is16bit )
				{
					invScale = 1.0f / 65535.0f;
					rowSize = (ulong)mPageSize * 2;
				}
				else
				{
					invScale = 1.0f / 255.0f;
					rowSize = (ulong)mPageSize;
				}
				// Read the data
				pSrc = pOrigSrc;
				//                for ( int j = 0; j < mPageSize; ++j )
				//                {
				//                    if ( mFlipTerrainZone )
				//                    {
				//                        //Array al = Array.CreateInstance(typeof(byte), pSrc.Length);
				//                        //pSrc.CopyTo(al, 0);
				//                        pOrigSrc.CopyTo( pSrc, 0 );
				//                        Array.Reverse( pSrc );
				//                        // Work backwards
				//                        // pSrc = pOrigSrc + (rowSize * (mPageSize - j - 1));
				//                    }
				//                    for ( int i = 0; i < mPageSize; ++i )
				//                    {
				//                        if ( is16bit )
				//                        {
				//#if OGRE_ENDIAN == OGRE_ENDIAN_BIG
				//                            int val = pSrc[ j + i ] << 8;
				//                            val += pSrc[ j + i ];
				//#else
				//                            ushort val = *pSrc++;
				//                            val += *pSrc++ << 8;
				//#endif
				//                            pDest[ j + i ] = new Real( val )*invScale;
				//                        }
				//                        else
				//                        {
				//                            pDest[ j + i ] = new Real( pSrc[ j + i ] )*invScale;
				//                        }
				//                    }
				//                }

				for ( ulong i = 0; i < totalPageSize - 1; i++ )
				{
					float height = pSrc[ i ] * invScale;
					pDest[ i ] = height;
				}

				heightData = pDest;
				// Call listeners
				OnPageConstructed( 0, 0, heightData );
				// Now turn into TerrainZonePage
				// Note that we're using a single material for now
				if ( null != mTerrainZone )
				{
					mPage = BuildPage( heightData,
						mTerrainZone.Options.terrainMaterial );
					mTerrainZone.AttachPage( 0, 0, mPage );
				}

				// Free temp store
				// OGRE_FREE(heightData, MEMCATEGORY_RESOURCE);
			}
		}
		//-------------------------------------------------------------------------
		public void ExpirePage( ushort x, ushort y )
		{
			// Single page
			if ( x == 0 && y == 0 && null != mPage )
			{
				mPage = null;
			}

		}
		//-------------------------------------------------------------------------
	}
}