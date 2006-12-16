#region LGPL License
/*
Chronos World Editor
Copyright (C) 2004 Chris "Antiarc" Heald [antiarc@antiarc.net]

This application is free software; you can redistribute it and/or
modify it under the terms of the GNU Lesser General Public
License as published by the Free Software Foundation; either
version 2.1 of the License, or (at your option) any later version.

This application is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
Lesser General Public License for more details.

You should have received a copy of the GNU Lesser General Public
License along with this library; if not, write to the Free Software
Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
*/
#endregion

using System;
using System.Diagnostics;
using System.Collections;
using System.Configuration;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Xml;

using Axiom.Core;
using Axiom.Math.Collections;

using Crownwood.Magic.Common;
using Crownwood.Magic.Controls;
using Crownwood.Magic.Docking;

using Chronos.Core;
using Chronos.Diagnostics;

namespace Chronos
{
	/// <summary>
	/// Summary description for MainForm.
	/// </summary>
	public class MainForm : System.Windows.Forms.Form
	{
		#region Fields
		public static MainForm main;
		public static string[] cmdLineParams;

		private System.Windows.Forms.ImageList imageList1;
		private System.Windows.Forms.SaveFileDialog saveFileDialog1;
		private System.ComponentModel.IContainer components;

		public DockingManager _manager;
		private SplashScreen splash = new SplashScreen();
		private System.Windows.Forms.ImageList pluginImages;
		private TD.SandBar.SandBarManager sandBarManager1;
		private TD.SandBar.ToolBarContainer leftSandBarDock;
		private TD.SandBar.ToolBarContainer rightSandBarDock;
		private TD.SandBar.ToolBarContainer bottomSandBarDock;
		private TD.SandBar.ToolBarContainer topSandBarDock;
		private TD.SandBar.MenuBar menuBar1;
		private TD.SandBar.MenuBarItem fileMenu;
		private TD.SandBar.MenuButtonItem newWorldItem;
		private TD.SandBar.MenuButtonItem openWorldItem;
		private TD.SandBar.MenuButtonItem saveWorldItem;
		private TD.SandBar.MenuButtonItem saveWorldAsItem;
		private TD.SandBar.MenuButtonItem configItem;
		private TD.SandBar.MenuButtonItem exitItem;
		private TD.SandBar.MenuBarItem pluginsMenu;
		private TD.SandBar.MenuBarItem helpMenu;
		private TD.SandBar.MenuButtonItem aboutEditorItem;
		private TD.SandBar.MenuButtonItem menuItem1;
		private DocumentManager.DocumentManager documentManager1;
		private TD.SandBar.MenuButtonItem menuButtonItem2;
		private TD.SandBar.MenuBar menuBar2;
		private TD.SandBar.MenuBarItem menuBarItem1;

		private bool shutting_down = false;
		#endregion

