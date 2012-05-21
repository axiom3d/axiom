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
using Axiom.Core;
using Axiom.Math;
using Axiom.Media;
using Axiom.Serialization;

#endregion Namespace Declarations

namespace Axiom.Components.Terrain
{
	/// <summary>
	/// Helper class to assist you in managing multiple terrain instances
	/// that are connected to each other.
	/// </summary>
	public class TerrainGroup : DisposableObject, WorkQueue.IRequestHandler, WorkQueue.IResponseHandler
	{
		#region Nested types

		/// <summary>
		/// Definition of how to populate a 'slot' in the terrain group.
		/// </summary>
		public class TerrainSlotDefinition : DisposableObject
		{
			/// <summary>
			/// Filename, if this is to be loaded from a file
			/// </summary>
			public string FileName;

			/// <summary>
			/// Import data, if this is to be defined based on importing
			/// </summary>
			public ImportData ImportData;

			/// <summary>
			/// Set to use import data 
			/// </summary>
			[OgreVersion( 1, 7, 2 )]
			public void UseImportData()
			{
				this.FileName = string.Empty;
				FreeImportData();
				this.ImportData = new ImportData();
				// we're going to own all the data in the def
				this.ImportData.DeleteInputData = true;
			}

			/// <summary>
			/// Set to use file name
			/// </summary>
			[OgreVersion( 1, 7, 2 )]
			public void UseFilename()
			{
				FreeImportData();
			}

			/// <summary>
			/// Destroy temp import resources
			/// </summary>
			[OgreVersion( 1, 7, 2 )]
			public void FreeImportData()
			{
				if ( this.ImportData != null )
				{
					this.ImportData.Dispose();
					this.ImportData = null;
				}
			}

			[OgreVersion( 1, 7, 2, "~TerrainSlotDefinition" )]
			protected override void dispose( bool disposeManagedResources )
			{
				if ( !IsDisposed )
				{
					if ( disposeManagedResources )
					{
						FreeImportData();
					}
				}
				base.dispose( disposeManagedResources );
			}
		};

		/// <summary>
		/// Slot for a terrain instance, together with its definition.
		/// </summary>
		public class TerrainSlot : DisposableObject
		{
			/// <summary>
			/// The coordinates of the terrain slot relative to the centre slot (signed).
			/// </summary>
			public long X;

			/// <summary>
			/// The coordinates of the terrain slot relative to the centre slot (signed).
			/// </summary>
			public long Y;

			/// <summary>
			/// Actual terrain instance
			/// </summary>
			public Terrain Instance;

			/// <summary>
			/// Definition used to load the terrain
			/// </summary>
			public TerrainSlotDefinition Def;

			[OgreVersion( 1, 7, 2 )]
			public TerrainSlot( long x, long y )
				: base()
			{
				this.X = x;
				this.Y = y;
				this.Instance = null;
				this.Def = new TerrainSlotDefinition();
			}

			[OgreVersion( 1, 7, 2 )]
			public void FreeInstance()
			{
				if ( this.Instance != null )
				{
					this.Instance.Dispose();
					this.Instance = null;
				}
			}

			[OgreVersion( 1, 7, 2, "~TerrainSlot" )]
			protected override void dispose( bool disposeManagedResources )
			{
				if ( !IsDisposed )
				{
					if ( disposeManagedResources )
					{
						FreeInstance();
					}
				}

				base.dispose( disposeManagedResources );
			}
		};

		/// <summary>
		/// Result from a terrain ray intersection with the terrain group. 
		/// </summary>
		public struct RayResult
		{
			/// <summary>
			/// Whether an intersection occurred
			/// </summary>
			public bool Hit;

			/// <summary>
			/// Which terrain instance was hit, if any
			/// </summary>
			public Terrain Terrain;

			/// <summary>
			/// Position at which the intersection occurred
			/// </summary>
			public Vector3 Position;

			/// <summary>
			/// Constructor.
			/// </summary>
			/// <param name="hit">Whether an intersection occurred</param>
			/// <param name="terrain">Which terrain instance was hit, if any</param>
			/// <param name="pos">Position at which the intersection occurred</param>
			[OgreVersion( 1, 7, 2 )]
			public RayResult( bool hit, Terrain terrain, Vector3 pos )
			{
				this.Hit = hit;
				this.Terrain = terrain;
				this.Position = pos;
			}
		};

		/// <summary>
		/// Structure for holding the load request
		/// </summary>
		protected struct LoadRequest
		{
			public TerrainSlot Slot;
			public TerrainGroup Origin;
		};

		#endregion Nested types

		public static ushort WORKQUEUE_LOAD_REQUEST = 1;
		public static uint ChunkID = StreamSerializer.MakeIdentifier( "TERG" );
		public static ushort ChunkVersion = 1;
		private readonly SceneManager _sceneManager;
		private Alignment _alignment;
		private ushort _terrainSize;
		private Real _terrainWorldSize;
		private readonly ImportData _defaultImportData;
		private Vector3 _origin;
		private readonly Dictionary<UInt32, TerrainSlot> _terrainSlots = new Dictionary<uint, TerrainSlot>();
		private readonly ushort _workQueueChannel;
		private string _filenamePrefix;
		private string _filenameExtension;
		private string _resourceGroup;

		protected DefaultGpuBufferAllocator BufferAllocator = new DefaultGpuBufferAllocator();

		#region Properties

		/// <summary>
		/// Define/Retrieve the centre position of the grid of terrain.
		/// </summary>
		public Vector3 Origin
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return this._origin;
			}

