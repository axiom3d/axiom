﻿#if (SILVERLIGHT || WINDOWS_PHONE || XBOX || PORTABLE)
//
// System.Diagnostics.TraceListener.cs
//
// Authors:
//   Jonathan Pryor (jonpryor@vt.edu)
//
// Comments from John R. Hicks <angryjohn69@nc.rr.com> original implementation 
// can be found at: /mcs/docs/apidocs/xml/en/System.Diagnostics
//
// (C) 2002 Jonathan Pryor
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

namespace System.Diagnostics
{
    public abstract class TraceListener : /*/MarshalByRefObject,/*/ IDisposable
    {
        [ThreadStatic]
        private int indentLevel;

        [ThreadStatic]
        private int indentSize = 4;

        private string name;
        private bool needIndent = true;

        protected TraceListener()
            : this("") { }

        protected TraceListener(string name)
        {
            Name = name;
        }

        public int IndentLevel
        {
            get { return indentLevel; }
            set { indentLevel = value; }
        }

        public int IndentSize
        {
            get { return indentSize; }
            set { indentSize = value; }
        }

        public virtual string Name
        {
            get { return name; }
            set { name = value; }
        }

        protected bool NeedIndent
        {
            get { return needIndent; }
            set { needIndent = value; }
        }

        public virtual void Close()
        {
            Dispose();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing) { }

        public virtual void Fail(string message)
        {
            Fail(message, "");
        }

        public virtual void Fail(string message, string detailMessage)
        {
            WriteLine("---- DEBUG ASSERTION FAILED ----");
            WriteLine("---- Assert Short Message ----");
            WriteLine(message);
            WriteLine("---- Assert Long Message ----");
            WriteLine(detailMessage);
            WriteLine("");
        }

        public virtual void Flush() { }

        public virtual void Write(object o)
        {
            Write(o.ToString());
        }

        public abstract void Write(string message);

        public virtual void Write(object o, string category)
        {
            Write(o.ToString(), category);
        }

        public virtual void Write(string message, string category)
        {
            Write(category + ": " + message);
        }

        protected virtual void WriteIndent()
        {
            // Must set NeedIndent to false before Write; otherwise, we get endless
            // recursion with Write->WriteIndent->Write->WriteIndent...*boom*
            NeedIndent = false;
            var indent = new String(' ', IndentLevel * IndentSize);
            Write(indent);
        }

        public virtual void WriteLine(object o)
        {
            WriteLine(o.ToString());
        }

        public abstract void WriteLine(string message);

        public virtual void WriteLine(object o, string category)
        {
            WriteLine(o.ToString(), category);
        }

        public virtual void WriteLine(string message, string category)
        {
            WriteLine(category + ": " + message);
        }
    }
}
#endif