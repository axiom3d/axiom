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
//     <id value="$Id: JPGCodec.cs 1054 2007-05-24 13:47:35Z borrillis $"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;

using Tao.DevIl;

#endregion Namespace Declarations

namespace Axiom.Plugins.DevILCodecs
{
	/// <summary>
	///    JPG image file codec.
	/// </summary>
	public class JPGCodec : ILImageCodec
	{
		public JPGCodec()
		{
		}

		#region ILImageCodec Implementation

		/// <summary>
		///    Passthrough implementation, no special code needed.
		/// </summary>
		/// <param name="input"></param>
		/// <param name="output"></param>
		/// <param name="args"></param>
		/// <returns></returns>
		public override object Decode( System.IO.Stream input, System.IO.Stream output, params object[] args )
		{
			// nothing special needed, just pass through
			return base.Decode( input, output, args );
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="source"></param>
		/// <param name="dest"></param>
		/// <param name="args"></param>
		public override void Encode( System.IO.Stream source, System.IO.Stream dest, params object[] args )
		{
			throw new NotImplementedException( "JPG encoding is not yet implemented." );
		}

		/// <summary>
		///    Returns the JPG file extension.
		/// </summary>
		public override String Type
		{
			get
			{
				return "jpg";
			}
		}


		/// <summary>
		///    Returns JPG enum.
		/// </summary>
		public override int ILType
		{
			get
			{
				return Il.IL_JPG;
			}
		}

        /// <see cref="Axiom.Media.ICodec.MagicNumberToFileExt"/>
        public override string MagicNumberToFileExt( byte[] magicBuf, int maxbytes )
        {
            //TODO
            return string.Empty;
        }

		#endregion ILImageCodec Implementation
	}
}