			[OgreVersion( 1, 7, 2 )]
			set
			{
				if ( value != this._origin )
				{
					this._origin = value;
					foreach ( var i in this._terrainSlots.Values )
					{
						if ( i.Instance != null )
						{
							i.Instance.Position = GetTerrainSlotPosition( i.X, i.Y );
						}
					}
				}
			}
		}

		/// <see cref="SetFilenameConvention"/>
		public string FilenamePrefix
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return this._filenamePrefix;
			}

			[OgreVersion( 1, 7, 2 )]
			set
			{
				this._filenamePrefix = value;
			}
		}

		/// <see cref="SetFilenameConvention"/>
		public string FilenameExtension
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return this._filenameExtension;
			}

			[OgreVersion( 1, 7, 2 )]
			set
			{
				this._filenameExtension = value;
			}
		}

		/// <summary>
		/// Calls Terrain::isDerivedDataUpdateInProgress on each loaded instance and returns true
		/// if any of them are undergoing a derived update.
		/// </summary>
		public bool IsDerivedDataUpdateInProgress
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				foreach ( var i in this._terrainSlots.Values )
				{
					if ( i.Instance != null && i.Instance.IsDerivedDataUpdateInProgress )
					{
						return true;
					}
				}
				return false;
			}
		}

		public List<TerrainSlot> TerrainSlots
		{
			get
			{
				var slots = new List<TerrainSlot>( this._terrainSlots.Values );
				return slots;
			}
		}

		/// <summary>
		/// Get's a shared structure which will provide the base settings for
		/// all terrains created via this group.
		/// </summary>
		/// <remarks>
		/// All neighbouring terrains should have the same basic settings (particularly
		/// the size parameters) - to avoid having to set the terrain import information 
		/// more than once, you can retrieve the standard settings for this group
		/// here and modify them to your needs. Once you've done that you can 
		/// use the shortcut methods in this class to create new terrain instances
		/// using these base settings (plus any per-instance settings you might
		/// want to use). 
		/// @note 
		/// The structure returned from this method is intended for in-place modification, 
		/// that's why it is not const and there is no equivalent 'set' method.
		/// You should not, however, change the alignment or any of the size parameters 
		/// after you start constructing instances, since neighbouring terrains
		/// should have the same size & alignment.
		/// </remarks>
		public ImportData DefaultImportSettings
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return this._defaultImportData;
			}
		}

		/// <summary>
		/// Get's the alignment of the grid of terrain (cannot be modified after construction).
		/// </summary>
		public Alignment Alignment
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return this._alignment;
			}
		}

		/// <summary>
		/// Retrieve the world size of each terrain instance (cannot be modified after construction).
		/// </summary>
		public Real TerrainWorldSize
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return this._terrainWorldSize;
			}
		}

		/// <summary>
		/// Get's the SceneManager being used for this group.
		/// </summary>
		public SceneManager SceneManager
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return this._sceneManager;
			}
		}

		/// <summary>
		/// Get's or Set's the resource group in which files will be located.
		/// </summary>
		public string ResourceGroup
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return this._resourceGroup;
			}

			[OgreVersion( 1, 7, 2 )]
			set
			{
				this._resourceGroup = value;
			}
		}

		#endregion Properties

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="sm">The SceneManager which will parent the terrain instances.</param>
		/// <param name="align">The alignment that all terrain instances will use</param>
		/// <param name="terrainSize">The size of each terrain down one edge in vertices (2^n+1)</param>
		/// <param name="terrainWorldSize">The world size of each terrain instance</param>
		[OgreVersion( 1, 7, 2 )]
		public TerrainGroup( SceneManager sm, Alignment align, ushort terrainSize, Real terrainWorldSize )
		{
			this._sceneManager = sm;
			this._alignment = align;
			this._terrainSize = terrainSize;
			this._terrainWorldSize = terrainWorldSize;
			this._origin = Vector3.Zero;
			this._filenamePrefix = "terrain";
			this._filenameExtension = "dat";
			this._resourceGroup = ResourceGroupManager.DefaultResourceGroupName;
			this._defaultImportData = new ImportData();
			this._defaultImportData.TerrainAlign = align;
			this._defaultImportData.WorldSize = terrainWorldSize;
			// by default we delete input data because we copy it, unless user
			// passes us an ImportData where they explicitly don't want it copied
			this._defaultImportData.DeleteInputData = true;

			WorkQueue wq = Root.Instance.WorkQueue;
			this._workQueueChannel = wq.GetChannel( "Axiom/TerrainGroup" );
			wq.AddRequestHandler( this._workQueueChannel, this );
			wq.AddResponseHandler( this._workQueueChannel, this );
		}

		/// <summary>
		/// Alternate constructor.
		/// *important*
		/// You can ONLY use this constructor if you subsequently call loadGroupDefinition
		/// to populate the rest.
		/// *important*
		/// </summary>
		/// <param name="sm">The SceneManager which will parent the terrain instances.</param>
		[OgreVersion( 1, 7, 2 )]
		public TerrainGroup( SceneManager sm )
			: this( sm, Alignment.Align_X_Z, 0, 0 )
		{
		}

		[OgreVersion( 1, 7, 2, "~TerrainGroup" )]
		protected override void dispose( bool disposeManagedResources )
		{
			if ( !IsDisposed )
			{
				if ( disposeManagedResources )
				{
					RemoveAllTerrains();

					WorkQueue wq = Root.Instance.WorkQueue;
					wq.RemoveRequestHandler( this._workQueueChannel, this );
					wq.RemoveResponseHandler( this._workQueueChannel, this );
				}
			}

			base.dispose( disposeManagedResources );
		}

		#region Methods

		/// <summary>
		/// Set the naming convention for file names in this terrain group.
		/// </summary>
		/// <remarks>
		/// You can more easily generate file names for saved / loaded terrain slots
		/// if you define a naming prefix. When you call saveAllTerrains(), all the
		/// terrain instances currently loaded will be saved to a file named
		/// &lt;prefix&gt;_&lt;index&gt;.&lt;extension&gt;, where &lt;index&gt; is
		/// given by packing the x and y coordinates of the entry into a 32-bit
		/// index (@see packIndex).
		/// </remarks>
		[OgreVersion( 1, 7, 2 )]
		public void SetFilenameConvention( string prefix, string extension )
		{
			this._filenamePrefix = prefix;
			this._filenameExtension = extension;
		}

		/// <summary>
		/// Define a 'slot' in the terrain grid - in this case to be loaded from 
		/// a generated file name.
		/// </summary>
		/// <remarks>
		/// At this stage the terrain instance isn't actually present in the grid, 
		/// you're merely expressing an intention for it to take its place there
		/// once it's loaded. The reason we do it like this is to support
		/// background preparation of this terrain instance. 
		/// @note This method assumes that you want a file name to be generated from 
		/// the naming convention that you have supplied (@see setFilenameConvention).
		/// If a file of that name isn't found during loading, then a flat terrain is
		/// created instead at height 0.
		/// </remarks>
		/// <param name="x"> The coordinates of the terrain slot relative to the centre slot (signed).</param>
		/// <param name="y"> The coordinates of the terrain slot relative to the centre slot (signed).</param>
		[OgreVersion( 1, 7, 2 )]
		public virtual void DefineTerrain( long x, long y )
		{
			DefineTerrain( x, y, GenerateFilename( x, y ) );
		}

		/// <summary>
		/// Define a 'slot' in the terrain grid - in this case a flat terrain.
		/// </summary>
		/// <remarks>
		/// At this stage the terrain instance isn't actually present in the grid, 
		/// you're merely expressing an intention for it to take its place there
		/// once it's loaded. The reason we do it like this is to support
		/// background preparation of this terrain instance. 
		/// </remarks>
		/// <param name="x">The coordinates of the terrain slot relative to the centre slot (signed).</param>
		/// <param name="y">The coordinates of the terrain slot relative to the centre slot (signed).</param>
		/// <param name="constantHeight">
		/// The constant, uniform height that you want the terrain
		/// to start at
		/// </param>
		[OgreVersion( 1, 7, 2 )]
		public virtual void DefineTerrain( long x, long y, float constantHeight )
		{
			var slot = GetTerrainSlot( x, y, true );

			slot.Def.UseImportData();

			// Copy all settings, but make sure our primary settings are immutable
			slot.Def.ImportData = this._defaultImportData;
			slot.Def.ImportData.ConstantHeight = constantHeight;
			slot.Def.ImportData.TerrainAlign = this._alignment;
			slot.Def.ImportData.TerrainSize = this._terrainSize;
			slot.Def.ImportData.WorldSize = this._terrainWorldSize;
		}

		/// <summary>
		/// Define the content of a 'slot' in the terrain grid.
		/// </summary>
		/// <remarks>
		/// At this stage the terrain instance isn't actually present in the grid, 
		/// you're merely expressing an intention for it to take its place there
		/// once it's loaded. The reason we do it like this is to support
		/// background preparation of this terrain instance. 
		/// </remarks>
		/// <param name="x">The coordinates of the terrain slot relative to the centre slot (signed).</param>
		/// <param name="y">The coordinates of the terrain slot relative to the centre slot (signed).</param>
		/// <param name="importData">Import data - this data is copied during the
		/// call so  you may destroy your copy afterwards.</param>
		[OgreVersion( 1, 7, 2 )]
		public virtual void DefineTerrain( long x, long y, ImportData importData )
		{
			var slot = GetTerrainSlot( x, y, true );

			slot.Def.UseImportData();

			// Copy all settings, but make sure our primary settings are immutable
			slot.Def.ImportData = importData;
			slot.Def.ImportData.TerrainAlign = this._alignment;
			slot.Def.ImportData.TerrainSize = this._terrainSize;
			slot.Def.ImportData.WorldSize = this._terrainWorldSize;
		}

		/// <summary>
		/// Define the content of a 'slot' in the terrain grid.
		/// </summary>
		/// <remarks>
		/// At this stage the terrain instance isn't actually present in the grid, 
		/// you're merely expressing an intention for it to take its place there
		/// once it's loaded. The reason we do it like this is to support
		/// background preparation of this terrain instance.
		/// </remarks>
		/// <param name="x">The coordinates of the terrain slot relative to the centre slot (signed).</param>
		/// <param name="y">The coordinates of the terrain slot relative to the centre slot (signed).</param>
		/// <param name="image">Heightfield image - this data is copied during the call so  you may destroy your copy afterwards.</param>
		/// <param name="layers">Optional texture layers to use (if not supplied, default import
		/// data layers will be used) - this data is copied during the
		/// call so  you may destroy your copy afterwards.</param>
		[OgreVersion( 1, 7, 2 )]
