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
        private const int POSITION = 0;
        private const int TEXCOORDS = 1;

        // temp array for use during position updates, prevents constant memory allocation
        private readonly float[] lefts = new float[8];
        private readonly float[] rights = new float[8];
        private readonly float[] tops = new float[8];
        private readonly float[] bottoms = new float[8];

        #endregion Member variables

        #region Constructors

        /// <summary>
        ///    Internal constructor, used when objects create by the factory.
        /// </summary>
        /// <param name="name"></param>
        internal BorderPanel(string name)
            : base(name)
        {
            for (var x = 0; x < 8; x++)
            {
                this.borderUV[x] = new CellUV();
            }
        }

        #endregion Constructors

        #region Methods

        protected override void dispose(bool disposeManagedResources)
        {
            if (!IsDisposed)
            {
                if (disposeManagedResources)
                {
                    this.renderOp2.SafeDispose();
                    this.renderOp2 = null;
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

            var vbuf = this.renderOp2.vertexData.vertexBufferBinding.GetBuffer(BorderPanel.TEXCOORDS);
            // Can't use discard since this discards whole buffer
            var data = vbuf.Lock(BufferLocking.Discard);
            var index = 0;
#if !AXIOM_SAFE_ONLY
            unsafe
#endif
            {
                var idxPtr = data.ToFloatPointer();

                for (short i = 0; i < 8; i++)
                {
                    idxPtr[index++] = this.borderUV[i].u1;
                    idxPtr[index++] = this.borderUV[i].v1;
                    idxPtr[index++] = this.borderUV[i].u1;
                    idxPtr[index++] = this.borderUV[i].v2;
                    idxPtr[index++] = this.borderUV[i].u2;
                    idxPtr[index++] = this.borderUV[i].v1;
                    idxPtr[index++] = this.borderUV[i].u2;
                    idxPtr[index++] = this.borderUV[i].v2;
                }
            }

            vbuf.Unlock();
        }

        public void GetLeftBorderUV(out float u1, out float v1, out float u2, out float v2)
        {
            u1 = this.borderUV[(int)BorderCell.Left].u1;
            u2 = this.borderUV[(int)BorderCell.Left].u2;
            v1 = this.borderUV[(int)BorderCell.Left].v1;
            v2 = this.borderUV[(int)BorderCell.Left].v2;
        }

        public void SetLeftBorderUV(float u1, float v1, float u2, float v2)
        {
            this.borderUV[(int)BorderCell.Left].u1 = u1;
            this.borderUV[(int)BorderCell.Left].u2 = u2;
            this.borderUV[(int)BorderCell.Left].v1 = v1;
            this.borderUV[(int)BorderCell.Left].v2 = v2;
            isGeomUVsOutOfDate = true;
        }

        //---------------------------------------------------------------------
        public void GetRightBorderUV(out float u1, out float v1, out float u2, out float v2)
        {
            u1 = this.borderUV[(int)BorderCell.Right].u1;
            u2 = this.borderUV[(int)BorderCell.Right].u2;
            v1 = this.borderUV[(int)BorderCell.Right].v1;
            v2 = this.borderUV[(int)BorderCell.Right].v2;
        }

        public void SetRightBorderUV(float u1, float v1, float u2, float v2)
        {
            this.borderUV[(int)BorderCell.Right].u1 = u1;
            this.borderUV[(int)BorderCell.Right].u2 = u2;
            this.borderUV[(int)BorderCell.Right].v1 = v1;
            this.borderUV[(int)BorderCell.Right].v2 = v2;
            isGeomUVsOutOfDate = true;
        }

        //---------------------------------------------------------------------
        public void GetTopBorderUV(out float u1, out float v1, out float u2, out float v2)
        {
            u1 = this.borderUV[(int)BorderCell.Top].u1;
            u2 = this.borderUV[(int)BorderCell.Top].u2;
            v1 = this.borderUV[(int)BorderCell.Top].v1;
            v2 = this.borderUV[(int)BorderCell.Top].v2;
        }

        public void SetTopBorderUV(float u1, float v1, float u2, float v2)
        {
            this.borderUV[(int)BorderCell.Top].u1 = u1;
            this.borderUV[(int)BorderCell.Top].u2 = u2;
            this.borderUV[(int)BorderCell.Top].v1 = v1;
            this.borderUV[(int)BorderCell.Top].v2 = v2;
            isGeomUVsOutOfDate = true;
        }

        //---------------------------------------------------------------------
        public void GetBottomBorderUV(out float u1, out float v1, out float u2, out float v2)
        {
            u1 = this.borderUV[(int)BorderCell.Bottom].u1;
            u2 = this.borderUV[(int)BorderCell.Bottom].u2;
            v1 = this.borderUV[(int)BorderCell.Bottom].v1;
            v2 = this.borderUV[(int)BorderCell.Bottom].v2;
        }

        public void SetBottomBorderUV(float u1, float v1, float u2, float v2)
        {
            this.borderUV[(int)BorderCell.Bottom].u1 = u1;
            this.borderUV[(int)BorderCell.Bottom].u2 = u2;
            this.borderUV[(int)BorderCell.Bottom].v1 = v1;
            this.borderUV[(int)BorderCell.Bottom].v2 = v2;
            isGeomUVsOutOfDate = true;
        }

        //---------------------------------------------------------------------
        public void GetTopLeftBorderUV(out float u1, out float v1, out float u2, out float v2)
        {
            u1 = this.borderUV[(int)BorderCell.TopLeft].u1;
            u2 = this.borderUV[(int)BorderCell.TopLeft].u2;
            v1 = this.borderUV[(int)BorderCell.TopLeft].v1;
            v2 = this.borderUV[(int)BorderCell.TopLeft].v2;
        }

        public void SetTopLeftBorderUV(float u1, float v1, float u2, float v2)
        {
            this.borderUV[(int)BorderCell.TopLeft].u1 = u1;
            this.borderUV[(int)BorderCell.TopLeft].u2 = u2;
            this.borderUV[(int)BorderCell.TopLeft].v1 = v1;
            this.borderUV[(int)BorderCell.TopLeft].v2 = v2;
            isGeomUVsOutOfDate = true;
        }

        //---------------------------------------------------------------------
        public void GetTopRightBorderUV(out float u1, out float v1, out float u2, out float v2)
        {
            u1 = this.borderUV[(int)BorderCell.TopRight].u1;
            u2 = this.borderUV[(int)BorderCell.TopRight].u2;
            v1 = this.borderUV[(int)BorderCell.TopRight].v1;
            v2 = this.borderUV[(int)BorderCell.TopRight].v2;
        }

        public void SetTopRightBorderUV(float u1, float v1, float u2, float v2)
        {
            this.borderUV[(int)BorderCell.TopRight].u1 = u1;
            this.borderUV[(int)BorderCell.TopRight].u2 = u2;
            this.borderUV[(int)BorderCell.TopRight].v1 = v1;
            this.borderUV[(int)BorderCell.TopRight].v2 = v2;
            isGeomUVsOutOfDate = true;
        }

        //---------------------------------------------------------------------
        public void GetBottomLeftBorderUV(out float u1, out float v1, out float u2, out float v2)
        {
            u1 = this.borderUV[(int)BorderCell.BottomLeft].u1;
            u2 = this.borderUV[(int)BorderCell.BottomLeft].u2;
            v1 = this.borderUV[(int)BorderCell.BottomLeft].v1;
            v2 = this.borderUV[(int)BorderCell.BottomLeft].v2;
        }

        public void SetBottomLeftBorderUV(float u1, float v1, float u2, float v2)
        {
            this.borderUV[(int)BorderCell.BottomLeft].u1 = u1;
            this.borderUV[(int)BorderCell.BottomLeft].u2 = u2;
            this.borderUV[(int)BorderCell.BottomLeft].v1 = v1;
            this.borderUV[(int)BorderCell.BottomLeft].v2 = v2;
            isGeomUVsOutOfDate = true;
        }

        //---------------------------------------------------------------------
        public void GetBottomRightBorderUV(out float u1, out float v1, out float u2, out float v2)
        {
            u1 = this.borderUV[(int)BorderCell.BottomRight].u1;
            u2 = this.borderUV[(int)BorderCell.BottomRight].u2;
            v1 = this.borderUV[(int)BorderCell.BottomRight].v1;
            v2 = this.borderUV[(int)BorderCell.BottomRight].v2;
        }

        public void SetBottomRightBorderUV(float u1, float v1, float u2, float v2)
        {
            this.borderUV[(int)BorderCell.BottomRight].u1 = u1;
            this.borderUV[(int)BorderCell.BottomRight].u2 = u2;
            this.borderUV[(int)BorderCell.BottomRight].v1 = v1;
            this.borderUV[(int)BorderCell.BottomRight].v2 = v2;
            isGeomUVsOutOfDate = true;
        }

        /// <summary>
        ///    Override from Panel.
        /// </summary>
        public override void Initialize()
        {
            var init = !isInitialized;
            base.Initialize();

            // superclass will handle the interior panel area
            if (init)
            {
                // base class already has added the center panel at this point, so lets create the borders
                this.renderOp2.vertexData = new VertexData();
                // 8 * 4, cant resuse vertices because they might not share same tex coords
                this.renderOp2.vertexData.vertexCount = 32;
                this.renderOp2.vertexData.vertexStart = 0;

                // get a reference to the vertex declaration
                var decl = this.renderOp2.vertexData.vertexDeclaration;
                // Position and texture coords each have their own buffers to allow
                // each to be edited separately with the discard flag
                decl.AddElement(POSITION, 0, VertexElementType.Float3, VertexElementSemantic.Position);
                decl.AddElement(TEXCOORDS, 0, VertexElementType.Float2, VertexElementSemantic.TexCoords, 0);

                // position buffer
                var buffer = HardwareBufferManager.Instance.CreateVertexBuffer(decl.Clone(POSITION),
                                                                                this.renderOp2.vertexData.vertexCount,
                                                                                BufferUsage.StaticWriteOnly);

                // bind position
                var binding = this.renderOp2.vertexData.vertexBufferBinding;
                binding.SetBinding(POSITION, buffer);

                // texcoord buffer
                buffer = HardwareBufferManager.Instance.CreateVertexBuffer(decl.Clone(TEXCOORDS),
                                                                            this.renderOp2.vertexData.vertexCount,
                                                                            BufferUsage.StaticWriteOnly, true);

                // bind texcoords
                binding = this.renderOp2.vertexData.vertexBufferBinding;
                binding.SetBinding(TEXCOORDS, buffer);

                this.renderOp2.operationType = OperationType.TriangleList;
                this.renderOp2.useIndices = true;

                // index data
                this.renderOp2.indexData = new IndexData();
                // 8 * 3 * 2 = 8 vertices, 3 indices per tri, 2 tris
                this.renderOp2.indexData.indexCount = 48;
                this.renderOp2.indexData.indexStart = 0;

                /* Each cell is
					0-----2
					|    /|
					|  /  |
					|/    |
					1-----3
				*/

                // create a new index buffer
                this.renderOp2.indexData.indexBuffer = HardwareBufferManager.Instance.CreateIndexBuffer(IndexType.Size16,
                                                                                                         this.renderOp2.indexData.
                                                                                                             indexCount,
                                                                                                         BufferUsage.StaticWriteOnly);

                // lock this bad boy
                var data = this.renderOp2.indexData.indexBuffer.Lock(BufferLocking.Discard);
                var index = 0;
#if !AXIOM_SAFE_ONLY
                unsafe
#endif
                {
                    var idxPtr = data.ToShortPointer();

                    for (short cell = 0; cell < 8; cell++)
                    {
                        var val = (short)(cell * 4);
                        idxPtr[index++] = val;
                        idxPtr[index++] = (short)(val + 1);
                        idxPtr[index++] = (short)(val + 2);

                        idxPtr[index++] = (short)(val + 2);
                        idxPtr[index++] = (short)(val + 1);
                        idxPtr[index++] = (short)(val + 3);
                    }
                }

                // unlock the buffer
                this.renderOp2.indexData.indexBuffer.Unlock();

                // create new seperate object for the panels since they have a different material
                this.borderRenderable = new BorderRenderable(this);
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
        public void SetBorderSize(float size)
        {
            if (metricsMode != MetricsMode.Pixels)
            {
                this.pixelTopBorderSize =
                    this.pixelRightBorderSize = this.pixelLeftBorderSize = this.pixelBottomBorderSize = (short)size;
            }
            else
            {
                this.topBorderSize = this.rightBorderSize = this.leftBorderSize = this.bottomBorderSize = size;
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
        public void SetBorderSize(float sides, float topAndBottom)
        {
            if (metricsMode != MetricsMode.Relative)
            {
                this.pixelRightBorderSize = this.pixelLeftBorderSize = (short)sides;
                this.pixelTopBorderSize = this.pixelBottomBorderSize = (short)topAndBottom;
            }
            else
            {
                this.topBorderSize = this.bottomBorderSize = topAndBottom;
                this.rightBorderSize = this.leftBorderSize = sides;
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
        public void SetBorderSize(float left, float right, float top, float bottom)
        {
            if (metricsMode != MetricsMode.Relative)
            {
                this.pixelTopBorderSize = (short)top;
                this.pixelBottomBorderSize = (short)bottom;
                this.pixelRightBorderSize = (short)right;
                this.pixelLeftBorderSize = (short)left;
            }
            else
            {
                this.topBorderSize = top;
                this.bottomBorderSize = bottom;
                this.rightBorderSize = right;
                this.leftBorderSize = left;
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
        public void SetCellUV(BorderCell cell, float u1, float v1, float u2, float v2)
        {
            var cellIndex = (int)cell;

            // no choice but to lock/unlock each time here, locking only what we want to modify
            var buffer = this.renderOp2.vertexData.vertexBufferBinding.GetBuffer(TEXCOORDS);

            // can't use discard, or it will discard the whole buffer, wiping out the positions too
            var data = buffer.Lock(cellIndex * 8 * Memory.SizeOf(typeof(float)), Memory.SizeOf(typeof(float)) * 8,
                                    BufferLocking.Normal);

            var index = 0;

#if !AXIOM_SAFE_ONLY
            unsafe
#endif
            {
                var texPtr = data.ToFloatPointer();

                texPtr[index++] = u1;
                texPtr[index++] = v1;
                texPtr[index++] = u1;
                texPtr[index++] = v2;
                texPtr[index++] = u2;
                texPtr[index++] = v1;
                texPtr[index++] = u2;
                texPtr[index] = v2;
            }

            buffer.Unlock();
        }

        /// <summary>
        ///    Overriden from Panel.
        /// </summary>
        public override void Update()
        {
            if (metricsMode != MetricsMode.Relative &&
                 (OverlayManager.Instance.HasViewportChanged || isGeomPositionsOutOfDate))
            {
                this.leftBorderSize = this.pixelLeftBorderSize * pixelScaleX;
                this.rightBorderSize = this.pixelRightBorderSize * pixelScaleX;
                this.topBorderSize = this.pixelTopBorderSize * pixelScaleY;
                this.bottomBorderSize = this.pixelBottomBorderSize * pixelScaleY;
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
            this.lefts[0] = this.lefts[3] = this.lefts[5] = DerivedLeft * 2 - 1;
            this.lefts[1] =
                this.lefts[6] =
                this.rights[0] = this.rights[3] = this.rights[5] = this.lefts[0] + (this.leftBorderSize * 2);
            this.rights[2] = this.rights[4] = this.rights[7] = this.lefts[0] + (width * 2);
            this.lefts[2] =
                this.lefts[4] =
                this.lefts[7] = this.rights[1] = this.rights[6] = this.rights[2] - (this.rightBorderSize * 2);
            // Vertical
            this.tops[0] = this.tops[1] = this.tops[2] = -((DerivedTop * 2) - 1);
            this.tops[3] =
                this.tops[4] =
                this.bottoms[0] = this.bottoms[1] = this.bottoms[2] = this.tops[0] - (this.topBorderSize * 2);
            this.bottoms[5] = this.bottoms[6] = this.bottoms[7] = this.tops[0] - (height * 2);
            this.tops[5] =
                this.tops[6] =
                this.tops[7] = this.bottoms[3] = this.bottoms[4] = this.bottoms[5] + (this.bottomBorderSize * 2);

            // get a reference to the buffer
            var buffer = this.renderOp2.vertexData.vertexBufferBinding.GetBuffer(POSITION);

            // lock this bad boy
            var data = buffer.Lock(BufferLocking.Discard);
            var index = 0;

            //float zValue = Root.Instance.RenderSystem.MaximumDepthInputValue;
            //float zValue = -1;
#if !AXIOM_SAFE_ONLY
            unsafe
#endif
            {
                var posPtr = data.ToFloatPointer();
                for (var cell = 0; cell < 8; cell++)
                {
                    posPtr[index++] = this.lefts[cell];
                    posPtr[index++] = this.tops[cell];
                    posPtr[index++] = -1;

                    posPtr[index++] = this.lefts[cell];
                    posPtr[index++] = this.bottoms[cell];
                    posPtr[index++] = -1;

                    posPtr[index++] = this.rights[cell];
                    posPtr[index++] = this.tops[cell];
                    posPtr[index++] = -1;

                    posPtr[index++] = this.rights[cell];
                    posPtr[index++] = this.bottoms[cell];
                    posPtr[index++] = -1;
                } // for
            } // unsafe

            // unlock the position buffer
            buffer.Unlock();

            // Also update center geometry
            // don't use base class because we need to make it smaller because of border
            buffer = renderOperation.vertexData.vertexBufferBinding.GetBuffer(POSITION);
            data = buffer.Lock(BufferLocking.Discard);

            index = 0;

#if !AXIOM_SAFE_ONLY
            unsafe
#endif
            {
                var posPtr = data.ToFloatPointer();

                posPtr[index++] = this.lefts[1];
                posPtr[index++] = this.tops[3];
                posPtr[index++] = -1;

                posPtr[index++] = this.lefts[1];
                posPtr[index++] = this.bottoms[3];
                posPtr[index++] = -1;

                posPtr[index++] = this.rights[1];
                posPtr[index++] = this.tops[3];
                posPtr[index++] = -1;

                posPtr[index++] = this.rights[1];
                posPtr[index++] = this.bottoms[3];
                posPtr[index] = -1;
            }

            // unlock the buffer to finish
            buffer.Unlock();
        }

        /// <summary>
        ///    Overriden from Panel.
        /// </summary>
        /// <param name="queue"></param>
        public override void UpdateRenderQueue(RenderQueue queue)
        {
            // Add self twice to the queue
            // Have to do this to allow 2 materials
            if (isVisible)
            {
                // add border first
                queue.AddRenderable(this.borderRenderable, (ushort)zOrder, RenderQueueGroupID.Overlay);

                // do inner last so the border artifacts don't overwrite the children
                // Add inner
                base.UpdateRenderQueue(queue);
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
                if (metricsMode == MetricsMode.Pixels)
                {
                    return this.pixelLeftBorderSize;
                }
                else
                {
                    return this.leftBorderSize;
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
                if (metricsMode == MetricsMode.Pixels)
                {
                    return this.pixelRightBorderSize;
                }
                else
                {
                    return this.rightBorderSize;
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
                if (metricsMode == MetricsMode.Pixels)
                {
                    return this.pixelTopBorderSize;
                }
                else
                {
                    return this.topBorderSize;
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
                if (metricsMode == MetricsMode.Pixels)
                {
                    return this.pixelBottomBorderSize;
                }
                else
                {
                    return this.bottomBorderSize;
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
                return this.borderMaterialName;
            }
            set
            {
                this.borderMaterialName = value;
                this.borderMaterial = (Material)MaterialManager.Instance[this.borderMaterialName];

                if (this.borderMaterial == null)
                {
                    throw new Exception(string.Format("Could not find material '{0}'.", this.borderMaterialName));
                }
                this.borderMaterial.Load();
                // Set some prerequisites to be sure
                this.borderMaterial.Lighting = false;
                this.borderMaterial.DepthCheck = false;
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

                if (value != MetricsMode.Relative)
                {
                    this.pixelBottomBorderSize = (short)this.bottomBorderSize;
                    this.pixelLeftBorderSize = (short)this.leftBorderSize;
                    this.pixelRightBorderSize = (short)this.rightBorderSize;
                    this.pixelTopBorderSize = (short)this.topBorderSize;
                }
            }
        }

        #endregion Properties

        #region ScriptableObject Interface Command Classes

        [ScriptableProperty("border_size", "", typeof(BorderPanel))]
        public class BorderSizeAttributeCommand : IPropertyCommand
        {
            #region Implementation of IPropertyCommand<object,string>

            /// <summary>
            ///    Gets the value for this command from the target object.
            /// </summary>
            /// <param name="target"></param>
            /// <returns></returns>
            public string Get(object target)
            {
                var element = target as BorderPanel;
                if (element != null)
                {
                    return String.Format("{0} {1} {2} {3}", element.LeftBorderSize, element.RightBorderSize, element.TopBorderSize,
                                          element.BottomBorderSize);
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
            public void Set(object target, string val)
            {
                var element = target as BorderPanel;
                var parms = val.Split(' ');
                if (element != null)
                {
                    Real left = StringConverter.ParseFloat(parms[0]),
                         right = StringConverter.ParseFloat(parms[1]),
                         top = StringConverter.ParseFloat(parms[2]),
                         bottom = StringConverter.ParseFloat(parms[3]);
                    element.SetBorderSize(left, right, top, bottom);
                }
            }

            #endregion Implementation of IPropertyCommand<object,string>
        }

        [ScriptableProperty("border_material", "", typeof(BorderPanel))]
        public class BorderMaterialHeightAttributeCommand : IPropertyCommand
        {
            #region Implementation of IPropertyCommand<object,string>

            /// <summary>
            ///    Gets the value for this command from the target object.
            /// </summary>
            /// <param name="target"></param>
            /// <returns></returns>
            public string Get(object target)
            {
                var element = target as BorderPanel;
                if (element != null)
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
            public void Set(object target, string val)
            {
                var element = target as BorderPanel;
                if (element != null)
                {
                    element.BorderMaterialName = val;
                }
            }

            #endregion Implementation of IPropertyCommand<object,string>
        }

        [ScriptableProperty("border_topleft_uv", "", typeof(BorderPanel))]
        public class BorderTopLeftUVAttributeCommand : IPropertyCommand
        {
            #region Implementation of IPropertyCommand<object,string>

            /// <summary>
            ///    Gets the value for this command from the target object.
            /// </summary>
            /// <param name="target"></param>
            /// <returns></returns>
            public string Get(object target)
            {
                var element = target as BorderPanel;
                if (element != null)
                {
                    float u1, v1, u2, v2;
                    element.GetTopLeftBorderUV(out u1, out v1, out u2, out v2);
                    return String.Format("{0} {1} {2} {3}", u1, v1, u2, v2);
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
            public void Set(object target, string val)
            {
                var element = target as BorderPanel;
                if (element != null)
                {
                    var parms = val.Split(' ');
                    Real u1 = StringConverter.ParseFloat(parms[0]),
                         v1 = StringConverter.ParseFloat(parms[1]),
                         u2 = StringConverter.ParseFloat(parms[2]),
                         v2 = StringConverter.ParseFloat(parms[3]);

                    element.SetTopLeftBorderUV(u1, v1, u2, v2);
                }
            }

            #endregion Implementation of IPropertyCommand<object,string>
        }

        [ScriptableProperty("border_topright_uv", "", typeof(BorderPanel))]
        public class BorderTopRightUVAttributeCommand : IPropertyCommand
        {
            #region Implementation of IPropertyCommand<object,string>

            /// <summary>
            ///    Gets the value for this command from the target object.
            /// </summary>
            /// <param name="target"></param>
            /// <returns></returns>
            public string Get(object target)
            {
                var element = target as BorderPanel;
                if (element != null)
                {
                    float u1, v1, u2, v2;
                    element.GetTopRightBorderUV(out u1, out v1, out u2, out v2);
                    return String.Format("{0} {1} {2} {3}", u1, v1, u2, v2);
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
            public void Set(object target, string val)
            {
                var element = target as BorderPanel;
                if (element != null)
                {
                    var parms = val.Split(' ');
                    Real u1 = StringConverter.ParseFloat(parms[0]),
                         v1 = StringConverter.ParseFloat(parms[1]),
                         u2 = StringConverter.ParseFloat(parms[2]),
                         v2 = StringConverter.ParseFloat(parms[3]);

                    element.SetTopRightBorderUV(u1, v1, u2, v2);
                }
            }

            #endregion Implementation of IPropertyCommand<object,string>
        }

        [ScriptableProperty("border_bottomleft_uv", "", typeof(BorderPanel))]
        public class BorderBottomLeftUVAttributeCommand : IPropertyCommand
        {
            #region Implementation of IPropertyCommand<object,string>

            /// <summary>
            ///    Gets the value for this command from the target object.
            /// </summary>
            /// <param name="target"></param>
            /// <returns></returns>
            public string Get(object target)
            {
                var element = target as BorderPanel;
                if (element != null)
                {
                    float u1, v1, u2, v2;
                    element.GetBottomLeftBorderUV(out u1, out v1, out u2, out v2);
                    return String.Format("{0} {1} {2} {3}", u1, v1, u2, v2);
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
            public void Set(object target, string val)
            {
                var element = target as BorderPanel;
                if (element != null)
                {
                    var parms = val.Split(' ');
                    Real u1 = StringConverter.ParseFloat(parms[0]),
                         v1 = StringConverter.ParseFloat(parms[1]),
                         u2 = StringConverter.ParseFloat(parms[2]),
                         v2 = StringConverter.ParseFloat(parms[3]);

                    element.SetBottomLeftBorderUV(u1, v1, u2, v2);
                }
            }

            #endregion Implementation of IPropertyCommand<object,string>
        }

        [ScriptableProperty("border_bottomright_uv", "", typeof(BorderPanel))]
        public class BorderBottomRightUVAttributeCommand : IPropertyCommand
        {
            #region Implementation of IPropertyCommand<object,string>

            /// <summary>
            ///    Gets the value for this command from the target object.
            /// </summary>
            /// <param name="target"></param>
            /// <returns></returns>
            public string Get(object target)
            {
                var element = target as BorderPanel;
                if (element != null)
                {
                    float u1, v1, u2, v2;
                    element.GetBottomRightBorderUV(out u1, out v1, out u2, out v2);
                    return String.Format("{0} {1} {2} {3}", u1, v1, u2, v2);
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
            public void Set(object target, string val)
            {
                var element = target as BorderPanel;
                if (element != null)
                {
                    var parms = val.Split(' ');
                    Real u1 = StringConverter.ParseFloat(parms[0]),
                         v1 = StringConverter.ParseFloat(parms[1]),
                         u2 = StringConverter.ParseFloat(parms[2]),
                         v2 = StringConverter.ParseFloat(parms[3]);

                    element.SetBottomRightBorderUV(u1, v1, u2, v2);
                }
            }

            #endregion Implementation of IPropertyCommand<object,string>
        }

        [ScriptableProperty("border_left_uv", "", typeof(BorderPanel))]
        public class BorderLeftUVAttributeCommand : IPropertyCommand
        {
            #region Implementation of IPropertyCommand<object,string>

            /// <summary>
            ///    Gets the value for this command from the target object.
            /// </summary>
            /// <param name="target"></param>
            /// <returns></returns>
            public string Get(object target)
            {
                var element = target as BorderPanel;
                if (element != null)
                {
                    float u1, v1, u2, v2;
                    element.GetLeftBorderUV(out u1, out v1, out u2, out v2);
                    return String.Format("{0} {1} {2} {3}", u1, v1, u2, v2);
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
            public void Set(object target, string val)
            {
                var element = target as BorderPanel;
                if (element != null)
                {
                    var parms = val.Split(' ');
                    Real u1 = StringConverter.ParseFloat(parms[0]),
                         v1 = StringConverter.ParseFloat(parms[1]),
                         u2 = StringConverter.ParseFloat(parms[2]),
                         v2 = StringConverter.ParseFloat(parms[3]);

                    element.SetLeftBorderUV(u1, v1, u2, v2);
                }
            }

            #endregion Implementation of IPropertyCommand<object,string>
        }

        [ScriptableProperty("border_top_uv", "", typeof(BorderPanel))]
        public class BorderTopUVAttributeCommand : IPropertyCommand
        {
            #region Implementation of IPropertyCommand<object,string>

            /// <summary>
            ///    Gets the value for this command from the target object.
            /// </summary>
            /// <param name="target"></param>
            /// <returns></returns>
            public string Get(object target)
            {
                var element = target as BorderPanel;
                if (element != null)
                {
                    float u1, v1, u2, v2;
                    element.GetTopBorderUV(out u1, out v1, out u2, out v2);
                    return String.Format("{0} {1} {2} {3}", u1, v1, u2, v2);
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
            public void Set(object target, string val)
            {
                var element = target as BorderPanel;
                if (element != null)
                {
                    var parms = val.Split(' ');
                    Real u1 = StringConverter.ParseFloat(parms[0]),
                         v1 = StringConverter.ParseFloat(parms[1]),
                         u2 = StringConverter.ParseFloat(parms[2]),
                         v2 = StringConverter.ParseFloat(parms[3]);

                    element.SetTopBorderUV(u1, v1, u2, v2);
                }
            }

            #endregion Implementation of IPropertyCommand<object,string>
        }

        [ScriptableProperty("border_right_uv", "", typeof(BorderPanel))]
        public class BorderRightUVAttributeCommand : IPropertyCommand
        {
            #region Implementation of IPropertyCommand<object,string>

            /// <summary>
            ///    Gets the value for this command from the target object.
            /// </summary>
            /// <param name="target"></param>
            /// <returns></returns>
            public string Get(object target)
            {
                var element = target as BorderPanel;
                if (element != null)
                {
                    float u1, v1, u2, v2;
                    element.GetRightBorderUV(out u1, out v1, out u2, out v2);
                    return String.Format("{0} {1} {2} {3}", u1, v1, u2, v2);
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
            public void Set(object target, string val)
            {
                var element = target as BorderPanel;
                if (element != null)
                {
                    var parms = val.Split(' ');
                    Real u1 = StringConverter.ParseFloat(parms[0]),
                         v1 = StringConverter.ParseFloat(parms[1]),
                         u2 = StringConverter.ParseFloat(parms[2]),
                         v2 = StringConverter.ParseFloat(parms[3]);

                    element.SetRightBorderUV(u1, v1, u2, v2);
                }
            }

            #endregion Implementation of IPropertyCommand<object,string>
        }

        [ScriptableProperty("border_bottom_uv", "", typeof(BorderPanel))]
        public class BorderBottomUVAttributeCommand : IPropertyCommand
        {
            #region Implementation of IPropertyCommand<object,string>

            /// <summary>
            ///    Gets the value for this command from the target object.
            /// </summary>
            /// <param name="target"></param>
            /// <returns></returns>
            public string Get(object target)
            {
                var element = target as BorderPanel;
                if (element != null)
                {
                    float u1, v1, u2, v2;
                    element.GetBottomBorderUV(out u1, out v1, out u2, out v2);
                    return String.Format("{0} {1} {2} {3}", u1, v1, u2, v2);
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
            public void Set(object target, string val)
            {
                var element = target as BorderPanel;
                if (element != null)
                {
                    var parms = val.Split(' ');
                    Real u1 = StringConverter.ParseFloat(parms[0]),
                         v1 = StringConverter.ParseFloat(parms[1]),
                         u2 = StringConverter.ParseFloat(parms[2]),
                         v2 = StringConverter.ParseFloat(parms[3]);

                    element.SetBottomBorderUV(u1, v1, u2, v2);
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

            private readonly LightList emptyLightList = new LightList();

            protected List<Vector4> customParams = new List<Vector4>();

            #endregion Member variables

            #region Constructors

            /// <summary>
            ///
            /// </summary>
            /// <param name="parent"></param>
            public BorderRenderable(BorderPanel parent)
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

            public Real GetSquaredViewDepth(Camera camera)
            {
                return this.parent.GetSquaredViewDepth(camera);
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

            public void GetWorldTransforms(Matrix4[] matrices)
            {
                this.parent.GetWorldTransforms(matrices);
            }

            public virtual bool PolygonModeOverrideable
            {
                get
                {
                    return this.parent.PolygonModeOverrideable;
                }
            }

            public Material Material
            {
                get
                {
                    return this.parent.borderMaterial;
                }
            }

            public Technique Technique
            {
                get
                {
                    return Material.GetBestTechnique();
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
                    return this.emptyLightList;
                }
            }

            public Vector4 GetCustomParameter(int index)
            {
                if (this.customParams[index] == null)
                {
                    throw new Exception("A parameter was not found at the given index");
                }
                else
                {
                    return (Vector4)this.customParams[index];
                }
            }

            public void SetCustomParameter(int index, Vector4 val)
            {
                while (this.customParams.Count <= index)
                {
                    this.customParams.Add(Vector4.Zero);
                }
                this.customParams[index] = val;
            }

            public void UpdateCustomGpuParameter(GpuProgramParameters.AutoConstantEntry entry, GpuProgramParameters gpuParams)
            {
                if (this.customParams[entry.Data] != null)
                {
                    gpuParams.SetConstant(entry.PhysicalIndex, (Vector4)this.customParams[entry.Data]);
                }
            }

            #endregion IRenderable Members

            #region IDisposable Implementation

            #region isDisposed Property

            /// <summary>
            /// Determines if this instance has been disposed of already.
            /// </summary>
            protected bool isDisposed { get; set; }

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
            protected virtual void dispose(bool disposeManagedResources)
            {
                if (!isDisposed)
                {
                    if (disposeManagedResources)
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
                dispose(true);
                GC.SuppressFinalize(this);
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