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

#endregion LGPL License

#region SVN Version Information

// <file>
//     <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
//     <id value="$Id:$"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;

#endregion Namespace Declarations

namespace Axiom.Plugins.SystemDrawingCodecs
{
	/// <summary>
	/// Provides loading mechanism for filetypes vis <see cref="SDImageCodec"/>
	/// </summary>
	public class SDImageLoader : SDImageCodec
	{
		private string extension;

		/// <summary>
		/// The file extension
		/// </summary>
		public string Extension
		{
			get
			{
				return extension;
			}
			set
			{
				extension = value;
			}
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="extension">the file extension that this will load.</param>
		public SDImageLoader( string extension )
		{
			this.extension = extension;
		}

		#region SDImageCodec Implementation

		/// <summary>
		/// Gets the type of data that this codec is meant to handle, typically a file extension.
		/// </summary>
		public override String Type
		{
			get
			{
				return extension;
			}
		}

		#endregion SDImageCodec Implementation
	};
}