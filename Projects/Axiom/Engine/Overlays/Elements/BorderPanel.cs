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
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using Axiom.Core;
using Axiom.Math;
using Axiom.Scripting;
using Axiom.Graphics;
using Axiom.Core.Collections;

#endregion Namespace Declarations

#region Ogre Synchronization Information

// <ogresynchronization>
//     <file name="OgreBorderPanelOverlayElement.h"   revision="1.6.2.3" lastUpdated="10/5/2005" lastUpdatedBy="DanielH" />
//     <file name="OgreBorderPanelOverlayElement.cpp" revision="1.10" lastUpdated="10/5/2005" lastUpdatedBy="DanielH" />
// </ogresynchronization>

#endregion Ogre Synchronization Information

namespace Axiom.Overlays.Elements
{
	/// <summary>
	/// 	A specialization of the Panel element to provide a panel with a border with a seperate material.
	/// </summary>
	/// <remarks>
	/// 	Whilst the standard panel can use a single tiled material, this class allows
	/// 	panels with a tileable backdrop plus a border texture. This is handy for large
	/// 	panels that are too big to use a single large texture with a border, or
	/// 	for multiple different size panels where you want the border a constant width
	/// 	but the center to repeat.
	/// 	<p/>
	/// 	In addition to the usual PanelGuiElement properties, this class has a 'border
	/// 	material', which specifies the material used for the edges of the panel,
	/// 	a border width (which can either be constant all the way around, or specified
	/// 	per edge), and the texture coordinates for each of the border sections.
	/// </remarks>
	public class BorderPanel : Panel
	{
		#region Member variables

		protected float leftBorderSize;
		protected float rightBorderSize;
		protected float topBorderSize;
		protected float bottomBorderSize;

		protected CellUV[] borderUV = new CellUV[ 8 ];

		protected short pixelLeftBorderSize;
		protected short pixelRightBorderSize;
		protected short pixelTopBorderSize;
		protected short pixelBottomBorderSize;

		protected string borderMaterialName;
		// border material, internal so BorderRenderable can access
		protected Material borderMaterial;

		// Render operation for the border area, internal so BorderRenderable can access
		protected RenderOperation renderOp2 = new RenderOperation();
		protected BorderRenderable borderRenderable;

		// buffer soruce bindings
		const int POSITION = 0;
		const int TEXCOORDS = 1;

		// temp array for use during position updates, prevents constant memory allocation
		private float[] lefts = new float[ 8 ];
		private float[] rights = new float[ 8 ];
		private float[] tops = new float[ 8 ];
		private float[] bottoms = new float[ 8 ];

		#endregion Member variables

		#region Constructors

		/// <summary>
		///    Internal constructor, used when objects create by the factory.
		/// </summary>
		/// <param name="name"></param>
		internal BorderPanel( string name )
			: base( name )
		{
			for ( int x = 0; x < 8; x++ )
			{
				borderUV[ x ] = new CellUV();
			}
		}

		#endregion Constructors

		#region Methods

        /// <summary>
        /// 
        /// </summary>
        /// <param name="disposeManagedResources"></param>
        protected override void dispose(bool disposeManagedResources)
        {
            if (!this.IsDisposed)
            {
                if (disposeManagedResources)
                {
                    if (this.renderOp2 != null)
                    {
                        if (!this.renderOp2.IsDisposed)
                            this.renderOp2.Dispose();

                        this.renderOp2 = null;
                    }
                }
            }

            base.dispose(disposeManagedResources);
        }

		protected override void UpdateTextureGeometry()
		{
			base.UpdateTextureGeometry();

			/* Each cell is
				0-----2
				|    /|
				|  /  |
				|/    |
				1-----3
			*/

			// No choice but to lock / unlock each time here, but lock only small sections

			HardwareVertexBuffer vbuf =	renderOp2.vertexData.vertexBufferBinding.GetBuffer( BorderPanel.TEXCOORDS );
			// Can't use discard since this discards whole buffer
			IntPtr data = vbuf.Lock( BufferLocking.Discard );
			int index = 0;
			unsafe
			{
				float* idxPtr = (float*)data.ToPointer();

				for ( short i = 0; i < 8; i++ )
				{
					idxPtr[ index++ ] = borderUV[ i ].u1;
					idxPtr[ index++ ] = borderUV[ i ].v1;
					idxPtr[ index++ ] = borderUV[ i ].u1;
					idxPtr[ index++ ] = borderUV[ i ].v2;
					idxPtr[ index++ ] = borderUV[ i ].u2;
					idxPtr[ index++ ] = borderUV[ i ].v1;
					idxPtr[ index++ ] = borderUV[ i ].u2;
					idxPtr[ index++ ] = borderUV[ i ].v2;
				}
			}

			vbuf.Unlock();
		}

