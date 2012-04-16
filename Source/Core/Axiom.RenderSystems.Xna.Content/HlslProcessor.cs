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
#endregion LGPL License

#region SVN Version Information
// <file>
//     <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
//     <id value="$Id:$"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline.Processors;

using TInput = System.String;
using TOutput = Axiom.RenderSystems.Xna.Content.HlslCompiledShaders;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.Xna.Content
{

	/// <summary>
	/// This class will be instantiated by the XNA Framework Content Pipeline
	/// to apply custom processing to content data, converting an object of
	/// type TInput to TOutput. The input and output types may be the same if
	/// the processor wishes to alter data without changing its type.
	///
	/// This should be part of a Content Pipeline Extension Library project.
	///
	/// TODO: change the ContentProcessor attribute to specify the correct
	/// display name for this processor.
	/// </summary>
	[ContentProcessor( DisplayName = "Axiom HLSL Processor" )]
	public class HlslProcessor : ContentProcessor<TInput, TOutput>
	{
#if !XNA4
		private HlslIncludeHandler includeHandler = new HlslIncludeHandler();

        [DisplayName("Shader Profile")]
        [DefaultValue(ShaderProfile.PS_2_0)]
        [Description("The profile to compile this shader with.")]
        public ShaderProfile Profile
        {
            get
            {
                return shaderProfile;
            }
            set
            {
                shaderProfile = value;
            }
        }
        private ShaderProfile shaderProfile = ShaderProfile.PS_2_0;
#endif

		[DisplayName( "Entry Point" )]
		[DefaultValue( "main" )]
		[Description( "The name of the function used as the entry point for this shader." )]
		public string EntryPoint
		{
			get
			{
				return entryPoint;
			}
			set
			{
				entryPoint = value;
			}
		}
		private string entryPoint = "main";

		[DisplayName( "Preprocessor Defines" )]
		[DefaultValue( "" )]
		[Description( "A comma separated list of names to define for the preprocessor." )]
		public string PreprocessorDefines
		{
			get
			{
				return preprocessorDefines;
			}
			set
			{
				preprocessorDefines = value;
			}
		}
		private string preprocessorDefines = String.Empty;

		public override TOutput Process( TInput input, ContentProcessorContext context )
		{
#if !XNA4
			// Populate preprocessor defines
			string stringBuffer = string.Empty;
			List<CompilerMacro> defines = new List<CompilerMacro>();
			if ( preprocessorDefines != string.Empty )
			{
				stringBuffer = preprocessorDefines;

				// Split preprocessor defines and build up macro array

				if ( stringBuffer.Contains( "," ) )
				{
					string[] definesArr = stringBuffer.Split( ',' );
					foreach ( string def in definesArr )
					{
						CompilerMacro macro = new CompilerMacro();
						macro.Definition = "1\0";
						macro.Name = def + "\0";
						defines.Add( macro );
					}
				}
			}

			CompiledShader shader = ShaderCompiler.CompileFromSource( input, defines.ToArray(), includeHandler,
																	  CompilerOptions.PackMatrixRowMajor,
																	  entryPoint, shaderProfile,
																	  context.TargetPlatform );
			if ( !shader.Success )
			{
				throw new InvalidContentException( shader.ErrorsAndWarnings );
			}

			HlslCompiledShader compiledShader = new HlslCompiledShader(entryPoint, shader.GetShaderCode());
#else
            throw new NotImplementedException();
            //var shader = new EffectProcessor().Process(new EffectContent { EffectCode = input }, context);
		    HlslCompiledShader compiledShader = new HlslCompiledShader( entryPoint,  /**/null/*/shader.GetEffectCode()/**/);
#endif
			HlslCompiledShaders compiledShaders = new HlslCompiledShaders();
			compiledShaders.Add(compiledShader);
			return compiledShaders;
		}
	}
}