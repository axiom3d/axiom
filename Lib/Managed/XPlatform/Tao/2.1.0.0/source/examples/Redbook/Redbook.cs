#region License
/*
MIT License
Copyright ©2003-2006 Tao Framework Team
http://www.taoframework.com
All rights reserved.

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/
#endregion License

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Reflection;

namespace Redbook
{
    public partial class Redbook : Form
    {
        public Redbook()
        {
            InitializeComponent();
        }

        private void frmExamples_Load(object sender, EventArgs e)
        {
            Type[] types = Assembly.GetExecutingAssembly().GetTypes();
            foreach (Type type in types)
            {
                MemberInfo[] runMethods = type.GetMember("Run");
                foreach (MemberInfo run in runMethods)
                {
                    lstExamples.Items.Add(type.Name);
                }
                if (lstExamples.Items.Count > 0)
                {
                    this.lstExamples.SelectedIndex = 0;
                }
            }
        }

        private void btnRun_Click(object sender, EventArgs e)
        {
            SelectExample();
        }

        private void SelectExample()
        {
            if (lstExamples.SelectedItem != null)
            {
                Type example = Assembly.GetExecutingAssembly().GetType("Redbook." + lstExamples.SelectedItem.ToString(), true, true);
                example.InvokeMember("Run", BindingFlags.InvokeMethod, null, null, null);
            }
        }

        private void lstExamples_SelectedIndexChanged(object sender, EventArgs e)
        {
            SelectExample();
        }
    }
}
