#region LGPL License
/*
Axiom Game Engine Library
Copyright (C) 2003  Axiom Project Team

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

using System;
using System.Collections;
using System.Diagnostics;
using Axiom.Core;
using Axiom.Collections;

namespace Axiom.Graphics {

    #region Delegate/EventArg Declarations

    /// <summary>
    ///    Delegate for RenderTarget update events.
    /// </summary>
    public delegate void RenderTargetUpdateEventHandler(object sender, RenderTargetUpdateEventArgs e);

    /// <summary>
    ///    Delegate for Viewport update events.
    /// </summary>
    public delegate void ViewportUpdateEventHandler(object sender, ViewportUpdateEventArgs e);

    /// <summary>
    ///    Event arguments for render target updates.
    /// </summary>
    public class RenderTargetUpdateEventArgs : EventArgs {
    }

    /// <summary>
    ///    Event arguments for viewport updates through while processing a RenderTarget.
    /// </summary>
    public class ViewportUpdateEventArgs : EventArgs {
        internal Viewport viewport;

        public Viewport Viewport {
            get {
                return viewport;
            }
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
    public abstract class RenderTarget {
        #region Fields

        /// <summary>
        ///    Height of this render target.
        /// </summary>
        protected int height;
        /// <summary>
        ///    Width of this render target.
        /// </summary>
        protected int width;
        protected int colorDepth;
        /// <summary>
        ///    Indicates the priority of this render target.  Higher priority targets will get processed first.
        /// </summary>
        protected RenderTargetPriority priority;
        /// <summary>
        ///    Unique name assigned to this render target.
        /// </summary>
        protected string name;
        /// <summary>
        ///    Optional debug text that can be display on this render target.  May not be relevant for all targets.
        /// </summary>
        protected string debugText;
        /// <summary>
        ///    Collection of viewports attached to this render target.
        /// </summary>
        protected ViewportCollection viewportList;
        /// <summary>
        ///    Number of faces rendered during the last update to this render target.
        /// </summary>
        protected int numFaces;
        /// <summary>
        ///    Custom attributes that can be assigned to this target.
        /// </summary>
        protected Hashtable customAttributes;
        /// <summary>
        ///    Flag that states whether this target is active or not.
        /// </summary>
        // TODO: Find out the proper way to set this
        protected bool isActive = true;

        #endregion Fields

        #region Constructor

        public RenderTarget() {
            this.viewportList = new ViewportCollection(this);
            this.customAttributes = new Hashtable();

            this.numFaces = 0;
        }

        #endregion

        #region Event handling

        /// <summary>
        ///    Gets fired before this RenderTarget is going to update.  Handling this event is ideal
        ///    in situation, such as RenderTextures, where before rendering the scene to the texture,
        ///    you would like to show/hide certain entities to avoid rendering more than was necessary
        ///    to reduce processing time.
        /// </summary>
        public event RenderTargetUpdateEventHandler BeforeUpdate;

        /// <summary>
        ///    Gets fired right after this RenderTarget has been updated each frame.  If the scene has been modified
        ///    in the BeforeUpdate event (such as showing/hiding objects), this event can be handled to set everything 
        ///    back to normal.
        /// </summary>
        public event RenderTargetUpdateEventHandler AfterUpdate;

        /// <summary>
        ///    Gets fired before rendering the contents of each viewport attached to this RenderTarget.
        /// </summary>
        public event ViewportUpdateEventHandler BeforeViewportUpdate;

        /// <summary>
        ///    Gets fired after rendering the contents of each viewport attached to this RenderTarget.
        /// </summary>
        public event ViewportUpdateEventHandler AfterViewportUpdate;

        protected virtual void OnBeforeUpdate() {
            if(BeforeUpdate != null) {
                BeforeUpdate(this, new RenderTargetUpdateEventArgs());
            }
        }

        protected virtual void OnAfterUpdate() {
            if(AfterUpdate != null) {
                AfterUpdate(this, new RenderTargetUpdateEventArgs());
            }
        }

        protected virtual void OnBeforeViewportUpdate(Viewport viewport) {
            if(BeforeViewportUpdate != null) {
                ViewportUpdateEventArgs e = new ViewportUpdateEventArgs();
                e.viewport = viewport;
                BeforeViewportUpdate(this, e);
            }
        }

        protected virtual void OnAfterViewportUpdate(Viewport viewport) {
            if(AfterViewportUpdate != null) {
                ViewportUpdateEventArgs e = new ViewportUpdateEventArgs();
                e.viewport = viewport;
                AfterViewportUpdate(this, e);
            }
        }

        #endregion

        #region Public properties

        /// <summary>
        ///    Gets/Sets the name of this render target.
        /// </summary>
        public string Name {
            get { 
                return this.name; 
            }
            set { 
                this.name = value; 
            }
        }

        /// <summary>
        ///    Gets sets whether this RenderTarget is active or not.  When inactive, it will be skipped
        ///    during processing each frame.
        /// </summary>
        public virtual bool IsActive {
            get {
                return isActive;
            }
            set {
                isActive = value;
            }
        }

        /// <summary>
        ///    Gets the priority of this render target.  Higher priority targets will get processed first.
        /// </summary>
        public RenderTargetPriority Priority {
            get {
                return priority;
            }
        }

        /// <summary>
        /// Gets/Sets the debug text of this render target.
        /// </summary>
        public string DebugText {
            get {
                return this.debugText;
            }
            set {
                this.debugText = value;
            }
        }

        /// <summary>
        /// Gets/Sets the width of this render target.
        /// </summary>
        public int Width {
            get { 
                return this.width; 
            }
            set { 
                this.width = value; 
            }
        }

        /// <summary>
        /// Gets/Sets the height of this render target.
        /// </summary>
        public int Height {
            get { 
                return this.height; 
            }
            set { 
                this.height = value; 
            }
        }

        /// <summary>
        /// Gets/Sets the color depth of this render target.
        /// </summary>
        public int ColorDepth {
            get { 
                return this.colorDepth; 
            }
            set { 
                this.colorDepth = value; 
            }
        } 

        /// <summary>
        ///     Gets the number of viewports attached to this render target.
        /// </summary>
        public int NumViewports {
            get {
                return viewportList.Count;
            }
        }

        /// <summary>
        ///     Signals whether textures should be flipping before this target
        ///     is updated.  Required for render textures in some API's.
        /// </summary>
        public virtual bool RequiresTextureFlipping {
            get {
                return false;
            }
        }

        #endregion

        #region Public methods

        /// <summary>
        ///		Tells the target to update it's contents.
        /// </summary>
        /// <remarks>
        ///		If the engine is not running in an automatic rendering loop
        ///		(started using RenderSystem.StartRendering()),
        ///		the user of the library is responsible for asking each render
        ///		target to refresh. This is the method used to do this. It automatically
        ///		re-renders the contents of the target using whatever cameras have been
        ///		pointed at it (using Camera.RenderTarget).
        ///	
        ///		This allows the engine to be used in multi-windowed utilities
        ///		and for contents to be refreshed only when required, rather than
        ///		constantly as with the automatic rendering loop.
        ///	</remarks>
        public virtual void Update() {
            numFaces = 0;

            // notify event handlers that this RenderTarget is about to be updated
            OnBeforeUpdate();

            // Go through viewportList in Z-order
            // Tell each to refresh
            for(int i = 0; i < viewportList.Count; i++) {
                Viewport viewport = viewportList[i];

                // notify listeners (pre)
                OnBeforeViewportUpdate(viewport);                

                viewportList[i].Update();
                numFaces += viewportList[i].Camera.NumRenderedFaces;

                // notify event handlers the the viewport is updated
                OnAfterViewportUpdate(viewport);
            }

            // notify event handlers that this target update is complete
            OnAfterUpdate();
        }

        /// <summary>
        ///		Destroys the RenderTarget.
        /// </summary>
        public abstract void Destroy();

        /// <summary>
        ///		Used to create a viewport for this RenderTarget.
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="top"></param>
        /// <param name="left"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="zOrder"></param>
        /// <returns></returns>
        public virtual Viewport AddViewport(Camera camera, int left, int top, int width, int height, int zOrder) {
            // create a new camera and add it to our internal collection
            Viewport viewport = new Viewport(camera, this, left, top, width, height, zOrder);
            this.viewportList.Add(viewport);

            return viewport;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public Viewport GetViewport(int index) {
            Debug.Assert(index >= 0 && index < viewportList.Count);

            return viewportList[index];
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="attribute"></param>
        /// <returns></returns>
        public virtual object GetCustomAttribute(string attribute) {
            Debug.Assert(customAttributes.ContainsKey(attribute));

            return customAttributes[attribute];
        }

        #endregion
    }
}
