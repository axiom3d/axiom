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
using Axiom.MathLib;

namespace Axiom.Animating
{
	/// <summary>
	/// A key frame in an animation sequence defined by an AnimationTrack.
	/// </summary>
	///	<remarks>
	///	This class can be used as a basis for all kinds of key frames. 
	///	The unifying principle is that multiple KeyFrames define an 
	///	animation sequence, with the exact state of the animation being an 
	///	interpolation between these key frames.
	/// </remarks>

	public class KeyFrame
	{
		#region Protected member variables

		protected float time;
		protected Vector3 translate;
		protected Vector3 scale;
		protected Quaternion rotation;

		#endregion

		#region Constructors

		/// <summary>
		/// Creates a new keyframe starting at time 0.
		/// </summary>
		public KeyFrame()
		{
			this.time = 0.0f;
			this.translate = new Vector3();
			this.scale = Vector3.UnitScale;
			this.rotation = Quaternion.Identity;
		}

		/// <summary>
		/// Creates a new keyframe with the specified time.  
		/// Should really be created by AnimationTrack.CreateKeyFrame() instead.
		/// </summary>
		/// <param name="pTime"></param>
		public KeyFrame(float time)
		{
			this.time = time;
			this.translate = new Vector3();
			this.scale = Vector3.UnitScale;
			this.rotation = Quaternion.Identity;
		}

		#endregion

		#region Public properties

		/// <summary>
		/// Sets the rotation applied by this keyframe.
		///	Use Quaternion methods to convert from angle/axis or Matrix3 if
		///	you don't like using Quaternions directly.
		/// </summary>
		public Quaternion Rotation
		{
			get { return this.rotation; }
			set { this.rotation = value; }
		}
		/// <summary>
		/// Sets the scaling factor applied by this keyframe to the animable
		///	object at it's time index.
		///	beware of supplying zero values for any component of this
		///	vector, it will scale the object to zero dimensions.
		/// </summary>
		public Vector3 Scale
		{
			get { return this.scale; }
			set { this.scale = value; }
		}

		/// <summary>
		/// Sets the translation associated with this keyframe. 
		/// </summary>
		/// <remarks>
		///	The translation factor affects how much the keyframe translates (moves) it's animable
		///	object at it's time index.
		///	</remarks>
		public Vector3 Translate
		{
			get { return this.translate; }
			set { this.translate = value; }
		}

		/// <summary>
		/// Gets the time of this keyframe in the animation sequence.
		/// </summary>
		public float Time
		{
			get { return this.time; }
		}

		#endregion
	}
}