#if NET_40
		public virtual void DefineTerrain( long x, long y, Image image, List<LayerInstance> layers = null )
#else
		public virtual void DefineTerrain( long x, long y, Image image, List<LayerInstance> layers )
#endif
		{
			var slot = GetTerrainSlot( x, y, true );

			slot.FreeInstance();
			slot.Def.UseImportData();

			slot.Def.ImportData = this._defaultImportData;

			// Copy all settings, but make sure our primary settings are immutable
			// copy image - this will get deleted by importData
			slot.Def.ImportData.InputImage = new Image( image );
			if ( layers != null )
			{
				// copy (held by value)
				slot.Def.ImportData.LayerList = layers;
			}
			slot.Def.ImportData.TerrainAlign = this._alignment;
			slot.Def.ImportData.TerrainSize = this._terrainSize;
			slot.Def.ImportData.WorldSize = this._terrainWorldSize;
		}

#if !NET_40
		/// <see cref="TerrainGroup.DefineTerrain(long, long, Image, List<LayerInstance>"/>
		public void DefineTerrain( long x, long y, Image image )
		{
			DefineTerrain( x, y, image, null );
		}
#endif

		/// <summary>
		/// Define the content of a 'slot' in the terrain grid.
		/// </summary>
		/// <remarks>
		/// At this stage the terrain instance isn't actually present in the grid, 
		/// you're merely expressing an intention for it to take its place there
		/// once it's loaded. The reason we do it like this is to support
		/// background preparation of this terrain instance.
		/// </remarks>
		/// <param name="x">The coordinates of the terrain slot relative to the centre slot (signed).</param>
		/// <param name="y">The coordinates of the terrain slot relative to the centre slot (signed).</param>
		/// <param name="data">Heights array</param>
		/// <param name="layers">Optional texture layers to use (if not supplied, default import
		/// data layers will be used) - this data is copied during the
		/// call so  you may destroy your copy afterwards.</param>
		[OgreVersion( 1, 7, 2 )]
#if NET_40
		public virtual void DefineTerrain( long x, long y, float[] data, List<LayerInstance> layers = null )
#else
		public virtual void DefineTerrain( long x, long y, float[] data, List<LayerInstance> layers )
#endif
		{
			var slot = GetTerrainSlot( x, y, true );

			slot.FreeInstance();
			slot.Def.UseImportData();

			slot.Def.ImportData = this._defaultImportData;

			// Copy all settings, but make sure our primary settings are immutable
			if ( data != null )
			{
				// copy data - this will get deleted by importData
				slot.Def.ImportData.InputFloat = new float[data.Length];
				Array.Copy( data, slot.Def.ImportData.InputFloat, data.Length );
			}
			if ( layers != null )
			{
				// copy (held by value)
				slot.Def.ImportData.LayerList = layers;
			}
			slot.Def.ImportData.TerrainAlign = this._alignment;
			slot.Def.ImportData.TerrainSize = this._terrainSize;
			slot.Def.ImportData.WorldSize = this._terrainWorldSize;
		}

#if !NET_40
		/// <see cref="TerrainGroup.DefineTerrain( long, long, float[], List<LayerInstance> )"/>
		public void DefineTerrain( long x, long y, float[] data )
		{
			DefineTerrain( x, y, data, null );
		}
