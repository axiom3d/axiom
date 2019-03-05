using System.IO;

namespace Axiom.Utilities
{
	public static class StringExtensions
	{
		public static string AsString( this Stream s )
		{
			using ( var r = new StreamReader( s ) )
			{
				return r.ReadToEnd();
			}
		}
	}
}