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

#region Namespace Declarations

using System;
using System.Diagnostics;

using Axiom.MathLib;

#endregion

#region Ogre Synchronization Information
/// <ogresynchronization>
///     <file name="AnimationTrack.h"   revision="1.13.2.2" lastUpdated="10/15/2005" lastUpdatedBy="DanielH" />
///     <file name="AnimationTrack.cpp" revision="1.28.2.3" lastUpdated="10/15/2005" lastUpdatedBy="DanielH" />
/// </ogresynchronization>
#endregion

namespace Axiom
{
    /// <summary>
    ///		A 'track' in an animation sequence, ie a sequence of keyframes which affect a
    ///		certain type of object that can be animated.
    /// </summary>
    /// <remarks>
    ///		This class is intended as a base for more complete classes which will actually
    ///		animate specific types of object, e.g. a bone in a skeleton to affect
    ///		skeletal animation. An animation will likely include multiple tracks each of which
    ///		can be made up of many KeyFrame instances. Note that the use of tracks allows each animatable
    ///		object to have it's own number of keyframes, i.e. you do not have to have the
    ///		maximum number of keyframes for all animable objects just to cope with the most
    ///		animated one.
    ///		<para/>
    ///		Since the most common animatable object is a Node, there are options in this class for associating
    ///		the track with a Node which will receive keyframe updates automatically when the <see cref="Apply"/> method
    ///		is called.
    ///     <para/>
    /// 	By default rotation is done using shortest-path algorithm. It is possible to change this behaviour using
    ///	    <see cref="setUseShortestRotationPath"/> method.
    /// </remarks>
    public class AnimationTrack
    {
        #region Fields

        /// <summary>
        ///		Handle of this animation track.
        ///	</summary>
        protected short handle;
        /// <summary>
        ///		Animation that owns this track.
        ///	</summary>
        protected Animation parent;
        /// <summary>
        ///		Target node to be animated.
        ///	</summary>
        protected Node targetNode;
        /// <summary>
        ///		Maximum keyframe time.
        ///	</summary>
        protected float maxKeyFrameTime;
        /// <summary>
        ///		Collection of key frames in this track.
        ///	</summary>
        protected KeyFrameCollection keyFrameList = new KeyFrameCollection();
        /// <summary>
        ///		Flag indicating we need to rebuild the splines next time.
        ///	</summary>
        protected bool isSplineRebuildNeeded;
        /// <summary>
        ///		Spline for position interpolation.
        ///	</summary>
        protected PositionalSpline positionSpline = new PositionalSpline();
        /// <summary>
        ///		Spline for scale interpolation.
        ///	</summary>
        protected PositionalSpline scaleSpline = new PositionalSpline();
        /// <summary>
        ///		Spline for rotation interpolation.
        ///	</summary>
        protected RotationalSpline rotationalSpline = new RotationalSpline();
        /// <summary>
        ///		Defines if rotation is done using shortest path
        /// </summary>
        protected bool useShortestRotationPath;

        #endregion Fields

        #region Constructors

        /// <summary>
        ///		Internal constructor, to prevent direction instantiation.  Should be created
        ///		via a call to the CreateTrack method of an Animation.
        /// </summary>
        internal AnimationTrack( Animation parent )
            : this( parent, null )
        {
        }

        /// <summary>
        ///		Internal constructor, to prevent direction instantiation.  Should be created
        ///		via a call to the CreateTrack method of an Animation.
        /// </summary>
        internal AnimationTrack( Animation parent, Node target )
        {
            this.parent = parent;
            this.targetNode = target;

            maxKeyFrameTime = -1;

            // use shortest path rotation by default
            useShortestRotationPath = true;
        }

        #endregion

        #region Properties

        /// <summary>
        ///		The name of this animation track.
        /// </summary>
        public short Handle
        {
            get
            {
                return handle;
            }
            set
            {
                handle = value;
            }
        }

        /// <summary>
        ///		Collection of the KeyFrames present in this AnimationTrack.
        /// </summary>
        public KeyFrameCollection KeyFrames
        {
            get
            {
                return keyFrameList;
            }
        }

        /// <summary>
        ///		Gets/Sets the target node that this track is associated with.
        /// </summary>
        public Node AssociatedNode
        {
            get
            {
                return targetNode;
            }
            set
            {
                targetNode = value;
            }
        }
		public bool UseShortestRotationPath
		{
			get
			{
				return useShortestRotationPath;
			}
			set
			{
				useShortestRotationPath = value;
			}
		}
        #endregion

