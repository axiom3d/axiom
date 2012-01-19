#region MIT/X11 License
//Copyright © 2003-2012 Axiom 3D Rendering Engine Project
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

using Axiom.Core;
using Axiom.Math;

#endregion Namespace Declarations

namespace Axiom.Components.Terrain
{
    /// <summary>
    /// Options class which just stores default options for the terrain.
    /// </summary>
    /// <remarks>
    /// None of these options are stored with the terrain when saved. They are
    /// options that you can use to modify the behaviour of the terrain when it
    /// is loaded or created. 
    /// @note
    /// You should construct a single instance of this class per application and
    /// do so before you start working with any other terrain classes.
    /// </remarks>
    public class TerrainGlobalOptions
    {
        //no instantiation
        protected TerrainGlobalOptions() { }

        #region - fields -

        protected static Real mSkirtSize = 30;
        protected static Vector3 msLightMapDir = new Vector3( 1, -1, 0 ).ToNormalized();
        protected static bool msCastsShadows;
        protected static Real msMaxPixelError = 3.0;
        protected static RenderQueueGroupID mRenderQueueGroup = RenderQueueGroupID.Main;
        protected static uint msVisibililityFlags = 0xFFFFFFFF;
        protected static uint mQueryFlags = 0xFFFFFFFF;
        protected static bool mUseRayBoxDistanceCalculation;
        protected static TerrainMaterialGenerator msDefaultMaterialGenerator;
        protected static ushort msLayerBlendMapSize = 1024;
        protected static Real msDefaultLayerTextureWorldSize = 10;
        protected static ushort msDefaultGlobalColourMapSize = 1024;
        protected static ushort msLightmapSize = 1024;
        protected static ushort mCompositeMapSize = 1024;
        protected static ColorEx msCompositeMapAmbient = ColorEx.White;
        protected static ColorEx msCompositeMapDiffuse = ColorEx.White;
        protected static Real msCompositeMapDistance = 4000;
        protected static string mResourceGroup = ResourceGroupManager.DefaultResourceGroupName;

        #endregion - fields -

        #region - properties -

        /// <summary>
        /// Static method - the default size of 'skirts' used to hide terrain cracks
        /// (default 30)
        /// </summary>
        /// <remarks>
        /// Changing this value only applies to Terrain instances loaded / reloaded afterwards.
        /// </remarks>
        public static Real SkirtSize
        {
            [OgreVersion( 1, 7, 2 )]
            get { return mSkirtSize; }

            [OgreVersion( 1, 7, 2 )]
            set { mSkirtSize = value; }
        }

        /// <summary>
        /// Get' or set's the shadow map light direction to use (world space)
        /// </summary>
        public static Vector3 LightMapDirection
        {
            [OgreVersion( 1, 7, 2 )]
            get { return msLightMapDir; }

            [OgreVersion( 1, 7, 2 )]
            set { msLightMapDir = value; }
        }

        /// <summary>
        /// Get's or set's  the composite map ambient light to use 
        /// </summary>
        public static ColorEx CompositeMapAmbient
        {
            [OgreVersion( 1, 7, 2 )]
            get { return msCompositeMapAmbient; }

            [OgreVersion( 1, 7, 2 )]
            set { msCompositeMapAmbient = value; }
        }

        /// <summary>
        /// Get's or set's the composite map diffuse light to use 
        /// </summary>
        public static ColorEx CompositeMapDiffuse
        {
            [OgreVersion( 1, 7, 2 )]
            get { return msCompositeMapDiffuse; }

            [OgreVersion( 1, 7, 2 )]
            set { msCompositeMapDiffuse = value; }
        }

        /// <summary>
        ///  Get's or set's the distance at which to start using a composite map if present
        /// </summary>
        public static Real CompositeMapDistance
        {
            [OgreVersion( 1, 7, 2 )]
            get { return msCompositeMapDistance; }

            [OgreVersion( 1, 7, 2 )]
            set { msCompositeMapDistance = value; }
        }

        /// <summary>
        /// Whether the terrain will be able to cast shadows (texture shadows
        /// only are supported, and you must be using depth shadow maps).
        /// 
        /// This value can be set dynamically, and affects all existing terrains.
        /// It defaults to false. 
        /// </summary>
        public static bool CastsDynamicShadows
        {
            [OgreVersion( 1, 7, 2 )]
            get { return msCastsShadows; }

            [OgreVersion( 1, 7, 2 )]
            set { msCastsShadows = value; }
        }

        /// <summary>
        /// Get' or Set's the maximum screen pixel error that should be allowed when rendering.
        /// </summary>
        public static Real MaxPixelError
        {
            [OgreVersion( 1, 7, 2 )]
            get { return msMaxPixelError; }

            [OgreVersion( 1, 7, 2 )]
            set { msMaxPixelError = value; }
        }

