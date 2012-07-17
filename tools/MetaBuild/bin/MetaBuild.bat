@if defined ECHO (echo %ECHO%) else (echo off)
REM
REM Builds the specified module or all modules within the current directory
REM if none specified.  Also provides a variety of other options.
REM
REM Use /? for short form help.
REM Refer to MetaBuild.txt for documentation.
REM
REM Implementor's note:
REM   This code is sensitive to whitespace and characters
REM   such as parentheses that may appear in arguments representing filenames.
REM
REM   So there are a few tricks, such as using the ~1 syntax to get unquoted
REM   arguments.  Read the documentation for the DOS FOR command for more
REM   details on these expansions.
REM
REM   We also place the & immediately at the end of the set statement allows
REM   commands to be chained while ensuring that no extraneous whitespace
REM   gets inserted.  If wrote "set FOO=1 & goto :BAR" then FOO would
REM   actually include a trailing space!
REM
REM   The very fact I have to write comments like these suggests that this
REM   script is too complex and should be rewritten as a standalone app to
REM   avoid these issues.
REM   -- Jeff.
REM
setlocal

set SCRIPT_DIR=%~dp0
set SCRIPT_NAME=%~nx0
set METABUILD_OPTIONS=%*

REM Default options.
set INITIAL_MODULES=
set CLEAN=
set BUILD=
set IMAGE=
set TEST=
set DIST=
set CALL_TARGETS=
set PARALLEL=1
set VERBOSITY=minimal
set MSBUILD_OPTIONS=
set ROOT_DIR=
set BUILD_DIR=
set METABUILD_CONFIG_FILE=
set CONFIGURATION=
set PLATFORM=
set VERSION=
set RUNTIME=

REM Parse arguments.
:NEXT_ARG
set ARG=%1
set UNQUOTED_ARG=%~1
shift
if not defined ARG goto :DONE_ARGS

if not "%UNQUOTED_ARG:~0,1%"=="/" goto :ADD_MODULE
if /I "%UNQUOTED_ARG%"=="/clean" set CLEAN=1& goto :NEXT_ARG
if /I "%UNQUOTED_ARG%"=="/build" set BUILD=1& goto :NEXT_ARG
if /I "%UNQUOTED_ARG%"=="/image" set IMAGE=1& goto :NEXT_ARG
if /I "%UNQUOTED_ARG%"=="/test" set TEST=1& goto :NEXT_ARG
if /I "%UNQUOTED_ARG%"=="/dist" set DIST=1& goto :NEXT_ARG
if /I "%UNQUOTED_ARG%"=="/call" set CALL_TARGETS=%CALL_TARGETS%;%~1& shift & goto :NEXT_ARG
if /I "%UNQUOTED_ARG%"=="/noparallel" set PARALLEL=& goto :NEXT_ARG
if /I "%UNQUOTED_ARG%"=="/verbosity" set VERBOSITY=%~1& shift & goto :NEXT_ARG
if /I "%UNQUOTED_ARG%"=="/rootdir" set ROOT_DIR=%~1& shift & goto :NEXT_ARG
if /I "%UNQUOTED_ARG%"=="/builddir" set BUILD_DIR=%~1& shift & goto :NEXT_ARG
if /I "%UNQUOTED_ARG%"=="/metabuildconfig" set METABUILD_CONFIG_FILE=%~1& shift & goto :NEXT_ARG
if /I "%UNQUOTED_ARG%"=="/configuration" set CONFIGURATION=%~1& shift & goto :NEXT_ARG
if /I "%UNQUOTED_ARG%"=="/platform" set PLATFORM=%~1& shift & goto :NEXT_ARG
if /I "%UNQUOTED_ARG%"=="/version" set VERSION=%~1& shift & goto :NEXT_ARG
if /I "%UNQUOTED_ARG%"=="/mono" set RUNTIME=Mono& goto :NEXT_ARG
if /I "%UNQUOTED_ARG%"=="//" goto :COPY_MSBUILD_OPTIONS
goto :HELP

:ADD_MODULE
if defined INITIAL_MODULES set INITIAL_MODULES=%INITIAL_MODULES%;%~dpnx1& goto :NEXT_ARG
set INITIAL_MODULES=%~dpnx1
goto :NEXT_ARG

:COPY_MSBUILD_OPTIONS
set ARG=%1
shift
if not defined ARG goto :DONE_ARGS
set MSBUILD_OPTIONS=%MSBUILD_OPTIONS% %ARG%
goto :COPY_MSBUILD_OPTIONS

:DONE_ARGS

REM Sanitize arguments.
call :SANITIZE ROOT_DIR
call :SANITIZE BUILD_DIR

REM Find MSBuild.
if not defined MSBUILD call :TRY_MSBUILD "%SystemRoot%\Microsoft.NET\Framework\v4.0.21006\msbuild.exe"
if not defined MSBUILD call :TRY_MSBUILD "%SystemRoot%\Microsoft.NET\Framework\v4.0.20506\msbuild.exe"
if not defined MSBUILD call :TRY_MSBUILD "%SystemRoot%\Microsoft.NET\Framework\v3.5\msbuild.exe"
if not defined MSBUILD (
  echo Build failed!
  echo Could not find MSBuild v3.5 or v4.0.
  goto :ERROR
)

