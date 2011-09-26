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

using Axiom.Core;
using Axiom.Graphics;
using Axiom.Math;
using Axiom.Scripting.Compiler.AST;

#endregion Namespace Declarations

namespace Axiom.Scripting.Compiler
{
	public partial class ScriptCompiler
	{
		public class CompositionPassClearTranslator : Translator
		{
			protected CompositionPass _Pass;

			public CompositionPassClearTranslator()
				: base()
			{
				_Pass = null;
			}

			#region Translator Implementation

			/// <see cref="Translator.CheckFor"/>
			internal override bool CheckFor( Keywords nodeId, Keywords parentId )
			{
				return nodeId == Keywords.ID_CLEAR && parentId == Keywords.ID_PASS;
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
							#region ID_BUFFERS
							case Keywords.ID_BUFFERS:
								{
									FrameBufferType buffers = 0;
									foreach ( var k in prop.Values )
									{
										if ( k is AtomAbstractNode )
										{
											switch ( (Keywords)( (AtomAbstractNode)k ).Id )
											{
												case Keywords.ID_COLOUR:
													buffers |= FrameBufferType.Color;
													break;

												case Keywords.ID_DEPTH:
													buffers |= FrameBufferType.Depth;
													break;

												case Keywords.ID_STENCIL:
													buffers |= FrameBufferType.Stencil;
													break;

												default:
													compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line );
													break;
											}
										}
										else
											compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line );
									}
									_Pass.ClearBuffers = buffers;
								}
								break;
							#endregion ID_BUFFERS

							#region ID_COLOUR_VALUE
							case Keywords.ID_COLOUR_VALUE:
								{
									if ( prop.Values.Count == 0 )
									{
										compiler.AddError( CompileErrorCode.NumberExpected, prop.File, prop.Line );
										return;
									}

									var val = ColorEx.White;
									if ( getColor( prop.Values, 0, out val ) )
										_Pass.ClearColor = val;
									else
										compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line );
								}
								break;
							#endregion ID_COLOUR_VALUE

							#region ID_DEPTH_VALUE
							case Keywords.ID_DEPTH_VALUE:
								{
									if ( prop.Values.Count == 0 )
									{
										compiler.AddError( CompileErrorCode.NumberExpected, prop.File, prop.Line );
										return;
									}
									Real val = 0;
									if ( getReal( prop.Values[ 0 ], out val ) )
										_Pass.ClearDepth = val;
									else
										compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line );
								}
								break;
							#endregion ID_DEPTH_VALUE

							#region ID_STENCIL_VALUE
							case Keywords.ID_STENCIL_VALUE:
								{
									if ( prop.Values.Count == 0 )
									{
										compiler.AddError( CompileErrorCode.NumberExpected, prop.File, prop.Line );
										return;
									}

									var val = 0;
									if ( getInt( prop.Values[ 0 ], out val ) )
										_Pass.ClearStencil = val;
									else
										compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line );
								}
								break;
							#endregion ID_STENCIL_VALUE

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
