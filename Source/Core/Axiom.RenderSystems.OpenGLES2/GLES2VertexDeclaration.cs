using System;
using Axiom.Graphics;

using GL = OpenTK.Graphics.ES20.GL;

namespace Axiom.RenderSystems.OpenGLES2
{
	public class GLES2VertexDeclaration : VertexDeclaration
	{
		private int _vao;

		public bool IsInitialized { get; protected set; }

		public GLES2VertexDeclaration()
		{
			_vao = 0;

#if !AXIOM_NO_GLES2_VAO_SUPPORT
#if GL_OES_vertex_array_object
			GL.GenVertexArraysOES( 1, &_vao );
			LogManager.Instance.Write( "[GLES2] Created VAO {0}." , _vao );

			GLES2Config.CheckError( this );
				
			if (_vao != 0)
			{
				throw new AxiomException( "[GLES2] Cannot create GL ES Vertex Array Object" );
			}
#endif
#endif
		}

		public void Bind()
		{
#if !AXIOM_NO_GLES2_VAO_SUPPORT
#if GL_OES_vertex_array_object
			LogManager.Instance.Write( "[GLES2] Binding VAO {0}." , _vao );
			GL.BindVertexArraysOES( 1, &_vao );
			GLES2Config.CheckError( this );
#endif
#endif
		}

		protected override void dispose (bool disposeManagedResources)
		{
			if ( !IsDisposed ) 
			{
				if ( disposeManagedResources ) { }

#if !AXIOM_NO_GLES2_VAO_SUPPORT
#if GL_OES_vertex_array_object
				LogManager.Instance.Write( "[GLES2] Deleting VAO {0}." , _vao );
				GL.DeleteVertexArraysOES( 1, &_vao );
				GLES2Config.CheckError( this );
#endif
#endif
			}
			base.dispose (disposeManagedResources);
		}
	}
}