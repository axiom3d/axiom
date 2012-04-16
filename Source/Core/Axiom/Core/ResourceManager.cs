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
using System.Diagnostics;
using System.IO;

using Axiom.Collections;
using Axiom.Math;
using Axiom.Scripting;

using ResourceHandle = System.UInt64;

#endregion Namespace Declarations

namespace Axiom.Core
{
	/// <summary>
	///		Defines a generic resource handler.
	/// </summary>
	/// <remarks>
	///		A resource manager is responsible for managing a pool of
	///		resources of a particular type. It must index them, look
	///		them up, load and destroy them. It may also need to stay within
	///		a defined memory budget, and temporarily unload some resources
	///		if it needs to stay within this budget.
	///		<para/>
	///		Resource managers use a priority system to determine what can
	///		be unloaded, and a Least Recently Used (LRU) policy within
	///		resources of the same priority.
	///     Resources can be loaded using the generalized load interface,
	///     and they can be unloaded and removed. In addition, each
	///     subclass of ResourceManager will likely define custom 'load' methods
	///     which take explicit parameters depending on the kind of resource
	///     being created.
	///     <para/>
	///     Resources can be loaded and unloaded through the Resource class,
	///     but they can only be removed (and thus eventually destroyed) using
	///     their parent ResourceManager.
	/// </remarks>
	///
	/// <ogre name="ResourceManager">
	///     <file name="OgreResourceManager.h"   revision="1.17.2.1" lastUpdated="6/19/2006" lastUpdatedBy="Borrillis" />
	///     <file name="OgreResourceManager.cpp" revision="1.17.2.2" lastUpdated="6/19/2006" lastUpdatedBy="Borrillis" />
	/// </ogre>
	///
	public abstract class ResourceManager : DisposableObject, IScriptLoader
	{
		#region Fields and Properties

		private static readonly object _autoMutex = new object();

		#region Resources Property

		private readonly Dictionary<string, Resource> _resources = new Dictionary<string, Resource>( new CaseInsensitiveStringComparer() );

		/// <summary>
		///		A cached list of all resources in memory.
		///	</summary>
		public ICollection<Resource> Resources
		{
			get
			{
				return resourceHandleMap.Values;
			}
		}

		#endregion Resources Property

		#region resourceHandleMap Property

		//  std::map<ResourceHandle, ResourcePtr>
		private readonly Dictionary<ResourceHandle, Resource> _resourceHandleMap = new Dictionary<ResourceHandle, Resource>();

		/// <summary>
		///		A cached list of all resources handles in memory.
		///	</summary>
		protected IDictionary<ResourceHandle, Resource> resourceHandleMap
		{
			get
			{
				return _resourceHandleMap;
			}
		}

		#endregion resourceHandleMap Property

		#region MemoryBudget Property

		private long _memoryBudget; // in bytes

		/// <summary>
		/// Get/Set a limit on the amount of memory all resource handlers may use.
		/// </summary>
		/// <remarks>
		/// If, when asked to load a new resource, the manager believes it will exceed this memory
		/// budget, it will temporarily unload a resource to make room for the new one. This unloading
		/// is not permanent and the Resource is not destroyed; it simply needs to be reloaded when
		/// next used.
		/// </remarks>
		public long MemoryBudget
		{
			get
			{
				return _memoryBudget;
			}
			set
			{
				_memoryBudget = value;
				checkUsage();
			}
		}

		#endregion MemoryBudget Property

		#region memoryUsage Property

		private long _memoryUsage; // in bytes

		/// <summary>
		///		Gets/Sets the current memory usages by all resource managers.
		/// </summary>
		protected long memoryUsage
		{
			get
			{
				return _memoryUsage;
			}
		}

		#endregion memoryUsage Property

		#region ResourceType Property

		private string _resourceType;

		/// <summary>
		/// Gets/Sets a string identifying the type of resource this manager handles.
		/// </summary>
		public string ResourceType
		{
			get
			{
				return _resourceType;
			}
			protected set
			{
				_resourceType = value;
			}
		}

		#endregion ResourceType Property

		#region Verbose Property

		/// <summary>
		/// Gets/Sets whether this manager and its resources habitually produce log output
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public virtual bool Verbose { get; set; }