        #region Public methods
		public bool HasNonZeroKeyFrames()
		{
			for ( int i = 0; i < keyFrameList.Count; i++ )
			{
				KeyFrame kf = keyFrameList[i];
				// look for keyframes which have any component which is non-zero
				// Since exporters can be a little inaccurate sometimes we use a
				// tolerance value rather than looking for nothing
				Vector3 trans = kf.Translate;
				Vector3 scale = kf.Scale;
				Vector3 axis = new Vector3();
				float angle = 0f;
				kf.Rotation.ToAngleAxis(ref angle, ref axis);
				float tolerance = 1e-3f;
				

				
				if (!trans.PositionEquals(Vector3.Zero, tolerance) ||
					!scale.PositionEquals(Vector3.UnitScale, tolerance) ||
					!Axiom.MathLib.MathUtil.RealEqual(angle, 0.0f, tolerance))
				{
					return true;
				}

		
				
			}

			return false;
		}

		public void Optimise()
		{
			// Eliminate duplicate keyframes from 2nd to penultimate keyframe 
			// NB only eliminate middle keys from sequences of 5+ identical keyframes
			// since we need to preserve the boundary keys in place, and we need
			// 2 at each end to preserve tangents for spline interpolation
			Vector3 lasttrans = new Vector3();
			Vector3 lastscale = new Vector3();
			Quaternion lastorientation = new Quaternion();

			float quatTolerance = 1e-3f;

			System.Collections.ArrayList removeList = new System.Collections.ArrayList();

			short k = 0;
			ushort dupKfCount = 0;


			for ( int i = 0; i < keyFrameList.Count; i++ )
			{
				KeyFrame kf = keyFrameList[i];

				Vector3 newtrans = kf.Translate;
				Vector3 newscale = kf.Scale;
				Quaternion neworientation = kf.Rotation;
				// Ignore first keyframe; now include the last keyframe as we eliminate
				// only k-2 in a group of 5 to ensure we only eliminate middle keys
				if (i != 0)
				{
					if (newtrans.PositionEquals(lasttrans) &&
						newscale.PositionEquals(lastscale) && 
						neworientation.Equals(lastorientation, quatTolerance))
					{
						++dupKfCount;

						// 4 indicates this is the 5th duplicate keyframe
						if (dupKfCount == 4)
						{
							// remove the 'middle' keyframe
							removeList.Add(k-2);	
							--dupKfCount;
						}
					}
					else
					{
						// reset
						dupKfCount = 0;
					}
						
				}

				lasttrans = newtrans;
				lastscale = newscale;
				lastorientation = neworientation;
			}

			// Now remove keyframes, in reverse order to avoid index revocation
			for ( int r = 0; r < removeList.Count; r++ )
			{
				short removeIndex = (short)removeList[r];
				RemoveKeyFrame(removeIndex);
			}
		}
        /// <summary>
        ///		Creates a new KeyFrame and adds it to this animation at the given time index.
        /// </summary>
        /// <remarks>
        ///		It is better to create KeyFrames in time order. Creating them out of order can result 
        ///		in expensive reordering processing. Note that a KeyFrame at time index 0.0 is always created
        ///		for you, so you don't need to create this one, just access it using KeyFrames[0];
        /// </remarks>
        /// <param name="time">Time within the animation at which this keyframe will lie.</param>
        /// <returns>A new KeyFrame.</returns>
        public KeyFrame CreateKeyFrame( float time )
        {
            KeyFrame keyFrame = new KeyFrame( this, time );

            if ( time > maxKeyFrameTime || ( time == 0 && keyFrameList.Count == 0 ) )
            {
                keyFrameList.Add( keyFrame );
                maxKeyFrameTime = time;
            }
            else
            {
                // search for the correct place to insert the keyframe
                int i = 0;
                KeyFrame kf = keyFrameList[i];

                while ( kf.Time < time && i != keyFrameList.Count )
                {
                    i++;
                }

                keyFrameList.Insert( i, kf );
            }

            // ensure a spline rebuild takes place
            OnKeyFrameDataChanged();

            return keyFrame;
        }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public KeyFrame GetKeyFrame( int index )
		{
			int count = keyFrameList.Count;
			if ( index > count || index < 0 )
				throw new ArgumentOutOfRangeException( "KeyFrame index is invalid." );
			KeyFrame keyFrame = keyFrameList[index];
			return keyFrame;
		}
        /// <summary>
        ///		Gets the 2 KeyFrame objects which are active at the time given, and the blend value between them.
        /// </summary>
        /// <remarks>
        ///		At any point in time  in an animation, there are either 1 or 2 keyframes which are 'active',
        ///		1 if the time index is exactly on a keyframe, 2 at all other times i.e. the keyframe before
        ///		and the keyframe after.
        /// </remarks>
        /// <param name="time">The time index in seconds.</param>
        /// <param name="keyFrame1">Receive the keyframe just before or at this time index.</param>
        /// <param name="keyFrame2">Receive the keyframe just after this time index.</param>
        /// <param name="firstKeyIndex">If supplied, will receive the index of the 'from' keyframe incase the caller needs it.</param>
        /// <returns>
        ///		Parametric value indicating how far along the gap between the 2 keyframes the time
        ///    value is, e.g. 0.0 for exactly at 1, 0.25 for a quarter etc. By definition the range of this 
        ///    value is:  0.0 &lt;= returnValue &lt; 1.0 .
        ///</returns>
        public float GetKeyFramesAtTime( float time, out KeyFrame keyFrame1, out KeyFrame keyFrame2, out ushort firstKeyIndex )
        {
            short firstIndex = -1;
            float totalLength = parent.Length;

            // wrap time
            while ( time > totalLength )
                time -= totalLength;

            int i = 0;

            keyFrame1 = null;

            // find the last keyframe before or on current time
            for ( i = 0; i < keyFrameList.Count; i++ )
            {
                KeyFrame keyFrame = keyFrameList[i];

                // kick out now if the current frames time is greater than the current time
                if ( keyFrame.Time > time )
                    break;

                keyFrame1 = keyFrame;
                ++firstIndex;
            }

            // trap case where there is no key before this time
            // use the first key anyway and pretend it's time index 0
            if ( firstIndex == -1 )
            {
                keyFrame1 = keyFrameList[0];
                ++firstIndex;
            }

            // fill index of the first key
            firstKeyIndex = (ushort)firstIndex;

            // parametric time
            // t1 = time of previous keyframe
            // t2 = time of next keyframe
            float t1, t2;

            // find first keyframe after the time
            // if no next keyframe, wrap back to first
            // TODO Verify logic
            if ( firstIndex == ( keyFrameList.Count - 1 ) )
            {
                keyFrame2 = keyFrameList[0];
                t2 = totalLength;
            }
            else
            {
                keyFrame2 = keyFrameList[firstIndex + 1];
                t2 = keyFrame2.Time;
            }

            t1 = keyFrame1.Time;

            if ( t1 == t2 )
            {
                // same keyframe
                return 0.0f;
            }
            else
            {
                return ( time - t1 ) / ( t2 - t1 );
            }
        }

