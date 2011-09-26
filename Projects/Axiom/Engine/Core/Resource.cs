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

using ResourceHandle = System.UInt64;

using Axiom.Scripting;

#endregion Namespace Declarations

namespace Axiom.Core
{
	/// <summary>
	/// Enum identifying the loading state of the resource
	/// </summary>
	public enum LoadingState
	{
		/// Not loaded
		Unloaded,
		/// Loading is in progress
		Loading,
		/// Fully loaded
		Loaded,
		/// Currently unloading
		Unloading
	};

	/// <summary>
	///		Abstract class representing a loadable resource (e.g. textures, sounds etc)
	/// </summary>
	/// <remarks>
	///		Resources are generally passive constructs, handled through the
	///		ResourceManager abstract class for the appropriate subclass.
	///		The main thing is that Resources can be loaded or unloaded by the
	///		ResourceManager to stay within a defined memory budget. Therefore,
	///		all Resources must be able to load, unload (whilst retainin enough
	///		info about themselves to be reloaded later), and state how big they are.
	/// <para/>
	///		Subclasses must implement:
	///		1. A constructor, with at least a mandatory name param.
	///			This constructor must set name and optionally size.
	///		2. The Load() and Unload() methods - size must be set after Load()
	///			Each must check &amp; update the isLoaded flag.
	/// </remarks>
	public abstract class Resource : ScriptableObject
	{
		#region Fields and Properties

#if AXIOM_MULTITHREADED
		private object _autoMutex = new object();
#endif
		protected object _loadingStatusMutex = new object();

		#region Creator Property

		private ResourceManager _creator;

		/// <summary>
		/// the manager which created this resource.
		/// </summary>
		public ResourceManager Creator
		{
			get
			{
				return _creator;
			}
			protected set
			{
				_creator = value;
			}
		}

		#endregion Creator Property

		#region Name Property

		protected string _name;

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

		#endregion Name Property

		#region Goup Property

		protected string _group;

		/// <summary>
        ///	Gets the group which this resource is a member of
		/// </summary>
		public string Group
		{
			get
			{
				return _group;
			}
			set
			{
				if ( _group != value )
				{
					var oldGroup = _group;
					_group = value;
					ResourceGroupManager.Instance.notifyResourceGroupChanged( oldGroup, this );
				}
			}
		}

		#endregion Goup Property

		#region IsLoad* Properties

		/// <summary>
		///		Has this resource been loaded yet?
		/// </summary>
		public bool IsLoaded
		{
			get
			{
				return _loadingState == LoadingState.Loaded;
			}
		}

		/// <summary>
		///		Has this resource been loaded yet?
		/// </summary>
		public bool IsLoading
		{
			get
			{
				return _loadingState == LoadingState.Loading;
			}
		}

		#endregion IsLoad* Properties

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

