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
		class TechniqueTranslator : Translator
		{
			private Technique _technique;

			public TechniqueTranslator( ScriptCompiler compiler, Technique technique )
				: base( compiler )
			{
				_technique = technique;
			}

			#region Translator Implementation

			protected override void ProcessObject( ObjectAbstractNode obj )
			{
				Compiler.Context = _technique;

				// Get the name of the technique
				if ( obj.Name != null && obj.Name.Length != 0 )
					_technique.Name = obj.Name;

				// Set the properties for the technique
				foreach ( AbstractNode node in obj.Children )
				{
					if ( node.Type == AbstractNodeType.Property )
					{
						Translator.Translate( this, node );
					}
					else if ( node.Type == AbstractNodeType.Object )
					{
						ObjectAbstractNode child = (ObjectAbstractNode)node;
						if ( (Keywords)child.Id == Keywords.ID_PASS )
						{
							// Create a pass and compile it
							Pass pass = _technique.CreatePass();
							PassTranslator translator = new PassTranslator( Compiler, pass );
							Translator.Translate( translator, child );
						}
					}
				}

			}

			protected override void ProcessProperty( PropertyAbstractNode property )
			{
				switch ( (Keywords)property.id )
				{
					case Keywords.ID_SCHEME:
						{
							if ( property.values.Count == 0 )
							{
								Compiler.AddError( CompileErrorCode.StringExpected, property.File, property.Line );
							}
							else if ( property.values.Count > 3 )
							{
								Compiler.AddError( CompileErrorCode.FewerParametersExpected, property.File, property.Line );
							}
							else
							{
								string val;
								if ( getString( property.values[ 0 ], out val ) )
									_technique.Scheme = val;
								else
									Compiler.AddError( CompileErrorCode.InvalidParameters, property.File, property.Line );
							}
							break;
						}
						break;
					case Keywords.ID_LOD_INDEX:
						{
							if ( property.values.Count == 0 )
							{
								Compiler.AddError( CompileErrorCode.StringExpected, property.File, property.Line );
							}
							else if ( property.values.Count > 3 )
							{
								Compiler.AddError( CompileErrorCode.FewerParametersExpected, property.File, property.Line );
							}
							else
							{
								float val;
								if ( getNumber( property.values[ 0 ], out val ) )
									_technique.LodIndex = (int)val;
								else
									Compiler.AddError( CompileErrorCode.InvalidParameters, property.File, property.Line );
							}
							break;
						}
						break;
				}
			}

			#endregion Translator Implementation
		}
	}
}

