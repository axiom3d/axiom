#region MIT/X11 License

//Copyright © 2003-2012 Axiom 3D Rendering Engine Project
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

using System.Collections.Generic;

using Axiom.Core;
using Axiom.Math;
using Axiom.Graphics;

namespace Axiom.Samples.CharacterSample
{
	public class CharacterSample : SdkSample
	{
		protected SinbadCharacterController chara;

		public CharacterSample()
		{
			Metadata[ "Title" ] = "Character";
			Metadata[ "Description" ] = "A demo showing 3rd-person character control and use of TagPoints.";
			Metadata[ "Thumbnail" ] = "thumb_char.png";
			Metadata[ "Category" ] = "Animation";
			Metadata[ "Help" ] = "Use the WASD keys to move Sinbad, and the space bar to jump. " + "Use mouse to look around and mouse wheel to zoom. Press Q to take out or put back " + "Sinbad's swords. With the swords equipped, you can left click to slice vertically or " + "right click to slice horizontally. When the swords are not equipped, press E to " + "start/stop a silly dance routine.";
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="evt"></param>
		/// <returns></returns>
		public override bool FrameRenderingQueued( FrameEventArgs evt )
		{
			// let character update animations and camera
			chara.AddTime( evt.TimeSinceLastFrame );
			return base.FrameRenderingQueued( evt );
		}

		public override bool FrameEnded( FrameEventArgs evt )
		{
			return base.FrameEnded( evt );
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="evt"></param>
		/// <returns></returns>
		public override bool KeyPressed( SharpInputSystem.KeyEventArgs evt )
		{
			// relay input events to character controller
			if ( !TrayManager.IsDialogVisible )
			{
				chara.InjectKeyDown( evt );
			}

			return base.KeyPressed( evt );
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="evt"></param>
		/// <returns></returns>
		public override bool KeyReleased( SharpInputSystem.KeyEventArgs evt )
		{
			if ( !TrayManager.IsDialogVisible )
			{
				chara.InjectKeyUp( evt );
			}

			return base.KeyReleased( evt );
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="evt"></param>
		/// <returns></returns>
		public override bool MouseMoved( SharpInputSystem.MouseEventArgs evt )
		{
			// relay input events to character controller
			if ( !TrayManager.IsDialogVisible )
			{
				chara.InjectMouseMove( evt );
			}

			return base.MouseMoved( evt );
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="evt"></param>
		/// <param name="id"></param>
		/// <returns></returns>
		public override bool MousePressed( SharpInputSystem.MouseEventArgs evt, SharpInputSystem.MouseButtonID id )
		{
			// relay input events to character controller
			if ( !TrayManager.IsDialogVisible )
			{
				chara.InjectMouseDown( evt, id );
			}

			return base.MousePressed( evt, id );
		}

		/// <summary>
		/// 
		/// </summary>
		protected override void SetupContent()
		{
			// set background and some fog
			Viewport.BackgroundColor = new ColorEx( 1.0f, 1.0f, 0.8f );
			SceneManager.SetFog( FogMode.Linear, new ColorEx( 1.0f, 1.0f, 1.0f ), 0, 15, 100 );

			// set shadow properties
			SceneManager.ShadowTechnique = ShadowTechnique.TextureModulative;
			SceneManager.ShadowColor = new ColorEx( 0.5f, 0.5f, 0.5f );
			SceneManager.ShadowTextureSize = 1024;
			SceneManager.ShadowTextureCount = 1;

			// disable default camera control so the character can do its own
			CameraManager.setStyle( CameraStyle.Manual );

			// use a small amount of ambient lighting
			SceneManager.AmbientLight = new ColorEx( 0.3f, 0.3f, 0.3f );

			// add a bright light above the scene
			Light light = SceneManager.CreateLight( "CharacterLight" );
			light.Type = LightType.Point;
			light.Position = new Vector3( -10, 40, 20 );
			light.Specular = ColorEx.White;

			MeshManager.Instance.CreatePlane( "floor", ResourceGroupManager.DefaultResourceGroupName, new Plane( Vector3.UnitY, 0 ), 100, 100, 10, 10, true, 1, 10, 10, Vector3.UnitZ );

			// create a floor entity, give it a material, and place it at the origin
			Entity floor = SceneManager.CreateEntity( "Floor", "floor" );
			floor.MaterialName = "Examples/Rockwall";
			floor.CastShadows = false;
			SceneManager.RootSceneNode.AttachObject( floor );


			// create our character controller
			chara = new SinbadCharacterController( Camera );

			TrayManager.ToggleAdvancedFrameStats();

			List<string> items = new List<string>();
			items.Add( "Help" );
			ParamsPanel help = TrayManager.CreateParamsPanel( TrayLocation.TopLeft, "HelpMessage", 100, items );
			help.SetParamValue( "Help", " H / F1" );
		}

		protected override void CleanupContent()
		{
			if ( chara != null )
			{
				chara = null;
			}

			MeshManager.Instance.Remove( "floor" );
		}
	}
}
