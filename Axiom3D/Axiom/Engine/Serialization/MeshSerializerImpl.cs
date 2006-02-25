using System;
using System.IO;

using Axiom.MathLib;

namespace Axiom
{
    /// <summary>
    /// Summary description for MeshSerializerImpl.
    /// </summary>
    public class MeshSerializerImpl : Serializer
    {
        #region Fields

        /// <summary>
        ///		Target mesh for importing/exporting.
        /// </summary>
        protected Mesh mesh;
        /// <summary>
        ///		Is this mesh animated with a skeleton?
        /// </summary>
        protected bool isSkeletallyAnimated;

        #endregion Fields

        #region Constructor

        /// <summary>
        ///		Default constructor.
        /// </summary>
        public MeshSerializerImpl()
        {
            version = "[MeshSerializer_v1.30]";
        }

        #endregion Constructor

        #region Methods

        /// <summary>
        ///		Exports a mesh to the file specified.
        /// </summary>
        /// <remarks>
        ///		This method takes an externally created Mesh object, and exports both it
        ///		to a .mesh file.
        /// </remarks>
        /// <param name="mesh">Reference to the mesh to export.</param>
        /// <param name="fileName">The destination file name.</param>
        public void ExportMesh( Mesh mesh, string fileName )
        {
        }

        /// <summary>
        ///		Imports mesh data from a .mesh file.
        /// </summary>
        /// <param name="stream">A stream containing the .mesh data.</param>
        /// <param name="mesh">Mesh to populate with the data.</param>
        public void ImportMesh( Stream stream, Mesh mesh )
        {
            this.mesh = mesh;

            BinaryReader reader = new BinaryReader( stream );

            // check header
            ReadFileHeader( reader );

            MeshChunkID chunkID = 0;

            // read until the end
            while ( !IsEOF( reader ) )
            {
                chunkID = ReadChunk( reader );

                if ( chunkID == MeshChunkID.Mesh )
                {
                    ReadMesh( reader );
                }
            }
        }

        #region Protected

        protected virtual void ReadSubMeshNameTable( BinaryReader reader )
        {
            if ( !IsEOF( reader ) )
            {
                MeshChunkID chunkID = ReadChunk( reader );

                while ( !IsEOF( reader ) && ( chunkID == MeshChunkID.SubMeshNameTableElement ) )
                {
                    // i'm not bothering with the name table business here, I don't see what the purpose is
                    // since we can simply name the submesh.  it appears this section always comes after all submeshes
                    // are read, so it should be safe
                    short index = ReadShort( reader );
                    string name = ReadString( reader );

                    SubMesh sub = mesh.GetSubMesh( index );

                    if ( sub != null )
                    {
                        sub.name = name;
                    }

                    // If we're not end of file get the next chunk ID
                    if ( !IsEOF( reader ) )
                    {
                        chunkID = ReadChunk( reader );
                    }
                }

                // backpedal to the start of the chunk
                if ( !IsEOF( reader ) )
                {
                    Seek( reader, -ChunkOverheadSize );
                }
            }
        }

        protected virtual void ReadMesh( BinaryReader reader )
        {
            MeshChunkID chunkID;

            // Never automatically build edge lists for this version
            // expect them in the file or not at all
            mesh.AutoBuildEdgeLists = false;

            // is this mesh animated?
            isSkeletallyAnimated = ReadBool( reader );

            // find all sub chunks
            if ( !IsEOF( reader ) )
            {
                chunkID = ReadChunk( reader );

                while ( !IsEOF( reader ) &&
                    ( chunkID == MeshChunkID.Geometry ||
                    chunkID == MeshChunkID.SubMesh ||
                    chunkID == MeshChunkID.MeshSkeletonLink ||
                    chunkID == MeshChunkID.MeshBoneAssignment ||
                    chunkID == MeshChunkID.MeshLOD ||
                    chunkID == MeshChunkID.MeshBounds ||
                    chunkID == MeshChunkID.SubMeshNameTable ||
                    chunkID == MeshChunkID.EdgeLists ) )
                {

                    switch ( chunkID )
                    {
                        case MeshChunkID.Geometry:
                            mesh.SharedVertexData = new VertexData();

                            // read geometry into shared vertex data
                            ReadGeometry( reader, mesh.SharedVertexData );

                            // TODO trap errors here
                            break;

                        case MeshChunkID.SubMesh:
                            // read the sub mesh data
                            ReadSubMesh( reader );
                            break;

                        case MeshChunkID.MeshSkeletonLink:
                            // read skeleton link
                            ReadSkeletonLink( reader );
                            break;

                        case MeshChunkID.MeshBoneAssignment:
                            // read mesh bone assignments
                            ReadMeshBoneAssignment( reader );
                            break;

                        case MeshChunkID.MeshLOD:
                            // Handle meshes with LOD
                            ReadMeshLodInfo( reader );
                            break;

                        case MeshChunkID.MeshBounds:
                            // read the pre-calculated bounding information
                            ReadBoundsInfo( reader );
                            break;

                        case MeshChunkID.SubMeshNameTable:
                            ReadSubMeshNameTable( reader );
                            break;

                        case MeshChunkID.EdgeLists:
                            ReadEdgeList( reader );
                            break;
                    } // switch

                    // grab the next chunk
                    if ( !IsEOF( reader ) )
                    {
                        chunkID = ReadChunk( reader );
                    }
                } // while

                // backpedal to the start of the chunk
                if ( !IsEOF( reader ) )
                {
                    Seek( reader, -ChunkOverheadSize );
                }
            }
        }

