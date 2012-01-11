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
using System.Collections.Generic;
using Axiom.Core;
using Axiom.Serialization;

#endregion Namespace Declarations

namespace Axiom.Components.Paging
{
    /// <summary>
    ///  Specialisation of PageContentCollection which just provides a simple list
	///	 of PageContent instances. 
    /// </summary>
    public class SimplePageContentCollection : PageContentCollection
    {
        public static uint SUBCLASS_CHUNK_ID = StreamSerializer.MakeIdentifier("SPCD");
        public static ushort SUBCLASS_CHUNK_VERSION = 1;
        protected List<PageContent> mContentList;

        /// <summary>
        /// 
        /// </summary>
        public List<PageContent> ContentList
        {
            get { return mContentList; }
        }

        #region - constructor, destructor -
        /// <summary>
        /// 
        /// </summary>
        /// <param name="factory"></param>
        public SimplePageContentCollection(SimplePageContentCollectionFactory factory)
            :base(factory){ }

        ~SimplePageContentCollection() 
        {
            Destroy();
            mContentList.Clear();
        }
        #endregion

        /// <summary>
        /// 
        /// </summary>
        /// <param name="typeName"></param>
        /// <returns></returns>
        public virtual PageContent CreateContent(string typeName)
        {
            PageContent c = Manager.CreateContent(typeName);
            AttachContent(c);
            return c;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pcont"></param>
        public virtual void DestroyContent(PageContent pcont)
        {
            DetachContent(pcont);
            Manager.DestroyContent(pcont);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="content"></param>
        public virtual void AttachContent(PageContent content)
        {
            mContentList.Add(content);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="content"></param>
        public virtual void DetachContent(PageContent content)
        {
            mContentList.Remove(content);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="stream"></param>
        public override void Save(StreamSerializer stream)
        {
            stream.WriteChunkBegin(SUBCLASS_CHUNK_ID, SUBCLASS_CHUNK_VERSION);

            foreach (PageContent c in mContentList)
                c.Save(stream);

            stream.WriteChunkEnd(SUBCLASS_CHUNK_ID);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="timeSinceLastFrame"></param>
        public override void FrameStart(float timeSinceLastFrame)
        {
            foreach (PageContent c in mContentList)
                c.FrameStart(timeSinceLastFrame);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="timeElapsed"></param>
        public override void FrameEnd(float timeElapsed)
        {
            foreach (PageContent c in mContentList)
                c.FrameEnd(timeElapsed);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="camera"></param>
        public override void NotifyCamera(Camera camera)
        {
            foreach (PageContent c in mContentList)
                c.NotifyCamera(camera);
        }

        /// <summary>
        /// Finalising the load of the data.
        /// </summary>
        protected override void LoadImpl()
        {
            foreach (PageContent c in mContentList)
                c.Load();
        }

        /// <summary>
        /// Unload the unit, deallocating any GPU resources.
        /// </summary>
        protected override void UnLoadImpl()
        {
            foreach (PageContent c in mContentList)
                c.Unload();
        }

        /// <summary>
        /// Deallocate any background resources.
        /// </summary>
        /// <returns></returns>
        protected override void UnPrepareImpl()
        {
            foreach (PageContent c in mContentList)
                c.UnPrepare();
        }

    }
}
