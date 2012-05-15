set rootdir=%~dp0
set debugbuildswitches=/p:WarningLevel=2 /nologo /v:n
set releasebuildswitches=/p:Configuration=Release /p:WarningLevel=2 /nologo /v:n

PATH=%PATH%;%WINDIR%\Microsoft.Net\Framework64\v4.0.30319

msbuild "%rootdir%\src\Vts.Desktop\Vts.Desktop.csproj" %debugbuildswitches%
msbuild "%rootdir%\src\Vts.Desktop\Vts.Desktop.csproj" %releasebuildswitches%

pause

msbuild "%rootdir%\src\Vts.MGRTE.ConsoleApp\Vts.MGRTE.ConsoleApp.csproj" %debugbuildswitches%
msbuild "%rootdir%\src\Vts.MGRTE.ConsoleApp\Vts.MGRTE.ConsoleApp.csproj" %releasebuildswitches%

pause

msbuild "%rootdir%\src\Vts.MonteCarlo.CommandLineApplication\Vts.MonteCarlo.CommandLineApplication.csproj" %debugbuildswitches%
msbuild "%rootdir%\src\Vts.MonteCarlo.CommandLineApplication\Vts.MonteCarlo.CommandLineApplication.csproj" %releasebuildswitches%

pause

msbuild "%rootdir%\src\Vts.MonteCarlo.PostProcessor\Vts.MonteCarlo.PostProcessor.csproj" %debugbuildswitches%
msbuild "%rootdir%\src\Vts.MonteCarlo.PostProcessor\Vts.MonteCarlo.PostProcessor.csproj" %releasebuildswitches%

pause

msbuild "%rootdir%\src\Vts.Desktop.Test\Vts.Desktop.Test.csproj" %debugbuildswitches%
msbuild "%rootdir%\src\Vts.Desktop.Test\Vts.Desktop.Test.csproj" %releasebuildswitches%

pause

msbuild "%rootdir%\src\Vts.MonteCarlo.CommandLineApplication.Test\Vts.MonteCarlo.CommandLineApplication.Test.csproj" %debugbuildswitches%
msbuild "%rootdir%\src\Vts.MonteCarlo.CommandLineApplication.Test\Vts.MonteCarlo.CommandLineApplication.Test.csproj" %releasebuildswitches%

pause

msbuild "%rootdir%\src\Vts.MonteCarlo.PostProcessor.Test\Vts.MonteCarlo.PostProcessor.Test.csproj" %debugbuildswitches%
msbuild "%rootdir%\src\Vts.MonteCarlo.PostProcessor.Test\Vts.MonteCarlo.PostProcessor.Test.csproj" %releasebuildswitches%

pause