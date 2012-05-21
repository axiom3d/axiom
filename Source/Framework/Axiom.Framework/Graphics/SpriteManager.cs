#region MIT/X11 License

//Copyright © 2003-2012 Axiom 3D Rendering Engine Project
//
//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:
//
//The above copyright notice and this permission notice shall be included in
//all copies or substantial portions of the Software.
//
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//THE SOFTWARE.

#endregion License

#region Namespace Declarations

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Axiom.Core;
using Axiom.Graphics;
using Axiom.Math;
using ResourceHandle = System.UInt64;

#endregion Namespace Declarations

namespace Axiom.Framework.Graphics
{
	public static class SpriteManager
	{
		#region Fields

		private static HardwareVertexBuffer hardwareBuffer;
		private static RenderOperation renderOp;
		private static SceneManager sceneMan;
		private static LinkedList<Sprite> sprites;

		#endregion Fields

		#region Properties

		public static bool AfterQueue { get; set; }

		public static int MinimalHardwareBufferSize { get; set; }

		public static RenderQueueGroupID TargetQueue { get; set; }

		#endregion Properties

		#region Methods

		public static void EnqueueTexture( string textureName, float x1, float y1, float x2, float y2, float alpha )
		{
			EnqueueTexture( textureName, x1, y1, x2, y2, 0, 0, 1, 1, alpha );
		}

		public static void EnqueueTexture( string textureName, float x1, float y1, float x2, float y2, float tx1, float ty1,
		                                   float tx2, float ty2, float alpha )
		{
			float z = -1.0f;

			var spr = new Sprite();
			spr.Alpha = alpha;

			spr.Pos = new Vector3[6];
			spr.UV = new Vector2[6];

			spr.Pos[ 0 ] = new Vector3( x1, y2, z );
			spr.UV[ 0 ] = new Vector2( tx1, ty2 );

			spr.Pos[ 1 ] = new Vector3( x2, y1, z );
			spr.UV[ 1 ] = new Vector2( tx2, ty1 );

			spr.Pos[ 2 ] = new Vector3( x1, y1, z );
			spr.UV[ 2 ] = new Vector2( tx1, ty1 );

			spr.Pos[ 3 ] = new Vector3( x1, y2, z );
			spr.UV[ 3 ] = new Vector2( tx1, ty2 );

			spr.Pos[ 4 ] = new Vector3( x2, y1, z );
			spr.UV[ 4 ] = new Vector2( tx2, ty1 );

			spr.Pos[ 5 ] = new Vector3( x2, y2, z );
			spr.UV[ 5 ] = new Vector2( tx2, ty2 );

			var tp = (Texture)TextureManager.Instance.GetByName( textureName );
			if ( tp == null || !tp.IsLoaded )
			{
				tp = TextureManager.Instance.Load( textureName, ResourceGroupManager.DefaultResourceGroupName );
			}

			spr.TexHandle = tp.Handle;
			tp.Dispose();

			if ( !sprites.Contains( spr ) )
			{
				sprites.AddLast( spr );
			}
		}

		public static void Initialize( SceneManager sceneManager )
		{
			sceneMan = sceneManager;
			TargetQueue = RenderQueueGroupID.Overlay;
			AfterQueue = false;
			MinimalHardwareBufferSize = 120;
			sprites = new LinkedList<Sprite>();
			sceneMan.QueueStarted += RenderQueueStarted;
			sceneMan.QueueEnded += RenderQueueEnded;
		}

		public static void Shutdown()
		{
			if ( hardwareBuffer != null )
			{
				HardwareBuffer_Destroy();
			}

			sceneMan.QueueStarted -= RenderQueueStarted;
			sceneMan.QueueEnded -= RenderQueueEnded;
		}

