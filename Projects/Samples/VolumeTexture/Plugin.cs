using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;

using Axiom.Core;

namespace Axiom.Samples.VolumeTexture
{
	[Export( typeof ( IPlugin ) )]
	public class Plugin : SamplePlugin
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
