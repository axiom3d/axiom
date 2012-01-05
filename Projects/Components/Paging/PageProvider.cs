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
using System.Linq;
using System.Text;

using Axiom.Serialization;

#endregion Namespace Declarations

namespace Axiom.Components.Paging
{
	public class PageProvider
	{
		/// <summary>
		/// 
		/// </summary>
		public PageProvider() {}

		/// <summary>
		/// Give a provider the opportunity to prepare page content procedurally. 
		/// </summary>
		/// <param name="page"></param>
		/// <param name="section"></param>
		/// <returns></returns>
		virtual public bool PrepareProcedualPage( Page page, PagedWorldSection section )
		{
			return false;
		}

		/// <summary>
		/// Give a provider the opportunity to load page content procedurally. 
		/// </summary>
		/// <param name="page"></param>
		/// <param name="section"></param>
		/// <returns></returns>
		virtual public bool LoadProcedualPage( Page page, PagedWorldSection section )
		{
			return false;
		}

		/// <summary>
		/// Give a provider the opportunity to unload page content procedurally. 
		/// </summary>
		/// <param name="page"></param>
		/// <param name="section"></param>
		/// <returns></returns>
		public bool UnloadProcedualPage( Page page, PagedWorldSection section )
		{
			return false;
		}

		/// <summary>
		/// Give a provider the opportunity to unprepare page content procedurally. 
		/// </summary>
		/// <param name="page"></param>
		/// <param name="section"></param>
		/// <returns></returns>
		public bool UnPrepareProcedualPage( Page page, PagedWorldSection section )
		{
			return false;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="pageId"></param>
		/// <param name="section"></param>
		/// <returns></returns>
		public StreamSerializer WritePageStream( PageID pageId, PagedWorldSection section )
		{
			return null;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="fileName"></param>
		/// <returns></returns>
		public StreamSerializer ReadWorldStream( string fileName )
		{
			return null;
		}

		/// <summary>
		/// Get a serialiser set up to read PagedWorld data for the given world filename. 
		/// </summary>
		/// <param name="pageId"></param>
		/// <param name="section"></param>
		/// <returns></returns>
		virtual public StreamSerializer ReadPageStream( PageID pageId, PagedWorldSection section )
		{
			return null;
		}
	}
}
