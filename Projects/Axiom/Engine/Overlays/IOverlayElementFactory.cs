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



#endregion Namespace Declarations

namespace Axiom.Overlays
{
	/// <summary>
	/// 	Defines the interface which all components wishing to 
	/// 	supply OverlayElement subclasses must implement.
	/// </summary>
	/// <remarks>
	/// 	To allow the OverlayElement types available for inclusion on 
	/// 	overlays to be extended, the engine allows external apps or plugins
	/// 	to register their ability to create custom OverlayElements with
	/// 	the GuiManager, using the AddOverlayElementFactory method. Classes
	/// 	wanting to do this must implement this interface.
	/// 	<p/>
	/// 	Each OverlayElementFactory creates a single type of OverlayElement, 
	/// 	identified by a 'type name' which must be unique.
	/// </remarks>
	public interface IOverlayElementFactory
	{
		#region Methods

		/// <summary>
		///    Classes that implement this interface will return an instance of a OverlayElement of their designated
		///    type.
		/// </summary>
		/// <param name="name">Name of the element to create.</param>
		/// <returns>A new instance of a OverlayElement with the specified name.</returns>
		OverlayElement Create( string name );

		#endregion

		#region Properties

		/// <summary>
		///    Classes that implement this interface should return the name of the OverlayElement that it will be
		///    responsible for creating.
		/// </summary>
		string Type { get; }

		#endregion
	}
}
