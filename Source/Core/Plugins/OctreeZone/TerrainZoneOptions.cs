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

namespace OctreeZone
{
	public class TerrainZoneOptions
	{
		public TerrainZoneOptions()
		{
			pageSize = 0;
			tileSize = 0;
			tilesPerPage = 0;
			maxGeoMipMapLevel = 0;
			scale = Vector3.UnitScale;
			maxPixelError = 4;
			detailTile = 1;
			lit = false;
			coloured = false;
			lodMorph = false;
			lodMorphStart = 0.5;
			useTriStrips = false;
			primaryCamera = null;
			terrainMaterial = null;
		}

		/// The size of one edge of a terrain page, in vertices
		public int pageSize;

		/// The size of one edge of a terrain tile, in vertices
		public int tileSize;

		/// Precalculated number of tiles per page
		public int tilesPerPage;

		/// The primary camera, used for error metric calculation and page choice
		public Camera primaryCamera;

		/// The maximum terrain geo-mipmap level
		public int maxGeoMipMapLevel;

		/// The scale factor to apply to the terrain (each vertex is 1 unscaled unit
		/// away from the next, and height is from 0 to 1)
		public Vector3 scale;

		/// The maximum pixel error allowed
		public int maxPixelError;

		/// Whether we should use triangle strips
		public bool useTriStrips;

		/// The number of times to repeat a detail texture over a tile
		public int detailTile;

		/// Whether LOD morphing is enabled
		public bool lodMorph;

		/// At what point (parametric) should LOD morphing start
		public Real lodMorphStart;

		/// Whether dynamic lighting is enabled
		public bool lit;

		/// Whether vertex colours are enabled
		public bool coloured;

		/// Pointer to the material to use to render the terrain
		public Material terrainMaterial;
	}
}
