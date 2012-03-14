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

using Axiom.Animating;
using Axiom.Collections;
using Axiom.Graphics;
using Axiom.Math;
using Axiom.Math.Collections;

#endregion Namespace Declarations

namespace Axiom.Core
{
	/// <summary>
	///    Representation of a dynamic light source in the scene.
	/// </summary>
	/// <remarks>
	///    Lights are added to the scene like any other object. They contain various
	///    parameters like type, position, attenuation (how light intensity fades with
	///    distance), color etc.
	///    <p/>
	///    The defaults when a light is created is pure white diffuse light, with no
	///    attenuation (does not decrease with distance) and a range of 1000 world units.
	///    <p/>
	///    Lights are created by using the SceneManager.CreateLight method. They can subsequently be
	///    added to a SceneNode if required to allow them to move relative to a node in the scene. A light attached
	///    to a SceneNode is assumed to have a base position of (0,0,0) and a direction of (0,0,1) before modification
	///    by the SceneNode's own orientation. If note attached to a SceneNode,
	///    the light's position and direction is as set using Position and Direction.
	///    <p/>
	///    Remember also that dynamic lights rely on modifying the color of vertices based on the position of
	///    the light compared to an object's vertex normals. Dynamic lighting will only look good if the
	///    object being lit has a fair level of tesselation and the normals are properly set. This is particularly
	///    true for the spotlight which will only look right on highly tessellated models.
	/// </remarks>
	public class Light : MovableObject, IComparable
	{
		#region Fields

		public static Vector3 DefaultDirection = Vector3.UnitZ;

		/// <summary></summary>
		protected float attenuationConst;

		/// <summary></summary>
		protected float attenuationLinear;

		/// <summary></summary>
		protected float attenuationQuad;

		/// <summary>
		///		Derived direction of this light.
		///	</summary>
		protected Vector3 derivedDirection = Vector3.Zero;

		/// <summary>
		///		Derived position of this light.
		///	</summary>
		protected Vector3 derivedPosition = Vector3.Zero;

		/// <summary>
		///		Diffuse color.
		///	</summary>
		protected ColorEx diffuse;

		/// <summary>
		///    Direction of this light.
		/// </summary>
		protected Vector3 direction = DefaultDirection;

		/// <summary>
		///
		/// </summary>
		protected PlaneBoundedVolumeList frustumClipVolumes = new PlaneBoundedVolumeList();

		/// <summary>
		///		Stored version of parent orientation.
		///	</summary>
		protected Quaternion lastParentOrientation = Quaternion.Identity;

		/// <summary>
		///		Stored version of parent position.
		///	</summary>
		protected Vector3 lastParentPosition = Vector3.Zero;

		/// <summary></summary>
		protected bool localTransformDirty;

		/// <summary>
		///		Stored version of the last near clip volume tested.
		/// </summary>
		protected PlaneBoundedVolume nearClipVolume = new PlaneBoundedVolume();

		protected bool ownShadowFarDistance;

		/// <summary>
		///    Position of this light.
		/// </summary>
		protected Vector3 position = Vector3.Zero;

		protected Real powerScale = 1.0f;

		/// <summary></summary>
		protected float range;

		protected Real shadowFarDistance = -1;
		protected Real shadowFarDistanceSquared = 0.0f;
		protected Real shadowNearDistance = -1;

		/// <summary>
		///		Specular color.
		///	</summary>
		protected ColorEx specular;

		/// <summary></summary>
		protected Real spotFalloff;

		/// <summary></summary>
		protected Radian spotInner;

		/// <summary></summary>
		protected Radian spotOuter;

		/// <summary>
		///    Used for sorting.  Internal for "friend" access to SceneManager.
		/// </summary>
		protected internal float tempSquaredDist;

		/// <summary>
		///    Type of light.
		/// </summary>
		protected LightType type;

		#endregion Fields

		#region Constructors

		/// <summary>
		///		Default constructor.
		/// </summary>
		public Light()
			: this( string.Empty ) { }

		/// <summary>
		///		Normal constructor. Should not be called directly, but rather the SceneManager.CreateLight method should be used.
		/// </summary>
		/// <param name="name"></param>
		public Light( string name )
			: base( name )
		{
			// Default to point light, white diffuse light, linear attenuation, fair range
			this.type = LightType.Point;
			this.diffuse = ColorEx.White;
			this.specular = ColorEx.Black;
			this.range = 100000;
			this.attenuationConst = 1.0f;
			this.attenuationLinear = 0.0f;
			this.attenuationQuad = 0.0f;

			// Center in world, direction irrelevant but set anyway
			this.position = Vector3.Zero;
			this.direction = Vector3.UnitZ;

			// Default some spot values
			this.spotInner = 30.0f;
			this.spotOuter = 40.0f;
			this.spotFalloff = 1.0f;

			this.localTransformDirty = false;
		}

