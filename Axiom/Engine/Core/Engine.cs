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

using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using Axiom.Collections;
using Axiom.Controllers;
using Axiom.Fonts;
using Axiom.Gui;
using Axiom.Physics;
using Axiom.Input;
using Axiom.ParticleSystems;
using Axiom.Utility;
using Axiom.Graphics;

namespace Axiom.Core {
    /// <summary>
    ///		A delegate for defining frame events.
    /// </summary>
    public delegate bool FrameEvent(object source, FrameEventArgs e);

    /// <summary>
    ///		Used to supply info to the FrameStarted and FrameEnded events.
    /// </summary>
    public class FrameEventArgs : System.EventArgs {
        /// <summary>
        ///    Time elapsed (in milliseconds) since the last frame event.
        /// </summary>
        public float TimeSinceLastEvent;

        /// <summary>
        ///    Time elapsed (in milliseconds) since the last frame.
        /// </summary>
        public float TimeSinceLastFrame;

        /// <summary>
        ///    Event handlers should set this to true if they wish to stop the render loop and begin shutdown.
        /// </summary>
        public bool Stop;
    }

    /// <summary>
    ///		The Engine class is the main container of all the subsystems.  This includes the RenderSystem, various ResourceManagers, etc.
    /// </summary>
    // INC: In progress
    public class Engine : IDisposable {
        #region Singleton implementation
        static Engine() {}
        private Engine() {
            pluginList = new ArrayList();
        }
        public static readonly Engine Instance = new Engine();
        #endregion

        private ArrayList pluginList;

        private SceneManager sceneManager;
        private SceneManagerList sceneManagerList;
        private RenderSystemCollection renderSystemList;
        private RenderSystem activeRenderSystem;
        private Log engineLog;

        private ITimer timer;

        // Framerate Related State
        private static bool framerateReady;										// Do We Have A Calculated Framerate?
        private static long lastCalculationTime;										// The Last Time We Calculated Framerate
        private static long frameCount;												// Frames Drawn Counter For FPS Calculations
        private static float currentFPS;											// Current FPS
        private static float highestFPS;											// Highest FPS
        private static float lowestFPS = 9999;								// Lowest FPS
        private static float averageFPS;

        /// <summary>
        ///    Flag that specifies whether or not the render loop should end.
        /// </summary>
        private bool stopRendering;

        /// <summary>
        ///    Has the first render window been created yet?
        /// </summary>
        private bool firstTime = true;

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
        public string Name {
            get {
                AssemblyTitleAttribute attribute = 
                    (AssemblyTitleAttribute)Attribute.GetCustomAttribute(Assembly.GetExecutingAssembly(), typeof(AssemblyTitleAttribute), false);

                if(attribute != null)
                    return attribute.Title;
                else
                    return "Not Found";
            }
        }

        /// <summary>
        /// Specifies the name of the engine that will be used where needed (i.e. log files, etc).  
        /// </summary>
        public string Copyright {
            get {
                AssemblyCopyrightAttribute attribute = 
                    (AssemblyCopyrightAttribute)Attribute.GetCustomAttribute(Assembly.GetExecutingAssembly(), typeof(AssemblyCopyrightAttribute), false);

                if(attribute != null)
                    return attribute.Copyright;
                else
                    return "Not Found";
            }
        }

        /// <summary>
        /// Returns the current version of the Engine assembly.
        /// </summary>
        public string Version {
            get {
                // returns the file version of this assembly
                return Assembly.GetExecutingAssembly().GetName().Version.ToString();
            }
        }

        /// <summary>
        /// The current SceneManager in use by the engine.
        /// </summary>
        public SceneManager SceneManager {
            get {
                return sceneManager;
            }
            set {
                sceneManager = value;
            }
        }

        /// <summary>
        ///		
        /// </summary>
        public SceneManagerList SceneManagers {
            get {
                return sceneManagerList;
            }
        }

