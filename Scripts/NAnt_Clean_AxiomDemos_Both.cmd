REM Build both debug and release versions of Axiom3D
..\BuildSupport\NAnt\bin\NAnt -buildfile:..\Source\AxiomDemos\Axiom.Demos.build debug build.clean

..\BuildSupport\NAnt\bin\NAnt -buildfile:..\Source\AxiomDemos\Axiom.Demos.build release build.clean
