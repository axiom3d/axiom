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

using System.Collections.Generic;
using System.Text;
using Axiom.Collections;

#endregion Namespace Declarations

namespace Axiom.Core
{
	/// <summary>
	/// Base for a general purpose request / response style background work queue.
	/// </summary>
	public abstract class DefaultWorkQueueBase : WorkQueue
	{
		protected string name;
		protected int workerThreadCount;
		protected bool workerRenderSystemAccess;
		protected bool isRunning;
		protected long responseTimeLimitMS;
		protected Deque<Request> requestQueue = new Deque<Request>();
		protected Deque<Request> processQueue = new Deque<Request>();
		protected Deque<Response> responseQueue = new Deque<Response>();

		/// <summary>
		/// Intermediate structure to hold a pointer to a request handler which 
		/// provides insurance against the handler itself being disconnected
		/// while the list remains unchanged.
		/// </summary>
		protected class RequestHandlerHolder
		{
			private object _mutex = new object();
			private IRequestHandler _handler;

			/// <summary>
			/// Get handler pointer - note, only use this for == comparison or similar,
			/// do not attempt to call it as it is not thread safe.
			/// </summary>
			[OgreVersion( 1, 7, 2 )]
			public IRequestHandler Handler
			{
				get
				{
					return _handler;
				}
			}

			[OgreVersion( 1, 7, 2 )]
			public RequestHandlerHolder( IRequestHandler handler )
			{
				_handler = handler;
			}

			/// <summary>
			/// Disconnect the handler to allow it to be destroyed
			/// </summary>
			[OgreVersion( 1, 7, 2 )]
			public void DisconnectHandler()
			{
				// write lock - must wait for all requests to finish
				lock ( _mutex )
				{
					_handler = null;
				}
			}

			/// <summary>
			/// Process a request if possible.
			/// </summary>
			/// <returns>Valid response if processed, null otherwise</returns>
			[OgreVersion( 1, 7, 2 )]
			public Response? HandleRequest( Request req, WorkQueue srcQ )
			{
				// Read mutex so that multiple requests can be processed by the
				// same handler in parallel if required
				Response? response = null;
				lock ( _mutex )
				{
					if ( _handler != null )
					{
						if ( _handler.CanHandleRequest( req, srcQ ) )
							response = _handler.HandleRequest( req, srcQ );
					}
				}

				return response;
			}
		};

		protected Dictionary<ushort, List<RequestHandlerHolder>> requestHandlers = new Dictionary<ushort, List<RequestHandlerHolder>>();
		protected Dictionary<ushort, List<IResponseHandler>> responseHandlers = new Dictionary<ushort, List<IResponseHandler>>();
		protected uint requestCount;
		protected bool paused;
		protected bool acceptRequests;
		protected bool shuttingDown;

		//Mutex declarations
		protected static readonly object requestMutex = new object();
		protected static readonly object processMutex = new object();
		protected static readonly object responseMutex = new object();
		protected static readonly object requestHandlerMutex = new object();

