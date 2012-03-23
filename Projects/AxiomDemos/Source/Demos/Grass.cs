#region Namespace Declarations

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Text;

using Axiom.Math;
using Axiom.Core;
using Axiom.Animating;
using Axiom.Controllers;
using Axiom.Demos;
using Axiom.Controllers.Canned;
using Axiom.Graphics;

#endregion Namespace Declarations

namespace Axiom.Demos
{
	public class LightGrassWibbler : Controllers.IControllerValue<Real>
	{
		protected Light light;
		protected Billboard billboard;
		protected ColorEx colorRange = new ColorEx();
		protected ColorEx halfColor = new ColorEx();
		protected Real minSize;
		protected Real sizeRange;
		protected Real intensity;

		public LightGrassWibbler( Light light, Billboard billboard, ColorEx minColor, ColorEx maxColor, int minSize, int maxSize )
		{
			this.light = light;
			this.billboard = billboard;

			this.colorRange.r = ( maxColor.r - minColor.r ) * 0.5f;
			this.colorRange.g = ( maxColor.g - minColor.g ) * 0.5f;
			this.colorRange.b = ( maxColor.b - minColor.b ) * 0.5f;

			this.halfColor.r = ( minColor.r + colorRange.r ); // 2;
			this.halfColor.g = ( minColor.g + colorRange.g ); // 2;
			this.halfColor.b = ( minColor.b + colorRange.b ); // 2;

			this.minSize = minSize;
			this.sizeRange = maxSize - minSize;
		}

		#region IControllerValue<Real> Members

		public Real Value
		{
			get
			{
				return intensity;
			}
			set
			{
				intensity = value;

				ColorEx newColor = new ColorEx();
				//atenuate the brightness of the light
				newColor.r = halfColor.r + ( colorRange.r * intensity );
				newColor.g = halfColor.g + ( colorRange.g * intensity );
				newColor.b = halfColor.b + ( colorRange.b * intensity );

				this.light.Diffuse = newColor;
				this.billboard.Color = newColor;

				Real newSize = minSize + ( intensity * sizeRange );
				this.billboard.SetDimensions( newSize, newSize );
			}
		}

		#endregion IControllerValue<Real> Members
	}

	[Export( typeof ( TechDemo ) )]
	public class Grass : TechDemo
	{
		protected const float GRASS_HEIGHT = 300;
		protected const float GRASS_WIDTH = 250;
		protected const string GRASS_MESH_NAME = "grassblades";
		protected string GRASS_MATERIAL = "Examples/GrassBlades";
		protected const int OFFSET_PARAM = 999;
		protected Real extraOffset = 0.1f;
		protected Real randomRange = 60;
		protected bool backward = false;

		protected Light Light;
		protected SceneNode LightNode;
		protected AnimationState AnimState;

		protected readonly ColorEx MinLightColour = new ColorEx( 0.5f, 0.1f, 0.0f );

		protected readonly ColorEx MaxLightColour = new ColorEx( 1.0f, 0.6f, 0.0f );

		protected int MinFlareSize = 40;
		protected int MaxFlareSize = 80;
		protected StaticGeometry StaticGeom;
		protected SceneNode HeadNode;

