#region LGPL License
/*
Axiom Game Engine Library
Copyright (C) 2003  Axiom Project Team

The overall design, and a majority of the core engine and rendering code 
contained within this library is a derivative of the open source Object Oriented 
Graphics Engine OGRE, which can be found at http://ogre.sourceforge.net.  
Many thanks to the OGRE team for maintaining such a high quality project.

This library is free software; you can redistribute it and/or
modify it under the terms of the GNU Lesser General Public
License as published by the Free Software Foundation; either
version 2.1 of the License, or (at your option) any later version.

This library is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
Lesser General Public License for more details.

You should have received a copy of the GNU Lesser General Public
License along with this library; if not, write to the Free Software
Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
*/
#endregion

using System;
using Axiom.Enumerations;
using Axiom.MathLib;
using Axiom.SubSystems.Rendering;

namespace Axiom.Core
{
	/// <summary>
	/// Summary description for WireBoundingBox.
	/// </summary>
	public class WireBoundingBox : SimpleRenderable
	{
		const int POSITION = 0;
		const int COLOR = 1;

		public WireBoundingBox()
		{
			vertexData = new VertexData();
			vertexData.vertexCount = 24;
			vertexData.vertexStart = 0;
			
			VertexDeclaration decl = vertexData.vertexDeclaration;
			VertexBufferBinding binding = vertexData.vertexBufferBinding;

			decl.AddElement(new VertexElement(POSITION, 0, VertexElementType.Float3, VertexElementSemantic.Position));
			decl.AddElement(new VertexElement(COLOR, 0, VertexElementType.Color, VertexElementSemantic.Diffuse));

			HardwareVertexBuffer buffer  =
				HardwareBufferManager.Instance.CreateVertexBuffer(
					decl.GetVertexSize(POSITION), 
					vertexData.vertexCount, 
					BufferUsage.StaticWriteOnly);

			binding.SetBinding(POSITION, buffer);

			buffer  = 	HardwareBufferManager.Instance.CreateVertexBuffer(
				decl.GetVertexSize(COLOR), 
				vertexData.vertexCount, 
				BufferUsage.StaticWriteOnly);

			binding.SetBinding(COLOR, buffer);
		}

		#region Implementation of SimpleRenderable

		public override Axiom.MathLib.Matrix4[] WorldTransforms
		{
			get { return new Matrix4[] { Matrix4.Identity }; }
		}

		
		public void InitAABB(AxisAlignedBox box)
		{
			SetupAABBVertices(box);

			HardwareVertexBuffer buffer =
				vertexData.vertexBufferBinding.GetBuffer(COLOR);

			IntPtr colPtr = buffer.Lock(BufferLocking.Discard);

			unsafe
			{
				int* pCol = (int*)colPtr.ToPointer();

				for(int i = 0; i < vertexData.vertexCount; i++)
					pCol[i] = System.Drawing.Color.Black.ToArgb();
			}

			buffer.Unlock();

			// HACK: USe BoundingBox property
			this.box = box; 
		}

		private void SetupAABBVertices(AxisAlignedBox aab)
		{
			Vector3 vmax = aab.Maximum;
			Vector3 vmin = aab.Minimum;

			// TODO: Add bounding sphere radius and set here

			float maxx = vmax.x + 1.0f;
			float maxy = vmax.y + 1.0f;
			float maxz = vmax.z + 1.0f;
		
			float minx = vmin.x - 1.0f;
			float miny = vmin.y - 1.0f;
			float minz = vmin.z - 1.0f;
		
			int i = 0;

			HardwareVertexBuffer buffer =
				vertexData.vertexBufferBinding.GetBuffer(POSITION);

			IntPtr posPtr = buffer.Lock(BufferLocking.Discard);

			unsafe
			{
				float* pPos = (float*)posPtr.ToPointer();

				// fill in the Vertex array: 12 lines with 2 endpoints each make up a box
				// line 0
				pPos[i++] = minx;
				pPos[i++] = miny;
				pPos[i++] = minz;
				pPos[i++] = maxx;
				pPos[i++] = miny;
				pPos[i++] = minz;
				// line 1
				pPos[i++] = minx;
				pPos[i++] = miny;
				pPos[i++] = minz;
				pPos[i++] = minx;
				pPos[i++] = miny;
				pPos[i++] = maxz;
				// line 2
				pPos[i++] = minx;
				pPos[i++] = miny;
				pPos[i++] = minz;
				pPos[i++] = minx;
				pPos[i++] = maxy;
				pPos[i++] = minz;
				// line 3
				pPos[i++] = minx;
				pPos[i++] = maxy;
				pPos[i++] = minz;
				pPos[i++] = minx;
				pPos[i++] = maxy;
				pPos[i++] = maxz;
				// line 4
				pPos[i++] = minx;
				pPos[i++] = maxy;
				pPos[i++] = minz;
				pPos[i++] = maxx;
				pPos[i++] = maxy;
				pPos[i++] = minz;
				// line 5
				pPos[i++] = maxx;
				pPos[i++] = miny;
				pPos[i++] = minz;
				pPos[i++] = maxx;
				pPos[i++] = miny;
				pPos[i++] = maxz;
				// line 6
				pPos[i++] = maxx;
				pPos[i++] = miny;
				pPos[i++] = minz;
				pPos[i++] = maxx;
				pPos[i++] = maxy;
				pPos[i++] = minz;
				// line 7
				pPos[i++] = minx;
				pPos[i++] = maxy;
				pPos[i++] = maxz;
				pPos[i++] = maxx;
				pPos[i++] = maxy;
				pPos[i++] = maxz;
				// line 8
				pPos[i++] = minx;
				pPos[i++] = maxy;
				pPos[i++] = maxz;
				pPos[i++] = minx;
				pPos[i++] = miny;
				pPos[i++] = maxz;
				// line 9
				pPos[i++] = maxx;
				pPos[i++] = maxy;
				pPos[i++] = minz;
				pPos[i++] = maxx;
				pPos[i++] = maxy;
				pPos[i++] = maxz;
				// line 10
				pPos[i++] = maxx;
				pPos[i++] = miny;
				pPos[i++] = maxz;
				pPos[i++] = maxx;
				pPos[i++] = maxy;
				pPos[i++] = maxz;
				// line 11
				pPos[i++] = minx;
				pPos[i++] = miny;
				pPos[i++] = maxz;
				pPos[i++] = maxx;
				pPos[i++] = miny;
				pPos[i++] = maxz;
			}

			buffer.Unlock();
		}

		public override float GetSquaredViewDepth(Camera camera)
		{
			Vector3 min, max, mid, dist;
			min = box.Minimum;
			max = box.Maximum;
			mid = ((min - max) * 0.5f) + min;
			dist = camera.DerivedPosition - mid;

			return dist.LengthSquared;
		}

		public override void GetRenderOperation(RenderOperation op)
		{
			op.vertexData = vertexData;
			op.indexData = null;
			op.operationType = RenderMode.LineList;
			op.useIndices = false;
		}


		#endregion
	}
}
