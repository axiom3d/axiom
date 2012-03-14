using System.ComponentModel.Composition;

using Axiom.Core;

namespace Axiom.Samples.VolumeTexture
{
	[Export( typeof( IPlugin ) )]
	public class Plugin : SamplePlugin
	{
		private VolumeTextureSample sample;

		public override void Initialize()
		{
			this.sample = new VolumeTextureSample();
			Name = this.sample.Metadata[ "Title" ] + " Sample";
			AddSample( this.sample );
		}
	}
}
