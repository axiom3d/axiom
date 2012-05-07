#region MIT/X11 License

//Copyright © 2003-2012 Axiom 3D Rendering Engine Project
//
//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:
//
//The above copyright notice and this permission notice shall be included in
//all copies or substantial portions of the Software.
//
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//THE SOFTWARE.

#endregion License

#region SVN Version Information

// <file>
//     <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using GL = OpenTK.Graphics.ES20.GL;
using GLenum = OpenTK.Graphics.ES20.All;

using Axiom.Graphics;

#endregion Namespace Declarations
			
namespace Axiom.RenderSystems.OpenGLES2.GLSLES
{
	internal abstract class GLSLESProgramCommon
	{
		protected List<GLSLESProgramManagerCommon.GLUniformReference> glUniformReferences;

		protected GLSLESGpuProgram vertexProgram, fragmentProgram;

		protected bool uniformRefsBuilt;
		protected int glProgramHandle;
		protected int linked;
		protected bool triedToLinkAndFailed;
		protected bool skeletalAnimation;

		protected int[ , ] customAttribues = new int[ 9,Configuration.Config.MaxTextureCoordSets ];
		protected static int NullCustomAttributesIndex = -2;
		protected static int NotFoundCustomAttributesIndex = -1;
		protected Dictionary<string, VertexElementSemantic> semanticTypeMap;

		public GLSLESProgramCommon( GLSLESGpuProgram vertexProgram, GLSLESGpuProgram fragmentProgram )
		{
			this.vertexProgram = vertexProgram;
			this.fragmentProgram = fragmentProgram;
			this.uniformRefsBuilt = false;
			this.linked = 0;
			this.triedToLinkAndFailed = false;

			//init customAttributeIndexes
			for ( int i = 0; i < 9; i++ )
			{
				for ( int j = 0; j < Configuration.Config.MaxTextureCoordSets; j++ )
				{
					this.customAttribues[ i, j ] = NullCustomAttributesIndex;
				}
			}

			//Initialize the attribute to semantic map
			this.semanticTypeMap.Add( "vertex", VertexElementSemantic.Position );
			this.semanticTypeMap.Add( "blendWeights", VertexElementSemantic.BlendWeights );
			this.semanticTypeMap.Add( "normal", VertexElementSemantic.Normal );
			this.semanticTypeMap.Add( "colour", VertexElementSemantic.Diffuse );
			this.semanticTypeMap.Add( "secondary_colour", VertexElementSemantic.Specular );
			this.semanticTypeMap.Add( "blendIndices", VertexElementSemantic.BlendIndices );
			this.semanticTypeMap.Add( "tangent", VertexElementSemantic.Tangent );
			this.semanticTypeMap.Add( "binormal", VertexElementSemantic.Binormal );
			this.semanticTypeMap.Add( "uv", VertexElementSemantic.TexCoords );

			if ( ( vertexProgram == null || fragmentProgram == null ) && false ) //!Core.Root.Instance.RenderSystem.Capabilities.HasCapability(Capabilities.SeperateShaderObjects))
			{
				throw new Core.AxiomException( "Attempted to create a shader program without both a vertex and fragment program" );
			}
		}

		~GLSLESProgramCommon()
		{
			OpenTK.Graphics.ES20.GL.DeleteProgram( this.glProgramHandle );
		}

		protected virtual void BuildGLUniformReferences() {}

		protected void GetMicroCodeFromCache()
		{
			/*Port notes
			 * Ogre attempts to get the code from a cache here,
			 * but Axiom hasn't implemented the GpuManager.GetMicrocodeFromCache() function
			 * Fortunately Ogre falls back on CompileAndLink(), which is what we're goint to do
			 */
			this.CompileAndLink();
		}

		protected abstract void CompileAndLink();
		protected abstract void _useProgram();

		protected VertexElementSemantic GetAttributeSemanticEnum( string type )
		{
			if ( this.semanticTypeMap.ContainsKey( type ) )
			{
				return this.semanticTypeMap[ type ];
			}
			else
			{
				return 0;
			}
		}

		protected string GetAttributeSemanticString( VertexElementSemantic semantic )
		{
			foreach ( var key in this.semanticTypeMap.Keys )
			{
				if ( this.semanticTypeMap[ key ] == semantic )
				{
					return key;
				}
			}

			return string.Empty;
		}

		public abstract void Activate();

		public abstract void UpdateUniforms( GpuProgramParameters parms, int mask, GpuProgramType fromProgType );

		public abstract void UpdatePassIterationUniforms( GpuProgramParameters parms );

		public virtual int GetAttributeIndex( VertexElementSemantic semantic, int index )
		{
			int res = this.customAttribues[ (int) semantic - 1, index ];
			if ( res == NullCustomAttributesIndex )
			{
				string attString = this.GetAttributeSemanticString( semantic );
				int attrib = GL.GetAttribLocation( this.glProgramHandle, attString );

				//sadly position is a special case
				if ( attrib == NotFoundCustomAttributesIndex && semantic == VertexElementSemantic.Position )
				{
					attrib = GL.GetAttribLocation( this.glProgramHandle, "position" );
				}

				//for uv and other case the index is a part of the name
				if ( attrib == NotFoundCustomAttributesIndex )
				{
					string attStringWithSemantic = attString + index.ToString();
					attrib = GL.GetAttribLocation( this.glProgramHandle, attStringWithSemantic );
				}

				//update customAttributes with the index we found (or didnt' find)
				this.customAttribues[ (int) semantic - 1, index ] = attrib;
				res = attrib;
			}
			return res;
		}

		public bool IsAttributeValid( VertexElementSemantic semantic, int index )
		{
			return this.GetAttributeIndex( semantic, index ) != NotFoundCustomAttributesIndex;
		}

		public int GLProgramHandle
		{
			get { return this.glProgramHandle; }
		}

		public bool SkeletalAnimationIncluded
		{
			get { return this.skeletalAnimation; }
			set { this.skeletalAnimation = value; }
		}

		public GLSLESGpuProgram VertexProgram
		{
			get { return this.vertexProgram; }
			set { this.vertexProgram = value; }
		}

		protected string CombinedName
		{
			get
			{
				string name = string.Empty;

				if ( this.vertexProgram != null )
				{
					name += "Vertex Program:";
					name += this.vertexProgram.Name;
				}
				if ( this.fragmentProgram != null )
				{
					name += " Fragment Program:";
					name += this.fragmentProgram.Name;
				}
				name += '\n';

				return name;
			}
		}
	}
}
