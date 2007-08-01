#!/bin/sh

mono ../../../BuildSupport/nAnt/bin/NAnt.exe -buildfile:../Axiom.build release build.axiom -logfile:../axiom.build.release.log
