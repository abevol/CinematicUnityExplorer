param(
    [switch]$Help,

    [ValidateSet(
        'ML_Cpp_net6', 'ML_Cpp_CoreCLR', 'ML_Cpp_net472', 'ML_Mono',
        'BIE_Cpp', 'BIE_CoreCLR', 'BIE_Unity_Cpp',
        'BIE5_Mono', 'BIE6_Mono', 'BIE6_Unity_Mono',
        'STANDALONE_Mono', 'STANDALONE_Cpp', 'Editor'
    )]
    [string[]]$Variant
)

$allVariants = @(
    'ML_Cpp_net6', 'ML_Cpp_CoreCLR', 'ML_Cpp_net472', 'ML_Mono',
    'BIE_Cpp', 'BIE_CoreCLR', 'BIE_Unity_Cpp',
    'BIE5_Mono', 'BIE6_Mono', 'BIE6_Unity_Mono',
    'STANDALONE_Mono', 'STANDALONE_Cpp', 'Editor'
)

function PrintHelp {
    Write-Host @"
Usage: .\build.ps1 [[-Variant] <name[]>] [-Help]

Build variants:
  ML_Cpp_net6         MelonLoader IL2CPP (net6)
  ML_Cpp_CoreCLR      MelonLoader IL2CPP CoreCLR (net6)
  ML_Cpp_net472       MelonLoader IL2CPP (net472)
  ML_Mono             MelonLoader Mono
  BIE_Cpp             BepInEx IL2CPP
  BIE_CoreCLR         BepInEx IL2CPP CoreCLR
  BIE_Unity_Cpp       BepInEx Unity IL2CPP CoreCLR
  BIE5_Mono           BepInEx 5 Mono
  BIE6_Mono           BepInEx 6 Mono
  BIE6_Unity_Mono     BepInEx 6 Unity Mono
  STANDALONE_Mono     Standalone Mono
  STANDALONE_Cpp      Standalone IL2CPP
  Editor              Editor package (copies from STANDALONE_Mono)

Examples:
  .\build.ps1                        Build all variants
  .\build.ps1 -Variant BIE6_Unity_Mono
  .\build.ps1 -Variant BIE6_Mono,BIE5_Mono

Note: UniverseLib is always built first via its own build.ps1.
"@
}

if ($Help) {
    PrintHelp
    exit 0
}

if ($Variant) {
    $invalid = $Variant | Where-Object { $_ -notin $allVariants }
    if ($invalid) {
        Write-Error "Unknown variant(s): $($invalid -join ', '). Valid values: $($allVariants -join ', ')"
        exit 1
    }
}

function ShouldBuild([string]$name) {
    return (-not $Variant) -or ($Variant -contains $name)
}

cd UniverseLib
.\build.ps1
cd ..

# ----------- MelonLoader IL2CPP (net6) -----------
if (ShouldBuild 'ML_Cpp_net6') {
    dotnet build src/CinematicUnityExplorer.sln -c Release_ML_Cpp_net6preview
    $Path = "Release\CinematicUnityExplorer.MelonLoader.IL2CPP.net6preview"
    lib/ILRepack.exe /target:library /lib:lib/net6 /lib:lib/unhollowed /lib:$Path /internalize /out:$Path/CinematicUnityExplorer.ML.IL2CPP.net6preview.dll $Path/CinematicUnityExplorer.ML.IL2CPP.net6preview.dll $Path/mcs.dll
    Remove-Item $Path/CinematicUnityExplorer.ML.IL2CPP.net6preview.deps.json
    Remove-Item $Path/Tomlet.dll
    Remove-Item $Path/mcs.dll
    Remove-Item $Path/Iced.dll
    Remove-Item $Path/UnhollowerBaseLib.dll
    New-Item -Path "$Path" -Name "Mods" -ItemType "directory" -Force
    Move-Item -Path $Path/CinematicUnityExplorer.ML.IL2CPP.net6preview.dll -Destination $Path/Mods -Force
    Move-Item -Path $Path/CinematicUnityExplorer.ML.IL2CPP.net6preview.pdb -Destination $Path/Mods -Force
    New-Item -Path "$Path" -Name "UserLibs" -ItemType "directory" -Force
    Move-Item -Path $Path/UniverseLib.IL2CPP.Unhollower.dll -Destination $Path/UserLibs -Force
    Copy-Item UniverseLib/Release/UniverseLib.Il2Cpp.Unhollower/UniverseLib.IL2CPP.Unhollower.pdb -Destination $Path/UserLibs -Force
    Remove-Item $Path/../CinematicUnityExplorer.MelonLoader.IL2CPP.net6preview.zip -ErrorAction SilentlyContinue
    compress-archive .\$Path\* $Path/../CinematicUnityExplorer.MelonLoader.IL2CPP.net6preview.zip
}