#endif

		/// <summary>
		/// Define the content of a 'slot' in the terrain grid.
		/// </summary>
		/// <remarks>
		/// At this stage the terrain instance isn't actually present in the grid, 
		/// you're merely expressing an intention for it to take its place there
		/// once it's loaded. The reason we do it like this is to support
		/// background preparation of this terrain instance.
		/// </remarks>
		/// <param name="x">The coordinates of the terrain slot relative to the centre slot (signed).</param>
		/// <param name="y">The coordinates of the terrain slot relative to the centre slot (signed).</param>
		/// <param name="filename">The name of a file which fully defines the terrain (as 
		/// written by Terrain::save). Size settings from all files must agree.</param>
		[OgreVersion( 1, 7, 2 )]
		public virtual void DefineTerrain( long x, long y, string filename )
		{
			var slot = GetTerrainSlot( x, y, true );
			slot.FreeInstance();

			slot.Def.UseFilename();
			slot.Def.FileName = filename;
		}

		/// <summary>
		/// Load any terrain instances that have been defined but not loaded yet.
		/// </summary>
		/// <param name="synchronous">Whether we should force this to happen entirely in the
		/// primary thread (default false, operations are threaded if possible)</param>
		[OgreVersion( 1, 7, 2 )]
#if NET_40
		public virtual void LoadAllTerrains( bool synchronous = false )
#else
		public virtual void LoadAllTerrains( bool synchronous )
#endif
		{
			// Just a straight iteration - for the numbers involved not worth 
			// keeping a loaded / unloaded list
			foreach ( var i in this._terrainSlots.Values )
			{
				LoadTerrainImpl( i, synchronous );
			}
		}

#if !NET_40
		/// <see cref="TerrainGroup.LoadAllTerrains( bool )"/>
		public void LoadAllTerrains()
		{
			LoadAllTerrains( false );
		}
#endif

		/// <summary>
		/// Save all terrain instances using the assigned file names, or
		/// via the filename convention.
		/// <see cref="TerrainGroup.SetFilenameConvention"/>
		/// <see cref="TerrainGroup.ResourceGroup"/>
		/// </summary>
		/// <param name="onlyIfModified">
		/// If true, only terrains that have been modified since load(),
		/// or since the last save(), will be saved. You want to set this to true if
		/// you loaded the terrain from these same files, but false if you 
		/// defined them using some other input data since the files wouldn't exist.
		/// </param>
		/// <param name="replaceManualFilenames">
		/// If true, replaces any manually defined filenames
		/// in the TerrainSlotDefinition with the generated names from the convention.
		/// This is recommended since it makes everything more consistent, although
		/// you might want to use manual filenames in the original definition to import 
		/// previously separate data.
		/// </param>
		[OgreVersion( 1, 7, 2 )]
#if NET_40
		public void SaveAllTerrains( bool onlyIfModified, bool replaceManualFilenames = true )
#else
		public void SaveAllTerrains( bool onlyIfModified, bool replaceManualFilenames )
#endif
		{
			foreach ( var i in this._terrainSlots.Values )
			{
				TerrainSlot slot = i;
				if ( slot.Instance != null )
				{
					Terrain t = slot.Instance;
					if ( t.IsLoaded && ( !onlyIfModified || t.IsModified ) )
					{
						// Overwrite the file names?
						if ( replaceManualFilenames )
						{
							slot.Def.FileName = GenerateFilename( slot.X, slot.Y );
						}

						string filename = string.Empty;
						if ( !string.IsNullOrEmpty( slot.Def.FileName ) )
						{
							filename = slot.Def.FileName;
						}
						else
						{
							filename = GenerateFilename( slot.X, slot.Y );
						}

						t.Save( filename );
					}
				}
			}
		}

#if !NET_40
		/// <see cref="TerrainGroup.SaveAllTerrains( bool, bool )"/>
		public void SaveAllTerrains( bool onlyIfModified )
		{
			SaveAllTerrains( onlyIfModified, true );
		}
#endif

		/// <summary>
		/// Load a specific terrain slot based on the definition that has already 
		/// been supplied.
		/// </summary>
		/// <param name="x">The coordinates of the terrain slot relative to the centre slot (signed).</param>
		/// <param name="y">The coordinates of the terrain slot relative to the centre slot (signed).</param>
		/// <param name="synchronous">
		/// Whether we should force this to happen entirely in the
		/// primary thread (default false, operations are threaded if possible)
		/// </param>
		[OgreVersion( 1, 7, 2 )]
#if NET_40
		public virtual void LoadTerrain( long x, long y, bool synchronous = false )
#else
		public virtual void LoadTerrain( long x, long y, bool synchronous )
#endif
		{
			var slot = GetTerrainSlot( x, y, false );
			if ( slot != null )
			{
				LoadTerrainImpl( slot, synchronous );
			}
		}

#if !NET_40
		/// <see cref="TerrainGroup.LoadTerrain(long, long, bool)"/>
		public void LoadTerrain( long x, long y )
		{
			LoadTerrain( x, y, false );
		}
