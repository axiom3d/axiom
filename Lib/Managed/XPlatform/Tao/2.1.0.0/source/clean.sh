#!/bin/sh
# Builds the Tao Framework using both Prebuild and NAnt 

rm -rf dist

# Build Solutions Using NAnt 
nant -t:mono-2.0 -buildfile:tao.build clean
