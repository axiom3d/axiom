#!/bin/sh

# Build both debug and release versions of Axiom3D
mono ../../../BuildSupport/nAnt/bin/NAnt.exe -buildfile:../YAT.build debug build.all

mono ../../../BuildSupport/nAnt/bin/NAnt.exe -buildfile:../YAT.build release build.all
