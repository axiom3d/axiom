using System;
using System.Collections.Generic;
using System.Text;
using Axiom.Core;
using Axiom.Graphics;
using Axiom.Math;
using Axiom.Media;
namespace Axiom.Demos
{
    public class VolumetricTexture : TechDemo, IManualResourceLoader
    {
        protected Texture mPTex;
        float global_real = 0.4f;
        float global_imag = 0.6f;
        float global_theta = 0.0f;

        public override void CreateCamera()
        {
            camera = scene.CreateCamera( "PlayerCam" );
            camera.Position = new Vector3( 220, -2, 176 );
            camera.LookAt( Vector3.Zero );
            camera.Near = 5;

        }

        public override void CreateViewports()
        {
            base.CreateViewports();
            viewport.BackgroundColor = ColorEx.CornflowerBlue;
        }

        public override void CreateScene()
        {
            RenderSystemCapabilities caps = Root.Instance.RenderSystem.Capabilities;
            if ( !caps.HasCapability( Capabilities.Texture3D ) )
            {
                throw new NotSupportedException( "Your card does not support 3d textures, so you can not run this Demo. Sorry!" );
            }
            mPTex = TextureManager.Instance.CreateManual( "DynaTex", "General", TextureType.ThreeD, 64, 64, 64, 0, PixelFormat.A8R8G8B8, TextureUsage.Default, this );
            Generate();
            //SaveToDisk( mPTex, "julia.dds" );
            scene.AmbientLight = new ColorEx( 0.6f, 0.6f, 0.6f );
            scene.SetSkyBox( true, "Examples/MorningSkyBox", 50 );

            Light l = scene.CreateLight( "MainLight" );
            l.Diffuse = new ColorEx( 0.75f, 0.75f, 0.80f );
            l.Specular = new ColorEx( 0.9f, 0.9f, 1 );
            l.Position = new Vector3( -100, 80, 50 );
            scene.RootSceneNode.AttachObject( l );
            SceneNode snode = scene.RootSceneNode.CreateChildSceneNode( Vector3.Zero );
            VolumeRenderable vrend = new VolumeRenderable( 32, 750f, "VolumeTexture.dds" );
            snode.AttachObject( vrend );
            camera.LookAt( snode.Position );
        }

        private void SaveToDisk( Texture tp, string filename )
        {
          // Declare buffer
            int buffSize = tp.Width * tp.Height * tp.Depth * 4;
            byte[] data = new byte[buffSize];
          

          // Setup Image with correct settings
          Image i = new Image();
          i.FromDynamicImage(data, tp.Width, tp.Height, tp.Depth,tp.Format);
          
          // Copy Texture buffer contents to image buffer
          HardwarePixelBuffer buf = tp.GetBuffer();      
          PixelBox destBox = i.GetPixelBox(0,0);
          buf.BlitToMemory(destBox);
          
          // Save to disk!
          i.Save( @"C:\" + filename );
        }

        private void Generate()
        {
            Julia julia = new Julia( global_real, global_imag, global_theta );
            float scale = 2.5f;
            float vcut = 29.0f;
            float vscale = 1.0f / vcut;

            HardwarePixelBuffer buffer = mPTex.GetBuffer( 0, 0 );

            IntPtr Data = buffer.Lock( BufferLocking.Normal );
            unsafe
            {
                PixelBox pb = buffer.CurrentLock;
                int* pbptr = (int*)pb.Data.ToPointer();
                for ( int z = pb.Front; z < pb.Back; z++ )
                {
                    for ( int y = pb.Top; y < pb.Bottom; y++ )
                    {
                        for ( int x = pb.Left; x < pb.Right; x++ )
                        {
                            if ( z == pb.Front || z == ( pb.Back - 1 ) || y == pb.Top || y == ( pb.Bottom - 1 ) ||
                                x == pb.Left || x == ( pb.Right - 1 ) )
                            {
                                // On border, must be zero
                                pbptr[ x ] = 0;
                            }
                            else
                            {
                                float val = julia.eval( ( (float)x / pb.Width - 0.5f ) * scale,
                                        ( (float)y / pb.Height - 0.5f ) * scale,
                                        ( (float)z / pb.Depth - 0.5f ) * scale );
                                if ( val > vcut )
                                    val = vcut;

                                ColorEx col = new ColorEx( ( 1.0f - ( val * vscale ) ) * 0.7f, (float)x / pb.Width, (float)y / pb.Height, (float)z / pb.Depth );
                                IntPtr vale = new IntPtr(pbptr + x );
                                PixelConverter.PackColor( col, PixelFormat.A8R8G8B8, vale );
                            }
                        }
                        pbptr += pb.RowPitch;
                    }
                    pbptr += pb.SliceSkip;
                }
                buffer.Unlock();
            }
        }

        #region IManualResourceLoader Members

        public void LoadResource( Resource resource )
        {
            this.Generate();
        }

        #endregion
    }

