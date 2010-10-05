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
using Axiom.Core.Collections;
using Axiom.Graphics;
using Axiom.Math;

using Axiom.Scripting.Compiler.AST;

using Real = System.Single;

#endregion Namespace Declarations

namespace Axiom.Scripting.Compiler
{
	public partial class ScriptCompiler
	{
		class MaterialTranslator : Translator
		{
			private Material _material;
			Dictionary<string, string> _textureAliases = new Dictionary<string, string>();

			public MaterialTranslator( ScriptCompiler compiler )
				: base( compiler )
			{
			}

			#region Translator Implementation

			protected override void ProcessObject( ObjectAbstractNode obj )
			{
				if ( obj.Name == null || obj.Name.Length == 0 )
					Compiler.AddError( CompileErrorCode.ObjectNameExpected, obj.File, obj.Line );

				// Create a material with the given name
				if ( CompilerListener != null )
					_material = CompilerListener.CreateMaterial( obj.Name, Compiler.ResourceGroup );
				else
					_material = (Material)MaterialManager.Instance.Create( obj.Name, Compiler.ResourceGroup );

				if ( _material == null )
				{
					Compiler.AddError( CompileErrorCode.ObjectAllocationError, obj.File, obj.Line );
					return;
				}

				_material.RemoveAllTechniques();
				Compiler.Context = _material;

				// Set the properties for the material
				foreach ( AbstractNode node in obj.Children )
				{
					if ( node.Type == AbstractNodeType.Property )
					{
						Translator.Translate( this, node );
					}
					else if ( node.Type == AbstractNodeType.Object )
					{
						ObjectAbstractNode child = (ObjectAbstractNode)node;
						if ( (Keywords)child.Id == Keywords.ID_TECHNIQUE )
						{
							// Compile the technique
							Technique tec = _material.CreateTechnique();
							TechniqueTranslator translator = new TechniqueTranslator( Compiler, tec );
							Translator.Translate( translator, child );
						}
					}
				}

				// TODO : Apply the texture aliases
				if ( CompilerListener != null )
					CompilerListener.PreApplyTextureAliases( _textureAliases );
				//_material.ApplyTextureAliases( _textureAliases );

			}

			protected override void ProcessProperty( PropertyAbstractNode property )
			{
				switch ( (Keywords)property.id )
				{
					case Keywords.ID_LOD_DISTANCES:
						{
							LodValueList lods = new LodValueList();
							foreach ( AbstractNode node in property.values )
							{
								if ( node.Type == AbstractNodeType.Atom && ( (AtomAbstractNode)node ).IsNumber )
									lods.Add( ( (AtomAbstractNode)node ).Number );
								else
									Compiler.AddError( CompileErrorCode.NumberExpected, node.File, node.Line );
							}
							_material.SetLodLevels( lods );
						}
						break;
					case Keywords.ID_RECEIVE_SHADOWS:
						if ( property.values.Count == 0 )
						{
							Compiler.AddError( CompileErrorCode.StringExpected, property.File, property.Line );
						}
						else if ( property.values.Count > 1 )
						{
							Compiler.AddError( CompileErrorCode.FewerParametersExpected, property.File, property.Line );
						}
						else
						{
							bool val = true;
							if ( getBoolean( property.values[ 0 ], out val ) )
								_material.ReceiveShadows = val;
							else
								Compiler.AddError( CompileErrorCode.InvalidParameters, property.File, property.Line );
						}
						break;
					case Keywords.ID_TRANSPARENCY_CASTS_SHADOWS:
						if ( property.values.Count == 0 )
						{
							Compiler.AddError( CompileErrorCode.StringExpected, property.File, property.Line );
						}
						else if ( property.values.Count > 1 )
						{
							Compiler.AddError( CompileErrorCode.FewerParametersExpected, property.File, property.Line );
						}
						else
						{
							bool val = true;
							if ( getBoolean( property.values[ 0 ], out val ) )
								_material.TransparencyCastsShadows = val;
							else
								Compiler.AddError( CompileErrorCode.InvalidParameters, property.File, property.Line );
						}
						break;
					case Keywords.ID_SET_TEXTURE_ALIAS:
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
							AbstractNode i0 = getNodeAt( property.values, 0 ), i1 = getNodeAt( property.values, 1 );
							String name, value;
							if ( getString( i0, out name ) && getString( i1, out value ) )
								_textureAliases.Add( name, value );
							else
								Compiler.AddError( CompileErrorCode.InvalidParameters, property.File, property.Line );
						}
						break;
				}
			}

			#endregion Translator Implementation
		}
	}
}

