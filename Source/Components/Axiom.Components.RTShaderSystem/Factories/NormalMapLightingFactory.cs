using System;
using Axiom.Scripting.Compiler.AST;
using Axiom.Math;

namespace Axiom.Components.RTShaderSystem
{
	internal class NormalMapLightingFactory : SubRenderStateFactory
	{
		public override string Type
		{
			get
			{
				return NormalMapLighting.SGXType;
			}
		}

		public override SubRenderState CreateInstance( Scripting.Compiler.ScriptCompiler compiler,
		                                               PropertyAbstractNode prop, Graphics.Pass pass,
		                                               SGScriptTranslator stranslator )
		{
			if ( prop.Name == "lighting_stage" )
			{
				if ( prop.Values.Count >= 2 )
				{
					string strValue;
					int it = 0;

					//Read light model type.
					if ( !SGScriptTranslator.GetString( prop.Values[ it ], out strValue ) )
					{
						//compiler.AddError(...)
						return null;
					}

					//Case light model type is normal map
					if ( strValue == "normal_map" )
					{
						it++;
						if ( !SGScriptTranslator.GetString( prop.Values[ it ], out strValue ) )
						{
							//compiler.AddError(...)
							return null;
						}

						SubRenderState subRenderState = CreateOrRetrieveInstance( stranslator );
						var normalMapSubRenderState = subRenderState as NormalMapLighting;

						normalMapSubRenderState.NormalMapTextureName = strValue;

						//Read normal map space type.
						if ( prop.Values.Count >= 3 )
						{
							it++;
							if ( !SGScriptTranslator.GetString( prop.Values[ it ], out strValue ) )
							{
								//compiler.AddError(...)
								return null;
							}

							//Normal map defines normals in tangent space.
							if ( strValue == "tangent_space" )
							{
								normalMapSubRenderState.NormalMapSpace = NormalMapSpace.Tangent;
							}

							//Normal map defines normals in object space
							if ( strValue == "object_space" )
							{
								normalMapSubRenderState.NormalMapSpace = NormalMapSpace.Object;
							}
						}

						//Read texture coordinate index.
						if ( prop.Values.Count >= 4 )
						{
							int textureCoordinatesIndex = 0;
							it++;
							if ( !SGScriptTranslator.GetInt( prop.Values[ it ], out textureCoordinatesIndex ) )
							{
								normalMapSubRenderState.TexCoordIndex = textureCoordinatesIndex;
							}
						}

						//Read texture filtering format
						if ( prop.Values.Count >= 5 )
						{
							it++;
							if ( !SGScriptTranslator.GetString( prop.Values[ it ], out strValue ) )
							{
								//compiler.AddError(...)
								return null;
							}

							if ( strValue == "none" )
							{
								normalMapSubRenderState.SetNormalMapFiltering( Graphics.FilterOptions.Point, Graphics.FilterOptions.Point,
								                                               Graphics.FilterOptions.None );
							}
							else if ( strValue == "bilinear" )
							{
								normalMapSubRenderState.SetNormalMapFiltering( Graphics.FilterOptions.Linear, Graphics.FilterOptions.Linear,
								                                               Graphics.FilterOptions.Point );
							}
							else if ( strValue == "trilinear" )
							{
								normalMapSubRenderState.SetNormalMapFiltering( Graphics.FilterOptions.Linear, Graphics.FilterOptions.Linear,
								                                               Graphics.FilterOptions.Linear );
							}
							else if ( strValue == "anisotropic" )
							{
								normalMapSubRenderState.SetNormalMapFiltering( Graphics.FilterOptions.Anisotropic,
								                                               Graphics.FilterOptions.Anisotropic, Graphics.FilterOptions.Linear );
							}
						}

						//Read max anisotropy value
						if ( prop.Values.Count >= 6 )
						{
							int maxAnisotropy = 0;
							it++;
							if ( SGScriptTranslator.GetInt( prop.Values[ it ], out maxAnisotropy ) )
							{
								normalMapSubRenderState.NormalMapAnisotropy = maxAnisotropy;
							}
						}
						//Read mip bias value.
						if ( prop.Values.Count >= 7 )
						{
							Real mipBias = 0;
							it++;
							if ( SGScriptTranslator.GetReal( prop.Values[ it ], out mipBias ) )
							{
								normalMapSubRenderState.NormalMapMipBias = mipBias;
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
			throw new NotImplementedException();
			var normalMapSubRenderState = (NormalMapLighting)subRenderState;

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