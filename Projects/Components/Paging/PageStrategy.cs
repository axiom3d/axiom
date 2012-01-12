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
using Axiom.Math;

#endregion Namespace Declarations

namespace Axiom.Components.Paging
{
	/// <summary>
	/// Defines the interface to a strategy class which is responsible for deciding
	/// when Page instances are requested for addition and removal from the 
	/// paging system.
	/// </summary>
	/// <remarks>
	/// The interface is deliberately light, with no specific mention of requesting
	/// new Page instances. It is entirely up to the PageStrategy to respond
	/// to the events raised on it and to call methods on other classes (such as
	/// requesting new pages).
	/// </remarks>
	public abstract class PageStrategy : DisposableObject
	{
		protected string mName;
		protected PageManager mManager;

		public string Name
		{
			[OgreVersion( 1, 7, 2 )]
			get { return mName; }
		}

		public PageManager Manager
		{
			[OgreVersion( 1, 7, 2 )]
			get { return mManager; }
		}

		[OgreVersion( 1, 7, 2 )]
		public PageStrategy( string name, PageManager manager )
			: base()
		{
			mName = name;
			mManager = manager;
		}

		/// <summary>
		/// Called when the frame starts
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public virtual void FrameStart( Real timeSinceLastFrame, PagedWorldSection section ) { }

		/// <summary>
		/// Called when the frame ends
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public virtual void FrameEnd( Real timeElapsed, PagedWorldSection section ) { }

		/// <summary>
		/// Called when a camera is used for any kind of rendering.
		/// </summary>
		/// <remarks>
		/// This is probably the primary way in which the strategy will request	new pages.
		/// </remarks>
		/// <param name="cam">Camera which is being used for rendering. Class should not
		/// rely on this pointer remaining valid permanently because no notification 
		/// will be given when the camera is destroyed.</param>
		[OgreVersion( 1, 7, 2 )]
		public virtual void NotifyCamera( Camera cam, PagedWorldSection section ) { }

		/// <summary>
		/// Create a PageStrategyData instance containing the data specific to this
		///	PageStrategy. 
		/// </summary>
		/// <remarks>
		/// This data will be held by a given PagedWorldSection and the structure of
		/// the data will be specific to the PageStrategy subclass.
		/// </remarks>
		[OgreVersion( 1, 7, 2 )]
		public abstract IPageStrategyData CreateData();

		/// <summary>
		/// Destroy a PageStrategyData instance containing the data specific to this
		/// PageStrategy. 
		/// </summary>
		/// <remarks>
		/// This data will be held by a given PagedWorldSection and the structure of
		/// the data will be specific to the PageStrategy subclass.
		/// </remarks>
		[OgreVersion( 1, 7, 2 )]
		public abstract void DestroyData( IPageStrategyData d );

		/// <summary>
		/// Update the contents of the passed in SceneNode to reflect the 
		/// debug display of a given page. 
		/// </summary>
		/// <remarks>
		/// The PageStrategy is to have complete control of the contents of this
		/// SceneNode, it must not be altered / added to by others.
		/// </remarks>
		[OgreVersion( 1, 7, 2 )]
		public abstract void UpdateDebugDisplay( Page p, SceneNode sn );

		/// <summary>
		/// Get the page ID for a given world position. 
		/// </summary>
		/// <returns>The page ID</returns>
		[OgreVersion( 1, 7, 2 )]
		public abstract PageID GetPageID( Vector3 worldPos, PagedWorldSection section );
	}
}
