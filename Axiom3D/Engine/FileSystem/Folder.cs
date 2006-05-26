#region LGPL License
/*
Axiom Game Engine Library
Copyright (C) 2003  Axiom Project Team

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

#region Namespace Declarations
using System.Collections;
using System.IO;
using Axiom;
#endregion

namespace Axiom
{
    /// <summary>
    /// Represents a file system folder.
    /// </summary>
    public class Folder : Archive
    {

        internal Folder( string archiveName )
            : base( archiveName )
        {
            // So that the substring will work out right, we definitely want this string
            // to end with the directory separator char.
            //if (!name.EndsWith(Path.DirectorySeparatorChar.ToString())) {
            //    name += Path.DirectorySeparatorChar;
            ///}
        }

        public override void Load()
        {
            LogManager.Instance.Write( "FileSystem codec for {0} created.", name );

            isLoaded = true;
        }

        public override Stream ReadFile( string fileName )
        {
            FileStream file = File.OpenRead( name + Path.DirectorySeparatorChar + fileName );

            return file;
        }

        public override string[] GetFileNamesLike( string startPath, string pattern )
        {
            // replace with wildcard if empty
            if ( pattern.Length == 0 )
            {
                pattern = "*.*";
            }
            // otherwise prefix with a star as a wildcard
            else if ( pattern.IndexOf( "*" ) == -1 )
            {
                pattern = "*" + pattern;
            }

            // Append the start path if there is one
            string path = Path.Combine( name, startPath );

            // Get the list of files, recursively, into an ArrayList
            ArrayList files = new ArrayList();
            GetFiles( path, pattern, files, false );//XEONX FIX: Dont recurse as will cause materials to be added as many times as the directory is nested for dynamically inspected content
            //and this is inconsistent with the way that the media files such as icons will only be added once and will not be found through recursion
            //without this fix, dynanmic inspection of directories not be possible

            // Copy the ArrayList into a string[] array suitable for returning.
            string[] retval = new string[files.Count];

            for ( int i = 0; i < files.Count; i++ )
            {
                retval[i] = ( (string)files[i] ).Replace( '\\', '/' );
            }

            return retval;
        }

        private void GetFiles( string path, string pattern, ArrayList files, bool recurse )
        {
            string[] newFiles = Directory.GetFiles( path, pattern );

            foreach ( string newFile in newFiles )
            {
                if ( !newFile.StartsWith( name ) )
                {
                    throw new AxiomException( "Directory {0} unexpectedly does not start from path {1}", newFile, name );
                }

                files.Add( newFile.Substring( name.Length + 1 ) );
            }

            if ( recurse )
            {
                foreach ( string directory in Directory.GetDirectories( path ) )
                {
                    GetFiles( directory, pattern, files, true );
                }
            }
        }
    }
}
