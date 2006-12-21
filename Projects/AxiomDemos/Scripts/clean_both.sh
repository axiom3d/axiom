#!/bin/sh

# Build both debug and release versions of Axiom3D
mono ../../../BuildSupport/nAnt/bin/NAnt.exe -buildfile:../Axiom.build debug build.clean

mono ../../../BuildSupport/nAnt/bin/NAnt.exe -buildfile:../Axiom.build release build.clean
