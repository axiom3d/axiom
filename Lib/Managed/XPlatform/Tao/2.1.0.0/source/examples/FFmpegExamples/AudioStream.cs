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
using System.Text;
//using Tao.OpenAl;
using System.Threading;
using System.Diagnostics;

namespace FFmpegExamples
{
    public class AudioStream
    {
        private const int MAX_BUFFERS = 10;
        private Decoder decoder;
        //private int source;
        private int[] buffers = new int[MAX_BUFFERS];
        //private float[] zeros = { 0.0f, 0.0f, 0.0f };
        private bool playing;

        public event LiveUpdateCallback LivtUpdateEvent;

        public AudioStream()
        {
            decoder = new Decoder();
            decoder.LivtUpdateEvent += new LiveUpdateCallback(decoder_LivtUpdateEvent);

            //Alut.alutInit();
            //Al.alGenBuffers(MAX_BUFFERS, buffers);
            Check();

            //Al.alGenSources(1,out source);
            Check();

            //Al.alSourcefv(source, Al.AL_POSITION, zeros);
            //Al.alSourcefv(source, Al.AL_VELOCITY, zeros);
            //Al.alSourcefv(source, Al.AL_DIRECTION, zeros);
            //Al.alSourcef(source, Al.AL_ROLLOFF_FACTOR, 0.0f);
            //Al.alSourcei(source, Al.AL_SOURCE_RELATIVE, Al.AL_TRUE);
        }

        void decoder_LivtUpdateEvent(object update)
        {
            if (LivtUpdateEvent != null)
                LivtUpdateEvent(update);
        }

        public bool Open(string path)
        {
            return decoder.Open(path);
        }

        public void Play()
        {
            //Thread t = new Thread(new ThreadStart(PlayFunc));
            //t.IsBackground = true;
            //t.Start();
            PlayFunc();
        }

        private bool Update()
        {
            int processed = 0;
            bool active = true;

            //Al.alGetSourcei(source, Al.AL_BUFFERS_PROCESSED, out processed);

            while (processed-- > 0) {
                int buffer = -1;

                //Al.alSourceUnqueueBuffers(source, 1, ref buffer);
                Check();

                active = Stream(buffer);

                if (active) {
                    //Al.alSourceQueueBuffers(source, 1, ref buffer);
                    Check();
                }
            }

            return active;
        }

        private void Check()
        {
            //int error = Al.alGetError();
            //if (error != Al.AL_NO_ERROR) {
            //    Debug.WriteLine("OpenAL error: " + Al.alGetString(error));
            //}
        }

        private bool Stream(int buffer)
        {
            if (decoder.Stream()) {
                if (decoder.IsAudioStream) {
                    //byte[] samples = decoder.Samples;
                    //int sampleSize = decoder.SampleSize;
                    //Al.alBufferData(buffer, decoder.Format, samples, sampleSize, decoder.Frequency);
                    Check();
                }
                return true;
            }
            return false;
        }

        public bool Playing()
        {
            //int state;
            //Al.alGetSourcei(source, Al.AL_SOURCE_STATE, out state);
            //return (state == Al.AL_PLAYING);
            return true;
        }

        public void Stop()
        {
            playing = false;
            //Al.alSourceStop(source);
        }

        public bool Playback()
        {
            int queue = 0;
            //Al.alGetSourcei(source, Al.AL_BUFFERS_QUEUED, out queue);

            if (queue > 0) {
                //Al.alSourcePlay(source);
            }

            if(Playing())
                return true;

            for(int i = 0; i < MAX_BUFFERS; ++i)
            {
                if (!Stream(buffers[i]))
                    return false;
            }

            //Al.alSourceQueueBuffers(source, MAX_BUFFERS, buffers);            

            return true;
        }

        private void PlayFunc()
        {
            playing = true;
            if (!Playback())
                throw new Exception("Refused to play");
            while (Update()) {
                if (!playing)
                    break;
                Thread.Sleep(10);
                if (!Playing()) {
                    Playback();
                }
            }
        }
    }
}
