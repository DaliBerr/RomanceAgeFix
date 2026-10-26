param(
    [ValidateSet('1.5','1.6','1.7','All')] [string]$Version = 'All',
    [string]$Configuration = 'Release',
    [switch]$Deploy,
    [string]$RimWorldPath = 'C:\Program Files (x86)\Steam\steamapps\common\RimWorld'
)
$ErrorActionPreference = 'Stop'
$versions = if ($Version -eq 'All') { @('1.5','1.6','1.7') } else { @($Version) }
foreach ($modVersion in $versions) {
    & dotnet build "$PSScriptRoot\$modVersion\Source\RomanceAgeFix.csproj" -c $Configuration "-p:RimWorldPath=$RimWorldPath" --nologo
    if ($LASTEXITCODE -ne 0) { throw "RimWorld $modVersion build failed." }
}
if ($Deploy) {
    if ($Version -ne 'All') { throw 'Deploy requires -Version All.' }
    $gameRoot = (Resolve-Path -LiteralPath $RimWorldPath).Path
    $modsRoot = [IO.Path]::GetFullPath((Join-Path $gameRoot 'Mods'))
    $destination = [IO.Path]::GetFullPath((Join-Path $modsRoot 'RomanceAgeFix'))
    $backupRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot 'Build\backups'))
    if (!$destination.StartsWith($modsRoot + '\', [StringComparison]::OrdinalIgnoreCase) -or
        !$backupRoot.StartsWith($PSScriptRoot + '\', [StringComparison]::OrdinalIgnoreCase)) { throw 'Unexpected deployment path.' }
    $backup = $null
    if (Test-Path -LiteralPath $destination) {
        New-Item -ItemType Directory -Path $backupRoot -Force | Out-Null
        $backup = Join-Path $backupRoot ('installed-' + (Get-Date -Format 'yyyyMMdd-HHmmss-fff'))
        Move-Item -LiteralPath $destination -Destination $backup
        Write-Host "Previous installation backed up to $backup"
    }
    try {
        New-Item -ItemType Directory -Path $destination -Force | Out-Null
        foreach ($folder in @('About','Common','Textures')) {
            $source = Join-Path $PSScriptRoot $folder
            if (Test-Path -LiteralPath $source) { Copy-Item -LiteralPath $source -Destination $destination -Recurse }
        }
        Copy-Item -LiteralPath "$PSScriptRoot\LoadFolders.xml" -Destination $destination
        if ($backup -and !(Test-Path -LiteralPath "$destination\About\PublishedFileId.txt") -and
            (Test-Path -LiteralPath "$backup\About\PublishedFileId.txt")) {
            Copy-Item -LiteralPath "$backup\About\PublishedFileId.txt" -Destination "$destination\About\PublishedFileId.txt"
        }
        foreach ($modVersion in $versions) {
            $assemblies = Join-Path $destination "$modVersion\Assemblies"
            New-Item -ItemType Directory -Path $assemblies -Force | Out-Null
            Copy-Item -LiteralPath "$PSScriptRoot\$modVersion\Assemblies\RomanceAgeFix.dll" -Destination $assemblies
        }
    }
    catch {
        if ($backup -and (Test-Path -LiteralPath $backup)) {
            if (Test-Path -LiteralPath $destination) { Move-Item -LiteralPath $destination -Destination ($backup + '-failed') }
            Move-Item -LiteralPath $backup -Destination $destination
        }
        throw
    }
    Write-Host "Deployed to $destination"
}
