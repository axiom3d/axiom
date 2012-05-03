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

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

using Axiom.Core;
using Axiom.Configuration;
using Axiom.Media;

#endregion Namespace Declarations

namespace Axiom.Graphics
{
	/// <summary>
	/// Delegate for handling material events.
	/// </summary>
	public delegate void CompositorInstanceMaterialEventHandler( CompositorInstance source, CompositorInstanceMaterialEventArgs e );

	/// <summary>
	/// Delegate for handling resource events.
	/// </summary>
	public delegate void CompositorInstanceResourceEventHandler( CompositorInstance source, CompositorInstanceResourceEventArgs e );

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

	///<summary>
	///    An instance of a Compositor object for one Viewport. It is part of the CompositorChain
	///    for a Viewport.
	///</summary>
	public class CompositorInstance : DisposableObject
	{
		#region Events

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

		#endregion Events

		///<summary>
		///    A pairing of int and CompositeRenderSystemOperation, needed because the collection
		///    in TargetOperation must be ordered
		///</summary>
		public class QueueIDAndOperation
		{
			#region Fields and Properties

			protected RenderQueueGroupID queueID;

			public RenderQueueGroupID QueueID
			{
				get
				{
					return queueID;
				}
				set
				{
					queueID = value;
				}
			}

			protected CompositeRenderSystemOperation operation;

			public CompositeRenderSystemOperation Operation
			{
				get
				{
					return operation;
				}
				set
				{
					operation = value;
				}
			}

			#endregion Fields and Properties

			#region Constructor

			public QueueIDAndOperation( RenderQueueGroupID queueID, CompositeRenderSystemOperation operation )
			{
				this.queueID = queueID;
				this.operation = operation;
			}

			#endregion Constructor
		}

		#region Fields and Properties

		///<summary>
		///    Compositor of which this is an instance
		///</summary>
		protected Compositor compositor;

		public Compositor Compositor
		{
			get
			{
				return compositor;
			}
			set
			{
				compositor = value;
			}
		}

		///<summary>
		///    Composition technique used by this instance
		///</summary>
		protected CompositionTechnique technique;

		public CompositionTechnique Technique
		{
			get
			{
				return technique;
			}
			set
			{
				technique = value;
			}
		}

		///<summary>
		///    Composition chain of which this instance is part
		///</summary>
		protected CompositorChain chain;

		public CompositorChain Chain
		{
			get
			{
				return chain;
			}
			set
			{
				chain = value;
			}
		}

		///<summary>
		///    Is this instance enabled?
		///</summary>
		protected bool enabled;

		/// <summary>
		/// The compositor instance will only render if it is enabled, otherwise it is pass-through.
		/// </summary>
		public bool IsEnabled
		{
			get
			{
				return enabled;
			}
			set
			{
				if ( enabled != value )
				{
					enabled = value;
					// Create of free resource.
					if ( value )
					{
						CreateResources( false );
					}
					else
					{
						FreeResources( false, true );
					}
				}
				// Notify chain state needs recompile.
				chain.Dirty = true;
			}
		}

		///<summary>
		///    Map from name->local texture
		///</summary>
		protected Dictionary<string, Texture> localTextures;

		public Dictionary<string, Texture> LocalTextures
		{
			get
			{
				return localTextures;
			}
		}

		/// <summary>
		/// Store a list of MRTs we've created
		/// </summary>
		private Dictionary<string, MultiRenderTarget> localMrts = new Dictionary<string, MultiRenderTarget>();

		/// <summary>
		/// Textures that are not currently in use, but that we want to keep for now,
		/// for example if we switch techniques but want to keep all textures available
		/// in case we switch back.
		/// </summary>
		private Dictionary<CompositionTechnique.TextureDefinition, Texture> reservedTextures = new Dictionary<CompositionTechnique.TextureDefinition, Texture>();

		///<summary>
		///    Render System operations queued by last compile, these are created by this
		///    instance thus managed and deleted by it. The list is cleared with
		///    clearCompilationState()
		///</summary>
		protected List<QueueIDAndOperation> renderSystemOperations;

		public List<QueueIDAndOperation> RenderSystemOperations
		{
			get
			{
				return renderSystemOperations;
			}
			set
			{
				renderSystemOperations = value;
			}
		}

		///<summary>
		///    Previous instance (set by chain)
		///</summary>
		protected CompositorInstance previousInstance;

