
SUBDIRS = Tao.DevIl \
          Tao.FFmpeg \
          Tao.FreeType \
          Tao.GlBindGen \
          Tao.Lua \
          Tao.Ode \
          Tao.OpenAl \
          Tao.OpenGl \
          Tao.Platform.Windows \
          Tao.Platform.X11 \
          Tao.Cg \
          Tao.FreeGlut \
          Tao.Glfw \
          Tao.PhysFs \
          Tao.Sdl

monodocdir = $(prefix)/lib/monodoc/sources/
monodoc_DATA = Tao.source Tao.zip Tao.tree

EXTRA_DIST = Tao.source
CLEANFILES = Tao.zip Tao.tree

# FIXME: doc generation is disabled for Tao.OpenGl because of a gmcs bug
# (see also Assembly.am)
$(monodoc_DATA):
	for x in $(filter-out Tao.OpenGl Tao.GlBindGen, $(SUBDIRS)); do \
	  monodocer --assembly:$$x/$$x.dll --path:doc/$$x; \
        done
	mdassembler --out Tao \
	  $(addprefix --ecma doc/, $(filter-out Tao.OpenGl Tao.GlBindGen, $(SUBDIRS)))

clean-local:
	rm -Rf doc
