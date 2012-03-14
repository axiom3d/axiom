using System;
using System.Collections.Generic;

using Axiom.Collections;

namespace Axiom.Graphics
{
	public class RenderWindowDescription
	{
		public uint Height;
		public NamedParameterList MiscParams;
		public String Name;
		public bool UseFullScreen;
		public uint Width;
	}

	public class RenderWindowDescriptionList : List<RenderWindowDescription> { }
}
