
PROGRAM = GlfwExamples
PACKAGES =
BINARY_LIBS = \
        Tao.Glfw.dll \
        Tao.OpenGl.dll
SYSTEM_LIBS = \
        System \
        System.Windows.Forms \
        System.Data \
        System.Drawing
RESX = GlfwExamples.resx
RESOURCES = $(PROGRAM).GlfwExamples.resources
SOURCES = \
        Boing.cs \
        Cube.cs \
        Gears.cs \
        GlfwExamples.cs \
        GlfwExamples.Designer.cs \
        GlslLesson01.cs \
        KeyTest.cs \
        ListModes.cs \
        Pong.cs \
        Program.cs \
        Triangle.cs \
        Properties/AssemblyInfo.cs
KEYFILE = ../../src/Tao.Glfw/Tao.Glfw.snk

$(PROGRAM).GlfwExamples.resources: GlfwExamples.resx
	$(RESGEN) $< $@

include $(top_srcdir)/Program.am

