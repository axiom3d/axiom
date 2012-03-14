#region LGPL License

/*
Axiom Graphics Engine Library
Copyright © 2003-2011 Axiom Project Team

The overall design, and a majority of the core engine and rendering code
contained within this library is a derivative of the open source Object Oriented
Graphics Engine OGRE, which can be found at http://ogre.sourceforge.net.
Many thanks to the OGRE team for maintaining such a high quality project.

This library is free software; you can redistribute it and/or
modify it under the terms of the GNU Lesser General Public
License as published by the Free Software Foundation; either
version 2.1 of the License, or (at your option) any later version.

This library is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
Lesser General Public License for more details.

You should have received a copy of the GNU Lesser General Public
License along with this library; if not, write to the Free Software
Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
*/
/*
 * Many thanks to the folks at Multiverse for providing the initial port for this class
 */

#endregion LGPL License

#region SVN Version Information

// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <id value="$Id$"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System.Collections.Generic;
using System.Diagnostics;

using Axiom.Core;

#endregion Namespace Declarations

namespace Axiom.Graphics
{
	///<summary>
	///    Chain of compositor effects applying to one viewport.
	///</summary>
	public class CompositorChain : DisposableObject
	{
		#region Nested type: RQListener

		public class RQListener
		{
			///<summary>
			///    The number of the first render system op to be processed by the event
			///</summary>
			private int currentOp;

			///<summary>
			///    The number of the last render system op to be processed by the event
			///</summary>
			private int lastOp;

			///<summary>
			/// Fields that are treated as temps by queue started/ended events
			///</summary>
			private CompositeTargetOperation operation;

			///<summary>
			///    The render system
			///</summary>
			private RenderSystem renderSystem;

			///<summary>
			///    The scene manager instance
			///</summary>
			private SceneManager sceneManager;

			///<summary>
			///    The view port
			///</summary>
			private Viewport viewport;

			public Viewport Viewport
			{
				set
				{
					this.viewport = value;
				}
			}

			///<summary>
			///    Set current operation and target */
			///</summary>
			public void SetOperation( CompositeTargetOperation op, SceneManager sm, RenderSystem rs )
			{
				this.operation = op;
				this.sceneManager = sm;
				this.renderSystem = rs;
				this.currentOp = 0;
				this.lastOp = op.RenderSystemOperations.Count;
			}

			///<summary>
			/// <see>RenderQueueListener.RenderQueueStarted</see>
			///</summary>
			public void OnRenderQueueStarted( object sender, SceneManager.BeginRenderQueueEventArgs e )
			{
				// Skip when not matching viewport
				// shadows update is nested within main viewport update
				if ( this.sceneManager.CurrentViewport != this.viewport )
				{
					return;
				}

				FlushUpTo( e.RenderQueueId );
				// If noone wants to render this queue, skip it
				// Don't skip the OVERLAY queue because that's handled seperately
				if ( !this.operation.RenderQueues[ (int)e.RenderQueueId ] && e.RenderQueueId != RenderQueueGroupID.Overlay )
				{
					e.SkipInvocation = true;
				}
			}

			///<summary>
			/// <see>RenderQueueListener.RenderQueueEnded</see>
			///</summary>
			public void OnRenderQueueEnded( object sender, SceneManager.EndRenderQueueEventArgs e )
			{
				e.RepeatInvocation = false;
			}

			///<summary>
			///    Flush remaining render system operations
			///</summary>
			public void FlushUpTo( RenderQueueGroupID id )
			{
				// Process all RenderSystemOperations up to and including render queue id.
				// Including, because the operations for RenderQueueGroup x should be executed
				// at the beginning of the RenderQueueGroup render for x.
				while ( this.currentOp != this.lastOp && ( (int)this.operation.RenderSystemOperations[ this.currentOp ].QueueID < (int)id ) )
				{
					this.operation.RenderSystemOperations[ this.currentOp ].Operation.Execute( this.sceneManager, this.renderSystem );
					this.currentOp++;
				}
			}
		}

		#endregion

		#region Fields and Properties

		///<summary>
		///    Identifier for "last" compositor in chain
		///</summary>
		protected static int lastCompositor = int.MaxValue;

		///<summary>
		///    Identifier for best technique
		///</summary>
		protected static int bestCompositor;

		///<summary>
		///    Any compositors enabled?
		///</summary>
		protected bool anyCompositorsEnabled;

		///<summary>
		///    Compiled state (updated with _compile)
		///</summary>
		protected List<CompositeTargetOperation> compiledState;

