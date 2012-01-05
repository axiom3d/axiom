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

using Axiom.Core;
using Axiom.Animating;
using Axiom.Math;
using Axiom.ParticleSystems;

namespace Axiom.Samples.Core
{
	/// <summary>
	/// 
	/// </summary>
	internal class SmokeSample : SdkSample
	{
		private SceneNode _pivot;

		/// <summary>
		/// 
		/// </summary>
		protected AnimationState animState;

		/// <summary>
		/// 
		/// </summary>
		public SmokeSample()
		{
			Metadata[ "Title" ] = "Smoke";
			Metadata[ "Description" ] = "Demonstrates depth-sorting of particles in particle systems.";
			Metadata[ "Thumbnail" ] = "thumb_smoke.png";
			Metadata[ "Category" ] = "Effects";
			Metadata[ "Help" ] = "Proof that Axiom is just the hottest thing. Bleh. So there. ^_^";
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="evt"></param>
		/// <returns></returns>
		public override bool FrameRenderingQueued( FrameEventArgs evt )
		{
			_pivot.Position = new Vector3( 0, Utility.Sin( Root.Timer.Milliseconds / 150.0f ) * 10, 0 );
			_pivot.Yaw( (Real)( new Degree( (Real)( -evt.TimeSinceLastFrame * 15f ) ) ) );
			return base.FrameRenderingQueued( evt );
		}

		protected override void SetupContent()
		{
			SceneManager.SetSkyBox( true, "Examples/EveningSkyBox", 5000 );

			// dim orange ambient and two bright orange lights to match the skybox
			SceneManager.AmbientLight = new ColorEx( 0.3f, 0.2f, 0.0f );
			Light light = SceneManager.CreateLight( "Light1" );
			light.Position = new Vector3( 2000, 1000, -1000 );
			light.Diffuse = new ColorEx( 1.0f, 0.5f, 0.0f );
			light = SceneManager.CreateLight( "Light2" );
			light.Position = new Vector3( 2000, 1000, 1000 );
			light.Diffuse = new ColorEx( 1.0f, 0.5f, 0.0f );

			_pivot = SceneManager.RootSceneNode.CreateChildSceneNode(); // create a pivot node

			// create a child node and attach ogre head and some smoke to it
			SceneNode headNode = _pivot.CreateChildSceneNode( new Vector3( 100, 0, 0 ) );
			headNode.AttachObject( SceneManager.CreateEntity( "Head", "ogrehead.mesh" ) );
			headNode.AttachObject( ParticleSystemManager.Instance.CreateSystem( "Smoke", "Examples/Smoke" ) );

			Camera.Position = new Vector3( 0.0f, 30.0f, 350.0f );

			base.SetupContent();
		}

		protected override void CleanupContent()
		{
			ParticleSystemManager.Instance.RemoveSystem( "Smoke" );
			base.CleanupContent();
		}
	}
}
