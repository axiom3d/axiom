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
using System.IO;
using System.Runtime.InteropServices;
using Tao.FFmpeg;

namespace TaoMediaPlayer
{
    class MediaFile
    {
        #region Delegates
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate int url_open(IntPtr urlcontext, string filename, int flags);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate int url_read(IntPtr urlcontext, IntPtr buffer, int size);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate long url_seek(IntPtr urlcontext, long pos, int whence);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate int url_close(IntPtr urlcontext);
        #endregion

        #region Child Objects
        [StructLayout(LayoutKind.Sequential)]
        class URLProtocol
        {
            [MarshalAs(UnmanagedType.LPStr)]
            public string name;
            [MarshalAs(UnmanagedType.FunctionPtr)]
            public url_open openfunc;
            [MarshalAs(UnmanagedType.FunctionPtr)]
            public url_read readfunc;
            IntPtr writefunc;
            [MarshalAs(UnmanagedType.FunctionPtr)]
            public url_seek seekfunc;
            [MarshalAs(UnmanagedType.FunctionPtr)]
            public url_close closefunc;
            IntPtr next;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct URLContext
        {
            IntPtr protocol;
            int flags;
            int is_streamed;
            int max_packet_size;
            public IntPtr privdata;
            //[MarshalAs( UnmanagedType.LPStr )]
            //string filename;
        }
        #endregion

        #region Static Fields
        static URLProtocol protocol;
        static IntPtr streamprotocol;
        static Dictionary<string, Stream> streams;
        static int counter = 1;
        #endregion

        static MediaFile()
        {
            streams = new Dictionary<string, Stream>();

            // Create FFmpeg protocol for reading .NET streams
            protocol = new URLProtocol();
            protocol.openfunc = Open;
            protocol.readfunc = Read;
            protocol.seekfunc = Seek;
            protocol.closefunc = Close;
            protocol.name = "stream";
                        
            // Write protocol to pointer
            streamprotocol = Allocate<URLProtocol>();
            Marshal.StructureToPtr(protocol, streamprotocol, false);

            // Register protocols and codecs with FFmpeg
            FFmpeg.av_register_all();
            FFmpeg.register_protocol(streamprotocol);
        }

        #region URLProtocol functions
        static int Open(IntPtr pUrlcontext, string filename, int flags)
        {
            // Get URLContext structure from pointer
            URLContext urlcontext = PtrToStructure<URLContext>(pUrlcontext);

            Stream s;
            if (streams.TryGetValue(filename, out s))
            {
                // Set privdata to stream id
                urlcontext.privdata = (IntPtr)counter;
                // Register stream id
                streams.Remove(filename);
                streams.Add(counter.ToString(), s);
                ++counter;

                // Write new URLContext to pointer
                Marshal.StructureToPtr(urlcontext, pUrlcontext, false);
                return 0;
            }
            return 1;
        }

        static int Read(IntPtr pUrlcontext, IntPtr buffer, int size)
        {
            URLContext urlcontext = PtrToStructure<URLContext>(pUrlcontext);

            Stream s;
            if (streams.TryGetValue(urlcontext.privdata.ToString(), out s))
            {
                // Write bytes to temporary buffer
                byte[] temp = new byte[size];
                int count = s.Read(temp, 0, size);

                // Write buffer to pointer
                Marshal.Copy(temp, 0, buffer, count);
                return count;
            }
            return 0;
        }

        static long Seek(IntPtr pUrlcontext, long pos, int whence)
        {
            URLContext urlcontext = PtrToStructure<URLContext>(pUrlcontext);

            Stream s;
            if (streams.TryGetValue(urlcontext.privdata.ToString(), out s))
            {
                // Get SeekOrigin matching given whence
                SeekOrigin so = SeekOrigin.Current;
                switch (whence)
                {
                    case 0:
                        so = SeekOrigin.Begin;
                        break;
                    case 1:
                        so = SeekOrigin.Current;
                        break;
                    case 3:
                        so = SeekOrigin.End;
                        break;
                    case 0x10000:
                        return s.Length;
                }

                // Seek
                return (int)s.Seek(pos, so);
            }
            return 0;
        }

        static int Close(IntPtr pUrlcontext)
        {
            URLContext urlcontext = PtrToStructure<URLContext>(pUrlcontext);

            Stream s;
            if (streams.TryGetValue(urlcontext.privdata.ToString(), out s))
            {
                s.Close();
                streams.Remove(urlcontext.privdata.ToString());
                return 0;
            }
            return 1;
        }
        #endregion

        #region Fields
        IntPtr pFormatContext;
        Queue<FFmpeg.AVPacket> videoPacketQueue = new Queue<FFmpeg.AVPacket>();
        Queue<FFmpeg.AVPacket> audioPacketQueue = new Queue<FFmpeg.AVPacket>();
        FFmpeg.AVPacket vPacket;
        FFmpeg.AVPacket aPacket;
        IntPtr vFrame;

