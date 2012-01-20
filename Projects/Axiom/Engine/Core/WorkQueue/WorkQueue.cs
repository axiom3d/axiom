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

using Axiom.Collections;

#endregion Namespace Declarations

namespace Axiom.Core
{
	/// <summary>
	/// Interface to a general purpose request / response style background work queue.
	/// </summary>
	/// <remarks>
	/// A work queue is a simple structure, where requests for work are placed
	/// onto the queue, then removed by a worker for processing, then finally
	/// a response is placed on the result queue for the originator to pick up
	/// at their leisure. The typical use for this is in a threaded environment, 
	/// although any kind of deferred processing could use this approach to 
	/// decouple and distribute work over a period of time even 
	/// if it was single threaded.
	/// @par
	/// WorkQueues also incorporate thread pools. One or more background worker threads
	/// can wait on the queue and be notified when a request is waiting to be
	/// processed. For maximal thread usage, a WorkQueue instance should be shared
	/// among many sources of work, rather than many work queues being created.
	/// This way, you can share a small number of hardware threads among a large 
	/// number of background tasks. This doesn't mean you have to implement all the
	/// request processing in one class, you can plug in many handlers in order to
	/// process the requests.
	/// @par
	/// This is an abstract interface definition; users can subclass this and 
	/// provide their own implementation if required to centralise task management
	/// in their own subsystems. We also provide a default implementation in the
	/// form of DefaultWorkQueue.
	/// </remarks>
	public abstract class WorkQueue : DisposableObject
	{
		protected ushort nextChannel;
		protected object channelMapMutex = new object();
		protected AxiomCollection<ushort> channelMap = new AxiomCollection<ushort>();

		/// <summary>
		/// Return whether the queue is paused ie not sending more work to workers and
		/// Set whether to pause further processing of any requests. 
		/// If true, any further requests will simply be queued and not processed until
		/// setPaused(false) is called. Any requests which are in the process of being
		/// worked on already will still continue.
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public abstract bool IsPaused
		{
			get;
			set;
		}

		/// <summary>
		/// Returns whether requests are being accepted right now and
		/// Set whether to accept new requests or not. 
		/// If true, requests are added to the queue as usual. If false, requests
		/// are silently ignored until setRequestsAccepted(true) is called.
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public abstract bool AreRequestsAccepted
		{
			get;
			set;
		}

		/// <summary>
		/// Get/Set the time limit imposed on the processing of responses in a
		/// single frame, in milliseconds (0 indicates no limit).
		/// This sets the maximum time that will be spent in processResponses() in 
		/// a single frame. The default is 8ms.
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public abstract long ResponseProcessingTimeLimit
		{
			get;
			set;
		}

		/// <summary>
		/// General purpose request structure.
		/// </summary>
		public class Request
		{
			// Note - There's no need to mark following fields as protected like in Ogre implementation of this struct,
			// because there's no inheritance for C# structs.

			/// <summary>
			/// The request channel, as an integer
			/// </summary>
			private ushort _channel;

			/// <summary>
			/// The request type, as an integer within the channel (user can define enumerations on this)
			/// </summary>
			private ushort _type;

			/// <summary>
			/// The details of the request (user defined)
			/// </summary>
			private object _data;

			/// <summary>
			/// Retry count - set this to non-zero to have the request try again on failure
			/// </summary>
			private byte _retryCount;

			/// <summary>
			/// Identifier (assigned by the system)
			/// </summary>
			private RequestID _id;

			/// <summary>
			/// Abort Flag
			/// </summary>
			private bool _aborted;

			/// <summary>
			/// Get the request channel (top level categorisation)
			/// </summary>
			[OgreVersion( 1, 7, 2 )]
			public ushort Channel
			{
				get
				{
					return _channel;
				}
			}

			/// <summary>
			/// Get the type of this request within the given channel
			/// </summary>
			[OgreVersion( 1, 7, 2 )]
			public ushort Type
			{
				get
				{
					return _type;
				}
			}

