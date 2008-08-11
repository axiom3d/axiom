
PROGRAM = CgExamples
PACKAGES =
BINARY_LIBS = \
        Tao.Cg.dll \
        Tao.FreeGlut.dll \
        Tao.OpenGl.dll
SYSTEM_LIBS = \
        System \
        System.Windows.Forms \
        System.Data \
        System.Drawing
RESX = CgExamples.resx
RESOURCES = $(PROGRAM).CgExamples.resources
SOURCES = \
        CgExamples.cs \
        CgExamples.Designer.cs \
        Gl_01_vertex_program.cs \
        Gl_02_vertex_and_fragment_program.cs \
        Gl_06_vertex_twisting_program.cs \
        Gl_14_bulge_program.cs \
        Program.cs \
        Properties/AssemblyInfo.cs
KEYFILE = ../../src/Tao.Cg/Tao.Cg.snk

$(PROGRAM).CgExamples.resources: CgExamples.resx
	$(RESGEN) $< $@

include $(top_srcdir)/Program.am

