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
    public class PageContentCollection : PageLoadableUnit
    {
        public static uint CHUNK_ID = StreamSerializer.MakeIdentifier("PGCC");
        public static ushort CHUNK_VERSION = 1;

        #region - fields -
        protected IPageContentCollectionFactory mCreator;
        protected Page mParent;
        #endregion

        #region - properties -
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
        public SceneManager SceneManager
        {
            get { return mParent.SceneManager; }
        }
        /// <summary>
        /// 
        /// </summary>
        public string Type
        {
            get
            {
                return mCreator.Name;
            }
        }
        #endregion
        /// <summary>
        /// Definition of the interface for a collection of PageContent instances. 
        /// </summary>
        /// <remarks>
        /// This class acts as a grouping level for PageContent instances. Rather than 
		/// PageContent instances being held in a list directly under Page, which might 
		/// be the most obvious solution, this intermediate class is here to allow
		/// the collection of relevant PageContent instances to be modified at runtime
		/// if required. For example, potentially you might want to define Page-level LOD
		/// in which different collections of PageContent are loaded at different times.
        /// </remarks>
        /// <param name="creator"></param>
        public PageContentCollection(IPageContentCollectionFactory creator)
        {
            mCreator = creator;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="page"></param>
        public virtual void NotifyAttached(Page page)
        {
            mParent = page;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="timeSinceLastFrame"></param>
        public virtual void FrameStart(float timeSinceLastFrame)
        {
        }
        /// <summary>
        /// /
        /// </summary>
        /// <param name="timeElapsed"></param>
        public virtual void FrameEnd(float timeElapsed)
        {
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="camera"></param>
        public virtual void NotifyCamera(Camera camera)
        {
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="stream"></param>
        public virtual void Save(StreamSerializer stream)
        {
        }
    }
}
