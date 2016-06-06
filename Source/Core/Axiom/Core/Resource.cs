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
using Axiom.Scripting;
using ResourceHandle = System.UInt64;

#endregion Namespace Declarations

namespace Axiom.Core
{
	/// <summary>
	/// Enum identifying the loading state of the resource
	/// </summary>
	[OgreVersion( 1, 7, 2 )]
	public enum LoadingState
	{
		/// <summary>
		/// Not loaded
		/// </summary>
		Unloaded,

		/// <summary>
		/// Loading is in progress
		/// </summary>
		Loading,

		/// <summary>
		/// Fully loaded
		/// </summary>
		Loaded,

		/// <summary>
		/// Currently unloading
		/// </summary>
		Unloading,

		/// <summary>
		/// Fully prepared
		/// </summary>
		Prepared,

		/// <summary>
		/// Preparing is in progress
		/// </summary>
		Preparing
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
		public interface IListener
		{
			/// <summary>
			/// Callback to indicate that background loading has completed.
			/// </summary>
			[OgreVersion( 1, 7, 2 )]
			[Obsolete( "Use LoadingComplete instead" )]
			void BackgroundLoadingComplete( Resource res );

			/// <summary>
			/// Callback to indicate that background preparing has completed.
			/// </summary>
			[OgreVersion( 1, 7, 2 )]
			[Obsolete( "Use PreparingComplete instead." )]
			void BackgroundPreparingComplete( Resource res );

			/// <summary>
			/// Called whenever the resource finishes loading.
			/// </summary>
			/// <remarks>
			/// If a Resource has been marked as background loaded (@see Resource::setBackgroundLoaded), 
			/// the call does not itself occur in the thread which is doing the loading;
			/// when loading is complete a response indicator is placed with the
			/// ResourceGroupManager, which will then be sent back to the 
			/// listener as part of the application's primary frame loop thread.
			/// </remarks>
			[OgreVersion( 1, 7, 2 )]
			void LoadingComplete( Resource res );

			/// <summary>
			/// Called whenever the resource finishes preparing (paging into memory).
			/// </summary>
			/// <remarks>
			/// If a Resource has been marked as background loaded (@see Resource::setBackgroundLoaded)
			/// the call does not itself occur in the thread which is doing the preparing;
			/// when preparing is complete a response indicator is placed with the
			/// ResourceGroupManager, which will then be sent back to the 
			/// listener as part of the application's primary frame loop thread.
			/// </remarks>
			[OgreVersion( 1, 7, 2 )]
			void PreparingComplete( Resource res );

			/// <summary>
			/// Called whenever the resource has been unloaded.
			/// </summary>
			[OgreVersion( 1, 7, 2 )]
			void UnloadingComplete( Resource res );
		};

		#region Fields and Properties

#if AXIOM_THREAD_SUPPORT
		private object _autoMutex = new object();
#endif
		protected object _loadingStatusMutex = new object();

		protected static readonly object listenerListMutex = new object();
		protected List<IListener> listenerList = new List<IListener>();

		#region Creator Property

		private ResourceManager _creator;

		/// <summary>
		/// the manager which created this resource.
		/// </summary>
		public ResourceManager Creator
		{
			get
			{
				return this._creator;
			}
			protected set
			{
				this._creator = value;
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
				return this._name;
			}
			protected set
			{
				this._name = value;
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
				return this._group;
			}
			set
			{
				if ( this._group != value )
				{
					var oldGroup = this._group;
					this._group = value;
					ResourceGroupManager.Instance.notifyResourceGroupChanged( oldGroup, this );
				}
			}
		}

		#endregion Goup Property

		#region IsLoad* Properties

