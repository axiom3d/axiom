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
using System.IO;

#endregion Namespace Declarations

namespace Axiom.Media
{
	/// <summary>
	///    Interface describing an object that can handle a form of media, be it
	///    a image, sound, video, etc.
	/// </summary>
	public interface ICodec
	{

		/// <summary>
		///    Codes the data from the input chunk into the output chunk.
		/// </summary>
		/// <param name="input">Input stream (encoded data).</param>
		/// <param name="output">Output stream (decoded data).</param>
		/// <param name="args">Variable number of extra arguments.</param>
		/// <returns>
		///    An object that holds data specific to the media format which this codec deal with.
		///    For example, an image codec might return a structure that has image related details,
		///    such as height, width, etc.
		/// </returns>
		object Decode( Stream input, Stream output, params object[] args );

		/// <summary>
		///    Encodes the data in the input stream and saves the result in the output stream.
		/// </summary>
		/// <param name="input">Input stream (decoded data).</param>
		/// <param name="output">Output stream (encoded data).</param>
		/// <param name="args">Variable number of extra arguments.</param>
		void Encode( Stream input, Stream output, params object[] args );

		/// <summary>
		///     Encodes data to a file.
		/// </summary>
		/// <param name="input">Stream containing data to write.</param>
		/// <param name="fileName">Filename to output to.</param>
		/// <param name="codecData">Extra data to use in order to describe the codec data.</param>
		void EncodeToFile( Stream input, string fileName, object codecData );

		/// <summary>
		///    Gets the type of data that this codec is meant to handle, typically a file extension.
		/// </summary>
		String Type
		{
			get;
		}

        /// <summary>
        /// Maps a magic number header to a file extension, if this codec recognises it.
        /// </summary>
        /// <param name="magicBuf">
        /// Pointer to a stream of bytes which should identify the file.
        /// Note that this may be more than needed - each codec may be looking for 
        /// a different size magic number.
        /// </param>
        /// <param name="maxbytes">The number of bytes passed</param>
        /// <returns>A blank string if the magic number was unknown, or a file extension.</returns>
        [OgreVersion( 1, 7, 2 )]
        string MagicNumberToFileExt( byte[] magicBuf, int maxbytes );
    }
}