#endif

		[OgreVersion( 1, 7, 2 )]
		protected void LoadTerrainImpl( TerrainSlot slot, bool synchronous )
		{
			if ( slot.Instance == null && ( !string.IsNullOrEmpty( slot.Def.FileName ) || slot.Def.ImportData != null ) )
			{
				// Allocate in main thread so no race conditions
				slot.Instance = new Terrain( this._sceneManager );
				slot.Instance.ResourceGroup = this._resourceGroup;
				// Use shared pool of buffers
				slot.Instance.GpuBufferAllocator = this.BufferAllocator;

				var req = new LoadRequest();
				req.Slot = slot;
				req.Origin = this;
				Root.Instance.WorkQueue.AddRequest( this._workQueueChannel, WORKQUEUE_LOAD_REQUEST, req, 0, synchronous );
			}
		}

		/// <summary>
		/// Unload a specific terrain slot.
		/// </summary>
		/// <remarks>
		/// This destroys the Terrain instance but retains the slot definition (so
		/// it would be reloaded next time you call loadAllTerrains() if you did not
		/// remove it beforehand).
		/// @note
		/// While the definition of the terrain is kept, if you used import data
		/// to populate it, this will have been lost so repeat loading cannot occur. 
		/// The only way to support repeat loading is via the 'filename' option of
		/// defineTerrain instead.
		/// </remarks>
		/// <param name="x">The coordinates of the terrain slot relative to the centre slot (signed).</param>
		/// <param name="y">The coordinates of the terrain slot relative to the centre slot (signed).</param>
		[OgreVersion( 1, 7, 2 )]
		public virtual void UnloadTerrain( long x, long y )
		{
			var slot = GetTerrainSlot( x, y, false );
			if ( slot != null )
			{
				slot.FreeInstance();
			}
		}

		/// <summary>
		/// Remove a specific terrain slot.
		/// </summary>
		/// <remarks>
		/// This destroys any Terrain instance at this position and also removes the 
		/// definition, so it essentially no longer exists.
		/// </remarks>
		/// <param name="x">The coordinates of the terrain slot relative to the centre slot (signed).</param>
		/// <param name="y">The coordinates of the terrain slot relative to the centre slot (signed).</param>
		[OgreVersion( 1, 7, 2 )]
		public virtual void RemoveTerrain( long x, long y )
		{
			uint key = PackIndex( x, y );
			TerrainSlot slot = null;
			if ( this._terrainSlots.TryGetValue( key, out slot ) )
			{
				slot.Dispose();
				slot = null;
				this._terrainSlots.Remove( key );
			}
		}

		/// <summary>
		/// Remove all terrain instances.
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public void RemoveAllTerrains()
		{
			foreach ( var i in this._terrainSlots.Values )
			{
				if ( !i.IsDisposed )
				{
					i.Dispose();
				}
			}
			this._terrainSlots.Clear();
			// Also clear buffer pools, if we're clearing completely may not be representative
			this.BufferAllocator.FreeAllBuffers();
		}

		/// <summary>
		/// Get the definition of a slot in the terrain.
		/// </summary>
		/// <remarks>
		/// Definitions exist before the actual instances to allow background loading.
		/// </remarks>
		/// <param name="x">The coordinates of the terrain slot relative to the centre slot (signed).</param>
		/// <param name="y">The coordinates of the terrain slot relative to the centre slot (signed).</param>
		/// <returns>
		/// The definition, or null if nothing is in this slot. While this return value is
		/// not const, you should be careful about modifying it; it will have no effect unless you load
		/// the terrain afterwards, and can cause a race condition if you modify it while a background
		/// load is in progress.
		/// </returns>
		[OgreVersion( 1, 7, 2 )]
		public virtual TerrainSlotDefinition GetTerrainDefinition( long x, long y )
		{
			var slot = GetTerrainSlot( x, y );
			if ( slot != null )
			{
				return slot.Def;
			}
			else
			{
				return null;
			}
		}

		/// <summary>
		/// Get the terrain instance at a given slot, if loaded.
		/// </summary>
		/// <param name="x">The coordinates of the terrain slot relative to the centre slot (signed).</param>
		/// <param name="y">The coordinates of the terrain slot relative to the centre slot (signed).</param>
		/// <returns>The terrain, or null if no terrain is loaded in this slot (call getTerrainDefinition if
		/// you want to access the definition before it is loaded).</returns>
		[OgreVersion( 1, 7, 2 )]
		public virtual Terrain GetTerrain( long x, long y )
		{
			var slot = GetTerrainSlot( x, y );
			if ( slot != null )
			{
				return slot.Instance;
			}
			else
			{
				return null;
			}
		}

		/// <summary>
		/// Get the height data for a given world position (projecting the point
		/// down on to the terrain underneath).
		/// </summary>
		/// <param name="x">Position in world space. Positions will be clamped to the edge of the terrain</param>
		/// <param name="y">Position in world space. Positions will be clamped to the edge of the terrain</param>
		/// <param name="z">Position in world space. Positions will be clamped to the edge of the terrain</param>
		/// <param name="terrain">Pointer to a pointer to a terrain which will be completed
		/// with the terrain that was found to resolve this query, or null if none were</param>
		[OgreVersion( 1, 7, 2 )]
		public float GetHeightAtWorldPosition( Real x, Real y, Real z, ref Terrain terrain )
		{
			return GetHeightAtWorldPosition( new Vector3( x, y, z ), ref terrain );
		}

		/// <see cref="TerrainGroup.GetHeightAtWorldPosition(Real, Real, Real, ref Terrain)"/>
		public float GetHeightAtWorldPosition( Real x, Real y, Real z )
		{
			Terrain t = null;
			return GetHeightAtWorldPosition( new Vector3( x, y, z ), ref t );
		}

		/// <summary>
		/// Get the height data for a given world position (projecting the point
		/// down on to the terrain).
		/// </summary>
		/// <param name="pos">Position in world space. Positions will be clamped to the edge of the terrain</param>
		/// <param name="terrain">Pointer to a pointer to a terrain which will be completed
		/// with the terrain that was found to resolve this query, or null if none were</param>
		[OgreVersion( 1, 7, 2 )]
		public float GetHeightAtWorldPosition( Vector3 pos, ref Terrain terrain )
		{
			long x = 0, y = 0;
			ConvertWorldPositionToTerrainSlot( pos, out x, out y );
			var slot = GetTerrainSlot( x, y );
			if ( slot != null && slot.Instance != null && slot.Instance.IsLoaded )
			{
				if ( terrain != null )
				{
					terrain = slot.Instance;
				}
				return slot.Instance.GetHeightAtWorldPosition( pos );
			}
			else
			{
				if ( terrain != null )
				{
					terrain = null;
				}
				return 0;
			}
		}

		/// <see cref="TerrainGroup.GetHeightAtWorldPosition(Vector3, ref Terrain)"/>
		public float GetHeightAtWorldPosition( Vector3 position )
		{
			Terrain t = null;
			return GetHeightAtWorldPosition( position, ref t );
		}

		/// <summary>
		/// Test for intersection of a given ray with any terrain in the group. If the ray hits
		/// a terrain, the point of intersection and terrain instance is returned.
		/// </summary>
		/// <remarks>
		/// This can be called from any thread as long as no parallel write to the terrain data occurs.
		/// </remarks>
		/// <param name="ray">The ray to test for intersection</param>
		/// <param name="distanceLimit">The distance from the ray origin at which we will stop looking,
		/// 0 indicates no limit</param>
		/// <returns>A result structure which contains whether the ray hit a terrain and if so, where.</returns>
		[OgreVersion( 1, 7, 2 )]
		public RayResult RayIntersects( Ray ray, Real distanceLimit )
		{
			long curr_x = 0, curr_z = 0;
			ConvertWorldPositionToTerrainSlot( ray.Origin, out curr_x, out curr_z );
			var slot = GetTerrainSlot( curr_x, curr_z );
			var result = new RayResult( false, null, Vector3.Zero );

			Vector3 tmp, localRayDir, centreOrigin, offset;
			tmp = Vector3.Zero;
			// get the middle of the current tile
			ConvertTerrainSlotToWorldPosition( curr_x, curr_z, out centreOrigin );
			offset = ray.Origin - centreOrigin;
			localRayDir = ray.Direction;
			switch ( Alignment )
			{
				case Alignment.Align_X_Y:
					Utility.Swap<Real>( ref localRayDir.x, ref localRayDir.z );
					Utility.Swap<Real>( ref offset.x, ref offset.z );
					break;

				case Alignment.Align_Y_Z:
					// x = z, z = y, y = -x
					tmp.x = localRayDir.z;
					tmp.z = localRayDir.y;
					tmp.y = -localRayDir.x;
					localRayDir = tmp;
					tmp.x = offset.z;
					tmp.z = offset.y;
					tmp.y = -offset.x;
					offset = tmp;
					break;

				case Alignment.Align_X_Z:
					// already in X/Z but values increase in -Z
					localRayDir.z = -localRayDir.z;
					offset.z = -offset.z;
					break;
			}

			// Normalise the offset  based on the world size of a square, and rebase to the bottom left
			offset /= this._terrainWorldSize;
			offset += 0.5f;
			// this is our counter moving away from the 'current' square
			var inc = new Vector3( Math.Utility.Abs( localRayDir.x ), Math.Utility.Abs( localRayDir.y ),
			                       Math.Utility.Abs( localRayDir.z ) );
			long xdir = localRayDir.x > 0.0f ? 1 : -1;
			long zdir = localRayDir.z > 0.0f ? 1 : -1;

			// We're always counting from 0 to 1 regardless of what direction we're heading
			if ( xdir < 0 )
			{
				offset.x = 1.0f - offset.x;
			}
			if ( zdir < 0 )
			{
				offset.z = 1.0f - offset.z;
			}

			// find next slot
			bool keepSearching = true;
			int numGaps = 0;
			while ( keepSearching )
			{
				if ( Math.Utility.RealEqual( inc.x, 0.0f ) && Math.Utility.RealEqual( inc.z, 0.0f ) )
				{
					keepSearching = false;
				}

				while ( ( slot == null || slot.Instance == null ) && keepSearching )
				{
					++numGaps;
					/// if we don't find any filled slot in 6 traversals, give up
					if ( numGaps > 6 )
					{
						keepSearching = false;
						break;
					}
					// find next slot
					Vector3 oldoffset = offset;
					while ( offset.x < 1.0f && offset.z < 1.0f )
					{
						offset += inc;
					}
					if ( offset.x >= 1.0f && offset.z >= 1.0f )
					{
						// We crossed a corner, need to figure out which we passed first
						Real diffz = 1.0f - oldoffset.z;
						Real diffx = 1.0f - oldoffset.x;
						Real distz = diffz/inc.z;
						Real distx = diffx/inc.x;
						if ( distx < distz )
						{
							curr_x += xdir;
							offset.x -= 1.0f;
						}
						else
						{
							curr_z += zdir;
							offset.z -= 1.0f;
						}
					}
					else if ( offset.x >= 1.0f )
					{
						curr_x += xdir;
						offset.x -= 1.0f;
					}
					else if ( offset.z >= 1.0f )
					{
						curr_z += zdir;
						offset.z -= 1.0f;
					}
					if ( distanceLimit > 0 )
					{
						Vector3 worldPos;
						ConvertTerrainSlotToWorldPosition( curr_x, curr_z, out worldPos );
						if ( ray.Origin.Distance( worldPos ) > distanceLimit )
						{
							keepSearching = false;
							break;
						}
					}
					slot = GetTerrainSlot( curr_x, curr_z );
				}
				if ( slot != null && slot.Instance != null )
				{
					numGaps = 0;
					// don't cascade into neighbours
					KeyValuePair<bool, Vector3> raypair = slot.Instance.RayIntersects( ray, false, distanceLimit );
					if ( raypair.Key )
					{
						keepSearching = false;
						result.Hit = true;
						result.Terrain = slot.Instance;
						result.Position = raypair.Value;
						break;
					}
					else
					{
						// not this one, trigger search for another slot
						slot = null;
					}
				}
			}

			return result;
		}

		/// <see cref="TerrainGroup.RayIntersects(Ray, Real)"/>
		public RayResult RayIntersects( Ray ray )
		{
			return RayIntersects( ray, 0 );
		}

		/// <summary>
		/// Test intersection of a box with the terrain.
		/// </summary>
		/// <remarks>
		/// Tests an AABB for overlap with a terrain bounding box. Note that this does not mean that the box
		/// touches the terrain itself, just the bounding box for the terrain. You can use this to get region
		/// results for further testing or use (e.g. painting areas).
		/// </remarks>
		/// <param name="box">The AABB you want to test in world units</param>
		/// <param name="terrainList">Pointer to a list of terrain pointers which will be updated to include just
		/// the terrains that overlap</param>
		[OgreVersion( 1, 7, 2 )]
		public void BoxIntersects( AxisAlignedBox box, out List<Terrain> terrainList )
		{
			terrainList = new List<Terrain>();
			foreach ( var i in this._terrainSlots.Values )
			{
				if ( i.Instance != null && box.Intersects( i.Instance.WorldAABB ) )
				{
					terrainList.Add( i.Instance );
				}
			}
		}

		/// <summary>
		/// Test intersection of a sphere with the terrain.
		/// </summary>
		/// <remarks>
		/// Tests a sphere for overlap with a terrain bounding box. Note that this does not mean that the sphere
		/// touches the terrain itself, just the bounding box for the terrain. You can use this to get region
		/// results for further testing or use (e.g. painting areas).
		/// </remarks>
		/// <param name="sphere">The sphere you want to test in world units</param>
		/// <param name="terrainList">Pointer to a list of terrain pointers which will be updated to include just
		/// the terrains that overlap</param>
		[OgreVersion( 1, 7, 2 )]
		public void SphereIntersects( Sphere sphere, out List<Terrain> terrainList )
		{
			terrainList = new List<Terrain>();
			foreach ( var i in this._terrainSlots.Values )
			{
				if ( i.Instance != null && sphere.Intersects( i.Instance.WorldAABB ) )
				{
					terrainList.Add( i.Instance );
				}
			}
		}

		/// <summary>
		/// Convert a world position to terrain slot coordinates.
		/// </summary>
		/// <param name="position">The world position</param>
		/// <param name="x">Pointers to the x coordinate to be completed.</param>
		/// <param name="y">Pointers to the y coordinate to be completed.</param>
		[OgreVersion( 1, 7, 2 )]
		public void ConvertWorldPositionToTerrainSlot( Vector3 position, out long x, out long y )
		{
			// 0,0 terrain is centred at the origin
			Vector3 terrainPos;
			// convert to standard xy base (z up), make relative to origin
			Terrain.ConvertWorldToTerrainAxes( this._alignment, position - this._origin, out terrainPos );

			Real offset = this._terrainWorldSize*0.5f;
			terrainPos.x += offset;
			terrainPos.y += offset;

			x = (long)( System.Math.Floor( terrainPos.x/this._terrainWorldSize ) );
			y = (long)( System.Math.Floor( terrainPos.y/this._terrainWorldSize ) );
		}

		/// <summary>
		/// Convert a slot location to a world position at the centre
		/// </summary>
		/// <param name="x">The x slot coordinate</param>
		/// <param name="y">The y slot coordinate</param>
		/// <param name="position">Pointer to the world position to be completed</param>
		[OgreVersion( 1, 7, 2 )]
		public void ConvertTerrainSlotToWorldPosition( long x, long y, out Vector3 position )
		{
			var terrainPos = new Vector3( x*this._terrainWorldSize, y*this._terrainWorldSize, 0 );
			Terrain.ConvertTerrainToWorldAxes( this._alignment, terrainPos, out position );
			position += this._origin;
		}

		[OgreVersion( 1, 7, 2 )]
		protected void ConnectNeighbour( TerrainSlot slot, long offsetx, long offsety )
		{
			var neighbourSlot = GetTerrainSlot( slot.X + offsetx, slot.Y + offsety );
			if ( neighbourSlot != null && neighbourSlot.Instance != null && neighbourSlot.Instance.IsLoaded )
			{
				// reclaculate if imported
				slot.Instance.SetNeighbour( Terrain.GetNeighbourIndex( offsetx, offsety ), neighbourSlot.Instance,
				                            slot.Def.ImportData != null );
			}
		}

		/// <summary>
		/// Convert coordinates to a packed integer index
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public uint PackIndex( long x, long y )
		{
			// Convert to signed 16-bit so sign bit is in bit 15
			var xs16 = (Int16)x;
			var ys16 = (Int16)y;

			// convert to unsigned because we do not want to propagate sign bit to 32-bits
			var x16 = (UInt16)xs16;
			var y16 = (UInt16)ys16;

			uint key = 0;
			key = (uint)( ( x16 << 16 ) | y16 );

			return key;
		}

		/// <summary>
		/// Convert a packed integer index to coordinates
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public void UnpackIndex( uint key, out long x, out long y )
		{
			// inverse of packIndex
			// unsigned versions
			var y16 = (UInt16)( key & 0xFFFF );
			var x16 = (UInt16)( ( key >> 16 ) & 0xFFFF );

			x = x16;
			y = x16;
		}

		/// <summary>
		/// Generate a file name based on the current naming convention
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public string GenerateFilename( long x, long y )
		{
			return string.Format( "{0}_{1}.{2}", this._filenamePrefix, PackIndex( x, y ).ToString().PadLeft( 8, '0' ),
			                      this._filenameExtension );
		}

		/// <summary>
		/// Get the position of a terrain instance
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		protected Vector3 GetTerrainSlotPosition( long x, long y )
		{
			Vector3 pos;
			ConvertTerrainSlotToWorldPosition( x, y, out pos );
			return pos;
		}

		/// <summary>
		/// Retrieve a slot, potentially allocate one
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		protected TerrainSlot GetTerrainSlot( long x, long y, bool createIfMissing )
		{
			var key = PackIndex( x, y );
			TerrainSlot i;
			if ( this._terrainSlots.TryGetValue( key, out i ) )
			{
				return i;
			}

			else if ( createIfMissing )
			{
				i = new TerrainSlot( x, y );
				this._terrainSlots.Add( key, i );
				return i;
			}
			return null;
		}

		[OgreVersion( 1, 7, 2 )]
		protected TerrainSlot GetTerrainSlot( long x, long y )
		{
			return GetTerrainSlot( x, y, false );
		}

		/// <summary>
		/// Free as many resources as possible for optimal run-time memory use for all terrain tiles.
		/// <see cref="Terrain.FreeTemporaryResources"/>
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public void FreeTemporaryResources()
		{
			foreach ( var i in this._terrainSlots.Values )
			{
				if ( i.Instance != null )
				{
					i.Instance.FreeTemporaryResources();
				}
			}
		}

		/// <summary>
		/// Trigger the update process for all terrain instances.
		/// <see cref="Terrain.Update"/>
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
#if NET_40
        public void Update( bool synchronous = false)