		public CompositorInstance PreviousInstance
		{
			get
			{
				return previousInstance;
			}
			set
			{
				previousInstance = value;
			}
		}

		protected static int materialDummyCounter = 0;

		protected static int resourceDummyCounter = 0;

		#endregion Fields and Properties

		#region Constructor

		public CompositorInstance( CompositionTechnique technique, CompositorChain chain )
		{
			this.compositor = technique.Parent;
			this.technique = technique;
			this.chain = chain;
			this.enabled = false;

			var logicName = technique.CompositorLogicName;
			if ( !String.IsNullOrEmpty( logicName ) )
			{
				CompositorManager.Instance.CompositorLogics[ logicName ].CompositorInstanceCreated( this );
			}

			localTextures = new Dictionary<string, Texture>();
			renderSystemOperations = new List<QueueIDAndOperation>();
		}

		#endregion Constructor

		#region Methods

		///<summary>
		///    Collect rendering passes. Here, passes are converted into render target operations
		///    and queued with queueRenderSystemOp.
		///</summary>
		protected void CollectPasses( CompositeTargetOperation finalState, CompositionTargetPass target )
		{
			// Here, passes are converted into render target operations
			Pass targetPass = null;
			Technique srctech = null;
			Material mat = null, srcmat = null;

			foreach ( var pass in target.Passes )
			{
				switch ( pass.Type )
				{
					case CompositorPassType.Clear:
					{
						QueueRenderSystemOp( finalState, new RSClearOperation( pass.ClearBuffers, pass.ClearColor, pass.ClearDepth, pass.ClearStencil ) );
					}
						break;
					case CompositorPassType.Stencil:
					{
						QueueRenderSystemOp( finalState, new RSStencilOperation( pass.StencilCheck, pass.StencilFunc, pass.StencilRefValue, pass.StencilMask, pass.StencilFailOp, pass.StencilDepthFailOp, pass.StencilPassOp, pass.StencilTwoSidedOperation ) );
					}
						break;
					case CompositorPassType.RenderScene:
					{
						if ( pass.FirstRenderQueue < finalState.CurrentQueueGroupId )
						{
							// Mismatch -- warn user
							// XXX We could support repeating the last queue, with some effort
							LogManager.Instance.Write( "Warning in compilation of Compositor {0}: Attempt to render queue {1} before {2}.", compositor.Name, pass.FirstRenderQueue, finalState.CurrentQueueGroupId );
						}
						RSSetSchemeOperation setSchemeOperation = null;
						if ( pass.MaterialScheme != string.Empty )
						{
							//Add the triggers that will set the scheme and restore it each frame
							finalState.CurrentQueueGroupId = pass.FirstRenderQueue;
							setSchemeOperation = new RSSetSchemeOperation( pass.MaterialScheme );
							QueueRenderSystemOp( finalState, setSchemeOperation );
						}
						// Add render queues
						for ( var x = (int)pass.FirstRenderQueue; x < (int)pass.LastRenderQueue; x++ )
						{
							Debug.Assert( x >= 0 );
							finalState.RenderQueues.Set( x, true );
						}
						finalState.CurrentQueueGroupId = pass.LastRenderQueue + 1;
						if ( setSchemeOperation != null )
						{
							//Restoring the scheme after the queues have been rendered
							QueueRenderSystemOp( finalState, new RSRestoreSchemeOperation( setSchemeOperation ) );
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
							LogManager.Instance.Write( "Warning in compilation of Compositor {0}: No material defined for composition pass.", compositor.Name );
							break;
						}
						srcmat.Load();
						if ( srcmat.SupportedTechniques.Count == 0 )
						{
							// No supported techniques -- warn user
							LogManager.Instance.Write( "Warning in compilation of Compositor {0}: material {1} has no supported techniques.", compositor.Name, srcmat.Name );
							break;
						}

						srctech = srcmat.GetBestTechnique( 0 );
						// Create local material
						mat = CreateLocalMaterial( srcmat.Name );
						// Copy and adapt passes from source material
						for ( var i = 0; i < srctech.PassCount; i++ )
						{
							var srcpass = srctech.GetPass( i );
							// Create new target pass
							targetPass = mat.GetTechnique( 0 ).CreatePass();
							srcpass.CopyTo( targetPass );
							// Set up inputs
							for ( var x = 0; x < pass.InputsCount; x++ )
							{
								var inp = pass.GetInput( x );
								if ( !string.IsNullOrEmpty( inp.Name ) )
								{
									if ( x < targetPass.TextureUnitStatesCount )
									{
										targetPass.GetTextureUnitState( x ).SetTextureName( this.GetTextureInstance( inp.Name, inp.MrtIndex ).Name );
									}
									else
									{
										// Texture unit not there
										LogManager.Instance.Write( "Warning in compilation of Compositor {0}: material {1} texture unit {2} out of bounds.", compositor.Name, srcmat.Name, x );
									}
								}
							} //end for inputs.length
						} //end for passcount

						var rsQuadOperation = new RSQuadOperation( this, pass.Identifier, mat );
						float left, top, right, bottom;
						if ( pass.GetQuadCorners( out left, out top, out right, out bottom ) )
						{
							rsQuadOperation.SetQuadCorners( left, top, right, bottom );
						}
						rsQuadOperation.SetQuadFarCorners( pass.QuadFarCorners, pass.QuadFarCornersViewSpace );
						QueueRenderSystemOp( finalState, rsQuadOperation );
					}
						break;
					case CompositorPassType.RenderCustom:
					{
						var customOperation = CompositorManager.Instance.CustomCompositionPasses[ pass.CustomType ].CreateOperation( this, pass );
						QueueRenderSystemOp( finalState, customOperation );
					}
						break;
				}
			}
		}

