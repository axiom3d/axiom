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
using Axiom.Core;
using Axiom.Math;
using Axiom.Serialization;

#endregion Namespace Declarations

namespace Axiom.Components.Paging
{
    /// <summary>
    /// Specialisation of PageStrategyData for GridPageStrategy.
    /// </summary>
    /// <remarks>
    /// Structurally this data defines with a grid of pages, with the logical 
    /// origin in the middle of the entire grid.
    /// The grid cells are indexed from 0 as a 'centre' slot, supporting both 
    /// positive and negative values. so (0,0) is the centre cell, (1,0) is the
    /// cell to the right of the centre, (1,0) is the cell above the centre, (-2,1) 
    /// is the cell two to the left of the centre and one up, etc. The maximum
    /// extent of each axis is -32768 to +32767, so in other words enough for
    /// over 4 billion entries. 
    /// @par
    /// To limit the page load requests that are generated to a fixed region, 
    /// you can set the min and max cell indexes (inclusive)for each direction;
    /// if a page request would address a cell outside this range it is ignored
    /// so you don't have the expense of checking for page data that will never
    /// exist.
    /// @par
    /// The data format for this in a file is:<br/>
    /// <b>Grid2DPageStrategyData (Identifier 'G2DD')</b>\n
    /// [Version 1]
    /// <table>
    /// <tr>
    /// <td><b>Name</b></td>
    /// <td><b>Type</b></td>
    /// <td><b>Description</b></td>
    /// </tr>
    /// <tr>
    /// <td>Grid orientation</td>
    /// <td>byte</td>
    /// <td>The orientation of the grid; XZ = 0, XY = 1, YZ = 2</td>
    /// </tr>
    /// <tr>
    /// <td>Grid origin</td>
    /// <td>Vector3</td>
    /// <td>World origin of the grid.</td>
    /// </tr>
    /// <tr>
    /// <td>Grid cell size</td>
    /// <td>Real</td>
    /// <td>The size of each cell (page) in the grid</td>
    /// </tr>
    /// <tr>
    /// <td>Grid cell range (minx, maxx, miny, maxy)</td>
    /// <td>short * 4</td>
    /// <td>The extents of the world in cell indexes</td>
    /// </tr>
    /// <tr>
    /// <td>Load radius</td>
    /// <td>Real</td>
    /// <td>The outer radius at which new pages should start loading</td>
    /// </tr>
    /// <tr>
    /// <td>Hold radius</td>
    /// <td>Real</td>
    /// <td>The radius at which existing pages should be held if already loaded 
    /// 	but not actively loaded (should be larger than Load radius)</td>
    /// </tr>
    /// </table>
    /// </remarks>
    public class Grid2DPageStrategyData : IPageStrategyData
    {
        #region - constants -

        public static uint CHUNK_ID = StreamSerializer.MakeIdentifier( "G2DD" );
        public static ushort CHUNK_VERSION = 1;
        
        #endregion - constants -

        #region - fields -

        /// <summary>
        /// Orientation of the grid.
        /// </summary>
        protected Grid2Mode mMode = Grid2Mode.G2D_X_Z;
        
        /// <summary>
        /// Origin (world space)
        /// </summary>
        protected Vector3 mWorldOrigin = Vector3.Zero;
        
        /// <summary>
        /// Origin (grid-aligned world space)
        /// </summary>
        protected Vector2 mOrigin = Vector2.Zero;
        
        /// <summary>
        /// Grid cell (page) size.
        /// </summary>
        protected Real mCellSize = 1000;

        /// <summary>
        /// Load radius
        /// </summary>
        protected Real mLoadRadius = 2000;

        /// <summary>
        /// hold radius.
        /// </summary>
        protected Real mHoldRadius = 3000;
       
        protected Real mLoadRadiusInCells;
        protected Real mHoldRadiusInCells;

        protected int mMinCellX = -32768;
        protected int mMinCellY = -32768;
        protected int mMaxCellX = 32767;
        protected int mMaxCellY = 32767;

        #endregion - fields -

        #region - properties -
        
        /// <summary>
        /// Get/Set the grid alignment mode
        /// </summary>
        public virtual Grid2Mode Mode
        {
            [OgreVersion( 1, 7, 2 )]
            get { return mMode; }

            [OgreVersion( 1, 7, 2 )]
            set
            {
                mMode = value;
                //reset origin
                Origin = mWorldOrigin;
            }
        }

