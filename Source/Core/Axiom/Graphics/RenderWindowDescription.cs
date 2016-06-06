using System;
using System.Collections.Generic;
using Axiom.Collections;

namespace Axiom.Graphics
{
	public class RenderWindowDescription
	{
		public String Name;
		public uint Width;
		public uint Height;
		public bool UseFullScreen;
		public NamedParameterList MiscParams;
	}

	public class RenderWindowDescriptionList : List<RenderWindowDescription>
	{
	}
}