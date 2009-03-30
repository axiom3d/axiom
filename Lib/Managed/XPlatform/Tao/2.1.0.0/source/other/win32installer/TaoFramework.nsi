!verbose 3

!define PRODUCT_NAME "TaoFramework"
!define PRODUCT_PACKAGE "taoframework"
!define PRODUCT_VERSION "2.1.0"
!define PRODUCT_BUILD "1"
!define PRODUCT_PUBLISHER "TaoFramework"
!define PRODUCT_WEB_SITE "http://www.taoframework.com"
!define PRODUCT_DIR_REGKEY "Software\Microsoft\Windows\CurrentVersion\App Paths\TaoFramework"
!define PRODUCT_UNINST_KEY "Software\Microsoft\Windows\CurrentVersion\Uninstall\TaoFramework"
!define PRODUCT_UNINST_ROOT_KEY "HKLM"
!define PRODUCT_DIR "..\..\dist"
!define PRODUCT_PATH "${PRODUCT_DIR}\${PRODUCT_PACKAGE}-${PRODUCT_VERSION}"
!define PRODUCT_SOURCE "${PRODUCT_PATH}\source"
!define PRODUCT_BIN "${PRODUCT_PATH}\bin"
!define PRODUCT_DOC "${PRODUCT_PATH}\doc"
!define PRODUCT_EXAMPLES "${PRODUCT_PATH}\examples"
!define PRODUCT_DEPS "${PRODUCT_PATH}\lib"

;!define MUI_WELCOMEFINISHPAGE_BITMAP "TaoLogo.bmp"
;!define MUI_WELCOMEFINISHPAGE_BITMAP_NOSTRETCH
;!define MUI_UNWELCOMEFINISHPAGE_BITMAP "TaoLogo.bmp"
;!define MUI_UNWELCOMEFINISHPAGE_BITMAP_NOSTRETCH

BrandingText "© 2003-2008 Tao Framework Team, http://www.taoframework.com"
SetCompressor lzma
CRCCheck on

; File Association defines
;!include "fileassoc.nsh"

; MUI 1.67 compatible ------
!include "MUI.nsh"

; MUI Settings
!define MUI_ABORTWARNING
!define MUI_ICON "${NSISDIR}/Contrib/Graphics/Icons/modern-install.ico"
!define MUI_UNICON "${NSISDIR}/Contrib/Graphics/Icons/modern-uninstall.ico"

;--------------------------------
;Variables

Var STARTMENU_FOLDER
Var INI_VALUE
Var file_handle
Var filename

;--------------------------------
;Installer Pages

; Welcome page
!insertmacro MUI_PAGE_WELCOME
; License page
!insertmacro MUI_PAGE_LICENSE "${PRODUCT_SOURCE}\COPYING"
; Components Page
!insertmacro MUI_PAGE_COMPONENTS
; Directory page
!insertmacro MUI_PAGE_DIRECTORY

;Start Menu Folder Page Configuration
!define MUI_STARTMENUPAGE_REGISTRY_ROOT "HKCU" 
!define MUI_STARTMENUPAGE_REGISTRY_KEY "Software\TaoFramework" 
!define MUI_STARTMENUPAGE_REGISTRY_VALUENAME "Start Menu Folder"
  
!insertmacro MUI_PAGE_STARTMENU Application $STARTMENU_FOLDER

Page custom CustomPageC 
; Instfiles page
!insertmacro MUI_PAGE_INSTFILES

; Finish page
!insertmacro MUI_PAGE_FINISH

;------------------------------------
; Uninstaller pages
!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES
!insertmacro MUI_UNPAGE_FINISH
;------------------------------------

; Language files
!insertmacro MUI_LANGUAGE "English"

; Reserve files
!insertmacro MUI_RESERVEFILE_INSTALLOPTIONS

; MUI end ------

ReserveFile "runtime.ini"
!insertmacro MUI_RESERVEFILE_INSTALLOPTIONS


Name "${PRODUCT_NAME} ${PRODUCT_VERSION}"
OutFile "${PRODUCT_DIR}\${PRODUCT_PACKAGE}-${PRODUCT_VERSION}-setup.exe"
InstallDir "$PROGRAMFILES\TaoFramework"
InstallDirRegKey HKLM "${PRODUCT_DIR_REGKEY}" ""
ShowInstDetails show
ShowUnInstDetails show