			/// <summary>
			/// Get the user details of this request
			/// </summary>
			[OgreVersion( 1, 7, 2 )]
			public object Data
			{
				get
				{
					return _data;
				}
			}

			/// <summary>
			/// Get the remaining retry count
			/// </summary>
			[OgreVersion( 1, 7, 2 )]
			public byte RetryCount
			{
				get
				{
					return _retryCount;
				}
			}

			/// <summary>
			/// Get the identifier of this request
			/// </summary>
			[OgreVersion( 1, 7, 2 )]
			public RequestID ID
			{
				get
				{
					return _id;
				}
			}

			/// <summary>
			/// Get the abort flag
			/// </summary>
			[OgreVersion( 1, 7, 2 )]
			public bool Aborted
			{
				get
				{
					return _aborted;
				}
			}

			/// <summary>
			/// Constructor
			/// </summary>
			[OgreVersion( 1, 7, 2 )]
			public Request( ushort channel, ushort rtype, object rData, byte retry, RequestID rid )
			{
				_channel = channel;
				_type = rtype;
				_data = rData;
				_retryCount = retry;
				_id = rid;
				_aborted = false;
			}

			/// <summary>
			/// Set the abort flag
			/// </summary>
			[OgreVersion( 1, 7, 2 )]
			public void AbortRequest()
			{
				_aborted = true;
			}

			public static bool operator ==( Request lr, Request rr )
			{
                // If both are null, or both are same instance, return true.
                if ( System.Object.ReferenceEquals( lr, rr ) )
                    return true;

                // If one is null, but not both, return false.
                if ( ( (object)lr == null ) || ( (object)rr == null ) )
                    return false;

				return lr._channel == rr._channel &&
					lr._type == rr._type &&
					lr._data == rr._data &&
					lr._retryCount == rr._retryCount &&
					lr._id == rr._id &&
					lr._aborted == rr._aborted;
			}

			public static bool operator !=( Request lr, Request rr )
			{
				return !( lr == rr );
			}

			public override bool Equals( object obj )
			{
				if ( obj != null && obj is Request )
					return this == (Request)obj;

				return false;
			}
		};

		/// <summary>
		/// General purpose response structure.
		/// </summary>
		public class Response
		{
			/// <summary>
			/// Pointer to the request that this response is in relation to
			/// </summary>
			private Request _request;

			/// <summary>
			/// Whether the work item succeeded or not
			/// </summary>
			private bool _success;

			/// <summary>
			/// Any diagnostic messages
			/// </summary>
			private string _messages;

			/// <summary>
			/// Data associated with the result of the process
			/// </summary>
			private object _data;

			/// <summary>
			/// Get the request that this is a response to (NB destruction destroys this)
			/// </summary>
			[OgreVersion( 1, 7, 2 )]
			public Request Request
			{
				get
				{
					return _request;
				}
			}

			/// <summary>
			/// Return whether this is a successful response
			/// </summary>
			[OgreVersion( 1, 7, 2 )]
			public bool Succeeded
			{
				get
				{
					return _success;
				}
			}

			/// <summary>
			/// Get any diagnostic messages about the process
			/// </summary>
			[OgreVersion( 1, 7, 2 )]
			public string Messages
			{
				get
				{
					return _messages;
				}
			}

			/// <summary>
			/// Return the response data (user defined, only valid on success)
			/// </summary>
			[OgreVersion( 1, 7, 2 )]
			public object Data
			{
				get
				{
					return _data;
				}
			}

			public Response( Request rq, bool success, object data )
				: this( rq, success, data, string.Empty )
			{
			}

			[OgreVersion( 1, 7, 2 )]
			public Response( Request rq, bool success, object data, string msg )
			{
				_request = rq;
				_success = success;
				_messages = msg;
				_data = data;
			}

			/// <summary>
			/// Abort the request
			/// </summary>
			[OgreVersion( 1, 7, 2 )]
			public void AbortRequest()
			{
				this.Request.AbortRequest();
				_data = null;
			}
		};

