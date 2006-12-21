#!/bin/sh

# Performs a release build and prepares a zip file for distribution
mono ../../../../../BuildSupport/nAnt/bin/NAnt.exe  -buildfile:../YAT.build -D:project.release.type=release release dist 
