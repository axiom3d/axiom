namespace Axiom.Components.RTShaderSystem
{
	internal class FFPLightingFactory : SubRenderStateFactory
	{
		public override string Type
		{
			get
			{
				return FFPLighting.FFPType;
			}
		}

		public override SubRenderState CreateInstance( Scripting.Compiler.ScriptCompiler compiler,
		                                               Scripting.Compiler.AST.PropertyAbstractNode prop,
		                                               Graphics.Pass pass, ScriptTranslator stranslator )
		{
			if ( prop.Name == "lighting_stage" )
			{
				if ( prop.Values.Count == 1 )
				{
					string modelType;

					if ( !SGScriptTranslator.GetString( prop.Values[ 0 ], out modelType ) )
					{
						//compiler.AddError(...);
						return null;
					}

					if ( modelType == "ffp" )
					{
						return CreateOrRetrieveInstance( stranslator );
					}
				}
			}
			return null;
		}

		public override void WriteInstance( Serialization.MaterialSerializer ser, SubRenderState subRenderState,
		                                    Graphics.Pass srcPass, Graphics.Pass dstPass )
		{
			//TODO
			//ser.WriteAttribute(4, "lighting_stage");
			//ser.WriteValue("ffp");
		}

		protected override SubRenderState CreateInstanceImpl()
		{
			return new FFPLighting();
		}
	}
}