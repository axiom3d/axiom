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

/* Define all previous supported version numbers
this is a hack to be able to test #if VERSION < 52
by doing #if !VERSION_52 */
#if AVFORMAT_VERSION_52
    #define AVFORMAT_VERSION_51
#elif AVFORMAT_VERSION_51
    // nothing to do
#else
    #define AVFORMAT_VERSION_51
    #warning No version for avformat specified, defaulting to 51
#endif

using System;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Security;

namespace Tao.FFmpeg
{
    /// <summary>
    /// 
    /// </summary>
    public static partial class FFmpeg
    {
        #region Private Constants
        #region string AVFORMAT_NATIVE_LIBRARY
        /// <summary>
        ///     Specifies AVFORMAT's native library archive.
        /// </summary>
        /// <remarks>
        ///     Specifies avformat.dll everywhere; will be mapped via .config for mono.
        /// </remarks>
#if AVFORMAT_VERSION_52
        private const string AVFORMAT_NATIVE_LIBRARY = "avformat-52.dll";
#elif AVFORMAT_VERSION_51
        private const string AVFORMAT_NATIVE_LIBRARY = "avformat-51.dll";
#endif
        #endregion string AVFORMAT_NATIVE_LIBRARY
        #endregion Private Constants

        /// <summary>
        /// 
        /// </summary>
        /// <param name="protocol"></param>
        /// <returns></returns>
        [DllImport(AVFORMAT_NATIVE_LIBRARY, CallingConvention = CALLING_CONVENTION), SuppressUnmanagedCodeSecurity]
        public static extern int register_protocol(IntPtr protocol);

        /// <summary>
        ///
        /// </summary>
        /// <param name="pAVPacket"></param>
        [DllImport(AVFORMAT_NATIVE_LIBRARY, CallingConvention = CALLING_CONVENTION), SuppressUnmanagedCodeSecurity]
        public static extern void av_destruct_packet_nofree(IntPtr pAVPacket);

        /// <summary>
        ///
        /// </summary>
        /// <param name="pAVPacket"></param>
        [DllImport(AVFORMAT_NATIVE_LIBRARY, CallingConvention = CALLING_CONVENTION), SuppressUnmanagedCodeSecurity]
        public static extern void av_destruct_packet(IntPtr pAVPacket);

        /// <summary>
        /// Initialize optional fields of a packet.
        /// </summary>
        /// <param name="pAVPacket"></param>
        [DllImport(AVFORMAT_NATIVE_LIBRARY, CallingConvention = CALLING_CONVENTION), SuppressUnmanagedCodeSecurity]
        public static extern void av_init_packet(IntPtr pAVPacket);

        /// <summary>
        ///
        /// </summary>
        /// <param name="pAVPacket"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        [DllImport(AVFORMAT_NATIVE_LIBRARY, CallingConvention = CALLING_CONVENTION), SuppressUnmanagedCodeSecurity]
        public static extern int av_new_packet(IntPtr pAVPacket, int size);

        /// <summary>
        ///
        /// </summary>
        /// <param name="pByteIOContext"></param>
        /// <param name="pAVPacket"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        [DllImport(AVFORMAT_NATIVE_LIBRARY, CallingConvention = CALLING_CONVENTION), SuppressUnmanagedCodeSecurity]
        public static extern int av_get_packet(IntPtr pByteIOContext, IntPtr pAVPacket, int size);

        /// <summary>
        ///
        /// </summary>
        /// <param name="pAVPacket"></param>
        /// <returns></returns>
        [DllImport(AVFORMAT_NATIVE_LIBRARY, CallingConvention = CALLING_CONVENTION), SuppressUnmanagedCodeSecurity]
        public static extern int av_dup_packet(IntPtr pAVPacket);