		public void GetLeftBorderUV( out float u1, out float v1, out float u2, out float v2 )
		{
			u1 = borderUV[ (int)BorderCell.Left ].u1;
			u2 = borderUV[ (int)BorderCell.Left ].u2;
			v1 = borderUV[ (int)BorderCell.Left ].v1;
			v2 = borderUV[ (int)BorderCell.Left ].v2;
		}
		public void SetLeftBorderUV( float u1, float v1, float u2, float v2 )
		{
			borderUV[ (int)BorderCell.Left ].u1 = u1;
			borderUV[ (int)BorderCell.Left ].u2 = u2;
			borderUV[ (int)BorderCell.Left ].v1 = v1;
			borderUV[ (int)BorderCell.Left ].v2 = v2;
			isGeomUVsOutOfDate = true;
		}
		//---------------------------------------------------------------------
		public void GetRightBorderUV( out float u1, out float v1, out float u2, out float v2 )
		{
			u1 = borderUV[ (int)BorderCell.Right ].u1;
			u2 = borderUV[ (int)BorderCell.Right ].u2;
			v1 = borderUV[ (int)BorderCell.Right ].v1;
			v2 = borderUV[ (int)BorderCell.Right ].v2;
		}
		public void SetRightBorderUV( float u1, float v1, float u2, float v2 )
		{
			borderUV[ (int)BorderCell.Right ].u1 = u1;
			borderUV[ (int)BorderCell.Right ].u2 = u2;
			borderUV[ (int)BorderCell.Right ].v1 = v1;
			borderUV[ (int)BorderCell.Right ].v2 = v2;
			isGeomUVsOutOfDate = true;
		}
		//---------------------------------------------------------------------
		public void GetTopBorderUV( out float u1, out float v1, out float u2, out float v2 )
		{
			u1 = borderUV[ (int)BorderCell.Top ].u1;
			u2 = borderUV[ (int)BorderCell.Top ].u2;
			v1 = borderUV[ (int)BorderCell.Top ].v1;
			v2 = borderUV[ (int)BorderCell.Top ].v2;
		}
		public void SetTopBorderUV( float u1, float v1, float u2, float v2 )
		{
			borderUV[ (int)BorderCell.Top ].u1 = u1;
			borderUV[ (int)BorderCell.Top ].u2 = u2;
			borderUV[ (int)BorderCell.Top ].v1 = v1;
			borderUV[ (int)BorderCell.Top ].v2 = v2;
			isGeomUVsOutOfDate = true;
		}
		//---------------------------------------------------------------------
		public void GetBottomBorderUV( out float u1, out float v1, out float u2, out float v2 )
		{
			u1 = borderUV[ (int)BorderCell.Bottom ].u1;
			u2 = borderUV[ (int)BorderCell.Bottom ].u2;
			v1 = borderUV[ (int)BorderCell.Bottom ].v1;
			v2 = borderUV[ (int)BorderCell.Bottom ].v2;
		}
		public void SetBottomBorderUV( float u1, float v1, float u2, float v2 )
		{
			borderUV[ (int)BorderCell.Bottom ].u1 = u1;
			borderUV[ (int)BorderCell.Bottom ].u2 = u2;
			borderUV[ (int)BorderCell.Bottom ].v1 = v1;
			borderUV[ (int)BorderCell.Bottom ].v2 = v2;
			isGeomUVsOutOfDate = true;
		}
		//---------------------------------------------------------------------
		public void GetTopLeftBorderUV( out float u1, out float v1, out float u2, out float v2 )
		{
			u1 = borderUV[ (int)BorderCell.TopLeft ].u1;
			u2 = borderUV[ (int)BorderCell.TopLeft ].u2;
			v1 = borderUV[ (int)BorderCell.TopLeft ].v1;
			v2 = borderUV[ (int)BorderCell.TopLeft ].v2;
		}
		public void SetTopLeftBorderUV( float u1, float v1, float u2, float v2 )
		{
			borderUV[ (int)BorderCell.TopLeft ].u1 = u1;
			borderUV[ (int)BorderCell.TopLeft ].u2 = u2;
			borderUV[ (int)BorderCell.TopLeft ].v1 = v1;
			borderUV[ (int)BorderCell.TopLeft ].v2 = v2;
			isGeomUVsOutOfDate = true;
		}
		//---------------------------------------------------------------------
		public void GetTopRightBorderUV( out float u1, out float v1, out float u2, out float v2 )
		{
			u1 = borderUV[ (int)BorderCell.TopRight ].u1;
			u2 = borderUV[ (int)BorderCell.TopRight ].u2;
			v1 = borderUV[ (int)BorderCell.TopRight ].v1;
			v2 = borderUV[ (int)BorderCell.TopRight ].v2;
		}
		public void SetTopRightBorderUV( float u1, float v1, float u2, float v2 )
		{
			borderUV[ (int)BorderCell.TopRight ].u1 = u1;
			borderUV[ (int)BorderCell.TopRight ].u2 = u2;
			borderUV[ (int)BorderCell.TopRight ].v1 = v1;
			borderUV[ (int)BorderCell.TopRight ].v2 = v2;
			isGeomUVsOutOfDate = true;
		}
		//---------------------------------------------------------------------
		public void GetBottomLeftBorderUV( out float u1, out float v1, out float u2, out float v2 )
		{
			u1 = borderUV[ (int)BorderCell.BottomLeft ].u1;
			u2 = borderUV[ (int)BorderCell.BottomLeft ].u2;
			v1 = borderUV[ (int)BorderCell.BottomLeft ].v1;
			v2 = borderUV[ (int)BorderCell.BottomLeft ].v2;
		}
		public void SetBottomLeftBorderUV( float u1, float v1, float u2, float v2 )
		{
			borderUV[ (int)BorderCell.BottomLeft ].u1 = u1;
			borderUV[ (int)BorderCell.BottomLeft ].u2 = u2;
			borderUV[ (int)BorderCell.BottomLeft ].v1 = v1;
			borderUV[ (int)BorderCell.BottomLeft ].v2 = v2;
			isGeomUVsOutOfDate = true;
		}
		//---------------------------------------------------------------------
		public void GetBottomRightBorderUV( out float u1, out float v1, out float u2, out float v2 )
		{
			u1 = borderUV[ (int)BorderCell.BottomRight ].u1;
			u2 = borderUV[ (int)BorderCell.BottomRight ].u2;
			v1 = borderUV[ (int)BorderCell.BottomRight ].v1;
			v2 = borderUV[ (int)BorderCell.BottomRight ].v2;
		}
		public void SetBottomRightBorderUV( float u1, float v1, float u2, float v2 )
		{
			borderUV[ (int)BorderCell.BottomRight ].u1 = u1;
			borderUV[ (int)BorderCell.BottomRight ].u2 = u2;
			borderUV[ (int)BorderCell.BottomRight ].v1 = v1;
			borderUV[ (int)BorderCell.BottomRight ].v2 = v2;
			isGeomUVsOutOfDate = true;
		}