# ----------- MelonLoader IL2CPP CoreCLR (net6) -----------
if (ShouldBuild 'ML_Cpp_CoreCLR') {
    dotnet build src/CinematicUnityExplorer.sln -c Release_ML_Cpp_CoreCLR
    $Path = "Release\CinematicUnityExplorer.MelonLoader.IL2CPP.CoreCLR"
    lib/ILRepack.exe /target:library /lib:lib/net6 /lib:lib/interop /lib:$Path /internalize /out:$Path/CinematicUnityExplorer.ML.IL2CPP.CoreCLR.dll $Path/CinematicUnityExplorer.ML.IL2CPP.CoreCLR.dll $Path/mcs.dll
    Remove-Item $Path/CinematicUnityExplorer.ML.IL2CPP.CoreCLR.deps.json
    Remove-Item $Path/Tomlet.dll
    Remove-Item $Path/mcs.dll
    Remove-Item $Path/Iced.dll
    Remove-Item $Path/Il2CppInterop.Common.dll
    Remove-Item $Path/Il2CppInterop.Runtime.dll
    Remove-Item $Path/Microsoft.Extensions.Logging.Abstractions.dll
    New-Item -Path "$Path" -Name "Mods" -ItemType "directory" -Force
    Move-Item -Path $Path/CinematicUnityExplorer.ML.IL2CPP.CoreCLR.dll -Destination $Path/Mods -Force
    Move-Item -Path $Path/CinematicUnityExplorer.ML.IL2CPP.CoreCLR.pdb -Destination $Path/Mods -Force
    New-Item -Path "$Path" -Name "UserLibs" -ItemType "directory" -Force
    Move-Item -Path $Path/UniverseLib.ML.IL2CPP.Interop.dll -Destination $Path/UserLibs -Force
    Copy-Item UniverseLib/Release/UniverseLib.Il2Cpp.Interop/UniverseLib.ML.IL2CPP.Interop.pdb -Destination $Path/UserLibs -Force
    Remove-Item $Path/../CinematicUnityExplorer.MelonLoader.IL2CPP.CoreCLR.zip -ErrorAction SilentlyContinue
    compress-archive .\$Path\* $Path/../CinematicUnityExplorer.MelonLoader.IL2CPP.CoreCLR.zip
}

# ----------- MelonLoader IL2CPP (net472) -----------
if (ShouldBuild 'ML_Cpp_net472') {
    dotnet build src/CinematicUnityExplorer.sln -c Release_ML_Cpp_net472
    $Path = "Release/CinematicUnityExplorer.MelonLoader.IL2CPP"
    lib/ILRepack.exe /target:library /lib:lib/net472 /lib:lib/net35 /lib:lib/unhollowed /lib:$Path /internalize /out:$Path/CinematicUnityExplorer.ML.IL2CPP.dll $Path/CinematicUnityExplorer.ML.IL2CPP.dll $Path/mcs.dll
    Remove-Item $Path/Tomlet.dll
    Remove-Item $Path/mcs.dll
    Remove-Item $Path/Iced.dll
    Remove-Item $Path/UnhollowerBaseLib.dll
    New-Item -Path "$Path" -Name "Mods" -ItemType "directory" -Force
    Move-Item -Path $Path/CinematicUnityExplorer.ML.IL2CPP.dll -Destination $Path/Mods -Force
    Move-Item -Path $Path/CinematicUnityExplorer.ML.IL2CPP.pdb -Destination $Path/Mods -Force
    New-Item -Path "$Path" -Name "UserLibs" -ItemType "directory" -Force
    Move-Item -Path $Path/UniverseLib.IL2CPP.Unhollower.dll -Destination $Path/UserLibs -Force
    Copy-Item UniverseLib/Release/UniverseLib.Il2Cpp.Unhollower/UniverseLib.IL2CPP.Unhollower.pdb -Destination $Path/UserLibs -Force
    Remove-Item $Path/../CinematicUnityExplorer.MelonLoader.IL2CPP.zip -ErrorAction SilentlyContinue
    compress-archive .\$Path\* $Path/../CinematicUnityExplorer.MelonLoader.IL2CPP.zip
}

