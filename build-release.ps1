$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$project = Join-Path $root 'GeniaText\GeniaText.csproj'
$solution = Join-Path $root 'GeniaText.sln'
$tests = Join-Path $root 'GeniaText.Tests\GeniaText.Tests.csproj'
$version = '0.7.0-rc.1'
$out = Join-Path $root "artifacts\GeniaText-$version"

if ($null -eq (Get-Command dotnet -ErrorAction SilentlyContinue)) { throw '.NET 10 SDK was not found.' }

& dotnet build $solution -c Release -warnaserror
if ($LASTEXITCODE -ne 0) { throw 'Build gate failed.' }
& dotnet run --project $tests -c Release --no-build
if ($LASTEXITCODE -ne 0) { throw 'Self-test gate failed.' }

if (Test-Path $out) { Remove-Item $out -Recurse -Force }
New-Item $out -ItemType Directory -Force | Out-Null

& dotnet publish $project -c Release -r win-x64 --self-contained false -p:DebugType=None -p:DebugSymbols=false -o $out
if ($LASTEXITCODE -ne 0) { throw 'dotnet publish failed.' }

Get-ChildItem $out -Filter '*.pdb' -ErrorAction SilentlyContinue | Remove-Item -Force
$zip = Join-Path $root "artifacts\GeniaText-$version-win-x64.zip"
if (Test-Path $zip) { Remove-Item $zip -Force }
Compress-Archive -Path (Join-Path $out '*') -DestinationPath $zip -CompressionLevel Optimal
$zipHash = (Get-FileHash $zip -Algorithm SHA256).Hash.ToLowerInvariant()
$hashPath = $zip + '.sha256'
Set-Content -Path $hashPath -Value "$zipHash  $(Split-Path $zip -Leaf)" -Encoding ASCII
Write-Host "Release created: $zip" -ForegroundColor Green
Write-Host "SHA256: $zipHash"
Write-Host "Hash file: $hashPath"
