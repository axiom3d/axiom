using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline;
using System.IO;

namespace Axiom.RenderSystems.Xna.Content
{
	class HlslIncludeHandler : CompilerIncludeHandler
	{
		public override Stream Open( CompilerIncludeHandlerType includeType, string filename )
		{
			return File.Open( filename, FileMode.Open );
		}
	}
}