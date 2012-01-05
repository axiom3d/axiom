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
	public class CompositorChain
	{
		public class RQListener
		{
			///<summary>
			/// Fields that are treated as temps by queue started/ended events
			///</summary>
			private CompositeTargetOperation operation;

			///<summary>
			///    The scene manager instance
			///</summary>
			private SceneManager sceneManager;

			///<summary>
			///    The render system
			///</summary>
			private RenderSystem renderSystem;

			///<summary>
			///    The view port
			///</summary>
			private Viewport viewport;

			public Viewport Viewport { set { viewport = value; } }

			///<summary>
			///    The number of the first render system op to be processed by the event
			///</summary>
			private int currentOp;

			///<summary>
			///    The number of the last render system op to be processed by the event
			///</summary>
			private int lastOp;

			///<summary>
			///    Set current operation and target */
			///</summary>
			public void SetOperation( CompositeTargetOperation op, SceneManager sm, RenderSystem rs )
			{
				operation = op;
				sceneManager = sm;
				renderSystem = rs;
				currentOp = 0;
				lastOp = op.RenderSystemOperations.Count;
			}

			///<summary>
			/// <see>RenderQueueListener.RenderQueueStarted</see>
			///</summary>
			public void OnRenderQueueStarted( object sender, SceneManager.BeginRenderQueueEventArgs e )
			{
				// Skip when not matching viewport
				// shadows update is nested within main viewport update
				if( sceneManager.CurrentViewport != viewport )
				{
					return;
				}

				FlushUpTo( e.RenderQueueId );
				// If noone wants to render this queue, skip it
				// Don't skip the OVERLAY queue because that's handled seperately
				if( !operation.RenderQueues[ (int)e.RenderQueueId ] && e.RenderQueueId != RenderQueueGroupID.Overlay )
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
				while( currentOp != lastOp &&
				       ( (int)operation.RenderSystemOperations[ currentOp ].QueueID < (int)id ) )
				{
					operation.RenderSystemOperations[ currentOp ].Operation.Execute( sceneManager, renderSystem );
					currentOp++;
				}
			}
		}

		#region Fields and Properties

		///<summary>
		///    Viewport affected by this CompositorChain
		///</summary>
		protected Viewport viewport;

		public Viewport Viewport { get { return viewport; } set { viewport = value; } }

		///<summary>
		///    Plainly renders the scene; implicit first compositor in the chain.
		///</summary>
		protected CompositorInstance originalScene;

		public CompositorInstance OriginalScene { get { return originalScene; } }

		///<summary>
		///    Postfilter instances in this chain
		///</summary>
		protected List<CompositorInstance> instances;

		public IList<CompositorInstance> Instances { get { return instances; } }

		///<summary>
		///    State needs recompile
		///</summary>
		protected bool dirty;

		public bool Dirty { get { return dirty; } set { dirty = value; } }

		///<summary>
		///    Any compositors enabled?
		///</summary>
		protected bool anyCompositorsEnabled;

		///<summary>
		///    Compiled state (updated with _compile)
		///</summary>
		protected List<CompositeTargetOperation> compiledState;

		protected CompositeTargetOperation outputOperation;

		/// <summary>
		///    Render System operations queued by last compile, these are created by this
		///    instance thus managed and deleted by it. The list is cleared with
		///    ClearCompilationState()
		/// </summary>
		protected List<CompositeRenderSystemOperation> renderSystemOperations;

		internal IList<CompositeRenderSystemOperation> RenderSystemOperations { get { return renderSystemOperations; } }

		///<summary>
		///    Old viewport settings
		///</summary>
		protected FrameBufferType oldClearEveryFrameBuffers;

		///<summary>
		///    Store old scene visibility mask
		///</summary>
		protected ulong oldVisibilityMask;

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

		/// <summary>
		///   The class that will handle the callbacks from the RenderQueue
		/// </summary>
		protected RQListener listener;

		///<summary>
		///    Identifier for "last" compositor in chain
		///</summary>
		protected static int lastCompositor = int.MaxValue;

		public static int LastCompositor { get { return lastCompositor; } }

		///<summary>
		///    Identifier for best technique
		///</summary>
		protected static int bestCompositor = 0;

		public static int BestCompositor { get { return BestCompositor; } }

		#endregion Fields and Properties

		#region Constructor

		public CompositorChain( Viewport vp )
		{
			this.viewport = vp;
			originalScene = null;
			instances = new List<CompositorInstance>();
			dirty = true;
			anyCompositorsEnabled = false;
			compiledState = new List<CompositeTargetOperation>();
			outputOperation = null;
			oldClearEveryFrameBuffers = viewport.ClearBuffers;
			renderSystemOperations = new List<CompositeRenderSystemOperation>();
			listener = new RQListener();
			Debug.Assert( viewport != null );
		}

		#endregion Constructor

		#region Methods

		///<summary>
		///    destroy internal resources
		///</summary>
		protected void DestroyResources()
		{
			ClearCompiledState();

			if( viewport != null )
			{
				RemoveAllCompositors();

				if( originalScene != null )
				{
					viewport.Target.BeforeUpdate -= BeforeRenderTargetUpdate;
					viewport.Target.AfterUpdate -= AfterRenderTargetUpdate;
					viewport.Target.BeforeViewportUpdate -= BeforeViewportUpdate;
					viewport.Target.AfterViewportUpdate -= AfterViewportUpdate;
					// Destroy "original scene" compositor instance
					originalScene = null;
				}
				viewport = null;
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
		private CompositorInstance AddCompositor( Compositor filter, int addPosition, int technique, string scheme )
		{
			// Init on demand
			if( originalScene == null )
			{
				viewport.Target.BeforeUpdate += BeforeRenderTargetUpdate;
				viewport.Target.AfterUpdate += AfterRenderTargetUpdate;
				viewport.Target.BeforeViewportUpdate += BeforeViewportUpdate;
				viewport.Target.AfterViewportUpdate += AfterViewportUpdate;
				// Create base "original scene" compositor
				Compositor baseCompositor = (Compositor)CompositorManager.Instance.Load( "Axiom/Scene", ResourceGroupManager.InternalResourceGroupName );
				originalScene = new CompositorInstance( baseCompositor.GetSupportedTechniqueByScheme(), this );
			}

			filter.Touch();
			CompositionTechnique tech = filter.GetSupportedTechniqueByScheme( scheme );
			if( tech == null )
			{
				// Warn user
				LogManager.Instance.Write( "CompositorChain: Compositor " + filter.Name + " has no supported techniques." );
				return null;
			}
			CompositorInstance t = new CompositorInstance( tech, this );

			if( addPosition == lastCompositor )
			{
				addPosition = instances.Count;
			}
			else
			{
				Debug.Assert( addPosition <= instances.Count, "Index out of bounds" );
			}
			instances.Insert( addPosition, t );

			dirty = true;
			anyCompositorsEnabled = true;
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
			CompositorInstance instance = instances[ position ];
			instances.RemoveAt( position );
			instance = null;
			dirty = true;
		}

		///<summary>
		///    Remove all compositors.
		///</summary>
		public void RemoveAllCompositors()
		{
			foreach( CompositorInstance compositorInstance in instances )
			{
				compositorInstance.Dispose();
			}
			instances.Clear();
			dirty = true;
		}

		///<summary>
		///    Remove a compositor by pointer. This is internally used by CompositionTechnique to
		///    "weak" remove any instanced of a deleted technique.
		///</summary>
		public void RemoveInstance( CompositorInstance instance )
		{
			instance.Dispose();
			instances.Remove( instance );
			instance = null;
			dirty = true;
		}

		///<summary>
		///    Get compositor instance by position.
		///</summary>
		public CompositorInstance GetCompositor( int index )
		{
			return instances[ index ];
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
			int begin = instances.Count - 1;
			int end = 0;
			for( ; begin >= end; begin-- )
			{
				if( found )
				{
					if( instances[ begin ].IsEnabled || !activeOnly )
					{
						return instances[ begin ];
					}
				}
				else if( curr == instances[ begin ] )
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
			for( int i = 0; i < instances.Count; i++ )
			{
				if( found )
				{
					if( instances[ i ].IsEnabled || !activeOnly )
					{
						return instances[ i ];
					}
				}
				else if( instances[ i ] == curr )
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
			if( !state && instance.IsEnabled )
			{
				// If we're disabling a 'middle' compositor in a chain, we have to be
				// careful about textures which might have been shared by non-adjacent
				// instances which have now become adjacent.
				CompositorInstance nextInstance = GetNextInstance( instance, true );
				if( nextInstance != null )
				{
					foreach( CompositionTargetPass tp in nextInstance.Technique.TargetPasses )
					{
						if( tp.InputMode == CompositorInputMode.Previous )
						{
							if( nextInstance.Technique.GetTextureDefinition( tp.OutputName ).Pooled )
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
			if( dirty )
			{
				Compile();
			}

			// Do nothing if no compositors enabled
			if( !anyCompositorsEnabled )
			{
				return;
			}

			// Update dependent render targets; this is done in the BeforeRenderTargetUpdate
			// and not the BeforeViewportUpdate for a reason: at this time, the
			// target Rendertarget will not yet have been set as current.
			// ( RenderSystem.Viewport = ... ) if it would have been, the rendering
			// order would be screwed up and problems would arise with copying rendertextures.
			Camera cam = viewport.Camera;
			if( cam == null )
			{
				return;
			}
			cam.SceneManager.ActiveCompositorChain = this;

			// Iterate over compiled state
			foreach( CompositeTargetOperation op in compiledState )
			{
				// Skip if this is a target that should only be initialised initially
				if( op.OnlyInitial && op.HasBeenRendered )
				{
					continue;
				}
				op.HasBeenRendered = true;
				// Setup and render
				PreTargetOperation( op, op.Target.GetViewport( 0 ), cam );
				op.Target.Update();
				//op.Target.WriteContentsToFile( op.Target.Name + ".png" );
				PostTargetOperation( op, op.Target.GetViewport( 0 ), cam );
			}
		}

		///<summary>
		///    @see RenderTargetListener.PreRenderTargetUpdate
		///</summary>
		public void AfterRenderTargetUpdate( RenderTargetEventArgs evt )
		{
			Camera cam = viewport.Camera;
			if( cam != null )
			{
				cam.SceneManager.ActiveCompositorChain = null;
			}
		}

		///<summary>
		///    @see RenderTargetListener.PreViewportUpdate
		///</summary>
		virtual public void BeforeViewportUpdate( RenderTargetViewportEventArgs evt )
		{
			// Only set up if there is at least one compositor enabled, and it's this viewport
			if( evt.Viewport != viewport || !anyCompositorsEnabled )
			{
				return;
			}

			// set original scene details from viewport
			CompositionPass pass = originalScene.Technique.OutputTarget.Passes[ 0 ];
			CompositionTargetPass passParent = pass.Parent;
			if( pass.ClearBuffers != viewport.ClearBuffers ||
			    pass.ClearColor != viewport.BackgroundColor ||
			    passParent.VisibilityMask != viewport.VisibilityMask ||
			    passParent.MaterialScheme != viewport.MaterialScheme ||
			    passParent.ShadowsEnabled != viewport.ShowShadows )
			{
				pass.ClearBuffers = viewport.ClearBuffers;
				pass.ClearColor = viewport.BackgroundColor;
				passParent.VisibilityMask = viewport.VisibilityMask;
				passParent.MaterialScheme = viewport.MaterialScheme;
				passParent.ShadowsEnabled = viewport.ShowShadows;
				Compile();
			}

			Camera camera = viewport.Camera;
			if( camera != null )
			{
				// Prepare for output operation
				PreTargetOperation( outputOperation, viewport, viewport.Camera );
			}
		}

		///<summary>
		///    Prepare a viewport, the camera and the scene for a rendering operation
		///</summary>
		protected void PreTargetOperation( CompositeTargetOperation op, Viewport vp, Camera cam )
		{
			SceneManager sm = cam.SceneManager;
			// Set up render target listener
			listener.SetOperation( op, sm, sm.TargetRenderSystem );
			listener.Viewport = vp;
			// Register it
			sm.QueueStarted += listener.OnRenderQueueStarted;
			sm.QueueEnded += listener.OnRenderQueueEnded;
			// Set visiblity mask
			oldVisibilityMask = sm.VisibilityMask;
			sm.VisibilityMask = op.VisibilityMask;
			// Set whether we find visibles
			oldFindVisibleObjects = sm.FindVisibleObjectsBool;
			sm.FindVisibleObjectsBool = op.FindVisibleObjects;
			// Set LOD bias level
			oldLodBias = cam.LodBias;
			cam.LodBias = cam.LodBias * op.LodBias;
			// Set material scheme
			oldMaterialScheme = vp.MaterialScheme;
			vp.MaterialScheme = op.MaterialScheme;
			// Set Shadows Enabled
			oldShowShadows = vp.ShowShadows;
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
			SceneManager sm = cam.SceneManager;
			// Unregister our listener
			sm.QueueStarted -= listener.OnRenderQueueStarted;
			sm.QueueEnded -= listener.OnRenderQueueEnded;
			// Restore default scene and camera settings
			sm.VisibilityMask = oldVisibilityMask;
			sm.FindVisibleObjectsBool = oldFindVisibleObjects;
			cam.LodBias = oldLodBias;
			vp.MaterialScheme = oldMaterialScheme;
			vp.ShowShadows = oldShowShadows;
		}

		///<summary>
		///    @see RenderTargetListener.PostViewportUpdate
		///</summary>
		virtual public void AfterViewportUpdate( RenderTargetViewportEventArgs evt )
		{
			// Only tidy up if there is at least one compositor enabled, and it's this viewport
			if( evt.Viewport != viewport || !anyCompositorsEnabled )
			{
				return;
			}

			if( viewport.Camera != null )
			{
				PostTargetOperation( outputOperation, viewport, viewport.Camera );
			}
		}

		///<summary>
		///    @see RenderTargetListener.ViewportRemoved
		///</summary>
		virtual public void OnViewportRemoved( RenderTargetViewportEventArgs evt )
		{
			// check this is the viewport we're attached to (multi-viewport targets)
			if( evt.Viewport == viewport )
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
			LogManager.Instance.Write( "[CORE] Compiling CompositorChain." );
			ClearCompiledState();

			bool compositorsEnabled = false;

			// force default scheme so materials for compositor quads will determined correctly
			MaterialManager matMgr = MaterialManager.Instance;
			string prevMaterialScheme = matMgr.ActiveScheme;
			matMgr.ActiveScheme = MaterialManager.DefaultSchemeName;

			// Set previous CompositorInstance for each compositor in the list
			CompositorInstance lastComposition = originalScene;
			originalScene.PreviousInstance = null;
			CompositionPass pass = originalScene.Technique.OutputTarget.Passes[ 0 ];
			pass.ClearBuffers = viewport.ClearBuffers;
			pass.ClearColor = viewport.BackgroundColor;
			foreach( CompositorInstance instance in instances )
			{
				if( instance.IsEnabled )
				{
					compositorsEnabled = true;
					instance.PreviousInstance = lastComposition;
					lastComposition = instance;
				}
			}

			// Compile misc targets
			lastComposition.CompileTargetOperations( compiledState );

			// Final target viewport (0)
			outputOperation.RenderSystemOperations.Clear();
			lastComposition.CompileOutputOperation( outputOperation );

			// Deal with viewport settings
			if( compositorsEnabled != anyCompositorsEnabled )
			{
				anyCompositorsEnabled = compositorsEnabled;
				if( anyCompositorsEnabled )
				{
					// Save old viewport clearing options
					oldClearEveryFrameBuffers = viewport.ClearBuffers;
					// Don't clear anything every frame since we have our own clear ops
					viewport.ClearEveryFrame = false;
				}
				else
				{
					// Reset clearing options
					viewport.ClearEveryFrame = oldClearEveryFrameBuffers > 0;
					viewport.ClearBuffers = oldClearEveryFrameBuffers;
				}
			}
			dirty = false;

			matMgr.ActiveScheme = prevMaterialScheme;
		}

		protected void ClearCompiledState()
		{
			renderSystemOperations.Clear();
			compiledState.Clear();
			outputOperation = new CompositeTargetOperation( null );
		}

		#endregion Methods
	}
}
