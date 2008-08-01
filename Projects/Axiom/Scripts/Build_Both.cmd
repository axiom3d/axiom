REM Build both debug and release versions of Axiom3D
..\..\..\BuildSupport\NAnt\bin\NAnt -buildfile:..\Axiom.build debug build.axiom -logfile:..\axiom.build.debug.log

..\..\..\BuildSupport\NAnt\bin\NAnt -buildfile:..\Axiom.build release build.axiom -logfile:..\axiom.build.release.log