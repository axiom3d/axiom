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

#region SVN Version Information

// <file>
//     <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using Axiom.Controllers;
using Axiom.Controllers.Canned;
using Axiom.Core;
using Axiom.Graphics;
using Axiom.Math;

#endregion Namespace Declarations

namespace Axiom.Samples.Primitives
{
	public class PrimitivesSample : SdkSample
	{
		private Vector4 _color = new Vector4( 1, 0, 0, 1 );
		private Line3d _line;
		private Triangle _tri;

		public PrimitivesSample()
		{
			Metadata[ "Title" ] = "Rotating Primitives";
			Metadata[ "Description" ] = "Sample which shows the classic spinning primitives, done in the Axiom engine";
			Metadata[ "Thumbnail" ] = "thumb_triangle.png";
			Metadata[ "Category" ] = "Geometry";
		}

		public override bool FrameStarted( FrameEventArgs evt )
		{
			if ( evt.StopRendering )
			{
				return false;
			}

			this._color.x += evt.TimeSinceLastFrame * .6f;
			if ( this._color.x > 1 )
			{
				this._color.x = 0;
			}

			this._color.y += evt.TimeSinceLastFrame * .6f;
			if ( this._color.y > 1 )
			{
				this._color.y = 0;
			}

			this._color.z += evt.TimeSinceLastFrame * .6f;
			if ( this._color.z > 1 )
			{
				this._color.z = 0;
			}
			return base.FrameStarted( evt );
		}

		protected override void SetupContent()
		{
			// create a 3d line
			this._line = new Line3d( new Vector3( 0, 0, 30 ), Vector3.UnitY, 50, ColorEx.Blue );

			this._tri = new Triangle( new Vector3( -25, 0, 0 ), new Vector3( 0, 50, 0 ), new Vector3( 25, 0, 0 ), ColorEx.Red, ColorEx.Blue, ColorEx.Green );

			// create a node for the line
			SceneNode node = SceneManager.RootSceneNode.CreateChildSceneNode();
			SceneNode lineNode = node.CreateChildSceneNode();
			SceneNode triNode = node.CreateChildSceneNode();
			triNode.Position = new Vector3( 50, 0, 0 );

			// add the line and triangle to the scene
			lineNode.AttachObject( this._line );
			triNode.AttachObject( this._tri );

			// create a node rotation controller value, which will mark the specified scene node as a target of the rotation
			// we want to rotate along the Y axis for the triangle and Z for the line (just for the hell of it)
			var rotate = new NodeRotationControllerValue( triNode, Vector3.UnitY );
			var rotate2 = new NodeRotationControllerValue( lineNode, Vector3.UnitZ );

			// the multiply controller function will multiply the source controller value by the specified value each frame.
			var func = new MultipyControllerFunction( 50 );

			// create a new controller, using the rotate and func objects created above.  there are 2 overloads to this method.  the one being
			// used uses an internal FrameTimeControllerValue as the source value by default.  The destination value will be the node, which
			// is implemented to simply call Rotate on the specified node along the specified axis.  The function will mutiply the given value
			// against the source value, which in this case is the current frame time.  The end result in this demo is that if 50 is specified in the
			// MultiplyControllerValue, then the node will rotate 50 degrees per second.  since the value is scaled by the frame time, the speed
			// of the rotation will be consistent on all machines regardless of CPU speed.
			ControllerManager.Instance.CreateController( rotate, func );
			ControllerManager.Instance.CreateController( rotate2, func );

			// place the camera in an optimal position
			base.Camera.Position = new Vector3( 30, 30, 220 );
		}

		protected override void CleanupContent()
		{
			this._line.SafeDispose();
			this._tri.SafeDispose();
		}
	};

	/// <summary>
	///	A class for rendering lines in 3d.
	/// </summary>
	public class Line3d : SimpleRenderable
	{
		// constants for buffer source bindings
		private const int POSITION = 0;
		private const int COLOR = 1;

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
			HardwareVertexBuffer buffer = HardwareBufferManager.Instance.CreateVertexBuffer( decl.Clone( POSITION ), vertexData.vertexCount, BufferUsage.StaticWriteOnly );
			var pos = new[]
                      {
                          startPoint, endPoint
                      };

