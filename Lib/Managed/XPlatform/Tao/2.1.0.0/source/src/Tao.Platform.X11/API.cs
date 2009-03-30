﻿#region License
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
using System.Runtime.InteropServices;

namespace Tao.Platform.X11
{
    #region Types

    // using XID = System.Int32;
    using Window = System.IntPtr;
    using Drawable = System.IntPtr;
    using Font = System.IntPtr;
    using Pixmap = System.IntPtr;
    using Cursor = System.IntPtr;
    using Colormap = System.IntPtr;
    using GContext = System.IntPtr;
    using KeySym = System.IntPtr;
    using Mask = System.IntPtr;
    using Atom = System.IntPtr;
    using VisualID = System.IntPtr;
    using Time = System.UInt32;
    using KeyCode = System.Byte;    // Or maybe ushort?

    using Display = System.IntPtr;
    using XPointer = System.IntPtr;

    #endregion

    #region internal static class API

    internal static class API
    {
        // Prevent BeforeFieldInit optimization.
        static API() { }

        private const string _dll_name = "libX11";
        private const string _dll_name_vid = "libXxf86vm";

        // Display management
        [DllImport(_dll_name, EntryPoint = "XOpenDisplay")]
        extern internal static IntPtr OpenDisplay([MarshalAs(UnmanagedType.LPTStr)] string display_name);

        [DllImport(_dll_name, EntryPoint = "XCloseDisplay")]
        extern internal static void CloseDisplay(Display display);

        [DllImport(_dll_name, EntryPoint = "XCreateColormap")]
        extern internal static IntPtr CreateColormap(Display display, Window window, IntPtr visual, int alloc);

        #region Window handling

        [DllImport(_dll_name, EntryPoint = "XRootWindow")]
        internal static extern Window RootWindow(Display display, int screen);

        [DllImport(_dll_name, EntryPoint = "XCreateWindow")]
        internal extern static Window CreateWindow(
            Display display,
            Window parent,
            int x, int y,
            //uint width, uint height,
            int width, int height,
            //uint border_width,
            int border_width,
            int depth,
            //uint @class,
            int @class,
            IntPtr visual,
            [MarshalAs(UnmanagedType.SysUInt)] CreateWindowMask valuemask,
            SetWindowAttributes attributes
        );

        [DllImport(_dll_name, EntryPoint = "XCreateSimpleWindow")]
        internal extern static Window CreateSimpleWindow(
            Display display,
            Window parent,
            int x, int y,
            int width, int height,
            int border_width,
            long border,
            long background
        );

        [DllImport(_dll_name, EntryPoint = "XResizeWindow")]
        internal extern static int XResizeWindow(Display display, Window window, int width, int height);

        [DllImport(_dll_name, EntryPoint = "XDestroyWindow")]
        internal extern static void DestroyWindow(Display display, Window window);

        [DllImport(_dll_name, EntryPoint = "XMapWindow")]
        extern internal static void MapWindow(Display display, Window window);

        [DllImport(_dll_name, EntryPoint = "XMapRaised")]
        extern internal static void MapRaised(Display display, Window window);

        #endregion

        [DllImport(_dll_name, EntryPoint = "XDefaultScreen")]
        extern internal static int DefaultScreen(Display display);

        [DllImport(_dll_name, EntryPoint = "XDefaultVisual")]
        extern internal static IntPtr DefaultVisual(Display display, int screen_number);

        #region XFree

        /// <summary>
        /// Frees the memory used by an X structure. Only use on unmanaged structures!
        /// </summary>
        /// <param name="data">A pointer to the structure that will be freed.</param>
        [DllImport(_dll_name, EntryPoint = "XFree")]
        extern internal static void Free(IntPtr data);

        #endregion

        #region Event queue management

        [System.Security.SuppressUnmanagedCodeSecurity]
        [DllImport(_dll_name, EntryPoint = "XEventsQueued")]
        extern internal static int EventsQueued(Display display, int mode);

        [System.Security.SuppressUnmanagedCodeSecurity]
        [DllImport(_dll_name, EntryPoint = "XPending")]
        extern internal static int Pending(Display display);

        //[System.Security.SuppressUnmanagedCodeSecurity]
        [DllImport(_dll_name, EntryPoint = "XNextEvent")]
        extern internal static void NextEvent(
            Display display,
            [MarshalAs(UnmanagedType.AsAny)][In, Out]object e);

        [DllImport(_dll_name, EntryPoint = "XNextEvent")]
        extern internal static void NextEvent(Display display, [In, Out] IntPtr e);

        [DllImport(_dll_name, EntryPoint = "XPeekEvent")]
        extern internal static void PeekEvent(
            Display display,
            [MarshalAs(UnmanagedType.AsAny)][In, Out]object event_return
        );

        [DllImport(_dll_name, EntryPoint = "XPeekEvent")]
        extern internal static void PeekEvent(Display display, [In, Out]XEvent event_return);

        [DllImport(_dll_name, EntryPoint = "XSendEvent")]
        [return: MarshalAs(UnmanagedType.Bool)]
        extern internal static bool SendEvent(Display display, Window window, bool propagate,
            [MarshalAs(UnmanagedType.SysInt)]EventMask event_mask, ref XEvent event_send);

