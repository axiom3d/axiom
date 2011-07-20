using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Axiom.Core
{
	public static class StringExtensions
	{
		public static string AsString( this Stream stream )
		{
			var reader = new StreamReader(stream, System.Text.Encoding.UTF8);
			return reader.ReadToEnd();
		}
	}
}
