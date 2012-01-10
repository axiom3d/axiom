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

#region Namespace Declarations

using System;
using System.Collections.Generic;
using Axiom.Core;
using Axiom.Math;
using Axiom.Graphics;

#endregion Namespace Declarations

namespace Axiom.Components.Paging
{
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
    /// Specialisation of PageStrategyData for GridPageStrategy.
    /// </summary>
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
        /// <param name="manager"></param>
        public Grid2PageStrategy(PageManager manager)
            : base("Grid2D", manager)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cam"></param>
        /// <param name="section"></param>
        public override void NotifyCamera(Camera cam, PagedWorldSection section)
        {
            Grid2DPageStrategyData stratData = (Grid2DPageStrategyData)section.StrategyData;

            Vector3 pos = cam.DerivedPosition;
            Vector2 gridpos = Vector2.Zero;
            stratData.ConvetWorldToGridSpace(pos, ref gridpos);
            int row = 0, col = 0;
            stratData.DetermineGridLocation(gridpos, ref row, ref col);

            float loadRadius = stratData.LoadRadiusInCells;
            float holdRadius = stratData.HoldRadiusInCells;
            //scan the whole hold range
            float frowmin = (float)row - holdRadius;
            float frowmax = (float)row + holdRadius;
            float fcolmin = (float)col - holdRadius;
            float fcolmax = (float)col + holdRadius;

            int clampRowAt = stratData.CellCountVert - 1;
            int clampColAt = stratData.CellCountHorz - 1;

            //round UP max, round DOWN min
            int rowmin = frowmin < 0 ? 0 : (int)System.Math.Floor(frowmin);
            int rowmax = frowmax > clampRowAt ? clampRowAt : (int)System.Math.Ceiling(frowmax);
            int colmin = fcolmin < 0 ? 0 : (int)System.Math.Floor(fcolmin);
            int colmax = fcolmax > clampColAt ? clampColAt : (int)System.Math.Ceiling(fcolmax);
            // the inner, active load range
            frowmin = (float)row - loadRadius;
            frowmax = (float)row + loadRadius;
            fcolmin = (float)col - loadRadius;
            fcolmax = (float)col + loadRadius;
            //round UP max, round DOWN min
            int loadrowmin = frowmin < 0 ? 0 : (int)System.Math.Floor(frowmin);
            int loadrowmax = frowmax > clampRowAt ? clampRowAt : (int)System.Math.Ceiling(frowmax);
            int loadcolmin = fcolmin < 0 ? 0 : (int)System.Math.Floor(fcolmin);
            int loadcolmax = fcolmax > clampColAt ? clampColAt : (int)System.Math.Ceiling(fcolmax);

            for (int r = rowmin; r <= rowmax; ++r)
            {
                for (int c = colmin; c <= colmax; ++c)
                {
                    PageID pageID = stratData.CalculatePageID(r, c);
                    if (r >= loadrowmin && r <= loadrowmax && c >= loadcolmin && c <= loadcolmax)
                    {
                        // int the 'load' range, request it
                        section.LoadPage(pageID);
                    }
                    else
                    {
                        // int the outer 'hold' range, keep it but don't actively load.
                        section.HoldPage(pageID);
                    }
                    // other paged will by inference be marked for unloading
                }
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override IPageStrategyData CreateData()
        {
            return new Grid2DPageStrategyData();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        public override void DestroyData(IPageStrategyData data)
        {
            data = null;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="p"></param>
        /// <param name="n"></param>
        public override void UpdateDebugDisplay(Page p, SceneNode sn)
        {
            uint dbglvl = mManager.DebugDisplayLevel;
            if (dbglvl != 0)
            {
                // we could try to avoid updating the geometry every time here, but this 
                // wouldn't easily deal with paging parameter changes. There shouldn't 
                // be that many pages anyway, and this is debug after all, so update every time
                int row = 0, col = 0;
                Grid2DPageStrategyData stratData = (Grid2DPageStrategyData)p.ParentSection.StrategyData;
                stratData.CalculateRowCol(p.PageID, ref row, ref col);
#warning data is created twice here, should be useless.
                Grid2DPageStrategyData data = (Grid2DPageStrategyData)p.ParentSection.StrategyData;
                // Determine our centre point, we'll anchor here
                // Note that world points are initialised to ZERO since only 2 dimensions
                // are updated by the grid data (we could display this grid anywhere)
                Vector2 gridMidPoint = Vector2.Zero;
                Vector3 worldMidPoint = Vector3.Zero;
                data.GetMidPointGridSpace(row, col, ref gridMidPoint);
                data.ConvertGridToWorldSpace(gridMidPoint, ref worldMidPoint);

                sn.Position = worldMidPoint;
                Vector2[] gridCorners = new Vector2[4];
                Vector3[] worldCorners = new Vector3[4];

                data.GetCornersGridSpace(row, col, ref gridCorners);
                for (int i = 0; i < 4; ++i)
                {
                    worldCorners[i] = Vector3.Zero;
                    data.ConvertGridToWorldSpace(gridCorners[i], ref worldCorners[i]);
                    //make relative to mid point
                    worldCorners[i] -= worldMidPoint;
                }
                
                string matName = "Ogre/G2D/Debug";
                Material mat = (Material)MaterialManager.Instance.GetByName(matName);
                if (mat == null)
                {
                    mat = (Material)MaterialManager.Instance.Create(matName, ResourceGroupManager.DefaultResourceGroupName);
                    Pass pass = mat.GetTechnique(0).GetPass(0);
                    pass.LightingEnabled = false;
                    pass.VertexColorTracking = TrackVertexColor.Ambient;
                    pass.DepthWrite = false;
                    mat.Load();
                }

                ManualObject mo = null;
                if (sn.ObjectCount == 0)
                {
                    mo = p.ParentSection.SceneManager.CreateManualObject("Grid2PageStrategyManualObject");
                    mo.Begin(matName, OperationType.LineStrip);
                }
                else
                {
					mo = (ManualObject)sn.GetObject( "Grid2PageStrategyManualObject" );
                    mo.BeginUpdate(0);
                }

                ColorEx vcol = p.Status == UnitStatus.Loaded ? ColorEx.Green : ColorEx.Red;

                for (int i = 0; i < 5; ++i)
                {
                    mo.Position(worldCorners[i % 4]);
                    mo.Color(vcol);
                }

                mo.End();
                if (sn.ObjectCount == 0)
                    sn.AttachObject(mo);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="worldPos"></param>
        /// <param name="section"></param>
        /// <returns></returns>
        public override PageID GetPageID(Vector3 worldPos, PagedWorldSection section)
        {
            Grid2DPageStrategyData stratData = (Grid2DPageStrategyData)section.StrategyData;

            Vector2 gridpos = Vector2.Zero;
            stratData.ConvetWorldToGridSpace(worldPos, ref gridpos);
            int row = 0, col = 0;
            stratData.DetermineGridLocation(gridpos, ref row, ref col);
            return stratData.CalculatePageID(row, col);
        }
    }
}
