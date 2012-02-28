#region MIT/X11 License
//Copyright © 2003-2012 Axiom 3D Rendering Engine Project
//
//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:
//
//The above copyright notice and this permission notice shall be included in
//all copies or substantial portions of the Software.
//
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//THE SOFTWARE.
#endregion License

#region SVN Version Information
// <file>
//     <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System.IO;
using Axiom.Collections;
using Axiom.Core;
using Axiom.Math;

#endregion Namespace Declarations

namespace Axiom.Media
{
    /// <summary>
    ///  Abstract class that defines a 'codec'.
    /// </summary>
    /// <remarks>
    /// A codec class works like a two-way filter for data - data entered on
    /// one end (the decode end) gets processed and transformed into easily
    /// usable data while data passed the other way around codes it back.
    /// @par
    /// The codec concept is a pretty generic one - you can easily understand
    /// how it can be used for images, sounds, archives, even compressed data.
    /// </remarks>
    public abstract class Codec
	{
        protected static AxiomCollection<Codec> mapCodecs = new AxiomCollection<Codec>();

        public class CodecData
        {
            /// <summary>
            /// Returns the type of the data.
            /// </summary>
            [OgreVersion( 1, 7, 2 )]
            public virtual string DataType
            {
                get
                {
                    return "CodecData";
                }
            }
        };

        /// <summary>
        /// Result of a decoding; both a decoded data stream and CodecData metadata
        /// </summary>
        public class DecodeResult
        {
            private readonly Tuple<Stream, CodecData> _tuple;

            public Stream First
            {
                get
                {
                    return _tuple.First;
                }
            }

            public CodecData Second
            {
                get
                {
                    return _tuple.Second;
                }
            }

            public DecodeResult( Stream s, CodecData data )
            {
                _tuple = new Tuple<Stream, CodecData>( s, data );
            }
        };

        /// <summary>
        /// Gets the file extension list for the registered codecs.
        /// </summary>
        [OgreVersion( 1, 7, 2 )]
        public static string[] Extensions
        {
            get
            {
                var res = new string[ mapCodecs.Count ];
                mapCodecs.Keys.CopyTo( res, 0 );
                return res;
            }
        }

        /// <summary>
        /// Returns the type of the codec as a String
        /// </summary>
        [OgreVersion( 1, 7, 2 )]
        public abstract string Type
        {
            get;
        }

        /// <summary>
        /// Returns the type of the data that supported by this codec as a String
        /// </summary>
        [OgreVersion( 1, 7, 2 )]
        public abstract string DataType
        {
            get;
        }

        /// <summary>
        /// Registers a new codec in the database.
        /// </summary>
        [OgreVersion( 1, 7, 2 )]
        public static void RegisterCodec( Codec codec )
        {
            if ( mapCodecs.ContainsKey( codec.Type ) )
                throw new AxiomException( "{0} already has a registered codec.", codec.Type );

            mapCodecs[ codec.Type ] = codec;
        }

        /// <summary>
        /// Return whether a codec is registered already.
        /// </summary>
        [OgreVersion( 1, 7, 2 )]
        public static bool IsCodecRegistered( string codecType )
        {
            return mapCodecs.ContainsKey( codecType );
        }

        /// <summary>
        /// Unregisters a codec from the database.
        /// </summary>
        [OgreVersion( 1, 7, 2 )]
        public static void UnregisterCodec( Codec codec )
        {
            mapCodecs.TryRemove( codec.Type );
        }

        /// <summary>
        /// Gets the codec registered for the passed in file extension.
        /// </summary>
        [OgreVersion( 1, 7, 2 )]
        public static Codec GetCodec( string extension )
        {
            var lwrcase = extension.ToLower();
            if ( !mapCodecs.ContainsKey( lwrcase ) )
            {
                var formatStr = string.Empty;
                if ( mapCodecs.Count == 0 )
                    formatStr = "There are no formats supported (no codecs registered).";
                else
                    formatStr = "Supported formats are: " + string.Join( ", ", Extensions );

                throw new AxiomException( "Can not find codec for '{0}' image format.\n{1}", extension, formatStr );
            }

            return mapCodecs[ lwrcase ];
        }

        /// <summary>
        /// Gets the codec that can handle the given 'magic' identifier.
        /// </summary>
        /// <param name="magicNumberBuf">
        /// Pointer to a stream of bytes which should identify the file.
        /// <note>
        /// Note that this may be more than needed - each codec may be looking for 
        /// a different size magic number.
        /// </note>
        /// </param>
        /// <param name="maxBytes">The number of bytes passed</param>
        [OgreVersion( 1, 7, 2 )]
        public static Codec GetCodec( byte[] magicNumberBuf, int maxBytes )
        {
            foreach ( var i in mapCodecs )
            {
                var ext = i.MagicNumberToFileExt( magicNumberBuf, maxBytes );
                if ( !string.IsNullOrEmpty( ext ) )
                {
                    // check codec type matches
                    // if we have a single codec class that can handle many types, 
                    // and register many instances of it against different types, we
                    // can end up matching the wrong one here, so grab the right one
                    if ( ext == i.Type )
                        return i;
                    else
                        return GetCodec( ext );
                }
            }

            return null;
        }

        /// <summary>
        /// Codes the data in the input stream and saves the result in the output stream.
        /// </summary>
        [OgreVersion( 1, 7, 2 )]
        public abstract Stream Encode( Stream input, CodecData data );

        /// <summary>
        /// Codes the data in the input chunk and saves the result in the output
        /// filename provided. Provided for efficiency since coding to memory is
        /// progressive therefore memory required is unknown leading to reallocations.
        /// </summary>
        /// <param name="input">The input data</param>
        /// <param name="outFileName">The filename to write to</param>
        /// <param name="data">Extra information to be passed to the codec (codec type specific)</param>
        [OgreVersion( 1, 7, 2 )]
        public abstract void EncodeToFile( Stream input, string outFileName, CodecData data );

		/// <summary>
        ///    Codes the data from the input chunk into the output chunk.
		/// </summary>
        /// <param name="input">Stream containing the encoded data</param>
        [OgreVersion( 1, 7, 2 )]
		public abstract DecodeResult Decode( Stream input );

        /// <summary>
        /// Returns whether a magic number header matches this codec.
        /// </summary>
        /// <param name="magicNumberBuf">
        /// Pointer to a stream of bytes which should identify the file.
        /// <note>
        /// Note that this may be more than needed - each codec may be looking for 
        /// a different size magic number.
        /// </note>
        /// </param>
        /// <param name="maxBytes">The number of bytes passed</param>
        [OgreVersion( 1, 7, 2 )]
        public virtual bool MagicNumberMatch( byte[] magicNumberBuf, int maxbytes )
        {
            return !string.IsNullOrEmpty( MagicNumberToFileExt( magicNumberBuf, maxbytes ) );
        }

        /// <summary>
        /// Maps a magic number header to a file extension, if this codec recognises it.
        /// </summary>
        /// <param name="magicNumberBuf">
        /// Pointer to a stream of bytes which should identify the file.
        /// Note that this may be more than needed - each codec may be looking for 
        /// a different size magic number.
        /// </param>
        /// <param name="maxbytes">The number of bytes passed</param>
        /// <returns>A blank string if the magic number was unknown, or a file extension.</returns>
        [OgreVersion( 1, 7, 2 )]
        public abstract string MagicNumberToFileExt( byte[] magicNumberBuf, int maxbytes );
    };
}
