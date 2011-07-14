#region MIT/X11 License
//Copyright © 2003-2011 Axiom 3D Rendering Engine Project
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

using System;
using System.Collections.Generic;

using Axiom.Collections;
using Axiom.Core;
using Axiom.Graphics;

using SIS = SharpInputSystem;
using Axiom.Framework.Configuration;

namespace Axiom.Samples
{
	/// <summary>
	/// Base class responsible for setting up a common context for samples.
	/// May be subclassed for specific sample types (not specific samples).
	/// Allows one sample to run at a time, while maintaining a sample queue.
	/// </summary>
	public class SampleContext : IWindowEventListener, SIS.IKeyboardListener, SIS.IMouseListener, IDisposable
	{
		public const string DefaultResourceGroupName = "Essential";

		#region Fields and Properties

		/// <summary>
		/// Axiom root
		/// </summary>
		protected Root Root;
		/// <summary>
		/// Configuration Manager
		/// </summary>
		protected IConfigurationManager ConfigurationManager;
		/// <summary>
		/// SharpInputSystem Input Manager
		/// </summary>
		protected SIS.InputManager InputManager;
		/// <summary>
		/// Keyboard Device
		/// </summary>
		protected SIS.Keyboard Keyboard;
		/// <summary>
		/// Mouse Device
		/// </summary>
		protected SIS.Mouse Mouse;
		/// <summary>
		/// Whether or not this is the final run
		/// </summary>
		protected bool IsLastRun;
		/// <summary>
		/// Name of renderer used for next run
		/// </summary>
		protected String NextRenderer;
		/// <summary>
		/// last sample run before reconfiguration
		/// </summary>
		protected Sample LastSample;
		/// <summary>
		/// state of last sample
		/// </summary>
		protected NameValuePairList LastSampleState;

		protected bool IsSamplePaused;
		/// <summary>
		/// Whether current sample is paused
		/// </summary>
		public virtual bool IsCurrentSamplePaused
		{
			get
			{
				if ( CurrentSample != null )
					return this.IsSamplePaused;
				return false;
			}
		}

		/// <summary>
		/// The Render Window
		/// </summary>
		public virtual RenderWindow RenderWindow
		{
			get;
			protected set;
		}

		/// <summary>
		/// Current running sample
		/// </summary>
		public virtual Sample CurrentSample
		{
			get;
			protected set;
		}

		#endregion Fields and Properties

		#region Construction and Destruction

		/// <summary>
		/// Creates a new instance of the type <see cref="SampleContext"/>
		/// </summary>
		/// <param name="cfgManager"></param>
		public SampleContext( IConfigurationManager cfgManager )
		{
			this.Root = null;
			this.ConfigurationManager = cfgManager;
			this.RenderWindow = null;
			this.CurrentSample = null;
			this.IsSamplePaused = false;
			this.IsLastRun = false;
			this.LastSample = null;
		}

		~SampleContext()
		{
		}

		#endregion

		/// <summary>
		/// Quits the current sample and starts a new one.
		/// </summary>
		/// <param name="s"></param>
		public virtual void RunSample( Sample s )
		{
			if ( CurrentSample != null )
			{
				CurrentSample.Shutdown();    // quit current sample
				this.IsSamplePaused = false;          // don't pause the next sample
			}

			RenderWindow.RemoveAllViewports();                  // wipe viewports

			if ( s != null )
			{
				// retrieve sample's required plugins and currently installed plugins
				var ip = PluginManager.Instance.InstalledPlugins;
				IList<String> rp = s.RequiredPlugins;

				string errorMsg = String.Empty;
				foreach ( string pluginName in rp )
				{
					bool found = false;
					//try to find the required plugin in the current installed plugins
					foreach ( IPlugin plugin in ip )
					{
						//if(plugin.na
						found = true;
						break;
					}

					if ( !found )  // throw an exception if a plugin is not found
					{
						String desc = String.Format( "Sample requires plugin: {0}", pluginName );
						this.Log( desc );
						errorMsg += desc + Environment.NewLine;
					}
				}
				if ( errorMsg != String.Empty )
					throw new AxiomException( errorMsg );

				// throw an exception if samples requires the use of another renderer
				errorMsg = String.Empty;
				String rrs = s.RequiredRenderSystem;
				if ( !String.IsNullOrEmpty( rrs ) && rrs != this.Root.RenderSystem.Name )
				{
					String desc = "Sample only runs with renderer: {0}";
					throw new AxiomException( desc, rrs );
				}

				// test system capabilities against sample requirements
				s.TestCapabilities( Root.RenderSystem.Capabilities );

				s.Setup( RenderWindow, this.Keyboard, this.Mouse );   // start new sample
			}

			CurrentSample = s;
		}