        /// <summary>
        /// The XSelectInput() function requests that the X server report the events associated
        /// with the specified event mask.
        /// </summary>
        /// <param name="display">Specifies the connection to the X server.</param>
        /// <param name="w">Specifies the window whose events you are interested in.</param>
        /// <param name="event_mask">Specifies the event mask.</param>
        /// <remarks>
        /// Initially, X will not report any of these events.
        /// Events are reported relative to a window.
        /// If a window is not interested in a device event,
        /// it usually propagates to the closest ancestor that is interested,
        /// unless the do_not_propagate mask prohibits it.
        /// Setting the event-mask attribute of a window overrides any previous call for the same window but not for other clients. Multiple clients can select for the same events on the same window with the following restrictions: 
        /// <para>Multiple clients can select events on the same window because their event masks are disjoint. When the X server generates an event, it reports it to all interested clients. </para>
        /// <para>Only one client at a time can select CirculateRequest, ConfigureRequest, or MapRequest events, which are associated with the event mask SubstructureRedirectMask. </para>
        /// <para>Only one client at a time can select a ResizeRequest event, which is associated with the event mask ResizeRedirectMask. </para>
        /// <para>Only one client at a time can select a ButtonPress event, which is associated with the event mask ButtonPressMask. </para>
        /// <para>The server reports the event to all interested clients. </para>
        /// <para>XSelectInput() can generate a BadWindow error.</para>
        /// </remarks>
        [DllImport(_dll_name, EntryPoint = "XSelectInput")]
        internal static extern void SelectInput(Display display, Window w, EventMask event_mask);