		/// <summary>
		/// Interface definition for a handler of requests.
		/// </summary>
		/// <remarks>
		/// User classes are expected to implement this interface in order to
		/// process requests on the queue. It's important to realise that
		/// the calls to this class may be in a separate thread to the main
		/// render context, and as such it may not be possible to make
		/// rendersystem or other GPU-dependent calls in this handler. You can only
		/// do so if the queue was created with 'workersCanAccessRenderSystem'
		/// set to true, and OGRE_THREAD_SUPPORT=1, but this puts extra strain
		/// on the thread safety of the render system and is not recommended.
		/// It is best to perform CPU-side work in these handlers and let the
		/// response handler transfer results to the GPU in the main render thread.
		/// </remarks>
		public interface IRequestHandler
		{
			/// <summary>
			/// Return whether this handler can process a given request.
			/// </summary>
			/// <remarks>
			/// Defaults to true, but if you wish to add several handlers each of
			/// which deal with different types of request, you can override
			/// this method.
			/// </remarks>
			[OgreVersion( 1, 7, 2 )]
			bool CanHandleRequest( Request req, WorkQueue srcQ );

			/// <summary>
			/// The handler method every subclass must implement. 
			/// If a failure is encountered, return a Response with a failure
			/// result rather than raise an exception.
			/// </summary>
			/// <param name="req">The Request structure, which is effectively owned by the
			/// handler during this call. It must be attached to the returned
			/// Response regardless of success or failure.</param>
			/// <param name="srcQ">The work queue that this request originated from</param>
			/// <returns>Pointer to a Response object - the caller is responsible for deleting the object.</returns>
			[OgreVersion( 1, 7, 2 )]
			Response HandleRequest( Request req, WorkQueue srcQ );
		};

		/// <summary>
		/// Interface definition for a handler of responses.
		/// </summary>
		/// <remarks>
		/// User classes are expected to implement this interface in order to
		/// process responses from the queue. All calls to this class will be 
		/// in the main render thread and thus all GPU resources will be
		/// available.
		/// </remarks>
		public interface IResponseHandler
		{
			/// <summary>
			/// Return whether this handler can process a given response.
			/// </summary>
			/// <remarks>
			/// Defaults to true, but if you wish to add several handlers each of
			/// which deal with different types of response, you can override
			/// this method.
			/// </remarks>
			[OgreVersion( 1, 7, 2 )]
			bool CanHandleResponse( Response res, WorkQueue srcq );

			/// <summary>
			/// The handler method every subclass must implement.
			/// </summary>
			/// <param name="res">The Response structure. The caller is responsible for
			/// deleting this after the call is made, none of the data contained
			/// (except pointers to structures in user Any data) will persist
			/// after this call is returned.</param>
			/// <param name="srcq">The work queue that this request originated from</param>
			[OgreVersion( 1, 7, 2 )]
			void HandleResponse( Response res, WorkQueue srcq );
		};

		/// <summary>
		/// Start up the queue with the options that have been set.
		/// </summary>
		/// <param name="forceRestart">If the queue is already running, whether to shut it down and restart.</param>
		[OgreVersion( 1, 7, 2 )]
#if NET_40
		public abstract void Startup( bool forceRestart = true );
#else
		public abstract void Startup( bool forceRestart );

		public void Startup()
		{
			this.Startup( true );
		}
#endif

		/// <summary>
		/// Add a request handler instance to the queue.
		/// </summary>
		/// <remarks>
		/// Every queue must have at least one request handler instance for each 
		/// channel in which requests are raised. If you 
		/// add more than one handler per channel, then you must implement canHandleRequest 
		/// differently	in each if you wish them to respond to different requests.
		/// </remarks>
		/// <param name="channel">The channel for requests you want to handle</param>
		/// <param name="rh">Your handler</param>
		[OgreVersion( 1, 7, 2 )]
		public abstract void AddRequestHandler( ushort channel, IRequestHandler rh );

		/// <summary>
		/// Remove a request handler.
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public abstract void RemoveRequestHandler( ushort channel, IRequestHandler rh );

