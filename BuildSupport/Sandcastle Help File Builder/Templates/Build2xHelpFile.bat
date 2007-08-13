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

IF EXIST "{@OutputFolder}{@HTMLHelpName}.hxs" DEL "{@OutputFolder}{@HTMLHelpName}.hxs" > NUL
IF EXIST "{@OutputFolder}{@HTMLHelpName}.hxi" DEL "{@OutputFolder}{@HTMLHelpName}.hxi" > NUL
IF EXIST ".\Output\{@HTMLHelpName}.hxs" COPY ".\Output\{@HTMLHelpName}.hxs" "{@OutputFolder}" > NUL
IF EXIST ".\Output\{@HTMLHelpName}.hxi" COPY ".\Output\{@HTMLHelpName}.hxi" "{@OutputFolder}" > NUL
