#region LGPL License
/*
 * Modified from the WireBoundingBox class from the Axiom source.
*/
#endregion

using System;

using Axiom.Math;
using Axiom.Graphics;
using Axiom.Core;
using System.Collections;

namespace Chronos.Core {
	/// <summary>
	/// Summary description for Line3D.
	/// </summary>
	public sealed class Line3D : SimpleRenderable {
		#region Member variables

		private ArrayList points = new ArrayList();
		private Vector3 min, max;
		private float radius;
		private bool depthCheck;
            
		#endregion Member variables

		#region Constants

		const int POSITION = 0;
		const int COLOR = 1;

		#endregion Constants

		#region Constructors
		/// <summary>
		///    Default constructor.
		/// </summary>
		public Line3D(bool depthCheck) {
			min = new Vector3(0,0,0);
			max = new Vector3(0,0,0);
			this.depthCheck = depthCheck;
		}

		public Line3D() {
			min = new Vector3(0,0,0);
			max = new Vector3(0,0,0);
			this.depthCheck = true;
		}

		#endregion Constructors

		#region Implementation of SimpleRenderable

		private void setupBuffers() {
			if(points.Count == 0) return;
			vertexData = new VertexData();
			vertexData.vertexCount = points.Count;
			vertexData.vertexStart = 0;

			// get a reference to the vertex declaration and buffer binding
			VertexDeclaration decl = vertexData.vertexDeclaration;
			VertexBufferBinding binding = vertexData.vertexBufferBinding;

			// add elements for position and color only
			decl.AddElement(POSITION, 0, VertexElementType.Float3, VertexElementSemantic.Position);
			decl.AddElement(COLOR, 0, VertexElementType.Color, VertexElementSemantic.Diffuse);

			// create a new hardware vertex buffer for the position data
			HardwareVertexBuffer buffer  =
				HardwareBufferManager.Instance.CreateVertexBuffer(
				decl.GetVertexSize(POSITION), 
				vertexData.vertexCount, 
				BufferUsage.StaticWriteOnly);

			// bind the position buffer
			binding.SetBinding(POSITION, buffer);

			// create a new hardware vertex buffer for the color data
			buffer  = 	HardwareBufferManager.Instance.CreateVertexBuffer(
				decl.GetVertexSize(COLOR), 
				vertexData.vertexCount, 
				BufferUsage.StaticWriteOnly);

			// bind the color buffer
			binding.SetBinding(COLOR, buffer);

			Material mat = MaterialManager.Instance.GetByName("Core/WireBB");

			if(mat == null) {
				mat = MaterialManager.Instance.GetByName("BaseWhite");
				mat = mat.Clone("Core/WireBB");
				mat.DepthCheck = depthCheck;
				mat.Lighting = false;
			}

			this.Material = mat;
		}
		
		/// <summary>
		/// 
		/// </summary>
		/// <param name="matrices"></param>
		/*public override void GetWorldTransforms(Matrix4[] matrices) {
			matrices[0] = Matrix4.Identity;
		}*/
		
		public void DrawLines() {
			DrawLines(ColorEx.White);
		}

		public void DrawLines(ColorEx color) {
			if(points.Count == 0) return;
			this.setupBuffers();

			// Populate the position buffer
			HardwareVertexBuffer buffer =
				vertexData.vertexBufferBinding.GetBuffer(POSITION);

			IntPtr posPtr = buffer.Lock(BufferLocking.Discard);

			unsafe {
				float* pPos = (float*)posPtr.ToPointer();
				for(int j=0; j<points.Count; j++) {
					Vector3 v = (Vector3)points[j];
					int k = j * 3;
					pPos[k] = v.x;
					pPos[k+1] = v.y;
					pPos[k+2] = v.z;
				}
			}

			// unlock the buffer
			buffer.Unlock();

			// Populate the color buffer
			// get a reference to the color buffer
			buffer = vertexData.vertexBufferBinding.GetBuffer(COLOR);

			// lock the buffer
			IntPtr colPtr = buffer.Lock(BufferLocking.Discard);

			// load the color buffer with the specified color for each element
			unsafe {
				int* pCol = (int*)colPtr.ToPointer();

				for(int i = 0; i < vertexData.vertexCount; i++)
					pCol[i] = Root.Instance.ConvertColor(color);
			}

			// unlock the buffer
			buffer.Unlock();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="camera"></param>
		/// <returns></returns>
		public override float GetSquaredViewDepth(Camera camera) {
			return (max-min).LengthSquared;
		}

		public void AddPoint(Vector3 p) {
			if(p.x < min.x) min.x = p.x;
			else if(p.x > max.x) max.x = p.x;
			if(p.y < min.y) min.y = p.y;
			else if(p.y > max.y) max.y = p.y;
			if(p.z < min.z) min.z = p.z;
			else if(p.z > max.z) max.z = p.z;
			if(Math.Abs(min.x) > radius) radius = min.x;
			if(Math.Abs(min.y) > radius) radius = min.y;
			if(Math.Abs(min.z) > radius) radius = min.z;
			if(Math.Abs(max.x) > radius) radius = max.x;
			if(Math.Abs(max.y) > radius) radius = max.y;
			if(Math.Abs(max.z) > radius) radius = max.z;
			points.Add(p);
		}

		public void DrawLine(Vector3 start, Vector3 end) {
			points.Clear();
			AddPoint(start);
			AddPoint(end);
			DrawLines();
		}

		public void DrawLine(Vector3 start, Vector3 end, ColorEx color) {
			points.Clear();
			AddPoint(start);
			AddPoint(end);
			DrawLines(color);
		}

		/// <summary>
		///    Gets the rendering operation required to render the wire box.
		/// </summary>
		/// <param name="op">A reference to a precreate RenderOpertion to be modifed here.</param>
		public override void GetRenderOperation(RenderOperation op) {
			op.vertexData = vertexData;
			op.indexData = null;
			op.operationType = OperationType.LineList;
			op.useIndices = false;
		}

		public override AxisAlignedBox GetWorldBoundingBox(bool derive) {
			return new AxisAlignedBox(min, max);
		}


		/// <summary>
		///    Get the local bounding radius of the wire bounding box.
		/// </summary>
		public override float BoundingRadius {
			get {
				return radius;		// TODO: Fix
			}
		}

		public override void UpdateRenderQueue(RenderQueue queue) {
			queue.AddRenderable(this, 100, this.RenderQueueGroup);
		}


		#endregion
	}
}
