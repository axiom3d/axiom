
PROGRAM = NateRobins
PACKAGES =
BINARY_LIBS = \
        Tao.FreeGlut.dll \
        Tao.OpenGl.dll
SYSTEM_LIBS = \
        System \
        System.Windows.Forms \
        System.Data \
        System.Drawing
RESX = NateRobins.resx
RESOURCES = $(PROGRAM).NateRobins.resources
SOURCES = \
        NateRobins.cs \
        Area.cs \
        Maiden.cs \
        MultiView.cs \
        NateRobins.Designer.cs \
        Nii.cs \
        Node.cs \
        Null.cs \
        Point.cs \
        Program.cs \
        Qix.cs \
        Rotate.cs \
        Starfield.cs \
        Strip.cs \
        Texture.cs \
        Voronoi.cs \
        Properties/AssemblyInfo.cs
KEYFILE = ../../src/Tao.FreeGlut/Tao.FreeGlut.snk

$(PROGRAM).NateRobins.resources: NateRobins.resx
	$(RESGEN) $< $@

include $(top_srcdir)/Program.am

