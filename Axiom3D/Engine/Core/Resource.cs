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
using System.IO;

using ResourceHandle = System.UInt64;

#endregion Namespace Declarations

namespace Axiom
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
    /// <ogre name="Resource">
    ///     <file name="OgreResource.h"   revision="1.16.2.1" lastUpdated="5/18/2006" lastUpdatedBy="Borrillis" />
    ///     <file name="OgreResource.cpp" revision="1.7" lastUpdated="5/18/2006" lastUpdatedBy="Borrillis" />
    /// </ogre> 
    public abstract class Resource : IDisposable
    {
        #region Fields and Properties

        #region Parent Property

        private ResourceManager _parent;
        /// <summary>
        /// the manager which created this resource.
        /// </summary>
        public ResourceManager Parent
        {
            get
            {
                return _parent;
            }
            protected set
            {
                _parent = value;
            }
        }

        #endregion Parent Property

        #region Name Property

        protected string name;
        /// <summary>
        ///		Name of this resource.
        /// </summary>
        public string Name
        {
            get
            {
                return _name;
            }
            protected set
            {
                _name = value;
            }
        }

        #endregion Property

        #region IsLoaded Property

        private bool _isLoaded;
        /// <summary>
        ///		Has this resource been loaded yet?
        /// </summary>
        public bool IsLoaded
        {
            get
            {
                return _isLoaded;
            }
            protected set
            {
                _isLoaded = value;
            }
        }

        #endregion IsLoaded Property

        #region IsManuallyLoaded Property

        private bool _isManuallyLoaded;
        /// <summary>
        /// 
        /// </summary>
        public bool IsManuallyLoaded
        {
            get
            {
                return _isManuallyLoaded;
            }
            protected set
            {
                _isManuallyLoaded = value;
            }
        }

        #endregion IsManual Property
			
        #region Size Property

        private long _size;
        /// <summary>
        ///		Size (in bytes) that this resource takes up in memory.
        /// </summary>
        public long Size
        {
            get
            {
                return _size;
            }
            protected set
            {
                _size = value;
            }
        }

        #endregion Size Property

        #region LastAccessed Property

        private long _lastAccessed;
        /// <summary>
        ///		Timestamp of the last time this resource was accessed.
        /// </summary>
        public long LastAccessed
        {
            get
            {
                return lastAccessed;
            }
        }

        #endregion LastAccessed Property

        #region Handle Property

        private ResourceHandle _handle;
        /// <summary>
        ///		Unique handle of this resource.
        /// </summary>
        public ResourceHandle Handle
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

        #endregion Handle Property

        #region Origin Property

        private string _origin;
        /// <summary>
        /// Origin of this resource (e.g. script name) - optional
        /// </summary>
        public string Origin
        {
            get
            {
                return _origin;
            }
            protected set
            {
                _origin = value;
            }
        }

        #endregion Origin Property

        #region loader Property

        private IManualResourceLoader _loader;
        /// <summary>
        /// Optional manual loader; if provided, data is loaded from here instead of a file
        /// </summary>
        protected IManualResourceLoader loader
        {
            get
            {
                return _loader;
            }
            set
            {
                _loader = value;
            }
        }

        #endregion loader Property

        /// <summary>
        ///  Is the Resource reloadable?
        /// </summary>
        /// <returns></returns>
        public bool IsReloadable
        {
            get
            {
                return _isManual || ( _loader != null );
            }
        }
							
        #endregion Fields and Properties

        #region Constructors and Destructors

        /// <summary>
        ///	Protected unnamed constructor to prevent default construction. 
        /// </summary>
        protected Resource()
        {
            _parent = null;
            _handle = 0;
            _isLoaded = false;
            _size = 0;
            _isManuallyLoaded = false;
            _loader = null;

        }

        /// <overloads>
        /// <summary>
        /// Standard constructor.
        /// </summary>
        /// <param name="parent">ResourceManager that is creating this resource</param>
        /// <param name="name">The unique name of the resource</param>
        /// <param name="handle"></param>
        /// <param name="group">The name of the resource group to which this resource belongs</param>
        /// </overloads>
        Resource( ResourceManager parent, string name, ResourceHandle handle, string group )
            : this( parent, name, handle, group, false, null )
        {
        }

        /// <param name="isManual">
        ///     Is this resource manually loaded? If so, you should really
        ///     populate the loader parameter in order that the load process
        ///     can call the loader back when loading is required. 
        /// </param>
        /// <param name="loader">
        ///     An IManualResourceLoader implementation which will be called
        ///     when the Resource wishes to load (should be supplied if you set
        ///     isManual to true). You can in fact leave this parameter null 
        ///     if you wish, but the Resource will never be able to reload if 
        ///     anything ever causes it to unload. Therefore provision of a proper
        ///     IManualResourceLoader instance is strongly recommended.
        /// </param>
        Resource( ResourceManager parent, string name, ResourceHandle handle, string group, bool isManual, IManualResourceLoader loader )
        {
            _parent = parent;
            _name = name;
            _handle = handle;
            _group = group;
            _size = 0;
            _isManuallyLoaded = isManual;
            _loader = loader;
        }

        #endregion

        #region Methods

        /// <summary>
        ///		Loads the resource, if not loaded already.
        /// </summary>
        /// <remarks>
        /// If the resource is loaded from a file, loading is automatic. If not,
        /// if for example this resource gained it's data from procedural calls
        /// rather than loading from a file, then this resource will not reload 
        /// on it's own
        /// </remarks>
        public virtual void Load()
        {
            if ( !_isLoaded )
            {
                if ( _isManuallyLoaded )
                {
                    // Load from manual loader
                    if ( _loader != null )
                    {
                        _loader.LoadResource( this );
                    }
                    else
                    {
                        // Warn that this resource is not reloadable
                        LogManager.Instance.Write( "WARNING: {0} instance '{1}' was defined as manually loaded, but no manual loader was provided. This Resource " +
                                                    "will be lost if it has to be reloaded.", _parent.ResourceType, _name );
                    }
                }
                else
                {
                    loadImpl();
                }

                // Calculate resource size
                _size = calculateSize();

                // Now loaded
                _isLoaded = true;

                // Notify manager
                if ( _parent != null ) _parent.NotifyResourceLoaded( this );
            }
        }

        /// <summary>
        ///		Unloads the resource data, but retains enough info. to be able to recreate it
        ///		on demand.
        /// </summary>
        public virtual void Unload()
        {
            if ( _isLoaded )
            {
                unloadImpl();
                _isLoaded = false;

                // Notify manager
                if ( _parent )
                    _parent.NotifyResourceUnloaded( this );
            }
        }

        /// <summary>
        /// Reloads the resource, if it is already loaded.
        /// </summary>
        /// <remarks>
        /// Calls unload() and then load() again, if the resource is already
        /// loaded. If it is not loaded already, then nothing happens.
        /// </remarks>
        public virtual void Reload()
        {
            if ( _isLoaded )
            {
                unload();
                load();
            }
        }

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

            if ( _parent != null )
                _parent.NotifyResourceTouched( this );

        }

        #region Abstract Load/Unload Implementation Methods

        /// <summary>
        /// Internal implementation of the 'load' action, only called if this 
        /// resource is not being loaded from a ManualResourceLoader. 
        /// </summary>
        protected abstract void load();

		/// <summary>
		/// Internal implementation of the 'unload' action; called regardless of
        /// whether this resource is being loaded from a ManualResourceLoader. 
		/// </summary>
		protected abstract void unload();

        /// <summary>
        /// Calculate the size of a resource; this will only be called after 'load'
        /// </summary>
        /// <returns></returns>
		protected abstract int calculateSize();

        #endregion Abstract Implementation Methods

        #endregion Methods

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

    /// <summary>
    /// Interface describing a manual resource loader.
    /// </summary>
    /// <remarks>
    /// Resources are usually loaded from files; however in some cases you
    /// want to be able to set the data up manually instead. This provides
    /// some problems, such as how to reload a Resource if it becomes
    /// unloaded for some reason, either because of memory constraints, or
    /// because a device fails and some or all of the data is lost.
    /// <para/>
    /// This interface should be implemented by all classes which wish to
    /// provide manual data to a resource. They provide a pointer to themselves
    /// when defining the resource (via the appropriate ResourceManager), 
    /// and will be called when the Resource tries to load. 
    /// They should implement the loadResource method such that the Resource 
    /// is in the end set up exactly as if it had loaded from a file, 
    /// although the implementations will likely differ	between subclasses 
    /// of Resource, which is why no generic algorithm can be stated here. 
    /// <para/>
    /// The loader must remain valid for the entire life of the resource,
    /// so that if need be it can be called upon to re-load the resource
    /// at any time.
    /// </remarks>
    /// <ogre name="ManualResourceLoader">
    ///     <file name="OgreResource.h"   revision="1.16.2.1" lastUpdated="5/18/2006" lastUpdatedBy="Borrillis" />
    /// </ogre> 
    public interface IManualResourceLoader
    {
        /// <summary>
        /// Called when a resource wishes to load.
        /// </summary>
        /// <param name="resource">The resource which wishes to load</param>
		void LoadResource(Resource resource);
    }
}
