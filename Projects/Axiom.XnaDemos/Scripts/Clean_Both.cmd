REM Build both debug and release versions of Axiom3D
..\..\..\BuildSupport\NAnt\bin\NAnt -buildfile:..\Axiom.XnaDemos.build debug clean.all

..\..\..\BuildSupport\NAnt\bin\NAnt -buildfile:..\Axiom.XnaDemos.build release clean.all