		private static void HardwareBuffer_Create( int size )
		{
			VertexDeclaration vd;

			renderOp = new RenderOperation();
			renderOp.vertexData = new VertexData();
			renderOp.vertexData.vertexStart = 0;

			vd = renderOp.vertexData.vertexDeclaration;
			vd.AddElement( 0, 0, VertexElementType.Float3, VertexElementSemantic.Position );

			vd.AddElement( 0, VertexElement.GetTypeSize( VertexElementType.Float3 ), VertexElementType.Float2,
			               VertexElementSemantic.TexCoords );

			hardwareBuffer = HardwareBufferManager.Instance.CreateVertexBuffer( vd.Clone( 0 ), size,
			                                                                    BufferUsage.DynamicWriteOnlyDiscardable, true );

			renderOp.vertexData.vertexBufferBinding.SetBinding( 0, hardwareBuffer );

			renderOp.operationType = OperationType.TriangleList;
			renderOp.useIndices = false;
		}

		private static void HardwareBuffer_Destroy()
		{
			hardwareBuffer.Dispose();
			renderOp.vertexData = null;
			renderOp = null;
		}

		private static void Render()
		{
			if ( sprites.Count == 0 )
			{
				return;
			}

			Axiom.Graphics.RenderSystem rs = Root.Instance.RenderSystem;

			var thisChunk = new Chunk();
			var chunks = new List<Chunk>();

			int newSize;

			newSize = sprites.Count*6;
			if ( newSize < MinimalHardwareBufferSize )
			{
				newSize = MinimalHardwareBufferSize;
			}

			// grow hardware buffer if needed
			if ( hardwareBuffer == null || hardwareBuffer.VertexCount < newSize )
			{
				if ( hardwareBuffer != null )
				{
					HardwareBuffer_Destroy();
				}

				HardwareBuffer_Create( newSize );
			}

			// write quads to the hardware buffer, and remember chunks
			unsafe
			{
				var buffer = (Vertex*)hardwareBuffer.Lock( BufferLocking.Discard ).Pin();

				LinkedListNode<Sprite> node = sprites.First;
				Sprite currSpr;

				while ( node != null )
				{
					currSpr = node.Value;
					thisChunk.Alpha = currSpr.Alpha;
					thisChunk.TexHandle = currSpr.TexHandle;

					for ( int i = 0; i < 6; i++ )
					{
						*buffer++ = new Vertex( currSpr.Pos[ i ], currSpr.UV[ i ] );
					}

					thisChunk.VertexCount += 6;

					node = node.Next;

					if ( node == null || thisChunk.TexHandle != node.Value.TexHandle || thisChunk.Alpha != node.Value.Alpha )
					{
						chunks.Add( thisChunk );
						thisChunk.VertexCount = 0;
					}
				}
			}

			hardwareBuffer.Unlock();

			// set up...
			RenderSystem_Setup();

			// do the real render!
			// do the real render!
			Texture tp = null;
			renderOp.vertexData.vertexStart = 0;
			foreach ( Chunk currChunk in chunks )
			{
				renderOp.vertexData.vertexCount = currChunk.VertexCount;
				tp = (Texture)TextureManager.Instance.GetByHandle( currChunk.TexHandle );
				rs.SetTexture( 0, true, tp.Name );
				rs.SetTextureUnitFiltering( 0, FilterOptions.Linear, FilterOptions.Linear, FilterOptions.Point );

				// set alpha
				var alphaBlendMode = new LayerBlendModeEx();
				alphaBlendMode.alphaArg1 = 0;
				alphaBlendMode.alphaArg2 = currChunk.Alpha;
				alphaBlendMode.source1 = LayerBlendSource.Texture;
				alphaBlendMode.source2 = LayerBlendSource.Manual;
				alphaBlendMode.blendType = LayerBlendType.Alpha;
				alphaBlendMode.operation = LayerBlendOperationEx.Modulate;
				alphaBlendMode.blendFactor = currChunk.Alpha;
				rs.SetTextureBlendMode( 0, alphaBlendMode );

				rs.Render( renderOp );
				renderOp.vertexData.vertexStart += currChunk.VertexCount;
			}

			if ( tp != null )
			{
				tp.Dispose();
			}

			// sprites go home!
			sprites.Clear();
		}

		private static void RenderQueueEnded( object sender, SceneManager.EndRenderQueueEventArgs e )
		{
			e.RepeatInvocation = false; // shut up compiler
			if ( AfterQueue && (byte)e.RenderQueueId == (byte)TargetQueue )
			{
				Render();
			}
		}

