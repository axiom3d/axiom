#region Namespace Declarations

using System.ComponentModel.Composition;

using Axiom.Animating;
using Axiom.Controllers;
using Axiom.Controllers.Canned;
using Axiom.Core;
using Axiom.Graphics;
using Axiom.Math;

#endregion Namespace Declarations

namespace Axiom.Demos
{
	public class LightGrassWibbler : IControllerValue<Real>
	{
		protected Billboard billboard;
		protected ColorEx colorRange;
		protected ColorEx halfColor;
		protected Real intensity;
		protected Light light;
		protected Real minSize;
		protected Real sizeRange;

		public LightGrassWibbler( Light light, Billboard billboard, ColorEx minColor, ColorEx maxColor, int minSize, int maxSize )
		{
			this.light = light;
			this.billboard = billboard;

			this.colorRange.r = ( maxColor.r - minColor.r ) * 0.5f;
			this.colorRange.g = ( maxColor.g - minColor.g ) * 0.5f;
			this.colorRange.b = ( maxColor.b - minColor.b ) * 0.5f;

			this.halfColor.r = ( minColor.r + this.colorRange.r ); // 2;
			this.halfColor.g = ( minColor.g + this.colorRange.g ); // 2;
			this.halfColor.b = ( minColor.b + this.colorRange.b ); // 2;

			this.minSize = minSize;
			this.sizeRange = maxSize - minSize;
		}

		#region IControllerValue<Real> Members

		public Real Value
		{
			get
			{
				return this.intensity;
			}
			set
			{
				this.intensity = value;

				var newColor = new ColorEx();
				//atenuate the brightness of the light
				newColor.r = this.halfColor.r + ( this.colorRange.r * this.intensity );
				newColor.g = this.halfColor.g + ( this.colorRange.g * this.intensity );
				newColor.b = this.halfColor.b + ( this.colorRange.b * this.intensity );

				this.light.Diffuse = newColor;
				this.billboard.Color = newColor;

				Real newSize = this.minSize + ( this.intensity * this.sizeRange );
				this.billboard.SetDimensions( newSize, newSize );
			}
		}

		#endregion
	}

	[Export( typeof( TechDemo ) )]
	public class Grass : TechDemo
	{
		protected const float GRASS_HEIGHT = 300;
		protected const float GRASS_WIDTH = 250;
		protected const string GRASS_MESH_NAME = "grassblades";
		protected const int OFFSET_PARAM = 999;
		protected readonly ColorEx MaxLightColour = new ColorEx( 1.0f, 0.6f, 0.0f );
		protected readonly ColorEx MinLightColour = new ColorEx( 0.5f, 0.1f, 0.0f );
		protected AnimationState AnimState;
		protected string GRASS_MATERIAL = "Examples/GrassBlades";
		protected SceneNode HeadNode;

		protected Light Light;
		protected SceneNode LightNode;
		protected int MaxFlareSize = 80;
		protected int MinFlareSize = 40;
		protected StaticGeometry StaticGeom;
		protected bool backward;
		protected Real extraOffset = 0.1f;
		protected Real randomRange = 60;

