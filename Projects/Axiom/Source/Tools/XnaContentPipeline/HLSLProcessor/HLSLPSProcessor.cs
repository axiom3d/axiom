using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline.Processors;

//clarabie - note this is a PixelShader processor - but once we get program definitions loading
//properly from scripts, we'll have a single processor for both VS and PS

namespace Axiom.HLSLProcessor
{
	/// <summary>
	/// This class will be instantiated by the XNA Framework Content Pipeline
	/// to apply custom processing to content data, converting an object of
	/// type TInput to TOutput. The input and output types may be the same if
	/// the processor wishes to alter data without changing its type.
	///
	/// This should be part of a Content Pipeline Extension Library project.
	/// </summary>
	[ContentProcessor(DisplayName = "Axiom HLSL Pixel Shader Processor")]
	class HLSLPSProcessor : ContentProcessor<HLSLSourceCode, HLSLCompiledShaders>
	{
		public override HLSLCompiledShaders Process(HLSLSourceCode input, ContentProcessorContext context)
		{
			CompiledShader shader = ShaderCompiler.CompileFromSource(input.SourceCode, null, null,
			CompilerOptions.None, "main", ShaderProfile.PS_2_0, context.TargetPlatform);
			if(!shader.Success)
			{
				throw new InvalidContentException(shader.ErrorsAndWarnings);
			}
			HLSLCompiledShader compiledShader = new HLSLCompiledShader("main", shader.GetShaderCode());
			HLSLCompiledShaders compiledShaders = new HLSLCompiledShaders();
			compiledShaders.AddCompiledShader(compiledShader);
			return compiledShaders;
		}
	}
}