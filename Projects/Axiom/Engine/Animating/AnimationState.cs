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

#endregion

#region SVN Version Information

// <file>
//     <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;

using Axiom.Controllers;
using Axiom.Collections;

#endregion Namespace Declarations

#region Ogre Synchronization Information

/// <ogresynchronization>
///     <file name="AnimationState.h"   revision="1.16" lastUpdated="10/15/2005" lastUpdatedBy="DanielH" />
///     <file name="AnimationState.cpp" revision="1.17" lastUpdated="10/15/2005" lastUpdatedBy="DanielH" />
/// </ogresynchronization>

#endregion

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
	public class AnimationState : IControllerValue<float>, IComparable
	{
		#region Member variables

		/// <summary>Name of this animation track.</summary>
		protected string animationName;

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

		/// <summary></summary>
		protected AnimationStateSet parent;

		protected bool loop;

		#endregion

		#region Constructors

		/// <summary>
		///		
		/// </summary>
		/// <param name="animationName"></param>
		/// <param name="parent">The animation state set parent</param>
		/// <param name="time"></param>
		/// <param name="length"></param>
		/// <param name="weight"></param>
		/// <param name="isEnabled"></param>
		public AnimationState( string animationName, AnimationStateSet parent, float time, float length, float weight, bool isEnabled )
		{
			this.animationName = animationName;
			this.parent = parent;
			this.time = time;
			this.weight = weight;

			// Set using Property
			this.IsEnabled = isEnabled;

			// set using the Length property
			this.Length = length;
			this.loop = true;

			parent.NotifyDirty();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="animationName"></param>
		/// <param name="parent">The animation state set parent</param>
		/// <param name="time"></param>
		/// <param name="length"></param>
		public AnimationState( string animationName, AnimationStateSet animationStates, float time, float length )
			: this( animationName, animationStates, time, length, 1.0f, false ) {}

		/// <summary>
		///     The moral equivalent of a copy constructor
		/// </summary>
		/// <param name="parent">The animation state set parent</param>
		/// <param name="source">An animation state to copy from</param>
		public AnimationState( AnimationStateSet parent, AnimationState source )
		{
			this.parent = parent;
			this.CopyFrom( source );

			parent.NotifyDirty();
		}

		#endregion

		#region Properties

		/// <summary>
		///		Gets the name of the animation to which this state applies
		/// </summary>
		public string Name { get { return animationName; } set { animationName = value; } }

		/// <summary>
		///		Gets/Sets the time position for this animation.
		/// </summary>
		public float Time
		{
			get { return time; }
			set
			{
				time = value;
				if( loop )
				{
					// Wrap
					time = (float)System.Math.IEEERemainder( time, length );
					if( time < 0 )
					{
						time += length;
					}
				}
				else
				{
					// Clamp
					if( time < 0 )
					{
						time = 0;
					}
					else if( time > length )
					{
						time = length;
					}
				}
			}
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
				if( length != 0 )
				{
					inverseLength = 1.0f / length;
				}
				else
				{
					inverseLength = 0.0f;
				}
			}
		}

		/// <summary>
		/// Gets/Sets the weight (influence) of this animation
		/// </summary>
		public float Weight { get { return weight; } set { weight = value; } }

		/// <summary>
		///		Gets/Sets whether this animation is enabled or not.
		/// </summary>
		public bool IsEnabled
		{
			get { return isEnabled; }
			set
			{
				isEnabled = value;
				parent.NotifyAnimationStateEnabled( this, isEnabled );
			}
		}

		public bool Loop { get { return loop; } set { loop = value; } }

		/// <summary>
		///		Gets/Sets the animation state set owning this animation
		/// </summary>
		public AnimationStateSet Parent { get { return parent; } set { parent = value; } }

		#endregion

		#region Public methods

		/// <summary>
		///		Modifies the time position, adjusting for animation length.
		/// </summary>
		/// <param name="offset">Offset from the current time position.</param>
		public void AddTime( float offset )
		{
			Time += offset;
		}

		/// <summary>
		/// Copies the states from another animation state, preserving the animation name
		/// (unlike CopyTo) but copying everything else.
		/// </summary>
		/// <param name="source">animation state which will use as source.</param>
		public void CopyFrom( AnimationState source )
		{
			source.isEnabled = isEnabled;
			source.inverseLength = inverseLength;
			source.length = length;
			source.time = time;
			source.weight = weight;
			source.loop = loop;
			parent.NotifyDirty();
		}

		#endregion

		#region Implementation of IControllerValue

		/// <summary>
		///		Gets/Sets the value to be used in a ControllerFunction.
		/// </summary>
		public float Value { get { return time * inverseLength; } set { time = value * length; } }

		#endregion

		#region Object overloads

		public static bool operator !=( AnimationState left, AnimationState right )
		{
			return !( left == right );
		}

		public override bool Equals( object obj )
		{
			return obj is AnimationState && this == (AnimationState)obj;
		}

		public static bool operator ==( AnimationState left, AnimationState right )
		{
			if( object.ReferenceEquals( left, null ) && object.ReferenceEquals( right, null ) )
			{
				return true;
			}
			if( object.ReferenceEquals( left, null ) || object.ReferenceEquals( right, null ) )
			{
				return false;
			}
			if( left.animationName == right.animationName &&
			    left.isEnabled == right.isEnabled &&
			    left.time == right.time &&
			    left.weight == right.weight &&
			    left.length == right.length &&
			    left.loop == right.loop )
			{
				return true;
			}
			else
			{
				return false;
			}
		}

		/// <summary>
		///    Override GetHashCode.
		/// </summary>
		/// <remarks>
		///    Done mainly to quash warnings, no real need for it.
		/// </remarks>
		/// <returns></returns>
		public override int GetHashCode()
		{
			return animationName.GetHashCode();
		}

		#endregion Object overloads

		#region IComparable Members

		/// <summary>
		///    
		/// </summary>
		/// <param name="obj"></param>
		/// <returns>0 if they are the same, -1 otherwise.</returns>
		public int CompareTo( object obj )
		{
			AnimationState other = obj as AnimationState;

			if( animationName == other.animationName &&
			    isEnabled == other.isEnabled &&
			    time == other.time &&
			    weight == other.weight &&
			    length == other.length )
			{
				return 0;
			}
			else
			{
				return -1;
			}
		}

		#endregion
	}
}
