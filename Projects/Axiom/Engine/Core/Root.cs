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
//     <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;
#if SILVERLIGHT
using System.Threading;
using System.Windows.Controls;
#endif
using Axiom.Animating;
using Axiom.Collections;
using Axiom.Controllers;
using Axiom.FileSystem;
using Axiom.Fonts;
using Axiom.Graphics;
using Axiom.Media;
using Axiom.Overlays;
using Axiom.ParticleSystems;
using Axiom.Scripting.Compiler;
using Axiom.Graphics.Collections;
using System.IO;

#endregion Namespace Declarations

namespace Axiom.Core
{
	/// <summary>
	///		The Engine class is the main container of all the subsystems.  This includes the RenderSystem, various ResourceManagers, etc.
	/// </summary>
	public sealed class Root : IDisposable
	{
		#region Singleton implementation

		/// <summary>
		///     Singleton instance of Root.
		/// </summary>
		private static Root instance;

#if SILVERLIGHT
		[ThreadStatic] public static bool inDrawCallback;

		public static bool InDrawCallback
		{
			get { return inDrawCallback; }
		}
#endif

		/// <summary>
		///     Constructor.
		/// </summary>
		/// <remarks>
		///     This public contructor is intended for the user to decide when the Root object gets instantiated.
		///     This is a critical step in preparing the engine for use.
		/// </remarks>
		public Root()
			: this( "axiom.log" )
		{
		}

		/// <summary>
		///     Constructor.
		/// </summary>
		/// <remarks>
		///     This public contructor is intended for the user to decide when the Root object gets instantiated.
		///     This is a critical step in preparing the engine for use.
		/// </remarks>
		/// <param name="logFilename">Name of the default log file.</param>
		public Root( string logFilename )
		{
			if ( instance == null )
			{
				instance = this;

				var info = new StringBuilder();

				// write the initial info at the top of the log
				info.AppendFormat( "*********Axiom 3D Engine Log *************\n" );
				info.AppendFormat( "Copyright {0}\n", this.Copyright );
				info.AppendFormat( "Version: {0}\n", this.Version );
				info.AppendFormat( "Operating System: {0}\n", Environment.OSVersion.ToString() );
				var isMono = Type.GetType("Mono.Runtime") != null;
				info.AppendFormat( "{1} Framework: {0}\n", Environment.Version.ToString(), isMono ? "Mono": ".Net" );

				// Initializes the Log Manager singleton
				if ( LogManager.Instance == null )
					new LogManager();

				this.logMgr = LogManager.Instance;

				//if logFileName is null, then just the Diagnostics (debug) writes will be made
				// create a new default log
				this.logMgr.CreateLog( logFilename, true, true );

				this.logMgr.Write( info.ToString() );
				this.logMgr.Write( "*-*-* Axiom Initializing" );

				ArchiveManager.Instance.Initialize();
				ArchiveManager.Instance.AddArchiveFactory( new ZipArchiveFactory() );
				ArchiveManager.Instance.AddArchiveFactory( new FileSystemArchiveFactory() );
				ArchiveManager.Instance.AddArchiveFactory( new IsolatedStorageArchiveFactory() );
				ArchiveManager.Instance.AddArchiveFactory( new EmbeddedArchiveFactory() );
#if WINDOWS_PHONE
				ArchiveManager.Instance.AddArchiveFactory( new TitleContainerArchiveFactory() );
#endif
#if !(XBOX || XBOX360 )
				ArchiveManager.Instance.AddArchiveFactory( new WebArchiveFactory() );
#endif
#if SILVERLIGHT
				ArchiveManager.Instance.AddArchiveFactory( new XapArchiveFactory() );
#endif

				new ResourceGroupManager();
				new CodecManager();
				new HighLevelGpuProgramManager();

				ResourceGroupManager.Instance.Initialize();

                // WorkQueue (note: users can replace this if they want)
                DefaultWorkQueue defaultQ = new DefaultWorkQueue( "Root" );
                // never process responses in main thread for longer than 10ms by default
                defaultQ.ResponseProcessingTimeLimit = 10;

#if AXIOM_THREAD_SUPPORT

#if !WINDOWS_PHONE
                defaultQ.WorkerThreadCount = Environment.ProcessorCount;
#endif
                // only allow workers to access rendersystem if threadsupport is 1
                if ( Axiom.Configuration.Config.AxiomThreadLevel == 1 )
                    defaultQ.WorkersCanAccessRenderSystem = true;
                else
                    defaultQ.WorkersCanAccessRenderSystem = false;
#endif
                _workQueue = defaultQ;

                var resBack = new ResourceBackgroundQueue();

				this.sceneManagerEnumerator = SceneManagerEnumerator.Instance;

				var mat = MaterialManager.Instance;
				var mesh = MeshManager.Instance;
				SkeletonManager.Instance.Initialize();
				new ParticleSystemManager();
#if !(XNA || ANDROID || IPHONE || WINDOWS_PHONE )
				new PlatformManager();
#endif

				// create a new timer
				this.timer = new Timer();

				FontManager.Instance.Initialize();

				OverlayManager.Instance.Initialize();
				new OverlayElementManager();

				CompositorManager.Instance.Initialize();

				LodStrategyManager.Instance.Initialize();

				ScriptCompilerManager.Instance.Initialize();

				new PluginManager();
				PluginManager.Instance.LoadAll();

				// instantiate and register base movable factories
				this.entityFactory = new EntityFactory();
				this.AddMovableObjectFactory( this.entityFactory, true );
				this.lightFactory = new LightFactory();
				this.AddMovableObjectFactory( this.lightFactory, true );
				this.billboardSetFactory = new BillboardSetFactory();
				this.AddMovableObjectFactory( this.billboardSetFactory, true );
				this.manualObjectFactory = new ManualObjectFactory();
				this.AddMovableObjectFactory( this.manualObjectFactory, true );
				this.billboardChainFactory = new BillboardChainFactory();
				this.AddMovableObjectFactory( this.billboardChainFactory, true );
				this.ribbonTrailFactory = new RibbonTrailFactory();
				this.AddMovableObjectFactory( this.ribbonTrailFactory, true );
				this.movableTextFactory = new MovableTextFactory();
				this.AddMovableObjectFactory( this.movableTextFactory, true );
			}
		}

		/// <summary>
		///     Gets the singleton instance of this class.
		/// </summary>
		/// <value></value>
		public static Root Instance
		{
			get
			{
				return instance;
			}
		}

		#endregion Singleton implementation

		#region Fields

		/// <summary>
		///     Current active render system.
		/// </summary>
		private RenderSystem activeRenderSystem;

		/// <summary>
		///     Auto created window (if one was created).
		/// </summary>
		private RenderWindow autoWindow;

