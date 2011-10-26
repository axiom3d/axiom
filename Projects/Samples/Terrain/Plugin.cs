using System.ComponentModel.Composition;
using Axiom.Core;

namespace Axiom.Samples.Terrain
{
	[Export(typeof(IPlugin))]
	public class Plugin : SamplePlugin
	{
		private TerrainSample sample;
		public override void Initialize()
		{
			sample = new TerrainSample();
			Name = sample.Metadata[ "Title" ] + " Sample";
			AddSample( sample );
		}
	}
}