        /// <summary>
        ///		Gets a KeyFrame object which contains the interpolated transforms at the time index specified.
        /// </summary>
        /// <remarks>
        ///		The KeyFrame objects held by this class are transformation snapshots at 
        ///		discrete points in time. Normally however, you want to interpolate between these
        ///		keyframes to produce smooth movement, and this method allows you to do this easily.
        ///		In animation terminology this is called 'tweening'. 
        /// </remarks>
        /// <param name="time">The time (in relation to the whole animation sequence).</param>
        /// <returns>
        ///		A new keyframe object containing the interpolated transforms. Note that the
        ///		position and scaling transforms are linearly interpolated (lerp), whilst the rotation is
        ///		spherically linearly interpolated (slerp) for the most natural result.
        /// </returns>
        public KeyFrame GetInterpolatedKeyFrame( float time )
        {
            // note: this is an un-attached keyframe
            KeyFrame result = new KeyFrame( null, time );

            KeyFrame k1, k2;
            ushort firstKeyIndex;

            float t = GetKeyFramesAtTime( time, out k1, out k2, out firstKeyIndex );

            if ( t == 0.0f )
            {
                // just use k1
                result.Rotation = k1.Rotation;
                result.Translate = k1.Translate;
                result.Scale = k1.Scale;
            }
            else
            {
                // interpolate by t
                InterpolationMode interpolationMode = parent.InterpolationMode;
				RotationInterpolationMode rim = parent.RotationInterpolationMode;
				Vector3 baseVector;

                switch ( interpolationMode )
                {

                    case InterpolationMode.Linear:
                        {

						// Interpolate linearly
						// Rotation
						// Interpolate to nearest rotation if mUseShortestRotationPath set
						if (rim == RotationInterpolationMode.Linear)
						{
						result.Rotation = Quaternion.Nlerp(t, k1.Rotation, k2.Rotation, useShortestRotationPath);
						}
						else //if (rim == Animation::RIM_SPHERICAL)
						{
							result.Rotation =  Quaternion.Slerp(t, k1.Rotation, k2.Rotation, useShortestRotationPath);
						}

						// Translation
						baseVector = k1.Translate;
						result.Translate = ( baseVector + ((k2.Translate - baseVector) * t) );

						// Scale
						baseVector = k1.Scale;
						result.Scale = (baseVector + ((k2.Scale - baseVector) * t));
						
                        }
						break;

                    case InterpolationMode.Spline:
                        {

						// Spline interpolation

						// Build splines if required
						if (isSplineRebuildNeeded)
						{
							this.BuildInterpolationSplines();
						}

						// Rotation, take mUseShortestRotationPath into account
						result.Rotation = this.rotationalSpline.Interpolate(firstKeyIndex, t, useShortestRotationPath);

						// Translation
						result.Translate = positionSpline.Interpolate(firstKeyIndex, t);

						// Scale
						result.Scale =  scaleSpline.Interpolate(firstKeyIndex, t);
                        }
                        break;

                }
            }

            // return the resulting keyframe
            return result;
        }
		public void Apply( float time, float weight, bool accumulate, float scale )
		{
			ApplyToNode(targetNode, time, weight, accumulate, scale);
		}