		#endregion Verbose Property

		#region Indexer Properties

		public Resource GetByName( string name )
		{
			return this[ name ];
		}

		public Resource GetByHandle( ResourceHandle handle )
		{
			return this[ handle ];
		}

		/// <summary>
		///    Gets a reference to the specified named resource.
		/// </summary>
		/// <param name="name">Name of the resource to retrieve.</param>
		/// <returns>A reference to a Resource with the given name or null.</returns>
		/// <ogre name="getByName" />
		public Resource this[ string name ]
		{
			get
			{
				// return the resource or null
				return this[ (ResourceHandle)name.ToLower().GetHashCode() ];
			}
		}

		/// <summary>
		///		Gets a resource with the given handle.
		/// </summary>
		/// <param name="handle">Handle of the resource to retrieve.</param>
		/// <returns>A reference to a Resource with the given handle or null.</returns>
		/// <ogre name="getByHandle" />
		public Resource this[ ResourceHandle handle ]
		{
			get
			{
				Debug.Assert( _resourceHandleMap != null, "A resource was being retrieved, but the list of Resources is null.", "" );

				Resource resource;

				// try to obtain the resource
				if ( _resourceHandleMap.TryGetValue( handle, out resource ) )
				{
					resource.Touch();
				}

				// return the resource or null
				return resource;
			}
		}

		#endregion Indexer Properties

		#endregion Fields and Properties

		#region Constructors and Destructors

		/// <summary>
		///		Default constructor
		/// </summary>
		protected ResourceManager()
			: base()
		{
			_memoryBudget = long.MaxValue;
			_memoryUsage = 0;
			_loadingOrder = 0;
		}

		#endregion Constructors and Destructors

		#region Methods

		#region Create Method

		/// <overloads>
		/// <summary>
		///		Creates a new blank resource, but does not immediately load it.
		/// </summary>
		/// <remarks>
		///		Resource managers handle disparate types of resources. This method returns a pointer to a
		///		valid new instance of the kind of resource managed here. The caller should  complete the
		///		details of the returned resource and call ResourceManager.Load to load the resource. Note
		///		that it is the CALLERS responsibility to destroy this object when it is no longer required
		///		(after calling ResourceManager.Unload if it had been loaded).
		///     If you want to get at the detailed interface of this resource, you'll have to
		///     cast the result to the subclass you know you're creating.
		/// </remarks>
		/// <param name="name">The unique name of the resource</param>
		/// <param name="group"></param>
		/// <returns></returns>
		/// </overloads>
		public Resource Create( string name, string group )
		{
			return Create( name, group, false, null, null );
		}

		/// <param name="group"></param>
		/// <param name="createParams">If any parameters are required to create an instance, they should be supplied here as name / value pairs</param>
		/// <param name="name"></param>
		public Resource Create( string name, string group, NameValuePairList createParams )
		{
			return Create( name, group, false, null, createParams );
		}

		/// <param name="group"></param>
		/// <param name="isManual">
		/// Is this resource manually loaded? If so, you should really
		/// populate the loader parameter in order that the load process
		/// can call the loader back when loading is required.
		/// </param>
		/// <param name="loader">
		/// Pointer to a ManualLoader implementation which will be called
		/// when the Resource wishes to load (should be supplied if you set
		/// isManual to true). You can in fact leave this parameter null
		/// if you wish, but the Resource will never be able to reload if
		/// anything ever causes it to unload. Therefore provision of a proper
		/// ManualLoader instance is strongly recommended.
		/// </param>
		/// <param name="createParams">If any parameters are required to create an instance, they should be supplied here as name / value pairs</param>
		/// <param name="name"></param>
		public virtual Resource Create( string name, string group, bool isManual, IManualResourceLoader loader, NameValuePairList createParams )
		{
			// Call creation implementation
			var ret = _create( name, (ResourceHandle)name.ToLower().GetHashCode(), group, isManual, loader, createParams );
			if ( createParams != null )
			{
				ret.SetParameters( createParams );
			}

			_add( ret );

			// Tell resource group manager
			ResourceGroupManager.Instance.notifyResourceCreated( ret );

			return ret;
		}

