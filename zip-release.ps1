param (
    [string]$version = "v1.0.0"
)

$releaseDir = "release"
$zipName = "Oggify-$version"
$zipFolder = "$releaseDir\$zipName"
$zipFile = "$releaseDir\$zipName.zip"

# Clean previous
Remove-Item -Recurse -Force $zipFolder -ErrorAction SilentlyContinue
Remove-Item $zipFile -Force -ErrorAction SilentlyContinue

# Locate publish folder
$publishDir = Get-ChildItem -Path "./bin/Release/" -Recurse -Directory |
        Where-Object { $_.FullName -like "*publish" } |
        Select-Object -First 1

if (-not $publishDir) {
    Write-Host "Could not locate publish directory."
    exit 1
}

# Create release folder
New-Item -ItemType Directory -Path $zipFolder | Out-Null

# Copy all publish contents
Copy-Item "$($publishDir.FullName)\*" -Destination "$zipFolder" -Recurse -Force

# Optional: include README and LICENSE
if (Test-Path "./README.md") {
    Copy-Item "./README.md" -Destination "$zipFolder/README.txt" -Force
}
if (Test-Path "./LICENSE") {
    Copy-Item "./LICENSE" -Destination "$zipFolder/LICENSE.txt" -Force
}

# Optional: run.bat
Set-Content -Path "$zipFolder/run.bat" -Value "@echo off`nOggify.exe"

# Create zip
Compress-Archive -Path "$zipFolder/*" -DestinationPath $zipFile

Write-Host "Release package created at: $zipFile"