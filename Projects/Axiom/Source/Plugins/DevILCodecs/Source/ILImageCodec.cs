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
using System.IO;

using Axiom.Core;
using Axiom.Math.Collections;
using Axiom.Media;

using Tao.DevIl;
using System.Runtime.InteropServices;

#endregion Namespace Declarations

namespace Axiom.Plugins.DevILCodecs
{
	/// <summary>
	///    Base DevIL (OpenIL) implementation for loading images.
	/// </summary>
	public abstract class ILImageCodec : ImageCodec
	{
		#region Fields

		/// <summary>
		///    Flag used to ensure DevIL gets initialized once.
		/// </summary>
		protected static bool isInitialized;

		#endregion

		#region Constructor

		public ILImageCodec()
		{
			InitializeIL();
		}

		#endregion Constructor

		#region ImageCodec Implementation

		public override void EncodeToFile( Stream input, string fileName, object codecData )
		{
			int imageID;

			// create and bind a new image
			Il.ilGenImages( 1, out imageID );
			Il.ilBindImage( imageID );

			byte[] buffer = new byte[ input.Length ];
			input.Read( buffer, 0, buffer.Length );

			ImageData data = (ImageData)codecData;

			GCHandle bufHandle = GCHandle.Alloc( buffer, GCHandleType.Pinned );
			PixelBox src = new PixelBox( data.width, data.height, data.depth, data.format, bufHandle.AddrOfPinnedObject() );

			try
			{
				// Convert image from Axiom to current IL image
				ILUtil.ConvertToIL( src );
			}
			catch ( Exception ex )
			{
				LogManager.Instance.Write( "IL Failed image conversion :", ex.Message );
			}

			// flip the image
			Ilu.iluFlipImage();

			// save the image to file
			Il.ilSaveImage( fileName );

			if ( bufHandle.IsAllocated )
				bufHandle.Free();

			int error = Il.ilGetError();

			if ( error != Il.IL_NO_ERROR )
				LogManager.Instance.Write( "IL Error, could not save file: {0} : {1}", fileName, Ilu.iluErrorString( error ) );
		}

		public override object Decode( Stream input, Stream output, params object[] args )
		{
			ImageData data = new ImageData();

			int imageID;
			int format, bytesPerPixel;

			// create and bind a new image
			Il.ilGenImages( 1, out imageID );
			Il.ilBindImage( imageID );

			// create a temp buffer and write the stream into it
			byte[] buffer = new byte[ input.Length ];
			input.Read( buffer, 0, buffer.Length );

			// load the data into DevIL
			Il.ilLoadL( this.ILType, buffer, buffer.Length );

			// check for an error
			int ilError = Il.ilGetError();

			if ( ilError != Il.IL_NO_ERROR )
			{
				throw new AxiomException( "Error while decoding image data: '{0}'", Ilu.iluErrorString( ilError ) );
			}

			format = Il.ilGetInteger( Il.IL_IMAGE_FORMAT );
			int imageType = Il.ilGetInteger( Il.IL_IMAGE_TYPE );
			bytesPerPixel = Il.ilGetInteger( Il.IL_IMAGE_BYTES_PER_PIXEL );

			// populate the image data
			data.width = Il.ilGetInteger( Il.IL_IMAGE_WIDTH );
			data.height = Il.ilGetInteger( Il.IL_IMAGE_HEIGHT );
			data.depth = Il.ilGetInteger( Il.IL_IMAGE_DEPTH );
			data.numMipMaps = Il.ilGetInteger( Il.IL_NUM_MIPMAPS );
			data.format = ILUtil.Convert( format, imageType );
			data.size = data.width * data.height * bytesPerPixel;

			// get the decoded data
			buffer = new byte[ data.size ];
			IntPtr ptr = Il.ilGetData();

			// copy the data into the byte array
			unsafe
			{
				byte* pBuffer = (byte*)ptr;
				for ( int i = 0; i < buffer.Length; i++ )
				{
					buffer[ i ] = pBuffer[ i ];
				}
			}

			// write the decoded data to the output stream
			output.Write( buffer, 0, buffer.Length );

			// we won't be needing this anymore
			Il.ilDeleteImages( 1, ref imageID );

			return data;
		}

		#endregion ImageCodec Implementation

		#region Methods

		/// <summary>
		///    One time DevIL initialization.
		/// </summary>
		public void InitializeIL()
		{
			if ( !isInitialized )
			{
				// fire it up!
				Il.ilInit();

				// enable automatic file overwriting
				Il.ilEnable( Il.IL_FILE_OVERWRITE );

				isInitialized = true;
			}
		}

		#endregion Methods

		#region Properties

		/// <summary>
		///    Implemented by subclasses to return the IL type enum value for this
		///    images file type.
		/// </summary>
		public abstract int ILType
		{
			get;
		}

		#endregion Properties
	}
}
