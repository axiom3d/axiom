#region LGPL License
/*
Axiom Graphics Engine Library
Copyright � 2003-2011 Axiom Project Team

The overall design, and a majority of the core engine and rendering code 
contained within this library is a derivative of the open source Object Oriented 
Graphics Engine OGRE, which can be found at http://ogre.sourceforge.net.  
Many thanks to the OGRE team for maintaining such a high quality project.

This library is free software; you can redistribute it and/or
modify it under the terms of the GNU Lesser General Public
License as published by the Free Software Foundation; either
version 2.1 of the License, or (at your option) any later version.

This library is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
Lesser General Public License for more details.

You should have received a copy of the GNU Lesser General Public
License along with this library; if not, write to the Free Software
Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
*/
#endregion

#region SVN Version Information
// <file>
//     <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections.Generic;
using Axiom.Core;
using Axiom.Core.Collections;
using Axiom.Graphics;
using Axiom.Math;
using Axiom.Scripting.Compiler.AST;

#endregion Namespace Declarations

namespace Axiom.Scripting.Compiler
{
	public partial class ScriptCompiler
	{
		public class MaterialTranslator : Translator
		{
			protected Material _material;

			protected Dictionary<string, string> _textureAliases = new Dictionary<string, string>();

			public MaterialTranslator()
				: base()
			{
			}

			#region Translator Implementation

			/// <see cref="Translator.CheckFor"/>
			internal override bool CheckFor( Keywords nodeId, Keywords parentId )
			{
				return nodeId == Keywords.ID_MATERIAL;
			}

