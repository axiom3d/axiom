#region LGPL License
/*
Axiom Graphics Engine Library
Copyright (C) 2003-2006 Axiom Project Team

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
//     <license see="http://axiomengine.sf.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;

using Axiom.Controllers;
using Axiom.Core;
using Axiom.Graphics;
using Axiom.Scripting;
using System.Collections.Generic;

#endregion  Namespace Declarations

namespace Axiom.Serialization
{
	/// <summary>
	/// Summary description for MaterialSerializer.
	/// </summary>
	public class MaterialSerializer
	{
		#region Fields


		public static Hashtable materialSourceFiles = new Hashtable();

		/// <summary>
		///		Represents the current parsing context.
		/// </summary>
		protected MaterialScriptContext scriptContext = new MaterialScriptContext();
		/// <summary>
		///		Parsers for the root of the material script
		/// </summary>
		protected Hashtable rootAttribParsers = new Hashtable();
		/// <summary>
		///		Parsers for the material section of a script.
		/// </summary>
		protected Hashtable materialAttribParsers = new Hashtable();
		/// <summary>
		///		Parsers for the technique section of a script.
		/// </summary>
		protected Hashtable techniqueAttribParsers = new Hashtable();
		/// <summary>
		///		Parsers for the pass section of a script.
		/// </summary>
		protected Hashtable passAttribParsers = new Hashtable();
		/// <summary>
		///		Parsers for the texture unit section of a script.
		/// </summary>
		protected Hashtable textureUnitAttribParsers = new Hashtable();
		/// <summary>
		///		Parsers for the program reference section of a script.
		/// </summary>
		protected Hashtable programRefAttribParsers = new Hashtable();
		/// <summary>
		///		Parsers for the program definition section of a script.
		/// </summary>
		protected Hashtable programAttribParsers = new Hashtable();
		/// <summary>
		///		Parsers for the program definition section of a script.
		/// </summary>
		protected Hashtable programDefaultParamAttribParsers = new Hashtable();

		#endregion Fields

		#region Delegates

		/// <summary>
		///		The method signature for all material attribute parsing methods.
		/// </summary>
		delegate bool MaterialAttributeParserHandler( string parameters, MaterialScriptContext context );

		#endregion Delegates

		#region Constructor

		/// <summary>
		///		Default constructor.
		/// </summary>
		public MaterialSerializer()
		{
			RegisterParserMethods();
		}

		#endregion Constructor

		#region Helper Methods

		/// <summary>
		///		Internal method for finding & invoking an attribute parser.
		/// </summary>
		/// <param name="line"></param>
		/// <param name="parsers"></param>
		/// <returns></returns>
		protected bool InvokeParser( string line, Hashtable parsers )
		{
            string[] splitCmd = StringConverter.Split( line, new char[] { ' ', '\t' }, 2 );

			// find attribute parser
			if ( parsers.ContainsKey( splitCmd[ 0 ] ) )
			{
				string cmd = string.Empty;

				if ( splitCmd.Length >= 2 )
				{
					cmd = splitCmd[ 1 ];
				}

				MaterialAttributeParserHandler handler = (MaterialAttributeParserHandler)parsers[ splitCmd[ 0 ] ];

				// Use parser, make sure we have 2 params before using splitCmd[1]
				// MONO: Does not like mangling the above and below lines into a single line (frankly, i don't blame it, but csc takes it).
				// i.e. (((MaterialAttributeParserHandler)parsers[splitCmd[0]]))(cmd, scriptContext);
				return handler( cmd, scriptContext );
			}
			else
			{
				// BAD command, BAD!!
				LogParseError( scriptContext, "Unrecognized command: {0}", splitCmd[ 0 ] );
				return false;
			}
		}

		/// <summary>
		///		Internal method for saving a program definition which has been built up.
		/// </summary>
		protected void FinishProgramDefinition()
		{
			MaterialScriptProgramDefinition def = scriptContext.programDef;
			GpuProgram gp = null;

			if ( def.language == "asm" )
			{
				// native assembler
				// validate
				if ( def.source == string.Empty )
				{
					LogParseError( scriptContext, "Invalid program definition for {0}, you must specify a source file.", def.name );
				}
				if ( def.syntax == string.Empty )
				{
					LogParseError( scriptContext, "Invalid program definition for {0}, you must specify a syntax code.", def.name );
				}

				// create
				gp = GpuProgramManager.Instance.CreateProgram( def.name, def.source, def.progType, def.syntax );
			}
			else
			{
				// high level program
				// validate
				if ( def.source == string.Empty )
				{
					LogParseError( scriptContext, "Invalid program definition for {0}, you must specify a source file.", def.name );
				}
				// create
				try
				{
					HighLevelGpuProgram hgp = HighLevelGpuProgramManager.Instance.CreateProgram( def.name, def.language, def.progType );
					gp = hgp;
					// set source file
					hgp.SourceFile = def.source;

					// set custom parameters
					foreach ( KeyValuePair<string,string> entry in def.customParameters )
					{
						string param = entry.Key;
						string val = entry.Value;

						if ( !hgp.SetParam( param, val ) )
						{
							LogParseError( scriptContext, "Error in program {0} parameter {1} is not valid.", def.name, param );
						}
					}
				}
				catch ( Exception ex )
				{
					LogManager.Instance.Write( "Could not create GPU program '{0}'. error reported was: {1}.", def.name, ex.Message );
					return;
				}
			}
			if ( gp == null )
			{
				LogManager.Instance.Write( string.Format( "Failed to create {0} {1} GPU program named '{2}' using syntax {3}.  This is likely due to your hardware not supporting advanced high-level shaders.",
					def.language, def.progType, def.name, def.syntax ) );
				return;
			}

			// set skeletal animation option
			gp.IsSkeletalAnimationIncluded = def.supportsSkeletalAnimation;
			gp.IsMorphAnimationIncluded = def.supportsMorphAnimation;
			gp.PoseAnimationCount = def.poseAnimationCount;

			// set up to receive default parameters
			if ( gp.IsSupported && scriptContext.defaultParamLines.Count > 0 )
			{
				scriptContext.programParams = gp.DefaultParameters;
				scriptContext.program = gp;

				for ( int i = 0; i < scriptContext.defaultParamLines.Count; i++ )
				{
					// find & invoke a parser
					// do this manually because we want to call a custom
					// routine when the parser is not found
					// First, split line on first divisor only
					string[] splitCmd = StringConverter.Split( scriptContext.defaultParamLines[ i ], new char[] { ' ', '\t' }, 2 );

					// find attribute parser
					if ( programDefaultParamAttribParsers.ContainsKey( splitCmd[ 0 ] ) )
					{
						string cmd = splitCmd.Length >= 2 ? splitCmd[ 1 ] : string.Empty;

						MaterialAttributeParserHandler handler = (MaterialAttributeParserHandler)programDefaultParamAttribParsers[ splitCmd[ 0 ] ];

						// Use parser, make sure we have 2 params before using splitCmd[1]
						handler( cmd, scriptContext );
					}
				}

				// reset
				scriptContext.program = null;
				scriptContext.programParams = null;
			}
		}

		/// <summary>
		///		Helper method for logging parser errors.
		/// </summary>
		/// <param name="context">Current parsing context.</param>
		/// <param name="error">Error message.</param>
		/// <param name="substitutions">Items to sub in for the error message, if the error contains them.</param>
		protected static void LogParseError( MaterialScriptContext context, string error, params object[] substitutions )
		{
			StringBuilder errorBuilder = new StringBuilder();

			// log material name only if filename not specified
			if ( context.filename == null && context.material != null )
			{
				errorBuilder.Append( "Error in material " );
				errorBuilder.Append( context.material.Name );
				errorBuilder.Append( " : " );
				errorBuilder.AppendFormat( error, substitutions );
			}
			else
			{
				if ( context.material != null )
				{
					errorBuilder.Append( "Error in material " );
					errorBuilder.Append( context.material.Name );
					errorBuilder.AppendFormat( " at line {0} ", context.lineNo );
					errorBuilder.AppendFormat( " of {0}: ", context.filename );
					errorBuilder.AppendFormat( error, substitutions );
				}
				else
				{
					errorBuilder.AppendFormat( "Error at line {0} ", context.lineNo );
					errorBuilder.AppendFormat( " of {0}: ", context.filename );
					errorBuilder.AppendFormat( error, substitutions );
				}
			}

			LogManager.Instance.Write( errorBuilder.ToString() );
		}

		/// <summary>
		///		Internal method for parsing a material.
		/// </summary>
		/// <param name="line"></param>
		/// <returns>True if it expects the next line to be a "{", false otherwise.</returns>
		protected bool ParseScriptLine( string line )
		{
			switch ( scriptContext.section )
			{
				case MaterialScriptSection.None:
					if ( line == "}" )
					{
						LogParseError( scriptContext, "Unexpected terminating brace." );
						return false;
					}
					else
					{
						// find and invoke a parser
						return InvokeParser( line, rootAttribParsers );
					}

				case MaterialScriptSection.Material:
					if ( line == "}" )
					{
						// end of material
						scriptContext.section = MaterialScriptSection.None;
						scriptContext.material = null;
						// reset all levels for the next material
						scriptContext.passLev = -1;
						scriptContext.stateLev = -1;
						scriptContext.techLev = -1;
					}
					else
					{
						// find and invoke parser
						return InvokeParser( line, materialAttribParsers );
					}
					break;

				case MaterialScriptSection.Technique:
					if ( line == "}" )
					{
						// end of technique
						scriptContext.section = MaterialScriptSection.Material;
						scriptContext.technique = null;
						scriptContext.passLev = -1;
					}
					else
					{
						// find and invoke parser
						return InvokeParser( line, techniqueAttribParsers );
					}
					break;

				case MaterialScriptSection.Pass:
					if ( line == "}" )
					{
						// end of pass
						scriptContext.section = MaterialScriptSection.Technique;
						scriptContext.pass = null;
						scriptContext.stateLev = -1;
					}
					else
					{
						// find and invoke parser
						return InvokeParser( line, passAttribParsers );
					}
					break;

				case MaterialScriptSection.TextureUnit:
					if ( line == "}" )
					{
						// end of texture unit
						scriptContext.section = MaterialScriptSection.Pass;
						scriptContext.textureUnit = null;
					}
					else
					{
						// find and invoke parser
						return InvokeParser( line, textureUnitAttribParsers );
					}
					break;

				case MaterialScriptSection.TextureSource:
					// TODO: Implement
					LogParseError( scriptContext, "Texture Source sections are not yet supported!" );
					break;

				case MaterialScriptSection.ProgramRef:
					if ( line == "}" )
					{
						// end of program
						scriptContext.section = MaterialScriptSection.Pass;
						scriptContext.program = null;
					}
					else
					{
						// find and invoke a parser
						return InvokeParser( line, programRefAttribParsers );
					}
					break;

				case MaterialScriptSection.Program:
					// Program definitions are slightly different, they are deferred
					// until all the information required is known
					if ( line == "}" )
					{
						// end of program
						FinishProgramDefinition();
						scriptContext.section = MaterialScriptSection.None;
						scriptContext.defaultParamLines.Clear();
						scriptContext.programDef = null;
					}
					else
					{
						// find & invoke a parser
						// do this manually because we want to call a custom
						// routine when the parser is not found
						// First, split line on first divisor only
                        string[] splitCmd = StringConverter.Split( line, new char[] { ' ', '\t' }, 2 );

						// find attribute parser
						if ( programAttribParsers.ContainsKey( splitCmd[ 0 ] ) )
						{
							// Use parser, make sure we have 2 params before using splitCmd[1]
							string cmd = splitCmd.Length >= 2 ? splitCmd[ 1 ] : string.Empty;

							MaterialAttributeParserHandler handler = (MaterialAttributeParserHandler)programAttribParsers[ splitCmd[ 0 ] ];

							return handler( cmd, scriptContext );
						}
						else
						{
							// custom parameter, use original line
							ParseProgramCustomParameter( line, scriptContext );
						}
					}
					break;

				case MaterialScriptSection.DefaultParameters:
					if ( line == "}" )
					{
						// End of default parameters
						scriptContext.section = MaterialScriptSection.Program;
					}
					else
					{
						// Save default parameter lines up until we finalise the program
						scriptContext.defaultParamLines.Add( line );
					}
					break;
			}

			return false;
		}

		/// <summary>
		///		Queries this serializer class for methods intended to parse material script attributes.
		/// </summary>
		protected void RegisterParserMethods()
		{
			MethodInfo[] methods = this.GetType().GetMethods( BindingFlags.NonPublic | BindingFlags.Static );

			// loop through all methods and look for ones marked with attributes
			for ( int i = 0; i < methods.Length; i++ )
			{
				// get the current method in the loop
				MethodInfo method = methods[ i ];

				// see if the method should be used to parse one or more material attributes
				MaterialAttributeParserAttribute[] parserAtts =
					(MaterialAttributeParserAttribute[])method.GetCustomAttributes( typeof( MaterialAttributeParserAttribute ), true );

				// loop through each one we found and register its parser
				for ( int j = 0; j < parserAtts.Length; j++ )
				{
					MaterialAttributeParserAttribute parserAtt = parserAtts[ j ];

					Hashtable parserList = null;

					// determine which parser list we will add this handler to
					switch ( parserAtt.Section )
					{
						case MaterialScriptSection.None:
							parserList = rootAttribParsers;
							break;

						case MaterialScriptSection.Material:
							parserList = materialAttribParsers;
							break;

						case MaterialScriptSection.Technique:
							parserList = techniqueAttribParsers;
							break;

						case MaterialScriptSection.Pass:
							parserList = passAttribParsers;
							break;

						case MaterialScriptSection.ProgramRef:
							parserList = programRefAttribParsers;
							break;

						case MaterialScriptSection.Program:
							parserList = programAttribParsers;
							break;

						case MaterialScriptSection.TextureUnit:
							parserList = textureUnitAttribParsers;
							break;

						case MaterialScriptSection.DefaultParameters:
							parserList = programDefaultParamAttribParsers;
							break;

						default:
							parserList = null;
							break;
					} // switch

					if ( parserList != null )
					{
						parserList.Add( parserAtt.Name, Delegate.CreateDelegate( typeof( MaterialAttributeParserHandler ), method ) );
					}
				} // for
			} // for
		}

		#endregion Helper Methods

		#region Parser Methods

		/// <summary>
		///		Parse custom GPU program parameters.
		/// </summary>
		/// <remarks>
		///		This one is called explicitly, and is not added to any parser list.
		/// </remarks>
		protected static bool ParseProgramCustomParameter( string parameters, MaterialScriptContext context )
		{
			// This params object does not have the command stripped
			// Lower case the command, but not the value incase it's relevant
			// Split only up to first delimiter, program deals with the rest
            string[] values = StringConverter.Split( line, new char[] { ' ', '\t' }, 2 );

			if ( values.Length != 2 )
			{
				LogParseError( context, "Invalid custom program parameter entry; there must be a parameter name and at least one value." );
				return false;
			}

			context.programDef.customParameters.Add( new KeyValuePair<string, string>( values[ 0 ], values[ 1 ] ) );

			return false;
		}


		[MaterialAttributeParser( "material", MaterialScriptSection.None )]
		protected static bool ParseMaterial( string parameters, MaterialScriptContext context )
		{

			// create a brand new material
			string materialName = parameters;
			string sourceFileForAlreadyExistingMaterial = (string)materialSourceFiles[ materialName ];
			if ( sourceFileForAlreadyExistingMaterial != null )
			{//if a material by this name was already created
				throw new ArgumentException( string.Format( "A material with name {0} was already created from material script file {1} and a duplicate from {2} cannot be created. "
					+ "You may need to qualify the material names to prevent this name collision", materialName, sourceFileForAlreadyExistingMaterial, context.filename ) );
			}
			context.material = (Material)MaterialManager.Instance.Create( materialName );
			materialSourceFiles.Add( materialName, context.filename );


			// remove pre-created technique from defaults
			context.material.RemoveAllTechniques();

			// update section
			context.section = MaterialScriptSection.Material;

			// return true because this must be followed by a {
			return true;
		}

		[MaterialAttributeParser( "vertex_program", MaterialScriptSection.None )]
		protected static bool ParseVertexProgram( string parameters, MaterialScriptContext context )
		{
			// update section
			context.section = MaterialScriptSection.Program;

			// create new program definition-in-progress
			context.programDef = new MaterialScriptProgramDefinition();
			context.programDef.progType = GpuProgramType.Vertex;
			context.programDef.supportsSkeletalAnimation = false;
			context.programDef.supportsMorphAnimation = false;
			context.programDef.poseAnimationCount = 0;

			// get name and language code
			string[] values = parameters.Split( new char[] { ' ', '\t' } );

			if ( values.Length != 2 )
			{
				LogParseError( context, "Invalid vertex_program entry - expected 2 parameters." );
				return true;
			}

			// name, preserve case
			context.programDef.name = values[ 0 ];
			// language code, make lower case
			context.programDef.language = values[ 1 ].ToLower();

			// return true, because this must be followed by a {
			return true;
		}

		[MaterialAttributeParser( "fragment_program", MaterialScriptSection.None )]
		protected static bool ParseFragmentProgram( string parameters, MaterialScriptContext context )
		{
			// update section
			context.section = MaterialScriptSection.Program;

			// create new program definition-in-progress
			context.programDef = new MaterialScriptProgramDefinition();
			context.programDef.progType = GpuProgramType.Fragment;
			context.programDef.supportsSkeletalAnimation = false;
			context.programDef.supportsMorphAnimation = false;
			context.programDef.poseAnimationCount = 0;

			// get name and language code
			string[] values = parameters.Split( new char[] { ' ', '\t' } );

			if ( values.Length != 2 )
			{
				LogParseError( context, "Invalid fragment_program entry - expected 2 parameters." );
				return true;
			}

			// name, preserve case
			context.programDef.name = values[ 0 ];
			// language code, make lower case
			context.programDef.language = values[ 1 ].ToLower();

			// return true, because this must be followed by a {
			return true;
		}

		[MaterialAttributeParser( "technique", MaterialScriptSection.Material )]
		protected static bool ParseTechnique( string parameters, MaterialScriptContext context )
		{
			// create a new technique
			context.technique = context.material.CreateTechnique();

			// update section
			context.section = MaterialScriptSection.Technique;

			// increate technique level depth
			context.techLev++;

			// get the technique name
			string[] values = parameters.Split( new char[] { ' ', '\t' } );

			if ( values.Length > 0 && values[ 0 ].Length > 0 )
				context.technique.Name = values[ 0 ];

			// return true because this must be followed by a {
			return true;
		}

		[MaterialAttributeParser( "pass", MaterialScriptSection.Technique )]
		protected static bool ParsePass( string parameters, MaterialScriptContext context )
		{
			// get the pass name
			string[] values = parameters.Split( new char[] { ' ', '\t' } );

			// if params is not empty then see if the pass name already exists
			if ( values.Length > 0 && values[ 0 ].Length > 0 && context.technique.NumPasses > 0 )
			{
				// find the pass with name = params
				Pass foundPass = context.technique.GetPass( values[ 0 ] );
				if ( foundPass != null )
					context.passLev = foundPass.Index;
				else
					// name was not found so a new pass is needed
					// position pass level to the end index
					// a new pass will be created later on
					context.passLev = context.technique.NumPasses;
			}
			else
			{
				// increase the pass level depth;
				context.passLev++;
			}

			if ( context.technique.NumPasses > context.passLev )
			{
				context.pass = context.technique.GetPass( context.passLev );
			}
			else
			{
				// create a new pass
				context.pass = context.technique.CreatePass();
				if ( values.Length > 0 && values[ 0 ].Length > 0 )
					context.pass.Name = values[ 0 ];
			}

			// update section
			context.section = MaterialScriptSection.Pass;

			// return true because this must be followed by a {
			return true;
		}

		[MaterialAttributeParser( "texture_unit", MaterialScriptSection.Pass )]
		protected static bool ParseTextureUnit( string parameters, MaterialScriptContext context )
		{
			// create a new texture unit
			context.textureUnit = context.pass.CreateTextureUnitState();

			// get the texture unit name
			string[] values = parameters.Split( new char[] { ' ', '\t' } );
			if ( values.Length > 0 && values[ 0 ].Length > 0 )
				context.textureUnit.Name = values[ 0 ];

			// update section
			context.section = MaterialScriptSection.TextureUnit;

			// increase texture unit level depth
			context.stateLev++;

			// return true because this must be followed by a {
			return true;
		}

		[MaterialAttributeParser( "texture_alias", MaterialScriptSection.Pass )]
		protected static bool ParseTextureAlias( string parameters, MaterialScriptContext context )
		{
			Debug.Assert( context.textureUnit != null );

			// get the texture alias
			string[] values = parameters.Split( new char[] { ' ', '\t' } );

			if ( values.Length != 1 )
			{
				LogParseError( context, "Invalid texture_alias entry - expected 1 parameter." );
				return true;
			}
			// update section
			context.textureUnit.TextureNameAlias = values[ 0 ];

			return false;
		}

		#region Material

		[MaterialAttributeParser( "lod_distances", MaterialScriptSection.Material )]
		protected static bool ParseLodDistances( string parameters, MaterialScriptContext context )
		{
			string[] values = parameters.Split( new char[] { ' ', '\t' } );

			FloatList lodDistances = new FloatList();

			for ( int i = 0; i < values.Length; i++ )
			{
				lodDistances.Add( StringConverter.ParseFloat( values[ i ] ) );
			}

			context.material.SetLodLevels( lodDistances );

			return false;
		}

		[MaterialAttributeParser( "receive_shadows", MaterialScriptSection.Material )]
		protected static bool ParseReceiveShadows( string parameters, MaterialScriptContext context )
		{
			if ( parameters != "on" && parameters != "off" )
			{
				LogParseError( context, "Bad receive_shadows attribute, valid parameters are 'on' or 'off'." );
			}

			context.material.ReceiveShadows = StringConverter.ParseBool( parameters );

			return false;
		}

		[MaterialAttributeParser( "transparency_casts_shadows", MaterialScriptSection.Material )]
		protected static bool ParseTransparencyCastsShadows( string parameters, MaterialScriptContext context )
		{
			if ( parameters != "on" && parameters != "off" )
			{
				LogParseError( context, "Bad transparency_casts_shadows attribute, valid parameters are 'on' or 'off'." );
			}

			context.material.TransparencyCastsShadows = StringConverter.ParseBool( parameters );

			return false;
		}

		#endregion Material

		#region Technique

		[MaterialAttributeParser( "lod_index", MaterialScriptSection.Technique )]
		protected static bool ParseLodIndex( string parameters, MaterialScriptContext context )
		{
			context.technique.LodIndex = int.Parse( parameters );

			return false;
		}

		#endregion Technique

		#region Pass

		[MaterialAttributeParser( "ambient", MaterialScriptSection.Pass )]
		protected static bool ParseAmbient( string parameters, MaterialScriptContext context )
		{
			string[] values = parameters.Split( new char[] { ' ', '\t' } );

			// must be 3 or 4 parameters
			if ( values.Length != 3 && values.Length != 4 )
			{
				LogParseError( context, "Bad ambient attribute, wrong number of parameters (expected 3 or 4)" );
			}
			else
			{
				context.pass.Ambient = StringConverter.ParseColor( values );
			}

			return false;
		}

		[MaterialAttributeParser( "colour_write", MaterialScriptSection.Pass )]
		[MaterialAttributeParser( "color_write", MaterialScriptSection.Pass )]
		protected static bool ParseColorWrite( string parameters, MaterialScriptContext context )
		{
			switch ( parameters.ToLower() )
			{
				case "on":
					context.pass.ColorWrite = true;
					break;
				case "off":
					context.pass.ColorWrite = false;
					break;
				default:
					LogParseError( context, "Bad color_write attribute, valid parameters are 'on' or 'off'." );
					break;
			}

			return false;
		}

		[MaterialAttributeParser( "cull_hardware", MaterialScriptSection.Pass )]
		protected static bool ParseCullHardware( string parameters, MaterialScriptContext context )
		{
			// lookup the real enum equivalent to the script value
			object val = ScriptEnumAttribute.Lookup( parameters, typeof( CullingMode ) );

			// if a value was found, assign it
			if ( val != null )
			{
				context.pass.CullMode = (CullingMode)val;
			}
			else
			{
				string legalValues = ScriptEnumAttribute.GetLegalValues( typeof( CullingMode ) );
				LogParseError( context, "Bad cull_hardware attribute, valid parameters are {0}.", legalValues );
			}

			return false;
		}

		[MaterialAttributeParser( "cull_software", MaterialScriptSection.Pass )]
		protected static bool ParseCullSoftware( string parameters, MaterialScriptContext context )
		{
			// lookup the real enum equivalent to the script value
			object val = ScriptEnumAttribute.Lookup( parameters, typeof( ManualCullingMode ) );

			// if a value was found, assign it
			if ( val != null )
			{
				context.pass.ManualCullMode = (ManualCullingMode)val;
			}
			else
			{
				string legalValues = ScriptEnumAttribute.GetLegalValues( typeof( ManualCullingMode ) );
				LogParseError( context, "Bad cull_software attribute, valid parameters are {0}.", legalValues );
			}

			return false;
		}

		[MaterialAttributeParser( "depth_bias", MaterialScriptSection.Pass )]
		protected static bool ParseDepthBias( string parameters, MaterialScriptContext context )
		{
			context.pass.DepthBias = int.Parse( parameters );

			return false;
		}

		[MaterialAttributeParser( "depth_check", MaterialScriptSection.Pass )]
		protected static bool ParseDepthCheck( string parameters, MaterialScriptContext context )
		{
			switch ( parameters.ToLower() )
			{
				case "on":
					context.pass.DepthCheck = true;
					break;
				case "off":
					context.pass.DepthCheck = false;
					break;
				default:
					LogParseError( context, "Bad depth_check attribute, valid parameters are 'on' or 'off'." );
					break;
			}

			return false;
		}

		[MaterialAttributeParser( "depth_func", MaterialScriptSection.Pass )]
		protected static bool ParseDepthFunc( string parameters, MaterialScriptContext context )
		{
			// lookup the real enum equivalent to the script value
			object val = ScriptEnumAttribute.Lookup( parameters, typeof( CompareFunction ) );

			// if a value was found, assign it
			if ( val != null )
			{
				context.pass.DepthFunction = (CompareFunction)val;
			}
			else
			{
				string legalValues = ScriptEnumAttribute.GetLegalValues( typeof( CompareFunction ) );
				LogParseError( context, "Bad depth_func attribute, valid parameters are {0}.", legalValues );
			}

			return false;
		}

		[MaterialAttributeParser( "depth_write", MaterialScriptSection.Pass )]
		protected static bool ParseDepthWrite( string parameters, MaterialScriptContext context )
		{
			switch ( parameters.ToLower() )
			{
				case "on":
					context.pass.DepthWrite = true;
					break;
				case "off":
					context.pass.DepthWrite = false;
					break;
				default:
					LogParseError( context, "Bad depth_write attribute, valid parameters are 'on' or 'off'." );
					break;
			}

			return false;
		}

		[MaterialAttributeParser( "diffuse", MaterialScriptSection.Pass )]
		protected static bool ParseDiffuse( string parameters, MaterialScriptContext context )
		{
			string[] values = parameters.Split( new char[] { ' ', '\t' } );

			if ( values.Length != 3 && values.Length != 4 )
			{
				LogParseError( context, "Bad diffuse attribute, wrong numbe of parameters (expected 3 or 4)." );
			}
			else
			{
				context.pass.Diffuse = StringConverter.ParseColor( values );
			}

			return false;
		}

		[MaterialAttributeParser( "emissive", MaterialScriptSection.Pass )]
		protected static bool ParseEmissive( string parameters, MaterialScriptContext context )
		{
			string[] values = parameters.Split( new char[] { ' ', '\t' } );

			if ( values.Length != 3 && values.Length != 4 )
			{
				LogParseError( context, "Bad emissive attribute, wrong number of parameters (expected 4)." );
			}
			else
			{
				context.pass.Emissive = StringConverter.ParseColor( values );
			}

			return false;
		}

		[MaterialAttributeParser( "fog_override", MaterialScriptSection.Pass )]
		protected static bool ParseFogOverride( string parameters, MaterialScriptContext context )
		{
			string[] values = parameters.ToLower().Split( new char[] { ' ', '\t' } );

			if ( values[ 0 ] == "true" )
			{
				// if true, we need to see if they supplied all arguments, or just the 1... if just the one,
				// Assume they want to disable the default fog from effecting this material.
				if ( values.Length == 8 )
				{
					// lookup the real enum equivalent to the script value
					object val = ScriptEnumAttribute.Lookup( values[ 1 ], typeof( FogMode ) );

					// if a value was found, assign it
					if ( val != null )
					{
						FogMode mode = (FogMode)val;

						context.pass.SetFog(
							true,
							mode,
							new ColorEx( StringConverter.ParseFloat( values[ 2 ] ), StringConverter.ParseFloat( values[ 3 ] ), StringConverter.ParseFloat( values[ 4 ] ) ),
							StringConverter.ParseFloat( values[ 5 ] ),
							StringConverter.ParseFloat( values[ 6 ] ),
							StringConverter.ParseFloat( values[ 7 ] ) );
					}
					else
					{
						string legalValues = ScriptEnumAttribute.GetLegalValues( typeof( FogMode ) );
						LogParseError( context, "Bad fogging attribute, valid parameters are {0}.", legalValues );
					}
				}
				else
				{
					context.pass.SetFog( true );
				}
			}
			else if ( values[ 0 ] == "false" )
			{
				context.pass.SetFog( false );
			}
			else
			{
				LogParseError( context, "Bad fog_override attribute, valid parameters are 'true' and 'false'." );
			}

			return false;
		}

		[MaterialAttributeParser( "iteration", MaterialScriptSection.Pass )]
		protected static bool ParseIteration( string parameters, MaterialScriptContext context )
		{
			string[] values = parameters.Split( new char[] { ' ', '\t' } );

			if ( values.Length < 1 || values.Length > 2 )
			{
				LogParseError( context, "Bad iteration attribute, wrong number of parameters (expected 1 or 2)." );
				return false;
			}

			if ( values[ 0 ] == "once" )
			{
				context.pass.SetRunOncePerLight( false );
			}
			else if ( values[ 0 ] == "once_per_light" )
			{
				if ( values.Length == 2 )
				{
					// parse light type

					// lookup the real enum equivalent to the script value
					object val = ScriptEnumAttribute.Lookup( values[ 1 ], typeof( LightType ) );

					// if a value was found, assign it
					if ( val != null )
					{
						context.pass.SetRunOncePerLight( true, true, (LightType)val );
					}
					else
					{
						string legalValues = ScriptEnumAttribute.GetLegalValues( typeof( LightType ) );
						LogParseError( context, "Bad iteration attribute, valid values are {0}", legalValues );
					}
				}
				else
				{
					context.pass.SetRunOncePerLight( true, false );
				}
			}
			else
			{
				LogParseError( context, "Bad iteration attribute, valid valies are 'once' and 'once_per_light'." );
			}

			return false;
		}

		[MaterialAttributeParser( "lighting", MaterialScriptSection.Pass )]
		protected static bool ParseLighting( string parameters, MaterialScriptContext context )
		{
			switch ( parameters.ToLower() )
			{
				case "on":
					context.pass.LightingEnabled = true;
					break;
				case "off":
					context.pass.LightingEnabled = false;
					break;
				default:
					LogParseError( context, "Bad lighting attribute, must be 'on' or 'off'" );
					break;
			}

			return false;
		}

		[MaterialAttributeParser( "max_lights", MaterialScriptSection.Pass )]
		protected static bool ParseMaxLights( string parameters, MaterialScriptContext context )
		{
			context.pass.MaxLights = int.Parse( parameters );
			return false;
		}

		[MaterialAttributeParser( "scene_blend", MaterialScriptSection.Pass )]
		protected static bool ParseSceneBlend( string parameters, MaterialScriptContext context )
		{
			string[] values = parameters.ToLower().Split( new char[] { ' ', '\t' } );

			switch ( values.Length )
			{
				case 1:
					// e.g. scene_blend add 
					// lookup the real enum equivalent to the script value 
					object val = ScriptEnumAttribute.Lookup( values[ 0 ], typeof( SceneBlendType ) );

					// if a value was found, assign it 
					if ( val != null )
					{
						context.pass.SetSceneBlending( (SceneBlendType)val );
					}
					else
					{
						string legalValues = ScriptEnumAttribute.GetLegalValues( typeof( SceneBlendType ) );
						LogParseError( context, "Bad scene_blend attribute, valid values for the 2nd parameter are {0}.", legalValues );
						return false;
					}
					break;
				case 2:
					// e.g. scene_blend source_alpha one_minus_source_alpha  
					// lookup the real enums equivalent to the script values 
					object srcVal = ScriptEnumAttribute.Lookup( values[ 0 ], typeof( SceneBlendFactor ) );
					object destVal = ScriptEnumAttribute.Lookup( values[ 1 ], typeof( SceneBlendFactor ) );

					// if both values were found, assign them 
					if ( srcVal != null && destVal != null )
					{
						context.pass.SetSceneBlending( (SceneBlendFactor)srcVal, (SceneBlendFactor)destVal );
					}
					else
					{
						string legalValues = ScriptEnumAttribute.GetLegalValues( typeof( SceneBlendFactor ) );

						if ( srcVal == null )
						{
							LogParseError( context, "Bad scene_blend attribute, valid src blend factor values are {0}.", legalValues );
						}
						if ( destVal == null )
						{
							LogParseError( context, "Bad scene_blend attribute, valid dest blend factor values are {0}.", legalValues );
						}
					}
					break;
				default:
					context.pass.SetSceneBlending( SceneBlendFactor.Zero, SceneBlendFactor.Zero );
					LogParseError( context, "Bad scene_blend attribute, wrong number of parameters (expected 1 or 2)." );
					break;
			}

			return false;
		}

		[MaterialAttributeParser( "shading", MaterialScriptSection.Pass )]
		protected static bool ParseShading( string parameters, MaterialScriptContext context )
		{
			// lookup the real enum equivalent to the script value
			object val = ScriptEnumAttribute.Lookup( parameters, typeof( Shading ) );

			// if a value was found, assign it
			if ( val != null )
			{
				context.pass.ShadingMode = (Shading)val;
			}
			else
			{
				string legalValues = ScriptEnumAttribute.GetLegalValues( typeof( Shading ) );
				LogParseError( context, "Bad shading attribute, valid parameters are {0}.", legalValues );
			}

			return false;
		}

		[MaterialAttributeParser( "specular", MaterialScriptSection.Pass )]
		protected static bool ParseSpecular( string parameters, MaterialScriptContext context )
		{
			string[] values = parameters.Split( new char[] { ' ', '\t' } );

			if ( values.Length != 4 && values.Length != 5 )
			{
				LogParseError( context, "Bad emissive attribute, wrong number of parameters (expected 4 or 5)." );
			}
			else
			{
				context.pass.Specular = StringConverter.ParseColor( values );

				if ( values.Length == 5 )
				{
					context.pass.Shininess = StringConverter.ParseFloat( values[ 4 ] );
				}
			}

			return false;
		}

		[MaterialAttributeParser( "vertex_program_ref", MaterialScriptSection.Pass )]
		protected static bool ParseVertexProgramRef( string parameters, MaterialScriptContext context )
		{
			// update section
			context.section = MaterialScriptSection.ProgramRef;

			context.program = GpuProgramManager.Instance.GetByName( parameters );

			if ( context.program == null )
			{
				// unknown program
				LogParseError( context, "Invalid vertex_program_ref entry - vertex program {0} has not been defined.", parameters );
				return true;
			}

			context.isProgramShadowCaster = false;
			context.isProgramShadowReceiver = false;

			// set the vertex program for this pass
			context.pass.SetVertexProgram( parameters );

			// create params?  skip this if program is not supported
			if ( context.program.IsSupported )
			{
				context.programParams = context.pass.VertexProgramParameters;
			}

			// Return TRUE because this must be followed by a {
			return true;
		}

		[MaterialAttributeParser( "shadow_caster_vertex_program_ref", MaterialScriptSection.Pass )]
		protected static bool ParseShadowCasterVertexProgramRef( string parameters, MaterialScriptContext context )
		{
			// update section
			context.section = MaterialScriptSection.ProgramRef;

			context.program = GpuProgramManager.Instance.GetByName( parameters );

			if ( context.program == null )
			{
				// unknown program
				LogParseError( context, "Invalid shadow_caster_vertex_program_ref entry - vertex program {0} has not been defined.", parameters );
				return true;
			}

			context.isProgramShadowCaster = true;
			context.isProgramShadowReceiver = false;

			// set the vertex program for this pass
			context.pass.SetShadowCasterVertexProgram( parameters );

			// create params?  skip this if program is not supported
			if ( context.program.IsSupported )
			{
				context.programParams = context.pass.ShadowCasterVertexProgramParameters;
			}

			// Return TRUE because this must be followed by a {
			return true;
		}

		[MaterialAttributeParser( "shadow_receiver_vertex_program_ref", MaterialScriptSection.Pass )]
		protected static bool ParseShadowReceiverVertexProgramRef( string parameters, MaterialScriptContext context )
		{
			// update section
			context.section = MaterialScriptSection.ProgramRef;

			context.program = GpuProgramManager.Instance.GetByName( parameters );

			if ( context.program == null )
			{
				// unknown program
				LogParseError( context, "Invalid shadow_receiver_vertex_program_ref entry - vertex program {0} has not been defined.", parameters );
				return true;
			}

			context.isProgramShadowCaster = false;
			context.isProgramShadowReceiver = true;

			// set the vertex program for this pass
			context.pass.SetShadowReceiverVertexProgram( parameters );

			// create params?  skip this if program is not supported
			if ( context.program.IsSupported )
			{
				context.programParams = context.pass.ShadowReceiverVertexProgramParameters;
			}

			// Return TRUE because this must be followed by a {
			return true;
		}

		[MaterialAttributeParser( "fragment_program_ref", MaterialScriptSection.Pass )]
		protected static bool ParseFragmentProgramRef( string parameters, MaterialScriptContext context )
		{
			// update section
			context.section = MaterialScriptSection.ProgramRef;

			context.program = GpuProgramManager.Instance.GetByName( parameters );

			if ( context.program == null )
			{
				// unknown program
				LogParseError( context, "Invalid fragment_program_ref entry - fragment program {0} has not been defined.", parameters );
				return true;
			}

			context.isProgramShadowCaster = false;
			context.isProgramShadowReceiver = false;

			// set the vertex program for this pass
			context.pass.SetFragmentProgram( parameters );

			// create params?  skip this if program is not supported
			if ( context.program.IsSupported )
			{
				context.programParams = context.pass.FragmentProgramParameters;
			}

			// Return TRUE because this must be followed by a {
			return true;
		}

		[MaterialAttributeParser( "shadow_caster_fragment_program_ref", MaterialScriptSection.Pass )]
		protected static bool ParseShadowCasterFragmentProgramRef( string parameters, MaterialScriptContext context )
		{
			// update section
			context.section = MaterialScriptSection.ProgramRef;

			context.program = GpuProgramManager.Instance.GetByName( parameters );

			if ( context.program == null )
			{
				// unknown program
				LogParseError( context, "Invalid shadow_caster_fragment_program_ref entry - fragment program {0} has not been defined.", parameters );
				return true;
			}

			context.isProgramShadowCaster = true;
			context.isProgramShadowReceiver = false;

			// set the vertex program for this pass
			context.pass.SetShadowCasterFragmentProgram( parameters );

			// create params?  skip this if program is not supported
			if ( context.program.IsSupported )
			{
				context.programParams = context.pass.ShadowCasterFragmentProgramParameters;
			}

			// Return TRUE because this must be followed by a {
			return true;
		}

		[MaterialAttributeParser( "shadow_receiver_fragment_program_ref", MaterialScriptSection.Pass )]
		protected static bool ParseShadowReceiverFragmentProgramRef( string parameters, MaterialScriptContext context )
		{
			// update section
			context.section = MaterialScriptSection.ProgramRef;

			context.program = GpuProgramManager.Instance.GetByName( parameters );

			if ( context.program == null )
			{
				// unknown program
				LogParseError( context, "Invalid shadow_receiver_fragment_program_ref entry - fragment program {0} has not been defined.", parameters );
				return true;
			}

			context.isProgramShadowCaster = false;
			context.isProgramShadowReceiver = true;

			// set the vertex program for this pass
			context.pass.SetShadowReceiverFragmentProgram( parameters );

			// create params?  skip this if program is not supported
			if ( context.program.IsSupported )
			{
				context.programParams = context.pass.ShadowReceiverFragmentProgramParameters;
			}

			// Return TRUE because this must be followed by a {
			return true;
		}

		#endregion Pass

		#region Texture Unit

		[MaterialAttributeParser( "alpha_op_ex", MaterialScriptSection.TextureUnit )]
		protected static bool ParseAlphaOpEx( string parameters, MaterialScriptContext context )
		{
			string[] values = parameters.ToLower().Split( new char[] { ' ', '\t' } );

			if ( values.Length < 3 || values.Length > 6 )
			{
				LogParseError( context, "Bad alpha_op_ex attribute, wrong number of parameters (expected 3 or 6)." );
				return false;
			}

			LayerBlendOperationEx op = 0;
			LayerBlendSource src1 = 0;
			LayerBlendSource src2 = 0;
			float manual = 0.0f;
			float arg1 = 1.0f, arg2 = 1.0f;

			try
			{
				op = (LayerBlendOperationEx)ScriptEnumAttribute.Lookup( values[ 0 ], typeof( LayerBlendOperationEx ) );
				src1 = (LayerBlendSource)ScriptEnumAttribute.Lookup( values[ 1 ], typeof( LayerBlendSource ) );
				src2 = (LayerBlendSource)ScriptEnumAttribute.Lookup( values[ 2 ], typeof( LayerBlendSource ) );

				if ( op == LayerBlendOperationEx.BlendManual )
				{
					if ( values.Length < 4 )
					{
						LogParseError( context, "Bad alpha_op_ex attribute, wrong number of parameters (expected 4 for manual blend)." );
						return false;
					}

					manual = int.Parse( values[ 3 ] );
				}

				if ( src1 == LayerBlendSource.Manual )
				{
					int paramIndex = 3;
					if ( op == LayerBlendOperationEx.BlendManual )
					{
						paramIndex++;
					}

					if ( values.Length < paramIndex )
					{
						LogParseError( context, "Bad alpha_op_ex attribute, wrong number of parameters (expected {0}).", paramIndex - 1 );
						return false;
					}

					arg1 = StringConverter.ParseFloat( values[ paramIndex ] );
				}

				if ( src2 == LayerBlendSource.Manual )
				{
					int paramIndex = 3;

					if ( op == LayerBlendOperationEx.BlendManual )
					{
						paramIndex++;
					}
					if ( src1 == LayerBlendSource.Manual )
					{
						paramIndex++;
					}

					if ( values.Length < paramIndex )
					{
						LogParseError( context, "Bad alpha_op_ex attribute, wrong number of parameters (expected {0}).", paramIndex - 1 );
						return false;
					}

					arg2 = StringConverter.ParseFloat( values[ paramIndex ] );
				}
			}
			catch ( Exception ex )
			{
				LogParseError( context, "Bad alpha_op_ex attribute, {0}.", ex.Message );
				return false;
			}

			context.textureUnit.SetAlphaOperation( op, src1, src2, arg1, arg2, manual );

			return false;
		}

		[MaterialAttributeParser( "alpha_rejection", MaterialScriptSection.TextureUnit )]
		protected static bool ParseAlphaRejection( string parameters, MaterialScriptContext context )
		{
			string[] values = parameters.Split( new char[] { ' ', '\t' } );

			if ( values.Length != 2 )
			{
				LogParseError( context, "Bad alpha_rejection attribute, wrong number of parameters (expected 2)." );
				return false;
			}

			// lookup the real enum equivalent to the script value
			object val = ScriptEnumAttribute.Lookup( values[ 0 ], typeof( CompareFunction ) );

			// if a value was found, assign it
			if ( val != null )
			{
				context.textureUnit.SetAlphaRejectSettings( (CompareFunction)val, byte.Parse( values[ 1 ] ) );
			}
			else
			{
				string legalValues = ScriptEnumAttribute.GetLegalValues( typeof( CompareFunction ) );
				LogParseError( context, "Bad alpha_rejection attribute, valid parameters are {0}.", legalValues );
			}

			return false;
		}

		[MaterialAttributeParser( "anim_texture", MaterialScriptSection.TextureUnit )]
		protected static bool ParseAnimTexture( string parameters, MaterialScriptContext context )
		{
			string[] values = parameters.Split( new char[] { ' ', '\t' } );

			if ( values.Length < 3 )
			{
				LogParseError( context, "Bad anim_texture attribute, wrong number of parameters (excepted at least 3)." );
				return false;
			}

			if ( values.Length == 3 && int.Parse( values[ 1 ] ) != 0 )
			{
				// first form using the base name and number of frames
				context.textureUnit.SetAnimatedTextureName( values[ 0 ], int.Parse( values[ 1 ] ), StringConverter.ParseFloat( values[ 2 ] ) );
			}
			else
			{
				// second form using individual names
				context.textureUnit.SetAnimatedTextureName( values, values.Length - 1, StringConverter.ParseFloat( values[ values.Length - 1 ] ) );
			}

			return false;
		}

		[MaterialAttributeParser( "max_anisotropy", MaterialScriptSection.TextureUnit )]
		protected static bool ParseAnisotropy( string parameters, MaterialScriptContext context )
		{
			context.textureUnit.TextureAnisotropy = int.Parse( parameters );

			return false;
		}

		/// Note: Allows both spellings of color :-).
		[MaterialAttributeParser( "color_op", MaterialScriptSection.TextureUnit )]
		[MaterialAttributeParser( "colour_op", MaterialScriptSection.TextureUnit )]
		protected static bool ParseColorOp( string parameters, MaterialScriptContext context )
		{
			// lookup the real enum equivalent to the script value
			object val = ScriptEnumAttribute.Lookup( parameters, typeof( LayerBlendOperation ) );

			// if a value was found, assign it
			if ( val != null )
			{
				context.textureUnit.SetColorOperation( (LayerBlendOperation)val );
			}
			else
			{
				string legalValues = ScriptEnumAttribute.GetLegalValues( typeof( LayerBlendOperation ) );
				LogParseError( context, "Bad color_op attribute, valid values are {0}.", legalValues );
			}

			return false;
		}

		/// Note: Allows both spellings of color :-).
		[MaterialAttributeParser( "colour_op_multipass_fallback", MaterialScriptSection.TextureUnit )]
		[MaterialAttributeParser( "color_op_multipass_fallback", MaterialScriptSection.TextureUnit )]
		protected static bool ParseColorOpFallback( string parameters, MaterialScriptContext context )
		{
			string[] values = parameters.Split( new char[] { ' ', '\t' } );

			// lookup the real enums equivalent to the script values 
			object srcVal = ScriptEnumAttribute.Lookup( values[ 0 ], typeof( SceneBlendFactor ) );
			object destVal = ScriptEnumAttribute.Lookup( values[ 1 ], typeof( SceneBlendFactor ) );

			// if both values were found, assign them 
			if ( srcVal != null && destVal != null )
			{
				context.textureUnit.SetColorOpMultipassFallback( (SceneBlendFactor)srcVal, (SceneBlendFactor)destVal );
			}
			else
			{
				string legalValues = ScriptEnumAttribute.GetLegalValues( typeof( SceneBlendFactor ) );

				if ( srcVal == null )
				{
					LogParseError( context, "Bad color_op_multipass_fallback attribute, valid values for src value are {0}.", legalValues );
				}
				if ( destVal == null )
				{
					LogParseError( context, "Bad color_op_multipass_fallback attribute, valid values for dest value are {0}.", legalValues );
				}
			}

			return false;
		}

		/// Note: Allows both spellings of color :-).
		[MaterialAttributeParser( "color_op_ex", MaterialScriptSection.TextureUnit )]
		[MaterialAttributeParser( "colour_op_ex", MaterialScriptSection.TextureUnit )]
		protected static bool ParseColorOpEx( string parameters, MaterialScriptContext context )
		{
			string[] values = parameters.ToLower().Split( new char[] { ' ', '\t' } );

			if ( values.Length < 3 || values.Length > 10 )
			{
				LogParseError( context, "Bad color_op_ex attribute, wrong number of parameters (expected 3 or 10)." );
				return false;
			}

			LayerBlendOperationEx op = 0;
			LayerBlendSource src1 = 0;
			LayerBlendSource src2 = 0;
			float manual = 0.0f;
			ColorEx colSrc1 = ColorEx.White;
			ColorEx colSrc2 = ColorEx.White;

			try
			{
				op = (LayerBlendOperationEx)ScriptEnumAttribute.Lookup( values[ 0 ], typeof( LayerBlendOperationEx ) );
				src1 = (LayerBlendSource)ScriptEnumAttribute.Lookup( values[ 1 ], typeof( LayerBlendSource ) );
				src2 = (LayerBlendSource)ScriptEnumAttribute.Lookup( values[ 2 ], typeof( LayerBlendSource ) );

				if ( op == LayerBlendOperationEx.BlendManual )
				{
					if ( values.Length < 4 )
					{
						LogParseError( context, "Bad color_op_ex attribute, wrong number of parameters (expected 4 params for manual blending)." );
						return false;
					}

					manual = int.Parse( values[ 3 ] );
				}

				if ( src1 == LayerBlendSource.Manual )
				{
					int paramIndex = 3;
					if ( op == LayerBlendOperationEx.BlendManual )
					{
						paramIndex++;
					}

					if ( values.Length < paramIndex + 3 )
					{
						LogParseError( context, "Bad color_op_ex attribute, wrong number of parameters (expected {0}).", paramIndex + 3 );
						return false;
					}

					colSrc1.r = StringConverter.ParseFloat( values[ paramIndex++ ] );
					colSrc1.g = StringConverter.ParseFloat( values[ paramIndex++ ] );
					colSrc1.b = StringConverter.ParseFloat( values[ paramIndex ] );

					if ( values.Length > paramIndex )
					{
						colSrc1.a = StringConverter.ParseFloat( values[ paramIndex ] );
					}
					else
					{
						colSrc1.a = 1.0f;
					}
				}

				if ( src2 == LayerBlendSource.Manual )
				{
					int paramIndex = 3;

					if ( op == LayerBlendOperationEx.BlendManual )
					{
						paramIndex++;
					}

					if ( values.Length < paramIndex + 3 )
					{
						LogParseError( context, "Bad color_op_ex attribute, wrong number of parameters (expected {0}).", paramIndex + 3 );
						return false;
					}

					colSrc2.r = StringConverter.ParseFloat( values[ paramIndex++ ] );
					colSrc2.g = StringConverter.ParseFloat( values[ paramIndex++ ] );
					colSrc2.b = StringConverter.ParseFloat( values[ paramIndex++ ] );

					if ( values.Length > paramIndex )
					{
						colSrc2.a = StringConverter.ParseFloat( values[ paramIndex ] );
					}
					else
					{
						colSrc2.a = 1.0f;
					}
				}
			}
			catch ( Exception ex )
			{
				LogParseError( context, "Bad color_op_ex attribute, {0}.", ex.Message );
				return false;
			}

			context.textureUnit.SetColorOperationEx( op, src1, src2, colSrc1, colSrc2, manual );

			return false;
		}

		[MaterialAttributeParser( "cubic_texture", MaterialScriptSection.TextureUnit )]
		protected static bool ParseCubicTexture( string parameters, MaterialScriptContext context )
		{
			string[] values = parameters.Split( new char[] { ' ', '\t' } );

			bool useUVW;
			string uvw = values[ values.Length - 1 ].ToLower();

			switch ( uvw )
			{
				case "combineduvw":
					useUVW = true;
					break;
				case "separateuv":
					useUVW = false;
					break;
				default:
					LogParseError( context, "Bad cubic_texture attribute, last param must be 'combinedUVW' or 'separateUV'." );
					return false;
			}

			// use base name to infer the 6 texture names
			if ( values.Length == 2 )
			{
				context.textureUnit.SetCubicTextureName( values[ 0 ], useUVW );
			}
			else if ( values.Length == 7 )
			{
				// copy the array elements for the 6 tex names
				string[] names = new string[ 6 ];
				Array.Copy( values, 0, names, 0, 6 );

				context.textureUnit.SetCubicTextureName( names, useUVW );
			}
			else
			{
				LogParseError( context, "Bad cubic_texture attribute, wrong number of parameters (expected 2 or 7)." );
			}

			return false;
		}

		[MaterialAttributeParser( "env_map", MaterialScriptSection.TextureUnit )]
		protected static bool ParseEnvMap( string parameters, MaterialScriptContext context )
		{
			if ( parameters == "off" )
			{
				context.textureUnit.SetEnvironmentMap( false );
			}
			else
			{
				// lookup the real enum equivalent to the script value
				object val = ScriptEnumAttribute.Lookup( parameters, typeof( EnvironmentMap ) );

				// if a value was found, assign it
				if ( val != null )
				{
					context.textureUnit.SetEnvironmentMap( true, (EnvironmentMap)val );
				}
				else
				{
					string legalValues = ScriptEnumAttribute.GetLegalValues( typeof( EnvironmentMap ) );
					LogParseError( context, "Bad env_map attribute, valid values are {0}.", legalValues );
				}
			}

			return false;
		}

		[MaterialAttributeParser( "filtering", MaterialScriptSection.TextureUnit )]
		protected static bool ParseFiltering( string parameters, MaterialScriptContext context )
		{
			string[] values = parameters.ToLower().Split( new char[] { ' ', '\t' } );

			if ( values.Length == 1 )
			{
				// lookup the real enum equivalent to the script value
				object val = ScriptEnumAttribute.Lookup( values[ 0 ], typeof( TextureFiltering ) );

				// if a value was found, assign it
				if ( val != null )
				{
					context.textureUnit.SetTextureFiltering( (TextureFiltering)val );
				}
				else
				{
					string legalValues = ScriptEnumAttribute.GetLegalValues( typeof( TextureFiltering ) );
					LogParseError( context, "Bad filtering attribute, valid filtering values are {0}.", legalValues );
					return false;
				}
			}
			else if ( values.Length == 3 )
			{
				// complex format
				object val1 = ScriptEnumAttribute.Lookup( values[ 0 ], typeof( FilterOptions ) );
				object val2 = ScriptEnumAttribute.Lookup( values[ 1 ], typeof( FilterOptions ) );
				object val3 = ScriptEnumAttribute.Lookup( values[ 2 ], typeof( FilterOptions ) );

				if ( val1 == null || val2 == null || val3 == null )
				{
					string legalValues = ScriptEnumAttribute.GetLegalValues( typeof( FilterOptions ) );
					LogParseError( context, "Bad filtering attribute, valid values for filter options are {0}", legalValues );
					return false;
				}
				else
				{
					context.textureUnit.SetTextureFiltering( (FilterOptions)val1, (FilterOptions)val2, (FilterOptions)val3 );
				}
			}
			else
			{
				LogParseError( context, "Bad filtering attribute, wrong number of paramters (expected 1 or 3)." );
			}

			return false;
		}

		[MaterialAttributeParser( "rotate", MaterialScriptSection.TextureUnit )]
		protected static bool ParseRotate( string parameters, MaterialScriptContext context )
		{
			context.textureUnit.SetTextureRotate( StringConverter.ParseFloat( parameters ) );
			return false;
		}

		[MaterialAttributeParser( "rotate_anim", MaterialScriptSection.TextureUnit )]
		protected static bool ParseRotateAnim( string parameters, MaterialScriptContext context )
		{
			context.textureUnit.SetRotateAnimation( StringConverter.ParseFloat( parameters ) );
			return false;
		}

		[MaterialAttributeParser( "scale", MaterialScriptSection.TextureUnit )]
		protected static bool ParseScale( string parameters, MaterialScriptContext context )
		{
			string[] values = parameters.Split( new char[] { ' ', '\t' } );

			if ( values.Length != 2 )
			{
				LogParseError( context, "Bad scale attribute, wrong number of parameters (expected 2)." );
			}
			else
			{
				context.textureUnit.SetTextureScale( StringConverter.ParseFloat( values[ 0 ] ), StringConverter.ParseFloat( values[ 1 ] ) );
			}

			return false;
		}

		[MaterialAttributeParser( "scroll", MaterialScriptSection.TextureUnit )]
		protected static bool ParseScroll( string parameters, MaterialScriptContext context )
		{
			string[] values = parameters.Split( new char[] { ' ', '\t' } );

			if ( values.Length != 2 )
			{
				LogParseError( context, "Bad scroll attribute, wrong number of parameters (expected 2)." );
			}
			else
			{
				context.textureUnit.SetTextureScroll( StringConverter.ParseFloat( values[ 0 ] ), StringConverter.ParseFloat( values[ 1 ] ) );
			}

			return false;
		}

		[MaterialAttributeParser( "scroll_anim", MaterialScriptSection.TextureUnit )]
		protected static bool ParseScrollAnim( string parameters, MaterialScriptContext context )
		{
			string[] values = parameters.Split( new char[] { ' ', '\t' } );

			if ( values.Length != 2 )
			{
				LogParseError( context, "Bad scroll_anim attribute, wrong number of parameters (expected 2)." );
			}
			else
			{
				context.textureUnit.SetScrollAnimation( StringConverter.ParseFloat( values[ 0 ] ), StringConverter.ParseFloat( values[ 1 ] ) );
			}

			return false;
		}

		[MaterialAttributeParser( "tex_address_mode", MaterialScriptSection.TextureUnit )]
		protected static bool ParseTexAddressMode( string parameters, MaterialScriptContext context )
		{
			// lookup the real enum equivalent to the script value
			object val = ScriptEnumAttribute.Lookup( parameters, typeof( TextureAddressing ) );

			// if a value was found, assign it
			if ( val != null )
			{
				context.textureUnit.TextureAddressing = (TextureAddressing)val;
			}
			else
			{
				string legalValues = ScriptEnumAttribute.GetLegalValues( typeof( TextureAddressing ) );
				LogParseError( context, "Bad tex_address_mode attribute, valid values are {0}", legalValues );
			}

			return false;
		}

		[MaterialAttributeParser( "tex_border_colour", MaterialScriptSection.TextureUnit )]
		[MaterialAttributeParser( "tex_border_color", MaterialScriptSection.TextureUnit )]
		protected static bool ParseTexBorderColor( string parameters, MaterialScriptContext context )
		{
			string[] values = parameters.Split( new char[] { ' ', '\t' } );

			if ( values.Length != 3 && values.Length != 4 )
			{
				LogParseError( context, "Bad tex_border_color attribute, wrong number of parameters (expected 3 or 4)." );
			}
			else
			{
				context.textureUnit.TextureBorderColor = StringConverter.ParseColor( values );
			}

			return false;
		}



		[MaterialAttributeParser( "tex_coord_set", MaterialScriptSection.TextureUnit )]
		protected static bool ParseTexCoordSet( string parameters, MaterialScriptContext context )
		{
			context.textureUnit.TextureCoordSet = int.Parse( parameters );

			return false;
		}

		[MaterialAttributeParser( "texture", MaterialScriptSection.TextureUnit )]
		protected static bool ParseTexture( string parameters, MaterialScriptContext context )
		{
			string[] values = parameters.Split( new char[] { ' ', '\t' } );

			if ( values.Length < 1 || values.Length > 2 )
			{
				LogParseError( context, "Bad texture attribute, wrong number of parameters (expected 1 or 2)." );
				return false;
			}

			// use 2d as default if anything goes wrong
			TextureType texType = TextureType.TwoD;

			if ( values.Length == 2 )
			{
				// check the transform type
				object val = ScriptEnumAttribute.Lookup( values[ 1 ], typeof( TextureType ) );

				if ( val == null )
				{
					string legalValues = ScriptEnumAttribute.GetLegalValues( typeof( TextureType ) );
					LogParseError( context, "Bad texture attribute, valid texture type values are {0}", legalValues );
				}
				else
				{
					texType = (TextureType)val;
				}
			}

			context.textureUnit.SetTextureName( values[ 0 ], texType );

			return false;
		}

		[MaterialAttributeParser( "wave_xform", MaterialScriptSection.TextureUnit )]
		protected static bool ParseWaveXForm( string parameters, MaterialScriptContext context )
		{
			string[] values = parameters.Split( new char[] { ' ', '\t' } );

			if ( values.Length != 6 )
			{
				LogParseError( context, "Bad wave_xform attribute, wrong number of parameters (expected 6)." );
				return false;
			}

			TextureTransform transType = 0;
			WaveformType waveType = 0;

			// check the transform type
			object val = ScriptEnumAttribute.Lookup( values[ 0 ], typeof( TextureTransform ) );

			if ( val == null )
			{
				string legalValues = ScriptEnumAttribute.GetLegalValues( typeof( TextureTransform ) );
				LogParseError( context, "Bad wave_xform attribute, valid transform type values are {0}.", legalValues );
				return false;
			}

			transType = (TextureTransform)val;

			// check the wavetype
			val = ScriptEnumAttribute.Lookup( values[ 1 ], typeof( WaveformType ) );

			if ( val == null )
			{
				string legalValues = ScriptEnumAttribute.GetLegalValues( typeof( WaveformType ) );
				LogParseError( context, "Bad wave_xform attribute, valid waveform type values are {0}.", legalValues );
				return false;

			}

			waveType = (WaveformType)val;

			// set the transform animation
			context.textureUnit.SetTransformAnimation(
				transType,
				waveType,
				StringConverter.ParseFloat( values[ 2 ] ),
				StringConverter.ParseFloat( values[ 3 ] ),
				StringConverter.ParseFloat( values[ 4 ] ),
				StringConverter.ParseFloat( values[ 5 ] ) );

			return false;
		}

		#endregion Texture Unit

		#region Program Ref/Default Program Ref

		[MaterialAttributeParser( "param_indexed", MaterialScriptSection.ProgramRef )]
		[MaterialAttributeParser( "param_indexed", MaterialScriptSection.DefaultParameters )]
		protected static bool ParseParamIndexed( string parameters, MaterialScriptContext context )
		{
			// skip this if the program is not supported or could not be found
			if ( context.program == null || !context.program.IsSupported )
			{
				return false;
			}

			string[] values = parameters.Split( new char[] { ' ', '\t' } );

			if ( values.Length < 3 )
			{
				LogParseError( context, "Invalid param_indexed attribute - expected at least 3 parameters." );
				return false;
			}

			// get start index
			int index = int.Parse( values[ 0 ] );

			ProcessManualProgramParam( index, "param_indexed", values, context );

			return false;
		}

		[MaterialAttributeParser( "param_indexed_auto", MaterialScriptSection.ProgramRef )]
		[MaterialAttributeParser( "param_indexed_auto", MaterialScriptSection.DefaultParameters )]
		protected static bool ParseParamIndexedAuto( string parameters, MaterialScriptContext context )
		{
			// skip this if the program is not supported or could not be found
			if ( context.program == null || !context.program.IsSupported )
			{
				return false;
			}

			string[] values = parameters.Split( new char[] { ' ', '\t' } );

			if ( values.Length != 2 && values.Length != 3 )
			{
				LogParseError( context, "Invalid param_indexed_auto attribute - expected at 2 or 3 parameters." );
				return false;
			}

			// get start index
			int index = int.Parse( values[ 0 ] );

			ProcessAutoProgramParam( index, "param_indexed_auto", values, context );

			return false;
		}

		[MaterialAttributeParser( "param_named", MaterialScriptSection.ProgramRef )]
		[MaterialAttributeParser( "param_named", MaterialScriptSection.DefaultParameters )]
		protected static bool ParseParamNamed( string parameters, MaterialScriptContext context )
		{
			// skip this if the program is not supported or could not be found
			if ( context.program == null || !context.program.IsSupported )
			{
				return false;
			}

			string[] values = parameters.Split( new char[] { ' ', '\t' } );

			if ( values.Length < 3 )
			{
				LogParseError( context, "Invalid param_named attribute - expected at least 3 parameters." );
				return false;
			}

			// get start index
			try
			{
				int index = context.programParams.GetParamIndex( values[ 0 ] );

				ProcessManualProgramParam( index, "param_named", values, context );
			}
			catch ( Exception ex )
			{
				LogParseError( context, "Invalid param_named attribute - {0}.", ex.Message );
				return false;
			}

			return false;
		}

		[MaterialAttributeParser( "param_named_auto", MaterialScriptSection.ProgramRef )]
		[MaterialAttributeParser( "param_named_auto", MaterialScriptSection.DefaultParameters )]
		protected static bool ParseParamNamedAuto( string parameters, MaterialScriptContext context )
		{
			// skip this if the program is not supported or could not be found
			if ( context.program == null || !context.program.IsSupported )
			{
				return false;
			}

			string[] values = parameters.Split( new char[] { ' ', '\t' } );

			if ( values.Length != 2 && values.Length != 3 )
			{
				LogParseError( context, "Invalid param_named_auto attribute - expected 2 or 3 parameters." );
				return false;
			}

			// get start index
			try
			{
				int index = context.programParams.GetParamIndex( values[ 0 ] );

				ProcessAutoProgramParam( index, "param_named_auto", values, context );
			}
			catch ( Exception ex )
			{
				LogParseError( context, "Invalid param_named_auto attribute - {0}.", ex.Message );
				return false;
			}

			return false;
		}

		#endregion Program Ref/Default Program Ref

		#region Program Definition

		[MaterialAttributeParser( "source", MaterialScriptSection.Program )]
		protected static bool ParseProgramSource( string parameters, MaterialScriptContext context )
		{
			// source filename, preserve case
			context.programDef.source = parameters;

			return false;
		}

		[MaterialAttributeParser( "syntax", MaterialScriptSection.Program )]
		protected static bool ParseProgramSyntax( string parameters, MaterialScriptContext context )
		{
			context.programDef.syntax = parameters.ToLower();

			return false;
		}

		[MaterialAttributeParser( "includes_skeletal_animation", MaterialScriptSection.Program )]
		protected static bool ParseProgramSkeletalAnimation( string parameters, MaterialScriptContext context )
		{
			context.programDef.supportsSkeletalAnimation = bool.Parse( parameters );

			return false;
		}

		[MaterialAttributeParser( "includes_morph_animation", MaterialScriptSection.Program )]
		protected static bool ParseProgramMorphAnimation( string parameters, MaterialScriptContext context )
		{
			context.programDef.supportsMorphAnimation = bool.Parse( parameters );

			return false;
		}

		[MaterialAttributeParser( "includes_pose_animation", MaterialScriptSection.Program )]
		protected static bool ParseProgramPoseAnimation( string parameters, MaterialScriptContext context )
		{
			context.programDef.poseAnimationCount = ushort.Parse( parameters );

			return false;
		}

		[MaterialAttributeParser( "default_params", MaterialScriptSection.Program )]
		protected static bool ParseDefaultParams( string parameters, MaterialScriptContext context )
		{
			context.section = MaterialScriptSection.DefaultParameters;

			// should be a brace next
			return true;
		}

		#endregion Program Definition

		#endregion Parser Methods

		#region Public Methods

		/// <summary>
		///		Parses a Material script file passed in the specified stream.
		/// </summary>
		/// <param name="data">Stream containing the material file data.</param></param>
		/// <param name="fileName">Name of the material file, which will only be used in logging.</param>
		public void ParseScript( Stream stream, string fileName )
		{
			StreamReader script = new StreamReader( stream, System.Text.Encoding.ASCII );

			string line = string.Empty;
			bool nextIsOpenBrace = false;

			scriptContext.section = MaterialScriptSection.None;
			scriptContext.material = null;
			scriptContext.technique = null;
			scriptContext.pass = null;
			scriptContext.textureUnit = null;
			scriptContext.program = null;
			scriptContext.lineNo = 0;
			scriptContext.techLev = -1;
			scriptContext.passLev = -1;
			scriptContext.stateLev = -1;
			scriptContext.filename = fileName;

			// parse through the data to the end
			while ( ( line = ParseHelper.ReadLine( script ) ) != null )
			{
				scriptContext.lineNo++;

				// ignore blank lines and comments
				if ( line.Length == 0 || line.StartsWith( "//" ) )
				{
					continue;
				}

				if ( nextIsOpenBrace )
				{
					if ( line != "{" )
					{
						// MONO: Couldn't put a literal "{" in the format string
						LogParseError( scriptContext, "Expecting '{0}' but got {1} instead", "{", line );
					}

					nextIsOpenBrace = false;
				}
				else
				{
					nextIsOpenBrace = ParseScriptLine( line );
				}
			} // while

			if ( scriptContext.section != MaterialScriptSection.None )
			{
				LogParseError( scriptContext, "Unexpected end of file." );
			}
		}
		#endregion


		#region Static Methods
		/// <summary>
		///		
		/// </summary>
		/// <param name="index"></param>
		/// <param name="commandName"></param>
		/// <param name="parameters"></param>
		/// <param name="context"></param>
		protected static void ProcessManualProgramParam( int index, string commandName, string[] parameters, MaterialScriptContext context )
		{
			// NB we assume that the first element of vecparams is taken up with either 
			// the index or the parameter name, which we ignore

			int dims, roundedDims;
			bool isFloat = false;
			string type = parameters[ 1 ].ToLower();

			if ( type == "matrix4x4" )
			{
				dims = 16;
				isFloat = true;
			}
			else if ( type.IndexOf( "float" ) != -1 )
			{
				if ( type == "float" )
				{
					dims = 1;
				}
				else
				{
					// the first 5 letters are "float", get the dim indicator at the end
					// this handles entries like 'float4'
					dims = int.Parse( type.Substring( 5 ) );
				}

				isFloat = true;
			}
			else if ( type.IndexOf( "int" ) != -1 )
			{
				if ( type == "int" )
				{
					dims = 1;
				}
				else
				{
					// the first 5 letters are "int", get the dim indicator at the end
					dims = int.Parse( type.Substring( 3 ) );
				}
			}
			else
			{
				LogParseError( context, "Invalid {0} attribute - unrecognized parameter type {1}.", commandName, type );
				return;
			}

			// make sure we have enough params for this type's size
			if ( parameters.Length != 2 + dims )
			{
				LogParseError( context, "Invalid {0} attribute - you need {1} parameters for a parameter of type {2}", commandName, 2 + dims, type );
				return;
			}

			// Round dims to multiple of 4
			if ( dims % 4 != 0 )
			{
				roundedDims = dims + 4 - ( dims % 4 );
			}
			else
			{
				roundedDims = dims;
			}

			int i = 0;

			// now parse all the values
			if ( isFloat )
			{
				float[] buffer = new float[ roundedDims ];

				// do specified values
				for ( i = 0; i < dims; i++ )
				{
					buffer[ i ] = StringConverter.ParseFloat( parameters[ i + 2 ] );
				}

				// fill up to multiple of 4 with zero
				for ( ; i < roundedDims; i++ )
				{
					buffer[ i ] = 0.0f;
				}

				context.programParams.SetConstant( index, buffer );
			}
			else
			{
				int[] buffer = new int[ roundedDims ];

				// do specified values
				for ( i = 0; i < dims; i++ )
				{
					buffer[ i ] = int.Parse( parameters[ i + 2 ] );
				}

				// fill up to multiple of 4 with zero
				for ( ; i < roundedDims; i++ )
				{
					buffer[ i ] = 0;
				}

				context.programParams.SetConstant( index, buffer );
			}
		}

		/// <summary>
		///		
		/// </summary>
		/// <param name="index"></param>
		/// <param name="commandName"></param>
		/// <param name="parameters"></param>
		/// <param name="context"></param>
		protected static void ProcessAutoProgramParam( int index, string commandName, string[] parameters, MaterialScriptContext context )
		{
			bool extras = false;

			object val = ScriptEnumAttribute.Lookup( parameters[ 1 ], typeof( AutoConstants ) );

			if ( val != null )
			{
				bool isFloat = false;
				AutoConstants constantType = (AutoConstants)val;

				// these types require extra data
				if ( constantType == AutoConstants.LightDiffuseColor ||
					constantType == AutoConstants.LightSpecularColor ||
					constantType == AutoConstants.LightAttenuation ||
					constantType == AutoConstants.LightPosition ||
					constantType == AutoConstants.LightDirection ||
					constantType == AutoConstants.LightPositionObjectSpace ||
					constantType == AutoConstants.LightDirectionObjectSpace ||
					constantType == AutoConstants.Custom )
				{

					extras = true;
					isFloat = false;
				}
				else if ( constantType == AutoConstants.Time_0_X ||
						   constantType == AutoConstants.SinTime_0_X )
				{
					extras = true;
					isFloat = true;
				}


				// do we require extra data for this parameter?
				if ( extras )
				{
					if ( parameters.Length != 3 )
					{
						LogParseError( context, "Invalid {0} attribute - Expected 3 parameters.", commandName );
						return;
					}
				}
				if ( isFloat && extras )
					context.programParams.SetAutoConstant( index, constantType, float.Parse( parameters[ 2 ] ) );
				else if ( extras )
					context.programParams.SetAutoConstant( index, constantType, int.Parse( parameters[ 2 ] ) );
				else if ( constantType == AutoConstants.Time )
				{
					if ( parameters.Length == 3 )
						context.programParams.SetAutoConstant( index, constantType, float.Parse( parameters[ 2 ] ) );
					else
						context.programParams.SetAutoConstant( index, constantType, 1.0f );
				}
				else
					context.programParams.SetAutoConstant( index, constantType, 0 );
			}
			else
			{
				string legalValues = ScriptEnumAttribute.GetLegalValues( typeof( AutoConstants ) );
				LogParseError( context, "Bad auto gpu program param - Invalid param type '{0}', valid values are {1}.", parameters[ 1 ], legalValues );
				return;
			}
		}

		#endregion
	}


	#region Utility Types
	/// <summary>
	///		Enum to identify material sections.
	/// </summary>
	public enum MaterialScriptSection
	{
		None,
		Material,
		Technique,
		Pass,
		TextureUnit,
		ProgramRef,
		Program,
		DefaultParameters,
		TextureSource
	}

	public class MaterialScriptProgramDefinition
	{
		public string name;
		public GpuProgramType progType;
		public string language;
		public string source;
		public string syntax;
		public bool supportsSkeletalAnimation;
		public bool supportsMorphAnimation;
		public ushort poseAnimationCount;
		public List< KeyValuePair<string, string>> customParameters = new List<KeyValuePair<string,string>>();
	}

	/// <summary>
	///		Struct for holding the script context while parsing.
	/// </summary>
	public class MaterialScriptContext
	{
		public MaterialScriptSection section;
		public Material material;
		public Technique technique;
		public Pass pass;
		public TextureUnitState textureUnit;
		public GpuProgram program; // used when referencing a program, not when defining it
		public bool isProgramShadowCaster; // when referencing, are we in context of shadow caster
		public bool isProgramShadowReceiver; // when referencing, are we in context of shadow caster
		public GpuProgramParameters programParams;
		public MaterialScriptProgramDefinition programDef = new MaterialScriptProgramDefinition(); // this is used while defining a program

		public int techLev;	//Keep track of what tech, pass, and state level we are in
		public int passLev;
		public int stateLev;
		public List<string> defaultParamLines = new List<string>();

		// Error reporting state
		public int lineNo;
		public string filename;
	}

	/// <summary>
	///		Custom attribute to mark methods as handling the parsing for a material script attribute.
	/// </summary>
	[AttributeUsage( AttributeTargets.Method, AllowMultiple = true )]
	public sealed class MaterialAttributeParserAttribute : Attribute
	{
		private string attributeName;
		private MaterialScriptSection section;

		public MaterialAttributeParserAttribute( string name, MaterialScriptSection section )
		{
			this.attributeName = name;
			this.section = section;
		}

		public string Name
		{
			get
			{
				return attributeName;
			}
		}

		public MaterialScriptSection Section
		{
			get
			{
				return section;
			}
		}
	}
	#endregion
}
