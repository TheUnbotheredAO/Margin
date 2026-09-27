$ErrorActionPreference = 'Stop'
$bundle = Split-Path $PSScriptRoot -Parent
& dotnet run --project (Join-Path $PSScriptRoot 'Scanner.Tests/Scanner.Tests.csproj') -c Release -- (Join-Path $bundle 'config/recipes.json')
if ($LASTEXITCODE -ne 0) { throw 'Tests failed.' }
& dotnet publish (Join-Path $PSScriptRoot 'Scanner.Desktop/Scanner.Desktop.csproj') -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -p:DebugType=None -o (Join-Path $bundle 'build')
if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
Write-Output 'Ready: build/AlbionScanner.exe (keep config beside it).'