# ----------- MelonLoader Mono -----------
if (ShouldBuild 'ML_Mono') {
    dotnet build src/CinematicUnityExplorer.sln -c Release_ML_Mono
    $Path = "Release/CinematicUnityExplorer.MelonLoader.Mono"
    lib/ILRepack.exe /target:library /lib:lib/net35 /lib:$Path /internalize /out:$Path/CinematicUnityExplorer.ML.Mono.dll $Path/CinematicUnityExplorer.ML.Mono.dll $Path/mcs.dll
    Remove-Item $Path/Tomlet.dll
    Remove-Item $Path/mcs.dll
    New-Item -Path "$Path" -Name "Mods" -ItemType "directory" -Force
    Move-Item -Path $Path/CinematicUnityExplorer.ML.Mono.dll -Destination $Path/Mods -Force
    Move-Item -Path $Path/CinematicUnityExplorer.ML.Mono.pdb -Destination $Path/Mods -Force
    New-Item -Path "$Path" -Name "UserLibs" -ItemType "directory" -Force
    Move-Item -Path $Path/UniverseLib.Mono.dll -Destination $Path/UserLibs -Force
    Copy-Item UniverseLib/Release/UniverseLib.Mono/UniverseLib.Mono.pdb -Destination $Path/UserLibs -Force
    Remove-Item $Path/../CinematicUnityExplorer.MelonLoader.Mono.zip -ErrorAction SilentlyContinue
    compress-archive .\$Path\* $Path/../CinematicUnityExplorer.MelonLoader.Mono.zip
}

# ----------- BepInEx IL2CPP -----------
if (ShouldBuild 'BIE_Cpp') {
    dotnet build src/CinematicUnityExplorer.sln -c Release_BIE_Cpp
    $Path = "Release/CinematicUnityExplorer.BepInEx.IL2CPP"
    lib/ILRepack.exe /target:library /lib:lib/net472/BepInEx/build423~577 /lib:lib/unhollowed /lib:$Path /internalize /out:$Path/CinematicUnityExplorer.BIE.IL2CPP.dll $Path/CinematicUnityExplorer.BIE.IL2CPP.dll $Path/mcs.dll $Path/Tomlet.dll
    Remove-Item $Path/Tomlet.dll
    Remove-Item $Path/mcs.dll
    Remove-Item $Path/Iced.dll
    Remove-Item $Path/UnhollowerBaseLib.dll
    New-Item -Path "$Path" -Name "plugins" -ItemType "directory" -Force
    New-Item -Path "$Path" -Name "plugins/CinematicUnityExplorer" -ItemType "directory" -Force
    Move-Item -Path $Path/CinematicUnityExplorer.BIE.IL2CPP.dll -Destination $Path/plugins/CinematicUnityExplorer -Force
    Move-Item -Path $Path/CinematicUnityExplorer.BIE.IL2CPP.pdb -Destination $Path/plugins/CinematicUnityExplorer -Force
    Move-Item -Path $Path/UniverseLib.IL2CPP.Unhollower.dll -Destination $Path/plugins/CinematicUnityExplorer -Force
    Copy-Item UniverseLib/Release/UniverseLib.Il2Cpp.Unhollower/UniverseLib.IL2CPP.Unhollower.pdb -Destination $Path/plugins/CinematicUnityExplorer -Force
    Remove-Item $Path/../CinematicUnityExplorer.BepInEx.IL2CPP.zip -ErrorAction SilentlyContinue
    compress-archive .\$Path\* $Path/../CinematicUnityExplorer.BepInEx.IL2CPP.zip
}

