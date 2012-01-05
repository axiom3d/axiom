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
using System.Text;

using Axiom.Core;
using Axiom.Graphics;
using Axiom.Math;

using System.IO;

using Axiom.SceneManagers.PortalConnected;

#endregion Namespace Declarations

/// <summary>
/// TerrainZonePageConstructedEventArgs
/// </summary>
public class TerrainZonePageConstructedEventArgs : EventArgs
{
	private int _pageX = 0;

	/// <summary>
	/// PageX
	/// </summary>
	public int PageX { get { return _pageX; } set { _pageX = value; } }

	private int _pageZ = 0;

	/// <summary>
	/// PageZ
	/// </summary>
	public int PageZ { get { return _pageZ; } set { _pageZ = value; } }

	private Real[] _heightData;

	/// <summary>
	/// HeightData
	/// </summary>
	public Real[] HeightData { get { return _heightData; } set { _heightData = value; } }

	/// <summary>
	/// TerrainZonePageConstructedEventArgs
	/// </summary>
	/// <param name="pagex">int</param>
	/// <param name="pagez">int</param>
	/// <param name="heightData">List<Real></param>
	public TerrainZonePageConstructedEventArgs( int pagex, int pagez, Real[] heightData )
	{
		_pageX = pagex;
		_pageZ = pagez;
		_heightData = heightData;
	}
}

/// <summary>
/// TerrainZonePageSource
/// </summary>
public class TerrainZonePageSource
{
	/// <summary>
	/// TerrainZonePageConstructedEventHandler
	/// </summary>
	/// <param name="sender">object</param>
	/// <param name="te">TerrainZonePageConstructedEventArgs</param>
	public delegate void TerrainZonePageConstructedEventHandler( object sender, TerrainZonePageConstructedEventArgs te );

	/// <summary>
	/// TerrainZonePageConstructedEventHandler (PageConstructed)
	/// </summary>
	public event TerrainZonePageConstructedEventHandler PageConstructed;

	private TerrainZone _terrainZone = null;

	/// <summary>
	/// Link back to parent manager
	/// </summary>
	protected TerrainZone TerrainZone { get { return _terrainZone; } set { _terrainZone = value; } }

	private bool _asyncLoading = false;

	/// <summary>
	/// Has asynchronous loading been requested?
	/// </summary>
	protected bool mAsyncLoading { get { return _asyncLoading; } set { _asyncLoading = value; } }

	private int _pageSize = 0;

	/// <summary>
	/// The expected size of the page in number of vertices
	/// </summary>
	protected int PageSize { get { return _pageSize; } set { _pageSize = value; } }

	private int _tileSize = 0;

	/// <summary>
	/// The expected size of a tile in number of vertices
	/// </summary>
	protected int mTileSize { get { return _tileSize; } set { _tileSize = value; } }

	/// <summary>
	/// Initialize
	/// </summary>
	/// <param name="tz">TerrainZone</param>
	/// <param name="tileSize">int</param>
	/// <param name="pageSize">int</param>
	/// <param name="asyncLoading">bool</param>
	/// <param name="optionList">TerrainZonePageSourceOptionList</param>
	virtual public void Initialize( TerrainZone tz,
	                                int tileSize, int pageSize, bool asyncLoading,
	                                TerrainZonePageSourceOptionList optionList )
	{
		_terrainZone = tz;
		_tileSize = tileSize;
		_pageSize = pageSize;
		_asyncLoading = asyncLoading;
	}