		///<summary>
		///    Recursively collect target states (except for final Pass).
		///</summary>
		///<param name="compiledState">This vector will contain a list of TargetOperation objects</param>
		public void CompileTargetOperations( List<CompositeTargetOperation> compiledState )
		{
			// Collect targets of previous state
			if ( previousInstance != null )
			{
				previousInstance.CompileTargetOperations( compiledState );
			}
			// Texture targets
			foreach ( var target in technique.TargetPasses )
			{
				var ts = new CompositeTargetOperation( GetTextureInstance( target.OutputName ).GetBuffer().GetRenderTarget() );
				// Set "only initial" flag, visibilityMask and lodBias according to CompositionTargetPass.
				ts.OnlyInitial = target.OnlyInitial;
				ts.VisibilityMask = target.VisibilityMask;
				ts.LodBias = target.LodBias;
				ts.ShadowsEnabled = target.ShadowsEnabled;
				// Check for input mode previous
				if ( target.InputMode == CompositorInputMode.Previous && previousInstance != null )
				{
					// Collect target state for previous compositor
					// The TargetOperation for the final target is collected seperately as it is merged
					// with later operations
					previousInstance.CompileOutputOperation( ts );
				}
				// Collect passes of our own target
				CollectPasses( ts, target );
				compiledState.Add( ts );
			}
		}

		///<summary>
		///    Compile the final (output) operation. This is done seperately because this
		///    is combined with the input in chained filters.
		///</summary>
		public void CompileOutputOperation( CompositeTargetOperation finalState )
		{
			// Final target
			var tpass = technique.OutputTarget;

			// Logical-and together the visibilityMask, and multiply the lodBias
			finalState.VisibilityMask &= tpass.VisibilityMask;
			finalState.LodBias *= tpass.LodBias;

			if ( tpass.InputMode == CompositorInputMode.Previous )
			{
				// Collect target state for previous compositor
				// The TargetOperation for the final target is collected seperately as it is merged
				// with later operations
				previousInstance.CompileOutputOperation( finalState );
			}
			// Collect passes
			CollectPasses( finalState, tpass );
		}

		///<summary>
		///    Get the instance for a local texture.
		///</summary>
		///<remarks>
		///    It is only valid to call this when local textures have been loaded,
		///    which in practice means that the compositor instance is active. Calling
		///    it at other times will cause an exception. Note that since textures
		///    are cleaned up aggressively, this name is not guaranteed to stay the
		///    same if you disable and renable the compositor instance.
		///</remarks>
		///<param name="name">The name of the texture in the original compositor definition</param>
		public Texture GetTextureInstance( string name )
		{
			return GetTextureInstance( name, 0 );
		}

