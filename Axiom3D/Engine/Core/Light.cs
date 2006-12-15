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

using DotNet3D.Math;
using DotNet3D.Math.Collections;

#endregion Namespace Declarations
			
namespace Axiom
{
    /// <summary>
    ///    Representation of a dynamic light source in the scene.
    /// </summary>
    /// <remarks>
    ///    Lights are added to the scene like any other object. They contain various
    ///    parameters like type, position, attenuation (how light intensity fades with
    ///    distance), color etc.
    ///    <p/>
    ///    The defaults when a light is created is pure white diffues light, with no
    ///    attenuation (does not decrease with distance) and a range of 1000 world units.
    ///    <p/>
    ///    Lights are created by using the SceneManager.CreateLight method. They can subsequently be
    ///    added to a SceneNode if required to allow them to move relative to a node in the scene. A light attached
    ///    to a SceneNode is assumed to have a base position of (0,0,0) and a direction of (0,0,1) before modification
    ///    by the SceneNode's own orientation. If not attached to a SceneNode,
    ///    the light's position and direction is as set using Position and Direction.
    ///    <p/>
    ///    Remember also that dynamic lights rely on modifying the color of vertices based on the position of
    ///    the light compared to an object's vertex normals. Dynamic lighting will only look good if the
    ///    object being lit has a fair level of tesselation and the normals are properly set. This is particularly
    ///    true for the spotlight which will only look right on highly tesselated models.
    /// </remarks>
    /// <ogre name="OgreLight">
    ///     <file name="OgreLight.h"   revision="1.24.2.1" lastUpdated="6/15/2006" lastUpdatedBy="Skyrapper" />
    ///     <file name="OgreLight.cpp" revision="1.32" lastUpdated="6/15/2005" lastUpdatedBy="Skyrapper" />
    /// </ogre>

    public class Light : MovableObject, IComparable
    {

        #region Constructors/Destructor

        /// <summary>
        ///		Default constructor.
        /// </summary>
        public Light()
        {
            // Default to point light, white diffuse light, linear attenuation, fair range
            _type = LightType.Point;
            _diffuse = ColorEx.White;
            _specular = ColorEx.Black;
            _range = 5000;
            _attenuationConst = 1.0f;
            _attenuationLinear = 0.0f;
            _attenuationQuad = 0.0f;

            // Center in world, direction irrelevant but set anyway
            _position = _derivedPosition = Vector3.Zero;
            _direction = _derivedDirection = Vector3.UnitZ;
            ParentNode = null;

            _localTransformDirty = false;

        }

        /// <summary>
        ///		Normal constructor. Should not be called directly, but rather the SceneManager.CreateLight method should be used.
        /// </summary>
        /// <param name="name"></param>
        public Light( string name )
        {

            base.Name = name;

            // Default to point light, white diffuse light, linear attenuation, fair range
            _type = LightType.Point;
            _diffuse = ColorEx.White;
            _specular = ColorEx.Black;
            _range = 100000;
            _attenuationConst = 1.0f;
            _attenuationLinear = 0.0f;
            _attenuationQuad = 0.0f;

            // Center in world, direction irrelevant but set anyway
            _position = Vector3.Zero;
            _direction = Vector3.UnitZ;

            // Default some spot values
            _spotInner = new Degree(30.0f);
            _spotOuter = new Degree(40.0f);
            _spotFalloff = 1.0f;
            ParentNode = null;

            _localTransformDirty = false;
        }

        #endregion

        #region Properties

        #region DefaultDirection

        private static Vector3 _defaultDirection = new Vector3(0, -1, 0);
        
        protected static Vector3 DefaultDirection
        {
            get { return _defaultDirection; }
            set { _defaultDirection = value; }
        }

        #endregion

        #region Type

        /// <summary>
        ///    Type of light.
        /// </summary>
        private LightType _type;

        /// <summary>
        ///		Gets/Sets the type of light this is.
        /// </summary>
        protected LightType Type
        {
            get
            {
                return _type;
            }
            set
            {
                _type = value;
            }
        }

        #endregion

        #region Position

        /// <summary>
        ///    Position of this light.
        /// </summary>
        private Vector3 _position = Vector3.Zero;

        /// <summary>
        ///		Gets/Sets the position of the light.
        /// </summary>
        protected Vector3 Position
        {
            get
            {
                return _position;
            }
            set
            {
                _position = value;
                _localTransformDirty = true;
            }
        }

