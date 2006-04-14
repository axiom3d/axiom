using System;
using System.IO;
using System.Text;
using Axiom;
using Axiom.MathLib;

namespace Axiom
{
    /// <summary>
    /// Summary description for Serializer.
    /// </summary>
    public class Serializer
    {
        #region Fields

        /// <summary>
        ///		Version string of this serializer.
        /// </summary>
        protected string version;

        /// <summary>
        ///		Length of the chunk that is currently being processed.
        /// </summary>
        protected int currentChunkLength;

        /// <summary>
        ///		Chunk ID + size (short + long).
        /// </summary>
        public const int ChunkOverheadSize = 6;

        #endregion Fields

        #region Constructor

        /// <summary>
        ///		Default constructor.
        /// </summary>
        public Serializer()
        {
            // default binary file version
            version = "[Serializer_v1.00]";
        }

        #endregion Constructor

        #region Methods

        /// <summary>
        ///		Skips past a particular chunk.
        /// </summary>
        /// <remarks>
        ///		Only really used during development, when logic for handling particular chunks is not yet complete.
        /// </remarks>
        protected void IgnoreCurrentChunk( BinaryReader reader )
        {
            Seek( reader, currentChunkLength - ChunkOverheadSize );
        }

        /// <summary>
        ///		Reads a specified number of floats and copies them into the destination pointer.
        /// </summary>
        /// <param name="count">Number of values to read.</param>
        /// <param name="dest">Pointer to copy the values into.</param>
        protected void ReadBytes( BinaryReader reader, int count, IntPtr dest )
        {
            // blast the data into the buffer
            unsafe
            {
                byte* pointer = (byte*)dest.ToPointer();

                for ( int i = 0; i < count; i++ )
                {
                    pointer[i] = reader.ReadByte();
                }
            }
        }

        /// <summary>
        ///		Reads a specified number of floats and copies them into the destination pointer.
        /// </summary>
        /// <param name="count">Number of values to read.</param>
        /// <param name="dest">Pointer to copy the values into.</param>
        protected void ReadFloats( BinaryReader reader, int count, IntPtr dest )
        {
            // blast the data into the buffer
            unsafe
            {
                float* pointer = (float*)dest.ToPointer();

                for ( int i = 0; i < count; i++ )
                {
                    pointer[i] = reader.ReadSingle();
                }
            }
        }

        /// <summary>
        ///		Reads a specified number of shorts and copies them into the destination pointer.
        /// </summary>
        /// <remarks>This overload will also copy the values into the specified destination array.</remarks>
        /// <param name="count">Number of values to read.</param>
        /// <param name="dest">Pointer to copy the values into.</param>
        /// <param name="destArray">A float array that is to have the values copied into it at the same time as 'dest'.</param>
        protected void ReadFloats( BinaryReader reader, int count, IntPtr dest, float[] destArray )
        {
            // blast the data into the buffer
            unsafe
            {
                float* pointer = (float*)dest.ToPointer();

                for ( int i = 0; i < count; i++ )
                {
                    float val = reader.ReadSingle();
                    pointer[i] = val;
                    destArray[i] = val;
                }
            }
        }

        protected bool ReadBool( BinaryReader reader )
        {
            return reader.ReadBoolean();
        }

        protected float ReadFloat( BinaryReader reader )
        {
            return reader.ReadSingle();
        }

        protected int ReadInt( BinaryReader reader )
        {
            return reader.ReadInt32();
        }

        protected uint ReadUInt( BinaryReader reader )
        {
            return reader.ReadUInt32();
        }

        protected long ReadLong( BinaryReader reader )
        {
            return reader.ReadInt64();
        }

        protected ulong ReadULong( BinaryReader reader )
        {
            return reader.ReadUInt64();
        }

        protected short ReadShort( BinaryReader reader )
        {
            return reader.ReadInt16();
        }

        protected ushort ReadUShort( BinaryReader reader )
        {
            return reader.ReadUInt16();
        }

        /// <summary>
        ///		Reads a specified number of integers and copies them into the destination pointer.
        /// </summary>
        /// <param name="count">Number of values to read.</param>
        /// <param name="dest">Pointer to copy the values into.</param>
        protected void ReadInts( BinaryReader reader, int count, IntPtr dest )
        {
            // blast the data into the buffer
            unsafe
            {
                int* pointer = (int*)dest.ToPointer();

                for ( int i = 0; i < count; i++ )
                {
                    pointer[i] = reader.ReadInt32();
                }
            }
        }

