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
//     <id value="$Id$"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections;

using Axiom.Core;

#endregion NAmespace Declarations

namespace Axiom.FileSystem
{
    /// <summary>
    ///     ResourceManager specialization to handle Archive plug-ins.
    /// </summary>
    public sealed class ArchiveManager : IDisposable
    {
        #region Singleton implementation

        /// <summary>
        ///     Singleton instance of this class.
        /// </summary>
        private static ArchiveManager instance;

        /// <summary>
        ///     Internal constructor.  This class cannot be instantiated externally.
        /// </summary>
        internal ArchiveManager()
        {
            if ( instance == null )
            {
                instance = this;

                // add zip and folder factories by default
                instance.AddArchiveFactory( new ZipArchiveFactory() );
                instance.AddArchiveFactory( new FolderFactory() );
            }
        }

        /// <summary>
        ///     Gets the singleton instance of this class.
        /// </summary>
        public static ArchiveManager Instance
        {
            get
            {
                return instance;
            }
        }

        #endregion Singleton implementation

        #region Fields

        /// <summary>
        /// The list of factories
        /// </summary>
        private Hashtable factories = System.Collections.Specialized.CollectionsUtil.CreateCaseInsensitiveHashtable();

        #endregion

        #region Methods

        /// <summary>
        /// Add an archive factory to the list
        /// </summary>
        /// <param name="type">The type of the factory (zip, file, etc.)</param>
        /// <param name="factory">The factory itself</param>
        public void AddArchiveFactory( IArchiveFactory factory )
        {
            if ( factories[ factory.Type ] != null )
            {
                throw new AxiomException( "Attempted to add the {0} factory to ArchiveManager more than once.", factory.Type );
            }

            factories.Add( factory.Type, factory );
        }

        /// <summary>
        /// Get the archive factory
        /// </summary>
        /// <param name="type">The type of factory to get</param>
        /// <returns>The corresponding factory, or null if no factory</returns>
        public IArchiveFactory GetArchiveFactory( string type )
        {
            return (IArchiveFactory)factories[ type ];
        }

        #endregion Methods

        #region IDisposable Implementation

        /// <summary>
        ///     Called when the engine is shutting down.
        /// </summary>
        public void Dispose()
        {
            factories.Clear();

            instance = null;
        }

        #endregion IDisposable Implementation
    }
}