		/// <summary>
		///    Override from Panel.
		/// </summary>
		public override void Initialize()
		{
			bool init = !isInitialized;
			base.Initialize();

			// superclass will handle the interior panel area
			if ( init )
			{
				// base class already has added the center panel at this point, so lets create the borders
				renderOp2.vertexData = new VertexData();
				// 8 * 4, cant resuse vertices because they might not share same tex coords
				renderOp2.vertexData.vertexCount = 32;
				renderOp2.vertexData.vertexStart = 0;

				// get a reference to the vertex declaration
				VertexDeclaration decl = renderOp2.vertexData.vertexDeclaration;
				// Position and texture coords each have their own buffers to allow
				// each to be edited separately with the discard flag
				decl.AddElement( POSITION, 0, VertexElementType.Float3, VertexElementSemantic.Position );
				decl.AddElement( TEXCOORDS, 0, VertexElementType.Float2, VertexElementSemantic.TexCoords, 0 );

				// position buffer
				HardwareVertexBuffer buffer =
					HardwareBufferManager.Instance.CreateVertexBuffer(
						decl.GetVertexSize( POSITION ),
						renderOp2.vertexData.vertexCount,
						BufferUsage.StaticWriteOnly );

				// bind position
				VertexBufferBinding binding = renderOp2.vertexData.vertexBufferBinding;
				binding.SetBinding( POSITION, buffer );

				// texcoord buffer
				buffer =
					HardwareBufferManager.Instance.CreateVertexBuffer(
					decl.GetVertexSize( TEXCOORDS ),
					renderOp2.vertexData.vertexCount,
						BufferUsage.StaticWriteOnly, true );

				// bind texcoords
				binding = renderOp2.vertexData.vertexBufferBinding;
				binding.SetBinding( TEXCOORDS, buffer );

				renderOp2.operationType = OperationType.TriangleList;
				renderOp2.useIndices = true;

				// index data
				renderOp2.indexData = new IndexData();
				// 8 * 3 * 2 = 8 vertices, 3 indices per tri, 2 tris
				renderOp2.indexData.indexCount = 48;
				renderOp2.indexData.indexStart = 0;

				/* Each cell is
					0-----2
					|    /|
					|  /  |
					|/    |
					1-----3
				*/

				// create a new index buffer
				renderOp2.indexData.indexBuffer =
					HardwareBufferManager.Instance.CreateIndexBuffer(
						IndexType.Size16,
						renderOp2.indexData.indexCount,
						BufferUsage.StaticWriteOnly );

				// lock this bad boy
				IntPtr data = renderOp2.indexData.indexBuffer.Lock( BufferLocking.Discard );
				int index = 0;
				unsafe
				{
					short* idxPtr = (short*)data.ToPointer();

					for ( short cell = 0; cell < 8; cell++ )
					{
						short val = (short)( cell * 4 );
						idxPtr[ index++ ] = val;
						idxPtr[ index++ ] = (short)( val + 1 );
						idxPtr[ index++ ] = (short)( val + 2 );

						idxPtr[ index++ ] = (short)( val + 2 );
						idxPtr[ index++ ] = (short)( val + 1 );
						idxPtr[ index++ ] = (short)( val + 3 );
					}
				}

				// unlock the buffer
				renderOp2.indexData.indexBuffer.Unlock();

				// create new seperate object for the panels since they have a different material
				borderRenderable = new BorderRenderable( this );
				isInitialized = true;
			}
		}

		/// <summary>
		///    Sets the size of the border.
		/// </summary>
		/// <remarks>
		///    This method sets a constant size for all borders. There are also alternative
		///    methods which allow you to set border widths for individual edges separately.
		///    Remember that the dimensions specified here are in relation to the size of
		///    the screen, so 0.1 is 1/10th of the screen width or height. Also note that because
		///    most screen resolutions are 1.333:1 width:height ratio that using the same
		///    border size will look slightly bigger across than up.
		/// </remarks>
		/// <param name="size">The size of the border as a factor of the screen dimensions ie 0.2 is one-fifth
		///    of the screen size.
		/// </param>
		public void SetBorderSize( float size )
		{
			if ( metricsMode != MetricsMode.Pixels )
			{
				pixelTopBorderSize = pixelRightBorderSize = pixelLeftBorderSize = pixelBottomBorderSize = (short)size;
			}
			else
			{
				topBorderSize = rightBorderSize = leftBorderSize = bottomBorderSize = size;
			}
			isGeomPositionsOutOfDate = true;
		}

		/// <summary>
		///    Sets the size of the border, with different sizes for vertical and horizontal borders.
		/// </summary>
		/// <remarks>
		///    This method sets a size for the side and top / bottom borders separately.
		///    Remember that the dimensions specified here are in relation to the size of
		///    the screen, so 0.1 is 1/10th of the screen width or height. Also note that because
		///    most screen resolutions are 1.333:1 width:height ratio that using the same
		///    border size will look slightly bigger across than up.
		/// </remarks>
		/// <param name="sides">The size of the side borders as a factor of the screen dimensions ie 0.2 is one-fifth
		///    of the screen size.</param>
		/// <param name="topAndBottom">The size of the top and bottom borders as a factor of the screen dimensions.</param>
		public void SetBorderSize( float sides, float topAndBottom )
		{
			if ( metricsMode != MetricsMode.Relative )
			{
				pixelRightBorderSize = pixelLeftBorderSize = (short)sides;
				pixelTopBorderSize = pixelBottomBorderSize = (short)topAndBottom;
			}
			else
			{
				topBorderSize = bottomBorderSize = topAndBottom;
				rightBorderSize = leftBorderSize = sides;
			}
			isGeomPositionsOutOfDate = true;
		}