        bool hasAudio;
        int numChannels;
        int audioDepth;
        int frequency;
        FFmpeg.AVStream audioStream;
        double audioTimebase;

        bool hasVideo;
        int width;
        int height;
        FFmpeg.AVStream videoStream;
        double videoTimebase;
        FFmpeg.PixelFormat originalVideoFormat;
        object locker = new object();
        #endregion

        #region Properties
        public bool HasAudio
        { get { return hasAudio; } }

        public bool HasVideo
        { get { return hasVideo; } }

        public int Width
        { get { return width; } }

        public int Height
        { get { return height; } }

        public int NumChannels
        { get { return numChannels; } }

        public int AudioDepth
        { get { return audioDepth; } }

        public int Frequency
        { get { return frequency; } }
        #endregion

        public MediaFile(string path)
            : this(File.OpenRead(path))
        { }

        public MediaFile(Stream inStream)
        {
            // Create unique name
            string filename = "stream://" + counter;
            
            // Register stream
            streams.Add(filename, inStream);
            
            // Open stream with FFmpeg
            if (FFmpeg.av_open_input_file(out pFormatContext, filename, IntPtr.Zero, 0, IntPtr.Zero) < 0)
                throw new Exception("Unable to open stream");

            // Get context
            FFmpeg.AVFormatContext formatContext = PtrToStructure<FFmpeg.AVFormatContext>(pFormatContext);

            // Get stream info
            if (FFmpeg.av_find_stream_info(pFormatContext) < 0)
                throw new Exception("Unable to find stream info");

            // Loop through streams in this file
            for (int i = 0; i < formatContext.nb_streams; ++i)
            {
                FFmpeg.AVStream stream = PtrToStructure<FFmpeg.AVStream>(formatContext.streams[i]);
                FFmpeg.AVCodecContext codecContext = PtrToStructure<FFmpeg.AVCodecContext>(stream.codec);
                
                // Get codec
                IntPtr pCodec = FFmpeg.avcodec_find_decoder(codecContext.codec_id);
                FFmpeg.AVCodec codec = PtrToStructure<FFmpeg.AVCodec>(pCodec);
                if (pCodec == IntPtr.Zero)
                    continue;

                // Check codec type
                bool open = false;
                switch (codecContext.codec_type)
                {
                    case FFmpeg.CodecType.CODEC_TYPE_AUDIO:
                        // We only need 1 audio stream
                        if (hasAudio)
                            break;

                        // Get stream information
                        hasAudio = true;
                        numChannels = codecContext.channels;
                        audioDepth = (codecContext.sample_fmt == FFmpeg.SampleFormat.SAMPLE_FMT_U8) ? 8 : 16;
                        frequency = codecContext.sample_rate;
                        audioStream = stream;
                        audioTimebase = (double)stream.time_base.num / (double)stream.time_base.den;
                        
                        open = true;
                        break;

                    case FFmpeg.CodecType.CODEC_TYPE_VIDEO:
                        // We only need 1 video stream
                        if (hasVideo)
                            break;

                        // Set codec flags
                        if ((codec.capabilities & FFmpeg.CODEC_CAP_TRUNCATED) != 0)
                            codecContext.flags = codecContext.flags | FFmpeg.CODEC_FLAG_TRUNCATED;

                        // Get stream information
                        hasVideo = true;
                        width = codecContext.width;
                        height = codecContext.height;
                        videoStream = stream;
                        originalVideoFormat = codecContext.pix_fmt;
                        videoTimebase = (double)codecContext.time_base.num / (double)codecContext.time_base.den;
                                                
                        open = true;
                        break;
                }

                // Update codec context
                Marshal.StructureToPtr(codecContext, stream.codec, false);

                // Open codec
                if (open)
                    if(FFmpeg.avcodec_open(stream.codec, pCodec) < 0)
                        throw new Exception("Unable to open codec");
            }

            // No video or audio found
            if (!hasAudio && !hasVideo)
                throw new Exception("No codecs or streams found");
        }

        ~MediaFile()
        {
            FFmpeg.av_free(vFrame);

            foreach (FFmpeg.AVPacket packet in videoPacketQueue)
                FFmpeg.av_free_packet(packet.priv);
            foreach (FFmpeg.AVPacket packet in audioPacketQueue)
                FFmpeg.av_free_packet(packet.priv);

            if (videoStream.codec != IntPtr.Zero)
                FFmpeg.avcodec_close(videoStream.codec);

            FFmpeg.av_close_input_file(pFormatContext);
        }

        public void Rewind()
        {
            lock (locker)
            {
                if (hasVideo)
                {
                    FFmpeg.av_seek_frame(pFormatContext, videoStream.index, 0, 0);
                    FFmpeg.avcodec_flush_buffers(videoStream.codec);
                }

                if (hasAudio)
                {
                    FFmpeg.av_seek_frame(pFormatContext, audioStream.index, 0, 0);
                    FFmpeg.avcodec_flush_buffers(audioStream.codec);
                }

                videoPacketQueue.Clear();
                audioPacketQueue.Clear();

                vPacket = new FFmpeg.AVPacket();
                aPacket = new FFmpeg.AVPacket();

            }
        }

