#region Namespace Declarations
using System;
using System.Collections.Generic;
using System.Text;
using Axiom.Math;
using Axiom.Core;
using Axiom.Animating;
using Axiom.Controllers;
using Axiom.Demos;
using Axiom.Controllers.Canned;
using Axiom.Graphics;
#endregion

namespace Axiom.Demos
{
	public class LightGrassWibbler : Controllers.IControllerValue<float>
	{
		protected Light light;
		protected Billboard billboard;
		protected ColorEx colorRange = new ColorEx();
		protected ColorEx halfColor = new ColorEx();
		protected float minSize;
		protected float sizeRange;
		protected float intensity;

		public LightGrassWibbler( Light light, Billboard billboard, ColorEx minColor,
			ColorEx maxColor, int minSize, int maxSize )
		{


			this.light = light;
			this.billboard = billboard;

			this.colorRange.r = ( maxColor.r - minColor.r ) * 0.5f;
			this.colorRange.g = ( maxColor.g - minColor.g ) * 0.5f;
			this.colorRange.b = ( maxColor.b - minColor.b ) * 0.5f;

			this.halfColor.r = ( minColor.r + colorRange.r );// 2;
			this.halfColor.g = ( minColor.g + colorRange.g );// 2;
			this.halfColor.b = ( minColor.b + colorRange.b );// 2;



			this.minSize = minSize;
			this.sizeRange = maxSize - minSize;



		}

		#region IControllerValue<float> Membres

		public float Value
		{
			get
			{
				return intensity;
			}
			set
			{
				intensity = value;

				ColorEx newColor = new ColorEx();
				//atenuate the brignetss of the light
				newColor.r = halfColor.r + ( colorRange.r * intensity );
				newColor.g = halfColor.g + ( colorRange.g * intensity );
				newColor.b = halfColor.b + ( colorRange.b * intensity );

				this.light.Diffuse = newColor;
				this.billboard.Color = newColor;

				float newSize = minSize + ( intensity * sizeRange );
				this.billboard.SetDimensions( newSize, newSize );


			}
		}

		#endregion
	}

	public class Grass : TechDemo
	{

		private const float GRASS_HEIGHT = 300;
		private const float GRASS_WIDTH = 250;
		private const string GRASS_MESH_NAME = "grassblades";
		private string GRASS_MATERIAL = "Examples/GrassBlades";
		private const int OFFSET_PARAM = 999;

		private Light m_Light;
		private SceneNode m_LightNode;
		private AnimationState m_AnimState;
		private ColorEx m_MinLightColour = new ColorEx( 0.5f, 0.1f, 0.0f );
		private ColorEx m_MaxLightColour = new ColorEx( 1.0f, 0.6f, 0.0f );
		private int m_MinFlareSize = 40;
		private int m_MaxFlareSize = 80;
		private StaticGeometry m_StaticGeom;

		protected override void CreateScene()
		{

			scene.SetSkyBox( true, "Skybox/Space", 10000 );

			SetupLighting();

			Plane plane;
			plane.Normal = Vector3.UnitY;
			plane.D = 0;

			MeshManager.Instance.CreatePlane( "MyPlane",
				ResourceGroupManager.DefaultResourceGroupName, plane,
				14500, 14500, 10, 10, true, 1, 50, 50, Vector3.UnitZ );

			Entity planeEnt = scene.CreateEntity( "plane", "MyPlane" );
			planeEnt.MaterialName = "Examples/GrassFloor";
			planeEnt.CastShadows = false;

			scene.RootSceneNode.CreateChildSceneNode().AttachObject( planeEnt );

			Vector3 minV = new Vector3( -2000, 0, -2000 );
			Vector3 maxV = new Vector3( 2000, 0, 2000 );

			CreateGrassMesh();

			Entity e = scene.CreateEntity( "1", GRASS_MESH_NAME );
			StaticGeometry s = scene.CreateStaticGeometry( "bing", 1 );
			s.RegionDimensions = new Vector3( 1000, 1000, 1000 );
			s.Origin = new Vector3( -500, 500, -500 );  //Set the region origin so the centre is at 0 world

			for ( int x = -1950; x < 1950; x += 150 )
			{
				for ( int z = -1950; z < 1950; z += 150 )
				{
					Vector3 pos = new Vector3(
						x + Math.Utility.RangeRandom( -25, 25 ),
						0,
						z + Math.Utility.RangeRandom( -25, 25 ) );

					Quaternion orientation = Quaternion.FromAngleAxis(
						Math.Utility.RangeRandom( 0, 359 ),
						Vector3.UnitY );

					Vector3 scale = new Vector3(
						1, Math.Utility.RangeRandom( 0.85f, 1.15f ), 1 );

					s.AddEntity( e, pos, orientation, scale );
				}

			}
			s.Build();
			m_StaticGeom = s;

			Mesh mesh = MeshManager.Instance.Load( "ogrehead.mesh",
				ResourceGroupManager.DefaultResourceGroupName );

			short src, dest;
			if ( !mesh.SuggestTangentVectorBuildParams( out src, out dest ) )
			{
				mesh.BuildTangentVectors( src, dest );
			}

			e = scene.CreateEntity( "head", "ogrehead.mesh" );
			e.MaterialName = "Examples/OffsetMapping/Specular";

			SceneNode headNode = scene.RootSceneNode.CreateChildSceneNode();
			headNode.AttachObject( e );
			headNode.Scale( new Vector3( 7, 7, 7 ) );
			headNode.Position = new Vector3( 0, 200, 0 );

			if ( e.GetSubEntity( 0 ).NormalizeNormals == false )
			{
				LogManager.Instance.Write( "aie aie aie" );
			}
			Root.Instance.RenderSystem.NormalizeNormals = true;

			camera.Move( new Vector3( 0, 350, 0 ) );
		}

