#!/bin/sh

# Performs a release build
mono ../../../../../BuildSupport/nAnt/bin/NAnt.exe -buildfile:../YAT.build -D:project.release.type=release release build.yat
