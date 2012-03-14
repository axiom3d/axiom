#region Namespace Declarations

using System;
using System.ComponentModel.Composition;

using Axiom.Core;
using Axiom.Graphics;
using Axiom.Math;

#endregion Namespace Declarations

namespace Axiom.Demos
{
	/// <summary>
	/// Summary description for CelShading.
	/// </summary>
#if !(WINDOWS_PHONE || XBOX || XBOX360)
	[Export( typeof( TechDemo ) )]
#endif
	public class CelShading : TechDemo
	{
		#region Constants

		private const int CustomShininess = 1;
		private const int CustomDiffuse = 2;
		private const int CustomSpecular = 3;

		#endregion Constants

		#region Fields

		private SceneNode rotNode;

		#endregion Fields

		public override void CreateScene()
		{
			if ( !Root.Instance.RenderSystem.Capabilities.HasCapability( Capabilities.VertexPrograms ) || !Root.Instance.RenderSystem.Capabilities.HasCapability( Capabilities.FragmentPrograms ) )
			{
				throw new Exception( "Your hardware does not support vertex and fragment programs, so you cannot run this demo." );
			}

			// create a simple default point light
			Light light = scene.CreateLight( "MainLight" );
			light.Position = new Vector3( 20, 80, 50 );

			this.rotNode = scene.RootSceneNode.CreateChildSceneNode();
			this.rotNode.CreateChildSceneNode( new Vector3( 20, 40, 50 ), Quaternion.Identity ).AttachObject( light );

			Entity entity = scene.CreateEntity( "Head", "ogrehead.mesh" );

			camera.Position = new Vector3( 20, 0, 100 );
			camera.LookAt( Vector3.Zero );

			// eyes
			SubEntity subEnt = entity.GetSubEntity( 0 );
			subEnt.MaterialName = "Examples/CelShading";
			subEnt.SetCustomParameter( CustomShininess, new Vector4( 35.0f, 0.0f, 0.0f, 0.0f ) );
			subEnt.SetCustomParameter( CustomDiffuse, new Vector4( 1.0f, 0.3f, 0.3f, 1.0f ) );
			subEnt.SetCustomParameter( CustomSpecular, new Vector4( 1.0f, 0.6f, 0.6f, 1.0f ) );

			// skin
			subEnt = entity.GetSubEntity( 1 );
			subEnt.MaterialName = "Examples/CelShading";
			subEnt.SetCustomParameter( CustomShininess, new Vector4( 10.0f, 0.0f, 0.0f, 0.0f ) );
			subEnt.SetCustomParameter( CustomDiffuse, new Vector4( 0.0f, 0.5f, 0.0f, 1.0f ) );
			subEnt.SetCustomParameter( CustomSpecular, new Vector4( 0.3f, 0.5f, 0.3f, 1.0f ) );

			// earring
			subEnt = entity.GetSubEntity( 2 );
			subEnt.MaterialName = "Examples/CelShading";
			subEnt.SetCustomParameter( CustomShininess, new Vector4( 25.0f, 0.0f, 0.0f, 0.0f ) );
			subEnt.SetCustomParameter( CustomDiffuse, new Vector4( 1.0f, 1.0f, 0.0f, 1.0f ) );
			subEnt.SetCustomParameter( CustomSpecular, new Vector4( 1.0f, 1.0f, 0.7f, 1.0f ) );

			// teeth
			subEnt = entity.GetSubEntity( 3 );
			subEnt.MaterialName = "Examples/CelShading";
			subEnt.SetCustomParameter( CustomShininess, new Vector4( 20.0f, 0.0f, 0.0f, 0.0f ) );
			subEnt.SetCustomParameter( CustomDiffuse, new Vector4( 1.0f, 1.0f, 0.7f, 1.0f ) );
			subEnt.SetCustomParameter( CustomSpecular, new Vector4( 1.0f, 1.0f, 1.0f, 1.0f ) );

			// add entity to the root scene node
			scene.RootSceneNode.CreateChildSceneNode().AttachObject( entity );

			window.GetViewport( 0 ).BackgroundColor = ColorEx.White;
		}

		protected override void OnFrameStarted( object source, FrameEventArgs evt )
		{
			this.rotNode.Yaw( evt.TimeSinceLastFrame * 30 );

			base.OnFrameStarted( source, evt );
		}
	}
}
