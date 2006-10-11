#!/bin/sh

# Build both debug and release versions of Axiom3D
mono ../BuildSupport/nAnt/bin/NAnt.exe -buildfile:../Source/Axiom3D/Axiom3D.build debug build.clean

mono ../BuildSupport/nAnt/bin/NAnt.exe -buildfile:../Source/Axiom3D/Axiom3D.build release build.clean
