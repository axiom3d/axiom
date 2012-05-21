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

using Axiom.Graphics;

using GLenum = OpenTK.Graphics.ES20.All;

using Axiom.Core;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.OpenGLES2
{
	/// <summary>
	/// Genralized low-level GL program, can be applied to multiple types (eg ARB and NV)
	/// </summary>
	internal class GLES2GpuProgram : GpuProgram
	{
		protected int _programID;
		private GLenum _programType;

		public GLES2GpuProgram( ResourceManager creator, string name, ulong handle, string group, bool isManual, IManualResourceLoader loader )
			: base( creator, name, handle, group, isManual, loader ) {}

		public virtual void BindProgram() {}
		public virtual void UnbindProgram() {}
		public virtual void BindProgramParameters( GpuProgramParameters parms, uint mask ) {}
		public virtual void BindProgramPassIterationParameters( GpuProgramParameters parms ) {}

		protected override void dispose( bool disposeManagedResources )
		{
			//Have to call this here rather than in Resource destructor
			//since calling virtual methods in base destructors causes crash
			unload();
			base.dispose( disposeManagedResources );
		}

		public static GLenum GetGLShaderType( GpuProgramType programType )
		{
			switch ( programType )
			{
				case GpuProgramType.Vertex:
				default:
					return GLenum.VertexShader;

				case GpuProgramType.Fragment:
					return GLenum.FragmentShader;
			}
		}

		/// <summary>
		/// Gets the assigned GL program id
		/// </summary>
		public int ProgramID
		{
			get { return this._programID; }
		}

		protected override void LoadFromSource()
		{
			//abstract override, nothing todo
		}
	}
}
