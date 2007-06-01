#!/bin/sh

# Build both debug and release versions of Axiom3D
mono ../../../BuildSupport/nAnt/bin/NAnt.exe -buildfile:../Axiom.Demos.build debug build.all -l:..\AxiomDemos.build.debug.log

mono ../../../BuildSupport/nAnt/bin/NAnt.exe -buildfile:../Axiom.Demos.build release build.all -l:..\AxiomDemos.build.release.log
