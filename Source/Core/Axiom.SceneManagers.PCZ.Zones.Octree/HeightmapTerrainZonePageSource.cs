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
//     <id value="$Id$"/>
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
			this.mIsRaw = false;
			this.mFlipTerrainZone = false;
			this.mPage = null;
		}

		//-------------------------------------------------------------------------
		~HeightmapTerrainZonePageSource()
		{
			Shutdown();
		}

		//-------------------------------------------------------------------------
		public override void Shutdown()
		{
			if ( null != this.mImage )
			{
				this.mImage.Dispose();
			}
			this.mPage = null;
		}

		//-------------------------------------------------------------------------
		public void LoadHeightmap()
		{
			int imgSize;
			// Special-case RAW format
			if ( this.mIsRaw )
			{
				// Image size comes from setting (since RAW is not self-describing)
				imgSize = this.mRawSize;

				// Load data
				this.mRawData = null;
				Stream stream = ResourceGroupManager.Instance.OpenResource( this.mSource,
				                                                            ResourceGroupManager.Instance.WorldResourceGroupName );
				var buffer = new byte[stream.Length];
				stream.Read( buffer, 0, (int)stream.Length );
				this.mRawData = new MemoryStream( buffer );

				// Validate size
				int numBytes = imgSize*imgSize*this.mRawBpp;
				if ( this.mRawData.Length != numBytes )
				{
					Shutdown();
					throw new AxiomException( "RAW size (" + this.mRawData.Length +
					                          ") does not agree with configuration settings. HeightmapTerrainZonePageSource.LoadHeightmap" );
				}
			}
			else
			{
				this.mImage =
					Image.FromStream(
						ResourceGroupManager.Instance.OpenResource( this.mSource, ResourceGroupManager.Instance.WorldResourceGroupName ),
						this.mSource.Split( '.' )[ 1 ] );

				// Must be square (dimensions checked later)
				if ( this.mImage.Width != this.mImage.Height )
				{
					Shutdown();
					throw new AxiomException( "Heightmap must be square. HeightmapTerrainZonePageSource.LoadHeightmap" );
				}
				imgSize = this.mImage.Width;
			}
			//check to make sure it's the expected size
			if ( imgSize != mPageSize )
			{
				Shutdown();
				throw new AxiomException( "Error: Invalid heightmap size : " + imgSize + ". Should be " + mPageSize +
				                          "HeightmapTerrainZonePageSource.LoadHeightmap" );
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
			this.mIsRaw = false;
			bool rawSizeFound = false;
			bool rawBppFound = false;
			foreach ( var opt in optionList )
			{
				string key = opt.Key;
				key = key.Trim();
				if ( key.StartsWith( "HeightmapImage".ToLower(), StringComparison.InvariantCultureIgnoreCase ) )
				{
					this.mSource = opt.Value;
					imageFound = true;
					// is it a raw?
					if ( this.mSource.ToLowerInvariant().Trim().EndsWith( "raw" ) )
					{
						this.mIsRaw = true;
					}
				}
				else if ( key.StartsWith( "Heightmap.raw.size", StringComparison.InvariantCultureIgnoreCase ) )
				{
					this.mRawSize = Convert.ToInt32( opt.Value );
					rawSizeFound = true;
				}
				else if ( key.StartsWith( "Heightmap.raw.bpp", StringComparison.InvariantCultureIgnoreCase ) )
				{
					this.mRawBpp = Convert.ToByte( opt.Value );
					if ( this.mRawBpp < 1 || this.mRawBpp > 2 )
					{
						throw new AxiomException(
							"Invalid value for 'Heightmap.raw.bpp', must be 1 or 2. HeightmapTerrainZonePageSource.Initialise" );
					}
					rawBppFound = true;
				}
				else if ( key.StartsWith( "Heightmap.flip", StringComparison.InvariantCultureIgnoreCase ) )
				{
					this.mFlipTerrainZone = Convert.ToBoolean( opt.Value );
				}
				else
				{
					LogManager.Instance.Write( "Warning: ignoring unknown Heightmap option '" + key + "'" );
				}
			}
			if ( !imageFound )
			{
				throw new AxiomException( "Missing option 'HeightmapImage'. HeightmapTerrainZonePageSource.Initialise" );
			}
			if ( this.mIsRaw && ( !rawSizeFound || !rawBppFound ) )
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
			if ( x == 0 && y == 0 && this.mPage == null )
			{
				// Convert the image data to unscaled floats
				var totalPageSize = (ulong)( mPageSize*mPageSize );
				var heightData = new Real[totalPageSize];
				byte[] pOrigSrc, pSrc;
				Real[] pDest = heightData;
				Real invScale;
				bool is16bit = false;

				if ( this.mIsRaw )
				{
					pOrigSrc = this.mRawData.GetBuffer();
					is16bit = ( this.mRawBpp == 2 );
				}
				else
				{
					PixelFormat pf = this.mImage.Format;
					if ( pf != PixelFormat.L8 && pf != PixelFormat.L16 )
					{
						throw new AxiomException( "Error: Image is not a grayscale image. HeightmapTerrainZonePageSource.RequestPage" );
					}

					pOrigSrc = this.mImage.Data;
					is16bit = ( pf == PixelFormat.L16 );
				}
				// Determine mapping from fixed to floating
				ulong rowSize;
				if ( is16bit )
				{
					invScale = 1.0f/65535.0f;
					rowSize = (ulong)mPageSize*2;
				}
				else
				{
					invScale = 1.0f/255.0f;
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
					float height = pSrc[ i ]*invScale;
					pDest[ i ] = height;
				}

				heightData = pDest;
				// Call listeners
				OnPageConstructed( 0, 0, heightData );
				// Now turn into TerrainZonePage
				// Note that we're using a single material for now
				if ( null != mTerrainZone )
				{
					this.mPage = BuildPage( heightData, mTerrainZone.Options.terrainMaterial );
					mTerrainZone.AttachPage( 0, 0, this.mPage );
				}

				// Free temp store
				// OGRE_FREE(heightData, MEMCATEGORY_RESOURCE);
			}
		}

		//-------------------------------------------------------------------------
		public void ExpirePage( ushort x, ushort y )
		{
			// Single page
			if ( x == 0 && y == 0 && null != this.mPage )
			{
				this.mPage = null;
			}
		}

		//-------------------------------------------------------------------------
	}
}