        /// <summary>
        /// Get/Set the origin of the grid in world space
        /// </summary>
        public virtual Vector3 Origin
        {
            [OgreVersion( 1, 7, 2 )]
            get { return mWorldOrigin; }

            [OgreVersion( 1, 7, 2 )]
            set
            {
                mWorldOrigin = value;
                ConvertWorldToGridSpace( mWorldOrigin, ref mOrigin );
                UpdateDerivedMetrics();
            }
        }

        /// <summary>
        /// Get/Set the size of the cells in the grid
        /// </summary>
        public virtual Real CellSize
        {
            [OgreVersion( 1, 7, 2 )]
            get { return mCellSize; }

            [OgreVersion( 1, 7, 2 )]
            set
            {
                mCellSize = value;
                UpdateDerivedMetrics();
            }
        }

        public int CellRangeMinX
        {
            [OgreVersion( 1, 7, 2 )]
            get
            {
                return mMinCellX;
            }

            [OgreVersion( 1, 7, 2 )]
            set
            {
                mMinCellX = value;
            }
        }

        public int CellRangeMinY
        {
            [OgreVersion( 1, 7, 2 )]
            get
            {
                return mMinCellY;
            }

            [OgreVersion( 1, 7, 2 )]
            set
            {
                mMinCellY = value;
            }
        }

        public int CellRangeMaxX
        {
            [OgreVersion( 1, 7, 2 )]
            get
            {
                return mMaxCellX;
            }

            [OgreVersion( 1, 7, 2 )]
            set
            {
                mMaxCellX = value;
            }
        }

        public int CellRangeMaxY
        {
            [OgreVersion( 1, 7, 2 )]
            get
            {
                return mMaxCellY;
            }

            [OgreVersion( 1, 7, 2 )]
            set
            {
                mMaxCellY = value;
            }
        }

        /// <summary>
        /// Get's or set's the loading radius.
        /// </summary>
        public virtual Real LoadRadius
        {
            [OgreVersion( 1, 7, 2 )]
            get { return mLoadRadius; }

            [OgreVersion( 1, 7, 2 )]
            set
            {
                mLoadRadius = value;
                UpdateDerivedMetrics();
            }
        }

        /// <summary>
        /// Get's or set's the holding radius.
        /// </summary>
        public virtual Real HoldRadius
        {
            [OgreVersion( 1, 7, 2 )]
            get { return mHoldRadius; }

            [OgreVersion( 1, 7, 2 )]
            set
            {
                mHoldRadius = value;
                UpdateDerivedMetrics();
            }
        }

        /// <summary>
        /// Get the load radius as a multiple of cells
        /// </summary>
        public virtual Real LoadRadiusInCells
        {
            [OgreVersion( 1, 7, 2 )]
            get { return mLoadRadiusInCells; }
        }

        /// <summary>
        /// Get the Hold radius as a multiple of cells
        /// </summary>
        public virtual Real HoldRadiusInCells
        {
            [OgreVersion( 1, 7, 2 )]
            get { return mHoldRadiusInCells; }
        }
        
        #endregion - properties -

        [OgreVersion( 1, 7, 2 )]
        public Grid2DPageStrategyData()
        {
            UpdateDerivedMetrics();
        }

        /// <summary>
        /// Set the index range of all cells (values outside this will be ignored)
        /// </summary>
        [OgreVersion( 1, 7, 2 )]
        public void SetCellRange( int minX, int minY, int maxX, int maxY )
        {
            mMinCellX = minX;
            mMinCellY = minY;
            mMaxCellX = maxX;
            mMaxCellY = maxY;
        }

        /// <summary>
        /// Convert a world point to grid space (not relative to origin)
        /// </summary>
        [OgreVersion( 1, 7, 2 )]
        public virtual void ConvertWorldToGridSpace( Vector3 world, ref Vector2 grid )
        {
            switch ( mMode )
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
                    throw new AxiomException( "Invalid grid mode detected!" );
            }
        }

