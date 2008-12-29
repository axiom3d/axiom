using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline.Processors;
using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Compiler;

namespace Axiom.RenderSystems.Xna.Content
{
    using TWrite = HlslCompiledShaders;

    /// <summary>
    /// This class will be instantiated by the XNA Framework Content Pipeline
    /// to write the specified data type into binary .xnb format.
    ///
    /// This should be part of a Content Pipeline Extension Library project.
    /// </summary>
    [ContentTypeWriter]
    public class HlslCompiledShaderWriter : ContentTypeWriter<TWrite>
    {
		protected override void Write(ContentWriter output, TWrite value)
		{
			//number of compiled shaders
			output.Write(value.Count);
			for (int i = 0; i < value.Count; ++i)
			{
				//write out compiled shader info - entry point and shader code
				output.Write(value[i].EntryPoint);
				output.Write(value[i].ShaderCode.Length);
				output.Write(value[i].ShaderCode);
			}
		}

		public override string GetRuntimeReader(TargetPlatform targetPlatform)
		{
            return "Axiom.RenderSystems.Xna.Content.HlslCompiledShaderReader, Axiom.RenderSystems.Xna.Content";
		}
    }
}
