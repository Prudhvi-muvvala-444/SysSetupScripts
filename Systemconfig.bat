@echo off
echo Installing required software...

:: Define shared path
set SHARED_PATH=\\shared\path\to\software

:: Install .NET Core Hosting Bundle
if not exist "%ProgramFiles%\dotnet\shared\Microsoft.AspNetCore.App" (
    echo Installing .NET Core Hosting Bundle...
    start /wait %SHARED_PATH%\dotnet-hosting-bundle.exe /quiet /norestart
) else (
    echo .NET Core Hosting Bundle is already installed.
)

:: Install Node.js
if not exist "%ProgramFiles%\nodejs" (
    echo Installing Node.js...
    start /wait msiexec /i %SHARED_PATH%\nodejs.msi /quiet /norestart
) else (
    echo Node.js is already installed.
)

:: Install IIS
powershell -Command "if (-not (Get-WindowsFeature -Name Web-Server).Installed) { Install-WindowsFeature -name Web-Server -IncludeManagementTools } else { Write-Host 'IIS is already installed.' }"

:: Install URL Rewrite Module
if not exist "%ProgramFiles%\IIS\Rewrite" (
    echo Installing URL Rewrite Module...
    start /wait msiexec /i %SHARED_PATH%\rewrite.msi /quiet /norestart
) else (
    echo URL Rewrite Module is already installed.
)

:: Set environment variables
echo Setting environment variables...
setx ASPNETCORE_ENVIRONMENT "Production"
setx NODE_ENV "production"

:: Restart IIS
echo Restarting IIS...
iisreset

echo Software installation completed.

:: Create IIS site for main application
if not exist "C:\inetpub\wwwroot\mainapp" (
    echo Creating IIS site for main application...
    powershell -Command "New-Item -Path 'C:\inetpub\wwwroot\mainapp' -ItemType Directory"
    powershell -Command "if (-not (Get-WebAppPoolState -Name 'MainAppPool')) { New-WebAppPool -Name 'MainAppPool' }"
    powershell -Command "if (-not (Get-Website -Name 'MainApp')) { New-Website -Name 'MainApp' -Port 80 -PhysicalPath 'C:\inetpub\wwwroot\mainapp' -ApplicationPool 'MainAppPool' }"
) else (
    echo Main application site already exists.
)

:: Create application pool for ASP.NET Core Web API
if (-not (Get-WebAppPoolState -Name 'AspNetCoreAppPool')) (
    echo Creating application pool for ASP.NET Core Web API...
    powershell -Command "New-WebAppPool -Name 'AspNetCoreAppPool'"
    powershell -Command "Set-ItemProperty IIS:\AppPools\AspNetCoreAppPool -Name processModel.identityType -Value ApplicationPoolIdentity"
    powershell -Command "Set-ItemProperty IIS:\AppPools\AspNetCoreAppPool -Name managedRuntimeVersion -Value ''"
) else (
    echo Application pool for ASP.NET Core Web API already exists.
)

:: Create sub-application for ASP.NET Core Web API
if not exist "C:\inetpub\wwwroot\mainapp\aspnetcoreapi" (
    echo Creating sub-application for ASP.NET Core Web API...
    powershell -Command "New-Item -Path 'C:\inetpub\wwwroot\mainapp\aspnetcoreapi' -ItemType Directory"
    powershell -Command "if (-not (Get-WebApplication -Site 'MainApp' -Name 'AspNetCoreAPI')) { New-WebApplication -Name 'AspNetCoreAPI' -Site 'MainApp' -PhysicalPath 'C:\inetpub\wwwroot\mainapp\aspnetcoreapi' -ApplicationPool 'AspNetCoreAppPool' }"
) else (
    echo ASP.NET Core Web API sub-application already exists.
)

:: Create application pool for React app
if (-not (Get-WebAppPoolState -Name 'ReactAppPool')) (
    echo Creating application pool for React app...
    powershell -Command "New-WebAppPool -Name 'ReactAppPool'"
    powershell -Command "Set-ItemProperty IIS:\AppPools\ReactAppPool -Name processModel.identityType -Value ApplicationPoolIdentity"
    powershell -Command "Set-ItemProperty IIS:\AppPools\ReactAppPool -Name managedRuntimeVersion -Value ''"
) else (
    echo Application pool for React app already exists.
)

:: Create sub-application for React app
if not exist "C:\inetpub\wwwroot\mainapp\reactapp" (
    echo Creating sub-application for React app...
    powershell -Command "New-Item -Path 'C:\inetpub\wwwroot\mainapp\reactapp' -ItemType Directory"
    powershell -Command "if (-not (Get-WebApplication -Site 'MainApp' -Name 'ReactApp')) { New-WebApplication -Name 'ReactApp' -Site 'MainApp' -PhysicalPath 'C:\inetpub\wwwroot\mainapp\reactapp' -ApplicationPool 'ReactAppPool' }"
) else (
    echo React app sub-application already exists.
)

:: Set folder permissions
echo Setting folder permissions...
icacls "C:\inetpub\wwwroot\mainapp" /grant "IIS_IUSRS:(OI)(CI)F" /T
icacls "C:\inetpub\wwwroot\mainapp\aspnetcoreapi" /grant "IIS_IUSRS:(OI)(CI)F" /T
icacls "C:\inetpub\wwwroot\mainapp\reactapp" /grant "IIS_IUSRS:(OI)(CI)F" /T

echo IIS setup and folder permissions completed.
echo Please deploy your ASP.NET Core Web API to 'C:\inetpub\wwwroot\mainapp\aspnetcoreapi' and your React app to 'C:\inetpub\wwwroot\mainapp\reactapp'.

pause