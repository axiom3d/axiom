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

using System;
using System.Collections.Generic;
using Axiom.Core;
using Axiom.Math;
using Axiom.Serialization;

#endregion Namespace Declarations

namespace Axiom.Components.Paging
{
    /// <summary>
    /// 
    /// </summary>
    public class Grid2DPageStrategyData : IPageStrategyData
    {

        #region - constants -
        public static uint CHUNK_ID = StreamSerializer.MakeIdentifier("G2DD");
        public static ushort CHUNK_VERSION = 1;
        #endregion
        #region - fields -
        /// <summary>
        /// Orientation of the grid.
        /// </summary>
        protected Grid2Mode mMode;
        /// <summary>
        /// Origin (world space)
        /// </summary>
        protected Vector3 mWorldOrigin;
        /// <summary>
        /// Origin (grid-aligned world space)
        /// </summary>
        protected Vector2 mOrigin;
        /// <summary>
        /// Grid horizontal extent in cells
        /// </summary>
        protected int mGridExtentsHorz;
        /// <summary>
        /// Grid vertical extent in cells
        /// </summary>
        protected int mGridExtentsVert;
        /// <summary>
        /// Bottom-left position (grid-aligned world space)
        /// </summary>
        protected Vector2 mBottomLeft;
        /// <summary>
        /// Grid cell (page) size.
        /// </summary>
        protected float mCellSize;
        /// <summary>
        /// Load radius
        /// </summary>
        protected float mLoadRadius;
        /// <summary>
        /// hold radius.
        /// </summary>
        protected float mHoldRadius;
        /// <summary>
        /// 
        /// </summary>
        protected float mLoadRadiusInCells;
        /// <summary>
        /// 
        /// </summary>
        protected float mHoldRadiusInCells;
        #endregion

        #region - properties -
        /// <summary>
        ///  Orientation of the grid.
        /// </summary>
        public virtual Grid2Mode Mode
        {
            get { return mMode; }
            set
            {
                mMode = value;
                //reset origin
                Origin = mWorldOrigin;
            }
        }

        /// <summary>
        /// Origin (world space)
        /// </summary>
        public virtual Vector3 Origin
        {
            set 
            { 
                mWorldOrigin = value;
                ConvetWorldToGridSpace(mWorldOrigin, ref mOrigin);
                UpdateDerivedMetrics();
            }
            get { return mWorldOrigin; }
        }

        /// <summary>
        /// Grid cell (page) size.
        /// </summary>
        public virtual float CellSize
        {
            set 
            { 
                mCellSize = value;
                UpdateDerivedMetrics();
            }
            get { return mCellSize; }
        }
        #endregion

        /// <summary>
        /// Set or gets the number of cells in the grid horizontally (defaults to max of 65536)
        /// </summary>
        public virtual int CellCountHorz
        {
            set
            {
                mGridExtentsHorz = value;
                UpdateDerivedMetrics();
            }
            get { return mGridExtentsHorz; }
        }

        /// <summary>
        /// Set or gets the number of cells in the grid vertically (defaults to max of 65536)
        /// </summary>
        public virtual int CellCountVert
        {
            set 
            { 
                mGridExtentsVert = value;
                UpdateDerivedMetrics();
            }
            get { return mGridExtentsVert; }
        }

        /// <summary>
        /// Get's or set's the load radius.
        /// </summary>
        public virtual float LoadRadius
        {
            set 
            { 
                mLoadRadius = value;
                UpdateDerivedMetrics();
            }
            get { return mLoadRadius; }
        }

        /// <summary>
        /// Get's or set's the hold radius.
        /// </summary>
        public virtual float HoldRadius
        {
            set 
            { 
                mHoldRadius = value;
                UpdateDerivedMetrics();
            }
            get { return mHoldRadius; }
        }

        /// <summary>
        /// Get the load radius as a multiple of cells
        /// </summary>
        public virtual float LoadRadiusInCells
        {
            get { return mLoadRadiusInCells; }
        }

        /// <summary>
        /// Get the Hold radius as a multiple of cells
        /// </summary>
        public virtual float HoldRadiusInCells
        {
            get { return mHoldRadiusInCells; }
        }

        /// <summary>
        /// 
        /// </summary>
        public Grid2DPageStrategyData()
        {
            mMode = Grid2Mode.G2D_X_Z;
            mWorldOrigin = Vector3.Zero;
            mOrigin = Vector2.Zero;
            mGridExtentsHorz = 65536;
            mGridExtentsVert = 65536;
            mCellSize = 1000;
            mLoadRadius = 2000;
            mHoldRadius = 3000;

            UpdateDerivedMetrics();
        }
        /// <summary>
        /// Set the number of cells in the grid in each dimension (defaults to max of 65536)
        /// </summary>
        /// <param name="horz"></param>
        /// <param name="vert"></param>
        public void SetCellCount(int horz, int vert)
        {
            mGridExtentsHorz = horz;
            mGridExtentsVert = vert;
            UpdateDerivedMetrics();
        }
        
