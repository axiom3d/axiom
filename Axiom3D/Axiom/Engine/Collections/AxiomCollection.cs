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
using System.Collections.Generic;
#endregion Namespace declarations

#endregion Namespace Declarations

namespace Axiom
{
    //public interface IKeyValue
    //{
    //}

    //public struct StringKeyValue : IKeyValue
    //{
    //    private string _value;

    //    #region Constructors

    //    public StringKeyValue( int value )
    //    {
    //        this._value = value.ToString();
    //    }

    //    public StringKeyValue( string value )
    //    {
    //        this._value = value;
    //    }

    //    #endregion Constructors

    //    #region Conversion Operators

    //    #region Int Conversions
    //    /// <summary>
    //    /// Implicit conversion from int to Real
    //    /// </summary>
    //    /// <param name="value"></param>
    //    /// <returns></returns>
    //    static public implicit operator StringKeyValue( int val )
    //    {
    //        return new StringKeyValue( val );
    //    }
    //    #endregion Int Conversions

    //    #region String Conversions

    //    /// <summary>
    //    /// Implicit conversion from string to Real
    //    /// </summary>
    //    /// <param name="value"></param>
    //    /// <returns></returns>
    //    static public implicit operator StringKeyValue( string val )
    //    {
    //        return new StringKeyValue( val );
    //    }

    //    /// <summary>
    //    /// Explicit conversion from Real to string
    //    /// </summary>
    //    /// <param name="value"></param>
    //    /// <returns></returns>
    //    static public explicit operator string( StringKeyValue val )
    //    {
    //        return val.ToString();
    //    }

    //    #endregion String Conversions

    //    #endregion Conversion Operators

    //    #region System.Object Overloads

    //    public override string ToString()
    //    {
    //        return _value;
    //    }

    //    #endregion
    //}

    //public struct IntKeyValue : IKeyValue
    //{
    //    private int _value;

    //    #region Constructors

    //    public IntKeyValue( int value )
    //    {
    //        this._value = value;
    //    }

    //    public IntKeyValue( string value )
    //    {
    //        this._value = int.Parse( value );
    //    }

    //    #endregion Constructors

    //    #region Conversion Operators

    //    #region Int Conversions
    //    /// <summary>
    //    /// Implicit conversion from int to Real
    //    /// </summary>
    //    /// <param name="value"></param>
    //    /// <returns></returns>
    //    static public implicit operator IntKeyValue( int val )
    //    {
    //        return new IntKeyValue( val );
    //    }
    //    #endregion Int Conversions

    //    #region String Conversions

    //    /// <summary>
    //    /// Implicit conversion from string to Real
    //    /// </summary>
    //    /// <param name="value"></param>
    //    /// <returns></returns>
    //    static public implicit operator IntKeyValue( string val )
    //    {
    //        return new IntKeyValue( val );
    //    }

    //    /// <summary>
    //    /// Explicit conversion from Real to string
    //    /// </summary>
    //    /// <param name="value"></param>
    //    /// <returns></returns>
    //    static public explicit operator string( IntKeyValue val )
    //    {
    //        return val.ToString();
    //    }

    //    #endregion String Conversions

    //    #endregion Conversion Operators

    //    #region System.Object Overloads

    //    public override string ToString()
    //    {
    //        return _value.ToString();
    //    }

    //    #endregion
    //}

    /// <summary>
    ///		Serves as a basis for strongly typed collections in the engine.
    /// </summary>
    public class AxiomCollection<K, T>: SortedList<K, T> where K : IConvertible
    {
        /// <summary></summary>
        private const int INITIAL_CAPACITY = 60;
        /// <summary></summary>
        static protected int nextUniqueKeyCounter;

        #region Constructors

        /// <summary>
        ///		
        /// </summary>
        public AxiomCollection(): base( INITIAL_CAPACITY )
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="capacity"></param>
        public AxiomCollection( int capacity ): base( capacity )
        {
        }

        #endregion

        /// <summary>
        ///		Get/Set indexer that allows access to the collection by index.
        /// </summary>
        public T this[ int index ]
        {
            get
            {
                return (T)base.Values[ index ];
            }
            set
            {
                base.Values[ index ] = value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        public void Remove( T item )
        {
            if ( this.ContainsValue( item ) )
            {
                int index = this.IndexOfValue( item );
                this.RemoveAt( index );
            }
        }

        /// <summary>
        ///		Accepts an unnamed object and names it manually.
        /// </summary>
        /// <param name="item"></param>
        public virtual void Add( T item )
        {
            K key = default( K );

            if ( key is string )
            {
                key = (K)Convert.ChangeType( key.GetType().Name + nextUniqueKeyCounter++, key.GetType() );
            }
            else
            {
                key = (K)Convert.ChangeType( nextUniqueKeyCounter++, key.GetType() );
            }

            base.Add( key, item );
        }

    }
}



