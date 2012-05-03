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
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <id value="$Id$"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;
using Axiom.Core;
using Axiom.Math;

#endregion Namespace Declarations

namespace Axiom.Animating
{
	/// <summary>
	/// The type of the value being animated
	/// </summary>
	[OgreVersion( 1, 7, 2, "Original name was ValueType" )]
	public enum AnimableType
	{
		Int,
		Real,
		Vector2,
		Vector3,
		Vector4,
		Quaternion,
		ColorEx,
		Radian,
		Degree
	}

	/// <summary>
	///     Defines an object property which is animable, ie may be keyframed.
	/// </summary>
	/// <remarks>
	///     Animable properties are those which can be altered over time by a 
	///     predefined keyframe sequence. They may be set directly, or they may
	///     be modified from their existing state (common if multiple animations
	///     are expected to apply at once). Implementors of this interface are
	///     expected to override the 'setValue', 'setCurrentStateAsBaseValue' and 
	///     'ApplyDeltaValue' methods appropriate to the type in question, and to 
	///     initialise the type.
	///     
	///     AnimableValue instances are accessible through any class which extends
	///     AnimableObject in order to expose it's animable properties.
	///     
	///     This class is an instance of the Adapter pattern, since it generalises
	///     access to a particular property. Whilst it could have been templated
	///     such that the type which was being referenced was compiled in, this would
	///     make it more difficult to aggregated generically, and since animations
	///     are often comprised of multiple properties it helps to be able to deal
	///     with all values through a single class.
	///</remarks>
#if CSHARP_30
	public abstract class AnimableValue<T> where T : struct
	{
	#region Fields and Properties

		protected T _value;
		public virtual T Value
		{
			get
			{
				return _value;
			}
			set
			{
				_value = value;
			}
		}

		protected T _baseValue;
		public virtual T BaseValue
		{
			get
			{
				return _baseValue;
			}
			set
			{
				_baseValue = value;
			}
		}

		#endregion Fields and Properties

	#region Methods

		public void Reset()
		{
			_value = _baseValue;
		}

		public virtual void ApplyDelta( T delta )
		{
			throw new NotImplementedException();
		}

		public static T Interpolate( float time, T k1, T k2 ) 
		{
			return k1.Interpolate( k2, time);
		}

		public static T Multiply( T k, float v )
		{
			return k.Multiply( v );
		}

		/// Sets the current state as the 'base' value; used for delta animation
		/// Any instantiated derived class must implement this guy
		public abstract void SetCurrentStateAsBaseValue();

		#endregion Methods
	}

	public static class Math
	{
		public static T Interpolate<T,K>( this T keyStart, T keyEnd, K stepping )
		{
			return default( T );
		}

		public static T Multiply<T, K>( this T operandA, K scalar )
		{
			return default( T );
		}

	}

#endif
	public abstract class AnimableValue
	{
		protected AnimableType type;
		protected Object valueObject;

		public AnimableValue( AnimableType type )
		{
			this.type = type;

			valueObject = null;
		}

		public AnimableType Type
		{
			get
			{
				return type;
			}
			set
			{
				type = value;
			}
		}

		public Object ValueObject
		{
			get
			{
				return ValueObject;
			}
			set
			{
				ValueObject = value;
			}
		}

		/// Internal method to set a value as base
		public virtual void SetAsBaseValue( int val )
		{
			valueObject = val;
		}

		/// Internal method to set a value as base
		public virtual void SetAsBaseValue( Real val )
		{
			valueObject = val;
		}

		/// Internal method to set a value as base
		public virtual void SetAsBaseValue( Vector2 val )
		{
			valueObject = val;
		}

		/// Internal method to set a value as base
		public virtual void SetAsBaseValue( Vector3 val )
		{
			valueObject = val;
		}

		/// Internal method to set a value as base
		public virtual void SetAsBaseValue( Vector4 val )
		{
			valueObject = val;
		}

		/// Internal method to set a value as base
		public virtual void SetAsBaseValue( Quaternion val )
		{
			valueObject = val;
		}

		/// Internal method to set a value as base
		public virtual void SetAsBaseValue( ColorEx val )
		{
			valueObject = val.Clone();
		}

