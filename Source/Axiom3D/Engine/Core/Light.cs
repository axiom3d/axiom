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



using Axiom.Math;
using Axiom.Math.Collections;

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
    public class Light : MovableObject, IComparable
    {
        #region Fields
        public static Vector3 DefaultDirection = new Vector3( 0, -1, 0 );

        /// <summary>
        ///    Type of light.
        /// </summary>
        protected LightType type;
        /// <summary>
        ///    Position of this light.
        /// </summary>
        protected Vector3 position = Vector3.Zero;
        /// <summary>
        ///    Direction of this light.
        /// </summary>
        protected Vector3 direction = DefaultDirection;
        /// <summary>
        ///		Derived position of this light.
        ///	</summary>
        protected Vector3 derivedPosition = Vector3.Zero;
        /// <summary>
        ///		Derived direction of this light.
        ///	</summary>
        protected Vector3 derivedDirection = Vector3.Zero;
        /// <summary>
        ///		Stored version of parent orientation.
        ///	</summary>
        protected Quaternion lastParentOrientation = Quaternion.Identity;
        /// <summary>
        ///		Stored version of parent position.
        ///	</summary>
        protected Vector3 lastParentPosition = Vector3.Zero;
        /// <summary>
        ///		Diffuse color.
        ///	</summary>
        protected ColorEx diffuse;
        /// <summary>
        ///		Specular color.
        ///	</summary>
        protected ColorEx specular;
        /// <summary></summary>
        protected float spotOuter;
        /// <summary></summary>
        protected float spotInner;
        /// <summary></summary>
        protected float spotFalloff;
        /// <summary></summary>
        protected float range;
        /// <summary></summary>
        protected float attenuationConst;
        /// <summary></summary>
        protected float attenuationLinear;
        /// <summary></summary>
        protected float attenuationQuad;
        /// <summary></summary>
        protected bool localTransformDirty;
        /// <summary>
        ///    Used for sorting.  Internal for "friend" access to SceneManager.
        /// </summary>
        internal protected float tempSquaredDist;
        /// <summary>
        ///		Stored version of the last near clip volume tested.
        /// </summary>
        protected PlaneBoundedVolume nearClipVolume = new PlaneBoundedVolume();
        /// <summary>
        ///		
        /// </summary>
        protected PlaneBoundedVolumeList frustumClipVolumes = new PlaneBoundedVolumeList();

        #endregion

        #region Constructors

        /// <summary>
        ///		Default constructor.
        /// </summary>
        public Light()
            : this( "" )
        {
        }

        /// <summary>
        ///		Normal constructor. Should not be called directly, but rather the SceneManager.CreateLight method should be used.
        /// </summary>
        /// <param name="name"></param>
        public Light( string name )
        {

            this.name = name;

            // Default to point light, white diffuse light, linear attenuation, fair range
            type = LightType.Point;
            diffuse = ColorEx.White;
            specular = ColorEx.Black;
            range = 100000;
            attenuationConst = 1.0f;
            attenuationLinear = 0.0f;
            attenuationQuad = 0.0f;

            // Center in world, direction irrelevant but set anyway
            position = Vector3.Zero;
            direction = Vector3.UnitZ;

            // Default some spot values
            spotInner = 30.0f;
            spotOuter = 40.0f;
            spotFalloff = 1.0f;

            localTransformDirty = false;
        }

        #endregion

        #region Properties

        /// <summary>
        ///		Gets/Sets the type of light this is.
        /// </summary>
        public LightType Type
        {
            get
            {
                return type;
            }
            set
            {
                type = value;
            }
        }

        /// <summary>
        ///		Gets/Sets the position of the light.
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
                localTransformDirty = true;
            }
        }

        /// <summary>
        ///		Gets/Sets the direction of the light.
        /// </summary>
        public Vector3 Direction
        {
            get
            {
                return direction;
            }
            set
            {
                //defualt to down, as Zero may cause the meshes to be rendered as white
                if ( value.IsZero )
                    value = DefaultDirection;

                direction = value;
                direction.Normalize();
                localTransformDirty = true;
            }
        }

        /// <summary>
        ///		Gets the inner angle of the spotlight.
        /// </summary>
        public float SpotlightInnerAngle
        {
            get
            {
                return spotInner;
            }
        }

        /// <summary>
        ///		Gets the outer angle of the spotlight.
        /// </summary>
        public float SpotlightOuterAngle
        {
            get
            {
                return spotOuter;
            }
        }

        /// <summary>
        ///		Gets the spotlight falloff.
        /// </summary>
        public float SpotlightFalloff
        {
            get
            {
                return spotFalloff;
            }
        }

        /// <summary>
        ///		Gets/Sets the diffuse color of the light.
        /// </summary>
        public virtual ColorEx Diffuse
        {
            get
            {
                return diffuse;
            }
            set
            {
                diffuse = value;
            }
        }

        /// <summary>
        ///		Gets/Sets the diffuse color of the light.
        /// </summary>
        public virtual ColorEx Specular
        {
            get
            {
                return specular;
            }
            set
            {
                specular = value;
            }
        }

        /// <summary>
        ///		Gets the attenuation range value.
        /// </summary>
        public float AttenuationRange
        {
            get
            {
                return range;
            }
        }

        /// <summary>
        ///		Gets the constant attenuation value.
        /// </summary>
        public float AttenuationConstant
        {
            get
            {
                return attenuationConst;
            }
        }

        /// <summary>
        ///		Gets the linear attenuation value.
        /// </summary>
        public float AttenuationLinear
        {
            get
            {
                return attenuationLinear;
            }
        }

        /// <summary>
        ///		Gets the quadratic attenuation value.
        /// </summary>
        public float AttenuationQuadratic
        {
            get
            {
                return attenuationQuad;
            }
        }

        /// <summary>
        ///		Updates this lights position.
        /// </summary>
        public virtual void Update()
        {
            if ( parentNode != null )
            {
                if ( !localTransformDirty
                    && parentNode.DerivedOrientation == lastParentOrientation
                    && parentNode.DerivedPosition == lastParentPosition )
                {
                }
                else
                {
                    // we are out of date with the scene node we are attached to
                    lastParentOrientation = parentNode.DerivedOrientation;
                    lastParentPosition = parentNode.DerivedPosition;
                    derivedDirection = lastParentOrientation * direction;
                    derivedPosition = ( lastParentOrientation * position ) + lastParentPosition;
                }
            }
            else
            {
                derivedPosition = position;
                derivedDirection = direction;
            }

            localTransformDirty = false;
        }

        /// <summary>
        ///		Gets the derived position of this light.
        /// </summary>
        public Vector3 DerivedPosition
        {
            get
            {
                // this is called to force an update
                Update();

                return derivedPosition;
            }
        }

        /// <summary>
        ///		Gets the derived position of this light.
        /// </summary>
        public Vector3 DerivedDirection
        {
            get
            {
                // this is called to force an update
                Update();

                return derivedDirection;
            }
        }

        /// <summary>
        ///		Override IsVisible to ensure we are updated when this changes.
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

        /// <summary>
        ///    Local bounding radius of this light.
        /// </summary>
        public override float BoundingRadius
        {
            get
            {
                // not visible
                return 0;
            }
        }

        #endregion

        #region Methods

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

            if ( type == LightType.Directional )
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="innerAngle"></param>
        /// <param name="outerAngle"></param>
        public void SetSpotlightRange( float innerAngle, float outerAngle )
        {
            SetSpotlightRange( innerAngle, outerAngle, 1.0f );
        }

        /// <summary>
        ///		Sets the spotlight parameters in a single call.
        /// </summary>
        /// <param name="innerAngle"></param>
        /// <param name="outerAngle"></param>
        /// <param name="falloff"></param>
        public void SetSpotlightRange( float innerAngle, float outerAngle, float falloff )
        {
            //allow it to be set ahead of time anyways
            /*if(type != LightType.Spotlight) {
				throw new Exception("Setting the spotlight range is only valid for spotlights.");
			}*/


            spotInner = innerAngle;
            spotOuter = outerAngle;
            spotFalloff = falloff;
        }

        /// <summary>
        ///		Sets the attenuation parameters of the light in a single call.
        /// </summary>
        /// <param name="range"></param>
        /// <param name="constant"></param>
        /// <param name="linear"></param>
        /// <param name="quadratic"></param>
        public void SetAttenuation( float range, float constant, float linear, float quadratic )
        {
            this.range = range;
            attenuationConst = constant;
            attenuationLinear = linear;
            attenuationQuad = quadratic;
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

            float n = camera.Near;

            // First check if the light is close to the near plane, since
            // in this case we have to build a degenerate clip volume
            nearClipVolume.planes.Clear();
            nearClipVolume.outside = Plane.Side.Negative;

            // Homogenous position
            Vector4 lightPos = GetAs4DVector();
            // 3D version (not the same as DerivedPosition, is -direction for
            // directional lights)
            Vector3 lightPos3 = new Vector3( lightPos.x, lightPos.y, lightPos.z );

                        // Get eye-space light position
                        // use 4D vector so directional lights still work
            Vector4 eyeSpaceLight = camera.ViewMatrix * lightPos;
            Matrix4 eyeToWorld = camera.ViewMatrix.Inverse();

            // Find distance to light, project onto -Z axis
            float d = eyeSpaceLight.DotProduct( new Vector4( 0, 0, -1, -n ) );

            if ( d > THRESHOLD || d < -THRESHOLD )
            {
                // light is not too close to the near plane
                // First find the worldspace positions of the corners of the viewport
                Vector3[] corners = camera.WorldSpaceCorners;

                // Iterate over world points and form side planes
                Vector3 normal = Vector3.Zero;
                Vector3 lightDir = Vector3.Zero;

                for ( int i = 0; i < 4; i++ )
                {
                    // Figure out light dir
                    lightDir = lightPos3 - ( corners[i] * lightPos.w );
                    // Cross with anticlockwise corner, therefore normal points in
                    // Note: C++ mod returns 3 for the first case where C# returns -1
                    int test = i > 0 ? ( ( i - 1 ) % 4 ) : 3;

                    normal = ( corners[i] - corners[test] ).CrossProduct( lightDir );
                    normal.Normalize();

                    if ( d < THRESHOLD )
                    {
                        // invert normal
                        normal = -normal;
                    }
                    // NB last param to Plane constructor is negated because it's -d
                    nearClipVolume.planes.Add( new Plane( normal, normal.DotProduct( corners[i] ) ) );
                }

                // Now do the near plane plane
                if ( d > THRESHOLD )
                {
                    // In front of near plane
                    // remember the -d negation in plane constructor
                    normal = eyeToWorld * -Vector3.UnitZ;
                    normal.Normalize();
                    nearClipVolume.planes.Add( new Plane( normal, -normal.DotProduct( camera.DerivedPosition ) ) );
                }
                else
                {
                    // Behind near plane
                    // remember the -d negation in plane constructor
                    normal = eyeToWorld * Vector3.UnitZ;
                    normal.Normalize();
                    nearClipVolume.planes.Add( new Plane( normal, -normal.DotProduct( camera.DerivedPosition ) ) );
                }

                // Finally, for a point/spot light we can add a sixth plane
                // This prevents false positives from behind the light
                if ( type != LightType.Directional )
                {
                    // Direction from light to centre point of viewport 
                    normal = ( eyeToWorld * new Vector3( 0, 0, -n ) ) - lightPos3;
                    normal.Normalize();
                    // remember the -d negation in plane constructor
                    nearClipVolume.planes.Add( new Plane( normal, normal.DotProduct( lightPos3 ) ) );
                }
            }
            else
            {
                // light is close to being on the near plane
                // degenerate volume including the entire scene 
                // we will always require light / dark caps
                nearClipVolume.planes.Add( new Plane( Vector3.UnitZ, -n ) );
                nearClipVolume.planes.Add( new Plane( -Vector3.UnitZ, n ) );
            }

            return nearClipVolume;
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
            Vector3 lightDir;

            Vector3[] clockwiseVerts = new Vector3[4];

            Matrix4 eyeToWorld = camera.ViewMatrix.Inverse();

            // Get worldspace frustum corners
            Vector3[] corners = camera.WorldSpaceCorners;

            bool infiniteViewDistance = ( camera.Far == 0 );

            frustumClipVolumes.Clear();

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
                float d = planeVec.DotProduct( lightPos );

                if ( d < -1e-06f )
                {
                    // Ok, this is a valid one
                    // clockwise verts mean we can cross-product and always get normals
                    // facing into the volume we create
                    frustumClipVolumes.Add( new PlaneBoundedVolume() );
                    PlaneBoundedVolume vol =
                        (PlaneBoundedVolume)frustumClipVolumes[frustumClipVolumes.Count - 1];

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

                    for ( int i = 0; i < 4; i++ )
                    {
                        // Figure out light dir
                        lightDir = lightPos3 - ( clockwiseVerts[i] * lightPos.w );

                        // Cross with anticlockwise corner, therefore normal points in
                        // Note: C++ mod returns 3 for the first case where C# returns -1
                        int test = i > 0 ? ( ( i - 1 ) % 4 ) : 3;

                        // Cross with anticlockwise corner, therefore normal points in
                        normal = ( clockwiseVerts[i] - clockwiseVerts[test] ).CrossProduct( lightDir );
                        normal.Normalize();

                        // NB last param to Plane constructor is negated because it's -d
                        vol.planes.Add( new Plane( normal, normal.DotProduct( clockwiseVerts[i] ) ) );
                    }

                    // Now do the near plane (this is the plane of the side we're 
                    // talking about, with the normal inverted (d is already interpreted as -ve)
                    vol.planes.Add( new Plane( -plane.Normal, plane.Distance ) );

                    // Finally, for a point/spot light we can add a sixth plane
                    // This prevents false positives from behind the light
                    if ( type != LightType.Directional )
                    {
                        // re-use our own plane normal
                        // remember the -d negation in plane constructor
                        vol.planes.Add( new Plane( plane.Normal, plane.Normal.DotProduct( lightPos3 ) ) );
                    }
                }
            }

            return frustumClipVolumes;
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

            if ( other.tempSquaredDist > this.tempSquaredDist )
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
