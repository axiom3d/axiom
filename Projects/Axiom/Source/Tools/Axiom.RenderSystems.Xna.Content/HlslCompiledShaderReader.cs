using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;


namespace Axiom.RenderSystems.Xna.Content
{
    /// <summary>
    /// This class will be instantiated by the XNA Framework Content
    /// Pipeline to read the specified data type from binary .xnb format.
    /// 
    /// Unlike the other Content Pipeline support classes, this should
    /// be a part of your main game project, and not the Content Pipeline
    /// Extension Library project.
    /// </summary>
    public class HlslCompiledShaderReader : ContentTypeReader<HlslCompiledShaders>
    {
        protected override HlslCompiledShaders Read( ContentReader input, HlslCompiledShaders existingInstance )
        {
            HlslCompiledShaders compiledShaders = new HlslCompiledShaders();
            int numCompiledShaders = input.ReadInt32();
            for ( int i = 0; i < numCompiledShaders; ++i )
            {
                string entryPoint = input.ReadString();
                int codeSize = input.ReadInt32();
                byte[] shaderCode = input.ReadBytes( codeSize );
                HlslCompiledShader compiledShader = new HlslCompiledShader( entryPoint, shaderCode );
                compiledShaders.Add( compiledShader );
            }
            return compiledShaders;
        }
    }
}
