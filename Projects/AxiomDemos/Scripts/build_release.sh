#!/bin/sh

mono ../../../BuildSupport/nAnt/bin/NAnt.exe -buildfile:../Axiom.Demos.build release build.all -l:..\AxiomDemos.build.release.log