		/// <summary>
		///    Sets the size of the border separately for all borders.
		/// </summary>
		/// <remarks>
		///    This method sets a size all borders separately.
		///    Remember that the dimensions specified here are in relation to the size of
		///    the screen, so 0.1 is 1/10th of the screen width or height. Also note that because
		///    most screen resolutions are 1.333:1 width:height ratio that using the same
		///    border size will look slightly bigger across than up.
		/// </remarks>
		/// <param name="left">The size of the left border as a factor of the screen dimensions ie 0.2 is one-fifth
		/// of the screen size.</param>
		/// <param name="right">The size of the right border as a factor of the screen dimensions.</param>
		/// <param name="top">The size of the top border as a factor of the screen dimensions.</param>
		/// <param name="bottom">The size of the bottom border as a factor of the screen dimensions.</param>
		public void SetBorderSize( float left, float right, float top, float bottom )
		{
			if ( metricsMode != MetricsMode.Relative )
			{
				pixelTopBorderSize = (short)top;
				pixelBottomBorderSize = (short)bottom;
				pixelRightBorderSize = (short)right;
				pixelLeftBorderSize = (short)left;
			}
			else
			{
				topBorderSize = top;
				bottomBorderSize = bottom;
				rightBorderSize = right;
				leftBorderSize = left;
			}
			isGeomPositionsOutOfDate = true;
		}

		/// <summary>
		///    Sets the texture coordinates for the left edge of the border.
		/// </summary>
		/// <remarks>
		///    The border panel uses 8 panels for the border (9 including the center).
		///    Imagine a table with 3 rows and 3 columns. The corners are always the same size,
		///    but the edges stretch depending on how big the panel is. Those who have done
		///    resizable HTML tables will be familiar with this approach.
		///    <p/>
		///    We only require 2 sets of uv coordinates, one for the top-left and one for the
		///    bottom-right of the panel, since it is assumed the sections are aligned on the texture.
		/// </remarks>
		/// <param name="cell">Index of the cell to update.</param>
		/// <param name="u1">Top left u.</param>
		/// <param name="v1">Top left v.</param>
		/// <param name="u2">Bottom right u.</param>
		/// <param name="v2">Bottom right v.</param>
		public void SetCellUV( BorderCell cell, float u1, float v1, float u2, float v2 )
		{
			int cellIndex = (int)cell;

			// no choice but to lock/unlock each time here, locking only what we want to modify
			HardwareVertexBuffer buffer =
				renderOp2.vertexData.vertexBufferBinding.GetBuffer( TEXCOORDS );

			// can't use discard, or it will discard the whole buffer, wiping out the positions too
			IntPtr data = buffer.Lock(
				cellIndex * 8 * Marshal.SizeOf( typeof( float ) ),
				Marshal.SizeOf( typeof( float ) ) * 8,
				BufferLocking.Normal );

			int index = 0;

			unsafe
			{
				float* texPtr = (float*)data.ToPointer();

				texPtr[ index++ ] = u1;
				texPtr[ index++ ] = v1;
				texPtr[ index++ ] = u1;
				texPtr[ index++ ] = v2;
				texPtr[ index++ ] = u2;
				texPtr[ index++ ] = v1;
				texPtr[ index++ ] = u2;
				texPtr[ index ] = v2;
			}

			buffer.Unlock();
		}

		/// <summary>
		///    Overriden from Panel.
		/// </summary>
		public override void Update()
		{
			if ( metricsMode != MetricsMode.Relative &&
				( OverlayManager.Instance.HasViewportChanged || isGeomPositionsOutOfDate ) )
			{
				leftBorderSize = pixelLeftBorderSize * pixelScaleX;
				rightBorderSize = pixelRightBorderSize * pixelScaleX;
				topBorderSize = pixelTopBorderSize * pixelScaleY;
				bottomBorderSize = pixelBottomBorderSize * pixelScaleY;
				isGeomPositionsOutOfDate = true;
			}
			base.Update();
		}

		/// <summary>
		///    Override from Panel.
		/// </summary>
		protected override void UpdatePositionGeometry()
		{
			/*
			Grid is like this:
			+--+---------------+--+
			|0 |       1       |2 |
			+--+---------------+--+
			|  |               |  |
			|  |               |  |
			|3 |    center     |4 |
			|  |               |  |
			+--+---------------+--+
			|5 |       6       |7 |
			+--+---------------+--+
			*/
			// Convert positions into -1, 1 coordinate space (homogenous clip space)
			// Top / bottom also need inverting since y is upside down

			// Horizontal
			lefts[ 0 ] = lefts[ 3 ] = lefts[ 5 ] = this.DerivedLeft * 2 - 1;
			lefts[ 1 ] = lefts[ 6 ] = rights[ 0 ] = rights[ 3 ] = rights[ 5 ] = lefts[ 0 ] + ( leftBorderSize * 2 );
			rights[ 2 ] = rights[ 4 ] = rights[ 7 ] = lefts[ 0 ] + ( width * 2 );
			lefts[ 2 ] = lefts[ 4 ] = lefts[ 7 ] = rights[ 1 ] = rights[ 6 ] = rights[ 2 ] - ( rightBorderSize * 2 );
			// Vertical
			tops[ 0 ] = tops[ 1 ] = tops[ 2 ] = -( ( this.DerivedTop * 2 ) - 1 );
			tops[ 3 ] = tops[ 4 ] = bottoms[ 0 ] = bottoms[ 1 ] = bottoms[ 2 ] = tops[ 0 ] - ( topBorderSize * 2 );
			bottoms[ 5 ] = bottoms[ 6 ] = bottoms[ 7 ] = tops[ 0 ] - ( height * 2 );
			tops[ 5 ] = tops[ 6 ] = tops[ 7 ] = bottoms[ 3 ] = bottoms[ 4 ] = bottoms[ 5 ] + ( bottomBorderSize * 2 );

			// get a reference to the buffer
			HardwareVertexBuffer buffer =
				renderOp2.vertexData.vertexBufferBinding.GetBuffer( POSITION );

			// lock this bad boy
			IntPtr data = buffer.Lock( BufferLocking.Discard );
			int index = 0;

			//float zValue = Root.Instance.RenderSystem.MaximumDepthInputValue;
			//float zValue = -1;
			unsafe
			{
				float* posPtr = (float*)data.ToPointer();
				for ( int cell = 0; cell < 8; cell++ )
				{
					posPtr[ index++ ] = lefts[ cell ];
					posPtr[ index++ ] = tops[ cell ];
					posPtr[ index++ ] = -1;

					posPtr[ index++ ] = lefts[ cell ];
					posPtr[ index++ ] = bottoms[ cell ];
					posPtr[ index++ ] = -1;

					posPtr[ index++ ] = rights[ cell ];
					posPtr[ index++ ] = tops[ cell ];
					posPtr[ index++ ] = -1;

					posPtr[ index++ ] = rights[ cell ];
					posPtr[ index++ ] = bottoms[ cell ];
					posPtr[ index++ ] = -1;
				} // for
			} // unsafe

			// unlock the position buffer
			buffer.Unlock();

			// Also update center geometry
			// don't use base class because we need to make it smaller because of border
			buffer = renderOperation.vertexData.vertexBufferBinding.GetBuffer( POSITION );
			data = buffer.Lock( BufferLocking.Discard );

			index = 0;

			unsafe
			{
				float* posPtr = (float*)data.ToPointer();

				posPtr[ index++ ] = lefts[ 1 ];
				posPtr[ index++ ] = tops[ 3 ];
				posPtr[ index++ ] = -1;

				posPtr[ index++ ] = lefts[ 1 ];
				posPtr[ index++ ] = bottoms[ 3 ];
				posPtr[ index++ ] = -1;

				posPtr[ index++ ] = rights[ 1 ];
				posPtr[ index++ ] = tops[ 3 ];
				posPtr[ index++ ] = -1;

				posPtr[ index++ ] = rights[ 1 ];
				posPtr[ index++ ] = bottoms[ 3 ];
				posPtr[ index ] = -1;
			}

			// unlock the buffer to finish
			buffer.Unlock();
		}

