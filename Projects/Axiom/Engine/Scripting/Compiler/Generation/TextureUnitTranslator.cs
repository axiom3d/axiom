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
using System.Collections.Generic;
using System.Text;

using Axiom.Core;
using Axiom.Graphics;
using Axiom.Math;

using Axiom.Scripting.Compiler.AST;

using Real = System.Single;
using Axiom.Media;

#endregion Namespace Declarations

namespace Axiom.Scripting.Compiler
{
	public partial class ScriptCompiler
	{
		class TextureUnitTranslator : Translator
		{
			private TextureUnitState _textureunit;
			public TextureUnitTranslator( ScriptCompiler compiler, TextureUnitState textureunit )
				: base( compiler )
			{
				_textureunit = textureunit;
			}

			#region Translator Implementation

			protected override void ProcessObject( ObjectAbstractNode obj )
			{
				Compiler.Context = _textureunit;

				// Get the name of the technique
				if ( !string.IsNullOrEmpty( obj.Name ) )
					_textureunit.Name = obj.Name;

				// Set the properties for the technique
				foreach ( AbstractNode node in obj.Children )
				{
					if ( node.Type == AbstractNodeType.Property )
					{
						Translator.Translate( this, node );
					}
					else if ( node.Type == AbstractNodeType.Object )
					{
					}
				}
			}

			protected override void ProcessProperty( PropertyAbstractNode property )
			{
				switch ( (Keywords)property.id )
				{
					case Keywords.ID_TEXTURE:
						{
							if ( property.values.Count == 0 )
							{
								Compiler.AddError( CompileErrorCode.StringExpected, property.File, property.Line );
							}
							else if ( property.values.Count > 5 )
							{
								Compiler.AddError( CompileErrorCode.FewerParametersExpected, property.File, property.Line );
							}
							else
							{
								string name;
								if ( getString( property.values[ 0 ], out name ) )
								{
									TextureType texType = TextureType.TwoD;
									bool isAlpha = false;
									PixelFormat format = PixelFormat.Unknown;
									int mipmaps = -1;

									for ( int index = 1; index < property.values.Count; index++ )
									{
										AbstractNode node = property.values[ index ];
										if ( node.Type == AbstractNodeType.Atom )
										{
											AtomAbstractNode atom = (AtomAbstractNode)node;
											switch ( (Keywords)atom.Id )
											{
												case Keywords.ID_1D:
													texType = TextureType.OneD;
													break;

												case Keywords.ID_2D:
													texType = TextureType.TwoD;
													break;

												case Keywords.ID_3D:
													texType = TextureType.ThreeD;
													break;

												case Keywords.ID_CUBIC:
													texType = TextureType.CubeMap;
													break;

												case Keywords.ID_UNLIMITED:
													mipmaps = 0;
													break;

												case Keywords.ID_ALPHA:
													isAlpha = true;
													break;

												default:
													if ( atom.IsNumber )
														mipmaps = (int)atom.Number;
													else
														;//format = PixelUtil.GetFormatFromName( atom.value, true );
													break;
											}
										}
										else
										{
											Compiler.AddError( CompileErrorCode.InvalidParameters, node.File, node.Line );
										}
									}

									if ( CompilerListener != null )
										CompilerListener.GetTextureNames( name, 1 );

									_textureunit.SetTextureName( name, texType, mipmaps, isAlpha );
								}
								else
								{
									Compiler.AddError( CompileErrorCode.InvalidParameters, property.File, property.Line );
								}

							}

						}
						break;
				}
			}

			#endregion Translator Implementation
		}
	}
}

