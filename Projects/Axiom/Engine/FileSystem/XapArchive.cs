#region LGPL License

/*
Axiom Graphics Engine Library
Copyright � 2003-2011 Axiom Project Team

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
//     <id value="$Id: XapArchive.cs 1537 2009-03-30 19:25:01Z borrillis $"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using Axiom.Core;

#endregion Namespace Declarations

namespace Axiom.FileSystem
{
    /// <summary>
    /// Specialization of the Archive class to allow reading of files from filesystem folders / directories.
    /// </summary>
    /// <ogre name="XapArchive">
    ///     <file name="OgreFileSystem.h"   revision="1.6.2.1" lastUpdated="5/18/2006" lastUpdatedBy="Borrillis" />
    ///     <file name="OgreFileSystem.cpp" revision="1.8" lastUpdated="5/18/2006" lastUpdatedBy="Borrillis" />
    /// </ogre>
    public class XapArchive : FileSystemArchive
    {
        #region Fields and Properties

        protected override string CurrentDirectory { get; set; }

        /// <summary>
        /// Is this archive capable of being monitored for additions, changes and deletions
        /// </summary>
        public override bool IsMonitorable
        {
            get
            {
                return false;
            }
        }

        #endregion Fields and Properties

        #region Utility Methods

        protected override bool DirectoryExists( string directory )
        {
            return true;
        }

        protected override IEnumerable<string> getFilesRecursively( string dir, string pattern,
                                                                    SearchOption searchOption )
        {
            var searchResults = new List<string>();
            var files = !pattern.Contains( "*" ) && Exists( dir + "/" + pattern )
                            ? new[]
                              {
                                  pattern
                              }
                            : new string[0]; //Directory.EnumerateFiles( dir );

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

        public XapArchive( string name, string archType )
            : base( name, archType )
        {
        }

        #endregion Constructors and Destructors

        #region Archive Implementation

        public override bool IsCaseSensitive
        {
            get
            {
                return true;
            }
        }

        public override void Load()
        {
            _basePath = Name + "/";
            IsReadOnly = true;
            SafeDirectoryChange( _basePath, () => IsReadOnly = true );
        }

        public override Stream Create( string filename, bool overwrite )
        {
            throw new AxiomException( "Cannot create a file in a read-only archive." );
        }

        public override Stream Open( string filename, bool readOnly )
        {
            if ( !readOnly )
            {
                throw new AxiomException( "Cannot create a file in a read-only archive." );
            }
            return Application.GetResourceStream( new Uri( _basePath + filename, UriKind.RelativeOrAbsolute ) ).Stream;
        }

        public override bool Exists( string fileName )
        {
            return Application.GetResourceStream( new Uri( _basePath + fileName, UriKind.RelativeOrAbsolute ) ) != null;
        }

        #endregion Archive Implementation
    }

    /// <summary>
    /// Specialization of IArchiveFactory for FileSystem files.
    /// </summary>
    /// <ogre name="XapArchiveFactory">
    ///     <file name="OgreFileSystem.h"   revision="1.6.2.1" lastUpdated="5/18/2006" lastUpdatedBy="Borrillis" />
    ///     <file name="OgreFileSystem.cpp" revision="1.8" lastUpdated="5/18/2006" lastUpdatedBy="Borrillis" />
    /// </ogre>
    public class XapArchiveFactory : ArchiveFactory
    {
        private const string _type = "Xap";

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
            return new XapArchive( name, _type );
        }

        public override void DestroyInstance( ref Archive obj )
        {
            obj.Dispose();
        }

        #endregion ArchiveFactory Implementation
    } ;
}