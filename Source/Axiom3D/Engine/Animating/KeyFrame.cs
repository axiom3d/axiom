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
using Axiom.MathLib;
#region Ogre Synchronization Information
/// <ogresynchronization>
///     <file name="KeyFrame.h"   revision="1.9" lastUpdated="10/15/2005" lastUpdatedBy="DanielH" />
///     <file name="KeyFrame.cpp" revision="1.13" lastUpdated="10/15/2005" lastUpdatedBy="DanielH" />
/// </ogresynchronization>
#endregion
namespace Axiom
{
    /// <summary>
    ///		A key frame in an animation sequence defined by an AnimationTrack.
    /// </summary>
    ///	<remarks>
    ///		This class can be used as a basis for all kinds of key frames. 
    ///		The unifying principle is that multiple KeyFrames define an 
    ///		animation sequence, with the exact state of the animation being an 
    ///		interpolation between these key frames.
    /// </remarks>
    public class KeyFrame {
        #region Protected member variables

		/// <summary>
		///		Time of this keyframe.
		/// </summary>
        protected float time;
		/// <summary>
		///		Translation at this keyframe.
		/// </summary>
        protected Vector3 translate;
		/// <summary>
		///		Scale factor at this keyframe.
		/// </summary>
        protected Vector3 scale;
		/// <summary>
		///		Rotation at this keyframe.
		/// </summary>
        protected Quaternion rotation;
		/// <summary>
		///		Animation track that this key frame belongs to.
		/// </summary>
		protected AnimationTrack parentTrack;

        #endregion

        #region Constructors

        /// <summary>
        ///		Creates a new keyframe with the specified time.  
        ///		Should really be created by <see cref="AnimationTrack.CreateKeyFrame"/> instead.
        /// </summary>
        /// <param name="parent">Animation track that this keyframe belongs to.</param>
        /// <param name="time">Time at which this keyframe begins.</param>
        public KeyFrame(AnimationTrack parent, float time) {
            this.time = time;
            translate = new Vector3();
            scale = Vector3.UnitScale;
            rotation = Quaternion.Identity;
        }

        #endregion

        #region Public properties

        /// <summary>
        ///		Sets the rotation applied by this keyframe.
        ///		Use Quaternion methods to convert from angle/axis or Matrix3 if
        ///		you don't like using Quaternions directly.
        /// </summary>
        public Quaternion Rotation {
            get { 
				return rotation; 
			}
            set { 
				rotation = value;

				if(parentTrack != null) {
					parentTrack.OnKeyFrameDataChanged();
				}
			}
        }
        /// <summary>
        ///		Sets the scaling factor applied by this keyframe to the animable
        ///		object at it's time index.
        ///		beware of supplying zero values for any component of this
        ///		vector, it will scale the object to zero dimensions.
        /// </summary>
        public Vector3 Scale {
            get { 
				return scale; 
			}
            set { 
				scale = value;

				if(parentTrack != null) {
					parentTrack.OnKeyFrameDataChanged();
				}
			}
        }

        /// <summary>
        ///		Sets the translation associated with this keyframe. 
        /// </summary>
        /// <remarks>
        ///		The translation factor affects how much the keyframe translates (moves) it's animable
        ///		object at it's time index.
        ///	</remarks>
        public Vector3 Translate {
            get { 
				return translate; 
			}
            set { 
				translate = value;

				if(parentTrack != null) {
					parentTrack.OnKeyFrameDataChanged();
				}
			}
        }

        /// <summary>
        ///		Gets the time of this keyframe in the animation sequence.
        /// </summary>
        public float Time {
            get { 
				return time; 
			}
        }

        #endregion
    }
}
