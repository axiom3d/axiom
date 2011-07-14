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
/*
 * Many thanks to the folks at Multiverse for providing the initial port for this class
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
using System.Globalization;
using System.IO;
using System.Text;

using Axiom.Core;
using Axiom.Media;
using Axiom.Scripting;

#endregion Namespace Declarations

namespace Axiom.Graphics
{
	static class CompositorScriptLoader
	{
		/// <summary>
		///		Enum to identify compositor sections.
		/// </summary>
		private enum CompositorScriptSection
		{
			None,
			Compositor,
			Technique,
			Target,
			Pass,
			Clear,
			Stencil
		}

		/// <summary>
		///		Struct for holding the script context while parsing.
		/// </summary>
		private class CompositorScriptContext
		{
			public CompositorScriptSection section = CompositorScriptSection.None;
			public Compositor compositor = null;
			public CompositionTechnique technique = null;
			public CompositionPass pass = null;
			public CompositionTargetPass target = null;
			public bool seenOpen = false;
			// Error reporting state
			public int lineNo;
			public string line;
			public string filename;
		}

		public static void ParseScript( CompositorManager compositorManager, Stream data, string groupName, string fileName )
		{
			string file = ( (FileStream)data ).Name;
			string line = "";
			CompositorScriptContext context = new CompositorScriptContext();
			context.filename = file;
			context.lineNo = 0;

			StreamReader script = new StreamReader( data, System.Text.Encoding.UTF8 );

			// parse through the data to the end
			while ( ( line = ParseHelper.ReadLine( script ) ) != null )
			{
				context.lineNo++;
				string[] splitCmd;
				string[] args;
				string arg;
				// ignore blank lines and comments
				if ( !( line.Length == 0 || line.StartsWith( "//" ) ) )
				{
					context.line = line;
					splitCmd = SplitByWhitespace( line, 2 );
					string token = splitCmd[ 0 ];
					args = SplitArgs( splitCmd.Length == 2 ? splitCmd[ 1 ] : "" );
					arg = ( args.Length > 0 ? args[ 0 ] : "" );
					if ( context.section == CompositorScriptSection.None )
					{
						if ( token != "compositor" )
						{
							LogError( context, "First token is not 'compositor'!" );
							break; // Give up
						}
						string compositorName = RemoveQuotes( splitCmd[ 1 ].Trim() );
						context.compositor = (Compositor)compositorManager.Create( compositorName, groupName );
						context.section = CompositorScriptSection.Compositor;
						context.seenOpen = false;
						continue; // next line
					}
					else
					{
						if ( !context.seenOpen )
						{
							if ( token == "{" )
								context.seenOpen = true;
							else
								LogError( context, "Expected open brace; instead got {0}", token );
							continue; // next line
						}
						switch ( context.section )
						{
							case CompositorScriptSection.Compositor:
								switch ( token )
								{
									case "technique":
										context.section = CompositorScriptSection.Technique;
										context.technique = context.compositor.CreateTechnique();
										context.seenOpen = false;
										continue; // next line
									case "}":
										context.section = CompositorScriptSection.None;
										context.seenOpen = false;
										if ( context.technique == null )
										{
											LogError( context, "No 'technique' section in compositor" );
											continue;
										}
										break;
									default:
										LogError( context,
												 "After opening brace '{' of compositor definition, expected 'technique', but got '{0}'",
												 token );
										continue; // next line
								}
								break;
							case CompositorScriptSection.Technique:
								switch ( token )
								{
									case "texture":
										ParseTextureLine( context, args );
										break;
									case "target":
										context.section = CompositorScriptSection.Target;
										context.target = context.technique.CreateTargetPass();
										context.target.OutputName = arg.Trim();
										context.seenOpen = false;
										break;
									case "target_output":
										context.section = CompositorScriptSection.Target;
										context.target = context.technique.OutputTarget;
										context.seenOpen = false;
										break;
									case "compositor_logic":
										if ( !OptionCount( context, token, 1, args.Length ) )
											break;
										context.technique.CompositorLogicName = args[ 0 ].Trim();
										break;
									case "}":
										context.section = CompositorScriptSection.Compositor;
										context.seenOpen = true;
										break;
									default:
										LogIllegal( context, "technique", token );
										break;
								}
								break;
							case CompositorScriptSection.Target:
								switch ( token )
								{
									case "input":
										if ( OptionCount( context, token, 1, args.Length ) )
										{
											arg = args[ 0 ];
											if ( arg == "previous" )
												context.target.InputMode = CompositorInputMode.Previous;
											else if ( arg == "none" )
												context.target.InputMode = CompositorInputMode.None;
											else
												LogError( context, "Illegal 'input' arg '{0}'", arg );
										}
										break;
									case "only_initial":
										context.target.OnlyInitial = OnOffArg( context, token, args );
										break;
									case "visibility_mask":
										if ( !OptionCount( context, token, 1, args.Length ) )
											break;
										context.target.VisibilityMask = ParseUint( context, arg );
										break;
									case "lod_bias":
										if ( !OptionCount( context, token, 1, args.Length ) )
											break;
										context.target.LodBias = ParseInt( context, arg );
										break;
									case "material_scheme":
										if ( !OptionCount( context, token, 1, args.Length ) )
											break;
										context.target.MaterialScheme = arg.Trim();
										break;
									case "pass":
										context.section = CompositorScriptSection.Pass;
										context.pass = context.target.CreatePass();
										context.seenOpen = false;
										if ( !OptionCount( context, token, 1, args.Length ) && !OptionCount( context, token, 2, args.Length ) )
											break;
										arg = args[ 0 ].Trim();
										switch ( arg )
										{
											case "render_quad":
												context.pass.Type = CompositorPassType.RenderQuad;
												break;
											case "clear":
												context.pass.Type = CompositorPassType.Clear;
												break;
											case "stencil":
												context.pass.Type = CompositorPassType.Stencil;
												break;
											case "render_scene":
												context.pass.Type = CompositorPassType.RenderScene;
												break;
											case "render_custom":
												context.pass.Type = CompositorPassType.RenderCustom;
												context.pass.CustomType = args[ 1 ].Trim();
												break;
											default:
												LogError( context, "In line '{0}', unrecognized compositor pass type '{1}'", arg );
												break;
										}
										break;
									case "}":
										context.section = CompositorScriptSection.Technique;
										context.seenOpen = true;
										break;
									default:
										LogIllegal( context, "target", token );
										break;
								}
								break;
							case CompositorScriptSection.Pass:
								switch ( token )
								{
									case "first_render_queue":
										if ( !OptionCount( context, token, 1, args.Length ) )
											break;
										context.pass.FirstRenderQueue = (RenderQueueGroupID)ParseInt( context, args[ 0 ] );
										break;
									case "last_render_queue":
										if ( !OptionCount( context, token, 1, args.Length ) )
											break;
										context.pass.LastRenderQueue = (RenderQueueGroupID)ParseInt( context, args[ 0 ] );
										break;
									case "identifier":
										if ( !OptionCount( context, token, 1, args.Length ) )
											break;
										context.pass.Identifier = ParseUint( context, args[ 0 ] );
										break;
									case "material":
										if ( !OptionCount( context, token, 1, args.Length ) )
											break;
										context.pass.MaterialName = args[ 0 ].Trim();
										break;
									case "input":
										if ( !OptionCount( context, token, 3, args.Length ) )
											break;
										int index = 0;
										if ( args.Length == 3 )
											index = ParseInt( context, args[ 2 ] );
										context.pass.SetInput( ParseInt( context, args[ 0 ] ), args[ 1 ].Trim(), index );
										break;
									case "clear":
										context.section = CompositorScriptSection.Clear;
										context.seenOpen = false;
										break;
									case "stencil":
										context.section = CompositorScriptSection.Clear;
										context.seenOpen = false;
										break;
									case "}":
										context.section = CompositorScriptSection.Target;
										context.seenOpen = true;
										break;
									default:
										LogIllegal( context, "pass", token );
										break;
								}
								break;
							case CompositorScriptSection.Clear:
								switch ( token )
								{
									case "buffers":
										FrameBufferType fb = (FrameBufferType)0;
										foreach ( string cb in args )
										{
											switch ( cb )
											{
												case "colour":
													fb |= FrameBufferType.Color;
													break;
												case "color":
													fb |= FrameBufferType.Color;
													break;
												case "depth":
													fb |= FrameBufferType.Depth;
													break;
												case "stencil":
													fb |= FrameBufferType.Stencil;
													break;
												default:
													LogError( context, "When parsing pass clear buffers options, illegal option '{0}'", cb );
													break;
											}
										}
										break;
									case "colour":
										context.pass.ClearColor = ParseClearColor( context, args );
										break;
									case "color":
										context.pass.ClearColor = ParseClearColor( context, args );
										break;
									case "depth_value":
										if ( !OptionCount( context, token, 1, args.Length ) )
											break;
										context.pass.ClearDepth = ParseFloat( context, args[ 0 ] );
										break;
									case "stencil_value":
										if ( !OptionCount( context, token, 1, args.Length ) )
											break;
										context.pass.ClearDepth = ParseInt( context, args[ 0 ] );
										break;
									case "}":
										context.section = CompositorScriptSection.Pass;
										context.seenOpen = true;
										break;
									default:
										LogIllegal( context, "clear", token );
										break;
								}
								break;
							case CompositorScriptSection.Stencil:
								switch ( token )
								{
									case "check":
										context.pass.StencilCheck = OnOffArg( context, token, args );
										break;
									case "compare_func":
										if ( !OptionCount( context, token, 1, args.Length ) )
											break;
										context.pass.StencilFunc = ParseCompareFunc( context, arg );
										break;
									case "ref_value":
										if ( !OptionCount( context, token, 1, args.Length ) )
											break;
										context.pass.StencilRefValue = ParseInt( context, arg );
										break;
									case "mask":
										if ( !OptionCount( context, token, 1, args.Length ) )
											break;
										context.pass.StencilMask = ParseInt( context, arg );
										break;
									case "fail_op":
										if ( !OptionCount( context, token, 1, args.Length ) )
											break;
										context.pass.StencilFailOp = ParseStencilOperation( context, arg );
										break;
									case "depth_fail_op":
										if ( !OptionCount( context, token, 1, args.Length ) )
											break;
										context.pass.StencilDepthFailOp = ParseStencilOperation( context, arg );
										break;
									case "pass_op":
										if ( !OptionCount( context, token, 1, args.Length ) )
											break;
										context.pass.StencilPassOp = ParseStencilOperation( context, arg );
										break;
									case "two_sided":
										if ( !OptionCount( context, token, 1, args.Length ) )
											break;
										context.pass.StencilTwoSidedOperation = OnOffArg( context, token, args );
										break;
									case "}":
										context.section = CompositorScriptSection.Pass;
										context.seenOpen = true;
										break;
									default:
										LogIllegal( context, "stencil", token );
										break;
								}
								break;
							default:
								LogError( context, "Internal compositor parser error: illegal context" );
								break;
						}
					} // if
				} // if
			} // while
			if ( context.section != CompositorScriptSection.None )
				LogError( context, "At end of file, unterminated compositor script!" );
		}

		#region Script Parsing Routines

		static void LogError( CompositorScriptContext context, string error, params object[] substitutions )
		{
			StringBuilder errorBuilder = new StringBuilder();

			// log compositor name only if filename not specified
			if ( context.filename == null && context.compositor != null )
			{
				errorBuilder.Append( "Error in compositor " );
				errorBuilder.Append( context.compositor.Name );
				errorBuilder.Append( " : " );
				errorBuilder.AppendFormat( "At line # {0}: '{1}'", context.lineNo, context.line );
				errorBuilder.AppendFormat( error, substitutions );
			}
			else
			{
				if ( context.compositor != null )
				{
					errorBuilder.Append( "Error in compositor " );
					errorBuilder.Append( context.compositor.Name );
					errorBuilder.AppendFormat( " at line # {0}: '{1}'", context.lineNo, context.line );
					errorBuilder.AppendFormat( " of {0}: ", context.filename );
					errorBuilder.AppendFormat( error, substitutions );
				}
				else
				{
					errorBuilder.AppendFormat( "Error at line # {0}: '{1}'", context.lineNo, context.line );
					errorBuilder.AppendFormat( " of {0}: ", context.filename );
					errorBuilder.AppendFormat( error, substitutions );
				}
			}

			LogManager.Instance.Write( errorBuilder.ToString() );
		}

		static void LogIllegal( CompositorScriptContext context, string category, string token )
		{
			LogError( context, "Illegal {0} attribute '{1}'", category, token );
		}

		static string[] SplitByWhitespace( string line, int count )
		{
			return StringConverter.Split( line, new char[] { ' ', '\t' }, count );
		}

		static string[] SplitArgs( string args )
		{
			return args.Split( new char[] { ' ', '\t' } );
		}

		static string RemoveQuotes( string token )
		{
			if ( token.Length >= 2 && token[ 0 ] == '\"' )
				token = token.Substring( 1 );
			if ( token[ token.Length - 1 ] == '\"' )
				token = token.Substring( 0, token.Length - 1 );
			return token;
		}

		static bool OptionCount( CompositorScriptContext context, string introducer, int expectedCount, int count )
		{
			if ( expectedCount < count )
			{
				LogError( context, "The '{0}' phrase requires {1} arguments", introducer, expectedCount );
				return false;
			}
			else
				return true;
		}

		static bool OnOffArg( CompositorScriptContext context, string introducer, string[] args )
		{
			if ( OptionCount( context, introducer, 1, args.Length ) )
			{
				string arg = args[ 0 ];
				if ( arg == "on" )
					return true;
				else if ( arg == "off" )
					return false;
				else
				{
					LogError( context, "Illegal '{0}' arg '{1}'; should be 'on' or 'off'", introducer, arg );
				}
			}
			return false;
		}

		static int ParseInt( CompositorScriptContext context, string s )
		{
			string n = s.Trim();
			try
			{
				return int.Parse( n );
			}
			catch ( Exception e )
			{
				LogError( context, "Error converting string '{0}' to integer; error message is '{1}'",
						 n, e.Message );
				return 0;
			}
		}

		static uint ParseUint( CompositorScriptContext context, string s )
		{
			string n = s.Trim();
			try
			{
				return uint.Parse( n );
			}
			catch ( Exception e )
			{
				LogError( context, "Error converting string '{0}' to unsigned integer; error message is '{1}'",
						 n, e.Message );
				return 0;
			}
		}

		static float ParseFloat( CompositorScriptContext context, string s )
		{
			string n = s.Trim();
			try
			{
			    return float.Parse( n, CultureInfo.InvariantCulture );
			}
			catch ( Exception e )
			{
				LogError( context, "Error converting string '{0}' to float; error message is '{1}'",
						 n, e.Message );
				return 0.0f;
			}
		}

		static ColorEx ParseClearColor( CompositorScriptContext context, string[] args )
		{
			if ( args.Length != 4 )
			{
				LogError( context, "A color value must consist of 4 floating point numbers" );
				return ColorEx.Black;
			}
			else
			{
				float r = ParseFloat( context, args[ 0 ] );
				float g = ParseFloat( context, args[ 0 ] );
				float b = ParseFloat( context, args[ 0 ] );
				float a = ParseFloat( context, args[ 0 ] );

				return new ColorEx( a, r, g, b );
			}
		}

		static void ParseTextureLine( CompositorScriptContext context, string[] args )
		{
			int widthPos = 1, heightPos = 2, formatPos = 3;
			if ( args.Length == 4 || args.Length == 6 )
			{
				if ( args.Length == 6 )
				{
					heightPos += 1;
					formatPos += 2;
				}

				CompositionTechnique.TextureDefinition textureDef = context.technique.CreateTextureDefinition( args[ 0 ] );
				if ( args[ widthPos ] == "target_width" )
				{
					textureDef.Width = 0;
					textureDef.WidthFactor = 1.0f;
				}
				else if ( args[ widthPos ] == "target_width_scaled" )
				{
					textureDef.Width = 0;
					textureDef.WidthFactor = ParseFloat( context, args[ widthPos + 1 ] );
				}
				else
				{
					textureDef.Width = ParseInt( context, args[ widthPos ] );
					textureDef.WidthFactor = 1.0f;
				}

				if ( args[ heightPos ] == "target_height" )
				{
					textureDef.Height = 0;
					textureDef.HeightFactor = 1.0f;
				}
				else if ( args[ heightPos ] == "target_height_scaled" )
				{
					textureDef.Height = 0;
					textureDef.HeightFactor = ParseFloat( context, args[ heightPos + 1 ] );
				}
				else
				{
					textureDef.Height = ParseInt( context, args[ heightPos ] );
					textureDef.HeightFactor = 1.0f;
				}

				switch ( args[ formatPos ] )
				{
					case "PF_A8R8G8B8":
						textureDef.PixelFormats.Add( PixelFormat.A8R8G8B8 );
						break;
					case "PF_R8G8B8A8":
						textureDef.PixelFormats.Add( Axiom.Media.PixelFormat.R8G8B8A8 );
						break;
					case "PF_R8G8B8":
						textureDef.PixelFormats.Add( Axiom.Media.PixelFormat.R8G8B8 );
						break;
					case "PF_FLOAT16_RGBA":
						textureDef.PixelFormats.Add( Axiom.Media.PixelFormat.FLOAT16_RGBA );
						break;
					case "PF_FLOAT16_RGB":
						textureDef.PixelFormats.Add( Axiom.Media.PixelFormat.FLOAT16_RGB );
						break;
					case "PF_FLOAT32_RGBA":
						textureDef.PixelFormats.Add( Axiom.Media.PixelFormat.FLOAT32_RGBA );
						break;
					case "PF_FLOAT16_R":
						textureDef.PixelFormats.Add( Axiom.Media.PixelFormat.FLOAT16_R );
						break;
					case "PF_FLOAT32_R":
						textureDef.PixelFormats.Add( Axiom.Media.PixelFormat.FLOAT32_R );
						break;
					default:
						LogError( context, "Unsupported texture pixel format '{0}'", args[ formatPos ] );
						break;
				}
			}
		}

		static CompareFunction ParseCompareFunc( CompositorScriptContext context, string arg )
		{
			switch ( arg.Trim() )
			{
				case "always_fail":
					return CompareFunction.AlwaysFail;
				case "always_pass":
					return CompareFunction.AlwaysPass;
				case "less_equal":
					return CompareFunction.LessEqual;
				case "less'":
					return CompareFunction.Less;
				case "equal":
					return CompareFunction.Equal;
				case "not_equal":
					return CompareFunction.NotEqual;
				case "greater_equal":
					return CompareFunction.GreaterEqual;
				case "greater":
					return CompareFunction.Greater;
				default:
					LogError( context, "Illegal stencil compare_func '{0}'", arg );
					return CompareFunction.AlwaysPass;
			}
		}

		static StencilOperation ParseStencilOperation( CompositorScriptContext context, string arg )
		{
			switch ( arg.Trim() )
			{
				case "keep":
					return StencilOperation.Keep;
				case "zero":
					return StencilOperation.Zero;
				case "replace":
					return StencilOperation.Replace;
				case "increment_wrap":
					return StencilOperation.IncrementWrap;
				case "increment":
					return StencilOperation.Increment;
				case "decrement_wrap":
					return StencilOperation.DecrementWrap;
				case "decrement":
					return StencilOperation.Decrement;
				case "invert":
					return StencilOperation.Invert;
				default:
					LogError( context, "Illegal stencil_operation '{0}'", arg );
					return StencilOperation.Keep;
			}
		}

		#endregion Script Parsing Routines


	}
}