		/// <summary>
		///     Gets the Auto created window (if one was created).
		/// </summary>
		public RenderWindow AutoWindow
		{
			get
			{
				return this.autoWindow;
			}
		}

		/// <summary>
		///     Average frames per second.
		/// </summary>
		private float averageFPS;

		/// <summary>
		///     Current frames per second.
		/// </summary>
		private float currentFPS;

		/// <summary>
		///		Global frame count since startup.
		/// </summary>
		private ulong currentFrameCount;

		/// <summary>
		///    In case multiple render windows are created, only once are the resources loaded.
		/// </summary>
		private bool firstTimePostWindowInit = true;

		/// <summary>
		///     Frames drawn counter for FPS calculations.
		/// </summary>
		private long frameCount;

		/// <summary>
		///     Highest recorded frames per second.
		/// </summary>
		private float highestFPS;

		/// <summary>
		///     The last time we calculated the framerate.
		/// </summary>
		private long lastCalculationTime;

		/// <summary>
		///     End time of last frame.
		/// </summary>
		private long lastEndTime;

		/// <summary>
		///     Start queued stage of last frame.
		/// </summary>
		private long lastQueuedTime;

		/// <summary>
		///     Start time of last frame.
		/// </summary>
		private long lastStartTime;

		/// <summary>
		/// Holds instance of LogManager
		/// </summary>
		private LogManager logMgr;

		/// <summary>
		///     Lowest recorded frames per second.
		/// </summary>
		private float lowestFPS = 9999;

		/// <summary>
		///		True if a request has been made to shutdown the rendering engine.
		/// </summary>
		private bool queuedEnd;

		/// <summary>
		///     List of available render systems.
		/// </summary>
		private RenderSystemCollection renderSystemList = new RenderSystemCollection();

		/// <summary>
		///     Current active scene manager.
		/// </summary>
		private SceneManager sceneManager;

		/// <summary>
		///     List of available scene managers.
		/// </summary>
		private SceneManagerEnumerator sceneManagerEnumerator;

		/// <summary>
		///     How often we determine the FPS average, in seconds
		/// </summary>
		private float secondsBetweenFPSAverages = 1f;

		/// <summary>
		///		True if a request has been made to suspend rendering, typically because the
		///	    form has been minimized
		/// </summary>
		private bool suspendRendering = false;

		/// <summary>
		///     Current active timer.
		/// </summary>
		private ITimer timer;


        private float frameSmoothingTime = 0.0f;
		#region MovableObjectFactory fields

		private readonly MovableObjectFactoryMap movableObjectFactoryMap = new MovableObjectFactoryMap();

		private EntityFactory entityFactory;
		private LightFactory lightFactory;
		private BillboardSetFactory billboardSetFactory;
		private BillboardChainFactory billboardChainFactory;
		private ManualObjectFactory manualObjectFactory;
		private uint nextMovableObjectTypeFlag;
		private RibbonTrailFactory ribbonTrailFactory;
		private MovableTextFactory movableTextFactory;

		#endregion MovableObjectFactory fields

        /// <summary>
        /// Are we initialised yet?
        /// </summary>
        private bool _isInitialized;

        private WorkQueue _workQueue;

		#endregion Fields

		#region Events

		// <summary>
		//    The time when the meter manager was started
		// </summary>
		//private long lastFrameStartTime = 0;

		/// <summary>
		///    The number of microseconds per frame when we're
		///    limiting frame rates.  By default, we don't limit frame
		///    rates, and in that case, the number is 0.
		/// </summary>
		private float microsecondsPerFrame = 0;

		/// <summary>
		///    The number of microseconds per tick; obviously a fraction
		/// </summary>
		private float microsecondsPerTick;

		private readonly ChainedEvent<FrameEventArgs> _frameStartedEvent = new ChainedEvent<FrameEventArgs>();
		/// <summary>
		/// Fired as a frame is about to be rendered.
		/// </summary>
		public event EventHandler<FrameEventArgs> FrameStarted
		{
			add
			{
				_frameStartedEvent.EventSinks += value;
			}
			remove
			{
				_frameStartedEvent.EventSinks -= value;
			}
		}

		private readonly ChainedEvent<FrameEventArgs> _frameEndedEvent = new ChainedEvent<FrameEventArgs>();
		/// <summary>
		/// Fired after a frame has completed rendering.
		/// </summary>
		public event EventHandler<FrameEventArgs> FrameEnded
		{
			add
			{
				_frameEndedEvent.EventSinks += value;
			}
			remove
			{
				_frameEndedEvent.EventSinks -= value;
			}
		}

		private readonly ChainedEvent<FrameEventArgs> _frameRenderingQueuedEvent = new ChainedEvent<FrameEventArgs>();
		/// <summary>
		/// Fired after a frame has completed rendering.
		/// </summary>
		public event EventHandler<FrameEventArgs> FrameRenderingQueued
		{
			add
			{
				_frameRenderingQueuedEvent.EventSinks += value;
			}
			remove
			{
				_frameRenderingQueuedEvent.EventSinks -= value;
			}
		}

		#endregion Events

		#region Properties

		/// <summary>
		/// Specifies the name of the engine that will be used where needed (i.e. log files, etc).
		/// </summary>
		public string Copyright
		{
			get
			{
				var attribute =
						(AssemblyCopyrightAttribute)
						Attribute.GetCustomAttribute( Assembly.GetExecutingAssembly(),
													  typeof( AssemblyCopyrightAttribute ),
													  false );

				if ( attribute != null )
				{
					return attribute.Copyright;
				}
				else
				{
					return "";
				}
			}
		}

		/// <summary>
		/// Returns the current version of the Engine assembly.
		/// </summary>
		public string Version
		{
			get
			{
				// returns the file version of this assembly
#if SILVERLIGHT || WINDOWS_PHONE
				var fullName = Assembly.GetExecutingAssembly().ToString();
				var a = fullName.IndexOf( "Version=" ) + 8;
				var b = fullName.IndexOf( ",", a );
				return fullName.Substring( a, b - a );
#else
				return Assembly.GetExecutingAssembly().GetName().Version.ToString();
#endif
			}
		}

		/// <summary>
		///		Gets the scene manager currently being used to render a frame.
		/// </summary>
		/// <remarks>
		///		This is only intended for internal use; it is only valid during the
		///		rendering of a frame.
		///</remarks>
		public SceneManager SceneManager
		{
			get
			{
				return this.sceneManager;
			}
			set
			{
				this.sceneManager = value;
			}
		}

		/// <summary>
		///		Gets a list over all the existing SceneManager instances.
		/// </summary>
		public SceneManagerCollection SceneManagerList
		{
			get
			{
				return this.sceneManagerEnumerator.SceneManagerList;
			}
		}

