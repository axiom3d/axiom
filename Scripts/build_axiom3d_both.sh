#!/bin/sh

# Build both debug and release versions of Axiom3D
mono ../BuildSupport/nAnt/bin/NAnt.exe -buildfile:../Source/Axiom3D/Axiom3D.build debug build.axiom3d

mono ../BuildSupport/nAnt/bin/NAnt.exe -buildfile:../Source/Axiom3D/Axiom3D.build release build.axiom3d
