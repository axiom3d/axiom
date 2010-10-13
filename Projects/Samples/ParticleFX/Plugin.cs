using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Axiom.Samples.ParticleFX
{
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
