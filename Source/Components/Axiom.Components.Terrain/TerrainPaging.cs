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

#endregion Namespace Declarations

namespace Axiom.Components.Terrain
{
    /// <summary>
    /// This class is the 'core' class for paging terrain, that will integrate
    /// with the larger paging system and provide the appropriate utility
    /// classes required.
    /// </summary>
    /// <remarks>
    /// You should construct this class after PageManager and before any PagedWorlds
    /// that might use it are created / loaded. Once constructed, it will make
    /// the "Terrain" PagedWorldSection type available, which uses a grid strategy for
    /// paging and uses TerrainGroup for the content. Other content can 
    /// be embedded in the pages too but the terrain is done like this in order
    /// to maintain connections between pages and other global data.
    /// @par
    /// Because PagedWorld and all attached classes have to be loadable from a stream, 
    /// most of the functionality is provided behind generalised interfaces. However,
    /// for constructing a paged terrain in code you can use utility methods on 
    /// this class. This procedurally created data can then be saved in a generic 
    /// form which will reconstruct on loading. Alternatively you can use the 
    /// generic methods and simply cast based on your prior knowledge of the types
    /// (or checking the type names exposed).
    /// </remarks>
    public class TerrainPaging : DisposableObject
    {
        #region Nested Types

        protected class SectionFactory : PagedWorldSectionFactory
        {
            public override string Name
            {
                [OgreVersion(1, 7, 2)]
                get
                {
                    return "Terrain";
                }
            }

            [OgreVersion(1, 7, 2)]
            public override PagedWorldSection CreateInstance(string name, PagedWorld parent, SceneManager sm)
            {
                return new TerrainPagedWorldSection(name, parent, sm);
            }

            [OgreVersion(1, 7, 2)]
            public override void DestroyInstance(ref PagedWorldSection p)
            {
                if (!p.IsDisposed)
                {
                    p.Dispose();
                }
            }
        };

        #endregion Nested Types

        #region Fields

        protected PageManager manager;
        protected SectionFactory sectionFactory = new SectionFactory();

        #endregion Fields

        [OgreVersion(1, 7, 2)]
        public TerrainPaging(PageManager pageMgr)
        {
            this.manager = pageMgr;
            this.manager.AddWorldSectionFactory(this.sectionFactory);
        }

        [OgreVersion(1, 7, 2, "~TerrainPaging")]
        protected override void dispose(bool disposeManagedResources)
        {
            if (!IsDisposed)
            {
                if (disposeManagedResources)
                {
                    this.manager.RemoveWorldSectionFactory(this.sectionFactory);
                }
            }
            base.dispose(disposeManagedResources);
        }

