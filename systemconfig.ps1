# Define shared path
$sharedPath = "\\shared\path\to\software"

# Install .NET Core Hosting Bundle
if (-not (Test-Path "$env:ProgramFiles\dotnet\shared\Microsoft.AspNetCore.App")) {
    Write-Output "Installing .NET Core Hosting Bundle..."
    Start-Process -FilePath "$sharedPath\dotnet-hosting-bundle.exe" -ArgumentList "/quiet /norestart" -Wait
} else {
    Write-Output ".NET Core Hosting Bundle is already installed."
}

# Install Node.js
if (-not (Test-Path "$env:ProgramFiles\nodejs")) {
    Write-Output "Installing Node.js..."
    Start-Process -FilePath "msiexec.exe" -ArgumentList "/i $sharedPath\nodejs.msi /quiet /norestart" -Wait
} else {
    Write-Output "Node.js is already installed."
}

# Install IIS
if (-not (Get-WindowsFeature -Name Web-Server).Installed) {
    Write-Output "Installing IIS..."
    Install-WindowsFeature -Name Web-Server -IncludeManagementTools
} else {
    Write-Output "IIS is already installed."
}

# Install URL Rewrite Module
if (-not (Test-Path "$env:ProgramFiles\IIS\Rewrite")) {
    Write-Output "Installing URL Rewrite Module..."
    Start-Process -FilePath "msiexec.exe" -ArgumentList "/i $sharedPath\rewrite.msi /quiet /norestart" -Wait
} else {
    Write-Output "URL Rewrite Module is already installed."
}

# Set environment variables
Write-Output "Setting environment variables..."
[System.Environment]::SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Production", [System.EnvironmentVariableTarget]::Machine)
[System.Environment]::SetEnvironmentVariable("NODE_ENV", "production", [System.EnvironmentVariableTarget]::Machine)

# Restart IIS
Write-Output "Restarting IIS..."
iisreset

Write-Output "Software installation completed."

# Create IIS site for main application
if (-not (Test-Path "C:\inetpub\wwwroot\mainapp")) {
    Write-Output "Creating IIS site for main application..."
    New-Item -Path "C:\inetpub\wwwroot\mainapp" -ItemType Directory
    if (-not (Get-WebAppPoolState -Name 'MainAppPool')) {
        New-WebAppPool -Name 'MainAppPool'
    }
    if (-not (Get-Website -Name 'MainApp')) {
        New-Website -Name 'MainApp' -Port 80 -PhysicalPath 'C:\inetpub\wwwroot\mainapp' -ApplicationPool 'MainAppPool'
    }
} else {
    Write-Output "Main application site already exists."
}

# Create application pool for ASP.NET Core Web API
if (-not (Get-WebAppPoolState -Name 'AspNetCoreAppPool')) {
    Write-Output "Creating application pool for ASP.NET Core Web API..."
    New-WebAppPool -Name 'AspNetCoreAppPool'
    Set-ItemProperty IIS:\AppPools\AspNetCoreAppPool -Name processModel.identityType -Value ApplicationPoolIdentity
    Set-ItemProperty IIS:\AppPools\AspNetCoreAppPool -Name managedRuntimeVersion -Value ''
} else {
    Write-Output "Application pool for ASP.NET Core Web API already exists."
}

# Create sub-application for ASP.NET Core Web API
if (-not (Test-Path "C:\inetpub\wwwroot\mainapp\aspnetcoreapi")) {
    Write-Output "Creating sub-application for ASP.NET Core Web API..."
    New-Item -Path "C:\inetpub\wwwroot\mainapp\aspnetcoreapi" -ItemType Directory
    if (-not (Get-WebApplication -Site 'MainApp' -Name 'AspNetCoreAPI')) {
        New-WebApplication -Name 'AspNetCoreAPI' -Site 'MainApp' -PhysicalPath 'C:\inetpub\wwwroot\mainapp\aspnetcoreapi' -ApplicationPool 'AspNetCoreAppPool'
    }
} else {
    Write-Output "ASP.NET Core Web API sub-application already exists."
}

# Create application pool for React app
if (-not (Get-WebAppPoolState -Name 'ReactAppPool')) {
    Write-Output "Creating application pool for React app..."
    New-WebAppPool -Name 'ReactAppPool'
    Set-ItemProperty IIS:\AppPools\ReactAppPool -Name processModel.identityType -Value ApplicationPoolIdentity
    Set-ItemProperty IIS:\AppPools\ReactAppPool -Name managedRuntimeVersion -Value ''
} else {
    Write-Output "Application pool for React app already exists."
}

# Create sub-application for React app
if (-not (Test-Path "C:\inetpub\wwwroot\mainapp\reactapp")) {
    Write-Output "Creating sub-application for React app..."
    New-Item -Path "C:\inetpub\wwwroot\mainapp\reactapp" -ItemType Directory