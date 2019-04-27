using System;
using System.Collections.Generic;

namespace Axiom.Components.RTShaderSystem
{
    internal class ProgramWriterManager : IDisposable
    {
        private static ProgramWriterManager _instance;
        private Dictionary<string, ProgramWriterFactory> factories;

        public ProgramWriterManager()
        {
        }

        public static ProgramWriterManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new ProgramWriterManager();
                }
                return _instance;
            }
        }

        internal bool IsLanguageSupported(string shaderLanguage)
        {
            return this.factories.ContainsKey(shaderLanguage);
        }

        internal void AddFactory(ProgramWriterFactory programWriterFactory)
        {
            this.factories.Add(programWriterFactory.TargetLanguage, programWriterFactory);
        }

        internal void RemoveFactory(ProgramWriterFactory programWriterFactory)
        {
            //remove only if equal to registered one, 
            //since it might be overridden by other plugins
            if (this.factories.ContainsKey(programWriterFactory.TargetLanguage))
            {
                this.factories.Remove(programWriterFactory.TargetLanguage);
            }
        }

        internal ProgramWriter CreateProgramWriter(string language)
        {
            if (this.factories.ContainsKey(language))
            {
                return this.factories[language].Create();
            }

            throw new Core.AxiomException("Could not create ShaderProgramWriter unknown language");
        }

        public virtual void Dispose()
        {
        }
    }
}