		private void CreateGrassMesh()
		{
			// Each grass section is 3 planes at 60 degrees to each other
			// Normals point straight up to simulate correct lighting
			Mesh msh = MeshManager.Instance.CreateManual( GRASS_MESH_NAME,
				ResourceGroupManager.DefaultResourceGroupName, null );

			SubMesh sm = msh.CreateSubMesh();
			sm.useSharedVertices = false;
			sm.vertexData = new VertexData();
			sm.vertexData.vertexStart = 0;
			sm.vertexData.vertexCount = 12;

			VertexDeclaration dcl = sm.vertexData.vertexDeclaration;
			int offset = 0;

			dcl.AddElement( 0, offset, VertexElementType.Float3, VertexElementSemantic.Position );
			offset += VertexElement.GetTypeSize( VertexElementType.Float3 );
			dcl.AddElement( 0, offset, VertexElementType.Float3, VertexElementSemantic.Normal );
			offset += VertexElement.GetTypeSize( VertexElementType.Float3 );
			dcl.AddElement( 0, offset, VertexElementType.Float2, VertexElementSemantic.TexCoords );
			offset += VertexElement.GetTypeSize( VertexElementType.Float2 );

			HardwareVertexBuffer vbuf = HardwareBufferManager.Instance.CreateVertexBuffer(
				offset, 12, BufferUsage.StaticWriteOnly );

			int i;
			unsafe
			{
				float* pData = (float*)( vbuf.Lock( BufferLocking.Discard ).ToPointer() );

				Vector3 baseVec = new Vector3( GRASS_WIDTH / 2, 0, 0 );
				Vector3 vec = baseVec;
				Quaternion rot = Quaternion.FromAngleAxis( Utility.DegreesToRadians( 60 ), Vector3.UnitY );


				for ( i = 0; i < 3; ++i )
				{
					//position
					*pData++ = -vec.x;
					*pData++ = GRASS_HEIGHT;
					*pData++ = -vec.z;
					// normal
					*pData++ = 0;
					*pData++ = 1;
					*pData++ = 0;
					// uv
					*pData++ = 0;
					*pData++ = 0;

					// position
					*pData++ = vec.x;
					*pData++ = GRASS_HEIGHT;
					*pData++ = vec.z;
					// normal
					*pData++ = 0;
					*pData++ = 1;
					*pData++ = 0;
					// uv
					*pData++ = 1;
					*pData++ = 0;

					// position
					*pData++ = -vec.x;
					*pData++ = 0;
					*pData++ = -vec.z;
					// normal
					*pData++ = 0;
					*pData++ = 1;
					*pData++ = 0;
					// uv
					*pData++ = 0;
					*pData++ = 1;

					// position
					*pData++ = vec.x;
					*pData++ = 0;
					*pData++ = vec.z;
					// normal
					*pData++ = 0;
					*pData++ = 1;
					*pData++ = 0;
					// uv
					*pData++ = 1;
					*pData++ = 1;

					vec = rot * vec;
				}//for
			}//unsafe

			vbuf.Unlock();

			sm.vertexData.vertexBufferBinding.SetBinding( 0, vbuf );
			sm.indexData.indexCount = 6 * 3;
			sm.indexData.indexBuffer = HardwareBufferManager.Instance.CreateIndexBuffer( IndexType.Size16, 6 * 3, BufferUsage.StaticWriteOnly );

			unsafe
			{
				ushort* pI = (ushort*)( sm.indexData.indexBuffer.Lock( BufferLocking.Discard ) ).ToPointer();

				for ( i = 0; i < 3; ++i )
				{
					int off = i * 4;
					*pI++ = (ushort)( off );
					*pI++ = (ushort)( off + 3 );
					*pI++ = (ushort)( off + 1 );

					*pI++ = (ushort)( off + 0 );
					*pI++ = (ushort)( off + 2 );
					*pI++ = (ushort)( off + 3 );
				}
			}

			sm.indexData.indexBuffer.Unlock();
			sm.MaterialName = GRASS_MATERIAL;

			msh.Load();





		}

