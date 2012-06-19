set rootdir=%~dp0
set version=1.0.5

rem ********** CLEAN THE MC RELEASE FOLDERS **********
rmdir "%rootdir%build" /s /q
rmdir "%rootdir%src\Vts.MonteCarlo.CommandLineApplication\bin" /s /q
rmdir "%rootdir%matlab\vts_wrapper\vts_libraries" /s /q


rem ********** BUILD THE DESKTOP VERSION **********
call "%rootdir%DesktopBuild.bat"
call "%rootdir%DesktopTests.bat"


rem ********** CREATE THE RELEASE PACKAGES **********
call "%rootdir%src\Vts.MonteCarlo.CommandLineApplication\CreateRelease.bat" %version%
call "%rootdir%matlab\CreateRelease.bat" %version% 

rem ********** BUILD THE SILVERLIGHT VERSION **********
rem call "%rootdir%SilverlightBuild.bat"

cd "%rootdir%src\Vts.Test\bin\Debug\"
start TestPage.html

pause

cd "%rootdir%src\Vts.SiteVisit\bin\Debug\"
start TestPage.html