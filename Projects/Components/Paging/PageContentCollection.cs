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

using Axiom.Core;
using Axiom.Math;
using Axiom.Serialization;

#endregion Namespace Declarations

namespace Axiom.Components.Paging
{
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
	public abstract class PageContentCollection : DisposableObject
	{
		public static uint CHUNK_ID = StreamSerializer.MakeIdentifier( "PGCC" );
		public static ushort CHUNK_VERSION = 1;

		#region - fields -
		
		protected IPageContentCollectionFactory mCreator;
		protected Page mParent;
		
		#endregion - fields -

		#region - properties -

		public PageManager Manager
		{
			[OgreVersion( 1, 7, 2 )]
			get { return mParent.Manager; }
		}

		/// <summary>
		/// Get the type of the collection, which will match it's factory
		/// </summary>
		public virtual string Type
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return mCreator.Name;
			}
		}

		public SceneManager SceneManager
		{
			[OgreVersion( 1, 7, 2 )]
			get { return mParent.SceneManager; }
		}

		#endregion - properties -

		/// <summary>
		/// Default Constructor
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public PageContentCollection( IPageContentCollectionFactory creator )
			: base()
		{
			mCreator = creator;
		}

		/// <summary>
		/// Internal method to notify a collection that it is attached
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		internal virtual void NotifyAttached( Page page )
		{
			mParent = page;
		}

        /// <summary>
        /// Save the collection to a stream
        /// </summary>
        [OgreVersion( 1, 7, 2 )]
        public abstract void Save( StreamSerializer stream );

		/// <summary>
        /// Called when the frame starts
		/// </summary>
        [OgreVersion( 1, 7, 2 )]
        public abstract void FrameStart( Real timeSinceLastFrame );

		/// <summary>
        /// Called when the frame ends
		/// </summary>
        [OgreVersion( 1, 7, 2 )]
        public abstract void FrameEnd( Real timeElapsed );

		/// <summary>
        /// Notify a section of the current camera
		/// </summary>
        [OgreVersion( 1, 7, 2 )]
        public abstract void NotifyCamera( Camera camera );

        /// <summary>
        /// Prepare data - may be called in the background
        /// </summary>
        [OgreVersion( 1, 7, 2 )]
        public abstract bool Prepare( StreamSerializer ser );

        /// <summary>
        /// Load - will be called in main thread
        /// </summary>
        [OgreVersion( 1, 7, 2 )]
        public abstract void Load();

        /// <summary>
        /// UnLoad - will be called in main thread
        /// </summary>
        [OgreVersion( 1, 7, 2 )]
        public abstract void UnLoad();

        /// <summary>
        /// UnPrepare date - may be called in the background
        /// </summary>
        [OgreVersion( 1, 7, 2 )]
        public abstract void UnPrepare();
	};
}
