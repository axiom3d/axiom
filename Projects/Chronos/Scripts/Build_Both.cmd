REM Build both debug and release versions of Axiom3D
..\..\..\BuildSupport\NAnt\bin\NAnt -buildfile:..\YAT.build debug build.all

..\..\..\BuildSupport\NAnt\bin\NAnt -buildfile:..\YAT.build release build.all 
