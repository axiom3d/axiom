#region Namespace Declarations

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

using Axiom.MathLib;
using Axiom.MathLib.Collections;

#endregion Namespace Declarations

namespace Axiom
{

    #region Strongly Typed Collections

    //TODO Modify for Generics
    public class QueuedGeometryList : ArrayList
    {
    }
    public class GeometryBucketList : ArrayList
    {
    }
    public class CurrentGeometryMap : Hashtable
    {
    }

    public class QueuedSubMeshList : List<QueuedSubMesh>
    {
        public void InsertAtEnd( QueuedSubMesh mesh )
        {
            //Insert(Count, mesh);
            Add( mesh );
        }
    }

    public class OptimisedSubMeshGeometryList : List<OptimisedSubMeshGeometry>
    {
        public void InsertAtEnd( OptimisedSubMeshGeometry geom )
        {
            Add( geom );
        }
    }

    public class SubMeshLodGeometryLinkList : List<SubMeshLodGeometryLink>
    {
    }

    public class SubMeshGeometryLookup : Dictionary<SubMesh, SubMeshLodGeometryLinkList>
    {
    }

    public class LODBucketList : ArrayList
    {
    }
    public class MaterialBucketMap : Hashtable
    {
    }
    public class RegionMap : Hashtable
    {
    }

    public class IndexRemap : List< Pair<int> >
    {
    }
    #endregion

    #region Structs

    public struct OptimisedSubMeshGeometry
    {
        public VertexData vertexData;
        public IndexData indexData;
    }

    public struct SubMeshLodGeometryLink
    {
        public VertexData vertexData;
        public IndexData indexData;
    }

    public struct QueuedSubMesh
    {
        public SubMesh submesh;
        public SubMeshLodGeometryLinkList geometryLodList;
        public string materialName;
        public Vector3 position;
        public Quaternion orientation;
        public Vector3 scale;
        public AxisAlignedBox worldBounds;
    }

    public struct QueuedGeometry
    {
        public SubMeshLodGeometryLink geometry;
        public Vector3 position;
        public Quaternion orientation;
        public Vector3 scale;
    }
    #endregion