        /// <summary>
        ///		Overloaded Apply method.  
        /// </summary>
        /// <param name="time"></param>
        public void Apply( float time )
        {
            // call overloaded method
            Apply( time, 1.0f, false, 1.0F );
        }


		public void ApplyToNode(Node node, float timePos, float weight, bool accumulate, float scl)
		{
			KeyFrame kf = this.GetInterpolatedKeyFrame(timePos);
			if (accumulate) 
			{
				// add to existing. Weights are not relative, but treated as absolute multipliers for the animation
				Vector3 translate = kf.Translate * weight * scl;
				node.Translate(translate);

				// interpolate between no-rotation and full rotation, to point 'weight', so 0 = no rotate, 1 = full
				Quaternion rotate;
				RotationInterpolationMode rim = parent.RotationInterpolationMode;
				if (rim == RotationInterpolationMode.Linear)
				{
					rotate = Quaternion.Nlerp(weight, Quaternion.Identity, kf.Rotation);
				}
				else //if (rim == Animation.RIM_SPHERICAL)
				{
					rotate = Quaternion.Slerp(weight, Quaternion.Identity, kf.Rotation);
				}
				node.Rotate(rotate);

				Vector3 scale = kf.Scale;
				// Not sure how to modify scale for cumulative anims... leave it alone
				//scale = ((Vector3.UNIT_SCALE - kf.getScale()) * weight) + Vector3.UNIT_SCALE;
				if (scl != 1.0f && scale != Vector3.UnitScale)
				{
					scale = Vector3.UnitScale + (scale - Vector3.UnitScale) * scl;
				}
				node.Scale(scale);
			} 
			else 
			{
				// apply using weighted transform method
				Vector3 scale = kf.Scale;
				if (scl != 1.0f && scale != Vector3.UnitScale)
				{
					scale = Vector3.UnitScale + (scale - Vector3.UnitScale) * scl;
				}
				node.WeightedTransform(weight, kf.Translate * scl, kf.Rotation,scale);
			}
		}

        /// <summary>
        ///		Removes all key frames from this animation track.
        /// </summary>
        public void RemoveAllKeyFrames()
        {
            keyFrameList.Clear();

            // ensure a spline rebuild takes place
            OnKeyFrameDataChanged();
        }

        /// <summary>
        ///		Removes the keyframe at the specified index.
        /// </summary>
        /// <param name="index">Index of the keyframe to remove from this track.</param>
        public void RemoveKeyFrame( int index )
        {
            Debug.Assert( index < keyFrameList.Count, "Index of of bounds when removing a key frame." );

            keyFrameList.RemoveAt( index );

            // ensure a spline rebuild takes place
            OnKeyFrameDataChanged();
        }

        #endregion

        #region Protected/Internal methods

        /// <summary>
        ///		Called internally when keyframes belonging to this track are changed, in order to
        ///		trigger a rebuild of the animation splines.
        /// </summary>
        internal void OnKeyFrameDataChanged()
        {
            isSplineRebuildNeeded = true;
        }

        /// <summary>Used to rebuild the internal interpolation splines for translations, rotations, and scaling.</summary>
        protected void BuildInterpolationSplines()
        {
            // dont calculate on the fly, wait till the end when we do it manually
            positionSpline.AutoCalculate = false;
            rotationalSpline.AutoCalculate = false;
            scaleSpline.AutoCalculate = false;

            positionSpline.Clear();
            rotationalSpline.Clear();
            scaleSpline.Clear();

            // add spline control points for each keyframe in the list
            for ( int i = 0; i < keyFrameList.Count; i++ )
            {
                KeyFrame keyFrame = keyFrameList[i];

                positionSpline.AddPoint( keyFrame.Translate );
                rotationalSpline.AddPoint( keyFrame.Rotation );
                scaleSpline.AddPoint( keyFrame.Scale );
            }

            // recalculate all spline tangents now
            positionSpline.RecalculateTangents();
            rotationalSpline.RecalculateTangents();
            scaleSpline.RecalculateTangents();

            isSplineRebuildNeeded = false;
        }

        #endregion
    }
}
