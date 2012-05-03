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

#endregion Namespace Declarations

namespace Axiom.Core
{
	/// <summary>
	///
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class ChainedEvent<T>
		where T : EventArgs
	{
		public EventHandler<T> EventSinks;

		/// <summary>
		///
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="arg"></param>
		/// <param name="compare"></param>
		/// <returns></returns>
		public virtual bool Fire( object sender, T arg, Predicate<T> compare )
		{
			var continueChain = true;

			// Assuming the multicast delegate is not null...
			if ( EventSinks != null )
			{
				// Call the methods until one of them handles the event
				// or all the methods in the delegate list are processed.
				foreach ( EventHandler<T> sink in EventSinks.GetInvocationList() )
				{
					sink( sender, arg );
					continueChain = compare( arg );
					if ( !continueChain )
					{
						break;
					}
				}
			}
			// Return a flag indicating whether an event sink canceled the event.
			return continueChain;
		}
	}
}