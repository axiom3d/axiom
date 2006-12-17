REM Build both debug and release versions of Axiom3D
..\BuildSupport\NAnt\bin\NAnt -buildfile:..\Source\Axiom3D\Axiom3D.build debug build.clean

..\BuildSupport\NAnt\bin\NAnt -buildfile:..\Source\Axiom3D\Axiom3D.build release build.clean
