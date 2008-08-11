
PROGRAM = OpenAlExamples
PACKAGES =
BINARY_LIBS = \
        Tao.OpenAl.dll \
        Tao.FreeGlut.dll \
        Tao.OpenGl.dll
SYSTEM_LIBS = \
        System \
        System.Windows.Forms \
        System.Data \
        System.Drawing
RESX = OpenAlExamples.resx
RESOURCES = $(PROGRAM).OpenAlExamples.resources
SOURCES = \
        OpenAlExamples.cs \
        Boxes.cs \
        Lesson01.cs \
        Lesson02.cs \
        Lesson03.cs \
        Lesson05.cs \
        OpenAlExamples.Designer.cs \
        Program.cs \
        Sound.cs \
        Waterfall.cs \
        Properties/AssemblyInfo.cs
KEYFILE = ../../src/Tao.OpenAl/Tao.OpenAl.snk

$(PROGRAM).OpenAlExamples.resources: OpenAlExamples.resx
	$(RESGEN) $< $@

include $(top_srcdir)/Program.am