        #endregion

        #region Direction

        /// <summary>
        ///    Direction of this light.
        /// </summary>
        private Vector3 _direction = _defaultDirection;

        /// <summary>
        ///		Gets/Sets the direction of the light.
        /// </summary>
        protected Vector3 Direction
        {
            get
            {
                return _direction;
            }
            set
            {
                //defualt to down, as Zero may cause the meshes to be rendered as white
                if ( value.IsZero )
                    value = _defaultDirection;

                _direction = value;
                _direction.Normalize();
                _localTransformDirty = true;
            }
        }

        #endregion

        #region SpotlightInnerAngle

        /// <summary>Inner angle of the spotlight</summary>
        private Radian _spotInner;

        /// <summary>
        ///		Gets the inner angle of the spotlight.
        /// </summary>
        protected Radian SpotlightInnerAngle
        {
            get
            {
                return _spotInner;
            }
        }

        #endregion

        #region SpotlightOuterAngle

        /// <summary>Outer angle of the spotlight</summary>
        private Radian _spotOuter;

        /// <summary>
        ///		Gets the outer angle of the spotlight.
        /// </summary>
        protected Radian SpotlightOuterAngle
        {
            get
            {
                return _spotOuter;
            }
        }

        #endregion

        #region SpotlightFalloff

        /// <summary>Spotlight falloff</summary>
        private Real _spotFalloff;

        /// <summary>
        ///		Gets the spotlight falloff between the inner and the outer cones of the spotlight.
        /// </summary>
        protected Real SpotlightFalloff
        {
            get
            {
                return _spotFalloff;
            }
        }

        #endregion

        #region Diffuse

        /// <summary>
        ///		Diffuse color.
        ///	</summary>
        private ColorEx _diffuse;

        /// <summary>
        ///		Gets/Sets the diffuse color of the light.
        /// </summary>
        protected virtual ColorEx Diffuse
        {
            get
            {
                return _diffuse;
            }
            set
            {
                _diffuse = value;
            }
        }

        #endregion

        #region Specular

        /// <summary>
        ///		Specular color.
        ///	</summary>
        private ColorEx _specular;

        /// <summary>
        ///		Gets/Sets the diffuse color of the light.
        /// </summary>
        protected virtual ColorEx Specular
        {
            get
            {
                return _specular;
            }
            set
            {
                _specular = value;
            }
        }

        #endregion

        #region AttenuationRange

        /// <summary>Attenuation range value</summary>
        private Real _range;

        /// <summary>
        ///		Gets the attenuation range value.
        /// </summary>
        protected Real AttenuationRange
        {
            get
            {
                return _range;
            }
        }

        #endregion

        #region AttenuationConstant

        /// <summary>Constant attenuation value</summary>
        private Real _attenuationConst;

        /// <summary>
        ///		Gets the constant attenuation value.
        /// </summary>
        protected Real AttenuationConstant
        {
            get
            {
                return _attenuationConst;
            }
        }

        #endregion

        #region AttenuationLinear

        /// <summary>Linear attenuation value</summary>
        private Real _attenuationLinear;

        /// <summary>
        ///		Gets the linear attenuation value.
        /// </summary>
        protected Real AttenuationLinear
        {
            get
            {
                return _attenuationLinear;
            }
        }

        #endregion

        #region AttenuationQuadratic

        /// <summary>Quadratic attenuation value</summary>
        private Real _attenuationQuad;

        /// <summary>
        ///		Gets the quadratic attenuation value.
        /// </summary>
        protected Real AttenuationQuadratic
        {
            get
            {
                return _attenuationQuad;
            }
        }

        #endregion

        #region DerivedPosition

        /// <summary>
        ///		Derived position of this light.
        ///	</summary>
        private Vector3 _derivedPosition = Vector3.Zero;

        /// <summary>
        ///		Gets the derived position of this light including any transforms from nodes it is attached to.
        /// </summary>
        protected Vector3 DerivedPosition
        {
            get
            {
                // this is called to force an update
                Update();

                return _derivedPosition;
            }
        }

        #endregion

        #region DerivedDirection

        /// <summary>
        ///		Derived direction of this light.
        ///	</summary>
        private Vector3 _derivedDirection = Vector3.Zero;