		/// <summary>
		///    Overriden from Panel.
		/// </summary>
		/// <param name="queue"></param>
		public override void UpdateRenderQueue( RenderQueue queue )
		{
			// Add self twice to the queue
			// Have to do this to allow 2 materials
			if ( isVisible )
			{
				// add border first
				queue.AddRenderable( borderRenderable, (ushort)zOrder, RenderQueueGroupID.Overlay );

				// do inner last so the border artifacts don't overwrite the children
				// Add inner
				base.UpdateRenderQueue( queue );
			}
		}

		#endregion Methods

		#region Properties

		/// <summary>
		///    Gets the size of the left border.
		/// </summary>
		public float LeftBorderSize
		{
			get
			{
				if ( metricsMode == MetricsMode.Pixels )
				{
					return pixelLeftBorderSize;
				}
				else
				{
					return leftBorderSize;
				}
			}
		}

		/// <summary>
		///    Gets the size of the right border.
		/// </summary>
		public float RightBorderSize
		{
			get
			{
				if ( metricsMode == MetricsMode.Pixels )
				{
					return pixelRightBorderSize;
				}
				else
				{
					return rightBorderSize;
				}
			}
		}

		/// <summary>
		///    Gets the size of the top border.
		/// </summary>
		public float TopBorderSize
		{
			get
			{
				if ( metricsMode == MetricsMode.Pixels )
				{
					return pixelTopBorderSize;
				}
				else
				{
					return topBorderSize;
				}
			}
		}

		/// <summary>
		///    Gets the size of the bottom border.
		/// </summary>
		public float BottomBorderSize
		{
			get
			{
				if ( metricsMode == MetricsMode.Pixels )
				{
					return pixelBottomBorderSize;
				}
				else
				{
					return bottomBorderSize;
				}
			}
		}

		/// <summary>
		///    Gets/Sets the name of the material to use for just the borders.
		/// </summary>
		public string BorderMaterialName
		{
			get
			{
				return borderMaterialName;
			}
			set
			{
				borderMaterialName = value;
				borderMaterial = (Material)MaterialManager.Instance[ borderMaterialName ];

				if ( borderMaterial == null )
				{
					throw new Exception( string.Format( "Could not find material '{0}'.", borderMaterialName ) );
				}
				borderMaterial.Load();
				// Set some prerequisites to be sure
				borderMaterial.Lighting = false;
				borderMaterial.DepthCheck = false;
			}
		}

		/// <summary>
		///    Override of Panel.
		/// </summary>
		public override MetricsMode MetricsMode
		{
			get
			{
				return base.MetricsMode;
			}
			set
			{
				base.MetricsMode = value;

				if ( value != MetricsMode.Relative )
				{
					pixelBottomBorderSize = (short)bottomBorderSize;
					pixelLeftBorderSize = (short)leftBorderSize;
					pixelRightBorderSize = (short)rightBorderSize;
					pixelTopBorderSize = (short)topBorderSize;
				}
			}
		}

		#endregion Properties

		#region ScriptableObject Interface Command Classes

		[ScriptableProperty( "border_size", "", typeof( BorderPanel ) )]
		private class BorderSizeAttributeCommand : IPropertyCommand
		{
			#region Implementation of IPropertyCommand<object,string>

			/// <summary>
			///    Gets the value for this command from the target object.
			/// </summary>
			/// <param name="target"></param>
			/// <returns></returns>
			public string Get( object target )
			{
				var element = target as BorderPanel;
				if ( element != null )
				{
					return String.Format( "{0} {1} {2} {3}", element.LeftBorderSize, element.RightBorderSize, element.TopBorderSize, element.BottomBorderSize );
				}
				else
				{
					return String.Empty;
				}
			}

			/// <summary>
			///    Sets the value for this command on the target object.
			/// </summary>
			/// <param name="target"></param>
			/// <param name="val"></param>
			public void Set( object target, string val )
			{
				var element = target as BorderPanel;
				var parms = val.Split( ' ' );
				if ( element != null )
				{
					Real left = StringConverter.ParseFloat( parms[ 0 ] ),
						 right = StringConverter.ParseFloat( parms[ 1 ] ),
						 top = StringConverter.ParseFloat( parms[ 2 ] ),
						 bottom = StringConverter.ParseFloat( parms[ 3 ] );
					element.SetBorderSize( left, right, top, bottom );
				}
			}