		public override void CreateScene()
		{
			scene.SetSkyBox( true, "Skybox/Space", 10000 );

			SetupLighting();

			var plane = new Plane();
			plane.Normal = Vector3.UnitY;
			plane.D = 0;

			MeshManager.Instance.CreatePlane( "MyPlane", ResourceGroupManager.DefaultResourceGroupName, plane, 14500, 14500, 10, 10, true, 1, 50, 50, Vector3.UnitZ );

			Entity planeEnt = scene.CreateEntity( "plane", "MyPlane" );
			planeEnt.MaterialName = "Examples/GrassFloor";
			planeEnt.CastShadows = false;

			scene.RootSceneNode.CreateChildSceneNode().AttachObject( planeEnt );

			var minV = new Vector3( -2000, 0, -2000 );
			var maxV = new Vector3( 2000, 0, 2000 );

			CreateGrassMesh();

			Entity e = scene.CreateEntity( "1", GRASS_MESH_NAME );
			StaticGeometry s = scene.CreateStaticGeometry( "bing", 1 );
			s.RegionDimensions = new Vector3( 1000, 1000, 1000 );
			s.Origin = new Vector3( -500, 500, -500 ); //Set the region origin so the centre is at 0 world

			for ( int x = -1950; x < 1950; x += 150 )
			{
				for ( int z = -1950; z < 1950; z += 150 )
				{
					var pos = new Vector3( x + Utility.RangeRandom( -25, 25 ), 0, z + Utility.RangeRandom( -25, 25 ) );

					Quaternion orientation = Quaternion.FromAngleAxis( Utility.RangeRandom( 0, 359 ), Vector3.UnitY );

					var scale = new Vector3( 1, Utility.RangeRandom( 0.85f, 1.15f ), 1 );

					s.AddEntity( e, pos, orientation, scale );
				}
			}
			s.Build();
			this.StaticGeom = s;

			Mesh mesh = MeshManager.Instance.Load( "ogrehead.mesh", ResourceGroupManager.DefaultResourceGroupName );

			short src, dest;
			if ( !mesh.SuggestTangentVectorBuildParams( out src, out dest ) )
			{
				mesh.BuildTangentVectors( src, dest );
			}

			e = scene.CreateEntity( "head", "ogrehead.mesh" );
			e.MaterialName = "Examples/OffsetMapping/Specular";

			this.HeadNode = scene.RootSceneNode.CreateChildSceneNode();
			this.HeadNode.AttachObject( e );
			this.HeadNode.Scale = new Vector3( 7, 7, 7 );
			this.HeadNode.Position = new Vector3( 0, 200, 0 );

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
			Mesh msh = MeshManager.Instance.CreateManual( GRASS_MESH_NAME, ResourceGroupManager.DefaultResourceGroupName, null );

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

			HardwareVertexBuffer vbuf = HardwareBufferManager.Instance.CreateVertexBuffer( dcl, 12, BufferUsage.StaticWriteOnly );

			int i;
#if !AXIOM_SAFE_ONLY
			unsafe
#endif
			{
				float* pData = vbuf.Lock( BufferLocking.Discard ).ToFloatPointer();
				int idx = 0;

				var baseVec = new Vector3( GRASS_WIDTH / 2, 0, 0 );
				Vector3 vec = baseVec;
				Quaternion rot = Quaternion.FromAngleAxis( Utility.DegreesToRadians( 60 ), Vector3.UnitY );

				for ( i = 0; i < 3; ++i )
				{
					//position
					pData[ idx++ ] = -vec.x;
					pData[ idx++ ] = GRASS_HEIGHT;
					pData[ idx++ ] = -vec.z;
					// normal
					pData[ idx++ ] = 0;
					pData[ idx++ ] = 1;
					pData[ idx++ ] = 0;
					// uv
					pData[ idx++ ] = 0;
					pData[ idx++ ] = 0;

					// position
					pData[ idx++ ] = vec.x;
					pData[ idx++ ] = GRASS_HEIGHT;
					pData[ idx++ ] = vec.z;
					// normal
					pData[ idx++ ] = 0;
					pData[ idx++ ] = 1;
					pData[ idx++ ] = 0;
					// uv
					pData[ idx++ ] = 1;
					pData[ idx++ ] = 0;

					// position
					pData[ idx++ ] = -vec.x;
					pData[ idx++ ] = 0;
					pData[ idx++ ] = -vec.z;
					// normal
					pData[ idx++ ] = 0;
					pData[ idx++ ] = 1;
					pData[ idx++ ] = 0;
					// uv
					pData[ idx++ ] = 0;
					pData[ idx++ ] = 1;

					// position
					pData[ idx++ ] = vec.x;
					pData[ idx++ ] = 0;
					pData[ idx++ ] = vec.z;
					// normal
					pData[ idx++ ] = 0;
					pData[ idx++ ] = 1;
					pData[ idx++ ] = 0;
					// uv
					pData[ idx++ ] = 1;
					pData[ idx++ ] = 1;

					vec = rot * vec;
				} //for
			} //unsafe

			vbuf.Unlock();

			sm.vertexData.vertexBufferBinding.SetBinding( 0, vbuf );
			sm.indexData.indexCount = 6 * 3;
			sm.indexData.indexBuffer = HardwareBufferManager.Instance.CreateIndexBuffer( IndexType.Size16, 6 * 3, BufferUsage.StaticWriteOnly );

#if !AXIOM_SAFE_ONLY
			unsafe
#endif
			{
				ushort* pI = ( sm.indexData.indexBuffer.Lock( BufferLocking.Discard ) ).ToUShortPointer();
				int idx = 0;

				for ( i = 0; i < 3; ++i )
				{
					int off = i * 4;
					pI[ idx++ ] = (ushort)( off );
					pI[ idx++ ] = (ushort)( off + 3 );
					pI[ idx++ ] = (ushort)( off + 1 );

					pI[ idx++ ] = (ushort)( off + 0 );
					pI[ idx++ ] = (ushort)( off + 2 );
					pI[ idx++ ] = (ushort)( off + 3 );
				}
			}

			sm.indexData.indexBuffer.Unlock();
			sm.MaterialName = this.GRASS_MATERIAL;

			msh.Load();
		}