        /// <summary>
        ///
        /// </summary>
        /// <param name="pAVPacket"></param>
        // av_free_packet is internal, so reimplemented in managed code
        public static void av_free_packet(IntPtr pAVPacket)
        {
            if(pAVPacket == IntPtr.Zero)
                return;

            AVPacket packet = (AVPacket) Marshal.PtrToStructure(pAVPacket, typeof (AVPacket));
            if (packet.destruct == null)
                return;

            packet.destruct(pAVPacket);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="pAVImageFormat"></param>
        [DllImport(AVFORMAT_NATIVE_LIBRARY, CallingConvention = CALLING_CONVENTION), SuppressUnmanagedCodeSecurity]
        public static extern void av_register_image_format(IntPtr pAVImageFormat);

        /// <summary>
        ///
        /// </summary>
        /// <param name="pAVProbeData"></param>
        /// <returns></returns>
        [DllImport(AVFORMAT_NATIVE_LIBRARY, CallingConvention = CALLING_CONVENTION), SuppressUnmanagedCodeSecurity]
        public static extern IntPtr av_probe_image_format(IntPtr pAVProbeData);

        /// <summary>
        ///
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        [DllImport(AVFORMAT_NATIVE_LIBRARY, CallingConvention = CALLING_CONVENTION), SuppressUnmanagedCodeSecurity]
        public static extern IntPtr guess_image_format([MarshalAs(UnmanagedType.LPTStr)]String filename);

        /// <summary>
        ///
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        [DllImport(AVFORMAT_NATIVE_LIBRARY, CallingConvention = CALLING_CONVENTION), SuppressUnmanagedCodeSecurity]
        public static extern CodecID av_guess_image2_codec([MarshalAs(UnmanagedType.LPTStr)]
String filename);

        /// <summary>
        ///
        /// </summary>
        /// <param name="pByteIOContext"></param>
        /// <param name="filename"></param>
        /// <param name="pAVImageFormat"></param>
        /// <param name="alloc_cb"></param>
        /// <param name="opaque"></param>
        /// <returns></returns>
        [DllImport(AVFORMAT_NATIVE_LIBRARY, CallingConvention = CALLING_CONVENTION), SuppressUnmanagedCodeSecurity]
        public static extern int av_read_image(IntPtr pByteIOContext,
        [MarshalAs(UnmanagedType.LPTStr)]String filename,
        IntPtr pAVImageFormat,
        [MarshalAs(UnmanagedType.FunctionPtr)]
AllocCBCallback alloc_cb,
        IntPtr opaque);

        /// <summary>
        ///
        /// </summary>
        /// <param name="pByteIOContext"></param>
        /// <param name="pAVImageFormat"></param>
        /// <param name="pAVImageInfo"></param>
        /// <returns></returns>
        [DllImport(AVFORMAT_NATIVE_LIBRARY, CallingConvention = CALLING_CONVENTION), SuppressUnmanagedCodeSecurity]
        public static extern int av_write_image(IntPtr pByteIOContext, IntPtr pAVImageFormat, IntPtr pAVImageInfo);

        /// <summary>
        ///
        /// </summary>
        /// <param name="pAVInputFormat"></param>
        [DllImport(AVFORMAT_NATIVE_LIBRARY, CallingConvention = CALLING_CONVENTION), SuppressUnmanagedCodeSecurity]
        public static extern void av_register_input_format(IntPtr pAVInputFormat);

        /// <summary>
        ///
        /// </summary>
        /// <param name="pAVOutputFormat"></param>
        [DllImport(AVFORMAT_NATIVE_LIBRARY, CallingConvention = CALLING_CONVENTION), SuppressUnmanagedCodeSecurity]
        public static extern void av_register_output_format(IntPtr pAVOutputFormat);

        /// <summary>
        ///
        /// </summary>
        /// <param name="short_name"></param>
        /// <param name="filename"></param>
        /// <param name="mime_type"></param>
        /// <returns></returns>
        [DllImport(AVFORMAT_NATIVE_LIBRARY, CallingConvention = CALLING_CONVENTION), SuppressUnmanagedCodeSecurity]
        public static extern IntPtr guess_stream_format([MarshalAs(UnmanagedType.LPTStr)]
String short_name,
        [MarshalAs(UnmanagedType.LPTStr)]
String filename,
        [MarshalAs(UnmanagedType.LPTStr)]
String mime_type);

        /// <summary>
        ///
        /// </summary>
        /// <param name="short_name"></param>
        /// <param name="filename"></param>
        /// <param name="mime_type"></param>
        /// <returns>AVOutputFormat pointer</returns>
        [DllImport(AVFORMAT_NATIVE_LIBRARY, CallingConvention = CALLING_CONVENTION), SuppressUnmanagedCodeSecurity]
        public static extern IntPtr guess_format([MarshalAs(UnmanagedType.LPTStr)]
String short_name,
        [MarshalAs(UnmanagedType.LPTStr)]
String filename,
        [MarshalAs(UnmanagedType.LPTStr)]
String mime_type);

        /// <summary>
        ///
        /// </summary>
        /// <param name="pAVOutoutFormat"></param>
        /// <param name="short_name"></param>
        /// <param name="filename"></param>
        /// <param name="mime_type"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        [DllImport(AVFORMAT_NATIVE_LIBRARY, CallingConvention = CALLING_CONVENTION), SuppressUnmanagedCodeSecurity]
        public static extern CodecID av_guess_codec(IntPtr pAVOutoutFormat,
        [MarshalAs(UnmanagedType.LPTStr)]
String short_name,
        [MarshalAs(UnmanagedType.LPTStr)]
String filename,
        [MarshalAs(UnmanagedType.LPTStr)]
String mime_type,
        CodecType type);

        /// <summary>
        ///
        /// </summary>
        /// <param name="pFile"></param>
        /// <param name="buf"></param>
        /// <param name="size"></param>
        [DllImport(AVFORMAT_NATIVE_LIBRARY, CallingConvention = CALLING_CONVENTION), SuppressUnmanagedCodeSecurity]
        public static extern void av_hex_dump(IntPtr pFile, IntPtr buf, int size);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pFile"></param>
        /// <param name="pAVPacket"></param>
        /// <param name="dump_payload"></param>
        [DllImport(AVFORMAT_NATIVE_LIBRARY, CallingConvention = CALLING_CONVENTION), SuppressUnmanagedCodeSecurity]
        public static extern void av_pkt_dump(IntPtr pFile, IntPtr pAVPacket, int dump_payload);

        /// <summary>
        ///
        /// </summary>
        [DllImport(AVFORMAT_NATIVE_LIBRARY, CallingConvention = CALLING_CONVENTION), SuppressUnmanagedCodeSecurity]
        public static extern void av_register_all();


        /* media file input */
        /// <summary>
        ///
        /// </summary>
        /// <param name="short_name"></param>
        /// <returns>AVInputFormat pointer</returns>
        [DllImport(AVFORMAT_NATIVE_LIBRARY, CallingConvention = CALLING_CONVENTION), SuppressUnmanagedCodeSecurity]
        public static extern IntPtr av_find_input_format([MarshalAs(UnmanagedType.LPTStr)]String short_name);

        /// <summary>
        ///
        /// </summary>
        /// <param name="pAVProbeData"></param>
        /// <param name="is_opened"></param>
        /// <returns>AVInputFormat pointer</returns>
        [DllImport(AVFORMAT_NATIVE_LIBRARY, CallingConvention = CALLING_CONVENTION), SuppressUnmanagedCodeSecurity]
        public static extern IntPtr av_probe_input_format(IntPtr pAVProbeData, int is_opened);

        /// <summary>
        ///
        /// </summary>
        /// <param name="pFormatContext"></param>
        /// <param name="filename"></param>
        /// <param name="pAVInputFormat"></param>
        /// <param name="buf_size"></param>
        /// <param name="pAVFormatParameters"></param>
        /// <returns></returns>
        [DllImport(AVFORMAT_NATIVE_LIBRARY, CallingConvention = CALLING_CONVENTION), SuppressUnmanagedCodeSecurity]
        public static extern int av_open_input_file([Out]out IntPtr pFormatContext,
        [MarshalAs(UnmanagedType.LPStr)]String filename,
        IntPtr pAVInputFormat, int buf_size, IntPtr pAVFormatParameters);

        /// <summary>
        ///
        /// </summary>
        /// <returns>AVFormatContext pointer</returns>
        [DllImport(AVFORMAT_NATIVE_LIBRARY, CallingConvention = CALLING_CONVENTION), SuppressUnmanagedCodeSecurity]
        public static extern IntPtr av_alloc_format_context();

        /// <summary>
        ///
        /// </summary>
        /// <param name="pAVFormatContext"></param>
        /// <returns></returns>
        [DllImport(AVFORMAT_NATIVE_LIBRARY, CallingConvention = CALLING_CONVENTION), SuppressUnmanagedCodeSecurity]
        public static extern int av_find_stream_info(IntPtr pAVFormatContext);

        /// <summary>
        ///
        /// </summary>
        /// <param name="pAVFormatContext"></param>
        /// <param name="pAVPacket"></param>
        /// <returns></returns>
        [DllImport(AVFORMAT_NATIVE_LIBRARY, CallingConvention = CALLING_CONVENTION), SuppressUnmanagedCodeSecurity]
        public static extern int av_read_packet(IntPtr pAVFormatContext, IntPtr pAVPacket);

        /// <summary>
        ///
        /// </summary>
        /// <param name="pAVFormatContext"></param>
        /// <param name="pAVPacket"></param>
        /// <returns></returns>
        [DllImport(AVFORMAT_NATIVE_LIBRARY, CallingConvention = CALLING_CONVENTION), SuppressUnmanagedCodeSecurity]
        public static extern int av_read_frame(IntPtr pAVFormatContext, IntPtr pAVPacket);

        /// <summary>
        ///
        /// </summary>
        /// <param name="pAVFormatContext"></param>
        /// <param name="stream_index"></param>
        /// <param name="timestamp"></param>
        /// <param name="flags"></param>
        /// <returns></returns>
        [DllImport(AVFORMAT_NATIVE_LIBRARY, CallingConvention = CALLING_CONVENTION), SuppressUnmanagedCodeSecurity]
        public static extern int av_seek_frame(IntPtr pAVFormatContext, int stream_index, Int64 timestamp, int flags);

        /// <summary>
        ///
        /// </summary>
        /// <param name="pAVFormatContext"></param>
        /// <returns></returns>
        [DllImport(AVFORMAT_NATIVE_LIBRARY, CallingConvention = CALLING_CONVENTION), SuppressUnmanagedCodeSecurity]
        public static extern int av_read_play(IntPtr pAVFormatContext);

        /// <summary>
        ///
        /// </summary>
        /// <param name="pAVFormatContext"></param>
        /// <returns></returns>
        [DllImport(AVFORMAT_NATIVE_LIBRARY, CallingConvention = CALLING_CONVENTION), SuppressUnmanagedCodeSecurity]
        public static extern int av_read_pause(IntPtr pAVFormatContext);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pAVFormatContext"></param>
        [DllImport(AVFORMAT_NATIVE_LIBRARY, CallingConvention = CALLING_CONVENTION), SuppressUnmanagedCodeSecurity]
        public static extern void av_close_input_stream(IntPtr pAVFormatContext);

        /// <summary>
        ///
        /// </summary>
        /// <param name="pAVFormatContext"></param>
        [DllImport(AVFORMAT_NATIVE_LIBRARY, CallingConvention = CALLING_CONVENTION), SuppressUnmanagedCodeSecurity]
        public static extern void av_close_input_file(IntPtr pAVFormatContext);

        /// <summary>
        ///
        /// </summary>
        /// <param name="pAVFormatContext"></param>
        /// <param name="id"></param>
        /// <returns>AVStream pointer</returns>
        [DllImport(AVFORMAT_NATIVE_LIBRARY, CallingConvention = CALLING_CONVENTION), SuppressUnmanagedCodeSecurity]
        public static extern IntPtr av_new_stream(IntPtr pAVFormatContext, int id);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pAVFormatContext"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        [DllImport(AVFORMAT_NATIVE_LIBRARY, CallingConvention = CALLING_CONVENTION), SuppressUnmanagedCodeSecurity]
        public static extern IntPtr av_new_program(IntPtr pAVFormatContext, int id);

        /// <summary>
        ///
        /// </summary>
        /// <param name="pAVStream"></param>
        /// <param name="pts_wrap_bits"></param>
        /// <param name="pts_num"></param>
        /// <param name="pts_den"></param>
        [DllImport(AVFORMAT_NATIVE_LIBRARY, CallingConvention = CALLING_CONVENTION), SuppressUnmanagedCodeSecurity]
        public static extern void av_set_pts_info(IntPtr pAVStream, int pts_wrap_bits, int pts_num, int pts_den);

        /// <summary>
        ///
        /// </summary>
        /// <param name="pAVFormatContext"></param>
        /// <returns></returns>
        [DllImport(AVFORMAT_NATIVE_LIBRARY, CallingConvention = CALLING_CONVENTION), SuppressUnmanagedCodeSecurity]
        public static extern int av_find_default_stream_index(IntPtr pAVFormatContext);

        /// <summary>
        ///
        /// </summary>
        /// <param name="pAVStream"></param>
        /// <param name="timestamp"></param>
        /// <param name="flags"></param>
        /// <returns></returns>
        [DllImport(AVFORMAT_NATIVE_LIBRARY, CallingConvention = CALLING_CONVENTION), SuppressUnmanagedCodeSecurity]
        public static extern int av_index_search_timestamp(IntPtr pAVStream, Int64 timestamp, int flags);

        /// <summary>
        ///
        /// </summary>
        /// <param name="pAVStream"></param>
        /// <param name="pos"></param>
        /// <param name="timestamp"></param>
        /// <param name="size"></param>
        /// <param name="distance"></param>
        /// <param name="flags"></param>
        /// <returns></returns>
        [DllImport(AVFORMAT_NATIVE_LIBRARY, CallingConvention = CALLING_CONVENTION), SuppressUnmanagedCodeSecurity]
        public static extern int av_add_index_entry(IntPtr pAVStream, Int64 pos, Int64 timestamp, int size, int distance, int flags);

        /// <summary>
        ///
        /// </summary>
        /// <param name="pAVFormatContext"></param>
        /// <param name="stream_index"></param>
        /// <param name="target_ts"></param>
        /// <param name="flags"></param>
        /// <returns></returns>
        [DllImport(AVFORMAT_NATIVE_LIBRARY, CallingConvention = CALLING_CONVENTION), SuppressUnmanagedCodeSecurity]
        public static extern int av_seek_frame_binary(IntPtr pAVFormatContext, int stream_index, Int64 target_ts, int flags);

        /// <summary>
        ///
        /// </summary>
        /// <param name="pAVFormatContext"></param>
        /// <param name="pAVStream"></param>
        /// <param name="timestamp"></param>
        [DllImport(AVFORMAT_NATIVE_LIBRARY, CallingConvention = CALLING_CONVENTION), SuppressUnmanagedCodeSecurity]
        public static extern void av_update_cur_dts(IntPtr pAVFormatContext, IntPtr pAVStream, Int64 timestamp);

        /* media file output */

        /// <summary>
        ///
        /// </summary>
        /// <param name="pAVFormatContext"></param>
        /// <param name="pAVFormatParameters"></param>
        /// <returns></returns>
        [DllImport(AVFORMAT_NATIVE_LIBRARY, CallingConvention = CALLING_CONVENTION), SuppressUnmanagedCodeSecurity]
        public static extern int av_set_parameters(IntPtr pAVFormatContext, IntPtr pAVFormatParameters);

        /// <summary>
        ///
        /// </summary>
        /// <param name="pAVFormatContext"></param>
        /// <returns></returns>
        [DllImport(AVFORMAT_NATIVE_LIBRARY, CallingConvention = CALLING_CONVENTION), SuppressUnmanagedCodeSecurity]
        public static extern int av_write_header(IntPtr pAVFormatContext);

        /// <summary>
        ///
        /// </summary>
        /// <param name="pAVFormatContext"></param>
        /// <param name="pAVPacket"></param>
        /// <returns></returns>
        [DllImport(AVFORMAT_NATIVE_LIBRARY, CallingConvention = CALLING_CONVENTION), SuppressUnmanagedCodeSecurity]
        public static extern int av_write_frame(IntPtr pAVFormatContext, IntPtr pAVPacket);

        /// <summary>
        ///
        /// </summary>
        /// <param name="pAVFormatContext"></param>
        /// <param name="pAVPacket"></param>
        /// <returns></returns>
        [DllImport(AVFORMAT_NATIVE_LIBRARY, CallingConvention = CALLING_CONVENTION), SuppressUnmanagedCodeSecurity]
        public static extern int av_interleaved_write_frame(IntPtr pAVFormatContext, IntPtr pAVPacket);

        /// <summary>
        ///
        /// </summary>
        /// <param name="pAVFormatContext"></param>
        /// <param name="p_out_AVPacket"></param>
        /// <param name="pAVPacket"></param>
        /// <param name="flush"></param>
        /// <returns></returns>
        [DllImport(AVFORMAT_NATIVE_LIBRARY, CallingConvention = CALLING_CONVENTION), SuppressUnmanagedCodeSecurity]
        public static extern int av_interleave_packet_per_dts(IntPtr pAVFormatContext, out IntPtr p_out_AVPacket, IntPtr pAVPacket, int flush);

        /// <summary>
        ///
        /// </summary>
        /// <param name="pAVFormatContext"></param>
        /// <returns></returns>
        [DllImport(AVFORMAT_NATIVE_LIBRARY, CallingConvention = CALLING_CONVENTION), SuppressUnmanagedCodeSecurity]
        public static extern int av_write_trailer(IntPtr pAVFormatContext);

        /// <summary>
        ///
        /// </summary>
        /// <param name="pAVFormatContext"></param>
        /// <param name="index"></param>
        /// <param name="url"></param>
        /// <param name="is_output"></param>
        [DllImport(AVFORMAT_NATIVE_LIBRARY, CallingConvention = CALLING_CONVENTION), SuppressUnmanagedCodeSecurity]
        public static extern void dump_format(IntPtr pAVFormatContext, int index,
        [MarshalAs(UnmanagedType.LPTStr)]
String url,
        int is_output);

        /// <summary>
        ///
        /// </summary>
        /// <param name="width_ptr"></param>
        /// <param name="height_ptr"></param>
        /// <param name="arg"></param>
        /// <returns></returns>
        [DllImport(AVFORMAT_NATIVE_LIBRARY, CallingConvention = CALLING_CONVENTION), SuppressUnmanagedCodeSecurity]
        public static extern int parse_image_size(IntPtr width_ptr, IntPtr height_ptr,
        [MarshalAs(UnmanagedType.LPTStr)]String arg);

        /// <summary>
        ///
        /// </summary>
        /// <param name="pFrame_rate"></param>
        /// <param name="pFrame_rate_base"></param>
        /// <param name="arg"></param>
        /// <returns></returns>
        [DllImport(AVFORMAT_NATIVE_LIBRARY, CallingConvention = CALLING_CONVENTION), SuppressUnmanagedCodeSecurity]
        public static extern int parse_frame_rate(IntPtr pFrame_rate, IntPtr pFrame_rate_base,
        [MarshalAs(UnmanagedType.LPTStr)]String arg);

        /// <summary>
        ///
        /// </summary>
        /// <param name="datestr"></param>
        /// <param name="duration"></param>
        /// <returns></returns>
        [DllImport(AVFORMAT_NATIVE_LIBRARY, CallingConvention = CALLING_CONVENTION), SuppressUnmanagedCodeSecurity]
        public static extern Int64 parse_date([MarshalAs(UnmanagedType.LPTStr)]String datestr, int duration);

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        [DllImport(AVFORMAT_NATIVE_LIBRARY, CallingConvention = CALLING_CONVENTION), SuppressUnmanagedCodeSecurity]
        public static extern Int64 av_gettime();

        /// <summary>
        ///
        /// </summary>
        /// <param name="fd"></param>
        /// <returns></returns>
        [DllImport(AVFORMAT_NATIVE_LIBRARY, CallingConvention = CALLING_CONVENTION), SuppressUnmanagedCodeSecurity]
        public static extern Int64 ffm_read_write_index(int fd);

        /// <summary>
        ///
        /// </summary>
        /// <param name="fd"></param>
        /// <param name="pos"></param>
        [DllImport(AVFORMAT_NATIVE_LIBRARY, CallingConvention = CALLING_CONVENTION), SuppressUnmanagedCodeSecurity]
        public static extern void ffm_write_write_index(int fd, Int64 pos);

        /// <summary>
        ///
        /// </summary>
        /// <param name="pAVFormatContext"></param>
        /// <param name="pos"></param>
        /// <param name="file_size"></param>
        [DllImport(AVFORMAT_NATIVE_LIBRARY, CallingConvention = CALLING_CONVENTION), SuppressUnmanagedCodeSecurity]
        public static extern void ffm_set_write_index(IntPtr pAVFormatContext, Int64 pos, Int64 file_size);

        /// <summary>
        ///
        /// </summary>
        /// <param name="arg"></param>
        /// <param name="arg_size"></param>
        /// <param name="tag1"></param>
        /// <param name="info"></param>
        /// <returns></returns>
        [DllImport(AVFORMAT_NATIVE_LIBRARY, CallingConvention = CALLING_CONVENTION), SuppressUnmanagedCodeSecurity]
        public static extern int find_info_tag([MarshalAs(UnmanagedType.LPTStr)]String arg,
        int arg_size,
        [MarshalAs(UnmanagedType.LPTStr)]String tag1,
        [MarshalAs(UnmanagedType.LPTStr)]String info);

        /// <summary>
        ///
        /// </summary>
        /// <param name="buf"></param>
        /// <param name="buf_size"></param>
        /// <param name="path"></param>
        /// <param name="number"></param>
        /// <returns></returns>
        [DllImport(AVFORMAT_NATIVE_LIBRARY, CallingConvention = CALLING_CONVENTION), SuppressUnmanagedCodeSecurity]
        public static extern int av_get_frame_filename(IntPtr buf, int buf_size,
        [MarshalAs(UnmanagedType.LPTStr)]String path, int number);

        /// <summary>
        ///
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        [DllImport(AVFORMAT_NATIVE_LIBRARY, CallingConvention = CALLING_CONVENTION), SuppressUnmanagedCodeSecurity]
        public static extern int av_filename_number_test([MarshalAs(UnmanagedType.LPTStr)]String filename);

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        [DllImport(AVFORMAT_NATIVE_LIBRARY, CallingConvention = CALLING_CONVENTION), SuppressUnmanagedCodeSecurity]
        public static extern int video_grab_init();

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        [DllImport(AVFORMAT_NATIVE_LIBRARY, CallingConvention = CALLING_CONVENTION), SuppressUnmanagedCodeSecurity]
        public static extern int audio_init();

        /* DV1394 */
        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        [DllImport(AVFORMAT_NATIVE_LIBRARY, CallingConvention = CALLING_CONVENTION), SuppressUnmanagedCodeSecurity]
        public static extern int dv1394_init();

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        [DllImport(AVFORMAT_NATIVE_LIBRARY, CallingConvention = CALLING_CONVENTION), SuppressUnmanagedCodeSecurity]
        public static extern int dc1394_init();


        // *********************************************************************************
        // Constants
        // *********************************************************************************

        /// <summary>
        /// 
        /// </summary>
        public const int AV_TIME_BASE = 1000000;

        /// <summary>
        /// 
        /// </summary>
        public const int AVFMT_INFINITEOUTPUTLOOP = 0;

        /// <summary>
        /// 
        /// </summary>
        [CLSCompliant(false)]
        public const uint AVFMT_FLAG_GENPTS = 0x0001;

        /// <summary>
        /// 
        /// </summary>
        [CLSCompliant(false)]
        public const uint AVFMT_FLAG_IGNIDX = 0x0002;

        /// <summary>
        /// 
        /// </summary>
        [CLSCompliant(false)]
        public const int AVFMT_NOOUTPUTLOOP = -1;

        // no file should be opened
        /// <summary>
        /// 
        /// </summary>
        [CLSCompliant(false)]
        public const uint AVFMT_NOFILE = 0x0001;

        // needs '%d' in filename
        /// <summary>
        /// 
        /// </summary>
        [CLSCompliant(false)]
        public const uint AVFMT_NEEDNUMBER = 0x0002;

        // show format stream IDs numbers
        /// <summary>
        /// 
        /// </summary>
        [CLSCompliant(false)]
        public const uint AVFMT_SHOW_IDS = 0x0008;

        // format wants AVPicture structure for raw picture data
        /// <summary>
        /// 
        /// </summary>
        [CLSCompliant(false)]
        public const uint AVFMT_RAWPICTURE = 0x0020;

        // format wants global header
        /// <summary>
        /// 
        /// </summary>
        [CLSCompliant(false)]
        public const uint AVFMT_GLOBALHEADER = 0x0040;

        // format doesnt need / has any timestamps
        /// <summary>
        /// 
        /// </summary>
        [CLSCompliant(false)]
        public const uint AVFMT_NOTIMESTAMPS = 0x0080;

        // AVImageFormat.flags field constants
        /// <summary>
        /// 
        /// </summary>
        [CLSCompliant(false)]
        public const uint AVIMAGE_INTERLEAVED = 0x0001;

        /// <summary>
        /// 
        /// </summary>
        public const int AVPROBE_SCORE_MAX = 100;

        /// <summary>
        /// 
        /// </summary>
        public const int PKT_FLAG_KEY = 0x0001;

        /// <summary>
        /// 
        /// </summary>
        public const int AVINDEX_KEYFRAME = 0x001;

        /// <summary>
        /// 
        /// </summary>
        public const int MAX_REORDER_DELAY = 4;

        /// <summary>
        /// 
        /// </summary>
        [CLSCompliant(false)]
        public const uint AVFMTCTX_NOHEADER = 0x001;
        /// <summary>
        /// 
        /// </summary>
        public const int MAX_STREAMS = 20;

        /// <summary>
        /// 
        /// </summary>
        public const int AVERROR_UNKNOWN = -1;
        /// <summary>
        /// 
        /// </summary>
        public const int AVERROR_IO = -2;
        /// <summary>
        /// 
        /// </summary>
        public const int AVERROR_NUMEXPECTED = -3;
        /// <summary>
        /// 
        /// </summary>
        public const int AVERROR_INVALIDDATA = -4;
        /// <summary>
        /// 
        /// </summary>
        public const int AVERROR_NOMEM = -5;
        /// <summary>
        /// 
        /// </summary>
        public const int AVERROR_NOFMT = -6;
        /// <summary>
        /// 
        /// </summary>
        public const int AVERROR_NOTSUPP = -7;

        /// <summary>
        /// 
        /// </summary>
        public const int AVSEEK_FLAG_BACKWARD = 1;
        /// <summary>
        /// 
        /// </summary>
        public const int AVSEEK_FLAG_BYTE = 2;
        /// <summary>
        /// 
        /// </summary>
        public const int AVSEEK_FLAG_ANY = 4;

        /// <summary>
        /// 
        /// </summary>
        public const int FFM_PACKET_SIZE = 4096;

        // *********************************************************************************
        // Constants
        // *********************************************************************************

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pAVPacket"></param>
        public delegate void DestructCallback(IntPtr pAVPacket);

        /// <summary>
        /// 
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct AVPacket
        {
            /// <summary>
            /// 
            /// </summary>
            [MarshalAs(UnmanagedType.I8)]
            public Int64 pts; // presentation time stamp in time_base units

            /// <summary>
            /// 
            /// </summary>
            [MarshalAs(UnmanagedType.I8)]
            public Int64 dts; // decompression time stamp in time_base units

            /// <summary>
            /// 
            /// </summary>
            public IntPtr data;

            /// <summary>
            /// 
            /// </summary>
            [MarshalAs(UnmanagedType.I4)]
            public int size;

            /// <summary>
            /// 
            /// </summary>
            [MarshalAs(UnmanagedType.I4)]
            public int stream_index;

            /// <summary>
            /// 
            /// </summary>
            [MarshalAs(UnmanagedType.I4)]
            public int flags;

            /// <summary>
            /// 
            /// </summary>
            [MarshalAs(UnmanagedType.I4)]
            public int duration;

            /// <summary>
            /// 
            /// </summary>
            [MarshalAs(UnmanagedType.FunctionPtr)]
            public DestructCallback destruct;

            /// <summary>
            /// 
            /// </summary>
            public IntPtr priv;

            /// <summary>
            /// 
            /// </summary>
            [MarshalAs(UnmanagedType.I8)]
            public Int64 pos;
        };

        /// <summary>
        /// 
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct AVFrac
        {
            /// <summary>
            /// 
            /// </summary>
            [MarshalAs(UnmanagedType.I8)]
            public Int64 val;

            /// <summary>
            /// 
            /// </summary>
            [MarshalAs(UnmanagedType.I8)]
            public Int64 num;

            /// <summary>
            /// 
            /// </summary>
            [MarshalAs(UnmanagedType.I8)]
            public Int64 den;
        };

        /// <summary>
        /// 
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct AVProbeData
        {
            /// <summary>
            /// 
            /// </summary>
            [MarshalAs(UnmanagedType.LPStr)]
            public String filename;

            /// <summary>
            /// 
            /// </summary>
            public IntPtr buf;

            /// <summary>
            /// 
            /// </summary>
            public int buf_size;
        };

        /// <summary>
        /// 
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct AVFormatParameters
        {
            /// <summary>
            /// 
            /// </summary>
            public AVRational time_base;
            /// <summary>
            /// 
            /// </summary>
            public int sample_rate;
            /// <summary>
            /// 
            /// </summary>
            public int channels;
            /// <summary>
            /// 
            /// </summary>
            public int width;
            /// <summary>
            /// 
            /// </summary>
            public int height;
            /// <summary>
            /// 
            /// </summary>
            public PixelFormat pix_fmt;
            /// <summary>
            /// 
            /// </summary>
            public IntPtr image_format; // AVImageFormat
            /// <summary>
            /// 
            /// </summary>
            public int channel;
#if !AVFORMAT_VERSION_52
            /// <summary>
            /// 
            /// </summary>
            [MarshalAs(UnmanagedType.LPStr)]
            public String device;
#endif
            /// <summary>
            /// 
            /// </summary>
            [MarshalAs(UnmanagedType.LPStr)]
            public String standard;
            /// <summary>
            /// 
            /// </summary>
            public int mpeg2ts_raw;
            /// <summary>
            /// 
            /// </summary>
            public int mpeg2ts_compute_pcr;

            /// <summary>
            /// 
            /// </summary>
            public int initial_pause;
            /// <summary>
            /// 
            /// </summary>
            public int prealloced_context;
#if !AVFORMAT_VERSION_52
            /// <summary>
            /// 
            /// </summary>
            public CodecID video_codec_id;
            /// <summary>
            /// 
            /// </summary>
            public CodecID audio_codec_id;
#endif
        };

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pAVFormatContext"></param>
        /// <returns></returns>
        public delegate int WriteHeader(IntPtr pAVFormatContext);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="pAVFormatContext"></param>
        /// <param name="pAVPacket"></param>
        /// <returns></returns>
        public delegate int WritePacket(IntPtr pAVFormatContext, IntPtr pAVPacket);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="pAVFormatContext"></param>
        /// <returns></returns>
        public delegate int WriteTrailer(IntPtr pAVFormatContext);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="pAVFormatContext"></param>
        /// <param name="avFormatParameters"></param>
        /// <returns></returns>
        public delegate int SetParametersCallback(IntPtr pAVFormatContext, IntPtr avFormatParameters);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="pAVFormatContext"></param>
        /// <param name="pOutAVPacket"></param>
        /// <param name="pInAVPacket"></param>
        /// <param name="flush"></param>
        /// <returns></returns>
        public delegate int InterleavePacketCallback(IntPtr pAVFormatContext, IntPtr pOutAVPacket, IntPtr pInAVPacket, int flush);

        /// <summary>
        /// 
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct AVOutputFormat
        {
            /// <summary>
            /// 
            /// </summary>
            [MarshalAs(UnmanagedType.LPStr)]
            public String name;

            /// <summary>
            /// 
            /// </summary>
            [MarshalAs(UnmanagedType.LPStr)]
            public String long_name;

            /// <summary>
            /// 
            /// </summary>
            [MarshalAs(UnmanagedType.LPStr)]
            public String mime_type;

            /// <summary>
            /// 
            /// </summary>
            [MarshalAs(UnmanagedType.LPStr)]
            public String extensions;

            /// <summary>
            /// 
            /// </summary>
            public int priv_data_size;

            /// <summary>
            /// 
            /// </summary>
            public CodecID audio_codec;

            /// <summary>
            /// 
            /// </summary>
            public CodecID video_codec;

            /// <summary>
            /// 
            /// </summary>
            [MarshalAs(UnmanagedType.FunctionPtr)]
            public WriteHeader write_header;

            /// <summary>
            /// 
            /// </summary>
            [MarshalAs(UnmanagedType.FunctionPtr)]
            public WritePacket write_packet;

            /// <summary>
            /// 
            /// </summary>
            [MarshalAs(UnmanagedType.FunctionPtr)]
            public WriteTrailer write_trailer;

            /// <summary>
            /// 
            /// </summary>
            public int flags;

            /// <summary>
            /// 
            /// </summary>
            [MarshalAs(UnmanagedType.FunctionPtr)]
            public SetParametersCallback set_parameters;

            /// <summary>
            /// 
            /// </summary>
            [MarshalAs(UnmanagedType.FunctionPtr)]
            public InterleavePacketCallback interleave_packet;

            /// <summary>
            /// 
            /// </summary>
            public IntPtr nextAVOutputFormat;
        };

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pAVProbeData"></param>
        /// <returns></returns>
        public delegate int ReadProbeCallback(IntPtr pAVProbeData);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="pAVFormatContext"></param>
        /// <param name="pAVFormatParameters"></param>
        /// <returns></returns>
        public delegate int ReadHeaderCallback(IntPtr pAVFormatContext, IntPtr pAVFormatParameters);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="pAVFormatContext"></param>
        /// <param name="pAVPacket"></param>
        /// <returns></returns>
        public delegate int ReadPacketCallback(IntPtr pAVFormatContext, IntPtr pAVPacket);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="pAVFormatContext"></param>
        /// <returns></returns>
        public delegate int ReadCloseCallback(IntPtr pAVFormatContext);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="pAVFormatContext"></param>
        /// <param name="stream_index"></param>
        /// <param name="timestamp"></param>
        /// <param name="flags"></param>
        /// <returns></returns>
        public delegate int ReadSeekCallback(IntPtr pAVFormatContext, int stream_index, Int64 timestamp, int flags);

        //int64_t (*read_timestamp)(struct AVFormatContext *s, int stream_index,
        //int64_t *pos, int64_t pos_limit);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="pAVFormatContext"></param>
        /// <param name="stream_index"></param>
        /// <param name="pos"></param>
        /// <param name="pos_limit"></param>
        /// <returns></returns>
        public delegate int ReadTimestampCallback(IntPtr pAVFormatContext, int stream_index, IntPtr pos, Int64 pos_limit);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="pAVFormatContext"></param>
        /// <returns></returns>
        public delegate int ReadPlayCallback(IntPtr pAVFormatContext);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="pAVFormatContext"></param>
        /// <returns></returns>
        public delegate int ReadPauseCallback(IntPtr pAVFormatContext);

        /// <summary>
        /// 
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct AVInputFormat
        {
            /// <summary>
            /// 
            /// </summary>
            [MarshalAs(UnmanagedType.LPStr)]
            public String name;

            /// <summary>
            /// 
            /// </summary>
            [MarshalAs(UnmanagedType.LPStr)]
            public String long_name;

            /// <summary>
            /// 
            /// </summary>
            public int priv_data_size;

            /// <summary>
            /// 
            /// </summary>
            [MarshalAs(UnmanagedType.FunctionPtr)]
            public ReadProbeCallback read_probe;

            /// <summary>
            /// 
            /// </summary>
            [MarshalAs(UnmanagedType.FunctionPtr)]
            public ReadHeaderCallback read_header;

            /// <summary>
            /// 
            /// </summary>
            [MarshalAs(UnmanagedType.FunctionPtr)]
            public ReadPacketCallback read_packet;

            /// <summary>
            /// 
            /// </summary>
            [MarshalAs(UnmanagedType.FunctionPtr)]
            public ReadCloseCallback read_close;

            /// <summary>
            /// 
            /// </summary>
            [MarshalAs(UnmanagedType.FunctionPtr)]
            public ReadSeekCallback read_seek;

            /// <summary>
            /// 
            /// </summary>
            [MarshalAs(UnmanagedType.FunctionPtr)]
            public ReadTimestampCallback read_timestamp;

            // can use flags: AVFMT_NOFILE, AVFMT_NEEDNUMBER
            /// <summary>
            /// 
            /// </summary>
            public int flags;

            /// <summary>
            /// 
            /// </summary>
            [MarshalAs(UnmanagedType.LPStr)]
            public String extensions;

            /// <summary>
            /// 
            /// </summary>
            public int value;

            /// <summary>
            /// 
            /// </summary>
            [MarshalAs(UnmanagedType.FunctionPtr)]
            public ReadPlayCallback read_play;

            /// <summary>
            /// 
            /// </summary>
            [MarshalAs(UnmanagedType.FunctionPtr)]
            public ReadPauseCallback read_pause;

            /// <summary>
            /// 
            /// </summary>
            public IntPtr nextAVInputFormat;
        };

        /// <summary>
        /// 
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct AVIndexEntry
        {
            /// <summary>
            /// 
            /// </summary>
            public Int64 pos;
            /// <summary>
            /// 
            /// </summary>
            public Int64 timestamp;
            /// <summary>
            /// 
            /// </summary>
            public int flags;
            /// <summary>
            /// 
            /// </summary>
            public int size;
            /// <summary>
            /// 
            /// </summary>
            public int min_distance;
        };

        /// <summary>
        /// 
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct AVStream
        {
            /// <summary>
            /// 
            /// </summary>
            [MarshalAs(UnmanagedType.I4)]
            public int index; // stream index in AVFormatContext

            /// <summary>
            /// 
            /// </summary>
            [MarshalAs(UnmanagedType.I4)]
            public int id; // format specific stream id

            /// <summary>
            /// 
            /// </summary>
            public IntPtr codec; // AVCodecContext

            /**
            * real base frame rate of the stream.
            * for example if the timebase is 1/90000 and all frames have either
            * approximately 3600 or 1800 timer ticks then r_frame_rate will be 50/1
            */
            public AVRational r_frame_rate;

            /// <summary>
            /// 
            /// </summary>
            public IntPtr priv_data;

            /// <summary>
            /// 
            /// </summary>
            [MarshalAs(UnmanagedType.I8)]
            public Int64 codec_info_duration; // internal data used in av_find_stream_info()

#if !AVFORMAT_VERSION_52
            /// <summary>
            /// 
            /// </summary>
            [MarshalAs(UnmanagedType.I4)]
            public int codec_info_nb_frames;
#endif

            /// <summary>
            /// 
            /// </summary>
            public AVFrac pts; // encoding: PTS generation when outputing stream

            /**
            * this is the fundamental unit of time (in seconds) in terms
            * of which frame timestamps are represented. for fixed-fps content,
            * timebase should be 1/framerate and timestamp increments should be
            * identically 1.
            */
            public AVRational time_base;

            /// <summary>
            /// 
            /// </summary>
            [MarshalAs(UnmanagedType.I4)]
            public int pts_wrap_bits; // number of bits in pts (used for wrapping control)

            /// <summary>
            /// 
            /// </summary>
            [MarshalAs(UnmanagedType.I4)]
            public int stream_copy; // if TRUE, just copy stream

            /// <summary>
            /// 
            /// </summary>
            public AVDiscard discard; // selects which packets can be discarded at will and dont need to be demuxed

            /// <summary>
            /// 
            /// </summary>
            [MarshalAs(UnmanagedType.R4)]
            public float quality;

            /// <summary>
            /// 
            /// </summary>
            [MarshalAs(UnmanagedType.I8)]
            public Int64 start_time;

            // decoding: duration of the stream, in AV_TIME_BASE fractional seconds.
            /// <summary>
            /// 
            /// </summary>
            [MarshalAs(UnmanagedType.I8)]
            public Int64 duration;

            /// <summary>
            /// 
            /// </summary>
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public byte[] language; // ISO 639 3-letter language code (empty string if undefined)

            /// <summary>
            /// 
            /// </summary>
            [MarshalAs(UnmanagedType.I4)]
            public int need_parsing;

            /// <summary>
            /// 
            /// </summary>
            public IntPtr pAVCodecParserContext;

            /// <summary>
            /// 
            /// </summary>
            [MarshalAs(UnmanagedType.I8)]
            public Int64 cur_dts;

            /// <summary>
            /// 
            /// </summary>
            [MarshalAs(UnmanagedType.I4)]
            public int last_IP_duration;

            /// <summary>
            /// 
            /// </summary>
            [MarshalAs(UnmanagedType.I8)]
            public Int64 last_IP_pts;

            /// <summary>
            /// 
            /// </summary>
            public IntPtr pAVIndexEntry; // only used if the format does not support seeking natively

            /// <summary>
            /// 
            /// </summary>
            [MarshalAs(UnmanagedType.I4)]
            public int nb_index_entries;

            /// <summary>
            /// 
            /// </summary>
            [MarshalAs(UnmanagedType.I4)]
            public int index_entries_allocated_size;

            /// <summary>
            /// 
            /// </summary>
            [MarshalAs(UnmanagedType.I8)]
            public Int64 nb_frames; // number of frames in this stream if known or 0

            /// <summary>
            /// 
            /// </summary>
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = (MAX_REORDER_DELAY + 1))]
            public Int64[] pts_buffer; // pts_buffer[MAX_REORDER_DELAY+1]
        };