		///<summary>
		///    State needs recompile
		///</summary>
		protected bool dirty;

		///<summary>
		///    Postfilter instances in this chain
		///</summary>
		protected List<CompositorInstance> instances;

		/// <summary>
		///   The class that will handle the callbacks from the RenderQueue
		/// </summary>
		protected RQListener listener;

		///<summary>
		///    Old viewport settings
		///</summary>
		protected FrameBufferType oldClearEveryFrameBuffers;

		///<summary>
		///    Store old find visible objects
		///</summary>
		protected bool oldFindVisibleObjects;

		///<summary>
		///    Store old camera LOD bias
		///</summary>
		protected float oldLodBias;

		///<summary>
		///    Store old viewport material scheme
		///</summary>
		protected string oldMaterialScheme;

		protected bool oldShowShadows;

		///<summary>
		///    Store old scene visibility mask
		///</summary>
		protected ulong oldVisibilityMask;

		///<summary>
		///    Plainly renders the scene; implicit first compositor in the chain.
		///</summary>
		protected CompositorInstance originalScene;

		protected string originalSceneMaterial;
		protected CompositeTargetOperation outputOperation;

		/// <summary>
		///    Render System operations queued by last compile, these are created by this
		///    instance thus managed and deleted by it. The list is cleared with
		///    ClearCompilationState()
		/// </summary>
		protected List<CompositeRenderSystemOperation> renderSystemOperations;

		///<summary>
		///    Viewport affected by this CompositorChain
		///</summary>
		protected Viewport viewport;

		public Viewport Viewport
		{
			get
			{
				return this.viewport;
			}
			set
			{
				this.viewport = value;
			}
		}

		public CompositorInstance OriginalScene
		{
			get
			{
				return this.originalScene;
			}
		}

		public IList<CompositorInstance> Instances
		{
			get
			{
				return this.instances;
			}
		}

		public bool Dirty
		{
			get
			{
				return this.dirty;
			}
			set
			{
				this.dirty = value;
			}
		}

		internal IList<CompositeRenderSystemOperation> RenderSystemOperations
		{
			get
			{
				return this.renderSystemOperations;
			}
		}

		public static int LastCompositor
		{
			get
			{
				return lastCompositor;
			}
		}

		public static int BestCompositor
		{
			get
			{
				return BestCompositor;
			}
		}

		#endregion Fields and Properties

		#region Constructor

		public CompositorChain( Viewport vp )
		{
			this.viewport = vp;
			this.originalScene = null;
			this.instances = new List<CompositorInstance>();
			this.dirty = true;
			this.anyCompositorsEnabled = false;
			this.compiledState = new List<CompositeTargetOperation>();
			this.outputOperation = null;
			this.oldClearEveryFrameBuffers = this.viewport.ClearBuffers;
			this.renderSystemOperations = new List<CompositeRenderSystemOperation>();

			CreateOriginalScene();
			this.listener = new RQListener();
			Debug.Assert( this.viewport != null );

			this.viewport.Target.BeforeUpdate += BeforeRenderTargetUpdate;
			this.viewport.Target.AfterUpdate += AfterRenderTargetUpdate;
			this.viewport.Target.BeforeViewportUpdate += BeforeViewportUpdate;
			this.viewport.Target.AfterViewportUpdate += AfterViewportUpdate;
		}

		#endregion Constructor

		#region Methods

		/// <summary>
		/// Create "default compositor"
		/// </summary>
		protected void CreateOriginalScene()
		{
			this.originalSceneMaterial = this.viewport.MaterialScheme;
			string compName = "Axiom/Scene/" + this.originalSceneMaterial;
			var scene = (Compositor)CompositorManager.Instance.GetByName( compName );
			if ( scene == null )
			{
				scene = (Compositor)CompositorManager.Instance.Create( compName, ResourceGroupManager.InternalResourceGroupName );
				CompositionTechnique t = scene.CreateTechnique();
				t.SchemeName = string.Empty;
				CompositionTargetPass tp = t.OutputTarget;
				tp.VisibilityMask = 0xFFFFFFFF;

				{
					CompositionPass pass = tp.CreatePass();
					pass.Type = CompositorPassType.Clear;
				}
				{
					CompositionPass pass = tp.CreatePass();
					pass.Type = CompositorPassType.RenderScene;
					//Render everything, including skies
					pass.FirstRenderQueue = RenderQueueGroupID.Background;
					pass.LastRenderQueue = RenderQueueGroupID.SkiesLate;
				}

				scene = (Compositor)CompositorManager.Instance.Load( compName, ResourceGroupManager.InternalResourceGroupName );
			}


			this.originalScene = new CompositorInstance( scene.GetSupportedTechniqueByScheme(), this );
		}