        protected virtual void ReadSubMesh( BinaryReader reader )
        {
            MeshChunkID chunkID;

            SubMesh subMesh = mesh.CreateSubMesh();

            // get the material name
            string materialName = ReadString( reader );
            subMesh.MaterialName = materialName;

            // use shared vertices?
            subMesh.useSharedVertices = ReadBool( reader );

            subMesh.indexData.indexStart = 0;
            subMesh.indexData.indexCount = ReadInt( reader );

            // does this use 32 bit index buffer
            bool idx32bit = ReadBool( reader );

            HardwareIndexBuffer idxBuffer = null;

            if ( idx32bit )
            {
                // create the index buffer
                idxBuffer =
                    HardwareBufferManager.Instance.
                    CreateIndexBuffer(
                    IndexType.Size32,
                    subMesh.indexData.indexCount,
                    mesh.IndexBufferUsage,
                    mesh.UseIndexShadowBuffer );

                IntPtr indices = idxBuffer.Lock( BufferLocking.Discard );

                // read the ints into the buffer data
                ReadInts( reader, subMesh.indexData.indexCount, indices );

                // unlock the buffer to commit					
                idxBuffer.Unlock();
            }
            else
            { // 16-bit
                // create the index buffer
                idxBuffer =
                    HardwareBufferManager.Instance.
                    CreateIndexBuffer(
                    IndexType.Size16,
                    subMesh.indexData.indexCount,
                    mesh.IndexBufferUsage,
                    mesh.UseIndexShadowBuffer );

                IntPtr indices = idxBuffer.Lock( BufferLocking.Discard );

                // read the shorts into the buffer data
                ReadShorts( reader, subMesh.indexData.indexCount, indices );

                idxBuffer.Unlock();
            }

            // save the index buffer
            subMesh.indexData.indexBuffer = idxBuffer;

            // Geometry chunk (optional, only present if useSharedVertices = false)
            if ( !subMesh.useSharedVertices )
            {
                chunkID = ReadChunk( reader );

                if ( chunkID != MeshChunkID.Geometry )
                {
                    throw new AxiomException( "Missing geometry data in mesh file." );
                }

                subMesh.vertexData = new VertexData();

                // read the geometry data
                ReadGeometry( reader, subMesh.vertexData );
            }

            // get the next chunkID
            chunkID = ReadChunk( reader );

            // walk through all the bone assignments for this submesh
            while ( !IsEOF( reader ) &&
                ( chunkID == MeshChunkID.SubMeshBoneAssignment ||
                chunkID == MeshChunkID.SubMeshOperation ) )
            {

                switch ( chunkID )
                {
                    case MeshChunkID.SubMeshBoneAssignment:
                        ReadSubMeshBoneAssignment( reader, subMesh );
                        break;

                    case MeshChunkID.SubMeshOperation:
                        ReadSubMeshOperation( reader, subMesh );
                        break;
                }

                // read the next chunkID
                if ( !IsEOF( reader ) )
                {
                    chunkID = ReadChunk( reader );
                }
            } // while

            // walk back to the beginning of the last chunk ID read since
            // we already moved past it and it wasnt of interest to us
            if ( !IsEOF( reader ) )
            {
                Seek( reader, -ChunkOverheadSize );
            }
        }

