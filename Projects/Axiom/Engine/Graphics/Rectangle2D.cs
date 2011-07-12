#region LGPL License
/*
Axiom Graphics Engine Library
Copyright � 2003-2011 Axiom Project Team

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

#region SVN Version Information
// <file>
//     <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;

using Axiom.Core;
using Axiom.Graphics;
using Axiom.Math;

#endregion Namespace Declarations

namespace Axiom.Graphics
{
	/// <summary>
	///		Allows the rendering of a simple 2D rectangle
	///		This class renders a simple 2D rectangle; this rectangle has no depth and
	///		therefore is best used with specific render queue and depth settings,
	///		like <see cref="RenderQueueGroupID.Background"/> and 'depth_write off' for backdrops, and 
	///		<see cref="RenderQueueGroupID.Overlay"/> and 'depth_check off' for fullscreen quads.
	/// </summary>
	public class Rectangle2D : SimpleRenderable
	{

		const int POSITION = 0;
		const int TEXCOORD = 1;
		const int NORMAL = 2;
		static float[] texCoords = new float[] { 0, 0, 0, 1, 1, 0, 1, 1 };
		static float[] defaultNormals = new float[] { 0.0f, 0.0f, 1.0f, 0.0f, 0.0f, 1.0f, 0.0f, 0.0f, 1.0f, 0.0f, 0.0f, 1.0f };

		public Rectangle2D()
			: this( false )
		{
		}

		public Rectangle2D( bool includeTextureCoordinates )
		{
			// use identity projection and view matrices
			vertexData = new VertexData();
			renderOperation.vertexData = vertexData;
			renderOperation.vertexData.vertexStart = 0;
			renderOperation.vertexData.vertexCount = 4;
			renderOperation.useIndices = false;
			renderOperation.operationType = OperationType.TriangleStrip;

			VertexDeclaration decl = vertexData.vertexDeclaration;
			VertexBufferBinding binding = vertexData.vertexBufferBinding;

			decl.AddElement( POSITION, 0, VertexElementType.Float3, VertexElementSemantic.Position );
			HardwareVertexBuffer buffer = HardwareBufferManager.Instance.CreateVertexBuffer( decl.Clone( POSITION ), vertexData.vertexCount, BufferUsage.StaticWriteOnly );
			binding.SetBinding( POSITION, buffer );

			decl.AddElement( NORMAL, 0, VertexElementType.Float3, VertexElementSemantic.Normal );
			buffer = HardwareBufferManager.Instance.CreateVertexBuffer( decl.Clone( NORMAL ), renderOperation.vertexData.vertexCount, BufferUsage.StaticWriteOnly );
			binding.SetBinding( NORMAL, buffer );

			buffer.SetData( defaultNormals );

			if ( includeTextureCoordinates )
			{
				decl.AddElement( TEXCOORD, 0, VertexElementType.Float2, VertexElementSemantic.TexCoords );
				buffer = HardwareBufferManager.Instance.CreateVertexBuffer( decl.Clone( TEXCOORD ), vertexData.vertexCount, BufferUsage.StaticWriteOnly );
				binding.SetBinding( TEXCOORD, buffer );

				buffer.SetData( texCoords, true );
			}

			//  set basic white material
			material = (Material)MaterialManager.Instance[ "BaseWhiteNoLighting" ];
		}

		#region SimpleRenderable Members

		public override float BoundingRadius
		{
			get
			{
				return 0;
			}
		}

		public override float GetSquaredViewDepth( Camera camera )
		{
			return 0;
		}

		public override void GetWorldTransforms( Axiom.Math.Matrix4[] matrices )
		{
			// return identity matrix to prevent parent transforms
			matrices[ 0 ] = Matrix4.Identity;
		}


		public override bool UseIdentityProjection
		{
			get
			{
				return true;
			}
		}

		public override bool UseIdentityView
		{
			get
			{
				return true;
			}
		}

		public override Quaternion WorldOrientation
		{
			get
			{
				return Quaternion.Identity;
			}
		}

		public override Vector3 WorldPosition
		{
			get
			{
				return Vector3.Zero;
			}
		}


		#endregion SimpleRenderable Members

		#region Methods

		/// <summary>
		///		Sets the corners of the rectangle, in relative coordinates.
		/// </summary>
		/// <param name="left">Left position in screen relative coordinates, -1 = left edge, 1.0 = right edge.</param>
		/// <param name="top">Top position in screen relative coordinates, 1 = top edge, -1 = bottom edge.</param>
		/// <param name="right">Position in screen relative coordinates.</param>
		/// <param name="bottom">Position in screen relative coordinates.</param>
		public void SetCorners( float left, float top, float right, float bottom )
		{
			SetCorners( left, top, right, bottom, true );
		}
		/// <summary>
		///		Sets the corners of the rectangle, in relative coordinates.
		/// </summary>
		/// <param name="left">Left position in screen relative coordinates, -1 = left edge, 1.0 = right edge.</param>
		/// <param name="top">Top position in screen relative coordinates, 1 = top edge, -1 = bottom edge.</param>
		/// <param name="right">Position in screen relative coordinates.</param>
		/// <param name="bottom">Position in screen relative coordinates.</param>
		/// <param name="updateAABB"></param>
		public void SetCorners( float left, float top, float right, float bottom, bool updateAABB )
		{
			float[] data = new float[] {
				left, top, -1,
				left, bottom, -1,
				right, top, -1, // Fix for Issue #1187096
				right, bottom, -1
			};

			HardwareVertexBuffer buffer = vertexData.vertexBufferBinding.GetBuffer( POSITION );

			buffer.SetData( data, 0, buffer.Length, true );

			if ( updateAABB )
			{
				box = new AxisAlignedBox();
				box.SetExtents( new Vector3( left, top, 0 ), new Vector3( right, bottom, 0 ) );
			}
		}
		/// <summary>
		/// Sets the normals of the rectangle
		/// </summary>
		/// <param name="left"></param>
		/// <param name="top"></param>
		/// <param name="right"></param>
		/// <param name="bottom"></param>
		public void SetNormals( Vector3 topLeft, Vector3 bottomLeft, Vector3 topRight, Vector3 bottomRight )
		{
			HardwareVertexBuffer vbuf = renderOperation.vertexData.vertexBufferBinding.GetBuffer( NORMAL );
			float[] normals = new float[] { 				
				topLeft.x,
				topLeft.y,
				topLeft.z,

				bottomLeft.x,
				bottomLeft.y,
				bottomLeft.z,

				topRight.x,
				topRight.y,
				topRight.z,

				bottomRight.x,
				bottomRight.y,
				bottomRight.z,
			};
			vbuf.SetData( normals, 0, normals.Length, true );
		}
		#endregion Methods
	}
}
