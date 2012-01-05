#region MIT/X11 License

//Copyright © 2003-2011 Axiom 3D Rendering Engine Project
//
//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:
//
//The above copyright notice and this permission notice shall be included in
//all copies or substantial portions of the Software.
//
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//THE SOFTWARE.

#endregion License

#region Namespace Declarations

using System;
using System.Collections.Generic;

using Axiom.Math;
using Axiom.Core;

#endregion Namespace Declarations

namespace Axiom.Components.Terrain
{
	public class TerrainGlobalOptions
	{
		//no instantiation
		protected TerrainGlobalOptions() {}

		#region - fields -

		protected static string mResourceGroup = ResourceGroupManager.DefaultResourceGroupName;

		/// <summary>
		/// 
		/// </summary>
		protected static float mSkirtSize;

		/// <summary>
		/// 
		/// </summary>
		protected static Vector3 msLightMapDir = Vector3.Zero;

		/// <summary>
		/// 
		/// </summary>
		protected static bool msCastsShadows;

		/// <summary>
		/// 
		/// </summary>
		protected static float msMaxPixelError;

		/// <summary>
		/// 
		/// </summary>
		protected static RenderQueueGroupID msRenderQueueGroup;

		/// <summary>
		/// 
		/// </summary>
		protected static bool msUseRayBoxDistanceCalculation;

		/// <summary>
		/// 
		/// </summary>
		protected static TerrainMaterialGenerator msDefaultMaterialGenerator;

		/// <summary>
		/// 
		/// </summary>
		protected static ushort msLayerBlendMapSize;

		/// <summary>
		/// 
		/// </summary>
		protected static float msDefaultLayerTextureWorldSize;

		/// <summary>
		/// 
		/// </summary>
		protected static ushort msDefaultGlobalColourMapSize;

		/// <summary>
		/// 
		/// </summary>
		protected static ushort msLightmapSize;

		/// <summary>
		/// 
		/// </summary>
		protected static ushort msCompositeMapSize;

		/// <summary>
		/// 
		/// </summary>
		protected static ColorEx msCompositeMapAmbient;

		/// <summary>
		/// 
		/// </summary>
		protected static ColorEx msCompositeMapDiffuse;

		/// <summary>
		/// 
		/// </summary>
		protected static float msCompositeMapDistance;

		/// <summary>
		/// 
		/// </summary>
		protected static uint msVisibililityFlags;

		/// <summary>
		/// 
		/// </summary>
		protected static uint msQueryFlags;

		#endregion

		#region - properties -

		public static string ResourceGroup { get { return mResourceGroup; } set { mResourceGroup = value; } }

		/// <summary>
		/// 
		/// </summary>
		public static uint QueryFlags { get { return msQueryFlags; } set { msQueryFlags = value; } }

		/// <summary>
		/// 
		/// </summary>
		public static uint VisibilityFlags { get { return msVisibililityFlags; } set { msVisibililityFlags = value; } }

		/// <summary>
		/// 
		/// </summary>
		public static ushort LightMapSize { get { return msLightmapSize; } set { msLightmapSize = value; } }

		/// <summary>
		/// Static method - the default size of 'skirts' used to hide terrain cracks
		/// (default 10)
		/// </summary>
		/// <remarks>
		/// Changing this value only applies to Terrain instances loaded / reloaded afterwards.
		/// </remarks>
		public static float SkirtSize { get { return mSkirtSize; } set { mSkirtSize = value; } }

		/// <summary>
		/// Get' or set's the shadow map light direction to use (world space)
		/// </summary>
		public static Vector3 LightMapDirection { get { return msLightMapDir; } set { msLightMapDir = value; } }

		/// <summary>
		/// Whether the terrain will be able to cast shadows (texture shadows
		/// only are supported, and you must be using depth shadow maps).
		/// 
		/// This value can be set dynamically, and affects all existing terrains.
		/// It defaults to false. 
		/// </summary>
		public static bool CastsDynamicShadows { get { return msCastsShadows; } set { msCastsShadows = value; } }

