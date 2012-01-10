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
using System.IO;
using Axiom.Core;
using Axiom.Serialization;

#endregion Namespace Declarations

namespace Axiom.Components.Paging
{
    public class PageWorld
    {
        #region - constanst -
        public static uint CHUNK_ID = StreamSerializer.MakeIdentifier("PWLD");
        public static ushort CHUNK_VERSION = 1;
        #endregion
        #region - fields -
        /// <summary>
        /// 
        /// </summary>
        protected string mName;
        /// <summary>
        /// 
        /// </summary>
        protected PageManager mManger;
        /// <summary>
        /// 
        /// </summary>
        protected PageProvider mPageProvider;
        /// <summary>
        /// 
        /// </summary>
        protected Dictionary<string, PagedWorldSection> mSections = new Dictionary<string, PagedWorldSection>();
        /// <summary>
        /// 
        /// </summary>
        protected NameGenerator<PagedWorldSection> mSectionNameGenerator;
        #endregion

        #region - properties -
        /// <summary>
        /// Set the PageProvider which can provide streams for Pages in this world. 
        /// </summary>
        public PageProvider PageProvider
        {
            set { mPageProvider = value; }
            get { return mPageProvider; }
        }
        /// <summary>
        /// 
        /// </summary>
        public Dictionary<string, PagedWorldSection> Sections
        {
            get
            {
                return mSections;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        public int SectionCount
        {
            get
            {
                return mSections.Count;
            }
        }
        /// <summary>
        /// Get's the name of this world.
        /// </summary>
        public string Name
        {
            get
            {
                return mName;
            }
        }
        /// <summary>
        /// Get's the manager of this world.
        /// </summary>
        public PageManager Manager
        {
            get
            {
                return mManger;
            }
        }
        #endregion

        #region - constructor, destructor -
        /// <summary>
        /// Default Constructor.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="manager"></param>
        public PageWorld(string name, PageManager manager)
        {
            mName = name;
            mManger = manager;
            mSectionNameGenerator = new NameGenerator<PagedWorldSection>("Section");
        }
        /// <summary>
        /// Destructor.
        /// </summary>
        ~PageWorld()
        {
        }
        #endregion
        /// <summary>
        /// Load world data from a file
        /// </summary>
        /// <param name="fileName"></param>
        public void Load(string fileName)
        {
            StreamSerializer stream = mManger.ReadWorldStream(fileName);
            Load(stream);
            stream = null;
        }
        /// <summary>
        /// Load world data from a stream
        /// </summary>
        /// <param name="stream"></param>
        public void Load(Stream stream)
        {
            StreamSerializer ser = new StreamSerializer(stream);
            Load(ser);
        }
        /// <summary>
        /// Load world data from a serialiser (returns true if successful)
        /// </summary>
        /// <param name="stream"></param>
        public bool Load(StreamSerializer stream)
        {
            if (stream.ReadChunkBegin(CHUNK_ID, CHUNK_VERSION, "PageWorld") == null)
                return false;

            //name
            stream.Read(out mName);
            //sections
            while (stream.NextChunkId == PagedWorldSection.CHUNK_ID)
            {
                PagedWorldSection sec = new PagedWorldSection(this);
                bool sectionOk = sec.Load(stream);
                if (sectionOk)
                    mSections.Add(sec.Name, sec);
                else
                {
                    sec = null;
                    break;
                }
            }

            stream.ReadChunkEnd(CHUNK_ID);

            return true;
        }
        /// <summary>
        /// Save world data to a file
        /// </summary>
        /// <param name="fileName"></param>
        public void Save(string fileName)
        {
            StreamSerializer stream = mManger.WriteWorldStream(fileName);
            Save(stream);
            stream = null;
        }
        /// <summary>
        /// Save world data to a stream
        /// </summary>
        /// <param name="stream"></param>
        public void Save(Stream stream)
        {
            StreamSerializer ser = new StreamSerializer(stream);
            Save(ser);
        }
        /// <summary>
        /// Save world data to a serialiser
        /// </summary>
        /// <param name="stream"></param>
        public void Save(StreamSerializer stream)
        {
            stream.WriteChunkBegin(CHUNK_ID, CHUNK_VERSION);

            //name
            stream.Write(mName);
            //sections
            foreach (PagedWorldSection section in mSections.Values)
                section.Save(stream);

            stream.WriteChunkEnd(CHUNK_ID);
        }
        /// <summary>
        /// Create a new section of the world.
        /// </summary>
        /// <param name="strategyName"></param>
        /// <param name="sceneMgr"></param>
        /// <returns></returns>
        PagedWorldSection CreateSection(string strategyName, SceneManager sceneMgr)
        {
            return CreateSection(strategyName, sceneMgr, string.Empty);
        }
        /// <summary>
        /// Create a new section of the world.
        /// </summary>
        /// <param name="strategyName"></param>
        /// <param name="sceneMgr"></param>
        /// <param name="sectionName"></param>
        /// <returns></returns>
        PagedWorldSection CreateSection(string strategyName, SceneManager sceneMgr,
            string sectionName)
        {
            //get the strategy
            PageStrategy strategy = mManger.GetStrategy(strategyName);

            return CreateSection(strategy, sceneMgr, sectionName);
        }
        /// <summary>
        /// Create a new section of the world.
        /// </summary>
        /// <param name="strategyName"></param>
        /// <param name="sceneMgr"></param>
        /// <returns></returns>
        PagedWorldSection CreateSection(PageStrategy strategy, SceneManager sceneMgr)
        {
            return CreateSection(strategy, sceneMgr, string.Empty);
        }
        /// <summary>
        /// Create a new section of the world.
        /// </summary>
        /// <param name="strategyName"></param>
        /// <param name="sceneMgr"></param>
        /// <param name="sectionName"></param>
        /// <returns></returns>
        PagedWorldSection CreateSection(PageStrategy strategy, SceneManager sceneMgr,
            string sectionName)
        {
            string theName = sectionName;
            if (theName == string.Empty)
            {
                do
                {
                    theName = mSectionNameGenerator.GetNextUniqueName();
                }
                while (!mSections.ContainsKey(theName));
            }
            else if (mSections.ContainsKey(theName))
            {
                throw new Exception("World section named '" + theName + "' allready exists!" + 
                "[PageWorld.CreateSection]");
            }

            PagedWorldSection ret = new PagedWorldSection(theName, this, strategy, sceneMgr);
            mSections.Add(theName, ret);
            return ret;
        }
        /// <summary>
        /// Destroy a section of world.
        /// </summary>
        /// <param name="name"></param>
        public void DestroySection(string name)
        {
            PagedWorldSection section;
            if (mSections.TryGetValue(name, out section))
            {
                mSections.Remove(section.Name);
                section = null;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="section"></param>
        public void DestroySection(PagedWorldSection section)
        {
            DestroySection(section.Name);
        }
        /// <summary>
        /// Retrieve a section of the world.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public PagedWorldSection GetSection(string name)
        {
            PagedWorldSection section = null;
            mSections.TryGetValue(name, out section);

            return section;
        }
        /// <summary>
        /// Give a world  the opportunity to prepare page content procedurally. 
        /// </summary>
        /// <param name="page"></param>
        /// <param name="section"></param>
        /// <returns></returns>
        public virtual bool PrepareProcedualPage(Page page, PagedWorldSection section)
        {
            bool generated = false;
            if (mPageProvider != null)
                generated = mPageProvider.PrepareProcedualPage(page, section);
            if (!generated)
                generated = mManger.PrepareProcedualPage(page, section);

            return generated;
        }
        /// <summary>
        /// Give a world  the opportunity to load page content procedurally. 
        /// </summary>
        /// <param name="page"></param>
        /// <param name="section"></param>
        /// <returns></returns>
        public virtual bool LoadProcedualPage(Page page, PagedWorldSection section)
        {
            bool generated = false;
            if (mPageProvider != null)
                generated = mPageProvider.LoadProcedualPage(page, section);
            if (!generated)
                generated = mManger.LoadProcedualPage(page, section);

            return generated;
        }
        /// <summary>
        /// Give a world  the opportunity to unload page content procedurally. 
        /// </summary>
        /// <param name="page"></param>
        /// <param name="section"></param>
        /// <returns></returns>
        public virtual bool UnloadProcedualPage(Page page, PagedWorldSection section)
        {
            bool generated = false;
            if (mPageProvider != null)
                generated = mPageProvider.UnloadProcedualPage(page, section);
            if (!generated)
                generated = mManger.UnloadProcedualPage(page, section);

            return generated;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="page"></param>
        /// <param name="section"></param>
        /// <returns></returns>
        public virtual bool UnPrepareProcedualPage(Page page, PagedWorldSection section)
        {
            bool generated = false;
            if (mPageProvider != null)
                generated = mPageProvider.UnPrepareProcedualPage(page, section);
            if (!generated)
                generated = mManger.UnPrepareProcedualPage(page, section);

            return generated;
        }
        /// <summary>
        /// Get a serialiser set up to read Page data for the given PageID. 
        /// </summary>
        /// <param name="pageId"></param>
        /// <param name="section"></param>
        /// <returns></returns>
        public StreamSerializer ReadPageStream(PageID pageId, PagedWorldSection section)
        {
            StreamSerializer ser = null;
            if (mPageProvider != null)
                ser = mPageProvider.ReadPageStream(pageId, section);
            if (ser == null)
                ser = mManger.ReadPageStream(pageId, section);

            return ser;
        }
        /// <summary>
        /// Get a serialiser set up to write Page data for the given PageID.
        /// </summary>
        /// <param name="pageId"></param>
        /// <param name="section"></param>
        /// <returns></returns>
        public StreamSerializer WritePageStream(PageID pageId, PagedWorldSection section)
        {
            StreamSerializer ser = null;
            if (mPageProvider != null)
                ser = mPageProvider.WritePageStream(pageId, section);
            if (ser == null)
                ser = mManger.WritePageStream(pageId, section);

            return ser;
        }
        /// <summary>
        /// Called when the frame starts
        /// </summary>
        /// <param name="timeSinceLastFrame"></param>
        public virtual void FrameStart(float timeSinceLastFrame)
        {
            foreach (PagedWorldSection section in mSections.Values)
                section.FrameStart(timeSinceLastFrame);
        }
        /// <summary>
        /// Called when the frame ends
        /// </summary>
        /// <param name="timeElapsed"></param>
        public virtual void FrameEnd(float timeElapsed)
        {
            foreach (PagedWorldSection section in mSections.Values)
                section.FrameEnd(timeElapsed);
        }
        /// <summary>
        /// Notify a world of the current camera
        /// </summary>
        /// <param name="cam"></param>
        public virtual void NotifyCamera(Camera cam)
        {
            foreach (PagedWorldSection section in mSections.Values)
                section.NotifyCamerea(cam);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "PagedWorld(" + this.Name + ")";
        }
    }
}
