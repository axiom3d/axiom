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

using GLenum = OpenTK.Graphics.ES20.All;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.OpenGLES2.GLSLES
{
	internal class GLSLESLinkProgramManager : GLSLESProgramManagerCommon
	{
		private Dictionary<long, GLSLESLinkProgram> linkPrograms;

		private GLSLESLinkProgram activeLinkProgram;
		private Dictionary<string, GLenum> typeEnumMap;
		private static GLSLESLinkProgramManager _instance = null;

		public GLSLESLinkProgramManager()
		{
			this.activeLinkProgram = null;
		}

		~GLSLESLinkProgramManager()
		{
			foreach ( var key in this.linkPrograms.Keys )
			{
				this.linkPrograms[ key ] = null;
			}
		}

		public GLSLESLinkProgram ActiveLinkProgram
		{
			get
			{
				if ( this.activeLinkProgram != null )
				{
					return this.activeLinkProgram;
				}

				long activeKey = 0;

				if ( activeVertexGpuProgram != null )
				{
					activeKey = activeVertexGpuProgram.ProgramID << 32;
				}
				if ( activeFragmentGpuProgram != null )
				{
					activeKey += activeFragmentGpuProgram.ProgramID;
				}

				//Only return a link program object if a vertex or fragment program exist
				if ( activeKey > 0 )
				{
					if ( !this.linkPrograms.ContainsKey( activeKey ) )
					{
						this.activeLinkProgram = new GLSLESLinkProgram( activeVertexGpuProgram, activeFragmentGpuProgram );
						this.linkPrograms.Add( activeKey, this.activeLinkProgram );
					}
					else
					{
						this.activeLinkProgram = this.linkPrograms[ activeKey ];
					}
				}
				//Make the program object active
				if ( this.activeLinkProgram != null )
				{
					this.activeLinkProgram.Activate();
				}

				return this.activeLinkProgram;
			}
		}

		public GLSLESGpuProgram ActiveFragmentShader
		{
			set
			{
				if ( value != activeFragmentGpuProgram )
				{
					activeFragmentGpuProgram = value;
					this.activeLinkProgram = null;
				}
			}
		}

		public GLSLESGpuProgram ActiveVertexShader
		{
			set
			{
				if ( value != activeVertexGpuProgram )
				{
					activeVertexGpuProgram = value;
					this.activeLinkProgram = null;
				}
			}
		}

		public static GLSLESLinkProgramManager Instance
		{
			get
			{
				if ( _instance == null )
				{
					_instance = new GLSLESLinkProgramManager();
				}
				return _instance;
			}
		}
	}
}