		/// <summary>
		/// Destroy default compositor
		/// </summary>
		protected void DestroyOriginalScene()
		{
			if ( this.originalScene != null )
			{
				this.originalScene.Dispose();
				this.originalScene = null;
			}
		}

		///<summary>
		///    destroy internal resources
		///</summary>
		protected void DestroyResources()
		{
			ClearCompiledState();

			if ( this.viewport != null )
			{
				//Remove listeners
				this.viewport.Target.BeforeUpdate -= BeforeRenderTargetUpdate;
				this.viewport.Target.AfterUpdate -= AfterRenderTargetUpdate;
				this.viewport.Target.BeforeViewportUpdate -= BeforeViewportUpdate;
				this.viewport.Target.AfterViewportUpdate -= AfterViewportUpdate;

				RemoveAllCompositors();
				// Destroy "original scene" compositor instance
				DestroyOriginalScene();

				this.viewport = null;
			}
		}

		///<summary>
		///    Apply a compositor. Initially, the filter is enabled.
		///</summary>
		///<param name="filter">Filter to apply</param>
		public CompositorInstance AddCompositor( Compositor filter )
		{
			return AddCompositor( filter, lastCompositor, bestCompositor, string.Empty );
		}

		///<summary>
		///    Apply a compositor. Initially, the filter is enabled.
		///</summary>
		///<param name="filter">Filter to apply</param>
		///<param name="addPosition">Position in filter chain to insert this filter at; defaults to the end (last applied filter)</param>
		public CompositorInstance AddCompositor( Compositor filter, int addPosition )
		{
			return AddCompositor( filter, addPosition, bestCompositor, string.Empty );
		}

		///<summary>
		///    Apply a compositor. Initially, the filter is enabled.
		///</summary>
		///<param name="filter">Filter to apply</param>
		///<param name="addPosition">Position in filter chain to insert this filter at; defaults to the end (last applied filter)</param>
		///<param name="technique">Technique to use; CompositorChain::BEST (default) chooses to the best one
		///                        available (first technique supported)
		///</param>
		private CompositorInstance AddCompositor( Compositor filter, int addPosition, int technique )
		{
			return AddCompositor( filter, addPosition, technique, string.Empty );
		}

		///<summary>
		///    Apply a compositor. Initially, the filter is enabled.
		///</summary>
		///<param name="filter">Filter to apply</param>
		///<param name="addPosition">Position in filter chain to insert this filter at; defaults to the end (last applied filter)</param>
		///<param name="technique">Technique to use; CompositorChain::BEST (default) chooses to the best one
		///                        available (first technique supported)
		///</param>
		///<param name="scheme"></param>
		private CompositorInstance AddCompositor( Compositor filter, int addPosition, int technique, string scheme )
		{
			filter.Touch();
			CompositionTechnique tech = filter.GetSupportedTechniqueByScheme( scheme );
			if ( tech == null )
			{
				LogManager.Instance.DefaultLog.Write( "CompositorChain: Compositor " + filter.Name + " has no supported techniques." );
			}
			var t = new CompositorInstance( tech, this );

			if ( addPosition == lastCompositor )
			{
				addPosition = this.instances.Count;
			}
			else
			{
				Debug.Assert( addPosition <= this.instances.Count, "Index out of bounds." );
			}
			this.instances.Insert( addPosition, t );

			this.dirty = true;
			this.anyCompositorsEnabled = true;

			return t;
		}


		///<summary>
		/// Removes the last compositor in the chain.
		///</summary>
		public void RemoveCompositor()
		{
			RemoveCompositor( lastCompositor );
		}

		///<summary>
		/// Remove a compositor.
		///</summary>
		///<param name="position">Position in filter chain of filter to remove</param>
		public void RemoveCompositor( int position )
		{
			Debug.Assert( position < this.instances.Count, "Index out of bounds." );
			CompositorInstance instance = this.instances[ position ];
			this.instances.RemoveAt( position );
			instance = null;
			this.dirty = true;
		}

		///<summary>
		///    Remove all compositors.
		///</summary>
		public void RemoveAllCompositors()
		{
			foreach ( CompositorInstance compositorInstance in this.instances )
			{
				compositorInstance.Dispose();
			}
			this.instances.Clear();
			this.dirty = true;
		}

