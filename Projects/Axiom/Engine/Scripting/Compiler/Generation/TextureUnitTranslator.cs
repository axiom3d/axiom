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

using System;
using Axiom.Controllers;
using Axiom.Core;
using Axiom.Graphics;
using Axiom.Math;
using Axiom.Media;
using Axiom.Scripting.Compiler.AST;

#endregion Namespace Declarations

namespace Axiom.Scripting.Compiler
{
	public partial class ScriptCompiler
	{
		public class TextureUnitTranslator : Translator
		{
			protected TextureUnitState _textureunit;

			public TextureUnitTranslator()
				: base()
			{
				_textureunit = null;
			}

			#region Translator Implementation

			/// <see cref="Translator.CheckFor"/>
			internal override bool CheckFor( Keywords nodeId, Keywords parentId )
			{
				return nodeId == Keywords.ID_TEXTURE_UNIT && parentId == Keywords.ID_PASS;
			}

			/// <see cref="Translator.Translate"/>
			public override void Translate( ScriptCompiler compiler, AbstractNode node )
			{
				ObjectAbstractNode obj = (ObjectAbstractNode)node;

				Pass pass = (Pass)obj.Parent.Context;
				_textureunit = pass.CreateTextureUnitState();
				obj.Context = _textureunit;

				// Get the name of the technique
				if ( !string.IsNullOrEmpty( obj.Name ) )
					_textureunit.Name = obj.Name;

				// Set the properties for the material
				foreach ( AbstractNode i in obj.Children )
				{
					if ( i is PropertyAbstractNode )
					{
						PropertyAbstractNode prop = (PropertyAbstractNode)i;
						switch ( (Keywords)prop.Id )
						{
							#region ID_TEXTURE_ALIAS
							case Keywords.ID_TEXTURE_ALIAS:
								if ( prop.Values.Count == 0 )
								{
									compiler.AddError( CompileErrorCode.StringExpected, prop.File, prop.Line );
								}
								else if ( prop.Values.Count > 1 )
								{
									compiler.AddError( CompileErrorCode.FewerParametersExpected, prop.File, prop.Line,
										"texture_alias must have at most 1 argument" );
								}
								else
								{
									string val;
									if ( getString( prop.Values[ 0 ], out val ) )
										_textureunit.TextureNameAlias = val;
									else
										compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
											prop.Values[ 0 ].Value + " is not a valid texture alias" );
								}
								break;
							#endregion ID_TEXTURE_ALIAS

							#region ID_TEXTURE
							case Keywords.ID_TEXTURE:
								if ( prop.Values.Count == 0 )
								{
									compiler.AddError( CompileErrorCode.StringExpected, prop.File, prop.Line );
								}
								else if ( prop.Values.Count > 5 )
								{
									compiler.AddError( CompileErrorCode.FewerParametersExpected, prop.File, prop.Line,
										"texture must have at most 5 arguments" );
								}
								else
								{
									AbstractNode j = getNodeAt( prop.Values, 0 );
									int index = 1;
									string val;
									if ( getString( j, out val ) )
									{
										TextureType texType = TextureType.TwoD;
										bool isAlpha = false;
										bool sRGBRead = false;
										PixelFormat format = PixelFormat.Unknown;
										int mipmaps = -1;//MIP_DEFAULT;

										while ( j != null )
										{
											if ( j is AtomAbstractNode )
											{
												AtomAbstractNode atom = (AtomAbstractNode)j;
												switch ( (Keywords)atom.Id )
												{
													case Keywords.ID_1D:
														texType = TextureType.OneD;
														break;

													case Keywords.ID_2D:
														texType = TextureType.TwoD;
														break;

													case Keywords.ID_3D:
														texType = TextureType.ThreeD;
														break;

													case Keywords.ID_CUBIC:
														texType = TextureType.CubeMap;
														break;

													case Keywords.ID_UNLIMITED:
														mipmaps = 0x7FFFFFFF;//MIP_UNLIMITED;
														break;

													case Keywords.ID_ALPHA:
														isAlpha = true;
														break;

													case Keywords.ID_GAMMA:
														sRGBRead = true;
														break;

													default:
														if ( atom.IsNumber )
															mipmaps = (int)atom.Number;
														else
															format = PixelUtil.GetFormatFromName( atom.Value, true );
														break;
												}
											}
											else
											{
												compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
													j.Value + " is not a supported argument to the texture property" );
											}

											j = getNodeAt( prop.Values, index++ );
										}

										ScriptCompilerEvent evt = new ProcessResourceNameScriptCompilerEvent(
											ProcessResourceNameScriptCompilerEvent.ResourceType.Texture, val );

										compiler._fireEvent( ref evt );

										string textureName = ( (ProcessResourceNameScriptCompilerEvent)evt ).Name;

										_textureunit.SetTextureName( textureName, texType );
										_textureunit.DesiredFormat = format;
										_textureunit.IsAlpha = isAlpha;
										_textureunit.MipmapCount = mipmaps;
										_textureunit.IsHardwareGammaEnabled = sRGBRead;
									}
									else
										compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
											j.Value + " is not a valid texture name" );
								}
								break;
							#endregion ID_TEXTURE

							#region ID_ANIM_TEXTURE
							case Keywords.ID_ANIM_TEXTURE:
								if ( prop.Values.Count < 3 )
								{
									compiler.AddError( CompileErrorCode.StringExpected, prop.File, prop.Line );
								}
								else
								{
									AbstractNode i1 = getNodeAt( prop.Values, 1 );
									if ( i1 is AtomAbstractNode && ( (AtomAbstractNode)i ).IsNumber )
									{
										// Short form
										AbstractNode i0 = getNodeAt( prop.Values, 0 ), i2 = getNodeAt( prop.Values, 2 );
										if ( i0 is AtomAbstractNode )
										{
											string val0;
											int val1;
											Real val2;
											if ( getString( i0, out val0 ) && getInt( i1, out val1 ) && getReal( i2, out val2 ) )
											{
												ScriptCompilerEvent evt = new ProcessResourceNameScriptCompilerEvent(
													ProcessResourceNameScriptCompilerEvent.ResourceType.Texture, val0 );

												compiler._fireEvent( ref evt );
												string evtName = ( (ProcessResourceNameScriptCompilerEvent)evt ).Name;

												_textureunit.SetAnimatedTextureName( evtName, val1, val2 );
											}
											else
											{
												compiler.AddError( CompileErrorCode.NumberExpected, prop.File, prop.Line,
													"anim_texture short form requires a texture name, number of frames, and animation duration" );
											}
										}
										else
										{
											compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
												"anim_texture short form requires a texture name, number of frames, and animation duration" );
										}
									}
									else
									{
										// Long form has n number of frames
										Real duration = 0;
										AbstractNode inNode = getNodeAt( prop.Values, prop.Values.Count - 1 );
										if ( getReal( inNode, out duration ) )
										{
											string[] names = new string[ prop.Values.Count - 1 ];
											int n = 0;

											AbstractNode j = prop.Values[ 0 ];
											int index = 0;
											while ( j != inNode )
											{
												if ( j is AtomAbstractNode )
												{
													string name = ( (AtomAbstractNode)j ).Value;

#warning check this if statement
													// Run the name through the listener
													if ( compiler.Listener != null )
													{
														ScriptCompilerEvent evt = new ProcessResourceNameScriptCompilerEvent(
															ProcessResourceNameScriptCompilerEvent.ResourceType.Texture, name );

														compiler._fireEvent( ref evt );
														names[ n++ ] = ( (ProcessResourceNameScriptCompilerEvent)evt ).Name;
													}
													else
													{
														names[ n++ ] = name;
													}
												}
												else
													compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
														j.Value + " is not supported as a texture name" );

												++index;
												j = prop.Values[ index ];
											}

											_textureunit.SetAnimatedTextureName( names, n, duration );
										}
										else
										{
											compiler.AddError( CompileErrorCode.NumberExpected, prop.File, prop.Line,
												inNode.Value + " is not supported for the duration argument" );
										}
									}
								}
								break;
							#endregion ID_ANIM_TEXTURE