		private void SetAsBaseValue( Object val )
		{
			switch ( type )
			{
				case AnimableType.Int:
					SetAsBaseValue( (int)val );
					break;
				case AnimableType.Real:
					SetAsBaseValue( (Real)val );
					break;
				case AnimableType.Vector2:
					SetAsBaseValue( (Vector2)val );
					break;
				case AnimableType.Vector3:
					SetAsBaseValue( (Vector3)val );
					break;
				case AnimableType.Vector4:
					SetAsBaseValue( (Vector4)val );
					break;
				case AnimableType.Quaternion:
					SetAsBaseValue( (Quaternion)val );
					break;
				case AnimableType.ColorEx:
					SetAsBaseValue( (ColorEx)val );
					break;
			}
		}

		public void ResetToBaseValue()
		{
			switch ( type )
			{
				case AnimableType.Int:
					SetValue( (int)valueObject );
					break;
				case AnimableType.Real:
					SetValue( (Real)valueObject );
					break;
				case AnimableType.Vector2:
					SetValue( (Vector2)valueObject );
					break;
				case AnimableType.Vector3:
					SetValue( (Vector3)valueObject );
					break;
				case AnimableType.Vector4:
					SetValue( (Vector4)valueObject );
					break;
				case AnimableType.Quaternion:
					SetValue( (Quaternion)valueObject );
					break;
				case AnimableType.ColorEx:
					SetValue( (ColorEx)valueObject );
					break;
			}
		}

		/// Set value 
		public virtual void SetValue( int val )
		{
			throw new AxiomException( "Animable SetValue to int not implemented" );
		}

		/// Set value 
		public virtual void SetValue( Real val )
		{
			throw new AxiomException( "Animable SetValue to float not implemented" );
		}

		/// Set value 
		public virtual void SetValue( Vector2 val )
		{
			throw new AxiomException( "Animable SetValue to Vector2 not implemented" );
		}

		/// Set value 
		public virtual void SetValue( Vector3 val )
		{
			throw new AxiomException( "Animable SetValue to Vector3 not implemented" );
		}

		/// Set value 
		public virtual void SetValue( Vector4 val )
		{
			throw new AxiomException( "Animable SetValue to Vector4 not implemented" );
		}

		/// Set value 
		public virtual void SetValue( Quaternion val )
		{
			throw new AxiomException( "Animable SetValue to Quaternion not implemented" );
		}

		/// Set value 
		public virtual void SetValue( ColorEx val )
		{
			throw new AxiomException( "Animable SetValue to ColorEx not implemented" );
		}

		/// Set value 
		public virtual void SetValue( Object val )
		{
			switch ( type )
			{
				case AnimableType.Int:
					SetValue( (int)val );
					break;
				case AnimableType.Real:
					SetValue( (Real)val );
					break;
				case AnimableType.Vector2:
					SetValue( (Vector2)val );
					break;
				case AnimableType.Vector3:
					SetValue( (Vector3)val );
					break;
				case AnimableType.Vector4:
					SetValue( (Vector4)val );
					break;
				case AnimableType.Quaternion:
					SetValue( (Quaternion)val );
					break;
				case AnimableType.ColorEx:
					SetValue( (ColorEx)val );
					break;
			}
		}

		/// Apply the specified delta 
		public virtual void ApplyDeltaValue( int val )
		{
			throw new AxiomException( "Animable ApplyDeltaValue to int not implemented" );
		}

		/// Apply the specified delta 
		public virtual void ApplyDeltaValue( Real val )
		{
			throw new AxiomException( "Animable ApplyDeltaValue to float not implemented" );
		}

		/// Apply the specified delta 
		public virtual void ApplyDeltaValue( Vector2 val )
		{
			throw new AxiomException( "Animable ApplyDeltaValue to Vector2 not implemented" );
		}

		/// Apply the specified delta 
		public virtual void ApplyDeltaValue( Vector3 val )
		{
			throw new AxiomException( "Animable ApplyDeltaValue to Vector3 not implemented" );
		}

		/// Apply the specified delta 
		public virtual void ApplyDeltaValue( Vector4 val )
		{
			throw new AxiomException( "Animable ApplyDeltaValue to Vector4 not implemented" );
		}

		/// Apply the specified delta 
		public virtual void ApplyDeltaValue( Quaternion val )
		{
			throw new AxiomException( "Animable ApplyDeltaValue to Quaternion not implemented" );
		}

		/// Apply the specified delta 
		public virtual void ApplyDeltaValue( ColorEx val )
		{
			throw new AxiomException( "Animable ApplyDeltaValue to ColorEx not implemented" );
		}

