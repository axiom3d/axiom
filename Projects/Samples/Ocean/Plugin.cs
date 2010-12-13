using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Axiom.Samples.Ocean
{
    public class Plugin : SamplePlugin
    {
        private OceanSample sample;
        public override void Initialize()
        {
            sample = new OceanSample();
            Name = sample.Metadata[ "Title" ] + " Sample";
            AddSample( sample );
        }
    }
}
