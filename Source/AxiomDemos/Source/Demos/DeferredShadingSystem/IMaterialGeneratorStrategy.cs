using System;
using System.Collections.Generic;
using System.Text;

using Axiom.Graphics;
using MaterialPermutation = System.UInt32;

namespace Axiom.Demos.DeferredShadingSystem
{
    interface IMaterialGeneratorStrategy
    {
        #region Material Generation

        GpuProgram GenerateVertexShader( MaterialPermutation permutation );

		GpuProgram GeneratePixelShader( MaterialPermutation permutation );

		Material GenerateTemplateMaterial( MaterialPermutation permutation );

        #endregion Material Generation
    }
}