		#endregion IsManuallyLoaded Property

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
				return _lastAccessed;
			}
			protected set
			{
				_lastAccessed = value;
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
				return _handle;
			}
			set
			{
				_handle = value;
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
			set
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

		#region LoadingState Property

		volatile private LoadingState _loadingState = LoadingState.Unloaded;

		/// <summary>
		/// Returns whether the resource is currently in the process of	background loading.
		/// </summary>
		public LoadingState LoadingState
		{
			get
			{
				return _loadingState;
			}
			protected set
			{
				_loadingState = value;
			}
		}

		#endregion LoadingState Property

		#region IsBackgroundLoaded Property

		private bool _isBackgroundLoaded;

		/// <summary>
		/// Returns whether this Resource has been earmarked for background loading.
		/// </summary>
		/// <remarks>
		/// This option only makes sense when you have built Axiom with
		/// thread support (AXIOM_THREAD_SUPPORT). If a resource has been marked
		/// for background loading, then it won't load on demand like normal
		/// when load() is called. Instead, it will ignore request to load()
		/// except if the caller indicates it is the background loader. Any
		/// other users of this resource should check isLoaded(), and if that
		/// returns false, don't use the resource and come back later.
		/// <para/>
		/// Note that setting this only	defers the normal on-demand loading
		/// behaviour of a resource, it	does not actually set up a thread to make
		/// sure the resource gets loaded in the background. You should use
		/// ResourceBackgroundLoadingQueue to manage the actual loading
		/// (which will set this property itself).
		/// </remarks>
		public bool IsBackgroundLoaded
		{
			get
			{
				return _isBackgroundLoaded;
			}
			set
			{
				_isBackgroundLoaded = value;
			}
		}

		#endregion IsBackgroundLoaded Property

		/// <summary>
		///  Is the Resource reloadable?
		/// </summary>
		/// <returns></returns>
		public bool IsReloadable
		{
			get
			{
				return _isManuallyLoaded || ( _loader != null );
			}
		}

		#endregion Fields and Properties

		#region Constructors and Destructor

		/// <summary>
		///	Protected unnamed constructor to prevent default construction.
		/// </summary>
		protected Resource()
            : this(null, string.Empty, 0, string.Empty, false, null)
		{
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
		protected Resource( ResourceManager parent, string name, ResourceHandle handle, string group )
			: this( parent, name, handle, group, false, null )
		{
		}

	    /// <param name="group"></param>
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
	    /// <param name="parent"></param>
	    /// <param name="name"></param>
	    /// <param name="handle"></param>
	    protected Resource( ResourceManager parent, string name, ResourceHandle handle, string group, bool isManual, IManualResourceLoader loader )
            : base()
		{
			_creator = parent;
			_name = name;
			_handle = handle;
			_group = group;
			_size = 0;
			_isManuallyLoaded = isManual;
			_loader = loader;
		}

		~Resource()
		{
			dispose( false );
		}

		#endregion Constructors and Destructor

		#region Methods

		#region Load/Unload Stage Notifiers

		/// <summary>
		/// Internal hook to perform actions before the load process, but
		/// after the resource has been marked as 'Loading'.
		/// </summary>
		/// <remarks>
		/// Mutex will have already been acquired by the loading thread.
		/// Also, this call will occur even when using a <see>IManualResourceLoader</see>
		/// (when <see>load()</see> is not actually called)
		/// </remarks>
		protected virtual void preLoad()
		{
		}

		/// <summary>
		/// Internal hook to perform actions after the load process, but
		/// before the resource has been marked as 'Loaded'.
		/// </summary>
		/// <remarks>
		/// Mutex will have already been acquired by the loading thread.
		/// Also, this call will occur even when using a <see>IManualResourceLoader</see>
		/// (when <see>load()</see> is not actually called)
		/// </remarks>
		protected virtual void postLoad()
		{
		}

		/// <summary>
		/// Internal hook to perform actions before the unload process, but
		/// after the resource has been marked as 'Unloading'.
		/// </summary>
		/// <remarks>
		/// Mutex will have already been acquired by the unloading thread.
		/// Also, this call will occur even when using a <see>IManualResourceLoader</see>
		/// (when <see>unload()</see> is not actually called)
		/// </remarks>
		protected virtual void preUnload()
		{
		}

		/// <summary>
		/// Internal hook to perform actions after the unload process, but
		/// before the resource has been marked as 'Unloaded'.
		/// </summary>
		/// <remarks>
		/// Mutex will have already been acquired by the unloading thread.
		/// Also, this call will occur even when using a <see>IManualResourceLoader</see>
		/// (when <see>unload()</see> is not actually called)
		/// </remarks>
		protected virtual void postUnload()
		{
		}

		#endregion Load/Unload Stage Notifiers

        /// <summary>
        /// Prepares the resource for load, if it is not already.
        /// </summary>
        /// <see cref="Resource.Prepare(bool)"/>
        public void Prepare()
        {
            this.Prepare( false );
        }

        /// <summary>
        /// Prepares the resource for load, if it is not already.
        /// </summary>
        /// <remarks>
        /// One can call prepare() before load(), but this is not required as load() will call prepare() 
        /// itself, if needed.  When OGRE_THREAD_SUPPORT==1 both load() and prepare() 
        /// are thread-safe.  When OGRE_THREAD_SUPPORT==2 however, only prepare() 
        /// is thread-safe.  The reason for this function is to allow a background 
        /// thread to do some of the loading work, without requiring the whole render
        /// system to be thread-safe.  The background thread would call
        /// prepare() while the main render loop would later call load().  So long as
        /// prepare() remains thread-safe, subclasses can arbitrarily split the work of
        /// loading a resource between load() and prepare().  It is best to try and
        /// do as much work in prepare(), however, since this will leave less work for
        /// the main render thread to do and thus increase FPS.
        /// </remarks>
        /// <param name="background">Whether this is occurring in a background thread</param>
        public virtual void Prepare( bool background )
        {
            throw new NotImplementedException();
        }

		/// <summary>
		/// Escalates the loading of a background loaded resource.
		/// </summary>
		/// <remarks>
		/// If a resource is set to load in the background, but something needs
		/// it before it's been loaded, there could be a problem. If the user
		/// of this resource really can't wait, they can escalate the loading
		/// which basically pulls the loading into the current thread immediately.
		/// If the resource is already being loaded but just hasn't quite finished
		/// then this method will simply wait until the background load is complete.
		/// </remarks>
		public void EscalateLoading()
		{
			// Just call load as if this is the background thread, locking on
			// load status will prevent race conditions
			Load( true );
		}

		/// <summary>
		///		Loads the resource, if not loaded already.
		/// </summary>
		/// <remarks>
		/// If the resource is loaded from a file, loading is automatic. If not,
		/// if for example this resource gained it's data from procedural calls
		/// rather than loading from a file, then this resource will not reload
		/// on it's own
		/// </remarks>
		public void Load()
		{
			Load( false );
		}

		/// <summary>
		/// Loads the resource, if not loaded already.
		/// </summary>
		/// <remarks>
		/// If the resource is loaded from a file, loading is automatic. If not,
		/// if for example this resource gained it's data from procedural calls
		/// rather than loading from a file, then this resource will not reload
		/// on it's own.
		/// </remarks>
		/// <param name="background">Indicates whether the caller of this method is
		/// the background resource loading thread.</param>
		public virtual void Load( bool background )
		{
			// Early-out without lock (mitigate perf cost of ensuring loaded)
			// Don't load if:
			// 1. We're already loaded
			// 2. Another thread is loading right now
			// 3. We're marked for background loading and this is not the background
			//    loading thread we're being called by
			if ( _loadingState != LoadingState.Unloaded || ( _isBackgroundLoaded && !background ) )
				return;

			// Scope lock over load status
			lock ( _loadingStatusMutex )
			{
				// Check again just in case status changed (since we didn't lock above)
				if ( _loadingState != LoadingState.Unloaded || ( _isBackgroundLoaded && !background ) )
				{
					// no loading to be done
					return;
				}
				_loadingState = LoadingState.Loading;
			}

			try
			{
#if AXIOM_MULTITHREADED
				// Scope loack for actual load
				lock ( _autoMutex )
#endif
				{
					preLoad();

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
														"will be lost if it has to be reloaded.", _creator.ResourceType, _name );
						}
					}
					else
					{
						if ( Group == ResourceGroupManager.AutoDetectResourceGroupName )
						{
							// Derive resource group
							Group = ResourceGroupManager.Instance.FindGroupContainingResource( Name );
						}
						load();
					}

					// Calculate resource size
					_size = calculateSize();

					postLoad();
				}
			}
			catch ( Exception )
			{
				// Reset loading in-progress flag in case failed for some reason
				lock ( _loadingStatusMutex )
				{
					_loadingState = LoadingState.Unloaded;
					// Re-throw
					throw;
				}
			}

