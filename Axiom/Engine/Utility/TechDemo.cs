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
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using Axiom.Configuration;
using Axiom.Core;
using Axiom.Enumerations;
using Axiom.Input;
using Axiom.MathLib;
using Axiom.SubSystems.Rendering;

namespace Axiom.Utility
{
	/// <summary>
	/// A base class that can be used to get a head start on writing a game or technical demo using the engine.
	/// </summary>
	public abstract class TechDemo : IDisposable
	{
		protected Engine engine;
		protected Camera camera;
		protected SceneManager sceneMgr;
		protected RenderWindow renderWindow;
		protected InputSystem inputReader;
		protected Vector3 camVec = Vector3.Zero;
		protected float camScale;

		public TechDemo()
		{
			// set the global error handler for this applications thread of excecution.
			System.Windows.Forms.Application.ThreadException += new System.Threading.ThreadExceptionEventHandler(GlobalErrorHandler);

			// add event handlers for frame events
			Engine.Instance.FrameStarted += new FrameEvent(OnFrameStarted);
			Engine.Instance.FrameEnded += new FrameEvent(OnFrameEnded);
		}

		public bool Start()
		{
			if(!Setup())
				return false;

			// start the engines rendering loop
			engine.StartRendering();

//			engine.Shutdown();

			return true;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		virtual protected bool Setup()
		{
			// get a reference to the engine singleton
			engine = Engine.Instance;

			// setup the engine
			engine.Setup();

			// allow for setting up resource gathering
			this.SetupResources();

			//show the config dialog and collect options
			if(!Configure())
				return false;
			
			this.ChooseSceneManager();
			this.CreateCamera();
			this.CreateViewports();

			// call the overridden CreateScene method
			CreateScene();

			// retreive and initialize the input system
			inputReader = engine.InputSystem;
			inputReader.Initialize(renderWindow, null, true, true, false);

			return true;
		}

		/// <summary>
		/// 
		/// </summary>
		protected virtual void SetupResources()
		{
			EngineConfig config = new EngineConfig();
	
			// load the config file
			// relative from the location of debug and releases executables
			config.ReadXml(@"..\..\Media\EngineConfig.xml");

			// interrogate the available resource paths
			foreach(EngineConfig.FilePathRow row in config.FilePath)
			{
				ResourceManager.AddCommonSearchPath(row.src);
			}
		}

		/// <summary>
		/// Configures the application 
		/// </summary>
		/// <returns></returns>
		protected bool Configure()
		{
			// show the config dialog
			if(engine.ShowConfigDialog())
			{
				renderWindow = engine.Initialize(true);
				engine.ShowDebugOverlay(true);
				return true;
			}
			
			// cancel configuration
			return false;
		}

		/// <summary>
		/// 
		/// </summary>
		protected virtual void ChooseSceneManager()
		{
			// Get the SceneManager, a generic one by default
			// REFACTOR: Create SceneManagerFactories and have them register their supported type?
			sceneMgr = engine.SceneManagers[SceneType.Generic];
		}

		/// <summary>
		/// 
		/// </summary>
		protected void CreateCamera()
		{
			// create a camera and initialize its position
			camera = sceneMgr.CreateCamera("MainCamera");
			camera.Position = new Vector3(0, 0, 500);
			camera.LookAt(new Vector3(0, 0, -300));

			// set the near clipping plane to be very close 
			camera.Near = 0.1f;

		}

		/// <summary>
		///		Called to create the default viewports.
		/// </summary>
		virtual protected void CreateViewports()
		{
			Debug.Assert(renderWindow != null, "Attempting to use a null RenderWindow.");

			// create a new viewport and set it's background color
			Viewport viewport = renderWindow.CreateViewport(camera, 0, 0, 100, 100, 100);
			viewport.BackgroundColor = ColorEx.FromColor(System.Drawing.Color.Black);

		}

		/// <summary>
		/// Called to create the scene to be rendered each frame by the renderer.
		/// </summary>
		protected abstract void CreateScene();

		/// <summary>
		///		Used to set up the events for the RenderSystem.  Provides default camera movement behavior
		///		and a few other basic functions, but can be overridden by base classes.  If overridden, the base class
		///		method should be called first. 
		/// </summary>
		protected virtual bool OnFrameStarted(object source, FrameEventArgs e)
		{
			// reset the camera
			camVec.x = 0;
			camVec.y = 0;
			camVec.z = 0;

			// set the scaling of camera motion
			camScale = 100 * e.TimeSinceLastFrame;

			// TODO: Move this into an event queueing mechanism that is processed every frame
			inputReader.Capture();

			if(inputReader.IsKeyPressed(Keys.Escape))
			{
				// returning false from the FrameStart event will cause the engine's render loop to shut down
				Engine.Instance.Shutdown();
			}

			if(inputReader.IsKeyPressed(Keys.A))
				camVec.x = -camScale;

			if(inputReader.IsKeyPressed(Keys.D))
				camVec.x = camScale;

			if(inputReader.IsKeyPressed(Keys.W))
				camVec.z = -camScale;

			if(inputReader.IsKeyPressed(Keys.S))
				camVec.z = camScale;

			if(inputReader.IsKeyPressed(Keys.Left))
				camera.Yaw(camScale);

			if(inputReader.IsKeyPressed(Keys.Right))
				camera.Yaw(-camScale);

			if(inputReader.IsKeyPressed(Keys.Up))
				camera.Pitch(camScale);

			if(inputReader.IsKeyPressed(Keys.Down))
				camera.Pitch(-camScale);

			if(inputReader.IsKeyPressed(Keys.T))
				camera.SceneDetail = SceneDetailLevel.Wireframe;

			if(inputReader.IsKeyPressed(Keys.Y))
				camera.SceneDetail = SceneDetailLevel.Solid;

			if(inputReader.IsKeyPressed(Keys.P))
				TakeScreenshot();

			float camYaw = -inputReader.RelativeMouseX * 0.13f;
			float camPitch = -inputReader.RelativeMouseY * 0.13f;

			camVec.z += -inputReader.RelativeMouseZ * 0.13f;

			camera.Yaw(camYaw);
			camera.Pitch(camPitch);

			// move the camera based on the accumulated movement vector
			camera.MoveRelative(camVec);

			return true;
		}

		/// <summary>
		/// Used to set up the events for the RenderSystem.  Should be overridden by base classes.
		/// </summary>
		protected virtual bool OnFrameEnded(object source, FrameEventArgs e)
		{
			// do nothing by default
			return true;
		}

		/// <summary>
		///		Used to take a screenshot of the current camera view.
		/// </summary>
		protected void TakeScreenshot()
		{
			string[] temp = Directory.GetFiles(Environment.CurrentDirectory, "screenshot*.jpg");

			renderWindow.SaveToFile(String.Format("screenshot{0}.jpg", temp.Length + 1));
		}

		#region Global Error Handling

		/// <summary>
		///		Global error handler to trap any unhandled exceptions.  Exception will be displayed and logged.
		/// </summary>
		/// <param name="source"></param>
		/// <param name="e"></param>
		public static void GlobalErrorHandler(object source, System.Threading.ThreadExceptionEventArgs e)
		{
			// show the error
			MessageBox.Show("An exception has occured.  Please check the log file for more information.\n\nError:\t" + e.Exception.ToString(), "Exception!");

			// log the error
			//System.Diagnostics.Trace.WriteLine(e.Exception.ToString());
		}

		#endregion

		#region Implementation of IDisposable

		/// <summary>
		///		Called to shutdown the engine and all of it's resources.
		/// </summary>
		public void Dispose()
		{
			// ask the engine to dispose of itself
			engine.Dispose();
		}
		#endregion

	}
}
