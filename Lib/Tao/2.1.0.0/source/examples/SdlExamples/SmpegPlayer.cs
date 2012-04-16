#region License
/*
MIT License
Copyright �2003-2005 Tao Framework Team
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
using System.Threading;
using Tao.Sdl;
using System.Runtime.InteropServices;
using System.IO;

namespace SdlExamples 
{
	#region Class Documentation
	/// <summary>
	/// Simple Tao.Sdl Example
	/// </summary>
	/// <remarks>
	/// Just draws a bunch of rectangles to the screen. 
	/// To quit, you can close the window, 
	/// press the Escape key or press the 'q' key
	/// <p>Written by David Hudson (jendave@yahoo.com)</p>
	/// <p>This is a reimplementation of an example 
	/// written by Will Weisser (ogl@9mm.com)</p>
	/// </remarks>
	#endregion Class Documentation
	public class SmpegPlayer 
	{		
		//int position = 0;
		//byte[] two = new byte[4096];
		//Smpeg.SMPEG_DisplayCallback callbackDelegate;
		static IntPtr surfacePtr;

		#region Run()
		/// <summary>
		/// 
		/// </summary>
        [STAThread]
		public static void Run() 
		{
			Sdl.SDL_Event evt;
			bool quitFlag = false;
			int flags = (Sdl.SDL_HWSURFACE|Sdl.SDL_DOUBLEBUF|Sdl.SDL_ANYFORMAT);
			int bpp = 16;
			int width = 352;
			int height = 240;
			
			Sdl.SDL_Init(Sdl.SDL_INIT_EVERYTHING);
			Sdl.SDL_WM_SetCaption("Tao.Sdl Example - SmpegPlayer", "");
			surfacePtr = Sdl.SDL_SetVideoMode(
				width, 
				height, 
				bpp, 
				flags);
			SdlMixer.Mix_OpenAudio(SdlMixer.MIX_DEFAULT_FREQUENCY, unchecked(Sdl.AUDIO_S16LSB), 2, 1024);
			Smpeg.SMPEG_Info info = new Smpeg.SMPEG_Info();

            string filePath = Path.Combine("..", "..");
            string fileDirectory = "Data";
            string fileName = "SdlExamples.SmpegPlayer.mpg";
            if (File.Exists(fileName))
            {
                filePath = "";
                fileDirectory = "";
            }
            else if (File.Exists(Path.Combine(fileDirectory, fileName)))
            {
                filePath = "";
            }

            string file = Path.Combine(Path.Combine(filePath, fileDirectory), fileName);

			//SdlMixer.MixFunctionDelegate audioMixer = new SdlMixer.MixFunctionDelegate(this.player);
			//int freq;
			//short format;
			//int channels;
			SdlMixer.Mix_CloseAudio();
			IntPtr intPtr = Smpeg.SMPEG_new(file, out info, 1); 
			//Smpeg.SMPEG_enableaudio(intPtr, 0);
			//SdlMixer.Mix_QuerySpec(out freq, out unchecked(format), out channels);
			//Sdl.SDL_AudioSpec audiofmt = new Tao.Sdl.Sdl.SDL_AudioSpec();
			//audiofmt.freq = freq;
			//audiofmt.format = unchecked(format);
			//audiofmt.channels = (byte) channels;
			//Console.WriteLine("Freq: " + audiofmt.freq);
			//Console.WriteLine("Format: " + audiofmt.format);
			//Console.WriteLine("Channels: " + audiofmt.channels);

			Smpeg.SMPEG_getinfo(intPtr, out info);
			Console.WriteLine("Time: " + info.total_time.ToString());
			Console.WriteLine("Width: " + info.width.ToString());
			Console.WriteLine("Height: " + info.height.ToString());
			Console.WriteLine("Size: " + info.total_size.ToString());
			Console.WriteLine("Smpeg_error: " + Smpeg.SMPEG_error(intPtr));
			
			//Smpeg.SMPEG_actualSpec(intPtr, ref audiofmt); 
			//SdlMixer.Mix_HookMusic(audioMixer, intPtr);
			Smpeg.SMPEG_setdisplay(intPtr, surfacePtr, IntPtr.Zero, null);
			
			Smpeg.SMPEG_play(intPtr);
			//Smpeg.SMPEG_loop(intPtr, 1);
			//Smpeg.SMPEG_enableaudio(intPtr, 1);

            try
            {
                while ((Smpeg.SMPEG_status(intPtr) == Smpeg.SMPEG_PLAYING) &&
                    (quitFlag == false))
                {
                    Sdl.SDL_PollEvent(out evt);

                    if (evt.type == Sdl.SDL_QUIT)
                    {
                        quitFlag = true;
                    }
                    else if (evt.type == Sdl.SDL_KEYDOWN)
                    {
                        if ((evt.key.keysym.sym == (int)Sdl.SDLK_ESCAPE) ||
                            (evt.key.keysym.sym == (int)Sdl.SDLK_q))
                        {
                            quitFlag = true;
                        }
                    }
                }
            }
            catch
            {
                Smpeg.SMPEG_stop(intPtr);
                Smpeg.SMPEG_delete(intPtr);
                Sdl.SDL_Quit();
                throw; 
            }
            finally
            {
                Smpeg.SMPEG_stop(intPtr);
                Smpeg.SMPEG_delete(intPtr);
                Sdl.SDL_Quit();
            }
		} 

//		private void player(IntPtr one, byte[] temp, int len)
//		{
//			//position +=len;
//			IntPtr tempPtr = new IntPtr(one.ToInt32() + position);
//			
//			Marshal.Copy(tempPtr, two, 0, len);
//			Smpeg.SMPEG_playAudio(one, two, len);
//			position +=len;
//		}
		#endregion Run()
	}
}