			#endregion Implementation of IPropertyCommand<object,string>
		}

		[ScriptableProperty( "border_material", "", typeof( BorderPanel ) )]
		private class BorderMaterialHeightAttributeCommand : IPropertyCommand
		{
			#region Implementation of IPropertyCommand<object,string>

			/// <summary>
			///    Gets the value for this command from the target object.
			/// </summary>
			/// <param name="target"></param>
			/// <returns></returns>
			public string Get( object target )
			{
				var element = target as BorderPanel;
				if ( element != null )
				{
					return element.BorderMaterialName;
				}
				else
				{
					return String.Empty;
				}
			}

			/// <summary>
			///    Sets the value for this command on the target object.
			/// </summary>
			/// <param name="target"></param>
			/// <param name="val"></param>
			public void Set( object target, string val )
			{
				var element = target as BorderPanel;
				if ( element != null )
				{
					element.BorderMaterialName = val;
				}
			}

			#endregion Implementation of IPropertyCommand<object,string>
		}

		[ScriptableProperty( "border_topleft_uv", "", typeof( BorderPanel ) )]
		private class BorderTopLeftUVAttributeCommand : IPropertyCommand
		{
			#region Implementation of IPropertyCommand<object,string>

			/// <summary>
			///    Gets the value for this command from the target object.
			/// </summary>
			/// <param name="target"></param>
			/// <returns></returns>
			public string Get( object target )
			{
				var element = target as BorderPanel;
				if ( element != null )
				{
					float u1, v1, u2, v2;
					element.GetTopLeftBorderUV( out u1, out v1, out u2, out v2 );
					return String.Format( "{0} {1} {2} {3}", u1, v1, u2, v2 );
				}
				else
				{
					return String.Empty;
				}
			}

			/// <summary>
			///    Sets the value for this command on the target object.
			/// </summary>
			/// <param name="target"></param>
			/// <param name="val"></param>
			public void Set( object target, string val )
			{
				var element = target as BorderPanel;
				if ( element != null )
				{
					var parms = val.Split( ' ' );
					Real u1 = StringConverter.ParseFloat( parms[ 0 ] ),
						 v1 = StringConverter.ParseFloat( parms[ 1 ] ),
						 u2 = StringConverter.ParseFloat( parms[ 2 ] ),
						 v2 = StringConverter.ParseFloat( parms[ 3 ] );

					element.SetTopLeftBorderUV( u1, v1, u2, v2 );
				}
			}

			#endregion Implementation of IPropertyCommand<object,string>
		}

		[ScriptableProperty( "border_topright_uv", "", typeof( BorderPanel ) )]
		private class BorderTopRightUVAttributeCommand : IPropertyCommand
		{
			#region Implementation of IPropertyCommand<object,string>

			/// <summary>
			///    Gets the value for this command from the target object.
			/// </summary>
			/// <param name="target"></param>
			/// <returns></returns>
			public string Get( object target )
			{
				var element = target as BorderPanel;
				if ( element != null )
				{
					float u1, v1, u2, v2;
					element.GetTopRightBorderUV( out u1, out v1, out u2, out v2 );
					return String.Format( "{0} {1} {2} {3}", u1, v1, u2, v2 );
				}
				else
				{
					return String.Empty;
				}
			}

			/// <summary>
			///    Sets the value for this command on the target object.
			/// </summary>
			/// <param name="target"></param>
			/// <param name="val"></param>
			public void Set( object target, string val )
			{
				var element = target as BorderPanel;
				if ( element != null )
				{
					var parms = val.Split( ' ' );
					Real u1 = StringConverter.ParseFloat( parms[ 0 ] ),
						 v1 = StringConverter.ParseFloat( parms[ 1 ] ),
						 u2 = StringConverter.ParseFloat( parms[ 2 ] ),
						 v2 = StringConverter.ParseFloat( parms[ 3 ] );

					element.SetTopRightBorderUV( u1, v1, u2, v2 );
				}
			}

			#endregion Implementation of IPropertyCommand<object,string>
		}

		[ScriptableProperty( "border_bottomleft_uv", "", typeof( BorderPanel ) )]
		private class BorderBottomLeftUVAttributeCommand : IPropertyCommand
		{
			#region Implementation of IPropertyCommand<object,string>

			/// <summary>
			///    Gets the value for this command from the target object.
			/// </summary>
			/// <param name="target"></param>
			/// <returns></returns>
			public string Get( object target )
			{
				var element = target as BorderPanel;
				if ( element != null )
				{
					float u1, v1, u2, v2;
					element.GetBottomLeftBorderUV( out u1, out v1, out u2, out v2 );
					return String.Format( "{0} {1} {2} {3}", u1, v1, u2, v2 );
				}
				else
				{
					return String.Empty;
				}
			}

			/// <summary>
			///    Sets the value for this command on the target object.
			/// </summary>
			/// <param name="target"></param>
			/// <param name="val"></param>
			public void Set( object target, string val )
			{
				var element = target as BorderPanel;
				if ( element != null )
				{
					var parms = val.Split( ' ' );
					Real u1 = StringConverter.ParseFloat( parms[ 0 ] ),
						 v1 = StringConverter.ParseFloat( parms[ 1 ] ),
						 u2 = StringConverter.ParseFloat( parms[ 2 ] ),
						 v2 = StringConverter.ParseFloat( parms[ 3 ] );

					element.SetBottomLeftBorderUV( u1, v1, u2, v2 );
				}
			}

			#endregion Implementation of IPropertyCommand<object,string>
		}

		[ScriptableProperty( "border_bottomright_uv", "", typeof( BorderPanel ) )]
		private class BorderBottomRightUVAttributeCommand : IPropertyCommand
		{
			#region Implementation of IPropertyCommand<object,string>