		#endregion Constructors

		#region Properties

		/// <summary>
		///		Gets/Sets the type of light this is.
		/// </summary>
		public virtual LightType Type
		{
			get
			{
				return this.type;
			}
			set
			{
				this.type = value;
			}
		}

		/// <summary>
		///		Gets/Sets the position of the light.
		/// </summary>
		public virtual Vector3 Position
		{
			get
			{
				return this.position;
			}
			set
			{
				this.position = value;
				this.localTransformDirty = true;
			}
		}

		/// <summary>
		///		Gets/Sets the direction of the light.
		/// </summary>
		public virtual Vector3 Direction
		{
			get
			{
				return this.direction;
			}
			set
			{
				//default to UnitZ, as Zero may cause the meshes to be rendered as white
				if ( value.IsZero )
				{
					value = DefaultDirection;
				}

				this.direction = value;
				this.direction.Normalize();
				this.localTransformDirty = true;
			}
		}

		/// <summary>
		///		Gets the inner angle of the spotlight.
		/// </summary>
		public virtual Radian SpotlightInnerAngle
		{
			get
			{
				return this.spotInner;
			}
			set
			{
				this.spotInner = value;
			}
		}

		/// <summary>
		///		Gets the outer angle of the spotlight.
		/// </summary>
		public virtual Radian SpotlightOuterAngle
		{
			get
			{
				return this.spotOuter;
			}
			set
			{
				this.spotOuter = value;
			}
		}

		/// <summary>
		///		Gets the spotlight falloff.
		/// </summary>
		public virtual Real SpotlightFalloff
		{
			get
			{
				return this.spotFalloff;
			}
			set
			{
				this.spotFalloff = value;
			}
		}

		/// <summary>
		///		Gets/Sets the diffuse color of the light.
		/// </summary>
		public virtual ColorEx Diffuse
		{
			get
			{
				return this.diffuse;
			}
			set
			{
				this.diffuse = value;
			}
		}

		/// <summary>
		///		Gets/Sets the specular color of the light.
		/// </summary>
		public virtual ColorEx Specular
		{
			get
			{
				return this.specular;
			}
			set
			{
				this.specular = value;
			}
		}

		/// <summary>
		///		Gets the attenuation range value.
		/// </summary>
		public virtual float AttenuationRange
		{
			get
			{
				return this.range;
			}
			set
			{
				this.range = value;
			}
		}

		/// <summary>
		///		Gets the constant attenuation value.
		/// </summary>
		public virtual float AttenuationConstant
		{
			get
			{
				return this.attenuationConst;
			}
			set
			{
				this.attenuationConst = value;
			}
		}

		/// <summary>
		///		Gets the linear attenuation value.
		/// </summary>
		public virtual float AttenuationLinear
		{
			get
			{
				return this.attenuationLinear;
			}
			set
			{
				this.attenuationLinear = value;
			}
		}

		/// <summary>
		///		Gets the quadratic attenuation value.
		/// </summary>
		public virtual float AttenuationQuadratic
		{
			get
			{
				return this.attenuationQuad;
			}
			set
			{
				this.attenuationQuad = value;
			}
		}

		/// <summary>
		///		Gets the derived position of this light.
		/// </summary>
		public virtual Vector3 DerivedDirection
		{
			get
			{
				// this is called to force an update
				Update();

				return this.derivedDirection;
			}
		}

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

		/// <summary>
		/// Get the near clip plane distance to be used by the shadow camera, if
		/// this light casts texture shadows.
		/// <remarks>
		/// May be zero if the light doesn't have it's own near distance set;
		/// use _deriveShadowNearDistance for a version guaranteed to give a result.
		/// </remarks>
		/// </summary>
		public Real ShadowNearDistance
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return this.shadowNearDistance;
			}

