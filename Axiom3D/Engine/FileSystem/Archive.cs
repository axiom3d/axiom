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

#region Namespace Declarations

using System;
using System.Collections.Generic;
using System.IO;

#endregion Namespace Declarations
			
namespace Axiom
{
    /// <summary>Information about a file/directory within the archive will be returned using a FileInfo struct.</summary>
    /// <see cref="Archive"/>
    public struct FileInfo 
    {
		/// The archive in which the file has been found (for info when performing
		/// multi-Archive searches, note you should still open through ResourceGroupManager)
		public Archive Archive;
        /// The file's fully qualified name
        public String Filename;
        /// Path name; separated by '/' and ending with '/'
        public String Path;
        /// Base filename
        public String Basename;
        /// Compressed size
        public uint CompressedSize;
        /// Uncompressed size
        public uint UncompressedSize;
    };

    public class FileInfoList : List<FileInfo> {}

	public interface IScriptLoader
	{

        /// <summary>
        /// Gets the file patterns which should be used to find scripts for this class.
        /// </summary>
        /// <remarks>
        /// This method is called when a resource group is loaded if you use 
        /// ResourceGroupManager::registerScriptLoader. Returns a list of file 
        /// patterns, in the order they should be searched in.
        /// </remarks>
		List<string> ScriptPatterns { get; }

        /// <summary>
        /// Parse a script file.
        /// </summary>
        /// <param name="stream">reference to a data stream which is the source of the script</param>
        /// <param name="groupName">
        /// The name of a resource group which should be used if any resources
        /// are created during the parse of this script.
        /// </param>
		void ParseScript(Stream stream, string groupName);

        /// <summary>
        /// Gets the relative loading order of scripts of this type.
        /// </summary>
        /// <remarks>
        /// There are dependencies between some kinds of scripts, and to enforce
        /// this all implementors of this interface must define a loading order. 
        /// Returns a value representing the relative loading order of these scripts
        /// compared to other script users, where higher values load later.
        /// </remarks>
		float LoadingOrder { get; }

	};

    /// <summary>
    ///    Summary description for Archive.
    /// </summary>
    public abstract class Archive : Resource
    {
        public Archive( string archiveName )
        {
            this.name = archiveName;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        public abstract Stream ReadFile( string fileName );

        public abstract string[] GetFileNamesLike( string startPath, string pattern );
    }
}
