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
using Axiom.Serialization;

#endregion Namespace Declarations

namespace Axiom.Components.Paging
{
	/// <summary>
	/// The PageManager is the entry point through which you load all PagedWorld instances, 
	/// and the place where PageStrategy instances and factory classes are
	/// registered to customise the paging behaviour.
	/// </summary>
	/// <remarks>
	/// To get started, the minimum you need is a PagedWorld with at least one PagedWorldSection
	/// within it, and at least one Camera being tracked (see addCamera).
	/// </remarks>
	public class PageManager : DisposableObject
	{
		protected Dictionary<string, PagedWorld> mWorlds = new Dictionary<string, PagedWorld>();
		protected Dictionary<string, PageStrategy> mStrategies = new Dictionary<string, PageStrategy>();
		protected Dictionary<string, IPageContentCollectionFactory> mContentCollectionFactories = new Dictionary<string, IPageContentCollectionFactory>();
		protected Dictionary<string, PagedWorldSectionFactory> mWorldSectionFactories = new Dictionary<string, PagedWorldSectionFactory>();
		protected Dictionary<string, IPageContentFactory> mContentFactories = new Dictionary<string, IPageContentFactory>();
		protected NameGenerator<PagedWorld> mWorldNameGenerator = new NameGenerator<PagedWorld>( "World" );
		protected PageProvider mPageProvider;
		protected string mPageResourceGroup;
		protected List<Camera> mCameraList = new List<Camera>();
		protected EventRouter mEventRouter;
		protected byte mDebugDisplayLvl;
		protected Grid2PageStrategy mGrid2DPageStrategy;
		protected SimplePageContentCollectionFactory mSimpleCollectionFactory;

		/// <summary>
		/// Get/Set the PageProvider which can provide streams for any Page.
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
			/// </remarks>
			[OgreVersion( 1, 7, 2 )]
			set { mPageProvider = value; }
		}

		public Dictionary<string, IPageContentFactory> ContentFactories
		{
			[OgreVersion( 1, 7, 2 )]
			get { return mContentFactories; }
		}

