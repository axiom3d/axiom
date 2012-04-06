using System;

namespace Axiom.Components.RTShaderSystem
{
    internal class FFPFogFactory : SubRenderStateFactory
    {
        public override string Type
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override SubRenderState CreateInstance( Scripting.Compiler.ScriptCompiler compiler,
                                                       Scripting.Compiler.AST.PropertyAbstractNode prop,
                                                       Graphics.Pass pass, SGScriptTranslator stranslator )
        {
            if ( prop.Name == "fog_stage" )
            {
                if ( prop.Values.Count >= 1 )
                {
                    string strValue;

                    if ( !SGScriptTranslator.GetString( prop.Values[ 0 ], out strValue ) )
                    {
                        //compiler.AddError(...);
                        return null;
                    }

                    if ( strValue == "ffp" )
                    {
                        SubRenderState subRenderState = CreateOrRetrieveInstance( stranslator );
                        var fogSubRenderState = (FFPFog)subRenderState;
                        int it = 0;

                        if ( prop.Values.Count >= 2 )
                        {
                            it++;
                            if ( !SGScriptTranslator.GetString( prop.Values[ it ], out strValue ) )
                            {
                                //compiler.AddError(...);
                                return null;
                            }
                            if ( strValue == "per_vertex" )
                            {
                                fogSubRenderState.CalculationMode = FFPFog.CalcMode.PerVertex;
                            }
                            else if ( strValue == "per_pixel" )
                            {
                                fogSubRenderState.CalculationMode = FFPFog.CalcMode.PerPixel;
                            }
                        }
                        return subRenderState;
                    }
                }
            }
            return null;
        }

        public override void WriteInstance( Serialization.MaterialSerializer ser, SubRenderState subRenderState,
                                            Graphics.Pass srcPass, Graphics.Pass dstPass )
        {
            base.WriteInstance( ser, subRenderState, srcPass, dstPass );
        }

        protected override SubRenderState CreateInstanceImpl()
        {
            throw new NotImplementedException();
        }
    }
}