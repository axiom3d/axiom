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

using Axiom.Graphics;
using Axiom.Scripting.Compiler.AST;

#endregion Namespace Declarations

namespace Axiom.Scripting.Compiler
{
	public partial class ScriptCompiler
	{
		public class TechniqueTranslator : Translator
		{
			protected Technique _technique;

			public TechniqueTranslator()
				: base()
			{
				_technique = null;
			}

			#region Translator Implementation

			/// <see cref="Translator.CheckFor"/>
			internal override bool CheckFor( Keywords nodeId, Keywords parentId )
			{
				return nodeId == Keywords.ID_TECHNIQUE && parentId == Keywords.ID_MATERIAL;
			}

			/// <see cref="Translator.Translate"/>
			public override void Translate( ScriptCompiler compiler, AbstractNode node )
			{
				var obj = (ObjectAbstractNode)node;

				// Create the technique from the material
				var material = (Material)obj.Parent.Context;
				_technique = material.CreateTechnique();
				obj.Context = _technique;

				// Get the name of the technique
				if ( !string.IsNullOrEmpty( obj.Name ) )
					_technique.Name = obj.Name;

				// Set the properties for the technique
				foreach ( var i in obj.Children )
				{
					if ( i is PropertyAbstractNode )
					{
						var prop = (PropertyAbstractNode)i;

						switch ( (Keywords)prop.Id )
						{
							#region ID_SCHEME
							case Keywords.ID_SCHEME:
								if ( prop.Values.Count == 0 )
								{
									compiler.AddError( CompileErrorCode.StringExpected, prop.File, prop.Line );
								}
								else if ( prop.Values.Count > 1 )
								{
									compiler.AddError( CompileErrorCode.FewerParametersExpected, prop.File, prop.Line,
										"scheme only supports 1 argument" );
								}
								else
								{
									string scheme;
									if ( getString( prop.Values[ 0 ], out scheme ) )
										_technique.Scheme = scheme;
									else
										compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
											"scheme must have 1 string argument" );
								}
								break;
							#endregion ID_SCHEME

							#region ID_LOD_INDEX
							case Keywords.ID_LOD_INDEX:
								if ( prop.Values.Count == 0 )
								{
									compiler.AddError( CompileErrorCode.StringExpected, prop.File, prop.Line );
								}
								else if ( prop.Values.Count > 1 )
								{
									compiler.AddError( CompileErrorCode.FewerParametersExpected, prop.File, prop.Line,
										"lod_index only supports 1 argument" );
								}
								else
								{
									int val;
									if ( getInt( prop.Values[ 0 ], out val ) )
										_technique.LodIndex = val;
									else
										compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
											"lod_index cannot accept argument \"" + prop.Values[ 0 ].Value + "\"" );
								}
								break;
							#endregion ID_LOD_INDEX

							#region ID_SHADOW_CASTER_MATERIAL
							case Keywords.ID_SHADOW_CASTER_MATERIAL:
								if ( prop.Values.Count == 0 )
								{
									compiler.AddError( CompileErrorCode.StringExpected, prop.File, prop.Line );
								}
								else if ( prop.Values.Count > 1 )
								{
									compiler.AddError( CompileErrorCode.FewerParametersExpected, prop.File, prop.Line,
										"shadow_caster_material only accepts 1 argument" );
								}
								else
								{
									string matName;
									if ( getString( prop.Values[ 0 ], out matName ) )
									{
										var evtMatName = string.Empty;

										ScriptCompilerEvent evt = new ProcessResourceNameScriptCompilerEvent(
											ProcessResourceNameScriptCompilerEvent.ResourceType.Material, matName );

										compiler._fireEvent( ref evt );
										evtMatName = ( (ProcessResourceNameScriptCompilerEvent)evt ).Name;
										_technique.ShadowCasterMaterial = (Material)MaterialManager.Instance[ evtMatName ]; // Use the processed name
									}
									else
										compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
											"shadow_caster_material cannot accept argument \"" + prop.Values[ 0 ].Value + "\"" );
								}
								break;
							#endregion ID_SHADOW_CASTER_MATERIAL

							#region ID_SHADOW_RECEIVER_MATERIAL
							case Keywords.ID_SHADOW_RECEIVER_MATERIAL:
								if ( prop.Values.Count == 0 )
								{
									compiler.AddError( CompileErrorCode.StringExpected, prop.File, prop.Line );
								}
								else if ( prop.Values.Count > 1 )
								{
									compiler.AddError( CompileErrorCode.FewerParametersExpected, prop.File, prop.Line,
										"shadow_receiver_material only accepts 1 argument" );
								}
								else
								{
									var i0 = getNodeAt( prop.Values, 0 );
									var matName = string.Empty;
									if ( getString( i0, out matName ) )
									{
										var evtName = string.Empty;

										ScriptCompilerEvent evt = new ProcessResourceNameScriptCompilerEvent(
											ProcessResourceNameScriptCompilerEvent.ResourceType.Material, matName );

										compiler._fireEvent( ref evt );
										evtName = ( (ProcessResourceNameScriptCompilerEvent)evt ).Name;
										_technique.ShadowReceiverMaterial = (Material)MaterialManager.Instance[ evtName ];
									}
									else
										compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
											"shadow_receiver_material_name cannot accept argument \"" + i0.Value + "\"" );
								}
								break;
							#endregion ID_SHADOW_RECEIVER_MATERIAL

