#region LGPL License
/*
Axiom Graphics Engine Library
Copyright (C) 2003-2006  Axiom Project Team

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
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <id value="$Id: ZipArchive.cs 1537 2009-03-30 19:25:01Z borrillis $"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;

using ICSharpCode.SharpZipLib.Zip;

#endregion Namespace Declarations

namespace Axiom.FileSystem
{

	/// <summary>
	/// Specialization of the Archive class to allow reading of files from from a zip format source archive.
	/// </summary>
	/// <remarks>
	/// This archive format supports all archives compressed in the standard
	/// zip format, including iD pk3 files.
	/// </remarks>
	/// <ogre name="ZipArchive">
	///     <file name="OgreZip.h"   revision="" lastUpdated="5/18/2006" lastUpdatedBy="Borrillis" />
	///     <file name="OgreZip.cpp" revision="" lastUpdated="5/18/2006" lastUpdatedBy="Borrillis" />
	/// </ogre> 
	public class ZipArchive : Archive
	{
		#region Fields and Properties

		/// <summary>
		/// root location of the zip file.
		/// </summary>
		private string _zipFile;
		private string _zipDir = "/";
		private ZipInputStream _zipStream;
		private List<FileInfo> _fileList = new List<FileInfo>();

		/// <summary>Directory stack of previous directories </summary>
		private Stack<string> _directoryStack;

		#endregion Fields and Properties

		#region Utility Methods

		/// <overloads><summary>
		/// Utility method to retrieve all files in a directory matching pattern.
		/// </summary>
		/// <param name="pattern">File pattern</param>
		/// <param name="recursive">Whether to cascade down directories</param>
		/// <param name="simpleList">Populated if retrieving a simple list</param>
		/// <param name="detailList">Populated if retrieving a detailed list</param>
		/// </overloads>
		protected void findFiles( string pattern, bool recursive, List<string> simpleList, FileInfoList detailList )
		{
			findFiles( pattern, recursive, simpleList, detailList, "" );
		}
		/// <param name="currentDir">The current directory relative to the base of the archive, for file naming</param>
		protected void findFiles( string pattern, bool recursive, List<string> simpleList, FileInfoList detailList, string currentDir )
		{
			if ( currentDir == "" )
				currentDir = _zipDir;
			FileStream fs = File.OpenRead( _zipFile );
			_zipStream = new ZipInputStream( fs );

			ZipEntry entry = _zipStream.GetNextEntry();
			if ( pattern[ 0 ] == '*' )
				pattern = pattern.Substring( 1 );
			Regex ex = new Regex( pattern );

			while ( entry != null )
			{
				// get the full path for the output file
				string file = entry.Name;
				if ( ex.IsMatch( file ) )
				{
					if ( simpleList != null )
					{
						simpleList.Add( file );
					}
					if ( detailList != null )
					{
						FileInfo fileInfo;
						fileInfo.Archive = this;
						fileInfo.Filename = entry.Name;
						fileInfo.Basename = Path.GetFileName( entry.Name );
						fileInfo.Path = Path.GetDirectoryName( entry.Name ) + Path.DirectorySeparatorChar;
						fileInfo.CompressedSize = entry.CompressedSize;
						fileInfo.UncompressedSize = entry.Size;
						detailList.Add( fileInfo );
					}

				}

				entry = _zipStream.GetNextEntry();
			}
		}

		#endregion Utility Methods

		#region Constructors and Destructor

		public ZipArchive( string name, string archType )
			: base( name, archType )
		{
		}

		~ZipArchive()
		{
			Unload();
		}

		#endregion Constructors and Destructor

		#region Archive Implementation

		public override bool IsCaseSensitive
		{
			get
			{
				return false;
			}
		}

		public override void Load()
		{
			if ( _zipFile == null || _zipFile.Length == 0 )
			{
				_zipFile = Path.GetFullPath( Name );

				// read the open the zip archive
				FileStream fs = File.OpenRead( _zipFile );
				fs.Position = 0;

				// get a input stream from the zip file
				_zipStream = new ZipInputStream( fs );
				//ZipEntry entry = _zipStream.GetNextEntry();
				//Regex ex = new Regex( pattern );

				//while ( entry != null )
				//{
				//    // get the full path for the output file
				//    string file = entry.Name;
				//    if ( ex.IsMatch( file ) )
				//    {
				//        FileInfo fileInfo;
				//        fileInfo.Archive = this;
				//        fileInfo.Filename = entry.Name;
				//        fileInfo.Basename = Path.GetFileName( entry.Name );
				//        fileInfo.Path = Path.GetDirectoryName( entry.Name ) + Path.DirectorySeparatorChar;
				//        fileInfo.CompressedSize = entry.CompressedSize;
				//        fileInfo.UncompressedSize = entry.Size;
				//        _fileList.Add( fileInfo );
				//    }

				//    entry = _zipStream.GetNextEntry();
				//}

			}
		}

		public override void Unload()
		{
			if ( _zipStream != null )
			{
				_zipStream.Close();
				_zipStream.Dispose();
				_zipStream = null;
			}
		}

		public override Stream Open( string filename, bool readOnly )
		{
			ZipEntry entry;

			// we will put the decompressed data into a memory stream
			MemoryStream output = new MemoryStream();
			FileStream fs = File.OpenRead( _zipFile );
			_zipStream = new ZipInputStream( fs );

			// get the first entry 
			entry = _zipStream.GetNextEntry();

			// loop through all the entries until we find the requested one
			while ( entry != null )
			{
				if ( entry.Name.ToLower() == filename.ToLower() )
				{
					break;
				}

				// look at the next file in the list
				entry = _zipStream.GetNextEntry();
			}

			if ( entry == null )
			{
				return null;
			}

			// write the data to the output stream
			int size = 2048;
			byte[] data = new byte[ 2048 ];
			while ( true )
			{
				size = _zipStream.Read( data, 0, data.Length );
				if ( size > 0 )
				{
					output.Write( data, 0, size );
				}
				else
				{
					break;
				}
			}

			// reset the position to make sure it is at the beginning of the stream
			output.Position = 0;
			return output;
		}

		public override List<string> List( bool recursive )
		{
			return Find( "*", recursive );
		}

		public override FileInfoList ListFileInfo( bool recursive )
		{
			return FindFileInfo( "*", recursive );
		}

		public override List<string> Find( string pattern, bool recursive )
		{
			List<string> ret = new List<string>();

			findFiles( pattern, recursive, ret, null );

			return ret;
		}

		public override FileInfoList FindFileInfo( string pattern, bool recursive )
		{
			FileInfoList ret = new FileInfoList();

			findFiles( pattern, recursive, null, ret );

			return ret;
		}

		public override bool Exists( string fileName )
		{
			List<string> ret = new List<string>();

			findFiles( fileName, false, ret, null );

			return (bool)( ret.Count > 0 );
		}

		#endregion Archive Implementation

	}

	/// <summary>
	/// Specialization of ArchiveFactory for Zip files.
	/// </summary>
	/// <ogre name="ZipArchive">
	///     <file name="OgreZip.h"   revision="" lastUpdated="5/18/2006" lastUpdatedBy="Borrillis" />
	///     <file name="OgreZip.cpp" revision="" lastUpdated="5/18/2006" lastUpdatedBy="Borrillis" />
	/// </ogre> 
	public class ZipArchiveFactory : ArchiveFactory
	{
		private const string _type = "ZipFile";

		#region ArchiveFactory Implementation

		public override string Type
		{
			get
			{
				return _type;
			}
		}

		public override Archive CreateInstance( string name )
		{
			return new ZipArchive( name, _type );
		}

		public override void DestroyInstance( Archive obj )
		{
			obj.Dispose();
		}

		#endregion
	};
}
