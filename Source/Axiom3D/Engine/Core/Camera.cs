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
using System.Diagnostics;

using Axiom.MathLib;
using Axiom.MathLib.Collections;

namespace Axiom
{
    /// <summary>
    ///		A viewpoint from which the scene will be rendered.
    /// </summary>
    ///<remarks>
    ///		The engine renders scenes from a camera viewpoint into a buffer of
    ///		some sort, normally a window or a texture (a subclass of
    ///		RenderTarget). the engine cameras support both perspective projection (the default,
    ///		meaning objects get smaller the further away they are) and
    ///		orthographic projection (blueprint-style, no decrease in size
    ///		with distance). Each camera carries with it a style of rendering,
    ///		e.g. full textured, flat shaded, wireframe), field of view,
    ///		rendering distances etc, allowing you to use the engine to create
    ///		complex multi-window views if required. In addition, more than
    ///		one camera can point at a single render target if required,
    ///		each rendering to a subset of the target, allowing split screen
    ///		and picture-in-picture views.
    ///		<p/>
    ///		Cameras maintain their own aspect ratios, field of view, and frustrum,
    ///		and project co-ordinates into a space measured from -1 to 1 in x and y,
    ///		and 0 to 1 in z. At render time, the camera will be rendering to a
    ///		Viewport which will translate these parametric co-ordinates into real screen
    ///		co-ordinates. Obviously it is advisable that the viewport has the same
    ///		aspect ratio as the camera to avoid distortion (unless you want it!).
    ///		<p/>
    ///		Note that a Camera can be attached to a SceneNode, using the method
    ///		SceneNode.AttachObject. If this is done the Camera will combine it's own
    ///		position/orientation settings with it's parent SceneNode. 
    ///		This is useful for implementing more complex Camera / object
    ///		relationships i.e. having a camera attached to a world object.
    /// </remarks>
    public class Camera : Frustum
    {
        #region Fields

        /// <summary>
        ///		Parent scene manager.
        /// </summary>
        protected SceneManager sceneManager;
        /// <summary>
        ///		Camera orientation.
        /// </summary>
        protected Quaternion orientation;
        /// <summary>
        ///		Camera position.
        /// </summary>
        protected Vector3 position;
        /// <summary>
        ///		Orientation dervied from parent.
        /// </summary>
        protected Quaternion derivedOrientation;
        /// <summary>
        ///		Position dervied from parent.
        /// </summary>
        protected Vector3 derivedPosition;
        /// <summary>
        ///		Whether to yaw around a fixed axis.
        /// </summary>
        protected bool isYawFixed;
        /// <summary>
        ///		Fixed axis to yaw around.
        /// </summary>
        protected Vector3 yawFixedAxis;
        /// <summary>
        ///		Rendering type (wireframe, solid, point).
        /// </summary>
        protected SceneDetailLevel sceneDetail;
        /// <summary>
        ///		Stored number of visible faces in the last render.
        /// </summary>
        protected int numFacesRenderedLastFrame;
        /// <summary>
        ///		SceneNode which this Camera will automatically track.
        /// </summary>
        protected SceneNode autoTrackTarget;
        /// <summary>
        ///		Tracking offset for fine tuning.
        /// </summary>
        protected Vector3 autoTrackOffset;
        /// <summary>
        ///		Scene LOD factor used to adjust overall LOD.
        /// </summary>
        protected float sceneLodFactor;
        /// <summary>
        ///		Inverted scene LOD factor, can be used by Renderables to adjust their LOD.
        /// </summary>
        protected float invSceneLodFactor;
        /// <summary>
        ///		Left window edge (window clip planes).
        /// </summary>
        protected float windowLeft;
        /// <summary>
        ///		Right window edge (window clip planes).
        /// </summary>
        protected float windowRight;
        /// <summary>
        ///		Top window edge (window clip planes).
        /// </summary>
        protected float windowTop;
        /// <summary>
        ///		Bottom window edge (window clip planes).
        /// </summary>
        protected float windowBottom;
        /// <summary>
        ///		Is viewing window used.
        /// </summary>
        protected bool isWindowSet;
        /// <summary>
        ///		Windowed viewport clip planes.
        /// </summary>
        protected PlaneList windowClipPlanes = new PlaneList();
        /// <summary>
        ///		Was viewing window changed?
        /// </summary>
        protected bool recalculateWindow;
        /// <summary>
        ///		The last viewport to be added using this camera.
        /// </summary>
        protected Viewport lastViewport;
        /// <summary>
        ///		Whether aspect ratio will automaticaally be recalculated when a vieport changes its size.
        /// </summary>
        protected bool autoAspectRatio;

