REM Build both debug and release versions of Axiom3D
..\..\..\BuildSupport\NAnt\bin\NAnt -buildfile:..\Axiom.Demos.build debug build.all -l:..\AxiomDemos.build.debug.log

..\..\..\BuildSupport\NAnt\bin\NAnt -buildfile:..\Axiom.Demos.build release build.all -l:..\AxiomDemos.build.release.log
