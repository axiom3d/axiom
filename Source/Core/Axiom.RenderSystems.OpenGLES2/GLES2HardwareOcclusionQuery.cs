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

using Axiom.Graphics;

using GLenum = OpenTK.Graphics.ES20.All;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.OpenGLES2
{
	/// <summary>
	///   If you use multiple rendering passes you can test only the first pass and all other passes don't have to be rendered if the first pass result has too few pixels visible. Be sure to render all occluder first and what's out so the RenderQueue don't switch places on the occluding objects and tested object because it thinks it's more effective...
	/// </summary>
	internal class GLES2HardwareOcclusionQuery : HardwareOcclusionQuery
	{
		private int _queryID;

		public GLES2HardwareOcclusionQuery()
		{
			/*Port notes
			 * OpenTK has decided that mobile devices are not to be graced with
			 * Hardware Occlusion Queries
			 * https://github.com/mono/MonoGame/issues/414
			 */

			throw new Core.AxiomException( "Cannot allocate a Hardware query. OpenTK does not support it, sorry." );
		}

		public override void Begin()
		{
			throw new NotImplementedException();
		}

		public override void End()
		{
			throw new NotImplementedException();
		}

		public override bool PullResults( out int NumOfFragments )
		{
			throw new NotImplementedException();
		}

		public override bool IsStillOutstanding()
		{
			return true;
		}
	}
}
