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
using System.IO;

using Axiom.Serialization;
using Axiom.Core;

#endregion Namespace Declarations

namespace Axiom.Components.Paging
{
	public class PageManager
	{
		/// <summary>
		/// 
		/// </summary>
		protected Dictionary<string, PageWorld> mWorlds = new Dictionary<string, PageWorld>();

		/// <summary>
		/// 
		/// </summary>
		protected Dictionary<string, PageStrategy> mStrategies = new Dictionary<string, PageStrategy>();

		/// <summary>
		/// 
		/// </summary>
		protected Dictionary<string, IPageContentCollectionFactory> mContentCollectionFactories = new Dictionary<string, IPageContentCollectionFactory>();

		/// <summary>
		/// 
		/// </summary>
		protected Dictionary<string, IPageContentFactory> mContentFactories = new Dictionary<string, IPageContentFactory>();

		/// <summary>
		/// 
		/// </summary>
		protected NameGenerator<PageWorld> mWorldNameGenerator;

		/// <summary>
		/// 
		/// </summary>
		protected PageRequestQueue mQueue;

		/// <summary>
		/// 
		/// </summary>
		protected PageProvider mPageProvider;

		/// <summary>
		/// 
		/// </summary>
		protected string mPageResourceGroup;

		/// <summary>
		/// 
		/// </summary>
		protected List<Camera> mCameraList = new List<Camera>();

		/// <summary>
		/// 
		/// </summary>
		protected EventRouter mEventRouter;

		/// <summary>
		/// 
		/// </summary>
		protected uint mDebugDisplayLvl;

		/// <summary>
		/// 
		/// </summary>
		protected Grid2PageStrategy mGrid2DPageStrategy;

		/// <summary>
		/// 
		/// </summary>
		protected SimplePageContentCollectionFactory mSimpleCollectionFactory;

		/// <summary>
		/// Set the PageProvider which can provide streams for any Page.
		/// </summary>
		public PageProvider PageProvider { set { mPageProvider = value; } get { return mPageProvider; } }

		/// <summary>
		/// 
		/// </summary>
		public Dictionary<string, IPageContentFactory> Factories { get { return mContentFactories; } }

		/// <summary>
		/// 
		/// </summary>
		public Dictionary<string, PageStrategy> Strategies { get { return mStrategies; } }

		/// <summary>
		/// 
		/// </summary>
		public Dictionary<string, PageWorld> Worlds { get { return mWorlds; } }

		/// <summary>
		/// Set the debug display level.
		/// </summary>
		public uint DebugDisplayLevel { set { mDebugDisplayLvl = value; } get { return mDebugDisplayLvl; } }

		/// <summary>
		/// 
		/// </summary>
		public List<Camera> CameraList { get { return mCameraList; } }

		/// <summary>
		/// 
		/// </summary>
		public string PageResourceGroup { set { mPageResourceGroup = value; } get { return mPageResourceGroup; } }

		/// <summary>
		/// 
		/// </summary>
		public PageRequestQueue Queue { get { return mQueue; } }

