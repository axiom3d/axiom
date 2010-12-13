
namespace Axiom.Samples.SphereMapping
{
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