        /// <summary>
        /// Load this data from a stream (returns true if successful)
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public virtual bool Load(StreamSerializer stream)
        {
            if (stream.ReadChunkBegin(CHUNK_ID, CHUNK_VERSION, "Grid2DPageStrategyData") == null)
                return false;

            int readMode = 0;
            stream.Read(out readMode);
            mMode = (Grid2Mode)readMode;
            Vector3 orgin = new Vector3();
            stream.Read(out orgin);
            Origin = orgin;
            stream.Read(out mCellSize);
            stream.Read(out mLoadRadius);
            stream.Read(out mHoldRadius);

            uint id = 0;
            stream.ReadChunkEnd(id);
            CHUNK_ID = id;

            return true;
        }
        /// <summary>
        /// Save this data to a stream
        /// </summary>
        /// <param name="stream"></param>
        public virtual void Save(StreamSerializer stream)
        {
            stream.WriteChunkBegin(CHUNK_ID, CHUNK_VERSION);
            stream.Write((int)mMode);
            stream.Write(mWorldOrigin);
            stream.Write(mCellSize);
            stream.Write(mLoadRadius);
            stream.Write(mHoldRadius);
            stream.WriteChunkEnd(CHUNK_ID);
        }
        /// <summary>
        /// Convert a world point to grid space (not relative to origin)
        /// </summary>
        /// <param name="world"></param>
        /// <param name="grid"></param>
        public virtual void ConvetWorldToGridSpace(Vector3 world, ref Vector2 grid)
        {
            switch (mMode)
            {
                case Grid2Mode.G2D_X_Z:
                    grid.x = world.x;
                    grid.y = -world.z;
                    break;
                case Grid2Mode.G2D_X_Y:
                    grid.x = world.x;
                    grid.y = world.y;
                    break;
                case Grid2Mode.G2D_Y_Z:
                    grid.x = -world.z;
                    grid.y = world.y;
                    break;
                default:
                    //should never happen ;)
                    throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Convert a grid point to world space - note only 2 axes populated
        /// </summary>
        /// <param name="grid"></param>
        /// <param name="world"></param>
        public virtual void ConvertGridToWorldSpace(Vector2 grid, ref Vector3 world)
        {
            switch (mMode)
            {
                case Grid2Mode.G2D_X_Z:
                    world.x = grid.x;
                    world.z = -grid.y;
                    break;
                case Grid2Mode.G2D_X_Y:
                    world.x = grid.x;
                    world.y = grid.y;
                    break;
                case Grid2Mode.G2D_Y_Z:
                    world.z = -grid.x;
                    world.y = grid.y;
                    break;
                default:
                    //should never happen ;)
                    throw new NotImplementedException();
            }
        }
        /// <summary>
        /// Get the (grid space) mid point of a cell
        /// </summary>
        /// <param name="row"></param>
        /// <param name="col"></param>
        /// <param name="mid"></param>
        public virtual void GetMidPointGridSpace(int row, int col, ref Vector2 mid)
        {
            GetBottomLeftGridSpace(row, col, ref mid);
            mid.x += mCellSize * 0.5f;
            mid.y += mCellSize * 0.5f;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="row"></param>
        /// <param name="col"></param>
        /// <param name="pFourPoints"></param>
        public virtual void GetCornersGridSpace(int row, int col, ref Vector2[] fourPoints)
        {
            GetBottomLeftGridSpace(row, col, ref fourPoints[0]);
            fourPoints[1] = fourPoints[0] + new Vector2(mCellSize, 0);
            fourPoints[2] = fourPoints[0] + new Vector2(mCellSize, mCellSize);
            fourPoints[3] = fourPoints[0] + new Vector2(0, mCellSize);
        }
        /// <summary>
        /// Get the (grid space) corners of a cell.
        /// </summary>
        /// <param name="row"></param>
        /// <param name="col"></param>
        /// <param name="pFourPoints"></param>
        public virtual void GetBottomLeftGridSpace(int row, int col, ref Vector2 bl)
        {
            bl.x = mBottomLeft.x + col * mCellSize;
            bl.y = mBottomLeft.y + row * mCellSize;
        }
        /// <summary>
        /// Convert a grid position into a row and column index
        /// </summary>
        /// <param name="gridPos"></param>
        /// <param name="row"></param>
        /// <param name="col"></param>
        public void DetermineGridLocation(Vector2 gridPos, ref int row, ref int col)
        {
            // get distance from bottom-left (indexing start)
            Vector2 localpos = gridPos - mBottomLeft;
            //int cells
            localpos = Vector2.Zero;
            localpos.x = localpos.x / mCellSize;
            localpos.y = localpos.y / mCellSize;
            //truncate
            col = (int)localpos.x;
            row = (int)localpos.y;
        }

        public PageID CalculatePageID(int x, int y)
        {
            // Convert to signed 16-bit so sign bit is in bit 15
            Int16 xs16 = (Int16)x;
            Int16 ys16 = (Int16)y;

            // convert to unsigned because we do not want to propagate sign bit to 32-bits
            UInt16 x16 = (UInt16)xs16;
            UInt16 y16 = (UInt16)ys16;

            uint key = 0;
            key = (uint)( ( x16 << 16 ) | y16 );

            return new PageID( key );
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="inPageID"></param>
        /// <param name="row"></param>
        /// <param name="col"></param>
        public void CalculateRowCol(PageID inPageID, ref int row, ref int col)
        {
            // inverse of calculatePageID
            row = (int)( inPageID.Value / mGridExtentsHorz );
            col = (int)( inPageID.Value % mGridExtentsHorz );
        }
        /// <summary>
        /// 
        /// </summary>
        protected void UpdateDerivedMetrics() 
        {
            mLoadRadiusInCells = mLoadRadius / mCellSize;
            mHoldRadiusInCells = mHoldRadius / mCellSize;
            mBottomLeft = mOrigin - new Vector2(mCellSize * mGridExtentsHorz * 0.5f,
                                                mCellSize * mGridExtentsVert * 0.5f);
        }




        public void SetCellRange( int minX, int minY, int maxX, int maxY )
        {
            throw new NotImplementedException();
        }

        public int CellRangeMinX
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public int CellRangeMinY
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public int CellRangeMaxX
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public int CellRangeMaxY
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }
    }
}
