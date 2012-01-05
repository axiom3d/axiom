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
//     <id value="$Id: DDSCodec.cs 1333 2008-07-28 18:51:56Z borrillis $"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;

using Axiom.Core;
using Axiom.Graphics;
using Axiom.Media;

using Tao.DevIl;

#endregion Namespace Declarations

namespace Axiom.Plugins.DevILCodecs
{
	/// <summary>
	///    Microsoft's DDS file format codec.
	/// </summary>
	public class DDSCodec : ILImageCodec
	{
		public DDSCodec() {}

		#region ILImageCodec Implementation

		public override object Decode( System.IO.Stream input, System.IO.Stream output, params object[] args )
		{
			// nothing special needed, just pass through
			return base.Decode( input, output, args );
		}

		public override void Encode( System.IO.Stream source, System.IO.Stream dest, params object[] args )
		{
			throw new NotImplementedException( "DDS file encoding is not yet supported." );
		}

		/// <summary>
		///    DDS enum value.
		/// </summary>
		public override int ILType { get { return Il.IL_DDS; } }

		/// <summary>
		///    Returns that this codec handles dds files.
		/// </summary>
		public override String Type { get { return "dds"; } }

		#endregion ILImageCodec Implementation
	}
}
