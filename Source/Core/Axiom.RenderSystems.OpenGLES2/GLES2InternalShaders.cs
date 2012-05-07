using System.Collections.Generic;

namespace Axiom.RenderSystems.OpenGLES2
{
	internal static class GLES2InternalShaders
	{
		public enum InternalShader
		{
			ES2_LightingShader
		}

		public static byte[] LightingShader
		{
			get
			{
				var retVal = new List<byte>();
				retVal.Add( byte.Parse( "attribute vec4 a_position;   \n" ) );
				retVal.Add( byte.Parse( "void main()                  \n" ) );
				retVal.Add( byte.Parse( "{                            \n" ) );
				retVal.Add( byte.Parse( " gl_Position = a_position;   \n" ) );
				retVal.Add( byte.Parse( "}                            \n" ) );
				return retVal.ToArray();
			}
		}
	}
}
