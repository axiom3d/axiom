namespace Tao.Platform.Windows
{
    using System;
    using System.Runtime.InteropServices;

    public static partial class Wgl
    {

        public static 
        IntPtr wglCreateContext(IntPtr hDc)
        {
            return Delegates.wglCreateContext((IntPtr)hDc);
        }

        public static 
        Boolean wglDeleteContext(IntPtr oldContext)
        {
            return Delegates.wglDeleteContext((IntPtr)oldContext);
        }

        public static 
        IntPtr wglGetCurrentContext()
        {
            return Delegates.wglGetCurrentContext();
        }

        public static 
        Boolean wglMakeCurrent(IntPtr hDc, IntPtr newContext)
        {
            return Delegates.wglMakeCurrent((IntPtr)hDc, (IntPtr)newContext);
        }

        [System.CLSCompliant(false)]
        public static 
        Boolean wglCopyContext(IntPtr hglrcSrc, IntPtr hglrcDst, UInt32 mask)
        {
            return Delegates.wglCopyContext((IntPtr)hglrcSrc, (IntPtr)hglrcDst, (UInt32)mask);
        }

        public static 
        Boolean wglCopyContext(IntPtr hglrcSrc, IntPtr hglrcDst, Int32 mask)
        {
            return Delegates.wglCopyContext((IntPtr)hglrcSrc, (IntPtr)hglrcDst, (UInt32)mask);
        }

        public static 
        int wglChoosePixelFormat(IntPtr hDc, Gdi.PIXELFORMATDESCRIPTOR[] pPfd)
        {
            unsafe
            {
                fixed (Gdi.PIXELFORMATDESCRIPTOR* pPfd_ptr = pPfd)
                {
                    return Delegates.wglChoosePixelFormat((IntPtr)hDc, (Gdi.PIXELFORMATDESCRIPTOR*)pPfd_ptr);
                }
            }
        }

        public static 
        int wglChoosePixelFormat(IntPtr hDc, ref Gdi.PIXELFORMATDESCRIPTOR pPfd)
        {
            unsafe
            {
                fixed (Gdi.PIXELFORMATDESCRIPTOR* pPfd_ptr = &pPfd)
                {
                    return Delegates.wglChoosePixelFormat((IntPtr)hDc, (Gdi.PIXELFORMATDESCRIPTOR*)pPfd_ptr);
                }
            }
        }

        [System.CLSCompliant(false)]
        public static 
        int wglChoosePixelFormat(IntPtr hDc, IntPtr pPfd)
        {
            unsafe
            {
                return Delegates.wglChoosePixelFormat((IntPtr)hDc, (Gdi.PIXELFORMATDESCRIPTOR*)pPfd);
            }
        }

        [System.CLSCompliant(false)]
        public static 
        int wglDescribePixelFormat(IntPtr hdc, int ipfd, UInt32 cjpfd, Gdi.PIXELFORMATDESCRIPTOR[] ppfd)
        {
            unsafe
            {
                fixed (Gdi.PIXELFORMATDESCRIPTOR* ppfd_ptr = ppfd)
                {
                    return Delegates.wglDescribePixelFormat((IntPtr)hdc, (int)ipfd, (UInt32)cjpfd, (Gdi.PIXELFORMATDESCRIPTOR*)ppfd_ptr);
                }
            }
        }

        public static 
        int wglDescribePixelFormat(IntPtr hdc, int ipfd, Int32 cjpfd, Gdi.PIXELFORMATDESCRIPTOR[] ppfd)
        {
            unsafe
            {
                fixed (Gdi.PIXELFORMATDESCRIPTOR* ppfd_ptr = ppfd)
                {
                    return Delegates.wglDescribePixelFormat((IntPtr)hdc, (int)ipfd, (UInt32)cjpfd, (Gdi.PIXELFORMATDESCRIPTOR*)ppfd_ptr);
                }
            }
        }

        [System.CLSCompliant(false)]
        public static 
        int wglDescribePixelFormat(IntPtr hdc, int ipfd, UInt32 cjpfd, ref Gdi.PIXELFORMATDESCRIPTOR ppfd)
        {
            unsafe
            {
                fixed (Gdi.PIXELFORMATDESCRIPTOR* ppfd_ptr = &ppfd)
                {
                    return Delegates.wglDescribePixelFormat((IntPtr)hdc, (int)ipfd, (UInt32)cjpfd, (Gdi.PIXELFORMATDESCRIPTOR*)ppfd_ptr);
                }
            }
        }

        public static 
        int wglDescribePixelFormat(IntPtr hdc, int ipfd, Int32 cjpfd, ref Gdi.PIXELFORMATDESCRIPTOR ppfd)
        {
            unsafe
            {
                fixed (Gdi.PIXELFORMATDESCRIPTOR* ppfd_ptr = &ppfd)
                {
                    return Delegates.wglDescribePixelFormat((IntPtr)hdc, (int)ipfd, (UInt32)cjpfd, (Gdi.PIXELFORMATDESCRIPTOR*)ppfd_ptr);
                }
            }
        }

        [System.CLSCompliant(false)]
        public static 
        int wglDescribePixelFormat(IntPtr hdc, int ipfd, UInt32 cjpfd, IntPtr ppfd)
        {
            unsafe
            {
                return Delegates.wglDescribePixelFormat((IntPtr)hdc, (int)ipfd, (UInt32)cjpfd, (Gdi.PIXELFORMATDESCRIPTOR*)ppfd);
            }
        }

        [System.CLSCompliant(false)]
        public static 
        int wglDescribePixelFormat(IntPtr hdc, int ipfd, Int32 cjpfd, IntPtr ppfd)
        {
            unsafe
            {
                return Delegates.wglDescribePixelFormat((IntPtr)hdc, (int)ipfd, (UInt32)cjpfd, (Gdi.PIXELFORMATDESCRIPTOR*)ppfd);
            }
        }

        public static 
        IntPtr wglGetCurrentDC()
        {
            return Delegates.wglGetCurrentDC();
        }

        public static 
        IntPtr wglGetDefaultProcAddress(String lpszProc)
        {
            return Delegates.wglGetDefaultProcAddress((String)lpszProc);
        }

        public static 
        IntPtr wglGetProcAddress(String lpszProc)
        {
            return Delegates.wglGetProcAddress((String)lpszProc);
        }

        public static 
        int wglGetPixelFormat(IntPtr hdc)
        {
            return Delegates.wglGetPixelFormat((IntPtr)hdc);
        }

        public static 
        Boolean wglSetPixelFormat(IntPtr hdc, int ipfd, Gdi.PIXELFORMATDESCRIPTOR[] ppfd)
        {
            unsafe
            {
                fixed (Gdi.PIXELFORMATDESCRIPTOR* ppfd_ptr = ppfd)
                {
                    return Delegates.wglSetPixelFormat((IntPtr)hdc, (int)ipfd, (Gdi.PIXELFORMATDESCRIPTOR*)ppfd_ptr);
                }
            }
        }

        public static 
        Boolean wglSetPixelFormat(IntPtr hdc, int ipfd, ref Gdi.PIXELFORMATDESCRIPTOR ppfd)
        {
            unsafe
            {
                fixed (Gdi.PIXELFORMATDESCRIPTOR* ppfd_ptr = &ppfd)
                {
                    return Delegates.wglSetPixelFormat((IntPtr)hdc, (int)ipfd, (Gdi.PIXELFORMATDESCRIPTOR*)ppfd_ptr);
                }
            }
        }

        [System.CLSCompliant(false)]
        public static 
        Boolean wglSetPixelFormat(IntPtr hdc, int ipfd, IntPtr ppfd)
        {
            unsafe
            {
                return Delegates.wglSetPixelFormat((IntPtr)hdc, (int)ipfd, (Gdi.PIXELFORMATDESCRIPTOR*)ppfd);
            }
        }

        public static 
        Boolean wglSwapBuffers(IntPtr hdc)
        {
            return Delegates.wglSwapBuffers((IntPtr)hdc);
        }

        public static 
        Boolean wglShareLists(IntPtr hrcSrvShare, IntPtr hrcSrvSource)
        {
            return Delegates.wglShareLists((IntPtr)hrcSrvShare, (IntPtr)hrcSrvSource);
        }

        public static 
        IntPtr wglCreateLayerContext(IntPtr hDc, int level)
        {
            return Delegates.wglCreateLayerContext((IntPtr)hDc, (int)level);
        }

        [System.CLSCompliant(false)]
        public static 
        Boolean wglDescribeLayerPlane(IntPtr hDc, int pixelFormat, int layerPlane, UInt32 nBytes, Gdi.LAYERPLANEDESCRIPTOR[] plpd)
        {
            unsafe
            {
                fixed (Gdi.LAYERPLANEDESCRIPTOR* plpd_ptr = plpd)
                {
                    return Delegates.wglDescribeLayerPlane((IntPtr)hDc, (int)pixelFormat, (int)layerPlane, (UInt32)nBytes, (Gdi.LAYERPLANEDESCRIPTOR*)plpd_ptr);
                }
            }
        }

        public static 
        Boolean wglDescribeLayerPlane(IntPtr hDc, int pixelFormat, int layerPlane, Int32 nBytes, Gdi.LAYERPLANEDESCRIPTOR[] plpd)
        {
            unsafe
            {
                fixed (Gdi.LAYERPLANEDESCRIPTOR* plpd_ptr = plpd)
                {
                    return Delegates.wglDescribeLayerPlane((IntPtr)hDc, (int)pixelFormat, (int)layerPlane, (UInt32)nBytes, (Gdi.LAYERPLANEDESCRIPTOR*)plpd_ptr);
                }
            }
        }

        [System.CLSCompliant(false)]
        public static 
        Boolean wglDescribeLayerPlane(IntPtr hDc, int pixelFormat, int layerPlane, UInt32 nBytes, ref Gdi.LAYERPLANEDESCRIPTOR plpd)
        {
            unsafe
            {
                fixed (Gdi.LAYERPLANEDESCRIPTOR* plpd_ptr = &plpd)
                {
                    return Delegates.wglDescribeLayerPlane((IntPtr)hDc, (int)pixelFormat, (int)layerPlane, (UInt32)nBytes, (Gdi.LAYERPLANEDESCRIPTOR*)plpd_ptr);
                }
            }
        }

        public static 
        Boolean wglDescribeLayerPlane(IntPtr hDc, int pixelFormat, int layerPlane, Int32 nBytes, ref Gdi.LAYERPLANEDESCRIPTOR plpd)
        {
            unsafe
            {
                fixed (Gdi.LAYERPLANEDESCRIPTOR* plpd_ptr = &plpd)
                {
                    return Delegates.wglDescribeLayerPlane((IntPtr)hDc, (int)pixelFormat, (int)layerPlane, (UInt32)nBytes, (Gdi.LAYERPLANEDESCRIPTOR*)plpd_ptr);
                }
            }
        }

        [System.CLSCompliant(false)]
        public static 
        Boolean wglDescribeLayerPlane(IntPtr hDc, int pixelFormat, int layerPlane, UInt32 nBytes, IntPtr plpd)
        {
            unsafe
            {
                return Delegates.wglDescribeLayerPlane((IntPtr)hDc, (int)pixelFormat, (int)layerPlane, (UInt32)nBytes, (Gdi.LAYERPLANEDESCRIPTOR*)plpd);
            }
        }

        [System.CLSCompliant(false)]
        public static 
        Boolean wglDescribeLayerPlane(IntPtr hDc, int pixelFormat, int layerPlane, Int32 nBytes, IntPtr plpd)
        {
            unsafe
            {
                return Delegates.wglDescribeLayerPlane((IntPtr)hDc, (int)pixelFormat, (int)layerPlane, (UInt32)nBytes, (Gdi.LAYERPLANEDESCRIPTOR*)plpd);
            }
        }

        public static 
        int wglSetLayerPaletteEntries(IntPtr hdc, int iLayerPlane, int iStart, int cEntries, Int32[] pcr)
        {
            unsafe
            {
                fixed (Int32* pcr_ptr = pcr)
                {
                    return Delegates.wglSetLayerPaletteEntries((IntPtr)hdc, (int)iLayerPlane, (int)iStart, (int)cEntries, (Int32*)pcr_ptr);
                }
            }
        }

        public static 
        int wglSetLayerPaletteEntries(IntPtr hdc, int iLayerPlane, int iStart, int cEntries, ref Int32 pcr)
        {
            unsafe
            {
                fixed (Int32* pcr_ptr = &pcr)
                {
                    return Delegates.wglSetLayerPaletteEntries((IntPtr)hdc, (int)iLayerPlane, (int)iStart, (int)cEntries, (Int32*)pcr_ptr);
                }
            }
        }

        [System.CLSCompliant(false)]
        public static 
        int wglSetLayerPaletteEntries(IntPtr hdc, int iLayerPlane, int iStart, int cEntries, IntPtr pcr)
        {
            unsafe
            {
                return Delegates.wglSetLayerPaletteEntries((IntPtr)hdc, (int)iLayerPlane, (int)iStart, (int)cEntries, (Int32*)pcr);
            }
        }

        public static 
        int wglGetLayerPaletteEntries(IntPtr hdc, int iLayerPlane, int iStart, int cEntries, Int32[] pcr)
        {
            unsafe
            {
                fixed (Int32* pcr_ptr = pcr)
                {
                    return Delegates.wglGetLayerPaletteEntries((IntPtr)hdc, (int)iLayerPlane, (int)iStart, (int)cEntries, (Int32*)pcr_ptr);
                }
            }
        }

        public static 
        int wglGetLayerPaletteEntries(IntPtr hdc, int iLayerPlane, int iStart, int cEntries, ref Int32 pcr)
        {
            unsafe
            {
                fixed (Int32* pcr_ptr = &pcr)
                {
                    return Delegates.wglGetLayerPaletteEntries((IntPtr)hdc, (int)iLayerPlane, (int)iStart, (int)cEntries, (Int32*)pcr_ptr);
                }
            }
        }

        [System.CLSCompliant(false)]
        public static 
        int wglGetLayerPaletteEntries(IntPtr hdc, int iLayerPlane, int iStart, int cEntries, IntPtr pcr)
        {
            unsafe
            {
                return Delegates.wglGetLayerPaletteEntries((IntPtr)hdc, (int)iLayerPlane, (int)iStart, (int)cEntries, (Int32*)pcr);
            }
        }

        public static 
        Boolean wglRealizeLayerPalette(IntPtr hdc, int iLayerPlane, Boolean bRealize)
        {
            return Delegates.wglRealizeLayerPalette((IntPtr)hdc, (int)iLayerPlane, (Boolean)bRealize);
        }

        [System.CLSCompliant(false)]
        public static 
        Boolean wglSwapLayerBuffers(IntPtr hdc, UInt32 fuFlags)
        {
            return Delegates.wglSwapLayerBuffers((IntPtr)hdc, (UInt32)fuFlags);
        }

        public static 
        Boolean wglSwapLayerBuffers(IntPtr hdc, Int32 fuFlags)
        {
            return Delegates.wglSwapLayerBuffers((IntPtr)hdc, (UInt32)fuFlags);
        }

        public static 
        Boolean wglUseFontBitmapsA(IntPtr hDC, Int32 first, Int32 count, Int32 listBase)
        {
            return Delegates.wglUseFontBitmapsA((IntPtr)hDC, (Int32)first, (Int32)count, (Int32)listBase);
        }

        public static 
        Boolean wglUseFontBitmapsW(IntPtr hDC, Int32 first, Int32 count, Int32 listBase)
        {
            return Delegates.wglUseFontBitmapsW((IntPtr)hDC, (Int32)first, (Int32)count, (Int32)listBase);
        }

        public static 
        Boolean wglUseFontOutlinesA(IntPtr hDC, Int32 first, Int32 count, Int32 listBase, float thickness, float deviation, Int32 fontMode, Gdi.GLYPHMETRICSFLOAT[] glyphMetrics)
        {
            unsafe
            {
                fixed (Gdi.GLYPHMETRICSFLOAT* glyphMetrics_ptr = glyphMetrics)
                {
                    return Delegates.wglUseFontOutlinesA((IntPtr)hDC, (Int32)first, (Int32)count, (Int32)listBase, (float)thickness, (float)deviation, (Int32)fontMode, (Gdi.GLYPHMETRICSFLOAT*)glyphMetrics_ptr);
                }
            }
        }

        public static 
        Boolean wglUseFontOutlinesA(IntPtr hDC, Int32 first, Int32 count, Int32 listBase, float thickness, float deviation, Int32 fontMode, ref Gdi.GLYPHMETRICSFLOAT glyphMetrics)
        {
            unsafe
            {
                fixed (Gdi.GLYPHMETRICSFLOAT* glyphMetrics_ptr = &glyphMetrics)
                {
                    return Delegates.wglUseFontOutlinesA((IntPtr)hDC, (Int32)first, (Int32)count, (Int32)listBase, (float)thickness, (float)deviation, (Int32)fontMode, (Gdi.GLYPHMETRICSFLOAT*)glyphMetrics_ptr);
                }
            }
        }

        [System.CLSCompliant(false)]
        public static 
        Boolean wglUseFontOutlinesA(IntPtr hDC, Int32 first, Int32 count, Int32 listBase, float thickness, float deviation, Int32 fontMode, IntPtr glyphMetrics)
        {
            unsafe
            {
                return Delegates.wglUseFontOutlinesA((IntPtr)hDC, (Int32)first, (Int32)count, (Int32)listBase, (float)thickness, (float)deviation, (Int32)fontMode, (Gdi.GLYPHMETRICSFLOAT*)glyphMetrics);
            }
        }

        public static 
        Boolean wglUseFontOutlinesW(IntPtr hDC, Int32 first, Int32 count, Int32 listBase, float thickness, float deviation, Int32 fontMode, Gdi.GLYPHMETRICSFLOAT[] glyphMetrics)
        {
            unsafe
            {
                fixed (Gdi.GLYPHMETRICSFLOAT* glyphMetrics_ptr = glyphMetrics)
                {
                    return Delegates.wglUseFontOutlinesW((IntPtr)hDC, (Int32)first, (Int32)count, (Int32)listBase, (float)thickness, (float)deviation, (Int32)fontMode, (Gdi.GLYPHMETRICSFLOAT*)glyphMetrics_ptr);
                }
            }
        }

        public static 
        Boolean wglUseFontOutlinesW(IntPtr hDC, Int32 first, Int32 count, Int32 listBase, float thickness, float deviation, Int32 fontMode, ref Gdi.GLYPHMETRICSFLOAT glyphMetrics)
        {
            unsafe
            {
                fixed (Gdi.GLYPHMETRICSFLOAT* glyphMetrics_ptr = &glyphMetrics)
                {
                    return Delegates.wglUseFontOutlinesW((IntPtr)hDC, (Int32)first, (Int32)count, (Int32)listBase, (float)thickness, (float)deviation, (Int32)fontMode, (Gdi.GLYPHMETRICSFLOAT*)glyphMetrics_ptr);
                }
            }
        }

        [System.CLSCompliant(false)]
        public static 
        Boolean wglUseFontOutlinesW(IntPtr hDC, Int32 first, Int32 count, Int32 listBase, float thickness, float deviation, Int32 fontMode, IntPtr glyphMetrics)
        {
            unsafe
            {
                return Delegates.wglUseFontOutlinesW((IntPtr)hDC, (Int32)first, (Int32)count, (Int32)listBase, (float)thickness, (float)deviation, (Int32)fontMode, (Gdi.GLYPHMETRICSFLOAT*)glyphMetrics);
            }
        }

        [System.CLSCompliant(false)]
        public static 
        IntPtr wglCreateBufferRegionARB(IntPtr hDC, int iLayerPlane, UInt32 uType)
        {
            return Delegates.wglCreateBufferRegionARB((IntPtr)hDC, (int)iLayerPlane, (UInt32)uType);
        }

        public static 
        IntPtr wglCreateBufferRegionARB(IntPtr hDC, int iLayerPlane, Int32 uType)
        {
            return Delegates.wglCreateBufferRegionARB((IntPtr)hDC, (int)iLayerPlane, (UInt32)uType);
        }

        public static 
        void wglDeleteBufferRegionARB(IntPtr hRegion)
        {
            Delegates.wglDeleteBufferRegionARB((IntPtr)hRegion);
        }

        public static 
        Boolean wglSaveBufferRegionARB(IntPtr hRegion, int x, int y, int width, int height)
        {
            return Delegates.wglSaveBufferRegionARB((IntPtr)hRegion, (int)x, (int)y, (int)width, (int)height);
        }

        public static 
        Boolean wglRestoreBufferRegionARB(IntPtr hRegion, int x, int y, int width, int height, int xSrc, int ySrc)
        {
            return Delegates.wglRestoreBufferRegionARB((IntPtr)hRegion, (int)x, (int)y, (int)width, (int)height, (int)xSrc, (int)ySrc);
        }

        public static 
        string wglGetExtensionsStringARB(IntPtr hdc)
        {
            unsafe
            {
                return System.Runtime.InteropServices.Marshal.PtrToStringAnsi(Delegates.wglGetExtensionsStringARB((IntPtr)hdc));
            }
        }

        [System.CLSCompliant(false)]
        public static 
        Boolean wglGetPixelFormatAttribivARB(IntPtr hdc, int iPixelFormat, int iLayerPlane, UInt32 nAttributes, int[] piAttributes, [Out] int[] piValues)
        {
            unsafe
            {
                fixed (int* piAttributes_ptr = piAttributes)
                fixed (int* piValues_ptr = piValues)
                {
                    return Delegates.wglGetPixelFormatAttribivARB((IntPtr)hdc, (int)iPixelFormat, (int)iLayerPlane, (UInt32)nAttributes, (int*)piAttributes_ptr, (int*)piValues_ptr);
                }
            }
        }

        public static 
        Boolean wglGetPixelFormatAttribivARB(IntPtr hdc, int iPixelFormat, int iLayerPlane, Int32 nAttributes, int[] piAttributes, [Out] int[] piValues)
        {
            unsafe
            {
                fixed (int* piAttributes_ptr = piAttributes)
                fixed (int* piValues_ptr = piValues)
                {
                    return Delegates.wglGetPixelFormatAttribivARB((IntPtr)hdc, (int)iPixelFormat, (int)iLayerPlane, (UInt32)nAttributes, (int*)piAttributes_ptr, (int*)piValues_ptr);
                }
            }
        }

        [System.CLSCompliant(false)]
        public static 
        Boolean wglGetPixelFormatAttribivARB(IntPtr hdc, int iPixelFormat, int iLayerPlane, UInt32 nAttributes, ref int piAttributes, [Out] out int piValues)
        {
            unsafe
            {
                fixed (int* piAttributes_ptr = &piAttributes)
                fixed (int* piValues_ptr = &piValues)
                {
                    Boolean retval = Delegates.wglGetPixelFormatAttribivARB((IntPtr)hdc, (int)iPixelFormat, (int)iLayerPlane, (UInt32)nAttributes, (int*)piAttributes_ptr, (int*)piValues_ptr);
                    piValues = *piValues_ptr;
                    return retval;
                }
            }
        }

        public static 
        Boolean wglGetPixelFormatAttribivARB(IntPtr hdc, int iPixelFormat, int iLayerPlane, Int32 nAttributes, ref int piAttributes, [Out] out int piValues)
        {
            unsafe
            {
                fixed (int* piAttributes_ptr = &piAttributes)
                fixed (int* piValues_ptr = &piValues)
                {
                    Boolean retval = Delegates.wglGetPixelFormatAttribivARB((IntPtr)hdc, (int)iPixelFormat, (int)iLayerPlane, (UInt32)nAttributes, (int*)piAttributes_ptr, (int*)piValues_ptr);
                    piValues = *piValues_ptr;
                    return retval;
                }
            }
        }

        [System.CLSCompliant(false)]
        public static 
        Boolean wglGetPixelFormatAttribivARB(IntPtr hdc, int iPixelFormat, int iLayerPlane, UInt32 nAttributes, IntPtr piAttributes, [Out] IntPtr piValues)
        {
            unsafe
            {
                return Delegates.wglGetPixelFormatAttribivARB((IntPtr)hdc, (int)iPixelFormat, (int)iLayerPlane, (UInt32)nAttributes, (int*)piAttributes, (int*)piValues);
            }
        }

        [System.CLSCompliant(false)]
        public static 
        Boolean wglGetPixelFormatAttribivARB(IntPtr hdc, int iPixelFormat, int iLayerPlane, Int32 nAttributes, IntPtr piAttributes, [Out] IntPtr piValues)
        {
            unsafe
            {
                return Delegates.wglGetPixelFormatAttribivARB((IntPtr)hdc, (int)iPixelFormat, (int)iLayerPlane, (UInt32)nAttributes, (int*)piAttributes, (int*)piValues);
            }
        }

        [System.CLSCompliant(false)]
        public static 
        Boolean wglGetPixelFormatAttribfvARB(IntPtr hdc, int iPixelFormat, int iLayerPlane, UInt32 nAttributes, int[] piAttributes, [Out] Single[] pfValues)
        {
            unsafe
            {
                fixed (int* piAttributes_ptr = piAttributes)
                fixed (Single* pfValues_ptr = pfValues)
                {
                    return Delegates.wglGetPixelFormatAttribfvARB((IntPtr)hdc, (int)iPixelFormat, (int)iLayerPlane, (UInt32)nAttributes, (int*)piAttributes_ptr, (Single*)pfValues_ptr);
                }
            }
        }

        public static 
        Boolean wglGetPixelFormatAttribfvARB(IntPtr hdc, int iPixelFormat, int iLayerPlane, Int32 nAttributes, int[] piAttributes, [Out] Single[] pfValues)
        {
            unsafe
            {
                fixed (int* piAttributes_ptr = piAttributes)
                fixed (Single* pfValues_ptr = pfValues)
                {
                    return Delegates.wglGetPixelFormatAttribfvARB((IntPtr)hdc, (int)iPixelFormat, (int)iLayerPlane, (UInt32)nAttributes, (int*)piAttributes_ptr, (Single*)pfValues_ptr);
                }
            }
        }

        [System.CLSCompliant(false)]
        public static 
        Boolean wglGetPixelFormatAttribfvARB(IntPtr hdc, int iPixelFormat, int iLayerPlane, UInt32 nAttributes, ref int piAttributes, [Out] out Single pfValues)
        {
            unsafe
            {
                fixed (int* piAttributes_ptr = &piAttributes)
                fixed (Single* pfValues_ptr = &pfValues)
                {
                    Boolean retval = Delegates.wglGetPixelFormatAttribfvARB((IntPtr)hdc, (int)iPixelFormat, (int)iLayerPlane, (UInt32)nAttributes, (int*)piAttributes_ptr, (Single*)pfValues_ptr);
                    pfValues = *pfValues_ptr;
                    return retval;
                }
            }
        }

        public static 
        Boolean wglGetPixelFormatAttribfvARB(IntPtr hdc, int iPixelFormat, int iLayerPlane, Int32 nAttributes, ref int piAttributes, [Out] out Single pfValues)
        {
            unsafe
            {
                fixed (int* piAttributes_ptr = &piAttributes)
                fixed (Single* pfValues_ptr = &pfValues)
                {
                    Boolean retval = Delegates.wglGetPixelFormatAttribfvARB((IntPtr)hdc, (int)iPixelFormat, (int)iLayerPlane, (UInt32)nAttributes, (int*)piAttributes_ptr, (Single*)pfValues_ptr);
                    pfValues = *pfValues_ptr;
                    return retval;
                }
            }
        }

        [System.CLSCompliant(false)]
        public static 
        Boolean wglGetPixelFormatAttribfvARB(IntPtr hdc, int iPixelFormat, int iLayerPlane, UInt32 nAttributes, IntPtr piAttributes, [Out] IntPtr pfValues)
        {
            unsafe
            {
                return Delegates.wglGetPixelFormatAttribfvARB((IntPtr)hdc, (int)iPixelFormat, (int)iLayerPlane, (UInt32)nAttributes, (int*)piAttributes, (Single*)pfValues);
            }
        }

        [System.CLSCompliant(false)]
        public static 
        Boolean wglGetPixelFormatAttribfvARB(IntPtr hdc, int iPixelFormat, int iLayerPlane, Int32 nAttributes, IntPtr piAttributes, [Out] IntPtr pfValues)
        {
            unsafe
            {
                return Delegates.wglGetPixelFormatAttribfvARB((IntPtr)hdc, (int)iPixelFormat, (int)iLayerPlane, (UInt32)nAttributes, (int*)piAttributes, (Single*)pfValues);
            }
        }

        [System.CLSCompliant(false)]
        public static 
        Boolean wglChoosePixelFormatARB(IntPtr hdc, int[] piAttribIList, Single[] pfAttribFList, UInt32 nMaxFormats, [Out] int[] piFormats, [Out] UInt32[] nNumFormats)
        {
            unsafe
            {
                fixed (int* piAttribIList_ptr = piAttribIList)
                fixed (Single* pfAttribFList_ptr = pfAttribFList)
                fixed (int* piFormats_ptr = piFormats)
                fixed (UInt32* nNumFormats_ptr = nNumFormats)
                {
                    return Delegates.wglChoosePixelFormatARB((IntPtr)hdc, (int*)piAttribIList_ptr, (Single*)pfAttribFList_ptr, (UInt32)nMaxFormats, (int*)piFormats_ptr, (UInt32*)nNumFormats_ptr);
                }
            }
        }

        public static 
        Boolean wglChoosePixelFormatARB(IntPtr hdc, int[] piAttribIList, Single[] pfAttribFList, Int32 nMaxFormats, [Out] int[] piFormats, [Out] Int32[] nNumFormats)
        {
            unsafe
            {
                fixed (int* piAttribIList_ptr = piAttribIList)
                fixed (Single* pfAttribFList_ptr = pfAttribFList)
                fixed (int* piFormats_ptr = piFormats)
                fixed (Int32* nNumFormats_ptr = nNumFormats)
                {
                    return Delegates.wglChoosePixelFormatARB((IntPtr)hdc, (int*)piAttribIList_ptr, (Single*)pfAttribFList_ptr, (UInt32)nMaxFormats, (int*)piFormats_ptr, (UInt32*)nNumFormats_ptr);
                }
            }
        }

        [System.CLSCompliant(false)]
        public static 
        Boolean wglChoosePixelFormatARB(IntPtr hdc, ref int piAttribIList, ref Single pfAttribFList, UInt32 nMaxFormats, [Out] out int piFormats, [Out] out UInt32 nNumFormats)
        {
            unsafe
            {
                fixed (int* piAttribIList_ptr = &piAttribIList)
                fixed (Single* pfAttribFList_ptr = &pfAttribFList)
                fixed (int* piFormats_ptr = &piFormats)
                fixed (UInt32* nNumFormats_ptr = &nNumFormats)
                {
                    Boolean retval = Delegates.wglChoosePixelFormatARB((IntPtr)hdc, (int*)piAttribIList_ptr, (Single*)pfAttribFList_ptr, (UInt32)nMaxFormats, (int*)piFormats_ptr, (UInt32*)nNumFormats_ptr);
                    piFormats = *piFormats_ptr;
                    nNumFormats = *nNumFormats_ptr;
                    return retval;
                }
            }
        }

        public static 
        Boolean wglChoosePixelFormatARB(IntPtr hdc, ref int piAttribIList, ref Single pfAttribFList, Int32 nMaxFormats, [Out] out int piFormats, [Out] out Int32 nNumFormats)
        {
            unsafe
            {
                fixed (int* piAttribIList_ptr = &piAttribIList)
                fixed (Single* pfAttribFList_ptr = &pfAttribFList)
                fixed (int* piFormats_ptr = &piFormats)
                fixed (Int32* nNumFormats_ptr = &nNumFormats)
                {
                    Boolean retval = Delegates.wglChoosePixelFormatARB((IntPtr)hdc, (int*)piAttribIList_ptr, (Single*)pfAttribFList_ptr, (UInt32)nMaxFormats, (int*)piFormats_ptr, (UInt32*)nNumFormats_ptr);
                    piFormats = *piFormats_ptr;
                    nNumFormats = *nNumFormats_ptr;
                    return retval;
                }
            }
        }

        [System.CLSCompliant(false)]
        public static 
        Boolean wglChoosePixelFormatARB(IntPtr hdc, IntPtr piAttribIList, IntPtr pfAttribFList, UInt32 nMaxFormats, [Out] IntPtr piFormats, [Out] IntPtr nNumFormats)
        {
            unsafe
            {
                return Delegates.wglChoosePixelFormatARB((IntPtr)hdc, (int*)piAttribIList, (Single*)pfAttribFList, (UInt32)nMaxFormats, (int*)piFormats, (UInt32*)nNumFormats);
            }
        }

        public static 
        Boolean wglChoosePixelFormatARB(IntPtr hdc, IntPtr piAttribIList, IntPtr pfAttribFList, Int32 nMaxFormats, [Out] IntPtr piFormats, [Out] IntPtr nNumFormats)
        {
            unsafe
            {
                return Delegates.wglChoosePixelFormatARB((IntPtr)hdc, (int*)piAttribIList, (Single*)pfAttribFList, (UInt32)nMaxFormats, (int*)piFormats, (UInt32*)nNumFormats);
            }
        }

        public static 
        Boolean wglMakeContextCurrentARB(IntPtr hDrawDC, IntPtr hReadDC, IntPtr hglrc)
        {
            return Delegates.wglMakeContextCurrentARB((IntPtr)hDrawDC, (IntPtr)hReadDC, (IntPtr)hglrc);
        }

        public static 
        IntPtr wglGetCurrentReadDCARB()
        {
            return Delegates.wglGetCurrentReadDCARB();
        }

        public static 
        IntPtr wglCreatePbufferARB(IntPtr hDC, int iPixelFormat, int iWidth, int iHeight, int[] piAttribList)
        {
            unsafe
            {
                fixed (int* piAttribList_ptr = piAttribList)
                {
                    return Delegates.wglCreatePbufferARB((IntPtr)hDC, (int)iPixelFormat, (int)iWidth, (int)iHeight, (int*)piAttribList_ptr);
                }
            }
        }

        public static 
        IntPtr wglCreatePbufferARB(IntPtr hDC, int iPixelFormat, int iWidth, int iHeight, ref int piAttribList)
        {
            unsafe
            {
                fixed (int* piAttribList_ptr = &piAttribList)
                {
                    return Delegates.wglCreatePbufferARB((IntPtr)hDC, (int)iPixelFormat, (int)iWidth, (int)iHeight, (int*)piAttribList_ptr);
                }
            }
        }

        [System.CLSCompliant(false)]
        public static 
        IntPtr wglCreatePbufferARB(IntPtr hDC, int iPixelFormat, int iWidth, int iHeight, IntPtr piAttribList)
        {
            unsafe
            {
                return Delegates.wglCreatePbufferARB((IntPtr)hDC, (int)iPixelFormat, (int)iWidth, (int)iHeight, (int*)piAttribList);
            }
        }

        public static 
        IntPtr wglGetPbufferDCARB(IntPtr hPbuffer)
        {
            return Delegates.wglGetPbufferDCARB((IntPtr)hPbuffer);
        }

        public static 
        int wglReleasePbufferDCARB(IntPtr hPbuffer, IntPtr hDC)
        {
            return Delegates.wglReleasePbufferDCARB((IntPtr)hPbuffer, (IntPtr)hDC);
        }

        public static 
        Boolean wglDestroyPbufferARB(IntPtr hPbuffer)
        {
            return Delegates.wglDestroyPbufferARB((IntPtr)hPbuffer);
        }

        public static 
        Boolean wglQueryPbufferARB(IntPtr hPbuffer, int iAttribute, [Out] int[] piValue)
        {
            unsafe
            {
                fixed (int* piValue_ptr = piValue)
                {
                    return Delegates.wglQueryPbufferARB((IntPtr)hPbuffer, (int)iAttribute, (int*)piValue_ptr);
                }
            }
        }

        public static 
        Boolean wglQueryPbufferARB(IntPtr hPbuffer, int iAttribute, [Out] out int piValue)
        {
            unsafe
            {
                fixed (int* piValue_ptr = &piValue)
                {
                    Boolean retval = Delegates.wglQueryPbufferARB((IntPtr)hPbuffer, (int)iAttribute, (int*)piValue_ptr);
                    piValue = *piValue_ptr;
                    return retval;
                }
            }
        }

        [System.CLSCompliant(false)]
        public static 
        Boolean wglQueryPbufferARB(IntPtr hPbuffer, int iAttribute, [Out] IntPtr piValue)
        {
            unsafe
            {
                return Delegates.wglQueryPbufferARB((IntPtr)hPbuffer, (int)iAttribute, (int*)piValue);
            }
        }

        public static 
        Boolean wglBindTexImageARB(IntPtr hPbuffer, int iBuffer)
        {
            return Delegates.wglBindTexImageARB((IntPtr)hPbuffer, (int)iBuffer);
        }

        public static 
        Boolean wglReleaseTexImageARB(IntPtr hPbuffer, int iBuffer)
        {
            return Delegates.wglReleaseTexImageARB((IntPtr)hPbuffer, (int)iBuffer);
        }

        public static 
        Boolean wglSetPbufferAttribARB(IntPtr hPbuffer, int[] piAttribList)
        {
            unsafe
            {
                fixed (int* piAttribList_ptr = piAttribList)
                {
                    return Delegates.wglSetPbufferAttribARB((IntPtr)hPbuffer, (int*)piAttribList_ptr);
                }
            }
        }

        public static 
        Boolean wglSetPbufferAttribARB(IntPtr hPbuffer, ref int piAttribList)
        {
            unsafe
            {
                fixed (int* piAttribList_ptr = &piAttribList)
                {
                    return Delegates.wglSetPbufferAttribARB((IntPtr)hPbuffer, (int*)piAttribList_ptr);
                }
            }
        }

        [System.CLSCompliant(false)]
        public static 
        Boolean wglSetPbufferAttribARB(IntPtr hPbuffer, IntPtr piAttribList)
        {
            unsafe
            {
                return Delegates.wglSetPbufferAttribARB((IntPtr)hPbuffer, (int*)piAttribList);
            }
        }

        [System.CLSCompliant(false)]
        public static 
        bool wglCreateDisplayColorTableEXT(UInt16 id)
        {
            return Delegates.wglCreateDisplayColorTableEXT((UInt16)id);
        }

        public static 
        bool wglCreateDisplayColorTableEXT(Int16 id)
        {
            return Delegates.wglCreateDisplayColorTableEXT((UInt16)id);
        }

        [System.CLSCompliant(false)]
        public static 
        bool wglLoadDisplayColorTableEXT(UInt16[] table, UInt32 length)
        {
            unsafe
            {
                fixed (UInt16* table_ptr = table)
                {
                    return Delegates.wglLoadDisplayColorTableEXT((UInt16*)table_ptr, (UInt32)length);
                }
            }
        }

        public static 
        bool wglLoadDisplayColorTableEXT(Int16[] table, Int32 length)
        {
            unsafe
            {
                fixed (Int16* table_ptr = table)
                {
                    return Delegates.wglLoadDisplayColorTableEXT((UInt16*)table_ptr, (UInt32)length);
                }
            }
        }

        [System.CLSCompliant(false)]
        public static 
        bool wglLoadDisplayColorTableEXT(ref UInt16 table, UInt32 length)
        {
            unsafe
            {
                fixed (UInt16* table_ptr = &table)
                {
                    return Delegates.wglLoadDisplayColorTableEXT((UInt16*)table_ptr, (UInt32)length);
                }
            }
        }

        public static 
        bool wglLoadDisplayColorTableEXT(ref Int16 table, Int32 length)
        {
            unsafe
            {
                fixed (Int16* table_ptr = &table)
                {
                    return Delegates.wglLoadDisplayColorTableEXT((UInt16*)table_ptr, (UInt32)length);
                }
            }
        }

        [System.CLSCompliant(false)]
        public static 
        bool wglLoadDisplayColorTableEXT(IntPtr table, UInt32 length)
        {
            unsafe
            {
                return Delegates.wglLoadDisplayColorTableEXT((UInt16*)table, (UInt32)length);
            }
        }

        public static 
        bool wglLoadDisplayColorTableEXT(IntPtr table, Int32 length)
        {
            unsafe
            {
                return Delegates.wglLoadDisplayColorTableEXT((UInt16*)table, (UInt32)length);
            }
        }

        [System.CLSCompliant(false)]
        public static 
        bool wglBindDisplayColorTableEXT(UInt16 id)
        {
            return Delegates.wglBindDisplayColorTableEXT((UInt16)id);
        }

        public static 
        bool wglBindDisplayColorTableEXT(Int16 id)
        {
            return Delegates.wglBindDisplayColorTableEXT((UInt16)id);
        }

        [System.CLSCompliant(false)]
        public static 
        void wglDestroyDisplayColorTableEXT(UInt16 id)
        {
            Delegates.wglDestroyDisplayColorTableEXT((UInt16)id);
        }

        public static 
        void wglDestroyDisplayColorTableEXT(Int16 id)
        {
            Delegates.wglDestroyDisplayColorTableEXT((UInt16)id);
        }

        public static 
        string wglGetExtensionsStringEXT()
        {
            unsafe
            {
                return System.Runtime.InteropServices.Marshal.PtrToStringAnsi(Delegates.wglGetExtensionsStringEXT());
            }
        }

        public static 
        Boolean wglMakeContextCurrentEXT(IntPtr hDrawDC, IntPtr hReadDC, IntPtr hglrc)
        {
            return Delegates.wglMakeContextCurrentEXT((IntPtr)hDrawDC, (IntPtr)hReadDC, (IntPtr)hglrc);
        }

        public static 
        IntPtr wglGetCurrentReadDCEXT()
        {
            return Delegates.wglGetCurrentReadDCEXT();
        }

        public static 
        IntPtr wglCreatePbufferEXT(IntPtr hDC, int iPixelFormat, int iWidth, int iHeight, int[] piAttribList)
        {
            unsafe
            {
                fixed (int* piAttribList_ptr = piAttribList)
                {
                    return Delegates.wglCreatePbufferEXT((IntPtr)hDC, (int)iPixelFormat, (int)iWidth, (int)iHeight, (int*)piAttribList_ptr);
                }
            }
        }

        public static 
        IntPtr wglCreatePbufferEXT(IntPtr hDC, int iPixelFormat, int iWidth, int iHeight, ref int piAttribList)
        {
            unsafe
            {
                fixed (int* piAttribList_ptr = &piAttribList)
                {
                    return Delegates.wglCreatePbufferEXT((IntPtr)hDC, (int)iPixelFormat, (int)iWidth, (int)iHeight, (int*)piAttribList_ptr);
                }
            }
        }

        [System.CLSCompliant(false)]
        public static 
        IntPtr wglCreatePbufferEXT(IntPtr hDC, int iPixelFormat, int iWidth, int iHeight, IntPtr piAttribList)
        {
            unsafe
            {
                return Delegates.wglCreatePbufferEXT((IntPtr)hDC, (int)iPixelFormat, (int)iWidth, (int)iHeight, (int*)piAttribList);
            }
        }

        public static 
        IntPtr wglGetPbufferDCEXT(IntPtr hPbuffer)
        {
            return Delegates.wglGetPbufferDCEXT((IntPtr)hPbuffer);
        }

        public static 
        int wglReleasePbufferDCEXT(IntPtr hPbuffer, IntPtr hDC)
        {
            return Delegates.wglReleasePbufferDCEXT((IntPtr)hPbuffer, (IntPtr)hDC);
        }

        public static 
        Boolean wglDestroyPbufferEXT(IntPtr hPbuffer)
        {
            return Delegates.wglDestroyPbufferEXT((IntPtr)hPbuffer);
        }

        public static 
        Boolean wglQueryPbufferEXT(IntPtr hPbuffer, int iAttribute, [Out] int[] piValue)
        {
            unsafe
            {
                fixed (int* piValue_ptr = piValue)
                {
                    return Delegates.wglQueryPbufferEXT((IntPtr)hPbuffer, (int)iAttribute, (int*)piValue_ptr);
                }
            }
        }

        public static 
        Boolean wglQueryPbufferEXT(IntPtr hPbuffer, int iAttribute, [Out] out int piValue)
        {
            unsafe
            {
                fixed (int* piValue_ptr = &piValue)
                {
                    Boolean retval = Delegates.wglQueryPbufferEXT((IntPtr)hPbuffer, (int)iAttribute, (int*)piValue_ptr);
                    piValue = *piValue_ptr;
                    return retval;
                }
            }
        }

        [System.CLSCompliant(false)]
        public static 
        Boolean wglQueryPbufferEXT(IntPtr hPbuffer, int iAttribute, [Out] IntPtr piValue)
        {
            unsafe
            {
                return Delegates.wglQueryPbufferEXT((IntPtr)hPbuffer, (int)iAttribute, (int*)piValue);
            }
        }

        [System.CLSCompliant(false)]
        public static 
        Boolean wglGetPixelFormatAttribivEXT(IntPtr hdc, int iPixelFormat, int iLayerPlane, UInt32 nAttributes, [Out] int[] piAttributes, [Out] int[] piValues)
        {
            unsafe
            {
                fixed (int* piAttributes_ptr = piAttributes)
                fixed (int* piValues_ptr = piValues)
                {
                    return Delegates.wglGetPixelFormatAttribivEXT((IntPtr)hdc, (int)iPixelFormat, (int)iLayerPlane, (UInt32)nAttributes, (int*)piAttributes_ptr, (int*)piValues_ptr);
                }
            }
        }

        public static 
        Boolean wglGetPixelFormatAttribivEXT(IntPtr hdc, int iPixelFormat, int iLayerPlane, Int32 nAttributes, [Out] int[] piAttributes, [Out] int[] piValues)
        {
            unsafe
            {
                fixed (int* piAttributes_ptr = piAttributes)
                fixed (int* piValues_ptr = piValues)
                {
                    return Delegates.wglGetPixelFormatAttribivEXT((IntPtr)hdc, (int)iPixelFormat, (int)iLayerPlane, (UInt32)nAttributes, (int*)piAttributes_ptr, (int*)piValues_ptr);
                }
            }
        }

        [System.CLSCompliant(false)]
        public static 
        Boolean wglGetPixelFormatAttribivEXT(IntPtr hdc, int iPixelFormat, int iLayerPlane, UInt32 nAttributes, [Out] out int piAttributes, [Out] out int piValues)
        {
            unsafe
            {
                fixed (int* piAttributes_ptr = &piAttributes)
                fixed (int* piValues_ptr = &piValues)
                {
                    Boolean retval = Delegates.wglGetPixelFormatAttribivEXT((IntPtr)hdc, (int)iPixelFormat, (int)iLayerPlane, (UInt32)nAttributes, (int*)piAttributes_ptr, (int*)piValues_ptr);
                    piAttributes = *piAttributes_ptr;
                    piValues = *piValues_ptr;
                    return retval;
                }
            }
        }

        public static 
        Boolean wglGetPixelFormatAttribivEXT(IntPtr hdc, int iPixelFormat, int iLayerPlane, Int32 nAttributes, [Out] out int piAttributes, [Out] out int piValues)
        {
            unsafe
            {
                fixed (int* piAttributes_ptr = &piAttributes)
                fixed (int* piValues_ptr = &piValues)
                {
                    Boolean retval = Delegates.wglGetPixelFormatAttribivEXT((IntPtr)hdc, (int)iPixelFormat, (int)iLayerPlane, (UInt32)nAttributes, (int*)piAttributes_ptr, (int*)piValues_ptr);
                    piAttributes = *piAttributes_ptr;
                    piValues = *piValues_ptr;
                    return retval;
                }
            }
        }

        [System.CLSCompliant(false)]
        public static 
        Boolean wglGetPixelFormatAttribivEXT(IntPtr hdc, int iPixelFormat, int iLayerPlane, UInt32 nAttributes, [Out] IntPtr piAttributes, [Out] IntPtr piValues)
        {
            unsafe
            {
                return Delegates.wglGetPixelFormatAttribivEXT((IntPtr)hdc, (int)iPixelFormat, (int)iLayerPlane, (UInt32)nAttributes, (int*)piAttributes, (int*)piValues);
            }
        }

        [System.CLSCompliant(false)]
        public static 
        Boolean wglGetPixelFormatAttribivEXT(IntPtr hdc, int iPixelFormat, int iLayerPlane, Int32 nAttributes, [Out] IntPtr piAttributes, [Out] IntPtr piValues)
        {
            unsafe
            {
                return Delegates.wglGetPixelFormatAttribivEXT((IntPtr)hdc, (int)iPixelFormat, (int)iLayerPlane, (UInt32)nAttributes, (int*)piAttributes, (int*)piValues);
            }
        }

        [System.CLSCompliant(false)]
        public static 
        Boolean wglGetPixelFormatAttribfvEXT(IntPtr hdc, int iPixelFormat, int iLayerPlane, UInt32 nAttributes, [Out] int[] piAttributes, [Out] Single[] pfValues)
        {
            unsafe
            {
                fixed (int* piAttributes_ptr = piAttributes)
                fixed (Single* pfValues_ptr = pfValues)
                {
                    return Delegates.wglGetPixelFormatAttribfvEXT((IntPtr)hdc, (int)iPixelFormat, (int)iLayerPlane, (UInt32)nAttributes, (int*)piAttributes_ptr, (Single*)pfValues_ptr);
                }
            }
        }

        public static 
        Boolean wglGetPixelFormatAttribfvEXT(IntPtr hdc, int iPixelFormat, int iLayerPlane, Int32 nAttributes, [Out] int[] piAttributes, [Out] Single[] pfValues)
        {
            unsafe
            {
                fixed (int* piAttributes_ptr = piAttributes)
                fixed (Single* pfValues_ptr = pfValues)
                {
                    return Delegates.wglGetPixelFormatAttribfvEXT((IntPtr)hdc, (int)iPixelFormat, (int)iLayerPlane, (UInt32)nAttributes, (int*)piAttributes_ptr, (Single*)pfValues_ptr);
                }
            }
        }

        [System.CLSCompliant(false)]
        public static 
        Boolean wglGetPixelFormatAttribfvEXT(IntPtr hdc, int iPixelFormat, int iLayerPlane, UInt32 nAttributes, [Out] out int piAttributes, [Out] out Single pfValues)
        {
            unsafe
            {
                fixed (int* piAttributes_ptr = &piAttributes)
                fixed (Single* pfValues_ptr = &pfValues)
                {
                    Boolean retval = Delegates.wglGetPixelFormatAttribfvEXT((IntPtr)hdc, (int)iPixelFormat, (int)iLayerPlane, (UInt32)nAttributes, (int*)piAttributes_ptr, (Single*)pfValues_ptr);
                    piAttributes = *piAttributes_ptr;
                    pfValues = *pfValues_ptr;
                    return retval;
                }
            }
        }

        public static 
        Boolean wglGetPixelFormatAttribfvEXT(IntPtr hdc, int iPixelFormat, int iLayerPlane, Int32 nAttributes, [Out] out int piAttributes, [Out] out Single pfValues)
        {
            unsafe
            {
                fixed (int* piAttributes_ptr = &piAttributes)
                fixed (Single* pfValues_ptr = &pfValues)
                {
                    Boolean retval = Delegates.wglGetPixelFormatAttribfvEXT((IntPtr)hdc, (int)iPixelFormat, (int)iLayerPlane, (UInt32)nAttributes, (int*)piAttributes_ptr, (Single*)pfValues_ptr);
                    piAttributes = *piAttributes_ptr;
                    pfValues = *pfValues_ptr;
                    return retval;
                }
            }
        }

        [System.CLSCompliant(false)]
        public static 
        Boolean wglGetPixelFormatAttribfvEXT(IntPtr hdc, int iPixelFormat, int iLayerPlane, UInt32 nAttributes, [Out] IntPtr piAttributes, [Out] IntPtr pfValues)
        {
            unsafe
            {
                return Delegates.wglGetPixelFormatAttribfvEXT((IntPtr)hdc, (int)iPixelFormat, (int)iLayerPlane, (UInt32)nAttributes, (int*)piAttributes, (Single*)pfValues);
            }
        }

        [System.CLSCompliant(false)]
        public static 
        Boolean wglGetPixelFormatAttribfvEXT(IntPtr hdc, int iPixelFormat, int iLayerPlane, Int32 nAttributes, [Out] IntPtr piAttributes, [Out] IntPtr pfValues)
        {
            unsafe
            {
                return Delegates.wglGetPixelFormatAttribfvEXT((IntPtr)hdc, (int)iPixelFormat, (int)iLayerPlane, (UInt32)nAttributes, (int*)piAttributes, (Single*)pfValues);
            }
        }

        [System.CLSCompliant(false)]
        public static 
        Boolean wglChoosePixelFormatEXT(IntPtr hdc, int[] piAttribIList, Single[] pfAttribFList, UInt32 nMaxFormats, [Out] int[] piFormats, [Out] UInt32[] nNumFormats)
        {
            unsafe
            {
                fixed (int* piAttribIList_ptr = piAttribIList)
                fixed (Single* pfAttribFList_ptr = pfAttribFList)
                fixed (int* piFormats_ptr = piFormats)
                fixed (UInt32* nNumFormats_ptr = nNumFormats)
                {
                    return Delegates.wglChoosePixelFormatEXT((IntPtr)hdc, (int*)piAttribIList_ptr, (Single*)pfAttribFList_ptr, (UInt32)nMaxFormats, (int*)piFormats_ptr, (UInt32*)nNumFormats_ptr);
                }
            }
        }

        public static 
        Boolean wglChoosePixelFormatEXT(IntPtr hdc, int[] piAttribIList, Single[] pfAttribFList, Int32 nMaxFormats, [Out] int[] piFormats, [Out] Int32[] nNumFormats)
        {
            unsafe
            {
                fixed (int* piAttribIList_ptr = piAttribIList)
                fixed (Single* pfAttribFList_ptr = pfAttribFList)
                fixed (int* piFormats_ptr = piFormats)
                fixed (Int32* nNumFormats_ptr = nNumFormats)
                {
                    return Delegates.wglChoosePixelFormatEXT((IntPtr)hdc, (int*)piAttribIList_ptr, (Single*)pfAttribFList_ptr, (UInt32)nMaxFormats, (int*)piFormats_ptr, (UInt32*)nNumFormats_ptr);
                }
            }
        }

        [System.CLSCompliant(false)]
        public static 
        Boolean wglChoosePixelFormatEXT(IntPtr hdc, ref int piAttribIList, ref Single pfAttribFList, UInt32 nMaxFormats, [Out] out int piFormats, [Out] out UInt32 nNumFormats)
        {
            unsafe
            {
                fixed (int* piAttribIList_ptr = &piAttribIList)
                fixed (Single* pfAttribFList_ptr = &pfAttribFList)
                fixed (int* piFormats_ptr = &piFormats)
                fixed (UInt32* nNumFormats_ptr = &nNumFormats)
                {
                    Boolean retval = Delegates.wglChoosePixelFormatEXT((IntPtr)hdc, (int*)piAttribIList_ptr, (Single*)pfAttribFList_ptr, (UInt32)nMaxFormats, (int*)piFormats_ptr, (UInt32*)nNumFormats_ptr);
                    piFormats = *piFormats_ptr;
                    nNumFormats = *nNumFormats_ptr;
                    return retval;
                }
            }
        }

        public static 
        Boolean wglChoosePixelFormatEXT(IntPtr hdc, ref int piAttribIList, ref Single pfAttribFList, Int32 nMaxFormats, [Out] out int piFormats, [Out] out Int32 nNumFormats)
        {
            unsafe
            {
                fixed (int* piAttribIList_ptr = &piAttribIList)
                fixed (Single* pfAttribFList_ptr = &pfAttribFList)
                fixed (int* piFormats_ptr = &piFormats)
                fixed (Int32* nNumFormats_ptr = &nNumFormats)
                {
                    Boolean retval = Delegates.wglChoosePixelFormatEXT((IntPtr)hdc, (int*)piAttribIList_ptr, (Single*)pfAttribFList_ptr, (UInt32)nMaxFormats, (int*)piFormats_ptr, (UInt32*)nNumFormats_ptr);
                    piFormats = *piFormats_ptr;
                    nNumFormats = *nNumFormats_ptr;
                    return retval;
                }
            }
        }

        [System.CLSCompliant(false)]
        public static 
        Boolean wglChoosePixelFormatEXT(IntPtr hdc, IntPtr piAttribIList, IntPtr pfAttribFList, UInt32 nMaxFormats, [Out] IntPtr piFormats, [Out] IntPtr nNumFormats)
        {
            unsafe
            {
                return Delegates.wglChoosePixelFormatEXT((IntPtr)hdc, (int*)piAttribIList, (Single*)pfAttribFList, (UInt32)nMaxFormats, (int*)piFormats, (UInt32*)nNumFormats);
            }
        }

        public static 
        Boolean wglChoosePixelFormatEXT(IntPtr hdc, IntPtr piAttribIList, IntPtr pfAttribFList, Int32 nMaxFormats, [Out] IntPtr piFormats, [Out] IntPtr nNumFormats)
        {
            unsafe
            {
                return Delegates.wglChoosePixelFormatEXT((IntPtr)hdc, (int*)piAttribIList, (Single*)pfAttribFList, (UInt32)nMaxFormats, (int*)piFormats, (UInt32*)nNumFormats);
            }
        }

        public static 
        Boolean wglSwapIntervalEXT(int interval)
        {
            return Delegates.wglSwapIntervalEXT((int)interval);
        }

        public static 
        int wglGetSwapIntervalEXT()
        {
            return Delegates.wglGetSwapIntervalEXT();
        }

        public static 
        IntPtr wglAllocateMemoryNV(Int32 size, Single readfreq, Single writefreq, Single priority)
        {
            return Delegates.wglAllocateMemoryNV((Int32)size, (Single)readfreq, (Single)writefreq, (Single)priority);
        }

        public static 
        void wglFreeMemoryNV([Out] IntPtr[] pointer)
        {
            unsafe
            {
                fixed (IntPtr* pointer_ptr = pointer)
                {
                    Delegates.wglFreeMemoryNV((IntPtr*)pointer_ptr);
                }
            }
        }

        public static 
        void wglFreeMemoryNV([Out] out IntPtr pointer)
        {
            unsafe
            {
                fixed (IntPtr* pointer_ptr = &pointer)
                {
                    Delegates.wglFreeMemoryNV((IntPtr*)pointer_ptr);
                    pointer = *pointer_ptr;
                }
            }
        }

        [System.CLSCompliant(false)]
        public static 
        void wglFreeMemoryNV([Out] IntPtr pointer)
        {
            unsafe
            {
                Delegates.wglFreeMemoryNV((IntPtr*)pointer);
            }
        }

        public static 
        Boolean wglGetSyncValuesOML(IntPtr hdc, [Out] Int64[] ust, [Out] Int64[] msc, [Out] Int64[] sbc)
        {
            unsafe
            {
                fixed (Int64* ust_ptr = ust)
                fixed (Int64* msc_ptr = msc)
                fixed (Int64* sbc_ptr = sbc)
                {
                    return Delegates.wglGetSyncValuesOML((IntPtr)hdc, (Int64*)ust_ptr, (Int64*)msc_ptr, (Int64*)sbc_ptr);
                }
            }
        }

        public static 
        Boolean wglGetSyncValuesOML(IntPtr hdc, [Out] out Int64 ust, [Out] out Int64 msc, [Out] out Int64 sbc)
        {
            unsafe
            {
                fixed (Int64* ust_ptr = &ust)
                fixed (Int64* msc_ptr = &msc)
                fixed (Int64* sbc_ptr = &sbc)
                {
                    Boolean retval = Delegates.wglGetSyncValuesOML((IntPtr)hdc, (Int64*)ust_ptr, (Int64*)msc_ptr, (Int64*)sbc_ptr);
                    ust = *ust_ptr;
                    msc = *msc_ptr;
                    sbc = *sbc_ptr;
                    return retval;
                }
            }
        }

        [System.CLSCompliant(false)]
        public static 
        Boolean wglGetSyncValuesOML(IntPtr hdc, [Out] IntPtr ust, [Out] IntPtr msc, [Out] IntPtr sbc)
        {
            unsafe
            {
                return Delegates.wglGetSyncValuesOML((IntPtr)hdc, (Int64*)ust, (Int64*)msc, (Int64*)sbc);
            }
        }

        public static 
        Boolean wglGetMscRateOML(IntPtr hdc, [Out] Int32[] numerator, [Out] Int32[] denominator)
        {
            unsafe
            {
                fixed (Int32* numerator_ptr = numerator)
                fixed (Int32* denominator_ptr = denominator)
                {
                    return Delegates.wglGetMscRateOML((IntPtr)hdc, (Int32*)numerator_ptr, (Int32*)denominator_ptr);
                }
            }
        }

        public static 
        Boolean wglGetMscRateOML(IntPtr hdc, [Out] out Int32 numerator, [Out] out Int32 denominator)
        {
            unsafe
            {
                fixed (Int32* numerator_ptr = &numerator)
                fixed (Int32* denominator_ptr = &denominator)
                {
                    Boolean retval = Delegates.wglGetMscRateOML((IntPtr)hdc, (Int32*)numerator_ptr, (Int32*)denominator_ptr);
                    numerator = *numerator_ptr;
                    denominator = *denominator_ptr;
                    return retval;
                }
            }
        }

        [System.CLSCompliant(false)]
        public static 
        Boolean wglGetMscRateOML(IntPtr hdc, [Out] IntPtr numerator, [Out] IntPtr denominator)
        {
            unsafe
            {
                return Delegates.wglGetMscRateOML((IntPtr)hdc, (Int32*)numerator, (Int32*)denominator);
            }
        }

        public static 
        Int64 wglSwapBuffersMscOML(IntPtr hdc, Int64 target_msc, Int64 divisor, Int64 remainder)
        {
            return Delegates.wglSwapBuffersMscOML((IntPtr)hdc, (Int64)target_msc, (Int64)divisor, (Int64)remainder);
        }

        public static 
        Int64 wglSwapLayerBuffersMscOML(IntPtr hdc, int fuPlanes, Int64 target_msc, Int64 divisor, Int64 remainder)
        {
            return Delegates.wglSwapLayerBuffersMscOML((IntPtr)hdc, (int)fuPlanes, (Int64)target_msc, (Int64)divisor, (Int64)remainder);
        }

        public static 
        Boolean wglWaitForMscOML(IntPtr hdc, Int64 target_msc, Int64 divisor, Int64 remainder, [Out] Int64[] ust, [Out] Int64[] msc, [Out] Int64[] sbc)
        {
            unsafe
            {
                fixed (Int64* ust_ptr = ust)
                fixed (Int64* msc_ptr = msc)
                fixed (Int64* sbc_ptr = sbc)
                {
                    return Delegates.wglWaitForMscOML((IntPtr)hdc, (Int64)target_msc, (Int64)divisor, (Int64)remainder, (Int64*)ust_ptr, (Int64*)msc_ptr, (Int64*)sbc_ptr);
                }
            }
        }

        public static 
        Boolean wglWaitForMscOML(IntPtr hdc, Int64 target_msc, Int64 divisor, Int64 remainder, [Out] out Int64 ust, [Out] out Int64 msc, [Out] out Int64 sbc)
        {
            unsafe
            {
                fixed (Int64* ust_ptr = &ust)
                fixed (Int64* msc_ptr = &msc)
                fixed (Int64* sbc_ptr = &sbc)
                {
                    Boolean retval = Delegates.wglWaitForMscOML((IntPtr)hdc, (Int64)target_msc, (Int64)divisor, (Int64)remainder, (Int64*)ust_ptr, (Int64*)msc_ptr, (Int64*)sbc_ptr);
                    ust = *ust_ptr;
                    msc = *msc_ptr;
                    sbc = *sbc_ptr;
                    return retval;
                }
            }
        }

        [System.CLSCompliant(false)]
        public static 
        Boolean wglWaitForMscOML(IntPtr hdc, Int64 target_msc, Int64 divisor, Int64 remainder, [Out] IntPtr ust, [Out] IntPtr msc, [Out] IntPtr sbc)
        {
            unsafe
            {
                return Delegates.wglWaitForMscOML((IntPtr)hdc, (Int64)target_msc, (Int64)divisor, (Int64)remainder, (Int64*)ust, (Int64*)msc, (Int64*)sbc);
            }
        }

        public static 
        Boolean wglWaitForSbcOML(IntPtr hdc, Int64 target_sbc, [Out] Int64[] ust, [Out] Int64[] msc, [Out] Int64[] sbc)
        {
            unsafe
            {
                fixed (Int64* ust_ptr = ust)
                fixed (Int64* msc_ptr = msc)
                fixed (Int64* sbc_ptr = sbc)
                {
                    return Delegates.wglWaitForSbcOML((IntPtr)hdc, (Int64)target_sbc, (Int64*)ust_ptr, (Int64*)msc_ptr, (Int64*)sbc_ptr);
                }
            }
        }

        public static 
        Boolean wglWaitForSbcOML(IntPtr hdc, Int64 target_sbc, [Out] out Int64 ust, [Out] out Int64 msc, [Out] out Int64 sbc)
        {
            unsafe
            {
                fixed (Int64* ust_ptr = &ust)
                fixed (Int64* msc_ptr = &msc)
                fixed (Int64* sbc_ptr = &sbc)
                {
                    Boolean retval = Delegates.wglWaitForSbcOML((IntPtr)hdc, (Int64)target_sbc, (Int64*)ust_ptr, (Int64*)msc_ptr, (Int64*)sbc_ptr);
                    ust = *ust_ptr;
                    msc = *msc_ptr;
                    sbc = *sbc_ptr;
                    return retval;
                }
            }
        }

        [System.CLSCompliant(false)]
        public static 
        Boolean wglWaitForSbcOML(IntPtr hdc, Int64 target_sbc, [Out] IntPtr ust, [Out] IntPtr msc, [Out] IntPtr sbc)
        {
            unsafe
            {
                return Delegates.wglWaitForSbcOML((IntPtr)hdc, (Int64)target_sbc, (Int64*)ust, (Int64*)msc, (Int64*)sbc);
            }
        }

        public static 
        Boolean wglGetDigitalVideoParametersI3D(IntPtr hDC, int iAttribute, [Out] int[] piValue)
        {
            unsafe
            {
                fixed (int* piValue_ptr = piValue)
                {
                    return Delegates.wglGetDigitalVideoParametersI3D((IntPtr)hDC, (int)iAttribute, (int*)piValue_ptr);
                }
            }
        }

        public static 
        Boolean wglGetDigitalVideoParametersI3D(IntPtr hDC, int iAttribute, [Out] out int piValue)
        {
            unsafe
            {
                fixed (int* piValue_ptr = &piValue)
                {
                    Boolean retval = Delegates.wglGetDigitalVideoParametersI3D((IntPtr)hDC, (int)iAttribute, (int*)piValue_ptr);
                    piValue = *piValue_ptr;
                    return retval;
                }
            }
        }

        [System.CLSCompliant(false)]
        public static 
        Boolean wglGetDigitalVideoParametersI3D(IntPtr hDC, int iAttribute, [Out] IntPtr piValue)
        {
            unsafe
            {
                return Delegates.wglGetDigitalVideoParametersI3D((IntPtr)hDC, (int)iAttribute, (int*)piValue);
            }
        }

        public static 
        Boolean wglSetDigitalVideoParametersI3D(IntPtr hDC, int iAttribute, int[] piValue)
        {
            unsafe
            {
                fixed (int* piValue_ptr = piValue)
                {
                    return Delegates.wglSetDigitalVideoParametersI3D((IntPtr)hDC, (int)iAttribute, (int*)piValue_ptr);
                }
            }
        }

        public static 
        Boolean wglSetDigitalVideoParametersI3D(IntPtr hDC, int iAttribute, ref int piValue)
        {
            unsafe
            {
                fixed (int* piValue_ptr = &piValue)
                {
                    return Delegates.wglSetDigitalVideoParametersI3D((IntPtr)hDC, (int)iAttribute, (int*)piValue_ptr);
                }
            }
        }

        [System.CLSCompliant(false)]
        public static 
        Boolean wglSetDigitalVideoParametersI3D(IntPtr hDC, int iAttribute, IntPtr piValue)
        {
            unsafe
            {
                return Delegates.wglSetDigitalVideoParametersI3D((IntPtr)hDC, (int)iAttribute, (int*)piValue);
            }
        }

        public static 
        Boolean wglGetGammaTableParametersI3D(IntPtr hDC, int iAttribute, [Out] int[] piValue)
        {
            unsafe
            {
                fixed (int* piValue_ptr = piValue)
                {
                    return Delegates.wglGetGammaTableParametersI3D((IntPtr)hDC, (int)iAttribute, (int*)piValue_ptr);
                }
            }
        }

        public static 
        Boolean wglGetGammaTableParametersI3D(IntPtr hDC, int iAttribute, [Out] out int piValue)
        {
            unsafe
            {
                fixed (int* piValue_ptr = &piValue)
                {
                    Boolean retval = Delegates.wglGetGammaTableParametersI3D((IntPtr)hDC, (int)iAttribute, (int*)piValue_ptr);
                    piValue = *piValue_ptr;
                    return retval;
                }
            }
        }

        [System.CLSCompliant(false)]
        public static 
        Boolean wglGetGammaTableParametersI3D(IntPtr hDC, int iAttribute, [Out] IntPtr piValue)
        {
            unsafe
            {
                return Delegates.wglGetGammaTableParametersI3D((IntPtr)hDC, (int)iAttribute, (int*)piValue);
            }
        }

        public static 
        Boolean wglSetGammaTableParametersI3D(IntPtr hDC, int iAttribute, int[] piValue)
        {
            unsafe
            {
                fixed (int* piValue_ptr = piValue)
                {
                    return Delegates.wglSetGammaTableParametersI3D((IntPtr)hDC, (int)iAttribute, (int*)piValue_ptr);
                }
            }
        }

        public static 
        Boolean wglSetGammaTableParametersI3D(IntPtr hDC, int iAttribute, ref int piValue)
        {
            unsafe
            {
                fixed (int* piValue_ptr = &piValue)
                {
                    return Delegates.wglSetGammaTableParametersI3D((IntPtr)hDC, (int)iAttribute, (int*)piValue_ptr);
                }
            }
        }

        [System.CLSCompliant(false)]
        public static 
        Boolean wglSetGammaTableParametersI3D(IntPtr hDC, int iAttribute, IntPtr piValue)
        {
            unsafe
            {
                return Delegates.wglSetGammaTableParametersI3D((IntPtr)hDC, (int)iAttribute, (int*)piValue);
            }
        }

        [System.CLSCompliant(false)]
        public static 
        Boolean wglGetGammaTableI3D(IntPtr hDC, int iEntries, [Out] UInt16[] puRed, [Out] UInt16[] puGreen, [Out] UInt16[] puBlue)
        {
            unsafe
            {
                fixed (UInt16* puRed_ptr = puRed)
                fixed (UInt16* puGreen_ptr = puGreen)
                fixed (UInt16* puBlue_ptr = puBlue)
                {
                    return Delegates.wglGetGammaTableI3D((IntPtr)hDC, (int)iEntries, (UInt16*)puRed_ptr, (UInt16*)puGreen_ptr, (UInt16*)puBlue_ptr);
                }
            }
        }

        public static 
        Boolean wglGetGammaTableI3D(IntPtr hDC, int iEntries, [Out] Int16[] puRed, [Out] Int16[] puGreen, [Out] Int16[] puBlue)
        {
            unsafe
            {
                fixed (Int16* puRed_ptr = puRed)
                fixed (Int16* puGreen_ptr = puGreen)
                fixed (Int16* puBlue_ptr = puBlue)
                {
                    return Delegates.wglGetGammaTableI3D((IntPtr)hDC, (int)iEntries, (UInt16*)puRed_ptr, (UInt16*)puGreen_ptr, (UInt16*)puBlue_ptr);
                }
            }
        }

        [System.CLSCompliant(false)]
        public static 
        Boolean wglGetGammaTableI3D(IntPtr hDC, int iEntries, [Out] out UInt16 puRed, [Out] out UInt16 puGreen, [Out] out UInt16 puBlue)
        {
            unsafe
            {
                fixed (UInt16* puRed_ptr = &puRed)
                fixed (UInt16* puGreen_ptr = &puGreen)
                fixed (UInt16* puBlue_ptr = &puBlue)
                {
                    Boolean retval = Delegates.wglGetGammaTableI3D((IntPtr)hDC, (int)iEntries, (UInt16*)puRed_ptr, (UInt16*)puGreen_ptr, (UInt16*)puBlue_ptr);
                    puRed = *puRed_ptr;
                    puGreen = *puGreen_ptr;
                    puBlue = *puBlue_ptr;
                    return retval;
                }
            }
        }

        public static 
        Boolean wglGetGammaTableI3D(IntPtr hDC, int iEntries, [Out] out Int16 puRed, [Out] out Int16 puGreen, [Out] out Int16 puBlue)
        {
            unsafe
            {
                fixed (Int16* puRed_ptr = &puRed)
                fixed (Int16* puGreen_ptr = &puGreen)
                fixed (Int16* puBlue_ptr = &puBlue)
                {
                    Boolean retval = Delegates.wglGetGammaTableI3D((IntPtr)hDC, (int)iEntries, (UInt16*)puRed_ptr, (UInt16*)puGreen_ptr, (UInt16*)puBlue_ptr);
                    puRed = *puRed_ptr;
                    puGreen = *puGreen_ptr;
                    puBlue = *puBlue_ptr;
                    return retval;
                }
            }
        }

        public static 
        Boolean wglGetGammaTableI3D(IntPtr hDC, int iEntries, [Out] IntPtr puRed, [Out] IntPtr puGreen, [Out] IntPtr puBlue)
        {
            unsafe
            {
                return Delegates.wglGetGammaTableI3D((IntPtr)hDC, (int)iEntries, (UInt16*)puRed, (UInt16*)puGreen, (UInt16*)puBlue);
            }
        }

        [System.CLSCompliant(false)]
        public static 
        Boolean wglSetGammaTableI3D(IntPtr hDC, int iEntries, UInt16[] puRed, UInt16[] puGreen, UInt16[] puBlue)
        {
            unsafe
            {
                fixed (UInt16* puRed_ptr = puRed)
                fixed (UInt16* puGreen_ptr = puGreen)
                fixed (UInt16* puBlue_ptr = puBlue)
                {
                    return Delegates.wglSetGammaTableI3D((IntPtr)hDC, (int)iEntries, (UInt16*)puRed_ptr, (UInt16*)puGreen_ptr, (UInt16*)puBlue_ptr);
                }
            }
        }

        public static 
        Boolean wglSetGammaTableI3D(IntPtr hDC, int iEntries, Int16[] puRed, Int16[] puGreen, Int16[] puBlue)
        {
            unsafe
            {
                fixed (Int16* puRed_ptr = puRed)
                fixed (Int16* puGreen_ptr = puGreen)
                fixed (Int16* puBlue_ptr = puBlue)
                {
                    return Delegates.wglSetGammaTableI3D((IntPtr)hDC, (int)iEntries, (UInt16*)puRed_ptr, (UInt16*)puGreen_ptr, (UInt16*)puBlue_ptr);
                }
            }
        }

        [System.CLSCompliant(false)]
        public static 
        Boolean wglSetGammaTableI3D(IntPtr hDC, int iEntries, ref UInt16 puRed, ref UInt16 puGreen, ref UInt16 puBlue)
        {
            unsafe
            {
                fixed (UInt16* puRed_ptr = &puRed)
                fixed (UInt16* puGreen_ptr = &puGreen)
                fixed (UInt16* puBlue_ptr = &puBlue)
                {
                    return Delegates.wglSetGammaTableI3D((IntPtr)hDC, (int)iEntries, (UInt16*)puRed_ptr, (UInt16*)puGreen_ptr, (UInt16*)puBlue_ptr);
                }
            }
        }

        public static 
        Boolean wglSetGammaTableI3D(IntPtr hDC, int iEntries, ref Int16 puRed, ref Int16 puGreen, ref Int16 puBlue)
        {
            unsafe
            {
                fixed (Int16* puRed_ptr = &puRed)
                fixed (Int16* puGreen_ptr = &puGreen)
                fixed (Int16* puBlue_ptr = &puBlue)
                {
                    return Delegates.wglSetGammaTableI3D((IntPtr)hDC, (int)iEntries, (UInt16*)puRed_ptr, (UInt16*)puGreen_ptr, (UInt16*)puBlue_ptr);
                }
            }
        }

        public static 
        Boolean wglSetGammaTableI3D(IntPtr hDC, int iEntries, IntPtr puRed, IntPtr puGreen, IntPtr puBlue)
        {
            unsafe
            {
                return Delegates.wglSetGammaTableI3D((IntPtr)hDC, (int)iEntries, (UInt16*)puRed, (UInt16*)puGreen, (UInt16*)puBlue);
            }
        }

        public static 
        Boolean wglEnableGenlockI3D(IntPtr hDC)
        {
            return Delegates.wglEnableGenlockI3D((IntPtr)hDC);
        }

        public static 
        Boolean wglDisableGenlockI3D(IntPtr hDC)
        {
            return Delegates.wglDisableGenlockI3D((IntPtr)hDC);
        }

        public static 
        Boolean wglIsEnabledGenlockI3D(IntPtr hDC, [Out] Boolean[] pFlag)
        {
            unsafe
            {
                fixed (Boolean* pFlag_ptr = pFlag)
                {
                    return Delegates.wglIsEnabledGenlockI3D((IntPtr)hDC, (Boolean*)pFlag_ptr);
                }
            }
        }

        public static 
        Boolean wglIsEnabledGenlockI3D(IntPtr hDC, [Out] out Boolean pFlag)
        {
            unsafe
            {
                fixed (Boolean* pFlag_ptr = &pFlag)
                {
                    Boolean retval = Delegates.wglIsEnabledGenlockI3D((IntPtr)hDC, (Boolean*)pFlag_ptr);
                    pFlag = *pFlag_ptr;
                    return retval;
                }
            }
        }

        [System.CLSCompliant(false)]
        public static 
        Boolean wglIsEnabledGenlockI3D(IntPtr hDC, [Out] IntPtr pFlag)
        {
            unsafe
            {
                return Delegates.wglIsEnabledGenlockI3D((IntPtr)hDC, (Boolean*)pFlag);
            }
        }

        [System.CLSCompliant(false)]
        public static 
        Boolean wglGenlockSourceI3D(IntPtr hDC, UInt32 uSource)
        {
            return Delegates.wglGenlockSourceI3D((IntPtr)hDC, (UInt32)uSource);
        }

        public static 
        Boolean wglGenlockSourceI3D(IntPtr hDC, Int32 uSource)
        {
            return Delegates.wglGenlockSourceI3D((IntPtr)hDC, (UInt32)uSource);
        }

        [System.CLSCompliant(false)]
        public static 
        Boolean wglGetGenlockSourceI3D(IntPtr hDC, [Out] UInt32[] uSource)
        {
            unsafe
            {
                fixed (UInt32* uSource_ptr = uSource)
                {
                    return Delegates.wglGetGenlockSourceI3D((IntPtr)hDC, (UInt32*)uSource_ptr);
                }
            }
        }

        public static 
        Boolean wglGetGenlockSourceI3D(IntPtr hDC, [Out] Int32[] uSource)
        {
            unsafe
            {
                fixed (Int32* uSource_ptr = uSource)
                {
                    return Delegates.wglGetGenlockSourceI3D((IntPtr)hDC, (UInt32*)uSource_ptr);
                }
            }
        }

        [System.CLSCompliant(false)]
        public static 
        Boolean wglGetGenlockSourceI3D(IntPtr hDC, [Out] out UInt32 uSource)
        {
            unsafe
            {
                fixed (UInt32* uSource_ptr = &uSource)
                {
                    Boolean retval = Delegates.wglGetGenlockSourceI3D((IntPtr)hDC, (UInt32*)uSource_ptr);
                    uSource = *uSource_ptr;
                    return retval;
                }
            }
        }

        public static 
        Boolean wglGetGenlockSourceI3D(IntPtr hDC, [Out] out Int32 uSource)
        {
            unsafe
            {
                fixed (Int32* uSource_ptr = &uSource)
                {
                    Boolean retval = Delegates.wglGetGenlockSourceI3D((IntPtr)hDC, (UInt32*)uSource_ptr);
                    uSource = *uSource_ptr;
                    return retval;
                }
            }
        }

        public static 
        Boolean wglGetGenlockSourceI3D(IntPtr hDC, [Out] IntPtr uSource)
        {
            unsafe
            {
                return Delegates.wglGetGenlockSourceI3D((IntPtr)hDC, (UInt32*)uSource);
            }
        }

        [System.CLSCompliant(false)]
        public static 
        Boolean wglGenlockSourceEdgeI3D(IntPtr hDC, UInt32 uEdge)
        {
            return Delegates.wglGenlockSourceEdgeI3D((IntPtr)hDC, (UInt32)uEdge);
        }

        public static 
        Boolean wglGenlockSourceEdgeI3D(IntPtr hDC, Int32 uEdge)
        {
            return Delegates.wglGenlockSourceEdgeI3D((IntPtr)hDC, (UInt32)uEdge);
        }

        [System.CLSCompliant(false)]
        public static 
        Boolean wglGetGenlockSourceEdgeI3D(IntPtr hDC, [Out] UInt32[] uEdge)
        {
            unsafe
            {
                fixed (UInt32* uEdge_ptr = uEdge)
                {
                    return Delegates.wglGetGenlockSourceEdgeI3D((IntPtr)hDC, (UInt32*)uEdge_ptr);
                }
            }
        }

        public static 
        Boolean wglGetGenlockSourceEdgeI3D(IntPtr hDC, [Out] Int32[] uEdge)
        {
            unsafe
            {
                fixed (Int32* uEdge_ptr = uEdge)
                {
                    return Delegates.wglGetGenlockSourceEdgeI3D((IntPtr)hDC, (UInt32*)uEdge_ptr);
                }
            }
        }

        [System.CLSCompliant(false)]
        public static 
        Boolean wglGetGenlockSourceEdgeI3D(IntPtr hDC, [Out] out UInt32 uEdge)
        {
            unsafe
            {
                fixed (UInt32* uEdge_ptr = &uEdge)
                {
                    Boolean retval = Delegates.wglGetGenlockSourceEdgeI3D((IntPtr)hDC, (UInt32*)uEdge_ptr);
                    uEdge = *uEdge_ptr;
                    return retval;
                }
            }
        }

        public static 
        Boolean wglGetGenlockSourceEdgeI3D(IntPtr hDC, [Out] out Int32 uEdge)
        {
            unsafe
            {
                fixed (Int32* uEdge_ptr = &uEdge)
                {
                    Boolean retval = Delegates.wglGetGenlockSourceEdgeI3D((IntPtr)hDC, (UInt32*)uEdge_ptr);
                    uEdge = *uEdge_ptr;
                    return retval;
                }
            }
        }

        public static 
        Boolean wglGetGenlockSourceEdgeI3D(IntPtr hDC, [Out] IntPtr uEdge)
        {
            unsafe
            {
                return Delegates.wglGetGenlockSourceEdgeI3D((IntPtr)hDC, (UInt32*)uEdge);
            }
        }

        [System.CLSCompliant(false)]
        public static 
        Boolean wglGenlockSampleRateI3D(IntPtr hDC, UInt32 uRate)
        {
            return Delegates.wglGenlockSampleRateI3D((IntPtr)hDC, (UInt32)uRate);
        }

        public static 
        Boolean wglGenlockSampleRateI3D(IntPtr hDC, Int32 uRate)
        {
            return Delegates.wglGenlockSampleRateI3D((IntPtr)hDC, (UInt32)uRate);
        }

        [System.CLSCompliant(false)]
        public static 
        Boolean wglGetGenlockSampleRateI3D(IntPtr hDC, [Out] UInt32[] uRate)
        {
            unsafe
            {
                fixed (UInt32* uRate_ptr = uRate)
                {
                    return Delegates.wglGetGenlockSampleRateI3D((IntPtr)hDC, (UInt32*)uRate_ptr);
                }
            }
        }

        public static 
        Boolean wglGetGenlockSampleRateI3D(IntPtr hDC, [Out] Int32[] uRate)
        {
            unsafe
            {
                fixed (Int32* uRate_ptr = uRate)
                {
                    return Delegates.wglGetGenlockSampleRateI3D((IntPtr)hDC, (UInt32*)uRate_ptr);
                }
            }
        }

        [System.CLSCompliant(false)]
        public static 
        Boolean wglGetGenlockSampleRateI3D(IntPtr hDC, [Out] out UInt32 uRate)
        {
            unsafe
            {
                fixed (UInt32* uRate_ptr = &uRate)
                {
                    Boolean retval = Delegates.wglGetGenlockSampleRateI3D((IntPtr)hDC, (UInt32*)uRate_ptr);
                    uRate = *uRate_ptr;
                    return retval;
                }
            }
        }

        public static 
        Boolean wglGetGenlockSampleRateI3D(IntPtr hDC, [Out] out Int32 uRate)
        {
            unsafe
            {
                fixed (Int32* uRate_ptr = &uRate)
                {
                    Boolean retval = Delegates.wglGetGenlockSampleRateI3D((IntPtr)hDC, (UInt32*)uRate_ptr);
                    uRate = *uRate_ptr;
                    return retval;
                }
            }
        }

        public static 
        Boolean wglGetGenlockSampleRateI3D(IntPtr hDC, [Out] IntPtr uRate)
        {
            unsafe
            {
                return Delegates.wglGetGenlockSampleRateI3D((IntPtr)hDC, (UInt32*)uRate);
            }
        }

        [System.CLSCompliant(false)]
        public static 
        Boolean wglGenlockSourceDelayI3D(IntPtr hDC, UInt32 uDelay)
        {
            return Delegates.wglGenlockSourceDelayI3D((IntPtr)hDC, (UInt32)uDelay);
        }

        public static 
        Boolean wglGenlockSourceDelayI3D(IntPtr hDC, Int32 uDelay)
        {
            return Delegates.wglGenlockSourceDelayI3D((IntPtr)hDC, (UInt32)uDelay);
        }

        [System.CLSCompliant(false)]
        public static 
        Boolean wglGetGenlockSourceDelayI3D(IntPtr hDC, [Out] UInt32[] uDelay)
        {
            unsafe
            {
                fixed (UInt32* uDelay_ptr = uDelay)
                {
                    return Delegates.wglGetGenlockSourceDelayI3D((IntPtr)hDC, (UInt32*)uDelay_ptr);
                }
            }
        }

        public static 
        Boolean wglGetGenlockSourceDelayI3D(IntPtr hDC, [Out] Int32[] uDelay)
        {
            unsafe
            {
                fixed (Int32* uDelay_ptr = uDelay)
                {
                    return Delegates.wglGetGenlockSourceDelayI3D((IntPtr)hDC, (UInt32*)uDelay_ptr);
                }
            }
        }

        [System.CLSCompliant(false)]
        public static 
        Boolean wglGetGenlockSourceDelayI3D(IntPtr hDC, [Out] out UInt32 uDelay)
        {
            unsafe
            {
                fixed (UInt32* uDelay_ptr = &uDelay)
                {
                    Boolean retval = Delegates.wglGetGenlockSourceDelayI3D((IntPtr)hDC, (UInt32*)uDelay_ptr);
                    uDelay = *uDelay_ptr;
                    return retval;
                }
            }
        }

        public static 
        Boolean wglGetGenlockSourceDelayI3D(IntPtr hDC, [Out] out Int32 uDelay)
        {
            unsafe
            {
                fixed (Int32* uDelay_ptr = &uDelay)
                {
                    Boolean retval = Delegates.wglGetGenlockSourceDelayI3D((IntPtr)hDC, (UInt32*)uDelay_ptr);
                    uDelay = *uDelay_ptr;
                    return retval;
                }
            }
        }

        public static 
        Boolean wglGetGenlockSourceDelayI3D(IntPtr hDC, [Out] IntPtr uDelay)
        {
            unsafe
            {
                return Delegates.wglGetGenlockSourceDelayI3D((IntPtr)hDC, (UInt32*)uDelay);
            }
        }

        [System.CLSCompliant(false)]
        public static 
        Boolean wglQueryGenlockMaxSourceDelayI3D(IntPtr hDC, [Out] UInt32[] uMaxLineDelay, [Out] UInt32[] uMaxPixelDelay)
        {
            unsafe
            {
                fixed (UInt32* uMaxLineDelay_ptr = uMaxLineDelay)
                fixed (UInt32* uMaxPixelDelay_ptr = uMaxPixelDelay)
                {
                    return Delegates.wglQueryGenlockMaxSourceDelayI3D((IntPtr)hDC, (UInt32*)uMaxLineDelay_ptr, (UInt32*)uMaxPixelDelay_ptr);
                }
            }
        }

        public static 
        Boolean wglQueryGenlockMaxSourceDelayI3D(IntPtr hDC, [Out] Int32[] uMaxLineDelay, [Out] Int32[] uMaxPixelDelay)
        {
            unsafe
            {
                fixed (Int32* uMaxLineDelay_ptr = uMaxLineDelay)
                fixed (Int32* uMaxPixelDelay_ptr = uMaxPixelDelay)
                {
                    return Delegates.wglQueryGenlockMaxSourceDelayI3D((IntPtr)hDC, (UInt32*)uMaxLineDelay_ptr, (UInt32*)uMaxPixelDelay_ptr);
                }
            }
        }

        [System.CLSCompliant(false)]
        public static 
        Boolean wglQueryGenlockMaxSourceDelayI3D(IntPtr hDC, [Out] out UInt32 uMaxLineDelay, [Out] out UInt32 uMaxPixelDelay)
        {
            unsafe
            {
                fixed (UInt32* uMaxLineDelay_ptr = &uMaxLineDelay)
                fixed (UInt32* uMaxPixelDelay_ptr = &uMaxPixelDelay)
                {
                    Boolean retval = Delegates.wglQueryGenlockMaxSourceDelayI3D((IntPtr)hDC, (UInt32*)uMaxLineDelay_ptr, (UInt32*)uMaxPixelDelay_ptr);
                    uMaxLineDelay = *uMaxLineDelay_ptr;
                    uMaxPixelDelay = *uMaxPixelDelay_ptr;
                    return retval;
                }
            }
        }

        public static 
        Boolean wglQueryGenlockMaxSourceDelayI3D(IntPtr hDC, [Out] out Int32 uMaxLineDelay, [Out] out Int32 uMaxPixelDelay)
        {
            unsafe
            {
                fixed (Int32* uMaxLineDelay_ptr = &uMaxLineDelay)
                fixed (Int32* uMaxPixelDelay_ptr = &uMaxPixelDelay)
                {
                    Boolean retval = Delegates.wglQueryGenlockMaxSourceDelayI3D((IntPtr)hDC, (UInt32*)uMaxLineDelay_ptr, (UInt32*)uMaxPixelDelay_ptr);
                    uMaxLineDelay = *uMaxLineDelay_ptr;
                    uMaxPixelDelay = *uMaxPixelDelay_ptr;
                    return retval;
                }
            }
        }

        public static 
        Boolean wglQueryGenlockMaxSourceDelayI3D(IntPtr hDC, [Out] IntPtr uMaxLineDelay, [Out] IntPtr uMaxPixelDelay)
        {
            unsafe
            {
                return Delegates.wglQueryGenlockMaxSourceDelayI3D((IntPtr)hDC, (UInt32*)uMaxLineDelay, (UInt32*)uMaxPixelDelay);
            }
        }

        [System.CLSCompliant(false)]
        public static 
        IntPtr wglCreateImageBufferI3D(IntPtr hDC, Int32 dwSize, UInt32 uFlags)
        {
            return Delegates.wglCreateImageBufferI3D((IntPtr)hDC, (Int32)dwSize, (UInt32)uFlags);
        }

        public static 
        IntPtr wglCreateImageBufferI3D(IntPtr hDC, Int32 dwSize, Int32 uFlags)
        {
            return Delegates.wglCreateImageBufferI3D((IntPtr)hDC, (Int32)dwSize, (UInt32)uFlags);
        }

        public static 
        Boolean wglDestroyImageBufferI3D(IntPtr hDC, IntPtr pAddress)
        {
            unsafe
            {
                return Delegates.wglDestroyImageBufferI3D((IntPtr)hDC, (IntPtr)pAddress);
            }
        }

        public static 
        Boolean wglDestroyImageBufferI3D(IntPtr hDC, [In, Out] object pAddress)
        {
            unsafe
            {
                System.Runtime.InteropServices.GCHandle pAddress_ptr = System.Runtime.InteropServices.GCHandle.Alloc(pAddress, System.Runtime.InteropServices.GCHandleType.Pinned);
                try
                {
                    return Delegates.wglDestroyImageBufferI3D((IntPtr)hDC, (IntPtr)pAddress_ptr.AddrOfPinnedObject());
                }
                finally
                {
                    pAddress_ptr.Free();
                }
            }
        }

        [System.CLSCompliant(false)]
        public static 
        Boolean wglAssociateImageBufferEventsI3D(IntPtr hDC, IntPtr[] pEvent, IntPtr pAddress, Int32[] pSize, UInt32 count)
        {
            unsafe
            {
                fixed (IntPtr* pEvent_ptr = pEvent)
                fixed (Int32* pSize_ptr = pSize)
                {
                    return Delegates.wglAssociateImageBufferEventsI3D((IntPtr)hDC, (IntPtr*)pEvent_ptr, (IntPtr)pAddress, (Int32*)pSize_ptr, (UInt32)count);
                }
            }
        }

        public static 
        Boolean wglAssociateImageBufferEventsI3D(IntPtr hDC, IntPtr[] pEvent, IntPtr pAddress, Int32[] pSize, Int32 count)
        {
            unsafe
            {
                fixed (IntPtr* pEvent_ptr = pEvent)
                fixed (Int32* pSize_ptr = pSize)
                {
                    return Delegates.wglAssociateImageBufferEventsI3D((IntPtr)hDC, (IntPtr*)pEvent_ptr, (IntPtr)pAddress, (Int32*)pSize_ptr, (UInt32)count);
                }
            }
        }

        [System.CLSCompliant(false)]
        public static 
        Boolean wglAssociateImageBufferEventsI3D(IntPtr hDC, IntPtr[] pEvent, [In, Out] object pAddress, Int32[] pSize, UInt32 count)
        {
            unsafe
            {
                fixed (IntPtr* pEvent_ptr = pEvent)
                fixed (Int32* pSize_ptr = pSize)
                {
                    System.Runtime.InteropServices.GCHandle pAddress_ptr = System.Runtime.InteropServices.GCHandle.Alloc(pAddress, System.Runtime.InteropServices.GCHandleType.Pinned);
                    try
                    {
                        return Delegates.wglAssociateImageBufferEventsI3D((IntPtr)hDC, (IntPtr*)pEvent_ptr, (IntPtr)pAddress_ptr.AddrOfPinnedObject(), (Int32*)pSize_ptr, (UInt32)count);
                    }
                    finally
                    {
                        pAddress_ptr.Free();
                    }
                }
            }
        }

        public static 
        Boolean wglAssociateImageBufferEventsI3D(IntPtr hDC, IntPtr[] pEvent, [In, Out] object pAddress, Int32[] pSize, Int32 count)
        {
            unsafe
            {
                fixed (IntPtr* pEvent_ptr = pEvent)
                fixed (Int32* pSize_ptr = pSize)
                {
                    System.Runtime.InteropServices.GCHandle pAddress_ptr = System.Runtime.InteropServices.GCHandle.Alloc(pAddress, System.Runtime.InteropServices.GCHandleType.Pinned);
                    try
                    {
                        return Delegates.wglAssociateImageBufferEventsI3D((IntPtr)hDC, (IntPtr*)pEvent_ptr, (IntPtr)pAddress_ptr.AddrOfPinnedObject(), (Int32*)pSize_ptr, (UInt32)count);
                    }
                    finally
                    {
                        pAddress_ptr.Free();
                    }
                }
            }
        }

        [System.CLSCompliant(false)]
        public static 
        Boolean wglAssociateImageBufferEventsI3D(IntPtr hDC, IntPtr[] pEvent, [In, Out] object pAddress, ref Int32 pSize, UInt32 count)
        {
            unsafe
            {
                fixed (IntPtr* pEvent_ptr = pEvent)
                fixed (Int32* pSize_ptr = &pSize)
                {
                    System.Runtime.InteropServices.GCHandle pAddress_ptr = System.Runtime.InteropServices.GCHandle.Alloc(pAddress, System.Runtime.InteropServices.GCHandleType.Pinned);
                    try
                    {
                        return Delegates.wglAssociateImageBufferEventsI3D((IntPtr)hDC, (IntPtr*)pEvent_ptr, (IntPtr)pAddress_ptr.AddrOfPinnedObject(), (Int32*)pSize_ptr, (UInt32)count);
                    }
                    finally
                    {
                        pAddress_ptr.Free();
                    }
                }
            }
        }

        public static 
        Boolean wglAssociateImageBufferEventsI3D(IntPtr hDC, IntPtr[] pEvent, [In, Out] object pAddress, ref Int32 pSize, Int32 count)
        {
            unsafe
            {
                fixed (IntPtr* pEvent_ptr = pEvent)
                fixed (Int32* pSize_ptr = &pSize)
                {
                    System.Runtime.InteropServices.GCHandle pAddress_ptr = System.Runtime.InteropServices.GCHandle.Alloc(pAddress, System.Runtime.InteropServices.GCHandleType.Pinned);
                    try
                    {
                        return Delegates.wglAssociateImageBufferEventsI3D((IntPtr)hDC, (IntPtr*)pEvent_ptr, (IntPtr)pAddress_ptr.AddrOfPinnedObject(), (Int32*)pSize_ptr, (UInt32)count);
                    }
                    finally
                    {
                        pAddress_ptr.Free();
                    }
                }
            }
        }

        [System.CLSCompliant(false)]
        public static 
        Boolean wglAssociateImageBufferEventsI3D(IntPtr hDC, ref IntPtr pEvent, [In, Out] object pAddress, Int32[] pSize, UInt32 count)
        {
            unsafe
            {
                fixed (IntPtr* pEvent_ptr = &pEvent)
                fixed (Int32* pSize_ptr = pSize)
                {
                    System.Runtime.InteropServices.GCHandle pAddress_ptr = System.Runtime.InteropServices.GCHandle.Alloc(pAddress, System.Runtime.InteropServices.GCHandleType.Pinned);
                    try
                    {
                        return Delegates.wglAssociateImageBufferEventsI3D((IntPtr)hDC, (IntPtr*)pEvent_ptr, (IntPtr)pAddress_ptr.AddrOfPinnedObject(), (Int32*)pSize_ptr, (UInt32)count);
                    }
                    finally
                    {
                        pAddress_ptr.Free();
                    }
                }
            }
        }

        public static 
        Boolean wglAssociateImageBufferEventsI3D(IntPtr hDC, ref IntPtr pEvent, [In, Out] object pAddress, Int32[] pSize, Int32 count)
        {
            unsafe
            {
                fixed (IntPtr* pEvent_ptr = &pEvent)
                fixed (Int32* pSize_ptr = pSize)
                {
                    System.Runtime.InteropServices.GCHandle pAddress_ptr = System.Runtime.InteropServices.GCHandle.Alloc(pAddress, System.Runtime.InteropServices.GCHandleType.Pinned);
                    try
                    {
                        return Delegates.wglAssociateImageBufferEventsI3D((IntPtr)hDC, (IntPtr*)pEvent_ptr, (IntPtr)pAddress_ptr.AddrOfPinnedObject(), (Int32*)pSize_ptr, (UInt32)count);
                    }
                    finally
                    {
                        pAddress_ptr.Free();
                    }
                }
            }
        }

        [System.CLSCompliant(false)]
        public static 
        Boolean wglAssociateImageBufferEventsI3D(IntPtr hDC, ref IntPtr pEvent, [In, Out] object pAddress, ref Int32 pSize, UInt32 count)
        {
            unsafe
            {
                fixed (IntPtr* pEvent_ptr = &pEvent)
                fixed (Int32* pSize_ptr = &pSize)
                {
                    System.Runtime.InteropServices.GCHandle pAddress_ptr = System.Runtime.InteropServices.GCHandle.Alloc(pAddress, System.Runtime.InteropServices.GCHandleType.Pinned);
                    try
                    {
                        return Delegates.wglAssociateImageBufferEventsI3D((IntPtr)hDC, (IntPtr*)pEvent_ptr, (IntPtr)pAddress_ptr.AddrOfPinnedObject(), (Int32*)pSize_ptr, (UInt32)count);
                    }
                    finally
                    {
                        pAddress_ptr.Free();
                    }
                }
            }
        }

        public static 
        Boolean wglAssociateImageBufferEventsI3D(IntPtr hDC, ref IntPtr pEvent, [In, Out] object pAddress, ref Int32 pSize, Int32 count)
        {
            unsafe
            {
                fixed (IntPtr* pEvent_ptr = &pEvent)
                fixed (Int32* pSize_ptr = &pSize)
                {
                    System.Runtime.InteropServices.GCHandle pAddress_ptr = System.Runtime.InteropServices.GCHandle.Alloc(pAddress, System.Runtime.InteropServices.GCHandleType.Pinned);
                    try
                    {
                        return Delegates.wglAssociateImageBufferEventsI3D((IntPtr)hDC, (IntPtr*)pEvent_ptr, (IntPtr)pAddress_ptr.AddrOfPinnedObject(), (Int32*)pSize_ptr, (UInt32)count);
                    }
                    finally
                    {
                        pAddress_ptr.Free();
                    }
                }
            }
        }

        [System.CLSCompliant(false)]
        public static 
        Boolean wglAssociateImageBufferEventsI3D(IntPtr hDC, IntPtr pEvent, [In, Out] object pAddress, IntPtr pSize, UInt32 count)
        {
            unsafe
            {
                System.Runtime.InteropServices.GCHandle pAddress_ptr = System.Runtime.InteropServices.GCHandle.Alloc(pAddress, System.Runtime.InteropServices.GCHandleType.Pinned);
                try
                {
                    return Delegates.wglAssociateImageBufferEventsI3D((IntPtr)hDC, (IntPtr*)pEvent, (IntPtr)pAddress_ptr.AddrOfPinnedObject(), (Int32*)pSize, (UInt32)count);
                }
                finally
                {
                    pAddress_ptr.Free();
                }
            }
        }

        [System.CLSCompliant(false)]
        public static 
        Boolean wglAssociateImageBufferEventsI3D(IntPtr hDC, IntPtr pEvent, [In, Out] object pAddress, IntPtr pSize, Int32 count)
        {
            unsafe
            {
                System.Runtime.InteropServices.GCHandle pAddress_ptr = System.Runtime.InteropServices.GCHandle.Alloc(pAddress, System.Runtime.InteropServices.GCHandleType.Pinned);
                try
                {
                    return Delegates.wglAssociateImageBufferEventsI3D((IntPtr)hDC, (IntPtr*)pEvent, (IntPtr)pAddress_ptr.AddrOfPinnedObject(), (Int32*)pSize, (UInt32)count);
                }
                finally
                {
                    pAddress_ptr.Free();
                }
            }
        }

        [System.CLSCompliant(false)]
        public static 
        Boolean wglAssociateImageBufferEventsI3D(IntPtr hDC, IntPtr pEvent, [In, Out] object pAddress, Int32[] pSize, UInt32 count)
        {
            unsafe
            {
                fixed (Int32* pSize_ptr = pSize)
                {
                    System.Runtime.InteropServices.GCHandle pAddress_ptr = System.Runtime.InteropServices.GCHandle.Alloc(pAddress, System.Runtime.InteropServices.GCHandleType.Pinned);
                    try
                    {
                        return Delegates.wglAssociateImageBufferEventsI3D((IntPtr)hDC, (IntPtr*)pEvent, (IntPtr)pAddress_ptr.AddrOfPinnedObject(), (Int32*)pSize_ptr, (UInt32)count);
                    }
                    finally
                    {
                        pAddress_ptr.Free();
                    }
                }
            }
        }

        [System.CLSCompliant(false)]
        public static 
        Boolean wglAssociateImageBufferEventsI3D(IntPtr hDC, IntPtr pEvent, [In, Out] object pAddress, Int32[] pSize, Int32 count)
        {
            unsafe
            {
                fixed (Int32* pSize_ptr = pSize)
                {
                    System.Runtime.InteropServices.GCHandle pAddress_ptr = System.Runtime.InteropServices.GCHandle.Alloc(pAddress, System.Runtime.InteropServices.GCHandleType.Pinned);
                    try
                    {
                        return Delegates.wglAssociateImageBufferEventsI3D((IntPtr)hDC, (IntPtr*)pEvent, (IntPtr)pAddress_ptr.AddrOfPinnedObject(), (Int32*)pSize_ptr, (UInt32)count);
                    }
                    finally
                    {
                        pAddress_ptr.Free();
                    }
                }
            }
        }

        [System.CLSCompliant(false)]
        public static 
        Boolean wglAssociateImageBufferEventsI3D(IntPtr hDC, IntPtr pEvent, [In, Out] object pAddress, ref Int32 pSize, UInt32 count)
        {
            unsafe
            {
                fixed (Int32* pSize_ptr = &pSize)
                {
                    System.Runtime.InteropServices.GCHandle pAddress_ptr = System.Runtime.InteropServices.GCHandle.Alloc(pAddress, System.Runtime.InteropServices.GCHandleType.Pinned);
                    try
                    {
                        return Delegates.wglAssociateImageBufferEventsI3D((IntPtr)hDC, (IntPtr*)pEvent, (IntPtr)pAddress_ptr.AddrOfPinnedObject(), (Int32*)pSize_ptr, (UInt32)count);
                    }
                    finally
                    {
                        pAddress_ptr.Free();
                    }
                }
            }
        }

        [System.CLSCompliant(false)]
        public static 
        Boolean wglAssociateImageBufferEventsI3D(IntPtr hDC, IntPtr pEvent, [In, Out] object pAddress, ref Int32 pSize, Int32 count)
        {
            unsafe
            {
                fixed (Int32* pSize_ptr = &pSize)
                {
                    System.Runtime.InteropServices.GCHandle pAddress_ptr = System.Runtime.InteropServices.GCHandle.Alloc(pAddress, System.Runtime.InteropServices.GCHandleType.Pinned);
                    try
                    {
                        return Delegates.wglAssociateImageBufferEventsI3D((IntPtr)hDC, (IntPtr*)pEvent, (IntPtr)pAddress_ptr.AddrOfPinnedObject(), (Int32*)pSize_ptr, (UInt32)count);
                    }
                    finally
                    {
                        pAddress_ptr.Free();
                    }
                }
            }
        }

        [System.CLSCompliant(false)]
        public static 
        Boolean wglReleaseImageBufferEventsI3D(IntPtr hDC, IntPtr pAddress, UInt32 count)
        {
            unsafe
            {
                return Delegates.wglReleaseImageBufferEventsI3D((IntPtr)hDC, (IntPtr)pAddress, (UInt32)count);
            }
        }

        public static 
        Boolean wglReleaseImageBufferEventsI3D(IntPtr hDC, IntPtr pAddress, Int32 count)
        {
            unsafe
            {
                return Delegates.wglReleaseImageBufferEventsI3D((IntPtr)hDC, (IntPtr)pAddress, (UInt32)count);
            }
        }

        [System.CLSCompliant(false)]
        public static 
        Boolean wglReleaseImageBufferEventsI3D(IntPtr hDC, [In, Out] object pAddress, UInt32 count)
        {
            unsafe
            {
                System.Runtime.InteropServices.GCHandle pAddress_ptr = System.Runtime.InteropServices.GCHandle.Alloc(pAddress, System.Runtime.InteropServices.GCHandleType.Pinned);
                try
                {
                    return Delegates.wglReleaseImageBufferEventsI3D((IntPtr)hDC, (IntPtr)pAddress_ptr.AddrOfPinnedObject(), (UInt32)count);
                }
                finally
                {
                    pAddress_ptr.Free();
                }
            }
        }

        public static 
        Boolean wglReleaseImageBufferEventsI3D(IntPtr hDC, [In, Out] object pAddress, Int32 count)
        {
            unsafe
            {
                System.Runtime.InteropServices.GCHandle pAddress_ptr = System.Runtime.InteropServices.GCHandle.Alloc(pAddress, System.Runtime.InteropServices.GCHandleType.Pinned);
                try
                {
                    return Delegates.wglReleaseImageBufferEventsI3D((IntPtr)hDC, (IntPtr)pAddress_ptr.AddrOfPinnedObject(), (UInt32)count);
                }
                finally
                {
                    pAddress_ptr.Free();
                }
            }
        }

        public static 
        Boolean wglEnableFrameLockI3D()
        {
            return Delegates.wglEnableFrameLockI3D();
        }

        public static 
        Boolean wglDisableFrameLockI3D()
        {
            return Delegates.wglDisableFrameLockI3D();
        }

        public static 
        Boolean wglIsEnabledFrameLockI3D([Out] Boolean[] pFlag)
        {
            unsafe
            {
                fixed (Boolean* pFlag_ptr = pFlag)
                {
                    return Delegates.wglIsEnabledFrameLockI3D((Boolean*)pFlag_ptr);
                }
            }
        }

        public static 
        Boolean wglIsEnabledFrameLockI3D([Out] out Boolean pFlag)
        {
            unsafe
            {
                fixed (Boolean* pFlag_ptr = &pFlag)
                {
                    Boolean retval = Delegates.wglIsEnabledFrameLockI3D((Boolean*)pFlag_ptr);
                    pFlag = *pFlag_ptr;
                    return retval;
                }
            }
        }

        [System.CLSCompliant(false)]
        public static 
        Boolean wglIsEnabledFrameLockI3D([Out] IntPtr pFlag)
        {
            unsafe
            {
                return Delegates.wglIsEnabledFrameLockI3D((Boolean*)pFlag);
            }
        }

        public static 
        Boolean wglQueryFrameLockMasterI3D([Out] Boolean[] pFlag)
        {
            unsafe
            {
                fixed (Boolean* pFlag_ptr = pFlag)
                {
                    return Delegates.wglQueryFrameLockMasterI3D((Boolean*)pFlag_ptr);
                }
            }
        }

        public static 
        Boolean wglQueryFrameLockMasterI3D([Out] out Boolean pFlag)
        {
            unsafe
            {
                fixed (Boolean* pFlag_ptr = &pFlag)
                {
                    Boolean retval = Delegates.wglQueryFrameLockMasterI3D((Boolean*)pFlag_ptr);
                    pFlag = *pFlag_ptr;
                    return retval;
                }
            }
        }

        [System.CLSCompliant(false)]
        public static 
        Boolean wglQueryFrameLockMasterI3D([Out] IntPtr pFlag)
        {
            unsafe
            {
                return Delegates.wglQueryFrameLockMasterI3D((Boolean*)pFlag);
            }
        }

        public static 
        Boolean wglGetFrameUsageI3D([Out] float[] pUsage)
        {
            unsafe
            {
                fixed (float* pUsage_ptr = pUsage)
                {
                    return Delegates.wglGetFrameUsageI3D((float*)pUsage_ptr);
                }
            }
        }

        public static 
        Boolean wglGetFrameUsageI3D([Out] out float pUsage)
        {
            unsafe
            {
                fixed (float* pUsage_ptr = &pUsage)
                {
                    Boolean retval = Delegates.wglGetFrameUsageI3D((float*)pUsage_ptr);
                    pUsage = *pUsage_ptr;
                    return retval;
                }
            }
        }

        [System.CLSCompliant(false)]
        public static 
        Boolean wglGetFrameUsageI3D([Out] IntPtr pUsage)
        {
            unsafe
            {
                return Delegates.wglGetFrameUsageI3D((float*)pUsage);
            }
        }

        public static 
        Boolean wglBeginFrameTrackingI3D()
        {
            return Delegates.wglBeginFrameTrackingI3D();
        }

        public static 
        Boolean wglEndFrameTrackingI3D()
        {
            return Delegates.wglEndFrameTrackingI3D();
        }

        public static 
        Boolean wglQueryFrameTrackingI3D([Out] Int32[] pFrameCount, [Out] Int32[] pMissedFrames, [Out] float[] pLastMissedUsage)
        {
            unsafe
            {
                fixed (Int32* pFrameCount_ptr = pFrameCount)
                fixed (Int32* pMissedFrames_ptr = pMissedFrames)
                fixed (float* pLastMissedUsage_ptr = pLastMissedUsage)
                {
                    return Delegates.wglQueryFrameTrackingI3D((Int32*)pFrameCount_ptr, (Int32*)pMissedFrames_ptr, (float*)pLastMissedUsage_ptr);
                }
            }
        }

        public static 
        Boolean wglQueryFrameTrackingI3D([Out] out Int32 pFrameCount, [Out] out Int32 pMissedFrames, [Out] out float pLastMissedUsage)
        {
            unsafe
            {
                fixed (Int32* pFrameCount_ptr = &pFrameCount)
                fixed (Int32* pMissedFrames_ptr = &pMissedFrames)
                fixed (float* pLastMissedUsage_ptr = &pLastMissedUsage)
                {
                    Boolean retval = Delegates.wglQueryFrameTrackingI3D((Int32*)pFrameCount_ptr, (Int32*)pMissedFrames_ptr, (float*)pLastMissedUsage_ptr);
                    pFrameCount = *pFrameCount_ptr;
                    pMissedFrames = *pMissedFrames_ptr;
                    pLastMissedUsage = *pLastMissedUsage_ptr;
                    return retval;
                }
            }
        }

        [System.CLSCompliant(false)]
        public static 
        Boolean wglQueryFrameTrackingI3D([Out] IntPtr pFrameCount, [Out] IntPtr pMissedFrames, [Out] IntPtr pLastMissedUsage)
        {
            unsafe
            {
                return Delegates.wglQueryFrameTrackingI3D((Int32*)pFrameCount, (Int32*)pMissedFrames, (float*)pLastMissedUsage);
            }
        }

    }
}
