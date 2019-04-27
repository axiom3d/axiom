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
using System.Collections.Generic;
using System.Text;

#endregion Namespace Declarations

namespace Axiom.Core
{
    /// <summary>
    /// Generates a unique name for a given type T
    /// </summary>
    /// <typeparam name="T">the type to generate a name for.</typeparam>
    public class NameGenerator<T>
    {
        private static long _nextId;
        private static string _baseName;

        /// <summary>
        /// Gets/sets the next identifier used to generate a name
        /// </summary>
        public long NextIdentifier
        {
            get
            {
                return _nextId;
            }
            set
            {
                _nextId = value;
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <remarks>
        /// use the name of the type as a base for generating unique names.
        /// </remarks>
        public NameGenerator()
            : this(typeof(T).Name)
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="baseName">the base of the name for the type</param>
        public NameGenerator(string baseName)
        {
            if (string.IsNullOrEmpty(_baseName))
            {
                _baseName = baseName;
            }
        }

        /// <summary>
        /// Generates the next name
        /// </summary>
        /// <returns>the generated name</returns>
        public string GetNextUniqueName()
        {
            return GetNextUniqueName(String.Empty);
        }

        /// <summary>
        /// Generates the next name using a given prefix
        /// </summary>
        /// <param name="prefix">a prefix for the name</param>
        /// <returns>the generated name</returns>
        public string GetNextUniqueName(string prefix)
        {
            return String.Format("{0}{1}{2}", prefix, _baseName, _nextId++);
        }
    }
}