        #endregion Fields

        #region Constructors

        public Camera( string name, SceneManager sceneManager )
        {
            // Init camera location & direction

            // Locate at (0,0,0)
            position = Vector3.Zero;
            derivedPosition = Vector3.Zero;

            // Point down -Z axis
            orientation = Quaternion.Identity;
            derivedOrientation = Quaternion.Identity;

            fieldOfView = MathUtil.RadiansToDegrees( MathUtil.PI / 4.0f );
            nearDistance = 100.0f;
            farDistance = 100000.0f;
            aspectRatio = 1.33333333333333f;
            projectionType = Projection.Perspective;

            viewMatrix = Matrix4.Zero;
            projectionMatrix = Matrix4.Zero;

            // Reasonable defaults to camera params
            sceneDetail = SceneDetailLevel.Solid;

            // Default to fixed yaw (freelook)
            this.FixedYawAxis = Vector3.UnitY;

            InvalidateFrustum();
            InvalidateView();

            // Record name & SceneManager
            this.name = name;
            this.sceneManager = sceneManager;

            // Init no tracking
            autoTrackTarget = null;
            autoTrackOffset = Vector3.Zero;

            // default these to 1 so Lod default to normal
            sceneLodFactor = this.invSceneLodFactor = 1.0f;
        }

        #endregion

        #region Frustum Members


        /// <summary>
        ///		Get the derived orientation of this frustum.
        /// </summary>
        /// <returns></returns>
        protected override Quaternion GetOrientationForViewUpdate()
        {
            return derivedOrientation;
        }


        /// <summary>
        ///		Get the derived position of this frustum.
        /// </summary>
        /// <returns></returns>
        protected override Vector3 GetPositionForViewUpdate()
        {
            return derivedPosition;
        }

        /// <summary>
        ///		Signal to update view information.
        /// </summary>
        protected override void InvalidateView()
        {
            recalculateView = true;
            recalculateWindow = true;
        }

        /// <summary>
        ///		Signal to update frustum information.
        /// </summary>
        protected override void InvalidateFrustum()
        {
            recalculateFrustum = true;
            recalculateWindow = true;
        }

        /// <summary>
        ///		Updates the frustum data.
        /// </summary>
        protected override void UpdateFrustum()
        {
            base.UpdateFrustum();
            SetWindowImpl();
        }

        /// <summary>
        ///		Updates the view matrix.
        /// </summary>
        protected override void UpdateView()
        {
            base.UpdateView();
            SetWindowImpl();
        }

        /// <summary>
        ///		Evaluates whether or not the view matrix is out of date.
        /// </summary>
        /// <returns></returns>
        protected override bool IsViewOutOfDate
        {
            get
            {
                bool returnVal = false;

                // Overridden from Frustum to use local orientation / position offsets
                // are we attached to another node?
                if ( parentNode != null )
                {
                    if ( !recalculateView && parentNode.DerivedOrientation == lastParentOrientation &&
                        parentNode.DerivedPosition == lastParentPosition )
                    {
                        returnVal = false;
                    }
                    else
                    {
                        // we are out of date with the parent scene node
                        lastParentOrientation = parentNode.DerivedOrientation;
                        lastParentPosition = parentNode.DerivedPosition;
                        derivedOrientation = lastParentOrientation * orientation;
                        derivedPosition = ( lastParentOrientation * position ) + lastParentPosition;
                        returnVal = true;
                    }
                }
                else
                {
                    // rely on own updates
                    derivedOrientation = orientation;
                    derivedPosition = position;
                }

                if ( isReflected && linkedReflectionPlane != null &&
                    !( lastLinkedReflectionPlane == linkedReflectionPlane.DerivedPlane ) )
                {

                    reflectionPlane = linkedReflectionPlane.DerivedPlane;
                    reflectionMatrix = MathUtil.BuildReflectionMatrix( reflectionPlane );
                    lastLinkedReflectionPlane = linkedReflectionPlane.DerivedPlane;
                    returnVal = true;
                }

                return returnVal || recalculateView;
            }
        }

        #endregion Frustum Members

