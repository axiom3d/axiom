#!/bin/sh

# Performs a nightly build in release mode and prepares a zip file for distribution
mono ../../../../../BuildSupport/nAnt/bin/NAnt.exe -buildfile:../YAT.build -D:project.release.type=nightly release dist 
