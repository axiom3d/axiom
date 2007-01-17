REM Build both debug and release versions of Axiom3D
..\..\..\BuildSupport\NAnt\bin\NAnt -buildfile:..\Axiom.build debug build.axiom 

..\..\..\BuildSupport\NAnt\bin\NAnt -buildfile:..\Axiom.build release build.axiom 