		/// <summary>
		///		Gets a list of all types of SceneManager available for construction,
		///		providing some information about each one.
		/// </summary>
		public List<SceneManagerMetaData> MetaDataList
		{
			get
			{
				return this.sceneManagerEnumerator.MetaDataList;
			}
		}

		/// <summary>
		/// Gets/Sets the current active RenderSystem that the engine is using.
		/// </summary>
		public RenderSystem RenderSystem
		{
			get
			{
				return this.activeRenderSystem;
			}
			set
			{
				// Sets the active rendering system
				// Can be called direct or will be called by
				// standard config dialog

				// Is there already an active renderer?
				// If so, disable it and initialize the new one
				if ( this.activeRenderSystem != null && this.activeRenderSystem != value )
				{
					this.activeRenderSystem.Shutdown();
				}

				this.activeRenderSystem = value;

				// Tell scene managers
				SceneManagerEnumerator.Instance.RenderSytem = this.activeRenderSystem;
			}
		}

		/// <summary>
		/// The list of available render systems for the engine to use (made available via plugins).
		/// </summary>
		public RenderSystemCollection RenderSystems
		{
			get
			{
				return this.renderSystemList;
			}
		}

		/// <summary>
		///    Gets a reference to the timer being used for timing throughout the engine.
		/// </summary>
		public ITimer Timer
		{
			get
			{
				return this.timer;
			}
		}

		/// <summary>
		///    Gets or sets the maximum frame rate, in frames per second
		/// </summary>
		public int MaxFramesPerSecond
		{
			get
			{
				return
						(int)
						( ( this.microsecondsPerFrame == 0 )
								  ? this.microsecondsPerFrame
								  : ( 1000000.0f / this.microsecondsPerFrame ) );
			}
			set
			{
				if ( value != 0 )
				{
					this.microsecondsPerTick = 1000000.0f / (float)Stopwatch.Frequency;
					this.microsecondsPerFrame = 1000000.0f / (float)value;
				}
				else // Disable MaxFPS
				{
					this.microsecondsPerFrame = 0;
				}
			}
		}

		/// <summary>
		///    Access to the float that determines how often we compute the FPS average
		/// </summary>
		public float SecondsBetweenFPSAverages
		{
			get
			{
				return this.secondsBetweenFPSAverages;
			}
			set
			{
				this.secondsBetweenFPSAverages = value;
			}
		}

        /// <summary>
        ///		Gets the number of frames drawn since startup.
        /// </summary>
        public ulong CurrentFrameCount
        {
            get
            {
                return this.currentFrameCount;
            }
        }

        /// <summary>
        ///		Exposes FPS stats to anyone who cares.
        /// </summary>
        public float CurrentFPS
        {
            get
            {
                return this.currentFPS;
            }
        }

        /// <summary>
        ///		Exposes FPS stats to anyone who cares.
        /// </summary>
        public float BestFPS
        {
            get
            {
                return this.highestFPS;
            }
        }

        /// <summary>
        ///		Exposes FPS stats to anyone who cares.
        /// </summary>
        public float WorstFPS
        {
            get
            {
                return this.lowestFPS;
            }
        }

        /// <summary>
        ///		Exposes FPS stats to anyone who cares.
        /// </summary>
        public float AverageFPS
        {
            get
            {
                return this.averageFPS;
            }
        }
        /// <summary>
        /// Axiom by default gives you the raw frame time, but can 
        /// optionally smooth it out over several frames, in order to reduce the
        /// noticable effect of occasional hiccups in framerate.
        /// These smoothed values are passed back as parameters to FrameListener calls.
        /// </summary>
        /// <remarks>This method allows you to tweak the smoothing period, and is expressed
        /// in seconds. Setting it to 0 will result in completely unsmoothed frame times (the default)</remarks>
        public float FrameSmoothingTime
        {
            get { return frameSmoothingTime; }
            set { frameSmoothingTime = value; }
        }

        /// <summary>
        ///	    Exposes the mechanism to suspend rendering
        /// </summary>
        public bool SuspendRendering
        {
            get
            {
                return this.suspendRendering;
            }
            set
            {
                this.suspendRendering = value;
            }
        }

        /// <summary>
        /// Get/Set the WorkQueue for processing background tasks.
        /// You are free to add new requests and handlers to this queue to
        /// process your custom background tasks using the shared thread pool. 
        /// However, you must remember to assign yourself a new channel through 
        /// which to process your tasks.
        /// </summary>
        /// <remarks>
        /// Root will delete this work queue
        /// at shutdown, so do not destroy it yourself.
        /// </remarks>
        public WorkQueue WorkQueue
        {
            [OgreVersion( 1, 7, 2 )]
            get
            {
                return _workQueue;
            }

            [OgreVersion( 1, 7, 2 )]
            set
            {
                if ( _workQueue != value )
                {
                    // delete old one (will shut down)
                    _workQueue.Dispose();
                    _workQueue = value;

                    if ( _isInitialized )
                        _workQueue.Startup();
                }
            }
        }

        /// <summary>
        /// Returns whether the system is initialised or not.
        /// </summary>
        public bool IsInitialized
        {
            [OgreVersion( 1, 7, 2 )]
            get
            {
                return _isInitialized;
            }
        }

		#endregion Properties

		/// <summary>
		///		Registers a new SceneManagerFactory, a factory object for creating instances
		///		of specific SceneManagers.
		/// </summary>
		/// <remarks>
		///		Plugins should call this to register as new SceneManager providers.
		/// </remarks>
		/// <param name="factory"></param>
		public void AddSceneManagerFactory( SceneManagerFactory factory )
		{
			this.sceneManagerEnumerator.AddFactory( factory );
		}

		/// <summary>
		///		Unregisters a SceneManagerFactory.
		/// </summary>
		/// <param name="factory"></param>
		public void RemoveSceneManagerFactory( SceneManagerFactory factory )
		{
			this.sceneManagerEnumerator.RemoveFactory( factory );
		}

		/// <summary>
		///		Gets more information about a given type of SceneManager.
		/// </summary>
		/// <remarks>
		///		The metadata returned tells you a few things about a given type
		///		of SceneManager, which can be created using a factory that has been
		///		registered already.
		/// </remarks>
		/// <param name="typeName">
		///		The type name of the SceneManager you want to enquire on.
		/// 	If you don't know the typeName already, you can iterate over the
		///		metadata for all types using getMetaDataIterator.
		/// </param>
		public SceneManagerMetaData GetSceneManagerMetaData( string typeName )
		{
			return this.sceneManagerEnumerator.GetMetaData( typeName );
		}

