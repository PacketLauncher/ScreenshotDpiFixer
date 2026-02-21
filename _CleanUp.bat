@echo off
echo Before running this script, make sure to run a 'Clean Solution' from Visual Studio for both Debug and Release. Then,
echo close the program and run this script for cleaning the following leftovers before uploading the source code to GitHub:
echo.
echo - .vs\
echo - ScreenshotDpiFixer\bin\
echo - ScreenshotDpiFixer\obj\

:START
echo.
set /P PARAM_VALUE=Do you wish to proceed? (Y/N)

if /I "%PARAM_VALUE%"=="y" goto BEGIN
if /I "%PARAM_VALUE%"=="n" goto END

echo Invalid input, please enter Y or N.
goto START

:BEGIN
if exist ".vs" rmdir ".vs" /S /Q
if exist "ScreenshotDpiFixer\bin" rmdir "ScreenshotDpiFixer\bin" /S /Q
if exist "ScreenshotDpiFixer\obj" rmdir "ScreenshotDpiFixer\obj" /S /Q

:END
exit