		///<summary>
		///    Remove a compositor by pointer. This is internally used by CompositionTechnique to
		///    "weak" remove any instanced of a deleted technique.
		///</summary>
		public void RemoveInstance( CompositorInstance instance )
		{
			instance.Dispose();
			this.instances.Remove( instance );
			instance = null;
			this.dirty = true;
		}

		///<summary>
		///    Get compositor instance by position.
		///</summary>
		public CompositorInstance GetCompositor( int index )
		{
			return this.instances[ index ];
		}

		/// <summary>
		/// Get Compositor instance by name
		/// </summary>
		/// <returns>Returns instance with matching name, null if none found.</returns>
		public CompositorInstance GetCompositor( string name )
		{
			foreach ( CompositorInstance item in this.instances )
			{
				if ( item.Compositor.Name == name )
				{
					return item;
				}
			}

			return null;
		}

		/// <summary>
		/// Get the previous instance in this chain to the one specified.
		/// </summary>
		/// <param name="curr"></param>
		/// <returns></returns>
		public CompositorInstance GetPreviousInstance( CompositorInstance curr )
		{
			return GetPreviousInstance( curr, true );
		}

		/// <summary>
		/// Get the previous instance in this chain to the one specified.
		/// </summary>
		/// <param name="curr"></param>
		/// <param name="activeOnly"></param>
		/// <returns></returns>
		public CompositorInstance GetPreviousInstance( CompositorInstance curr, bool activeOnly )
		{
			bool found = false;

			for ( int i = this.instances.Count - 1; i >= 0; i-- )
			{
				if ( found )
				{
					if ( this.instances[ i ].IsEnabled || !activeOnly )
					{
						return this.instances[ i ];
					}
				}
				else if ( this.instances[ i ] == curr )
				{
					found = true;
				}
			}

			return null;
		}

		/// <summary>
		/// Get the next instance in this chain to the one specified.
		/// </summary>
		/// <param name="curr"></param>
		/// <returns></returns>
		public CompositorInstance GetNextInstance( CompositorInstance curr )
		{
			return GetNextInstance( curr, true );
		}

		/// <summary>
		/// Get the next instance in this chain to the one specified.
		/// </summary>
		/// <param name="curr"></param>
		/// <param name="activeOnly"></param>
		/// <returns></returns>
		public CompositorInstance GetNextInstance( CompositorInstance curr, bool activeOnly )
		{
			bool found = false;
			for ( int i = 0; i < this.instances.Count; i++ )
			{
				if ( found )
				{
					if ( this.instances[ i ].IsEnabled || !activeOnly )
					{
						return this.instances[ i ];
					}
				}
				else if ( this.instances[ i ] == curr )
				{
					found = true;
				}
			}

			return null;
		}

		/// <summary>
		///    Enable or disable a compositor, by position. Disabling a compositor stops it from rendering
		///    but does not free any resources. This can be more efficient than using removeCompositor and
		///    addCompositor in cases the filter is switched on and off a lot.
		/// </summary>
		/// <param name="position">Position in filter chain of filter</param>
		/// <param name="state"></param>
		public void SetCompositorEnabled( int position, bool state )
		{
			CompositorInstance instance = GetCompositor( position );
			if ( !state && instance.IsEnabled )
			{
				// If we're disabling a 'middle' compositor in a chain, we have to be
				// careful about textures which might have been shared by non-adjacent
				// instances which have now become adjacent.
				CompositorInstance nextInstance = GetNextInstance( instance, true );
				if ( nextInstance != null )
				{
					foreach ( CompositionTargetPass tp in nextInstance.Technique.TargetPasses )
					{
						if ( tp.InputMode == CompositorInputMode.Previous )
						{
							if ( nextInstance.Technique.GetTextureDefinition( tp.OutputName ).Pooled )
							{
								// recreate
								nextInstance.FreeResources( false, true );
								nextInstance.CreateResources( false );
							}
						}
					}
				}
			}
			instance.IsEnabled = state;
		}

