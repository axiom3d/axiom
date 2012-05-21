using System.IO;
using System.IO.IsolatedStorage;

#if (SILVERLIGHT || WINDOWS_PHONE || XBOX )

namespace System.Diagnostics
{
    public class DefaultTraceListener : TraceListener
    {
        public static string LogName { get; set; }

        public override void WriteLine(string message)
        {
            Write(message + "\r\n");
        }

        public override void Write(string message)
        {
            Console.Write(message);

            if (Debugger.IsAttached)
                Debugger.Log(0, "TRACE", message);

            if (LogName == null)
                return;

            using (var isf = IsolatedStorageFile.GetUserStoreForApplication())
            using (var isfs = new IsolatedStorageFileStream(LogName, FileMode.Append, isf))
            using (var sw = new StreamWriter(isfs))
            {
                sw.Write(message);
                sw.Close();
            }
        }
    }
}

#endif