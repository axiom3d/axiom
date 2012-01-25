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
using System.IO;
using Axiom.Core;
using Axiom.Math;
using Axiom.Serialization;

#endregion Namespace Declarations

namespace Axiom.Components.Paging
{
	/// <summary>
	/// This class represents a collection of pages which make up a world.
	/// </summary>
	/// <remarks>
	/// It's important to bear in mind that the PagedWorld only delineates the
	/// world and knows how to find out about the contents of it. It does not, 
	/// by design, contain all elements of the world, in memory, at once.
	/// </remarks>
	public class PagedWorld: DisposableObject
	{
		#region - constanst -

		public static uint CHUNK_ID = StreamSerializer.MakeIdentifier( "PWLD" );
		public static uint CHUNK_SECTIONDECLARATION_ID = StreamSerializer.MakeIdentifier( "PWLS" );
		public static ushort CHUNK_VERSION = 1;
		
		#endregion - constants -

		#region - fields -

		protected string mName;
		protected PageManager mManager;
		protected PageProvider mPageProvider;
		protected Dictionary<string, PagedWorldSection> mSections = new Dictionary<string, PagedWorldSection>();
		protected NameGenerator<PagedWorldSection> mSectionNameGenerator;

		#endregion - fields -

		#region - properties -

		/// <summary>
		/// Get/Set the PageProvider which can provide streams for Pages in this world.
		/// </summary>
		public PageProvider PageProvider
		{
			[OgreVersion( 1, 7, 2 )]
			get { return mPageProvider; }

			/// <remarks>
			/// This is the top-level way that you can direct how Page data is loaded. 
			/// When data for a Page is requested for a PagedWorldSection, the following
			/// sequence of classes will be checked to see if they have a provider willing
			/// to supply the stream: PagedWorldSection, PagedWorld, PageManager.
			/// If none of these do, then the default behaviour is to look for a file
			/// called worldname_sectionname_pageID.page. 
			/// @note
			/// The caller remains responsible for the destruction of the provider.
			/// </remarks>
			[OgreVersion( 1, 7, 2 )]
			set { mPageProvider = value; }
		}

