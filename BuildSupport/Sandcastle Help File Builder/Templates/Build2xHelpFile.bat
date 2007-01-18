@ECHO OFF

REM Step 6 - Build the HTML 2.x help file
cd .\Output
copy ..\*.hxc . > NUL
copy ..\*.hxf . > NUL
copy ..\*.hxt . > NUL
copy ..\*.hxk . > NUL
"{@HXCompPath}hxcomp" -p Help2x.hxc -l "{@HTMLHelpName}.log"

type "{@HTMLHelpName}.log"

cd ..

IF EXIST "..\{@HTMLHelpName}.hxs" DEL "..\{@HTMLHelpName}.hxs" > NUL
IF EXIST "..\{@HTMLHelpName}.hxi" DEL "..\{@HTMLHelpName}.hxi" > NUL
IF EXIST ".\Output\{@HTMLHelpName}.hxs" COPY ".\Output\{@HTMLHelpName}.hxs" ..\ > NUL
IF EXIST ".\Output\{@HTMLHelpName}.hxi" COPY ".\Output\{@HTMLHelpName}.hxi" ..\ > NUL