    /// <summary>
    /// Pre-transforms and batches up meshes for efficient use as static geometry in a scene.
    /// </summary>
    /// <remarks>
    /// Modern graphics cards (GPUs) prefer to receive geometry in large
    /// batches. It is orders of magnitude faster to render 10 batches
    /// of 10,000 triangles than it is to render 10,000 batches of 10 
    /// triangles, even though both result in the same number of on-screen
    /// triangles.
    /// <br>
    /// Therefore it is important when you are rendering a lot of geometry to 
    /// batch things up into as few rendering calls as possible. This
    /// class allows you to build a batched object from a series of entities 
    /// in order to benefit from this behaviour.
    /// Batching has implications of it's own though:
    /// <ul>
    /// <li> Batched geometry cannot be subdivided; that means that the whole
    /// 	group will be displayed, or none of it will. This obivously has
    /// 	culling issues.
    /// <li> A single world transform must apply to the entire batch. Therefore
    /// 	once you have batched things, you can't move them around relative to
    /// 	each other. That's why this class is most useful when dealing with 
    /// 	static geometry (hence the name). In addition, geometry is 
    /// 	effectively duplicated, so if you add 3 entities based on the same 
    /// 	mesh in different positions, they will use 3 times the geometry 
    /// 	space than the movable version (which re-uses the same geometry). 
    /// 	So you trade memory	and flexibility of movement for pure speed when
    /// 	using this class.
    /// <li> A single material must apply for each batch. In fact this class 
    /// 	allows you to use multiple materials, but you should be aware that 
    /// 	internally this means that there is one batch per material. 
    /// 	Therefore you won't gain as much benefit from the batching if you 
    /// 	use many different materials; try to keep the number down.
    /// </ul>
    /// <br>
    /// In order to retain some sort of culling, this class will batch up 
    /// meshes in localised regions. The size and shape of these blocks is
    /// controlled by the SceneManager which contructs this object, since it
    /// makes sense to batch things up in the most appropriate way given the 
    /// existing partitioning of the scene. 
    /// <br>
    /// The LOD settings of both the Mesh and the Materials used in 
    /// constructing this static geometry will be respected. This means that 
    /// if you use meshes/materials which have LOD, batches in the distance 
    /// will have a lower polygon count or material detail to those in the 
    /// foreground. Since each mesh might have different LOD distances, during 
    /// build the furthest distance at each LOD level from all meshes  
    /// in that region is used. This means all the LOD levels change at the 
    /// same time, but at the furthest distance of any of them (so quality is 
    /// not degraded). Be aware that using Mesh LOD in this class will 
    /// further increase the memory required. Only generated LOD
    /// is supported for meshes.
    /// <br>
    /// There are 2 ways you can add geometry to this class; you can add
    /// Entity objects directly with predetermined positions, scales and 
    /// orientations, or you can add an entire SceneNode and it's subtree, 
    /// including all the objects attached to it. Once you've added everthing
    /// you need to, you have to call build() the fix the geometry in place. 
    /// <br>
    /// This class is not a replacement for world geometry (see 
    /// SceneManager.WorldGeometry). The single most efficient way to 
    /// render large amounts of static geometry is to use a SceneManager which 
    /// is specialised for dealing with that particular world structure. 
    /// However, this class does provide you with a good 'halfway house'
    /// between generalised movable geometry (Entity) which works with all 
    /// SceneManagers but isn't efficient when using very large numbers, and 
    /// highly specialised world geometry which is extremely fast but not 
    /// generic and typically requires custom world editors.
    /// <br>
    /// You should not construct instances of this class directly; instead, call
    /// SceneManager.CreateStaticGeometry, which gives the SceneManager the 
    /// option of providing you with a specialised version of this class if it
    /// wishes, and also handles the memory management for you like other 
    /// classes.
    /// </remarks>
    /// Port started by jwace81
    /// OGRE Header File: http://cvs.sourceforge.net/viewcvs.py/ogre/ogrenew/OgreMain/include/OgreStaticGeometry.h?rev=1.11.2.2&view=auto
    /// OGRE Source File: http://cvs.sourceforge.net/viewcvs.py/ogre/ogrenew/OgreMain/src/OgreStaticGeometry.cpp?rev=1.16.2.11&view=auto
    /// Port continued by arilou aka Serge Lobko-Lobanovsky
    /// Port completed by Borrillis
    /// <ogre name="StaticGeometry">
    ///     <file name="OgreStaticGeometry.h"   revision="1.11.2.2" lastUpdated="02/06/2006" lastUpdatedBy="Borrillis" />
    ///     <file name="OgreStaticGeometry.cpp" revision="1.16.2.11" lastUpdated="02/06/2006" lastUpdatedBy="Borrillis" />
    /// </ogre> 
    public class StaticGeometry
    {
        #region Fields and Properties

        protected SceneManager owner;
        protected string name;
        protected bool built;
        protected float upperDistance;
        protected float squaredUpperDistance;
        protected bool castShadows;
        protected bool visible = true;
        protected RenderQueueGroupID renderQueueID = RenderQueueGroupID.Main;
        protected bool renderQueueIDSet = false;
        protected QueuedSubMeshList queuedSubMeshes = new QueuedSubMeshList();
        protected OptimisedSubMeshGeometryList optimisedSubMeshGeometryList = new OptimisedSubMeshGeometryList();
        protected SubMeshGeometryLookup subMeshGeometryLookup = new SubMeshGeometryLookup();
        protected RegionMap regionMap = new RegionMap();


        #region RegionDimensions Property

        protected Vector3 halfRegionDimensions;
        protected Vector3 regionDimensions;
        /// <summary>
        /// Gets/Sets the size of a single region of geometry.
        /// </summary>
        /// <remarks>
        /// This method allows you to configure the physical world size of 
        ///	each region, so you can balance culling against batch size. Entities
        ///	will be fitted within the batch they most closely fit, and the 
        ///	eventual bounds of each batch may well be slightly larger than this
        ///	if they overlap a little. The default is Vector3(1000, 1000, 1000).
        /// 
        /// Must be called before 'build'.
        /// </remarks>
        public Vector3 RegionDimensions
        {
            get
            {
                return regionDimensions;
            }
            set
            {
                regionDimensions = value;
                halfRegionDimensions = value * 0.5f;
            }
        }

        #endregion RegionDimensions Property


        #region Origin Property

        protected Vector3 origin;

        /// <summary>
        /// Gets/Sets the origin of the geometry.
        /// </summary>
        /// <remarks>
        /// This method allows you to configure the world centre of the geometry,
        ///	thus the place which all regions surround. You probably don't need 
        ///	to mess with this unless you have a seriously large world, since the
        ///	default set up can handle an area 1024 * mRegionDimensions, and 
        ///	the sparseness of population is no issue when it comes to rendering.
        ///	The default is Vector3(0,0,0).
        /// 
        /// Must be called before 'build'
        /// </remarks>
        public Vector3 Origin
        {
            get
            {
                return origin;
            }
            set
            {
                origin = value;
            }
        }

