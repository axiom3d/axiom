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
    /// <summary>
    /// Specialization of the Archive class to allow reading of files from filesystem folders / directories.
    /// </summary>
    /// <ogre name="IsolatedStorageArchive">
    ///     <file name="OgreFileSystem.h"   revision="1.6.2.1" lastUpdated="5/18/2006" lastUpdatedBy="Borrillis" />
    ///     <file name="OgreFileSystem.cpp" revision="1.8" lastUpdated="5/18/2006" lastUpdatedBy="Borrillis" />
    /// </ogre>
    public class IsolatedStorageArchive : FileSystemArchive
    {
        #region Fields and Properties

        private readonly IsolatedStorageFile isolatedStorage;

        #endregion Fields and Properties

        #region Utility Methods

#if NET_40
        protected override bool DirectoryExists( string directory )
        {
            return isolatedStorage.DirectoryExists( directory );
        }

        private bool FileExists(string fileName)
        {
            return isolatedStorage.FileExists( _basePath + fileName );
        }

        private FileStream CreateFile( string fileName)
        {
            return isolatedStorage.CreateFile( _basePath + fileName );
        }

        private FileStream OpenFile(string fileName, bool readOnly)
        {
            return isolatedStorage.OpenFile( _basePath + fileName, FileMode.Open,
                                             readOnly ? FileAccess.Read : FileAccess.ReadWrite );
        }
#else
        protected override bool DirectoryExists(string directory)
        {
            return isolatedStorage.GetDirectoryNames(directory).Length != 0;
        }

        private bool FileExists(string fileName)
        {
            return File.Exists(RootDirectory + _basePath + fileName);
        }

        private FileStream CreateFile(string fileName)
        {
            return File.Create(RootDirectory + _basePath + fileName);
        }

        private FileStream OpenFile(string fileName, bool readOnly)
        {
            return File.Open(RootDirectory + _basePath + fileName, FileMode.Open,
                              readOnly ? FileAccess.Read : FileAccess.ReadWrite);
        }

        public static readonly Class<IsolatedStorage>.Getter<String> RootDirectoryGet =
            Class<IsolatedStorage>.FieldGet<String>( "m_RootDir" );

        private string RootDirectory
        {
            get
            {
                return RootDirectoryGet(isolatedStorage);
            }
        }
#endif

        protected override IEnumerable<string> getFilesRecursively( string dir, string pattern,
                                                                    SearchOption searchOption )
        {
            var searchResults = new List<string>();
            var folders = isolatedStorage.GetDirectoryNames( dir );
            var files = isolatedStorage.GetFileNames( dir );

            if ( searchOption == SearchOption.AllDirectories )
            {
                foreach ( var folder in folders )
                {
                    searchResults.AddRange(
                        getFilesRecursively( dir + Path.GetFileName( folder ) + Path.DirectorySeparatorChar, pattern,
                                             searchOption ) );
                }
            }

            foreach ( var file in files )
            {
                var ext = Path.GetExtension( file );

                if ( pattern == "*" || pattern.Contains( ext ) )
                {
                    searchResults.Add( file );
                }
            }

            return searchResults;
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

            SafeDirectoryChange(
                _basePath,
                () =>
                {
                    try
                    {
                        CreateFile(_basePath + @"__testWrite.Axiom");
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
            var exists = FileExists( fullPath );
            if ( !exists || overwrite )
            {
                try
                {
                    return CreateFile( fullPath );
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

            SafeDirectoryChange(
                _basePath,
                () =>
                {
                    if ( FileExists( _basePath + filename ) )
                    {
                        strm = OpenFile( _basePath + filename, readOnly );
                    }
                } );
            return strm;
        }

        public override bool Exists( string fileName )
        {
            return FileExists( _basePath + fileName );
        }

        #endregion Archive Implementation
    }

    /// <summary>
    /// Specialization of IArchiveFactory for FileSystem files.
    /// </summary>
    /// <ogre name="IsolatedStorageArchiveFactory">
    ///     <file name="OgreFileSystem.h"   revision="1.6.2.1" lastUpdated="5/18/2006" lastUpdatedBy="Borrillis" />
    ///     <file name="OgreFileSystem.cpp" revision="1.8" lastUpdated="5/18/2006" lastUpdatedBy="Borrillis" />
    /// </ogre>
    public class IsolatedStorageArchiveFactory : ArchiveFactory
    {
        private const string _type = "Isolated";

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
    } ;
}