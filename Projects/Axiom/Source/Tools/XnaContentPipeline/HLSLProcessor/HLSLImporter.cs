using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;

// TODO: replace this with the type you want to import.
using TImport = System.String;

namespace Axiom.HLSLProcessor
{
	/// <summary>
	/// This class will be instantiated by the XNA Framework Content Pipeline
	/// to import a file from disk into the specified type, TImport.
	/// 
	/// This should be part of a Content Pipeline Extension Library project.
	/// 	
	/// </summary>
	[ContentImporter(".hlsl", DisplayName = "Axiom HLSL Importer", DefaultProcessor = "Axiom.HLSLProcessor.HLSLProcessor")]
	class HLSLImporter : ContentImporter<HLSLSourceCode>
	{
		public override HLSLSourceCode Import(string filename, ContentImporterContext context)
		{
			string sourceCode = System.IO.File.ReadAllText(filename);
			return new HLSLSourceCode(sourceCode);
		}
	}
}
