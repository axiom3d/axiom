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
using System.Diagnostics;



using DotNet3D.Math;
using DotNet3D.Math.Collections;

#endregion Namespace Declarations
			
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
    ///		<para/>
    ///		Cameras maintain their own aspect ratios, field of view, and frustrum,
    ///		and project co-ordinates into a space measured from -1 to 1 in x and y,
    ///		and 0 to 1 in z. At render time, the camera will be rendering to a
    ///		Viewport which will translate these parametric co-ordinates into real screen
    ///		co-ordinates. Obviously it is advisable that the viewport has the same
    ///		aspect ratio as the camera to avoid distortion (unless you want it!).
    ///		<para/>
    ///		Note that a Camera can be attached to a SceneNode, using the method
    ///		SceneNode.AttachObject. If this is done the Camera will combine it's own
    ///		position/orientation settings with it's parent SceneNode. 
    ///		This is useful for implementing more complex Camera / object
    ///		relationships i.e. having a camera attached to a world object.
    /// </remarks>
    /// <ogre name="Camera">
    ///     <file name="OgreCamera.h" revision="1.36.2.1" lastUpdated="6/16/06" lastUpdatedBy="Lehuvyterz" />
    ///     <file name="OgreCamera.cpp" revision="1.54" lastUpdated="6/16/06" lastUpdatedBy="Lehuvyterz" />
    /// </ogre>
    public class Camera : Frustum
    {
        #region Fields

        #region SceneManager Property

        /// <summary>
        ///		Parent scene manager.
        /// </summary>
        /// <ogre name="mSceneMgr" />
        private SceneManager _sceneManager;
        /// <summary>
        ///    Returns the current SceneManager that this camera is using.
        /// </summary>
        /// <ogre name="getSceneManager" />
        public SceneManager SceneManager
        {
            get
            {
                return _sceneManager;
            }
            protected set
            {
                _sceneManager = value;
            }
        }

        #endregion SceneManager Property

        #region Orientation Property

        /// <summary>
        ///		Camera orientation.
        /// </summary>
        /// <ogre name="mOrientation" />
        private Quaternion _orientation;
        /// <summary>
        ///     Gets/Sets the camera's orientation.
        /// </summary>
        /// <ogre name="getOrientation" />
        /// <ogre name="setOrientation" />
        public Quaternion Orientation
        {
            get
            {
                return _orientation;
            }
            set
            {
                _orientation = value;
                InvalidateView();
            }
        }

        #endregion Orientation Property

        #region Position Property

        /// <summary>
        ///		Camera position.
        /// </summary>
        /// <ogre name="mPosition" />
        private Vector3 _position;
        /// <summary>
        ///     Gets/Sets the camera's position.
        /// </summary>
        /// <ogre name="getPosition" />
        /// <ogre name="setPosition" />
        public Vector3 Position
        {
            get
            {
                return _position;
            }
            set
            {
                _position = value;
                InvalidateView();
            }
        }

        #endregion Position Property

        #region DerivedOrientation Property

        /// <summary>
        ///		Orientation dervied from parent.
        /// </summary>
        /// </ogre name="mDerivedOrientation" />
        private Quaternion _derivedOrientation;
        /// <summary>
        ///		Gets the derived orientation of the camera.
        /// </summary>
        /// <ogre name="getDerivedOrientation" />
        public Quaternion DerivedOrientation
        {
            get
            {
                UpdateView();
                return _derivedOrientation;
            }
            protected set
            {
                _derivedOrientation = value;
            }
        }

        #endregion DerivedOrientation Property

        #region DerivedPosition Property

        /// <summary>
        ///		Position dervied from parent.
        /// </summary>
        /// <ogre name="mDerivedPosition" />
        private Vector3 _derivedPosition;
        /// <summary>
        ///		Gets the derived position of the camera.
        /// </summary>
        /// <ogre name="getDerivedPosition" />
        public Vector3 DerivedPosition
        {
            get
            {
                UpdateView();
                return _derivedPosition;
            }
            protected set
            {
                _derivedPosition = value;
            }
        }

        #endregion DerivedPosition Property

        #region FixedYawAxis Property

        /// <summary>
        ///		Whether to yaw around a fixed axis.
        /// </summary>
        /// <ogre name="mYawFixed" />
        private bool _isYawFixed;
        /// <summary>
        ///		Fixed axis to yaw around.
        /// </summary>
        /// <ogre name="mYawFixedAxis" />
        private Vector3 _yawFixedAxis;
        /// <summary>
        /// 
        /// </summary>
        /// <ogre name="setFixedYawAxis" />
        public Vector3 FixedYawAxis
        {
            get
            {
                return _yawFixedAxis;
            }
            set
            {
                _yawFixedAxis = value;

                if ( _yawFixedAxis != Vector3.Zero )
                {
                    _isYawFixed = true;
                }
                else
                {
                    _isYawFixed = false;
                }
            }
        }

        #endregion FixedYawAxis Property

        #region SceneDetail Property

        /// <summary>
        ///		Rendering type (wireframe, solid, point).
        /// </summary>
        /// <ogre name="mSceneDetail" />
        private SceneDetailLevel _sceneDetail;
        /// <summary>
        ///		Sets the level of rendering detail required from this camera.
        /// </summary>
        /// <remarks>
        ///		Each camera is set to render at full detail by default, that is
        ///		with full texturing, lighting etc. This method lets you change
        ///		that behavior, allowing you to make the camera just render a
        ///		wireframe view, for example.
        /// </remarks>
        /// <ogre name="getDetailLevel" />
        /// <ogre name="setDetailLevel" />
        public SceneDetailLevel SceneDetail
        {
            get
            {
                return _sceneDetail;
            }
            set
            {
                _sceneDetail = value;
            }
        }

        #endregion SceneDetail Property

        #region RenderFaceCount Property
        
        /// <summary>
        ///		Stored number of visible faces in the last render.
        /// </summary>
        /// <ogre name="mVisFacesLastRender" />
        private int _numFacesRenderedLastFrame;
        /// <summary>
        /// Gets the last count of triangles visible in the view of this camera.
        /// </summary>
        /// <Ogre name="_getNumRenderedFaces" />
        public int RenderedFaceCount
        {
            get
            {
                return _numFacesRenderedLastFrame;
            }
            protected set
            {
                _numFacesRenderedLastFrame = value;
            }
        }

        #endregion RenderFaceCount Property

        #region AutoTrackingTarget Property

        /// <summary>
        ///		SceneNode which this Camera will automatically track.
        /// </summary>
        /// <ogre name="mAutoTrackTarget" />
        private SceneNode _autoTrackTarget;
        /// <Ogre name="getAutoTrackTarget" />
        public SceneNode AutoTrackingTarget
        {
            get
            {
                return _autoTrackTarget;
            }
            protected set
            {
                _autoTrackTarget = value;
            }
        }

        #endregion AutoTrackingTarget Property

        #region AutoTrackingOffset Property

        /// <summary>
        ///		Tracking offset for fine tuning.
        /// </summary>
        /// <ogre name="mAutoTrackOffset" />
        private Vector3 _autoTrackOffset;
        /// <ogre name="getAutoTrackOffset" />
        public Vector3 AutoTrackingOffset
        {
            get
            {
                return _autoTrackOffset;
            }
            protected set
            {
                _autoTrackOffset = value;
            }
        }

        #endregion AutoTrackingOffset Property

        #region LodBias Property

        /// <summary>
        ///		Scene LOD factor used to adjust overall LOD.
        /// </summary>
        /// <ogre name="mSceneLodFactor" />
        private Real _sceneLodFactor;
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
        /// <ogre name="getLodBias" />
        /// <ogre name="setLodBias" />
        public Real LodBias
        {
            get
            {
                return _sceneLodFactor;
            }
            set
            {
                Debug.Assert(value > 0.0f, "Lod bias must be greater than 0");
                _sceneLodFactor = value;
                _invSceneLodFactor = 1.0f / _sceneLodFactor;
            }
        }
        #endregion LodBias Property

        #region InverseLodBias Property

        /// <summary>
        ///		Inverted scene LOD factor, can be used by Renderables to adjust their LOD.
        /// </summary>
        /// <ogre name="mSceneLodFactorInv" />
        protected Real _invSceneLodFactor;
        /// <summary>
        ///     Used for internal Lod calculations.
        /// </summary>
        /// <ogre name="_getLodBiasInverse" />
        public Real InverseLodBias
        {
            get
            {
                return _invSceneLodFactor;
            }
            protected set
            {
                _invSceneLodFactor = value;
            }
        }

        #endregion InverseLodBias Property

        #region windowLeft Property

        /// <summary>
        ///		Left window edge (window clip planes).
        /// </summary>
        /// <ogre name="mWLeft" />
        private Real _windowLeft;
        protected Real windowLeft
        {
            get
            {
                return _windowLeft;
            }
            set
            {
                _windowLeft = value;
            }
        }

        #endregion windowLeft Property

        #region windowRight Property

        /// <summary>
        ///		Right window edge (window clip planes).
        /// </summary>
        /// <ogre name="mWRigh" />
        private Real _windowRight;
        protected Real windowRight
        {
            get
            {
                return _windowRight;
            }
            set
            {
                _windowRight = value;
            }
        }

        #endregion windowRight Property

        #region windowTop Property

        /// <summary>
        ///		Top window edge (window clip planes).
        /// </summary>
        /// <ogre name="mWTop" />
        private Real _windowTop;
        protected Real windowTop
        {
            get
            {
                return _windowTop;
            }
            set
            {
                _windowTop = value;
            }
        }

        #endregion windowTop Property

        #region windowBottom Property

        /// <summary>
        ///		Bottom window edge (window clip planes).
        /// </summary>
        /// <ogre name="mWBottom" />
        private Real _windowBottom;
        protected Real windowBottom
        {
            get
            {
                return _windowBottom;
            }
            set
            {
                _windowBottom = value;
            }
        }

        #endregion windowBottom Property

        #region IsWindowSet Property

        /// <summary>
        ///		Is viewing window used.
        /// </summary>
        /// <ogre name="mWindowSet" />
        private bool _isWindowSet;
        /// <summary>
        ///		Gets the flag specifying if a viewport window is being used.
        /// </summary>
        /// <ogre name="isWindowSet" />
        public virtual bool IsWindowSet
        {
            get
            {
                return _isWindowSet;
            }
            protected set
            {
                _isWindowSet = value;
            }
        }

        #endregion IsWindowSet Property

        #region windowClipPlanes Property

        /// <summary>
        ///		Windowed viewport clip planes.
        /// </summary>
        /// <ogre name="mWindowClipPlanes" />
        private PlaneList _windowClipPlanes = new PlaneList();
        /// <Ogre name=getWindowPlanes" />
        protected PlaneList windowClipPlanes
        {
            get
            {
                return _windowClipPlanes;
            }
            protected set
            {
                _windowClipPlanes = value;
            }
        }

        #endregion windowClipPlanes Property

        #region recalculateWindow Property

        /// <summary>
        ///		Was viewing window changed?
        /// </summary>
        /// <ogre name="mRecalcWindow" />
        private bool _recalculateWindow;
        protected bool recalculateWindow
        {
            get
            {
                return _recalculateWindow;
            }
            set
            {
                _recalculateWindow = value;
            }
        }

        #endregion recalculateWindow Property

        #region Viewport Property

        /// <summary>
        ///		The last viewport to be added using this camera.
        /// </summary>
        /// <ogre name="mLastViewport" />
        private Viewport _lastViewport;
        /// <summary>
        ///		Get the last viewport which was attached to this camera. 
        /// </summary>
        /// <remarks>
        ///		This is not guaranteed to be the only viewport which is
        ///		using this camera, just the last once which was created referring
        ///		to it.
        /// </remarks>
        /// <ogre name="getViewport" />
        public Viewport Viewport
        {
            get
            {
                return _lastViewport;
            }
            protected set
            {
                _lastViewport = value;
            }
        }

        #endregion Viewport Property

        #region AutoAspectRatio Property

        /// <summary>
        ///		Whether aspect ratio will automaticaally be recalculated when a vieport changes its size.
        /// </summary>
        /// <ogre name="mAutoAspectRatio" />
        private bool _autoAspectRatio;
        /// <summary>
        ///		If set to true a vieport that owns this frustum will be able to 
        ///		recalculate the aspect ratio whenever the frustum is resized.
        /// </summary>
        /// <remarks>
        ///		You should set this to true only if the frustum / camera is used by 
        ///		one viewport at the same time. Otherwise the aspect ratio for other 
        ///		viewports may be wrong.
        /// </remarks>
        /// <ogre name="getAutoAspectRatio" />
        /// <ogre name="setAutoAspectRatio" />
        public bool AutoAspectRatio
        {
            get
            {
                return _autoAspectRatio;
            }
            set
            {
                _autoAspectRatio = value;
            }
        }

        #endregion AutoAspectRatio Property

        #endregion Fields

        #region Constructors

        public Camera( string name, SceneManager sceneManager )
        {
            // Init camera location & direction

            // Locate at (0,0,0)
            _position = Vector3.Zero;
            derivedPosition = Vector3.Zero;

            // Point down -Z axis
            _orientation = Quaternion.Identity;
            derivedOrientation = Quaternion.Identity;

//            fieldOfView = MathUtil.RadiansToDegrees( MathUtil.PI / 4.0f );
            this.FOV = (Real)(new Radian( new Real( Utility.PI / 4.0f ) ).InDegrees);
            _nearDistance = 100.0f;
            _farDistance = 100000.0f;
            _aspectRatio = 1.33333333333333f;
            _projectionType = Projection.Perspective;

            ViewMatrix = Matrix4.Zero;
            ProjectionMatrix = Matrix4.Zero;

            // Reasonable defaults to camera params
            _sceneDetail = SceneDetailLevel.Solid;

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

            // no reflection
            IsReflected = false;
            IsVisible = false;

            _isWindowSet = false;
            _autoAspectRatio = false;
        }

        #endregion

        #region Frustum Members


        /// <summary>
        ///		Get the derived orientation of this frustum.
        /// </summary>
        /// <returns></returns>
        /// <ogre name="getOrientationForViewUpdate" />
        protected override Quaternion GetOrientationForViewUpdate()
        {
            return _derivedOrientation;
        }

        /// <summary>
        ///		Get the derived position of this frustum.
        /// </summary>
        /// <returns></returns>
        /// <ogre name="getPositionForViewUpdate" />
        protected override Vector3 GetPositionForViewUpdate()
        {
            return _derivedPosition;
        }

        /// <summary>
        ///		Signal to update view information.
        /// </summary>
        /// <ogre name="invalidateView" />
        protected override void InvalidateView()
        {
            recalculateView = true;
            recalculateWindow = true;
        }

        /// <summary>
        ///		Signal to update frustum information.
        /// </summary>
        /// <ogre name="invalidateFrustum" />
        protected override void InvalidateFrustum()
        {
            recalculateFrustum = true;
            recalculateWindow = true;
        }

        /// <summary>
        ///		Updates the frustum data.
        /// </summary>
        /// <ogre name="updateFrustum" />
        protected override void UpdateFrustum()
        {
            base.UpdateFrustum();
            // Set the clipping planes
            setWindow();
        }

        /// <summary>
        ///		Updates the view matrix.
        /// </summary>
        /// <ogre name="updateView" />
        protected override void UpdateView()
        {
            base.UpdateView();
            setWindow();
        }

        /// <summary>
        ///		Evaluates whether or not the view matrix is out of date.
        /// </summary>
        /// <returns></returns>
        /// <ogre name="isViewOutOfDate" />
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
                        derivedOrientation = lastParentOrientation * _orientation;
                        derivedPosition = ( lastParentOrientation * _position ) + lastParentPosition;
                        returnVal = true;
                    }
                }
                else
                {
                    // rely on own updates
                    _derivedOrientation = _orientation;
                    _derivedPosition = _position;
                }

                if ( IsReflected && linkedReflectionPlane != null &&
                    !( lastLinkedReflectionPlane == linkedReflectionPlane.DerivedPlane ) )
                {

                    ReflectionPlane = linkedReflectionPlane.DerivedPlane;
                    ReflectionMatrix = Utility.BuildReflectionMatrix( ReflectionPlane );
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
        /// <ogre name="getBoundingRadius" />
        public override Real BoundingRadius
        {
            get
            {
                // return a little bigger than the near distance
                // just to keep things just outside
                return Near * 1.5f;
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
        /// <ogre name="_notifyRenderedFaces" />
        internal void NotifyRenderedFaces( int renderedFaceCount )
        {
            _numFacesRenderedLastFrame = renderedFaceCount;
        }

        #endregion

        #region Public Properties

        /// <summary>
        ///		Gets/Sets the camera's direction vector.
        /// </summary>
        /// <ogre name="getDirection" />
        /// <ogre name="setDirection" />
        public Vector3 Direction
        {
            // Direction points down the negatize Z axis by default.
            get
            {
                return _orientation * -Vector3.UnitZ;
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

                if ( _isYawFixed )
                {
                    Vector3 xVector = _yawFixedAxis.CrossProduct( zAdjustVector );
                    xVector.Normalize();

                    Vector3 yVector = zAdjustVector.CrossProduct( xVector );
                    yVector.Normalize();

                    _orientation.FromAxes( xVector, yVector, zAdjustVector );
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
                        rotationQuat = Quaternion.FromAngleAxis( Utility.PI, yAxis );
                    }
                    else
                    {
                        // Derive shortest arc to new direction
                        rotationQuat = zAxis.GetRotationTo( zAdjustVector );
                    }

                    _orientation = rotationQuat * _orientation;
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
        /// <ogre name="getRight" />
        public Vector3 Right
        {
            get
            {
                return _orientation * Vector3.UnitX;
            }
        }

        /// <summary>
        ///		Gets camera's 'up' vector.
        /// </summary>
        /// <ogre name="getUp" />
        public Vector3 Up
        {
            get
            {
                return _orientation * Vector3.UnitY;
            }
        }


        /// <summary>
        ///		Gets the derived direction of the camera.
        /// </summary>
        /// <ogre name="getDerivedDirection" />
        public Vector3 DerivedDirection
        {
            get
            {
                UpdateView();

                // RH coords, direction points down -Z by default
                return _derivedOrientation * -Vector3.UnitZ;
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
        /// <ogre name="move" />
        public void Move( Vector3 offset )
        {
            _position = _position + offset;
            InvalidateView();
        }

        /// <summary>
        /// Moves the camera's position by the vector offset provided along it's own axes (relative to orientation).
        /// </summary>
        /// <param name="offset"></param>
        /// <ogre name="moveRelative" />
        public void MoveRelative( Vector3 offset )
        {
            // Transform the axes of the relative vector by camera's local axes
            Vector3 transform = _orientation * offset;

            _position = _position + transform;
            InvalidateView();
        }

        /// <summary>
        ///		Specifies a target that the camera should look at.
        /// </summary>
        /// <remarks>
        ///     This is a helper method to automatically generatee the
        ///     direction vector for the camera, based on it's current position
        ///     and the supplied look-at point.
        /// </remarks>
        /// <param name="target">A vector specifying the look at point.</param>
        /// <ogre name="lookAt" />
        public void LookAt( Vector3 target )
        {
            UpdateView();
            this.Direction = ( target - _derivedPosition );
        }

        /// <summary>
        ///     Points the camera at a location in worldspace.
        /// </summary>
        /// <remarks>
        ///     This is a helper method to automatically generate the
        ///     direction vector for the camera, based on it's current position
        ///     and the supplied look-at point.
        /// </remarks>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <ogre name="lookAt" />
        public void LookAt(Real x, Real y, Real z)
        {
            Vector3 vTemp = new Vector3(x, y, z);
            this.LookAt(vTemp);
        }
        /// <summary>
        ///		Pitches the camera up/down counter-clockwise around it's local x axis.
        /// </summary>
        /// <param name="degrees"></param>
        /// <ogre name="pitch" />
        public void Pitch( Real degrees )
        {
            Vector3 xAxis = orientation * Vector3.UnitX;
            Rotate( xAxis, degrees );

            InvalidateView();
        }

        /// <summary>
        ///		Rolls the camera counter-clockwise, in degrees, around its local y axis.
        /// </summary>
        /// <param name="degrees"></param>
        /// <ogre name="yaw" />
        public void Yaw( Real degrees )
        {
            Vector3 yAxis;

            if ( _isYawFixed )
            {
                // Rotate around fixed yaw axis
                yAxis = _yawFixedAxis;
            }
            else
            {
                // Rotate around local Y axis
                yAxis = _orientation * Vector3.UnitY;
            }

            Rotate( yAxis, degrees );

            InvalidateView();
        }

        /// <summary>
        ///		Rolls the camera counter-clockwise, in degrees, around its local z axis.
        /// </summary>
        /// <param name="degrees"></param>
        /// <ogre name="roll" />
        public void Roll( Real degrees )
        {
            // Rotate around local Z axis
            Vector3 zAxis = _orientation * Vector3.UnitZ;
            Rotate( zAxis, degrees );

            InvalidateView();
        }

        /// <summary>
        ///		Rotates the camera about an arbitrary axis.
        /// </summary>
        /// <param name="quat"></param>
        /// <ogre name="rotate" />
        public void Rotate( Quaternion quat )
        {
            // Note the order of the multiplication
            _orientation = quat * _orientation;

            InvalidateView();
        }

        /// <summary>
        ///		Rotates the camera about an arbitrary axis.
        /// </summary>
        /// <param name="axis"></param>
        /// <param name="degrees"></param>
        /// <ogre name="rotate" />
        public void Rotate( Vector3 axis, Real degrees )
        {
            Quaternion q = Quaternion.FromAngleAxis( (Real)(new Degree( (Real)degrees ).InRadians), axis );
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
        /// <ogre name="setAutoTracking" />
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
        ///	<ogre name="setAutoTracking" />
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
        ///	<ogre name="setAutoTracking" />
        public void SetAutoTracking( bool enabled, SceneNode target, Vector3 offset )
        {
            if ( enabled )
            {
                Debug.Assert( target != null, "A camera's auto track target cannot be null." );
                _autoTrackTarget = target;
                _autoTrackOffset = offset;
            }
            else
            {
                _autoTrackTarget = null;
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
        /// <ogre name="setWindow" />
        public virtual void SetWindow( Real left, Real top, Real right, Real bottom )
        {
            _windowLeft = left;
            _windowTop = top;
            _windowRight = right;
            _windowBottom = bottom;

            _isWindowSet = true;
            _recalculateWindow = true;

            InvalidateView();
        }

        /// <summary>
        ///		Do actual window setting, using parameters set in <see cref="SetWindow"/> call.
        /// </summary>
        /// <remarks>The method is called after projection matrix each change.</remarks>
        /// <ogre name="setWindowImpl" />
        protected void setWindow()
        {
            if ( !_isWindowSet || !_recalculateWindow )
            {
                return;
            }

            Real thetaY = (Real)( new Degree( new Real( FOV * 0.5f ) ).InRadians );
            //MathUtil.DegreesToRadians( fieldOfView * 0.5f );
            Real tanThetaY = Utility.Tan( (Real)thetaY );
            Real tanThetaX = tanThetaY * AspectRatio;

            Real vpTop = tanThetaY * Near;
            Real vpLeft = -tanThetaX * Near;
            Real vpWidth = -2 * vpLeft;
            Real vpHeight = -2 * vpTop;

            Real wvpLeft = vpLeft + _windowLeft * vpWidth;
            Real wvpRight = vpLeft + _windowRight * vpWidth;
            Real wvpTop = vpTop - _windowTop * vpHeight;
            Real wvpBottom = vpTop - _windowBottom * vpHeight;

            Vector3 vpUpLeft = new Vector3( wvpLeft, wvpTop, -Near );
            Vector3 vpUpRight = new Vector3( wvpRight, wvpTop, -Near );
            Vector3 vpBottomLeft = new Vector3( wvpLeft, wvpBottom, -Near );
            Vector3 vpBottomRight = new Vector3( wvpRight, wvpBottom, -Near );

            Matrix4 inv = ViewMatrix.Inverse();

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

            _recalculateWindow = false;
        }

        /// <summary>
        ///		Cancel view window.
        /// </summary>
        /// <ogre name="resetWindow" />
        public virtual void ResetWindow()
        {
            _isWindowSet = false;
        }

        /// <summary>
        ///		Gets the window plane at the specified index.
        /// </summary>
        /// <param name="index">Index of the plane to get.</param>
        /// <returns>The window plane at the specified index.</returns>
        /// <ogre name="getWindowPlanes" />
        public Plane GetWindowPlane( int index )
        {
            Debug.Assert( index < windowClipPlanes.Count, "Window clip plane index out of bounds." );

            // ensure the window is recalced
            setWindow();

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
        /// <ogre name="getCameraToViewportRay" />
        public Ray GetCameraToViewportRay( Real screenX, Real screenY )
        {
            Real centeredScreenX = ( screenX - 0.5f );
            Real centeredScreenY = ( 0.5f - screenY );

            Real normalizedSlope = Utility.Tan( (Real)(new Degree( new Real( FOV * 0.5f ) ).InRadians ) );
            //MathUtil.DegreesToRadians( fieldOfView * 0.5f ) );
            Real viewportYToWorldY = normalizedSlope * _nearDistance * 2;
            Real viewportXToWorldX = viewportYToWorldY * _aspectRatio;

            Vector3 rayDirection, rayOrigin;

            if (ProjectionType == Projection.Perspective)
            {
                // From camer center
                rayOrigin = DerivedPosition;
                // Point to perspective projected position
                rayDirection.x = centeredScreenX * viewportXToWorldX;
                rayDirection.y = centeredScreenY * viewportYToWorldY;
                rayDirection.z = -Near;
                rayDirection = DerivedOrientation * rayDirection;
                rayDirection.Normalize();
            }
            else
            {
                // Ortho always parallel to point on screen
                rayOrigin.x = centeredScreenX * viewportXToWorldX;
                rayOrigin.y = centeredScreenY * viewportYToWorldY;
                rayOrigin.z = 0.0f;
                rayOrigin = DerivedOrientation * rayOrigin;
                rayOrigin = DerivedPosition + rayOrigin;
                rayDirection = DerivedDirection;
            }

            return new Ray(rayOrigin, rayDirection);
        }

        /// <summary>
        ///		Notifies this camera that a viewport is using it.
        /// </summary>
        /// <param name="viewport">Viewport that is using this camera.</param>
        /// <ogre name="_notifyViewport" />
        public void NotifyViewport( Viewport viewport )
        {
            lastViewport = viewport;
        }

        #endregion

        #region Internal engine methods

        /// <summary>
        ///		Called to ask a camera to render the scene into the given viewport.
        /// </summary>
        /// <param name="viewport">The viewport to render to</param>
        /// <param name="showOverlays">Whether or not any overlay objects should be included</param>
        /// <ogre name="_renderScene" />
        internal void RenderScene( Viewport viewport, bool showOverlays )
        {
            _sceneManager.RenderScene( this, viewport, showOverlays );
        }

        /// <summary>
        ///		Updates an auto-tracking camera.
        /// </summary>
        /// <ogre name="_autoTrack" />
        internal void AutoTrack()
        {
            // assumes all scene nodes have been updated
            if ( _autoTrackTarget != null )
            {
                LookAt( _autoTrackTarget.DerivedPosition + _autoTrackOffset );
            }
        }

        #endregion
    }
}
