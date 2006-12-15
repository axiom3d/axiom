#region LGPL License
/*
Axiom Graphics Engine Library
Copyright (C) 2003-2006  Axiom Project Team

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
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <id value="$Id$"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections;
using System.Text;
using System.IO;


using DotNet3D.Math;

#endregion Namespace Declarations
			
namespace Axiom
{
    /// <summary>
    ///    The details of a topological region which is the highest level of partitioning for this class.
    /// </summary>
    /// <remarks>
    ///     The size & shape of regions entirely depends on the SceneManager
	///		specific implementation. It is a MovableObject since it will be
	///		attached to a node based on the local centre - in practice it
    ///		won't actually move (although in theory it could).    /// </remarks>
    /// <ogre name="OgreRegion">
    ///     <file name="OgreStaticGeometry.h"   revision="1.19" lastUpdated="6/15/2006" lastUpdatedBy="Skyrapper" />
    ///     <file name="OgreStaticGeometry.cpp" revision="1.27" lastUpdated="6/15/2005" lastUpdatedBy="Skyrapper" />
    /// </ogre>
    public class Region : MovableObject
    {
        #region Inner Classes
        public class RegionShadowRenderable : ShadowRenderable
        {
            public RegionShadowRenderable(Region parent, HardwareIndexBuffer indexBuffer, VertexData vertexData, bool createSeparateLightCap)
                : this(parent, indexBuffer, vertexData, createSeparateLightCap, false)
            {
            }

            public RegionShadowRenderable( Region parent, HardwareIndexBuffer indexBuffer, VertexData vertexData, bool createSeparateLightCap, bool isLightCap )
            {
                _parent = parent;

                //Initialize render op
                renderOp.indexData = new IndexData();
                renderOp.indexData.indexBuffer = indexBuffer;
                renderOp.indexData.indexStart = 0;

                //index start and count are sorted later

                //Create vertex data which just references position component (and 2 component)
                renderOp.vertexData = new VertexData();
                renderOp.vertexData.vertexDeclaration = HardwareBufferManager.Instance.CreateVertexDeclaration();
                renderOp.vertexData.vertexBufferBinding = HardwareBufferManager.Instance.CreateVertexBufferBinding();

                //Map in position data
                renderOp.vertexData.vertexDeclaration.AddElement(0,0,VertexElementType.Float3,VertexElementSemantic.Position);
                ushort origPosBind=vertexData.vertexDeclaration.FindElementBySemantic(VertexElementSemantic.Position).Source;


                _positionBuffer= vertexData.vertexBufferBinding.GetBuffer(origPosBind);

                renderOp.vertexData.vertexBufferBinding.SetBinding(0, _positionBuffer);

                //Map in w-coord buffer (if present)
                if (vertexData.hardwareShadowVolWBuffer != null)
                {
                    renderOp.vertexData.vertexDeclaration.AddElement(1, 0, VertexElementType.Float1, VertexElementSemantic.TexCoords, 0);
                    _wBuffer = vertexData.hardwareShadowVolWBuffer;
                    renderOp.vertexData.vertexBufferBinding.SetBinding(1, _wBuffer);
                }

                //Use same vertex start as input
                renderOp.vertexData.vertexStart = vertexData.vertexStart;

                if (isLightCap)
                {
                    //Use original vertex count, no extrusion
                    renderOp.vertexData.vertexCount = vertexData.vertexCount;
                }
                else
                {
                    //Vertex count must take into account the doubling of the buffer,
                    //because second half of the buffer is the extruded copy
                    renderOp.vertexData.vertexCount = vertexData.vertexCount * 2;
                    if (createSeparateLightCap)
                    {
                        //Create child light cap
                        lightCap = new RegionShadowRenderable(parent, indexBuffer, vertexData, false, true);
                    }
                }
            }

            #region Properties

            #region Parent

            private Region _parent;

            protected Region Parent
            {
                get { return _parent; }
                set { _parent = value; }
            }

            #endregion

            #region PositionBuffer

            private HardwareVertexBuffer _positionBuffer;

            protected HardwareVertexBuffer PositionBuffer
            {
                get
                {
                    return _positionBuffer;
                }
            }

            #endregion

            #region WBuffer

            private HardwareVertexBuffer _wBuffer;

            protected HardwareVertexBuffer WBuffer
            {
                get
                {
                    return _wBuffer;
                }
            }

            #endregion

            public override void GetWorldTransforms(Matrix4[] matrices)
            {
                matrices = _parent.ParentNodeFullTransform();
            }

            public override Quaternion WorldOrientation
            {
                get
                {
                    return _parent.ParentNode.DerivedOrientation;
                }
            }

            public override Vector3 WorldPosition
            {
                get
                {
                    _parent.Center;
                }
            }

            #endregion
        }

        #endregion

        #region Constructors

        public Region(StaticGeometry parent, string name, SceneManager mgr, UInt32 regionID, Vector3 center):base(name)
        {
            _parent = parent;
            _sceneMgr = mgr;
            _node = null;
            _regionID = regionID;
            _center = center;
            _boundingRadius = 0.0f;
            _currentLod = 0;
            _lightListUpdated = 0;
            _edgeList = null;
            _vertexProgramInUse = false;

            //First LOD mandatory, and always from 0
            _lodSquaredDistances.Add(0.0f);
        }

        ~Region() 
        {
            if (_node != null)
            {
                _node.Parent.RemoveChild(_node);
                _sceneMgr.DestroySceneNode(_node.Name);
                _node = null;
            }

            _lodBucketList.Clear();
            _shadowRenderables.Clear();
        
        }

        #endregion

        #region Properties
        
        #region Parent

        private StaticGeometry _parent;

        /// <summary>
        /// Parent static geometry
        /// </summary>
        protected StaticGeometry Parent
        {
            get
            {
                return _parent;
            }
        }

        #endregion

        #region ID

        private UInt32 _regionID;

        /// <summary>
        /// Unique identifier for the region
        /// </summary>
        protected UInt32 ID
        {
            get
            {
                return _regionID;
            }
        }

        #endregion

        #region Center

        private Vector3 _center;

        /// <summary>
        /// Center of the region
        /// </summary>
        protected Vector3 Center
        {
            get
            {
                return _center;
            }
        }

        #endregion

        #region MovableType

        public string MovableType
        {
            get
            {
                return "StaticGeometry";
            }
        }

        #endregion

        #region BoundingBox

        private AxisAlignedBox _aabb;

        /// <summary>
        /// Local AABB relative to region center
        /// </summary>
        public override AxisAlignedBox BoundingBox
        {
            get
            {
                return _aabb;
            }
        }

        #endregion

        #region BoundingRadius

        private Real _boundingRadius;

        /// <summary>
        /// Local bounding radius
        /// </summary>
        public override Real BoundingRadius
        {
            get
            {
                return _boundingRadius;
            }
        }

        #endregion

        #region Lights

        /// <summary>
        /// List of lights for this region
        /// </summary>
        private LightList _lightList;

        /// <summary>
        /// Shared set of lights for all GeometryBuckets
        /// </summary>
        protected LightList Lights
        {
            get
            {
                // Make sure we only update this once per frame no matter how many
                // times we're asked
                ulong frame = Root.Instance.CurrentFrameCount;
                if ( frame > _lightListUpdated )
                {
                    _lightList = _node.FindLights( _boundingRadius );
                    _lightListUpdated = frame;
                }
                return _lightList;
            }
        }

        #endregion

        #region EdgeList

        private EdgeData _edgeList;

        /// <summary>
        /// Edge list, used if stencil shadow casting is enabled
        /// </summary>
        protected EdgeData EdgeList
        {
            get
            {
                return _edgeList;
            }
        }

        #endregion

        #region SceneMgr

        private SceneManager _sceneMgr;

        /// <summary>
        /// Scene manager link
        /// </summary>
        protected SceneManager SceneMgr
        {
            get { return _sceneMgr; }
            set { _sceneMgr = value; }
        }

        #endregion

        #region Node

        private SceneNode _node;

        /// <summary>
        /// Scene node
        /// </summary>
        protected SceneNode Node
        {
            get { return _node; }
            set { _node = value; }
        }

        #endregion

        #region QueuedSubMeshes

        private QueuedSubMeshList _queuedSubMeshes;

        /// <summary>
        /// Local list of queued meshes
        /// </summary>
        protected SceneNode QueuedSubMeshes
        {
            get { return _queuedSubMeshes; }
            set { _queuedSubMeshes = value; }
        }

        #endregion

        #region LodSquaredDistances

        private ArrayList _lodSquaredDistances = new ArrayList();

        /// <summary>
        /// LOD distance (squared) as built up - use the max at each level
        /// </summary>
        protected SceneNode LodSquaredDistances
        {
            get { return _lodSquaredDistances; }
            set { _lodSquaredDistances = value; }
        }

        #endregion

        #region CurrentLod

        private ushort _currentLod;
        
        /// <summary>
        /// The current lod level, as determined from the last camera
        /// </summary>
        protected ushort CurrentLod
        {
            get{return _currentLod;}
            set{_currentLod=value;}
        }

        #endregion

        #region CamDistanceSquared

        private float _camDistanceSquared;
        
        /// <summary>
        /// Current camera distance, passed on to do material lod later 
        /// </summary>
        protected float CamDistanceSquared
        {
            get{return _camDistanceSquared;}
            set{_camDistanceSquared=value;}
        }

        #endregion

        #region LodBucketList

        private LODBucketList _lodBucketList;
        
        /// <summary>
        /// List of LOD buckets
        /// </summary>
        protected LODBucketList LodBucketList
        {
            get{return _lodBucketList;}
            set{_lodBucketList=value;}
        }

        #endregion

        #region LightListUpdated

        private ulong _lightListUpdated;

        /// <summary>
        /// The last frame that this list was updated in
        /// </summary>
        protected ulong LightListUpdated
        {
            get{return _lightListUpdated;}
            set{_lightListUpdated=value;}
        }

        #endregion

        #region BeyondFarDistance

        private bool _beyondFarDistance;
        
        protected bool BeyondFarDistance
        {
            get{return _beyondFarDistance;}
            set{_beyondFarDistance=value;}
        }

        #endregion

        #region ShadowRenderables

        private ShadowRenderableList _shadowRenderables;

        /// <summary>
        /// List of shadow renderables
        /// </summary>
        protected ShadowRenderableList ShadowRenderables
        {
            get { return _shadowRenderables; }
            set { _shadowRenderables = value; }
        }

        #endregion

        #region VertexProgrammInUse

        private bool _vertexProgramInUse;

        /// <summary>
        /// Is a vertex programm in use somwhere in this region?
        /// </summary>
        protected bool VertexProgrammInUse
        {
            get { return _vertexProgramInUse; }
            set { _vertexProgramInUse = value; }
        }

        #endregion

        #region IsVisible

        public override bool IsVisible
        {
            get
            {
                return base.IsVisible && !_beyondFarDistance;
            }
            set
            {
                base.IsVisible = false;
            }
        }

        #endregion

        #region TypeFlags

        protected UInt32 TypeFlags
        {
            get { }
        }

        #endregion

        #region LODIterator

        /// <summary>
        /// Get an iterator over the LODs in this region
        /// </summary>
        protected IEnumerator GetLODEnumerator
        {
            get
            {
                return LodBucketList.GetEnumerator();
            }
        }

        #endregion

        #endregion

        #region Public Methods

        /// <summary>
        /// Assign a queued mesh to this region, read for final build
        /// </summary>
        /// <param name="qsm"></param>
        public void Assign( QueuedSubMesh qsm )
        {
            _queuedSubMeshes.Add( qsm );
            // update lod distances
            ushort lodLevels = (ushort)qsm.submesh.Parent.LodLevelCount;
            if ( qsm.geometryLodList.Count != lodLevels )
                throw new AxiomException( "" );

            while ( _lodSquaredDistances.Count < lodLevels )
            {
                _lodSquaredDistances.Add( 0.0f );
            }
            // Make sure LOD levels are max of all at the requested level
            for ( ushort lod = 1; lod < lodLevels; ++lod )
            {
                MeshLodUsage meshLod = qsm.submesh.Parent.GetLodLevel( lod );
                _lodSquaredDistances[lod] = Math.Max( (float)_lodSquaredDistances[lod], meshLod.fromSquaredDepth );
            }

            // update bounds
            // Transform world bounds relative to our center
            AxisAlignedBox localBounds = new AxisAlignedBox( qsm.worldBounds.Minimum - _center, qsm.worldBounds.Maximum - _center );
            _aabb.Merge( localBounds );
            _boundingRadius = Math.Max( _boundingRadius, localBounds.Minimum.Length );
            _boundingRadius = Math.Max( _boundingRadius, localBounds.Maximum.Length );
        }

        /// <summary>
        /// Build this region
        /// </summary>
        /// <param name="stencilShadows"></param>
        public void Build( bool stencilShadows )
        {
            // Create a node
            _node = _sceneMgr.RootSceneNode.CreateChildSceneNode( name, _center );
            _node.AttachObject( this );
            // We need to create enough LOD buckets to deal with the highest LOD
            // we encountered in all the meshes queued
            for ( ushort lod = 0; lod < _lodSquaredDistances.Count; ++lod )
            {
                LODBucket lodBucket = new LODBucket( this, lod, (float)_lodSquaredDistances[lod] );
                LodBucketList.Add( lodBucket );
                // Now iterate over the meshes and assign to LODs
                // LOD bucket will pick the right LOD to use
                IEnumerator iter = _queuedSubMeshes.GetEnumerator();
                while ( iter.MoveNext() )
                {
                    QueuedSubMesh qsm = (QueuedSubMesh)iter.Current;
                    lodBucket.Assign( qsm, lod );
                }
                // now build
                lodBucket.Build( stencilShadows );
            }

            // Do we need to build an edge list?
            if ( stencilShadows )
            {
                EdgeListBuilder eb = new EdgeListBuilder();
                int vertexSet = 0;
                IEnumerator lodIterator = GetLODEnumerator();
                while ( lodIterator.MoveNext() )
                {
                    LODBucket lod = (LODBucket)lodIterator.Current;
                    IDictionaryEnumerator matIter = lod.GetMaterialEnumerator();
                    while ( matIter.MoveNext() )
                    {
                        MaterialBucket mat = (MaterialBucket)matIter.Value;
                        IEnumerator geoIter = mat.GetGeometryEnumerator();
                        // Check if we have vertex programs here
                        Technique t = mat.Material.GetBestTechnique();
                        if ( null != t )
                        {
                            Pass p = t.GetPass( 0 );
                            if ( null != p )
                            {
                                if ( p.HasVertexProgram )
                                {
                                    _vertexProgramInUse = true;
                                }
                            }
                        }

                        while ( geoIter.MoveNext() )
                        {
                            GeometryBucket geom = (GeometryBucket)geoIter.Current;

                            // Check we're dealing with 16-bit indexes here
                            // Since stencil shadows can only deal with 16-bit
                            // More than that and stencil is probably too CPU-heavy
                            // in any case
                            if ( geom.IndexData.indexBuffer.Type != IndexType.Size16 )
                                throw new AxiomException( "Only 16-bit indexes allowed when using stencil shadows" );
                            eb.AddVertexData( geom.VertexData );
                            eb.AddIndexData( geom.IndexData , vertexSet++);
                        }
                    }
                }
                _edgeList = eb.Build();
            }
        }

        public override void NotifyCurrentCamera(Camera cam)
        {
            // Determine active lod
            Vector3 diff = cam.DerivedPosition - _center;
            Real squaredDepth = diff.LengthSquared;

            //Determin whether to still render
            Real renderingDist = _parent.RenderingDistance;

            if (renderingDist > 0)
            {
                //Max distance to still render
                Real maxDist = renderingDist + _boundingRadius;
                if (squaredDepth > Math.Sqrt(maxDist))
                {
                    _beyondFarDistance = true;
                    return;
                }
            }

            _beyondFarDistance = false;

            // Distance from the edge of the bounding sphere
            _camDistanceSquared = squaredDepth - _boundingRadius * _boundingRadius;
            // Clamp to 0
            _camDistanceSquared = Math.Max(0.0f, _camDistanceSquared);

            //Determin active LOD
            _currentLod = (ushort)(_lodSquaredDistances.Count - 1);
            for (ushort i = 0; i < _lodSquaredDistances.Count; ++i)
            {
                if ((float)_lodSquaredDistances[i] > _camDistanceSquared)
                {
                    _currentLod = (ushort)(i - 1);
                    break;
                }
            }
        }

        public override void UpdateRenderQueue( RenderQueue queue )
        {
            LODBucket lodBucket = (LODBucket)LodBucketList[_currentLod];
            lodBucket.AddRenderables( queue, renderQueueID, _camDistanceSquared );
        }

        public IEnumerator GetShadowVolumeRenderableIterator(ShadowTechnique shadowTechnique, Light light, HardwareIndexBuffer indexBuffer, bool extrudeVertices, float extrusionDistance)
        {
            GetShadowVolumeRenderableIterator(shadowTechnique, light, indexBuffer, extrudeVertices, extrusionDistance,0);
        }

        public IEnumerator GetShadowVolumeRenderableIterator( ShadowTechnique shadowTechnique, Light light, HardwareIndexBuffer indexBuffer, bool extrudeVertices, float extrusionDistance, ulong flags )
        {
            if (indexBuffer == null)
                throw new ApplicationException("Only external index buffers are supported right now");
            if (indexBuffer.Type != IndexType.Size16)
                throw new ApplicationException("Only 16-bit indexes supported for now");

            Vector4 lightPos = light.GetAs4DVector();
            Matrix4 world2Obj = _node.FullTransform.Inverse();
            lightPos = world2Obj * lightPos;

            //We need to search the edge listfor silhouette edges
            if (_edgeList == null)
                throw new ApplicationException("You enabled stencil shadows after the build process");

            //Init shadow renderable list if required
            bool init = _shadowRenderables.Count == 0;

            RegionShadowRenderable esr=null;
            for (int i = 0; i < _edgeList.edgeGroups.Count; i++)
            {
                if (init)
                {
                    _shadowRenderables.Add(new RegionShadowRenderable(this, indexBuffer, _edgeList.edgeGroups[i].vertexData, _vertexProgramInUse || !extrudeVertices));
                }

                //Get shadow renderable
                esr = _shadowRenderables[i];

                HardwareVertexBuffer esrPositionBuffer = esr.PositionBuffer;

                //Extrude vertices in buffer if required
                if (extrudeVertices)
                {
                    ExtrudeVertices(esrPositionBuffer, _edgeList.edgeGroups[i].vertexData.vertexCount, lightPos, extrusionDistance);
                }

            }
    
            //Calc triangle light facing
            UpdateEdgeListLightFacing(_edgeList, lightPos);

            //Generate indexes and update renderables
            GenerateShadowVolume(_edgeList, indexBuffer, light, _shadowRenderables, flags);

            return _shadowRenderables.GetEnumerator();
        }

        public void Dump( TextWriter output )
        {
            output.WriteLine( "Region {0}", _regionID );
            output.WriteLine( "--------------------------" );
            output.WriteLine( "Center: {0}", _center );
            output.WriteLine( "Local AABB: {0}", _aabb );
            output.WriteLine( "Bounding radius: {0}", _boundingRadius );
            output.WriteLine( "Number of LODs: {0}", LodBucketList.Count );
            IEnumerator iter = LodBucketList.GetEnumerator();
            while ( iter.MoveNext() )
            {
                LODBucket lodBucket = (LODBucket)iter.Current;
                lodBucket.Dump( output );
            }
            output.WriteLine( "--------------------------" );
        }
        #endregion
    }

}
