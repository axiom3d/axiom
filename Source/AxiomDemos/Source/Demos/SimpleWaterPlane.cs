using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using Axiom;
using Axiom.Core;
using Axiom.Media;
using Axiom.Graphics;
using Axiom.Math;
namespace Axiom.Demos
{
    /// <summary>
    /// Vertex struct for position and uv data.
    /// </summary>
    public struct VertexPositionTexture
    {
        public float X, Y, Z;
        public float TU, TV;

        public static int SizeInBytes = ( 3 + 2 ) * sizeof( float );
    }
    /// <summary>
    /// 
    /// </summary>
    public abstract class RenderTargetListener
    {
        protected RenderTarget mTarget;
        protected Viewport mViewport;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="targetName"></param>
        public RenderTargetListener( string targetName, Camera camera )
        {
            Texture tex = TextureManager.Instance.CreateManual(
                targetName, ResourceGroupManager.DefaultResourceGroupName,
                TextureType.TwoD,
                512,
                512,
                0,
                PixelFormat.R8G8B8, TextureUsage.RenderTarget );

            mTarget = tex.GetBuffer().GetRenderTarget();

            mViewport = mTarget.AddViewport( camera );
            mViewport.ShowOverlays = false;

            mTarget.ViewportAdded += new RenderTargetViewportEventHandler( ViewportAdded );
            mTarget.ViewportRemoved += new RenderTargetViewportEventHandler( ViewportRemoved );
            mTarget.BeforeViewportUpdate += new RenderTargetViewportEventHandler( PostViewportUpdate );
            mTarget.BeforeUpdate += new RenderTargetEventHandler( PreRenderTargetUpdate );
            mTarget.AfterUpdate += new RenderTargetEventHandler( PostRenderTargetUpdate );
        }
        public virtual void ViewportRemoved( RenderTargetViewportEventArgs e )
        {

        }
        public virtual void ViewportAdded( RenderTargetViewportEventArgs e )
        {
        }
        public virtual void PostViewportUpdate( RenderTargetViewportEventArgs e )
        {
        }
        public virtual void PreViewportUpdate( RenderTargetViewportEventArgs e )
        {
        }
        public virtual void PostRenderTargetUpdate( RenderTargetEventArgs e )
        {
        }
        public virtual void PreRenderTargetUpdate( RenderTargetEventArgs e )
        {
        }


    }
    /// <summary>
    /// 
    /// </summary>
    public class RefractionListener : RenderTargetListener
    {
        SceneNode mWaterNode;
        public RefractionListener( SceneNode waterNode, Camera camera )
            : base( "RefractionMap", camera )
        {

            mWaterNode = waterNode;

            Material waterMat = (Material)MaterialManager.Instance.GetByName( "SimpleWaterMesh_Material" );
            TextureUnitState tus = waterMat.GetTechnique( 0 ).GetPass( 0 ).CreateTextureUnitState( "RefractionMap" );
            tus.SetTextureAddressingMode( TextureAddressing.Clamp );
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="evt"></param>
        public override void PreRenderTargetUpdate( RenderTargetEventArgs e )
        {
            mWaterNode.IsVisible = false;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="evt"></param>
        public override void PostRenderTargetUpdate( RenderTargetEventArgs e )
        {
            mWaterNode.IsVisible = true;
        }
    }
    /// <summary>
    /// 
    /// </summary>
    public class ReflectionListener
    {
        Plane mReflectionPlane;
        public MovablePlane mMovablePlane;
        SceneNode mWaterNode;
        SceneNode mPlaneSceneNode;
        Vector3 mPlaneNormal = Vector3.NegativeUnitY;
        public Camera mReflectCamera;
        protected Texture mReflectionMap;
        protected Viewport mReflectionViewport;
        protected Camera mSceneCam;

        public void NotifyResize( Camera camera )
        {
            mReflectCamera.Near = camera.Near;
            mReflectCamera.Far = camera.Far;
            mReflectCamera.AspectRatio = camera.AspectRatio;
        }
        /// <summary>
        /// 
        /// </summary>
        public ReflectionListener( SceneNode waterNode, Camera camera, SceneManager sm )
        {
            mSceneCam = camera;
            mWaterNode = waterNode;
            mReflectionPlane = new Plane( Vector3.UnitY, 0 );

            mPlaneSceneNode = sm.RootSceneNode.CreateChildSceneNode();
            //mMovablePlane = new MovablePlane(new Plane(Vector3.UnitY, 0));
            mMovablePlane = new MovablePlane( "ReflectionMovablePlane" );
            // mMovablePlane.DerivedPlane = new Plane(Vector3.UnitY, 0);
            mPlaneSceneNode.AttachObject( mMovablePlane );
            mPlaneSceneNode.Position = mWaterNode.Position;
            MeshManager.Instance.CreatePlane( "ReflectionMapClipPlane", ResourceGroupManager.DefaultResourceGroupName,
                mReflectionPlane, 512, 512, 10, 10, true, 1, 5, 5, Vector3.UnitZ );
            mMovablePlane.CastShadows = false;
            mReflectionMap = TextureManager.Instance.CreateManual(
                "ReflectionMap", ResourceGroupManager.DefaultResourceGroupName,
                TextureType.TwoD,
                512,
                512,
                0,
                PixelFormat.R8G8B8, TextureUsage.RenderTarget );

            RenderTarget mTarget = mReflectionMap.GetBuffer().GetRenderTarget();

            mReflectionViewport = mTarget.AddViewport( camera );
            mReflectionViewport.ShowOverlays = false;

            mTarget.BeforeUpdate += new RenderTargetEventHandler( PreRenderTargetUpdate );
            mTarget.AfterUpdate += new RenderTargetEventHandler( PostRenderTargetUpdate );

            RenderTarget reflTarget = mReflectionMap.GetBuffer().GetRenderTarget();
            mReflectCamera = sm.CreateCamera( "ReflectCam" );
            mReflectCamera.Near = camera.Near;
            mReflectCamera.Far = camera.Far;
            mReflectCamera.AspectRatio = camera.AspectRatio;
            mReflectionViewport = reflTarget.AddViewport( mReflectCamera, 0, 0, (float)camera.Viewport.ActualWidth, (float)camera.Viewport.ActualHeight, 1 );

            mReflectionViewport.ShowOverlays = false;
            mReflectionViewport.BackgroundColor = ColorEx.Blue;
            mReflectionViewport.SetClearEveryFrame( true );
            mReflectionViewport.ShowSkies = false;

            reflTarget.BeforeUpdate += new RenderTargetEventHandler( PreRenderTargetUpdate );
            reflTarget.AfterUpdate += new RenderTargetEventHandler( PostRenderTargetUpdate );
            Material waterMat = (Material)MaterialManager.Instance.GetByName( "SimpleWaterMesh_Material" );
            TextureUnitState tus = waterMat.GetTechnique( 0 ).GetPass( 0 ).CreateTextureUnitState( "ReflectionMap" );
            tus.SetTextureAddressingMode( TextureAddressing.Clamp );

            mReflectCamera.EnableReflection( mMovablePlane.DerivedPlane );
            mReflectCamera.EnableCustomNearClipPlane( mMovablePlane.DerivedPlane );
        }

        public void Update()
        {
            mReflectCamera.Orientation = mSceneCam.Orientation;
            mReflectCamera.Position = mSceneCam.Position;
        }
        public void PreRenderTargetUpdate( RenderTargetEventArgs e )
        {
            mWaterNode.IsVisible = false;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="evt"></param>
        public void PostRenderTargetUpdate( RenderTargetEventArgs e )
        {
            mWaterNode.IsVisible = true;
        }

    }
    /// <summary>
    /// 
    /// </summary>
    public class SimpleWaterPlane : TechDemo
    {
        Vector4 mVelocity0 = new Vector4( 0.01f, 0.03f, 0, 0 );
        Vector4 mVelocity1 = new Vector4( -0.01f, 0.03f, 0, 0 );
        Vector4 mWaveMapOffset0 = new Vector4();
        Vector4 mWaveMapOffset1 = new Vector4();
        /// <summary>
        /// 
        /// </summary>
        protected HardwareIndexBuffer mIndexBuffer;
        /// <summary>
        /// 
        /// </summary>
        protected HardwareVertexBuffer mVertexBuffer;
        /// <summary>
        /// 
        /// </summary>
        protected Mesh mWaterMesh;
        /// <summary>
        /// 
        /// </summary>
        protected SubMesh mWaterSubMesh;
        /// <summary>
        /// 
        /// </summary>
        protected int mMeshResolution = 256;
        protected VertexPositionTexture[] mVertices;
        protected SceneNode mWaterNode;
        protected MovablePlane mDepthMapPlane;

        protected ReflectionListener mReflectionListener;
        protected RefractionListener mRefractionListener;
        protected SceneNode mLightNode;
        public override void CreateScene()
        {
            Plane plane = new Plane();
            plane.Normal = Vector3.UnitY;
            plane.D = 0;

            MeshManager.Instance.CreatePlane( "MyPlane", ResourceGroupManager.DefaultResourceGroupName, plane, 1000000, 1000000, 10, 10, true, 1, 50, 50, Vector3.UnitZ );

            Entity planeEnt = scene.CreateEntity( "plane", "MyPlane" );
            planeEnt.MaterialName = "Examples/GrassFloor";
            planeEnt.CastShadows = false;

            scene.RootSceneNode.CreateChildSceneNode().AttachObject( planeEnt );
            camera.Position = new Vector3( camera.Position.x, camera.Position.y + 1000, camera.Position.z );

            Light mLight0 = scene.CreateLight( "Sun" );
            mLight0.Type = LightType.Directional;
            mLight0.Position = new Vector3( camera.Position.x, camera.Position.y + 100000, camera.Position.z );
            mLight0.Diffuse = new ColorEx( 0.5f, 0.5f, 0.5f );
            mLight0.Specular = new ColorEx( .5f, .5f, .5f );
            mLight0.Position = new Vector3( 0, 10, 0 );
            // set some ambient light
            //scene.AmbientLight = new ColorEx( 1, 0.5f, 0.5f, 0.5f );

            // set a basic skybox
            scene.SetSkyBox( true, "Skybox/Space", 5000.0f );

            LogManager.Instance.Write( "Creating Water Mesh..." );
            mWaterMesh = MeshManager.Instance.CreateManual( "SimpleWater_Mesh", ResourceGroupManager.DefaultResourceGroupName, null );
            mWaterSubMesh = mWaterMesh.CreateSubMesh( "SimpleWater_SubMesh" );
            mWaterSubMesh.useSharedVertices = false;
            CreateGeometry();
            AxisAlignedBox meshBounds = new AxisAlignedBox(
                    new Vector3( -1000000, -10, -1000000 ),
                    new Vector3( 1000000, 10, 1000000 ) );
            mWaterMesh.BoundingBox = meshBounds;
            mWaterMesh.Load();
            mWaterMesh.Touch();
            Entity waterEnt = scene.CreateEntity( "Water_Entity", "SimpleWater_Mesh" );
            waterEnt.CastShadows = false;
            waterEnt.RenderQueueGroup = RenderQueueGroupID.Main;
            CreateMaterial();

            waterEnt.MaterialName = "SimpleWaterMesh_Material";
            mWaterNode = scene.RootSceneNode.CreateChildSceneNode( "SimpleWater_Node" );
            mWaterNode.ShowBoundingBox = true;
            mWaterNode.AttachObject( waterEnt );
            mWaterNode.Position = new Vector3( 0, 5, 0 );
            mWaterNode.IsVisible = true;
            //mWaterNode.Scale = new Vector3(20, 20, 20);

            Material mat = (Material)MaterialManager.Instance.GetByName( "SimpleWaterMesh_Material" );
            mat.GetBestTechnique( 0 ).GetPass( 0 ).CreateTextureUnitState( "Wave0.dds" );
            mat.GetBestTechnique( 0 ).GetPass( 0 ).CreateTextureUnitState( "Wave1.dds" );
            mReflectionListener = new ReflectionListener( mWaterNode, camera, scene );
            mRefractionListener = new RefractionListener( mWaterNode, camera );

            LogManager.Instance.Write( "Creating Water Mesh...Done!" );
        }

        protected override void OnFrameStarted( object source, FrameEventArgs evt )
        {
            base.OnFrameStarted( source, evt );
            Camera cam = camera;
            mReflectionListener.Update();
            //update the wave map offsets so that they will scroll across the water
            mWaveMapOffset0 += mVelocity0 * evt.TimeSinceLastFrame;
            mWaveMapOffset1 += mVelocity1 * evt.TimeSinceLastFrame;

            if ( mWaveMapOffset0.x >= 1.0f || mWaveMapOffset0.x <= -1.0f )
                mWaveMapOffset0.x = 0.0f;
            if ( mWaveMapOffset1.x >= 1.0f || mWaveMapOffset1.x <= -1.0f )
                mWaveMapOffset1.x = 0.0f;
            if ( mWaveMapOffset0.y >= 1.0f || mWaveMapOffset0.y <= -1.0f )
                mWaveMapOffset0.y = 0.0f;
            if ( mWaveMapOffset1.y >= 1.0f || mWaveMapOffset1.y <= -1.0f )
                mWaveMapOffset1.y = 0.0f;

            Material material = (Material)MaterialManager.Instance.GetByName( "SimpleWaterMesh_Material" );
            GpuProgramParameters fragment = material.GetTechnique( 0 ).GetPass( 0 ).VertexProgramParameters;

            fragment.SetNamedConstant( "WaveMapOffset0", mWaveMapOffset0 );
            fragment.SetNamedConstant( "WaveMapOffset1", mWaveMapOffset1 );
            fragment = material.GetTechnique( 0 ).GetPass( 0 ).FragmentProgramParameters;
            fragment.SetNamedConstant( "SunFactor", 0.5f );//the intensity of the sun specular term.
            fragment.SetNamedConstant( "SunPower", .5f ); //how shiny we want the sun specular term on the water to be.

        }
        /// <summary>
        /// 
        /// </summary>
        public SimpleWaterPlane()
        {
        }
        /// <summary>
        /// 
        /// </summary>
        void CreateGeometry()
        {
            int mRows = 129;
            int mCells = 129;
            float dx = 1f;
            float halfWidth = ( mRows - 1 ) * dx * 0.5f;
            float halfDepth = ( mCells - 1 ) * dx * 0.5f;
            int mNumVertices = mRows * mCells;
            int mNumFaces = ( mRows - 1 ) * ( mCells - 1 ) * 2;


            //prepare vertex buffer
            mWaterSubMesh.vertexData = new VertexData();
            mWaterSubMesh.VertexData.vertexStart = 0;
            mWaterSubMesh.VertexData.vertexCount = mNumVertices;

            //create the specific vertex declaration
            VertexDeclaration vdecl = mWaterSubMesh.VertexData.vertexDeclaration;
            VertexBufferBinding vbind = mWaterSubMesh.VertexData.vertexBufferBinding;
            int offset = 0;
            short source = 0;
            vdecl.AddElement( source, offset, VertexElementType.Float3, VertexElementSemantic.Position );
            offset += VertexElement.GetTypeSize( VertexElementType.Float3 );

            vdecl.AddElement( 0, offset, VertexElementType.Float2, VertexElementSemantic.TexCoords );
			mVertexBuffer = HardwareBufferManager.Instance.CreateVertexBuffer(vdecl,
                mNumVertices,
                BufferUsage.DynamicWriteOnly );


            vbind.SetBinding( 0, mVertexBuffer );

            VertexPositionTexture[] vertices = new VertexPositionTexture[ mRows * mCells ];
            float du = 1.0f / ( mRows - 1 );
            float dv = 1.0f / ( mCells - 1 );
            for ( int i = 0; i < mRows; i++ )
            {
                float z = halfDepth - i * dx;
                for ( int j = 0; j < mCells; j++ )
                {
                    float x = -halfWidth + j * dx;
                    float y = 0f;
                    int index = i * mRows + j;
                    vertices[ index ].X = x * 512;
                    vertices[ index ].Y = y;
                    vertices[ index ].Z = z * 512;
                    vertices[ index ].TU = j * du;
                    vertices[ index ].TV = i * dv;
                }
            }

            mVertexBuffer.WriteData(
                0, mVertexBuffer.Size, vertices, true );

            ////prepare buffer for indices
            mIndexBuffer = HardwareBufferManager.Instance.CreateIndexBuffer(
                IndexType.Size32,
                mNumFaces * 3,
                BufferUsage.Static,
                true );

            int[] indices = new int[ mNumFaces * 3 ];
            int k = 0;
            for ( int i = 0; i < ( mRows - 1 ); i++ )
            {
                for ( int j = 0; j < ( mCells - 1 ); j++ )
                {
                    indices[ k ] = i * mRows + j;
                    indices[ k + 1 ] = i * mRows + j + 1;
                    indices[ k + 2 ] = ( i + 1 ) * mRows + j;

                    indices[ k + 3 ] = ( i + 1 ) * mRows + j;
                    indices[ k + 4 ] = i * mRows + j + 1;
                    indices[ k + 5 ] = ( i + 1 ) * mRows + j + 1;
                    k += 6; // next quad
                }
            }


            mIndexBuffer.WriteData( 0,
                mIndexBuffer.Size,
                indices,
                true );
            //indexbuffer = null;

            /////set index buffer for this submesh
            mWaterSubMesh.IndexData.indexBuffer = mIndexBuffer;
            mWaterSubMesh.IndexData.indexStart = 0;
            mWaterSubMesh.IndexData.indexCount = mNumFaces * 3;

        }
        Material CreateMaterial()
        {
            Material waterMat = (Material)MaterialManager.Instance.Create(
                "SimpleWaterMesh_Material", ResourceGroupManager.DefaultResourceGroupName );

            Pass waterPass = waterMat.GetTechnique( 0 ).GetPass( 0 );
            waterPass.DepthWrite = true;

            HighLevelGpuProgram fragprog = HighLevelGpuProgramManager.Instance.CreateProgram(
                "WaterShader_FP", ResourceGroupManager.DefaultResourceGroupName,
                "hlsl", GpuProgramType.Fragment );

            string fpsource = GetFPSWaterShader();
            fragprog.Source = fpsource;
            fragprog.SetParam( "entry_point", "WaterPS" );
            fragprog.SetParam( "target", "ps_2_0" );

            fragprog.Load();
            fragprog.Source = fpsource;
            HighLevelGpuProgram vertprog = HighLevelGpuProgramManager.Instance.CreateProgram(
                "WaterShader_VP", ResourceGroupManager.DefaultResourceGroupName,
                "hlsl", GpuProgramType.Vertex );

            string vpsource = GetVSWaterShader();
            vertprog.Source = vpsource;
            vertprog.SetParam( "entry_point", "WaterVS" );
            vertprog.SetParam( "target", "vs_2_0" );

            vertprog.Load();
            waterPass.VertexProgramName = "WaterShader_VP";
            waterPass.FragmentProgramName = "WaterShader_FP";

            GpuProgramParameters vertexParams = waterPass.VertexProgramParameters;
            GpuProgramParameters fragmentParams = waterPass.FragmentProgramParameters;

            vertexParams.SetNamedAutoConstant( "World", GpuProgramParameters.AutoConstantType.WorldMatrix, 0 );
            vertexParams.SetNamedAutoConstant( "WorldViewProj", GpuProgramParameters.AutoConstantType.WorldViewProjMatrix, 0 );
            vertexParams.SetNamedAutoConstant( "EyePos", GpuProgramParameters.AutoConstantType.CameraPosition, 0 );
            vertexParams.SetNamedConstant( "TexScale", 2.5f );
            fragmentParams.SetNamedConstant( "WaterColor", new ColorEx( 0.5f, 0.79f, 0.75f, 1.0f ) );
            fragmentParams.SetNamedConstant( "SunDirection", new Vector3( 2.6f, -1.0f, -1.5f ) );
            fragmentParams.SetNamedConstant( "SunColor", new ColorEx( 0.5f, 0.8f, 0.4f, 1.0f ) );
            fragmentParams.SetNamedConstant( "SunFactor", 0.05f );//the intensity of the sun specular term.
            fragmentParams.SetNamedConstant( "SunPower", .1f ); //how shiny we want the sun specular term on the water to be.

            return waterMat;

        }
        public ColorEx SunColor
        {
            set
            {
                Material mat = (Material)MaterialManager.Instance.GetByName( "SimpleWaterMesh_Material" );
                GpuProgramParameters fragmentParams = mat.GetBestTechnique().GetPass( 0 ).FragmentProgramParameters;
                fragmentParams.SetNamedConstant( "SunColor", value );
            }
        }
        public Vector3 SunDirection
        {
            set
            {
                Material mat = (Material)MaterialManager.Instance.GetByName( "SimpleWaterMesh_Material" );
                GpuProgramParameters fragmentParams = mat.GetBestTechnique().GetPass( 0 ).FragmentProgramParameters;
                fragmentParams.SetNamedConstant( "SunDirection", value );
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="vp"></param>
        public void NotifyResize( Camera camera )
        {
            mReflectionListener.NotifyResize( camera );
        }
        float time = 0;

        public string GetVSWaterShader()
        {
            string vertex = "";
            vertex += "float4x4 World;\n";
            vertex += "float4x4 WorldViewProj;\n";
            vertex += "float3 EyePos;\n";
            vertex += "float4 WaveMapOffset0;\n";
            vertex += "float4 WaveMapOffset1;\n";
            vertex += "float TexScale;\n";
            vertex += "struct OutputVS\n";
            vertex += "{\n";
            vertex += "float4 posH			: POSITION0;\n";
            vertex += "float3 toEyeW		: TEXCOORD0;\n";
            vertex += "float2 tex0			: TEXCOORD1;\n";
            vertex += "float2 tex1			: TEXCOORD2;\n";
            vertex += "float4 projTexC		: TEXCOORD3;\n";
            vertex += "float4 pos			: TEXCOORD4;\n";
            vertex += "};\n";

            vertex += "OutputVS WaterVS( float3 posL	: POSITION0, \n";
            vertex += "float2 texC   : TEXCOORD0)\n";
            vertex += "{\n";
            // Zero out our output.
            vertex += "OutputVS outVS = (OutputVS)0;\n";

            // Transform vertex position to world space.
            vertex += "float3 posW = mul(World, float4(posL, 1.0f)).xyz;\n";
            vertex += "outVS.pos.xyz = posW;\n";
            vertex += "outVS.pos.w = 1.0f;\n";

            // Compute the unit vector from the vertex to the eye.
            vertex += "outVS.toEyeW = posW - EyePos;\n";

            // Transform to homogeneous clip space.
            vertex += "outVS.posH = mul(WorldViewProj,float4(posL, 1.0f));\n";

            // Scroll texture coordinates.
            vertex += "outVS.tex0 = (texC * TexScale) + WaveMapOffset0.xy;\n";
            vertex += "outVS.tex1 = (texC * TexScale) + WaveMapOffset1.xy;\n";

            // Generate projective texture coordinates from camera's perspective.
            vertex += "outVS.projTexC = outVS.posH;\n";

            // Done--return the output.
            vertex += "return outVS;\n";
            vertex += "}";
            return vertex;
        }
        public string GetFPSWaterShader()
        {
            string fragment = "";
            fragment += "float4 WaterColor;\n";
            fragment += "float3 SunDirection;\n";
            fragment += "float4 SunColor;\n";
            fragment += "float SunFactor;\n"; //the intensity of the sun specular term.
            fragment += "float SunPower;\n"; //how shiny we want the sun specular term on the water to be.
            fragment += "float3 EyePos;\n";
            fragment += "static const float	  R0 = 0.02037f;\n";
            fragment += "float4 WaterPS( float3 toEyeW		: TEXCOORD0,\n";
            fragment += "float2 tex0			: TEXCOORD1,\n";
            fragment += "float2 tex1			: TEXCOORD2,\n";
            fragment += "float4 projTexC		: TEXCOORD3,\n";
            fragment += "float4 pos			: TEXCOORD4, \n";
            fragment += "uniform sampler2D WaveMap0 : register(s0),\n";
            fragment += "uniform sampler2D WaveMap1 : register(s1),\n";
            fragment += "uniform sampler2D ReflectMap : register(s2),\n";
            fragment += "uniform sampler2D RefractMap: register(s3)\n";
            fragment += " ) : COLOR\n";
            fragment += "{\n";

            //transform the projective texcoords to NDC space
            //and scale and offset xy to correctly sample a DX texture
            fragment += "projTexC.xyz /= projTexC.w;            \n";
            fragment += "projTexC.x =  0.5f*projTexC.x + 0.5f; \n";
            fragment += "projTexC.y = -0.5f*projTexC.y + 0.5f;\n";
            fragment += "projTexC.z = .1f / projTexC.z; //refract more based on distance from the camera\n";

            fragment += "toEyeW    = normalize(toEyeW);\n";

            // Light vector is opposite the direction of the light.
            fragment += "float3 lightVecW = -SunDirection;\n";

            // Sample normal map.
            fragment += "float3 normalT0 = tex2D(WaveMap0, tex0);\n";
            fragment += "float3 normalT1 = tex2D(WaveMap1, tex1);\n";

            //unroll the normals retrieved from the normalmaps
            fragment += "normalT0.yz = normalT0.zy;	\n";
            fragment += "normalT1.yz = normalT1.zy;\n";

            fragment += "normalT0 = 2.0f*normalT0 - 1.0f;\n";
            fragment += " normalT1 = 2.0f*normalT1 - 1.0f;\n";

            fragment += "float3 normalT = normalize(0.5f*(normalT0 + normalT1));\n";
            fragment += "float3 n1 = float3(0,1,0); //we'll just use the y unit vector for spec reflection.\n";

            //get the reflection vector from the eye
            fragment += "float3 R = normalize(reflect(toEyeW,normalT));\n";

            fragment += "float4 finalColor;\n";
            fragment += "finalColor.a = 1;\n";

            //compute the fresnel term to blend reflection and refraction maps
            fragment += "float ang = saturate(dot(-toEyeW,n1));\n";
            fragment += "float f = R0 + (1.0f-R0) * pow(1.0f-ang,5.0);	\n";

            //also blend based on distance
            fragment += "f = min(1.0f, f + 0.007f * EyePos.y);	\n";

            //compute the reflection from sunlight
            fragment += "float sunFactor = SunFactor;\n";
            fragment += "float sunPower = SunPower;\n";
            /*
            fragment += "if(EyePos.y < pos.y)\n";
            fragment += "{\n";
            fragment += "	sunFactor = 7.0f; //these could also be sent to the shader\n";
            fragment += "	sunPower = 55.0f;\n";
            fragment += "}\n";
            */
            fragment += "float3 sunlight = sunFactor * pow(saturate(dot(R, lightVecW)), sunPower) * SunColor;\n";
            
            fragment += "float4 refl = tex2D(ReflectMap, projTexC.xy + projTexC.z * normalT.xz);\n";
            fragment += "float4 refr = tex2D(RefractMap, projTexC.xy - projTexC.z * normalT.xz);\n";

            //only use the refraction map if we're under water
            fragment += "if(EyePos.y < pos.y)\n";
            fragment += "	f = 0.0f;\n";

            //interpolate the reflection and refraction maps based on the fresnel term and add the sunlight
            fragment += "finalColor.rgb = WaterColor * lerp( refr, refl, f) + sunlight;\n";

            fragment += "return finalColor;\n";
            fragment += "}\n";

            return fragment;
        }
    }
}