		/// <summary>
		/// Get the name of the work queue
		/// </summary>
		public string Name
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return name;
			}
		}

		/// <summary>
		/// Get the number of worker threads that this queue will start when 
		/// startup() is called (default 1).
		/// </summary>
		/// <remarks>
		/// Calling the setter of this will have no effect unless the queue is shut down and restarted.
		/// </remarks>
		public virtual int WorkerThreadCount
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return workerThreadCount;
			}

			[OgreVersion( 1, 7, 2 )]
			set
			{
				workerThreadCount = value;
			}
		}

		/// <summary>
		/// Get/Set whether worker threads will be allowed to access render system
		/// resources. 
		/// Accessing render system resources from a separate thread can require that
		/// a context is maintained for that thread. Also, it requires that the
		/// render system is running in threadsafe mode, which only happens
		/// when AXIOM_THREAD_SUPPORT=1. This option defaults to false, which means
		/// that threads can not use GPU resources, and the render system can 
		/// work in non-threadsafe mode, which is more efficient.
		/// </summary>
		/// <remarks>
		/// Calling the setter of this will have no effect unless the queue is shut down and restarted.
		/// </remarks>
		public virtual bool WorkersCanAccessRenderSystem
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return workerRenderSystemAccess;
			}

			[OgreVersion( 1, 7, 2 )]
			set
			{
				workerRenderSystemAccess = value;
			}
		}

		/// <see cref="Axiom.Core.WorkQueue.IsPaused"/>
		public override bool IsPaused
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return paused;
			}

			[OgreVersion( 1, 7, 2 )]
			set
			{
				lock ( requestMutex )
					paused = value;
			}
		}

		/// <see cref="Axiom.Core.WorkQueue.AreRequestsAccepted"/>
		public override bool AreRequestsAccepted
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return acceptRequests;
			}

			[OgreVersion( 1, 7, 2 )]
			set
			{
				lock ( requestMutex )
					acceptRequests = value;
			}
		}

		/// <see cref="Axiom.Core.WorkQueue.ResponseProcessingTimeLimit"/>
		public override long ResponseProcessingTimeLimit
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return responseTimeLimitMS;
			}

			[OgreVersion( 1, 7, 2 )]
			set
			{
				responseTimeLimitMS = value;
			}
		}

		/// <summary>
		/// Returns whether the queue is trying to shut down.
		/// </summary>
		public virtual bool IsShuttingDown
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return shuttingDown;
			}
		}

#if NET_40
		/// <summary>
		/// Contructor
		/// Call startup() to initialise.
		/// </summary>
		/// <param name="name">Optional name, just helps to identify logging output</param>
		[OgreVersion( 1, 7, 2 )]
		public DefaultWorkQueueBase( string name = "" )
#else
		public DefaultWorkQueueBase()
			: this( string.Empty )
		{
		}

		/// <summary>
		/// Contructor
		/// Call startup() to initialise.
		/// </summary>
		/// <param name="name">Optional name, just helps to identify logging output</param>
		[OgreVersion( 1, 7, 2 )]
		public DefaultWorkQueueBase( string name )
#endif
			: base()
		{
			this.name = name;
			workerThreadCount = 1;
			responseTimeLimitMS = 8;
			AreRequestsAccepted = true;
		}

		[OgreVersion( 1, 7, 2, "~DefaultWorkQueueBase" )]
		protected override void dispose( bool disposeManagedResources )
		{
			if ( !this.IsDisposed )
			{
				if ( disposeManagedResources )
				{
					//Shutdown(); // can't call here; abstract function

					requestQueue.Clear();
					responseQueue.Clear();
				}
			}

			base.dispose( disposeManagedResources );
		}

		/// <see cref="Axiom.Core.WorkQueue.AddRequestHandler"/>
		[OgreVersion( 1, 7, 2 )]
		public override void AddRequestHandler( ushort channel, WorkQueue.IRequestHandler rh )
		{
			lock ( requestHandlerMutex )
			{
				if ( !requestHandlers.ContainsKey( channel ) )
					requestHandlers.Add( channel, new List<RequestHandlerHolder>() );

				bool duplicate = false;
				foreach ( var j in requestHandlers[ channel ] )
				{
					if ( j.Handler == rh )
					{
						duplicate = true;
						break;
					}
				}
				if ( !duplicate )
					requestHandlers[ channel ].Add( new RequestHandlerHolder( rh ) );
			}
		}

		/// <see cref="Axiom.Core.WorkQueue.RemoveRequestHandler"/>
		[OgreVersion( 1, 7, 2 )]
		public override void RemoveRequestHandler( ushort channel, WorkQueue.IRequestHandler rh )
		{
			lock ( requestHandlerMutex )
			{
				if ( requestHandlers.ContainsKey( channel ) )
				{
					foreach ( var j in requestHandlers[ channel ] )
					{
						if ( j.Handler == rh )
						{
							// Disconnect - this will make it safe across copies of the list
							// this is threadsafe and will wait for existing processes to finish
							j.DisconnectHandler();
							requestHandlers[ channel ].Remove( j );
							break;
						}
					}
				}
			}
		}

		/// <see cref="Axiom.Core.WorkQueue.AddResponseHandler"/>
		[OgreVersion( 1, 7, 2 )]
		public override void AddResponseHandler( ushort channel, WorkQueue.IResponseHandler rh )
		{
			if ( !responseHandlers.ContainsKey( channel ) )
				responseHandlers.Add( channel, new List<IResponseHandler>() );

			if ( !responseHandlers[ channel ].Contains( rh ) )
				responseHandlers[ channel ].Add( rh );
		}

		/// <see cref="WorkQueue.RemoveResponseHandler"/>
		[OgreVersion( 1, 7, 2 )]
		public override void RemoveResponseHandler( ushort channel, WorkQueue.IResponseHandler rh )
		{
			if ( responseHandlers.ContainsKey( channel ) )
			{
				if ( responseHandlers[ channel ].Contains( rh ) )
					responseHandlers[ channel ].Remove( rh );
			}
		}

		protected string GetThreadName()
		{
#if AXIOM_THREAD_SUPPORT
			string name = System.Threading.Thread.CurrentThread.Name;
			int threadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
			return string.Format( "[{0}]{1}", threadId, string.IsNullOrEmpty( name ) ? "<noname>" : name );
#else
			return "main";
#endif
		}

		/// <see cref="Axiom.Core.WorkQueue.AddRequest(ushort, ushort, object, byte, bool)"/>
		[OgreVersion( 1, 7, 2 )]
