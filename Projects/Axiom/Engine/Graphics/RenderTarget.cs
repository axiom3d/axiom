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
using System.Text;
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
    public delegate void RenderTargetEventHandler(RenderTargetEventArgs e);

    /// <summary>
    ///    Delegate for Viewport update events.
    /// </summary>
    public delegate void RenderTargetViewportEventHandler(RenderTargetViewportEventArgs e);

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

        public RenderTargetEventArgs(RenderTarget source)
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

        public RenderTargetViewportEventArgs(RenderTarget source, Viewport viewport)
            : base(source)
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
        // The following funcs are not implemented 1:1 as we use native
        // delegates for dispatching events:
        // addListener
        // removeListener
        // removeAllListeners
        //
        // The following type and methods are not implemented yet
        // Impl
        // GetImpl
        //
        // The following events currently have different names:
        // PreUpdate -> BeforeUpdate
        // PostUpdate -> AfterUpdate
        // ViewportPreUpdate -> BeforeViewportUpdate
        // ViewportPostUpdate -> AfterViewportUpdate

        #region Enumerations and Structures

        [Flags]
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

        [AxiomHelper(0, 8, "Cached timer used for statistic queries")]
        private readonly ITimer _timer = Root.Instance.Timer;

        [OgreVersion(1, 7, 2790)]
        protected FrameStatistics stats;

        [OgreVersion(1, 7, 2790)]
        protected long lastTime;

        [OgreVersion(1, 7, 2790)]
        protected long lastSecond;

        [OgreVersion(1, 7, 2790)]
        protected long frameCount;

        #region DepthBufferPool Property

        [OgreVersion(1, 7, 2790)]
        protected PoolId depthBufferPoolId;

        [OgreVersion(1, 7, 2790)]
        public PoolId DepthBufferPool
        {
            get
            {
                return depthBufferPoolId;
            }
            set
            {
                if (depthBufferPoolId != value)
                {
                    depthBufferPoolId = value;
                    DetachDepthBuffer();
                }
            }
        }
        #endregion DepthBufferPool Property

        #region Height Property

        /// <summary>
        /// Height of this render target.
        /// </summary>
        [OgreVersion(1, 7, 2790)]
        protected int height;
        /// <summary>
        /// Gets the height of this render target.
        /// </summary>
        [OgreVersion(1, 7, 2790)]
        public virtual int Height
        {
            get
            {
                return height;
            }
        }

        #endregion Height Property

        #region Width Property

        /// <summary>
        /// Width of this render target.
        /// </summary>
        [OgreVersion(1, 7, 2790)]
        protected int width;
        /// <summary>
        /// Gets the width of this render target.
        /// </summary>
        [OgreVersion(1, 7, 2790)]
        public virtual int Width
        {
            get
            {
                return width;
            }
        }

        #endregion Width Property

        #region ColorDepth Property

        /// <summary>
        /// Color depth of this render target.
        /// </summary>
        [OgreVersion(1, 7, 2790)]
        protected int colorDepth;
        /// <summary>
        /// Gets the color depth of this render target.
        /// </summary>
        [OgreVersion(1, 7, 2790)]
        public virtual int ColorDepth
        {
            get
            {
                return colorDepth;
            }
        }

        #endregion ColorDepth Property

        #region DepthBuffer Property

        [OgreVersion(1, 7, 2790)]
        protected DepthBuffer depthBuffer;
        /// <summary>
        /// Gets the depthbuffer attached to this render target.
        /// </summary>
        [OgreVersion(1, 7, 2790)]
        public virtual DepthBuffer DepthBuffer
        {
            get
            {
                return depthBuffer;
            }
        }

        #endregion ColorDepth Property

        #region Priority Property

        /// <summary>
        /// Indicates the priority of this render target.  Higher priority targets will get processed first.
        /// </summary>
        [OgreVersion(1, 7, 2790)]
        protected RenderTargetPriority priority;
        /// <summary>
        /// Gets/Sets the priority of this render target.  Higher priority targets will get processed first.
        /// </summary>
        [OgreVersion(1, 7, 2790)]
        public virtual RenderTargetPriority Priority
        {
            get
            {
                return priority;
            }
            set
            {
                priority = value;
            }
        }

        #endregion Priority Property

        #region Name Property

        /// <summary>
        /// Unique name assigned to this render target.
        /// </summary>
        [OgreVersion(1, 7, 2790)]
        protected string name;
        /// <summary>
        /// Gets the name of this render target.
        /// </summary>
        [OgreVersion(1, 7, 2790)]
        public virtual string Name
        {
            get
            {
                return name;
            }
        }


        #endregion Name Property

        #region RequiresTextureFlipping Property

        /// <summary>
        ///     Signals whether textures should be flipping before this target
        ///     is updated.  Required for render textures in some API's.
        /// </summary>
        [OgreVersion(1, 7, 2790)]
        public abstract bool RequiresTextureFlipping { get; }

        #endregion RequiresTextureFlipping Property

        #region IsActive Property

        /// <summary>
        ///    Flag that states whether this target is active or not.
        /// </summary>
        [OgreVersion(1, 7, 2790)]
        protected bool active = true;

        /// <summary>
        ///    Gets/Sets whether this RenderTarget is active or not.  When inactive, it will be skipped
        ///    during processing each frame.
        /// </summary>
        [OgreVersion(1, 7, 2790)]
        public virtual bool IsActive
        {
            get
            {
                return active && !IsDisposed;
            }
            set
            {
                active = value;
            }
        }

        #endregion IsActive Property

        #region IsPrimary Property

        /// <summary>
        /// Indicates whether this target is the primary window. The
        /// primary window is special in that it is destroyed when
        /// ogre is shut down, and cannot be destroyed directly.
        /// This is the case because it holds the context for vertex,
        /// index buffers and textures..
        /// </summary>
        [OgreVersion(1, 7, 2790)]
        public virtual bool IsPrimary
        {
            get
            {
                return false;
            }
        }

        #endregion IsActive Property

        #region IsAutoUpdated Property

        /// <summary>
        ///     Is this render target updated automatically each frame?
        /// </summary>
        private bool autoUpdate = true;
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
                return autoUpdate;
            }
            set
            {
                autoUpdate = value;
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

        #region IsHardwareGammaEnabled Property

        protected bool hwGamma;

        /// <summary>
        /// Indicates whether on rendering, linear color space is converted to 
        /// sRGB gamma colour space. This is the exact opposite conversion of
        /// what is indicated by <see cref="Texture.HardwareGammaEnabled" />, and can only
        /// be enabled on creation of the render target. For render windows, it's
        /// enabled through the 'gamma' creation misc parameter. For textures, 
        /// it is enabled through the hwGamma parameter to the create call.
        /// </summary>
        public virtual bool IsHardwareGammaEnabled
        {
            get
            {
                return hwGamma;
            }
        }

        #endregion IsHardwareGammaEnabled Property

        #region FSAA Property

        /// <summary>
        ///    Flag that states whether this target is FSAA.
        /// </summary>
        protected int fsaa;
        /// <summary>
        ///    Gets/Sets whether this RenderTarget is FSAA or not.
        /// </summary>
        public virtual int FSAA
        {
            get
            {
                return fsaa;
            }
        }

        #endregion FSAA Property

        #region FSAAHint Property

        protected string fsaaHint = "";

        /// <summary>
        /// Gets the FSAA hint 
        /// <see cref="Root.CreateRenderWindow(string, int, int, bool, NamedParameterList)"/>
        /// </summary>
        public string FSAAHint
        {
            get
            {
                return fsaaHint;
            }
        }

        #endregion FSAAHint Property

        #region NumViewports

        /// <summary>
        /// Returns the number of viewports attached to this target.
        /// </summary>
        [OgreVersion(1, 7, 2790)]
        public virtual int NumViewports
        {
            get
            {
                return ViewportList.Count;
            }
        }

        #endregion

        #endregion Fields and Properties

        #region Constructor

        [OgreVersion(1, 7, 2790)]
        protected RenderTarget()
            : this("")
        {
        }

        [AxiomHelper(0, 8, "Default initializes name")]
        protected RenderTarget(string name)
        {
            this.name = name;
            priority = RenderTargetPriority.Default;
            depthBufferPoolId = PoolId.Default;
            active = true;
            autoUpdate = true;
            ResetStatistics();
        }

        #endregion

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

        #region FirePreUpdate

        [OgreVersion(1, 7, 2790)]
        protected virtual void FirePreUpdate()
        {
            if (BeforeUpdate != null)
            {
                BeforeUpdate(new RenderTargetEventArgs(this));
            }
        }

        #endregion

        #region FirePostUpdate

        [OgreVersion(1, 7, 2790)]
        protected virtual void FirePostUpdate()
        {
            if (AfterUpdate != null)
            {
                AfterUpdate(new RenderTargetEventArgs(this));
            }
        }

        #endregion

        #region FireViewportPreUpdate

        [OgreVersion(1, 7, 2790)]
        protected virtual void FireViewportPreUpdate(Viewport viewport)
        {
            if (BeforeViewportUpdate != null)
            {
                BeforeViewportUpdate(new RenderTargetViewportEventArgs(this, viewport));
            }
        }

        #endregion

        #region FireViewportPostUpdate

        [OgreVersion(1, 7, 2790)]
        protected virtual void FireViewportPostUpdate(Viewport viewport)
        {
            if (AfterViewportUpdate != null)
            {
                AfterViewportUpdate(new RenderTargetViewportEventArgs(this, viewport));
            }
        }

        #endregion

        #region FireViewportAdded

        [OgreVersion(1, 7, 2790)]
        protected virtual void FireViewportAdded(Viewport viewport)
        {
            if (ViewportAdded != null)
            {
                ViewportAdded(new RenderTargetViewportEventArgs(this, viewport));
            }
        }

        #endregion

        #region FireViewportRemoved

        [OgreVersion(1, 7, 2790)]
        protected virtual void FireViewportRemoved(Viewport viewport)
        {
            if (ViewportRemoved != null)
            {
                ViewportRemoved(new RenderTargetViewportEventArgs(this, viewport));
            }
        }

        #endregion

        #endregion Event Handling

        #region Viewport Management

        [OgreVersion(1, 7, 2790)]
        protected ViewportCollection ViewportList = new ViewportCollection();

        #region GetViewport

        /// <summary>
        /// Retrieves a pointer to the viewport with the given index.
        /// </summary>
        [OgreVersion(1, 7, 2790)]
        public virtual Viewport GetViewport(int index)
        {
            Debug.Assert(index >= 0 && index < ViewportList.Count);

            return ViewportList.Values[index];
        }

        #endregion

        #region GetViewportByZOrder

        /// <summary>
        ///  Retrieves a pointer to the viewport with the given zorder. 
        /// </summary>
        /// <remarks>
        /// throws if not found.
        /// </remarks>
        [OgreVersion(1, 7, 2790)]
        public virtual Viewport GetViewportByZOrder(int zOrder)
        {
            Viewport viewport;
            if (!ViewportList.TryGetValue(zOrder, out viewport))
            {
                throw new AxiomException("No viewport with given zorder : {0}", zOrder);
            }
            return viewport;
        }

        #endregion

        #region HasViewportWithZOrder

        /// <summary>
        /// Checks if a viewport exists at the given ZOrder.
        /// </summary>
        /// <param name="zOrder"></param>
        /// <returns>true if and only if a viewport exists at the given ZOrder.</returns>
        public virtual bool HasViewportWithZOrder(int zOrder)
        {
            return ViewportList.ContainsKey(zOrder);
        }

        #endregion

        #region AddViewport

        /// <summary>
        ///     Adds a viewport to the rendering target.
        /// </summary>
        /// <param name="camera"></param>
        /// <returns></returns>
        [AxiomHelper(0, 8, "defaulting overload")]
        public Viewport AddViewport(Camera camera)
        {
            return AddViewport(camera, 0, 0, 1.0f, 1.0f, 0);
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
        /// <param name="nwidth">The relative width of the viewport on the target, as a value between 0 and 1.</param>
        /// <param name="nheight">The relative height of the viewport on the target, as a value between 0 and 1.</param>
        /// <param name="zOrder">The relative order of the viewport with others on the target (allows overlapping
        ///		viewports i.e. picture-in-picture). Higher ZOrders are on top of lower ones. The actual number
        ///		is irrelevant, only the relative ZOrder matters (you can leave gaps in the numbering)</param>
        /// <returns></returns>
        [OgreVersion(1, 7, 2790)]
        public virtual Viewport AddViewport(Camera camera, float left, float top, float nwidth, float nheight, int zOrder)
        {
            if (ViewportList.ContainsKey(zOrder))
                throw new AxiomException("Can't create another viewport for {0} with Z-Order {1} because a viewport exists with this Z-Order already.", name, zOrder);

            // create a new camera and add it to our internal collection
            var viewport = new Viewport(camera, this, left, top, nwidth, nheight, zOrder);
            ViewportList.Add(viewport);

            FireViewportAdded(viewport);

            return viewport;
        }

        #endregion

        #region RemoveViewport

        /// <summary>
        /// Removes a viewport at a given ZOrder.
        /// </summary>
        /// <param name="zOrder">
        /// The <see cref="Viewport.ZOrder"/> of the viewport to be removed.
        /// </param>
        [OgreVersion(1, 7, 2790)]
        public virtual void RemoveViewport(int zOrder)
        {
            Viewport viewport;
            if (!ViewportList.TryGetValue(zOrder, out viewport))
                return;
            FireViewportRemoved(viewport);
            viewport.Dispose();
            ViewportList.Remove(zOrder);
        }

        #endregion

        #region RemoveAllViewports

        /// <summary>
        /// Removes all viewports on this target.
        /// </summary>
        [OgreVersion(1, 7, 2790)]
        public virtual void RemoveAllViewports()
        {
            foreach (var it in ViewportList.Values)
            {
                FireViewportRemoved(it);
                it.Dispose();
            }

            ViewportList.Clear();
        }

        #endregion

        #endregion Viewport Management

        #region Statistics

        #region GetStatistics

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
        [OgreVersion(1, 7, 2790)]
        public virtual void GetStatistics(out float lastFPS, out float avgFPS, out float bestFPS, out float worstFPS)
        {
            lastFPS = stats.LastFPS;
            avgFPS = stats.AverageFPS;
            bestFPS = stats.BestFPS;
            worstFPS = stats.WorstFPS;
        }

        #endregion

        #region FrameStatistics

        /// <summary>
        /// Retieves details of current rendering performance.
        /// </summary>
        [OgreVersion(1, 7, 2790)]
        public virtual FrameStatistics Statistics
        {
            get
            {
                return stats;
            }
        }

        #endregion

        #region LastFPS

        /// <summary>
        /// The number of frames per second (FPS) based on the last frame rendered.
        /// </summary>
        [OgreVersion(1, 7, 2790)]
        public virtual float LastFPS
        {
            get
            {
                return stats.LastFPS;
            }
        }

        #endregion

        #region AverageFPS

        /// <summary>
        /// The average frames per second (FPS) since call to Root.StartRendering.
        /// </summary>
        [OgreVersion(1, 7, 2790)]
        public virtual float AverageFPS
        {
            get
            {
                return stats.AverageFPS;
            }
        }

        #endregion

        #region BestFPS

        /// <summary>
        /// The best frames per second (FPS) since call to Root.StartRendering.
        /// </summary>
        [OgreVersion(1, 7, 2790)]
        public virtual float BestFPS
        {
            get
            {
                return stats.BestFPS;
            }
        }

        #endregion

        #region WorstFPS

        /// <summary>
        /// The worst frames per second (FPS) since call to Root.StartRendering.
        /// </summary>
        [OgreVersion(1, 7, 2790)]
        public virtual float WorstFPS
        {
            get
            {
                return stats.WorstFPS;
            }
        }

        #endregion

        #region BestFrameTime

        /// <summary>
        /// The best frame time
        /// </summary>
        [OgreVersion(1, 7, 2790)]
        public virtual float BestFrameTime
        {
            get
            {
                return stats.BestFrameTime;
            }
        }

        #endregion

        #region WorstFrameTime

        /// <summary>
        /// The worst frame time
        /// </summary>
        [OgreVersion(1, 7, 2790)]
        public virtual float WorstFrameTime
        {
            get
            {
                return stats.WorstFrameTime;
            }
        }

        #endregion

        #region LastTriangleCount

        /// <summary>
        /// The number of triangles rendered in the last Update() call. 
        /// </summary>
        [OgreVersion(1, 7, 2790, "TriangleCount")]
        public virtual float LastTriangleCount
        {
            get
            {
                return stats.TriangleCount;
            }
        }

        #endregion

        #region LastBatchCount

        /// <summary>
        /// The number of triangles rendered in the last Update() call. 
        /// </summary>
        [OgreVersion(1, 7, 2790, "BatchCount")]
        public virtual float LastBatchCount
        {
            get
            {
                return stats.BatchCount;
            }
        }

        #endregion

        #region ResetStatistics

        /// <summary>
        /// Resets saved frame-rate statistices.
        /// </summary>
        [OgreVersion(1, 7, 2790)]
        public virtual void ResetStatistics()
        {
            stats.AverageFPS = 0.0F;
            stats.BestFPS = 0.0F;
            stats.LastFPS = 0.0F;
            stats.WorstFPS = 999.0F;
            stats.TriangleCount = 0;
            stats.BatchCount = 0;
            stats.BestFrameTime = 999999;
            stats.WorstFrameTime = 0;

            lastTime = _timer.Milliseconds;
            lastSecond = lastTime;
            frameCount = 0;
        }

        #endregion

        #region UpdateStatistics

        [OgreVersion(1, 7, 2790)]
        protected void UpdateStatistics()
        {
            frameCount++;
            var thisTime = _timer.Milliseconds;

            // check frame time
            var frameTime = thisTime - lastTime;
            lastTime = thisTime;

            stats.BestFrameTime = Math.Utility.Min(stats.BestFrameTime, frameTime);
            stats.WorstFrameTime = Math.Utility.Max(stats.WorstFrameTime, frameTime);

            // check if new second (update only once per second)
            if (thisTime - lastSecond <= 1000)
                return;

            // new second - not 100% precise
            stats.LastFPS = (float)frameCount / (thisTime - lastSecond) * 1000;

            if (stats.AverageFPS == 0)
                stats.AverageFPS = stats.LastFPS;
            else
                stats.AverageFPS = (stats.AverageFPS + stats.LastFPS) / 2; // not strictly correct, but good enough

            stats.BestFPS = Math.Utility.Max(stats.BestFPS, stats.LastFPS);
            stats.WorstFPS = Math.Utility.Min(stats.WorstFPS, stats.LastFPS);

            lastSecond = thisTime;
            frameCount = 0;
        }

        #endregion

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
        [Obsolete("The GetCustomAttribute function has been deprecated in favor of an indexer property. Use object[\"attribute\"] to get custom attributes.")]
        [OgreVersion(1, 7, 2790)]
        public object GetCustomAttribute(string attribute)
        {
            return this[attribute];
        }

        [AxiomHelper(0, 8, "GetCustomAttribute replacement")]
        public virtual object this[string attribute]
        {
            get
            {
                throw new Exception(String.Format("Attribute [{0}] not found.", attribute));
            }
        }

        #endregion Custom Attributes

        #region Methods

        #region AttachDepthBuffer

        /// <summary>
        /// Attaches a depth buffer to this target
        /// </summary>
        /// <param name="ndepthBuffer">The buffer to attach</param>
        /// <returns>false if couldn't attach</returns>
        [OgreVersion(1, 7, 2790)]
        public virtual bool AttachDepthBuffer(DepthBuffer ndepthBuffer)
        {
            var retVal = false;

            if (ndepthBuffer.IsCompatible(this))
            {
                retVal = true;
                DetachDepthBuffer();
                depthBuffer = ndepthBuffer;
                depthBuffer.NotifyRenderTargetAttached(this);
            }

            return retVal;
        }

        #endregion

        #region DetachDepthBuffer

        [OgreVersion(1, 7, 2790)]
        public virtual void DetachDepthBuffer()
        {
            if (depthBuffer == null)
                return;

            depthBuffer.NotifyRenderTargetDetached(this);
            depthBuffer = null;
        }

        #endregion

        #region _DetachDepthBuffer

        /// <summary>
        /// Detaches DepthBuffer without notifying it from the detach.
        /// Useful when called from the DepthBuffer while it iterates through attached
        /// RenderTargets <see cref="Axiom.Graphics.DepthBuffer.PoolId"/>
        /// </summary>
        [OgreVersion(1, 7, 2790)]
        public virtual void _DetachDepthBuffer()
        {
            depthBuffer = null;
        }

        #endregion

        #region Update

        /// <summary>
        ///		Updates the window contents.
        /// </summary>
        /// <remarks>
        ///		The window is updated by telling each camera which is supposed
        ///		to render into this window to render it's view, and then
        ///		the window buffers are swapped via SwapBuffers()
        ///	</remarks>
        [AxiomHelper(0, 8, "defaulting overload")]
        public void Update()
        {
            Update(true);
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
        [OgreVersion(1, 7, 2790)]
        public virtual void Update(bool swapBuffers)
        {
            // call implementation
            UpdateImpl();


            if (swapBuffers)
            {
                // Swap buffers
                SwapBuffers(Root.Instance.RenderSystem.WaitForVerticalBlank);
            }
        }

        #endregion

        #region UpdateImpl

        [OgreVersion(1, 7, 2790)]
        protected virtual void UpdateImpl()
        {
            BeginUpdate();
            UpdateAutoUpdatedViewports(true);
            EndUpdate();
        }

        #endregion

        #region BeginUpdate

        /// <summary>
        /// Method for manual management of rendering : fires 'preRenderTargetUpdate'
        /// and initializes statistics etc.
        /// </summary>
        /// <remarks>
        /// <ul>
        /// <li>BeginUpdate resets statistics and fires 'preRenderTargetUpdate'.</li>
        /// <li>UpdateViewport renders the given viewport (even if it is not autoupdated),
        /// fires preViewportUpdate and postViewportUpdate and manages statistics.</li>
        /// <li>UpdateAutoUpdatedViewports renders only viewports that are auto updated,
        /// fires preViewportUpdate and postViewportUpdate and manages statistics.</li>
        /// <li>EndUpdate() ends statistics calculation and fires postRenderTargetUpdate.</li>
        /// </ul>
        /// you can use it like this for example :
        /// <pre>
        /// renderTarget.BeginUpdate();
        /// renderTarget.UpdateViewport(1); // which is not auto updated
        /// renderTarget.UpdateViewport(2); // which is not auto updated
        /// renderTarget.UpdateAutoUpdatedViewports();
        /// renderTarget.EndUpdate();
        /// renderTarget.SwapBuffers(true);
        /// </pre>
        /// Please note that in that case, the zorder may not work as you expect,
        /// since you are responsible for calling UpdateViewport in the correct order.
        /// </remarks>
        [OgreVersion(1, 7, 2790)]
        public virtual void BeginUpdate()
        {
            FirePreUpdate();

            stats.TriangleCount = 0;
            stats.BatchCount = 0;
        }

        #endregion

        #region UpdateViewport

        /// <summary>
        /// Method for manual management of rendering - renders the given 
        /// viewport (even if it is not autoupdated)
        /// </summary>
        /// <remarks>
        /// This also fires preViewportUpdate and postViewportUpdate, and manages statistics.
        /// You should call it between <see cref="BeginUpdate"/> and <see cref="EndUpdate"/>.
        /// </remarks>
        /// <param name="viewport">The viewport you want to update, it must be bound to the rendertarget.</param>
        /// <param name="updateStatistics">Whether you want to update statistics or not.</param>
        [OgreVersion(1, 7, 2790)]
        public virtual void UpdateViewport(Viewport viewport, bool updateStatistics)
        {
            Debug.Assert(viewport.Target == this,
                          "RenderTarget::_updateViewport the requested viewport is not bound to the rendertarget!");

            FireViewportPreUpdate(viewport);
            viewport.Update();
            if (updateStatistics)
            {
                stats.TriangleCount += viewport.RenderedFaceCount;
                stats.BatchCount += viewport.RenderedBatchCount;
            }
            FireViewportPostUpdate(viewport);
        }

        /// <summary>
        /// Method for manual management of rendering - renders the given 
        /// viewport (even if it is not autoupdated)
        /// </summary>
        /// <remarks>
        /// This also fires preViewportUpdate and postViewportUpdate, and manages statistics.
        /// You should call it between <see cref="BeginUpdate"/> and <see cref="EndUpdate"/>.
        /// </remarks>
        /// <param name="zorder">The zorder of the viewport to update.</param>
        /// <param name="updateStatistics">Whether you want to update statistics or not.</param>
        [OgreVersion(1, 7, 2790)]
        public virtual void UpdateViewport(int zorder, bool updateStatistics)
        {
            Viewport viewport;
            if (ViewportList.TryGetValue(zorder, out viewport))
            {
                UpdateViewport(viewport, updateStatistics);
            }
            else
            {
                throw new AxiomException("No viewport with given zorder : {0}", zorder);
            }
        }

        #endregion

        #region UpdateAutoUpdatedViewports
        /// <summary>
        /// Method for manual management of rendering - renders only viewports that are auto updated
        /// </summary>
        /// <remarks>
        /// This also fires preViewportUpdate and postViewportUpdate, and manages statistics.
        /// You should call it between <see cref="BeginUpdate"/> and 
        /// <see cref="EndUpdate"/>.
        /// </remarks>
        [OgreVersion(1, 7, 2790)]
        public void UpdateAutoUpdatedViewports()
        {
			UpdateAutoUpdatedViewports( true );
		}

        /// <summary>
        /// Method for manual management of rendering - renders only viewports that are auto updated
        /// </summary>
        /// <remarks>
        /// This also fires preViewportUpdate and postViewportUpdate, and manages statistics.
        /// You should call it between <see cref="BeginUpdate"/> and 
        /// <see cref="EndUpdate"/>.
        /// </remarks>
        /// <param name="updateStatistics"></param>
        [OgreVersion(1, 7, 2790)]
        public virtual void UpdateAutoUpdatedViewports(bool updateStatistics )
        {
            // Go through viewports in Z-order
            // Tell each to refresh
            foreach (var it in ViewportList)
            {
                var viewport = it.Value;
                if (viewport.IsAutoUpdated)
                {
                    UpdateViewport(viewport, updateStatistics);
                }
            }
        }

        #endregion

        #region EndUpdate

        /// <summary>
        /// Method for manual management of rendering - finishes statistics calculation
        /// and fires 'postRenderTargetUpdate'.
        /// </summary>
        /// <remarks>
        /// You should call it after a <see cref="BeginUpdate"/>
        /// </remarks>
        [OgreVersion(1, 7, 2790)]
        public virtual void EndUpdate()
        {
            // notify listeners (post)
            FirePostUpdate();

            // Update statistics (always on top)
            UpdateStatistics();
        }

        #endregion

        #region NotifyCameraRemoved

        /// <summary>
        ///	Utility method to notify a render target that a camera has been removed, 
        /// incase it was referring to it as a viewer.
        /// </summary>
        /// <param name="camera"></param>
        [OgreVersion(1, 7, 2790)]
        internal void NotifyCameraRemoved(Camera camera)
        {
            if (ViewportList == null)
                return;

            for (var i = 0; i < ViewportList.Count; i++)
            {
                var viewport = ViewportList.Values[i];

                // remove the link to this camera
                if (viewport.Camera == camera)
                {
                    viewport.Camera = null;
                }
            }
        }

        #endregion

        #region GetMetrics

        /// <summary>
        /// Retrieve information about the render target.
        /// </summary>
        [OgreVersion(1, 7, 2790)]
        public void GetMetrics(out int nwidth, out int nheight, out int ncolorDepth)
        {
            nwidth = width;
            nheight = height;
            ncolorDepth = colorDepth;
        }

        #endregion

        #region WriteContentsToFile

        /// <summary>
        /// Saves window contents to file (i.e. screenshot);
        /// </summary>
        [OgreVersion(1, 7, 2790)]
        public void WriteContentsToFile(string fileName)
        {
            var pf = SuggestPixelFormat();

            var data = new byte[Width * Height * PixelUtil.GetNumElemBytes(pf)];
            var bufGcHandle = GCHandle.Alloc(data, GCHandleType.Pinned);
            var pb = new PixelBox(Width, Height, 1, pf, bufGcHandle.AddrOfPinnedObject());

            CopyContentsToMemory(pb);

            (new Image()).FromDynamicImage(data, Width, Height, 1, pf, false, 1, 0).Save(fileName);

            if (bufGcHandle.IsAllocated)
                bufGcHandle.Free();
        }

        #endregion

        #region WriteContentsToTimestampedFile

        /// <summary>
        /// Writes the current contents of the render target to the (PREFIX)(time-stamp)(SUFFIX) file.
        /// </summary>
        /// <returns>the name of the file used.</returns>
        [OgreVersion(1, 7, 2790)]
        public virtual String WriteContentsToTimestampedFile(String filenamePrefix, String filenameSuffix)
        {
            var filename = string.Format("{2}{0:MMddyyyyHHmmss}{1:D3}{3}",
                DateTime.Now, _timer.Milliseconds % 1000,
                filenamePrefix, filenameSuffix);
            WriteContentsToFile(filename);
            return filename;
        }

        #endregion

        #region CopyContentsToMemory

        [AxiomHelper(0, 8, "default overload")]
        public void CopyContentsToMemory(PixelBox pb)
        {
            CopyContentsToMemory(pb, FrameBuffer.Auto);
        }

        /// <summary>
        /// Copies the current contents of the render target to a pixelbox. 
        /// </summary>
        /// <remarks>
        /// See <see cref="SuggestPixelFormat"/> for a tip as to the best pixel format to
        /// extract into, although you can use whatever format you like and the 
        /// results will be converted.
        /// </remarks>
        [OgreVersion(1, 7, 2790)]
        public abstract void CopyContentsToMemory(PixelBox pb, FrameBuffer buffer);

        #endregion

        #region SuggestPixelFormat

        /// <summary>
        /// Suggests a pixel format to use for extracting the data in this target, when calling 
        /// <see cref="CopyContentsToMemory(PixelBox, FrameBuffer)"/>.
        /// </summary>
        [OgreVersion(1, 7, 2790)]
        public virtual PixelFormat SuggestPixelFormat()
        {
            return PixelFormat.BYTE_RGBA;
        }

        #endregion

        #region SwapBuffers
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
        [OgreVersion(1, 7, 2790)]
        public void SwapBuffers()
        {
			SwapBuffers( true );
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
        [OgreVersion(1, 7, 2790)]
        public virtual void SwapBuffers(bool waitForVSync )
        {
        }

        #endregion

        #endregion Methods

        #region IDisposable Implementation

        /// <summary>
        /// Class level dispose method
        /// </summary>
        /// <param name="disposeManagedResources">True if Unmanaged resources should be released.</param>
        protected override void dispose(bool disposeManagedResources)
        {
            if (!IsDisposed)
            {
                if (disposeManagedResources)
                {
                    // Delete viewports
                    if (ViewportList != null)
                    {
                        RemoveAllViewports();
                        ViewportList = null;
                    }

                    //DepthBuffer keeps track of us, avoid a dangling pointer
                    DetachDepthBuffer();

                    // Write final performance stats
                    if (LogManager.Instance != null)
                        LogManager.Instance.Write("Final Stats [{0}]: FPS <A,B,W> : {1:#.00} {2:#.00} {3:#.00}", name, stats.AverageFPS, stats.BestFPS, stats.WorstFPS);
                }
            }
            base.dispose(disposeManagedResources);
        }

        #endregion IDisposable Implementation

    }
}