		///<summary>
		///    Get the instance for a local texture.
		///</summary>
		///<remarks>
		///    It is only valid to call this when local textures have been loaded,
		///    which in practice means that the compositor instance is active. Calling
		///    it at other times will cause an exception. Note that since textures
		///    are cleaned up aggressively, this name is not guaranteed to stay the
		///    same if you disable and renable the compositor instance.
		///</remarks>
		///<param name="name">The name of the texture in the original compositor definition</param>
		/// <param name="mrtIndex">If name identifies a MRT, which texture attachment to retrieve</param>
		///<returns>The instance for the texture, corresponds to a real texture</returns>
		public Texture GetTextureInstance( string name, int mrtIndex )
		{
			Texture texture;
			if ( localTextures.TryGetValue( name, out texture ) )
			{
				return texture;
			}
			if ( localTextures.TryGetValue( this.GetMrtTextureLocalName( name, mrtIndex ), out texture ) )
			{
				return texture;
			}

			return null;
		}

		///<summary>
		///    Create a local dummy material with one technique but no passes.
		///    The material is detached from the Material Manager to make sure it is destroyed
		///    when going out of scope.
		///</summary>
		protected Material CreateLocalMaterial( string name )
		{
			var mat = (Material)MaterialManager.Instance.Create( "CompositorInstanceMaterial" + materialDummyCounter++ + "/" + name, ResourceGroupManager.InternalResourceGroupName );

			MaterialManager.Instance.Remove( mat.Name );
			mat.GetTechnique( 0 ).RemoveAllPasses();
			return mat;
		}

		///<summary>
		///    Create local rendertextures and other resources. Builds mLocalTextures.
		///</summary>
		/// <param name="forResizeOnly"></param>
		public void CreateResources( bool forResizeOnly )
		{
			// Create temporary textures
			// In principle, temporary textures could be shared between multiple viewports
			// (CompositorChains). This will save a lot of memory in case more viewports
			// are composited.
			var assignedTextures = new List<Texture>();
			foreach ( var def in technique.TextureDefinitions )
			{
				//This is a reference, isn't created in this compositor
				if ( !string.IsNullOrEmpty( def.ReferenceCompositorName ) )
				{
					continue;
				}
				//This is a global texture, just link the created resources from the parent
				if ( def.Scope == CompositionTechnique.TextureScope.Global )
				{
					var parentComp = technique.Parent;
					if ( def.PixelFormats.Count > 1 )
					{
						var atch = 0;
						foreach ( var p in def.PixelFormats )
						{
							var tex = parentComp.GetTextureInstance( def.Name, atch++ );
							localTextures.Add( GetMrtTextureLocalName( def.Name, atch ), tex );
						}
						var mrt = (MultiRenderTarget)parentComp.GetRenderTarget( def.Name );
						localMrts.Add( def.Name, mrt );
					}
					else
					{
						var tex = parentComp.GetTextureInstance( def.Name, 0 );
						localTextures.Add( def.Name, tex );
					}
					continue;
				}
				// Determine width and height
				var width = def.Width;
				var height = def.Height;
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
					width = (int)( chain.Viewport.ActualWidth * def.WidthFactor );
				}
				if ( height == 0 )
				{
					height = (int)( chain.Viewport.ActualHeight * def.HeightFactor );
				}

				// determine options as a combination of selected options and possible options
				if ( !def.Fsaa )
				{
					fsaa = 0;
					fsaahint = string.Empty;
				}
				hwGamma = hwGamma || def.HwGammaWrite;

				// Make the tetxure
				RenderTarget rendTarget;
				if ( def.PixelFormats.Count > 1 )
				{
					var mrtBaseName = "c" + resourceDummyCounter++ + "/" + def.Name + "/" + chain.Viewport.Target.Name;
					var mrt = Root.Instance.RenderSystem.CreateMultiRenderTarget( mrtBaseName );
					localMrts.Add( mrtBaseName, mrt );

					// create and bind individual surfaces
					var atch = 0;
					foreach ( var p in def.PixelFormats )
					{
						var texName = mrtBaseName + "/" + atch;
						var mrtLocalName = GetMrtTextureLocalName( def.Name, atch );
						Texture tex;
						if ( def.Pooled )
						{
							// get / create pooled texture
							tex = CompositorManager.Instance.GetPooledTexture( texName, mrtLocalName, width, height, p, fsaa, fsaahint, hwGamma && !PixelUtil.IsFloatingPoint( p ), assignedTextures, this, def.Scope );
						}
						else
						{
							tex = TextureManager.Instance.CreateManual( texName, ResourceGroupManager.InternalResourceGroupName, TextureType.TwoD, width, height, 0, p, TextureUsage.RenderTarget, null, hwGamma && !PixelUtil.IsFloatingPoint( p ), fsaa, fsaahint );
						}

						var rt = tex.GetBuffer().GetRenderTarget();
						rt.IsAutoUpdated = false;
						mrt.BindSurface( atch++, rt );
						// Also add to local textures so we can look up
						localTextures.Add( mrtLocalName, tex );
					}

					rendTarget = mrt;
				}
				else
				{
					var texName = "c" + resourceDummyCounter++ + "/" + def.Name + "/" + chain.Viewport.Target.Name;
					// spaces in the name can cause plugin problems.
					// this is an auto generated name - so no spaces can't hurt us.
					texName = texName.Replace( ' ', '_' );

					Texture tex;
					if ( def.Pooled )
					{
						// get / create pooled texture
						tex = CompositorManager.Instance.GetPooledTexture( texName, def.Name, width, height, def.PixelFormats[ 0 ], fsaa, fsaahint, hwGamma && !PixelUtil.IsFloatingPoint( def.PixelFormats[ 0 ] ), assignedTextures, this, def.Scope );
					}
					else
					{
						tex = TextureManager.Instance.CreateManual( texName, ResourceGroupManager.InternalResourceGroupName, TextureType.TwoD, width, height, 0, def.PixelFormats[ 0 ], TextureUsage.RenderTarget, null, hwGamma && !PixelUtil.IsFloatingPoint( def.PixelFormats[ 0 ] ), fsaa, fsaahint );
					}

					rendTarget = tex.GetBuffer().GetRenderTarget();
					localTextures.Add( def.Name, tex );
				}

				//Set DepthBuffer pool for sharing
				rendTarget.DepthBufferPool = def.DepthBufferId;

				// Set up viewport over entire texture
				rendTarget.IsAutoUpdated = false;

				// We may be sharing / reusing this texture, so test before adding viewport
				if ( rendTarget.NumViewports == 0 )
				{
					var camera = chain.Viewport.Camera;
					// Save last viewport and current aspect ratio
					var oldViewport = camera.Viewport;
					var aspectRatio = camera.AspectRatio;

					var v = rendTarget.AddViewport( camera );
					v.SetClearEveryFrame( false );
					v.ShowOverlays = false;
					v.BackgroundColor = new ColorEx( 0, 0, 0, 0 );
					// Should restore aspect ratio, in case of auto aspect ratio
					// enabled, it'll changed when add new viewport.
					camera.AspectRatio = aspectRatio;
					// Should restore last viewport, i.e. never disturb user code
					// which might based on that.
					camera.NotifyViewport( oldViewport );
				}
			}