		public Dictionary<string, IPageContentCollectionFactory> ContentCollectionFactories
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return mContentCollectionFactories;
			}
		}

		public Dictionary<string, PagedWorldSectionFactory> WorldSectionFactories
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return mWorldSectionFactories;
			}
		}

		public Dictionary<string, PageStrategy> Strategies
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return mStrategies;
			}
		}

		/// <summary>
		/// Get a reference to the worlds that are currently loaded.
		/// </summary>
		public Dictionary<string, PagedWorld> Worlds
		{
			[OgreVersion( 1, 7, 2 )]
			get { return mWorlds; }
		}

		/// <summary>
		/// Set the debug display level.
		/// </summary>
		public byte DebugDisplayLevel
		{
			[OgreVersion( 1, 7, 2 )]
			get { return mDebugDisplayLvl; }

			/// <remarks>
			/// This setting controls how much debug information is displayed in the scene.
			/// The exact interpretation of this value depends on the PageStrategy you're
			/// using, and whether the PageContent decides to also display debug information.
			/// Generally speaking, 0 means no debug display, 1 means simple debug
			/// display (usually the PageStrategy) and anything more than that is
			/// up to the classes involved. 
			/// </remarks>
			[OgreVersion( 1, 7, 2 )]
			set { mDebugDisplayLvl = value; }
		}

		/// <summary>
		/// Returns a list of camerasl being tracked.
		/// </summary>
		public List<Camera> CameraList
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return mCameraList;
			}
		}

		/// <summary>
		/// Get/Set the resource group that will be used to read/write files when the
		//  default load routines are used. 
		/// </summary>
		public string PageResourceGroup
		{
			[OgreVersion( 1, 7, 2 )]
			get { return mPageResourceGroup; }

			[OgreVersion( 1, 7, 2 )]
			set { mPageResourceGroup = value; }
		}

		/// <summary>
		/// Get/set whether paging operations are currently allowed to happen.
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public bool ArePagingOperationsEnabled
		{
			get;

			// Using this method you can stop pages being loaded or unloaded for a
			// period of time, 'freezing' the current page state as it is. 
			set;
		}

		/// <summary>
		/// The PageManager is the entry point through which you load all PagedWorld instances, 
		/// and the place where PageStrategy instances and factory classes are
		/// registered to customise the paging behaviour.
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public PageManager()
			: base()
		{
			mPageResourceGroup = ResourceGroupManager.DefaultResourceGroupName;
			this.ArePagingOperationsEnabled = true;

			mEventRouter = new EventRouter();
			mEventRouter.pManager = this;
			mEventRouter.pWorlds = mWorlds;
			mEventRouter.pCameraList = mCameraList;

			Root.Instance.FrameStarted += mEventRouter.FrameStarted;
			Root.Instance.FrameEnded += mEventRouter.FrameEnded;

			CreateStandardStrategies();
			CreateStandardContentFactories();
		}

		[OgreVersion( 1, 7, 2, "~PageManager" )]
		protected override void dispose( bool disposeManagedResources )
		{
			if ( !this.IsDisposed )
			{
				if ( disposeManagedResources )
				{
					Root.Instance.FrameStarted -= mEventRouter.FrameStarted;
					Root.Instance.FrameEnded -= mEventRouter.FrameEnded;

					mGrid2DPageStrategy.Dispose();
					mSimpleCollectionFactory.Dispose();
				}
			}

			base.dispose( disposeManagedResources );
		}

		[OgreVersion( 1, 7, 2 )]
		protected void CreateStandardStrategies()
		{
			mGrid2DPageStrategy = new Grid2PageStrategy( this );
			AddStrategy( mGrid2DPageStrategy );
		}

		[OgreVersion( 1, 7, 2 )]
		protected void CreateStandardContentFactories()
		{
			// collection
			mSimpleCollectionFactory = new SimplePageContentCollectionFactory();
			AddContentCollectionFactory( mSimpleCollectionFactory );
		}

		/// <summary>
		/// Create a new PagedWorld instance. 
		/// </summary>
		/// <param name="name">Optionally give a name to the world (if no name is given, one
		///	will be generated).</param>
		[OgreVersion( 1, 7, 2 )]
#if NET_40
        public PagedWorld CreateWorld( string name = "" )
#else
		public PagedWorld CreateWorld( string name )
#endif
		{
			string theName = name;
			if ( theName == string.Empty )
			{
				do
				{
					theName = mWorldNameGenerator.GetNextUniqueName();
				}
				while ( mWorlds.ContainsKey( theName ) );
			}
			else if ( mWorlds.ContainsKey( theName ) )
			{
				throw new AxiomException( "World named '{0}' allready exists! PageManager.CreateWorld", theName );
			}

			PagedWorld ret = new PagedWorld( theName, this );
			mWorlds.Add( theName, ret );

			return ret;
		}

#if !NET_40
        /// <see cref="PagedWorld.CreateWorld(string)"/>
        public PagedWorld CreateWorld()
        {
            return CreateWorld( string.Empty );
        }
#endif

        /// <summary>
		/// Destroy a world.
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public void DestroyWorld( string name )
		{
			if ( mWorlds.ContainsKey( name ) )
			{
				mWorlds[ name ].Dispose();
				mWorlds.Remove( name );
			}
		}

		/// <summary>
		/// Destroy a world.
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public void DestroyWorld( PagedWorld world )
		{
			this.DestroyWorld( world.Name );
		}

		/// <summary>
		/// Load a new PagedWorld from a file. 
		/// </summary>
		/// <param name="fileName">The name of the file to load (standard is .world)</param>
		/// <param name="name">Optionally give a name to the world (if no name is given, one
		/// will be generated).</param>
		[OgreVersion( 1, 7, 2 )]
