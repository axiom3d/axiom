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
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using SWF = System.Windows.Forms;

using Axiom.Configuration;
using Axiom.Core;
using Axiom.Graphics;
using Axiom.Input;
using Axiom.Overlays;

using Chess.Coldet;

namespace Chess
{
	/// <summary>
	///     Base class for Axiom examples.
	/// </summary>
	public abstract class Application : IDisposable 
	{
		#region Fields
		#region constants for the names of the widget types
		//const string ImageSetName		= "TaharezLook";
		const string ImageSetName		= "WindowsLook.WL";
		const string GuiSheetType		= "CrayzEdsGui.Base.GuiSheet";
		const string ButtonType			= ImageSetName+"Button";
		const string CheckboxType		= ImageSetName+"Checkbox";
		const string FrameWindowType	= ImageSetName+"FrameWindow";
		const string SliderType			= ImageSetName+"Slider";
		const string RadioButtonType	= ImageSetName+"RadioButton";
		const string ProgressBarType	= ImageSetName+"ProgressBar";
		const string StaticTextType		= ImageSetName+"StaticText";
		const string EditBoxType		= ImageSetName+"EditBox";
		const string VertScrollbarType	= "TaharezLook.TLMiniVerticalScrollbar";
		const string HorzScrollbarType	= "TaharezLook.TLMiniHorizontalScrollbar";
		const string StaticImageType	= "CrayzEdsGui.Base.Widgets.StaticImage";
		const string ComboBoxType		= ImageSetName+"ComboBox";
		const string ListboxType		= ImageSetName+"Listbox";
		const string MultiColumnListType = ImageSetName+"MultiColumnList";

		#endregion Constants

		protected CollisionManager mCollisionManager;
		protected CeGui.Renderer  guiRenderer;  
		protected CeGui.GuiSystem guiSystem;

		protected Root root;
		protected SceneManager sceneManager; 
		protected RenderWindow window;
		protected InputReader input;
		protected Axiom.Math.Vector3 cameraVector = Axiom.Math.Vector3.Zero;
		protected float cameraScale;
		protected bool showDebugOverlay = true;
		protected float statDelay = 0.0f;
		protected float debugTextDelay = 0.0f;
		protected float toggleDelay = 0.0f;
		protected Axiom.Math.Vector3 camVelocity = Axiom.Math.Vector3.Zero;
		protected Axiom.Math.Vector3 camAccel = Axiom.Math.Vector3.Zero;
		protected float camSpeed = 2.5f;
		protected int aniso = 1;
		protected TextureFiltering filtering = TextureFiltering.Bilinear;
        protected string configFile = "EngineConfig.xml";
        protected string logFile = "Engine.log";
        protected string configTitle = ConfigDialog.DefaultTitle;
        protected string configImage = ConfigDialog.DefaultImage;
        protected string renderWindowTitle = "Axiom Render Window";


		#endregion Protected Fields

		#region Constructors
		public Application()
		{

		}
		#endregion

		#region Properties
		public bool ShowDebugOverlay
		{
			get {return showDebugOverlay;}
			set {showDebugOverlay = value;}
		}
		#endregion

		#region Protected Methods
		public SceneManager SceneManager 
		{
			get 
			{
				return sceneManager;
			}
		}
		public RenderWindow RenderWindow 
		{
			get {return window;}
		}
		public CeGui.Renderer GUIRenderer
		{
			get {return guiRenderer;}
		}

		public CeGui.GuiSystem getGUISystem
		{
			get {return guiSystem;}
		}

		protected bool Configure() 
		{
			// HACK: Temporary
            ConfigDialog dlg = new ConfigDialog( configTitle, configImage );
            SWF.DialogResult result = dlg.ShowDialog();
            if ( result == SWF.DialogResult.Cancel )
                return false;


			//RenderDebugOverlay(showDebugOverlay);
			
			return true;
		}



		/// <summary>
		///    Shows the debug overlay, which displays performance statistics.
		/// </summary>
		protected virtual void RenderDebugOverlay(bool show) 
		{
			// gets a reference to the default overlay
			Overlay o = OverlayManager.Instance.GetByName("Core/DebugOverlay");

			if(o == null) 
			{
				throw new Exception(string.Format("Could not find overlay named '{0}'.", "Core/DebugOverlay"));
			}

			if(show) 
			{
				o.Show();
			}
			else 
			{
				o.Hide();
			}
		}

		public void TakeScreenshot(string fileName) 
		{
			window.Save(fileName);
		}

		#endregion Protected Methods

		#region Protected Virtual Methods

		protected virtual void ChooseSceneManager() 
		{
			// Get the SceneManager, a generic one by default
			sceneManager = root.SceneManagers.GetSceneManager(SceneType.Generic);
		}

		protected virtual void CreateViewports() 
		{

		}

		protected virtual bool Initialize() 
		{
			// instantiate the Root singleton
			root = new Root( configFile, logFile );

			// add event handlers for frame events
			root.FrameStarted += new FrameEvent(OnFrameStarted);
			root.FrameEnded += new FrameEvent(OnFrameEnded);

			// allow for setting up resource gathering
			SetupResources();

			//show the config dialog and collect options
			if(!Configure()) 
			{
				// shutting right back down
				root.Shutdown();
                
				return false;
			}
            window = Root.Instance.Initialize( true, renderWindowTitle );

			ChooseSceneManager();


			// set default mipmap level
			TextureManager.Instance.DefaultNumMipMaps = 5;

			// retreive and Initialize the input system
			input = PlatformManager.Instance.CreateInputReader();
			//input = new Chess.Main.InputCustom();
			input.Initialize(window, true, true, false, true);

			// CeGui setup
			CreateCeGui();
	
			// create a new collision manager
			mCollisionManager = new CollisionManager();
			return true;
		}