        protected virtual void ReadSubMeshOperation( BinaryReader reader, SubMesh sub )
        {
            sub.operationType = (OperationType)ReadShort( reader );
        }

        protected virtual void ReadGeometry( BinaryReader reader, VertexData data )
        {
            data.vertexStart = 0;
            data.vertexCount = ReadInt( reader );

            // find optional geometry chunks
            if ( !IsEOF( reader ) )
            {
                MeshChunkID chunkID = ReadChunk( reader );

                while ( !IsEOF( reader ) &&
                    ( chunkID == MeshChunkID.GeometryVertexDeclaration ||
                    chunkID == MeshChunkID.GeometryVertexBuffer ) )
                {

                    switch ( chunkID )
                    {
                        case MeshChunkID.GeometryVertexDeclaration:
                            ReadGeometryVertexDeclaration( reader, data );
                            break;

                        case MeshChunkID.GeometryVertexBuffer:
                            ReadGeometryVertexBuffer( reader, data );
                            break;
                    }

                    // get the next chunk
                    if ( !IsEOF( reader ) )
                    {
                        chunkID = ReadChunk( reader );
                    }
                }

                if ( !IsEOF( reader ) )
                {
                    // backpedal to start of non-submesh chunk
                    Seek( reader, -ChunkOverheadSize );
                }
            }
        }

        protected virtual void ReadGeometryVertexDeclaration( BinaryReader reader, VertexData data )
        {
            // find optional geometry chunks
            if ( !IsEOF( reader ) )
            {
                MeshChunkID chunkID = ReadChunk( reader );

                while ( !IsEOF( reader ) &&
                    ( chunkID == MeshChunkID.GeometryVertexElement ) )
                {

                    switch ( chunkID )
                    {
                        case MeshChunkID.GeometryVertexElement:
                            ReadGeometryVertexElement( reader, data );
                            break;
                    }

                    // get the next chunk
                    if ( !IsEOF( reader ) )
                    {
                        chunkID = ReadChunk( reader );
                    }
                }

                if ( !IsEOF( reader ) )
                {
                    // backpedal to start of non-submesh chunk
                    Seek( reader, -ChunkOverheadSize );
                }
            }
        }

        protected virtual void ReadGeometryVertexElement( BinaryReader reader, VertexData data )
        {
            short source = ReadShort( reader );
            VertexElementType type = (VertexElementType)ReadShort( reader );
            VertexElementSemantic semantic = (VertexElementSemantic)ReadShort( reader );
            short offset = ReadShort( reader );
            short index = ReadShort( reader );

            // add the element to the declaration for the current vertex data
            data.vertexDeclaration.AddElement( source, offset, type, semantic, index );
        }

        protected virtual void ReadGeometryVertexBuffer( BinaryReader reader, VertexData data )
        {
            // Index to bind this buffer to
            short bindIdx = ReadShort( reader );

            // Per-vertex size, must agree with declaration at this index
            short vertexSize = ReadShort( reader );

            // check for vertex data header
            MeshChunkID chunkID = ReadChunk( reader );

            if ( chunkID != MeshChunkID.GeometryVertexBufferData )
            {
                throw new AxiomException( "Can't find vertex buffer data area!" );
            }

            // check that vertex size agrees
            if ( data.vertexDeclaration.GetVertexSize( bindIdx ) != vertexSize )
            {
                throw new AxiomException( "Vertex buffer size does not agree with vertex declaration!" );
            }

            // create/populate vertex buffer
            HardwareVertexBuffer buffer =
                HardwareBufferManager.Instance.CreateVertexBuffer(
                    vertexSize,
                    data.vertexCount,
                    mesh.VertexBufferUsage,
                    mesh.UseVertexShadowBuffer );

            IntPtr bufferPtr = buffer.Lock( BufferLocking.Discard );

            ReadBytes( reader, data.vertexCount * vertexSize, bufferPtr );

            buffer.Unlock();

            // set binding
            data.vertexBufferBinding.SetBinding( bindIdx, buffer );
        }

        protected virtual void ReadSkeletonLink( BinaryReader reader )
        {
            mesh.SkeletonName = ReadString( reader );
        }

