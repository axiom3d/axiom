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
using System.Net.Mime;
using System.Text;

using Axiom.Math;
using Axiom.Media;
using Axiom.Core;

#endregion Namespace Declarations

/// <summary>
/// Height map Terrain Zone Page Source
/// </summary>
public class HeightmapTerrainZonePageSource : TerrainZonePageSource
{
	private bool _isRaw = false;

	/// <summary>
	///  Is this input RAW?
	/// </summary>
	protected bool IsRaw { get { return _isRaw; } set { _isRaw = value; } }

	private bool _flipTerrainZone = false;

	/// <summary>
	/// Should we flip terrain vertically?
	/// </summary>
	protected bool FlipTerrainZone { get { return _flipTerrainZone; } set { _flipTerrainZone = value; } }

	private Image _Image = null;

	/// <summary>
	/// Image containing the source height map if loaded from non-RAW
	/// </summary>
	protected Image Image { get { return _Image; } set { _Image = value; } }

	private MemoryStream _rawData = null;

	/// <summary>
	/// Arbitrary data loaded from RAW
	/// </summary>
	protected MemoryStream RawData { get { return _rawData; } set { _rawData = value; } }

	private TerrainZonePage _page = null;

	/// <summary>
	/// The (single) terrain page this source will provide
	/// </summary>
	protected TerrainZonePage Page { get { return _page; } set { _page = value; } }

	private string _source = "";

	/// <summary>
	/// Source file name
	/// </summary>
	protected string Source { get { return _source; } set { _source = value; } }

	private int _rawSize = 0;

	/// <summary>
	/// Manual size if source is RAW
	/// </summary>
	protected int RawSize { get { return _rawSize; } set { _rawSize = value; } }

	private byte _rawBpp;

	/// <summary>
	/// Manual bpp if source is RAW
	/// </summary>
	protected byte RawBpp { get { return _rawBpp; } set { _rawBpp = value; } }

	/// <summary>
	/// Constructor HeightmapTerrainZonePageSource
	/// </summary>
	public HeightmapTerrainZonePageSource() {}

	/// <summary>
	/// Destructor HeightmapTerrainZonePageSource
	/// </summary>
	~HeightmapTerrainZonePageSource()
	{
		Shutdown();
	}

	/// <summary>
	/// Shutdown
	/// </summary>
	public override void Shutdown()
	{
		if( null != Image )
		{
			Image.Dispose();
		}
		Page = null;
	}

	/// <summary>
	/// Load Height map
	/// </summary>
	public void LoadHeightmap()
	{
		int imgSize;
		// Special-case RAW format
		if( IsRaw )
		{
			// Image size comes from setting (since RAW is not self-describing)
			imgSize = RawSize;

			// Load data
			RawData = null;
			Stream stream = ResourceGroupManager.Instance.OpenResource( Source, ResourceGroupManager.Instance.WorldResourceGroupName );
			byte[] buffer = new byte[stream.Length];
			stream.Read( buffer, 0, (int)stream.Length );
			RawData = new MemoryStream( buffer );

			// Validate size
			int numBytes = imgSize * imgSize * RawBpp;
			if( RawData.Length != numBytes )
			{
				Shutdown();
				throw new AxiomException( "RAW size ({0}) does not agree with configuration settings. HeightmapTerrainZonePageSource.LoadHeightmap", RawData.Length );
			}
		}
		else
		{
			Image =
				Image.FromStream( ResourceGroupManager.Instance.OpenResource( Source,
				                                                              ResourceGroupManager.Instance.
				                                                              	WorldResourceGroupName ), Source.Split( '.' )[ 1 ] );

			// Must be square (dimensions checked later)
			if( Image.Width != Image.Height )
			{
				Shutdown();
				throw new AxiomException( "Heightmap must be square. HeightmapTerrainZonePageSource.LoadHeightmap" );
			}
			imgSize = Image.Width;
		}
		//check to make sure it's the expected size
		if( imgSize != PageSize )
		{
			Shutdown();
			throw new AxiomException( "Error: Invalid heightmap size : {0}. Should be {1} HeightmapTerrainZonePageSource.LoadHeightmap", imgSize, PageSize );
		}
	}

