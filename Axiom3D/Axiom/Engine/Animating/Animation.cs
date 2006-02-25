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

using Axiom.Collections;
using Axiom.Core;

#endregion

#region Versioning Information
/// File								Revision
/// ===============================================
/// OgreAnimation.h		                
/// OgreAnimation.cpp		            
/// 
#endregion


namespace Axiom.Animating
{
    /// <summary>
    ///		Types of interpolation used in animation.
    /// </summary>
    public enum InterpolationMode
    {
        /// <summary>
        ///		More robotic movement, not as realistic.
        ///	 </summary>
        Linear,

        /// <summary>
        ///		Smooth movement between keyframes.
        ///	 </summary>
        Spline
    }

    /// <summary>
    /// The types of rotational interpolation available.
    /// </summary>
    public enum RotationInterpolationMode
    {
        /// <summary>
        /// Values are interpolated linearly. This is faster but does not 
        ///    necessarily give a completely accurate result.
        /// </summary>
        Linear,

        /// <summary>
        ///  Values are interpolated spherically. This is more accurate but
        ///    has a higher cost.
        /// </summary>
        Spherical
    };

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
        protected string name;
        /// <summary>The total length of this animation (sum of the tracks).</summary>
        protected float length;
        /// <summary>Collection of AnimationTracks.</summary>
        protected AnimationTrackCollection trackList;
        /// <summary>Interpolation mode of this animation.</summary>
        protected InterpolationMode interpolationMode;
        /// <summary>Rotation Interpolation mode of this animation.</summary>
        protected RotationInterpolationMode rotationInterpolationMode;

        /// <summary>Default interpolation mode of any animations.</summary>
        static protected InterpolationMode defaultInterpolationMode;
        /// <summary>Default rotation interpolation mode of any animations.</summary>
        static protected RotationInterpolationMode defaultRotationInterpolationMode;

        #endregion

        #region Constructors

        /// <summary>Static constructor.</summary>
        static Animation()
        {
            // set default interpolation mode to Spline (mmm....spline)
            defaultInterpolationMode = InterpolationMode.Linear;
            defaultRotationInterpolationMode = RotationInterpolationMode.Linear;
        }

        /// <summary>
        ///		Internal constructor, to prevent from using new outside of the engine.
        ///		<p/>
        ///		Animations should be created within objects that can own them (skeletons, scene managers, etc).
        /// </summary>
        internal Animation( string name, float length )
        {
            this.name = name;
            this.length = length;

            // use the default interpolation mode
            this.interpolationMode = Animation.DefaultInterpolationMode;

            // use the default rotation interpolation mode
            this.rotationInterpolationMode = Animation.DefaultRotationInterpolationMode;

            // initialize the collection
            this.trackList = new AnimationTrackCollection();
        }

        #endregion

        #region Properties


        /// <summary>
        ///		Gets the name of this animation.
        /// </summary>
        public string Name
        {
            get
            {
                return name;
            }
        }

        /// <summary>
        ///		Gets the total length of this animation.
        /// </summary>
        public float Length
        {
            get
            {
                return length;
            }
        }

        /// <summary>
        ///		Gets/Sets the current interpolation mode for this animation.
        /// </summary>
        public InterpolationMode InterpolationMode
        {
            get
            {
                return interpolationMode;
            }
            set
            {
                interpolationMode = value;
            }
        }

        /// <summary>
        ///		A collection of the tracks in this animation.
        /// </summary>
        // TODO See if we can ensure that the track list is not modified somehow.
        public AnimationTrackCollection Tracks
        {
            get
            {
                return trackList;
            }
        }

        /// <summary>
        ///		Gets/Sets the default interpolation mode to be used for all animations.
        /// </summary>
        public static InterpolationMode DefaultInterpolationMode
        {
            get
            {
                return defaultInterpolationMode;
            }
            set
            {
                defaultInterpolationMode = value;
            }
        }

        /// <summary>
        ///		Gets/Sets the default rotation interpolation mode to be used for all animations.
        /// </summary>
        public static RotationInterpolationMode DefaultRotationInterpolationMode
        {
            get
            {
                return defaultRotationInterpolationMode;
            }
            set
            {
                defaultRotationInterpolationMode = value;
            }
        }

        #endregion

        #region Public methods

        /// <summary>
        ///		Creates an AnimationTrack. 
        /// </summary>
        /// <param name="handle">Numeric handle to give the track, used for accessing the track later.</param>
        /// <returns></returns>
        public AnimationTrack CreateTrack( short handle )
        {
            AnimationTrack track = new AnimationTrack( this );
            track.Handle = handle;

            // add the track to the list
            trackList.Add( track );

            return track;
        }

        /// <summary>
        ///		Creates a new AnimationTrack automatically associated with a Node. 
        /// </summary>
        /// <param name="index">Numeric handle to give the track, used for accessing the track later.</param>
        /// <param name="target">Node object which will be affected by this track.</param>
        /// <returns></returns>
        public AnimationTrack CreateTrack( short handle, Node target )
        {
            // create a new track and set it's target
            AnimationTrack track = CreateTrack( handle );
            track.AssociatedNode = target;
            track.Handle = handle;

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
        public void Apply( float time, float weight, bool accumulate )
        {
            Apply( time, weight, accumulate, 1.0F, false );
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
        public void Apply( float time, float weight, bool accumulate, float scale, bool lookInDirectionOfTranslation )
        {
            // loop through tracks and update them all with current time
            foreach ( AnimationTrack track in trackList )
            {
                track.Apply( time, weight, accumulate, scale, lookInDirectionOfTranslation );
            }
            //for ( int i = 0; i < trackList.Count; i++ )
            //{
            //    trackList[i].Apply( time, weight, accumulate, scale, lookInDirectionOfTranslation );
            //}
        }

        public void Apply( Skeleton skeleton, float time, float weight, bool accumulate, float scale )
        {
            // loop through tracks and update them all with current time
            foreach ( AnimationTrack track in trackList )
            {
                Bone bone = skeleton.GetBone( (ushort)track.Handle );
                track.ApplyToNode( bone, time, weight, accumulate, scale, false );
            }

            //for ( int i = 0; i < trackList.Count; i++ )
            //{
            //    AnimationTrack track = trackList[i];
            //    Bone bone = skeleton.GetBone( (ushort)track.Handle );
            //    track.ApplyToNode( bone, time, weight, accumulate, scale, false );
            //}
        }

        #endregion

        #region Event handlers

        private void TrackAdded( object source, System.EventArgs e )
        {

        }

        private void TracksCleared( object source, System.EventArgs e )
        {
            // clear the tangents list when the points are cleared
            //tangentList.Clear();
        }

        #endregion
    }
}