        protected virtual void ReadMeshBoneAssignment( BinaryReader reader )
        {
            VertexBoneAssignment assignment = new VertexBoneAssignment();

            // read the data from the file
            assignment.vertexIndex = ReadInt( reader );
            assignment.boneIndex = ReadUShort( reader );
            assignment.weight = ReadFloat( reader );

            // add the assignment to the mesh
            mesh.AddBoneAssignment( ref assignment );
        }

        protected virtual void ReadSubMeshBoneAssignment( BinaryReader reader, SubMesh sub )
        {
            VertexBoneAssignment assignment = new VertexBoneAssignment();

            // read the data from the file
            assignment.vertexIndex = ReadInt( reader );
            assignment.boneIndex = ReadUShort( reader );
            assignment.weight = ReadFloat( reader );

            // add the assignment to the mesh
            sub.AddBoneAssignment( ref assignment );
        }

        protected virtual void ReadMeshLodInfo( BinaryReader reader )
        {
            MeshChunkID chunkId;

            // number of lod levels
            mesh.numLods = ReadShort( reader );

            // load manual?
            mesh.isLodManual = ReadBool( reader );

            // preallocate submesh lod face data if not manual
            if ( !mesh.isLodManual )
            {
                for ( int i = 0; i < mesh.SubMeshCount; i++ )
                {
                    SubMesh sub = mesh.GetSubMesh( i );

                    // TODO Create typed collection and implement resize
                    for ( int j = 1; j < mesh.numLods; j++ )
                    {
                        sub.lodFaceList.Add( null );
                    }
                    //sub.lodFaceList.Resize(mesh.numLods - 1);
                }
            }

            // Loop from 1 rather than 0 (full detail index is not in file)
            for ( int i = 1; i < mesh.numLods; i++ )
            {
                chunkId = ReadChunk( reader );

                if ( chunkId != MeshChunkID.MeshLODUsage )
                {
                    throw new AxiomException( "Missing MeshLodUsage chunk in mesh '{0}'", mesh.Name );
                }

                // camera depth
                MeshLodUsage usage = new MeshLodUsage();
                usage.fromSquaredDepth = ReadFloat( reader );

                if ( mesh.isLodManual )
                {
                    ReadMeshLodUsageManual( reader, i, ref usage );
                }
                else
                {
                    ReadMeshLodUsageGenerated( reader, i, ref usage );
                }

                // push lod usage onto the mesh lod list
                mesh.lodUsageList.Add( usage );
            }
        }

        protected virtual void ReadMeshLodUsageManual( BinaryReader reader, int lodNum, ref MeshLodUsage usage )
        {
            MeshChunkID chunkId = ReadChunk( reader );

            if ( chunkId != MeshChunkID.MeshLODManual )
            {
                throw new AxiomException( "Missing MeshLODManual chunk in '{0}'.", mesh.Name );
            }

            usage.manualName = ReadString( reader );

            // clearing the reference just in case
            usage.manualMesh = null;
        }

        protected virtual void ReadMeshLodUsageGenerated( BinaryReader reader, int lodNum, ref MeshLodUsage usage )
        {
            usage.manualName = "";
            usage.manualMesh = null;

            // get one set of detail per submesh
            MeshChunkID chunkId;

            for ( int i = 0; i < mesh.SubMeshCount; i++ )
            {
                chunkId = ReadChunk( reader );

                if ( chunkId != MeshChunkID.MeshLODGenerated )
                {
                    throw new AxiomException( "Missing MeshLodGenerated chunk in '{0}'", mesh.Name );
                }

                // get the current submesh
                SubMesh sm = mesh.GetSubMesh( i );

                // drop another index data object into the list
                IndexData indexData = new IndexData();
                sm.lodFaceList[lodNum - 1] = indexData;

                // number of indices
                indexData.indexCount = ReadInt( reader );

                bool is32bit = ReadBool( reader );

                // create an appropriate index buffer and stuff in the data
                if ( is32bit )
                {
                    indexData.indexBuffer =
                        HardwareBufferManager.Instance.CreateIndexBuffer(
                        IndexType.Size32,
                        indexData.indexCount,
                        mesh.IndexBufferUsage,
                        mesh.UseIndexShadowBuffer );

                    // lock the buffer
                    IntPtr data = indexData.indexBuffer.Lock( BufferLocking.Discard );

                    // stuff the data into the index buffer
                    ReadInts( reader, indexData.indexCount, data );

                    // unlock the index buffer
                    indexData.indexBuffer.Unlock();
                }
                else
                {
                    indexData.indexBuffer =
                        HardwareBufferManager.Instance.CreateIndexBuffer(
                        IndexType.Size16,
                        indexData.indexCount,
                        mesh.IndexBufferUsage,
                        mesh.UseIndexShadowBuffer );

                    // lock the buffer
                    IntPtr data = indexData.indexBuffer.Lock( BufferLocking.Discard );

                    // stuff the data into the index buffer
                    ReadShorts( reader, indexData.indexCount, data );

                    // unlock the index buffer
                    indexData.indexBuffer.Unlock();
                }
            }
        }

