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
		class HighLevelGpuProgramTranslator : Translator
		{
			public HighLevelGpuProgramTranslator( ScriptCompiler compiler )
				: base( compiler )
			{
			}

			#region Translator Implementation

			protected override void ProcessObject( ObjectAbstractNode obj )
			{
				if ( obj.name.Length == 0 )
				{
					Compiler.AddError( CompileErrorCode.ObjectNameExpected, obj.file, obj.line );
					return;
				}

				List<Pair<String>> customParameters = new List<Pair<String>>();
				String source = "", language = ( (AtomAbstractNode)( obj.values[ 0 ] ) ).value;
				AbstractNode param = null;
				foreach ( AbstractNode child in obj.children )
				{
					if ( child.type == AbstractNodeType.Property )
					{
						PropertyAbstractNode prop = (PropertyAbstractNode)child;
						if ( (Keywords)prop.id == Keywords.ID_SOURCE )
						{
							if ( prop.values.Count != 0 )
							{
								if ( prop.values[ 0 ].type == AbstractNodeType.Atom )
									source = ((AtomAbstractNode)(prop.values[0])).value;
								else
									Compiler.AddError( CompileErrorCode.InvalidParameters, prop.file, prop.line );
							}
							else
							{
								Compiler.AddError( CompileErrorCode.StringExpected, prop.file, prop.line );
							}
						}
						else
						{
							String name = prop.name, value = "";
							if ( prop.values.Count != 0 && prop.values[ 0 ].type == AbstractNodeType.Atom )
								value = ( (AtomAbstractNode)( prop.values[ 0 ] ) ).value;
							customParameters.Add( new Pair<String>(name, value) );
						}
					}
					else if ( child.type == AbstractNodeType.Object )
					{
						if ( (Keywords)(( (ObjectAbstractNode)child ).id) == Keywords.ID_DEFAULT_PARAMS )
							param = child;
					}
				}

				// Allocate the program
				HighLevelGpuProgram prog;
				if ( CompilerListener != null )
					prog = CompilerListener.CreateHighLevelGpuProgram( obj.name, Compiler.ResourceGroup, language, (Keywords)( obj.id ) == Keywords.ID_VERTEX_PROGRAM ? GpuProgramType.Vertex : GpuProgramType.Fragment, source );
				else
				{
					prog = HighLevelGpuProgramManager.Instance.CreateProgram( obj.name, Compiler.ResourceGroup, language, (Keywords)( obj.id ) == Keywords.ID_VERTEX_PROGRAM ? GpuProgramType.Vertex : GpuProgramType.Fragment );
					prog.SourceFile = source;
				}

				// Check that allocation worked
				if ( prog == null )
				{
					Compiler.AddError( CompileErrorCode.ObjectAllocationError, obj.file, obj.line );
					return;
				}

				Compiler.Context = prog;
				prog.Origin = obj.file;

				// Set the custom parameters
				foreach ( Pair<String> item in customParameters )
					prog.Properties[ item.first ] = item.second;

				// Set up default parameters
				if(prog.IsSupported && param != null)
				{
				    GpuProgramParameters ptr = prog.DefaultParameters;
				    Translator.Translate(new GpuProgramParametersTranslator(Compiler, ptr), param);
				}
				//prog.Touch();
			}

			protected override void ProcessProperty( PropertyAbstractNode node )
			{
			}

			#endregion Translator Implementation

		}
	}
}
