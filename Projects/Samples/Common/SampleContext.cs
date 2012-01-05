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
		virtual public bool IsCurrentSamplePaused
		{
			get
			{
				if( CurrentSample != null )
				{
					return this.IsSamplePaused;
				}
				return false;
			}
		}

		/// <summary>
		/// The Render Window
		/// </summary>
		virtual public RenderWindow RenderWindow { get; protected set; }

		/// <summary>
		/// Current running sample
		/// </summary>
		virtual public Sample CurrentSample { get; protected set; }

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

		~SampleContext() {}

		#endregion

		/// <summary>
		/// Quits the current sample and starts a new one.
		/// </summary>
		/// <param name="s"></param>
		virtual public void RunSample( Sample s )
		{
			if( CurrentSample != null )
			{
				CurrentSample.Shutdown(); // quit current sample
				this.IsSamplePaused = false; // don't pause the next sample
			}

			RenderWindow.RemoveAllViewports(); // wipe viewports

			if( s != null )
			{
				// retrieve sample's required plugins and currently installed plugins
				var ip = PluginManager.Instance.InstalledPlugins;
				IList<String> rp = s.RequiredPlugins;

				string errorMsg = String.Empty;
				foreach( string pluginName in rp )
				{
					bool found = false;
					//try to find the required plugin in the current installed plugins
					foreach( IPlugin plugin in ip )
					{
						//if(plugin.na
						found = true;
						break;
					}

					if( !found ) // throw an exception if a plugin is not found
					{
						String desc = String.Format( "Sample requires plugin: {0}", pluginName );
						this.Log( desc );
						errorMsg += desc + Environment.NewLine;
					}
				}
				if( errorMsg != String.Empty )
				{
					throw new AxiomException( errorMsg );
				}

				// throw an exception if samples requires the use of another renderer
				errorMsg = String.Empty;
				String rrs = s.RequiredRenderSystem;
				if( !String.IsNullOrEmpty( rrs ) && rrs != this.Root.RenderSystem.Name )
				{
					String desc = "Sample only runs with renderer: {0}";
					throw new AxiomException( desc, rrs );
				}

				// test system capabilities against sample requirements
				s.TestCapabilities( this.Root.RenderSystem.HardwareCapabilities );

				s.Setup( RenderWindow, this.Keyboard, this.Mouse ); // start new sample
			}

			CurrentSample = s;
		}

		/// <summary>
		/// This function encapsulates the entire lifetime of the context.
		/// </summary>
		virtual public void Go()
		{
			this.Go( null );
		}

		/// <summary>
		/// This function encapsulates the entire lifetime of the context.
		/// </summary>
		/// <param name="initialSample"></param>
		virtual public void Go( Sample initialSample )
		{
			bool firstRun = true;

			while( !this.IsLastRun )
			{
				this.IsLastRun = true; // assume this is our last run

				CreateRoot();
				if( !OneTimeConfig() )
				{
					return;
				}

				// if the context was reconfigured, set requested renderer
				if( !firstRun )
				{
					this.Root.RenderSystem = this.Root.RenderSystems[ this.NextRenderer ];
				}

				Setup();

				// restore the last sample if there was one or, if not, start initial sample
				if( !firstRun )
				{
					RecoverLastSample();
				}
				else if( initialSample != null )
				{
					RunSample( initialSample );
				}

				this.Root.StartRendering(); // start the render loop

				ConfigurationManager.SaveConfiguration( Root, this.NextRenderer );

				Shutdown();
				if( this.Root != null )
				{
					this.Root.Dispose();
				}
				firstRun = false;
			}
		}

		/// <summary>
		/// Pauses the current running sample
		/// </summary>
		virtual public void PauseCurrentSample()
		{
			if( CurrentSample != null && !this.IsSamplePaused )
			{
				this.IsSamplePaused = true;
				CurrentSample.Paused();
			}
		}

		/// <summary>
		/// Unpauses the current sample
		/// </summary>
		virtual public void UnpauseCurrentSample()
		{
			if( CurrentSample != null && this.IsSamplePaused )
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
		virtual public void FrameStarted( object sender, FrameEventArgs evt )
		{
			CaptureInputDevices(); // capture input
			// manually call sample callback to ensure correct order
			if( CurrentSample != null && !IsSamplePaused )
			{
				CurrentSample.FrameStarted( evt );
			}
		}

		/// <summary>
		/// Processes rendering queued events.
		/// </summary>
		/// <param name="evt"></param>
		/// <returns></returns>
		virtual public void FrameRenderingQueued( object sender, FrameEventArgs evt )
		{
			// manually call sample callback to ensure correct order
			if( CurrentSample != null && !IsSamplePaused )
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
		virtual public void FrameEnded( object sender, FrameEventArgs evt )
		{
			// manually call sample callback to ensure correct order
			if( CurrentSample != null && !IsSamplePaused )
			{
				CurrentSample.FrameEnded( evt );
			}

			// quit if window was closed
			if( RenderWindow.IsClosed )
			{
				evt.StopRendering = true;
			}
			// go into idle mode if current sample has ended
			if( CurrentSample != null && CurrentSample.IsDone )
			{
				RunSample( null );
			}
		}

		/// <summary>
		/// Processes window size change event. Adjusts mouse's region to match that
		/// of the window. You could also override this method to prevent resizing.
		/// </summary>
		/// <param name="rw"></param>
		virtual public void WindowResized( RenderWindow rw )
		{
			// manually call sample callback to ensure correct order
			if( CurrentSample != null && !this.IsSamplePaused )
			{
				CurrentSample.WindowResized( rw );
			}
			if( Mouse != null )
			{
				SIS.MouseState ms = this.Mouse.MouseState;
				ms.Width = rw.Width;
				ms.Height = rw.Height;
			}
		}

		// window event callbacks which manually call their respective sample callbacks to ensure correct order

		virtual public void WindowMoved( RenderWindow rw )
		{
			if( CurrentSample != null && !this.IsSamplePaused )
			{
				CurrentSample.WindowMoved( rw );
			}
		}

		virtual public bool WindowClosing( RenderWindow rw )
		{
			if( CurrentSample != null && !this.IsSamplePaused )
			{
				return CurrentSample.WindowClosing( rw );
			}
			return true;
		}

		virtual public void WindowClosed( RenderWindow rw )
		{
			if( CurrentSample != null && !this.IsSamplePaused )
			{
				CurrentSample.WindowClosed( rw );
			}
		}

		virtual public void WindowFocusChange( RenderWindow rw )
		{
			if( CurrentSample != null && !this.IsSamplePaused )
			{
				CurrentSample.WindowFocusChange( rw );
			}
		}

		// keyboard and mouse callbacks which manually call their respective sample callbacks to ensure correct order

		virtual public bool KeyPressed( SIS.KeyEventArgs evt )
		{
			if( CurrentSample != null && !this.IsSamplePaused )
			{
				return CurrentSample.KeyPressed( evt );
			}
			return true;
		}

		virtual public bool KeyReleased( SIS.KeyEventArgs evt )
		{
			if( CurrentSample != null && !this.IsSamplePaused )
			{
				return CurrentSample.KeyReleased( evt );
			}
			return true;
		}

		virtual public bool MouseMoved( SIS.MouseEventArgs evt )
		{
			if( CurrentSample != null && !this.IsSamplePaused )
			{
				return CurrentSample.MouseMoved( evt );
			}
			return true;
		}

		virtual public bool MousePressed( SIS.MouseEventArgs evt, SIS.MouseButtonID id )
		{
			if( CurrentSample != null && !this.IsSamplePaused )
			{
				return CurrentSample.MousePressed( evt, id );
			}
			return true;
		}

		virtual public bool MouseReleased( SIS.MouseEventArgs evt, SIS.MouseButtonID id )
		{
			if( CurrentSample != null && !this.IsSamplePaused )
			{
				return CurrentSample.MouseReleased( evt, id );
			}
			return true;
		}

		/// <summary>
		/// Creates the Axiom root.
		/// </summary>
		virtual protected void CreateRoot()
		{
			this.Root = new Root( this.ConfigurationManager.LogFilename );
		}

		/// <summary>
		/// Configures the startup settings for Axiom. It will first
		/// load the settings from a configuration file, then open
		/// a dialog for any further configuration.
		/// </summary>
		/// <returns></returns>
		virtual protected bool OneTimeConfig()
		{
			if( this.ConfigurationManager.RestoreConfiguration( this.Root ) )
			{
				return true;
			}

			return this.ConfigurationManager.ShowConfigDialog( this.Root );
		}

		/// <summary>
		/// Sets up the context after configuration.
		/// </summary>
		virtual protected void Setup()
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
		virtual protected void CreateWindow()
		{
			RenderWindow = this.Root.Initialize( true );
		}

		/// <summary>
		/// Sets up SIS input.
		/// </summary>
		virtual protected void SetupInput()
		{
			SIS.ParameterList pl = new SIS.ParameterList();
			pl.Add( new SIS.Parameter( "WINDOW", RenderWindow[ "WINDOW" ] ) );
#if !(XBOX || XBOX360 )
			pl.Add( new SIS.Parameter( "w32_mouse", "CLF_BACKGROUND" ) );
			pl.Add( new SIS.Parameter( "w32_mouse", "CLF_NONEXCLUSIVE" ) );
#endif
			pl.Add( new SIS.Parameter( "x11_keyboard_grab", false ) );
			pl.Add( new SIS.Parameter( "x11_mouse_grab", false ) );
			pl.Add( new SIS.Parameter( "x11_mouse_hide", true ) );

			this.InputManager = SIS.InputManager.CreateInputSystem( pl );

			CreateInputDevices(); // create the specific input devices

			this.WindowResized( RenderWindow ); // do an initial adjustment of mouse area
		}

		/// <summary>
		/// Creates the individual input devices. I only create a keyboard and mouse
		/// here because they are the most common, but you can override this method
		/// for other modes and devices.
		/// </summary>
		virtual protected void CreateInputDevices()
		{
#if !(XBOX || XBOX360 )
			this.Keyboard = this.InputManager.CreateInputObject<SIS.Keyboard>( true, String.Empty );
			this.Mouse = this.InputManager.CreateInputObject<SIS.Mouse>( true, String.Empty );

			this.Keyboard.EventListener = this;
			this.Mouse.EventListener = this;
#endif
		}

		/// <summary>
		/// Finds context-wide resource groups. I load paths from a config file here,
		/// but you can choose your resource locations however you want.
		/// </summary>
		virtual protected void LocateResources() {}

		/// <summary>
		/// Loads context-wide resource groups. I chose here to simply initialise all
		/// groups, but you can fully load specific ones if you wish.
		/// </summary>
		virtual protected void LoadResources()
		{
			ResourceGroupManager.Instance.InitializeAllResourceGroups();
		}

		/// <summary>
		/// Reconfigures the context. Attempts to preserve the current sample state.
		/// </summary>
		/// <param name="renderer"></param>
		/// <param name="options"></param>
		virtual protected void Reconfigure( String renderer, NameValuePairList options )
		{
			// save current sample state
			this.LastSample = CurrentSample;
			if( CurrentSample != null )
			{
				CurrentSample.SaveState( this.LastSampleState );
			}

			this.NextRenderer = renderer;
			Axiom.Graphics.RenderSystem rs = this.Root.RenderSystems[ renderer ];

			// set all given render system options
			foreach( var option in options )
			{
				rs.ConfigOptions[ option.Key ].Value = option.Value;
			}

			this.IsLastRun = false; // we want to go again with the new settings
			this.Root.QueueEndRendering(); // break from render loop
		}

		/// <summary>
		/// Recovers the last sample after a reset. You can override in the case that
		/// the last sample is destroyed in the process of resetting, and you have to
		/// recover it through another means.
		/// </summary>
		virtual protected void RecoverLastSample()
		{
			RunSample( this.LastSample );
			this.LastSample.RestoreState( this.LastSampleState );
			this.LastSample = null;
			this.LastSampleState.Clear();
		}

		/// <summary>
		/// Cleans up and shuts down the context.
		/// </summary>
		virtual protected void Shutdown()
		{
			if( CurrentSample != null )
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
		virtual protected void ShutdownInput()
		{
			if( this.InputManager != null )
			{
				this.InputManager.DestroyInputObject( this.Keyboard );
				this.InputManager.DestroyInputObject( this.Mouse );

				this.InputManager = null;
			}
		}

		/// <summary>
		/// Captures input device states.
		/// </summary>
		virtual protected void CaptureInputDevices()
		{
			if( this.Keyboard != null )
			{
				this.Keyboard.Capture();
			}
			if( this.Mouse != null )
			{
				this.Mouse.Capture();
			}
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

		virtual public void Dispose() {}

		#endregion
	}
}
