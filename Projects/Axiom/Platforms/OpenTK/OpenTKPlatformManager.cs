#region Namespace Declarations

using System.ComponentModel.Composition;

using Axiom.Core;
using Axiom.Input;

#endregion Namespace Declarations

namespace Axiom.Platforms.OpenTK
{
	[Export( typeof( IPlatformManager ) )]
	public class OpenTKPlatformManager : IPlatformManager
	{
		#region Fields

		/// <summary>
		///		Reference to the current input reader.
		/// </summary>
		private InputReader inputReader;

		#endregion Fields

		#region IPlatformManager Members

		/// <summary>
		///		Creates an InputReader
		/// </summary>
		/// <returns></returns>
		public InputReader CreateInputReader()
		{
			this.inputReader = new OpenTKInputReader();
			return this.inputReader;
		}

		public void Dispose()
		{
			if ( this.inputReader != null )
			{
				this.inputReader.Dispose();
				this.inputReader = null;
			}
			LogManager.Instance.Write( "OpenTK Platform Manager Shutdown." );
		}

		#endregion
	}
}