		public Axiom.Math.Tuple<Resource, bool> CreateOrRetrieve( string name, string group )
		{
			return CreateOrRetrieve( name, group, false, null, null );
		}

		public Axiom.Math.Tuple<Resource, bool> CreateOrRetrieve( string name, string group, bool isManual, IManualResourceLoader loader, NameValuePairList paramaters )
		{
			ResourceHandle hashCode = (ResourceHandle)name.ToLower().GetHashCode();

			var res = this[ hashCode ];
			var created = false;

			if ( res == null )
			{
				res = _create( name, hashCode, group, isManual, loader, paramaters );
				if ( paramaters != null )
				{
					res.SetParameters( paramaters );
				}

				_add( res );

				// Tell resource group manager
				ResourceGroupManager.Instance.notifyResourceCreated( res );

				created = true;
			}

			return new Axiom.Math.Tuple<Resource, bool>( res, created );
		}

		#endregion Create Method

		#region Prepare Method

		/// <summary>
		/// Generic prepare method, used to create a Resource specific to this 
		/// ResourceManager without using one of the specialised 'prepare' methods
		/// (containing per-Resource-type parameters).
		/// </summary>
		/// <param name="name">The name of the Resource</param>
		/// <param name="group">The resource group to which this resource will belong</param>
		/// <param name="isManual">Is the resource to be manually loaded? If so, you should
		/// provide a value for the loader parameter</param>
		/// <param name="loader">The manual loader which is to perform the required actions
		/// when this resource is loaded; only applicable when you specify true
		/// for the previous parameter</param>
		/// <param name="loadParams">Optional pointer to a list of name/value pairs 
		/// containing loading parameters for this type of resource.</param>
		/// <param name="backgroundThread">Optional boolean which lets the load routine know if it
		/// is being run on the background resource loading thread</param>
		[OgreVersion( 1, 7, 2 )]
		public virtual Resource Prepare( string name, string group, bool isManual, IManualResourceLoader loader, NameValuePairList loadParams, bool backgroundThread )
		{
			var r = CreateOrRetrieve( name, group, isManual, loader, loadParams ).First;
			// ensure prepared
			r.Prepare( backgroundThread );
			return r;
		}

		public Resource Prepare( string name, string group )
		{
			return this.Prepare( name, group, false, null, null, false );
		}

		public Resource Prepare( string name, string group, bool isManual )
		{
			return this.Prepare( name, group, isManual, null, null, false );
		}

		public Resource Prepare( string name, string group, bool isManual, IManualResourceLoader loader )
		{
			return this.Prepare( name, group, isManual, loader, null, false );
		}

		public Resource Prepare( string name, string group, bool isManual, IManualResourceLoader loader, NameValuePairList loadParams )
		{
			return this.Prepare( name, group, isManual, loader, loadParams, false );
		}

		#endregion Prepare Method

		#region Load Method

		//TODO : Look at generics method for implementing this.

		/// <summary>
		/// Generic load method, used to create a Resource specific to this 
		/// ResourceManager without using one of the specialised 'load' methods
		/// (containing per-Resource-type parameters).
		/// </summary>
		/// <param name="name">The name of the Resource</param>
		/// <param name="group">The resource group to which this resource will belong</param>
		/// <param name="isManual">Is the resource to be manually loaded? If so, you should
		/// provide a value for the loader parameter</param>
		/// <param name="loader">The manual loader which is to perform the required actions
		/// when this resource is loaded; only applicable when you specify true
		/// for the previous parameter</param>
		/// <param name="loadParams">Optional pointer to a list of name/value pairs 
		/// containing loading parameters for this type of resource.</param>
		/// <param name="backgroundThread">Optional boolean which lets the load routine know if it
		/// is being run on the background resource loading thread</param>
		[OgreVersion( 1, 7, 2 )]
		public virtual Resource Load( string name, string group, bool isManual, IManualResourceLoader loader, NameValuePairList loadParams, bool backgroundThread )
		{
			var r = CreateOrRetrieve( name, group, isManual, loader, loadParams ).First;
			// ensure loaded
			r.Load( backgroundThread );
			return r;
		}

