param(
    [string]$RuntimeIdentifier = "win-x64"
)

$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$project = Join-Path $root 'GeniaText\GeniaText.csproj'
$version = '0.7.0-beta.1'
$artifacts = Join-Path $root 'artifacts'
$publishOut = Join-Path $artifacts "publish-$RuntimeIdentifier"
$packageOut = Join-Path $artifacts "GeniaText-$version-portable-$RuntimeIdentifier"
$zip = Join-Path $artifacts "GeniaText-$version-portable-$RuntimeIdentifier.zip"

Write-Host "== GeniaText $version compact portable ($RuntimeIdentifier) ==" -ForegroundColor Cyan
Write-Host "Checking .NET SDK..."

$dotnetCommand = Get-Command dotnet -ErrorAction SilentlyContinue
if ($null -eq $dotnetCommand) {
    throw '.NET SDK was not found. Install the .NET 10 SDK and run build-portable.cmd again.'
}

$dotnetVersion = & dotnet --version
if ($LASTEXITCODE -ne 0) {
    throw 'Unable to query the installed .NET SDK version.'
}
Write-Host "  SDK: $dotnetVersion"

Write-Host "Running build + self-test gate..."
$solution = Join-Path $root 'GeniaText.sln'
$tests = Join-Path $root 'GeniaText.Tests\GeniaText.Tests.csproj'
& dotnet build $solution -c Release -warnaserror
if ($LASTEXITCODE -ne 0) { throw "Build gate failed with exit code $LASTEXITCODE." }
& dotnet run --project $tests -c Release --no-build
if ($LASTEXITCODE -ne 0) { throw "Self-test gate failed with exit code $LASTEXITCODE." }

if (-not (Test-Path $project)) {
    throw "Project file was not found: $project"
}

foreach ($path in @($publishOut, $packageOut)) {
    if (Test-Path $path) { Remove-Item $path -Recurse -Force }
}
if (Test-Path $zip) { Remove-Item $zip -Force }
New-Item $publishOut -ItemType Directory -Force | Out-Null
New-Item $packageOut -ItemType Directory -Force | Out-Null

Write-Host "Publishing self-contained SINGLE-FILE build..."
& dotnet publish $project `
    -c Release `
    -r $RuntimeIdentifier `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -p:PublishTrimmed=false `
    -p:PublishReadyToRun=false `
    -p:DebugType=None `
    -p:DebugSymbols=false `
    -p:GeniaTextPortableBuild=true `
    -o $publishOut

if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE."
}

$exePath = Join-Path $publishOut 'GeniaText.exe'
$markerPath = Join-Path $publishOut 'GeniaText.portable'
if (-not (Test-Path $exePath)) {
    throw 'GeniaText.exe was not found after dotnet publish.'
}
if (-not (Test-Path $markerPath)) {
    # Be defensive in case MSBuild did not copy the marker for some SDK version.
    Set-Content -Path $markerPath -Value 'GeniaText portable mode' -Encoding ASCII
}

# The field-test package intentionally contains only the executable and the portable marker.
Copy-Item $exePath (Join-Path $packageOut 'GeniaText.exe') -Force
Copy-Item $markerPath (Join-Path $packageOut 'GeniaText.portable') -Force

$packageFiles = @(Get-ChildItem $packageOut -File)
if ($packageFiles.Count -ne 2) {
    throw "Unexpected compact package file count: $($packageFiles.Count). Expected 2."
}

Write-Host "Creating ZIP..."
Compress-Archive -Path (Join-Path $packageOut '*') -DestinationPath $zip -CompressionLevel Optimal
if (-not (Test-Path $zip)) {
    throw 'Portable ZIP was not created.'
}

$exeMb = [math]::Round((Get-Item (Join-Path $packageOut 'GeniaText.exe')).Length / 1MB, 1)
Write-Host ''
Write-Host 'Build completed successfully.' -ForegroundColor Green
Write-Host "  Package: $packageOut"
$zipHash = (Get-FileHash $zip -Algorithm SHA256).Hash.ToLowerInvariant()
$hashPath = $zip + '.sha256'
Set-Content -Path $hashPath -Value "$zipHash  $(Split-Path $zip -Leaf)" -Encoding ASCII
Write-Host "  ZIP:     $zip"
Write-Host "  SHA256:  $zipHash"
Write-Host "  Hash:    $hashPath"
Write-Host "  EXE:     $exeMb MB"
Write-Host ''
Write-Host 'The portable package contains exactly 2 files:'
Write-Host '  GeniaText.exe'
Write-Host '  GeniaText.portable'
Write-Host ''
Write-Host 'Keep both files together. User data will be created next to GeniaText.exe.'
