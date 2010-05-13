#region LGPL License

/*
Axiom Graphics Engine Library
Copyright (C) 2003-20010  Axiom Project Team
 
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
    /// Delegate for handling material events.
    /// </summary>
    /// <param name="e"></param>
    public delegate void CompositorInstanceMaterialEventHandler( CompositorInstanceMaterialEventArgs e );

    /// <summary>
    /// Delegate for handling resource events.
    /// </summary>
    /// <param name="e"></param>
    public delegate void CompositorInstanceResourceEventHandler( CompositorInstanceResourceEventArgs e );

    /// <summary>
    /// 
    /// </summary>
    public class CompositorInstanceMaterialEventArgs : EventArgs
    {
        /// <summary>
        /// Pass identifier within Compositor instance, this is specified
        /// by the user by CompositionPass.SetIdentifier().
        /// </summary>
        public uint PassID;

        /// <summary>
        /// Material, this may be changed at will and will only affect
        /// the current instance of the Compositor, not the global material
        /// it was cloned from.
        /// </summary>
        public Material Material;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="passId"></param>
        /// <param name="material"></param>
        public CompositorInstanceMaterialEventArgs( uint passId, Material material )
        {
            PassID = passId;
            Material = material;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class CompositorInstanceResourceEventArgs : EventArgs
    {
        /// <summary>
        /// Was the creation because the viewport was resized?
        /// </summary>
        public bool ForResizeOnly;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="forResizeOnly"></param>
        public CompositorInstanceResourceEventArgs( bool forResizeOnly )
        {
            ForResizeOnly = forResizeOnly;
        }
    }

    /// <summary>
    /// An instance of a Compositor object for one Viewport. It is part of the CompositorChain
    /// for a Viewport.
    /// </summary>
    public class CompositorInstance : IDisposable
    {
        /// <summary>
        /// Notification of when a render target operation involving a material (like
        /// rendering a quad) is compiled, so that miscellaneous parameters that are different
        /// per Compositor instance can be set up.
        /// </summary>
        public event CompositorInstanceMaterialEventHandler MaterialSetup;

        /// <summary>
        /// Notification before a render target operation involving a material (like
        /// rendering a quad), so that material parameters can be varied.
        /// </summary>
        public event CompositorInstanceMaterialEventHandler MaterialRender;

        /// <summary>
        /// Notification after resources have been created (or recreated).
        /// </summary>
        public event CompositorInstanceResourceEventHandler ResourceCreated;

        /// <summary>
        /// Specific render system operation. A render target operation does special operations
        /// between render queues like rendering a quad, clearing the frame buffer or 
        /// setting stencil state.
        /// </summary>
        public abstract class RenderSystemOperation
        {
            /// <summary>
            /// Set state to SceneManager and RenderSystem
            /// </summary>
            /// <param name="sm"></param>
            /// <param name="rs"></param>
            public abstract void Execute( SceneManager sm, RenderSystem rs );
        }

        /// <summary>
        /// Operation setup for a RenderTarget (collected).
        /// </summary>
        public class TargetOperation
        {
            /// <summary>
            /// Target
            /// </summary>
            public RenderTarget Target { get; set; }

            /// <summary>
            /// Current group ID
            /// </summary>
            public RenderQueueGroupID CurrentQueueGroupID { get; set; }

            /// <summary>
            /// RenderSystem operations to queue into the scene manager, by uint8
            /// </summary>
            public List<Axiom.Math.Tuple<int, RenderSystemOperation>> RenderSystemOperations { get; protected set; }

            /// <summary>
            /// Scene visibility mask
            /// If this is 0, the scene is not rendered at all
            /// </summary>
            public ulong VisibilityMask { get; set; }

            /// <summary>
            /// LOD offset. This is multiplied with the camera LOD offset
            ///  1.0 is default, lower means lower detail, higher means higher detail
            /// </summary>
            public float LodBias { get; set; }

            /// <summary>
            /// A set of render queues to either include or exclude certain render queues.
            /// </summary>
            public BitArray RenderQueues { get; set; }

            /// <summary>
            /// <see cref="CompositionTargetPass._onlyInitial"/>
            /// </summary>
            public bool OnlyInitial { get; set; }

            /// <summary>
            /// "Has been rendered" flag; used in combination with
            /// onlyInitial to determine whether to skip this target operation.
            /// </summary>
            public bool HasBeenRendered { get; set; }

            /// <summary>
            /// Whether this op needs to find visible scene objects or not
            /// </summary>
            public bool FindVisibleObjects { get; set; }

            /// <summary>
            /// Which material scheme this op will use
            /// </summary>
            public string MaterialScheme { get; set; }

            /// <summary>
            /// Whether shadows will be enabled or not
            /// </summary>
            public bool ShadowsEnabled { get; set; }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="target"></param>
            public TargetOperation( RenderTarget target )
            {
                Target = target;
                CurrentQueueGroupID = 0;
                VisibilityMask = 0xFFFFFFFF;
                LodBias = 1.0f;
                ShadowsEnabled = true;
                RenderQueues = new BitArray( (int)RenderQueueGroupID.Count );   
                RenderSystemOperations = new List<Tuple<int, RenderSystemOperation>>();
                MaterialScheme = MaterialManager.DefaultSchemeName;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private static int _dummyCounter = 0;

        /// <summary>
        /// Composition technique used by this instance
        /// </summary>
        private CompositionTechnique _technique;

        /// <summary>
        /// Compositor of which this is an instance
        /// </summary>
        private Compositor _compositor;

        /// <summary>
        /// Composition chain of which this instance is part
        /// </summary>
        private CompositorChain _chain;

        /// <summary>
        /// Is this instance enabled?
        /// </summary>
        private bool _enabled;

        /// <summary>
        /// Map from name->local texture
        /// </summary>
        private Dictionary<string, Texture> _localTextures = new Dictionary<string, Texture>();

        /// <summary>
        /// Store a list of MRTs we've created
        /// </summary>
        private Dictionary<string, MultiRenderTarget> _localMRTS = new Dictionary<string, MultiRenderTarget>();

        /// <summary>
        /// Textures that are not currently in use, but that we want to keep for now,
        /// for example if we switch techniques but want to keep all textures available
        /// in case we switch back.
        /// </summary>
        private Dictionary<CompositionTechnique.TextureDefinition, Texture> _reservedTextures = new Dictionary<CompositionTechnique.TextureDefinition, Texture>();

        /// <summary>
        /// Previous instance (set by chain)
        /// </summary>
        private CompositorInstance _previousInstance;

        /// <summary>
        /// The scheme which is being used in this instance
        /// </summary>
        private string _activeScheme;

        /// <summary>
        /// Get CompositionTechnique used by this instance
        /// </summary>
        public CompositionTechnique Technique
        {
            get
            {
                return _technique;
            }
            set
            {
                SetTechnique( _technique );
            }
        }

        /// <summary>
        /// Get's or Set's enabled flag. The compositor instance will only render if it is
        /// enabled, otherwise it is pass-through.
        /// </summary>
        public bool Enabled
        {
            get
            {
                return _enabled;
            }
            set
            {
                if ( _enabled != value )
                {
                    _enabled = value;
                    // Create of free resource.
                    if ( value )
                    {
                        CreateResources( false );
                    }
                    else
                    {
                        FreeResources( false, true );
                    }
                    /// Notify chain state needs recompile.
                    _chain.MarkDirty();
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Previous instance (set by chain)
        /// </summary>
        public CompositorInstance PreviousInstance
        {
            get
            {
                return _previousInstance;
            }
            set
            {
                _previousInstance = value;
            }
        }

        /// <summary>
        /// Get's Compositor of which this is an instance
        /// </summary>
        public Compositor Compositor
        {
            get
            {
                return _compositor;
            }
        }

        /// <summary>
        /// Pick a technique to use to render this compositor based on a scheme.
        /// </summary>
        public string ActiveScheme
        {
            get
            {
                return _activeScheme;
            }
            set
            {
                SetScheme( value );
            }
        }

        /// <summary>
        /// Get's Chain that this instance is part of
        /// </summary>
        public CompositorChain Chain
        {
            get
            {
                return _chain;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="technique"></param>
        /// <param name="chain"></param>
        public CompositorInstance( CompositionTechnique technique, CompositorChain chain )
        {
            _compositor = technique.Parent;
            _technique = technique;
            _chain = chain;
            _enabled = false;

            string logicName = _technique.CompositorLogicName;
            if ( !String.IsNullOrEmpty( logicName ) )
            {
                CompositorManager.Instance.GetCompositorLogic( logicName ).CompositorInstanceCreated( this );
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void Dispose()
        {
            if ( _technique != null )
            {
                string logicName = _technique.CompositorLogicName;
                if ( !String.IsNullOrEmpty( logicName ) && CompositorManager.Instance != null )
                {
                    CompositorManager.Instance.GetCompositorLogic( logicName ).CompositorInstanceDestroyed( this );
                }
            }

            FreeResources( false, true );
        }

        /// <summary>
        /// Get the instance name for a local texture.
        /// </summary>
        /// <param name="name">The name of the texture in the original compositor definition</param>
        /// <param name="mrtIndex">If name identifies a MRT, which texture attachment to retrieve</param>
        /// <returns>The instance name for the texture, corresponds to a real texture</returns>
        /// <note>
        /// It is only valid to call this when local textures have been loaded,
        /// which in practice means that the compositor instance is active. Calling
        /// it at other times will cause an exception. Note that since textures
        /// are cleaned up aggressively, this name is not guaranteed to stay the
        /// same if you disable and re-enable the compositor instance.
        /// </note>
        public string GetTextureInstanceName( string name, int mrtIndex )
        {
            return GetSourceForTex( name, mrtIndex );
        }

        /// <summary>
        /// Get the instance of a local texture.
        /// </summary>
        /// <param name="name">The name of the texture in the original compositor definition</param>
        /// <param name="mrt">IndexIf name identifies a MRT, which texture attachment to retrieve</param>
        /// <returns>The texture pointer, corresponds to a real texture</returns>
        /// <note>
        /// Textures are only valid when local textures have been loaded,
        /// which in practice means that the compositor instance is active. Calling
        /// this method at other times will return null pointers. Note that since textures
        /// are cleaned up aggressively, this pointer is not guaranteed to stay the
        /// same if you disable and re-enable the compositor instance.
        /// </note>
        public Texture GetTextureInstance( string name, int mrtIndex )
        {
            // try simple textures first
            Texture ret = null;
            if ( !_localTextures.TryGetValue( name, out ret ) )
            {
                // try MRTs - texture (rather than target)
                _localTextures.TryGetValue( GetMRTTexLocalName( name, mrtIndex ), out ret );
            }
            return ret;
        }

        /// <summary>
        /// Get the render target for a given render texture name.
        /// </summary>
        /// <param name="name">name of the render target</param>
        /// <returns>render target for the given name</returns>
        /// <remarks>
        /// You can use this to add listeners etc, but do not use it to update the
        /// targets manually or any other modifications, the compositor instance
        /// is in charge of this.
        /// </remarks>
        public RenderTarget GetRenderTarget( string name )
        {
            return GetTargetForTex( name );
        }

        /// <summary>
        ///  Recursively collect target states (except for final Pass).
        /// </summary>
        /// <param name="compiledState">This list will contain a list of TargetOperation objects</param>
        public virtual void CompileTargetOperations( ref List<TargetOperation> compiledState )
        {
            // Collect targets of previous state
            if ( _previousInstance != null )
            {
                _previousInstance.CompileTargetOperations( ref compiledState );
            }
            // Texture targets
            foreach ( CompositionTargetPass target in _technique.TargetPasses )
            {
                TargetOperation ts = new TargetOperation( GetTargetForTex( target.OutputName ) );
                // Set "only initial" flag, visibilityMask and lodBias according to CompositionTargetPass.
                ts.OnlyInitial = target.OnlyInitial;
                ts.VisibilityMask = target.VisibilityMask;
                ts.LodBias = target.LodBias;
                ts.ShadowsEnabled = target.ShadowsEnabled;
                // Check for input mode previous
                if ( target.InputMode == CompositorInputMode.Previous )
                {
                    // Collect target state for previous compositor
                    // The TargetOperation for the final target is collected seperately as it is merged
                    // with later operations
                    _previousInstance.CompileOutputOperation( ts );
                }
                // Collect passes of our own target
                CollectPasses( ts, target );
                compiledState.Add( ts );
            }
        }

        /// <summary>
        /// Compile the final (output) operation. This is done separately because this
        /// is combined with the input in chained filters.
        /// </summary>
        /// <param name="finalState"></param>
        public virtual void CompileOutputOperation( TargetOperation finalState )
        {
            // Final target
            CompositionTargetPass tpass = _technique.OutputTarget;
            // Logical-and together the visibilityMask, and multiply the lodBias
            finalState.VisibilityMask &= tpass.VisibilityMask;
            finalState.LodBias *= tpass.LodBias;
            if ( tpass.InputMode == CompositorInputMode.Previous )
            {
                // Collect target state for previous compositor
                // The TargetOperation for the final target is collected seperately as it is merged
                // with later operations
                _previousInstance.CompileOutputOperation( finalState );
            }
            // Collect passes
            CollectPasses( finalState, tpass );
        }

        /// <summary>
        /// Destroy local rendertextures and other resources.
        /// </summary>
        /// <param name="forResizeOnly"></param>
        /// <param name="clearReserverTextures"></param>
        /// <returns></returns>
        public void FreeResources( bool forResizeOnly, bool clearReserverTextures )
        {
            // Remove temporary textures
            // We only remove those that are not shared, shared textures are dealt with
            // based on their reference count.
            // We can also only free textures which are derived from the target size, if
            // required (saves some time & memory thrashing / fragmentation on resize)
            List<Texture> assignedTextures = new List<Texture>();
            foreach ( CompositionTechnique.TextureDefinition def in _technique.TextureDefinitions )
            {
                if ( !string.IsNullOrEmpty( def.ReferenceCompositorName ) )
                {
                    //This is a reference, isn't created here
                    continue;
                }
                // potentially only remove this one if based on size
                if ( !forResizeOnly || def.Width == 0 | def.Height == 0 )
                {
                    int subsurf = def.FormatList.Count;
                    // Potentially many surfaces
                    for ( int s = 0; s < subsurf; s++ )
                    {
                        string texName = subsurf > 1 ? GetMRTTexLocalName( def.Name, s ) : def.Name;
                        Texture tex = null;
                        if ( _localTextures.TryGetValue( texName, out tex ) )
                        {
                            if ( !def.Pooled && def.Scope != CompositionTechnique.TextureScope.Global )
                            {
                                // remove myself from central only if not pooled and not global
                                TextureManager.Instance.Remove( tex.Name );
                            }
                            _localTextures.Remove( texName );
                        }
                    } //subsurf
                    if ( subsurf > 1 )
                    {
                        MultiRenderTarget i = null;
                        if ( _localMRTS.TryGetValue( def.Name, out i ) )
                        {
                            if ( def.Scope != CompositionTechnique.TextureScope.Global )
                            {
                                // remove MRT if not global
                                Root.Instance.RenderSystem.DestroyRenderTarget( i.Name );
                            }
                            _localMRTS.Remove( def.Name );
                        }
                    }
                } // not for resize or width/height 0
            } //end foreach

            if ( clearReserverTextures )
            {
                if ( forResizeOnly )
                {
                    List<CompositionTechnique.TextureDefinition> toDelete = new List<CompositionTechnique.TextureDefinition>();
                    foreach ( CompositionTechnique.TextureDefinition def in _reservedTextures.Keys )
                    {
                        if ( def.Width == 0 || def.Height == 0 )
                        {
                            toDelete.Add( def );
                        }
                    }
                    // just remove the ones which would be affected by a resize
                    for ( int i = 0; i < toDelete.Count; i++ )
                    {
                        _reservedTextures.Remove( toDelete[ i ] );
                    }
                    toDelete = null;
                }
                else
                {
                    // clear all
                    _reservedTextures.Clear();
                }
            }
            // Now we tell the central list of textures to check if its unreferenced,
            // and to remove if necessary. Anything shared that was left in the reserve textures
            // will not be released here
            CompositorManager.Instance.FreePooledTextures( true );
        }

        /// <summary>
        /// Create local rendertextures and other resources. Builds _localTextures.
        /// </summary>
        /// <param name="forResizeOnly"></param>
        public void CreateResources( bool forResizeOnly )
        {
            // Create temporary textures
            // In principle, temporary textures could be shared between multiple viewports
            // (CompositorChains). This will save a lot of memory in case more viewports
            // are composited.
            List<Texture> assignedTextures = new List<Texture>();
            foreach ( CompositionTechnique.TextureDefinition def in _technique.TextureDefinitions )
            {
                //This is a reference, isn't created in this compositor
                if ( !string.IsNullOrEmpty( def.ReferenceCompositorName ) )
                {
                    continue;
                }
                //This is a global texture, just link the created resources from the parent
                if ( def.Scope == CompositionTechnique.TextureScope.Global )
                {
                    Compositor parentComp = _technique.Parent;
                    if ( def.FormatList.Count > 1 )
                    {
                        int atch = 0;
                        foreach ( Axiom.Media.PixelFormat p in def.FormatList )
                        {
                            Texture tex = parentComp.GetTextureInstance( def.Name, atch++ );
                            _localTextures.Add( GetMRTTexLocalName( def.Name, atch ), tex );
                        }
                        MultiRenderTarget mrt = (MultiRenderTarget)parentComp.GetRenderTarget( def.Name );
                        _localMRTS.Add( def.Name, mrt );
                    }
                    else
                    {
                        Texture tex = parentComp.GetTextureInstance( def.Name, 0 );
                        _localTextures.Add( def.Name, tex );
                    }
                    continue;
                }
                // Determine width and height
                int width = def.Width;
                int height = def.Height;
                int fsaa;
                string fsaahint;
                bool hwGamma;
                // Skip this one if we're only (re)creating for a resize & it's not derived
                // from the target size
                if ( forResizeOnly && width != 0 && height != 0 )
                {
                    continue;
                }

                DeriveTextureRenderTargetOptions( def.Name, out hwGamma, out fsaa, out fsaahint );

                if ( width == 0 )
                {
                    width = (int)( _chain.Viewport.ActualWidth * def.WidthFactor );
                }
                if ( height == 0 )
                {
                    height = (int)( _chain.Viewport.ActualHeight * def.HeightFactor );
                }

                // determine options as a combination of selected options and possible options
                if ( !def.FSAA )
                {
                    fsaa = 0;
                    fsaahint = string.Empty;
                }
                hwGamma = hwGamma || def.HWGammaWrite;

                // Make the tetxure
                RenderTarget rendTarget;
                if ( def.FormatList.Count > 1 )
                {
                    string mrtBaseName = "c" + _dummyCounter++ +
                                         "/" + def.Name + "/" + _chain.Viewport.Target.Name;
                    MultiRenderTarget mrt =
                        Root.Instance.RenderSystem.CreateMultiRenderTarget( mrtBaseName );
                    _localMRTS.Add( mrtBaseName, mrt );

                    // create and bind individual surfaces
                    int atch = 0;
                    foreach ( Axiom.Media.PixelFormat p in def.FormatList )
                    {
                        string texName = mrtBaseName + "/" + atch;
                        string mrtLocalName = GetMRTTexLocalName( def.Name, atch );
                        Texture tex;
                        if ( def.Pooled )
                        {
                            // get / create pooled texture
                            tex = CompositorManager.Instance.GetPooledTexture( texName,
                                                                               mrtLocalName,
                                                                               width, height, p, fsaa, fsaahint, hwGamma && !Axiom.Media.PixelUtil.IsFloatingPoint( p ),
                                                                               ref assignedTextures, this, def.Scope );
                        }
                        else
                        {
                            tex = TextureManager.Instance.CreateManual(
                                texName, ResourceGroupManager.InternalResourceGroupName, TextureType.TwoD,
                                width, height, 0, p, TextureUsage.RenderTarget, null, hwGamma && !Axiom.Media.PixelUtil.IsFloatingPoint( p ),
                                fsaa, fsaahint );
                        }

                        RenderTexture rt = tex.GetBuffer().GetRenderTarget();
                        rt.IsAutoUpdated = false;
                        mrt.BindSurface( atch++, rt );
                        // Also add to local textures so we can look up
                        _localTextures.Add( mrtLocalName, tex );
                    }

                    rendTarget = mrt;
                }
                else
                {
                    string texName = "c" + _dummyCounter++ + "/" + def.Name + "/" + _chain.Viewport.Target.Name;
                    // space in the name mixup the cegui in the compositor demo
                    // this is an auto generated name - so no spaces can't hart us.
                    texName = texName.Replace( ' ', '_' );

                    Texture tex;
                    if ( def.Pooled )
                    {
                        // get / create pooled texture
                        tex = CompositorManager.Instance.GetPooledTexture( texName,
                                                                           def.Name, width, height, def.FormatList[ 0 ], fsaa, fsaahint,
                                                                           hwGamma && !Axiom.Media.PixelUtil.IsFloatingPoint( def.FormatList[ 0 ] ), ref assignedTextures,
                                                                           this, def.Scope );
                    }
                    else
                    {
                        tex = TextureManager.Instance.CreateManual(
                            texName, ResourceGroupManager.InternalResourceGroupName, TextureType.TwoD,
                            width, height, 0, def.FormatList[ 0 ], TextureUsage.RenderTarget, null,
                            hwGamma && !Axiom.Media.PixelUtil.IsFloatingPoint( def.FormatList[ 0 ] ), fsaa, fsaahint );
                    }

                    rendTarget = tex.GetBuffer().GetRenderTarget();
                    _localTextures.Add( def.Name, tex );
                }

                //Set DepthBuffer pool for sharing
                rendTarget.DepthBufferPool = def.DepthBufferID;

                // Set up viewport over entire texture
                rendTarget.IsAutoUpdated = false;

                // We may be sharing / reusing this texture, so test before adding viewport
                if ( rendTarget.ViewportCount == 0 )
                {
                    Viewport v = null;
                    Camera camera = _chain.Viewport.Camera;
                    if ( camera == null )
                    {
                        v = rendTarget.AddViewport( camera );
                    }
                    else
                    {
                        // Save last viewport and current aspect ratio
                        Viewport oldViewport = camera.Viewport;
                        float aspectRatio = camera.AspectRatio;
                        v = rendTarget.AddViewport( camera );
                        // Should restore aspect ratio, in case of auto aspect ratio
                        // enabled, it'll changed when add new viewport.
                        camera.AspectRatio = aspectRatio;
                        // Should restore last viewport, i.e. never disturb user code
                        // which might based on that.
                        camera.NotifyViewport( oldViewport );
                    }
                    v.ClearEveryFrame = false;
                    v.ShowOverlays = false;
                    v.BackgroundColor = new ColorEx( 0, 0, 0, 0 );
                }
            }

            OnResourceCreated( new CompositorInstanceResourceEventArgs( forResizeOnly ) );
        }

        public void NotifyCameraChanged( Camera camera )
        {
            // update local texture's viewports.
            foreach ( Texture localTexIter in _localTextures.Values )
            {
                RenderTexture target = localTexIter.GetBuffer().GetRenderTarget();
                // skip target that has no viewport (this means texture is under MRT)
                if ( target.ViewportCount == 1 )
                {
                    target.GetViewport( 0 ).Camera = camera;
                }
            }

            // update MRT's viewports
            foreach ( MultiRenderTarget localMRTIter in _localMRTS.Values )
            {
                MultiRenderTarget target = localMRTIter;
                target.GetViewport( 0 ).Camera = camera;
            }
        }

        /// <summary>
        /// Notify this instance that the primary surface has been resized.
        /// </summary>
        /// <remarks>
        /// This will allow the instance to recreate its resources that
        /// are dependent on the size.
        /// </remarks>
        public void NotifyResized()
        {
            FreeResources( true, true );
            CreateResources( true );
        }

        /// <summary>
        /// Change the technique we're using to render this compositor.
        /// </summary>
        /// <param name="tech">The technique to use (must be supported and from the same Compositor)</param>
        public void SetTechnique( CompositionTechnique tech )
        {
            SetTechnique( tech, true );
        }

        /// <summary>
        /// Change the technique we're using to render this compositor.
        /// </summary>
        /// <param name="tech">The technique to use (must be supported and from the same Compositor)</param>
        /// <param name="reuseTextures">If textures have already been created for the current
        /// technique, whether to try to re-use them if sizes & formats match.
        /// </param>
        public void SetTechnique( CompositionTechnique tech, bool reuseTextures )
        {
            if ( _technique != tech )
            {
                if ( reuseTextures )
                {
                    // make sure we store all (shared) textures in use in our reserve pool
                    // this will ensure they don't get destroyed as unreferenced
                    // so they're ready to use again later
                    foreach ( CompositionTechnique.TextureDefinition def in _technique.TextureDefinitions )
                    {
                        if ( def.Pooled )
                        {
                            Texture i = null;
                            if ( _localTextures.TryGetValue( def.Name, out i ) )
                            {
                                // overwriting duplicates is fine, we only want one entry per def
                                if ( _reservedTextures.ContainsKey( def ) )
                                {
                                    _reservedTextures.Remove( def );
                                }

                                _reservedTextures.Add( def, i );
                            }
                        }
                    } //end foreach
                } //end if
                _technique = tech;
                if ( _enabled )
                {
                    // free up resources, but keep reserves if reusing
                    FreeResources( false, !reuseTextures );
                    CreateResources( false );
                    /// Notify chain state needs recompile.
                    _chain.MarkDirty();
                }
            } //end if
        }

        /// <summary>
        /// Pick a technique to use to render this compositor based on a scheme.
        /// </summary>
        /// <remarks>
        /// If there is no specific supported technique with this scheme name,
        /// then the first supported technique with no specific scheme will be used.
        /// <see cref=" CompositionTechnique.SchemeName"/>
        /// </remarks>
        /// <param name="schemeName">The scheme to use</param>
        public void SetScheme( string schemeName )
        {
            SetScheme( schemeName, true );
        }

        /// <summary>
        /// Pick a technique to use to render this compositor based on a scheme.
        /// </summary>
        /// <remarks>
        /// If there is no specific supported technique with this scheme name,
        /// then the first supported technique with no specific scheme will be used.
        /// <see cref=" CompositionTechnique.SchemeName"/>
        /// </remarks>
        /// <param name="schemeName">The scheme to use</param>
        /// <param name="reuseTextures">
        /// If textures have already been created for the current
        /// technique, whether to try to re-use them if sizes & formats match.
        /// Note that for this feature to be of benefit, the textures must have been created
        /// with the 'pooled' option enabled.
        /// </param>
        public void SetScheme( string schemeName, bool reuseTextures )
        {
            CompositionTechnique tech = _compositor.GetSupportedTechnique( schemeName );
            if ( tech != null )
            {
                SetTechnique( tech, reuseTextures );
            }
        }

        /// <summary>
        /// Collect rendering passes. Here, passes are converted into render target operations
        /// and queued with queueRenderSystemOp.
        /// </summary>
        /// <param name="finalState"></param>
        /// <param name="target"></param>
        private void CollectPasses( TargetOperation finalState, CompositionTargetPass target )
        {
            /// Here, passes are converted into render target operations
            Pass targetPass = null;
            Technique srctech = null;
            Material mat = null, srcmat = null;

            foreach ( CompositionPass pass in target.Passes )
            {
                switch ( pass.Type )
                {
                    case CompositorPassType.Clear:
                    {
                        QueueRendersystemOp( finalState, new RSClearOperation(
                                                             pass.ClearBuffers,
                                                             pass.ClearColor,
                                                             pass.ClearDepth,
                                                             pass.ClearStencil ) );
                    }
                        break;
                    case CompositorPassType.Stencil:
                    {
                        QueueRendersystemOp( finalState, new RSStencilOperation(
                                                             pass.StencilCheck, pass.StencilFunc, pass.StencilRefValue,
                                                             pass.StencilMask, pass.StencilFailOp, pass.StencilDepthFailOp,
                                                             pass.StencilPassOp, pass.StencilTwoSidedOperation ) );
                    }
                        break;
                    case CompositorPassType.RenderScene:
                    {
                        if ( pass.FirstRenderQueue < finalState.CurrentQueueGroupID )
                        {
                            // Mismatch -- warn user
                            // XXX We could support repeating the last queue, with some effort
                            LogManager.Instance.Write(
                                "Warning in compilation of Compositor " +
                                _compositor.Name + ": Attempt to render queue " +
                                pass.FirstRenderQueue.ToString() + " before " +
                                finalState.CurrentQueueGroupID.ToString(), null );
                        }
                        RSSetSchemeOperation setSchemeOperation = null;
                        if ( pass.MaterialScheme != string.Empty )
                        {
                            //Add the triggers that will set the scheme and restore it each frame
                            finalState.CurrentQueueGroupID = pass.FirstRenderQueue;
                            setSchemeOperation = new RSSetSchemeOperation( pass.MaterialScheme );
                            QueueRendersystemOp( finalState, setSchemeOperation );
                        }
                        // Add render queues
                        for ( int x = (int)pass.FirstRenderQueue; x < (int)pass.LastRenderQueue; x++ )
                        {
                            Debug.Assert( x >= 0 );
                            finalState.RenderQueues.Set( x, true );
                        }
                        finalState.CurrentQueueGroupID = pass.LastRenderQueue + 1;
                        if ( setSchemeOperation != null )
                        {
                            //Restoring the scheme after the queues have been rendered
                            QueueRendersystemOp( finalState, new RSRestoreSchemeOperation( setSchemeOperation ) );
                        }
                        finalState.FindVisibleObjects = true;
                        finalState.MaterialScheme = target.MaterialScheme;
                        finalState.ShadowsEnabled = target.ShadowsEnabled;
                    }
                        break;
                    case CompositorPassType.RenderQuad:
                    {
                        srcmat = pass.Material;
                        if ( srcmat == null )
                        {
                            // No material -- warn user
                            LogManager.Instance.Write( "Warning in compilation of Compositor " +
                                                       _compositor.Name + ": No material defined for composition pass", null );
                            break;
                        }
                        srcmat.Load();
                        if ( srcmat.SupportedTechniques.Count == 0 )
                        {
                            // No supported techniques -- warn user
                            LogManager.Instance.Write( "Warning in compilation of Compositor " +
                                                       _compositor.Name + ": material " + srcmat.Name + " has no supported techniques", null );
                            break;
                        }

                        srctech = srcmat.GetBestTechnique( 0 );
                        // Create local material
                        mat = CreateLocalMaterial( srcmat.Name );
                        // Copy and adapt passes from source material
                        for ( int i = 0; i < srctech.PassCount; i++ )
                        {
                            Pass srcpass = srctech.GetPass( i );
                            // Create new target pass
                            targetPass = mat.GetTechnique( 0 ).CreatePass();
                            srcpass.CopyTo( targetPass );
                            // Set up inputs
                            for ( int x = 0; x < pass.NumInputs; x++ )
                            {
                                CompositionPass.InputTexture inp = pass.GetInput( x );
                                if ( !string.IsNullOrEmpty( inp.Name ) )
                                {
                                    if ( x < targetPass.TextureUnitStageCount )
                                    {
                                        targetPass.GetTextureUnitState( x ).SetTextureName( GetSourceForTex( inp.Name, inp.MRTIndex ) );
                                    }
                                    else
                                    {
                                        // Texture unit not there
                                        LogManager.Instance.Write( "Warning in compilation of Compositor " +
                                                                   _compositor.Name + ": material " + srcmat.Name + " texture unit " +
                                                                   x.ToString() + " out of bounds.", null );
                                    }
                                }
                            } //end for inputs.length
                        } //end for passcount

                        RSQuadOperation rsQuadOperation = new RSQuadOperation( this, pass.Identifier, mat );
                        float left, top, right, bottom;
                        if ( pass.GetQuadCorners( out left, out top, out right, out bottom ) )
                        {
                            rsQuadOperation.SetQuadCorners( left, top, right, bottom );
                            rsQuadOperation.SetQuadFarCorners( pass.QuadFarCorners, pass.QuadFarCornersViewSpace );
                            QueueRendersystemOp( finalState, rsQuadOperation );
                        }
                    }
                        break;
                    case CompositorPassType.RenderCustom:
                    {
                        RenderSystemOperation customOperation = CompositorManager.Instance.GetCustomCompositionPass
                            ( pass.CustomType ).CreateOperation( this, pass );
                        QueueRendersystemOp( finalState, customOperation );
                    }
                        break;
                } //end switch pass.type
            } //end foreach
        }

        /// <summary>
        /// Create a local dummy material with one technique but no passes.
        /// The material is detached from the Material Manager to make sure it is destroyed
        /// when going out of scope.
        /// </summary>
        /// <param name="srcName"></param>
        /// <returns></returns>
        private Material CreateLocalMaterial( string srcName )
        {
            Material mat = (Material)
                           MaterialManager.Instance.Create(
                               "c" + _dummyCounter++ + "/" + srcName, ResourceGroupManager.InternalResourceGroupName );

            MaterialManager.Instance.Remove( mat.Name );
            mat.GetTechnique( 0 ).RemoveAllPasses();
            return mat;
        }

        /// <summary>
        ///  Get RenderTarget for a named local texture.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private RenderTarget GetTargetForTex( string name )
        {
            // try simple texture
            Texture ret;
            if ( _localTextures.TryGetValue( name, out ret ) )
            {
                return ret.GetBuffer().GetRenderTarget();
            }
            MultiRenderTarget mrt = null;
            // try MRTs
            if ( _localMRTS.TryGetValue( name, out mrt ) )
            {
                return (RenderTarget)mrt;
            }
            else
            {
                throw new Exception( "Non-existent local texture named " + name + ", CompositorInstance.GetTargetForTex()" );
            }
        }

        /// <summary>
        /// Get source texture name for a named local texture.
        /// </summary>
        /// <param name="name">The local name of the texture as given to it in the compositor</param>
        /// <returns></returns>
        private string GetSourceForTex( string name )
        {
            return GetSourceForTex( name, 0 );
        }

        /// <summary>
        /// Get source texture name for a named local texture.
        /// </summary>
        /// <param name="name">The local name of the texture as given to it in the compositor</param>
        /// <param name="mrtIndex">For MRTs, which attached surface to retrieve</param>
        /// <returns></returns>
        private string GetSourceForTex( string name, int mrtIndex )
        {
            CompositionTechnique.TextureDefinition texDef = _technique.GetTextureDefinition( name );
            if ( !string.IsNullOrEmpty( texDef.ReferenceCompositorName ) )
            {
                //This is a reference - find the compositor and referenced texture definition
                Compositor refComp = CompositorManager.Instance.GetByName( texDef.ReferenceCompositorName ) as Compositor;
                if ( refComp == null )
                {
                    throw new Exception( "Referencing non-existent compositor texture, CompositorInstance.GetSourceForTex()" );
                }
                CompositionTechnique.TextureDefinition reflTexDef = refComp.GetSupportedTechnique().GetTextureDefinition( texDef.ReferenceTextureName );
                if ( reflTexDef == null )
                {
                    throw new Exception( "Referencing non-existent compositor texture, CompositorInstance.GetSourceForTex()" );
                }

                switch ( reflTexDef.Scope )
                {
                    case CompositionTechnique.TextureScope.Chain:
                    {
                        //Find the instance and check if it is before us
                        CompositorInstance refCompInst = null;
                        bool beforeMe = true;
                        foreach ( CompositorInstance nextCompInst in _chain.Instances )
                        {
                            if ( nextCompInst.Compositor.Name == texDef.ReferenceCompositorName )
                            {
                                refCompInst = nextCompInst;
                                break;
                            }
                            if ( nextCompInst == this )
                            {
                                //We encountered ourselves while searching for the compositor -
                                //we are earlier in the chain.
                                beforeMe = false;
                            }
                        }
                        if ( refCompInst == null || !refCompInst.Enabled )
                        {
                            throw new Exception( "Referencing inactive compositor texture, CompositorInstance.GetSourceForTex()" );
                        }
                        if ( !beforeMe )
                        {
                            throw new Exception( "Referencing compositor that is later in the chain, CompositorInstance.GetSourceForTex()" );
                        }
                        return refCompInst.GetTextureInstanceName( texDef.ReferenceTextureName, mrtIndex );
                    }
                    case CompositionTechnique.TextureScope.Global:
                    {
                        //Chain and global case - the referenced compositor will know how to handle
                        return refComp.GetTextureInstanceName( texDef.ReferenceTextureName, mrtIndex );
                    }
                    case CompositionTechnique.TextureScope.Local:
                    default:
                    {
                        throw new Exception( "Referencing local compositor texture, CompositorInstance.GetSourceForTex()" );
                    }
                } //end switch
            } // End of handling texture references

            if ( texDef.FormatList.Count == 1 ) //This is a simple texture
            {
                Texture tex = null;
                if ( _localTextures.TryGetValue( name, out tex ) )
                {
                    return tex.Name;
                }
            }
            else // try MRTs - texture (rather than target)
            {
                Texture tex = null;
                if ( _localTextures.TryGetValue( GetMRTTexLocalName( name, mrtIndex ), out tex ) )
                {
                    return tex.Name;
                }
            }

            throw new Exception( "Non-existent local texture name, CompositorInstance.GetSourceForTex()" );
        }

        /// <summary>
        /// Queue a render system operation.
        /// </summary>
        /// <param name="finalState"></param>
        /// <param name="op"></param>
        private void QueueRendersystemOp( TargetOperation finalState, RenderSystemOperation op )
        {
            /// Store operation for current QueueGroup ID
            finalState.RenderSystemOperations.Add( new Axiom.Math.Tuple<int, RenderSystemOperation>( (int)finalState.CurrentQueueGroupID, op ) );
            /// Tell parent for deletion
            _chain.QueuedOperation( op );
        }

        /// <summary>
        /// Util method for assigning a local texture name to a MRT attachment
        /// </summary>
        /// <param name="baseName"></param>
        /// <param name="attachment"></param>
        /// <returns></returns>
        private string GetMRTTexLocalName( string baseName, int attachment )
        {
            return baseName + "/" + attachment.ToString();
        }

        /// <summary>
        /// Search for options like AA and hardware gamma which we may want to
        /// inherit from the main render target to which we're attached.
        /// </summary>
        /// <param name="texname"></param>
        /// <param name="hwGammaWrite"></param>
        /// <param name="fsaa"></param>
        /// <param name="fsaaHint"></param>
        private void DeriveTextureRenderTargetOptions( string texname,
                                                       out bool hwGammaWrite, out int fsaa, out string fsaaHint )
        {
            // search for passes on this texture def that either include a render_scene
            // or use input previous
            bool renderingScene = false;
            foreach ( CompositionTargetPass tp in _technique.TargetPasses )
            {
                if ( tp.OutputName == texname )
                {
                    if ( tp.InputMode == CompositorInputMode.Previous )
                    {
                        // this may be rendering the scene implicitly
                        // Can't check _previousInstance against _Chain.OriginalSceneCompositor
                        // at this time, so check the position
                        renderingScene = true;
                        foreach ( CompositorInstance inst in _chain.Instances )
                        {
                            if ( inst == this )
                            {
                                break;
                            }
                            else if ( inst.Enabled )
                            {
                                // nope, we have another compositor before us, this will
                                // be doing the AA
                                renderingScene = false;
                            }
                        }
                        if ( renderingScene )
                        {
                            break;
                        }
                    }
                    else
                    {
                        // look for a render_scene pass
                        foreach ( CompositionPass pass in tp.Passes )
                        {
                            if ( pass.Type == CompositorPassType.RenderScene )
                            {
                                renderingScene = true;
                                break;
                            }
                        }
                    }
                }
            }

            if ( renderingScene )
            {
                // Ok, inherit settings from target
                RenderTarget target = _chain.Viewport.Target;
                hwGammaWrite = target.HardwareGammaEnabled;
                fsaa = target.FSAA;
                fsaaHint = target.FSAAHint;
            }
            else
            {
                hwGammaWrite = false;
                fsaa = 0;
                fsaaHint = string.Empty;
            }
        }

        /// <summary>
        /// Notify listeners of a material compilation.
        /// </summary>
        /// <param name="e"></param>
        public void OnMaterialSetup( CompositorInstanceMaterialEventArgs e )
        {
            if ( MaterialSetup != null )
            {
                MaterialSetup( e );
            }
        }

        /// <summary>
        /// Notify listeners of a material render.
        /// </summary>
        /// <param name="e"></param>
        public void OnMaterialRender( CompositorInstanceMaterialEventArgs e )
        {
            if ( MaterialRender != null )
            {
                MaterialRender( e );
            }
        }

        /// <summary>
        /// Notify listeners of a material render.
        /// </summary>
        /// <param name="e"></param>
        public void OnResourceCreated( CompositorInstanceResourceEventArgs e )
        {
            if ( ResourceCreated != null )
            {
                ResourceCreated( e );
            }
        }
    }

    ///<summary>
    ///    Clear framebuffer RenderSystem operation
    ///</summary>
    public class RSClearOperation : CompositorInstance.RenderSystemOperation
    {
        ///<summary>
        ///    Which buffers to clear (FrameBuffer)
        ///</summary>
        protected FrameBufferType buffers;

        ///<summary>
        ///    Color to clear in case FrameBuffer.Color is set
        ///</summary>
        protected ColorEx color;

        ///<summary>
        ///    Depth to set in case FrameBuffer.Depth is set
        ///</summary>
        protected float depth;

        ///<summary>
        ///    Stencil value to set in case FrameBuffer.Stencil is set
        ///</summary>
        protected int stencil;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="buffers"></param>
        /// <param name="color"></param>
        /// <param name="depth"></param>
        /// <param name="stencil"></param>
        public RSClearOperation( FrameBufferType buffers, ColorEx color, float depth, int stencil )
        {
            this.buffers = buffers;
            this.color = color;
            this.depth = depth;
            this.stencil = stencil;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sm"></param>
        /// <param name="rs"></param>
        public override void Execute( SceneManager sm, RenderSystem rs )
        {
            rs.ClearFrameBuffer( buffers, color, depth, stencil );
        }
    }

    ///<summary>
    ///    "Set stencil state" RenderSystem operation
    ///</summary>
    public class RSStencilOperation : CompositorInstance.RenderSystemOperation
    {
        protected bool stencilCheck;
        protected CompareFunction func;
        protected int refValue;
        protected int mask;
        protected StencilOperation stencilFailOp;
        protected StencilOperation depthFailOp;
        protected StencilOperation passOp;
        protected bool twoSidedOperation;

        public RSStencilOperation( bool stencilCheck, CompareFunction func, int refValue, int mask,
                                   StencilOperation stencilFailOp, StencilOperation depthFailOp, StencilOperation passOp,
                                   bool twoSidedOperation )
        {
            this.stencilCheck = stencilCheck;
            this.func = func;
            this.refValue = refValue;
            this.mask = mask;
            this.stencilFailOp = stencilFailOp;
            this.depthFailOp = depthFailOp;
            this.passOp = passOp;
            this.twoSidedOperation = twoSidedOperation;
        }

        public override void Execute( SceneManager sm, RenderSystem rs )
        {
            rs.StencilCheckEnabled = stencilCheck;
            rs.SetStencilBufferParams( func, refValue, mask, stencilFailOp,
                                       depthFailOp, passOp, twoSidedOperation );
        }
    }

    ///<summary>
    ///    "Render quad" RenderSystem operation
    ///</summary>
    public class RSQuadOperation : CompositorInstance.RenderSystemOperation
    {
        /// <summary>
        /// 
        /// </summary>
        public Material Material { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public Technique Technique { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public CompositorInstance Instance { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public uint PassID { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public bool QuadCornerModified { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public bool QuadFarCorners { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public bool QuadFarCornersViewSpace { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public float QuadLeft { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public float QuadTop { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public float QuadRight { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public float QuadBottom { get; set; }

        public RSQuadOperation( CompositorInstance instance, uint pass_id, Material mat )
        {
            Material = mat;
            Instance = instance;
            PassID = pass_id;
            QuadLeft = -1;
            QuadRight = 1;
            QuadTop = 1;
            QuadBottom = -1;

            mat.Load();
            instance.OnMaterialSetup( new CompositorInstanceMaterialEventArgs( PassID, Material ) );
            Technique = mat.GetTechnique( 0 );
            Debug.Assert( Technique != null, "Material has no supported technique, RSQuadOperation.Ctor" );
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="left"></param>
        /// <param name="top"></param>
        /// <param name="right"></param>
        /// <param name="bottom"></param>
        public void SetQuadCorners( float left, float top, float right, float bottom )
        {
            QuadLeft = left;
            QuadTop = top;
            QuadRight = right;
            QuadBottom = bottom;
            QuadCornerModified = true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="farCorners"></param>
        /// <param name="farCornersViewSpace"></param>
        public void SetQuadFarCorners( bool farCorners, bool farCornersViewSpace )
        {
            QuadFarCorners = farCorners;
            QuadFarCornersViewSpace = farCornersViewSpace;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sm"></param>
        /// <param name="rs"></param>
        public override void Execute( SceneManager sm, RenderSystem rs )
        {
            // Fire listener
            Instance.OnMaterialRender( new CompositorInstanceMaterialEventArgs( PassID, Material ) );

            Viewport vp = rs.ActiveViewport;
            Rectangle2D rect = (Rectangle2D)CompositorManager.Instance.TexturedRectangle2D;
            if ( QuadCornerModified )
            {
                // ensure positions are using peculiar render system offsets
                float hOffset = rs.HorizontalTexelOffset / ( 0.5f * vp.ActualWidth );
                float vOffset = rs.VerticalTexelOffset / ( 0.5f * vp.ActualHeight );
                rect.SetCorners( QuadLeft + hOffset, QuadTop - vOffset, QuadRight + hOffset, QuadBottom - vOffset );
            }

            if ( QuadFarCorners )
            {
                Axiom.Math.Vector3[] corners = vp.Camera.WorldSpaceCorners;
                if ( QuadFarCornersViewSpace )
                {
                    Axiom.Math.Matrix4 viewMat = vp.Camera.FrustumViewMatrix;
                    rect.SetNormals( viewMat * corners[ 5 ], viewMat * corners[ 6 ], viewMat * corners[ 4 ], viewMat * corners[ 7 ] );
                }
                else
                {
                    rect.SetNormals( corners[ 5 ], corners[ 6 ], corners[ 4 ], corners[ 7 ] );
                }
            }
            // Queue passes from mat
            for ( int i = 0; i < Technique.PassCount; i++ )
            {
                Pass pass = Technique.GetPass( i );
                sm.InjectRenderWithPass( pass,
                                         CompositorManager.Instance.TexturedRectangle2D,
                                         false // don't allow replacement of shadow passes
                    );
            }
        }
    }

    /// <summary>
    /// "Set material scheme" RenderSystem operation
    /// </summary>
    public class RSSetSchemeOperation : CompositorInstance.RenderSystemOperation
    {
        /// <summary>
        /// 
        /// </summary>
        public string PreviousSchemeName { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public bool PreviousLateResolving { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string SchemeName { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="schemeName"></param>
        public RSSetSchemeOperation( string schemeName )
        {
            SchemeName = schemeName;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sm"></param>
        /// <param name="rs"></param>
        public override void Execute( SceneManager sm, RenderSystem rs )
        {
            MaterialManager matMgr = MaterialManager.Instance;
            PreviousSchemeName = matMgr.ActiveScheme;

            PreviousLateResolving = sm.IsLateMaterialResolving;
            sm.IsLateMaterialResolving = true;
        }
    }

    /// <summary>
    /// Restore the settings changed by the set scheme operation
    /// </summary>
    public class RSRestoreSchemeOperation : CompositorInstance.RenderSystemOperation
    {
        /// <summary>
        /// 
        /// </summary>
        public RSSetSchemeOperation SetOperation { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="setoperation"></param>
        public RSRestoreSchemeOperation( RSSetSchemeOperation setoperation )
        {
            SetOperation = setoperation;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sm"></param>
        /// <param name="rs"></param>
        public override void Execute( SceneManager sm, RenderSystem rs )
        {
            MaterialManager.Instance.ActiveScheme = SetOperation.PreviousSchemeName;
            sm.IsLateMaterialResolving = SetOperation.PreviousLateResolving;
        }
    }
}