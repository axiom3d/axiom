using System;
using System.Collections.Generic;
using System.Text;

namespace Axiom.Demos.DeferredShadingSystem
{
    class LightMaterialGenerator : MaterialGenerator
    {
        public LightMaterialGenerator( string language ) 
        {
            _bitNames.Add( "Quad" );		    // MaterialId.Quad
            _bitNames.Add( "Attenuated" );      // MaterialId.Attenuated
            _bitNames.Add( "Specular" );        // MaterialId.Specular

            _vsMask = 0x00000001;
            _psMask = 0x00000006;
            _matMask = 0x00000001;

            _materialBaseName = "DeferredShading/LightMaterial/";
            if ( language == "hlsl" )
                _generator = new LightMaterialGeneratorHlsl( "DeferredShading/LightMaterial/hlsl/" );
            else
                _generator = new LightMaterialGeneratorGlsl( "DeferredShading/LightMaterial/glsl/" );
        }
    }
}