		public Resource Load( string name, string group )
		{
			return this.Load( name, group, false, null, null, false );
		}

		public Resource Load( string name, string group, bool isManual )
		{
			return this.Load( name, group, isManual, null, null, false );
		}

		public Resource Load( string name, string group, bool isManual, IManualResourceLoader loader )
		{
			return this.Load( name, group, isManual, loader, null, false );
		}

		public Resource Load( string name, string group, bool isManual, IManualResourceLoader loader, NameValuePairList loadParams )
		{
			return this.Load( name, group, isManual, loader, loadParams, false );
		}

		#endregion Load Method

		#region Unload Method

		/// <overloads>
		/// <summary>
		/// Unloads a single resource by name.
		/// </summary>
		/// <remarks>
		/// Unloaded resources are not removed, they simply free up their memory
		/// as much as they can and wait to be reloaded.
		/// <see>ResourceGroupManager</see> for unloading of resource groups.
		/// </remarks>
		/// </overloads>
		/// <param name="name">Name of the resource.</param>
		public virtual void Unload( string name )
		{
			var res = this[ name ];

			if ( res != null )
			{
				// Unload resource
				res.Unload();
			}
		}

		/// <param name="handle">Handle of the resource</param>
		public virtual void Unload( ResourceHandle handle )
		{
			var res = this[ handle ];

			if ( res != null )
			{
				// Unload resource
				res.Unload();
			}
		}

		/// <summary>
		///		Unloads a Resource from the managed resources list, calling it's Unload() method.
		/// </summary>
		/// <remarks>
		///		This method removes a resource from the list maintained by this manager, and unloads it from
		///		memory. It does NOT destroy the resource itself, although the memory used by it will be largely
		///		freed up. This would allow you to reload the resource again if you wished.
		/// </remarks>
		/// <param name="resource"></param>
		public virtual void Unload( Resource resource )
		{
			// unload the resource
			resource.Unload();

			// remove the resource
			_resources.Remove( resource.Name );

			// update memory usage
			_memoryUsage -= resource.Size;
		}

		#endregion Unload Method

		/// <summary>
		/// Unloads all resources.
		/// </summary>
		/// <remarks>
		/// Unloaded resources are not removed, they simply free up their memory
		/// as much as they can and wait to be reloaded.
		/// <see>ResourceGroupManager</see> for unloading of resource groups.
		/// </remarks>
		public virtual void UnloadAll()
		{
			foreach ( var res in _resources.Values )
			{
				res.Unload();
			}
		}

		/// <summary>Causes all currently loaded resources to be reloaded.</summary>
		/// <remarks>
		/// Unloaded resources are not removed, they simply free up their memory
		/// as much as they can and wait to be reloaded.
		/// <see>ResourceGroupManager</see> for unloading of resource groups.
		/// </remarks>
		public virtual void ReloadAll()
		{
			foreach ( var res in _resources.Values )
			{
				res.Reload();
			}
		}

		#region Remove Method

		/// <overloads>
		/// <summary>
		/// Remove a single resource.
		/// </summary>
		/// <remarks>
		/// Removes a single resource, meaning it will be removed from the list
		/// of valid resources in this manager, also causing it to be unloaded.
		/// <para/>
		/// The word 'Destroy' is not used here, since
		/// if any other pointers are referring to this resource, it will persist
		/// until they have finished with it; however to all intents and purposes
		/// it no longer exists and will likely get destroyed imminently.
		/// <para/>
		/// If you do have references to resources hanging around after the
		/// ResourceManager is destroyed, you may get problems on destruction of
		/// these resources if they were relying on the manager (especially if
		/// it is a plugin). If you find you get problems on shutdown in the
		/// destruction of resources, try making sure you release all your
		/// references before you shutdown OGRE.
		/// </remarks>
		/// </overloads>
		/// <param name="resource">The resource to remove</param>
		public virtual void Remove( Resource resource )
		{
			_remove( resource );
		}

		/// <param name="name">The name of the resource to remove</param>
		public virtual void Remove( string name )
		{
			var resource = this[ name ];
			if ( resource != null )
			{
				_remove( resource );
			}
		}

