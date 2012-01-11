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

using System.Collections.Generic;
using Axiom.Core;
using Axiom.Math;
using Axiom.Serialization;

#endregion Namespace Declarations

namespace Axiom.Components.Paging
{
    /// <summary>
    /// Represents a section of the PagedWorld which uses a given PageStrategy, and
    /// which is made up of a generally localised set of Page instances.
    /// </summary>
    /// <remarks>
    /// The reason for PagedWorldSection is that you may wish to cater for multiple
    /// sections of your world which use a different approach to paging (ie a
    /// different PageStrategy), or which are significantly far apart or separate 
    /// that the parameters you want to pass to the PageStrategy are different.
    /// @par
    /// PagedWorldSection instances are fully contained within the PagedWorld and
    /// their definitions are loaded in their entirety when the PagedWorld is
    /// loaded. However, no Page instances are initially loaded - those are the
    /// responsibility of the PageStrategy.
    /// @par
    /// PagedWorldSection can be subclassed and derived types provided by a
    /// PagedWorldSectionFactory. These subclasses might come preconfigured
    /// with a strategy for example, or with additional metadata used only for
    /// that particular type of section.
    /// @par
    /// A PagedWorldSection targets a specific SceneManager. When you create one
    /// in code via PagedWorld::createSection, you pass that SceneManager in manually.
    /// When loading from a saved world file however, the SceneManager type and
    /// instance name are saved and that SceneManager is looked up on loading, or
    /// created if it didn't exist. 
    /// </remarks>
    public class PagedWorldSection : DisposableObject
    {
        #region - constants -

        public static uint CHUNK_ID = StreamSerializer.MakeIdentifier( "PWSC" );
        public static ushort CHUNK_VERSION = 1;

        #endregion - constants -

        #region - fields -

        protected Dictionary<PageID, Page> mPages = new Dictionary<PageID, Page>();
        protected string mName;
        protected AxisAlignedBox mAABB;
        protected PageWorld mParent;
        protected PageStrategy mStrategy;
        protected IPageStrategyData mStrategyData;
        protected PageProvider mPageProvider;
        protected SceneManager mSceneMgr;

        #endregion - fields -

        #region - properties -

        /// <summary>
        /// Get the data required by the PageStrategy which is specific to this world section
        /// </summary>
        public virtual IPageStrategyData StrategyData
        {
            [OgreVersion( 1, 7, 2 )]
            get { return mStrategyData; }
        }

        /// <summary>
        /// Get the name of this section
        /// </summary>
        public virtual string Name
        {
            [OgreVersion( 1, 7, 2 )]
            get { return mName; }
        }

        /// <summary>
        /// Get the type name of this section.
        /// </summary>
        public virtual string Type
        {
            [OgreVersion( 1, 7, 2 )]
            get
            {
                return "General";
            }
        }

        /// <summary>
        /// Change the page strategy.
        /// </summary>
        /// <remarks>
        /// Doing this will invalidate any pages attached to this world section, and
        /// require the PageStrategyData to be repopulated.
        /// </remarks>
        public virtual PageStrategy Strategy
        {
            [OgreVersion( 1, 7, 2 )]
            get { return mStrategy; }

            [OgreVersion( 1, 7, 2 )]
            set 
            {
                PageStrategy strat = value;
                if (strat != mStrategy)
                {
                    if (mStrategy != null)
                    {
                        mStrategy.DestroyData(mStrategyData);
                        mStrategy = null;
                        mStrategyData = null;
                    }
                    mStrategy = strat;
                    if (mStrategy != null)
                        mStrategyData = mStrategy.CreateData();

                    RemoveAllPages();
                }
            }
        }

        public virtual PageManager Manager
        {
            [OgreVersion( 1, 7, 2 )]
            get { return mParent.Manager; }
        }

        /// <summary>
        /// Get the parent world
        /// </summary>
        public virtual PageWorld World
        {
            [OgreVersion( 1, 7, 2 )]
            get { return mParent; }
        }

        public virtual AxisAlignedBox BoundingBox
        {
            [OgreVersion( 1, 7, 2 )]
            get { return mAABB; }

            [OgreVersion( 1, 7, 2 )]
            set { mAABB = value; }
        }

