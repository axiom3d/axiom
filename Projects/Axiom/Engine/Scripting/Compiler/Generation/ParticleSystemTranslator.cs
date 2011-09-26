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
		public class ParticleSystemTranslator : Translator
		{
			protected ParticleSystem _System;

			public ParticleSystemTranslator()
				: base()
			{
				_System = null;
			}

			#region Translator Implementation

			/// <see cref="Translator.CheckFor"/>
			internal override bool CheckFor( Keywords nodeId, Keywords parentId )
			{
				return nodeId == Keywords.ID_PARTICLE_SYSTEM;
			}

			/// <see cref="Translator.Translate"/>
			public override void Translate( ScriptCompiler compiler, AbstractNode node )
			{
				var obj = (ObjectAbstractNode)node;

				// Find the name
				if ( obj != null )
				{
					if ( string.IsNullOrEmpty( obj.Name ) )
					{
						compiler.AddError( CompileErrorCode.ObjectNameExpected, obj.File, obj.Line );
						return;
					}
				}
				else
				{
					compiler.AddError( CompileErrorCode.ObjectNameExpected, obj.File, obj.Line );
					return;
				}

				// Allocate the particle system
				object sysObject;
				ScriptCompilerEvent evt = new CreateParticleSystemScriptCompilerEvent( obj.File, obj.Name, compiler.ResourceGroup );
				var processed = compiler._fireEvent( ref evt, out sysObject );

				if ( !processed )
				{
					_System = ParticleSystemManager.Instance.CreateTemplate( obj.Name, compiler.ResourceGroup );
				}
				else
					_System = (ParticleSystem)sysObject;

				if ( _System == null )
				{
					compiler.AddError( CompileErrorCode.ObjectAllocationError, obj.File, obj.Line );
					return;
				}

				_System.Origin = obj.File;

				_System.RemoveAllEmitters();
				;
				_System.RemoveAllAffectors();

				obj.Context = _System;

				foreach ( var i in obj.Children )
				{
					if ( i is PropertyAbstractNode )
					{
						var prop = (PropertyAbstractNode)i;
						switch ( (Keywords)prop.Id )
						{
							case Keywords.ID_MATERIAL:
								if ( prop.Values.Count == 0 )
								{
									compiler.AddError( CompileErrorCode.StringExpected, prop.File, prop.Line );
									return;
								}
								else
								{
									if ( prop.Values[ 0 ] is AtomAbstractNode )
									{
										var name = ( (AtomAbstractNode)prop.Values[ 0 ] ).Value;

										ScriptCompilerEvent locEvt = new ProcessResourceNameScriptCompilerEvent(
											ProcessResourceNameScriptCompilerEvent.ResourceType.Material, name );

										compiler._fireEvent( ref locEvt );
										var locEvtName = ( (ProcessResourceNameScriptCompilerEvent)locEvt ).Name;

										if ( !_System.SetParameter( "material", locEvtName ) )
										{
											if ( _System.Renderer != null )
											{
												if ( !_System.Renderer.SetParameter( "material", locEvtName ) )
												{
													compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
														"material property could not be set with material \"" + locEvtName + "\"" );
												}
											}
										}
									}
								}
								break;

							default:
								if ( prop.Values.Count == 0 )
								{
									compiler.AddError( CompileErrorCode.StringExpected, prop.File, prop.Line );
									return;
								}
								else
								{
									string name = prop.Name, value = string.Empty;

									// Glob the values together
									foreach ( var it in prop.Values )
									{
										if ( it is AtomAbstractNode )
										{
											if ( string.IsNullOrEmpty( value ) )
												value = ( (AtomAbstractNode)it ).Value;
											else
												value = value + " " + ( (AtomAbstractNode)it ).Value;
										}
										else
										{
											compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line );
											return;
										}
									}

									if ( !_System.SetParameter( name, value ) )
									{
										if ( _System.Renderer != null )
										{
											if ( !_System.Renderer.SetParameter( name, value ) )
												compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line );
										}
									}
								}
								break;
						}
					}
					else
					{
						_processNode( compiler, i );
					}
				}
			}

			#endregion Translator Implementation
		}
	}
}

