
PROGRAM = SdlExamples
PACKAGES =
BINARY_LIBS = \
        Tao.Sdl.dll
SYSTEM_LIBS = \
        System \
        System.Windows.Forms \
        System.Data \
        System.Drawing
RESX = SdlExamples.resx
RESOURCES = $(PROGRAM).SdlExamples.resources
SOURCES = \
        SdlExamples.cs \
        GfxPrimitives.cs \
        Program.cs \
        Rectangles.cs \
        SdlExamples.Designer.cs \
        SmpegPlayer.cs \
        Properties/AssemblyInfo.cs
KEYFILE = ../../src/Tao.Sdl/Tao.Sdl.snk

$(PROGRAM).SdlExamples.resources: SdlExamples.resx
	$(RESGEN) $< $@

include $(top_srcdir)/Program.am