		/// <summary>
		/// Retrieve a const reference to all the sections in this world
		/// </summary>
		public Dictionary<string, PagedWorldSection> Sections
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return mSections;
			}
		}

		/// <summary>
		/// Get the number of sections this world has.
		/// </summary>
		public int SectionCount
		{
			[OgreVersion( 1, 7, 2 )]
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
			[OgreVersion( 1, 7, 2 )]
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
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return mManager;
			}
		}
		
		#endregion - properties -

		/// <summary>
		/// Default Constructor.
		/// </summary>
		/// <param name="name">The name of the world, which must be enough to identify the 
		/// place where data for it can be loaded from (doesn't have to be a filename
		/// necessarily).
		/// </param>
		/// <param name="manager">The PageManager that is in charge of providing this world with
		/// services such as related object factories.</param>
		[OgreVersion( 1, 7, 2 )]
		public PagedWorld( string name, PageManager manager )
			: base()
		{
			mName = name;
			mManager = manager;
			mSectionNameGenerator = new NameGenerator<PagedWorldSection>( "Section" );
		}

		[OgreVersion( 1, 7, 2, "~PagedWorld" )]
		protected override void dispose( bool disposeManagedResources )
		{
			if ( !this.IsDisposed )
			{
				if ( disposeManagedResources )
				{
					DestroyAllSections();
				}
			}

			base.dispose( disposeManagedResources );
		}

		/// <summary>
		/// Load world data from a file
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public void Load( string fileName )
		{
			StreamSerializer stream = mManager.ReadWorldStream( fileName );
			Load( stream );
			stream.SafeDispose();
		}

		/// <summary>
		/// Load world data from a stream
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public void Load( Stream stream )
		{
			StreamSerializer ser = new StreamSerializer( stream );
			Load( ser );
		}

		/// <summary>
		/// Load world data from a serialiser (returns true if successful)
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public bool Load( StreamSerializer stream )
		{
			if ( stream.ReadChunkBegin( CHUNK_ID, CHUNK_VERSION, "PageWorld" ) == null )
				return false;

			//name
			stream.Read( out mName );
			//sections
			while ( stream.NextChunkId == PagedWorld.CHUNK_SECTIONDECLARATION_ID )
			{
				stream.ReadChunkBegin();
				string sectionType, sectionName;
				stream.Read( out sectionType );
				stream.Read( out sectionName );
				stream.ReadChunkEnd( CHUNK_SECTIONDECLARATION_ID );
				// Scene manager will be loaded
				PagedWorldSection sec = CreateSection( null, sectionType, sectionName );
				bool sectionOk = sec.Load( stream );
				if ( !sectionOk )
					DestroySection( sec );
			}

			stream.ReadChunkEnd( CHUNK_ID );

			return true;
		}

		/// <summary>
		/// Save world data to a file
		/// </summary>
		/// <param name="fileName">
		/// The name of the file to create; this can either be an 
		/// absolute filename or
		/// </param>
		[OgreVersion( 1, 7, 2 )]
		public void Save( string fileName )
		{
			StreamSerializer stream = mManager.WriteWorldStream( fileName );
			Save( stream );
			stream.SafeDispose();
		}

		/// <summary>
		/// Save world data to a stream
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public void Save( Stream stream )
		{
			StreamSerializer ser = new StreamSerializer( stream );
			Save( ser );
		}

		/// <summary>
		/// Save world data to a serialiser
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public void Save( StreamSerializer stream )
		{
			stream.WriteChunkBegin( CHUNK_ID, CHUNK_VERSION );

			//name
			stream.Write( mName );
			//sections
			foreach ( var section in mSections.Values )
			{
				//declaration
				stream.Write( CHUNK_SECTIONDECLARATION_ID );
				stream.Write( section.Type );
				stream.Write( section.Name );
				stream.WriteChunkEnd( CHUNK_SECTIONDECLARATION_ID );

				//data
				section.Save( stream );
			}

			stream.WriteChunkEnd( CHUNK_ID );
		}

		/// <summary>
		/// Create a new section of the world based on a specialised type.
		/// </summary>
		/// <remarks>
		/// World sections are areas of the world that use a particular
		/// PageStrategy, with a certain set of parameters specific to that
		/// strategy, and potentially some other rules. 
		/// So you would have more than one section in a world only 
		/// if you needed different simultaneous paging strategies, or you 
		/// wanted the same strategy but parameterised differently.
		/// </remarks>
		/// <param name="sceneMgr">The SceneManager to use for this section.</param>
		/// <param name="typeName">The type of section to use (must be registered	
		/// with PageManager), or blank to use the default type (simple grid)</param>
		/// <param name="sectionName">An optional name to give the section (if none is
		/// provided, one will be generated)</param>
		[OgreVersion( 1, 7, 2 )]
		public PagedWorldSection CreateSection( SceneManager sceneMgr, string typeName, string sectionName )
		{
			var theName = sectionName;
			if ( theName == string.Empty )
			{
				do
				{
					theName = mSectionNameGenerator.GetNextUniqueName();
				} while ( mSections.ContainsKey( theName ) );
			}
			else if ( mSections.ContainsKey( theName ) )
				throw new AxiomException( "World section named '{0}' already exists! PagedWorld.CreateSection", theName );

			PagedWorldSection ret = null;
			if ( typeName == "General" )
				ret = new PagedWorldSection( theName, this, sceneMgr );
			else
			{
				PagedWorldSectionFactory fact = mManager.GetWorldSectionFactory( typeName );
				if ( fact == null )
					throw new AxiomException( "World section type '{0}' does not exist! PagedWorld::createSection", typeName );

				ret = fact.CreateInstance( theName, this, sceneMgr );

			}
			mSections.Add( theName, ret );

			return ret;
		}

		public PagedWorldSection CreateSection( SceneManager sceneMgr, string typeName )
		{
			return this.CreateSection( sceneMgr, typeName, string.Empty );
		}

		/// <summary>
		/// Create a new manually defined section of the world.
		/// </summary>
		/// <remarks>
		/// World sections are areas of the world that use a particular
		/// PageStrategy, with a certain set of parameters specific to that
		/// strategy. So you would have more than one section in a world only 
		/// if you needed different simultaneous paging strategies, or you 
		/// wanted the same strategy but parameterised differently.
		/// </remarks>
		/// <param name="strategyName">The name of the strategy to use (must be registered	
		/// with PageManager)</param>
		/// <param name="sceneMgr">The SceneManager to use for this section</param>
		/// <param name="sectionName">An optional name to give the section (if none is
		/// provided, one will be generated)</param>
		[OgreVersion( 1, 7, 2 )]
		public PagedWorldSection CreateSection( string strategyName, SceneManager sceneMgr, string sectionName )
		{
			// get the strategy
			PageStrategy strategy = mManager.GetStrategy( strategyName );
			return this.CreateSection( strategy, sceneMgr, sectionName );
		}

		public PagedWorldSection CreateSection( string strategyName, SceneManager sceneMgr )
		{
			return this.CreateSection( strategyName, sceneMgr, string.Empty );
		}

		/// <summary>
		/// Create a manually defined new section of the world.
		/// </summary>
		/// <remarks>
		/// World sections are areas of the world that use a particular
		/// PageStrategy, with a certain set of parameters specific to that
		/// strategy. So you would have more than one section in a world only 
		/// if you needed different simultaneous paging strategies, or you 
		/// wanted the same strategy but parameterised differently.
		/// </remarks>
		/// <param name="strategy">The strategy to use</param>
		/// <param name="sceneMgr">The SceneManager to use for this section</param>
		/// <param name="sectionName">An optional name to give the section (if none is
		///provided, one will be generated)</param>
		[OgreVersion( 1, 7, 2 )]
		public PagedWorldSection CreateSection( PageStrategy strategy, SceneManager sceneMgr, string sectionName )
		{
			PagedWorldSection ret = this.CreateSection( sceneMgr, "General", sectionName );
			ret.Strategy = strategy;
			return ret;
		}

		public PagedWorldSection CreateSection( PageStrategy strategy, SceneManager sceneMgr )
		{
			return this.CreateSection( strategy, sceneMgr, string.Empty );
		}
		
		/// <summary>
		/// Destroy a section of world.
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public void DestroySection(string name)
		{
			if ( mSections.ContainsKey( name ) )
			{
				mSections[ name ].SafeDispose();
				mSections.Remove( name );
			}
		}

		/// <summary>
		/// Destroy a section of world.
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public void DestroySection( PagedWorldSection section )
		{
			DestroySection( section.Name );
		}

		/// <summary>
		/// Destroy all world sections
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public void DestroyAllSections()
		{
			foreach ( var i in mSections.Values )
				i.SafeDispose();

			mSections.Clear();
		}

		/// <summary>
		/// Retrieve a section of the world.
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public PagedWorldSection GetSection( string name )
		{
			PagedWorldSection section = null;
			mSections.TryGetValue( name, out section );
			return section;
		}

		/// <summary>
		/// Give a world  the opportunity to prepare page content procedurally. 
		/// </summary>
		/// <remarks>
		/// You should not call this method directly. This call may well happen in 
		/// a separate thread so it should not access GPU resources, use LoadeProceduralPage
		/// for that
		/// </remarks>
		/// <returns>true if the page was populated, false otherwise</returns>
		[OgreVersion( 1, 7, 2 )]
		internal virtual bool PrepareProcedualPage( Page page, PagedWorldSection section )
		{
			bool generated = false;
			if ( mPageProvider != null )
				generated = mPageProvider.PrepareProcedualPage( page, section );
			if ( !generated )
				generated = mManager.PrepareProcedualPage( page, section );

			return generated;
		}

		/// <summary>
		/// Give a world  the opportunity to load page content procedurally. 
		/// </summary>
		/// <remarks>
		/// You should not call this method directly. This call will happen in 
		/// the main render thread so it can access GPU resources. Use PrepareProceduralPage
		/// for background preparation.
		/// </remarks>
		/// <returns>true if the page was populated, false otherwise</returns>
		[OgreVersion( 1, 7, 2 )]
		internal virtual bool LoadProcedualPage( Page page, PagedWorldSection section )
		{
			bool generated = false;
			if ( mPageProvider != null )
				generated = mPageProvider.LoadProcedualPage( page, section );
			if ( !generated )
				generated = mManager.LoadProcedualPage( page, section );

			return generated;
		}

		/// <summary>
		/// Give a world  the opportunity to unprepare page content procedurally.
		/// </summary>
		/// <remarks>
		/// You should not call this method directly. This call may well happen in 
		/// a separate thread so it should not access GPU resources, use UnLoadProceduralPage
		/// for that
		/// </remarks>
		/// <returns>true if the page was unpopulated, false otherwise</returns>
		[OgreVersion( 1, 7, 2 )]
		internal virtual bool UnPrepareProcedualPage( Page page, PagedWorldSection section )
		{
			bool generated = false;
			if ( mPageProvider != null )
				generated = mPageProvider.UnPrepareProcedualPage( page, section );
			if ( !generated )
				generated = mManager.UnPrepareProcedualPage( page, section );

			return generated;
		}

		/// <summary>
		/// Give a world  the opportunity to unload page content procedurally. 
		/// </summary>
		/// <remarks>
		/// You should not call this method directly. This call will happen in 
		/// the main render thread so it can access GPU resources. Use UnPrepareProceduralPage
		/// for background preparation.
		/// </remarks>
		/// <returns>true if the page was populated, false otherwise</returns>
		[OgreVersion( 1, 7, 2 )]
		internal virtual bool UnloadProcedualPage(Page page, PagedWorldSection section)
		{
			bool generated = false;
			if (mPageProvider != null)
				generated = mPageProvider.UnloadProcedualPage(page, section);
			if (!generated)
				generated = mManager.UnloadProcedualPage(page, section);

			return generated;
		}

		/// <summary>
		/// Get a serialiser set up to read Page data for the given PageID. 
		/// </summary>
		/// <remarks>
		/// The StreamSerialiser returned is the responsibility of the caller to delete.
		/// </remarks>
		/// <param name="pageId">The ID of the page being requested</param>
		/// <param name="section">The parent section to which this page will belong</param>
		[OgreVersion( 1, 7, 2 )]
		internal StreamSerializer ReadPageStream( PageID pageId, PagedWorldSection section )
		{
			StreamSerializer ser = null;
			if ( mPageProvider != null )
				ser = mPageProvider.ReadPageStream( pageId, section );
			if ( ser == null )
				ser = mManager.ReadPageStream( pageId, section );

			return ser;
		}

		/// <summary>
		/// Get a serialiser set up to write Page data for the given PageID.
		/// </summary>
		/// <remarks>
		/// The StreamSerialiser returned is the responsibility of the caller to delete.
		/// </remarks>
		/// <param name="pageId">The ID of the page being requested</param>
		/// <param name="section">The parent section to which this page will belong</param>
		[OgreVersion( 1, 7, 2 )]
		public StreamSerializer WritePageStream( PageID pageId, PagedWorldSection section )
		{
			StreamSerializer ser = null;
			if ( mPageProvider != null )
				ser = mPageProvider.WritePageStream( pageId, section );
			if ( ser == null )
				ser = mManager.WritePageStream( pageId, section );

			return ser;
		}

		/// <summary>
		/// Called when the frame starts
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public virtual void FrameStart( Real t )
		{
			foreach ( var section in mSections.Values )
				section.FrameStart( t );
		}

		/// <summary>
		/// Called when the frame ends
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public virtual void FrameEnd( Real t )
		{
			foreach ( var section in mSections.Values )
				section.FrameEnd( t );
		}

		/// <summary>
		/// Notify a world of the current camera
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public virtual void NotifyCamera( Camera cam )
		{
			foreach ( var section in mSections.Values )
				section.NotifyCamerea( cam );
		}

		[OgreVersion( 1, 7, 2, "operator <<" )]
		public override string ToString()
		{
			return string.Format( "PagedWorld({0})", this.Name );
		}
	};
}
