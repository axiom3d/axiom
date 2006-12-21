#!/bin/sh

# Performs a nightly build in debug mode and prepares a zip file for distribution
mono ../../../../../BuildSupport/nAnt/bin/NAnt.exe -buildfile:../YAT.build -D:project.release.type=nightly debug dist 
