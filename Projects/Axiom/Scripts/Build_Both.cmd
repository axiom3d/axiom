REM Build both debug and release versions of Axiom3D
..\..\..\BuildSupport\NAnt\bin\NAnt -buildfile:..\Axiom.build -D:project.config=debug -l:..\Axiom.build.debug.log
..\..\..\BuildSupport\NAnt\bin\NAnt -buildfile:..\Axiom.build -D:project.config=release -l:..\Axiom.build.release.log
