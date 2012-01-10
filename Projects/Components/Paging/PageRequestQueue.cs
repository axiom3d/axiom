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

using Axiom.Collections;
using Axiom.Core;
using Axiom.Serialization;

#endregion Namespace Declarations

namespace Axiom.Components.Paging
{
    public class PageRequestQueue
    {
        #region - enums, structs -
        /// <summary>
        /// The request type for a request - listed in general lifecycle order
        /// </summary>
        protected enum RequestType
        {
            PreparePage = 0,
            LoadPage = 1,
            UnloadPage = 2,
            UnpreparePage = 3,
            DeletePage = 4,
        }
        /// <summary>
        /// 
        /// </summary>
        protected struct Request
        {
            public RequestType RequestType;
            public Page Page;
            public PagedWorldSection Section;

            public Request(RequestType rt, Page p, PagedWorldSection s)
            {
                RequestType = rt;
                Page = p;
                Section = s;
            }
        }
        #endregion

        #region - fields -
        /// <summary>
        /// 
        /// </summary>
        protected PageManager mPageManager;
        /// <summary>
        /// Enable this option if you want to force synchronous loading of all 
        ///	future requests.
        /// </summary>
        protected bool mForceSynchronous;
        /// <summary>
        /// Set the amount of time the render thread is allowed to spend on pending requests.
        /// </summary>
        protected uint mRenderThreadTimeLimit;
        /// <summary>
        /// Requests pending for the background queue
        /// </summary>
        protected Deque<Request> mBackgroundQueue = new Deque<Request>();
        /// <summary>
        /// Requests pending for the render queue (follow on from background)
        /// </summary>
        protected Deque<Request> mRenderQueue = new Deque<Request>();
        #endregion

        #region - properties -
        /// <summary>
        /// Set the amount of time the render thread is allowed to spend on pending requests.
        /// </summary>
        public uint RenderThreadTimeLimit
        {
            set { mRenderThreadTimeLimit = value; }
            get { return mRenderThreadTimeLimit; }
        }
        /// <summary>
        /// Enable this option if you want to force synchronous loading of all 
		///	future requests.
        /// </summary>
        public bool ForceSynchronous
        {
            set { mForceSynchronous = value; }
            get { return mForceSynchronous; }
        }
        #endregion

