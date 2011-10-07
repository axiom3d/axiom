#region Namespace Declarations

using System;
using System.ComponentModel.Composition;
using Axiom.Core;
using Axiom.ParticleSystems;

#endregion Namespace Declarations

namespace Axiom.Demos
{
	/// <summary>
	/// Summary description for Smoke.
	/// </summary>
#if !WINDOWS_PHONE
    [Export(typeof(TechDemo))]
#endif
    public class Smoke : TechDemo
	{
		public override void CreateScene()
		{
			scene.AmbientLight = ColorEx.Gray;

			scene.SetSkyDome( true, "Examples/CloudySky", 5, 8 );

			ParticleSystem smokeSystem =
				ParticleSystemManager.Instance.CreateSystem( "SmokeSystem", "Examples/Smoke" );

			scene.RootSceneNode.CreateChildSceneNode().AttachObject( smokeSystem );

		}
	}
}