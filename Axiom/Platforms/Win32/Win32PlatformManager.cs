using System;
using System.Runtime.InteropServices;
using Axiom.Core;
using Axiom.Input;

namespace Axiom.Platforms.Win32
{
	/// <summary>
	///		Platform management specialization for Microsoft Windows (r) platform.
	/// </summary>
	// TODO: Disposal of object create here.
	public class Win32PlatformManager : IPlatformManager {
		#region Fields

		/// <summary>
		///		Reference to the current input reader.
		/// </summary>
		private InputReader inputReader;
		/// <summary>
		///		Reference to the current active timer.
		/// </summary>
        private ITimer timer;

		#endregion Fields

		#region IPlatformManager Members

		/// <summary>
		///		Creates an InputReader implemented using Microsoft DirectInput (tm).
		/// </summary>
		/// <returns></returns>
		public Axiom.Input.InputReader CreateInputReader() {
			inputReader = new Win32InputReader();
			return inputReader;
		}

		/// <summary>
		///		Creates a high precision Windows timer.
		/// </summary>
		/// <returns></returns>
		public ITimer CreateTimer() {
            timer = new Win32Timer();
			return timer;
		}

		/// <summary>
		///		Implements the Microsoft Windows (r) message pump for allowing the OS to process
		///		pending events.
		/// </summary>
		public void DoEvents() {
			Msg msg;

			// pump those events!
			while(!PeekMessage(out msg, IntPtr.Zero, 0, 0, PM_REMOVE)) {
				TranslateMessage(msg);
				DispatchMessage(msg);
			}
		}

		#endregion

		#region P/Invoke Declarations

		struct POINTAPI {
			int x;
			int y;
		}

		struct Msg {
			int hWnd;
			int Message;
			int wParam;
			int lParam;
			int time;
			POINTAPI pt;
		}

		/// <summary>
		///		PeekMessage option to remove the message from the queue after processing.
		/// </summary>
		const int PM_REMOVE = 0x0001;
		const string USER_DLL = "user32.dll";

		/// <summary>
		///		The PeekMessage function dispatches incoming sent messages, checks the thread message 
		///		queue for a posted message, and retrieves the message (if any exist).
		/// </summary>
		/// <param name="msg">A <see cref="Msg"/> structure that receives message information.</param>
		/// <param name="handle"></param>
		/// <param name="msgFilterMin"></param>
		/// <param name="msgFilterMax"></param>
		/// <param name="removeMsg"></param>
		[DllImport(USER_DLL)]
		private static extern bool PeekMessage(out Msg msg, IntPtr handle, int msgFilterMin, int msgFilterMax, int removeMsg);

		/// <summary>
		///		The TranslateMessage function translates virtual-key messages into character messages.
		/// </summary>
		/// <param name="msg">
		///		an MSG structure that contains message information retrieved from the calling thread's message queue 
		///		by using the GetMessage or <see cref="PeekMessage"/> function.
		/// </param>
		[DllImport(USER_DLL)]
		private static extern void TranslateMessage(Msg msg);

		/// <summary>
		///		The DispatchMessage function dispatches a message to a window procedure.
		/// </summary>
		/// <param name="msg">A <see cref="Msg"/> structure containing the message.</param>
		[DllImport(USER_DLL)]
		private static extern void DispatchMessage(Msg msg);

		#endregion P/Invoke Declarations
	}
}