; .NET Framework check
; http://msdn.microsoft.com/netframework/default.aspx?pull=/library/en-us/dnnetdep/html/redistdeploy1_1.asp
; Section "Detecting that the .NET Framework 2.0 is installed"
Function .onInit
!insertmacro MUI_INSTALLOPTIONS_EXTRACT "runtime.ini"
	ReadRegDWORD $R0 HKLM "SOFTWARE\Microsoft\NET Framework Setup\NDP\v2.0.50727" Install
	StrCmp $R0 "" 0 CheckPreviousVersion
	MessageBox MB_OK "Microsoft .NET Framework 2.0 was not found on this system.$\r$\n$\r$\nUnable to continue this installation."
	Abort

  CheckPreviousVersion:
	ReadRegStr $R0 ${PRODUCT_UNINST_ROOT_KEY} "${PRODUCT_UNINST_KEY}" "DisplayName"
	StrCmp $R0 "" CheckOSVersion 0
	MessageBox MB_OK "An old version of TaoFramework is installed on this computer, please uninstall first.$\r$\n$\r$\nUnable to continue this installation."
	Abort
	
  CheckOSVersion:
        Call IsSupportedWindowsVersion
        Pop $R0
        StrCmp $R0 "False" NoAbort 0
	MessageBox MB_OK "The operating system you are using is not supported by TaoFramework (95/98/ME/NT3.x/NT4.x)."
        Abort

  NoAbort:
FunctionEnd

Section "Source" SecSrc
  SetOverwrite ifnewer
  
  SetOutPath "$INSTDIR\source\m4"
  File /r /x .svn ${PRODUCT_SOURCE}\m4\*

  SetOutPath "$INSTDIR\source\tests"
  File /r /x obj /x .svn ${PRODUCT_SOURCE}\tests\*

  SetOutPath "$INSTDIR\source\src"
  File /r /x obj /x bin /x doc /x .svn ${PRODUCT_SOURCE}\src\*

  SetOutPath "$INSTDIR\source\other"
  File /r /x .svn /x *.swp ${PRODUCT_SOURCE}\other\*

  SetOutPath "$INSTDIR\source\lib"
  File /r /x .svn /x *.swp ${PRODUCT_SOURCE}\lib\*

  SetOutPath "$INSTDIR\source\examples"
  File /r /x .svn /x *.swp ${PRODUCT_SOURCE}\examples\*

  SetOutPath "$INSTDIR\source"
  File /x .svn /x .auto /x autom4te.cache ${PRODUCT_SOURCE}\*

  ;Store installation folder
  WriteRegStr HKCU "Software\TaoFramework" "" $INSTDIR
  
SectionEnd

Section "Runtime" SecRuntime
  SetOverwrite ifnewer
  SetOutPath "$INSTDIR\bin"
  File /r /x .svn /x *.config ${PRODUCT_BIN}\*
  
  SetOutPath "$INSTDIR\lib"
  File /r /x .svn ${PRODUCT_DEPS}\*

  SetOutPath "$INSTDIR\other\Prebuild"
  File "${PRODUCT_SOURCE}\other\Prebuild\*"

  ;Store installation folder
  WriteRegStr HKCU "Software\TaoFramework" "" $INSTDIR
  
  ;Read a value from an InstallOptions INI file
  !insertmacro MUI_INSTALLOPTIONS_READ $INI_VALUE "runtime.ini" "Field 3" "State"
  StrCmp $INI_VALUE "1" "" +3
  SetOutPath "$SYSDIR"
  File /r /x .svn ${PRODUCT_SOURCE}\lib\*
  
  !insertmacro MUI_INSTALLOPTIONS_READ $INI_VALUE "runtime.ini" "Field 2" "State"
  StrCmp $INI_VALUE "1" "" +4
  Push "TaoFramework"
  Push $INSTDIR\bin
  Call AddManagedDLL
SectionEnd

Section "Examples" SecExamples
  SetOverwrite ifnewer

  SetOutPath "$INSTDIR\examples"
  File /r /x obj ${PRODUCT_EXAMPLES}\*

  CreateDirectory "$SMPROGRAMS\TaoFramework"
  CreateDirectory "$SMPROGRAMS\TaoFramework\Examples"
  call CreateExampleShortcuts

  ;Store installation folder
  WriteRegStr HKCU "Software\TaoFramework" "" $INSTDIR
  
SectionEnd

Section "Documentation" SecDocs
  SetOverwrite ifnewer
  SetOutPath "$INSTDIR\doc"
  File /r ${PRODUCT_DOC}\*.chm
  
  CreateDirectory "$SMPROGRAMS\TaoFramework\Documentation"
  call CreateDocShortcuts

  ;Store installation folder
  WriteRegStr HKCU "Software\TaoFramework" "" $INSTDIR