		private void CreateCeGui()
		{
			// Create Axiom Renderer
			guiRenderer = new CeGui.Renderers.Axiom.Renderer(window, RenderQueueGroupID.Overlay, false);

			// init the subsystem singleton
			guiSystem = new CeGui.GuiSystem(guiRenderer);

			CeGui.Logger.Instance.LoggingLevel = CeGui.LoggingLevel.Errors;

            loadCeGuiResources();

            // max window size, based on the size of the Axiom window
			Size maxSize = new Size(window.Width, window.Height);

			// configure the default mouse cursor
			CeGui.GuiSystem.Instance.SetDefaultMouseCursor("TaharezLook", "MouseArrow");

			// programatically Create a default font bitmap
			CeGui.Font defaultFont = CeGui.FontManager.Instance.CreateFont("Tahoma-10", "Tahoma", 10, CeGui.FontFlags.None);

			// configure the default font
			CeGui.GuiSystem.Instance.SetDefaultFont(defaultFont);

			// Create a root window, and set it as the current gui sheet.
            CeGui.GuiSystem.Instance.GuiSheet = CeGui.WindowManager.Instance.CreateWindow( "DefaultWindow", "root_wnd" );

            CeGui.MouseCursor.Instance.SetImage( "TaharezLook", "MouseArrow" );
            CeGui.MouseCursor.Instance.Show();
		}

        /// <summary>Loads dynamic resources</summary>
        private static void loadCeGuiResources()
        {

            // Widget sets are collections of widgets that provide the widget classes defined
            // in CeGui (like PushButton, CheckBox and so on) with their own distinctive look
            // (like a theme) and possibly even custom behavior.
            //
            // Here we load all compiled widget sets we can find in the current directory. This
            // is done to demonstrate how you could add widget set dynamically to your
            // application. Other possibilities would be to hardcode the widget set an
            // application uses or determining the assemblies to load from a configuration file.
            string[] assemblyFiles = System.IO.Directory.GetFiles(
              System.IO.Directory.GetCurrentDirectory(), "CeGui.WidgetSets.*.dll"
            );
            foreach ( string assemblyFile in assemblyFiles )
            {
                CeGui.WindowManager.Instance.AttachAssembly(
                  System.Reflection.Assembly.LoadFile( assemblyFile )
                );
            }

            // Imagesets are a collection of named areas within a texture or image file. Each
            // area becomes an Image, and has a unique name by which it can be referenced. Note
            // that an Imageset would normally be specified as part of a scheme file, although
            // as this example is demonstrating, it is not a requirement.
            //
            // Again, we load all image sets we can find, this time searching the resources folder.
            string[] imageSetFiles = System.IO.Directory.GetFiles(
              System.IO.Directory.GetCurrentDirectory() + @"\Media\Gui", "*.imageset"
            );
            foreach ( string imageSetFile in imageSetFiles )
                CeGui.ImagesetManager.Instance.CreateImageset( imageSetFile );

        }

		/// <summary>
		///		Loads default resource configuration if one exists.
		/// </summary>
		protected virtual void SetupResources() 
		{
			string resourceConfigPath = Path.GetFullPath("EngineConfig.xml");

			if(File.Exists(resourceConfigPath)) 
			{
				EngineConfig config = new EngineConfig();

				// load the config file
				// relative from the location of debug and releases executables
				config.ReadXml("EngineConfig.xml");

				// interrogate the available resource paths
				foreach(EngineConfig.FilePathRow row in config.FilePath) 
				{
					ResourceManager.AddCommonArchive(row.src, row.type);
				}
			}
		}

		#endregion Protected Virtual Methods

		#region Protected Abstract Methods

		/// <summary>
		/// 
		/// </summary>
		protected abstract void CreateScene();

		#endregion Protected Abstract Methods

		#region Public Methods
		public virtual void Delete()
		{
			mCollisionManager.Delete();
			this.guiSystem = null; 
			this.guiRenderer = null;  
			this.root = null;   
		}

		protected virtual void PreStartRendering()
		{

		}

		public void Run() 
		{
			try 
			{
				if (Initialize()) 
				{
					PreStartRendering();
					// start the engines rendering loop
					root.StartRendering();
				}
			}
			catch (Exception ex) 
			{
				// try logging the error here first, before Root is disposed of
				if (LogManager.Instance != null) 
				{
					LogManager.Instance.Write(ex.ToString());
				}
			}
		}

		public void Dispose() 
		{
			if(root != null) 
			{
				// remove event handlers
				root.FrameStarted -= new FrameEvent(OnFrameStarted);
				root.FrameEnded -= new FrameEvent(OnFrameEnded);

				root.Dispose();
			}
		}

		#endregion Public Methods

		#region Event Handlers
		protected virtual void OnFrameStarted(Object source, FrameEventArgs e) 
		{
		}
		protected virtual void OnFrameEnded(Object source, FrameEventArgs e) 
		{
		}

		public virtual void UpdateStats() 
		{
		}

		#endregion Event Handlers
	}
}
