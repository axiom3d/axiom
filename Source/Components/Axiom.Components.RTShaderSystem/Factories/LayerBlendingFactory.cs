namespace Axiom.Components.RTShaderSystem
{
	internal class LayerBlendingFactory : SubRenderStateFactory
	{
		public override string Type
		{
			get
			{
				return LayeredBlending.LBType;
			}
		}

		public LayeredBlending.BlendMode StringToBlendMode( string strValue )
		{
			for ( int i = 0; i < LayeredBlending.blendModes.Length; i++ )
			{
				if ( LayeredBlending.blendModes[ i ].Name == strValue )
				{
					return LayeredBlending.blendModes[ i ].Type;
				}
			}
			return LayeredBlending.BlendMode.Invalid;
		}

		public string BlendModeToString( LayeredBlending.BlendMode blendMode )
		{
			for ( int i = 0; i < LayeredBlending.blendModes.Length; i++ )
			{
				if ( LayeredBlending.blendModes[ i ].Type == blendMode )
				{
					return LayeredBlending.blendModes[ i ].Name;
				}
			}

			return string.Empty;
		}

		private LayeredBlending.SourceModifier StringToSourceModifier( string strValue )
		{
			for ( int i = 0; i < LayeredBlending.sourceModifiers.Length; i++ )
			{
				if ( LayeredBlending.sourceModifiers[ i ].Name == strValue )
				{
					return LayeredBlending.sourceModifiers[ i ].Type;
				}
			}

			return LayeredBlending.SourceModifier.Invalid;
		}

		public string SourceModifierToString( LayeredBlending.SourceModifier modifier )
		{
			for ( int i = 0; i < LayeredBlending.sourceModifiers.Length; i++ )
			{
				if ( LayeredBlending.sourceModifiers[ i ].Type == modifier )
				{
					return LayeredBlending.sourceModifiers[ i ].Name;
				}
			}

			return string.Empty;
		}

		public override SubRenderState CreateInstance( Scripting.Compiler.ScriptCompiler compiler,
		                                               Scripting.Compiler.AST.PropertyAbstractNode prop,
		                                               Graphics.TextureUnitState texState, SGScriptTranslator translator )
		{
			if ( prop.Name == "layered_blend" )
			{
				string blendType;
				if ( !SGScriptTranslator.GetString( prop.Values[ 0 ], out blendType ) )
				{
					// compiler.AddError(...);
					return null;
				}

				LayeredBlending.BlendMode blendMode = StringToBlendMode( blendType );
				if ( blendMode == LayeredBlending.BlendMode.Invalid )
				{
					//  compiler.AddError(...);
					return null;
				}

				//get the layer blend sub-render state to work on
				var layeredBlendState = (LayeredBlending)CreateOrRetrieveInstance( translator );

				int texIndex = -1;
				//TODO: check impl. Ogre use: texIndex = texState.Parent.GetTextureUnitStateIndex(texState);
				for ( int i = 0; i < texState.Parent.TextureUnitStatesCount; i++ )
				{
					if ( texState.Parent.GetTextureUnitState( i ) == texState )
					{
						texIndex = i;
						break;
					}
				}
				layeredBlendState.SetBlendMode( texIndex, blendMode );

				return layeredBlendState;
			}
			if ( prop.Name == "source_modifier" )
			{
				if ( prop.Values.Count < 3 )
				{
					//compiler.AddError(..);
					return null;
				}

				//Read light model type
				bool isParseSuccess;
				string modifierString;
				string paramType;
				int customNum;

				int itValue = 0;
				isParseSuccess = SGScriptTranslator.GetString( prop.Values[ itValue ], out modifierString );
				LayeredBlending.SourceModifier modType = StringToSourceModifier( modifierString );
				isParseSuccess &= modType != LayeredBlending.SourceModifier.Invalid;
				if ( isParseSuccess == false )
				{
					//compiler.AddError(...);
					return null;
				}
				itValue++;
				isParseSuccess = SGScriptTranslator.GetString( prop.Values[ itValue ], out paramType );
				isParseSuccess &= ( paramType == "custom" );
				if ( isParseSuccess == false )
				{
					// compiler.AddError(...);

					return null;
				}

				itValue++;
				isParseSuccess = SGScriptTranslator.GetString( prop.Values[ itValue ], out paramType );
				if ( isParseSuccess == false )
				{
					//compiler.AddError(...);
					return null;
				}
				itValue++;
				isParseSuccess = SGScriptTranslator.GetInt( prop.Values[ itValue ], out customNum );
				if ( isParseSuccess == false )
				{
					//compiler.AddError(...);
					return null;
				}

				//get the layer blend sub render state to work on
				var layeredBlendState = (LayeredBlending)CreateOrRetrieveInstance( translator );

				int texIndex = 0;
				//update the layer sub render state
				for ( int i = 0; i < texState.Parent.TextureUnitStatesCount; i++ )
				{
					if ( texState.Parent.GetTextureUnitState( i ) == texState )
					{
						texIndex = i;
						break;
					}
				}
				layeredBlendState.SetSourceModifier( texIndex, modType, customNum );

				return layeredBlendState;
			}
			return null;
		}

		public override void WriteInstance( Serialization.MaterialSerializer ser, SubRenderState subRenderState,
		                                    Graphics.TextureUnitState srcTextureUnit,
		                                    Graphics.TextureUnitState dstTextureUnit )
		{
			int texIndex = 0;
			for ( int i = 0; i < srcTextureUnit.Parent.TextureUnitStatesCount; i++ )
			{
				if ( srcTextureUnit.Parent.GetTextureUnitState( i ) == srcTextureUnit )
				{
					texIndex = i;
					break;
				}
			}

			//get blend mode for current texture unit
			var layeredBlendingSubRenderState = subRenderState as LayeredBlending;

			//write the blend mode
			LayeredBlending.BlendMode blendMode = layeredBlendingSubRenderState.GetBlendMode( texIndex );
			if ( blendMode != LayeredBlending.BlendMode.Invalid )
			{
				//TODO
				//ser.WriteAttribute(5, "layered_blend");
				//ser.WriteAttribute(BlendModeToString(blendMode));
			}

			//write the source modifier
			LayeredBlending.SourceModifier modType;
			int customNum;
			if ( layeredBlendingSubRenderState.GetSourceModifier( texIndex, out modType, out customNum ) == true )
			{
				//TODO
				//ser.WriteAttribute(5, "source_modifier");
				//ser.WriteValue(SourceModifierToString(modType));
				//ser.WriteValue("custom");
				//ser.WriteValue(customNum.ToString());
			}
		}

		private LayeredBlending CreateOrRetrieveSubRenderState( SGScriptTranslator translator )
		{
			LayeredBlending layeredBlendState = null;
			//check if we already create a blend srs
			SubRenderState subState = translator.GetGeneratedSubRenderState( Type );
			if ( subState != null )
			{
				layeredBlendState = subState as LayeredBlending;
			}
			else
			{
				SubRenderState subRenderState = CreateOrRetrieveInstance( translator );
				layeredBlendState = (LayeredBlending)subRenderState;
			}

			return layeredBlendState;
		}

		protected override SubRenderState CreateInstanceImpl()
		{
			return new LayeredBlending();
		}
	}
}