							#region ID_CUBIC_TEXTURE
							case Keywords.ID_CUBIC_TEXTURE:
								if ( prop.Values.Count == 0 )
								{
									compiler.AddError( CompileErrorCode.StringExpected, prop.File, prop.Line );
								}
								else if ( prop.Values.Count == 2 )
								{
									AbstractNode i0 = getNodeAt( prop.Values, 0 ), i1 = getNodeAt( prop.Values, 1 );
									if ( i0 is AtomAbstractNode && i1 is AtomAbstractNode )
									{
										AtomAbstractNode atom0 = (AtomAbstractNode)i0, atom1 = (AtomAbstractNode)i1;

										ScriptCompilerEvent evt = new ProcessResourceNameScriptCompilerEvent(
											ProcessResourceNameScriptCompilerEvent.ResourceType.Texture, atom0.Value );

										compiler._fireEvent( ref evt );
										string evtName = ( (ProcessResourceNameScriptCompilerEvent)evt ).Name;

										_textureunit.SetCubicTextureName( evtName, atom1.Id == (uint)Keywords.ID_COMBINED_UVW );
									}
									else
									{
										compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line );
									}
								}
								else if ( prop.Values.Count == 7 )
								{
									AbstractNode i0 = getNodeAt( prop.Values, 0 ),
										i1 = getNodeAt( prop.Values, 1 ),
										i2 = getNodeAt( prop.Values, 2 ),
										i3 = getNodeAt( prop.Values, 3 ),
										i4 = getNodeAt( prop.Values, 4 ),
										i5 = getNodeAt( prop.Values, 5 ),
										i6 = getNodeAt( prop.Values, 6 );

									if ( i0 is AtomAbstractNode && i1 is AtomAbstractNode && i2 is AtomAbstractNode &&
										i3 is AtomAbstractNode && i4 is AtomAbstractNode && i5 is AtomAbstractNode &&
										i6 is AtomAbstractNode )
									{
										AtomAbstractNode atom0 = (AtomAbstractNode)i0, atom1 = (AtomAbstractNode)i1,
											atom2 = (AtomAbstractNode)i2, atom3 = (AtomAbstractNode)i3,
											atom4 = (AtomAbstractNode)i4, atom5 = (AtomAbstractNode)i5,
											atom6 = (AtomAbstractNode)i6;

										string[] names = new string[ 6 ];
										names[ 0 ] = atom0.Value;
										names[ 1 ] = atom1.Value;
										names[ 2 ] = atom2.Value;
										names[ 3 ] = atom3.Value;
										names[ 4 ] = atom4.Value;
										names[ 5 ] = atom5.Value;

										if ( compiler.Listener != null )
										{
											// Run each name through the listener
											for ( int j = 0; j < 6; ++j )
											{
												ScriptCompilerEvent evt = new ProcessResourceNameScriptCompilerEvent(
													ProcessResourceNameScriptCompilerEvent.ResourceType.Texture, names[ j ] );

												compiler._fireEvent( ref evt );
												names[ j ] = ( (ProcessResourceNameScriptCompilerEvent)evt ).Name;
											}
										}

										_textureunit.SetCubicTextureName( names, atom6.Id == (uint)Keywords.ID_COMBINED_UVW );
									}

								}
								else
								{
									compiler.AddError( CompileErrorCode.FewerParametersExpected, prop.File, prop.Line,
										"cubic_texture must have at most 7 arguments" );
								}
								break;
							#endregion ID_CUBIC_TEXTURE

