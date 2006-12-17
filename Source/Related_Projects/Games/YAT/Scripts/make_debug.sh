#!/bin/sh

# Performs a debug build
mono ../../../../../BuildSupport/nAnt/bin/NAnt.exe -buildfile:../YAT.build -D:project.release.type=debug debug build.yat
