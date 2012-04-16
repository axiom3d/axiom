﻿#if (SILVERLIGHT || WINDOWS_PHONE || XBOX || PORTABLE)
//
// System.Diagnostics.TraceImpl.cs
//
// Authors:
//   Jonathan Pryor (jonpryor@vt.edu)
//
// (C) 2002, 2005 Jonathan Pryor
//

//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System.Collections;
using System.Threading;

namespace System.Diagnostics
{
    internal class TraceImpl
    {
#if NO_LOCK_FREE
		private static object lock_ = new object ();
#endif

        [ThreadStatic] private static int indentLevel;

        [ThreadStatic] private static int indentSize;

        static TraceImpl()
        {
            // defaults
            AutoFlush = false;
            indentLevel = 0;
            indentSize = 4;
        }

        private TraceImpl() {}

        public static bool AutoFlush { get; set; }

        public static int IndentLevel
        {
            get { return indentLevel; }
            set
            {
                lock (ListenersSyncRoot)
                {
                    indentLevel = value;

                    foreach (TraceListener t in Listeners)
                    {
                        t.IndentLevel = indentLevel;
                    }
                }
            }
        }

        public static int IndentSize
        {
            get { return indentSize; }
            set
            {
                lock (ListenersSyncRoot)
                {
                    indentSize = value;

                    foreach (TraceListener t in Listeners)
                    {
                        t.IndentSize = indentSize;
                    }
                }
            }
        }

        private static object listeners;

        public static TraceListenerCollection Listeners
        {
            get
            {
                InitOnce();

                return (TraceListenerCollection) listeners;
            }
        }

        private static object ListenersSyncRoot
        {
            get { return ((ICollection) Listeners).SyncRoot; }
        }

        // Initialize the world.
        //
        // This logically belongs in the static constructor (as it only needs
        // to be done once), except for one thing: if the .config file has a
        // syntax error, .NET throws a ConfigurationException.  If we read the
        // .config file in the static ctor, we throw a ConfigurationException
        // from the static ctor, which results in a TypeLoadException.  Oops.
        // Reading the .config file here will allow the static ctor to
        // complete successfully, allowing us to throw a normal
        // ConfigurationException should the .config file contain an error.
        //
        // There are also some ordering issues.
        //
        // The DiagnosticsConfigurationHandler assumes that the TraceImpl.Listeners
        // collection exists (so it can initialize the DefaultTraceListener and
        // add/remove existing listeners).
        private static object InitOnce()
        {
            object d = null;
#if !NO_LOCK_FREE
            // The lock-free version
            if (listeners == null)
            {
                var c = new TraceListenerCollection();
                Thread.MemoryBarrier();
                while (Interlocked.CompareExchange(ref listeners, c, null) == null)
                {
                    // Read in the .config file and get the ball rolling...
                    // d = DiagnosticsConfiguration.Settings;
                }
                Thread.MemoryBarrier();
            }
#else
    // The lock version (saved for posterity and potential debugging)
			lock (lock_) {
				if (listeners == null) {
					listeners = new TraceListenerCollection ();
					// Read in the .config file and get the ball rolling...
					d = DiagnosticsConfiguration.Settings;
				}
			}
#endif
            return d;
        }

        // FIXME: According to MSDN, this method should display a dialog box
        public static void Assert(bool condition)
        {
            if (!condition)
                Fail(new StackTrace().ToString());
        }

        // FIXME: According to MSDN, this method should display a dialog box
        public static void Assert(bool condition, string message)
        {
            if (!condition)
                Fail(message);
        }

        // FIXME: According to MSDN, this method should display a dialog box
        public static void Assert(bool condition, string message, string detailMessage)
        {
            if (!condition)
                Fail(message, detailMessage);
        }

        public static void Close()
        {
            lock (ListenersSyncRoot)
            {
                foreach (TraceListener listener in Listeners)
                {
                    listener.Close();
                }
            }
        }

