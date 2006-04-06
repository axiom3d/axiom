using System;

using Axiom;

namespace Axiom.RenderSystems.DirectX9.HLSL
{
    /// <summary>
    /// Summary description for HLSLProgramFactory.
    /// </summary>
    public class HLSLProgramFactory : IHighLevelGpuProgramFactory
    {
        #region Fields

        private string language = "hlsl";

        #endregion

        #region IHighLevelGpuProgramFactory Members

        public HighLevelGpuProgram Create( string name, GpuProgramType type )
        {
            return new HLSLProgram( name, type, language );
        }

        /// <summary>
        ///     Gets the high level language that this factory handles requests for.
        /// </summary>
        public string Language
        {
            get
            {
                return language;
            }
        }

        #endregion
    }
}