		/// <summary>
		/// This function encapsulates the entire lifetime of the context.
		/// </summary>
		public virtual void Go()
		{
			this.Go( null );
		}

		/// <summary>
		/// This function encapsulates the entire lifetime of the context.
		/// </summary>
		/// <param name="initialSample"></param>
		public virtual void Go( Sample initialSample )
		{
			bool firstRun = true;

			while ( !this.IsLastRun )
			{
				this.IsLastRun = true; // assume this is our last run

				CreateRoot();
				if ( !OneTimeConfig() )
					return;

				// if the context was reconfigured, set requested renderer
				if ( !firstRun )
					this.Root.RenderSystem = this.Root.RenderSystems[ this.NextRenderer ];

				Setup();

				// restore the last sample if there was one or, if not, start initial sample
				if ( !firstRun )
					RecoverLastSample();
				else if ( initialSample != null )
					RunSample( initialSample );

				this.Root.StartRendering(); // start the render loop

				ConfigurationManager.SaveConfiguration( Root, this.NextRenderer );

				Shutdown();
				if ( this.Root != null )
					this.Root.Dispose();
				firstRun = false;
			}
		}

		/// <summary>
		/// Pauses the current running sample
		/// </summary>
		public virtual void PauseCurrentSample()
		{
			if ( CurrentSample != null && !this.IsSamplePaused )
			{
				this.IsSamplePaused = true;
				CurrentSample.Paused();
			}
		}

		/// <summary>
		/// Unpauses the current sample
		/// </summary>
		public virtual void UnpauseCurrentSample()
		{
			if ( CurrentSample != null && this.IsSamplePaused )
			{
				this.IsSamplePaused = false;
				CurrentSample.Unpaused();
			}
		}

		/// <summary>
		///  Processes frame started events.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="evt"></param>
		/// <returns></returns>
		public virtual void FrameStarted( object sender, FrameEventArgs evt )
		{
			CaptureInputDevices();      // capture input
			// manually call sample callback to ensure correct order
			if ( CurrentSample != null  && !IsSamplePaused ) 
			{ 
				CurrentSample.FrameStarted( evt );
			} 
		}

		/// <summary>
		/// Processes rendering queued events.
		/// </summary>
		/// <param name="evt"></param>
		/// <returns></returns>
		public virtual void FrameRenderingQueued( object sender, FrameEventArgs evt )
		{
			// manually call sample callback to ensure correct order
			if ( CurrentSample != null && !IsSamplePaused )
			{
				CurrentSample.FrameRenderingQueued( evt );
			}
		}

		/// <summary>
		/// Processes frame ended events.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="evt"></param>
		/// <returns></returns>
		public virtual void FrameEnded( object sender, FrameEventArgs evt )
		{
			// manually call sample callback to ensure correct order
			if ( CurrentSample != null && !IsSamplePaused )
			{
				CurrentSample.FrameStarted( evt );
			}

			// quit if window was closed
			if ( RenderWindow.IsClosed )
				evt.StopRendering = true;
			// go into idle mode if current sample has ended
			if ( CurrentSample != null && CurrentSample.IsDone )
				RunSample( null );
		}

		/// <summary>
		/// Processes window size change event. Adjusts mouse's region to match that
		/// of the window. You could also override this method to prevent resizing.
		/// </summary>
		/// <param name="rw"></param>
		public virtual void WindowResized( RenderWindow rw )
		{
			// manually call sample callback to ensure correct order
			if ( CurrentSample != null && !this.IsSamplePaused )
				CurrentSample.WindowResized( rw );
			if ( Mouse != null )
			{
				SIS.MouseState ms = this.Mouse.MouseState;
				ms.Width = rw.Width;
				ms.Height = rw.Height;
			}
		}

