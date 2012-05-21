using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.Composition;
using Axiom.Core;

namespace Axiom.Samples.ShaderSystem
{
	[Export( typeof ( IPlugin ) )]
	public class Plugin : SamplePlugin
	{
		private ShaderSample sample;

		public override void Initialize()
		{
			this.sample = new ShaderSample();
			Name = this.sample.Metadata[ "Title" ] + " Sample";
			AddSample( this.sample );
		}
	}
}