			OnResourceCreated( new CompositorInstanceResourceEventArgs( forResizeOnly ) );
		}

		///<summary>
		///    Destroy local rendertextures and other resources.
		///</summary>
		public void FreeResources( bool forResizeOnly, bool clearReservedTextures )
		{
			// Remove temporary textures
			// We only remove those that are not shared, shared textures are dealt with
			// based on their reference count.
			// We can also only free textures which are derived from the target size, if
			// required (saves some time & memory thrashing / fragmentation on resize)
			var assignedTextures = new List<Texture>();
			foreach ( var def in technique.TextureDefinitions )
			{
				if ( !string.IsNullOrEmpty( def.ReferenceCompositorName ) )
				{
					//This is a reference, isn't created here
					continue;
				}
				// potentially only remove this one if based on size
				if ( !forResizeOnly || def.Width == 0 | def.Height == 0 )
				{
					var subSurfaceCount = def.PixelFormats.Count;
					// Potentially many surfaces
					for ( var subSurface = 0; subSurface < subSurfaceCount; subSurface++ )
					{
						var texName = subSurfaceCount > 1 ? GetMrtTextureLocalName( def.Name, subSurface ) : def.Name;
						Texture tex = null;
						if ( localTextures.TryGetValue( texName, out tex ) )
						{
							if ( !def.Pooled && def.Scope != CompositionTechnique.TextureScope.Global )
							{
								// remove myself from central only if not pooled and not global
								TextureManager.Instance.Remove( tex.Name );
							}
							localTextures.Remove( texName );
						}
					}
					if ( subSurfaceCount > 1 )
					{
						MultiRenderTarget i = null;
						if ( localMrts.TryGetValue( def.Name, out i ) )
						{
							if ( def.Scope != CompositionTechnique.TextureScope.Global )
							{
								// remove MRT if not global
								Root.Instance.RenderSystem.DestroyRenderTarget( i.Name );
							}
							localMrts.Remove( def.Name );
						}
					}
				}
			}

			if ( clearReservedTextures )
			{
				if ( forResizeOnly )
				{
					var toDelete = new List<CompositionTechnique.TextureDefinition>();
					foreach ( var def in reservedTextures.Keys )
					{
						if ( def.Width == 0 || def.Height == 0 )
						{
							toDelete.Add( def );
						}
					}
					// just remove the ones which would be affected by a resize
					for ( var i = 0; i < toDelete.Count; i++ )
					{
						this.reservedTextures.Remove( toDelete[ i ] );
					}
					toDelete = null;
				}
				else
				{
					// clear all
					reservedTextures.Clear();
				}
			}
			// Now we tell the central list of textures to check if its unreferenced,
			// and to remove if necessary. Anything shared that was left in the reserve textures
			// will not be released here
			CompositorManager.Instance.FreePooledTextures( true );
		}