        protected virtual void ReadBoundsInfo( BinaryReader reader )
        {
            // min abb extent
            Vector3 min = ReadVector3( reader );

            // max abb extent
            Vector3 max = ReadVector3( reader );

            // set the mesh's aabb
            mesh.BoundingBox = new AxisAlignedBox( min, max );

            // set the bounding sphere radius
            mesh.BoundingSphereRadius = ReadFloat( reader );
        }

        protected virtual void ReadEdgeList( BinaryReader reader )
        {
            if ( !IsEOF( reader ) )
            {
                MeshChunkID chunkID = ReadChunk( reader );

                while ( !IsEOF( reader ) &&
                    chunkID == MeshChunkID.EdgeListLOD )
                {

                    // process single LOD
                    short lodIndex = ReadShort( reader );

                    // If manual, no edge data here, loaded from manual mesh
                    bool isManual = ReadBool( reader );

                    // Only load in non-manual levels; others will be connected up by Mesh on demand
                    if ( !isManual )
                    {
                        MeshLodUsage usage = mesh.GetLodLevel( lodIndex );

                        usage.edgeData = new EdgeData();

                        int triCount = ReadInt( reader );
                        int edgeGroupCount = ReadInt( reader );

                        // TODO Resize triangle list
                        // TODO Resize edge groups

                        for ( int i = 0; i < triCount; i++ )
                        {
                            EdgeData.Triangle tri = new EdgeData.Triangle();

                            tri.indexSet = ReadInt( reader );
                            tri.vertexSet = ReadInt( reader );

                            tri.vertIndex[0] = ReadInt( reader );
                            tri.vertIndex[1] = ReadInt( reader );
                            tri.vertIndex[2] = ReadInt( reader );

                            tri.sharedVertIndex[0] = ReadInt( reader );
                            tri.sharedVertIndex[1] = ReadInt( reader );
                            tri.sharedVertIndex[2] = ReadInt( reader );

                            tri.normal = ReadVector4( reader );

                            usage.edgeData.triangles.Add( tri );
                        }

                        for ( int eg = 0; eg < edgeGroupCount; eg++ )
                        {
                            chunkID = ReadChunk( reader );

                            if ( chunkID != MeshChunkID.EdgeListGroup )
                            {
                                throw new AxiomException( "Missing EdgeListGroup chunk." );
                            }

                            EdgeData.EdgeGroup edgeGroup = new EdgeData.EdgeGroup();

                            edgeGroup.vertexSet = ReadInt( reader );

                            int edgeCount = ReadInt( reader );

                            // TODO Resize the edge group list

                            for ( int e = 0; e < edgeCount; e++ )
                            {
                                EdgeData.Edge edge = new EdgeData.Edge();

                                edge.triIndex[0] = ReadInt( reader );
                                edge.triIndex[1] = ReadInt( reader );

                                edge.vertIndex[0] = ReadInt( reader );
                                edge.vertIndex[1] = ReadInt( reader );

                                edge.sharedVertIndex[0] = ReadInt( reader );
                                edge.sharedVertIndex[1] = ReadInt( reader );

                                edge.isDegenerate = ReadBool( reader );

                                // add the edge to the list
                                edgeGroup.edges.Add( edge );
                            }

                            // Populate edgeGroup.vertexData references
                            // If there is shared vertex data, vertexSet 0 is that, 
                            // otherwise 0 is first dedicated
                            if ( mesh.SharedVertexData != null )
                            {
                                if ( edgeGroup.vertexSet == 0 )
                                {
                                    edgeGroup.vertexData = mesh.SharedVertexData;
                                }
                                else
                                {
                                    edgeGroup.vertexData = mesh.GetSubMesh( edgeGroup.vertexSet - 1 ).vertexData;
                                }
                            }
                            else
                            {
                                edgeGroup.vertexData = mesh.GetSubMesh( edgeGroup.vertexSet ).vertexData;
                            }

                            // add the edge group to the list
                            usage.edgeData.edgeGroups.Add( edgeGroup );
                        }
                    }

                    // grab the next chunk
                    if ( !IsEOF( reader ) )
                    {
                        chunkID = ReadChunk( reader );
                    }
                }

                // grab the next chunk
                if ( !IsEOF( reader ) )
                {
                    // backpedal to the start of chunk
                    Seek( reader, -ChunkOverheadSize );
                }
            }

            mesh.edgeListsBuilt = true;
        }

