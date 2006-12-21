REM Build both debug and release versions of Axiom3D
..\BuildSupport\NAnt\bin\NAnt -buildfile:..\Source\Axiom3D\Axiom3D.build debug build.axiom3d 

..\BuildSupport\NAnt\bin\NAnt -buildfile:..\Source\Axiom3D\Axiom3D.build release build.axiom3d 