    public class VolumeRenderable : SimpleRenderable
    {
        protected float mSize;
        /// <summary>
        /// 
        /// </summary>
        protected int mSlices;
        /// <summary>
        /// 
        /// </summary>
        protected float mRadius;
        /// <summary>
        /// 
        /// </summary>
        protected Matrix3 mFakeOrientation;
        /// <summary>
        /// 
        /// </summary>
        protected string mTexture;
        protected TextureUnitState mUnit;
        protected IndexData iData;
        protected VertexData vData;

        public VolumeRenderable( int slices, float size, string textureName )
        {
            mSlices = slices;
            mSize = size;

            mTexture = textureName;
            //for each face
            mRadius = Utility.Sqrt(  size * size + size * size + size * size ) / 2.0;
            base.box = new AxisAlignedBox( new Vector3( -size, -size, -size ), new Vector3( size, size, size ) );
            base.castShadows = false;
            Initialize();
        }

        /// <summary>
        /// 
        /// </summary>
        protected void Initialize()
        {
            //create geometry
            int nvertices = mSlices * 4;
            int elemsize = 3 * 3;
            int dsize = elemsize * nvertices;
            int x = 0;

            iData = new IndexData();
            vData = new VertexData();

            float[] vertices = new float[ dsize ];
            float[,] coords = new float[ 4 ,2]
            {
                { 0.0f, 0.0f },
                { 0.0f, 1.0f },
                { 1.0f, 0.0f },
                { 1.0f, 1.0f },
            };
            for ( x = 0; x < mSlices; x++ )
            {
                for ( int y = 0; y < 4; y++ )
                {
                    float xcoord = coords[ y , 0 ] - 0.5f;
                    float ycoord = coords[ y , 1 ] - 0.5f;
                    float zcoord = -( x / ( mSlices - 1.0f ) - 0.5f );
                    // 1.0f .. a/(a+1)
                    // coordinate
                    vertices[ x * 4 * elemsize + y * elemsize + 0 ] = xcoord * ( mSize / 2.0f );
                    vertices[ x * 4 * elemsize + y * elemsize + 1 ] = ycoord * ( mSize / 2.0f );
                    vertices[ x * 4 * elemsize + y * elemsize + 2 ] = zcoord * ( mSize / 2.0f );
                    // normal
                    vertices[ x * 4 * elemsize + y * elemsize + 3 ] = 0.0f;
                    vertices[ x * 4 * elemsize + y * elemsize + 4 ] = 0.0f;
                    vertices[ x * 4 * elemsize + y * elemsize + 5 ] = 1.0f;
                    // tex
                    vertices[ x * 4 * elemsize + y * elemsize + 6 ] = xcoord * Utility.Sqrt( 3.0f );
                    vertices[ x * 4 * elemsize + y * elemsize + 7 ] = ycoord * Utility.Sqrt( 3.0f );
                    vertices[ x * 4 * elemsize + y * elemsize + 8 ] = zcoord * Utility.Sqrt( 3.0f );
                }
            }

            int[] faces = new int[ mSlices * 6 ];
            for ( x = 0; x < mSlices; x++ )
            {
                faces[ x * 6 + 0 ] = ( x * 4 + 0 );
                faces[ x * 6 + 1 ] = ( x * 4 + 1 );
                faces[ x * 6 + 2 ] = ( x * 4 + 2 );
                faces[ x * 6 + 3 ] = ( x * 4 + 1 );
                faces[ x * 6 + 4 ] = ( x * 4 + 2 );
                faces[ x * 6 + 5 ] = ( x * 4 + 3 );
            }

            //setup buffers
            vData.vertexStart = 0;
            vData.vertexCount = nvertices;

            VertexDeclaration vdecl = vData.vertexDeclaration;
            VertexBufferBinding vbuf = vData.vertexBufferBinding;

            int offset = 0;

            vdecl.AddElement( 0, offset, VertexElementType.Float3, VertexElementSemantic.Position );
            offset += VertexElement.GetTypeSize( VertexElementType.Float3 );
            vdecl.AddElement( 0, offset, VertexElementType.Float3, VertexElementSemantic.Normal );
            offset += VertexElement.GetTypeSize( VertexElementType.Float3 );
            vdecl.AddElement( 0, offset, VertexElementType.Float3, VertexElementSemantic.TexCoords );
            offset += VertexElement.GetTypeSize( VertexElementType.Float3 );

			HardwareVertexBuffer vbuffer = HardwareBufferManager.Instance.CreateVertexBuffer( vdecl, nvertices, BufferUsage.StaticWriteOnly);

            vbuf.SetBinding( 0, vbuffer );
            vbuffer.WriteData( 0, vbuffer.Size, vertices, true );

            HardwareIndexBuffer ibuf = HardwareBufferManager.Instance
                .CreateIndexBuffer( IndexType.Size16, mSlices * 6,
                 BufferUsage.StaticWriteOnly );


            iData.indexBuffer = ibuf;
            iData.indexCount = mSlices * 6;
            iData.indexStart = 0;
            ibuf.WriteData( 0, ibuf.Size, faces, true );

            vertices = null;
            faces = null;

            // Now make the render operation
            renderOperation.operationType = OperationType.TriangleList;
            renderOperation.indexData = iData;
            renderOperation.vertexData = vData;
            renderOperation.useIndices = true;

            Material material = (Material)MaterialManager.Instance.Create( mTexture, "General" );


            material.RemoveAllTechniques();

            Technique technique = material.CreateTechnique();
            Pass pass = technique.CreatePass();

            TextureUnitState textureUnit = pass.CreateTextureUnitState();

            pass.SetSceneBlending( SceneBlendType.TransparentAlpha );
            pass.DepthWrite = false;
            pass.CullingMode = CullingMode.None;
            pass.LightingEnabled = false;

            textureUnit.SetTextureAddressingMode( TextureAddressing.Clamp );
            textureUnit.SetTextureName( mTexture, TextureType.ThreeD );
            textureUnit.SetTextureFiltering( TextureFiltering.Trilinear );

            mUnit = textureUnit;

            this.Material = material;

        }

