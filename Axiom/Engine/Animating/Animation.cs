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

namespace Axiom.Animating
{
	/// <summary>
	///		An animation sequence. 
	/// </summary>
	/// <remarks>
	///		This class defines the interface for a sequence of animation, whether that
	///		be animation of a mesh, a path along a spline, or possibly more than one
	///		type of animation in one. An animation is made up of many 'tracks', which are
	///		the more specific types of animation.
	///		<p/>
	///		You should not create these animations directly. They will be created via a parent
    ///		object which owns the animation, e.g. Skeleton, SceneManager, etc.
	/// </remarks>
	public class Animation
	{
		#region Member variables
		
		/// <summary>Name of this animation.</summary>
		protected String name;
		/// <summary>The total length of this animation (sum of the tracks).</summary>
		protected float length;
		/// <summary>Collection of AnimationTracks.</summary>
		protected AnimationTrackCollection trackList;
		/// <summary>Interpolation mode of this animation.</summary>
		protected InterpolationMode interpolationMode;
		/// <summary>Default interpolation mode of any animations.</summary>
		static protected InterpolationMode defaultInterpolationMode;

		#endregion

		#region Constructors

		/// <summary>Static constructor.</summary>
		static Animation()
		{
			// set default interpolation mode to Spline (mmm....spline)
			defaultInterpolationMode = InterpolationMode.Spline;
		}

		/// <summary>
		///		Internal constructor, to prevent from using new outside of the engine.
		///		<p/>
		///		Animations should be created within objects that can own them (skeletons, scene managers, etc).
		/// </summary>
		internal Animation(String name, float length)
		{
			this.name = name;
			this.length = length;

			// use the default interpolation mode
			this.interpolationMode = Animation.DefaultInterpolationMode;

			// initialize the collection
			this.trackList = new AnimationTrackCollection();
		}

		#endregion

		#region Properties

		/// <summary>
		///		Gets the name of this animation.
		/// </summary>
		public String Name
		{
			get { return name; }
		}

		/// <summary>
		///		Gets the total length of this animation.
		/// </summary>
		public float Length
		{
			get { return length; }
		}

		/// <summary>
		///		Gets/Sets the current interpolation mode for this animation.
		/// </summary>
		public InterpolationMode InterpolationMode
		{
			get { return interpolationMode; }
			set { interpolationMode = value; }
		}

		/// <summary>
		///		A collection of the tracks in this animation.
		/// </summary>
		// TODO: See if we can ensure that the track list is not modified somehow.
		public AnimationTrackCollection Tracks
		{
			get { return trackList; }
		}

		/// <summary>
		///		Gets/Sets the default interpolation mode to be used for all animations.
		/// </summary>
		static public InterpolationMode DefaultInterpolationMode
		{
			get { return defaultInterpolationMode; }
			set { defaultInterpolationMode = value; }
		}

		#endregion

		#region Public methods

		/// <summary>
		///		Creates an AnimationTrack. 
		/// </summary>
		/// <param name="index">Numeric handle to give the track, used for accessing the track later.</param>
		/// <returns></returns>
		public AnimationTrack CreateTrack(short index)
		{
			AnimationTrack track = new AnimationTrack(this);
			track.Index = index;

			// add the track to the list
			trackList.Add(track);

			return track;
		}

		/// <summary>
		///		Creates a new AnimationTrack automatically associated with a Node. 
		/// </summary>
		/// <param name="index">Numeric handle to give the track, used for accessing the track later.</param>
		/// <param name="target">Node object which will be affected by this track.</param>
		/// <returns></returns>
		public AnimationTrack CreateTrack(short index, Node target)
		{
			// create a new track and set it's target
			AnimationTrack track = CreateTrack(index);
			track.TargetNode = target;
			track.Index = index;

			return track;
		}

		/// <summary>
		///		Applies an animation given a specific time point and weight.
		/// </summary>
		/// <remarks>
		///		Where you have associated animation tracks with Node objects, you can eaily apply
		///		an animation to those nodes by calling this method.
		/// </remarks>
		/// <param name="time">The time position in the animation to apply.</param>
		/// <param name="weight">The influence to give to this track, 1.0 for full influence, less to blend with
		///		other animations.</param>
		/// <param name="accumulate"></param>
		public void Apply(float time, float weight, bool accumulate)
		{
			// loop through tracks and update them all with current time
			for(int i = 0; i < trackList.Count; i++)
				trackList[i].Apply(time, weight, accumulate);
		}

		#endregion

		#region Event handlers

		private void TrackAdded(object source, System.EventArgs e)
		{
			
		}

		private void TracksCleared(object source, System.EventArgs e)
		{
			// clear the tangents list when the points are cleared
			//tangentList.Clear();
		}

		#endregion
	}
}