		public MainForm()
		{
			_manager = new DockingManager(this, VisualStyle.IDE);
			InitializeComponent();
		}

		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if(components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(MainForm));
			this.imageList1 = new System.Windows.Forms.ImageList(this.components);
			this.saveFileDialog1 = new System.Windows.Forms.SaveFileDialog();
			this.pluginImages = new System.Windows.Forms.ImageList(this.components);
			this.sandBarManager1 = new TD.SandBar.SandBarManager();
			this.bottomSandBarDock = new TD.SandBar.ToolBarContainer();
			this.leftSandBarDock = new TD.SandBar.ToolBarContainer();
			this.rightSandBarDock = new TD.SandBar.ToolBarContainer();
			this.topSandBarDock = new TD.SandBar.ToolBarContainer();
			this.menuBar1 = new TD.SandBar.MenuBar();
			this.fileMenu = new TD.SandBar.MenuBarItem();
			this.newWorldItem = new TD.SandBar.MenuButtonItem();
			this.menuButtonItem2 = new TD.SandBar.MenuButtonItem();
			this.openWorldItem = new TD.SandBar.MenuButtonItem();
			this.saveWorldItem = new TD.SandBar.MenuButtonItem();
			this.saveWorldAsItem = new TD.SandBar.MenuButtonItem();
			this.configItem = new TD.SandBar.MenuButtonItem();
			this.exitItem = new TD.SandBar.MenuButtonItem();
			this.pluginsMenu = new TD.SandBar.MenuBarItem();
			this.menuBarItem1 = new TD.SandBar.MenuBarItem();
			this.helpMenu = new TD.SandBar.MenuBarItem();
			this.aboutEditorItem = new TD.SandBar.MenuButtonItem();
			this.menuItem1 = new TD.SandBar.MenuButtonItem();
			this.documentManager1 = new DocumentManager.DocumentManager();
			this.menuBar2 = new TD.SandBar.MenuBar();
			this.topSandBarDock.SuspendLayout();
			this.SuspendLayout();
			// 
			// imageList1
			// 
			this.imageList1.ImageSize = new System.Drawing.Size(16, 16);
			//this.imageList1.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageList1.ImageStream")));
			this.imageList1.TransparentColor = System.Drawing.Color.Transparent;
			// 
			// saveFileDialog1
			// 
			this.saveFileDialog1.Filter = "Scene Files (*.scene)|*.scene|All files (*.*)|*.*";
			// 
			// pluginImages
			// 
			this.pluginImages.ColorDepth = System.Windows.Forms.ColorDepth.Depth16Bit;
			this.pluginImages.ImageSize = new System.Drawing.Size(16, 16);
			//this.pluginImages.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("pluginImages.ImageStream")));
			this.pluginImages.TransparentColor = System.Drawing.Color.Transparent;
			// 
			// sandBarManager1
			// 
			this.sandBarManager1.BottomContainer = this.bottomSandBarDock;
			this.sandBarManager1.LeftContainer = this.leftSandBarDock;
			this.sandBarManager1.OwnerForm = this;
			this.sandBarManager1.RightContainer = this.rightSandBarDock;
			this.sandBarManager1.TopContainer = this.topSandBarDock;
			// 
			// bottomSandBarDock
			// 
			this.bottomSandBarDock.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.bottomSandBarDock.Location = new System.Drawing.Point(0, 451);
			this.bottomSandBarDock.Manager = this.sandBarManager1;
			this.bottomSandBarDock.Name = "bottomSandBarDock";
			this.bottomSandBarDock.Size = new System.Drawing.Size(632, 0);
			this.bottomSandBarDock.TabIndex = 9;
			// 
			// leftSandBarDock
			// 
			this.leftSandBarDock.Dock = System.Windows.Forms.DockStyle.Left;
			this.leftSandBarDock.Location = new System.Drawing.Point(0, 24);
			this.leftSandBarDock.Manager = this.sandBarManager1;
			this.leftSandBarDock.Name = "leftSandBarDock";
			this.leftSandBarDock.Size = new System.Drawing.Size(0, 427);
			this.leftSandBarDock.TabIndex = 7;
			// 
			// rightSandBarDock
			// 
			this.rightSandBarDock.Dock = System.Windows.Forms.DockStyle.Right;
			this.rightSandBarDock.Location = new System.Drawing.Point(632, 24);
			this.rightSandBarDock.Manager = this.sandBarManager1;
			this.rightSandBarDock.Name = "rightSandBarDock";
			this.rightSandBarDock.Size = new System.Drawing.Size(0, 427);
			this.rightSandBarDock.TabIndex = 8;
			// 
			// topSandBarDock
			// 
			this.topSandBarDock.Controls.Add(this.menuBar1);
			this.topSandBarDock.Dock = System.Windows.Forms.DockStyle.Top;
			this.topSandBarDock.Location = new System.Drawing.Point(0, 0);
			this.topSandBarDock.Manager = this.sandBarManager1;
			this.topSandBarDock.Name = "topSandBarDock";
			this.topSandBarDock.Size = new System.Drawing.Size(632, 24);
			this.topSandBarDock.TabIndex = 10;
			// 
			// menuBar1
			// 
			this.menuBar1.Buttons.AddRange(new TD.SandBar.ToolbarItemBase[] {
																				this.fileMenu,
																				this.pluginsMenu,
																				this.menuBarItem1,
																				this.helpMenu});
			this.menuBar1.Guid = new System.Guid("cf125475-f8c4-45e3-9190-a334af3c1575");
			this.menuBar1.IsOpen = true;
			this.menuBar1.Location = new System.Drawing.Point(2, 0);
			this.menuBar1.Name = "menuBar1";
			this.menuBar1.Size = new System.Drawing.Size(630, 24);
			this.menuBar1.TabIndex = 0;
			// 
			// fileMenu
			// 
			this.fileMenu.MenuItems.AddRange(new TD.SandBar.MenuButtonItem[] {
																				 this.newWorldItem,
																				 this.menuButtonItem2,
																				 this.openWorldItem,
																				 this.saveWorldItem,
																				 this.saveWorldAsItem,
																				 this.configItem,
																				 this.exitItem});
			this.fileMenu.Text = "&File";
			// 
			// newWorldItem
			// 
			this.newWorldItem.Text = "&New Project...";
			this.newWorldItem.Activate += new System.EventHandler(this.newWorldItem_Activate);
			// 
			// menuButtonItem2
			// 
			this.menuButtonItem2.Text = "New &Scene";
			// 
			// openWorldItem
			// 
			this.openWorldItem.BeginGroup = true;
			this.openWorldItem.Text = "&Open Project...";
			// 
			// saveWorldItem
			// 
			this.saveWorldItem.BeginGroup = true;
			this.saveWorldItem.Text = "&Save Project";
			// 
			// saveWorldAsItem
			// 
			this.saveWorldAsItem.Text = "Save Project &As";
			// 
			// configItem
			// 
			this.configItem.BeginGroup = true;
			this.configItem.Text = "&Configure...";
			// 
			// exitItem
			// 
			this.exitItem.BeginGroup = true;
			this.exitItem.Text = "E&xit";
			this.exitItem.Activate += new System.EventHandler(this.exitItem_Click);
			// 
			// pluginsMenu
			// 
			this.pluginsMenu.Text = "&Plugins";
			// 
			// menuBarItem1
			// 
			this.menuBarItem1.MdiWindowList = true;
			this.menuBarItem1.ShowIconsOnMdiWindowList = true;
			this.menuBarItem1.Text = "&Window";
			// 
			// helpMenu
			// 
			this.helpMenu.MenuItems.AddRange(new TD.SandBar.MenuButtonItem[] {
																				 this.aboutEditorItem,
																				 this.menuItem1});
			this.helpMenu.Text = "&Help";
			// 
			// aboutEditorItem
			// 
			this.aboutEditorItem.Text = "&About Ogre Editor...";
			this.aboutEditorItem.Activate += new System.EventHandler(this.aboutEditorItem_Click);
			// 
			// documentManager1
			// 
			this.documentManager1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.documentManager1.Location = new System.Drawing.Point(0, 24);
			this.documentManager1.Name = "documentManager1";
			this.documentManager1.Size = new System.Drawing.Size(632, 427);
			this.documentManager1.TabIndex = 11;
			this.documentManager1.TabStripBackColor = System.Drawing.SystemColors.Control;
			this.documentManager1.UseCustomTabStripBackColor = true;
			this.documentManager1.CloseButtonPressed += new DocumentManager.DocumentManager.CloseButtonPressedEventHandler(this.documentManager1_CloseButtonPressed);
			// 
			// menuBar2
			// 
			this.menuBar2.DockLine = 1;
			this.menuBar2.Guid = new System.Guid("e2e83650-2203-4fed-859c-f862f12ab1fb");
			this.menuBar2.IsOpen = true;
			this.menuBar2.Location = new System.Drawing.Point(0, 0);
			this.menuBar2.Name = "menuBar2";
			this.menuBar2.Size = new System.Drawing.Size(632, 24);
			this.menuBar2.Stretch = false;
			this.menuBar2.TabIndex = 13;
			this.menuBar2.Text = "menuBar2";
			// 
			// MainForm
			// 
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.ClientSize = new System.Drawing.Size(632, 451);
			this.Controls.Add(this.documentManager1);
			this.Controls.Add(this.leftSandBarDock);
			this.Controls.Add(this.rightSandBarDock);
			this.Controls.Add(this.bottomSandBarDock);
			this.Controls.Add(this.topSandBarDock);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.ImeMode = System.Windows.Forms.ImeMode.On;
			this.IsMdiContainer = true;
			this.MinimumSize = new System.Drawing.Size(640, 480);
			this.Name = "MainForm";
			this.Text = "Chronos Editor";
			this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
			this.Closing += new System.ComponentModel.CancelEventHandler(this.MainForm_Closing);
			this.Load += new System.EventHandler(this.MainForm_Load);
			this.topSandBarDock.ResumeLayout(false);
			this.ResumeLayout(false);

		}
		#endregion

