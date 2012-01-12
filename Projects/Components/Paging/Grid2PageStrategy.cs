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

using Axiom.Core;
using Axiom.Graphics;
using Axiom.Math;

#endregion Namespace Declarations

namespace Axiom.Components.Paging
{
    [OgreVersion( 1, 7, 2 )]
    public enum Grid2Mode
    {
        /// <summary>
        ///  Grid is in the X/Z plane
        /// </summary>
        G2D_X_Z = 0,
        /// <summary>
        /// Grid is in the X/Y plane
        /// </summary>
        G2D_X_Y = 1,
        /// <summary>
        /// Grid is in the Y/Z plane
        /// </summary>
        G2D_Y_Z = 2
    }
    
    /// <summary>
    /// Page strategy which loads new pages based on a regular 2D grid.
    /// </summary>
    /// <remarks>
    /// The grid can be up to 65536 x 65536 cells in size. PageIDs are generated
    /// like this: (row * 65536) + col. The grid is centred around the grid origin, such 
    /// that the boundaries of the cell around that origin are [-CellSize/2, CellSize/2)
    /// </remarks>
    public class Grid2PageStrategy : PageStrategy
    {
        /// <summary>
        /// Page strategy which loads new pages based on a regular 2D grid.
        /// </summary>
        /// <remarks>
        /// The grid can be up to 65536 x 65536 cells in size. PageIDs are generated
		/// like this: (row * 65536) + col. The grid is centred around the grid origin, such 
		/// that the boundaries of the cell around that origin are [-CellSize/2, CellSize/2)
        /// </remarks>
        [OgreVersion( 1, 7, 2 )]
        public Grid2PageStrategy( PageManager manager )
            : base( "Grid2D", manager )
        {
        }

        [OgreVersion( 1, 7, 2 )]
        public override void NotifyCamera( Camera cam, PagedWorldSection section )
        {
            Grid2DPageStrategyData stratData = (Grid2DPageStrategyData)section.StrategyData;

            Vector3 pos = cam.DerivedPosition;
            Vector2 gridpos = Vector2.Zero;
            stratData.ConvertWorldToGridSpace(pos, ref gridpos);
            int x, y;
            stratData.DetermineGridLocation(gridpos, out x, out y);

            Real loadRadius = stratData.LoadRadiusInCells;
            Real holdRadius = stratData.HoldRadiusInCells;
            //scan the whole hold range
            Real fxmin = (Real)x - holdRadius;
            Real fxmax = (Real)x + holdRadius;
            Real fymin = (Real)y - holdRadius;
            Real fymax = (Real)y + holdRadius;

            int xmin = stratData.CellRangeMinX;
            int xmax = stratData.CellRangeMaxX;
            int ymin = stratData.CellRangeMinY;
            int ymax = stratData.CellRangeMaxY;

            // Round UP max, round DOWN min
            xmin = fxmin < xmin ? xmin : (int)System.Math.Floor( fxmin );
            xmax = fxmax > xmax ? xmax : (int)System.Math.Ceiling( fxmax );
            ymin = fymin < ymin ? ymin : (int)System.Math.Floor( fymin );
            ymax = fymax > ymax ? ymax : (int)System.Math.Ceiling( fymax );
            // the inner, active load range
            fxmin = (Real)x - loadRadius;
            fxmax = (Real)x + loadRadius;
            fymin = (Real)y - loadRadius;
            fymax = (Real)y + loadRadius;
            // Round UP max, round DOWN min
            int loadxmin = fxmin < xmin ? xmin : (int)System.Math.Floor( fxmin );
            int loadxmax = fxmax > xmax ? xmax : (int)System.Math.Ceiling( fxmax );
            int loadymin = fymin < ymin ? ymin : (int)System.Math.Floor( fymin );
            int loadymax = fymax > ymax ? ymax : (int)System.Math.Ceiling( fymax );

            for ( int cy = ymin; cy <= ymax; ++cy )
            {
                for ( int cx = xmin; cx <= xmax; ++cx )
                {
                    PageID pageID = stratData.CalculatePageID( cx, cy );
                    if ( cx >= loadxmin && cx <= loadxmax && cy >= loadymin && cy <= loadymax )
                    {
                        // in the 'load' range, request it
                        section.LoadPage( pageID );
                    }
                    else
                    {
                        // in the outer 'hold' range, keep it but don't actively load
                        section.HoldPage( pageID );
                    }
                    // other pages will by inference be marked for unloading
                }
            }	
        }

