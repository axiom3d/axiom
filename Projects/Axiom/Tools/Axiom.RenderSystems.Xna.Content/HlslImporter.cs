using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;
using TImport = System.String;

namespace Axiom.RenderSystems.Xna.Content
{
	/// <summary>
	/// This class will be instantiated by the XNA Framework Content Pipeline
	/// to import a file from disk into the specified type, TImport.
	///
	/// This should be part of a Content Pipeline Extension Library project.
	///
	/// TODO: change the ContentImporter attribute to specify the correct file
	/// extension, display name, and default processor for this importer.
	/// </summary>
	[ContentImporter( ".hlsl", DisplayName = "Axiom HLSL Importer", DefaultProcessor = "Axiom HLSL Processor" )]
	public class HlslImporter : ContentImporter<TImport>
	{
		public override TImport Import( string filename, ContentImporterContext context )
		{
			return System.IO.File.ReadAllText( filename );
		}
	}
}