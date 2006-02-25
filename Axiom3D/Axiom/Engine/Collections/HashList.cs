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
using System.Collections;

#endregion Namespace Declarations
			
namespace Axiom
{
    /// <summary>
    /// 	Summary description for HashList.
    /// </summary>
    public class HashList
    {
        Hashtable itemTable = new Hashtable();
        SortedList itemList = new SortedList();
        ArrayList itemKeys = new ArrayList();

        #region Member variables

        #endregion

        #region Constructors

        public HashList()
        {
            //
            // TODO Add constructor logic here
            //
        }

        #endregion

        #region Methods

        public void Add( object key, object item )
        {
            itemTable.Add( key, item );
            itemList.Add( key, item );
            itemKeys.Add( key );
        }

        public object GetKeyAt( int index )
        {
            return itemList.GetKey( index );
        }

        public object GetByKey( object key )
        {
            return itemTable[key];
        }

        public bool ContainsKey( object key )
        {
            return itemTable.ContainsKey( key );
        }

        public void Clear()
        {
            itemTable.Clear();
            itemList.Clear();
            itemKeys.Clear();
        }

        public void Remove( object key )
        {
            itemTable.Remove( key );
            itemList.Remove( key );
            itemKeys.Remove( key );
        }

        #endregion

        #region Properties

        public int Count
        {
            get
            {
                return itemList.Count;
            }
        }

        #endregion

        #region Operators

        public object this[int index]
        {
            get
            {
                return itemList.GetByIndex( index );
            }
        }

        public object this[object key]
        {
            get
            {
                return itemList[key];
                //return itemTable[key]; 
            }
        }

        #endregion

    }
}