		private void MainForm_Load(object sender, System.EventArgs e) {
			this.Hide();
			splash.Show();
			Application.DoEvents();

			// Init singletons

			Chronos.Diagnostics.SystemLogRecorder.StartRecording();
			
			splash.setStatus("Initializing GUI manager...");
			Toolbox toolbox = new Toolbox();
			GuiManager.Init(this, _manager, toolbox.outlookBar1, this.documentManager1, this.topSandBarDock);
			
			splash.setStatus("Initializing logging services...");
			LoggingWindow logForm = new LoggingWindow();
			logForm.Text = "Logs";
			GuiManager.Instance.CreateDockingWindow(logForm);
			System.Diagnostics.Trace.Listeners.Add(new AxiomLogListener());

			splash.setStatus("Initializing garbage manager...");
			Chronos.GarbageManager g = Chronos.GarbageManager.Instance;

			splash.setStatus("Initializing Root...");
			RootManager.Init();
			RootManager.Instance.Setup();

			// Create a dummy window, mostly so that hardware caps get set up.
			// This is ugly, but necessary.
			splash.setStatus("Setting up rendering environment...");
			RootManager.Instance.CreateRenderWindow(new PictureBox());

			// We're gonna cheat a bit with this one plugin, since its starting up
			// is so integral to the rest of the process.
			ViewportPlugin.Init();

			// Load the start screen instead of a renderer.
			StartPage sp = new StartPage();
			DocumentEventHook startPageHook = 
				GuiManager.Instance.CreateDocument(sp, "Start Page");
			startPageHook.Closing += new Chronos.Core.DocumentEventHook.DocumentEventArgsDelegate(startPageHook_Closing);

			splash.setStatus("Initializing scene graph...");
			CommonSetup.Setup();

			Root.Instance.FrameStarted += new FrameEvent(Instance_FrameStarted);
			
			splash.setStatus("Setting up toolbox...");
			GuiManager.Instance.CreateDockingWindow(toolbox);

			splash.setStatus("Loading plugins...");
			LoadPlugins();

			GuiManager.Instance.WireUpPostPluginEvents();

			splash.setStatus("Loading layout...");
			LoadWindowLayout();

			splash.setStatus("Starting rendering...");
			RootManager.Instance.RequestStart();
			splash.Close();
			this.Show();

			if(cmdLineParams.Length > 0) {
				Chronos.Diagnostics.Log.WriteLine(Logs.Status, "Loading " + cmdLineParams[0] + "...");
				EditorSceneManager.Instance.LoadScene(cmdLineParams[0]);
			}

			Chronos.Diagnostics.Log.WriteLine(Logs.Status, "Startup done!");
		}