        /// <summary>
        ///		Gets the derived position of this light including any transforms from nodes it is attached to.
        /// </summary>
        protected Vector3 DerivedDirection
        {
            get
            {
                // this is called to force an update
                Update();

                return _derivedDirection;
            }
        }

        #endregion

        #region IsVisible

        /// <summary>
        ///		Override IsVisible to ensure we are updated when this changes. Altough lights are not visible themselves, setting a light to invisible means it no longer affects the scene.
        /// </summary>
        public override bool IsVisible
        {
            get
            {
                return base.IsVisible;
            }
            set
            {
                base.IsVisible = value;
            }
        }

        #endregion

        #region BoundingRadius

        /// <summary>
        ///    Local bounding radius of this light.
        /// </summary>
        public override Real BoundingRadius
        {
            get
            {
                // not visible
                return 0;
            }
        }

        #endregion

        #region LastParentOrientation

        /// <summary>
        ///		Stored version of parent orientation.
        ///	</summary>
        private Quaternion _lastParentOrientation = Quaternion.Identity;

        /// <summary>
        ///		Stored version of parent orientation.
        ///	</summary>
        protected Quaternion LastParentOrientation
        {
            get { return _lastParentOrientation; }
            set { _lastParentOrientation = value; }
        }

        #endregion

        #region LastParentPosition

        /// <summary>
        ///		Stored version of parent position.
        ///	</summary>
        private Vector3 _lastParentPosition = Vector3.Zero;

        /// <summary>
        ///		Stored version of parent position.
        ///	</summary>
        protected Quaternion LastParentPosition
        {
            get { return _lastParentPosition; }
            set { _lastParentPosition = value; }
        }

        #endregion

        #region LocalTransformDirty

        /// <summary></summary>
        private bool _localTransformDirty;

        /// <summary>
        ///		Stored version of parent position.
        ///	</summary>
        protected bool LocalTransformDirty
        {
            get { return _localTransformDirty; }
            set { _localTransformDirty = value; }
        }

        #endregion

        #region TempSquaredDist

        /// <summary>
        ///    Used for sorting.
        /// </summary>
        private float _tempSquaredDist;

        /// <summary>
        ///    Used for sorting.  Internal for "friend" access to SceneManager.
        /// </summary>
        internal protected float TempSquaredDist
        {
            get { return _tempSquaredDist; }
            set { _tempSquaredDist = value; }
        }

        #endregion

        #region NearClipVolume

        /// <summary>
        ///		Stored version of the last near clip volume tested.
        /// </summary>
        private PlaneBoundedVolume _nearClipVolume = new PlaneBoundedVolume();

        /// <summary>
        ///		Stored version of the last near clip volume tested.
        /// </summary>
        protected PlaneBoundedVolume NearClipVolume
        {
            get { return _nearClipVolume; }
            set { _nearClipVolume = value; }
        }

        #endregion

        #region FrustumClipVolumes

        /// <summary>
        ///		
        /// </summary>
        private PlaneBoundedVolumeList _frustumClipVolumes = new PlaneBoundedVolumeList();

        /// <summary>
        ///		
        /// </summary>
        protected PlaneBoundedVolumeList FrustumClipVolumes
        {
            get { return _frustumClipVolumes; }
            set { _frustumClipVolumes = value; }
        }

        #endregion

        #endregion

        #region Methods

        /// <summary>
        /// Sets the diffuse color of the light.
        /// </summary>
        protected void SetDiffuse(Real red, Real green, Real blue)
        {
            _diffuse.r = red;
            _diffuse.g = green;
            _diffuse.b = blue;
        }

        /// <summary>
        /// Sets the specular color of the light.
        /// </summary>
        protected void SetSpecular(Real red, Real green, Real blue)
        {
            _specular.r = red;
            _specular.g = green;
            _specular.b = blue;
        }

        /// <summary>
        ///		Updates this lights position.
        /// </summary>
        private void Update()
        {
            Node parentNode = ParentNode;
            if (parentNode != null)
            {
                if (_localTransformDirty
                    || parentNode.DerivedOrientation != _lastParentOrientation
                    || parentNode.DerivedPosition != _lastParentPosition)
                {
                    // we are out of date with the scene node we are attached to
                    _lastParentOrientation = parentNode.DerivedOrientation;
                    _lastParentPosition = parentNode.DerivedPosition;
                    _derivedDirection = _lastParentOrientation * _direction;
                    _derivedPosition = (_lastParentOrientation * _position) + _lastParentPosition;
                }
            }
            else
            {
                _derivedPosition = _position;
                _derivedDirection = _direction;
            }

            _localTransformDirty = false;
        }