        #endregion Origin Property

        public float SquaredRenderingDistance
        {
            get
            {
                throw new NotSupportedException();
            }
        }

        #endregion Fields and Properties

        #region Constructors & Destructors

        /// <summary>
        /// Constructs a new instance of StaticGeometry
        /// </summary>
        /// <param name="owner">Scene manager</param>
        /// <param name="name">unique name</param>
        public StaticGeometry( SceneManager owner, string name )
        {
            this.owner = owner;
            this.name = name;
            this.built = false;
            this.upperDistance = 0.0f;
            this.squaredUpperDistance = 0.0f;
            this.castShadows = false;
            this.regionDimensions = new Vector3( 1000, 1000, 1000 );
            this.halfRegionDimensions = new Vector3( 500, 500, 500 );
            this.origin = Vector3.Zero;
            this.visible = true;
            this.renderQueueID = RenderQueueGroupID.Main;
            this.renderQueueIDSet = false;
        }

        ~StaticGeometry()
        {
            //reset();
        }

        #endregion Constructors & Descructors

        #region AddEntity
        /// <summary>Adds an Entity to the static geometry</summary>
        /// <remarks>This method takes an existing Entity and adds its details to 
        /// the list of	elements to include when building. Note that the Entity
        /// itself is not copied or referenced in this method; an Entity is 
        ///	passed simply so that you can change the materials of attached 
        ///	SubEntity objects if you want. You can add the same Entity 
        ///	instance multiple times with different material settings 
        ///	completely safely, and destroy the Entity before destroying 
        ///	this StaticGeometry if you like. The Entity passed in is simply 
        ///	used as a definition.
        /// 
        /// Must be called before 'build'.
        /// </remarks>
        /// <param name="ent">The Entity to use as a definition (the Mesh and Materials 
        /// referenced will be recorded for the build call)</param>
        /// <param name="position">The world position at which to add this Entity</param>
        public virtual void AddEntity( Entity ent, Vector3 position )
        {
            AddEntity( ent, position, Quaternion.Identity, Vector3.UnitScale );
        }

        /// <summary>
        /// Overloaded. Adds an Entity to the static geometry.
        /// </summary>
        /// <param name="ent"></param>
        /// <param name="position"></param>
        /// <param name="orientation"></param>
        public virtual void AddEntity( Entity ent, Vector3 position,
            Quaternion orientation )
        {
            AddEntity( ent, position, orientation, Vector3.UnitScale );
        }

        /// <summary>
        /// Overloaded. Adds an Entity to the static geometry.
        /// </summary>
        /// <param name="ent"></param>
        /// <param name="position"></param>
        /// <param name="orientation"></param>
        /// <param name="scale"></param>
        public virtual void AddEntity( Entity ent, Vector3 position, Quaternion orientation, Vector3 scale )
        {
            Mesh msh = ent.Mesh;
            // Validate
            if ( msh.IsLodManual )
            {
                LogManager.Instance.Write(
                    "WARNING (StaticGeometry): Manual LOD is not supported. " +
                    "Using only highest LOD level for mesh {0}", msh.Name );
            }

            AxisAlignedBox sharedWorldBounds = new AxisAlignedBox();

            // queue this entities submeshes and choice of material
            // also build the lists of geometry to be used for the source of lods
            for ( int i = 0; i < ent.SubEntityCount; ++i )
            {
                SubEntity se = ent.SubEntities[ i ];
                QueuedSubMesh q = new QueuedSubMesh();

                // Get the geometry for this SubMesh
                q.submesh = se.SubMesh;
                q.geometryLodList = determineGeometry( q.submesh );
                q.materialName = se.MaterialName;
                q.orientation = orientation;
                q.position = position;
                q.scale = scale;
                // Determine the bounds based on the highest LOD
                q.worldBounds = calculateBounds(
                    q.geometryLodList[ 0 ].vertexData,
                        position, orientation, scale );

                queuedSubMeshes.InsertAtEnd( q );
            }

        }
        #endregion

        #region private helper methods

