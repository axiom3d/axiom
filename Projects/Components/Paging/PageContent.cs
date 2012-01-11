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
    public class PageContent : PageLoadableUnit
    {
        IPageContentFactory mCreator;
        PageContentCollection mParent;

        #region - properties -
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
        #endregion

        #region - constructor, destructor -
        /// <summary>
        /// 
        /// </summary>
        /// <param name="creator"></param>
        public PageContent(IPageContentFactory creator)
        {
            mCreator = creator;
        }
        #endregion

        /// <summary>
        /// Internal method to notify a page that it is attached
        /// </summary>
        /// <param name="parent"></param>
        public virtual void NotifyAttached(PageContentCollection parent)
        {
            mParent = parent;
        }
        /// <summary>
        /// Save content to a stream
        /// </summary>
        /// <param name="stream"></param>
        public virtual void Save(StreamSerializer stream)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Called when the frame starts.
        /// </summary>
        /// <param name="timeSinceLastFrame"></param>
        public virtual void FrameStart(float timeSinceLastFrame)
        {
        }

        /// <summary>
        /// Called when the frame ends.
        /// </summary>
        /// <param name="timeElapsed"></param>
        public virtual void FrameEnd(float timeElapsed)
        {
        }

        /// <summary>
        /// Notify a section of the current camera.
        /// </summary>
        /// <param name="camera"></param>
        public virtual void NotifyCamera(Camera camera)
        {
        }
    }
}
