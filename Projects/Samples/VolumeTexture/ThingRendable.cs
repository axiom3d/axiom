using System.Collections.Generic;
using Axiom.Core;
using Axiom.Graphics;
using Axiom.Math;

namespace Axiom.Samples.VolumeTexture
{
	/// <summary>
	/// Quad fragments that rotate around origin (0,0,0) in a random orbit, always oriented to 0,0,0.
	/// </summary>
	/// <OriginalAuthor>W.J. van der Laan</OriginalAuthor>
	public class ThingRendable : SimpleRenderable
	{
		protected HardwareVertexBuffer vertexBuffer;
		protected Real radius;
		protected int count;
		protected float qSize;
		protected List<Quaternion> things = new List<Quaternion>();
		protected List<Quaternion> orbits = new List<Quaternion>();

		public override Real BoundingRadius
		{
			get { return radius; }
		}
		/// <summary>
		/// Default ctor.
		/// </summary>
		/// <param name="radius">Radius of orbits</param>
		/// <param name="count">Number of quads</param>
		/// <param name="qSize">Size of quads</param>
		public ThingRendable( float radius, int count, float qSize )
		{
			this.radius = radius;
			this.count = count;
			this.qSize = qSize;

			box = new AxisAlignedBox( new Vector3( -radius, -radius, -radius ), new Vector3( radius, radius, radius ) );
			Initialize();
			FillBuffer();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="disposeManagedResources"></param>
		protected override void dispose( bool disposeManagedResources )
		{
			if ( disposeManagedResources )
			{
				renderOperation.indexData.Dispose();
				renderOperation.vertexData.Dispose();
			}
			base.dispose( disposeManagedResources );
		}

		/// <summary>
		/// Notify that t seconds have elapsed.
		/// </summary>
		/// <param name="time">time elapsed</param>
		public void AddTime( float elapsedTime )
		{
			for ( int x = 0; x < count; x++ )
			{
				Quaternion dest = things[ x ] * orbits[ x ];
				things[ x ] = things[ x ] + elapsedTime * ( dest - things[ x ] );
				things[ x ].Normalize();
			}

			FillBuffer();
		}

		/// <summary>
		/// Returns the camera-relative squared depth of this renderable.
		/// </summary>
		/// <param name="camera"></param>
		/// <returns></returns>
		public override Real GetSquaredViewDepth( Camera camera )
		{
			Vector3 min, max, mid, dist;

			min = box.Minimum;
			max = box.Maximum;
			mid = ( ( min - max ) * 0.5 ) + min;
			dist = camera.DerivedPosition - mid;

			return dist.LengthSquared;
		}

		/// <summary>
		/// 
		/// </summary>
		protected void Initialize()
		{
			Vector3 ax = Vector3.Zero, ay = Vector3.Zero, az = Vector3.Zero;
			int x = 0;
			Quaternion q = Quaternion.Identity;
			things.Clear();
			orbits.Clear();

			for ( x = 0; x < count; x++ )
			{
				ax = new Vector3( GenerateRandomFloat(), GenerateRandomFloat(), GenerateRandomFloat() );
				ay = new Vector3( GenerateRandomFloat(), GenerateRandomFloat(), GenerateRandomFloat() );
				az = ax.Cross( ay );
				ay = az.Cross( ax );
				ax.Normalize();
				ay.Normalize();
				az.Normalize();
				q = Quaternion.FromAxes( ax, ay, az );
				things.Add( q );

				ax = new Vector3( GenerateRandomFloat(), GenerateRandomFloat(), GenerateRandomFloat() );
				ay = new Vector3( GenerateRandomFloat(), GenerateRandomFloat(), GenerateRandomFloat() );
				az = ax.Cross( ay );
				ay = az.Cross( ax );
				ax.Normalize();
				ay.Normalize();
				az.Normalize();
				q = Quaternion.FromAxes( ax, ay, az );
				orbits.Add( q );
			}

			int nVertices = count * 4;

			IndexData indexData  = new IndexData();
			VertexData vertexData = new VertexData();

			//Quads
			short[] faces = new short[ count * 6 ];
			for ( x = 0; x < count; x++ )
			{
				faces[ x * 6 + 0 ] = (short)( x * 4 + 0 );
				faces[ x * 6 + 1 ] = (short)( x * 4 + 1 );
				faces[ x * 6 + 2 ] = (short)( x * 4 + 2 );
				faces[ x * 6 + 3 ] = (short)( x * 4 + 0 );
				faces[ x * 6 + 4 ] = (short)( x * 4 + 2 );
				faces[ x * 6 + 5 ] = (short)( x * 4 + 3 );
			}

			vertexData.vertexStart = 0;
			vertexData.vertexCount = nVertices;

			VertexDeclaration decl = vertexData.vertexDeclaration;
			VertexBufferBinding bind = vertexData.vertexBufferBinding;

			int offset = 0;
			offset += decl.AddElement( 0, offset, VertexElementType.Float3, VertexElementSemantic.Position ).Size;

			vertexBuffer = HardwareBufferManager.Instance.CreateVertexBuffer( decl.Clone(0), nVertices, BufferUsage.DynamicWriteOnly );

			bind.SetBinding( 0, vertexBuffer );

			HardwareIndexBuffer indexBuffer = HardwareBufferManager.Instance.CreateIndexBuffer( IndexType.Size16, count * 6, BufferUsage.StaticWriteOnly );
			indexData.indexBuffer = indexBuffer;
			indexData.indexStart = 0;
			indexData.indexCount = count * 6;
            
			indexBuffer.WriteData( 0, indexBuffer.Size, faces, true );

			faces = null;

			renderOperation.operationType = OperationType.TriangleList;
			renderOperation.indexData = indexData;
			renderOperation.vertexData = vertexData;
            renderOperation.useIndices = true;
		}

		/// <summary>
		/// 
		/// </summary>
		protected void FillBuffer()
		{
			unsafe
			{
				// Transfer vertices and normals
				var vIdx = vertexBuffer.Lock( BufferLocking.Discard ).ToFloatPointer();
				int elemsize = 1 * 3; // position only
				int planesize = 4 * elemsize; // four vertices per plane
				for ( int x = 0; x < count; x++ )
				{
					Vector3 ax, ay, az;
					things[ x ].ToAxes( out ax, out ay, out az );
					Vector3 pos = az * radius; // scale to radius
					ax *= qSize;
					ay *= qSize;
					Vector3 pos1 = pos - ax - ay;
					Vector3 pos2 = pos + ax - ay;
					Vector3 pos3 = pos + ax + ay;
					Vector3 pos4 = pos - ax + ay;
					vIdx[ x * planesize + 0 * elemsize + 0 ] = pos1.x;
					vIdx[ x * planesize + 0 * elemsize + 1 ] = pos1.y;
					vIdx[ x * planesize + 0 * elemsize + 2 ] = pos1.z;
					vIdx[ x * planesize + 1 * elemsize + 0 ] = pos2.x;
					vIdx[ x * planesize + 1 * elemsize + 1 ] = pos2.y;
					vIdx[ x * planesize + 1 * elemsize + 2 ] = pos2.z;
					vIdx[ x * planesize + 2 * elemsize + 0 ] = pos3.x;
					vIdx[ x * planesize + 2 * elemsize + 1 ] = pos3.y;
					vIdx[ x * planesize + 2 * elemsize + 2 ] = pos3.z;
					vIdx[ x * planesize + 3 * elemsize + 0 ] = pos4.x;
					vIdx[ x * planesize + 3 * elemsize + 1 ] = pos4.y;
					vIdx[ x * planesize + 3 * elemsize + 2 ] = pos4.z;
				}
				vertexBuffer.Unlock();
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		protected float GenerateRandomFloat()
		{
			return Utility.RangeRandom( -1, 1 );
		}
	}
}
