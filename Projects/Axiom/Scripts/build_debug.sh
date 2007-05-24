#!/bin/sh

mono ../../../BuildSupport/nAnt/bin/NAnt.exe -buildfile:../Axiom.build debug build.axiom -l:../Axiom.build.debug.log
