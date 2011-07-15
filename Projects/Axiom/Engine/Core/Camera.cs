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

using System.Collections.Generic;
using System.Diagnostics;
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
		#region Fields and Properties

		#region SceneManager Property

		/// <summary>
		///		Parent scene manager.
		/// </summary>
		protected SceneManager sceneManager;
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

		#endregion SceneManager Property

		#region Orientation Property

		/// <summary>
		///		Camera orientation.
		/// </summary>
		protected Quaternion orientation;
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

		#endregion Orientation Property

		#region Position Property

		/// <summary>
		///		Camera position.
		/// </summary>
		protected Vector3 position;
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
				if ( value == Vector3.Zero )
					return;

				// Remember, camera points down -Z of local axes!
				// Therefore reverse direction of direction vector before determining local Z
				Vector3 zAdjustVector = -value;
				zAdjustVector.Normalize();

				if ( isYawFixed )
				{
					Vector3 xVector = yawFixedAxis.Cross( zAdjustVector );
					xVector.Normalize();

					Vector3 yVector = zAdjustVector.Cross( xVector );
					yVector.Normalize();

					orientation = Quaternion.FromAxes( xVector, yVector, zAdjustVector );
				}
				else
				{
					// Get axes from current quaternion
					Vector3 xAxis, yAxis, zAxis;

					// get the vector components of the derived orientation vector
					this.realOrientation.ToAxes( out xAxis, out yAxis, out zAxis );

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

					orientation = rotationQuat * orientation;
				}

				// transform to parent space
				if ( parentNode != null )
				{
					orientation = parentNode.DerivedOrientation.Inverse() * orientation;
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
		protected Quaternion derivedOrientation;
		/// <summary>
		///	Gets the derived orientation of the camera, including any
		/// rotation inherited from a node attachment and reflection matrix.
		/// </summary>
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
		protected Vector3 derivedPosition;
		/// <summary>
		///	Gets the derived position of the camera, including any
		/// rotation inherited from a node attachment and reflection matrix.
		/// </summary>
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
		protected Quaternion realOrientation;
		/// <summary>
		/// Gets the real world orientation of the camera, including any
		/// rotation inherited from a node attachment
		/// </summary>
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
		protected Vector3 realPosition;
		/// <summary>
		/// Gets the real world orientation of the camera, including any
		/// rotation inherited from a node attachment
		/// </summary>
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

		#endregion FixedYawAxis Property

		#region PolygonMode Property

		/// <summary>
		///		Rendering type (wireframe, solid, point).
		/// </summary>
		protected PolygonMode sceneDetail;
		/// <summary>
		///		Sets the level of rendering detail required from this camera.
		/// </summary>
		/// <remarks>
		///		Each camera is set to render at full detail by default, that is
		///		with full texturing, lighting etc. This method lets you change
		///		that behavior, allowing you to make the camera just render a
		///		wireframe view, for example.
		/// </remarks>
		public PolygonMode PolygonMode
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

		#endregion PolygonMode Property

		#region RenderedFaceCount Property

		/// <summary>
		///		Stored number of visible faces in the last render.
		/// </summary>
		protected int faceCountRenderedLastFrame;
		/// <summary>
		/// Gets the last count of triangles visible in the view of this camera.
		/// </summary>
		public int RenderedFaceCount
		{
			get
			{
				return faceCountRenderedLastFrame;
			}
		}

		#endregion RenderedFaceCount Property

		#region RenderedBatchCount Property

		/// <summary>
		///		Stored number of visible batches in the last render.
		/// </summary>
		protected int batchCountRenderedLastFrame;
		/// <summary>
		/// Gets the last count of batches visible in the view of this camera.
		/// </summary>
		public int RenderedBatchCount
		{
			get
			{
				return batchCountRenderedLastFrame;
			}
		}

		#endregion RenderedBatchCount Property

		#region AutoTrackingTarget Property

		/// <summary>
		///		SceneNode which this Camera will automatically track.
		/// </summary>
		protected SceneNode autoTrackTarget;
		/// <summary>
		///		SceneNode which this Camera will automatically track.
		/// </summary>
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

		#endregion AutoTrackingTarget Property

		#region AutoTrackingOffset Property

		/// <summary>
		///		Tracking offset for fine tuning.
		/// </summary>
		protected Vector3 autoTrackOffset;
		/// <summary>
		///		Tracking offset for fine tuning.
		/// </summary>
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

		#endregion AutoTrackingOffset Property

		#region LodBias Property

		/// <summary>
		///		Scene LOD factor used to adjust overall LOD.
		/// </summary>
		protected float sceneLodFactor;
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

		#endregion LodBias Property

		#region LodCamera Property

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
		public Camera LodCamera
		{
			get
			{
				return this._lodCamera ?? this;
			}
			set
			{
				if ( this == value )
				{
					_lodCamera = null;
				}
				else
				{
					_lodCamera = value;
				}
			}
		}

		#endregion LodCamera Property

		#region InverseLodBias Property

		/// <summary>
		///		Inverted scene LOD factor, can be used by Renderables to adjust their LOD.
		/// </summary>
		protected float invSceneLodFactor;
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
		protected bool isWindowSet;
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

		#endregion IsWindowSet Property

		#region WindowPlanes Property

		/// <summary>
		/// Windowed viewport clip planes.
		/// </summary>
		protected List<Plane> windowClipPlanes = new List<Plane>();
		/// <summary>
		///  Gets the window clip planes, only applicable if isWindowSet == true
		/// </summary>
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
		///		The last viewport to be added using this camera.
		/// </summary>
		protected Viewport lastViewport;
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

		#endregion Viewport Property

		#region AutoAspectRatio Property

		/// <summary>
		///		Whether aspect ratio will automaticaally be recalculated when a vieport changes its size.
		/// </summary>
		protected bool autoAspectRatio;
		/// <summary>
		///		If set to true a viewport that owns this frustum will be able to
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
				autoAspectRatio = value;
			}
		}

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

		private Frustum cullFrustum;

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
		public Frustum CullFrustum
		{
			get
			{
				return cullFrustum;
			}
			set
			{
				cullFrustum = value;
			}
		}

		#endregion Fields and Properties

		#region Constructors

		public Camera( string name, SceneManager sceneManager )
			: base(name)
		{
			// Init camera location & direction

			// Locate at (0,0,0)
			position = Vector3.Zero;
			derivedPosition = Vector3.Zero;

			// Point down -Z axis
			orientation = Quaternion.Identity;
			derivedOrientation = Quaternion.Identity;

			// Reasonable defaults to camera params
			sceneDetail = PolygonMode.Solid;

			// Default to fixed yaw (freelook)
			this.FixedYawAxis = Vector3.UnitY;

			// Record name & SceneManager
			this.sceneManager = sceneManager;

			InvalidateFrustum();
			InvalidateView();

			// Init no tracking
			autoTrackTarget = null;
			autoTrackOffset = Vector3.Zero;

			// default these to 1 so Lod default to normal
			sceneLodFactor = this.invSceneLodFactor = 1.0f;

			// default to using the rendering distance
			useRenderingDistance = true;
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
				if ( parentNode != null )
				{
					if ( _recalculateView ||
						parentNode.DerivedOrientation != _lastParentOrientation ||
						parentNode.DerivedPosition != _lastParentPosition )
					{
						// we are out of date with the parent scene node
						_lastParentOrientation = parentNode.DerivedOrientation;
						_lastParentPosition = parentNode.DerivedPosition;
						realOrientation = _lastParentOrientation * orientation;
						realPosition = ( _lastParentOrientation * position ) + _lastParentPosition;
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

				if ( IsReflected &&
					linkedReflectionPlane != null &&
					lastLinkedReflectionPlane != linkedReflectionPlane.DerivedPlane )
				{

					ReflectionPlane = linkedReflectionPlane.DerivedPlane;
					ReflectionMatrix = Utility.BuildReflectionMatrix( ReflectionPlane );
					lastLinkedReflectionPlane = linkedReflectionPlane.DerivedPlane;
					_recalculateView = true;
					recalculateWindow = true;
				}

				// Deriving reflected orientation / position
				if ( _recalculateView )
				{
					if ( IsReflected )
					{
						// Calculate reflected orientation, use up-vector as fallback axis.
						Vector3 dir = realOrientation * Vector3.NegativeUnitZ;
						Vector3 rdir = dir.Reflect( ReflectionPlane.Normal );
						Vector3 up = realOrientation * Vector3.UnitY;
						derivedOrientation = dir.GetRotationTo( rdir, up ) * realOrientation;

						// Calculate reflected position.
						derivedPosition = ReflectionMatrix.TransformAffine( realPosition );
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

		new public bool IsObjectVisible( AxisAlignedBox box )
		{
			if ( null != CullFrustum )
			{
				return CullFrustum.IsObjectVisible( box );
			}
			else
			{
				return base.IsObjectVisible( box );
			}
		}

		new public bool IsObjectVisible( AxisAlignedBox box, out FrustumPlane culledBy )
		{
			if ( null != CullFrustum )
			{
				return CullFrustum.IsObjectVisible( box, out culledBy );
			}
			else
			{
				return base.IsObjectVisible( box, out culledBy );
			}
		}

		new public bool IsObjectVisible( Sphere sphere )
		{
			if ( null != CullFrustum )
			{
				return CullFrustum.IsObjectVisible( sphere );
			}
			else
			{
				return base.IsObjectVisible( sphere );
			}
		}

		new public bool IsObjectVisible( Sphere sphere, out FrustumPlane culledBy )
		{
			if ( null != CullFrustum )
			{
				return CullFrustum.IsObjectVisible( sphere, out culledBy );
			}
			else
			{
				return base.IsObjectVisible( sphere, out culledBy );
			}
		}

		new public bool IsObjectVisible( Vector3 vertex )
		{
			if ( null != CullFrustum )
			{
				return CullFrustum.IsObjectVisible( vertex );
			}
			else
			{
				return base.IsObjectVisible( vertex );
			}
		}

		new public bool IsObjectVisible( Vector3 vertex, out FrustumPlane culledBy )
		{
			if ( null != CullFrustum )
			{
				return CullFrustum.IsObjectVisible( vertex, out culledBy );
			}
			else
			{
				return base.IsObjectVisible( vertex, out culledBy );
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
				if ( null != CullFrustum )
				{
					return CullFrustum.FrustumPlanes;
				}
				else
				{
					return base.FrustumPlanes;
				}
			}
		}

		public override bool ProjectSphere( Sphere sphere, out float left, out float top, out float right, out float bottom )
		{
			if ( null != CullFrustum )
			{
				return CullFrustum.ProjectSphere( sphere, out left, out top, out right, out bottom );
			}
			else
			{
				return base.ProjectSphere( sphere, out left, out top, out right, out bottom );
			}
		}

		public override float Near
		{
			get
			{
				if ( null != CullFrustum )
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
				if ( null != CullFrustum )
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
				if ( null != CullFrustum )
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
				if ( null != CullFrustum )
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
				if ( null != CullFrustum )
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
				if ( null != CullFrustum )
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
				return Near * 1.5f;
			}
		}

		public override void NotifyCurrentCamera( Axiom.Core.Camera camera )
		{
			// Do nothing
		}

		/// <summary>
		///    Called by the scene manager to let this camera know how many faces were rendered within
		///    view of this camera every frame.
		/// </summary>
		/// <param name="renderedFaceCount"></param>
		internal void NotifyRenderedFaces( int renderedFaceCount )
		{
			faceCountRenderedLastFrame = renderedFaceCount;
		}

		/// <summary>
		///    Called by the scene manager to let this camera know how many batches were rendered within
		///    view of this camera every frame.
		/// </summary>
		/// <param name="renderedFaceCount"></param>
		internal void NotifyRenderedBatches( int renderedBatchCount )
		{
			batchCountRenderedLastFrame = renderedBatchCount;
		}
		#endregion SceneObject Implementation

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
			this.Direction = ( target - realPosition );
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
			Quaternion q = Quaternion.FromAngleAxis(
					Utility.DegreesToRadians( (Real)degrees ),
					axis );
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

			// Calculate general projection parameters
			Real vpLeft, vpRight, vpBottom, vpTop;
			CalculateProjectionParameters( out vpLeft, out vpRight, out vpBottom, out vpTop );

			float vpWidth = vpRight - vpLeft;
			float vpHeight = vpTop - vpBottom;

			float wvpLeft = vpLeft + windowLeft * vpWidth;
			float wvpRight = vpLeft + windowRight * vpWidth;
			float wvpTop = vpTop - windowTop * vpHeight;
			float wvpBottom = vpTop - windowBottom * vpHeight;

			Vector3 vpUpLeft = new Vector3( wvpLeft, wvpTop, -Near );
			Vector3 vpUpRight = new Vector3( wvpRight, wvpTop, -Near );
			Vector3 vpBottomLeft = new Vector3( wvpLeft, wvpBottom, -Near );
			Vector3 vpBottomRight = new Vector3( wvpRight, wvpBottom, -Near );

			Matrix4 inv = _viewMatrix.Inverse();

			Vector3 vwUpLeft = inv.TransformAffine( vpUpLeft );
			Vector3 vwUpRight = inv.TransformAffine( vpUpRight );
			Vector3 vwBottomLeft = inv.TransformAffine( vpBottomLeft );
			Vector3 vwBottomRight = inv.TransformAffine( vpBottomRight );

			windowClipPlanes.Clear();

			if ( ProjectionType == Projection.Perspective )
			{
				Vector3 pos = GetPositionForViewUpdate();

				windowClipPlanes.Add( new Plane( pos, vwBottomLeft, vwUpLeft ) );
				windowClipPlanes.Add( new Plane( pos, vwUpLeft, vwUpRight ) );
				windowClipPlanes.Add( new Plane( pos, vwUpRight, vwBottomRight ) );
				windowClipPlanes.Add( new Plane( pos, vwBottomRight, vwBottomLeft ) );
			}
			else
			{
				Vector3 x_axis = new Vector3( inv.m00, inv.m01, inv.m02 );
				Vector3 y_axis = new Vector3( inv.m10, inv.m11, inv.m12 );
				x_axis.Normalize();
				y_axis.Normalize();

				windowClipPlanes.Add( new Plane( x_axis, vwBottomLeft ) );
				windowClipPlanes.Add( new Plane( -x_axis, vwUpRight ) );
				windowClipPlanes.Add( new Plane( y_axis, vwBottomLeft ) );
				windowClipPlanes.Add( new Plane( -y_axis, vwUpLeft ) );
			}

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

			return (Plane)windowClipPlanes[ index ];
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
		public Ray GetCameraToViewportRay( float screenX, float screenY )
		{
			Matrix4 inverseVP = ( _projectionMatrix * _viewMatrix ).Inverse();

			Real nx = ( 2.0f * screenX ) - 1.0f;
			Real ny = 1.0f - ( 2.0f * screenY );
			Vector3 nearPoint = new Vector3( nx, ny, -1.0f );
			// Use midPoint rather than far point to avoid issues with infinite projection
			Vector3 midPoint = new Vector3( nx, ny, 0.0f );

			// Get ray origin and ray target on near plane in world space
			Vector3 rayOrigin, rayTarget;

			rayOrigin = inverseVP * nearPoint;
			rayTarget = inverseVP * midPoint;

			Vector3 rayDirection = rayTarget - rayOrigin;
			rayDirection.Normalize();

			return new Ray( rayOrigin, rayDirection );
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
		public PlaneBoundedVolume GetCameraToViewportBoxVolume( Real screenLeft, Real screenTop, Real screenRight, Real screenBottom, bool includeFarPlane )
		{
			PlaneBoundedVolume outVolume = new PlaneBoundedVolume();

			if ( ProjectionType == Projection.Perspective )
			{
				// Use the corner rays to generate planes
				Ray ul = GetCameraToViewportRay( screenLeft, screenTop );
				Ray ur = GetCameraToViewportRay( screenRight, screenTop );
				Ray bl = GetCameraToViewportRay( screenLeft, screenBottom );
				Ray br = GetCameraToViewportRay( screenRight, screenBottom );

				Vector3 normal;
				// top plane
				normal = ul.Direction.Cross( ur.Direction );
				normal.Normalize();
				outVolume.planes.Add( new Plane( normal, DerivedPosition ) );

				// right plane
				normal = ur.Direction.Cross( br.Direction );
				normal.Normalize();
				outVolume.planes.Add( new Plane( normal, DerivedPosition ) );

				// bottom plane
				normal = br.Direction.Cross( bl.Direction );
				normal.Normalize();
				outVolume.planes.Add( new Plane( normal, DerivedPosition ) );

				// left plane
				normal = bl.Direction.Cross( ul.Direction );
				normal.Normalize();
				outVolume.planes.Add( new Plane( normal, DerivedPosition ) );
			}
			else
			{
				// ortho planes are parallel to frustum planes

				Ray ul = GetCameraToViewportRay( screenLeft, screenTop );
				Ray br = GetCameraToViewportRay( screenRight, screenBottom );

				UpdateFrustumPlanes();
				outVolume.planes.Add( new Plane( FrustumPlanes[ (int)FrustumPlane.Top ].Normal, ul.Origin ) );
				outVolume.planes.Add( new Plane( FrustumPlanes[ (int)FrustumPlane.Right ].Normal, br.Origin ) );
				outVolume.planes.Add( new Plane( FrustumPlanes[ (int)FrustumPlane.Bottom ].Normal, br.Origin ) );
				outVolume.planes.Add( new Plane( FrustumPlanes[ (int)FrustumPlane.Left ].Normal, ul.Origin ) );
			}

			// near/far planes applicable to both projection types
			outVolume.planes.Add( new Plane( FrustumPlanes[ (int)FrustumPlane.Near ] ) );
			if ( includeFarPlane )
				outVolume.planes.Add( new Plane( FrustumPlanes[ (int)FrustumPlane.Far ] ) );

			return outVolume;
		}

		/// <summary>
		///		Notifies this camera that a viewport is using it.
		/// </summary>
		/// <param name="viewport">Viewport that is using this camera.</param>
		public void NotifyViewport( Viewport viewport )
		{
			lastViewport = viewport;
		}

		#endregion Public methods

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

		#endregion Internal engine methods
	}
}