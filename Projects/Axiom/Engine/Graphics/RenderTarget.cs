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
#endregion

#region SVN Version Information
// <file>
//     <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections;
using System.Diagnostics;
using System.IO;

using Axiom.Core;
using Axiom.Collections;
using Axiom.Media;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Axiom.Core.Collections;

#endregion Namespace Declarations

namespace Axiom.Graphics
{

	#region Delegate/EventArg Declarations

	/// <summary>
	///    Delegate for RenderTarget update events.
	/// </summary>
	public delegate void RenderTargetEventHandler( RenderTargetEventArgs e );

	/// <summary>
	///    Delegate for Viewport update events.
	/// </summary>
	public delegate void RenderTargetViewportEventHandler( RenderTargetViewportEventArgs e );

	/// <summary>
	///    Event arguments for render target updates.
	/// </summary>
	public class RenderTargetEventArgs : EventArgs
	{
		internal RenderTarget source;

		public RenderTarget Source
		{
			get
			{
				return source;
			}
		}

		public RenderTargetEventArgs( RenderTarget source )
		{
			this.source = source;
		}

	}

	/// <summary>
	///    Event arguments for viewport updates while processing a RenderTarget.
	/// </summary>
	public class RenderTargetViewportEventArgs : RenderTargetEventArgs
	{
		internal Viewport viewport;

		public Viewport Viewport
		{
			get
			{
				return viewport;
			}
		}

		public RenderTargetViewportEventArgs( RenderTarget source, Viewport viewport )
			: base( source )
		{
			this.viewport = viewport;
		}

	}

	#endregion Delegate/EventArg Declarations

	/// <summary>
	///		A 'canvas' which can receive the results of a rendering operation.
	/// </summary>
	/// <remarks>
	///		This abstract class defines a common root to all targets of rendering operations. A
	///		render target could be a window on a screen, or another
	///		offscreen surface like a render texture.
	///	</remarks>
	public abstract class RenderTarget : DisposableObject
	{
		#region Enumerations and Structures

		[Flags()]
		public enum Stats
		{
			None = 0,
			FramesPreSecond = 1,
			AverageFPS = 2,
			BestFPS = 4,
			WorstFPS = 8,
			TriangleCount = 16,
			All = 0xFFFF
		};

		/// <summary>
		/// Holds all the current statistics for a RenderTarget
		/// </summary>
		public struct FrameStatistics
		{
			/// <summary>
			/// The number of Frames per second.
			/// </summary>
			public float LastFPS;
			/// <summary>
			/// The average number of Frames per second since Root.StartRendering was called.
			/// </summary>
			public float AverageFPS;
			/// <summary>
			/// The highest number of Frames per second since Root.StartRendering was called.
			/// </summary>
			public float BestFPS;
			/// <summary>
			/// The lowest number of Frames per second since Root.StartRendering was called.
			/// </summary>
			public float WorstFPS;
			/// <summary>
			/// The best frame time recorded since Root.StartRendering was called.
			/// </summary>
			public float BestFrameTime;
			/// <summary>
			/// The worst frame time recorded since Root.StartRendering was called.
			/// </summary>
			public float WorstFrameTime;
			/// <summary>
			/// The number of triangles processed in the last call to Update()
			/// </summary>
			public float TriangleCount;
			/// <summary>
			/// The number of batches procecssed in the last call to Update()
			/// </summary>
			public float BatchCount;
		};

		public enum FrameBuffer
		{
			Front,
			Back,
			Auto
		};

		#endregion Enumerations

		#region Fields and Properties

		private ITimer _timer = Root.Instance.Timer;

		private FrameStatistics _statistics;
		private long _lastTime;
		private long _lastSecond;
		private long _frameCount;

		#region DepthBufferPool Property
		private ushort _poolID;
		public ushort DepthBufferPool
		{
			get
			{
				return _poolID;
			}
			set
			{
				_poolID = value;
			}
		}
		#endregion DepthBufferPool Property

