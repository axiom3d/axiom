#!/bin/sh

# Build both debug and release versions of Axiom3D
mono ../../../BuildSupport/nAnt/bin/NAnt.exe -buildfile:../Axiom.build debug build.axiom -logfile:../axiom.build.debug.log

mono ../../../BuildSupport/nAnt/bin/NAnt.exe -buildfile:../Axiom.build release build.axiom -logfile:../axiom.build.release.log
