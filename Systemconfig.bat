@echo off
echo Installing required software...

:: Define shared path
set SHARED_PATH=\\shared\path\to\software

:: Install .NET Core Hosting Bundle
echo Installing .NET Core Hosting Bundle...
start /wait %SHARED_PATH%\dotnet-hosting-bundle.exe /quiet /norestart

:: Install Node.js
echo Installing Node.js...
start /wait msiexec /i %SHARED_PATH%\nodejs.msi /quiet /norestart

:: Install IIS
echo Installing IIS...
powershell -Command "Install-WindowsFeature -name Web-Server -IncludeManagementTools"

:: Install URL Rewrite Module
echo Installing URL Rewrite Module...
start /wait msiexec /i %SHARED_PATH%\rewrite.msi /quiet /norestart

:: Set environment variables
echo Setting environment variables...
setx ASPNETCORE_ENVIRONMENT "Production"
setx NODE_ENV "production"

:: Restart IIS
echo Restarting IIS...
iisreset

echo Software installation completed.

:: Create IIS site for ASP.NET Core Web API
echo Creating IIS site for ASP.NET Core Web API...
powershell -Command "New-Item -Path 'C:\inetpub\wwwroot\aspnetcoreapi' -ItemType Directory"
powershell -Command "New-WebAppPool -Name 'AspNetCoreAppPool'"
powershell -Command "New-Website -Name 'AspNetCoreAPI' -Port 8080 -PhysicalPath 'C:\inetpub\wwwroot\aspnetcoreapi' -ApplicationPool 'AspNetCoreAppPool'"

:: Create IIS site for React app
echo Creating IIS site for React app...
powershell -Command "New-Item -Path 'C:\inetpub\wwwroot\reactapp' -ItemType Directory"
powershell -Command "New-Website -Name 'ReactApp' -Port 8081 -PhysicalPath 'C:\inetpub\wwwroot\reactapp'"

echo IIS setup completed.
echo Please deploy your ASP.NET Core Web API to 'C:\inetpub\wwwroot\aspnetcoreapi' and your React app to 'C:\inetpub\wwwroot\reactapp'.

pause