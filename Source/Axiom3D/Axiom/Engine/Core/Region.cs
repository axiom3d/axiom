using System;
using System.Collections;
using System.Text;
using System.IO;

using Axiom.MathLib;

namespace Axiom
{

    public class Region : MovableObject
    {
        #region Inner Classes
        public class RegionShadowRenderable : ShadowRenderable
        {
            protected Region parent;
            protected HardwareVertexBuffer positionBuffer;
            protected HardwareVertexBuffer wBuffer;

            public RegionShadowRenderable( Region parent, HardwareIndexBuffer indexBuffer, VertexData vertexData, bool createSeparateLightCap, bool isLightCap )
            {
                throw new NotImplementedException();
            }

            public RegionShadowRenderable( Region parent, HardwareIndexBuffer indexBuffer, VertexData vertexData, bool createSeparateLightCap )
                : this( parent, indexBuffer, vertexData, createSeparateLightCap, false )
            {
            }

            public HardwareVertexBuffer PositionBuffer
            {
                get
                {
                    return positionBuffer;
                }
            }

            public HardwareVertexBuffer WBuffer
            {
                get
                {
                    return wBuffer;
                }
            }

            public override void GetWorldTransforms( Axiom.MathLib.Matrix4[] matrices )
            {
                throw new NotImplementedException();
            }

            public override Quaternion WorldOrientation
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public override Vector3 WorldPosition
            {
                get
                {
                    throw new NotImplementedException();
                }
            }
        }

        #endregion

        #region Fields and Properties
        protected StaticGeometry parent;
        protected SceneManager sceneMgr;
        protected SceneNode node;
        protected QueuedSubMeshList queuedSubMeshes;
        protected UInt32 regionID;
        protected Vector3 center;
        protected ArrayList lodSquaredDistances = new ArrayList();
        protected AxisAlignedBox aabb;
        protected float boundingRadius;
        protected ushort currentLod;
        protected float camDistanceSquared;
        protected LODBucketList LodBucketList;
        protected LightList lightList;
        protected ulong lightListUpdated;
        protected bool beyondFarDistance;
        protected EdgeData edgeList;
        protected ShadowRenderableList shadowRenderables;
        protected bool vertexProgramInUse;

        public StaticGeometry Parent
        {
            get
            {
                return parent;
            }
        }


        public UInt32 ID
        {
            get
            {
                return regionID;
            }
        }

        public Vector3 Center
        {
            get
            {
                return center;
            }
        }

        public string MovableType
        {
            get
            {
                return "StaticGeometry";
            }
        }

        public override AxisAlignedBox BoundingBox
        {
            get
            {
                return aabb;
            }
        }

        public override float BoundingRadius
        {
            get
            {
                return boundingRadius;
            }
        }
        public LightList Lights
        {
            get
            {
                // Make sure we only update this once per frame no matter how many
                // times we're asked
                ulong frame = Root.Instance.CurrentFrameCount;
                if ( frame > lightListUpdated )
                {
                    lightList = node.FindLights( boundingRadius );
                    lightListUpdated = frame;
                }
                return lightList;
            }
        }

        public EdgeData EdgeList
        {
            get
            {
                return edgeList;
            }
        }
        #endregion

        #region Constructors

        public Region( StaticGeometry parent, string name, SceneManager mgr, UInt32 regionID, Vector3 center )
        {
            this.parent = parent;
            this.name = name;
            this.sceneMgr = mgr;
            this.regionID = regionID;
            this.center = center;
        }

        #endregion

        #region Public Methods
        public void Assign( QueuedSubMesh qsm )
        {
            queuedSubMeshes.Add( qsm );
            // update lod distances
            ushort lodLevels = (ushort)qsm.submesh.Parent.LodLevelCount;
            if ( qsm.geometryLodList.Count != lodLevels )
                throw new AxiomException( "" );

            while ( lodSquaredDistances.Count < lodLevels )
            {
                lodSquaredDistances.Add( 0.0f );
            }
            // Make sure LOD levels are max of all at the requested level
            for ( ushort lod = 1; lod < lodLevels; ++lod )
            {
                MeshLodUsage meshLod = qsm.submesh.Parent.GetLodLevel( lod );
                lodSquaredDistances[lod] = Math.Max( (float)lodSquaredDistances[lod], meshLod.fromSquaredDepth );
            }

            // update bounds
            // Transform world bounds relative to our center
            AxisAlignedBox localBounds = new AxisAlignedBox( qsm.worldBounds.Minimum - center, qsm.worldBounds.Maximum - center );
            aabb.Merge( localBounds );
            boundingRadius = Math.Max( boundingRadius, localBounds.Minimum.Length );
            boundingRadius = Math.Max( boundingRadius, localBounds.Maximum.Length );
        }

        public void Build( bool stencilShadows )
        {
            // Create a node
            node = sceneMgr.RootSceneNode.CreateChildSceneNode( name, center );
            node.AttachObject( this );
            // We need to create enough LOD buckets to deal with the highest LOD
            // we encountered in all the meshes queued
            for ( ushort lod = 0; lod < lodSquaredDistances.Count; ++lod )
            {
                LODBucket lodBucket = new LODBucket( this, lod, (float)lodSquaredDistances[lod] );
                LodBucketList.Add( lodBucket );
                // Now iterate over the meshes and assign to LODs
                // LOD bucket will pick the right LOD to use
                IEnumerator iter = queuedSubMeshes.GetEnumerator();
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
                                    vertexProgramInUse = true;
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
                            eb.AddIndexData( geom.IndexData );
                        }
                    }
                }
                edgeList = eb.Build();
            }
        }

        public override void NotifyCurrentCamera( Camera cam )
        {
            // Determine active lod
            Vector3 diff = cam.DerivedPosition - center;
            // Distance from the edge of the bounding sphere
            camDistanceSquared = diff.LengthSquared - boundingRadius * boundingRadius;
            // Clamp to 0
            camDistanceSquared = Math.Max( 0.0f, camDistanceSquared );

            float maxDist = parent.SquaredRenderingDistance;
            if ( camDistanceSquared > maxDist )
            {
                beyondFarDistance = true;
            }
            else
            {
                beyondFarDistance = false;

                currentLod = (ushort)( lodSquaredDistances.Count - 1 );
                for ( ushort i = 0; i < lodSquaredDistances.Count; ++i )
                {
                    if ( (float)lodSquaredDistances[i] > camDistanceSquared )
                    {
                        currentLod = (ushort)( i - 1 );
                        break;
                    }
                }
            }
        }



        public override void UpdateRenderQueue( RenderQueue queue )
        {
            LODBucket lodBucket = (LODBucket)LodBucketList[currentLod];
            lodBucket.AddRenderables( queue, renderQueueID, camDistanceSquared );
        }

        public override bool IsVisible
        {
            get
            {
                return isVisible && !beyondFarDistance;
            }
            set
            {
                isVisible = false;
            }
        }

        public IEnumerator GetLODEnumerator()
        {
            return LodBucketList.GetEnumerator();
        }


        public IEnumerator GetShadowVolumeRenderableIterator( ShadowTechnique shadowTechnique, Light light, HardwareIndexBuffer indexBuffer, bool extrudeVertices, float extrusionDistance, ulong flags )
        {
            // TODO Port this from Ogre
            throw new NotImplementedException();
        }

        public void Dump( TextWriter output )
        {
            output.WriteLine( "Region {0}", regionID );
            output.WriteLine( "--------------------------" );
            output.WriteLine( "Center: {0}", center );
            output.WriteLine( "Local AABB: {0}", aabb );
            output.WriteLine( "Bounding radius: {0}", boundingRadius );
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