		#region Height Property

		/// <summary>
		///    Height of this render target.
		/// </summary>
		private int _height;
		/// <summary>
		/// Gets/Sets the height of this render target.
		/// </summary>
		public virtual int Height
		{
			get
			{
				return _height;
			}
			protected set
			{
				_height = value;
			}
		}

		#endregion Height Property

		#region Width Property

		/// <summary>
		///    Width of this render target.
		/// </summary>
		private int _width;
		/// <summary>
		/// Gets/Sets the width of this render target.
		/// </summary>
		public virtual int Width
		{
			get
			{
				return _width;
			}
			protected set
			{
				_width = value;
			}
		}

		#endregion Width Property

		#region ColorDepth Property

		/// <summary>
		///     Color depth of this render target.
		/// </summary>
		private int _colorDepth;
		/// <summary>
		/// Gets/Sets the color depth of this render target.
		/// </summary>
		public virtual int ColorDepth
		{
			get
			{
				return _colorDepth;
			}
			protected set
			{
				_colorDepth = value;
			}
		}

		#endregion ColorDepth Property

		#region Priority Property

		/// <summary>
		///    Indicates the priority of this render target.  Higher priority targets will get processed first.
		/// </summary>
		private RenderTargetPriority _priority;
		/// <summary>
		///    Gets/Sets the priority of this render target.  Higher priority targets will get processed first.
		/// </summary>
		public virtual RenderTargetPriority Priority
		{
			get
			{
				return _priority;
			}
			set
			{
				_priority = value;
			}
		}

		#endregion Priority Property

		#region Name Property

		/// <summary>
		///    Unique name assigned to this render target.
		/// </summary>
		private string _name;
		/// <summary>
		///    Gets/Sets the name of this render target.
		/// </summary>
		public virtual string Name
		{
			get
			{
				return _name;
			}
			protected set
			{
				_name = value;
			}
		}


		#endregion Name Property

		#region RequiresTextureFlipping Property

		/// <summary>
		///     Signals whether textures should be flipping before this target
		///     is updated.  Required for render textures in some API's.
		/// </summary>
		public virtual bool RequiresTextureFlipping
		{
			get
			{
				return false;
			}
		}

		#endregion RequiresTextureFlipping Property

		#region IsActive Property

		/// <summary>
		///    Flag that states whether this target is active or not.
		/// </summary>
		private bool _isActive = true;
		/// <summary>
		///    Gets/Sets whether this RenderTarget is active or not.  When inactive, it will be skipped
		///    during processing each frame.
		/// </summary>
		public virtual bool IsActive
		{
			get
			{
				return _isActive && !IsDisposed;
			}
			set
			{
				_isActive = value;
			}
		}

		#endregion IsActive Property

		#region IsAutoUpdated Property

		/// <summary>
		///     Is this render target updated automatically each frame?
		/// </summary>
		private bool _isAutoUpdated = true;
		/// <summary>
		///    Gets/Sets whether this target should be automatically updated if Axiom's rendering
		///    loop or Root.UpdateAllRenderTargets is being used.
		/// </summary>
		/// <remarks>
		///		By default, if you use Axiom's own rendering loop (Root.StartRendering)
		///		or call Root.UpdateAllRenderTargets, all render targets are updated
		///		automatically. This method allows you to control that behaviour, if 
		///		for example you have a render target which you only want to update periodically.
		/// </remarks>
		public virtual bool IsAutoUpdated
		{
			get
			{
				return _isAutoUpdated;
			}
			set
			{
				_isAutoUpdated = value;
			}
		}

		#endregion IsAutoUpdated Property

		#region isDepthBuffered Property

		private bool _isDepthBuffered = true;
		protected bool isDepthBuffered
		{
			get
			{
				return _isDepthBuffered;
			}
			set
			{
				_isDepthBuffered = value;
			}
		}

		#endregion isDepthBuffered Property

		#region HardwareGammaEnabled Property

		private bool _hwGamma;