		private String GetMrtTextureLocalName( String baseName, int attachment )
		{
			return baseName + "/" + attachment;
		}

		///<summary>
		///    Queue a render system operation.
		///</summary>
		///<returns>destination pass</returns>
		protected void QueueRenderSystemOp( CompositeTargetOperation finalState, CompositeRenderSystemOperation op )
		{
			// Store operation for current QueueGroup ID
			finalState.RenderSystemOperations.Add( new QueueIDAndOperation( finalState.CurrentQueueGroupId, op ) );
			// Save a pointer, so that it will be freed on recompile
			chain.RenderSystemOperations.Add( op );
		}

		/// <summary>
		/// Search for options like AA and hardware gamma which we may want to
		/// inherit from the main render target to which we're attached.
		/// </summary>
		/// <param name="texname"></param>
		/// <param name="hwGammaWrite"></param>
		/// <param name="fsaa"></param>
		/// <param name="fsaaHint"></param>
		private void DeriveTextureRenderTargetOptions( string texname, out bool hwGammaWrite, out int fsaa, out string fsaaHint )
		{
			// search for passes on this texture def that either include a render_scene
			// or use input previous
			var renderingScene = false;
			foreach ( var tp in technique.TargetPasses )
			{
				if ( tp.OutputName == texname )
				{
					if ( tp.InputMode == CompositorInputMode.Previous )
					{
						// this may be rendering the scene implicitly
						// Can't check _previousInstance against _Chain.OriginalSceneCompositor
						// at this time, so check the position
						renderingScene = true;
						foreach ( var inst in chain.Instances )
						{
							if ( inst == this )
							{
								break;
							}
							else if ( inst.IsEnabled )
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
						foreach ( var pass in tp.Passes )
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
				var target = chain.Viewport.Target;
				hwGammaWrite = target.IsHardwareGammaEnabled;
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

		///<summary>
		///    Notify listeners of a material compilation.
		///</summary>
		public void OnMaterialSetup( CompositorInstanceMaterialEventArgs args )
		{
			if ( MaterialSetup != null )
			{
				MaterialSetup( this, args );
			}
		}

		///<summary>
		///    Notify listeners of a material render.
		///</summary>
		public void OnMaterialRender( CompositorInstanceMaterialEventArgs args )
		{
			if ( MaterialRender != null )
			{
				MaterialRender( this, args );
			}
		}

		/// <summary>
		/// Notify listeners of a material render.
		/// </summary>
		public void OnResourceCreated( CompositorInstanceResourceEventArgs args )
		{
			if ( ResourceCreated != null )
			{
				ResourceCreated( this, args );
			}
		}

		#endregion Methods

		#region IDisposable Implementation

		protected override void dispose( bool disposeManagedResources )
		{
			if ( !IsDisposed )
			{
				if ( disposeManagedResources )
				{
					// Dispose managed resources.
					localTextures.Clear();
					localMrts.Clear();
				}

				// There are no unmanaged resources to release, but
				// if we add them, they need to be released here.
			}

			// If it is available, make the call to the
			// base class's Dispose(Boolean) method
			base.dispose( disposeManagedResources );
		}

		#endregion IDisposable Implementation
	}

	///<summary>
	///    Operation setup for a RenderTarget (collected).
	///</summary>
	public class CompositeTargetOperation
	{
		#region Fields and Properties

		///<summary>
		///    Target
		///</summary>
		public RenderTarget Target { get; set; }

		///<summary>
		///    Current group ID
		///</summary>
		public RenderQueueGroupID CurrentQueueGroupId { get; set; }

		///<summary>
		///    RenderSystem operations to queue into the scene manager
		///</summary>
		public List<CompositorInstance.QueueIDAndOperation> RenderSystemOperations { get; protected set; }

		///<summary>
		///    Scene visibility mask
		///    If this is 0, the scene is not rendered at all
		///</summary>
		public ulong VisibilityMask { get; set; }

		///<summary>
		///    LOD offset. This is multiplied with the camera LOD offset
		///    1.0 is default, lower means lower detail, higher means higher detail
		///</summary>
		public float LodBias { get; set; }

		///<summary>
		///    A set of render queues to either include or exclude certain render queues.
		///</summary>
		public BitArray RenderQueues { get; set; }

		///<summary>
		/// <see>CompositionTargetPass.OnlyInitial</see>
		///</summary>
		public bool OnlyInitial { get; set; }

		///<summary>
		///    "Has been rendered" flag; used in combination with
		///    onlyInitial to determine whether to skip this target operation.
		///</summary>
		public bool HasBeenRendered { get; set; }

		///<summary>
		///    Whether this op needs to find visible scene objects or not
		///</summary>
		public bool FindVisibleObjects { get; set; }

		///<summary>
		///    Which material scheme this op will use */
		///</summary>
		public string MaterialScheme { get; set; }

		/// <summary>
		/// Whether shadows will be enabled or not
		/// </summary>
		public bool ShadowsEnabled { get; set; }

		#endregion Fields and Properties

		#region Constructors

		public CompositeTargetOperation( RenderTarget target )
		{
			this.RenderQueues = new BitArray( (int)RenderQueueGroupID.Count );
			this.Target = target;
			this.CurrentQueueGroupId = 0;
			this.RenderSystemOperations = new List<CompositorInstance.QueueIDAndOperation>();
			this.VisibilityMask = 0xFFFFFFFF;
			this.LodBias = 1.0f;
			this.OnlyInitial = false;
			this.HasBeenRendered = false;
			this.FindVisibleObjects = false;
			// This fixes an issue, but seems to be wrong for some reason.
			this.MaterialScheme = string.Empty;
			this.ShadowsEnabled = true;
		}

		#endregion Constructors
	}

	///<summary>
	///    Base class for other render system operations
	///</summary>
	public abstract class CompositeRenderSystemOperation
	{
		/// Set state to SceneManager and RenderSystem
		public abstract void Execute( SceneManager sm, RenderSystem rs );
	}

	///<summary>
	///    Clear framebuffer RenderSystem operation
	///</summary>
	public class RSClearOperation : CompositeRenderSystemOperation
	{
		#region Fields

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

		#endregion Fields

		#region Constructor

		public RSClearOperation( FrameBufferType buffers, ColorEx color, float depth, int stencil )
		{
			this.buffers = buffers;
			this.color = color;
			this.depth = depth;
			this.stencil = stencil;
		}

		#endregion Constructor

		#region CompositorInstance.CompositeRenderSystemOperation Implementation

		public override void Execute( SceneManager sm, RenderSystem rs )
		{
			rs.ClearFrameBuffer( buffers, color, depth, (ushort)stencil );
		}

		#endregion CompositorInstance.CompositeRenderSystemOperation Implementation
	}

	///<summary>
	///    "Set stencil state" RenderSystem operation
	///</summary>
	public class RSStencilOperation : CompositeRenderSystemOperation
	{
		#region Fields

		protected bool stencilCheck;
		protected CompareFunction func;
		protected int refValue;
		protected int mask;
		protected StencilOperation stencilFailOp;
		protected StencilOperation depthFailOp;
		protected StencilOperation passOp;
		protected bool twoSidedOperation;

		#endregion Fields

		#region Constructor

		public RSStencilOperation( bool stencilCheck, CompareFunction func, int refValue, int mask, StencilOperation stencilFailOp, StencilOperation depthFailOp, StencilOperation passOp, bool twoSidedOperation )
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

		#endregion Constructor

		#region CompositorInstance.CompositeRenderSystemOperation Implementation

		public override void Execute( SceneManager sm, RenderSystem rs )
		{
			rs.StencilCheckEnabled = stencilCheck;
			rs.SetStencilBufferParams( func, refValue, mask, stencilFailOp, depthFailOp, passOp, twoSidedOperation );
		}

		#endregion CompositorInstance.CompositeRenderSystemOperation Implementation
	}

	///<summary>
	///    "Render quad" RenderSystem operation
	///</summary>
	public class RSQuadOperation : CompositeRenderSystemOperation
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
		public uint PassId { get; set; }

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
			this.PassId = pass_id;
			QuadLeft = -1;
			QuadRight = 1;
			QuadTop = 1;
			QuadBottom = -1;

			mat.Load();
			instance.OnMaterialSetup( new CompositorInstanceMaterialEventArgs( this.PassId, Material ) );
			Technique = mat.GetTechnique( 0 );
			Debug.Assert( Technique != null, "Material has no supported technique." );
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
			Instance.OnMaterialRender( new CompositorInstanceMaterialEventArgs( this.PassId, Material ) );

			var vp = rs.Viewport;
			var rect = (Rectangle2D)CompositorManager.Instance.TexturedRectangle2D;
			if ( QuadCornerModified )
			{
				// ensure positions are using peculiar render system offsets
				float hOffset = rs.HorizontalTexelOffset / ( 0.5f * vp.ActualWidth );
				float vOffset = rs.VerticalTexelOffset / ( 0.5f * vp.ActualHeight );
				rect.SetCorners( QuadLeft + hOffset, QuadTop - vOffset, QuadRight + hOffset, QuadBottom - vOffset );
			}

			if ( QuadFarCorners )
			{
				var corners = vp.Camera.WorldSpaceCorners;
				if ( QuadFarCornersViewSpace )
				{
					var viewMat = vp.Camera.FrustumViewMatrix;
					rect.SetNormals( viewMat * corners[ 5 ], viewMat * corners[ 6 ], viewMat * corners[ 4 ], viewMat * corners[ 7 ] );
				}
				else
				{
					rect.SetNormals( corners[ 5 ], corners[ 6 ], corners[ 4 ], corners[ 7 ] );
				}
			}
			// Queue passes from mat
			for ( var i = 0; i < Technique.PassCount; i++ )
			{
				var pass = Technique.GetPass( i );
				sm.InjectRenderWithPass( pass, CompositorManager.Instance.TexturedRectangle2D, false /*don't allow replacement of shadow passes*/ );
			}
		}
	}

	/// <summary>
	/// "Set material scheme" RenderSystem operation
	/// </summary>
	public class RSSetSchemeOperation : CompositeRenderSystemOperation
	{
		#region Fields and Properties

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

		#endregion Fields and Properties

		#region Construction and Destruction

		/// <summary>
		///
		/// </summary>
		/// <param name="schemeName"></param>
		public RSSetSchemeOperation( string schemeName )
		{
			SchemeName = schemeName;
		}

		#endregion Construction and Destruction

		#region CompositeRenderSystemOperation Implementation

		/// <summary>
		///
		/// </summary>
		/// <param name="sm"></param>
		/// <param name="rs"></param>
		public override void Execute( SceneManager sm, RenderSystem rs )
		{
			var matMgr = MaterialManager.Instance;
			PreviousSchemeName = matMgr.ActiveScheme;

			PreviousLateResolving = sm.IsLateMaterialResolving;
			sm.IsLateMaterialResolving = true;
		}

		#endregion CompositeRenderSystemOperation Implementation
	}

	/// <summary>
	/// Restore the settings changed by the set scheme operation
	/// </summary>
	public class RSRestoreSchemeOperation : CompositeRenderSystemOperation
	{
		#region Fields and Properties

		/// <summary>
		///
		/// </summary>
		public RSSetSchemeOperation SetOperation { get; set; }

		#endregion Fields and Properties

		#region Constructiona and Destruction

		/// <summary>
		///
		/// </summary>
		/// <param name="setoperation"></param>
		public RSRestoreSchemeOperation( RSSetSchemeOperation setoperation )
		{
			SetOperation = setoperation;
		}

		#endregion Constructiona and Destruction

		#region CompositeRenderSystemOperation Implementation

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

		#endregion CompositeRenderSystemOperation Implementation
	}
}
