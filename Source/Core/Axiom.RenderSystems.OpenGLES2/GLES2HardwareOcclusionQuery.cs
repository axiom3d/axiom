using System;

using Axiom.Graphics;

using GLenum = OpenTK.Graphics.ES20.All;

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
