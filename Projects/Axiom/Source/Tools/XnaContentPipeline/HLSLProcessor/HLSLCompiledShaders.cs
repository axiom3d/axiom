using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Axiom.HLSLProcessor
{
	class HLSLCompiledShader
	{
		private byte[] shaderCode;
		private string entryPoint;

		public HLSLCompiledShader(string entryPoint, byte[] shaderCode)
		{
			this.shaderCode = shaderCode;
			this.entryPoint = entryPoint;
		}

		public string EntryPoint
		{
			get
			{
				return entryPoint;
			}
		}

		public byte[] ShaderCode
		{
			get
			{
				return shaderCode;
			}
		}
	}

	class HLSLCompiledShaders
	{
		List<HLSLCompiledShader> compiledShaders = new List<HLSLCompiledShader>();

		public HLSLCompiledShaders()
		{
		}

		public void AddCompiledShader(HLSLCompiledShader compiledShader)
		{
			compiledShaders.Add(compiledShader);
		}

		public List<HLSLCompiledShader> CompiledShaders
		{
			get
			{
				return compiledShaders;
			}
		}
	}
}
