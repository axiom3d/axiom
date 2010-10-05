#region LGPL License
/*
Axiom Graphics Engine Library
Copyright (C) 2003-2010 Axiom Project Team

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
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <id value="$Id$"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections.Generic;
using System.Text;

using Axiom.Core;
using Axiom.Graphics;
using Axiom.Math;

using Axiom.Scripting.Compiler.AST;

using Real = System.Single;

#endregion Namespace Declarations

namespace Axiom.Scripting.Compiler
{
	public partial class ScriptCompiler
	{
		class GpuProgramParametersTranslator : Translator
		{
			private GpuProgramParameters _parameters;
			private int _animParametricsCount;

			public GpuProgramParametersTranslator( ScriptCompiler compiler, GpuProgramParameters parameters )
				: base( compiler )
			{
				_parameters = parameters;
				_animParametricsCount = 0;
			}

			#region Translator Implementation

			protected override void ProcessObject( ObjectAbstractNode obj )
			{
				_animParametricsCount = 0;

				// Set up the parameters
				foreach ( AbstractNode node in obj.Children )
				{
					if ( node.Type == AbstractNodeType.Property )
					{
						Translator.Translate( this, node );
					}
				}
			}

			protected override void ProcessProperty( PropertyAbstractNode prop )
			{
				switch ( (Keywords)prop.id )
				{
					case Keywords.ID_PARAM_INDEXED:
					case Keywords.ID_PARAM_NAMED:
						{
							if ( prop.values.Count >= 3 )
							{
								bool named = ( (Keywords)prop.id == Keywords.ID_PARAM_NAMED );
								AbstractNode i0 = getNodeAt( prop.values, 0 ),
											 i1 = getNodeAt( prop.values, 1 ),
											 k = getNodeAt( prop.values, 2 );

								if ( i0.Type != AbstractNodeType.Atom || i1.Type != AbstractNodeType.Atom )
								{
									Compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line );
									return;
								}

								AtomAbstractNode atom0 = (AtomAbstractNode)i0, atom1 = (AtomAbstractNode)i1;
								if ( !named && !atom0.IsNumber )
								{
									Compiler.AddError( CompileErrorCode.NumberExpected, prop.File, prop.Line );
									return;
								}

								String name = "";
								int index = 0;
								// Assign the name/index
								if ( named )
									name = atom0.Value;
								else
									index = (int)atom0.Number;

								// Determine the type
								if ( atom1.Value == "matrix4x4" )
								{
									Matrix4 m;
									if ( getMatrix4( prop.values, 2, out m ) )
									{
										if ( named )
											_parameters.SetNamedConstant( name, m );
										else
											_parameters.SetConstant( index, m );
									}
									else
									{
										Compiler.AddError( CompileErrorCode.NumberExpected, prop.File, prop.Line );
									}
								}
								else
								{
									// Find the number of parameters
									bool isValid = true;
									String type = "int";
									int count = 0;
									if ( atom1.Value.Contains( "float" ) )
									{
										type = "float";
										if ( atom1.Value.Length >= 6 )
											count = Int32.Parse( atom1.Value.Substring( 5 ) );
										else
										{
											count = 1;
										}
									}
									else if ( atom1.Value.Contains( "int" ) )
									{
										type = "int";
										if ( atom1.Value.Length >= 4 )
											count = Int32.Parse( atom1.Value.Substring( 3 ) );
										else
										{
											count = 1;
										}
									}
									else
									{
										Compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line );
										isValid = false;
									}
									if ( isValid )
									{
										// First, clear out any offending auto constants
										//if ( named = true )
										//    _parameters.ClearNamedAutoConstant( name );
										//else
										//    _parameters.ClearAutoConstant( index );

										int roundedCount = count % 4 != 0 ? count + 4 - ( count % 4 ) : count;
										if ( type == "int" )
										{
											int[] vals = new int[ roundedCount ];
											if ( getInts( prop.values, 2, out vals, roundedCount ) )
											{
												if ( named )
												{
													//_parameters.SetNamedConstant( name, vals );
												}
												else
													_parameters.SetConstant( index, vals );
											}
											else
											{
												Compiler.AddError( CompileErrorCode.NumberExpected, prop.File, prop.Line );
											}

										}
										else
										{
											float[] vals = new float[ roundedCount ];
											if ( getFloats( prop.values, 2, out vals, roundedCount ) )
											{
												if ( named )
													_parameters.SetNamedConstant( name, vals );
												else
													_parameters.SetConstant( index, vals );
											}
											else
											{
												Compiler.AddError( CompileErrorCode.NumberExpected, prop.File, prop.Line );
											}
										}
									}
								}
							}
							else
							{
								Compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line );
							}
						}
						break;

					case Keywords.ID_PARAM_INDEXED_AUTO:
					case Keywords.ID_PARAM_NAMED_AUTO:
						{
							if ( prop.values.Count >= 3 )
							{
								bool named = ( (Keywords)prop.id == Keywords.ID_PARAM_NAMED );
								AbstractNode i0 = getNodeAt( prop.values, 0 ),
											 i1 = getNodeAt( prop.values, 1 ),
											 i2 = getNodeAt( prop.values, 2 );

								if ( i0.Type != AbstractNodeType.Atom || i1.Type != AbstractNodeType.Atom )
								{
									Compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line );
									return;
								}

								AtomAbstractNode atom0 = (AtomAbstractNode)i0, atom1 = (AtomAbstractNode)i1;
								if ( !named && !atom0.IsNumber )
								{
									Compiler.AddError( CompileErrorCode.NumberExpected, prop.File, prop.Line );
									return;
								}

								String name;
								int index = 0;
								// Assign the name/index
								if ( named )
									name = atom0.Value;
								else
									index = (int)atom0.Number;

								GpuProgramUsage def = null;
								if ( def != null )
								{
								}
								else
								{
									Compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line );
								}
							}
							else
							{
								Compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line );
							}
						}
						break;

				}
			}

			#endregion Translator Implementation
		}
	}
}
