using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Axiom.Scripting.Compiler.AST;

namespace Axiom.Components.RTShaderSystem
{
    class NormalMapLightingFactory : SubRenderStateFactory
    {
        public override string Type
        {
            get { return NormalMapLighting.SGXType; }
        }
        public override SubRenderState CreateInstance(Scripting.Compiler.ScriptCompiler compiler, PropertyAbstractNode prop, Graphics.Pass pass, SGScriptTranslator stranslator)
        {
            throw new NotImplementedException();
        }
        public override void WriteInstance(Serialization.MaterialSerializer ser, SubRenderState subRenderState, Graphics.Pass srcPass, Graphics.Pass dstPass)
        {
            throw new NotImplementedException();
            NormalMapLighting normalMapSubRenderState = (NormalMapLighting)subRenderState;

            //ser.WriteAtrribute(4, "lighting_stage");
            //ser.WriteValue("normal_map");
            //ser.WriteValue(normalMapSubRenderState.NormalMapTextureName);

            //if (normalMapSubRenderState.NormalMapSpace == NormalMapSpace.Tangent)
            //{
            //    ser.WriteValue("tangent_space");
            //}
            //else if (normalMapSubRenderState.NormalMapSpace == NormalMapSpace.Object)
            //{
            //    ser.WriteValue("object_space");
            //}
            //ser.WriteValue(normalMapSubRenderState.TexCoordIndex.ToString());
        }
       protected override SubRenderState CreateInstanceImpl()
        {
            return new NormalMapLighting();
        }
    }
}
