using System;
using System.Collections;
using System.Collections.Generic;
using Axiom.Core;
using Axiom.Math;
using Axiom.Media;
using Axiom.Serialization;
namespace Axiom.Components.Terrain
{
    /// <summary>
    /// Definition of how to populate a 'slot' in the terrain group.
    /// </summary>
    public class TerrainSlotDefinition : IDisposable
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
        public void UseImportData()
        {
            FileName = string.Empty;
            FreeImportData();
            ImportData = new ImportData();
            ImportData.DeleteInputData = true;
        }
        /// <summary>
        /// Set to use file name
        /// </summary>
        public void UseFilename()
        {
            FreeImportData();
        }
        /// <summary>
        /// Destroy temp import resources
        /// </summary>
        public void FreeImportData()
        {
            ImportData = null;
        }
        /// <summary>
        /// 
        /// </summary>
        public void Dispose()
        {
            FreeImportData();
        }
    }
    /// <summary>
    /// Slot for a terrain instance, together with its definition.
    /// </summary>
    public class TerrainSlot : IDisposable
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
        /// <summary>
        /// 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public TerrainSlot(long x, long y)
        {
            X = x;
            Y = y;
            Instance = null;
            Def = new TerrainSlotDefinition();
        }
        /// <summary>
        /// 
        /// </summary>
        public void FreeInstance()
        {
            Instance = null;
        }

        public void Dispose()
        {
            FreeInstance();
        }
    }
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
        public RayResult(bool hit, Terrain terrain, Vector3 pos)
        {
            Hit = hit;
            Terrain = terrain;
            Position = pos;
        }
    }
    /// <summary>
    /// Helper class to assist you in managing multiple terrain instances
    /// that are connected to each other.
    /// </summary>
    public class TerrainGroup : IDisposable
    {
        /// <summary>
        /// 
        /// </summary>
        public static uint ChunkID = StreamSerializer.MakeIdentifier("TERG");
        /// <summary>
        /// 
        /// </summary>
        public static ushort ChunkVersion = 1;
        /// <summary>
        /// 
        /// </summary>
        private SceneManager _sceneManager;
        /// <summary>
        /// 
        /// </summary>
        private Alignment _alignment;
        /// <summary>
        /// 
        /// </summary>
        private ushort _terrainSize;
        /// <summary>
        /// 
        /// </summary>
        private float _terrainWorldSize;
        /// <summary>
        /// 
        /// </summary>
        private ImportData _defaultImportData;
        /// <summary>
        /// 
        /// </summary>
        private Vector3 _origin;
        /// <summary>
        /// 
        /// </summary>
        private Dictionary<UInt32, TerrainSlot> _terrainSlots;
        /// <summary>
        /// 
        /// </summary>
        private ushort _workQueueChannel;
        /// <summary>
        /// 
        /// </summary>
        private string _filenamePrefix;
        /// <summary>
        /// 
        /// </summary>
        private string _filenameExtension;
        /// <summary>
        /// 
        /// </summary>
        private string _resourceGroup;

        /// <summary>
        /// 
        /// </summary>
        public List<TerrainSlot> TerrainSlots
        {
            get
            {
                List<TerrainSlot> slots = new List<TerrainSlot>();
                foreach (TerrainSlot slot in _terrainSlots.Values)
                    slots.Add(slot);

                return slots;
            }
        }
        /// <summary>
        /// Get's a shared structure which will provide the base settings for
        /// all terrains created via this group.
        /// </summary>
        public ImportData DefaultImportSettings
        {
            get { return _defaultImportData; }
        }
        /// <summary>
        /// Define the centre position of the grid of terrain.
        /// </summary>
        public Vector3 Origin
        {
            get { return _origin; }
            set
            {
                if (value != _origin)
                {
                    _origin = value;
                    foreach (TerrainSlot i in _terrainSlots.Values)
                    {
                        if (i.Instance != null)
                            i.Instance.Position = GetTerrainSlotPosition(i.X, i.Y);
                    }
                }
            }
        }
        /// <summary>
        /// Get's the alignment of the grid of terrain (cannot be modified after construction).
        /// </summary>
        public Alignment Alignment
        {
            get { return _alignment; }
        }
        /// <summary>
        /// Retrieve the world size of each terrain instance
        /// </summary>
        public float TerrainWorldSize
        {
            get { return _terrainWorldSize; }
            set 
            {
                if (value != _terrainWorldSize)
                {
                    _terrainWorldSize = value;
                    foreach (TerrainSlot i in _terrainSlots.Values)
                    {
                        if (i.Instance != null)
                        {
                            i.Instance.WorldSize = _terrainWorldSize;
                            i.Instance.Position = GetTerrainSlotPosition(i.X, i.Y);
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Get's the size of each terrain instance in number of vertices down one side
        /// </summary>
        public ushort TerrainSize
        {
            get { return _terrainSize; }
            set 
            {
                if (_terrainSize != value)
                {
                    _terrainSize = value;
                    foreach (TerrainSlot i in _terrainSlots.Values)
                    {
                        if (i.Instance != null)
                        {
                            i.Instance.Size = _terrainSize;
                        }
                    }
                }

            }
        }
        /// <summary>
        /// Get's the SceneManager being used for this group.
        /// </summary>
        public SceneManager SceneManager
        {
            get { return _sceneManager; }
        }
        /// <summary>
        /// <see cref="SetFilenameConvention"/>
        /// </summary>
        public string FilenamePrefix
        {
            get { return _filenamePrefix; }
            set { _filenamePrefix = value; }
        }
        /// <summary>
        /// <see cref="SetFilenameConvention"/>
        /// </summary>
        public string FilenameExtension
        {
            get { return _filenameExtension; }
            set { _filenameExtension = value; }
        }
        /// <summary>
        /// Get's or Set's the resource group in which files will be located.
        /// </summary>
        public string ResourceGroup
        {
            get { return _resourceGroup; }
            set { _resourceGroup = value; }
        }
        /// <summary>
        /// 
        /// </summary>
        public bool IsDerivedDataUpdateInProgress
        {
            get 
            {
                foreach (TerrainSlot i in _terrainSlots.Values)
                {
                    if (i.Instance != null && i.Instance.IsDerivedDataUpdateInProgress)
                        return true;
                }
                return false;
            }
        }
        /// <summary>
        /// Alternate constructor.
        /// *important*
        /// You can ONLY use this constructor if you subsequently call loadGroupDefinition
        /// to populate the rest.
        /// *important*
        /// </summary>
        /// <param name="sm">The SceneManager which will parent the terrain instances.</param>
        public TerrainGroup(SceneManager sm)
            : this(sm, Alignment.Align_X_Z, 0, 0)
        { }
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="sm">The SceneManager which will parent the terrain instances.</param>
        /// <param name="align">The alignment that all terrain instances will use</param>
        /// <param name="terrainSize">The size of each terrain down one edge in vertices (2^n+1)</param>
        /// <param name="terrainWorldSize">The world size of each terrain instance</param>
        public TerrainGroup(SceneManager sm, Alignment align, ushort terrainSize,
            float terrainWorldSize)
        {
            _sceneManager = sm;
            _alignment = align;
            _terrainSize = terrainSize;
            _terrainWorldSize = terrainWorldSize;
            _origin = Vector3.Zero;
            _filenamePrefix = "terrain";
            _filenameExtension = "dat";
            _resourceGroup = ResourceGroupManager.DefaultResourceGroupName;
            _terrainSlots = new Dictionary<uint, TerrainSlot>();
            _defaultImportData = new ImportData();
            _defaultImportData.TerrainAlign = align;
            _defaultImportData.WorldSize = terrainWorldSize;
            _defaultImportData.DeleteInputData = true;
        }
        /// <summary>
        /// Set the naming convention for file names in this terrain group.
        /// </summary>
        /// <param name="prefix"></param>
        /// <param name="extension"></param>
        public void SetFilenameConvention(string prefix, string extension)
        {
            _filenamePrefix = prefix;
            _filenameExtension = extension;
        }
        /// <summary>
        /// Define a 'slot' in the terrain grid - in this case to be loaded from 
        /// a generated file name.
        /// </summary>
        /// <param name="x"> The coordinates of the terrain slot relative to the centre slot (signed).</param>
        /// <param name="y"> The coordinates of the terrain slot relative to the centre slot (signed).</param>
        public void DefineTerrain(long x, long y)
        {
            DefineTerrain(x, y, GenerateFilename(x, y));
        }
        /// <summary>
        /// Define a 'slot' in the terrain grid - in this case a flat terrain.
        /// </summary>
        /// <param name="x">The coordinates of the terrain slot relative to the centre slot (signed).</param>
        /// <param name="y">The coordinates of the terrain slot relative to the centre slot (signed).</param>
        /// <param name="constantHeight">
        /// The constant, uniform height that you want the terrain
        /// to start at
        /// </param>
        public void DefineTerrain(long x, long y, float constantHeight)
        {
            TerrainSlot slot = GetTerrainSlot(x, y, true);

            slot.Def.UseImportData();

            slot.Def.ImportData = _defaultImportData;
            slot.Def.ImportData.ConstantHeight = constantHeight;
            slot.Def.ImportData.TerrainAlign = _alignment;
            slot.Def.ImportData.TerrainSize = _terrainSize;
            slot.Def.ImportData.WorldSize = _terrainWorldSize;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="importData"></param>
        public void DefineTerrain(long x, long y, ImportData importData)
        {
            TerrainSlot slot = GetTerrainSlot(x, y, true);

            slot.Def.UseImportData();

            slot.Def.ImportData = importData;
            slot.Def.ImportData.TerrainAlign = _alignment;
            slot.Def.ImportData.TerrainSize = _terrainSize;
            slot.Def.ImportData.WorldSize = _terrainWorldSize;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="image"></param>
        public void DefineTerrain(long x, long y, Image image)
        {
            DefineTerrain(x, y, image, null);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="image"></param>
        /// <param name="layers"></param>
        public void DefineTerrain(long x, long y, Image image, List<LayerInstance> layers)
        {
            TerrainSlot slot = GetTerrainSlot(x, y, true);
            slot.FreeInstance();
            slot.Def.UseImportData();

            slot.Def.ImportData = _defaultImportData;
#warning implement image copy constructor
            slot.Def.ImportData.InputImage = image;
            if (layers != null)
            {
                slot.Def.ImportData.LayerList = layers;
            }
            slot.Def.ImportData.TerrainAlign = _alignment;
            slot.Def.ImportData.TerrainSize = _terrainSize;
            slot.Def.ImportData.WorldSize = _terrainWorldSize;

        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="image"></param>
        public void DefineTerrain(long x, long y, float[] data)
        {
            DefineTerrain(x, y, data, null);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="image"></param>
        /// <param name="layers"></param>
        public void DefineTerrain(long x, long y, float[] data, List<LayerInstance> layers)
        {
            TerrainSlot slot = GetTerrainSlot(x, y, true);
            slot.FreeInstance();
            slot.Def.UseImportData();

            slot.Def.ImportData = _defaultImportData;

            if (data != null)
            {
                slot.Def.ImportData.InputFloat = new float[data.Length];
                Array.Copy(data, slot.Def.ImportData.InputFloat, data.Length);
            }
            if (layers != null)
            {
                slot.Def.ImportData.LayerList = layers;
            }
            slot.Def.ImportData.TerrainAlign = _alignment;
            slot.Def.ImportData.TerrainSize = _terrainSize;
            slot.Def.ImportData.WorldSize = _terrainWorldSize;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="filename"></param>
        public void DefineTerrain(long x, long y, string filename)
        {
            TerrainSlot slot = GetTerrainSlot(x, y, true);
            slot.FreeInstance();

            slot.Def.UseFilename();
            slot.Def.FileName = filename;
        }
        /// <summary>
        /// Load any terrain instances that have been defined but not loaded yet.
        /// </summary>
        public void LoadAllTerrains()
        {
#warning normaly we should try to load the terrain's asynchron (threaded), but this is not yet supported by axiom
            LoadAllTerrains(true);
        }
        /// <summary>
        /// Load any terrain instances that have been defined but not loaded yet.
        /// </summary>
        /// <param name="synchronous"></param>
        public void LoadAllTerrains(bool synchronous)
        {
            if (!synchronous)
            {
                throw new NotImplementedException("Loading the Terrain asynchronous is not yet possible!");
            }

            foreach (TerrainSlot i in _terrainSlots.Values)
            {
                LoadTerrainImpl(i, synchronous);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public void LoadTerrain(long x, long y)
        {
            LoadTerrain(x, y, true);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="synchronous"></param>
        public void LoadTerrain(long x, long y, bool synchronous)
        {
            if (!synchronous)
            {
                throw new NotImplementedException("Loading the Terrain asynchronous is not yet possible!");
            }

            TerrainSlot slot = GetTerrainSlot(x, y, false);
            if(slot != null)
            {
                LoadTerrainImpl(slot, synchronous);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public void UnloadTerrain(long x, long y)
        {
            TerrainSlot slot = GetTerrainSlot(x, y, false);
            if (slot != null)
            {
                slot.FreeInstance();
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public void RemoveTerrain(long x, long y)
        {
            uint key = PackIndex(x, y);
            TerrainSlot slot = null;
            if (_terrainSlots.TryGetValue(key, out slot))
            {
                slot = null;
                _terrainSlots.Remove(key);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        public void RemoveAllTerrains()
        {
            _terrainSlots.Clear();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="onlyIfModified"></param>
        public void SaveAllTerrains(bool onlyIfModified)
        {
            SaveAllTerrains(onlyIfModified, true);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="onlyIfModified"></param>
        /// <param name="replaceManualFilenames"></param>
        public void SaveAllTerrains(bool onlyIfModified, bool replaceManualFilenames)
        {
            foreach (TerrainSlot i in _terrainSlots.Values)
            {
                TerrainSlot slot = i;
                if (slot.Instance != null)
                {
                    Terrain t = slot.Instance;
                    if (t.IsLoaded &&
                        (!onlyIfModified || t.IsModified))
                    {
                        if (replaceManualFilenames)
                            slot.Def.FileName = GenerateFilename(slot.X, slot.Y);

                        string filename = string.Empty;
                        if (!string.IsNullOrEmpty(slot.Def.FileName))
                            filename = slot.Def.FileName;
                        else
                            filename = GenerateFilename(slot.X, slot.Y);

                        t.Save(filename);
                    }
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public TerrainSlotDefinition GetTerrainDefinition(long x, long y)
        {
            TerrainSlot slot = GetTerrainSlot(x, y);
            if (slot != null)
                return slot.Def;
            else
                return null;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public Terrain GetTerrain(long x, long y)
        {
            TerrainSlot slot = GetTerrainSlot(x, y);
            if (slot != null)
                return slot.Instance;
            else
                return null;
        }
        /// <summary>
        /// 
        /// </summary>
        public void FreeTemporaryResources()
        {
            foreach (TerrainSlot i in _terrainSlots.Values)
                if (i.Instance != null)
                    i.Instance.FreeTemporaryResources();
        }
        /// <summary>
        /// Trigger the update process for all terrain instances.
        /// </summary>
        public void Update()
        {
            Update(true);
        }
        /// <summary>
        /// Trigger the update process for all terrain instances.
        /// </summary>
        /// <param name="synchronous"></param>
        public void Update(bool synchronous)
        {
            if (!synchronous)
            {
                throw new NotImplementedException("Loading the Terrain asynchronous is not yet possible!");
            }
            foreach (TerrainSlot i in _terrainSlots.Values)
            {
                if (i.Instance != null)
                    i.Instance.Update(synchronous);
            }
        }
        /// <summary>
        /// Performs an update on all terrain geometry.
        /// </summary>
        public void UpdateGeometry()
        {
            foreach (TerrainSlot i in _terrainSlots.Values)
            {
                if (i.Instance != null)
                    i.Instance.UpdateGeometry();
            }
        }
        /// <summary>
        /// 
        /// </summary>
        public void UpdateDerivedData()
        {
            UpdateDerivedData(true, 0xff);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="synchronous"></param>
        public void UpdateDerivedData(bool synchronous)
        {
            UpdateDerivedData(synchronous, 0xff);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="synchronous"></param>
        /// <param name="typeMask"></param>
        public void UpdateDerivedData(bool synchronous, byte typeMask)
        {
            if (!synchronous)
            {
                throw new NotImplementedException("Loading the Terrain asynchronous is not yet possible!");
            }

            foreach (TerrainSlot i in _terrainSlots.Values)
            {
                if (i.Instance != null)
                    i.Instance.UpdateDerivedData(synchronous, typeMask);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <returns></returns>
        public float GetHeightAtWorldPosition(float x, float y, float z)
        {
            Terrain t = null;
            return GetHeightAtWorldPosition(new Vector3(x, y, z), ref t);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <param name="terrain"></param>
        /// <returns></returns>
        public float GetHeightAtWorldPosition(float x, float y, float z,ref Terrain terrain)
        {
            return GetHeightAtWorldPosition(new Vector3(x, y, z),ref  terrain);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public float GetHeightAtWorldPosition(Vector3 position)
        {
            Terrain t = null;
            return GetHeightAtWorldPosition(position, ref t);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <param name="terrain"></param>
        /// <returns></returns>
        public float GetHeightAtWorldPosition(Vector3 pos, ref Terrain terrain)
        {
            long x = 0, y = 0;
            ConvertWorldPositionToTerrainSlot(pos, out x, out y);
            TerrainSlot slot = GetTerrainSlot(x, y);
            if (slot != null && slot.Instance != null && slot.Instance.IsLoaded)
            {
                if (terrain != null)
                    terrain = slot.Instance;
                return slot.Instance.GetHeightAtWorldPosition(pos);
            }
            else
            {
                if (terrain != null)
                    terrain = null;
                return 0;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="ray"></param>
        /// <returns></returns>
        public RayResult RayIntersects(Ray ray)
        {
            return RayIntersects(ray, 0);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="ray"></param>
        /// <param name="distanceLimit"></param>
        /// <returns></returns>
        public RayResult RayIntersects(Ray ray, float distanceLimit)
        {
            long curr_x = 0, curr_z = 0;
            ConvertWorldPositionToTerrainSlot(ray.Origin, out curr_x, out curr_z);
            TerrainSlot slot = GetTerrainSlot(curr_x, curr_z);

            RayResult result = new RayResult(false, null, Vector3.Zero);

            Vector3 tmp, localRayDir, centreOrigin, offset;
            tmp = localRayDir = centreOrigin = offset = Vector3.Zero;
            ConvertTerrainSlotToWorldPosition(curr_x, curr_z, out centreOrigin);
            offset = ray.Origin - centreOrigin;
            localRayDir = ray.Direction;
            switch (Alignment)
            {
                case Alignment.Align_X_Y:
                    float t1 = localRayDir.x, t2 = localRayDir.z;
                    Swap(t1, t2);
                    localRayDir = new Vector3(t1, localRayDir.y, t2);
                    t1 = offset.x;
                    t2 = offset.z;
                    Swap(t1, t2);
                    localRayDir = new Vector3(t1, offset.y, t2);
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

            offset /= _terrainWorldSize;
            offset = new Vector3(offset.x +0.5f,offset.y + 0.5f, offset.z +0.5f);
            Vector3 inc = new Vector3(Math.Utility.Abs(localRayDir.x), Math.Utility.Abs(localRayDir.y), Math.Utility.Abs(localRayDir.z));
            long xdir = localRayDir.x > 0.0f ? 1 : -1;
            long zdir = localRayDir.z > 0.0f ? 1 : -1;

            // We're always counting from 0 to 1 regardless of what direction we're heading
	                if (xdir < 0)
	                        offset.x = 1.0f - offset.x;
	                if (zdir < 0)
	                        offset.z = 1.0f - offset.z;
	
	                // find next slot
	                bool keepSearching = true;
	                int numGaps = 0;
                    while (keepSearching)
                    {
                        if (Math.Utility.RealEqual(inc.x, 0.0f) && Math.Utility.RealEqual(inc.z, 0.0f))
                            keepSearching = false;

                        while ((slot != null || slot.Instance != null) && keepSearching)
                        {
                            ++numGaps;
                            /// if we don't find any filled slot in 6 traversals, give up
                            if (numGaps > 6)
                            {
                                keepSearching = false;
                                break;
                            }
                            // find next slot
                            Vector3 oldoffset = offset;
                            while (offset.x < 1.0f && offset.z < 1.0f)
                                offset += inc;
                            if (offset.x >= 1.0f && offset.z >= 1.0f)
                            {
                                // We crossed a corner, need to figure out which we passed first
                                Real diffz = 1.0f - oldoffset.z;
                                Real diffx = 1.0f - oldoffset.x;
                                Real distz = diffz / inc.z;
                                Real distx = diffx / inc.x;
                                if (distx < distz)
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
                            else if (offset.x >= 1.0f)
                            {
                                curr_x += xdir;
                                offset.x -= 1.0f;
                            }
                            else if (offset.z >= 1.0f)
                            {
                                curr_z += zdir;
                                offset.z -= 1.0f;
                            }
                            if (distanceLimit > 0)
                            {
                                Vector3 worldPos;
                                ConvertTerrainSlotToWorldPosition(curr_x, curr_z, out worldPos);
                                if (ray.Origin.Distance(worldPos) > distanceLimit)
                                {
                                    keepSearching = false;
                                    break;
                                }
                            }
                            slot = GetTerrainSlot(curr_x, curr_z);
                        }
                        if (slot != null && slot.Instance != null)
                        {
                            numGaps = 0;
                            // don't cascade into neighbours
                            KeyValuePair<bool, Vector3> raypair = slot.Instance.RayIntersects(ray, false, distanceLimit);
                            if (raypair.Key)
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
        /// <summary>
        /// Swaps to objects. be sure they have the same type
        /// </summary>
        /// <param name="oba"></param>
        /// <param name="obb"></param>
        public void Swap(float oba, float obb)
        {
            float tmp = oba;
            oba = obb;
            obb = oba; ;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="box"></param>
        /// <param name="terrainList"></param>
        public void BoxIntersects(AxisAlignedBox box, out List<Terrain> terrainList)
        {
            terrainList = new List<Terrain>();
            foreach (TerrainSlot i in _terrainSlots.Values)
            {
                if (i.Instance != null && box.Intersects(i.Instance.WorldAABB))
                    terrainList.Add(i.Instance);
            }
        }
        /// <summary>
        /// /
        /// </summary>
        /// <param name="sphere"></param>
        /// <param name="terrainList"></param>
        public void SphereIntersects(Sphere sphere, out List<Terrain> terrainList)
        {
            terrainList = new List<Terrain>();
            foreach (TerrainSlot i in _terrainSlots.Values)
            {
                if (i.Instance != null && sphere.Intersects(i.Instance.WorldAABB))
                    terrainList.Add(i.Instance);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="position"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public void ConvertWorldPositionToTerrainSlot(Vector3 position, out long x, out long y)
        {
            Vector3 terrainPos = Vector3.Zero;
            Terrain.ConvertWorldToTerrainAxes(_alignment, position - _origin, out terrainPos);

            float offset = _terrainWorldSize * 0.5f;
            terrainPos.x += offset;
            terrainPos.y += offset;

            x = (long)(System.Math.Floor(terrainPos.x / _terrainWorldSize));
            y = (long)(System.Math.Floor(terrainPos.y / _terrainWorldSize));
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="position"></param>
        public void ConvertTerrainSlotToWorldPosition(long x, long y, out Vector3 position)
        {
            Vector3 terrainPos = new Vector3(x * _terrainWorldSize, y * _terrainWorldSize, 0);
            Terrain.ConvertTerrainToWorldAxes(_alignment, terrainPos, out position);
            position += _origin;
        }
        /// <summary>
        /// 
        /// </summary>
        public void Dispose()
        {
            RemoveAllTerrains();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public uint PackIndex(long x, long y)
        {
            // Convert to signed 16-bit so sign bit is in bit 15
            Int16 xs16 = (Int16)x;
            Int16 ys16 = (Int16)y;

            // convert to unsigned because we do not want to propagate sign bit to 32-bits
            UInt16 x16 = (UInt16)xs16;
            UInt16 y16 = (UInt16)ys16;

            uint key = 0;
            key = (uint)((x16 << 16) | y16);

            return key;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public void UnpackIndex(uint key, out long x, out long y)
        {
            // inverse of packIndex
            // unsigned versions
            UInt16 y16 = (UInt16)(key & 0xFFFF);
            UInt16 x16 = (UInt16)((key >> 16) & 0xFFFF);

            x = x16;
            y = x16;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public string GenerateFilename(long x, long y)
        {
            string str = string.Empty;
            str = "terrain" + "_" +
                string.Format("{0:########}", PackIndex(x, y)) + "." + "dat";

            return str;

        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="filename"></param>
        public void SaveGroupDefinition(string filename)
        {
            throw new NotImplementedException();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="stream"></param>
        public void SaveGroupDefinition(ref StreamSerializer stream)
        {
            stream.WriteChunkBegin(ChunkID, ChunkVersion);
            stream.Write(_alignment);
            stream.Write(_terrainSize);
            stream.Write(_terrainWorldSize);
            stream.Write(_filenamePrefix);
            stream.Write(_filenameExtension);
            stream.Write(_resourceGroup);
            stream.Write(_origin);

            stream.Write(_defaultImportData.ConstantHeight);
            stream.Write(_defaultImportData.InputBias);
            stream.Write(_defaultImportData.InputScale);
            stream.Write(_defaultImportData.MaxBatchSize);
            stream.Write(_defaultImportData.MinBatchSize);
            Terrain.WriteLayerDeclaration(_defaultImportData.LayerDeclaration, ref stream);
            Terrain.WriteLayerInstanceList(_defaultImportData.LayerList, ref stream);

            stream.WriteChunkEnd(ChunkID);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="filename"></param>
        public void LoadGroupDefinition(string filename)
        {
            throw new NotImplementedException();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="stream"></param>
        public void LoadGroupDefinition(ref StreamSerializer stream)
        {
            if (stream.ReadChunkBegin(ChunkID, ChunkVersion) == null)
                throw new AxiomException("Item not found!,Stream does not contain TerrainGroup data", new object[] { });

            // Base details
            stream.Read(out _alignment);
            stream.Read(out _terrainSize);
            stream.Read(out _terrainWorldSize);
            stream.Read(out _filenamePrefix);
            stream.Read(out _filenameExtension);
            stream.Read(out _resourceGroup);
            stream.Read(out _origin);

            stream.Read(out _defaultImportData.ConstantHeight);
            stream.Read(out _defaultImportData.InputBias);
            stream.Read(out _defaultImportData.InputScale);
            stream.Read(out _defaultImportData.MaxBatchSize);
            stream.Read(out _defaultImportData.MinBatchSize);
            _defaultImportData.LayerDeclaration = new TerrainLayerDeclaration();
            Terrain.ReadLayerDeclaration(ref stream, ref _defaultImportData.LayerDeclaration);
            _defaultImportData.LayerList = new List<LayerInstance>();
            Terrain.ReadLayerInstanceList(ref stream, _defaultImportData.LayerDeclaration.Samplers.Count,
                ref _defaultImportData.LayerList);

            _defaultImportData.TerrainAlign = _alignment;
            _defaultImportData.TerrainSize = _terrainSize;
            _defaultImportData.WorldSize = _terrainWorldSize;
            _defaultImportData.DeleteInputData = true;

            stream.ReadChunkEnd(ChunkID);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        protected Vector3 GetTerrainSlotPosition(long x, long y)
        {
            Vector3 pos;
            ConvertTerrainSlotToWorldPosition(x, y, out pos);
            return pos;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="createIfMissing"></param>
        /// <returns></returns>
        protected TerrainSlot GetTerrainSlot(long x, long y, bool createIfMissing)
        {
            uint key = PackIndex(x, y);
            TerrainSlot i = null;
            if (_terrainSlots.TryGetValue(key, out i))
            {
                return i;
            }
            else if (createIfMissing)
            {
                i = new TerrainSlot(x, y);
                _terrainSlots.Add(key, i);
                return i;
            }
            return null;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        protected TerrainSlot GetTerrainSlot(long x, long y)
        {
            return GetTerrainSlot(x, y, false);
            
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="slot"></param>
        /// <param name="offsetx"></param>
        /// <param name="offsety"></param>
        protected void ConnectNeighbour(TerrainSlot slot, long offsetx, long offsety)
        {
            TerrainSlot neighbourSlot = GetTerrainSlot(slot.X + offsetx, slot.Y + offsety);
            if (neighbourSlot != null && neighbourSlot.Instance != null && neighbourSlot.Instance.IsLoaded)
            {
                // reclaculate if imported
                slot.Instance.SetNeighbour(Terrain.GetNeighbourIndex(offsetx, offsety), neighbourSlot.Instance,
                    slot.Def.ImportData != null, true);
            }
        }
        protected void LoadTerrainImpl(TerrainSlot slot)
        {
            LoadTerrainImpl(slot, true);
        }
        protected void LoadTerrainImpl(TerrainSlot slot, bool synchronous)
        {
            if (!synchronous)
                throw new NotSupportedException("Loading terrain asynchronous is not yet supported!");

            if (slot.Instance == null &&
                (string.IsNullOrEmpty(slot.Def.FileName) || slot.Def.ImportData != null))
            {
                slot.Instance = new Terrain(_sceneManager);
                slot.Instance.ResourceGroup = _resourceGroup;
                slot.Instance.Prepare(_defaultImportData);
                slot.Instance.Position = GetTerrainSlotPosition(slot.X, slot.Y);
                slot.Instance.Load();
                // hook up with neighbours
                for (int i = -1; i <= 1; ++i)
                {
                    for (int j = -1; j <= 1; ++j)
                    {
                        if (i != 0 || j != 0)
                            ConnectNeighbour(slot, i, j);
                    }

                }
            }
        }
    }
}
