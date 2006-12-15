#region LGPL License
/*
Axiom Graphics Engine Library
Copyright (C) 2003-2006 Axiom Project Team

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
using System.Collections;
using System.Diagnostics;


using DotNet3D.Math;

#endregion Namespace Declarations
			
namespace Axiom
{

    /// <summary>
    ///     A frustum represents a pyramid, capped at the near and far end which is
    ///     used to represent either a visible area or a projection area. Can be used
    ///     for a number of applications.
    /// </summary>
    /// <ogre name="Frustum"
    ///     <file name="OgreFrustum.h" revision="1.15" lastUpdated="6/16/06" lastUpdatedBy="Lehuvyterz" />
    ///     <file name="OgreFrustum.cpp" revision="1.27.2.2" lastUpdated="6/16/06" lastUpdatedBy="Lehuvyterz" />
    /// </ogre>
    // TODO Review attaching object in the scene and making them no longer require a name.
    public class Frustum : MovableObject, IRenderable
    {
        #region Constants

        /// <summary>
        ///		Small constant used to reduce far plane projection to avoid inaccuracies.
        /// </summary>
        public const float InfiniteFarPlaneAdjust = 0.00001f;

        /// <summary>
        ///		Arbitrary large distance to use for the far plane when set to 0 (infinite far plane).
        /// </summary>
        public const float InfiniteFarPlaneDistance = 100000.0f;

        #endregion Constants

        #region Fields and Properties

        #region ProjectionType Property
        /// <summary>
        ///		Perspective or Orthographic?
        /// </summary>
        /// <ogre name="mProjType" />
        private Projection _projectionType;
        /// <summary>
        ///    Gets/Sets the type of projection to use (orthographic or perspective). Default is perspective.
        /// </summary>
        /// <ogre name="getProjectionType" />
        /// <ogre name="setProjectionType" />
        public Projection ProjectionType
        {
            get
            {
                return _projectionType;
            }
            set
            {
                _projectionType = value;
                InvalidateFrustum();
            }
        }

        #endregion ProjectionType Property

        #region FOV property
        /// <summary>
        ///     y-direction field-of-view (default 45).
        /// </summary>
        /// <ogre name="mFOVy" />
        private Real _fieldOfView;
        /// <summary>
        ///		Sets the Y-dimension Field Of View (FOV) of the camera.
        /// </summary>
        /// <remarks>
        ///		Field Of View (FOV) is the angle made between the camera's position, and the left & right edges
        ///		of the 'screen' onto which the scene is projected. High values (90+) result in a wide-angle,
        ///		fish-eye kind of view, low values (30-) in a stretched, telescopic kind of view. Typical values
        ///		are between 45 and 60.
        ///		<p/>
        ///		This value represents the HORIZONTAL field-of-view. The vertical field of view is calculated from
        ///		this depending on the dimensions of the viewport (they will only be the same if the viewport is square).
        /// </remarks>
        /// <ogre name="getFOVy" />
        /// <ogre name="setFOVy" />
        public virtual Real FOV
        {
            get
            {
                return _fieldOfView;
            }
            set
            {
                _fieldOfView = value;
                InvalidateFrustum();
                InvalidateView();	//XEONX FIX: Now the IsObjectVisible() will work properly
            }
        }

        #endregion FOV Property

        #region Far Property

        /// <summary>
        ///     Far clip distance - default 10000.
        /// </summary>
        /// <ogre name="mFarDist" />
        private Real _farDistance;
        /// <summary>
        ///		Gets/Sets the distance to the far clipping plane.
        ///	 </summary>
        ///	 <remarks>
        ///		The view frustrum is a pyramid created from the frustum position and the edges of the viewport.
        ///		This frustrum does not extend to infinity - it is cropped near to the camera and there is a far
        ///		plane beyond which nothing is displayed. This method sets the distance for the far plane. Different
        ///		applications need different values: e.g. a flight sim needs a much further far clipping plane than
        ///		a first-person shooter. An important point here is that the larger the gap between near and far
        ///		clipping planes, the lower the accuracy of the Z-buffer used to depth-cue pixels. This is because the
        ///		Z-range is limited to the size of the Z buffer (16 or 32-bit) and the max values must be spread over
        ///		the gap between near and far clip planes. The bigger the range, the more the Z values will
        ///		be approximated which can cause artifacts when lots of objects are close together in the Z-plane. So
        ///		make sure you clip as close to the camera as you can - don't set a huge value for the sake of
        ///		it.
        /// </remarks>
        /// <value>
        ///		The distance to the far clipping plane from the frustum in 
        ///		world coordinates.  If you specify 0, this means an infinite view
        ///		distance which is useful especially when projecting shadows; but
        ///		be careful not to use a near distance too close.
        /// </value>
        /// <ogre name="getFarClipDistance" />
        /// <ogre name="setFarClipDistance" />
        public virtual Real Far
        {
            get
            {
                return _farDistance;
            }
            set
            {
                _farDistance = value;
                InvalidateFrustum();
                InvalidateView();	//XEONX FIX: Now the IsObjectVisible() will work properly
            }
        }

        #endregion Far Property

        #region Near Property

        /// <summary>
        ///     Near clip distance - default 100.
        /// </summary>
        /// <ogre name="mNearDist" />
        private Real _nearDistance;
        /// <summary>
        ///		Gets/Sets the position of the near clipping plane.
        ///	</summary>
        ///	<remarks>
        ///		The position of the near clipping plane is the distance from the frustums position to the screen
        ///		on which the world is projected. The near plane distance, combined with the field-of-view and the
        ///		aspect ratio, determines the size of the viewport through which the world is viewed (in world
        ///		co-ordinates). Note that this world viewport is different to a screen viewport, which has it's
        ///		dimensions expressed in pixels. The cameras viewport should have the same aspect ratio as the
        ///		screen viewport it renders into to avoid distortion.
        /// </remarks>
        /// <ogre name="getNearClipDistance" />
        /// <ogre name="setNearClipDistance" />
        public virtual Real Near
        {
            get
            {
                return _nearDistance;
            }
            set
            {
                Debug.Assert(value > 0, "Near clip distance must be greater than zero.");

                _nearDistance = value;
                InvalidateFrustum();
                InvalidateView();	//XEONX FIX: Now the IsObjectVisible() will work properly
            }
        }

        #endregion NearDistance Property

        #region AspectRatio Property

        /// <summary>
        ///     x/y viewport ratio - default 1.3333
        /// </summary>
        /// <ogre name="mAspect" />
        private Real _aspectRatio;
        /// <summary>
        ///		Gets/Sets the aspect ratio to use for the camera viewport.
        /// </summary>
        /// <remarks>
        ///		The ratio between the x and y dimensions of the rectangular area visible through the camera
        ///		is known as aspect ratio: aspect = width / height .
        ///		<para />
        ///		The default for most fullscreen windows is 1.3333f - this is also assumed unless you
        ///		use this property to state otherwise.
        /// </remarks>
        /// <ogre name="getAspectRatio" />
        /// <ogre name="setAspectRatio" />
        public virtual Real AspectRatio
        {
            get
            {
                return _aspectRatio;
            }
            set
            {
                _aspectRatio = value;
                InvalidateFrustum();
            }
        }

        #endregion AspectRatio Property

        #region planes Property

        /// <summary>
        ///     The 6 main clipping planes.
        /// </summary>
        /// <ogre name="mFrustrumPlanes" />
        private Plane[] _planes = new Plane[6];
        protected Plane[] planes
        {
            get
            {
                return _planes;
            }
            set
            {
                _planes = value;
            }
        }

        #endregion planes Property

        #region lastParentOrientation Property

        /// <summary>
        ///     Stored versions of parent orientation.
        /// </summary>
        /// <ogre name="mLastParentOrientation" />
        private Quaternion _lastParentOrientation;
        protected Quaternion lastParentOrientation
        {
            get
            {
                return _lastParentOrientation;
            }
            set
            {
                _lastParentOrientation = value;
            }
        }

        #endregion lastParentOrientation Property

        #region lastParentPosition Property

        /// <summary>
        ///     Stored versions of parent position.
        /// </summary>
        /// <ogre name="mLastParentPosition" />
        private Vector3 _lastParentPosition;
        protected Vector3 lastParentPosition
        {
            get
            {
                return _lastParentPosition;
            }
            set
            {
                _lastParentPosition = value;
            }
        }

        #endregion lastParentPosition property

        #region ProjectionMatrix Property

        /// <summary>
        ///     Pre-calced projection matrix.
        /// </summary>
        /// <ogre name="mProjMatrix" />
        private Matrix4 _projectionMatrix;
        /// <summary>
        /// Gets the projection matrix for this frustum.
        /// </summary>
        /// <remarks>
        /// This Property returns the rendering-API dependent version of the projection
        /// matrix. If you want a 'typical' projection matrix then use StandardProjectionMatrix.
        /// </remarks>
        /// <ogre name="getProjectionmatrix" />
        public virtual Matrix4 ProjectionMatrix
        {
            get
            {
                UpdateFrustum();

                return _projectionMatrix;
            }
            protected set
            {
                _projectionMatrix = value;
            }
        }

        #endregion ProjectionMatrix Property

        #region StandardProjectionMatrix Property
        /// <summary>
        ///     Pre-calced standard projection matrix.
        /// </summary>
        /// <ogre name="mStandardProjMatrix" />
        private Matrix4 _standardProjMatrix;
        /// <summary>
        ///    Gets the 'standard' projection matrix for this camera, ie the 
        ///    projection matrix which conforms to standard right-handed rules.
        /// </summary>
        /// <remarks>
        ///    This differs from the rendering-API dependent ProjectionMatrix
        ///    in that it always returns a right-handed projection matrix result 
        ///    no matter what rendering API is being used - this is required for
        ///    vertex and fragment programs for example. However, the resulting depth
        ///    range may still vary between render systems since D3D uses [0,1] and 
        ///    GL uses [-1,1], and the range must be kept the same between programmable
        ///    and fixed-function pipelines.
        /// </remarks>
        /// <ogre name-"getStandardProjectionMatrix" />
        public virtual Matrix4 StandardProjectionMatrix
        {
            get
            {
                UpdateFrustum();

                return _standardProjMatrix;
            }
            protected set
            {
                _standardProjMatrix = value;
            }
        }

        #endregion StandardProjectionMatrix Property

        #region ViewMatrix Property

        /// <summary>
        ///     Pre-calced view matrix.
        /// </summary>
        /// <ogre name="mViewMatrix" />
        private Matrix4 _viewMatrix;
        /// <summary>
        ///     Gets the view matrix for this frustum. Mainly for use by the engine internally.
        /// </summary>
        /// <ogre name="getViewMatrix" />
        public virtual Matrix4 ViewMatrix
        {
            get
            {
                UpdateView();

                return _viewMatrix;
            }
            protected set
            {
                _viewMatrix = value;
            }
        }

        #endregion ViewMatrix Property

        #region recalculateFrustum Property

        /// <summary>
        ///     Something's changed in the frustum shape?
        /// </summary>
        /// <ogre name="mRecalcFrustrum" />
        private bool _recalculateFrustum;
        protected bool recalculateFrustum
        {
            get
            {
                return _recalculateFrustum;
            }
            set
            {
                _recalculateFrustum = value;
            }
        }

        #endregion recalculateFrustum Property

        #region recalculateView Property

        /// <summary>
        ///     Something in the view pos has changed?
        /// </summary>
        /// <ogre name="mRecalcView" />
        private bool _recalculateView;
        protected bool recalculateView
        {
            get
            {
                return _recalculateView;
            }
            set
            {
                _recalculateView = value;
            }
        }

        #endregion recalculateView Property



        


        #region vertexData Property

        /// <summary>
        ///     Vertex info for rendering this frustum.
        /// </summary>
        private VertexData _vertexData = new VertexData();
        protected VertexData vertexData
        {
            get
            {
                return _vertexData;
            }
            set
            {
                _vertexData = value;
            }
        }

        #endregion vertexData Property


        /// <summary>
        ///     Material to use when rendering this frustum.
        /// </summary>
        private Material _material;


        #region worldSpaceCorners Property

        /// <summary>
        ///		Frustum corners in world space.
        /// </summary>
        private Vector3[] _worldSpaceCorners = new Vector3[8];
        /// <summary>
        ///     Gets the world space corners of the frustum.
        /// </summary>
        /// <remarks>
        ///     The corners are ordered as follows: top-right near,
        ///     top-left near, bottom-left near, bottom-right near,
        ///     top-right far, top-left far, bottom-left far, bottom-right far.
        /// </remarks>
        public Vector3[] WorldSpaceCorners
        {
            get
            {
                UpdateView();

                return _worldSpaceCorners;
            }
            protected set
            {
                _worldSpaceCorners = value;
            }
        }

        #endregion worldSpaceCorners Property

        #region Temp Coeefficient Properties

        /** Temp coefficient values calculated from a frustum change,
			used when establishing the frustum planes when the view changes. */
        /// <ogre name="mCoeffl" />
        private Real[] _coeffL = new Real[2];
        protected Real[] coeffL
        {
            get
            {
                return _coeffL;
            }
            set
            {
                _coeffL = value;
            }
        }

        /// <ogre name="mCoeffR" />
        private Real[] _coeffR = new Real[2];
        protected Real[] coeffR
        {
            get
            {
                return _coeffR;
            }
            set
            {
                _coeffR = value;
            }
        }

        /// <ogre name="mCoeffB" />
        private Real[] _coeffB = new Real[2];
        protected Real[] coeffB
        {
            get
            {
                return _coeffB;
            }
            set
            {
                _coeffB = value;
            }
        }

        /// <ogre name="mCoeffT" />
        private Real[] _coeffT = new Real[2];
        protected Real[] coeffT
        {
            get
            {
                return _coeffT;
            }
            set
            {
                _coeffT = value;
            }
        }

        #endregion Temp Coeefficient Properties

        #region IsReflected Property

        /// <summary>
        ///		Is this frustum to act as a reflection of itself?
        /// </summary>
        /// <ogre name="mReflect" />
        private bool _isReflected;
        /// <summary>
        ///     Gets a flag that specifies whether this camera is being reflected or not.
        /// </summary>
        /// <ogre name="isReflected" />
        public virtual bool IsReflected
        {
            get
            {
                return _isReflected;
            }
            protected set
            {
                _isReflected = value;
            }
        }

        #endregion IsReflected Property

        #region ReflectionMatrix Property

        /// <summary>
        ///		Derive reflection matrix.
        /// </summary>
        /// <ogre name="mReflectmatrix" />
        private Matrix4 _reflectionMatrix;
        /// <summary>
        ///     Returns the reflection matrix of the frustum if appropriate.
        /// </summary>
        /// <ogre name="getReflectionMatrix" />
        public virtual Matrix4 ReflectionMatrix
        {
            get
            {
                return _reflectionMatrix;
            }
            protected set
            {
                _reflectionMatrix = value;
            }
        }

        #endregion ReflectionMatrix Property

        #region ReflectionPlane Property

        /// <summary>
        ///		Fixed reflection.
        /// </summary>
        /// <ogre name="mReflectPlane" />
        private Plane _reflectionPlane;
        /// <summary>
        ///     Returns the reflection plane of the frustum if appropriate.
        /// </summary>
        /// <ogre name="getReflectionPlane" />
        public virtual Plane ReflectionPlane
        {
            get
            {
                return _reflectionPlane;
            }
            protected set
            {
                _reflectionPlane = value;
            }
        }

        #endregion ReflectionPlane Property

        #region linkedReflectionPlane Property

        /// <summary>
        ///		Reference of a reflection plane (automatically updated).
        /// </summary>
        /// <ogre name="mLinkedReflectPlane" />
        private IDerivedPlaneProvider _linkedReflectionPlane;
        protected IDerivedPlaneProvider linkedReflectionPlane
        {
            get
            {
                return _linkedReflectionPlane;
            }
            set
            {
                _linkedReflectionPlane = value;
            }
        }

        #endregion linkedReflectionPlane Property

        #region lastLinkedReflectionPlane Property

        /// <summary>
        ///		Record of the last world-space reflection plane info used.
        /// </summary>
        /// <ogre name="mLastLinkedReflectionPlane" />
        private Plane _lastLinkedReflectionPlane;
        protected Plane lastLinkedReflectionPlane
        {
            get
            {
                return _lastLinkedReflectionPlane;
            }
            set
            {
                _lastLinkedReflectionPlane = value;
            }
        }

        #endregion lastLinkedReflectionPlane Property

        #region useObliqueDepthProjection Property

        /// <summary>
        ///		Is this frustum using an oblique depth projection?
        /// </summary>
        /// <ogre name="mObliqueDepthProjection" />
        private bool _useObliqueDepthProjection;
        protected bool useObliqueDepthProjection
        {
            get
            {
                return _useObliqueDepthProjection;
            }
            set
            {
                _useObliqueDepthProjection = value;
            }
        }

        #endregion

        #region obliqueProjPlane Property

        /// <summary>
        ///		Fixed oblique projection plane.
        /// </summary>
        /// <ogre name="mObliqueProjPlane" />
        private Plane _obliqueProjPlane;
        protected Plane obliqueProjPlane
        {
            get
            {
                return _obliqueProjPlane;
            }
            set
            {
                _obliqueProjPlane = value;
            }
        }

        #endregion obliqueProjPlane Property

        #region linkedObliqueProjPlane Property

        /// <summary>
        ///		Reference to oblique projection plane (automatically updated).
        /// </summary>
        /// <ogre name="mLinkedObliqueProjPlane" />
        private IDerivedPlaneProvider _linkedObliqueProjPlane;
        protected IDerivedPlaneProvider linkedObliqueProjPlane
        {
            get
            {
                return _linkedObliqueProjPlane;
            }
            set
            {
                _linkedObliqueProjPlane = value;
            }
        }

        #endregion linkedObliqueProjPlane Property

        #region lastLinkedObliqueProjPlane Property

        /// <summary>
        ///		Record of the last world-space oblique depth projection plane info used.
        /// </summary>
        /// <ogre name="mLastLinkedObliqueProjPlane" />
        private Plane _lastLinkedObliqueProjPlane;
        protected Plane lastLinkedObliqueProjPlane
        {
            get
            {
                return _lastLinkedObliqueProjPlane;
            }
            set
            {
                _lastLinkedObliqueProjPlane = value;
            }
        }

        #endregion lastlinkedObliqueProjPlane Property

        #region IRenderable fields

        /// <summary>
        ///     Dummy list for IRenderable.Lights since we wont be lit.
        /// </summary>
        private LightList _dummyLightList = new LightList();

        private Hashtable _customParams = new Hashtable();
        protected Hashtable customParams
        {
            get
            {
                return _customParams;
            }
            set
            {
                _customParams = value;
            }
        }

        #endregion IRenderable fields
        #endregion Fields

        #region Constructors

        /// <summary>
        ///     Default constructor.
        /// </summary>
        public Frustum()
        {
            for ( int i = 0; i < 6; i++ )
            {
                planes[i] = new Plane();
            }

            fieldOfView = (Real)(new Radian( Utility.PI / 4.0f ).InDegrees);
            nearDistance = 100.0f;
            farDistance = 100000.0f;
            aspectRatio = 1.33333333333333f;

            recalculateFrustum = true;
            recalculateView = true;

            // Init matrices
            viewMatrix = Matrix4.Zero;
            projectionMatrix = Matrix4.Zero;

            projectionType = Projection.Perspective;

            lastParentPosition = Vector3.Zero;
            lastParentOrientation = Quaternion.Identity;

            // init vertex data
            _vertexData.vertexDeclaration.AddElement( 0, 0, VertexElementType.Float3, VertexElementSemantic.Position );
            _vertexData.vertexStart = 0;
            _vertexData.vertexCount = 32;
            _vertexData.vertexBufferBinding.SetBinding( 0,
                HardwareBufferManager.Instance.CreateVertexBuffer( 4 * 3, _vertexData.vertexCount, BufferUsage.DynamicWriteOnly ) );

            // Initialize material
            _material = MaterialManager.Instance.GetByName( "BaseWhite" );

            // Alter baseclass members
            isVisible = false;
            parentNode = null;

            UpdateView();
        }

        #endregion

        #region Properties

        /// <summary>
        ///		Evaluates whether or not the view frustum is out of date.
        /// </summary>
        /// <ogre name="isFrustumOutOfDate" />
        protected virtual bool IsFrustumOutOfDate
        {
            get
            {
                // deriving custom near plane from linked plane?
                bool returnVal = false;

                if ( _useObliqueDepthProjection )
                {
                    // always out of date since plane needs to be in view space
                    returnVal = true;

                    // update derived plane
                    if ( _linkedObliqueProjPlane != null &&
                        !( _lastLinkedObliqueProjPlane == _linkedObliqueProjPlane.DerivedPlane ) )
                    {

                        _obliqueProjPlane = _linkedObliqueProjPlane.DerivedPlane;
                        _lastLinkedObliqueProjPlane = _obliqueProjPlane;
                    }
                }

                return _recalculateFrustum || returnVal;
            }
        }

        /// <summary>
        ///		Gets whether or not the view matrix is out of date.
        /// </summary>
        /// <ogre name="isViewOutOfDate" />
        protected virtual bool IsViewOutOfDate
        {
            get
            {
                bool returnVal = false;

                // are we attached to another node?
                if ( parentNode != null )
                {
                    if ( !recalculateView && parentNode.DerivedOrientation == _lastParentOrientation &&
                        parentNode.DerivedPosition == _lastParentPosition )
                    {
                        returnVal = false;
                    }
                    else
                    {
                        // we are out of date with the parent scene node
                        _lastParentOrientation = parentNode.DerivedOrientation;
                        _lastParentPosition = parentNode.DerivedPosition;
                        returnVal = true;
                    }
                }

                // deriving direction from linked plane?
                if ( _isReflected && _linkedReflectionPlane != null &&
                    !( _lastLinkedReflectionPlane == _linkedReflectionPlane.DerivedPlane ) )
                {

                    _reflectionPlane = _linkedReflectionPlane.DerivedPlane;
                    _reflectionMatrix = Utility.BuildReflectionMatrix( _reflectionPlane );
                    _lastLinkedReflectionPlane = _linkedReflectionPlane.DerivedPlane;
                    returnVal = true;
                }

                return _recalculateView || returnVal;
            }
        }




        #endregion Properties

        #region Methods

        /// <summary>
        ///		Disables any custom near clip plane.
        /// </summary>
        /// <ogre name="disableCustomNearClipPlane" />
        public virtual void DisableCustomNearClipPlane()
        {
            _useObliqueDepthProjection = false;
            _linkedObliqueProjPlane = null;
            InvalidateFrustum();
        }

        /// <summary>
        ///     Disables reflection modification previously turned on with <see cref="EnableReflection"/>.
        /// </summary>
        /// <ogre name="disableReflection" />
        public virtual void DisableReflection()
        {
            _isReflected = false;
            _lastLinkedReflectionPlane.Normal = Vector3.Zero;
            InvalidateView();
        }

        /// <summary>
        ///		Links the frustum to a custom near clip plane, which can be used
        ///		to clip geometry in a custom manner without using user clip planes.
        /// </summary>
        /// <remarks>
        ///		<para>
        ///		There are several applications for clipping a scene arbitrarily by
        ///		a single plane; the most common is when rendering a reflection to 
        ///		a texture, and you only want to render geometry that is above the 
        ///		water plane (to do otherwise results in artefacts). Whilst it is
        ///		possible to use user clip planes, they are not supported on all
        ///		cards, and sometimes are not hardware accelerated when they are
        ///		available. Instead, where a single clip plane is involved, this
        ///		technique uses a 'fudging' of the near clip plane, which is 
        ///		available and fast on all hardware, to perform as the arbitrary
        ///		clip plane. This does change the shape of the frustum, leading 
        ///		to some depth buffer loss of precision, but for many of the uses of
        ///		this technique that is not an issue.</para>
        ///		<para>
        ///		This version of the method links to a plane, rather than requiring
        ///		a by-value plane definition, and therefore you can 
        ///		make changes to the plane (e.g. by moving / rotating the node it is
        ///		attached to) and they will automatically affect this object.
        ///		</para>
        ///		<para>This technique only works for perspective projection.</para>
        /// </remarks>
        /// <param name="plane">The plane to link to to perform the clipping.</param>
        /// <ogre name="enableCustomNearClipPlane" />
        public virtual void EnableCustomNearClipPlane( IDerivedPlaneProvider plane )
        {
            _useObliqueDepthProjection = true;
            _linkedObliqueProjPlane = plane;
            _obliqueProjPlane = plane.DerivedPlane;
            InvalidateFrustum();
        }

        /// <summary>
        ///		Links the frustum to a custom near clip plane, which can be used
        ///		to clip geometry in a custom manner without using user clip planes.
        /// </summary>
        /// <remarks>
        ///		<p>
        ///		There are several applications for clipping a scene arbitrarily by
        ///		a single plane; the most common is when rendering a reflection to 
        ///		a texture, and you only want to render geometry that is above the 
        ///		water plane (to do otherwise results in artefacts). Whilst it is
        ///		possible to use user clip planes, they are not supported on all
        ///		cards, and sometimes are not hardware accelerated when they are
        ///		available. Instead, where a single clip plane is involved, this
        ///		technique uses a 'fudging' of the near clip plane, which is 
        ///		available and fast on all hardware, to perform as the arbitrary
        ///		clip plane. This does change the shape of the frustum, leading 
        ///		to some depth buffer loss of precision, but for many of the uses of
        ///		this technique that is not an issue.</p>
        ///		<p>
        ///		This version of the method links to a plane, rather than requiring
        ///		a by-value plane definition, and therefore you can 
        ///		make changes to the plane (e.g. by moving / rotating the node it is
        ///		attached to) and they will automatically affect this object.
        ///		</p>
        ///		<p>This technique only works for perspective projection.</p>
        /// </remarks>
        /// <param name="plane">The plane to link to to perform the clipping.</param>
        /// <ogre name="enableCustomNearClipPlane" />
        public virtual void EnableCustomNearClipPlane( Plane plane )
        {
            _useObliqueDepthProjection = true;
            _linkedObliqueProjPlane = null;
            _obliqueProjPlane = plane;
            InvalidateFrustum();
        }

        /// <summary>
        ///     Modifies this camera so it always renders from the reflection of itself through the
        ///     plane specified.
        /// </summary>
        /// <remarks>
        ///     This is obviously useful for rendering planar reflections.
        /// </remarks>
        /// <param name="plane"></param>
        /// <ogre name="enableReflection" />
        public virtual void EnableReflection( Plane plane )
        {
            _isReflected = true;
            _reflectionPlane = plane;
            _linkedReflectionPlane = null;
            _reflectionMatrix = Utility.BuildReflectionMatrix( plane );
            InvalidateView();
        }

        /// <summary>
        ///		Modifies this frustum so it always renders from the reflection of itself through the
        ///		plane specified. Note that this version of the method links to a plane
        ///		so that changes to it are picked up automatically.
        /// </summary>
        /// <remarks>This is obviously useful for performing planar reflections.</remarks>
        /// <param name="plane"></param>
        /// <ogre name="enableReflection" />
        public virtual void EnableReflection( IDerivedPlaneProvider plane )
        {
            _isReflected = true;
            _linkedReflectionPlane = plane;
            _reflectionPlane = _linkedReflectionPlane.DerivedPlane;
            _reflectionMatrix = Utility.BuildReflectionMatrix( _reflectionPlane );
            _lastLinkedReflectionPlane = _reflectionPlane;
            InvalidateView();
        }

        /// <summary>
        ///		Get the derived position of this frustum.
        /// </summary>
        /// <returns></returns>
        /// <ogre name="getPositionForViewUpdate" />
        protected virtual Vector3 GetPositionForViewUpdate()
        {
            return _lastParentPosition;
        }

        /// <summary>
        ///		Get the derived orientation of this frustum.
        /// </summary>
        /// <returns></returns>
        /// <ogre name="getOrientationForViewUpdate" />
        protected virtual Quaternion GetOrientationForViewUpdate()
        {
            return _lastParentOrientation;
        }

        /// <summary>
        ///		Overloaded method.
        /// </summary>
        /// <param name="box"></param>
        /// <returns></returns>
        /// <ogre name="isVisible" />
        public bool IsObjectVisible( AxisAlignedBox box )
        {
            // this overload doesnt care about the clipping plane, but we gotta
            // pass in something to the out param anyway
            FrustumPlane dummy;
            return IsObjectVisible( box, out dummy );
        }

        /// <summary>
        ///		Tests whether the given box is visible in the Frustum.
        ///	 </summary>
        /// <param name="box"> Bounding box to be checked.</param>
        /// <param name="culledBy">
        ///		Optional FrustrumPlane params which will be filled by the plane which culled
        ///		the box if the result was false.
        ///	</param>
        /// <returns>True if the box is visible, otherwise false.</returns>
        /// <ogre name="isVisible" />
        public bool IsObjectVisible( AxisAlignedBox box, out FrustumPlane culledBy )
        {
            // Null boxes are always invisible
            if ( box.IsNull )
            {
                culledBy = FrustumPlane.None;
                return false;
            }

            // Make any pending updates to the calculated frustum
            UpdateView();

            // Get corners of the box
            Vector3[] corners = box.Corners;

            // For each plane, see if all points are on the negative side
            // If so, object is not visible
            for ( int plane = 0; plane < 6; plane++ )
            {
                // skip far plane if infinite view frustum
                if ( farDistance == 0 && plane == (int)FrustumPlane.Far )
                {
                    continue;
                }

                if ( planes[plane].GetSide( corners[0] ) == Plane.Side.Negative &&
                    planes[plane].GetSide( corners[1] ) == Plane.Side.Negative &&
                    planes[plane].GetSide( corners[2] ) == Plane.Side.Negative &&
                    planes[plane].GetSide( corners[3] ) == Plane.Side.Negative &&
                    planes[plane].GetSide( corners[4] ) == Plane.Side.Negative &&
                    planes[plane].GetSide( corners[5] ) == Plane.Side.Negative &&
                    planes[plane].GetSide( corners[6] ) == Plane.Side.Negative &&
                    planes[plane].GetSide( corners[7] ) == Plane.Side.Negative )
                {
                    // ALL corners on negative side therefore out of view
                    culledBy = (FrustumPlane)plane;
                    return false;
                }
            }

            // box is not culled
            culledBy = FrustumPlane.None;
            return true;
        }

        /// <summary>
        ///		Overloaded method.
        /// </summary>
        /// <param name="box"></param>
        /// <returns></returns>
        /// <ogre name="isVisible" />
        public bool IsObjectVisible( Sphere sphere )
        {
            // this overload doesnt care about the clipping plane, but we gotta
            // pass in something to the out param anyway
            FrustumPlane dummy;
            return IsObjectVisible( sphere, out dummy );
        }

        /// <summary>
        ///		Tests whether the given sphere is in the viewing frustum.
        /// </summary>
        /// <param name="sphere">Bounding sphere to be checked.</param>
        /// <param name="culledBy">
        ///		Optional FrustrumPlane params which will be filled by the plane which culled
        ///		the box if the result was false.
        ///	</param>
        /// <returns>True if the box is visible, otherwise false.</returns>
        /// <ogre name="isVisible" />
        public bool IsObjectVisible( Sphere sphere, out FrustumPlane culledBy )
        {
            // Make any pending updates to the calculated frustum
            UpdateView();

            // For each plane, see if sphere is on negative side
            // If so, object is not visible
            for ( int plane = 0; plane < 6; plane++ )
            {
                // Skip far plane if infinite view frustum
                if ( farDistance == 0 && plane == (int)FrustumPlane.Far )
                {
                    continue;
                }

                // If the distance from sphere center to plane is negative, and 'more negative' 
                // than the radius of the sphere, sphere is outside frustum
                if ( planes[plane].GetDistance( sphere.Center ) < -sphere.Radius )
                {
                    // ALL corners on negative side therefore out of view
                    culledBy = (FrustumPlane)plane;
                    return false;
                }
            }

            // sphere is not culled
            culledBy = FrustumPlane.None;
            return true;
        }

        /// <summary>
        ///		Overloaded method.
        /// </summary>
        /// <param name="box"></param>
        /// <returns></returns>
        /// <oge name="isVisible" />
        public bool IsObjectVisible( Vector3 vertex )
        {
            // this overload doesnt care about the clipping plane, but we gotta
            // pass in something to the out param anyway
            FrustumPlane dummy;
            return IsObjectVisible( vertex, out dummy );
        }

        /// <summary>
        ///		Tests whether the given 3D point is in the viewing frustum.
        /// </summary>
        /// <param name="vector">3D point to check for frustum visibility.</param>
        /// <param name="culledBy">
        ///		Optional FrustrumPlane params which will be filled by the plane which culled
        ///		the box if the result was false.
        ///	</param>
        /// <returns>True if the box is visible, otherwise false.</returns>
        /// <ogre name="isVisible" />
        public bool IsObjectVisible( Vector3 vertex, out FrustumPlane culledBy )
        {
            // Make any pending updates to the calculated frustum
            UpdateView();

            // For each plane, see if all points are on the negative side
            // If so, object is not visible
            for ( int plane = 0; plane < 6; plane++ )
            {
                if ( farDistance == 0 && plane == (int)FrustumPlane.Far )
                {
                    continue;
                }

                if ( planes[plane].GetSide( vertex ) == Plane.Side.Negative )
                {
                    // ALL corners on negative side therefore out of view
                    culledBy = (FrustumPlane)plane;
                    return false;
                }
            }

            // vertex is not culled
            culledBy = FrustumPlane.None;
            return true;
        }

        
        /// <summary>
        ///     Project a sphere onto the near plane and get the bounding rectangle.
        /// </summary>
        /// <param name="sphere">The world-space sphere to project</param>
        /// <param name="left"></param>
        /// <param name="top"></param>
        /// <param name="right"></param>
        /// <param name="bottom"></param>
        /// <returns>true if the sphere was projected to a subset of the near plane,
        ///     false if the entire near plane was contained.</returns>
        public virtual bool ProjectSphere( Sphere sphere, out Real left, out Real top, out Real right, out Real bottom )
        {
            // initialise
            left = bottom = -1.0f;
            right = top = 1.0f;

            // Transform light position into camera space
            Vector3 eyeSpacePos = this.ViewMatrix * sphere.Center;

            if ( eyeSpacePos.z < 0 )
            {
                Real r = sphere.Radius;
                // early-exit
                if ( eyeSpacePos.LengthSquared <= r * r )
                    return false;

                Vector3 screenSpacePos = this.StandardProjectionMatrix * eyeSpacePos;

                // perspective attenuate
                Vector3 spheresize = new Vector3( r, r, eyeSpacePos.z );
                spheresize = this.StandardProjectionMatrix * spheresize;

                Real possLeft = screenSpacePos.x - spheresize.x;
                Real possRight = screenSpacePos.x + spheresize.x;
                Real possTop = screenSpacePos.y + spheresize.y;
                Real possBottom = screenSpacePos.y - spheresize.y;

                left = Utility.Max( -1.0f, possLeft );
                right = Utility.Min( 1.0f, possRight );
                top = Utility.Min( 1.0f, possTop );
                bottom = Utility.Max( -1.0f, possBottom );
            }

            return ( left != -1.0f ) || ( top != 1.0f ) || ( right != 1.0f ) || ( bottom != -1.0f );
        }

        /// <summary>
        ///     Signal to update frustum information.
        /// </summary>
        /// <ogre name="invalidateFrustum" />
        protected virtual void InvalidateFrustum()
        {
            _recalculateFrustum = true;
        }

        /// <summary>
        ///     Signal to update view information.
        /// </summary>
        /// <ogre name="invalidateView" />
        protected virtual void InvalidateView()
        {
            _recalculateView = true;
        }

        /// <summary>
        ///		Updates the frustum data.
        /// </summary>
        /// <ogre name="updateFrustum" />
        protected virtual void UpdateFrustum()
        {
            if ( IsFrustumOutOfDate )
            {
                Real thetaY = (Real)(new Degree( new Real(fieldOfView * 0.5f) ).InRadians);
                Real tanThetaY = Utility.Tan( (Real)thetaY );
                Real tanThetaX = tanThetaY * aspectRatio;
                Real vpTop = tanThetaY * nearDistance;
                Real vpRight = tanThetaX * nearDistance;
                Real vpBottom = -vpTop;
                Real vpLeft = -vpRight;

                // grab a reference to the current render system
                RenderSystem renderSystem = Root.Instance.RenderSystem;

                // Recalc if frustum params chagned
                if ( projectionType == Projection.Perspective )
                {
                    // perspective transform, API specific
                    renderSystem.MakeProjectionMatrix( _fieldOfView, _aspectRatio, _nearDistance, _farDistance, ref _projectionMatrix );

                    // perspective transform, API specific for GPU programs
                    renderSystem.MakeProjectionMatrix( _fieldOfView, _aspectRatio, _nearDistance, _farDistance, ref _standardProjMatrix, true );
                    
                    if ( _useObliqueDepthProjection )
                    {
                        // translate the plane into view space
                        Plane viewSpaceNear = _viewMatrix * _obliqueProjPlane;

                        renderSystem.ApplyObliqueDepthProjection( ref _projectionMatrix, viewSpaceNear, false );
                        renderSystem.ApplyObliqueDepthProjection( ref _standardProjMatrix, viewSpaceNear, true );
                    }
                }
                else if ( projectionType == Projection.Orthographic )
                {
                    // orthographic projection, API specific
                    renderSystem.MakeOrthoMatrix( _fieldOfView, _aspectRatio, _nearDistance, _farDistance, ref _projectionMatrix );
                    // orthographic projection, API specific for GPU programs
                    renderSystem.MakeOrthoMatrix( _fieldOfView, _aspectRatio, _nearDistance, _farDistance, ref _standardProjMatrix, true );
                }

                // Calculate bounding box (local)
                // Box is from 0, down -Z, max dimensions as determined from far plane
                // If infinite view frustum, use a far value
                Real actualFar = ( _farDistance == 0 ) ? InfiniteFarPlaneDistance : _farDistance;
                Real farTop = tanThetaY * ( ( _projectionType == Projection.Orthographic ) ? _nearDistance : actualFar );
                Real farRight = tanThetaX * ( ( _projectionType == Projection.Orthographic ) ? _nearDistance : actualFar );
                Real farBottom = -farTop;
                Real farLeft = -farRight;
                Vector3 min = new Vector3( -farRight, -farTop, 0 );
                Vector3 max = new Vector3( farRight, farTop, -actualFar );
                _boundingBox.SetExtents( min, max );

                // Calculate vertex positions
                // 0 is the origin
                // 1, 2, 3, 4 are the points on the near plane, top left first, clockwise
                // 5, 6, 7, 8 are the points on the far plane, top left first, clockwise
                HardwareVertexBuffer buffer = _vertexData.vertexBufferBinding.GetBuffer( 0 );

                IntPtr posPtr = buffer.Lock( BufferLocking.Discard );

                unsafe
                {
                    float* pPos = (float*)posPtr.ToPointer();

                    // near plane (remember frustum is going in -Z direction)
                    *pPos++ = vpLeft;
                    *pPos++ = vpTop;
                    *pPos++ = -_nearDistance;
                    *pPos++ = vpRight;
                    *pPos++ = vpTop;
                    *pPos++ = -_nearDistance;

                    *pPos++ = vpRight;
                    *pPos++ = vpTop;
                    *pPos++ = -_nearDistance;
                    *pPos++ = vpRight;
                    *pPos++ = vpBottom;
                    *pPos++ = -_nearDistance;

                    *pPos++ = vpRight;
                    *pPos++ = vpBottom;
                    *pPos++ = -_nearDistance;
                    *pPos++ = vpLeft;
                    *pPos++ = vpBottom;
                    *pPos++ = -_nearDistance;

                    *pPos++ = vpLeft;
                    *pPos++ = vpBottom;
                    *pPos++ = -_nearDistance;
                    *pPos++ = vpLeft;
                    *pPos++ = vpTop;
                    *pPos++ = -_nearDistance;

                    // far plane (remember frustum is going in -Z direction)
                    *pPos++ = farLeft;
                    *pPos++ = farTop;
                    *pPos++ = -actualFar;
                    *pPos++ = farRight;
                    *pPos++ = farTop;
                    *pPos++ = -actualFar;

                    *pPos++ = farRight;
                    *pPos++ = farTop;
                    *pPos++ = -actualFar;
                    *pPos++ = farRight;
                    *pPos++ = farBottom;
                    *pPos++ = -actualFar;

                    *pPos++ = farRight;
                    *pPos++ = farBottom;
                    *pPos++ = -actualFar;
                    *pPos++ = farLeft;
                    *pPos++ = farBottom;
                    *pPos++ = -actualFar;

                    *pPos++ = farLeft;
                    *pPos++ = farBottom;
                    *pPos++ = -actualFar;
                    *pPos++ = farLeft;
                    *pPos++ = farTop;
                    *pPos++ = -actualFar;

                    // Sides of the pyramid
                    *pPos++ = 0.0f;
                    *pPos++ = 0.0f;
                    *pPos++ = 0.0f;
                    *pPos++ = vpLeft;
                    *pPos++ = vpTop;
                    *pPos++ = -_nearDistance;

                    *pPos++ = 0.0f;
                    *pPos++ = 0.0f;
                    *pPos++ = 0.0f;
                    *pPos++ = vpRight;
                    *pPos++ = vpTop;
                    *pPos++ = -_nearDistance;

                    *pPos++ = 0.0f;
                    *pPos++ = 0.0f;
                    *pPos++ = 0.0f;
                    *pPos++ = vpRight;
                    *pPos++ = vpBottom;
                    *pPos++ = -_nearDistance;

                    *pPos++ = 0.0f;
                    *pPos++ = 0.0f;
                    *pPos++ = 0.0f;
                    *pPos++ = vpLeft;
                    *pPos++ = vpBottom;
                    *pPos++ = -_nearDistance;

                    // Sides of the box
                    *pPos++ = vpLeft;
                    *pPos++ = vpTop;
                    *pPos++ = -_nearDistance;
                    *pPos++ = farLeft;
                    *pPos++ = farTop;
                    *pPos++ = -actualFar;

                    *pPos++ = vpRight;
                    *pPos++ = vpTop;
                    *pPos++ = -_nearDistance;
                    *pPos++ = farRight;
                    *pPos++ = farTop;
                    *pPos++ = -actualFar;

                    *pPos++ = vpRight;
                    *pPos++ = vpBottom;
                    *pPos++ = -_nearDistance;
                    *pPos++ = farRight;
                    *pPos++ = farBottom;
                    *pPos++ = -actualFar;

                    *pPos++ = vpLeft;
                    *pPos++ = vpBottom;
                    *pPos++ = -_nearDistance;
                    *pPos++ = farLeft;
                    *pPos++ = farBottom;
                    *pPos++ = -actualFar;
                }

                // don't forget to unlock!
                buffer.Unlock();

                _recalculateFrustum = false;
            }
        }


        /// <summary>
        ///		Updates the view matrix.
        /// </summary>
        /// <ogre name="updateView" />
        protected virtual void UpdateView()
        {
            // check if the view is out of date
            if ( IsViewOutOfDate )
            {
                // View matrix is:
                //
                //  [ Lx  Uy  Dz  Tx  ]
                //  [ Lx  Uy  Dz  Ty  ]
                //  [ Lx  Uy  Dz  Tz  ]
                //  [ 0   0   0   1   ]
                //
                // Where T = -(Transposed(Rot) * Pos)

                // This is most efficiently done using 3x3 Matrices

                // Get orientation from quaternion
                Quaternion orientation = GetOrientationForViewUpdate();
                Vector3 position = GetPositionForViewUpdate();
                Matrix3 rotation = orientation.ToRotationMatrix();
                /*
                Vector3 left = rotation.GetColumn( 0 );
                Vector3 up = rotation.GetColumn( 1 );
                Vector3 direction = rotation.GetColumn( 2 );
                */

                // make the translation relative to the new axis
                Matrix3 rotationT = rotation.Transpose();
                Vector3 translation = -rotationT * position;

                _viewMatrix = Matrix4.Identity;

                // initialize the upper 3x3 portion with the rotation
                _viewMatrix = rotationT;

                // add the translation portion, add set 1 for the bottom right portion
                _viewMatrix.m03 = translation.x;
                _viewMatrix.m13 = translation.y;
                _viewMatrix.m23 = translation.z;
                //_viewMatrix.m33 = 1.0f;

                // deal with reflections
                if ( _isReflected )
                {
                    _viewMatrix = _viewMatrix * _reflectionMatrix;
                }

                // update the frustum planes
                UpdateFrustum();
                
                /*
                // Use camera view for frustum calcs, using -Z rather than Z
                Vector3 camDirection = orientation * -Vector3.UnitZ;

                // calculate distance along direction to our derived position
                float distance = camDirection.DotProduct( position );
                */

                Vector3 newpos = position;
                if ( _isReflected )
                {
                    newpos = _reflectionMatrix * newpos;
                }

                Matrix4 combo = _standardProjMatrix * _viewMatrix;

                _planes[(int)FrustumPlane.Left].Normal.x = combo.m30 + combo.m00;
                _planes[(int)FrustumPlane.Left].Normal.y = combo.m31 + combo.m01;
                _planes[(int)FrustumPlane.Left].Normal.z = combo.m32 + combo.m02;
                _planes[ (int)FrustumPlane.Left ].Distance = combo.m33 + combo.m03;
                
                _planes[(int)FrustumPlane.Right].Normal.x = combo.m30 - combo.m00;
                _planes[(int)FrustumPlane.Right].Normal.y = combo.m31 - combo.m01;
                _planes[(int)FrustumPlane.Right].Normal.z = combo.m32 - combo.m02;
                _planes[ (int)FrustumPlane.Right ].Distance = combo.m33 - combo.m03;

                _planes[(int)FrustumPlane.Top].Normal.x = combo.m30 - combo.m10;
                _planes[(int)FrustumPlane.Top].Normal.y = combo.m31 - combo.m11;
                _planes[(int)FrustumPlane.Top].Normal.z = combo.m32 - combo.m12;
                _planes[ (int)FrustumPlane.Top ].Distance = combo.m33 - combo.m13;

                _planes[(int)FrustumPlane.Bottom].Normal.x = combo.m30 + combo.m10;
                _planes[(int)FrustumPlane.Bottom].Normal.y = combo.m31 + combo.m11;
                _planes[(int)FrustumPlane.Bottom].Normal.z = combo.m32 + combo.m12;
                _planes[ (int)FrustumPlane.Bottom ].Distance = combo.m33 + combo.m13;

                _planes[(int)FrustumPlane.Near].Normal.x = combo.m30 + combo.m20;
                _planes[(int)FrustumPlane.Near].Normal.y = combo.m31 + combo.m21;
                _planes[(int)FrustumPlane.Near].Normal.z = combo.m32 + combo.m22;
                //_planes[(int)FrustumPlane.Near].Distance = combo.m33 + combo.m23;
                _planes[(int)FrustumPlane.Near].Normal.Normalize();
                _planes[(int)FrustumPlane.Near].Distance = -( _planes[(int)FrustumPlane.Near].Normal.DotProduct( newpos ) + _nearDistance );

                _planes[(int)FrustumPlane.Far].Normal.x = combo.m30 - combo.m20;
                _planes[(int)FrustumPlane.Far].Normal.y = combo.m31 - combo.m21;
                _planes[(int)FrustumPlane.Far].Normal.z = combo.m32 - combo.m22;
                //planes[(int)FrustumPlane.Far].Distance = combo.m33 - combo.m23;
                _planes[(int)FrustumPlane.Far].Normal.Normalize();
                _planes[(int)FrustumPlane.Far].Distance = _planes[(int)FrustumPlane.Far].Normal.DotProduct( newpos ) - _farDistance;

                // renormalize any normals which were not unit length
                for ( int i = 0; i < 6; i++ )
                {
                    float length = _planes[i].Normal.Normalize();
                    _planes[ i ].Distance /= length;
                }

                // Update worldspace corners
                Matrix4 eyeToWorld = _viewMatrix.Inverse();

                // Get worldspace frustum corners
                // treat infinite far distance as some far value
                Real actualFar = ( _farDistance == 0 ) ? InfiniteFarPlaneDistance : _farDistance;
                Real y = Utility.Tan( new Real(_fieldOfView * 0.5f) );
                Real x = _aspectRatio * y;
                Real neary = y * _nearDistance;
                Real fary = y * ( ( _projectionType == Projection.Orthographic ) ? _nearDistance : actualFar );
                Real nearx = x * nearDistance;
                Real farx = x * ( ( _projectionType == Projection.Orthographic ) ? _nearDistance : actualFar );

                // near
                _worldSpaceCorners[0] = eyeToWorld * new Vector3( nearx, neary, -_nearDistance );
                _worldSpaceCorners[1] = eyeToWorld * new Vector3( -nearx, neary, -_nearDistance );
                _worldSpaceCorners[2] = eyeToWorld * new Vector3( -nearx, -neary, -_nearDistance );
                _worldSpaceCorners[3] = eyeToWorld * new Vector3( nearx, -neary, -_nearDistance );
                // far
                _worldSpaceCorners[4] = eyeToWorld * new Vector3( farx, fary, -actualFar );
                _worldSpaceCorners[5] = eyeToWorld * new Vector3( -farx, fary, -actualFar );
                _worldSpaceCorners[6] = eyeToWorld * new Vector3( -farx, -fary, -actualFar );
                _worldSpaceCorners[7] = eyeToWorld * new Vector3( farx, -fary, -actualFar );

                // update since we have now recalculated everything
                _recalculateView = false;
            }
        }

        #endregion Methods

        #region Overloaded operators

        /// <summary>
        ///		An indexer that accepts a FrustumPlane enum value and return the appropriate plane side of the Frustum.
        /// </summary>
        /// <ogre name="getFrustumPlane" />
        public Plane this[FrustumPlane plane]
        {
            get
            {
                // make any pending updates to the calculated frustum
                // TODO Was causing a stack overflow, revisit
                UpdateView();

                // convert the incoming plan enum type to a int
                int index = (int)plane;

                // access the planes array by index
                return planes[index];
            }
        }

        #endregion

        #region SceneObject Members

        // overridden from MovableObject
        /// <summary>
        ///    Local bounding radius of this camera.
        /// </summary>
        /// <ogre name="getBoundingRadius" />
        public override Real BoundingRadius
        {
            get
            {
                return ( _farDistance == 0 ) ? InfiniteFarPlaneDistance : _farDistance;
            }
        }

        /// <summary>
        ///     Bounding box of this frustum.
        /// </summary>
        private AxisAlignedBox _boundingBox = AxisAlignedBox.Null;
        //overridden from MovableObject
        /// <summary>
        ///     Returns the bounding box for this frustum.
        /// </summary>
        /// <ogre name="getBoundingBox" />
        public override AxisAlignedBox BoundingBox
        {
            get
            {
                return _boundingBox;
            }
            protected set
            {
                _boundingBox = value;
            }
        }

        // overridden from movableobject
        /// <summary>
        /// 
        /// </summary>
        /// <param name="camera"></param>
        /// <ogre name="_notifyCurrentCamera" />
        public override void NotifyCurrentCamera( Camera camera )
        {
            // do nothing
        }

        //Overridden from MovableObject
        /// <summary>
        ///     Implemented to add outself to the rendering queue.
        /// </summary>
        /// <param name="queue"></param>
        public override void UpdateRenderQueue( RenderQueue queue )
        {
            
            if ( isVisible )
            {   
                queue.AddRenderable( this );
            }
        }

        #endregion SceneObject Members

        #region IRenderable Members

        public bool CastsShadows
        {
            get
            {
                return false;
            }
        }

        //implements IRenderable
        /// <summary>
        ///     Returns the material to use when rendering this frustum.
        /// </summary>
        /// <ogre name="getMaterial" />
        public Material Material
        {
            get
            {
                return _material;
            }
            protected set
            {
                _material = value;
            }
        }

        /// <summary>
        ///     Just returns the best technique for our material.
        /// </summary>
        public Technique Technique
        {
            get
            {
                return material.GetBestTechnique();
            }
        }

        //implements IRenderable
        /// <ogre name="getRenderOperation" />
        public void GetRenderOperation( RenderOperation op )
        {
            UpdateView();
            UpdateFrustum();

            op.operationType = OperationType.LineList;
            op.useIndices = false;
            op.vertexData = _vertexData;
        }

        //implements IRenderable
        /// <ogre name="getWorldTransforms" />
        public void GetWorldTransforms( Matrix4[] matrices )
        {
            if ( parentNode != null )
            {
                parentNode.GetWorldTransforms( matrices );
            }
        }

        /// <summary>
        ///     Returns a dummy list since we won't be lit.
        /// </summary>
        /// <ogre name="getLights" />
        public LightList Lights
        {
            get
            {
                return _dummyLightList;
            }
            protected set
            {
                _dummyLightList = value;
            }
        }

        public bool NormalizeNormals
        {
            get
            {
                return false;
            }
        }

        public ushort NumWorldTransforms
        {
            get
            {
                return 1;
            }
        }

        public bool UseIdentityProjection
        {
            get
            {
                return false;
            }
        }

        public bool UseIdentityView
        {
            get
            {
                return false;
            }
        }

        public SceneDetailLevel RenderDetail
        {
            get
            {
                return SceneDetailLevel.Solid;
            }
        }

        //implements IRenderable
        /// <ogre name="getWorldOrientation" />
        public Quaternion WorldOrientation
        {
            get
            {
                if ( parentNode != null )
                {
                    return parentNode.DerivedOrientation;
                }
                else
                {
                    return Quaternion.Identity;
                }
            }
        }

        // implements IRenderable
        /// <ogre name="getWorldPosition" />
        public Vector3 WorldPosition
        {
            get
            {
                if ( parentNode != null )
                {
                    return parentNode.DerivedPosition;
                }
                else
                {
                    return Vector3.Zero;
                }
            }
        }

        // implements IRenderable
        /// <param name="camera"></param>
        /// <returns></returns>
        /// <ogre name="getSquaredViewDepth" />
        public Real GetSquaredViewDepth( Camera camera )
        {
            // calc from center
            if ( parentNode != null )
            {
                return ( camera.DerivedPosition - parentNode.DerivedPosition ).LengthSquared;
            }
            else
            {
                return 0;
            }
        }

        public Vector4 GetCustomParameter( int index )
        {
            if ( customParams[index] == null )
            {
                throw new Exception( "A parameter was not found at the given index" );
            }
            else
            {
                return (Vector4)customParams[index];
            }
        }

        public void SetCustomParameter( int index, Vector4 val )
        {
            customParams[index] = val;
        }

        public void UpdateCustomGpuParameter( GpuProgramParameters.AutoConstantEntry entry, GpuProgramParameters gpuParams )
        {
            if ( customParams[entry.data] != null )
            {
                gpuParams.SetConstant( entry.index, (Vector4)customParams[entry.data] );
            }
        }

        #endregion
    }
}
