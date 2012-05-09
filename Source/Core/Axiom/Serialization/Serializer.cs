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
using System.Text;
using Axiom.Core;
using Axiom.Math;

#endregion Namespace Declarations

namespace Axiom.Serialization
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
			this.version = "[Serializer_v1.00]";
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
			Seek( reader, this.currentChunkLength - ChunkOverheadSize );
		}

		/// <summary>
		///		Reads a specified number of floats and copies them into the destination pointer.
		/// </summary>
		/// <param name="reader"></param>
		/// <param name="count">Number of values to read.</param>
		/// <param name="dest">Pointer to copy the values into.</param>
		protected void ReadBytes( BinaryReader reader, int count, BufferBase dest )
		{
			// blast the data into the buffer
#if !AXIOM_SAFE_ONLY
			unsafe
#endif
			{
				var pointer = dest.ToBytePointer();
#if !(XBOX || XBOX360)
				for ( var i = 0; i < count; i++ )
				{
					pointer[ i ] = reader.ReadByte();
				}
#else
				for ( var i = 0; i < count; i += 4 )
				{
					pointer[ i + 3 ] = reader.ReadByte();
					pointer[ i + 2 ] = reader.ReadByte();
					pointer[ i + 1 ] = reader.ReadByte();
					pointer[ i ] = reader.ReadByte();
				}
#endif
			}
		}

		/// <summary>
		///		Writes a specified number of bytes.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="count">Number of values to write.</param>
		/// <param name="src">Pointer that holds the values.</param>
		protected void WriteBytes( BinaryWriter writer, int count, BufferBase src )
		{
#if !AXIOM_SAFE_ONLY
			unsafe
#endif
			{
				var pointer = src.ToBytePointer();

				for ( var i = 0; i < count; i++ )
				{
					writer.Write( pointer[ i ] );
				}
			}
		}

		/// <summary>
		///		Reads a specified number of floats and copies them into the destination pointer.
		/// </summary>
		/// <param name="reader"></param>
		/// <param name="count">Number of values to read.</param>
		/// <param name="dest">Pointer to copy the values into.</param>
		protected void ReadFloats( BinaryReader reader, int count, BufferBase dest )
		{
			// blast the data into the buffer
#if !AXIOM_SAFE_ONLY
			unsafe
#endif
			{
				var pointer = dest.ToFloatPointer();

				for ( var i = 0; i < count; i++ )
				{
					pointer[ i ] = ReadFloat( reader );
				}
			}
		}

		/// <summary>
		///		Writes a specified number of floats.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="count">Number of values to write.</param>
		/// <param name="src">Pointer that holds the values.</param>
		protected void WriteFloats( BinaryWriter writer, int count, BufferBase src )
		{
#if !AXIOM_SAFE_ONLY
			unsafe
#endif
			{
				var pointer = src.ToFloatPointer();

				for ( var i = 0; i < count; i++ )
				{
					writer.Write( pointer[ i ] );
				}
			}
		}

		/// <summary>
		///		Reads a specified number of floats and copies them into the destination pointer.
		/// </summary>
		/// <remarks>This overload will also copy the values into the specified destination array.</remarks>
		/// <param name="reader"></param>
		/// <param name="count">Number of values to read.</param>
		/// <param name="dest">Pointer to copy the values into.</param>
		/// <param name="destArray">A float array that is to have the values copied into it at the same time as 'dest'.</param>
		protected void ReadFloats( BinaryReader reader, int count, BufferBase dest, float[] destArray )
		{
			// blast the data into the buffer
#if !AXIOM_SAFE_ONLY
			unsafe
#endif
			{
				var pointer = dest.ToFloatPointer();

				for ( var i = 0; i < count; i++ )
				{
					var val = ReadFloat( reader );
					pointer[ i ] = val;
					destArray[ i ] = val;
				}
			}
		}

		protected bool ReadBool( BinaryReader reader )
		{
			return reader.ReadBoolean();
		}

		protected void WriteBool( BinaryWriter writer, bool val )
		{
			writer.Write( val );
		}

		protected float ReadFloat( BinaryReader reader )
		{
			var inputdata = new byte[4];
			reader.Read( inputdata, 0, 4 );
			// The file format store integral types in little endian order
			if ( !BitConverter.IsLittleEndian )
			{
				Array.Reverse( inputdata, 0, 4 );
			}
			return BitConverter.ToSingle( inputdata, 0 );
			//return reader.ReadSingle();
		}

		protected void WriteFloat( BinaryWriter writer, float val )
		{
			writer.Write( val );
		}

		protected int ReadInt( BinaryReader reader )
		{
			var inputdata = new byte[4];
			reader.Read( inputdata, 0, 4 );
			// The file format store integral types in little endian order
			if ( !BitConverter.IsLittleEndian )
			{
				Array.Reverse( inputdata, 0, 4 );
			}
			return BitConverter.ToInt32( inputdata, 0 );
			//return reader.ReadInt32();
		}

		protected void WriteInt( BinaryWriter writer, int val )
		{
			writer.Write( val );
		}

		protected uint ReadUInt( BinaryReader reader )
		{
			var inputdata = new byte[4];
			reader.Read( inputdata, 0, 4 );
			// The file format store integral types in little endian order
			if ( !BitConverter.IsLittleEndian )
			{
				Array.Reverse( inputdata, 0, 4 );
			}
			return BitConverter.ToUInt32( inputdata, 0 );

			//return reader.ReadUInt32();
		}

		protected void WriteUInt( BinaryWriter writer, uint val )
		{
			writer.Write( val );
		}

		protected long ReadLong( BinaryReader reader )
		{
			var inputdata = new byte[8];
			reader.Read( inputdata, 0, 8 );
			// The file format store integral types in little endian order
			if ( !BitConverter.IsLittleEndian )
			{
				Array.Reverse( inputdata, 0, 8 );
			}
			return BitConverter.ToInt64( inputdata, 0 );

			//return reader.ReadInt64();
		}

		protected void WriteLong( BinaryWriter writer, long val )
		{
			writer.Write( val );
		}

		protected ulong ReadULong( BinaryReader reader )
		{
			var inputdata = new byte[8];
			reader.Read( inputdata, 0, 8 );
			// The file format store integral types in little endian order
			if ( !BitConverter.IsLittleEndian )
			{
				Array.Reverse( inputdata, 0, 8 );
			}
			return BitConverter.ToUInt64( inputdata, 0 );

			//return reader.ReadUInt64();
		}

		protected void WriteULong( BinaryWriter writer, ulong val )
		{
			writer.Write( val );
		}

		protected short ReadShort( BinaryReader reader )
		{
			var inputdata = new byte[2];
			reader.Read( inputdata, 0, 2 );
			// The file format store integral types in little endian order
			if ( !BitConverter.IsLittleEndian )
			{
				Array.Reverse( inputdata, 0, 2 );
			}
			return BitConverter.ToInt16( inputdata, 0 );

			//return reader.ReadInt16();
		}

		protected void WriteShort( BinaryWriter writer, short val )
		{
			writer.Write( val );
		}

		protected ushort ReadUShort( BinaryReader reader )
		{
			var inputdata = new byte[2];
			reader.Read( inputdata, 0, 2 );
			// The file format store integral types in little endian order
			if ( !BitConverter.IsLittleEndian )
			{
				Array.Reverse( inputdata, 0, 2 );
			}
			return BitConverter.ToUInt16( inputdata, 0 );

			//return reader.ReadUInt16();
		}

		protected void WriteUShort( BinaryWriter writer, ushort val )
		{
			writer.Write( val );
		}

		/// <summary>
		///		Reads a specified number of integers and copies them into the destination pointer.
		/// </summary>
		/// <param name="reader"></param>
		/// <param name="count">Number of values to read.</param>
		/// <param name="dest">Pointer to copy the values into.</param>
		protected void ReadInts( BinaryReader reader, int count, BufferBase dest )
		{
			// blast the data into the buffer
#if !AXIOM_SAFE_ONLY
			unsafe
#endif
			{
				var pointer = dest.ToIntPointer();

				for ( var i = 0; i < count; i++ )
				{
					pointer[ i ] = ReadInt( reader );
				}
			}
		}

		/// <summary>
		///		Writes a specified number of integers.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="count">Number of values to write.</param>
		/// <param name="src">Pointer that holds the values.</param>
		protected void WriteInts( BinaryWriter writer, int count, BufferBase src )
		{
			// blast the data into the buffer
#if !AXIOM_SAFE_ONLY
			unsafe
#endif
			{
				var pointer = src.ToIntPointer();

				for ( var i = 0; i < count; i++ )
				{
					writer.Write( pointer[ i ] );
				}
			}
		}

		/// <summary>
		///		Reads a specified number of shorts and copies them into the destination pointer.
		/// </summary>
		/// <param name="reader"></param>
		/// <param name="count">Number of values to read.</param>
		/// <param name="dest">Pointer to copy the values into.</param>
		protected void ReadShorts( BinaryReader reader, int count, BufferBase dest )
		{
			// blast the data into the buffer
#if !AXIOM_SAFE_ONLY
			unsafe
#endif
			{
				var pointer = dest.ToShortPointer();
				for ( var i = 0; i < count; i++ )
				{
					pointer[ i ] = ReadShort( reader );
				}
			}
		}

		/// <summary>
		///		Writes a specified number of shorts.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="count">Number of values to write.</param>
		/// <param name="src">Pointer that holds the values.</param>
		protected void WriteShorts( BinaryWriter writer, int count, BufferBase src )
		{
			// blast the data into the buffer
#if !AXIOM_SAFE_ONLY
			unsafe
#endif
			{
				var pointer = src.ToShortPointer();

				for ( var i = 0; i < count; i++ )
				{
					writer.Write( pointer[ i ] );
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
		///		Writes the string to the stream including the endline character.
		/// </summary>
		protected void WriteString( BinaryWriter writer, string str )
		{
			WriteString( writer, str, '\n' );
		}

		/// <summary>
		///		Reads from the stream up to the specified delimiter character.
		/// </summary>
		/// <param name="reader"></param>
		/// <param name="delimiter">The character that signals the end of the string.</param>
		/// <returns>A string formed from characters up to the first instance of the specified delimeter.</returns>
		protected string ReadString( BinaryReader reader, char delimiter )
		{
			var sb = new StringBuilder();

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
		///		Writes the string to the stream including the specified delimiter character.
		/// </summary>
		/// <param name="str"></param>
		/// <param name="delimiter">The character that signals the end of the string.</param>
		/// <param name="writer"></param>
		protected void WriteString( BinaryWriter writer, string str, char delimiter )
		{
			var sb = new StringBuilder( str );
			sb.Append( delimiter );
			writer.Write( sb.ToString().ToCharArray() );
		}

		/// <summary>
		///    Reads and returns a Quaternion.
		/// </summary>
		/// <returns></returns>
		protected Quaternion ReadQuat( BinaryReader reader )
		{
			var quat = new Quaternion();

			quat.x = ReadFloat( reader );
			quat.y = ReadFloat( reader );
			quat.z = ReadFloat( reader );
			quat.w = ReadFloat( reader );

			return quat;
		}

		/// <summary>
		///    Reads and returns a Quaternion.
		/// </summary>
		protected void WriteQuat( BinaryWriter writer, Quaternion quat )
		{
			writer.Write( quat.x );
			writer.Write( quat.y );
			writer.Write( quat.z );
			writer.Write( quat.w );
		}

		/// <summary>
		///    Reads and returns a Vector3 structure.
		/// </summary>
		/// <returns></returns>
		protected Vector3 ReadVector3( BinaryReader reader )
		{
			var vector = new Vector3();

			vector.x = ReadFloat( reader );
			vector.y = ReadFloat( reader );
			vector.z = ReadFloat( reader );

			return vector;
		}

		/// <summary>
		///    Writes a Vector3 structure.
		/// </summary>
		protected void WriteVector3( BinaryWriter writer, Vector3 vector )
		{
			WriteFloat( writer, vector.x );
			WriteFloat( writer, vector.y );
			WriteFloat( writer, vector.z );
		}

		/// <summary>
		///    Reads and returns a Vector4 structure.
		/// </summary>
		/// <returns></returns>
		protected Vector4 ReadVector4( BinaryReader reader )
		{
			var vector = new Vector4();

			vector.x = ReadFloat( reader );
			vector.y = ReadFloat( reader );
			vector.z = ReadFloat( reader );
			vector.w = ReadFloat( reader );

			return vector;
		}

		/// <summary>
		///    Writes a Vector4 structure.
		/// </summary>
		protected void WriteVector4( BinaryWriter writer, Vector4 vector )
		{
			WriteFloat( writer, vector.x );
			WriteFloat( writer, vector.y );
			WriteFloat( writer, vector.z );
			WriteFloat( writer, vector.w );
		}

		/// <summary>
		///		Reads a chunk ID and chunk size.
		/// </summary>
		/// <returns>The chunk ID at the current location.</returns>
		protected short ReadFileChunk( BinaryReader reader )
		{
			// get the chunk id
			var id = ReadShort( reader );
#if ( XBOX || XBOX360 )
			if (id == 0)
			{
				reader.BaseStream.Position -= 2;
				id = ReadShort(reader);
			}
#endif
			// read the length for this chunk
			this.currentChunkLength = ReadInt( reader );

			return id;
		}

		/// <summary>
		///		Writes a chunk ID and chunk size.  This would be more accurately named
		///     WriteChunkHeader, but this name is the counter of ReadChunk.
		/// </summary>
		protected void WriteChunk( BinaryWriter writer, MeshChunkID id, int chunkLength )
		{
			writer.Write( (short)id );
			writer.Write( chunkLength );
		}

		/// <summary>
		///		Writes a chunk ID and chunk size.  This would be more accurately named
		///     WriteChunkHeader, but this name is the counter of ReadChunk.
		/// </summary>
		protected void WriteChunk( BinaryWriter writer, SkeletonChunkID id, int chunkLength )
		{
			writer.Write( (short)id );
			writer.Write( chunkLength );
		}

		/// <summary>
		///		Reads a file header and checks the version string.
		/// </summary>
		protected void ReadFileHeader( BinaryReader reader )
		{
			short headerID = 0;

			// read the header ID
			headerID = ReadShort( reader );

			// better hope this is the header
			if ( headerID == (short)MeshChunkID.Header )
			{
				var fileVersion = ReadString( reader );

				// read the version string
				if ( this.version != fileVersion )
				{
					throw new AxiomException( "Invalid file: version incompatible, file reports {0}, Serializer is version {1}",
					                          fileVersion, this.version );
				}
			}
			else
			{
				throw new AxiomException( "Invalid file: no header found." );
			}
		}


		/// <summary>
		///		Writes a file header and version string.
		/// </summary>
		protected void WriteFileHeader( BinaryWriter writer, string fileVersion )
		{
			writer.Write( (short)MeshChunkID.Header );
			WriteString( writer, fileVersion );
		}

		protected void Seek( BinaryReader reader, long length )
		{
			Seek( reader, length, SeekOrigin.Current );
		}

		/// <summary>
		///		Skips to a particular part of the binary stream.
		/// </summary>
		/// <param name="reader"></param>
		/// <param name="length">Number of bytes to skip.</param>
		/// <param name="origin"></param>
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
			// [BUG:1710556]
			// The following commented line would usually work. However, due the fact that we are using PeekChar()
			// Mono and .Net are respecting the encoding type of the file (usually utf-8 ) which is causing this overflow.
			// To avoid this the EOF check is done by comparing the current Position to the Length of the file.
			// return reader.PeekChar() == -1;
			// References : 
			//		.Net : http://forums.microsoft.com/MSDN/ShowPost.aspx?PostID=127647&SiteID=1
			//		Mono : http://bugzilla.ximian.com/show_bug.cgi?id=76374
			//			  (google cache ) http://209.85.165.104/search?q=cache:ggELZnLcXpAJ:bugzilla.ximian.com/show_bug.cgi%3Fid%3D76374+An+error+ocurred+in+BinaryReader.PeekChar+()+method+when+execute+the+next&hl=en&ct=clnk&cd=1&gl=us
			return ( reader.BaseStream.Position == reader.BaseStream.Length );
		}

		#endregion Methods
	}
}