		///<summary>
		///    @see RenderTargetListener.PreRenderTargetUpdate
		///</summary>
		public void BeforeRenderTargetUpdate( RenderTargetEventArgs evt )
		{
			// Compile if state is dirty
			if ( this.dirty )
			{
				Compile();
			}

			// Do nothing if no compositors enabled
			if ( !this.anyCompositorsEnabled )
			{
				return;
			}

			// Update dependent render targets; this is done in the BeforeRenderTargetUpdate
			// and not the BeforeViewportUpdate for a reason: at this time, the
			// target Rendertarget will not yet have been set as current.
			// ( RenderSystem.Viewport = ... ) if it would have been, the rendering
			// order would be screwed up and problems would arise with copying rendertextures.
			Camera cam = this.viewport.Camera;
			if ( cam == null )
			{
				return;
			}
			cam.SceneManager.ActiveCompositorChain = this;

			// Iterate over compiled state
			foreach ( CompositeTargetOperation op in this.compiledState )
			{
				// Skip if this is a target that should only be initialised initially
				if ( op.OnlyInitial && op.HasBeenRendered )
				{
					continue;
				}
				op.HasBeenRendered = true;
				// Setup and render
				PreTargetOperation( op, op.Target.GetViewport( 0 ), cam );
				op.Target.Update();
				PostTargetOperation( op, op.Target.GetViewport( 0 ), cam );
			}
		}

		///<summary>
		///    @see RenderTargetListener.PreRenderTargetUpdate
		///</summary>
		public void AfterRenderTargetUpdate( RenderTargetEventArgs evt )
		{
			Camera cam = this.viewport.Camera;
			if ( cam != null )
			{
				cam.SceneManager.ActiveCompositorChain = null;
			}
		}

		///<summary>
		///    @see RenderTargetListener.PreViewportUpdate
		///</summary>
		public virtual void BeforeViewportUpdate( RenderTargetViewportEventArgs evt )
		{
			// Only set up if there is at least one compositor enabled, and it's this viewport
			if ( evt.Viewport != this.viewport || !this.anyCompositorsEnabled )
			{
				return;
			}

			// set original scene details from viewport
			CompositionPass pass = this.originalScene.Technique.OutputTarget.Passes[ 0 ];
			CompositionTargetPass passParent = pass.Parent;
			if ( pass.ClearBuffers != this.viewport.ClearBuffers || pass.ClearColor != this.viewport.BackgroundColor || passParent.VisibilityMask != this.viewport.VisibilityMask || passParent.MaterialScheme != this.viewport.MaterialScheme || passParent.ShadowsEnabled != this.viewport.ShowShadows )
			{
				pass.ClearBuffers = this.viewport.ClearBuffers;
				pass.ClearColor = this.viewport.BackgroundColor;
				pass.ClearDepth = this.viewport.ClearDepth;
				passParent.VisibilityMask = this.viewport.VisibilityMask;
				passParent.MaterialScheme = this.viewport.MaterialScheme;
				passParent.ShadowsEnabled = this.viewport.ShowShadows;
				Compile();
			}

			Camera camera = this.viewport.Camera;
			if ( camera != null )
			{
				// Prepare for output operation
				PreTargetOperation( this.outputOperation, this.viewport, this.viewport.Camera );
			}
		}

		///<summary>
		///    Prepare a viewport, the camera and the scene for a rendering operation
		///</summary>
		protected void PreTargetOperation( CompositeTargetOperation op, Viewport vp, Camera cam )
		{
			if ( cam != null )
			{
				SceneManager sm = cam.SceneManager;
				// Set up render target listener
				this.listener.SetOperation( op, sm, sm.TargetRenderSystem );
				this.listener.Viewport = vp;
				// Register it
				sm.QueueStarted += this.listener.OnRenderQueueStarted;
				sm.QueueEnded += this.listener.OnRenderQueueEnded;
				// Set visiblity mask
				this.oldVisibilityMask = sm.VisibilityMask;
				sm.VisibilityMask = op.VisibilityMask;
				// Set whether we find visibles
				this.oldFindVisibleObjects = sm.FindVisibleObjectsBool;
				sm.FindVisibleObjectsBool = op.FindVisibleObjects;
				// Set LOD bias level
				this.oldLodBias = cam.LodBias;
				cam.LodBias = cam.LodBias * op.LodBias;
			}


			// Set material scheme
			this.oldMaterialScheme = vp.MaterialScheme;
			vp.MaterialScheme = op.MaterialScheme;
			// Set Shadows Enabled
			this.oldShowShadows = vp.ShowShadows;
			vp.ShowShadows = op.ShadowsEnabled;

			//vp.ClearEveryFrame = true;
			//vp.ShowOverlays = false;
			//vp.BackgroundColor = op.ClearColor;
		}

