#region Namespace Declarations

using System.ComponentModel.Composition;
using Axiom.Core;
using Axiom.Input;

#endregion Namespace Declarations

namespace Axiom.Platforms.OpenTK
{
	[Export(typeof(IPlatformManager))]
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
		public Axiom.Input.InputReader CreateInputReader()
		{
			inputReader = new OpenTKInputReader();
			return inputReader;
		}

		#endregion IPlatformManager Members

		#region IDisposable Members
		public void Dispose()
		{
			if ( inputReader != null )
			{
				inputReader.Dispose();
				inputReader = null;
			}
			LogManager.Instance.Write( "OpenTK Platform Manager Shutdown." );
		}
		#endregion IDisposable Members
	}
}