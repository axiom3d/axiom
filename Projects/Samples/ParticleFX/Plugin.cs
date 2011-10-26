using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using Axiom.Core;

namespace Axiom.Samples.ParticleFX
{
	[Export(typeof(IPlugin))]
	public class Plugin : SamplePlugin
	{
		private ParticleFXSample sample;
		public override void Initialize()
		{
			sample = new ParticleFXSample();
			Name = sample.Metadata[ "Title" ] + " Sample";
			AddSample( sample );
		}
	}
}
