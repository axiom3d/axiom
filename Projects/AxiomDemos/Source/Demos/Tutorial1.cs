#region Namespace Declarations

using System;

using Axiom.Controllers;
using Axiom.Controllers.Canned;
using Axiom.Core;
using Axiom.Math;
using Axiom.Graphics;

#endregion Namespace Declarations

namespace Axiom.Demos
{
	/// <summary>
	/// 	Sample class which shows the classic spinning triangle, done in the Axiom engine.
	/// </summary>
	public class Tutorial1 : TechDemo
	{
		#region Methods

		private Vector4 color = new Vector4( 1, 0, 0, 1 );

		protected override void OnFrameStarted( object source, FrameEventArgs evt )
		{
			//base.OnFrameStarted( source, evt );
			if ( evt.StopRendering )
				return;

			color.x += evt.TimeSinceLastFrame * .6f;
			if ( color.x > 1 )
				color.x = 0;

			color.y += evt.TimeSinceLastFrame * .6f;
			if ( color.y > 1 )
				color.y = 0;

			color.z += evt.TimeSinceLastFrame * .6f;
			if ( color.z > 1 )
				color.z = 0;
		}

		public override void CreateScene()
		{
			// create a 3d line
			Line3d line = new Line3d( new Vector3( 0, 0, 30 ), Vector3.UnitY, 50, ColorEx.Blue );

			Triangle tri = new Triangle(
				new Vector3( -25, 0, 0 ),
				new Vector3( 0, 50, 0 ),
				new Vector3( 25, 0, 0 ),
				ColorEx.Red,
				ColorEx.Blue,
				ColorEx.Green );

			Entity cube = scene.CreateEntity( "cube", PrefabEntity.Cube );
			// create a node for the line
			SceneNode node = scene.RootSceneNode.CreateChildSceneNode();

			SceneNode lineNode = node.CreateChildSceneNode();
			SceneNode triNode = node.CreateChildSceneNode();
			SceneNode cubeNode = node.CreateChildSceneNode();

			triNode.Position = new Vector3( 50, 0, 0 );
			cubeNode.Position = new Vector3( 50, 50, 0 );

			// add the line and triangle to the scene
			lineNode.AttachObject( line );
			triNode.AttachObject( tri );
			cubeNode.AttachObject( cube );

			// create a node rotation controller value, which will mark the specified scene node as a target of the rotation
			// we want to rotate along the Y axis for the triangle and Z for the line (just for the hell of it)
			NodeRotationControllerValue rotate = new NodeRotationControllerValue( triNode, Vector3.UnitY );
			NodeRotationControllerValue rotate2 = new NodeRotationControllerValue( lineNode, Vector3.UnitZ );

			// the multiply controller function will multiply the source controller value by the specified value each frame.
			MultipyControllerFunction func = new MultipyControllerFunction( 50 );

			// create a new controller, using the rotate and func objects created above.  there are 2 overloads to this method.  the one being
			// used uses an internal FrameTimeControllerValue as the source value by default.  The destination value will be the node, which
			// is implemented to simply call Rotate on the specified node along the specified axis.  The function will multiply the given value
			// against the source value, which in this case is the current frame time.  The end result in this demo is that if 50 is specified in the
			// MultiplyControllerValue, then the node will rotate 50 degrees per second.  since the value is scaled by the frame time, the speed
			// of the rotation will be consistent on all machines regardless of CPU speed.
			ControllerManager.Instance.CreateController( rotate, func );
			ControllerManager.Instance.CreateController( rotate2, func );

			// place the camera in an optimal position
			camera.Position = new Vector3( 30, 30, 220 );

			debugText = "Spinning triangle - Using custom built geometry";
		}

		#endregion Methods
	}

	/// <summary>
	///		A class for rendering lines in 3d.
	/// </summary>
	public class Line3d : SimpleRenderable
	{
		// constants for buffer source bindings
		const int POSITION = 0;
		const int COLOR = 1;

