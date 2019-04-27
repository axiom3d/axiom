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
using Axiom.Core;
using Axiom.Graphics;
using Tao.OpenGl;
using System.Diagnostics;
using ResourceHandle = System.UInt64;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.OpenGL
{
    /// <summary>
    /// 	Specialization of vertex/fragment programs for OpenGL.
    /// </summary>
    public class GLGpuProgram : GpuProgram
    {
        #region Fields

        /// <summary>
        ///    Internal OpenGL id assigned to this program.
        /// </summary>
        protected int programId;

        /// <summary>
        ///    Type of this program (vertex or fragment).
        /// </summary>
        protected int programType;

        /// <summary>
        ///     For use internally to store temp values for passing constants, etc.
        /// </summary>
        protected float[] tempProgramFloats = new float[4];

        #endregion Fields

        #region Constructors

        /// <summary>
        ///     Constructor.
        /// </summary>
        /// <param name="name">Name of the program.</param>
        /// <param name="type">Type of program (vertex or fragment).</param>
        /// <param name="syntaxCode">Syntax code (i.e. arbvp1, etc).</param>
        internal GLGpuProgram(ResourceManager parent, string name, ResourceHandle handle, string group, bool isManual,
                               IManualResourceLoader loader)
            : base(parent, name, handle, group, isManual, loader)
        {
        }

        #endregion Constructors

        #region GpuProgram Methods

        /// <summary>
        ///     Called when a program needs to be bound.
        /// </summary>
        public virtual void Bind()
        {
            // do nothing
        }

        /// <summary>
        ///     Called when a program needs to be unbound.
        /// </summary>
        public virtual void Unbind()
        {
            // do nothing
        }

        /// <summary>
        ///     Called to create the program from source.
        /// </summary>
        protected override void LoadFromSource()
        {
            // do nothing
        }

        /// <summary>
        ///     Called when a program needs to bind the supplied parameters.
        /// </summary>
        [OgreVersion(1, 7, 2790)]
        public virtual void BindProgramParameters(GpuProgramParameters parms, GpuProgramParameters.GpuParamVariability mask)
        {
            // do nothing
        }

        /// <summary>
        /// Bind just the pass iteration parameters
        /// </summary>
        [OgreVersion(1, 7, 2790)]
        public virtual void BindProgramPassIterationParameters(GpuProgramParameters parms)
        {
            // do nothing
        }

        #endregion GpuProgram Methods

        #region Properties

        /// <summary>
        ///    Access to the internal program id.
        /// </summary>
        public int ProgramID
        {
            get
            {
                return this.programId;
            }
        }

        /// <summary>
        ///    Gets the program type (GL_VERTEX_PROGRAM_ARB, GL_FRAGMENT_PROGRAM_ARB, etc);
        /// </summary>
        public int GLProgramType
        {
            get
            {
                return this.programType;
            }
        }

        #endregion Properties

        internal virtual bool IsAttributeValid(VertexElementSemantic semantic, uint index)
        {
            switch (semantic)
            {
                case VertexElementSemantic.Diffuse:
                case VertexElementSemantic.Normal:
                case VertexElementSemantic.Position:
                case VertexElementSemantic.Specular:
                case VertexElementSemantic.TexCoords:
                default:
                    return false;
                case VertexElementSemantic.Binormal:
                case VertexElementSemantic.BlendIndices:
                case VertexElementSemantic.BlendWeights:
                case VertexElementSemantic.Tangent:
                    return true;
            }
        }

        internal static uint FixedAttributeIndex(VertexElementSemantic semantic, uint index)
        {
            // Some drivers (e.g. OS X on nvidia) incorrectly determine the attribute binding automatically
            // and end up aliasing existing built-ins. So avoid! Fixed builtins are: 

            //  a  builtin                          custom attrib name
            // ----------------------------------------------
            //      0  gl_Vertex                    vertex
            //  1  n/a                                      blendWeights            
            //      2  gl_Normal                    normal
            //      3  gl_Color                             colour
            //      4  gl_SecondaryColor    secondary_colour
            //      5  gl_FogCoord                  fog_coord
            //  7  n/a                                      blendIndices
            //      8  gl_MultiTexCoord0    uv0
            //      9  gl_MultiTexCoord1    uv1
            //      10 gl_MultiTexCoord2    uv2
            //      11 gl_MultiTexCoord3    uv3
            //      12 gl_MultiTexCoord4    uv4
            //      13 gl_MultiTexCoord5    uv5
            //      14 gl_MultiTexCoord6    uv6, tangent
            //      15 gl_MultiTexCoord7    uv7, binormal
            switch (semantic)
            {
                case VertexElementSemantic.Position:
                    return 0;
                case VertexElementSemantic.BlendWeights:
                    return 1;
                case VertexElementSemantic.Normal:
                    return 2;
                case VertexElementSemantic.Diffuse:
                    return 3;
                case VertexElementSemantic.Specular:
                    return 4;
                case VertexElementSemantic.BlendIndices:
                    return 7;
                case VertexElementSemantic.TexCoords:
                    return 8 + index;
                case VertexElementSemantic.Tangent:
                    return 14;
                case VertexElementSemantic.Binormal:
                    return 15;
                default:
                    Debug.Assert(false, "Missing attribute!");
                    // Unreachable code, but keeps compiler happy
                    return 0;
            }
        }

        internal virtual uint AttributeIndex(VertexElementSemantic semantic, uint index)
        {
            return FixedAttributeIndex(semantic, index);
        }
    }
}