		private void LoadPlugin(XmlTextReader reader)
        {
            IEditorPlugin plugin;

            // Read some attributes.
            string typeName = reader.GetAttribute("type");
            if(typeName == string.Empty) {
                throw new XmlException("Missing required attribute, 'type'.");
            }

			string fileName = reader.GetAttribute("path");
            if(fileName == string.Empty) {
                throw new XmlException("Missing required attribute, 'path'.");
            }

            fileName = Path.GetFullPath(fileName);

            // Load the plugin.
            try {
                plugin = Chronos.Core.PluginManager.Instance.Load(fileName, typeName, true); 
				if (splash.Visible) splash.setStatus("Loading plugin: (name)");
            } catch(Exception e) {
                StringBuilder sb = new StringBuilder();
                sb.AppendFormat("Error: {0}", e.Message);
                if(e.InnerException != null) {
                    sb.AppendFormat("\nReason: {0}", e.InnerException.Message);
                }

                System.Diagnostics.Trace.Write(sb.ToString());
                return;
            }

            // Start the plugin.
            try {
    		    plugin.Start();
            } catch(Exception e) {
				// Todo: Fix removing
                Chronos.Core.PluginManager.Instance.Remove("dummy");

                StringBuilder sb = new StringBuilder();
                sb.AppendFormat("[{0}] Error: {1}", Path.GetFileName(fileName), e.Message);
                if(e.InnerException != null) {
                    sb.AppendFormat("\nReason: {0}", e.InnerException.Message);
                }
				Chronos.Diagnostics.Log.WriteError(sb.ToString());
                return;
            }

            // Add the plugin to the menu.
		    //MenuItem pluginMenu = pluginsMenu.MenuItems.Add(plugin.Name);
        }

