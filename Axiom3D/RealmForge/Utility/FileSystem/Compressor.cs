using System;
using System.Collections;
using System.Collections.Specialized;
using System.IO;

namespace RealmForge.FileSystem
{
    #region Enums
    /// <summary>
    /// Represents the format used for the creation and reading of compressed streams and archives
    /// </summary>
    public enum CompressionFormat
    {
        Zip,
        Tar,
        GZip,
        BZip2
    }
    #endregion

    /// <summary>
    /// Compression and decompression singleton utility which is implemented using SharpZipLib
    /// </summary>
    /// <remarks>
    /// gzip is faster then Zip on Compression and is slightly smaller
    /// bzip2 is about 2.2x as slow, but produces about .8 the size for larger streams
    /// Though a lot of speed is lost with larger files, bzip likely wont make the stream that much smaller for small packets of XML, so the faster gzip should be used
    /// Output Streams Compress
    /// </remarks>
    /// TODO
    public abstract class Compressor
    {
        #region Constructors
        public Compressor()
        {
        }
        #endregion

        #region Singleton
        public static Compressor Instance;
        public static void AssertExists()
        {
            if ( Instance == null )
                Errors.InvalidState( "No Compresser singleton instance has been set so there is no implementation for compression facilities such as through 'ICSharpCode.SharpZipLib.dll'." );
        }
        #endregion

        #region Public Methods

        public Stream Read( CompressionFormat format, Stream compressedData )
        {
            switch ( format )
            {
                case CompressionFormat.Zip:
                    return ReadZip( compressedData );
                case CompressionFormat.Tar:
                    return ReadTar( compressedData );
                case CompressionFormat.GZip:
                    return ReadGZip( compressedData );
                case CompressionFormat.BZip2:
                    return ReadBZip2( compressedData );
                default:
                    Errors.NotSupported( "Compression format not supported." );
                    return null;
            }
        }

        public Stream Write( CompressionFormat format, Stream output )
        {
            switch ( format )
            {
                case CompressionFormat.Zip:
                    return WriteZip( output );
                case CompressionFormat.Tar:
                    return WriteTar( output );
                case CompressionFormat.GZip:
                    return WriteGZip( output );
                case CompressionFormat.BZip2:
                    return WriteBZip2( output );
                default:
                    Errors.NotSupported( "Compression format not supported." );
                    return null;
            }
        }
        #endregion

        #region Abstract methods
        public abstract Stream ReadZip( Stream compressedData );
        public abstract Stream ReadTar( Stream compressedData );
        public abstract Stream ReadGZip( Stream compressedData );
        public abstract Stream ReadBZip2( Stream compressedData );

        public abstract Stream WriteZip( Stream output );
        public abstract Stream WriteTar( Stream output );
        public abstract Stream WriteGZip( Stream output );
        public abstract Stream WriteBZip2( Stream output );
        #endregion
    }
}