        /// <summary>
        /// Get/Set the current scene manager.
        /// </summary>
        /// <remarks>
        /// Doing this will invalidate any pages attached to this world section, and
		/// require the pages to be reloaded.
        /// </remarks>
        public virtual SceneManager SceneManager
        {
            [OgreVersion( 1, 7, 2 )]
            get { return mSceneMgr; }

            [OgreVersion( 1, 7, 2 )]
            set
            {
                if ( value != mSceneMgr )
                {
                    mSceneMgr = value;
                    RemoveAllPages();
                }
            }
        }

        /// <summary>
        /// Get/Set the PageProvider which can provide streams Pages in this section.
        /// </summary>
        /// <remarks>
        /// This is the top-level way that you can direct how Page data is loaded. 
        /// When data for a Page is requested for a PagedWorldSection, the following
        /// sequence of classes will be checked to see if they have a provider willing
        /// to supply the stream: PagedWorldSection, PagedWorld, PageManager.
        /// If none of these do, then the default behaviour is to look for a file
        /// called worldname_sectionname_pageID.page. 
        /// @note
        /// The caller remains responsible for the destruction of the provider.
        /// </remarks>
        public PageProvider PageProvider
        {
            [OgreVersion( 1, 7, 2 )]
            get { return mPageProvider; }

            [OgreVersion( 1, 7, 2 )]
            set { mPageProvider = value; }
        }

        #endregion - properties -

        #region - constructor -
        
        /// <summary>
        /// Construct a new instance, specifying just the parent and the scene manager.
        /// </summary>
        [OgreVersion( 1, 7, 2 )]
        public PagedWorldSection( string name, PageWorld parent, SceneManager sm )
            : base()
        {
            mName = name;
            mParent = parent;
            Strategy = null;
            mPageProvider = null;
            mSceneMgr = sm;
        }

        #endregion - constructor -

        [OgreVersion( 1, 7, 2, "~PagedWorldSection" )]
        protected override void dispose( bool disposeManagedResources )
        {
            if ( !this.IsDisposed )
            {
                if ( disposeManagedResources )
                {
                    if ( mStrategy != null )
                    {
                        mStrategy.DestroyData( mStrategyData );
                        mStrategy.Dispose();
                        mStrategyData = null;
                    }

                    RemoveAllPages();
                }
            }

            base.dispose( disposeManagedResources );
        }

        /// <summary>
        /// Change the page strategy.
        /// </summary>
        /// <remarks>
        /// Doing this will invalidate any pages attached to this world section, and
        /// require the PageStrategyData to be repopulated.
        /// </remarks>
        [OgreVersion( 1, 7, 2 )]
        public virtual void SetStrategy( string stratName )
        {
            this.Strategy = this.Manager.GetStrategy( stratName );
        }

        /// <summary>
        /// Change the SceneManager.
        /// </summary>
        /// <param name="smName">The instance name of the SceneManager</param>
        [OgreVersion( 1, 7, 2 )]
        public virtual void SetSceneManager( string smName )
        {
            this.SceneManager = Root.Instance.GetSceneManager( smName );
        }

        /// <summary>
        /// Load this section from a stream (returns true if successful)
        /// </summary>
        [OgreVersion( 1, 7, 2 )]
        public virtual bool Load( StreamSerializer stream )
        {
            if ( stream.ReadChunkBegin( CHUNK_ID, CHUNK_VERSION, "PagedWorldSection" ) == null )
                return false;

            //name
            stream.Read( out mName );
            // AABB
            stream.Read( out mAABB );
            // SceneManager type
            string smType, smInstanceName;
            SceneManager sm = null;
            stream.Read( out smType );
            stream.Read( out smInstanceName );
            if ( Root.Instance.HasSceneManager( smInstanceName ) )
                sm = Root.Instance.GetSceneManager( smInstanceName );
            else
                sm = Root.Instance.CreateSceneManager( smType, smInstanceName );
            this.SceneManager = sm;
            //page strategy name
            string stratName = string.Empty;
            stream.Read( out stratName );
            SetStrategy( stratName );
            //page strategy data
            bool strategyDataOk = mStrategyData.Load( stream );
            if (!strategyDataOk)
                LogManager.Instance.Write( "Error: PageStrategyData for section '{0}' was not loaded correctly, check file contens", mName );

            // Load any data specific to a subtype of this class
            LoadSubtypeData( stream );

            stream.ReadChunkEnd( CHUNK_ID );

            return true;
        }

        /// <summary>
        /// Load data specific to a subtype of this class (if any)
        /// </summary>
        [OgreVersion( 1, 7, 2 )]
        protected virtual void LoadSubtypeData( StreamSerializer stream )
        {
        }

