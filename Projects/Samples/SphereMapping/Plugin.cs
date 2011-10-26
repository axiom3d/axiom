using System.ComponentModel.Composition;
using Axiom.Core;

namespace Axiom.Samples.SphereMapping
{
	[Export(typeof(IPlugin))]
	public class Plugin : SamplePlugin
	{
		private SphereMappingSample sample;
		public override void Initialize()
		{
			sample = new SphereMappingSample();
			Name = sample.Metadata[ "Title" ] + " Sample";
			AddSample( sample );
		}
	}
}
