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

using Axiom.Scripting.Compiler.Parser;
using Axiom.Scripting.Compiler.AST;

#endregion Namespace Declarations

namespace Axiom.Scripting.Compiler
{

	/// <summary>
	/// This is the main class for the compiler. It calls the parser
	/// and processes the CST into an AST and then uses translators
	/// to translate the AST into the final resources.
	/// </summary>
	public partial class ScriptCompiler
	{
		// This enum are built-in word id values
		enum BuiltIn : uint
		{
			ID_ON = 1,
			ID_OFF = 0,
			ID_TRUE = 1,
			ID_FALSE = 0,
			ID_YES = 1,
			ID_NO = 0
		};

		private List<CompileError> _errors = new List<CompileError>();

		private String _resourceGroup;
		public String ResourceGroup
		{
			get
			{
				return _resourceGroup;
			}
		}

		object _context = null;
		public object Context
		{
			get
			{
				return _context;
			}
			set
			{
				_context = value;
			}
		}

		private Dictionary<string, string> _environment = new Dictionary<string, string>();
		public Dictionary<string, string> Environment
		{
			get
			{
				return _environment;
			}
		}

		private Dictionary<string, uint> _keywordMap = new Dictionary<string, uint>();
		public Dictionary<string, uint> KeywordMap
		{
			get
			{
				return _keywordMap;
			}
		}

		private List<AbstractNode> _importTable = new List<AbstractNode>();

		private ScriptCompilerListener _listener;
		public ScriptCompilerListener Listener
		{
			get
			{
				return _listener;
			}
			set
			{
				_listener = value;
			}
		}

		public ScriptCompiler()
		{
			InitializeWordMap();
		}


		/// <summary>
		/// Takes in a string of script code and compiles it into resources
		/// </summary>
		/// <param name="script">The script code</param>
		/// <param name="source">The source of the script code (e.g. a script file)</param>
		/// <param name="group">The resource group to place the compiled resources into</param>
		/// <returns></returns>
		public bool Compile( String script, String source, String group )
		{
			ScriptLexer lexer = new ScriptLexer();
			ScriptParser parser = new ScriptParser();
			IList<ScriptToken> tokens = lexer.Tokenize( script, source );
			IList<ConcreteNode> nodes = parser.Parse( tokens );
			return Compile( nodes, group );
		}

		/// <summary>
		/// Compiles resources from the given concrete node list
		/// </summary>
		/// <param name="nodes">The list of nodes to compile</param>
		/// <param name="group">The resource group to place the compiled resources into</param>
		/// <returns></returns>
		private bool Compile( IList<ConcreteNode> nodes, string group )
		{
			// Save the group
			_resourceGroup = group;

			// Clear the past errors
			_errors.Clear();

			// Clear the environment
			_environment.Clear();

			// Convert our nodes to an AST
			IList<AbstractNode> ast = _convertToAST( nodes );
			// Processes the imports for this script
			//_processImports( ast );
			// Process object inheritance
			_processObjects( ast, ast );
			// Process variable expansion
			//_processVariables( ast );

			// Translate the nodes
			int iter = 0;
			int end = ast.Count;
			while ( iter != end )
			{
				if ( ast[ iter ].Type == AbstractNodeType.Object )
				{
					ObjectAbstractNode obj = (ObjectAbstractNode)( ast[ iter ] );
					switch ( (Keywords)obj.Id )
					{
						case Keywords.ID_MATERIAL:
							{
								MaterialTranslator translator = new MaterialTranslator( this );
								Translator.Translate( translator, ast[ iter ] );
							}
							break;

						case Keywords.ID_PARTICLE_SYSTEM:
							{
								ParticleSystemTranslator translator = new ParticleSystemTranslator( this );
								Translator.Translate( translator, ast[ iter ] );
							}
							break;

						case Keywords.ID_COMPOSITOR:
							{
								CompositorTranslator translator = new CompositorTranslator( this );
								Translator.Translate( translator, ast[ iter ] );
							}
							break;

						case Keywords.ID_VERTEX_PROGRAM:
						case Keywords.ID_FRAGMENT_PROGRAM:
							{
								if ( obj.Values.Count != 0 )
								{
									if ( obj.Values[ 0 ].Type == AbstractNodeType.Atom )
									{
										String language = ( (AtomAbstractNode)( obj.Values[ 0 ] ) ).Value;
										Translator translator = null;
										switch ( language )
										{
											case "asm":
												translator = new GpuProgramTranslator( this );
												break;
											case "unified":
												translator = new UnifiedGpuProgramTranslator( this );
												break;
											default:
												translator = new HighLevelGpuProgramTranslator( this );
												break;
										}
										if ( translator != null )
										{
											Translator.Translate( translator, ast[ iter ] );
										}
									}
									else
									{
										AddError( CompileErrorCode.InvalidParameters, obj.File, obj.Line );
									}
								}
								else
								{
									AddError( CompileErrorCode.StringExpected, obj.File, obj.Line );
								}
							}
							break;
					}
				}
				iter++;
			}
			return true;
		}

