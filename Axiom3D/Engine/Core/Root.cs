#region LGPL License
/*
Axiom Game Engine Library
Copyright (C) 2003  Axiom Project Team

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

#region Namespace Declarations

using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Text;

// This is coming from RealmForge.Utility
using Axiom.Core;
using System.Collections.Generic;

#endregion Namespace Declarations

#region Versioning Information
/// File								Revision
/// ===============================================
/// OgreRoot.h	        	            ?.??
/// OgreRoot.cpp		                ?.??
/// 
#endregion

namespace Axiom
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

        /// <summary>
        ///     Constructor.
        /// </summary>
        /// <remarks>
        ///     This public contructor is intended for the user to decide when the Root object gets instantiated.
        ///     This is a critical step in preparing the engine for use.
        /// </remarks>
        /// <param name="configFileName">Name of the config file to load.</param>
        /// <param name="logFileName">Name of the default log file.</param>
        public Root( string configFileName, string logFileName )
        {
            if ( instance == null )
            {
                instance = this;

                this.configFileName = configFileName;

                StringBuilder info = new StringBuilder();

                // write the initial info at the top of the log
                info.AppendFormat( "*********Axiom 3D Engine Log *************{0}", Environment.NewLine );
                info.AppendFormat( "Copyright {0}{1}", this.Copyright, Environment.NewLine );
                info.AppendFormat( "Version: {0}{1}", this.Version, Environment.NewLine );
                info.AppendFormat( "Operating System: {0}{1}", Environment.OSVersion.ToString(), Environment.NewLine );
                info.AppendFormat( ".Net Framework: {0}{1}", Environment.Version.ToString(), Environment.NewLine );

                // Initializes the Log Manager singleton
                LogManager logMgr = new LogManager();

                //if logFileName is null, then just the Diagnostics (debug) writes will be made
                // create a new default log
                logMgr.CreateLog( logFileName, true, true );

                logMgr.Write( info.ToString() );
                logMgr.Write( "*-*-* Axiom Intializing" );
                
                Vfs.Instance.Initialize();
                
                new PluginManager();
                new ArchiveManager();
                new ZipArchiveFactory();
                new FontManager();
                new OverlayManager();
                new OverlayElementManager();
                new MaterialManager();
                new MeshManager();
                new SkeletonManager();
                new HighLevelGpuProgramManager();
                new CodecManager();

                // register all build in codecs
                CodecManager.Instance.RegisterCodecs();

                sceneManagerList = SceneManagerEnumerator.Instance;

                new ParticleSystemManager();
                
                PlatformManager.LoadInstance();
                
                // init the rendersystem manager
                new RenderSystemManager();

                // create a new timer
                timer = PlatformManager.Instance.CreateTimer();

                // load the still unloaded but specified plugins
                // (third-party?)
                // PluginManager.Instance.LoadPlugins();
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

        #endregion

        #region Fields

        /// <summary>
        ///     Current active scene manager.
        /// </summary>
        private SceneManager sceneManager;
        /// <summary>
        ///     List of available scene managers.
        /// </summary>
        private SceneManagerEnumerator sceneManagerList;
        /// <summary>
        ///     List of available render systems.
        /// </summary>
        private RenderSystemCollection renderSystemList = new RenderSystemCollection();
        /// <summary>
        ///     Current active render system.
        /// </summary>
        private RenderSystem activeRenderSystem;
        /// <summary>
        ///     Current active timer.
        /// </summary>
        private ITimer timer;
        /// <summary>
        ///     Auto created window (if one was created).
        /// </summary>
        private RenderWindow autoWindow;
        /// <summary>
        ///     Name of the file containing configuration info.
        /// </summary>
        private string configFileName;

        /// <summary>
        ///     Start time of last frame.
        /// </summary>
        private long lastStartTime;
        /// <summary>
        ///     End time of last frame.
        /// </summary>
        private long lastEndTime;
        /// <summary>
        ///     The last time we calculated the framerate.
        /// </summary>
        private long lastCalculationTime;
        /// <summary>
        ///     Frames drawn counter for FPS calculations.
        /// </summary>
        private long frameCount;
        /// <summary>
        ///     Current frames per second.
        /// </summary>
        private float currentFPS;
        /// <summary>
        ///     Highest recorded frames per second.
        /// </summary>
        private float highestFPS;
        /// <summary>
        ///     Lowest recorded frames per second.
        /// </summary>
        private float lowestFPS = 9999;
        /// <summary>
        ///     Average frames per second.
        /// </summary>
        private float averageFPS;

        /// <summary>
        ///		Global frame count since startup.
        /// </summary>
        private ulong currentFrameCount;
        /// <summary>
        ///    In case multiple render windows are created, only once are the resources loaded.
        /// </summary>
        private bool firstTimePostWindowInit = true;
        /// <summary>
        ///		True if a request has been made to shutdown the rendering engine.
        /// </summary>
        private bool queuedEnd;

        #endregion Fields

        #region Events

        /// <summary>
        /// Fired as a frame is about to be rendered.
        /// </summary>
        public event FrameEvent FrameStarted;

        /// <summary>
        /// Fired after a frame has completed rendering.
        /// </summary>
        public event FrameEvent FrameEnded;

        #endregion

        #region Properties

        /// <summary>
        /// Specifies the name of the engine that will be used where needed (i.e. log files, etc).  
        /// </summary>
        public string Copyright
        {
            get
            {
                AssemblyCopyrightAttribute attribute =
                    (AssemblyCopyrightAttribute)Attribute.GetCustomAttribute( Assembly.GetExecutingAssembly(), typeof( AssemblyCopyrightAttribute ), false );

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
                return Assembly.GetExecutingAssembly().GetName().Version.ToString();
            }
        }

        /// <summary>
        /// The current SceneManager in use by the engine.
        /// </summary>
        public SceneManager SceneManager
        {
            get
            {
                return sceneManager;
            }
            set
            {
                sceneManager = value;
            }
        }

        /// <summary>
        ///		
        /// </summary>
        public SceneManagerEnumerator SceneManagers
        {
            get
            {
                return sceneManagerList;
            }
        }

        /// <summary>
        /// Gets/Sets the current active RenderSystem that the engine is using.
        /// </summary>
        public RenderSystem RenderSystem
        {
            get
            {
                return activeRenderSystem;
            }
            set
            {
                // Sets the active rendering system
                // Can be called direct or will be called by
                // standard config dialog

                // Is there already an active renderer?
                // If so, disable it and initialize the new one
                if ( activeRenderSystem != null && activeRenderSystem != value )
                {
                    activeRenderSystem.Shutdown();
                }

                activeRenderSystem = value;

                // Tell scene managers
                SceneManagerEnumerator.Instance.SetRenderSystem( activeRenderSystem );
            }
        }

        /// <summary>
        /// The list of available render systems for the engine to use (made available via plugins.
        /// </summary>
        public IEnumerable<RenderSystem> RenderSystems
        {
            get
            {
                return ((INamespaceExtender)
                    Vfs.Instance["/Axiom/RenderSystems/"]).Subtree<RenderSystem>();

                //return renderSystemList;
            }
        }

        /// <summary>
        ///    Gets a reference to the timer being used for timing throughout the engine.
        /// </summary>
        public ITimer Timer
        {
            get
            {
                return timer;
            }
        }

        #endregion

        /// <summary>
        ///    Initializes the renderer.
        /// </summary>
        /// <remarks>
        ///     This method can only be called after a renderer has been
        ///     selected with <see cref="Root.RenderSystem"/>, and it will initialize
        ///     the selected rendering system ready for use.
        /// </remarks>
        /// <param name="autoCreateWindow">
        ///     If true, a rendering window will automatically be created (saving a call to
        ///     <see cref="RenderSystem.CreateRenderWindow"/>). The window will be
        ///     created based on the options currently set on the render system.
        /// </param>
        /// <returns>A reference to the automatically created window (if requested), or null otherwise.</returns>
        public RenderWindow Initialize( bool autoCreateWindow )
        {
            return Initialize( autoCreateWindow, "Axiom Render Window" );
        }

        public SceneManager SetSceneManager( SceneType type )
        {
            return this.SceneManager = sceneManagerList[type];
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
        ///     If true, a rendering window will automatically be created (saving a call to
        ///     <see cref="RenderSystem.CreateRenderWindow"/>). The window will be
        ///     created based on the options currently set on the render system.
        /// </param>
        /// <param name="windowTitle">Title to use by the window.</param>
        /// <returns>A reference to the automatically created window (if requested), or null otherwise.</returns>
        public RenderWindow Initialize( bool autoCreateWindow, string windowTitle )
        {
            if ( activeRenderSystem == null )
            {
                throw new AxiomException( "Cannot initialize - no render system has been selected." );
            }

            new ControllerManager();

            // initialize the current render system
            autoWindow = activeRenderSystem.Initialize( autoCreateWindow, windowTitle );

            // if they chose to auto create a window, also initialize several subsystems
            if ( autoCreateWindow )
            {
                OneTimePostWindowInit();
            }

            // initialize timer
            timer.Reset();

            return autoWindow;
        }

        /// <summary>
        ///    Internal method for one-time tasks after first window creation.
        /// </summary>
        private void OneTimePostWindowInit()
        {
            if ( firstTimePostWindowInit )
            {
                // init material manager singleton, which parse sources for materials
                MaterialManager.Instance.Initialize();

                // init the particle system manager singleton
                ParticleSystemManager.Instance.Initialize();

                // init font manager singletons
                FontManager.Instance.ParseAllSources();

                // init overlay manager
                OverlayManager.Instance.ParseAllSources();

                // init mesh manager
                MeshManager.Instance.Initialize();

                firstTimePostWindowInit = false;
            }
        }

        /// <summary>
        ///		Overloaded method.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="target"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="colorDepth"></param>
        /// <param name="isFullscreen"></param>
        /// <returns></returns>
        public RenderWindow CreateRenderWindow( string name, int width, int height, int colorDepth, bool isFullscreen )
        {
            return CreateRenderWindow( name, width, height, colorDepth, isFullscreen, 0, 0, true, false, IntPtr.Zero );
        }

        /// <summary>
        ///		
        /// </summary>
        /// <param name="name"></param>
        /// <param name="target"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="colorDepth"></param>
        /// <param name="isFullscreen"></param>
        /// <param name="left"></param>
        /// <param name="top"></param>
        /// <param name="depthBuffer"></param>
        /// <param name="handle">
        ///		A handle to a pre-created window to be used for the rendering target.
        ///	 </param>
        /// <returns></returns>
        public RenderWindow CreateRenderWindow( string name, int width, int height, int colorDepth,
            bool isFullscreen, int left, int top, bool depthBuffer, bool vsync, object targetHandle )
        {

            Debug.Assert( activeRenderSystem != null, "Cannot create a RenderWindow without an active RenderSystem." );

            // create a new render window via the current render system
            RenderWindow window =
                activeRenderSystem.CreateRenderWindow(
                name, width, height, colorDepth, isFullscreen, left, top,
                depthBuffer, vsync, targetHandle );

            // do any required initialization
            OneTimePostWindowInit();

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
            Debug.Assert( activeRenderSystem != null, "Cannot covert color value without an active renderer." );

            return activeRenderSystem.ConvertColor( color );
        }

        /// <summary>
        ///     
        /// </summary>
        /// <param name="target"></param>
        public void DetachRenderTarget( RenderTarget target )
        {
            if ( activeRenderSystem == null )
            {
                throw new AxiomException( "Cannot detach render target - no render system has been selected." );
            }

            activeRenderSystem.DetachRenderTarget( target );
        }

        /// <summary>
        ///		Renders one frame.
        /// </summary>
        /// <remarks>
        ///		Updates all the render targets automatically and then returns, raising frame events before and after.
        /// </remarks>
        /// <returns>True if execution should continue, false if a quit was requested.</returns>
        public void RenderOneFrame()
        {
            // Stop rendering if frame callback says so
            OnFrameStarted();

            // bail out before continuing
            if ( queuedEnd )
            {
                return;
            }

            // update all current render targets
            UpdateAllRenderTargets();

            // Stop rendering if frame callback says so
            OnFrameEnded();
        }

        /// <summary>
        ///		Starts the default rendering loop.
        /// </summary>
        public void StartRendering()
        {
            Debug.Assert( activeRenderSystem != null, "Engine cannot start rendering without an active RenderSystem." );

            activeRenderSystem.InitRenderTargets();

            // initialize the vars
            lastStartTime = lastEndTime = timer.Milliseconds;

            // reset to false so that rendering can begin
            queuedEnd = false;

            while ( !queuedEnd )
            {
                // allow OS events to process (if the platform requires it
                PlatformManager.Instance.DoEvents();

                RenderOneFrame();
            }
        }

        /// <summary>
        ///		Shuts down the engine and unloads plugins.
        /// </summary>
        public void Shutdown()
        {
            SceneManagerEnumerator.Instance.ShutdownAll();

            // destroy all auto created GPU programs
            ShadowVolumeExtrudeProgram.Shutdown();

            LogManager.Instance.Write( "*-*-* Axiom Shutdown" );
        }

        /// <summary>
        ///		Requests that the rendering engine shutdown at the beginning of the next frame.
        /// </summary>
        public void QueueEndRendering()
        {
            queuedEnd = true;
        }

        /// <summary>
        ///     Internal method used for updating all <see cref="RenderTarget"/> objects (windows, 
        ///     renderable textures etc) which are set to auto-update.
        /// </summary>
        /// <remarks>
        ///     You don't need to use this method if you're using Axiom's own internal
        ///     rendering loop (<see cref="Root.StartRendering"/>). If you're running your own loop
        ///     you may wish to call it to update all the render targets which are
        ///     set to auto update (<see cref="RenderTarget.AutoUpdated"/>). You can also update
        ///     individual <see cref="RenderTarget"/> instances using their own Update() method.
        /// </remarks>
        public void UpdateAllRenderTargets()
        {
            activeRenderSystem.UpdateAllRenderTargets();
        }

        /// <summary>
        ///		Gets the number of frames drawn since startup.
        /// </summary>
        public ulong CurrentFrameCount
        {
            get
            {
                return currentFrameCount;
            }
        }

        /// <summary>
        ///		Exposes FPS stats to anyone who cares.
        /// </summary>
        public int CurrentFPS
        {
            get
            {
                return (int)currentFPS;
            }
        }

        /// <summary>
        ///		Exposes FPS stats to anyone who cares.
        /// </summary>
        public int BestFPS
        {
            get
            {
                return (int)highestFPS;
            }
        }

        /// <summary>
        ///		Exposes FPS stats to anyone who cares.
        /// </summary>
        public int WorstFPS
        {
            get
            {
                return (int)lowestFPS;
            }
        }

        /// <summary>
        ///		Exposes FPS stats to anyone who cares.
        /// </summary>
        public int AverageFPS
        {
            get
            {
                return (int)averageFPS;
            }
        }

        #region Implementation of IDisposable

        /// <summary>
        ///		Called to shutdown the engine and dispose of all it's resources.
        /// </summary>
        public void Dispose()
        {
            // force the engine to shutdown
            Shutdown();

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
            MaterialSerializer.materialSourceFiles.Clear();


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

            if ( PlatformManager.Instance != null )
            {
                PlatformManager.Instance.Dispose();
            }
            if ( LogManager.Instance != null )
            {
                LogManager.Instance.Dispose();
            }

            instance = null;
        }

        #endregion

        #region Internal Engine Methods

        /// <summary>
        ///    Internal method for calculating the average time between recently fired events.
        /// </summary>
        /// <param name="time">The current time in milliseconds.</param>
        /// <param name="type">The type event to calculate.</param>
        /// <returns>Average time since last event of the same type.</returns>
        private float CalculateEventTime( long time, FrameEventType type )
        {
            float result = 0;

            if ( type == FrameEventType.Start )
            {
                result = (float)( time - lastStartTime ) / 1000;

                // update the last start time before the render targets are rendered
                lastStartTime = time;
            }
            else
            {
                // increment frameCount
                frameCount++;

                // collect performance stats
                if ( ( time - lastCalculationTime ) > 1000 )
                {
                    // Is It Time To Update Our Calculations?
                    // Calculate New Framerate
                    currentFPS = (float)frameCount / (float)( time - lastCalculationTime ) * 1000;

                    // calculate the averge framerate
                    if ( averageFPS == 0 )
                        averageFPS = currentFPS;
                    else
                        averageFPS = ( averageFPS + currentFPS ) / 2.0f;

                    // Is The New Framerate A New Low?
                    if ( currentFPS < lowestFPS || (int)lowestFPS == 0 )
                    {
                        // Set It To The New Low
                        lowestFPS = currentFPS;
                    }

                    // Is The New Framerate A New High?
                    if ( currentFPS > highestFPS )
                    {
                        // Set It To The New High
                        highestFPS = currentFPS;
                    }

                    // Update Our Last Frame Time To Now
                    lastCalculationTime = time;

                    // Reset Our Frame Count
                    frameCount = 0;
                }

                result = (float)( time - lastEndTime ) / 1000;

                lastEndTime = time;
            }

            return result;
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
        ///    This method calculates the frame timing information for you based on the elapsed
        ///    time. If you want to specify elapsed times yourself you should call the other 
        ///    version of this method which takes event details as a parameter.
        /// </remarks>
        public void OnFrameStarted()
        {
            FrameEventArgs e = new FrameEventArgs();
            long now = timer.Milliseconds;
            e.TimeSinceLastFrame = CalculateEventTime( now, FrameEventType.Start );

            // if any event handler set this to true, that will signal the engine to shutdown
            OnFrameStarted( e );
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
        public void OnFrameEnded()
        {
            FrameEventArgs e = new FrameEventArgs();
            long now = timer.Milliseconds;
            e.TimeSinceLastFrame = CalculateEventTime( now, FrameEventType.End );

            // if any event handler set this to true, that will signal the engine to shutdown
            OnFrameEnded( e );
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
        public void OnFrameStarted( FrameEventArgs e )
        {
            // increment the current frame count
            currentFrameCount++;

            // call the event, which automatically fires all registered handlers
            if ( this.FrameStarted != null )
            {
                FrameStarted( this, e );
            }
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
        public void OnFrameEnded( FrameEventArgs e )
        {
            // call the event, which automatically fires all registered handlers
            if ( this.FrameEnded != null )
            {
                FrameEnded( this, e );
            }

            // Tell buffer manager to free temp buffers used this frame
            if ( HardwareBufferManager.Instance != null )
            {
                HardwareBufferManager.Instance.ReleaseBufferCopies();
            }
        }

        #endregion
    }

    #region Frame Events

    /// <summary>
    ///		A delegate for defining frame events.
    /// </summary>
    public delegate void FrameEvent( object source, FrameEventArgs e );

    /// <summary>
    ///		Used to supply info to the FrameStarted and FrameEnded events.
    /// </summary>
    public class FrameEventArgs : EventArgs
    {
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
        End
    }

    #endregion Frame Events
}