		// window event callbacks which manually call their respective sample callbacks to ensure correct order

		public virtual void WindowMoved( RenderWindow rw )
		{
			if ( CurrentSample != null && !this.IsSamplePaused )
				CurrentSample.WindowMoved( rw );
		}

		public virtual bool WindowClosing( RenderWindow rw )
		{
			if ( CurrentSample != null && !this.IsSamplePaused )
				return CurrentSample.WindowClosing( rw );
			return true;
		}

		public virtual void WindowClosed( RenderWindow rw )
		{
			if ( CurrentSample != null && !this.IsSamplePaused )
				CurrentSample.WindowClosed( rw );
		}

		public virtual void WindowFocusChange( RenderWindow rw )
		{
			if ( CurrentSample != null && !this.IsSamplePaused )
				CurrentSample.WindowFocusChange( rw );
		}

		// keyboard and mouse callbacks which manually call their respective sample callbacks to ensure correct order

		public virtual bool KeyPressed( SIS.KeyEventArgs evt )
		{
			if ( CurrentSample != null && !this.IsSamplePaused )
				return CurrentSample.KeyPressed( evt );
			return true;
		}

		public virtual bool KeyReleased( SIS.KeyEventArgs evt )
		{
			if ( CurrentSample != null && !this.IsSamplePaused )
				return CurrentSample.KeyReleased( evt );
			return true;
		}

		public virtual bool MouseMoved( SIS.MouseEventArgs evt )
		{
			if ( CurrentSample != null && !this.IsSamplePaused )
				return CurrentSample.MouseMoved( evt );
			return true;
		}

		public virtual bool MousePressed( SIS.MouseEventArgs evt, SIS.MouseButtonID id )
		{
			if ( CurrentSample != null && !this.IsSamplePaused )
				return CurrentSample.MousePressed( evt, id );
			return true;
		}

		public virtual bool MouseReleased( SIS.MouseEventArgs evt, SIS.MouseButtonID id )
		{
			if ( CurrentSample != null && !this.IsSamplePaused )
				return CurrentSample.MouseReleased( evt, id );
			return true;
		}

		/// <summary>
		/// Creates the Axiom root.
		/// </summary>
		protected virtual void CreateRoot()
		{
			this.Root = new Root( this.ConfigurationManager.LogFilename );
		}

		/// <summary>
		/// Configures the startup settings for Axiom. It will first
		/// load the settings from a configuration file, then open
		/// a dialog for any further configuration.
		/// </summary>
		/// <returns></returns>
		protected virtual bool OneTimeConfig()
		{
			if ( this.ConfigurationManager.RestoreConfiguration( this.Root ) )
				return true;

			return this.ConfigurationManager.ShowConfigDialog( this.Root );
		}

		/// <summary>
		/// Sets up the context after configuration.
		/// </summary>
		protected virtual void Setup()
		{
			CreateWindow();
			SetupInput();
			LocateResources();
			LoadResources();

			TextureManager.Instance.DefaultMipmapCount = 5;

			// adds context as listener to process context-level (above the sample level) events
			this.Root.FrameStarted += this.FrameStarted;
			this.Root.FrameEnded += this.FrameEnded;
			this.Root.FrameRenderingQueued += this.FrameRenderingQueued;
			WindowEventMonitor.Instance.RegisterListener( RenderWindow, this );
		}

		/// <summary>
		/// Creates the render window to be used for this context. I use an auto-created
		/// window here, but you can also create an external window if you wish.
		/// Just don't forget to initialize the root.
		/// </summary>
		protected virtual void CreateWindow()
		{
			RenderWindow = this.Root.Initialize( true );
		}