		/// <summary>
		///		Creates a <see cref="SceneManager"/> instance of a given type.
		/// </summary>
		/// <remarks>
		///		You can use this method to create a SceneManager instance of a
		///		given specific type. You may know this type already, or you may
		///		have discovered it by looking at the results from <see cref="Root.GetSceneManagerMetaData"/>.
		/// </remarks>
		/// <param name="typeName">String identifying a unique SceneManager type.</param>
		/// <returns></returns>
		public SceneManager CreateSceneManager( string typeName )
		{
			var instanceName = ( new NameGenerator<SceneManager>() ).GetNextUniqueName( typeName.ToString() );
			return this.sceneManagerEnumerator.CreateSceneManager( typeName, instanceName );
		}

		/// <summary>
		///		Creates a <see cref="SceneManager"/> instance of a given type.
		/// </summary>
		/// <remarks>
		///		You can use this method to create a SceneManager instance of a
		///		given specific type. You may know this type already, or you may
		///		have discovered it by looking at the results from <see cref="Root.GetSceneManagerMetaData"/>.
		/// </remarks>
		/// <param name="typeName">String identifying a unique SceneManager type.</param>
		/// <param name="instanceName">
		///		Optional name to given the new instance that is created.
		///		If you leave this blank, an auto name will be assigned.
		/// </param>
		/// <returns></returns>
		public SceneManager CreateSceneManager( string typeName, string instanceName )
		{
			if ( String.IsNullOrEmpty( instanceName ) )
			{
				return CreateSceneManager( typeName );
			}
			return this.sceneManagerEnumerator.CreateSceneManager( typeName, instanceName );
		}

		/// <summary>
		///		Creates a <see cref="SceneManager"/> instance based on scene type support.
		/// </summary>
		/// <remarks>
		///		Creates an instance of a <see cref="SceneManager"/> which supports the scene types
		///		identified in the parameter. If more than one type of SceneManager
		///		has been registered as handling that combination of scene types,
		///		in instance of the last one registered is returned.
		/// </remarks>
		/// <param name="sceneType"> A mask containing one or more <see cref="SceneType"/> flags.</param>
		/// <returns></returns>
		public SceneManager CreateSceneManager( SceneType sceneType )
		{
			var instanceName = ( new NameGenerator<SceneManager>() ).GetNextUniqueName( sceneType.ToString() );
			return this.sceneManagerEnumerator.CreateSceneManager( sceneType, instanceName );
		}

		/// <summary>
		///		Creates a <see cref="SceneManager"/> instance based on scene type support.
		/// </summary>
		/// <remarks>
		///		Creates an instance of a <see cref="SceneManager"/> which supports the scene types
		///		identified in the parameter. If more than one type of SceneManager
		///		has been registered as handling that combination of scene types,
		///		in instance of the last one registered is returned.
		/// </remarks>
		/// <param name="sceneType"> A mask containing one or more <see cref="SceneType"/> flags.</param>
		/// <param name="instanceName">
		///		Optional name to given the new instance that is
		///		created. If you leave this blank, an auto name will be assigned.
		/// </param>
		/// <returns></returns>
		public SceneManager CreateSceneManager( SceneType sceneType, string instanceName )
		{
			if ( String.IsNullOrEmpty( instanceName ) )
			{
				return CreateSceneManager( sceneType );
			}
			return this.sceneManagerEnumerator.CreateSceneManager( sceneType, instanceName );
		}

		/// <summary>
		///		Destroys an instance of a SceneManager.
		/// </summary>
		/// <param name="instance"></param>
		public void DestroySceneManager( SceneManager instance )
		{
			this.sceneManagerEnumerator.DestroySceneManager( instance );
		}

		/// <summary>
		///		Gets an existing SceneManager instance that has already been created,
		///		identified by the instance name.
		/// </summary>
		/// <param name="instanceName">The name of the instance to retrieve.</param>
		/// <returns></returns>
		public SceneManager GetSceneManager( string instanceName )
		{
			return this.sceneManagerEnumerator.GetSceneManager( instanceName );
		}

        /// <summary>
        /// Determines if a given SceneManager already exists
        /// </summary>
        /// <param name="instanceName">The name of the instance to retrieve.</param>
        [OgreVersion( 1, 7, 2 )]
        public bool HasSceneManager( string instanceName )
        {
            return sceneManagerEnumerator.HasSceneManager( instanceName );
        }

		/// <summary>
		///    Initializes the renderer.
		/// </summary>
		/// <remarks>
		///     This method can only be called after a renderer has been
		///     selected with <see cref="Root.RenderSystem"/>, and it will initialize
		///     the selected rendering system ready for use.
		/// </remarks>
		/// <param name="autoCreateWindow">
		///     If true, a rendering window will automatically be created. The window will be
		///     created based on the options currently set on the render system.
		/// </param>
		/// <returns>A reference to the automatically created window (if requested), or null otherwise.</returns>
		public RenderWindow Initialize( bool autoCreateWindow )
		{
			return this.Initialize( autoCreateWindow, "Axiom Render Window" );
		}

		/// <summary>
		///    Initializes the renderer.
		/// </summary>
		/// <remarks>
		///     This method can only be called after a renderer has been
		///     selected with <see cref="Root.RenderSystem"/>, and it will initialize
		///     the selected rendering system ready for use.
		/// </remarks>
		/// <param name="autoCreateWindow">
		///     If true, a rendering window will automatically be created The window will be
		///     created based on the options currently set on the render system.
		/// </param>
		/// <param name="windowTitle">Title to use by the window.</param>
		/// <returns>A reference to the automatically created window (if requested), or null otherwise.</returns>
		public RenderWindow Initialize( bool autoCreateWindow, string windowTitle )
		{
			if ( this.activeRenderSystem == null )
			{
				throw new AxiomException( "Cannot initialize - no render system has been selected." );
			}

			new ControllerManager();

#if !(XBOX || XBOX360)
			PlatformInformation.Log( LogManager.Instance.DefaultLog );
#endif
			// initialize the current render system
			this.autoWindow = this.activeRenderSystem.Initialize( autoCreateWindow, windowTitle );

			// if they chose to auto create a window, also initialize several subsystems
			if ( autoCreateWindow )
			{
				this.OneTimePostWindowInit();
			}

			// initialize timer
			this.timer.Reset();

#if SILVERLIGHT && !WINDOWS_PHONE
			ThreadUI.Invoke(delegate
								{
									var drawingSurface = (DrawingSurface) autoWindow["DRAWINGSURFACE"];
									drawingSurface.Draw += (s, args) =>
															{
																try
																{
																	inDrawCallback = true;
																	if (!RenderOneFrame())
																		queuedEnd = true;
																}
																catch (Exception ex)
																{
																	if (LogManager.Instance != null)
																		LogManager.Instance.Write(LogManager.BuildExceptionString(ex));
																}
																finally
																{
																	inDrawCallback = false;
																}
															};
								});
#endif
            _isInitialized = true;
			return this.autoWindow;
		}