        private SubMeshLodGeometryLinkList determineGeometry( SubMesh sm )
        {
            // First, determine if we've already seen this submesh before
            if ( subMeshGeometryLookup.ContainsKey( sm ) )
                return subMeshGeometryLookup[ sm ];

            // Otherwise, we have to create a new one

            SubMeshLodGeometryLinkList lodList = new SubMeshLodGeometryLinkList();
            subMeshGeometryLookup.Add( sm, lodList );

            int numLods = sm.Parent.IsLodManual ? 1 : sm.Parent.LodLevelCount;

            for ( int lod = 0; lod < numLods; ++lod )
            {
                SubMeshLodGeometryLink geomLink = lodList[ lod ];
                IndexData lodIndexData;

                if ( lod == 0 )
                    lodIndexData = sm.indexData;
                else
                    lodIndexData = (IndexData)sm.lodFaceList[ lod - 1 ];

                // Can use the original mesh geometry?
                if (  false ) //sm.UseSharedVertices )
                {
                    if ( sm.Parent.SubMeshCount == 1 )
                    {
                        // Ok, this is actually our own anyway
                        geomLink.vertexData = sm.Parent.SharedVertexData;
                        geomLink.indexData = lodIndexData;
                    }
                    else
                    {
                        // We have to split it
                        splitGeometry( sm.Parent.SharedVertexData,
                            lodIndexData, geomLink );
                    }
                }
                else
                {
                    if ( lod == 0 )
                    {
                        // Ok, we can use the existing geometry; should be in full 
                        // use by just this SubMesh
                        geomLink.vertexData = sm.vertexData;
                        geomLink.indexData = sm.indexData;
                    }
                    else
                    {
                        // We have to split it
                        splitGeometry( sm.vertexData, lodIndexData, geomLink );
                    }
                }

                Debug.Assert( geomLink.vertexData.vertexStart == 0,
                    "Cannot use vertexStart > 0 on indexed geometry due to rendersystem incompatibilities - see the docs!" );
            }


            return lodList;
        }

        private unsafe void buildIndexRemap( IntPtr pBuffer, int numIndexes, IndexRemap remap )
        {
            remap.Clear();
            for ( int i = 0; i < numIndexes; ++i )
            {
                // use insert since duplicates are silently discarded
                //remap.insert(IndexRemap::value_type(*pBuffer++, remap.size()));
                int* val = (int*)pBuffer.ToInt32();
                remap.Add( new Pair<int>( *val, remap.Count ) );
                // this will have mapped oldindex -> new index IF oldindex
                // wasn't already there
            }
        }

