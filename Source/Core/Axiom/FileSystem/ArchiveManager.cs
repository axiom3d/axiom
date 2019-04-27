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

using System;
using System.Collections;
using System.Collections.Generic;
using Axiom.Core;

#endregion Namespace Declarations

namespace Axiom.FileSystem
{
    /// <summary>
    ///  This class manages the available ArchiveFactory plugins.
    /// </summary>
    /// <ogre name="ArchiveManager">
    ///     <file name="OgreArchiveManager.h"   revision="1.8.2.1" lastUpdated="5/18/2006" lastUpdatedBy="Borrillis" />
    ///     <file name="OgreArchiveManager.cpp" revision="1.14.2.1" lastUpdated="5/18/2006" lastUpdatedBy="Borrillis" />
    /// </ogre> 
    public sealed class ArchiveManager : Singleton<ArchiveManager>
    {
        #region Fields and Properties

        /// <summary>
        /// The list of factories
        /// </summary>
        private readonly Dictionary<string, ArchiveFactory> _factories = new Dictionary<string, ArchiveFactory>();

        private readonly Dictionary<string, Archive> _archives = new Dictionary<string, Archive>();

        #endregion

        #region Constructor

        /// <summary>
        /// Internal constructor.  This class cannot be instantiated externally.
        /// </summary>
        public ArchiveManager()
        {
        }

        #endregion Constructor

        #region Methods

        /// <summary>
        /// Opens an archive for file reading.
        /// </summary>
        /// <remarks>
        /// The archives are created using class factories within
        /// extension libraries.
        /// </remarks>
        /// <param name="filename">The filename that will be opened</param>
        /// <param name="archiveType">The library that contains the data-handling code</param>
        /// <returns>
        /// If the function succeeds, a valid pointer to an Archive object is returned.
        /// <para/>
        /// If the function fails, an exception is thrown.
        /// </returns>
        public Archive Load(string filename, string archiveType)
        {
            Archive arch = null;
            if (!this._archives.TryGetValue(filename, out arch))
            {
                // Search factories
                ArchiveFactory fac = null;
                if (!this._factories.TryGetValue(archiveType, out fac))
                {
                    throw new AxiomException("Cannot find an archive factory to deal with archive of type {0}", archiveType);
                }

                arch = fac.CreateInstance(filename);
                arch.Load();
                this._archives.Add(filename, arch);
            }
            return arch;
        }

        #region Unload Method

        /// <summary>
        ///  Unloads an archive.
        ///  </summary>
        ///  <remarks>
        /// You must ensure that this archive is not being used before removing it.
        ///  </remarks>
        /// <param name="arch">The Archive to unload</param>
        public void Unload(Archive arch)
        {
            Unload(arch.Name);
        }

        /// <summary>
        ///  Unloads an archive.
        ///  </summary>
        ///  <remarks>
        /// You must ensure that this archive is not being used before removing it.
        ///  </remarks>
        /// <param name="filename">The Archive to unload</param>
        public void Unload(string filename)
        {
            var arch = this._archives[filename];

            if (arch != null)
            {
                arch.Unload();

                var fac = this._factories[arch.Type];
                if (fac == null)
                {
                    throw new AxiomException("Cannot find an archive factory to deal with archive of type {0}", arch.Type);
                }
                this._archives.Remove(arch.Name);
                fac.DestroyInstance(ref arch);
            }
        }

        #endregion Unload Method

        /// <summary>
        /// Add an archive factory to the list
        /// </summary>
        /// <param name="factory">The factory itself</param>
        public void AddArchiveFactory(ArchiveFactory factory)
        {
            if (this._factories.ContainsKey(factory.Type) == true)
            {
                throw new AxiomException("Attempted to add the {0} factory to ArchiveManager more than once.", factory.Type);
            }

            this._factories.Add(factory.Type, factory);
            LogManager.Instance.Write("ArchiveFactory for archive type {0} registered.", factory.Type);
        }

        #endregion Methods

        #region Singleton<ArchiveManager> Implementation

        /// <summary>
        ///     Called when the engine is shutting down.
        /// </summary>
        protected override void dispose(bool disposeManagedResources)
        {
            if (!isDisposed)
            {
                if (disposeManagedResources)
                {
                    // Unload & delete resources in turn
                    foreach (var arch in this._archives)
                    {
                        // Unload
                        arch.Value.Unload();

                        // Find factory to destroy
                        var fac = this._factories[arch.Value.Type];
                        if (fac == null)
                        {
                            // Factory not found
                            throw new AxiomException("Cannot find an archive factory to deal with archive of type {0}", arch.Value.Type);
                        }
                        var tmp = arch.Value;
                        fac.DestroyInstance(ref tmp);
                    }

                    // Empty the list
                    this._archives.Clear();
                }

                // There are no unmanaged resources to release, but
                // if we add them, they need to be released here.
            }

            // If it is available, make the call to the
            // base class's Dispose(Boolean) method
            base.dispose(disposeManagedResources);
        }

        #endregion Singleton<ArchiveManager> Implementation
    }
}