		/// <param name="handle">The Handle of the resource to remove</param>
		public virtual void Remove( ResourceHandle handle )
		{
			var resource = this[ handle ];
			if ( resource != null )
			{
				_remove( resource );
			}
		}

		#endregion Remove Method

		/// <summary>
		/// Removes all resources.
		/// </summary>
		/// <remarks>
		/// Removes all resources, meaning they will be removed from the list
		/// of valid resources in this manager, also causing them to be unloaded.
		/// <para/>
		/// The word 'Destroy' is not used here, since
		/// if any other pointers are referring to this resource, it will persist
		/// until they have finished with it; however to all intents and purposes
		/// it no longer exists and will likely get destroyed imminently.
		/// <para/>
		/// If you do have references to resources hanging around after the
		/// ResourceManager is destroyed, you may get problems on destruction of
		/// these resources if they were relying on the manager (especially if
		/// it is a plugin). If you find you get problems on shutdown in the
		/// destruction of resources, try making sure you release all your
		/// references before you shutdown Axiom.
		/// </remarks>
		public virtual void RemoveAll()
		{
			foreach ( var resource in _resources.Values )
			{
				if ( !resource.IsDisposed )
				{
					resource.Dispose();
				}
			}
			_resources.Clear();
			_resourceHandleMap.Clear();

			ResourceGroupManager.Instance.notifyAllResourcesRemoved( this );
		}

		#region ResourceExists Method

		/// <summary>Returns whether the named resource exists in this manager</summary>
		/// <param name="name">name of the resource</param>
		public virtual bool ResourceExists( string name )
		{
			return this[ name ] != null;
		}

		/// <summary>Returns whether a resource with the given handle exists in this manager</summary>
		/// <param name="handle">handle of the resource</param>
		public virtual bool ResourceExists( ResourceHandle handle )
		{
			return this[ handle ] != null;
		}

		#endregion ResourceExists Method

		/// <summary>Notify this manager that a resource which it manages has been 'touched', ie used. </summary>
		/// <param name="res">the resource</param>
		[OgreVersion( 1, 7, 2 )]
		public virtual void NotifyResourceTouched( Resource res )
		{
			// TODO
		}

		/// <summary> Notify this manager that a resource which it manages has been loaded. </summary>
		/// <param name="res">the resource</param>
		[OgreVersion( 1, 7, 2 )]
		public virtual void NotifyResourceLoaded( Resource res )
		{
			lock ( _autoMutex )
			{
				_memoryUsage += res.Size;
			}
		}

		/// <summary>Notify this manager that a resource which it manages has been unloaded.</summary>
		/// <param name="res">the resource</param>
		[OgreVersion( 1, 7, 2 )]
		public virtual void NotifyResourceUnloaded( Resource res )
		{
			lock ( _autoMutex )
			{
				_memoryUsage -= res.Size;
			}
		}

		/// <summary>
		/// Create a new resource instance compatible with this manager (no custom
		/// parameters are populated at this point).
		/// </summary>
		/// <param name="name">The unique name of the resource</param>
		/// <param name="handle"></param>
		/// <param name="group">The name of the resource group to attach this new resource to</param>
		/// <param name="isManual">
		///     Is this resource manually loaded? If so, you should really
		///     populate the loader parameter in order that the load process
		///     can call the loader back when loading is required.
		/// </param>
		/// <param name="loader">
		///     A ManualLoader implementation which will be called
		///     when the Resource wishes to load (should be supplied if you set
		///     isManual to true). You can in fact leave this parameter null
		///     if you wish, but the Resource will never be able to reload if
		///     anything ever causes it to unload. Therefore provision of a proper
		///     ManualLoader instance is strongly recommended.
		/// </param>
		/// <param name="createParams">
		///     If any parameters are required to create an instance,
		///     they should be supplied here as name / value pairs. These do not need
		///     to be set on the instance (handled elsewhere), just used if required
		///     to differentiate which concrete class is created.
		/// </param>
		/// <returns></returns>
		protected abstract Resource _create( string name, ResourceHandle handle, string group, bool isManual, IManualResourceLoader loader, NameValuePairList createParams );

