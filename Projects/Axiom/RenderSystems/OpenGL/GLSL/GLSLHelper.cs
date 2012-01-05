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

#endregion

#region SVN Version Information

// <file>
//     <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;

using Tao.OpenGl;

using System.Text;

using Axiom.Core;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.OpenGL.GLSL
{
	/// <summary>
	/// Summary description for GLSLHelper.
	/// </summary>
	public class GLSLHelper
	{
		/// <summary>
		///		Check for GL errors and report them in the Axiom Log.
		/// </summary>
		/// <param name="error"></param>
		/// <param name="handle"></param>
		public static void CheckForGLSLError( string error, int handle )
		{
			CheckForGLSLError( error, handle, false, false );
		}

		/// <summary>
		///		Check for GL errors and report them in the Axiom Log.
		/// </summary>
		/// <param name="error"></param>
		/// <param name="handle"></param>
		/// <param name="forceException"></param>
		/// <param name="forceInfoLog"></param>
		public static void CheckForGLSLError( string error, int handle, bool forceInfoLog, bool forceException )
		{
			int glErr;
			bool errorsFound = false;
			String msg = error;

			// get all the GL errors
			glErr = Gl.glGetError();
			while( glErr != Gl.GL_NO_ERROR )
			{
				string errMsg = Glu.gluErrorString( glErr );
				msg += "\n" + errMsg;
				glErr = Gl.glGetError();
				errorsFound = true;
			}

			// if errors were found then put them in the Log and raise and exception
			if( errorsFound || forceInfoLog )
			{
				// if shader or program object then get the log message and send to the log manager
				LogObjectInfo( msg, handle );

				if( forceException )
				{
					throw new Exception( msg );
				}
			}
		}

		/// <summary>
		///		If there is a message in GL info log then post it in the Axiom Log
		/// </summary>
		/// <param name="message">The info log message string is appended to this string.</param>
		/// <param name="handle">The GL object handle that is used to retrieve the info log</param>
		/// <returns></returns>
		public static string LogObjectInfo( string message, int handle )
		{
			StringBuilder logMessage = new StringBuilder();

			if( handle > 0 )
			{
				int infologLength = 0;

				Gl.glGetObjectParameterivARB( handle, Gl.GL_OBJECT_INFO_LOG_LENGTH_ARB, out infologLength );

				if( infologLength > 0 )
				{
					int charsWritten = 0;

					Gl.glGetInfoLogARB( handle, infologLength, out charsWritten, logMessage );
					if( charsWritten > 0 )
					{
						logMessage.Append( "\n" );
						message += "\n" + logMessage.ToString();
					}
					LogManager.Instance.Write( message );
				}
			}

			return logMessage.ToString();
		}
	}
}
