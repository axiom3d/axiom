using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Axiom.Samples.VolumeTexture
{
	public class Plugin :SamplePlugin
	{
		private VolumeTextureSample sample;
		public override void Initialize()
		{
			sample = new VolumeTextureSample();
			Name = sample.Metadata[ "Title" ] + " Sample";
			AddSample( sample );
		}
	}
}