        [OgreVersion( 1, 7, 2 )]
        protected virtual void SaveSubtypeData( StreamSerializer stream )
        {
        }

        /// <summary>
        /// Save this section to a stream
        /// </summary>
        [OgreVersion( 1, 7, 2 )]
        public virtual void Save( StreamSerializer stream )
        {
            stream.WriteChunkBegin( CHUNK_ID, CHUNK_VERSION );

            //name 
            stream.Write( mName );
            //AABB
            stream.Write( mAABB );
            // SceneManager type & name
            stream.Write( mSceneMgr.TypeName );
            stream.Write( mSceneMgr.Name );
            //page strategy name
            stream.Write( mStrategy.Name );
            //page strategy data
            mStrategyData.Save( stream );

            // Save any data specific to a subtype of this class
            SaveSubtypeData( stream );

            stream.WriteChunkEnd( CHUNK_ID );

            // save all pages (in separate files)
            foreach ( var i in mPages )
                i.Value.Save();
        }

        /// <summary>
        /// Get the page ID for a given world position. */
        /// </summary>
        [OgreVersion( 1, 7, 2 )]
        public virtual PageID GetPageID( Vector3 worldPos )
        {
            return mStrategy.GetPageID( worldPos, this );
        }

        /// <summary>
        /// Load or create a page against this section covering the given world 
        ///	space position. 
        /// </summary>
        /// <remarks>
        /// This method is designed mainly for editors - it will try to load
        /// an existing page if there is one, otherwise it will create a new one
        /// synchronously.
        /// </remarks>
        [OgreVersion( 1, 7, 2 )]
        public virtual Page LoadOrCreatePage( Vector3 worldPos )
        {
            PageID id = GetPageID( worldPos );
            // this will create a Page instance no matter what, even if load fails
            // we force the load attempt to happen immediately (forceSynchronous)
            LoadPage( id, true );
            return GetPage( id );
        }

        /// <summary>
        /// Ask for a page to be loaded with the given (section-relative) PageID
        /// </summary>
        /// <remarks>
        /// You would not normally call this manually, the PageStrategy is in 
        /// charge of it usually.
        /// If this page is already loaded, this request will not load it again.
        /// If the page needs loading, then it may be an asynchronous process depending
        /// on whether threading is enabled.
        /// </remarks>
        /// <param name="pageID">The page ID to load</param>
        public void LoadPage( PageID pageID )
        {
            LoadPage( pageID, false );
        }

        /// <summary>
        /// Ask for a page to be loaded with the given (section-relative) PageID
        /// </summary>
        /// <remarks>
        /// You would not normally call this manually, the PageStrategy is in 
        /// charge of it usually.
        /// If this page is already loaded, this request will not load it again.
        /// If the page needs loading, then it may be an asynchronous process depending
        /// on whether threading is enabled.
        /// </remarks>
        /// <param name="pageID">The page ID to load</param>
        /// <param name="forceSynchronous">If true, the page will always be loaded synchronously</param>
        [OgreVersion( 1, 7, 2 )]
        public virtual void LoadPage( PageID pageID, bool forceSynchronous )
        {
            if ( !mParent.Manager.ArePagingOperationsEnabled )
                return;

            if ( !mPages.ContainsKey( pageID ) )
            {
                Page page = new Page( pageID, this );
                page.Load( forceSynchronous );
                mPages.Add( pageID, page );
            }
            else
                mPages[ pageID ].Touch();
        }

        /// <summary>
        /// Ask for a page to be unloaded with the given (section-relative) PageID
        /// </summary>
        /// <remarks>
        /// You would not normally call this manually, the PageStrategy is in 
        /// charge of it usually.
        /// </remarks>
        /// <param name="pageID">The page ID to unload</param>
        /// <param name="forceSynchonous">If true, the page will always be unloaded synchronously</param>
        [OgreVersion( 1, 7, 2 )]
        public virtual void UnloadPage( PageID pageID, bool forceSynchonous )
        {
            if ( !mParent.Manager.ArePagingOperationsEnabled )
                return;

            if ( mPages.ContainsKey( pageID ) )
            {
                Page page = mPages[ pageID ];
                mPages.Remove( pageID );
                page.Unload();
                page.Dispose();
            }
        }

        public void UnloadPage( PageID pageId )
        {
            UnloadPage( pageId, false );
        }

