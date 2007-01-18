@ECHO OFF

REM Step 6 - Build the HTML 1.x help file
cd .\Output
copy ..\*.hhp . > NUL
copy ..\*.hhc . > NUL
copy ..\*.hhk . > NUL
"{@HHCPath}hhc.exe" Help1x.hhp
cd ..

IF EXIST "..\{@HTMLHelpName}.chm" DEL "..\{@HTMLHelpName}.chm" > NUL
IF EXIST ".\Output\{@HTMLHelpName}.chm" COPY ".\Output\{@HTMLHelpName}.chm" ..\ > NUL