#if NET_40
        public PagedWorld LoadWorld( string fileName, string name = "" )
#else
		public PagedWorld LoadWorld( string fileName, string name )
#endif
		{
			PagedWorld ret = CreateWorld( name );

			StreamSerializer ser = ReadWorldStream( fileName );
			ret.Load( ser );
			ser.Dispose();
			ser = null;

			return ret;
		}

#if !NET_40
        /// <see cref="PagedWorld.LoadWorld(string, string)"/>
		public PagedWorld LoadWorld( string fileName )
		{
			return LoadWorld( fileName, string.Empty );
		}
#endif

		/// <summary>
		/// Load a new PagedWorld from a stream. 
		/// </summary>
		/// <param name="stream">A stream from which to load the world data</param>
		/// <param name="name">Optionally give a name to the world (if no name is given, one
		/// will be generated).</param>
		[OgreVersion( 1, 7, 2 )]
#if NET_40
        public PagedWorld LoadWorld( Stream stream, string name = "" )
#else
		public PagedWorld LoadWorld( Stream stream, string name )
#endif
		{
			PagedWorld ret = CreateWorld( name );

			ret.Load( stream );

			return ret;
		}

#if !NET_40
        /// <see cref="PagedWorld.LoadWorld(Stream, string)"/>
		public PagedWorld LoadWorld( Stream stream )
		{
			return LoadWorld( stream, string.Empty );
		}
