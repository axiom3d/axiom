using System;
using System.Collections;
using System.Text;
using System.IO;

using Axiom.MathLib;

namespace Axiom
{
    public class GeometryBucket : IRenderable
    {
        #region Fields and Properties
        protected QueuedGeometryList queuedGeometry;
        protected MaterialBucket parent;
        protected string formatString;
        protected VertexData vertexData;
        protected IndexData indexData;
        protected IndexType indexType;
        protected int maxVertexIndex;

        public MaterialBucket Parent
        {
            get
            {
                return parent;
            }
        }

        public VertexData VertexData
        {
            get
            {
                return VertexData;
            }
        }

        public IndexData IndexData
        {
            get
            {
                return indexData;
            }
        }
        #endregion

        #region Constructors
        public GeometryBucket( MaterialBucket parent, string formatString, VertexData vData, IndexData iData )
        {
            this.parent = parent;
            this.formatString = formatString;
            vertexData = vData;
            indexData = iData;
        }
        #endregion

        #region TODO Port from Ogre
        public Material Material
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public Technique Technique
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        //			bool getCastsShadows(void) const;
        public bool CastsShadows
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        //			void getRenderOperation(RenderOperation& op);
        public void GetRenderOperation( RenderOperation op )
        {
            throw new NotImplementedException();
        }

        //	        void getWorldTransforms(Matrix4* xform) const;
        public void GetWorldTransforms( Matrix4[] transforms )
        {
            throw new NotImplementedException();
        }

        //	        const LightList& getLights(void) const;
        public LightList Lights
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        //	        const Quaternion& getWorldOrientation(void) const;
        public Quaternion WorldOrientation
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        //	        const Vector3& getWorldPosition(void) const;
        public Vector3 WorldPosition
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        //			Real getSquaredViewDepth(const Camera* cam) const;
        public float GetSquaredViewDepth( Camera cam )
        {
            throw new NotImplementedException();
            //return 0.0f;
        }
        #endregion

        #region IRenderable members
        public bool NormalizeNormals
        {
            get
            {
                throw new NotImplementedException();
            }
        }
        public ushort NumWorldTransforms
        {
            get
            {
                throw new NotImplementedException();
            }
        }
        public bool UseIdentityProjection
        {
            get
            {
                throw new NotImplementedException();
            }
        }
        public bool UseIdentityView
        {
            get
            {
                throw new NotImplementedException();
            }
        }
        public SceneDetailLevel RenderDetail
        {
            get
            {
                throw new NotImplementedException();
            }
        }
        public Vector4 GetCustomParameter( int index )
        {
            throw new NotImplementedException();
        }
        public void SetCustomParameter( int index, Vector4 val )
        {
            throw new NotImplementedException();
        }
        public void UpdateCustomGpuParameter( GpuProgramParameters.AutoConstantEntry constant, GpuProgramParameters parameters )
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Public Methods
        public bool Assign( QueuedGeometry qgeom )
        {
            // do we have enough space
            if ( vertexData.vertexCount + qgeom.geometry.vertexData.vertexCount > maxVertexIndex )
            {
                return false;
            }

            queuedGeometry.Add( qgeom );
            vertexData.vertexCount += qgeom.geometry.vertexData.vertexCount;
            indexData.indexCount += qgeom.geometry.indexData.indexCount;

            return true;
        }

