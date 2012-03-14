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

using Axiom.Core;
#if SILVERLIGHT
using System.Windows;
#endif

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

		/// <summary>Directory stack of previous directories </summary>
		private readonly Stack<string> _directoryStack = new Stack<string>();

		/// <summary>Base path; actually the same as Name, but for clarity </summary>
		protected string _basePath;

		protected virtual string CurrentDirectory { get; set; }

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

		protected virtual bool DirectoryExists( string directory )
		{
			return Directory.Exists( directory );
		}

		protected void SafeDirectoryChange( string directory, Action action )
		{
			if ( DirectoryExists( directory ) )
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
		protected virtual void findFiles( string pattern, bool recursive, List<string> simpleList, FileInfoList detailList, string currentDir )
		{
			if ( pattern == "" )
			{
				pattern = "*";
			}
			if ( currentDir == "" )
			{
				currentDir = this._basePath;
			}

			string[] files = getFiles( currentDir, pattern, recursive );

			foreach ( string file in files )
			{
				var fi = new System.IO.FileInfo( file );
				if ( simpleList != null )
				{
					simpleList.Add( fi.Name );
				}
				if ( detailList != null )
				{
					FileInfo fileInfo;
					fileInfo.Archive = this;
					fileInfo.Filename = fi.FullName;
					fileInfo.Basename = fi.FullName.Substring( Path.GetFullPath( currentDir ).Length );
					fileInfo.Path = currentDir;
					fileInfo.CompressedSize = fi.Length;
					fileInfo.UncompressedSize = fi.Length;
					fileInfo.ModifiedTime = fi.LastWriteTime;
					detailList.Add( fileInfo );
				}
			}
		}

		/// <summary>
		/// Returns the names of all files in the specified directory that match the specified search pattern, performing a recursive search
		/// </summary>
		/// <param name="dir">The directory to search.</param>
		/// <param name="pattern">The search string to match against the names of files in path.</param>
		protected virtual string[] getFiles( string dir, string pattern, bool recurse )
		{
			string[] files;
#if ( XBOX || XBOX360 || ANDROID || WINDOWS_PHONE )
			if ( !recurse )
			{				
				files = Directory.GetFiles( dir, pattern );
			}
			else
			{
				files = getFilesRecursively( dir, pattern );
			}
#elif SILVERLIGHT
			files = Directory.EnumerateFiles( dir, pattern, recurse ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly ).ToArray<string>();
#else
			files = Directory.GetFiles( dir, pattern, recurse ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly );
#endif
			return files;
		}

		/// <summary>
		/// Returns the names of all files in the specified directory that match the specified search pattern, performing a recursive search
		/// </summary>
		/// <param name="dir">The directory to search.</param>
		/// <param name="pattern">The search string to match against the names of files in path.</param>
		protected virtual string[] getFilesRecursively( string dir, string pattern )
		{
			var searchResults = new List<string>();
#if !SILVERLIGHT
			string[] folders = Directory.GetDirectories( dir );
			string[] files = Directory.GetFiles( dir );

			foreach ( string folder in folders )
			{
				searchResults.AddRange( getFilesRecursively( dir + Path.GetFileName( folder ) + "\\", pattern ) );
			}

			foreach ( string file in files )
			{
				string ext = Path.GetExtension( file );

				if ( pattern == "*" || pattern.Contains( ext ) )
				{
					searchResults.Add( file );
				}
			}
#endif

			return searchResults.ToArray();
		}

		/// <summary>Utility method to change the current directory </summary>
		protected void changeDirectory( string dir )
		{
#if !(SILVERLIGHT || WINDOWS_PHONE )
			Directory.SetCurrentDirectory( dir );
#else
			CurrentDirectory = dir;
#endif
		}

		/// <summary>Utility method to change directory and push the current directory onto a stack </summary>
		private void pushDirectory( string dir )
		{
			// get current directory and push it onto the stack
#if SILVERLIGHT || WINDOWS_PHONE
			_directoryStack.Push( CurrentDirectory );
#elif !( XBOX || XBOX360 )
			string cwd = Directory.GetCurrentDirectory();
			this._directoryStack.Push( cwd );
#endif
			changeDirectory( dir );
		}

		/// <summary>Utility method to pop a previous directory off the stack and change to it </summary>
		private void popDirectory()
		{
			if ( this._directoryStack.Count == 0 )
			{
#if !( XBOX || XBOX360 )
				throw new AxiomException( "No directories left in the stack." );
#else
				return;
#endif
			}
			string cwd = this._directoryStack.Pop();
			changeDirectory( cwd );
		}

		protected delegate void Action();

		#endregion Utility Methods

		#region Constructors and Destructors

		public FileSystemArchive( string name, string archType )
			: base( name, archType ) { }

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
#if SILVERLIGHT && !WINDOWS_PHONE
			if(!Application.Current.HasElevatedPermissions)
				throw new AxiomException( "FileSystem Access needs ElevatedPermissions!" );
#endif
			this._basePath = Path.GetFullPath( Name ) + Path.DirectorySeparatorChar;
			IsReadOnly = false;

			SafeDirectoryChange( this._basePath, () =>
												 {
													 try
													 {
#if !( SILVERLIGHT || WINDOWS_PHONE || XBOX || XBOX360 || ANDROID || IOS )
														 File.Create( this._basePath + @"__testWrite.Axiom", 1, FileOptions.DeleteOnClose );
#else
													File.Create( _basePath + @"__testWrite.Axiom", 1 );
													File.Delete( _basePath + @"__testWrite.Axiom" );
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
			string fullPath = this._basePath + Path.DirectorySeparatorChar + filename;
			bool exists = File.Exists( fullPath );
			if ( !exists || overwrite )
			{
				try
				{
#if !( SILVERLIGHT || WINDOWS_PHONE || XBOX || XBOX360 || ANDROID || IOS )
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

		public override Stream Open( string filename, bool readOnly )
		{
			Stream strm = null;

			SafeDirectoryChange( this._basePath, () =>
												 {
													 if ( File.Exists( this._basePath + filename ) )
													 {
														 var fi = new System.IO.FileInfo( this._basePath + filename );
														 strm = fi.Open( FileMode.Open, readOnly ? FileAccess.Read : FileAccess.ReadWrite );
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
			var ret = new List<string>();

			SafeDirectoryChange( this._basePath, () => findFiles( pattern, recursive, ret, null ) );

			return ret;
		}

		public override FileInfoList FindFileInfo( string pattern, bool recursive )
		{
			var ret = new FileInfoList();

			SafeDirectoryChange( this._basePath, () => findFiles( pattern, recursive, null, ret ) );

			return ret;
		}

		public override bool Exists( string fileName )
		{
			return File.Exists( this._basePath + fileName );
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
