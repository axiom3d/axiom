#region LGPL License
/*
Axiom Graphics Engine Library
Copyright © 2003-2011 Axiom Project Team

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

using System.Collections.Generic;
using Axiom.Graphics;
using Axiom.Media;
using Axiom.Scripting.Compiler.AST;

#endregion Namespace Declarations

namespace Axiom.Scripting.Compiler
{
	public partial class ScriptCompiler
	{
		public class CompositionTechniqueTranslator : Translator
		{
			protected CompositionTechnique _Technique;

			public CompositionTechniqueTranslator()
				: base()
			{
				_Technique = null;
			}

			#region Translator Implementation

			/// <see cref="Translator.CheckFor"/>
			internal override bool CheckFor( Keywords nodeId, Keywords parentId )
			{
				return nodeId == Keywords.ID_TECHNIQUE && parentId == Keywords.ID_COMPOSITOR;
			}

			/// <see cref="Translator.Translate"/>
			public override void Translate( ScriptCompiler compiler, AbstractNode node )
			{
				var obj = (ObjectAbstractNode)node;

				var compositor = (Compositor)obj.Parent.Context;
				_Technique = compositor.CreateTechnique();
				obj.Context = _Technique;

				foreach ( var i in obj.Children )
				{
					if ( i is ObjectAbstractNode )
					{
						processNode( compiler, i );
					}
					else if ( i is PropertyAbstractNode )
					{
						var prop = (PropertyAbstractNode)i;
						switch ( (Keywords)prop.Id )
						{
							#region ID_TEXTURE
							case Keywords.ID_TEXTURE:
								{
									var atomIndex = 1;

									var it = getNodeAt( prop.Values, 0 );

									if ( it is AtomAbstractNode )
									{
										compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line );
										return;
									}
									// Save the first atom, should be name
									var atom0 = (AtomAbstractNode)it;

									int width = 0, height = 0;
									float widthFactor = 1.0f, heightFactor = 1.0f;
									bool widthSet = false, heightSet = false, formatSet = false;
									var pooled = false;
									var hwGammaWrite = false;
									var fsaa = true;

									var scope = CompositionTechnique.TextureScope.Local;
									var formats = new List<PixelFormat>();

									while ( atomIndex < prop.Values.Count )
									{
										it = getNodeAt( prop.Values, atomIndex++ );
										if ( !(it is AtomAbstractNode) )
										{
											compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line );
											return;
										}
										var atom = (AtomAbstractNode)it;

										switch ( (Keywords)atom.Id )
										{
											case Keywords.ID_TARGET_WIDTH:
												width = 0;
												widthSet = true;
												break;

											case Keywords.ID_TARGET_HEIGHT:
												height = 0;
												heightSet = true;
												break;

											case Keywords.ID_TARGET_WIDTH_SCALED:
											case Keywords.ID_TARGET_HEIGHT_SCALED:
												{
													var pSetFlag = false;
													var pSize = 0;
													float pFactor = 0;
													if ( atom.Id == (uint)Keywords.ID_TARGET_WIDTH_SCALED )
													{
														pSetFlag = widthSet;
														pSize = width;
														pFactor = widthFactor;
													}
													else
													{
														pSetFlag = heightSet;
														pSize = height;
														pFactor = heightFactor;
													}
													// advance to next to get scaling
													it = getNodeAt( prop.Values, atomIndex++ );
													if ( it == null || !(it is AtomAbstractNode) )
													{
														compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line );
														return;
													}
													atom = (AtomAbstractNode)it;
													if ( !atom.IsNumber )
													{
														compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line );
														return;
													}

													pSize = 0;
													pFactor = atom.Number;
													pSetFlag = true;
												}
												break;

											case Keywords.ID_POOLED:
												pooled = true;
												break;

											case Keywords.ID_SCOPE_LOCAL:
												scope = CompositionTechnique.TextureScope.Local;
												break;

											case Keywords.ID_SCOPE_CHAIN:
												scope = CompositionTechnique.TextureScope.Chain;
												break;

											case Keywords.ID_SCOPE_GLOBAL:
												scope = CompositionTechnique.TextureScope.Global;
												break;

											case Keywords.ID_GAMMA:
												hwGammaWrite = true;
												break;

											case Keywords.ID_NO_FSAA:
												fsaa = false;
												break;

											default:
												if ( atom.IsNumber )
												{
													if ( atomIndex == 2 )
													{
														width = (int)atom.Number;
														widthSet = true;
													}
													else if ( atomIndex == 3 )
													{
														height = (int)atom.Number;
														heightSet = true;
													}
													else
													{
														compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line );
														return;
													}
												}
												else
												{
													// pixel format?
													var format = PixelUtil.GetFormatFromName( atom.Value, true );
													if ( format == PixelFormat.Unknown )
													{
														compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line );
														return;
													}
													formats.Add( format );
													formatSet = true;
												}
												break;
										}
									}
									if ( !widthSet || !heightSet || !formatSet )
									{
										compiler.AddError( CompileErrorCode.StringExpected, prop.File, prop.Line );
										return;
									}

									// No errors, create
									var def = _Technique.CreateTextureDefinition( atom0.Value );
									def.Width = width;
									def.Height = height;
									def.WidthFactor = widthFactor;
									def.HeightFactor = heightFactor;
									def.PixelFormats = formats;
									def.HwGammaWrite = hwGammaWrite;
									def.Fsaa = fsaa;
									def.Pooled = pooled;
									def.Scope = scope;
								}
								break;
							#endregion ID_TEXTURE

							#region ID_TEXTURE_REF
							case Keywords.ID_TEXTURE_REF:
								if ( prop.Values.Count == 0 )
								{
									compiler.AddError( CompileErrorCode.StringExpected, prop.File, prop.Line );
								}
								else if ( prop.Values.Count != 3 )
								{
									compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
										"texture_ref only supports 3 argument" );
								}
								else
								{
									string texName = string.Empty, refCompName = string.Empty, refTexName = string.Empty;

									var it = getNodeAt( prop.Values, 0 );
									if ( !getString( it, out texName ) )
									{
										compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
										"texture_ref must have 3 string arguments" );
									}

									it = getNodeAt( prop.Values, 1 );
									if ( !getString( it, out refCompName ) )
									{
										compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
										"texture_ref must have 3 string arguments" );
									}

									it = getNodeAt( prop.Values, 2 );
									if ( !getString( it, out refTexName ) )
									{
										compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
										"texture_ref must have 3 string arguments" );
									}

									var refTexDef = _Technique.CreateTextureDefinition( texName );

									refTexDef.ReferenceCompositorName = refCompName;
									refTexDef.ReferenceTextureName = refTexName;
								}
								break;
							#endregion ID_TEXTURE_REF

							#region ID_SCHEME
							case Keywords.ID_SCHEME:
								if ( prop.Values.Count == 0 )
								{
									compiler.AddError( CompileErrorCode.StringExpected, prop.File, prop.Line );
								}
								else if ( prop.Values.Count > 1 )
								{
									compiler.AddError( CompileErrorCode.FewerParametersExpected, prop.File, prop.Line,
										"scheme only supports 1 argument" );
								}
								else
								{
									var i0 = getNodeAt( prop.Values, 0 );
									var scheme = string.Empty;

									if ( getString( i0, out scheme ) )
										_Technique.SchemeName = scheme;
									else
										compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
										"scheme must have 1 string argument" );
								}
								break;
							#endregion ID_SCHEME

							#region ID_COMPOSITOR_LOGIC
							case Keywords.ID_COMPOSITOR_LOGIC:
								if ( prop.Values.Count == 0 )
								{
									compiler.AddError( CompileErrorCode.StringExpected, prop.File, prop.Line );
								}
								else if ( prop.Values.Count > 1 )
								{
									compiler.AddError( CompileErrorCode.FewerParametersExpected, prop.File, prop.Line,
										"compositor logic only supports 1 argument" );
								}
								else
								{
									var i0 = getNodeAt( prop.Values, 0 );
									var logicName = string.Empty;

									if ( getString( i0, out logicName ) )
										_Technique.CompositorLogicName = logicName;
									else
										compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
										"compositor logic must have 1 string argument" );
								}
								break;
							#endregion ID_COMPOSITOR_LOGIC

							default:
								compiler.AddError( CompileErrorCode.UnexpectedToken, prop.File, prop.Line,
									"token \"" + prop.Name + "\" is not recognized" );
								break;
						}
					}
				}
			}

			#endregion Translator Implementation
		}
	}
}

