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
//     <license see="http://axiomengine.sf.net/wiki/index.php/license.txt"/>
//     <id value="$Id: GLESDefaultHardwareBufferManager.cs 2805 2011-08-17 16:26:56Z borrillis $"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using Axiom.Graphics;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.OpenGLES2
{
	internal class GLES2DefaultHardwareBufferManager : HardwareBufferManagerBase
	{
		public GLES2DefaultHardwareBufferManager() {}

		protected override void dispose( bool disposeManagedResources )
		{
			if ( !IsDisposed )
			{
				if ( disposeManagedResources )
				{
					DestroyAllDeclarations();
					DestroyAllBindings();
				}
			}

			base.dispose( disposeManagedResources );
		}

		public override HardwareVertexBuffer CreateVertexBuffer( VertexDeclaration vertexDeclaration, int numVerts, BufferUsage usage, bool useShadowBuffer )
		{
			return new GLES2DefaultHardwareVertexBuffer( vertexDeclaration, numVerts, usage );
		}

		public override HardwareIndexBuffer CreateIndexBuffer( IndexType type, int numIndices, BufferUsage usage, bool useShadowBuffer )
		{
			return new GLES2DefaultHardwareIndexBuffer( type, numIndices, usage );
		}

		//public override RenderToVertexBuffer CreateRenderToVertexBuffer()
		//{
		//    throw new AxiomException("Cannot create RenderToVertexBuffer in GLES2DefaultHardwareBufferManagerBase);
		//}
	}
}