		/// <summary>
		///    Internal method for one-time tasks after first window creation.
		/// </summary>
		private void OneTimePostWindowInit()
		{
			if ( this.firstTimePostWindowInit )
			{
                // Background loader
                ResourceBackgroundQueue.Instance.Initialize();
                _workQueue.Startup();

				// init material manager singleton, which parse sources for materials
				if ( MaterialManager.Instance == null )
					new MaterialManager();

				MaterialManager.Instance.Initialize();

				// init the particle system manager singleton
				ParticleSystemManager.Instance.Initialize();

				// init mesh manager
				MeshManager.Instance.Initialize();

				this.firstTimePostWindowInit = false;
			}
		}

		/// <summary>
		///		Overloaded method.
		/// </summary>
		/// <returns></returns>
		public RenderWindow CreateRenderWindow( string name, int width, int height, bool isFullScreen )
		{
			return this.CreateRenderWindow( name, width, height, isFullScreen, null );
		}

		/// <summary>
		///		A collection of addition render system specific options.
		///	</summary>
		public RenderWindow CreateRenderWindow( string name,
												int width,
												int height,
												bool isFullscreen,
												NamedParameterList miscParams )
		{
			Debug.Assert( this.activeRenderSystem != null,
						  "Cannot create a RenderWindow without an active RenderSystem." );

			// create a new render window via the current render system
			var window = this.activeRenderSystem.CreateRenderWindow( name,
																			  width,
																			  height,
																			  isFullscreen,
																			  miscParams );

			// do any required initialization
			if ( this.firstTimePostWindowInit )
			{
				this.OneTimePostWindowInit();
				// window.Primary = true;
			}

			return window;
		}

