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
using Axiom.Collections;
using Axiom.Core;
using Axiom.Enumerations;
using Axiom.MathLib;

namespace Axiom.Animating
{
	/// <summary>
	///		A 'track' in an animation sequence, ie a sequence of keyframes which affect a
    ///		certain type of object that can be animated.
	/// </summary>
	/// <remarks>
	///		This class is intended as a base for more complete classes which will actually
	///		animate specific types of object, e.g. a bone in a skeleton to affect
	///		skeletal animation. An animation will likely include multiple tracks each of which
	///		can be made up of many KeyFrame instances. Note that the use of tracks allows each animable
	///		object to have it's own number of keyframes, i.e. you do not have to have the
	///		maximum number of keyframes for all animable objects just to cope with the most
	///		animated one.
	///		<p/>
	///		Since the most common animable object is a Node, there are options in this class for associating
	///		the track with a Node which will receive keyframe updates automatically when the 'apply' method
	///		is called.
	/// </remarks>
	public class AnimationTrack
	{
		#region Member variables

		/// <summary>Index of this animation track.</summary>
		protected short index;
		/// <summary>Animation that owns this track.</summary>
		protected Animation parent;
		/// <summary>Target node to be animated.</summary>
		protected Node target;
		/// <summary>Maximum keyframe time.</summary>
		protected float maxKeyFrameTime;
		/// <summary>Collection of key frames in this track.</summary>
		protected KeyFrameCollection keyFrameList;
		/// <summary>Flag indicating we need to rebuild the splines next time.</summary>
		protected bool isSplineRebuildNeeded;
		/// <summary>Spline for position interpolation.</summary>
		protected PositionalSpline positionSpline;
		/// <summary>Spline for scale interpolation.</summary>
		protected PositionalSpline scaleSpline;
		/// <summary>Spline for rotation interpolation.</summary>
		protected RotationalSpline rotationSpline;

		#endregion

		#region Constructors

		/// <summary>
		///		Internal constructor, to prevent direction instantiation.  Should be created
		///		via a call to the CreateTrack method of an Animation.
		/// </summary>
		internal AnimationTrack(Animation parent)
		{
			this.parent = parent;
			keyFrameList = new KeyFrameCollection();
			this.maxKeyFrameTime = -1;

			positionSpline = new PositionalSpline();
			scaleSpline = new PositionalSpline();
			rotationSpline = new RotationalSpline();
		}

		/// <summary>
		///		Internal constructor, to prevent direction instantiation.  Should be created
		///		via a call to the CreateTrack method of an Animation.
		/// </summary>
		internal AnimationTrack(Animation parent, Node target)
		{
			this.parent = parent;
			this.target = target;
			keyFrameList = new KeyFrameCollection();
			this.maxKeyFrameTime = -1;

			positionSpline = new PositionalSpline();
			scaleSpline = new PositionalSpline();
			rotationSpline = new RotationalSpline();
		}

		#endregion

		#region Properties

		/// <summary>
		///		The name of this animation track.
		/// </summary>
		public short Index
		{
			get { return index; }
			set { index = value; }
		}

		/// <summary>
		///		Collection of the KeyFrames present in this AnimationTrack.
		/// </summary>
		public KeyFrameCollection KeyFrames
		{
			get { return keyFrameList; }
		}

		/// <summary>
		///		Gets/Sets the target node that this track is associated with.
		/// </summary>
		public Node TargetNode
		{
			get { return target; }
			set { target = value; }
		}

		#endregion

		#region Public methods