        /// <summary>
        /// Convert a grid point to world space - note only 2 axes populated
        /// </summary>
        [OgreVersion( 1, 7, 2 )]
        public virtual void ConvertGridToWorldSpace( Vector2 grid, ref Vector3 world )
        {
            // Note that we don't set the 3rd coordinate, let the caller determine that
            switch ( mMode )
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
                    throw new AxiomException( "Invalid grid mode detected!" );
            }
        }

        [OgreVersion( 1, 7, 2 )]
        protected void UpdateDerivedMetrics()
        {
            mLoadRadiusInCells = mLoadRadius / mCellSize;
            mHoldRadiusInCells = mHoldRadius / mCellSize;
        }

        /// <summary>
        /// Convert a grid position into a row and column index
        /// </summary>
        [OgreVersion( 1, 7, 2 )]
        public void DetermineGridLocation( Vector2 gridPos, out int x, out int y )
        {
            Vector2 relPos = gridPos - mOrigin;
            Real offset = mCellSize * 0.5f;
            relPos.x += offset;
            relPos.y += offset;

            x = (int)System.Math.Floor( relPos.x / mCellSize );
            y = (int)System.Math.Floor( relPos.y / mCellSize );
        }

        /// <summary>
        /// Get the (grid space) bottom-left of a cell
        /// </summary>
        [OgreVersion( 1, 7, 2 )]
        public virtual void GetBottomLeftGridSpace( int x, int y, ref Vector2 bl )
        {
            Real offset = mCellSize * 0.5f;
            bl.x = mOrigin.x - offset + x * mCellSize;
            bl.y = mOrigin.y - offset + y * mCellSize;
        }

        /// <summary>
        /// Get the (grid space) mid point of a cell
        /// </summary>
        [OgreVersion( 1, 7, 2 )]
        public virtual void GetMidPointGridSpace( int x, int y, ref Vector2 mid )
        {
            mid.x = mOrigin.x + x * mCellSize;
            mid.y = mOrigin.y + y * mCellSize;
        }

        /// <summary>
        /// Get the (grid space) corners of a cell.
        /// </summary>
        /// <remarks>
        /// Populates pFourPoints in anticlockwise order from the bottom left point.
        /// </remarks>
        [OgreVersion( 1, 7, 2 )]
        public virtual void GetCornersGridSpace( int x, int y, ref Vector2[] fourPoints )
        {
            this.GetBottomLeftGridSpace( x, y, ref fourPoints[ 0 ] );
            fourPoints[ 1 ] = fourPoints[ 0 ] + new Vector2( mCellSize, 0 );
            fourPoints[ 2 ] = fourPoints[ 0 ] + new Vector2( mCellSize, mCellSize );
            fourPoints[ 3 ] = fourPoints[ 0 ] + new Vector2( 0, mCellSize );
        }

        [OgreVersion( 1, 7, 2 )]
        public PageID CalculatePageID( int x, int y )
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

        [OgreVersion( 1, 7, 2 )]
        public void CalculateCell( PageID inPageID, out int x, out int y )
        {
            // inverse of calculatePageID
            // unsigned versions
            UInt16 y16 = (UInt16)( inPageID.Value & 0xFFFF );
            UInt16 x16 = (UInt16)( ( inPageID.Value >> 16 ) & 0xFFFF );

            x = (Int16)x16;
            y = (Int16)y16;
        }

        /// <summary>
        /// Load this data from a stream (returns true if successful)
        /// </summary>
        [OgreVersion( 1, 7, 2 )]
        public bool Load( StreamSerializer stream )
        {
            if ( stream.ReadChunkBegin( CHUNK_ID, CHUNK_VERSION, "Grid2DPageStrategyData" ) == null )
                return false;

            byte readMode = 0;
            stream.Read( out readMode );
            mMode = (Grid2Mode)readMode;

            Vector3 orgin;
            stream.Read( out orgin );
            Origin = orgin;

            stream.Read( out mCellSize );
            stream.Read( out mLoadRadius );
            stream.Read( out mHoldRadius );
            stream.Read( out mMinCellX );
            stream.Read( out mMaxCellX );
            stream.Read( out mMinCellY );
            stream.Read( out mMaxCellY );

            stream.ReadChunkEnd( CHUNK_ID );

            return true;
        }

        /// <summary>
        /// Save this data to a stream
        /// </summary>
        [OgreVersion( 1, 7, 2 )]
        public void Save( StreamSerializer stream )
        {
            stream.WriteChunkBegin( CHUNK_ID, CHUNK_VERSION );
            stream.Write( (byte)mMode );
            stream.Write( mWorldOrigin );
            stream.Write( mCellSize );
            stream.Write( mLoadRadius );
            stream.Write( mHoldRadius );
            stream.Write( mMinCellX );
            stream.Write( mMaxCellX );
            stream.Write( mMinCellY );
            stream.Write( mMaxCellY );

            stream.WriteChunkEnd( CHUNK_ID );
        }
    };
}
