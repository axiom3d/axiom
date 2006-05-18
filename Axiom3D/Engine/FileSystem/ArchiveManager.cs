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
using Axiom;

#endregion Namespace Declarations
			
namespace Axiom
{
    /// <summary>
    ///  This class manages the available ArchiveFactory plugins.
    /// </summary>
    /// <ogre name="ArchiveManager">
    ///     <file name="OgreArchiveManager.h"   revision="1.8.2.1" lastUpdated="5/18/2006" lastUpdatedBy="Borrillis" />
    ///     <file name="OgreArchiveManager.cpp" revision="1.14.2.1" lastUpdated="5/18/2006" lastUpdatedBy="Borrillis" />
    /// </ogre> 
    public sealed class ArchiveManager : Singleton<ArchiveManager>, IDisposable
    {
        #region Fields and Properties

        /// <summary>
        /// The list of factories
        /// </summary>
        private Dictionary<string, IArchiveFactory> _factories = new Dictionary<string,IArchiveFactory>();
        private Dictionary<string, Archive> _archives = new Dictionary<string, Archive>();

        #endregion

        #region Constructor

        /// <summary>
        /// Internal constructor.  This class cannot be instantiated externally.
        /// </summary>
        private ArchiveManager()
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
        public Archive Load( string filename, string archiveType )
        {
            Archive arch = _archives[filename];

            if (arch == null)
            {
                // Search factories
                IArchiveFactory fac = _factories[archiveType];
                if ( fac == null )
                    throw new AxiomException("Cannot find an archive factory to deal with archive of type {0}", archiveType );

                arch = fac.CreateInstance(filename);
                arch.Load();
                _archives.Add(filename,arch);

            }   
            return pArch;
        }


        #region Unload Method

        /// <summary>
        ///  Unloads an archive.
        ///  </summary>
        ///  <remarks>
        /// You must ensure that this archive is not being used before removing it.
        ///  </remarks>
        /// <param name="arch">The Archive to unload</param>
        public void Unload( Archive arch )
        {
            Unload( arch.Name );
        }

        /// <summary>
        ///  Unloads an archive.
        ///  </summary>
        ///  <remarks>
        /// You must ensure that this archive is not being used before removing it.
        ///  </remarks>
        /// <param name="arch">The Archive to unload</param>
        public void Unload( string filename )
        {
            Archive arch = _archives[filename];

            if ( arch != null )
            {
                arch.Unload();

                IArchiveFactory fac = _factories[ arch.Type ];
                if ( fac == null )
                    throw new AxiomException( "Cannot find an archive factory to deal with archive of type {0}", archiveType );
                fac.DestroyInstance( arch );
                _archives.Remove( arch.Name );
            }
        }

        #endregion Unload Method

        /// <summary>
        /// Add an archive factory to the list
        /// </summary>
        /// <param name="type">The type of the factory (zip, file, etc.)</param>
        /// <param name="factory">The factory itself</param>
        public void AddArchiveFactory( IArchiveFactory factory )
        {
            if ( _factories.ContainsKey(factory.Type) == true )
            {
                throw new AxiomException( "Attempted to add the {0} factory to ArchiveManager more than once.", factory.Type );
            }

            _factories.Add( factory.Type, factory );
            LogManager.Instance.Write("ArchiveFactory for archive type {0} registered.", factory.Type);

        }

        #endregion Methods

        #region IDisposable Implementation

        /// <summary>
        ///     Called when the engine is shutting down.
        /// </summary>
        public void Dispose()
        {
            // Unload & delete resources in turn
            foreach( Archive arch in _archives )
            {
                // Unload
                arch.Unload();

                // Find factory to destroy
                IArchiveFactory fac = _factories[ arch.Type] ;
                if (fac == null)
                {
                    // Factory not found
                    throw new AxiomException( "Cannot find an archive factory to deal with archive of type {0}", arch.Type );
                }
                fac.DestroyInstance(arch);
                
            }

            // Empty the list
            _archives.clear();

            instance = null;
        }

        #endregion IDisposable Implementation
    }
}