			/// <summary>
			///    Gets the value for this command from the target object.
			/// </summary>
			/// <param name="target"></param>
			/// <returns></returns>
			public string Get( object target )
			{
				var element = target as BorderPanel;
				if ( element != null )
				{
					float u1, v1, u2, v2;
					element.GetBottomRightBorderUV( out u1, out v1, out u2, out v2 );
					return String.Format( "{0} {1} {2} {3}", u1, v1, u2, v2 );
				}
				else
				{
					return String.Empty;
				}
			}

			/// <summary>
			///    Sets the value for this command on the target object.
			/// </summary>
			/// <param name="target"></param>
			/// <param name="val"></param>
			public void Set( object target, string val )
			{
				var element = target as BorderPanel;
				if ( element != null )
				{
					var parms = val.Split( ' ' );
					Real u1 = StringConverter.ParseFloat( parms[ 0 ] ),
						 v1 = StringConverter.ParseFloat( parms[ 1 ] ),
						 u2 = StringConverter.ParseFloat( parms[ 2 ] ),
						 v2 = StringConverter.ParseFloat( parms[ 3 ] );

					element.SetBottomRightBorderUV( u1, v1, u2, v2 );
				}
			}

			#endregion Implementation of IPropertyCommand<object,string>
		}

		[ScriptableProperty( "border_left_uv", "", typeof( BorderPanel ) )]
		private class BorderLeftUVAttributeCommand : IPropertyCommand
		{
			#region Implementation of IPropertyCommand<object,string>

			/// <summary>
			///    Gets the value for this command from the target object.
			/// </summary>
			/// <param name="target"></param>
			/// <returns></returns>
			public string Get( object target )
			{
				var element = target as BorderPanel;
				if ( element != null )
				{
					float u1, v1, u2, v2;
					element.GetLeftBorderUV( out u1, out v1, out u2, out v2 );
					return String.Format( "{0} {1} {2} {3}", u1, v1, u2, v2 );
				}
				else
				{
					return String.Empty;
				}
			}

			/// <summary>
			///    Sets the value for this command on the target object.
			/// </summary>
			/// <param name="target"></param>
			/// <param name="val"></param>
			public void Set( object target, string val )
			{
				var element = target as BorderPanel;
				if ( element != null )
				{
					var parms = val.Split( ' ' );
					Real u1 = StringConverter.ParseFloat( parms[ 0 ] ),
						 v1 = StringConverter.ParseFloat( parms[ 1 ] ),
						 u2 = StringConverter.ParseFloat( parms[ 2 ] ),
						 v2 = StringConverter.ParseFloat( parms[ 3 ] );

					element.SetLeftBorderUV( u1, v1, u2, v2 );
				}
			}

			#endregion Implementation of IPropertyCommand<object,string>
		}

		[ScriptableProperty( "border_top_uv", "", typeof( BorderPanel ) )]
		private class BorderTopUVAttributeCommand : IPropertyCommand
		{
			#region Implementation of IPropertyCommand<object,string>

			/// <summary>
			///    Gets the value for this command from the target object.
			/// </summary>
			/// <param name="target"></param>
			/// <returns></returns>
			public string Get( object target )
			{
				var element = target as BorderPanel;
				if ( element != null )
				{
					float u1, v1, u2, v2;
					element.GetTopBorderUV( out u1, out v1, out u2, out v2 );
					return String.Format( "{0} {1} {2} {3}", u1, v1, u2, v2 );
				}
				else
				{
					return String.Empty;
				}
			}

			/// <summary>
			///    Sets the value for this command on the target object.
			/// </summary>
			/// <param name="target"></param>
			/// <param name="val"></param>
			public void Set( object target, string val )
			{
				var element = target as BorderPanel;
				if ( element != null )
				{
					var parms = val.Split( ' ' );
					Real u1 = StringConverter.ParseFloat( parms[ 0 ] ),
						 v1 = StringConverter.ParseFloat( parms[ 1 ] ),
						 u2 = StringConverter.ParseFloat( parms[ 2 ] ),
						 v2 = StringConverter.ParseFloat( parms[ 3 ] );

					element.SetTopBorderUV( u1, v1, u2, v2 );
				}
			}

			#endregion Implementation of IPropertyCommand<object,string>
		}

		[ScriptableProperty( "border_right_uv", "", typeof( BorderPanel ) )]
		private class BorderRightUVAttributeCommand : IPropertyCommand
		{
			#region Implementation of IPropertyCommand<object,string>

			/// <summary>
			///    Gets the value for this command from the target object.
			/// </summary>
			/// <param name="target"></param>
			/// <returns></returns>
			public string Get( object target )
			{
				var element = target as BorderPanel;
				if ( element != null )
				{
					float u1, v1, u2, v2;
					element.GetRightBorderUV( out u1, out v1, out u2, out v2 );
					return String.Format( "{0} {1} {2} {3}", u1, v1, u2, v2 );
				}
				else
				{
					return String.Empty;
				}
			}

			/// <summary>
			///    Sets the value for this command on the target object.
			/// </summary>
			/// <param name="target"></param>
			/// <param name="val"></param>
			public void Set( object target, string val )
			{
				var element = target as BorderPanel;
				if ( element != null )
				{
					var parms = val.Split( ' ' );
					Real u1 = StringConverter.ParseFloat( parms[ 0 ] ),
						 v1 = StringConverter.ParseFloat( parms[ 1 ] ),
						 u2 = StringConverter.ParseFloat( parms[ 2 ] ),
						 v2 = StringConverter.ParseFloat( parms[ 3 ] );

					element.SetRightBorderUV( u1, v1, u2, v2 );
				}
			}

			#endregion Implementation of IPropertyCommand<object,string>
		}

		[ScriptableProperty( "border_bottom_uv", "", typeof( BorderPanel ) )]
		private class BorderBottomUVAttributeCommand : IPropertyCommand
		{
			#region Implementation of IPropertyCommand<object,string>