	/// <summary>
	/// Initialize
	/// </summary>
	/// <param name="tsm">TerrainZone</param>
	/// <param name="tileSize">int</param>
	/// <param name="pageSize">int</param>
	/// <param name="asyncLoading">bool</param>
	/// <param name="optionList">TerrainZonePageSourceOptionList</param>
	public override void Initialize( TerrainZone tsm, int tileSize, int pageSize, bool asyncLoading,
	                                 TerrainZonePageSourceOptionList optionList )
	{
		// Shutdown to clear any previous data
		Shutdown();

		base.Initialize( tsm, tileSize, pageSize, asyncLoading, optionList );

		// Get source image

		bool imageFound = false;
		IsRaw = false;
		bool rawSizeFound = false;
		bool rawBppFound = false;
		foreach( KeyValuePair<string, string> opt in optionList )
		{
			string key = opt.Key;
			key = key.Trim();
			if( key.StartsWith( "HeightmapImage".ToLower(), StringComparison.InvariantCultureIgnoreCase ) )
			{
				Source = opt.Value;
				imageFound = true;
				// is it a raw?
				if( Source.ToLowerInvariant().Trim().EndsWith( "raw" ) )
				{
					IsRaw = true;
				}
			}
			else if( key.StartsWith( "Heightmap.raw.size", StringComparison.InvariantCultureIgnoreCase ) )
			{
				RawSize = Convert.ToInt32( opt.Value );
				rawSizeFound = true;
			}
			else if( key.StartsWith( "Heightmap.raw.bpp", StringComparison.InvariantCultureIgnoreCase ) )
			{
				RawBpp = Convert.ToByte( opt.Value );
				if( RawBpp < 1 || RawBpp > 2 )
				{
					throw new AxiomException(
						"Invalid value for 'Heightmap.raw.bpp', must be 1 or 2. HeightmapTerrainZonePageSource.Initialise" );
				}
				rawBppFound = true;
			}
			else if( key.StartsWith( "Heightmap.flip", StringComparison.InvariantCultureIgnoreCase ) )
			{
				FlipTerrainZone = Convert.ToBoolean( opt.Value );
			}
			else
			{
				LogManager.Instance.Write( "Warning: ignoring unknown Heightmap option '"
				                           + key + "'" );
			}
		}
		if( !imageFound )
		{
			throw new AxiomException( "Missing option 'HeightmapImage'. HeightmapTerrainZonePageSource.Initialise" );
		}
		if( IsRaw &&
		    ( !rawSizeFound || !rawBppFound ) )
		{
			throw new AxiomException(
				"Options 'Heightmap.raw.size' and 'Heightmap.raw.bpp' must be specified for RAW heightmap sources. HeightmapTerrainZonePageSource.Initialise" );
		}
		// Load it!
		LoadHeightmap();
	}

	/// <summary>
	/// Request Page
	/// </summary>
	/// <param name="x">ushort</param>
	/// <param name="y">ushort</param>
	public override void RequestPage( ushort x, ushort y )
	{
		// Only 1 page provided
		if( x == 0 && y == 0 && Page == null )
		{
			// Convert the image data to unscaled floats
			ulong totalPageSize = (ulong)( PageSize * PageSize );
			Real[] heightData = new Real[totalPageSize];
			byte[] pOrigSrc, pSrc;
			Real[] pDest = heightData;
			Real invScale;
			bool is16bit = false;

			if( IsRaw )
			{
				pOrigSrc = RawData.GetBuffer();
				is16bit = ( RawBpp == 2 );
			}
			else
			{
				PixelFormat pf = Image.Format;
				if( pf != PixelFormat.L8 && pf != PixelFormat.L16 )
				{
					throw new AxiomException( "Error: Image is not a grayscale image. HeightmapTerrainZonePageSource.RequestPage" );
				}

				pOrigSrc = Image.Data;
				is16bit = ( pf == PixelFormat.L16 );
			}
			// Determine mapping from fixed to floating
			ulong rowSize;
			if( is16bit )
			{
				invScale = 1.0f / 65535.0f;
				rowSize = (ulong)PageSize * 2;
			}
			else
			{
				invScale = 1.0f / 255.0f;
				rowSize = (ulong)PageSize;
			}
			// Read the data
			pSrc = pOrigSrc;
			for( int j = 0; j < PageSize; ++j )
			{
				if( _flipTerrainZone )
				{
					//Array al = Array.CreateInstance(typeof(byte), pSrc.Length);
					//pSrc.CopyTo(al, 0);
					pOrigSrc.CopyTo( pSrc, 0 );
					Array.Reverse( pSrc );
					// Work backwards
					// pSrc = pOrigSrc + (rowSize * (mPageSize - j - 1));
				}
				for( int i = 0; i < PageSize; ++i )
				{
					if( is16bit )
					{
#if OGRE_ENDIAN == OGRE_ENDIAN_BIG
						int val = pSrc[ j + i ] << 8;
						val += pSrc[ j + i ];
#else
                                        ushort val = *pSrc++;
                                        val += *pSrc++ << 8;
#endif
						pDest[ j + i ] = new Real( val ) * invScale;
					}
					else
					{
						pDest[ j + i ] = new Real( pSrc[ j + i ] ) * invScale;
					}
				}
			}

			for( ulong i = 0; i < totalPageSize - 1; i++ )
			{
				float height = pSrc[ i ] * invScale;
				pDest[ i ] = height;
			}

			heightData = pDest;
			// Call listeners
			OnPageConstructed( 0, 0, heightData );
			// Now turn into TerrainZonePage
			// Note that we're using a single material for now
			if( null != TerrainZone )
			{
				Page = BuildPage( heightData, TerrainZone.Options.terrainMaterial );
				TerrainZone.AttachPage( 0, 0, Page );
			}
		}
	}

	/// <summary>
	/// ExpirePage
	/// </summary>
	/// <param name="x">ushort</param>
	/// <param name="y">ushort</param>
	public void ExpirePage( ushort x, ushort y )
	{
		// Single page
		if( x == 0 && y == 0 && null != Page )
		{
			Page = null;
		}
	}
}
