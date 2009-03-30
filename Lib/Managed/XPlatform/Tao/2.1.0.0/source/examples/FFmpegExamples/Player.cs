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
using System.Threading;

namespace FFmpegExamples
{
    public partial class Player : Form
    {
        public Player()
        {
            InitializeComponent();
            this.audio.LivtUpdateEvent += new LiveUpdateCallback(audio_LivtUpdateEvent);
            
        }

        void audio_LivtUpdateEvent(object update)
        {
            if (InvokeRequired) {
                BeginInvoke(new TimerCallback(audio_LivtUpdateEvent), new object[] { update });
                return;
            }
            if (this.label1.Text != update.ToString())
                this.label1.Text = update.ToString();
        }

        AudioStream audio = new AudioStream();
        bool canPlay;

        private void button2_Click(object sender, EventArgs e)
        {
            if (DialogResult.OK == this.openFileDialog1.ShowDialog()) {
                string path = this.openFileDialog1.FileName;
                audio.Open(path);
                canPlay = true;
            }
        }

        Thread thread;
        private void button1_Click(object sender, EventArgs e)
        {
            thread = new Thread(new ThreadStart(RunMusic));
            thread.IsBackground = true;
            thread.Start();
        }

        private void RunMusic()
        {
            if (canPlay) {
                audio.Play();
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            audio.Stop();
        }
    }
}