			// write the data to the position buffer
			buffer.WriteData( 0, buffer.Size, pos, true );

			// bind the position buffer
			binding.SetBinding( POSITION, buffer );

			// create a color buffer
			buffer = HardwareBufferManager.Instance.CreateVertexBuffer( decl.Clone( COLOR ), vertexData.vertexCount, BufferUsage.StaticWriteOnly );

			int colorValue = Root.Instance.RenderSystem.ConvertColor( color );

			var colors = new[]
                         {
                             colorValue, colorValue
                         };

			// write the data to the position buffer
			buffer.WriteData( 0, buffer.Size, colors, true );

			// bind the color buffer
			binding.SetBinding( COLOR, buffer );

			// MATERIAL
			// grab a copy of the BaseWhite material for our use
			var material = (Material)MaterialManager.Instance.GetByName( "BaseWhite" );
			material = material.Clone( "LineMat" );
			// disable lighting to vertex colors are used
			material.Lighting = false;
			// set culling to none so the triangle is drawn 2 sided
			material.CullingMode = CullingMode.None;

			Material = material;

			// set the bounding box of the line
			box = new AxisAlignedBox( startPoint, endPoint );
		}

		public override Real BoundingRadius
		{
			get
			{
				return 0;
			}
		}

		protected override void dispose( bool disposeManagedResources )
		{
			if ( !IsDisposed )
			{
				if ( disposeManagedResources )
				{
					MaterialManager.Instance.Remove( Material );
				}
			}

			base.dispose( disposeManagedResources );
		}

		public override Real GetSquaredViewDepth( Camera camera )
		{
			Vector3 min, max, mid, dist;
			min = box.Minimum;
			max = box.Maximum;
			mid = ( ( min - max ) * 0.5f ) + min;
			dist = camera.DerivedPosition - mid;

			return dist.LengthSquared;
		}
	};

	/// <summary>
	///	A class for rendering a simple triangle with colored vertices.
	/// </summary>
	public class Triangle : SimpleRenderable
	{
		// constants for buffer source bindings
		private const int POSITION = 0;
		private const int COLOR = 1;

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

			var positions = new[]
                            {
                                v1, v2, v3
                            };

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
			var colors = new[]
                         {
                             Root.Instance.RenderSystem.ConvertColor( c1 ), Root.Instance.RenderSystem.ConvertColor( c2 ), Root.Instance.RenderSystem.ConvertColor( c3 )
                         };

			// write the colors to the color buffer
			buffer.WriteData( 0, buffer.Size, colors, true );

			// bind the color buffer
			binding.SetBinding( COLOR, buffer );

			// MATERIAL
			// grab a copy of the BaseWhite material for our use
			var material = (Material)MaterialManager.Instance.GetByName( "BaseWhite" );
			material = material.Clone( "TriMat" );

			// disable lighting to vertex colors are used
			material.Lighting = false;
			// set culling to none so the triangle is drawn 2 sided
			material.CullingMode = CullingMode.None;

			Material = material;

			// set the bounding box of the tri
			// TODO: not right, but good enough for now
			box = new AxisAlignedBox( new Vector3( 25, 50, 0 ), new Vector3( -25, 0, 0 ) );
		}

		public override Real BoundingRadius
		{
			get
			{
				return 0;
			}
		}

		protected override void dispose( bool disposeManagedResources )
		{
			if ( !IsDisposed )
			{
				if ( disposeManagedResources )
				{
					MaterialManager.Instance.Remove( Material );
				}
			}

			base.dispose( disposeManagedResources );
		}

		public override Real GetSquaredViewDepth( Camera camera )
		{
			Vector3 min, max, mid, dist;
			min = box.Minimum;
			max = box.Maximum;
			mid = ( ( min - max ) * 0.5f ) + min;
			dist = camera.DerivedPosition - mid;

			return dist.LengthSquared;
		}
	};
}
