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

using D3D9 = SharpDX.Direct3D9;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.DirectX9
{
	/// <summary>
	/// Represents a Direct3D rendering resource.
	/// Provide unified interface to
	/// handle various device states.
	/// </summary>
	/// <note>
	/// romeoxbm: LockDeviceAccess and UnlockDeviceAccess have been removed because those interface members
	/// cannot be implemented as static, and they has been moved to ID3D9ResourceExtensions class as
	/// extension methods.
	/// </note>
	[OgreVersion( 1, 7, 2790 )]
	public interface ID3D9Resource
	{
		/// <summary>
		/// Called immediately after the Direct3D device has been created.
		/// </summary>
		[OgreVersion( 1, 7, 2790 )]
		void NotifyOnDeviceCreate( D3D9.Device d3d9Device );

		/// <summary>
		/// Called before the Direct3D device is going to be destroyed.
		/// </summary>
		[OgreVersion( 1, 7, 2790 )]
		void NotifyOnDeviceDestroy( D3D9.Device d3d9Device );

		/// <summary>
		/// Called immediately after the Direct3D device has entered a lost state.
		/// This is the place to release non-managed resources.
		/// </summary>
		[OgreVersion( 1, 7, 2790 )]
		void NotifyOnDeviceLost( D3D9.Device d3d9Device );

		/// <summary>
		/// Called immediately after the Direct3D device has been reset.
		/// This is the place to create non-managed resources.
		/// </summary>
		[OgreVersion( 1, 7, 2790 )]
		void NotifyOnDeviceReset( D3D9.Device d3d9Device );
	};

	public static class ID3D9ResourceExtensions
	{
#if AXIOM_THREAD_SUPPORT
		private static readonly object deviceLockMutex = new object();
#endif

		[OgreVersion( 1, 7, 2 )]
		public static void LockDeviceAccess( this ID3D9Resource res )
		{
#if AXIOM_THREAD_SUPPORT
			if ( Configuration.Config.AxiomThreadLevel == 1 )
				System.Threading.Monitor.Enter( deviceLockMutex );
#endif
		}

		[OgreVersion( 1, 7, 2 )]
		public static void UnlockDeviceAccess( this ID3D9Resource res )
		{
#if AXIOM_THREAD_SUPPORT
			if ( Configuration.Config.AxiomThreadLevel == 1 )
				System.Threading.Monitor.Exit( deviceLockMutex );
#endif
		}
	};
}