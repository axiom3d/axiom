REM Build both debug and release versions of Axiom3D
..\..\..\BuildSupport\NAnt\bin\NAnt -buildfile:..\Chess.build debug build.all

..\..\..\BuildSupport\NAnt\bin\NAnt -buildfile:..\Chess.build release build.all 
