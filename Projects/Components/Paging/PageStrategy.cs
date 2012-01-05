#region MIT/X11 License

//Copyright © 2003-2011 Axiom 3D Rendering Engine Project
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

#endregion Namespace Declarations

namespace Axiom.Components.Paging
{
	/// <summary>
	/// Defines the interface to a strategy class which is responsible for deciding
	/// when Page instances are requested for addition and removal from the 
	/// paging system.
	/// </summary>
	public class PageStrategy
	{
		/// <summary>
		/// 
		/// </summary>
		protected string mName;

		/// <summary>
		/// 
		/// </summary>
		protected PageManager mManager;

		/// <summary>
		/// 
		/// </summary>
		public string Name { get { return mName; } }

		/// <summary>
		/// 
		/// </summary>
		public PageManager Manager { get { return mManager; } }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="name"></param>
		/// <param name="manager"></param>
		public PageStrategy( string name, PageManager manager )
		{
			mName = name;
			mManager = manager;
		}

		/// <summary>
		/// Destructor.
		/// </summary>
		public PageStrategy() {}

		/// <summary>
		/// Called when the frame starts
		/// </summary>
		/// <param name="timeSinceLastFrame"></param>
		/// <param name="section"></param>
		virtual public void FrameStart( float timeSinceLastFrame, PagedWorldSection section ) {}

		/// <summary>
		/// Called when the frame ends
		/// </summary>
		/// <param name="timeSinceLastFrame"></param>
		/// <param name="section"></param>
		virtual public void FrameEnd( float timeElapsed, PagedWorldSection section ) {}

		/// <summary>
		/// Called when a camera is used for any kind of rendering.
		/// </summary>
		/// <param name="cam"></param>
		/// <param name="section"></param>
		virtual public void NotifyCamera( Camera cam, PagedWorldSection section ) {}

		/// <summary>
		/// Create a PageStrategyData instance containing the data specific to this
		///	PageStrategy. 
		/// </summary>
		/// <returns></returns>
		virtual public IPageStrategyData CreateData()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Destroy a PageStrategyData instance containing the data specific to this
		/// PageStrategy. 
		/// </summary>
		/// <param name="data"></param>
		virtual public void DestroyData( IPageStrategyData data )
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Update the contents of the passed in SceneNode to reflect the 
		/// debug display of a given page. 
		/// </summary>
		/// <param name="p"></param>
		/// <param name="n"></param>
		virtual public void UpdateDebugDisplay( Page p, SceneNode n )
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Get the page ID for a given world position. 
		/// </summary>
		/// <param name="worldPos"></param>
		/// <param name="section"></param>
		/// <returns></returns>
		virtual public PageID GetPageID( Vector3 worldPos, PagedWorldSection section )
		{
			throw new NotImplementedException();
		}
	}
}
