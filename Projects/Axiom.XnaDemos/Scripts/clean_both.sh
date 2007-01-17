#!/bin/sh

# Build both debug and release versions of Axiom3D
mono ../../../BuildSupport/nAnt/bin/NAnt.exe -buildfile:../Axiom.XnaDemos.build debug clean.all

mono ../../../BuildSupport/nAnt/bin/NAnt.exe -buildfile:../Axiom.XnaDemos.build release clean.all
