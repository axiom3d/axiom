#region LGPL License
/*
Axiom Game Engine Library
Copyright (C) 2003  Axiom Project Team

The overall design, and a majority of the core engine and rendering code 
contained within this library is a derivative of the open source Object Oriented 
Graphics Engine OGRE, which can be found at http://ogre.sourceforge.net.  
Many thanks to the OGRE team for maintaining such a high quality project.

The .zip archive functionality uses a dynamically linked version of
SharpZipLib (http://www.icsharpcode.net/OpenSource/SharpZipLib/Default.aspx.

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

using System;
using System.Collections;
using System.Collections.Specialized;
using System.IO;
using ICSharpCode.SharpZipLib.Zip;

namespace Axiom.FileSystem {
    /// <summary>
    ///    Implementation of Archive that allows for reading resources from a .zip file.
    /// </summary>
    /// <remarks>
    ///    This would also be suitable for reading other .zip like formats, including
    ///     .pak3.
    /// </remarks>
    public class Zip : Archive {
        public Zip(string archiveName) : base(archiveName) {
        }

        public override void Load() {
            // do nothing
        }

        /// <summary>
        ///    Reads a file with the specified name in the .zip file and returns the
        ///    file as a MemoryStream.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public override Stream ReadFile(string name) {

            FileStream fs = File.OpenRead(archiveName);
            fs.Position = 0;
            ZipInputStream s = new ZipInputStream(fs);
            ZipEntry entry;

            MemoryStream output = new MemoryStream();

            entry = s.GetNextEntry();

            while (entry != null) {
                
                if(entry.Name.ToLower() == name.ToLower()) {
                    break;
                }

                // look at the next file in the list
                entry = s.GetNextEntry();
            }

            if(entry == null) {
                return null;
            }

            int size = 2048;
            byte[] data = new byte[2048];
            while (true) {
                size = s.Read(data, 0, data.Length);
                if (size > 0) {
                    output.Write(data, 0, size);
                } 
                else {
                    break;
                }
            }

            return output;
        }

        /// <summary>
        ///    Returns a list of files matching the specified pattern (usually extension) located
        ///    within this .zip file.
        /// </summary>
        /// <param name="startPath"></param>
        /// <param name="pattern"></param>
        /// <returns></returns>
        public override string[] GetFileNamesLike(string startPath, string pattern) {
            FileStream fs = File.OpenRead(archiveName);
            fs.Position = 0;
            ZipInputStream s = new ZipInputStream(fs);
            ZipEntry entry;
            StringCollection fileList = new StringCollection();

            entry = s.GetNextEntry();

            while (entry != null) {

                // get the full path for the output file
                string file = Path.GetFileName(entry.Name);
				
                if(file.EndsWith(pattern)) {
                    fileList.Add(file);
                }

                entry = s.GetNextEntry();
            }
            s.Close();

            string[] files = new string[fileList.Count];
            fileList.CopyTo(files, 0);

            return files;
        }
    }
}
