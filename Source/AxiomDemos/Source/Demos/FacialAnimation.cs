using System;
using System.ComponentModel.Composition;

using Axiom.Animating;
using Axiom.Controllers;
using Axiom.Controllers.Canned;
using Axiom.Core;
using Axiom.Math;
using Axiom.Graphics;

namespace Axiom.Demos
{
	/// <summary>
	/// 	Summary description for Controllers.
	/// </summary>
	[Export( typeof ( TechDemo ) )]
	public class FacialAnimation : TechDemo
	{
		#region Member variables

		private AnimationState speakAnimState;
		private AnimationState manualAnimState;
		private VertexPoseKeyFrame manualKeyFrame;

		private enum ScrollbarIndex
		{
			Happy = 0,
			Sad = 1,
			Angry = 2,
			A = 3,
			E = 4,
			I = 5,
			O = 6,
			U = 7,
			C = 8,
			W = 9,
			M = 10,
			L = 11,
			F = 12,
			T = 13,
			P = 14,
			R = 15,
			S = 16,
			Th = 17,
			Count = 18
		}

		private string[] scrollbarNames = {
		                                  	"Facial/Happy_Scroll", "Facial/Sad_Scroll", "Facial/Angry_Scroll", "Facial/A_Scroll", "Facial/E_Scroll", "Facial/I_Scroll", "Facial/O_Scroll", "Facial/U_Scroll", "Facial/C_Scroll", "Facial/W_Scroll", "Facial/M_Scroll", "Facial/L_Scroll", "Facial/F_Scroll", "Facial/T_Scroll", "Facial/P_Scroll", "Facial/R_Scroll", "Facial/S_Scroll", "Facial/TH_Scroll",
		                                  };

		private ushort[] poseIndexes = {
		                               	1, 2, 3, 4, 7, 8, 6, 5, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18
		                               };

		#endregion

		#region Methods

		public override void CreateScene()
		{
			// Set ambient light
			scene.AmbientLight = new ColorEx( 0.5f, 0.5f, 0.5f );

			// Create a light
			Light light = scene.CreateLight( "MainLight" );
			// Accept default settings: point light, white diffuse, just set position
			// NB I could attach the light to a SceneNode if I wanted it to move automatically with
			//  other objects, but I don't
			light.Position = new Vector3( 20f, 80f, 50f );
			light.Diffuse = new ColorEx( 1.0f, 1.0f, 1.0f );

			// Create a light
			light = scene.CreateLight( "MainLight2" );
			// Accept default settings: point light, white diffuse, just set position
			// NB I could attach the light to a SceneNode if I wanted it to move automatically with
			//  other objects, but I don't
			light.Position = new Vector3( -120f, -80f, -50f );
			light.Diffuse = new ColorEx( 0.7f, 0.7f, 0.6f );

			// Debugging stuff - - load iraq-f.mesh
			//Mesh iraqf_mesh = MeshManager.Instance.Load("iraq-f.mesh");
			//Entity iraqf = scene.CreateEntity("iraqf", "iraq-f.mesh");

			// Pre-load the mesh so that we can tweak it with a manual animation
			Mesh mesh = (Mesh)MeshManager.Instance.Load( "facial.mesh", ResourceGroupManager.DefaultResourceGroupName );
			Animation anim = mesh.CreateAnimation( "manual", 0 );
			VertexAnimationTrack track = anim.CreateVertexTrack( 4, VertexAnimationType.Pose );
			manualKeyFrame = track.CreateVertexPoseKeyFrame( 0 );
			// create pose references, initially zero
			for ( int i = 0; i < (int)ScrollbarIndex.Count; ++i )
			{
				manualKeyFrame.AddPoseReference( poseIndexes[ i ], 0.0f );
			}

			Entity head = scene.CreateEntity( "Head", "facial.mesh" );
			speakAnimState = head.GetAnimationState( "Speak" );
			speakAnimState.IsEnabled = true;
			manualAnimState = head.GetAnimationState( "manual" );
			manualAnimState.IsEnabled = false;
			manualAnimState.Time = 0;

			SceneNode headNode = scene.RootSceneNode.CreateChildSceneNode();
			headNode.AttachObject( head );

			camera.Position = new Vector3( -20f, 50f, 150f );
			camera.LookAt( new Vector3( 0f, 35f, 0f ) );
		}

		protected override void OnFrameStarted( object source, FrameEventArgs evt )
		{
			speakAnimState.AddTime( evt.TimeSinceLastFrame );
			base.OnFrameStarted( source, evt );
		}

		#endregion
	}
}