        #endregion Protected

        #endregion Methods
    }

    /// <summary>
    ///     Mesh serializer for supporint OGRE 1.20 meshes.
    /// </summary>
    public class MeshSerializerImplv12 : MeshSerializerImpl
    {
        #region Constructor

        public MeshSerializerImplv12()
        {
            version = "[MeshSerializer_v1.20]";
        }

        #endregion Constructor

        #region Methods

        protected override void ReadMesh( BinaryReader reader )
        {
            base.ReadMesh( reader );

            // always automatically build edge lists for this version
            mesh.AutoBuildEdgeLists = true;
        }

        protected override void ReadGeometry( BinaryReader reader, VertexData data )
        {
            ushort texCoordSet = 0;

            short bindIdx = 0;

            data.vertexStart = 0;
            data.vertexCount = ReadInt( reader );

            ReadGeometryPositions( bindIdx++, reader, data );

            if ( !IsEOF( reader ) )
            {
                // check out the next chunk
                MeshChunkID chunkID = ReadChunk( reader );

                // keep going as long as we have more optional buffer chunks
                while ( !IsEOF( reader ) &&
                    ( chunkID == MeshChunkID.GeometryNormals ||
                    chunkID == MeshChunkID.GeometryColors ||
                    chunkID == MeshChunkID.GeometryTexCoords ) )
                {

                    switch ( chunkID )
                    {
                        case MeshChunkID.GeometryNormals:
                            ReadGeometryNormals( bindIdx++, reader, data );
                            break;

                        case MeshChunkID.GeometryColors:
                            ReadGeometryColors( bindIdx++, reader, data );
                            break;

                        case MeshChunkID.GeometryTexCoords:
                            ReadGeometryTexCoords( bindIdx++, reader, data, texCoordSet++ );
                            break;

                    } // switch

                    // read the next chunk
                    if ( !IsEOF( reader ) )
                    {
                        chunkID = ReadChunk( reader );
                    }
                } // while

                if ( !IsEOF( reader ) )
                {
                    // skip back so the continuation of the calling loop can look at the next chunk
                    // since we already read past it
                    Seek( reader, -ChunkOverheadSize );
                }
            }
        }

        protected virtual void ReadGeometryPositions( short bindIdx, BinaryReader reader, VertexData data )
        {
            data.vertexDeclaration.AddElement( bindIdx, 0, VertexElementType.Float3, VertexElementSemantic.Position );

            // vertex buffers
            HardwareVertexBuffer vBuffer = HardwareBufferManager.Instance.
                CreateVertexBuffer( data.vertexDeclaration.GetVertexSize( bindIdx ),
                data.vertexCount, mesh.VertexBufferUsage, mesh.UseVertexShadowBuffer );

            IntPtr posData = vBuffer.Lock( BufferLocking.Discard );

            // ram the floats into the buffer data
            ReadFloats( reader, data.vertexCount * 3, posData );

            // unlock the buffer
            vBuffer.Unlock();

            // bind the position data
            data.vertexBufferBinding.SetBinding( bindIdx, vBuffer );
        }

        protected virtual void ReadGeometryNormals( short bindIdx, BinaryReader reader, VertexData data )
        {
            // add an element for normals
            data.vertexDeclaration.AddElement( bindIdx, 0, VertexElementType.Float3, VertexElementSemantic.Normal );

            HardwareVertexBuffer vBuffer = HardwareBufferManager.Instance.CreateVertexBuffer(
                data.vertexDeclaration.GetVertexSize( bindIdx ),
                data.vertexCount,
                mesh.VertexBufferUsage,
                mesh.UseVertexShadowBuffer );

            // lock the buffer for editing
            IntPtr normals = vBuffer.Lock( BufferLocking.Discard );

            // stuff the floats into the normal buffer
            ReadFloats( reader, data.vertexCount * 3, normals );

            // unlock the buffer to commit
            vBuffer.Unlock();

            // bind this buffer
            data.vertexBufferBinding.SetBinding( bindIdx, vBuffer );
        }