		/// <summary>
		/// Get's or set's  the composite map ambient light to use 
		/// </summary>
		public static ColorEx CompositeMapAmbient { set { msCompositeMapAmbient = value; } get { return msCompositeMapAmbient; } }

		/// <summary>
		/// Get's or set's the composite map iffuse light to use 
		/// </summary>
		public static ColorEx CompositeMapDiffuse { set { msCompositeMapDiffuse = value; } get { return msCompositeMapDiffuse; } }

		/// <summary>
		/// Get' or Set's the maximum screen pixel error that should be allowed when rendering.
		/// </summary>
		public static float MaxPixelError { set { msMaxPixelError = value; } get { return msMaxPixelError; } }

		/// <summary>
		/// Get's or set's the render queue group that this terrain will be rendered into
		/// </summary>
		public static RenderQueueGroupID RenderQueueGroupID { set { msRenderQueueGroup = value; } get { return msRenderQueueGroup; } }

		/// <summary>
		/// Get's - whether or not to use an accurate calculation of camera distance
		///	from a terrain tile (ray / AABB intersection) or whether to use the
		///	simpler distance from the tile centre.
		///	
		/// Set's - whether to use an accurate ray / box intersection to determine
		///	distance from a terrain tile, or whether to use the simple distance
		///	from the tile centre.
		///	Using ray/box intersection will result in higher detail terrain because 
		///	the LOD calculation is more conservative, assuming the 'worst case scenario' 
		///	of a large height difference at the edge of a tile. This is guaranteed to give you at least
		///	the max pixel error or better, but will often give you more detail than
		///	you need. Not using the ray/box method is cheaper but will only use
		///	the max pixel error as a guide, the actual error will vary above and
		///	below that. The default is not to use the ray/box approach.
		/// </summary>
		public static bool IsUseRayBoxDistanceCalculation { get { return msUseRayBoxDistanceCalculation; } set { msUseRayBoxDistanceCalculation = value; } }

		/// <summary>
		///  Get's or set's the default material generator.
		/// </summary>
		public static TerrainMaterialGenerator DefaultMaterialGenerator
		{
			set { msDefaultMaterialGenerator = value; }
			get
			{
				if( msDefaultMaterialGenerator == null )
				{
					//default
					msDefaultMaterialGenerator = new TerrainMaterialGeneratorA();
				}
				return msDefaultMaterialGenerator;
			}
		}

		/// <summary>
		/// Set's  - the default size of blend maps for a new terrain.
		/// This is the resolution of each blending layer for a new terrain. 
		/// Once created, this information will be stored with the terrain.
		/// 
		/// Get's - the default size of the blend maps for a new terrain. 
		/// </summary>
		public static ushort LayerBlendMapSize { set { msLayerBlendMapSize = value; } get { return msLayerBlendMapSize; } }

		/// <summary>
		/// Get's or Set's the default world size for a layer 'splat' texture to cover. 
		/// </summary>
		public static float DefaultLayerTextureWorldSize { set { msDefaultLayerTextureWorldSize = value; } get { return msDefaultLayerTextureWorldSize; } }

		/// <summary>
		/// Get's - the default size of the terrain global colour map for a new terrain.
		/// Set's -the default size of the terrain global colour map for a new terrain. 
		/// Once created, this information will be stored with the terrain. 
		/// </summary>
		public static ushort DefaultGlobalColorMapSize { set { msDefaultGlobalColourMapSize = value; } get { return msDefaultGlobalColourMapSize; } }

		/// <summary>
		/// Get' or set's the default size of the lightmaps for a new terrain. 
		/// </summary>
		public static ushort LightsMapCount { get { return msLightmapSize; } set { msLightmapSize = value; } }

		/// <summary>
		/// Get's or set's the default size of the composite maps for a new terrain. 
		/// </summary>
		public static ushort CompositeMapSize { set { msCompositeMapSize = value; } get { return msCompositeMapSize; } }

		/// <summary>
		///  Get's or set's the distance at which to start using a composite map if present
		/// </summary>
		public static float CompositeMapDistance { get { return msCompositeMapDistance; } set { msCompositeMapDistance = value; } }

		#endregion
	}
}