			// Scope lock for loading progress
			lock ( _loadingStatusMutex )
			{
				_loadingState = LoadingState.Loaded;
			}

			// Notify manager
			if ( _creator != null )
				_creator.NotifyResourceLoaded( this );

			// TODO: Fire (deferred) events
			//if ( _isBackgroundLoaded )
			//    queueFireBackgroundLoadingComplete();
		}

		/// <summary>
		///		Unloads the resource data, but retains enough info. to be able to recreate it
		///		on demand.
		/// </summary>
		public virtual void Unload()
		{
			// Early-out without lock (mitigate perf cost of ensuring unloaded)
			if ( LoadingState != LoadingState.Loaded )
				return;

			// Scope lock over load status
			lock ( _loadingStatusMutex )
			{
				// Check again just in case status changed (since we didn't lock above)
				if ( _loadingState == LoadingState.Loading )
				{
					throw new Exception( "Cannot unload resource " + Name + " whilst loading is in progress!" );
				}

				if ( _loadingState != LoadingState.Loaded )
					return; // nothing to do

				_loadingState = LoadingState.Unloading;
			}

#if AXIOM_MULTITHREADED
			// Scope lock for actual unload
			lock ( _autoMutex )
#endif
			{
				preUnload();
				unload();
				postUnload();
			}

			// Scope lock over load status
			lock ( _loadingStatusMutex )
			{
				_loadingState = LoadingState.Unloaded;
			}

			// Notify manager
			if ( _creator != null )
				_creator.NotifyResourceUnloaded( this );
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
#if AXIOM_MULTITHREADED
			lock( _autoMutex )
#endif
			{
				if ( _loadingState == LoadingState.Loaded )
				{
					Unload();
					Load();
				}
			}
		}

		/// <summary>
		///		Indicates this resource has been used.
		/// </summary>
		public virtual void Touch()
		{
#if AXIOM_MULTITHREADED
			lock( _autoMutex )
#endif
			{
				Load();

				if ( _creator != null )
					_creator.NotifyResourceTouched( this );
			}
		}

		/// <summary>
		/// Calculate the size of a resource; this will only be called after 'load'
		/// </summary>
		/// <returns></returns>
		protected virtual int calculateSize()
		{
			return 0;
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

		#endregion Abstract Load/Unload Implementation Methods

		#endregion Methods

		#region IDisposable Implementation

		protected override void dispose( bool disposeManagedResources )
		{
			if ( !IsDisposed )
			{
				if ( disposeManagedResources )
				{
					// Dispose managed resources.
					if ( IsLoaded )
						Unload();
				}

				// There are no unmanaged resources to release, but
				// if we add them, they need to be released here.
			}

            base.dispose(disposeManagedResources);
		}


		#endregion IDisposable Implementation
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
		void LoadResource( Resource resource );
	}
}