							#region ID_TEX_COORD_SET
							case Keywords.ID_TEX_COORD_SET:
								if ( prop.Values.Count == 0 )
								{
									compiler.AddError( CompileErrorCode.StringExpected, prop.File, prop.Line );
								}
								else if ( prop.Values.Count > 1 )
								{
									compiler.AddError( CompileErrorCode.FewerParametersExpected, prop.File, prop.Line,
										"tex_coord_set must have at most 1 argument" );
								}
								else
								{
									int val = 0;
									if ( getInt( prop.Values[ 0 ], out val ) )
										_textureunit.TextureCoordSet = val;
									else
										compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
											prop.Values[ 0 ].Value + " is not supported as an integer argument" );
								}
								break;
							#endregion ID_TEX_COORD_SET

							#region ID_TEX_ADDRESS_MODE
							case Keywords.ID_TEX_ADDRESS_MODE:
								if ( prop.Values.Count == 0 )
								{
									compiler.AddError( CompileErrorCode.StringExpected, prop.File, prop.Line );
								}
								else
								{
									AbstractNode i0 = getNodeAt( prop.Values, 0 ),
										i1 = getNodeAt( prop.Values, 1 ),
										i2 = getNodeAt( prop.Values, 2 );

									UVWAddressing mode = new UVWAddressing( TextureAddressing.Wrap );

									if ( i0 != null && i0 is AtomAbstractNode )
									{
										AtomAbstractNode atom = (AtomAbstractNode)i0;
										switch ( (Keywords)atom.Id )
										{
											case Keywords.ID_WRAP:
												mode.U = TextureAddressing.Wrap;
												break;

											case Keywords.ID_CLAMP:
												mode.U = TextureAddressing.Clamp;
												break;

											case Keywords.ID_MIRROR:
												mode.U = TextureAddressing.Mirror;
												break;

											case Keywords.ID_BORDER:
												mode.U = TextureAddressing.Border;
												break;

											default:
												compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
													i0.Value + " not supported as first argument (must be \"wrap\", \"clamp\", \"mirror\", or \"border\")" );
												break;
										}
									}
									mode.V = mode.U;
									mode.W = mode.U;

									if ( i1 != null && i1 is AtomAbstractNode )
									{
										AtomAbstractNode atom = (AtomAbstractNode)i1;
										switch ( (Keywords)atom.Id )
										{
											case Keywords.ID_WRAP:
												mode.V = TextureAddressing.Wrap;
												break;

											case Keywords.ID_CLAMP:
												mode.V = TextureAddressing.Clamp;
												break;

											case Keywords.ID_MIRROR:
												mode.V = TextureAddressing.Mirror;
												break;

											case Keywords.ID_BORDER:
												mode.V = TextureAddressing.Border;
												break;

											default:
												compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
													i0.Value + " not supported as second argument (must be \"wrap\", \"clamp\", \"mirror\", or \"border\")" );
												break;
										}
									}

									if ( i2 != null && i2 is AtomAbstractNode )
									{
										AtomAbstractNode atom = (AtomAbstractNode)i2;
										switch ( (Keywords)atom.Id )
										{
											case Keywords.ID_WRAP:
												mode.W = TextureAddressing.Wrap;
												break;

											case Keywords.ID_CLAMP:
												mode.W = TextureAddressing.Clamp;
												break;

											case Keywords.ID_MIRROR:
												mode.W = TextureAddressing.Mirror;
												break;

											case Keywords.ID_BORDER:
												mode.W = TextureAddressing.Border;
												break;

											default:
												compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
													i0.Value + " not supported as third argument (must be \"wrap\", \"clamp\", \"mirror\", or \"border\")" );
												break;
										}
									}

									_textureunit.SetTextureAddressingMode( mode );
								}
								break;
							#endregion ID_TEX_ADDRESS_MODE

							#region ID_TEX_BORDER_COLOUR
							case Keywords.ID_TEX_BORDER_COLOUR:
								if ( prop.Values.Count == 0 )
								{
									compiler.AddError( CompileErrorCode.NumberExpected, prop.File, prop.Line );
								}
								else
								{
									ColorEx val;
									if ( getColor( prop.Values, 0, out val ) )
										_textureunit.TextureBorderColor = val;
									else
										compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
											"tex_border_colour only accepts a colour argument" );
								}
								break;
							#endregion ID_TEX_BORDER_COLOUR

							#region ID_FILTERING
							case Keywords.ID_FILTERING:
								if ( prop.Values.Count == 0 )
								{
									compiler.AddError( CompileErrorCode.StringExpected, prop.File, prop.Line );
								}
								else if ( prop.Values.Count == 1 )
								{
									if ( prop.Values[ 0 ] is AtomAbstractNode )
									{
										AtomAbstractNode atom = (AtomAbstractNode)prop.Values[ 0 ];
										switch ( (Keywords)atom.Id )
										{
											case Keywords.ID_NONE:
												_textureunit.SetTextureFiltering( TextureFiltering.None );
												break;

											case Keywords.ID_BILINEAR:
												_textureunit.SetTextureFiltering( TextureFiltering.Bilinear );
												break;

											case Keywords.ID_TRILINEAR:
												_textureunit.SetTextureFiltering( TextureFiltering.Trilinear );
												break;

											case Keywords.ID_ANISOTROPIC:
												_textureunit.SetTextureFiltering( TextureFiltering.Anisotropic );
												break;

											default:
												compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
													prop.Values[ 0 ].Value + " not supported as first argument (must be \"none\", \"bilinear\", \"trilinear\", or \"anisotropic\")" );
												break;
										}
									}
									else
									{
										compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
											prop.Values[ 0 ].Value + " not supported as first argument (must be \"none\", \"bilinear\", \"trilinear\", or \"anisotropic\")" );
									}
								}
								else if ( prop.Values.Count == 3 )
								{
									AbstractNode i0 = getNodeAt( prop.Values, 0 ),
										i1 = getNodeAt( prop.Values, 1 ),
										i2 = getNodeAt( prop.Values, 2 );

									if ( i0 is AtomAbstractNode && i1 is AtomAbstractNode && i2 is AtomAbstractNode )
									{
										AtomAbstractNode atom0 = (AtomAbstractNode)i0,
											atom1 = (AtomAbstractNode)i1,
											atom2 = (AtomAbstractNode)i2;

										FilterOptions tmin = FilterOptions.None, tmax = FilterOptions.None, tmip = FilterOptions.None;
										switch ( (Keywords)atom0.Id )
										{
											case Keywords.ID_NONE:
												tmin = FilterOptions.None;
												break;

											case Keywords.ID_POINT:
												tmin = FilterOptions.Point;
												break;

											case Keywords.ID_LINEAR:
												tmin = FilterOptions.Linear;
												break;

											case Keywords.ID_ANISOTROPIC:
												tmin = FilterOptions.Anisotropic;
												break;

											default:
												compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
													i0.Value + " not supported as first argument (must be \"none\", \"point\", \"linear\", or \"anisotropic\")" );
												break;
										}

										switch ( (Keywords)atom1.Id )
										{
											case Keywords.ID_NONE:
												tmax = FilterOptions.None;
												break;

											case Keywords.ID_POINT:
												tmax = FilterOptions.Point;
												break;

											case Keywords.ID_LINEAR:
												tmax = FilterOptions.Linear;
												break;

											case Keywords.ID_ANISOTROPIC:
												tmax = FilterOptions.Anisotropic;
												break;

											default:
												compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
													i0.Value + " not supported as second argument (must be \"none\", \"point\", \"linear\", or \"anisotropic\")" );
												break;
										}

										switch ( (Keywords)atom2.Id )
										{
											case Keywords.ID_NONE:
												tmip = FilterOptions.None;
												break;

											case Keywords.ID_POINT:
												tmip = FilterOptions.Point;
												break;

											case Keywords.ID_LINEAR:
												tmip = FilterOptions.Linear;
												break;

											case Keywords.ID_ANISOTROPIC:
												tmip = FilterOptions.Anisotropic;
												break;

											default:
												compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
													i0.Value + " not supported as third argument (must be \"none\", \"point\", \"linear\", or \"anisotropic\")" );
												break;
										}

										_textureunit.SetTextureFiltering( tmin, tmax, tmip );
									}
									else
									{
										compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line );
									}
								}
								else
								{
									compiler.AddError( CompileErrorCode.FewerParametersExpected, prop.File, prop.Line,
										"filtering must have either 1 or 3 arguments" );
								}
								break;
							#endregion ID_FILTERING

							#region ID_MAX_ANISOTROPY
							case Keywords.ID_MAX_ANISOTROPY:
								if ( prop.Values.Count == 0 )
								{
									compiler.AddError( CompileErrorCode.NumberExpected, prop.File, prop.Line );
								}
								else if ( prop.Values.Count > 1 )
								{
									compiler.AddError( CompileErrorCode.FewerParametersExpected, prop.File, prop.Line,
										"max_anisotropy must have at most 1 argument" );
								}
								else
								{
									int val = 0;
									if ( getInt( prop.Values[ 0 ], out val ) )
										_textureunit.TextureAnisotropy = val;
									else
										compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
											prop.Values[ 0 ].Value + " is not a valid integer argument" );
								}
								break;
							#endregion ID_MAX_ANISOTROPY

							#region ID_MIPMAP_BIAS
							case Keywords.ID_MIPMAP_BIAS:
								if ( prop.Values.Count == 0 )
								{
									compiler.AddError( CompileErrorCode.NumberExpected, prop.File, prop.Line );
								}
								else if ( prop.Values.Count > 1 )
								{
									compiler.AddError( CompileErrorCode.FewerParametersExpected, prop.File, prop.Line,
										"mipmap_bias must have at most 1 argument" );
								}
								else
								{
									throw new NotImplementedException();
#if UNREACHABLE_CODE
									Real val = 0.0f;
									if ( getReal( prop.Values[ 0 ], out val ) )
									{ /*mUnit->setTextureMipmapBias(val);*/
									}
									else
										compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
											prop.Values[ 0 ].Value + " is not a valid number argument" );
