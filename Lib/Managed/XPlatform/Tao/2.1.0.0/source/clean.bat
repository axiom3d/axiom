@REM Builds the Tao Framework using NAnt 

@ECHO OFF 

rmdir /s /q dist

@REM Build Solutions Using NAnt 
NAnt.exe -t:net-2.0 -buildfile:tao.build clean