			[OgreVersion( 1, 7, 2 )]
			set
			{
				this.shadowNearDistance = value;
			}
		}

		public Real ShadowFarDistance
		{
			get
			{
				return this.ownShadowFarDistance ? this.shadowFarDistance : Manager.ShadowFarDistance;
			}
			set
			{
				this.ownShadowFarDistance = true;
				this.shadowFarDistance = value;
				this.shadowFarDistanceSquared = value * value;
			}
		}

		public float ShadowFarDistanceSquared
		{
			get
			{
				return this.ownShadowFarDistance ? this.shadowFarDistanceSquared : Manager.ShadowFarDistanceSquared;
			}
		}

		public Real PowerScale
		{
			get
			{
				return this.powerScale;
			}
			set
			{
				this.powerScale = value;
			}
		}


		/// <summary>
		///    Used for sorting.   *** Internal for "friend" access to SceneManager. ***
		/// </summary>
		public float TempSquaredDist
		{
			get
			{
				return this.tempSquaredDist;
			}
			set
			{
				this.tempSquaredDist = value;
			}
		}

		/// <summary>
		///		Gets the derived position of this light.
		/// </summary>
		public virtual Vector3 GetDerivedPosition()
		{
			return GetDerivedPosition( false );
		}

		/// <summary>
		///		Gets the derived position of this light.
		/// </summary>
		public virtual Vector3 GetDerivedPosition( bool cameraRelative )
		{
			// this is called to force an update
			Update();

			if ( cameraRelative && this._cameraToBeRelativeTo != null )
			{
				throw new NotImplementedException();
				//return mDerivedCamRelativePosition;
			}
			else
			{
				return this.derivedPosition;
			}
		}

		#endregion Properties

		#region Methods

		/// <summary>
		///		Updates this lights position.
		/// </summary>
		public virtual void Update()
		{
			if ( parentNode != null )
			{
				if ( !this.localTransformDirty && parentNode.DerivedOrientation == this.lastParentOrientation && parentNode.DerivedPosition == this.lastParentPosition ) { }
				else
				{
					// we are out of date with the scene node we are attached to
					this.lastParentOrientation = parentNode.DerivedOrientation;
					this.lastParentPosition = parentNode.DerivedPosition;
					this.derivedDirection = this.lastParentOrientation * this.direction;
					this.derivedPosition = ( this.lastParentOrientation * this.position ) + this.lastParentPosition;
				}
			}
			else
			{
				this.derivedPosition = this.position;
				this.derivedDirection = this.direction;
			}

			this.localTransformDirty = false;
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
		/// <returns>A 4D vector representation of the light.</returns>
		public virtual Vector4 GetAs4DVector()
		{
			return GetAs4DVector( false );
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
		/// <returns>A 4D vector representation of the light.</returns>
		[OgreVersion( 1, 7, 2 )]
		public virtual Vector4 GetAs4DVector( bool cameraRelativeIfSet )
		{
			Vector4 vec;

			if ( this.type == LightType.Directional )
			{
				// negate direction as 'position'
				vec = -DerivedDirection;

				// infinite distance
				vec.w = 0.0;
			}
			else
			{
				vec = GetDerivedPosition( cameraRelativeIfSet );
				vec.w = 1.0;
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
		public virtual void SetSpotlightRange( float innerAngle, float outerAngle, float falloff )
		{
			//allow it to be set ahead of time anyways
			/*if(type != LightType.Spotlight) {
				throw new Exception("Setting the spotlight range is only valid for spotlights.");
			}*/

			this.spotInner = innerAngle;
			this.spotOuter = outerAngle;
			this.spotFalloff = falloff;
		}

		/// <summary>
		///		Sets the attenuation parameters of the light in a single call.
		/// </summary>
		/// <param name="range"></param>
		/// <param name="constant"></param>
		/// <param name="linear"></param>
		/// <param name="quadratic"></param>
		public virtual void SetAttenuation( float range, float constant, float linear, float quadratic )
		{
			this.range = range;
			this.attenuationConst = constant;
			this.attenuationLinear = linear;
			this.attenuationQuad = quadratic;
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
		internal virtual PlaneBoundedVolume GetNearClipVolume( Camera camera )
		{
			const float THRESHOLD = -1e-06f;

			Real n = camera.Near;

			// First check if the light is close to the near plane, since
			// in this case we have to build a degenerate clip volume
			this.nearClipVolume.planes.Clear();
			this.nearClipVolume.outside = PlaneSide.Negative;

			// Homogenous position
			Vector4 lightPos = GetAs4DVector();
			// 3D version (not the same as DerivedPosition, is -direction for
			// directional lights)
			var lightPos3 = new Vector3( lightPos.x, lightPos.y, lightPos.z );

			// Get eye-space light position
			// use 4D vector so directional lights still work
			Vector4 eyeSpaceLight = camera.ViewMatrix * lightPos;
			Matrix4 eyeToWorld = camera.ViewMatrix.Inverse();

			// Find distance to light, project onto -Z axis
			float d = eyeSpaceLight.Dot( new Vector4( 0, 0, -1, -n ) );

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
					lightDir = lightPos3 - ( corners[ i ] * lightPos.w );
					// Cross with anticlockwise corner, therefore normal points in
					// Note: C++ mod returns 3 for the first case where C# returns -1
					int test = i > 0 ? ( ( i - 1 ) % 4 ) : 3;

					normal = ( corners[ i ] - corners[ test ] ).Cross( lightDir );
					normal.Normalize();

					if ( d < THRESHOLD )
					{
						// invert normal
						normal = -normal;
					}
					// NB last param to Plane constructor is negated because it's -d
					this.nearClipVolume.planes.Add( new Plane( normal, normal.Dot( corners[ i ] ) ) );
				}

				// Now do the near plane plane
				if ( d > THRESHOLD )
				{
					// In front of near plane
					// remember the -d negation in plane constructor
					normal = eyeToWorld * -Vector3.UnitZ;
					normal.Normalize();
					this.nearClipVolume.planes.Add( new Plane( normal, -normal.Dot( camera.DerivedPosition ) ) );
				}
				else
				{
					// Behind near plane
					// remember the -d negation in plane constructor
					normal = eyeToWorld * Vector3.UnitZ;
					normal.Normalize();
					this.nearClipVolume.planes.Add( new Plane( normal, -normal.Dot( camera.DerivedPosition ) ) );
				}

				// Finally, for a point/spot light we can add a sixth plane
				// This prevents false positives from behind the light
				if ( this.type != LightType.Directional )
				{
					// Direction from light to centre point of viewport
					normal = ( eyeToWorld * new Vector3( 0, 0, -n ) ) - lightPos3;
					normal.Normalize();
					// remember the -d negation in plane constructor
					this.nearClipVolume.planes.Add( new Plane( normal, normal.Dot( lightPos3 ) ) );
				}
			}
			else
			{
				// light is close to being on the near plane
				// degenerate volume including the entire scene
				// we will always require light / dark caps
				this.nearClipVolume.planes.Add( new Plane( Vector3.UnitZ, -n ) );
				this.nearClipVolume.planes.Add( new Plane( -Vector3.UnitZ, n ) );
			}

			return this.nearClipVolume;
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
		internal virtual PlaneBoundedVolumeList GetFrustumClipVolumes( Camera camera )
		{
			// Homogenous light position
			Vector4 lightPos = GetAs4DVector();

			// 3D version (not the same as DerivedPosition, is -direction for
			// directional lights)
			var lightPos3 = new Vector3( lightPos.x, lightPos.y, lightPos.z );
			Vector3 lightDir;

			var clockwiseVerts = new Vector3[ 4 ];

			Matrix4 eyeToWorld = camera.ViewMatrix.Inverse();

			// Get worldspace frustum corners
			Vector3[] corners = camera.WorldSpaceCorners;

			bool infiniteViewDistance = ( camera.Far == 0 );

			this.frustumClipVolumes.Clear();

			for ( int n = 0; n < 6; n++ )
			{
				var frustumPlane = (FrustumPlane)n;

				// skip far plane if infinite view frustum
				if ( infiniteViewDistance && ( frustumPlane == FrustumPlane.Far ) )
				{
					continue;
				}

				Plane plane = camera[ frustumPlane ];

				var planeVec = new Vector4( plane.Normal.x, plane.Normal.y, plane.Normal.z, plane.D );

				// planes face inwards, we need to know if light is on negative side
				float d = planeVec.Dot( lightPos );

				if ( d < -1e-06f )
				{
					// Ok, this is a valid one
					// clockwise verts mean we can cross-product and always get normals
					// facing into the volume we create
					this.frustumClipVolumes.Add( new PlaneBoundedVolume() );
					PlaneBoundedVolume vol = this.frustumClipVolumes[ this.frustumClipVolumes.Count - 1 ];

					switch ( frustumPlane )
					{
						case ( FrustumPlane.Near ):
							clockwiseVerts[ 0 ] = corners[ 3 ];
							clockwiseVerts[ 1 ] = corners[ 2 ];
							clockwiseVerts[ 2 ] = corners[ 1 ];
							clockwiseVerts[ 3 ] = corners[ 0 ];
							break;
						case ( FrustumPlane.Far ):
							clockwiseVerts[ 0 ] = corners[ 7 ];
							clockwiseVerts[ 1 ] = corners[ 6 ];
							clockwiseVerts[ 2 ] = corners[ 5 ];
							clockwiseVerts[ 3 ] = corners[ 4 ];
							break;
						case ( FrustumPlane.Left ):
							clockwiseVerts[ 0 ] = corners[ 2 ];
							clockwiseVerts[ 1 ] = corners[ 6 ];
							clockwiseVerts[ 2 ] = corners[ 5 ];
							clockwiseVerts[ 3 ] = corners[ 1 ];
							break;
						case ( FrustumPlane.Right ):
							clockwiseVerts[ 0 ] = corners[ 7 ];
							clockwiseVerts[ 1 ] = corners[ 3 ];
							clockwiseVerts[ 2 ] = corners[ 0 ];
							clockwiseVerts[ 3 ] = corners[ 4 ];
							break;
						case ( FrustumPlane.Top ):
							clockwiseVerts[ 0 ] = corners[ 0 ];
							clockwiseVerts[ 1 ] = corners[ 1 ];
							clockwiseVerts[ 2 ] = corners[ 5 ];
							clockwiseVerts[ 3 ] = corners[ 4 ];
							break;
						case ( FrustumPlane.Bottom ):
							clockwiseVerts[ 0 ] = corners[ 7 ];
							clockwiseVerts[ 1 ] = corners[ 6 ];
							clockwiseVerts[ 2 ] = corners[ 2 ];
							clockwiseVerts[ 3 ] = corners[ 3 ];
							break;
					}

					// Build a volume
					// Iterate over world points and form side planes
					Vector3 normal;

					for ( int i = 0; i < 4; i++ )
					{
						// Figure out light dir
						lightDir = lightPos3 - ( clockwiseVerts[ i ] * lightPos.w );

						// Cross with anticlockwise corner, therefore normal points in
						// Note: C++ mod returns 3 for the first case where C# returns -1
						int test = i > 0 ? ( ( i - 1 ) % 4 ) : 3;

						// Cross with anticlockwise corner, therefore normal points in
						normal = ( clockwiseVerts[ i ] - clockwiseVerts[ test ] ).Cross( lightDir );
						normal.Normalize();

						// NB last param to Plane constructor is negated because it's -d
						vol.planes.Add( new Plane( normal, normal.Dot( clockwiseVerts[ i ] ) ) );
					}

					// Now do the near plane (this is the plane of the side we're
					// talking about, with the normal inverted (d is already interpreted as -ve)
					vol.planes.Add( new Plane( -plane.Normal, plane.D ) );

					// Finally, for a point/spot light we can add a sixth plane
					// This prevents false positives from behind the light
					if ( this.type != LightType.Directional )
					{
						// re-use our own plane normal
						// remember the -d negation in plane constructor
						vol.planes.Add( new Plane( plane.Normal, plane.Normal.Dot( lightPos3 ) ) );
					}
				}
			}

			return this.frustumClipVolumes;
		}

		#endregion Methods

		#region CustomShadowCameraSetup Implementation

		/// <summary>
		/// the custom shadow camera setup (null means use <see cref="SceneManager"/> global version).
		/// </summary>
		private IShadowCameraSetup _customShadowCameraSetup;

		/// <summary>
		/// this light's reference to the custom shadow camera to use when rendering texture shadows.
		/// (null means use <see cref="SceneManager"/> global version).
		/// </summary>
		/// <remarks>
		/// This changes the shadow camera setup for just this light, you can set
		/// the shadow camera setup globally using <see cref="SceneManager.ShadowCameraSetup"/>
		/// </remarks>
		public virtual IShadowCameraSetup CustomShadowCameraSetup
		{
			get
			{
				return this._customShadowCameraSetup;
			}

			set
			{
				this._customShadowCameraSetup = value;
			}
		}

		/// <summary>
		/// Reset the shadow camera setup to the default.
		/// </summary>
		/// <seealso cref="IShadowCameraSetup"/>
		public virtual void ResetCustomShadowCameraSetup()
		{
			this._customShadowCameraSetup = null;
		}

		#endregion CustomShadowCameraSetup Implementation

		#region IAnimable Implementation

		public static string[] animableAttributes = {
                                                        "diffuseColour", "specularColour", "attenuation", "AttenuationRange", "AttenuationConstant", "AttenuationLinear", "AttenuationQuadratic", "spotlightInner", "spotlightOuter", "spotlightFalloff", "Diffuse", "Specular"
                                                    };

		private Camera _cameraToBeRelativeTo;
		private bool _derivedCamRelativeDirty;

		protected internal Camera CameraRelative
		{
			set
			{
				this._cameraToBeRelativeTo = value;
				this._derivedCamRelativeDirty = true;
			}
		}

		/// <summary>
		///     Part of the IAnimableObject interface.
		/// </summary>
		public override string[] AnimableValueNames
		{
			get
			{
				return animableAttributes;
			}
		}

		public override AnimableValue CreateAnimableValue( string valueName )
		{
			switch ( valueName )
			{
				case "diffuseColour":
				case "Diffuse":
					return new LightDiffuseColorValue( this );
				case "specularColour":
				case "Specular":
					return new LightSpecularColorValue( this );
				case "attenuation":
					return new LightAttenuationValue( this );
				case "AttenuationRange":
					return new LightAttenuationRangeValue( this );
				case "AttenuationConstant":
					return new LightAttenuationConstantValue( this );
				case "AttenuationLinear":
					return new LightAttenuationLinearValue( this );
				case "AttenuationQuadratic":
					return new LightAttenuationQuadraticValue( this );
				case "spotlightInner":
					return new LightSpotlightInnerValue( this );
				case "spotlightOuter":
					return new LightSpotlightOuterValue( this );
				case "spotlightFalloff":
					return new LightSpotlightFalloffValue( this );
			}
			throw new Exception( string.Format( "Could not find animable attribute '{0}'", valueName ) );
		}

		#region Nested type: LightAttenuationConstantValue

		protected class LightAttenuationConstantValue : AnimableValue
		{
			protected Light light;

			public LightAttenuationConstantValue( Light light )
				: base( AnimableType.Real )
			{
				this.light = light;
				SetAsBaseValue( 0.0f );
			}

			public override void SetValue( Real val )
			{
				this.light.AttenuationConstant = val;
			}

			public override void ApplyDeltaValue( Real val )
			{
				SetValue( val + this.light.AttenuationConstant );
			}

			public override void SetCurrentStateAsBaseValue()
			{
				SetAsBaseValue( this.light.AttenuationConstant );
			}
		}

		#endregion

		#region Nested type: LightAttenuationLinearValue

		protected class LightAttenuationLinearValue : AnimableValue
		{
			protected Light light;

			public LightAttenuationLinearValue( Light light )
				: base( AnimableType.Real )
			{
				this.light = light;
				SetAsBaseValue( 0.0f );
			}

			public override void SetValue( Real val )
			{
				this.light.AttenuationLinear = val;
			}

			public override void ApplyDeltaValue( Real val )
			{
				SetValue( val + this.light.AttenuationLinear );
			}

			public override void SetCurrentStateAsBaseValue()
			{
				SetAsBaseValue( this.light.AttenuationLinear );
			}
		}

		#endregion

		#region Nested type: LightAttenuationQuadraticValue

		protected class LightAttenuationQuadraticValue : AnimableValue
		{
			protected Light light;

			public LightAttenuationQuadraticValue( Light light )
				: base( AnimableType.Real )
			{
				this.light = light;
				SetAsBaseValue( 0.0f );
			}

			public override void SetValue( Real val )
			{
				this.light.AttenuationQuadratic = val;
			}

			public override void ApplyDeltaValue( Real val )
			{
				SetValue( val + this.light.AttenuationQuadratic );
			}

			public override void SetCurrentStateAsBaseValue()
			{
				SetAsBaseValue( this.light.AttenuationQuadratic );
			}
		}

		#endregion

		#region Nested type: LightAttenuationRangeValue

		protected class LightAttenuationRangeValue : AnimableValue
		{
			protected Light light;

			public LightAttenuationRangeValue( Light light )
				: base( AnimableType.Real )
			{
				this.light = light;
				SetAsBaseValue( 0.0f );
			}

			public override void SetValue( Real val )
			{
				this.light.AttenuationRange = val;
			}

			public override void ApplyDeltaValue( Real val )
			{
				SetValue( val + this.light.AttenuationRange );
			}

			public override void SetCurrentStateAsBaseValue()
			{
				SetAsBaseValue( this.light.AttenuationRange );
			}
		}

		#endregion

		#region Nested type: LightAttenuationValue

		protected class LightAttenuationValue : AnimableValue
		{
			protected Light light;

			public LightAttenuationValue( Light light )
				: base( AnimableType.Vector4 )
			{
				this.light = light;
			}

			public override void SetValue( Vector4 val )
			{
				this.light.SetAttenuation( val.x, val.y, val.z, val.w );
			}

			public override void ApplyDeltaValue( Vector4 val )
			{
				Vector4 v = this.light.GetAs4DVector();
				SetValue( new Vector4( v.x + val.x, v.y + val.y, v.z + val.z, v.w + val.w ) );
			}

			public override void SetCurrentStateAsBaseValue()
			{
				SetAsBaseValue( this.light.GetAs4DVector() );
			}
		}

		#endregion

		#region Nested type: LightDiffuseColorValue

		protected class LightDiffuseColorValue : AnimableValue
		{
			protected Light light;

			public LightDiffuseColorValue( Light light )
				: base( AnimableType.ColorEx )
			{
				this.light = light;
				SetAsBaseValue( ColorEx.Black );
			}

			public override void SetValue( ColorEx val )
			{
				this.light.Diffuse = val;
			}

			public override void ApplyDeltaValue( ColorEx val )
			{
				ColorEx c = this.light.Diffuse;
				SetValue( new ColorEx( c.a * val.a, c.r + val.r, c.g + val.g, c.b + val.b ) );
			}

			public override void SetCurrentStateAsBaseValue()
			{
				SetAsBaseValue( this.light.Diffuse );
			}
		}

		#endregion

		#region Nested type: LightSpecularColorValue

		protected class LightSpecularColorValue : AnimableValue
		{
			protected Light light;

			public LightSpecularColorValue( Light light )
				: base( AnimableType.ColorEx )
			{
				this.light = light;
				SetAsBaseValue( ColorEx.Black );
			}

			public override void SetValue( ColorEx val )
			{
				this.light.Specular = val;
			}

			public override void ApplyDeltaValue( ColorEx val )
			{
				ColorEx c = this.light.Specular;
				SetValue( new ColorEx( c.a + val.a, c.r + val.r, c.g + val.g, c.b + val.b ) );
			}

			public override void SetCurrentStateAsBaseValue()
			{
				SetAsBaseValue( this.light.Specular );
			}
		}

		#endregion

		#region Nested type: LightSpotlightFalloffValue

		protected class LightSpotlightFalloffValue : AnimableValue
		{
			protected Light light;

			public LightSpotlightFalloffValue( Light light )
				: base( AnimableType.Real )
			{
				this.light = light;
			}

			public override void SetValue( Real val )
			{
				this.light.SpotlightFalloff = val;
			}

			public override void ApplyDeltaValue( Real val )
			{
				SetValue( this.light.SpotlightFalloff + val );
			}

			public override void SetCurrentStateAsBaseValue()
			{
				SetAsBaseValue( this.light.SpotlightFalloff );
			}
		}

		#endregion

		#region Nested type: LightSpotlightInnerValue

		protected class LightSpotlightInnerValue : AnimableValue
		{
			protected Light light;

			public LightSpotlightInnerValue( Light light )
				: base( AnimableType.Real )
			{
				this.light = light;
			}

			public override void SetValue( Real val )
			{
				this.light.SpotlightInnerAngle = Utility.RadiansToDegrees( val );
			}

			public override void ApplyDeltaValue( Real val )
			{
				SetValue( Utility.DegreesToRadians( this.light.SpotlightInnerAngle ) + val );
			}

			public override void SetCurrentStateAsBaseValue()
			{
				SetAsBaseValue( Utility.DegreesToRadians( this.light.SpotlightInnerAngle ) );
			}
		}

		#endregion

		#region Nested type: LightSpotlightOuterValue

		protected class LightSpotlightOuterValue : AnimableValue
		{
			protected Light light;

			public LightSpotlightOuterValue( Light light )
				: base( AnimableType.Real )
			{
				this.light = light;
			}

			public override void SetValue( Real val )
			{
				this.light.SpotlightOuterAngle = Utility.RadiansToDegrees( val );
			}

			public override void ApplyDeltaValue( Real val )
			{
				SetValue( Utility.DegreesToRadians( this.light.SpotlightOuterAngle ) + val );
			}

			public override void SetCurrentStateAsBaseValue()
			{
				SetAsBaseValue( Utility.DegreesToRadians( this.light.SpotlightOuterAngle ) );
			}
		}

		#endregion

		#endregion IAnimable Implementation

		#region MovableObject Implementation

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

		/// <summary>
		/// Get the 'type flags' for this <see cref="Light"/>.
		/// </summary>
		/// <seealso cref="MovableObject.TypeFlags"/>
		public override uint TypeFlags
		{
			get
			{
				return (uint)SceneQueryTypeMask.Light;
			}
		}

		public override void NotifyCurrentCamera( Camera camera )
		{
			// Do nothing
		}

		public override void UpdateRenderQueue( RenderQueue queue )
		{
			// Do Nothing
		}

		#endregion MovableObject Implementation

		/// <summary>
		/// Gets the index at which this light is in the current render.
		/// </summary>
		/// <remarks>
		/// Lights will be present in the in a list for every renderable,
		/// detected and sorted appropriately, and sometimes it's useful to know 
		/// what position in that list a given light occupies. This can vary 
		/// from frame to frame (and object to object) so you should not use this
		/// value unless you're sure the context is correct.
		/// </remarks>
		public int IndexInFrame
		{
			get
			{
				throw new NotImplementedException();
			}
			set
			{
				throw new NotImplementedException();
			}
		}

		#region IComparable Members

		/// <summary>
		///    Used to compare this light to another light for sorting.
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public virtual int CompareTo( object obj )
		{
			var other = obj as Light;

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

		/// <summary>
		/// Update a custom GpuProgramParameters constant which is derived from 
		/// information only this Light knows.
		/// </summary>
		/// <remarks>
		/// This method allows a Light to map in a custom GPU program parameter
		/// based on it's own data. This is represented by a GPU auto parameter
		/// of ACT_LIGHT_CUSTOM, and to allow there to be more than one of these per
		/// Light, the 'data' field on the auto parameter will identify
		/// which parameter is being updated and on which light. The implementation 
		/// of this method must identify the parameter being updated, and call a 'setConstant' 
		/// method on the passed in GpuProgramParameters object.
		/// @par
		/// You do not need to override this method if you're using the standard
		/// sets of data associated with the Renderable as provided by setCustomParameter
		/// and getCustomParameter. By default, the implementation will map from the
		/// value indexed by the 'constantEntry.data' parameter to a value previously
		/// set by setCustomParameter. But custom Renderables are free to override
		/// this if they want, in any case.
		/// </remarks>
		/// 
		/// <param name="paramIndex">The index of the constant being updated</param>
		/// <param name="constantEntry">The auto constant entry from the program parameters</param>
		/// <param name="parameters">The parameters object which this method should call to 
		/// set the updated parameters.</param>
		internal void UpdateCustomGpuParameter( ushort paramIndex, GpuProgramParameters.AutoConstantEntry constantEntry, GpuProgramParameters parameters )
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Derive a shadow camera near distance from either the light, or
		///from the main camera if the light doesn't have its own setting.
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		internal Real DeriveShadowNearClipDistance( Camera camera )
		{
			if ( this.shadowNearDistance > 0 )
			{
				return this.shadowNearDistance;
			}
			else
			{
				return camera.Near;
			}
		}

		/// <summary>
		/// Derive a shadow camera far distance from either the light, or
		/// from the main camera if the light doesn't have its own setting.
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		internal Real DeriveShadowFarClipDistance( Camera camera )
		{
			if ( this.shadowFarDistance >= 0 )
			{
				return this.shadowFarDistance;
			}
			else
			{
				if ( this.type == LightType.Directional )
				{
					return 0;
				}
				else
				{
					return this.range;
				}
			}
		}
	}

	#region MovableObjectFactory Implementation

	public class LightFactory : MovableObjectFactory
	{
		public new const string TypeName = "Light";

		public LightFactory()
		{
			base.Type = TypeName;
			base.TypeFlag = (uint)SceneQueryTypeMask.Light;
		}

		protected override MovableObject _createInstance( string name, NamedParameterList param )
		{
			var light = new Light( name );

			if ( param != null )
			{
				// Setting the light type first before any property specific to a certain light type
				if ( param.ContainsKey( "type" ) )
				{
					switch ( param[ "type" ].ToString() )
					{
						case "point":
							light.Type = LightType.Point;
							break;
						case "directional":
							light.Type = LightType.Directional;
							break;
						case "spot":
						case "spotlight":
							light.Type = LightType.Spotlight;
							break;
						default:
							throw new AxiomException( "Invalid light type '" + param[ "type" ] + "'." );
					}
				}

				// Common properties
				if ( param.ContainsKey( "position" ) )
				{
					light.Position = Vector3.Parse( param[ "position" ].ToString() );
				}

				if ( param.ContainsKey( "direction" ) )
				{
					light.Direction = Vector3.Parse( param[ "direction" ].ToString() );
				}

				if ( param.ContainsKey( "diffuseColour" ) )
				{
					light.Diffuse = ColorEx.Parse_0_255_String( param[ "diffuseColour" ].ToString() );
				}

				if ( param.ContainsKey( "specularColour" ) )
				{
					light.Specular = ColorEx.Parse_0_255_String( param[ "specularColour" ].ToString() );
				}

				if ( param.ContainsKey( "attenuation" ) )
				{
					Vector4 attenuation = Vector4.Parse( param[ "attenuation" ].ToString() );
					light.SetAttenuation( attenuation.x, attenuation.y, attenuation.z, attenuation.w );
				}

				if ( param.ContainsKey( "castShadows" ) )
				{
					light.CastShadows = Convert.ToBoolean( param[ "castShadows" ].ToString() );
				}

				if ( param.ContainsKey( "visible" ) )
				{
					light.CastShadows = Convert.ToBoolean( param[ "visible" ].ToString() );
				}
				// TODO: Add PowerScale Property to Light
				if ( param.ContainsKey( "powerScale" ) )
				{
					light.PowerScale = (float)Convert.ToDouble( param[ "powerScale" ].ToString() );
				}
				// TODO: Add ShadowFarDistance to Light
				if ( param.ContainsKey( "shadowFarDistance" ) )
				{
					light.ShadowFarDistance = (float)Convert.ToDouble( param[ "shadowFarDistance" ].ToString() );
				}

				// Spotlight properties
				if ( param.ContainsKey( "spotlightInner" ) )
				{
					light.SpotlightInnerAngle = (float)Convert.ToDouble( param[ "spotlightInner" ].ToString() );
				}

				if ( param.ContainsKey( "spotlightOuter" ) )
				{
					light.SpotlightOuterAngle = (float)Convert.ToDouble( param[ "spotlightOuter" ].ToString() );
				}

				if ( param.ContainsKey( "spotlightFalloff" ) )
				{
					light.SpotlightFalloff = (float)Convert.ToDouble( param[ "spotlightFalloff" ].ToString() );
				}
			}

			return light;
		}

		public override void DestroyInstance( ref MovableObject obj )
		{
			obj = null;
		}
	}

	#endregion MovableObjectFactory Implementation
}