#endif
								}
								break;
							#endregion ID_MIPMAP_BIAS

							#region ID_COLOR_OP
							case Keywords.ID_COLOR_OP:
								if ( prop.Values.Count == 0 )
								{
									compiler.AddError( CompileErrorCode.NumberExpected, prop.File, prop.Line );
								}
								else if ( prop.Values.Count > 1 )
								{
									compiler.AddError( CompileErrorCode.FewerParametersExpected, prop.File, prop.Line,
										"color_op must have at most 1 argument" );
								}
								else
								{
									if ( prop.Values[ 0 ] is AtomAbstractNode )
									{
										AtomAbstractNode atom = (AtomAbstractNode)prop.Values[ 0 ];
										switch ( (Keywords)atom.Id )
										{
											case Keywords.ID_REPLACE:
												_textureunit.ColorOperation = LayerBlendOperation.Replace;
												break;

											case Keywords.ID_ADD:
												_textureunit.ColorOperation = LayerBlendOperation.Add;
												break;

											case Keywords.ID_MODULATE:
												_textureunit.ColorOperation = LayerBlendOperation.Modulate;
												break;

											case Keywords.ID_ALPHA_BLEND:
												_textureunit.ColorOperation = LayerBlendOperation.AlphaBlend;
												break;

											default:
												compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
													prop.Values[ 0 ].Value + " is not a valid argument (must be \"replace\", \"add\", \"modulate\", or \"alpha_blend\")" );
												break;
										}
									}
									else
									{
										compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
											prop.Values[ 0 ].Value + " is not a valid argument (must be \"replace\", \"add\", \"modulate\", or \"alpha_blend\")" );
									}
								}
								break;
							#endregion ID_COLOR_OP

							#region ID_COLOR_OP_EX
							case Keywords.ID_COLOR_OP_EX:
								if ( prop.Values.Count < 3 )
								{
									compiler.AddError( CompileErrorCode.StringExpected, prop.File, prop.Line,
										"color_op_ex must have at least 3 arguments" );
								}
								else if ( prop.Values.Count > 10 )
								{
									compiler.AddError( CompileErrorCode.FewerParametersExpected, prop.File, prop.Line,
										"color_op_ex must have at most 10 arguments" );
								}
								else
								{
									AbstractNode i0 = getNodeAt( prop.Values, 0 ),
										i1 = getNodeAt( prop.Values, 1 ),
										i2 = getNodeAt( prop.Values, 2 );

									if ( i0 is AtomAbstractNode && i1 is AtomAbstractNode && i2 is AtomAbstractNode )
									{
										AtomAbstractNode atom0 = (AtomAbstractNode)i0,
											atom1 = (AtomAbstractNode)i1,
											atom2 = (AtomAbstractNode)i2;

										LayerBlendOperationEx op = LayerBlendOperationEx.Add;
										LayerBlendSource source1 = LayerBlendSource.Current, source2 = LayerBlendSource.Texture;
										ColorEx arg1 = ColorEx.White, arg2 = ColorEx.White;
										Real manualBlend = 0.0f;

										switch ( (Keywords)atom0.Id )
										{
											case Keywords.ID_SOURCE1:
												op = LayerBlendOperationEx.Source1;
												break;

											case Keywords.ID_SOURCE2:
												op = LayerBlendOperationEx.Source2;
												break;

											case Keywords.ID_MODULATE:
												op = LayerBlendOperationEx.Modulate;
												break;

											case Keywords.ID_MODULATE_X2:
												op = LayerBlendOperationEx.ModulateX2;
												break;

											case Keywords.ID_MODULATE_X4:
												op = LayerBlendOperationEx.ModulateX4;
												break;

											case Keywords.ID_ADD:
												op = LayerBlendOperationEx.Add;
												break;

											case Keywords.ID_ADD_SIGNED:
												op = LayerBlendOperationEx.AddSigned;
												break;

											case Keywords.ID_ADD_SMOOTH:
												op = LayerBlendOperationEx.AddSmooth;
												break;

											case Keywords.ID_SUBTRACT:
												op = LayerBlendOperationEx.Subtract;
												break;

											case Keywords.ID_BLEND_DIFFUSE_ALPHA:
												op = LayerBlendOperationEx.BlendDiffuseAlpha;
												break;

											case Keywords.ID_BLEND_TEXTURE_ALPHA:
												op = LayerBlendOperationEx.BlendTextureAlpha;
												break;

											case Keywords.ID_BLEND_CURRENT_ALPHA:
												op = LayerBlendOperationEx.BlendCurrentAlpha;
												break;

											case Keywords.ID_BLEND_MANUAL:
												op = LayerBlendOperationEx.BlendManual;
												break;

											case Keywords.ID_DOT_PRODUCT:
												op = LayerBlendOperationEx.DotProduct;
												break;

											case Keywords.ID_BLEND_DIFFUSE_COLOUR:
												op = LayerBlendOperationEx.BlendDiffuseColor;
												break;

											default:
												compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
													i0.Value + " is not a valid first argument (must be \"source1\", \"source2\", \"modulate\", \"modulate_x2\", \"modulate_x4\", \"add\", \"add_signed\", \"add_smooth\", \"subtract\", \"blend_diffuse_alpha\", \"blend_texture_alpha\", \"blend_current_alpha\", \"blend_manual\", \"dot_product\", or \"blend_diffuse_colour\")" );
												break;
										}

										switch ( (Keywords)atom1.Id )
										{
											case Keywords.ID_SRC_CURRENT:
												source1 = LayerBlendSource.Current;
												break;

											case Keywords.ID_SRC_TEXTURE:
												source1 = LayerBlendSource.Texture;
												break;

											case Keywords.ID_SRC_DIFFUSE:
												source1 = LayerBlendSource.Diffuse;
												break;

											case Keywords.ID_SRC_SPECULAR:
												source1 = LayerBlendSource.Specular;
												break;

											case Keywords.ID_SRC_MANUAL:
												source1 = LayerBlendSource.Manual;
												break;

											default:
												compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
													i1.Value + " is not a valid second argument (must be \"src_current\", \"src_texture\", \"src_diffuse\", \"src_specular\", or \"src_manual\")" );
												break;
										}

										switch ( (Keywords)atom2.Id )
										{
											case Keywords.ID_SRC_CURRENT:
												source2 = LayerBlendSource.Current;
												break;

											case Keywords.ID_SRC_TEXTURE:
												source2 = LayerBlendSource.Texture;
												break;

											case Keywords.ID_SRC_DIFFUSE:
												source2 = LayerBlendSource.Diffuse;
												break;

											case Keywords.ID_SRC_SPECULAR:
												source2 = LayerBlendSource.Specular;
												break;

											case Keywords.ID_SRC_MANUAL:
												source2 = LayerBlendSource.Manual;
												break;

											default:
												compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
													i2.Value + " is not a valid third argument (must be \"src_current\", \"src_texture\", \"src_diffuse\", \"src_specular\", or \"src_manual\")" );
												break;
										}

										if ( op == LayerBlendOperationEx.BlendManual )
										{
											AbstractNode i3 = getNodeAt( prop.Values, 3 );
											if ( i3 != null )
											{
												if ( !getReal( i3, out manualBlend ) )
													compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
														i3.Value + " is not a valid number argument" );
											}
											else
											{
												compiler.AddError( CompileErrorCode.NumberExpected, prop.File, prop.Line,
													"fourth argument expected when blend_manual is used" );
											}
										}

										AbstractNode j = getNodeAt( prop.Values, 3 );
										int index = 3;
										if ( op == LayerBlendOperationEx.BlendManual )
											j = getNodeAt( prop.Values, ++index );

										if ( source1 == LayerBlendSource.Manual )
										{
											if ( j != null )
											{
												if ( !getColor( prop.Values, 3, out arg1, 3 ) )
													compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
														"valid colour expected when src_manual is used" );
											}
											else
											{
												compiler.AddError( CompileErrorCode.NumberExpected, prop.File, prop.Line,
													"valid colour expected when src_manual is used" );
											}
										}

										if ( source2 == LayerBlendSource.Manual )
										{
											if ( j != null )
											{
												if ( !getColor( prop.Values, 3, out arg2, 3 ) )
													compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
														"valid colour expected when src_manual is used" );
											}
											else
											{
												compiler.AddError( CompileErrorCode.NumberExpected, prop.File, prop.Line,
													"valid colour expected when src_manual is used" );
											}
										}

										_textureunit.SetColorOperationEx( op, source1, source2, arg1, arg2, manualBlend );
									}
									else
									{
										compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line );
									}
								}
								break;
							#endregion ID_COLOR_OP_EX

							#region ID_COLOR_OP_MULTIPASS_FALLBACK
							case Keywords.ID_COLOR_OP_MULTIPASS_FALLBACK:
								if ( prop.Values.Count == 0 )
								{
									compiler.AddError( CompileErrorCode.StringExpected, prop.File, prop.Line );
								}
								else if ( prop.Values.Count > 2 )
								{
									compiler.AddError( CompileErrorCode.FewerParametersExpected, prop.File, prop.Line,
										"color_op_multiplass_fallback must have at most 2 arguments" );
								}
								else if ( prop.Values.Count == 1 )
								{
									if ( prop.Values[ 0 ] is AtomAbstractNode )
									{
										AtomAbstractNode atom = (AtomAbstractNode)prop.Values[ 0 ];
										switch ( (Keywords)atom.Id )
										{
											case Keywords.ID_ADD:
												_textureunit.SetColorOpMultipassFallback( SceneBlendFactor.One, SceneBlendFactor.One );
												break;

											case Keywords.ID_MODULATE:
												_textureunit.SetColorOpMultipassFallback( SceneBlendFactor.DestColor, SceneBlendFactor.Zero );
												break;

											case Keywords.ID_COLOUR_BLEND:
												_textureunit.SetColorOpMultipassFallback( SceneBlendFactor.SourceColor, SceneBlendFactor.OneMinusSourceColor );
												break;

											case Keywords.ID_ALPHA_BLEND:
												_textureunit.SetColorOpMultipassFallback( SceneBlendFactor.SourceAlpha, SceneBlendFactor.OneMinusSourceAlpha );
												break;

											case Keywords.ID_REPLACE:
												_textureunit.SetColorOpMultipassFallback( SceneBlendFactor.One, SceneBlendFactor.Zero );
												break;

											default:
												compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
													"argument must be a valid scene blend type (add, modulate, colour_blend, alpha_blend, or replace)" );
												break;
										}
									}
									else
									{
										compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
											"argument must be a valid scene blend type (add, modulate, colour_blend, alpha_blend, or replace)" );
									}
								}
								else
								{
									AbstractNode i0 = getNodeAt( prop.Values, 0 ), i1 = getNodeAt( prop.Values, 1 );
									SceneBlendFactor sbf0, sbf1;
									if ( getEnumeration<SceneBlendFactor>( i0, compiler, out sbf0 ) && getEnumeration<SceneBlendFactor>( i1, compiler, out sbf1 ) )
										_textureunit.SetColorOpMultipassFallback( sbf0, sbf1 );
									else
										compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
											"arguments must be valid scene blend factors" );
								}
								break;
							#endregion ID_COLOR_OP_MULTIPASS_FALLBACK

							#region ID_ALPHA_OP_EX
							case Keywords.ID_ALPHA_OP_EX:
								if ( prop.Values.Count < 3 )
								{
									compiler.AddError( CompileErrorCode.StringExpected, prop.File, prop.Line,
										"alpha_op_ex must have at least 3 arguments" );
								}
								else if ( prop.Values.Count > 6 )
								{
									compiler.AddError( CompileErrorCode.FewerParametersExpected, prop.File, prop.Line,
										"alpha_op_ex must have at most 6 arguments" );
								}
								else
								{
									AbstractNode i0 = getNodeAt( prop.Values, 0 ),
										i1 = getNodeAt( prop.Values, 1 ),
										i2 = getNodeAt( prop.Values, 2 );

									if ( i0 is AtomAbstractNode && i1 is AtomAbstractNode && i2 is AtomAbstractNode )
									{
										AtomAbstractNode atom0 = (AtomAbstractNode)i0,
											atom1 = (AtomAbstractNode)i1,
											atom2 = (AtomAbstractNode)i2;

										LayerBlendOperationEx op = LayerBlendOperationEx.Add;
										LayerBlendSource source1 = LayerBlendSource.Current, source2 = LayerBlendSource.Texture;
										Real arg1 = 0.0f, arg2 = 0.0f;
										Real manualBlend = 0.0f;

										switch ( (Keywords)atom0.Id )
										{
											case Keywords.ID_SOURCE1:
												op = LayerBlendOperationEx.Source1;
												break;

											case Keywords.ID_SOURCE2:
												op = LayerBlendOperationEx.Source2;
												break;

											case Keywords.ID_MODULATE:
												op = LayerBlendOperationEx.Modulate;
												break;

											case Keywords.ID_MODULATE_X2:
												op = LayerBlendOperationEx.ModulateX2;
												break;

											case Keywords.ID_MODULATE_X4:
												op = LayerBlendOperationEx.ModulateX4;
												break;

											case Keywords.ID_ADD:
												op = LayerBlendOperationEx.Add;
												break;

											case Keywords.ID_ADD_SIGNED:
												op = LayerBlendOperationEx.AddSigned;
												break;

											case Keywords.ID_ADD_SMOOTH:
												op = LayerBlendOperationEx.AddSmooth;
												break;

											case Keywords.ID_SUBTRACT:
												op = LayerBlendOperationEx.Subtract;
												break;

											case Keywords.ID_BLEND_DIFFUSE_ALPHA:
												op = LayerBlendOperationEx.BlendDiffuseAlpha;
												break;

											case Keywords.ID_BLEND_TEXTURE_ALPHA:
												op = LayerBlendOperationEx.BlendTextureAlpha;
												break;

											case Keywords.ID_BLEND_CURRENT_ALPHA:
												op = LayerBlendOperationEx.BlendCurrentAlpha;
												break;

											case Keywords.ID_BLEND_MANUAL:
												op = LayerBlendOperationEx.BlendManual;
												break;

											case Keywords.ID_DOT_PRODUCT:
												op = LayerBlendOperationEx.DotProduct;
												break;

											case Keywords.ID_BLEND_DIFFUSE_COLOUR:
												op = LayerBlendOperationEx.BlendDiffuseColor;
												break;

											default:
												compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
													i0.Value + " is not a valid first argument (must be \"source1\", \"source2\", \"modulate\", \"modulate_x2\", \"modulate_x4\", \"add\", \"add_signed\", \"add_smooth\", \"subtract\", \"blend_diffuse_alpha\", \"blend_texture_alpha\", \"blend_current_alpha\", \"blend_manual\", \"dot_product\", or \"blend_diffuse_colour\")" );
												break;
										}

										switch ( (Keywords)atom1.Id )
										{
											case Keywords.ID_SRC_CURRENT:
												source1 = LayerBlendSource.Current;
												break;

											case Keywords.ID_SRC_TEXTURE:
												source1 = LayerBlendSource.Texture;
												break;

											case Keywords.ID_SRC_DIFFUSE:
												source1 = LayerBlendSource.Diffuse;
												break;

											case Keywords.ID_SRC_SPECULAR:
												source1 = LayerBlendSource.Specular;
												break;

											case Keywords.ID_SRC_MANUAL:
												source1 = LayerBlendSource.Manual;
												break;

											default:
												compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
													i1.Value + " is not a valid second argument (must be \"src_current\", \"src_texture\", \"src_diffuse\", \"src_specular\", or \"src_manual\")" );
												break;
										}

										switch ( (Keywords)atom2.Id )
										{
											case Keywords.ID_SRC_CURRENT:
												source2 = LayerBlendSource.Current;
												break;

											case Keywords.ID_SRC_TEXTURE:
												source2 = LayerBlendSource.Texture;
												break;

											case Keywords.ID_SRC_DIFFUSE:
												source2 = LayerBlendSource.Diffuse;
												break;

											case Keywords.ID_SRC_SPECULAR:
												source2 = LayerBlendSource.Specular;
												break;

											case Keywords.ID_SRC_MANUAL:
												source2 = LayerBlendSource.Manual;
												break;

											default:
												compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
													i2.Value + " is not a valid third argument (must be \"src_current\", \"src_texture\", \"src_diffuse\", \"src_specular\", or \"src_manual\")" );
												break;
										}

										if ( op == LayerBlendOperationEx.BlendManual )
										{
											AbstractNode i3 = getNodeAt( prop.Values, 3 );
											if ( i3 != null )
											{
												if ( !getReal( i3, out manualBlend ) )
													compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
														"valid number expected when blend_manual is used" );
											}
											else
											{
												compiler.AddError( CompileErrorCode.NumberExpected, prop.File, prop.Line,
													"valid number expected when blend_manual is used" );
											}
										}

										AbstractNode j = getNodeAt( prop.Values, 3 );
										int index = 3;
										if ( op == LayerBlendOperationEx.BlendManual )
											j = getNodeAt( prop.Values, ++index );

										if ( source1 == LayerBlendSource.Manual )
										{
											if ( j != null )
											{
												if ( !getReal( j, out arg1 ) )
													compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
														"valid colour expected when src_manual is used" );
												else
													j = getNodeAt( prop.Values, ++index );
											}
											else
											{
												compiler.AddError( CompileErrorCode.NumberExpected, prop.File, prop.Line,
													"valid colour expected when src_manual is used" );
											}
										}

										if ( source2 == LayerBlendSource.Manual )
										{
											if ( j != null )
											{
												if ( !getReal( j, out arg2 ) )
													compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
														"valid colour expected when src_manual is used" );
											}
											else
											{
												compiler.AddError( CompileErrorCode.NumberExpected, prop.File, prop.Line,
													"valid colour expected when src_manual is used" );
											}
										}

										_textureunit.SetAlphaOperation( op, source1, source2, arg1, arg2, manualBlend );
									}
									else
									{
										compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line );
									}
								}
								break;
							#endregion ID_ALPHA_OP_EX

							#region ID_ENV_MAP
							case Keywords.ID_ENV_MAP:
								if ( prop.Values.Count == 0 )
								{
									compiler.AddError( CompileErrorCode.StringExpected, prop.File, prop.Line );
								}
								else if ( prop.Values.Count > 1 )
								{
									compiler.AddError( CompileErrorCode.FewerParametersExpected, prop.File, prop.Line,
										"env_map must have at most 1 argument" );
								}
								else
								{
									if ( prop.Values[ 0 ] is AtomAbstractNode )
									{
										AtomAbstractNode atom = (AtomAbstractNode)prop.Values[ 0 ];
										switch ( atom.Id )
										{
											case (uint)BuiltIn.ID_OFF:
												_textureunit.SetEnvironmentMap( false );
												break;

											case (uint)Keywords.ID_SPHERICAL:
												_textureunit.SetEnvironmentMap( true, EnvironmentMap.Curved );
												break;

											case (uint)Keywords.ID_PLANAR:
												_textureunit.SetEnvironmentMap( true, EnvironmentMap.Planar );
												break;

											case (uint)Keywords.ID_CUBIC_REFLECTION:
												_textureunit.SetEnvironmentMap( true, EnvironmentMap.Reflection );
												break;

											case (uint)Keywords.ID_CUBIC_NORMAL:
												_textureunit.SetEnvironmentMap( true, EnvironmentMap.Normal );
												break;

											default:
												compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
													prop.Values[ 0 ].Value + " is not a valid argument (must be \"off\", \"spherical\", \"planar\", \"cubic_reflection\", or \"cubic_normal\")" );
												break;
										}
									}
									else
									{
										compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
											prop.Values[ 0 ].Value + " is not a valid argument (must be \"off\", \"spherical\", \"planar\", \"cubic_reflection\", or \"cubic_normal\")" );
									}
								}
								break;
							#endregion ID_ENV_MAP

							#region ID_SCROLL
							case Keywords.ID_SCROLL:
								if ( prop.Values.Count == 0 )
								{
									compiler.AddError( CompileErrorCode.NumberExpected, prop.File, prop.Line );
								}
								else if ( prop.Values.Count > 2 )
								{
									compiler.AddError( CompileErrorCode.FewerParametersExpected, prop.File, prop.Line,
										"scroll must have at most 2 arguments" );
								}
								else
								{
									AbstractNode i0 = getNodeAt( prop.Values, 0 ), i1 = getNodeAt( prop.Values, 1 );
									Real x, y;
									if ( getReal( i0, out x ) && getReal( i1, out y ) )
										_textureunit.SetTextureScroll( x, y );
									else
										compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
											i0.Value + " and/or " + i1.Value + " is invalid; both must be numbers" );
								}
								break;
							#endregion ID_SCROLL

							#region ID_SCROLL_ANIM
							case Keywords.ID_SCROLL_ANIM:
								if ( prop.Values.Count == 0 )
								{
									compiler.AddError( CompileErrorCode.NumberExpected, prop.File, prop.Line );
								}
								else if ( prop.Values.Count > 2 )
								{
									compiler.AddError( CompileErrorCode.FewerParametersExpected, prop.File, prop.Line,
										"scroll_anim must have at most 2 arguments" );
								}
								else
								{
									AbstractNode i0 = getNodeAt( prop.Values, 0 ), i1 = getNodeAt( prop.Values, 1 );
									Real x, y;
									if ( getReal( i0, out x ) && getReal( i1, out y ) )
										_textureunit.SetScrollAnimation( x, y );
									else
										compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
											i0.Value + " and/or " + i1.Value + " is invalid; both must be numbers" );
								}
								break;
							#endregion ID_SCROLL_ANIM

							#region ID_ROTATE
							case Keywords.ID_ROTATE:
								if ( prop.Values.Count == 0 )
								{
									compiler.AddError( CompileErrorCode.NumberExpected, prop.File, prop.Line );
								}
								else if ( prop.Values.Count > 1 )
								{
									compiler.AddError( CompileErrorCode.FewerParametersExpected, prop.File, prop.Line,
										"rotate must have at most 1 argument" );
								}
								else
								{
									Real angle;
									if ( getReal( prop.Values[ 0 ], out angle ) )
#warning check this statement
										//mUnit->setTextureRotate(Degree(angle));
										_textureunit.SetTextureRotate( angle );
									else
										compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
											prop.Values[ 0 ].Value + " is not a valid number value" );
								}
								break;
							#endregion ID_ROTATE

							#region ID_ROTATE_ANIM
							case Keywords.ID_ROTATE_ANIM:
								if ( prop.Values.Count == 0 )
								{
									compiler.AddError( CompileErrorCode.NumberExpected, prop.File, prop.Line );
								}
								else if ( prop.Values.Count > 1 )
								{
									compiler.AddError( CompileErrorCode.FewerParametersExpected, prop.File, prop.Line,
										"rotate_anim must have at most 1 argument" );
								}
								else
								{
									Real angle;
									if ( getReal( prop.Values[ 0 ], out angle ) )
										_textureunit.SetRotateAnimation( angle );
									else
										compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
											prop.Values[ 0 ].Value + " is not a valid number value" );
								}
								break;
							#endregion ID_ROTATE_ANIM

							#region ID_SCALE
							case Keywords.ID_SCALE:
								if ( prop.Values.Count == 0 )
								{
									compiler.AddError( CompileErrorCode.NumberExpected, prop.File, prop.Line );
								}
								else if ( prop.Values.Count > 2 )
								{
									compiler.AddError( CompileErrorCode.FewerParametersExpected, prop.File, prop.Line,
										"scale must have at most 2 arguments" );
								}
								else
								{
									AbstractNode i0 = getNodeAt( prop.Values, 0 ), i1 = getNodeAt( prop.Values, 1 );
									Real x, y;
									if ( getReal( i0, out x ) && getReal( i1, out y ) )
										_textureunit.SetTextureScale( x, y );
									else
										compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
										"first and second arguments must both be valid number values (received " + i0.Value + ", " + i1.Value + ")" );
								}
								break;
							#endregion ID_SCALE

							#region ID_WAVE_XFORM
							case Keywords.ID_WAVE_XFORM:
								if ( prop.Values.Count == 0 )
								{
									compiler.AddError( CompileErrorCode.NumberExpected, prop.File, prop.Line );
								}
								else if ( prop.Values.Count > 6 )
								{
									compiler.AddError( CompileErrorCode.FewerParametersExpected, prop.File, prop.Line,
										"wave_xform must have at most 6 arguments" );
								}
								else
								{
									AbstractNode i0 = getNodeAt( prop.Values, 0 ), i1 = getNodeAt( prop.Values, 1 ),
										i2 = getNodeAt( prop.Values, 2 ), i3 = getNodeAt( prop.Values, 3 ),
										i4 = getNodeAt( prop.Values, 4 ), i5 = getNodeAt( prop.Values, 5 );

									if ( i0 is AtomAbstractNode && i1 is AtomAbstractNode && i2 is AtomAbstractNode &&
										i3 is AtomAbstractNode && i4 is AtomAbstractNode && i5 is AtomAbstractNode )
									{
										AtomAbstractNode atom0 = (AtomAbstractNode)i0, atom1 = (AtomAbstractNode)i1;
										TextureTransform type = TextureTransform.Rotate;
										WaveformType wave = WaveformType.Sine;
										Real baseVal = 0.0f, freq = 0.0f, phase = 0.0f, amp = 0.0f;

										switch ( (Keywords)atom0.Id )
										{
											case Keywords.ID_SCROLL_X:
												type = TextureTransform.TranslateU;
												break;

											case Keywords.ID_SCROLL_Y:
												type = TextureTransform.TranslateV;
												break;

											case Keywords.ID_SCALE_X:
												type = TextureTransform.ScaleU;
												break;

											case Keywords.ID_SCALE_Y:
												type = TextureTransform.ScaleV;
												break;

											case Keywords.ID_ROTATE:
												type = TextureTransform.Rotate;
												break;

											default:
												compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
													atom0.Value + " is not a valid transform type (must be \"scroll_x\", \"scroll_y\", \"scale_x\", \"scale_y\", or \"rotate\")" );
												break;
										}

										switch ( (Keywords)atom1.Id )
										{
											case Keywords.ID_SINE:
												wave = WaveformType.Sine;
												break;

											case Keywords.ID_TRIANGLE:
												wave = WaveformType.Triangle;
												break;

											case Keywords.ID_SQUARE:
												wave = WaveformType.Square;
												break;

											case Keywords.ID_SAWTOOTH:
												wave = WaveformType.Sawtooth;
												break;

											case Keywords.ID_INVERSE_SAWTOOTH:
												wave = WaveformType.InverseSawtooth;
												break;

											default:
												compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
													atom1.Value + " is not a valid waveform type (must be \"sine\", \"triangle\", \"square\", \"sawtooth\", or \"inverse_sawtooth\")" );
												break;
										}

										if ( !getReal( i2, out baseVal ) || !getReal( i3, out freq ) || !getReal( i4, out phase ) || !getReal( i5, out amp ) )
											compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
												"arguments 3, 4, 5, and 6 must be valid numbers; received " + i2.Value + ", " + i3.Value + ", " + i4.Value + ", " + i5.Value );

										_textureunit.SetTransformAnimation( type, wave, baseVal, freq, phase, amp );
									}
									else
									{
										compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line );
									}
								}
								break;
							#endregion ID_WAVE_XFORM

							#region ID_TRANSFORM
							case Keywords.ID_TRANSFORM:
						    {
						        throw new NotImplementedException();
#if UNREACHABLE_CODE
									Matrix4 m;
									if ( getMatrix4( prop.Values, 0, out m ) )
									{ /*mUnit->setTextureTransform(m);*/
									}
									else
										compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line );
								}
								break;