		/// Apply the specified delta 
		public virtual void ApplyDeltaValue( Object val )
		{
			switch ( type )
			{
				case AnimableType.Int:
					ApplyDeltaValue( (int)val );
					break;
				case AnimableType.Real:
					ApplyDeltaValue( (Real)val );
					break;
				case AnimableType.Vector2:
					ApplyDeltaValue( (Vector2)val );
					break;
				case AnimableType.Vector3:
					ApplyDeltaValue( (Vector3)val );
					break;
				case AnimableType.Vector4:
					ApplyDeltaValue( (Vector4)val );
					break;
				case AnimableType.Quaternion:
					ApplyDeltaValue( (Quaternion)val );
					break;
				case AnimableType.ColorEx:
					ApplyDeltaValue( (ColorEx)val );
					break;
			}
		}

		public static Object InterpolateValues( float time, AnimableType type, Object k1, Object k2 )
		{
			switch ( type )
			{
				case AnimableType.Int:
					var i1 = (int)k1;
					var i2 = (int)k2;
					return (Object)(int)( i1 + ( i2 - i1 )*time );
				case AnimableType.Real:
					var f1 = (float)k1;
					var f2 = (float)k2;
					return (Object)( f1 + ( f2 - f1 )*time );
				case AnimableType.Vector2:
					var v21 = (Vector2)k1;
					var v22 = (Vector2)k2;
					return (Object)( v21 + ( v22 - v21 )*time );
				case AnimableType.Vector3:
					var v31 = (Vector3)k1;
					var v32 = (Vector3)k2;
					return (Object)( v31 + ( v32 - v31 )*time );
				case AnimableType.Vector4:
					var v41 = (Vector4)k1;
					var v42 = (Vector4)k2;
					return (Object)( v41 + ( v42 - v41 )*time );
				case AnimableType.Quaternion:
					var q1 = (Quaternion)k1;
					var q2 = (Quaternion)k2;
					return (Object)( q1 + ( q2 + ( -1*q1 ) )*time );
				case AnimableType.ColorEx:
					var c1 = (ColorEx)k1;
					var c2 = (ColorEx)k2;
					return
						(Object)
						( new ColorEx( c1.a + ( c2.a - c1.a )*time, c1.r + ( c2.r - c1.r )*time, c1.g + ( c2.g - c1.g )*time,
						               c1.b + ( c2.b - c1.b )*time ) );
			}
			throw new AxiomException( "In AmiableValue.InterpolateValues, unknown type {0}", type );
		}

		public static Object MultiplyFloat( AnimableType type, float v, Object k )
		{
			switch ( type )
			{
				case AnimableType.Int:
					return (Object)(int)( ( (int)k )*v );
				case AnimableType.Real:
					var f = (Real)k;
					return (Object)( f*v );
				case AnimableType.Vector2:
					var v2 = (Vector2)k;
					return (Object)( v2*v );
				case AnimableType.Vector3:
					var v3 = (Vector3)k;
					return (Object)( v3*v );
				case AnimableType.Vector4:
					var v4 = (Vector4)k;
					return (Object)( v4*v );
				case AnimableType.Quaternion:
					var q = (Quaternion)k;
					return (Object)( q*v );
				case AnimableType.ColorEx:
					var c = (ColorEx)k;
					return (Object)( new ColorEx( c.a*v, c.r*v, c.g*v, c.b*v ) );
			}
			throw new AxiomException( "In AmiableValue.InterpolateValues, unknown type {0}", type );
		}

		/// Sets the current state as the 'base' value; used for delta animation
		/// Any instantiated derived class must implement this guy
		public abstract void SetCurrentStateAsBaseValue();
	}

	/// <summary>
	/// Defines an interface to classes which have one or more AnimableValue instances to expose.
	/// </summary>
	public interface IAnimableObject
	{
		#region Methods

		/// <summary>
		///		Create an AnimableValue for the attribute with the given name, or 
		///     throws an exception if this object doesn't support creating them.
		/// </summary>
		AnimableValue CreateAnimableValue( string valueName );

		#endregion Methods

		#region Properties

		/// <summary>
		///		Return the names of all the AnimableValue names supported by this object.
		///     This can return the null list if there are none.
		/// </summary>
		string[] AnimableValueNames { get; }

		#endregion Properties
	}
}