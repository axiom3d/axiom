#region LGPL License
/*
Axiom Game Engine Library
Copyright (C) 2003  Axiom Project Team

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
using Axiom.Physics;
using Axiom.Input;
using Axiom.ParticleSystems;
using Axiom.Utility;
using Axiom.SubSystems.Rendering;

namespace Axiom.Core
{
	/// <summary>
	///		A delegate for defining frame events.
	/// </summary>
	public delegate bool FrameEvent(object source, FrameEventArgs e);

	/// <summary>
	///		Used to supply info to the FrameStarted and FrameEnded events.
	/// </summary>
	public struct FrameEventArgs
	{
		public float TimeSinceLastEvent;
		public float TimeSinceLastFrame;
	}

	/// <summary>
	/// The Engine class is the main container of all the subsystems.  This includes the RenderSystem, various ResourceManagers, etc.
	/// </summary>
	// INC: In progress
	public class Engine : IDisposable
	{
		#region Singleton implementation
			static Engine() {}
			private Engine() 
			{
				pluginList = new ArrayList();
				timer = new HighResolutionTimer();

			}
			public static readonly Engine Instance = new Engine();
		#endregion

		private ArrayList pluginList;

		private SceneManager sceneManager;
		private SceneManagerList sceneManagerList;
		private RenderSystemCollection renderSystemList;
		private RenderSystem activeRenderSystem;
		private TextureManager textureManager;
		private Log engineLog;
		private InputSystem inputSystem;

		protected HighResolutionTimer timer;
		// Framerate Related State
		protected static bool framerateReady = false;										// Do We Have A Calculated Framerate?
		protected static ulong timerFrequency;											// The Frequency Of The Timer
		//protected static ulong currentFrameTime;											// The Current Frame Time
		protected static ulong lastCalculationTime;										// The Last Time We Calculated Framerate
		protected static ulong framesDrawn;												// Frames Drawn Counter For FPS Calculations
		protected static float currentFramerate;											// Current FPS
		protected static float highestFramerate;											// Highest FPS
		protected static float lowestFramerate = 999999999;								// Lowest FPS

		protected bool stopRendering;

		/// <summary>A flag which safeguards against Setup being run more than once.</summary>
		protected bool isSetupComplete;

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
		public String Name
		{
			get
			{
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
		public String Copyright
		{
			get
			{
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
		public String Version
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
		public SceneManagerList SceneManagers
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
				if(activeRenderSystem != null && activeRenderSystem != value )
				{
					activeRenderSystem.Shutdown();
				}

				activeRenderSystem = value;

				// Tell scene managers
				SceneManagerList.Instance.RegisterRenderSystem(activeRenderSystem);
			}
		}

		/// <summary>
		///		Gets/Sets the current texture manager.  Should be set in RenderSystem plugins.
		/// </summary>
		public TextureManager TextureManager
		{
			get
			{
				return textureManager;
			}
		}

		/// <summary>
		/// The list of available render systems for the engine to use (made available via plugins.
		/// </summary>
		public RenderSystemCollection RenderSystems
		{
			get
			{
				return renderSystemList;
			}
		}

		/// <summary>
		///		Gets/Sets the current system to use for reading input.
		/// </summary>
		public InputSystem InputSystem
		{
			get 
			{ 
				return inputSystem;	
			}
			set 
			{ 
				if(inputSystem == null) 
					inputSystem = value; 
			}
		}

	#endregion

		/// <summary>
		/// 
		/// </summary>
		/// <param name="autoCreateWindow"></param>
		/// <returns></returns>
		// TODO: Revisit this
		public RenderWindow Initialize(bool autoCreateWindow)
		{			
			Debug.Assert(activeRenderSystem != null, "Engine cannot be initialized if a valid RenderSystem is not also initialized.");

			// initialize the current render system
			RenderWindow window = activeRenderSystem.Initialize(autoCreateWindow);

			// have the render system check the hardware capabilities
			activeRenderSystem.CheckCaps();

			// if they chose to auto create a window, also initialize several subsystems
			if(autoCreateWindow)
			{
				// init material manager singleton, which parse sources for materials
				MaterialManager.Init();

				//init particle system manager singleton
				ParticleSystemManager.Instance.Initialize();

				// init font manager singletons
				FontManager.Init();

				// init overlay manager
				OverlayManager.Init();
			}

			//LoadPlugins();

			return window;
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
		public RenderWindow CreateRenderWindow(string name, System.Windows.Forms.Control target, int width, int height, int colorDepth, bool isFullscreen)
		{
			return CreateRenderWindow(name, target, width, height, colorDepth, isFullscreen, 0, 0, true, null);
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
		/// <param name="parent"></param>
		/// <returns></returns>
		public RenderWindow CreateRenderWindow(
			string name, System.Windows.Forms.Control target, int width, int height, int colorDepth,
			bool isFullscreen, int left, int top, bool depthBuffer, RenderWindow parent)
		{
			Debug.Assert(activeRenderSystem != null, "Cannot create a RenderWindow without an active RenderSystem.");

			RenderWindow window = 
				activeRenderSystem.CreateRenderWindow(
					name, target, width, height, colorDepth, isFullscreen, left, top,
					depthBuffer, parent);

			// is this the first window being created?
			if(activeRenderSystem.RenderWindows.Count == 1)
			{
				// init the material manager singleton
				MaterialManager.Init();

				// init the particle system manager singleton
				ParticleSystemManager.Init();

				// init font manager singleton
				FontManager.Init();

				// init overlay manager singleton
				OverlayManager.Init();
			}

			return window;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		// TODO: Rethink this if ever a time comes where the .Net Windows Forms implementation is not sufficient
		// for all .Net platforms (Linux, XBox, etc)
		public bool ShowConfigDialog()
		{
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
		public int ConvertColor(ColorEx color)
		{
			Debug.Assert(activeRenderSystem != null, "Cannot covert color value without an active renderer.");

			return activeRenderSystem.ConvertColor(color);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="makeVisible"></param>
		// TODO: Implementation
		public void ShowDebugOverlay(bool makeVisible)
		{

		}

		/// <summary>
		/// Used to setup the engine and all it's dependencies.  
		/// This should be called before anything else is.
		/// </summary>
		public void Setup()
		{
			// create a new engine log
			engineLog = new Log("AxiomEngine.log");

			// add the log to the list of trace listeners to capture output
			System.Diagnostics.Trace.Listeners.Add(engineLog);

			// initialize all singletons, resetting them in the case of running more than once within the same AppDomain
			InitializeSingletons();

			// get the singleton instance of the SceneManagerList
			sceneManagerList = SceneManagerList.Instance;

			renderSystemList = new RenderSystemCollection();

			// write the initial info at the top of the log
			System.Diagnostics.Trace.WriteLine("*********" + this.Name + " Log *************");
			System.Diagnostics.Trace.WriteLine("---- Copyright " + this.Copyright + " ---");
			System.Diagnostics.Trace.WriteLine("----- Version: " + this.Version + " ------");
			System.Diagnostics.Trace.WriteLine("Engine initializing...");

			// dynamically load plugins
			this.LoadPlugins();
		}
		
		/// <summary>
		///		Starts the default rendering loop.
		/// </summary>
		public void StartRendering()
		{
			Debug.Assert(activeRenderSystem != null, "Engine cannot start rendering without an active RenderSystem.");

			ulong lastStartTime, lastEndTime;

			// start the internal timer
			timer.Start();

			// capture the frequency of the timer for fps calculations
			timerFrequency = timer.Frequency;

			// initialize the vars
			lastStartTime = lastEndTime = timer.Count;

			// get a reference to the render windows of the current render system
			RenderWindowCollection renderWindows = activeRenderSystem.RenderWindows;

			while(renderWindows.Count > 0 && !stopRendering)
			{
				// if we only have 1 window and it isn't active, skip the rendering loop
				// so that things dont happen while it is minimized
				if(renderWindows.Count == 1 && !renderWindows[0].IsActive)
				{
					// allow windows events to process
					System.Windows.Forms.Application.DoEvents();

					continue;
				}

				FrameEventArgs evt = new FrameEventArgs();

				// get the current time
				ulong time = timer.Count;

				// only fire a frame started event if time has elapsed
				// prevent overupdating
				if(time != lastStartTime || time != lastEndTime)
				{
					evt.TimeSinceLastFrame = (float)(time - lastStartTime) / timer.Frequency;
					evt.TimeSinceLastEvent = (float)(time - lastEndTime) / timer.Frequency;

					// Stop rendering if frame callback says so
					if(!OnFrameStarted(evt) || stopRendering)
						return;

					// We'll also check here if they decided to shut us down
				}

				// update the last start time before the render targets are rendered
				lastStartTime = time;

				// force all active render windows to update
				for(int i = 0; i < renderWindows.Count; i++)
				{
					if(renderWindows[i].IsActive)
						renderWindows[i].Update();
				}

				// increment framesDrawn
				framesDrawn++;

				// Do frame ended event
				time = timer.Count; // Get current time

				// collect performance stats
				if((time - lastCalculationTime) > timerFrequency) 
				{		// Is It Time To Update Our Calculations?
					// Calculate New Framerate
					currentFramerate = (framesDrawn * timerFrequency) / (time - lastCalculationTime);

					if(currentFramerate < lowestFramerate || lowestFramerate == 0) 
					{						// Is The New Framerate A New Low?
						lowestFramerate = currentFramerate;							// Set It To The New Low
					}

					if(currentFramerate > highestFramerate) 
					{						// Is The New Framerate A New High?
						highestFramerate = currentFramerate;						// Set It To The New High
					}

					lastCalculationTime = time;							// Update Our Last Frame Time To Now
					framesDrawn = 0;												// Reset Our Frame Count

				}


				if (lastEndTime != time || time != lastStartTime)
				{
					evt.TimeSinceLastFrame = (float)(time - lastEndTime) / timer.Frequency;
					evt.TimeSinceLastEvent = (float)(time - lastStartTime) / timer.Frequency;
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
		public void Shutdown()
		{
			System.Diagnostics.Trace.WriteLine("***** " + this.Name + " Shutdown Initiated. ****");

			// trigger a disposal of all resources
			// TODO: This actually will do all resources (not just textures) because it calls the base class Dispose, which calls UnloadAndDestroyAll on all resources.
			if(TextureManager.Instance != null)
				TextureManager.Instance.Dispose();

			if(activeRenderSystem != null)
			{
				// shutdown the current render system
				activeRenderSystem.Shutdown();
			}

			// destroy all disposable objects
			GarbageManager.Instance.DisposeAll();

			// unload all plugins that were loaded at engine start
			UnloadPlugins();

			// Write final performance stats
			System.Diagnostics.Trace.WriteLine("Final Stats:\r\nHighest FPS - " + highestFramerate + "\r\nLowest FPS: " + lowestFramerate);
		}

		private void InitializeSingletons()
		{
			ParticleSystemManager.Init();

			// init the SceneManagerList
			SceneManagerList.Init();

			GarbageManager.Instance.Add(ParticleSystemManager.Instance);
			GarbageManager.Instance.Add(MaterialManager.Instance);
			GarbageManager.Instance.Add(ControllerManager.Instance);

		}

		/// <summary>
		///		Exposes FPS stats to anyone who cares.
		/// </summary>
		public int CurrentFPS
		{
			get { return (int)currentFramerate; }
		}

		#region Implementation of IDisposable

		/// <summary>
		///		Called to shutdown the engine and dispose of all it's resources.
		/// </summary>
		public void Dispose()
		{
			// force the engine to shutdown
			//Shutdown();

			engineLog.Dispose();
		}
		#endregion

		#region Internal Engine Methods

		/// <summary>
		///		Used to manually fire the FrameStarted event.
		/// </summary>
		/// <param name="pEventArgs"></param>
		protected bool OnFrameStarted(FrameEventArgs eventArgs)
		{
			// call the event, which automatically fires all registered handlers
			if(this.FrameStarted != null)
			{
				return FrameStarted(this, eventArgs);
			}
			else
			{
				return false;
			}
		}

		/// <summary>
		///		Used to manually fire the FrameEnded event.
		/// </summary>
		/// <param name="pEventArgs"></param>
		protected bool OnFrameEnded(FrameEventArgs eventArgs)
		{
			// call the event, which automatically fires all registered handlers
			if(this.FrameEnded != null)
			{
				return FrameEnded(this, eventArgs);
			}
			else
			{
				return false;
			}
		}

		/// <summary>
		/// Searches for IPlugin implementations for the engine and loads them.
		/// </summary>
		internal void LoadPlugins()
		{
			// get a list of .dll files in the current directory

			// TODO: Figure out how to make searching for assemblies cross platform compatible.  Linux, XBox likely dont use ".dll" extensions.
			string[] files = Directory.GetFiles(Environment.CurrentDirectory, "*.dll");
			
			// loop through and load the assemblies 
			for(int i = 0; i < files.Length; i++)
			{
				// dont load the engine .dll itself
				if(files[i].Equals("AxiomEngine.dll"))
					continue;

				// load the assembly
				Assembly assembly = Assembly.LoadFrom(files[i]);

				// get the list of types within the assembly
				Type[] types = assembly.GetTypes();

				// check each class to see if it implements the IPlugin interface
				foreach(Type type in types)
				{
					// if the type implements the interface, then...
					if(type.GetInterface("IPlugin") != null)
					{
						/// ...create an instance of it
						IPlugin plugin = (IPlugin)Activator.CreateInstance(type);

						AssemblyTitleAttribute title = 
							(AssemblyTitleAttribute)Attribute.GetCustomAttribute(assembly, typeof(AssemblyTitleAttribute));
						
						// log the fact that the plugin is being loaded
						System.Diagnostics.Trace.WriteLine("Loading plugin: " + title.Title);

						// invoke the start method to fire up the plugin
						plugin.Start();

						// keep the plugin around for later release
						pluginList.Add(plugin);
					}
				}
			}
		}

		/// <summary>
		/// Used to unload any previously loaded plugins.
		/// </summary>
		internal void UnloadPlugins()
		{
			// loop through and stop each plugin
			foreach(IPlugin plugin in pluginList)
			{
				AssemblyTitleAttribute title = 
					(AssemblyTitleAttribute)Attribute.GetCustomAttribute(plugin.GetType().Assembly, typeof(AssemblyTitleAttribute));

				// log the fact that the plugin is being loaded
				System.Diagnostics.Trace.WriteLine("Unloading plugin: " + title.Title);

				// stop the plugin from running
				plugin.Stop();
			}

			// empty the list of plugins
			pluginList.Clear();
		}


		#endregion
	}
}
