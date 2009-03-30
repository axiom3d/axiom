using System;
using System.Collections.Generic;
using System.Text;
using Axiom.Core;
using Axiom.Graphics;
using Axiom.Input;

namespace Axiom.Demos
{
	class MultipleSceneManager : TechDemo
	{
		const string CAMERA_NAME = "";

		SceneManager _primary;
		SceneManager _secondary;

		bool _dual = false;

		protected override void ChooseSceneManager()
		{
			_primary = Root.Instance.CreateSceneManager( SceneType.Generic, "primary" );
			_secondary = Root.Instance.CreateSceneManager( SceneType.Generic, "secondary" );
		}

		protected override void CreateScene()
		{
			//Setup the TerrainSceneManager
			_primary.SetSkyDome( true, "Examples/CloudySky", 1, 15 );

			//Setup the Generic SceneManager
			_secondary.SetSkyBox( true, "Skybox/CloudyHills", 150 );

		}

		protected override void CreateCamera()
		{
			_primary.CreateCamera( CAMERA_NAME );
			_secondary.CreateCamera( CAMERA_NAME );
		}

		protected override void CreateViewports()
		{
			SetupViewport( window, _primary );
		}

		private void SetupViewport( RenderWindow window, SceneManager current )
		{
			window.RemoveAllViewports();

			Camera cam = current.GetCamera( CAMERA_NAME );
			Viewport vp = window.AddViewport( cam );

			vp.BackgroundColor = ColorEx.Black;
			cam.AspectRatio = vp.ActualWidth / vp.ActualHeight;

		}

		private void DualViewport( RenderWindow window, SceneManager primary, SceneManager secondary )
		{
			window.RemoveAllViewports();

			Camera cam = primary.GetCamera( CAMERA_NAME );
			Viewport vp = window.AddViewport( cam, 0, 0, 1, 1, 0 );
			vp.BackgroundColor = ColorEx.Black;
			cam.AspectRatio = vp.ActualWidth / vp.ActualHeight;

			cam = secondary.GetCamera( CAMERA_NAME );
			vp = window.AddViewport( cam, 0.6f, 0, 0.4f, 0.4f, 102 );
			vp.BackgroundColor = ColorEx.Black;
			cam.AspectRatio = vp.ActualWidth / vp.ActualHeight;
		}

		private void Swap( ref SceneManager first, ref SceneManager second )
		{
			SceneManager temp = first;
			first = second;
			second = temp;
		}

		protected override bool OnFrameStarted( object source, Axiom.Core.FrameEventArgs e )
		{
			input.Capture();


			if ( input.IsKeyPressed( KeyCodes.Escape ) )
			{
				engine.QueueEndRendering();
				return false;
			}
            else if ( input.IsKeyPressed( KeyCodes.V ) && toggleDelay < 0 )
			{
				_dual = !_dual;

				if ( _dual )
					DualViewport( window, _primary, _secondary );
				else
					SetupViewport( window, _primary );
                toggleDelay = 1000f;
			}

            else if ( input.IsKeyPressed( KeyCodes.C ) && toggleDelay < 0 )
			{
				Swap( ref _primary, ref _secondary );

				if ( _dual )
					DualViewport( window, _primary, _secondary );
				else
					SetupViewport( window, _primary );
                toggleDelay = 1000f;
			}

            toggleDelay -= e.TimeSinceLastEvent;
               

            return true;
		}
	}
}