		/// <summary>
		///		Asks the current API to convert an instance of ColorEx to a 4 byte packed
		///		int value the way it would expect it.
		/// </summary>
		/// <param name="color"></param>
		/// <returns></returns>
		public int ConvertColor( ColorEx color )
		{
			Debug.Assert( this.activeRenderSystem != null, "Cannot covert color value without an active renderer." );

			return this.activeRenderSystem.ConvertColor( color );
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="target"></param>
		public void DetachRenderTarget( RenderTarget target )
		{
			if ( this.activeRenderSystem == null )
			{
				throw new AxiomException( "Cannot detach render target - no render system has been selected." );
			}

			this.activeRenderSystem.DetachRenderTarget( target );
		}

		private long CaptureCurrentTime()
		{
			return Stopwatch.GetTimestamp();
		}

		/// <summary>
		///		Renders one frame.
		/// </summary>
		/// <remarks>
		///		Updates all the render targets automatically and then returns, raising frame events before and after.
		/// </remarks>
		/// <returns>True if execution should continue, false if a quit was requested.</returns>
		public bool RenderOneFrame()
		{
			// Stop rendering if frame callback says so
			if ( !this.OnFrameStarted() )
			{
				return false;
			}

			// update all current render targets
			if ( !this.UpdateAllRenderTargets() )
			{
				return false;
			}

			// Stop rendering if frame callback says so
			return this.OnFrameEnded();
		}

		/// <summary>
		///		Starts the default rendering loop.
		/// </summary>
		public void StartRendering()
		{
			Debug.Assert( this.activeRenderSystem != null, "Engine cannot start rendering without an active RenderSystem." );

			this.activeRenderSystem.InitRenderTargets();

			// initialize the vars
			this.lastStartTime = this.lastQueuedTime = this.lastEndTime = this.timer.Milliseconds;

			// reset to false so that rendering can begin
			this.queuedEnd = false;

#if SILVERLIGHT && !WINDOWS_PHONE
			ThreadUI.Invoke(delegate
								{
									var drawingSurface = (DrawingSurface) autoWindow["DRAWINGSURFACE"];
									drawingSurface.Draw += (s, args) =>
															{
																if (!queuedEnd)
																	args.InvalidateSurface();
															};
									drawingSurface.Invalidate();
								});
#else
			while ( !this.queuedEnd )
			{
				// allow OS events to process (if the platform requires it
				if ( WindowEventMonitor.Instance.MessagePump != null )
				{
					WindowEventMonitor.Instance.MessagePump();
				}

				if ( !this.RenderOneFrame() )
				{
					break;
				}
			}
#endif
		}

		/// <summary>
		///		Shuts down the engine and unloads plugins.
		/// </summary>
		public void Shutdown()
		{
            LogManager.Instance.Write( "*-*-* Axiom Shutdown Initiated." );
			SceneManagerEnumerator.Instance.ShutdownAll();

			// destroy all auto created GPU programs
			ShadowVolumeExtrudeProgram.Shutdown();
            ResourceGroupManager.Instance.ShutdownAll();

			// ResourceBackGroundPool.Instance.Shutdown();
            _isInitialized = false;
		}

		/// <summary>
		///		Requests that the rendering engine shutdown at the beginning of the next frame.
		/// </summary>
		public void QueueEndRendering()
		{
			this.queuedEnd = true;
		}

		/// <summary>
		///     Internal method used for updating all <see cref="RenderTarget"/> objects (windows,
		///     renderable textures etc) which are set to auto-update.
		/// </summary>
		/// <remarks>
		///     You don't need to use this method if you're using Axiom's own internal
		///     rendering loop (<see cref="Root.StartRendering"/>). If you're running your own loop
		///     you may wish to call it to update all the render targets which are
		///     set to auto update (<see cref="RenderTarget.IsAutoUpdated"/>). You can also update
		///     individual <see cref="RenderTarget"/> instances using their own Update() method.
		/// </remarks>
		public bool UpdateAllRenderTargets()
		{
			// update all targets but don't swap buffers
			this.activeRenderSystem.UpdateAllRenderTargets( false );
			// give client app opportunity to use queued GPU time
			var ret = OnFrameRenderingQueued();
			// block for final swap
			this.activeRenderSystem.SwapAllRenderTargetBuffers( this.activeRenderSystem.WaitForVerticalBlank );

			return ret;
		}

		#region Implementation of IDisposable

		/// <summary>
		///		Called to shutdown the engine and dispose of all it's resources.
		/// </summary>
		public void Dispose()
		{
			// force the engine to shutdown
			this.Shutdown();

			if ( CompositorManager.Instance != null )
			{
				CompositorManager.Instance.Dispose();
			}

			if ( OverlayManager.Instance != null )
			{
				OverlayManager.Instance.Dispose();
			}

			if ( OverlayElementManager.Instance != null )
			{
				OverlayElementManager.Instance.Dispose();
			}

			if ( FontManager.Instance != null )
			{
				FontManager.Instance.Dispose();
			}

			if ( ArchiveManager.Instance != null )
			{
				ArchiveManager.Instance.Dispose();
			}

			if ( SkeletonManager.Instance != null )
			{
				SkeletonManager.Instance.Dispose();
			}

			if ( MeshManager.Instance != null )
			{
				MeshManager.Instance.Dispose();
			}

			if ( MaterialManager.Instance != null )
			{
				MaterialManager.Instance.Dispose();
			}

			if ( ParticleSystemManager.Instance != null )
			{
				ParticleSystemManager.Instance.Dispose();
			}

			if ( ControllerManager.Instance != null )
			{
				ControllerManager.Instance.Dispose();
			}

			if ( HighLevelGpuProgramManager.Instance != null )
			{
				HighLevelGpuProgramManager.Instance.Dispose();
			}

			if ( PluginManager.Instance != null )
			{
				PluginManager.Instance.Dispose();
			}

			Pass.ProcessPendingUpdates();

			if ( ResourceGroupManager.Instance != null )
			{
				ResourceGroupManager.Instance.Dispose();
			}

            // Note: The dispose method implementation of both ResourceBackgroundQueue and
            // DefaultWorkQueue internally calls Shutdown, so the direct call to Shutdown methods
            // isn't necessary in Root.Shutdown.
            ResourceBackgroundQueue.Instance.Dispose();
            _workQueue.Dispose();
            _workQueue = null;

			if ( CodecManager.Instance != null )
			{
				if ( !CodecManager.Instance.IsDisposed )
					CodecManager.Instance.Dispose();
			}

#if !XBOX360
			if ( PlatformManager.Instance != null )
			{
				PlatformManager.Instance.Dispose();
			}
#endif

			this.activeRenderSystem = null;

			if ( WindowEventMonitor.Instance != null )
			{
				WindowEventMonitor.Instance.Dispose();
			}

			if ( ObjectManager.Instance != null )
			{
				ObjectManager.Instance.Dispose();
			}

			if ( LogManager.Instance != null )
			{
				LogManager.Instance.Dispose();
			}

			instance = null;
		}

		#endregion Implementation of IDisposable

		#region Internal Engine Methods

		/// <summary>
		///    Internal method for calculating the average time between recently fired events.
		/// </summary>
		/// <param name="time">The current time in milliseconds.</param>
		/// <param name="type">The type event to calculate.</param>
		/// <returns>Average time since last event of the same type.</returns>
		private float CalculateEventTime( long time, FrameEventType type )
		{
            //calculate the average time passed between events of the given type
            //during the last frameSmoothingTime seconds
			float result = 0;
            float discardThreshold = frameSmoothingTime * 1000.0f;
         


            if (time > discardThreshold)
                time -= (long)(frameSmoothingTime * discardThreshold);
			if ( type == FrameEventType.Start )
			{
				result = (float)( time - this.lastStartTime ) / 1000;

               
                
				// update the last start time before the render targets are rendered
				this.lastStartTime = time;
			}
			else if ( type == FrameEventType.Queued )
			{
				result = (float)( time - this.lastQueuedTime ) / 1000;
				// update the last queued time before the render targets are rendered
				this.lastQueuedTime = time;
			}
			else if ( type == FrameEventType.End )
			{
				// increment frameCount
				this.frameCount++;

				// collect performance stats
				if ( ( time - this.lastCalculationTime ) > this.secondsBetweenFPSAverages * 1000f )
				{
					// Is It Time To Update Our Calculations?
					// Calculate New Framerate
					this.currentFPS = (float)this.frameCount / (float)( time - this.lastCalculationTime ) * 1000f;

					// calculate the average framerate
					if ( this.averageFPS == 0 )
					{
						this.averageFPS = this.currentFPS;
					}
					else
					{
						this.averageFPS = ( this.averageFPS + this.currentFPS ) / 2.0f;
					}

					// Is The New Framerate A New Low?
					if ( this.currentFPS < this.lowestFPS || (int)this.lowestFPS == 0 )
					{
						// Set It To The New Low
						this.lowestFPS = this.currentFPS;
					}

					// Is The New Framerate A New High?
					if ( this.currentFPS > this.highestFPS )
					{
						// Set It To The New High
						this.highestFPS = this.currentFPS;
					}

					// Update Our Last Frame Time To Now
					this.lastCalculationTime = time;

					// Reset Our Frame Count
					this.frameCount = 0;
				}
                
				result = (float)( time - this.lastEndTime ) / 1000;

				this.lastEndTime = time;
			}

			return result;
		}

		FrameEventArgs frameEventArgs = new FrameEventArgs();
		/// <summary>
		///    Method for raising frame started events.
		/// </summary>
		/// <remarks>
		///    This method is only for internal use when you use the built-in rendering
		///    loop (Root.StartRendering). However, if you run your own rendering loop then
		///    you should call this method to ensure that FrameEvent handlers are notified
		///    of frame events; processes like texture animation and particle systems rely on
		///    this.
		///    <p/>
		///    This method calculates the frame timing information for you based on the elapsed
		///    time. If you want to specify elapsed times yourself you should call the other
		///    version of this method which takes event details as a parameter.
		/// </remarks>
		public bool OnFrameStarted()
		{
			//FrameEventArgs e = new FrameEventArgs();
			var now = this.timer.Milliseconds;
			frameEventArgs.TimeSinceLastFrame = this.CalculateEventTime( now, FrameEventType.Start );

			// if any event handler set this to true, that will signal the engine to shutdown
			return this.OnFrameStarted( frameEventArgs );
		}

		/// <summary>
		///    Method for raising frame rendering queued events.
		/// </summary>
		/// <remarks>
		///    This method is only for internal use when you use the built-in rendering
		///    loop (Root.StartRendering). However, if you run your own rendering loop then
		///    you you may want to call this method too, although nothing in Axiom relies on this
		///    particular event. Really if you're running your own rendering loop at
		///    this level of detail then you can get the same effect as doing your
		///    updates in a OnFrameRenderingQueued event by just calling
		///    <see name="RenderWindow.Update" /> with the 'swapBuffers' option set to false.
		/// </remarks>
		public bool OnFrameRenderingQueued()
		{
			//FrameEventArgs e = new FrameEventArgs();
			var now = this.timer.Milliseconds;
			frameEventArgs.TimeSinceLastFrame = this.CalculateEventTime( now, FrameEventType.Queued );

			// if any event handler set this to true, that will signal the engine to shutdown
			return this.OnFrameRenderingQueued( frameEventArgs );
		}

		/// <summary>
		///    Method for raising frame ended events.
		/// </summary>
		/// <remarks>
		///    This method is only for internal use when you use the built-in rendering
		///    loop (Root.StartRendering). However, if you run your own rendering loop then
		///    you should call this method to ensure that FrameEvent handlers are notified
		///    of frame events; processes like texture animation and particle systems rely on
		///    this.
		///    <p/>
		///    This method calculates the frame timing information for you based on the elapsed
		///    time. If you want to specify elapsed times yourself you should call the other
		///    version of this method which takes event details as a parameter.
		/// </remarks>
		public bool OnFrameEnded()
		{
			//FrameEventArgs e = new FrameEventArgs();
			var now = this.timer.Milliseconds;
			frameEventArgs.TimeSinceLastFrame = this.CalculateEventTime( now, FrameEventType.End );

			// if any event handler set this to true, that will signal the engine to shutdown
			return this.OnFrameEnded( frameEventArgs );
		}

		/// <summary>
		///    Method for raising frame started events.
		/// </summary>
		/// <remarks>
		///    This method is only for internal use when you use the built-in rendering
		///    loop (Root.StartRendering). However, if you run your own rendering loop then
		///    you should call this method to ensure that FrameEvent handlers are notified
		///    of frame events; processes like texture animation and particle systems rely on
		///    this.
		///    <p/>
		///    This method takes an event object as a parameter, so you can specify the times
		///    yourself. If you are happy for the engine to automatically calculate the frame time
		///    for you, then call the other version of this method with no parameters.
		/// </remarks>
		/// <param name="e">
		///    Event object which includes all the timing information which must already be
		///    calculated.  RequestShutdown should be checked after each call, because that means
		///    an event handler is requesting that shudown begin for one reason or another.
		/// </param>
		public bool OnFrameStarted( FrameEventArgs e )
		{
			// increment the current frame count
			this.currentFrameCount++;

			// call the event, which automatically fires all registered handlers
			this._frameStartedEvent.Fire( this, e, ( args ) => args.StopRendering != true );
			return !e.StopRendering;
		}

		/// <summary>
		///    Method for raising frame rendering queued events.
		/// </summary>
		/// <remarks>
		///    This method is only for internal use when you use the built-in rendering
		///    loop (Root.StartRendering). However, if you run your own rendering loop then
		///    you you may want to call this method too, although nothing in Axiom relies on this
		///    particular event. Really if you're running your own rendering loop at
		///    this level of detail then you can get the same effect as doing your
		///    updates in a OnFrameRenderingQueued event by just calling
		///    <see name="RenderWindow.Update" /> with the 'swapBuffers' option set to false.
		/// </remarks>
		public bool OnFrameRenderingQueued( FrameEventArgs e )
		{
			this._frameRenderingQueuedEvent.Fire( this, e, ( args ) => args.StopRendering != true );

			return !e.StopRendering;
		}

		/// <summary>
		///    Method for raising frame ended events.
		/// </summary>
		/// <remarks>
		///    This method is only for internal use when you use the built-in rendering
		///    loop (Root.StartRendering). However, if you run your own rendering loop then
		///    you should call this method to ensure that FrameEvent handlers are notified
		///    of frame events; processes like texture animation and particle systems rely on
		///    this.
		///    <p/>
		///    This method takes an event object as a parameter, so you can specify the times
		///    yourself. If you are happy for the engine to automatically calculate the frame time
		///    for you, then call the other version of this method with no parameters.
		/// </remarks>
		/// <param name="e">
		///    Event object which includes all the timing information which must already be
		///    calculated.  RequestShutdown should be checked after each call, because that means
		///    an event handler is requesting that shudown begin for one reason or another.
		/// </param>
		public bool OnFrameEnded( FrameEventArgs e )
		{
			this._frameEndedEvent.Fire( this, e, ( args ) => args.StopRendering != true );

			// Tell buffer manager to free temp buffers used this frame
			if ( HardwareBufferManager.Instance != null )
			{
				HardwareBufferManager.Instance.ReleaseBufferCopies( false );
			}

            // Tell the queue to process responses
            _workQueue.ProcessResponses();

			return !e.StopRendering;
		}

		#endregion Internal Engine Methods

		#region MovableObjectFactory methods

		/// <summary>
		///     Allocate and retrieve the next MovableObject type flag.
		/// </summary>
		/// <remarks>
		///     This is done automatically if MovableObjectFactory.RequestTypeFlags
		///	    returns true; don't call this manually unless you're sure you need to.
		/// </remarks>
		public uint NextMovableObjectTypeFlag()
		{
			if ( this.nextMovableObjectTypeFlag == (uint)SceneQueryTypeMask.UserLimit )
			{
				throw new AxiomException(
						"Cannot allocate a type flag since all the available flags have been used." );
			}

			var ret = this.nextMovableObjectTypeFlag;
			this.nextMovableObjectTypeFlag <<= 1;
			return ret;
		}

		/// <summary>
		///     Checks whether a factory is registered for a given MovableObject type
		/// </summary>
		/// <param name="typeName">
		///     The factory type to check for.
		/// </param>
		/// <returns>True if the factory type is registered.</returns>
		public bool HasMovableObjectFactory( string typeName )
		{
			return this.movableObjectFactoryMap.ContainsKey( typeName );
		}

		/// <summary>
		///     Get a MovableObjectFactory for the given type.
		/// </summary>
		/// <param name="typeName">
		///     The factory type to obtain.
		/// </param>
		/// <returns>
		///     A factory for the given type of MovableObject.
		/// </returns>
		public MovableObjectFactory GetMovableObjectFactory( string typeName )
		{
			if ( !this.movableObjectFactoryMap.ContainsKey( typeName ) )
			{
				throw new AxiomException( "MovableObjectFactory for type " + typeName + " does not exist." );
			}

			return this.movableObjectFactoryMap[ typeName ];
		}

		/// <summary>
		///     Removes a previously registered MovableObjectFactory.
		/// </summary>
		/// <remarks>
		///	    All instances of objects created by this factory will be destroyed
		///	    before removing the factory (by calling back the factories
		///	    'DestroyInstance' method). The plugin writer is responsible for actually
		///	    destroying the factory.
		/// </remarks>
		/// <param name="fact">The instance to remove.</param>
		public void RemoveMovableObjectFactory( MovableObjectFactory fact )
		{
			if ( this.movableObjectFactoryMap.ContainsValue( fact ) )
			{
				this.movableObjectFactoryMap.Remove( fact.Type );
			}
		}

		/// <summary>
		///     Register a new MovableObjectFactory which will create new MovableObject
		///	    instances of a particular type, as identified by the Type property.
		/// </summary>
		/// <remarks>
		///     Plugin creators can create subclasses of MovableObjectFactory which
		///	    construct custom subclasses of MovableObject for insertion in the
		///	    scene. This is the primary way that plugins can make custom objects
		///	    available.
		/// </remarks>
		/// <param name="fact">
		///     The factory instance.
		/// </param>
		/// <param name="overrideExisting">
		///     Set this to true to override any existing
		///	    factories which are registered for the same type. You should only
		///	    change this if you are very sure you know what you're doing.
		/// </param>
		public void AddMovableObjectFactory( MovableObjectFactory fact, bool overrideExisting )
		{
			if ( this.movableObjectFactoryMap.ContainsKey( fact.Type ) && !overrideExisting )
			{
				throw new AxiomException( "A factory of type '" + fact.Type + "' already exists." );
			}

			if ( fact.RequestTypeFlags )
			{
				if ( this.movableObjectFactoryMap.ContainsValue( fact ) )
				{
					// Copy type flags from the factory we're replacing
					fact.TypeFlag = ( this.movableObjectFactoryMap[ fact.Type ] ).TypeFlag;
				}
				else
				{
					// Allocate new
					fact.TypeFlag = this.NextMovableObjectTypeFlag();
				}
			}

			// Save
			if ( this.movableObjectFactoryMap.ContainsKey( fact.Type ) )
			{
				LogManager.Instance.Write( "Factory {0} has been replaced by {1}.",
					this.movableObjectFactoryMap[ fact.Type ].GetType().Name,
					fact.GetType().Name
					);

				this.movableObjectFactoryMap[ fact.Type ] = fact;
			}
			else
				this.movableObjectFactoryMap.Add( fact.Type, fact );

			LogManager.Instance.Write( "Factory " + fact.GetType().Name + " registered for MovableObjectType '" + fact.Type + "'." );
		}

		public MovableObjectFactoryMap MovableObjectFactories
		{
			get
			{
				return movableObjectFactoryMap;
			}
		}

		public int NextFrameNumber { get; private set; }

		#endregion MovableObjectFactory methods

        /// <summary>
        /// Helper method to assist you in creating writeable file streams.
        /// </summary>
        /// <remarks>
        /// This is a high-level utility method which you can use to find a place to 
        /// save a file more easily. If the filename you specify is either an
        /// absolute or relative filename (ie it includes path separators), then
        /// the file will be created in the normal filesystem using that specification.
        /// If it doesn't, then the method will look for a writeable resource location
        /// via ResourceGroupManager::createResource using the other params provided.
        /// </remarks>
        /// <param name="fileName">The name of the file to create. If it includes path separators, 
        /// the filesystem will be accessed direct. If no path separators are
        /// present the resource system is used, falling back on the raw filesystem after.</param>
        /// <param name="groupName">The name of the group in which to create the file, if the 
        /// resource system is used</param>
        /// <param name="overwrite">If true, an existing file will be overwritten, if false
        /// an error will occur if the file already exists</param>
        /// <param name="locationPattern">If the resource group contains multiple locations, 
        /// then usually the file will be created in the first writable location. If you 
        /// want to be more specific, you can include a location pattern here and 
        /// only locations which match that pattern (as determined by StringUtil::match)
        /// will be considered candidates for creation.</param>
        [OgreVersion( 1, 7, 2 )]
        public Stream CreateFileStream( string fileName, string groupName, bool overwrite, string locationPattern )
        {
            // Does this file include path specifiers?
            string path = Path.GetDirectoryName( fileName );
            string basename = Path.GetFileName( fileName );

            // no path elements, try the resource system first
            Stream stream = null;
            if ( string.IsNullOrEmpty( path ) )
            {
                try
                {
                    stream = ResourceGroupManager.Instance.CreateResource( fileName, groupName, overwrite, locationPattern );
                }
                catch
                { }
            }

            if ( stream == null )
            {
                // save direct in filesystem
                try
                {
#if !( SILVERLIGHT || WINDOWS_PHONE || XBOX || XBOX360 || ANDROID || IOS )
                    stream = File.Create( fileName, 1, FileOptions.RandomAccess );
#else
					stream = File.Create( fileName, 1 );
#endif
                }
                catch ( Exception ex )
                {
                    throw new AxiomException( "Can't open '{0}' for writing", ex, fileName );
                }
            }

            return stream;
        }

        public Stream CreateFileStream( string fileName )
        {
            return this.CreateFileStream( fileName, ResourceGroupManager.DefaultResourceGroupName, false, string.Empty );
        }

        public Stream CreateFileStream( string fileName, string groupName )
        {
            return this.CreateFileStream( fileName, groupName, false, string.Empty );
        }

        public Stream CreateFileStream( string fileName, string groupName, bool overwrite )
        {
            return this.CreateFileStream( fileName, groupName, overwrite, string.Empty );
        }

        /// <summary>
        /// Helper method to assist you in accessing readable file streams.
        /// </summary>
        /// <remarks>
        /// This is a high-level utility method which you can use to find a place to 
        /// open a file more easily. It checks the resource system first, and if
        /// that fails falls back on accessing the file system directly.
        /// </remarks>
        /// <param name="filename">The name of the file to open.</param>
        /// <param name="groupName">The name of the group in which to create the file, if the 
        /// resource system is used</param>
        /// <param name="locationPattern">
        /// If the resource group contains multiple locations, 
        /// then usually the file will be created in the first writable location. If you 
        /// want to be more specific, you can include a location pattern here and 
        /// only locations which match that pattern (as determined by StringUtil::match)
        /// will be considered candidates for creation.
        /// </param>
        [OgreVersion( 1, 7, 2 )]
        public Stream OpenFileStream( string filename, string groupName, string locationPattern )
        {
            Stream stream = null;
            if ( ResourceGroupManager.Instance.ResourceExists( groupName, filename ) )
            {
                stream = ResourceGroupManager.Instance.OpenResource( filename, groupName );
            }
            else
            {
                // try direct
                if ( !File.Exists( filename ) )
                    throw new AxiomException( "'{0}' file not found!", filename );

                try
                {
                    stream = File.Open( filename, FileMode.Open );
                }
                catch ( Exception ex )
                {
                    throw new AxiomException( "Can't open file '{0}' for reading", ex, filename );
                }
            }

            return stream;
        }

        public Stream OpenFileStream( string filename )
        {
            return this.OpenFileStream( filename, ResourceGroupManager.DefaultResourceGroupName, string.Empty );
        }

        public Stream OpenFileStream( string filename, string groupName )
        {
            return this.OpenFileStream( filename, groupName, string.Empty );
        }
	}

	#region Frame Events

	/// <summary>
	///		Used to supply info to the FrameStarted and FrameEnded events.
	/// </summary>
	public class FrameEventArgs : EventArgs
	{
		/// <summary>
		/// Request that the renderer stop the render loop
		/// </summary>
		public bool StopRendering;

		/// <summary>
		///    Time elapsed (in milliseconds) since the last frame event.
		/// </summary>
		public float TimeSinceLastEvent;

		/// <summary>
		///    Time elapsed (in milliseconds) since the last frame.
		/// </summary>
		public float TimeSinceLastFrame;
	}

	public enum FrameEventType
	{
		Start,
		Queued,
		End
	}

	#endregion Frame Events
}