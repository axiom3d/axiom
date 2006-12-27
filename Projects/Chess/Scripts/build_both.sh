#!/bin/sh

# Build both debug and release versions of Axiom3D
mono ../../../BuildSupport/nAnt/bin/NAnt.exe -buildfile:../Chess.build debug build.all

mono ../../../BuildSupport/nAnt/bin/NAnt.exe -buildfile:../Chess.build release build.all
