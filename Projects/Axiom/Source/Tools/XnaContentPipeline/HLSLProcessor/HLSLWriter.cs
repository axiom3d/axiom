using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline.Processors;
using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Compiler;

// TODO: replace this with the type you want to write out.
using TWrite = System.String;

namespace Axiom.HLSLProcessor
{
	/// <summary>
	/// This class will be instantiated by the XNA Framework Content Pipeline
	/// to write the specified data type into binary .xnb format.
	///
	/// This should be part of a Content Pipeline Extension Library project.
	/// </summary>
	[ContentTypeWriter]
	class HLSLWriter : ContentTypeWriter<HLSLCompiledShaders>
	{
		protected override void Write(ContentWriter output, HLSLCompiledShaders value)
		{
			//number of compiled shaders
			output.Write(value.CompiledShaders.Count);
			for (int i = 0; i < value.CompiledShaders.Count; ++i)
			{
				//write out compiled shader info - entry point and shader code
				output.Write(value.CompiledShaders[i].EntryPoint);
				output.Write(value.CompiledShaders[i].ShaderCode.Length);
				output.Write(value.CompiledShaders[i].ShaderCode);
			}
		}

		public override string GetRuntimeReader(TargetPlatform targetPlatform)
		{
			return "Axiom.HLSLReader.HLSLReader, Axiom.HLSLReader";
		}
	}
}
