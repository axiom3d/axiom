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
	[OgreVersion( 1, 7, 2 )]
	public struct DerivedDataRequest
	{
		public Terrain Terrain;

		/// <summary>
		/// types requested
		/// </summary>
		public byte TypeMask;

		public Rectangle DirtyRect;
		public Rectangle LightmapExtraDirtyRect;
	}

	/// <summary>
	///  A data holder for communicating with the background derived data update
	/// </summary>
	[OgreVersion( 1, 7, 2 )]
	public struct DerivedDataResponse
	{
		public Terrain Terrain;

		/// <summary>
		/// remaining types not yet processed
		/// </summary>
		public byte RemainingTypeMask;

		/// <summary>
		/// The area of deltas that was updated
		/// </summary>
		public Rectangle DeltaUpdateRect;

		/// <summary>
		/// the area of normals that was updated
		/// </summary>
		public Rectangle NormalUpdateRect;

		/// <summary>
		/// the area of lightmap that was updated
		/// </summary>
		public Rectangle LightMapUpdateRect;

		//all CPU-side data, independent of textures; to be blitted in main thread
		public PixelBox NormalMapBox;
		public PixelBox LightMapPixelBox;
	};

	/// <summary>
	/// An instance of a layer, with specific texture names
	/// </summary>
	[OgreVersion( 1, 7, 2 )]
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
	};

	/// <summary>
	///  The alignment of the terrain
	/// </summary>
	[OgreVersion( 1, 7, 2 )]
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
	};

	/// <summary>
	/// Enumeration of relative spaces that you might want to use to address the terrain
	/// </summary>
	[OgreVersion( 1, 7, 2 )]
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
	};

	/// <summary>
	/// Neighbour index enumeration - indexed anticlockwise from East like angles
	/// </summary>
	[OgreVersion( 1, 7, 2 )]
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
	};

	/// <summary>
	/// Structure encapsulating import data that you may use to bootstrap 
	/// the terrain without loading from a native data stream. 
	/// </summary>
	[OgreVersion( 1, 7, 2 )]
	public class ImportData : DisposableObject
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
		///  If neither inputImage or inputFloat are supplied, the constant
		///   height at which the initial terrain should be created (flat).
		/// </summary>
		public float ConstantHeight;

		/// <summary>
		/// Definition of the contents of each layer (required).
		/// Most likely,  you will pull a declaration from a TerrainMaterialGenerator
		/// of your choice.
		/// </summary>
		public bool DeleteInputData;

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

		[OgreVersion( 1, 7, 2 )]
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
			ConstantHeight = 0;
			LayerList = new List<LayerInstance>();
			DeleteInputData = false;
			InputScale = 1.0f;
			InputBias = 0.0f;
		}

		/// <summary>
		/// Copy constructor
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public ImportData( ImportData d )
		{
			TerrainAlign = d.TerrainAlign;
			TerrainSize = d.TerrainSize;
			MaxBatchSize = d.MaxBatchSize;
			MinBatchSize = d.MinBatchSize;
			Pos = d.Pos;
			WorldSize = d.WorldSize;
			InputImage = d.InputImage;
			InputFloat = d.InputFloat;
			ConstantHeight = d.ConstantHeight;
			LayerList = new List<LayerInstance>( d.LayerList );
			DeleteInputData = d.DeleteInputData;
			InputScale = d.InputScale;
			InputBias = d.InputBias;
		}

		/// <summary>
		/// Delete any input data if this struct is set to do so
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public void Destroy()
		{
			if ( DeleteInputData )
			{
				InputImage.SafeDispose();
				InputImage = null;
			}
		}

		[OgreVersion( 1, 7, 2, "~ImportData" )]
		protected override void dispose( bool disposeManagedResources )
		{
			if ( !IsDisposed )
			{
				if ( disposeManagedResources )
				{
					Destroy();
				}
			}

			base.dispose( disposeManagedResources );
		}
	};

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
	public class Terrain : DisposableObject, WorkQueue.IRequestHandler, WorkQueue.IResponseHandler
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
		public static ushort WORKQUEUE_DERIVED_DATA_REQUEST = 1;
		public static int LOD_MORPH_CUSTOM_PARAM = 1001;
		public static byte DERIVED_DATA_DELTAS = 1;
		public static byte DERIVED_DATA_NORMALS = 2;
		public static byte DERIVED_DATA_LIGHTMAP = 4;
		// This MUST match the bitwise OR of all the types above with no extra bits!
		public static byte DERIVED_DATA_ALL = 7;

		#endregion - constants -

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
		protected RealList mLayerUVMultiplier = new RealList();
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
		protected SceneNode mRootNode;

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
		protected Terrain[] mNeighbours = new Terrain[(int)NeighbourIndex.Count];
		protected Rectangle mDirtyGeometryRectForNeighbours = new Rectangle( 0, 0, 0, 0 );
		protected Rectangle mDirtyLightmapFromNeighboursRect = new Rectangle( 0, 0, 0, 0 );
		private BufferBase mHeightDataPtr;
		private BufferBase mDeltaDataPtr;
		protected GpuBufferAllocator mCustomGpuBufferAllocator;
		protected DefaultGpuBufferAllocator mDefaultGpuBufferAllocator;
		protected ushort workQueueChannel;

		#endregion - fields -

		#region - properties -

		/// <summary>
		/// Get'S the AABB (local coords) of the entire terrain
		/// </summary>
		public AxisAlignedBox AABB
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				if ( QuadTree == null )
				{
					return AxisAlignedBox.Null;
				}
				else
				{
					return QuadTree.AABB;
				}
			}
		}

		/// <summary>
		/// Get the AABB (world coords) of the entire terrain
		/// </summary>
		public AxisAlignedBox WorldAABB
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				Matrix4 m = Matrix4.Identity;
				m.Translation = Position;

				AxisAlignedBox ret = AABB;
				ret.TransformAffine( m );
				return ret;
			}
		}

		/// <summary>
		/// Get's the minimum height of the terrain
		/// </summary>
		public Real MinHeight
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				if ( QuadTree == null )
				{
					return 0;
				}
				else
				{
					return QuadTree.MinHeight;
				}
			}
		}

		/// <summary>
		/// Get's the maximum height of the terrain.
		/// </summary>
		public Real MaxHeight
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				if ( QuadTree == null )
				{
					return 0;
				}
				else
				{
					return QuadTree.MaxHeight;
				}
			}
		}

		/// <summary>
		/// Get's the bounding radius of the entire terrain
		/// </summary>
		public Real BoundingRadius
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				if ( QuadTree == null )
				{
					return 0;
				}
				else
				{
					return QuadTree.BoundingRadius;
				}
			}
		}

		/// <summary>
		/// Get the final resource group to use when loading / saving.
		/// </summary>
		internal string DerivedResourceGroup
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				if ( string.IsNullOrEmpty( ResourceGroup ) )
				{
					return TerrainGlobalOptions.DefaultResourceGroup;
				}
				else
				{
					return ResourceGroup;
				}
			}
		}

		/// <summary>
		/// Get a pointer to all the height data for this terrain.
		/// </summary>
		/// <remarks>
		/// The height data is in world coordinates, relative to the position 
		///	of the terrain.
		/// </remarks>
		public float[] HeightData
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return mHeightData;
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
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return mDeltaDataPtr;
			}
		}

		/// <summary>
		/// Get the alignment of the terrain
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public Alignment Alignment { get; protected set; }

		/// <summary>
		/// Get the size of the terrain in vertices along one side
		/// </summary>
		public ushort Size
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return mSize;
			}
		}

		/// <summary>
		/// Get the maximum size in vertices along one side of a batch
		/// </summary>
		public ushort MaxBatchSize
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return mMaxBatchSize;
			}
		}

		/// <summary>
		/// Get the minimum size in vertices along one side of a batch
		/// </summary>
		public ushort MinBatchSize
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return mMinBatchSize;
			}
		}

		/// <summary>
		/// Get the size of the terrain in world units
		/// </summary>
		public Real WorldSize
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return mWorldSize;
			}
		}

		/// <summary>
		/// Get's or set's the world position of the terrain centre
		/// </summary>
		public Vector3 Position
		{
			get
			{
				return mPos;
			}

			[OgreVersion( 1, 7, 2 )]
			set
			{
				if ( mPos != value )
				{
					mPos = value;
					RootSceneNode.Position = mPos;
					UpdateBaseScale();
					IsModified = true;
				}
			}
		}

		/// <summary>
		/// Get the root scene node for the terrain (internal use only)
		/// </summary>
		internal SceneNode RootSceneNode
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return mRootNode;
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
				if ( mMaterial == null || mMaterialGenerator.ChangeCount != mMaterialGenerationCount || mMaterialDirty )
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
					{
						mMaterialGenerator.UpdateParamsForCompositeMap( mCompositeMapMaterial, this );
					}
					mMaterialParamsDirty = false;
				}

				return mMaterial;
			}
		}

		/// <summary>
		/// Get's the material being used for the terrain composite map
		/// </summary>
		public Material CompositeMapMaterial
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				// both materials updated together since they change at the same time
				var matNam = Material.Name;
				return mCompositeMapMaterial;
			}
		}

		/// <summary>
		/// Get the maximum number of layers supported with the current options. 
		/// </summary>
		/// <note>When you change the options requested, this value can change. </note>
		public byte MaxLayers
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return mMaterialGenerator.GetMaxLayers( this );
			}
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
		internal bool NormalMapRequired
		{
			set
			{
				if ( mNormalMapRequired != value )
				{
					mNormalMapRequired = value;

					// Check NPOT textures supported. We have to use NPOT textures to map
					// texels to vertices directly!
					if ( !mNormalMapRequired &&
					     Root.Instance.RenderSystem.Capabilities.HasCapability( Capabilities.NonPowerOf2Textures ) )
					{
						mNormalMapRequired = false;
						LogManager.Instance.Write( LogMessageLevel.Critical, false,
						                           @"Terrain: Ignoring request for normal map generation since
							non-power-of-two texture support is required." );
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
		internal bool CompositeMapRequired
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
				{
					return mCustomGpuBufferAllocator;
				}
				else
				{
					return mDefaultGpuBufferAllocator;
				}
			}

			[OgreVersion( 1, 7, 2 )]
			set
			{
				if ( GpuBufferAllocator != value )
				{
					if ( IsLoaded )
					{
						throw new AxiomException( "Cannot alter the allocator when loaded!" );
					}

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
					_posDecl.AddElement( TerrainQuadTreeNode.POSITION_BUFFER, 0, VertexElementType.Float3,
					                     VertexElementSemantic.Position );
					_posDecl.AddElement( TerrainQuadTreeNode.POSITION_BUFFER, VertexElement.GetTypeSize( VertexElementType.Float3 ),
					                     VertexElementType.Float2, VertexElementSemantic.TexCoords, 0 );
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
					_deltaDecl.AddElement( TerrainQuadTreeNode.DELTA_BUFFER, 0, VertexElementType.Float2,
					                       VertexElementSemantic.TexCoords, 1 );
				}

				return _deltaDecl;
			}
		}

		/// <summary>
		/// Query whether a derived data update is in progress or not.
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public bool IsDerivedDataUpdateInProgress { get; protected set; }

		/// <summary>
		/// Set the default resource group to use to load / save terrains.
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public string ResourceGroup { get; set; }

		/// <summary>
		/// Returns whether this terrain has been modified since it was first loaded / defined.
		/// </summary>
		/// <remarks>This flag is reset on save().</remarks>
		[OgreVersion( 1, 7, 2 )]
		public bool IsModified { get; protected set; }

		/// <summary>
		/// Returns whether terrain heights have been modified since the terrain was first loaded / defined. 
		/// </summary>
		/// <remarks>This flag is reset on save().</remarks>
		[OgreVersion( 1, 7, 2 )]
		public bool IsHeightDataModified { get; protected set; }

		/// <summary>
		/// Return whether the terrain is loaded. 
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public bool IsLoaded { get; protected set; }

		/// <summary>
		/// Get's or set's the visbility flags that terrains will be rendered with
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public uint VisibilityFlags { get; set; }

		/// <summary>
		/// Get or set the query flags for this terrain.
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public uint QueryFlags { get; set; }

		/// <summary>
		/// Get's the scenemanager of the terrain
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public SceneManager SceneManager { get; protected set; }

		/// <summary>
		/// Get the default size of the blend maps for a new terrain.
		/// </summary>
		public ushort LayerBlendMapSize
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return mLayerBlendMapSize;
			}
		}

		/// <summary>
		/// The default size of 'skirts' used to hide terrain cracks
		///	(default 10, set for new Terrain using TerrainGlobalOptions)
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public Real SkirtSize { get; protected set; }

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
			get
			{
				return mCompositeMapMaterial;
			}
		}

		/// <summary>
		/// Get's the name of the material being used for the terrain
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public string MaterialName
		{
			get
			{
				return mMaterialName;
			}
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
		[OgreVersion( 1, 7, 2 )]
		internal bool IsMorphRequired { get; set; }

		/// <summary>
		/// Get's whether a global color map is enabled on this terrain
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public bool IsGlobalColorMapEnabled { get; protected set; }

		/// <summary>
		/// Get access to the lightmap, if enabled (as requested by the material generator)
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public Texture LightMap { get; protected set; }

		/// <summary>
		/// Get the requested size of lightmap for this terrain. 
		/// Note that where hardware limits this, the actual lightmap may be lower
		/// resolution. This option is derived from TerrainGlobalOptions when the
		/// terrain is created.
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public ushort LightMapSize { get; protected set; }

		/// <summary>
		/// Get's access to the global colour map, if enabled
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public Texture GlobalColorMap { get; protected set; }

		/// <summary>
		///  Get's the size of the global colour map (if used)
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public ushort GlobalColorMapSize { get; protected set; }

		/// <summary>
		/// Get's the declaration which describes the layers in this terrain.
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public TerrainLayerDeclaration LayerDeclaration
		{
			get
			{
				return mLayerDecl;
			}
		}

		/// <summary>
		/// Get's the (global) normal map texture
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public Texture TerrainNormalMap { get; protected set; }

		/// <summary>
		/// Get access to the composite map, if enabled (as requested by the material generator)
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public Texture CompositeMap { get; protected set; }

		/// <summary>
		/// Get the requested size of composite map for this terrain. 
		/// Note that where hardware limits this, the actual texture may be lower
		/// resolution. This option is derived from TerrainGlobalOptions when the
		/// terrain is created.
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public ushort CompositeMapSize
		{
			get
			{
				return mCompositeMapSize;
			}
		}

		/// <summary>
		/// Get's the top level of the quad tree which is used to divide up the terrain
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public TerrainQuadTreeNode QuadTree { get; protected set; }

		/// <summary>
		/// Get's the number of layers in this terrain.
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public byte LayerCount
		{
			get
			{
				return (byte)mLayers.Count;
			}
		}

		/// <summary>
		/// Get the total number of LOD levels in the terrain
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public ushort NumLodLevels { get; protected set; }

		/// <summary>
		/// Get the number of LOD levels in a leaf of the terrain quadtree
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public ushort NumLodLevelsPerLeaf { get; protected set; }

		/// <summary>
		/// Get's or set's the render queue group that this terrain will be rendered into
		/// </summary>
		/// <remarks>The default is specified in TerrainGlobalOptions</remarks>
		[OgreVersion( 1, 7, 2 )]
		public RenderQueueGroupID RenderQueueGroupID { get; protected set; }

		#endregion properties

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="sm">The SceneManager to use.</param>
		[OgreVersion( 1, 7, 2 )]
		public Terrain( SceneManager sm )
			: base()
		{
			SceneManager = sm;
			ResourceGroup = string.Empty;

			mRootNode = sm.RootSceneNode.CreateChildSceneNode();
			SceneManager.PreFindVisibleObjects += new FindVisibleObjectsEvent( _preFindVisibleObjects );
			SceneManager.SceneManagerDestroyed += new SceneManagerDestroyedEvent( _sceneManagerDestroyed );
			msBlendTextureGenerator = new NameGenerator<Texture>( "TerrBlend" );

			var wq = Root.Instance.WorkQueue;
			workQueueChannel = wq.GetChannel( "AxiomTerrain" );
			wq.AddRequestHandler( workQueueChannel, this );
			wq.AddResponseHandler( workQueueChannel, this );

			// generate a material name, it's important for the terrain material
			// name to be consistent & unique no matter what generator is being used
			// so use our own pointer as identifier, use FashHash rather than just casting
			// the pointer to a long so we support 64-bit pointers
			mMaterialName = "AxiomTerrain/" + GetHashCode();
		}

		[OgreVersion( 1, 7, 2, "~Terrain" )]
		protected override void dispose( bool disposeManagedResources )
		{
			if ( !IsDisposed )
			{
				if ( disposeManagedResources )
				{
					mDerivedUpdatePendingMask = 0;
					WaitForDerivedProcesses();

					var wq = Root.Instance.WorkQueue;
					wq.RemoveRequestHandler( workQueueChannel, this );
					wq.RemoveResponseHandler( workQueueChannel, this );

					FreeTemporaryResources();
					FreeGPUResources();
					FreeCPUResources();
					if ( SceneManager != null )
					{
						SceneManager.DestroySceneNode( RootSceneNode );
						SceneManager.PreFindVisibleObjects -= _preFindVisibleObjects;
						SceneManager.SceneManagerDestroyed -= _sceneManagerDestroyed;
					}
				}
			}

			base.dispose( disposeManagedResources );
		}

		#region Methods

		/// <summary>
		/// Save terrain data in native form to a standalone file
		/// </summary>
		/// <param name="fileName"></param>
		/// <note>
		/// This is a fairly basic way of saving the terrain, to save to a
		///	file in the resource system, or to insert the terrain data into a
		///	shared file, use the StreamSerialiser form.
		/// </note>
		[OgreVersion( 1, 7, 2 )]
		public void Save( string filename )
		{
			var stream = Root.Instance.CreateFileStream( filename, DerivedResourceGroup, true );
			var ser = new StreamSerializer( stream );
			Save( ser );
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

			if ( IsHeightDataModified )
			{
				// When modifying, for efficiency we only increase the max deltas at each LOD,
				// we never reduce them (since that would require re-examining more samples)
				// Since we now save this data in the file though, we need to make sure we've
				// calculated the optimal
				var rect = new Rectangle();
				rect.Top = 0;
				rect.Bottom = mSize;
				rect.Left = 0;
				rect.Right = mSize;
				CalculateHeightDeltas( rect );
				FinalizeHeightDeltas( rect, false );
			}

			stream.WriteChunkBegin( TERRAIN_CHUNK_ID, TERRAIN_CHUNK_VERSION );

			var align = (byte)Alignment;
			stream.Write( align );

			stream.Write( mSize );
			stream.Write( mWorldSize );
			stream.Write( mMaxBatchSize );
			stream.Write( mMinBatchSize );
			stream.Write( mPos );
			for ( var i = 0; i < mHeightData.Length; i++ )
			{
				stream.Write( mHeightData[ i ] );
			}

			WriteLayerDeclaration( mLayerDecl, ref stream );

			//Layers
			CheckLayers( false );
			var numLayers = (byte)mLayers.Count;
			WriteLayerInstanceList( mLayers, ref stream );

			//packed layer blend data
			if ( mCpuBlendMapStorage.Count > 0 )
			{
				// save from CPU data if it's there, it means GPU data was never created
				stream.Write( mLayerBlendMapSize );

				// load packed cpu data
				var numBlendTex = (byte)GetBlendTextureCount( numLayers );
				for ( var i = 0; i < numBlendTex; ++i )
				{
					var fmt = GetBlendTextureFormat( (byte)i, numLayers );
					var channels = PixelUtil.GetNumElemBytes( fmt );
					var dataSz = channels*mLayerBlendMapSize*mLayerBlendMapSize;
					var pData = mCpuBlendMapStorage[ i ];
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
				var tmpData = new byte[mLayerBlendSizeActual*mLayerBlendSizeActual*4];
				var pTmpDataF = BufferBase.Wrap( tmpData );
				foreach ( var tex in mBlendTextureList )
				{
					var dst = new PixelBox( mLayerBlendSizeActual, mLayerBlendSizeActual, 1, tex.Format, pTmpDataF );
					tex.GetBuffer().BlitToMemory( dst );
					int dataSz = PixelUtil.GetNumElemBytes( tex.Format )*mLayerBlendSizeActual*mLayerBlendSizeActual;
					stream.Write( tmpData );
					stream.Write( dataSz );
				}
			}

			//other data
			//normals
			stream.WriteChunkBegin( TERRAINDERIVEDDATA_CHUNK_ID, TERRAINDERIVEDDATA_CHUNK_VERSION );
			stream.Write( "normalmap" );
			stream.Write( mSize );
			if ( mCpuTerrainNormalMap != null )
			{
				var aData = new byte[mSize*mSize*3];
				using ( var dest = BufferBase.Wrap( aData ) )
				{
					Memory.Copy( mCpuTerrainNormalMap.Data, dest, aData.Length );
				}
				// save from CPU data if it's there, it means GPU data was never created
				stream.Write( aData );
			}
			else
			{
				var tmpData = new byte[mSize*mSize*3];
				using ( var wrap = BufferBase.Wrap( tmpData ) )
				{
					var dst = new PixelBox( mSize, mSize, 1, PixelFormat.BYTE_RGB, wrap );
					TerrainNormalMap.GetBuffer().BlitToMemory( dst );
					stream.Write( tmpData );
				}
				tmpData = null;
			}
			stream.WriteChunkEnd( TERRAINDERIVEDDATA_CHUNK_ID );

			//color map
			if ( IsGlobalColorMapEnabled )
			{
				stream.WriteChunkBegin( TERRAINDERIVEDDATA_CHUNK_ID, TERRAINDERIVEDDATA_CHUNK_VERSION );
				stream.Write( "colormap" );
				stream.Write( GlobalColorMapSize );
				if ( mCpuColorMapStorage != null )
				{
					// save from CPU data if it's there, it means GPU data was never created
					stream.Write( mCpuColorMapStorage );
				}
				else
				{
					var aData = new byte[GlobalColorMapSize*GlobalColorMapSize*3];
					using ( var pDataF = BufferBase.Wrap( aData ) )
					{
						var dst = new PixelBox( GlobalColorMapSize, GlobalColorMapSize, 1, PixelFormat.BYTE_RGB, pDataF );
						GlobalColorMap.GetBuffer().BlitToMemory( dst );
					}
					stream.Write( aData );
				}
				stream.WriteChunkEnd( TERRAINDERIVEDDATA_CHUNK_ID );
			}

			//ligthmap
			if ( mLightMapRequired )
			{
				stream.WriteChunkBegin( TERRAINDERIVEDDATA_CHUNK_ID, TERRAINDERIVEDDATA_CHUNK_VERSION );
				stream.Write( "lightmap" );
				stream.Write( LightMapSize );
				if ( mCpuLightmapStorage != null )
				{
					// save from CPU data if it's there, it means GPU data was never created
					stream.Write( mCpuLightmapStorage );
				}
				else
				{
					var aData = new byte[LightMapSize*LightMapSize];
					using ( var pDataF = BufferBase.Wrap( aData ) )
					{
						var dst = new PixelBox( LightMapSize, LightMapSize, 1, PixelFormat.L8, pDataF );
						LightMap.GetBuffer().BlitToMemory( dst );
					}
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
					var aData = new byte[mCompositeMapSize*mCompositeMapSize*4];
					using ( var pDataF = BufferBase.Wrap( aData ) )
					{
						var dst = new PixelBox( mCompositeMapSize, mCompositeMapSize, 1, PixelFormat.BYTE_RGB, pDataF );
						CompositeMap.GetBuffer().BlitToMemory( dst );
					}
					stream.Write( aData );
				}
				stream.WriteChunkEnd( TERRAINDERIVEDDATA_CHUNK_ID );
			}

			//write deltas
			stream.Write( mDeltaDataPtr );

			//write the quadtree
			QuadTree.Save( stream );

			stream.WriteChunkEnd( TERRAIN_CHUNK_ID );

			IsModified = false;
			IsHeightDataModified = false;
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
			var numSamplers = (byte)decl.Samplers.Count;
			stream.Write( numSamplers );
			foreach ( var sampler in decl.Samplers )
			{
				stream.WriteChunkBegin( TERRAINLAYERSAMPLER_CHUNK_ID, TERRAINLAYERSAMPLER_CHUNK_VERSION );
				stream.Write( sampler.Alias );
				var pixFmt = (byte)sampler.Format;
				stream.Write( pixFmt );
				stream.WriteChunkEnd( TERRAINLAYERSAMPLER_CHUNK_ID );
			}

			//  elements
			var numElems = (byte)decl.Elements.Count;
			stream.Write( numElems );
			foreach ( var elem in decl.Elements )
			{
				stream.WriteChunkBegin( TERRAINLAYERSAMPLERELEMENT_CHUNK_ID, TERRAINLAYERSAMPLERELEMENT_CHUNK_VERSION );
				stream.Write( elem.Source );
				var sem = (byte)elem.Semantic;
				stream.Write( sem );
				stream.Write( elem.ElementStart );
				stream.Write( elem.ElementCount );
				stream.WriteChunkEnd( TERRAINLAYERSAMPLERELEMENT_CHUNK_ID );
			}
			stream.WriteChunkEnd( TERRAINLAYERDECLARATION_CHUNK_ID );
		}

		/// <summary>
		/// Utility method to read a layer declaration from a stream
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public static bool ReadLayerDeclaration( ref StreamSerializer stream, ref TerrainLayerDeclaration targetDecl )
		{
			if ( stream.ReadChunkBegin( TERRAINLAYERDECLARATION_CHUNK_ID, TERRAINLAYERDECLARATION_CHUNK_VERSION ) == null )
			{
				return false;
			}

			//  samplers
			byte numSamplers;
			stream.Read( out numSamplers );
			targetDecl.Samplers = new List<TerrainLayerSampler>( numSamplers );
			for ( var s = 0; s < numSamplers; ++s )
			{
				if ( stream.ReadChunkBegin( TERRAINLAYERSAMPLER_CHUNK_ID, TERRAINLAYERSAMPLER_CHUNK_VERSION ) == null )
				{
					return false;
				}

				var sampler = new TerrainLayerSampler();
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
			for ( var e = 0; e < numElems; ++e )
			{
				if ( stream.ReadChunkBegin( TERRAINLAYERSAMPLERELEMENT_CHUNK_ID, TERRAINLAYERSAMPLERELEMENT_CHUNK_VERSION ) == null )
				{
					return false;
				}

				var samplerElem = new TerrainLayerSamplerElement();

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
		/// Utility method to write a layer instance list to a stream
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public static void WriteLayerInstanceList( List<LayerInstance> layers, ref StreamSerializer stream )
		{
			var numLayers = (byte)layers.Count;
			stream.Write( numLayers );
			foreach ( var inst in layers )
			{
				stream.WriteChunkBegin( TERRAINLAYERINSTANCE_CHUNK_ID, TERRAINLAYERINSTANCE_CHUNK_VERSION );
				stream.Write( inst.WorldSize );
				foreach ( var t in inst.TextureNames )
				{
					stream.Write( t );
				}

				stream.WriteChunkEnd( TERRAINLAYERINSTANCE_CHUNK_ID );
			}
		}

		/// <summary>
		/// Utility method to read a layer instance list from a stream
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public static bool ReadLayerInstanceList( ref StreamSerializer stream, int numSamplers,
		                                          ref List<LayerInstance> targetLayers )
		{
			byte numLayers;
			stream.Read( out numLayers );
			targetLayers = new List<LayerInstance>( numLayers );
			for ( var l = 0; l < numLayers; ++l )
			{
				if ( stream.ReadChunkBegin( TERRAINLAYERINSTANCE_CHUNK_ID, TERRAINLAYERINSTANCE_CHUNK_VERSION ) == null )
				{
					return false;
				}

				var inst = new LayerInstance();

				stream.Read( out inst.WorldSize );
				inst.TextureNames = new List<string>( numSamplers );
				for ( var t = 0; t < numSamplers; ++t )
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
			var stream = (FileStream)ResourceGroupManager.Instance.OpenResource( fileName, DerivedResourceGroup );
			var ser = new StreamSerializer( stream );
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
			{
				return false;
			}

			byte align;
			stream.Read( out align );
			Alignment = (Alignment)align;
			stream.Read( out mSize );
			stream.Read( out mWorldSize );

			stream.Read( out mMaxBatchSize );
			stream.Read( out mMinBatchSize );
			stream.Read( out mPos );
			RootSceneNode.Position = mPos;
			UpdateBaseScale();
			DetermineLodLevels();

			int numVertices = mSize*mSize;
			mHeightData = new float[numVertices];
			stream.Read( out mHeightData );

			// layer declaration
			if ( !ReadLayerDeclaration( ref stream, ref mLayerDecl ) )
			{
				return false;
			}

			CheckDeclaration();

			// Layers
			if ( !ReadLayerInstanceList( ref stream, mLayerDecl.Elements.Count, ref mLayers ) )
			{
				return false;
			}

			DeriveUVMultipliers();

			// Packed layer blend data
			var numLayers = (byte)mLayers.Count;
			stream.Read( out mLayerBlendMapSize );
			mLayerBlendSizeActual = mLayerBlendMapSize; // for now, until we check
			//load packed CPU data
			var numBlendTex = GetBlendTextureCount( numLayers );
			for ( var i = 0; i < numBlendTex; ++i )
			{
				var fmt = GetBlendTextureFormat( (byte)i, numLayers );
				var channels = PixelUtil.GetNumElemBytes( fmt );
				var dataSz = channels*mLayerBlendMapSize*mLayerBlendMapSize;
				var data = new byte[dataSz];
				stream.Read( out data );
				mCpuBlendMapStorage.AddRange( data );
			}

			//derived data
			while ( !stream.IsEndOfChunk( TERRAIN_CHUNK_ID ) && stream.NextChunkId == TERRAINDERIVEDDATA_CHUNK_ID )
			{
				stream.ReadChunkBegin( TERRAINDERIVEDDATA_CHUNK_ID, TERRAINDERIVEDDATA_CHUNK_VERSION );
				//name
				var name = string.Empty;
				stream.Read( out name );
				ushort sz;
				stream.Read( out sz );
				if ( name == "normalmap" )
				{
					mNormalMapRequired = true;
					var data = new byte[sz*sz*3];
					stream.Read( out data );
					using ( var pDataF = BufferBase.Wrap( data ) )
					{
						mCpuTerrainNormalMap = new PixelBox( sz, sz, 1, PixelFormat.BYTE_RGB, pDataF );
					}
				}
				else if ( name == "colormap" )
				{
					IsGlobalColorMapEnabled = true;
					GlobalColorMapSize = sz;
					mCpuColorMapStorage = new byte[sz*sz*3];
					stream.Read( out mCpuColorMapStorage );
				}
				else if ( name == "lightmap" )
				{
					mLightMapRequired = true;
					LightMapSize = sz;
					mCpuLightmapStorage = new byte[sz*sz];
					stream.Read( out mCpuLightmapStorage );
				}
				else if ( name == "compositemap" )
				{
					mCompositeMapRequired = true;
					mCompositeMapSize = sz;
					mCpuCompositeMapStorage = new byte[sz*sz*4];
					stream.Read( out mCpuCompositeMapStorage );
				}

				stream.ReadChunkEnd( TERRAINDERIVEDDATA_CHUNK_ID );
			}

			//Load delta data
			var deltaData = new float[sizeof ( float )*numVertices];
			stream.Read( out deltaData );
			mDeltaDataPtr = BufferBase.Wrap( deltaData );

			//Create and load quadtree
			QuadTree = new TerrainQuadTreeNode( this, null, 0, 0, mSize, (ushort)( NumLodLevels - 1 ), 0, 0 );
			QuadTree.Prepare();

			stream.ReadChunkEnd( TERRAIN_CHUNK_ID );

			DistributeVertexData();

			IsModified = false;
			IsHeightDataModified = false;

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
			if (
				!( Bitwise.IsPow2( importData.TerrainSize - 1 ) && Bitwise.IsPow2( importData.MinBatchSize - 1 ) &&
				   Bitwise.IsPow2( importData.MaxBatchSize - 1 ) ) )
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

			Alignment = importData.TerrainAlign;
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

			int numVertices = mSize*mSize;
			mHeightData = new float[numVertices];

			if ( importData.InputFloat != null )
			{
				if ( Utility.RealEqual( importData.InputBias, 0.0f ) && Utility.RealEqual( importData.InputScale, 1.0f ) )
				{
					//straigt copy
					mHeightData = new float[numVertices];
					Array.Copy( importData.InputFloat, mHeightData, mHeightData.Length );
				}
				else
				{
					// scale & bias, lets do it unsafe, should be faster :)
					var src = importData.InputFloat;
					for ( var i = 0; i < numVertices; ++i )
					{
						mHeightData[ i ] = ( src[ i ]*importData.InputScale ) + importData.InputBias;
					}
				}
			}
			else if ( importData.InputImage != null )
			{
				var img = importData.InputImage;
				if ( img.Width != mSize || img.Height != mSize )
				{
					img.Resize( mSize, mSize );
				}

				// convert image data to floats
				// Do this on a row-by-row basis, because we describe the terrain in
				// a bottom-up fashion (ie ascending world coords), while Image is top-down
				var pSrcBaseF = BufferBase.Wrap( img.Data );
				var pHeightDataF = BufferBase.Wrap( mHeightData );
				for ( var i = 0; i < mSize; ++i )
				{
					var srcy = mSize - i - 1;
					using ( var pSrc = pSrcBaseF + srcy*img.RowSpan )
					{
						using ( var pDest = pHeightDataF + i*mSize*sizeof ( float ) )
						{
							PixelConverter.BulkPixelConversion( pSrc, img.Format, pDest, PixelFormat.FLOAT32_R, mSize );
						}
					}
				}

				pSrcBaseF.Dispose();
				pHeightDataF.Dispose();

				if ( !Utility.RealEqual( importData.InputBias, 0.0f ) || !Utility.RealEqual( importData.InputScale, 1.0f ) )
				{
					for ( int i = 0; i < numVertices; ++i )
					{
						mHeightData[ i ] = ( mHeightData[ i ]*importData.InputScale ) + importData.InputBias;
					}
				}
			}
			else
			{
				// start with flat terrain
				mHeightData = new float[mSize*mSize];
			}

			var deltaData = new float[numVertices];

			mHeightDataPtr = BufferBase.Wrap( mHeightData );
			mDeltaDataPtr = BufferBase.Wrap( deltaData );

			var numLevel = (ushort)(int)( NumLodLevels - 1 );
			QuadTree = new TerrainQuadTreeNode( this, null, 0, 0, mSize, (ushort)( NumLodLevels - 1 ), 0, 0 );
			QuadTree.Prepare();

			//calculate entire terrain
			var rect = new Rectangle();
			rect.Top = 0;
			rect.Bottom = mSize;
			rect.Left = 0;
			rect.Right = mSize;
			CalculateHeightDeltas( rect );
			FinalizeHeightDeltas( rect, true );

			DistributeVertexData();

			// Imported data is treated as modified because it's not saved
			IsModified = true;
			IsHeightDataModified = true;

			return true;
		}

		[OgreVersion( 1, 7, 2 )]
		protected void CopyGlobalOptions()
		{
			SkirtSize = TerrainGlobalOptions.SkirtSize;
			RenderQueueGroupID = TerrainGlobalOptions.RenderQueueGroup;
			VisibilityFlags = TerrainGlobalOptions.VisibilityFlags;
			QueryFlags = TerrainGlobalOptions.QueryFlags;
			mLayerBlendMapSize = TerrainGlobalOptions.LayerBlendMapSize;
			mLayerBlendSizeActual = mLayerBlendMapSize; // for now, until we check
			LightMapSize = TerrainGlobalOptions.LightMapSize;
			mLightmapSizeActual = LightMapSize; // for now, until we check
			mCompositeMapSize = TerrainGlobalOptions.CompositeMapSize;
			mCompositeMapSizeActual = mCompositeMapSize; // for now, until we check
		}

		[OgreVersion( 1, 7, 2 )]
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
			*/
			NumLodLevelsPerLeaf = (ushort)( Utility.Log2( mMaxBatchSize - 1 ) - Utility.Log2( mMinBatchSize - 1 ) + 1 );
			NumLodLevels = (ushort)( Utility.Log2( mSize - 1 ) - Utility.Log2( mMinBatchSize - 1 ) + 1 );
			mTreeDepth = (ushort)( NumLodLevels - NumLodLevelsPerLeaf + 1 );

			LogManager.Instance.Write(
				"Terrain created; size={0}, minBatch={1}, maxBatch={2}, treedepth={3}, lodLevels={4}, leafNodes={5}", mSize,
				mMinBatchSize, mMaxBatchSize, mTreeDepth, NumLodLevels, NumLodLevelsPerLeaf );
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
			logMgr.Write( LogMessageLevel.Trivial, false,
			              @"Terrain.DistributeVertexData processing source 
				terrain size of " + mSize, null );

			var depth = mTreeDepth;
			var prevDepth = depth;
			var currentresolution = mSize;
			var bakedresolution = mSize;
			var targetsplits = (ushort)( ( bakedresolution - 1 )/( TERRAIN_MAX_BATCH_SIZE - 1 ) );
			while ( depth-- != 0 && targetsplits != 0 )
			{
				var splits = (ushort)( 1 << depth );
				if ( splits == targetsplits )
				{
					logMgr.Write( LogMessageLevel.Trivial, false,
					              "Assigning vertex data, resolution={0}, startDepth={1}, endDepth={2}, splits={3}", bakedresolution,
					              depth, prevDepth, splits );
					// vertex data goes at this level, at bakedresolution
					// applies to all lower levels (except those with a closer vertex data)
					// determine physical size (as opposed to resolution)
					int sz = ( ( bakedresolution - 1 )/splits ) + 1;
					QuadTree.AssignVertexData( depth, prevDepth, bakedresolution, (ushort)sz );

					// next set to look for
					bakedresolution = (ushort)( ( ( currentresolution - 1 ) >> 1 ) + 1 );
					targetsplits = (ushort)( ( bakedresolution - 1 )/( TERRAIN_MAX_BATCH_SIZE - 1 ) );
					prevDepth = depth;
				}

				currentresolution = (ushort)( ( ( currentresolution - 1 ) >> 1 ) + 1 );
			}

			// Always assign vertex data to the top of the tree
			if ( prevDepth > 0 )
			{
				QuadTree.AssignVertexData( 0, 1, bakedresolution, bakedresolution );
				logMgr.Write( LogMessageLevel.Trivial, false,
				              "Assigning vertex data, resolution: {0}, startDepth=0, endDepth=1, splits=1", bakedresolution );
			}

			logMgr.Write( LogMessageLevel.Trivial, false, "Terrain.DistributeVertexdata finished" );
		}

		/// <summary>
		/// Prepare and load the terrain in one simple call from a standalone file.
		/// </summary>
		/// <param name="fileName"></param>
		/// <note>
		/// This method must be called from the primary render thread. To load data
		///	in a background thread, use the prepare() method.
		/// </note>
		[OgreVersion( 1, 7, 2 )]
		public void Load( string fileName )
		{
			if ( Prepare( fileName ) )
			{
				Load();
			}
			else
			{
				throw new AxiomException( "Error while preparing {0}, see log for details. Terrain.Load", fileName );
			}
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
			{
				Load();
			}
			else
			{
				throw new AxiomException( "Error while preparing from stream, see log for details. Terrain.Load" );
			}
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
			if ( IsLoaded )
			{
				return;
			}

			if ( QuadTree != null )
			{
				QuadTree.Load();
			}

			CheckLayers( true );
			CreateOrDestroyGPUColorMap();
			CreateOrDestroyGPUNormalMap();
			CreateOrDestroyGPULightmap();
			CreateOrDestroyGPUCompositeMap();

			mMaterialGenerator.RequestOption( this );

			IsLoaded = true;
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
			if ( !IsLoaded )
			{
				return;
			}

			if ( QuadTree != null )
			{
				QuadTree.Unload();
			}

			// free own buffers if used, but not custom
			mDefaultGpuBufferAllocator.FreeAllBuffers();

			IsLoaded = false;
			IsModified = false;
			IsHeightDataModified = false;
		}

		/// <summary>
		/// Free CPU resources created during prepare methods.
		/// </summary>
		/// <remarks>
		/// This is safe to do in a background thread after calling unload().
		/// </remarks>
		[OgreVersion( 1, 7, 2 )]
		public void Unprepare()
		{
			if ( QuadTree != null )
			{
				QuadTree.Unprepare();
			}
		}

		/// <summary>
		/// Get a pointer to the height data for a given point.
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public BufferBase GetHeightData( long x, long y )
		{
			System.Diagnostics.Debug.Assert( x >= 0 && x < mSize && y >= 0 && y < mSize, "Out of bounds.." );
			return mHeightDataPtr + ( y*mSize + x )*sizeof ( float );
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

			return mHeightData[ y + mSize*x ];
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

			mHeightData[ y + mSize*x ] = heightVal;
			var rec = new Rectangle();
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
		[OgreVersion( 1, 7, 2 )]
		public float GetHeightAtTerrainPosition( Real x, Real y )
		{
			// get left / bottom points (rounded down)
			Real factor = mSize - 1;
			Real invFactor = 1.0f/factor;

			var startX = (long)( x*factor );
			var startY = (long)( y*factor );
			var endX = startX + 1;
			var endY = startY + 1;

			// now get points in terrain space (effectively rounding them to boundaries)
			// note that we do not clamp! We need a valid plane
			Real startXTS = startX*invFactor;
			Real startYTS = startY*invFactor;
			Real endXTS = endX*invFactor;
			Real endYTS = endY*invFactor;

			//now clamp
			endX = Utility.Min( endX, (long)mSize - 1 );
			endY = Utility.Min( endY, (long)mSize - 1 );

			// get parametric from start coord to next point
			Real xParam = ( x - startXTS )/invFactor;
			Real yParam = ( y - startYTS )/invFactor;

			/* For even / odd tri strip rows, triangles are this shape:
				even     odd
				3---2   3---2
				| / |   | \ |
				0---1   0---1
				*/

			// Build all 4 positions in terrain space, using point-sampled height
			var v0 = new Vector3( startXTS, startYTS, GetHeightAtPoint( startX, startY ) );
			var v1 = new Vector3( endXTS, startYTS, GetHeightAtPoint( endX, startY ) );
			var v2 = new Vector3( endXTS, endYTS, GetHeightAtPoint( endX, endY ) );
			var v3 = new Vector3( startXTS, endYTS, GetHeightAtPoint( startX, endY ) );
			//define this plane in terrain space
			var plane = new Plane();
			if ( startY%2 != 0 )
			{
				//odd row
				var secondTri = ( ( 1.0f - yParam ) > xParam );
				if ( secondTri )
				{
					plane.Redefine( v0, v1, v3 );
				}
				else
				{
					plane.Redefine( v1, v2, v3 );
				}
			}
			else
			{
				//even row
				var secondtri = ( yParam > xParam );
				if ( secondtri )
				{
					plane.Redefine( v0, v2, v3 );
				}
				else
				{
					plane.Redefine( v0, v1, v2 );
				}
			}

			//solve plane quation for z
			return ( -plane.Normal.x*x - plane.Normal.y*y - plane.D )/plane.Normal.z;
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
		[OgreVersion( 1, 7, 2 )]
		public float GetHeightAtWorldPosition( Real x, Real y, Real z )
		{
			var terrPos = Vector3.Zero;
			GetTerrainPosition( x, y, z, ref terrPos );
			return GetHeightAtTerrainPosition( terrPos.x, terrPos.y );
		}

		/// <summary>
		/// Get the height data for a given world position (projecting the point
		/// down on to the terrain).
		/// </summary>
		/// <param name="pos">Position in world space. Positions will be clamped to the edge
		/// of the terrain</param>
		[OgreVersion( 1, 7, 2 )]
		public float GetHeightAtWorldPosition( Vector3 pos )
		{
			return GetHeightAtWorldPosition( pos.x, pos.y, pos.z );
		}

		/// <summary>
		/// Get a pointer to the delta data for a given point. 
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public BufferBase GetDeltaData( long x, long y )
		{
			System.Diagnostics.Debug.Assert( x >= 0 && x < mSize && y >= 0 && y < mSize, "Out of bounds.." );
			return mDeltaDataPtr + ( y*mSize + x )*sizeof ( float );
		}

		/// <summary>
		/// Convert a position from one space to another with respect to this terrain.
		/// </summary>
		/// <param name="inSpace"> The space that inPos is expressed as</param>
		/// <param name="inPos">The incoming position</param>
		/// <param name="outSpace">The space which outPos should be expressed as</param>
		[OgreVersion( 1, 7, 2 )]
		public Vector3 ConvertPosition( Space inSpace, Vector3 inPos, Space outSpace )
		{
			var ret = Vector3.Zero;
			ConvertPosition( inSpace, inPos, outSpace, ref ret );
			return ret;
		}

		/// <summary>
		/// Convert a direction from one space to another with respect to this terrain.
		/// </summary>
		/// <param name="inSpace">The space that inDir is expressed as</param>
		/// <param name="inDir">The incoming direction</param>
		/// <param name="outSpace">The space which outDir should be expressed as</param>
		/// <returns>The output direction </returns>
		[OgreVersion( 1, 7, 2 )]
		public Vector3 ConvertDirection( Space inSpace, Vector3 inDir, Space outSpace )
		{
			var ret = Vector3.Zero;
			ConvertDirection( inSpace, inDir, outSpace, ref ret );
			return ret;
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

		[OgreVersion( 1, 7, 2 )]
		protected void ConvertSpace( Space inSpace, Vector3 inVec, Space outSpace, ref Vector3 outVec, bool translation )
		{
			var currSpace = inSpace;
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
								{
									outVec += mPos;
								}
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
									outVec.x -= mBase;
									outVec.y -= mBase;
									outVec.x /= ( mSize - 1 )*mScale;
									outVec.y /= ( mSize - 1 )*mScale;
								}
								currSpace = Space.TerrainSpace;
							}
								break;
						} //end outSpace
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
									outVec.x *= ( mSize - 1 )*mScale;
									outVec.y *= ( mSize - 1 )*mScale;
									outVec.x += mBase;
									outVec.y += mBase;
								}
								outVec = convertTerrainToWorldAxes( outVec );
								currSpace = Space.LocalSpace;
							}
								break;

							case Space.PointSpace:
							{
								outVec.x *= ( mSize - 1 );
								outVec.y *= ( mSize - 1 );
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
						{
							outVec.x /= ( mSize - 1 );
						}
						outVec.y /= ( mSize - 1 );

						currSpace = Space.TerrainSpace;
					}
						break;
				} //end main switch
			} //end while
		}

		[OgreVersion( 1, 7, 2 )]
		public static void ConvertWorldToTerrainAxes( Alignment align, Vector3 worldVec, out Vector3 terrainVec )
		{
			terrainVec = new Vector3();
			switch ( align )
			{
				case Alignment.Align_X_Z:
					terrainVec.z = worldVec.y;
					terrainVec.x = worldVec.x;
					terrainVec.y = -worldVec.z;
					break;

				case Alignment.Align_Y_Z:
					terrainVec.z = worldVec.x;
					terrainVec.x = -worldVec.z;
					terrainVec.y = worldVec.y;
					break;

				case Alignment.Align_X_Y:
					terrainVec = worldVec;
					break;
			}
		}

		[OgreVersion( 1, 7, 2 )]
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

		[OgreVersion( 1, 7, 2 )]
		protected Vector3 convertWorldToTerrainAxes( Vector3 inVec )
		{
			Vector3 ret;
			ConvertWorldToTerrainAxes( Alignment, inVec, out ret );
			return ret;
		}

		[OgreVersion( 1, 7, 2 )]
		protected Vector3 convertTerrainToWorldAxes( Vector3 inVec )
		{
			Vector3 ret;
			ConvertTerrainToWorldAxes( Alignment, inVec, out ret );
			return ret;
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
		[OgreVersion( 1, 7, 2 )]
		public void GetPoint( long x, long y, ref Vector3 outpos )
		{
			GetPointAlign( x, y, Alignment, ref outpos );
		}

		/// <summary>
		/// Get a Vector3 of the world-space point on the terrain, supplying the
		/// height data manually (can be more optimal). 
		/// @note This point is relative to Terrain.Position
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public void GetPoint( long x, long y, float height, ref Vector3 outpos )
		{
			GetPointAlign( x, y, height, Alignment, ref outpos );
		}

		/// <summary>
		/// Get a Vector3 of the world-space point on the terrain, aligned Y-up always.
		/// @note This point is relative to Terrain.Position
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public void GetPointAlign( long x, long y, Alignment align, ref Vector3 outPos )
		{
			var height = mHeightData[ y + mSize*x ];
			GetPointAlign( x, y, height, align, ref outPos );
		}

		/// <summary>
		/// Get a Vector3 of the world-space point on the terrain, supplying the
		/// height data manually (can be more optimal). 
		/// @note This point is relative to Terrain.Position
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public void GetPointAlign( long x, long y, float height, Alignment align, ref Vector3 outPos )
		{
			switch ( align )
			{
				case Alignment.Align_X_Z:
					outPos.y = height;
					outPos.x = x*mScale + mBase;
					outPos.z = y*-mScale - mBase;
					break;

				case Alignment.Align_Y_Z:
					outPos.x = height;
					outPos.z = x*-mScale - mBase;
					outPos.y = y*mScale + mBase;
					break;

				case Alignment.Align_X_Y:
					outPos.z = height;
					outPos.x = x*mScale + mBase;
					outPos.y = y*mScale + mBase;
					break;
			}
		}

		/// <summary>
		/// Translate a vector into world space based on the alignment options.
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public void GetVector( Vector3 invec, ref Vector3 outVec )
		{
			GetVectorAlign( invec.x, invec.y, invec.z, Alignment, ref outVec );
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
		[OgreVersion( 1, 7, 2 )]
		public void GetVector( Real x, Real y, Real z, ref Vector3 outVec )
		{
			GetVectorAlign( x, y, z, Alignment, ref outVec );
		}

		/// <summary>
		/// Translate a vector into world space based on a specified alignment.
		/// </summary>
		/// <param name="invec">The vector in basis space, where x/y represents the 
		/// terrain plane and z represents the up vector</param>
		[OgreVersion( 1, 7, 2 )]
		public void GetVectorAlign( Vector3 invec, Alignment align, ref Vector3 outVec )
		{
			GetVectorAlign( invec.x, invec.y, invec.z, align, ref outVec );
		}

		/// <summary>
		/// Translate a vector into world space based on a specified alignment.
		/// </summary>
		/// <param name="x">The vector in basis space, where x/y represents the terrain plane and z represents the up vector</param>
		/// <param name="y">The vector in basis space, where x/y represents the terrain plane and z represents the up vector</param>
		/// <param name="z">The vector in basis space, where x/y represents the terrain plane and z represents the up vector</param>
		[OgreVersion( 1, 7, 2 )]
		public void GetVectorAlign( Real x, Real y, Real z, Alignment align, ref Vector3 outVec )
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
		/// Convert a position from terrain basis space to world space.
		/// </summary>
		/// <param name="TSPos">Terrain space position, where (0,0) is the bottom-left of the
		/// terrain, and (1,1) is the top-right. The Z coordinate is in absolute height units.</param>
		/// <param name="outWSpos">World space output position (setup according to current alignment).</param>
		/// <remarks>This position is relative to Terrain.Position</remarks>
		[OgreVersion( 1, 7, 2 )]
		public void GetPosition( Vector3 TSPos, ref Vector3 outWSpos )
		{
			GetPositionAlign( TSPos, Alignment, ref outWSpos );
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
		/// <param name="outWSpos">World space output position (setup according to current alignment).</param>
		/// <remarks>This position is relative to Terrain.Position</remarks>
		[OgreVersion( 1, 7, 2 )]
		public void GetPosition( Real x, Real y, Real z, ref Vector3 outWSpos )
		{
			GetPositionAlign( x, y, z, Alignment, ref outWSpos );
		}

		/// <summary>
		/// Convert a position from world space to terrain basis space. 
		/// </summary>
		/// <param name="WSpos">World space position (setup according to current alignment). </param>
		/// <param name="outTSpos">Terrain space output position, where (0,0) is the bottom-left of the
		/// terrain, and (1,1) is the top-right. The Z coordinate is in absolute
		/// height units.</param>
		[OgreVersion( 1, 7, 2 )]
		public void GetTerrainPosition( Vector3 WSpos, ref Vector3 outTSpos )
		{
			GetTerrainPositionAlign( WSpos, Alignment, ref outTSpos );
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
		[OgreVersion( 1, 7, 2 )]
		public void GetTerrainPosition( Real x, Real y, Real z, ref Vector3 outTSpos )
		{
			GetTerrainPositionAlign( x, y, z, Alignment, ref outTSpos );
		}

		/// <summary>
		/// Convert a position from terrain basis space to world space based on a specified alignment. 
		/// </summary>
		/// <param name="TSpos">Terrain space position, where (0,0) is the bottom-left of the
		///	terrain, and (1,1) is the top-right. The Z coordinate is in absolute
		///	height units.</param>
		/// <param name="outWSpos">World space output position (setup according to alignment). </param>
		[OgreVersion( 1, 7, 2 )]
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
		/// <param name="outWSpos">World space output position (setup according to alignment). </param>
		[OgreVersion( 1, 7, 2 )]
		public void GetPositionAlign( Real x, Real y, Real z, Alignment align, ref Vector3 outWSpos )
		{
			switch ( align )
			{
				case Alignment.Align_X_Z:
					outWSpos.y = z;
					outWSpos.x = x*( mSize - 1 )*mScale + mBase;
					outWSpos.z = y*( mSize - 1 )*-mScale - mBase;
					break;

				case Alignment.Align_Y_Z:
					outWSpos.x = z;
					outWSpos.y = y*( mSize - 1 )*mScale + mBase;
					outWSpos.z = x*( mSize - 1 )*-mScale - mBase;
					break;

				case Alignment.Align_X_Y:
					outWSpos.z = z;
					outWSpos.x = x*( mSize - 1 )*mScale + mBase;
					outWSpos.y = y*( mSize - 1 )*mScale + mBase;
					break;
			}
		}

		/// <summary>
		/// Convert a position from world space to terrain basis space based on a specified alignment. 
		/// </summary>
		/// <param name="WSpos">World space position (setup according to alignment). </param>
		/// <param name="outTSpos"> Terrain space output position, where (0,0) is the bottom-left of the
		/// terrain, and (1,1) is the top-right. The Z coordinate is in absolute
		/// height units.</param>
		[OgreVersion( 1, 7, 2 )]
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
		/// <param name="outTSpos">Terrain space output position, where (0,0) is the bottom-left of the
		/// terrain, and (1,1) is the top-right. The Z coordinate is in absolute
		/// height units.</param>
		[OgreVersion( 1, 7, 2 )]
		public void GetTerrainPositionAlign( Real x, Real y, Real z, Alignment align, ref Vector3 outTSpos )
		{
			switch ( align )
			{
				case Alignment.Align_X_Z:
					outTSpos.x = ( x - mBase - mPos.x )/( ( mSize - 1 )*mScale );
					outTSpos.y = ( z + mBase - mPos.z )/( ( mSize - 1 )*-mScale );
					outTSpos.z = y;
					break;

				case Alignment.Align_Y_Z:
					outTSpos.x = ( z - mBase - mPos.z )/( ( mSize - 1 )*-mScale );
					outTSpos.y = ( y + mBase - mPos.y )/( ( mSize - 1 )*mScale );
					outTSpos.z = x;
					break;

				case Alignment.Align_X_Y:
					outTSpos.x = ( x - mBase - mPos.x )/( ( mSize - 1 )*mScale );
					outTSpos.y = ( y - mBase - mPos.y )/( ( mSize - 1 )*mScale );
					outTSpos.z = z;
					break;
			}
		}

		/// <summary>
		/// Translate a vector from world space to local terrain space based on the alignment options.
		/// </summary>
		/// <param name="inVec">The vector in basis space, where x/y represents the 
		/// terrain plane and z represents the up vector</param>
		[OgreVersion( 1, 7, 2 )]
		public void GetTerrainVector( Vector3 inVec, ref Vector3 outVec )
		{
			GetTerrainVectorAlign( inVec.x, inVec.y, inVec.z, Alignment, ref outVec );
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
		[OgreVersion( 1, 7, 2 )]
		public void GetTerrainVector( Real x, Real y, Real z, ref Vector3 outVec )
		{
			GetTerrainVectorAlign( x, y, z, Alignment, ref outVec );
		}

		/// <summary>
		/// Translate a vector from world space to local terrain space based on the alignment options.
		/// </summary>
		/// <param name="inVec">The vector in basis space, where x/y represents the 
		/// terrain plane and z represents the up vector</param>
		[OgreVersion( 1, 7, 2 )]
		public void GetTerrainVectorAlign( Vector3 inVec, Alignment align, ref Vector3 outVec )
		{
			GetTerrainVectorAlign( inVec.x, inVec.y, inVec.z, align, ref outVec );
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
		[OgreVersion( 1, 7, 2 )]
		public void GetTerrainVectorAlign( Real x, Real y, Real z, Alignment align, ref Vector3 outVec )
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
		/// How large an area in world space the texture in a terrain layer covers
		/// before repeating.
		/// </summary>
		/// <param name="index">The layer index.</param>
		[OgreVersion( 1, 7, 2 )]
		public Real GetLayerWorldSize( byte index )
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
		[OgreVersion( 1, 7, 2 )]
		public void SetLayerWorldSize( byte index, Real size )
		{
			if ( index < mLayers.Count )
			{
				if ( index >= mLayerUVMultiplier.Count )
				{
					mLayerUVMultiplier.Add( mWorldSize/size );
				}
				else
				{
					mLayerUVMultiplier[ index ] = mWorldSize/size;
				}

				var inst = mLayers[ index ];
				inst.WorldSize = size;
				mMaterialParamsDirty = true;
				IsModified = true;
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
		[OgreVersion( 1, 7, 2 )]
		public Real GetLayerUVMultiplier( byte index )
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

		[OgreVersion( 1, 7, 2 )]
		protected void DeriveUVMultipliers()
		{
			mLayerUVMultiplier.Capacity = mLayers.Count;
			mLayerUVMultiplier.Clear();
			for ( var i = 0; i < mLayers.Count; ++i )
			{
				var inst = mLayers[ i ];
				mLayerUVMultiplier.Add( mWorldSize/inst.WorldSize );
			}
		}

		/// <summary>
		/// Get the name of the texture bound to a given index within a given layer.
		/// See the LayerDeclaration for a list of sampelrs within a layer.
		/// </summary>
		/// <param name="layerIndex">The layer index.</param>
		/// <param name="samplerIndex"> The sampler index within a layer</param>
		[OgreVersion( 1, 7, 2 )]
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
					IsModified = true;
				}
			}
		}

		[OgreVersion( 1, 7, 2 )]
		protected void UpdateBaseScale()
		{
			//centre the terrain on local origin
			mBase = -mWorldSize*0.5f;
			// scale determines what 1 unit on the grid becomes in world space
			mScale = mWorldSize/(Real)( mSize - 1 );
		}

		/// <summary>
		/// Mark the entire terrain as dirty. 
		/// By marking a section of the terrain as dirty, you are stating that you have
		/// changed the height data within this rectangle. This rectangle will be merged with
		/// any existing outstanding changes. To finalise the changes, you must 
		/// call update(), updateGeometry(), or updateDerivedData().
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public void Dirty()
		{
			var rect = new Rectangle();
			rect.Top = 0;
			rect.Bottom = mSize;
			rect.Left = 0;
			rect.Right = mSize;
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

			IsModified = true;
			IsHeightDataModified = true;
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
		internal void DirtyCompositeMapRect( Rectangle rect )
		{
			mCompositeMapDirtyRect.Merge( rect );
			IsModified = true;
		}

		/// <summary>
		/// Mark a region of the lightmap as dirty.
		/// </summary>
		/// <remarks>
		/// You only need to call this if you need to tell the terrain to update
		/// the lightmap data for some reason other than the terrain geometry
		/// has changed. Changing terrain geometry automatically dirties the
		/// correct lightmap areas.
		/// @note
		/// The lightmap won't actually be updated until update() or updateDerivedData()
		/// is called.
		/// </remarks>
		[OgreVersion( 1, 7, 2 )]
		public void DirtyLightmapRect( Rectangle rect )
		{
			mDirtyDerivedDataRect.Merge( rect );
			IsModified = true;
		}

		/// <summary>
		/// Mark a the entire lightmap as dirty.
		/// </summary>
		/// <remarks>
		/// You only need to call this if you need to tell the terrain to update
		/// the lightmap data for some reason other than the terrain geometry
		/// has changed. Changing terrain geometry automatically dirties the
		/// correct lightmap areas.
		/// @note
		/// The lightmap won't actually be updated until update() or updateDerivedData()
		/// is called.
		/// </remarks>
		[OgreVersion( 1, 7, 2 )]
		public void DirtyLightmap()
		{
			var rect = new Rectangle();
			rect.Top = 0;
			rect.Bottom = mSize;
			rect.Left = 0;
			rect.Right = mSize;
			DirtyLightmapRect( rect );
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
#if NET_40
		public void Update( bool synchronous = false )
#else
		public void Update( bool synchronous )
#endif
		{
			UpdateGeometry();
			UpdateDerivedData( synchronous );
		}

#if !NET_40
		/// <see cref="Terrain.Update(bool)"/>
		public void Update()
		{
			Update( false );
		}
#endif

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
				QuadTree.UpdateVertexData( true, false, mDirtyGeometryRect, false );
				mDirtyGeometryRect.IsNull = true;
			}

			//propagate changes
			NotifyNeighbours();
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
		/// <param name="typeMask">Mask indicating the types of data we should generate</param>
		[OgreVersion( 1, 7, 2 )]
#if NET_40
		public void UpdateDerivedData( bool synchrounus = false, byte typeMask = 0xFF )
#else
		public void UpdateDerivedData( bool synchrounus, byte typeMask )
#endif
		{
			if ( !mDirtyDerivedDataRect.IsNull || !mDirtyLightmapFromNeighboursRect.IsNull )
			{
				IsModified = true;
				if ( IsDerivedDataUpdateInProgress )
				{
					// Don't launch many updates, instead wait for the other one 
					// to finish and issue another afterwards.
					mDerivedUpdatePendingMask |= typeMask;
				}
				else
				{
					UpdateDerivedDataImpl( mDirtyDerivedDataRect, mDirtyLightmapFromNeighboursRect, synchrounus, typeMask );
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

#if !NET_40
		/// <see cref="Terrain.UpdateDerivedData(bool, byte)"/>
		public void UpdateDerivedData()
		{
			UpdateDerivedData( false, 0xFF );
		}

		/// <see cref="Terrain.UpdateDerivedData(bool, byte)"/>
		public void UpdateDerivedData( bool synchronous )
		{
			UpdateDerivedData( synchronous, 0xFF );
		}
#endif

		[OgreVersion( 1, 7, 2 )]
		protected void UpdateDerivedDataImpl( Rectangle rect, Rectangle lightmapExtraRect, bool synchronous, byte typeMask )
		{
			IsDerivedDataUpdateInProgress = true;
			mDerivedUpdatePendingMask = 0;

			var req = new DerivedDataRequest();
			req.Terrain = this;
			req.DirtyRect = rect;
			req.LightmapExtraDirtyRect = lightmapExtraRect;
			req.TypeMask = typeMask;

			if ( !mNormalMapRequired )
			{
				req.TypeMask = (byte)( req.TypeMask & ~DERIVED_DATA_NORMALS );
			}

			if ( !mLightMapRequired )
			{
				req.TypeMask = (byte)( req.TypeMask & ~DERIVED_DATA_LIGHTMAP );
			}

			Root.Instance.WorkQueue.AddRequest( workQueueChannel, WORKQUEUE_DERIVED_DATA_REQUEST, req, 0, synchronous );
		}

		[OgreVersion( 1, 7, 2 )]
		protected void WaitForDerivedProcesses()
		{
			while ( IsDerivedDataUpdateInProgress )
			{
				//we need to wait for this to finish
				System.Threading.Thread.Sleep( 50 );
				Root.Instance.WorkQueue.ProcessResponses();
			}
		}

		[OgreVersion( 1, 7, 2 )]
		protected void FreeCPUResources()
		{
			mHeightData = null;

			mDeltaDataPtr.SafeDispose();
			mDeltaDataPtr = null;

			QuadTree.SafeDispose();
			QuadTree = null;

			if ( mCpuTerrainNormalMap != null )
			{
				mCpuTerrainNormalMap.Data.SafeDispose();
				mCpuTerrainNormalMap.Data = null;

				mCpuTerrainNormalMap = null;
			}

			mCpuColorMapStorage = null;
			mCpuLightmapStorage = null;
			mCpuCompositeMapStorage = null;
		}

		[OgreVersion( 1, 7, 2 )]
		protected void FreeGPUResources()
		{
			//remove textures
			var tmgr = TextureManager.Instance;
			if ( tmgr != null )
			{
				foreach ( var tex in mBlendTextureList )
				{
					tmgr.Remove( tex.Handle );
				}

				mBlendTextureList.Clear();

				if ( TerrainNormalMap != null )
				{
					tmgr.Remove( TerrainNormalMap.Handle );
					TerrainNormalMap = null;
				}

				if ( GlobalColorMap != null )
				{
					tmgr.Remove( GlobalColorMap.Handle );
					GlobalColorMap = null;
				}

				if ( LightMap != null )
				{
					tmgr.Remove( LightMap.Handle );
					LightMap = null;
				}

				if ( CompositeMap != null )
				{
					tmgr.Remove( CompositeMap.Handle );
					CompositeMap = null;
				}

				if ( mMaterial != null )
				{
					MaterialManager.Instance.Remove( mMaterial.Handle );
					mMaterial = null;
				}

				if ( mCompositeMapMaterial != null )
				{
					MaterialManager.Instance.Remove( mCompositeMapMaterial.Handle );
					mCompositeMapMaterial = null;
				}
			}
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
			var clampedRect = new Rectangle( rect );

			clampedRect.Left = Utility.Max( 0L, clampedRect.Left );
			clampedRect.Top = Utility.Max( 0L, clampedRect.Top );
			clampedRect.Right = Utility.Min( (long)mSize, clampedRect.Right );
			clampedRect.Bottom = Utility.Min( (long)mSize, clampedRect.Bottom );

			var finalRect = new Rectangle( clampedRect );
			QuadTree.PreDeltaCalculation( clampedRect );

			// Iterate over target levels, 
			for ( var targetLevel = 1; targetLevel < NumLodLevels; ++targetLevel )
			{
				var sourceLevel = targetLevel - 1;
				var step = 1 << targetLevel;

				// need to widen the dirty rectangle since change will affect surrounding
				// vertices at lower LOD
				var widendRect = rect;
				widendRect.Left = Utility.Max( 0L, widendRect.Left - step );
				widendRect.Top = Utility.Max( 0L, widendRect.Top - step );
				widendRect.Right = Utility.Min( (long)mSize, widendRect.Right + step );
				widendRect.Bottom = Utility.Min( (long)mSize, widendRect.Bottom + step );

				// keep a merge of the widest
				finalRect = finalRect.Merge( widendRect );

				// now round the rectangle at this level so that it starts & ends on 
				// the step boundaries
				var lodRect = new Rectangle( widendRect );
				lodRect.Left -= lodRect.Left%step;
				lodRect.Top -= lodRect.Top%step;
				if ( lodRect.Right%step != 0 )
				{
					lodRect.Right += step - ( lodRect.Right%step );
				}
				if ( lodRect.Bottom%step != 0 )
				{
					lodRect.Bottom += step - ( lodRect.Bottom%step );
				}

				for ( var j = (int)lodRect.Top; j < lodRect.Bottom - step; j += step )
				{
					for ( var i = (int)lodRect.Left; i < lodRect.Right - step; i += step )
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
						var v0 = Vector3.Zero;
						var v1 = Vector3.Zero;
						var v2 = Vector3.Zero;
						var v3 = Vector3.Zero;

						GetPointAlign( i, j, Alignment.Align_X_Y, ref v0 );
						GetPointAlign( i + step, j, Alignment.Align_X_Y, ref v1 );
						GetPointAlign( i, j + step, Alignment.Align_X_Y, ref v2 );
						GetPointAlign( i + step, j + step, Alignment.Align_X_Y, ref v3 );

						var t1 = new Plane();
						var t2 = new Plane();
						var backwardTri = false;
						// Odd or even in terms of target level
						if ( ( j/step )%2 == 0 )
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
						var yubound = ( j == ( mSize - step ) ? step : step - 1 );
						for ( var y = 0; y <= yubound; y++ )
						{
							// include the rightmost col of vertices if this is the last col
							var xubound = ( i == ( mSize - step ) ? step : step - 1 );
							for ( var x = 0; x <= xubound; x++ )
							{
								var fulldetailx = i + x;
								var fulldetaily = j + y;
								if ( fulldetailx%step == 0 && fulldetaily%step == 0 )
								{
									// Skip, this one is a vertex at this level
									continue;
								}
								var ypct = (Real)y/(Real)step;
								var xpct = (Real)x/(Real)step;

								//interpolated height
								var actualPos = Vector3.Zero;
								GetPointAlign( fulldetailx, fulldetaily, Alignment.Align_X_Y, ref actualPos );
								Real interp_h = 0;
								// Determine which tri we're on 
								if ( ( xpct > ypct && !backwardTri ) || ( xpct > ( 1 - ypct ) && backwardTri ) )
								{
									// Solve for x/z
									interp_h = ( -t1.Normal.x*actualPos.x - t1.Normal.y*actualPos.y - t1.D )/t1.Normal.z;
								}
								else
								{
									// Second tri
									interp_h = ( -t2.Normal.x*actualPos.x - t2.Normal.y*actualPos.y - t2.D )/t2.Normal.z;
								}

								var actual_h = actualPos.z;
								var delta = interp_h - actual_h;

								// max(delta) is the worst case scenario at this LOD
								// compared to the original heightmap
								if ( delta == float.NaN )
								{
								}
								// tell the quadtree about this 
								QuadTree.NotifyDelta( (ushort)fulldetailx, (ushort)fulldetaily, (ushort)sourceLevel, delta );


								// If this vertex is being removed at this LOD, 
								// then save the height difference since that's the move
								// it will need to make. Vertices to be removed at this LOD
								// are halfway between the steps, but exclude those that
								// would have been eliminated at earlier levels
								int halfStep = step/2;
								if ( ( ( fulldetailx%step ) == halfStep && ( fulldetaily%halfStep ) == 0 ) ||
								     ( ( fulldetaily%step ) == halfStep && ( fulldetailx%halfStep ) == 0 ) )
								{
#if !AXIOM_SAFE_ONLY
									unsafe
#endif
									{
										// Save height difference 
										var pDest = GetDeltaData( fulldetailx, fulldetaily ).ToFloatPointer();
										pDest[ 0 ] = delta;
									}
								}
							} //x
						} //y
					}
				} //j
			} //targetlevel

			QuadTree.PostDeltaCalculation( clampedRect );

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
		[OgreVersion( 1, 7, 2 )]
		public void FinalizeHeightDeltas( Rectangle rect, bool cpuData )
		{
			var clampedRect = new Rectangle( rect );
			clampedRect.Left = Utility.Max( 0L, clampedRect.Left );
			clampedRect.Top = Utility.Max( 0L, clampedRect.Top );
			clampedRect.Right = Utility.Min( (long)mSize, clampedRect.Right );
			clampedRect.Bottom = Utility.Min( (long)mSize, clampedRect.Bottom );

			// min/max information
			QuadTree.FinaliseDeltaValues( clampedRect );
			// dekta vertex data
			QuadTree.UpdateVertexData( false, true, clampedRect, cpuData );
		}

		/// <summary>
		/// Gets the resolution of the entire terrain (down one edge) at a 
		///	given LOD level.
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public ushort GetResolutionAtLod( ushort lodLevel )
		{
			return (ushort)( ( ( mSize - 1 ) >> lodLevel ) + 1 );
		}

		[OgreVersion( 1, 7, 2 )]
		private void _preFindVisibleObjects( SceneManager source, IlluminationRenderStage irs, Viewport v )
		{
			//Early-out
			if ( !IsLoaded )
			{
				return;
			}

			// check deferred updates
			var currMillis = Root.Instance.Timer.Milliseconds;
			var elapsedMillis = currMillis - mLastMillis;
			if ( mCompositeMapUpdateCountdown > 0 && elapsedMillis > 0 )
			{
				if ( elapsedMillis > mCompositeMapUpdateCountdown )
				{
					mCompositeMapUpdateCountdown = 0;
				}
				else
				{
					mCompositeMapUpdateCountdown -= elapsedMillis;
				}

				if ( mCompositeMapUpdateCountdown == 0 )
				{
					UpdateCompositeMap();
				}
			}
			mLastMillis = currMillis;
			// only calculate LOD once per LOD camera, per frame, per viewport height
			var lodCamera = v.Camera.LodCamera;
			var frameNum = (ulong)Root.Instance.NextFrameNumber;
			var vpHeight = v.ActualHeight;
			if ( mLastLODCamera != lodCamera || frameNum != mLastLODFrame || mLastViewportHeight != vpHeight )
			{
				mLastLODCamera = lodCamera;
				mLastLODFrame = frameNum;
				mLastViewportHeight = vpHeight;
				CalculateCurrentLod( v );
			}
		}

		[OgreVersion( 1, 7, 2 )]
		private void _sceneManagerDestroyed( SceneManager source )
		{
			Unload();
			Unprepare();
			if ( source == SceneManager )
			{
				SceneManager = null;
			}
		}

		[OgreVersion( 1, 7, 2 )]
		protected void CalculateCurrentLod( Viewport vp )
		{
			if ( QuadTree != null )
			{
				// calculate error terms
				var cam = vp.Camera.LodCamera;

				// W. de Boer 2000 calculation
				// A = vp_near / abs(vp_top)
				// A = 1 / tan(fovy*0.5)    (== 1 for fovy=45*2)
				var A = 1.0f/Utility.Tan( cam.FieldOfView*0.5 );
				// T = 2 * maxPixelError / vertRes
				var maxPixelError = TerrainGlobalOptions.MaxPixelError*cam.InverseLodBias;
				var T = 2.0f*maxPixelError/(Real)vp.ActualHeight;

				// CFactor = A / T
				var cFactor = A/T;
				QuadTree.CalculateCurrentLod( cam, cFactor );
			}
		}

		/// <summary>
		/// Test for intersection of a given ray with the terrain. If the ray hits
		/// the terrain, the point of intersection is returned.
		/// </summary>
		/// <remarks>
		/// This can be called from any thread as long as no parallel write to the heightmap data occurs.
		/// </remarks>
		/// <param name="ray">The ray to test for intersection</param>
		/// <param name="cascadeToNeighbours">Whether the ray will be projected onto neighbours if
		/// no intersection is found</param>
		/// <param name="distanceLimit">The distance from the ray origin at which we will stop looking,
		/// 0 indicates no limit</param>
		/// <returns>A pair which contains whether the ray hit the terrain and, if so, where.</returns>
		[OgreVersion( 1, 7, 2 )]
		public KeyValuePair<bool, Vector3> RayIntersects( Ray ray, bool cascadeToNeighbours, Real distanceLimit )
		{
			KeyValuePair<bool, Vector3> Result;
			// first step: convert the ray to a local vertex space
			// we assume terrain to be in the x-z plane, with the [0,0] vertex
			// at origin and a plane distance of 1 between vertices.
			// This makes calculations easier.
			var rayOrigin = ray.Origin - Position;
			var rayDirection = ray.Direction;
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
			rayOrigin.x += mWorldSize/2;
			rayOrigin.z += mWorldSize/2;
			// scale down to vertex level
			rayOrigin.x /= mScale;
			rayOrigin.z /= mScale;
			rayDirection.x /= mScale;
			rayDirection.z /= mScale;
			rayDirection.Normalize();
			var localRay = new Ray( rayOrigin, rayDirection );

			// test if the ray actually hits the terrain's bounds
			var maxHeight = MaxHeight;
			var minHeight = MinHeight;

			var aabb = new AxisAlignedBox( new Vector3( 0, minHeight, 0 ), new Vector3( mSize, maxHeight, mSize ) );
			var aabbTest = localRay.Intersects( aabb );
			if ( !aabbTest.Hit )
			{
				if ( cascadeToNeighbours )
				{
					var neighbour = RaySelectNeighbour( ray, distanceLimit );
					if ( neighbour != null )
					{
						return neighbour.RayIntersects( ray, cascadeToNeighbours, distanceLimit );
					}
				}
				return new KeyValuePair<bool, Vector3>( false, new Vector3() );
			}
			// get intersection point and move inside
			var cur = localRay.GetPoint( aabbTest.Distance );

			// now check every quad the ray touches
			var quadX = Utility.Min( Utility.Max( (int)( cur.x ), 0 ), (int)mSize - 2 );
			var quadZ = Utility.Min( Utility.Max( (int)( cur.z ), 0 ), (int)mSize - 2 );
			var flipX = ( rayDirection.x < 0 ? 0 : 1 );
			var flipZ = ( rayDirection.z < 0 ? 0 : 1 );
			var xDir = ( rayDirection.x < 0 ? -1 : 1 );
			var zDir = ( rayDirection.z < 0 ? -1 : 1 );

			Result = new KeyValuePair<bool, Vector3>( true, Vector3.Zero );
			var dummyHighValue = (Real)mSize*10000;

			while ( cur.y >= ( minHeight - 1e-3 ) && cur.y <= ( maxHeight + 1e-3 ) )
			{
				if ( quadX < 0 || quadX >= (int)mSize - 1 || quadZ < 0 || quadZ >= (int)mSize - 1 )
				{
					break;
				}

				Result = CheckQuadIntersection( quadX, quadZ, localRay );
				if ( Result.Key )
				{
					break;
				}

				// determine next quad to test
				var xDist = Utility.RealEqual( rayDirection.x, 0.0f ) ? dummyHighValue : ( quadX - cur.x + flipX )/rayDirection.x;
				var zDist = Utility.RealEqual( rayDirection.z, 0.0f ) ? dummyHighValue : ( quadZ - cur.z + flipZ )/rayDirection.z;
				if ( xDist < zDist )
				{
					quadX += xDir;
					cur += rayDirection*xDist;
				}
				else
				{
					quadZ += zDir;
					cur += rayDirection*zDist;
				}
			}
			var resVec = Vector3.Zero;

			if ( Result.Key )
			{
				// transform the point of intersection back to world space
				resVec = Result.Value;
				resVec.x *= mScale;
				resVec.z *= mScale;
				resVec.x -= mWorldSize/2;
				resVec.z -= mWorldSize/2;
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
				var neighbour = RaySelectNeighbour( ray, distanceLimit );
				if ( neighbour != null )
				{
					Result = neighbour.RayIntersects( ray, cascadeToNeighbours, distanceLimit );
				}
			}
			return new KeyValuePair<bool, Vector3>( Result.Key, resVec );
		}

		/// <see cref="Terrain.RayIntersects(Ray, bool, Real)"/>
		public KeyValuePair<bool, Vector3> RayIntersects( Ray ray )
		{
			return RayIntersects( ray, false, 0 );
		}

		/// <see cref="Terrain.RayIntersects(Ray, bool, Real)"/>
		public KeyValuePair<bool, Vector3> RayIntersects( Ray ray, bool cascadeToNeighbours )
		{
			return RayIntersects( ray, cascadeToNeighbours, 0 );
		}

		/// <summary>
		/// Test a single quad of the terrain for ray intersection.
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		protected KeyValuePair<bool, Vector3> CheckQuadIntersection( int x, int z, Ray ray )
		{
			// build the two planes belonging to the quad's triangles
			Vector3 v1 = new Vector3( x, mHeightData[ z + mSize*x ], z ),
			        v2 = new Vector3( x + 1, mHeightData[ z + mSize*( x + 1 ) ], z ),
			        v3 = new Vector3( x, mHeightData[ ( z + 1 ) + mSize*x ], z + 1 ),
			        v4 = new Vector3( x + 1, mHeightData[ ( z + 1 ) + mSize*( x + 1 ) ], z + 1 );

			Plane p1 = new Plane(), p2 = new Plane();
			var oddRow = false;
			if ( z%2 != 0 )
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
			var planeInt = ray.Intersects( p1 );
			if ( planeInt.Hit )
			{
				var where = ray.GetPoint( planeInt.Distance );
				var rel = where - v1;
				if ( rel.x >= -0.01 && rel.x <= 1.01 && rel.z >= -0.01 && rel.z <= 1.01 // quad bounds
				     && ( ( rel.x >= rel.z && !oddRow ) || ( rel.x >= ( 1 - rel.z ) && oddRow ) ) ) // triangle bounds
				{
					return new KeyValuePair<bool, Vector3>( true, where );
				}
			}
			planeInt = ray.Intersects( p2 );
			if ( planeInt.Hit )
			{
				var where = ray.GetPoint( planeInt.Distance );
				var rel = where - v1;
				if ( rel.x >= -0.01 && rel.x <= 1.01 && rel.z >= -0.01 && rel.z <= 1.01 // quad bounds
				     && ( ( rel.x <= rel.z && !oddRow ) || ( rel.x <= ( 1 - rel.z ) && oddRow ) ) ) // triangle bounds
				{
					return new KeyValuePair<bool, Vector3>( true, where );
				}
			}

			return new KeyValuePair<bool, Vector3>( false, Vector3.Zero );
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
		public void ReplaceLayer( byte index, bool keepBlends, Real worldSize, List<string> textureNames )
		{
			if ( LayerCount > 0 )
			{
				if ( index >= LayerCount )
				{
					index = (byte)( LayerCount - 1 );
				}

				var i = mLayers[ index ];

				if ( textureNames != null )
				{
					i.TextureNames = new List<string>( textureNames );
				}

				// use utility method to update UV scaling
				SetLayerWorldSize( index, worldSize );

				// Delete the blend map if its not the base
				if ( !keepBlends && index > 0 )
				{
					if ( mLayerBlendMapList[ index - 1 ] != null )
					{
						mLayerBlendMapList[ index - 1 ] = null;
					}

					// Reset the layer to black
					var layerPair = GetLayerBlendTextureIndex( index );
					clearGPUBlendChannel( layerPair.Key, layerPair.Value );
				}

				mMaterialDirty = true;
				mMaterialParamsDirty = true;
				IsModified = true;
			}
		}

		/// <summary>
		/// Add a new layer to this terrain.
		/// </summary>
		/// <param name="worldSize">The size of the texture in this layer in world units. Default
		/// to zero to use the default</param>
		/// <param name="textureNames">A list of textures to assign to the samplers in this
		///	layer. Leave blank to provide these later. </param>
		[OgreVersion( 1, 7, 2 )]
		public void AddLayer( Real worldSize, List<string> textureNames )
		{
			AddLayer( LayerCount, worldSize, textureNames );
		}

		/// <see cref="Terrain.AddLayer(Real, List<string>)"/>
		public void AddLayer()
		{
			AddLayer( 0, null );
		}

		/// <see cref="Terrain.AddLayer(Real, List<string>)"/>
		public void AddLayer( Real worldSize )
		{
			AddLayer( worldSize, null );
		}

		/// <summary>
		/// Add a new layer to this terrain at a specific index.
		/// </summary>
		/// <param name="index">The index at which to insert this layer (existing layers are shifted forwards)</param>
		/// <param name="worldSize">The size of the texture in this layer in world units. Default
		/// to zero to use the default</param>
		/// <param name="textureNames">A list of textures to assign to the samplers in this
		/// layer. Leave blank to provide these later.</param>
		[OgreVersion( 1, 7, 2 )]
		public void AddLayer( byte index, Real worldSize, List<string> textureNames )
		{
			if ( worldSize == 0 )
			{
				worldSize = TerrainGlobalOptions.DefaultLayerTextureWorldSize;
			}

			var blendIndex = (byte)Utility.Max( index - 1, 0 );
			if ( index >= LayerCount )
			{
				mLayers.Add( new LayerInstance() );
				index = (byte)( LayerCount - 1 );
			}
			else
			{
				mLayers.Insert( index, new LayerInstance() );
				mLayerUVMultiplier.Insert( index, 0.0f );
				mLayerBlendMapList.Insert( blendIndex, null );
			}
			if ( textureNames != null )
			{
				LayerInstance inst = mLayers[ index ];
				inst.TextureNames = new List<string>( textureNames );
			}
			// use utility method to update UV scaling
			SetLayerWorldSize( index, worldSize );
			CheckLayers( true );

			// Is this an insert into the middle of the layer list?
			if ( index < LayerCount - 1 )
			{
				// Shift all GPU texture channels up one
				shiftUpGPUBlendChannels( blendIndex );

				// All blend maps above this layer index will need to be recreated since their buffers/channels have changed
				deleteBlendMaps( index );
			}

			mMaterialDirty = true;
			mMaterialParamsDirty = true;
			IsModified = true;
		}

		/// <see cref="Terrain.AddLayer(byte, Real, List<string>)"/>
		public void AddLayer( byte index )
		{
			AddLayer( index, 0, null );
		}

		/// <see cref="Terrain.AddLayer(byte, Real, List<string>)"/>
		public void AddLayer( byte index, Real worldSize )
		{
			AddLayer( index, worldSize, null );
		}

		/// <summary>
		/// Remove a layer from the terrain.
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public void RemoveLayer( byte index )
		{
			if ( index < mLayers.Count )
			{
				var blendIndex = (byte)Utility.Max( index - 1, 0 );

				// Shift all GPU texture channels down one
				shiftDownGPUBlendChannels( blendIndex );

				mLayers.RemoveAt( index );
				mLayerUVMultiplier.RemoveAt( index );

				if ( mLayerBlendMapList.Count > 0 )
				{
					// If they removed the base OR the first layer, we need to erase the first blend map
					mLayerBlendMapList.RemoveAt( blendIndex );

					// Check to see if a GPU textures can be released
					CheckLayers( true );

					// All blend maps for layers above the erased will need to be recreated since their buffers/channels have changed
					deleteBlendMaps( blendIndex );
				}

				mMaterialDirty = true;
				mMaterialParamsDirty = true;
				IsModified = true;
			}
		}

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
		[OgreVersion( 1, 7, 2 )]
		public TerrainLayerBlendMap GetLayerBlendMap( byte layerIndex )
		{
			if ( layerIndex == 0 || layerIndex - 1 >= (byte)mLayerBlendMapList.Count )
			{
				throw new AxiomException( "Invalid layer index. Terrain.GetLayerBlendMap" );
			}

			var idx = (byte)( layerIndex - 1 );
			if ( mLayerBlendMapList[ idx ] == null )
			{
				if ( mBlendTextureList.Count < (int)( idx/4 ) )
				{
					CheckLayers( true );
				}

				var tex = mBlendTextureList[ idx/4 ];
				mLayerBlendMapList[ idx ] = new TerrainLayerBlendMap( this, layerIndex, tex.GetBuffer() );
			}

			return mLayerBlendMapList[ idx ];
		}

		/// <summary>
		/// Get the number of blend textures needed for a given number of layers
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public byte GetBlendTextureCount( byte numLayers )
		{
			return (byte)( ( ( numLayers - 1 )/4 ) + 1 );
		}

		/// <summary>
		/// Get the number of blend textures in use
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public byte GetBlendTextureCount()
		{
			return (byte)mBlendTextureList.Count;
		}

		[OgreVersion( 1, 7, 2 )]
		protected PixelFormat GetBlendTextureFormat( byte textureIndex, byte numLayers )
		{
			// Always create RGBA; no point trying to create RGB since all cards pad to 32-bit (XRGB)
			// and it makes it harder to expand layer count dynamically if format has to change
			return PixelFormat.BYTE_RGBA;
		}

		/// <summary>
		/// Shift/slide all GPU blend texture channels > index up one slot.  Blend data may shift into the next texture
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		protected void shiftUpGPUBlendChannels( byte index )
		{
			// checkLayers() has been called to make sure the blend textures have been created
			System.Diagnostics.Debug.Assert( mBlendTextureList.Count == GetBlendTextureCount( LayerCount ) );

			// Shift all blend channels > index UP one slot, possibly into the next texture
			// Example:  index = 2
			//      Before: [1 2 3 4] [5]
			//      After:  [1 2 0 3] [4 5]

			var layerIndex = (byte)( index + 1 );
			for ( var i = (byte)( LayerCount - 1 ); i > layerIndex; --i )
			{
				var destPair = GetLayerBlendTextureIndex( i );
				var srcPair = GetLayerBlendTextureIndex( (byte)( i - 1 ) );

				copyBlendTextureChannel( srcPair.Key, srcPair.Value, destPair.Key, destPair.Value );
			}

			// Reset the layer to black
			var layerPair = GetLayerBlendTextureIndex( layerIndex );
			clearGPUBlendChannel( layerPair.Key, layerPair.Value );
		}

		/// <summary>
		/// Shift/slide all GPU blend texture channels > index down one slot.  Blend data may shift into the previous texture
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		protected void shiftDownGPUBlendChannels( byte index )
		{
			// checkLayers() has been called to make sure the blend textures have been created
			System.Diagnostics.Debug.Assert( mBlendTextureList.Count == GetBlendTextureCount( LayerCount ) );

			// Shift all blend channels above layerIndex DOWN one slot, possibly into the previous texture
			// Example:  index = 2
			//      Before: [1 2 3 4] [5]
			//      After:  [1 2 4 5] [0]

			var layerIndex = (byte)( index + 1 );
			for ( var i = layerIndex; i < LayerCount - 1; ++i )
			{
				var destPair = GetLayerBlendTextureIndex( i );
				var srcPair = GetLayerBlendTextureIndex( (byte)( i + 1 ) );

				copyBlendTextureChannel( srcPair.Key, srcPair.Value, destPair.Key, destPair.Value );
			}

			// Reset the layer to black
			if ( LayerCount > 1 )
			{
				var layerPair = GetLayerBlendTextureIndex( (byte)( LayerCount - 1 ) );
				clearGPUBlendChannel( layerPair.Key, layerPair.Value );
			}
		}

		/// <summary>
		/// Copy a GPU blend channel from one source to another.  Source and Dest are not required to be in the same texture
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		protected void copyBlendTextureChannel( byte srcIndex, byte srcChannel, byte destIndex, byte destChannel )
		{
			var srcBuffer = GetLayerBlendTexture( srcIndex ).GetBuffer();
			var destBuffer = GetLayerBlendTexture( destIndex ).GetBuffer();

			var box = new BasicBox( 0, 0, destBuffer.Width, destBuffer.Height );

			var pDestBase = destBuffer.Lock( box, BufferLocking.Normal ).Data;
			var rgbaShift = PixelUtil.GetBitShifts( destBuffer.Format );
			var pDest = pDestBase + rgbaShift[ destChannel ]/8;
			var destInc = PixelUtil.GetNumElemBytes( destBuffer.Format );

			int srcInc;
			BufferBase pSrc;

			if ( destBuffer == srcBuffer )
			{
				pSrc = pDestBase + rgbaShift[ srcChannel ]/8;
				srcInc = destInc;
			}
			else
			{
				pSrc = srcBuffer.Lock( box, BufferLocking.ReadOnly ).Data;
				rgbaShift = PixelUtil.GetBitShifts( srcBuffer.Format );
				pSrc += rgbaShift[ srcChannel ]/8;
				srcInc = PixelUtil.GetNumElemBytes( srcBuffer.Format );
			}

			for ( var y = box.Top; y < box.Bottom; ++y )
			{
				for ( var x = box.Left; x < box.Right; ++x )
				{
					pDest = pSrc;
					pSrc += srcInc;
					pDest += destInc;
				}
			}

			destBuffer.Unlock();
			pSrc.Dispose();
			if ( destBuffer != srcBuffer )
			{
				srcBuffer.Unlock();
			}
		}

		/// <summary>
		/// Reset a blend channel back to full black
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		protected void clearGPUBlendChannel( byte index, byte channel )
		{
			var buffer = GetLayerBlendTexture( index ).GetBuffer();
			var box = new BasicBox( 0, 0, buffer.Width, buffer.Height );

			var pData = buffer.Lock( box, BufferLocking.Normal ).Data;
			var rgbaShift = PixelUtil.GetBitShifts( buffer.Format );
			pData += rgbaShift[ channel ]/8;
			var inc = PixelUtil.GetNumElemBytes( buffer.Format );

			for ( var y = box.Top; y < box.Bottom; ++y )
			{
				for ( var x = box.Left; x < box.Right; ++x )
				{
					pData = null;
					pData += inc;
				}
			}
			buffer.Unlock();
		}

		[OgreVersion( 1, 7, 2 )]
		protected void CreateGPUBlendTextures()
		{
			// Create enough RGBA/RGB textures to cope with blend layers
			var numTex = GetBlendTextureCount( LayerCount );
			//delete extras
			var tmgr = TextureManager.Instance;
			if ( tmgr == null )
			{
				return;
			}

			while ( mBlendTextureList.Count > numTex )
			{
				tmgr.Remove( mBlendTextureList[ mBlendTextureList.Count - 1 ].Handle );
				mBlendTextureList.Remove( mBlendTextureList[ mBlendTextureList.Count - 1 ] );
			}

			var currentTex = (byte)mBlendTextureList.Count;
			mBlendTextureList.Capacity = numTex;
			//create new textures
			for ( var i = currentTex; i < numTex; ++i )
			{
				var fmt = GetBlendTextureFormat( i, LayerCount );
				// Use TU_STATIC because although we will update this, we won't do it every frame
				// in normal circumstances, so we don't want TU_DYNAMIC. Also we will 
				// read it (if we've cleared local temp areas) so no WRITE_ONLY
				mBlendTextureList.Add(
					(Texture)
					tmgr.CreateManual( msBlendTextureGenerator.GetNextUniqueName(), DerivedResourceGroup, TextureType.TwoD,
					                   mLayerBlendMapSize, mLayerBlendMapSize, 1, 0, fmt, TextureUsage.Static, null ) );

				mLayerBlendSizeActual = (ushort)mBlendTextureList[ i ].Width;
				if ( mCpuBlendMapStorage.Count > i )
				{
					//load blend data
					using ( var data = BufferBase.Wrap( mCpuBlendMapStorage[ i ] ) )
					{
						var src = new PixelBox( mLayerBlendMapSize, mLayerBlendMapSize, 1, fmt, data );
						mBlendTextureList[ i ].GetBuffer().BlitFromMemory( src );
					}
				}
				else
				{
					//initialse black
					var box = new BasicBox( 0, 0, mLayerBlendMapSize, mLayerBlendMapSize );
					var buf = mBlendTextureList[ i ].GetBuffer();
					var pInit = buf.Lock( box, BufferLocking.Discard ).Data;
					var aZero = new byte[PixelUtil.GetNumElemBytes( fmt )*mLayerBlendMapSize*mLayerBlendMapSize];
					using ( var src = BufferBase.Wrap( aZero ) )
					{
						Memory.Copy( src, pInit, aZero.Length );
					}
					buf.Unlock();
				}
			} //i

			mCpuBlendMapStorage.Clear();
		}

		[OgreVersion( 1, 7, 2 )]
		protected void CreateLayerBlendMaps()
		{
			// delete extra blend layers (affects GPU)
			while ( mLayerBlendMapList.Count > mLayers.Count - 1 )
			{
				mLayerBlendMapList.RemoveAt( mLayerBlendMapList.Count - 1 );
			}

			// resize up (initialises to 0, populate as necessary)
			if ( mLayers.Count > 1 )
			{
				mLayerBlendMapList.Capacity = mLayers.Count - 1;
				for ( var i = 0; i < mLayers.Count - 1; i++ )
				{
					mLayerBlendMapList.Add( null );
				}
			}
		}

		[OgreVersion( 1, 7, 2 )]
		protected void CreateOrDestroyGPUNormalMap()
		{
			if ( mNormalMapRequired && TerrainNormalMap == null )
			{
				//create 
				TerrainNormalMap = TextureManager.Instance.CreateManual( mMaterialName + "/nm", DerivedResourceGroup,
				                                                         TextureType.TwoD, mSize, mSize, 1, 0, PixelFormat.BYTE_RGB,
				                                                         TextureUsage.Static, null );

				//upload loaded normal data if present
				if ( mCpuTerrainNormalMap != null )
				{
					TerrainNormalMap.GetBuffer().BlitFromMemory( mCpuTerrainNormalMap );
					mCpuTerrainNormalMap.Data = null;
					mCpuTerrainNormalMap = null;
				}
			}
			else if ( !mNormalMapRequired && TerrainNormalMap != null )
			{
				//destroy 
				TextureManager.Instance.Remove( TerrainNormalMap.Handle );
				TerrainNormalMap = null;
			}
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
		[OgreVersion( 1, 7, 2 )]
		public void FreeTemporaryResources()
		{
			// cpu blend maps
			mCpuBlendMapStorage.Clear();

			// Editable structures for blend layers (not needed at runtime,  only blend textures are)
			deleteBlendMaps( 0 );
		}

		/// <summary>
		///  Delete blend maps for all layers >= lowIndex
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		protected void deleteBlendMaps( byte lowIndex )
		{
			mLayerBlendMapList.RemoveRange( lowIndex, mLayerBlendMapList.Count - lowIndex );
		}

		/// <summary>
		/// Get a blend texture with a given index.
		/// </summary>
		/// <param name="index">The blend texture index (note: not layer index; derive
		/// the texture index from getLayerBlendTextureIndex)</param>
		[OgreVersion( 1, 7, 2 )]
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
		[OgreVersion( 1, 7, 2 )]
		public KeyValuePair<byte, byte> GetLayerBlendTextureIndex( byte layerIndex )
		{
			System.Diagnostics.Debug.Assert( layerIndex > 0 && layerIndex < mLayers.Count, "Given index is out of Bound!" );
			var idx = (byte)( layerIndex - 1 );
			return new KeyValuePair<byte, byte>( (byte)( idx/4 ), (byte)( idx%4 ) );
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
#if NET_40
		internal void SetLightMapRequired( bool lightMap, bool shadowsOnly = false )
#else
		internal void SetLightMapRequired( bool lightMap, bool shadowsOnly )
#endif
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

#if !NET_40
		/// <see cref="Terrain.SetLightMapRequired(bool, bool)"/>
		internal void SetLightMapRequired( bool lightMap )
		{
			SetLightMapRequired( lightMap, false );
		}
#endif

		/// <summary>
		/// Utility method, get the first LOD Level at which this vertex is no longer included
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public ushort GetLODLevelWhenVertexEliminated( long x, long y )
		{
			// gets eliminated by either row or column first
			return Utility.Min( GetLODLevelWhenVertexEliminated( x ), GetLODLevelWhenVertexEliminated( y ) );
		}

		/// <summary>
		/// Utility method, get the first LOD Level at which this vertex is no longer included
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public ushort GetLODLevelWhenVertexEliminated( long rowOrColulmn )
		{
			// LOD levels bisect the domain.
			// start at the lowest detail
			var currentElim = (ushort)( ( mSize - 1 )/( mMinBatchSize - 1 ) );
			// start at a non-exitant LOD index, this applies to the min batch vertices
			// which are never eliminated
			var currentLod = NumLodLevels;

			while ( rowOrColulmn%currentElim != 0 )
			{
				// not on this boundary, look finer
				currentElim = (ushort)( currentElim/2 );
				--currentLod;
				// This will always terminate since (anything % 1 == 0)
			}

			return currentLod;
		}

		/// <summary>
		/// Calculate (or recalculate) the normals on the terrain
		/// </summary>
		/// <param name="rect">Rectangle describing the area of heights that were changed</param>
		/// <param name="outFinalRect"> Output rectangle describing the area updated</param>
		/// <returns>PixelBox full of normals (caller responsible for deletion)</returns>
		[OgreVersion( 1, 7, 2 )]
		public PixelBox CalculateNormals( Rectangle rect, ref Rectangle outFinalRect )
		{
			// Widen the rectangle by 1 element in all directions since height
			// changes affect neighbours normals
			var widenedRect = new Rectangle( Utility.Max( 0L, rect.Left - 1L ), Utility.Max( 0L, rect.Top - 1L ),
			                                 Utility.Min( (long)mSize, rect.Right + 1L ),
			                                 Utility.Min( (long)mSize, rect.Bottom + 1L ) );

			// allocate memory for RGB
			var pData = new byte[widenedRect.Width*widenedRect.Height*3];
			var pixbox = new PixelBox( (int)widenedRect.Width, (int)widenedRect.Height, 1, PixelFormat.BYTE_RGB,
			                           BufferBase.Wrap( pData ) );

			// Evaluate normal like this
			//  3---2---1
			//  | \ | / |
			//	4---P---0
			//  | / | \ |
			//	5---6---7

			var plane = new Plane();
			for ( var y = widenedRect.Top; y < widenedRect.Bottom; ++y )
			{
				for ( var x = widenedRect.Left; x < widenedRect.Right; ++x )
				{
					var cumulativeNormal = Vector3.Zero;

					// Build points to sample
					var centrePoint = Vector3.Zero;
					var adjacentPoints = new Vector3[8];
					GetPointFromSelfOrNeighbour( x, y, ref centrePoint );
					GetPointFromSelfOrNeighbour( x + 1, y, ref adjacentPoints[ 0 ] );
					GetPointFromSelfOrNeighbour( x + 1, y + 1, ref adjacentPoints[ 1 ] );
					GetPointFromSelfOrNeighbour( x, y + 1, ref adjacentPoints[ 2 ] );
					GetPointFromSelfOrNeighbour( x - 1, y + 1, ref adjacentPoints[ 3 ] );
					GetPointFromSelfOrNeighbour( x - 1, y, ref adjacentPoints[ 4 ] );
					GetPointFromSelfOrNeighbour( x - 1, y - 1, ref adjacentPoints[ 5 ] );
					GetPointFromSelfOrNeighbour( x, y - 1, ref adjacentPoints[ 6 ] );
					GetPointFromSelfOrNeighbour( x + 1, y - 1, ref adjacentPoints[ 7 ] );

					for ( var i = 0; i < 8; ++i )
					{
						plane.Redefine( centrePoint, adjacentPoints[ i ], adjacentPoints[ ( i + 1 )%8 ] );
						cumulativeNormal += plane.Normal;
					}

					// normalise & store normal
					cumulativeNormal.Normalize();

					// encode as RGB, object space
					// invert the Y to deal with image space
					var storeX = x - widenedRect.Left;
					var storeY = widenedRect.Bottom - y - 1;

					var pStore = ( ( storeY*widenedRect.Width ) + storeX )*3;
					pData[ pStore++ ] = (byte)( ( cumulativeNormal.x + 1.0f )*0.5f*255.0f );
					pData[ pStore++ ] = (byte)( ( cumulativeNormal.y + 1.0f )*0.5f*255.0f );
					pData[ pStore++ ] = (byte)( ( cumulativeNormal.z + 1.0f )*0.5f*255.0f );
				} //x
			} //y

			outFinalRect = widenedRect;
			return pixbox;
		}

		/// <summary>
		/// Finalise the normals. 
		/// Calculated normals are kept in a separate calculation area to make
		/// them safe to perform in a background thread. This call promotes those
		/// calculations to the runtime values, and must be called in the main thread.
		/// </summary>
		/// <param name="rect">Rectangle describing the area to finalize </param>
		/// <param name="normalsBox">PixelBox full of normals</param>
		[OgreVersion( 1, 7, 2 )]
		public void FinalizeNormals( Rectangle rect, PixelBox normalsBox )
		{
			CreateOrDestroyGPUNormalMap();
			// deal with race condition where nm has been disabled while we were working!
			if ( TerrainNormalMap != null )
			{
				// blit the normals into the texture
				if ( rect.Left == 0 && rect.Top == 0 && rect.Bottom == mSize && rect.Right == mSize )
				{
					TerrainNormalMap.GetBuffer().BlitFromMemory( normalsBox );
				}
				else
				{
					// content of normalsBox is already inverted in Y, but rect is still 
					// in terrain space for dealing with sub-rect, so invert
					var dstBox = new BasicBox();
					dstBox.Left = (int)rect.Left;
					dstBox.Right = (int)rect.Right;
					dstBox.Top = (int)( mSize - rect.Bottom );
					dstBox.Bottom = (int)( mSize - rect.Top );
					TerrainNormalMap.GetBuffer().BlitFromMemory( normalsBox, dstBox );
				}
			}

			// delete memory
			if ( normalsBox.Data != null )
			{
				normalsBox.Data.Dispose();
				normalsBox.Data = null;
			}
			normalsBox = null;
		}

		/// <summary>
		/// Widen a rectangular area of terrain to take into account an extrusion vector.
		/// </summary>
		/// <param name="vec"> A vector in world space</param>
		/// <param name="inRec">Input rectangle</param>
		/// <param name="outRect">Output rectangle</param>
		[OgreVersion( 1, 7, 2 )]
		public void WidenRectByVector( Vector3 vec, Rectangle inRect, ref Rectangle outRect )
		{
			WidenRectByVector( vec, inRect, MinHeight, MaxHeight, ref outRect );
		}

		/// <summary>
		/// Widen a rectangular area of terrain to take into account an extrusion vector, 
		/// but specify the min / max heights to extrude manually.
		/// </summary>
		/// <param name="vec">A vector in world space</param>
		/// <param name="inRect">Input rectangle</param>
		/// <param name="minHeight">The extents of the height to extrude</param>
		/// <param name="maxHeight">The extents of the height to extrude</param>
		/// <param name="outRect">Output rectangle</param>
		[OgreVersion( 1, 7, 2 )]
		public void WidenRectByVector( Vector3 vec, Rectangle inRect, Real minHeight, Real maxHeight, ref Rectangle outRect )
		{
			outRect = inRect;

			var p = new Plane();
			switch ( Alignment )
			{
				case Alignment.Align_X_Y:
					p.Redefine( Vector3.UnitZ, new Vector3( 0, 0, vec.z < 0.0f ? minHeight : maxHeight ) );
					break;

				case Alignment.Align_X_Z:
					p.Redefine( Vector3.UnitY, new Vector3( 0, vec.y < 0.0f ? minHeight : maxHeight, 0 ) );
					break;

				case Alignment.Align_Y_Z:
					p.Redefine( Vector3.UnitX, new Vector3( vec.x < 0.0f ? minHeight : maxHeight, 0, 0 ) );
					break;
			}
			var verticalVal = vec.Dot( p.Normal );

			if ( Utility.RealEqual( verticalVal, 0.0f ) )
			{
				return;
			}

			var corners = new Vector3[4];
			var startHeight = verticalVal < 0.0f ? maxHeight : minHeight;
			GetPoint( inRect.Left, inRect.Top, startHeight, ref corners[ 0 ] );
			GetPoint( inRect.Right - 1, inRect.Top, startHeight, ref corners[ 1 ] );
			GetPoint( inRect.Left, inRect.Bottom - 1, startHeight, ref corners[ 2 ] );
			GetPoint( inRect.Right - 1, inRect.Bottom - 1, startHeight, ref corners[ 3 ] );

			for ( int i = 0; i < 4; ++i )
			{
				var ray = new Ray( corners[ i ] + mPos, vec );
				var rayHit = ray.Intersects( p );
				if ( rayHit.Hit )
				{
					var pt = ray.GetPoint( rayHit.Distance );
					// convert back to terrain point
					var terrainHitPos = Vector3.Zero;
					GetTerrainPosition( pt, ref terrainHitPos );
					// build rectangle which has rounded down & rounded up values
					// remember right & bottom are exclusive
					var mergeRect = new Rectangle( (long)terrainHitPos.x*( mSize - 1 ), (long)terrainHitPos.y*( mSize - 1 ),
					                               (long)( terrainHitPos.x*(float)( mSize - 1 ) + 0.5f ) + 1,
					                               (long)( terrainHitPos.y*(float)( mSize - 1 ) + 0.5f ) + 1 );
					outRect.Merge( mergeRect );
				}
			}
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
		[OgreVersion( 1, 7, 2 )]
		public PixelBox CalculateLightMap( Rectangle rect, Rectangle extraTargetRect, ref Rectangle outFinalRect )
		{
			// as well as calculating the lighting changes for the area that is
			// dirty, we also need to calculate the effect on casting shadow on
			// other areas. To do this, we project the dirt rect by the light direction
			// onto the minimum height

			var lightVec = TerrainGlobalOptions.LightMapDirection;
			var widenedRect = new Rectangle();
			WidenRectByVector( lightVec, rect, ref widenedRect );

			//merge in the extra area (e.g. from neighbours)
			widenedRect.Merge( extraTargetRect );

			// widenedRect now contains terrain point space version of the area we
			// need to calculate. However, we need to calculate in lightmap image space
			var terrainToLightmapScale = (float)mLightmapSizeActual/(float)mSize;
			widenedRect.Left = (long)( widenedRect.Left*terrainToLightmapScale );
			widenedRect.Right = (long)( widenedRect.Right*terrainToLightmapScale );
			widenedRect.Top = (long)( widenedRect.Top*terrainToLightmapScale );
			widenedRect.Bottom = (long)( widenedRect.Bottom*terrainToLightmapScale );

			//clamp
			widenedRect.Left = Utility.Max( 0L, widenedRect.Left );
			widenedRect.Top = Utility.Max( 0L, widenedRect.Top );
			widenedRect.Right = Utility.Min( (long)mLightmapSizeActual, widenedRect.Right );
			widenedRect.Bottom = Utility.Min( (long)mLightmapSizeActual, widenedRect.Bottom );

			outFinalRect = widenedRect;

			// allocate memory (L8)
			var pData = new byte[widenedRect.Width*widenedRect.Height];
			var pDataPtr = BufferBase.Wrap( pData );
			var pixbox = new PixelBox( (int)widenedRect.Width, (int)widenedRect.Height, 1, PixelFormat.L8, pDataPtr );

			var heightPad = ( MaxHeight - MinHeight )*1.0e-3f;

			for ( var y = widenedRect.Top; y < widenedRect.Bottom; ++y )
			{
				for ( var x = widenedRect.Left; x < widenedRect.Right; ++x )
				{
					var litVal = 1.0f;

					// convert to terrain space (not points, allow this to go between points)
					var Tx = (float)x/(float)( mLightmapSizeActual - 1 );
					var Ty = (float)y/(float)( mLightmapSizeActual - 1 );

					// get world space point
					// add a little height padding to stop shadowing self
					var wpos = Vector3.Zero;
					GetPosition( Tx, Ty, GetHeightAtTerrainPosition( Tx, Ty ) + heightPad, ref wpos );
					wpos += Position;
					// build ray, cast backwards along light direction
					var ray = new Ray( wpos, -lightVec );

					// Cascade into neighbours when casting, but don't travel further
					// than world size
					var rayHit = RayIntersects( ray, true, mWorldSize );

					if ( rayHit.Key )
					{
						litVal = 0.0f;
					}

					// encode as L8
					// invert the Y to deal with image space
					var storeX = x - widenedRect.Left;
					var storeY = widenedRect.Bottom - y - 1;

					using ( var wrap = pDataPtr + ( ( storeY*widenedRect.Width ) + storeX ) )
					{
						var pStore = (ITypePointer<byte>)wrap;
						pStore[ 0 ] = (byte)( litVal*255.0 );
					}
				}
			}
			pDataPtr.Dispose();
			return pixbox;
		}

		/// <summary>
		/// Finalise the lightmap. 
		/// Calculating lightmaps is kept in a separate calculation area to make
		/// it safe to perform in a background thread. This call promotes those
		/// calculations to the runtime values, and must be called in the main thread.
		/// </summary>
		/// <param name="rect">Rectangle describing the area to finalize </param>
		/// <param name="lightmapBox">PixelBox full of normals</param>
		[OgreVersion( 1, 7, 2 )]
		public void FinalizeLightMap( Rectangle rect, PixelBox lightmapBox )
		{
			CreateOrDestroyGPULightmap();
			// deal with race condition where lm has been disabled while we were working!
			if ( LightMap != null )
			{
				// blit the normals into the texture
				if ( rect.Left == 0 && rect.Top == 0 && rect.Bottom == mLightmapSizeActual && rect.Right == mLightmapSizeActual )
				{
					LightMap.GetBuffer().BlitFromMemory( lightmapBox );
				}
				else
				{
					// content of PixelBox is already inverted in Y, but rect is still 
					// in terrain space for dealing with sub-rect, so invert
					var dstBox = new BasicBox();
					dstBox.Left = (int)rect.Left;
					dstBox.Right = (int)rect.Right;
					dstBox.Top = (int)( mLightmapSizeActual - rect.Bottom );
					dstBox.Bottom = (int)( mLightmapSizeActual - rect.Top );
					LightMap.GetBuffer().BlitFromMemory( lightmapBox, dstBox );
				}
			}

			// delete memory
			if ( lightmapBox.Data != null )
			{
				lightmapBox.Data.Dispose();
				lightmapBox.Data = null;
			}
			lightmapBox = null;
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
				IsModified = true;
				CreateOrDestroyGPUCompositeMap();
				if ( mCompositeMapDirtyRectLightmapUpdate &&
				     ( mCompositeMapDirtyRect.Width < mSize || mCompositeMapDirtyRect.Height < mSize ) )
				{
					// widen the dirty rectangle since lighting makes it wider
					var widenedRect = new Rectangle();
					WidenRectByVector( TerrainGlobalOptions.LightMapDirection, mCompositeMapDirtyRect, ref widenedRect );
					// clamp
					widenedRect.Left = Utility.Max( widenedRect.Left, 0L );
					widenedRect.Top = Utility.Max( widenedRect.Top, 0L );
					widenedRect.Right = Utility.Min( widenedRect.Right, (long)mSize );
					widenedRect.Bottom = Utility.Min( widenedRect.Bottom, (long)mSize );
					mMaterialGenerator.UpdateCompositeMap( this, widenedRect );
				}
				else
				{
					mMaterialGenerator.UpdateCompositeMap( this, mCompositeMapDirtyRect );
				}

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
		[OgreVersion( 1, 7, 2 )]
		public void UpdateCompositeMapWithDelay( Real delay )
		{
			mCompositeMapUpdateCountdown = (long)( delay*1000 );
		}

		/// <see cref="Terrain.UpdateCompositeMapWithDelay"/>
		public void UpdateCompositeMapWithDelay()
		{
			UpdateCompositeMapWithDelay( 2 );
		}

		/// <summary>
		///  Get the index of the blend texture that a given layer uses.
		/// </summary>
		/// <param name="layerIndex">The layer index, must be >= 1 and less than the number
		///	of layers</param>
		/// <returns>The index of the shared blend texture</returns>
		[OgreVersion( 1, 7, 2 )]
		public byte GetBlendTextureIndex( byte layerIndex )
		{
			if ( layerIndex == 0 || layerIndex - 1 >= (byte)mLayerBlendMapList.Count )
			{
				throw new AxiomException( "Invalid layer index, Terrain.GetBlendTextureIndex" );
			}

			return (byte)( layerIndex - 1%4 );
		}

		/// <summary>
		/// Get the name of the packed blend texture at a specific index.
		/// </summary>
		/// <param name="textureIndex">This is the blend texture index, not the layer index
		///	(multiple layers will share a blend texture)</param>
		[OgreVersion( 1, 7, 2 )]
		public string GetBlendTextureName( byte textureIndex )
		{
			if ( textureIndex >= (byte)mBlendTextureList.Count )
			{
				throw new AxiomException( "Invalid texture index, Terrain.GetBlendTextureName" );
			}

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
		/// <param name="size">the resolution of the color map. A value of zero means 'no change'
		///	and the default is set in TerrainGlobalOptions.</param>
		[OgreVersion( 1, 7, 2 )]
#if NET_40
		public void SetGlobalColorMapEnabled( bool enabled, ushort sz = 0 )
#else
		public void SetGlobalColorMapEnabled( bool enabled, ushort sz )
#endif
		{
			if ( sz == 0 )
			{
				sz = TerrainGlobalOptions.DefaultGlobalColorMapSize;
			}

			if ( enabled != IsGlobalColorMapEnabled || ( enabled && GlobalColorMapSize != sz ) )
			{
				IsGlobalColorMapEnabled = enabled;
				GlobalColorMapSize = sz;

				CreateOrDestroyGPUColorMap();

				mMaterialDirty = true;
				mMaterialParamsDirty = true;
				IsModified = true;
			}
		}

#if !NET_40
		/// <see cref="Terrain.SetGlobalColorMapEnabled(bool, ushort)"/>
		public void SetGlobalColorMapEnabled( bool enabled )
		{
			SetGlobalColorMapEnabled( enabled, 0 );
		}
#endif

		[OgreVersion( 1, 7, 2 )]
		protected void CreateOrDestroyGPUColorMap()
		{
			if ( IsGlobalColorMapEnabled && GlobalColorMap == null )
			{
#warning Check MIP_DEFAULT
				// create
				GlobalColorMap = TextureManager.Instance.CreateManual( mMaterialName + "/cm", DerivedResourceGroup, TextureType.TwoD,
				                                                       GlobalColorMapSize, GlobalColorMapSize, 1,
				                                                       PixelFormat.BYTE_RGB, TextureUsage.Static );

				if ( mCpuColorMapStorage != null )
				{
					// Load cached data
					using ( var data = BufferBase.Wrap( mCpuColorMapStorage ) )
					{
						var src = new PixelBox( (int)GlobalColorMapSize, (int)GlobalColorMapSize, 1, PixelFormat.BYTE_RGB, data );
						GlobalColorMap.GetBuffer().BlitFromMemory( src );
					}
					// release CPU copy, don't need it anymore
					mCpuColorMapStorage = null;
				}
			}
			else if ( !IsGlobalColorMapEnabled && GlobalColorMap != null )
			{
				// destroy
				TextureManager.Instance.Remove( GlobalColorMap.Handle );
				GlobalColorMap = null;
			}
		}

		[OgreVersion( 1, 7, 2 )]
		protected void CreateOrDestroyGPULightmap()
		{
			if ( mLightMapRequired && LightMap == null )
			{
				//create
				LightMap = TextureManager.Instance.CreateManual( mMaterialName + "/lm", DerivedResourceGroup, TextureType.TwoD,
				                                                 LightMapSize, LightMapSize, 0, PixelFormat.L8, TextureUsage.Static );

				mLightmapSizeActual = (ushort)LightMap.Width;

				if ( mCpuLightmapStorage != null )
				{
					// Load cached data
					using ( var data = BufferBase.Wrap( mCpuLightmapStorage ) )
					{
						var src = new PixelBox( (int)LightMapSize, (int)LightMapSize, 1, PixelFormat.L8, data );
						LightMap.GetBuffer().BlitFromMemory( src );
					}
					mCpuLightmapStorage = null;
				}
				else
				{
					// initialise to full-bright
					var box = new BasicBox( 0, 0, (int)mLightmapSizeActual, (int)mLightmapSizeActual );
					var aInit = new byte[mLightmapSizeActual*mLightmapSizeActual];
					var buf = LightMap.GetBuffer();
					var pInit = buf.Lock( box, BufferLocking.Discard ).Data;
					using ( var wrap = BufferBase.Wrap( aInit ) )
					{
						Memory.Set( wrap, 255, aInit.Length );
						Memory.Copy( wrap, pInit, aInit.Length );
					}
					buf.Unlock();
				}
			}
			else if ( !mLightMapRequired && LightMap != null )
			{
				// destroy
				TextureManager.Instance.Remove( LightMap.Handle );
				LightMap = null;
			}
		}

		[OgreVersion( 1, 7, 2 )]
		protected void CreateOrDestroyGPUCompositeMap()
		{
			if ( mCompositeMapRequired && CompositeMap == null )
			{
				//create
				CompositeMap = TextureManager.Instance.CreateManual( mMaterialName + "/comp", DerivedResourceGroup, TextureType.TwoD,
				                                                     mCompositeMapSize, mCompositeMapSize, 0, PixelFormat.BYTE_RGBA,
				                                                     TextureUsage.Static );

				mCompositeMapSizeActual = (ushort)CompositeMap.Width;

				if ( mCpuCompositeMapStorage != null )
				{
					// Load cached data
					using ( var data = BufferBase.Wrap( mCpuCompositeMapStorage ) )
					{
						var src = new PixelBox( (int)mCompositeMapSize, (int)mCompositeMapSize, 1, PixelFormat.BYTE_RGBA, data );
						CompositeMap.GetBuffer().BlitFromMemory( src );
						// release CPU copy, don't need it anymore
					}
					mCpuCompositeMapStorage = null;
				}
				else
				{
					// initialise to black
					var box = new BasicBox( 0, 0, (int)mCompositeMapSizeActual, (int)mCompositeMapSizeActual );
					var aInit = new byte[mCompositeMapSizeActual*mCompositeMapSizeActual];
					var buf = CompositeMap.GetBuffer();
					var pInit = buf.Lock( box, BufferLocking.Discard ).Data;
					using ( var wrap = BufferBase.Wrap( aInit ) )
					{
						Memory.Copy( wrap, pInit, aInit.Length );
					}
					buf.Unlock();
				}
			}
			else if ( !mCompositeMapRequired && CompositeMap != null )
			{
				// destroy
				TextureManager.Instance.Remove( CompositeMap.Handle );
				CompositeMap = null;
			}
		}

		/// <summary>
		/// Retrieve the terrain's neighbour, or null if not present.
		/// </summary>
		/// <remarks>
		/// Terrains only know about their neighbours if they are notified via
		/// setNeighbour. This information is not saved with the terrain since every
		/// tile must be able to be independent.
		/// </remarks>
		/// <param name="index">The index of the neighbour</param>
		[OgreVersion( 1, 7, 2 )]
		public Terrain GetNeighbour( NeighbourIndex index )
		{
			return mNeighbours[ (int)index ];
		}

		/// <summary>
		/// Set a terrain's neighbour, or null to detach one.
		/// </summary>
		/// <remarks>
		/// This information is not saved with the terrain since every
		/// tile must be able to be independent. However if modifications are
		/// made to a tile which can affect its neighbours, while connected the
		/// changes will be propagated.
		/// </remarks>
		/// <param name="index">The index of the neighbour</param>
		/// <param name="neighbour">The terrain instance to become the neighbour, or null to reset.</param>
		/// <param name="recalculate">
		/// If true, this terrain instance will recalculate elements
		/// that could be affected by the connection of this tile (e.g. matching 
		/// heights, calcaulting normals, calculating shadows crossing the boundary). 
		/// If false, this terrain's state is assumed to be up to date already 
		/// (e.g. was calculated with this tile present before and the state saved).
		/// </param>
		/// <param name="notifyOther">
		/// Whether the neighbour should also be notified (recommended
		/// to leave this at the default so relationships are up to date before
		/// background updates are triggered)
		/// </param>
		[OgreVersion( 1, 7, 2 )]
#if NET_40
		public void SetNeighbour( NeighbourIndex index, Terrain neighbour, bool recalculate = false, bool notifyOther = true )
#else
		public void SetNeighbour( NeighbourIndex index, Terrain neighbour, bool recalculate, bool notifyOther )
#endif
		{
			if ( mNeighbours[ (int)index ] != neighbour )
			{
				System.Diagnostics.Debug.Assert( neighbour != this, "Can't set self as own neighbour!" );

				// detach existing
				if ( mNeighbours[ (int)index ] != null && notifyOther )
				{
					mNeighbours[ (int)index ].SetNeighbour( GetOppositeNeighbour( index ), null, false, false );
				}

				mNeighbours[ (int)index ] = neighbour;
				if ( neighbour != null && notifyOther )
				{
					mNeighbours[ (int)index ].SetNeighbour( GetOppositeNeighbour( index ), this, recalculate, false );
				}

				if ( recalculate )
				{
					//recalculate, pass OUR edge rect
					var edgerect = new Rectangle();
					GetEdgeRect( index, 2, ref edgerect );
					NeighbourModified( index, edgerect, edgerect );
				}
			}
		}

#if !NET_40
		/// <see cref="Terrain.SetNeighbour(NeighbourIndex, Terrain, bool, bool)"/>
		public void SetNeighbour( NeighbourIndex index, Terrain neighbour )
		{
			SetNeighbour( index, neighbour, false, true );
		}

		/// <see cref="Terrain.SetNeighbour(NeighbourIndex, Terrain, bool, bool)"/>
		public void SetNeighbour( NeighbourIndex index, Terrain neighbour, bool recalculate )
		{
			SetNeighbour( index, neighbour, recalculate, true );
		}
#endif

		/// <summary>
		/// Get the opposite neighbour relationship (useful for finding the 
		/// neighbour index from the perspective of the tile the other side of the boundary).
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public static NeighbourIndex GetOppositeNeighbour( NeighbourIndex index )
		{
			var intindex = (int)index;
			intindex += (int)( NeighbourIndex.Count )/2;
			intindex = intindex%(int)NeighbourIndex.Count;
			return (NeighbourIndex)intindex;
		}

		/// <summary>
		/// Get the neighbour enum for a given offset in a grid (signed).
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public static NeighbourIndex GetNeighbourIndex( long x, long y )
		{
			if ( x < 0 )
			{
				if ( y < 0 )
				{
					return NeighbourIndex.SouthWest;
				}
				else if ( y > 0 )
				{
					return NeighbourIndex.NorthWest;
				}
				else
				{
					return NeighbourIndex.West;
				}
			}

			else if ( x > 0 )
			{
				if ( y < 0 )
				{
					return NeighbourIndex.SouthEast;
				}
				else if ( y > 0 )
				{
					return NeighbourIndex.NorthEast;
				}
				else
				{
					return NeighbourIndex.East;
				}
			}

			if ( y < 0 )
			{
				if ( x == 0 )
				{
					return NeighbourIndex.South;
				}
			}
			else if ( y > 0 )
			{
				if ( x == 0 )
				{
					return NeighbourIndex.North;
				}
			}

			return NeighbourIndex.North;
		}

		/// <summary>
		/// Tell this instance to notify all neighbours that will be affected
		/// by a height change that has taken place.
		/// </summary>
		/// <remarks>
		/// This method will determine which neighbours need notification and call
		/// their neighbourModified method. It is called automatically by 
		/// updateGeometry().
		/// </remarks>
		[OgreVersion( 1, 7, 2 )]
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
				var dirtyRectForNeighbours = new Rectangle( mDirtyGeometryRectForNeighbours );
				mDirtyGeometryRectForNeighbours.IsNull = true;
				// calculate light update rectangle
				var lightVec = TerrainGlobalOptions.LightMapDirection;
				var lightmapRect = new Rectangle();
				WidenRectByVector( lightVec, dirtyRectForNeighbours, MinHeight, MaxHeight, ref lightmapRect );

				for ( var i = 0; i < (int)NeighbourIndex.Count; ++i )
				{
					var ni = (NeighbourIndex)( i );
					var neighbour = GetNeighbour( ni );
					if ( neighbour == null )
					{
						continue;
					}

					// Intersect the incoming rectangles with the edge regions related to this neighbour
					var edgeRect = new Rectangle();
					GetEdgeRect( ni, 2, ref edgeRect );
					var heightEdgeRect = edgeRect.Intersect( dirtyRectForNeighbours );
					var lightmapEdgeRect = edgeRect.Intersect( lightmapRect );

					if ( !heightEdgeRect.IsNull || !lightmapRect.IsNull )
					{
						// ok, we have something valid to pass on
						var neighbourHeightEdgeRect = new Rectangle();
						var neighbourLightmapEdgeRect = new Rectangle();
						if ( !heightEdgeRect.IsNull )
						{
							GetNeighbourEdgeRect( ni, heightEdgeRect, ref neighbourHeightEdgeRect );
						}

						if ( !lightmapRect.IsNull )
						{
							GetNeighbourEdgeRect( ni, lightmapEdgeRect, ref neighbourLightmapEdgeRect );
						}

						neighbour.NeighbourModified( GetOppositeNeighbour( ni ), neighbourHeightEdgeRect, neighbourLightmapEdgeRect );
					}
				}
			}
		}

		/// <summary>
		/// Notify that a neighbour has just finished updating and that this
		/// change affects this tile.
		/// </summary>
		/// <param name="index">The neighbour index (from this tile's perspective)</param>
		/// <param name="edgerect">The area at the edge of this tile that needs height / normal
		/// recalculation (may be null)</param>
		/// <param name="shadowrect">The area on this tile where shadows need recalculating (may be null)</param>
		[OgreVersion( 1, 7, 2 )]
		public void NeighbourModified( NeighbourIndex index, Rectangle edgerect, Rectangle shadowrect )
		{
			// We can safely assume that we would not have been contacted if it wasn't 
			// important
			var neighbour = GetNeighbour( index );
			if ( neighbour == null )
			{
				return; // bogus request
			}

			var updateGeom = false;
			byte updateDerived = 0;

			if ( !edgerect.IsNull )
			{
				// update edges; match heights first, then recalculate normals
				// reduce to just single line / corner
				var heightMatchRect = new Rectangle();
				GetEdgeRect( index, 1, ref heightMatchRect );
				heightMatchRect = heightMatchRect.Intersect( edgerect );

				for ( var y = heightMatchRect.Top; y < heightMatchRect.Bottom; ++y )
				{
					for ( var x = heightMatchRect.Left; x < heightMatchRect.Right; ++x )
					{
						long nx = 0, ny = 0;
						GetNeighbourPoint( index, x, y, ref nx, ref ny );
						var neighbourHeight = neighbour.GetHeightAtPoint( nx, ny );
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
				var lightVec = TerrainGlobalOptions.LightMapDirection;
				var widenedRect = new Rectangle();
				WidenRectByVector( lightVec, shadowrect, neighbour.MinHeight, neighbour.MaxHeight, ref widenedRect );

				// set the special-case lightmap dirty rectangle
				mDirtyLightmapFromNeighboursRect.Merge( widenedRect );
				updateDerived |= DERIVED_DATA_LIGHTMAP;
			}

			if ( updateGeom )
			{
				UpdateGeometry();
			}
			if ( updateDerived != 0 )
			{
				UpdateDerivedData( false, updateDerived );
			}
		}

		[OgreVersion( 1, 7, 2 )]
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

				case NeighbourIndex.Count:
				default:
					break;
			}
			;

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
				case NeighbourIndex.SouthWest:
				case NeighbourIndex.SouthEast:
					outRect.Top = 0;
					outRect.Bottom = range;
					break;

				case NeighbourIndex.East:
				case NeighbourIndex.West:
					outRect.Top = 0;
					outRect.Bottom = mSize;
					break;

				case NeighbourIndex.Count:
				default:
					break;
			}
			;
		}

		/// <summary>
		/// get the equivalent of the passed in edge rectangle in neighbour
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public void GetNeighbourEdgeRect( NeighbourIndex index, Rectangle inRect, ref Rectangle outRect )
		{
			System.Diagnostics.Debug.Assert( mSize == GetNeighbour( index ).Size,
			                                 "Neighbour has not the same size as this instance" );

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
			}
			;

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
			;
		}

		/// <summary>
		/// get the equivalent of the passed in edge point in neighbour
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public void GetNeighbourPoint( NeighbourIndex index, long x, long y, ref long outx, ref long outy )
		{
			// Get the index of the point we should be looking at on a neighbour
			// in order to match up points
			System.Diagnostics.Debug.Assert( mSize == GetNeighbour( index ).Size,
			                                 "Neighbour has not the same size as this instance" );

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
			;

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
			;
		}

		/// <summary>
		/// Get a Vector3 of the world-space point on the terrain, aligned as per
		/// options. Cascades into neighbours if out of bounds.
		/// @note This point is relative to Terrain.Position - neighbours are
		/// adjusted to be relative to this tile
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public void GetPointFromSelfOrNeighbour( long x, long y, ref Vector3 outpos )
		{
			if ( x >= 0 && y >= 0 && x < mSize && y < mSize )
			{
				GetPoint( x, y, ref outpos );
			}
			else
			{
				long nx, ny;
				var ni = NeighbourIndex.East;
				GetNeighbourPointOverflow( x, y, out ni, out nx, out ny );
				var neighbour = GetNeighbour( ni );
				if ( neighbour != null )
				{
					var neighbourPos = Vector3.Zero;
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
		/// overflow a point into a neighbour index and point
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public void GetNeighbourPointOverflow( long x, long y, out NeighbourIndex outindex, out long outx, out long outy )
		{
			outindex = NeighbourIndex.Count;
			if ( x < 0 )
			{
				outx = x + mSize - 1;
				if ( y < 0 )
				{
					outindex = NeighbourIndex.SouthWest;
				}
				else if ( y >= mSize )
				{
					outindex = NeighbourIndex.NorthWest;
				}
				else
				{
					outindex = NeighbourIndex.West;
				}
			}
			else if ( x >= mSize )
			{
				outx = x - mSize + 1;
				if ( y < 0 )
				{
					outindex = NeighbourIndex.SouthEast;
				}
				else if ( y >= mSize )
				{
					outindex = NeighbourIndex.NorthEast;
				}
				else
				{
					outindex = NeighbourIndex.East;
				}
			}
			else
			{
				outx = x;
			}

			if ( y < 0 )
			{
				outy = y + mSize - 1;
				if ( x >= 0 && x < mSize )
				{
					outindex = NeighbourIndex.South;
				}
			}
			else if ( y >= mSize )
			{
				outy = y - mSize + 1;
				if ( x >= 0 && x < mSize )
				{
					outindex = NeighbourIndex.North;
				}
			}
			else
			{
				outy = y;
			}

			System.Diagnostics.Debug.Assert( outindex != NeighbourIndex.Count );
		}

		/// <summary>
		/// Utility method to pick a neighbour based on a ray.
		/// </summary>
		/// <param name="ray">The ray in world space</param>
		/// <param name="distanceLimit">Limit beyond which we want to ignore neighbours (0 for infinite)</param>
		/// <returns>The first neighbour along this ray, or null</returns>
		[OgreVersion( 1, 7, 2 )]
		public Terrain RaySelectNeighbour( Ray ray, Real distanceLimit )
		{
			var modifiedRay = new Ray( ray.Origin, ray.Direction );
			// Move back half a square - if we're on the edge of the AABB we might
			// miss the intersection otherwise; it's ok for everywhere else since
			// we want the far intersection anyway
			modifiedRay.Origin = modifiedRay.GetPoint( -mWorldSize/mSize*0.5f );

			// transform into terrain space
			var tPos = Vector3.Zero;
			var tDir = Vector3.Zero;
			ConvertPosition( Space.WorldSpace, modifiedRay.Origin, Space.TerrainSpace, ref tPos );
			ConvertDirection( Space.WorldSpace, modifiedRay.Direction, Space.TerrainSpace, ref tDir );
			// Discard rays with no lateral component
			if ( Utility.RealEqual( tDir.x, 0.0f, 1e-4 ) && Utility.RealEqual( tDir.y, 0.0f, 1e-4 ) )
			{
				return null;
			}

			var terrainRay = new Ray( tPos, tDir );
			// Intersect with boundary planes 
			// Only collide with the positive (exit) side of the plane, because we may be
			// querying from a point outside ourselves if we've cascaded more than once
			var dist = Real.MaxValue;
			IntersectResult intersectResult;
			if ( tDir.x < 0.0f )
			{
				intersectResult = Utility.Intersects( terrainRay, new Plane( Vector3.UnitX, Vector3.Zero ) );
				if ( intersectResult.Hit && intersectResult.Distance < dist )
				{
					dist = intersectResult.Distance;
				}
			}
			else if ( tDir.x > 0.0f )
			{
				intersectResult = Utility.Intersects( terrainRay, new Plane( Vector3.NegativeUnitX, Vector3.UnitX ) );
				if ( intersectResult.Hit && intersectResult.Distance < dist )
				{
					dist = intersectResult.Distance;
				}
			}
			if ( tDir.y < 0.0f )
			{
				intersectResult = Utility.Intersects( terrainRay, new Plane( Vector3.UnitY, Vector3.Zero ) );
				if ( intersectResult.Hit && intersectResult.Distance < dist )
				{
					dist = intersectResult.Distance;
				}
			}
			else if ( tDir.y > 0.0f )
			{
				intersectResult = Utility.Intersects( terrainRay, new Plane( Vector3.NegativeUnitY, Vector3.UnitY ) );
				if ( intersectResult.Hit && intersectResult.Distance < dist )
				{
					dist = intersectResult.Distance;
				}
			}

			// discard out of range
			if ( dist*mWorldSize > distanceLimit )
			{
				return null;
			}

			var terrainIntersectPos = terrainRay.GetPoint( dist );
			var x = terrainIntersectPos.x;
			var y = terrainIntersectPos.y;
			var dx = tDir.x;
			var dy = tDir.y;

			// Never return diagonal directions, we will navigate those recursively anyway
			if ( Utility.RealEqual( x, 1.0f, 1e-4f ) && dx > 0 )
			{
				return GetNeighbour( NeighbourIndex.East );
			}

			else if ( Utility.RealEqual( x, 0.0f, 1e-4f ) && dx < 0 )
			{
				return GetNeighbour( NeighbourIndex.West );
			}

			else if ( Utility.RealEqual( y, 1.0f, 1e-4f ) && dy > 0 )
			{
				return GetNeighbour( NeighbourIndex.North );
			}

			else if ( Utility.RealEqual( y, 0.0f, 1e-4f ) && dy < 0 )
			{
				return GetNeighbour( NeighbourIndex.South );
			}

			return null;
		}

		/// <see cref="Terrain.RaySelectNeighbour(Ray, Real)"/>
		public Terrain RaySelectNeighbour( Ray ray )
		{
			return RaySelectNeighbour( ray, 0 );
		}

		/// <summary>
		/// Dump textures to files.
		/// </summary>
		/// <remarks>This is a debugging method.</remarks>
		[OgreVersion( 1, 7, 2 )]
		public void DumpTextures( string prefix, string suffix )
		{
			var format = string.Format( "{0}_{1}{2}", prefix, "{0}", suffix );
			Image img;

			if ( TerrainNormalMap != null )
			{
				TerrainNormalMap.ConvertToImage( out img );
				img.Save( string.Format( format, "normalmap" ) );
			}

			if ( GlobalColorMap != null )
			{
				GlobalColorMap.ConvertToImage( out img );
				img.Save( string.Format( format, "colormap" ) );
			}

			if ( LightMap != null )
			{
				LightMap.ConvertToImage( out img );
				img.Save( string.Format( format, "lightmap" ) );
			}

			if ( CompositeMap != null )
			{
				CompositeMap.ConvertToImage( out img );
				img.Save( string.Format( format, "compositemap" ) );
			}

			int blendTextureIndex = 0;
			foreach ( var i in mBlendTextureList )
			{
				if ( i != null )
				{
					i.ConvertToImage( out img );
					img.Save( string.Format( format, "blendtexture" + blendTextureIndex ) );
				}
				blendTextureIndex++;
			}
		}

		/// <summary>
		/// Utility method to get the number of indexes required to render a given batch
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		internal static int GetNumIndexesForBatchSize( ushort batchSize )
		{
			int mainIndexesPerRow = batchSize*2 + 1;
			int numRows = batchSize - 1;
			int mainIndexCount = mainIndexesPerRow*numRows;
			// skirts share edges, so they take 1 less row per side than batchSize, 
			// but with 2 extra at the end (repeated) to finish the strip
			// * 2 for the vertical line, * 4 for the sides, +2 to finish
			int skirtIndexCount = ( batchSize - 1 )*2*4 + 2;
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
		internal static void PopulateIndexBuffer( BufferBase pIndexes, ushort batchSize, ushort vdatasize, int vertexIncrement,
		                                          ushort xoffset, ushort yoffset, ushort numSkirtRowsCols,
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
				var rowSize = vdatasize*vertexIncrement;
				var numRows = batchSize - 1;
				var pI = pIndexes.ToUShortPointer();
				var offset = 0;

				// Start on the right
				var currentVertex = (ushort)( ( batchSize - 1 )*vertexIncrement );
				// but, our quad area might not start at 0 in this vertex data
				// offsets are at main terrain resolution, remember
				var columnStart = xoffset;
				var rowStart = yoffset;
				currentVertex += (ushort)( rowStart*vdatasize + columnStart );
				var rightToLeft = true;
				for ( ushort r = 0; r < numRows; ++r )
				{
					for ( var c = 0; c < batchSize; ++c )
					{
						pI[ offset++ ] = currentVertex;
						pI[ offset++ ] = (ushort)( currentVertex + rowSize );

						// don't increment / decrement at a border, keep this vertex for next
						// row as we 'snake' across the tile
						if ( c + 1 < batchSize )
						{
							currentVertex = rightToLeft
							                	? (ushort)( currentVertex - vertexIncrement )
							                	: (ushort)( currentVertex + vertexIncrement );
						}
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
					var skirtIndex = CalcSkirtVertexIndex( currentVertex, vdatasize, ( s%2 ) != 0, numSkirtRowsCols, skirtRowColSkip );

					for ( var c = 0; c < batchSize - 1; ++c )
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

		/// <summary>
		/// Utility method to calculate the skirt index for a given original vertex index.
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		internal static ushort CalcSkirtVertexIndex( ushort mainIndex, ushort vdatasize, bool isCol, ushort numSkirtRowsCols,
		                                             ushort skirtRowColSkip )
		{
			// row / col in main vertex resolution
			var row = (ushort)( mainIndex/vdatasize );
			var col = (ushort)( mainIndex%vdatasize );

			// skrits are after main vertices, so skip them
			var b = (ushort)( vdatasize*vdatasize );

			// The layout in vertex data is:
			// 1. row skirts
			//    numSkirtRowsCols rows of resolution vertices each
			// 2. column skirts
			//    numSkirtRowsCols cols of resolution vertices each

			// No offsets used here, this is an index into the current vertex data, 
			// which is already relative
			if ( isCol )
			{
				var skirtNum = (ushort)( col/skirtRowColSkip );
				var colbase = (ushort)( numSkirtRowsCols*vdatasize );
				return (ushort)( b + colbase + vdatasize*skirtNum + row );
			}
			else
			{
				var skirtNum = (ushort)( row/skirtRowColSkip );
				return (ushort)( b + vdatasize*skirtNum + col );
			}
		}

		#endregion Methods

		#region IRequestHandler Members

		/// <see cref="WorkQueue.IRequestHandler.CanHandleRequest"/>
		[OgreVersion( 1, 7, 2 )]
		public bool CanHandleRequest( WorkQueue.Request req, WorkQueue srcQ )
		{
			var ddr = (DerivedDataRequest)req.Data;
			// only deal with own requests
			// we do this because if we delete a terrain we want any pending tasks to be discarded
			if ( ddr.Terrain != this )
			{
				return false;
			}
			else
			{
				return !req.Aborted;
			}
		}

		/// <see cref="WorkQueue.IRequestHandler.HandleRequest"/>
		[OgreVersion( 1, 7, 2 )]
		public WorkQueue.Response HandleRequest( WorkQueue.Request req, WorkQueue srcQ )
		{
			// Background thread (maybe)

			var ddr = (DerivedDataRequest)req.Data;
			// only deal with own requests; we shouldn't ever get here though
			if ( ddr.Terrain != this )
			{
				return null;
			}

			var ddres = new DerivedDataResponse();
			ddr.TypeMask = (byte)( ddr.TypeMask & DERIVED_DATA_ALL );

			// Do only ONE type of task per background iteration, in order of priority
			// this means we return faster, can abort faster and we repeat less redundant calcs
			// we don't do this as separate requests, because we only want one background
			// task per Terrain instance in flight at once
			if ( ( ddr.TypeMask & DERIVED_DATA_DELTAS ) == ddr.TypeMask )
			{
				ddres.DeltaUpdateRect = CalculateHeightDeltas( ddr.DirtyRect );
				ddres.RemainingTypeMask &= (byte)~DERIVED_DATA_DELTAS;
			}
			else if ( ( ddr.TypeMask & DERIVED_DATA_NORMALS ) == ddr.TypeMask )
			{
				ddres.NormalMapBox = CalculateNormals( ddr.DirtyRect, ref ddres.NormalUpdateRect );
				ddres.RemainingTypeMask &= (byte)~DERIVED_DATA_NORMALS;
			}
			else if ( ( ddr.TypeMask & DERIVED_DATA_LIGHTMAP ) == ddr.TypeMask )
			{
				ddres.LightMapPixelBox = CalculateLightMap( ddr.DirtyRect, ddr.LightmapExtraDirtyRect, ref ddres.LightMapUpdateRect );
				ddres.RemainingTypeMask &= (byte)~DERIVED_DATA_LIGHTMAP;
			}

			ddres.Terrain = ddr.Terrain;
			return new WorkQueue.Response( req, true, ddres );
		}

		#endregion IRequestHandler Members

		#region IResponseHandler Members

		/// <see cref="WorkQueue.IResponseHandler.CanHandleResponse"/>
		[OgreVersion( 1, 7, 2 )]
		public bool CanHandleResponse( WorkQueue.Response res, WorkQueue srcq )
		{
			var ddreq = (DerivedDataRequest)res.Request.Data;
			// only deal with own requests
			// we do this because if we delete a terrain we want any pending tasks to be discarded
			if ( ddreq.Terrain != this )
			{
				return false;
			}
			else
			{
				return true;
			}
		}

		/// <see cref="WorkQueue.IResponseHandler.HandleResponse"/>
		[OgreVersion( 1, 7, 2 )]
		public void HandleResponse( WorkQueue.Response res, WorkQueue srcq )
		{
			// Main thread
			var ddres = (DerivedDataResponse)res.Data;
			var ddreq = (DerivedDataRequest)res.Request.Data;

			// only deal with own requests
			if ( ddreq.Terrain != this )
			{
				return;
			}

			if ( ( ( ddreq.TypeMask & DERIVED_DATA_DELTAS ) == ddreq.TypeMask ) &&
			     ( ( ddres.RemainingTypeMask & DERIVED_DATA_DELTAS ) != DERIVED_DATA_DELTAS ) )
			{
				FinalizeHeightDeltas( ddres.DeltaUpdateRect, false );
			}
			if ( ( ( ddreq.TypeMask & DERIVED_DATA_NORMALS ) == ddreq.TypeMask ) &&
			     ( ( ddres.RemainingTypeMask & DERIVED_DATA_NORMALS ) != DERIVED_DATA_NORMALS ) )
			{
				FinalizeNormals( ddres.NormalUpdateRect, ddres.NormalMapBox );
				mCompositeMapDirtyRect.Merge( ddreq.DirtyRect );
			}
			if ( ( ( ddreq.TypeMask & DERIVED_DATA_LIGHTMAP ) == ddreq.TypeMask ) &&
			     ( ( ddres.RemainingTypeMask & DERIVED_DATA_LIGHTMAP ) != DERIVED_DATA_LIGHTMAP ) )
			{
				FinalizeLightMap( ddres.LightMapUpdateRect, ddres.LightMapPixelBox );
				mCompositeMapDirtyRect.Merge( ddreq.DirtyRect );
				mCompositeMapDirtyRectLightmapUpdate = true;
			}

			IsDerivedDataUpdateInProgress = false;

			// Re-trigger another request if there are still things to do, or if
			// we had a new request since this one
			var newRect = new Rectangle( 0, 0, 0, 0 );
			if ( ddres.RemainingTypeMask != 0 )
			{
				newRect.Merge( ddreq.DirtyRect );
			}

			if ( mDerivedUpdatePendingMask != 0 )
			{
				newRect.Merge( mDirtyDerivedDataRect );
				mDirtyDerivedDataRect.IsNull = true;
			}

			var newLightmapExtraRext = new Rectangle( 0, 0, 0, 0 );
			if ( ddres.RemainingTypeMask != 0 )
			{
				newLightmapExtraRext.Merge( ddreq.LightmapExtraDirtyRect );
			}
			if ( mDerivedUpdatePendingMask != 0 )
			{
				newLightmapExtraRext.Merge( mDirtyLightmapFromNeighboursRect );
				mDirtyLightmapFromNeighboursRect.IsNull = true;
			}
			var newMask = (byte)( ddres.RemainingTypeMask | mDerivedUpdatePendingMask );
			if ( newMask != 0 )
			{
				// trigger again
				UpdateDerivedDataImpl( newRect, newLightmapExtraRext, false, newMask );
			}
			else
			{
				// we've finished all the background processes
				// update the composite map if enabled
				if ( mCompositeMapRequired )
				{
					UpdateCompositeMap();
				}
			}
		}

		#endregion IResponseHandler Members
	};
}