using System;
using System.Collections.Generic;
using System.Text;

using Axiom.Graphics;
using Axiom.Math;
using Axiom.Core;

using MaterialPermutation = System.UInt32;

namespace Axiom.Demos.DeferredShadingSystem
{
    class MiniLight : SimpleRenderable
    {
        #region Fields and Properties

        protected bool _ignoreWorld;
        protected float _radius;
        protected MaterialPermutation _permutation;
        protected MaterialGenerator _materialGenerator;

        public Vector3 Attenuation
        {
            set
            { 	
                // Set Attenuation parameter to shader
                SetCustomParameter( 3, new Vector4( value.x, value.y, value.z, 0 ) );

                /// There is attenuation? Set material accordingly
                if ( value.x != 1.0f || value.y != 0.0f || value.z != 0.0f )
                    _permutation |= (uint)MaterialId.Attenuated;
                else
                    _permutation &= ~(uint)MaterialId.Attenuated;

                // Calculate radius from Attenuation
                int threshold_level = 15;// differece of 10-15 levels deemed unnoticable
                float threshold = 1.0f / ( (float)threshold_level / 256.0f );

                // Use quadratic formula to determine outer radius
                value.x = value.x - threshold;
                float d = Math.Utility.Sqrt( value.y * value.y - 4 * value.z * value.x );
                float x = ( -2 * value.x ) / ( value.y + d );

                this.RebuildGeometry( x );
            }
        }

        public ColorEx Diffuse
        {
            get
            {
                Vector4 val = this.GetCustomParameter( 1 );
                return new ColorEx( val[ 0 ], val[ 1 ], val[ 2 ], val[ 3 ] );
            }
            set
            {
                this.SetCustomParameter( 1, new Vector4( value.r, value.g, value.b, value.a ) );
            }
        }

        public ColorEx Specular
        {
            get
            {
                Vector4 val = this.GetCustomParameter( 2 );
                return new ColorEx( val[ 0 ], val[ 1 ], val[ 2 ], val[ 3 ] );
            }
            set
            {
                this.SetCustomParameter( 2, new Vector4( value.r, value.g, value.b, value.a ) );
                if ( value.r != 0.0f || value.g != 0.0f || value.b != 0.0f )
                    _permutation |= (uint)MaterialId.Specular;
                else
                    _permutation &= ~(uint)MaterialId.Specular;
            }
        }

        #endregion Fields and Properties

        #region Construction and Destruction

        public MiniLight( MaterialGenerator generator )
        {
            this._materialGenerator = generator;

            this.RenderQueueGroup = RenderQueueGroupID.Two;
            this.renderOperation.operationType = OperationType.TriangleList;
            this.renderOperation.indexData = null;
            this.renderOperation.vertexData = null;
            this.renderOperation.useIndices = true;

            this.Diffuse = ColorEx.White;
            this.Specular = ColorEx.Black;
            this.Attenuation = Vector3.UnitX;

        }

        #endregion Construction and Destruction

        #region Methods

        public void RebuildGeometry( float radius )
        {
            // Scale node to radius

            if ( this._radius > 10000.0f )
            {
                this.CreateRectangle2D();
                this._permutation |= (uint)MaterialId.Quad;
            }
            else
            {
                /// XXX some more intelligent expression for rings and segments
                this.CreateSphere( this._radius, 5, 5 );
                this._permutation &= ~(uint)MaterialId.Quad;
            }	
        }