		/// <summary>
		/// Add a newly created resource to the manager
		/// </summary>
		/// <param name="res"></param>
		protected virtual void _add( Resource res )
		{
			if ( !_resourceHandleMap.ContainsKey( res.Handle ) )
			{
				_resourceHandleMap.Add( res.Handle, res );
			}
			else
			{
				throw new AxiomException( String.Format( "Resource '{0}' with the handle {1} already exists.", res.Name, res.Handle ) );
			}
		}

		/// <summary>
		/// Remove a resource from this manager; remove it from the lists.
		/// </summary>
		/// <param name="res"></param>
		protected virtual void _remove( Resource res )
		{
			if ( _resources.ContainsKey( res.Name ) )
			{
				_resources.Remove( res.Name );
			}

			if ( _resourceHandleMap.ContainsKey( res.Handle ) )
			{
				_resourceHandleMap.Remove( res.Handle );
			}

			ResourceGroupManager.Instance.notifyResourceRemoved( res );
		}

		/// <summary>
		///		Makes sure we are still within budget.
		/// </summary>
		protected void checkUsage()
		{
			// TODO Implementation of CheckUsage.
			// Keep a sorted list of resource by LastAccessed for easy removal of oldest?
		}

		#endregion Methods

		#region DisposableObject Implementation

		/// <summary>
		/// Class level dispose method
		/// </summary>
		/// <remarks>
		/// When implementing this method in an inherited class the following template should be used;
		/// protected override void dispose( bool disposeManagedResources )
		/// {
		/// 	if ( !isDisposed )
		/// 	{
		/// 		if ( disposeManagedResources )
		/// 		{
		/// 			// Dispose managed resources.
		/// 		}
		///
		/// 		// There are no unmanaged resources to release, but
		/// 		// if we add them, they need to be released here.
		/// 	}
		///
		/// 	// If it is available, make the call to the
		/// 	// base class's Dispose(Boolean) method
		/// 	base.dispose( disposeManagedResources );
		/// }
		/// </remarks>
		/// <param name="disposeManagedResources">True if Unmanaged resources should be released.</param>
		protected override void dispose( bool disposeManagedResources )
		{
			if ( !this.IsDisposed )
			{
				if ( disposeManagedResources )
				{
					UnloadAll();
					RemoveAll();
				}

				// There are no unmanaged resources to release, but
				// if we add them, they need to be released here.
			}

			base.dispose( disposeManagedResources );
		}

		#endregion DisposableObject Implementation

		#region IScriptLoader Members

		#region ScriptPatterns Property

		private List<string> _scriptPatterns = new List<string>();

		/// <summary>
		/// Gets the file patterns which should be used to find scripts for this class.
		/// </summary>
		/// <remarks>
		/// This method is called when a resource group is loaded if you use
		/// ResourceGroupManager::registerScriptLoader. Returns a list of file
		/// patterns, in the order they should be searched in.
		/// </remarks>
		public virtual List<string> ScriptPatterns
		{
			get
			{
				return _scriptPatterns;
			}
			protected set
			{
				_scriptPatterns = value;
			}
		}

		#endregion ScriptPatterns Property

		#region ParseScriptMethod

		/// <summary>
		/// Parse a script file.
		/// </summary>
		/// <param name="stream">reference to a data stream which is the source of the script</param>
		/// <param name="groupName">
		/// The name of a resource group which should be used if any resources
		/// are created during the parse of this script.
		/// </param>
		/// <param name="fileName"></param>
		public virtual void ParseScript( Stream stream, string groupName, string fileName ) {}

		#endregion ParseScriptMethod

		#region LoadingOrder Property

		private Real _loadingOrder;

		/// <summary>
		/// Gets the relative loading order of scripts of this type.
		/// </summary>
		/// <remarks>
		/// There are dependencies between some kinds of scripts, and to enforce
		/// this all implementors of this interface must define a loading order.
		/// Returns a value representing the relative loading order of these scripts
		/// compared to other script users, where higher values load later.
		/// </remarks>
		public virtual Real LoadingOrder
		{
			get
			{
				return _loadingOrder;
			}
			protected set
			{
				_loadingOrder = value;
			}
		}

		#endregion LoadingOrder Property

		#endregion IScriptLoader Members
	};
}