		internal void AddError( CompileErrorCode code, string file, uint line )
		{
			CompileError error = new CompileError( code, file, line );
			// OnError ( error );
			_errors.Add( error );
		}

		private IList<AbstractNode> _convertToAST( IList<ConcreteNode> nodes )
		{
			AbstractTreeBuilder builder = new AbstractTreeBuilder( this );
			AbstractTreeBuilder.Visit( builder, nodes );
			return builder.Result;
		}

		private void _processObjects( IList<AbstractNode> nodes, IList<AbstractNode> top )
		{
			foreach ( AbstractNode node in nodes )
			{
				if ( node.Type == AbstractNodeType.Object )
				{
					ObjectAbstractNode obj = (ObjectAbstractNode)node;

					// Check if it is inheriting anything
					if ( obj.BaseClass != null )
					{
						// Check the top level first, then check the import table
						List<AbstractNode> newNodes = _locateTarget( top, obj.BaseClass );
						if ( newNodes.Count == 0 )
							newNodes = _locateTarget( _importTable, obj.BaseClass );

						if ( newNodes.Count != 0 )
						{
							foreach ( AbstractNode j in newNodes )
								_overlayObject( j, obj );
						}
					}

					// Recurse into children
					_processObjects( obj.Children, top );
				}
			}
		}

		private List<AbstractNode> _locateTarget( IList<AbstractNode> nodes, string target )
		{
			AbstractNode iter = null;

			// Search for a top-level object node
			foreach ( AbstractNode node in nodes )
			{
				if ( node.Type == AbstractNodeType.Object )
				{
					ObjectAbstractNode impl = (ObjectAbstractNode)node;
					if ( impl.Name == target )
						iter = node;
				}
			}

			List<AbstractNode> newNodes = new List<AbstractNode>();
			if ( iter != null )
			{
				newNodes.Add( iter );
			}
			return newNodes;
		}

		private void _overlayObject( AbstractNode source, ObjectAbstractNode destination )
		{
			if ( source.Type == AbstractNodeType.Object )
			{
				ObjectAbstractNode src = (ObjectAbstractNode)source;
			}
		}

