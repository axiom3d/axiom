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
		public class CompositionPassStencilTranslator : Translator
		{
			protected CompositionPass _Pass;

			public CompositionPassStencilTranslator()
				: base()
			{
				_Pass = null;
			}

			#region Translator Implementation

			/// <see cref="Translator.CheckFor"/>
			internal override bool CheckFor( Keywords nodeId, Keywords parentId )
			{
				return nodeId == Keywords.ID_STENCIL && parentId == Keywords.ID_PASS;
			}

			/// <see cref="Translator.Translate"/>
			public override void Translate( ScriptCompiler compiler, AbstractNode node )
			{
				var obj = (ObjectAbstractNode)node;

				_Pass = (CompositionPass)obj.Parent.Context;

				// Should be no parameters, just children
				if ( obj.Values.Count != 0 )
				{
					compiler.AddError( CompileErrorCode.UnexpectedToken, obj.File, obj.Line );
				}

				foreach ( var i in obj.Children )
				{
					if ( i is ObjectAbstractNode )
					{
						_processNode( compiler, i );
					}
					else if ( i is PropertyAbstractNode )
					{
						var prop = (PropertyAbstractNode)i;
						switch ( (Keywords)prop.Id )
						{
							#region ID_CHECK
							case Keywords.ID_CHECK:
								{
									if ( prop.Values.Count == 0 )
									{
										compiler.AddError( CompileErrorCode.StringExpected, prop.File, prop.Line );
										return;
									}

									var val = false;
									if ( getBoolean( prop.Values[ 0 ], out val ) )
										_Pass.StencilCheck = val;
									else
										compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line );
								}
								break;
							#endregion ID_CHECK

							#region ID_COMP_FUNC
							case Keywords.ID_COMP_FUNC:
								{
									if ( prop.Values.Count == 0 )
									{
										compiler.AddError( CompileErrorCode.StringExpected, prop.File, prop.Line );
										return;
									}

									CompareFunction func;
									if ( getEnumeration<CompareFunction>( prop.Values[ 0 ], compiler, out func ) )
										_Pass.StencilFunc = func;
									else
										compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line );
								}
								break;
							#endregion ID_COMP_FUNC

							#region ID_REF_VALUE
							case Keywords.ID_REF_VALUE:
								{
									if ( prop.Values.Count == 0 )
									{
										compiler.AddError( CompileErrorCode.NumberExpected, prop.File, prop.Line );
										return;
									}

									int val;
									if ( getInt( prop.Values[ 0 ], out val ) )
										_Pass.StencilRefValue = val;
									else
										compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line );
								}
								break;
							#endregion ID_REF_VALUE

							#region ID_MASK
							case Keywords.ID_MASK:
								{
									if ( prop.Values.Count == 0 )
									{
										compiler.AddError( CompileErrorCode.NumberExpected, prop.File, prop.Line );
										return;
									}
									int val;
									if ( getInt( prop.Values[ 0 ], out val ) )
										_Pass.StencilMask = val;
									else
										compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line );
								}
								break;
							#endregion ID_MASK

							#region ID_FAIL_OP
							case Keywords.ID_FAIL_OP:
								{
									if ( prop.Values.Count == 0 )
									{
										compiler.AddError( CompileErrorCode.StringExpected, prop.File, prop.Line );
										return;
									}

									StencilOperation val;
									if ( getEnumeration<StencilOperation>( prop.Values[ 0 ], compiler, out val ) )
										_Pass.StencilFailOp = val;
									else
										compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line );
								}
								break;
							#endregion ID_FAIL_OP

							#region ID_DEPTH_FAIL_OP
							case Keywords.ID_DEPTH_FAIL_OP:
								{
									if ( prop.Values.Count == 0 )
									{
										compiler.AddError( CompileErrorCode.StringExpected, prop.File, prop.Line );
										return;
									}

									StencilOperation val;
									if ( getEnumeration<StencilOperation>( prop.Values[ 0 ], compiler, out val ) )
										_Pass.StencilDepthFailOp = val;
									else
										compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line );
								}
								break;
							#endregion ID_DEPTH_FAIL_OP

							#region ID_PASS_OP
							case Keywords.ID_PASS_OP:
								{
									if ( prop.Values.Count == 0 )
									{
										compiler.AddError( CompileErrorCode.StringExpected, prop.File, prop.Line );
										return;
									}

									StencilOperation val;
									if ( getEnumeration<StencilOperation>( prop.Values[ 0 ], compiler, out val ) )
										_Pass.StencilPassOp = val;
									else
										compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line );
								}
								break;
							#endregion ID_PASS_OP

							#region ID_TWO_SIDED
							case Keywords.ID_TWO_SIDED:
								{
									if ( prop.Values.Count == 0 )
									{
										compiler.AddError( CompileErrorCode.StringExpected, prop.File, prop.Line );
										return;
									}

									bool val;
									if ( getBoolean( prop.Values[ 0 ], out val ) )
										_Pass.StencilTwoSidedOperation = val;
									else
										compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line );
								}
								break;
							#endregion ID_TWO_SIDED

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