        public void Build( bool stencilShadows )
        {
            // Ok, here's where we transfer the vertices and indexes to the shared buffers
            VertexDeclaration dcl = vertexData.vertexDeclaration;
            VertexBufferBinding binds = vertexData.vertexBufferBinding;

            // create index buffer, and lock
            indexData.indexBuffer = HardwareBufferManager.Instance.CreateIndexBuffer( indexType, indexData.indexCount, BufferUsage.StaticWriteOnly );
            IntPtr pDest = indexData.indexBuffer.Lock( BufferLocking.Discard );

            // create all vertex buffers, and lock
            short b;
            short posBufferIdx = dcl.FindElementBySemantic( VertexElementSemantic.Position ).Source;

            ArrayList destBufferLocks = new ArrayList();
            ArrayList bufferElements = new ArrayList();
            for ( b = 0; b < binds.BindingCount; ++b )
            {
                int vertexCount = vertexData.vertexCount;
                // Need to double the vertex count for the position buffer
                // if we're doing stencil shadows
                if ( stencilShadows && b == posBufferIdx )
                {
                    vertexCount = vertexCount * 2;
                    if ( vertexCount > maxVertexIndex )
                        throw new Exception( "Index range exceeded when using stencil shadows, consider " +
                            "reducing your region size or reducing poly count" );
                }
                HardwareVertexBuffer vbuf = HardwareBufferManager.Instance.CreateVertexBuffer( dcl.GetVertexSize( b ), vertexCount, BufferUsage.StaticWriteOnly );
                binds.SetBinding( b, vbuf );
                IntPtr pLock = vbuf.Lock( BufferLocking.Discard );
                destBufferLocks.Add( pLock );
                // Pre-cache vertex elements per buffer
                bufferElements.Add( dcl.FindElementBySource( (ushort)b ) );
            }

            // iterate over the geometry items
            int indexOffset = 0;
            IEnumerator iter = queuedGeometry.GetEnumerator();
            Vector3 regionCenter = Vector3.Zero;// TODO Parent.Parent.Parent.GetCenter();
            while ( iter.MoveNext() )
            {
                QueuedGeometry geom = (QueuedGeometry)iter.Current;
                // copy indexes across with offset
                IndexData srcIdxData = geom.geometry.indexData;
                if ( indexType == IndexType.Size32 )
                {
                    IntPtr pSrc = srcIdxData.indexBuffer.Lock( BufferLocking.ReadOnly );
                    pDest = CopyIndexes32( pSrc, pDest, srcIdxData.indexCount, indexOffset );
                    srcIdxData.indexBuffer.Unlock();
                }
                else
                {
                    IntPtr pSrc = srcIdxData.indexBuffer.Lock( BufferLocking.ReadOnly );
                    pDest = CopyIndexes16( pSrc, pDest, srcIdxData.indexCount, indexOffset );
                    srcIdxData.indexBuffer.Unlock();
                }

                // Now deal with vertex buffers
                // we can rely on buffer counts / formats being the same
                VertexData srcVData = geom.geometry.vertexData;
                VertexBufferBinding srcBinds = srcVData.vertexBufferBinding;
                for ( b = 0; b < binds.BindingCount; ++b )
                {
                    // Iterate over vertices
                    destBufferLocks[b] = CopyVertices( srcBinds.GetBuffer( b ), (IntPtr)destBufferLocks[b], (VertexElementList)bufferElements[b], geom, regionCenter );
                }

                indexOffset += geom.geometry.vertexData.vertexCount;
            }

            // unlock everything
            indexData.indexBuffer.Unlock();
            for ( b = 0; b < binds.BindingCount; ++b )
            {
                binds.GetBuffer( b ).Unlock();
            }

            // If we're dealing with stencil shadows, copy the position data from
            // the early half of the buffer to the latter part
            if ( stencilShadows )
            {
                unsafe
                {
                    HardwareVertexBuffer buf = binds.GetBuffer( posBufferIdx );
                    IntPtr src = buf.Lock( BufferLocking.Normal );
                    byte* pSrc = (byte*)src.ToPointer();
                    // Point dest at second half (remember vertexcount is original count)
                    byte* pDst = pSrc + buf.VertexSize * vertexData.vertexCount;

                    int count = buf.VertexSize * buf.VertexCount;
                    while ( count-- > 0 )
                    {
                        *pDst++ = *pSrc++;
                    }
                    buf.Unlock();

                    // Also set up hardware W buffer if appropriate
                    RenderSystem rend = Root.Instance.RenderSystem;
                    if ( null != rend && rend.Caps.CheckCap( Capabilities.VertexPrograms ) )
                    {
                        buf = HardwareBufferManager.Instance.CreateVertexBuffer( sizeof( float ), vertexData.vertexCount * 2, BufferUsage.StaticWriteOnly, false );
                        // Fill the first half with 1.0, second half with 0.0
                        float* pW = (float*)buf.Lock( BufferLocking.Discard ).ToPointer();
                        for ( int v = 0; v < vertexData.vertexCount; ++v )
                        {
                            *pW++ = 1.0f;
                        }
                        for ( int v = 0; v < vertexData.vertexCount; ++v )
                        {
                            *pW++ = 0.0f;
                        }
                        buf.Unlock();
                        vertexData.hardwareShadowVolWBuffer = buf;
                    }
                }
            }
        }