SectionEnd

Function CustomPageC

  !insertmacro MUI_HEADER_TEXT "$(TEXT_IO_TITLE)" "$(TEXT_IO_SUBTITLE)"
  !insertmacro MUI_INSTALLOPTIONS_DISPLAY "runtime.ini"

FunctionEnd

; Usage:
;   Push $SYSDIR\myDll.dll
;   Push "MyAssemblyName"
;   Call AddManagedDLL
;
Function AddManagedDLL
  Exch $R0
  Exch
  Exch $R1
 
  call GACInstall
  WriteRegStr HKLM "SOFTWARE\Microsoft\.NETFramework\AssemblyFolders\$R1" "" "$R0"
  WriteRegStr HKCU "SOFTWARE\Microsoft\.NETFramework\AssemblyFolders\$R1" "" "$R0"
  WriteRegStr HKLM "SOFTWARE\Microsoft\VisualStudio\8.0\AssemblyFolders\$R1" "" "$R0"
 
  Pop $R1
  Pop $R0
FunctionEnd

Function un.DeleteManagedDLLKey
  Exch $R0
  Exch
  Exch $R1
  
  DeleteRegKey HKLM "SOFTWARE\Microsoft\.NETFramework\AssemblyFolders\$R1" 
  DeleteRegKey HKCU "SOFTWARE\Microsoft\.NETFramework\AssemblyFolders\$R1" 
  DeleteRegKey HKLM "SOFTWARE\Microsoft\VisualStudio\8.0\AssemblyFolders\$R1"
 
  Pop $R1
  Pop $R0
FunctionEnd

;Language strings
LangString TEXT_IO_TITLE ${LANG_ENGLISH} "Installation Options"
LangString TEXT_IO_SUBTITLE ${LANG_ENGLISH} "TaoFramework Installation Options."
LangString DESC_SecExamples ${LANG_ENGLISH} "Installs examples using various features of TaoFramework."
LangString DESC_SecSrc ${LANG_ENGLISH} "Installs the source code."
LangString DESC_SecDocs ${LANG_ENGLISH} "Installs documentation"
LangString DESC_SecRuntime ${LANG_ENGLISH} "Copies the runtime libaries to the TaoFramework directory. It does not install them into the GAC."

;Assign language strings to sections
!insertmacro MUI_FUNCTION_DESCRIPTION_BEGIN
!insertmacro MUI_DESCRIPTION_TEXT ${SecExamples} $(DESC_SecExamples)
!insertmacro MUI_DESCRIPTION_TEXT ${SecSrc} $(DESC_SecSrc)
!insertmacro MUI_DESCRIPTION_TEXT ${SecDocs} $(DESC_SecDocs)
!insertmacro MUI_DESCRIPTION_TEXT ${SecRuntime} $(DESC_SecRuntime)
!insertmacro MUI_FUNCTION_DESCRIPTION_END


Section -AdditionalIcons
  WriteIniStr "$INSTDIR\${PRODUCT_NAME}.url" "InternetShortcut" "URL" "${PRODUCT_WEB_SITE}"
  CreateShortCut "$SMPROGRAMS\TaoFramework\Website.lnk" "$INSTDIR\${PRODUCT_NAME}.url"
  CreateShortCut "$SMPROGRAMS\TaoFramework\Uninstall.lnk" "$INSTDIR\uninst.exe"
SectionEnd

Section -Post
  WriteUninstaller "$INSTDIR\uninst.exe"
  WriteRegStr ${PRODUCT_UNINST_ROOT_KEY} "${PRODUCT_UNINST_KEY}" "DisplayName" "$(^Name)"
  WriteRegStr ${PRODUCT_UNINST_ROOT_KEY} "${PRODUCT_UNINST_KEY}" "UninstallString" "$INSTDIR\uninst.exe"
  WriteRegStr ${PRODUCT_UNINST_ROOT_KEY} "${PRODUCT_UNINST_KEY}" "DisplayVersion" "${PRODUCT_VERSION}"
  WriteRegStr ${PRODUCT_UNINST_ROOT_KEY} "${PRODUCT_UNINST_KEY}" "URLInfoAbout" "${PRODUCT_WEB_SITE}"
  WriteRegStr ${PRODUCT_UNINST_ROOT_KEY} "${PRODUCT_UNINST_KEY}" "Publisher" "${PRODUCT_PUBLISHER}"
SectionEnd