        /// <summary>
        ///		Gets the details of this light as a 4D vector.
        /// </summary>
        /// <remarks>
        ///		Getting details of a light as a 4D vector can be useful for
        ///		doing general calculations between different light types; for
        ///		example the vector can represent both position lights (w=1.0f)
        ///		and directional lights (w=0.0f) and be used in the same 
        ///		calculations.
        /// </remarks>
        /// <returns>A 4D vector represenation of the light.</returns>
        public Vector4 GetAs4DVector()
        {
            Vector4 vec;

            if ( _type == LightType.Directional )
            {
                // negate direction as 'position'
                vec = -(Vector4)this.DerivedDirection;

                // infinite distance
                vec.w = 0.0f;
            }
            else
            {
                vec = (Vector4)this.DerivedPosition;
                vec.w = 1.0f;
            }

            return vec;
        }

        /// <overload>
        /// Sets the spotlight parameters in a single call.
        /// </overload>
        /// <param name="innerAngle"></param>
        /// <param name="outerAngle"></param>
        /// <param name="falloff"></param>
        public void SetSpotlightRange(Real innerAngle, Real outerAngle)
        {
            SetSpotlightRange(new Radian(innerAngle), new Radian(outerAngle), 1.0f);
        }

        /// <param name="innerAngle"></param>
        /// <param name="outerAngle"></param>
        /// <param name="falloff"></param>
        public void SetSpotlightRange(Real innerAngle, Real outerAngle, Real falloff)
        {
            SetSpotlightRange(new Radian(innerAngle), new Radian(outerAngle), falloff);
        }

        /// <param name="innerAngle"></param>
        /// <param name="outerAngle"></param>
        /// <param name="falloff"></param>
        public void SetSpotlightRange(Radian innerAngle, Radian outerAngle, Real falloff)
        {
            //allow it to be set ahead of time anyways
            //if (_type != LightType.Spotlight)
            //  throw new AxiomException("SetSpotlightRange is only valid for spotlights");

            _spotInner = innerAngle;
            _spotOuter = outerAngle;
            _spotFalloff = falloff;

        }

        /// <summary>
        ///		Sets the attenuation parameters of the light in a single call.
        /// </summary>
        /// <param name="range"></param>
        /// <param name="constant"></param>
        /// <param name="linear"></param>
        /// <param name="quadratic"></param>
        protected void SetAttenuation(Real range, Real constant, Real linear, Real quadratic)
        {
            _range = range;
            _attenuationConst = constant;
            _attenuationLinear = linear;
            _attenuationQuad = quadratic;
        }

        /// <summary>
        /// Sets the direction of the light.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        protected void SetDirection(Real x, Real y, Real z)
        {
            _direction.x = x;
            _direction.y = y;
            _direction.z = z;
            _direction.Normalize();

            _localTransformDirty = true;
        }

        /// <summary>
        /// Sets the position of the light.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        protected void SetPosition(Real x, Real y, Real z)
        {
            _position.x = x;
            _position.y = y;
            _position.z = z;
            _localTransformDirty = true;
        }

