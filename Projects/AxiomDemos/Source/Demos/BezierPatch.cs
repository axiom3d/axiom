#region Namespace Declarations

using System;
using System.ComponentModel.Composition;

using Axiom.Core;
using Axiom.Graphics;
using Axiom.Math;

#endregion Namespace Declarations

namespace Axiom.Demos
{
#if !(WINDOWS_PHONE || XBOX || XBOX360)
	[Export( typeof( TechDemo ) )]
#endif
	public class BezierPatch : TechDemo
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
			public float Nx, Ny, Nz;
			public float U, V;
			public float X, Y, Z;
		}

		#endregion Private Structs

		// --- Protected Override Methods ---

		#region CreateScene()

		// Just override the mandatory create scene method
		public override void CreateScene()
		{
			// Set ambient light
			scene.AmbientLight = new ColorEx( 0.2f, 0.2f, 0.2f );

			// Create point light
			Light light = scene.CreateLight( "MainLight" );

			// Accept default settings: point light, white diffuse, just set position.
			// I could attach the light to a SceneNode if I wanted it to move automatically with
			// other objects, but I don't.
			light.Type = LightType.Directional;
			light.Direction = new Vector3( -0.5f, -0.5f, 0 );

			// Create patch with positions, normals, and 1 set of texcoords
			this.patchDeclaration = HardwareBufferManager.Instance.CreateVertexDeclaration();
			this.patchDeclaration.AddElement( 0, 0, VertexElementType.Float3, VertexElementSemantic.Position );
			this.patchDeclaration.AddElement( 0, 12, VertexElementType.Float3, VertexElementSemantic.Normal );
			this.patchDeclaration.AddElement( 0, 24, VertexElementType.Float2, VertexElementSemantic.TexCoords, 0 );

			// Patch data
			var patchVertices = new PatchVertex[ 9 ];

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

			this.patch = MeshManager.Instance.CreateBezierPatch( "Bezier1", ResourceGroupManager.DefaultResourceGroupName, patchVertices, this.patchDeclaration, 3, 3, 5, 5, VisibleSide.Both, BufferUsage.StaticWriteOnly, BufferUsage.DynamicWriteOnly, true, true );

			// Start patch at 0 detail
			this.patch.Subdivision = 0;

			// Create entity based on patch
			this.patchEntity = scene.CreateEntity( "Entity1", "Bezier1" );
			var material = (Material)MaterialManager.Instance.Create( "TextMat", ResourceGroupManager.DefaultResourceGroupName, null );
			material.GetTechnique( 0 ).GetPass( 0 ).CreateTextureUnitState( "BumpyMetal.jpg" );

			this.patchEntity.MaterialName = "TextMat";
			this.patchPass = material.GetTechnique( 0 ).GetPass( 0 );

			// Attach the entity to the root of the scene
			scene.RootSceneNode.AttachObject( this.patchEntity );

			camera.Position = new Vector3( 500, 500, 1500 );
			camera.LookAt( new Vector3( 0, 200, -300 ) );
		}

		#endregion CreateScene()

		// --- Protected Override Event Handlers ---

		#region bool OnFrameStarted(Object source, FrameEventArgs e)

		// Event handler to add ability to alter subdivision
		protected override void OnFrameRenderingQueued( Object source, FrameEventArgs evt )
		{
			this.timeLapse += evt.TimeSinceLastFrame;

			// Progressively grow the patch
			if ( this.timeLapse > 1.0f )
			{
				this.factor += 0.2f;

				if ( this.factor > 1.0f )
				{
					this.isWireframe = !this.isWireframe;
					this.patchPass.PolygonMode = ( this.isWireframe ? PolygonMode.Wireframe : PolygonMode.Solid );
					this.factor = 0.0f;
				}

				this.patch.Subdivision = this.factor;
				debugText = "Bezier subdivision factor: " + this.factor;
				this.timeLapse = 0.0f;
			}

			// Call default
			base.OnFrameStarted( source, evt );
		}

		#endregion bool OnFrameStarted(Object source, FrameEventArgs e)
	}
}
