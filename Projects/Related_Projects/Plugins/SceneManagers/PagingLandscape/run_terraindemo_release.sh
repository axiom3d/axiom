#!/bin/sh

# Performs a release build and prepares a zip file for distribution
mono ../../../../../BuildSupport/nAnt/bin/NAnt.exe -D:project.release.type=release run.terraindemo
