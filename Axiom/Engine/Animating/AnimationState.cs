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

using Axiom.Core;
using Axiom.Controllers;

namespace Axiom.Animating
{
	/// <summary>
	///		Represents the state of an animation and the weight of it's influence. 
	/// </summary>
	/// <remarks>
	///		Other classes can hold instances of this class to store the state of any animations
	///		they are using.
	///		This class implements the IControllerValue interface to enable automatic update of
	///		animation state through controllers.
	/// </remarks>
	public class AnimationState : IControllerValue
	{
		#region Member variables

		/// <summary>Name of this animation track.</summary>
		protected String animationName;
		/// <summary></summary>
		protected float time;
		/// <summary></summary>
		protected float length;
		/// <summary></summary>
		protected float inverseLength;
		/// <summary></summary>
		protected float weight;
		/// <summary></summary>
		protected bool isEnabled;

		#endregion

		#region Constructors

		/// <summary>
		///		
		/// </summary>
		/// <param name="animationName"></param>
		/// <param name="time"></param>
		/// <param name="length"></param>
		/// <param name="weight"></param>
		/// <param name="isEnabled"></param>
		internal AnimationState(string animationName, float time, float length, float weight, bool isEnabled)
		{
			this.animationName = animationName;
			this.time = time;
			this.weight = weight;
			this.isEnabled = isEnabled;

			// set using the Length property
			this.Length = length;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="animationName"></param>
		/// <param name="time"></param>
		/// <param name="length"></param>
		internal AnimationState(string animationName, float time, float length)
		{
			this.animationName = animationName;
			this.time = time;
			this.length = length;
			this.weight = 1.0f;
			this.isEnabled = false;
		}

		#endregion

		#region Properties

		/// <summary>
		///		Gets the name of the animation to which this state applies
		/// </summary>
		public String Name
		{
			get { return animationName; }
			set { animationName = value; }
		}

		/// <summary>
		///		Gets/Sets the time position for this animation.
		/// </summary>
		public float Time
		{
			get { return time; }
			set { time = value; }
		}

		/// <summary>
		///		Gets/Sets the total length of this animation (may be shorter than whole animation)
		/// </summary>
		public float Length
		{
			get { return length; }
			set 
			{ 
				length = value; 

				// update the inverse length of the animation
				if(length != 0)
					inverseLength = 1.0f / length;
				else
					inverseLength = 0.0f;
			}
		}

		/// <summary>
		/// Gets/Sets the weight (influence) of this animation
		/// </summary>
		public float Weight
		{
			get { return weight; }
			set { weight = value; }
		}

		/// <summary>
		///		Gets/Sets whether this animation is enabled or not.
		/// </summary>
		// TODO: Do something with this value, like *stop* the animation when set to false.
		public bool IsEnabled
		{
			get { return isEnabled; }
			set { isEnabled = value; }
		}

		#endregion

		#region Public methods

		/// <summary>
		///		Modifies the time position, adjusting for animation length.
		/// </summary>
		/// <param name="offset"></param>
		public void AddTime(float offset)
		{
			time = time + offset;

			// Wrap over upper bound
			while(time >= length)
				time -= length;

			// Wrap over lower bound
			while(time < 0)
				time += length;
		}

		#endregion

		#region Operator overloads

		/// <summary>
		///		Compares 2 animation states for equality.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		static public bool operator == (AnimationState left, AnimationState right)
		{
			if (left.animationName == right.animationName &&
				left.isEnabled == right.isEnabled &&
				left.time == right.time &&
				left.weight == right.weight &&
				left.length == right.length)
			{
				return true;
			}
			else
			{
				return false;
			}
		}

		/// <summary>
		///		Compares 2 animation states for inequality.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		static public bool operator != (AnimationState left, AnimationState right)
		{
			return !(left == right);
		}

		#endregion

		#region Implementation of IControllerValue

		/// <summary>
		///		Gets/Sets the value to be used in a ControllerFunction.
		/// </summary>
		public float Value
		{
			get { return time * inverseLength;	}
			set {	time = value * length; }
		}
		#endregion
	}
}
