#region LGPL License

/*
Axiom Graphics Engine Library
Copyright (C) 2003-2006  Axiom Project Team
 
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

#endregion

#region SVN Version Information

// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <id value="$Id$"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

using Axiom.Core;
using Axiom.Configuration;
using Axiom.Math;

#endregion Namespace Declarations

namespace Axiom.Graphics
{
    /// <summary>
    /// Chain of compositor effects applying to one viewport.
    /// </summary>
    public class CompositorChain : IDisposable
    {
        /// <summary>
        /// 
        /// </summary>
        public class RQListener
        {
            /// <summary>
            /// 
            /// </summary>
            private CompositorInstance.TargetOperation _operation;

            /// <summary>
            /// 
            /// </summary>
            private SceneManager _sceneManager;

            /// <summary>
            /// 
            /// </summary>
            private RenderSystem _renderSystem;

            /// <summary>
            /// 
            /// </summary>
            private Viewport _viewport;

            /// <summary>
            /// 
            /// </summary>
            private IEnumerator<Tuple<int,CompositorInstance.RenderSystemOperation>> _currentOp;

            /// <summary>
            /// 
            /// </summary>
            public Viewport Viewport
            {
                get
                {
                    return _viewport;
                }
                set
                {
                    _viewport = value;
                }
            }

            /// <summary>
            /// 
            /// </summary>
            public RQListener()
            {
            }

            /// <summary>
            /// Set current operation and target
            /// </summary>
            /// <param name="op"></param>
            /// <param name="sm"></param>
            /// <param name="rs"></param>
            public void SetOperation( CompositorInstance.TargetOperation op, SceneManager sm, RenderSystem rs )
            {
                _operation = op;
                _sceneManager = sm;
                _renderSystem = rs;
                _currentOp = op.RenderSystemOperations.GetEnumerator();
                _currentOp.MoveNext();
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="sender"></param>
            /// <param name="e"></param>
            public void RenderQueueEnded( object sender, SceneManager.EndRenderQueueEventArgs e )
            {
                //nothing todo
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="sender"></param>
            /// <param name="e"></param>
            public void RenderQueueStarted( object sender, SceneManager.BeginRenderQueueEventArgs e )
            {
                // Skip when not matching viewport
                // shadows update is nested within main viewport update
                if ( _sceneManager.CurrentViewport != _viewport )
                {
                    return;
                }

                FlushUpTo( e.RenderQueueId );

                // If noone wants to render this queue, skip it
                // Don't skip the OVERLAY queue because that's handled seperately
                if ( !_operation.RenderQueues[ (int)e.RenderQueueId ] && e.RenderQueueId != RenderQueueGroupID.Overlay )
                {
                    e.SkipInvocation = true;
                }
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="id"></param>
            public void FlushUpTo( RenderQueueGroupID id )
            {
                // Process all RenderSystemOperations up to and including render queue id.
                // Including, because the operations for RenderQueueGroup x should be executed
                // at the beginning of the RenderQueueGroup render for x.
                while ( _currentOp.Current.Second != null && _currentOp.Current.First <= (int)id)
                {
                    _currentOp.Current.Second.Execute(_sceneManager, _renderSystem);
                    _currentOp.MoveNext();
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private RQListener _ourListener = new RQListener();

        /// <summary>
        /// Store old shadows enabled flag
        /// </summary>
        private bool _oldShadowsEnabled;

        /// <summary>
        /// Store old viewport material scheme
        /// </summary>
        private string _oldMaterialScheme;

        /// <summary>
        /// Store old camera LOD bias
        /// </summary>
        private float _oldLodBias;

        /// <summary>
        /// Store old find visible objects
        /// </summary>
        private bool _oldFindVisibleObjects;

        /// <summary>
        /// Store old scene visibility mask
        /// </summary>
        private ulong _oldVisibilityMask;

        /// <summary>
        /// Old viewport settings
        /// </summary>
        private FrameBufferType _oldClearEveryFrameBuffers;

        /// <summary>
        /// Viewport affected by this CompositorChain
        /// </summary>
        protected Viewport _viewport;

        /// <summary>
        /// Plainly renders the scene; implicit first compositor in the chain.
        /// </summary>
        protected CompositorInstance _originalScene;

        /// <summary>
        /// Postfilter instances in this chain
        /// </summary>
        protected List<CompositorInstance> _instances;

        /// <summary>
        /// State needs recompile
        /// </summary>
        protected bool _dirty;

        /// <summary>
        /// Any compositors enabled?
        /// </summary>
        protected bool _anyCompositorEnabled;

        /// <summary>
        /// Compiled state (updated with _compile)
        /// </summary>
        protected List<CompositorInstance.TargetOperation> _compiledState;

        /// <summary>
        /// 
        /// </summary>
        protected CompositorInstance.TargetOperation _outputOperation;

        /// <summary>
        /// Render System operations queued by last compile, these are created by this
        /// instance thus managed and deleted by it. The list is cleared with
        /// clearCompilationState()
        /// </summary>
        protected List<CompositorInstance.RenderSystemOperation> _rendersystemOperations;

        /// <summary>
        /// Identifier for "last" compositor in chain
        /// </summary>
        public static int LastCompositor
        {
            get
            {
                return -1;
            }
        }

        /// <summary>
        /// Identifier for best technique
        /// </summary>
        public static int BestCompositor
        {
            get
            {
                return 0;
            }
        }

        /// <summary>
        /// Get's the number of compositors.
        /// </summary>
        public int CompositorCount
        {
            get
            {
                return _instances.Count;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public List<CompositorInstance> Instances
        {
            get
            {
                return _instances;
            }
        }

        /// <summary>
        /// Get the original scene compositor instance for this chain (internal use).
        /// </summary>
        public CompositorInstance OriginalSceneCompositor
        {
            get
            {
                return _originalScene;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public Viewport Viewport
        {
            get
            {
                return _viewport;
            }
            internal set
            {
                _viewport = value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="vp"></param>
        public CompositorChain( Viewport vp )
        {
            Debug.Assert( vp != null, "Viewport NULL, CompositorChain ctor" );
            _oldClearEveryFrameBuffers = vp.ClearBuffers;
            _viewport = vp;
            _dirty = true;
            _instances = new List<CompositorInstance>();
            _oldMaterialScheme = string.Empty;
            _rendersystemOperations = new List<CompositorInstance.RenderSystemOperation>();
            _compiledState = new List<CompositorInstance.TargetOperation>();

        }

        /// <summary>
        /// 
        /// </summary>
        public void Dispose()
        {
            DestroyResources();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        public void AfterViewportUpdate( RenderTargetViewportEventArgs e )
        {
            // Only tidy up if there is at least one compositor enabled, and it's this viewport
            if ( e.Viewport != _viewport || !_anyCompositorEnabled )
            {
                return;
            }

            Camera cam = _viewport.Camera;
            AfterTargetOperation( _outputOperation, _viewport, cam );
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        public void AfterUpdate( RenderTargetEventArgs e )
        {
            Camera cam = _viewport.Camera;
            if ( cam != null )
            {
                cam.SceneManager.ActiveCompositorChain = null;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        public void BeforeViewportUpdate( RenderTargetViewportEventArgs e )
        {
            // Only set up if there is at least one compositor enabled, and it's this viewport
            if ( e.Viewport != _viewport || !_anyCompositorEnabled )
            {
                return;
            }

            // set original scene details from viewport
            CompositionPass pass = _originalScene.Technique.OutputTarget.GetPass( 0 );
            CompositionTargetPass passParent = pass.Parent;
            if ( pass.ClearBuffers != _viewport.ClearBuffers ||
                 pass.ClearColor != _viewport.BackgroundColor ||
                 pass.ClearDepth != _viewport.ClearDepth ||
                 passParent.VisibilityMask != _viewport.VisibilityMask ||
                 passParent.MaterialScheme != _viewport.MaterialScheme ||
                 passParent.ShadowsEnabled != _viewport.ShowShadows )
            {
                // recompile if viewport settings are different
                pass.ClearBuffers = _viewport.ClearBuffers;
                pass.ClearColor = _viewport.BackgroundColor;
                pass.ClearDepth = _viewport.ClearDepth;
                passParent.VisibilityMask = _viewport.VisibilityMask;
                passParent.MaterialScheme = _viewport.MaterialScheme;
                passParent.ShadowsEnabled = _viewport.ShowShadows;
                Compile();
            }

            Camera cam = _viewport.Camera;
            if ( cam != null )
            {
                /// Prepare for output operation
                BeforeTargetOperation( _outputOperation, _viewport, cam );
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        public void BeforeUpdate( RenderTargetEventArgs e )
        {
            // Compile if state is dirty
            if ( _dirty )
            {
                Compile();
            }

            // Do nothing if no compositors enabled
            if ( !_anyCompositorEnabled )
            {
                return;
            }

            // Update dependent render targets; this is done in the BeforeUpdate
            // and not the BeforeViewportUpdate for a reason: at this time, the
            // target Rendertarget will not yet have been set as current.
            // ( RenderSystem.SetViewport(...) ) if it would have been, the rendering
            // order would be screwed up and problems would arise with copying rendertextures.
            Camera cam = _viewport.Camera;
            if ( cam != null )
            {
                cam.SceneManager.ActiveCompositorChain = this;
            }

            // Iterate over compiled state
            foreach ( CompositorInstance.TargetOperation i in _compiledState )
            {
                // Skip if this is a target that should only be initialised initially
                if ( i.OnlyInitial && i.HasBeenRendered )
                {
                    continue;
                }
                i.HasBeenRendered = true;
                // Setup and render
                BeforeTargetOperation( i, i.Target.GetViewport( 0 ), cam );
                i.Target.Update();
                i.Target.WriteContentsToFile( i.Target.Name + ".jpg" );
                AfterTargetOperation( i, i.Target.GetViewport( 0 ), cam );
            }
        }

        /// <summary>
        /// Apply a compositor. Initially, the filter is enabled.
        /// </summary>
        /// <param name="filter">Filter to apply</param>
        /// <returns>new Compositor Instance</returns>
        public CompositorInstance AddCompositor( Compositor filter )
        {
            return AddCompositor( filter, LastCompositor, string.Empty );
        }

        /// <summary>
        /// Apply a compositor. Initially, the filter is enabled.
        /// </summary>
        /// <param name="filter">Filter to apply</param>
        /// <param name="addPosition">Position in filter chain to insert this filter at; defaults to the end (last applied filter)</param>
        /// <returns>new Compositor Instance</returns>
        public CompositorInstance AddCompositor( Compositor filter, int addPosition )
        {
            return AddCompositor( filter, addPosition, string.Empty );
        }

        /// <summary>
        /// Apply a compositor. Initially, the filter is enabled.
        /// </summary>
        /// <param name="filter">Filter to apply</param>
        /// <param name="addPosition">Position in filter chain to insert this filter at; defaults to the end (last applied filter)</param>
        /// <param name="scheme">Scheme to use (blank means default)</param>
        /// <returns>new Compositor Instance</returns>
        public CompositorInstance AddCompositor( Compositor filter, int addPosition, string scheme )
        {
            if ( _originalScene == null )
            {
                _viewport.Target.BeforeUpdate += new RenderTargetEventHandler( BeforeUpdate );
                _viewport.Target.BeforeViewportUpdate += new RenderTargetViewportEventHandler( BeforeViewportUpdate );
                _viewport.Target.AfterUpdate += new RenderTargetEventHandler( AfterUpdate );
                _viewport.Target.AfterViewportUpdate += new RenderTargetViewportEventHandler( AfterViewportUpdate );

                /// Create base "original scene" compositor
                Compositor cbase = CompositorManager.Instance.Load( "Axiom/Scene", ResourceGroupManager.InternalResourceGroupName ) as Compositor;
                _originalScene = new CompositorInstance( cbase.GetSupportedTechnique(), this );
            }

            filter.Touch();
            CompositionTechnique tech = filter.GetSupportedTechnique( scheme );
            if ( tech == null )
            {
                /// Warn user
                LogManager.Instance.Write( LogMessageLevel.Critical, false, "CompositorChain: Compositor " + filter.Name + " has no supported techniques.", null );
            }

            CompositorInstance t = new CompositorInstance( tech, this );
            if ( addPosition == LastCompositor )
            {
                addPosition = _instances.Count;
            }
            else
            {
                Debug.Assert( addPosition <= _instances.Count, "Index out of bounds, CompositorChain.AddCompositor" );
            }
            _instances.Insert( addPosition, t );

            _dirty = true;
            _anyCompositorEnabled = true;
            return t;
        }

        /// <summary>
        /// Remove a compositor.
        /// </summary>
        /// <remarks>
        /// defaults to the end (last applied filter)
        /// </remarks>
        public void RemoveCompositor()
        {
            RemoveCompositor( LastCompositor );
        }

        /// <summary>
        /// Remove a compositor.
        /// </summary>
        /// <param name="position">Position in filter chain of filter to remove </param>
        public void RemoveCompositor( int position )
        {
            Debug.Assert( position <= _instances.Count, "Index out of bounds, CompositorChain.RemoveCompositor" );
            _instances.RemoveAt( position );
            _dirty = true;
        }

        /// <summary>
        /// Remove all compositors.
        /// </summary>
        public void RemoveAllCompositors()
        {
            _instances.Clear();
        }

        /// <summary>
        /// Get's compositor instance by position.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public CompositorInstance GetCompositor( int index )
        {
            Debug.Assert( index <= _instances.Count, "Index out of bounds, CompositorChain.GetCompositor" );
            return _instances[ index ];
        }

        /// <summary>
        /// Get compositor instance by name. 
        /// </summary>
        /// <param name="name"></param>
        /// <returns>Returns null if not found.</returns>
        public CompositorInstance GetCompositor( string name )
        {
            foreach ( CompositorInstance iter in _instances )
            {
                if ( iter.Name == name )
                {
                    return iter;
                }
            }

            return null;
        }

        /// <summary>
        /// Enable or disable a compositor, by position. Disabling a compositor stops it from rendering
        /// but does not free any resources. This can be more efficient than using removeCompositor and
        /// addCompositor in cases the filter is switched on and off a lot.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="state"></param>
        public void SetCompositorEnabled( int position, bool state )
        {
            CompositorInstance inst = GetCompositor( position );
            if ( !state && inst.Enabled )
            {
                // If we're disabling a 'middle' compositor in a chain, we have to be
                // careful about textures which might have been shared by non-adjacent
                // instances which have now become adjacent.
                CompositorInstance nextInstance = GetNextInstance( inst, true );
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
            inst.Enabled = state;
        }

        /// <summary>
        /// Mark state as dirty, and to be recompiled next frame.
        /// </summary>
        public void MarkDirty()
        {
            _dirty = true;
        }

        /// <summary>
        /// Remove a compositor by pointer. This is internally used by CompositionTechnique to
        /// "weak" remove any instanced of a deleted technique.
        /// </summary>
        /// <param name="instance"></param>
        public void RemoveInstance( CompositorInstance instance )
        {
            _instances.Remove( instance );
        }

        /// <summary>
        /// Internal method for registering a queued operation for deletion later
        /// </summary>
        /// <param name="op"></param>
        public void QueuedOperation( CompositorInstance.RenderSystemOperation op )
        {
            _rendersystemOperations.Add( op );
        }

        /// <summary>
        /// Compile this Composition chain into a series of RenderTarget operations.
        /// </summary>
        public void Compile()
        {
            ClearCompiledState();

            bool compositorsenabled = false;
            // force default scheme so materials for compositor quads will determined correctly
            MaterialManager matMgr = MaterialManager.Instance;
            string prevMaterialScheme = matMgr.ActiveScheme;
            matMgr.ActiveScheme = MaterialManager.DefaultSchemeName;

            /// Set previous CompositorInstance for each compositor in the list
            CompositorInstance lastComposition = _originalScene;
            _originalScene.PreviousInstance = null;
            CompositionPass pass = _originalScene.Technique.OutputTarget.GetPass( 0 );
            pass.ClearBuffers = _viewport.ClearBuffers;
            pass.ClearColor = _viewport.BackgroundColor;
            pass.ClearDepth = _viewport.ClearDepth;
            foreach ( CompositorInstance i in _instances )
            {
                if ( i.Enabled )
                {
                    compositorsenabled = true;
                    i.PreviousInstance = lastComposition;
                    lastComposition = i;
                }
            }

            /// Compile misc targets
            lastComposition.CompileTargetOperations( ref _compiledState );

            /// Final target viewport (0)
            _outputOperation.RenderSystemOperations.Clear();
            lastComposition.CompileOutputOperation( _outputOperation );

            // Deal with viewport settings
            if ( compositorsenabled != _anyCompositorEnabled )
            {
                _anyCompositorEnabled = compositorsenabled;
                if ( _anyCompositorEnabled )
                {
                    // Save old viewport clearing options
                    _oldClearEveryFrameBuffers = _viewport.ClearBuffers;
                    // Don't clear anything every frame since we have our own clear ops
                    _viewport.ClearEveryFrame = false;
                }
                else
                {
                    // Reset clearing options
                    _viewport.ClearEveryFrame = ( (int)_oldClearEveryFrameBuffers > 0 );
                    _viewport.ClearBuffers = _oldClearEveryFrameBuffers;
                }
            }

            // restore material scheme
            matMgr.ActiveScheme = prevMaterialScheme;
            _dirty = false;
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
            int begin = _instances.Count - 1;
            int end = 0;
            for ( ; begin >= end; begin-- )
            {
                if ( found )
                {
                    if ( _instances[ begin ].Enabled || !activeOnly )
                    {
                        return _instances[ begin ];
                    }
                }
                else if ( curr == _instances[ begin ] )
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
            for ( int i = 0; i < _instances.Count; i++ )
            {
                if ( found )
                {
                    if ( _instances[ i ].Enabled || !activeOnly )
                    {
                        return _instances[ i ];
                    }
                }
                else if ( _instances[ i ] == curr )
                {
                    found = true;
                }
            }

            return null;
        }

        /// <summary>
        /// Clear compiled state
        /// </summary>
        protected void ClearCompiledState()
        {
            _rendersystemOperations.Clear();

            // Clear compiled state
            _compiledState.Clear();
            _outputOperation = new CompositorInstance.TargetOperation( null );
        }

        /// <summary>
        /// Prepare a viewport, the camera and the scene for a rendering operation
        /// </summary>
        /// <param name="op"></param>
        /// <param name="vp"></param>
        /// <param name="cam"></param>
        protected void BeforeTargetOperation( CompositorInstance.TargetOperation op, Viewport vp, Camera cam )
        {
            if ( cam != null )
            {
                SceneManager sm = cam.SceneManager;
                /// Set up render target listener
                _ourListener.SetOperation( op, sm, sm.TargetRenderSystem );
                _ourListener.Viewport = vp;
                /// Register it
                sm.QueueStarted += _ourListener.RenderQueueStarted;
                sm.QueueEnded += _ourListener.RenderQueueEnded;
                /// Set visiblity mask
                _oldVisibilityMask = sm.VisibilityMask;
                sm.VisibilityMask = op.VisibilityMask;
                /// Set whether we find visibles
                _oldFindVisibleObjects = sm.FindVisibleObjectsBool;
                sm.FindVisibleObjectsBool = op.FindVisibleObjects;
                /// Set LOD bias level
                _oldLodBias = cam.LodBias;
                cam.LodBias = cam.LodBias * op.LodBias;
            }
            /// Set material scheme
            _oldMaterialScheme = vp.MaterialScheme;
            vp.MaterialScheme = op.MaterialScheme;
            /// Set shadows enabled
            _oldShadowsEnabled = vp.ShowShadows;
            vp.ShowShadows = op.ShadowsEnabled;
            /// XXX TODO
            //vp->setClearEveryFrame( true );
            //vp->setOverlaysEnabled( false );
            //vp->setBackgroundColour( op.clearColour );
        }

        /// <summary>
        /// Restore a viewport, the camera and the scene after a rendering operation
        /// </summary>
        /// <param name="op"></param>
        /// <param name="vp"></param>
        /// <param name="cam"></param>
        protected void AfterTargetOperation( CompositorInstance.TargetOperation op, Viewport vp, Camera cam )
        {
            if ( cam != null )
            {
                SceneManager sm = cam.SceneManager;
                /// Unregister our listener
                sm.QueueStarted -= _ourListener.RenderQueueStarted;
                sm.QueueEnded -= _ourListener.RenderQueueEnded;
                /// Restore default scene and camera settings
                sm.VisibilityMask = _oldVisibilityMask;
                sm.FindVisibleObjectsBool = _oldFindVisibleObjects;
                cam.LodBias = _oldLodBias;
            }

            vp.MaterialScheme = _oldMaterialScheme;
            vp.ShowShadows = _oldShadowsEnabled;
        }

        /// <summary>
        /// 
        /// </summary>
        protected void DestroyResources()
        {
            ClearCompiledState();
            if ( _viewport != null )
            {
                /// Destroy "original scene" compositor instance
                if ( _originalScene != null )
                {
                    _viewport.Target.BeforeUpdate -= BeforeUpdate;
                    _viewport.Target.BeforeViewportUpdate -= BeforeViewportUpdate;
                    _viewport.Target.AfterUpdate -= AfterUpdate;
                    _viewport.Target.AfterViewportUpdate -= AfterViewportUpdate;
                    _originalScene = null;
                }
                _viewport = null;
            }
        }
    }
}