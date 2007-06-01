#!/bin/sh

# Clean both debug and release versions of Axiom3D
mono ../../../BuildSupport/nAnt/bin/NAnt.exe -buildfile:../Axiom.Demos.build build.cleanall -l:..\AxiomDemos.build.release.log