        protected virtual void ReadGeometryColors( short bindIdx, BinaryReader reader, VertexData data )
        {
            // add an element for normals
            data.vertexDeclaration.AddElement( bindIdx, 0, VertexElementType.Color, VertexElementSemantic.Diffuse );

            HardwareVertexBuffer vBuffer = HardwareBufferManager.Instance.CreateVertexBuffer(
                data.vertexDeclaration.GetVertexSize( bindIdx ),
                data.vertexCount,
                mesh.VertexBufferUsage,
                mesh.UseVertexShadowBuffer );

            // lock the buffer for editing
            IntPtr colors = vBuffer.Lock( BufferLocking.Discard );

            // stuff the floats into the normal buffer
            ReadInts( reader, data.vertexCount, colors );

            // unlock the buffer to commit
            vBuffer.Unlock();

            // bind this buffer
            data.vertexBufferBinding.SetBinding( bindIdx, vBuffer );
        }

        protected virtual void ReadGeometryTexCoords( short bindIdx, BinaryReader reader, VertexData data, int coordSet )
        {
            // get the number of texture dimensions (1D, 2D, 3D, etc)
            short dim = ReadShort( reader );

            // add a vertex element for the current tex coord set
            data.vertexDeclaration.AddElement(
                bindIdx, 0,
                VertexElement.MultiplyTypeCount( VertexElementType.Float1, dim ),
                VertexElementSemantic.TexCoords,
                coordSet );

            // create the vertex buffer for the tex coords
            HardwareVertexBuffer vBuffer = HardwareBufferManager.Instance.CreateVertexBuffer(
                data.vertexDeclaration.GetVertexSize( bindIdx ),
                data.vertexCount,
                mesh.VertexBufferUsage,
                mesh.UseVertexShadowBuffer );

            // lock the vertex buffer
            IntPtr texCoords = vBuffer.Lock( BufferLocking.Discard );

            // blast the tex coord data into the buffer
            ReadFloats( reader, data.vertexCount * dim, texCoords );

            // unlock the buffer to commit
            vBuffer.Unlock();

            // bind the tex coord buffer
            data.vertexBufferBinding.SetBinding( bindIdx, vBuffer );
        }

        #endregion Methods
    }

    /// <summary>
    ///     Mesh serializer for supporint OGRE 1.10 meshes.
    /// </summary>
    public class MeshSerializerImplv11 : MeshSerializerImplv12
    {
        #region Constructor

        public MeshSerializerImplv11()
        {
            version = "[MeshSerializer_v1.10]";
        }

        #endregion Constructor

        #region Methods

        protected override void ReadGeometryTexCoords( short bindIdx, BinaryReader reader, VertexData data, int coordSet )
        {
            // get the number of texture dimensions (1D, 2D, 3D, etc)
            short dim = ReadShort( reader );

            // add a vertex element for the current tex coord set
            data.vertexDeclaration.AddElement(
                bindIdx, 0,
                VertexElement.MultiplyTypeCount( VertexElementType.Float1, dim ),
                VertexElementSemantic.TexCoords,
                coordSet );

            // create the vertex buffer for the tex coords
            HardwareVertexBuffer vBuffer = HardwareBufferManager.Instance.CreateVertexBuffer(
                data.vertexDeclaration.GetVertexSize( bindIdx ),
                data.vertexCount,
                mesh.VertexBufferUsage,
                mesh.UseVertexShadowBuffer );

            // lock the vertex buffer
            IntPtr texCoords = vBuffer.Lock( BufferLocking.Discard );

            // blast the tex coord data into the buffer
            ReadFloats( reader, data.vertexCount * dim, texCoords );

            // Adjust individual v values to (1 - v)
            if ( dim == 2 )
            {
                int count = 0;

                unsafe
                {
                    float* pTex = (float*)texCoords.ToPointer();

                    for ( int i = 0; i < data.vertexCount; i++ )
                    {
                        count++; // skip u
                        pTex[count] = 1.0f - pTex[count]; // v = 1 - v
                        count++;
                    }
                }
            }

            // unlock the buffer to commit
            vBuffer.Unlock();

            // bind the tex coord buffer
            data.vertexBufferBinding.SetBinding( bindIdx, vBuffer );
        }

        #endregion Methods
    }
}