#if NET_40
		public override RequestID AddRequest( ushort channel, ushort requestType, object rData, byte retryCount = 0, bool forceSynchronous = false )
#else
		public override RequestID AddRequest( ushort channel, ushort requestType, object rData, byte retryCount, bool forceSynchronous )
#endif
		{
			Request req;
			RequestID rid;
			// lock to acquire rid and push request to the queue
			lock ( requestMutex )
			{
				if ( !acceptRequests || shuttingDown )
					return 0;

				rid = ++requestCount;
				req = new Request( channel, requestType, rData, retryCount, rid );

				LogManager.Instance.Write(
					LogMessageLevel.Trivial,
					false,
					"DefaultWorkQueueBase('{0}') - QUEUED(thread:{1}): ID={2} channel={3} requestType={4}",
					name,
					GetThreadName(),
					rid,
					channel,
					requestType
					);

#if AXIOM_THREAD_SUPPORT
				if ( !forceSynchronous )
				{
					requestQueue.Add( req );
					NotifyWorkers();
					return rid;
				}
#endif
				ProcessRequestResponse( req, true );
				return rid;
			}
		}

		/// <summary>
		/// Notify workers about a new request.
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		protected abstract void NotifyWorkers();

		/// <summary>
		/// Put a Request on the queue with a specific RequestID.
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		protected void AddRequestWithRID( RequestID rid, ushort channel, ushort requestType, object rData, byte retryCount )
		{
			// lock to push request to the queue
			lock ( requestMutex )
			{
				if ( shuttingDown )
					return;

				Request req = new Request( channel, requestType, rData, retryCount, rid );

				LogManager.Instance.Write(
					LogMessageLevel.Trivial,
					false,
					"DefaultWorkQueueBase('{0}') - REQUEUED(thread:{1}): ID={2} channel={3} requestType={4}",
					name,
					GetThreadName(),
					rid,
					channel,
					requestType
					);

#if AXIOM_THREAD_SUPPORT
				requestQueue.Add( req );
				NotifyWorkers();
#else
				ProcessRequestResponse( req, true );
#endif
			}
		}

		/// <see cref="WorkQueue.AbortRequest"/>
		[OgreVersion( 1, 7, 2 )]
		public override void AbortRequest( RequestID id )
		{
			// NOTE: Pending requests are exist any of RequestQueue, ProcessQueue and
			// ResponseQueue when keeping ProcessMutex, so we check all of these queues.

			lock ( processMutex )
			{
				foreach ( var i in processQueue )
				{
					if ( i.ID == id )
					{
						i.AbortRequest();
						break;
					}
				}
			}

			lock ( requestMutex )
			{
				foreach ( var i in requestQueue )
				{
					if ( i.ID == id )
					{
						i.AbortRequest();
						break;
					}
				}
			}

			lock ( responseMutex )
			{
				foreach ( var i in responseQueue )
				{
					if ( i.Request.ID == id )
					{
						i.AbortRequest();
						break;
					}
				}
			}
		}

		/// <see cref="WorkQueue.AbortRequestByChannel"/>
		[OgreVersion( 1, 7, 2 )]
		public override void AbortRequestByChannel( ushort channel )
		{
			lock ( processMutex )
			{
				foreach ( var i in processQueue )
				{
					if ( i.Channel == channel )
						i.AbortRequest();
				}
			}

			lock ( requestMutex )
			{
				foreach ( var i in requestQueue )
				{
					if ( i.Channel == channel )
						i.AbortRequest();
				}
			}

			lock ( responseMutex )
			{
				foreach ( var i in responseQueue )
				{
					if ( i.Request.Channel == channel )
						i.AbortRequest();
				}
			}
		}

		/// <see cref="WorkQueue.AbortAllRequests"/>
		[OgreVersion( 1, 7, 2 )]
		public override void AbortAllRequests()
		{
			lock ( processMutex )
			{
				foreach ( var i in processQueue )
					i.AbortRequest();
			}

			lock ( requestMutex )
			{
				foreach ( var i in requestQueue )
					i.AbortRequest();
			}

			lock ( responseMutex )
			{
				foreach ( var i in responseQueue )
					i.AbortRequest();
			}
		}

		/// <summary>
		/// Process the next request on the queue.
		/// </summary>
		/// <remarks>
		/// This method is public, but only intended for advanced users to call. 
		/// The only reason you would call this, is if you were using your 
		/// own thread to drive the worker processing. The thread calling this
		/// method will be the thread used to call the RequestHandler.
		/// </remarks>
		[OgreVersion( 1, 7, 2 )]
		internal virtual void ProcessNextRequest()
		{
			Request? request = null;
			// scoped to only lock while retrieving the next request
			lock ( processMutex )
			{
				lock ( requestMutex )
				{
					if ( requestQueue.Count > 0 )
					{
						request = requestQueue.RemoveFromHead();
						processQueue.Add( request.Value );
					}
				}
			}

			if ( request != null )
				ProcessRequestResponse( request.Value, false );
		}

		/// <summary>
		/// Main function for each thread spawned.
		/// </summary>
		internal abstract void ThreadMain();

		protected void ProcessRequestResponse( Request r, bool synchronous )
		{
			Response response;
			bool hasResponse = ProcessRequest( r, out response );

			lock ( processMutex )
			{
				foreach ( var it in processQueue )
				{
					if ( it == r )
					{
						processQueue.Remove( it );
						break;
					}
				}

				if ( hasResponse )
				{
					if ( !response.Succeeded )
					{
						// Failed, should we retry?
						Request req = response.Request;
						if ( req.RetryCount != 0 )
						{
							AddRequestWithRID( req.ID, req.Channel, req.Type, req.Data, (byte)( req.RetryCount - 1 ) );
							// discard response (this also deletes request)
							//OGRE_DELETE response;
							return;
						}
					}
					if ( synchronous )
					{
						ProcessResponse( response );
						//OGRE_DELETE response;
					}
					else
					{
						if ( response.Request.Aborted )
						{
							// destroy response user data
							response.AbortRequest();
						}
						// Queue response
						lock ( responseMutex )
						{
							responseQueue.Add( response );
							// no need to wake thread, this is processed by the main thread
						}
					}
				}
				else
				{
					// no response, delete request
					LogManager.Instance.Write(
						"DefaultWorkQueueBase('{0}') warning: no handler processed request {1}, channel {2}, type {3}",
						name,
						r.ID,
						r.Channel,
						r.Type
						);
					//OGRE_DELETE r;
				}
			}
		}

		[OgreVersion( 1, 7, 2 )]
		public override void ProcessResponses()
		{
			long msStart = Root.Instance.Timer.Milliseconds;
			long msCurrent = msStart;

			// keep going until we run out of responses or out of time
			while ( true )
			{
				Response? response = null;
				lock ( responseMutex )
				{
					if ( responseQueue.Count == 0 )
						break; // exit loop
					else
						response = responseQueue.RemoveFromHead();
				}

				if ( response != null )
				{
					ProcessResponse( response.Value );
					response = null;
				}

				//time limit
				if ( responseTimeLimitMS > 0 )
				{
					msCurrent = Root.Instance.Timer.Milliseconds;
					if ( msCurrent - msStart > responseTimeLimitMS )
						break;
				}
			}
		}

		//Original method declaration was protected Response ProcessRequest(Request r)
		[OgreVersion( 1, 7, 2 )]
		protected bool ProcessRequest( Request r, out Response response )
		{
			Dictionary<ushort, List<RequestHandlerHolder>> handlerListCopy;
			bool retValue = false;
			response = new Response();

			// lock the list only to make a copy of it, to maximise parallelism
			lock ( requestHandlerMutex )
				handlerListCopy = new Dictionary<ushort, List<RequestHandlerHolder>>( requestHandlers );

			var dbgMsg = new StringBuilder();
			dbgMsg.AppendFormat(
				"{0}): ID={1} channel={2} requestType={3}",
				GetThreadName(),
				r.ID,
				r.Channel,
				r.Type
				);

			LogManager.Instance.Write( LogMessageLevel.Trivial, false, "DefaultWorkQueueBase('{0}') - PROCESS_REQUEST_START({1}", name, dbgMsg.ToString() );
			if ( handlerListCopy.ContainsKey( r.Channel ) )
			{
				List<RequestHandlerHolder> handlers = handlerListCopy[ r.Channel ];
				for ( int j = handlers.Count - 1; j >= 0; --j )
				{
					// threadsafe call which tests canHandleRequest and calls it if so
					Response? res = handlers[ j ].HandleRequest( r, this );
					retValue = res.HasValue;

					if ( retValue )
					{
						response = res.Value;
						break;
					}
				}
			}

			LogManager.Instance.Write(
				LogMessageLevel.Trivial,
				false,
				"DefaultWorkQueueBase('{0}') - PROCESS_REQUEST_END({1} processed={2}",
				name,
				dbgMsg.ToString(),
				retValue
				);

			return retValue;
		}

		[OgreVersion( 1, 7, 2 )]
		protected void ProcessResponse( Response r )
		{
			var dbgMsg = new StringBuilder();
			dbgMsg.AppendFormat(
				"thread:{0}): ID={1} success={2} messages=[{3}] channel={4} requestType={5}",
				GetThreadName(),
				r.Request.ID,
				r.Succeeded,
				r.Messages,
				r.Request.Channel,
				r.Request.Type
				);

			LogManager.Instance.Write( LogMessageLevel.Trivial, false, "DefaultWorkQueueBase('{0}') - PROCESS_RESPONSE_START({1}", name, dbgMsg.ToString() );
			ushort channel = r.Request.Channel;
			if ( responseHandlers.ContainsKey( channel ) )
			{
				for ( int j = responseHandlers[ channel ].Count - 1; j >= 0; --j )
				{
					if ( responseHandlers[ channel ][ j ].CanHandleResponse( r, this ) )
						responseHandlers[ channel ][ j ].HandleResponse( r, this );
				}
			}

			LogManager.Instance.Write( LogMessageLevel.Trivial, false, "DefaultWorkQueueBase('{0}') - PROCESS_RESPONSE_END({1}", name, dbgMsg.ToString() );
		}
	};
}
