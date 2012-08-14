using System;
using System.Collections.Generic;
$if$ ($targetframeworkversion$ >= 3.5)using System.Linq;
$endif$using System.Text;

using Axiom.Core;
using Axiom.Graphics;
using Axiom.Math;

namespace $safeprojectname$
{
	internal class Game
	{
		private Root _engine;
		private RenderWindow _window;
		private SceneManager _scene;
		private Camera _camera;

		public void OnLoad()
		{
			ResourceGroupManager.Instance.AddResourceLocation("media", "Folder", true);

			_scene = _engine.CreateSceneManager("DefaultSceneManager", "DefaultSM");
			_scene.ClearScene();

			_camera = _scene.CreateCamera("MainCamera");

			_camera.Position = new Vector3(0, 10, 200);
			_camera.LookAt(Vector3.Zero);
			_camera.Near = 5;
			_camera.AutoAspectRatio = true;

			var vp = _window.AddViewport(_camera, 0, 0, 1.0f, 1.0f, 100);
			vp.BackgroundColor = ColorEx.CornflowerBlue;

			ResourceGroupManager.Instance.InitializeAllResourceGroups();

		}

		public void CreateScene()
		{
		}

		public void OnUnload()
		{
		}

		public void OnRenderFrame(object s, FrameEventArgs e)
		{
		}

		public void Run()
		{
			using (_engine = new Root())
			{
				_engine.RenderSystem = _engine.RenderSystems[0];
				using (_window = _engine.Initialize(true))
				{
					OnLoad();
					CreateScene();
					_engine.FrameRenderingQueued += OnRenderFrame;
					_engine.StartRendering();
					OnUnload();
				}
			}
		}
	}
}
