#!/bin/sh

# Clean both debug and release versions of Axiom3D
mono ../../../BuildSupport/nAnt/bin/NAnt.exe -buildfile:../Chess.build build.cleanall
