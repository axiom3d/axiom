#region LGPL License

/*
Axiom Graphics Engine Library
Copyright (C) 2003-2010 Axiom Project Team
This file is part of Axiom.RenderSystems.OpenGLES
C# version developed by bostich.

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
//     <license see="http://axiomengine.sf.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections.Generic;

using Axiom.Core;
using Axiom.Graphics;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.OpenGLES
{
	public class GLESGpuProgramManager : GpuProgramManager
	{
		public delegate GpuProgram CreateGpuProgramDelegate( ResourceManager creator, string name, ulong handle, string group, bool isManual, IManualResourceLoader loader, GpuProgramType type, string syntaxCode );

		/// <summary>
		/// </summary>
		private Dictionary<string, CreateGpuProgramDelegate> _programMap;

		/// <summary>
		/// </summary>
		public GLESGpuProgramManager()
		{
			ResourceGroupManager.Instance.RegisterResourceManager( base.ResourceType, this );
		}

		/// <summary>
		/// </summary>
		/// <param name="disposeManagedResources"> </param>
		protected override void dispose( bool disposeManagedResources )
		{
			if ( !IsDisposed )
			{
				ResourceGroupManager.Instance.UnregisterResourceManager( base.ResourceType );
			}
			// If it is available, make the call to the 
			// base class's Dispose(Boolean) method
			base.dispose( disposeManagedResources );
		}

		/// <summary>
		/// </summary>
		/// <param name="syntaxCode"> </param>
		/// <param name="createFn"> </param>
		/// <returns> </returns>
		public bool RegisterProgramFactory( string syntaxCode, CreateGpuProgramDelegate createFn )
		{
			if ( !this._programMap.ContainsKey( syntaxCode ) )
			{
				this._programMap.Add( syntaxCode, createFn );
				return true;
			}
			return false;
		}

		/// <summary>
		/// </summary>
		/// <param name="syntaxCode"> </param>
		/// <returns> </returns>
		public bool UnregisterProgramFactory( string syntaxCode )
		{
			if ( this._programMap.ContainsKey( syntaxCode ) )
			{
				this._programMap.Remove( syntaxCode );
				return true;
			}
			return false;
		}

		/// <summary>
		/// </summary>
		/// <param name="name"> </param>
		/// <param name="handle"> </param>
		/// <param name="group"> </param>
		/// <param name="isManual"> </param>
		/// <param name="loader"> </param>
		/// <param name="createParams"> </param>
		/// <returns> </returns>
		protected override Resource _create( string name, ulong handle, string group, bool isManual, IManualResourceLoader loader, Collections.NameValuePairList createParams )
		{
			if ( createParams == null || !createParams.ContainsKey( "syntax" ) || !createParams.ContainsKey( "type" ) )
			{
				throw new NotImplementedException( "You must supply 'syntax' and 'type' parameters" );
			}

			GpuProgramType gpt = 0;
			CreateGpuProgramDelegate iter = this._programMap[ createParams[ "syntax" ] ];
			if ( iter == null )
			{
				return null;
			}
			string syntaxcode = string.Empty;
			foreach ( var pair in this._programMap )
			{
				if ( pair.Value == iter )
				{
					syntaxcode = pair.Key;
					break;
				}
			}
			if ( createParams[ "type" ] == "vertex_program" )
			{
				gpt = GpuProgramType.Vertex;
			}
			else if ( createParams[ "type" ] == "fragment_program" )
			{
				gpt = GpuProgramType.Fragment;
			}
			else
			{
				throw new AxiomException( "Unknown GpuProgramType : " + createParams[ "type" ] );
			}
			return iter( this, name, handle, group, isManual, loader, gpt, syntaxcode );
		}

		/// <summary>
		/// </summary>
		/// <param name="name"> </param>
		/// <param name="handle"> </param>
		/// <param name="group"> </param>
		/// <param name="isManual"> </param>
		/// <param name="loader"> </param>
		/// <param name="type"> </param>
		/// <param name="syntaxCode"> </param>
		/// <returns> </returns>
		protected override Resource _create( string name, ulong handle, string group, bool isManual, IManualResourceLoader loader, GpuProgramType type, string syntaxCode )
		{
			return new DummyGpuProgram( this, name, handle, group, isManual, loader );
		}

		/// <summary>
		///   TODO not implemented
		/// </summary>
		private class DummyGpuProgram : GpuProgram
		{
			public DummyGpuProgram( ResourceManager creator, string name, ulong handle, string group, bool isManual, IManualResourceLoader loader )
				: base( creator, name, handle, group, isManual, loader ) {}

			/// <summary>
			/// </summary>
			public override void Unload() {}

			/// <summary>
			/// </summary>
			protected override void LoadFromSource() {}
		}
	}
}
