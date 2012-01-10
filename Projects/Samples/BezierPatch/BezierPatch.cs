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

using System;

using Axiom.Samples;
using Axiom.Core;
using Axiom.Graphics;
using Axiom.Math;

namespace Axiom.Samples.BezierPatch
{
	public class BezierSample : SdkSample
	{
		#region Protected Fields

		protected VertexDeclaration patchDeclaration;
		protected float timeLapse;
		protected float factor;
		protected bool isWireframe;
		protected PatchMesh patch;
		protected Entity patchEntity;
		protected Pass patchPass;

		#endregion Protected Fields

		#region Private Structs
		private struct PatchVertex
		{
			public float X, Y, Z;
			public float Nx, Ny, Nz;
			public float U, V;

			public PatchVertex( float x, float y, float z,
				float nx, float ny, float nz,
				float u, float v )
			{
				X = x;
				Y = y;
				Z = z;
				Nx = nx;
				Ny = ny;
				Nz = nz;
				U = u;
				V = v;
			}
		}

		#endregion Private Structs

		public BezierSample()
		{
			Metadata[ "Title" ] = "Bezier Patch";
			Metadata[ "Description" ] = "A demonstration of the Bezier patch support.";
			Metadata[ "Thumbnail" ] = "thumb_bezier.png";
			Metadata[ "Category" ] = "Geometry";
		}

