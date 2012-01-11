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

using Axiom.Components.Paging;
using Axiom.Core;
using Axiom.Math;
using Axiom.Serialization;

#endregion Namespace Declarations

namespace Axiom.Components.Terrain
{
    /// <summary>
    /// A world section which includes paged terrain. 
    /// </summary>
    /// <remarks>
    /// Rather than implement terrain paging as a PageContent subclass, because terrain
    /// benefits from direct knowledge of neighbour arrangements and the tight
    /// coupling between that and the paging strategy, instead we use a PagedWorldSection
    /// subclass. This automatically provides a PageStrategy subclass of the
    /// correct type (Grid2DPageStrategy) and derives the correct settings for
    /// it compared to the terrain being used. This frees the user from having to 
    /// try to match all these up through the generic interfaces. 
    /// @par
    /// When creating this in code, the user should make use of the helper
    /// functions on the TerrainPaging class. The basic sequence is that 
    /// you define your terrain settings in a TerrainGroup, and then create an 
    /// instance of TerrainPagedWorldSection via TerrainPaging::createWorldSection. 
    /// That's basically all there is to it - the normal rules of TerrainGroup
    /// apply, it's just that instead of having to choose when to try to load / unload
    /// pages from the TerrainGroup, that is automatically done by the paging system.
    /// You can also create other types of content in this PagedWorldSection other
    /// than terrain, it's just that this comes with terrain handling built-in.
    /// @par
    /// When this world data is saved, you basically get 3 sets of data - firstly
    /// the top-level 'world data' which is where the definition of the world
    /// sections are stored (and hence, the definition of this class, although none
    /// of the terrain instance content is included). Secondly, you get a number
    /// of .page files which include any other content that you might have inserted
    /// into the pages. Lastly, you get the standard terrain data files which are
    /// saved as per TerrainGroup.
    /// </remarks>
    public class TerrainPagedWorldSection : PagedWorldSection
    {
        protected TerrainGroup terrainGroup;

        /// <summary>
        /// Get the TerrainGroup which this world section is using. 
        /// </summary>
        /// <remarks>
        /// For information, you can use the returned TerrainGroup to 
        /// convert to/from x/y locations and the pageID, since the grid strategy
        /// is the same.
        /// </remarks>
        public virtual TerrainGroup TerrainGroup
        {
            [OgreVersion( 1, 7, 2 )]
            get
            {
                return terrainGroup;
            }
        }

        /// <summary>
        /// Convenience property - this section always uses a grid strategy
        /// </summary>
        public virtual Grid2PageStrategy GridStrategy
        {
            get
            {
                return (Grid2PageStrategy)this.Strategy;
            }
        }

        /// <summary>
        /// Convenience property - this section always uses a grid strategy
        /// </summary>
        public virtual Grid2DPageStrategyData GridStrategyData
        {
            get
            {
                return (Grid2DPageStrategyData)mStrategyData;
            }
        }

        /// <summary>
        /// Get/Set the loading radius
        /// </summary>
        public virtual Real LoadRadius
        {
            [OgreVersion( 1, 7, 2 )]
            get
            {
                return this.GridStrategyData.LoadRadius;
            }

            [OgreVersion( 1, 7, 2 )]
            set
            {
                this.GridStrategyData.LoadRadius = value;
            }
        }

        /// <summary>
        /// Get/Set the Holding radius
        /// </summary>
        public virtual Real HoldRadius
        {
            [OgreVersion( 1, 7, 2 )]
            get
            {
                return this.GridStrategyData.HoldRadius;
            }

            [OgreVersion( 1, 7, 2 )]
            set
            {
                this.GridStrategyData.HoldRadius = value;
            }
        }

        /// <summary>
        /// Get/Set the index range of all Pages (values outside this will be ignored)
        /// </summary>
        public virtual int PageRangeMinX
        {
            [OgreVersion( 1, 7, 2 )]
            get
            {
                return this.GridStrategyData.CellRangeMinX;
            }

            [OgreVersion( 1, 7, 2 )]
            set
            {
                this.GridStrategyData.CellRangeMinX = value;
            }
        }

        /// <summary>
        /// Get/Set the index range of all Pages (values outside this will be ignored)
        /// </summary>
        public virtual int PageRangeMinY
        {
            [OgreVersion( 1, 7, 2 )]
            get
            {
                return this.GridStrategyData.CellRangeMinY;
            }

            [OgreVersion( 1, 7, 2 )]
            set
            {
                this.GridStrategyData.CellRangeMinY = value;
            }
        }

        /// <summary>
        /// Get/Set the index range of all Pages (values outside this will be ignored)
        /// </summary>
        public virtual int PageRangeMaxX
        {
            [OgreVersion( 1, 7, 2 )]
            get
            {
                return this.GridStrategyData.CellRangeMaxX;
            }

            [OgreVersion( 1, 7, 2 )]
            set
            {
                this.GridStrategyData.CellRangeMaxX = value;
            }
        }