#else
						    }
#endif

						        #endregion ID_TRANSFORM

							#region ID_BINDING_TYPE
							case Keywords.ID_BINDING_TYPE:
								if ( prop.Values.Count == 0 )
								{
									compiler.AddError( CompileErrorCode.NumberExpected, prop.File, prop.Line );
								}
								else if ( prop.Values.Count > 1 )
								{
									compiler.AddError( CompileErrorCode.FewerParametersExpected, prop.File, prop.Line,
										"binding_type must have at most 1 argument" );
								}
								else
								{
									if ( prop.Values[ 0 ] is AtomAbstractNode )
									{
										AtomAbstractNode atom = (AtomAbstractNode)prop.Values[ 0 ];
										switch ( (Keywords)atom.Id )
										{
											case Keywords.ID_VERTEX:
												_textureunit.BindingType = TextureBindingType.Vertex;
												break;

											case Keywords.ID_FRAGMENT:
												_textureunit.BindingType = TextureBindingType.Fragment;
												break;

											default:
												compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
													atom.Value + " is not a valid binding type (must be \"vertex\" or \"fragment\")" );
												break;
										}
									}
									else
									{
										compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
											prop.Values[ 0 ].Value + " is not a valid binding type" );
									}
								}
								break;
							#endregion ID_BINDING_TYPE

							#region ID_CONTENT_TYPE
							case Keywords.ID_CONTENT_TYPE:
								if ( prop.Values.Count == 0 )
								{
									compiler.AddError( CompileErrorCode.NumberExpected, prop.File, prop.Line );
								}
								else if ( prop.Values.Count > 4 )
								{
									compiler.AddError( CompileErrorCode.FewerParametersExpected, prop.File, prop.Line,
										"content_type must have at most 4 arguments" );
								}
								else
								{
									if ( prop.Values[ 0 ] is AtomAbstractNode )
									{
										throw new NotImplementedException();
#if UNREACHABLE_CODE
										AtomAbstractNode atom = (AtomAbstractNode)prop.Values[ 0 ];
										switch ( (Keywords)atom.Id )
										{
											case Keywords.ID_NAMED:
												//mUnit->setContentType(TextureUnitState::CONTENT_NAMED);
												break;

											case Keywords.ID_SHADOW:
												//mUnit->setContentType(TextureUnitState::CONTENT_SHADOW);
												break;

											case Keywords.ID_COMPOSITOR:
												//mUnit->setContentType(TextureUnitState::CONTENT_COMPOSITOR);
												if ( prop.Values.Count >= 3 )
												{
													string compositorName;
													getString( getNodeAt( prop.Values, 1 ), out compositorName );
													string textureName;
													getString( getNodeAt( prop.Values, 2 ), out textureName );

													if ( prop.Values.Count == 4 )
													{
														uint mrtIndex;
														getUInt( getNodeAt( prop.Values, 3 ), out mrtIndex );
														//mUnit->setCompositorReference(compositorName, textureName, mrtIndex);
													}
													else
													{
														//mUnit->setCompositorReference(compositorName, textureName);
													}
												}
												else
												{
													compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
														"content_type compositor must have an additional 2 or 3 parameters" );
												}

												break;
											default:
												compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
													atom.Value + " is not a valid content type (must be \"named\" or \"shadow\" or \"compositor\")" );
												break;
										}
#endif
									}
									else
									{
										compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
											prop.Values[ 0 ].Value + " is not a valid content type" );
									}
								}
								break;
							#endregion ID_CONTENT_TYPE

							default:
								compiler.AddError( CompileErrorCode.UnexpectedToken, prop.File, prop.Line,
									"token \"" + prop.Name + "\" is not recognized" );
								break;
						}
					}
					else if ( i is ObjectAbstractNode )
					{
						_processNode( compiler, i );
					}
				}
			}

			#endregion Translator Implementation
		}
	}
}
