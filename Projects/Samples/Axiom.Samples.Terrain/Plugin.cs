
namespace Axiom.Samples.Terrain
{
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
