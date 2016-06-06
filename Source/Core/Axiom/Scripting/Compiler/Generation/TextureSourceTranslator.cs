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

using System;
using Axiom.Graphics;
using Axiom.Scripting.Compiler.AST;

#endregion Namespace Declarations

namespace Axiom.Scripting.Compiler
{
	public partial class ScriptCompiler
	{
		public class TextureSourceTranslator : Translator
		{
			public TextureSourceTranslator()
				: base()
			{
			}

			#region Translator Implementation

			/// <see cref="Translator.CheckFor"/>
			public override bool CheckFor( Keywords nodeId, Keywords parentId )
			{
				return nodeId == Keywords.ID_TEXTURE_SOURCE && parentId == Keywords.ID_TEXTURE_UNIT;
			}

			/// <see cref="Translator.Translate"/>
			public override void Translate( ScriptCompiler compiler, AbstractNode node )
			{
				throw new NotImplementedException();
#if UNREACHABLE_CODE
				ObjectAbstractNode obj = (ObjectAbstractNode)node;

				// It has to have one value identifying the texture source name
				if ( obj.Values.Count == 0 )
				{
					compiler.AddError( CompileErrorCode.StringExpected, node.File, node.Line,
						"texture_source requires a type value" );
					return;
				}

				// Set the value of the source
				//TODO: ExternalTextureSourceManager::getSingleton().setCurrentPlugIn(obj->values.front()->getValue());

				// Set up the technique, pass, and texunit levels
				if ( true/*TODO: ExternalTextureSourceManager::getSingleton().getCurrentPlugIn() != 0*/)
				{
					TextureUnitState texunit = (TextureUnitState)obj.Parent.Context;
					Pass pass = texunit.Parent;
					Technique technique = pass.Parent;
					Material material = technique.Parent;

					ushort techniqueIndex = 0, passIndex = 0, texUnitIndex = 0;
					for ( ushort i = 0; i < material.TechniqueCount; i++ )
					{
						if ( material.GetTechnique( i ) == technique )
						{
							techniqueIndex = i;
							break;
						}
					}
					for ( ushort i = 0; i < technique.PassCount; i++ )
					{
						if ( technique.GetPass( i ) == pass )
						{
							passIndex = i;
							break;
						}
					}
					for ( ushort i = 0; i < pass.TextureUnitStageCount; i++ )
					{
						if ( pass.GetTextureUnitState( i ) == texunit )
						{
							texUnitIndex = i;
							break;
						}
					}

					string tps = string.Format( "{0} {1} {2}", techniqueIndex, passIndex, texUnitIndex );

					//TODO: ExternalTextureSourceManager::getSingleton().getCurrentPlugIn()->setParameter( "set_T_P_S", tps );

					foreach ( AbstractNode i in obj.Children )
					{
						if ( i is PropertyAbstractNode )
						{
							PropertyAbstractNode prop = (PropertyAbstractNode)i;
							// Glob the property values all together
							string str = string.Empty;

							foreach ( AbstractNode j in prop.Values )
							{
								if ( j != prop.Values[ 0 ] )
									str += " ";

								str = str + j.Value;
							}
							//TODO: ExternalTextureSourceManager::getSingleton().getCurrentPlugIn()->setParameter(prop->name, str);
						}
						else if ( i is ObjectAbstractNode )
						{
							_processNode( compiler, i );
						}
					}

					//TODO: ExternalTextureSourceManager::getSingleton().getCurrentPlugIn()->createDefinedTexture(material->getName(), material->getGroup());
				}
#endif
			}

			#endregion Translator Implementation
		}
	}
}