		private static void RenderQueueStarted( object sender, SceneManager.BeginRenderQueueEventArgs e )
		{
			e.SkipInvocation = false; // shut up compiler
			if ( !AfterQueue && (byte)e.RenderQueueId == (byte)TargetQueue )
			{
				Render();
			}
		}

		private static void RenderSystem_Setup()
		{
			Axiom.Graphics.RenderSystem rs = Root.Instance.RenderSystem;

			var colorBlendMode = new LayerBlendModeEx();
			colorBlendMode.blendType = LayerBlendType.Color;
			colorBlendMode.source1 = LayerBlendSource.Texture;
			colorBlendMode.operation = LayerBlendOperationEx.Source1;

			var uvwAddressMode = new UVWAddressing( TextureAddressing.Clamp );

			rs.WorldMatrix = Matrix4.Identity;
			rs.ViewMatrix = Matrix4.Identity;
			rs.ProjectionMatrix = Matrix4.Identity;
			rs.SetTextureMatrix( 0, Matrix4.Identity );
			rs.SetTextureCoordSet( 0, 0 );
			rs.SetTextureCoordCalculation( 0, TexCoordCalcMethod.None );
			rs.SetTextureBlendMode( 0, colorBlendMode );
			rs.SetTextureAddressingMode( 0, uvwAddressMode );
			rs.DisableTextureUnitsFrom( 1 );
			rs.LightingEnabled = false;
			rs.SetFog( FogMode.None );
			rs.CullingMode = CullingMode.None;
			rs.SetDepthBufferParams( false, false );
			rs.SetColorBufferWriteEnabled( true, true, true, false );
			rs.ShadingType = ShadeOptions.Gouraud;
			rs.PolygonMode = PolygonMode.Solid;
			rs.UnbindGpuProgram( GpuProgramType.Fragment );
			rs.UnbindGpuProgram( GpuProgramType.Vertex );
			rs.SetSeparateSceneBlending( SceneBlendFactor.SourceAlpha, SceneBlendFactor.OneMinusSourceAlpha, SceneBlendFactor.One,
			                             SceneBlendFactor.One );
			rs.SetAlphaRejectSettings( CompareFunction.AlwaysPass, 0, true );
		}

		#endregion Methods

		#region Nested Types

		internal struct Chunk
		{
			#region Properties

			public float Alpha { get; set; }

			public ResourceHandle TexHandle { get; set; }

			public int VertexCount { get; set; }

			#endregion Properties
		}

		internal struct Sprite
		{
			#region Properties

			public float Alpha { get; set; }

			public Vector3[] Pos { get; set; }

			public ResourceHandle TexHandle { get; set; }

			public Vector2[] UV { get; set; }

			#endregion Properties

			#region Methods

			public static bool operator !=( Sprite left, Sprite right )
			{
				return !left.Equals( right );
			}

			public static bool operator ==( Sprite left, Sprite right )
			{
				return left.Equals( right );
			}

			public override bool Equals( object obj )
			{
				if ( obj is Sprite )
				{
					return Equals( (Sprite)obj ); // use Equals method below
				}
				else
				{
					return false;
				}
			}

			public bool Equals( Sprite other )
			{
				bool equal = TexHandle == other.TexHandle && Alpha == other.Alpha;

				if ( !equal )
				{
					return false;
				}

				for ( int i = 0; i < 6; i++ )
				{
					if ( Pos[ i ] != other.Pos[ i ] )
					{
						return false;
					}

					if ( UV[ i ] != other.UV[ i ] )
					{
						return false;
					}
				}

				return true;
			}

			public override int GetHashCode()
			{
				return Alpha.GetHashCode() ^ Pos.GetHashCode() ^ UV.GetHashCode() ^ TexHandle.GetHashCode();
			}

			#endregion Methods
		}

		[StructLayout( LayoutKind.Explicit )]
		internal struct Vertex
		{
			[FieldOffset( 0 )] public Vector3 Pos;

			[FieldOffset( 12 )] public Vector2 UV;

			#region Constructors

			public Vertex( Vector3 pos, Vector2 uv )
			{
				this.Pos = pos;
				this.UV = uv;
			}

			#endregion Constructors
		}

		#endregion Nested Types
	}
}