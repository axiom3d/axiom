using System;
using System.Collections.Generic;
using System.Text;

namespace Axiom.RenderSystems.Xna.Content
{
    public class HlslCompiledShader
    {
        public HlslCompiledShader( string entryPoint, byte[] shaderCode ) 
		{
			this.ShaderCode = shaderCode;
			this.EntryPoint = entryPoint;
		}

        public string EntryPoint
        {
            get;
            private set;
        }

        public byte[] ShaderCode
        {
            get;
            private set;
        }
    }

    public class HlslCompiledShaders : List<HlslCompiledShader>
    {
    }

}
