using System;
using System.Collections;
using System.Diagnostics;
using System.Windows.Forms;
using System.Threading;

using Axiom.Core;
using Axiom.Configuration;
using Axiom.Graphics;
using Axiom.Input;
using Chronos.Core;

namespace Chronos.Core
{
	/// <summary>
	/// Summary description for RootManager.
	/// </summary>
	public class RootManager : IDisposable
	{
		#region Private member variables
		private InputReader input;
		private ArrayList addedSearchPaths = new ArrayList();
		private int bpp;
		private bool isSetup = false;
		private bool RootRunning = false;
		private int windowCount;
		private System.Windows.Forms.Timer RootStartTimer;
		private ITimer timer;
		private int MaxFPS;

		#endregion

		#region Singleton implementation

		protected static RootManager instance;

		protected RootManager() 
		{
			RootRunning = false;
			RootStartTimer = new System.Windows.Forms.Timer();
			RootStartTimer.Interval = 100;
			RootStartTimer.Enabled = false;
			RootStartTimer.Tick += new EventHandler(RootStartTimer_Tick);
		}

		public static RootManager Instance 
		{
			get 
			{ 
				return instance; 
			}
		}

		public static void Init() 
		{
			if (instance != null) 
			{
				throw new ApplicationException("RootManager.Instance is null!");
			}
			instance = new RootManager();
			Chronos.GarbageManager.Instance.Add(instance);
		}

		public void Dispose() 
		{
			if (instance == this) 
			{
				instance = null;
			}
		}
		
		#endregion

		#region Private functions
		private void addZipArchives(string path)  
		{
			path = path.Replace(@"\", "/");
			/*string[] dirs = System.IO.Directory.GetDirectories(path);
			foreach(string dir in dirs) 
			{
				addZipArchives(dir);
			}*/
			string[] files = System.IO.Directory.GetFiles(path, "*.zip");
			foreach(string file in files) 
			{
				if(!this.addedSearchPaths.Contains(path)) 
				{
					ResourceManager.AddCommonArchive(file, "ZipFile");
					this.addedSearchPaths.Add(file);
				}
			}
		}

		private void addCommonArchive(string path) 
		{
			/*string[] dirs = System.IO.Directory.GetDirectories(path);
			foreach(string dir in dirs) 
			{
				addCommonArchive(dir);
			}*/
			path = path.Replace(@"\", "/");
			if(!this.addedSearchPaths.Contains(path)) 
			{
				ResourceManager.AddCommonSearchPath(path);
				this.addedSearchPaths.Add(path);
			}
		}

		private void SetupResources() 
		{
			ArrayList l = Utils.getMediaDirectories();
			foreach(string s in l) 
			{
				addZipArchives(s);
				addCommonArchive(s); 
			}
		}

		private void Instance_FrameStarted(object source, FrameEventArgs e)
		{
            //if(!e.RequestShutdown)
            //    e.RequestShutdown = !RootRunning;
		}

		// I'm still using a timer as using a separate thread causes all kinds of
		// thread safety errors the the Root itself. No bueno!
		private void RootStartTimer_Tick(object sender, EventArgs e)
		{
			RootStartTimer.Enabled = false;
			if(!RootRunning) {
				RootRunning = true;
				Setup();
				long frameTime = 1000 / MaxFPS;
				long runTime = 0;
				while(RootRunning) {
					timer.Reset();

					//if(!Root.Instance.OnFrameStarted()) break;
                    Root.Instance.OnFrameStarted();
					Root.Instance.RenderSystem.UpdateAllRenderTargets();
                    Root.Instance.OnFrameEnded();
					//if(!Root.Instance.OnFrameEnded()) break;
					runTime = timer.Milliseconds;

					Application.DoEvents();

					int time = (int)(frameTime - runTime);
					if(time > 0)
						Thread. Sleep(time);
				}
			}
		}

		#endregion

		#region Public functions

		protected void Start() 
		{
			if(RootRunning) return;
			RootRunning = true;
			Setup();
			Root.Instance.StartRendering();
			//RootRunning = false;		// Uncomment if you're not using MaxFPS
		}

		public void RequestStart() 
		{
			RootStartTimer.Enabled = true;
		}

		public void Stop() 
		{
			RootRunning = false;
		}

		public void Setup() 
		{
			if(!isSetup) 
			{
                new Root( "", "chronos.log" );
				SetupResources();			// allow for setting up resource gathering
				// TODO: Select the rendersystem, in this case, DirectX. This needs to be changed.
				Root.Instance.RenderSystem = Root.Instance.RenderSystems[0];
				Root.Instance.Initialize( false );	// setup the Root
				this.bpp = 32;				// TODO: HACK!
				Root.Instance.SceneManager = Root.Instance.SceneManagers.GetSceneManager(SceneType.Generic);
				Root.Instance.FrameStarted += new FrameEvent(Instance_FrameStarted);
				MaxFPS = 100;
				timer = PlatformManager.Instance.CreateTimer();
				isSetup = true;
			}
		}

		public RenderWindow CreateRenderWindow(Control target) 
		{
			Debug.Assert(Root.Instance.RenderSystem != null, "Cannot create a RenderWindow without an active RenderSystem.");

			// create a new render window via the current render system
			RenderWindow rWin = Root.Instance.RenderSystem.CreateRenderWindow(
				"__window" + windowCount.ToString(), 100, 100, bpp, false, 0,
				0, true, false, target);
			if(windowCount == 0) 
			{
				Root.Instance.SceneManager.ShadowTechnique = ShadowTechnique.StencilModulative;

				// set default mipmap level
				TextureManager.Instance.DefaultNumMipMaps = 5;
				// retreive and initialize the input system
				input = PlatformManager.Instance.CreateInputReader();
				input.Initialize(rWin, true, true, false, false);
			}
			windowCount++;
			return rWin;
		}

		#endregion

		#region Properties
		public int BitsPerPixel 
		{
			get { return bpp; }
		}

		public InputReader InputReader 
		{
			get { return input; }
		}
		#endregion

	}
}
