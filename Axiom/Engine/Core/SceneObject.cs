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

using Axiom.MathLib;
using Axiom.Graphics;

namespace Axiom.Core {
    /// <summary>
    /// Abstract class definining a movable object in a scene.
    /// </summary>
    /// <remarks>
    /// Instances of this class are discrete, relatively small, movable objects
    /// which are attached to SceneNode objects to define their position.						  
    /// </remarks>
    // TODO: Add local OBB / convex hull
    public abstract class SceneObject {
        #region Member variables 

        /// <summary>Node that this node is attached to.</summary>
        protected SceneNode parentNode;
        /// <summary>Is this object visible?</summary>
        protected bool isVisible;
        /// <summary>Name of this object.</summary>
        protected string name;
        /// <summary>The render queue to use when rendering this object.</summary>
        protected RenderQueueGroupID renderQueueID;
        /// <summary>Flags determining whether this object is included/excluded from scene queries.</summary>
        protected ulong queryFlags;
        /// <summary>Cached world bounding box of this object.</summary>
        protected AxisAlignedBox worldAABB;
        /// <summary>Cached world bounding spehere.</summary>
        protected Sphere worldBoundingSphere;

        #endregion Member variables 

        #region Constructors

        public SceneObject() {
            this.isVisible = true;

            // set default RenderQueueGroupID for this movable object
            renderQueueID = RenderQueueGroupID.Main;

            queryFlags = unchecked(0xffffffff);
        }

        #endregion Constructors

        #region Properties

        /// <summary>
        ///		An abstract method required by subclasses to return the bounding box of this object in local coordinates.
        /// </summary>
        public abstract AxisAlignedBox BoundingBox {
            get;
        }

        /// <summary>
        ///		An abstract method required by subclasses to return the bounding box of this object in local coordinates.
        /// </summary>
        public abstract float BoundingRadius {
            get;
        }

        /// <summary>
        ///		Gets the parent node that this object is attached to.
        /// </summary>
        public SceneNode ParentNode {
            get {
                return parentNode;
            }
        }

        /// <summary>
        ///		See if this object is attached to another node.
        /// </summary>
        public bool IsAttached {
            get {
                return (parentNode == null);
            }
        }

        /// <summary>
        ///		States whether or not this object should be visible.
        /// </summary>
        public virtual bool IsVisible {
            get {
                return isVisible;
            }
            set {
                isVisible = value;
            }
        }

        /// <summary>
        ///		Name of this SceneObject.
        /// </summary>
        public string Name {
            get { return name;}
            set { name = value; }
        }

        /// <summary>
        ///    Returns the full transformation of the parent SceneNode or the attachingPoint node
        /// </summary>
        public virtual Matrix4 ParentFullTransform {
            get {
                if(parentNode != null)
                    return parentNode.FullTransform;
                
                // identity if no parent
                return Matrix4.Identity;
            }
        }

        /// <summary>
        ///    Allows showing the bounding box of an invidual SceneObject.
        /// </summary>
        /// <remarks>
        ///    This shows the bounding box of the SceneNode that the SceneObject is currently attached to.
        /// </remarks>
        public bool ShowBoundingBox {
            get {
                return parentNode.ShowBoundingBox;
            }
            set {
                parentNode.ShowBoundingBox = true;
            }
        }

        #endregion Properties

        #region Methods

        /// <summary>
        ///    Overloaded method.  Calls the overload with a default of not deriving the transform.
        /// </summary>
        /// <returns></returns>
        public AxisAlignedBox GetWorldBoundingBox() {
            return GetWorldBoundingBox(false);
        }

        /// <summary>
        ///    Retrieves the axis-aligned bounding box for this object in world coordinates.
        /// </summary>
        /// <returns></returns>
        public virtual AxisAlignedBox GetWorldBoundingBox(bool derive) {
            if(derive) {
                worldAABB = this.BoundingBox;
                worldAABB.Transform(this.ParentFullTransform);
            }

            return worldAABB;
        }

        /// <summary>
        ///    Overloaded method.  Calls the overload with a default of not deriving the transform.
        /// </summary>
        /// <returns></returns>
        public Sphere GetWorldBoundingSphere() {
            return GetWorldBoundingSphere(false);
        }

        /// <summary>
        ///    Retrieves the worldspace bounding sphere for this object.
        /// </summary>
        /// <param name="derive">Whether or not to derive from parent transforms.</param>
        /// <returns></returns>
        public virtual Sphere GetWorldBoundingSphere(bool derive) {
            if(derive) {
                worldBoundingSphere.Radius = this.BoundingRadius;
                worldBoundingSphere.Center = parentNode.DerivedPosition;
            }

            return worldBoundingSphere;
        }

        #endregion Methods

        #region Internal engine methods

        /// <summary>
        ///		Internal method called to notify the object that it has been attached to a node.
        /// </summary>
        /// <param name="node">Scene node to notify.</param>
        internal virtual void NotifyAttached(SceneNode node) {
            parentNode = node;
        }

        /// <summary>
        ///		Internal method to notify the object of the camera to be used for the next rendering operation.
        /// </summary>
        /// <remarks>
        ///		Certain objects may want to do specific processing based on the camera position. This method notifies
        ///		them incase they wish to do this.
        /// </remarks>
        /// <param name="camera"></param>
        internal abstract void NotifyCurrentCamera(Camera camera);

        /// <summary>
        ///		An abstract method that causes the specified RenderQueue to update itself.  
        /// </summary>
        /// <remarks>This is an internal method used by the engine assembly only.</remarks>
        /// <param name="queue">The render queue that this object should be updated in.</param>
        internal abstract void UpdateRenderQueue(RenderQueue queue);

        #endregion Internal engine methods
    }
}
