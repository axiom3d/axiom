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
//     <id value="$Id$"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections;
using System.IO;
using Axiom.Core;
using System.Collections.Generic;

#endregion Namespace Declarations

namespace Axiom.Media
{
	/// <summary>
	///    Manages registering/fulfilling requests for codecs that handle various types of media.
	/// </summary>
	public sealed class CodecManager : DisposableObject
	{
		#region Singleton implementation

		/// <summary>
		///     Singleton instance of this class.
		/// </summary>
		private static CodecManager instance;

		/// <summary>
		///     Internal constructor.  This class cannot be instantiated externally.
		/// </summary>
		internal CodecManager()
			: base()
		{
			if ( instance == null )
			{
				instance = this;
			}
		}

		/// <summary>
		///     Gets the singleton instance of this class.
		/// </summary>
		public static CodecManager Instance
		{
			get
			{
				return instance;
			}
		}

		#endregion Singleton implementation

		protected override void dispose(bool disposeManagedResources)
		{
			if ( instance == this )
			{
				if (disposeManagedResources)
				{
					instance = null;
				}
			}

			base.dispose(disposeManagedResources);
		}

		#region Fields

		/// <summary>
		///    List of registered media codecs.
		/// </summary>
		private Dictionary<string, ICodec> codecs = new Dictionary<string, ICodec>( new CaseInsensitiveStringComparer() );

		#endregion Fields

		/// <summary>
		///     Register all default IL image codecs.
		/// </summary>
		public void RegisterCodecs()
		{
			// register codecs
			//RegisterCodec( new JPGCodec() );
			//RegisterCodec( new BMPCodec() );
			//RegisterCodec( new PNGCodec() );
			//RegisterCodec( new DDSCodec() );
			//RegisterCodec( new TGACodec() );
		}

		/// <summary>
		///    Registers a new codec that can handle a particular type of media files.
		/// </summary>
		/// <param name="codec"></param>
		public void RegisterCodec( ICodec codec )
		{
			codecs[ codec.Type ] = codec;
		}

		/// <summary>
		///    Gets the codec registered for the passed in file extension.
		/// </summary>
		/// <returns></returns>
		public ICodec GetCodec( string extension )
		{
			if ( !codecs.ContainsKey( extension ) )
			{
				LogManager.Instance.Write( "No codec available for media with extension .{0}", extension );
				RegisterCodec( new NullCodec( extension ) );
			}

			return (ICodec)codecs[ extension ];
		}

        /// <summary>
        /// Gets the codec that can handle the given 'magic' identifier.
        /// </summary>
        /// <param name="magicBuf">
        /// Pointer to a stream of bytes which should identify the file.
        /// Note that this may be more than needed - each codec may be looking for 
        /// a different size magic number.
        /// </param>
        /// <param name="magicLen">The number of bytes passed</param>
        [OgreVersion( 1, 7, 2 )]
        public ICodec GetCodec( byte[] magicBuf, int magicLen )
        {
            foreach ( var i in codecs )
            {
                var ext = i.Value.MagicNumberToFileExt( magicBuf, magicLen );

                if ( !string.IsNullOrEmpty( ext ) )
                {
                    // check codec type matches
                    // if we have a single codec class that can handle many types, 
                    // and register many instances of it against different types, we
                    // can end up matching the wrong one here, so grab the right one
                    if ( ext == i.Value.Type )
                        return i.Value;
                    else
                        return GetCodec( ext );
                }
            }

            return null;
        }

		/// <summary>
		/// Checks whether a codec is aviable or not.
		/// </summary>
		/// <param name="extension">codec to check for</param>
		/// <returns></returns>
		public bool IsCodecAviable( string extension )
		{
			return codecs.ContainsKey( extension );
	}

		/// <summary>
		///  URegisters a new codec that can handle a particular type of media files.
		/// </summary>
		/// <param name="codec">codec to unrerigster</param>
		public void UnregisterCodec( ICodec codec )
		{
			if ( codecs.ContainsKey( codec.Type ) )
				codecs.Remove( codec.Type );
		}
    }

	public class NullCodec : ICodec
	{
		private string _type;
		public NullCodec( string extension )
		{
			_type = extension;
		}

		public object Decode( Stream input, Stream output, params object[] args )
		{
			return null;
		}

		public void Encode( Stream input, Stream output, params object[] args )
		{
			return;
		}

		public void EncodeToFile( Stream input, string fileName, object codecData )
		{
			return;
		}

		public string Type
		{
			get
			{
				return _type;
			}
		}

        /// <see cref="Axiom.Media.ICodec.MagicNumberToFileExt"/>
        public string MagicNumberToFileExt( byte[] magicBuf, int maxbytes )
        {
            return string.Empty;
        }
    }
}