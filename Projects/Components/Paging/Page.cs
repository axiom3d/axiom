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
    public class Page : PageLoadableUnit
    {
        public static uint CHUNK_ID = StreamSerializer.MakeIdentifier("PAGE");
        public static ushort CHUNK_VERSION = 1;
        protected PageID mID;
        protected PagedWorldSection mParent;
        protected int mFrameLastHeld;
        protected List<PageContentCollection> mContentCollections = new List<PageContentCollection>();
        protected SceneNode mDebugNode;
        protected bool mDeferredProcessInProgress;

        /// <summary>
        /// Get the ID of this page, unique withing the parent
        /// </summary>
        public virtual PageID PageID
        {
            get { return mID; }
        }
        /// <summary>
        ///  Get the PagedWorldSection this page belongs to, or zero if unattached
        /// </summary>
        public virtual PagedWorldSection ParentSection
        {
            get { return mParent; }
        }
        /// <summary>
        /// Get the frame number in which this Page was last loaded or held.
        /// </summary>
        /// <remarks>
        /// A Page that has not been requested to be loaded or held in the recent
		///	past will be a candidate for removal. 
        /// </remarks>
        public virtual int FrameLastHeld
        {
            get { return mFrameLastHeld; }
        }
        /// <summary>
        /// Get whether or not this page is currently attached 
        /// </summary>
        public virtual bool IsAttached
        {
            get { return mParent != null; }
        }
        /// <summary>
        /// 
        /// </summary>
        public PageManager Manager
        {
            get { return mParent.Manager; }
        }
        /// <summary>
        /// 
        /// </summary>
        public int ContentCollectionCount
        {
            get { return mContentCollections.Count; }
        }
        /// <summary>
        /// 
        /// </summary>
        public List<PageContentCollection> ContentCollectionList
        {
            get { return mContentCollections; }
        }
        /// <summary>
        /// 
        /// </summary>
        public bool IsHeld
        {
            get
            {
#warning implement nextFrameNumber;
                uint nextFrame = 1;// Root.Instance.NextFrameNumber;
                // 1-frame tolerance, since the next frame number varies depending which
                // side of frameRenderingQueued you are
                return (mFrameLastHeld == nextFrame ||
                    mFrameLastHeld + 1 == nextFrame);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        public SceneManager SceneManager
        {
            get { return mParent.SceneManager; }
        }

        /// <summary>
        /// If true, it's not safe to access this Page at this time, contents may be changing
        /// </summary>
        public bool IsDeferredProcessInProgress
        {
            [OgreVersion( 1, 7, 2 )]
            get
            {
                return mDeferredProcessInProgress;
            }
        }

        #region - constructor -
        /// <summary>
        /// Page class
        /// </summary>
        public Page( PageID pageID, PagedWorldSection parent )
            : base()
        {
            mID = pageID;
            mParent = parent;

#if WORKQUEUE_IMPLEMENTED
		    WorkQueue* wq = Root::getSingleton().getWorkQueue();
		    mWorkQueueChannel = wq->getChannel("Axiom/Page");
		    wq->addRequestHandler(mWorkQueueChannel, this);
		    wq->addResponseHandler(mWorkQueueChannel, this);
#endif
            Touch();
        }

        #endregion - constructor -

        [OgreVersion( 1, 7, 2, "~Page" )]
        protected override void dispose( bool disposeManagedResources )
        {
            if ( !this.IsDisposed )
            {
                if ( disposeManagedResources )
                {
#if WORKQUEUE_IMPLEMENTED
                    WorkQueue* wq = Root::getSingleton().getWorkQueue();
		            wq->removeRequestHandler(mWorkQueueChannel, this);
		            wq->removeResponseHandler(mWorkQueueChannel, this);
#endif
                    DestroyAllContentCollections();

                    if ( mDebugNode != null )
                    {
                        // destroy while we have the chance
                        for ( int i = 0; i < mDebugNode.ObjectCount; ++i )
                            mParent.SceneManager.DestroyMovableObject( mDebugNode.GetObject( i ) );

                        mDebugNode.RemoveAndDestroyAllChildren();
                        mParent.SceneManager.DestroySceneNode( mDebugNode );

                        mDebugNode = null;
                    }
                }
            }
            
            base.dispose( disposeManagedResources );
        }

        /// <summary>
        /// Destroy all PageContentCollections within this page.
        /// </summary>
        [OgreVersion( 1, 7, 2 )]
        public virtual void DestroyAllContentCollections()
        {
            foreach ( var i in mContentCollections )
            {
                if ( !i.IsDisposed )
                    i.Dispose();
            }
            mContentCollections.Clear();
        }

        /// <summary>
        /// 'Touch' the page to let it know it's being used
        /// </summary>
        [OgreVersion( 1, 7, 2 )]
        public void Touch()
        {
            mFrameLastHeld = Root.Instance.NextFrameNumber;
        }

        /// <summary>
        /// Internal method to notify a page that it is attached
        /// </summary>
        /// <param name="parent"></param>
        public void NotifyAttached(PagedWorldSection parent)
        {
            if (parent == null && mParent != null && mDebugNode != null)
            {
#warning unsure if movableobject is correct here
                // destroy while we have the chance
                List<MovableObject> nodes = (List<MovableObject>)mDebugNode.Objects;
                foreach (MovableObject m in nodes)
                    mParent.SceneManager.DestroyMovableObject(m);

                mDebugNode.RemoveAndDestroyAllChildren();
                mParent.SceneManager.DestroySceneNode(mDebugNode.Name);
                mDebugNode = null;
            }

            mParent = parent;

        }
        /// <summary>
        /// Notify a section of the current camera
        /// </summary>
        /// <param name="cam"></param>
        public void NotifyCamera(Camera cam)
        {
            foreach (PageContentCollection coll in mContentCollections)
            {
                coll.NotifyCamera(cam);
            }
        }

        [OgreVersion( 1, 7, 2 )]
        public override void Load( bool synchronous )
        {
            if ( !mDeferredProcessInProgress )
            {
                DestroyAllContentCollections();
                mDeferredProcessInProgress = true;
#if WORKQUEUE_IMPLEMENTED
                PageRequest req(this);
			    Root::getSingleton().getWorkQueue()->addRequest(mWorkQueueChannel, WORKQUEUE_PREPARE_REQUEST, 
				    Any(req), 0, synchronous);
#endif
            }
        }

        public virtual void Save(StreamSerializer stream)
        {
            stream.WriteChunkBegin(CHUNK_ID, CHUNK_VERSION);

            //page id
            stream.Write(mID.Value);

            //content collections
            foreach (PageContentCollection coll in mContentCollections)
            {
                coll.Save(stream);
            }

            stream.WriteChunkEnd(CHUNK_ID);
        }

        /// <summary>
        /// Save page data to an automatically generated file name
        /// </summary>
        public virtual void Save()
        {
            throw new System.NotImplementedException();
            //String filename = generateFilename();
            //save( filename );
        }

        /// <summary>
        /// Called when the frame starts
        /// </summary>
        /// <param name="timeSinceLastFrame"></param>
        public virtual void FrameStart(Real timeSinceLastFrame)
        {
            UpdateDebugDisplay();

            foreach (PageContentCollection coll in mContentCollections)
            {
                coll.FrameStart(timeSinceLastFrame);
            }
        }
        /// <summary>
        /// Called when the frame ends
        /// </summary>
        /// <param name="timeElapsed"></param>
        public virtual void FrameEnd(float timeElapsed)
        {
            foreach (PageContentCollection coll in mContentCollections)
            {
                coll.FrameEnd(timeElapsed);
            }
        }
        /// <summary>
        /// Create a new PageContentCollection within this page.
		/// This is equivalent to calling PageManager.CreateContentCollection and 
		/// then attachContentCollection.
        /// </summary>
        /// <param name="typeName">The name of the type of content collection (see PageManager.GetContentCollectionFactories)</param>
        /// <returns></returns>
        public virtual PageContentCollection CreateContentCollection(string typeName)
        {
            PageContentCollection coll = Manager.CreateContentCollection(typeName);
            AttachContentCollection(coll);
            return coll;
        }
        /// <summary>
        /// Destroy a PageContentCollection within this page.
		/// This is equivalent to calling DetachContentCollection and 
		///	PageManager.DestroyContentCollection.
        /// </summary>
        /// <param name="coll"></param>
        public virtual void DestroyContentCollection(PageContentCollection coll)
        {
            DetachContentCollection(coll);
            Manager.DestroyContentCollection(ref coll);
        }
        
        /// <summary>
        /// Add a content collection to this Page. 
        /// </summary>
        /// <remarks>This class ceases to be responsible for deleting this collection.</remarks>
        /// <param name="coll"></param>
        public virtual void AttachContentCollection(PageContentCollection coll)
        {
            coll.NotifyAttached(this);
            mContentCollections.Add(coll);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="coll"></param>
        public virtual void DetachContentCollection(PageContentCollection coll)
        {
            bool found = false;
            foreach (PageContentCollection i in mContentCollections)
            {
                if (coll == i)
                {
                    coll.NotifyAttached(null);
                    found = true;
                }
            }
            if (found)
                mContentCollections.Remove(coll);
        }
        /// <summary>
        /// Get a content collection
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public virtual PageContentCollection GetContentCollection(int index)
        {
            System.Diagnostics.Debug.Assert(index < mContentCollections.Count);

            return mContentCollections[index];
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        protected override bool PrepareImpl(StreamSerializer stream)
        {
            //now do the real loading
            if (stream.ReadChunkBegin(CHUNK_ID, CHUNK_VERSION, "Page") == null)
                return false;

            // pageID check (we should know the ID we're expecting)
            int storedID = -1;
            stream.Read(out storedID);
            if (mID.Value != storedID)
            {
                LogManager.Instance.Write("Error: Tried to populate Page ID " +
                    mID.Value + " with data corresponding to page ID " + storedID);
                stream.UndoReadChunk(CHUNK_ID);
                return false;
            }

            PageManager mgr = Manager;

            while (stream.NextChunkId == PageContentCollection.CHUNK_ID)
            {
                Chunk collChunk = stream.ReadChunkBegin();
                string factoryName = string.Empty;
                stream.Read(out factoryName);
                //Supported type?
                IPageContentCollectionFactory collFact = mgr.GetContentCollectionFactory(factoryName);
                if (collFact != null)
                {
                    PageContentCollection collInst = collFact.CreateInstance();
                    if (collInst.Prepare(stream))
                    {
                        AttachContentCollection(collInst);
                    }
                    else
                    {
                        LogManager.Instance.Write("Error preparing PageContentCollection type: " +
                            factoryName + " in + " + this.ToString());

                        collFact.DestroyInstance(ref collInst);
                    }
                }
                else
                {
                    LogManager.Instance.Write("Unsupported PageContentCollection type: " +
                            factoryName + " in + " + this.ToString());

                    //skip
                    //stream.ReadChunkEnd(ref collChunk.ID);
                    stream.ReadChunkEnd(collChunk.id);
                }
            }

            return true;
        }
        /// <summary>
        /// 
        /// </summary>
        protected void UpdateDebugDisplay()
        {
            uint dbglvl = Manager.DebugDisplayLevel;
            if (dbglvl > 0)
            {
                if (mDebugNode != null)
                {
                    mDebugNode = mParent.SceneManager.RootSceneNode.CreateChildSceneNode();
                }
                mParent.Strategy.UpdateDebugDisplay(this, mDebugNode);

                mDebugNode.IsVisible = true;
            }
            else if (mDebugNode != null)
            {
                mDebugNode.IsVisible = false;
            }
        }

        public override string ToString()
        {
            return "Page( ID: " + mID.Value + ", seciont: " + ParentSection.Name + ", world: " +
                ParentSection.World.Name + ")";
        }
    }
}
