#region LGPL License
/*
Axiom Game Engine Library
Copyright (C) 2003  Axiom Project Team

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

using System;
using Axiom.Core;
using Axiom.Graphics;
using Tao.OpenGl;

namespace Axiom.RenderSystems.OpenGL {
	/// <summary>
	/// 	Specialization of vertex/fragment programs for OpenGL.
	/// </summary>
	public class GLGpuProgram : GpuProgram {
        /// <summary>
        ///    Internal OpenGL id assigned to this program.
        /// </summary>
        protected int programId;

        /// <summary>
        ///    Type of this program (vertex or fragment).
        /// </summary>
        protected int programType;

        internal GLGpuProgram(string name, GpuProgramType type, string syntaxCode) : base(name, type, syntaxCode) {
            programType = GLHelper.ConvertEnum(type);

            Ext.glGenProgramsARB(1, out programId);
        }

        protected override void LoadFromSource() {
            Ext.glBindProgramARB(programType, programId);
     
            Ext.glProgramStringARB(programType, Gl.GL_PROGRAM_FORMAT_ASCII_ARB, source.Length, source);

            // check for any errors
            if(Gl.glGetError() == Gl.GL_INVALID_OPERATION) {
                int pos;
                string error;

                Gl.glGetIntegerv(Gl.GL_PROGRAM_ERROR_POSITION_ARB, out pos);
                error = Gl.glGetString(Gl.GL_PROGRAM_ERROR_STRING_ARB);

                throw new Exception(string.Format("Error on line {0} in program '{1}'\nError: {2}", pos, name, error));
            }
        }

        public override void Unload() {
            base.Unload ();

            Ext.glDeleteProgramsARB(1, ref programId);
        }

        /// <summary>
        ///    Access to the internal program id.
        /// </summary>
        public int ProgramID {
            get {
                return programId;
            }
        }

        /// <summary>
        ///    Gets the program type (GL_VERTEX_PROGRAM_ARB, GL_FRAGMENT_PROGRAM_ARB);
        /// </summary>
        public int GLProgramType {
            get {
                return programType;
            }
        }
	}
}