		/// <summary>
		///		Creates a new KeyFrame and adds it to this animation at the given time index.
		/// </summary>
		/// <remarks>
		///		It is better to create KeyFrames in time order. Creating them out of order can result 
		///		in expensive reordering processing. Note that a KeyFrame at time index 0.0 is always created
		///		for you, so you don't need to create this one, just access it using KeyFrames[0];
		/// </remarks>
		/// <param name="time"></param>
		/// <returns></returns>
		public KeyFrame CreateKeyFrame(float time)
		{
			KeyFrame keyFrame = new KeyFrame(time);

			// adds sorted by time
			keyFrameList.Add(keyFrame.Time, keyFrame);

			// update max keyframe time if necessary
			if(time > maxKeyFrameTime)
				maxKeyFrameTime = time;

			isSplineRebuildNeeded = true;

			return keyFrame;
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
		public KeyFrame GetInterpolatedKeyFrame(float time)
		{
			KeyFrame result = new KeyFrame(time);

			KeyFrame k1, k2;
			ushort firstKeyIndex;

			float t = this.GetKeyFramesAtTime(time, out k1, out k2, out firstKeyIndex);

			if(t == 0.0f)
			{
				// just use k1
				result.Rotation = k1.Rotation;
				result.Translate = k1.Translate;
				result.Scale = k1.Scale;
			}
			else
			{
				// interpolate by t
				InterpolationMode mode = parent.InterpolationMode;

				switch(mode)
				{
					case InterpolationMode.Linear:
					{
						// linear interoplation
						result.Rotation = Quaternion.Slerp(t, k1.Rotation, k2.Rotation);
						result.Translate = k1.Translate + ((k2.Translate - k1.Translate) * t);
						result.Scale = k1.Scale + ((k2.Scale - k1.Scale) * t);

					}	break;
					case InterpolationMode.Spline:
					{
						// spline interpolation
						if(isSplineRebuildNeeded)
							BuildInterpolationSplines();

						result.Rotation = rotationSpline.Interpolate(firstKeyIndex, t);
						result.Translate = positionSpline.Interpolate(firstKeyIndex, t);
						result.Scale = scaleSpline.Interpolate(firstKeyIndex, t);
					}	break;

				}
			}

			// return the resulting keyframe
			return result;
		}

		/// <summary>
		///		Applies an animation track at a certain position to the target node.
		/// </summary>
		/// <remarks>
		///		When a track has bee associated with a target node, you can eaisly apply the animation
		///		to the target by calling this method.
		/// </remarks>
		/// <param name="time">The time position in the animation to apply.</param>
		/// <param name="weight">The influence to give to this track, 1.0 for full influence, less to blend with
		///		other animations.</param>
		/// <param name="accumulate"></param>
		public void Apply(float time, float weight, bool accumulate)
		{
			// call ApplyToNode with our target node
			ApplyToNode(target, time, weight, accumulate);
		}

		/// <summary>
		///		Overloaded Apply method.  
		/// </summary>
		/// <param name="time"></param>
		public void Apply(float time)
		{
			// call overloaded method
			Apply(time, 1.0f, false);
		}

		/// <summary>
		/// Sames as the Apply method, but applies to a specified Node instead of it's associated node.
		/// </summary>
		/// <param name="node"></param>
		/// <param name="time"></param>
		/// <param name="weight"></param>
		/// <param name="accumulate"></param>
		public void ApplyToNode(Node node, float time, float weight, bool accumulate)
		{
			KeyFrame keyFrame = this.GetInterpolatedKeyFrame(time);

			if(accumulate)
			{
				// add to existing. Weights are not relative, but treated as absolute multipliers for the animation
				Vector3 translate = keyFrame.Translate * weight;
				node.Translate(translate);

				// interpolate between not rotation and full rotation, to point weight, so 0 = no rotate, and 1 = full rotation
				Quaternion rotate = Quaternion.Slerp(weight, Quaternion.Identity, keyFrame.Rotation);
				node.Rotate(rotate);

				// TODO: not yet sure how to modify scale for cumulative animations
				Vector3 scale = keyFrame.Scale;
				node.Scale(scale);
			}
			else
			{
				// apply using weighted transform method
				node.WeightedTransform(weight, keyFrame.Translate, keyFrame.Rotation, keyFrame.Scale);
			}
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
		public float GetKeyFramesAtTime(float time, out KeyFrame keyFrame1, out KeyFrame keyFrame2, out ushort firstKeyIndex)
		{
			short firstIndex = -1;
			float totalLength = parent.Length;

			// wrap time
			while(time > totalLength)
				time -= totalLength;

			int i = 0;

			// makes compiler happy so it wont complain about this var being unassigned
			keyFrame1 = null;

			// find the last keyframe before or on current time
			for(i = 0; i < keyFrameList.Count; i++)
			{
				KeyFrame keyFrame = keyFrameList[i];

				// kick out now if the current frames time is greater than the current time
				if(keyFrame.Time > time)
					break;

				keyFrame1 = keyFrame;
				++firstIndex;
			}

			// trap case where there is no key before this time
			// use the first key anyway and pretend it's time index 0
			if(firstIndex == -1)
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
			// TODO: Verify logic
			if(firstIndex == (keyFrameList.Count - 1))
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

			if(t1 == t2)
			{
				// same keyframe
				return 0.0f;
			}
			else
			{
				return (time - t1) / (t2 - t1);
			}
		}

		#endregion

		#region Protected methods

		/// <summary>Used to rebuild the internal interpolation splines for translations, rotations, and scaling.</summary>
		protected void BuildInterpolationSplines()
		{
			// dont calculate on the fly, wait till the end when we do it manually
			positionSpline.AutoCalculate = false;
			rotationSpline.AutoCalculate = false;
			scaleSpline.AutoCalculate = false;

			positionSpline.Points.Clear();
			rotationSpline.Points.Clear();
			scaleSpline.Points.Clear();

			// add spline control points for each keyframe in the list
			for(int i = 0; i < keyFrameList.Count; i++)
			{
				KeyFrame keyFrame = keyFrameList[i];

				positionSpline.Points.Add(keyFrame.Translate);
				rotationSpline.Points.Add(keyFrame.Rotation);
				scaleSpline.Points.Add(keyFrame.Scale);
			}

			// recalculate all spline tangents now
			positionSpline.RecalculateTangents();
			rotationSpline.RecalculateTangents();
			scaleSpline.RecalculateTangents();
		}

		#endregion
	}
}