REM Prepare command.
set TARGETS=
if defined CLEAN set TARGETS=%TARGETS%;Clean
if defined BUILD set TARGETS=%TARGETS%;Build
if defined IMAGE set TARGETS=%TARGETS%;Image
if defined TEST set TARGETS=%TARGETS%;Test
if defined DIST set TARGETS=%TARGETS%;Dist
if defined CALL_TARGETS set TARGETS=%TARGETS%;Call
if defined TARGETS set TARGETS=%TARGETS:~1%

set MSBUILD_COMMAND="%MSBUILD%" /nologo "%SCRIPT_DIR%MetaBuild.msbuild"
if defined PARALLEL set MSBUILD_COMMAND=%MSBUILD_COMMAND% /m /p:IsParallel=true
if not defined PARALLEL set MSBUILD_COMMAND=%MSBUILD_COMMAND% /p:IsParallel=false
if defined VERBOSITY set MSBUILD_COMMAND=%MSBUILD_COMMAND% /v:%VERBOSITY%
if defined INITIAL_MODULES set MSBUILD_COMMAND=%MSBUILD_COMMAND% /p:InitialModules="%INITIAL_MODULES%"
if defined ROOT_DIR set MSBUILD_COMMAND=%MSBUILD_COMMAND% /p:RootDir="%ROOT_DIR%"
if defined BUILD_DIR set MSBUILD_COMMAND=%MSBUILD_COMMAND% /p:BuildDir="%BUILD_DIR%"
if defined METABUILD_CONFIG_FILE set MSBUILD_COMMAND=%MSBUILD_COMMAND% /p:MetaBuildConfigFile="%METABUILD_CONFIG_FILE%"
if defined CONFIGURATION set MSBUILD_COMMAND=%MSBUILD_COMMAND% /p:Configuration="%CONFIGURATION%"
if defined PLATFORM set MSBUILD_COMMAND=%MSBUILD_COMMAND% /p:Platform="%PLATFORM%"
if defined VERSION set MSBUILD_COMMAND=%MSBUILD_COMMAND% /p:Version="%VERSION%"
if defined RUNTIME set MSBUILD_COMMAND=%MSBUILD_COMMAND% /p:Runtime="%RUNTIME%"
if defined TARGETS set MSBUILD_COMMAND=%MSBUILD_COMMAND% /t:%TARGETS%
if defined CALL_TARGETS set MSBUILD_COMMAND=%MSBUILD_COMMAND% /p:CallTargets="%CALL_TARGETS:~1%"
set MSBUILD_COMMAND=%MSBUILD_COMMAND% %MSBUILD_OPTIONS%

REM Run the build.
echo Building...
echo.
endlocal & %MSBUILD_COMMAND%
echo.
if errorlevel 1 (echo BUILD FAILED!) else (echo DONE.)
exit /b %ERRORLEVEL%


REM Error exit.
:ERROR
endlocal & exit /b 1


REM Conditionally set the MSBUILD env var if the provided path exists.
:TRY_MSBUILD
if exist "%~1" set MSBUILD=%~1
exit /b 0


REM Sanitizes a path by converting it to a full path without a trailing backslash.
REM We do this because the .Net command parser handles sequences like '\"' by taking the
REM quote literally.  This causes problems when specifying MSBuild property values
REM unless we remove or double-up the trailing backslash.
:SANITIZE
if not defined %~1 exit /b 0
for /F "tokens=*" %%V in ('echo %%%~1%%') do set SANITIZE_TEMP=%%~dpnxV
if "%SANITIZE_TEMP:~-1%"=="\" set SANITIZE_TEMP=%SANITIZE_TEMP:~0,-1%
set %~1=%SANITIZE_TEMP%
set SANITIZE_TEMP=
exit /b 0


REM Display help information.
:HELP
echo.
echo Usage: %SCRIPT_NAME% [modules] [metabuild options] [// [msbuild options]]
echo.
echo   Targets:
echo     /clean                Clean previous build outputs before starting.
echo     /build                Run the build pass.  (default if none)
echo     /image                Run the image pass.  (default if none)
echo     /test                 Run the tests.
echo     /dist                 Generate distribution packages.
echo     /call ...             Call the specified target in the initial modules.
echo.
echo   Paths:
echo     /rootdir ...          Set the root directory of the build tree.
echo     /builddir ...         Set the build output directory.
echo     /metabuildconfig ...  Set the path of the MetaBuild.config file.
echo.
echo   Settings:
echo     /configuration ...    Set the build configuration.
echo     /platform ...         Set the build platform.
echo     /version ...          Set the build version.
echo     /mono                 Set the build runtime to "Mono".
echo.
echo   MSBuild Options:
echo     /noparallel           Do not build projects in parallel.
echo     /verbosity ...        Set the MSBuild verbosity level.
echo                           Default value is: minimal.
echo     // ...                All subsequent arguments sent to MSBuild
echo                           without further reinterpretation.
echo.
echo   If modules are specified, builds those that are specified.
echo   Otherwise, builds all modules within the current directory ^(if any^).
echo.
goto :ERROR

