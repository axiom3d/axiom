#region LGPL License

/*
Axiom Graphics Engine Library
Copyright � 2003-2011 Axiom Project Team

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
using Axiom.Core;
using Axiom.Math;
using Axiom.Graphics;
using System.Collections.Generic;
using Axiom.Utilities;
using static Axiom.Math.Utility;

#endregion Namespace Declarations

#region Ogre Synchronization Information

// <ogresynchronization>
//     <file name="OgreOverlay.h"   revision="1.26.2.1" lastUpdated="10/5/2005" lastUpdatedBy="DanielH" />
//     <file name="OgreOverlay.cpp" revision="1.31" lastUpdated="10/5/2005" lastUpdatedBy="DanielH" />
// </ogresynchronization>

#endregion

namespace Axiom.Overlays
{
    /// <summary>
    ///    Represents a layer which is rendered on top of the 'normal' scene contents.
    /// </summary>
    /// <remarks>
    ///    An overlay is a container for visual components (2D and 3D) which will be 
    ///    rendered after the main scene in order to composite heads-up-displays, menus
    ///    or other layers on top of the contents of the scene.
    ///    <p/>
    ///    An overlay always takes up the entire size of the viewport, although the 
    ///    components attached to it do not have to. An overlay has no visual element
    ///    in itself, it it merely a container for visual elements.
    ///    <p/>
    ///    Overlays are created by calling SceneManager.CreateOverlay, or by defining them
    ///    in special text scripts (.overlay files). As many overlays
    ///    as you like can be defined; after creation an overlay is hidden i.e. not
    ///    visible until you specifically enable it by calling Show(). This allows you to have multiple
    ///    overlays predefined (menus etc) which you make visible only when you want.
    ///    It is possible to have multiple overlays enabled at once; in this case the
    ///    relative ZOrder parameter of the overlays determine which one is displayed
    ///    on top.
    ///    <p/>
    ///    By default overlays are rendered into all viewports. This is fine when you only
    ///    have fullscreen viewports, but if you have picture-in-picture views, you probably
    ///    don't want the overlay displayed in the smaller viewports. You turn this off for 
    ///    a specific viewport by calling the Viewport.DisplayOverlays property.
    /// </remarks>
    public class Overlay : Resource
    {
        #region Member variables

        /// <summary>
        /// Internal root node, used as parent for 3D objects
        /// </summary>
        protected SceneNode rootNode;

        /// <summary>2D element list.</summary>
        protected List<OverlayElementContainer> elementList = new List<OverlayElementContainer>();

        protected Dictionary<string, OverlayElementContainer> elementLookup =
            new Dictionary<string, OverlayElementContainer>();

        /// <summary>Degrees of rotation around center.</summary>
        protected float rotate;

        /// <summary>Scroll values, offsets.</summary>
        protected float scrollX, scrollY;

        /// <summary>Scale values.</summary>
        protected float scaleX, scaleY;

        /// <summary> Camera relative transform. </summary>
        protected Matrix4 transform = Matrix4.Identity;

        /// <summary> Used when passing transform to overlay elements.</summary>
        protected Matrix4[] xform = new Matrix4[1]
                                    {
                                        Matrix4.Identity
                                    };

        protected bool isTransformOutOfDate;
        protected bool isTransformUpdated;

        protected int zOrder;
        protected bool isVisible;
        protected bool isInitialised;
        protected string origin;

        #endregion Member variables

        #region Constructors

        /// <summary>
        ///    Constructor: do not call direct, use SceneManager.CreateOverlay
        /// </summary>
        /// <param name="name"></param>
        internal Overlay(string name)
            : base()
        {
            Name = name;
            this.scaleX = 1.0f;
            this.scaleY = 1.0f;
            this.isTransformOutOfDate = true;
            this.isTransformUpdated = true;
            this.zOrder = 100;
            this.isInitialised = false;
            this.rootNode = new SceneNode(null);
        }

        #endregion Constructors

        #region Methods

        /// <summary>
        ///    Adds a 2d element to this overlay.
        /// </summary>
        /// <remarks>
        ///    Containers are created and managed using the OverlayManager. A container
        ///    could be as simple as a square panel, or something more complex like
        ///    a grid or tree view. Containers group collections of other elements,
        ///    giving them a relative coordinate space and a common z-order.
        ///    If you want to attach a gui widget to an overlay, you have to do it via
        ///    a container.
        /// </remarks>
        /// <param name="element"></param>
        public void AddElement(OverlayElementContainer element)
        {
            this.elementList.Add(element);
            this.elementLookup.Add(element.Name, element);

            // notify the parent
            element.NotifyParent(null, this);

            AssignZOrders();

            GetWorldTransforms(this.xform);

            element.NotifyWorldTransforms(this.xform);
            element.NotifyViewport();
        }

        /// <summary>
        ///    Adds a node capable of holding 3D objects to the overlay.
        /// </summary>
        /// <remarks>
        ///    Although overlays are traditionally associated with 2D elements, there 
        ///    are reasons why you might want to attach 3D elements to the overlay too.
        ///    For example, if you wanted to have a 3D cockpit, which was overlaid with a
        ///    HUD, then you would create 2 overlays, one with a 3D object attached for the
        ///    cockpit, and one with the HUD elements attached (the zorder of the HUD 
        ///    overlay would be higher than the cockpit to ensure it was always on top).
        ///    <p/>
        ///    A SceneNode can have any number of 3D objects attached to it. SceneNodes
        ///    are created using SceneManager.CreateSceneNode, and are normally attached 
        ///    (directly or indirectly) to the root node of the scene. By attaching them
        ///    to an overlay, you indicate that:<OL>
        ///    <LI>You want the contents of this node to only appear when the overlay is active</LI>
        ///    <LI>You want the node to inherit a coordinate space relative to the camera,
        ///    rather than relative to the root scene node</LI>
        ///    <LI>You want these objects to be rendered after the contents of the main scene
        ///    to ensure they are rendered on top</LI>
        ///    </OL>
        ///    One major consideration when using 3D objects in overlays is the behavior of 
        ///    the depth buffer. Overlays are rendered with depth checking off, to ensure
        ///    that their contents are always displayed on top of the main scene (to do 
        ///    otherwise would result in objects 'poking through' the overlay). The problem
        ///    with using 3D objects is that if they are concave, or self-overlap, then you
        ///    can get artifacts because of the lack of depth buffer checking. So you should 
        ///    ensure that any 3D objects you us in the overlay are convex and don't overlap
        ///    each other. If they must overlap, split them up and put them in 2 overlays.
        /// </remarks>
        /// <param name="node"></param>
        public void AddElement(SceneNode node)
        {
            // add the scene node as a child of the root node
            this.rootNode.AddChild(node);
        }

        /// <summary>
        /// Removes a 2D container from the overlay.
        /// </summary>
        /// <remarks>
        /// Consider using <see>Hide</see>.
        /// </remarks>
        public void RemoveElement(string name)
        {
            RemoveElement(GetChild(name));
        }

        /// <summary>
        /// Removes a 2D container from the overlay.
        /// </summary>
        /// <remarks>
        /// Consider using <see>Hide</see>.
        /// </remarks>
        /// <param name="element"></param>
        public void RemoveElement(OverlayElementContainer element)
        {
            if (this.elementList.Contains(element))
            {
                this.elementList.Remove(element);
            }
            if (this.elementLookup.ContainsKey(element.Name))
            {
                this.elementLookup.Remove(element.Name);
            }

            AssignZOrders();
            element.NotifyParent(null, null);
        }

        /// <summary>
        /// Removes a 3D element from the overlay.
        /// </summary>
        /// <param name="node"></param>
        public void RemoveElement(SceneNode node)
        {
            this.rootNode.RemoveChild(node);
        }

        /// <summary>
        ///    Clears the overlay of all attached items.
        /// </summary>
        public void Clear()
        {
            this.rootNode.Clear();
            this.elementList.Clear();
        }

        /// <summary>
        ///    Shows this overlay if it is not already visible.
        /// </summary>
        public void Show()
        {
            IsVisible = true;
        }

        /// <summary>
        ///    Hides this overlay if it is currently being displayed.
        /// </summary>
        public void Hide()
        {
            IsVisible = false;
        }

        protected void Initialize()
        {
            // add 2d elements
            for (var i = 0; i < this.elementList.Count; i++)
            {
                var container = (OverlayElementContainer)this.elementList[i];
                container.Initialize();
            }
            this.isInitialised = true;
        }

        /// <summary>
        ///    Internal method to put the overlay contents onto the render queue.
        /// </summary>
        /// <param name="camera">Current camera being used in the render loop.</param>
        /// <param name="queue">Current render queue.</param>
        public void FindVisibleObjects(Camera camera, RenderQueue queue)
        {
            if (OverlayManager.Instance.HasViewportChanged)
            {
                for (var i = 0; i < this.elementList.Count; i++)
                {
                    var container = (OverlayElementContainer)this.elementList[i];
                    container.NotifyViewport();
                }
            }

            if (this.isVisible)
            {
                // update transform of elements
                if (this.isTransformUpdated)
                {
                    GetWorldTransforms(this.xform);
                    for (var i = 0; i < this.elementList.Count; i++)
                    {
                        var container = (OverlayElementContainer)this.elementList[i];
                        container.NotifyWorldTransforms(this.xform);
                    }
                    this.isTransformUpdated = false;
                }

                // add 3d elements
                this.rootNode.Position = camera.DerivedPosition;
                this.rootNode.Orientation = camera.DerivedOrientation;
                this.rootNode.Update(true, false);

                // set up the default queue group for the objects about to be added
                var oldGroupID = queue.DefaultRenderGroup;
                var oldPriority = queue.DefaultRenderablePriority;

                queue.DefaultRenderGroup = RenderQueueGroupID.Overlay;
                queue.DefaultRenderablePriority = (ushort)((this.zOrder * 100) - 1);
                this.rootNode.FindVisibleObjects(camera, queue, true, false);

                // reset the group
                queue.DefaultRenderGroup = oldGroupID;
                queue.DefaultRenderablePriority = oldPriority;

                // add 2d elements
                for (var i = 0; i < this.elementList.Count; i++)
                {
                    var container = (OverlayElementContainer)this.elementList[i];
                    container.Update();
                    container.UpdateRenderQueue(queue);
                }
            }
        }

        /// <summary>
        /// This returns a OverlayElement at position x,y.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public virtual OverlayElement FindElementAt(float x, float y)
        {
            OverlayElement ret = null;
            var currZ = -1;

            for (var i = 0; i < this.elementList.Count; i++)
            {
                var container = (OverlayElementContainer)this.elementList[i];
                var z = container.ZOrder;
                if (z > currZ)
                {
                    var elementFound = container.FindElementAt(x, y);
                    if (elementFound != null)
                    {
                        currZ = elementFound.ZOrder;
                        ret = elementFound;
                    }
                }
            }
            return ret;
        }

        /// <summary>
        ///    Gets a child container of this overlay by name.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public OverlayElementContainer GetChild(string name)
        {
            return (OverlayElementContainer)this.elementLookup[name];
        }

        /// <summary>
        ///    Used to transform the overlay when scrolling, scaling etc.
        /// </summary>
        /// <param name="xform">Array of Matrix4s to populate with the world 
        ///    transforms of this overlay.
        /// </param>
        public void GetWorldTransforms(Matrix4[] xform)
        {
            if (this.isTransformOutOfDate)
            {
                UpdateTransforms();
            }

            xform[0] = this.transform;
        }

        public Quaternion GetWorldOrientation()
        {
            // n/a
            return Quaternion.Identity;
        }

        public Vector3 GetWorldPosition()
        {
            // n/a
            return Vector3.Zero;
        }

        /// <summary>
        ///    Adds the passed in angle to the rotation applied to this overlay.
        /// </summary>
        public void Rotate(float degrees)
        {
            Rotation = (this.rotate += degrees);
        }

        /// <summary>
        ///    Scrolls the overlay by the offsets provided.
        /// </summary>
        /// <remarks>
        ///    This method moves the overlay by the amounts provided. As with
        ///    other methods on this object, a full screen width / height is represented
        ///    by the value 1.0.
        /// </remarks>
        /// <param name="xOffset"></param>
        /// <param name="yOffset"></param>
        public void Scroll(float xOffset, float yOffset)
        {
            this.scrollX += xOffset;
            this.scrollY += yOffset;
            this.isTransformOutOfDate = true;
            this.isTransformUpdated = true;
        }

        /// <summary>
        ///    Sets the scaling factor of this overlay.
        /// </summary>
        /// <remarks>
        ///    You can use this to set an scale factor to be used to zoom an overlay.
        /// </remarks>
        /// <param name="x">Horizontal scale value, where 1.0 = normal, 0.5 = half size etc</param>
        /// <param name="y">Vertical scale value, where 1.0 = normal, 0.5 = half size etc</param>
        public void SetScale(float x, float y)
        {
            this.scaleX = x;
            this.scaleY = y;
            this.isTransformOutOfDate = true;
            this.isTransformUpdated = true;
        }

        /// <summary>
        ///    Sets the scrolling factor of this overlay.
        /// </summary>
        /// <remarks>
        ///    You can use this to set an offset to be used to scroll an 
        ///    overlay around the screen.
        /// </remarks>
        /// <param name="x">
        ///    Horizontal scroll value, where 0 = normal, -0.5 = scroll so that only
        ///    the right half the screen is visible etc
        /// </param>
        /// <param name="y">
        ///    Vertical scroll value, where 0 = normal, 0.5 = scroll down by half 
        ///    a screen etc.
        /// </param>
        public void SetScroll(float x, float y)
        {
            this.scrollX = x;
            this.scrollY = y;
            this.isTransformOutOfDate = true;
            this.isTransformUpdated = true;
        }

        /// <summary>
        ///    Internal lazy update method.
        /// </summary>
        protected void UpdateTransforms()
        {
            // Ordering:
            //    1. Scale
            //    2. Rotate
            //    3. Translate
            var rot3x3 = Matrix3.Identity;
            var scale3x3 = Matrix3.Zero;

            rot3x3.FromEulerAnglesXYZ(0.0f, 0.0f, DegreesToRadians((Real)this.rotate));
            scale3x3.m00 = this.scaleX;
            scale3x3.m11 = this.scaleY;
            scale3x3.m22 = 1.0f;

            this.transform = rot3x3 * scale3x3;
            this.transform.Translation = new Vector3(this.scrollX, this.scrollY, 0);

            this.isTransformOutOfDate = false;
        }

        /// <summary>
        /// Updates container elements' Z-ordering
        /// </summary>
        protected void AssignZOrders()
        {
            var zorder = this.zOrder * 100;
            // notify attached 2d elements
            for (var i = 0; i < this.elementList.Count; i++)
            {
                zorder = ((OverlayElementContainer)this.elementList[i]).NotifyZOrder(zorder);
            }
        }

        #endregion Methods

        #region Properties

        /// <summary>
        ///    Gets whether this overlay is being displayed or not.
        /// </summary>
        public bool IsVisible
        {
            get
            {
                return this.isVisible;
            }
            set
            {
                this.isVisible = value;
                if (this.isVisible && !this.isInitialised)
                {
                    Initialize();
                }
            }
        }

        public bool IsInitialized
        {
            get
            {
                return this.isInitialised;
            }
        }

        /// <summary>
        ///    Gets/Sets the rotation applied to this overlay, in degrees.
        /// </summary>
        public float Rotation
        {
            get
            {
                return this.rotate;
            }
            set
            {
                this.rotate = value;
                this.isTransformOutOfDate = true;
                this.isTransformUpdated = true;
            }
        }

        /// <summary>
        ///    Gets the current x scale value.
        /// </summary>
        public float ScaleX
        {
            get
            {
                return this.scaleX;
            }
        }

        /// <summary>
        ///    Gets the current y scale value.
        /// </summary>
        public float ScaleY
        {
            get
            {
                return this.scaleY;
            }
        }

        /// <summary>
        ///    Gets the current x scroll value.
        /// </summary>
        public float ScrollX
        {
            get
            {
                return this.scrollX;
            }
        }

        /// <summary>
        ///    Gets the  current y scroll value.
        /// </summary>
        public float ScrollY
        {
            get
            {
                return this.scrollY;
            }
        }

        /// <summary>
        ///    Z ordering of this overlay. Valid values are between 0 and 650.
        /// </summary>
        public int ZOrder
        {
            get
            {
                return this.zOrder;
            }
            set
            {
                Contract.Requires(value < 650, "ZOrder", "Overlay ZOrder cannot be greater than 650!");
                this.zOrder = value;
                AssignZOrders();
            }
        }

        public Quaternion DerivedOrientation
        {
            get
            {
                return Quaternion.Identity;
            }
        }

        public Vector3 DerivedPosition
        {
            get
            {
                return Vector3.Zero;
            }
        }

        #endregion Properties

        #region Implementation of Resource

        /// <summary>
        ///		
        /// </summary>
        protected override void load()
        {
            // do nothing
        }

        /// <summary>
        ///		
        /// </summary>
        protected override void unload()
        {
            // do nothing
        }

        #endregion
    }
}