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

using Axiom.Core;
using Axiom.Graphics;
using Axiom.Math;

#endregion Namespace Declarations

/// <summary>
/// Terrain Zone Options
/// </summary>
public class TerrainZoneOptions
{
	private int _pageSize = 0;

	/// <summary>
	/// The size of one edge of a terrain page, in vertices
	/// </summary>
	public int PageSize { get { return _pageSize; } set { _pageSize = value; } }

	private int _tileSize = 0;

	/// <summary>
	/// The size of one edge of a terrain tile, in vertices
	/// </summary>
	public int TileSize { get { return _tileSize; } set { _tileSize = value; } }

	private int _tilesPerPage;

	/// <summary>
	/// Precalculated number of tiles per page
	/// </summary>
	public int TilesPerPage { get { return _tilesPerPage; } set { _tilesPerPage = value; } }

	private Camera _primaryCamera = null;

	/// <summary>
	/// The primary camera, used for error metric calculation and page choice
	/// </summary> 
	public Camera PrimaryCamera { get { return _primaryCamera; } set { _primaryCamera = value; } }

	private int _maxGeoMipMapLevel = 0;

	/// <summary>
	/// The maximum terrain geo-mipmap level
	/// </summary>
	public int MaxGeoMipMapLevel { get { return _maxGeoMipMapLevel; } set { _maxGeoMipMapLevel = value; } }

	public Vector3 _scale = Vector3.UnitScale;

	/// <summary>
	/// The scale factor to apply to the terrain (each vertex is 1 unscaled unit
	/// away from the next, and height is from 0 to 1)
	/// </summary>
	public Vector3 Scale { get { return _scale; } set { _scale = value; } }

	private int _maxPixelError = 4;

	/// <summary>
	/// The maximum pixel error allowed
	/// </summary>
	public int MaxPixelError { get { return _maxPixelError; } set { _maxPixelError = value; } }

	private bool _useTriStrips = false;

	/// <summary>
	/// Whether we should use triangle strips
	/// </summary>
	public bool UseTriStrips { get { return _useTriStrips; } set { _useTriStrips = value; } }

	private int _detailTile = 1;

	/// <summary>
	/// The number of times to repeat a detail texture over a tile
	/// </summary>
	public int DetailTile { get { return _detailTile; } set { _detailTile = value; } }

	private bool _lodMorph = false;

	/// <summary>
	/// Whether LOD morphing is enabled
	/// </summary>
	public bool LodMorph { get { return _lodMorph; } set { _lodMorph = value; } }

	private Real _lodMorphStart = 0.5;

	/// <summary>
	/// At what point (parametric) should LOD morphing start
	/// </summary>
	public Real LodMorphStart { get { return _lodMorphStart; } set { _lodMorphStart = value; } }

	private bool _useDynamicLighting = false;

	/// <summary>
	/// Whether dynamic lighting is enabled
	/// </summary>
	public bool UseDynamicLighting { get { return _useDynamicLighting; } set { _useDynamicLighting = value; } }

	private bool _coloured = false;

	/// <summary>
	/// Whether vertex colors are enabled
	/// </summary>
	public bool Coloured { get { return _coloured; } set { _coloured = value; } }

	private Material _terrainMaterial = null;

	/// <summary>
	/// Pointer to the material to use to render the terrain
	/// </summary>
	public Material terrainMaterial { get { return _terrainMaterial; } set { _terrainMaterial = value; } }

	public TerrainZoneOptions() {}
}
