; NSIS Script for Multi-Algorithm Compression Tool
; Creates a Windows installer that includes both CLI and GUI applications

!define PRODUCT_NAME "Multi-Algorithm Compression Tool"
!define PRODUCT_VERSION "1.0.0"
!define PRODUCT_PUBLISHER "Multi-Algorithm Compression Tool"
!define PRODUCT_WEB_SITE "https://github.com/user/cpp-csharp-file-compression"
!define PRODUCT_DIR_REGKEY "Software\Microsoft\Windows\CurrentVersion\App Paths\compress.exe"
!define PRODUCT_UNINST_KEY "Software\Microsoft\Windows\CurrentVersion\Uninstall\${PRODUCT_NAME}"
!define PRODUCT_UNINST_ROOT_KEY "HKLM"

; Modern UI
!include "MUI2.nsh"

; General settings
Name "${PRODUCT_NAME}"
OutFile "MultiAlgorithmCompressionTool-${PRODUCT_VERSION}-Setup.exe"
InstallDir "$PROGRAMFILES64\${PRODUCT_NAME}"
InstallDirRegKey HKLM "${PRODUCT_DIR_REGKEY}" ""
ShowInstDetails show
ShowUnInstDetails show

; Compression
SetCompressor /SOLID lzma

; Request admin privileges
RequestExecutionLevel admin

; Interface Settings
!define MUI_ABORTWARNING

; Pages
!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_LICENSE "LICENSE"
!insertmacro MUI_PAGE_COMPONENTS
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES
!define MUI_FINISHPAGE_RUN "$INSTDIR\CompressionToolGUI.exe"
!define MUI_FINISHPAGE_SHOWREADME "$INSTDIR\README.txt"
!insertmacro MUI_PAGE_FINISH

; Uninstaller pages
!insertmacro MUI_UNPAGE_INSTFILES

; Language files
!insertmacro MUI_LANGUAGE "English"

; Version Information
VIProductVersion "${PRODUCT_VERSION}.0"
VIAddVersionKey "ProductName" "${PRODUCT_NAME}"
VIAddVersionKey "ProductVersion" "${PRODUCT_VERSION}"
VIAddVersionKey "CompanyName" "${PRODUCT_PUBLISHER}"
VIAddVersionKey "FileDescription" "${PRODUCT_NAME} Installer"
VIAddVersionKey "FileVersion" "${PRODUCT_VERSION}.0"
VIAddVersionKey "LegalCopyright" "© 2025 ${PRODUCT_PUBLISHER}"

; Installer sections
Section "Core Files (Required)" SecCore
  SectionIn RO

  ; Set output path to installation directory
  SetOutPath "$INSTDIR"

  ; Core files
  File "README.md"
  Rename "$INSTDIR\README.md" "$INSTDIR\README.txt"

  ; Create uninstaller
  WriteUninstaller "$INSTDIR\uninst.exe"

  ; Registry entries
  WriteRegStr HKLM "${PRODUCT_DIR_REGKEY}" "" "$INSTDIR\compress.exe"
  WriteRegStr ${PRODUCT_UNINST_ROOT_KEY} "${PRODUCT_UNINST_KEY}" "DisplayName" "$(^Name)"
  WriteRegStr ${PRODUCT_UNINST_ROOT_KEY} "${PRODUCT_UNINST_KEY}" "UninstallString" "$INSTDIR\uninst.exe"
  WriteRegStr ${PRODUCT_UNINST_ROOT_KEY} "${PRODUCT_UNINST_KEY}" "DisplayIcon" "$INSTDIR\compress.exe"
  WriteRegStr ${PRODUCT_UNINST_ROOT_KEY} "${PRODUCT_UNINST_KEY}" "DisplayVersion" "${PRODUCT_VERSION}"
  WriteRegStr ${PRODUCT_UNINST_ROOT_KEY} "${PRODUCT_UNINST_KEY}" "URLInfoAbout" "${PRODUCT_WEB_SITE}"
  WriteRegStr ${PRODUCT_UNINST_ROOT_KEY} "${PRODUCT_UNINST_KEY}" "Publisher" "${PRODUCT_PUBLISHER}"
