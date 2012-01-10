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
using Axiom.Serialization;

#endregion Namespace Declarations

namespace Axiom.Components.Paging
{
    /// <summary>
    /// Represents a section of the PagedWorld which uses a given PageStrategy, and
	/// which is made up of a generally localised set of Page instances.
    /// </summary>
    public class PagedWorldSection
    {
        #region - constants -
        public static uint CHUNK_ID = StreamSerializer.MakeIdentifier("PWSC");
        public static ushort CHUNK_VERSION = 1;
        #endregion

        #region - fields -
        /// <summary>
        /// 
        /// </summary>
        protected Dictionary<PageID, Page> mPages = new Dictionary<PageID, Page>();
        /// <summary>
        /// 
        /// </summary>
        protected string mName;
        /// <summary>
        /// 
        /// </summary>
        protected AxisAlignedBox mAABB;
        /// <summary>
        /// 
        /// </summary>
        protected PageWorld mParent;
        /// <summary>
        /// 
        /// </summary>
        protected PageStrategy mStrategy;
        /// <summary>
        /// 
        /// </summary>
        protected IPageStrategyData mStrategyData;
        /// <summary>
        /// 
        /// </summary>
        protected PageProvider mPageProvider;
        /// <summary>
        /// 
        /// </summary>
        protected SceneManager mSceneMgr;
        #endregion

