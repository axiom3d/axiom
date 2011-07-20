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
//     <id value="$Id:$"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Axiom.Core;
using Axiom.Serialization;

#endregion Namespace Declarations

namespace Axiom.Graphics
{
	public partial class GpuProgramParameters
	{
        /// <summary>
        /// Find a constant definition for a named parameter.
        /// <remarks>
        /// This method returns null if the named parameter did not exist, unlike
        /// <see cref="GetConstantDefinition" /> which is more strict; unless you set the 
        /// last parameter to true.
        /// </remarks>
        /// </summary>
        /// <param name="name">The name to look up</param>
        [OgreVersion(1, 7, 2790)]
        public GpuConstantDefinition FindNamedConstantDefinition( string name )
		{
			return FindNamedConstantDefinition( name, false );
		}
        /// <summary>
        /// Find a constant definition for a named parameter.
        /// <remarks>
        /// This method returns null if the named parameter did not exist, unlike
        /// <see cref="GetConstantDefinition" /> which is more strict; unless you set the 
        /// last parameter to true.
        /// </remarks>
        /// </summary>
        /// <param name="name">The name to look up</param>
        /// <param name="throwExceptionIfNotFound"> If set to true, failure to find an entry
        /// will throw an exception.</param>
        [OgreVersion(1, 7, 2790)]
        public GpuConstantDefinition FindNamedConstantDefinition( string name, bool throwExceptionIfNotFound )
	    {

            if (_namedConstants == null)
		    {
                if (throwExceptionIfNotFound)
                    throw new AxiomException( "Named constants have not been initialized, perhaps a compile error." );
			    return null;
		    }

            GpuConstantDefinition def;
            if (!_namedConstants.Map.TryGetValue(name, out def))
		    {
			    if (throwExceptionIfNotFound)
			        throw new AxiomException( "Parameter called " + name + " does not exist. " );
			    return null;
		    }
            return def;
	    }
	}
}