        /// <summary>
        /// Ask for a page to be unloaded with the given (section-relative) PageID
        /// </summary>
        /// <remarks>
        /// You would not normally call this manually, the PageStrategy is in 
        /// charge of it usually.
        /// </remarks>
        /// <param name="page">The Page to unload</param>
        /// <param name="sync">If true, the page will always be unloaded synchronously</param>
        [OgreVersion( 1, 7, 2 )]
        public void UnloadPage( Page page, bool sync )
        {
            UnloadPage( page.PageID, sync );
        }

        public void UnloadPage( Page page )
        {
            UnloadPage( page.PageID, false );
        }

        /// <summary>
        /// Give a section the opportunity to prepare page content procedurally.
        /// </summary>
        /// <remarks>
        /// You should not call this method directly. This call may well happen in 
		/// a separate thread so it should not access GPU resources, use _loadProceduralPage
		/// for that
        /// </remarks>
        /// <returns>true if the page was populated, false otherwise</returns>
        [OgreVersion( 1, 7, 2 )]
        public virtual bool PrepareProcedualePage( Page page )
        {
            bool generated = false;
            if ( mPageProvider != null )
                generated = mPageProvider.PrepareProcedualPage( page, this );
            if ( !generated )
                generated = mParent.PrepareProcedualPage( page, this );

            return generated;
        }

        /// <summary>
        /// Give a section the opportunity to load page content procedurally
        /// </summary>
        /// <remarks>
        /// You should not call this method directly. This call will happen in 
		/// the main render thread so it can access GPU resources. Use _prepareProceduralPage
		/// for background preparation.
        /// </remarks>
        /// <returns>true if the page was populated, false otherwise</returns>
        [OgreVersion( 1, 7, 2 )]
        public virtual bool LoadProcedualPage( Page page )
        {
            bool generated = false;
            if ( mPageProvider != null )
                generated = mPageProvider.LoadProcedualPage( page, this );
            if ( !generated )
                generated = mParent.LoadProcedualPage( page, this );

            return generated;
        }

        /// <summary>
        /// Give a section  the opportunity to unload page content procedurally. 
        /// </summary>
        /// <remarks>
        /// You should not call this method directly. This call will happen in 
		/// the main render thread so it can access GPU resources. Use _unprepareProceduralPage
		/// for background preparation.
        /// </remarks>
        /// <returns>true if the page was populated, false otherwise</returns>
        [OgreVersion( 1, 7, 2 )]
        public virtual bool UnloadProcedualPage( Page page )
        {
            bool generated = false;
            if ( mPageProvider != null )
                generated = mPageProvider.UnloadProcedualPage( page, this );
            if ( !generated )
                generated = mParent.UnloadProcedualPage( page, this );

            return generated;
        }

        /// <summary>
        /// Give a section  the opportunity to unprepare page content procedurally. 
        /// </summary>
        /// <remarks>
        /// You should not call this method directly. This call may well happen in 
        /// a separate thread so it should not access GPU resources, use _unloadProceduralPage
        /// for that
        /// </remarks>
        /// <returns>true if the page was unpopulated, false otherwise</returns>
        [OgreVersion( 1, 7, 2 )]
        public virtual bool UnprepareProcedualPage( Page page )
        {
            bool generated = false;
            if ( mPageProvider != null )
                generated = mPageProvider.UnPrepareProcedualPage( page, this );
            if ( !generated )
                generated = mParent.UnPrepareProcedualPage( page, this );

            return generated;
        }

        /// <summary>
        /// Ask for a page to be kept in memory if it's loaded.
        /// </summary>
        /// <remarks>
        /// This method indicates that a page should be retained if it's already
        /// in memory, but if it's not then it won't trigger a load. This is useful
        /// for retaining pages that have just gone out of range, but which you
        /// don't want to unload just yet because it's quite possible they may come
        /// back into the active set again very quickly / easily. But at the same
        /// time, if they've already been purged you don't want to force them to load. 
        /// This is the 'maybe' region of pages. 
        /// @par
        /// Any Page that is neither requested nor held in a frame will be
        /// deemed a candidate for unloading.
        /// </remarks>
        [OgreVersion( 1, 7, 2 )]
        public virtual void HoldPage( PageID pageID )
        {
            Page page;
            if ( mPages.TryGetValue( pageID, out page ) )
                page.Touch();
        }