# ----------- BepInEx IL2CPP CoreCLR -----------
if (ShouldBuild 'BIE_CoreCLR') {
    dotnet build src/CinematicUnityExplorer.sln -c Release_BIE_CoreCLR
    $Path = "Release/CinematicUnityExplorer.BepInEx.IL2CPP.CoreCLR"
    lib/ILRepack.exe /target:library /lib:lib/net472/BepInEx/build423~577 /lib:lib/net6/ /lib:lib/interop/ /lib:$Path /internalize /out:$Path/CinematicUnityExplorer.BIE.IL2CPP.CoreCLR.dll $Path/CinematicUnityExplorer.BIE.IL2CPP.CoreCLR.dll $Path/mcs.dll $Path/Tomlet.dll
    Remove-Item $Path/Tomlet.dll
    Remove-Item $Path/mcs.dll
    Remove-Item $Path/Iced.dll
    Remove-Item $Path/Il2CppInterop.Common.dll
    Remove-Item $Path/Il2CppInterop.Runtime.dll
    Remove-Item $Path/Microsoft.Extensions.Logging.Abstractions.dll
    Remove-Item $Path/CinematicUnityExplorer.BIE.IL2CPP.CoreCLR.deps.json
    New-Item -Path "$Path" -Name "plugins" -ItemType "directory" -Force
    New-Item -Path "$Path" -Name "plugins/CinematicUnityExplorer" -ItemType "directory" -Force
    Move-Item -Path $Path/CinematicUnityExplorer.BIE.IL2CPP.CoreCLR.dll -Destination $Path/plugins/CinematicUnityExplorer -Force
    Move-Item -Path $Path/CinematicUnityExplorer.BIE.IL2CPP.CoreCLR.pdb -Destination $Path/plugins/CinematicUnityExplorer -Force
    Move-Item -Path $Path/UniverseLib.BIE.IL2CPP.Interop.dll -Destination $Path/plugins/CinematicUnityExplorer -Force
    Copy-Item UniverseLib/Release/UniverseLib.Il2Cpp.Interop/UniverseLib.BIE.IL2CPP.Interop.pdb -Destination $Path/plugins/CinematicUnityExplorer -Force
    Remove-Item $Path/../CinematicUnityExplorer.BepInEx.IL2CPP.CoreCLR.zip -ErrorAction SilentlyContinue
    compress-archive .\$Path\* $Path/../CinematicUnityExplorer.BepInEx.IL2CPP.CoreCLR.zip
}

# ----------- BepInEx Unity IL2CPP CoreCLR -----------
if (ShouldBuild 'BIE_Unity_Cpp') {
    dotnet build src/CinematicUnityExplorer.sln -c Release_BIE_Unity_Cpp
    $Path = "Release/CinematicUnityExplorer.BepInEx.Unity.IL2CPP.CoreCLR"
    lib/ILRepack.exe /target:library /lib:lib/net472/BepInEx/build647+ /lib:lib/net6/ /lib:lib/interop/ /lib:$Path /internalize /out:$Path/CinematicUnityExplorer.BIE.Unity.IL2CPP.CoreCLR.dll $Path/CinematicUnityExplorer.BIE.Unity.IL2CPP.CoreCLR.dll $Path/mcs.dll $Path/Tomlet.dll
    Remove-Item $Path/Tomlet.dll
    Remove-Item $Path/mcs.dll
    Remove-Item $Path/Iced.dll
    Remove-Item $Path/Il2CppInterop.Common.dll
    Remove-Item $Path/Il2CppInterop.Runtime.dll
    Remove-Item $Path/Microsoft.Extensions.Logging.Abstractions.dll
    Remove-Item $Path/CinematicUnityExplorer.BIE.Unity.IL2CPP.CoreCLR.deps.json
    New-Item -Path "$Path" -Name "plugins" -ItemType "directory" -Force
    New-Item -Path "$Path" -Name "plugins/CinematicUnityExplorer" -ItemType "directory" -Force
    Move-Item -Path $Path/CinematicUnityExplorer.BIE.Unity.IL2CPP.CoreCLR.dll -Destination $Path/plugins/CinematicUnityExplorer -Force
    Move-Item -Path $Path/CinematicUnityExplorer.BIE.Unity.IL2CPP.CoreCLR.pdb -Destination $Path/plugins/CinematicUnityExplorer -Force
    Move-Item -Path $Path/UniverseLib.BIE.IL2CPP.Interop.dll -Destination $Path/plugins/CinematicUnityExplorer -Force
    Copy-Item UniverseLib/Release/UniverseLib.Il2Cpp.Interop/UniverseLib.BIE.IL2CPP.Interop.pdb -Destination $Path/plugins/CinematicUnityExplorer -Force
    Remove-Item $Path/../CinematicUnityExplorer.BepInEx.Unity.IL2CPP.CoreCLR.zip -ErrorAction SilentlyContinue
    compress-archive .\$Path\* $Path/../CinematicUnityExplorer.BepInEx.Unity.IL2CPP.CoreCLR.zip
}

