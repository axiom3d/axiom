#region LGPL License
/*
Axiom Graphics Engine Library
Copyright (C) 2003-2006 Axiom Project Team

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
//     <license see="http://axiomengine.sf.net/wiki/index.php/license.txt"/>
//     <id value="$Id:"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.IO;

using Axiom.Core;
using Axiom.Graphics;

#endregion Namespace Declarations

namespace Axiom.FileSystem
{
    public class Watcher
    {
        #region Fields and Properties

        private FileSystemWatcher _fsWatcher;

        #endregion Fields and Properties

        #region Construction and Destruction

        public Watcher(string path, bool recurse)
        {
            // Create a new FileSystemWatcher and set its properties.
            _fsWatcher = new FileSystemWatcher();
            _fsWatcher.Path = path;
            /* Watch for changes in LastAccess and LastWrite times, and
               the renaming of files or directories. */
            _fsWatcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite
                                      | NotifyFilters.FileName | NotifyFilters.DirectoryName;
            // Watch all files.
            _fsWatcher.Filter = "*.*";
            _fsWatcher.IncludeSubdirectories = recurse;

            // Add event handlers.
            _fsWatcher.Changed += new FileSystemEventHandler( OnChanged );
            _fsWatcher.Created += new FileSystemEventHandler( OnChanged );
            _fsWatcher.Deleted += new FileSystemEventHandler( OnChanged );
            _fsWatcher.Renamed += new RenamedEventHandler( OnRenamed );

            // Begin watching.
            _fsWatcher.EnableRaisingEvents = true;

            LogManager.Instance.Write( "File monitor created for {0}.", path );
        }

        #endregion Construction and Destruction

        #region Methods

        private static void OnChanged(object source, FileSystemEventArgs e)
        {
            // Specify what is done when a file is changed, created, or deleted.
          LogManager.Instance.Write("File: " +  e.FullPath + " " + e.ChangeType);
        }

        private static void OnRenamed(object source, RenamedEventArgs e)
        {
            // Specify what is done when a file is renamed.
            LogManager.Instance.Write( "File: {0} renamed to {1}", e.OldFullPath, e.FullPath );
        }

        #endregion Methods
    }
}