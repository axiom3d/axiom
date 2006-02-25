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

#region Namespace declarations
using System;
using System.Diagnostics;

using Axiom.Core;
#endregion Namespace declarations

// used to alias a type in the code for easy copying and pasting.  Come on generics!!
//using T = Axiom.Core.Node;
// used to alias a key value in the code for easy copying and pasting.  Come on generics!!
//using K = System.String;

namespace Axiom
{
    /// <summary>
    ///		A strongly-typed collection of <see cref="Node"/> objects.
    /// </summary>
    [Serializable]
    public class NodeCollection : AxiomCollection<string, Node>
    {
        #region Constructors

        /// <summary>
        ///		Default constructor.
        /// </summary>
        public NodeCollection() : base() { }

        /// <summary>
        ///		Constructor that takes a parent object to, and calls the base class constructor to 
        /// </summary>
        /// <param name="entity"></param>
        //public SceneNodeCollection(P parent) : base(parent) {}

        #endregion


        public override void Add(Node item)
        {
            if (item.Name == string.Empty)
                base.Add("Node" + nextUniqueKeyCounter++, item);
            else
                base.Add(item.Name, item);
        }
    }
}