        public bool NextAudioFrame(IntPtr target, ref int targetsize, int minbuffer)
        {
            if (!hasAudio)
                return false;

            int byteswritten = 0;

            // Decode packets until we're satisfied
            while (true)
            {
                // If we need a new packet, get it
                if (aPacket.size == 0)
                {
                    if(aPacket.data != IntPtr.Zero)
                        FFmpeg.av_free_packet(aPacket.priv);
                }

                lock (locker)
                {
                    // If there are no more packets in the queue, read them from stream
                    while (audioPacketQueue.Count < 1)
                        if (!ReadPacket())
                        {
                            targetsize = byteswritten;
                            return false;
                        }
                    aPacket = audioPacketQueue.Dequeue();
                }

                // Decode packet
                int datasize = targetsize - byteswritten;
                int length =
                    FFmpeg.avcodec_decode_audio(audioStream.codec, target, ref datasize, aPacket.data, aPacket.size);

                if(length < 0)
                {
                    // Error, skip packet
                    aPacket.size = 0;
                    continue;
                }

                // Move forward in packet
                aPacket.size -= length;
                aPacket.data = new IntPtr(aPacket.data.ToInt64() + length);

                // Frame not finished yet
                if(datasize <= 0)
                    continue;

                // Move forward in target buffer
                target = new IntPtr(target.ToInt64() + datasize);
                byteswritten += datasize;

                // Load next frame when minimum buffer size is not reached
                if(byteswritten < minbuffer)
                    continue;

                break;
            }

            // Output buffer size
            targetsize = byteswritten;

            return true;
        }

        public bool NextVideoFrame(IntPtr target, FFmpeg.PixelFormat desiredFormat, ref double time)
        {
            if (!hasVideo)
                return false;

            int got_picture = 0;
            long pts = -1;
            
            // Allocate video frame
            vFrame = FFmpeg.avcodec_alloc_frame();

            // Decode packets until we've got a full frame
            while (got_picture == 0)
            {
                // If we need a new packet, get it
                if (vPacket.size <= 0)
                {
                    if (vPacket.data != IntPtr.Zero)
                        FFmpeg.av_free_packet(vPacket.priv);

                    lock (locker)
                    {
                        // If there are no more packets in the queue, read them from stream
                        while (videoPacketQueue.Count < 1)
                            if (!ReadPacket())
                                return false;

                        vPacket = videoPacketQueue.Dequeue();
                    }
                }

                // Do nothing if timing is too early
                if (pts == -1)
                {
                    pts = vPacket.pts;
                    if (pts * videoTimebase > time)
                    {
                        time = pts*videoTimebase - time;
                        return true;
                    }
                    time = 0;
                }

                // Decode packet
                int length = FFmpeg.avcodec_decode_video(videoStream.codec, vFrame, ref got_picture, vPacket.data, vPacket.size);

                // Error, skip packet
                if (length < 0)
                {
                    vPacket.size = 0;
                    continue;
                }

                // Move forward in packet
                vPacket.data = new IntPtr(vPacket.data.ToInt64() + length);
                vPacket.size -= length;
            }
       
            // Create RGB frame
            IntPtr rgbFrame = FFmpeg.avcodec_alloc_frame();
            FFmpeg.avpicture_fill(rgbFrame, target, (int)desiredFormat, width, height);

            // Convert video frame to RGB
            FFmpeg.img_convert(rgbFrame, (int)desiredFormat, vFrame, (int)originalVideoFormat, width, height);

            // Free memory
            FFmpeg.av_free(rgbFrame);
            FFmpeg.av_free(vFrame);

            return true;
        }

        bool ReadPacket()
        {
            // Allocate memory for packet
            IntPtr pPacket = Allocate<FFmpeg.AVPacket>();

            // Read next frame into packet
            if (FFmpeg.av_read_frame(pFormatContext, pPacket) < 0)
                return false;

            // Get packet from pointer
            FFmpeg.AVPacket packet = PtrToStructure<FFmpeg.AVPacket>(pPacket);
            packet.priv = pPacket;

            // If packet belongs to our video or audio stream, enqueue it
            if (hasVideo && packet.stream_index == videoStream.index)
                videoPacketQueue.Enqueue(packet);
            if (hasAudio && packet.stream_index == audioStream.index)
                audioPacketQueue.Enqueue(packet);

            return true;
        }

        static T PtrToStructure<T>(IntPtr pointer)
        {
            return (T)Marshal.PtrToStructure(pointer, typeof(T));
        }

        static IntPtr Allocate<T>()
        {
            return Marshal.AllocHGlobal(Marshal.SizeOf(typeof(T)));
        }
    }
}