        /// <summary>
        /// Indicates whether on rendering, linear color space is converted to 
        /// sRGB gamma colour space. This is the exact opposite conversion of
        /// what is indicated by <see cref="Texture.HardwareGammaEnabled" />, and can only
        /// be enabled on creation of the render target. For render windows, it's
        /// enabled through the 'gamma' creation misc parameter. For textures, 
        /// it is enabled through the hwGamma parameter to the create call.
        /// </summary>
        public bool HardwareGammaEnabled
        {
            get
            {
                return _hwGamma;
            }
            protected set
            {
                _hwGamma = value;
            }
        }

		#endregion HardwareGammaEnabled Property

		#region FSAA Property

		/// <summary>
		///    Flag that states whether this target is FSAA.
		/// </summary>
		private int _fsaa = 0;
		/// <summary>
		///    Gets/Sets whether this RenderTarget is FSAA or not.
		/// </summary>
		public virtual int FSAA
		{
			get
			{
				return _fsaa;
			}
			set
			{
				_fsaa = value;
			}
		}

		#endregion FSAA Property

		#region FSAAHint Property
		private string _fsaaHint;
		public string FSAAHint
		{
			get
			{
				return _fsaaHint;
			}
			set
			{
				_fsaaHint = value;
			}
		}
		#endregion FSAAHint Property

		#endregion Fields and Properties

		public RenderTarget()
            : base()
		{

		}

		public RenderTarget( string name )
            : base()
		{
			_name = name;
		}

		#region Event Handling

		/// <summary>
		///    Gets fired before this RenderTarget is going to update.  Handling this event is ideal
		///    in situation, such as RenderTextures, where before rendering the scene to the texture,
		///    you would like to show/hide certain entities to avoid rendering more than was necessary
		///    to reduce processing time.
		/// </summary>
		public event RenderTargetEventHandler BeforeUpdate;

		/// <summary>
		///    Gets fired right after this RenderTarget has been updated each frame.  If the scene has been modified
		///    in the BeforeUpdate event (such as showing/hiding objects), this event can be handled to set everything 
		///    back to normal.
		/// </summary>
		public event RenderTargetEventHandler AfterUpdate;

		/// <summary>
		///    Gets fired before rendering the contents of each viewport attached to this RenderTarget.
		/// </summary>
		public event RenderTargetViewportEventHandler BeforeViewportUpdate;

		/// <summary>
		///    Gets fired after rendering the contents of each viewport attached to this RenderTarget.
		/// </summary>
		public event RenderTargetViewportEventHandler AfterViewportUpdate;

		/// <summary>
		/// Gets fired when a Viewport has been added to this RenderTarget.
		/// </summary>
		public event RenderTargetViewportEventHandler ViewportAdded;

		/// <summary>
		/// Gets fired when a Viewport has been removed from this RenderTarget.
		/// </summary>
		public event RenderTargetViewportEventHandler ViewportRemoved;

		protected virtual void OnBeforeUpdate()
		{
			if ( BeforeUpdate != null )
			{
				BeforeUpdate( new RenderTargetEventArgs( this ) );
			}
		}

		protected virtual void OnAfterUpdate()
		{
			if ( AfterUpdate != null )
			{
				AfterUpdate( new RenderTargetEventArgs( this ) );
			}
		}

		protected virtual void OnBeforeViewportUpdate( Viewport viewport )
		{
			if ( BeforeViewportUpdate != null )
			{
				BeforeViewportUpdate( new RenderTargetViewportEventArgs( this, viewport ) );
			}
		}

		protected virtual void OnAfterViewportUpdate( Viewport viewport )
		{
			if ( AfterViewportUpdate != null )
			{
				AfterViewportUpdate( new RenderTargetViewportEventArgs( this, viewport ) );
			}
		}

		protected virtual void OnViewportAdded( Viewport viewport )
		{
			if ( ViewportAdded != null )
			{
				ViewportAdded( new RenderTargetViewportEventArgs( this, viewport ) );
			}
		}

