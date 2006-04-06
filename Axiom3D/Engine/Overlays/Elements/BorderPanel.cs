#region LGPL License
/*
Axiom Graphics Engine Library
Copyright (C) 2003-2006  Axiom Project Team

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

#region Namespace Declarations

using System;
using System.Collections;
using System.Runtime.InteropServices;

using DotNet3D.Math;

#endregion Namespace Declarations
			
#region Ogre Synchronization Information
/// <ogresynchronization>
///     <file name="OgreBorderPanelOverlayElement.h"   revision="1.6.2.3" lastUpdated="10/5/2005" lastUpdatedBy="DanielH" />
///     <file name="OgreBorderPanelOverlayElement.cpp" revision="1.10" lastUpdated="10/5/2005" lastUpdatedBy="DanielH" />
/// </ogresynchronization>
#endregion

namespace Axiom
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

        protected Real leftBorderSize;
        protected Real rightBorderSize;
        protected Real topBorderSize;
        protected Real bottomBorderSize;

		protected CellUV[] borderUV = new CellUV[8];

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
        private Real[] lefts = new Real[8];
        private Real[] rights = new Real[8];
        private Real[] tops = new Real[8];
        private Real[] bottoms = new Real[8];

        #endregion

        #region Constructors

        /// <summary>
        ///    Internal constructor, used when objects create by the factory.
        /// </summary>
        /// <param name="name"></param>
        internal BorderPanel( string name )
            : base( name )
        {
			for (int x = 0; x < 8;x++)
			{
				borderUV[x] = new CellUV();
			}
        }

        #endregion

        #region Methods
		protected override void UpdateTextureGeometry()
		{
			/* Each cell is
				0-----2
				|    /|
				|  /  |
				|/    |
				1-----3
			*/
			
			// No choice but to lock / unlock each time here, but lock only small sections
		    
			HardwareVertexBuffer vbuf =
				renderOp2.vertexData.vertexBufferBinding.GetBuffer(BorderPanel.TEXCOORDS);
			// Can't use discard since this discards whole buffer
			IntPtr data = vbuf.Lock( BufferLocking.Discard );
			int index = 0;
			unsafe
			{
				float* idxPtr = (float*)data.ToPointer();

				for ( short i = 0; i < 8; i++ )
				{
					idxPtr[index++] = borderUV[i].u1; idxPtr[index++] = borderUV[i].v1;
					idxPtr[index++] = borderUV[i].u1; idxPtr[index++] = borderUV[i].v2;
					idxPtr[index++] = borderUV[i].u2; idxPtr[index++] = borderUV[i].v1;
					idxPtr[index++] = borderUV[i].u2; idxPtr[index++] = borderUV[i].v2;

				}
			}

			vbuf.Unlock();
			
		}


		public void SetLeftBorderUV(Real u1, Real v1, Real u2, Real v2)
		{
			borderUV[(int)BorderCell.Left].u1 = u1; 
			borderUV[(int)BorderCell.Left].u2 = u2; 
			borderUV[(int)BorderCell.Left].v1 = v1; 
			borderUV[(int)BorderCell.Left].v2 = v2; 
			isGeomUVsOutOfDate = true;
		}
		//---------------------------------------------------------------------
		public void SetRightBorderUV(Real u1, Real v1, Real u2, Real v2)
		{
			borderUV[(int)BorderCell.Right].u1 = u1; 
			borderUV[(int)BorderCell.Right].u2 = u2; 
			borderUV[(int)BorderCell.Right].v1 = v1; 
			borderUV[(int)BorderCell.Right].v2 = v2; 
			isGeomUVsOutOfDate = true;
		}
		//---------------------------------------------------------------------
		public void SetTopBorderUV(Real u1, Real v1, Real u2, Real v2)
		{
			borderUV[(int)BorderCell.Top].u1 = u1; 
			borderUV[(int)BorderCell.Top].u2 = u2; 
			borderUV[(int)BorderCell.Top].v1 = v1; 
			borderUV[(int)BorderCell.Top].v2 = v2; 
			isGeomUVsOutOfDate = true;
		}
		//---------------------------------------------------------------------
		public void SetBottomBorderUV(Real u1, Real v1, Real u2, Real v2)
		{
			borderUV[(int)BorderCell.Bottom].u1 = u1; 
			borderUV[(int)BorderCell.Bottom].u2 = u2; 
			borderUV[(int)BorderCell.Bottom].v1 = v1; 
			borderUV[(int)BorderCell.Bottom].v2 = v2; 
			isGeomUVsOutOfDate = true;
		}
		//---------------------------------------------------------------------
		public void SetTopLeftBorderUV(Real u1, Real v1, Real u2, Real v2)
		{
			borderUV[(int)BorderCell.TopLeft].u1 = u1; 
			borderUV[(int)BorderCell.TopLeft].u2 = u2; 
			borderUV[(int)BorderCell.TopLeft].v1 = v1; 
			borderUV[(int)BorderCell.TopLeft].v2 = v2; 
			isGeomUVsOutOfDate = true;
		}
		//---------------------------------------------------------------------
		public void SetTopRightBorderUV(Real u1, Real v1, Real u2, Real v2)
		{
			borderUV[(int)BorderCell.TopRight].u1 = u1; 
			borderUV[(int)BorderCell.TopRight].u2 = u2; 
			borderUV[(int)BorderCell.TopRight].v1 = v1; 
			borderUV[(int)BorderCell.TopRight].v2 = v2; 
			isGeomUVsOutOfDate = true;
		}
		//---------------------------------------------------------------------
		public void SetBottomLeftBorderUV(Real u1, Real v1, Real u2, Real v2)
		{
			borderUV[(int)BorderCell.BottomLeft].u1 = u1; 
			borderUV[(int)BorderCell.BottomLeft].u2 = u2; 
			borderUV[(int)BorderCell.BottomLeft].v1 = v1; 
			borderUV[(int)BorderCell.BottomLeft].v2 = v2; 
			isGeomUVsOutOfDate = true;
		}
		//---------------------------------------------------------------------
		public void SetBottomRightBorderUV(Real u1, Real v1, Real u2, Real v2)
		{
			borderUV[(int)BorderCell.BottomRight].u1 = u1; 
			borderUV[(int)BorderCell.BottomRight].u2 = u2; 
			borderUV[(int)BorderCell.BottomRight].v1 = v1; 
			borderUV[(int)BorderCell.BottomRight].v2 = v2; 
			isGeomUVsOutOfDate = true;
		}

        /// <summary>
        ///    Override from Panel.
        /// </summary>
        public override void Initialize()
        {
			bool init = !isInitialised;
            base.Initialize();

			// superclass will handle the interior panel area 
			if (init)
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
					BufferUsage.StaticWriteOnly,true );

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
						idxPtr[index++] = val;
						idxPtr[index++] = (short)( val + 1 );
						idxPtr[index++] = (short)( val + 2 );

						idxPtr[index++] = (short)( val + 2 );
						idxPtr[index++] = (short)( val + 1 );
						idxPtr[index++] = (short)( val + 3 );
					}
				}

				// unlock the buffer
				renderOp2.indexData.indexBuffer.Unlock();

				// create new seperate object for the panels since they have a different material
				borderRenderable = new BorderRenderable( this );
				isInitialised = true;
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
        public void SetBorderSize( Real size )
        {
            if ( metricsMode != MetricsMode.Relative )
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
        public void SetBorderSize( Real sides, Real topAndBottom )
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
        public void SetBorderSize( Real left, Real right, Real top, Real bottom )
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
        public void SetCellUV( BorderCell cell, Real u1, Real v1, Real u2, Real v2 )
        {
            int cellIndex = (int)cell;

            // no choice but to lock/unlock each time here, locking only what we want to modify
            HardwareVertexBuffer buffer =
                renderOp2.vertexData.vertexBufferBinding.GetBuffer( TEXCOORDS );

            // can't use discard, or it will discard the whole buffer, wiping out the positions too
            IntPtr data = buffer.Lock(
                cellIndex * 8 * Marshal.SizeOf( typeof( Real ) ),
                Marshal.SizeOf( typeof( Real ) ) * 8,
                BufferLocking.Normal );

            int index = 0;

            unsafe
            {
                float* texPtr = (float*)data.ToPointer();

                texPtr[index++] = u1;
                texPtr[index++] = v1;
                texPtr[index++] = u1;
                texPtr[index++] = v2;
                texPtr[index++] = u2;
                texPtr[index++] = v1;
                texPtr[index++] = u2;
                texPtr[index++] = v2;
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
            lefts[0] = lefts[3] = lefts[5] = this.DerivedLeft * 2 - 1;
            lefts[1] = lefts[6] = rights[0] = rights[3] = rights[5] = lefts[0] + ( leftBorderSize * 2 );
            rights[2] = rights[4] = rights[7] = lefts[0] + ( width * 2 );
            lefts[2] = lefts[4] = lefts[7] = rights[1] = rights[6] = rights[2] - ( rightBorderSize * 2 );
            // Vertical
            tops[0] = tops[1] = tops[2] = -( ( this.DerivedTop * 2 ) - 1 );
            tops[3] = tops[4] = bottoms[0] = bottoms[1] = bottoms[2] = tops[0] - ( topBorderSize * 2 );
            bottoms[5] = bottoms[6] = bottoms[7] = tops[0] - ( height * 2 );
            tops[5] = tops[6] = tops[7] = bottoms[3] = bottoms[4] = bottoms[5] + ( bottomBorderSize * 2 );

            // get a reference to the buffer
            HardwareVertexBuffer buffer =
                renderOp2.vertexData.vertexBufferBinding.GetBuffer( POSITION );

            // lock this bad boy
            IntPtr data = buffer.Lock( BufferLocking.Discard );
            int index = 0;

			//Real zValue = Root.Instance.RenderSystem.MaximumDepthInputValue;
			Real zValue = -1;
            unsafe
            {
                float* posPtr = (float*)data.ToPointer();
                for ( int cell = 0; cell < 8; cell++ )
                {
                    posPtr[index++] = lefts[cell];
                    posPtr[index++] = tops[cell];
                    posPtr[index++] = zValue;

                    posPtr[index++] = lefts[cell];
                    posPtr[index++] = bottoms[cell];
                    posPtr[index++] = zValue;

                    posPtr[index++] = rights[cell];
                    posPtr[index++] = tops[cell];
                    posPtr[index++] = zValue;

                    posPtr[index++] = rights[cell];
                    posPtr[index++] = bottoms[cell];
                    posPtr[index++] = zValue;
                } // for
            } // unsafe

            // unlock the position buffer
            buffer.Unlock();

            // Also update center geometry
            // don't use base class because we need to make it smaller because of border
            buffer = renderOp.vertexData.vertexBufferBinding.GetBuffer( POSITION );
            data = buffer.Lock( BufferLocking.Discard );

            index = 0;

            unsafe
            {
                float* posPtr = (float*)data.ToPointer();

                posPtr[index++] = lefts[1];
                posPtr[index++] = tops[3];
                posPtr[index++] = zValue;

                posPtr[index++] = lefts[1];
                posPtr[index++] = bottoms[3];
                posPtr[index++] = zValue;

                posPtr[index++] = rights[1];
                posPtr[index++] = tops[3];
                posPtr[index++] = zValue;

                posPtr[index++] = rights[1];
                posPtr[index++] = bottoms[3];
                posPtr[index++] = zValue;
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


        #endregion

        #region Properties

        /// <summary>
        ///    Gets the size of the left border.
        /// </summary>
        public Real LeftBorderSize
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
        public Real RightBorderSize
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
        public Real TopBorderSize
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
        public Real BottomBorderSize
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
                borderMaterial = MaterialManager.Instance.GetByName( borderMaterialName );

                if ( borderMaterial == null )
                {
                    throw new Exception( string.Format( "Could not find material '{0}'.", borderMaterialName ) );
                }
                borderMaterial.Load();
				// Set some prerequisites to be sure
				borderMaterial.Lighting=(false);
				borderMaterial.DepthCheck=(false);
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

        #region Script parser methods

        [AttributeParser( "border_left_uv", "BorderPanel" )]
        public static void ParserLeftBorderUV( string[] parms, params object[] objects )
        {
            BorderPanel borderPanel = (BorderPanel)objects[0];

            Real u1 = Real.Parse( parms[0] );
            Real v1 = Real.Parse( parms[1] );
            Real u2 = Real.Parse( parms[2] );
            Real v2 = Real.Parse( parms[3] );

            //borderPanel.SetCellUV( BorderCell.Left, u1, v1, u2, v2 );
			borderPanel.SetLeftBorderUV(u1, v1, u2, v2);
        }

        [AttributeParser( "border_right_uv", "BorderPanel" )]
        public static void ParserRightBorderUV( string[] parms, params object[] objects )
        {
            BorderPanel borderPanel = (BorderPanel)objects[0];

            Real u1 = Real.Parse( parms[0] );
            Real v1 = Real.Parse( parms[1] );
            Real u2 = Real.Parse( parms[2] );
            Real v2 = Real.Parse( parms[3] );

            //borderPanel.SetCellUV( BorderCell.Right, u1, v1, u2, v2 );
			borderPanel.SetRightBorderUV(u1, v1, u2, v2);
        }

        [AttributeParser( "border_top_uv", "BorderPanel" )]
        public static void ParserTopBorderUV( string[] parms, params object[] objects )
        {
            BorderPanel borderPanel = (BorderPanel)objects[0];

            Real u1 = Real.Parse( parms[0] );
            Real v1 = Real.Parse( parms[1] );
            Real u2 = Real.Parse( parms[2] );
            Real v2 = Real.Parse( parms[3] );

            //borderPanel.SetCellUV( BorderCell.Top, u1, v1, u2, v2 );
			borderPanel.SetTopBorderUV(u1, v1, u2, v2);
        }

        [AttributeParser( "border_bottom_uv", "BorderPanel" )]
        public static void ParserBottomBorderUV( string[] parms, params object[] objects )
        {
            BorderPanel borderPanel = (BorderPanel)objects[0];

            Real u1 = Real.Parse( parms[0] );
            Real v1 = Real.Parse( parms[1] );
            Real u2 = Real.Parse( parms[2] );
            Real v2 = Real.Parse( parms[3] );

            //borderPanel.SetCellUV( BorderCell.Bottom, u1, v1, u2, v2 );
			borderPanel.SetBottomBorderUV(u1, v1, u2, v2);
        }

        [AttributeParser( "border_topleft_uv", "BorderPanel" )]
        public static void ParserTopLeftBorderUV( string[] parms, params object[] objects )
        {
            BorderPanel borderPanel = (BorderPanel)objects[0];

            Real u1 = Real.Parse( parms[0] );
            Real v1 = Real.Parse( parms[1] );
            Real u2 = Real.Parse( parms[2] );
            Real v2 = Real.Parse( parms[3] );

            //borderPanel.SetCellUV( BorderCell.TopLeft, u1, v1, u2, v2 );
			borderPanel.SetTopLeftBorderUV(u1, v1, u2, v2);
        }

        [AttributeParser( "border_topright_uv", "BorderPanel" )]
        public static void ParserTopRightBorderUV( string[] parms, params object[] objects )
        {
            BorderPanel borderPanel = (BorderPanel)objects[0];

            Real u1 = Real.Parse( parms[0] );
            Real v1 = Real.Parse( parms[1] );
            Real u2 = Real.Parse( parms[2] );
            Real v2 = Real.Parse( parms[3] );

            //borderPanel.SetCellUV( BorderCell.TopRight, u1, v1, u2, v2 );
			borderPanel.SetTopRightBorderUV(u1, v1, u2, v2);
        }

        [AttributeParser( "border_bottomleft_uv", "BorderPanel" )]
        public static void ParserBottomLeftBorderUV( string[] parms, params object[] objects )
        {
            BorderPanel borderPanel = (BorderPanel)objects[0];

            Real u1 = Real.Parse( parms[0] );
            Real v1 = Real.Parse( parms[1] );
            Real u2 = Real.Parse( parms[2] );
            Real v2 = Real.Parse( parms[3] );

            //borderPanel.SetCellUV( BorderCell.BottomLeft, u1, v1, u2, v2 );
			borderPanel.SetBottomLeftBorderUV(u1, v1, u2, v2);
        }

        [AttributeParser( "border_bottomright_uv", "BorderPanel" )]
        public static void ParserBottomRightBorderUV( string[] parms, params object[] objects )
        {
            BorderPanel borderPanel = (BorderPanel)objects[0];

            Real u1 = Real.Parse( parms[0] );
            Real v1 = Real.Parse( parms[1] );
            Real u2 = Real.Parse( parms[2] );
            Real v2 = Real.Parse( parms[3] );

            //borderPanel.SetCellUV( BorderCell.BottomRight, u1, v1, u2, v2 );
			borderPanel.SetBottomRightBorderUV(u1, v1, u2, v2);
        }

        [AttributeParser( "border_size", "BorderPanel" )]
        public static void ParserBorderSize( string[] parms, params object[] objects )
        {
            BorderPanel borderPanel = (BorderPanel)objects[0];

            Real left = Real.Parse( parms[0] );
            Real right = Real.Parse( parms[1] );
            Real top = Real.Parse( parms[2] );
            Real bottom = Real.Parse( parms[3] );

            borderPanel.SetBorderSize( left, right, top, bottom );
        }

        [AttributeParser( "border_material", "BorderPanel" )]
        public static void ParserBorderMaterial( string[] parms, params object[] objects )
        {
            BorderPanel borderPanel = (BorderPanel)objects[0];

            borderPanel.BorderMaterialName = parms[0];
        }

        #endregion Script parser methods

        #endregion
		public class CellUV 
		{
			public Real u1, v1, u2, v2;
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

            protected Hashtable customParams = new Hashtable();

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

            public Real GetSquaredViewDepth( Camera camera )
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

            public void GetRenderOperation( RenderOperation op )
            {
                op.vertexData = parent.renderOp2.vertexData;
                op.useIndices = parent.renderOp2.useIndices;
                op.indexData = parent.renderOp2.indexData;
                op.operationType = parent.renderOp2.operationType;
            }

            public void GetWorldTransforms( Matrix4[] matrices )
            {
                parent.GetWorldTransforms( matrices );
            }

            public SceneDetailLevel RenderDetail
            {
                get
                {
                    return SceneDetailLevel.Solid;
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
                if ( customParams[index] == null )
                {
                    throw new Exception( "A parameter was not found at the given index" );
                }
                else
                {
                    return (Vector4)customParams[index];
                }
            }

            public void SetCustomParameter( int index, Vector4 val )
            {
                customParams[index] = val;
            }

            public void UpdateCustomGpuParameter( GpuProgramParameters.AutoConstantEntry entry, GpuProgramParameters gpuParams )
            {
                if ( customParams[entry.data] != null )
                {
                    gpuParams.SetConstant( entry.index, (Vector4)customParams[entry.data] );
                }
            }

            #endregion
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
