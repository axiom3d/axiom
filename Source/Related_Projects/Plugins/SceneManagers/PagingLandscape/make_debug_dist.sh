#!/bin/sh

# Performs a debug build and prepares a zip file for distribution
mono ../../../../../BuildSupport/nAnt/bin/NAnt.exe -D:project.release.type=debug debug dist 
