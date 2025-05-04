# This script prepares the library for release. 
# It takes a single parameter: the version number.
# Usage: .\save_library.ps1 -version "1.0.0"
# It: 
# - removes the TcUnit reference from the project
# - sets the version number
# - sets the release flag
# - saves & installs the library

param (
    [string]$version
)

if (-not $version) {
    Write-Host "Error: Version argument is required."
    exit 1
}

$ErrorActionPreference = "Stop"

$plcProjectSourceDir = "C:\repos\TcLog\src\TcLogProj"
$plcProjectTargetDir = "C:\TcLogLibraryRelease\TcLogProj"
$plcProjectPath = "C:\TcLogLibraryRelease\TcLogProj\TcLogProj.tsproj"

$slnDir = "C:\TcLogLibraryRelease"
$slnName = "TcLogLibRelease"

$librarySavePath = Join-Path -Path (Get-Location) -ChildPath "..\library\TcLog.library"

# Copy the PLC project files to the target directory
if (-not (Test-Path $plcProjectTargetDir)) {
    New-Item -ItemType Directory -Path $plcProjectTargetDir | Out-Null
}
Write-Host "Copying files from $plcProjectSourceDir to $plcProjectTargetDir..."
Copy-Item -Path "$plcProjectSourceDir\*" -Destination $plcProjectTargetDir -Recurse -Force
Write-Host "Files copied successfully."

if ($null -eq $plcProjectPath -or -not (Test-Path $plcProjectPath)) {
    Write-Host "Error: PLC project path does not exist or is undefined: $plcProjectPath"
    exit 1
}

# Open Visual Studio and create a new solution
$dte = new-object -com TcXaeShell.DTE.17.0
try {
    $dte.SuppressUI = $false
    $dte.MainWindow.Visible = $true
    $solution = $dte.Solution
    Write-Host "Creating new solution..."

    # !! Variable assignments are needed to avoid some weird PS error !!
    $a = $solution.Create($slnDir, $slnName)
    Write-Host "Adding PLC project to solution..."
    $a = $solution.AddFromFile($plcProjectPath)
    Write-Host "Solution created and PLC project added successfully."

    $project = $solution.Projects.Item(1)

    # Remove the reference to TcUnit from the project
    Write-Host "Removing reference to TcUnit from the project..."
    $a = $project.Object.LookupTreeItem("TIPC^TcLog^TcLog Projekt^References").RemoveReference("TcUnit")

    # Set the version number
    $xml = "<TreeItem><IECProjectDef><ProjectInfo><Version>$version</Version></ProjectInfo></IECProjectDef></TreeItem>"
    $a = $project.Object.LookupTreeItem("TIPC^TcLog^TcLog Projekt").ConsumeXml($xml)

    # Set the release flag
    $xml = "<TreeItem><IECProjectDef><ProjectInfo><Released>true</Released></ProjectInfo></IECProjectDef></TreeItem>"
    $a = $project.Object.LookupTreeItem("TIPC^TcLog^TcLog Projekt").ConsumeXml($xml)

    # Save as library
    Write-Host "Saving PLC project as library..."
    $a = $project.Object.LookupTreeItem("TIPC^TcLog^TcLog Projekt").SaveAsLibrary($librarySavePath, $true)
    Write-Host "PLC project saved as library successfully."

    # Close the project
    Write-Host "Closing the project."
    $a = $dte.Quit()

    # Delete the project files
    Write-Host "Deleting the project files..."
    Remove-Item -Path "$plcProjectTargetDir\*" -Recurse -Force
    Write-Host "Project files deleted successfully."
}
catch {
    Write-Host "An error occurred: $_"
    $dte.Quit()
}