		protected override void SetupContent()
		{
			ResourceGroupManager.Instance.InitializeAllResourceGroups();
			// setup some basic lighting for our scene
			SceneManager.AmbientLight = new ColorEx( 0.5f, 0.5f, 0.5f );
			SceneManager.CreateLight( "BezierLight" ).Position = new Vector3( 100, 100, 100 );

			// define the control point vertices for our patch
			// Patch data
			PatchVertex[] patchVertices = new PatchVertex[ 9 ];

			patchVertices[ 0 ].X = -500;
			patchVertices[ 0 ].Y = 200;
			patchVertices[ 0 ].Z = -500;
			patchVertices[ 0 ].Nx = -0.5f;
			patchVertices[ 0 ].Ny = 0.5f;
			patchVertices[ 0 ].Nz = 0;
			patchVertices[ 0 ].U = 0;
			patchVertices[ 0 ].V = 0;

			patchVertices[ 1 ].X = 0;
			patchVertices[ 1 ].Y = 500;
			patchVertices[ 1 ].Z = -750;
			patchVertices[ 1 ].Nx = 0;
			patchVertices[ 1 ].Ny = 0.5f;
			patchVertices[ 1 ].Nz = 0;
			patchVertices[ 1 ].U = 0.5f;
			patchVertices[ 1 ].V = 0;

			patchVertices[ 2 ].X = 500;
			patchVertices[ 2 ].Y = 1000;
			patchVertices[ 2 ].Z = -500;
			patchVertices[ 2 ].Nx = 0.5f;
			patchVertices[ 2 ].Ny = 0.5f;
			patchVertices[ 2 ].Nz = 0;
			patchVertices[ 2 ].U = 1;
			patchVertices[ 2 ].V = 0;

			patchVertices[ 3 ].X = -500;
			patchVertices[ 3 ].Y = 0;
			patchVertices[ 3 ].Z = 0;
			patchVertices[ 3 ].Nx = -0.5f;
			patchVertices[ 3 ].Ny = 0.5f;
			patchVertices[ 3 ].Nz = 0;
			patchVertices[ 3 ].U = 0;
			patchVertices[ 3 ].V = 0.5f;

			patchVertices[ 4 ].X = 0;
			patchVertices[ 4 ].Y = 500;
			patchVertices[ 4 ].Z = 0;
			patchVertices[ 4 ].Nx = 0;
			patchVertices[ 4 ].Ny = 0.5f;
			patchVertices[ 4 ].Nz = 0;
			patchVertices[ 4 ].U = 0.5f;
			patchVertices[ 4 ].V = 0.5f;

			patchVertices[ 5 ].X = 500;
			patchVertices[ 5 ].Y = -50;
			patchVertices[ 5 ].Z = 0;
			patchVertices[ 5 ].Nx = 0.5f;
			patchVertices[ 5 ].Ny = 0.5f;
			patchVertices[ 5 ].Nz = 0;
			patchVertices[ 5 ].U = 1;
			patchVertices[ 5 ].V = 0.5f;

			patchVertices[ 6 ].X = -500;
			patchVertices[ 6 ].Y = 0;
			patchVertices[ 6 ].Z = 500;
			patchVertices[ 6 ].Nx = -0.5f;
			patchVertices[ 6 ].Ny = 0.5f;
			patchVertices[ 6 ].Nz = 0;
			patchVertices[ 6 ].U = 0;
			patchVertices[ 6 ].V = 1;

			patchVertices[ 7 ].X = 0;
			patchVertices[ 7 ].Y = 500;
			patchVertices[ 7 ].Z = 500;
			patchVertices[ 7 ].Nx = 0;
			patchVertices[ 7 ].Ny = 0.5f;
			patchVertices[ 7 ].Nz = 0;
			patchVertices[ 7 ].U = 0.5f;
			patchVertices[ 7 ].V = 1;

			patchVertices[ 8 ].X = 500;
			patchVertices[ 8 ].Y = 200;
			patchVertices[ 8 ].Z = 800;
			patchVertices[ 8 ].Nx = 0.5f;
			patchVertices[ 8 ].Ny = 0.5f;
			patchVertices[ 8 ].Nz = 0;
			patchVertices[ 8 ].U = 1;
			patchVertices[ 8 ].V = 1;
			// specify a vertex format declaration for our patch: 3 floats for position, 3 floats for normal, 2 floats for UV
			patchDeclaration = HardwareBufferManager.Instance.CreateVertexDeclaration();
			patchDeclaration.AddElement( 0, 0, VertexElementType.Float3, VertexElementSemantic.Position );
			patchDeclaration.AddElement( 0, 12, VertexElementType.Float3, VertexElementSemantic.Normal );
			patchDeclaration.AddElement( 0, 24, VertexElementType.Float2, VertexElementSemantic.TexCoords, 0 );

			// create a patch mesh using vertices and declaration
			patch = MeshManager.Instance.CreateBezierPatch( "patch", ResourceGroupManager.DefaultResourceGroupName, patchVertices, patchDeclaration, 3, 3, 5, 5, VisibleSide.Both, BufferUsage.StaticWriteOnly, BufferUsage.DynamicWriteOnly, true, true );

			// Start patch at 0 detail
			patch.Subdivision = 0;

			// Create entity based on patch
			patchEntity = SceneManager.CreateEntity( "Entity1", "patch" );
			Material material = (Material)MaterialManager.Instance.Create( "TextMat", ResourceGroupManager.DefaultResourceGroupName, null );
			material.GetTechnique( 0 ).GetPass( 0 ).CreateTextureUnitState( "BumpyMetal.jpg" );

			patchEntity.MaterialName = "TextMat";
			patchPass = material.GetTechnique( 0 ).GetPass( 0 );

			// Attach the entity to the root of the scene
			SceneManager.RootSceneNode.AttachObject( patchEntity );

			// save the main pass of the material so we can toggle wireframe on it
			if ( material != null )
			{
				patchPass = material.GetTechnique( 0 ).GetPass( 0 );

				// use an orbit style camera
				CameraManager.setStyle( CameraStyle.Orbit );
				CameraManager.SetYawPitchDist( 0, 0, 250 );

				TrayManager.ShowCursor();

				// create slider to adjust detail and checkbox to toggle wireframe
				Slider slider = TrayManager.CreateThickSlider( TrayLocation.TopLeft, "Detail", "Detail", 120, 44, 0, 1, 6 );
				CheckBox box = TrayManager.CreateCheckBox( TrayLocation.TopLeft, "Wireframe", "Wireframe", 120 );
				slider.SliderMoved += new SliderMovedHandler( slider_SliderMoved );
				box.CheckChanged += new CheckChangedHandler( box_CheckChanged );

			}
		}

		void box_CheckChanged( object sender, CheckBox box )
		{
			patchPass.PolygonMode = ( box.IsChecked ? PolygonMode.Wireframe : PolygonMode.Solid );
		}

		void slider_SliderMoved( object sender, Slider slider )
		{
			patch.Subdivision = slider.Value;
		}

		/// <summary>
		/// 
		/// </summary>
		protected override void CleanupContent()
		{
			patchPass.PolygonMode = PolygonMode.Solid;
			MeshManager.Instance.Remove( patch.Handle );
		}
	}
}
