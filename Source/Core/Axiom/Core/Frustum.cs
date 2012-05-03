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
using Axiom.Core.Collections;
using Axiom.Graphics;
using Axiom.Math;

#endregion Namespace Declarations

namespace Axiom.Core
{
	/// <summary>
	///     A frustum represents a pyramid, capped at the near and far end which is
	///     used to represent either a visible area or a projection area. Can be used
	///     for a number of applications.
	/// </summary>
	public class Frustum : MovableObject, IRenderable
	{
		#region Constants

		/// <summary>
		///		Small constant used to reduce far plane projection to avoid inaccuracies.
		/// </summary>
		public static readonly Real InfiniteFarPlaneAdjust = 0.00001f;

		/// <summary>
		///		Arbitrary large distance to use for the far plane when set to 0 (infinite far plane).
		/// </summary>
		public static readonly Real InfiniteFarPlaneDistance = 100000.0f;

		#endregion Constants

		#region Fields and Properties

		#region ProjectionType Property

		/// <summary>
		///		Perspective or Orthographic?
		/// </summary>
		private Projection _projectionType;

		/// <summary>
		///    Gets/Sets the type of projection to use (orthographic or perspective). Default is perspective.
		/// </summary>
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

		#region FieldOfView Property

		/// <summary>
		///     y-direction field-of-view (default 45).
		/// </summary>
		private Radian _fieldOfView;

		/// <summary>
		///		Sets the Y-dimension Field Of View (FOV) of the camera.
		/// </summary>
		/// <remarks>
		///		Field Of View (FOV) is the angle made between the camera's position, and the left &amp; right edges
		///		of the 'screen' onto which the scene is projected. High values (90+ Degrees) result in a wide-angle,
		///		fish-eye kind of view, low values (30- Degrees) in a stretched, telescopic kind of view. Typical values
		///		are between 45 and 60 Degrees.
		///		<p/>
		///		This value represents the HORIZONTAL field-of-view. The vertical field of view is calculated from
		///		this depending on the dimensions of the viewport (they will only be the same if the viewport is square).
		/// </remarks>
		public virtual Radian FieldOfView
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return _fieldOfView;
			}