		public override void CreateScene()
		{
			scene.SetSkyBox( true, "Skybox/Space", 10000 );

			SetupLighting();

			Plane plane = new Plane();
			plane.Normal = Vector3.UnitY;
			plane.D = 0;

			MeshManager.Instance.CreatePlane( "MyPlane", ResourceGroupManager.DefaultResourceGroupName, plane, 14500, 14500, 10, 10, true, 1, 50, 50, Vector3.UnitZ );

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
			s.Origin = new Vector3( -500, 500, -500 ); //Set the region origin so the centre is at 0 world

			for ( int x = -1950; x < 1950; x += 150 )
			{
				for ( int z = -1950; z < 1950; z += 150 )
				{
					Vector3 pos = new Vector3( x + Math.Utility.RangeRandom( -25, 25 ), 0, z + Math.Utility.RangeRandom( -25, 25 ) );

					Quaternion orientation = Quaternion.FromAngleAxis( Math.Utility.RangeRandom( 0, 359 ), Vector3.UnitY );

					Vector3 scale = new Vector3( 1, Math.Utility.RangeRandom( 0.85f, 1.15f ), 1 );

					s.AddEntity( e, pos, orientation, scale );
				}
			}
			s.Build();
			StaticGeom = s;

			Mesh mesh = MeshManager.Instance.Load( "ogrehead.mesh", ResourceGroupManager.DefaultResourceGroupName );

			short src, dest;
			if ( !mesh.SuggestTangentVectorBuildParams( out src, out dest ) )
			{
				mesh.BuildTangentVectors( src, dest );
			}

			e = scene.CreateEntity( "head", "ogrehead.mesh" );
			e.MaterialName = "Examples/OffsetMapping/Specular";

			HeadNode = scene.RootSceneNode.CreateChildSceneNode();
			HeadNode.AttachObject( e );
			HeadNode.Scale = new Vector3( 7, 7, 7 );
			HeadNode.Position = new Vector3( 0, 200, 0 );

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
				var pData = vbuf.Lock( BufferLocking.Discard ).ToFloatPointer();
				var idx = 0;

				Vector3 baseVec = new Vector3( GRASS_WIDTH / 2, 0, 0 );
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
				var pI = ( sm.indexData.indexBuffer.Lock( BufferLocking.Discard ) ).ToUShortPointer();
				var idx = 0;

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
			sm.MaterialName = GRASS_MATERIAL;

			msh.Load();
		}

		private void SetupLighting()
		{
			scene.AmbientLight = new ColorEx( 0.2f, 0.2f, 0.2f );
			Light = scene.CreateLight( "Light2" );
			Light.Diffuse = new ColorEx( MinLightColour );
			Light.SetAttenuation( 8000, 1, 0.0005f, 0 );
			Light.Specular = new ColorEx( 1, 1, 1 );

			LightNode = scene.RootSceneNode.CreateChildSceneNode( "MovingLightNode" );
			LightNode.AttachObject( Light );
			//create billboard set

			BillboardSet bbs = scene.CreateBillboardSet( "lightbbs", 1 );
			bbs.MaterialName = "Examples/Flare";
			Billboard bb = bbs.CreateBillboard( new Vector3( 0, 0, 0 ), MinLightColour );
			LightNode.AttachObject( bbs );

			LightGrassWibbler val = new LightGrassWibbler( Light, bb, MinLightColour, this.MaxLightColour, MinFlareSize, MaxFlareSize );

			// create controller, after this is will get updated on its own
			WaveformControllerFunction func = new WaveformControllerFunction( WaveformType.Sine, 0.0f, 0.5f );

			ControllerManager.Instance.CreateController( val, func );

			LightNode.Position = new Vector3( 300, 250, -300 );

			Animation anim = scene.CreateAnimation( "LightTrack", 20 );
			//Spline it for nce curves
			anim.InterpolationMode = InterpolationMode.Spline;
			//create a srtack to animte the camera's node
			NodeAnimationTrack track = anim.CreateNodeTrack( 0, LightNode );
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

			AnimState = scene.CreateAnimationState( "LightTrack" );
			AnimState.IsEnabled = true;
		}

		protected override void OnFrameStarted( object source, FrameEventArgs evt )
		{
			base.OnFrameStarted( source, evt );
			if ( evt.StopRendering )
			{
				return;
			}

			// animate Light Wibbler
			AnimState.AddTime( evt.TimeSinceLastFrame );

			randomRange = Math.Utility.RangeRandom( 20, 100 );

			if ( !backward )
			{
				extraOffset += 0.5f;
				if ( extraOffset > randomRange )
				{
					backward = true;
				}
			}
			if ( backward )
			{
				extraOffset -= 0.5f;
				if ( extraOffset < 0.02f )
				{
					backward = false;
				}
			}

			// we are animating the static mesh ( Entity ) here with a simple offset
			foreach ( Axiom.Core.StaticGeometry.Region reg in StaticGeom.RegionMap.Values )
			{
				foreach ( StaticGeometry.LODBucket lod in reg.LodBucketList )
				{
					foreach ( StaticGeometry.MaterialBucket mat in lod.MaterialBucketMap.Values )
					{
						foreach ( StaticGeometry.GeometryBucket geom in mat.GeometryBucketList )
						{
							geom.SetCustomParameter( OFFSET_PARAM, new Vector4( extraOffset, 0, 0, 0 ) );
						}
					}
				}
			}
		}

		//end function
	}
}
