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
using Axiom.Core;
using Axiom.Core.Collections;
using Axiom.CrossPlatform;
using Axiom.Graphics;
using Axiom.Math;
using Axiom.Media;
using Axiom.Serialization;

#endregion Namespace Declarations

namespace Axiom.Components.Terrain
{
    /// <summary>
    /// A data holder for communicating with the background derived data update
    /// </summary>
    public struct DerivedDataRequest
    {
        public Terrain terrain;
        public byte TypeMask;
        public Rectangle DirtyRect;
        public Rectangle LightmapExtraDirtyRect;
    }

    /// <summary>
    ///  A data holder for communicating with the background derived data update
    /// </summary>
    public struct DerivedDataResponse
    {
        public Terrain Terrain;
        public byte RemainingTypeMask;
        public Rectangle DeltaUpdateRect;
        public Rectangle NormalUpdateRect;
        public Rectangle LightMapUpdateRect;
        public PixelBox NormalMapBox;
        public PixelBox LightMapPixelBox;
    }

    /// <summary>
    /// An instance of a layer, with specific texture names
    /// </summary>
    public struct LayerInstance
    {
        /// <summary>
        /// The world size of the texture to be applied in this layer
        /// </summary>
        public Real WorldSize;

        /// <summary>
        /// List of texture names to import; must match with TerrainLayerDeclaration
        /// </summary>
        public List<string> TextureNames;
    }

    /// <summary>
    ///  The alignment of the terrain
    /// </summary>
    public enum Alignment
    {
        /// <summary>
        /// Terrain is in the X/Z plane
        /// </summary>
        Align_X_Z = 0,

        /// <summary>
        /// Terrain is in the X/Y plane
        /// </summary>
        Align_X_Y = 1,

        /// <summary>
        /// Terrain is in the Y/Z plane
        /// </summary>
        Align_Y_Z = 2
    }

    /// <summary>
    /// Enumeration of relative spaces that you might want to use to address the terrain
    /// </summary>
    public enum Space
    {
        /// <summary>
        ///  Simple global world space, axes and positions are all in world space
        /// </summary>
        WorldSpace = 0,
        /// <summary>
        /// As world space, but positions are relative to the terrain world position
        /// </summary>
        LocalSpace = 1,
        /// <summary>
        /// x & y are parametric values on the terrain from 0 to 1, with the
        /// origin at the bottom left. z is the world space height at that point.
        /// </summary>
        TerrainSpace = 2,
        /// <summary>
        /// x & y are integer points on the terrain from 0 to size-1, with the
        /// origin at the bottom left. z is the world space height at that point.
        /// </summary>
        PointSpace = 3
    }

    /// <summary>
    /// Neighbour index enumeration - indexed anticlockwise from East like angles
    /// </summary>
    public enum NeighbourIndex
    {
        East = 0,
        NorthEast = 1,
        North = 2,
        NorthWest = 3,
        West = 4,
        SouthWest = 5,
        South = 6,
        SouthEast = 7,
        Count = 8
    }

    /// <summary>
    /// Structure encapsulating import data that you may use to bootstrap 
    /// the terrain without loading from a native data stream. 
    /// </summary>
    public class ImportData
    {
        /// <summary>
        /// The alignment of the terrain
        /// </summary>
        public Alignment TerrainAlign;
        /// <summary>
        /// Terrain size (along one edge) in vertices; must be 2^n+1
        /// </summary>
        public ushort TerrainSize;
        /// <summary>
        /// Maximum batch size (along one pubedge) in vertices; must be 2^n+1 and <= 65
        /// <remarks>
        /// The terrain will be divided into hierarchical tiles, and this is the maximum
        ///	size of one tile in vertices (at any LOD).
        /// </remarks>
        /// </summary>
        public ushort MaxBatchSize;
        /// <summary>
        /// Minimum batch size (along one edge) in vertices; must be 2^n+1.
        /// <remarks>
        /// The terrain will be divided into tiles, and this is the minimum
        ///	size of one tile in vertices (at any LOD). Adjacent tiles will be
        ///	collected together into one batch to drop LOD levels once they are individually at this minimum,
        ///	so setting this value higher means greater batching at the expense
        ///	of making adjacent tiles use a common LOD.
        ///	Once the entire terrain is collected together into one batch this 
        ///	effectively sets the minimum LOD.
        /// </remarks>
        /// </summary>
        public ushort MinBatchSize;
        /// <summary>
        /// Position of the terrain.
        /// <remarks>
        /// Represents the position of the centre of the terrain.
        /// </remarks>
        /// </summary>
        public Vector3 Pos;
        /// <summary>
        ///  The world size of the terrain.
        /// </summary>
        public Real WorldSize;
        /// <summary>
        /// Optional heightmap providing the initial heights for the terrain.
        /// <remarks>
        /// If supplied, should ideally be terrainSize * terrainSize, but if
        /// it isn't it will be resized.
        /// </remarks>
        /// </summary>
        public Image InputImage;
        /// <summary>
        /// Optional list of terrainSize * terrainSize floats defining the terrain. 
        ///	The list of floats wil be interpreted such that the first row
        ///	in the array equates to the bottom row of vertices. 
        /// </summary>
        public float[] InputFloat;
        /// <summary>
        /// How to scale the input values provided (if any)
        /// </summary>
        public Real InputScale;
        /// <summary>
        /// How to bias the input values provided (if any)
        /// </summary>
        public Real InputBias;
        /// <summary>
        /// Definition of the contents of each layer (required).
        ///	Most likely,  you will pull a declaration from a TerrainMaterialGenerator
        ///	of your choice.
        /// </summary>
        public TerrainLayerDeclaration LayerDeclaration;
        /// <summary>
        /// List of layer structures, one for each layer required.
        ///	Can be empty or underfilled if required, list will be padded with
        ///	blank textures.
        /// </summary>
        public List<LayerInstance> LayerList;
        /// <summary>
        /// Definition of the contents of each layer (required).
        /// Most likely,  you will pull a declaration from a TerrainMaterialGenerator
        /// of your choice.
        /// </summary>
        public bool DeleteInputData;
        /// <summary>
        ///  If neither inputImage or inputFloat are supplied, the constant
        ///   height at which the initial terrain should be created (flat).
        /// </summary>
        public float ConstantHeight;

        public ImportData()
        {
            TerrainAlign = Alignment.Align_X_Z;
            TerrainSize = 1025;
            MaxBatchSize = 65;
            MinBatchSize = 17;
            Pos = Vector3.Zero;
            WorldSize = 1000;
            InputImage = null;
            InputFloat = null;
            InputScale = 1.0f;
            InputBias = 0.0f;
            LayerList = new List<LayerInstance>();
            DeleteInputData = false;
            ConstantHeight = 0;
        }
    }

    /// <summary>
    /// The main containing class for a chunk of terrain.
    /// </summary>
    /// <remarks>
    /// 	Terrain can be edited and stored.
    /// The data format for this in a file is:<br/>
    /// <b>TerrainData (Identifier 'TERR')</b>\n
    /// [Version 1]
    /// <table>
    /// <tr>
    /// 	<td><b>Name</b></td>
    /// 	<td><b>Type</b></td>
    /// 	<td><b>Description</b></td>
    /// </tr>
    /// <tr>
    /// 	<td>Terrain orientation</td>
    /// 	<td>uint8</td>
    /// 	<td>The orientation of the terrain; XZ = 0, XY = 1, YZ = 2</td>
    /// </tr>
    /// <tr>
    /// 	<td>Terrain size</td>
    /// 	<td>uint16</td>
    /// 	<td>The number of vertices along one side of the terrain</td>
    /// </tr>
    /// <tr>
    /// 	<td>Terrain world size</td>
    /// 	<td>Real</td>
    /// 	<td>The world size of one side of the terrain</td>
    /// </tr>
    /// <tr>
    /// 	<td>Max batch size</td>
    /// 	<td>uint16</td>
    /// 	<td>The maximum batch size in vertices along one side</td>
    /// </tr>
    /// <tr>
    /// 	<td>Min batch size</td>
    /// 	<td>uint16</td>
    /// 	<td>The minimum batch size in vertices along one side</td>
    /// </tr>
    /// <tr>
    /// 	<td>Position</td>
    /// 	<td>Vector3</td>
    /// 	<td>The location of the centre of the terrain</td>
    /// </tr>
    /// <tr>
    /// 	<td>Height data</td>
    /// 	<td>float[size*size]</td>
    /// 	<td>List of floating point heights</td>
    /// </tr>
    /// <tr>
    /// 	<td>LayerDeclaration</td>
    /// 	<td>LayerDeclaration*</td>
    /// 	<td>The layer declaration for this terrain (see below)</td>
    /// </tr>
    /// <tr>
    /// 	<td>Layer count</td>
    /// 	<td>uint8</td>
    /// 	<td>The number of layers in this terrain</td>
    /// </tr>
    /// <tr>
    /// 	<td>LayerInstance list</td>
    /// 	<td>LayerInstance*</td>
    /// 	<td>A number of LayerInstance definitions based on layer count (see below)</td>
    /// </tr>
    /// <tr>
    /// 	<td>Layer blend map size</td>
    /// 	<td>uint16</td>
    /// 	<td>The size of the layer blend maps as stored in this file</td>
    /// </tr>
    /// <tr>
    /// 	<td>Packed blend texture data</td>
    /// 	<td>uint8*</td>
    /// 	<td>layerCount-1 sets of blend texture data interleaved as either RGB or RGBA 
    /// 		depending on layer count</td>
    /// </tr>
    /// <tr>
    /// 	<td>Optional derived map data</td>
    /// 	<td>TerrainDerivedMap list</td>
    /// 	<td>0 or more sets of map data derived from the original terrain</td>
    /// </tr>
    /// </table>
    /// <b>TerrainLayerDeclaration (Identifier 'TDCL')</b>\n
    /// [Version 1]
    /// <table>
    /// <tr>
    /// 	<td><b>Name</b></td>
    /// 	<td><b>Type</b></td>
    /// 	<td><b>Description</b></td>
    /// </tr>
    /// <tr>
    /// 	<td><b>TerrainLayerSampler Count</b></td>
    /// 	<td><b>uint8</b></td>
    /// 	<td><b>Number of samplers in this declaration</b></td>
    /// </tr>
    /// <tr>
    /// 	<td><b>TerrainLayerSampler List</b></td>
    /// 	<td><b>TerrainLayerSampler*</b></td>
    /// 	<td><b>List of TerrainLayerSampler structures</b></td>
    /// </tr>
    /// <tr>
    /// 	<td><b>Sampler Element Count</b></td>
    /// 	<td><b>uint8</b></td>
    /// 	<td><b>Number of sampler elements in this declaration</b></td>
    /// </tr>
    /// <tr>
    /// 	<td><b>TerrainLayerSamplerElement List</b></td>
    /// 	<td><b>TerrainLayerSamplerElement*</b></td>
    /// 	<td><b>List of TerrainLayerSamplerElement structures</b></td>
    /// </tr>
    /// </table>
    /// <b>TerrainLayerSampler (Identifier 'TSAM')</b>\n
    /// [Version 1]
    /// <table>
    /// <tr>
    /// 	<td><b>Name</b></td>
    /// 	<td><b>Type</b></td>
    /// 	<td><b>Description</b></td>
    /// </tr>
    /// <tr>
    /// 	<td><b>Alias</b></td>
    /// 	<td><b>String</b></td>
    /// 	<td><b>Alias name of this sampler</b></td>
    /// </tr>
    /// <tr>
    /// 	<td><b>Format</b></td>
    /// 	<td><b>uint8</b></td>
    /// 	<td><b>Desired pixel format</b></td>
    /// </tr>
    /// </table>
    /// <b>TerrainLayerSamplerElement (Identifier 'TSEL')</b>\n
    /// [Version 1]
    /// <table>
    /// <tr>
    /// 	<td><b>Name</b></td>
    /// 	<td><b>Type</b></td>
    /// 	<td><b>Description</b></td>
    /// </tr>
    /// <tr>
    /// 	<td><b>Source</b></td>
    /// 	<td><b>uint8</b></td>
    /// 	<td><b>Sampler source index</b></td>
    /// </tr>
    /// <tr>
    /// 	<td><b>Semantic</b></td>
    /// 	<td><b>uint8</b></td>
    /// 	<td><b>Semantic interpretation of this element</b></td>
    /// </tr>
    /// <tr>
    /// 	<td><b>Element start</b></td>
    /// 	<td><b>uint8</b></td>
    /// 	<td><b>Start of this element in the sampler</b></td>
    /// </tr>
    /// <tr>
    /// 	<td><b>Element count</b></td>
    /// 	<td><b>uint8</b></td>
    /// 	<td><b>Number of elements in the sampler used by this entry</b></td>
    /// </tr>
    /// </table>
    /// <b>LayerInstance (Identifier 'TLIN')</b>\n
    /// [Version 1]
    /// <table>
    /// <tr>
    /// 	<td><b>Name</b></td>
    /// 	<td><b>Type</b></td>
    /// 	<td><b>Description</b></td>
    /// </tr>
    /// <tr>
    /// 	<td><b>World size</b></td>
    /// 	<td><b>Real</b></td>
    /// 	<td><b>The world size of this layer (determines UV scaling)</b></td>
    /// </tr>
    /// <tr>
    /// 	<td><b>Texture list</b></td>
    /// 	<td><b>String*</b></td>
    /// 	<td><b>List of texture names corresponding to the number of samplers in the layer declaration</b></td>
    /// </tr>
    /// </table>
    /// <b>TerrainDerivedData (Identifier 'TDDA')</b>\n
    /// [Version 1]
    /// <table>
    /// <tr>
    /// 	<td><b>Name</b></td>
    /// 	<td><b>Type</b></td>
    /// 	<td><b>Description</b></td>
    /// </tr>
    /// <tr>
    /// 	<td><b>Derived data type name</b></td>
    /// 	<td><b>String</b></td>
    /// 	<td><b>Name of the derived data type ('normalmap', 'lightmap', 'colourmap', 'compositemap')</b></td>
    /// </tr>
    /// <tr>
    /// 	<td><b>Size</b></td>
    /// 	<td><b>uint16</b></td>
    /// 	<td><b>Size of the data along one edge</b></td>
    /// </tr>
    /// <tr>
    /// 	<td><b>Data</b></td>
    /// 	<td><b>varies based on type</b></td>
    /// 	<td><b>The data</b></td>
    /// </tr>
    /// </table>
    /// </remarks>
    public class Terrain : DisposableObject
    {
        #region - constants -

        public static uint TERRAIN_CHUNK_ID = StreamSerializer.MakeIdentifier( "TERR" );
        public static ushort TERRAIN_CHUNK_VERSION = 1;
        public static uint TERRAINLAYERDECLARATION_CHUNK_ID = StreamSerializer.MakeIdentifier( "TDCL" );
        public static ushort TERRAINLAYERDECLARATION_CHUNK_VERSION = 1;
        public static uint TERRAINLAYERSAMPLER_CHUNK_ID = StreamSerializer.MakeIdentifier( "TSAM" );
        public static ushort TERRAINLAYERSAMPLER_CHUNK_VERSION = 1;
        public static uint TERRAINLAYERSAMPLERELEMENT_CHUNK_ID = StreamSerializer.MakeIdentifier( "TSEL" );
        public static ushort TERRAINLAYERSAMPLERELEMENT_CHUNK_VERSION = 1;
        public static uint TERRAINLAYERINSTANCE_CHUNK_ID = StreamSerializer.MakeIdentifier( "TLIN" );
        public static ushort TERRAINLAYERINSTANCE_CHUNK_VERSION = 1;
        public static uint TERRAINDERIVEDDATA_CHUNK_ID = StreamSerializer.MakeIdentifier( "TDDA" );
        public static ushort TERRAINDERIVEDDATA_CHUNK_VERSION = 1;
        // since 129^2 is the greatest power we can address in 16-bit index
        public static ushort TERRAIN_MAX_BATCH_SIZE = 129;
        //static ushort WORKQUEUE_CHANNEL = Root::MAX_USER_WORKQUEUE_CHANNEL + 10;
        public static ushort WORKQUEUE_DERIVED_DATA_REQUEST = 1;
        public static int LOD_MORPH_CUSTOM_PARAM = 1001;
        public static byte DERIVED_DATA_DELTAS = 1;
        public static byte DERIVED_DATA_NORMALS = 2;
        public static byte DERIVED_DATA_LIGHTMAP = 4;
        // This MUST match the bitwise OR of all the types above with no extra bits!
        public static byte DERIVED_DATA_ALL = 7;
        public static ushort WORKQUEUE_CHANNEL;

        #endregion

        #region - fields -

        /// <summary>
        /// The height data (world coords relative to mPos)
        /// </summary>
        protected float[] mHeightData;

        protected Real mWorldSize;
        protected ushort mSize;
        protected ushort mMaxBatchSize;
        protected ushort mMinBatchSize;
        protected Vector3 mPos = Vector3.Zero;
        protected ushort mTreeDepth = 0;

        /// <summary>
        /// Base position in world space, relative to mPos
        /// </summary>
        protected Real mBase;

        /// <summary>
        /// Relationship between one point on the terrain and world size
        /// </summary>
        protected Real mScale;

        protected TerrainLayerDeclaration mLayerDecl;
        protected List<LayerInstance> mLayers = new List<LayerInstance>();
        protected FloatList mLayerUVMultiplier = new FloatList();
        protected Rectangle mDirtyGeometryRect = new Rectangle( 0, 0, 0, 0 );
        protected Rectangle mDirtyDerivedDataRect = new Rectangle( 0, 0, 0, 0 );

        /// <summary>
        /// if another update is requested while one is already running
        /// </summary>
        protected byte mDerivedUpdatePendingMask = 0;

        protected string mMaterialName;
        protected Material mMaterial;
        protected TerrainMaterialGenerator mMaterialGenerator;
        protected long mMaterialGenerationCount = 0;
        protected bool mMaterialDirty = false;
        protected bool mMaterialParamsDirty = false;
        protected ushort mLayerBlendMapSize;
        protected ushort mLayerBlendSizeActual;
        protected List<byte> mCpuBlendMapStorage = new List<byte>();
        protected List<Texture> mBlendTextureList = new List<Texture>();
        protected List<TerrainLayerBlendMap> mLayerBlendMapList = new List<TerrainLayerBlendMap>();
        protected byte[] mCpuColorMapStorage;
        protected ushort mLightmapSizeActual;
        protected byte[] mCpuLightmapStorage;
        protected ushort mCompositeMapSize;
        protected ushort mCompositeMapSizeActual;
        protected byte[] mCpuCompositeMapStorage;
        protected Rectangle mCompositeMapDirtyRect = new Rectangle( 0, 0, 0, 0 );
        protected long mCompositeMapUpdateCountdown = 0;
        protected long mLastMillis = 0;

        protected int mLastViewportHeight = 0;

        /// <summary>
        /// true if the updates included lightmap changes (widen)
        /// </summary>
        protected bool mCompositeMapDirtyRectLightmapUpdate = false;

        protected Material mCompositeMapMaterial;
        protected static NameGenerator<Texture> msBlendTextureGenerator;
        protected static NameGenerator<Texture> msNormalMapNameGenerator;
        protected static NameGenerator<Texture> msLightmapNameGenerator;
        protected static NameGenerator<Texture> msCompositeMapNameGenerator;
        protected bool mNormalMapRequired = false;
        protected bool mLightMapRequired = false;
        protected bool mLightMapShadowsOnly = true;
        protected bool mCompositeMapRequired = false;

        /// <summary>
        /// pending data
        /// </summary>
        protected PixelBox mCpuTerrainNormalMap;

        protected Camera mLastLODCamera;
        protected ulong mLastLODFrame = 0;
        protected Rectangle mLightmapExtraDirtyRect;
        protected Terrain[] mNeighbours = new Terrain[ (int)NeighbourIndex.Count ];
        protected Rectangle mDirtyGeometryRectForNeighbours = new Rectangle( 0, 0, 0, 0 );
        protected Rectangle mDirtyLightmapFromNeighboursRect = new Rectangle( 0, 0, 0, 0 );
        private BufferBase mHeightDataPtr;
        private BufferBase mDeltaDataPtr;
        protected GpuBufferAllocator mCustomGpuBufferAllocator;
        protected DefaultGpuBufferAllocator mDefaultGpuBufferAllocator;

        #endregion

        #region - properties -
        /// <summary>
        /// Query whether a derived data update is in progress or not.
        /// </summary>
        public bool IsDerivedDataUpdateInProgress
        {
            get;
            protected set;
        }

        /// <summary>
        /// Get the AABB (world coords) of the entire terrain
        /// </summary>
        public AxisAlignedBox WorldAABB
        {
            get
            {
                Matrix4 m = Matrix4.Identity;
                AxisAlignedBox ret = AABB;
                ret.Transform( m );
                return ret;
            }
        }

        public string DerivedResourceGroup
        {
            get
            {
                if ( string.IsNullOrEmpty( this.ResourceGroup ) )
                    return TerrainGlobalOptions.ResourceGroup;
                else
                    return this.ResourceGroup;
            }
        }

        /// <summary>
        /// Set the default resource group to use to load / save terrains.
        /// </summary>
        public string ResourceGroup
        {
            get;
            set;
        }

        /// <summary>
        /// Returns whether this terrain has been modified since it was first loaded / defined.
        /// </summary>
        /// <remarks>This flag is reset on save().</remarks>
        public bool IsModified
        {
            get;
            protected set;
        }

        /// <summary>
        /// Returns whether terrain heights have been modified since the terrain was first loaded / defined. 
        /// </summary>
        /// <remarks>This flag is reset on save().</remarks>
        public bool IsHeightDataModified
        {
            get;
            protected set;
        }

        /// <summary>
        /// Return whether the terrain is loaded. 
        /// </summary>
        public bool IsLoaded
        {
            get;
            protected set;
        }

        /// <summary>
        /// Get's or set's the visbility flags that terrains will be rendered with
        /// </summary>
        public uint VisibilityFlags
        {
            get;
            set;
        }

        /// <summary>
        /// Get or set the query flags for this terrain.
        /// </summary>
        public uint QueryFlags
        {
            get;
            set;
        }

        /// <summary>
        /// Get's the scenemanager of the terrain
        /// </summary>
        public SceneManager SceneManager
        {
            get;
            protected set;
        }

        /// <summary>
        /// Get/Set the current buffer allocator
        /// </summary>
        /// <remarks>
        /// Setter may only be called when the terrain is not loaded.
        /// </remarks>
        public GpuBufferAllocator GpuBufferAllocator
        {
            [OgreVersion( 1, 7, 2 )]
            get
            {
                if ( mCustomGpuBufferAllocator != null )
                    return mCustomGpuBufferAllocator;
                else
                    return mDefaultGpuBufferAllocator;
            }

            [OgreVersion( 1, 7, 2 )]
            set
            {
                if ( this.GpuBufferAllocator != value )
                {
                    if ( IsLoaded )
                        throw new AxiomException( "Cannot alter the allocator when loaded!" );

                    mCustomGpuBufferAllocator = value;
                }
            }
        }

        //public int PositionBufVertexSize
        //{
        //    [OgreVersion( 1, 7, 2 )]
        //    get
        //    {
        //        int sz = 0;
        //        // float3 position
        //        // TODO we can compress this when shaders in use if we use parametric positioning
        //        sz += sizeof( float ) * 3;
        //        // float2 uv
        //        // TODO we can omit these where shaders are being used & calculate
        //        sz += sizeof( float ) * 2;

        //        return sz;
        //    }
        //}

        private VertexDeclaration _posDecl;
        public VertexDeclaration PositionVertexDecl
        {
            get
            {
                if ( _posDecl == null )
                {
                    _posDecl = new VertexDeclaration();
                    _posDecl.AddElement( TerrainQuadTreeNode.POSITION_BUFFER, 0, VertexElementType.Float3, VertexElementSemantic.Position );
                    _posDecl.AddElement( TerrainQuadTreeNode.POSITION_BUFFER, VertexElement.GetTypeSize( VertexElementType.Float3 ), VertexElementType.Float2, VertexElementSemantic.TexCoords, 0 );
                }

                return _posDecl;
            }
        }

        //public int DeltaBufVertexSize
        //{
        //    [OgreVersion( 1, 7, 2 )]
        //    get
        //    {
        //        return sizeof( float ) * 2;
        //    }
        //}

        private VertexDeclaration _deltaDecl;
        public VertexDeclaration DeltaVertexDecl
        {
            get
            {
                if ( _deltaDecl == null )
                {
                    _deltaDecl = new VertexDeclaration();
                    _deltaDecl.AddElement( TerrainQuadTreeNode.DELTA_BUFFER, 0, VertexElementType.Float2, VertexElementSemantic.TexCoords, 1 );
                }

                return _deltaDecl;
            }
        }

        /// <summary>
        /// Get a pointer to all the delta data for this terrain.
        /// </summary>
        /// <remarks>
        /// The delta data is a measure at a given vertex of by how much vertically
        ///	a vertex will have to move to reach the point at which it will be
        ///	removed in the next lower LOD.
        /// </remarks>
        public BufferBase DeltaData
        {
            get
            {
                return mDeltaDataPtr;
            }
        }

        /// <summary>
        /// Get the requested size of the blend maps used to blend between layers
        ///	for this terrain. 
        ///	Note that where hardware limits this, the actual blend maps may be lower
        ///	resolution. This option is derived from TerrainGlobalOptions when the
        ///	terrain is created.
        /// </summary>
        public ushort LayerBlendMapSize
        {
            get { return mLayerBlendMapSize; }
        }

        /// <summary>
        /// Get'S the AABB (local coords) of the entire terrain
        /// </summary>
        public AxisAlignedBox AABB
        {
            get
            {
                if ( this.QuadTree == null )
                    return null;
                else
                    return this.QuadTree.AABB;
            }
        }

        /// <summary>
        /// Get the size of the terrain in vertices along one side
        /// </summary>
        public ushort Size
        {
            get { return mSize; }
            set
            {
                if ( mSize != value )
                {
                    WaitForDerivedProcesses();
                    int numVertices = value * value;
                    PixelBox src = new PixelBox( mSize, mSize, 1, PixelFormat.FLOAT32_R, GetHeightData( 0, 0 ) );
                    throw new NotImplementedException();
                }
            }
        }

        /// <summary>
        /// Get the root scene node for the terrain (internal use only)
        /// </summary>
        public SceneNode RootSceneNode
        {
            get;
            protected set;
        }

        /// <summary>
        /// Get the alignment of the terrain
        /// </summary>
        public Alignment Alignment
        {
            get;
            protected set;
        }

        /// <summary>
        /// Get the minimum size in vertices along one side of a batch
        /// </summary>
        public ushort MinBatchSize
        {
            get { return mMinBatchSize; }
        }

        /// <summary>
        /// Get the maximum size in vertices along one side of a batch
        /// </summary>
        public ushort MaxBatchSize
        {
            get { return mMaxBatchSize; }
        }

