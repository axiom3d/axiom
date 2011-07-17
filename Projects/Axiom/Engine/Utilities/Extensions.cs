using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Axiom.Utilities
{
    public static class Extensions
    {
        public static string AsString(this Stream s)
        {
            using (var r = new StreamReader(s))
                return r.ReadToEnd();
        }
    }
}