#else
		public void Update( bool synchronous )
#endif
		{
			foreach ( var i in this._terrainSlots.Values )
			{
				if ( i.Instance != null )
				{
					i.Instance.Update( synchronous );
				}
			}
		}

#if !NET_40
		/// <see cref="TerrainGroup.Update(bool)"/>
		public void Update()
		{
			Update( false );
		}
#endif

		/// <summary>
		/// Performs an update on all terrain geometry.
		/// <see cref="Terrain.UpdateGeometry"/>
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public void UpdateGeometry()
		{
			foreach ( var i in this._terrainSlots.Values )
			{
				if ( i.Instance != null )
				{
					i.Instance.UpdateGeometry();
				}
			}
		}

		/// <summary>
		/// Updates derived data for all terrains (LOD, lighting) to reflect changed height data.
		/// <see cref="Terrain.UpdateDerivedData"/>
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
#if NET_40
        public void UpdateDerivedData( bool synchronous = false, byte typeMask = 0xFF )
#else
		public void UpdateDerivedData( bool synchronous, byte typeMask )
#endif
		{
			foreach ( var i in this._terrainSlots.Values )
			{
				if ( i.Instance != null )
				{
					i.Instance.UpdateDerivedData( synchronous, typeMask );
				}
			}
		}

#if !NET_40
		/// <see cref="TerrainGroup.UpdateDerivedData(bool, byte)"/>
		public void UpdateDerivedData()
		{
			UpdateDerivedData( false, 0xFF );
		}

		/// <see cref="TerrainGroup.UpdateDerivedData(bool, byte)"/>
		public void UpdateDerivedData( bool synchronous )
		{
			UpdateDerivedData( synchronous, 0xFF );
		}