        private unsafe void splitGeometry( VertexData vd, IndexData id, SubMeshLodGeometryLink targetGeomLink )
        {
            // Firstly we need to scan to see how many vertices are being used 
            // and while we're at it, build the remap we can use later
            bool use32bitIndexes = ( id.indexBuffer.Type == IndexType.Size32 );

            ushort* p16;
            uint* p32;
            IndexRemap indexRemap = new IndexRemap();

            if ( use32bitIndexes )
            {
                IntPtr p32ptr = id.indexBuffer.Lock( id.indexStart, id.indexCount, BufferLocking.ReadOnly );

                buildIndexRemap( p32ptr, id.indexCount, indexRemap );
                id.indexBuffer.Unlock();
            }
            else
            {
                IntPtr p16ptr = id.indexBuffer.Lock( id.indexStart, id.indexCount, BufferLocking.ReadOnly );
                buildIndexRemap( p16ptr, id.indexCount, indexRemap );
                id.indexBuffer.Unlock();
            }

            if ( indexRemap.Count == vd.vertexCount )
            {
                // ha, complete usage after all
                targetGeomLink.vertexData = vd;
                targetGeomLink.indexData = id;

                return;
            }


            // Create the new vertex data records
            targetGeomLink.vertexData = vd.Clone( false );
            // Convenience
            VertexData newvd = targetGeomLink.vertexData;
            IndexData newid = targetGeomLink.indexData;
            // Update the vertex count
            newvd.vertexCount = indexRemap.Count;

            int numvbufs = vd.vertexBufferBinding.BindingCount;
            // Copy buffers from old to new
            for ( short b = 0; b < numvbufs; ++b )
            {
                // Lock old buffer
                HardwareVertexBuffer oldBuf = vd.vertexBufferBinding.GetBuffer( b );
                // Create new buffer
                HardwareVertexBuffer newBuf = HardwareBufferManager.Instance.CreateVertexBuffer( oldBuf.VertexSize, indexRemap.Count, BufferUsage.Static );
                // rebind
                newvd.vertexBufferBinding.SetBinding( b, newBuf );

                // Copy all the elements of the buffer across, by iterating over
                // the IndexRemap which describes how to move the old vertices 
                // to the new ones. By nature of the map the remap is in order of
                // indexes in the old buffer, but note that we're not guaranteed to
                // address every vertex (which is kinda why we're here)
                IntPtr pSrcBasePtr = oldBuf.Lock( BufferLocking.ReadOnly );
                IntPtr pDstBasePtr = newBuf.Lock( BufferLocking.Discard );
                int vertexSize = oldBuf.VertexSize;
                // Buffers should be the same size
                Debug.Assert( vertexSize == newBuf.VertexSize );

                foreach ( Pair<int> r in indexRemap )
                {
                    //assert (r->first < oldBuf->getNumVertices());
                    //assert (r->second < newBuf->getNumVertices());

                    IntPtr pSrc = (IntPtr)( pSrcBasePtr.ToInt32() + r.first * vertexSize );
                    IntPtr pDst = (IntPtr)( pDstBasePtr.ToInt32() + r.second * vertexSize );

                    Memory.Copy( pSrc, pDst, vertexSize );
                }

                // unlock
                oldBuf.Unlock();
                newBuf.Unlock();
            }

            // Now create a new index buffer
            HardwareIndexBuffer ibuf = HardwareBufferManager.Instance.CreateIndexBuffer( id.indexBuffer.Type, id.indexCount, BufferUsage.Static );

            if ( use32bitIndexes )
            {
                IntPtr pSrc32Ptr, pDst32Ptr;

                pSrc32Ptr = id.indexBuffer.Lock( id.indexStart, id.indexCount, BufferLocking.ReadOnly );
                pDst32Ptr = ibuf.Lock( BufferLocking.Discard );
                //remapIndexes( pSrc32Ptr, pDst32Ptr, indexRemap, id.indexCount );
                id.indexBuffer.Unlock();
                ibuf.Unlock();
            }
            else
            {
                IntPtr pSrc16Ptr, pDst16Ptr;
                pSrc16Ptr = id.indexBuffer.Lock( id.indexStart, id.indexCount, BufferLocking.ReadOnly );
                pDst16Ptr = ibuf.Lock( BufferLocking.Discard );
                //remapIndexes( pSrc16Ptr, pDst16Ptr, indexRemap, id.indexCount );
                id.indexBuffer.Unlock();
                ibuf.Unlock();
            }

            targetGeomLink.indexData = new IndexData();
            targetGeomLink.indexData.indexStart = 0;
            targetGeomLink.indexData.indexCount = id.indexCount;
            targetGeomLink.indexData.indexBuffer = ibuf;

            // Store optimised geometry for deallocation later
            OptimisedSubMeshGeometry optGeom = new OptimisedSubMeshGeometry();
            optGeom.indexData = targetGeomLink.indexData;
            optGeom.vertexData = targetGeomLink.vertexData;
            optimisedSubMeshGeometryList.InsertAtEnd( optGeom );
        }

        private AxisAlignedBox calculateBounds( VertexData vertexData, Vector3 position, Quaternion orientation, Vector3 scale )
        {
            VertexElement posElem = vertexData.vertexDeclaration.FindElementBySemantic( VertexElementSemantic.Position );
            HardwareVertexBuffer vbuf = vertexData.vertexBufferBinding.GetBuffer( posElem.Source );
            IntPtr vertexPtr = vbuf.Lock( BufferLocking.ReadOnly );

            Vector3 min = Vector3.Zero, max = Vector3.Zero;
            unsafe
            {
                float* pFloat = (float*)vertexPtr.ToPointer();

                bool first = true;

                for ( int j = 0; j < vertexData.vertexCount; ++j )
                {
                    //posElem.baseVertexPointerToElement(vertexPtr, &pFloat);
                    ;

                    Vector3 pt = Vector3.Zero;

                    pt.x = ( *pFloat++ );
                    pt.y = ( *pFloat++ );
                    pt.z = ( *pFloat++ );

                    // Transform to world (scale, rotate, translate)
                    pt = ( orientation * ( pt * scale ) ) + position;
                    if ( first )
                    {
                        min = max = pt;
                        first = false;
                    }
                    else
                    {
                        min.Floor( pt );
                        max.Ceil( pt );
                    }

                    pFloat += vbuf.VertexSize;
                }
            }
            vbuf.Unlock();

            return new AxisAlignedBox( min, max );
        }

        #endregion
    }
}
