using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;
using Microsoft.Xna.Framework.Content;
#if !(XBOX || XBOX360)
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Compiler;
#endif
using System.IO;

namespace Axiom.Xna.Content.Pipeline
{
#if !(XBOX || XBOX360)

    public struct HlslProgramImporterData
    {
        public string ProgramSource;
    }

    public struct CompiledHlslProgram
    {
        public CompiledShader CompiledShader;
    }

    [ContentImporter( ".program", DefaultProcessor = "HlslProgramProcessor" )]
    public class HlslProgramImporter : ContentImporter<HlslProgramImporterData>
    {
        public override HlslProgramImporterData Import( string filename, ContentImporterContext context )
        {
            HlslProgramImporterData hlslSource;

            hlslSource.ProgramSource = File.ReadAllText( filename );

            return hlslSource;

        }
    }

    [ContentProcessor]
    public class HlslProgramProcessor : ContentProcessor<HlslProgramImporterData,  CompiledHlslProgram>
    {
        public override CompiledHlslProgram Process( HlslProgramImporterData input, ContentProcessorContext context )
        {
            CompiledHlslProgram compiledHlslProgram;

            compiledHlslProgram.CompiledShader = ShaderCompiler.CompileFromSource( input.ProgramSource, null, null, CompilerOptions.PackMatrixRowMajor, context.TargetPlatform );

            return compiledHlslProgram;
        }
    }

    [ContentTypeWriter]
    public class HlslProgramWriter : ContentTypeWriter<CompiledHlslProgram>
    {
        
        protected override void Write(ContentWriter output,
                                      CompiledHlslProgram value )
        {
            output.Write(value.CompiledShader.GetShaderCode());
        }

        public override string GetRuntimeType(TargetPlatform targetPlatform)
        {
            return typeof(BoundingBox).AssemblyQualifiedName;
        }

        public override string GetRuntimeReader(TargetPlatform targetPlatform)
        {
            return typeof(HlslProgramReader).AssemblyQualifiedName;

        }
    }

#endif

    public class HlslProgramReader : ContentTypeReader<VertexShader>
    {
        protected override VertexShader Read( ContentReader input, VertexShader existingInstance )
        {
            throw new NotImplementedException();
        }
    }

}