        /// <summary>
        /// Get the size of the terrain in world units
        /// </summary>
        public Real WorldSize
        {
            get { return mWorldSize; }
            set
            {
                if ( mWorldSize != value )
                {
                    WaitForDerivedProcesses();
                    mWorldSize = value;
                    UpdateBaseScale();

                    mMaterialParamsDirty = true;

                    if ( this.IsLoaded )
                    {
                        Rectangle dRect = new Rectangle( 0, 0, mSize, mSize );
                        DirtyRect( dRect );
                        Update();
                    }
                    throw new NotImplementedException();

                }
            }
        }

        /// <summary>
        /// The default size of 'skirts' used to hide terrain cracks
        ///	(default 10, set for new Terrain using TerrainGlobalOptions)
        /// </summary>
        public Real SkirtSize
        {
            get;
            protected set;
        }

        /// <summary>
        /// Get's the minimum height of the terrain
        /// </summary>
        public float MinHeight
        {
            get
            {
                if ( this.QuadTree == null )
                    return 0;
                else
                    return this.QuadTree.MinHeight;
            }
        }

        /// <summary>
        /// Get's the maximum height of the terrain.
        /// </summary>
        public float MaxHeight
        {
            get
            {
                if ( this.QuadTree == null )
                    return 0;
                else
                    return this.QuadTree.MaxHeight;
            }
        }

        /// <summary>
        /// Get's the bounding radius of the entire terrain
        /// </summary>
        public Real BoundingRadius
        {
            get
            {
                if ( this.QuadTree == null )
                    return 0;
                else
                    return this.QuadTree.BoundingRadius;
            }
        }

        /// <summary>
        /// Get's the material being used for the terrain
        /// </summary>
        public Material Material
        {
            [OgreVersion( 1, 7, 2 )]
            get
            {
                if ( mMaterial == null ||
                    mMaterialGenerator.ChangeCount != mMaterialGenerationCount ||
                    mMaterialDirty )
                {
                    mMaterial = mMaterialGenerator.Generate( this );
                    mMaterial.Load();
                    if ( mCompositeMapRequired )
                    {
                        mCompositeMapMaterial = mMaterialGenerator.GenerateForCompositeMap( this );
                        mCompositeMapMaterial.Load();
                    }
                    mMaterialGenerationCount = mMaterialGenerator.ChangeCount;
                    mMaterialDirty = false;
                }
                if ( mMaterialParamsDirty )
                {
                    mMaterialGenerator.UpdateParams( mMaterial, this );
                    if ( mCompositeMapRequired )
                        mMaterialGenerator.UpdateParamsForCompositeMap( mCompositeMapMaterial, this );
                    mMaterialParamsDirty = false;
                }

                return mMaterial;
            }
        }

        /// <summary>
        /// Internal getting of material 
        /// </summary>
        internal Material _Material
        {
            [OgreVersion( 1, 7, 2 )]
            get
            {
                return mMaterial;
            }
        }

        /// <summary>
        /// Internal getting of material  for the terrain composite map
        /// </summary>
        internal Material _CompositeMapMaterial
        {
            [OgreVersion( 1, 7, 2 )]
            get { return mCompositeMapMaterial; }
        }

        /// <summary>
        /// Get's the material being used for the terrain composite map
        /// </summary>
        public Material CompositeMapMaterial
        {
            get
            {
                // both materials updated together since they change at the same time
                string matNam = Material.Name;
                return mCompositeMapMaterial;
            }
        }

        /// <summary>
        /// Get's the name of the material being used for the terrain
        /// </summary>
        public string MaterialName
        {
            get { return mMaterialName; }
        }

        /// <summary>
        /// Get a pointer to all the height data for this terrain.
        /// </summary>
        /// <remarks>
        /// The height data is in world coordinates, relative to the position 
        ///	of the terrain.
        /// </remarks>
        /// <returns></returns>
        public float[] HeightData
        {
            get { return mHeightData; }
        }

        /// <summary>
        /// Request internal implementation options for the terrain material to use, 
        ///	in this case vertex morphing information. 
        /// The TerrainMaterialGenerator should call this method to specify the 
        /// options it would like to use when creating a material. Not all the data
        /// is guaranteed to be up to date on return from this method - for example som
        /// maps may be generated in the background. However, on return from this method
        /// all the features that are requested will be referenceable by materials, the
        /// data may just take a few frames to be fully populated.
        /// </summary>
        public bool IsMorphRequired
        {
            get;
            set;
        }

        /// <summary>
        /// Request internal implementation options for the terrain material to use, 
        /// in this case a terrain-wide normal map. 
        /// The TerrainMaterialGenerator should call this method to specify the 
        /// options it would like to use when creating a material. Not all the data
        /// is guaranteed to be up to date on return from this method - for example some
        /// maps may be generated in the background. However, on return from this method
        /// all the features that are requested will be referenceable by materials, the
        /// data may just take a few frames to be fully populated.
        /// </summary>
        [OgreVersion( 1, 7, 2, "Original: name _setNormalMapRquired" )]
        public bool NormalMapRequired
        {
            set
            {
                if ( mNormalMapRequired != value )
                {
                    mNormalMapRequired = value;
                    // Check NPOT textures supported. We have to use NPOT textures to map
                    // texels to vertices directly!
                    if ( !mNormalMapRequired && Root.Instance.RenderSystem
                        .Capabilities.HasCapability( Capabilities.NonPowerOf2Textures ) )
                    {
                        mNormalMapRequired = false;
                        LogManager.Instance.Write( LogMessageLevel.Critical, false,
                            "Terrain: Ignoring request for normal map generation since " +
                            "non-power-of-two texture support is required.", null );
                    }

                    CreateOrDestroyGPUNormalMap();

                    // if we enabled, generate normal maps
                    if ( mNormalMapRequired )
                    {
                        // update derived data for whole terrain, but just normals
                        mDirtyDerivedDataRect = new Rectangle();
                        mDirtyDerivedDataRect.Left = mDirtyDerivedDataRect.Top = 0;
                        mDirtyDerivedDataRect.Right = mDirtyDerivedDataRect.Bottom = mSize;
                        UpdateDerivedData( false, DERIVED_DATA_NORMALS );
                    }
                }
            }
        }

        /// <summary>
        /// Get's or set's the world position of the terrain centre
        /// </summary>
        public Vector3 Position
        {
            get { return mPos; }

            [OgreVersion( 1, 7, 2 )]
            set
            {
                if ( mPos != value )
                {
                    mPos = value;
                    this.RootSceneNode.Position = mPos;
                    UpdateBaseScale();
                    this.IsModified = true;
                }
            }
        }

        /// <summary>
        /// Request internal implementation options for the terrain material to use, 
        /// in this case a terrain-wide composite map. 
        /// The TerrainMaterialGenerator should call this method to specify the 
        /// options it would like to use when creating a material. Not all the data
        /// is guaranteed to be up to date on return from this method - for example some
        /// maps may be generated in the background. However, on return from this method
        /// all the features that are requested will be referenceable by materials, the
        /// data may just take a few frames to be fully populated.
        /// ------------------------------------------------------
        /// compositeMap Whether a terrain-wide composite map is needed. A composite
        /// map is a texture with all of the blending and lighting baked in, such that
        /// at distance this texture can be used as an approximation of the multi-layer
        /// blended material. It is actually up to the material generator to render this
        /// composite map, because obviously precisely what it looks like depends on what
        /// the main material looks like. For this reason, the composite map is one piece
        /// of derived terrain data that is always calculated in the render thread, and
        /// usually on the GPU. It is expected that if this option is requested, 
        /// the material generator will use it to construct distant LOD techniques.
        /// </summary>
        [OgreVersion( 1, 7, 2, "Original: name _setCompositeMapRequired" )]
        public bool CompositeMapRequired
        {
            set
            {
                if ( mCompositeMapRequired != value )
                {
                    mCompositeMapRequired = value;
                    CreateOrDestroyGPUCompositeMap();

                    // if we enabled, generate normal maps
                    if ( mCompositeMapRequired )
                    {
                        mCompositeMapDirtyRect.Left = mCompositeMapDirtyRect.Top = 0;
                        mCompositeMapDirtyRect.Right = mCompositeMapDirtyRect.Bottom = mSize;
                        UpdateCompositeMap();
                    }
                }
            }
        }

        /// <summary>
        /// Get's whether a global color map is enabled on this terrain
        /// </summary>
        public bool IsGlobalColorMapEnabled
        {
            get;
            protected set;
        }

        /// <summary>
        /// Get access to the lightmap, if enabled (as requested by the material generator)
        /// </summary>
        public Texture LightMap
        {
            get;
            protected set;
        }

        /// <summary>
        /// Get the requested size of lightmap for this terrain. 
        /// Note that where hardware limits this, the actual lightmap may be lower
        /// resolution. This option is derived from TerrainGlobalOptions when the
        /// terrain is created.
        /// </summary>
        public ushort LightMapSize
        {
            get;
            protected set;
        }

        /// <summary>
        /// Get's access to the global colour map, if enabled
        /// </summary>
        public Texture GlobalColorMap
        {
            get;
            protected set;
        }

        /// <summary>
        ///  Get's the size of the global colour map (if used)
        /// </summary>
        public ushort GlobalColorMapSize
        {
            get;
            protected set;
        }

        /// <summary>
        /// Get's the declaration which describes the layers in this terrain.
        /// </summary>
        public TerrainLayerDeclaration LayerDeclaration
        {
            get { return mLayerDecl; }
        }

        /// <summary>
        /// Get's the (global) normal map texture
        /// </summary>
        public Texture TerrainNormalMap
        {
            get;
            protected set;
        }

        /// <summary>
        /// Get access to the composite map, if enabled (as requested by the material generator)
        /// </summary>
        public Texture CompositeMap
        {
            get;
            protected set;
        }

        /// <summary>
        /// Get's the top level of the quad tree which is used to divide up the terrain
        /// </summary>
        public TerrainQuadTreeNode QuadTree
        {
            get;
            protected set;
        }

        /// <summary>
        /// Get the requested size of composite map for this terrain. 
        /// Note that where hardware limits this, the actual texture may be lower
        /// resolution. This option is derived from TerrainGlobalOptions when the
        /// terrain is created.
        /// </summary>
        public ushort CompositeMapSize
        {
            get { return mCompositeMapSize; }
        }

        /// <summary>
        /// Get's the number of layers in this terrain.
        /// </summary>
        public byte LayerCount
        {
            get { return (byte)mLayers.Count; }
        }

        /// <summary>
        /// Get the total number of LOD levels in the terrain
        /// </summary>
        public ushort NumLodLevels
        {
            get;
            protected set;
        }

        /// <summary>
        /// Get the number of LOD levels in a leaf of the terrain quadtree
        /// </summary>
        public ushort NumLodLevelsPerLeaf
        {
            get;
            protected set;
        }

        /// <summary>
        /// Get's or set's the render queue group that this terrain will be rendered into
        /// </summary>
        /// <remarks>The default is specified in TerrainGlobalOptions</remarks>
        public RenderQueueGroupID RenderQueueGroupID
        {
            get;
            protected set;
        }

        /// <summary>
        /// Get the maximum number of layers supported with the current options. 
        /// </summary>
        /// <note>When you change the options requested, this value can change. </note>
        public byte MaxLayers
        {
            get { return mMaterialGenerator.GetMaxLayers( this ); }
        }
        #endregion properties

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="sm">The SceneManager to use.</param>
        public Terrain( SceneManager sm )
            : base()
        {
            this.SceneManager = sm;
            this.ResourceGroup = string.Empty;
            this.IsLoaded = false;
            this.IsModified = false;
            this.IsHeightDataModified = false;
            this.QuadTree = null;
            this.NumLodLevels = 0;
            this.NumLodLevelsPerLeaf = 0;
            this.IsDerivedDataUpdateInProgress = false;
            this.GlobalColorMapSize = 0;
            this.IsGlobalColorMapEnabled = false;
            //TODO
            //mLodMorphRequired(false)
            //mCustomGpuBufferAllocator = null

            this.RootSceneNode = sm.RootSceneNode.CreateChildSceneNode();
#warning add SceneManager.AddListerner - or simmilar event approach
            //sm->addListener( this );

#warning implement workerqueue here
#if false
        WorkQueue* wq = Root::getSingleton().getWorkQueue();
		mWorkQueueChannel = wq->getChannel("AxiomTerrain");
		wq->addRequestHandler(mWorkQueueChannel, this);
		wq->addResponseHandler(mWorkQueueChannel, this);
#endif

            // generate a material name, it's important for the terrain material
            // name to be consistent & unique no matter what generator is being used
            // so use our own pointer as identifier, use FashHash rather than just casting
            // the pointer to a long so we support 64-bit pointers
            mMaterialName = "AxiomTerrain/" + this.GetHashCode();

            this.SceneManager.PreFindVisibleObjects += new FindVisibleObjectsEvent( _preFindVisibleObjects );

            TerrainGlobalOptions.SkirtSize = 30;
            Vector3 normalized = new Vector3( 1, -1, 0 );
            normalized.Normalize();
            //TerrainGlobalOptions.LightMapDirection = normalized;
            TerrainGlobalOptions.CastsDynamicShadows = false;
            TerrainGlobalOptions.MaxPixelError = 8.0f;
            TerrainGlobalOptions.RenderQueueGroupID = RenderQueueGroupID.Main;
            TerrainGlobalOptions.VisibilityFlags = 0xFFFFFFFF;
            TerrainGlobalOptions.QueryFlags = 0xFFFFFFFF;
            TerrainGlobalOptions.IsUseRayBoxDistanceCalculation = false;
            TerrainGlobalOptions.DefaultMaterialGenerator = new TerrainMaterialGeneratorA();
            TerrainGlobalOptions.LayerBlendMapSize = 1024;
            TerrainGlobalOptions.DefaultLayerTextureWorldSize = 10;
            TerrainGlobalOptions.DefaultGlobalColorMapSize = 1024;
            TerrainGlobalOptions.LightMapSize = 1024;
            TerrainGlobalOptions.CompositeMapSize = 1024;
            TerrainGlobalOptions.CompositeMapAmbient = sm.AmbientLight;
            TerrainGlobalOptions.CompositeMapDiffuse = ColorEx.White;
            TerrainGlobalOptions.CompositeMapDistance = 3000;
            mLightMapShadowsOnly = true;
            msBlendTextureGenerator = new NameGenerator<Texture>( "TerrBlend" );
            TerrainGlobalOptions.DefaultMaterialGenerator.DebugLevel = 0;
        }

        protected override void dispose( bool disposeManagedResources )
        {
            if ( !this.IsDisposed )
            {
                if ( disposeManagedResources )
                {
                    mDerivedUpdatePendingMask = 0;
                    WaitForDerivedProcesses();
#warning delete workerqueue
#if false
WorkQueue* wq = Root::getSingleton().getWorkQueue();
		wq->removeRequestHandler(WORKQUEUE_CHANNEL, this);
		wq->removeResponseHandler(WORKQUEUE_CHANNEL, this);	
#endif
                    FreeTemporaryResources();
                    FreeGPUResources();
                    FreeCPUResources();
                    if ( this.SceneManager != null )
                    {
                        this.SceneManager.PreFindVisibleObjects -= _preFindVisibleObjects;
                        this.SceneManager.DestroySceneNode( this.RootSceneNode );
#warning implement scenemanager.removelistener - or simmilar approach
                    }
                }
            }

            base.dispose( disposeManagedResources );
        }

        #region - public functions -

        /// <summary>
        /// Utility method to get the number of indexes required to render a given batch
        /// </summary>
        [OgreVersion( 1, 7, 2 )]
        public static int GetNumIndexesForBatchSize( ushort batchSize )
        {
            int mainIndexesPerRow = batchSize * 2 + 1;
            int numRows = batchSize - 1;
            int mainIndexCount = mainIndexesPerRow * numRows;
            // skirts share edges, so they take 1 less row per side than batchSize, 
            // but with 2 extra at the end (repeated) to finish the strip
            // * 2 for the vertical line, * 4 for the sides, +2 to finish
            int skirtIndexCount = ( batchSize - 1 ) * 2 * 4 + 2;
            return mainIndexCount + skirtIndexCount;
        }

        /// <summary>
        /// Utility method to populate a (locked) index buffer.
        /// </summary>
        /// <param name="pIndexes">Pointer to an index buffer to populate</param>
        /// <param name="batchSize">The number of vertices down one side of the batch</param>
        /// <param name="vdatasize">The number of vertices down one side of the vertex data being referenced</param>
        /// <param name="vertexIncrement">The number of vertices to increment for each new indexed row / column</param>
        /// <param name="xoffset">The x offset from the start of the vertex data being referenced</param>
        /// <param name="yoffset">The y offset from the start of the vertex data being referenced</param>
        /// <param name="numSkirtRowsCols">Number of rows and columns of skirts</param>
        /// <param name="skirtRowColSkip">The number of rows / cols to skip in between skirts</param>
        [OgreVersion( 1, 7, 2 )]
        public static void PopulateIndexBuffer( BufferBase pIndexes, ushort batchSize,
            ushort vdatasize, int vertexIncrement, ushort xoffset, ushort yoffset, ushort numSkirtRowsCols,
            ushort skirtRowColSkip )
        {
            /* For even / odd tri strip rows, triangles are this shape:
            6---7---8
            | \ | \ |
            3---4---5
            | / | / |
            0---1---2
            Note how vertex rows count upwards. In order to match up the anti-clockwise
            winding and this upward transitioning list, we need to start from the
            right hand side. So we get (2,5,1,4,0,3) etc on even lines (right-left)
            and (3,6,4,7,5,8) etc on odd lines (left-right). At the turn, we emit the end index 
            twice, this forms a degenerate triangle, which lets us turn without any artefacts. 
            So the full list in this simple case is (2,5,1,4,0,3,3,6,4,7,5,8)

            Skirts are part of the same strip, so after finishing on 8, where sX is
            the skirt vertex corresponding to main vertex X, we go
            anticlockwise around the edge, (s8,7,s7,6,s6) to do the top skirt, 
            then (3,s3,0,s0),(1,s1,2,s2),(5,s5,8,s8) to finish the left, bottom, and
            right skirts respectively.
            */

            // to issue a complete row, it takes issuing the upper and lower row
            // and one extra index, which is the degenerate triangle and also turning
            // around the winding
#if !AXIOM_SAFE_ONLY
            unsafe
#endif
            {
                int rowSize = vdatasize * vertexIncrement;
                int numRows = batchSize - 1;
                var pI = pIndexes.ToUShortPointer();
                int offset = 0;

                // Start on the right
                ushort currentVertex = (ushort)( ( batchSize - 1 ) * vertexIncrement );
                // but, our quad area might not start at 0 in this vertex data
                // offsets are at main terrain resolution, remember
                ushort columnStart = xoffset;
                ushort rowStart = yoffset;
                currentVertex += (ushort)( rowStart * vdatasize + columnStart );
                bool rightToLeft = true;
                for ( ushort r = 0; r < numRows; ++r )
                {
                    for ( ushort c = 0; c < batchSize; ++c )
                    {
                        pI[ offset++ ] = currentVertex;
                        pI[ offset++ ] = (ushort)( currentVertex + rowSize );

                        // don't increment / decrement at a border, keep this vertex for next
                        // row as we 'snake' across the tile
                        if ( c + 1 < batchSize )
                            currentVertex = rightToLeft ? (ushort)( currentVertex - vertexIncrement ) : (ushort)( currentVertex + vertexIncrement );
                    }
                    rightToLeft = !rightToLeft;
                    currentVertex += (ushort)rowSize;
                    // issue one extra index to turn winding around
                    pI[ offset++ ] = currentVertex;
                }

                // Skirts
                for ( ushort s = 0; s < 4; ++s )
                {
                    // edgeIncrement is the index offset from one original edge vertex to the next
                    // in this row or column. Columns skip based on a row size here
                    // skirtIncrement is the index offset from one skirt vertex to the next, 
                    // because skirts are packed in rows/cols then there is no row multiplier for
                    // processing columns
                    int edgeIncrement = 0, skirtIncrement = 0;
                    switch ( s )
                    {
                        case 0: // top
                            edgeIncrement = -(int)vertexIncrement;
                            skirtIncrement = -(int)vertexIncrement;
                            break;

                        case 1: // left
                            edgeIncrement = -(int)rowSize;
                            skirtIncrement = -(int)vertexIncrement;
                            break;

                        case 2: // bottom
                            edgeIncrement = (int)vertexIncrement;
                            skirtIncrement = (int)vertexIncrement;
                            break;

                        case 3: // right
                            edgeIncrement = (int)rowSize;
                            skirtIncrement = (int)vertexIncrement;
                            break;
                    }
                    // Skirts are stored in contiguous rows / columns (rows 0/2, cols 1/3)
                    ushort skirtIndex = CalcSkirtVertexIndex( currentVertex, vdatasize, ( s % 2 ) != 0, numSkirtRowsCols, skirtRowColSkip );

                    for ( ushort c = 0; c < batchSize - 1; ++c )
                    {
                        pI[ offset++ ] = currentVertex;
                        pI[ offset++ ] = skirtIndex;
                        currentVertex += (ushort)edgeIncrement;
                        skirtIndex += (ushort)skirtIncrement;
                    }

                    if ( s == 3 )
                    {
                        // we issue an extra 2 indices to finish the skirt off
                        pI[ offset++ ] = currentVertex;
                        pI[ offset++ ] = skirtIndex;
                        currentVertex += (ushort)edgeIncrement;
                        skirtIndex += (ushort)skirtIncrement;
                    }
                }
            }
        }

        [OgreVersion( 1, 7, 2 )]
        public static ushort CalcSkirtVertexIndex( ushort mainIndex, ushort vdatasize, bool isCol,
            ushort numSkirtRowsCols, ushort skirtRowColSkip )
        {
            // row / col in main vertex resolution
            ushort row = (ushort)( mainIndex / vdatasize );
            ushort col = (ushort)( mainIndex % vdatasize );

            // skrits are after main vertices, so skip them
            ushort b = (ushort)( vdatasize * vdatasize );

            // The layout in vertex data is:
            // 1. row skirts
            //    numSkirtRowsCols rows of resolution vertices each
            // 2. column skirts
            //    numSkirtRowsCols cols of resolution vertices each

            // No offsets used here, this is an index into the current vertex data, 
            // which is already relative
            if ( isCol )
            {
                ushort skirtNum = (ushort)( col / skirtRowColSkip );
                ushort colbase = (ushort)( numSkirtRowsCols * vdatasize );
                return (ushort)( b + colbase + vdatasize * skirtNum + row );
            }
            else
            {
                ushort skirtNum = (ushort)( row / skirtRowColSkip );
                return (ushort)( b + vdatasize * skirtNum + col );
            }
        }