        /// <summary>
        ///		Reads a specified number of shorts and copies them into the destination pointer.
        /// </summary>
        /// <param name="count">Number of values to read.</param>
        /// <param name="dest">Pointer to copy the values into.</param>
        protected void ReadShorts( BinaryReader reader, int count, IntPtr dest )
        {
            // blast the data into the buffer
            unsafe
            {
                short* pointer = (short*)dest.ToPointer();

                for ( int i = 0; i < count; i++ )
                {
                    pointer[i] = reader.ReadInt16();
                }
            }
        }

        /// <summary>
        ///		Reads from the stream up to the first endline character.
        /// </summary>
        /// <returns>A string formed from characters up to the first '\n' character.</returns>
        protected string ReadString( BinaryReader reader )
        {
            // note: Not using Environment.NewLine here, this character is specifically used in Ogre files.
            return ReadString( reader, '\n' );
        }

        /// <summary>
        ///		Reads from the stream up to the specified delimiter character.
        /// </summary>
        /// <param name="delimiter">The character that signals the end of the string.</param>
        /// <returns>A string formed from characters up to the first instance of the specified delimeter.</returns>
        protected string ReadString( BinaryReader reader, char delimiter )
        {
            StringBuilder sb = new StringBuilder();

            char c;

            // sift through each character until we hit the delimiter
            while ( ( c = reader.ReadChar() ) != delimiter )
            {
                sb.Append( c );
            }

            // return the accumulated string
            return sb.ToString();
        }

        /// <summary>
        ///    Reads and returns a Quaternion.
        /// </summary>
        /// <returns></returns>
        protected Quaternion ReadQuat( BinaryReader reader )
        {
            Quaternion quat = new Quaternion();

            quat.x = reader.ReadSingle();
            quat.y = reader.ReadSingle();
            quat.z = reader.ReadSingle();
            quat.w = reader.ReadSingle();

            return quat;
        }

        /// <summary>
        ///    Reads and returns a Vector3 structure.
        /// </summary>
        /// <returns></returns>
        protected Vector3 ReadVector3( BinaryReader reader )
        {
            Vector3 vector = new Vector3();

            vector.x = ReadFloat( reader );
            vector.y = ReadFloat( reader );
            vector.z = ReadFloat( reader );

            return vector;
        }

        /// <summary>
        ///    Reads and returns a Vector4 structure.
        /// </summary>
        /// <returns></returns>
        protected Vector4 ReadVector4( BinaryReader reader )
        {
            Vector4 vector = new Vector4();

            vector.x = ReadFloat( reader );
            vector.y = ReadFloat( reader );
            vector.z = ReadFloat( reader );
            vector.w = ReadFloat( reader );

            return vector;
        }

        /// <summary>
        ///		Reads a chunk ID and chunk size.
        /// </summary>
        /// <returns>The chunk ID at the current location.</returns>
        protected MeshChunkID ReadChunk( BinaryReader reader )
        {
            // get the chunk id
            short id = reader.ReadInt16();

            // read the length for this chunk
            currentChunkLength = reader.ReadInt32();

            return (MeshChunkID)id;
        }

        /// <summary>
        ///		Reads a file header and checks the version string.
        /// </summary>
        protected void ReadFileHeader( BinaryReader reader )
        {
            short headerID = 0;

            // read the header ID
            headerID = reader.ReadInt16();

            // better hope this is the header
            if ( headerID == (short)MeshChunkID.Header )
            {
                string fileVersion = ReadString( reader );

                // read the version string
                if ( version != fileVersion )
                {
                    throw new AxiomException( "Invalid file: version incompatible, file reports {0}, Serializer is version {1}", fileVersion, version );
                }
            }
            else
            {
                throw new AxiomException( "Invalid file: no header found." );
            }
        }

        protected void Seek( BinaryReader reader, long length )
        {
            Seek( reader, length, SeekOrigin.Current );
        }

        /// <summary>
        ///		Skips to a particular part of the binary stream.
        /// </summary>
        /// <param name="length">Number of bytes to skip.</param>
        protected void Seek( BinaryReader reader, long length, SeekOrigin origin )
        {
            if ( reader.BaseStream.CanSeek )
            {
                reader.BaseStream.Seek( length, origin );
            }
            else
            {
                throw new Exception( "Serializer only supports stream types that CanSeek." );
            }
        }

        protected bool IsEOF( BinaryReader reader )
        {
            return reader.PeekChar() == -1;
        }

        #endregion Methods
    }
}