        // FIXME: From testing .NET, this method should display a dialog
        public static void Fail(string message)
        {
            lock (ListenersSyncRoot)
            {
                foreach (TraceListener listener in Listeners)
                {
                    listener.Fail(message);
                }
            }
        }

        // FIXME: From testing .NET, this method should display a dialog
        public static void Fail(string message, string detailMessage)
        {
            lock (ListenersSyncRoot)
            {
                foreach (TraceListener listener in Listeners)
                {
                    listener.Fail(message, detailMessage);
                }
            }
        }

        public static void Flush()
        {
            lock (ListenersSyncRoot)
            {
                foreach (TraceListener listener in Listeners)
                {
                    listener.Flush();
                }
            }
        }

        public static void Indent()
        {
            lock (ListenersSyncRoot)
            {
                foreach (TraceListener listener in Listeners)
                {
                    listener.IndentLevel++;
                }
            }
        }

        public static void Unindent()
        {
            lock (ListenersSyncRoot)
            {
                foreach (TraceListener listener in Listeners)
                {
                    listener.IndentLevel--;
                }
            }
        }

        public static void Write(object value)
        {
            lock (ListenersSyncRoot)
            {
                foreach (TraceListener listener in Listeners)
                {
                    listener.Write(value);

                    if (AutoFlush)
                        listener.Flush();
                }
            }
        }

        public static void Write(string message)
        {
            lock (ListenersSyncRoot)
            {
                foreach (TraceListener listener in Listeners)
                {
                    listener.Write(message);

                    if (AutoFlush)
                        listener.Flush();
                }
            }
        }

        public static void Write(object value, string category)
        {
            lock (ListenersSyncRoot)
            {
                foreach (TraceListener listener in Listeners)
                {
                    listener.Write(value, category);

                    if (AutoFlush)
                        listener.Flush();
                }
            }
        }

        public static void Write(string message, string category)
        {
            lock (ListenersSyncRoot)
            {
                foreach (TraceListener listener in Listeners)
                {
                    listener.Write(message, category);

                    if (AutoFlush)
                        listener.Flush();
                }
            }
        }

        public static void WriteIf(bool condition, object value)
        {
            if (condition)
                Write(value);
        }

        public static void WriteIf(bool condition, string message)
        {
            if (condition)
                Write(message);
        }

        public static void WriteIf(bool condition, object value, string category)
        {
            if (condition)
                Write(value, category);
        }

        public static void WriteIf(bool condition, string message, string category)
        {
            if (condition)
                Write(message, category);
        }

        public static void WriteLine(object value)
        {
            lock (ListenersSyncRoot)
            {
                foreach (TraceListener listener in Listeners)
                {
                    listener.WriteLine(value);

                    if (AutoFlush)
                        listener.Flush();
                }
            }
        }

        public static void WriteLine(string message)
        {
            lock (ListenersSyncRoot)
            {
                foreach (TraceListener listener in Listeners)
                {
                    listener.WriteLine(message);

                    if (AutoFlush)
                        listener.Flush();
                }
            }
        }

        public static void WriteLine(object value, string category)
        {
            lock (ListenersSyncRoot)
            {
                foreach (TraceListener listener in Listeners)
                {
                    listener.WriteLine(value, category);

                    if (AutoFlush)
                        listener.Flush();
                }
            }
        }

        public static void WriteLine(string message, string category)
        {
            lock (ListenersSyncRoot)
            {
                foreach (TraceListener listener in Listeners)
                {
                    listener.WriteLine(message, category);

                    if (AutoFlush)
                        listener.Flush();
                }
            }
        }

        public static void WriteLineIf(bool condition, object value)
        {
            if (condition)
                WriteLine(value);
        }

        public static void WriteLineIf(bool condition, string message)
        {
            if (condition)
                WriteLine(message);
        }

        public static void WriteLineIf(bool condition, object value, string category)
        {
            if (condition)
                WriteLine(value, category);
        }

        public static void WriteLineIf(bool condition, string message, string category)
        {
            if (condition)
                WriteLine(message, category);
        }
    }
}
#endif