		/// <summary>
		/// Add a response handler instance to the queue.
		/// </summary>
		/// <remarks>
		/// Every queue must have at least one response handler instance for each 
		/// channel in which requests are raised. If you add more than one, then you 
		/// must implement canHandleResponse differently in each if you wish them 
		/// to respond to different responses.
		/// </remarks>
		/// <param name="channel">The channel for responses you want to handle</param>
		/// <param name="rh">Your handler</param>
		[OgreVersion( 1, 7, 2 )]
		public abstract void AddResponseHandler( ushort channel, IResponseHandler rh );

		/// Remove a Response handler.
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public abstract void RemoveResponseHandler( ushort channel, IResponseHandler rh );

		/// <summary>
		/// Add a new request to the queue.
		/// </summary>
		/// <param name="channel">The channel this request will go into = 0; the channel is the top-level
		/// categorisation of the request</param>
		/// <param name="requestType">An identifier that's unique within this queue which
		/// identifies the type of the request (user decides the actual value)</param>
		/// <param name="rData">The data required by the request process.</param>
		/// <param name="retryCount">The number of times the request should be retried if it fails.</param>
		/// <param name="forceSynchronous">Forces the request to be processed immediately even if threading is enabled.</param>
		/// <returns>The ID of the request that has been added</returns>
		[OgreVersion( 1, 7, 2 )]
#if NET_40
		public abstract RequestID AddRequest( ushort channel, ushort requestType, object rData, byte retryCount = 0, bool forceSynchronous = false );
#else
		public abstract RequestID AddRequest( ushort channel, ushort requestType, object rData, byte retryCount, bool forceSynchronous );

		public RequestID AddRequest( ushort channel, ushort requestType, object rData )
		{
			return this.AddRequest( channel, requestType, rData, 0, false );
		}

		public RequestID AddRequest( ushort channel, ushort requestType, object rData, byte retryCount )
		{
			return this.AddRequest( channel, requestType, rData, retryCount, false );
		}
#endif

		/// <summary>
		/// Abort a previously issued request.
		/// If the request is still waiting to be processed, it will be 
		/// removed from the queue.
		/// </summary>
		/// <param name="id">The ID of the previously issued request.</param>
		[OgreVersion( 1, 7, 2 )]
		public abstract void AbortRequest( RequestID id );

		/// <summary>
		/// Abort all previously issued requests in a given channel.
		/// Any requests still waiting to be processed of the given channel, will be 
		/// removed from the queue.
		/// </summary>
		/// <param name="channel">The type of request to be aborted</param>
		[OgreVersion( 1, 7, 2 )]
		public abstract void AbortRequestByChannel( ushort channel );

		/// <summary>
		/// Abort all previously issued requests.
		/// Any requests still waiting to be processed will be removed from the queue.
		/// Any requests that are being processed will still complete.
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public abstract void AbortAllRequests();

		/// <summary>
		/// Process the responses in the queue.
		/// </summary>
		/// <remarks>
		/// This method is public, and must be called from the main render
		/// thread to 'pump' responses through the system. The method will usually
		/// try to clear all responses before returning = 0; however, you can specify
		/// a time limit on the response processing to limit the impact of
		/// spikes in demand by calling setResponseProcessingTimeLimit.
		/// </remarks>
		[OgreVersion( 1, 7, 2 )]
		public abstract void ProcessResponses();

		/// <summary>
		/// Shut down the queue.
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public abstract void Shutdown();

		/// <summary>
		/// Get a channel ID for a given channel name.
		/// </summary>
		/// <remarks>
		/// Channels are assigned on a first-come, first-served basis and are
		/// not persistent across application instances. This method allows 
		/// applications to not worry about channel clashes through manually
		/// assigned channel numbers.
		/// </remarks>
		[OgreVersion( 1, 7, 2 )]
		public virtual ushort GetChannel( string channelName )
		{
			lock ( channelMapMutex )
			{
				if ( !channelMap.ContainsKey( channelName ) )
					channelMap.Add( channelName, nextChannel++ );
			}

			return channelMap[ channelName ];
		}
	};
}