        public override float BoundingRadius
        {
            get
            {
                return mRadius;
            }
        }

        public override float GetSquaredViewDepth( Camera camera )
        {
            Vector3 min = Vector3.Zero, max = Vector3.Zero, mid = Vector3.Zero, dist = Vector3.Zero;
            min = BoundingBox.Minimum;
            max = BoundingBox.Maximum;
            mid = ( ( min - max ) * 0.5f ) + min;
            dist = camera.DerivedPosition - mid;
            return dist.LengthSquared;
        }
        public override void GetWorldTransforms( Axiom.Math.Matrix4[] matrices )
        {
            Matrix4 destMatrix = Matrix4.Identity;
            Vector3 position = ParentNode.DerivedPosition;
            Vector3 scale = ParentNode.DerivedScale;
            Matrix3 scale3x3 = Matrix3.Zero;
            scale3x3[ 0, 0 ] = scale.x;
            scale3x3[ 1, 1 ] = scale.y;
            scale3x3[ 2, 2 ] = scale.z;

            destMatrix = mFakeOrientation * scale3x3;
            destMatrix.Translation = position;
            matrices[ 0 ] = destMatrix;
        }
        public override void NotifyCurrentCamera( Camera camera )
        {
            base.NotifyCurrentCamera( camera );

            Vector3 zVec = ParentNode.DerivedPosition - camera.DerivedPosition;
            float sqdist = zVec.LengthSquared;
            zVec.Normalize();
            Vector3 fixedAxis = camera.DerivedOrientation * Vector3.UnitY;
            Vector3 xVec = fixedAxis.Cross( zVec );
            xVec.Normalize();
            Vector3 yVec = zVec.Cross( xVec );
            yVec.Normalize();
            Quaternion oriQuat = Quaternion.FromAxes( xVec, yVec, zVec );
            if ( sqdist > mSize * mSize / 2 )    // ADD THIS CONDITION HERE
            {
                mFakeOrientation = oriQuat.ToRotationMatrix();
            }
            else
            {
                mFakeOrientation = camera.DerivedOrientation.ToRotationMatrix(); //cam->getDerivedOrientation().ToRotationMatrix(m_fakeOrientation);
            }


            Matrix3 tempMat = Matrix3.Zero;
            Quaternion q = ParentNode.DerivedOrientation.UnitInverse * oriQuat;
            tempMat = q.ToRotationMatrix();
            Matrix4 rotMat = Matrix4.Identity;
            rotMat = tempMat;
            rotMat.Translation = new Vector3( 0.5f, 0.5f, 0.5f );
            mUnit.TextureMatrix = rotMat;
            material.GetTechnique( 0 ).GetPass( 0 ).GetTextureUnitState( 0 ).TextureMatrix = rotMat;
        }


    }