		/// <summary>
		/// The PageManager is the entry point through which you load all PagedWorld instances, 
		/// and the place where PageStrategy instances and factory classes are
		/// registered to customise the paging behaviour.
		/// </summary>
		public PageManager()
		{
			mQueue = new PageRequestQueue( this );
			mPageResourceGroup = ResourceGroupManager.DefaultResourceGroupName;

			mEventRouter = new EventRouter();
			mEventRouter.pManager = this;
			mEventRouter.pWorlds = mWorlds;

			Root.Instance.FrameStarted += mEventRouter.FrameStarted;
			Root.Instance.FrameEnded += mEventRouter.FrameEnded;

			CreateStandardStrategies();
			CreateStandardContentFactories();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public PageWorld CreateWorld()
		{
			return CreateWorld( string.Empty );
		}

		/// <summary>
		/// Create a new PagedWorld instance. 
		/// </summary>
		/// <param name="name">name Optionally give a name to the world (if no name is given, one
		///	will be generated).</param>
		/// <returns></returns>
		public PageWorld CreateWorld( string name )
		{
			string theName = name;
			if( theName == string.Empty )
			{
				do
				{
					theName = mWorldNameGenerator.GetNextUniqueName();
				}
				while( !mWorlds.ContainsKey( theName ) );
			}
			else if( mWorlds.ContainsKey( theName ) )
			{
				throw new Exception( "World named '" + theName + "' allready exists!" +
				                     "PageManager.CreateWorld" );
			}

			PageWorld ret = new PageWorld( theName, this );
			mWorlds.Add( theName, ret );
			return ret;
		}

		/// <summary>
		/// Destroy a world.
		/// </summary>
		/// <param name="name"></param>
		public void DestroyWorld( string name )
		{
			PageWorld world;
			if( mWorlds.TryGetValue( name, out world ) )
			{
				DestroyWorld( world );
			}
		}

		/// <summary>
		/// Destroy a world.
		/// </summary>
		/// <param name="world"></param>
		public void DestroyWorld( PageWorld world )
		{
			mWorlds.Remove( world.Name );
			world = null;
		}

		/// <summary>
		/// Attach a pre-created PagedWorld instance to this manager. 
		/// </summary>
		/// <param name="world"></param>
		public void AttachWorld( PageWorld world )
		{
			if( mWorlds.ContainsKey( world.Name ) )
			{
				throw new Exception( "World named '" + world.Name + "' allready exists!" +
				                     "PageManager.AttachWorld" );
			}

			mWorlds.Add( world.Name, world );
		}

		/// <summary>
		/// Detach a PagedWorld instance from this manager (note: the caller then
		///	becomes responsible for the correct destruction of this instance)
		/// </summary>
		/// <param name="world"></param>
		public void DetachWorld( PageWorld world )
		{
			mWorlds.Remove( world.Name );
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="fileName"></param>
		/// <returns></returns>
		public PageWorld LoadWorld( Stream stream )
		{
			return LoadWorld( stream, string.Empty );
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="fileName"></param>
		/// <param name="name"></param>
		/// <returns></returns>
		public PageWorld LoadWorld( Stream stream, string name )
		{
			PageWorld ret = CreateWorld( name );

			ret.Load( stream );

			return ret;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="fileName"></param>
		/// <returns></returns>
		public PageWorld LoadWorld( string fileName )
		{
			return LoadWorld( fileName, string.Empty );
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="fileName"></param>
		/// <param name="name"></param>
		/// <returns></returns>
		public PageWorld LoadWorld( string fileName, string name )
		{
			PageWorld ret = CreateWorld( name );

			StreamSerializer ser = ReadWorldStream( fileName );
			ret.Load( ser );
			ser = null;

			return ret;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="stream"></param>
		public void SaveWorld( PageWorld world, string fileName )
		{
			world.Save( fileName );
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="stream"></param>
		/// <param name="name"></param>
		public void SaveWorld( PageWorld world, Stream stream )
		{
			world.Save( stream );
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="strategy"></param>
		public void AddStrategy( PageStrategy strategy )
		{
			// note - deliberately allowing overriding
			if( mStrategies.ContainsKey( strategy.Name ) )
			{
				mStrategies.Remove( strategy.Name );
			}
			mStrategies.Add( strategy.Name, strategy );
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="strategy"></param>
		public void RemoveStrategy( PageStrategy strategy )
		{
			mStrategies.Remove( strategy.Name );
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="f"></param>
		public void AddContentCollectionFactory( IPageContentCollectionFactory f )
		{
			// note - deliberately allowing overriding
			if( mContentCollectionFactories.ContainsKey( f.Name ) )
			{
				mContentCollectionFactories.Remove( f.Name );
			}

			mContentCollectionFactories.Add( f.Name, f );
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="f"></param>
		public void RemoveContentCollectionFactory( IPageContentCollectionFactory f )
		{
			mContentCollectionFactories.Remove( f.Name );
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="f"></param>
		public IPageContentCollectionFactory GetContentCollectionFactory( string name )
		{
			IPageContentCollectionFactory factory;
			mContentCollectionFactories.TryGetValue( name, out factory );
			return factory;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="typeName"></param>
		/// <returns></returns>
		public PageContentCollection CreateContentCollection( string typeName )
		{
			IPageContentCollectionFactory fact = GetContentCollectionFactory( typeName );
			if( fact == null )
			{
				throw new Exception( typeName + " is not the of a valid PageContentCollectionFactory!" +
				                     "PageManager.CreateContentCollection" );
			}

			return fact.CreateInstance();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="f"></param>
		public void AddContentFactory( IPageContentFactory f )
		{
			// note - deliberately allowing overriding
			if( mContentFactories.ContainsKey( f.Name ) )
			{
				mContentFactories.Remove( f.Name );
			}

			mContentFactories.Add( f.Name, f );
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="f"></param>
		public void RemoveContentFactory( IPageContentFactory f )
		{
			mContentFactories.Remove( f.Name );
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public IPageContentFactory GetContentFactory( string name )
		{
			IPageContentFactory f;
			mContentFactories.TryGetValue( name, out f );
			return f;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="coll"></param>
		public void DestroyContentenCollection( PageContentCollection coll )
		{
			IPageContentCollectionFactory fact = GetContentCollectionFactory( coll.Type );
			if( fact != null )
			{
				fact.DestroyInstance( ref coll );
			}
			else
			{
				coll = null; //normally a safe fallback
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public PageWorld GetWorld( string name )
		{
			PageWorld ret = null;

			mWorlds.TryGetValue( name, out ret );

			return ret;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="page"></param>
		/// <param name="section"></param>
		/// <returns></returns>
		public bool PrepareProcedualPage( Page page, PagedWorldSection section )
		{
			bool generated = false;
			if( mPageProvider != null )
			{
				generated = mPageProvider.PrepareProcedualPage( page, section );
			}

			return generated;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="page"></param>
		/// <param name="section"></param>
		/// <returns></returns>
		public bool LoadProcedualPage( Page page, PagedWorldSection section )
		{
			bool generated = false;
			if( mPageProvider != null )
			{
				generated = mPageProvider.LoadProcedualPage( page, section );
			}

			return generated;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="page"></param>
		/// <param name="section"></param>
		/// <returns></returns>
		public bool UnloadProcedualPage( Page page, PagedWorldSection section )
		{
			bool generated = false;
			if( mPageProvider != null )
			{
				generated = mPageProvider.UnloadProcedualPage( page, section );
			}

			return generated;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="page"></param>
		/// <param name="section"></param>
		/// <returns></returns>
		public bool UnPrepareProcedualPage( Page page, PagedWorldSection section )
		{
			bool generated = false;

			if( mPageProvider != null )
			{
				generated = mPageProvider.UnPrepareProcedualPage( page, section );
			}

			return generated;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="pageId"></param>
		/// <param name="section"></param>
		/// <returns></returns>
		public StreamSerializer ReadPageStream( PageID pageId, PagedWorldSection section )
		{
			StreamSerializer ser = null;
			if( mPageProvider != null )
			{
				ser = mPageProvider.ReadPageStream( pageId, section );
			}
			if( ser == null )
			{
				// use default implementation
				string nameStr = string.Empty;
				nameStr += section.World.Name + "_" + section.Name + "_" + pageId.Value + ".page";
				Stream stream = ResourceGroupManager.Instance.OpenResource( nameStr );

				ser = new StreamSerializer( stream );
			}

			return ser;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="pageId"></param>
		/// <param name="section"></param>
		/// <returns></returns>
		public StreamSerializer WritePageStream( PageID pageId, PagedWorldSection section )
		{
			StreamSerializer ser = null;

			if( mPageProvider != null )
			{
				ser = mPageProvider.WritePageStream( pageId, section );
			}
			if( ser == null )
			{
				// use default implementation
				string nameStr = string.Empty;
				nameStr += section.World.Name + "_" + section.Name + "_" + pageId.Value + ".page";

				// create file, overwrite if necessary
			}
			throw new NotImplementedException();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="fileName"></param>
		/// <returns></returns>
		public StreamSerializer ReadWorldStream( string fileName )
		{
			StreamSerializer ser = null;
			if( mPageProvider != null )
			{
				ser = mPageProvider.ReadWorldStream( fileName );
			}
			if( ser == null )
			{
				Stream stream = ResourceGroupManager.Instance.OpenResource( fileName );

				ser = new StreamSerializer( stream );
			}

			return ser;
		}

		public StreamSerializer WriteWorldStream( string fileName )
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="stratName"></param>
		/// <returns></returns>
		public PageStrategy GetStrategy( string stratName )
		{
			PageStrategy ret;

			mStrategies.TryGetValue( stratName, out ret );

			return ret;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="c"></param>
		public void AddCamera( Camera c )
		{
#warning implement camera events
			// c.OnPreRenderScene += mEventRouter.OnPreRenderScene;
			//    c.OnPostRenderScene += mEventRouter.OnPostRenderScene;
			//    c.OnCameraDestroyed += mEventRouter.OnCameraDestroyed;
			mCameraList.Add( c );
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="c"></param>
		public void RemoveCamera( Camera c )
		{
			if( mCameraList.Contains( c ) )
			{
#warning implement camera events
				///     c.OnPreRenderScene -= mEventRouter.OnPreRenderScene;
				//    c.OnPostRenderScene -= mEventRouter.OnPostRenderScene;
				//    c.OnCameraDestroyed -= mEventRouter.OnCameraDestroyed;
				mCameraList.Remove( c );
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="c"></param>
		/// <returns></returns>
		public bool HasCamera( Camera c )
		{
			return mCameraList.Contains( c );
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="typeName"></param>
		/// <returns></returns>
		public PageContent CreateContent( string typeName )
		{
			IPageContentFactory fact = GetContentFactory( typeName );
			if( fact == null )
			{
				throw new Exception( typeName + " is not the name of a valid PageContentFactory!" +
				                     "PageManager.CreateContent" );
			}

			return fact.CreateInstance();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="typeName"></param>
		public void DestroyContent( PageContent c )
		{
			IPageContentFactory fact = GetContentFactory( c.Type );
			if( fact != null )
			{
				fact.DestroyInstance( c );
			}
			else
			{
				c = null; // normally a safe fallback
			}
		}

		/// <summary>
		/// 
		/// </summary>
		protected void CreateStandardStrategies()
		{
			mGrid2DPageStrategy = new Grid2PageStrategy( this );
			AddStrategy( mGrid2DPageStrategy );
		}

		/// <summary>
		/// 
		/// </summary>
		protected void CreateStandardContentFactories()
		{
			mSimpleCollectionFactory = new SimplePageContentCollectionFactory();
			AddContentCollectionFactory( mSimpleCollectionFactory );
		}

		protected class EventRouter
		{
			public PageManager pManager;
			public Dictionary<string, PageWorld> pWorlds = new Dictionary<string, PageWorld>();

			public EventRouter() {}

			/// <summary>
			/// 
			/// </summary>
			/// <param name="evt"></param>
			/// <returns></returns>
			public void FrameStarted( object source, FrameEventArgs e )
			{
				foreach( PageWorld world in pWorlds.Values )
				{
					world.FrameStart( e.TimeSinceLastFrame );
				}

				pManager.Queue.ProcessRenderThreadsRequest();
			}

			/// <summary>
			/// 
			/// </summary>
			/// <param name="evt"></param>
			/// <returns></returns>
			public void FrameEnded( object source, FrameEventArgs e )
			{
				foreach( PageWorld world in pWorlds.Values )
				{
					world.FrameEnd( e.TimeSinceLastFrame );
				}
			}

			/// <summary>
			/// 
			/// </summary>
			/// <param name="camera"></param>
			public void OnPreRenderScene( Camera camera )
			{
				foreach( PageWorld world in pWorlds.Values )
				{
					world.NotifyCamera( camera );
				}
			}

			/// <summary>
			/// 
			/// </summary>
			/// <param name="camera"></param>
			public void OnPostRenderScene( Camera camera ) {}

			/// <summary>
			/// 
			/// </summary>
			/// <param name="camera"></param>
			public void OnCameraDestroyed( Camera camera )
			{
				pManager.RemoveCamera( camera );
			}
		}
	}
}
