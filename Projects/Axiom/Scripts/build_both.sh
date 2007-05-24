#!/bin/sh

# Build both debug and release versions of Axiom3D
mono ../../../BuildSupport/nAnt/bin/NAnt.exe -buildfile:../Axiom.build debug build.axiom -l:../Axiom.build.debug.log

mono ../../../BuildSupport/nAnt/bin/NAnt.exe -buildfile:../Axiom.build release build.axiom -l:../Axiom.build.release.log
