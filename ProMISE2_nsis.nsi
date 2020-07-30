# define the name of the installer
Outfile "ProMISE2.exe"
Icon "ProMISE2\Resources\ProMISE.ico"

InstallDir $TEMP\ProMISE2Setup

AutoCloseWindow true

# default section
Section

HideWindow

SetOutPath $INSTDIR

File /r ProMISE2Setup\Release\*.*
ExecWait "$INSTDIR\Setup.exe"

RMDir /r "$INSTDIR"

SectionEnd
