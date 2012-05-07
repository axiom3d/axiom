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

using System.Collections.Generic;

using Axiom.Core;
using Axiom.Graphics;

#endregion Namespace Declarations
			
namespace Axiom.RenderSystems.OpenGLES2
{
	internal class GLES2GpuProgramManager : GpuProgramManager
	{
		private Dictionary<string, CreateGpuProgramDelegate> _programMap;

		public delegate GpuProgram CreateGpuProgramDelegate( ResourceManager creator, string name, ulong handle, string group, bool isManual, IManualResourceLoader loader, GpuProgramType type, string syntaxCode );

		public GLES2GpuProgramManager()
		{
			//Register with resource group member
			ResourceGroupManager.Instance.RegisterResourceManager( ResourceType, this );
		}

		protected override void dispose( bool disposeManagedResources )
		{
			//Unregister withh resource group manager
			ResourceGroupManager.Instance.UnregisterResourceManager( ResourceType );
			base.dispose( disposeManagedResources );
		}

		protected override Resource _create( string name, ulong handle, string group, bool isManual, IManualResourceLoader loader, Collections.NameValuePairList createParams )
		{
			string paramSyntax = string.Empty;
			string paramType = string.Empty;

			if ( createParams == null || createParams.ContainsKey( "syntax" ) == false || createParams.ContainsKey( "type" ) == false )
			{
				throw new AxiomException( "You must supply 'syntax' and 'type' parameters" );
			}
			else
			{
				paramSyntax = createParams[ "syntax" ];
				paramType = createParams[ "type" ];
			}
			CreateGpuProgramDelegate iter = null;
			if ( this._programMap.ContainsKey( paramSyntax ) )
			{
				iter = this._programMap[ paramSyntax ];
			}
			else
			{
				// No factory, this is an unsupported syntax code, probably for another rendersystem
				// Create a basic one, it doesn't matter what it is since it won't be used
				return new GLES2GpuProgram( this, name, handle, group, isManual, loader );
			}
			GpuProgramType gpt;
			if ( paramType == "vertex_program" )
			{
				gpt = GpuProgramType.Vertex;
			}
			else
			{
				gpt = GpuProgramType.Fragment;
			}

			return iter( this, name, handle, group, isManual, loader, gpt, paramSyntax );
		}

		protected override Resource _create( string name, ulong handle, string group, bool isManual, IManualResourceLoader loader, GpuProgramType type, string syntaxCode )
		{
			CreateGpuProgramDelegate iter = null;

			if ( this._programMap.ContainsKey( syntaxCode ) == false )
			{
				//No factory, this is an unsupported syntax code, probably for another rendersystem
				//Create a basic one, it doens't matter what it is since it won't be used
				return new GLES2GpuProgram( this, name, handle, group, isManual, loader );
			}
			else
			{
				iter = this._programMap[ syntaxCode ];
			}
			return iter( this, name, handle, group, isManual, loader, type, syntaxCode );
		}

		public bool RegisterProgramFactory( string syntaxCode, CreateGpuProgramDelegate createFN )
		{
			if ( this._programMap.ContainsKey( syntaxCode ) == false )
			{
				this._programMap.Add( syntaxCode, createFN );
				return true;
			}
			else
			{
				return false;
			}
		}

		public bool UnregisterProgramFactory( string syntaxCode )
		{
			if ( this._programMap.ContainsKey( syntaxCode ) == false )
			{
				this._programMap.Remove( syntaxCode );
				return true;
			}
			else
			{
				return false;
			}
		}
	}
}