        /// <summary>
        /// Convert a position from one space to another with respect to this terrain.
        /// </summary>
        /// <param name="inSpace">The space that inPos is expressed as</param>
        /// <param name="inPos">The incoming position</param>
        /// <param name="outSpace">The space which outPos should be expressed as</param>
        /// <param name="outPos"> The output position to be populated</param>
        [OgreVersion( 1, 7, 2 )]
        public void ConvertPosition( Space inSpace, Vector3 inPos, Space outSpace, ref Vector3 outPos )
        {
            ConvertSpace( inSpace, inPos, outSpace, ref outPos, true );
        }
        /// <summary>
        /// Convert a position from one space to another with respect to this terrain.
        /// </summary>
        /// <param name="inSpace"> The space that inPos is expressed as</param>
        /// <param name="inPos">The incoming position</param>
        /// <param name="outSpace">The space which outPos should be expressed as</param>
        public Vector3 ConvertPosition( Space inSpace, Vector3 inPos, Space outSpace )
        {
            Vector3 ret = Vector3.Zero;
            ConvertPosition( inSpace, inPos, outSpace, ref ret );
            return ret;
        }
        /// <summary>
        /// Convert a direction from one space to another with respect to this terrain.
        /// </summary>
        /// <param name="inSpace">The space that inDir is expressed as</param>
        /// <param name="inDir">The incoming direction</param>
        /// <param name="outSpace">The space which outDir should be expressed as</param>
        /// <param name="outDir">The output direction to be populated</param>
        [OgreVersion( 1, 7, 2 )]
        public void ConvertDirection( Space inSpace, Vector3 inDir, Space outSpace, ref Vector3 outDir )
        {
            ConvertSpace( inSpace, inDir, outSpace, ref outDir, false );
        }
        /// <summary>
        /// Convert a direction from one space to another with respect to this terrain.
        /// </summary>
        /// <param name="inSpace">The space that inDir is expressed as</param>
        /// <param name="inDir">The incoming direction</param>
        /// <param name="outSpace">The space which outDir should be expressed as</param>
        /// <returns>The output direction </returns>
        public Vector3 ConvertDirection( Space inSpace, Vector3 inDir, Space outSpace )
        {
            Vector3 ret = Vector3.Zero;
            ConvertDirection( inSpace, inDir, outSpace, ref ret );
            return ret;
        }
        /// <summary>
        /// Save terrain data in native form to a standalone file
        /// </summary>
        /// <param name="fileName"></param>
        /// <note>
        /// This is a fairly basic way of saving the terrain, to save to a
        ///	file in the resource system, or to insert the terrain data into a
        ///	shared file, use the StreamSerialiser form.
        /// </note>
        public void Save( string filename )
        {
            FileStream fs = null;
            try
            {
                fs = new FileStream( filename, FileMode.OpenOrCreate, FileAccess.ReadWrite );
            }
            catch ( FileNotFoundException )
            {
                throw new FileNotFoundException( string.Format( "Can't open {0} for writing. Terrain.Save()", filename ) );
            }
            finally
            {
                if ( fs != null )
                {
                    StreamSerializer sr = new StreamSerializer( fs );
                    Save( sr );
                }
            }
        }
        /// <summary>
        /// Save terrain data in native form to a serializing stream
        /// </summary>
        /// <param name="stream"></param>
        [OgreVersion( 1, 7, 2 )]
        public void Save( StreamSerializer stream )
        {
            // wait for any queued processes to finish
            WaitForDerivedProcesses();

            if ( this.IsHeightDataModified )
            {
                // When modifying, for efficiency we only increase the max deltas at each LOD,
                // we never reduce them (since that would require re-examining more samples)
                // Since we now save this data in the file though, we need to make sure we've
                // calculated the optimal
                Rectangle rect = new Rectangle();
                rect.Top = 0; rect.Bottom = mSize;
                rect.Left = 0; rect.Right = mSize;
                CalculateHeightDeltas( rect );
                FinalizeHeightDeltas( rect, false );
            }

            stream.WriteChunkBegin( TERRAIN_CHUNK_ID, TERRAIN_CHUNK_VERSION );

            byte align = (byte)this.Alignment;
            stream.Write( align );

            stream.Write( mSize );
            stream.Write( mWorldSize );
            stream.Write( mMaxBatchSize );
            stream.Write( mMinBatchSize );
            stream.Write( mPos );
            for ( int i = 0; i < mHeightData.Length; i++ )
                stream.Write( mHeightData[ i ] );

            WriteLayerDeclaration( mLayerDecl, ref stream );

            //Layers
            CheckLayers( false );
            byte numLayers = (byte)mLayers.Count;
            WriteLayerInstanceList( mLayers, ref stream );

            //packed layer blend data
            if ( mCpuBlendMapStorage.Count > 0 )
            {
                // save from CPU data if it's there, it means GPU data was never created
                stream.Write( mLayerBlendMapSize );

                // load packed cpu data
                int numBlendTex = (byte)GetBlendTextureCount( numLayers );
                for ( int i = 0; i < numBlendTex; ++i )
                {
                    PixelFormat fmt = GetBlendTextureFormat( (byte)i, numLayers );
                    int channels = PixelUtil.GetNumElemBytes( fmt );
                    int dataSz = channels * mLayerBlendMapSize * mLayerBlendMapSize;
                    byte pData = mCpuBlendMapStorage[ i ];
                    stream.Write( pData );
                    stream.Write( dataSz );
                }
            }
            else
            {
                if ( mLayerBlendMapSize != mLayerBlendSizeActual )
                {
                    LogManager.Instance.Write(
                        @"WARNING: blend maps were requested at a size larger than was supported
                        on this hardware, which means the quality has been degraded" );
                }
                stream.Write( mLayerBlendSizeActual );
                byte[] tmpData = new byte[ mLayerBlendSizeActual * mLayerBlendSizeActual * 4 ];
                var pTmpDataF = BufferBase.Wrap( tmpData );
                foreach ( Texture tex in mBlendTextureList )
                {
                    PixelBox dst = new PixelBox( mLayerBlendSizeActual, mLayerBlendSizeActual, 1, tex.Format, pTmpDataF );
                    tex.GetBuffer().BlitToMemory( dst );
                    int dataSz = PixelUtil.GetNumElemBytes( tex.Format )
                                 * mLayerBlendSizeActual * mLayerBlendSizeActual;
                    stream.Write( tmpData );
                    stream.Write( dataSz );
                }
            }

            //other data
            //normals
            stream.ReadChunkBegin( TERRAINDERIVEDDATA_CHUNK_ID, TERRAINDERIVEDDATA_CHUNK_VERSION );
            stream.Write( "normalmap" );
            stream.Write( mSize );
            if ( mCpuTerrainNormalMap != null )
            {
                byte[] aData = new byte[ mSize * mSize * 3 ];
                Memory.Copy( mCpuTerrainNormalMap.Data, BufferBase.Wrap( aData ), aData.Length );
                // save from CPU data if it's there, it means GPU data was never created
                stream.Write( aData );
            }
            else
            {
                byte[] tmpData = new byte[ mSize * mSize * 3 ];
                PixelBox dst = new PixelBox( mSize, mSize, 1, PixelFormat.BYTE_RGB, Memory.PinObject( tmpData ) );
                this.TerrainNormalMap.GetBuffer().BlitToMemory( dst );
                stream.Write( tmpData );
                Memory.UnpinObject( tmpData );
                tmpData = null;
            }
            stream.ReadChunkEnd( TERRAINDERIVEDDATA_CHUNK_ID );

            //color map
            if ( this.IsGlobalColorMapEnabled )
            {
                stream.WriteChunkBegin( TERRAINDERIVEDDATA_CHUNK_ID, TERRAINDERIVEDDATA_CHUNK_VERSION );
                stream.Write( "colormap" );
                stream.Write( this.GlobalColorMapSize );
                if ( mCpuColorMapStorage != null )
                {
                    // save from CPU data if it's there, it means GPU data was never created
                    stream.Write( mCpuColorMapStorage );
                }
                else
                {
                    byte[] aData = new byte[ this.GlobalColorMapSize * this.GlobalColorMapSize * 3 ];
                    var pDataF = BufferBase.Wrap( aData );
                    PixelBox dst = new PixelBox( this.GlobalColorMapSize, this.GlobalColorMapSize, 1, PixelFormat.BYTE_RGB,
                                                 pDataF );
                    this.GlobalColorMap.GetBuffer().BlitToMemory( dst );
                    stream.Write( aData );
                }
                stream.WriteChunkEnd( TERRAINDERIVEDDATA_CHUNK_ID );
            }

            //ligthmap
            if ( mLightMapRequired )
            {
                stream.WriteChunkBegin( TERRAINDERIVEDDATA_CHUNK_ID, TERRAINDERIVEDDATA_CHUNK_VERSION );
                stream.Write( "lightmap" );
                stream.Write( this.LightMapSize );
                if ( mCpuLightmapStorage != null )
                {
                    // save from CPU data if it's there, it means GPU data was never created
                    stream.Write( mCpuLightmapStorage );
                }
                else
                {
                    byte[] aData = new byte[ this.LightMapSize * this.LightMapSize ];
                    var pDataF = BufferBase.Wrap( aData );
                    PixelBox dst = new PixelBox( this.LightMapSize, this.LightMapSize, 1, PixelFormat.L8, pDataF );
                    this.LightMap.GetBuffer().BlitToMemory( dst );
                    stream.Write( aData );
                }
                stream.WriteChunkEnd( TERRAIN_CHUNK_ID );
            }

            // composite map
            if ( mCompositeMapRequired )
            {
                stream.WriteChunkBegin( TERRAINDERIVEDDATA_CHUNK_ID, TERRAINDERIVEDDATA_CHUNK_VERSION );
                stream.Write( "compositemap" );
                stream.Write( mCompositeMapSize );
                if ( mCpuCompositeMapStorage != null )
                {
                    // save from CPU data if it's there, it means GPU data was never created
                    stream.Write( mCpuCompositeMapStorage );
                }
                else
                {

                    // composite map is 4 channel, 3x diffuse, 1x specular mask
                    byte[] aData = new byte[ mCompositeMapSize * mCompositeMapSize * 4 ];
                    var pDataF = BufferBase.Wrap( aData );
                    PixelBox dst = new PixelBox( mCompositeMapSize, mCompositeMapSize, 1, PixelFormat.BYTE_RGB, pDataF );
                    this.CompositeMap.GetBuffer().BlitToMemory( dst );
                    stream.Write( aData );
                }
                stream.WriteChunkEnd( TERRAINDERIVEDDATA_CHUNK_ID );
            }

            //write deltas
            stream.Write( mDeltaDataPtr );

            //write the quadtree
            this.QuadTree.Save( stream );

            stream.WriteChunkEnd( TERRAIN_CHUNK_ID );

            this.IsModified = false;
            this.IsHeightDataModified = false;
        }
        /// <summary>
        /// Prepare the terrain from a standalone file.
        /// </summary>
        /// <param name="fileName"></param>
        /// <note>
        /// This is safe to do in a background thread as it creates no GPU resources.
        /// It reads data from a native terrain data chunk. For more advanced uses, 
        /// such as loading from a shared file, use the StreamSerialiser form.
        /// </note>
        [OgreVersion( 1, 7, 2 )]
        public bool Prepare( string fileName )
        {
            FileStream stream = (FileStream)ResourceGroupManager.Instance.OpenResource(
                    fileName, this.DerivedResourceGroup );
            StreamSerializer ser = new StreamSerializer( stream );
            return Prepare( ser );
        }
        /// <summary>
        /// Prepare terrain data from saved data.
        /// </summary>
        /// <param name="stream"></param>
        /// <remarks>
        /// This is safe to do in a background thread as it creates no GPU resources.
        ///	It reads data from a native terrain data chunk. 
        /// </remarks>
        /// <returns>true if the preparation was successful</returns>
        [OgreVersion( 1, 7, 2 )]
        public bool Prepare( StreamSerializer stream )
        {
            FreeTemporaryResources();
            FreeCPUResources();

            CopyGlobalOptions();

            if ( stream.ReadChunkBegin( TERRAIN_CHUNK_ID, TERRAIN_CHUNK_VERSION ) == null )
                return false;

            byte align;
            stream.Read( out align );
            this.Alignment = (Alignment)align;
            stream.Read( out mSize );
            stream.Read( out mWorldSize );

            stream.Read( out mMaxBatchSize );
            stream.Read( out mMinBatchSize );
            stream.Read( out mPos );
            this.RootSceneNode.Position = mPos;
            UpdateBaseScale();
            DetermineLodLevels();

            int numVertices = mSize * mSize;
            mHeightData = new float[ numVertices ];
            stream.Read( out mHeightData );

            // layer declaration
            if ( !ReadLayerDeclaration( ref stream, ref mLayerDecl ) )
                return false;

            this.CheckDeclaration();

            // Layers
            if ( !ReadLayerInstanceList( ref stream, mLayerDecl.Elements.Count, ref mLayers ) )
                return false;

            DeriveUVMultipliers();

            // Packed layer blend data
            byte numLayers = (byte)mLayers.Count;
            stream.Read( out mLayerBlendMapSize );
            mLayerBlendSizeActual = mLayerBlendMapSize;// for now, until we check
            //load packed CPU data
            int numBlendTex = GetBlendTextureCount( numLayers );
            for ( int i = 0; i < numBlendTex; ++i )
            {
                PixelFormat fmt = GetBlendTextureFormat( (byte)i, numLayers );
                int channels = PixelUtil.GetNumElemBytes( fmt );
                int dataSz = channels * mLayerBlendMapSize * mLayerBlendMapSize;
                byte[] data = new byte[ dataSz ];
                stream.Read( out data );
                mCpuBlendMapStorage.AddRange( data );
            }

            //derived data
            while ( !stream.IsEndOfChunk( TERRAIN_CHUNK_ID ) &&
                stream.NextChunkId == TERRAINDERIVEDDATA_CHUNK_ID )
            {
                stream.ReadChunkBegin( TERRAINDERIVEDDATA_CHUNK_ID, TERRAINDERIVEDDATA_CHUNK_VERSION );
                //name
                string name = string.Empty;
                stream.Read( out name );
                ushort sz;
                stream.Read( out sz );
                if ( name == "normalmap" )
                {
                    mNormalMapRequired = true;
                    byte[] data = new byte[ sz * sz * 3 ];
                    stream.Read( out data );
                    var pDataF = BufferBase.Wrap( data );
                    mCpuTerrainNormalMap = new PixelBox( sz, sz, 1, PixelFormat.BYTE_RGB, pDataF );
                }
                else if ( name == "colormap" )
                {
                    this.IsGlobalColorMapEnabled = true;
                    this.GlobalColorMapSize = sz;
                    mCpuColorMapStorage = new byte[ sz * sz * 3 ];
                    stream.Read( out mCpuColorMapStorage );
                }
                else if ( name == "lightmap" )
                {
                    mLightMapRequired = true;
                    this.LightMapSize = sz;
                    mCpuLightmapStorage = new byte[ sz * sz ];
                    stream.Read( out mCpuLightmapStorage );
                }
                else if ( name == "compositemape" )
                {
                    mCompositeMapRequired = true;
                    mCompositeMapSize = sz;
                    mCpuCompositeMapStorage = new byte[ sz * sz * 4 ];
                    stream.Read( out mCpuCompositeMapStorage );
                }

                stream.ReadChunkEnd( TERRAINDERIVEDDATA_CHUNK_ID );
            }

            //Load delta data
            float[] deltaData = new float[ sizeof( float ) * numVertices ];
            stream.Read( out deltaData );
            mDeltaDataPtr = BufferBase.Wrap( deltaData );

            //Create and load quadtree
            this.QuadTree = new TerrainQuadTreeNode( this, null, 0, 0, mSize, (ushort)( this.NumLodLevels - 1 ), 0, 0 );
            this.QuadTree.Prepare();

            stream.ReadChunkEnd( TERRAIN_CHUNK_ID );

            DistributeVertexData();

            this.IsModified = false;
            this.IsHeightDataModified = false;

            return true;
        }
        /// <summary>
        /// Prepare the terrain from some import data rather than loading from 
        ///	native data. 
        /// </summary>
        /// <param name="importData"></param>
        /// <returns></returns>
        /// <remarks>
        /// This method may be called in a background thread.
        /// </remarks>
        [OgreVersion( 1, 7, 2 )]
        public bool Prepare( ImportData importData )
        {
            FreeTemporaryResources();
            FreeCPUResources();

            CopyGlobalOptions();

            //validate
            if ( !( IsPowerOfTwo( (ulong)( importData.TerrainSize - 1 ) ) && IsPowerOfTwo( (ulong)( importData.MinBatchSize - 1 ) )
                && IsPowerOfTwo( (ulong)( importData.MaxBatchSize - 1 ) ) ) )
            {
                throw new AxiomException( "terrainSize, minBatchSize and maxBatchSize must all be n^2 + 1. Terrain.Prepare" );
            }

            if ( importData.MinBatchSize > importData.MaxBatchSize )
            {
                throw new AxiomException( "MinBatchSize must be less then or equal to MaxBatchSize. Terrain.Prepare" );
            }

            if ( importData.MaxBatchSize > TERRAIN_MAX_BATCH_SIZE )
            {
                throw new AxiomException( "MaxBatchSize must be not larger then {0} . Terrain.Prepare", TERRAIN_MAX_BATCH_SIZE );
            }

            this.Alignment = importData.TerrainAlign;
            mSize = importData.TerrainSize;
            mWorldSize = importData.WorldSize;
            mLayerDecl = importData.LayerDeclaration;
            CheckDeclaration();
            mLayers = importData.LayerList;
            CheckLayers( false );
            DeriveUVMultipliers();
            mMaxBatchSize = importData.MaxBatchSize;
            mMinBatchSize = importData.MinBatchSize;
            mPos = importData.Pos;
            UpdateBaseScale();
            DetermineLodLevels();

            int numVertices = mSize * mSize;

            mHeightData = new float[ numVertices ];

            if ( importData.InputFloat != null )
            {

                if ( Utility.RealEqual( importData.InputBias, 0.0f ) && Utility.RealEqual( importData.InputScale, 1.0f ) )
                {
                    //straigt copy
                    mHeightData = new float[ numVertices ];
                    Array.Copy( importData.InputFloat, mHeightData, mHeightData.Length );
                }
                else
                {
                    // scale & bias, lets do it unsafe, should be faster :)
                    var src = importData.InputFloat;
                    for ( var i = 0; i < numVertices; ++i )
                        mHeightData[ i ] = ( src[ i ] * importData.InputScale ) + importData.InputBias;
                }
            }
            else if ( importData.InputImage != null )
            {
                Image img = importData.InputImage;
                if ( img.Width != mSize || img.Height != mSize )
                    img.Resize( mSize, mSize );

                // convert image data to floats
                // Do this on a row-by-row basis, because we describe the terrain in
                // a bottom-up fashion (ie ascending world coords), while Image is top-down
                var pSrcBaseF = BufferBase.Wrap( img.Data );
                var pHeightDataF = BufferBase.Wrap( mHeightData );
                for ( int i = 0; i < mSize; ++i )
                {
                    int srcy = mSize - i - 1;
                    var psrc = pSrcBaseF + srcy * img.RowSpan;
                    var pDest = pHeightDataF + ( i * mSize ) * sizeof( float );
                    PixelConverter.BulkPixelConversion( psrc, 0, img.Format, pDest, 0, PixelFormat.FLOAT32_R, mSize );
                }

                if ( !Utility.RealEqual( importData.InputBias, 0.0f ) || !Utility.RealEqual( importData.InputScale, 1.0f ) )
                {
                    for ( int i = 0; i < numVertices; ++i )
                    {
                        mHeightData[ i ] = ( mHeightData[ i ] * importData.InputScale ) + importData.InputBias;
                    }
                }
            }
            else
            {
                // start with flat terrain
                mHeightData = new float[ mSize * mSize ];
            }

            float[] deltaData = new float[ numVertices ];

            mHeightDataPtr = Memory.PinObject( mHeightData );
            mDeltaDataPtr = Memory.PinObject( deltaData );

            ushort numLevel = (ushort)(int)( this.NumLodLevels - 1 );
            this.QuadTree = new TerrainQuadTreeNode( this, null, 0, 0, mSize, (ushort)( this.NumLodLevels - 1 ), 0, 0 );
            this.QuadTree.Prepare();

            //calculate entire terrain
            Rectangle rect = new Rectangle();
            rect.Top = 0; rect.Bottom = mSize;
            rect.Left = 0; rect.Right = mSize;
            CalculateHeightDeltas( rect );
            FinalizeHeightDeltas( rect, true );

            DistributeVertexData();

            // Imported data is treated as modified because it's not saved
            this.IsModified = true;
            this.IsHeightDataModified = true;

            return true;
        }
        /// <summary>
        /// Prepare and load the terrain in one simple call from a standalone file.
        /// </summary>
        /// <param name="fileName"></param>
        /// <note>
        /// This method must be called from the primary render thread. To load data
        ///	in a background thread, use the prepare() method.
        /// </note>
        public void Load( string fileName )
        {
            if ( Prepare( fileName ) )
                Load();
            else
                throw new AxiomException( "Error while preparing {0}, see log for details. Terrain.Load", fileName );
        }
        /// <summary>
        ///  Prepare and load the terrain in one simple call from a standalone file.
        /// </summary>
        /// <param name="stream"></param>
        /// <note>
        /// This method must be called from the primary render thread. To load data
        ///	in a background thread, use the prepare() method.
        /// </note>
        [OgreVersion( 1, 7, 2 )]
        public void Load( StreamSerializer stream )
        {
            if ( Prepare( stream ) )
                Load();
            else
                throw new AxiomException( "Error while preparing from stream, see log for details. Terrain.Load" );
        }

        /// <summary>
        /// Load the terrain based on the data already populated via prepare methods.
        /// </summary>
        /// <remarks>
        /// This method must be called in the main render thread. 
        /// </remarks>
        [OgreVersion( 1, 7, 2 )]
        public void Load()
        {
            if ( this.IsLoaded )
                return;

            if ( this.QuadTree != null )
                this.QuadTree.Load();

            CheckLayers( true );
            CreateOrDestroyGPUColorMap();
            CreateOrDestroyGPUNormalMap();
            CreateOrDestroyGPULightmap();
            CreateOrDestroyGPUCompositeMap();

            mMaterialGenerator.RequestOption( this );

            this.IsLoaded = true;
        }

        /// <summary>
        ///  Unload the terrain and free GPU resources. 
        /// </summary>
        /// <remarks>
        /// This method must be called in the main render thread.
        /// </remarks>
        [OgreVersion( 1, 7, 2 )]
        public void Unload()
        {
            if ( !this.IsLoaded )
                return;

            if ( this.QuadTree != null )
                this.QuadTree.Unload();

            // free own buffers if used, but not custom
            mDefaultGpuBufferAllocator.FreeAllBuffers();

            this.IsLoaded = false;
            this.IsModified = false;
            this.IsHeightDataModified = false;
        }

        /// <summary>
        /// Free CPU resources created during prepare methods.
        /// </summary>
        /// <remarks>
        /// This is safe to do in a background thread after calling unload().
        /// </remarks>
        public void Unprepare()
        {
            if ( this.QuadTree != null )
                this.QuadTree.Unprepare();
        }
        /// <summary>
        /// Get the height data for a given terrain point. 
        /// </summary>
        /// <param name="x">x, y Discrete coordinates in terrain vertices, values from 0 to size-1,
        ///	left/right bottom/top</param>
        /// <param name="y">x, y Discrete coordinates in terrain vertices, values from 0 to size-1,
        ///	left/right bottom/top</param>
        /// <returns></returns>
        [OgreVersion( 1, 7, 2 )]
        public float GetHeightAtPoint( long x, long y )
        {
            //clamp
            x = Utility.Min( x, (long)mSize - 1L );
            x = Utility.Max( x, 0L );
            y = Utility.Min( y, (long)mSize - 1L );
            y = Utility.Max( y, 0L );

            return mHeightData[ y + mSize * x ];
        }
        /// <summary>
        /// Set the height data for a given terrain point. 
        /// </summary>
        /// <note>
        /// this doesn't take effect until you call update()
        /// </note>
        /// <param name="x"> x, y Discrete coordinates in terrain vertices, values from 0 to size-1,
        /// left/right bottom/top</param>
        /// <param name="y"> x, y Discrete coordinates in terrain vertices, values from 0 to size-1,
        /// left/right bottom/top</param>
        /// <param name="heightVal">The new height</param>
        [OgreVersion( 1, 7, 2 )]
        public void SetHeightAtPoint( long x, long y, float heightVal )
        {
            //clamp
            x = Utility.Min( x, (long)mSize - 1L );
            x = Utility.Max( x, 0L );
            y = Utility.Min( y, (long)mSize - 1L );
            y = Utility.Max( y, 0L );
            mHeightData[ y + mSize * x ] = heightVal;
            Rectangle rec = new Rectangle();
            rec.Left = x;
            rec.Right = x + 1;
            rec.Top = y;
            rec.Bottom = y + 1;
            DirtyRect( rec );
        }
        /// <summary>
        /// Get the height data for a given terrain position. 
        /// </summary>
        /// <param name="x">x, y Position in terrain space, values from 0 to 1 left/right bottom/top</param>
        /// <param name="y">x, y Position in terrain space, values from 0 to 1 left/right bottom/top</param>
        public float GetHeightAtTerrainPosition( float x, float y )
        {
            // get left / bottom points (rounded down)
            float factor = mSize - 1;
            float invFactor = 1.0f / factor;

            long startX = (long)( x * factor );
            long startY = (long)( y * factor );
            long endX = startX + 1;
            long endY = startY + 1;

            // now get points in terrain space (effectively rounding them to boundaries)
            // note that we do not clamp! We need a valid plane
            float startXTS = startX * invFactor;
            float startYTS = startX * invFactor;
            float endXTS = endX * invFactor;
            float endYTS = endY * invFactor;

            //now clamp
            endX = Utility.Min( endX, (long)mSize - 1 );
            endY = Utility.Min( endY, (long)mSize - 1 );

            // get parametric from start coord to next point
            float xParam = ( x - startXTS ) / invFactor;
            float yParam = ( y - startYTS ) / invFactor;

            /* For even / odd tri strip rows, triangles are this shape:
                even     odd
                3---2   3---2
                | / |   | \ |
                0---1   0---1
                */

            // Build all 4 positions in terrain space, using point-sampled height
            Vector3 v0 = new Vector3( startXTS, startYTS, GetHeightAtPoint( startX, startY ) );
            Vector3 v1 = new Vector3( endXTS, startYTS, GetHeightAtPoint( endX, startY ) );
            Vector3 v2 = new Vector3( endXTS, endYTS, GetHeightAtPoint( endX, endY ) );
            Vector3 v3 = new Vector3( startXTS, endYTS, GetHeightAtPoint( startX, endY ) );
            //define this plane in terrain space
            Plane plane = new Plane();
            if ( startY % 2 != 0 )
            {
                //odd row
                bool secondTri = ( ( 1.0f - yParam ) > xParam );
                if ( secondTri )
                    plane.Redefine( v0, v1, v3 );
                else
                    plane.Redefine( v1, v2, v3 );
            }
            else
            {
                //even row
                bool secondtri = ( yParam > xParam );
                if ( secondtri )
                    plane.Redefine( v0, v2, v3 );
                else
                    plane.Redefine( v0, v1, v2 );
            }

            //solve plane quation for z
            return ( -plane.Normal.x * x
                    - plane.Normal.y * y
                    - plane.D ) / plane.Normal.z;
        }
        /// <summary>
        /// Get the height data for a given world position (projecting the point
        ///	down on to the terrain).
        /// </summary>
        /// <param name="x">x, y,z Position in world space. Positions will be clamped to the edge
        ///	of the terrain</param>
        /// <param name="y">x, y,z Position in world space. Positions will be clamped to the edge
        ///	of the terrain</param>
        /// <param name="z">x, y,z Position in world space. Positions will be clamped to the edge
        ///	of the terrain</param>
        /// <returns></returns>
        public float GetHeightAtWorldPosition( float x, float y, float z )
        {
            Vector3 terrPos = Vector3.Zero;
            GetTerrainPosition( x, y, z, ref terrPos );
            return GetHeightAtTerrainPosition( terrPos.x, terrPos.y );
        }
        /// <summary>
        /// Get the height data for a given world position (projecting the point
        /// down on to the terrain).
        /// </summary>
        /// <param name="pos">Position in world space. Positions will be clamped to the edge
        /// of the terrain</param>
        /// <returns></returns>
        public float GetHeightAtWorldPosition( Vector3 pos )
        {
            return GetHeightAtWorldPosition( pos.x, pos.y, pos.z );
        }
        /// <summary>
        /// Get a pointer to the delta data for a given point. 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public BufferBase GetDeltaData( long x, long y )
        {
            System.Diagnostics.Debug.Assert( x >= 0 && x < mSize && y >= 0 && y < mSize, "Out of bounds.." );
            return mDeltaDataPtr + ( y * mSize + x ) * sizeof( float );
            //return new IntPtr(mDeltaDataPtr.ToInt32() + (y * mSize + x));
            //unsafe
            //{
            //    float* val = (float*)mDeltaDataPtr;
            //    return (IntPtr)(float*)&val[y * mSize + x];
            //}
            //return Memory.PinObject(mDeltaData[y * mSize + x]);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public BufferBase GetHeightData( long x, long y )
        {
            System.Diagnostics.Debug.Assert( x >= 0 && x < mSize && y >= 0 && y < mSize, "Out of bounds.." );
            return mHeightDataPtr + ( y * mSize + x ) * sizeof( float );
            //unsafe
            //{
            //    float* val = (float*)mHeightDataPtr;
            //    return (IntPtr)(float*)&val[y * mSize + x];
            //}
            //return new IntPtr( (float*)mHeightDataPtr + (y * mSize + x));
            //return Memory.PinObject(mHeightData[y * mSize + x]);
        }

        /// <summary>
        /// Get a Vector3 of the world-space point on the terrain, aligned as per
        ///	options.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="outpos"></param>
        /// <note>
        /// This point is relative to Terrain.Position
        /// </note>
        public void GetPoint( long x, long y, ref Vector3 outpos )
        {
            GetPointAlign( x, y, this.Alignment, ref outpos );
        }

        public void GetPointAlign( long x, long y, Alignment align, ref Vector3 outPos )
        {
            float height = mHeightData[ y + mSize * x ];
            GetPointAlign( x, y, height, align, ref outPos );

        }

        public void GetPointAlign( long x, long y, float height, Alignment align, ref Vector3 outPos )
        {
            switch ( align )
            {
                case Alignment.Align_X_Z:
                    outPos.y = height;
                    outPos.x = x * mScale + mBase;
                    outPos.z = y * -mScale - mBase;
                    break;
                case Alignment.Align_Y_Z:
                    outPos.x = height;
                    outPos.z = x * -mScale - mBase;
                    outPos.y = y * mScale + mBase;
                    break;
                case Alignment.Align_X_Y:
                    outPos.z = height;
                    outPos.x = x * mScale + mBase;
                    outPos.y = y * mScale + mBase;
                    break;
            }
        }
        /// <summary>
        /// Get a Vector3 of the world-space point on the terrain, supplying the
        ///	height data manually (can be more optimal). 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="height"></param>
        /// <param name="outPos"></param>
        /// <note>
        /// This point is relative to Terrain.Position
        /// </note>
        public void GetPoint( long midpointx, long midpointy, float height, ref Vector3 localCentre )
        {
            GetPointAlign( midpointx, midpointy, height, this.Alignment, ref localCentre );
        }

        /// <summary>
        /// Translate a vector from world space to local terrain space based on the alignment options.
        /// </summary>
        /// <param name="inVec">The vector in basis space, where x/y represents the 
        /// terrain plane and z represents the up vector</param>
        /// <param name="outVec"></param>
        public void GetTerrainVector( Vector3 inVec, ref Vector3 outVec )
        {
            GetTerrainVectorAlign( inVec.x, inVec.y, inVec.z, this.Alignment, ref outVec );
        }

        public void GetVectorAlign( Vector3 invec, Alignment align, ref Vector3 outVec )
        {
            GetVectorAlign( invec.x, invec.y, invec.z, align, ref outVec );
        }

        public void GetVectorAlign( float x, float y, float z, Alignment align, ref Vector3 outVec )
        {
            switch ( align )
            {
                case Alignment.Align_X_Z:
                    outVec.y = z;
                    outVec.x = x;
                    outVec.z = -y;
                    break;
                case Alignment.Align_Y_Z:
                    outVec.x = z;
                    outVec.y = y;
                    outVec.z = -x;
                    break;
                case Alignment.Align_X_Y:
                    outVec.x = x;
                    outVec.y = y;
                    outVec.z = z;
                    break;
            }
        }
        /// <summary>
        /// Translate a vector from world space to local terrain space based on the alignment options.
        /// </summary>
        /// <param name="inVec">The vector in basis space, where x/y represents the 
        /// terrain plane and z represents the up vector</param>
        /// <param name="align"></param>
        /// <param name="outVec"></param>
        public void GetTerrainVectorAlign( Vector3 inVec, Alignment align, ref Vector3 outVec )
        {
            GetTerrainVectorAlign( inVec.x, inVec.y, inVec.z, align, ref outVec );
        }

        /// <summary>
        /// Translate a vector from world space to local terrain space based on the alignment options. 
        /// </summary>
        /// <param name="x">x, y, z The vector in basis space, where x/y represents the 
        /// terrain plane and z represents the up vector</param>
        /// <param name="y">x, y, z The vector in basis space, where x/y represents the 
        /// terrain plane and z represents the up vector</param>
        /// <param name="z">x, y, z The vector in basis space, where x/y represents the 
        /// terrain plane and z represents the up vector</param>
        /// <param name="outVec"></param>
        public void GetTerrainVector( float x, float y, float z, ref Vector3 outVec )
        {
            GetTerrainVectorAlign( x, y, z, this.Alignment, ref outVec );
        }
        /// <summary>
        /// Translate a vector from world space to local terrain space based on a specified alignment.
        /// </summary>
        /// <param name="x">x, y, z The vector in world space, where x/y represents the 
        /// terrain plane and z represents the up vector</param>
        /// <param name="y">x, y, z The vector in world space, where x/y represents the 
        /// terrain plane and z represents the up vector</param>
        /// <param name="z">x, y, z The vector in world space, where x/y represents the 
        /// terrain plane and z represents the up vector</param>
        /// <param name="aling"></param>
        /// <param name="outVec"></param>
        public void GetTerrainVectorAlign( float x, float y, float z, Alignment align, ref Vector3 outVec )
        {
            switch ( align )
            {
                case Alignment.Align_X_Z:
                    outVec.z = y;
                    outVec.x = x;
                    outVec.y = -z;
                    break;
                case Alignment.Align_Y_Z:
                    outVec.z = x;
                    outVec.y = y;
                    outVec.x = -z;
                    break;
                case Alignment.Align_X_Y:
                    outVec.x = x;
                    outVec.y = y;
                    outVec.z = z;
                    break;
            }
        }

        /// <summary>
        /// Translate a vector into world space based on the alignment options.
        /// </summary>
        /// <param name="x">x, y, z The vector in basis space, where x/y represents the 
        /// terrain plane and z represents the up vector</param>
        /// <param name="y">x, y, z The vector in basis space, where x/y represents the 
        /// terrain plane and z represents the up vector</param>
        /// <param name="z">x, y, z The vector in basis space, where x/y represents the 
        /// terrain plane and z represents the up vector</param>
        /// <param name="outVec"></param>
        public void GetVector( float x, float y, float z, ref Vector3 outVec )
        {
            GetVectorAlign( x, y, z, this.Alignment, ref outVec );
        }
        /// <summary>
        /// Translate a vector into world space based on the alignment options.
        /// </summary>
        /// <param name="invec"></param>
        /// <param name="outVec"></param>
        public void GetVector( Vector3 invec, ref Vector3 outVec )
        {
            GetVectorAlign( invec.x, invec.y, invec.z, this.Alignment, ref outVec );
        }

        public void GetPosition( Vector3 TSPos, ref Vector3 outWSpos )
        {
            GetPositionAlign( TSPos, this.Alignment, ref outWSpos );
        }

        /// <summary>
        /// Convert a position from terrain basis space to world space. 
        /// </summary>
        /// <param name="x">x,y,z Terrain space position, where (0,0) is the bottom-left of the
        /// terrain, and (1,1) is the top-right. The Z coordinate is in absolute
        /// height units.</param>
        /// <param name="y">x,y,z Terrain space position, where (0,0) is the bottom-left of the
        /// terrain, and (1,1) is the top-right. The Z coordinate is in absolute
        /// height units.</param>
        /// <param name="z">x,y,z Terrain space position, where (0,0) is the bottom-left of the
        /// terrain, and (1,1) is the top-right. The Z coordinate is in absolute
        /// height units.</param>
        /// <param name="outWSpos"></param>
        public void GetPosition( float x, float y, float z, ref Vector3 outWSpos )
        {
            GetPositionAlign( x, y, z, this.Alignment, ref outWSpos );
        }
        /// <summary>
        /// Convert a position from world space to terrain basis space. 
        /// </summary>
        /// <param name="WSpos">World space position (setup according to current alignment). </param>
        /// <param name="outTSpos">Terrain space output position, where (0,0) is the bottom-left of the
        /// terrain, and (1,1) is the top-right. The Z coordinate is in absolute
        /// height units.</param>
        public void GetTerrainPosition( Vector3 WSpos, ref Vector3 outTSpos )
        {
            GetTerrainPositionAlign( WSpos, this.Alignment, ref outTSpos );
        }
        /// <summary>
        /// Convert a position from world space to terrain basis space. 
        /// </summary>
        /// <param name="x">x,y,z World space position (setup according to current alignment).</param>
        /// <param name="y">x,y,z World space position (setup according to current alignment).</param>
        /// <param name="z">x,y,z World space position (setup according to current alignment).</param>
        /// <param name="outTSpos">Terrain space output position, where (0,0) is the bottom-left of the
        /// terrain, and (1,1) is the top-right. The Z coordinate is in absolute
        /// height units.</param>
        public void GetTerrainPosition( float x, float y, float z, ref Vector3 outTSpos )
        {
            GetTerrainPositionAlign( x, y, z, this.Alignment, ref outTSpos );
        }
        /// <summary>
        /// Convert a position from terrain basis space to world space based on a specified alignment. 
        /// </summary>
        /// <param name="TSpos">Terrain space position, where (0,0) is the bottom-left of the
        ///	terrain, and (1,1) is the top-right. The Z coordinate is in absolute
        ///	height units.</param>
        /// <param name="align"></param>
        /// <param name="outWSpos">World space output position (setup according to alignment). </param>
        public void GetPositionAlign( Vector3 TSpos, Alignment align, ref Vector3 outWSpos )
        {
            GetPositionAlign( TSpos.x, TSpos.y, TSpos.z, align, ref outWSpos );
        }
        /// <summary>
        /// Convert a position from terrain basis space to world space based on a specified alignment. 
        /// </summary>
        /// <param name="x">x,y,z Terrain space position, where (0,0) is the bottom-left of the
        /// terrain, and (1,1) is the top-right. The Z coordinate is in absolute
        /// height units.</param>
        /// <param name="y">x,y,z Terrain space position, where (0,0) is the bottom-left of the
        /// terrain, and (1,1) is the top-right. The Z coordinate is in absolute
        /// height units.</param>
        /// <param name="z">x,y,z Terrain space position, where (0,0) is the bottom-left of the
        /// terrain, and (1,1) is the top-right. The Z coordinate is in absolute
        /// height units.</param>
        /// <param name="align"></param>
        /// <param name="outWSpos">World space output position (setup according to alignment). </param>
        public void GetPositionAlign( float x, float y, float z, Alignment align, ref Vector3 outWSpos )
        {
            switch ( align )
            {
                case Alignment.Align_X_Z:
                    outWSpos.y = z;
                    outWSpos.x = x * ( mSize - 1 ) * mScale + mBase;
                    outWSpos.z = y * ( mSize - 1 ) * -mScale - mBase;
                    break;
                case Alignment.Align_Y_Z:
                    outWSpos.x = z;
                    outWSpos.y = y * ( mSize - 1 ) * mScale + mBase;
                    outWSpos.z = x * ( mSize - 1 ) * -mScale - mBase;
                    break;
                case Alignment.Align_X_Y:
                    outWSpos.z = z;
                    outWSpos.x = x * ( mSize - 1 ) * mScale + mBase;
                    outWSpos.y = y * ( mSize - 1 ) * mScale + mBase;
                    break;
            }
        }
        /// <summary>
        /// Convert a position from world space to terrain basis space based on a specified alignment. 
        /// </summary>
        /// <param name="WSpos">World space position (setup according to alignment). </param>
        /// <param name="align"></param>
        /// <param name="outTSpos"> Terrain space output position, where (0,0) is the bottom-left of the
        /// terrain, and (1,1) is the top-right. The Z coordinate is in absolute
        /// height units.</param>
        public void GetTerrainPositionAlign( Vector3 WSpos, Alignment align, ref Vector3 outTSpos )
        {
            GetTerrainPositionAlign( WSpos.x, WSpos.y, WSpos.z, align, ref outTSpos );
        }
        /// <summary>
        /// Convert a position from world space to terrain basis space based on a specified alignment. 
        /// </summary>
        /// <param name="x">x,y,z World space position (setup according to alignment). </param>
        /// <param name="y">x,y,z World space position (setup according to alignment). </param>
        /// <param name="z">x,y,z World space position (setup according to alignment). </param>
        /// <param name="align"></param>
        /// <param name="outTSpos">Terrain space output position, where (0,0) is the bottom-left of the
        /// terrain, and (1,1) is the top-right. The Z coordinate is in absolute
        /// height units.</param>
        public void GetTerrainPositionAlign( float x, float y, float z, Alignment align, ref Vector3 outTSpos )
        {
            switch ( align )
            {
                case Alignment.Align_X_Z:
                    outTSpos.x = ( x - mBase - mPos.x ) / ( ( mSize - 1 ) * mScale );
                    outTSpos.y = ( z + mBase - mPos.z ) / ( ( mSize - 1 ) * -mScale );
                    outTSpos.z = y;
                    break;
                case Alignment.Align_Y_Z:
                    outTSpos.x = ( z - mBase - mPos.z ) / ( ( mSize - 1 ) * -mScale );
                    outTSpos.y = ( y + mBase - mPos.y ) / ( ( mSize - 1 ) * mScale );
                    outTSpos.z = x;
                    break;
                case Alignment.Align_X_Y:
                    outTSpos.x = ( x - mBase - mPos.x ) / ( ( mSize - 1 ) * mScale );
                    outTSpos.y = ( y - mBase - mPos.y ) / ( ( mSize - 1 ) * mScale );
                    outTSpos.z = z;
                    break;
            }
        }
        /// <summary>
        /// Add a new layer to this terrain.
        /// </summary>
        public void AddLayer()
        {
            AddLayer( 0 );
        }
        /// <summary>
        /// Add a new layer to this terrain.
        /// </summary>
        /// <param name="worldSize">The size of the texture in this layer in world units. Default
        /// to zero to use the default</param>
        public void AddLayer( Real worldSize )
        {
            AddLayer( worldSize, null );
        }
        /// <summary>
        /// Add a new layer to this terrain.
        /// </summary>
        /// <param name="worldSize">The size of the texture in this layer in world units. Default
        /// to zero to use the default</param>
        /// <param name="textureNames">A list of textures to assign to the samplers in this
        ///	layer. Leave blank to provide these later. </param>
        public void AddLayer( Real worldSize, List<string> textureNames )
        {
            if ( worldSize == 0 )
                worldSize = TerrainGlobalOptions.DefaultLayerTextureWorldSize;
            mLayers.Add( new LayerInstance() );
            if ( textureNames != null )
            {
                LayerInstance inst = mLayers[ mLayers.Count - 1 ];
                inst.TextureNames = textureNames;
            }
            // use utility method to update UV scaling
            SetLayerWorldSize( (byte)( mLayers.Count - 1 ), worldSize );
            CheckLayers( true );
            mMaterialDirty = true;
            mMaterialParamsDirty = true;
            this.IsModified = true;
        }
        /// <summary>
        /// Remove a layer from the terrain.
        /// </summary>
        /// <param name="index"></param>
        public void RemoveLayer( byte index )
        {
            if ( index < mLayers.Count )
            {
                LayerInstance ins = mLayers[ index ];
                mLayers.Remove( ins );
                mMaterialDirty = true;
                mMaterialParamsDirty = true;
                this.IsModified = true;
            }
        }
        /// <summary>
        /// How large an area in world space the texture in a terrain layer covers
        /// before repeating.
        /// </summary>
        /// <param name="index">The layer index.</param>
        /// <returns></returns>
        public float GetLayerWorldSize( byte index )
        {
            if ( index < mLayers.Count )
            {
                return mLayers[ index ].WorldSize;
            }
            else if ( mLayers.Count > 0 )
            {
                return mLayers[ 0 ].WorldSize;
            }
            else
            {
                return TerrainGlobalOptions.DefaultLayerTextureWorldSize;
            }
        }
        /// <summary>
        /// How large an area in world space the texture in a terrain layer covers
        /// before repeating.
        /// </summary>
        /// <param name="index">The layer index.</param>
        /// <param name="size">The world size of the texture before repeating</param>
        public void SetLayerWorldSize( byte index, float size )
        {
            if ( index < mLayers.Count )
            {
                if ( index >= mLayerUVMultiplier.Count )
                    mLayerUVMultiplier.Add( mWorldSize / size );
                else
                    mLayerUVMultiplier[ index ] = mWorldSize / size;

                LayerInstance inst = mLayers[ index ];
                inst.WorldSize = size;
                mMaterialParamsDirty = true;
                this.IsModified = true;
            }
        }
        /// <summary>
        /// Get the layer UV multiplier. 
        /// </summary>
        /// <remarks>
        /// This is derived from the texture world size. The base UVs in the 
        ///	terrain vary from 0 to 1 and this multiplier is used (in a fixed-function 
        ///	texture coord scaling or a shader parameter) to translate it to the
        ///	final value.
        /// </remarks>
        /// <param name="index">The layer index.</param>
        /// <returns></returns>
        public float GetLayerUVMultiplier( byte index )
        {
            if ( index < mLayerUVMultiplier.Count )
            {
                return mLayerUVMultiplier[ index ];
            }
            else if ( mLayerUVMultiplier.Count > 0 )
            {
                return mLayerUVMultiplier[ 0 ];
            }
            else
            {
                // default to tile 100 times
                return 100;
            }
        }
        /// <summary>
        /// Get the name of the texture bound to a given index within a given layer.
        /// See the LayerDeclaration for a list of sampelrs within a layer.
        /// </summary>
        /// <param name="layerIndex">The layer index.</param>
        /// <param name="samplerIndex"> The sampler index within a layer</param>
        /// <returns></returns>
        public string GetLayerTextureName( byte layerIndex, byte samplerIndex )
        {
            if ( layerIndex < mLayers.Count && samplerIndex < mLayerDecl.Samplers.Count )
            {
                return mLayers[ layerIndex ].TextureNames[ samplerIndex ];
            }
            else
            {
                return string.Empty;
            }
        }
        /// <summary>
        /// Set the name of the texture bound to a given index within a given layer.
        /// See the LayerDeclaration for a list of sampelrs within a layer.
        /// </summary>
        /// <param name="layerIndex">The layer index.</param>
        /// <param name="samplerIndex">The sampler index within a layer</param>
        /// <param name="textureName">The name of the texture to use</param>
        [OgreVersion( 1, 7, 2 )]
        public void SetLayerTextureName( byte layerIndex, byte samplerIndex, string textureName )
        {
            if ( layerIndex < mLayers.Count && samplerIndex < mLayerDecl.Samplers.Count )
            {
                if ( mLayers[ layerIndex ].TextureNames[ samplerIndex ] != textureName )
                {
                    mLayers[ layerIndex ].TextureNames[ samplerIndex ] = textureName;
                    mMaterialDirty = true;
                    mMaterialParamsDirty = true;
                    this.IsModified = true;
                }
            }
        }
        /// <summary>
        /// Mark the entire terrain as dirty. 
        /// By marking a section of the terrain as dirty, you are stating that you have
        /// changed the height data within this rectangle. This rectangle will be merged with
        /// any existing outstanding changes. To finalise the changes, you must 
        /// call update(), updateGeometry(), or updateDerivedData().
        /// </summary>
        public void Dirty()
        {
            Rectangle rect = new Rectangle();
            rect.Top = 0; rect.Bottom = mSize;
            rect.Left = 0; rect.Right = mSize;
            DirtyRect( rect );
        }
        /// <summary>
        /// Mark a region of the terrain as dirty. 
        /// By marking a section of the terrain as dirty, you are stating that you have
        /// changed the height data within this rectangle. This rectangle will be merged with
        /// any existing outstanding changes. To finalise the changes, you must 
        /// call update(), updateGeometry(), or updateDerivedData().
        /// </summary>
        /// <param name="rect">rect A rectangle expressed in vertices describing the dirty region;
        /// left < right, top < bottom, left & top are inclusive, right & bottom exclusive</param>
        [OgreVersion( 1, 7, 2 )]
        public void DirtyRect( Rectangle rect )
        {
            mDirtyGeometryRect.Merge( rect );
            mDirtyGeometryRectForNeighbours.Merge( rect );
            mDirtyDerivedDataRect.Merge( rect );
            mCompositeMapDirtyRect.Merge( rect );

            this.IsModified = true;
            this.IsHeightDataModified = true;
        }
        /// <summary>
        ///  Mark a region of the terrain composite map as dirty. 
        /// </summary>
        /// <param name="rect"></param>
        /// <remarks>
        /// You don't usually need to call this directly, it is inferred from 
        ///	changing the other data on the terrain.
        /// </remarks>
        [OgreVersion( 1, 7, 2 )]
        public void DirtyCompositeMapRect( Rectangle rect )
        {
            mCompositeMapDirtyRect.Merge( rect );
            this.IsModified = true;
        }
        /// <summary>
        /// Trigger the update process for the terrain. (default non synchronous update)
        /// </summary>
        /// <remarks>
        /// Updating the terrain will process any dirty sections of the terrain.
        /// This may affect many things:
        /// <ol><li>The terrain geometry</li>
        /// <li>The terrain error metrics which determine LOD transitions</li>
        /// <li>The terrain normal map, if present</li>
        /// <li>The terrain lighting map, if present</li>
        /// <li>The terrain composite map, if present</li>
        /// </ol>
        /// If threading is enabled, only item 1 (the geometry) will be updated
        /// synchronously, ie will be fully up to date when this method returns.
        /// The other elements are more expensive to compute, and will be queued
        /// for processing in a background thread, in the order shown above. As these
        /// updates complete, the effects will be shown.
        /// 
        /// You can also separate the timing of updating the geometry, LOD and the lighting
        /// information if you want, by calling updateGeometry() and
        /// updateDerivedData() separately.
        /// </remarks>
        public void Update()
        {
            Update( false );
        }
        /// <summary>
        /// Trigger the update process for the terrain.
        /// </summary>
        /// <remarks>
        /// Updating the terrain will process any dirty sections of the terrain.
        /// This may affect many things:
        /// <ol><li>The terrain geometry</li>
        /// <li>The terrain error metrics which determine LOD transitions</li>
        /// <li>The terrain normal map, if present</li>
        /// <li>The terrain lighting map, if present</li>
        /// <li>The terrain composite map, if present</li>
        /// </ol>
        /// If threading is enabled, only item 1 (the geometry) will be updated
        /// synchronously, ie will be fully up to date when this method returns.
        /// The other elements are more expensive to compute, and will be queued
        /// for processing in a background thread, in the order shown above. As these
        /// updates complete, the effects will be shown.
        /// 
        /// You can also separate the timing of updating the geometry, LOD and the lighting
        /// information if you want, by calling updateGeometry() and
        /// updateDerivedData() separately.
        /// </remarks>
        /// <param name="synchronous">synchronous If true, all updates will happen immediately and not
        ///	in a separate thread.</param>
        [OgreVersion( 1, 7, 2 )]
        public void Update( bool synchronous )
        {
            UpdateGeometry();
            UpdateDerivedData( synchronous );
        }
        /// <summary>
        /// Performs an update on the terrain geometry based on the dirty region.
        /// </summary>
        /// <remarks>
        /// Terrain geometry will be updated when this method returns.
        /// </remarks>
        [OgreVersion( 1, 7, 2 )]
        public void UpdateGeometry()
        {
            if ( !mDirtyGeometryRect.IsNull )
            {
                this.QuadTree.UpdateVertexData( true, false, mDirtyGeometryRect, false );
                mDirtyGeometryRect.IsNull = true;
            }

            //propagate changes
            NotifyNeighbours();
        }

        public void UpdateDerivedData()
        {
            UpdateDerivedData( false, 0xFF );
        }
        /// <summary>
        /// Updates derived data for the terrain (LOD, lighting) to reflect changed height data, in a separate
        /// thread if threading is enabled. 
        /// If threading is enabled, on return from this method the derived
        /// data will not necessarily be updated immediately, the calculation 
        /// may be done in the background. Only one update will run in the background
        /// at once. This derived data can typically survive being out of sync for a 
        /// few frames which is why it is not done synchronously
        /// </summary>
        /// <param name="synchronous">If true, the update will happen immediately and not
        ///	in a separate thread.</param>
        public void UpdateDerivedData( bool synchronous )
        {
            UpdateDerivedData( synchronous, 0xFF );
        }
        private bool firstTime = true;
        /// <summary>
        /// Updates derived data for the terrain (LOD, lighting) to reflect changed height data, in a separate
        /// thread if threading is enabled. 
        /// If threading is enabled, on return from this method the derived
        /// data will not necessarily be updated immediately, the calculation 
        /// may be done in the background. Only one update will run in the background
        /// at once. This derived data can typically survive being out of sync for a 
        /// few frames which is why it is not done synchronously
        /// </summary>
        /// <param name="synchronous">If true, the update will happen immediately and not
        ///	in a separate thread.</param>
        /// <param name="typeMask">Mask indicating the types of data we should generate</param>
        [OgreVersion( 1, 7, 2 )]
        public void UpdateDerivedData( bool synchrounus, byte typeMask )
        {
            if ( !mDirtyDerivedDataRect.IsNull && !mDirtyLightmapFromNeighboursRect.IsNull )
            {
                this.IsModified = true;
                if ( this.IsDerivedDataUpdateInProgress )
                {
                    // Don't launch many updates, instead wait for the other one 
                    // to finish and issue another afterwards.
                    mDerivedUpdatePendingMask |= typeMask;
                }
                else
                {
                    UpdateDerivedDataImpl( mDirtyDerivedDataRect, mDirtyLightmapFromNeighboursRect,
                        synchrounus, typeMask );
                    mDirtyDerivedDataRect.IsNull = true;
                    mDirtyLightmapFromNeighboursRect.IsNull = true;
                }
            }
            else
            {
                // Usually the composite map is updated after the other background
                // data is updated (no point doing it beforehand), but if there's
                // nothing to update, then we'll do it right now.
                UpdateCompositeMap();
            }
        }
        /// <summary>
        /// Performs an update on the terrain composite map based on its dirty region.
        /// </summary>
        /// <remarks>
        /// Rather than calling this directly, call updateDerivedData, which will
        ///	also call it after the other derived data has been updated (there is
        ///	no point updating the composite map until lighting has been updated).
        ///	However the blend maps may call this directly when only the blending 
        ///	information has been updated.
        /// </remarks>
        [OgreVersion( 1, 7, 2 )]
        public void UpdateCompositeMap()
        {
            // All done in the render thread
            if ( mCompositeMapRequired && !mCompositeMapDirtyRect.IsNull )
            {
                this.IsModified = true;
                CreateOrDestroyGPUCompositeMap();
                if ( mCompositeMapDirtyRectLightmapUpdate &&
                    ( mCompositeMapDirtyRect.Width < mSize || mCompositeMapDirtyRect.Height < mSize ) )
                {
                    // widen the dirty rectangle since lighting makes it wider
                    Rectangle widenedRect = new Rectangle();
                    WidenRectByVector( TerrainGlobalOptions.LightMapDirection, mCompositeMapDirtyRect, ref widenedRect );
                    // clamp
                    widenedRect.Left = Utility.Max( widenedRect.Left, 0L );
                    widenedRect.Top = Utility.Max( widenedRect.Top, 0L );
                    widenedRect.Right = Utility.Min( widenedRect.Right, (long)mSize );
                    widenedRect.Bottom = Utility.Min( widenedRect.Bottom, (long)mSize );
                    mMaterialGenerator.UpdateCompositeMap( this, widenedRect );
                }
                else
                    mMaterialGenerator.UpdateCompositeMap( this, mCompositeMapDirtyRect );

                mCompositeMapDirtyRectLightmapUpdate = false;
                mCompositeMapDirtyRect.IsNull = true;
            }
        }
        /// <summary>
        /// Performs an update on the terrain composite map based on its dirty region, 
        ///	but only at a maximum frequency. 
        /// </summary>
        /// <remarks>
        /// Rather than calling this directly, call updateDerivedData, which will
        /// also call it after the other derived data has been updated (there is
        /// no point updating the composite map until lighting has been updated).
        /// However the blend maps may call this directly when only the blending 
        /// information has been updated.
        /// </remarks>
        /// <note>
        /// This method will log the request for an update, but won't do it just yet 
        /// unless there are no further requests in the next 'delay' seconds. This means
        /// you can call it all the time but only pick up changes in quiet times.
        /// </note>
        public void UpdateCompositeMapWithDelay()
        {
            UpdateCompositeMapWithDelay( 2 );
        }
        /// <summary>
        /// Performs an update on the terrain composite map based on its dirty region, 
        ///	but only at a maximum frequency. 
        /// </summary>
        /// <remarks>
        /// Rather than calling this directly, call updateDerivedData, which will
        /// also call it after the other derived data has been updated (there is
        /// no point updating the composite map until lighting has been updated).
        /// However the blend maps may call this directly when only the blending 
        /// information has been updated.
        /// </remarks>
        /// <note>
        /// This method will log the request for an update, but won't do it just yet 
        /// unless there are no further requests in the next 'delay' seconds. This means
        /// you can call it all the time but only pick up changes in quiet times.
        /// </note>
        [OgreVersion( 1, 7, 2 )]
        public void UpdateCompositeMapWithDelay( Real delay )
        {
            mCompositeMapUpdateCountdown = (long)( delay * 1000 );
        }
        /// <summary>
        /// Calculate (or recalculate) the delta values of heights between a vertex
        ///	in its recorded position, and the place it will end up in the LOD
        ///	in which it is removed. 
        /// </summary>
        /// <param name="rect">Rectangle describing the area in which heights have altered </param>
        /// <returns>A Rectangle describing the area which was updated (may be wider
        ///	than the input rectangle)</returns>
        [OgreVersion( 1, 7, 2 )]
        public Rectangle CalculateHeightDeltas( Rectangle rect )
        {
            Rectangle clampedRect = new Rectangle( rect );

            clampedRect.Left = Utility.Max( 0L, clampedRect.Left );
            clampedRect.Top = Utility.Max( 0L, clampedRect.Top );
            clampedRect.Right = Utility.Min( (long)mSize, clampedRect.Right );
            clampedRect.Bottom = Utility.Min( (long)mSize, clampedRect.Bottom );

            Rectangle finalRect = new Rectangle( clampedRect );
            this.QuadTree.PreDeltaCalculation( clampedRect );

            // Iterate over target levels, 
            for ( int targetLevel = 1; targetLevel < this.NumLodLevels; ++targetLevel )
            {
                int sourceLevel = targetLevel - 1;
                int step = 1 << targetLevel;

                // need to widen the dirty rectangle since change will affect surrounding
                // vertices at lower LOD
                Rectangle widendRect = rect;
                widendRect.Left = Utility.Max( 0L, widendRect.Left - step );
                widendRect.Top = Utility.Max( 0L, widendRect.Top - step );
                widendRect.Right = Utility.Min( (long)mSize, widendRect.Right + step );
                widendRect.Bottom = Utility.Min( (long)mSize, widendRect.Bottom + step );

                // keep a merge of the widest
                finalRect = finalRect.Merge( widendRect );


                // now round the rectangle at this level so that it starts & ends on 
                // the step boundaries
                Rectangle lodRect = new Rectangle( widendRect );
                lodRect.Left -= lodRect.Left % step;
                lodRect.Top -= lodRect.Top % step;
                if ( lodRect.Right % step != 0 )
                    lodRect.Right += step - ( lodRect.Right % step );
                if ( lodRect.Bottom % step != 0 )
                    lodRect.Bottom += step - ( lodRect.Bottom % step );

                for ( int j = (int)lodRect.Top; j < lodRect.Bottom - step; j += step )
                {
                    for ( int i = (int)lodRect.Left; i < lodRect.Right - step; i += step )
                    {
                        // Form planes relating to the lower detail tris to be produced
                        // For even tri strip rows, they are this shape:
                        // 2---3
                        // | / |
                        // 0---1
                        // For odd tri strip rows, they are this shape:
                        // 2---3
                        // | \ |
                        // 0---1
                        Vector3 v0 = Vector3.Zero,
                                v1 = Vector3.Zero,
                                v2 = Vector3.Zero,
                                v3 = Vector3.Zero;

                        GetPointAlign( i, j, Alignment.Align_X_Y, ref v0 );
                        GetPointAlign( i + step, j, Alignment.Align_X_Y, ref v1 );
                        GetPointAlign( i, j + step, Alignment.Align_X_Y, ref v2 );
                        GetPointAlign( i + step, j + step, Alignment.Align_X_Y, ref v3 );

                        Plane t1 = new Plane(),
                              t2 = new Plane();
                        bool backwardTri = false;
                        // Odd or even in terms of target level
                        if ( ( j / step ) % 2 == 0 )
                        {
                            t1.Redefine( v0, v1, v3 );
                            t2.Redefine( v0, v3, v2 );
                        }
                        else
                        {
                            t1.Redefine( v1, v3, v2 );
                            t2.Redefine( v0, v1, v2 );
                            backwardTri = true;
                        }

                        //include the bottommost row of vertices if this is the last row
                        int yubound = ( j == ( mSize - step ) ? step : step - 1 );
                        for ( int y = 0; y <= yubound; y++ )
                        {
                            // include the rightmost col of vertices if this is the last col
                            int xubound = ( i == ( mSize - step ) ? step : step - 1 );
                            for ( int x = 0; x <= xubound; x++ )
                            {
                                int fulldetailx = i + x;
                                int fulldetaily = j + y;
                                if ( fulldetailx % step == 0 &&
                                    fulldetaily % step == 0 )
                                {
                                    // Skip, this one is a vertex at this level
                                    continue;
                                }
                                Real ypct = (Real)y / (Real)step;
                                Real xpct = (Real)x / (Real)step;

                                //interpolated height
                                Vector3 actualPos = Vector3.Zero;
                                GetPointAlign( fulldetailx, fulldetaily, Alignment.Align_X_Y, ref actualPos );
                                Real interp_h = 0;
                                // Determine which tri we're on 
                                if ( ( xpct > ypct && !backwardTri ) ||
                                   ( xpct > ( 1 - ypct ) && backwardTri ) )
                                {
                                    // Solve for x/z
                                    interp_h =
                                        ( -t1.Normal.x * actualPos.x
                                         - t1.Normal.y * actualPos.y
                                         - t1.D ) / t1.Normal.z;
                                }
                                else
                                {
                                    // Second tri
                                    interp_h =
                                        ( -t2.Normal.x * actualPos.x
                                         - t2.Normal.y * actualPos.y
                                         - t2.D ) / t2.Normal.z;
                                }

                                Real actual_h = actualPos.z;
                                Real delta = interp_h - actual_h;

                                // max(delta) is the worst case scenario at this LOD
                                // compared to the original heightmap
                                if ( delta == float.NaN )
                                {
                                }
                                // tell the quadtree about this 
                                this.QuadTree.NotifyDelta( (ushort)fulldetailx, (ushort)fulldetaily, (ushort)sourceLevel, delta );


                                // If this vertex is being removed at this LOD, 
                                // then save the height difference since that's the move
                                // it will need to make. Vertices to be removed at this LOD
                                // are halfway between the steps, but exclude those that
                                // would have been eliminated at earlier levels
                                int halfStep = step / 2;
                                if (
                                    ( ( fulldetailx % step ) == halfStep && ( fulldetaily % halfStep ) == 0 ) ||
                                    ( ( fulldetaily % step ) == halfStep && ( fulldetailx % halfStep ) == 0 ) )
                                {
#if !AXIOM_SAFE_ONLY
                                    unsafe
#endif
                                    {
                                        // Save height difference 
                                        var pDest = this.GetDeltaData( fulldetailx, fulldetaily ).ToFloatPointer();
                                        pDest[ 0 ] = delta;
                                    }

                                }
                            }//x
                        }//y
                    }
                }//j
            }//targetlevel

            this.QuadTree.PostDeltaCalculation( clampedRect );

            return finalRect;
        }
        /// <summary>
        /// Finalise the height deltas. 
        /// Calculated height deltas are kept in a separate calculation field to make
        /// them safe to perform in a background thread. This call promotes those
        /// calculations to the runtime values, and must be called in the main thread.
        /// </summary>
        /// <param name="rect">Rectangle describing the area to finalise </param>
        /// <param name="cpuData">When updating vertex data, update the CPU copy (background)</param>
        public void FinalizeHeightDeltas( Rectangle rect, bool cpuData )
        {
            Rectangle clampedRect = new Rectangle( rect );
            clampedRect.Left = Utility.Max( 0L, clampedRect.Left );
            clampedRect.Top = Utility.Max( 0L, clampedRect.Top );
            clampedRect.Right = Utility.Min( (long)mSize, clampedRect.Right );
            clampedRect.Bottom = Utility.Min( (long)mSize, clampedRect.Bottom );

            // min/max information
            this.QuadTree.FinaliseDeltaValues( clampedRect );
            // dekta vertex data
            this.QuadTree.UpdateVertexData( false, true, clampedRect, cpuData );
        }

        public void GetPointFromSelfOrNeighbour( long x, long y, ref Vector3 outpos )
        {
            if ( x >= 0 && y >= 0 && x < mSize && y < mSize )
                GetPoint( x, y, ref outpos );
            else
            {
                long nx, ny;
                NeighbourIndex ni;
                GetNeighbourPointOverflow( x, y, out ni, out nx, out ny );
                Terrain neighbour = GetNeighbour( ni );
                if ( neighbour != null )
                {
                    Vector3 neighbourPos = Vector3.Zero;
                    neighbour.GetPoint( nx, ny, ref neighbourPos );
                    // adjust to make it relative to our position
                    outpos = neighbourPos + neighbour.Position - Position;
                }
                else
                {
                    // use our getPoint() after all, just clamp
                    x = Utility.Min( x, mSize - 1L );
                    y = Utility.Min( y, mSize - 1L );
                    x = Utility.Max( x, 0L );
                    y = Utility.Max( y, 0L );
                    GetPoint( x, y, ref outpos );
                }

            }
        }
        /// <summary>
        /// Calculate (or recalculate) the normals on the terrain
        /// </summary>
        /// <param name="rect">Rectangle describing the area of heights that were changed</param>
        /// <param name="outFinalRect"> Output rectangle describing the area updated</param>
        /// <returns>PixelBox full of normals (caller responsible for deletion)</returns>
        public PixelBox CalculateNormals( Rectangle rect, ref Rectangle outFinalRect )
        {
            // Widen the rectangle by 1 element in all directions since height
            // changes affect neighbours normals
            Rectangle widenedRect = new Rectangle(
                Utility.Max( 0L, rect.Left - 1L ),
                Utility.Max( 0L, rect.Top - 1L ),
                Utility.Min( (long)mSize, rect.Right + 1L ),
                Utility.Min( (long)mSize, rect.Bottom + 1L ) );

            // allocate memory for RGB
            byte[] pData = new byte[ widenedRect.Width * widenedRect.Height * 3 ];
            PixelBox pixbox = new PixelBox( (int)widenedRect.Width, (int)widenedRect.Height, 1, PixelFormat.BYTE_RGB, Memory.PinObject( pData ) );

            // Evaluate normal like this
            //  3---2---1
            //  | \ | / |
            //	4---P---0
            //  | / | \ |
            //	5---6---7

            Plane plane = new Plane();
            for ( long y = widenedRect.Top; y < widenedRect.Bottom; ++y )
            {
                for ( long x = widenedRect.Left; x < widenedRect.Right; ++x )
                {
                    Vector3 cumulativeNormal = Vector3.Zero;

                    // Build points to sample
                    Vector3 centrePoint = Vector3.Zero;
                    Vector3[] adjacentPoints = new Vector3[ 8 ];
                    GetPointFromSelfOrNeighbour( x, y, ref centrePoint );
                    GetPointFromSelfOrNeighbour( x + 1, y, ref adjacentPoints[ 0 ] );
                    GetPointFromSelfOrNeighbour( x + 1, y + 1, ref adjacentPoints[ 1 ] );
                    GetPointFromSelfOrNeighbour( x, y + 1, ref adjacentPoints[ 2 ] );
                    GetPointFromSelfOrNeighbour( x - 1, y + 1, ref adjacentPoints[ 3 ] );
                    GetPointFromSelfOrNeighbour( x - 1, y, ref adjacentPoints[ 4 ] );
                    GetPointFromSelfOrNeighbour( x - 1, y - 1, ref adjacentPoints[ 5 ] );
                    GetPointFromSelfOrNeighbour( x, y - 1, ref adjacentPoints[ 6 ] );
                    GetPointFromSelfOrNeighbour( x + 1, y - 1, ref adjacentPoints[ 7 ] );

                    for ( int i = 0; i < 8; ++i )
                    {
                        plane.Redefine( centrePoint, adjacentPoints[ i ], adjacentPoints[ ( i + 1 ) % 8 ] );
                        cumulativeNormal += plane.Normal;
                    }
                    // normalise & store normal
                    cumulativeNormal.Normalize();

                    // encode as RGB, object space
                    // invert the Y to deal with image space
                    long storeX = x - widenedRect.Left;
                    long storeY = widenedRect.Bottom - y - 1;

                    var pStore = ( ( storeY * widenedRect.Width ) + storeX ) * 3;
                    pData[ pStore++ ] = (byte)( ( cumulativeNormal.x + 1.0f ) * 0.5f * 255.0f );
                    pData[ pStore++ ] = (byte)( ( cumulativeNormal.y + 1.0f ) * 0.5f * 255.0f );
                    pData[ pStore ] = (byte)( ( cumulativeNormal.z + 1.0f ) * 0.5f * 255.0f );
                } //x
            }//y

            outFinalRect = widenedRect;
            return pixbox;//new PixelBox((int)widenedRect.Width, (int)widenedRect.Height, 1, PixelFormat.BYTE_RGB, Memory.PinObject(pData));
        }
        /// <summary>
        /// Finalise the normals. 
        /// Calculated normals are kept in a separate calculation area to make
        /// them safe to perform in a background thread. This call promotes those
        /// calculations to the runtime values, and must be called in the main thread.
        /// </summary>
        /// <param name="rect">Rectangle describing the area to finalize </param>
        /// <param name="normalsBox">PixelBox full of normals</param>
        public void FinalizeNormals( Rectangle rect, PixelBox normalsBox )
        {
            CreateOrDestroyGPUNormalMap();
            // deal with race condition where nm has been disabled while we were working!
            if ( this.TerrainNormalMap != null )
            {
                // blit the normals into the texture
                if ( rect.Left == 0 && rect.Top == 0 && rect.Bottom == mSize && rect.Right == mSize )
                {
                    this.TerrainNormalMap.GetBuffer().BlitFromMemory( normalsBox );
                }
                else
                {
                    // content of normalsBox is already inverted in Y, but rect is still 
                    // in terrain space for dealing with sub-rect, so invert
                    BasicBox dstBox = new BasicBox();
                    dstBox.Left = (int)rect.Left;
                    dstBox.Right = (int)rect.Bottom;
                    dstBox.Top = (int)( mSize - rect.Bottom );
                    dstBox.Bottom = (int)( mSize - rect.Top );
                    this.TerrainNormalMap.GetBuffer().BlitFromMemory( normalsBox, dstBox );
                }
            }

            // delete memory
            normalsBox.Data = null;
            normalsBox = null;
        }
        /// <summary>
        ///  Calculate (or recalculate) the terrain lightmap
        /// </summary>
        /// <param name="rect">Rectangle describing the area of heights that were changed</param>
        /// <param name="extraTargetRect">extraTargetRect Rectangle describing a target area of the terrain that
        /// needs to be calculated additionally (e.g. from a neighbour)
        /// </param>
        /// <param name="outFinalRect">Output rectangle describing the area updated in the lightmap</param>
        /// <returns> PixelBox full of lighting data (caller responsible for deletion)</returns>
        public PixelBox CalculateLightMap( Rectangle rect, Rectangle extraTargetRect, ref Rectangle outFinalRect )
        {
            // TODO - allow calculation of all lighting, not just shadows
            // TODO - handle neighbour page casting

            // as well as calculating the lighting changes for the area that is
            // dirty, we also need to calculate the effect on casting shadow on
            // other areas. To do this, we project the dirt rect by the light direction
            // onto the minimum height

            Vector3 lightVec = TerrainGlobalOptions.LightMapDirection;
            Rectangle widenedRect = new Rectangle();
            WidenRectByVector( lightVec, rect, ref widenedRect );

            //merge in the extra area (e.g. from neighbours)
            widenedRect.Merge( extraTargetRect );

            // widenedRect now contains terrain point space version of the area we
            // need to calculate. However, we need to calculate in lightmap image space
            float terrainToLightmapScale = (float)mLightmapSizeActual / (float)mSize;
            float left = widenedRect.Left;
            float right = widenedRect.Right;
            float top = widenedRect.Top;
            float bottom = widenedRect.Bottom;
            left *= terrainToLightmapScale;
            right *= terrainToLightmapScale;
            top *= terrainToLightmapScale;
            bottom *= terrainToLightmapScale;
            widenedRect = new Rectangle( (long)left, (long)top, (long)right, (long)bottom );
            //clamp
            widenedRect.Left = Utility.Max( 0L, widenedRect.Left );
            widenedRect.Top = Utility.Max( 0L, widenedRect.Top );
            widenedRect.Right = Utility.Min( (long)mLightmapSizeActual, widenedRect.Right );
            widenedRect.Bottom = Utility.Min( (long)mLightmapSizeActual, widenedRect.Bottom );

            outFinalRect = widenedRect;

            // allocate memory (L8)
            byte[] pData = new byte[ widenedRect.Width * widenedRect.Height ];
            var pDataPtr = Memory.PinObject( pData );


            float heightPad = (float)( ( MaxHeight - MinHeight ) * 1e-3f );
            for ( long y = widenedRect.Top; y < widenedRect.Bottom; ++y )
            {
                for ( long x = widenedRect.Left; x < widenedRect.Right; ++x )
                {
                    float litVal = 1.0f;
                    // convert to terrain space (not points, allow this to go between points)
                    float Tx = (float)x / (float)( mLightmapSizeActual - 1 );
                    float Ty = (float)y / (float)( mLightmapSizeActual - 1 );

                    // get world space point
                    // add a little height padding to stop shadowing self
                    Vector3 wpos = Vector3.Zero;
                    GetPosition( Tx, Ty, GetHeightAtTerrainPosition( Tx, Ty ) + heightPad, ref wpos );
                    wpos += Position;
                    // build ray, cast backwards along light direction
                    Ray ray = new Ray( wpos, -lightVec );
                    //cascade int neighbours when casting, but don't travel further
                    //than world size
                    KeyValuePair<bool, Vector3> rayHit = RayIntersects( ray, true, mWorldSize );

                    // TODO - cast multiple rays to antialias?
                    // TODO - fade by distance?

                    if ( rayHit.Key )
                        litVal = 0.0f;

                    // encode as L8
                    // invert the Y to deal with image space
                    long storeX = x - widenedRect.Left;
                    long storeY = widenedRect.Bottom - y - 1;

#if !AXIOM_SAFE_ONLY
                    unsafe
#endif
                    {
                        var pStoreF = pDataPtr.ToBytePointer();
                        var pStore = ( pDataPtr + (int)( ( storeY * widenedRect.Width ) + storeX ) ).ToBytePointer();
                        pStore[ 0 ] = (byte)( litVal * 255.0f );
                    }
                }
            }

            return new PixelBox( (int)widenedRect.Width, (int)widenedRect.Height, 1, PixelFormat.L8, pDataPtr );
        }
        /// <summary>
        /// Finalise the lightmap. 
        /// Calculating lightmaps is kept in a separate calculation area to make
        /// it safe to perform in a background thread. This call promotes those
        /// calculations to the runtime values, and must be called in the main thread.
        /// </summary>
        /// <param name="rect">Rectangle describing the area to finalize </param>
        /// <param name="lightmapBox">PixelBox full of normals</param>
        public void FinalizeLightMap( Rectangle rect, PixelBox lightmapBox )
        {
            CreateOrDestroyGPULightmap();
            // deal with race condition where lm has been disabled while we were working!
            if ( this.LightMap != null )
            {
                // blit the normals into the texture
                if ( rect.Left == 0 && rect.Top == 0 && rect.Bottom == mLightmapSizeActual && rect.Right == mLightmapSizeActual )
                {
                    this.LightMap.GetBuffer().BlitFromMemory( lightmapBox );
                }
                else
                {
                    // content of PixelBox is already inverted in Y, but rect is still 
                    // in terrain space for dealing with sub-rect, so invert
                    BasicBox dstBox = new BasicBox();
                    dstBox.Left = (int)rect.Left;
                    dstBox.Right = (int)rect.Right;
                    dstBox.Top = (int)( mLightmapSizeActual - rect.Bottom );
                    dstBox.Bottom = (int)( mLightmapSizeActual - rect.Top );
                    this.LightMap.GetBuffer().BlitFromMemory( lightmapBox, dstBox );
                }
            }
        }
        /// <summary>
        /// Gets the resolution of the entire terrain (down one edge) at a 
        ///	given LOD level.
        /// </summary>
        /// <param name="lodLevel"></param>
        /// <returns></returns>
        public ushort GetResolutionAtLod( ushort lodLevel )
        {
            return (ushort)( ( ( mSize - 1 ) >> lodLevel ) + 1 );
        }
        /// <summary>
        /// Test for intersection of a given ray with the terrain. If the ray hits
        /// the terrain, the point of intersection is returned.
        /// </summary>
        /// <param name="ray">The ray to test for intersection</param>
        /// <returns>A pair which contains whether the ray hit the terrain and, if so, where.</returns>
        /// <remarks>
        /// This can be called from any thread as long as no parallel write to
        /// the heightmap data occurs.
        /// </remarks>
        public KeyValuePair<bool, Vector3> RRayIntersects( Ray ray )
        {
            KeyValuePair<bool, Vector3> result = new KeyValuePair<bool, Vector3>();
            // first step: convert the ray to a local vertex space
            // we assume terrain to be in the x-z plane, with the [0,0] vertex
            // at origin and a plane distance of 1 between vertices.
            // This makes calculations easier.
            Vector3 rayOrigin = ray.Origin - Position;
            Vector3 rayDirection = ray.Direction;
            //change alignment
            Vector3 tmp = Vector3.Zero;
            switch ( Alignment )
            {
                case Alignment.Align_X_Y:
                    {
                        Axiom.Math.Utility.Swap( ref rayOrigin.y, ref rayOrigin.z );
                        Axiom.Math.Utility.Swap( ref rayDirection.y, ref rayDirection.z );
                    }
                    break;
                case Alignment.Align_Y_Z:
                    {
                        // x = z, z = y, y = -x
                        tmp.x = rayOrigin.z;
                        tmp.z = rayOrigin.y;
                        tmp.y = -rayOrigin.x;
                        rayOrigin = tmp;
                        tmp.x = rayDirection.z;
                        tmp.z = rayDirection.y;
                        tmp.y = -rayDirection.x;
                        rayDirection = tmp;
                    }
                    break;
                case Alignment.Align_X_Z:
                    {
                        // already in X/Z but values increase in -Z
                        rayOrigin.z = -rayOrigin.z;
                        rayDirection.z = -rayDirection.z;
                    }
                    break;
            }
            // readjust coordinate origin
            rayOrigin.x += mWorldSize / 2;
            rayOrigin.z += mWorldSize / 2;
            // scale down to vertex level
            rayOrigin.x /= mScale;
            rayOrigin.z /= mScale;
            rayDirection.x /= mScale;
            rayDirection.z /= mScale;
            rayDirection.Normalize();
            Ray localRay = new Ray( rayOrigin, rayDirection );

            // test if the ray actually hits the terrain's bounds
            float maxHeight = MaxHeight;
            float minHeight = MinHeight;
            AxisAlignedBox aabb = new AxisAlignedBox( new Vector3( 0, minHeight, 0 ), new Vector3( mSize, maxHeight, mSize ) );
            IntersectResult aabbTest = localRay.Intersects( aabb );
            if ( !aabbTest.Hit )
            {
                result = new KeyValuePair<bool, Vector3>( false, Vector3.Zero );
                return result;
            }

            //get intersection point and move inside
            Vector3 cur = localRay.GetPoint( aabbTest.Distance );

            // now check every quad the ray touches
            int quadX = Utility.Min( Utility.Max( (int)cur.x, 0 ), (int)mSize - 2 );
            int quadZ = Utility.Min( Utility.Max( (int)cur.z, 0 ), (int)mSize - 2 );
            int flipX = ( rayDirection.x < 0 ? 0 : 1 );
            int flipZ = ( rayDirection.z < 0 ? 0 : 1 );
            int xDir = ( rayDirection.x < 0 ? -1 : 1 );
            int zDir = ( rayDirection.z < 0 ? -1 : 1 );

            result = new KeyValuePair<bool, Vector3>( true, Vector3.Zero );
            float dummyHighValue = mSize * 10000;
            while ( cur.y >= ( minHeight - 1e-3 ) && cur.y <= ( maxHeight + 1e-3 ) )
            {
                if ( quadX < 0 || quadX >= (int)mSize - 1 || quadZ < 0 || quadZ >= (int)mSize - 1 )
                    break;

                result = CheckQuadIntersection( quadX, quadZ, localRay );
                if ( result.Key )
                    break;

                // determine next quad to test

                float xDist = Utility.RealEqual( ray.Direction.x, 0.0f ) ? (Real)dummyHighValue :
                    ( quadX - cur.x + flipX ) / rayDirection.x;
                float zDist = Utility.RealEqual( ray.Direction.z, 0.0f ) ? (Real)dummyHighValue :
                    ( quadZ - cur.z + flipZ ) / rayDirection.z;
                if ( xDist < zDist )
                {
                    quadX += xDir;
                    cur += rayDirection * xDist;
                }
                else
                {
                    quadZ += zDir;
                    cur += rayDirection * zDist;
                }
            }

            if ( result.Key )
            {
                // transform the point of intersection back to world space
                Vector3 val = result.Value;
                val.x *= mScale;
                val.z *= mScale;
                val.x -= mWorldSize / 2;
                val.z -= mWorldSize / 2;
                switch ( Alignment )
                {
                    case Alignment.Align_X_Y:
                        {
                            Axiom.Math.Utility.Swap( ref val.y, ref val.z );
                        }
                        break;
                    case Alignment.Align_Y_Z:
                        {
                            // z = x, y = z, x = -y
                            tmp.x = -rayOrigin.y;
                            tmp.y = rayOrigin.z;
                            tmp.z = rayOrigin.x;
                            rayOrigin = tmp;
                        }
                        break;
                    case Alignment.Align_X_Z:
                        {
                            val.z = -val.z;
                        }
                        break;
                }
                val += Position;
                result = new KeyValuePair<bool, Vector3>( true, val );
            }
            return result;
        }

        public KeyValuePair<bool, Vector3> RayIntersects( Ray ray )
        {
            return RayIntersects( ray, false, 0 );
        }

        public KeyValuePair<bool, Vector3> RayIntersects( Ray ray, bool cascadeToNeighbours, float distanceLimit )
        {
            KeyValuePair<bool, Vector3> Result;
            // first step: convert the ray to a local vertex space
            // we assume terrain to be in the x-z plane, with the [0,0] vertex
            // at origin and a plane distance of 1 between vertices.
            // This makes calculations easier.
            Vector3 rayOrigin = ray.Origin - Position;
            Vector3 rayDirection = ray.Direction;
            // change alignment
            Vector3 tmp;
            switch ( Alignment )
            {
                case Alignment.Align_X_Y:
                    Utility.Swap<Real>( ref rayOrigin.y, ref rayOrigin.z );
                    Utility.Swap<Real>( ref rayDirection.y, ref rayDirection.z );
                    break;
                case Alignment.Align_Y_Z:
                    // x = z, z = y, y = -x
                    tmp.x = rayOrigin.z;
                    tmp.z = rayOrigin.y;
                    tmp.y = -rayOrigin.x;
                    rayOrigin = tmp;
                    tmp.x = rayDirection.z;
                    tmp.z = rayDirection.y;
                    tmp.y = -rayDirection.x;
                    rayDirection = tmp;
                    break;
                case Alignment.Align_X_Z:
                    // already in X/Z but values increase in -Z
                    rayOrigin.z = -rayOrigin.z;
                    rayDirection.z = -rayDirection.z;
                    break;
            }
            // readjust coordinate origin
            rayOrigin.x += mWorldSize / 2;
            rayOrigin.z += mWorldSize / 2;
            // scale down to vertex level
            rayOrigin.x /= mScale;
            rayOrigin.z /= mScale;
            rayDirection.x /= mScale;
            rayDirection.z /= mScale;
            rayDirection.Normalize();
            Ray localRay = new Ray( rayOrigin, rayDirection );

            // test if the ray actually hits the terrain's bounds
            Real maxHeight = MaxHeight;
            Real minHeight = MinHeight;

            AxisAlignedBox aabb = new AxisAlignedBox( new Vector3( 0, minHeight, 0 ), new Vector3( mSize, maxHeight, mSize ) );
            IntersectResult aabbTest = localRay.Intersects( aabb );
            if ( !aabbTest.Hit )
            {
                if ( cascadeToNeighbours )
                {
                    Terrain neighbour = RaySelectNeighbour( ray, distanceLimit );
                    if ( neighbour != null )
                        return neighbour.RayIntersects( ray, cascadeToNeighbours, distanceLimit );
                }
                return new KeyValuePair<bool, Vector3>( false, new Vector3() );
            }
            // get intersection point and move inside
            Vector3 cur = localRay.GetPoint( aabbTest.Distance );

            // now check every quad the ray touches
            int quadX = Utility.Min( Utility.Max( (int)( cur.x ), 0 ), (int)mSize - 2 );
            int quadZ = Utility.Min( Utility.Max( (int)( cur.z ), 0 ), (int)mSize - 2 );
            int flipX = ( rayDirection.x < 0 ? 0 : 1 );
            int flipZ = ( rayDirection.z < 0 ? 0 : 1 );
            int xDir = ( rayDirection.x < 0 ? -1 : 1 );
            int zDir = ( rayDirection.z < 0 ? -1 : 1 );

            Result = new KeyValuePair<bool, Vector3>( true, Vector3.Zero );
            float dummyHighValue = mSize * 10000;


            while ( cur.y >= ( minHeight - 1e-3 ) && cur.y <= ( maxHeight + 1e-3 ) )
            {
                if ( quadX < 0 || quadX >= (int)mSize - 1 || quadZ < 0 || quadZ >= (int)mSize - 1 )
                    break;

                Result = CheckQuadIntersection( quadX, quadZ, localRay );
                if ( Result.Key )
                    break;

                // determine next quad to test
                Real xDist = Utility.RealEqual( rayDirection.x, 0.0f ) ? (Real)dummyHighValue :
                    ( quadX - cur.x + flipX ) / rayDirection.x;
                Real zDist = Utility.RealEqual( rayDirection.z, 0.0f ) ? (Real)dummyHighValue :
                    ( quadZ - cur.z + flipZ ) / rayDirection.z;
                if ( xDist < zDist )
                {
                    quadX += xDir;
                    cur += rayDirection * xDist;
                }
                else
                {
                    quadZ += zDir;
                    cur += rayDirection * zDist;
                }

            }
            Vector3 resVec = Vector3.Zero;
            if ( Result.Key )
            {
                // transform the point of intersection back to world space
                resVec = Result.Value;
                resVec.x *= mScale;
                resVec.z *= mScale;
                resVec.x -= mWorldSize / 2;
                resVec.z -= mWorldSize / 2;
                switch ( Alignment )
                {
                    case Alignment.Align_X_Y:
                        Utility.Swap<Real>( ref resVec.y, ref resVec.z );
                        break;
                    case Alignment.Align_Y_Z:
                        // z = x, y = z, x = -y
                        tmp.x = -rayOrigin.y;
                        tmp.y = rayOrigin.z;
                        tmp.z = rayOrigin.x;
                        rayOrigin = tmp;
                        break;
                    case Alignment.Align_X_Z:
                        resVec.z = -resVec.z;
                        break;
                }
                resVec += Position;
            }
            else if ( cascadeToNeighbours )
            {
                Terrain neighbour = RaySelectNeighbour( ray, distanceLimit );
                if ( neighbour != null )
                    Result = neighbour.RayIntersects( ray, cascadeToNeighbours, distanceLimit );
            }
            return new KeyValuePair<bool, Vector3>( Result.Key, resVec );
        }

#warning implement void sceneManagerDestroyed(SceneManager* source);

        /// <summary>
        /// Retrieve the layer blending map for a given layer, which may
        ///	be used to edit the blending information for that layer.
        /// </summary>
        /// <note>
        /// You can only do this after the terrain has been loaded. You may 
        ///	edit the content of the blend layer in another thread, but you
        ///	may only upload it in the main render thread.
        /// </note>
        /// <param name="layerIndex">The layer index, which should be 1 or higher (since 
        ///	the bottom layer has no blending).</param>
        /// <returns>Pointer to the TerrainLayerBlendMap requested. The caller must
        ///	not delete this instance, use freeTemporaryResources if you want
        ///	to save the memory after completing your editing.</returns>
        public TerrainLayerBlendMap GetLayerBlendMap( byte layerIndex )
        {
            if ( layerIndex == 0 || layerIndex - 1 >= (byte)mLayerBlendMapList.Count )
                throw new Exception( "Invalid layer index. Terrain.GetLayerBlendMap" );

            byte idx = (byte)( layerIndex - 1 );
            if ( mLayerBlendMapList[ idx ] == null )
            {
                if ( mBlendTextureList.Count < (int)( idx / 4 ) )
                    CheckLayers( true );

                Texture tex = mBlendTextureList[ idx / 4 ];
                mLayerBlendMapList[ idx ] = new TerrainLayerBlendMap( this, layerIndex, tex.GetBuffer() );
            }

            return mLayerBlendMapList[ idx ];
        }
        /// <summary>
        ///  Get the index of the blend texture that a given layer uses.
        /// </summary>
        /// <param name="layerIndex">The layer index, must be >= 1 and less than the number
        ///	of layers</param>
        /// <returns>The index of the shared blend texture</returns>
        public byte GetBlendTextureIndex( byte layerIndex )
        {
            if ( layerIndex == 0 || layerIndex - 1 >= (byte)mLayerBlendMapList.Count )
                throw new Exception( "Invalid layer index, Terrain.GetBlendTextureIndex" );

            return (byte)( layerIndex - 1 % 4 );
        }
        /// <summary>
        /// Get the number of blend textures in use
        /// </summary>
        /// <returns></returns>
        public byte GetBlendTextureCount()
        {
            return (byte)mBlendTextureList.Count;
        }
        /// <summary>
        /// Get the number of blend textures needed for a given number of layers
        /// </summary>
        /// <param name="numLayers"></param>
        /// <returns></returns>
        public byte GetBlendTextureCount( byte numLayers )
        {
            return (byte)( ( ( numLayers - 1 ) / 4 ) + 1 );
        }
        /// <summary>
        /// Get the name of the packed blend texture at a specific index.
        /// </summary>
        /// <param name="textureIndex">This is the blend texture index, not the layer index
        ///	(multiple layers will share a blend texture)</param>
        /// <returns></returns>
        public string GetBlendTextureName( byte textureIndex )
        {
            if ( textureIndex >= (byte)mBlendTextureList.Count )
                throw new Exception( "Invalid texture index, Terrain.GetBlendTextureName" );

            return mBlendTextureList[ textureIndex ].Name;
        }
        /// <summary>
        ///  Set whether a global colour map is enabled. 
        /// </summary>
        /// <remarks>
        /// A global colour map can add variation to your terrain and reduce the 
        ///	perceived tiling effect you might get in areas of continuous lighting
        ///	and the same texture. 
        ///	The global colour map is only used when the material generator chooses
        ///	to use it.
        /// </remarks>
        /// <note>
        /// You must only call this from the main render thread
        /// </note>
        /// <param name="enable">Whether the global colour map is enabled or not</param>
        public void SetGlobalColorMapEnabled( bool enabled )
        {
            SetGlobalColorMapEnabled( enabled, 0 );
        }
        /// <summary>
        ///  Set whether a global colour map is enabled. 
        /// </summary>
        /// <remarks>
        /// A global colour map can add variation to your terrain and reduce the 
        ///	perceived tiling effect you might get in areas of continuous lighting
        ///	and the same texture. 
        ///	The global colour map is only used when the material generator chooses
        ///	to use it.
        /// </remarks>
        /// <note>
        /// You must only call this from the main render thread
        /// </note>
        /// <param name="enable">Whether the global colour map is enabled or not</param>
        /// <param name="size">the resolution of the color map. A value of zero means 'no change'
        ///	and the default is set in TerrainGlobalOptions.</param>
        [OgreVersion( 1, 7, 2 )]
        public void SetGlobalColorMapEnabled( bool enabled, ushort sz )
        {
            if ( sz == 0 )
                sz = TerrainGlobalOptions.DefaultGlobalColorMapSize;

            if ( enabled != this.IsGlobalColorMapEnabled ||
                ( enabled && this.GlobalColorMapSize != sz ) )
            {
                this.IsGlobalColorMapEnabled = enabled;
                this.GlobalColorMapSize = sz;

                CreateOrDestroyGPUColorMap();

                mMaterialDirty = true;
                mMaterialParamsDirty = true;
                this.IsModified = true;
            }
        }

        public void WidenRectByVector( Vector3 vec, Rectangle inRect, float minHeight, float maxHeight, ref Rectangle outRect )
        {
            outRect = inRect;

            Plane p = new Plane();

            switch ( Alignment )
            {
                case Alignment.Align_X_Y:
                    {
                        p.Redefine( Vector3.UnitZ, new Vector3( 0, 0, vec.z < 0.0f ? minHeight : maxHeight ) );
                    }
                    break;
                case Alignment.Align_X_Z:
                    {
                        p.Redefine( Vector3.UnitY, new Vector3( 0, vec.y < 0.0f ? minHeight : maxHeight, 0 ) );
                    }
                    break;
                case Alignment.Align_Y_Z:
                    {
                        p.Redefine( Vector3.UnitX, new Vector3( vec.x < 0.0f ? minHeight : maxHeight, 0, 0 ) );
                    }
                    break;
            }
            float verticalVal = vec.Dot( p.Normal );

            if ( Utility.RealEqual( verticalVal, 0.0f ) )
                return;

            Vector3[] corners = new Vector3[ 4 ];
            float startHeight = verticalVal < 0.0f ? maxHeight : minHeight;
            GetPoint( inRect.Left, inRect.Top, startHeight, ref corners[ 0 ] );
            GetPoint( inRect.Right - 1, inRect.Top, startHeight, ref corners[ 1 ] );
            GetPoint( inRect.Left, inRect.Bottom - 1, startHeight, ref corners[ 2 ] );
            GetPoint( inRect.Right - 1, inRect.Bottom - 1, startHeight, ref corners[ 3 ] );

            for ( int i = 0; i < 4; ++i )
            {
                Ray ray = new Ray( corners[ i ] + mPos, vec );
                IntersectResult rayHit = ray.Intersects( p );
                if ( rayHit.Hit )
                {
                    Vector3 pt = ray.GetPoint( rayHit.Distance );
                    // convert back to terrain point
                    Vector3 terrainHitPos = Vector3.Zero;
                    GetTerrainPosition( pt, ref terrainHitPos );
                    // build rectangle which has rounded down & rounded up values
                    // remember right & bottom are exclusive
                    Rectangle mergeRect = new Rectangle(
                        (long)terrainHitPos.x * ( mSize - 1 ),
                        (long)terrainHitPos.y * ( mSize - 1 ),
                        (long)( terrainHitPos.x * (float)( mSize - 1 ) + 0.5f ) + 1,
                        (long)( terrainHitPos.x * (float)( mSize - 1 ) + 0.5f ) + 1
                        );
                }
            }
        }
        /// <summary>
        /// Widen a rectangular area of terrain to take into account an extrusion vector.
        /// </summary>
        /// <param name="vec"> A vector in world space</param>
        /// <param name="inRec">Input rectangle</param>
        /// <param name="outRect">Output rectangle</param>
        public void WidenRectByVector( Vector3 vec, Rectangle inRect, ref Rectangle outRect )
        {
            WidenRectByVector( vec, inRect, MinHeight, MaxHeight, ref outRect );
        }
        /// <summary>
        /// Free as many resources as possible for optimal run-time memory use.
        /// </summary>
        /// <remarks>
        /// This class keeps some temporary storage around in order to make
        ///	certain actions (such as editing) possible more quickly. Calling this
        ///	method will cause as many of those resources as possible to be
        ///	freed. You might want to do this for example when you are finished
        ///	editing a particular terrain and want to have optimal runtime
        ///	efficiency.
        /// </remarks>
        public void FreeTemporaryResources()
        {
            // cpu blend maps
            mCpuBlendMapStorage.Clear();

            // Editable structures for blend layers (not needed at runtime,  only blend textures are)
            // mLayerBlendMapList.Clear();
        }
        /// <summary>
        /// Get a blend texture with a given index.
        /// </summary>
        /// <param name="index">The blend texture index (note: not layer index; derive
        /// the texture index from getLayerBlendTextureIndex)</param>
        public Texture GetLayerBlendTexture( byte index )
        {
            System.Diagnostics.Debug.Assert( index < mBlendTextureList.Count, "Given index is out of Bound!" );

            return mBlendTextureList[ index ];
        }
        /// <summary>
        /// Get the texture index and colour channel of the blend information for 
        ///	a given layer. 
        /// </summary>
        /// <param name="layerIndex">The index of the layer (1 or higher, layer 0 has no blend data)</param>
        /// <returns>A pair in which the first value is the texture index, and the 
        ///	second value is the colour channel (RGBA)</returns>
        public KeyValuePair<byte, byte> GetLayerBlendTextureIndex( byte layerIndex )
        {
            System.Diagnostics.Debug.Assert( layerIndex > 0 && layerIndex < mLayers.Count, "Given index is out of Bound!" );
            byte idx = (byte)( layerIndex - 1 );
            return new KeyValuePair<byte, byte>( (byte)( idx / 4 ), (byte)( idx % 4 ) );
        }
        /// <summary>
        /// Request internal implementation options for the terrain material to use, 
        /// in this case a terrain-wide normal map. 
        /// The TerrainMaterialGenerator should call this method to specify the 
        /// options it would like to use when creating a material. Not all the data
        /// is guaranteed to be up to date on return from this method - for example some
        /// maps may be generated in the background. However, on return from this method
        /// all the features that are requested will be referenceable by materials, the
        /// data may just take a few frames to be fully populated.
        /// </summary>
        /// <param name="lightMap">Whether a terrain-wide lightmap including precalculated 
        ///	lighting is required (light direction in TerrainGlobalOptions)</param>
        public void SetLightMapRequired( bool lightMap )
        {
            SetLightMapRequired( lightMap, false );
        }
        /// <summary>
        /// Request internal implementation options for the terrain material to use, 
        /// in this case a terrain-wide normal map. 
        /// The TerrainMaterialGenerator should call this method to specify the 
        /// options it would like to use when creating a material. Not all the data
        /// is guaranteed to be up to date on return from this method - for example some
        /// maps may be generated in the background. However, on return from this method
        /// all the features that are requested will be referenceable by materials, the
        /// data may just take a few frames to be fully populated.
        /// </summary>
        /// <param name="lightMap">Whether a terrain-wide lightmap including precalculated 
        ///	lighting is required (light direction in TerrainGlobalOptions)</param>
        /// <param name="shadowsOnly">If true, the lightmap contains only shadows, 
        ///	no directional lighting intensity</param>
        [OgreVersion( 1, 7, 2 )]
        public void SetLightMapRequired( bool lightMap, bool shadowsOnly )
        {
            if ( lightMap != mLightMapRequired || shadowsOnly != mLightMapShadowsOnly )
            {
                mLightMapRequired = lightMap;
                mLightMapShadowsOnly = shadowsOnly;

                CreateOrDestroyGPULightmap();

                // if we enabled, generate light maps
                if ( mLightMapRequired )
                {
                    // update derived data for whole terrain, but just lightmap
                    mDirtyDerivedDataRect = new Rectangle();
                    mDirtyDerivedDataRect.Left = mDirtyDerivedDataRect.Top = 0;
                    mDirtyDerivedDataRect.Right = mDirtyDerivedDataRect.Bottom = mSize;
                    UpdateDerivedData( false, DERIVED_DATA_LIGHTMAP );
                }
            }
        }
#warning implement WorkerQueue class
#if false
        		/// WorkQueue::RequestHandler override
		bool canHandleRequest(const WorkQueue::Request* req, const WorkQueue* srcQ);
		/// WorkQueue::RequestHandler override
		WorkQueue::Response* handleRequest(const WorkQueue::Request* req, const WorkQueue* srcQ);
		/// WorkQueue::ResponseHandler override
		bool canHandleResponse(const WorkQueue::Response* res, const WorkQueue* srcQ);
		/// WorkQueue::ResponseHandler override
		void handleResponse(const WorkQueue::Response* res, const WorkQueue* srcQ);
#endif
        /// <summary>
        /// Utility method, get the first LOD Level at which this vertex is no longer included
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public ushort GetLODLevelWhenVertexEliminated( long x, long y )
        {
            // gets eliminated by either row or column first
            return Utility.Min( GetLODLevelWhenVertexEliminated( x ), GetLODLevelWhenVertexEliminated( y ) );
        }
        /// <summary>
        /// Utility method, get the first LOD Level at which this vertex is no longer included
        /// </summary>
        /// <param name="rowOrColulmn"></param>
        /// <returns></returns>
        public ushort GetLODLevelWhenVertexEliminated( long rowOrColulmn )
        {
            // LOD levels bisect the domain.
            // start at the lowest detail
            ushort currentElim = (ushort)( ( mSize - 1 ) / ( mMinBatchSize - 1 ) );
            // start at a non-exitant LOD index, this applies to the min batch vertices
            // which are never eliminated
            ushort currentLod = this.NumLodLevels;

            while ( rowOrColulmn % currentElim != 0 )
            {
                // not on this boundary, look finer
                currentElim = (ushort)( currentElim / 2 );
                --currentLod;
                // This will always terminate since (anything % 1 == 0)
            }

            return currentLod;
        }

        public static void ConvertWorldToTerrainAxes( Alignment aling, Vector3 worldVec, out Vector3 terrainVec )
        {
            throw new NotImplementedException();
        }

        public static void ConvertTerrainToWorldAxes( Alignment align, Vector3 terrainVec, out Vector3 worldVec )
        {
            worldVec = new Vector3();
            switch ( align )
            {
                case Alignment.Align_X_Z:
                    worldVec.x = terrainVec.x;
                    worldVec.y = terrainVec.z;
                    worldVec.z = -terrainVec.y;
                    break;

                case Alignment.Align_Y_Z:
                    worldVec.x = terrainVec.z;
                    worldVec.y = terrainVec.y;
                    worldVec.z = -terrainVec.x;
                    break;

                case Alignment.Align_X_Y:
                    worldVec = terrainVec;
                    break;
            }
        }

        public static NeighbourIndex GetNeighbourIndex( long x, long y )
        {
            if ( x < 0 )
            {
                if ( y < 0 )
                    return NeighbourIndex.SouthWest;
                else if ( y > 0 )
                    return NeighbourIndex.NorthWest;
                else
                    return NeighbourIndex.West;
            }

            else if ( x > 0 )
            {
                if ( y < 0 )
                    return NeighbourIndex.SouthEast;
                else if ( y > 0 )
                    return NeighbourIndex.NorthEast;
                else
                    return NeighbourIndex.East;
            }

            if ( y < 0 )
            {
                if ( x == 0 )
                    return NeighbourIndex.South;
            }
            else if ( y > 0 )
            {
                if ( x == 0 )
                    return NeighbourIndex.North;
            }

            return NeighbourIndex.North;
        }

        /// <summary>
        /// Utility method to write a layer declaration to a stream
        /// </summary>
        [OgreVersion( 1, 7, 2 )]
        public static void WriteLayerDeclaration( TerrainLayerDeclaration decl, ref StreamSerializer stream )
        {
            // Layer declaration
            stream.WriteChunkBegin( TERRAINLAYERDECLARATION_CHUNK_ID, TERRAINLAYERDECLARATION_CHUNK_VERSION );

            //  samplers
            byte numSamplers = (byte)decl.Samplers.Count;
            stream.Write( numSamplers );
            foreach ( var sampler in decl.Samplers )
            {
                stream.WriteChunkBegin( TERRAINLAYERSAMPLER_CHUNK_ID, TERRAINLAYERSAMPLER_CHUNK_VERSION );
                stream.Write( sampler.Alias );
                byte pixFmt = (byte)sampler.Format;
                stream.Write( pixFmt );
                stream.WriteChunkEnd( TERRAINLAYERSAMPLER_CHUNK_ID );
            }

            //  elements
            byte numElems = (byte)decl.Elements.Count;
            stream.Write( numElems );
            foreach ( var elem in decl.Elements )
            {
                stream.WriteChunkBegin( TERRAINLAYERSAMPLERELEMENT_CHUNK_ID, TERRAINLAYERSAMPLERELEMENT_CHUNK_VERSION );
                stream.Write( elem.Source );
                byte sem = (byte)elem.Semantic;
                stream.Write( sem );
                stream.Write( elem.ElementStart );
                stream.Write( elem.ElementCount );
                stream.WriteChunkEnd( TERRAINLAYERSAMPLERELEMENT_CHUNK_ID );
            }
            stream.WriteChunkEnd( TERRAINLAYERDECLARATION_CHUNK_ID );
        }

        /// <summary>
        /// Utility method to write a layer instance list to a stream
        /// </summary>
        [OgreVersion( 1, 7, 2 )]
        public static void WriteLayerInstanceList( List<LayerInstance> layers, ref StreamSerializer stream )
        {
            byte numLayers = (byte)layers.Count;
            stream.Write( numLayers );
            foreach ( var inst in layers )
            {
                stream.WriteChunkBegin( TERRAINLAYERINSTANCE_CHUNK_ID, TERRAINLAYERINSTANCE_CHUNK_VERSION );
                stream.Write( inst.WorldSize );
                foreach ( var t in inst.TextureNames )
                    stream.Write( t );

                stream.WriteChunkEnd( TERRAINLAYERINSTANCE_CHUNK_ID );
            }
        }

        /// <summary>
        /// Utility method to read a layer declaration from a stream
        /// </summary>
        [OgreVersion( 1, 7, 2 )]
        public static bool ReadLayerDeclaration( ref StreamSerializer stream, ref TerrainLayerDeclaration targetDecl )
        {
            if ( stream.ReadChunkBegin( TERRAINLAYERDECLARATION_CHUNK_ID, TERRAINLAYERDECLARATION_CHUNK_VERSION ) == null )
                return false;

            //  samplers
            byte numSamplers;
            stream.Read( out numSamplers );
            targetDecl.Samplers = new List<TerrainLayerSampler>( numSamplers );
            for ( byte s = 0; s < numSamplers; ++s )
            {
                if ( stream.ReadChunkBegin( TERRAINLAYERSAMPLER_CHUNK_ID, TERRAINLAYERSAMPLER_CHUNK_VERSION ) == null )
                    return false;

                TerrainLayerSampler sampler = new TerrainLayerSampler();
                stream.Read( out sampler.Alias );
                byte pixFmt;
                stream.Read( out pixFmt );
                sampler.Format = (PixelFormat)pixFmt;
                stream.ReadChunkEnd( TERRAINLAYERSAMPLER_CHUNK_ID );
                targetDecl.Samplers.Add( sampler );
            }

            //  elements
            byte numElems;
            stream.Read( out numElems );
            targetDecl.Elements = new List<TerrainLayerSamplerElement>( numElems );
            for ( byte e = 0; e < numElems; ++e )
            {
                if ( stream.ReadChunkBegin( TERRAINLAYERSAMPLERELEMENT_CHUNK_ID, TERRAINLAYERSAMPLERELEMENT_CHUNK_VERSION ) == null )
                    return false;

                TerrainLayerSamplerElement samplerElem = new TerrainLayerSamplerElement();

                stream.Read( out samplerElem.Source );
                byte sem;
                stream.Read( out sem );
                samplerElem.Semantic = (TerrainLayerSamplerSemantic)sem;
                stream.Read( out samplerElem.ElementStart );
                stream.Read( out samplerElem.ElementCount );
                stream.ReadChunkEnd( TERRAINLAYERSAMPLERELEMENT_CHUNK_ID );
                targetDecl.Elements.Add( samplerElem );
            }
            stream.ReadChunkEnd( TERRAINLAYERDECLARATION_CHUNK_ID );

            return true;
        }

        /// <summary>
        /// Utility method to read a layer instance list from a stream
        /// </summary>
        [OgreVersion( 1, 7, 2 )]
        public static bool ReadLayerInstanceList( ref StreamSerializer stream, int numSamplers, ref List<LayerInstance> targetLayers )
        {
            byte numLayers;
            stream.Read( out numLayers );
            targetLayers = new List<LayerInstance>( numLayers );
            for ( byte l = 0; l < numLayers; ++l )
            {
                if ( stream.ReadChunkBegin( TERRAINLAYERINSTANCE_CHUNK_ID, TERRAINLAYERINSTANCE_CHUNK_VERSION ) == null )
                    return false;

                LayerInstance inst = new LayerInstance();

                stream.Read( out inst.WorldSize );
                inst.TextureNames = new List<string>( numSamplers );
                for ( int t = 0; t < numSamplers; ++t )
                {
                    string texName;
                    stream.Read( out texName );
                    inst.TextureNames.Add( texName );
                }

                stream.ReadChunkEnd( TERRAINLAYERINSTANCE_CHUNK_ID );
                targetLayers.Add( inst );
            }

            return true;
        }

        #endregion - public functions -

        #region - protected functions -

        [OgreVersion( 1, 7, 2 )]
        protected void FreeCPUResources()
        {
            mHeightData = null;
            mDeltaDataPtr = null;
            this.QuadTree = null;
            if ( mCpuTerrainNormalMap != null )
            {
                mCpuTerrainNormalMap.Data = null;
                mCpuTerrainNormalMap = null;
            }

            mCpuColorMapStorage = null;
            mCpuLightmapStorage = null;
            mCpuCompositeMapStorage = null;
        }

        protected KeyValuePair<bool, Vector3> CheckQuadIntersection( int x, int z, Ray ray )
        {
            //build the two planes belonging to the quad's triangles
            //float* val = (float*)GetHeightData(x, z);
            //v1F = *val;
            //val = (float*)GetHeightData(x+1,z);
            //v2F = *val;
            //val = (float*)GetHeightData(x,z+1);
            //v3F = *val;
            //val = (float*)GetHeightData(x + 1, z + 1);
            //v4F = *val;
            //float vv = ;

            Vector3 v1 = new Vector3( x, mHeightData[ z + mSize * x ], z ),
                    v2 = new Vector3( x + 1, mHeightData[ z + mSize * ( x + 1 ) ], z ),
                    v3 = new Vector3( x, mHeightData[ ( z + 1 ) + mSize * x ], z + 1 ),
                    v4 = new Vector3( x + 1, mHeightData[ ( z + 1 ) + mSize * ( x + 1 ) ], z + 1 );

            Plane p1 = new Plane(),
                  p2 = new Plane();
            bool oddRow = false;
            if ( z % 2 != 0 )
            {
                /* odd
                    3---4
                    | \ |
                    1---2
                    */
                p1.Redefine( v2, v4, v3 );
                p2.Redefine( v1, v2, v3 );
                oddRow = true;
            }
            else
            {
                /* even
                    3---4
                    | / |
                    1---2
                    */
                p1.Redefine( v1, v2, v4 );
                p2.Redefine( v1, v4, v3 );
            }
            // Test for intersection with the two planes. 
            // Then test that the intersection points are actually
            // still inside the triangle (with a small error margin)
            // Also check which triangle it is in
            IntersectResult planeInt = ray.Intersects( p1 );
            if ( planeInt.Hit )
            {
                Vector3 where = ray.GetPoint( planeInt.Distance );
                Vector3 rel = where - v1;
                if ( rel.x >= -0.01 && rel.x <= 1.01 && rel.z >= -0.01 && rel.z <= 1.01 // quad bounds
                    && ( ( rel.x >= rel.z && !oddRow ) || ( rel.x >= ( 1 - rel.z ) && oddRow ) ) ) // triangle bounds
                    return new KeyValuePair<bool, Vector3>( true, where );
            }
            planeInt = ray.Intersects( p2 );
            if ( planeInt.Hit )
            {
                Vector3 where = ray.GetPoint( planeInt.Distance );
                Vector3 rel = where - v1;
                if ( rel.x >= -0.01 && rel.x <= 1.01 && rel.z >= -0.01 && rel.z <= 1.01 // quad bounds
                    && ( ( rel.x <= rel.z && !oddRow ) || ( rel.x <= ( 1 - rel.z ) && oddRow ) ) ) // triangle bounds
                    return new KeyValuePair<bool, Vector3>( true, where );
            }

            return new KeyValuePair<bool, Vector3>( false, Vector3.Zero );
        }

        [OgreVersion( 1, 7, 2 )]
        protected void FreeGPUResources()
        {
            //remove textures
            TextureManager tmgr = TextureManager.Instance;
            if ( tmgr != null )
            {
                foreach ( Texture tex in mBlendTextureList )
                {
                    tmgr.Remove( tex );
                }
                mBlendTextureList.Clear();

                if ( this.TerrainNormalMap != null )
                {
                    tmgr.Remove( this.TerrainNormalMap );
                    this.TerrainNormalMap = null;
                }

                if ( this.GlobalColorMap != null )
                {
                    tmgr.Remove( this.GlobalColorMap );
                    this.GlobalColorMap = null;
                }

                if ( this.LightMap != null )
                {
                    tmgr.Remove( this.LightMap );
                    this.LightMap = null;
                }

                if ( this.CompositeMap != null )
                {
                    tmgr.Remove( this.CompositeMap );
                    this.CompositeMap = null;
                }

                if ( mMaterial != null )
                {
                    MaterialManager.Instance.Remove( mMaterial );
                    mMaterial = null;
                }

                if ( mCompositeMapMaterial != null )
                {
                    MaterialManager.Instance.Remove( mCompositeMapMaterial );
                    mCompositeMapMaterial = null;
                }
            }
        }

        protected void DetermineLodLevels()
        {
            /* On a leaf-node basis, LOD can vary from maxBatch to minBatch in 
             number of vertices. After that, nodes will be gathered into parent
             nodes with the same number of vertices, but they are combined with
             3 of their siblings. In practice, the number of LOD levels overall 
             is:
                 LODlevels = log2(size - 1) - log2(minBatch - 1) + 1
                 TreeDepth = log2((size - 1) / (maxBatch - 1)) + 1

             .. it's just that at the max LOD, the terrain is divided into 
             (size - 1) / (maxBatch - 1) tiles each of maxBatch vertices, and
             at the lowest LOD the terrain is made up of one single tile of 
             minBatch vertices. 

             Example: size = 257, minBatch = 17, maxBatch = 33

             LODlevels = log2(257 - 1) - log2(17 - 1) + 1 = 8 - 4 + 1 = 5
             TreeDepth = log2((size - 1) / (maxBatch - 1)) + 1 = 4

             LOD list - this assumes everything changes at once, which rarely happens of course
                        in fact except where groupings must occur, tiles can change independently
             LOD 0: 257 vertices, 8 x 33 vertex tiles (tree depth 3)
             LOD 1: 129 vertices, 8 x 17 vertex tiles (tree depth 3)
             LOD 2: 65  vertices, 4 x 17 vertex tiles (tree depth 2)
             LOD 3: 33  vertices, 2 x 17 vertex tiles (tree depth 1)
             LOD 4: 17  vertices, 1 x 17 vertex tiles (tree depth 0)

             Notice how we only have 2 sizes of index buffer to be concerned about,
             17 vertices (per side) or 33. This makes buffer re-use much easier while
             still giving the full range of LODs.
             * mNumLodLevelsPerLeafNode = Math::Log2(mMaxBatchSize - 1) - Math::Log2(mMinBatchSize - 1) + 1;
        mNumLodLevels = Math::Log2(mSize - 1) - Math::Log2(mMinBatchSize - 1) + 1;
        //mTreeDepth = Math::Log2(mMaxBatchSize - 1) - Math::Log2(mMinBatchSize - 1) + 2;
        mTreeDepth = mNumLodLevels - mNumLodLevelsPerLeafNode + 1;
         */

            this.NumLodLevelsPerLeaf = (ushort)( Utility.Log2( mMaxBatchSize - 1 ) - Utility.Log2( mMinBatchSize - 1 ) + 1 );
            this.NumLodLevels = (ushort)( Utility.Log2( mSize - 1 ) - Utility.Log2( mMinBatchSize - 1 ) + 1 );
            mTreeDepth = (ushort)( this.NumLodLevels - this.NumLodLevelsPerLeaf + 1 );

            LogManager.Instance.Write( string.Format( "Terrain created; size={0}, minBatch={1}, maxBatch={2}, treedepth={3}, lodLevels={4}, leafNodes={5}",
                mSize, mMinBatchSize, mMaxBatchSize, mTreeDepth, this.NumLodLevels, this.NumLodLevelsPerLeaf ), null );
        }

        [OgreVersion( 1, 7, 2 )]
        protected void DistributeVertexData()
        {
            /* Now we need to figure out how to distribute vertex data. We want to 
            use 16-bit indexes for compatibility, which means that the maximum patch
            size that we can address (even sparsely for lower LODs) is 129x129 
            (the next one up, 257x257 is too big). 

            So we need to split the vertex data into chunks of 129. The number of 
            primary tiles this creates also indicates the point above which in
            the node tree that we can no longer merge tiles at lower LODs without
            using different vertex data. For example, using the 257x257 input example
            above, the vertex data would have to be split in 2 (in each dimension)
            in order to fit within the 129x129 range. This data could be shared by
            all tree depths from 1 onwards, it's just that LODs 3-1 would sample 
            the 129x129 data sparsely. LOD 0 would sample all of the vertices.

            LOD 4 however, the lowest LOD, could not work with the same vertex data
            because it needs to cover the entire terrain. There are 2 choices here:
            create another set of vertex data at 17x17 which is only used by LOD 4, 
            or make LOD 4 occur at tree depth 1 instead (ie still split up, and 
            rendered as 2x9 along each edge instead. 

            Since rendering very small batches is not desirable, and the vertex counts
            are inherently not going to be large, creating a separate vertex set is
            preferable. This will also be more efficient on the vertex cache with
            distant terrains. 

            We probably need a larger example, because in this case only 1 level (LOD 0)
            needs to use this separate vertex data. Higher detail terrains will need
            it for multiple levels, here's a 2049x2049 example with 65 / 33 batch settings:

            LODlevels = log2(2049 - 1) - log2(33 - 1) + 1 = 11 - 5 + 1 = 7
            TreeDepth = log2((2049 - 1) / (65 - 1)) + 1 = 6
            Number of vertex data splits at most detailed level: 
            (size - 1) / (TERRAIN_MAX_BATCH_SIZE - 1) = 2048 / 128 = 16

            LOD 0: 2049 vertices, 32 x 65 vertex tiles (tree depth 5) vdata 0-15  [129x16]
            LOD 1: 1025 vertices, 32 x 33 vertex tiles (tree depth 5) vdata 0-15  [129x16]
            LOD 2: 513  vertices, 16 x 33 vertex tiles (tree depth 4) vdata 0-15  [129x16]
            LOD 3: 257  vertices, 8  x 33 vertex tiles (tree depth 3) vdata 16-17 [129x2] 
            LOD 4: 129  vertices, 4  x 33 vertex tiles (tree depth 2) vdata 16-17 [129x2]
            LOD 5: 65   vertices, 2  x 33 vertex tiles (tree depth 1) vdata 16-17 [129x2]
            LOD 6: 33   vertices, 1  x 33 vertex tiles (tree depth 0) vdata 18    [33]

            All the vertex counts are to be squared, they are just along one edge. 
            So as you can see, we need to have 3 levels of vertex data to satisy this
            (admittedly quite extreme) case, and a total of 19 sets of vertex data. 
            The full detail geometry, which is  16(x16) sets of 129(x129), used by 
            LODs 0-2. LOD 3 can't use this set because it needs to group across them, 
            because it has only 8 tiles, so we make another set which satisfies this 
            at a maximum of 129 vertices per vertex data section. In this case LOD 
            3 needs 257(x257) total vertices so we still split into 2(x2) sets of 129. 
            This set is good up to and including LOD 5, but LOD 6 needs a single 
            contiguous set of vertices, so we make a 33x33 vertex set for it. 

            In terms of vertex data stored, this means that while our primary data is:
            2049^2 = 4198401 vertices
            our final stored vertex data is 
            (16 * 129)^2 + (2 * 129)^2 + 33^2 = 4327749 vertices

            That equals a 3% premium, but it's both necessary and worth it for the
            reduction in batch count resulting from the grouping. In addition, at
            LODs 3 and 6 (or rather tree depth 3 and 0) there is the opportunity 
            to free up the vertex data used by more detailed LODs, which is
            important when dealing with large terrains. For example, if we
            freed the (GPU) vertex data for LOD 0-2 in the medium distance, 
            we would save 98% of the memory overhead for this terrain. 

        */
            LogManager logMgr = LogManager.Instance;
            logMgr.Write( LogMessageLevel.Trivial, false, @"Terrain.DistributeVertexData processing source 
                terrain size of " + mSize, null );

            ushort depth = mTreeDepth;
            ushort prevDepth = depth;
            ushort currentresolution = mSize;
            ushort bakedresolution = mSize;
            ushort targetsplits = (ushort)( ( bakedresolution - 1 ) / ( TERRAIN_MAX_BATCH_SIZE - 1 ) );
            while ( depth-- != 0 && targetsplits != 0 )
            {
                ushort splits = (ushort)( 1 << depth );
                if ( splits == targetsplits )
                {
                    logMgr.Write( LogMessageLevel.Trivial, false, "Assigning vertex data, resolution={0}, startDepth={1}, endDepth={2}, splits={3}",
                        bakedresolution, depth, prevDepth, splits );
                    // vertex data goes at this level, at bakedresolution
                    // applies to all lower levels (except those with a closer vertex data)
                    // determine physical size (as opposed to resolution)
                    int sz = ( ( bakedresolution - 1 ) / splits ) + 1;
                    this.QuadTree.AssignVertexData( depth, prevDepth, bakedresolution, (ushort)sz );

                    // next set to look for
                    bakedresolution = (ushort)( ( ( currentresolution - 1 ) >> 1 ) + 1 );
                    targetsplits = (ushort)( ( bakedresolution - 1 ) / ( TERRAIN_MAX_BATCH_SIZE - 1 ) );
                    prevDepth = depth;
                }

                currentresolution = (ushort)( ( ( currentresolution - 1 ) >> 1 ) + 1 );
            }

            // Always assign vertex data to the top of the tree
            if ( prevDepth > 0 )
            {
                this.QuadTree.AssignVertexData( 0, 1, bakedresolution, bakedresolution );
                logMgr.Write( LogMessageLevel.Trivial, false, "Assigning vertex data, resolution: {0}, startDepth=0, endDepth=1, splits=1",
                    bakedresolution );
            }

            logMgr.Write( LogMessageLevel.Trivial, false, "Terrain.DistributeVertexdata finished", null );
        }

        [OgreVersion( 1, 7, 2 )]
        protected void UpdateBaseScale()
        {
            //centre the terrain on local origin
            mBase = -mWorldSize * 0.5f;
            // scale determines what 1 unit on the grid becomes in world space
            mScale = mWorldSize / (Real)( mSize - 1 );
        }

        protected void CreateGPUBlendTextures()
        {
            // Create enough RGBA/RGB textures to cope with blend layers
            byte numTex = GetBlendTextureCount( LayerCount );
            //delete extras
            TextureManager tmgr = TextureManager.Instance;
            if ( tmgr == null )
                return;

            while ( mBlendTextureList.Count > numTex )
            {
                tmgr.Remove( mBlendTextureList[ mBlendTextureList.Count - 1 ] );
                mBlendTextureList.Remove( mBlendTextureList[ mBlendTextureList.Count - 1 ] );
            }

            byte currentTex = (byte)mBlendTextureList.Count;
            mBlendTextureList.Capacity = numTex;
            //create new textures
            for ( byte i = currentTex; i < numTex; ++i )
            {
                PixelFormat fmt = GetBlendTextureFormat( i, LayerCount );
                // Use TU_STATIC because although we will update this, we won't do it every frame
                // in normal circumstances, so we don't want TU_DYNAMIC. Also we will 
                // read it (if we've cleared local temp areas) so no WRITE_ONLY
                mBlendTextureList.Add( (Texture)tmgr.CreateManual(
                    msBlendTextureGenerator.GetNextUniqueName(), ResourceGroupManager.DefaultResourceGroupName,
                     TextureType.TwoD, mLayerBlendMapSize, mLayerBlendMapSize, 1, 0, fmt, TextureUsage.Static, null ) );

                mLayerBlendSizeActual = (ushort)mBlendTextureList[ i ].Width;
                if ( mCpuBlendMapStorage.Count > i )
                {
                    //load blend data
                    PixelBox src = new PixelBox( mLayerBlendMapSize, mLayerBlendMapSize, 1, fmt, Memory.PinObject( mCpuBlendMapStorage[ i ] ) );
                    mBlendTextureList[ i ].GetBuffer().BlitFromMemory( src );
                    //realease cpu copy, dont need it anymore
                    Memory.UnpinObject( mCpuBlendMapStorage[ i ] );
                }
                else
                {
                    //initialse black
                    BasicBox box = new BasicBox( 0, 0, mLayerBlendMapSize, mLayerBlendMapSize );
                    HardwarePixelBuffer buf = mBlendTextureList[ i ].GetBuffer();
                    var pInit = buf.Lock( box, BufferLocking.Discard ).Data;
                    byte[] aZero = new byte[ PixelUtil.GetNumElemBytes( fmt ) * mLayerBlendMapSize * mLayerBlendMapSize ];
                    Memory.Copy( BufferBase.Wrap( aZero ), pInit, aZero.Length );
                    buf.Unlock();
                }
            }//i

            mCpuBlendMapStorage.Clear();
        }

        protected void CreateLayerBlendMaps()
        {
            // delete extra blend layers (affects GPU)
            while ( mLayerBlendMapList.Count > mLayers.Count - 1 )
            {
                mLayerBlendMapList.Remove( mLayerBlendMapList[ mLayerBlendMapList.Count - 1 ] );
            }

            // resize up or down (up initialises to 0, populate as necessary)
            object o = null;
            mLayerBlendMapList.Capacity = mLayers.Count;
            for ( int i = 0; i < mLayers.Count - 1; i++ )
                mLayerBlendMapList.Add( (TerrainLayerBlendMap)o );
        }

        [OgreVersion( 1, 7, 2 )]
        protected void CreateOrDestroyGPUNormalMap()
        {
            if ( mNormalMapRequired && this.TerrainNormalMap == null )
            {
                //create 
                this.TerrainNormalMap = TextureManager.Instance.CreateManual(
                    mMaterialName + "/nm", this.DerivedResourceGroup,
                     TextureType.TwoD, mSize, mSize, 1, 0, PixelFormat.BYTE_RGB, TextureUsage.Static, null );

                //upload loaded normal data if present
                if ( mCpuTerrainNormalMap != null )
                {
                    this.TerrainNormalMap.GetBuffer().BlitFromMemory( mCpuTerrainNormalMap );
                    mCpuTerrainNormalMap.Data = null;
                    mCpuTerrainNormalMap = null;
                }
            }
            else if ( !mNormalMapRequired && this.TerrainNormalMap != null )
            {
                //destroy 
                TextureManager.Instance.Remove( this.TerrainNormalMap );
                this.TerrainNormalMap = null;
            }
        }

        [OgreVersion( 1, 7, 2 )]
        protected void ConvertSpace( Space inSpace, Vector3 inVec, Space outSpace, ref Vector3 outVec, bool translation )
        {
            Space currSpace = inSpace;
            outVec = inVec;
            while ( currSpace != outSpace )
            {
                switch ( currSpace )
                {
                    case Space.WorldSpace:
                        {
                            // In all cases, transition to local space
                            outVec -= mPos;
                            currSpace = Space.LocalSpace;
                        }
                        break;
                    case Space.LocalSpace:
                        {
                            switch ( outSpace )
                            {
                                case Space.WorldSpace:
                                    {
                                        if ( translation )
                                            outVec += mPos;
                                        currSpace = Space.WorldSpace;
                                    }
                                    break;
                                case Space.PointSpace:
                                case Space.TerrainSpace:
                                    {
                                        // go via terrain space
                                        outVec = convertWorldToTerrainAxes( outVec );
                                        if ( translation )
                                        {
                                            outVec.x -= mBase; outVec.y -= mBase;
                                            outVec.x /= ( mSize - 1 ) * mScale; outVec.y /= ( mSize - 1 ) * mScale;
                                        }
                                        currSpace = Space.TerrainSpace;
                                    }
                                    break;
                            }//end outSpace
                        }
                        break;
                    case Space.TerrainSpace:
                        {
                            switch ( outSpace )
                            {
                                case Space.WorldSpace:
                                case Space.LocalSpace:
                                    {
                                        // go via local space
                                        if ( translation )
                                        {
                                            outVec.x *= ( mSize - 1 ) * mScale; outVec.y *= ( mSize - 1 ) * mScale;
                                            outVec.x += mBase; outVec.y += mBase;
                                        }
                                        outVec = convertTerrainToWorldAxes( outVec );
                                        currSpace = Space.LocalSpace;
                                    }
                                    break;
                                case Space.PointSpace:
                                    {
                                        outVec.x *= ( mSize - 1 ); outVec.y *= ( mSize - 1 );
                                        // rounding up/down
                                        // this is why POINT_SPACE is the last on the list, because it loses data
                                        outVec.x = (Real)( (int)( outVec.x + 0.5 ) );
                                        outVec.y = (Real)( (int)( outVec.y + 0.5 ) );
                                        currSpace = Space.PointSpace;
                                    }
                                    break;
                            }
                        }
                        break;
                    case Space.PointSpace:
                        {
                            // always go via terrain space
                            if ( translation )
                                outVec.x /= ( mSize - 1 ); outVec.y /= ( mSize - 1 );

                            currSpace = Space.TerrainSpace;
                        }
                        break;
                }//end main switch
            }//end while
        }

        [OgreVersion( 1, 7, 2 )]
        protected void CreateOrDestroyGPUColorMap()
        {
            if ( this.IsGlobalColorMapEnabled && this.GlobalColorMap == null )
            {
#warning Check MIP_DEFAULT
                // create
                this.GlobalColorMap = TextureManager.Instance.CreateManual(
                    mMaterialName + "/cm", this.DerivedResourceGroup,
                     TextureType.TwoD, this.GlobalColorMapSize, this.GlobalColorMapSize, 1, PixelFormat.BYTE_RGB, TextureUsage.Static );

                if ( mCpuColorMapStorage != null )
                {
                    // Load cached data
                    PixelBox src = new PixelBox( (int)this.GlobalColorMapSize, (int)this.GlobalColorMapSize, 1, PixelFormat.BYTE_RGB, Memory.PinObject( mCpuColorMapStorage ) );
                    this.GlobalColorMap.GetBuffer().BlitFromMemory( src );
                    // release CPU copy, don't need it anymore
                    Memory.UnpinObject( mCpuColorMapStorage );
                    mCpuColorMapStorage = null;
                }
            }
            else if ( !this.IsGlobalColorMapEnabled && this.GlobalColorMap != null )
            {
                // destroy
                TextureManager.Instance.Remove( this.GlobalColorMap );
                this.GlobalColorMap = null;
            }
        }

        [OgreVersion( 1, 7, 2 )]
        protected void CreateOrDestroyGPULightmap()
        {
            if ( mLightMapRequired && this.LightMap == null )
            {
                //create
                this.LightMap = TextureManager.Instance.CreateManual(
                    mMaterialName + "/lm", this.DerivedResourceGroup,
                     TextureType.TwoD, this.LightMapSize, this.LightMapSize, 0, PixelFormat.L8, TextureUsage.Static );

                mLightmapSizeActual = (ushort)this.LightMap.Width;

                if ( mCpuLightmapStorage != null )
                {
                    // Load cached data
                    PixelBox src = new PixelBox( (int)this.LightMapSize, (int)this.LightMapSize, 1, PixelFormat.L8, Memory.PinObject( mCpuLightmapStorage ) );
                    this.LightMap.GetBuffer().BlitFromMemory( src );
                    Memory.UnpinObject( mCpuLightmapStorage );
                    mCpuLightmapStorage = null;
                }
                else
                {
                    // initialise to full-bright
                    BasicBox box = new BasicBox( 0, 0, (int)mLightmapSizeActual, (int)mLightmapSizeActual );
                    byte[] aInit = new byte[ mLightmapSizeActual * mLightmapSizeActual ];
                    for ( int i = 0; i < aInit.Length; i++ )
                        aInit[ i ] = 255;
                    HardwarePixelBuffer buf = this.LightMap.GetBuffer();
                    var pInit = buf.Lock( box, BufferLocking.Discard ).Data;
                    Memory.Copy( BufferBase.Wrap( aInit ), pInit, aInit.Length );
                    buf.Unlock();
                }
            }
            else if ( !mLightMapRequired && this.LightMap != null )
            {
                // destroy
                TextureManager.Instance.Remove( this.LightMap );
                this.LightMap = null;
            }
        }

        [OgreVersion( 1, 7, 2 )]
        protected void CreateOrDestroyGPUCompositeMap()
        {
            if ( mCompositeMapRequired && this.CompositeMap == null )
            {
                //create
                this.CompositeMap = TextureManager.Instance.CreateManual(
                    mMaterialName + "/comp", this.DerivedResourceGroup,
                    TextureType.TwoD, mCompositeMapSize, mCompositeMapSize, 0, PixelFormat.BYTE_RGBA, TextureUsage.Static );

                mCompositeMapSizeActual = (ushort)this.CompositeMap.Width;

                if ( mCpuCompositeMapStorage != null )
                {
                    // Load cached data
                    PixelBox src = new PixelBox( (int)mCompositeMapSize, (int)mCompositeMapSize, 1, PixelFormat.BYTE_RGBA, Memory.PinObject( mCpuCompositeMapStorage ) );
                    this.CompositeMap.GetBuffer().BlitFromMemory( src );
                    // release CPU copy, don't need it anymore
                    Memory.UnpinObject( mCpuCompositeMapStorage );
                    mCpuCompositeMapStorage = null;
                }
                else
                {
                    // initialise to black
                    BasicBox box = new BasicBox( 0, 0, (int)mCompositeMapSizeActual, (int)mCompositeMapSizeActual );
                    byte[] aInit = new byte[ mCompositeMapSizeActual * mCompositeMapSizeActual ];

                    HardwarePixelBuffer buf = this.CompositeMap.GetBuffer();
                    var pInit = buf.Lock( box, BufferLocking.Discard ).Data;
                    Memory.Copy( BufferBase.Wrap( aInit ), pInit, aInit.Length );
                    buf.Unlock();
                }
            }
            else if ( !mCompositeMapRequired && this.CompositeMap != null )
            {
                TextureManager.Instance.Remove( this.CompositeMap );
                this.CompositeMap = null;
            }
        }

        protected void WaitForDerivedProcesses()
        {
            while ( this.IsDerivedDataUpdateInProgress )
            {
                //we need to wait for this to finish
                System.Threading.Thread.Sleep( 50 );
#warning implement WorkQueue
#if false
                Root::getSingleton().getWorkQueue()->processResponses();
#endif
            }
        }

        [OgreVersion( 1, 7, 2 )]
        protected Vector3 convertWorldToTerrainAxes( Vector3 inVec )
        {
            Vector3 ret = Vector3.Zero;
            switch ( this.Alignment )
            {
                case Alignment.Align_X_Z:
                    {
                        ret.z = inVec.y;
                        ret.x = inVec.x;
                        ret.y = -inVec.z;
                    }
                    break;
                case Alignment.Align_Y_Z:
                    {
                        ret.z = inVec.x;
                        ret.x = -inVec.z;
                        ret.y = inVec.y;
                    }
                    break;
                case Alignment.Align_X_Y:
                    {
                        ret = inVec;
                    }
                    break;
            }

            return ret;
        }

        [OgreVersion( 1, 7, 2 )]
        protected Vector3 convertTerrainToWorldAxes( Vector3 inVec )
        {
            Vector3 ret = Vector3.Zero;
            switch ( this.Alignment )
            {
                case Alignment.Align_X_Z:
                    ret.x = inVec.x;
                    ret.y = inVec.z;
                    ret.z = -inVec.y;
                    break;
                case Alignment.Align_Y_Z:
                    ret.x = inVec.z;
                    ret.y = inVec.y;
                    ret.z = -inVec.x;
                    break;
                case Alignment.Align_X_Y:
                    ret = inVec;
                    break;
            }
            return ret;
        }

        [OgreVersion( 1, 7, 2 )]
        protected void CheckLayers( bool includeGpuResources )
        {
            foreach ( LayerInstance inst in mLayers )
            {
                LayerInstance layer = inst;
                // If we're missing sampler entries compared to the declaration, initialise them
                for ( int i = layer.TextureNames.Count; i < mLayerDecl.Samplers.Count; ++i )
                {
                    layer.TextureNames.Add( string.Empty );
                }
                // if we have too many layers for the declaration, trim them
                if ( layer.TextureNames.Count > mLayerDecl.Samplers.Count )
                {
                    layer.TextureNames.Capacity = mLayerDecl.Samplers.Count;
                }
            }

            if ( includeGpuResources )
            {
                CreateGPUBlendTextures();
                CreateLayerBlendMaps();
            }
        }

        [OgreVersion( 1, 7, 2 )]
        protected void CheckDeclaration()
        {
            if ( mMaterialGenerator == null )
            {
                mMaterialGenerator = TerrainGlobalOptions.DefaultMaterialGenerator;
            }
            if ( mLayerDecl.Elements == null || mLayerDecl.Elements.Count == 0 )
            {
                //default the declaration
                mLayerDecl = mMaterialGenerator.LayerDeclaration;
            }
        }

        [OgreVersion( 1, 7, 2 )]
        protected PixelFormat GetBlendTextureFormat( byte textureIndex, byte numLayers )
        {
            // Always create RGBA; no point trying to create RGB since all cards pad to 32-bit (XRGB)
            // and it makes it harder to expand layer count dynamically if format has to change
            return PixelFormat.BYTE_RGBA;
        }

        [OgreVersion( 1, 7, 2 )]
        protected void CopyGlobalOptions()
        {
            this.SkirtSize = TerrainGlobalOptions.SkirtSize;
            this.RenderQueueGroupID = TerrainGlobalOptions.RenderQueueGroupID;
            VisibilityFlags = TerrainGlobalOptions.VisibilityFlags;
            QueryFlags = TerrainGlobalOptions.QueryFlags;
            mLayerBlendMapSize = TerrainGlobalOptions.LayerBlendMapSize;
            mLayerBlendSizeActual = mLayerBlendMapSize; // for now, until we check
            this.LightMapSize = TerrainGlobalOptions.LightMapSize;
            mLightmapSizeActual = this.LightMapSize;// for now, until we check
            mCompositeMapSize = TerrainGlobalOptions.CompositeMapSize;
            mCompositeMapSizeActual = mCompositeMapSize; // for now, until we check
        }

        [OgreVersion( 1, 7, 2 )]
        protected void DeriveUVMultipliers()
        {
            mLayerUVMultiplier.Capacity = mLayers.Count;
            mLayerUVMultiplier.Clear();
            for ( int i = 0; i < mLayers.Count; ++i )
            {
                LayerInstance inst = mLayers[ i ];
                mLayerUVMultiplier.Add( mWorldSize / inst.WorldSize );
            }
        }
        /// <summary>
        /// Copys an intptr pointer to an byte array.
        /// </summary>
        /// <param name="srcPtr"></param>
        /// <param name="dstArray"></param>
        protected void IntPtrToArray( BufferBase srcPtr, ref byte[] dstArray )
        {
            System.Diagnostics.Debug.Assert( srcPtr != null );
            System.Diagnostics.Debug.Assert( dstArray.Length > 0 );
            var dst = Memory.PinObject( dstArray );
            Memory.Copy( srcPtr, dst, dstArray.Length );
            Memory.UnpinObject( dstArray );
        }

        protected void UpdateDerivedDataImpl( Rectangle rect, Rectangle lightmapExtraRect, bool synchronous, byte typeMask )
        {
            this.IsDerivedDataUpdateInProgress = true;
            mDerivedUpdatePendingMask = 0;

            DerivedDataRequest req = new DerivedDataRequest();
            req.terrain = this;
            req.DirtyRect = rect;
            req.LightmapExtraDirtyRect = lightmapExtraRect;
            req.TypeMask = typeMask;
            if ( !mNormalMapRequired )
                req.TypeMask = (byte)( req.TypeMask & ~DERIVED_DATA_NORMALS );
            if ( !mLightMapRequired )
                req.TypeMask = (byte)( req.TypeMask & ~DERIVED_DATA_LIGHTMAP );

            _handleRequest( req );

#warning implement workqueue
#if false
            Root::getSingleton().getWorkQueue()->addRequest(
			WORKQUEUE_CHANNEL, WORKQUEUE_DERIVED_DATA_REQUEST, 
			Any(req), 0, synchronous);
#endif
        }

        protected void CalculateCurrentLod( Viewport vp )
        {
            if ( this.QuadTree != null )
            {
                // calculate error terms
#warning implement Camera.LodCamera;
                Camera cam = vp.Camera;

                // W. de Boer 2000 calculation
                // A = vp_near / abs(vp_top)
                // A = 1 / tan(fovy*0.5)    (== 1 for fovy=45*2)
                float A = (float)( 1.0f / Utility.Tan( (Radian)( ( cam.FieldOfView * 0.5 ) ) ) );
                // T = 2 * maxPixelError / vertRes
                float maxPixelError = 8 * cam.InverseLodBias;//TerrainGlobalOptions.MaxPixelError * cam.InverseLodBias;
                float T = 2.0f * maxPixelError / (float)vp.ActualHeight;

                // CFactor = A / T
                float cFactor = A / T;
                this.QuadTree.CalculateCurrentLod( cam, cFactor );
            }
        }

        public void AddQueryFlag( uint flags )
        {
            this.QueryFlags |= flags;
        }

        public void RemoveQueryFlags( uint flags )
        {
            this.QueryFlags &= ~flags;
        }

        public Terrain GetNeighbour( NeighbourIndex index )
        {
            return mNeighbours[ (int)index ];
        }

        public void SetNeighbour( NeighbourIndex index, Terrain neighbour )
        {
            SetNeighbour( index, neighbour, false, true );
        }

        public void SetNeighbour( NeighbourIndex index, Terrain neighbour, bool recalculate, bool notifyOther )
        {
            if ( mNeighbours[ (int)index ] != neighbour )
            {
                // detach existing
                if ( mNeighbours[ (int)index ] != null && notifyOther )
                    mNeighbours[ (int)index ].SetNeighbour( GetOppositeNeighbour( index ), null, false, false );

                mNeighbours[ (int)index ] = neighbour;
                if ( mNeighbours[ (int)index ] != null && notifyOther )
                    mNeighbours[ (int)index ].SetNeighbour( GetOppositeNeighbour( index ), this, recalculate, false );

                if ( recalculate )
                {
                    //recalculate, pass OUR edge rect
                    Rectangle edgerect = new Rectangle();
                    GetEdgeRect( index, 2, ref edgerect );
                    NeighbourModified( index, edgerect, edgerect );
                }
            }
        }

        public NeighbourIndex GetOppositeNeighbour( NeighbourIndex index )
        {
            int intindex = (int)index;
            intindex += (int)( NeighbourIndex.Count ) / 2;
            intindex = intindex % (int)NeighbourIndex.Count;
            return (NeighbourIndex)intindex;
        }

        public void NotifyNeighbours()
        {
            // There are 3 things that can need updating:
            // Height at edge - match to neighbour (first one to update 'loses' to other since read-only)
            // Normal at edge - use heights from across boundary too
            // Shadows across edge
            // The extent to which these can affect the current tile vary:
            // Height at edge - only affected by a change at the adjoining edge / corner
            // Normal at edge - only affected by a change to the 2 rows adjoining the edge / corner
            // Shadows across edge - possible effect extends based on the projection of the
            // neighbour AABB along the light direction (worst case scenario)
            if ( !mDirtyGeometryRectForNeighbours.IsNull )
            {
                Rectangle dirtyRect = new Rectangle( mDirtyGeometryRectForNeighbours );
                mDirtyGeometryRectForNeighbours = new Rectangle();
                // calculate light update rectangle
                Vector3 lightVec = TerrainGlobalOptions.LightMapDirection;
                Rectangle lightmapRect = new Rectangle();
                WidenRectByVector( lightVec, dirtyRect, MinHeight, MaxHeight, ref lightmapRect );

                for ( int i = 0; i < (int)NeighbourIndex.Count; ++i )
                {
                    NeighbourIndex ni = (NeighbourIndex)( i );
                    Terrain neighbour = GetNeighbour( ni );
                    if ( neighbour == null )
                        continue;

                    // Intersect the incoming rectangles with the edge regions related to this neighbour
                    Rectangle edgeRect = new Rectangle();
                    GetEdgeRect( ni, 2, ref edgeRect );
                    Rectangle heightEdgeRect = edgeRect.Intersect( dirtyRect );
                    Rectangle lightmapEdgeRect = edgeRect.Intersect( lightmapRect );

                    if ( !heightEdgeRect.IsNull || !lightmapRect.IsNull )
                    {
                        // ok, we have something valid to pass on
                        Rectangle neighbourHeightEdgeRect = new Rectangle(), neighbourLightmapEdgeRect = new Rectangle();
                        if ( !heightEdgeRect.IsNull )
                            GetNeighbourEdgeRect( ni, heightEdgeRect, ref neighbourHeightEdgeRect );

                        if ( !lightmapRect.IsNull )
                            GetNeighbourEdgeRect( ni, lightmapEdgeRect, ref neighbourLightmapEdgeRect );

                        neighbour.NeighbourModified( GetOppositeNeighbour( ni ),
                            neighbourHeightEdgeRect, neighbourLightmapEdgeRect );

                    }
                }
            }
        }

        public void NeighbourModified( NeighbourIndex index, Rectangle edgerect, Rectangle shadowrect )
        {
            // We can safely assume that we would not have been contacted if it wasn't 
            // important
            Terrain neighbour = GetNeighbour( index );
            if ( neighbour == null )
                return; // bogus request

            bool updateGeom = false;
            byte updateDerived = 0;


            if ( !edgerect.IsNull )
            {
                // update edges; match heights first, then recalculate normals
                // reduce to just single line / corner
                Rectangle heightMatchRect = new Rectangle();
                GetEdgeRect( index, 1, ref heightMatchRect );
                heightMatchRect = heightMatchRect.Intersect( edgerect );

                for ( long y = heightMatchRect.Top; y < heightMatchRect.Bottom; ++y )
                {
                    for ( long x = heightMatchRect.Left; x < heightMatchRect.Right; ++x )
                    {
                        long nx = 0, ny = 0;
                        GetNeighbourPoint( index, x, y, ref nx, ref ny );
                        float neighbourHeight = neighbour.GetHeightAtPoint( nx, ny );
                        if ( !Utility.RealEqual( neighbourHeight, GetHeightAtPoint( x, y ), 1e-3f ) )
                        {
                            SetHeightAtPoint( x, y, neighbourHeight );
                            if ( !updateGeom )
                            {
                                updateGeom = true;
                                updateDerived |= DERIVED_DATA_ALL;
                            }

                        }
                    }
                }
                // if we didn't need to update heights, we still need to update normals
                // because this was called only if neighbor changed
                if ( !updateGeom )
                {
                    // ideally we would deal with normal dirty rect separately (as we do with
                    // lightmaps) because a dirty geom rectangle will actually grow by one 
                    // element in each direction for normals recalculation. However for
                    // the sake of one row/column it's really not worth it.
                    mDirtyDerivedDataRect.Merge( edgerect );
                    updateDerived |= DERIVED_DATA_NORMALS;
                }
            }

            if ( !shadowrect.IsNull )
            {
                // update shadows
                // here we need to widen the rect passed in based on the min/max height 
                // of the *neighbour*
                Vector3 lightVec = TerrainGlobalOptions.LightMapDirection;
                Rectangle widenedRect = new Rectangle();
                WidenRectByVector( lightVec, shadowrect, neighbour.MinHeight, neighbour.MaxHeight, ref widenedRect );

                // set the special-case lightmap dirty rectangle
                mDirtyLightmapFromNeighboursRect.Merge( widenedRect );
                updateDerived |= DERIVED_DATA_LIGHTMAP;
            }

            if ( updateGeom )
                UpdateGeometry();
            if ( updateDerived != 0 )
                UpdateDerivedData( true, updateDerived );
        }

        public Terrain RaySelectNeighbour( Ray ray )
        {
            return RaySelectNeighbour( ray, 0 );
        }

        public Terrain RaySelectNeighbour( Ray ray, float distanceLimit )
        {
            Real dNear = 0, dFar = 0;
            Ray localRay = new Ray( ray.Origin - Position, ray.Direction );
            // Move back half a square - if we're on the edge of the AABB we might
            // miss the intersection otherwise; it's ok for everywhere else since
            // we want the far intersection anyway
            localRay.Origin = localRay.GetPoint( -mWorldSize / mSize * 0.5f );
            IntersectResult res = new IntersectResult();
            //   Tuple<bool, Real, Real> intersects = Utility.Intersects(localRay, AABB);
            IntersectResult intersects = Utility.Intersects( localRay, AABB );
            if ( intersects.Hit )
            {
                dNear = intersects.Distance;
                dFar = intersects.Distance;
                // discard out of range
                if ( dFar <= 0 || ( distanceLimit != 0 && dFar > distanceLimit ) )
                    return null;

                // we're interested in the exit point
                // convert to standard form so we can use x/y always
                Ray terrainRay = new Ray( convertWorldToTerrainAxes( localRay.Origin ),
                    convertWorldToTerrainAxes( localRay.Direction ) );

                Vector3 terrainIntersectPos = terrainRay.GetPoint( dFar );
                Real x = terrainIntersectPos.x;
                Real y = terrainIntersectPos.y;
                Real dx = terrainRay.Direction.x;
                Real dy = terrainRay.Direction.y;

                if ( Utility.RealEqual( Utility.Abs( x ), Utility.Abs( y ) ) )
                {
                    if ( x > 0 && y > 0 && dx > 0 && dy > 0 )
                        return GetNeighbour( NeighbourIndex.NorthEast );
                    if ( x > 0 && y < 0 && dx > 0 && dy < 0 )
                        return GetNeighbour( NeighbourIndex.SouthEast );
                    if ( x < 0 && y > 0 && dx < 0 && dy > 0 )
                        return GetNeighbour( NeighbourIndex.NorthWest );
                    if ( x < 0 && y < 0 && dx < 0 && dy < 0 )
                        return GetNeighbour( NeighbourIndex.SouthWest );
                }
                if ( x > 0 && x > y && dx > 0 )
                    return GetNeighbour( NeighbourIndex.East );
                if ( x < 0 && x < y && dx < 0 )
                    return GetNeighbour( NeighbourIndex.West );
                if ( y > 0 && y > x && dy > 0 )
                    return GetNeighbour( NeighbourIndex.North );
                if ( y < 0 && y < x && dy < 0 )
                    return GetNeighbour( NeighbourIndex.South );

            }
            return null;
        }

        public void DumpTextures( string prefix, string suffix )
        {
            throw new NotImplementedException();
        }

        public void GetEdgeRect( NeighbourIndex index, long range, ref Rectangle outRect )
        {
            // We make the edge rectangle 2 rows / columns at the edge of the tile
            // 2 because this copes with normal changes and potentially filtered
            // shadows.
            // all right / bottom values are exclusive
            // terrain origin is bottom-left remember so north is highest value

            // set left/right
            switch ( index )
            {
                case NeighbourIndex.East:
                case NeighbourIndex.NorthEast:
                case NeighbourIndex.SouthEast:
                    outRect.Left = mSize - range;
                    outRect.Right = mSize;
                    break;
                case NeighbourIndex.West:
                case NeighbourIndex.NorthWest:
                case NeighbourIndex.SouthWest:
                    outRect.Left = 0;
                    outRect.Right = range;
                    break;
                case NeighbourIndex.North:
                case NeighbourIndex.South:
                    outRect.Left = 0;
                    outRect.Right = mSize;
                    break;
            }

            // set top / bottom
            switch ( index )
            {
                case NeighbourIndex.North:
                case NeighbourIndex.NorthEast:
                case NeighbourIndex.NorthWest:
                    outRect.Top = mSize - range;
                    outRect.Bottom = mSize;
                    break;
                case NeighbourIndex.South:
                case NeighbourIndex.SouthEast:
                case NeighbourIndex.SouthWest:
                    outRect.Top = 0;
                    outRect.Bottom = range;
                    break;
                case NeighbourIndex.East:
                case NeighbourIndex.West:
                    outRect.Top = 0;
                    outRect.Bottom = mSize;
                    break;
            }
        }

        public void GetNeighbourEdgeRect( NeighbourIndex index, Rectangle inRect, ref Rectangle outRect )
        {
            System.Diagnostics.Debug.Assert( mSize == GetNeighbour( index ).Size, "Neighbour has not the same size as this instance" );
            // Basically just reflect the rect 
            // remember index is neighbour relationship from OUR perspective so
            // arrangement is backwards to getEdgeRect

            // left/right
            switch ( index )
            {
                case NeighbourIndex.East:
                case NeighbourIndex.NorthEast:
                case NeighbourIndex.SouthEast:
                case NeighbourIndex.West:
                case NeighbourIndex.NorthWest:
                case NeighbourIndex.SouthWest:
                    outRect.Left = mSize - inRect.Right;
                    outRect.Right = mSize - inRect.Left;
                    break;
                default:
                    outRect.Left = inRect.Left;
                    outRect.Right = inRect.Right;
                    break;
            };

            // top / bottom
            switch ( index )
            {
                case NeighbourIndex.North:
                case NeighbourIndex.NorthEast:
                case NeighbourIndex.NorthWest:
                case NeighbourIndex.South:
                case NeighbourIndex.SouthWest:
                case NeighbourIndex.SouthEast:
                    outRect.Top = mSize - inRect.Bottom;
                    outRect.Bottom = mSize - inRect.Top;
                    break;
                default:
                    outRect.Top = inRect.Top;
                    outRect.Bottom = inRect.Bottom;
                    break;
            }
        }

        public void GetNeighbourPoint( NeighbourIndex index, long x, long y, ref long outx, ref long outy )
        {
            // Get the index of the point we should be looking at on a neighbour
            // in order to match up points
            System.Diagnostics.Debug.Assert( mSize == GetNeighbour( index ).Size, "Neighbour has not the same size as this instance" );

            // left/right
            switch ( index )
            {
                case NeighbourIndex.East:
                case NeighbourIndex.NorthEast:
                case NeighbourIndex.SouthEast:
                case NeighbourIndex.West:
                case NeighbourIndex.NorthWest:
                case NeighbourIndex.SouthWest:
                    outx = mSize - x - 1;
                    break;
                default:
                    outx = x;
                    break;
            }

            // top / bottom
            switch ( index )
            {
                case NeighbourIndex.North:
                case NeighbourIndex.NorthEast:
                case NeighbourIndex.NorthWest:
                case NeighbourIndex.South:
                case NeighbourIndex.SouthWest:
                case NeighbourIndex.SouthEast:
                    outy = mSize - y - 1;
                    break;
                default:
                    outy = y;
                    break;
            }
        }

        public void GetNeighbourPointOverflow( long x, long y, out NeighbourIndex outindex, out long outx, out long outy )
        {
            outindex = NeighbourIndex.Count;
            if ( x < 0 )
            {
                outx = x + mSize - 1;
                if ( y < 0 )
                    outindex = NeighbourIndex.SouthWest;
                else if ( y >= mSize )
                    outindex = NeighbourIndex.NorthWest;
                else
                    outindex = NeighbourIndex.West;
            }
            else if ( x >= mSize )
            {
                outx = x - mSize + 1;
                if ( y < 0 )
                    outindex = NeighbourIndex.SouthEast;
                else if ( y >= mSize )
                    outindex = NeighbourIndex.NorthEast;
                else
                    outindex = NeighbourIndex.East;
            }
            else
                outx = x;

            if ( y < 0 )
            {
                outy = y + mSize - 1;
                if ( x >= 0 && x < mSize )
                    outindex = NeighbourIndex.South;
            }
            else if ( y >= mSize )
            {
                outy = y - mSize + 1;
                if ( x >= 0 && x < mSize )
                    outindex = NeighbourIndex.North;
            }
            else
                outy = y;

            System.Diagnostics.Debug.Assert( outindex != NeighbourIndex.Count );
        }
        /// <summary>
        /// checks if the given value is power of two.
        /// </summary>
        /// <param name="x">the value to check</param>
        /// <returns>true if the given value is power of two</returns>
        protected bool IsPowerOfTwo( ulong x )
        {
            return ( x & ( x - 1 ) ) == 0;
        }
        #endregion

        #region - private functions -

        private void _handleRequest( DerivedDataRequest request )
        {
            DerivedDataRequest ddr = request;
            if ( ddr.terrain != this )
                return;

            DerivedDataResponse ddres = new DerivedDataResponse();
            ddr.TypeMask = (byte)( ddr.TypeMask & DERIVED_DATA_ALL );
            if ( ( ddr.TypeMask & DERIVED_DATA_DELTAS ) == ddr.TypeMask )
            {
                ddres.DeltaUpdateRect = CalculateHeightDeltas( ddr.DirtyRect );
                ddres.RemainingTypeMask &= (byte)~DERIVED_DATA_DELTAS;
            }
            else if ( ( ddr.TypeMask & (byte)DERIVED_DATA_NORMALS ) == ddr.TypeMask )
            {
                ddres.NormalMapBox = CalculateNormals( ddr.DirtyRect, ref ddres.NormalUpdateRect );
                ddres.RemainingTypeMask &= (byte)~DERIVED_DATA_NORMALS;
            }
            else if ( ( ddr.TypeMask & (byte)DERIVED_DATA_LIGHTMAP ) == ddr.TypeMask )
            {
                ddres.LightMapPixelBox = CalculateLightMap( ddr.DirtyRect, ddr.LightmapExtraDirtyRect, ref ddres.LightMapUpdateRect );
                ddres.RemainingTypeMask &= (byte)~DERIVED_DATA_LIGHTMAP;
            }

            ddres.Terrain = ddr.terrain;
            _handleResponse( ddres, ddr );
        }

        private void _handleResponse( DerivedDataResponse ddrs, DerivedDataRequest ddreq )
        {
            if ( ddreq.terrain != this )
                return;

            if ( ( ( ddreq.TypeMask & DERIVED_DATA_DELTAS ) == ddreq.TypeMask ) &&
                ( ( ddrs.RemainingTypeMask & DERIVED_DATA_DELTAS ) != DERIVED_DATA_DELTAS ) )
            {
                FinalizeHeightDeltas( ddrs.DeltaUpdateRect, false );
            }
            if ( ( ( ddreq.TypeMask & DERIVED_DATA_NORMALS ) == ddreq.TypeMask ) &&
                ( ( ddrs.RemainingTypeMask & DERIVED_DATA_NORMALS ) != DERIVED_DATA_NORMALS ) )
            {
                FinalizeNormals( ddrs.NormalUpdateRect, ddrs.NormalMapBox );
                mCompositeMapDirtyRect.Merge( ddreq.DirtyRect );
            }
            if ( ( ( ddreq.TypeMask & DERIVED_DATA_LIGHTMAP ) == ddreq.TypeMask ) &&
                ( ( ddrs.RemainingTypeMask & DERIVED_DATA_LIGHTMAP ) != DERIVED_DATA_LIGHTMAP ) )
            {
                FinalizeLightMap( ddrs.LightMapUpdateRect, ddrs.LightMapPixelBox );
                mCompositeMapDirtyRect.Merge( ddreq.DirtyRect );
                mCompositeMapDirtyRectLightmapUpdate = true;
            }

            this.IsDerivedDataUpdateInProgress = false;

            Rectangle newRect = new Rectangle( 0, 0, 0, 0 );
            if ( ddrs.RemainingTypeMask != 0 )
                newRect.Merge( ddreq.DirtyRect );
            if ( mDerivedUpdatePendingMask != 0 )
            {
                newRect.Merge( mDirtyDerivedDataRect );
                mDirtyDerivedDataRect = new Rectangle( 0, 0, 0, 0 );
            }
            Rectangle newLightmapExtraRext = new Rectangle( 0, 0, 0, 0 );
            if ( ddrs.RemainingTypeMask != 0 )
                newLightmapExtraRext.Merge( ddreq.LightmapExtraDirtyRect );
            if ( mDerivedUpdatePendingMask != 0 )
            {
                newLightmapExtraRext.Merge( mDirtyLightmapFromNeighboursRect );
                mDirtyLightmapFromNeighboursRect = new Rectangle( 0, 0, 0, 0 );
            }
            byte newMask = (byte)( ddrs.RemainingTypeMask | mDerivedUpdatePendingMask );
            if ( newMask != 0 )
            {
                UpdateDerivedDataImpl( newRect, newLightmapExtraRext, true, newMask );
            }
            else
            {
                if ( mCompositeMapRequired )
                    UpdateCompositeMap();
            }
        }

        [OgreVersion( 1, 7, 2 )]
        private void _preFindVisibleObjects( SceneManager source, IlluminationRenderStage irs, Viewport v )
        {
            //Early-out
            if ( !this.IsLoaded )
                return;

            // check deferred updates
            long currMillis = Root.Instance.Timer.Milliseconds;
            long elapsedMillis = currMillis - mLastMillis;
            if ( mCompositeMapUpdateCountdown > 0 && elapsedMillis > 0 )
            {
                if ( elapsedMillis > mCompositeMapUpdateCountdown )
                    mCompositeMapUpdateCountdown = 0;
                else
                    mCompositeMapUpdateCountdown -= elapsedMillis;

                if ( mCompositeMapUpdateCountdown == 0 )
                    UpdateCompositeMap();
            }
            mLastMillis = currMillis;
            // only calculate LOD once per LOD camera, per frame, per viewport height
#warning implement LodCamera
            Camera lodCamera = v.Camera.LodCamera;
            ulong frameNum = (ulong)Root.Instance.NextFrameNumber;
            int vpHeight = v.ActualHeight;
            if ( mLastLODCamera != lodCamera || frameNum != mLastLODFrame ||
                mLastViewportHeight != vpHeight )
            {
                mLastLODCamera = lodCamera;
                mLastLODFrame = frameNum;
                mLastViewportHeight = vpHeight;
                CalculateCurrentLod( v );
            }
        }

        #endregion - private functions -
    }
}
