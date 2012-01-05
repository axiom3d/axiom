using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

using Axiom.Core;
using Axiom.Graphics;
using Axiom.Math;
using Axiom.Overlays;

namespace Axiom.Demos
{
	internal class LoadingBar : IResourceGroupListener
	{
		protected RenderWindow mWindow;
		protected Overlay mLoadOverlay;
		protected Real mInitProportion;
		protected ushort mNumGroupsInit;
		protected ushort mNumGroupsLoad;
		protected Real mProgressBarMaxSize;
		protected Real mProgressBarScriptSize;
		protected Real mProgressBarInc;
		protected OverlayElement mLoadingBarElement;
		protected OverlayElement mLoadingDescriptionElement;
		protected OverlayElement mLoadingCommentElement;

		/** Show the loading bar and start listening.
		@param window The window to update
		@param numGroupsInit The number of groups you're going to be initialising
		@param numGroupsLoad The number of groups you're going to be loading
		@param initProportion The proportion of the progress which will be taken
			up by initialisation (ie script parsing etc). Defaults to 0.7 since
			script parsing can often take the majority of the time.
		*/

		virtual public void Start( RenderWindow window )
		{
			Start( window, 1, 1, 0.7f );
		}

		virtual public void Start( RenderWindow window, ushort numGroupsInit, ushort numGroupsLoad )
		{
			Start( window, numGroupsInit, numGroupsLoad, 0.7f );
		}

		virtual public void Start( RenderWindow window, ushort numGroupsInit, ushort numGroupsLoad, Real initProportion )
		{
			mWindow = window;
			mNumGroupsInit = numGroupsInit;
			mNumGroupsLoad = numGroupsLoad;
			mInitProportion = initProportion;
			// We need to pre-initialise the 'Bootstrap' group so we can use
			// the basic contents in the loading screen
			ResourceGroupManager.Instance.InitializeResourceGroup( "Bootstrap" );

			OverlayManager omgr = OverlayManager.Instance;
			mLoadOverlay = omgr.GetByName( "Core/LoadOverlay" );
			if( mLoadOverlay == null )
			{
				throw new KeyNotFoundException( "Cannot find loading overlay" );
			}
			mLoadOverlay.Show();

			// Save links to the bar and to the loading text, for updates as we go
			mLoadingBarElement = omgr.Elements.GetElement( "Core/LoadPanel/Bar/Progress" );
			mLoadingCommentElement = omgr.Elements.GetElement( "Core/LoadPanel/Comment" );
			mLoadingDescriptionElement = omgr.Elements.GetElement( "Core/LoadPanel/Description" );

			OverlayElement barContainer = omgr.Elements.GetElement( "Core/LoadPanel/Bar" );
			mProgressBarMaxSize = barContainer.Width;
			mLoadingBarElement.Width = 0;

			// self is listener
			ResourceGroupManager.Instance.AddResourceGroupListener( this );
		}

		/** Hide the loading bar and stop listening.
		*/

		virtual public void Finish()
		{
			// hide loading screen
			mLoadOverlay.Hide();

			// Unregister listener
			ResourceGroupManager.Instance.RemoveResourceGroupListener( this );
		}

		protected Stream resourceLoading( string name, string group, Resource resource )
		{
			return new MemoryStream();
		}

		protected bool resourceCollision( Resource resource, ResourceManager resourceManager )
		{
			return false;
		}

		#region IResourceGroupListener Members

		public void ResourceGroupScriptingStarted( string groupName, int scriptCount )
		{
			Debug.Assert( mNumGroupsInit > 0, "You stated you were not going to init any groups, but you did! Divide by zero would follow..." );
			// Lets assume script loading is 70%
			mProgressBarInc = mProgressBarMaxSize * mInitProportion / (Real)scriptCount;
			mProgressBarInc /= mNumGroupsInit;
			mLoadingDescriptionElement.Text = "Parsing scripts...";
			mWindow.Update();
		}

		public void ScriptParseStarted( string scriptName, ref bool skipThisScript )
		{
			mLoadingCommentElement.Text = scriptName;
			mWindow.Update();
		}

		public void ScriptParseEnded( string scriptName, bool skipped )
		{
			mLoadingBarElement.Width += mProgressBarInc;
			mWindow.Update();
		}

		public void ResourceGroupScriptingEnded( string groupName ) {}

		public void ResourceGroupLoadStarted( string groupName, int resourceCount )
		{
			Debug.Assert( mNumGroupsInit > 0, "You stated you were not going to init any groups, but you did! Divide by zero would follow..." );
			mProgressBarInc = mProgressBarMaxSize * ( 1 - mInitProportion ) / (Real)resourceCount;
			mProgressBarInc /= mNumGroupsLoad;
			mLoadingDescriptionElement.Text = "Loading resources...";
			mWindow.Update();
		}

		public void ResourceLoadStarted( Resource resource )
		{
			mLoadingDescriptionElement.Text = resource.Name;
			mWindow.Update();
		}

		public void ResourceLoadEnded() {}

		public void WorldGeometryStageStarted( string description )
		{
			mLoadingDescriptionElement.Text = description;
			mWindow.Update();
		}

		public void WorldGeometryStageEnded()
		{
			mLoadingBarElement.Width += mProgressBarInc;
			mWindow.Update();
		}

		public void ResourceGroupLoadEnded( string groupName ) {}

		public void ResourceGroupPrepareStarted( string groupName, int resourceCount ) {}

		public void ResourcePrepareStarted( Resource resource ) {}

		public void ResourcePrepareEnded() {}

		public void ResourceGroupPrepareEnded( string groupName ) {}

		#endregion IResourceGroupListener Members
	}
}
