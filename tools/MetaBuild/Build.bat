@if defined ECHO (echo %ECHO%) else (echo off)
REM
REM Runs the build.
REM
"%~dp0bin\MetaBuild.bat" /rootdir "%~dp0" /metabuildconfig "%~dp0bin\MetaBuild.config.custom" %*