        public void CreateSphere( float radius, int rings, int segments )
        {
            renderOperation.operationType = OperationType.TriangleStrip;
            renderOperation.vertexData = new VertexData();
            renderOperation.indexData = new IndexData();
            renderOperation.useIndices = false;

            VertexData vertexData = renderOperation.vertexData;
            IndexData indexData = renderOperation.indexData;

            // define the vertex format
            VertexDeclaration vertexDecl = vertexData.vertexDeclaration;
            int currOffset = 0;
            // only generate positions
            vertexDecl.AddElement( 0, currOffset, VertexElementType.Float3, VertexElementSemantic.Position );
            currOffset += VertexElement.GetTypeSize( VertexElementType.Float3 );
            // allocate the vertex buffer
            vertexData.vertexCount = ( rings + 1 ) * ( segments + 1 );
            HardwareVertexBuffer vBuf = HardwareBufferManager.Instance.CreateVertexBuffer( vertexDecl.GetVertexSize( 0 ), vertexData.vertexCount, BufferUsage.StaticWriteOnly, false );
            VertexBufferBinding binding = vertexData.vertexBufferBinding;
            binding.SetBinding( 0, vBuf );

            // allocate index buffer
            indexData.indexCount = 6 * rings * ( segments + 1 );
            indexData.indexBuffer = HardwareBufferManager.Instance.CreateIndexBuffer( IndexType.Size16, indexData.indexCount, BufferUsage.StaticWriteOnly, false );
            HardwareIndexBuffer iBuf = indexData.indexBuffer;

            float fDeltaRingAngle = ( Math.Utility.PI / rings );
            float fDeltaSegAngle = ( 2 * Math.Utility.PI / segments );
            ushort wVerticeIndex = 0;
            unsafe
            {
                float* pVertex = (float*)( vBuf.Lock( BufferLocking.Discard ).ToPointer() );
                ushort* pIndices = (ushort*)( iBuf.Lock( BufferLocking.Discard ).ToPointer() );
                // Generate the group of rings for the sphere
                for ( int ring = 0; ring <= rings; ring++ )
                {
                    float r0 = radius * Math.Utility.Sin( ring * fDeltaRingAngle );
                    float y0 = radius * Math.Utility.Cos( ring * fDeltaRingAngle );

                    // Generate the group of segments for the current ring
                    for ( int seg = 0; seg <= segments; seg++ )
                    {
                        float x0 = r0 * Math.Utility.Sin( seg * fDeltaSegAngle );
                        float z0 = r0 * Math.Utility.Cos( seg * fDeltaSegAngle );

                        // Add one vertex to the strip which makes up the sphere
                        *pVertex++ = x0;
                        *pVertex++ = y0;
                        *pVertex++ = z0;

                        if ( ring != rings )
                        {
                            // each vertex (except the last) has six indicies pointing to it
                            *pIndices++ = (ushort)(wVerticeIndex + segments + 1 );
                            *pIndices++ = wVerticeIndex;
                            *pIndices++ = (ushort)(wVerticeIndex + segments);
                            *pIndices++ = (ushort)(wVerticeIndex + segments + 1);
                            *pIndices++ = (ushort)(wVerticeIndex + 1);
                            *pIndices++ = wVerticeIndex;
                            wVerticeIndex++;
                        }
                    }; // end for seg
                } // end for ring
            }
            // Unlock
            vBuf.Unlock();
            iBuf.Unlock();

            // Set bounding box and sphere
            this.box = new AxisAlignedBox( new Vector3( -radius, -radius, -radius ), new Vector3( radius, radius, radius ) );
            this._radius = 15000;
            this._ignoreWorld = false;

        }

        public void CreateRectangle2D()
        {
            /// TODO: this RenderOp should really be re-used between MLight objects,
            /// not generated every time
            renderOperation.vertexData = new VertexData();
            renderOperation.indexData = null;

            renderOperation.vertexData.vertexCount = 4;
            renderOperation.vertexData.vertexStart = 0;
            renderOperation.operationType = OperationType.TriangleStrip;
            renderOperation.useIndices = false;

            VertexDeclaration decl = renderOperation.vertexData.vertexDeclaration;
            VertexBufferBinding bind = renderOperation.vertexData.vertexBufferBinding;

            decl.AddElement( 0, 0, VertexElementType.Float3, VertexElementSemantic.Position );

            HardwareVertexBuffer vbuf = HardwareBufferManager.Instance.CreateVertexBuffer( decl.GetVertexSize( 0 ),
                                                                                            renderOperation.vertexData.vertexCount,
                                                                                            BufferUsage.StaticWriteOnly );

            // Bind buffer
            bind.SetBinding( 0, vbuf );
            // Upload data
            float[] data = new float[] {
                                		-1, 1,-1,   // corner 1
                                		-1,-1,-1,   // corner 2
                                		 1, 1,-1,   // corner 3
                                		 1,-1,-1 }; // corner 4
            vbuf.WriteData( 0, sizeof( float ) * data.Length, data, true );

            // Set bounding
            this.box = new AxisAlignedBox( new Vector3( -10000, -10000, -10000 ), new Vector3( 10000, 10000, 10000 ) );
            this._radius = 15000;
            this._ignoreWorld = true;
        }

        #endregion Methods

        #region SimpleRenderable Implementation
		public override RenderOperation RenderOperation
		{
			get
			{
				return base.RenderOperation;
			}
		}

        public override float GetSquaredViewDepth( Axiom.Core.Camera camera )
        {
            if ( this._ignoreWorld )
            {
                return 0.0f;
            }
            else
            {
                Vector3 dist = this.camera.DerivedPosition - WorldPosition;
                return dist.LengthSquared;
            }
        }

        public override float BoundingRadius
        {
            get
            {
                return this._radius;
            }
        }

        public override Material Material
        {
            get
            {
                base.Material = this._materialGenerator.GetMaterial( this._permutation );
                return base.Material;
            }
            set
            {
                base.Material = value;
            }
        }

        #endregion SimpleRenderable Implementation
    }
}
