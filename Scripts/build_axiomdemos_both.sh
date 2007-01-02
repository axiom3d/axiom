#!/bin/sh

mono ../BuildSupport/nAnt/bin/NAnt.exe -buildfile:../Source/AxiomDemos/Axiom.Demos.build debug build.axiom.demos 
mono ../BuildSupport/nAnt/bin/NAnt.exe -buildfile:../Source/AxiomDemos/Axiom.Demos.build release build.axiom.demos 
