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

#endregion Namespace Declarations

#region Ogre Synchronization Information

// <ogresynchronization>
//     <file name="AnimationState.h"   revision="1.16" lastUpdated="10/15/2005" lastUpdatedBy="DanielH" />
//     <file name="AnimationState.cpp" revision="1.17" lastUpdated="10/15/2005" lastUpdatedBy="DanielH" />
// </ogresynchronization>

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
		protected float inverseLength;

		/// <summary></summary>
		protected bool isEnabled;

		/// <summary></summary>
		protected float length;

		protected bool loop;

		/// <summary></summary>
		protected AnimationStateSet parent;

		/// <summary></summary>
		protected float time;

		/// <summary></summary>
		protected float weight;

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
			IsEnabled = isEnabled;

			// set using the Length property
			Length = length;
			this.loop = true;

			parent.NotifyDirty();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="animationName"></param>
		/// <param name="animationStates">The animation state set parent</param>
		/// <param name="time"></param>
		/// <param name="length"></param>
		public AnimationState( string animationName, AnimationStateSet animationStates, float time, float length )
			: this( animationName, animationStates, time, length, 1.0f, false ) { }

		/// <summary>
		///     The moral equivalent of a copy constructor
		/// </summary>
		/// <param name="parent">The animation state set parent</param>
		/// <param name="source">An animation state to copy from</param>
		public AnimationState( AnimationStateSet parent, AnimationState source )
		{
			this.parent = parent;
			CopyFrom( source );

			parent.NotifyDirty();
		}

		#endregion

		#region Properties

		/// <summary>
		///		Gets the name of the animation to which this state applies
		/// </summary>
		public string Name
		{
			get
			{
				return this.animationName;
			}
			set
			{
				this.animationName = value;
			}
		}

		/// <summary>
		///		Gets/Sets the time position for this animation.
		/// </summary>
		public float Time
		{
			get
			{
				return this.time;
			}
			set
			{
				this.time = value;
				if ( this.loop )
				{
					// Wrap
					this.time = (float)System.Math.IEEERemainder( this.time, this.length );
					if ( this.time < 0 )
					{
						this.time += this.length;
					}
				}
				else
				{
					// Clamp
					if ( this.time < 0 )
					{
						this.time = 0;
					}
					else if ( this.time > this.length )
					{
						this.time = this.length;
					}
				}
			}
		}

		/// <summary>
		///		Gets/Sets the total length of this animation (may be shorter than whole animation)
		/// </summary>
		public float Length
		{
			get
			{
				return this.length;
			}
			set
			{
				this.length = value;

				// update the inverse length of the animation
				if ( this.length != 0 )
				{
					this.inverseLength = 1.0f / this.length;
				}
				else
				{
					this.inverseLength = 0.0f;
				}
			}
		}

		/// <summary>
		/// Gets/Sets the weight (influence) of this animation
		/// </summary>
		public float Weight
		{
			get
			{
				return this.weight;
			}
			set
			{
				this.weight = value;
			}
		}

		/// <summary>
		///		Gets/Sets whether this animation is enabled or not.
		/// </summary>
		public bool IsEnabled
		{
			get
			{
				return this.isEnabled;
			}
			set
			{
				this.isEnabled = value;
				this.parent.NotifyAnimationStateEnabled( this, this.isEnabled );
			}
		}

		public bool Loop
		{
			get
			{
				return this.loop;
			}
			set
			{
				this.loop = value;
			}
		}

		/// <summary>
		///		Gets/Sets the animation state set owning this animation
		/// </summary>
		public AnimationStateSet Parent
		{
			get
			{
				return this.parent;
			}
			set
			{
				this.parent = value;
			}
		}

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
			source.isEnabled = this.isEnabled;
			source.inverseLength = this.inverseLength;
			source.length = this.length;
			source.time = this.time;
			source.weight = this.weight;
			source.loop = this.loop;
			this.parent.NotifyDirty();
		}

		#endregion

		#region Implementation of IControllerValue

		/// <summary>
		///		Gets/Sets the value to be used in a ControllerFunction.
		/// </summary>
		public float Value
		{
			get
			{
				return this.time * this.inverseLength;
			}
			set
			{
				this.time = value * this.length;
			}
		}

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
			if ( ReferenceEquals( left, null ) && ReferenceEquals( right, null ) )
			{
				return true;
			}
			if ( ReferenceEquals( left, null ) || ReferenceEquals( right, null ) )
			{
				return false;
			}
			if ( left.animationName == right.animationName && left.isEnabled == right.isEnabled && left.time == right.time && left.weight == right.weight && left.length == right.length && left.loop == right.loop )
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
			return this.animationName.GetHashCode();
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
			var other = obj as AnimationState;

			if ( this.animationName == other.animationName && this.isEnabled == other.isEnabled && this.time == other.time && this.weight == other.weight && this.length == other.length )
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
