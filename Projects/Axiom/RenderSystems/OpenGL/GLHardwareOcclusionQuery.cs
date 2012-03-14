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

		private readonly BaseGLSupport _glSupport;

		private readonly bool isSupportedARB;
		private readonly bool isSupportedNV;

		/// <summary>
		///		Number of fragments returned from the last query.
		/// </summary>
		private int lastFragmentCount;

		/// <summary>
		///		Id of the GL query.
		/// </summary>
		private int queryId;

		internal GLHardwareOcclusionQuery( BaseGLSupport glSupport )
		{
			this._glSupport = glSupport;
			this.isSupportedARB = this._glSupport.CheckMinVersion( GL_Version_1_5 ) || this._glSupport.CheckExtension( GL_ARB_occlusion_query );
			this.isSupportedNV = this._glSupport.CheckExtension( GL_NV_occlusion_query );

			if ( this.isSupportedNV )
			{
				Gl.glGenOcclusionQueriesNV( 1, out this.queryId );
			}
			else if ( this.isSupportedARB )
			{
				Gl.glGenQueriesARB( 1, out this.queryId );
			}
		}

		public override void Begin()
		{
			if ( this.isSupportedNV )
			{
				Gl.glBeginOcclusionQueryNV( this.queryId );
			}
			else if ( this.isSupportedARB )
			{
				Gl.glBeginQueryARB( Gl.GL_SAMPLES_PASSED_ARB, this.queryId );
			}
		}

		public override void End()
		{
			if ( this.isSupportedNV )
			{
				Gl.glEndOcclusionQueryNV();
			}
			else if ( this.isSupportedARB )
			{
				Gl.glEndQueryARB( Gl.GL_SAMPLES_PASSED_ARB );
			}
		}

		public override bool PullResults( out int NumOfFragments )
		{
			// note: flush doesn't apply to GL

			// default to returning a high count.  will be set otherwise if the query runs
			NumOfFragments = 100000;

			if ( this.isSupportedNV )
			{
				Gl.glGetOcclusionQueryivNV( this.queryId, Gl.GL_PIXEL_COUNT_NV, out NumOfFragments );
				return true;
			}
			else if ( this.isSupportedARB )
			{
				Gl.glGetQueryObjectivARB( this.queryId, Gl.GL_QUERY_RESULT_ARB, out NumOfFragments );
				return true;
			}

			return false;
		}

		public override bool IsStillOutstanding()
		{
			int available = 0;

			if ( this.isSupportedNV )
			{
				Gl.glGetOcclusionQueryivNV( this.queryId, Gl.GL_PIXEL_COUNT_AVAILABLE_NV, out available );
			}
			else if ( this.isSupportedARB )
			{
				Gl.glGetQueryivARB( this.queryId, Gl.GL_QUERY_RESULT_AVAILABLE_ARB, out available );
			}

			return available == 0;
		}

		protected override void dispose( bool disposeManagedResources )
		{
			if ( this.isSupportedNV )
			{
				Gl.glDeleteOcclusionQueriesNV( 1, ref this.queryId );
			}
			else if ( this.isSupportedARB )
			{
				Gl.glDeleteQueriesARB( 1, ref this.queryId );
			}
			base.dispose( disposeManagedResources );
		}
	}
}
