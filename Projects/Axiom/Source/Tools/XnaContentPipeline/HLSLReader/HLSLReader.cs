using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Content;

namespace Axiom.HLSLReader
{
	class HLSLReader : ContentTypeReader<HLSLCompiledShaders>
	{
		/// <summary>
		/// Loads compiled shaders
		/// </summary>
		protected override HLSLCompiledShaders Read(ContentReader input, HLSLCompiledShaders existingInstance)
		{
			int numCompiledShaders = input.ReadInt32();
			HLSLCompiledShaders compiledShaders = new HLSLCompiledShaders();
			for (int i = 0; i < numCompiledShaders; ++i)
			{
				string entryPoint = input.ReadString();
				int codeSize = input.ReadInt32();
				byte[] shaderCode = input.ReadBytes(codeSize);
				HLSLCompiledShader compiledShader = new HLSLCompiledShader(entryPoint, shaderCode);
				compiledShaders.AddCompiledShader(compiledShader);
			}
			return compiledShaders;
		}
	}
}