		private void SetupLighting()
		{
			scene.AmbientLight = new ColorEx( 0.2f, 0.2f, 0.2f );
			this.Light = scene.CreateLight( "Light2" );
			this.Light.Diffuse = new ColorEx( this.MinLightColour );
			this.Light.SetAttenuation( 8000, 1, 0.0005f, 0 );
			this.Light.Specular = new ColorEx( 1, 1, 1 );

			this.LightNode = scene.RootSceneNode.CreateChildSceneNode( "MovingLightNode" );
			this.LightNode.AttachObject( this.Light );
			//create billboard set

			BillboardSet bbs = scene.CreateBillboardSet( "lightbbs", 1 );
			bbs.MaterialName = "Examples/Flare";
			Billboard bb = bbs.CreateBillboard( new Vector3( 0, 0, 0 ), this.MinLightColour );
			this.LightNode.AttachObject( bbs );

			var val = new LightGrassWibbler( this.Light, bb, this.MinLightColour, this.MaxLightColour, this.MinFlareSize, this.MaxFlareSize );

			// create controller, after this is will get updated on its own
			var func = new WaveformControllerFunction( WaveformType.Sine, 0.0f, 0.5f );

			ControllerManager.Instance.CreateController( val, func );

			this.LightNode.Position = new Vector3( 300, 250, -300 );

			Animation anim = scene.CreateAnimation( "LightTrack", 20 );
			//Spline it for nce curves
			anim.InterpolationMode = InterpolationMode.Spline;
			//create a srtack to animte the camera's node
			NodeAnimationTrack track = anim.CreateNodeTrack( 0, this.LightNode );
			//setup keyframes
			TransformKeyFrame key = track.CreateNodeKeyFrame( 0 );
			key.Translate = new Vector3( 300, 550, -300 );
			key = track.CreateNodeKeyFrame( 2 ); //B
			key.Translate = new Vector3( 150, 600, -250 );
			key = track.CreateNodeKeyFrame( 4 ); //C
			key.Translate = new Vector3( -150, 650, -100 );
			key = track.CreateNodeKeyFrame( 6 ); //D
			key.Translate = new Vector3( -400, 500, -200 );
			key = track.CreateNodeKeyFrame( 8 ); //E
			key.Translate = new Vector3( -200, 500, -400 );
			key = track.CreateNodeKeyFrame( 10 ); //F
			key.Translate = new Vector3( -100, 450, -200 );
			key = track.CreateNodeKeyFrame( 12 ); //G
			key.Translate = new Vector3( -100, 400, 180 );
			key = track.CreateNodeKeyFrame( 14 ); //H
			key.Translate = new Vector3( 0, 250, 600 );
			key = track.CreateNodeKeyFrame( 16 ); //I
			key.Translate = new Vector3( 100, 650, 100 );
			key = track.CreateNodeKeyFrame( 18 ); //J
			key.Translate = new Vector3( 250, 600, 0 );
			key = track.CreateNodeKeyFrame( 20 ); //K == A
			key.Translate = new Vector3( 300, 550, -300 );
			// Create a new animation state to track this

			this.AnimState = scene.CreateAnimationState( "LightTrack" );
			this.AnimState.IsEnabled = true;
		}

		protected override void OnFrameStarted( object source, FrameEventArgs evt )
		{
			base.OnFrameStarted( source, evt );
			if ( evt.StopRendering )
			{
				return;
			}

			// animate Light Wibbler
			this.AnimState.AddTime( evt.TimeSinceLastFrame );

			this.randomRange = Utility.RangeRandom( 20, 100 );

			if ( !this.backward )
			{
				this.extraOffset += 0.5f;
				if ( this.extraOffset > this.randomRange )
				{
					this.backward = true;
				}
			}
			if ( this.backward )
			{
				this.extraOffset -= 0.5f;
				if ( this.extraOffset < 0.02f )
				{
					this.backward = false;
				}
			}

			// we are animating the static mesh ( Entity ) here with a simple offset
			foreach ( StaticGeometry.Region reg in this.StaticGeom.RegionMap.Values )
			{
				foreach ( StaticGeometry.LODBucket lod in reg.LodBucketList )
				{
					foreach ( StaticGeometry.MaterialBucket mat in lod.MaterialBucketMap.Values )
					{
						foreach ( StaticGeometry.GeometryBucket geom in mat.GeometryBucketList )
						{
							geom.SetCustomParameter( OFFSET_PARAM, new Vector4( this.extraOffset, 0, 0, 0 ) );
						}
					}
				}
			}
		}

		//end function
	}
}
