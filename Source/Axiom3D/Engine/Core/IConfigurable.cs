#region LGPL License
/*
Axiom Graphics Engine Library
Copyright (C) 2003-2006 Axiom Project Team

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
//     <license see="http://axiomengine.sf.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;

#endregion Namespace Declarations

namespace Axiom.Core
{
    /// <summary>
    /// 	Describes behaviors required by objects that can be configured, whether through script
    /// 	parameters or programatically.
    /// </summary>
    public interface IConfigurable
    {
        /// <summary>
        ///    Will be called by script parsers that run across extended properties, and will pass them
        ///    along expecting the target object to handle them.
        /// </summary>
        /// <param name="name">
        ///    Name of the parameter.
        /// </param>
        /// <param name="val">
        ///    Value of the parameter.
        /// </param>
        /// <returns>
        ///    False if the param was not dealt with, True if it was.
        /// </returns>
        bool SetParam( string name, string val );
    }
}