		/// <summary>
		///
		/// </summary>
		/// <param name="startPoint">Point where the line will start.</param>
		/// <param name="direction">The direction the vector is heading in.</param>
		/// <param name="length">The length (magnitude) of the line vector.</param>
		/// <param name="color">The color which this line should be.</param>
		public Line3d( Vector3 startPoint, Vector3 direction, float length, ColorEx color )
		{
			// normalize the direction vector to ensure all elements fall in [0,1] range.
			direction.Normalize();

			// calculate the actual endpoint
			Vector3 endPoint = startPoint + ( direction * length );

			vertexData = new VertexData();
			renderOperation.vertexData = vertexData;
			renderOperation.vertexData.vertexCount = 2;
			renderOperation.vertexData.vertexStart = 0;
			renderOperation.indexData = null;
			renderOperation.operationType = OperationType.LineList;
			renderOperation.useIndices = false;

			VertexDeclaration decl = vertexData.vertexDeclaration;
			VertexBufferBinding binding = vertexData.vertexBufferBinding;

			// add a position and color element to the declaration
			decl.AddElement( POSITION, 0, VertexElementType.Float3, VertexElementSemantic.Position );
			decl.AddElement( COLOR, 0, VertexElementType.Color, VertexElementSemantic.Diffuse );

			// create a vertex buffer for the position
			HardwareVertexBuffer buffer =
				HardwareBufferManager.Instance.CreateVertexBuffer( decl.Clone( POSITION ), vertexData.vertexCount, BufferUsage.StaticWriteOnly );

			Vector3[] pos = new Vector3[] { startPoint, endPoint };

			// write the data to the position buffer
			buffer.WriteData( 0, buffer.Size, pos, true );

			// bind the position buffer
			binding.SetBinding( POSITION, buffer );

			// create a color buffer
			buffer = HardwareBufferManager.Instance.CreateVertexBuffer( decl.Clone( COLOR ), vertexData.vertexCount, BufferUsage.StaticWriteOnly );

			int colorValue = Root.Instance.RenderSystem.ConvertColor( color );

			int[] colors = new int[] { colorValue, colorValue };

			// write the data to the position buffer
			buffer.WriteData( 0, buffer.Size, colors, true );

			// bind the color buffer
			binding.SetBinding( COLOR, buffer );

			// MATERIAL
			// grab a copy of the BaseWhite material for our use
			Material material = (Material)MaterialManager.Instance.GetByName( "BaseWhite" );
			material = material.Clone( "LineMat" );
			// disable lighting to vertex colors are used
			material.Lighting = false;
			// set culling to none so the triangle is drawn 2 sided
			material.CullingMode = CullingMode.None;

			this.Material = material;

			// set the bounding box of the line
			this.box = new AxisAlignedBox( startPoint, endPoint );
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="camera"></param>
		/// <returns></returns>
		public override float GetSquaredViewDepth( Camera camera )
		{
			Vector3 min, max, mid, dist;
			min = box.Minimum;
			max = box.Maximum;
			mid = ( ( min - max ) * 0.5f ) + min;
			dist = camera.DerivedPosition - mid;

			return dist.LengthSquared;
		}

		public override float BoundingRadius
		{
			get
			{
				return 0;
			}
		}

	}


	/// <summary>
	///		A class for rendering a simple triangle with colored vertices.
	/// </summary>
	public class Triangle : SimpleRenderable
	{
		// constants for buffer source bindings
		const int POSITION = 0;
		const int COLOR = 1;

		/// <summary>
		///
		/// </summary>
		/// <param name="v1"></param>
		/// <param name="v2"></param>
		/// <param name="v3"></param>
		public Triangle( Vector3 v1, Vector3 v2, Vector3 v3, ColorEx c1, ColorEx c2, ColorEx c3 )
		{
			vertexData = new VertexData();
			renderOperation.vertexData = vertexData;
			renderOperation.vertexData.vertexCount = 3;
			renderOperation.vertexData.vertexStart = 0;
			renderOperation.indexData = null;
			renderOperation.operationType = OperationType.TriangleList;
			renderOperation.useIndices = false;

			VertexDeclaration decl = vertexData.vertexDeclaration;
			VertexBufferBinding binding = vertexData.vertexBufferBinding;

			// add a position and color element to the declaration
			decl.AddElement( POSITION, 0, VertexElementType.Float3, VertexElementSemantic.Position );
			decl.AddElement( COLOR, 0, VertexElementType.Color, VertexElementSemantic.Diffuse );

			// POSITIONS
			// create a vertex buffer for the position
			HardwareVertexBuffer buffer = HardwareBufferManager.Instance.CreateVertexBuffer( decl.Clone( POSITION ), vertexData.vertexCount, BufferUsage.StaticWriteOnly );

			Vector3[] positions = new Vector3[] { v1, v2, v3 };

			// write the positions to the buffer
			buffer.WriteData( 0, buffer.Size, positions, true );

			// bind the position buffer
			binding.SetBinding( POSITION, buffer );

			// COLORS
			// create a color buffer
			buffer = HardwareBufferManager.Instance.CreateVertexBuffer( decl.Clone( COLOR ), vertexData.vertexCount, BufferUsage.StaticWriteOnly );

			// create an int array of the colors to use.
			// note: these must be converted to the current API's
			// preferred packed int format
			int[] colors = new int[] {
				Root.Instance.RenderSystem.ConvertColor(c1),
				Root.Instance.RenderSystem.ConvertColor(c2),
				Root.Instance.RenderSystem.ConvertColor(c3)
			};

			// write the colors to the color buffer
			buffer.WriteData( 0, buffer.Size, colors, true );

			// bind the color buffer
			binding.SetBinding( COLOR, buffer );

			// MATERIAL
			// grab a copy of the BaseWhite material for our use
			Material material = (Material)MaterialManager.Instance.GetByName( "BaseWhite" );
			material = material.Clone( "TriMat" );

			// disable lighting to vertex colors are used
			material.Lighting = false;
			// set culling to none so the triangle is drawn 2 sided
			material.CullingMode = CullingMode.None;

			materialName = "TriMat";

			this.Material = material;

			// set the bounding box of the tri
			// TODO: not right, but good enough for now
			this.box = new AxisAlignedBox( new Vector3( 25, 50, 0 ), new Vector3( -25, 0, 0 ) );
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="camera"></param>
		/// <returns></returns>
		public override float GetSquaredViewDepth( Camera camera )
		{
			Vector3 min, max, mid, dist;
			min = box.Minimum;
			max = box.Maximum;
			mid = ( ( min - max ) * 0.5f ) + min;
			dist = camera.DerivedPosition - mid;

			return dist.LengthSquared;
		}

		public override float BoundingRadius
		{
			get
			{
				return 0;
			}
		}
	}
}