    public class Julia
    {
        /**
         * Simple, fast, inline quaternion math functions
         */
        public struct Quat
        {
            public float r, i, j, k;
        };

        public void qadd( ref Quat a, ref Quat b )
        {
            a.r += b.r;
            a.i += b.i;
            a.j += b.j;
            a.k += b.k;
        }

        public void qmult( ref Quat c, ref Quat a, ref Quat b )
        {
            c.r = a.r * b.r - a.i * b.i - a.j * b.j - a.k * b.k;
            c.i = a.r * b.i + a.i * b.r + a.j * b.k - a.k * b.j;
            c.j = a.r * b.j + a.j * b.r + a.k * b.i - a.i * b.k;
            c.k = a.r * b.k + a.k * b.r + a.i * b.j - a.j * b.i;
        }

        public void qsqr( ref Quat b, ref Quat a )
        {
            b.r = a.r * a.r - a.i * a.i - a.j * a.j - a.k * a.k;
            b.i = 2.0f * a.r * a.i;
            b.j = 2.0f * a.r * a.j;
            b.k = 2.0f * a.r * a.k;
        }

        /**
         * Implicit function that evaluates the Julia set.
         */
        private float global_real, global_imag, global_theta;
        private Quat oc, c, eio, emio;

        public float eval( float x, float y, float z )
        {
            Quat q = new Quat(), temp = new Quat();
            int i;

            q.r = x;
            q.i = y;
            q.j = z;
            q.k = 0.0f;

            for ( i = 30; i > 0; i-- )
            {
                qsqr( ref temp, ref q );
                qmult( ref q, ref emio, ref temp );
                qadd( ref q, ref c );

                if ( q.r * q.r + q.i * q.i + q.j * q.j + q.k * q.k > 8.0 )
                    break;
            }

            return ( (float)i );
        }

        public Julia( float global_real, float global_imag, float global_theta )
        {
            this.global_real = global_real;
            this.global_imag = global_imag;
            this.global_theta = global_theta;
            oc.r = global_real;
            oc.i = global_imag;
            oc.j = oc.k = 0.0f;

            eio.r = (float)System.Math.Cos( global_theta );
            eio.i = (float)System.Math.Sin( global_theta );
            eio.j = 0.0f;
            eio.k = 0.0f;

            emio.r = (float)System.Math.Cos( -global_theta );
            emio.i = (float)System.Math.Sin( -global_theta );
            emio.j = 0.0f;
            emio.k = 0.0f;

            /***
             *** multiply eio*c only once at the beginning of iteration
             *** (since q |-> sqrt(eio*(q-eio*c)))
             *** q -> e-io*q^2 - eio*c
             ***/

            qmult( ref c, ref eio, ref oc );

        }
    }
}
