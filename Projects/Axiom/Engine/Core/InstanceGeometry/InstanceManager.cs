using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Axiom.Core.InstanceGeometry
{
	public class InstanceManager
	{
		public enum InstancingTechnique
		{
			ShaderBased, //Any SM 2.0+ @See InstanceBatchShader
			TextureVTF, //Needs Vertex Texture Fetch & SM 3.0+ @See InstanceBatchVTF
			HardwareInstancing, //Needs SM 3.0+ and HW instancing support
			Count,
		};

		public bool ShowBoundingBoxes { get; set; }
	}
}