		private void InitializeWordMap()
		{
			_keywordMap[ "on" ] = (uint)BuiltIn.ID_ON;
			_keywordMap[ "off" ] = (uint)BuiltIn.ID_OFF;
			_keywordMap[ "true" ] = (uint)BuiltIn.ID_TRUE;
			_keywordMap[ "false" ] = (uint)BuiltIn.ID_FALSE;
			_keywordMap[ "yes" ] = (uint)BuiltIn.ID_YES;
			_keywordMap[ "no" ] = (uint)BuiltIn.ID_NO;

			// Material ids
			_keywordMap[ "material" ] = (uint)Keywords.ID_MATERIAL;
			_keywordMap[ "vertex_program" ] = (uint)Keywords.ID_VERTEX_PROGRAM;
			_keywordMap[ "fragment_program" ] = (uint)Keywords.ID_FRAGMENT_PROGRAM;
			_keywordMap[ "technique" ] = (uint)Keywords.ID_TECHNIQUE;
			_keywordMap[ "pass" ] = (uint)Keywords.ID_PASS;
			_keywordMap[ "texture_unit" ] = (uint)Keywords.ID_TEXTURE_UNIT;
			_keywordMap[ "vertex_program_ref" ] = (uint)Keywords.ID_VERTEX_PROGRAM_REF;
			_keywordMap[ "fragment_program_ref" ] = (uint)Keywords.ID_FRAGMENT_PROGRAM_REF;
			_keywordMap[ "shadow_caster_vertex_program_ref" ] = (uint)Keywords.ID_SHADOW_CASTER_VERTEX_PROGRAM_REF;
			_keywordMap[ "shadow_receiver_vertex_program_ref" ] = (uint)Keywords.ID_SHADOW_RECEIVER_VERTEX_PROGRAM_REF;
			_keywordMap[ "shadow_receiver_fragment_program_ref" ] = (uint)Keywords.ID_SHADOW_RECEIVER_FRAGMENT_PROGRAM_REF;

			_keywordMap[ "lod_distances" ] = (uint)Keywords.ID_LOD_DISTANCES;
			_keywordMap[ "receive_shadows" ] = (uint)Keywords.ID_RECEIVE_SHADOWS;
			_keywordMap[ "transparency_casts_shadows" ] = (uint)Keywords.ID_TRANSPARENCY_CASTS_SHADOWS;
			_keywordMap[ "set_texture_alias" ] = (uint)Keywords.ID_SET_TEXTURE_ALIAS;

			_keywordMap[ "source" ] = (uint)Keywords.ID_SOURCE;
			_keywordMap[ "syntax" ] = (uint)Keywords.ID_SYNTAX;
			_keywordMap[ "default_params" ] = (uint)Keywords.ID_DEFAULT_PARAMS;
			_keywordMap[ "param_indexed" ] = (uint)Keywords.ID_PARAM_INDEXED;
			_keywordMap[ "param_named" ] = (uint)Keywords.ID_PARAM_NAMED;
			_keywordMap[ "param_indexed_auto" ] = (uint)Keywords.ID_PARAM_INDEXED_AUTO;
			_keywordMap[ "param_named_auto" ] = (uint)Keywords.ID_PARAM_NAMED_AUTO;

			_keywordMap[ "scheme" ] = (uint)Keywords.ID_SCHEME;
			_keywordMap[ "lod_index" ] = (uint)Keywords.ID_LOD_INDEX;

			_keywordMap[ "ambient" ] = (uint)Keywords.ID_AMBIENT;
			_keywordMap[ "diffuse" ] = (uint)Keywords.ID_DIFFUSE;
			_keywordMap[ "specular" ] = (uint)Keywords.ID_SPECULAR;
			_keywordMap[ "emissive" ] = (uint)Keywords.ID_EMISSIVE;
			_keywordMap[ "vertex_colour" ] = (uint)Keywords.ID_VERTEX_COLOUR;
			_keywordMap[ "scene_blend" ] = (uint)Keywords.ID_SCENE_BLEND;
			_keywordMap[ "colour_blend" ] = (uint)Keywords.ID_COLOUR_BLEND;
			_keywordMap[ "one" ] = (uint)Keywords.ID_ONE;
			_keywordMap[ "zero" ] = (uint)Keywords.ID_ZERO;
			_keywordMap[ "dest_colour" ] = (uint)Keywords.ID_DEST_COLOUR;
			_keywordMap[ "src_colour" ] = (uint)Keywords.ID_SRC_COLOUR;
			_keywordMap[ "one_minus_src_colour" ] = (uint)Keywords.ID_ONE_MINUS_SRC_COLOUR;
			_keywordMap[ "one_minus_dest_colour" ] = (uint)Keywords.ID_ONE_MINUS_DEST_COLOUR;
			_keywordMap[ "dest_alpha" ] = (uint)Keywords.ID_DEST_ALPHA;
			_keywordMap[ "src_alpha" ] = (uint)Keywords.ID_SRC_ALPHA;
			_keywordMap[ "one_minus_dest_alpha" ] = (uint)Keywords.ID_ONE_MINUS_DEST_ALPHA;
			_keywordMap[ "one_minus_src_alpha" ] = (uint)Keywords.ID_ONE_MINUS_SRC_ALPHA;
			_keywordMap[ "separate_scene_blend" ] = (uint)Keywords.ID_SEPARATE_SCENE_BLEND;
			_keywordMap[ "depth_check" ] = (uint)Keywords.ID_DEPTH_CHECK;
			_keywordMap[ "depth_write" ] = (uint)Keywords.ID_DEPTH_WRITE;
			_keywordMap[ "depth_func" ] = (uint)Keywords.ID_DEPTH_FUNC;
			_keywordMap[ "depth_bias" ] = (uint)Keywords.ID_DEPTH_BIAS;
			_keywordMap[ "iteration_depth_bias" ] = (uint)Keywords.ID_ITERATION_DEPTH_BIAS;
			_keywordMap[ "always_fail" ] = (uint)Keywords.ID_ALWAYS_FAIL;
			_keywordMap[ "always_pass" ] = (uint)Keywords.ID_ALWAYS_PASS;
			_keywordMap[ "less_equal" ] = (uint)Keywords.ID_LESS_EQUAL;
			_keywordMap[ "less" ] = (uint)Keywords.ID_LESS;
			_keywordMap[ "equal" ] = (uint)Keywords.ID_EQUAL;
			_keywordMap[ "not_equal" ] = (uint)Keywords.ID_NOT_EQUAL;
			_keywordMap[ "greater_equal" ] = (uint)Keywords.ID_GREATER_EQUAL;
			_keywordMap[ "greater" ] = (uint)Keywords.ID_GREATER;
			_keywordMap[ "alpha_rejection" ] = (uint)Keywords.ID_ALPHA_REJECTION;
			_keywordMap[ "light_scissor" ] = (uint)Keywords.ID_LIGHT_SCISSOR;
			_keywordMap[ "light_clip_planes" ] = (uint)Keywords.ID_LIGHT_CLIP_PLANES;
			_keywordMap[ "illumination_stage" ] = (uint)Keywords.ID_ILLUMINATION_STAGE;
			_keywordMap[ "decal" ] = (uint)Keywords.ID_DECAL;
			_keywordMap[ "cull_hardware" ] = (uint)Keywords.ID_CULL_HARDWARE;
			_keywordMap[ "clockwise" ] = (uint)Keywords.ID_CLOCKWISE;
			_keywordMap[ "anticlockwise" ] = (uint)Keywords.ID_ANTICLOCKWISE;
			_keywordMap[ "cull_software" ] = (uint)Keywords.ID_CULL_SOFTWARE;
			_keywordMap[ "back" ] = (uint)Keywords.ID_BACK;
			_keywordMap[ "front" ] = (uint)Keywords.ID_FRONT;
			_keywordMap[ "normalise_normals" ] = (uint)Keywords.ID_NORMALISE_NORMALS;
			_keywordMap[ "lighting" ] = (uint)Keywords.ID_LIGHTING;
			_keywordMap[ "shading" ] = (uint)Keywords.ID_SHADING;
			_keywordMap[ "flat" ] = (uint)Keywords.ID_FLAT;
			_keywordMap[ "gouraud" ] = (uint)Keywords.ID_GOURAUD;
			_keywordMap[ "phong" ] = (uint)Keywords.ID_PHONG;
			_keywordMap[ "polygon_mode" ] = (uint)Keywords.ID_POLYGON_MODE;
			_keywordMap[ "polygon_mode_overrideable" ] = (uint)Keywords.ID_POLYGON_MODE_OVERRIDEABLE;
			_keywordMap[ "fog_override" ] = (uint)Keywords.ID_FOG_OVERRIDE;
			_keywordMap[ "none" ] = (uint)Keywords.ID_NONE;
			_keywordMap[ "linear" ] = (uint)Keywords.ID_LINEAR;
			_keywordMap[ "exp" ] = (uint)Keywords.ID_EXP;
			_keywordMap[ "exp2" ] = (uint)Keywords.ID_EXP2;
			_keywordMap[ "colour_write" ] = (uint)Keywords.ID_COLOUR_WRITE;
			_keywordMap[ "max_lights" ] = (uint)Keywords.ID_MAX_LIGHTS;
			_keywordMap[ "start_light" ] = (uint)Keywords.ID_START_LIGHT;
			_keywordMap[ "iteration" ] = (uint)Keywords.ID_ITERATION;
			_keywordMap[ "once" ] = (uint)Keywords.ID_ONCE;
			_keywordMap[ "once_per_light" ] = (uint)Keywords.ID_ONCE_PER_LIGHT;
			_keywordMap[ "per_n_lights" ] = (uint)Keywords.ID_PER_N_LIGHTS;
			_keywordMap[ "per_light" ] = (uint)Keywords.ID_PER_LIGHT;
			_keywordMap[ "point" ] = (uint)Keywords.ID_POINT;
			_keywordMap[ "spot" ] = (uint)Keywords.ID_SPOT;
			_keywordMap[ "directional" ] = (uint)Keywords.ID_DIRECTIONAL;
			_keywordMap[ "point_size" ] = (uint)Keywords.ID_POINT_SIZE;
			_keywordMap[ "point_sprites" ] = (uint)Keywords.ID_POINT_SPRITES;
			_keywordMap[ "point_size_min" ] = (uint)Keywords.ID_POINT_SIZE_MIN;
			_keywordMap[ "point_size_max" ] = (uint)Keywords.ID_POINT_SIZE_MAX;

			_keywordMap[ "texture_alias" ] = (uint)Keywords.ID_TEXTURE_ALIAS;
			_keywordMap[ "texture" ] = (uint)Keywords.ID_TEXTURE;
			_keywordMap[ "1d" ] = (uint)Keywords.ID_1D;
			_keywordMap[ "2d" ] = (uint)Keywords.ID_2D;
			_keywordMap[ "3d" ] = (uint)Keywords.ID_3D;
			_keywordMap[ "cubic" ] = (uint)Keywords.ID_CUBIC;
			_keywordMap[ "unlimited" ] = (uint)Keywords.ID_UNLIMITED;
			_keywordMap[ "alpha" ] = (uint)Keywords.ID_ALPHA;
			_keywordMap[ "anim_texture" ] = (uint)Keywords.ID_ANIM_TEXTURE;
			_keywordMap[ "cubic_texture" ] = (uint)Keywords.ID_CUBIC_TEXTURE;
			_keywordMap[ "separateUV" ] = (uint)Keywords.ID_SEPARATE_UV;
			_keywordMap[ "combinedUVW" ] = (uint)Keywords.ID_COMBINED_UVW;
			_keywordMap[ "tex_coord_set" ] = (uint)Keywords.ID_TEX_COORD_SET;
			_keywordMap[ "tex_address_mode" ] = (uint)Keywords.ID_TEX_ADDRESS_MODE;
			_keywordMap[ "wrap" ] = (uint)Keywords.ID_WRAP;
			_keywordMap[ "clamp" ] = (uint)Keywords.ID_CLAMP;
			_keywordMap[ "mirror" ] = (uint)Keywords.ID_MIRROR;
			_keywordMap[ "border" ] = (uint)Keywords.ID_BORDER;
			_keywordMap[ "filtering" ] = (uint)Keywords.ID_FILTERING;
			_keywordMap[ "bilinear" ] = (uint)Keywords.ID_BILINEAR;
			_keywordMap[ "trilinear" ] = (uint)Keywords.ID_TRILINEAR;
			_keywordMap[ "anisotropic" ] = (uint)Keywords.ID_ANISOTROPIC;
			_keywordMap[ "max_anisotropy" ] = (uint)Keywords.ID_MAX_ANISOTROPY;
			_keywordMap[ "mipmap_bias" ] = (uint)Keywords.ID_MIPMAP_BIAS;
			_keywordMap[ "colour_op" ] = (uint)Keywords.ID_COLOUR_OP;
			_keywordMap[ "replace" ] = (uint)Keywords.ID_REPLACE;
			_keywordMap[ "add" ] = (uint)Keywords.ID_ADD;
			_keywordMap[ "modulate" ] = (uint)Keywords.ID_MODULATE;
			_keywordMap[ "alpha_blend" ] = (uint)Keywords.ID_ALPHA_BLEND;
			_keywordMap[ "colour_op_ex" ] = (uint)Keywords.ID_COLOUR_OP_EX;
			_keywordMap[ "source1" ] = (uint)Keywords.ID_SOURCE1;
			_keywordMap[ "source2" ] = (uint)Keywords.ID_SOURCE2;
			_keywordMap[ "modulate" ] = (uint)Keywords.ID_MODULATE;
			_keywordMap[ "modulate_x2" ] = (uint)Keywords.ID_MODULATE_X2;
			_keywordMap[ "modulate_x4" ] = (uint)Keywords.ID_MODULATE_X4;
			_keywordMap[ "add_signed" ] = (uint)Keywords.ID_ADD_SIGNED;
			_keywordMap[ "add_smooth" ] = (uint)Keywords.ID_ADD_SMOOTH;
			_keywordMap[ "blend_diffuse_alpha" ] = (uint)Keywords.ID_BLEND_DIFFUSE_ALPHA;
			_keywordMap[ "blend_texture_alpha" ] = (uint)Keywords.ID_BLEND_TEXTURE_ALPHA;
			_keywordMap[ "blend_current_alpha" ] = (uint)Keywords.ID_BLEND_CURRENT_ALPHA;
			_keywordMap[ "blend_manual" ] = (uint)Keywords.ID_BLEND_MANUAL;
			_keywordMap[ "dotproduct" ] = (uint)Keywords.ID_DOT_PRODUCT;
			_keywordMap[ "blend_diffuse_colour" ] = (uint)Keywords.ID_BLEND_DIFFUSE_COLOUR;
			_keywordMap[ "src_current" ] = (uint)Keywords.ID_SRC_CURRENT;
			_keywordMap[ "src_texture" ] = (uint)Keywords.ID_SRC_TEXTURE;
			_keywordMap[ "src_diffuse" ] = (uint)Keywords.ID_SRC_DIFFUSE;
			_keywordMap[ "src_specular" ] = (uint)Keywords.ID_SRC_SPECULAR;
			_keywordMap[ "src_manual" ] = (uint)Keywords.ID_SRC_MANUAL;
			_keywordMap[ "colour_op_multipass_fallback" ] = (uint)Keywords.ID_COLOUR_OP_MULTIPASS_FALLBACK;
			_keywordMap[ "alpha_op_ex" ] = (uint)Keywords.ID_ALPHA_OP_EX;
			_keywordMap[ "env_map" ] = (uint)Keywords.ID_ENV_MAP;
			_keywordMap[ "spherical" ] = (uint)Keywords.ID_SPHERICAL;
			_keywordMap[ "planar" ] = (uint)Keywords.ID_PLANAR;
			_keywordMap[ "cubic_reflection" ] = (uint)Keywords.ID_CUBIC_REFLECTION;
			_keywordMap[ "cubic_normal" ] = (uint)Keywords.ID_CUBIC_NORMAL;
			_keywordMap[ "scroll" ] = (uint)Keywords.ID_SCROLL;
			_keywordMap[ "scroll_anim" ] = (uint)Keywords.ID_SCROLL_ANIM;
			_keywordMap[ "rotate" ] = (uint)Keywords.ID_ROTATE;
			_keywordMap[ "rotate_anim" ] = (uint)Keywords.ID_ROTATE_ANIM;
			_keywordMap[ "scale" ] = (uint)Keywords.ID_SCALE;
			_keywordMap[ "wave_xform" ] = (uint)Keywords.ID_WAVE_XFORM;
			_keywordMap[ "scroll_x" ] = (uint)Keywords.ID_SCROLL_X;
			_keywordMap[ "scroll_y" ] = (uint)Keywords.ID_SCROLL_Y;
			_keywordMap[ "scale_x" ] = (uint)Keywords.ID_SCALE_X;
			_keywordMap[ "scale_y" ] = (uint)Keywords.ID_SCALE_Y;
			_keywordMap[ "sine" ] = (uint)Keywords.ID_SINE;
			_keywordMap[ "triangle" ] = (uint)Keywords.ID_TRIANGLE;
			_keywordMap[ "sawtooth" ] = (uint)Keywords.ID_SAWTOOTH;
			_keywordMap[ "square" ] = (uint)Keywords.ID_SQUARE;
			_keywordMap[ "inverse_sawtooth" ] = (uint)Keywords.ID_INVERSE_SAWTOOTH;
			_keywordMap[ "transform" ] = (uint)Keywords.ID_TRANSFORM;
			_keywordMap[ "binding_type" ] = (uint)Keywords.ID_BINDING_TYPE;
			_keywordMap[ "vertex" ] = (uint)Keywords.ID_VERTEX;
			_keywordMap[ "fragment" ] = (uint)Keywords.ID_FRAGMENT;
			_keywordMap[ "content_type" ] = (uint)Keywords.ID_CONTENT_TYPE;
			_keywordMap[ "named" ] = (uint)Keywords.ID_NAMED;
			_keywordMap[ "shadow" ] = (uint)Keywords.ID_SHADOW;

			// Particle system
			_keywordMap[ "particle_system" ] = (uint)Keywords.ID_PARTICLE_SYSTEM;
			_keywordMap[ "emitter" ] = (uint)Keywords.ID_EMITTER;
			_keywordMap[ "affector" ] = (uint)Keywords.ID_AFFECTOR;

			// Compositor
			_keywordMap[ "compositor" ] = (uint)Keywords.ID_COMPOSITOR;
			_keywordMap[ "target" ] = (uint)Keywords.ID_TARGET;
			_keywordMap[ "target_output" ] = (uint)Keywords.ID_TARGET_OUTPUT;

			_keywordMap[ "input" ] = (uint)Keywords.ID_INPUT;
			_keywordMap[ "none" ] = (uint)Keywords.ID_NONE;
			_keywordMap[ "previous" ] = (uint)Keywords.ID_PREVIOUS;
			_keywordMap[ "target_width" ] = (uint)Keywords.ID_TARGET_WIDTH;
			_keywordMap[ "target_height" ] = (uint)Keywords.ID_TARGET_HEIGHT;
			_keywordMap[ "only_initial" ] = (uint)Keywords.ID_ONLY_INITIAL;
			_keywordMap[ "visibility_mask" ] = (uint)Keywords.ID_VISIBILITY_MASK;
			_keywordMap[ "lod_bias" ] = (uint)Keywords.ID_LOD_BIAS;
			_keywordMap[ "material_scheme" ] = (uint)Keywords.ID_MATERIAL_SCHEME;

			_keywordMap[ "clear" ] = (uint)Keywords.ID_CLEAR;
			_keywordMap[ "stencil" ] = (uint)Keywords.ID_STENCIL;
			_keywordMap[ "render_scene" ] = (uint)Keywords.ID_RENDER_SCENE;
			_keywordMap[ "render_quad" ] = (uint)Keywords.ID_RENDER_QUAD;
			_keywordMap[ "identifier" ] = (uint)Keywords.ID_IDENTIFIER;
			_keywordMap[ "first_render_queue" ] = (uint)Keywords.ID_FIRST_RENDER_QUEUE;
			_keywordMap[ "last_render_queue" ] = (uint)Keywords.ID_LAST_RENDER_QUEUE;

			_keywordMap[ "buffers" ] = (uint)Keywords.ID_BUFFERS;
			_keywordMap[ "colour" ] = (uint)Keywords.ID_COLOUR;
			_keywordMap[ "depth" ] = (uint)Keywords.ID_DEPTH;
			_keywordMap[ "colour_value" ] = (uint)Keywords.ID_COLOUR_VALUE;
			_keywordMap[ "depth_value" ] = (uint)Keywords.ID_DEPTH_VALUE;
			_keywordMap[ "stencil_value" ] = (uint)Keywords.ID_STENCIL_VALUE;

			_keywordMap[ "check" ] = (uint)Keywords.ID_CHECK;
			_keywordMap[ "comp_func" ] = (uint)Keywords.ID_COMP_FUNC;
			_keywordMap[ "ref_value" ] = (uint)Keywords.ID_REF_VALUE;
			_keywordMap[ "mask" ] = (uint)Keywords.ID_MASK;
			_keywordMap[ "fail_op" ] = (uint)Keywords.ID_FAIL_OP;
			_keywordMap[ "keep" ] = (uint)Keywords.ID_KEEP;
			_keywordMap[ "increment" ] = (uint)Keywords.ID_INCREMENT;
			_keywordMap[ "decrement" ] = (uint)Keywords.ID_DECREMENT;
			_keywordMap[ "increment_wrap" ] = (uint)Keywords.ID_INCREMENT_WRAP;
			_keywordMap[ "decrement_wrap" ] = (uint)Keywords.ID_DECREMENT_WRAP;
			_keywordMap[ "invert" ] = (uint)Keywords.ID_INVERT;
			_keywordMap[ "depth_fail_op" ] = (uint)Keywords.ID_DEPTH_FAIL_OP;
			_keywordMap[ "pass_op" ] = (uint)Keywords.ID_PASS_OP;
			_keywordMap[ "two_sided" ] = (uint)Keywords.ID_TWO_SIDED;
		}
	}
}