SectionEnd

Section "Console Application (CLI)" SecCLI
  ; Console application files
  File "build\compress.exe"
  File "build\compression_lib.dll"

  ; Create start menu shortcuts
  CreateDirectory "$SMPROGRAMS\${PRODUCT_NAME}"
  CreateShortCut "$SMPROGRAMS\${PRODUCT_NAME}\Compression Tool (Console).lnk" "$INSTDIR\compress.exe" "" "$INSTDIR\compress.exe" 0

  ; Add to PATH (optional)
  ; Note: Manual PATH addition - can be enabled if needed
  ; WriteRegExpandStr HKLM "SYSTEM\CurrentControlSet\Control\Session Manager\Environment" "PATH" "$INSTDIR;%PATH%"
SectionEnd

Section "GUI Application" SecGUI
  ; GUI application files
  File "CSharpUI\CompressionTool\bin\Release\net8.0\win-x64\publish\CompressionToolGUI.exe"

  ; Create start menu shortcuts
  CreateDirectory "$SMPROGRAMS\${PRODUCT_NAME}"
  CreateShortCut "$SMPROGRAMS\${PRODUCT_NAME}\Compression Tool (GUI).lnk" "$INSTDIR\CompressionToolGUI.exe" "" "$INSTDIR\CompressionToolGUI.exe" 0

  ; Desktop shortcut (optional)
  CreateShortCut "$DESKTOP\Multi-Algorithm Compression Tool.lnk" "$INSTDIR\CompressionToolGUI.exe" "" "$INSTDIR\CompressionToolGUI.exe" 0
SectionEnd

Section "Documentation & Examples" SecDocs
  ; Documentation
  SetOutPath "$INSTDIR\docs"
  File /nonfatal "docs\*.*"

  ; Examples
  SetOutPath "$INSTDIR\examples"
  File /nonfatal "test_*.txt"
SectionEnd

; Section descriptions
!insertmacro MUI_FUNCTION_DESCRIPTION_BEGIN
  !insertmacro MUI_DESCRIPTION_TEXT ${SecCore} "Core application files (required)"
  !insertmacro MUI_DESCRIPTION_TEXT ${SecCLI} "Command-line interface for compression operations"
  !insertmacro MUI_DESCRIPTION_TEXT ${SecGUI} "Graphical user interface for easy file compression"
  !insertmacro MUI_DESCRIPTION_TEXT ${SecDocs} "Documentation and example files"
!insertmacro MUI_FUNCTION_DESCRIPTION_END

; Uninstaller
Section Uninstall
  ; Remove registry keys
  DeleteRegKey ${PRODUCT_UNINST_ROOT_KEY} "${PRODUCT_UNINST_KEY}"
  DeleteRegKey HKLM "${PRODUCT_DIR_REGKEY}"

  ; Remove files and directories
  Delete "$INSTDIR\compress.exe"
  Delete "$INSTDIR\compression_lib.dll"
  Delete "$INSTDIR\CompressionToolGUI.exe"
  Delete "$INSTDIR\README.txt"
  Delete "$INSTDIR\uninst.exe"

  ; Remove documentation and examples
  RMDir /r "$INSTDIR\docs"
  RMDir /r "$INSTDIR\examples"

  ; Remove shortcuts
  Delete "$SMPROGRAMS\${PRODUCT_NAME}\*.*"
  RMDir "$SMPROGRAMS\${PRODUCT_NAME}"
  Delete "$DESKTOP\Multi-Algorithm Compression Tool.lnk"

  ; Remove installation directory
  RMDir "$INSTDIR"

  ; Note: PATH cleanup would go here if PATH was modified during installation

  SetAutoClose true
SectionEnd