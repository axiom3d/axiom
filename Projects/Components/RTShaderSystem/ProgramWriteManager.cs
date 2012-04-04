using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Axiom.Components.RTShaderSystem
{
    class ProgramWriterManager : IDisposable
    {
        static ProgramWriterManager _instance;
        Dictionary<string, ProgramWriterFactory> factories;

        public ProgramWriterManager()
        { }
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
            return factories.ContainsKey(shaderLanguage);
        }

        internal void AddFactory(ProgramWriterFactory programWriterFactory)
        {
            factories.Add(programWriterFactory.TargetLanguage, programWriterFactory);
        }

        internal void RemoveFactory(ProgramWriterFactory programWriterFactory)
        {
            //remove only if equal to registered one, 
            //since it might be overridden by other plugins
            if (factories.ContainsKey(programWriterFactory.TargetLanguage))
            {
                factories.Remove(programWriterFactory.TargetLanguage);
            }
        }
        internal ProgramWriter CreateProgramWriter(string language)
        {
            if (factories.ContainsKey(language))
            {
                return factories[language].Create();
            }

            throw new Core.AxiomException("Could not create ShaderProgramWriter unknown language");

        }

        public virtual void Dispose()
        {

        }
    }
}
