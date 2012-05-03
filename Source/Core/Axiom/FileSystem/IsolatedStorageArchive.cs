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
//     <id value="$Id: IsolatedStorageArchive.cs 1537 2009-03-30 19:25:01Z borrillis $"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;

using Axiom.Core;

#endregion Namespace Declarations

namespace Axiom.FileSystem
{
	public static class IsolatedStorageExtensionMethods
	{
#if !NET_40
		public static bool DirectoryExists( this IsolatedStorageFile isolatedStorage, string directory )
		{
			return isolatedStorage.GetDirectoryNames( directory ).Length != 0;
		}

		public static bool FileExists( this IsolatedStorageFile isolatedStorage, string fileName )
		{
			return File.Exists( RootDirectoryGet( isolatedStorage ) + fileName );
		}

		public static FileStream CreateFile( this IsolatedStorageFile isolatedStorage, string fileName )
		{
			return File.Create( RootDirectoryGet( isolatedStorage ) + fileName );
		}

		public static FileStream OpenFile( this IsolatedStorageFile isolatedStorage, string fileName, FileMode mode, FileAccess access )
		{
			return File.Open( RootDirectoryGet( isolatedStorage ) + fileName, mode, access );
		}

		private static readonly Class<IsolatedStorage>.Getter<String> RootDirectoryGet = Class<IsolatedStorage>.FieldGet<String>( "m_RootDir" );
#endif
	}

	/// <summary>
	/// </summary>
	public class IsolatedStorageArchive : FileSystemArchive
	{
		#region Fields and Properties

		private readonly IsolatedStorageFile isolatedStorage;

		#endregion Fields and Properties

		#region Utility Methods

		protected override bool DirectoryExists( string directory )
		{
			return isolatedStorage.DirectoryExists( directory );
		}

		protected override string[] getFiles( string dir, string pattern, bool recurse )
		{
			var searchResults = new List<string>();
			var folders = isolatedStorage.GetDirectoryNames( dir );
			var files = isolatedStorage.GetFileNames( dir );

			if ( recurse )
			{
				foreach ( var folder in folders )
				{
					searchResults.AddRange( getFilesRecursively( dir, pattern ) );
				}
			}
			else
			{
				foreach ( var file in files )
				{
					var ext = Path.GetExtension( file );

					if ( pattern == "*" || pattern.Contains( ext ) )
					{
						searchResults.Add( file );
					}
				}
			}
			return searchResults.ToArray();
		}

		protected override string[] getFilesRecursively( string dir, string pattern )
		{
			var searchResults = new List<string>();
			var folders = isolatedStorage.GetDirectoryNames( dir );
			var files = isolatedStorage.GetFileNames( dir );

			foreach ( var folder in folders )
			{
				searchResults.AddRange( getFilesRecursively( dir + Path.GetFileName( folder ) + Path.DirectorySeparatorChar, pattern ) );
			}

			foreach ( var file in files )
			{
				var ext = Path.GetExtension( file );

				if ( pattern == "*" || pattern.Contains( ext ) )
				{
					searchResults.Add( file );
				}
			}
			return searchResults.ToArray();
		}

		#endregion Utility Methods

		#region Constructors and Destructors

		public IsolatedStorageArchive( string name, string archType )
			: base( name, archType )
		{
			isolatedStorage = IsolatedStorageFile.GetUserStoreForApplication();
		}

		#endregion Constructors and Destructors

		#region Archive Implementation

		public override void Load()
		{
			_basePath = Name + "/";
			IsReadOnly = false;

			SafeDirectoryChange( _basePath, () =>
			                                {
			                                	try
			                                	{
			                                		isolatedStorage.CreateFile( _basePath + @"__testWrite.Axiom" );
			                                		isolatedStorage.DeleteFile( _basePath + @"__testWrite.Axiom" );
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

			var fullPath = _basePath + Path.DirectorySeparatorChar + filename;
			var exists = isolatedStorage.FileExists( fullPath );
			if ( !exists || overwrite )
			{
				try
				{
					return isolatedStorage.CreateFile( fullPath );
				}
				catch ( Exception ex )
				{
					throw new AxiomException( "Failed to open file : " + filename, ex );
				}
			}
			return Open( fullPath, false );
		}

		public override Stream Open( string filename, bool readOnly )
		{
			Stream strm = null;

			SafeDirectoryChange( _basePath, () =>
			                                {
			                                	if ( isolatedStorage.FileExists( _basePath + filename ) )
			                                	{
			                                		strm = isolatedStorage.OpenFile( _basePath + filename, FileMode.Open, readOnly ? FileAccess.Read : FileAccess.ReadWrite );
			                                	}
			                                } );
			return strm;
		}

		public override bool Exists( string fileName )
		{
			return isolatedStorage.FileExists( _basePath + fileName );
		}

		#endregion Archive Implementation
	}

	/// <summary>
	/// Specialization of IArchiveFactory for IsolatedStorage files.
	/// </summary>
	public class IsolatedStorageArchiveFactory : ArchiveFactory
	{
		private const string _type = "IsolatedStorage";

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
			return new IsolatedStorageArchive( name, _type );
		}

		public override void DestroyInstance( ref Archive obj )
		{
			obj.Dispose();
		}

		#endregion ArchiveFactory Implementation
	};
}