        /// <summary>
        ///		Internal method for calculating the 'near clip volume', which is
        ///		the volume formed between the near clip rectangle of the 
        ///		camera and the light.
        /// </summary>
        /// <remarks>
        ///		This volume is a pyramid for a point/spot light and
        ///		a cuboid for a directional light. It can used to detect whether
        ///		an object could be casting a shadow on the viewport. Note that
        ///		the reference returned is to a shared volume which will be 
        ///		reused across calls to this method.
        /// </remarks>
        /// <param name="camera"></param>
        /// <returns></returns>
        internal PlaneBoundedVolume GetNearClipVolume( Camera camera )
        {
            const float THRESHOLD = -1e-06f;

            // First check if the light is close to the near plane, since
            // in this case we have to build a degenerate clip volume
            _nearClipVolume.planes.Clear();
            _nearClipVolume.outside = Plane.Side.Negative;

            Real n = camera.Near;
            // Homogenous position
            Vector4 lightPos = GetAs4DVector();
            // 3D version (not the same as DerivedPosition, is -direction for
            // directional lights)
            Vector3 lightPos3 = new Vector3( lightPos.x, lightPos.y, lightPos.z );

            // Get eye-space light position
            // use 4D vector so directional lights still work
            Vector4 eyeSpaceLight = camera.ViewMatrix * lightPos;

            // Find distance to light, project onto -Z axis
            Real d = eyeSpaceLight.DotProduct( new Vector4( 0, 0, -1, -n ) );

            if ( d > THRESHOLD || d < -THRESHOLD )
            {
                // light is not too close to the near plane
                // First find the worldspace positions of the corners of the viewport
                Vector3[] corners = camera.WorldSpaceCorners;
                int winding = (d < 0) ^ camera.IsReflected ? +1 : -1;

                // Iterate over world points and form side planes
                Vector3 normal = Vector3.Zero;
                Vector3 lightDir = Vector3.Zero;

                for ( int i = 0; i < 4; i++ )
                {
                    // Figure out light dir
                    lightDir = lightPos3 - ( corners[i] * lightPos.w );
                    // Cross with anticlockwise corner, therefore normal points in
                    // Note: C++ mod returns 3 for the first case (i==0 && winding==-1) where C# returns -1
                    int test = i > 0 || winding==1 ? ((i + winding ) % 4) : 3;

                    normal = ( corners[i] - corners[test] ).CrossProduct( lightDir );
                    normal.Normalize();

                    _nearClipVolume.planes.Add( new Plane( normal, corners[i] ) );
                }

                // Now do the near plane plane
                normal = camera[FrustumPlane.Near].Normal;
                if (d < 0)
                {
                    //Behind near plane
                    normal = -normal;
                }

                //Hack: There bug in Camera.GetDerivedPosition which should be take reflection into account.
                Vector3 cameraPos = camera.DerivedPosition;
                if (camera.IsReflected)
                {
                    //Camera is reflected, used the reflect of derived position as world position
                    cameraPos = camera.ReflectionMatrix * cameraPos;
                }
                _nearClipVolume.planes.Add(new Plane(normal, cameraPos));

                // Finally, for a point/spot light we can add a sixth plane
                // This prevents false positives from behind the light
                if ( _type != LightType.Directional )
                {
                    // Direction from the light perpendicular to near plane
                    _nearClipVolume.planes.Add(new Plane(-normal, lightPos3));
                }
            }
            else
            {
                // light is close to being on the near plane
                // degenerate volume including the entire scene 
                // we will always require light / dark caps
                _nearClipVolume.planes.Add( new Plane( Vector3.UnitZ, -n ) );
                _nearClipVolume.planes.Add( new Plane( -Vector3.UnitZ, n ) );
            }

            return _nearClipVolume;
        }