        /// <summary>
        /// Get/Set the index range of all Pages (values outside this will be ignored)
        /// </summary>
        public virtual int PageRangeMaxY
        {
            [OgreVersion( 1, 7, 2 )]
            get
            {
                return this.GridStrategyData.CellRangeMaxY;
            }

            [OgreVersion( 1, 7, 2 )]
            set
            {
                this.GridStrategyData.CellRangeMaxY = value;
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="name">The name of the section</param>
        /// <param name="parent">The parent world</param>
        /// <param name="sm">The SceneManager to use (can be left as null if to be loaded)</param>
        [OgreVersion( 1, 7, 2 )]
        public TerrainPagedWorldSection( string name, PageWorld parent, SceneManager sm )
            : base( name, parent, sm )
        {
            // we always use a grid strategy
            this.Strategy = parent.Manager.GetStrategy( "Grid2D" );
        }

        [OgreVersion( 1, 7, 2, "~TerrainPagedWorldSection" )]
        protected override void dispose( bool disposeManagedResources )
        {
            if ( !this.IsDisposed )
            {
                if ( disposeManagedResources )
                {
                    terrainGroup.Dispose();
                }
            }

            base.dispose( disposeManagedResources );
        }

        /// <summary>
        /// Initialise this section from an existing TerrainGroup instance.
        /// </summary>
        /// <remarks>
        /// This is the route you will take if you're defining this world section
        /// from scratch in code. The other alternative is that you'll be loading
        /// this section from a file, in which case all the settings will be
        /// derived from that.
        /// </remarks>
        /// <param name="grp">grp The TerrainGroup which will form the basis of this world section. 
        /// The instance will be owned by this class from now on and will be destroyed by it.</param>
        [OgreVersion( 1, 7, 2, "Original name was init" )]
        public virtual void Initialize( TerrainGroup grp )
        {
            if ( terrainGroup == grp )
                return;

            if ( terrainGroup != null )
                terrainGroup.Dispose();

            terrainGroup = grp;
            SyncSettings();

            // Unload all existing terrain pages, because we want the paging system
            // to be in charge of this
            terrainGroup.RemoveAllTerrains();
        }

        [OgreVersion( 1, 7, 2 )]
        protected virtual void SyncSettings()
        {
            // Base grid on terrain settings
            switch ( terrainGroup.Alignment )
            {
                case Alignment.Align_X_Y:
                    this.GridStrategyData.Mode = Grid2Mode.G2D_X_Y;
                    break;

                case Alignment.Align_X_Z:
                    this.GridStrategyData.Mode = Grid2Mode.G2D_X_Z;
                    break;

                case Alignment.Align_Y_Z:
                    this.GridStrategyData.Mode = Grid2Mode.G2D_Y_Z;
                    break;
            }

            this.GridStrategyData.Origin = terrainGroup.Origin;
            this.GridStrategyData.CellSize = terrainGroup.TerrainWorldSize;
        }

        /// <summary>
        /// Set the index range of all Pages (values outside this will be ignored)
        /// </summary>
        [OgreVersion( 1, 7, 2 )]
        public virtual void SetPageRange( int minX, int minY, int maxX, int maxY )
        {
            this.GridStrategyData.SetCellRange( minX, minY, maxX, maxY );
        }

        [OgreVersion( 1, 7, 2 )]
        protected override void LoadSubtypeData( StreamSerializer ser )
        {
            // we load the TerrainGroup information from here
            if ( terrainGroup == null )
                terrainGroup = new TerrainGroup( this.SceneManager );

            terrainGroup.LoadGroupDefinition( ref ser );

            // params that are in the Grid2DStrategyData will have already been loaded
            // as part of the main load() routine
            SyncSettings();
        }

        [OgreVersion( 1, 7, 2 )]
        protected override void SaveSubtypeData( StreamSerializer ser )
        {
            terrainGroup.SaveGroupDefinition( ref ser );

            // params that are in the Grid2DStrategyData will have already been saved
            // as part of the main save() routine
        }

        [OgreVersion( 1, 7, 2 )]
        public override void LoadPage( PageID pageID, bool forceSynchronous )
        {
            if ( !mParent.Manager.ArePagingOperationsEnabled )
                return;

            if ( mPages.ContainsKey( pageID ) )
            {
                // trigger terrain load
                long x, y;
                // pageID is the same as a packed index
                terrainGroup.UnpackIndex( pageID.Value, out x, out y );
                terrainGroup.DefineTerrain( x, y );
                terrainGroup.LoadTerrain( x, y, forceSynchronous );
            }

            base.LoadPage( pageID, forceSynchronous );
        }

        [OgreVersion( 1, 7, 2 )]
        public override void UnloadPage( PageID pageID, bool forceSynchonous )
        {
            if ( !mParent.Manager.ArePagingOperationsEnabled )
                return;

            base.UnloadPage( pageID, forceSynchonous );

            // trigger terrain unload
            long x, y;
            // pageID is the same as a packed index
            terrainGroup.UnpackIndex( pageID.Value, out x, out y );
            terrainGroup.UnloadTerrain( x, y );
        }
    };
}