        [OgreVersion( 1, 7, 2 )]
        public override IPageStrategyData CreateData()
        {
            return new Grid2DPageStrategyData();
        }
        
        [OgreVersion( 1, 7, 2 )]
        public override void DestroyData( IPageStrategyData data )
        {
            data = null;
        }

        [OgreVersion( 1, 7, 2 )]
        public override void UpdateDebugDisplay( Page p, SceneNode sn )
        {
            byte dbglvl = mManager.DebugDisplayLevel;
            if ( dbglvl != 0 )
            {
                // we could try to avoid updating the geometry every time here, but this 
                // wouldn't easily deal with paging parameter changes. There shouldn't 
                // be that many pages anyway, and this is debug after all, so update every time
                int x, y;
                Grid2DPageStrategyData stratData = (Grid2DPageStrategyData)p.ParentSection.StrategyData;
                stratData.CalculateCell( p.PageID, out x, out y );

                Grid2DPageStrategyData data = (Grid2DPageStrategyData)p.ParentSection.StrategyData;

                // Determine our centre point, we'll anchor here
                // Note that world points are initialised to ZERO since only 2 dimensions
                // are updated by the grid data (we could display this grid anywhere)
                Vector2 gridMidPoint = Vector2.Zero;
                Vector3 worldMidPoint = Vector3.Zero;
                data.GetMidPointGridSpace( x, y, ref gridMidPoint );
                data.ConvertGridToWorldSpace( gridMidPoint, ref worldMidPoint );

                sn.Position = worldMidPoint;

                Vector2[] gridCorners = new Vector2[ 4 ];
                Vector3[] worldCorners = new Vector3[ 4 ];

                data.GetCornersGridSpace( x, y, ref gridCorners );
                for ( int i = 0; i < 4; ++i )
                {
                    worldCorners[ i ] = Vector3.Zero;
                    data.ConvertGridToWorldSpace( gridCorners[ i ], ref worldCorners[ i ] );
                    // make relative to mid point
                    worldCorners[ i ] -= worldMidPoint;
                }

                string matName = "Axiom/G2D/Debug";
                Material mat = (Material)MaterialManager.Instance.GetByName( matName );
                if ( mat == null )
                {
                    mat = (Material)MaterialManager.Instance.Create( matName, ResourceGroupManager.DefaultResourceGroupName );
                    Pass pass = mat.GetTechnique( 0 ).GetPass( 0 );
                    pass.LightingEnabled = false;
                    pass.VertexColorTracking = TrackVertexColor.Ambient;
                    pass.DepthWrite = false;
                    mat.Load();
                }

                ManualObject mo = null;
                if ( sn.ChildCount == 0 )
                {
                    mo = p.ParentSection.SceneManager.CreateManualObject();
                    mo.Begin( matName, OperationType.LineStrip );
                }
                else
                {
                    mo = (ManualObject)sn.GetObject( 0 );
                    mo.BeginUpdate( 0 );
                }

                ColorEx vcol = ColorEx.Green;
                for ( int i = 0; i < 5; ++i )
                {
                    mo.Position( worldCorners[ i % 4 ] );
                    mo.Color( vcol );
                }

                mo.End();

                if ( sn.ObjectCount == 0 )
                    sn.AttachObject( mo );
            }
        }

        [OgreVersion( 1, 7, 2 )]
        public override PageID GetPageID( Vector3 worldPos, PagedWorldSection section )
        {
            Grid2DPageStrategyData stratData = (Grid2DPageStrategyData)section.StrategyData;

            Vector2 gridpos = Vector2.Zero;
            stratData.ConvertWorldToGridSpace( worldPos, ref gridpos );
            int x, y;
            stratData.DetermineGridLocation( gridpos, out x, out y );
            return stratData.CalculatePageID( x, y );
        }
    }
}