		private void LoadPlugins() 
		{
			string fileName = ConfigurationSettings.AppSettings["editorPluginsFile"];

            if(!File.Exists(fileName)) {
                string message = "The file, " + fileName + ", does not exist.";
				System.Diagnostics.Trace.Write(message);
                return;
            }
		
            // Load config from XML
			XmlTextReader reader = null;
			try {
				reader = new XmlTextReader(fileName);
				while(reader.Read()) {
					if(reader.NodeType == XmlNodeType.Element) {
						if(reader.LocalName.ToLower().Equals("plugin")) {
                            LoadPlugin(reader);
                        }
                    }
                }
			} finally {
				if(reader != null)
					reader.Close();
			}
		}

		private void LoadWindowLayout() 
		{
			try 
			{
				if(System.IO.File.Exists(Path.Combine(Application.StartupPath, "ChronosLayout.xml")))
					_manager.LoadConfigFromFile(Path.Combine(Application.StartupPath, "ChronosLayout.xml"));
			} 
			catch (Exception except) 
			{
				System.Diagnostics.Trace.Write("Could not parse layout XML file: " + except.Message + " - trying default.");
				try 
				{
					if(System.IO.File.Exists(Path.Combine(Application.StartupPath, "DefaultEditorLayout.xml")))
						_manager.LoadConfigFromFile(Path.Combine(Application.StartupPath, "DefaultEditorLayout.xml"));
				}
				catch (Exception defexcept) 
				{
					System.Diagnostics.Trace.Write("Could not parse default layout XML file: " + defexcept.Message);
				}
			}
		}

		[STAThread]
		static void Main(string[] args) 
		{
			cmdLineParams = args;
			main = new MainForm();
			Application.Run(main);
		}

		private void MainForm_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			// TODO: Update this once serialization is in.
			bool needToSave = false;
			if(needToSave) 
			{
				DialogResult d = MessageBox.Show(this, "Do you wish to save the current world?", this.Text, MessageBoxButtons.YesNoCancel);
				if(d == DialogResult.Yes) 
				{
					// Execute save
				} 
				if(d != DialogResult.Cancel) 
				{
					_manager.SaveConfigToFile(Path.Combine(Application.StartupPath, "ChronosLayout.xml"));
					shutting_down = true;
					Root.Instance.Shutdown();
					e.Cancel = false;
					Application.Exit();
				}
			} 
			else 
			{
				_manager.SaveConfigToFile(Path.Combine(Application.StartupPath, "ChronosLayout.xml"));
				shutting_down = true;
				Root.Instance.Shutdown();
				e.Cancel = false;
			}
			RootManager.Instance.Stop();
			foreach(Form f in this.MdiChildren) 
			{
				f.Close();
			}
			Application.Exit();
		}

		private void aboutEditorItem_Click(object sender, System.EventArgs e)
		{
			ArrayList a = new ArrayList();
			a.Add("Chronos World Editor v 0.2.0");
			a.Add("Send questions/suggestions/bug reports to chronos@digitalsentience.com");
			string str = "";
			foreach(string s in a)
				str += s + "\n";
			MessageBox.Show (str);
		}

		private void exitItem_Click(object sender, System.EventArgs e)
		{
			RootManager.Instance.Stop();
			this.Close();
		}

		private void Instance_FrameStarted(object source, FrameEventArgs e)
		{
            //if(!e.RequestShutdown)
            //    e.RequestShutdown = shutting_down;
		}

		private void documentManager1_CloseButtonPressed(object sender, DocumentManager.CloseButtonPressedEventArgs e) {
			(e.TabStrip.SelectedDocument as Chronos.Core.ClosableDocument).Close(sender);
		}

		private void d_Closing(object sender, ref DocumentEventArgs e) {
			e.Cancel = true; // Don't allow this document to be closed.
		}

		private void newWorldItem_Activate(object sender, System.EventArgs e) {
		
		}

		private void startPageHook_Closing(object sender, ref DocumentEventArgs e) {
			e.Cancel = true;
		}
	}

	public class AxiomLogListener : TraceListener {
		public AxiomLogListener() {
		}

		public override void Write(string message) {
			Chronos.Diagnostics.Log.Write(Logs.Axiom, message);
		}

		public override void WriteLine(string message) {
			Chronos.Diagnostics.Log.WriteLine(Logs.Axiom, message);
		}
	}
}
