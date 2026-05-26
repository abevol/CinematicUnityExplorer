# Script to build selected solution release configurations.
#
# Build the solution-level Release_* configurations instead of invoking
# UnityExplorer.csproj directly. The solution maps these release configurations
# to both UnityExplorer.csproj and UniverseLib project configurations.

Push-Location $PSScriptRoot

$configurations = @(
    "Release_BIE_CoreCLR",
    "Release_BIE5_Mono",
    "Release_BIE6_Mono",
    "Release_ML_Cpp_net6",
    "Release_ML_Cpp_net6_interop",
    "Release_ML_Mono",
    "Release_STANDALONE_Mono"
)

foreach ($config in $configurations) {
    Write-Host "============================================="
    Write-Host "Building configuration: $($config)"
    Write-Host "============================================="

    dotnet build src/UnityExplorer.sln -c $($config)

    if ($LASTEXITCODE -ne 0) {
        Write-Host "---------------------------------------------"
        Write-Host "Build FAILED for configuration: $($config)" -ForegroundColor Red
        Write-Host "---------------------------------------------"
    } else {
        Write-Host "---------------------------------------------"
        Write-Host "Build SUCCESSFUL for configuration: $($config)" -ForegroundColor Green
        Write-Host "---------------------------------------------"
    }

    Write-Host "" # Add a blank line for readability
}

Pop-Location

Write-Host "Finished building all configurations."
