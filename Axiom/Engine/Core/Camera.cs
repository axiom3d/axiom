#region LGPL License
/*
Axiom Game Engine Library
Copyright (C) 2003  Axiom Project Team

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
using Axiom.Enumerations;
using Axiom.MathLib;
using Axiom.SubSystems.Rendering;

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
	///		SceneNode.Objects.Add. If this is done the Camera will combine it's own
	///		position/orientation settings with it's parent SceneNode. 
	///		This is useful for implementing more complex Camera / object
	///		relationships i.e. having a camera attached to a world object.
	/// </remarks>

	// TESTME
	public class Camera : SceneObject
	{
		#region Protected member variables

		protected Frustum				frustum;
		protected SceneManager	sceneManager;
		protected Quaternion			orientation;
		protected Vector3				position;
		protected Quaternion			lastParentOrientation;
		protected Vector3				lastParentPosition;
		protected Quaternion			derivedOrientation;
		protected Vector3				derivedPosition;				
		protected float fieldOfView, farDistance, nearDistance, aspectRatio;
		protected bool					isYawFixed;
		protected Vector3				yawFixedAxis;
		protected Projection			projectionType;
		protected SceneDetailLevel sceneDetail;
		protected Matrix4				projectionMatrix;
		protected Matrix4				viewMatrix;
		protected bool					recalculateFrustum;
		protected bool					recalculateView;
		protected int						numFacesRenderedLastFrame;
		protected SceneNode			autoTrackTarget;
		protected Vector3				autoTrackOffset;	

		/** Temp coefficient values calculated from a frustum change,
			used when establishing the frustum planes when the view changes. */
		float[] coeffL = new float[2];
		float[] coeffR = new float[2];
		float[] coeffB = new float[2];
		float[] coeffT = new float[2];

		#endregion

		#region Constructors

		public Camera(String name, SceneManager sceneManager) 
		{
			// Init camera location & direction

			// Locate at (0,0,0)
			this.position = Vector3.Zero;
			this.lastParentPosition = Vector3.Zero;
			this.derivedPosition = Vector3.Zero;

			// Point down -Z axis
			this.orientation = Quaternion.Identity;
			this.lastParentOrientation = Quaternion.Identity;
			this.derivedOrientation = Quaternion.Identity;

			// Reasonable defaults to camera params
			this.frustum = new Frustum();
			this.fieldOfView = 45.0f;
			this.nearDistance = 0.1f;
			this.farDistance = 100000.0f;
			this.aspectRatio = 1.33333333333333f;
			this.projectionType = Projection.Perspective;
			this.sceneDetail = SceneDetailLevel.Solid;

			// Default to fixed yaw (freelook)
			this.FixedYawAxis = Vector3.UnitY;
			
			this.recalculateFrustum = true;
			this.recalculateView = true;

			// Init matrices
			this.viewMatrix = Matrix4.Zero;
			this.projectionMatrix = Matrix4.Zero;

			// Record name & SceneManager
			this.name = name;
			this.sceneManager = sceneManager;

			// Init no tracking
			this.autoTrackTarget = null;
			this.autoTrackOffset = Vector3.Zero;

			UpdateView();

		}

		#endregion

		#region Protected methods

		/// <summary>
		///		Updates the frustum data.
		/// </summary>
		protected void UpdateFrustum()
		{
			if(recalculateFrustum)
			{
				switch(projectionType)
				{
					case Projection.Perspective:
					{
						// recreate the projection matrix
						projectionMatrix = Engine.Instance.RenderSystem.MakeProjectionMatrix(fieldOfView, aspectRatio, nearDistance, farDistance);

						// Calculate co-efficients for the frustum planes
						// Special-cased for L = -R and B = -T i.e. viewport centered 
						// on direction vector.
						// Taken from ideas in WildMagic 0.2 http://www.magic-software.com
						float thetaY = MathUtil.DegreesToRadians(fieldOfView * 0.5f);
						float tanThetaY = MathUtil.Tan(thetaY);
						float tanThetaX = tanThetaY * aspectRatio;

						float vpTop = tanThetaY * nearDistance;
						float vpRight = tanThetaX * nearDistance;
						float vpBottom = -vpTop;
						float vpLeft = -vpRight;

						float nSqr = nearDistance * nearDistance;
						float lSqr = vpRight * vpRight;
						float rSqr = lSqr;
						float tSqr = vpTop * vpTop;
						float bSqr = tSqr;

						float inverseLength = 1.0f / MathUtil.Sqrt( nSqr + lSqr );
						coeffL[0] = nearDistance * inverseLength;
						coeffL[1] = -vpLeft * inverseLength;

						inverseLength = 1.0f / MathUtil.Sqrt( nSqr + rSqr );
						coeffR[0] = -nearDistance * inverseLength;
						coeffR[1] = vpRight * inverseLength;

						inverseLength = 1.0f / MathUtil.Sqrt( nSqr + bSqr );
						coeffB[0] = nearDistance * inverseLength;
						coeffB[1] = -vpBottom * inverseLength;

						inverseLength = 1.0f / MathUtil.Sqrt( nSqr + tSqr );
						coeffT[0] = -nearDistance * inverseLength;
						coeffT[1] = vpTop * inverseLength;

					} break;
					case Projection.Orthographic:
					{
						// TODO: Add Orthographic projection
					} break;
				}

				recalculateFrustum = false;
			}
		}

		/// <summary>
		///		Updates the view matrix.
		/// </summary>
		protected void UpdateView()
		{
			// check if the view is out of date
			if(IsViewOutOfDate())
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
				Matrix3 rotation = derivedOrientation.ToRotationMatrix();
				Vector3 left = rotation.GetColumn(0);
				Vector3 up = rotation.GetColumn(1);
				Vector3 direction = rotation.GetColumn(2);

				// make the translation relative to the new axis
				Matrix3 rotationT = rotation.Transpose();
				Vector3 translation = -rotationT * derivedPosition;

				// construct the final matrix, first zero out the view matrix
				//viewMatrix = Matrix4.Zero;

				// initialize the upper 3x3 portion with the rotation
				viewMatrix = rotationT;

				// add the translation portion, add set 1 for the bottom right portion
				viewMatrix[0,3] = translation.x;
				viewMatrix[1,3] = translation.y;
				viewMatrix[2,3] = translation.z;
				//viewMatrix[3,3] = 1.0f;

				// update the frustum planes
				UpdateFrustum();

				// Use camera view for frustum calcs, using -Z rather than Z
				Vector3 camDirection = derivedOrientation * -Vector3.UnitZ;

				// calculate distance along direction to our derived position
				float distance = camDirection.Dot(derivedPosition);

				// left plane
				frustum[FrustumPlane.Left].Normal = coeffL[0] * left + 	coeffL[1] * camDirection;
				frustum[FrustumPlane.Left].D = -derivedPosition.Dot(frustum[FrustumPlane.Left].Normal);

				// right plane
				frustum[FrustumPlane.Right].Normal = coeffR[0] * left + coeffR[1] * camDirection;
				frustum[FrustumPlane.Right].D = -derivedPosition.Dot(frustum[FrustumPlane.Right].Normal);

				// bottom plane
				frustum[FrustumPlane.Bottom].Normal = coeffB[0] * up + coeffB[1] * camDirection;
				frustum[FrustumPlane.Bottom].D = -derivedPosition.Dot(frustum[FrustumPlane.Bottom].Normal);

				// top plane
				frustum[FrustumPlane.Top].Normal = coeffT[0] * up + coeffT[1] * camDirection;
				frustum[FrustumPlane.Top].D = -derivedPosition.Dot(frustum[FrustumPlane.Top].Normal);

				// far plane
				frustum[FrustumPlane.Far].Normal = -camDirection;
				frustum[FrustumPlane.Far].D = distance + farDistance;

				// near plane
				frustum[FrustumPlane.Near].Normal = camDirection;
				frustum[FrustumPlane.Near].D = -(distance + nearDistance);

				// update since we have now recalculated everything
				recalculateView = false;
			}
		}

		/// <summary>
		///		Evaluates whether or not the view matrix is out of date.
		/// </summary>
		/// <returns></returns>
		protected bool IsViewOutOfDate()
		{
			// are we attached to another node?
			if(parentNode != null)
			{
				if(!recalculateView && parentNode.DerivedOrientation == lastParentOrientation &&
					parentNode.DerivedPosition == lastParentPosition)
				{
					return false;
				}
				else
				{
					// we are out of date with the parent scene node
					lastParentOrientation = parentNode.DerivedOrientation;
					lastParentPosition = parentNode.DerivedPosition;
					derivedOrientation = lastParentOrientation * orientation;
					derivedPosition = (lastParentOrientation * position) + lastParentPosition;
					return true;
				}
			}
			else
			{
				// rely on own updates
				derivedOrientation = orientation;
				derivedPosition = position;
				return recalculateView;
			}
		}

		/// <summary>
		///		Evaluates whether or not the view frustum is out of date.
		/// </summary>
		/// <returns></returns>
		protected bool IsFrustumOutOfDate()
		{
			return recalculateFrustum;
		}

		#endregion

		#region SceneObject Implementation

		internal override void UpdateRenderQueue(RenderQueue pQueue)
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

		internal override void NotifyCurrentCamera(Axiom.Core.Camera pCamera)
		{
			// Do nothing
		}

		#endregion

		#region Public Properties
	
		/// <summary>
		/// Returns the current SceneManager that this camera is using.
		/// </summary>
		public SceneManager SceneManager
		{
			get { return sceneManager; }
		}

		// <summary>
		/// Gets/Sets the type of projection to use (orthographic or perspective). Default is perspective.
		/// </summary>
		public Projection ProjectionType
		{
			get { return projectionType; }
			set 
			{ 
				projectionType = value;	
				recalculateFrustum = true; 
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
			get { return sceneDetail; }
			set { sceneDetail = value; }
		}

		/// <summary>
		///		Gets the viewing frustum of this camera.
		/// </summary>
		public Frustum Frustum
		{
			get { return frustum; }
		}

		/// <summary>
		/// Gets/Sets the cameras position.
		/// </summary>
		public Vector3 Position
		{
			get { return position; }
			set 
			{ 
				position = value;	
				recalculateView = true; 
			}
		}

		/// <summary>
		/// Gets/Sets the cameras direction vector.
		/// </summary>
		public Vector3 Direction
		{
			// Direction points down the negatize Z axis by default.
			get { return orientation * -Vector3.UnitZ; }
			set
			{
				Vector3 direction = value;

				// Do nothing if given a zero vector
				// (Replaced assert since this could happen with auto tracking camera and
				//  camera passes through the lookAt point)
				if (direction == Vector3.Zero) 
					return;

				// Remember, camera points down -Z of local axes!
				// Therefore reverse direction of direction vector before determining local Z
				Vector3 zAdjustVector = -direction;
				zAdjustVector.Normalize();

				if( isYawFixed )
				{
					Vector3 xVector = yawFixedAxis.Cross( zAdjustVector );
					xVector.Normalize();

					Vector3 yVector = zAdjustVector.Cross( xVector );
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
					derivedOrientation.ToAxes(out xAxis, out yAxis, out zAxis);

					Quaternion rotationQuat;

					if (-zAdjustVector == zAxis)
					{
						// Oops, a 180 degree turn (infinite possible rotation axes)
						// Default to yaw i.e. use current UP
						rotationQuat = Quaternion.FromAngleAxis(MathUtil.PI, yAxis);
					}
					else
					{
						// Derive shortest arc to new direction
						rotationQuat = zAxis.GetRotationTo(zAdjustVector);
					}

					orientation = rotationQuat * orientation;
				}

				// TODO: If we have a fixed yaw axis, we mustn't break it by using the
				// shortest arc because this will sometimes cause a relative yaw
				// which will tip the camera

				recalculateView = true;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		public Vector3 FixedYawAxis
		{
			get { return yawFixedAxis; }
			set 
			{ 
				yawFixedAxis = value; 

				if(yawFixedAxis != Vector3.Zero)
					isYawFixed = true;
			}
		}

		/// <summary>
		/// Gets the last count of triangles visible in the view of this camera.
		/// </summary>
		public int NumRenderedFaces
		{
			get { return numFacesRenderedLastFrame; }
		}

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
		public float FOV
		{
			get { return fieldOfView; } 
			set
			{
				fieldOfView = value;
				recalculateFrustum = true;
			}
		}

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
		public float Near
		{
			get { return nearDistance; }
			set
			{
				Debug.Assert(value > 0, "Near clip distance must be greater than zero.");

				nearDistance = value;
				recalculateFrustum = true;
			}
		}

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
		public float Far
		{
			get { return farDistance; }
			set
			{
				farDistance = value;
				recalculateFrustum = true;
			}
		}

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
		public float AspectRatio
		{
			get { return aspectRatio; }
			set
			{
				aspectRatio = value;
				recalculateFrustum = true;
			}
		}

		/// <summary>
		/// Gets the projection matrix for this camera.
		/// </summary>
		// TODO: Decide if internal use only or not.
		public Matrix4 ProjectionMatrix
		{
			get
			{
				UpdateFrustum();

				return projectionMatrix;
			}
		}

		/// <summary>
		/// Gets the view matrix for this camera.
		/// </summary>
		// TODO: Decide if internal use only or not.
		public Matrix4 ViewMatrix
		{
			get
			{
				UpdateView();

				return viewMatrix;
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

		#endregion

		#region Public methods

		/// <summary>
		/// Moves the camera's position by the vector offset provided along world axes.
		/// </summary>
		/// <param name="offset"></param>
		public void Move(Vector3 offset)
		{
			position = position + offset;
			recalculateView = true;
		}

		/// <summary>
		/// Moves the camera's position by the vector offset provided along it's own axes (relative to orientation).
		/// </summary>
		/// <param name="offset"></param>
		public void MoveRelative(Vector3 offset)
		{
			// Transform the axes of the relative vector by camera's local axes
			Vector3 transform = orientation * offset;

			position = position + transform;
			recalculateView = true;
		}

		/// <summary>
		///		Specifies a target that the camera should look at.
		/// </summary>
		/// <param name="target"></param>
		public void LookAt(Vector3 target)
		{
			UpdateView();
			this.Direction = (target - derivedPosition);
		}

		/// <summary>
		///		Pitches the camera up/down counter-clockwise around it's local x axis.
		/// </summary>
		/// <param name="degrees"></param>
		public void Pitch(float degrees)
		{
			Vector3 xAxis = orientation * Vector3.UnitX;
			Rotate(xAxis, degrees);

			recalculateView = true;
		}

		/// <summary>
		///		Rolls the camera counter-clockwise, in degrees, around its local y axis.
		/// </summary>
		/// <param name="degrees"></param>
		public void Yaw(float degrees)
		{
			Vector3 yAxis;

			if(isYawFixed)
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

			recalculateView = true;
		}

		/// <summary>
		///		Rolls the camera counter-clockwise, in degrees, around its local z axis.
		/// </summary>
		/// <param name="degrees"></param>
		public void Roll(float degrees)
		{
			// Rotate around local Z axis
			Vector3 zAxis = orientation * Vector3.UnitZ;
			Rotate(zAxis, degrees);

			recalculateView = true;
		}

		/// <summary>
		///		Rotates the camera about an arbitrary axis.
		/// </summary>
		/// <param name="quat"></param>
		public void Rotate(Quaternion quat)
		{
			// Note the order of the multiplication
			orientation = quat * orientation;

			recalculateView = true;
		}

		/// <summary>
		///		Rotates the camera about an arbitrary axis.
		/// </summary>
		/// <param name="axis"></param>
		/// <param name="degrees"></param>
		public void Rotate(Vector3 axis, float degrees)
		{
			Quaternion q = Quaternion.FromAngleAxis(MathUtil.DegreesToRadians(degrees), axis);
			Rotate(q);

		}

		/// <summary>
		///		Overloaded method.
		/// </summary>
		/// <param name="box"></param>
		/// <returns></returns>
		public bool IsObjectVisible(AxisAlignedBox box)
		{
			// this overload doesnt care about the clipping plane, but we gotta
			// pass in something to the out param anyway
			FrustumPlane dummy;
			return IsObjectVisible(box, out dummy);
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
		public bool IsObjectVisible(AxisAlignedBox box, out FrustumPlane culledBy)
		{
			// Null boxes are always invisible
			if (box.IsNull) 
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
			for (int i = 0; i < 6; i++)
			{
				// to make this loop easier, get the enum type based on the current plane
				FrustumPlane plane = (FrustumPlane)i;

				if (frustum[plane].GetSide(corners[0]) == PlaneSide.Negative &&
					frustum[plane].GetSide(corners[1]) == PlaneSide.Negative &&
					frustum[plane].GetSide(corners[2]) == PlaneSide.Negative &&
					frustum[plane].GetSide(corners[3]) == PlaneSide.Negative &&
					frustum[plane].GetSide(corners[4]) == PlaneSide.Negative &&
					frustum[plane].GetSide(corners[5]) == PlaneSide.Negative &&
					frustum[plane].GetSide(corners[6]) == PlaneSide.Negative &&
					frustum[plane].GetSide(corners[7]) == PlaneSide.Negative)
				{
					// ALL corners on negative side therefore out of view
					culledBy = plane;
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
		public bool IsObjectVisible(Sphere sphere)
		{
			// this overload doesnt care about the clipping plane, but we gotta
			// pass in something to the out param anyway
			FrustumPlane dummy;
			return IsObjectVisible(sphere, out dummy);
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
		public bool IsObjectVisible(Sphere sphere, out FrustumPlane culledBy)
		{
			// Make any pending updates to the calculated frustum
			UpdateView();

			// For each plane, see if sphere is on negative side
			// If so, object is not visible
			for (int i = 0; i < 6; i++)
			{
				// to make this loop easier, get the enum type based on the current plane
				FrustumPlane plane = (FrustumPlane)i;

				// If the distance from sphere center to plane is negative, and 'more negative' 
				// than the radius of the sphere, sphere is outside frustum
				if (frustum[plane].GetDistance(sphere.Center) < -sphere.Radius)
				{
					// ALL corners on negative side therefore out of view
					culledBy = plane;
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
		public bool IsObjectVisible(Vector3 vertex)
		{
			// this overload doesnt care about the clipping plane, but we gotta
			// pass in something to the out param anyway
			FrustumPlane dummy;
			return IsObjectVisible(vertex, out dummy);
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
		public bool IsObjectVisible(Vector3 vertex, out FrustumPlane culledBy)
		{
			// Make any pending updates to the calculated frustum
			UpdateView();

			// For each plane, see if all points are on the negative side
			// If so, object is not visible
			for (int i = 0; i < 6; i++)
			{
				// to make this loop easier, get the enum type based on the current plane
				FrustumPlane plane = (FrustumPlane)i;

				if (frustum[plane].GetSide(vertex) == PlaneSide.Negative)
				{
					// ALL corners on negative side therefore out of view
					culledBy = plane;
					return false;
				}
			}

			// vertex is not culled
			culledBy = FrustumPlane.None;
			return true;
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
		public void SetAutoTracking(bool enabled, SceneNode target, Vector3 offset)
		{
			if(enabled)
			{
				Debug.Assert(target != null, "A camera's auto track target cannot be null.");
				autoTrackTarget = target;
				autoTrackOffset = offset;
			}
			else
			{
				autoTrackTarget = null;
			}
		}

		#endregion

		#region Internal engine methods

		/// <summary>
		///		Called to ask a camera to render the scene into the given viewport.
		/// </summary>
		/// <param name="viewport"></param>
		/// <param name="showOverlays"></param>
		internal void RenderScene(Viewport viewport, bool showOverlays)
		{
			sceneManager.RenderScene(this, viewport, showOverlays);
		}

		/// <summary>
		///		Updates an auto-tracking camera.
		/// </summary>
		internal void AutoTrack()
		{
			// assumes all scene nodes have been updated
			if(autoTrackTarget != null)
			{
				LookAt(autoTrackTarget.DerivedPosition + autoTrackOffset);
			}
		}

		#endregion

	}
}
