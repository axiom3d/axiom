#!/bin/sh

# Performs a debug build and prepares a zip file for distribution
mono ../../../../../BuildSupport/nAnt/bin/NAnt.exe  -buildfile:../YAT.build -D:project.release.type=debug debug dist 
