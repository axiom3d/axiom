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

using Axiom.Core;
using Axiom.Graphics;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.DirectX9.HLSL
{
	/// <summary>
	/// Summary description for HLSLProgramFactory.
	/// </summary>
	public class D3D9HLSLProgramFactory : HighLevelGpuProgramFactory
	{
		private string language = "hlsl";

		#region HighLevelGpuProgramFactory Implementation

		/// <summary>
		/// Gets the high level language that this factory handles requests for.
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public override string Language
		{
			get
			{
				return this.language;
			}
		}

		[OgreVersion( 1, 7, 2 )]
		public override HighLevelGpuProgram CreateInstance( ResourceManager creator, string name, ulong handle, string group, bool isManual, IManualResourceLoader loader )
		{
			return new D3D9HLSLProgram( creator, name, handle, group, isManual, loader );
		}

		#endregion HighLevelGpuProgramFactory Implementation
	};
}
