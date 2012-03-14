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
	/// <summary>
	///  Specialisation of PageContentCollection which just provides a simple list
	///	 of PageContent instances. 
	/// </summary>
	/// <remarks>
	/// @par
	/// The data format for this in a file is:<br/>
	/// <b>SimplePageContentCollectionData (Identifier 'SPCD')</b>\n
	/// [Version 1]
	/// <table>
	/// <tr>
	/// <td><b>Name</b></td>
	/// <td><b>Type</b></td>
	/// <td><b>Description</b></td>
	/// </tr>
	/// <tr>
	/// <td>Nested contents</td>
	/// <td>Nested chunk list</td>
	/// <td>A sequence of nested PageContent chunks</td>
	/// </tr>
	/// </table>
	/// </remarks>
	public class SimplePageContentCollection : PageContentCollection
	{
		public static uint SUBCLASS_CHUNK_ID = StreamSerializer.MakeIdentifier( "SPCD" );
		public static ushort SUBCLASS_CHUNK_VERSION = 1;

		protected List<PageContent> mContentList;

		[OgreVersion( 1, 7, 2 )]
		public SimplePageContentCollection( SimplePageContentCollectionFactory creator )
			: base( creator ) { }

		/// <summary>
		/// Get const access to the list of content
		/// </summary>
		public List<PageContent> ContentList
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return this.mContentList;
			}
		}

		[OgreVersion( 1, 7, 2, "~SimplePageContentCollection" )]
		protected override void dispose( bool disposeManagedResources )
		{
			if ( !IsDisposed )
			{
				if ( disposeManagedResources )
				{
					foreach ( PageContent i in this.mContentList )
					{
						i.SafeDispose();
					}

					this.mContentList.Clear();
				}
			}

			base.dispose( disposeManagedResources );
		}

		/// <summary>
		/// Create a new PageContent within this collection.
		/// </summary>
		/// <param name="typeName">The name of the type of content  (see PageManager::getContentFactories)</param>
		[OgreVersion( 1, 7, 2 )]
		public virtual PageContent CreateContent( string typeName )
		{
			PageContent c = Manager.CreateContent( typeName );
			this.mContentList.Add( c );
			return c;
		}

		/// <summary>
		/// Destroy a PageContent within this page.
		/// This is equivalent to calling detachContent and 
		/// PageManager::destroyContent.
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public virtual void DestroyContent( PageContent c )
		{
			if ( this.mContentList.Contains( c ) )
			{
				this.mContentList.Remove( c );
			}

			Manager.DestroyContent( ref c );
		}

		[OgreVersion( 1, 7, 2 )]
		public override void Save( StreamSerializer stream )
		{
			stream.WriteChunkBegin( SUBCLASS_CHUNK_ID, SUBCLASS_CHUNK_VERSION );

			foreach ( PageContent c in this.mContentList )
			{
				c.Save( stream );
			}

			stream.WriteChunkEnd( SUBCLASS_CHUNK_ID );
		}

		[OgreVersion( 1, 7, 2 )]
		public override void FrameStart( Real timeSinceLastFrame )
		{
			foreach ( PageContent c in this.mContentList )
			{
				c.FrameStart( timeSinceLastFrame );
			}
		}

		[OgreVersion( 1, 7, 2 )]
		public override void FrameEnd( Real timeElapsed )
		{
			foreach ( PageContent c in this.mContentList )
			{
				c.FrameEnd( timeElapsed );
			}
		}

		[OgreVersion( 1, 7, 2 )]
		public override void NotifyCamera( Camera camera )
		{
			foreach ( PageContent c in this.mContentList )
			{
				c.NotifyCamera( camera );
			}
		}

		[OgreVersion( 1, 7, 2 )]
		public override bool Prepare( StreamSerializer stream )
		{
			if ( stream.ReadChunkBegin( SUBCLASS_CHUNK_ID, SUBCLASS_CHUNK_VERSION, "SimplePageContentCollection" ) == null )
			{
				return false;
			}

			bool ret = true;
			foreach ( PageContent i in this.mContentList )
			{
				ret &= i.Prepare( stream );
			}

			stream.ReadChunkEnd( SUBCLASS_CHUNK_ID );
			return ret;
		}

		[OgreVersion( 1, 7, 2 )]
		public override void Load()
		{
			foreach ( PageContent i in this.mContentList )
			{
				i.Load();
			}
		}

		[OgreVersion( 1, 7, 2 )]
		public override void UnLoad()
		{
			foreach ( PageContent i in this.mContentList )
			{
				i.UnLoad();
			}
		}

		[OgreVersion( 1, 7, 2 )]
		public override void UnPrepare()
		{
			foreach ( PageContent i in this.mContentList )
			{
				i.UnPrepare();
			}
		}
	};
}
