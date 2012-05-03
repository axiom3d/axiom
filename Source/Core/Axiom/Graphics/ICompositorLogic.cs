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
	/// Interface for compositor logics, which can be automatically bound to compositors,
	/// allowing per-compositor logic (such as attaching a relevant listener) to happen
	/// automatically.
	/// </summary>
	public interface ICompositorLogic
	{
		/// <summary>
		/// Called when a compositor instance has been created.
		/// </summary>
		/// <remarks>
		/// This happens after its setup was finished, so the chain is also accessible.
		/// This is an ideal method to automatically attach a compositor listener.
		/// </remarks>
		/// <param name="newInstance"></param>
		void CompositorInstanceCreated( CompositorInstance newInstance );

		/// <summary>
		/// Called when a compositor instance has been destroyed
		/// </summary>
		/// <remarks>
		/// The chain that contained the compositor is still alive during this call.
		/// </remarks>
		/// <param name="destroyedInstance"></param>
		void CompositorInstanceDestroyed( CompositorInstance destroyedInstance );
	}

	/// <summary>
	/// Implementation base class for compositor logics, which can be automatically bound to compositors,
	/// allowing per-compositor logic (such as attaching a relevant listener) to happen
	/// automatically.
	/// </summary>
	/// <remarks>
	/// All methods have empty implementations to not force an implementer into
	/// extending all of them.
	/// </remarks>
	public class CompositorLogic : ICompositorLogic
	{
		#region Implementation of ICompositorLogic

		/// <summary>
		/// Called when a compositor instance has been created.
		/// </summary>
		/// <remarks>
		/// This happens after its setup was finished, so the chain is also accessible.
		/// This is an ideal method to automatically attach a compositor listener.
		/// </remarks>
		/// <param name="newInstance"></param>
		public virtual void CompositorInstanceCreated( CompositorInstance newInstance )
		{
		}

		/// <summary>
		/// Called when a compositor instance has been destroyed
		/// </summary>
		/// <remarks>
		/// The chain that contained the compositor is still alive during this call.
		/// </remarks>
		/// <param name="destroyedInstance"></param>
		public virtual void CompositorInstanceDestroyed( CompositorInstance destroyedInstance )
		{
		}

		#endregion
	}
}