        /// <summary>
        /// Get's or set's the render queue group that this terrain will be rendered into
        /// </summary>
        public static RenderQueueGroupID RenderQueueGroup
        {
            [OgreVersion( 1, 7, 2 )]
            get { return mRenderQueueGroup; }

            [OgreVersion( 1, 7, 2 )]
            set { mRenderQueueGroup = value; }
        }

        /// <summary>
        /// Get/set the visbility flags that terrains will be rendered with
        /// </summary>
        public static uint VisibilityFlags
        {
            [OgreVersion( 1, 7, 2 )]
            get { return msVisibililityFlags; }

            [OgreVersion( 1, 7, 2 )]
            set { msVisibililityFlags = value; }
        }

        /// <summary>
        /// Get/Set the default query flags for terrains.
        /// </summary>
        public static uint QueryFlags
        {
            [OgreVersion( 1, 7, 2 )]
            get { return mQueryFlags; }

            [OgreVersion( 1, 7, 2 )]
            set { mQueryFlags = value; }
        }

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
        public static bool UseRayBoxDistanceCalculation
        {
            [OgreVersion( 1, 7, 2 )]
            get { return mUseRayBoxDistanceCalculation; }

            [OgreVersion( 1, 7, 2 )]
            set { mUseRayBoxDistanceCalculation = value; }
        }

        /// <summary>
        ///  Get's or set's the default material generator.
        /// </summary>
        public static TerrainMaterialGenerator DefaultMaterialGenerator
        {
            [OgreVersion( 1, 7, 2 )]
            get
            {
                if ( msDefaultMaterialGenerator == null )
                {
                    //default
                    msDefaultMaterialGenerator = new TerrainMaterialGeneratorA();
                }
                return msDefaultMaterialGenerator;
            }

            [OgreVersion( 1, 7, 2 )]
            set
            {
                msDefaultMaterialGenerator = value;
            }
        }

        /// <summary>
        /// Set's  - the default size of blend maps for a new terrain.
        /// This is the resolution of each blending layer for a new terrain. 
        /// Once created, this information will be stored with the terrain.
        /// 
        /// Get's - the default size of the blend maps for a new terrain. 
        /// </summary>
        public static ushort LayerBlendMapSize
        {
            [OgreVersion( 1, 7, 2 )]
            get { return msLayerBlendMapSize; }

            [OgreVersion( 1, 7, 2 )]
            set { msLayerBlendMapSize = value; }
        }

        /// <summary>
        /// Get's or Set's the default world size for a layer 'splat' texture to cover. 
        /// </summary>
        public static Real DefaultLayerTextureWorldSize
        {
            [OgreVersion( 1, 7, 2 )]
            get { return msDefaultLayerTextureWorldSize; }

            [OgreVersion( 1, 7, 2 )]
            set { msDefaultLayerTextureWorldSize = value; }
        }

        /// <summary>
        /// Get's - the default size of the terrain global colour map for a new terrain.
        /// Set's -the default size of the terrain global colour map for a new terrain. 
        /// Once created, this information will be stored with the terrain. 
        /// </summary>
        public static ushort DefaultGlobalColorMapSize
        {
            [OgreVersion( 1, 7, 2 )]
            get { return msDefaultGlobalColourMapSize; }

            [OgreVersion( 1, 7, 2 )]
            set { msDefaultGlobalColourMapSize = value; }
        }

        /// <summary>
        /// Get/Set the default size of the lightmaps for a new terrain.
        /// </summary>
        public static ushort LightMapSize
        {
            [OgreVersion( 1, 7, 2 )]
            get { return msLightmapSize; }

            [OgreVersion( 1, 7, 2 )]
            set { msLightmapSize = value; }
        }

        /// <summary>
        /// Get's or set's the default size of the composite maps for a new terrain. 
        /// </summary>
        public static ushort CompositeMapSize
        {
            [OgreVersion( 1, 7, 2 )]
            get { return mCompositeMapSize; }

            [OgreVersion( 1, 7, 2 )]
            set { mCompositeMapSize = value; }
        }

        /// <summary>
        /// Get/Set the default resource group to use to load / save terrains.
        /// </summary>
        public static string DefaultResourceGroup
        {
            [OgreVersion( 1, 7, 2 )]
            get { return mResourceGroup; }

            [OgreVersion( 1, 7, 2 )]
            set { mResourceGroup = value; }
        }

        #endregion - properties -

        /// <summary>
        /// As setQueryFlags, except the flags passed as parameters are appended to the existing flags on this object.
        /// </summary>
        [OgreVersion( 1, 7, 2 )]
        public static void AddQueryFlags( uint flags )
        {
            mQueryFlags |= flags;
        }

        /// <summary>
        /// As setQueryFlags, except the flags passed as parameters are removed from the existing flags on this object.
        /// </summary>
        [OgreVersion( 1, 7, 2 )]
        public static void RemoveQueryFlags( uint flags )
        {
            mQueryFlags &= ~flags;
        }
    };
}