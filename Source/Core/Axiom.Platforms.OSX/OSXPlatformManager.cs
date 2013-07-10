#region Namespace Declarations

using System.ComponentModel.Composition;
using Axiom.Core;
using Axiom.Input;

#endregion Namespace Declarations

namespace Axiom.Platforms.Linux
{
	[Export( typeof ( IPlatformManager ) )]
	public class OSXPlatformManager : IPlatformManager
	{
		public OSXPlatformManager()
		{
			LogManager.Instance.Write( "OSX Platform Manager Loaded." );
		}

		public void Dispose()
		{
			LogManager.Instance.Write( "OSX Platform Manager Shutdown." );
		}
	}
}