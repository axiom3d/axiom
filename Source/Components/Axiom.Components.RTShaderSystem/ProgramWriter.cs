using System;
using System.IO;

namespace Axiom.Components.RTShaderSystem
{
    internal abstract class ProgramWriter : IDisposable
    {
        public ProgramWriter()
        {
        }

        internal virtual void WriteSourceCode(StreamWriter stream, Program program)
        {
        }

        protected void WriteProgramTitle(StreamWriter stream, Program program)
        {
            stream.WriteLine("//-----------------------------------------------------------------------------");
            stream.Write("// Program Type: ");
            switch (program.Type)
            {
                case Axiom.Graphics.GpuProgramType.Fragment:
                    stream.Write("Fragment shader");
                    break;
                case Axiom.Graphics.GpuProgramType.Geometry:
                    stream.Write("Geometry shader");
                    break;
                case Axiom.Graphics.GpuProgramType.Vertex:
                    stream.Write("Vertex shader");
                    break;
                default:
                    break;
            }
            stream.Write("\n");
            stream.WriteLine("// Language: " + TargetLanguage);
            stream.WriteLine("// Created by Axiom RT Shader Generator. All rights reserved.");
            stream.WriteLine("//-----------------------------------------------------------------------------");
        }

        protected void WriteUniformParametersTitle(StreamWriter stream, Program program)
        {
            stream.WriteLine("//-----------------------------------------------------------------------------");
            stream.WriteLine("//                    GLOBAL PARAMETERS");
            stream.WriteLine("//-----------------------------------------------------------------------------");
        }

        protected void WriteFunctionTitle(StreamWriter stream, Function function)
        {
            stream.Write("//-----------------------------------------------------------------------------");
            stream.WriteLine("// Function Name: " + function.Name);
            stream.WriteLine("//Function Desc: " + function.Description);
            stream.WriteLine("//-----------------------------------------------------------------------------");
        }

        internal abstract string TargetLanguage { get; }

        public virtual void Dispose()
        {
        }
    }
}