		/// <summary>
		/// Sets up SIS input.
		/// </summary>
		protected virtual void SetupInput()
		{
			SIS.ParameterList pl = new SIS.ParameterList();
			pl.Add( new SIS.Parameter( "WINDOW", RenderWindow[ "WINDOW" ] ) );
#if !(XBOX || XBOX360 )
			pl.Add( new SIS.Parameter( "w32_mouse", "CLF_BACKGROUND" ) );
			pl.Add( new SIS.Parameter( "w32_mouse", "CLF_NONEXCLUSIVE" ) );
#endif
			this.InputManager = SIS.InputManager.CreateInputSystem( pl );

			CreateInputDevices();      // create the specific input devices

			this.WindowResized( RenderWindow );    // do an initial adjustment of mouse area
		}

		/// <summary>
		/// Creates the individual input devices. I only create a keyboard and mouse
		/// here because they are the most common, but you can override this method
		/// for other modes and devices.
		/// </summary>
		protected virtual void CreateInputDevices()
		{
#if !(XBOX || XBOX360 )
			this.Keyboard = this.InputManager.CreateInputObject<SIS.Keyboard>( true, "" );
			this.Mouse = this.InputManager.CreateInputObject<SIS.Mouse>( true, String.Empty );

			this.Keyboard.EventListener = this;
			this.Mouse.EventListener = this;
#endif
		}

		/// <summary>
		/// Finds context-wide resource groups. I load paths from a config file here,
		/// but you can choose your resource locations however you want.
		/// </summary>
		protected virtual void LocateResources()
		{
		}

		/// <summary>
		/// Loads context-wide resource groups. I chose here to simply initialise all
		/// groups, but you can fully load specific ones if you wish.
		/// </summary>
		protected virtual void LoadResources()
		{
			ResourceGroupManager.Instance.InitializeAllResourceGroups();
		}

		/// <summary>
		/// Reconfigures the context. Attempts to preserve the current sample state.
		/// </summary>
		/// <param name="renderer"></param>
		/// <param name="options"></param>
		protected virtual void Reconfigure( String renderer, NameValuePairList options )
		{
			// save current sample state
			this.LastSample = CurrentSample;
			if ( CurrentSample != null )
				CurrentSample.SaveState( this.LastSampleState );

			this.NextRenderer = renderer;
			Axiom.Graphics.RenderSystem rs = this.Root.RenderSystems[ renderer ];

			// set all given render system options
			foreach ( var option in options )
			{
				rs.ConfigOptions[ option.Key ].Value = option.Value;
			}

			this.IsLastRun = false;            // we want to go again with the new settings
			this.Root.QueueEndRendering();   // break from render loop
		}

		/// <summary>
		/// Recovers the last sample after a reset. You can override in the case that
		/// the last sample is destroyed in the process of resetting, and you have to
		/// recover it through another means.
		/// </summary>
		protected virtual void RecoverLastSample()
		{
			RunSample( this.LastSample );
			this.LastSample.RestoreState( this.LastSampleState );
			this.LastSample = null;
			this.LastSampleState.Clear();
		}

		/// <summary>
		/// Cleans up and shuts down the context.
		/// </summary>
		protected virtual void Shutdown()
		{
			if ( CurrentSample != null )
			{
				CurrentSample.Shutdown();
				CurrentSample = null;
			}

			// remove window event listener before shutting down SIS
			WindowEventMonitor.Instance.UnregisterListener( RenderWindow, this );

			ShutdownInput();
		}

		/// <summary>
		/// Destroys SIS input devices and the input manager.
		/// </summary>
		protected virtual void ShutdownInput()
		{
			if ( this.InputManager != null )
			{
				this.InputManager.DestroyInputObject( this.Keyboard );
				this.InputManager.DestroyInputObject( this.Mouse );

				this.InputManager = null;
			}
		}

		/// <summary>
		/// Captures input device states.
		/// </summary>
		protected virtual void CaptureInputDevices()
		{
			if ( this.Keyboard != null )
				this.Keyboard.Capture();
			if ( this.Mouse != null )
				this.Mouse.Capture();
		}

		/// <summary>
		/// Logs a message to the default logfile.
		/// </summary>
		/// <param name="msg"></param>
		/// <param name="substitutions"></param>
		protected void Log( string msg, params object[] substitutions )
		{
			LogManager.Instance.Write( "SDK : " + msg, substitutions );
		}


		#region IDisposable Members

		public virtual void Dispose()
		{
		}

		#endregion
	}
}