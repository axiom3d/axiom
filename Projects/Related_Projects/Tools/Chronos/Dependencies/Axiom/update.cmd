@echo off

set AXDIR=\Ogre\Axiom\DemoTest\bin
set DEPDIR=\Ogre\Editor\Dependencies\Axiom
set BINDIR=\Ogre\Editor\WorldEditor\bin\Debug

cd /d %BINDIR%
del Axiom.*.dll

cd /d %AXDIR%

copy Axiom.*.dll %DEPDIR%

cd /d %DEPDIR%

set AXDIR=
set DEPDIR=
set BINDIR=