        /// <summary>
        /// Create a TerrainPagedWorldSection.
        /// </summary>
        /// <remarks>
        /// This is the simplest way to create a world section which is configured
        /// to contain terrain (among other objects if you want). You can also do this
        /// by calling PagedWorld::createSection with the type "Terrain" but there
        /// are more steps to configuring it that way (note: this is how loading 
        /// works though so it remains generic).
        /// </remarks>
        /// <param name="world">The PagedWorld that is to contain this section</param>
        /// <param name="terrainGroup">
        /// A TerrainGroup which must have been pre-configured before
        /// this call, to at least set the origin, the world size, and the file name
        /// convention.
        /// </param>
        /// <param name="loadRadius">The radius from the camera at which terrain pages will be loaded</param>
        /// <param name="holdRadius">
        /// The radius from the camera at which terrain pages will be held in memory
        /// but not loaded if they're not already. This must be larger than loadRadius and is used
        /// to minimise thrashing if the camera goes backwards and forwards at the loading border.
        /// </param>
        /// <param name="minX">
        /// The min/max page indexes that the world will try to load pages for, 
        /// as measured from the origin page at (0,0) by a signed index. The default is -10 to 10
        /// in each direction or 20x20 pages.
        /// </param>
        /// <param name="minY">
        /// The min/max page indexes that the world will try to load pages for, 
        /// as measured from the origin page at (0,0) by a signed index. The default is -10 to 10
        /// in each direction or 20x20 pages.
        /// </param>
        /// <param name="maxX">
        /// The min/max page indexes that the world will try to load pages for, 
        /// as measured from the origin page at (0,0) by a signed index. The default is -10 to 10
        /// in each direction or 20x20 pages.
        /// </param>
        /// <param name="maxY">
        /// The min/max page indexes that the world will try to load pages for, 
        /// as measured from the origin page at (0,0) by a signed index. The default is -10 to 10
        /// in each direction or 20x20 pages.
        /// </param>
        /// <param name="sectionName">An optional name to give the section (if none is
        /// provided, one will be generated)</param>
        /// <returns>The world section which is already attached to and owned by the world you passed in. 
        /// There is no 'destroy' method because you destroy via the PagedWorld, this is just a
        /// helper function.</returns>
        [OgreVersion(1, 7, 2)]
        public TerrainPagedWorldSection CreateWorldSection(PagedWorld world, TerrainGroup terrainGroup, Real loadRadius,
                                                            Real holdRadius,
#if NET_40
            int minX = -10, int minY = -10, int maxX = 10, int maxY = 10, string sectionName = "" )
#else
                                                            int minX, int minY, int maxX, int maxY, string sectionName)
#endif
        {
            var ret =
                (TerrainPagedWorldSection)world.CreateSection(terrainGroup.SceneManager, this.sectionFactory.Name, sectionName);

            ret.Initialize(terrainGroup);
            ret.LoadRadius = loadRadius;
            ret.HoldRadius = holdRadius;
            ret.SetPageRange(minX, minY, maxX, maxY);

            return ret;
        }

#if !NET_40
        /// <see cref="TerrainPagedWorldSection.CreateWorldSection( PagedWorld, TerrainGroup, Real, Real, int, int, int, int, string )"/>
        public TerrainPagedWorldSection CreateWorldSection(PagedWorld world, TerrainGroup terrainGroup, Real loadRadius,
                                                            Real holdRadius)
        {
            return CreateWorldSection(world, terrainGroup, loadRadius, holdRadius, -10, -10, 10, 10, string.Empty);
        }

        /// <see cref="TerrainPagedWorldSection.CreateWorldSection( PagedWorld, TerrainGroup, Real, Real, int, int, int, int, string )"/>
        public TerrainPagedWorldSection CreateWorldSection(PagedWorld world, TerrainGroup terrainGroup, Real loadRadius,
                                                            Real holdRadius, int minX)
        {
            return CreateWorldSection(world, terrainGroup, loadRadius, holdRadius, minX, -10, 10, 10, string.Empty);
        }

        /// <see cref="TerrainPagedWorldSection.CreateWorldSection( PagedWorld, TerrainGroup, Real, Real, int, int, int, int, string )"/>
        public TerrainPagedWorldSection CreateWorldSection(PagedWorld world, TerrainGroup terrainGroup, Real loadRadius,
                                                            Real holdRadius, int minX, int minY)
        {
            return CreateWorldSection(world, terrainGroup, loadRadius, holdRadius, minX, minY, 10, 10, string.Empty);
        }

        /// <see cref="TerrainPagedWorldSection.CreateWorldSection( PagedWorld, TerrainGroup, Real, Real, int, int, int, int, string )"/>
        public TerrainPagedWorldSection CreateWorldSection(PagedWorld world, TerrainGroup terrainGroup, Real loadRadius,
                                                            Real holdRadius, int minX, int minY, int maxX)
        {
            return CreateWorldSection(world, terrainGroup, loadRadius, holdRadius, minX, minY, maxX, 10, string.Empty);
        }

        /// <see cref="TerrainPagedWorldSection.CreateWorldSection( PagedWorld, TerrainGroup, Real, Real, int, int, int, int, string )"/>
        public TerrainPagedWorldSection CreateWorldSection(PagedWorld world, TerrainGroup terrainGroup, Real loadRadius,
                                                            Real holdRadius, int minX, int minY, int maxX, int maxY)
        {
            return CreateWorldSection(world, terrainGroup, loadRadius, holdRadius, minX, minY, maxX, maxY, string.Empty);
        }
#endif
    };
}