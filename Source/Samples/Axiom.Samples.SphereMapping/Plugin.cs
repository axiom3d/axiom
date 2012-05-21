using System.ComponentModel.Composition;
using Axiom.Core;

namespace Axiom.Samples.SphereMapping
{
	[Export( typeof ( IPlugin ) )]
	public class Plugin : SamplePlugin
	{
		private SphereMappingSample sample;

		public override void Initialize()
		{
			this.sample = new SphereMappingSample();
			Name = this.sample.Metadata[ "Title" ] + " Sample";
			AddSample( this.sample );
		}
	}
}