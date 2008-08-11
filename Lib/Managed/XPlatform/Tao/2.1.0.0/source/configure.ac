AC_INIT(taoframework, 2.0.0.svn20080222)
AC_CONFIG_AUX_DIR(.auto)
AM_INIT_AUTOMAKE([no-define tar-ustar])
AM_MAINTAINER_MODE

AC_SUBST(VERSION)

# Find version information for all projects
ACX_ASSEMBLY_VERSION(Tao.Cg, TAOCG_MAJOR, TAOCG_VERSION)
ACX_ASSEMBLY_VERSION(Tao.DevIl, TAODEVIL_MAJOR, TAODEVIL_VERSION)
ACX_ASSEMBLY_VERSION(Tao.FFmpeg, TAOFFMPEG_MAJOR, TAOFFMPEG_VERSION)
ACX_ASSEMBLY_VERSION(Tao.FreeGlut, TAOFREEGLUT_MAJOR, TAOFREEGLUT_VERSION)
ACX_ASSEMBLY_VERSION(Tao.FreeType, TAOFREETYPE_MAJOR, TAOFREETYPE_VERSION)
ACX_ASSEMBLY_VERSION(Tao.Glfw, TAOGLFW_MAJOR, TAOGLFW_VERSION)
ACX_ASSEMBLY_VERSION(Tao.Lua, TAOLUA_MAJOR, TAOLUA_VERSION)
ACX_ASSEMBLY_VERSION(Tao.Ode, TAOODE_MAJOR, TAOODE_VERSION)
ACX_ASSEMBLY_VERSION(Tao.OpenAl, TAOOPENAL_MAJOR, TAOOPENAL_VERSION)
ACX_ASSEMBLY_VERSION(Tao.OpenGl, TAOOPENGL_MAJOR, TAOOPENGL_VERSION)
ACX_ASSEMBLY_VERSION(Tao.PhysFs, TAOPHYSFS_MAJOR, TAOPHYSFS_VERSION)
ACX_ASSEMBLY_VERSION(Tao.Platform.Windows, TAOWINDOWS_MAJOR, TAOWINDOWS_VERSION)
ACX_ASSEMBLY_VERSION(Tao.Platform.X11, TAOX11_MAJOR, TAOX11_VERSION)
ACX_ASSEMBLY_VERSION(Tao.Sdl, TAOSDL_MAJOR, TAOSDL_VERSION)

MONO_REQUIRED_VERSION=1.1
PKG_CHECK_MODULES(MONO_DEPENDENCY, mono >= $MONO_REQUIRED_VERSION,
                  has_mono=true, has_mono=false)
if test "x$has_mono" = "xtrue"; then
  AC_PATH_PROG(RUNTIME, mono, no)
  AC_PATH_PROG(CSC, gmcs, no)
  if test `uname -s` = "Darwin"; then
        LIB_PREFIX=
        LIB_SUFFIX=.dylib
  else
        LIB_PREFIX=.so
        LIB_SUFFIX=
  fi
else
  AC_PATH_PROG(CSC, csc.exe, no)
  if test x$CSC = "xno"; then
        AC_MSG_ERROR([You need to install either mono or .Net])
  else
    RUNTIME=
    LIB_PREFIX=
    LIB_SUFFIX=
  fi
fi

AC_SUBST(LIB_PREFIX)
AC_SUBST(LIB_SUFFIX)
AC_SUBST(RUNTIME)
AC_SUBST(CSC)

AC_PATH_PROG(GACUTIL, gacutil)
if test "x$GACUTIL" = "xno" ; then
  AC_MSG_ERROR([No gacutil tool found])
fi
AC_SUBST(GACUTIL)

AC_PATH_PROG(RESGEN, resgen)
if test "x$RESGEN" = "xno" ; then
  AC_MSG_ERROR([No resgen tool found])
fi
AC_SUBST(RESGEN)

MONODOC_REQUIRED_VERSION=1.0
AC_SUBST(MONODOC_REQUIRED_VERSION)
PKG_CHECK_MODULES(MONODOC_DEPENDENCY, monodoc >= $MONODOC_REQUIRED_VERSION,
                  enable_monodoc=yes, enable_monodoc=no)
if test "x$enable_monodoc" = "xyes"; then
  AC_PATH_PROG(MONODOC, monodoc, no)
  if test x$MONODOC = xno; then
    enable_monodoc=no
  fi
else
  MONODOC=
fi
AC_SUBST(MONODOC)
AM_CONDITIONAL(ENABLE_MONODOC, test "x$enable_monodoc" = "xyes")

winbuild=no
case "$host" in
       *-*-mingw*|*-*-cygwin*)
               winbuild=yes
               ;;
esac
AM_CONDITIONAL(WINBUILD, test x$winbuild = xyes)

dnl
dnl TODO: put .pc files in there, too
dnl

AC_CONFIG_FILES(
  Makefile
  m4/Makefile
)
AC_CONFIG_FILES(
  src/Makefile
  src/Tao.Cg/Makefile
  src/Tao.Cg/tao-cg.pc
  src/Tao.DevIl/Makefile
  src/Tao.DevIl/tao-devil.pc
  src/Tao.FFmpeg/Makefile
  src/Tao.FFmpeg/tao-ffmpeg.pc
  src/Tao.FreeGlut/Makefile
  src/Tao.FreeGlut/tao-freeglut.pc
  src/Tao.FreeType/Makefile
  src/Tao.FreeType/tao-freetype.pc
  src/Tao.GlBindGen/Makefile
  src/Tao.Glfw/Makefile
  src/Tao.Glfw/tao-glfw.pc
  src/Tao.Lua/Makefile
  src/Tao.Lua/tao-lua.pc
  src/Tao.Ode/Makefile
  src/Tao.Ode/tao-ode.pc
  src/Tao.OpenAl/Makefile
  src/Tao.OpenAl/tao-openal.pc
  src/Tao.OpenGl/Makefile
  src/Tao.OpenGl/tao-opengl.pc
  src/Tao.PhysFs/Makefile
  src/Tao.PhysFs/tao-physfs.pc
  src/Tao.Platform.Windows/Makefile
  src/Tao.Platform.Windows/tao-platform-windows.pc
  src/Tao.Platform.X11/Makefile
  src/Tao.Platform.X11/tao-platform-x11.pc
  src/Tao.Sdl/Makefile
  src/Tao.Sdl/tao-sdl.pc
)
AC_CONFIG_FILES(
  tests/Makefile
  tests/Ode/Makefile
  tests/Ode/tao-ode-tests.pc
  tests/Sdl/Makefile
  tests/Sdl/tao-sdl-tests.pc
)
AC_CONFIG_FILES(
  examples/Makefile
  examples/CgExamples/Makefile
  examples/DevIlExamples/Makefile
  examples/FFmpegExamples/Makefile
  examples/FreeGlutExamples/Makefile
  examples/GeWangExamples/Makefile
  examples/GlfwExamples/Makefile
  examples/LuaFunctions/Makefile
  examples/LuaSimple/Makefile
  examples/NateRobins/Makefile
  examples/NeHe/Makefile
  examples/OdeExamples/Makefile
  examples/OpenAlExamples/Makefile
  examples/PhysFsExamples/Makefile
  examples/Redbook/Makefile
  examples/SdlExamples/Makefile
)

AC_OUTPUT

echo "==="
echo "Project configuration summary"
echo "  * installation prefix: $prefix"
echo "  * compiler:            $CSC"
echo "  * documentation:       $enable_monodoc ($MONODOC)"
echo "==="

