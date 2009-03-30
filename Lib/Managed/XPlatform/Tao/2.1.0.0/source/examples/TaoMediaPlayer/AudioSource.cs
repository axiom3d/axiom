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
using System.Collections.Generic;
using System.Threading;
using Tao.OpenAl;

namespace TaoMediaPlayer
{
    class AudioSource
    {
        private IntPtr device;
        private IntPtr context;
        private int source;
        private Queue<int> buffers = new Queue<int>();
        
        public AudioSource()
        {
            // Open default device
            device = Alc.alcOpenDevice(null);
            if (device == IntPtr.Zero)
                throw new Exception("Unable to open OpenAL device");

            // Create context
            context = Alc.alcCreateContext(device, IntPtr.Zero);
            if(context == IntPtr.Zero)
                throw new Exception("Unable to create OpenAL context");
            int rv = Alc.alcMakeContextCurrent(context);

            // Create buffers
            for (int i = 0; i < 8; ++i)
            {
                int b;
                Al.alGenBuffers(1, out b);
                buffers.Enqueue(b);
            }

            // Create source
            Al.alGenSources(1, out source);
        }

        ~AudioSource()
        {
            // Delete source
            Al.alDeleteSources(1, ref source);

            // Close device
            Alc.alcCloseDevice(device);
        }

        public void RegisterThread()
        {
            int rv = Alc.alcMakeContextCurrent(context);
        }

        public void BufferData(IntPtr data, int length, int format, int samplerate)
        {
            int buffer;
            lock (buffers)
            {
                // Get a free buffer
                if (buffers.Count == 0)
                    Monitor.Wait(buffers);

                buffer = buffers.Dequeue();
            }

            IntPtr t = Alc.alcGetCurrentContext();

            // Fill buffer
            Al.alBufferData(buffer, format, data, length, samplerate);

            // Queue buffer
            Al.alSourceQueueBuffers(source, 1, ref buffer);
        }

        public bool BufferFinished()
        {
            // Get number of finished buffers
            int processed;
            Al.alGetSourcei(source, Al.AL_BUFFERS_PROCESSED, out processed);

            if (processed > 0)
            {
                // Remove finished buffers from queue
                int[] finished = new int[processed];
                Al.alSourceUnqueueBuffers(source, processed, finished);

                // Add finished buffers to free queue
                lock (buffers)
                {
                    foreach (int i in finished)
                    {
                        buffers.Enqueue(i);
                        Monitor.Pulse(buffers);
                    }
                }
            }

            // Return if we have finished buffers
            lock (buffers)
            {
                return buffers.Count > 0;
            }
        }

        public bool HasFreeBuffers()
        {
            lock(buffers)
            {
                return buffers.Count > 0;
            }
        }

        public void Play()
        {
            Al.alSourcePlay(source);
        }

        public void Stop()
        {
            Al.alSourceStop(source);
        }

        public void Pause()
        {
            Al.alSourcePause(source);
        }
    }
}