#endif

		/// <summary>
		/// Save a PagedWorld instance to a file. 
		/// </summary>
		/// <param name="world">The world to be saved</param>
		/// <param name="fileName">The filename to save the data to</param>
		[OgreVersion( 1, 7, 2 )]
		public void SaveWorld( PagedWorld world, string fileName )
		{
			world.Save( fileName );
		}

		/// <summary>
		/// Save a PagedWorld instance to a file. 
		/// </summary>
		/// <param name="world">The world to be saved</param>
		/// <param name="stream">The stream to save the data to</param>
		[OgreVersion( 1, 7, 2 )]
		public void SaveWorld( PagedWorld world, Stream stream )
		{
			world.Save( stream );
		}

		/// <summary>
		/// Get a named world.
		/// </summary>
		/// <param name="name">The name of the world (not a filename, the identifying name)</param>
		/// <returns>The world, or null if the world doesn't exist.</returns>
		[OgreVersion( 1, 7, 2 )]
		public PagedWorld GetWorld( string name )
		{
			PagedWorld ret = null;
			mWorlds.TryGetValue( name, out ret );
			return ret;
		}

		/// <summary>
		/// Add a new PageStrategy implementation. 
		/// </summary>
		/// <remarks>
		/// The caller remains resonsible for destruction of this instance.
		/// </remarks>
		[OgreVersion( 1, 7, 2 )]
		public void AddStrategy( PageStrategy strategy )
		{
			// note - deliberately allowing overriding
			if ( mStrategies.ContainsKey( strategy.Name ) )
			{
				mStrategies[ strategy.Name ] = strategy;
				return;
			}

			mStrategies.Add( strategy.Name, strategy );
		}

		/// <summary>
		/// Remove a PageStrategy implementation.
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public void RemoveStrategy( PageStrategy strategy )
		{
			if ( mStrategies.ContainsKey( strategy.Name ) )
				mStrategies.Remove( strategy.Name );
		}

		/// <summary>
		/// Get a PageStrategy.
		/// </summary>
		/// <param name="stratName">The name of the strategy to retrieve</param>
		/// <returns>Pointer to a PageStrategy, or null if the strategy was not found.</returns>
		[OgreVersion( 1, 7, 2 )]
		public PageStrategy GetStrategy( string stratName )
		{
			PageStrategy ret;
			mStrategies.TryGetValue( stratName, out ret );
			return ret;
		}

		/// <summary>
		/// Add a new PageContentCollectionFactory implementation.
		/// </summary>
		/// <remarks>
		/// The caller remains resonsible for destruction of this instance.
		/// </remarks>
		[OgreVersion( 1, 7, 2 )]
		public void AddContentCollectionFactory( IPageContentCollectionFactory f )
		{
			// note - deliberately allowing overriding
			if ( mContentCollectionFactories.ContainsKey( f.Name ) )
			{
				mContentCollectionFactories[ f.Name ] = f;
				return;
			}

			mContentCollectionFactories.Add( f.Name, f );
		}

		/// <summary>
		/// Remove a PageContentCollectionFactory implementation. 
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public void RemoveContentCollectionFactory( IPageContentCollectionFactory f )
		{
			if ( mContentCollectionFactories.ContainsKey( f.Name ) )
				mContentCollectionFactories.Remove( f.Name );
		}

		/// <summary>
		/// Get a PageContentCollectionFactory.
		/// </summary>
		/// <param name="name">The name of the factory to retrieve</param>
		/// <returns>Pointer to a PageContentCollectionFactory, or null if the ContentCollection was not found.</returns>
		[OgreVersion( 1, 7, 2 )]
		public IPageContentCollectionFactory GetContentCollectionFactory( string name )
		{
			IPageContentCollectionFactory factory;
			mContentCollectionFactories.TryGetValue( name, out factory );
			return factory;
		}

		/// <summary>
		/// Create a new instance of PageContentCollection using the registered factories.
		/// </summary>
		/// <param name="typeName">The name of the type of collection to create</param>
		[OgreVersion( 1, 7, 2 )]
		public PageContentCollection CreateContentCollection( string typeName )
		{
			IPageContentCollectionFactory fact = GetContentCollectionFactory( typeName );
			if ( fact == null )
				throw new AxiomException( "{0} is not the of a valid PageContentCollectionFactory! PageManager.CreateContentCollection", typeName );

			return fact.CreateInstance();
		}

		/// <summary>
		/// Destroy an instance of PageContentCollection.
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public void DestroyContentCollection( ref PageContentCollection coll )
		{
			IPageContentCollectionFactory fact = GetContentCollectionFactory( coll.Type );
			if ( fact != null )
				fact.DestroyInstance( ref coll );
			else
				coll.Dispose(); //normally a safe fallback
		}

		/// <summary>
		/// Add a new PageContentFactory implementation.
		/// </summary>
		/// <remarks>The caller remains resonsible for destruction of this instance.</remarks>
		[OgreVersion( 1, 7, 2 )]
		public void AddContentFactory( IPageContentFactory f )
		{
			// note - deliberately allowing overriding
			if ( mContentFactories.ContainsKey( f.Name ) )
			{
				mContentFactories[ f.Name ] = f;
				return;
			}

			mContentFactories.Add( f.Name, f );
		}

		/// <summary>
		/// Remove a PageContentFactory implementation. 
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public void RemoveContentFactory( IPageContentFactory f )
		{
			if ( mContentFactories.ContainsKey( f.Name ) )
				mContentFactories.Remove( f.Name );
		}

		/// <summary>
		/// Get a PageContentFactory.
		/// </summary>
		/// <param name="name">The name of the factory to retrieve</param>
		/// <returns>Pointer to a PageContentFactory, or null if the Content was not found.</returns>
		[OgreVersion( 1, 7, 2 )]
		public IPageContentFactory GetContentFactory( string name )
		{
			IPageContentFactory f;
			mContentFactories.TryGetValue( name, out f );
			return f;
		}

		/// <summary>
		/// Create a new instance of PageContent using the registered factories.
		/// </summary>
		/// <param name="typeName">The name of the type of content to create</param>
		[OgreVersion( 1, 7, 2 )]
		public PageContent CreateContent( string typeName )
		{
			IPageContentFactory fact = GetContentFactory( typeName );
			if ( fact == null )
				throw new AxiomException( "{0} is not the name of a valid PageContentFactory! PageManager.CreateContent", typeName );

			return fact.CreateInstance();
		}

		/// <summary>
		/// Destroy an instance of PageContent.
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public void DestroyContent( ref PageContent c )
		{
			IPageContentFactory fact = GetContentFactory( c.Type );
			if ( fact != null )
				fact.DestroyInstance( ref c );
			else
				c.Dispose();// normally a safe fallback
		}

		/// <summary>
		/// Add a new PagedWorldSectionFactory implementation.
		/// </summary>
		/// <remarks>The caller remains resonsible for destruction of this instance.</remarks>
		[OgreVersion( 1, 7, 2 )]
		public void AddWorldSectionFactory( PagedWorldSectionFactory f )
		{
			// note - deliberately allowing overriding
			if ( mWorldSectionFactories.ContainsKey( f.Name ) )
			{
				mWorldSectionFactories[ f.Name ] = f;
				return;
			}

			mWorldSectionFactories.Add( f.Name, f );
		}

		/// <summary>
		/// Remove a PagedWorldSectionFactory implementation.
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public void RemoveWorldSectionFactory( PagedWorldSectionFactory f )
		{
			if ( mWorldSectionFactories.ContainsKey( f.Name ) )
				mWorldSectionFactories.Remove( f.Name );
		}

		/// <summary>
		/// Get a PagedWorldSectionFactory.
		/// </summary>
		/// <param name="name">The name of the factory to retrieve</param>
		/// <returns>Pointer to a PagedWorldSectionFactory, or null if the WorldSection was not found.</returns>
		[OgreVersion( 1, 7, 2 )]
		public PagedWorldSectionFactory GetWorldSectionFactory( string name )
		{
			PagedWorldSectionFactory ret;
			mWorldSectionFactories.TryGetValue( name, out ret );
			return ret;
		}

		/// <summary>
		/// Create a new instance of PagedWorldSection using the registered factories.
		/// </summary>
		/// <param name="typeName">The name of the type of collection to create</param>
		/// <param name="name">The instance name</param>
		/// <param name="parent">The parent world</param>
		/// <param name="sm">The SceneManager to use (can be null if this is to be loaded)</param>
		[OgreVersion( 1, 7, 2 )]
		public PagedWorldSection CreateWorldSection( string typeName, string name, PagedWorld parent, SceneManager sm )
		{
			PagedWorldSectionFactory fact = this.GetWorldSectionFactory( typeName );
			if ( fact == null )
				throw new AxiomException( "{0} is not the name of a valid PagedWorldSectionFactory PageManager.CreateWorldSection", typeName );

			return fact.CreateInstance( name, parent, sm );
		}

		/// <summary>
		/// Destroy an instance of PagedWorldSection.
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public void DestroyWorldSection( ref PagedWorldSection coll )
		{
			PagedWorldSectionFactory fact = this.GetWorldSectionFactory( coll.Type );
			if ( fact != null )
				fact.DestroyInstance( ref coll );
			else
				coll.Dispose(); // normally a safe fallback
		}

		/// <summary>
		/// Get a serialiser set up to read Page data for the given PageID.
		/// </summary>
		/// <param name="pageId">The ID of the page being requested</param>
		/// <param name="section">The parent section to which this page will belong</param>
		/// <remarks>The StreamSerialiser returned is the responsibility of the caller to delete.</remarks>
		[OgreVersion( 1, 7, 2 )]
		internal StreamSerializer ReadPageStream( PageID pageId, PagedWorldSection section )
		{
			StreamSerializer ser = null;
			if ( mPageProvider != null )
				ser = mPageProvider.ReadPageStream( pageId, section );
			if ( ser == null )
			{
				// use default implementation
				string nameStr = string.Format( "{0}_{1}_{2}.page", section.World.Name, section.Name, pageId );
				var stream = ResourceGroupManager.Instance.OpenResource( nameStr );
				ser = new StreamSerializer( stream );
			}

			return ser;
		}

		/// <summary>
		/// Get a serialiser set up to write Page data for the given PageID.
		/// </summary>
		/// <param name="pageId">The ID of the page being requested</param>
		/// <param name="section">The parent section to which this page will belong</param>
		/// <remarks>The StreamSerialiser returned is the responsibility of the caller to delete.</remarks>
		[OgreVersion( 1, 7, 2 )]
		internal StreamSerializer WritePageStream( PageID pageId, PagedWorldSection section )
		{
			StreamSerializer ser = null;

			if ( mPageProvider != null )
				ser = mPageProvider.WritePageStream( pageId, section );
			
			if ( ser == null )
			{
				// use default implementation
				string nameStr = string.Format( "{0}_{1}_{2}.page", section.World.Name, section.Name, pageId );
				// create file, overwrite if necessary
				var stream = ResourceGroupManager.Instance.CreateResource( nameStr, mPageResourceGroup, true );
				ser = new StreamSerializer( stream );
			}

			return ser;
		}

		/// <summary>
		/// Get a serialiser set up to read PagedWorld data for the given world name.
		/// </summary>
		/// <remarks>The StreamSerialiser returned is the responsibility of the caller to delete.</remarks>
		[OgreVersion( 1, 7, 2 )]
		internal StreamSerializer ReadWorldStream( string fileName )
		{
			StreamSerializer ser = null;
			if ( mPageProvider != null )
				ser = mPageProvider.ReadWorldStream( fileName );
			
			if ( ser == null )
			{
				// use default implementation
				Stream stream = ResourceGroupManager.Instance.OpenResource( fileName );
				ser = new StreamSerializer( stream );
			}

			return ser;
		}

		/// <summary>
		/// Get a serialiser set up to write PagedWorld data.
		/// </summary>
		/// <reremarks>The StreamSerialiser returned is the responsibility of the caller to	delete.</reremarks>
		[OgreVersion( 1, 7, 2 )]
		internal StreamSerializer WriteWorldStream( string fileName )
		{
			StreamSerializer ser = null;
			if ( mPageProvider != null )
				ser = mPageProvider.WriteWorldStream( fileName );
			
			if ( ser == null )
			{
				// use default implementation
				// create file, overwrite if necessary
				var stream = ResourceGroupManager.Instance.CreateResource( fileName, mPageResourceGroup, true );
				ser = new StreamSerializer( stream );
			}

			return ser;
		}

		/// <summary>
		/// Give a provider the opportunity to prepare page content procedurally.
		/// </summary>
		/// <remarks>
		/// You should not call this method directly. This call may well happen in 
		/// a separate thread so it should not access GPU resources, use _loadProceduralPage for that
		/// </remarks>
		/// <returns>true if the page was populated, false otherwise</returns>
		[OgreVersion( 1, 7, 2 )]
		internal bool PrepareProcedualPage( Page page, PagedWorldSection section )
		{
			bool generated = false;
			if ( mPageProvider != null )
				generated = mPageProvider.PrepareProcedualPage( page, section );

			return generated;
		}

		/// <summary>
		/// Give a provider the opportunity to prepare page content procedurally. 
		/// </summary>
		/// <remarks>
		/// You should not call this method directly. This call will happen in 
		/// the main render thread so it can access GPU resources. Use _prepareProceduralPage
		/// for background preparation.
		/// </remarks>
		/// <returns>true if the page was populated, false otherwise</returns>
		[OgreVersion( 1, 7, 2 )]
		internal bool LoadProcedualPage( Page page, PagedWorldSection section )
		{
			bool generated = false;
			if ( mPageProvider != null )
				generated = mPageProvider.LoadProcedualPage( page, section );

			return generated;
		}

		/// <summary>
		/// Give a manager the opportunity to unprepare page content procedurally.
		/// </summary>
		/// <remarks>
		/// You should not call this method directly. This call may well happen in 
		/// a separate thread so it should not access GPU resources, use _unloadProceduralPage for that
		/// </remarks>
		/// <returns>true if the page was unpopulated, false otherwise</returns>
		[OgreVersion( 1, 7, 2 )]
		internal bool UnPrepareProcedualPage( Page page, PagedWorldSection section )
		{
			bool generated = false;

			if ( mPageProvider != null )
				generated = mPageProvider.UnPrepareProcedualPage( page, section );

			return generated;
		}

		/// <summary>
		/// Give a manager  the opportunity to unload page content procedurally.
		/// </summary>
		/// <remarks>
		/// You should not call this method directly. This call will happen in 
		/// the main render thread so it can access GPU resources. Use _unprepareProceduralPage
		/// for background preparation.
		/// </remarks>
		/// <returns>true if the page was populated, false otherwise</returns>
		[OgreVersion( 1, 7, 2 )]
		internal bool UnloadProcedualPage( Page page, PagedWorldSection section )
		{
			bool generated = false;
			if ( mPageProvider != null )
				generated = mPageProvider.UnloadProcedualPage( page, section );

			return generated;
		}

		/// <summary>
		/// Tells the paging system to start tracking a given camera.
		/// </summary>
		/// <remarks>
		/// In order for the paging system to funciton it needs to know which
		/// Cameras to track. You may not want to have all your cameras affect
		/// the paging system, so just add the cameras you want it to keep track of	here.
		/// </remarks>
		[OgreVersion( 1, 7, 2 )]
		public void AddCamera( Camera c )
		{
			if ( !mCameraList.Contains( c ) )
			{
				mCameraList.Add( c );
				c.CameraPreRenderScene += mEventRouter.OnPreRenderScene;
				c.CameraDestroyed += mEventRouter.OnCameraDestroyed;
			}
		}

		/// <summary>
		/// Tells the paging system to stop tracking a given camera.
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public void RemoveCamera( Camera c )
		{
			if ( mCameraList.Contains( c ) )
			{
				c.CameraPreRenderScene -= mEventRouter.OnPreRenderScene;
				c.CameraDestroyed -= mEventRouter.OnCameraDestroyed;
				mCameraList.Remove( c );
			}
		}

		/// <summary>
		/// Returns whether or not a given camera is being watched by the paging system.
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public bool HasCamera( Camera c )
		{
			return mCameraList.Contains( c );
		}

		protected class EventRouter
		{
			public PageManager pManager;
			public Dictionary<string, PagedWorld> pWorlds;
			public List<Camera> pCameraList;

			[OgreVersion( 1, 7, 2 )]
			public void FrameStarted( object source, FrameEventArgs e )
			{
				foreach ( var world in pWorlds.Values )
				{
					world.FrameStart( e.TimeSinceLastFrame );

					// Notify of all active cameras
					// Previously we did this in cameraPreRenderScene, but that had the effect
					// of causing unnecessary unloading of pages if a camera was rendered
					// intermittently, so we assume that all cameras we're told to watch are 'active'
					foreach ( var c in pCameraList )
						world.NotifyCamera( c );
				}
			}

			[OgreVersion( 1, 7, 2 )]
			public void FrameEnded( object source, FrameEventArgs e )
			{
				foreach ( var world in pWorlds.Values )
					world.FrameEnd( e.TimeSinceLastFrame );
			}

			[OgreVersion( 1, 7, 2 )]
			public void OnPreRenderScene( Camera.CameraEventArgs e )
			{
			}

			[OgreVersion( 1, 7, 2 )]
			public void OnCameraDestroyed( Camera.CameraEventArgs e )
			{
				pManager.RemoveCamera( e.Source );
			}
		}
	}
}