        /// <summary>
        /// Gets/Sets the current active RenderSystem that the engine is using.
        /// </summary>
        public RenderSystem RenderSystem {
            get {
                return activeRenderSystem;
            }
            set {
                // Sets the active rendering system
                // Can be called direct or will be called by
                // standard config dialog

                // Is there already an active renderer?
                // If so, disable it and initialize the new one
                if(activeRenderSystem != null && activeRenderSystem != value ) {
                    activeRenderSystem.Shutdown();
                }

                activeRenderSystem = value;

                // Tell scene managers
                SceneManagerList.Instance.RegisterRenderSystem(activeRenderSystem);
            }
        }

        /// <summary>
        /// The list of available render systems for the engine to use (made available via plugins.
        /// </summary>
        public RenderSystemCollection RenderSystems {
            get {
                return renderSystemList;
            }
        }

        /// <summary>
        ///    Gets a reference to the timer being used for timing throughout the engine.
        /// </summary>
        public ITimer Timer {
            get {
                return timer;
            }
        }

        #endregion

        /// <summary>
        /// 
        /// </summary>
        /// <param name="autoCreateWindow"></param>
        /// <returns></returns>
        public RenderWindow Initialize(bool autoCreateWindow) {			
            Debug.Assert(activeRenderSystem != null, "Engine cannot be initialized if a valid RenderSystem is not also initialized.");

            // initialize the current render system
            RenderWindow window = activeRenderSystem.Initialize(autoCreateWindow);

            // if they chose to auto create a window, also initialize several subsystems
            if(autoCreateWindow) {
                OneTimePostWindowInit();
            }

            return window;
        }

