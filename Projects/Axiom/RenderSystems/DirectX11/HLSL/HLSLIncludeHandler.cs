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

using System.IO;

using Axiom.Core;

using D3D9 = SharpDX.Direct3D9;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.DirectX9.HLSL
{
	public class HLSLIncludeHandler : DisposableObject, D3D9.Include
	{
		protected Resource program;

		public System.IDisposable Shadow
		{
			get
			{
				throw new System.NotImplementedException();
			}
			set
			{
				throw new System.NotImplementedException();
			}
		}


		[OgreVersion( 1, 7, 2 )]
		public HLSLIncludeHandler( Resource sourceProgram )
			: base()
		{
			this.program = sourceProgram;
		}

		protected override void dispose( bool disposeManagedResources )
		{
			if ( !this.IsDisposed && disposeManagedResources )
			{
				program.SafeDispose();
				program = null;
			}

			base.dispose( disposeManagedResources );
		}

		public void Open( D3D9.IncludeType type, string fileName, out Stream fileStream )
		{
			fileStream = ResourceGroupManager.Instance.OpenResource( fileName, this.program.Group, true, this.program );
		}

		public Stream Open( D3D9.IncludeType type, string fileName, Stream parentStream )
		{
			return ResourceGroupManager.Instance.OpenResource( fileName, this.program.Group, true, this.program );
		}

		public void Close( Stream fileStream )
		{
			fileStream.Close();
		}
	};
}
