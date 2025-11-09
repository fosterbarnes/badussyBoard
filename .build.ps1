Write-Host "Syncing version number..."
$versionFile = Join-Path $PSScriptRoot ".version";              $version = Get-Content $versionFile -Raw
$csprojFile  = Join-Path $PSScriptRoot "BadussyBoard.csproj";   $csproj = Get-Content $csprojFile
$csproj[3] = "    <AssemblyVersion>$version</AssemblyVersion>"
$csproj[4] = "    <Version>$version</Version>"
$csproj[5] = "    <FileVersion>$version</FileVersion>"
Set-Content $csprojFile -Value $csproj

Write-Host "Building..."
Set-Location $PSScriptRoot; dotnet build

Write-Host "Zipping build..."
$binDebugFolder = Join-Path $PSScriptRoot "\bin\Debug\net8.0-windows7.0"
$releaseFolder = Join-Path $PSScriptRoot "\release"
$zipFile = Join-Path $releaseFolder "BadussyBoard.$version.zip"
7z a $zipFile "$binDebugFolder\*"