        /// <summary>
        /// When the predicate procedure finds a match, XCheckIfEvent() copies the matched event into the client-supplied XEvent structure and returns True. (This event is removed from the queue.) If the predicate procedure finds no match, XCheckIfEvent() returns False, and the output buffer will have been flushed. All earlier events stored in the queue are not discarded.
        /// </summary>
        /// <param name="display">Specifies the connection to the X server.</param>
        /// <param name="event_return">Returns a copy of the matched event's associated structure.</param>
        /// <param name="predicate">Specifies the procedure that is to be called to determine if the next event in the queue matches what you want</param>
        /// <param name="arg">Specifies the user-supplied argument that will be passed to the predicate procedure.</param>
        /// <returns>true if the predicate returns true for some event, false otherwise</returns>
        [DllImport(_dll_name, EntryPoint = "XCheckIfEvent")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CheckIfEvent(Display display, ref XEvent event_return,
            /*[MarshalAs(UnmanagedType.FunctionPtr)] */ CheckEventPredicate predicate, /*XPointer*/ IntPtr arg);

        [DllImport(_dll_name, EntryPoint = "XIfEvent")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool IfEvent(Display display, ref XEvent event_return,
            /*[MarshalAs(UnmanagedType.FunctionPtr)] */ CheckEventPredicate predicate, /*XPointer*/ IntPtr arg);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate bool CheckEventPredicate(Display display, ref XEvent @event, IntPtr arg);

        [DllImport(_dll_name, EntryPoint = "XCheckMaskEvent")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CheckMaskEvent(Display display, EventMask event_mask, ref XEvent event_return);

        #endregion

        #region Pointer and Keyboard functions

        [DllImport(_dll_name, EntryPoint = "XGrabPointer")]
        extern internal static ErrorCodes GrabPointer(Display display, IntPtr grab_window,
            bool owner_events, int event_mask, GrabMode pointer_mode, GrabMode keyboard_mode,
            IntPtr confine_to, IntPtr cursor, int time);

        [DllImport(_dll_name, EntryPoint = "XUngrabPointer")]
        extern internal static ErrorCodes UngrabPointer(Display display, int time);

        [DllImport(_dll_name, EntryPoint = "XGrabKeyboard")]
        extern internal static ErrorCodes GrabKeyboard(Display display, IntPtr grab_window,
            bool owner_events, GrabMode pointer_mode, GrabMode keyboard_mode, int time);

        [DllImport(_dll_name, EntryPoint = "XUngrabKeyboard")]
        extern internal static void UngrabKeyboard(Display display, int time);

        /// <summary>
        /// The XGetKeyboardMapping() function returns the symbols for the specified number of KeyCodes starting with first_keycode.
        /// </summary>
        /// <param name="display">Specifies the connection to the X server.</param>
        /// <param name="first_keycode">Specifies the first KeyCode that is to be returned.</param>
        /// <param name="keycode_count">Specifies the number of KeyCodes that are to be returned</param>
        /// <param name="keysyms_per_keycode_return">Returns the number of KeySyms per KeyCode.</param>
        /// <returns></returns>
        /// <remarks>
        /// <para>The value specified in first_keycode must be greater than or equal to min_keycode as returned by XDisplayKeycodes(), or a BadValue error results. In addition, the following expression must be less than or equal to max_keycode as returned by XDisplayKeycodes(): </para>
        /// <para>first_keycode + keycode_count - 1 </para>
        /// <para>If this is not the case, a BadValue error results. The number of elements in the KeySyms list is: </para>
        /// <para>keycode_count * keysyms_per_keycode_return </para>
        /// <para>KeySym number N, counting from zero, for KeyCode K has the following index in the list, counting from zero: </para>
        /// <para> (K - first_code) * keysyms_per_code_return + N </para>
        /// <para>The X server arbitrarily chooses the keysyms_per_keycode_return value to be large enough to report all requested symbols. A special KeySym value of NoSymbol is used to fill in unused elements for individual KeyCodes. To free the storage returned by XGetKeyboardMapping(), use XFree(). </para>
        /// <para>XGetKeyboardMapping() can generate a BadValue error.</para>
        /// <para>Diagnostics:</para>
        /// <para>BadValue:	Some numeric value falls outside the range of values accepted by the request. Unless a specific range is specified for an argument, the full range defined by the argument's type is accepted. Any argument defined as a set of alternatives can generate this error.</para>
        /// </remarks>
        [DllImport(_dll_name, EntryPoint = "XGetKeyboardMapping")]
        internal static extern KeySym GetKeyboardMapping(Display display, KeyCode first_keycode, int keycode_count,
            ref int keysyms_per_keycode_return);

        /// <summary>
        /// The XDisplayKeycodes() function returns the min-keycodes and max-keycodes supported by the specified display.
        /// </summary>
        /// <param name="display">Specifies the connection to the X server.</param>
        /// <param name="min_keycodes_return">Returns the minimum number of KeyCodes</param>
        /// <param name="max_keycodes_return">Returns the maximum number of KeyCodes.</param>
        /// <remarks> The minimum number of KeyCodes returned is never less than 8, and the maximum number of KeyCodes returned is never greater than 255. Not all KeyCodes in this range are required to have corresponding keys.</remarks>
        [DllImport(_dll_name, EntryPoint = "XDisplayKeycodes")]
        internal static extern void DisplayKeycodes(Display display, ref int min_keycodes_return, ref int max_keycodes_return);

        #endregion

        #region Xf86VidMode internal structures

        [StructLayout(LayoutKind.Sequential)]
        internal struct XF86VidModeModeLine
        {
            short hdisplay;   /* Number of display pixels horizontally */
            short hsyncstart; /* Horizontal sync start */
            short hsyncend;   /* Horizontal sync end */
            short htotal;     /* Total horizontal pixels */
            short vdisplay;   /* Number of display pixels vertically */
            short vsyncstart; /* Vertical sync start */
            short vsyncend;   /* Vertical sync start */
            short vtotal;     /* Total vertical pixels */
            int flags;      /* Mode flags */
            int privsize;   /* Size of private */
            IntPtr _private;   /* Server privates */
        }

        /// <summary>
        /// Specifies an XF86 display mode.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        internal struct XF86VidModeModeInfo
        {
            /// <summary>
            /// Pixel clock.
            /// </summary>
            internal int dotclock;

            /// <summary>
            /// Number of display pixels horizontally
            /// </summary>
            internal short hdisplay;

            /// <summary>
            /// Horizontal sync start
            /// </summary>
            internal short hsyncstart;

            /// <summary>
            /// Horizontal sync end
            /// </summary>
            internal short hsyncend;

            /// <summary>
            /// Total horizontal pixel
            /// </summary>
            internal short htotal;

            /// <summary>
            /// 
            /// </summary>
            internal short hskew;

            /// <summary>
            /// Number of display pixels vertically
            /// </summary>
            internal short vdisplay;

            /// <summary>
            /// Vertical sync start
            /// </summary>
            internal short vsyncstart;

            /// <summary>
            /// Vertical sync end
            /// </summary>
            internal short vsyncend;

            /// <summary>
            /// Total vertical pixels
            /// </summary>
            internal short vtotal;

            /// <summary>
            /// 
            /// </summary>
            internal short vskew;

            /// <summary>
            /// Mode flags
            /// </summary>
            internal int flags;

            int privsize;   /* Size of private */
            IntPtr _private;   /* Server privates */
        }

        //Monitor information:
        [StructLayout(LayoutKind.Sequential)]
        internal struct XF86VidModeMonitor
        {
            [MarshalAs(UnmanagedType.LPStr)]
            string vendor;     /* Name of manufacturer */
            [MarshalAs(UnmanagedType.LPStr)]
            string model;      /* Model name */
            float EMPTY;      /* unused, for backward compatibility */
            byte nhsync;     /* Number of horiz sync ranges */
            /*XF86VidModeSyncRange* */
            IntPtr hsync;/* Horizontal sync ranges */
            byte nvsync;     /* Number of vert sync ranges */
            /*XF86VidModeSyncRange* */
            IntPtr vsync;/* Vertical sync ranges */
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct XF86VidModeSyncRange
        {
            float hi;         /* Top of range */
            float lo;         /* Bottom of range */
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct XF86VidModeNotifyEvent
        {
            int type;                      /* of event */
            ulong serial;          /* # of last request processed by server */
            bool send_event;               /* true if this came from a SendEvent req */
            Display display;              /* Display the event was read from */
            IntPtr root;                   /* root window of event screen */
            int state;                     /* What happened */
            int kind;                      /* What happened */
            bool forced;                   /* extents of new region */
            /* Time */
            IntPtr time;                     /* event timestamp */
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct XF86VidModeGamma
        {
            float red;                     /* Red Gamma value */
            float green;                   /* Green Gamma value */
            float blue;                    /* Blue Gamma value */
        }
        #endregion

        #region libXxf86vm Functions

        [DllImport(_dll_name_vid)]
        extern internal static bool XF86VidModeQueryExtension(
            Display display,
            out int event_base_return,
            out int error_base_return);
        /*
        [DllImport(_dll_name_vid)]
        extern internal static bool XF86VidModeSwitchMode(
            Display display,
            int screen,
            int zoom);
        */

        [DllImport(_dll_name_vid)]
        extern internal static bool XF86VidModeSwitchToMode(
            Display display,
            int screen,
            IntPtr
            /*XF86VidModeModeInfo* */ modeline);


        [DllImport(_dll_name_vid)]
        extern internal static bool XF86VidModeQueryVersion(
            Display display,
            out int major_version_return,
            out int minor_version_return);

        [DllImport(_dll_name_vid)]
        extern internal static bool XF86VidModeGetAllModeLines(
            Display display,
            int screen,
            out int modecount_return,
            /*XF86VidModeModeInfo***  <-- yes, that's three *'s. */
            out IntPtr modesinfo);

        [DllImport(_dll_name_vid)]
        extern internal static bool XF86VidModeSetViewPort(
            Display display,
            int screen,
            int x,
            int y);

        /*
Bool XF86VidModeSetClientVersion(
    Display *display);

Bool XF86VidModeGetModeLine(
    Display *display,
    int screen,
    int *dotclock_return,
    XF86VidModeModeLine *modeline);

Bool XF86VidModeDeleteModeLine(
    Display *display,
    int screen,
    XF86VidModeModeInfo *modeline);

Bool XF86VidModeModModeLine(
    Display *display,
    int screen,
    XF86VidModeModeLine *modeline);

Status XF86VidModeValidateModeLine(
    Display *display,
    int screen,
    XF86VidModeModeLine *modeline);


Bool XF86VidModeLockModeSwitch(
    Display *display,
    int screen,
    int lock);

Bool XF86VidModeGetMonitor(
    Display *display,
    int screen,
    XF86VidModeMonitor *monitor);

Bool XF86VidModeGetViewPort(
    Display *display,
    int screen,
    int *x_return,
    int *y_return);


XF86VidModeGetDotClocks(
    Display *display,
    int screen,
    int *flags return,
    int *number of clocks return,
    int *max dot clock return,
    int **clocks return);

XF86VidModeGetGamma(
    Display *display,
    int screen,
    XF86VidModeGamma *Gamma);

XF86VidModeSetGamma(
    Display *display,
    int screen,
    XF86VidModeGamma *Gamma);

XF86VidModeGetGammaRamp(
    Display *display,
    int screen,
    int size,
    unsigned short *red array,
    unsigned short *green array,
    unsigned short *blue array);

XF86VidModeSetGammaRamp(
    Display *display,
    int screen,
    int size,
    unsigned short *red array,
    unsigned short *green array,
    unsigned short *blue array);

XF86VidModeGetGammaRampSize(
    Display *display,
    int screen,
    int *size);
         * */

        #endregion

        [DllImport(_dll_name, EntryPoint = "XLookupKeysym")]
        internal static extern KeySym LookupKeysym(ref XKeyEvent key_event, int index);

    }
    #endregion

    #region X11 Structures

    #region internal class VisualInfo

    /// <summary>
    /// 
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public class VisualInfo
    {
        internal IntPtr visual;
        internal int visualid;
        internal int screen;
        internal int depth;
        internal int @class;
        internal long redMask;
        internal long greenMask;
        internal long blueMask;
        internal int colormap_size;
        internal int bits_per_rgb;

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            // return base.ToString();
            return String.Format("id ({0}), screen ({1}), depth ({2}), class ({3})",
                visualid, screen, depth, @class);
        }
    }

    #endregion

    #region internal class SetWindowAttributes

    [StructLayout(LayoutKind.Sequential)]
    internal class SetWindowAttributes
    {
        /// <summary>
        /// background, None, or ParentRelative
        /// </summary>
        internal Pixmap background_pixmap;
        /// <summary>
        /// background pixel
        /// </summary>
        internal long background_pixel;
        /// <summary>
        /// border of the window or CopyFromParent
        /// </summary>
        internal Pixmap border_pixmap;
        /// <summary>
        /// border pixel value
        /// </summary>
        internal long border_pixel;
        /// <summary>
        /// one of bit gravity values
        /// </summary>
        internal int bit_gravity;
        /// <summary>
        /// one of the window gravity values
        /// </summary>
        internal int win_gravity;
        /// <summary>
        /// NotUseful, WhenMapped, Always
        /// </summary>
        internal int backing_store;
        /// <summary>
        /// planes to be preserved if possible
        /// </summary>
        internal long backing_planes;
        /// <summary>
        /// value to use in restoring planes
        /// </summary>
        internal long backing_pixel;
        /// <summary>
        /// should bits under be saved? (popups)
        /// </summary>
        internal bool save_under;
        /// <summary>
        /// set of events that should be saved
        /// </summary>
        internal EventMask event_mask;
        /// <summary>
        /// set of events that should not propagate
        /// </summary>
        internal long do_not_propagate_mask;
        /// <summary>
        /// boolean value for override_redirect
        /// </summary>
        internal bool override_redirect;
        /// <summary>
        /// color map to be associated with window
        /// </summary>
        internal Colormap colormap;
        /// <summary>
        /// cursor to be displayed (or None)
        /// </summary>
        internal Cursor cursor;
    }

    #endregion

    #region internal struct SizeHints

    [StructLayout(LayoutKind.Sequential)]
    internal struct SizeHints
    {
        internal long flags;         /* marks which fields in this structure are defined */
        internal int x, y;           /* Obsolete */
        internal int width, height;  /* Obsolete */
        internal int min_width, min_height;
        internal int max_width, max_height;
        internal int width_inc, height_inc;
        internal Rectangle min_aspect, max_aspect;
        internal int base_width, base_height;
        internal int win_gravity;
        internal struct Rectangle
        {
            internal int x;       /* numerator */
            internal int y;       /* denominator */
            private void stop_the_compiler_warnings() { x = y = 0; }
        }
        /* this structure may be extended in the future */
    }

    #endregion

    #endregion

    #region X11 Constants and Enums

    internal struct Constants
    {
        internal const int QueuedAlready = 0;
        internal const int QueuedAfterReading = 1;
        internal const int QueuedAfterFlush = 2;

        internal const int CopyFromParent	= 0;
        internal const int CWX = 1;
        internal const int InputOutput = 1;
        internal const int InputOnly = 2;
    }

    internal enum ErrorCodes : int
    {
        Success = 0,
        BadRequest = 1,
        BadValue = 2,
        BadWindow = 3,
        BadPixmap = 4,
        BadAtom = 5,
        BadCursor = 6,
        BadFont = 7,
        BadMatch = 8,
        BadDrawable = 9,
        BadAccess = 10,
        BadAlloc = 11,
        BadColor = 12,
        BadGC = 13,
        BadIDChoice = 14,
        BadName = 15,
        BadLength = 16,
        BadImplementation = 17,
    }

    [Flags]
    internal enum CreateWindowMask : long//: ulong
    {
        CWBackPixmap	= (1L<<0),
        CWBackPixel     = (1L<<1),
        CWSaveUnder	    = (1L<<10),
        CWEventMask	    = (1L<<11),
        CWDontPropagate	= (1L<<12),
        CWColormap  	= (1L<<13),
        CWCursor	    = (1L<<14),
        CWBorderPixmap	= (1L<<2),
        CWBorderPixel	= (1L<<3),
        CWBitGravity	= (1L<<4),
        CWWinGravity	= (1L<<5),
        CWBackingStore	= (1L<<6),
        CWBackingPlanes	= (1L<<7),
        CWBackingPixel 	= (1L<<8),
        CWOverrideRedirect	= (1L<<9),

        //CWY	= (1<<1),
        //CWWidth	= (1<<2),
        //CWHeight	= (1<<3),
        //CWBorderWidth	= (1<<4),
        //CWSibling	= (1<<5),
        //CWStackMode	= (1<<6),
    }

    #region XKey

    /// <summary>
    /// Defines LATIN-1 and miscellaneous keys.
    /// </summary>
    internal enum XKey
    {
        /*
         * TTY function keys, cleverly chosen to map to ASCII, for convenience of
         * programming, but could have been arbitrary (at the cost of lookup
         * tables in client code).
         */

        BackSpace                   = 0xff08,  /* Back space, back char */
        Tab                         = 0xff09,
        Linefeed                    = 0xff0a,  /* Linefeed, LF */
        Clear                       = 0xff0b,
        Return                      = 0xff0d,  /* Return, enter */
        Pause                       = 0xff13,  /* Pause, hold */
        Scroll_Lock                 = 0xff14,
        Sys_Req                     = 0xff15,
        Escape                      = 0xff1b,
        Delete                      = 0xffff,  /* Delete, rubout */



        /* International & multi-key character composition */

        Multi_key                   = 0xff20,  /* Multi-key character compose */
        Codeinput                   = 0xff37,
        SingleCandidate             = 0xff3c,
        MultipleCandidate           = 0xff3d,
        PreviousCandidate           = 0xff3e,
                
        /* Japanese keyboard support */

        Kanji                       = 0xff21,  /* Kanji, Kanji convert */
        Muhenkan                    = 0xff22,  /* Cancel Conversion */
        Henkan_Mode                 = 0xff23,  /* Start/Stop Conversion */
        Henkan                      = 0xff23,  /* Alias for Henkan_Mode */
        Romaji                      = 0xff24,  /* to Romaji */
        Hiragana                    = 0xff25,  /* to Hiragana */
        Katakana                    = 0xff26,  /* to Katakana */
        Hiragana_Katakana           = 0xff27,  /* Hiragana/Katakana toggle */
        Zenkaku                     = 0xff28,  /* to Zenkaku */
        Hankaku                     = 0xff29,  /* to Hankaku */
        Zenkaku_Hankaku             = 0xff2a,  /* Zenkaku/Hankaku toggle */
        Touroku                     = 0xff2b,  /* Add to Dictionary */
        Massyo                      = 0xff2c,  /* Delete from Dictionary */
        Kana_Lock                   = 0xff2d,  /* Kana Lock */
        Kana_Shift                  = 0xff2e,  /* Kana Shift */
        Eisu_Shift                  = 0xff2f,  /* Alphanumeric Shift */
        Eisu_toggle                 = 0xff30,  /* Alphanumeric toggle */
        Kanji_Bangou                = 0xff37,  /* Codeinput */
        Zen_Koho                    = 0xff3d,  /* Multiple/All Candidate(s) */
        Mae_Koho                    = 0xff3e,  /* Previous Candidate */

        /* 0xff31 thru 0xff3f are under XK_KOREAN */

        /* Cursor control & motion */

        Home                        = 0xff50,
        Left                        = 0xff51,  /* Move left, left arrow */
        Up                          = 0xff52,  /* Move up, up arrow */
        Right                       = 0xff53,  /* Move right, right arrow */
        Down                        = 0xff54,  /* Move down, down arrow */
        Prior                       = 0xff55,  /* Prior, previous */
        Page_Up                     = 0xff55,
        Next                        = 0xff56,  /* Next */
        Page_Down                   = 0xff56,
        End                         = 0xff57,  /* EOL */
        Begin                       = 0xff58,  /* BOL */


        /* Misc functions */

        Select                      = 0xff60,  /* Select, mark */
        Print                       = 0xff61,
        Execute                     = 0xff62,  /* Execute, run, do */
        Insert                      = 0xff63,  /* Insert, insert here */
        Undo                        = 0xff65,
        Redo                        = 0xff66,  /* Redo, again */
        Menu                        = 0xff67,
        Find                        = 0xff68,  /* Find, search */
        Cancel                      = 0xff69,  /* Cancel, stop, abort, exit */
        Help                        = 0xff6a,  /* Help */
        Break                       = 0xff6b,
        Mode_switch                 = 0xff7e,  /* Character set switch */
        script_switch               = 0xff7e,  /* Alias for mode_switch */
        Num_Lock                    = 0xff7f,

        /* Keypad functions, keypad numbers cleverly chosen to map to ASCII */

        KP_Space                    = 0xff80,  /* Space */
        KP_Tab                      = 0xff89,
        KP_Enter                    = 0xff8d,  /* Enter */
        KP_F1                       = 0xff91,  /* PF1, KP_A, ... */
        KP_F2                       = 0xff92,
        KP_F3                       = 0xff93,
        KP_F4                       = 0xff94,
        KP_Home                     = 0xff95,
        KP_Left                     = 0xff96,
        KP_Up                       = 0xff97,
        KP_Right                    = 0xff98,
        KP_Down                     = 0xff99,
        KP_Prior                    = 0xff9a,
        KP_Page_Up                  = 0xff9a,
        KP_Next                     = 0xff9b,
        KP_Page_Down                = 0xff9b,
        KP_End                      = 0xff9c,
        KP_Begin                    = 0xff9d,
        KP_Insert                   = 0xff9e,
        KP_Delete                   = 0xff9f,
        KP_Equal                    = 0xffbd,  /* Equals */
        KP_Multiply                 = 0xffaa,
        KP_Add                      = 0xffab,
        KP_Separator                = 0xffac,  /* Separator, often comma */
        KP_Subtract                 = 0xffad,
        KP_Decimal                  = 0xffae,
        KP_Divide                   = 0xffaf,

        KP_0                        = 0xffb0,
        KP_1                        = 0xffb1,
        KP_2                        = 0xffb2,
        KP_3                        = 0xffb3,
        KP_4                        = 0xffb4,
        KP_5                        = 0xffb5,
        KP_6                        = 0xffb6,
        KP_7                        = 0xffb7,
        KP_8                        = 0xffb8,
        KP_9                        = 0xffb9,

        /*
         * Auxiliary functions; note the duplicate definitions for left and right
         * function keys;  Sun keyboards and a few other manufacturers have such
         * function key groups on the left and/or right sides of the keyboard.
         * We've not found a keyboard with more than 35 function keys total.
         */

        F1                          = 0xffbe,
        F2                          = 0xffbf,
        F3                          = 0xffc0,
        F4                          = 0xffc1,
        F5                          = 0xffc2,
        F6                          = 0xffc3,
        F7                          = 0xffc4,
        F8                          = 0xffc5,
        F9                          = 0xffc6,
        F10                         = 0xffc7,
        F11                         = 0xffc8,
        L1                          = 0xffc8,
        F12                         = 0xffc9,
        L2                          = 0xffc9,
        F13                         = 0xffca,
        L3                          = 0xffca,
        F14                         = 0xffcb,
        L4                          = 0xffcb,
        F15                         = 0xffcc,
        L5                          = 0xffcc,
        F16                         = 0xffcd,
        L6                          = 0xffcd,
        F17                         = 0xffce,
        L7                          = 0xffce,
        F18                         = 0xffcf,
        L8                          = 0xffcf,
        F19                         = 0xffd0,
        L9                          = 0xffd0,
        F20                         = 0xffd1,
        L10                         = 0xffd1,
        F21                         = 0xffd2,
        R1                          = 0xffd2,
        F22                         = 0xffd3,
        R2                          = 0xffd3,
        F23                         = 0xffd4,
        R3                          = 0xffd4,
        F24                         = 0xffd5,
        R4                          = 0xffd5,
        F25                         = 0xffd6,
        R5                          = 0xffd6,
        F26                         = 0xffd7,
        R6                          = 0xffd7,
        F27                         = 0xffd8,
        R7                          = 0xffd8,
        F28                         = 0xffd9,
        R8                          = 0xffd9,
        F29                         = 0xffda,
        R9                          = 0xffda,
        F30                         = 0xffdb,
        R10                         = 0xffdb,
        F31                         = 0xffdc,
        R11                         = 0xffdc,
        F32                         = 0xffdd,
        R12                         = 0xffdd,
        F33                         = 0xffde,
        R13                         = 0xffde,
        F34                         = 0xffdf,
        R14                         = 0xffdf,
        F35                         = 0xffe0,
        R15                         = 0xffe0,

        /* Modifiers */

        Shift_L                     = 0xffe1,  /* Left shift */
        Shift_R                     = 0xffe2,  /* Right shift */
        Control_L                   = 0xffe3,  /* Left control */
        Control_R                   = 0xffe4,  /* Right control */
        Caps_Lock                   = 0xffe5,  /* Caps lock */
        Shift_Lock                  = 0xffe6,  /* Shift lock */

        Meta_L                      = 0xffe7,  /* Left meta */
        Meta_R                      = 0xffe8,  /* Right meta */
        Alt_L                       = 0xffe9,  /* Left alt */
        Alt_R                       = 0xffea,  /* Right alt */
        Super_L                     = 0xffeb,  /* Left super */
        Super_R                     = 0xffec,  /* Right super */
        Hyper_L                     = 0xffed,  /* Left hyper */
        Hyper_R                     = 0xffee,  /* Right hyper */

        /*
         * Latin 1
         * (ISO/IEC 8859-1 = Unicode U+0020..U+00FF)
         * Byte 3 = 0
         */

        space                       = 0x0020,  /* U+0020 SPACE */
        exclam                      = 0x0021,  /* U+0021 EXCLAMATION MARK */
        quotedbl                    = 0x0022,  /* U+0022 QUOTATION MARK */
        numbersign                  = 0x0023,  /* U+0023 NUMBER SIGN */
        dollar                      = 0x0024,  /* U+0024 DOLLAR SIGN */
        percent                     = 0x0025,  /* U+0025 PERCENT SIGN */
        ampersand                   = 0x0026,  /* U+0026 AMPERSAND */
        apostrophe                  = 0x0027,  /* U+0027 APOSTROPHE */
        quoteright                  = 0x0027,  /* deprecated */
        parenleft                   = 0x0028,  /* U+0028 LEFT PARENTHESIS */
        parenright                  = 0x0029,  /* U+0029 RIGHT PARENTHESIS */
        asterisk                    = 0x002a,  /* U+002A ASTERISK */
        plus                        = 0x002b,  /* U+002B PLUS SIGN */
        comma                       = 0x002c,  /* U+002C COMMA */
        minus                       = 0x002d,  /* U+002D HYPHEN-MINUS */
        period                      = 0x002e,  /* U+002E FULL STOP */
        slash                       = 0x002f,  /* U+002F SOLIDUS */
        Number0                           = 0x0030,  /* U+0030 DIGIT ZERO */
        Number1                           = 0x0031,  /* U+0031 DIGIT ONE */
        Number2                           = 0x0032,  /* U+0032 DIGIT TWO */
        Number3                           = 0x0033,  /* U+0033 DIGIT THREE */
        Number4                           = 0x0034,  /* U+0034 DIGIT FOUR */
        Number5                           = 0x0035,  /* U+0035 DIGIT FIVE */
        Number6                           = 0x0036,  /* U+0036 DIGIT SIX */
        Number7                           = 0x0037,  /* U+0037 DIGIT SEVEN */
        Number8                           = 0x0038,  /* U+0038 DIGIT EIGHT */
        Number9                     = 0x0039,  /* U+0039 DIGIT NINE */
        colon                       = 0x003a,  /* U+003A COLON */
        semicolon                   = 0x003b,  /* U+003B SEMICOLON */
        less                        = 0x003c,  /* U+003C LESS-THAN SIGN */
        equal                       = 0x003d,  /* U+003D EQUALS SIGN */
        greater                     = 0x003e,  /* U+003E GREATER-THAN SIGN */
        question                    = 0x003f,  /* U+003F QUESTION MARK */
        at                          = 0x0040,  /* U+0040 COMMERCIAL AT */
        A                           = 0x0041,  /* U+0041 LATIN CAPITAL LETTER A */
        B                           = 0x0042,  /* U+0042 LATIN CAPITAL LETTER B */
        C                           = 0x0043,  /* U+0043 LATIN CAPITAL LETTER C */
        D                           = 0x0044,  /* U+0044 LATIN CAPITAL LETTER D */
        E                           = 0x0045,  /* U+0045 LATIN CAPITAL LETTER E */
        F                           = 0x0046,  /* U+0046 LATIN CAPITAL LETTER F */
        G                           = 0x0047,  /* U+0047 LATIN CAPITAL LETTER G */
        H                           = 0x0048,  /* U+0048 LATIN CAPITAL LETTER H */
        I                           = 0x0049,  /* U+0049 LATIN CAPITAL LETTER I */
        J                           = 0x004a,  /* U+004A LATIN CAPITAL LETTER J */
        K                           = 0x004b,  /* U+004B LATIN CAPITAL LETTER K */
        L                           = 0x004c,  /* U+004C LATIN CAPITAL LETTER L */
        M                           = 0x004d,  /* U+004D LATIN CAPITAL LETTER M */
        N                           = 0x004e,  /* U+004E LATIN CAPITAL LETTER N */
        O                           = 0x004f,  /* U+004F LATIN CAPITAL LETTER O */
        P                           = 0x0050,  /* U+0050 LATIN CAPITAL LETTER P */
        Q                           = 0x0051,  /* U+0051 LATIN CAPITAL LETTER Q */
        R                           = 0x0052,  /* U+0052 LATIN CAPITAL LETTER R */
        S                           = 0x0053,  /* U+0053 LATIN CAPITAL LETTER S */
        T                           = 0x0054,  /* U+0054 LATIN CAPITAL LETTER T */
        U                           = 0x0055,  /* U+0055 LATIN CAPITAL LETTER U */
        V                           = 0x0056,  /* U+0056 LATIN CAPITAL LETTER V */
        W                           = 0x0057,  /* U+0057 LATIN CAPITAL LETTER W */
        X                           = 0x0058,  /* U+0058 LATIN CAPITAL LETTER X */
        Y                           = 0x0059,  /* U+0059 LATIN CAPITAL LETTER Y */
        Z                           = 0x005a,  /* U+005A LATIN CAPITAL LETTER Z */
        bracketleft                 = 0x005b,  /* U+005B LEFT SQUARE BRACKET */
        backslash                   = 0x005c,  /* U+005C REVERSE SOLIDUS */
        bracketright                = 0x005d,  /* U+005D RIGHT SQUARE BRACKET */
        asciicircum                 = 0x005e,  /* U+005E CIRCUMFLEX ACCENT */
        underscore                  = 0x005f,  /* U+005F LOW LINE */
        grave                       = 0x0060,  /* U+0060 GRAVE ACCENT */
        quoteleft                   = 0x0060,  /* deprecated */
        a                           = 0x0061,  /* U+0061 LATIN SMALL LETTER A */
        b                           = 0x0062,  /* U+0062 LATIN SMALL LETTER B */
        c                           = 0x0063,  /* U+0063 LATIN SMALL LETTER C */
        d                           = 0x0064,  /* U+0064 LATIN SMALL LETTER D */
        e                           = 0x0065,  /* U+0065 LATIN SMALL LETTER E */
        f                           = 0x0066,  /* U+0066 LATIN SMALL LETTER F */
        g                           = 0x0067,  /* U+0067 LATIN SMALL LETTER G */
        h                           = 0x0068,  /* U+0068 LATIN SMALL LETTER H */
        i                           = 0x0069,  /* U+0069 LATIN SMALL LETTER I */
        j                           = 0x006a,  /* U+006A LATIN SMALL LETTER J */
        k                           = 0x006b,  /* U+006B LATIN SMALL LETTER K */
        l                           = 0x006c,  /* U+006C LATIN SMALL LETTER L */
        m                           = 0x006d,  /* U+006D LATIN SMALL LETTER M */
        n                           = 0x006e,  /* U+006E LATIN SMALL LETTER N */
        o                           = 0x006f,  /* U+006F LATIN SMALL LETTER O */
        p                           = 0x0070,  /* U+0070 LATIN SMALL LETTER P */
        q                           = 0x0071,  /* U+0071 LATIN SMALL LETTER Q */
        r                           = 0x0072,  /* U+0072 LATIN SMALL LETTER R */
        s                           = 0x0073,  /* U+0073 LATIN SMALL LETTER S */
        t                           = 0x0074,  /* U+0074 LATIN SMALL LETTER T */
        u                           = 0x0075,  /* U+0075 LATIN SMALL LETTER U */
        v                           = 0x0076,  /* U+0076 LATIN SMALL LETTER V */
        w                           = 0x0077,  /* U+0077 LATIN SMALL LETTER W */
        x                           = 0x0078,  /* U+0078 LATIN SMALL LETTER X */
        y                           = 0x0079,  /* U+0079 LATIN SMALL LETTER Y */
        z                           = 0x007a,  /* U+007A LATIN SMALL LETTER Z */
        braceleft                   = 0x007b,  /* U+007B LEFT CURLY BRACKET */
        bar                         = 0x007c,  /* U+007C VERTICAL LINE */
        braceright                  = 0x007d,  /* U+007D RIGHT CURLY BRACKET */
        asciitilde                  = 0x007e,  /* U+007E TILDE */
    }

    #endregion

    #endregion
}
