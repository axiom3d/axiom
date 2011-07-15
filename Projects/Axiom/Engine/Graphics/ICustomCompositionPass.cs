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
/*
 * Many thanks to the folks at Multiverse for providing the initial port for this class
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
using System.Collections.Generic;

#endregion Namespace Declarations

namespace Axiom.Graphics
{
	/// <summary>
	/// Interface for custom composition passes, allowing custom operations (in addition to
	/// the quad, scene and clear operations) in composition passes.
    /// <seealso cref="CompositorManager.RegisterCustomCompositorPass"/>
	/// </summary>
	public interface ICustomCompositionPass
	{
		/// <summary>
		/// Create a custom composition operation.
		/// </summary>
		/// <param name="instance">The compositor instance that this operation will be performed in</param>
		/// <param name="pass">The CompositionPass that triggered the request</param>
		/// <returns></returns>
		/// <remarks>
		/// This call only happens once during creation. The CompositeRenderSystemOperation will
		/// get called each render.
		/// </remarks>
		CompositeRenderSystemOperation CreateOperation( CompositorInstance instance, CompositionPass pass );
	}
}
