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
using Axiom.Core;
using Axiom.Graphics;
using Axiom.Math;
using Axiom.Scripting.Compiler.AST;
using System;

#endregion Namespace Declarations

namespace Axiom.Scripting.Compiler
{
	public partial class ScriptCompiler
	{
		public class PassTranslator : Translator
		{
			protected Pass _pass;

			public PassTranslator()
				: base()
			{
				_pass = null;
			}

			#region Translator Implementation

			/// <see cref="Translator.CheckFor"/>
			internal override bool CheckFor( Keywords nodeId, Keywords parentId )
			{
				return nodeId == Keywords.ID_PASS && parentId == Keywords.ID_TECHNIQUE;
			}

			/// <see cref="Translator.Translate"/>
			public override void Translate( ScriptCompiler compiler, AbstractNode node )
			{
				ObjectAbstractNode obj = (ObjectAbstractNode)node;

				Technique technique = (Technique)obj.Parent.Context;
				_pass = technique.CreatePass();
				obj.Context = _pass;

				// Get the name of the technique
				if ( !string.IsNullOrEmpty( obj.Name ) )
					_pass.Name = obj.Name;

				// Set the properties for the material
				foreach ( AbstractNode i in obj.Children )
				{
					if ( i is PropertyAbstractNode )
					{
						PropertyAbstractNode prop = (PropertyAbstractNode)i;

						switch ( (Keywords)prop.Id )
						{
							#region ID_AMBIENT
							case Keywords.ID_AMBIENT:
								if ( prop.Values.Count == 0 )
								{
									compiler.AddError( CompileErrorCode.NumberExpected, prop.File, prop.Line );
								}
								else if ( prop.Values.Count > 4 )
								{
									compiler.AddError( CompileErrorCode.FewerParametersExpected, prop.File, prop.Line,
										"ambient must have at most 4 parameters" );
								}
								else
								{
									if ( prop.Values[ 0 ] is AtomAbstractNode &&
										( (AtomAbstractNode)prop.Values[ 0 ] ).Id == (uint)Keywords.ID_VERTEX_COLOUR )
									{
										_pass.VertexColorTracking = _pass.VertexColorTracking | TrackVertexColor.Ambient;
									}
									else
									{
										ColorEx val = ColorEx.White;
										if ( getColor( prop.Values, 0, out val ) )
											_pass.Ambient = val;
										else
											compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
												"ambient requires 3 or 4 colour arguments, or a \"vertexcolour\" directive" );
									}
								}
								break;
							#endregion ID_AMBIENT

							#region ID_DIFFUSE
							case Keywords.ID_DIFFUSE:
								if ( prop.Values.Count == 0 )
								{
									compiler.AddError( CompileErrorCode.NumberExpected, prop.File, prop.Line );
								}
								else if ( prop.Values.Count > 4 )
								{
									compiler.AddError( CompileErrorCode.FewerParametersExpected, prop.File, prop.Line,
										"diffuse must have at most 4 arguments" );
								}
								else
								{
									if ( prop.Values[ 0 ] is AtomAbstractNode &&
										( (AtomAbstractNode)prop.Values[ 0 ] ).Id == (uint)Keywords.ID_VERTEX_COLOUR )
									{
										_pass.VertexColorTracking = _pass.VertexColorTracking | TrackVertexColor.Diffuse;
									}
									else
									{
										ColorEx val;
										if ( getColor( prop.Values, 0, out val ) )
											_pass.Diffuse = val;
										else
											compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
												"diffuse requires 3 or 4 colour arguments, or a \"vertexcolour\" directive" );
									}
								}
								break;
							#endregion ID_DIFFUSE

							#region ID_SPECULAR
							case Keywords.ID_SPECULAR:
								if ( prop.Values.Count == 0 )
								{
									compiler.AddError( CompileErrorCode.NumberExpected, prop.File, prop.Line );
								}
								else if ( prop.Values.Count > 5 )
								{
									compiler.AddError( CompileErrorCode.FewerParametersExpected, prop.File, prop.Line,
										"specular must have at most 5 arguments" );
								}
								else
								{
									if ( prop.Values[ 0 ] is AtomAbstractNode &&
										( (AtomAbstractNode)prop.Values[ 0 ] ).Id == (uint)Keywords.ID_VERTEX_COLOUR )
									{
										_pass.VertexColorTracking = _pass.VertexColorTracking | TrackVertexColor.Specular;

										if ( prop.Values.Count >= 2 )
										{
											Real val = 0;
											if ( getReal( prop.Values[ prop.Values.Count - 1 ], out val ) )
												_pass.Shininess = val;
											else
											{
												compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
													"specular does not support \"" + prop.Values[ prop.Values.Count - 1 ].Value + "\" as its second argument" );
											}
										}
									}
									else
									{
										if ( prop.Values.Count < 4 )
										{
											compiler.AddError( CompileErrorCode.NumberExpected, prop.File, prop.Line,
												"specular expects at least 4 arguments" );
										}
										else
										{
											AbstractNode i0 = getNodeAt( prop.Values, 0 ),
											i1 = getNodeAt( prop.Values, 1 ),
											i2 = getNodeAt( prop.Values, 2 );
											ColorEx val = new ColorEx( 0, 0, 0, 0 );
											if ( getFloat( i0, out val.r ) && getFloat( i1, out val.g ) && getFloat( i2, out val.b ) )
											{
												if ( prop.Values.Count == 4 )
												{
													_pass.Specular = val;

													AbstractNode i3 = getNodeAt( prop.Values, 3 );

													Real shininess = 0.0f;
													if ( getReal( i3, out shininess ) )
														_pass.Shininess = shininess;
													else
														compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
															 "specular fourth argument must be a valid number for shininess attribute" );
												}
												else
												{
													AbstractNode i3 = getNodeAt( prop.Values, 3 );
													if ( !getFloat( i3, out val.a ) )
													{
														compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
															"specular fourth argument must be a valid color component value" );
													}
													else
														_pass.Specular = val;

													AbstractNode i4 = getNodeAt( prop.Values, 4 );

													Real shininess = 0.0f;
													if ( getReal( i4, out shininess ) )
														_pass.Shininess = shininess;
													else
													{
														compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
															"specular fourth argument must be a valid number for shininess attribute" );
													}
												}
											}
											else
											{
												compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
													"specular must have first 3 arguments be a valid colour" );
											}
										}
									}
								}
								break;
							#endregion ID_SPECULAR

							#region ID_EMISSIVE
							case Keywords.ID_EMISSIVE:
								if ( prop.Values.Count == 0 )
								{
									compiler.AddError( CompileErrorCode.NumberExpected, prop.File, prop.Line );
								}
								else if ( prop.Values.Count > 4 )
								{
									compiler.AddError( CompileErrorCode.FewerParametersExpected, prop.File, prop.Line,
										"emissive must have at most 4 arguments" );
								}
								else
								{
									if ( prop.Values[ 0 ] is AtomAbstractNode &&
													( (AtomAbstractNode)prop.Values[ 0 ] ).Id == (uint)Keywords.ID_VERTEX_COLOUR )
									{
										_pass.VertexColorTracking = _pass.VertexColorTracking | TrackVertexColor.Emissive;
									}
									else
									{
										ColorEx val;
										if ( getColor( prop.Values, 0, out val ) )
											_pass.SelfIllumination = val;
										else
											compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
												"emissive requires 3 or 4 colour arguments, or a \"vertexcolour\" directive" );
									}
								}
								break;
							#endregion ID_EMISSIVE

							#region ID_SCENE_BLEND
							case Keywords.ID_SCENE_BLEND:
								if ( prop.Values.Count == 0 )
								{
									compiler.AddError( CompileErrorCode.StringExpected, prop.File, prop.Line );
								}
								else if ( prop.Values.Count > 2 )
								{
									compiler.AddError( CompileErrorCode.FewerParametersExpected, prop.File, prop.Line,
										"scene_blend supports at most 2 arguments" );
								}
								else if ( prop.Values.Count == 1 )
								{
									if ( prop.Values[ 0 ] is AtomAbstractNode )
									{
										AtomAbstractNode atom = (AtomAbstractNode)prop.Values[ 0 ];
										switch ( (Keywords)atom.Id )
										{
											case Keywords.ID_ADD:
												_pass.SetSceneBlending( SceneBlendType.Add );
												break;

											case Keywords.ID_MODULATE:
												_pass.SetSceneBlending( SceneBlendType.Modulate );
												break;

											case Keywords.ID_COLOUR_BLEND:
												_pass.SetSceneBlending( SceneBlendType.TransparentColor );

												break;

											case Keywords.ID_ALPHA_BLEND:
												_pass.SetSceneBlending( SceneBlendType.TransparentAlpha );
												break;

											default:
												compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
													"scene_blend does not support \"" + prop.Values[ 0 ].Value + "\" for argument 1" );
												break;
										}
									}
									else
									{
										compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
											"scene_blend does not support \"" + prop.Values[ 0 ].Value + "\" for argument 1" );
									}
								}
								else
								{
									AbstractNode i0 = getNodeAt( prop.Values, 0 ), i1 = getNodeAt( prop.Values, 1 );
									SceneBlendFactor sbf0, sbf1;
									if ( getEnumeration<SceneBlendFactor>( i0, compiler, out sbf0 ) && getEnumeration<SceneBlendFactor>( i1, compiler, out sbf1 ) )
									{
										_pass.SetSceneBlending( sbf0, sbf1 );
									}
									else
									{
										compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
											"scene_blend does not support \"" + i0.Value + "\" and \"" + i1.Value + "\" as arguments" );
									}
								}
								break;
							#endregion ID_SCENE_BLEND

							#region ID_SEPARATE_SCENE_BLEND
							case Keywords.ID_SEPARATE_SCENE_BLEND:
								if ( prop.Values.Count == 0 )
								{
									compiler.AddError( CompileErrorCode.StringExpected, prop.File, prop.Line );
								}
								else if ( prop.Values.Count == 3 )
								{
									compiler.AddError( CompileErrorCode.FewerParametersExpected, prop.File, prop.Line,
										"separate_scene_blend must have 2 or 4 arguments" );
								}
								else if ( prop.Values.Count > 4 )
								{
									compiler.AddError( CompileErrorCode.FewerParametersExpected, prop.File, prop.Line,
										"separate_scene_blend must have 2 or 4 arguments" );
								}
								else if ( prop.Values.Count == 2 )
								{
									AbstractNode i0 = getNodeAt( prop.Values, 0 ), i1 = getNodeAt( prop.Values, 1 );
									if ( i0 is AtomAbstractNode && i1 is AtomAbstractNode )
									{
										AtomAbstractNode atom0 = (AtomAbstractNode)i0, atom1 = (AtomAbstractNode)i1;
										SceneBlendType sbt0, sbt1;
										switch ( (Keywords)atom0.Id )
										{
											case Keywords.ID_ADD:
												sbt0 = SceneBlendType.Add;
												break;

											case Keywords.ID_MODULATE:
												sbt0 = SceneBlendType.Modulate;
												break;

											case Keywords.ID_COLOUR_BLEND:
												sbt0 = SceneBlendType.TransparentColor;
												break;

											case Keywords.ID_ALPHA_BLEND:
												sbt0 = SceneBlendType.TransparentAlpha;
												break;

											default:
												compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
													"separate_scene_blend does not support \"" + atom0.Value + "\" as argument 1" );
												return;
										}

										switch ( (Keywords)atom1.Id )
										{
											case Keywords.ID_ADD:
												sbt1 = SceneBlendType.Add;
												break;

											case Keywords.ID_MODULATE:
												sbt1 = SceneBlendType.Modulate;
												break;

											case Keywords.ID_COLOUR_BLEND:
												sbt1 = SceneBlendType.TransparentColor;
												break;

											case Keywords.ID_ALPHA_BLEND:
												sbt1 = SceneBlendType.TransparentAlpha;
												break;

											default:
												compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
													"separate_scene_blend does not support \"" + atom1.Value + "\" as argument 2" );
												return;
										}

									    throw new NotImplementedException(
                                            string.Format("SetSeparateSceneBlending({0}, {1})", sbt0, sbt1));
									    //TODO
									    //mPass->setSeparateSceneBlending(sbt0, sbt1);
									}
									else
									{
										compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
											"separate_scene_blend does not support \"" + i0.Value + "\" as argument 1" );
									}
								}
								else
								{
									AbstractNode i0 = getNodeAt( prop.Values, 0 ), i1 = getNodeAt( prop.Values, 1 ),
										i2 = getNodeAt( prop.Values, 2 ), i3 = getNodeAt( prop.Values, 3 );

									if ( i0 is AtomAbstractNode && i1 is AtomAbstractNode
										&& i2 is AtomAbstractNode && i3 is AtomAbstractNode )
									{
										SceneBlendFactor sbf0, sbf1, sbf2, sbf3;
										if ( getEnumeration<SceneBlendFactor>( i0, compiler, out sbf0 ) && getEnumeration<SceneBlendFactor>( i1, compiler, out sbf1 )
											&& getEnumeration<SceneBlendFactor>( i2, compiler, out sbf2 ) && getEnumeration<SceneBlendFactor>( i3, compiler, out sbf3 ) )
										{
											//TODO
											//mPass->setSeparateSceneBlending(sbf0, sbf1, sbf2, sbf3);
										}
										else
										{
											compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
												"one of the arguments to separate_scene_blend is not a valid scene blend factor directive" );
										}
									}
									else
									{
										compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
											"one of the arguments to separate_scene_blend is not a valid scene blend factor directive" );
									}
								}
								break;
							#endregion ID_SEPARATE_SCENE_BLEND

							#region ID_SCENE_BLEND_OP
							case Keywords.ID_SCENE_BLEND_OP:
								if ( prop.Values.Count == 0 )
								{
									compiler.AddError( CompileErrorCode.StringExpected, prop.File, prop.Line );
								}
								else if ( prop.Values.Count > 1 )
								{
									compiler.AddError( CompileErrorCode.FewerParametersExpected, prop.File, prop.Line,
										"scene_blend_op must have 1 argument" );
								}
								else
								{
									if ( prop.Values[ 0 ] is AtomAbstractNode )
									{
										AtomAbstractNode atom = (AtomAbstractNode)prop.Values[ 0 ];

										switch ( (Keywords)atom.Id )
										{
											case Keywords.ID_ADD:
												//TODO
												//mPass->setSceneBlendingOperation(SBO_ADD);
												break;

											case Keywords.ID_SUBTRACT:
												//TODO
												//mPass->setSceneBlendingOperation(SBO_SUBTRACT);
												break;

											case Keywords.ID_REVERSE_SUBTRACT:
												//TODO
												//mPass->setSceneBlendingOperation(SBO_REVERSE_SUBTRACT);
												break;

											case Keywords.ID_MIN:
												//TODO
												//mPass->setSceneBlendingOperation(SBO_MIN);
												break;

											case Keywords.ID_MAX:
												//TODO
												//mPass->setSceneBlendingOperation(SBO_MAX);
												break;

											default:
												compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
													atom.Value + ": unrecognized argument" );
												break;
										}
									}
									else
									{
										compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
											prop.Values[ 0 ].Value + ": unrecognized argument" );
									}
								}
								break;
							#endregion ID_SCENE_BLEND_OP

							#region ID_SEPARATE_SCENE_BLEND_OP
							case Keywords.ID_SEPARATE_SCENE_BLEND_OP:
								if ( prop.Values.Count == 0 )
								{
									compiler.AddError( CompileErrorCode.StringExpected, prop.File, prop.Line );
								}
								else if ( prop.Values.Count != 2 )
								{
									compiler.AddError( CompileErrorCode.FewerParametersExpected, prop.File, prop.Line,
										"separate_scene_blend_op must have 2 arguments" );
								}
								else
								{
									AbstractNode i0 = getNodeAt( prop.Values, 0 ), i1 = getNodeAt( prop.Values, 1 );
									if ( i0 is AtomAbstractNode && i1 is AtomAbstractNode )
									{
										AtomAbstractNode atom0 = (AtomAbstractNode)i0,
											atom1 = (AtomAbstractNode)i1;

										//TODO
										//SceneBlendOperation op = SBO_ADD, alphaOp = SBO_ADD;
										switch ( (Keywords)atom0.Id )
										{
											case Keywords.ID_ADD:
												//TODO
												//op = SBO_ADD;
												break;

											case Keywords.ID_SUBTRACT:
												//TODO
												//op = SBO_SUBTRACT;
												break;

											case Keywords.ID_REVERSE_SUBTRACT:
												//TODO
												//op = SBO_REVERSE_SUBTRACT;
												break;

											case Keywords.ID_MIN:
												//TODO
												//op = SBO_MIN;
												break;

											case Keywords.ID_MAX:
												//TODO
												//op = SBO_MAX;
												break;

											default:
												compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
													atom0.Value + ": unrecognized first argument" );
												break;
										}

										switch ( (Keywords)atom1.Id )
										{
											case Keywords.ID_ADD:
												//TODO
												//alphaOp = SBO_ADD;
												break;

											case Keywords.ID_SUBTRACT:
												//TODO
												//alphaOp = SBO_SUBTRACT;
												break;

											case Keywords.ID_REVERSE_SUBTRACT:
												//TODO
												//alphaOp = SBO_REVERSE_SUBTRACT;
												break;

											case Keywords.ID_MIN:
												//TODO
												//alphaOp = SBO_MIN;
												break;

											case Keywords.ID_MAX:
												//TODO
												//alphaOp = SBO_MAX;
												break;

											default:
												compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
													atom1.Value + ": unrecognized second argument" );
												break;
										}

										//TODO
										//mPass->setSeparateSceneBlendingOperation(op, alphaOp);
									}
									else
									{
										compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
											prop.Values[ 0 ].Value + ": unrecognized argument" );
									}
								}
								break;
							#endregion ID_SEPARATE_SCENE_BLEND_OP

							#region ID_DEPTH_CHECK
							case Keywords.ID_DEPTH_CHECK:
								if ( prop.Values.Count == 0 )
								{
									compiler.AddError( CompileErrorCode.StringExpected, prop.File, prop.Line );
								}
								else if ( prop.Values.Count > 1 )
								{
									compiler.AddError( CompileErrorCode.FewerParametersExpected, prop.File, prop.Line,
										"depth_check must have 1 argument" );
								}
								else
								{
									bool val = true;
									if ( getBoolean( prop.Values[ 0 ], out val ) )
										_pass.DepthCheck = val;
									else
										compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
											"depth_check third argument must be \"true\", \"false\", \"yes\", \"no\", \"on\", or \"off\"" );
								}
								break;
							#endregion ID_DEPTH_CHECK

							#region ID_DEPTH_WRITE
							case Keywords.ID_DEPTH_WRITE:
								if ( prop.Values.Count == 0 )
								{
									compiler.AddError( CompileErrorCode.StringExpected, prop.File, prop.Line );
								}
								else if ( prop.Values.Count > 1 )
								{
									compiler.AddError( CompileErrorCode.FewerParametersExpected, prop.File, prop.Line,
										"depth_write must have 1 argument" );
								}
								else
								{
									bool val = true;
									if ( getBoolean( prop.Values[ 0 ], out val ) )
										_pass.DepthWrite = val;
									else
										compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
											"depth_write third argument must be \"true\", \"false\", \"yes\", \"no\", \"on\", or \"off\"" );
								}
								break;
							#endregion ID_DEPTH_WRITE

							#region ID_DEPTH_BIAS
							case Keywords.ID_DEPTH_BIAS:
								if ( prop.Values.Count == 0 )
								{
									compiler.AddError( CompileErrorCode.StringExpected, prop.File, prop.Line );
								}
								else if ( prop.Values.Count > 2 )
								{
									compiler.AddError( CompileErrorCode.FewerParametersExpected, prop.File, prop.Line,
										"depth_bias must have at most 2 arguments" );
								}
								else
								{
									AbstractNode i0 = getNodeAt( prop.Values, 0 ), i1 = getNodeAt( prop.Values, 1 );
									float val0, val1 = 0.0f;
									if ( getFloat( i0, out val0 ) )
									{
										if ( i1 != null )
											getFloat( i1, out val1 );

										_pass.SetDepthBias( val0, val1 );
									}
									else
									{
										compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
											"depth_bias does not support \"" + i0.Value + "\" for argument 1" );
									}
								}
								break;
							#endregion ID_DEPTH_BIAS

							#region ID_DEPTH_FUNC
							case Keywords.ID_DEPTH_FUNC:
								if ( prop.Values.Count == 0 )
								{
									compiler.AddError( CompileErrorCode.StringExpected, prop.File, prop.Line );
								}
								else if ( prop.Values.Count > 1 )
								{
									compiler.AddError( CompileErrorCode.FewerParametersExpected, prop.File, prop.Line,
										"depth_func must have 1 argument" );
								}
								else
								{
									CompareFunction func;
									if ( getEnumeration<CompareFunction>( prop.Values[ 0 ], compiler, out func ) )
										_pass.DepthFunction = func;
									else
										compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
											prop.Values[ 0 ].Value + " is not a valid CompareFunction" );
								}
								break;
							#endregion ID_DEPTH_FUNC

							#region ID_ITERATION_DEPTH_BIAS
							case Keywords.ID_ITERATION_DEPTH_BIAS:
								if ( prop.Values.Count == 0 )
								{
									compiler.AddError( CompileErrorCode.StringExpected, prop.File, prop.Line );
								}
								else if ( prop.Values.Count > 1 )
								{
									compiler.AddError( CompileErrorCode.FewerParametersExpected, prop.File, prop.Line,
										"iteration_depth_bias must have 1 argument" );
								}
								else
								{
									float val = 0.0f;
									if ( getFloat( prop.Values[ 0 ], out val ) )
									{
										//TODO
										/*mPass->setIterationDepthBias(val);*/
									}
									else
										compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
											prop.Values[ 0 ].Value + " is not a valid float value" );
								}
								break;
							#endregion ID_ITERATION_DEPTH_BIAS

							#region ID_ALPHA_REJECTION
							case Keywords.ID_ALPHA_REJECTION:
								if ( prop.Values.Count == 0 )
								{
									compiler.AddError( CompileErrorCode.StringExpected, prop.File, prop.Line );
								}
								else if ( prop.Values.Count > 2 )
								{
									compiler.AddError( CompileErrorCode.FewerParametersExpected, prop.File, prop.Line,
										"alpha_rejection must have at most 2 arguments" );
								}
								else
								{
									AbstractNode i0 = getNodeAt( prop.Values, 0 ), i1 = getNodeAt( prop.Values, 1 );
									CompareFunction func;
									if ( getEnumeration<CompareFunction>( i0, compiler, out func ) )
									{
										if ( i1 != null )
										{
											int val = 0;
											if ( getInt( i1, out val ) )
												_pass.SetAlphaRejectSettings( func, val );
											else
												compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
													i1.Value + " is not a valid integer" );
										}
										else
											_pass.AlphaRejectFunction = func;
									}
									else
										compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
											i0.Value + " is not a valid CompareFunction" );
								}
								break;
							#endregion ID_ALPHA_REJECTION

							#region ID_ALPHA_TO_COVERAGE
							case Keywords.ID_ALPHA_TO_COVERAGE:
								if ( prop.Values.Count == 0 )
								{
									compiler.AddError( CompileErrorCode.StringExpected, prop.File, prop.Line );
								}
								else if ( prop.Values.Count > 1 )
								{
									compiler.AddError( CompileErrorCode.FewerParametersExpected, prop.File, prop.Line,
										"alpha_to_coverage must have 1 argument" );
								}
								else
								{
									bool val = true;
									if ( getBoolean( prop.Values[ 0 ], out val ) )
										_pass.IsAlphaToCoverageEnabled = val;
									else
										compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
										"alpha_to_coverage argument must be \"true\", \"false\", \"yes\", \"no\", \"on\", or \"off\"" );
								}
								break;
							#endregion ID_ALPHA_TO_COVERAGE

							#region ID_LIGHT_SCISSOR
							case Keywords.ID_LIGHT_SCISSOR:
								if ( prop.Values.Count == 0 )
								{
									compiler.AddError( CompileErrorCode.StringExpected, prop.File, prop.Line );
								}
								else if ( prop.Values.Count > 1 )
								{
									compiler.AddError( CompileErrorCode.FewerParametersExpected, prop.File, prop.Line,
										"light_scissor must have only 1 argument" );
								}
								else
								{
									bool val = false;
									if ( getBoolean( prop.Values[ 0 ], out val ) )
									{
										//TODO
										/*mPass->setLightScissoringEnabled(val);*/
									}
									else
										compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
											prop.Values[ 0 ].Value + " is not a valid boolean" );
								}
								break;
							#endregion ID_LIGHT_SCISSOR

							#region ID_LIGHT_CLIP_PLANES
							case Keywords.ID_LIGHT_CLIP_PLANES:
								if ( prop.Values.Count == 0 )
								{
									compiler.AddError( CompileErrorCode.StringExpected, prop.File, prop.Line );
								}
								else if ( prop.Values.Count > 1 )
								{
									compiler.AddError( CompileErrorCode.FewerParametersExpected, prop.File, prop.Line,
										"light_clip_planes must have at most 1 argument" );
								}
								else
								{
									bool val = false;
									if ( getBoolean( prop.Values[ 0 ], out val ) )
									{
										//TODO
										/*mPass->setLightClipPlanesEnabled(val);*/
									}
									else
										compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
											prop.Values[ 0 ].Value + " is not a valid boolean" );
								}
								break;
							#endregion ID_LIGHT_CLIP_PLANES

							#region ID_TRANSPARENT_SORTING
							case Keywords.ID_TRANSPARENT_SORTING:
								if ( prop.Values.Count == 0 )
								{
									compiler.AddError( CompileErrorCode.StringExpected, prop.File, prop.Line );
								}
								else if ( prop.Values.Count > 1 )
								{
									compiler.AddError( CompileErrorCode.FewerParametersExpected, prop.File, prop.Line,
										"transparent_sorting must have at most 1 argument" );
								}
								else
								{
									bool val = true;
									if ( getBoolean( prop.Values[ 0 ], out val ) )
									{
										//TODO
										//mPass->setTransparentSortingEnabled(val);
										//mPass->setTransparentSortingForced(false);
									}
									else
									{
										string val2;
										if ( getString( prop.Values[ 0 ], out val2 ) && val2 == "force" )
										{
											//TODO
											//mPass->setTransparentSortingEnabled(true);
											//mPass->setTransparentSortingForced(true);
										}
										else
										{
											compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
												prop.Values[ 0 ].Value + " must be boolean or force" );
										}
									}
								}
								break;
							#endregion ID_TRANSPARENT_SORTING

							#region ID_ILLUMINATION_STAGE
							case Keywords.ID_ILLUMINATION_STAGE:
								if ( prop.Values.Count == 0 )
								{
									compiler.AddError( CompileErrorCode.StringExpected, prop.File, prop.Line );
								}
								else if ( prop.Values.Count > 1 )
								{
									compiler.AddError( CompileErrorCode.FewerParametersExpected, prop.File, prop.Line,
										"illumination_stage must have at most 1 argument" );
								}
								else
								{
									if ( prop.Values[ 0 ] is AtomAbstractNode )
									{
										AtomAbstractNode atom = (AtomAbstractNode)prop.Values[ 0 ];
										switch ( (Keywords)atom.Id )
										{
											case Keywords.ID_AMBIENT:
												//TODO
												//mPass->setIlluminationStage(IS_AMBIENT);
												break;

											case Keywords.ID_PER_LIGHT:
												//TODO
												//mPass->setIlluminationStage(IS_PER_LIGHT);
												break;

											case Keywords.ID_DECAL:
												//TODO
												//mPass->setIlluminationStage(IS_DECAL);
												break;

											default:
												compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
													prop.Values[ 0 ].Value + " is not a valid IlluminationStage" );
												break;
										}
									}
									else
									{
										compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
											prop.Values[ 0 ].Value + " is not a valid IlluminationStage" );
									}
								}
								break;
							#endregion ID_ILLUMINATION_STAGE

							#region ID_CULL_HARDWARE
							case Keywords.ID_CULL_HARDWARE:
								if ( prop.Values.Count == 0 )
								{
									compiler.AddError( CompileErrorCode.StringExpected, prop.File, prop.Line );
								}
								else if ( prop.Values.Count > 1 )
								{
									compiler.AddError( CompileErrorCode.FewerParametersExpected, prop.File, prop.Line,
										"cull_hardware must have at most 1 argument" );
								}
								else
								{
									if ( prop.Values[ 0 ] is AtomAbstractNode )
									{
										AtomAbstractNode atom = (AtomAbstractNode)prop.Values[ 0 ];
										switch ( (Keywords)atom.Id )
										{
											case Keywords.ID_CLOCKWISE:
												_pass.CullingMode = CullingMode.Clockwise;
												break;

											case Keywords.ID_ANTICLOCKWISE:
												_pass.CullingMode = CullingMode.CounterClockwise;
												break;

											case Keywords.ID_NONE:
												_pass.CullingMode = CullingMode.None;
												break;

											default:
												compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
													prop.Values[ 0 ].Value + " is not a valid CullingMode" );
												break;
										}
									}
									else
									{
										compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
											prop.Values[ 0 ].Value + " is not a valid CullingMode" );
									}
								}
								break;
							#endregion ID_CULL_HARDWARE

							#region ID_CULL_SOFTWARE
							case Keywords.ID_CULL_SOFTWARE:
								if ( prop.Values.Count == 0 )
								{
									compiler.AddError( CompileErrorCode.StringExpected, prop.File, prop.Line );
								}
								else if ( prop.Values.Count > 1 )
								{
									compiler.AddError( CompileErrorCode.FewerParametersExpected, prop.File, prop.Line,
										"cull_software must have at most 1 argument" );
								}
								else
								{
									if ( prop.Values[ 0 ] is AtomAbstractNode )
									{
										AtomAbstractNode atom = (AtomAbstractNode)prop.Values[ 0 ];
										switch ( (Keywords)atom.Id )
										{
											case Keywords.ID_FRONT:
												_pass.ManualCullingMode = ManualCullingMode.Front;
												break;

											case Keywords.ID_BACK:
												_pass.ManualCullingMode = ManualCullingMode.Back;
												break;

											case Keywords.ID_NONE:
												_pass.ManualCullingMode = ManualCullingMode.None;
												break;

											default:
												compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
													prop.Values[ 0 ].Value + " is not a valid ManualCullingMode" );
												break;
										}
									}
									else
									{
										compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
											prop.Values[ 0 ].Value + " is not a valid ManualCullingMode" );
									}
								}
								break;
							#endregion ID_CULL_SOFTWARE

							#region ID_NORMALISE_NORMALS
							case Keywords.ID_NORMALISE_NORMALS:
								if ( prop.Values.Count == 0 )
								{
									compiler.AddError( CompileErrorCode.StringExpected, prop.File, prop.Line );
								}
								else if ( prop.Values.Count > 1 )
								{
									compiler.AddError( CompileErrorCode.FewerParametersExpected, prop.File, prop.Line,
										"normalise_normals must have at most 1 argument" );
								}
								else
								{
									bool val = false;
									if ( getBoolean( prop.Values[ 0 ], out val ) )
									{
										//TODO
										/*mPass->setNormaliseNormals(val);*/
									}
									else
										compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
											prop.Values[ 0 ].Value + " is not a valid boolean" );
								}
								break;
							#endregion ID_NORMALISE_NORMALS

							#region ID_LIGHTING
							case Keywords.ID_LIGHTING:
								if ( prop.Values.Count == 0 )
								{
									compiler.AddError( CompileErrorCode.StringExpected, prop.File, prop.Line );
								}
								else if ( prop.Values.Count > 1 )
								{
									compiler.AddError( CompileErrorCode.FewerParametersExpected, prop.File, prop.Line,
										"lighting must have at most 1 argument" );
								}
								else
								{
									bool val = false;
									if ( getBoolean( prop.Values[ 0 ], out val ) )
										_pass.LightingEnabled = val;
									else
										compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
											prop.Values[ 0 ].Value + " is not a valid boolean" );
								}
								break;
							#endregion ID_LIGHTING

							#region ID_SHADING
							case Keywords.ID_SHADING:
								if ( prop.Values.Count == 0 )
								{
									compiler.AddError( CompileErrorCode.StringExpected, prop.File, prop.Line );
								}
								else if ( prop.Values.Count > 1 )
								{
									compiler.AddError( CompileErrorCode.FewerParametersExpected, prop.File, prop.Line,
										"shading must have at most 1 argument" );
								}
								else
								{
									if ( prop.Values[ 0 ] is AtomAbstractNode )
									{
										AtomAbstractNode atom = (AtomAbstractNode)prop.Values[ 0 ];
										switch ( (Keywords)atom.Id )
										{
											case Keywords.ID_FLAT:
												_pass.ShadingMode = Shading.Flat;
												break;

											case Keywords.ID_GOURAUD:
												_pass.ShadingMode = Shading.Gouraud;
												break;

											case Keywords.ID_PHONG:
												_pass.ShadingMode = Shading.Phong;
												break;

											default:
												compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
													prop.Values[ 0 ].Value + " is not a valid shading mode (flat, gouraud, or phong)" );
												break;
										}
									}
									else
									{
										compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
											prop.Values[ 0 ].Value + " is not a valid shading mode (flat, gouraud, or phong)" );
									}
								}
								break;
							#endregion ID_SHADING

							#region ID_POLYGON_MODE
							case Keywords.ID_POLYGON_MODE:
								if ( prop.Values.Count == 0 )
								{
									compiler.AddError( CompileErrorCode.StringExpected, prop.File, prop.Line );
								}
								else if ( prop.Values.Count > 1 )
								{
									compiler.AddError( CompileErrorCode.FewerParametersExpected, prop.File, prop.Line,
										"polygon_mode must have at most 1 argument" );
								}
								else
								{
									if ( prop.Values[ 0 ] is AtomAbstractNode )
									{
										AtomAbstractNode atom = (AtomAbstractNode)prop.Values[ 0 ];
										switch ( (Keywords)atom.Id )
										{
											case Keywords.ID_SOLID:
												_pass.PolygonMode = PolygonMode.Solid;
												break;

											case Keywords.ID_POINTS:
												_pass.PolygonMode = PolygonMode.Points;
												break;

											case Keywords.ID_WIREFRAME:
												_pass.PolygonMode = PolygonMode.Wireframe;
												break;

											default:
												compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
													prop.Values[ 0 ].Value + " is not a valid polygon mode (solid, points, or wireframe)" );
												break;
										}
									}
									else
									{
										compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
											prop.Values[ 0 ].Value + " is not a valid polygon mode (solid, points, or wireframe)" );
									}
								}
								break;
							#endregion ID_POLYGON_MODE

							#region ID_POLYGON_MODE_OVERRIDEABLE
							case Keywords.ID_POLYGON_MODE_OVERRIDEABLE:
								if ( prop.Values.Count == 0 )
								{
									compiler.AddError( CompileErrorCode.StringExpected, prop.File, prop.Line );
								}
								else if ( prop.Values.Count > 1 )
								{
									compiler.AddError( CompileErrorCode.FewerParametersExpected, prop.File, prop.Line,
										"polygon_mode_overrideable must have at most 1 argument" );
								}
								else
								{
									bool val = false;
									if ( getBoolean( prop.Values[ 0 ], out val ) )
									{
										//TODO
										/*mPass->setPolygonModeOverrideable(val);*/
									}
									else
										compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
											prop.Values[ 0 ].Value + " is not a valid boolean" );
								}
								break;
							#endregion ID_POLYGON_MODE_OVERRIDEABLE

							#region ID_FOG_OVERRIDE
							case Keywords.ID_FOG_OVERRIDE:
								if ( prop.Values.Count == 0 )
								{
									compiler.AddError( CompileErrorCode.StringExpected, prop.File, prop.Line );
								}
								else if ( prop.Values.Count > 8 )
								{
									compiler.AddError( CompileErrorCode.FewerParametersExpected, prop.File, prop.Line,
										"fog_override must have at most 8 arguments" );
								}
								else
								{
									AbstractNode i0 = getNodeAt( prop.Values, 0 ), i1 = getNodeAt( prop.Values, 1 ), i2 = getNodeAt( prop.Values, 2 );
									bool val = false;
									if ( getBoolean( prop.Values[ 0 ], out val ) )
									{
										FogMode mode = FogMode.None;
										ColorEx clr = ColorEx.White;

										Real dens = 0.001, start = 0.0f, end = 1.0f;

										if ( i1 != null )
										{
											if ( i1 is AtomAbstractNode )
											{
												AtomAbstractNode atom = (AtomAbstractNode)i1;
												switch ( (Keywords)atom.Id )
												{
													case Keywords.ID_NONE:
														mode = FogMode.None;
														break;

													case Keywords.ID_LINEAR:
														mode = FogMode.Linear;
														break;

													case Keywords.ID_EXP:
														mode = FogMode.Exp;
														break;

													case Keywords.ID_EXP2:
														mode = FogMode.Exp2;
														break;

													default:
														compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
															i1.Value + " is not a valid FogMode" );
														break;
												}
											}
											else
											{
												compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
													i1.Value + " is not a valid FogMode" );
												break;
											}
										}

										if ( i2 != null )
										{
											// following line code was if(!getColour(i2, prop->values.end(), &clr, 3))
											if ( !getColor( prop.Values, 2, out clr, 3 ) )
											{
												compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
													i2.Value + " is not a valid colour" );
												break;
											}

											i2 = getNodeAt( prop.Values, 5 );
										}

										if ( i2 != null )
										{
											if ( !getReal( i2, out dens ) )
											{
												compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
													i2.Value + " is not a valid number" );
												break;
											}
											//++i2;
											i2 = getNodeAt( prop.Values, 6 );
										}

										if ( i2 != null )
										{
											if ( !getReal( i2, out start ) )
											{
												compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
													i2.Value + " is not a valid number" );
												return;
											}
											//++i2;
											i2 = getNodeAt( prop.Values, 7 );
										}

										if ( i2 != null )
										{
											if ( !getReal( i2, out end ) )
											{
												compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
													i2.Value + " is not a valid number" );
												return;
											}
											//++i2;
											i2 = getNodeAt( prop.Values, 8 );
										}

										_pass.SetFog( val, mode, clr, dens, start, end );
									}
									else
										compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
											prop.Values[ 0 ].Value + " is not a valid boolean" );
								}
								break;
							#endregion ID_FOG_OVERRIDE

							#region ID_COLOUR_WRITE
							case Keywords.ID_COLOUR_WRITE:
								if ( prop.Values.Count == 0 )
								{
									compiler.AddError( CompileErrorCode.StringExpected, prop.File, prop.Line );
								}
								else if ( prop.Values.Count > 1 )
								{
									compiler.AddError( CompileErrorCode.FewerParametersExpected, prop.File, prop.Line,
										"colour_write must have at most 1 argument" );
								}
								else
								{
									bool val = false;
									if ( getBoolean( prop.Values[ 0 ], out val ) )
										_pass.ColorWriteEnabled = val;
									else
										compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
											prop.Values[ 0 ].Value + " is not a valid boolean" );
								}
								break;
							#endregion ID_COLOUR_WRITE

							#region ID_MAX_LIGHTS
							case Keywords.ID_MAX_LIGHTS:
								if ( prop.Values.Count == 0 )
								{
									compiler.AddError( CompileErrorCode.StringExpected, prop.File, prop.Line );
								}
								else if ( prop.Values.Count > 1 )
								{
									compiler.AddError( CompileErrorCode.FewerParametersExpected, prop.File, prop.Line,
										"max_lights must have at most 1 argument" );
								}
								else
								{
									int val = 0;
									if ( getInt( prop.Values[ 0 ], out val ) )
										_pass.MaxSimultaneousLights = val;
									else
										compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
											prop.Values[ 0 ].Value + " is not a valid integer" );
								}
								break;
							#endregion ID_MAX_LIGHTS

							#region ID_START_LIGHT
							case Keywords.ID_START_LIGHT:
								if ( prop.Values.Count == 0 )
								{
									compiler.AddError( CompileErrorCode.StringExpected, prop.File, prop.Line );
								}
								else if ( prop.Values.Count > 1 )
								{
									compiler.AddError( CompileErrorCode.FewerParametersExpected, prop.File, prop.Line,
										"start_light must have at most 1 argument" );
								}
								else
								{
									uint val = 0;
									if ( getUInt( prop.Values[ 0 ], out val ) )
									{
										//TODO
										/*mPass->setStartLight(static_cast<unsigned short>(val));*/
									}
									else
										compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
											prop.Values[ 0 ].Value + " is not a valid integer" );
								}
								break;
							#endregion ID_START_LIGHT

							#region ID_ITERATION
							case Keywords.ID_ITERATION:
								if ( prop.Values.Count == 0 )
								{
									compiler.AddError( CompileErrorCode.StringExpected, prop.File, prop.Line );
								}
								else
								{
									AbstractNode i0 = getNodeAt( prop.Values, 0 );
									if ( i0 is AtomAbstractNode )
									{
										AtomAbstractNode atom = (AtomAbstractNode)i0;
										if ( atom.Id == (uint)Keywords.ID_ONCE )
										{
											_pass.IteratePerLight = false;
										}
										else if ( atom.Id == (uint)Keywords.ID_ONCE_PER_LIGHT )
										{
											AbstractNode i1 = getNodeAt( prop.Values, 1 );
											if ( i1 != null && i1 is AtomAbstractNode )
											{
												atom = (AtomAbstractNode)i1;
												switch ( (Keywords)atom.Id )
												{
													case Keywords.ID_POINT:
														_pass.IteratePerLight = true;
														break;

													case Keywords.ID_DIRECTIONAL:
														//TODO
														//_pass.SetIteratePerLight(true, true, LightType.Directional );
														break;

													case Keywords.ID_SPOT:
														//TODO
                                                        //_pass.SetIteratePerLight(true, true, LightType.Spotlight );
														break;

													default:
														compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
															prop.Values[ 0 ].Value + " is not a valid light type (point, directional, or spot)" );
														break;
												}
											}
											else
											{
												//TODO
                                                //_pass.SetIteratePerLight(true, false);
											}

										}
										else if ( atom.IsNumber )
										{
											//TODO
											_pass.IterationCount = Int32.Parse( atom.Value );

											AbstractNode i1 = getNodeAt( prop.Values, 1 );
											if ( i1 != null && i1 is AtomAbstractNode )
											{
												atom = (AtomAbstractNode)i1;
												if ( atom.Id == (uint)Keywords.ID_PER_LIGHT )
												{
													AbstractNode i2 = getNodeAt( prop.Values, 2 );
													if ( i2 != null && i2 is AtomAbstractNode )
													{
														atom = (AtomAbstractNode)i2;
														switch ( (Keywords)atom.Id )
														{
															case Keywords.ID_POINT:
																_pass.IteratePerLight = true;
																break;

															case Keywords.ID_DIRECTIONAL:
																//TODO
																//mPass->setIteratePerLight(true, true, Light::LT_DIRECTIONAL);
																break;

															case Keywords.ID_SPOT:
																//TODO
																//mPass->setIteratePerLight(true, true, Light::LT_SPOTLIGHT);
																break;

															default:
																compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
																	i2.Value + " is not a valid light type (point, directional, or spot)" );
																break;
														}
													}
													else
													{
														//TODO
														//mPass->setIteratePerLight(true, false);
													}
												}
												else if ( atom.Id == (uint)Keywords.ID_PER_N_LIGHTS )
												{
													AbstractNode i2 = getNodeAt( prop.Values, 2 );
													if ( i2 != null && i2 is AtomAbstractNode )
													{
														atom = (AtomAbstractNode)i2;
														if ( atom.IsNumber )
														{
															//TODO
															//mPass->setLightCountPerIteration(
															//    static_cast<unsigned short>(StringConverter::parseInt(atom->value)));

															AbstractNode i3 = getNodeAt( prop.Values, 3 );
															if ( i3 != null && i3 is AtomAbstractNode )
															{
																atom = (AtomAbstractNode)i3;
																switch ( (Keywords)atom.Id )
																{
																	case Keywords.ID_POINT:
																		_pass.IteratePerLight = true;
																		break;

																	case Keywords.ID_DIRECTIONAL:
																		//TODO
																		//mPass->setIteratePerLight(true, true, Light::LT_DIRECTIONAL);
																		break;

																	case Keywords.ID_SPOT:
																		//TODO
																		//mPass->setIteratePerLight(true, true, Light::LT_SPOTLIGHT);
																		break;

																	default:
																		compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
																			i3.Value + " is not a valid light type (point, directional, or spot)" );
																		break;
																}
															}
															else
															{
																//TODO
																//mPass->setIteratePerLight(true, false);
															}
														}
														else
														{
															compiler.AddError( CompileErrorCode.NumberExpected, prop.File, prop.Line,
																i2.Value + " is not a valid number" );
														}
													}
													else
													{
														compiler.AddError( CompileErrorCode.NumberExpected, prop.File, prop.Line,
															prop.Values[ 0 ].Value + " is not a valid number" );
													}
												}
											}
										}
										else
										{
											compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line );
										}
									}
									else
									{
										compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line );
									}
								}
								break;
							#endregion ID_ITERATION

							#region ID_POINT_SIZE
							case Keywords.ID_POINT_SIZE:
								if ( prop.Values.Count == 0 )
								{
									compiler.AddError( CompileErrorCode.StringExpected, prop.File, prop.Line );
								}
								else if ( prop.Values.Count > 1 )
								{
									compiler.AddError( CompileErrorCode.FewerParametersExpected, prop.File, prop.Line,
										"point_size must have at most 1 argument" );
								}
								else
								{
									Real val = 0.0f;
									if ( getReal( prop.Values[ 0 ], out val ) )
										_pass.PointSize = val;
									else
										compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
											prop.Values[ 0 ].Value + " is not a valid number" );
								}
								break;
							#endregion ID_POINT_SIZE

							#region ID_POINT_SPRITES
							case Keywords.ID_POINT_SPRITES:
								if ( prop.Values.Count == 0 )
								{
									compiler.AddError( CompileErrorCode.StringExpected, prop.File, prop.Line );
								}
								else if ( prop.Values.Count > 1 )
								{
									compiler.AddError( CompileErrorCode.FewerParametersExpected, prop.File, prop.Line,
										"point_sprites must have at most 1 argument" );
								}
								else
								{
									bool val = false;
									if ( getBoolean( prop.Values[ 0 ], out val ) )
										_pass.PointSpritesEnabled = val;
									else
										compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
											prop.Values[ 0 ].Value + " is not a valid boolean" );
								}
								break;
							#endregion ID_POINT_SPRITES

							#region ID_POINT_SIZE_ATTENUATION
							case Keywords.ID_POINT_SIZE_ATTENUATION:
								if ( prop.Values.Count == 0 )
								{
									compiler.AddError( CompileErrorCode.StringExpected, prop.File, prop.Line );
								}
								else if ( prop.Values.Count > 4 )
								{
									compiler.AddError( CompileErrorCode.FewerParametersExpected, prop.File, prop.Line,
										"point_size_attenuation must have at most 4 arguments" );
								}
								else
								{
									bool val = false;
									if ( getBoolean( prop.Values[ 0 ], out val ) )
									{
										if ( val )
										{
											AbstractNode i1 = getNodeAt( prop.Values, 1 ), i2 = getNodeAt( prop.Values, 2 ),
												i3 = getNodeAt( prop.Values, 3 );

											if ( prop.Values.Count > 1 )
											{
												Real constant = 0.0f, linear = 1.0f, quadratic = 0.0f;

												if ( i1 != null && i1 is AtomAbstractNode )
												{
													AtomAbstractNode atom = (AtomAbstractNode)i1;
													if ( atom.IsNumber )
														constant = atom.Number;
													else
														compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line );
												}
												else
												{
													compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
														i1.Value + " is not a valid number" );
												}

												if ( i2 != null && i2 is AtomAbstractNode )
												{
													AtomAbstractNode atom = (AtomAbstractNode)i2;
													if ( atom.IsNumber )
														linear = atom.Number;
													else
														compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line );
												}
												else
												{
													compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
														i2.Value + " is not a valid number" );
												}

												if ( i3 != null && i3 is AtomAbstractNode )
												{
													AtomAbstractNode atom = (AtomAbstractNode)i3;
													if ( atom.IsNumber )
														quadratic = atom.Number;
													else
														compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line );
												}
												else
												{
													compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
														i3.Value + " is not a valid number" );
												}

												//TODO
												//mPass->setPointAttenuation(true, constant, linear, quadratic);
											}
											else
											{
												//TODO
												//mPass->setPointAttenuation(true);
											}
										}
										else
										{
											//TODO
											//mPass->setPointAttenuation(false);
										}
									}
									else
										compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
											prop.Values[ 0 ].Value + " is not a valid boolean" );
								}
								break;
							#endregion ID_POINT_SIZE_ATTENUATION

							#region ID_POINT_SIZE_MIN
							case Keywords.ID_POINT_SIZE_MIN:
								if ( prop.Values.Count == 0 )
								{
									compiler.AddError( CompileErrorCode.StringExpected, prop.File, prop.Line );
								}
								else if ( prop.Values.Count > 1 )
								{
									compiler.AddError( CompileErrorCode.FewerParametersExpected, prop.File, prop.Line,
										"point_size_min must have at most 1 argument" );
								}
								else
								{
									Real val = 0.0f;
									if ( getReal( prop.Values[ 0 ], out val ) )
										_pass.PointMinSize = val;
									else
										compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
											prop.Values[ 0 ].Value + " is not a valid number" );
								}
								break;
							#endregion ID_POINT_SIZE_MIN

							#region ID_POINT_SIZE_MAX
							case Keywords.ID_POINT_SIZE_MAX:
								if ( prop.Values.Count == 0 )
								{
									compiler.AddError( CompileErrorCode.StringExpected, prop.File, prop.Line );
								}
								else if ( prop.Values.Count > 1 )
								{
									compiler.AddError( CompileErrorCode.FewerParametersExpected, prop.File, prop.Line,
										"point_size_max must have at most 1 argument" );
								}
								else
								{
									Real val = 0.0f;
									if ( getReal( prop.Values[ 0 ], out val ) )
										_pass.PointMaxSize = val;
									else
										compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
											prop.Values[ 0 ].Value + " is not a valid number" );
								}
								break;
							#endregion ID_POINT_SIZE_MAX

							default:
								compiler.AddError( CompileErrorCode.UnexpectedToken, prop.File, prop.Line,
									"token \"" + prop.Name + "\" is not recognized" );
								break;

						} // end of switch statement
					} // end of if ( i is PropertyAbstractNode )
					else if ( i is ObjectAbstractNode )
					{
						ObjectAbstractNode child = (ObjectAbstractNode)i;
						switch ( (Keywords)child.Id )
						{
							case Keywords.ID_FRAGMENT_PROGRAM_REF:
								_translateFragmentProgramRef( compiler, child );
								break;

							case Keywords.ID_VERTEX_PROGRAM_REF:
								_translateVertexProgramRef( compiler, child );
								break;

							case Keywords.ID_GEOMETRY_PROGRAM_REF:
								_translateGeometryProgramRef( compiler, child );
								break;

							case Keywords.ID_SHADOW_CASTER_VERTEX_PROGRAM_REF:
								_translateShadowCasterVertexProgramRef( compiler, child );
								break;

							case Keywords.ID_SHADOW_RECEIVER_VERTEX_PROGRAM_REF:
								_translateShadowReceiverVertexProgramRef( compiler, child );
								break;

							case Keywords.ID_SHADOW_RECEIVER_FRAGMENT_PROGRAM_REF:
								_translateShadowReceiverFragmentProgramRef( compiler, child );
								break;

							default:
								_processNode( compiler, i );
								break;
						}
					}
				}
			}
			#endregion Translator Implementation

			protected void _translateFragmentProgramRef( ScriptCompiler compiler, ObjectAbstractNode node )
			{
				string createdProgramName;
				Pass pass = _commonProgramChecks( compiler, node, out createdProgramName );

				if ( pass == null )
					return;

				pass.SetFragmentProgram( createdProgramName );
				if ( pass.FragmentProgram.IsSupported )
				{
					var parameters = pass.FragmentProgramParameters;
					GpuProgramTranslator.TranslateProgramParameters( compiler, parameters, node );
				}
			}

			protected void _translateVertexProgramRef( ScriptCompiler compiler, ObjectAbstractNode node )
			{
				string createdProgramName;
				Pass pass = _commonProgramChecks( compiler, node, out createdProgramName );

				if ( pass == null )
					return;

				pass.SetVertexProgram( createdProgramName );
				if ( pass.VertexProgram.IsSupported )
				{
					var parameters = pass.VertexProgramParameters;
					GpuProgramTranslator.TranslateProgramParameters( compiler, parameters, node );
				}
			}

			protected void _translateGeometryProgramRef( ScriptCompiler compiler, ObjectAbstractNode node )
			{
				string createdProgramName;
				Pass pass = _commonProgramChecks( compiler, node, out createdProgramName );

				if ( pass == null )
					return;

				pass.SetGeometryProgram( createdProgramName );
				if ( pass.GeometryProgram.IsSupported )
				{
					var parameters = pass.GeometryProgramParameters;
					GpuProgramTranslator.TranslateProgramParameters( compiler, parameters, node );
				}
			}

			protected void _translateShadowCasterVertexProgramRef( ScriptCompiler compiler, ObjectAbstractNode node )
			{
				string createdProgramName;
				Pass pass = _commonProgramChecks( compiler, node, out createdProgramName );

				if ( pass == null )
					return;

				pass.SetShadowCasterVertexProgram( createdProgramName );

				if ( GpuProgramManager.Instance.GetByName( createdProgramName ).IsSupported )
				{
#warning this need GpuProgramParametersShared implementation
					//    GpuProgramParametersSharedPtr params = pass->getShadowCasterVertexProgramParameters();
					//    GpuProgramTranslator::translateProgramParameters(compiler, params, node);
				}
			}

			protected void _translateShadowReceiverVertexProgramRef( ScriptCompiler compiler, ObjectAbstractNode node )
			{
				string createdProgramName;
				Pass pass = _commonProgramChecks( compiler, node, out createdProgramName );

				if ( pass == null )
					return;

				pass.SetShadowReceiverVertexProgram( createdProgramName );

				if ( GpuProgramManager.Instance.GetByName( createdProgramName ).IsSupported )
				{
#warning this need GpuProgramParametersShared implementation
					//    GpuProgramParametersSharedPtr params = pass->getShadowReceiverVertexProgramParameters();
					//    GpuProgramTranslator::translateProgramParameters(compiler, params, node);
				}
			}

			protected void _translateShadowReceiverFragmentProgramRef( ScriptCompiler compiler, ObjectAbstractNode node )
			{
				string createdProgramName;
				Pass pass = _commonProgramChecks( compiler, node, out createdProgramName );

				if ( pass == null )
					return;

				pass.SetShadowReceiverFragmentProgram( createdProgramName );

				if ( GpuProgramManager.Instance.GetByName( createdProgramName ).IsSupported )
				{
#warning this need GpuProgramParametersShared implementation
					//    GpuProgramParametersSharedPtr params = pass->getShadowReceiverFragmentProgramParameters();
					//    GpuProgramTranslator::translateProgramParameters(compiler, params, node);
				}
			}

			private Pass _commonProgramChecks( ScriptCompiler compiler, ObjectAbstractNode node, out string createdProgramName )
			{
				createdProgramName = string.Empty;

				if ( string.IsNullOrEmpty( node.Name ) )
				{
					compiler.AddError( CompileErrorCode.ObjectNameExpected, node.File, node.Line );
					return null;
				}

				ScriptCompilerEvent evt = new ProcessResourceNameScriptCompilerEvent(
					ProcessResourceNameScriptCompilerEvent.ResourceType.GpuProgram, node.Name );

				compiler._fireEvent( ref evt );
				createdProgramName = ( (ProcessResourceNameScriptCompilerEvent)evt ).Name;

				if ( GpuProgramManager.Instance.GetByName( createdProgramName ) == null )
				{
					compiler.AddError( CompileErrorCode.ReferenceToaNonExistingObject, node.File, node.Line );
					return null;
				}

				Pass pass = (Pass)node.Parent.Context;
				return pass;
			}
		}
	}
}

