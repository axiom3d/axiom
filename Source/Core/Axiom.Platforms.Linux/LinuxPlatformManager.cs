#region Namespace Declarations

using System.ComponentModel.Composition;
using Axiom.Core;
using Axiom.Input;

#endregion Namespace Declarations

namespace Axiom.Platforms.Linux
{
	[Export( typeof ( IPlatformManager ) )]
	public class LinuxPlatformManager : IPlatformManager
	{
		public LinuxPlatformManager()
		{
			LogManager.Instance.Write( "Linux Platform Manager Loaded." );
		}

		public void Dispose()
		{
			LogManager.Instance.Write( "Linux Platform Manager Shutdown." );
		}
	}
}