			/// <see cref="Translator.Translate"/>
			public override void Translate( ScriptCompiler compiler, AbstractNode node )
			{
				ObjectAbstractNode obj = (ObjectAbstractNode)node;
				if ( obj != null )
				{
					if ( string.IsNullOrEmpty( obj.Name ) )
						compiler.AddError( CompileErrorCode.ObjectNameExpected, obj.File, obj.Line );
				}
				else
				{
					compiler.AddError( CompileErrorCode.ObjectNameExpected, node.File, node.Line );
					return;
				}

				// Create a material with the given name
				object mat;
				ScriptCompilerEvent evt = new CreateMaterialScriptCompilerEvent( node.File, obj.Name, compiler.ResourceGroup );
				bool processed = compiler._fireEvent( ref evt, out mat );

				if ( !processed )
				{
					//TODO
					// The original translated implementation of this code block was simply the following:
					// _material = (Material)MaterialManager.Instance.Create( obj.Name, compiler.ResourceGroup );
					// but sometimes it generates an exception due to a duplicate resource.
					// In order to avoid the above mentioned exception, the implementation was changed, but
					// it need to be checked when ResourceManager._add will be updated to the latest version

					Material checkForExistingMat = (Material)MaterialManager.Instance.GetByName( obj.Name );

					if ( checkForExistingMat == null )
						_material = (Material)MaterialManager.Instance.Create( obj.Name, compiler.ResourceGroup );
					else
						_material = checkForExistingMat;
				}
				else
				{
					_material = (Material)mat;

					if ( _material == null )
					{
						compiler.AddError( CompileErrorCode.ObjectAllocationError, obj.File, obj.Line, "failed to find or create material \"" + obj.Name + "\"" );
					}
				}

				_material.RemoveAllTechniques();
				obj.Context = _material;
				_material.Origin = obj.File;

				foreach ( AbstractNode i in obj.Children )
				{
					if ( i.Type == AbstractNodeType.Property )
					{
						PropertyAbstractNode prop = (PropertyAbstractNode)i;

						switch ( (Keywords)prop.Id )
						{
							#region ID_LOD_VALUES
							case Keywords.ID_LOD_VALUES:
								{
									LodValueList lods = new LodValueList();
									foreach ( AbstractNode j in prop.Values )
									{
										Real v = 0;
										if ( getReal( j, out v ) )
											lods.Add( v );
										else
											compiler.AddError( CompileErrorCode.NumberExpected, prop.File, prop.Line,
												"lod_values expects only numbers as arguments" );
									}
									_material.SetLodLevels( lods );
								}
								break;
							#endregion ID_LOD_VALUES

							#region ID_LOD_DISTANCES
							case Keywords.ID_LOD_DISTANCES:
								{
									// Set strategy to distance strategy
									LodStrategy strategy = DistanceLodStrategy.Instance;
									_material.LodStrategy = strategy;

									// Real in lod distances
									LodValueList lods = new LodValueList();
									foreach ( AbstractNode j in prop.Values )
									{
										Real v = 0;
										if ( getReal( j, out v ) )
											lods.Add( v );
										else
											compiler.AddError( CompileErrorCode.NumberExpected, prop.File, prop.Line,
												"lod_values expects only numbers as arguments" );
									}
									_material.SetLodLevels( lods );
								}
								break;
							#endregion ID_LOD_DISTANCES

							#region ID_LOD_STRATEGY
							case Keywords.ID_LOD_STRATEGY:
								if ( prop.Values.Count == 0 )
								{
									compiler.AddError( CompileErrorCode.StringExpected, prop.File, prop.Line );
								}
								else if ( prop.Values.Count > 1 )
								{
									compiler.AddError( CompileErrorCode.FewerParametersExpected, prop.File, prop.Line,
										"lod_strategy only supports 1 argument" );
								}
								else
								{
									string strategyName = string.Empty;
									bool result = getString( prop.Values[ 0 ], out strategyName );
									if ( result )
									{
										LodStrategy strategy = LodStrategyManager.Instance.GetStrategy( strategyName );

										result = strategy != null;

										if ( result )
											_material.LodStrategy = strategy;
									}

									if ( !result )
									{
										compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
											"lod_strategy argument must be a valid lod strategy" );
									}
								}
								break;
							#endregion ID_LOD_STRATEGY

							#region ID_RECEIVE_SHADOWS
							case Keywords.ID_RECEIVE_SHADOWS:
								if ( prop.Values.Count == 0 )
								{
									compiler.AddError( CompileErrorCode.StringExpected, prop.File, prop.Line );
								}
								else if ( prop.Values.Count > 1 )
								{
									compiler.AddError( CompileErrorCode.FewerParametersExpected, prop.File, prop.Line,
										"receive_shadows only supports 1 argument" );
								}
								else
								{
									bool val = true;
									if ( getBoolean( prop.Values[ 0 ], out val ) )
										_material.ReceiveShadows = val;
									else
										compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
											"receive_shadows argument must be \"true\", \"false\", \"yes\", \"no\", \"on\", or \"off\"" );
								}
								break;
							#endregion ID_RECEIVE_SHADOWS

							#region ID_TRANSPARENCY_CASTS_SHADOWS
							case Keywords.ID_TRANSPARENCY_CASTS_SHADOWS:
								if ( prop.Values.Count == 0 )
								{
									compiler.AddError( CompileErrorCode.StringExpected, prop.File, prop.Line );
								}
								else if ( prop.Values.Count > 1 )
								{
									compiler.AddError( CompileErrorCode.FewerParametersExpected, prop.File, prop.Line,
										"transparency_casts_shadows only supports 1 argument" );
								}
								else
								{
									bool val = true;
									if ( getBoolean( prop.Values[ 0 ], out val ) )
										_material.TransparencyCastsShadows = val;
									else
										compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
											"transparency_casts_shadows argument must be \"true\", \"false\", \"yes\", \"no\", \"on\", or \"off\"" );
								}
								break;
							#endregion ID_TRANSPARENCY_CASTS_SHADOWS

							#region ID_SET_TEXTURE_ALIAS
							case Keywords.ID_SET_TEXTURE_ALIAS:
								if ( prop.Values.Count == 0 )
								{
									compiler.AddError( CompileErrorCode.StringExpected, prop.File, prop.Line );
								}
								else if ( prop.Values.Count > 3 )
								{
									compiler.AddError( CompileErrorCode.FewerParametersExpected, prop.File, prop.Line );
								}
								else
								{
									AbstractNode i0 = getNodeAt( prop.Values, 0 ), i1 = getNodeAt( prop.Values, 1 );
									String name, value;
									if ( getString( i0, out name ) && getString( i1, out value ) )
										_textureAliases.Add( name, value );
									else
										compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
											"set_texture_alias must have 2 string argument" );
								}
								break;
							#endregion ID_SET_TEXTURE_ALIAS

							default:
								compiler.AddError( CompileErrorCode.UnexpectedToken, prop.File, prop.Line, "token \"" + prop.Name + "\" is not recognized" );
								break;
						} //end of switch statement
					}
					else if ( i.Type == AbstractNodeType.Object )
						_processNode( compiler, i );
				}

				// Apply the texture aliases
				ScriptCompilerEvent locEvt = new PreApplyTextureAliasesScriptCompilerEvent( _material, ref _textureAliases );
				compiler._fireEvent( ref locEvt );

				_material.ApplyTextureAliases( _textureAliases );
				_textureAliases.Clear();
			}

			#endregion Translator Implementation
		}
	}
}