		private void SetupLighting()
		{
			scene.AmbientLight = new ColorEx( 0.2f, 0.2f, 0.2f );
			m_Light = scene.CreateLight( "Light2" );
			m_Light.Diffuse = new ColorEx( m_MinLightColour );
			m_Light.SetAttenuation( 8000, 1, 0.0005f, 0 );
			m_Light.Specular = new ColorEx( 1, 1, 1 );

			m_LightNode = scene.RootSceneNode.CreateChildSceneNode( "MovingLightNode" );
			m_LightNode.AttachObject( m_Light );
			//create billboard set

			BillboardSet bbs = scene.CreateBillboardSet( "lightbbs", 1 );
			bbs.MaterialName = "Examples/Flare";
			Billboard bb = bbs.CreateBillboard( new Vector3( 0, 0, 0 ), m_MinLightColour );
			m_LightNode.AttachObject( bbs );

			LightGrassWibbler val = new LightGrassWibbler( m_Light,
				bb,
				m_MinLightColour, m_MaxLightColour,
				m_MinFlareSize, m_MaxFlareSize );

			// create controller, after this is will get updated on its own
			WaveformControllerFunction func =
				new WaveformControllerFunction( WaveformType.Sine, 0.0f, 0.5f );

			ControllerManager.Instance.CreateController( val, func );

			m_LightNode.Position = new Vector3( 300, 250, -300 );

			Animation anim = scene.CreateAnimation( "LightTrack", 20 );
			//Spline it for nce curves
			anim.InterpolationMode = InterpolationMode.Spline;
			//create a srtack to animte the camera's node
			NodeAnimationTrack track = anim.CreateNodeTrack( 0, m_LightNode );
			//setup keyframes
			TransformKeyFrame key = track.CreateNodeKeyFrame( 0 );
			key.Translate = new Vector3( 300, 550, -300 );
			key = track.CreateNodeKeyFrame( 2 );//B
			key.Translate = new Vector3( 150, 600, -250 );
			key = track.CreateNodeKeyFrame( 4 );//C
			key.Translate = new Vector3( -150, 650, -100 );
			key = track.CreateNodeKeyFrame( 6 );//D
			key.Translate = new Vector3( -400, 500, -200 );
			key = track.CreateNodeKeyFrame( 8 );//E
			key.Translate = new Vector3( -200, 500, -400 );
			key = track.CreateNodeKeyFrame( 10 );//F
			key.Translate = new Vector3( -100, 450, -200 );
			key = track.CreateNodeKeyFrame( 12 );//G
			key.Translate = new Vector3( -100, 400, 180 );
			key = track.CreateNodeKeyFrame( 14 );//H
			key.Translate = new Vector3( 0, 250, 600 );
			key = track.CreateNodeKeyFrame( 16 );//I
			key.Translate = new Vector3( 100, 650, 100 );
			key = track.CreateNodeKeyFrame( 18 );//J
			key.Translate = new Vector3( 250, 600, 0 );
			key = track.CreateNodeKeyFrame( 20 );//K == A
			key.Translate = new Vector3( 300, 550, -300 );
			// Create a new animation state to track this

			m_AnimState = scene.CreateAnimationState( "LightTrack" );
			m_AnimState.IsEnabled = true;

		}

		protected override bool OnFrameStarted( object source, FrameEventArgs e )
		{

            if ( base.OnFrameStarted( source, e ) == false )
                return false;

            m_AnimState.AddTime( e.TimeSinceLastFrame );

            float xinc = Math.Utility.PI * 0.4f;
			float zinc = Math.Utility.PI * 0.55f;
			float xpos = Math.Utility.RangeRandom( -Math.Utility.PI, Math.Utility.PI );
			float zpos = Math.Utility.RangeRandom( -Math.Utility.PI, Math.Utility.PI );

			xpos += xinc * e.TimeSinceLastFrame;
			zpos += zinc * e.TimeSinceLastFrame;

			// Update vertex program parameters by binding a value to each renderable
			Vector4 offset = new Vector4( 0, 0, 0, 0 );

			foreach ( Axiom.Core.StaticGeometry.Region reg in m_StaticGeom.RegionMap.Values )
			{
				//a little randomness
				xpos += reg.Center.x * 0.001f;
				zpos += reg.Center.z * 0.001f;
				offset.x = Math.Utility.Sin( xpos ) * 5;
				offset.z = Math.Utility.Sin( zpos ) * 5;

				foreach ( StaticGeometry.LODBucket lod in reg.LodBucketList )
				{
					foreach ( StaticGeometry.MaterialBucket mat in lod.MaterialBucketMap.Values )
					{
						foreach ( StaticGeometry.GeometryBucket geom in mat.GeometryBucketList )
						{

							geom.SetCustomParameter( OFFSET_PARAM, offset );
						}
					}
				}
			}

            return true;

		}//end function


	}
}

