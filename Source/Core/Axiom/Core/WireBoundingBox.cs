#region LGPL License

/*
Axiom Graphics Engine Library
Copyright © 2003-2011 Axiom Project Team

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

#endregion LGPL License

#region SVN Version Information

// <file>
//     <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;
using Axiom.Math;
using Axiom.Graphics;

#endregion Namespace Declarations

namespace Axiom.Core
{
	/// <summary>
	/// Summary description for WireBoundingBox.
	/// </summary>
	public class WireBoundingBox : SimpleRenderable
	{
		#region Constants

		private const int PositionBinding = 0;

		#endregion Constants

		#region Field and Properties

		protected Real Radius;

		public new AxisAlignedBox BoundingBox
		{
			get
			{
				return base.BoundingBox;
			}
			set
			{
				// init the vertices to the aabb
				SetupBoundingBoxVertices( value );

				// setup the bounding box of this SimpleRenderable
				box = value;
			}
		}

		#endregion Field and Properties

		#region Constructors

		/// <summary>
		///    Default constructor.
		/// </summary>
		public WireBoundingBox()
		{
			vertexData = new VertexData();
			vertexData.vertexCount = 24;
			vertexData.vertexStart = 0;

			renderOperation.vertexData = vertexData;
			renderOperation.operationType = OperationType.LineList;
			renderOperation.useIndices = false;

			// get a reference to the vertex declaration and buffer binding
			var decl = vertexData.vertexDeclaration;
			var binding = vertexData.vertexBufferBinding;

			// add elements for position and color only
			decl.AddElement( PositionBinding, 0, VertexElementType.Float3, VertexElementSemantic.Position );

			// create a new hardware vertex buffer for the position data
			var buffer = HardwareBufferManager.Instance.CreateVertexBuffer( decl.Clone( PositionBinding ), vertexData.vertexCount,
			                                                                BufferUsage.StaticWriteOnly );

			// bind the position buffer
			binding.SetBinding( PositionBinding, buffer );

			material = (Material)MaterialManager.Instance[ "BaseWhiteNoLighting" ];
		}

		#endregion Constructors

		#region Methods

		[Obsolete( "Use WireBoundingBox.BoundingBox property." )]
		public void InitAABB( AxisAlignedBox box )
		{
			// store the bounding box locally
			BoundingBox = box;
		}

		[Obsolete( "Use WireBoundingBox.BoundingBox property." )]
		public void SetupBoundingBox( AxisAlignedBox aabb )
		{
			// store the bounding box locally
			BoundingBox = box;
		}

		protected virtual void SetupBoundingBoxVertices( AxisAlignedBox aab )
		{
			var vmax = aab.Maximum;
			var vmin = aab.Minimum;

			var sqLen = System.Math.Max( vmax.LengthSquared, vmin.LengthSquared );
			//mRadius = System.Math.Sqrt(sqLen);

			float maxx = vmax.x;
			float maxy = vmax.y;
			float maxz = vmax.z;

			float minx = vmin.x;
			float miny = vmin.y;
			float minz = vmin.z;

			var buffer = vertexData.vertexBufferBinding.GetBuffer( PositionBinding );

#if !AXIOM_SAFE_ONLY
			unsafe
#endif
			{
				var posPtr = buffer.Lock( BufferLocking.Discard ).ToFloatPointer();
				var pPos = 0;

				// line 0
				posPtr[ pPos++ ] = minx;
				posPtr[ pPos++ ] = miny;
				posPtr[ pPos++ ] = minz;
				posPtr[ pPos++ ] = maxx;
				posPtr[ pPos++ ] = miny;
				posPtr[ pPos++ ] = minz;
				// line 1
				posPtr[ pPos++ ] = minx;
				posPtr[ pPos++ ] = miny;
				posPtr[ pPos++ ] = minz;
				posPtr[ pPos++ ] = minx;
				posPtr[ pPos++ ] = miny;
				posPtr[ pPos++ ] = maxz;
				// line 2
				posPtr[ pPos++ ] = minx;
				posPtr[ pPos++ ] = miny;
				posPtr[ pPos++ ] = minz;
				posPtr[ pPos++ ] = minx;
				posPtr[ pPos++ ] = maxy;
				posPtr[ pPos++ ] = minz;
				// line 3
				posPtr[ pPos++ ] = minx;
				posPtr[ pPos++ ] = maxy;
				posPtr[ pPos++ ] = minz;
				posPtr[ pPos++ ] = minx;
				posPtr[ pPos++ ] = maxy;
				posPtr[ pPos++ ] = maxz;
				// line 4
				posPtr[ pPos++ ] = minx;
				posPtr[ pPos++ ] = maxy;
				posPtr[ pPos++ ] = minz;
				posPtr[ pPos++ ] = maxx;
				posPtr[ pPos++ ] = maxy;
				posPtr[ pPos++ ] = minz;
				// line 5
				posPtr[ pPos++ ] = maxx;
				posPtr[ pPos++ ] = miny;
				posPtr[ pPos++ ] = minz;
				posPtr[ pPos++ ] = maxx;
				posPtr[ pPos++ ] = miny;
				posPtr[ pPos++ ] = maxz;
				// line 6
				posPtr[ pPos++ ] = maxx;
				posPtr[ pPos++ ] = miny;
				posPtr[ pPos++ ] = minz;
				posPtr[ pPos++ ] = maxx;
				posPtr[ pPos++ ] = maxy;
				posPtr[ pPos++ ] = minz;
				// line 7
				posPtr[ pPos++ ] = minx;
				posPtr[ pPos++ ] = maxy;
				posPtr[ pPos++ ] = maxz;
				posPtr[ pPos++ ] = maxx;
				posPtr[ pPos++ ] = maxy;
				posPtr[ pPos++ ] = maxz;
				// line 8
				posPtr[ pPos++ ] = minx;
				posPtr[ pPos++ ] = maxy;
				posPtr[ pPos++ ] = maxz;
				posPtr[ pPos++ ] = minx;
				posPtr[ pPos++ ] = miny;
				posPtr[ pPos++ ] = maxz;
				// line 9
				posPtr[ pPos++ ] = maxx;
				posPtr[ pPos++ ] = maxy;
				posPtr[ pPos++ ] = minz;
				posPtr[ pPos++ ] = maxx;
				posPtr[ pPos++ ] = maxy;
				posPtr[ pPos++ ] = maxz;
				// line 10
				posPtr[ pPos++ ] = maxx;
				posPtr[ pPos++ ] = miny;
				posPtr[ pPos++ ] = maxz;
				posPtr[ pPos++ ] = maxx;
				posPtr[ pPos++ ] = maxy;
				posPtr[ pPos++ ] = maxz;
				// line 11
				posPtr[ pPos++ ] = minx;
				posPtr[ pPos++ ] = miny;
				posPtr[ pPos++ ] = maxz;
				posPtr[ pPos++ ] = maxx;
				posPtr[ pPos++ ] = miny;
				posPtr[ pPos ] = maxz;
			}
			buffer.Unlock();
		}

		#endregion Methods

		#region Implementation of SimpleRenderable

		/// <summary>
		///
		/// </summary>
		/// <param name="matrices"></param>
		public override void GetWorldTransforms( Matrix4[] matrices )
		{
			// return identity matrix to prevent parent transforms
			matrices[ 0 ] = Matrix4.Identity;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="camera"></param>
		/// <returns></returns>
		public override Real GetSquaredViewDepth( Camera camera )
		{
			Vector3 min = box.Minimum, max = box.Maximum, mid = ( ( max - min )*0.5f ) + min, dist = camera.DerivedPosition - mid;

			return dist.LengthSquared;
		}

		/// <summary>
		///    Get the local bounding radius of the wire bounding box.
		/// </summary>
		public override Real BoundingRadius
		{
			get
			{
				return Radius;
			}
		}

		#endregion Implementation of SimpleRenderable
	}
}