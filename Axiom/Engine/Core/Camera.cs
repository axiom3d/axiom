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
using Axiom.Graphics;

namespace Axiom.Core {
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
    public class Camera : Frustum {
        #region Fields

        protected SceneManager sceneManager;
        protected Quaternion orientation;
        protected Vector3 position;
        protected Quaternion derivedOrientation;
        protected Vector3 derivedPosition;				
        protected bool isYawFixed;
        protected Vector3 yawFixedAxis;
        protected Projection projectionType;
        protected SceneDetailLevel sceneDetail;
        protected int numFacesRenderedLastFrame;
        protected SceneNode autoTrackTarget;
        protected Vector3 autoTrackOffset;	
        protected float sceneLodFactor;
        protected float invSceneLodFactor;
        protected bool isReflected;
        protected Matrix4 reflectionMatrix;
        protected Plane reflectionPlane;

        #endregion Fields

        #region Constructors

        public Camera(string name, SceneManager sceneManager) {
            // Init camera location & direction

            // Locate at (0,0,0)
            position = Vector3.Zero;
            derivedPosition = Vector3.Zero;

            // Point down -Z axis
            orientation = Quaternion.Identity;
            derivedOrientation = Quaternion.Identity;

            fieldOfView = MathUtil.RadiansToDegrees(MathUtil.PI / 4.0f);
            nearDistance = 100.0f;
            farDistance = 100000.0f;
            aspectRatio = 1.33333333333333f;

            viewMatrix = Matrix4.Zero;
            projectionMatrix = Matrix4.Zero;

            // Reasonable defaults to camera params
            projectionType = Projection.Perspective;
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

        #region Protected methods

        /// <summary>
        ///		Updates the frustum data.
        /// </summary>
        protected override void UpdateFrustum() {
            if(recalculateFrustum) {
                switch(projectionType) {
                    case Projection.Perspective: {

                        // PERSPECTIVE transform, API specific
                        projectionMatrix = Root.Instance.RenderSystem.MakeProjectionMatrix(fieldOfView, aspectRatio, nearDistance, farDistance);

                        // PERSPECTIVE transform, API specific for GPU programs
                        standardProjMatrix = Root.Instance.RenderSystem.MakeProjectionMatrix(fieldOfView, aspectRatio, nearDistance, farDistance, true);

                        float thetaY = MathUtil.DegreesToRadians(fieldOfView * 0.5f);
                        float tanThetaY = MathUtil.Tan(thetaY);

                        // Calculate co-efficients for the frustum planes
                        // Special-cased for L = -R and B = -T i.e. viewport centered 
                        // on direction vector.
                        // Taken from ideas in WildMagic 0.2 http://www.magic-software.com
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
                    case Projection.Orthographic: {
						// ORTHOGRAPHIC projection, API specific 
						projectionMatrix = Root.Instance.RenderSystem.MakeOrthoMatrix(
							fieldOfView, aspectRatio, nearDistance, farDistance);

						float thetaY = MathUtil.DegreesToRadians(fieldOfView * 0.5f);
						float sinThetaY = MathUtil.Sin(thetaY);
						float thetaX = thetaY * aspectRatio;
						float sinThetaX = MathUtil.Sin(thetaX);
						// Calculate co-efficients for the frustum planes
						// Special-cased for L = -R and B = -T i.e. viewport centered 
						// on direction vector.
						// Taken from ideas in WildMagic 0.2 http://www.magic-software.com
						float vpTop = sinThetaY * nearDistance;
						float vpRight = sinThetaX * nearDistance;
						float vpBottom = -vpTop;
						float vpLeft = -vpRight;

						float fNSqr = nearDistance * nearDistance;
						float fLSqr = vpRight * vpRight;
						float fRSqr = fLSqr;
						float fTSqr = vpTop * vpTop;
						float fBSqr = fTSqr;

						float invLength = 1.0f / MathUtil.Sqrt( fNSqr + fLSqr );
						coeffL[0] = nearDistance * invLength;
						coeffL[1] = -vpLeft * invLength;

						invLength = 1.0f / MathUtil.Sqrt( fNSqr + fRSqr );
						coeffR[0] = -nearDistance * invLength;
						coeffR[1] = vpRight * invLength;

						invLength = 1.0f / MathUtil.Sqrt( fNSqr + fBSqr );
						coeffB[0] = nearDistance * invLength;
						coeffB[1] = -vpBottom * invLength;

						invLength = 1.0f / MathUtil.Sqrt( fNSqr + fTSqr );
						coeffT[0] = -nearDistance * invLength;
						coeffT[1] = vpTop * invLength;

                    } break;
                }

                recalculateFrustum = false;
            }
        }

        /// <summary>
        ///		Updates the view matrix.
        /// </summary>
        protected override void UpdateView() {
            // check if the view is out of date
            if(!this.IsViewOutOfDate) {
                return;
            }

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

            // initialize the upper 3x3 portion with the rotation
            viewMatrix = rotationT;

            // add the translation portion, add set 1 for the bottom right portion
            viewMatrix.m03 = translation.x;
            viewMatrix.m13 = translation.y;
            viewMatrix.m23 = translation.z;

            // deal with reflections
            if(isReflected) {
                viewMatrix = viewMatrix * reflectionMatrix;
            }

            // update the frustum planes
            UpdateFrustum();

            // Use camera view for frustum calcs, using -Z rather than Z
            Vector3 camDirection = derivedOrientation * -Vector3.UnitZ;

            // calculate distance along direction to our derived position
            float distance = camDirection.Dot(derivedPosition);

            // left plane
            this[FrustumPlane.Left].Normal = coeffL[0] * left + coeffL[1] * camDirection;
            this[FrustumPlane.Left].D = -derivedPosition.Dot(this[FrustumPlane.Left].Normal);

            // right plane
            this[FrustumPlane.Right].Normal = coeffR[0] * left + coeffR[1] * camDirection;
            this[FrustumPlane.Right].D = -derivedPosition.Dot(this[FrustumPlane.Right].Normal);

            // bottom plane
            this[FrustumPlane.Bottom].Normal = coeffB[0] * up + coeffB[1] * camDirection;
            this[FrustumPlane.Bottom].D = -derivedPosition.Dot(this[FrustumPlane.Bottom].Normal);

            // top plane
            this[FrustumPlane.Top].Normal = coeffT[0] * up + coeffT[1] * camDirection;
            this[FrustumPlane.Top].D = -derivedPosition.Dot(this[FrustumPlane.Top].Normal);

            // far plane
            this[FrustumPlane.Far].Normal = -camDirection;
            this[FrustumPlane.Far].D = distance + farDistance;

            // near plane
            this[FrustumPlane.Near].Normal = camDirection;
            this[FrustumPlane.Near].D = -(distance + nearDistance);

			// Update worldspace corners
			Matrix4 eyeToWorld = viewMatrix.Inverse();

			// Get worldspace frustum corners
			float y = MathUtil.Tan(fieldOfView * 0.5f);
			float x = aspectRatio * y;
			float neary = y * nearDistance;
			float fary = y * farDistance;
			float nearx = x * nearDistance;
			float farx = x * farDistance;

			// near
			worldSpaceCorners[0] = eyeToWorld * new Vector3(nearx, neary, -nearDistance);
			worldSpaceCorners[1] = eyeToWorld * new Vector3(-nearx,  neary, -nearDistance);
			worldSpaceCorners[2] = eyeToWorld * new Vector3(-nearx, -neary, -nearDistance);
			worldSpaceCorners[3] = eyeToWorld * new Vector3(nearx, -neary, -nearDistance);
			// far
			worldSpaceCorners[4] = eyeToWorld * new Vector3(farx, fary, -farDistance);
			worldSpaceCorners[5] = eyeToWorld * new Vector3(-farx, fary, -farDistance);
			worldSpaceCorners[6] = eyeToWorld * new Vector3(-farx, -fary, -farDistance);
			worldSpaceCorners[7] = eyeToWorld * new Vector3(farx, -fary, -farDistance);

            // Deal with reflection on frustum planes
            if (isReflected) {
                Vector3 pos = reflectionMatrix * derivedPosition;
                Vector3 dir = camDirection.Reflect(reflectionPlane.Normal);
                distance = dir.Dot(pos);
                for(int i = 0; i < 6; i++) {
                    planes[i].Normal = planes[i].Normal.Reflect(reflectionPlane.Normal);
                    // Near / far plane dealt with differently since they don't pass through camera
                    switch((FrustumPlane)i) {
                    case FrustumPlane.Near:
                        planes[i].D = -(distance + nearDistance);
                        break;
                    case FrustumPlane.Far:
                        planes[i].D = distance + farDistance;
                        break;
                    default:
                        planes[i].D = -pos.Dot(planes[i].Normal);
                        break;
                    }
                }

				// Also reflect corners
				for (int i = 0; i < 8; i++) {
					worldSpaceCorners[i] = reflectionMatrix * worldSpaceCorners[i];
				}
            }

            // update since we have now recalculated everything
            recalculateView = false;
        }

        #endregion

        #region SceneObject Implementation

        public override void UpdateRenderQueue(RenderQueue queue) {
            // Do nothing
        }

        public override AxisAlignedBox BoundingBox {
            get {
                // a camera is not visible in the scene
                return AxisAlignedBox.Null;
            }
        }

		/// <summary>
		///		Overridden to return a proper bounding radius for the camera.
		/// </summary>
		public override float BoundingRadius {
			get {
				// return a little bigger than the near distance
				// just to keep things just outside
				return nearDistance * 1.5f;
			}
		}

        public override void NotifyCurrentCamera(Axiom.Core.Camera pCamera) {
            // Do nothing
        }

        /// <summary>
        ///    Called by the scene manager to let this camera know how many faces were renderer within
        ///    view of this camera every frame.
        /// </summary>
        /// <param name="numRenderedFaces"></param>
        internal void NotifyRenderedFaces(int numRenderedFaces) {
            numFacesRenderedLastFrame = numRenderedFaces;
        }

        #endregion

        #region Public Properties
	
        /// <summary>
        ///    Returns the current SceneManager that this camera is using.
        /// </summary>
        public SceneManager SceneManager {
            get { 
                return sceneManager; 
            }
        }

        /// <summary>
        ///    Gets/Sets the type of projection to use (orthographic or perspective). Default is perspective.
        /// </summary>
        public Projection ProjectionType {
            get { 
                return projectionType; 
            }
            set { 
                projectionType = value;	
                InvalidateFrustum();
            }
        }

        /// <summary>
        ///     Returns the reflection matrix of the camera if appropriate.
        /// </summary>
        public Matrix4 ReflectionMatrix {
            get {
                return reflectionMatrix;
            }
        }

        /// <summary>
        ///     Returns the reflection plane of the camera if appropriate.
        /// </summary>
        public Plane ReflectionPlane {
            get {
                return reflectionPlane;
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
        public SceneDetailLevel SceneDetail {
            get { 
                return sceneDetail; 
            }
            set { 
                sceneDetail = value; 
            }
        }

        /// <summary>
        ///     Gets/Sets the camera orientation.
        /// </summary>
        public Quaternion Orientation {
            get {
                return orientation;
            }
            set {
                orientation = value;
                InvalidateView();
            }
        }

        /// <summary>
        ///     Gets/Sets the cameras position.
        /// </summary>
        public Vector3 Position {
            get { 
                return position; 
            }
            set { 
                position = value;	
                InvalidateView();
            }
        }

        /// <summary>
        /// Gets/Sets the cameras direction vector.
        /// </summary>
        public Vector3 Direction {
            // Direction points down the negatize Z axis by default.
            get { 
                return orientation * -Vector3.UnitZ; 
            }
            set {
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

                if( isYawFixed ) {
                    Vector3 xVector = yawFixedAxis.Cross( zAdjustVector );
                    xVector.Normalize();

                    Vector3 yVector = zAdjustVector.Cross( xVector );
                    yVector.Normalize();

                    orientation.FromAxes( xVector, yVector, zAdjustVector );
                }
                else {
                    // update the view of the camera
                    UpdateView();

                    // Get axes from current quaternion
                    Vector3 xAxis, yAxis, zAxis;

                    // get the vector components of the derived orientation vector
                    this.DerivedOrientation.ToAxes(out xAxis, out yAxis, out zAxis);

                    Quaternion rotationQuat;

                    if (-zAdjustVector == zAxis) {
                        // Oops, a 180 degree turn (infinite possible rotation axes)
                        // Default to yaw i.e. use current UP
                        rotationQuat = Quaternion.FromAngleAxis(MathUtil.PI, yAxis);
                    }
                    else {
                        // Derive shortest arc to new direction
                        rotationQuat = zAxis.GetRotationTo(zAdjustVector);
                    }

                    orientation = rotationQuat * orientation;
                }

                // TODO: If we have a fixed yaw axis, we musn't break it by using the
                // shortest arc because this will sometimes cause a relative yaw
                // which will tip the camera

                InvalidateView();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public Vector3 FixedYawAxis {
            get { 
                return yawFixedAxis; 
            }
            set { 
                yawFixedAxis = value; 

                if(yawFixedAxis != Vector3.Zero) {
                    isYawFixed = true;
                }
                else {
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
        public float LodBias {
            get {
                return sceneLodFactor;
            }
            set {
                Debug.Assert(value > 0.0f, "Lod bias must be greater than 0");
                sceneLodFactor = value;
                invSceneLodFactor = 1.0f / sceneLodFactor;
            }
        }

        /// <summary>
        ///     Gets a flag that specifies whether this camera is being reflected or not.
        /// </summary>
        public bool IsReflected {
            get {
                return isReflected;
            }
        }
        
        /// <summary>
        ///     Used for internal Lod calculations.
        /// </summary>
        public float InverseLodBias {
            get {
                return invSceneLodFactor;
            }
        }

        /// <summary>
        /// Gets the last count of triangles visible in the view of this camera.
        /// </summary>
        public int NumRenderedFaces {
            get { 
                return numFacesRenderedLastFrame; 
            }
        }

        /// <summary>
        ///		Gets the derived orientation of the camera.
        /// </summary>
        public Quaternion DerivedOrientation {
            get { 
                UpdateView();
                return derivedOrientation;
            }
        }

        /// <summary>
        ///		Gets the derived position of the camera.
        /// </summary>
        public Vector3 DerivedPosition {
            get { 
                UpdateView();
                return derivedPosition;
            }
        }

        /// <summary>
        ///		Gets the derived direction of the camera.
        /// </summary>
        public Vector3 DerivedDirection {
            get { 
                UpdateView();

                // RH coords, direction points down -Z by default
                return derivedOrientation * -Vector3.UnitZ;
            }
        }

        /// <summary>
        ///		Evaluates whether or not the view matrix is out of date.
        /// </summary>
        /// <returns></returns>
        protected override bool IsViewOutOfDate {
            get {
                // Overridden from Frustum to use local orientation / position offsets
                // are we attached to another node?
                if(parentNode != null) {
                    if(!recalculateView && parentNode.DerivedOrientation == lastParentOrientation &&
                        parentNode.DerivedPosition == lastParentPosition) {
                        return false;
                    }
                    else {
                        // we are out of date with the parent scene node
                        lastParentOrientation = parentNode.DerivedOrientation;
                        lastParentPosition = parentNode.DerivedPosition;
                        derivedOrientation = lastParentOrientation * orientation;
                        derivedPosition = (lastParentOrientation * position) + lastParentPosition;
                        return true;
                    }
                }
                else {
                    // rely on own updates
                    derivedOrientation = orientation;
                    derivedPosition = position;
                    return recalculateView;
                }
            }
        }

        #endregion

        #region Public methods

        /// <summary>
        ///     Disables reflection modification previously turned on with EnableReflection.
        /// </summary>
        public void DisableReflection() {
            isReflected = false;
            InvalidateView();
        }

        /// <summary>
        ///     Modifies this camera so it always renders from the reflection of itself through the
        ///     plane specified.
        /// </summary>
        /// <remarks>
        ///     This is obviously useful for rendering planar reflections.
        /// </remarks>
        /// <param name="plane"></param>
        public void EnableReflection(Plane plane) {
            isReflected = true;
            reflectionPlane = plane;
            reflectionMatrix = MathUtil.BuildReflectionMatrix(plane);
            InvalidateView();
        }

        /// <summary>
        /// Moves the camera's position by the vector offset provided along world axes.
        /// </summary>
        /// <param name="offset"></param>
        public void Move(Vector3 offset) {
            position = position + offset;
            InvalidateView();
        }

        /// <summary>
        /// Moves the camera's position by the vector offset provided along it's own axes (relative to orientation).
        /// </summary>
        /// <param name="offset"></param>
        public void MoveRelative(Vector3 offset) {
            // Transform the axes of the relative vector by camera's local axes
            Vector3 transform = orientation * offset;

            position = position + transform;
            InvalidateView();
        }

        /// <summary>
        ///		Specifies a target that the camera should look at.
        /// </summary>
        /// <param name="target"></param>
        public void LookAt(Vector3 target) {
            UpdateView();
            this.Direction = (target - derivedPosition);
        }

        /// <summary>
        ///		Pitches the camera up/down counter-clockwise around it's local x axis.
        /// </summary>
        /// <param name="degrees"></param>
        public void Pitch(float degrees) {
            Vector3 xAxis = orientation * Vector3.UnitX;
            Rotate(xAxis, degrees);

            InvalidateView();
        }

        /// <summary>
        ///		Rolls the camera counter-clockwise, in degrees, around its local y axis.
        /// </summary>
        /// <param name="degrees"></param>
        public void Yaw(float degrees) {
            Vector3 yAxis;

            if(isYawFixed) {
                // Rotate around fixed yaw axis
                yAxis = yawFixedAxis;
            }
            else {
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
        public void Roll(float degrees) {
            // Rotate around local Z axis
            Vector3 zAxis = orientation * Vector3.UnitZ;
            Rotate(zAxis, degrees);

            InvalidateView();
        }

        /// <summary>
        ///		Rotates the camera about an arbitrary axis.
        /// </summary>
        /// <param name="quat"></param>
        public void Rotate(Quaternion quat) {
            // Note the order of the multiplication
            orientation = quat * orientation;

            InvalidateView();
        }

        /// <summary>
        ///		Rotates the camera about an arbitrary axis.
        /// </summary>
        /// <param name="axis"></param>
        /// <param name="degrees"></param>
        public void Rotate(Vector3 axis, float degrees) {
            Quaternion q = Quaternion.FromAngleAxis(MathUtil.DegreesToRadians(degrees), axis);
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
        public void SetAutoTracking(bool enabled, SceneObject target) {
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
        public void SetAutoTracking(bool enabled, SceneNode target) {
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
        public void SetAutoTracking(bool enabled, SceneNode target, Vector3 offset) {
            if(enabled) {
                Debug.Assert(target != null, "A camera's auto track target cannot be null.");
                autoTrackTarget = target;
                autoTrackOffset = offset;
            }
            else {
                autoTrackTarget = null;
            }
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
        public Ray GetCameraToViewportRay(float screenX, float screenY) {
            float centeredScreenX = (screenX - 0.5f);
            float centeredScreenY = (0.5f - screenY);
     
            float normalizedSlope = MathUtil.Tan(MathUtil.DegreesToRadians(fieldOfView * 0.5f));
            float viewportYToWorldY = normalizedSlope * nearDistance * 2;
            float viewportXToWorldX = viewportYToWorldY * aspectRatio;
     
            Vector3 rayDirection = 
                new Vector3(
                    centeredScreenX * viewportXToWorldX, 
                    centeredScreenY * viewportYToWorldY,
                    -nearDistance);

            rayDirection = this.DerivedOrientation * rayDirection;
            rayDirection.Normalize();
     
            return new Ray(this.DerivedPosition, rayDirection);
        }

        #endregion

        #region Internal engine methods

        /// <summary>
        ///		Called to ask a camera to render the scene into the given viewport.
        /// </summary>
        /// <param name="viewport"></param>
        /// <param name="showOverlays"></param>
        public void RenderScene(Viewport viewport, bool showOverlays) {
            sceneManager.RenderScene(this, viewport, showOverlays);
        }

        /// <summary>
        ///		Updates an auto-tracking camera.
        /// </summary>
        internal void AutoTrack() {
            // assumes all scene nodes have been updated
            if(autoTrackTarget != null) {
                LookAt(autoTrackTarget.DerivedPosition + autoTrackOffset);
            }
        }

        #endregion
    }
}
