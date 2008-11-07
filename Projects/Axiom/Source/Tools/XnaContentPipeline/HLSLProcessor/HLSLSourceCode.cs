using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Content.Pipeline;

namespace Axiom.HLSLProcessor
{
	class HLSLSourceCode
	{
		private string sourceCode;

		public HLSLSourceCode(string sourceCode)
		{
			this.sourceCode = sourceCode;
		}

		public string SourceCode 
		{
			get 
			{ 
				return sourceCode; 
			} 
		}
	}
}