        /// <summary>
        ///		Internal method for calculating the clip volumes outside of the 
        ///		frustum which can be used to determine which objects are casting
        ///		shadow on the frustum as a whole. 
        /// </summary>
        /// <remarks>
        ///		Each of the volumes is a pyramid for a point/spot light and
        ///		a cuboid for a directional light.
        /// </remarks>
        /// <param name="camera"></param>
        /// <returns></returns>
        internal PlaneBoundedVolumeList GetFrustumClipVolumes( Camera camera )
        {
            // Homogenous light position
            Vector4 lightPos = GetAs4DVector();

            // 3D version (not the same as DerivedPosition, is -direction for
            // directional lights)
            Vector3 lightPos3 = new Vector3( lightPos.x, lightPos.y, lightPos.z );

            Vector3[] clockwiseVerts = new Vector3[4];

            // Get worldspace frustum corners
            Vector3[] corners = camera.WorldSpaceCorners;
            int winding = camera.IsReflected ? +1 : -1;

            bool infiniteViewDistance = ( camera.Far == 0 );

            _frustumClipVolumes.Clear();

            for ( int n = 0; n < 6; n++ )
            {
                FrustumPlane frustumPlane = (FrustumPlane)n;

                // skip far plane if infinite view frustum
                if ( infiniteViewDistance && ( frustumPlane == FrustumPlane.Far ) )
                {
                    continue;
                }

                Plane plane = camera[frustumPlane];

                Vector4 planeVec = new Vector4( plane.Normal.x, plane.Normal.y, plane.Normal.z, plane.Distance );

                // planes face inwards, we need to know if light is on negative side
                Real d = planeVec.DotProduct( lightPos );

                if ( d < -1e-06f )
                {
                    // Ok, this is a valid one
                    // clockwise verts mean we can cross-product and always get normals
                    // facing into the volume we create
                    PlaneBoundedVolume vol = new PlaneBoundedVolume();
                    _frustumClipVolumes.Add(vol);

                    switch ( frustumPlane )
                    {
                        case ( FrustumPlane.Near ):
                            clockwiseVerts[0] = corners[3];
                            clockwiseVerts[1] = corners[2];
                            clockwiseVerts[2] = corners[1];
                            clockwiseVerts[3] = corners[0];
                            break;
                        case ( FrustumPlane.Far ):
                            clockwiseVerts[0] = corners[7];
                            clockwiseVerts[1] = corners[6];
                            clockwiseVerts[2] = corners[5];
                            clockwiseVerts[3] = corners[4];
                            break;
                        case ( FrustumPlane.Left ):
                            clockwiseVerts[0] = corners[2];
                            clockwiseVerts[1] = corners[6];
                            clockwiseVerts[2] = corners[5];
                            clockwiseVerts[3] = corners[1];
                            break;
                        case ( FrustumPlane.Right ):
                            clockwiseVerts[0] = corners[7];
                            clockwiseVerts[1] = corners[3];
                            clockwiseVerts[2] = corners[0];
                            clockwiseVerts[3] = corners[4];
                            break;
                        case ( FrustumPlane.Top ):
                            clockwiseVerts[0] = corners[0];
                            clockwiseVerts[1] = corners[1];
                            clockwiseVerts[2] = corners[5];
                            clockwiseVerts[3] = corners[4];
                            break;
                        case ( FrustumPlane.Bottom ):
                            clockwiseVerts[0] = corners[7];
                            clockwiseVerts[1] = corners[6];
                            clockwiseVerts[2] = corners[2];
                            clockwiseVerts[3] = corners[3];
                            break;
                    }

                    // Build a volume
                    // Iterate over world points and form side planes
                    Vector3 normal;
                    Vector3 lightDir;

                    for ( int i = 0; i < 4; i++ )
                    {
                        // Figure out light dir
                        lightDir = lightPos3 - ( clockwiseVerts[i] * lightPos.w );

                        // Cross with anticlockwise corner, therefore normal points in
                        // Note: C++ mod returns 3 for the first case (i==0 && winding==-1) where C# returns -1
                        int test = i > 0 || winding == 1 ? ((i + winding) % 4) : 3;

                        // Cross with anticlockwise corner, therefore normal points in
                        Vector3 edgeDir = ( clockwiseVerts[i] - clockwiseVerts[test] );
                        normal=edgeDir.CrossProduct( lightDir );
                        normal.Normalize();

                        // NB last param to Plane constructor is negated because it's -d
                        vol.planes.Add( new Plane( normal, clockwiseVerts[i] ) );
                    }

                    // Now do the near plane (this is the plane of the side we're 
                    // talking about, with the normal inverted (d is already interpreted as -ve)
                    vol.planes.Add( new Plane( -plane.Normal, plane.Distance ) );

                    // Finally, for a point/spot light we can add a sixth plane
                    // This prevents false positives from behind the light
                    if ( _type != LightType.Directional )
                    {
                        // re-use our own plane normal
                        // remember the -d negation in plane constructor
                        vol.planes.Add( new Plane( plane.Normal, lightPos3 ) );
                    }
                }
            }

            return _frustumClipVolumes;
        }

        #endregion

        #region MovableObject Implementation

        public override void NotifyCurrentCamera( Camera camera )
        {
            // Do nothing
        }

        public override void UpdateRenderQueue( RenderQueue queue )
        {
            // Do Nothing	
        }

        /// <summary>
        /// 
        /// </summary>
        public override AxisAlignedBox BoundingBox
        {
            get
            {
                return AxisAlignedBox.Null;
            }
        }

        #endregion MovableObject Implementation

        #region IComparable Members

        /// <summary>
        ///    Used to compare this light to another light for sorting.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public int CompareTo( object obj )
        {
            Light other = obj as Light;

            if ( other._tempSquaredDist > this._tempSquaredDist )
            {
                return 1;
            }
            else
            {
                return -1;
            }
        }

        #endregion
    }
}