							#region ID_GPU_VENDOR_RULE
							case Keywords.ID_GPU_VENDOR_RULE:
								if ( prop.Values.Count < 2 )
								{
									compiler.AddError( CompileErrorCode.StringExpected, prop.File, prop.Line,
										"gpu_vendor_rule must have 2 arguments" );
								}
								else if ( prop.Values.Count > 2 )
								{
									compiler.AddError( CompileErrorCode.FewerParametersExpected, prop.File, prop.Line,
										"gpu_vendor_rule must have 2 arguments" );
								}
								else
								{
									var i0 = getNodeAt( prop.Values, 0 );
									var i1 = getNodeAt( prop.Values, 1 );

									var rule = new Technique.GPUVendorRule();
									if ( i0 is AtomAbstractNode )
									{
										var atom0 = (AtomAbstractNode)i0;
										var atom0Id = (Keywords)atom0.Id;

										if ( atom0Id == Keywords.ID_INCLUDE )
										{
											rule.Include = true;
										}
										else if ( atom0Id == Keywords.ID_EXCLUDE )
										{
											rule.Include = false;
										}
										else
										{
											compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
												"gpu_vendor_rule cannot accept \"" + i0.Value + "\" as first argument" );
										}

										var vendor = string.Empty;
										if ( !getString( i1, out vendor ) )
										{
											compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
												"gpu_vendor_rule cannot accept \"" + i1.Value + "\" as second argument" );
										}

										rule.Vendor = RenderSystemCapabilities.VendorFromString( vendor );

										if ( rule.Vendor != GPUVendor.Unknown )
										{
											_technique.AddGPUVenderRule( rule );
										}
									}
									else
									{
										compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
											"gpu_vendor_rule cannot accept \"" + i0.Value + "\" as first argument" );
									}
								}
								break;
							#endregion ID_GPU_VENDOR_RULE

							#region ID_GPU_DEVICE_RULE
							case Keywords.ID_GPU_DEVICE_RULE:
								if ( prop.Values.Count < 2 )
								{
									compiler.AddError( CompileErrorCode.StringExpected, prop.File, prop.Line,
										"gpu_device_rule must have at least 2 arguments" );
								}
								else if ( prop.Values.Count > 3 )
								{
									compiler.AddError( CompileErrorCode.FewerParametersExpected, prop.File, prop.Line,
										"gpu_device_rule must have at most 3 arguments" );
								}
								else
								{
									var i0 = getNodeAt( prop.Values, 0 );
									var i1 = getNodeAt( prop.Values, 1 );

									var rule = new Technique.GPUDeviceNameRule();
									if ( i0 is AtomAbstractNode )
									{
										var atom0 = (AtomAbstractNode)i0;
										var atom0Id = (Keywords)atom0.Id;

										if ( atom0Id == Keywords.ID_INCLUDE )
										{
											rule.Include = true;
										}
										else if ( atom0Id == Keywords.ID_EXCLUDE )
										{
											rule.Include = false;
										}
										else
										{
											compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
												"gpu_device_rule cannot accept \"" + i0.Value + "\" as first argument" );
										}

										if ( !getString( i1, out rule.DevicePattern ) )
										{
											compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
												"gpu_device_rule cannot accept \"" + i1.Value + "\" as second argument" );
										}

										if ( prop.Values.Count == 3 )
										{
											var i2 = getNodeAt( prop.Values, 2 );
											if ( !getBoolean( i2, out rule.CaseSensitive ) )
											{
												compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
													"gpu_device_rule third argument must be \"true\", \"false\", \"yes\", \"no\", \"on\", or \"off\"" );
											}
										}

										_technique.AddGPUDeviceNameRule( rule );
									}
									else
									{
										compiler.AddError( CompileErrorCode.InvalidParameters, prop.File, prop.Line,
											"gpu_device_rule cannot accept \"" + i0.Value + "\" as first argument" );
									}
								}
								break;
							#endregion ID_GPU_DEVICE_RULE

							default:
								compiler.AddError( CompileErrorCode.UnexpectedToken, prop.File, prop.Line, "token \"" + prop.Name + "\" is not recognized" );
								break;

						} //end of switch statement
					} // end of if ( i is PropertyAbstractNode )
					else if ( i is ObjectAbstractNode )
					{
						_processNode( compiler, i );
					}
				}
			}

			#endregion Translator Implementation
		}
	}
}

