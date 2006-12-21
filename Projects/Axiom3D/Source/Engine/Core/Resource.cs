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
using System.IO;

#endregion Namespace Declarations

namespace Axiom.Core
{
    /// <summary>
    ///		Abstract class reprensenting a loadable resource (e.g. textures, sounds etc)
    /// </summary>
    /// <remarks>
    ///		Resources are generally passive constructs, handled through the
    ///		ResourceManager abstract class for the appropriate subclass.
    ///		The main thing is that Resources can be loaded or unloaded by the
    ///		ResourceManager to stay within a defined memory budget. Therefore,
    ///		all Resources must be able to load, unload (whilst retainin enough
    ///		info about themselves to be reloaded later), and state how big they are.
    ///
    ///		Subclasses must implement:
    ///		1. A constructor, with at least a mandatory name param.
    ///			This constructor must set name and optionally size.
    ///		2. The Load() and Unload() methods - size must be set after Load()
    ///			Each must check & update the isLoaded flag.
    /// </remarks>
    public abstract class Resource : IDisposable
    {
        #region Fields

        /// <summary>
        ///		Name of this resource.
        /// </summary>
        protected string name;
        /// <summary>
        ///		Has this resource been loaded yet?
        /// </summary>
        protected bool isLoaded;
        /// <summary>
        ///		 Size (in bytes) that this resource takes up in memory.
        /// </summary>
        protected long size;
        /// <summary>
        ///		Timestamp of the last time this resource was accessed.
        /// </summary>
        protected long lastAccessed;
        /// <summary>
        ///		Unique handle of this resource.
        /// </summary>
        protected int handle;

        #endregion Fields

        #region Constructors

        /// <summary>
        ///		Default constructor.
        /// </summary>
        /// <remarks>Subclasses must initialize name and size.</remarks>
        public Resource()
        {
            isLoaded = false;
            size = 0;
        }

        #endregion

        #region Virtual/Abstract methods

        /// <summary>
        ///		Loads the resource, if not loaded already.
        /// </summary>
        public abstract void Load();

        /// <summary>
        ///		Unloads the resource data, but retains enough info. to be able to recreate it
        ///		on demand.
        /// </summary>
        public virtual void Unload()
        {
        }

        #endregion

        #region Properties

        /// <summary>
        ///		Size of this resource.
        /// </summary>
        public long Size
        {
            get
            {
                return size;
            }
        }

        /// <summary>
        ///		Name of this resource.
        /// </summary>
        public string Name
        {
            get
            {
                return name;
            }
        }

        /// <summary>
        ///		Is this resource loaded?
        /// </summary>
        public bool IsLoaded
        {
            get
            {
                return isLoaded;
            }
        }

        /// <summary>
        ///		The time the resource was last touched.
        /// </summary>
        public long LastAccessed
        {
            get
            {
                return lastAccessed;
            }
        }

        /// <summary>
        ///		Gets/Sets the unique handle of this resource.
        /// </summary>
        public int Handle
        {
            get
            {
                return handle;
            }
            set
            {
                handle = value;
            }
        }

        #endregion

        #region Public methods

        /// <summary>
        ///		Indicates this resource has been used.
        /// </summary>
        public virtual void Touch()
        {
            lastAccessed = Root.Instance.Timer.Milliseconds;

            if ( !isLoaded )
            {
                Load();
            }
        }

        #endregion

        #region Implementation of IDisposable

        /// <summary>
        ///		Dispose method.  Made virtual to allow subclasses to destroy resources their own way.
        /// </summary>
        public virtual void Dispose()
        {
            if ( isLoaded )
            {
                // unload this resource
                Unload();
            }
        }

        #endregion
    }
}