			[OgreVersion( 1, 7, 2 )]
			set
			{
				_fieldOfView = value;
				InvalidateFrustum();
			}
		}

		#endregion FieldOfView Property

		#region Far Property

		/// <summary>
		///     Far clip distance - default 10000.
		/// </summary>
		protected Real _farDistance;

		/// <summary>
		///		Gets/Sets the distance to the far clipping plane.
		///	 </summary>
		///	 <remarks>
		///		The view frustrum is a pyramid created from the camera position and the edges of the viewport.
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
				InvalidateView(); //XEONX FIX: Now the IsObjectVisible() will work properly
			}
		}

		#endregion Far Property

		#region Near Property

		/// <summary>
		///     Near clip distance - default 100.
		/// </summary>
		private Real _nearDistance;

		/// <summary>
		///		Gets/Sets the position of the near clipping plane.
		///	</summary>
		///	<remarks>
		///		The position of the near clipping plane is the distance from the cameras position to the screen
		///		on which the world is projected. The near plane distance, combined with the field-of-view and the
		///		aspect ratio, determines the size of the viewport through which the world is viewed (in world
		///		co-ordinates). Note that this world viewport is different to a screen viewport, which has it's
		///		dimensions expressed in pixels. The cameras viewport should have the same aspect ratio as the
		///		screen viewport it renders into to avoid distortion.
		/// </remarks>
		public virtual Real Near
		{
			get
			{
				return _nearDistance;
			}
			set
			{
				Debug.Assert( value > 0, "Near clip distance must be greater than zero." );

				_nearDistance = value;
				InvalidateFrustum();
				InvalidateView(); //XEONX FIX: Now the IsObjectVisible() will work properly
			}
		}

		#endregion Near Property

		#region AspectRatio Property

		/// <summary>
		///     x/y viewport ratio - default 1.3333
		/// </summary>
		private Real _aspectRatio;

		/// <summary>
		///		Gets/Sets the aspect ratio to use for the camera viewport.
		/// </summary>
		/// <remarks>
		///		The ratio between the x and y dimensions of the rectangular area visible through the camera
		///		is known as aspect ratio: aspect = width / height .
		///		<p/>
		///		The default for most fullscreen windows is 1.3333f - this is also assumed unless you
		///		use this property to state otherwise.
		/// </remarks>
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

		#region FrustumOffset Property

		///<summary>
		/// Off-axis frustum center offset - default (0.0, 0.0)
		///</summary>
		protected Vector2 _frustumOffset;

		///<summary>
		/// Off-axis frustum center offset - default (0.0, 0.0)
		///</summary>
		public Vector2 FrustumOffset
		{
			get
			{
				return _frustumOffset;
			}
			set
			{
				_frustumOffset = value;
				UpdateFrustum();
			}
		}

		#endregion FrustumOffset Property

		#region FocalLength Property

		/// <summary>
		/// Get/Set FocalLength
		/// <remarks>Focal length of frustum</remarks>
		/// </summary>
		public Real FocalLength
		{
			get
			{
				return _focalLength;
			}
			set
			{
				if ( _focalLength <= 0 )
				{
					throw new AxiomException( "Focal length must be greater than zero." );
				}
				_focalLength = value;
				InvalidateFrustum();
			}
		}

		#endregion FocalLength Property

		#region OrientationMode

		private OrientationMode _orientationMode;

		[OgreVersion( 1, 7, 2790 )]
		public OrientationMode OrientationMode
		{
			get
			{
#if AXIOM_NO_VIEWPORT_ORIENTATIONMODE
                throw new AxiomException( "Getting Frustrum orientation mode is not supported" );
#endif
				return _orientationMode;
			}
			set
			{
#if AXIOM_NO_VIEWPORT_ORIENTATIONMODE
                throw new AxiomException( "Setting Frustrum orientation mode is not supported" );
#endif
				_orientationMode = value;
				InvalidateFrustum();
			}
		}

		#endregion

		///<summary>
		/// Focal length of frustum (for stereo rendering, defaults to 1.0)
		///</summary>
		protected float _focalLength;

		/// <summary>
		///     The 6 main clipping planes.
		/// </summary>
		protected Plane[] _planes = new Plane[6];

		/// <summary>
		///     Stored versions of parent orientation.
		/// </summary>
		protected Quaternion _lastParentOrientation;

		/// <summary>
		///     Stored versions of parent position.
		/// </summary>
		protected Vector3 _lastParentPosition;

		#region ProjectionMatrixRS Property

		/// <summary>
		/// Gets the projection matrix for this frustum adjusted for the current
		/// rendersystem specifics (may be right or left-handed, depth range
		/// may vary).
		/// </summary>
		/// <remarks>
		/// This method retrieves the rendering-API dependent version of the projection
		/// matrix. If you want a 'typical' projection matrix then use _projectionMatrix.
		/// </remarks>
		protected Matrix4 _projectionMatrixRS;

		/// <summary>
		/// Gets the projection matrix for this frustum adjusted for the current
		/// rendersystem specifics (may be right or left-handed, depth range
		/// may vary).
		/// </summary>
		/// <remarks>
		/// This method retrieves the rendering-API dependent version of the projection
		/// matrix. If you want a 'typical' projection matrix then use ProjectionMatrix.
		/// </remarks>
		public virtual Matrix4 ProjectionMatrixRS
		{
			get
			{
				UpdateFrustum();

				return _projectionMatrixRS;
			}
		}

		#endregion ProjectionMatrixRS Property

		#region ProjectionMatrixRSDepth Property

		/// <summary>
		///  The depth-adjusted projection matrix for the current rendersystem,
		///  but one which still conforms to right-hand rules.
		/// </summary>
		/// <remarks>
		///     This differs from the rendering-API dependent getProjectionMatrix
		///     in that it always returns a right-handed projection matrix result
		///     no matter what rendering API is being used - this is required for
		///     vertex and fragment programs for example. However, the resulting depth
		///     range may still vary between render systems since D3D uses [0,1] and
		///     GL uses [-1,1], and the range must be kept the same between programmable
		///     and fixed-function pipelines.
		/// </remarks>
		protected Matrix4 _projectionMatrixRSDepth;

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
		///    <para/>
		///    This corresponds to the Ogre mProjMatrixRSDepth and
		///    getProjectionMatrixWithRSDepth
		/// </remarks>
		public virtual Matrix4 ProjectionMatrixRSDepth
		{
			get
			{
				UpdateFrustum();

				return _projectionMatrixRSDepth;
			}
		}

		#endregion ProjectionMatrixRSDepth Property

		#region ProjectionMatrix Property

		/// <summary>
		/// The normal projection matrix for this frustum, ie the
		/// projection matrix which conforms to standard right-handed rules and
		/// uses depth range [-1,+1].
		/// </summary>
		///<remarks>
		///    This differs from the rendering-API dependent getProjectionMatrixRS
		///    in that it always returns a right-handed projection matrix with depth
		///    range [-1,+1], result no matter what rendering API is being used - this
		///    is required for some uniform algebra for example.
		///</remarks>
		protected Matrix4 _projectionMatrix;

		/// <summary>
		/// Gets the projection matrix for this frustum.
		/// </summary>
		public virtual Matrix4 ProjectionMatrix
		{
			get
			{
				UpdateFrustum();

				return _projectionMatrix;
			}
			set
			{
				{
					_projectionMatrix = value;
				}
			}
		}

		#endregion ProjectionMatrix Property

		#region ViewMatrix Property

		/// <summary>
		///     Pre-calced view matrix.
		/// </summary>
		protected Matrix4 _viewMatrix;

		/// <summary>
		///     Gets the view matrix for this frustum.
		/// </summary>
		public virtual Matrix4 ViewMatrix
		{
			get
			{
				UpdateView();

				return _viewMatrix;
			}
			set
			{
				_viewMatrix = value;
			}
		}

		#endregion ViewMatrix Property

		protected bool _recalculateFrustumPlanes;

		public Plane[] FrustumPlanes
		{
			get
			{
				UpdateFrustumPlanes();
				return _planes;
			}
		}

		/// <summary>
		///     Something's changed in the frustum shape?
		/// </summary>
		protected bool _recalculateFrustum;

		/// <summary>
		///		Evaluates whether or not the view frustum is out of date.
		/// </summary>
		protected virtual bool IsFrustumOutOfDate
		{
			get
			{
				if ( useObliqueDepthProjection )
				{
					// always out of date since plane needs to be in view space
					if ( IsViewOutOfDate )
					{
						_recalculateFrustum = true;
					}

					// update derived plane
					if ( linkedObliqueProjPlane != null && !( lastLinkedObliqueProjPlane == linkedObliqueProjPlane.DerivedPlane ) )
					{
						obliqueProjPlane = linkedObliqueProjPlane.DerivedPlane;
						lastLinkedObliqueProjPlane = obliqueProjPlane;
						_recalculateFrustum = true;
					}
				}

				return _recalculateFrustum;
			}
		}

		/// <summary>
		///     Something in the view pos has changed?
		/// </summary>
		protected bool _recalculateView;

		/// <summary>
		///		Gets whether or not the view matrix is out of date.
		/// </summary>
		protected virtual bool IsViewOutOfDate
		{
			get
			{
				// are we attached to another node?
				if ( parentNode != null )
				{
					if ( _recalculateView || parentNode.DerivedOrientation != _lastParentOrientation ||
					     parentNode.DerivedPosition != _lastParentPosition )
					{
						// we are out of date with the parent scene node
						_lastParentOrientation = parentNode.DerivedOrientation;
						_lastParentPosition = parentNode.DerivedPosition;
						_recalculateView = true;
					}
				}

				// deriving direction from linked plane?
				if ( isReflected && linkedReflectionPlane != null &&
				     !( lastLinkedReflectionPlane == linkedReflectionPlane.DerivedPlane ) )
				{
					_reflectionPlane = linkedReflectionPlane.DerivedPlane;
					_reflectionMatrix = Utility.BuildReflectionMatrix( _reflectionPlane );
					lastLinkedReflectionPlane = linkedReflectionPlane.DerivedPlane;
					_recalculateView = true;
				}

				return _recalculateView;
			}
		}

		///<summary>
		/// Are we using a custom view matrix?
		/// </summary>
		private bool _customViewMatrix;

		public bool IsCustomViewMatrixEnabled
		{
			get
			{
				return _customViewMatrix;
			}
		}

		/// <summary>
		/// Are we using a custom projection matrix?
		/// </summary>
		private bool _customProjectionMatrix;

		public bool IsCustomProjectionMatrixEnabled
		{
			get
			{
				return _customProjectionMatrix;
			}
		}

		/// <summary>
		///     Bounding box of this frustum.
		/// </summary>
		protected AxisAlignedBox _boundingBox = AxisAlignedBox.Null;

		/// <summary>
		///
		/// </summary>
		protected bool _recalculateVertexData = true;

		/// <summary>
		///     Vertex info for rendering this frustum.
		/// </summary>
		protected VertexData _vertexData = new VertexData();

		/// <summary>
		///     Material to use when rendering this frustum.
		/// </summary>
		protected Material _material;

		/// <summary>
		/// Signal to recalculate World Space Corners
		/// </summary>
		protected bool _recalculateWorldSpaceCorners;

		/// <summary>
		///		Frustum corners in world space.
		/// </summary>
		protected Vector3[] _worldSpaceCorners = new Vector3[8];

		/** Temp coefficient values calculated from a frustum change,
            used when establishing the frustum planes when the view changes. */
		protected float[] _coeffL = new float[2];
		protected float[] _coeffR = new float[2];
		protected float[] _coeffB = new float[2];
		protected float[] _coeffT = new float[2];

		#region IsRefelcted Property

		/// <summary>
		///		Is this frustum to act as a reflection of itself?
		/// </summary>
		protected bool isReflected;

		/// <summary>
		///     Gets a flag that specifies whether this camera is being reflected or not.
		/// </summary>
		public virtual bool IsReflected
		{
			get
			{
				return isReflected;
			}
		}

		#endregion IsRefelcted Property

		#region ReflectionMatrix Property

		/// <summary>
		///		Derive reflection matrix.
		/// </summary>
		private Matrix4 _reflectionMatrix;

		/// <summary>
		///     Returns the reflection matrix of the camera if appropriate.
		/// </summary>
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
		private Plane _reflectionPlane;

		/// <summary>
		///     Returns the reflection plane of the camera if appropriate.
		/// </summary>
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

		/// <summary>
		///		Reference of a reflection plane (automatically updated).
		/// </summary>
		protected IDerivedPlaneProvider linkedReflectionPlane;

		/// <summary>
		///		Record of the last world-space reflection plane info used.
		/// </summary>
		protected Plane lastLinkedReflectionPlane;

		/// <summary>
		///		Is this frustum using an oblique depth projection?
		/// </summary>
		protected bool useObliqueDepthProjection;

		/// <summary>
		///		Fixed oblique projection plane.
		/// </summary>
		protected Plane obliqueProjPlane;

		/// <summary>
		///		Reference to oblique projection plane (automatically updated).
		/// </summary>
		protected IDerivedPlaneProvider linkedObliqueProjPlane;

		/// <summary>
		///		Record of the last world-space oblique depth projection plane info used.
		/// </summary>
		protected Plane lastLinkedObliqueProjPlane;

		/// <summary>
		///     Dummy list for IRenderable.Lights since we wont be lit.
		/// </summary>
		protected LightList dummyLightList = new LightList();

		protected List<Vector4> customParams = new List<Vector4>();

		/* Frustum extents */
		protected float _left, _right, _top, _bottom;

		protected bool _frustumExtentsManuallySet;

		protected float _orthoHeight;

		/// <summary>
		///
		/// </summary>
		public float OrthoWindowHeight
		{
			get
			{
				return _orthoHeight;
			}
			set
			{
				_orthoHeight = value;
				InvalidateFrustum();
			}
		}

		/// <summary>
		///
		/// </summary>
		public float OrthoWindowWidth
		{
			get
			{
				return _orthoHeight*_aspectRatio;
			}
			set
			{
				_orthoHeight = value/_aspectRatio;
				InvalidateFrustum();
			}
		}

		#endregion Fields and Properties

		#region Constructors

		/// <summary>
		///     Default constructor.
		/// </summary>
		public Frustum()
			: base()
		{
			Initialize();
		}

		public Frustum( string name )
			: base( name )
		{
			Initialize();
		}

		private void Initialize()
		{
			for ( var i = 0; i < 6; i++ )
			{
				_planes[ i ] = new Plane();
			}

			_fieldOfView = Utility.PI/4.0f;
			_nearDistance = 100.0f;
			_farDistance = 100000.0f;
			_aspectRatio = 1.33333333333333f;
			_orthoHeight = 1000.0f;
			_frustumExtentsManuallySet = false;
			_recalculateFrustum = true;
			_recalculateView = true;

			// Init matrices
			_viewMatrix = Matrix4.Zero;
			_projectionMatrix = Matrix4.Zero;
			_projectionMatrixRS = Matrix4.Zero;

			_projectionType = Projection.Perspective;

			_lastParentPosition = Vector3.Zero;
			_lastParentOrientation = Quaternion.Identity;

			// init vertex data
			_vertexData.vertexDeclaration.AddElement( 0, 0, VertexElementType.Float3, VertexElementSemantic.Position );
			_vertexData.vertexStart = 0;
			_vertexData.vertexCount = 32;
			_vertexData.vertexBufferBinding.SetBinding( 0,
			                                            HardwareBufferManager.Instance.CreateVertexBuffer(
			                                            	_vertexData.vertexDeclaration, _vertexData.vertexCount,
			                                            	BufferUsage.DynamicWriteOnly ) );

			_material = (Material)MaterialManager.Instance[ "BaseWhite" ];

			_customProjectionMatrix = false;
			_customViewMatrix = false;

			_frustumOffset = new Vector2( 0.0f, 0.0f );
			_focalLength = 1.0f;

			lastLinkedReflectionPlane = new Plane();
			lastLinkedObliqueProjPlane = new Plane();

			UpdateView();
		}

		#endregion Constructors

		#region Methods

		/// <summary>
		///		Disables any custom near clip plane.
		/// </summary>
		public virtual void DisableCustomNearClipPlane()
		{
			useObliqueDepthProjection = false;
			linkedObliqueProjPlane = null;
			InvalidateFrustum();
		}

		/// <summary>
		/// Calculate a view matrix for this frustum, relative to a potentially dynamic point. 
		/// Mainly for use by AXIOM internally when using camera-relative rendering
		/// for frustums that are not the centre (e.g. texture projection)
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public void CalcViewMatrixRelative( Vector3 relPos, out Matrix4 matToUpdate )
		{
			Matrix4 matTrans = Matrix4.Identity;
			matTrans.Translation = relPos;
			matToUpdate = ViewMatrix*matTrans;
		}

		public void SetCustomViewMatrix( bool enable )
		{
			SetCustomViewMatrix( enable, Matrix4.Identity );
		}

		public void SetCustomViewMatrix( bool enable, Matrix4 viewMatrix )
		{
			_customViewMatrix = enable;
			if ( enable )
			{
				Debug.Assert( viewMatrix.IsAffine );
				_viewMatrix = viewMatrix;
			}
			InvalidateView();
		}

		public void SetCustomProjectionMatrix( bool enable )
		{
			SetCustomProjectionMatrix( enable, Matrix4.Identity );
		}

		public void SetCustomProjectionMatrix( bool enable, Matrix4 projMatrix )
		{
			_customProjectionMatrix = enable;
			if ( enable )
			{
				_projectionMatrix = projMatrix;
			}

			InvalidateFrustum();
		}

		/// <summary>
		///     Disables reflection modification previously turned on with 
		///     <see cref="EnableReflection(Plane)"/>.
		/// </summary>
		public virtual void DisableReflection()
		{
			isReflected = false;
			lastLinkedReflectionPlane.Normal = Vector3.Zero;
			InvalidateView();
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
		public virtual void EnableCustomNearClipPlane( IDerivedPlaneProvider plane )
		{
			useObliqueDepthProjection = true;
			linkedObliqueProjPlane = plane;
			obliqueProjPlane = plane.DerivedPlane;
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
		public virtual void EnableCustomNearClipPlane( Plane plane )
		{
			useObliqueDepthProjection = true;
			linkedObliqueProjPlane = null;
			obliqueProjPlane = plane;
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
		public virtual void EnableReflection( Plane plane )
		{
			isReflected = true;
			_reflectionPlane = plane;
			linkedReflectionPlane = null;
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
		public virtual void EnableReflection( IDerivedPlaneProvider plane )
		{
			isReflected = true;
			linkedReflectionPlane = plane;
			_reflectionPlane = linkedReflectionPlane.DerivedPlane;
			_reflectionMatrix = Utility.BuildReflectionMatrix( _reflectionPlane );
			lastLinkedReflectionPlane = _reflectionPlane;
			InvalidateView();
		}

		/// <summary>
		///		Get the derived position of this frustum.
		/// </summary>
		/// <returns></returns>
		protected virtual Vector3 GetPositionForViewUpdate()
		{
			return _lastParentPosition;
		}

		/// <summary>
		///		Get the derived orientation of this frustum.
		/// </summary>
		/// <returns></returns>
		protected virtual Quaternion GetOrientationForViewUpdate()
		{
			return _lastParentOrientation;
		}

		/// <summary>
		///		Overloaded method.
		/// </summary>
		/// <param name="box"></param>
		/// <returns></returns>
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
		public bool IsObjectVisible( AxisAlignedBox box, out FrustumPlane culledBy )
		{
			culledBy = FrustumPlane.None;

			// Null boxes are always invisible
			if ( box.IsNull )
			{
				return false;
			}

			// Infinite Boxes are always visible
			if ( box.IsInfinite )
			{
				return true;
			}

			// Make any pending updates to the calculated frustum
			UpdateFrustumPlanes();

			// Get corners of the box
			var corners = box.Corners;

			// For each plane, see if all points are on the negative side
			// If so, object is not visible
			for ( var plane = 0; plane < 6; plane++ )
			{
				// skip far plane if infinite view frustum
				if ( _farDistance == 0 && plane == (int)FrustumPlane.Far )
				{
					continue;
				}

				if ( _planes[ plane ].GetSide( corners[ 0 ] ) == PlaneSide.Negative &&
				     _planes[ plane ].GetSide( corners[ 1 ] ) == PlaneSide.Negative &&
				     _planes[ plane ].GetSide( corners[ 2 ] ) == PlaneSide.Negative &&
				     _planes[ plane ].GetSide( corners[ 3 ] ) == PlaneSide.Negative &&
				     _planes[ plane ].GetSide( corners[ 4 ] ) == PlaneSide.Negative &&
				     _planes[ plane ].GetSide( corners[ 5 ] ) == PlaneSide.Negative &&
				     _planes[ plane ].GetSide( corners[ 6 ] ) == PlaneSide.Negative &&
				     _planes[ plane ].GetSide( corners[ 7 ] ) == PlaneSide.Negative )
				{
					// ALL corners on negative side therefore out of view
					culledBy = (FrustumPlane)plane;
					return false;
				}
			}

			// box is not culled
			return true;
		}

		/// <summary>
		///		Overloaded method.
		/// </summary>
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
		public bool IsObjectVisible( Sphere sphere, out FrustumPlane culledBy )
		{
			// Make any pending updates to the calculated frustum
			UpdateFrustumPlanes();

			// For each plane, see if sphere is on negative side
			// If so, object is not visible
			for ( var plane = 0; plane < 6; plane++ )
			{
				if ( _farDistance == 0 && plane == (int)FrustumPlane.Far )
				{
					continue;
				}

				// If the distance from sphere center to plane is negative, and 'more negative'
				// than the radius of the sphere, sphere is outside frustum
				if ( _planes[ plane ].GetDistance( sphere.Center ) < -sphere.Radius )
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
		/// <param name="vertex">3D point to check for frustum visibility.</param>
		/// <param name="culledBy">
		///		Optional FrustrumPlane params which will be filled by the plane which culled
		///		the box if the result was false.
		///	</param>
		/// <returns>True if the box is visible, otherwise false.</returns>
		public bool IsObjectVisible( Vector3 vertex, out FrustumPlane culledBy )
		{
			// Make any pending updates to the calculated frustum
			UpdateFrustumPlanes();

			// For each plane, see if all points are on the negative side
			// If so, object is not visible
			for ( var plane = 0; plane < 6; plane++ )
			{
				if ( _farDistance == 0 && plane == (int)FrustumPlane.Far )
				{
					continue;
				}

				if ( _planes[ plane ].GetSide( vertex ) == PlaneSide.Negative )
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

		public virtual bool ProjectSphere( Sphere sphere, out float left, out float top, out float right, out float bottom )
		{
			// initialise
			left = bottom = -1.0f;
			right = top = 1.0f;

			// Transform light position into camera space
			var eyeSpacePos = ViewMatrix.TransformAffine( sphere.Center );

			if ( eyeSpacePos.z < 0 )
			{
				float r = sphere.Radius;
				// early-exit
				if ( eyeSpacePos.LengthSquared <= r*r )
				{
					return false;
				}

				var screenSpacePos = ProjectionMatrix*eyeSpacePos;

				// perspective attenuate
				var spheresize = new Vector3( r, r, eyeSpacePos.z );
				spheresize = ProjectionMatrixRSDepth*spheresize;

				float possLeft = screenSpacePos.x - spheresize.x;
				float possRight = screenSpacePos.x + spheresize.x;
				float possTop = screenSpacePos.y + spheresize.y;
				float possBottom = screenSpacePos.y - spheresize.y;

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
		protected virtual void InvalidateFrustum()
		{
			_recalculateFrustum = true;
			_recalculateFrustumPlanes = true;
			_recalculateWorldSpaceCorners = true;
			_recalculateVertexData = true;
		}

		/// <summary>
		///     Signal to update view information.
		/// </summary>
		protected virtual void InvalidateView()
		{
			_recalculateView = true;
			_recalculateFrustumPlanes = true;
			_recalculateWorldSpaceCorners = true;
		}

		protected void CalculateProjectionParameters( out Real vpLeft, out Real vpRight, out Real vpBottom, out Real vpTop )
		{
			if ( _customProjectionMatrix )
			{
				// Convert clipspace corners to camera space
				var invProj = _projectionMatrix.Inverse();
				var topLeft = new Vector3( -0.5f, 0.5f, 0.0f );
				var bottomRight = new Vector3( 0.5f, -0.5f, 0.0f );

				topLeft = invProj*topLeft;
				bottomRight = invProj*bottomRight;

				vpLeft = topLeft.x;
				vpTop = topLeft.y;
				vpRight = bottomRight.x;
				vpBottom = bottomRight.y;
			}
			else if ( _frustumExtentsManuallySet )
			{
				vpLeft = _left;
				vpRight = _right;
				vpTop = _top;
				vpBottom = _bottom;
			}
				// Calculate general projection parameters
			else if ( ProjectionType == Projection.Perspective )
			{
				// Calculate general projection parameters

				Real thetaY = _fieldOfView*0.5;
				Real tanThetaY = Utility.Tan( thetaY );
				var tanThetaX = tanThetaY*_aspectRatio;

				// Unknown how to apply frustum offset to orthographic camera, just ignore here
				var nearFocal = _nearDistance/_focalLength;
				Real nearOffsetX = _frustumOffset.x*nearFocal;
				Real nearOffsetY = _frustumOffset.y*nearFocal;
				var half_w = tanThetaX*_nearDistance;
				var half_h = tanThetaY*_nearDistance;

				vpLeft = -half_w + nearOffsetX;
				vpRight = +half_w + nearOffsetX;
				vpBottom = -half_h + nearOffsetY;
				vpTop = +half_h + nearOffsetY;

				_left = vpLeft;
				_right = vpRight;
				_top = vpTop;
				_bottom = vpBottom;
			}
			else //if (ProjectionType == Projection.Orthographic)
			{
				// Unknown how to apply frustum offset to orthographic camera, just ignore here
				var half_w = OrthoWindowWidth*0.5f;
				var half_h = OrthoWindowHeight*0.5f;

				vpLeft = -half_w;
				vpRight = +half_w;
				vpBottom = -half_h;
				vpTop = +half_h;

				_left = vpLeft;
				_right = vpRight;
				_top = vpTop;
				_bottom = vpBottom;
			}
		}

		/// <summary>
		///		Updates the frustum data.
		/// </summary>
		protected void UpdateFrustum()
		{
			if ( IsFrustumOutOfDate )
			{
				_updateFrustum();
			}
		}

		protected virtual void _updateFrustum()
		{
			Real vpTop, vpRight, vpBottom, vpLeft;

			CalculateProjectionParameters( out vpLeft, out vpRight, out vpBottom, out vpTop );

			if ( !_customProjectionMatrix )
			{
				// The code below will dealing with general projection
				// parameters, similar glFrustum and glOrtho.
				// Doesn't optimise manually except division operator, so the
				// code more self-explaining.

				Real inv_w = 1.0f/( vpRight - vpLeft );
				Real inv_h = 1.0f/( vpTop - vpBottom );
				Real inv_d = 1.0f/( _farDistance - _nearDistance );

				// Recalc if frustum params changed
				if ( _projectionType == Projection.Perspective )
				{
					// Calc matrix elements
					Real A = 2.0f*_nearDistance*inv_w;
					Real B = 2.0f*_nearDistance*inv_h;
					Real C = ( vpRight + vpLeft )*inv_w;
					Real D = ( vpTop + vpBottom )*inv_h;
					Real q, qn;
					if ( _farDistance == 0.0f )
					{
						// Infinite far plane
						q = Frustum.InfiniteFarPlaneAdjust - 1.0f;
						qn = _nearDistance*( Frustum.InfiniteFarPlaneAdjust - 2.0f );
					}
					else
					{
						q = -( _farDistance + _nearDistance )*inv_d;
						qn = -2.0f*( _farDistance*_nearDistance )*inv_d;
					}

					// NB: This creates 'uniform' perspective projection matrix,
					// which depth range [-1,1], right-handed rules
					//
					// [ A   0   C   0  ]
					// [ 0   B   D   0  ]
					// [ 0   0   q   qn ]
					// [ 0   0   -1  0  ]
					//
					// A = 2 * near / (right - left)
					// B = 2 * near / (top - bottom)
					// C = (right + left) / (right - left)
					// D = (top + bottom) / (top - bottom)
					// q = - (far + near) / (far - near)
					// qn = - 2 * (far * near) / (far - near)

					_projectionMatrix = Matrix4.Zero;
					_projectionMatrix.m00 = A;
					_projectionMatrix.m02 = C;
					_projectionMatrix.m11 = B;
					_projectionMatrix.m12 = D;
					_projectionMatrix.m22 = q;
					_projectionMatrix.m23 = qn;
					_projectionMatrix.m32 = -1.0f;

					if ( useObliqueDepthProjection )
					{
						// Translate the plane into view space

						// Don't use getViewMatrix here, incase overrided by
						// camera and return a cull frustum view matrix
						UpdateView();

						var plane = _viewMatrix*obliqueProjPlane;

						// Thanks to Eric Lenyel for posting this calculation
						// at www.terathon.com

						// Calculate the clip-space corner point opposite the
						// clipping plane
						// as (sgn(clipPlane.x), sgn(clipPlane.y), 1, 1) and
						// transform it into camera space by multiplying it
						// by the inverse of the projection matrix

						/* generalised version
                        Vector4 q = matrix.inverse() *
                        Vector4(Math::Sign(plane.normal.x),
                        Math::Sign(plane.normal.y), 1.0f, 1.0f);
                         */
						var q1 = new Vector4();
						q1.x = ( System.Math.Sign( plane.Normal.x ) + _projectionMatrix.m02 )/_projectionMatrix.m00;
						q1.y = ( System.Math.Sign( plane.Normal.y ) + _projectionMatrix.m12 )/_projectionMatrix.m11;
						q1.z = -1.0f;
						q1.w = ( 1.0f + _projectionMatrix.m22 )/_projectionMatrix.m23;

						// Calculate the scaled plane vector
						var clipPlane4d = new Vector4( plane.Normal.x, plane.Normal.y, plane.Normal.z, plane.D );
						var c = clipPlane4d*( 2.0f/( clipPlane4d.Dot( q1 ) ) );

						// Replace the third row of the projection matrix
						_projectionMatrix.m20 = c.x;
						_projectionMatrix.m21 = c.y;
						_projectionMatrix.m22 = c.z + 1.0f;
						_projectionMatrix.m23 = c.w;
					}
				} // perspective
				else if ( _projectionType == Projection.Orthographic )
				{
					var A = 2.0f*inv_w;
					var B = 2.0f*inv_h;
					Real C = -( vpRight + vpLeft )*inv_w;
					Real D = -( vpTop + vpBottom )*inv_h;
					Real q, qn;
					if ( _farDistance == 0.0f )
					{
						// Can not do infinite far plane here, avoid divided zero only
						q = -Frustum.InfiniteFarPlaneAdjust/_nearDistance;
						qn = -Frustum.InfiniteFarPlaneAdjust - 1.0f;
					}
					else
					{
						q = -2.0f*inv_d;
						qn = -( _farDistance + _nearDistance )*inv_d;
					}

					// NB: This creates 'uniform' orthographic projection matrix,
					// which depth range [-1,1], right-handed rules
					//
					// [ A   0   0   C  ]
					// [ 0   B   0   D  ]
					// [ 0   0   q   qn ]
					// [ 0   0   0   1  ]
					//
					// A = 2 * / (right - left)
					// B = 2 * / (top - bottom)
					// C = - (right + left) / (right - left)
					// D = - (top + bottom) / (top - bottom)
					// q = - 2 / (far - near)
					// qn = - (far + near) / (far - near)

					_projectionMatrix = Matrix4.Zero;
					_projectionMatrix.m00 = A;
					_projectionMatrix.m03 = C;
					_projectionMatrix.m11 = B;
					_projectionMatrix.m13 = D;
					_projectionMatrix.m22 = q;
					_projectionMatrix.m23 = qn;
					_projectionMatrix.m33 = 1.0f;
				} // ortho
			} // if !_customProjectionMatrix

			// grab a reference to the current render system
			var renderSystem = Root.Instance.RenderSystem;
			// API specific
			renderSystem.ConvertProjectionMatrix( _projectionMatrix, out _projectionMatrixRS );
			// API specific for Gpu Programs
			renderSystem.ConvertProjectionMatrix( _projectionMatrix, out _projectionMatrixRSDepth, true );

			// Calculate bounding box (local)
			// Box is from 0, down -Z, max dimensions as determined from far plane
			// If infinite view frustum just pick a far value
			var farDist = ( _farDistance == 0.0f ) ? InfiniteFarPlaneDistance : _farDistance;

			// Near plane bounds
			var min = new Vector3( vpLeft, vpBottom, -farDist );
			var max = new Vector3( vpRight, vpTop, 0 );

			if ( _customProjectionMatrix )
			{
				// Some custom projection matrices can have unusual inverted settings
				// So make sure the AABB is the right way around to start with
				var tmp = min;
				min.Floor( max );
				max.Ceil( tmp );
			}

			var radio = 1.0f;
			if ( _projectionType == Projection.Perspective )
			{
				// Merge with far plane bounds
				radio = _farDistance/_nearDistance;
				min.Floor( new Vector3( vpLeft*radio, vpBottom*radio, -_farDistance ) );
				max.Ceil( new Vector3( vpRight*radio, vpTop*radio, 0 ) );
			}

			_boundingBox.SetExtents( min, max );

			_recalculateFrustum = false;

			// Signal to update frustum clipping planes
			_recalculateFrustumPlanes = true;
		}

		protected void UpdateFrustumPlanes()
		{
			UpdateView();
			UpdateFrustum();

			if ( _recalculateFrustumPlanes )
			{
				_updateFrustumPlanes();
			}
		}

		protected virtual void _updateFrustumPlanes()
		{
			// -------------------------
			// Update the frustum planes
			// -------------------------
			var combo = _projectionMatrix*_viewMatrix;

			_planes[ (int)FrustumPlane.Left ].Normal.x = combo[ 3, 0 ] + combo[ 0, 0 ];
			_planes[ (int)FrustumPlane.Left ].Normal.y = combo[ 3, 1 ] + combo[ 0, 1 ];
			_planes[ (int)FrustumPlane.Left ].Normal.z = combo[ 3, 2 ] + combo[ 0, 2 ];
			_planes[ (int)FrustumPlane.Left ].D = combo[ 3, 3 ] + combo[ 0, 3 ];

			_planes[ (int)FrustumPlane.Right ].Normal.x = combo[ 3, 0 ] - combo[ 0, 0 ];
			_planes[ (int)FrustumPlane.Right ].Normal.y = combo[ 3, 1 ] - combo[ 0, 1 ];
			_planes[ (int)FrustumPlane.Right ].Normal.z = combo[ 3, 2 ] - combo[ 0, 2 ];
			_planes[ (int)FrustumPlane.Right ].D = combo[ 3, 3 ] - combo[ 0, 3 ];

			_planes[ (int)FrustumPlane.Top ].Normal.x = combo[ 3, 0 ] - combo[ 1, 0 ];
			_planes[ (int)FrustumPlane.Top ].Normal.y = combo[ 3, 1 ] - combo[ 1, 1 ];
			_planes[ (int)FrustumPlane.Top ].Normal.z = combo[ 3, 2 ] - combo[ 1, 2 ];
			_planes[ (int)FrustumPlane.Top ].D = combo[ 3, 3 ] - combo[ 1, 3 ];

			_planes[ (int)FrustumPlane.Bottom ].Normal.x = combo[ 3, 0 ] + combo[ 1, 0 ];
			_planes[ (int)FrustumPlane.Bottom ].Normal.y = combo[ 3, 1 ] + combo[ 1, 1 ];
			_planes[ (int)FrustumPlane.Bottom ].Normal.z = combo[ 3, 2 ] + combo[ 1, 2 ];
			_planes[ (int)FrustumPlane.Bottom ].D = combo[ 3, 3 ] + combo[ 1, 3 ];

			_planes[ (int)FrustumPlane.Near ].Normal.x = combo[ 3, 0 ] + combo[ 2, 0 ];
			_planes[ (int)FrustumPlane.Near ].Normal.y = combo[ 3, 1 ] + combo[ 2, 1 ];
			_planes[ (int)FrustumPlane.Near ].Normal.z = combo[ 3, 2 ] + combo[ 2, 2 ];
			_planes[ (int)FrustumPlane.Near ].D = combo[ 3, 3 ] + combo[ 2, 3 ];

			_planes[ (int)FrustumPlane.Far ].Normal.x = combo[ 3, 0 ] - combo[ 2, 0 ];
			_planes[ (int)FrustumPlane.Far ].Normal.y = combo[ 3, 1 ] - combo[ 2, 1 ];
			_planes[ (int)FrustumPlane.Far ].Normal.z = combo[ 3, 2 ] - combo[ 2, 2 ];
			_planes[ (int)FrustumPlane.Far ].D = combo[ 3, 3 ] - combo[ 2, 3 ];

			// Renormalise any normals which were not unit length
			for ( var i = 0; i < 6; i++ )
			{
				float length = _planes[ i ].Normal.Normalize();
				_planes[ i ].D /= length;
			}

			_recalculateFrustumPlanes = false;
		}

		/// <summary>
		///		Updates the view matrix.
		/// </summary>
		protected void UpdateView()
		{
			// check if the view is out of date
			if ( IsViewOutOfDate )
			{
				_updateView();
			}
		}

		protected virtual void _updateView()
		{
			// ----------------------
			// Update the view matrix
			// ----------------------

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

			if ( !_customViewMatrix )
			{
				Quaternion orientation = GetOrientationForViewUpdate();
				Vector3 position = GetPositionForViewUpdate();

				_viewMatrix = Axiom.Math.Matrix4.MakeViewMatrix( position, orientation,
				                                                 isReflected ? ReflectionMatrix : Matrix4.Zero );
			}

			_recalculateView = false;

			//Signal to update frustum clipping planes
			_recalculateFrustumPlanes = true;
			//Signal to update world space corners
			_recalculateWorldSpaceCorners = true;
			//Signal to update frustum if oblique plane enabled,
			//since plane needs to be in view space
			if ( useObliqueDepthProjection )
			{
				_recalculateFrustum = true;
			}
		}

		protected void UpdateWorldSpaceCorners()
		{
			UpdateView();

			if ( _recalculateWorldSpaceCorners )
			{
				_updateWorldSpaceCorners();
			}
		}

		protected virtual void _updateWorldSpaceCorners()
		{
			var eyeToWorld = _viewMatrix.InverseAffine();

			// Note: Even though we could be dealing with general a projection matrix here,
			//       it is incompatible with the infinite far plane, thus, we need to work
			//		 with projection parameters.

			// Calc near plane corners
			Real nearLeft, nearRight, nearBottom, nearTop;
			CalculateProjectionParameters( out nearLeft, out nearRight, out nearBottom, out nearTop );

			// Treat infinite fardist as some arbitrary far value
			Real farDist = ( _farDistance == 0 ) ? 100000 : _farDistance;

			// Calc far palne corners
			Real ratio = _projectionType == Projection.Perspective ? _farDistance/_nearDistance : 1;
			var farLeft = nearLeft*ratio;
			var farRight = nearRight*ratio;
			var farBottom = nearBottom*ratio;
			var farTop = nearTop*ratio;

			// near
			_worldSpaceCorners[ 0 ] = eyeToWorld.TransformAffine( new Vector3( nearRight, nearTop, -_nearDistance ) );
			_worldSpaceCorners[ 1 ] = eyeToWorld.TransformAffine( new Vector3( nearLeft, nearTop, -_nearDistance ) );
			_worldSpaceCorners[ 2 ] = eyeToWorld.TransformAffine( new Vector3( nearLeft, nearBottom, -_nearDistance ) );
			_worldSpaceCorners[ 3 ] = eyeToWorld.TransformAffine( new Vector3( nearRight, nearBottom, -_nearDistance ) );
			// far
			_worldSpaceCorners[ 4 ] = eyeToWorld.TransformAffine( new Vector3( farRight, farTop, -_farDistance ) );
			_worldSpaceCorners[ 5 ] = eyeToWorld.TransformAffine( new Vector3( farLeft, farTop, -_farDistance ) );
			_worldSpaceCorners[ 6 ] = eyeToWorld.TransformAffine( new Vector3( farLeft, farBottom, -_farDistance ) );
			_worldSpaceCorners[ 7 ] = eyeToWorld.TransformAffine( new Vector3( farRight, farBottom, -_farDistance ) );

			_recalculateWorldSpaceCorners = false;
		}

		protected virtual void UpdateVertexData()
		{
			if ( _recalculateVertexData )
			{
				// Note: Even though we could be dealing with general a projection matrix here,
				//       it is incompatible with the infinite far plane, thus, we need to work
				//		 with projection parameters.

				// Calc near plane corners
				Real nearLeft, nearRight, nearBottom, nearTop;
				CalculateProjectionParameters( out nearLeft, out nearRight, out nearBottom, out nearTop );

				// Treat infinite fardist as some arbitrary far value
				Real farDist = ( _farDistance == 0 ) ? 100000 : _farDistance;

				// Calc far palne corners
				Real ratio = _projectionType == Projection.Perspective ? _farDistance/_nearDistance : 1;
				var farLeft = nearLeft*ratio;
				var farRight = nearRight*ratio;
				var farBottom = nearBottom*ratio;
				var farTop = nearTop*ratio;

				// Calculate vertex positions
				// 0 is the origin
				// 1, 2, 3, 4 are the points on the near plane, top left first, clockwise
				// 5, 6, 7, 8 are the points on the far plane, top left first, clockwise
				var buffer = _vertexData.vertexBufferBinding.GetBuffer( 0 );

				var posPtr = buffer.Lock( BufferLocking.Discard );

#if !AXIOM_SAFE_ONLY
				unsafe
#endif
				{
					var pos = 0;
					var pPos = posPtr.ToFloatPointer();

					// near plane (remember frustum is going in -Z direction)
					pPos[ pos++ ] = nearLeft;
					pPos[ pos++ ] = nearTop;
					pPos[ pos++ ] = -_nearDistance;
					pPos[ pos++ ] = nearRight;
					pPos[ pos++ ] = nearTop;
					pPos[ pos++ ] = -_nearDistance;

					pPos[ pos++ ] = nearRight;
					pPos[ pos++ ] = nearTop;
					pPos[ pos++ ] = -_nearDistance;
					pPos[ pos++ ] = nearRight;
					pPos[ pos++ ] = nearBottom;
					pPos[ pos++ ] = -_nearDistance;

					pPos[ pos++ ] = nearRight;
					pPos[ pos++ ] = nearBottom;
					pPos[ pos++ ] = -_nearDistance;
					pPos[ pos++ ] = nearLeft;
					pPos[ pos++ ] = nearBottom;
					pPos[ pos++ ] = -_nearDistance;

					pPos[ pos++ ] = nearLeft;
					pPos[ pos++ ] = nearBottom;
					pPos[ pos++ ] = -_nearDistance;
					pPos[ pos++ ] = nearLeft;
					pPos[ pos++ ] = nearTop;
					pPos[ pos++ ] = -_nearDistance;

					// far plane (remember frustum is going in -Z direction)
					pPos[ pos++ ] = farLeft;
					pPos[ pos++ ] = farTop;
					pPos[ pos++ ] = -farDist;
					pPos[ pos++ ] = farRight;
					pPos[ pos++ ] = farTop;
					pPos[ pos++ ] = -farDist;

					pPos[ pos++ ] = farRight;
					pPos[ pos++ ] = farTop;
					pPos[ pos++ ] = -farDist;
					pPos[ pos++ ] = farRight;
					pPos[ pos++ ] = farBottom;
					pPos[ pos++ ] = -farDist;

					pPos[ pos++ ] = farRight;
					pPos[ pos++ ] = farBottom;
					pPos[ pos++ ] = -farDist;
					pPos[ pos++ ] = farLeft;
					pPos[ pos++ ] = farBottom;
					pPos[ pos++ ] = -farDist;

					pPos[ pos++ ] = farLeft;
					pPos[ pos++ ] = farBottom;
					pPos[ pos++ ] = -farDist;
					pPos[ pos++ ] = farLeft;
					pPos[ pos++ ] = farTop;
					pPos[ pos++ ] = -farDist;

					// Sides of the pyramid
					pPos[ pos++ ] = 0.0f;
					pPos[ pos++ ] = 0.0f;
					pPos[ pos++ ] = 0.0f;
					pPos[ pos++ ] = nearLeft;
					pPos[ pos++ ] = nearTop;
					pPos[ pos++ ] = -_nearDistance;

					pPos[ pos++ ] = 0.0f;
					pPos[ pos++ ] = 0.0f;
					pPos[ pos++ ] = 0.0f;
					pPos[ pos++ ] = nearRight;
					pPos[ pos++ ] = nearTop;
					pPos[ pos++ ] = -_nearDistance;

					pPos[ pos++ ] = 0.0f;
					pPos[ pos++ ] = 0.0f;
					pPos[ pos++ ] = 0.0f;
					pPos[ pos++ ] = nearRight;
					pPos[ pos++ ] = nearBottom;
					pPos[ pos++ ] = -_nearDistance;

					pPos[ pos++ ] = 0.0f;
					pPos[ pos++ ] = 0.0f;
					pPos[ pos++ ] = 0.0f;
					pPos[ pos++ ] = nearLeft;
					pPos[ pos++ ] = nearBottom;
					pPos[ pos++ ] = -_nearDistance;

					// Sides of the box
					pPos[ pos++ ] = nearLeft;
					pPos[ pos++ ] = nearTop;
					pPos[ pos++ ] = -_nearDistance;
					pPos[ pos++ ] = farLeft;
					pPos[ pos++ ] = farTop;
					pPos[ pos++ ] = -farDist;

					pPos[ pos++ ] = nearRight;
					pPos[ pos++ ] = nearTop;
					pPos[ pos++ ] = -_nearDistance;
					pPos[ pos++ ] = farRight;
					pPos[ pos++ ] = farTop;
					pPos[ pos++ ] = -farDist;

					pPos[ pos++ ] = nearRight;
					pPos[ pos++ ] = nearBottom;
					pPos[ pos++ ] = -_nearDistance;
					pPos[ pos++ ] = farRight;
					pPos[ pos++ ] = farBottom;
					pPos[ pos++ ] = -farDist;

					pPos[ pos++ ] = nearLeft;
					pPos[ pos++ ] = nearBottom;
					pPos[ pos++ ] = -_nearDistance;
					pPos[ pos++ ] = farLeft;
					pPos[ pos++ ] = farBottom;
					pPos[ pos++ ] = -farDist;
				}

				// don't forget to unlock!
				buffer.Unlock();

				_recalculateVertexData = false;
			}
		}

		/// <summary>
		/// Set view area size for orthographic mode.
		/// </summary>
		/// <remarks>
		/// Note that this method adjusts the frustum's aspect ratio.
		/// </remarks>
		/// <param name="w">Width of the area to be visible</param>
		/// <param name="h">Height of the area to be visible</param>
		public void SetOrthoWindow( float w, float h )
		{
			_orthoHeight = h;
			_aspectRatio = w/h;
			InvalidateFrustum();
		}

		public void SetFrustumExtents( float left, float right, float top, float bottom )
		{
			_frustumExtentsManuallySet = true;
			_left = left;
			_right = right;
			_top = top;
			_bottom = bottom;

			InvalidateFrustum();
		}

		public void ResetFrustumExtents()
		{
			_frustumExtentsManuallySet = false;
			InvalidateFrustum();
		}

		public void GetFrustumExtents( out float left, out float right, out float top, out float bottom )
		{
			UpdateFrustum();
			left = _left;
			right = _right;
			top = _top;
			bottom = _bottom;
		}

		#endregion Methods

		#region Overloaded operators

		/// <summary>
		///		An indexer that accepts a FrustumPlane enum value and return the appropriate plane side of the Frustum.
		/// </summary>
		public Plane this[ FrustumPlane plane ]
		{
			get
			{
				// make any pending updates to the calculated frustum
				UpdateFrustumPlanes();

				// convert the incoming plan enum type to a int
				var index = (int)plane;

				// access the planes array by index
				return _planes[ index ];
			}
		}

		#endregion Overloaded operators

		#region Implementation of MovableObject

		/// <summary>
		///    Local bounding radius of this camera.
		/// </summary>
		public override Real BoundingRadius
		{
			get
			{
				return ( _farDistance == 0 ) ? InfiniteFarPlaneDistance : _farDistance;
			}
		}

		/// <summary>
		///     Returns the bounding box for this frustum.
		/// </summary>
		public override AxisAlignedBox BoundingBox
		{
			get
			{
				return _boundingBox;
			}
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="camera"></param>
		public override void NotifyCurrentCamera( Camera camera )
		{
			// do nothing
		}

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

		/// <summary>
		/// Get the 'type flags' for this <see cref="Frustum"/>.
		/// </summary>
		/// <seealso cref="MovableObject.TypeFlags"/>
		public override uint TypeFlags
		{
			get
			{
				return (uint)SceneQueryTypeMask.Frustum;
			}
		}

		#endregion Implementation of MovableObject

		#region IRenderable Members

		public bool CastsShadows
		{
			get
			{
				return false;
			}
		}

		/// <summary>
		///     Returns the material to use when rendering this frustum.
		/// </summary>
		public Material Material
		{
			get
			{
				return _material;
			}
		}

		/// <summary>
		///     Just returns the best technique for our material.
		/// </summary>
		public Technique Technique
		{
			get
			{
				return _material.GetBestTechnique();
			}
		}

		protected RenderOperation renderOperation = new RenderOperation();

		public RenderOperation RenderOperation
		{
			get
			{
				UpdateVertexData();

				renderOperation.operationType = OperationType.LineList;
				renderOperation.useIndices = false;
				renderOperation.vertexData = _vertexData;

				return renderOperation;
			}
		}

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
		public LightList Lights
		{
			get
			{
				return dummyLightList;
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

		public virtual bool PolygonModeOverrideable
		{
			get
			{
				return false;
			}
		}

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

		public Vector3[] WorldSpaceCorners
		{
			get
			{
				UpdateWorldSpaceCorners();

				return _worldSpaceCorners;
			}
		}

		public Real GetSquaredViewDepth( Camera camera )
		{
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
			if ( customParams[ index ] == null )
			{
				throw new Exception( "A parameter was not found at the given index" );
			}
			else
			{
				return (Vector4)customParams[ index ];
			}
		}

		public void SetCustomParameter( int index, Vector4 val )
		{
			while ( customParams.Count <= index )
			{
				customParams.Add( Vector4.Zero );
			}
			customParams[ index ] = val;
		}

		public void UpdateCustomGpuParameter( GpuProgramParameters.AutoConstantEntry entry, GpuProgramParameters gpuParams )
		{
			if ( customParams[ entry.Data ] != null )
			{
				gpuParams.SetConstant( entry.PhysicalIndex, (Vector4)customParams[ entry.Data ] );
			}
		}

		#endregion IRenderable Members

		#region IDisposable Implementation

		/// <summary>
		/// Class level dispose method
		/// </summary>
		/// <remarks>
		/// When implementing this method in an inherited class the following template should be used;
		/// protected override void dispose( bool disposeManagedResources )
		/// {
		/// 	if ( !isDisposed )
		/// 	{
		/// 		if ( disposeManagedResources )
		/// 		{
		/// 			// Dispose managed resources.
		/// 		}
		///
		/// 		// There are no unmanaged resources to release, but
		/// 		// if we add them, they need to be released here.
		/// 	}
		///
		/// 	// If it is available, make the call to the
		/// 	// base class's Dispose(Boolean) method
		/// 	base.dispose( disposeManagedResources );
		/// }
		/// </remarks>
		/// <param name="disposeManagedResources">True if Unmanaged resources should be released.</param>
		protected override void dispose( bool disposeManagedResources )
		{
			if ( !IsDisposed )
			{
				if ( disposeManagedResources )
				{
					// Dispose managed resources.
					if ( renderOperation != null )
					{
						if ( !renderOperation.IsDisposed )
						{
							renderOperation.Dispose();
						}

						renderOperation = null;
					}

					if ( _vertexData != null )
					{
						if ( !_vertexData.IsDisposed )
						{
							_vertexData.Dispose();
						}

						_vertexData = null;
					}

					if ( _material != null )
					{
						_material = null;
					}

					dummyLightList.Clear();
				}

				// There are no unmanaged resources to release, but
				// if we add them, they need to be released here.
			}

			base.dispose( disposeManagedResources );
		}

		#endregion IDisposable Implementation
	}
}