#endif

		/// <summary>
		/// Save the group data only in native form to a file.
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public void SaveGroupDefinition( string filename )
		{
			var stream = Root.Instance.CreateFileStream( filename, ResourceGroup, true );
			var ser = new StreamSerializer( stream );
			SaveGroupDefinition( ref ser );
		}

		/// <summary>
		/// Save the group data only in native form to a serializing stream.
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public void SaveGroupDefinition( ref StreamSerializer stream )
		{
			stream.WriteChunkBegin( ChunkID, ChunkVersion );
			// Base details
			stream.Write( this._alignment );
			stream.Write( this._terrainSize );
			stream.Write( this._terrainWorldSize );
			stream.Write( this._filenamePrefix );
			stream.Write( this._filenameExtension );
			stream.Write( this._resourceGroup );
			stream.Write( this._origin );

			// Default import settings (those not duplicated by the above)
			stream.Write( this._defaultImportData.ConstantHeight );
			stream.Write( this._defaultImportData.InputBias );
			stream.Write( this._defaultImportData.InputScale );
			stream.Write( this._defaultImportData.MaxBatchSize );
			stream.Write( this._defaultImportData.MinBatchSize );
			Terrain.WriteLayerDeclaration( this._defaultImportData.LayerDeclaration, ref stream );
			Terrain.WriteLayerInstanceList( this._defaultImportData.LayerList, ref stream );

			stream.WriteChunkEnd( ChunkID );
		}

		/// <summary>
		/// Load the group definition only in native form from a file.
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public void LoadGroupDefinition( string filename )
		{
			var stream = Root.Instance.OpenFileStream( filename, ResourceGroup );
			var ser = new StreamSerializer( stream );
			LoadGroupDefinition( ref ser );
		}

		/// <summary>
		/// Load the group definition only in native form from a serializing stream.
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public void LoadGroupDefinition( ref StreamSerializer stream )
		{
			if ( stream.ReadChunkBegin( ChunkID, ChunkVersion ) == null )
			{
				throw new AxiomException( "Item not found!,Stream does not contain TerrainGroup data" );
			}

			// Base details
			stream.Read( out this._alignment );
			stream.Read( out this._terrainSize );
			stream.Read( out this._terrainWorldSize );
			stream.Read( out this._filenamePrefix );
			stream.Read( out this._filenameExtension );
			stream.Read( out this._resourceGroup );
			stream.Read( out this._origin );

			// Default import settings (those not duplicated by the above)
			stream.Read( out this._defaultImportData.ConstantHeight );
			stream.Read( out this._defaultImportData.InputBias );
			stream.Read( out this._defaultImportData.InputScale );
			stream.Read( out this._defaultImportData.MaxBatchSize );
			stream.Read( out this._defaultImportData.MinBatchSize );
			this._defaultImportData.LayerDeclaration = new TerrainLayerDeclaration();
			Terrain.ReadLayerDeclaration( ref stream, ref this._defaultImportData.LayerDeclaration );
			this._defaultImportData.LayerList = new List<LayerInstance>();
			Terrain.ReadLayerInstanceList( ref stream, this._defaultImportData.LayerDeclaration.Samplers.Count,
			                               ref this._defaultImportData.LayerList );

			// copy data that would have normally happened on construction
			this._defaultImportData.TerrainAlign = this._alignment;
			this._defaultImportData.TerrainSize = this._terrainSize;
			this._defaultImportData.WorldSize = this._terrainWorldSize;
			this._defaultImportData.DeleteInputData = true;

			stream.ReadChunkEnd( ChunkID );
		}

		#endregion Methods

		#region IRequestHandler Members

		/// <see cref="WorkQueue.IRequestHandler.CanHandleRequest"/>
		[OgreVersion( 1, 7, 2 )]
		public bool CanHandleRequest( WorkQueue.Request req, WorkQueue srcQ )
		{
			var lreq = (LoadRequest)req.Data;
			// only deal with own requests
			if ( lreq.Origin != this )
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
			var lreq = (LoadRequest)req.Data;

			var def = lreq.Slot.Def;
			var t = lreq.Slot.Instance;
			System.Diagnostics.Debug.Assert( t != null, "Terrain instance should have been constructed in the main thread" );
			WorkQueue.Response response;
			try
			{
				if ( !string.IsNullOrEmpty( def.FileName ) )
				{
					t.Prepare( def.FileName );
				}
				else
				{
					System.Diagnostics.Debug.Assert( def.ImportData != null, "No import data or file name" );
					t.Prepare( def.ImportData );
					// if this worked, we can destroy the input data to save space
					def.FreeImportData();
				}
				response = new WorkQueue.Response( req, true, new object() );
			}
			catch ( Exception e )
			{
				// oops
				response = new WorkQueue.Response( req, false, new object(), e.Message );
			}

			return response;
		}

		#endregion IRequestHandler Members

		#region IResponseHandler Members

		/// <see cref="WorkQueue.IResponseHandler.CanHandleResponse"/>
		[OgreVersion( 1, 7, 2 )]
		public bool CanHandleResponse( WorkQueue.Response res, WorkQueue srcq )
		{
			var lreq = (LoadRequest)res.Request.Data;
			// only deal with own requests
			if ( lreq.Origin != this )
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
			// No response data, just request
			var lreq = (LoadRequest)res.Request.Data;

			if ( res.Succeeded )
			{
				var slot = lreq.Slot;
				var terrain = slot.Instance;
				if ( terrain != null )
				{
					// do final load now we've prepared in the background
					// we must set the position
					terrain.Position = GetTerrainSlotPosition( slot.X, slot.Y );
					terrain.Load();

					// hook up with neighbours
					for ( int i = -1; i <= 1; ++i )
					{
						for ( int j = -1; j <= 1; ++j )
						{
							if ( i != 0 || j != 0 )
							{
								ConnectNeighbour( slot, i, j );
							}
						}
					}
				}
			}
			else
			{
				// oh dear
				LogManager.Instance.Write( LogMessageLevel.Critical, false,
				                           "We failed to prepare the terrain at ({0}, {1}) with the error '{2}'", lreq.Slot.X,
				                           lreq.Slot.Y, res.Messages );
				lreq.Slot.FreeInstance();
			}
		}

		#endregion IResponseHandler Members
	};
}