        #region - constructors, destructors -
        /// <summary>
        /// The PageRequestQueue is where pages are queued for loading and freeing.
        /// </summary>
        /// <param name="manager"></param>
        public PageRequestQueue(PageManager manager)
        {
            mPageManager = manager;
            mForceSynchronous = true;
        }
        #endregion
        /// <summary>
        /// To be called in the main render thread each frame
        /// </summary>
        public void ProcessRenderThreadsRequest()
        {
            ITimer timer = Root.Instance.Timer;
            long msStart = timer.Milliseconds;

            while (mRenderQueue.Count > 0)
            {
                // FIFO
#warning TODO lock
                Request r = mRenderQueue.PeekHead();
                mRenderQueue.RemoveFromHead();

                ProcessRenderRequest(r);

#warning check me
                if ((mRenderThreadTimeLimit <= timer.Milliseconds) &&
                    ((msStart + mRenderThreadTimeLimit) <= timer.Milliseconds))
                {
                    //time up!
                    break;
                }
            }
        }
        /// <summary>
        /// Load a Page, for a given PagedWorldSection.
        /// </summary>
        /// <param name="page"></param>
        /// <param name="section"></param>
        public void LoadPage(Page page, PagedWorldSection section)
        {
            LoadPage(page, section, false);
        }
        /// <summary>
        /// Load a Page, for a given PagedWorldSection.
        /// </summary>
        /// <param name="page"></param>
        /// <param name="section"></param>
        /// <param name="forceSync"></param>
        public void LoadPage(Page page, PagedWorldSection section, bool forceSync)
        {
            // Prepare in the background
            Request req = new Request(RequestType.PreparePage, page, section);
            AddBackgroundRequest(req, forceSync);

            // load will happen in the main thread once preparation is complete
        }
        /// <summary>
        /// Dispose of a page
        /// </summary>
        /// <param name="page"></param>
        /// <param name="section"></param>
        /// <param name="forceSync"></param>
        public void UnLoadPage(Page page, PagedWorldSection section, bool forceSync)
        {
            // unload in main thread, then unprepare in background
            Request req = new Request(RequestType.UnloadPage, page, section);
            AddRenderRequest(req, forceSync);
        }
        /// <summary>
        /// Cancel any pending operations for a Page.
        /// </summary>
        /// <param name="page"></param>
        public void CancelOperationsForPage(Page page)
        {
            // cancel background
            {
#warning TODO: lock background queue
                Request[] queue = mBackgroundQueue.ToArray();
                foreach (Request r in queue)
                {
                    if (r.Page == page)
                    {
                        mBackgroundQueue.Remove(r);
                    }
                }
            }

            //cancel render
            {
#warning TODO: render background queue
                Request[] queue = mRenderQueue.ToArray();
                foreach (Request r in queue)
                {
                    if (r.Page == page)
                    {
                        mRenderQueue.Remove(r);
                    }
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="r"></param>
        protected void AddBackgroundRequest(Request r)
        {
            AddBackgroundRequest(r, false);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="r"></param>
        /// <param name="forceSync"></param>
        protected void AddBackgroundRequest(Request r, bool forceSync)
        {
            Log log = LogManager.Instance.DefaultLog;

            if (log.LogDetail == LoggingLevel.Verbose)
            {
                log.Write(LogMessageLevel.Trivial, false, "PageRequestQueue: queueing background thread request " + r.RequestType +
                    " for page ID " + r.Page.PageID.Value + " world " + r.Section.World.Name + " : " +
                    r.Section.Name, null);
            }

            if (mForceSynchronous || forceSync)
            {
                ProcessBackgroundRequest(r);
            }
            else
            {
#warning TODO: lock
                mBackgroundQueue.Add(r);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="r"></param>
        protected void AddRenderRequest(Request r)
        {
            AddRenderRequest(r, false);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="r"></param>
        /// <param name="forceSync"></param>
        protected void AddRenderRequest(Request r, bool forceSync)
        {
            Log log = LogManager.Instance.DefaultLog;

            if (log.LogDetail == LoggingLevel.Verbose)
            {
                log.Write(LogMessageLevel.Trivial, false, "PageRequestQueue: queueing render thread request " + r.RequestType +
                    " for page ID " + r.Page.PageID.Value + " world " + r.Section.World.Name + " : " +
                        r.Section.Name, null);
            }

            if (mForceSynchronous || forceSync)
            {
                ProcessRenderRequest(r);
            }
            else
            {
#warning TODO: lock
                mRenderQueue.Add(r);
            }
        }
        /// <summary>
        /// Process the background portion of a request (may be threaded)
        /// </summary>
        /// <param name="r"></param>
        protected void ProcessBackgroundRequest(Request r)
        {
            Log log = LogManager.Instance.DefaultLog;

            if (log.LogDetail == LoggingLevel.Verbose)
            {
                log.Write(LogMessageLevel.Trivial, false, "PageRequestQueue: processing background thread request " + r.RequestType +
                    " for page ID " + r.Page.PageID.Value + " world " + r.Section.World.Name + " : " +
                        r.Section.Name, null);
            }

            try
            {
                switch (r.RequestType)
                {
                    case RequestType.PreparePage:
                        {
                            // Allow procedural generation
                            if (r.Section.PrepareProcedualePage(r.Page))
                            {
                                r.Page.ChangeStatus(UnitStatus.Unloaded, UnitStatus.Prepared);
                            }
                            else
                            {
                                StreamSerializer ser = r.Section.ReadPageStream(r.Page.PageID);
                                r.Page.Prepare(ser);
                                ser = null;
                            }
                            // Pass back to render thread to finalise
                            Request req = new Request(RequestType.LoadPage, r.Page, r.Section);
                            AddRenderRequest(req);

                        }
                        break;
                    case RequestType.UnpreparePage:
                        {
                            // Allow procedural generation
                            if (r.Section.UnprepareProcedualPage(r.Page))
                            {
                                r.Page.ChangeStatus(UnitStatus.Prepared, UnitStatus.Unloaded);
                            }
                            else
                            {
                                r.Page.UnPrepare();
                            }
                            // Pass back to render thread to finalise
                            Request req = new Request(RequestType.DeletePage, r.Page, r.Section);
                            AddRenderRequest(req);
                        }
                        break;
                }
            }
            catch (Exception e)
            {
                LogManager.Instance.Write("Error processing background request: " +
                    e.Message);
            }
        }
        /// <summary>
        /// Process the render portion of a request (may be threaded)
        /// </summary>
        /// <param name="r"></param>
        protected void ProcessRenderRequest(Request r)
        {
            Log log = LogManager.Instance.DefaultLog;

            if (log.LogDetail == LoggingLevel.Verbose)
            {
                log.Write(LogMessageLevel.Trivial, false, "PageRequestQueue: processing render thread request " + r.RequestType +
                    " for page ID " + r.Page.PageID.Value + " world " + r.Section.World.Name + " : " +
                        r.Section.Name, null);
            }

            try
            {
                switch (r.RequestType)
                {
                    case RequestType.LoadPage:
                        {
                            // Allow procedural generation
                            if (r.Section.LoadProcedualPage(r.Page))
                            {
                                r.Page.ChangeStatus(UnitStatus.Prepared, UnitStatus.Loaded);
                            }
                            else
                            {
                                r.Page.Load();
                            }
                        }
                        break;
                    case RequestType.UnloadPage:
                        {
                            // Allow procedural generation
                            if (r.Section.UnloadProcedualPage(r.Page))
                            {
                                r.Page.ChangeStatus(UnitStatus.Loaded, UnitStatus.Prepared);
                            }
                            else
                            {
                                r.Page.Unload();
                            }
                            // Pass back to render thread to finalise
                            Request req = new Request(RequestType.UnpreparePage, r.Page, r.Section);
                            AddBackgroundRequest(req);
                        }
                        break;
                    case RequestType.DeletePage:
                        r.Page = null;
                        break;
                }
            }
            catch (Exception e)
            {
                LogManager.Instance.Write("Error processing render request: " +
                    e.Message);
            }
        }
    }
}
