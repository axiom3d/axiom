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
//     <id value="$Id$"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using Axiom.Core;

#endregion Namespace Declarations

namespace Axiom.FileSystem
{
    /// <summary>
    /// </summary>
    public class EmbeddedArchive : FileSystemArchive
    {
        #region Fields and Properties

        private readonly Assembly assembly;
        private readonly List<string> resources;

        public override bool IsMonitorable
        {
            get
            {
                return false;
            }
        }

        #endregion Fields and Properties

        #region Utility Methods

        protected override bool DirectoryExists(string directory)
        {
            return (from res in this.resources
                    where res.StartsWith(directory)
                    select res).Any();
        }

        protected override void findFiles(string pattern, bool recursive, List<string> simpleList, FileInfoList detailList,
                                           string currentDir)
        {
            if (pattern == "")
            {
                pattern = "*";
            }
            if (currentDir == "")
            {
                currentDir = _basePath;
            }

            var files = getFilesRecursively(currentDir, pattern);

            foreach (var file in files)
            {
                if (simpleList != null)
                {
                    simpleList.Add(file);
                }

                if (detailList != null)
                {
                    detailList.Add(new FileInfo
                    {
                        Archive = this,
                        Filename = file,
                        Basename = file.Substring(currentDir.Length),
                        Path = currentDir,
                        CompressedSize = 0,
                        UncompressedSize = 0,
                        ModifiedTime = DateTime.Now
                    });
                }
            }
        }

        protected override string[] getFiles(string dir, string pattern, bool recurse)
        {
            var files = !pattern.Contains("*") && Exists(dir + pattern)
                            ? new[]
                              {
                                  pattern
                              }
                            : from res in this.resources
                              where res.StartsWith(dir)
                              select res;

            if (pattern == "*")
            {
                return files.ToArray<string>();
            }

            pattern = pattern.Substring(pattern.LastIndexOf('*') + 1);

            return (from file in files
                    where file.EndsWith(pattern)
                    select file).ToArray<string>();
        }

        protected override string[] getFilesRecursively(string dir, string pattern)
        {
            return getFiles(dir, pattern, true);
        }

        #endregion Utility Methods

        #region Constructors and Destructors

        public EmbeddedArchive(string name, string archType)
            : base(name.Split('/')[0], archType)
        {
            var named = Name + ",";

            this.assembly = (from a in AssemblyEx.Neighbors()
                             where a.FullName.StartsWith(named)
                             select a).First();
            Name = name.Replace('/', '.');
            this.resources = (from resource in this.assembly.GetManifestResourceNames()
                                  //where resource.StartsWith(Name)
                              select resource).ToList();
            this.resources.Sort();
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
            _basePath = Name + ".";
            IsReadOnly = true;
        }

        public override Stream Create(string filename, bool overwrite)
        {
            throw new AxiomException("Cannot create a file in a read-only archive.");
        }

        public override Stream Open(string filename, bool readOnly)
        {
            if (!readOnly)
            {
                throw new AxiomException("Cannot create a file in a read-only archive.");
            }
            return this.assembly.GetManifestResourceStream(this.resources[this.resources.BinarySearch(_basePath + filename)]);
        }

        public override bool Exists(string fileName)
        {
            return this.resources.BinarySearch(_basePath + fileName) >= 0;
        }

        #endregion Archive Implementation
    }

    /// <summary>
    /// Specialization of IArchiveFactory for Embedded files.
    /// </summary>
    public class EmbeddedArchiveFactory : ArchiveFactory
    {
        private const string _type = "Embedded";

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
            return new EmbeddedArchive(name, _type);
        }

        public override void DestroyInstance(ref Archive obj)
        {
            obj.Dispose();
        }

        #endregion ArchiveFactory Implementation
    };
}