        #region - properties -
        /// <summary>
        /// 
        /// </summary>
        public virtual IPageStrategyData StrategyData
        {
            get { return mStrategyData; }
        }
        /// <summary>
        /// 
        /// </summary>
        public virtual string Name
        {
            get { return mName; }
            set { mName = value; }
        }
        /// <summary>
        /// 
        /// </summary>
        public virtual PageStrategy Strategy
        {
            get { return mStrategy; }
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
        /// <summary>
        /// 
        /// </summary>
        public virtual PageManager Manager
        {
            get { throw new NotImplementedException(); }
        }
        /// <summary>
        /// 
        /// </summary>
        public virtual PageWorld World
        {
            get { return mParent; }
        }
        /// <summary>
        /// 
        /// </summary>
        public virtual AxisAlignedBox BoundingBox
        {
            set { mAABB = value; }
            get { return mAABB; }
        }
        /// <summary>
        /// Get's the current scene manager.
        /// </summary>
        public virtual SceneManager SceneManager
        {
            get { return mSceneMgr; }
        }
        /// <summary>
        /// 
        /// </summary>
        public PageProvider PageProvider
        {
            set { mPageProvider = value; }
            get { return mPageProvider; }
        }
        #endregion

        #region - constructor, destructor -
        /// <summary>
        /// Construct a new instance, specifying just the parent (expecting to load). 
        /// </summary>
        /// <param name="parent"></param>
        public PagedWorldSection(PageWorld parent)
        {
            mParent = parent;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="parent"></param>
        /// <param name="strategy"></param>
        public PagedWorldSection(string name, PageWorld parent, PageStrategy strategy,
            SceneManager sm)
        {
            mName = name;
            mParent = parent;
            Strategy = strategy;
            mSceneMgr = sm;
        }
        /// <summary>
        /// Destrouctor.
        /// </summary>
        ~PagedWorldSection()
        {
            if (mStrategy != null)
            {
                mStrategy.DestroyData(mStrategyData);
                mStrategyData = null;
            }

            RemoveAllPages();
        }
        #endregion
        /// <summary>
        /// 
        /// </summary>
        /// <param name="stratName"></param>
        public virtual void SetStrategy(string stratName)
        {
            Strategy = Manager.GetStrategy(stratName);
        }
        /// <summary>
        /// Change the SceneManager.
        /// </summary>
        /// <param name="sm">The instance of the SceneManager</param>
        public virtual void SetSceneManager(SceneManager sm)
        {
            if (sm != mSceneMgr)
            {
                mSceneMgr = sm;
                RemoveAllPages();
            }
        }
        /// <summary>
        /// Change the SceneManager.
        /// </summary>
        /// <param name="smName">The instance name of the SceneManager</param>
        public virtual void SetSceneManager(string smName)
        {
            SetSceneManager(Root.Instance.GetSceneManager(smName));
        }
        /// <summary>
        /// Load this section from a stream (returns true if successful)
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public virtual bool Load(StreamSerializer stream)
        {
            if (stream.ReadChunkBegin(CHUNK_ID, CHUNK_VERSION, "PagedWorldSection") == null)
                return false;

            //name
            stream.Read(out mName);
            // AABB
            stream.Read(out mAABB);
            //page strategy name
            string stratName = string.Empty;
            stream.Read(out stratName);
            SetStrategy(stratName);
            //page strategy data
            bool strategyDataOk = mStrategyData.Load(stream);
            if (!strategyDataOk)
                LogManager.Instance.Write("Error: PageStrategyData for section '" +
                    mName + "' was not loaded correctly, check file contens");

            stream.ReadChunkEnd(CHUNK_ID);

            return true;
        }
        /// <summary>
        /// Save this section to a stream
        /// </summary>
        /// <param name="stream"></param>
        public virtual void Save(StreamSerializer stream)
        {
            stream.WriteChunkBegin(CHUNK_ID, CHUNK_VERSION);

            //name 
            stream.Write(mName);
            //AABB
            stream.Write(mAABB);
            //page strategy name
            stream.Write(mStrategy.Name);
            //page strategy data
            mStrategyData.Save(stream);

            //save all pages
#warning TODO: save all pages.

            stream.WriteChunkEnd(CHUNK_ID);
        }

        /// <summary>
        /// Called when the frame starts
        /// </summary>
        /// <param name="timeSinceLastFrame"></param>
        public virtual void FrameStart(float timeSinceLastFrame)
        {
            mStrategy.FrameStart(timeSinceLastFrame, this);
            foreach (Page page in mPages.Values)
                page.FrameStart(timeSinceLastFrame);
        }
        /// <summary>
        /// Called when the frame ends
        /// </summary>
        /// <param name="timeElapsed"></param>
        public virtual void FrameEnd(float timeElapsed)
        {
            mStrategy.FrameEnd(timeElapsed, this);

            ///copy to temporary array, to avoid exception
            Page[] temp = new Page[mPages.Count];
            mPages.Values.CopyTo(temp, 0);
            for (int i = 0; i < temp.Length; i++)
            {
                if (!temp[i].IsHeld)
                    UnloadPage(temp[i].PageID);
                else
                    temp[i].FrameEnd(timeElapsed);
            }
        }
        /// <summary>
        /// Notify a section of the current camera
        /// </summary>
        /// <param name="cam"></param>
        public virtual void NotifyCamerea(Camera cam)
        {
            mStrategy.NotifyCamera(cam, this);
            foreach (Page page in mPages.Values)
                page.NotifyCamera(cam);
        }

        /// <summary>
        /// Load or create a page against this section covering the given world 
		///	space position. 
        /// </summary>
        /// <param name="worldPos"></param>
        /// <returns></returns>
        public virtual Page LoadOrCreatePage(Vector3 worldPos)
        {
            PageID id = GetPageID(worldPos);
            // this will create a Page instance no matter what, even if load fails
            // we force the load attempt to happen immediately (forceSynchronous)
            LoadPage(id, true);
            return GetPage(id);
        }
        /// <summary>
        /// Get the page ID for a given world position.
        /// </summary>
        /// <param name="worldPos"></param>
        /// <returns></returns>
        public virtual PageID GetPageID(Vector3 worldPos)
        {
            return mStrategy.GetPageID(worldPos, this);
        }
        /// <summary>
        /// Ask for a page to be loaded with the given (section-relative) PageID
        /// </summary>
        /// <param name="pageID">The page ID to load</param>
        public void LoadPage(PageID pageID)
        {
            LoadPage(pageID, false);
        }
        /// <summary>
        /// Ask for a page to be loaded with the given (section-relative) PageID
        /// </summary>
        /// <param name="pageID">The page ID to load</param>
        /// <param name="forceSynchronous">If true, the page will always be loaded synchronously</param>
        public virtual void LoadPage(PageID pageID, bool forceSynchronous)
        {
            Page page;
            if (!mPages.TryGetValue(pageID, out page))
            {
                page = new Page(pageID);
                // attach page immediately, but notice that it's not loaded yet
                AttachPage(page);
                Manager.Queue.LoadPage(page, this, forceSynchronous);
            }
            else
                page.Touch();
        }
        /// <summary>
        /// Ask for a page to be unloaded with the given (section-relative) PageID
        /// </summary>
        /// <param name="pageId">The page ID to unload</param>
        public void UnloadPage(PageID pageId)
        {
            UnloadPage(pageId, false);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="page"></param>
        /// <param name="sync"></param>
        public void UnloadPage(Page page, bool sync)
        {
            UnloadPage(page.PageID, sync);
        }
        /// <summary>
        /// Ask for a page to be unloaded with the given (section-relative) PageID
        /// </summary>
        /// <param name="pageID">The page ID to unload</param>
        /// <param name="forceSynchonous">If true, the page will always be unloaded synchronously</param>
        public virtual void UnloadPage(PageID pageID, bool forceSynchonous)
        {
            Page page;
            if (mPages.TryGetValue(pageID, out page))
            {
                mPages.Remove(pageID);
                page.NotifyAttached(null);
                Manager.Queue.UnLoadPage(page, this, forceSynchonous);
            }
        }
        /// <summary>
        /// Give a section the opportunity to prepare page content procedurally.
        /// </summary>
        /// <param name="page"></param>
        /// <returns>true if the page was populated, false otherwise</returns>
        public virtual bool PrepareProcedualePage(Page page)
        {
            bool generated = false;
            if (mPageProvider != null)
                generated = mPageProvider.PrepareProcedualPage(page, this);
            if (!generated)
                generated = mParent.PrepareProcedualPage(page, this);

            return generated;
        }
        /// <summary>
        /// Give a section the opportunity to load page content procedurally
        /// </summary>
        /// <param name="page"></param>
        /// <returns>true if the page was populated, false otherwise</returns>
        public virtual bool LoadProcedualPage(Page page)
        {
            bool generated = false;
            if (mPageProvider != null)
                generated = mPageProvider.LoadProcedualPage(page, this);
            if (!generated)
                generated = mParent.LoadProcedualPage(page, this);

            return generated;
        }
        /// <summary>
        /// Give a section  the opportunity to unload page content procedurally. 
        /// </summary>
        /// <param name="page"></param>
        /// <returns></returns>
        public virtual bool UnloadProcedualPage(Page page)
        {
            bool generated = false;
            if (mPageProvider != null)
                generated = mPageProvider.UnloadProcedualPage(page, this);
            if (!generated)
                generated = mPageProvider.UnloadProcedualPage(page, this);

            return generated;
        }
        /// <summary>
        /// Give a section  the opportunity to unprepare page content procedurally. 
        /// </summary>
        /// <remarks>
        /// This method indicates that a page should be retained if it's already
		///	in memory, but if it's not then it won't trigger a load. This is useful
		///	for retaining pages that have just gone out of range, but which you
		///	don't want to unload just yet because it's quite possible they may come
		///	back into the active set again very quickly / easily. But at the same
		///	time, if they've already been purged you don't want to force them to load. 
		///	This is the 'maybe' region of pages.
        /// </remarks>
        /// <param name="page"></param>
        /// <returns></returns>
        public virtual bool UnprepareProcedualPage(Page page)
        {
            bool generated = false;
            if (mPageProvider != null)
                generated = mPageProvider.UnPrepareProcedualPage(page, this);
            if (!generated)
                generated = mParent.UnPrepareProcedualPage(page, this);

            return generated;
        }
        /// <summary>
        /// Ask for a page to be kept in memory if it's loaded.
        /// </summary>
        /// <param name="pageID"></param>
        public virtual void HoldPage(PageID pageID)
        {
            Page page;
            if (mPages.TryGetValue(pageID, out page))
                page.Touch();
        }
        /// <summary>
        /// Retrieves a Page.
        /// </summary>
        /// <param name="pageID"></param>
        /// <returns></returns>
        public virtual Page GetPage(PageID pageID)
        {
            Page page;
            mPages.TryGetValue(pageID, out page);
            return page;
        }
        /// <summary>
        /// Attach a page to this section. 
        /// </summary>
        /// <param name="page"></param>
        public virtual void AttachPage(Page page)
        {
            if (mPages.ContainsKey(page.PageID))
            {
                //page with this id allready in map
                Page existingPage;
                mPages.TryGetValue(page.PageID,out existingPage);

                if (existingPage != page)
                {
                    Manager.Queue.CancelOperationsForPage(existingPage);
                    mPages.Remove(existingPage.PageID);
                    existingPage = null;
                }
            }
            page.NotifyAttached(this);
        }
        /// <summary>
        /// Detach a page to this section. 
        /// </summary>
        /// <param name="page"></param>
        public virtual void DetachPage(Page page)
        {
            Page pageFound;
            if (mPages.TryGetValue(page.PageID, out pageFound))
            {
                if (page != pageFound)
                {
                    mPages.Remove(pageFound.PageID);
                    page.NotifyAttached(null);
                }
            }
        }
        /// <summary>
        ///  Remove all pages immediately. 
        /// </summary>
        public virtual void RemoveAllPages()
        {
            foreach (Page page in mPages.Values)
            {
                Manager.Queue.CancelOperationsForPage(page);
            }
            mPages.Clear();
        }
        /// <summary>
        /// Get a serialiser set up to read Page data for the given PageID. 
        /// </summary>
        /// <param name="pageID"></param>
        /// <returns></returns>
        public StreamSerializer ReadPageStream(PageID pageID)
        {
            StreamSerializer stream = null;
            if (mPageProvider != null)
                stream = mPageProvider.ReadPageStream(pageID, this);
            if (stream == null)
                stream = mParent.ReadPageStream(pageID, this);

            return stream;
        }
        /// <summary>
        /// Get a serialiser set up to write Page data for the given PageID. 
        /// </summary>
        /// <param name="pageID"></param>
        public StreamSerializer WritePageStream(PageID pageID)
        {
            StreamSerializer stream = null;
            if (mPageProvider != null)
                stream = mPageProvider.WritePageStream(pageID, this);
            if (stream == null)
                stream = mParent.WritePageStream(pageID, this);

            return stream;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "PagedWorldSection(" + this.Name + ", world:" + this.World.Name + ")";
        }
    }
}
