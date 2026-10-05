param([string]$Configuration = 'Release')
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$solution = Join-Path $root 'GeniaText.sln'
$tests = Join-Path $root 'GeniaText.Tests\GeniaText.Tests.csproj'

if ($null -eq (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw '.NET 10 SDK was not found.'
}

Write-Host '== Build with warnings as errors ==' -ForegroundColor Cyan
& dotnet build $solution -c $Configuration -warnaserror
if ($LASTEXITCODE -ne 0) { throw "Build failed with exit code $LASTEXITCODE." }

Write-Host ''
Write-Host '== Pure logic self-tests ==' -ForegroundColor Cyan
& dotnet run --project $tests -c $Configuration --no-build
if ($LASTEXITCODE -ne 0) { throw "Self-tests failed with exit code $LASTEXITCODE." }

Write-Host ''
Write-Host 'All automated build/self-test checks passed.' -ForegroundColor Green
