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
using Axiom.Serialization;

#endregion Namespace Declarations

namespace Axiom.Components.Paging
{
	/// <summary>
	/// Abstract class that can be implemented by the user application to 
	/// provide a way to retrieve or generate page data from a source of their choosing.
	/// </summary>
	/// <remarks>
	/// All of the methods in this class can be called in a background, non-render thread.
	/// </remarks>
	public abstract class PageProvider
	{
		/// <summary>
		/// Give a provider the opportunity to prepare page content procedurally. 
		/// </summary>
		/// <remarks>
		/// This call may well happen in a separate thread so it should not access 
		/// GPU resources, use loadProceduralPage for that
		/// </remarks>
		/// <returns>true if the page was populated, false otherwise</returns>
		[OgreVersion( 1, 7, 2 )]
		public virtual bool PrepareProcedualPage( Page page, PagedWorldSection section )
		{
			return false;
		}

		/// <summary>
		/// Give a provider the opportunity to load page content procedurally. 
		/// </summary>
		/// <remarks>
		/// This call will happen in the main render thread so it can access GPU resources. 
		/// Use prepareProceduralPage for background preparation.
		/// </remarks>
		/// <returns>true if the page was populated, false otherwise</returns>
		[OgreVersion( 1, 7, 2 )]
		public virtual bool LoadProcedualPage( Page page, PagedWorldSection section )
		{
			return false;
		}

		/// <summary>
		/// Give a provider the opportunity to unload page content procedurally. 
		/// </summary>
		/// <remarks>
		/// You should not call this method directly. This call will happen in 
		//  the main render thread so it can access GPU resources. Use _unprepareProceduralPage
		/// for background preparation.
		/// </remarks>
		/// <returns>true if the page was populated, false otherwise</returns>
		[OgreVersion( 1, 7, 2 )]
		public virtual bool UnloadProcedualPage( Page page, PagedWorldSection section )
		{
			return false;
		}

		/// <summary>
		/// Give a provider the opportunity to unprepare page content procedurally. 
		/// </summary>
		/// <remarks>
		/// You should not call this method directly. This call may well happen in 
		/// a separate thread so it should not access GPU resources, use _unloadProceduralPage
		/// for that
		/// </remarks>
		/// <returns>true if the page was unpopulated, false otherwise</returns>
		[OgreVersion( 1, 7, 2 )]
		public virtual bool UnPrepareProcedualPage( Page page, PagedWorldSection section )
		{
			return false;
		}

		/// <summary>
		/// Get a serialiser set up to read PagedWorld data for the given world filename.
		/// </summary>
		/// <remarks>
		/// The StreamSerialiser returned is the responsibility of the caller to delete. 
		/// </remarks>
		[OgreVersion( 1, 7, 2 )]
		public virtual StreamSerializer ReadWorldStream( string fileName )
		{
			return null;
		}

		/// <summary>
		/// Get a serialiser set up to write PagedWorld data for the given world filename.
		/// </summary>
		/// <remarks>
		/// The StreamSerialiser returned is the responsibility of the caller to delete.
		/// </remarks>
		[OgreVersion( 1, 7, 2 )]
		public virtual StreamSerializer WriteWorldStream( string fileName )
		{
			return null;
		}

		/// <summary>
		/// Get a serialiser set up to read Page data for the given PageID, 
		/// or null if this provider cannot supply one.
		/// </summary>
		/// <remarks>
		/// The StreamSerialiser returned is the responsibility of the caller to delete. 
		/// </remarks>
		/// <param name="pageId">The ID of the page being requested</param>
		/// <param name="section">The parent section to which this page will belong</param>
		[OgreVersion( 1, 7, 2 )]
		public virtual StreamSerializer ReadPageStream( PageID pageId, PagedWorldSection section )
		{
			return null;
		}

		/// <summary>
		/// Get a serialiser set up to write Page data for the given PageID, 
		//  or null if this provider cannot supply one.
		/// </summary>
		/// <remarks>
		/// The StreamSerialiser returned is the responsibility of the caller to delete. 
		/// </remarks>
		/// <param name="pageId">The ID of the page being requested</param>
		/// <param name="section">The parent section to which this page will belong</param>
		[OgreVersion( 1, 7, 2 )]
		public virtual StreamSerializer WritePageStream( PageID pageId, PagedWorldSection section )
		{
			return null;
		}
	};
}