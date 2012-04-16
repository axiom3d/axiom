namespace Tao.Platform.Windows
{
    using System;
    using System.Runtime.InteropServices;

    partial class Wgl
    {

        internal static partial class Imports
        {

            [System.Security.SuppressUnmanagedCodeSecurity()]
            [System.Runtime.InteropServices.DllImport(Wgl.Library, EntryPoint = "wglCreateContext", ExactSpelling = true)]
            internal extern static IntPtr CreateContext(IntPtr hDc);
            [System.Security.SuppressUnmanagedCodeSecurity()]
            [System.Runtime.InteropServices.DllImport(Wgl.Library, EntryPoint = "wglDeleteContext", ExactSpelling = true)]
            internal extern static Boolean DeleteContext(IntPtr oldContext);
            [System.Security.SuppressUnmanagedCodeSecurity()]
            [System.Runtime.InteropServices.DllImport(Wgl.Library, EntryPoint = "wglGetCurrentContext", ExactSpelling = true)]
            internal extern static IntPtr GetCurrentContext();
            [System.Security.SuppressUnmanagedCodeSecurity()]
            [System.Runtime.InteropServices.DllImport(Wgl.Library, EntryPoint = "wglMakeCurrent", ExactSpelling = true)]
            internal extern static Boolean MakeCurrent(IntPtr hDc, IntPtr newContext);
            [System.Security.SuppressUnmanagedCodeSecurity()]
            [System.Runtime.InteropServices.DllImport(Wgl.Library, EntryPoint = "wglCopyContext", ExactSpelling = true)]
            internal extern static Boolean CopyContext(IntPtr hglrcSrc, IntPtr hglrcDst, UInt32 mask);
            [System.Security.SuppressUnmanagedCodeSecurity()]
            [System.Runtime.InteropServices.DllImport(Wgl.Library, EntryPoint = "wglChoosePixelFormat", ExactSpelling = true)]
            internal extern static unsafe int ChoosePixelFormat(IntPtr hDc, Gdi.PIXELFORMATDESCRIPTOR* pPfd);
            [System.Security.SuppressUnmanagedCodeSecurity()]
            [System.Runtime.InteropServices.DllImport(Wgl.Library, EntryPoint = "wglDescribePixelFormat", ExactSpelling = true)]
            internal extern static unsafe int DescribePixelFormat(IntPtr hdc, int ipfd, UInt32 cjpfd, Gdi.PIXELFORMATDESCRIPTOR* ppfd);
            [System.Security.SuppressUnmanagedCodeSecurity()]
            [System.Runtime.InteropServices.DllImport(Wgl.Library, EntryPoint = "wglGetCurrentDC", ExactSpelling = true)]
            internal extern static IntPtr GetCurrentDC();
            [System.Security.SuppressUnmanagedCodeSecurity()]
            [System.Runtime.InteropServices.DllImport(Wgl.Library, EntryPoint = "wglGetDefaultProcAddress", ExactSpelling = true)]
            internal extern static IntPtr GetDefaultProcAddress(String lpszProc);
            [System.Security.SuppressUnmanagedCodeSecurity()]
            [System.Runtime.InteropServices.DllImport(Wgl.Library, EntryPoint = "wglGetProcAddress", ExactSpelling = true)]
            internal extern static IntPtr GetProcAddress(String lpszProc);
            [System.Security.SuppressUnmanagedCodeSecurity()]
            [System.Runtime.InteropServices.DllImport(Wgl.Library, EntryPoint = "wglGetPixelFormat", ExactSpelling = true)]
            internal extern static int GetPixelFormat(IntPtr hdc);
            [System.Security.SuppressUnmanagedCodeSecurity()]
            [System.Runtime.InteropServices.DllImport(Wgl.Library, EntryPoint = "wglSetPixelFormat", ExactSpelling = true)]
            internal extern static unsafe Boolean SetPixelFormat(IntPtr hdc, int ipfd, Gdi.PIXELFORMATDESCRIPTOR* ppfd);
            [System.Security.SuppressUnmanagedCodeSecurity()]
            [System.Runtime.InteropServices.DllImport(Wgl.Library, EntryPoint = "wglSwapBuffers", ExactSpelling = true)]
            internal extern static Boolean SwapBuffers(IntPtr hdc);
            [System.Security.SuppressUnmanagedCodeSecurity()]
            [System.Runtime.InteropServices.DllImport(Wgl.Library, EntryPoint = "wglShareLists", ExactSpelling = true)]
            internal extern static Boolean ShareLists(IntPtr hrcSrvShare, IntPtr hrcSrvSource);
            [System.Security.SuppressUnmanagedCodeSecurity()]
            [System.Runtime.InteropServices.DllImport(Wgl.Library, EntryPoint = "wglCreateLayerContext", ExactSpelling = true)]
            internal extern static IntPtr CreateLayerContext(IntPtr hDc, int level);
            [System.Security.SuppressUnmanagedCodeSecurity()]
            [System.Runtime.InteropServices.DllImport(Wgl.Library, EntryPoint = "wglDescribeLayerPlane", ExactSpelling = true)]
            internal extern static unsafe Boolean DescribeLayerPlane(IntPtr hDc, int pixelFormat, int layerPlane, UInt32 nBytes, Gdi.LAYERPLANEDESCRIPTOR* plpd);
            [System.Security.SuppressUnmanagedCodeSecurity()]
            [System.Runtime.InteropServices.DllImport(Wgl.Library, EntryPoint = "wglSetLayerPaletteEntries", ExactSpelling = true)]
            internal extern static unsafe int SetLayerPaletteEntries(IntPtr hdc, int iLayerPlane, int iStart, int cEntries, Int32* pcr);
            [System.Security.SuppressUnmanagedCodeSecurity()]
            [System.Runtime.InteropServices.DllImport(Wgl.Library, EntryPoint = "wglGetLayerPaletteEntries", ExactSpelling = true)]
            internal extern static unsafe int GetLayerPaletteEntries(IntPtr hdc, int iLayerPlane, int iStart, int cEntries, Int32* pcr);
            [System.Security.SuppressUnmanagedCodeSecurity()]
            [System.Runtime.InteropServices.DllImport(Wgl.Library, EntryPoint = "wglRealizeLayerPalette", ExactSpelling = true)]
            internal extern static Boolean RealizeLayerPalette(IntPtr hdc, int iLayerPlane, Boolean bRealize);
            [System.Security.SuppressUnmanagedCodeSecurity()]
            [System.Runtime.InteropServices.DllImport(Wgl.Library, EntryPoint = "wglSwapLayerBuffers", ExactSpelling = true)]
            internal extern static Boolean SwapLayerBuffers(IntPtr hdc, UInt32 fuFlags);
            [System.Security.SuppressUnmanagedCodeSecurity()]
            [System.Runtime.InteropServices.DllImport(Wgl.Library, EntryPoint = "wglUseFontBitmapsA", CharSet = CharSet.Auto)]
            internal extern static Boolean UseFontBitmapsA(IntPtr hDC, Int32 first, Int32 count, Int32 listBase);
            [System.Security.SuppressUnmanagedCodeSecurity()]
            [System.Runtime.InteropServices.DllImport(Wgl.Library, EntryPoint = "wglUseFontBitmapsW", CharSet = CharSet.Auto)]
            internal extern static Boolean UseFontBitmapsW(IntPtr hDC, Int32 first, Int32 count, Int32 listBase);
            [System.Security.SuppressUnmanagedCodeSecurity()]
            [System.Runtime.InteropServices.DllImport(Wgl.Library, EntryPoint = "wglUseFontOutlinesA", CharSet = CharSet.Auto)]
            internal extern static unsafe Boolean UseFontOutlinesA(IntPtr hDC, Int32 first, Int32 count, Int32 listBase, float thickness, float deviation, Int32 fontMode, Gdi.GLYPHMETRICSFLOAT* glyphMetrics);
            [System.Security.SuppressUnmanagedCodeSecurity()]
            [System.Runtime.InteropServices.DllImport(Wgl.Library, EntryPoint = "wglUseFontOutlinesW", CharSet = CharSet.Auto)]
            internal extern static unsafe Boolean UseFontOutlinesW(IntPtr hDC, Int32 first, Int32 count, Int32 listBase, float thickness, float deviation, Int32 fontMode, Gdi.GLYPHMETRICSFLOAT* glyphMetrics);
        }
    }
}