# ----------- BepInEx 5 Mono -----------
if (ShouldBuild 'BIE5_Mono') {
    dotnet build src/CinematicUnityExplorer.sln -c Release_BIE5_Mono
    $Path = "Release/CinematicUnityExplorer.BepInEx5.Mono"
    lib/ILRepack.exe /target:library /lib:lib/net35 /lib:lib/net35/BepInEx /lib:$Path /internalize /out:$Path/CinematicUnityExplorer.BIE5.Mono.dll $Path/CinematicUnityExplorer.BIE5.Mono.dll $Path/mcs.dll $Path/Tomlet.dll
    Remove-Item $Path/Tomlet.dll
    Remove-Item $Path/mcs.dll
    New-Item -Path "$Path" -Name "plugins" -ItemType "directory" -Force
    New-Item -Path "$Path" -Name "plugins/CinematicUnityExplorer" -ItemType "directory" -Force
    Move-Item -Path $Path/CinematicUnityExplorer.BIE5.Mono.dll -Destination $Path/plugins/CinematicUnityExplorer -Force
    Move-Item -Path $Path/CinematicUnityExplorer.BIE5.Mono.pdb -Destination $Path/plugins/CinematicUnityExplorer -Force
    Move-Item -Path $Path/UniverseLib.Mono.dll -Destination $Path/plugins/CinematicUnityExplorer -Force
    Copy-Item UniverseLib/Release/UniverseLib.Mono/UniverseLib.Mono.pdb -Destination $Path/plugins/CinematicUnityExplorer -Force
    Remove-Item $Path/../CinematicUnityExplorer.BepInEx5.Mono.zip -ErrorAction SilentlyContinue
    compress-archive .\$Path\* $Path/../CinematicUnityExplorer.BepInEx5.Mono.zip
}

# ----------- BepInEx 6 Mono -----------
if (ShouldBuild 'BIE6_Mono') {
    dotnet build src/CinematicUnityExplorer.sln -c Release_BIE6_Mono
    $Path = "Release/CinematicUnityExplorer.BepInEx6.Mono"
    lib/ILRepack.exe /target:library /lib:lib/net35 /lib:lib/net35/BepInEx/build423~577 /lib:$Path /internalize /out:$Path/CinematicUnityExplorer.BIE6.Mono.dll $Path/CinematicUnityExplorer.BIE6.Mono.dll $Path/mcs.dll $Path/Tomlet.dll
    Remove-Item $Path/Tomlet.dll
    Remove-Item $Path/mcs.dll
    New-Item -Path "$Path" -Name "plugins" -ItemType "directory" -Force
    New-Item -Path "$Path" -Name "plugins/CinematicUnityExplorer" -ItemType "directory" -Force
    Move-Item -Path $Path/CinematicUnityExplorer.BIE6.Mono.dll -Destination $Path/plugins/CinematicUnityExplorer -Force
    Move-Item -Path $Path/CinematicUnityExplorer.BIE6.Mono.pdb -Destination $Path/plugins/CinematicUnityExplorer -Force
    Move-Item -Path $Path/UniverseLib.Mono.dll -Destination $Path/plugins/CinematicUnityExplorer -Force
    Copy-Item UniverseLib/Release/UniverseLib.Mono/UniverseLib.Mono.pdb -Destination $Path/plugins/CinematicUnityExplorer -Force
    Remove-Item $Path/../CinematicUnityExplorer.BepInEx6.Mono.zip -ErrorAction SilentlyContinue
    compress-archive .\$Path\* $Path/../CinematicUnityExplorer.BepInEx6.Mono.zip
}