		/// <summary>
		///	Has this resource been loaded yet?
		/// </summary>
		public bool IsLoaded
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				// No lock required to read this state since no modify
				return this._loadingState.Value == LoadingState.Loaded;
			}
		}

		/// <summary>
		///	Returns whether the resource is currently in the process of
		/// background loading.
		/// </summary>
		public bool IsLoading
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return this._loadingState.Value == LoadingState.Loading;
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
				return this._isManuallyLoaded;
			}
			protected set
			{
				this._isManuallyLoaded = value;
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
				return this._size;
			}
			protected set
			{
				this._size = value;
			}
		}

		#endregion Size Property

		#region LastAccessed Property

		/// <summary>
		///		Timestamp of the last time this resource was accessed.
		/// </summary>
		public long LastAccessed { get; protected set; }

		#endregion LastAccessed Property

		#region Handle Property

		/// <summary>
		///		Unique handle of this resource.
		/// </summary>
		public ResourceHandle Handle { get; set; }

		#endregion Handle Property

		#region Origin Property

		/// <summary>
		/// Origin of this resource (e.g. script name) - optional
		/// </summary>
		public string Origin { get; set; }

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
				return this._loader;
			}
			set
			{
				this._loader = value;
			}
		}

		#endregion loader Property

		#region LoadingState Property

		/// <summary>
		/// Is the resource currently loaded?
		/// </summary>
		protected AtomicScalar<LoadingState> _loadingState = new AtomicScalar<LoadingState>( LoadingState.Unloaded );

		/// <summary>
		/// Returns whether the resource is currently in the process of	background loading.
		/// </summary>
		public LoadingState LoadingState
		{
			get
			{
				return this._loadingState.Value;
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
				return this._isBackgroundLoaded;
			}
			set
			{
				this._isBackgroundLoaded = value;
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
				return this._isManuallyLoaded || ( this._loader != null );
			}
		}

		#endregion Fields and Properties

		#region Constructors and Destructor

		/// <summary>
		///	Protected unnamed constructor to prevent default construction.
		/// </summary>
		protected Resource()
			: this( null, string.Empty, 0, string.Empty, false, null )
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
		protected Resource( ResourceManager parent, string name, ResourceHandle handle, string group, bool isManual,
		                    IManualResourceLoader loader )
			: base()
		{
			this._creator = parent;
			this._name = name;
			Handle = handle;
			this._group = group;
			this._size = 0;
			this._isManuallyLoaded = isManual;
			this._loader = loader;
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

		/// <summary>
		/// Internal implementation of the meat of the 'prepare' action.
		/// </summary>
		protected virtual void prepare()
		{
		}

		/// <summary>
		/// Internal function for undoing the 'prepare' action.  Called when
		/// the load is completed, and when resources are unloaded when they
		/// are prepared but not yet loaded.
		/// </summary>
		protected virtual void unPrepare()
		{
		}

		#endregion Load/Unload Stage Notifiers

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
#if NET_40
		public virtual void Prepare( bool background = false )
#else
		public virtual void Prepare( bool background )
#endif
		{
			throw new NotImplementedException();
		}

#if !NET_40
		/// <summary>
		/// Prepares the resource for load, if it is not already.
		/// </summary>
		/// <see cref="Resource.Prepare(bool)"/>
		public void Prepare()
		{
			Prepare( false );
		}
#endif

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
		[OgreVersion( 1, 7, 2, "Just missing _dirtyState implementation" )]
#if NET_40
		public virtual void Load( bool background = false )
#else
		public virtual void Load( bool background )
#endif
		{
			// Early-out without lock (mitigate perf cost of ensuring loaded)
			// Don't load if:
			// 1. We're already loaded
			// 2. Another thread is loading right now
			// 3. We're marked for background loading and this is not the background
			//    loading thread we're being called by
			if ( this._isBackgroundLoaded && !background )
			{
				return;
			}

			// This next section is to deal with cases where 2 threads are fighting over
			// who gets to prepare / load - this will only usually happen if loading is escalated
			var keepChecking = true;
			var old = LoadingState.Unloaded;
			while ( keepChecking )
			{
				// quick check that avoids any synchronisation
				old = this._loadingState.Value;

				if ( old == LoadingState.Preparing )
				{
					while ( this._loadingState.Value == LoadingState.Preparing )
					{
#if AXIOM_THREAD_SUPPORT
				        lock ( _autoMutex ) { }
#endif
					}
					old = this._loadingState.Value;
				}

				if ( old != LoadingState.Unloaded && old != LoadingState.Prepared && old != LoadingState.Loading )
				{
					return;
				}

				// atomically do slower check to make absolutely sure,
				// and set the load state to LOADING
				if ( old == Core.LoadingState.Loading || !this._loadingState.Cas( old, Core.LoadingState.Loading ) )
				{
					while ( this._loadingState.Value == LoadingState.Loading )
					{
#if AXIOM_THREAD_SUPPORT
				        lock ( _autoMutex ) { }
#endif
					}

					var state = this._loadingState.Value;
					if ( state == LoadingState.Prepared || state == LoadingState.Preparing )
					{
						// another thread is preparing, loop around
						continue;
					}
					else if ( state != LoadingState.Loaded )
					{
						throw new AxiomException( "Another thread failed in resource operation" );
					}
				}
				keepChecking = false;
			}

			// Scope lock for actual loading
			try
			{
#if AXIOM_THREAD_SUPPORT
	// Scope lock for actual load
				lock ( _autoMutex )
#endif
				{
					if ( this._isManuallyLoaded )
					{
						preLoad();

						// Load from manual loader
						if ( this._loader != null )
						{
							this._loader.LoadResource( this );
						}
						else
						{
							// Warn that this resource is not reloadable
							LogManager.Instance.Write(
								"WARNING: {0} instance '{1}' was defined as manually loaded, but no manual loader was provided. This Resource " +
								"will be lost if it has to be reloaded.", this._creator.ResourceType, this._name );
						}
						postLoad();
					}
					else
					{
						if ( old == LoadingState.Unloaded )
						{
							prepare();
						}

						preLoad();

						old = LoadingState.Prepared;

						if ( Group == ResourceGroupManager.AutoDetectResourceGroupName )
						{
							// Derive resource group
							var result = ResourceGroupManager.Instance.FindGroupContainingResource( Name );
							if ( result.First )
							{
								Group = result.Second;
							}
							else
							{
								LogManager.Instance.Write(
									string.Format( "Unable to derive resource group for {0} automatically since the resource was not found.", Name ) );
								this._loadingState.Value = LoadingState.Unloaded;
								return;
							}
						}
						load();
						postLoad();
					}

					// Calculate resource size
					this._size = calculateSize();
				}
			}
			catch ( Exception ex )
			{
				// Reset loading in-progress flag, in case failed for some reason.
				// We reset it to UNLOADED because the only other case is when
				// old == PREPARED in which case the loadImpl should wipe out
				// any prepared data since it might be invalid.
				this._loadingState.Value = LoadingState.Unloaded;
				// Re-throw
				LogManager.Instance.Write( LogManager.BuildExceptionString( ex ) );
				throw;
			}

			this._loadingState.Value = LoadingState.Loaded;

			//TODO
			//_dirtyState();

			// Notify manager
			if ( this._creator != null )
			{
				this._creator.NotifyResourceLoaded( this );
			}

			// Fire events, if not background
			if ( !background )
			{
				FireLoadingComplete( false );
			}
		}

#if !NET_40
		/// <see cref="Load(bool)"/>
		public void Load()
		{
			Load( false );
		}
#endif

		/// <summary>
		///		Unloads the resource data, but retains enough info. to be able to recreate it
		///		on demand.
		/// </summary>
		public virtual void Unload()
		{
			// Early-out without lock (mitigate perf cost of ensuring unloaded)
			if ( LoadingState != LoadingState.Loaded )
			{
				return;
			}

			// Scope lock over load status
			lock ( this._loadingStatusMutex )
			{
				// Check again just in case status changed (since we didn't lock above)
				if ( this._loadingState.Value == LoadingState.Loading )
				{
					throw new Exception( "Cannot unload resource " + Name + " whilst loading is in progress!" );
				}

				if ( this._loadingState.Value != LoadingState.Loaded )
				{
					return; // nothing to do
				}

				this._loadingState.Value = LoadingState.Unloading;
			}

#if AXIOM_THREAD_SUPPORT
	// Scope lock for actual unload
			lock ( _autoMutex )
#endif
			{
				preUnload();
				unload();
				postUnload();
			}

			// Scope lock over load status
			lock ( this._loadingStatusMutex )
			{
				this._loadingState.Value = LoadingState.Unloaded;
			}

			// Notify manager
			if ( this._creator != null )
			{
				this._creator.NotifyResourceUnloaded( this );
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
#if AXIOM_THREAD_SUPPORT
			lock( _autoMutex )
#endif
			{
				if ( this._loadingState.Value == LoadingState.Loaded )
				{
					Unload();
					Load();
				}
			}
		}

		/// <summary>
		///	'Touches' the resource to indicate it has been used.
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public virtual void Touch()
		{
			// make sure loaded
			Load();

			if ( this._creator != null )
			{
				this._creator.NotifyResourceTouched( this );
			}
		}

		/// <summary>
		/// Register a listener on this resource.
		/// <seealso cref="Resource.IListener"/>
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public void AddListener( IListener lis )
		{
			lock ( listenerListMutex )
			{
				this.listenerList.Add( lis );
			}
		}

		/// <summary>
		/// Remove a listener on this resource.
		/// <seealso cref="Resource.IListener"/>
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public void RemoveListener( IListener lis )
		{
			lock ( listenerListMutex )
			{
				this.listenerList.Remove( lis );
			}
		}

		/// <summary>
		/// Firing of loading complete event
		/// </summary>
		/// <remarks>
		/// You should call this from the thread that runs the main frame loop 
		/// to avoid having to make the receivers of this event thread-safe.
		/// If you use Axiom's built in frame loop you don't need to call this
		/// yourself.
		/// </remarks>
		/// <param name="wasBackgroundLoaded">Whether this was a background loaded event</param>
		[OgreVersion( 1, 7, 2 )]
		internal virtual void FireLoadingComplete( bool wasBackgroundLoaded )
		{
			// Lock the listener list
			lock ( listenerListMutex )
			{
				foreach ( var i in this.listenerList )
				{
					// deprecated call
					if ( wasBackgroundLoaded )
					{
						i.BackgroundLoadingComplete( this );
					}

					i.LoadingComplete( this );
				}
			}
		}

		/// <summary>
		/// Firing of preparing complete event
		/// </summary>
		/// <remarks>
		/// You should call this from the thread that runs the main frame loop 
		/// to avoid having to make the receivers of this event thread-safe.
		/// If you use Axiom's built in frame loop you don't need to call this
		/// yourself.
		/// </remarks>
		/// <param name="wasBackgroundLoaded">Whether this was a background loaded event</param>
		[OgreVersion( 1, 7, 2 )]
		internal virtual void FirePreparingComplete( bool wasBackgroundLoaded )
		{
			// Lock the listener list
			lock ( listenerListMutex )
			{
				foreach ( var i in this.listenerList )
				{
					// deprecated call
					if ( wasBackgroundLoaded )
					{
						i.BackgroundPreparingComplete( this );
					}

					i.PreparingComplete( this );
				}
			}
		}

		/// <summary>
		/// Firing of unloading complete event
		/// </summary>
		/// <remarks>
		/// You should call this from the thread that runs the main frame loop 
		/// to avoid having to make the receivers of this event thread-safe.
		/// If you use Axiom's built in frame loop you don't need to call this
		/// yourself.
		/// </remarks>
		[OgreVersion( 1, 7, 2 )]
		internal virtual void FireUnloadingComplete()
		{
			// Lock the listener list
			lock ( listenerListMutex )
			{
				foreach ( var i in this.listenerList )
				{
					i.UnloadingComplete( this );
				}
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
					{
						Unload();
					}
				}

				// There are no unmanaged resources to release, but
				// if we add them, they need to be released here.
			}

			base.dispose( disposeManagedResources );
		}

		#endregion IDisposable Implementation
	};

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
	};
}