			/// <summary>
			///    Gets the value for this command from the target object.
			/// </summary>
			/// <param name="target"></param>
			/// <returns></returns>
			public string Get( object target )
			{
				var element = target as BorderPanel;
				if ( element != null )
				{
					float u1, v1, u2, v2;
					element.GetBottomBorderUV( out u1, out v1, out u2, out v2 );
					return String.Format( "{0} {1} {2} {3}", u1, v1, u2, v2 );
				}
				else
				{
					return String.Empty;
				}
			}

			/// <summary>
			///    Sets the value for this command on the target object.
			/// </summary>
			/// <param name="target"></param>
			/// <param name="val"></param>
			public void Set( object target, string val )
			{
				var element = target as BorderPanel;
				if ( element != null )
				{
					var parms = val.Split( ' ' );
					Real u1 = StringConverter.ParseFloat( parms[ 0 ] ),
						 v1 = StringConverter.ParseFloat( parms[ 1 ] ),
						 u2 = StringConverter.ParseFloat( parms[ 2 ] ),
						 v2 = StringConverter.ParseFloat( parms[ 3 ] );

					element.SetBottomBorderUV( u1, v1, u2, v2 );
				}
			}

			#endregion Implementation of IPropertyCommand<object,string>
		}

		#endregion ScriptableObject Interface Command Classes

		public class CellUV
		{
			public float u1, v1, u2, v2;
		};

		/// <summary>
		///    Class for rendering the border of a BorderPanel.
		/// </summary>
		/// <remarks>
		///    We need this because we have to render twice, once with the inner panel's repeating
		///    material (handled by superclass) and once for the border's separate meterial.
		/// </remarks>
		public class BorderRenderable : IRenderable
		{
			#region Member variables

			protected BorderPanel parent;

			private LightList emptyLightList = new LightList();

			protected List<Vector4> customParams = new List<Vector4>();

			#endregion Member variables

			#region Constructors

			/// <summary>
			///
			/// </summary>
			/// <param name="parent"></param>
			public BorderRenderable( BorderPanel parent )
			{
				this.parent = parent;
			}

			#endregion Constructors

			#region IRenderable Members

			public bool CastsShadows
			{
				get
				{
					return false;
				}
			}

			public float GetSquaredViewDepth( Camera camera )
			{
				return parent.GetSquaredViewDepth( camera );
			}

			public bool NormalizeNormals
			{
				get
				{
					return false;
				}
			}

			public bool UseIdentityView
			{
				get
				{
					return true;
				}
			}

			public bool UseIdentityProjection
			{
				get
				{
					return true;
				}
			}

			public RenderOperation RenderOperation
			{
				get
				{
					return this.parent.renderOp2;
				}
			}

			public void GetWorldTransforms( Matrix4[] matrices )
			{
				parent.GetWorldTransforms( matrices );
			}

			public virtual bool PolygonModeOverrideable
			{
				get
				{
					return parent.PolygonModeOverrideable;
				}
			}

			public Material Material
			{
				get
				{
					return parent.borderMaterial;
				}
			}

			public Technique Technique
			{
				get
				{
					return this.Material.GetBestTechnique();
				}
			}

			public ushort NumWorldTransforms
			{
				get
				{
					return 1;
				}
			}

			public Quaternion WorldOrientation
			{
				get
				{
					return Quaternion.Identity;
				}
			}

			public Vector3 WorldPosition
			{
				get
				{
					return Vector3.Zero;
				}
			}

			public LightList Lights
			{
				get
				{
					return emptyLightList;
				}
			}

			public Vector4 GetCustomParameter( int index )
			{
				if ( customParams[ index ] == null )
				{
					throw new Exception( "A parameter was not found at the given index" );
				}
				else
				{
					return (Vector4)customParams[ index ];
				}
			}

			public void SetCustomParameter( int index, Vector4 val )
			{
				while ( customParams.Count <= index )
					customParams.Add( Vector4.Zero );
				customParams[ index ] = val;
			}

			public void UpdateCustomGpuParameter( GpuProgramParameters.AutoConstantEntry entry, GpuProgramParameters gpuParams )
			{
				if ( customParams[ entry.Data ] != null )
				{
					gpuParams.SetConstant( entry.PhysicalIndex, (Vector4)customParams[ entry.Data ] );
				}
			}

			#endregion IRenderable Members

			#region IDisposable Implementation

			#region isDisposed Property

			private bool _disposed = false;

			/// <summary>
			/// Determines if this instance has been disposed of already.
			/// </summary>
			protected bool isDisposed
			{
				get
				{
					return _disposed;
				}
				set
				{
					_disposed = value;
				}
			}

			#endregion isDisposed Property

			/// <summary>
			/// Class level dispose method
			/// </summary>
			/// <remarks>
			/// When implementing this method in an inherited class the following template should be used;
			/// protected override void dispose( bool disposeManagedResources )
			/// {
			/// 	if ( !isDisposed )
			/// 	{
			/// 		if ( disposeManagedResources )
			/// 		{
			/// 			// Dispose managed resources.
			/// 		}
			///
			/// 		// There are no unmanaged resources to release, but
			/// 		// if we add them, they need to be released here.
			/// 	}
			///
			/// 	// If it is available, make the call to the
			/// 	// base class's Dispose(Boolean) method
			/// 	base.dispose( disposeManagedResources );
			/// }
			/// </remarks>
			/// <param name="disposeManagedResources">True if Unmanaged resources should be released.</param>
			protected virtual void dispose( bool disposeManagedResources )
			{
				if ( !isDisposed )
				{
					if ( disposeManagedResources )
					{
						// Dispose managed resources.
					}

					// There are no unmanaged resources to release, but
					// if we add them, they need to be released here.
				}
				isDisposed = true;
			}

			public void Dispose()
			{
				dispose( true );
				GC.SuppressFinalize( this );
			}

			#endregion IDisposable Implementation
		}

		/// <summary>
		///    Enum for border cells.
		/// </summary>
		public enum BorderCell
		{
			TopLeft,
			Top,
			TopRight,
			Left,
			Right,
			BottomLeft,
			Bottom,
			BottomRight
		};
	}
}