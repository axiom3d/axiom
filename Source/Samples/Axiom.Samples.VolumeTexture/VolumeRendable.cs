using System.Collections.Generic;
using Axiom.Math;
using Axiom.Core;
using Axiom.Graphics;

namespace Axiom.Samples.VolumeTexture
{
	public class VolumeRendable : SimpleRenderable
	{
		protected int slices;
		protected float size;
		protected Real radius;
		protected Matrix3 fakeOrientation;
		protected string texture;
		protected TextureUnitState unit;

		/// <summary>
		/// 
		/// </summary>
		public override Real BoundingRadius
		{
			get
			{
				return this.radius;
			}
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="slices"></param>
		/// <param name="size"></param>
		/// <param name="texture"></param>
		public VolumeRendable( int slices, int size, string texture )
		{
			this.slices = slices;
			this.size = size;
			this.texture = texture;

			this.radius = Utility.Sqrt( size*size + size*size + size*size )/2.0f;
			box = new AxisAlignedBox( new Vector3( -size, -size, -size ), new Vector3( size, size, size ) );

			CastShadows = false;

			Initialize();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="disposeManagedResources"></param>
		protected override void dispose( bool disposeManagedResources )
		{
			if ( disposeManagedResources )
			{
				if ( TextureManager.Instance != null )
				{
					TextureManager.Instance.Remove( this.texture );
				}
			}
			base.dispose( disposeManagedResources );
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="camera"></param>
		public override void NotifyCurrentCamera( Camera camera )
		{
			base.NotifyCurrentCamera( camera );


			///Fake orientation toward camera
			Vector3 zVec = ParentNode.DerivedPosition - camera.DerivedPosition;
			zVec.Normalize();

			Vector3 fixedAxis = camera.DerivedOrientation*Vector3.UnitY;

			Vector3 xVec = fixedAxis.Cross( zVec );
			xVec.Normalize();

			Vector3 yVec = zVec.Cross( xVec );
			yVec.Normalize();

			Quaternion oriQuat = Quaternion.FromAxes( xVec, yVec, zVec );

			this.fakeOrientation = oriQuat.ToRotationMatrix();

			Quaternion q = ParentNode.DerivedOrientation.UnitInverse*oriQuat;
			Matrix3 tempMat = q.ToRotationMatrix();

			Matrix4 rotMat = Matrix4.Identity;
			rotMat = tempMat;
			rotMat.Translation = new Vector3( 0.5f, 0.5f, 0.5f );

			this.unit.TextureMatrix = rotMat;
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="matrices"></param>
		public override void GetWorldTransforms( Matrix4[] matrices )
		{
			// this initialisation is needed
			Matrix4 destMatrix = Matrix4.Identity;

			Vector3 position = ParentNode.DerivedPosition;
			Vector3 scale = ParentNode.DerivedScale;
			Matrix3 scale3x3 = Matrix3.Zero;
			scale3x3.m00 = scale.x;
			scale3x3.m11 = scale.y;
			scale3x3.m22 = scale.z;

			destMatrix = this.fakeOrientation*scale3x3;
			destMatrix.Translation = position;
			matrices[ 0 ] = destMatrix;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="camera"></param>
		/// <returns></returns>
		public override Real GetSquaredViewDepth( Camera camera )
		{
			Vector3 min, max, mid, dist;

			min = box.Minimum;
			max = box.Maximum;
			mid = ( ( min - max )*0.5 ) + min;
			dist = camera.DerivedPosition - mid;

			return dist.LengthSquared;
		}

		/// <summary>
		/// 
		/// </summary>
		protected void Initialize()
		{
			// Create geometry
			int nvertices = this.slices*4; // n+1 planes
			int elemsize = 3*3;
			int dsize = elemsize*nvertices;
			int x;

			var indexData = new IndexData();
			var vertexData = new VertexData();
			var vertices = new float[dsize];

			var coords = new float[4,2]
			             {
			             	{
			             		0.0f, 0.0f
			             	}, {
			             	   	0.0f, 1.0f
			             	   }, {
			             	      	1.0f, 0.0f
			             	      }, {
			             	         	1.0f, 1.0f
			             	         }
			             };

			for ( x = 0; x < this.slices; x++ )
			{
				for ( int y = 0; y < 4; y++ )
				{
					float xcoord = coords[ y, 0 ] - 0.5f;
					float ycoord = coords[ y, 1 ] - 0.5f;
					float zcoord = -( (float)x/(float)( this.slices - 1 ) - 0.5f );
					// 1.0f .. a/(a+1)
					// coordinate
					vertices[ x*4*elemsize + y*elemsize + 0 ] = xcoord*( this.size/2.0f );
					vertices[ x*4*elemsize + y*elemsize + 1 ] = ycoord*( this.size/2.0f );
					vertices[ x*4*elemsize + y*elemsize + 2 ] = zcoord*( this.size/2.0f );
					// normal
					vertices[ x*4*elemsize + y*elemsize + 3 ] = 0.0f;
					vertices[ x*4*elemsize + y*elemsize + 4 ] = 0.0f;
					vertices[ x*4*elemsize + y*elemsize + 5 ] = 1.0f;
					// tex
					vertices[ x*4*elemsize + y*elemsize + 6 ] = xcoord*Utility.Sqrt( 3.0f );
					vertices[ x*4*elemsize + y*elemsize + 7 ] = ycoord*Utility.Sqrt( 3.0f );
					vertices[ x*4*elemsize + y*elemsize + 8 ] = zcoord*Utility.Sqrt( 3.0f );
				}
			}

			var faces = new short[this.slices*6];
			for ( x = 0; x < this.slices; x++ )
			{
				faces[ x*6 + 0 ] = (short)( x*4 + 0 );
				faces[ x*6 + 1 ] = (short)( x*4 + 1 );
				faces[ x*6 + 2 ] = (short)( x*4 + 2 );
				faces[ x*6 + 3 ] = (short)( x*4 + 1 );
				faces[ x*6 + 4 ] = (short)( x*4 + 2 );
				faces[ x*6 + 5 ] = (short)( x*4 + 3 );
			}

			//setup buffers
			vertexData.vertexStart = 0;
			vertexData.vertexCount = nvertices;

			VertexDeclaration decl = vertexData.vertexDeclaration;
			VertexBufferBinding bind = vertexData.vertexBufferBinding;
			int offset = 0;
			offset += decl.AddElement( 0, 0, VertexElementType.Float3, VertexElementSemantic.Position ).Size;
			offset += decl.AddElement( 0, offset, VertexElementType.Float3, VertexElementSemantic.Normal ).Size;
			offset += decl.AddElement( 0, offset, VertexElementType.Float3, VertexElementSemantic.TexCoords ).Size;

			HardwareVertexBuffer vertexBuffer = HardwareBufferManager.Instance.CreateVertexBuffer( decl, nvertices,
			                                                                                       BufferUsage.StaticWriteOnly );

			bind.SetBinding( 0, vertexBuffer );

			HardwareIndexBuffer indexBuffer = HardwareBufferManager.Instance.CreateIndexBuffer( IndexType.Size16, this.slices*6,
			                                                                                    BufferUsage.StaticWriteOnly );

			indexData.indexBuffer = indexBuffer;
			indexData.indexCount = this.slices*6;
			indexData.indexStart = 0;

			indexBuffer.WriteData( 0, indexBuffer.Size, faces, true );
			vertexBuffer.WriteData( 0, vertexBuffer.Size, vertices );
			vertices = null;
			faces = null;

			// Now make the render operation
			renderOperation.operationType = OperationType.TriangleList;
			renderOperation.indexData = indexData;
			renderOperation.vertexData = vertexData;
			renderOperation.useIndices = true;

			// Create a brand new private material
			if ( !ResourceGroupManager.Instance.GetResourceGroups().Contains( "VolumeRendable" ) )
			{
				ResourceGroupManager.Instance.CreateResourceGroup( "VolumeRendable" );
			}

			var material = (Material)MaterialManager.Instance.Create( this.texture, "VolumeRendable" );
			// Remove pre-created technique from defaults
			material.RemoveAllTechniques();

			// Create a techinique and a pass and a texture unit
			Technique technique = material.CreateTechnique();
			Pass pass = technique.CreatePass();
			TextureUnitState textureUnit = pass.CreateTextureUnitState();

			// Set pass parameters
			pass.SetSceneBlending( SceneBlendType.TransparentAlpha );
			pass.DepthWrite = false;
			pass.CullingMode = CullingMode.None;
			pass.LightingEnabled = false;
			textureUnit.SetTextureAddressingMode( TextureAddressing.Clamp );
			textureUnit.SetTextureName( this.texture, TextureType.ThreeD );
			textureUnit.SetTextureFiltering( TextureFiltering.Trilinear );

			this.unit = textureUnit;
			base.material = material;
		}
	}
}