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

using Axiom.Graphics;
using Axiom.Scripting.Compiler.AST;

#endregion Namespace Declarations

namespace Axiom.Scripting.Compiler
{
	public partial class ScriptCompiler
	{
		public class CompositionTargetPassTranslator : Translator
		{
			protected CompositionTargetPass _Target;

			public CompositionTargetPassTranslator()
				: base()
			{
				_Target = null;
			}

			#region Translator Implementation

			/// <see cref="Translator.CheckFor"/>
            public override bool CheckFor(Keywords nodeId, Keywords parentId)
			{
				return ( nodeId == Keywords.ID_TARGET || nodeId == Keywords.ID_TARGET_OUTPUT ) && parentId == Keywords.ID_TECHNIQUE;
			}

			/// <see cref="Translator.Translate"/>
			public override void Translate( ScriptCompiler compiler, AbstractNode node )
			{
				var obj = (ObjectAbstractNode)node;

				var technique = (CompositionTechnique)obj.Parent.Context;
				if ( obj.Id == (uint)Keywords.ID_TARGET )
				{
					_Target = technique.CreateTargetPass();
					if ( !string.IsNullOrEmpty( obj.Name ) )
					{
						_Target.OutputName = obj.Name;
					}
				}
				else if ( obj.Id == (uint)Keywords.ID_TARGET_OUTPUT )
				{
					_Target = technique.OutputTarget;
				}
				obj.Context = _Target;

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
								#region ID_INPUT

							case Keywords.ID_INPUT:
								if ( prop.Values.Count == 0 )
								{
									compiler.AddError( CompileErrorCode.StringExpected, prop.File, prop.Line );
									return;
								}
								else if ( prop.Values.Count > 1 )
								{
									compiler.AddError( CompileErrorCode.FewerParametersExpected, prop.File, prop.Line );
									return;
								}
								else
								{
									if ( prop.Values[ 0 ] is AtomAbstractNode )
									{
										var atom = (AtomAbstractNode)prop.Values[ 0 ];
										switch ( (Keywords)atom.Id )
										{
											case Keywords.ID_NONE:
												_Target.InputMode = CompositorInputMode.None;
												break;

											case Keywords.ID_PREVIOUS:
												_Target.InputMode = CompositorInputMode.Previous;
												break;

											default:
												compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line );
												break;
										}
									}
									else
									{
										compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line );
									}
								}
								break;

								#endregion ID_INPUT

								#region ID_ONLY_INITIAL

							case Keywords.ID_ONLY_INITIAL:
								if ( prop.Values.Count == 0 )
								{
									compiler.AddError( CompileErrorCode.StringExpected, prop.File, prop.Line );
									return;
								}
								else if ( prop.Values.Count > 1 )
								{
									compiler.AddError( CompileErrorCode.FewerParametersExpected, prop.File, prop.Line );
									return;
								}
								else
								{
									var val = false;
									if ( getBoolean( prop.Values[ 0 ], out val ) )
									{
										_Target.OnlyInitial = val;
									}
									else
									{
										compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line );
									}
								}
								break;

								#endregion ID_ONLY_INITIAL

								#region ID_VISIBILITY_MASK

							case Keywords.ID_VISIBILITY_MASK:
								if ( prop.Values.Count == 0 )
								{
									compiler.AddError( CompileErrorCode.StringExpected, prop.File, prop.Line );
									return;
								}
								else if ( prop.Values.Count > 1 )
								{
									compiler.AddError( CompileErrorCode.FewerParametersExpected, prop.File, prop.Line );
									return;
								}
								else
								{
									uint val;
									if ( getUInt( prop.Values[ 0 ], out val ) )
									{
										_Target.VisibilityMask = val;
									}
									else
									{
										compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line );
									}
								}
								break;

								#endregion ID_VISIBILITY_MASK

								#region ID_LOD_BIAS

							case Keywords.ID_LOD_BIAS:
								if ( prop.Values.Count == 0 )
								{
									compiler.AddError( CompileErrorCode.StringExpected, prop.File, prop.Line );
									return;
								}
								else if ( prop.Values.Count > 1 )
								{
									compiler.AddError( CompileErrorCode.FewerParametersExpected, prop.File, prop.Line );
									return;
								}
								else
								{
									float val;
									if ( getFloat( prop.Values[ 0 ], out val ) )
									{
										_Target.LodBias = val;
									}
									else
									{
										compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line );
									}
								}
								break;

								#endregion ID_LOD_BIAS

								#region ID_MATERIAL_SCHEME

							case Keywords.ID_MATERIAL_SCHEME:
								if ( prop.Values.Count == 0 )
								{
									compiler.AddError( CompileErrorCode.StringExpected, prop.File, prop.Line );
									return;
								}
								else if ( prop.Values.Count > 1 )
								{
									compiler.AddError( CompileErrorCode.FewerParametersExpected, prop.File, prop.Line );
									return;
								}
								else
								{
									string val;
									if ( getString( prop.Values[ 0 ], out val ) )
									{
										_Target.MaterialScheme = val;
									}
									else
									{
										compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line );
									}
								}
								break;

								#endregion ID_MATERIAL_SCHEME

								#region ID_SHADOWS_ENABLED

							case Keywords.ID_SHADOWS_ENABLED:
								if ( prop.Values.Count == 0 )
								{
									compiler.AddError( CompileErrorCode.StringExpected, prop.File, prop.Line );
									return;
								}
								else if ( prop.Values.Count > 1 )
								{
									compiler.AddError( CompileErrorCode.FewerParametersExpected, prop.File, prop.Line );
									return;
								}
								else
								{
									bool val;
									if ( getBoolean( prop.Values[ 0 ], out val ) )
									{
										_Target.ShadowsEnabled = val;
									}
									else
									{
										compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line );
									}
								}
								break;

								#endregion ID_SHADOWS_ENABLED

							default:
								compiler.AddError( CompileErrorCode.UnexpectedToken, prop.File, prop.Line, "token \"" + prop.Name + "\" is not recognized" );
								break;
						}
					}
				}
			}

			#endregion Translator Implementation
		}
	}
}
