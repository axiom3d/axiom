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

using Axiom.Math;
using Axiom.Core;

namespace Axiom.Samples.Core
{
	/// <summary>
	/// 
	/// </summary>
	class SphereMappingSample : SdkSample
	{
		public SphereMappingSample()
		{
			Metadata[ "Title" ] = "Sphere Mapping";
			Metadata[ "Description" ] = "Shows the sphere mapping feature of materials. " +
				"Sphere maps are not wrapped, and look the same from all directions.";
			Metadata[ "Thumbnail" ] = "thumb_spheremap.png";
			Metadata[ "Category" ] = "Unsorted";
		}

		protected override void SetupContent()
		{
			Viewport.BackgroundColor = ColorEx.White;
			// setup some basic lighting for our scene
			SceneManager.AmbientLight = new ColorEx( 0.3f, 0.3f, 0.3f );
			SceneManager.CreateLight( "SphereMappingSampleLight" ).Position = new Vector3( 20, 80, 50 );
			// set our camera to orbit around the origin and show cursor
			CameraManager.setStyle( CameraStyle.Orbit );
			TrayManager.ShowCursor();

			// create our model, give it the environment mapped material, and place it at the origin
			Entity ent = SceneManager.CreateEntity( "Head", "ogrehead.mesh" );
			ent.MaterialName = "Examples/SphereMappedRustySteel";
			SceneManager.RootSceneNode.AttachObject( ent );
		}
	}
}