        /// <summary>
        ///    Internal method for one-time tasks after first window creation.
        /// </summary>
        private void OneTimePostWindowInit() {
            if(firstTime) {
                // init material manager singleton, which parse sources for materials
                MaterialManager.Instance.ParseAllSources();

                // init the particle system manager singleton
                ParticleSystemManager.Instance.ParseAllSources();

                // init font manager singletons
                FontManager.Init();

                // init overlay manager
                OverlayManager.Instance.ParseAllSources();

                firstTime = false;
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
        public RenderWindow CreateRenderWindow(string name, int width, int height, int colorDepth, bool isFullscreen) {
            return CreateRenderWindow(name, width, height, colorDepth, isFullscreen, 0, 0, true, IntPtr.Zero);
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
        public RenderWindow CreateRenderWindow(string name, int width, int height, int colorDepth,
            bool isFullscreen, int left, int top, bool depthBuffer, object targetHandle) {

            Debug.Assert(activeRenderSystem != null, "Cannot create a RenderWindow without an active RenderSystem.");

			// create a new render window via the current render system
            RenderWindow window = 
                activeRenderSystem.CreateRenderWindow(
                name, width, height, colorDepth, isFullscreen, left, top,
                depthBuffer, targetHandle);

            // do any required initialization
            OneTimePostWindowInit();

            return window;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        // TODO: Rethink this if ever a time comes where the .Net Windows Forms implementation is not sufficient
        // for all .Net platforms (Linux, XBox, etc)
        public bool ShowConfigDialog() {
            ConfigDialog dialog = new ConfigDialog();
			
            // if they said ok, lets keep going
            if(dialog.ShowDialog() == DialogResult.OK)
                return true;

            return false;
        }

        /// <summary>
        ///		Asks the current API to convert an instance of ColorEx to a 4 byte packed
        ///		int value the way it would expect it. 		
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public int ConvertColor(ColorEx color) {
            Debug.Assert(activeRenderSystem != null, "Cannot covert color value without an active renderer.");

            return activeRenderSystem.ConvertColor(color);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="show"></param>
        public void ShowDebugOverlay(bool show) {
            Overlay o = OverlayManager.Instance.GetByName("Core/DebugOverlay");

            if(o == null) {
                throw new Exception(string.Format("Could not find overlay named '{0}'.", "Core/DebugOverlay"));
            }

            if(show) {
                o.Show();
            }
            else {
                o.Hide();
            }
        }

        /// <summary>
        /// Used to setup the engine and all it's dependencies.  
        /// This should be called before anything else is.
        /// </summary>
        public void Setup() {
            // create a new engine log
            engineLog = new Log("AxiomEngine.log");

            // add the log to the list of trace listeners to capture output
            System.Diagnostics.Trace.Listeners.Add(engineLog);

            // initialize all singletons, resetting them in the case of running more than once within the same AppDomain
            InitializeSingletons();

            // create a new timer
            timer = PlatformManager.Instance.CreateTimer();

            // get the singleton instance of the SceneManagerList
            sceneManagerList = SceneManagerList.Instance;

            renderSystemList = new RenderSystemCollection();

            // write the initial info at the top of the log
            System.Diagnostics.Trace.WriteLine("*********" + this.Name + " Log *************");
            System.Diagnostics.Trace.WriteLine("---- Copyright " + this.Copyright + " ---");
            System.Diagnostics.Trace.WriteLine("----- Version: " + this.Version + " ------");
            System.Diagnostics.Trace.WriteLine("Engine initializing...");
            System.Diagnostics.Trace.WriteLine(string.Format("Operating System: {0}", Environment.OSVersion.ToString()));
            System.Diagnostics.Trace.WriteLine(string.Format(".Net Framework: {0}", Environment.Version.ToString()));

            // dynamically load plugins
            this.LoadPlugins();
        }
		
        /// <summary>
        ///		Starts the default rendering loop.
        /// </summary>
        public void StartRendering() {
            Debug.Assert(activeRenderSystem != null, "Engine cannot start rendering without an active RenderSystem.");

            long lastStartTime, lastEndTime;

            // start the internal timer
            timer.Reset();

            // initialize the vars
            lastStartTime = lastEndTime = timer.Milliseconds;

            // get a reference to the render windows of the current render system
            RenderWindowCollection renderWindows = activeRenderSystem.RenderWindows;

            while(renderWindows.Count > 0 && !stopRendering) {
                // if we only have 1 window and it isn't active, skip the rendering loop
                // so that things dont happen while it is minimized
                if(renderWindows.Count == 1 && !renderWindows[0].IsActive) {
                    // allow windows events to process
                    System.Windows.Forms.Application.DoEvents();

                    continue;
                }

                FrameEventArgs evt = new FrameEventArgs();

                // get the current time
                long time = timer.Milliseconds;

                // only fire a frame started event if time has elapsed
                // prevent overupdating
                if(time != lastStartTime || time != lastEndTime) {
                    evt.TimeSinceLastFrame = (float)(time - lastStartTime) / 1000;
                    evt.TimeSinceLastEvent = (float)(time - lastEndTime) / 1000;

                    // Stop rendering if frame callback says so
                    if(!OnFrameStarted(evt) || stopRendering)
                        return;

                    // We'll also check here if they decided to shut us down
                }

                // update the last start time before the render targets are rendered
                lastStartTime = time;

                // force all active render windows to update
                for(int i = 0; i < renderWindows.Count; i++) {
                    if(renderWindows[i].IsActive)
                        renderWindows[i].Update();
                }

                // increment frameCount
                frameCount++;

                // Do frame ended event
                time = timer.Milliseconds; // Get current time

                // collect performance stats
                if((time - lastCalculationTime) > 1000) { 
				 		// Is It Time To Update Our Calculations?
                    // Calculate New Framerate
                    currentFPS = (float)frameCount / (float)(time - lastCalculationTime) * 1000;

                    // calculate the averge framerate
                    if(averageFPS == 0)
                        averageFPS = currentFPS;
                    else
                        averageFPS = (averageFPS + currentFPS) / 2.0f;

					 // Is The New Framerate A New Low?
                    if(currentFPS < lowestFPS || (int) lowestFPS == 0) { 
                        // Set It To The New Low
                        lowestFPS = currentFPS;							
                    }

                    // Is The New Framerate A New High?
                    if(currentFPS > highestFPS) { 
					 	// Set It To The New High
                        highestFPS = currentFPS;
                    }

                    // Update Our Last Frame Time To Now
                    lastCalculationTime = time;

                    // Reset Our Frame Count
                    frameCount = 0;												
                }


                if (lastEndTime != time || time != lastStartTime) {
                    evt.TimeSinceLastFrame = (float)(time - lastEndTime) / 1000;
                    evt.TimeSinceLastEvent = (float)(time - lastStartTime) / 1000;
                    // Stop rendering if frame callback says so
                    if(!OnFrameEnded(evt) || stopRendering)
                        return;
                }

                lastEndTime = time;

                // allow windows events to process
                System.Windows.Forms.Application.DoEvents();
            }

        }

        /// <summary>
        ///		Shuts down the engine and unloads plugins.
        /// </summary>
        public void Shutdown() {
            System.Diagnostics.Trace.WriteLine("***** " + this.Name + " Shutdown Initiated. ****");

            // trigger a disposal of all resources
            // destroy all textures
            if(TextureManager.Instance != null)
                TextureManager.Instance.Dispose();

            // shutdown the current render system if there is one
            if(activeRenderSystem != null)
                activeRenderSystem.Shutdown();

            // destroy all disposable objects
            GarbageManager.Instance.DisposeAll();

            // unload all plugins that were loaded at engine start
            UnloadPlugins();

            // Write final performance stats
            System.Diagnostics.Trace.WriteLine("Final Stats:");
            System.Diagnostics.Trace.WriteLine("Axiom Framerate Average FPS: " + averageFPS.ToString("0.000000") + " Best FPS: " + highestFPS.ToString("0.000000") + " Worst FPS: " + lowestFPS.ToString("0.000000"));
            
            engineLog.Dispose();
        }

        private void InitializeSingletons() {
            PlatformManager.Init();
            MaterialManager.Init();
            ParticleSystemManager.Init();
            SceneManagerList.Init();
            OverlayManager.Init();
            GuiManager.Init();
            HighLevelGpuProgramManager.Init();

            GarbageManager.Instance.Add(ParticleSystemManager.Instance);
            GarbageManager.Instance.Add(MaterialManager.Instance);
            GarbageManager.Instance.Add(ControllerManager.Instance);
            GarbageManager.Instance.Add(OverlayManager.Instance);
        }

        /// <summary>
        ///		Exposes FPS stats to anyone who cares.
        /// </summary>
        public int CurrentFPS {
            get { 
                return (int)currentFPS; 
            }
        }

        /// <summary>
        ///		Exposes FPS stats to anyone who cares.
        /// </summary>
        public int BestFPS {
            get { 
                return (int)highestFPS; 
            }
        }

        /// <summary>
        ///		Exposes FPS stats to anyone who cares.
        /// </summary>
        public int WorstFPS {
            get { 
                return (int)lowestFPS; 
            }
        }

        /// <summary>
        ///		Exposes FPS stats to anyone who cares.
        /// </summary>
        public int AverageFPS {
            get { 
                return (int)averageFPS; 
            }
        }

        #region Implementation of IDisposable

        /// <summary>
        ///		Called to shutdown the engine and dispose of all it's resources.
        /// </summary>
        public void Dispose() {
            // force the engine to shutdown
            Shutdown();

            engineLog.Dispose();
        }
        #endregion

        #region Internal Engine Methods

        /// <summary>
        ///		Used to manually fire the FrameStarted event.
        /// </summary>
        /// <param name="eventArgs"></param>
        protected bool OnFrameStarted(FrameEventArgs eventArgs) {
            // call the event, which automatically fires all registered handlers
            if(this.FrameStarted != null) {
                return FrameStarted(this, eventArgs);
            }
            else {
                return false;
            }
        }

        /// <summary>
        ///		Used to manually fire the FrameEnded event.
        /// </summary>
        /// <param name="eventArgs"></param>
        protected bool OnFrameEnded(FrameEventArgs eventArgs) {
            // call the event, which automatically fires all registered handlers
            if(this.FrameEnded != null) {
                return FrameEnded(this, eventArgs);
            }
            else {
                return false;
            }
        }

        /// <summary>
        /// Searches for IPlugin implementations for the engine and loads them.
        /// </summary>
        internal void LoadPlugins() {
			// load all registered plugins
			PluginManager.Instance.LoadAll();
        }

        /// <summary>
        /// Used to unload any previously loaded plugins.
        /// </summary>
        internal void UnloadPlugins() {
			PluginManager.Instance.UnloadAll();
        }


        #endregion
    }
}
