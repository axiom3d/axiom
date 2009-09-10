#region LGPL License
/*
Axiom Graphics Engine Library
Copyright (C) 2003-2007  Axiom Project Team

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
		class PassTranslator : Translator
		{
			private Pass _pass;
			public PassTranslator( ScriptCompiler compiler, Pass pass )
				: base( compiler )
			{
				_pass = pass;
			}

			#region Translator Implementation

			protected override void ProcessObject( ObjectAbstractNode obj )
			{
				Compiler.Context = _pass;

				// Get the name of the technique
				if ( obj.name != null && obj.name.Length != 0 )
					_pass.Name = obj.name;

				// Set the properties for the technique
				foreach ( AbstractNode node in obj.children )
				{
					if ( node.type == AbstractNodeType.Property )
					{
						Translator.Translate( this, node );
					}
					else if ( node.type == AbstractNodeType.Object )
					{
						ObjectAbstractNode child = (ObjectAbstractNode)node;
						switch ( (Keywords)child.id )
						{
							case Keywords.ID_TEXTURE_UNIT:
								{
									// Create a TextureUnitState and compile it
									TextureUnitState textureunitstate = _pass.CreateTextureUnitState();
									TextureUnitTranslator translator = new TextureUnitTranslator( Compiler, textureunitstate );
									Translator.Translate( translator, child );
								}
								break;

						}
					}
				}
			}

			protected override void ProcessProperty( PropertyAbstractNode property )
			{
				switch ( (Keywords)property.id )
				{
					case Keywords.ID_AMBIENT:
						{
							if ( property.values.Count == 0 )
							{
								Compiler.AddError( CompileErrorCode.StringExpected, property.file, property.line );
							}
							else if ( property.values.Count > 4 )
							{
								Compiler.AddError( CompileErrorCode.FewerParametersExpected, property.file, property.line );
							}
							else
							{
								ColorEx val;
								if ( getColor( property.values, 0, out val ) )
									_pass.Ambient = val;
								else
									Compiler.AddError( CompileErrorCode.InvalidParameters, property.file, property.line );
							}
							break;
						}
						break;

					case Keywords.ID_DIFFUSE:
						{
							if ( property.values.Count == 0 )
							{
								Compiler.AddError( CompileErrorCode.StringExpected, property.file, property.line );
							}
							else if ( property.values.Count > 4 )
							{
								Compiler.AddError( CompileErrorCode.FewerParametersExpected, property.file, property.line );
							}
							else
							{
								ColorEx val;
								if ( getColor( property.values, 0, out val ) )
									_pass.Diffuse = val;
								else
									Compiler.AddError( CompileErrorCode.InvalidParameters, property.file, property.line );
							}
							break;
						}
						break;

					case Keywords.ID_SPECULAR:
						{
							if ( property.values.Count == 0 )
							{
								Compiler.AddError( CompileErrorCode.StringExpected, property.file, property.line );
							}
							else if ( property.values.Count > 4 )
							{
								Compiler.AddError( CompileErrorCode.FewerParametersExpected, property.file, property.line );
							}
							else
							{
								ColorEx val;
								if ( getColor( property.values, 0, out val ) )
									_pass.Specular = val;
								else
									Compiler.AddError( CompileErrorCode.InvalidParameters, property.file, property.line );
							}
							break;
						}
						break;

					case Keywords.ID_EMISSIVE:
						{
							if ( property.values.Count == 0 )
							{
								Compiler.AddError( CompileErrorCode.StringExpected, property.file, property.line );
							}
							else if ( property.values.Count > 4 )
							{
								Compiler.AddError( CompileErrorCode.FewerParametersExpected, property.file, property.line );
							}
							else
							{
								ColorEx val;
								if ( getColor( property.values, 0, out val ) )
									_pass.Emissive = val;
								else
									Compiler.AddError( CompileErrorCode.InvalidParameters, property.file, property.line );
							}
							break;
						}
						break;

					case Keywords.ID_SCENE_BLEND:
						{
							if ( property.values.Count == 0 )
							{
								Compiler.AddError( CompileErrorCode.StringExpected, property.file, property.line );
							}
							else if ( property.values.Count > 2 )
							{
								Compiler.AddError( CompileErrorCode.FewerParametersExpected, property.file, property.line );
							}
							else if ( property.values.Count == 1 )
							{
							}
							else
							{
							}

						}
						break;
				}
			}

			#endregion Translator Implementation
		}
	}
}

