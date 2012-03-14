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
		protected Real mInitProportion;
		protected Overlay mLoadOverlay;
		protected OverlayElement mLoadingBarElement;
		protected OverlayElement mLoadingCommentElement;
		protected OverlayElement mLoadingDescriptionElement;
		protected ushort mNumGroupsInit;
		protected ushort mNumGroupsLoad;
		protected Real mProgressBarInc;
		protected Real mProgressBarMaxSize;
		protected Real mProgressBarScriptSize;
		protected RenderWindow mWindow;

		#region IResourceGroupListener Members

		public void ResourceGroupScriptingStarted( string groupName, int scriptCount )
		{
			Debug.Assert( this.mNumGroupsInit > 0, "You stated you were not going to init any groups, but you did! Divide by zero would follow..." );
			// Lets assume script loading is 70%
			this.mProgressBarInc = this.mProgressBarMaxSize * this.mInitProportion / scriptCount;
			this.mProgressBarInc /= this.mNumGroupsInit;
			this.mLoadingDescriptionElement.Text = "Parsing scripts...";
			this.mWindow.Update();
		}

		public void ScriptParseStarted( string scriptName, ref bool skipThisScript )
		{
			this.mLoadingCommentElement.Text = scriptName;
			this.mWindow.Update();
		}

		public void ScriptParseEnded( string scriptName, bool skipped )
		{
			this.mLoadingBarElement.Width += this.mProgressBarInc;
			this.mWindow.Update();
		}

		public void ResourceGroupScriptingEnded( string groupName ) { }

		public void ResourceGroupLoadStarted( string groupName, int resourceCount )
		{
			Debug.Assert( this.mNumGroupsInit > 0, "You stated you were not going to init any groups, but you did! Divide by zero would follow..." );
			this.mProgressBarInc = this.mProgressBarMaxSize * ( 1 - this.mInitProportion ) / resourceCount;
			this.mProgressBarInc /= this.mNumGroupsLoad;
			this.mLoadingDescriptionElement.Text = "Loading resources...";
			this.mWindow.Update();
		}

		public void ResourceLoadStarted( Resource resource )
		{
			this.mLoadingDescriptionElement.Text = resource.Name;
			this.mWindow.Update();
		}

		public void ResourceLoadEnded() { }

		public void WorldGeometryStageStarted( string description )
		{
			this.mLoadingDescriptionElement.Text = description;
			this.mWindow.Update();
		}

		public void WorldGeometryStageEnded()
		{
			this.mLoadingBarElement.Width += this.mProgressBarInc;
			this.mWindow.Update();
		}

		public void ResourceGroupLoadEnded( string groupName ) { }

		public void ResourceGroupPrepareStarted( string groupName, int resourceCount ) { }

		public void ResourcePrepareStarted( Resource resource ) { }

		public void ResourcePrepareEnded() { }

		public void ResourceGroupPrepareEnded( string groupName ) { }

		#endregion

		public virtual void Start( RenderWindow window )
		{
			Start( window, 1, 1, 0.7f );
		}

		public virtual void Start( RenderWindow window, ushort numGroupsInit, ushort numGroupsLoad )
		{
			Start( window, numGroupsInit, numGroupsLoad, 0.7f );
		}

		public virtual void Start( RenderWindow window, ushort numGroupsInit, ushort numGroupsLoad, Real initProportion )
		{
			this.mWindow = window;
			this.mNumGroupsInit = numGroupsInit;
			this.mNumGroupsLoad = numGroupsLoad;
			this.mInitProportion = initProportion;
			// We need to pre-initialise the 'Bootstrap' group so we can use
			// the basic contents in the loading screen
			ResourceGroupManager.Instance.InitializeResourceGroup( "Bootstrap" );

			OverlayManager omgr = OverlayManager.Instance;
			this.mLoadOverlay = omgr.GetByName( "Core/LoadOverlay" );
			if ( this.mLoadOverlay == null )
			{
				throw new KeyNotFoundException( "Cannot find loading overlay" );
			}
			this.mLoadOverlay.Show();

			// Save links to the bar and to the loading text, for updates as we go
			this.mLoadingBarElement = omgr.Elements.GetElement( "Core/LoadPanel/Bar/Progress" );
			this.mLoadingCommentElement = omgr.Elements.GetElement( "Core/LoadPanel/Comment" );
			this.mLoadingDescriptionElement = omgr.Elements.GetElement( "Core/LoadPanel/Description" );

			OverlayElement barContainer = omgr.Elements.GetElement( "Core/LoadPanel/Bar" );
			this.mProgressBarMaxSize = barContainer.Width;
			this.mLoadingBarElement.Width = 0;

			// self is listener
			ResourceGroupManager.Instance.AddResourceGroupListener( this );
		}

		/** Hide the loading bar and stop listening.
		*/

		public virtual void Finish()
		{
			// hide loading screen
			this.mLoadOverlay.Hide();

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
	}
}
