#region LGPL License
/*
Axiom Game Engine Library
Copyright (C) 2003  Axiom Project Team

The overall design, and a majority of the core engine and rendering code 
contained within this library is a derivative of the open source Object Oriented 
Graphics Engine OGRE, which can be found at http://ogre.sourceforge.net.  
Many thanks to the OGRE team for maintaining such a high quality project.

This library is free software; you can redistribute it and/or
modify it under the terms of the GNU Lesser General Public
License as published by the Free Software Foundation; either
version 2.1 of the License, or (at your option) any later version.

This library is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
Lesser General Public License for more details.

You should have received a copy of the GNU Lesser General Public
License along with this library; if not, write to the Free Software
Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
*/
#endregion

#region Namespace Declarations

using System;
using System.Runtime.InteropServices;

using Axiom;

#endregion Namespace Declarations

namespace Axiom.Platforms.Win32
{
    /// <summary>
    ///		Platform management specialization for Microsoft Windows (r) platform.
    /// </summary>
    [PluginMetadata(IsSingleton=true, Name="PlatformManager",
        Description="Axiom Win32 Platform")]
    public class Win32PlatformManager : IPlatformManager, ISingletonPlugin
    {
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
        public InputReader CreateInputReader()
        {
            inputReader = new Win32InputReader();
            return inputReader;
        }

        /// <summary>
        ///		Creates a high precision Windows timer.
        /// </summary>
        /// <returns></returns>
        public ITimer CreateTimer()
        {
            timer = new Win32Timer();
            return timer;
        }

        /// <summary>
        ///		Implements the Microsoft Windows (r) message pump for allowing the OS to process
        ///		pending events.
        /// </summary>
        public void DoEvents()
        {
            Msg msg;

            // pump those events!
            while ( !( PeekMessage( out msg, IntPtr.Zero, 0, 0, PM_REMOVE ) == 0 ) )
            {
                TranslateMessage( ref msg );
                DispatchMessage( ref msg );
            }
        }

        /// <summary>
        ///     Called when the engine is being shutdown.
        /// </summary>
        public void Dispose()
        {
            if ( inputReader != null )
            {
                inputReader.Dispose();
            }
        }

        #endregion

        #region P/Invoke Declarations

        struct Msg
        {
            public int hWnd;
            public int Message;
            public int wParam;
            public int lParam;
            public int time;
            public POINTAPI pt;
        }

        struct POINTAPI
        {
            public int x;
            public int y;

            // Just to get rid of Warning CS0649.
            public POINTAPI( int x, int y )
            {
                this.x = x;
                this.y = y;
            }
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
        [DllImport( USER_DLL )]
        private static extern int PeekMessage( out Msg msg, IntPtr handle, int msgFilterMin, int msgFilterMax, int removeMsg );

        /// <summary>
        ///		The TranslateMessage function translates virtual-key messages into character messages.
        /// </summary>
        /// <param name="msg">
        ///		an MSG structure that contains message information retrieved from the calling thread's message queue 
        ///		by using the GetMessage or <see cref="PeekMessage"/> function.
        /// </param>
        [DllImport( USER_DLL )]
        private static extern void TranslateMessage( ref Msg msg );

        /// <summary>
        ///		The DispatchMessage function dispatches a message to a window procedure.
        /// </summary>
        /// <param name="msg">A <see cref="Msg"/> structure containing the message.</param>
        [DllImport( USER_DLL )]
        private static extern void DispatchMessage( ref Msg msg );

        #endregion P/Invoke Declarations

        public object GetSubsystemImplementation()
        {
            return this;
        }

        public void Start()
        {
            LogManager.Instance.Write("Win32 Platform Manager started");
            _isStarted = true;
        }

        public void Stop()
        {
            LogManager.Instance.Write("Win32 Platform Manager stopped");
        }

        private bool _isStarted = false;

        public bool IsStarted
        {
            get { return _isStarted; }
        }

    }
}