        public void Dump( TextWriter output )
        {
            output.WriteLine( "Geometry Bucket" );
            output.WriteLine( "---------------" );
            output.WriteLine( "Format string: {0}", formatString );
            output.WriteLine( "Geometry items: {0}", queuedGeometry.Count );
            output.WriteLine( "Vertex count: {0}", vertexData.vertexCount );
            output.WriteLine( "Index count: {0}", indexData.indexCount );
            output.WriteLine( "---------------" );
        }
        #endregion

        #region Protected Methods
        protected unsafe IntPtr CopyIndexes32( IntPtr src, IntPtr dst, int count, int offset )
        {
            UInt32* pSrc = (UInt32*)src.ToPointer();
            UInt32* pDst = (UInt32*)dst.ToPointer();

            while ( count-- > 0 )
            {
                *pDst++ = *( pSrc++ + offset );
            }

            return new IntPtr( (void*)pDst );
        }

        protected unsafe IntPtr CopyIndexes16( IntPtr src, IntPtr dst, int count, int offset )
        {
            UInt16* pSrc = (UInt16*)src.ToPointer();
            UInt16* pDst = (UInt16*)dst.ToPointer();

            while ( count-- > 0 )
            {
                *pDst++ = *( pSrc++ + offset );
            }

            return new IntPtr( (void*)pDst );
        }

        protected unsafe IntPtr CopyVertices( HardwareVertexBuffer srcBuf, IntPtr dst, VertexElementList elems, QueuedGeometry geom, Vector3 regionCenter )
        {
            // lock source
            IntPtr src = srcBuf.Lock( BufferLocking.ReadOnly );
            int bufInc = srcBuf.VertexSize;

            byte* pSrc = (byte*)src.ToPointer();
            byte* pDst = (byte*)dst.ToPointer();
            float* pSrcReal;
            float* pDstReal;
            Vector3 temp = Vector3.Zero;

            for ( int v = 0; v < geom.geometry.vertexData.vertexCount; ++v )
            {
                // iterate over vertex elements
                IEnumerator iter = elems.GetEnumerator();
                while ( iter.MoveNext() )
                {
                    VertexElement elem = (VertexElement)iter.Current;
                    pSrcReal = (float*)( pSrc + elem.Offset );
                    pDstReal = (float*)( pDst + elem.Offset );

                    switch ( elem.Semantic )
                    {
                        case VertexElementSemantic.Position:
                            temp.x = *pSrcReal++;
                            temp.y = *pSrcReal++;
                            temp.z = *pSrcReal++;
                            // transform
                            temp = ( geom.orientation * ( temp * geom.scale ) ) + geom.position;
                            // adjust for region center
                            temp -= regionCenter;
                            *pDstReal++ = temp.x;
                            *pDstReal++ = temp.y;
                            *pDstReal++ = temp.z;
                            break;
                        case VertexElementSemantic.Normal:
                            temp.x = *pSrcReal++;
                            temp.y = *pSrcReal++;
                            temp.z = *pSrcReal++;
                            // rotation only
                            temp = geom.orientation * temp;
                            *pDstReal++ = temp.x;
                            *pDstReal++ = temp.y;
                            *pDstReal++ = temp.z;
                            break;
                        default:
                            // just raw copy
                            byte* pbSrc = (byte*)pSrcReal;
                            byte* pbDst = (byte*)pDstReal;
                            int size = VertexElement.GetTypeSize( elem.Type );
                            while ( size-- > 0 )
                            {
                                *pbDst++ = *pbSrc++;
                            }
                            break;
                    }
                }

                // Increment both pointers
                pDst += bufInc;
                pSrc += bufInc;
            }

            srcBuf.Unlock();
            return new IntPtr( pDst );
        }

        #endregion

    }

}
