
PROGRAM = FFmpegExamples
PACKAGES =
BINARY_LIBS = Tao.FFmpeg.dll
SYSTEM_LIBS = \
        System \
        System.Windows.Forms \
        System.Data \
        System.Drawing
RESX = \
        Player.resx \
        Properties/Resources.resx
RESOURCES = \
        $(PROGRAM).Player.resources \
        $(PROGRAM).Resources.resources
SOURCES = \
        Player.cs \
        AudioStream.cs \
        Decoder.cs \
        Player.Designer.cs \
        Program.cs \
        Properties/Resources.Designer.cs \
        Properties/AssemblyInfo.cs

$(PROGRAM).Player.resources: Player.resx
	$(RESGEN) $< $@
$(PROGRAM).Resources.resources: Properties/Resources.resx
	$(RESGEN) $< $@

include $(top_srcdir)/Program.am

