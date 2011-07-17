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
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <id value="$Id$"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;
using Axiom.Graphics;
using Axiom.Scripting.Compiler.AST;

#endregion Namespace Declarations

namespace Axiom.Scripting.Compiler
{
	public partial class ScriptCompiler
	{
        // The whole thing is private and not used
        // thus it is commented it out for now
        /*
		class GpuProgramParametersTranslator : Translator
		{
			private GpuProgramParameters _parameters;
			private int _animParametricsCount;

			public GpuProgramParametersTranslator()
				: base()
			{
				_parameters = null;
				_animParametricsCount = 0;
			}

			#region Translator Implementation

			internal override bool CheckFor( Keywords nodeId, Keywords parentId )
			{
				throw new NotImplementedException();
			}

			/// <see cref="Translator.Translate"/>
			public override void Translate( ScriptCompiler compiler, AbstractNode node )
			{
				throw new NotImplementedException();
			}

			#endregion Translator Implementation
		}
         */
	}
}