	/// <summary>
	/// BuildPage
	/// </summary>
	/// <param name="heightData">Real[]</param>
	/// <param name="material">Material</param>
	/// <returns>TerrainZonePage</returns>
	virtual public TerrainZonePage BuildPage( Real[] heightData, Material material )
	{
		string name;

		// Create a TerrainZone Page
		TerrainZonePage page = new TerrainZonePage( (ushort)( ( _pageSize - 1 ) / ( _tileSize - 1 ) ) );
		// Create a node for all tiles to be attached to
		// Note we sequentially name since page can be attached at different points
		// so page x/z is not appropriate
		int pageIndex = _terrainZone.PageCount;
		name = _terrainZone.Name + "_page[";
		name += pageIndex + "]_Node";
		if( _terrainZone.PCZSM.HasSceneNode( name ) )
		{
			page.PageSceneNode = _terrainZone.PCZSM.GetSceneNode( name );
			// set the home zone of the scene node to the terrain zone
			( (PCZSceneNode)( page.PageSceneNode ) ).AnchorToHomeZone( _terrainZone );
			// EXPERIMENTAL - prevent terrain zone pages from visiting other zones
			( (PCZSceneNode)( page.PageSceneNode ) ).AllowToVisit = false;
		}
		else
		{
			page.PageSceneNode = _terrainZone.TerrainRootNode.CreateChildSceneNode( name );
			// set the home zone of the scene node to the terrain zone
			( (PCZSceneNode)( page.PageSceneNode ) ).AnchorToHomeZone( _terrainZone );
			// EXPERIMENTAL - prevent terrain zone pages from visiting other zones
			( (PCZSceneNode)( page.PageSceneNode ) ).AllowToVisit = false;
		}

		int q = 0;
		for( int j = 0; j < _pageSize - 1; j += ( _tileSize - 1 ) )
		{
			int p = 0;

			for( int i = 0; i < _pageSize - 1; i += ( _tileSize - 1 ) )
			{
				// Create scene node for the tile and the TerrainZoneRenderable
				name = _terrainZone.Name + "_tile[" + pageIndex + "][" + p + "," + q + "]_Node";

				SceneNode c;
				if( _terrainZone.PCZSM.HasSceneNode( name ) )
				{
					c = _terrainZone.PCZSM.GetSceneNode( name );
					if( c.Parent != page.PageSceneNode )
					{
						page.PageSceneNode.AddChild( c );
					}
					// set the home zone of the scene node to the terrainzone
					( (PCZSceneNode)c ).AnchorToHomeZone( _terrainZone );
					// EXPERIMENTAL - prevent terrain zone pages from visiting other zones
					( (PCZSceneNode)c ).AllowToVisit = false;
				}
				else
				{
					c = page.PageSceneNode.CreateChildSceneNode( name );
					// set the home zone of the scene node to the terrainzone
					( (PCZSceneNode)c ).AnchorToHomeZone( _terrainZone );
					// EXPERIMENTAL - prevent terrain zone pages from visiting other zones
					( (PCZSceneNode)c ).AllowToVisit = false;
				}

				TerrainZoneRenderable tile = new TerrainZoneRenderable( name, _terrainZone );
				// set queue
				tile.RenderQueueGroup = _terrainZone.PCZSM.WorldGeometryRenderQueueId;
				// Initialise the tile
				tile.Material = material;
				tile.Initialize( i, j, heightData );
				// Attach it to the page
				page.Tiles[ p ][ q ] = tile;
				// Attach it to the node
				c.AttachObject( tile );
				p++;
			}

			q++;
		}

		pageIndex++;

		// calculate neighbours for page
		page.LinkNeighbours();

		if( _terrainZone.Options.UseDynamicLighting )
		{
			q = 0;
			for( int j = 0; j < _pageSize - 1; j += ( _tileSize - 1 ) )
			{
				int p = 0;

				for( int i = 0; i < _pageSize - 1; i += ( _tileSize - 1 ) )
				{
					page.Tiles[ p ][ q ].CalculateNormals();
					p++;
				}
				q++;
			}
		}

		return page;
	}

	virtual public void Shutdown() {}

	virtual public void RequestPage( ushort x, ushort z ) {}

	protected void OnPageConstructed( int x, int z, Real[] data )
	{
		object o = PageConstructed;
		if( null != o )
		{
			PageConstructed( this, new TerrainZonePageConstructedEventArgs( x, z, data ) );
		}
	}
}
