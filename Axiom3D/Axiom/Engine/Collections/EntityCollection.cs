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
//using T = Axiom.Core.Entity;
// used to alias a key value in the code for easy copying and pasting.  Come on generics!!
//using K = System.String;

namespace Axiom {
    /// <summary>
    /// Summary description for EntityCollection.
    /// </summary>
    public class EntityList : AxiomCollection<string, Entity>
    {
        #region Constructors

        /// <summary>
        ///		Default constructor.
        /// </summary>
        public EntityList() : base() { }

        #endregion

        public override void Add(Entity item)
        {
            if (item.Name == string.Empty)
                base.Add("Entity" + nextUniqueKeyCounter++, item);
            else
                base.Add(item.Name, item);
        }
    }
}
