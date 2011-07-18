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
#endregion LGPL License

#region SVN Version Information
// <file>
//     <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Axiom.Graphics;
using Axiom.Math;

#endregion Namespace Declarations

namespace Axiom.Core
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
        // Not implemented/missing
        // SetPosition(x,y,z) as Position is exposed as Vector3
        // SetDirection(x,y,z) as Direction is exposed as Vector3
        // LookAt(x,y,z) we might expose LookAt as setter property
        // string MovableType (not implemented in base, yet?)
        // IsVisible overrides from Frustum
        // getWorldSpaceCorners override from Frustum
        // getFrustumPlane override from Frustum
        // getNearClipDistance override from Frustum
        // getFarClipDistance override from Frustum
        //
        // the current impl shadows some props above that were overriden in ogre
        // this is most likeley a bug!
        //
        // Differences:
        // FixedYawAxis is exposed as Vector3 whereas
        // Ogre exposes it together with a bool (isYawFixed)
        // we might want to expose both rather than implicitly set
        // the isYawFixed

        #region events

        /// <summary>
        ///    Delegate for Viewport update events.
        /// </summary>
        public delegate void CameraEventHandler(CameraEventArgs e);

        /// <summary>
        ///    Event arguments for render target updates.
        /// </summary>
        public class CameraEventArgs : EventArgs
        {
            public Camera Source { get; internal set; }

            public CameraEventArgs(Camera source)
            {
                Source = source;
            }
        }

        /// <summary>
        /// Called prior to the scene being rendered with this camera
        /// </summary>
        [OgreVersion(1, 7, 2790, "Merged from Listener subclass")]
        public event CameraEventHandler CameraPreRenderScene;

        /// <summary>
        /// Called after the scene has been rendered with this camera
        /// </summary>
        [OgreVersion(1, 7, 2790, "Merged from Listener subclass")]
        public event CameraEventHandler CameraPostRenderScene;

        /// <summary>
        /// Called when the camera is being destroyed
        /// </summary>
        [OgreVersion(1, 7, 2790, "Merged from Listener subclass")]
        public event CameraEventHandler CameraDestroyed;

        #endregion

        #region Fields and Properties

        #region SceneManager Property


        /// <summary>
        ///    Returns the current SceneManager that this camera is using.
        /// </summary>
        [OgreVersion(1, 7, 2790)]
        public SceneManager SceneManager { get; protected set; }

        #endregion SceneManager Property

        #region Orientation Property

        /// <summary>
        ///		Camera orientation.
        /// </summary>
        [OgreVersion(1, 7, 2790)]
        protected Quaternion orientation;
        /// <summary>
        ///     Gets/Sets the camera's orientation.
        /// </summary>
        [OgreVersion(1, 7, 2790)]
        public Quaternion Orientation
        {
            get
            {
                return orientation;
            }
            set
            {
                orientation = value;
                orientation.Normalize();
                InvalidateView();
            }
        }

        #endregion Orientation Property

        #region Position Property

        /// <summary>
        ///		Camera position.
        /// </summary>
        [OgreVersion(1, 7, 2790)]
        protected Vector3 position;
        /// <summary>
        ///     Gets/Sets the camera's position.
        /// </summary>
        [OgreVersion(1, 7, 2790)]
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

        #endregion Position Property

        #region Direction Property

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
                // Do nothing if given a zero vector
                // (Replaced assert since this could happen with auto tracking camera and
                //  camera passes through the lookAt point)
                // should rather be if (value.LengthSquared <= someSmallEpsilon)
                if (value == Vector3.Zero)
                    return;

                // Remember, camera points down -Z of local axes!
                // Therefore reverse direction of direction vector before determining local Z
                var zAdjustVector = -value;
                zAdjustVector.Normalize();

                Quaternion targetWorldOrientation;
                if (isYawFixed)
                {
                    var xVector = yawFixedAxis.Cross(zAdjustVector);
                    xVector.Normalize();

                    var yVector = zAdjustVector.Cross(xVector);
                    yVector.Normalize();

                    targetWorldOrientation = Quaternion.FromAxes(xVector, yVector, zAdjustVector);
                }
                else
                {
                    // Get axes from current quaternion
                    Vector3 xAxis, yAxis, zAxis;

                    UpdateView();

                    // get the vector components of the derived orientation vector
                    realOrientation.ToAxes(out xAxis, out yAxis, out zAxis);

                    Quaternion rotationQuat;

                    if ((zAxis + zAdjustVector).LengthSquared < 0.00005f)
                    {
                        // Oops, a 180 degree turn (infinite possible rotation axes)
                        // Default to yaw i.e. use current UP
                        rotationQuat = Quaternion.FromAngleAxis(Utility.PI, yAxis);
                    }
                    else
                    {
                        // Derive shortest arc to new direction
                        rotationQuat = zAxis.GetRotationTo(zAdjustVector);
                    }

                    targetWorldOrientation = rotationQuat * orientation;
                }

                // transform to parent space
                if (parentNode != null)
                {
                    orientation = parentNode.DerivedOrientation.Inverse() * targetWorldOrientation;
                }
                else
                {
                    orientation = targetWorldOrientation;
                }

                // TODO: If we have a fixed yaw axis, we musn't break it by using the
                // shortest arc because this will sometimes cause a relative yaw
                // which will tip the camera

                InvalidateView();
            }
        }

        #endregion Direction Property

        #region Up Property

        /// <summary>
        ///		Gets camera's 'up' vector.
        /// </summary>
        public Vector3 Up
        {
            get
            {
                return Orientation * Vector3.UnitY;
            }
        }

        #endregion Up Property

        #region Right Property

        /// <summary>
        ///		Gets camera's 'right' vector.
        /// </summary>
        public Vector3 Right
        {
            get
            {
                return Orientation * Vector3.UnitX;
            }
        }

        #endregion Right Property

        #region DerivedOrientation Property

        /// <summary>
        ///		Orientation derived from parent.
        /// </summary>
        [OgreVersion(1, 7, 2790)]
        protected Quaternion derivedOrientation;
        /// <summary>
        ///	Gets the derived orientation of the camera, including any
        /// rotation inherited from a node attachment and reflection matrix.
        /// </summary>
        [OgreVersion(1, 7, 2790)]
        public Quaternion DerivedOrientation
        {
            get
            {
                UpdateView();
                return derivedOrientation;
            }
        }

        #endregion DerivedOrientation Property

        #region DerivedPosition Property

        /// <summary>
        ///		Position derived from parent.
        /// </summary>
        [OgreVersion(1, 7, 2790)]
        protected Vector3 derivedPosition;
        /// <summary>
        ///	Gets the derived position of the camera, including any
        /// rotation inherited from a node attachment and reflection matrix.
        /// </summary>
        [OgreVersion(1, 7, 2790)]
        public Vector3 DerivedPosition
        {
            get
            {
                UpdateView();
                return derivedPosition;
            }
        }

        #endregion DerivedPosition Property

        #region DerivedDirection Property

        /// <summary>
        ///	Gets the derived direction of the camera, including any
        /// rotation inherited from a node attachment and reflection matrix.
        /// </summary>
        [OgreVersion(1, 7, 2790)]
        public Vector3 DerivedDirection
        {
            get
            {
                UpdateView();

                // RH coords, direction points down -Z by default
                return derivedOrientation * -Vector3.UnitZ;
            }
        }

        #endregion DerivedDirection Property

        #region DerivedUp Property

        /// <summary>
        ///	Gets the derived up vector of the camera, including any
        /// rotation inherited from a node attachment and reflection matrix.
        /// </summary>
        [OgreVersion(1, 7, 2790)]
        public Vector3 DerivedUp
        {
            get
            {
                UpdateView();
                return derivedOrientation * Vector3.UnitY;
            }
        }

        #endregion DerivedUp Property

        #region DerivedRight Property

        /// <summary>
        ///	Gets the derived right vector of the camera, including any
        /// rotation inherited from a node attachment and reflection matrix.
        /// </summary>
        [OgreVersion(1, 7, 2790)]
        public Vector3 DerivedRight
        {
            get
            {
                UpdateView();
                return derivedOrientation * Vector3.UnitX;
            }
        }

        #endregion DerivedRight Property

        #region RealOrientation Property

        /// <summary>
        ///		Real world orientation of the camera.
        /// </summary>
        [OgreVersion(1, 7, 2790)]
        protected Quaternion realOrientation;
        /// <summary>
        /// Gets the real world orientation of the camera, including any
        /// rotation inherited from a node attachment
        /// </summary>
        [OgreVersion(1, 7, 2790)]
        public Quaternion RealOrientation
        {
            get
            {
                UpdateView();
                return realOrientation;
            }
        }

        #endregion RealOrientation Property

        #region RealPosition Property

        /// <summary>
        ///		Real world position of the camera.
        /// </summary>
        [OgreVersion(1, 7, 2790)]
        protected Vector3 realPosition;
        /// <summary>
        /// Gets the real world orientation of the camera, including any
        /// rotation inherited from a node attachment
        /// </summary>
        [OgreVersion(1, 7, 2790)]
        public Vector3 RealPosition
        {
            get
            {
                UpdateView();
                return realPosition;
            }
        }

        #endregion RealPosition Property

        #region RealDirection Property

        /// <summary>
        ///	Gets the derived direction of the camera, including any
        /// rotation inherited from a node attachment.
        /// </summary>
        [OgreVersion(1, 7, 2790)]
        public Vector3 RealDirection
        {
            get
            {
                UpdateView();

                // RH coords, direction points down -Z by default
                return realOrientation * -Vector3.UnitZ;
            }
        }

        #endregion RealDirection Property

        #region RealUp Property

        /// <summary>
        ///	Gets the derived up vector of the camera, including any
        /// rotation inherited from a node attachment.
        /// </summary>
        [OgreVersion(1, 7, 2790)]
        public Vector3 RealUp
        {
            get
            {
                UpdateView();
                return realOrientation * Vector3.UnitY;
            }
        }

        #endregion RealUp Property

        #region RealRight Property

        /// <summary>
        ///	Gets the derived right vector of the camera, including any
        /// rotation inherited from a node attachment.
        /// </summary>
        [OgreVersion(1, 7, 2790)]
        public Vector3 RealRight
        {
            get
            {
                UpdateView();
                return realOrientation * Vector3.UnitX;
            }
        }

        #endregion RealRight Property

        #region FixedYawAxis Property

        /// <summary>
        ///		Whether to yaw around a fixed axis.
        /// </summary>
        protected bool isYawFixed;
        /// <summary>
        ///		Fixed axis to yaw around.
        /// </summary>
        protected Vector3 yawFixedAxis;
        /// <summary>
        /// Tells the camera whether to yaw around it's own local Y axis or a fixed axis of choice.
        /// </summary>
        /// <remarks>
        /// This property allows you to change the yaw behaviour of the camera
        /// - by default, the camera yaws around a fixed Y axis. This is
        /// often what you want - for example if you're making a first-person
        /// shooter, you really don't want the yaw axis to reflect the local
        /// camera Y, because this would mean a different yaw axis if the
        /// player is looking upwards rather than when they are looking
        /// straight ahead. You can change this behaviour by setting this
        /// property to <seealso cref="Vector3.Zero"/>, which you will want to do if you are making a completely
        /// free camera like the kind used in a flight simulator.
        /// </remarks>
        public Vector3 FixedYawAxis
        {
            get
            {
                return yawFixedAxis;
            }
            set
            {
                yawFixedAxis = value;

                if (yawFixedAxis != Vector3.Zero)
                {
                    isYawFixed = true;
                }
                else
                {
                    isYawFixed = false;
                }
            }
        }

        #endregion FixedYawAxis Property

        #region PolygonMode Property

        /// <summary>
        ///		Sets the level of rendering detail required from this camera.
        /// </summary>
        /// <remarks>
        ///		Each camera is set to render at full detail by default, that is
        ///		with full texturing, lighting etc. This method lets you change
        ///		that behavior, allowing you to make the camera just render a
        ///		wireframe view, for example.
        /// </remarks>
        [OgreVersion(1, 7, 2790)]
        public PolygonMode PolygonMode { get; set; }

        #endregion PolygonMode Property

        #region RenderedFaceCount Property

        /// <summary>
        /// Gets the last count of triangles visible in the view of this camera.
        /// </summary>
        [OgreVersion(1, 7, 2790, "NumRenderedFaces in Ogre")]
        public int RenderedFaceCount { get; protected set; }

        #endregion RenderedFaceCount Property

        #region RenderedBatchCount Property


        /// <summary>
        /// Gets the last count of batches visible in the view of this camera.
        /// </summary>
        [OgreVersion(1, 7, 2790, "NumRenderedBatches in Ogre")]
        public int RenderedBatchCount { get; protected set; }

        #endregion RenderedBatchCount Property

        #region AutoTrackingTarget Property

        /// <summary>
        ///		SceneNode which this Camera will automatically track.
        /// </summary>
        public SceneNode AutoTrackingTarget { get; set; }

        #endregion AutoTrackingTarget Property

        #region AutoTrackingOffset Property

        /// <summary>
        ///		Tracking offset for fine tuning.
        /// </summary>
        public Vector3 AutoTrackingOffset { get; protected set; }


        #endregion AutoTrackingOffset Property

        #region LodBias Property

        /// <summary>
        ///		Scene LOD factor used to adjust overall LOD.
        /// </summary>
        [OgreVersion(1, 7, 2790)]
        protected Real sceneLodFactor;
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
        [OgreVersion(1, 7, 2790, "default = 1.0")]
        public Real LodBias
        {
            get
            {
                return sceneLodFactor;
            }
            set
            {
                Debug.Assert(value > 0.0f, "Lod bias must be greater than 0");
                sceneLodFactor = value;
                invSceneLodFactor = 1.0f / sceneLodFactor;
            }
        }

        #endregion LodBias Property

        #region LodCamera Property

        [OgreVersion(1, 7, 2790)]
        private Camera _lodCamera;
        /// <summary>
        /// Get/Sets a reference to the Camera which should be used to determine LOD settings.
        /// </summary>
        /// <remarks>
        /// Sometimes you don't want the LOD of a render to be based on the camera
        /// that's doing the rendering, you want it to be based on a different
        /// camera. A good example is when rendering shadow maps, since they will
        /// be viewed from the perspective of another camera. Therefore this method
        /// lets you associate a different camera instance to use to determine the LOD.
        /// <para />
        /// To revert the camera to determining LOD based on itself, set this property with
        /// a reference to itself.
        /// </remarks>
        [OgreVersion(1, 7, 2790)]
        public Camera LodCamera
        {
            get
            {
                return _lodCamera ?? this;
            }
            set
            {
                _lodCamera = this == value ? null : value;
            }
        }

        #endregion LodCamera Property

        #region InverseLodBias Property

        /// <summary>
        ///		Inverted scene LOD factor, can be used by Renderables to adjust their LOD.
        /// </summary>
        [OgreVersion(1, 7, 2790)]
        protected float invSceneLodFactor;
        /// <summary>
        ///     Used for internal Lod calculations.
        /// </summary>
        [OgreVersion(1, 7, 2790, "LodBiasInverse in Ogre")]
        public float InverseLodBias
        {
            get
            {
                return invSceneLodFactor;
            }
        }

        #endregion InverseLodBias Property

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

        #region IsWindowSet Property

        /// <summary>
        ///		Is viewing window used.
        /// </summary>
        [OgreVersion(1, 7, 2790)]
        protected bool isWindowSet;
        /// <summary>
        ///		Gets the flag specifying if a viewport window is being used.
        /// </summary>
        [OgreVersion(1, 7, 2790)]
        public virtual bool IsWindowSet
        {
            get
            {
                return isWindowSet;
            }
        }

        #endregion IsWindowSet Property

        #region WindowPlanes Property

        /// <summary>
        /// Windowed viewport clip planes.
        /// </summary>
        [OgreVersion(1, 7, 2790)]
        protected List<Plane> windowClipPlanes = new List<Plane>();
        /// <summary>
        ///  Gets the window clip planes, only applicable if isWindowSet == true
        /// </summary>
        [OgreVersion(1, 7, 2790)]
        public IList<Plane> WindowPlanes
        {
            get
            {
                UpdateView();
                SetWindowImpl();
                return windowClipPlanes;
            }
        }

        #endregion WindowPlanes Property

        /// <summary>
        ///		Was viewing window changed?
        /// </summary>
        protected bool recalculateWindow;

        #region Viewport Property

        /// <summary>
        ///		Get the last viewport which was attached to this camera.
        /// </summary>
        /// <remarks>
        ///		This is not guaranteed to be the only viewport which is
        ///		using this camera, just the last once which was created referring
        ///		to it.
        /// </remarks>
        [OgreVersion(1, 7, 2790)]
        public Viewport Viewport { get; protected set; }

        #endregion Viewport Property

        #region AutoAspectRatio Property

        /// <summary>
        ///		If set to true a viewport that owns this frustum will be able to
        ///		recalculate the aspect ratio whenever the frustum is resized.
        /// </summary>
        /// <remarks>
        ///		You should set this to true only if the frustum / camera is used by
        ///		one viewport at the same time. Otherwise the aspect ratio for other
        ///		viewports may be wrong.
        /// </remarks>
        [OgreVersion(1, 7, 2790)]
        public bool AutoAspectRatio { get; set; }

        #endregion AutoAspectRatio Property

        #region UseRenderingQueue Property

        /// <summary>
        ///     Whether or not the rendering distance of objects should take effect for this camera
        /// </summary>
        protected bool useRenderingDistance;
        /// <summary>
        ///		Whether or not the rendering distance of objects should take effect for this camera
        /// </summary>
        public bool UseRenderingDistance
        {
            get
            {
                return useRenderingDistance;
            }
            set
            {
                useRenderingDistance = value;
            }
        }

        #endregion UseRenderingQueue Property

        /// <summary>
        ///     Tells the camera to use a separate Frustum instance to perform culling.
        /// </summary>
        /// <remarks>
        /// 	By calling this method, you can tell the camera to perform culling
        /// 	against a different frustum to it's own. This is mostly useful for
        /// 	debug cameras that allow you to show the culling behaviour of another
        ///	    camera, or a manual frustum instance.
        ///     This can either be a manual Frustum instance (which you can attach
        ///     to scene nodes like any other MovableObject), or another camera.
        ///     If you pass null to this property it reverts the camera to normal behaviour.
        /// </remarks>
        [OgreVersion(1, 7, 2790, "CullingFrustum in Ogre")]
        public Frustum CullFrustum { get; set; }

        #endregion Fields and Properties

        #region Constructors

        [OgreVersion(1, 7, 2790)]
        public Camera(string name, SceneManager sceneManager)
            : base(name)
        {
            // Record name & SceneManager
            SceneManager = sceneManager;

            // Init camera location & direction

            // Point down -Z axis
            orientation = Quaternion.Identity;
            position = Vector3.Zero;

            // Reasonable defaults to camera params
            PolygonMode = PolygonMode.Solid;

            // Init no tracking
            AutoTrackingTarget = null;
            AutoTrackingOffset = Vector3.Zero;

            // default these to 1 so Lod default to normal
            sceneLodFactor = invSceneLodFactor = 1.0f;

            useRenderingDistance = true;


            FieldOfView = (float)System.Math.PI / 4.0f;
            Near = 100.0f;
            Far = 100000.0f;
            AspectRatio = 1.33333333333333f;
            ProjectionType = Projection.Perspective;

            //SetFixedYawAxis( true );
            FixedYawAxis = Vector3.UnitY; // Axiom specific

            derivedOrientation = Quaternion.Identity;

            InvalidateFrustum();
            InvalidateView();

            ViewMatrix = Matrix4.Zero;
            ProjectionMatrix = Matrix4.Zero;

            parentNode = null;

            isReflected = false;
            isVisible = false;
        }

        #endregion Constructors

        #region Frustum Members

        /// <summary>
        ///		Get the derived orientation of this frustum.
        /// </summary>
        /// <returns></returns>
        protected override Quaternion GetOrientationForViewUpdate()
        {
            return realOrientation;
        }

        /// <summary>
        ///		Get the derived position of this frustum.
        /// </summary>
        /// <returns></returns>
        protected override Vector3 GetPositionForViewUpdate()
        {
            return realPosition;
        }

        /// <summary>
        ///		Signal to update view information.
        /// </summary>
        protected override void InvalidateView()
        {
            recalculateWindow = true;
            base.InvalidateView();
        }

        /// <summary>
        ///		Signal to update frustum information.
        /// </summary>
        protected override void InvalidateFrustum()
        {
            _recalculateFrustum = true;
            base.InvalidateFrustum();
        }

        /// <summary>
        ///		Evaluates whether or not the view matrix is out of date.
        /// </summary>
        /// <returns></returns>
        protected override bool IsViewOutOfDate
        {
            get
            {
                // Overridden from Frustum to use local orientation / position offsets
                // are we attached to another node?
                if (parentNode != null)
                {
                    if (_recalculateView ||
                        parentNode.DerivedOrientation != _lastParentOrientation ||
                        parentNode.DerivedPosition != _lastParentPosition)
                    {
                        // we are out of date with the parent scene node
                        _lastParentOrientation = parentNode.DerivedOrientation;
                        _lastParentPosition = parentNode.DerivedPosition;
                        realOrientation = _lastParentOrientation * orientation;
                        realPosition = (_lastParentOrientation * position) + _lastParentPosition;
                        _recalculateView = true;
                        recalculateWindow = true;
                    }
                }
                else
                {
                    // rely on own updates
                    realOrientation = orientation;
                    realPosition = position;
                }

                if (IsReflected &&
                    linkedReflectionPlane != null &&
                    lastLinkedReflectionPlane != linkedReflectionPlane.DerivedPlane)
                {

                    ReflectionPlane = linkedReflectionPlane.DerivedPlane;
                    ReflectionMatrix = Utility.BuildReflectionMatrix(ReflectionPlane);
                    lastLinkedReflectionPlane = linkedReflectionPlane.DerivedPlane;
                    _recalculateView = true;
                    recalculateWindow = true;
                }

                // Deriving reflected orientation / position
                if (_recalculateView)
                {
                    if (IsReflected)
                    {
                        // Calculate reflected orientation, use up-vector as fallback axis.
                        Vector3 dir = realOrientation * Vector3.NegativeUnitZ;
                        Vector3 rdir = dir.Reflect(ReflectionPlane.Normal);
                        Vector3 up = realOrientation * Vector3.UnitY;
                        derivedOrientation = dir.GetRotationTo(rdir, up) * realOrientation;

                        // Calculate reflected position.
                        derivedPosition = ReflectionMatrix.TransformAffine(realPosition);
                    }
                    else
                    {
                        derivedOrientation = realOrientation;
                        derivedPosition = realPosition;
                    }
                }

                return _recalculateView;
            }
        }

        #region Custom Frustum culling implementation

        new public bool IsObjectVisible(AxisAlignedBox box)
        {
            if (null != CullFrustum)
            {
                return CullFrustum.IsObjectVisible(box);
            }
            else
            {
                return base.IsObjectVisible(box);
            }
        }

        new public bool IsObjectVisible(AxisAlignedBox box, out FrustumPlane culledBy)
        {
            if (null != CullFrustum)
            {
                return CullFrustum.IsObjectVisible(box, out culledBy);
            }
            else
            {
                return base.IsObjectVisible(box, out culledBy);
            }
        }

        new public bool IsObjectVisible(Sphere sphere)
        {
            if (null != CullFrustum)
            {
                return CullFrustum.IsObjectVisible(sphere);
            }
            else
            {
                return base.IsObjectVisible(sphere);
            }
        }

        new public bool IsObjectVisible(Sphere sphere, out FrustumPlane culledBy)
        {
            if (null != CullFrustum)
            {
                return CullFrustum.IsObjectVisible(sphere, out culledBy);
            }
            else
            {
                return base.IsObjectVisible(sphere, out culledBy);
            }
        }

        new public bool IsObjectVisible(Vector3 vertex)
        {
            if (null != CullFrustum)
            {
                return CullFrustum.IsObjectVisible(vertex);
            }
            else
            {
                return base.IsObjectVisible(vertex);
            }
        }

        new public bool IsObjectVisible(Vector3 vertex, out FrustumPlane culledBy)
        {
            if (null != CullFrustum)
            {
                return CullFrustum.IsObjectVisible(vertex, out culledBy);
            }
            else
            {
                return base.IsObjectVisible(vertex, out culledBy);
            }
        }

        new public Vector3[] WorldSpaceCorners
        {
            get
            {
                return null != CullFrustum ? CullFrustum.WorldSpaceCorners : base.WorldSpaceCorners;
            }
        }

        new public Plane[] FrustumPlanes
        {
            get
            {
                UpdateFrustumPlanes();
                if (null != CullFrustum)
                {
                    return CullFrustum.FrustumPlanes;
                }
                else
                {
                    return base.FrustumPlanes;
                }
            }
        }

        [OgreVersion(1, 7, 2790)]
        public override bool ProjectSphere(Sphere sphere, out float left, out float top, out float right, out float bottom)
        {
            if (null != CullFrustum)
            {
                return CullFrustum.ProjectSphere(sphere, out left, out top, out right, out bottom);
            }
            //else
            {
                return base.ProjectSphere(sphere, out left, out top, out right, out bottom);
            }
        }

        public override float Near
        {
            get
            {
                if (null != CullFrustum)
                {
                    return CullFrustum.Near;
                }
                else
                {
                    return base.Near;
                }
            }
            set
            {
                if (null != CullFrustum)
                {
                    CullFrustum.Near = value;
                }
                else
                {
                    base.Near = value;
                }
            }
        }

        public override float Far
        {
            get
            {
                if (null != CullFrustum)
                {
                    return CullFrustum.Near;
                }
                else
                {
                    return base.Far;
                }
            }
            set
            {
                if (null != CullFrustum)
                {
                    CullFrustum.Far = value;
                }
                else
                {
                    base.Far = value;
                }
            }
        }

        public override Matrix4 ViewMatrix
        {
            get
            {
                if (null != CullFrustum)
                {
                    return CullFrustum.ViewMatrix;
                }
                else
                {
                    return base.ViewMatrix;
                }
            }
            set
            {
                if (null != CullFrustum)
                {
                    CullFrustum.ViewMatrix = value;
                }
                else
                {
                    base.ViewMatrix = value;
                }
            }
        }

        /// <summary>
        /// Returns the ViewMatrix for the underlying Frustum only
        /// </summary>
        public Matrix4 FrustumViewMatrix
        {
            get
            {
                return base.ViewMatrix;
            }
        }

        #endregion Custom Frustum culling implementation

        #endregion Frustum Members

        #region SceneObject Implementation

        public override void UpdateRenderQueue(RenderQueue queue)
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
        [OgreVersion(1, 7, 2790)]
        public override float BoundingRadius
        {
            get
            {
                // return a little bigger than the near distance
                // just to keep things just outside
                return Near * 1.5f;
            }
        }

        public override void NotifyCurrentCamera(Axiom.Core.Camera camera)
        {
            // Do nothing
        }

        /// <summary>
        ///    Called by the scene manager to let this camera know how many faces were rendered within
        ///    view of this camera every frame.
        /// </summary>
        [OgreVersion(1, 7, 2790)]
        internal void NotifyRenderedFaces(int renderedFaceCount)
        {
            RenderedFaceCount = renderedFaceCount;
        }

        /// <summary>
        ///    Called by the scene manager to let this camera know how many batches were rendered within
        ///    view of this camera every frame.
        /// </summary>
        [OgreVersion(1, 7, 2790)]
        internal void NotifyRenderedBatches(int renderedBatchCount)
        {
            RenderedBatchCount = renderedBatchCount;
        }
        #endregion SceneObject Implementation

        #region Public methods

        /// <summary>
        /// Moves the camera's position by the vector offset provided along world axes.
        /// </summary>
        /// <param name="offset"></param>
        [OgreVersion(1, 7, 2790)]
        public void Move(Vector3 offset)
        {
            position = position + offset;
            InvalidateView();
        }

        /// <summary>
        /// Moves the camera's position by the vector offset provided along it's own axes (relative to orientation).
        /// </summary>
        /// <param name="offset"></param>
        [OgreVersion(1, 7, 2790)]
        public void MoveRelative(Vector3 offset)
        {
            // Transform the axes of the relative vector by camera's local axes
            var transform = orientation * offset;

            position += transform;
            InvalidateView();
        }

        /// <summary>
        ///		Specifies a target that the camera should look at.
        /// </summary>
        /// <param name="target"></param>
        public void LookAt(Vector3 target)
        {
            UpdateView();
            Direction = (target - realPosition);
        }

        /// <summary>
        ///		Pitches the camera up/down counter-clockwise around it's local x axis.
        /// </summary>
        /// <param name="degrees"></param>
        [OgreVersion(1, 7, 2790)]
        public void Pitch(float degrees)
        {
            var xAxis = orientation * Vector3.UnitX;
            Rotate(xAxis, degrees);

            InvalidateView();
        }

        /// <summary>
        ///		Rolls the camera counter-clockwise, in degrees, around its local y axis.
        /// </summary>
        /// <param name="degrees"></param>
        [OgreVersion(1, 7, 2790)]
        public void Yaw(float degrees)
        {
            Vector3 yAxis;

            if (isYawFixed)
            {
                // Rotate around fixed yaw axis
                yAxis = yawFixedAxis;
            }
            else
            {
                // Rotate around local Y axis
                yAxis = orientation * Vector3.UnitY;
            }

            Rotate(yAxis, degrees);

            InvalidateView();
        }

        /// <summary>
        ///		Rolls the camera counter-clockwise, in degrees, around its local z axis.
        /// </summary>
        /// <param name="degrees"></param>
        [OgreVersion(1, 7, 2790)]
        public void Roll(float degrees)
        {
            // Rotate around local Z axis
            var zAxis = orientation * Vector3.UnitZ;
            Rotate(zAxis, degrees);

            InvalidateView();
        }

        /// <summary>
        ///		Rotates the camera about an arbitrary axis.
        /// </summary>
        /// <param name="qnorm"></param>
        [OgreVersion(1, 7, 2790)]
        public void Rotate(Quaternion qnorm)
        {
            // Note the order of the mult, i.e. q comes after

            // Normalise the quat to avoid cumulative problems with precision
            qnorm.Normalize();
            orientation = qnorm * orientation;

            InvalidateView();
        }

        /// <summary>
        ///		Rotates the camera about an arbitrary axis.
        /// </summary>
        /// <param name="axis"></param>
        /// <param name="degrees"></param>
        [OgreVersion(1, 7, 2790)]
        public void Rotate(Vector3 axis, float degrees)
        {
            var q = Quaternion.FromAngleAxis(
                    Utility.DegreesToRadians((Real)degrees),
                    axis);
            Rotate(q);
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
        [AxiomHelper(0, 8, "default overload")]
        public void SetAutoTracking(bool enabled, MovableObject target)
        {
            SetAutoTracking(enabled, (SceneNode)target.ParentNode, Vector3.Zero);
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
        [AxiomHelper(0, 8, "default overload")]
        public void SetAutoTracking(bool enabled, SceneNode target)
        {
            SetAutoTracking(enabled, target, Vector3.Zero);
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
        [OgreVersion(1, 7, 2790)]
        public void SetAutoTracking(bool enabled, SceneNode target, Vector3 offset)
        {
            if (enabled)
            {
                Debug.Assert(target != null, "A camera's auto track target cannot be null.");
                AutoTrackingTarget = target;
                AutoTrackingOffset = offset;
            }
            else
            {
                AutoTrackingTarget = null;
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
        [OgreVersion(1, 7, 2790)]
        public virtual void SetWindow(float left, float top, float right, float bottom)
        {
            windowLeft = left;
            windowTop = top;
            windowRight = right;
            windowBottom = bottom;

            isWindowSet = true;
            recalculateWindow = true;
        }

        //-----------------------------------------------------------------------
        //_______________________________________________________
        //|														|
        //|	getRayForwardIntersect								|
        //|	-----------------------------						|
        //|	get the intersections of frustum rays with a plane	|
        //| of interest.  The plane is assumed to have constant	|
        //| z.  If this is not the case, rays					|
        //| should be rotated beforehand to work in a			|
        //| coordinate system in which this is true.			|
        //|_____________________________________________________|
        //
        /// <summary>
        /// Helper function for forwardIntersect that intersects rays with canonical plane
        /// </summary>
        [OgreVersion(1, 7, 2790)]
        protected virtual IEnumerable<Vector4> GetRayForwardIntersect(Vector3 anchor, IEnumerable<Vector3> dir, Real planeOffset)
        {

            if (dir == null)
                yield break;

            var vec = new Vector3[4];
            var infpt = new[] { 0, 0, 0, 0 }; // 0=finite, 1=infinite, 2=straddles infinity


            // find how much the anchor point must be displaced in the plane's
            // constant variable
            var delta = planeOffset - anchor.z;

            // now set the intersection point and note whether it is a 
            // point at infinity or straddles infinity
            var edir = dir.GetEnumerator();
            for (var i = 0; i < 4; i++)
            {
                edir.MoveNext();
                var cur = edir.Current;
                var test = cur.z * delta;
                if (test == 0.0)
                {
                    vec[i] = cur;
                    infpt[i] = 1;
                }
                else
                {
                    var lambda = delta / cur.z;
                    vec[i] = anchor + (lambda * cur);
                    if (test < 0.0)
                        infpt[i] = 2;
                }
            }

            for (var i = 0; i < 4; i++)
            {
                var v = vec[i];
                // store the finite intersection points
                if (infpt[i] == 0)
                    yield return new Vector4(v.x, v.y, v.z, 1.0);
                else
                {
                    // handle the infinite points of intersection;
                    // cases split up into the possible frustum planes 
                    // pieces which may contain a finite intersection point
                    var nextind = (i + 1) % 4;
                    var prevind = (i + 3) % 4;
                    if ((infpt[prevind] == 0) || (infpt[nextind] == 0))
                    {
                        if (infpt[i] == 1)
                            yield return new Vector4(v.x, v.y, v.z, 0.0);
                        else
                        {
                            // handle the intersection points that straddle infinity (back-project)
                            if (infpt[prevind] == 0)
                            {
                                var temp = vec[prevind] - vec[i];
                                yield return new Vector4(temp.x, temp.y, temp.z, 0.0);
                            }
                            if (infpt[nextind] == 0)
                            {
                                var temp = vec[nextind] - vec[i];
                                yield return new Vector4(temp.x, temp.y, temp.z, 0.0);
                            }
                        }
                    } // end if we need to add an intersection point to the list
                } // end if infinite point needs to be considered
            } // end loop over frustun corners

            // we end up with either 0, 3, 4, or 5 intersection points

            //return res;
        }


        //_______________________________________________________
        //|														|
        //|	forwardIntersect									|
        //|	-----------------------------						|
        //|	Forward intersect the camera's frustum rays with	|
        //| a specified plane of interest.						|
        //| Note that if the frustum rays shoot out and would	|
        //| back project onto the plane, this means the forward	|
        //| intersection of the frustum would occur at the		|
        //| line at infinity.									|
        //|_____________________________________________________|
        //
        /// <summary>
        /// Forward projects frustum rays to find forward intersection with plane.
        /// </summary>
        /// <remarks>
        /// Forward projection may lead to intersections at infinity.
        /// </remarks>
        [OgreVersion(1, 7, 2790)]
        public virtual void ForwardIntersect(Plane worldPlane, IList<Vector4> intersect3D)
        {
            if (intersect3D == null)
                return;

            var trCorner = WorldSpaceCorners[0];
            var tlCorner = WorldSpaceCorners[1];
            var blCorner = WorldSpaceCorners[2];
            var brCorner = WorldSpaceCorners[3];

            // need some sort of rotation that will bring the plane normal to the z axis
            var pval = worldPlane;
            if (pval.Normal.z < 0.0)
            {
                pval.Normal *= -1.0;
                pval.D *= -1.0;
            }
            var invPlaneRot = pval.Normal.GetRotationTo(Vector3.UnitZ);

            var vec = new Vector3[4];

            // get rotated light
            var lPos = invPlaneRot * DerivedPosition;
            vec[0] = invPlaneRot * trCorner - lPos;
            vec[1] = invPlaneRot * tlCorner - lPos;
            vec[2] = invPlaneRot * blCorner - lPos;
            vec[3] = invPlaneRot * brCorner - lPos;

            var iPnt = GetRayForwardIntersect(lPos, vec, -pval.D);

            // return wanted data
            //if (intersect3D != null) // cant be null
            {
                var planeRot = invPlaneRot.Inverse();
                intersect3D.Clear();
                foreach (var v in iPnt)
                {
                    var intersection = planeRot * new Vector3(v.x, v.y, v.z);
                    intersect3D.Add(new Vector4(intersection.x, intersection.y, intersection.z, v.w));
                }
            }
        }

        /// <summary>
        ///		Do actual window setting, using parameters set in <see cref="SetWindow"/> call.
        /// </summary>
        /// <remarks>The method is called after projection matrix each change.</remarks>
        protected void SetWindowImpl()
        {
            if (!isWindowSet || !recalculateWindow)
            {
                return;
            }

            // Calculate general projection parameters
            Real vpLeft, vpRight, vpBottom, vpTop;
            CalculateProjectionParameters(out vpLeft, out vpRight, out vpBottom, out vpTop);

            float vpWidth = vpRight - vpLeft;
            float vpHeight = vpTop - vpBottom;

            float wvpLeft = vpLeft + windowLeft * vpWidth;
            float wvpRight = vpLeft + windowRight * vpWidth;
            float wvpTop = vpTop - windowTop * vpHeight;
            float wvpBottom = vpTop - windowBottom * vpHeight;

            Vector3 vpUpLeft = new Vector3(wvpLeft, wvpTop, -Near);
            Vector3 vpUpRight = new Vector3(wvpRight, wvpTop, -Near);
            Vector3 vpBottomLeft = new Vector3(wvpLeft, wvpBottom, -Near);
            Vector3 vpBottomRight = new Vector3(wvpRight, wvpBottom, -Near);

            Matrix4 inv = _viewMatrix.Inverse();

            Vector3 vwUpLeft = inv.TransformAffine(vpUpLeft);
            Vector3 vwUpRight = inv.TransformAffine(vpUpRight);
            Vector3 vwBottomLeft = inv.TransformAffine(vpBottomLeft);
            Vector3 vwBottomRight = inv.TransformAffine(vpBottomRight);

            windowClipPlanes.Clear();

            if (ProjectionType == Projection.Perspective)
            {
                Vector3 pos = GetPositionForViewUpdate();

                windowClipPlanes.Add(new Plane(pos, vwBottomLeft, vwUpLeft));
                windowClipPlanes.Add(new Plane(pos, vwUpLeft, vwUpRight));
                windowClipPlanes.Add(new Plane(pos, vwUpRight, vwBottomRight));
                windowClipPlanes.Add(new Plane(pos, vwBottomRight, vwBottomLeft));
            }
            else
            {
                Vector3 x_axis = new Vector3(inv.m00, inv.m01, inv.m02);
                Vector3 y_axis = new Vector3(inv.m10, inv.m11, inv.m12);
                x_axis.Normalize();
                y_axis.Normalize();

                windowClipPlanes.Add(new Plane(x_axis, vwBottomLeft));
                windowClipPlanes.Add(new Plane(-x_axis, vwUpRight));
                windowClipPlanes.Add(new Plane(y_axis, vwBottomLeft));
                windowClipPlanes.Add(new Plane(-y_axis, vwUpLeft));
            }

            recalculateWindow = false;
        }

        /// <summary>
        ///		Cancel view window.
        /// </summary>
        [OgreVersion(1, 7, 2790)]
        public virtual void ResetWindow()
        {
            isWindowSet = false;
        }

        /// <summary>
        ///		Gets the window plane at the specified index.
        /// </summary>
        /// <param name="index">Index of the plane to get.</param>
        /// <returns>The window plane at the specified index.</returns>
        public Plane GetWindowPlane(int index)
        {
            Debug.Assert(index < windowClipPlanes.Count, "Window clip plane index out of bounds.");

            // ensure the window is recalced
            SetWindowImpl();

            return (Plane)windowClipPlanes[index];
        }

        /// <summary>
        ///     Gets a world space ray as cast from the camera through a viewport position.
        /// </summary>
        /// <param name="screenX">
        ///     The x position at which the ray should intersect the viewport,
        ///     in normalized screen coordinates [0,1].
        /// </param>
        /// <param name="screenY">
        ///     The y position at which the ray should intersect the viewport,
        ///     in normalized screen coordinates [0,1].
        /// </param>
        /// <param name="ray">
        ///     Ray instance to populate with result
        /// </param>
        /// <returns></returns>
        [OgreVersion(1, 7, 2790, "Slightly different")]
        public void GetCameraToViewportRay(float screenX, float screenY, out Ray ray)
        {
            var inverseVP = (_projectionMatrix * _viewMatrix).Inverse();

#if !AXIOM_NO_VIEWPORT_ORIENTATIONMODE
            // We need to convert screen point to our oriented viewport (temp solution)
            Real tX = screenX; Real a = (int)OrientationMode * System.Math.PI * 0.5f;
            screenX = System.Math.Cos(a) * (tX - 0.5f) + System.Math.Sin(a) * (screenY - 0.5f) + 0.5f;
            screenY = System.Math.Sin(a) * (tX - 0.5f) + System.Math.Cos(a) * (screenY - 0.5f) + 0.5f;
            if ((((int)OrientationMode) & 1) == 1) screenY = 1.0f - screenY;
#endif

            Real nx = (2.0f * screenX) - 1.0f;
            Real ny = 1.0f - (2.0f * screenY);
            var nearPoint = new Vector3(nx, ny, -1.0f);
            // Use midPoint rather than far point to avoid issues with infinite projection
            var midPoint = new Vector3(nx, ny, 0.0f);

            // Get ray origin and ray target on near plane in world space

            var rayOrigin = inverseVP * nearPoint;
            var rayTarget = inverseVP * midPoint;

            var rayDirection = rayTarget - rayOrigin;
            rayDirection.Normalize();

            ray = new Ray(rayOrigin, rayDirection);
        }

        /// <summary>
        ///     Gets a world space ray as cast from the camera through a viewport position.
        /// </summary>
        /// <param name="screenX">
        ///     The x position at which the ray should intersect the viewport,
        ///     in normalized screen coordinates [0,1].
        /// </param>
        /// <param name="screenY">
        ///     The y position at which the ray should intersect the viewport,
        ///     in normalized screen coordinates [0,1].
        /// </param>
        /// <returns></returns>
        [OgreVersion(1, 7, 2790)]
        public Ray GetCameraToViewportRay(float screenX, float screenY)
        {
            Ray ret;
            GetCameraToViewportRay(screenX, screenY, out ret);
            return ret;
        }

        /// <summary>
        /// Gets a world-space list of planes enclosing a volume based on a viewport rectangle.
        /// </summary>
        /// <param name="screenLeft">the left bound of the on-screen rectangle, expressed in normalized screen coordinates [0,1]</param>
        /// <param name="screenTop">the upper bound of the on-screen rectangle, expressed in normalized screen coordinates [0,1]</param>
        /// <param name="screenRight">the right bound of the on-screen rectangle, expressed in normalized screen coordinates [0,1]</param>
        /// <param name="screenBottom">the lower bound of the on-screen rectangle, expressed in normalized screen coordinates [0,1]</param>
        /// <param name="includeFarPlane">whether to include the far frustum plane</param>
        /// <remarks>
        /// Can be useful for populating a <see cref="PlaneBoundedVolumeListSceneQuery"/>, e.g. for a rubber-band selection.
        /// </remarks>
        /// <returns></returns>
        public PlaneBoundedVolume GetCameraToViewportBoxVolume(Real screenLeft, Real screenTop, Real screenRight, Real screenBottom, bool includeFarPlane)
        {
            var vol = new PlaneBoundedVolume();
            GetCameraToViewportBoxVolume(screenLeft, screenTop, screenRight, screenBottom,
                vol, includeFarPlane);
            return vol;
        }

        /// <summary>
        /// Gets a world-space list of planes enclosing a volume based on a viewport rectangle.
        /// </summary>
        /// <param name="screenLeft">the left bound of the on-screen rectangle, expressed in normalized screen coordinates [0,1]</param>
        /// <param name="screenTop">the upper bound of the on-screen rectangle, expressed in normalized screen coordinates [0,1]</param>
        /// <param name="screenRight">the right bound of the on-screen rectangle, expressed in normalized screen coordinates [0,1]</param>
        /// <param name="screenBottom">the lower bound of the on-screen rectangle, expressed in normalized screen coordinates [0,1]</param>
        /// <param name="outVolume">The plane list to populate with the result</param>
        /// <param name="includeFarPlane">whether to include the far frustum plane</param>
        /// <remarks>
        /// Can be useful for populating a <see cref="PlaneBoundedVolumeListSceneQuery"/>, e.g. for a rubber-band selection.
        /// </remarks>
        /// <returns></returns>
        public void GetCameraToViewportBoxVolume(Real screenLeft, Real screenTop, Real screenRight, Real screenBottom,
            PlaneBoundedVolume outVolume, bool includeFarPlane)
        {
            outVolume.planes.Clear();

            if (ProjectionType == Projection.Perspective)
            {
                // Use the corner rays to generate planes
                var ul = GetCameraToViewportRay(screenLeft, screenTop);
                var ur = GetCameraToViewportRay(screenRight, screenTop);
                var bl = GetCameraToViewportRay(screenLeft, screenBottom);
                var br = GetCameraToViewportRay(screenRight, screenBottom);

                // top plane
                var normal = ul.Direction.Cross(ur.Direction);
                normal.Normalize();
                outVolume.planes.Add(new Plane(normal, DerivedPosition));

                // right plane
                normal = ur.Direction.Cross(br.Direction);
                normal.Normalize();
                outVolume.planes.Add(new Plane(normal, DerivedPosition));

                // bottom plane
                normal = br.Direction.Cross(bl.Direction);
                normal.Normalize();
                outVolume.planes.Add(new Plane(normal, DerivedPosition));

                // left plane
                normal = bl.Direction.Cross(ul.Direction);
                normal.Normalize();
                outVolume.planes.Add(new Plane(normal, DerivedPosition));
            }
            else
            {
                // ortho planes are parallel to frustum planes

                var ul = GetCameraToViewportRay(screenLeft, screenTop);
                var br = GetCameraToViewportRay(screenRight, screenBottom);

                UpdateFrustumPlanes();
                outVolume.planes.Add(new Plane(FrustumPlanes[(int)FrustumPlane.Top].Normal, ul.Origin));
                outVolume.planes.Add(new Plane(FrustumPlanes[(int)FrustumPlane.Right].Normal, br.Origin));
                outVolume.planes.Add(new Plane(FrustumPlanes[(int)FrustumPlane.Bottom].Normal, br.Origin));
                outVolume.planes.Add(new Plane(FrustumPlanes[(int)FrustumPlane.Left].Normal, ul.Origin));
            }

            // near/far planes applicable to both projection types
            outVolume.planes.Add(new Plane(FrustumPlanes[(int)FrustumPlane.Near]));
            if (includeFarPlane)
                outVolume.planes.Add(new Plane(FrustumPlanes[(int)FrustumPlane.Far]));
        }

        /// <summary>
        ///		Notifies this camera that a viewport is using it.
        /// </summary>
        /// <param name="viewport">Viewport that is using this camera.</param>
        [OgreVersion(1, 7, 2790)]
        public void NotifyViewport(Viewport viewport)
        {
            Viewport = viewport;
        }

        #endregion Public methods

        #region Internal engine methods

        /// <summary>
        ///		Called to ask a camera to render the scene into the given viewport.
        /// </summary>
        /// <param name="viewport"></param>
        /// <param name="showOverlays"></param>
        internal void RenderScene(Viewport viewport, bool showOverlays)
        {
            if (CameraPreRenderScene != null)
                CameraPreRenderScene(new CameraEventArgs(this));

            SceneManager.RenderScene(this, viewport, showOverlays);

            if (CameraPostRenderScene != null)
                CameraPostRenderScene(new CameraEventArgs(this));
        }

        /// <summary>
        ///		Updates an auto-tracking camera.
        /// </summary>
        [OgreVersion(1, 7, 2790)]
        internal void AutoTrack()
        {
            // assumes all scene nodes have been updated
            if (AutoTrackingTarget != null)
            {
                LookAt(AutoTrackingTarget.DerivedPosition + AutoTrackingOffset);
            }
        }

        #endregion Internal engine methods

        #region DisposableObject overrides

        protected override void dispose(bool disposeManagedResources)
        {
            if (!IsDisposed)
            {
                if (CameraDestroyed != null)
                    CameraDestroyed(new CameraEventArgs(this));
            }
            base.dispose(disposeManagedResources);
        }

        #endregion

        public override string ToString()
        {
            var dir = orientation * new Vector3(0, 0, -1);
            var s = new StringBuilder();

            s.Append("Camera(");
            s.AppendFormat("Name='{0}, ", name);
            s.AppendFormat("pos={0}, ", position);
            s.AppendFormat("direction={0}, ", dir);
            s.AppendFormat("near={0}, ", Near);
            s.AppendFormat("far={0}, ", Far);
            s.AppendFormat("FOVy={0}, ", FieldOfView); // todo: to .Degrees()
            s.AppendFormat("aspect={0}, ", AspectRatio);
            s.AppendFormat("xoffset={0}, ", FrustumOffset.x);
            s.AppendFormat("yoffset={0}, ", FrustumOffset.y);
            s.AppendFormat("focalLength={0}, ", _focalLength);
            s.AppendFormat("NearFrustumPlane={0}, ", FrustumPlanes[(int)FrustumPlane.Near]);
            s.AppendFormat("FarFrustumPlane={0}, ", FrustumPlanes[(int)FrustumPlane.Far]);
            s.AppendFormat("LeftFrustumPlane={0}, ", FrustumPlanes[(int)FrustumPlane.Left]);
            s.AppendFormat("RightFrustumPlane={0}, ", FrustumPlanes[(int)FrustumPlane.Right]);
            s.AppendFormat("TopFrustumPlane={0}, ", FrustumPlanes[(int)FrustumPlane.Top]);
            s.AppendFormat("BottomFrustumPlane={0}, ", FrustumPlanes[(int)FrustumPlane.Bottom]);
            s.Append(")");

            return s.ToString();
        }
    }
}