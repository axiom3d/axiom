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

#endregion

#region SVN Version Information

// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <id value="$Id$"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System.Collections.Generic;
#if (XBOX || XBOX360)
using System.IO.IsolatedStorage;
#endif
using System.Linq;
using System.Text.RegularExpressions;
using System.IO;
using Axiom.Core;
using Ionic.Zip;

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
        protected string _zipFile;

        protected string _zipDir = "/";
        protected ZipFile _zipStream;
        protected List<FileInfo> _fileList = new List<FileInfo>();

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
        protected void findFiles(string pattern, bool recursive, List<string> simpleList, FileInfoList detailList)
        {
            findFiles(pattern, recursive, simpleList, detailList, "");
        }

        /// <param name="detailList"></param>
        /// <param name="currentDir">The current directory relative to the base of the archive, for file naming</param>
        /// <param name="pattern"></param>
        /// <param name="recursive"></param>
        /// <param name="simpleList"></param>
        protected void findFiles(string pattern, bool recursive, List<string> simpleList, FileInfoList detailList,
                                  string currentDir)
        {
            if (currentDir == "")
            {
                currentDir = this._zipDir;
            }

            Load();
            if (pattern.Contains("*"))
            {
                pattern = pattern.Replace(".", @"\.").Replace("*", ".*") + "$";
            }


            var ex = new Regex(pattern);
            var files = from entry in _zipStream.Entries
                        where ex.IsMatch(entry.FileName)
                        select new FileInfo
                        {
                            Archive = this,
                            Filename = entry.FileName,
                            Basename = Path.GetFileName(entry.FileName),
                            Path = Path.GetDirectoryName(entry.FileName) + Path.DirectorySeparatorChar,
                            CompressedSize = entry.CompressedSize,
                            UncompressedSize = entry.UncompressedSize,
                            ModifiedTime = entry.CreationTime
                        };
            if (detailList != null)
            {
                detailList.AddRange(files);
            }

            if (simpleList != null)
            {
                simpleList.AddRange(from file in files
                                    select file.Filename);
            }
        }

        #endregion Utility Methods

        #region Constructors and Destructor

        public ZipArchive(string name, string archType)
            : base(name, archType)
        {
        }

        ~ZipArchive()
        {
            Unload();
        }

        #endregion Constructors and Destructor

        #region Archive Implementation

        /// <summary>
        /// 
        /// </summary>
        public override bool IsCaseSensitive
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public override void Load()
        {
            if (this._zipFile == null || this._zipFile.Length == 0 || this._zipStream == null)
            {
                // read the open the zip archive
                Stream fs = null;

                this._zipFile = Path.GetFullPath(Name);
                if (File.Exists(this._zipFile))
                {
                    fs = File.OpenRead(this._zipFile);
                }

                if (fs == null)
                {
                    this._zipFile = Name.Replace('/', '.');

                    var assemblyContent = (from assembly in AssemblyEx.Neighbors()
                                           where this._zipFile.StartsWith(assembly.FullName.Split(',')[0])
                                           select assembly).FirstOrDefault();
                    if (assemblyContent != null)
                    {
                        fs = assemblyContent.GetManifestResourceStream(this._zipFile);
                    }
                }

#if (XBOX || XBOX360)
				if (fs == null)
				{
					_zipFile = Name;
					var isf = IsolatedStorageFile.GetUserStoreForApplication();
					fs = isf.OpenFile(_zipFile, FileMode.Open, FileAccess.Read);
				}
#endif

                if (fs == null)
                {
                    throw new FileNotFoundException(Name);
                }

                fs.Position = 0;

                // get a input stream from the zip file
                this._zipStream = ZipFile.Read(fs);

                this._fileList.AddRange(from entry in _zipStream.Entries
                                        select new FileInfo
                                        {
                                            Archive = this,
                                            Filename = entry.FileName,
                                            Basename = Path.GetFileName(entry.FileName),
                                            Path = Path.GetDirectoryName(entry.FileName) + Path.DirectorySeparatorChar,
                                            CompressedSize = entry.CompressedSize,
                                            UncompressedSize = entry.UncompressedSize
                                        });
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public override void Unload()
        {
            if (this._zipStream != null)
            {
                this._zipStream.Dispose();
                this._zipStream = null;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="readOnly"></param>
        /// <returns></returns>
        public override Stream Open(string filename, bool readOnly)
        {
            Load();

            if (this._zipStream.ContainsEntry(filename))
            {
                var entry = this._zipStream[filename];
                var output = new MemoryStream();
                entry.Extract(output);

                // reset the position to make sure it is at the beginning of the stream
                output.Position = 0;
                return output;
            }

            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="recursive"></param>
        /// <returns></returns>
        public override List<string> List(bool recursive)
        {
            return Find("*", recursive);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="recursive"></param>
        /// <returns></returns>
        public override FileInfoList ListFileInfo(bool recursive)
        {
            return FindFileInfo("*", recursive);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pattern"></param>
        /// <param name="recursive"></param>
        /// <returns></returns>
        public override List<string> Find(string pattern, bool recursive)
        {
            var ret = new List<string>();

            findFiles(pattern, recursive, ret, null);

            return ret;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pattern"></param>
        /// <param name="recursive"></param>
        /// <returns></returns>
        public override FileInfoList FindFileInfo(string pattern, bool recursive)
        {
            var ret = new FileInfoList();

            findFiles(pattern, recursive, null, ret);

            return ret;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public override bool Exists(string fileName)
        {
            var ret = new List<string>();

            findFiles(fileName, false, ret, null);

            return (bool)(ret.Count > 0);
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

        public override Archive CreateInstance(string name)
        {
            return new ZipArchive(name, _type);
        }

        public override void DestroyInstance(ref Archive obj)
        {
            obj.Dispose();
        }

        #endregion
    };
}