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
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <id value="$Id: FileSystemArchive.cs 1537 2009-03-30 19:25:01Z borrillis $"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Axiom.Core;

#endregion Namespace Declarations

namespace Axiom.FileSystem
{
	/// <summary>
	/// Specialization of the Archive class to allow reading of files from filesystem folders / directories.
	/// </summary>
	/// <ogre name="FileSystemArchive">
	///     <file name="OgreFileSystem.h"   revision="1.6.2.1" lastUpdated="5/18/2006" lastUpdatedBy="Borrillis" />
	///     <file name="OgreFileSystem.cpp" revision="1.8" lastUpdated="5/18/2006" lastUpdatedBy="Borrillis" />
	/// </ogre>
	public class FileSystemArchive : Archive
	{
		#region Fields and Properties

		/// <summary>Base path; actually the same as Name, but for clarity </summary>
		private string _basePath;

		/// <summary>Directory stack of previous directories </summary>
		private Stack<string> _directoryStack = new Stack<string>();

		/// <summary>
		/// Is this archive capable of being monitored for additions, changes and deletions
		/// </summary>
		public override bool IsMonitorable
		{
			get
			{
				return true;
			}
		}

		#endregion Fields and Properties

		#region Utility Methods

		protected delegate void Action();

		protected void SafeDirectoryChange( string directory, Action action )
		{
			if ( Directory.Exists( directory ) )
			{
				// Check we can change to it
				pushDirectory( directory );

				try
				{
					action();
				}
				catch ( Exception ex )
				{
					LogManager.Instance.Write( LogManager.BuildExceptionString( ex ) );
				}
				finally
				{
					// return to previous
					popDirectory();
				}
			}
		}

		/// <overloads>
		/// <summary>
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

	    /// <param name="detailList"></param>
	    /// <param name="currentDir">The current directory relative to the base of the archive, for file naming</param>
	    /// <param name="pattern"></param>
	    /// <param name="recursive"></param>
	    /// <param name="simpleList"></param>
	    protected void findFiles( string pattern, bool recursive, List<string> simpleList, FileInfoList detailList, string currentDir )
		{
			if ( pattern == "" )
				pattern = "*";
			if ( currentDir == "" )
				currentDir = _basePath;

			string[] files;

#if !( XBOX || XBOX360 || ANDROID )
			files = Directory.GetFiles( currentDir, pattern, recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly );
#else
			files = recursive ? this.getFilesRecursively(currentDir, pattern) : Directory.GetFiles(currentDir, pattern);
#endif
			foreach ( string file in files )
			{
				System.IO.FileInfo fi = new System.IO.FileInfo( file );
				if ( simpleList != null )
				{
					simpleList.Add( fi.Name );
				}
				if ( detailList != null )
				{
					FileInfo fileInfo;
					fileInfo.Archive = this;
					fileInfo.Filename = fi.FullName;
					fileInfo.Basename = fi.FullName.Substring( Path.GetFullPath(currentDir).Length );
					fileInfo.Path = currentDir;
					fileInfo.CompressedSize = fi.Length;
					fileInfo.UncompressedSize = fi.Length;
                    fileInfo.ModifiedTime = fi.LastWriteTime;
					detailList.Add( fileInfo );
				}
			}
		}

#if ( XBOX || XBOX360 ||ANDROID )
		/// <summary>
		/// Returns the names of all files in the specified directory that match the specified search pattern, performing a recursive search
		/// </summary>
		/// <param name="dir">The directory to search.</param>
		/// <param name="pattern">The search string to match against the names of files in path.</param>
		private string[] getFilesRecursively( string dir, string pattern )
		{
			List<string> searchResults = new List<string>();
			string[] folders = Directory.GetDirectories( dir );
			string[] files = Directory.GetFiles( dir );

			foreach ( string folder in folders )
			{
				searchResults.AddRange( this.getFilesRecursively( dir + Path.GetFileName( folder ) + "\\", pattern ) );
			}

			foreach ( string file in files )
			{
				string ext = Path.GetExtension( file );

				if ( pattern == "*" || pattern.Contains( ext ) )
					searchResults.Add( file );
			}

			return searchResults.ToArray();
		}
#endif

		/// <summary>Utility method to change the current directory </summary>
		protected void changeDirectory( string dir )
		{
			Directory.SetCurrentDirectory( dir );
		}

