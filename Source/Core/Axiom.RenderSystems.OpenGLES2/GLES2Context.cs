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
//     <id value="$Id: GLESContext.cs 2173 2010-09-09 15:27:05Z borrillis $"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

#endregion Namespace Declarations

namespace Axiom.RenderSystems.OpenGLES2
{
	/// <summary>
	///   Class that encapsulates an GL context. (IE a window/pbuffer). This is a virtual base class which should be implemented in a GLES2Support. This object can also be used to cache renderstate if we decide to do so in the future.
	/// </summary>
	[OgreVersion( 1, 8, 0, "It's from trunk rev.'b0d2092773fb'" )]
	public abstract class GLES2Context
	{
		protected bool _initialized;

		/// <summary>
		/// </summary>
		public GLES2Context()
		{
			this._initialized = false;
		}

		/// <summary>
		/// </summary>
		public bool IsInitialized
		{
			get { return this._initialized; }
			set { this._initialized = value; }
		}

		/// <summary>
		///   Enable the context. All subsequent rendering commands will go here.
		/// </summary>
		public abstract void SetCurrent();

		/// <summary>
		///   This is called before another context is made current. By default, nothing is done here.
		/// </summary>
		public abstract void EndCurrent();

		/// <summary>
		///   Create a new context based on the same window/pbuffer as this context - mostly useful for additional threads.
		/// </summary>
		/// <note>The caller is responsible for deleting the returned context.</note>
		/// <returns> Cloned GLESContext </returns>
		public abstract GLES2Context Clone();

		/// <summary>
		///   Release the render context
		/// </summary>
		public virtual void ReleaseContext() {}

		/// <summary>
		/// </summary>
		public abstract void Dispose();
	}
}
