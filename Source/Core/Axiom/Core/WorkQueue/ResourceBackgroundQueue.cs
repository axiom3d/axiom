#region MIT/X11 License

//Copyright © 2003-2012 Axiom 3D Rendering Engine Project
//
//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:
//
//The above copyright notice and this permission notice shall be included in
//all copies or substantial portions of the Software.
//
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//THE SOFTWARE.

#endregion License

#region SVN Version Information

// <file>
//     <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections.Generic;
using Axiom.Collections;
using ResourceHandle = System.UInt64;

#endregion Namespace Declarations

namespace Axiom.Core
{
	/// <summary>
	/// Encapsulates the result of a background queue request
	/// </summary>
	public struct BackgroundProcessResult
	{
		/// <summary>
		/// Whether an error occurred
		/// </summary>
		public bool Error;

		/// <summary>
		/// Any messages from the process
		/// </summary>
		public string Message;
	};

	/// <summary>
	/// This class is used to perform Resource operations in a
	/// background thread.
	/// </summary>
	/// <remarks>
	/// All these requests are now queued via Root::getWorkQueue in order
	/// to share the thread pool amongst all background tasks. You should therefore
	/// refer to that class for configuring the behaviour of the threads
	/// themselves, this class merely provides an interface that is specific
	/// to resource loading around this common functionality.
	/// @par
	/// The general approach here is that on requesting a background resource
	/// process, your request is placed on a queue ready for the background
	/// thread to be picked up, and you will get a 'ticket' back, identifying
	/// the request. Your call will then return and your thread can
	/// proceed, knowing that at some point in the background the operation will 
	/// be performed. In it's own thread, the resource operation will be 
	/// performed, and once finished the ticket will be marked as complete. 
	/// You can check the status of tickets by calling isProcessComplete() 
	/// from your queueing thread.
	/// 
	/// Note, no locks are required here anymore because all of the parallelisation
	/// is now contained in WorkQueue - this class is entirely single-threaded
	/// </remarks>
	public class ResourceBackgroundQueue
		: DisposableObject, ISingleton<ResourceBackgroundQueue>, WorkQueue.IRequestHandler, WorkQueue.IResponseHandler
	{
		/// <summary>
		/// Enumerates the type of requests
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		protected enum RequestType
		{
			InitializeGroup = 0,
			InitializeAllGroups = 1,
			PrepareGroup = 2,
			PrepareResource = 3,
			LoadGroup = 4,
			LoadResource = 5,
			UnloadGroup = 6,
			UnloadResource = 7
		};

		/// <summary>
		/// This delegate lets you get notifications of completed background
		/// processes instead of having to poll ticket statuses.
		/// </summary>
		/// <remarks>
		/// For simplicity, this callback is not issued direct from the background
		/// loading thread, it is queued to be sent from the main thread
		/// so that you don't have to be concerned about thread safety.
		/// </remarks>
		public delegate void OnOperationCompleted( RequestID ticket, BackgroundProcessResult result );

		/// <summary>
		/// Encapsulates a queued request for the background queue
		/// </summary>
		protected struct ResourceRequest
		{
			public RequestType Type;
			public string ResourceName;
			public ResourceHandle ResourceHandle;
			public string ResourceType;
			public string GroupName;
			public bool IsManual;
			public IManualResourceLoader Loader;
			public NameValuePairList LoadParams;
			public OnOperationCompleted Listener;
			public BackgroundProcessResult Result;
		};

		/// <summary>
		/// Struct that holds details of queued notifications
		/// </summary>
		protected struct ResourceResponse
		{
			public Resource Resource;
			public ResourceRequest Request;

			public ResourceResponse( Resource r, ResourceRequest req )
			{
				Resource = r;
				Request = req;
			}
		};

		protected ushort workQueueChannel;
		protected List<RequestID> outstandingRequestSet = new List<RequestID>();

		public ResourceBackgroundQueue()
			: base()
		{
			if ( instance == null )
			{
				instance = this;
			}
		}

		[OgreVersion( 1, 7, 2, "~ResourceBackgroundQueue" )]
		protected override void dispose( bool disposeManagedResources )
		{
			if ( !IsDisposed )
			{
				if ( disposeManagedResources )
				{
					ShutDown();
				}
			}

			base.dispose( disposeManagedResources );
		}

		#region Methods

		/// <summary>
		/// Shut down the background queue system.
		/// </summary>
		/// <remarks>
		/// Called automatically by Root.Shutdown.
		/// </remarks>
		[OgreVersion( 1, 7, 2 )]
		public virtual void ShutDown()
		{
			WorkQueue wq = Root.Instance.WorkQueue;
			wq.AbortRequestByChannel( workQueueChannel );
			wq.RemoveRequestHandler( workQueueChannel, this );
			wq.RemoveResponseHandler( workQueueChannel, this );
		}

		/// <summary>
		/// Initialise a resource group in the background.
		/// </summary>
		/// <param name="name">The name of the resource group to initialise</param>
		/// <param name="listener">Optional callback interface, take note of warnings in 
		/// the header and only use if you understand them.</param>
		/// <returns>Ticket identifying the request, use isProcessComplete() to 
		/// determine if completed if not using listener</returns>
		/// <see cref="ResourceGroupManager.InitializeResourceGroup"/>
		[OgreVersion( 1, 7, 2 )]
#if NET_40
		public virtual RequestID InitializeResourceGroup( string name, OnOperationCompleted listener = null )
#else
		public virtual RequestID InitializeResourceGroup( string name, OnOperationCompleted listener )
#endif
		{
#if AXIOM_THREAD_SUPPORT
	//queue a request
			ResourceRequest req = new ResourceRequest();
			req.Type = RequestType.InitializeGroup;
			req.GroupName = name;
			req.Listener = listener;
			return AddRequest( req );
#else
			// synchronous
			ResourceGroupManager.Instance.InitializeResourceGroup( name );
			return 0;
#endif
		}

#if !NET_40
		/// <see cref="ResourceBackgroundQueue.InitializeResourceGroup( string, OnOperationCompleted )"/>
		public RequestID InitializeResourceGroup( string name )
		{
			return InitializeResourceGroup( name, null );
		}
#endif

		/// <summary>
		/// Initialise all resource groups which are yet to be initialised in 
		/// the background.
		/// </summary>
		/// <param name="listener">Optional callback interface, take note of warnings in 
		/// the header and only use if you understand them.</param>
		/// <returns>Ticket identifying the request, use isProcessComplete() to 
		/// determine if completed if not using listener</returns>
		/// <see cref="ResourceGroupManager.InitializeAllResourceGroups"/>
		[OgreVersion( 1, 7, 2 )]
#if NET_40
		public virtual RequestID InitializeAllResourceGroups( OnOperationCompleted listener = null )
#else
		public virtual RequestID InitializeAllResourceGroups( OnOperationCompleted listener )
#endif
		{
#if AXIOM_THREAD_SUPPORT
	//queue a request
			ResourceRequest req = new ResourceRequest();
			req.Type = RequestType.InitializeAllGroups;
			req.Listener = listener;
			return AddRequest( req );
#else
			// synchronous
			ResourceGroupManager.Instance.InitializeAllResourceGroups();
			return 0;
#endif
		}

#if !NET_40
		/// <see cref="ResourceBackgroundQueue.InitializeAllResourceGroups( OnOperationCompleted )"/>
		public RequestID InitializeAllResourceGroups()
		{
			return InitializeAllResourceGroups( null );
		}
#endif

		/// <summary>
		/// Prepares a resource group in the background.
		/// </summary>
		/// <param name="name">The name of the resource group to prepare</param>
		/// <param name="listener">Optional callback interface, take note of warnings in 
		/// the header and only use if you understand them.</param>
		/// <returns>Ticket identifying the request, use isProcessComplete() to 
		/// determine if completed if not using listener</returns>
		/// <see cref="ResourceGroupManager.PrepareResourceGroup(string)"/>
		[OgreVersion( 1, 7, 2 )]
#if NET_40
		public virtual RequestID PrepareResourceGroup( string name, OnOperationCompleted listener = null )
#else
		public virtual RequestID PrepareResourceGroup( string name, OnOperationCompleted listener )
#endif
		{
#if AXIOM_THREAD_SUPPORT
	//queue a request
			ResourceRequest req = new ResourceRequest();
			req.Type = RequestType.PrepareGroup;
			req.GroupName = name;
			req.Listener = listener;
			return AddRequest( req );
#else
			// synchronous
			ResourceGroupManager.Instance.PrepareResourceGroup( name );
			return 0;
#endif
		}

#if !NET_40
		/// <see cref="ResourceBackgroundQueue.PrepareResourceGroup( string, OnOperationCompleted )"/>
		public RequestID PrepareResourceGroup( string name )
		{
			return PrepareResourceGroup( name, null );
		}
#endif

		/// <summary>
		/// Loads a resource group in the background.
		/// </summary>
		/// <param name="name">The name of the resource group to load</param>
		/// <param name="listener">Optional callback interface, take note of warnings in 
		/// the header and only use if you understand them.</param>
		/// <returns>Ticket identifying the request, use isProcessComplete() to 
		/// determine if completed if not using listener</returns>
		/// <see cref="ResourceGroupManager.LoadResourceGroup(string)"/>
		[OgreVersion( 1, 7, 2 )]
#if NET_40
		public virtual RequestID LoadResourceGroup( string name, OnOperationCompleted listener = null )
#else
		public virtual RequestID LoadResourceGroup( string name, OnOperationCompleted listener )
#endif
		{
#if AXIOM_THREAD_SUPPORT
	//queue a request
			ResourceRequest req = new ResourceRequest();
			req.Type = RequestType.LoadGroup;
			req.GroupName = name;
			req.Listener = listener;
			return AddRequest( req );
#else
			// synchronous
			ResourceGroupManager.Instance.LoadResourceGroup( name );
			return 0;
#endif
		}

#if !NET_40
		/// <see cref="ResourceBackgroundQueue.LoadResourceGroup( string, OnOperationCompleted )"/>
		public RequestID LoadResourceGroup( string name )
		{
			return LoadResourceGroup( name, null );
		}
#endif

		/// <summary>
		/// Prepare a single resource in the background.
		/// </summary>
		/// <param name="resType">The type of the resource (from ResourceManager.ResourceType)</param>
		/// <param name="name">The name of the Resource</param>
		/// <param name="group">The resource group to which this resource will belong</param>
		/// <param name="isManual">Is the resource to be manually loaded? If so, you should
		/// provide a value for the loader parameter</param>
		/// <param name="loader">The manual loader which is to perform the required actions
		/// when this resource is loaded; only applicable when you specify true
		/// for the previous parameter. NOTE: must be thread safe!!</param>
		/// <param name="loadParams">Optional pointer to a list of name/value pairs 
		/// containing loading parameters for this type of resource. Remember 
		/// that this must have a lifespan longer than the return of this call!</param>
		/// <param name="listener">Optional callback interface, take note of warnings in 
		/// the header and only use if you understand them.</param>
		/// <returns>Ticket identifying the request, use isProcessComplete() to 
		/// determine if completed if not using listener</returns>
		[OgreVersion( 1, 7, 2 )]
#if NET_40
		public virtual RequestID Prepare( string resType, string name, string group, bool isManual = false, IManualResourceLoader loader = null,
			NameValuePairList loadParams = null, OnOperationCompleted listener = null )
#else
		public virtual RequestID Prepare( string resType, string name, string group, bool isManual,
		                                  IManualResourceLoader loader, NameValuePairList loadParams,
		                                  OnOperationCompleted listener )
#endif
		{
#if AXIOM_THREAD_SUPPORT
	// queue a request
			ResourceRequest req = new ResourceRequest();
			req.Type = RequestType.PrepareResource;
			req.ResourceType = resType;
			req.ResourceName = name;
			req.GroupName = group;
			req.IsManual = isManual;
			req.Loader = loader;
			// Make instance copy of loadParams for thread independence
			req.LoadParams = ( loadParams != null ? new NameValuePairList( loadParams ) : null );
			req.Listener = listener;
			return AddRequest( req );
#else
			// synchronous
			ResourceManager rm = ResourceGroupManager.Instance.ResourceManagers[ resType ];
			rm.Prepare( name, group, isManual, loader, loadParams );
			return 0;
#endif
		}

#if !NET_40
		/// <see cref="ResourceBackgroundQueue.Prepare( string, string, string, bool, IManualResourceLoader, NameValuePairList, OnOperationCompleted )"/>
		public RequestID Prepare( string resType, string name, string group )
		{
			return Prepare( resType, name, group, false, null, null, null );
		}

		/// <see cref="ResourceBackgroundQueue.Prepare( string, string, string, bool, IManualResourceLoader, NameValuePairList, OnOperationCompleted )"/>
		public RequestID Prepare( string resType, string name, string group, bool isManual )
		{
			return Prepare( resType, name, group, isManual, null, null, null );
		}

		/// <see cref="ResourceBackgroundQueue.Prepare( string, string, string, bool, IManualResourceLoader, NameValuePairList, OnOperationCompleted )"/>
		public RequestID Prepare( string resType, string name, string group, bool isManual, IManualResourceLoader loader )
		{
			return Prepare( resType, name, group, isManual, loader, null, null );
		}

		/// <see cref="ResourceBackgroundQueue.Prepare( string, string, string, bool, IManualResourceLoader, NameValuePairList, OnOperationCompleted )"/>
		public RequestID Prepare( string resType, string name, string group, bool isManual, IManualResourceLoader loader,
		                          NameValuePairList loadParams )
		{
			return Prepare( resType, name, group, isManual, loader, loadParams, null );
		}
#endif

		/// <summary>
		/// Load a single resource in the background.
		/// </summary>
		/// <param name="resType">The type of the resource (from ResourceManager.ResourceType)</param>
		/// <param name="name">The name of the Resource</param>
		/// <param name="group">The resource group to which this resource will belong</param>
		/// <param name="isManual">Is the resource to be manually loaded? If so, you should
		/// provide a value for the loader parameter</param>
		/// <param name="loader">The manual loader which is to perform the required actions
		/// when this resource is loaded; only applicable when you specify true
		/// for the previous parameter. NOTE: must be thread safe!!</param>
		/// <param name="loadParams">Optional pointer to a list of name/value pairs 
		/// containing loading parameters for this type of resource. Remember 
		/// that this must have a lifespan longer than the return of this call!</param>
		/// <param name="listener">Optional callback interface, take note of warnings in 
		/// the header and only use if you understand them.</param>
		/// <returns>Ticket identifying the request, use isProcessComplete() to 
		/// determine if completed if not using listener</returns>
		[OgreVersion( 1, 7, 2 )]
#if NET_40
		public virtual RequestID Load( string resType, string name, string group, bool isManual = false, IManualResourceLoader loader = null,
			NameValuePairList loadParams = null, OnOperationCompleted listener = null )
#else
		public virtual RequestID Load( string resType, string name, string group, bool isManual, IManualResourceLoader loader,
		                               NameValuePairList loadParams, OnOperationCompleted listener )
#endif
		{
#if AXIOM_THREAD_SUPPORT
	// queue a request
			ResourceRequest req = new ResourceRequest();
			req.Type = RequestType.LoadResource;
			req.ResourceType = resType;
			req.ResourceName = name;
			req.GroupName = group;
			req.IsManual = isManual;
			req.Loader = loader;
			// Make instance copy of loadParams for thread independence
			req.LoadParams = ( loadParams != null ? new NameValuePairList( loadParams ) : null );
			req.Listener = listener;
			return AddRequest( req );
#else
			// synchronous
			ResourceManager rm = ResourceGroupManager.Instance.ResourceManagers[ resType ];
			rm.Load( name, group, isManual, loader, loadParams );
			return 0;
#endif
		}

#if !NET_40
		/// <see cref="ResourceBackgroundQueue.Load( string, string, string, bool, IManualResourceLoader, NameValuePairList, OnOperationCompleted )"/>
		public RequestID Load( string resType, string name, string group )
		{
			return Load( resType, name, group, false, null, null, null );
		}

		/// <see cref="ResourceBackgroundQueue.Load( string, string, string, bool, IManualResourceLoader, NameValuePairList, OnOperationCompleted )"/>
		public RequestID Load( string resType, string name, string group, bool isManual )
		{
			return Load( resType, name, group, isManual, null, null, null );
		}

		/// <see cref="ResourceBackgroundQueue.Load( string, string, string, bool, IManualResourceLoader, NameValuePairList, OnOperationCompleted )"/>
		public RequestID Load( string resType, string name, string group, bool isManual, IManualResourceLoader loader )
		{
			return Load( resType, name, group, isManual, loader, null, null );
		}

		/// <see cref="ResourceBackgroundQueue.Load( string, string, string, bool, IManualResourceLoader, NameValuePairList, OnOperationCompleted )"/>
		public RequestID Load( string resType, string name, string group, bool isManual, IManualResourceLoader loader,
		                       NameValuePairList loadParams )
		{
			return Load( resType, name, group, isManual, loader, loadParams, null );
		}
#endif

		/// <summary>
		/// Unload a single resource in the background.
		/// </summary>
		/// <param name="resType">The type of the resource (from ResourceManager.ResourceType)</param>
		/// <param name="name">The name of the Resource</param>
		/// <param name="listener">Optional callback interface, take note of warnings in 
		/// the header and only use if you understand them.</param>
		/// <returns>Ticket identifying the request, use isProcessComplete() to 
		/// determine if completed if not using listener</returns>
		[OgreVersion( 1, 7, 2 )]
#if NET_40
		public virtual RequestID Unload( string resType, string name, OnOperationCompleted listener = null )
#else
		public virtual RequestID Unload( string resType, string name, OnOperationCompleted listener )
#endif
		{
#if AXIOM_THREAD_SUPPORT
	// queue a request
			ResourceRequest req = new ResourceRequest();
			req.Type = RequestType.UnloadResource;
			req.ResourceType = resType;
			req.ResourceName = name;
			req.Listener = listener;
			return AddRequest( req );
#else
			// synchronous
			ResourceManager rm = ResourceGroupManager.Instance.ResourceManagers[ resType ];
			rm.Unload( name );
			return 0;
#endif
		}

#if !NET_40
		/// <see cref="ResourceBackgroundQueue.Unload( string, string, OnOperationCompleted )"/>
		public RequestID Unload( string resType, string name )
		{
			return Unload( resType, name, null );
		}
#endif

		/// <summary>
		/// Unload a single resource in the background.
		/// </summary>
		/// <param name="resType">The type of the resource (from ResourceManager.ResourceType)</param>
		/// <param name="handle">Handle to the resource</param>
		/// <param name="listener">Optional callback interface, take note of warnings in 
		/// the header and only use if you understand them.</param>
		/// <returns>Ticket identifying the request, use isProcessComplete() to 
		/// determine if completed if not using listener</returns>
		[OgreVersion( 1, 7, 2 )]
#if NET_40
		public virtual RequestID Unload( string resType, ResourceHandle handle, OnOperationCompleted listener = null )
#else
		public virtual RequestID Unload( string resType, ResourceHandle handle, OnOperationCompleted listener )
#endif
		{
#if AXIOM_THREAD_SUPPORT
	// queue a request
			ResourceRequest req = new ResourceRequest();
			req.Type = RequestType.UnloadResource;
			req.ResourceType = resType;
			req.ResourceHandle = handle;
			req.Listener = listener;
			return AddRequest( req );
#else
			// synchronous
			ResourceManager rm = ResourceGroupManager.Instance.ResourceManagers[ resType ];
			rm.Unload( handle );
			return 0;
#endif
		}

#if !NET_40
		/// <see cref="ResourceBackgroundQueue.Unload( string, ResourceHandle, OnOperationCompleted )"/>
		public RequestID Unload( string resType, ResourceHandle handle )
		{
			return Unload( resType, handle, null );
		}
#endif

		/// <summary>
		/// Unloads a resource group in the background.
		/// </summary>
		/// <param name="name">The name of the resource group to load</param>
		/// <param name="listener">Optional callback interface, take note of warnings in 
		/// the header and only use if you understand them.</param>
		/// <returns>Ticket identifying the request, use isProcessComplete() to 
		/// determine if completed if not using listener</returns>
		/// <see cref="ResourceGroupManager.UnloadResourceGroup(string)"/>
		[OgreVersion( 1, 7, 2 )]
#if NET_40
		public virtual RequestID UnloadResourceGroup( string name, OnOperationCompleted listener = null )
#else
		public virtual RequestID UnloadResourceGroup( string name, OnOperationCompleted listener )
#endif
		{
#if AXIOM_THREAD_SUPPORT
	// queue a request
			ResourceRequest req = new ResourceRequest();
			req.Type = RequestType.UnloadGroup;
			req.GroupName = name;
			req.Listener = listener;
			return AddRequest( req );
#else
			// synchronous
			ResourceGroupManager.Instance.UnloadResourceGroup( name );
			return 0;
#endif
		}

#if !NET_40
		/// <see cref="ResourceBackgroundQueue.UnloadResourceGroup( string, OnOperationCompleted )"/>
		public RequestID UnloadResourceGroup( string name )
		{
			return UnloadResourceGroup( name, null );
		}
#endif

		/// <summary>
		/// Returns whether a previously queued process has completed or not.
		/// </summary>
		/// <remarks>
		/// This method of checking that a background process has completed is
		/// the 'polling' approach. Each queued method takes an optional listener
		/// parameter to allow you to register a callback instead, which is
		/// arguably more efficient.
		/// @note
		/// Tickets are not stored once complete so do not accumulate over time.
		/// This is why a non-existent ticket will return 'true'.
		/// </remarks>
		/// <param name="ticket">The ticket which was returned when the process was queued</param>
		/// <returns>true if process has completed (or if the ticket is unrecognised), false otherwise</returns>
		[OgreVersion( 1, 7, 2 )]
		public virtual bool IsProcessComplete( RequestID ticket )
		{
			return !outstandingRequestSet.Contains( ticket );
		}

		/// <summary>
		/// Aborts background process.
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public void AbortRequest( RequestID ticket )
		{
			WorkQueue queue = Root.Instance.WorkQueue;
			queue.AbortRequest( ticket );
		}

		[OgreVersion( 1, 7, 2 )]
		protected RequestID AddRequest( ResourceRequest req )
		{
			WorkQueue queue = Root.Instance.WorkQueue;
			RequestID requestID = queue.AddRequest( workQueueChannel, (ushort)req.Type, req );
			outstandingRequestSet.Add( requestID );
			return requestID;
		}

		#endregion Methods

		#region ISingleton<ResourceBackgroundQueue> Members

		/// <summary>
		/// Singleton instance of this class.
		/// </summary>
		protected static ResourceBackgroundQueue instance;

		/// <summary>
		/// Gets the singleton instance of this class.
		/// </summary>
		public static ResourceBackgroundQueue Instance
		{
			get
			{
				return instance;
			}
		}

		/// <summary>
		/// Initialise the background queue system.
		/// </summary>
		/// <remarks>
		/// Called automatically by Root.Initialize.
		/// </remarks>
		[OgreVersion( 1, 7, 2 )]
		public bool Initialize( params object[] args )
		{
			WorkQueue wq = Root.Instance.WorkQueue;
			workQueueChannel = wq.GetChannel( "Axiom/ResourceBGQ" );
			wq.AddResponseHandler( workQueueChannel, this );
			wq.AddRequestHandler( workQueueChannel, this );

			return true;
		}

		#endregion ISingleton<ResourceBackgroundQueue> Members

		#region IRequestHandler Members

		/// <see cref="WorkQueue.IRequestHandler.CanHandleRequest"/>
		[OgreVersion( 1, 7, 2 )]
		public bool CanHandleRequest( WorkQueue.Request req, WorkQueue srcQ )
		{
			return true;
		}

		/// <see cref="WorkQueue.IRequestHandler.HandleRequest"/>
		[OgreVersion( 1, 7, 2 )]
		public WorkQueue.Response HandleRequest( WorkQueue.Request req, WorkQueue srcQ )
		{
			var resreq = (ResourceRequest)req.Data;

			if ( req.Aborted )
			{
				if ( resreq.Type == RequestType.PrepareResource || resreq.Type == RequestType.LoadResource )
				{
					resreq.LoadParams.Clear();
					resreq.LoadParams = null;
				}

				resreq.Result.Error = false;
				var resresp = new ResourceResponse( null, resreq );
				return new WorkQueue.Response( req, true, resresp );
			}

			ResourceManager rm = null;
			Resource resource = null;
			try
			{
				switch ( resreq.Type )
				{
					case RequestType.InitializeGroup:
						ResourceGroupManager.Instance.InitializeResourceGroup( resreq.GroupName );
						break;

					case RequestType.InitializeAllGroups:
						ResourceGroupManager.Instance.InitializeAllResourceGroups();
						break;

					case RequestType.PrepareGroup:
						ResourceGroupManager.Instance.PrepareResourceGroup( resreq.GroupName );
						break;

					case RequestType.LoadGroup:
#if AXIOM_THREAD_SUPPORT
						if ( Axiom.Configuration.Config.AxiomThreadLevel == 2 )
							ResourceGroupManager.Instance.PrepareResourceGroup( resreq.GroupName );
						else
							ResourceGroupManager.Instance.LoadResourceGroup( resreq.GroupName );
#endif
						break;

					case RequestType.UnloadGroup:
						ResourceGroupManager.Instance.UnloadResourceGroup( resreq.GroupName );
						break;

					case RequestType.PrepareResource:
						rm = ResourceGroupManager.Instance.ResourceManagers[ resreq.ResourceType ];
						resource = rm.Prepare( resreq.ResourceName, resreq.GroupName, resreq.IsManual, resreq.Loader, resreq.LoadParams,
						                       true );
						break;

					case RequestType.LoadResource:
#if AXIOM_THREAD_SUPPORT
						rm = ResourceGroupManager.Instance.ResourceManagers[ resreq.ResourceType ];
						if ( Axiom.Configuration.Config.AxiomThreadLevel == 2 )
							resource = rm.Prepare( resreq.ResourceName, resreq.GroupName, resreq.IsManual, resreq.Loader, resreq.LoadParams, true );
						else
							resource = rm.Load( resreq.ResourceName, resreq.GroupName, resreq.IsManual, resreq.Loader, resreq.LoadParams, true );
#endif
						break;

					case RequestType.UnloadResource:
						rm = ResourceGroupManager.Instance.ResourceManagers[ resreq.ResourceType ];
						if ( string.IsNullOrEmpty( resreq.ResourceName ) )
						{
							rm.Unload( resreq.ResourceHandle );
						}
						else
						{
							rm.Unload( resreq.ResourceName );
						}
						break;
				}
			}
			catch ( Exception e )
			{
				if ( resreq.Type == RequestType.PrepareResource || resreq.Type == RequestType.LoadResource )
				{
					resreq.LoadParams.Clear();
					resreq.LoadParams = null;
				}
				resreq.Result.Error = true;
				resreq.Result.Message = e.Message;

				//return error response
				var resresp = new ResourceResponse( resource, resreq );
				return new WorkQueue.Response( req, false, resresp, e.Message );
			}

			//success
			if ( resreq.Type == RequestType.PrepareResource || resreq.Type == RequestType.LoadResource )
			{
				resreq.LoadParams.Clear();
				resreq.LoadParams = null;
			}

			resreq.Result.Error = false;
			var resp = new ResourceResponse( resource, resreq );
			return new WorkQueue.Response( req, true, resp );
		}

		#endregion IRequestHandler Members

		#region IResponseHandler Members

		/// <see cref="WorkQueue.IResponseHandler.CanHandleResponse"/>
		[OgreVersion( 1, 7, 2 )]
		public bool CanHandleResponse( WorkQueue.Response res, WorkQueue srcq )
		{
			return true;
		}

		/// <see cref="WorkQueue.IResponseHandler.HandleResponse"/>
		[OgreVersion( 1, 7, 2 )]
		public void HandleResponse( WorkQueue.Response res, WorkQueue srcq )
		{
			if ( res.Request.Aborted )
			{
				outstandingRequestSet.Remove( res.Request.ID );
				return;
			}

			if ( res.Succeeded )
			{
				var resresp = (ResourceResponse)res.Data;
				// Complete full loading in main thread if semithreading
				ResourceRequest req = resresp.Request;

#if AXIOM_THREAD_SUPPORT
				if ( Configuration.Config.AxiomThreadLevel == 2 )
				{
					// These load commands would have been downgraded to prepare() for the background
					if ( req.Type == RequestType.LoadResource )
					{
						ResourceManager rm = ResourceGroupManager.Instance.ResourceManagers[ req.ResourceType ];
						rm.Load( req.ResourceName, req.GroupName, req.IsManual, req.Loader, req.LoadParams, true );
					}
					else if ( req.Type == RequestType.LoadGroup )
						ResourceGroupManager.Instance.LoadResourceGroup( req.GroupName );
				}
#endif
				outstandingRequestSet.Remove( res.Request.ID );

				// Call resource listener
				if ( resresp.Resource != null )
				{
					if ( req.Type == RequestType.LoadResource )
					{
						resresp.Resource.FireLoadingComplete( true );
					}
					else
					{
						resresp.Resource.FirePreparingComplete( true );
					}
				}

				// Call queue listener
				if ( req.Listener != null )
				{
					req.Listener.Invoke( res.Request.ID, req.Result );
				}
			}
		}

		#endregion IResponseHandler Members
	};
}