		/// <summary>Utility method to change directory and push the current directory onto a stack </summary>
		void pushDirectory( string dir )
		{
			// get current directory and push it onto the stack
#if !( XBOX || XBOX360 )
			string cwd = Directory.GetCurrentDirectory();
			_directoryStack.Push( cwd );
#endif
			changeDirectory( dir );
		}

		/// <summary>Utility method to pop a previous directory off the stack and change to it </summary>
		void popDirectory()
		{
			if ( _directoryStack.Count == 0 )
			{
#if !( XBOX || XBOX360 )
				throw new AxiomException( "No directories left in the stack." );
#else
				return;
#endif
			}
			string cwd = _directoryStack.Pop();
			changeDirectory( cwd );
		}

		#endregion Utility Methods

		#region Constructors and Destructors

		public FileSystemArchive( string name, string archType )
			: base( name, archType )
		{
		}

		~FileSystemArchive()
		{
			Unload();
		}

		#endregion Constructors and Destructors

		#region Archive Implementation

		public override bool IsCaseSensitive
		{
			get
			{
				return !PlatformManager.IsWindowsOS;
			}
		}

		public override void Load()
		{
			_basePath = Path.GetFullPath( Name ) + Path.DirectorySeparatorChar;
			IsReadOnly = false;

			SafeDirectoryChange( _basePath, () =>
											{
												try
												{

#if !( XBOX || XBOX360 || ANDROID )
													File.Create( _basePath + @"__testWrite.Axiom", 1, FileOptions.DeleteOnClose );
#else
													File.Create(_basePath + @"__testWrite.Axiom", 1 );
#endif
												}
												catch ( Exception )
												{
													IsReadOnly = true;
												}
											} );

		}

		public override Stream Create( string filename, bool overwrite )
		{
			if ( IsReadOnly )
			{
				throw new AxiomException( "Cannot create a file in a read-only archive." );
			}

			Stream stream = null;
			string fullPath = _basePath + Path.DirectorySeparatorChar + filename;
			bool exists = File.Exists( fullPath );
			if ( !exists || overwrite )
			{
				try
				{
#if !( XBOX || XBOX360 || ANDROID )
					stream = File.Create( fullPath, 1, FileOptions.RandomAccess );
#else
					stream = File.Create( fullPath, 1 );
#endif
				}
				catch ( Exception ex )
				{
					throw new AxiomException( "Failed to open file : " + filename, ex );
				}
			}
			else
			{
				stream = Open( fullPath, false );
			}

			return stream;
		}

		public override void Unload()
		{
			// Nothing to do here.
		}

		public override System.IO.Stream Open( string filename, bool readOnly )
		{
			Stream strm = null;

			SafeDirectoryChange( _basePath, () =>
											{
												if ( File.Exists( _basePath + filename ) )
												{
													System.IO.FileInfo fi = new System.IO.FileInfo( _basePath + filename );
													strm = (Stream)fi.Open( FileMode.Open, readOnly ? FileAccess.Read : FileAccess.ReadWrite );
												}
											} );

			return strm;
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

			SafeDirectoryChange( _basePath, () => findFiles( pattern, recursive, ret, null ) );

			return ret;
		}

		public override FileInfoList FindFileInfo( string pattern, bool recursive )
		{
			FileInfoList ret = new FileInfoList();

			SafeDirectoryChange( _basePath, () => findFiles( pattern, recursive, null, ret ) );

			return ret;
		}

		public override bool Exists( string fileName )
		{
			return File.Exists( _basePath + fileName );
		}

		#endregion Archive Implementation
	}

	/// <summary>
	/// Specialization of IArchiveFactory for FileSystem files.
	/// </summary>
	/// <ogre name="FileSystemArchiveFactory">
	///     <file name="OgreFileSystem.h"   revision="1.6.2.1" lastUpdated="5/18/2006" lastUpdatedBy="Borrillis" />
	///     <file name="OgreFileSystem.cpp" revision="1.8" lastUpdated="5/18/2006" lastUpdatedBy="Borrillis" />
	/// </ogre>
	public class FileSystemArchiveFactory : ArchiveFactory
	{
		private const string _type = "Folder";

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
			return new FileSystemArchive( name, _type );
		}

		public override void DestroyInstance( ref Archive obj )
		{
			obj.Dispose();
		}

		#endregion ArchiveFactory Implementation
	};
}