        /// <summary>
        /// 
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct AVFormatContext
        {
            /// <summary>
            /// 
            /// </summary>
            public IntPtr pAVClass; // set by av_alloc_format_context

            /// <summary>
            /// 
            /// </summary>
            public IntPtr pAVInputFormat; // can only be iformat or oformat, not both at the same time

            /// <summary>
            /// 
            /// </summary>
            public IntPtr pAVOutputFormat;

            //[MarshalAs(UnmanagedType.FunctionPtr)]
            //AnonymousCallback priv_data;

            /// <summary>
            /// 
            /// </summary>
            public IntPtr priv_data;

            /// <summary>
            /// 
            /// </summary>
#if AVFORMAT_VERSION_52
            public IntPtr pb; // ByteIOContext
#else
            public ByteIOContext pb;
#endif

            /// <summary>
            /// 
            /// </summary>
            [MarshalAs(UnmanagedType.I4)]
            public int nb_streams;

            /// <summary>
            /// 
            /// </summary>
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = MAX_STREAMS)]
            public IntPtr[] streams; // AVStream

            /// <summary>
            /// 
            /// </summary>
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1024)]
            public byte[] filename; // input or output filename

            /* stream info */
            /// <summary>
            /// 
            /// </summary>
            [MarshalAs(UnmanagedType.I8)]
            public Int64 timestamp;

            /// <summary>
            /// 
            /// </summary>
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 512)]
            public byte[] title;

            /// <summary>
            /// 
            /// </summary>
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 512)]
            public byte[] author;

            /// <summary>
            /// 
            /// </summary>
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 512)]
            public byte[] copyright;

            /// <summary>
            /// 
            /// </summary>
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 512)]
            public byte[] comment;

            /// <summary>
            /// 
            /// </summary>
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 512)]
            public byte[] album;

            /// <summary>
            /// 
            /// </summary>
            [MarshalAs(UnmanagedType.I4)]
            public int year; // ID3 year, 0 if none

            /// <summary>
            /// 
            /// </summary>
            [MarshalAs(UnmanagedType.I4)]
            public int tract; // track number, 0 if none

            /// <summary>
            /// 
            /// </summary>
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            public byte[] genre; // ID3 genre

            /// <summary>
            /// 
            /// </summary>
            [MarshalAs(UnmanagedType.I4)]
            public int ctx_flags; // format specific flags, see AVFMTCTX_xx

            /* This buffer is only needed when packets were already buffered but
            not decoded, for example to get the codec parameters in mpeg
            streams */

            /// <summary>
            /// 
            /// </summary>
            public IntPtr packet_buffer; // AVPacketList
            /* decoding: position of the first frame of the component, in
            AV_TIME_BASE fractional seconds. NEVER set this value directly:
            it is deduced from the AVStream values. */

            /// <summary>
            /// 
            /// </summary>
            [MarshalAs(UnmanagedType.I8)]
            public Int64 start_time;
            /* decoding: duration of the stream, in AV_TIME_BASE fractional
            seconds. NEVER set this value directly: it is deduced from the
            AVStream values. */

            /// <summary>
            /// 
            /// </summary>
            [MarshalAs(UnmanagedType.I8)]
            public Int64 duration;

            /// <summary>
            /// 
            /// </summary>
            [MarshalAs(UnmanagedType.I8)]
            public Int64 file_size; // decoding: total file size. 0 if unknown

            /* decoding: total stream bitrate in bit/s, 0 if not
            available. Never set it directly if the file_size and the
            duration are known as ffmpeg can compute it automatically. */
            /// <summary>
            /// 
            /// </summary>
            [MarshalAs(UnmanagedType.I4)]
            public int bit_rate;

            /* av_read_frame() support */
            /// <summary>
            /// 
            /// </summary>
            public IntPtr cur_st; // AVStream

            /// <summary>
            /// 
            /// </summary>
            public IntPtr cur_ptr; // uint8_t

            /// <summary>
            /// 
            /// </summary>
            [MarshalAs(UnmanagedType.I4)]
            public int cur_len;

            /// <summary>
            /// 
            /// </summary>
            public AVPacket cur_pkt; // AVPacket

            /* av_seek_frame() support */
            /// <summary>
            /// 
            /// </summary>
            [MarshalAs(UnmanagedType.I8)]
            public Int64 data_offset; // offset of the first packet

            /// <summary>
            /// 
            /// </summary>
            [MarshalAs(UnmanagedType.I4)]
            public int index_built;

            /// <summary>
            /// 
            /// </summary>
            [MarshalAs(UnmanagedType.I4)]
            public int mux_rate;

            /// <summary>
            /// 
            /// </summary>
            [MarshalAs(UnmanagedType.I4)]
            public int packet_size;

            /// <summary>
            /// 
            /// </summary>
            [MarshalAs(UnmanagedType.I4)]
            public int preload;

            /// <summary>
            /// 
            /// </summary>
            [MarshalAs(UnmanagedType.I4)]
            public int max_delay;

            /// <summary>
            /// 
            /// </summary>
            [MarshalAs(UnmanagedType.I4)]
            public int loop_output; // number of times to loop output in formats that support it

            /// <summary>
            /// 
            /// </summary>
            [MarshalAs(UnmanagedType.I4)]
            public int flags;

            /// <summary>
            /// 
            /// </summary>
            [MarshalAs(UnmanagedType.I4)]
            public int loop_input;

            /// <summary>
            /// 
            /// </summary>
            [MarshalAs(UnmanagedType.U4)]
            [CLSCompliant(false)]
            public uint probesize; // decoding: size of data to probe; encoding unused
          
            /// <summary>
            /// 
            /// </summary>
            [MarshalAs(UnmanagedType.I4)]
            public int max_analyze_duration;

            /// <summary>
            /// 
            /// </summary>
            public IntPtr key;

            /// <summary>
            /// 
            /// </summary>
            [MarshalAs(UnmanagedType.I4)]
            public int keylen;

            /// <summary>
            /// 
            /// </summary>
            [MarshalAs(UnmanagedType.U4)]
            [CLSCompliant(false)]
            public uint nb_programs;
            
            /// <summary>
            /// 
            /// </summary>
            public IntPtr programs;

            /// <summary>
            /// 
            /// </summary>
            public CodecID video_codec_id;

            /// <summary>
            /// 
            /// </summary>
            public CodecID audio_codec_id;

            /// <summary>
            /// 
            /// </summary>
            public CodecID subtitle_codec_id;
        };

        /// <summary>
        /// 
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct AVPacketList
        {
            /// <summary>
            /// 
            /// </summary>
            public AVPacket pkt;
            /// <summary>
            /// 
            /// </summary>
            public IntPtr next; // AVPacketList
        };

        /// <summary>
        /// 
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct AVImageInfo
        {
            /// <summary>
            /// 
            /// </summary>
            public PixelFormat pix_fmt; // requested pixel format

            /// <summary>
            /// 
            /// </summary>
            [MarshalAs(UnmanagedType.I4)]
            public int width; // requested width

            /// <summary>
            /// 
            /// </summary>
            [MarshalAs(UnmanagedType.I4)]
            public int height; // requested height

            /// <summary>
            /// 
            /// </summary>
            [MarshalAs(UnmanagedType.I4)]
            public int interleaved; // image is interleaved (e.g. interleaved GIF)

            /// <summary>
            /// 
            /// </summary>
            public AVPicture pict; // returned allocated image
        };

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pVoid"></param>
        /// <param name="pAVImageInfo"></param>
        /// <returns></returns>
        public delegate int AllocCBCallback(IntPtr pVoid, IntPtr pAVImageInfo);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pAVProbeData"></param>
        /// <returns></returns>
        public delegate int ImgProbeCallback(IntPtr pAVProbeData);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="pByteIOContext"></param>
        /// <param name="alloc_cb"></param>
        /// <param name="pVoid"></param>
        /// <returns></returns>
        public delegate int ImgReadCallback(IntPtr pByteIOContext,
        [MarshalAs(UnmanagedType.FunctionPtr)]
AllocCBCallback alloc_cb,
        IntPtr pVoid);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="pByteIOContext"></param>
        /// <param name="pAVImageInfo"></param>
        /// <returns></returns>
        public delegate int ImgWriteCallback(IntPtr pByteIOContext, IntPtr pAVImageInfo);

        /// <summary>
        /// 
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct AVImageFormat
        {
            /// <summary>
            /// 
            /// </summary>
            [MarshalAs(UnmanagedType.LPTStr)]
            public String name;

            /// <summary>
            /// 
            /// </summary>
            [MarshalAs(UnmanagedType.LPTStr)]
            public String extensions;

            // tell if a given file has a chance of being parsing by this format
            /// <summary>
            /// 
            /// </summary>
            [MarshalAs(UnmanagedType.FunctionPtr)]
            public ImgProbeCallback img_probe;

            /* read a whole image. 'alloc_cb' is called when the image size is
            known so that the caller can allocate the image. If 'allo_cb'
            returns non zero, then the parsing is aborted. Return '0' if
            OK. */
            /// <summary>
            /// 
            /// </summary>
            [MarshalAs(UnmanagedType.FunctionPtr)]
            public ImgReadCallback img_read;

            /* write the image */
            /// <summary>
            /// 
            /// </summary>
            public int supported_pixel_formats; // mask of supported formats for output

            /// <summary>
            /// 
            /// </summary>
            [MarshalAs(UnmanagedType.FunctionPtr)]
            public ImgWriteCallback img_write;

            /// <summary>
            /// 
            /// </summary>
            public int flags;

            /// <summary>
            /// 
            /// </summary>
            public IntPtr next; // AVImageFormat
        };
    }
}
