/*
Copyright (c) 2008 Tao Framework

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
 */

using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Tao.OpenAl;

namespace TaoMediaPlayer
{
    public partial class MainForm : Form
    {
        [STAThread]
        public static void Main (string[] args)
        {
            Application.Run(new MainForm());
        }

        MediaFile media;
        private delegate void VoidDelegate();

        bool paused = false;
        bool playing = false;
        Stopwatch timer = new Stopwatch();
        private AudioSource audio = new AudioSource();
        private int audioformat;
        ManualResetEvent audiosync = new ManualResetEvent(false);

        public MainForm()
        {
            InitializeComponent();
        }

        private void openbutton_Click(object sender, System.EventArgs e)
        {
            // Create file dialog
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.CheckFileExists = true;
            ofd.Multiselect = false;

            // Open file
            if(ofd.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                return;

            try
            {
                media = new MediaFile(ofd.FileName);
            }
            catch (TypeInitializationException tie)
            {
                throw new Exception("Unable to open mediafile, are the ffmpeg libraries available?", tie);
            }

            // Translate audioformat
            if (media.NumChannels == 1)
            {
                if (media.AudioDepth == 8)
                    audioformat = Al.AL_FORMAT_MONO8;
                else if (media.AudioDepth == 16)
                    audioformat = Al.AL_FORMAT_MONO16;
                else
                    throw new Exception("Unsupported audio bit depth");
            }
            else if (media.NumChannels == 2)
            {
                if (media.AudioDepth == 8)
                    audioformat = Al.AL_FORMAT_STEREO8;
                else if (media.AudioDepth == 16)
                    audioformat = Al.AL_FORMAT_STEREO16;
                else
                    throw new Exception("Unsupported audio bit depth");
            }
            else
            {
                throw new Exception("Unsupported amount of channels");
            }

            // Set frame size
            Size = new Size(media.Width, media.Height);
        }

        private void playbutton_Click(object sender, EventArgs e)
        {
            if (!playing)
            {
                // Start playing
                playing = true;

                if (media.HasVideo)
                    ThreadPool.QueueUserWorkItem(videoUpdater);
                if (media.HasAudio)
                {
                    ThreadPool.QueueUserWorkItem(audioUpdater);

                    // Wait until audio is buffered
                    audiosync.WaitOne();
                    audio.Play();
                }
                timer.Start();
                playbutton.Text = "Pause";
            }
            else if (playing && !paused)
            {
                // Pause
                if (media.HasAudio)
                    audio.Pause();

                timer.Stop();
                paused = true;
                playbutton.Text = "Play";
            }
            else if (playing && paused)
            {
                // Resume
                if (media.HasAudio)
                    audio.Play();

                timer.Start();
                paused = false;
                playbutton.Text = "Pause";
            }
        }

        private void stopbutton_Click(object sender, EventArgs e)
        {
            // Stop and rewind
            if (media != null & media.HasAudio)
                audio.Stop();

            playing = false;
            paused = false;

            timer.Reset();
            media.Rewind();

            playbutton.Text = "Play";
        }

        private void audioUpdater(object state)
        {
            // Notify audio api of the update thread
            audio.RegisterThread();

            // Allocate buffer
            IntPtr buffer = Marshal.AllocHGlobal(192000);

            try
            {
                while (playing)
                {
                    // Check if we have free audio buffers
                    if (audio.BufferFinished())
                    {
                        // Decode next audio frame
                        int buffersize = 192000;
                        bool rv = media.NextAudioFrame(buffer, ref buffersize, 20000);
                        if (!media.HasVideo)
                            playing = rv;

                        // Send audio frame to audio buffer
                        if (buffersize != 0)
                            audio.BufferData(buffer, buffersize, audioformat, media.Frequency);

                        audiosync.Set();

                        if (!rv)
                            break;
                    }

                    if (!audio.HasFreeBuffers())
                        Thread.Sleep(10);
                }

                audiosync.Reset();

                // Rewind when stopped
                if (!media.HasVideo)
                {
                    media.Rewind();
                    timer.Reset();
                }
            }
            finally
            {
                // Free buffer
                Marshal.FreeHGlobal(buffer);

                if (!media.HasVideo)
                    pictureBox.Invoke(
                        (VoidDelegate)delegate
                                           {
                                               playbutton.Text = "Play";
                                           });

            }
        }

        private void videoUpdater(object state)
        {
            // Create Bitmaps (double buffering)
            Bitmap[] bmp = new Bitmap[]
                {
                    new Bitmap(media.Width, media.Height, PixelFormat.Format24bppRgb),
                    new Bitmap(media.Width, media.Height, PixelFormat.Format24bppRgb)
                };
            int target = 0;

            try
            {
                while (playing)
                {
                    // Load next frame, or stop playing if eof
                    double time = (double)timer.ElapsedMilliseconds / 1000.0;
                    // Write to bitmap buffer
                    BitmapData bd =
                        bmp[target].LockBits(new Rectangle(0, 0, bmp[target].Width, bmp[target].Height), ImageLockMode.WriteOnly,
                                     PixelFormat.Format24bppRgb);
                    playing = media.NextVideoFrame(bd.Scan0, Tao.FFmpeg.FFmpeg.PixelFormat.PIX_FMT_BGR24, ref time);
                    bmp[target].UnlockBits(bd);

                    if(time == 0)
                    {
                        pictureBox.Invoke(
                            (VoidDelegate) delegate
                                               {
                                                   pictureBox.Image = bmp[target];
                                                   target = (target == 0) ? 1 : 0;
                                               });
                    }
                    if (playing && time > 0.005)
                    {
                        // Wait for next frame
                        Thread.Sleep((int)(time * 1000.0));
                    }

                    // Wait while we're paused
                    while (paused)
                        Thread.Sleep((int)((1.0 / 20.0) * 1000));
                }

                // Rewind to start when video ended
                media.Rewind();
                timer.Reset();
            }
            finally
            {
                // Free framebuffer, reset UI
                bmp[target].Dispose();
                pictureBox.Invoke(
                    (VoidDelegate) delegate
                                       {
                                           playbutton.Text = "Play";
                                       });
            }
        }

    }
}