namespace Axiom.Components.RTShaderSystem
{
	internal class FFPTexturingFactory : SubRenderStateFactory
	{
		public override string Type
		{
			get
			{
				return FFPTexturing.FFPType;
			}
		}

		public override SubRenderState CreateInstance( Scripting.Compiler.ScriptCompiler compiler,
		                                               Scripting.Compiler.AST.PropertyAbstractNode prop,
		                                               Graphics.Pass pass, SGScriptTranslator stranslator )
		{
			if ( prop.Name == "texturing_stage" )
			{
				if ( prop.Values.Count == 1 )
				{
					string modelType;

					if ( !SGScriptTranslator.GetString( prop.Values[ 0 ], out modelType ) )
					{
						// compiler.AddError(...);
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
			//ser.WriteAttribute(4, "texturing_stage");
			//ser.WriteValue("ffp");
		}

		protected override SubRenderState CreateInstanceImpl()
		{
			return new FFPTexturing();
		}
	}
}