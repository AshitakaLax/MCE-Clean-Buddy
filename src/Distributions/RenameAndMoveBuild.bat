@echo off
set build_ver=2.4.1
set dest_folder=\\192.168.1.251\MCEBuddyHostedFiles\EARLY_ACCESS\%build_ver%_BETA
set dt=%date:~-4,4%%date:~-10,2%%date:~-7,2%

echo Changing directory to %1
cd %1

echo Creating directory %dest_folder%
md "%dest_folder%"

echo Copying ..\lib\readme\readme.rtf to %dest_folder%\
copy "..\lib\readme\readme.rtf" "%dest_folder%\readme - %dt%.rtf" /y

REM Copy 32bit if nothing is specified or x86 is specified
if NOT "%2"=="x64" (
echo Copying MCEBuddy 2.3.x Remote Client.zip to %dest_folder%\MCEBuddy %build_ver% Remote Client - %dt%.zip
copy "MCEBuddy 2.3.x Remote Client.zip" "%dest_folder%\MCEBuddy %build_ver% Remote Client - %dt%.zip" /y

echo Copying MCEBuddy 2.3.x 32bit.zip to %dest_folder%\MCEBuddy %build_ver% 32bit - %dt%.zip
copy "MCEBuddy 2.3.x 32bit.zip" "%dest_folder%\MCEBuddy %build_ver% 32bit - %dt%.zip" /y
)

REM Copy 64bit if nothing is specified or x64 is specified
if NOT "%2"=="x86" (
echo Copying MCEBuddy 2.3.x 64bit.zip to %dest_folder%\MCEBuddy %build_ver% 64bit - %dt%.zip
copy "MCEBuddy 2.3.x 64bit.zip" "%dest_folder%\MCEBuddy %build_ver% 64bit - %dt%.zip" /y
)

exit /b 0