        #region SceneObject Implementation


        public override void UpdateRenderQueue( RenderQueue queue )
        {
            // Do nothing
        }

        public override AxisAlignedBox BoundingBox
        {
            get
            {
                // a camera is not visible in the scene
                return AxisAlignedBox.Null;
            }
        }

        /// <summary>
        ///		Overridden to return a proper bounding radius for the camera.
        /// </summary>
        public override float BoundingRadius
        {
            get
            {
                // return a little bigger than the near distance
                // just to keep things just outside
                return nearDistance * 1.5f;
            }
        }

        public override void NotifyCurrentCamera( Camera camera )
        {
            // Do nothing
        }



        /// <summary>
        ///    Called by the scene manager to let this camera know how many faces were renderer within
        ///    view of this camera every frame.
        /// </summary>
        /// <param name="renderedFaceCount"></param>
        internal void NotifyRenderedFaces( int renderedFaceCount )
        {
            numFacesRenderedLastFrame = renderedFaceCount;
        }

        #endregion

        #region Public Properties


        public SceneNode AutoTrackingTarget
        {
            get
            {
                return autoTrackTarget;
            }
            set
            {
                autoTrackTarget = value;
            }
        }


        public Vector3 AutoTrackingOffset
        {
            get
            {
                return autoTrackOffset;
            }
            set
            {
                autoTrackOffset = value;
            }
        }

        /// <summary>
        ///		If set to true a vieport that owns this frustum will be able to 
        ///		recalculate the aspect ratio whenever the frustum is resized.
        /// </summary>
        /// <remarks>
        ///		You should set this to true only if the frustum / camera is used by 
        ///		one viewport at the same time. Otherwise the aspect ratio for other 
        ///		viewports may be wrong.
        /// </remarks>
        public bool AutoAspectRatio
        {
            get
            {
                return autoAspectRatio;
            }
            set
            {
                autoAspectRatio = value;	//FIXED: From true to value
            }
        }

        /// <summary>
        ///    Returns the current SceneManager that this camera is using.
        /// </summary>
        public SceneManager SceneManager
        {
            get
            {
                return sceneManager;
            }
        }

        /// <summary>
        ///		Sets the level of rendering detail required from this camera.
        /// </summary>
        /// <remarks>
        ///		Each camera is set to render at full detail by default, that is
        ///		with full texturing, lighting etc. This method lets you change
        ///		that behavior, allowing you to make the camera just render a
        ///		wireframe view, for example.
        /// </remarks>
        public SceneDetailLevel SceneDetail
        {
            get
            {
                return sceneDetail;
            }
            set
            {
                sceneDetail = value;
            }
        }

        /// <summary>
        ///     Gets/Sets the camera's orientation.
        /// </summary>
        public Quaternion Orientation
        {
            get
            {
                return orientation;
            }
            set
            {
                orientation = value;
                InvalidateView();
            }
        }

        /// <summary>
        ///     Gets/Sets the camera's position.
        /// </summary>
        public Vector3 Position
        {
            get
            {
                return position;
            }
            set
            {
                position = value;
                InvalidateView();
            }
        }

        /// <summary>
        ///		Gets/Sets the camera's direction vector.
        /// </summary>
        public Vector3 Direction
        {
            // Direction points down the negatize Z axis by default.
            get
            {
                return orientation * -Vector3.UnitZ;
            }
            set
            {
                Vector3 direction = value;

                // Do nothing if given a zero vector
                // (Replaced assert since this could happen with auto tracking camera and
                //  camera passes through the lookAt point)
                if ( direction == Vector3.Zero )
                    return;

                // Remember, camera points down -Z of local axes!
                // Therefore reverse direction of direction vector before determining local Z
                Vector3 zAdjustVector = -direction;
                zAdjustVector.Normalize();

                if ( isYawFixed )
                {
                    Vector3 xVector = yawFixedAxis.CrossProduct( zAdjustVector );
                    xVector.Normalize();

                    Vector3 yVector = zAdjustVector.CrossProduct( xVector );
                    yVector.Normalize();

                    orientation.FromAxes( xVector, yVector, zAdjustVector );
                }
                else
                {
                    // update the view of the camera
                    UpdateView();

                    // Get axes from current quaternion
                    Vector3 xAxis, yAxis, zAxis;

                    // get the vector components of the derived orientation vector
                    this.DerivedOrientation.ToAxes( out xAxis, out yAxis, out zAxis );

                    Quaternion rotationQuat;

                    if ( -zAdjustVector == zAxis )
                    {
                        // Oops, a 180 degree turn (infinite possible rotation axes)
                        // Default to yaw i.e. use current UP
                        rotationQuat = Quaternion.FromAngleAxis( MathUtil.PI, yAxis );
                    }
                    else
                    {
                        // Derive shortest arc to new direction
                        rotationQuat = zAxis.GetRotationTo( zAdjustVector );
                    }

                    orientation = rotationQuat * orientation;
                }

                // TODO If we have a fixed yaw axis, we musn't break it by using the
                // shortest arc because this will sometimes cause a relative yaw
                // which will tip the camera

                InvalidateView();
            }
        }

