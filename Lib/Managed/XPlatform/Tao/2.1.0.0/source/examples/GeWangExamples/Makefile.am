
PROGRAM = GeWangExamples
PACKAGES =
BINARY_LIBS = \
        Tao.FreeGlut.dll \
        Tao.OpenGl.dll
SYSTEM_LIBS = \
        System \
        System.Windows.Forms \
        System.Data \
        System.Drawing
RESX = GeWangExamples.resx
RESOURCES = $(PROGRAM).GeWang.resources
SOURCES = \
        ClippingPlanes.cs \
        GeWangExamples.cs \
        GeWangExamples.Designer.cs \
        Lorenz3d.cs \
        Mirror.cs \
        Program.cs \
        Shadow.cs \
        Starfield.cs \
        Xform.cs \
        Properties/AssemblyInfo.cs
KEYFILE = ../../src/Tao.FreeGlut/Tao.FreeGlut.snk

$(PROGRAM).GeWang.resources: GeWangExamples.resx
	$(RESGEN) $< $@

include $(top_srcdir)/Program.am