		///<summary>
		///    Restore a viewport, the camera and the scene after a rendering operation
		///</summary>
		protected void PostTargetOperation( CompositeTargetOperation op, Viewport vp, Camera cam )
		{
			if ( cam != null )
			{
				SceneManager sm = cam.SceneManager;
				// Unregister our listener
				sm.QueueStarted -= this.listener.OnRenderQueueStarted;
				sm.QueueEnded -= this.listener.OnRenderQueueEnded;
				// Restore default scene and camera settings
				sm.VisibilityMask = this.oldVisibilityMask;
				sm.FindVisibleObjectsBool = this.oldFindVisibleObjects;
				cam.LodBias = this.oldLodBias;
			}

			vp.MaterialScheme = this.oldMaterialScheme;
			vp.ShowShadows = this.oldShowShadows;
		}

		///<summary>
		///    @see RenderTargetListener.PostViewportUpdate
		///</summary>
		public virtual void AfterViewportUpdate( RenderTargetViewportEventArgs evt )
		{
			// Only tidy up if there is at least one compositor enabled, and it's this viewport
			if ( evt.Viewport != this.viewport || !this.anyCompositorsEnabled )
			{
				return;
			}

			if ( this.viewport.Camera != null )
			{
				PostTargetOperation( this.outputOperation, this.viewport, this.viewport.Camera );
			}
		}


		///<summary>
		///    @see RenderTargetListener.ViewportRemoved
		///</summary>
		public virtual void OnViewportRemoved( RenderTargetViewportEventArgs evt )
		{
			// check this is the viewport we're attached to (multi-viewport targets)
			if ( evt.Viewport == this.viewport )
			{
				// this chain is now orphaned
				// can't delete it since held from outside, but release all resources being used
				DestroyResources();
			}
		}

		///<summary>
		///    Compile this Composition chain into a series of RenderTarget operations.
		///</summary>
		protected void Compile()
		{
			//remove original scen if it has the wrong material scheme
			if ( this.originalSceneMaterial != this.viewport.MaterialScheme )
			{
				DestroyOriginalScene();
				CreateOriginalScene();
			}

			ClearCompiledState();

			bool compositorsEnabled = false;

			// force default scheme so materials for compositor quads will determined correctly
			MaterialManager matMgr = MaterialManager.Instance;
			string prevMaterialScheme = matMgr.ActiveScheme;
			matMgr.ActiveScheme = MaterialManager.DefaultSchemeName;

			// Set previous CompositorInstance for each compositor in the list
			CompositorInstance lastComposition = this.originalScene;
			this.originalScene.PreviousInstance = null;
			CompositionPass pass = this.originalScene.Technique.OutputTarget.Passes[ 0 ];
			pass.ClearBuffers = this.viewport.ClearBuffers;
			pass.ClearColor = this.viewport.BackgroundColor;
			pass.ClearDepth = this.viewport.ClearDepth;

			foreach ( CompositorInstance instance in this.instances )
			{
				if ( instance.IsEnabled )
				{
					compositorsEnabled = true;
					instance.PreviousInstance = lastComposition;
					lastComposition = instance;
				}
			}

			// Compile misc targets
			lastComposition.CompileTargetOperations( this.compiledState );

			// Final target viewport (0)
			this.outputOperation.RenderSystemOperations.Clear();
			lastComposition.CompileOutputOperation( this.outputOperation );

			// Deal with viewport settings
			if ( compositorsEnabled != this.anyCompositorsEnabled )
			{
				this.anyCompositorsEnabled = compositorsEnabled;
				if ( this.anyCompositorsEnabled )
				{
					// Save old viewport clearing options
					this.oldClearEveryFrameBuffers = this.viewport.ClearBuffers;
					// Don't clear anything every frame since we have our own clear ops
					this.viewport.SetClearEveryFrame( false );
				}
				else
				{
					// Reset clearing options
					this.viewport.SetClearEveryFrame( this.oldClearEveryFrameBuffers > 0, this.oldClearEveryFrameBuffers );
				}
			}
			this.dirty = false;

			matMgr.ActiveScheme = prevMaterialScheme;
		}

		protected void ClearCompiledState()
		{
			this.renderSystemOperations.Clear();
			this.compiledState.Clear();
			this.outputOperation = new CompositeTargetOperation( null );
		}

		protected override void dispose( bool disposeManagedResources )
		{
			if ( !IsDisposed )
			{
				if ( disposeManagedResources )
				{
					return;
					DestroyResources();
				}
			}
			base.dispose( disposeManagedResources );
		}

		#endregion Methods
	}
}