Section Uninstall
  Call un.GACUnInstall
  Delete "$SMPROGRAMS\TaoFramework\*.*"

  ; set OutPath to somewhere else because the current working directory cannot be deleted!
  SetOutPath "$DESKTOP"
  
  RMDir /r "$SMPROGRAMS\TaoFramework"
  
  DeleteRegKey ${PRODUCT_UNINST_ROOT_KEY} "${PRODUCT_UNINST_KEY}"
  DeleteRegKey HKLM "${PRODUCT_DIR_REGKEY}"

  Push "TaoFramework"
  Push $INSTDIR\bin
  Call un.DeleteManagedDLLKey
  
  RMDir /r "$INSTDIR"
SectionEnd

; GetWindowsVersion, taken from NSIS help, modified for our purposes
Function IsSupportedWindowsVersion

   Push $R0
   Push $R1

   ReadRegStr $R0 HKLM \
   "SOFTWARE\Microsoft\Windows NT\CurrentVersion" CurrentVersion

   IfErrors 0 lbl_winnt

   ; we are not NT
   ReadRegStr $R0 HKLM \
   "SOFTWARE\Microsoft\Windows\CurrentVersion" VersionNumber

   StrCpy $R1 $R0 1
   StrCmp $R1 '4' 0 lbl_error

   StrCpy $R1 $R0 3

   StrCmp $R1 '4.0' lbl_win32_95
   StrCmp $R1 '4.9' lbl_win32_ME lbl_win32_98

   lbl_win32_95:
     StrCpy $R0 'False'
   Goto lbl_done

   lbl_win32_98:
     StrCpy $R0 'False'
   Goto lbl_done

   lbl_win32_ME:
     StrCpy $R0 'False'
   Goto lbl_done

   lbl_winnt:

   StrCpy $R1 $R0 1

   StrCmp $R1 '3' lbl_winnt_x
   StrCmp $R1 '4' lbl_winnt_x

   StrCpy $R1 $R0 3

   StrCmp $R1 '5.0' lbl_winnt_2000
   StrCmp $R1 '5.1' lbl_winnt_XP
   StrCmp $R1 '5.2' lbl_winnt_2003 lbl_error

   lbl_winnt_x:
     StrCpy $R0 'False'
   Goto lbl_done

   lbl_winnt_2000:
     Strcpy $R0 'True'
   Goto lbl_done

   lbl_winnt_XP:
     Strcpy $R0 'True'
   Goto lbl_done

   lbl_winnt_2003:
     Strcpy $R0 'True'
   Goto lbl_done

   lbl_error:
     Strcpy $R0 'False'
   lbl_done:

   Pop $R1
   Exch $R0

FunctionEnd

Function AddExampleToStartMenu
    Pop $0 ; link
    IfFileExists $INSTDIR\examples\$0 0 +2
      CreateShortCut $SMPROGRAMS\TaoFramework\Examples\$0.lnk $INSTDIR\examples\$0
FunctionEnd

Function CreateExampleShortcuts
  FindFirst $file_handle $filename $INSTDIR\examples\*.exe
  loop:
	StrCmp $filename "" done
  	Push $filename
  	call AddExampleToStartMenu
	FindNext $file_handle $filename
  	Goto loop
  done:

FunctionEnd

Function CreateDocShortcuts
  FindFirst $file_handle $filename $INSTDIR\doc\*.chm
  loop:
	StrCmp $filename "" done
  	Push $filename
	Push $filename
  	call AddDocToStartMenu
	FindNext $file_handle $filename
  	Goto loop
  done:

FunctionEnd

Function AddDocToStartMenu
    Pop $0 ; link
    Pop $1 ; file
    IfFileExists $INSTDIR\doc\$1 0 +2
      CreateShortCut $SMPROGRAMS\TaoFramework\Documentation\$0.lnk $INSTDIR\doc\$1
FunctionEnd

Function GACInstall
  FindFirst $file_handle $filename $INSTDIR\bin\*.dll
  loop:
	StrCmp $filename "" done
	nsExec::Exec '"$INSTDIR/other/Prebuild/prebuild.exe" /install "$INSTDIR/bin/$filename"'
	FindNext $file_handle $filename
  	Goto loop
  done:

FunctionEnd

Function un.GACUnInstall
  FindFirst $file_handle $filename $INSTDIR\bin\*.dll
  loop:
	StrCmp $filename "" done
	nsExec::Exec '"$INSTDIR/other/Prebuild/prebuild.exe" /remove "$INSTDIR/bin/$filename"'
	FindNext $file_handle $filename
  	Goto loop
  done:
FunctionEnd