        /// <summary>
        /// Retrieves a Page.
        /// </summary>
        /// <remarks>
        /// This method will only return Page instances that are already loaded. It
        /// will return null if a page is not loaded. 
        /// </remarks>
        [OgreVersion( 1, 7, 2 )]
        public virtual Page GetPage( PageID pageID )
        {
            Page page;
            mPages.TryGetValue( pageID, out page );
            return page;
        }

        /// <summary>
        ///  Remove all pages immediately. 
        /// </summary>
        /// <remarks>
        /// Effectively 'resets' this section by deleting all pages. 
        /// </remarks>
        [OgreVersion( 1, 7, 2 )]
        public virtual void RemoveAllPages()
        {
            if ( !mParent.Manager.ArePagingOperationsEnabled )
                return;

            foreach ( var page in mPages.Values )
                page.Dispose();

            mPages.Clear();
        }

        /// <summary>
        /// Called when the frame starts
        /// </summary>
        [OgreVersion( 1, 7, 2 )]
        public virtual void FrameStart( Real timeSinceLastFrame )
        {
            mStrategy.FrameStart( timeSinceLastFrame, this );
            foreach ( var page in mPages.Values )
                page.FrameStart( timeSinceLastFrame );
        }

        /// <summary>
        /// Called when the frame ends
        /// </summary>
        [OgreVersion( 1, 7, 2 )]
        public virtual void FrameEnd( Real timeElapsed )
        {
            mStrategy.FrameEnd( timeElapsed, this );
            PageID[] ids = new PageID[ mPages.Count ];
            mPages.Keys.CopyTo( ids, 0 );

            for ( int i = 0; i < mPages.Count; ++i )
            {
                // if this page wasn't used, unload
                Page p = mPages[ ids[ i ] ];

                if ( !p.IsHeld )
                {
                    UnloadPage( p );

                    // update indices since unloading will invalidate it
                    ids = new PageID[ mPages.Count ];
                    mPages.Keys.CopyTo( ids, 0 );

                    // pre-decrement since unloading will remove it
                    --i;
                }
                else
                    p.FrameEnd( timeElapsed );
            }
        }

        /// <summary>
        /// Notify a section of the current camera
        /// </summary>
        [OgreVersion( 1, 7, 2 )]
        public virtual void NotifyCamerea( Camera cam )
        {
            mStrategy.NotifyCamera( cam, this );
            
            foreach ( var page in mPages.Values )
                page.NotifyCamera( cam );
        }

        /// <summary>
        /// Get a serialiser set up to read Page data for the given PageID. 
        /// </summary>
        /// <remarks>
        /// The StreamSerialiser returned is the responsibility of the caller to
		/// delete. 
        /// </remarks>
        /// <param name="pageID">The ID of the page being requested</param>
        [OgreVersion( 1, 7, 2 )]
        public virtual StreamSerializer ReadPageStream( PageID pageID )
        {
            StreamSerializer stream = null;
            if ( mPageProvider != null )
                stream = mPageProvider.ReadPageStream( pageID, this );
            if ( stream == null )
                stream = mParent.ReadPageStream( pageID, this );

            return stream;
        }

        /// <summary>
        /// Get a serialiser set up to write Page data for the given PageID. 
        /// </summary>
        /// <remarks>
        /// The StreamSerialiser returned is the responsibility of the caller to
		/// delete. 
        /// </remarks>
        /// <param name="pageID">The ID of the page being requested</param>
        [OgreVersion( 1, 7, 2 )]
        public StreamSerializer WritePageStream( PageID pageID )
        {
            StreamSerializer stream = null;
            if ( mPageProvider != null )
                stream = mPageProvider.WritePageStream( pageID, this );
            if ( stream == null )
                stream = mParent.WritePageStream( pageID, this );

            return stream;
        }

        [OgreVersion( 1, 7, 2, "operator <<" )]
        public override string ToString()
        {
            return string.Format( "PagedWorldSection({0}, world:{1})", mName, this.World.Name );
        }
    };

    /// <summary>
    /// A factory class for creating types of world section.
    /// </summary>
    public abstract class PagedWorldSectionFactory
    {
        [OgreVersion( 1, 7, 2 )]
        public abstract string Name { get; }

        [OgreVersion( 1, 7, 2 )]
        public abstract PagedWorldSection CreateInstance( string name, PageWorld parent, SceneManager sm );

        [OgreVersion( 1, 7, 2 )]
        public abstract void DestroyInstance( ref PagedWorldSection p );
    };
}
