using System.Collections.Generic;
using Axiom.Math;

namespace Axiom.Components.RTShaderSystem
{
	internal class IntegratedPSSM3Factory : SubRenderStateFactory
	{
		public override SubRenderState CreateInstance( Scripting.Compiler.ScriptCompiler compiler,
		                                               Scripting.Compiler.AST.PropertyAbstractNode prop,
		                                               Graphics.Pass pass, ScriptTranslator stranslator )
		{
			if ( prop.Name == "integrated_pssm4" )
			{
				if ( prop.Values.Count != 4 )
				{
					//TODO
					// compiler.AddError(...);
				}
				else
				{
					var splitPointList = new List<Real>();

					foreach ( var it in prop.Values )
					{
						Real curSplitValue;

						if ( !SGScriptTranslator.GetReal( it, out curSplitValue ) )
						{
							//TODO
							//compiler.AddError(...);
							break;
						}

						splitPointList.Add( curSplitValue );
					}

					if ( splitPointList.Count == 4 )
					{
						SubRenderState subRenderState = CreateOrRetrieveInstance( stranslator );
						var pssmSubRenderState = (IntegratedPSSM3)subRenderState;
						pssmSubRenderState.SetSplitPoints( splitPointList );

						return pssmSubRenderState;
					}
				}
			}

			return null;
		}

		protected override SubRenderState CreateInstanceImpl()
		{
			return new IntegratedPSSM3();
		}

		public override string Type
		{
			get
			{
				return IntegratedPSSM3.SGXType;
			}
		}
	}
}