#region LGPL License
/*
Axiom Game Engine Library
Copyright (C) 2003  Axiom Project Team

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
		public WireBoundingBox()
		{
			
		}

		#region Implementation of SimpleRenderable

		public override Axiom.MathLib.Matrix4[] WorldTransforms
		{
			get { return new Matrix4[] { Matrix4.Identity }; }
		}

		
		public void InitAABB(AxisAlignedBox box)
		{
			// TODO: Fix post VBO implementation
			/*
			vertexData = new float[24 * 3];		
			colorData = new int[24];

			SetupAABBVertices(box);

			for(int i = 0; i < colorData.Length; i++)
				colorData[i] = System.Drawing.Color.Black.ToArgb();

			vertexBuffer.numVertices = 24;
			vertexBuffer.useIndices = false;
			vertexBuffer.vertices = vertexData;
			vertexBuffer.renderOp = RenderMode.LineList;
			vertexBuffer.vertexFlags = VertexFlags.Diffuse;
			vertexBuffer.numTexCoordSets = 0;
			vertexBuffer.colors = colorData;	

			// HACK: USe BoundingBox property
			this.box = box; */
		}

		private void SetupAABBVertices(AxisAlignedBox aab)
		{
			Vector3 vmax = aab.Maximum;
			Vector3 vmin = aab.Minimum;
		
			float maxx = vmax.x;
			float maxy = vmax.y;
			float maxz = vmax.z;
		
			float minx = vmin.x;
			float miny = vmin.y;
			float minz = vmin.z;
		
			int i = 0;

			// fill in the Vertex array: 12 lines with 2 endpoints each make up a box
			// line 0
			vertexData[i++] = minx;
			vertexData[i++] = miny;
			vertexData[i++] = minz;
			vertexData[i++] = maxx;
			vertexData[i++] = miny;
			vertexData[i++] = minz;
			// line 1
			vertexData[i++] = minx;
			vertexData[i++] = miny;
			vertexData[i++] = minz;
			vertexData[i++] = minx;
			vertexData[i++] = miny;
			vertexData[i++] = maxz;
			// line 2
			vertexData[i++] = minx;
			vertexData[i++] = miny;
			vertexData[i++] = minz;
			vertexData[i++] = minx;
			vertexData[i++] = maxy;
			vertexData[i++] = minz;
			// line 3
			vertexData[i++] = minx;
			vertexData[i++] = maxy;
			vertexData[i++] = minz;
			vertexData[i++] = minx;
			vertexData[i++] = maxy;
			vertexData[i++] = maxz;
			// line 4
			vertexData[i++] = minx;
			vertexData[i++] = maxy;
			vertexData[i++] = minz;
			vertexData[i++] = maxx;
			vertexData[i++] = maxy;
			vertexData[i++] = minz;
			// line 5
			vertexData[i++] = maxx;
			vertexData[i++] = miny;
			vertexData[i++] = minz;
			vertexData[i++] = maxx;
			vertexData[i++] = miny;
			vertexData[i++] = maxz;
			// line 6
			vertexData[i++] = maxx;
			vertexData[i++] = miny;
			vertexData[i++] = minz;
			vertexData[i++] = maxx;
			vertexData[i++] = maxy;
			vertexData[i++] = minz;
			// line 7
			vertexData[i++] = minx;
			vertexData[i++] = maxy;
			vertexData[i++] = maxz;
			vertexData[i++] = maxx;
			vertexData[i++] = maxy;
			vertexData[i++] = maxz;
			// line 8
			vertexData[i++] = minx;
			vertexData[i++] = maxy;
			vertexData[i++] = maxz;
			vertexData[i++] = minx;
			vertexData[i++] = miny;
			vertexData[i++] = maxz;
			// line 9
			vertexData[i++] = maxx;
			vertexData[i++] = maxy;
			vertexData[i++] = minz;
			vertexData[i++] = maxx;
			vertexData[i++] = maxy;
			vertexData[i++] = maxz;
			// line 10
			vertexData[i++] = maxx;
			vertexData[i++] = miny;
			vertexData[i++] = maxz;
			vertexData[i++] = maxx;
			vertexData[i++] = maxy;
			vertexData[i++] = maxz;
			// line 11
			vertexData[i++] = minx;
			vertexData[i++] = miny;
			vertexData[i++] = maxz;
			vertexData[i++] = maxx;
			vertexData[i++] = miny;
			vertexData[i++] = maxz;

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


		#endregion
	}
}