		protected virtual void OnViewportRemoved( Viewport viewport )
		{
			if ( ViewportRemoved != null )
			{
				ViewportRemoved( new RenderTargetViewportEventArgs( this, viewport ) );
			}
		}

		#endregion Event Handling

		#region Viewport Management

		private ViewportCollection _viewportList = new ViewportCollection();
		/// <summary>
		/// The list of viewports
		/// </summary>
		protected virtual ViewportCollection viewportList
		{
			get
			{
				return _viewportList;
			}
		}

		/// <summary>
		///     Gets the number of viewports attached to this render target.
		/// </summary>
		public virtual int ViewportCount
		{
			get
			{
				return _viewportList.Count;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public virtual Viewport GetViewport( int index )
		{
			Debug.Assert( index >= 0 && index < _viewportList.Count );

			return _viewportList.Values[ index ];
		}

		/// <summary>
		///     Adds a viewport to the rendering target.
		/// </summary>
		/// <param name="camera"></param>
		/// <returns></returns>
		public Viewport AddViewport( Camera camera )
		{
			return AddViewport( camera, 0, 0, 1.0f, 1.0f, 0 );
		}

		/// <summary>
		///		Adds a viewport to the rendering target.
		/// </summary>
		/// <remarks>
		///		A viewport is the rectangle into which rendering output is sent. This method adds
		///		a viewport to the render target, rendering from the supplied camera. The
		///		rest of the parameters are only required if you wish to add more than one viewport
		///		to a single rendering target. Note that size information passed to this method is
		///		passed as a parametric, i.e. it is relative rather than absolute. This is to allow
		///		viewports to automatically resize along with the target.
		/// </remarks>
		/// <param name="camera">The camera from which the viewport contents will be rendered (mandatory)</param>
		/// <param name="left">The relative position of the left of the viewport on the target, as a value between 0 and 1.</param>
		/// <param name="top">The relative position of the top of the viewport on the target, as a value between 0 and 1.</param>
		/// <param name="width">The relative width of the viewport on the target, as a value between 0 and 1.</param>
		/// <param name="height">The relative height of the viewport on the target, as a value between 0 and 1.</param>
		/// <param name="zOrder">The relative order of the viewport with others on the target (allows overlapping
		///		viewports i.e. picture-in-picture). Higher ZOrders are on top of lower ones. The actual number
		///		is irrelevant, only the relative ZOrder matters (you can leave gaps in the numbering)</param>
		/// <returns></returns>
		public virtual Viewport AddViewport( Camera camera, float left, float top, float width, float height, int zOrder )
		{
			if ( _viewportList.ContainsKey( zOrder ) )
				throw new AxiomException( String.Format( "Can't create another viewport for {0} with Z-Order {1} because a viewport exists with this Z-Order already.", _name, zOrder ) );

			// create a new camera and add it to our internal collection
			Viewport viewport = new Viewport( camera, this, left, top, width, height, zOrder );
			this._viewportList.Add( viewport );

			OnViewportAdded( viewport );

			return viewport;
		}

		/// <summary>
		/// Removes a viewport at a given ZOrder.
		/// </summary>
		/// <param name="zOrder">
		/// The <see cref="Viewport.ZOrder"/> of the viewport to be removed.
		/// </param>
		public virtual void RemoveViewport( int zOrder )
		{
			if ( _viewportList.ContainsKey( zOrder ) )
			{
				Viewport viewport = _viewportList[ zOrder ];

				OnViewportRemoved( viewport );

				_viewportList.Remove( zOrder );
			}
		}

		/// <summary>
		/// Removes all viewports on this target.
		/// </summary>
		public virtual void RemoveAllViewports()
		{
			foreach ( Viewport pair in _viewportList.Values )
			{
				OnViewportRemoved( pair );
			}

			_viewportList.Clear();
		}

		#endregion Viewport Management

		#region Statistics

		/// <summary>
		/// Retieves details of current rendering performance.
		/// </summary>
		/// <param name="lastFPS">The number of frames per second (FPS) based on the last frame rendered.</param>
		/// <param name="avgFPS">
		/// The FPS rating based on an average of all the frames rendered 
		/// since rendering began (the call to Root.StartRendering).
		/// </param>
		/// <param name="bestFPS">The best FPS rating that has been achieved since rendering began.</param>
		/// <param name="worstFPS">The worst FPS rating seen so far</param>
		[Obsolete("The RenderTarget.Statistics Property provides complete access to all statistical data.")]
		public virtual void GetStatistics( out float lastFPS, out float avgFPS, out float bestFPS, out float worstFPS )
		{
			lastFPS = _statistics.LastFPS;
			avgFPS = _statistics.AverageFPS;
			bestFPS = _statistics.BestFPS;
			worstFPS = _statistics.WorstFPS;
		}

		/// <summary>
		/// Retieves details of current rendering performance.
		/// </summary>
		public virtual FrameStatistics Statistics
		{
			get
			{
				return this._statistics;
			}
		}

		/// <summary>
		/// The number of frames per second (FPS) based on the last frame rendered.
		/// </summary>
		public virtual float LastFPS
		{
			get
			{
				return _statistics.LastFPS;
			}
		}

		/// The average frames per second (FPS) since call to Root.StartRendering.
		/// </summary>
		public virtual float AverageFPS
		{
			get
			{
				return _statistics.AverageFPS;
			}
		}

		/// <summary>
		/// The best frames per second (FPS) since call to Root.StartRendering.
		/// </summary>
		public virtual float BestFPS
		{
			get
			{
				return _statistics.BestFPS;
			}
		}

		/// <summary>
		/// The worst frames per second (FPS) since call to Root.StartRendering.
		/// </summary>
		public virtual float WorstFPS
		{
			get
			{
				return _statistics.WorstFPS;
			}
		}

		/// <summary>
		/// The best frame time
		/// </summary>
		public virtual float BestFrameTime
		{
			get
			{
				return _statistics.BestFrameTime;
			}
		}

		/// <summary>
		/// The worst frame time
		/// </summary>
		public virtual float WorstFrameTime
		{
			get
			{
				return _statistics.WorstFrameTime;
			}
		}

		/// <summary>
		/// The number of triangles rendered in the last Update() call. 
		/// </summary>
		public virtual float LastTriangleCount
		{
			get
			{
				return _statistics.TriangleCount;
			}
		}

		/// <summary>
		/// The number of triangles rendered in the last Update() call. 
		/// </summary>
		public virtual float LastBatchCount
		{
			get
			{
				return _statistics.BatchCount;
			}
		}

		/// <summary>
		/// Resets saved frame-rate statistices.
		/// </summary>
		public virtual void ResetStatistics()
		{
			_statistics.AverageFPS = 0.0F;
			_statistics.BestFPS = 0.0F;
			_statistics.LastFPS = 0.0F;
			_statistics.WorstFPS = 999.0F;
			_statistics.TriangleCount = 0;
			_statistics.BatchCount = 0;
			_statistics.BestFrameTime = 999999;
			_statistics.WorstFrameTime = 0;

			_lastTime = _timer.Milliseconds;
			_lastSecond = _lastTime;
			_frameCount = 0;
		}

		protected void updateStatistics()
		{
			_frameCount++;
			long thisTime = _timer.Milliseconds;

			// check frame time
			long frameTime = thisTime - _lastTime;
			_lastTime = thisTime;

			_statistics.BestFrameTime = Math.Utility.Min( _statistics.BestFrameTime, frameTime );
			_statistics.WorstFrameTime = Math.Utility.Max( _statistics.WorstFrameTime, frameTime );

			// check if new second (update only once per second)
			if ( thisTime - _lastSecond > 1000 )
			{
				// new second - not 100% precise
				_statistics.LastFPS = (float)_frameCount / (float)( thisTime - _lastSecond ) * 1000;

				if ( _statistics.AverageFPS == 0 )
					_statistics.AverageFPS = _statistics.LastFPS;
				else
					_statistics.AverageFPS = ( _statistics.AverageFPS + _statistics.LastFPS ) / 2; // not strictly correct, but good enough

				_statistics.BestFPS = Math.Utility.Max( _statistics.BestFPS, _statistics.LastFPS );
				_statistics.WorstFPS = Math.Utility.Min( _statistics.WorstFPS, _statistics.LastFPS );

				_lastSecond = thisTime;
				_frameCount = 0;

			}

		}
		#endregion Statistics

		#region Custom Attributes

		/// <summary>
		/// Gets a custom (maybe platform-specific) attribute.
		/// </summary>
		/// <remarks>
		/// This is a nasty way of satisfying any API's need to see platform-specific details.
		/// Its horrid, but D3D needs this kind of info. At least it's abstracted.
		/// </remarks>
		/// <param name="attribute">The name of the attribute.</param>
		/// <returns></returns>
		[Obsolete( "The GetCustomAttribute function has been deprecated in favor of an indexer property. Use object[\"attribute\"] to get custom attributes." )]
		public object GetCustomAttribute( string attribute )
		{
			return this[ attribute ];
		}

		public virtual object this[ string attribute ]
		{
			get
			{
				throw new Exception( String.Format( "Attribute [{0}] not found.", attribute ) );
			}
		}

		#endregion Custom Attributes

		#region Methods

		/// <summary>
		///		Updates the window contents.
		/// </summary>
		/// <remarks>
		///		The window is updated by telling each camera which is supposed
		///		to render into this window to render it's view, and then
		///		the window buffers are swapped via SwapBuffers()
		///	</remarks>
		public virtual void Update()
		{
			Update( true );
		}

		/// <summary>
		///		Updates the window contents.
		/// </summary>
		/// <remarks>
		///		The window is updated by telling each camera which is supposed
		///		to render into this window to render it's view, and then
		///		the window buffers are swapped via SwapBuffers() if requested.
		///	</remarks>
		///	<param name="swapBuffers">
		///	If set to true, the window will immediately
		///	swap it's buffers after update. Otherwise, the buffers are
		///	not swapped, and you have to call swapBuffers yourself sometime
		///	later. You might want to do this on some rendersystems which 
		///	pause for queued rendering commands to complete before accepting
		///	swap buffers calls - so you could do other CPU tasks whilst the 
		///	queued commands complete. Or, you might do this if you want custom
		///	control over your windows, such as for externally created windows.
		///	</param>
		public virtual void Update( bool swapBuffers )
		{
			// Clear per frame statistics
			_statistics.BatchCount = _statistics.TriangleCount = 0;

			// notify event handlers that this RenderTarget is about to be updated
			OnBeforeUpdate();

			// Go through viewportList in Z-order
			// Tell each to refresh
			for ( int i = 0; i < _viewportList.Count; i++ )
			{
				Viewport viewport = _viewportList.Values[ i ];

				// notify listeners (pre)
				OnBeforeViewportUpdate( viewport );

				viewport.Update();

				_statistics.TriangleCount += viewport.Camera.RenderedFaceCount;
				_statistics.BatchCount += viewport.Camera.RenderedBatchCount;

				// notify event handlers the the viewport is updated
				OnAfterViewportUpdate( viewport );
			}

			// Update statistics (always on top)
			updateStatistics();

			// notify event handlers that this target update is complete
			OnAfterUpdate();

			if ( swapBuffers )
				this.SwapBuffers( Root.Instance.RenderSystem.WaitForVerticalBlank );
		}



		/// <summary>
		///		Utility method to notify a render target that a camera has been removed, 
		///		incase it was referring to it as a viewer.
		/// </summary>
		/// <param name="camera"></param>
		internal void NotifyCameraRemoved( Camera camera )
		{
            if ( _viewportList == null )
                return;

            for ( int i = 0; i < _viewportList.Count; i++ )
            {
                Viewport viewport = _viewportList.Values[ i ];

                // remove the link to this camera
                if ( viewport.Camera == camera )
                {
                    viewport.Camera = null;
                }
            }
        }

		/// <summary>
		/// Retrieve information about the render target.
		/// </summary>
		/// <param name="width"></param>
		/// <param name="height"></param>
		/// <param name="colourDepth"></param>
		public void GetMetrics( out int width, out int height, out int colorDepth )
		{
			width = _width;
			height = _height;
			colorDepth = _colorDepth;
		}

		/// <summary>
		///		Saves window contents to file (i.e. screenshot);
		/// </summary>
		public void WriteContentsToFile( string fileName )
		{
			PixelFormat pf = suggestPixelFormat();

			byte[] data = new byte[ Width * Height * PixelUtil.GetNumElemBytes( pf ) ];
			GCHandle bufGCHandle = new GCHandle();
			bufGCHandle = GCHandle.Alloc( data, GCHandleType.Pinned );
			PixelBox pb = new PixelBox( Width, Height, 1, pf, bufGCHandle.AddrOfPinnedObject() );

			CopyContentsToMemory( pb );

			( new Image() ).FromDynamicImage( data, Width, Height, 1, pf, false, 1, 0 ).Save( fileName );

			if ( bufGCHandle.IsAllocated )
				bufGCHandle.Free();
		}

		public void CopyContentsToMemory( PixelBox pb )
		{
			CopyContentsToMemory( pb, FrameBuffer.Auto );
		}

		public abstract void CopyContentsToMemory( PixelBox pb, FrameBuffer buffer );

		/// <summary>
		/// Suggests a pixel format to use for extracting the data in this target, when calling <see cref="CopyContentsToMemory"/>.
		/// </summary>
		/// <returns></returns>
		protected virtual PixelFormat suggestPixelFormat()
		{
			return PixelFormat.BYTE_RGBA;
		}

		/// <summary>
		///		Swaps the frame buffers to display the next frame.
		/// </summary>
		/// <remarks>
		///		For targets that are double-buffered so that no
		///     'in-progress' versions of the scene are displayed
		///     during rendering. Once rendering has completed (to
		///		an off-screen version of the window) the buffers
		///		are swapped to display the new frame.
		///	</remarks>
		/// <param name="waitForVSync">
		///		If true, the system waits for the
		///		next vertical blank period (when the CRT beam turns off
		///		as it travels from bottom-right to top-left at the
		///		end of the pass) before flipping. If false, flipping
		///		occurs no matter what the beam position. Waiting for
		///		a vertical blank can be slower (and limits the
		///		framerate to the monitor refresh rate) but results
		///		in a steadier image with no 'tearing' (a flicker
		///		resulting from flipping buffers when the beam is
		///		in the progress of drawing the last frame). 
		///</param>
		public virtual void SwapBuffers( bool waitForVSync )
		{
		}


		#endregion Methods

		#region IDisposable Implementation

		/// <summary>
		/// Class level dispose method
		/// </summary>
		/// <param name="disposeManagedResources">True if Unmanaged resources should be released.</param>
		protected override void dispose( bool disposeManagedResources )
		{
			if ( !IsDisposed )
			{
				if ( disposeManagedResources )
				{
					// Delete viewports
                    if (_viewportList != null)
                    {
                        this.RemoveAllViewports();
                        _viewportList = null;
                    }


					// Write final performance stats
					if ( LogManager.Instance != null )
						LogManager.Instance.Write( "Final Stats [{0}]: FPS <A,B,W> : {1:#.00} {2:#.00} {3:#.00}", this.Name, this._statistics.AverageFPS, this._statistics.BestFPS, this._statistics.WorstFPS );
				}
			}
			base.dispose( disposeManagedResources );
		}

		#endregion IDisposable Implementation

	}
}
