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

using Axiom.ParticleSystems;
using Axiom.Scripting.Compiler.AST;

#endregion Namespace Declarations

namespace Axiom.Scripting.Compiler
{
	public partial class ScriptCompiler
	{
		public class ParticleEmitterTranslator : Translator
		{
			protected ParticleEmitter _Emitter;

			public ParticleEmitterTranslator()
				: base()
			{
				_Emitter = null;
			}

			#region Translator Implementation

			/// <see cref="Translator.CheckFor"/>
			public override bool CheckFor( Keywords nodeId, Keywords parentId )
			{
				return nodeId == Keywords.ID_EMITTER;
			}

			/// <see cref="Translator.Translate"/>
			public override void Translate( ScriptCompiler compiler, AbstractNode node )
			{
				var obj = (ObjectAbstractNode)node;

				// Must have a type as the first value
				if ( obj.Values.Count == 0 )
				{
					compiler.AddError( CompileErrorCode.StringExpected, obj.File, obj.Line );
					return;
				}

				var type = string.Empty;
				if ( !getString( obj.Values[ 0 ], out type ) )
				{
					compiler.AddError( CompileErrorCode.InvalidParameters, obj.File, obj.Line );
					return;
				}

				var system = (ParticleSystem)obj.Parent.Context;
				_Emitter = system.AddEmitter( type );

				foreach ( var i in obj.Children )
				{
					if ( i is PropertyAbstractNode )
					{
						var prop = (PropertyAbstractNode)i;
						var value = string.Empty;

						// Glob the values together
						foreach ( var it in prop.Values )
						{
							if ( it is AtomAbstractNode )
							{
								if ( string.IsNullOrEmpty( value ) )
								{
									value = ( (AtomAbstractNode)it ).Value;
								}
								else
								{
									value = value + " " + ( (AtomAbstractNode)it ).Value;
								}
							}
							else
							{
								compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line );
								break;
							}
						}

						if ( !_Emitter.SetParam( prop.Name, value ) )
						{
							compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line );
						}
					}
					else
					{
						processNode( compiler, i );
					}
				}
			}

			#endregion Translator Implementation
		}
	}
}