# ----------- BepInEx 6 Unity Mono -----------
if (ShouldBuild 'BIE6_Unity_Mono') {
    dotnet build src/CinematicUnityExplorer.sln -c Release_BIE6_Unity_Mono
    $Path = "Release/CinematicUnityExplorer.BepInEx6.Unity.Mono"
    lib/ILRepack.exe /target:library /lib:lib/net35 /lib:lib/net35/BepInEx/build647+ /lib:$Path /internalize /out:$Path/CinematicUnityExplorer.BIE6.Unity.Mono.dll $Path/CinematicUnityExplorer.BIE6.Unity.Mono.dll $Path/mcs.dll $Path/Tomlet.dll
    Remove-Item $Path/Tomlet.dll
    Remove-Item $Path/mcs.dll
    New-Item -Path "$Path" -Name "plugins" -ItemType "directory" -Force
    New-Item -Path "$Path" -Name "plugins/CinematicUnityExplorer" -ItemType "directory" -Force
    Move-Item -Path $Path/CinematicUnityExplorer.BIE6.Unity.Mono.dll -Destination $Path/plugins/CinematicUnityExplorer -Force
    Move-Item -Path $Path/CinematicUnityExplorer.BIE6.Unity.Mono.pdb -Destination $Path/plugins/CinematicUnityExplorer -Force
    Move-Item -Path $Path/UniverseLib.Mono.dll -Destination $Path/plugins/CinematicUnityExplorer -Force
    Copy-Item UniverseLib/Release/UniverseLib.Mono/UniverseLib.Mono.pdb -Destination $Path/plugins/CinematicUnityExplorer -Force
    Remove-Item $Path/../CinematicUnityExplorer.BepInEx6.Unity.Mono.zip -ErrorAction SilentlyContinue
    compress-archive .\$Path\* $Path/../CinematicUnityExplorer.BepInEx6.Unity.Mono.zip
}

# ----------- Standalone Mono -----------
if (ShouldBuild 'STANDALONE_Mono') {
    dotnet build src/CinematicUnityExplorer.sln -c Release_STANDALONE_Mono
    $Path = "Release/CinematicUnityExplorer.Standalone.Mono"
    lib/ILRepack.exe /target:library /lib:lib/net35 /lib:$Path /internalize /out:$Path/CinematicUnityExplorer.Standalone.Mono.dll $Path/CinematicUnityExplorer.Standalone.Mono.dll $Path/mcs.dll $Path/Tomlet.dll
    Remove-Item $Path/Tomlet.dll
    Remove-Item $Path/mcs.dll
    Copy-Item UniverseLib/Release/UniverseLib.Mono/UniverseLib.Mono.pdb -Destination $Path -Force
    Remove-Item $Path/../CinematicUnityExplorer.Standalone.Mono.zip -ErrorAction SilentlyContinue
    compress-archive .\$Path\* $Path/../CinematicUnityExplorer.Standalone.Mono.zip
}

# ----------- Standalone IL2CPP -----------
if (ShouldBuild 'STANDALONE_Cpp') {
    dotnet build src/CinematicUnityExplorer.sln -c Release_STANDALONE_Cpp
    $Path = "Release/CinematicUnityExplorer.Standalone.IL2CPP"
    lib/ILRepack.exe /target:library /lib:lib/net472 /lib:lib/unhollowed /lib:$Path /internalize /out:$Path/CinematicUnityExplorer.Standalone.IL2CPP.dll $Path/CinematicUnityExplorer.Standalone.IL2CPP.dll $Path/mcs.dll $Path/Tomlet.dll
    Remove-Item $Path/Tomlet.dll
    Remove-Item $Path/mcs.dll
    Remove-Item $Path/Iced.dll
    Remove-Item $Path/UnhollowerBaseLib.dll
    Copy-Item UniverseLib/Release/UniverseLib.Il2Cpp.Unhollower/UniverseLib.IL2CPP.Unhollower.pdb -Destination $Path -Force
    Remove-Item $Path/../CinematicUnityExplorer.Standalone.IL2CPP.zip -ErrorAction SilentlyContinue
    compress-archive .\$Path\* $Path/../CinematicUnityExplorer.Standalone.IL2CPP.zip
}

# ----------- Editor (mono) -----------
if (ShouldBuild 'Editor') {
    if (-not (ShouldBuild 'STANDALONE_Mono')) {
        Write-Warning "Editor depends on STANDALONE_Mono, building it first..."
        dotnet build src/CinematicUnityExplorer.sln -c Release_STANDALONE_Mono
    }
    $Path1 = "Release/CinematicUnityExplorer.Standalone.Mono"
    $Path2 = "UnityEditorPackage/Runtime"
    Copy-Item $Path1/CinematicUnityExplorer.STANDALONE.Mono.dll -Destination $Path2
    Copy-Item $Path1/CinematicUnityExplorer.STANDALONE.Mono.pdb -Destination $Path2
    Copy-Item $Path1/UniverseLib.Mono.dll -Destination $Path2
    Copy-Item $Path1/UniverseLib.Mono.pdb -Destination $Path2
    Remove-Item Release/CinematicUnityExplorer.Editor.zip -ErrorAction SilentlyContinue
    compress-archive .\UnityEditorPackage\*  Release/CinematicUnityExplorer.Editor.zip
}
