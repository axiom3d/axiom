using System;
using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline.Processors;


namespace Axiom.RenderSystems.Xna.Content
{
    using TInput = System.String;
    using TOutput = HlslCompiledShaders;

    /// <summary>
    /// This class will be instantiated by the XNA Framework Content Pipeline
    /// to apply custom processing to content data, converting an object of
    /// type TInput to TOutput. The input and output types may be the same if
    /// the processor wishes to alter data without changing its type.
    ///
    /// This should be part of a Content Pipeline Extension Library project.
    ///
    /// TODO: change the ContentProcessor attribute to specify the correct
    /// display name for this processor.
    /// </summary>
    [ContentProcessor( DisplayName = "Axiom HLSL Processor" )]
    public class HlslProcessor : ContentProcessor<TInput, TOutput>
    {
        [DisplayName( "Shader Profile" )]
        [DefaultValue( ShaderProfile.PS_2_0 )]
        [Description( "The profile to compile this shader with." )]
        public ShaderProfile Profile
        {
            get
            {
                return shaderProfile;
            }
            set
            {
                shaderProfile = value;
            }
        }
        private ShaderProfile shaderProfile = ShaderProfile.PS_2_0;

        [DisplayName( "Entry Point" )]
        [DefaultValue( "main" )]
        [Description( "The name of the function used as the entry point for this shader." )]
        public string EntryPoint
        {
            get
            {
                return entryPoint;
            }
            set
            {
                entryPoint = value;
            }
        }
        private string entryPoint = "main";

        public override TOutput Process( TInput input, ContentProcessorContext context )
        {
            CompiledShader shader = ShaderCompiler.CompileFromSource( input, null, null,
                                                                      CompilerOptions.None, 
                                                                      entryPoint, shaderProfile, 
                                                                      context.TargetPlatform );
            if ( !shader.Success )
            {
                throw new InvalidContentException( shader.ErrorsAndWarnings );
            }

            HlslCompiledShader compiledShader = new HlslCompiledShader( entryPoint, shader.GetShaderCode() );
            HlslCompiledShaders compiledShaders = new HlslCompiledShaders();
            compiledShaders.Add( compiledShader );
            return compiledShaders;
        }
    }
}