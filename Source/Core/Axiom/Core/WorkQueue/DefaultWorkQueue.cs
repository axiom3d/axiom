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


#if AXIOM_THREAD_SUPPORT
using System.Collections.Generic;
using System.Threading;
#endif

#endregion Namespace Declarations

namespace Axiom.Core
{
	/// <summary>
	/// Implementation of a general purpose request / response style background work queue.
	/// </summary>
	/// <remarks>
	/// This default implementation of a work queue starts a thread pool and 
	/// provides queues to process requests.
	/// </remarks>
	public class DefaultWorkQueue : DefaultWorkQueueBase
	{
		protected int numThreadsRegisteredWithRS;

		/// <summary>
		/// Synchroniser token to wait / notify on thread init
		/// </summary>
		protected static readonly object initSync = new object();

#if AXIOM_THREAD_SUPPORT
		protected List<Thread> workers;
#endif

		[OgreVersion( 1, 7, 2 )]
#if NET_40
		public DefaultWorkQueue( string name = "" )
#else
		public DefaultWorkQueue()
			: this( string.Empty )
		{
		}


		public DefaultWorkQueue( string name )
#endif
			: base( name )
		{
		}

		[OgreVersion( 1, 7, 2, "~DefaultWorkQueue" )]
		protected override void dispose( bool disposeManagedResources )
		{
			if ( !IsDisposed )
			{
				if ( disposeManagedResources )
				{
					Shutdown();
				}
			}

			base.dispose( disposeManagedResources );
		}

		/// <see cref="Axiom.Core.WorkQueue.Startup(bool)"/>
#if NET_40
		public override void Startup( bool forceRestart = true )
#else
		public override void Startup( bool forceRestart )
#endif
		{
			if ( isRunning )
			{
				if ( forceRestart )
				{
					Shutdown();
				}
				else
				{
					return;
				}
			}

			shuttingDown = false;
			LogManager.Instance.Write( "DefaultWorkQueue('{0}') initialising on thread {1}.", name, GetThreadName() );

#if AXIOM_THREAD_SUPPORT
			if ( workerRenderSystemAccess )
				Root.Instance.RenderSystem.PreExtraThreadsStarted();

			numThreadsRegisteredWithRS = 0;
			workers = new List<Thread>( workerThreadCount );
			for ( byte i = 0; i < workerThreadCount; ++i )
			{
				var worker = new Thread( this.ThreadMain );
				worker.Start();
				workers.Add( worker );
			}

			if ( workerRenderSystemAccess )
			{
				lock ( initSync )
				{
					// have to wait until all threads are registered with the render system
					while ( numThreadsRegisteredWithRS < workerThreadCount )
						Monitor.Wait( initSync );
				}

				Root.Instance.RenderSystem.PostExtraThreadsStarted();
			}
#endif

			isRunning = true;
		}

		/// <summary>
		/// Notify that a thread has registered itself with the render system
		/// </summary>
		protected virtual void NotifyThreadRegistered()
		{
			lock ( initSync )
			{
				++this.numThreadsRegisteredWithRS;

#if AXIOM_THREAD_SUPPORT
				// wake up main thread
				Monitor.PulseAll( initSync );
#endif
			}
		}

		/// <see cref="Axiom.Core.WorkQueue.Shutdown"/>
		public override void Shutdown()
		{
			if ( !isRunning )
			{
				return;
			}

			LogManager.Instance.Write( "DefaultWorkQueue('{0}') shutting down on thread {1}.", name, GetThreadName() );
			shuttingDown = true;
			AbortAllRequests();
#if AXIOM_THREAD_SUPPORT
			lock ( requestMutex )
				Monitor.PulseAll( requestMutex );

			// all our threads should have been woken now, so join
			foreach ( var i in workers )
				i.Join();

			workers.Clear();
#endif
			isRunning = false;
		}

		/// <see cref="DefaultWorkQueueBase.NotifyWorkers"/>
		[OgreVersion( 1, 7, 2 )]
		protected override void NotifyWorkers()
		{
#if AXIOM_THREAD_SUPPORT
			// wake up waiting thread
			Monitor.Pulse( requestMutex );
#endif
		}

		/// <summary>
		/// To be called by a separate thread; will return immediately if there
		/// are items in the queue, or suspend the thread until new items are added	otherwise.
		/// </summary>
		protected virtual void WaitForNextRequest()
		{
#if AXIOM_THREAD_SUPPORT
			// Lock; note that OGRE_THREAD_WAIT will free the lock
			lock ( requestMutex )
			{
				if ( requestQueue.Count == 0 )
				{
					// frees lock and suspends the thread
					Monitor.Wait( requestMutex );
				}
			}
			// When we get back here, it's because we've been notified 
			// and thus the thread has been woken up. Lock has also been
			// re-acquired, but we won't use it. It's safe to try processing and fail
			// if another thread has got in first and grabbed the request
#endif
		}

		/// <summary>
		/// Main function for each thread spawned.
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		internal override void ThreadMain()
		{
			// default worker thread
#if AXIOM_THREAD_SUPPORT
			LogManager.Instance.Write( "DefaultWorkQueue('{0}').ThreadMain - thread {1} starting.",
				this.Name,
				GetThreadName()
				);

			// Initialise the thread for RS if necessary
			if ( workerRenderSystemAccess )
			{
				Root.Instance.RenderSystem.RegisterThread();
				NotifyThreadRegistered();
			}

			// Spin forever until we're told to shut down
			while ( !this.IsShuttingDown )
			{
				WaitForNextRequest();
				ProcessNextRequest();
			}

			LogManager.Instance.Write( "DefaultWorkQueue('{0}').ThreadMain - thread {1} stopped.",
				this.Name,
				GetThreadName()
				);
#endif
		}
	};
}