        /// <summary>
        ///		Gets camera's 'right' vector.
        /// </summary>
        public Vector3 Right
        {
            get
            {
                return orientation * Vector3.UnitX;
            }
        }

        /// <summary>
        ///		Gets camera's 'up' vector.
        /// </summary>
        public Vector3 Up
        {
            get
            {
                return orientation * Vector3.UnitY;
            }
        }

        /// <summary>
        ///		Get the last viewport which was attached to this camera. 
        /// </summary>
        /// <remarks>
        ///		This is not guaranteed to be the only viewport which is
        ///		using this camera, just the last once which was created referring
        ///		to it.
        /// </remarks>
        public Viewport Viewport
        {
            get
            {
                return lastViewport;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public Vector3 FixedYawAxis
        {
            get
            {
                return yawFixedAxis;
            }
            set
            {
                yawFixedAxis = value;

                if ( yawFixedAxis != Vector3.Zero )
                {
                    isYawFixed = true;
                }
                else
                {
                    isYawFixed = false;
                }
            }
        }

        /// <summary>
        ///     Sets the level-of-detail factor for this Camera.
        /// </summary>
        /// <remarks>
        ///     This method can be used to influence the overall level of detail of the scenes 
        ///     rendered using this camera. Various elements of the scene have level-of-detail
        ///     reductions to improve rendering speed at distance; this method allows you 
        ///     to hint to those elements that you would like to adjust the level of detail that
        ///     they would normally use (up or down). 
        ///     <p/>
        ///     The most common use for this method is to reduce the overall level of detail used
        ///     for a secondary camera used for sub viewports like rear-view mirrors etc.
        ///     Note that scene elements are at liberty to ignore this setting if they choose,
        ///     this is merely a hint.
        ///     <p/>
        ///     Higher values increase the detail, so 2.0 doubles the normal detail and 0.5 halves it.
        /// </remarks>
        public float LodBias
        {
            get
            {
                return sceneLodFactor;
            }
            set
            {
                Debug.Assert( value > 0.0f, "Lod bias must be greater than 0" );
                sceneLodFactor = value;
                invSceneLodFactor = 1.0f / sceneLodFactor;
            }
        }

        /// <summary>
        ///     Used for internal Lod calculations.
        /// </summary>
        public float InverseLodBias
        {
            get
            {
                return invSceneLodFactor;
            }
        }

        /// <summary>
        /// Gets the last count of triangles visible in the view of this camera.
        /// </summary>
        public int RenderedFaceCount
        {
            get
            {
                return numFacesRenderedLastFrame;
            }
        }

        /// <summary>
        ///		Gets the derived orientation of the camera.
        /// </summary>
        public Quaternion DerivedOrientation
        {
            get
            {
                UpdateView();
                return derivedOrientation;
            }
        }

        /// <summary>
        ///		Gets the derived position of the camera.
        /// </summary>
        public Vector3 DerivedPosition
        {
            get
            {
                UpdateView();
                return derivedPosition;
            }
        }

        /// <summary>
        ///		Gets the derived direction of the camera.
        /// </summary>
        public Vector3 DerivedDirection
        {
            get
            {
                UpdateView();

                // RH coords, direction points down -Z by default
                return derivedOrientation * -Vector3.UnitZ;
            }
        }

        /// <summary>
        ///		Gets the flag specifying if a viewport window is being used.
        /// </summary>
        public virtual bool IsWindowSet
        {
            get
            {
                return isWindowSet;
            }
        }

        /// <summary>
        ///		Gets the number of window clip planes for this camera.
        /// </summary>
        /// <remarks>Only applicable if IsWindowSet == true.
        /// </remarks>
        public int WindowPlaneCount
        {
            get
            {
                return windowClipPlanes.Count;
            }
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Moves the camera's position by the vector offset provided along world axes.
        /// </summary>
        /// <param name="offset"></param>
        public void Move( Vector3 offset )
        {
            position = position + offset;
            InvalidateView();
        }

        /// <summary>
        /// Moves the camera's position by the vector offset provided along it's own axes (relative to orientation).
        /// </summary>
        /// <param name="offset"></param>
        public void MoveRelative( Vector3 offset )
        {
            // Transform the axes of the relative vector by camera's local axes
            Vector3 transform = orientation * offset;

            position = position + transform;
            InvalidateView();
        }

        /// <summary>
        ///		Specifies a target that the camera should look at.
        /// </summary>
        /// <param name="target"></param>
        public void LookAt( Vector3 target )
        {
            UpdateView();
            this.Direction = ( target - derivedPosition );
        }

        /// <summary>
        ///		Pitches the camera up/down counter-clockwise around it's local x axis.
        /// </summary>
        /// <param name="degrees"></param>
        public void Pitch( float degrees )
        {
            Vector3 xAxis = orientation * Vector3.UnitX;
            Rotate( xAxis, degrees );

            InvalidateView();
        }

        /// <summary>
        ///		Rolls the camera counter-clockwise, in degrees, around its local y axis.
        /// </summary>
        /// <param name="degrees"></param>
        public void Yaw( float degrees )
        {
            Vector3 yAxis;

            if ( isYawFixed )
            {
                // Rotate around fixed yaw axis
                yAxis = yawFixedAxis;
            }
            else
            {
                // Rotate around local Y axis
                yAxis = orientation * Vector3.UnitY;
            }

            Rotate( yAxis, degrees );

            InvalidateView();
        }

        /// <summary>
        ///		Rolls the camera counter-clockwise, in degrees, around its local z axis.
        /// </summary>
        /// <param name="degrees"></param>
        public void Roll( float degrees )
        {
            // Rotate around local Z axis
            Vector3 zAxis = orientation * Vector3.UnitZ;
            Rotate( zAxis, degrees );

            InvalidateView();
        }

        /// <summary>
        ///		Rotates the camera about an arbitrary axis.
        /// </summary>
        /// <param name="quat"></param>
        public void Rotate( Quaternion quat )
        {
            // Note the order of the multiplication
            orientation = quat * orientation;

            InvalidateView();
        }

        /// <summary>
        ///		Rotates the camera about an arbitrary axis.
        /// </summary>
        /// <param name="axis"></param>
        /// <param name="degrees"></param>
        public void Rotate( Vector3 axis, float degrees )
        {
            Quaternion q = Quaternion.FromAngleAxis( MathUtil.DegreesToRadians( degrees ), axis );
            Rotate( q );
        }

        /// <summary>
        ///		Enables / disables automatic tracking of a SceneObject.
        /// </summary>
        /// <remarks>
        ///		If you enable auto-tracking, this Camera will automatically rotate to
        ///		look at the target SceneNode every frame, no matter how 
        ///		it or SceneNode move. This is handy if you want a Camera to be focused on a
        ///		single object or group of objects. Note that by default the Camera looks at the 
        ///		origin of the SceneNode, if you want to tweak this, e.g. if the object which is
        ///		attached to this target node is quite big and you want to point the camera at
        ///		a specific point on it, provide a vector in the 'offset' parameter and the 
        ///		camera's target point will be adjusted.
        /// </remarks>
        /// <param name="enabled">If true, the Camera will track the SceneNode supplied as the next 
        ///		parameter (cannot be null). If false the camera will cease tracking and will
        ///		remain in it's current orientation.
        ///	 </param> 
        /// <param name="target">The SceneObject which this Camera will track.</param>
        public void SetAutoTracking( bool enabled, MovableObject target )
        {
            SetAutoTracking( enabled, (SceneNode)target.ParentNode, Vector3.Zero );
        }

        /// <summary>
        ///		Enables / disables automatic tracking of a SceneNode.
        /// </summary>
        /// <remarks>
        ///		If you enable auto-tracking, this Camera will automatically rotate to
        ///		look at the target SceneNode every frame, no matter how 
        ///		it or SceneNode move. This is handy if you want a Camera to be focused on a
        ///		single object or group of objects. Note that by default the Camera looks at the 
        ///		origin of the SceneNode, if you want to tweak this, e.g. if the object which is
        ///		attached to this target node is quite big and you want to point the camera at
        ///		a specific point on it, provide a vector in the 'offset' parameter and the 
        ///		camera's target point will be adjusted.
        /// </remarks>
        /// <param name="enabled">If true, the Camera will track the SceneNode supplied as the next 
        ///		parameter (cannot be null). If false the camera will cease tracking and will
        ///		remain in it's current orientation.
        ///	 </param> 
        /// <param name="target">The SceneNode which this Camera will track. Make sure you don't
        ///		delete this SceneNode before turning off tracking (e.g. SceneManager.ClearScene will
        ///		delete it so be careful of this). Can be null if and only if the enabled param is false.
        ///	</param>
        public void SetAutoTracking( bool enabled, SceneNode target )
        {
            SetAutoTracking( enabled, target, Vector3.Zero );
        }

        /// <summary>
        ///		Enables / disables automatic tracking of a SceneNode.
        /// </summary>
        /// <remarks>
        ///		If you enable auto-tracking, this Camera will automatically rotate to
        ///		look at the target SceneNode every frame, no matter how 
        ///		it or SceneNode move. This is handy if you want a Camera to be focused on a
        ///		single object or group of objects. Note that by default the Camera looks at the 
        ///		origin of the SceneNode, if you want to tweak this, e.g. if the object which is
        ///		attached to this target node is quite big and you want to point the camera at
        ///		a specific point on it, provide a vector in the 'offset' parameter and the 
        ///		camera's target point will be adjusted.
        /// </remarks>
        /// <param name="enabled">If true, the Camera will track the SceneNode supplied as the next 
        ///		parameter (cannot be null). If false the camera will cease tracking and will
        ///		remain in it's current orientation.
        ///	 </param> 
        /// <param name="target">The SceneNode which this Camera will track. Make sure you don't
        ///		delete this SceneNode before turning off tracking (e.g. SceneManager.ClearScene will
        ///		delete it so be careful of this). Can be null if and only if the enabled param is false.
        ///	</param>
        /// <param name="offset">If supplied, the camera targets this point in local space of the target node
        ///		instead of the origin of the target node. Good for fine tuning the look at point.
        ///	</param>
        public void SetAutoTracking( bool enabled, SceneNode target, Vector3 offset )
        {
            if ( enabled )
            {
                Debug.Assert( target != null, "A camera's auto track target cannot be null." );
                autoTrackTarget = target;
                autoTrackOffset = offset;
            }
            else
            {
                autoTrackTarget = null;
            }
        }

        /// <summary>
        ///		Sets the viewing window inside of viewport.
        /// </summary>
        /// <remarks>
        ///		This method can be used to set a subset of the viewport as the rendering target. 
        /// </remarks>
        /// <param name="left">Relative to Viewport - 0 corresponds to left edge, 1 - to right edge (default - 0).</param>
        /// <param name="top">Relative to Viewport - 0 corresponds to top edge, 1 - to bottom edge (default - 0).</param>
        /// <param name="right">Relative to Viewport - 0 corresponds to left edge, 1 - to right edge (default - 1).</param>
        /// <param name="bottom">Relative to Viewport - 0 corresponds to top edge, 1 - to bottom edge (default - 1).</param>
        public virtual void SetWindow( float left, float top, float right, float bottom )
        {
            windowLeft = left;
            windowTop = top;
            windowRight = right;
            windowBottom = bottom;

            isWindowSet = true;
            recalculateWindow = true;

            InvalidateView();
        }

        /// <summary>
        ///		Do actual window setting, using parameters set in <see cref="SetWindow"/> call.
        /// </summary>
        /// <remarks>The method is called after projection matrix each change.</remarks>
        protected void SetWindowImpl()
        {
            if ( !isWindowSet || !recalculateWindow )
            {
                return;
            }

            float thetaY = MathUtil.DegreesToRadians( fieldOfView * 0.5f );
            float tanThetaY = MathUtil.Tan( thetaY );
            float tanThetaX = tanThetaY * aspectRatio;

            float vpTop = tanThetaY * nearDistance;
            float vpLeft = -tanThetaX * nearDistance;
            float vpWidth = -2 * vpLeft;
            float vpHeight = -2 * vpTop;

            float wvpLeft = vpLeft + windowLeft * vpWidth;
            float wvpRight = vpLeft + windowRight * vpWidth;
            float wvpTop = vpTop - windowTop * vpHeight;
            float wvpBottom = vpTop - windowBottom * vpHeight;

            Vector3 vpUpLeft = new Vector3( wvpLeft, wvpTop, -nearDistance );
            Vector3 vpUpRight = new Vector3( wvpRight, wvpTop, -nearDistance );
            Vector3 vpBottomLeft = new Vector3( wvpLeft, wvpBottom, -nearDistance );
            Vector3 vpBottomRight = new Vector3( wvpRight, wvpBottom, -nearDistance );

            Matrix4 inv = viewMatrix.Inverse();

            Vector3 vwUpLeft = inv * vpUpLeft;
            Vector3 vwUpRight = inv * vpUpRight;
            Vector3 vwBottomLeft = inv * vpBottomLeft;
            Vector3 vwBottomRight = inv * vpBottomRight;

            Vector3 pos = Position;

            windowClipPlanes.Clear();
            windowClipPlanes.Add( new Plane( pos, vwBottomLeft, vwUpLeft ) );
            windowClipPlanes.Add( new Plane( pos, vwUpLeft, vwUpRight ) );
            windowClipPlanes.Add( new Plane( pos, vwUpRight, vwBottomRight ) );
            windowClipPlanes.Add( new Plane( pos, vwBottomRight, vwBottomLeft ) );

            recalculateWindow = false;
        }

        /// <summary>
        ///		Cancel view window.
        /// </summary>
        public virtual void ResetWindow()
        {
            isWindowSet = false;
        }

        /// <summary>
        ///		Gets the window plane at the specified index.
        /// </summary>
        /// <param name="index">Index of the plane to get.</param>
        /// <returns>The window plane at the specified index.</returns>
        public Plane GetWindowPlane( int index )
        {
            Debug.Assert( index < windowClipPlanes.Count, "Window clip plane index out of bounds." );

            // ensure the window is recalced
            SetWindowImpl();

            return (Plane)windowClipPlanes[index];
        }

        /// <summary>
        ///     Gets a world space ray as cast from the camera through a viewport position.
        /// </summary>
        /// <param name="screenX">
        ///     The x position at which the ray should intersect the viewport, 
        ///     in normalised screen coordinates [0,1].
        /// </param>
        /// <param name="screenY">
        ///     The y position at which the ray should intersect the viewport, 
        ///     in normalised screen coordinates [0,1].
        /// </param>
        /// <returns></returns>
        public Ray GetCameraToViewportRay( float screenX, float screenY )
        {
            float centeredScreenX = ( screenX - 0.5f );
            float centeredScreenY = ( 0.5f - screenY );

            float normalizedSlope = MathUtil.Tan( MathUtil.DegreesToRadians( fieldOfView * 0.5f ) );
            float viewportYToWorldY = normalizedSlope * nearDistance * 2;
            float viewportXToWorldX = viewportYToWorldY * aspectRatio;

            Vector3 rayDirection =
                new Vector3(
                centeredScreenX * viewportXToWorldX,
                centeredScreenY * viewportYToWorldY,
                -nearDistance );

            rayDirection = this.DerivedOrientation * rayDirection;
            rayDirection.Normalize();

            return new Ray( this.DerivedPosition, rayDirection );
        }

        /// <summary>
        ///		Notifies this camera that a viewport is using it.
        /// </summary>
        /// <param name="viewport">Viewport that is using this camera.</param>
        public void NotifyViewport( Viewport viewport )
        {
            lastViewport = viewport;
        }

        #endregion

        #region Internal engine methods

        /// <summary>
        ///		Called to ask a camera to render the scene into the given viewport.
        /// </summary>
        /// <param name="viewport"></param>
        /// <param name="showOverlays"></param>
        internal void RenderScene( Viewport viewport, bool showOverlays )
        {
            sceneManager.RenderScene( this, viewport, showOverlays );
        }

        /// <summary>
        ///		Updates an auto-tracking camera.
        /// </summary>
        internal void AutoTrack()
        {
            // assumes all scene nodes have been updated
            if ( autoTrackTarget != null )
            {
                LookAt( autoTrackTarget.DerivedPosition + autoTrackOffset );
            }
        }

        #endregion
    }
}
