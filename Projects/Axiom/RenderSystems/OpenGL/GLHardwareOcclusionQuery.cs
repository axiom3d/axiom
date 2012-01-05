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

#endregion LGPL License

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

#endregion Namespace Declarations

namespace Axiom.RenderSystems.OpenGL
{
	/// <summary>
	/// Summary description for GLHardwareOcclusionQuery.
	/// </summary>
	public class GLHardwareOcclusionQuery : HardwareOcclusionQuery
	{
		private const string GL_ARB_occlusion_query = "GL_ARB_occlusion_query";
		private const string GL_NV_occlusion_query = "GL_NV_occlusion_query";
		private const string GL_Version_1_5 = "1.5";

		private BaseGLSupport _glSupport;

		/// <summary>
		///		Number of fragments returned from the last query.
		/// </summary>
		private int lastFragmentCount;

		/// <summary>
		///		Id of the GL query.
		/// </summary>
		private int queryId;

		private bool isSupportedARB;
		private bool isSupportedNV;

		internal GLHardwareOcclusionQuery( BaseGLSupport glSupport )
		{
			this._glSupport = glSupport;
			isSupportedARB = _glSupport.CheckMinVersion( GL_Version_1_5 ) || _glSupport.CheckExtension( GL_ARB_occlusion_query );
			isSupportedNV = _glSupport.CheckExtension( GL_NV_occlusion_query );

			if( isSupportedNV )
			{
				Gl.glGenOcclusionQueriesNV( 1, out this.queryId );
			}
			else if( isSupportedARB )
			{
				Gl.glGenQueriesARB( 1, out this.queryId );
			}
		}

		#region HardwareOcclusionQuery Members

		public override void Begin()
		{
			if( isSupportedNV )
			{
				Gl.glBeginOcclusionQueryNV( this.queryId );
			}
			else if( isSupportedARB )
			{
				Gl.glBeginQueryARB( Gl.GL_SAMPLES_PASSED_ARB, this.queryId );
			}
		}

		public override void End()
		{
			if( isSupportedNV )
			{
				Gl.glEndOcclusionQueryNV();
			}
			else if( isSupportedARB )
			{
				Gl.glEndQueryARB( Gl.GL_SAMPLES_PASSED_ARB );
			}
		}

		public override int PullResults()
		{
			// note: flush doesn't apply to GL

			// default to returning a high count.  will be set otherwise if the query runs
			lastFragmentCount = 100000;

			if( isSupportedNV )
			{
				Gl.glGetOcclusionQueryivNV( this.queryId, Gl.GL_PIXEL_COUNT_NV, out lastFragmentCount );
			}
			else if( isSupportedARB )
			{
				Gl.glGetQueryObjectivARB( this.queryId, Gl.GL_QUERY_RESULT_ARB, out lastFragmentCount );
			}

			return lastFragmentCount;
		}

		public override bool IsStillOutstanding()
		{
			int available = 0;

			if( isSupportedNV )
			{
				Gl.glGetOcclusionQueryivNV( this.queryId, Gl.GL_PIXEL_COUNT_AVAILABLE_NV, out available );
			}
			else if( isSupportedARB )
			{
				Gl.glGetQueryivARB( this.queryId, Gl.GL_QUERY_RESULT_AVAILABLE_ARB, out available );
			}

			return available == 0;
		}

		protected override void dispose( bool disposeManagedResources )
		{
			if( isSupportedNV )
			{
				Gl.glDeleteOcclusionQueriesNV( 1, ref this.queryId );
			}
			else if( isSupportedARB )
			{
				Gl.glDeleteQueriesARB( 1, ref this.queryId );
			}
			base.dispose( disposeManagedResources );
		}

		#endregion HardwareOcclusionQuery Members
	}
}
