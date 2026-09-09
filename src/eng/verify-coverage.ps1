param(
    [Parameter()]
    [string]$ResultsDirectory = (Join-Path $PSScriptRoot '..\TestResults')
)

$ErrorActionPreference = 'Stop'

# Discover shipping assemblies so adding a package cannot silently bypass this gate.
$sourceDirectory = Split-Path -Parent $PSScriptRoot
$expectedModules = @(
    foreach ($projectDirectory in Get-ChildItem -LiteralPath $sourceDirectory -Directory -Filter 'MQTTnet.Rx.*') {
        $projectPath = Join-Path $projectDirectory.FullName "$($projectDirectory.Name).csproj"
        if (-not (Test-Path -LiteralPath $projectPath)) {
            continue
        }

        [xml]$project = Get-Content -Raw -LiteralPath $projectPath
        if (@($project.SelectNodes('/Project/PropertyGroup/IsPackable')) | Where-Object { $_.InnerText -eq 'false' }) {
            continue
        }

        $assemblyNames = @($project.SelectNodes('/Project/PropertyGroup/AssemblyName'))
        if ($assemblyNames.Count -eq 0) {
            $projectDirectory.Name
        }
        elseif ($assemblyNames.Count -eq 1 -and $assemblyNames[0].InnerText -notmatch '\$\(') {
            $assemblyNames[0].InnerText
        }
        else {
            throw "Cannot determine the shipping assembly name from '$projectPath'."
        }
    }
) | Sort-Object -Unique
if ($expectedModules.Count -eq 0) {
    throw "No shipping projects were found under '$sourceDirectory'."
}

$resolvedResultsDirectory = Resolve-Path -LiteralPath $ResultsDirectory -ErrorAction Stop
$coverageFiles = @(Get-ChildItem -LiteralPath $resolvedResultsDirectory -Recurse -Filter '*.cobertura.xml' -File)
if ($coverageFiles.Count -eq 0) {
    throw "No Cobertura reports were found under '$resolvedResultsDirectory'."
}

$observations = @{}
foreach ($coverageFile in $coverageFiles) {
    [xml]$coverage = Get-Content -Raw -LiteralPath $coverageFile.FullName
    foreach ($package in @($coverage.coverage.packages.package)) {
        $moduleName = [string]$package.name
        if ($moduleName -notin $expectedModules) {
            continue
        }

        if (-not $observations.ContainsKey($moduleName)) {
            $observations[$moduleName] = [System.Collections.Generic.List[object]]::new()
        }

        $observations[$moduleName].Add([pscustomobject]@{
            File = $coverageFile.FullName
            LineRate = [decimal]$package.'line-rate'
            BranchRate = [decimal]$package.'branch-rate'
            # Check individual entries as well: rounded aggregate rates can conceal misses.
            MissedLines = @($package.SelectNodes('classes/class/lines/line[@hits="0"]')).Count
            MissedBranches = @($package.SelectNodes('classes/class/lines/line[@branch="true"]') |
                Where-Object { $_.'condition-coverage' -notmatch '^100%' }).Count
        })
    }
}

$failures = [System.Collections.Generic.List[string]]::new()
foreach ($moduleName in $expectedModules) {
    if (-not $observations.ContainsKey($moduleName)) {
        $failures.Add("Missing coverage module: $moduleName")
        continue
    }

    foreach ($observation in $observations[$moduleName]) {
        if ($observation.LineRate -lt 1 -or $observation.BranchRate -lt 1 -or
            $observation.MissedLines -gt 0 -or $observation.MissedBranches -gt 0) {
            $failures.Add(
                "$moduleName is below 100% in '$($observation.File)': " +
                "line=$($observation.LineRate), branch=$($observation.BranchRate), " +
                "missed lines=$($observation.MissedLines), missed branch lines=$($observation.MissedBranches)")
        }
    }
}

if ($failures.Count -gt 0) {
    throw "Coverage verification failed with $($failures.Count) error(s):`n$($failures -join "`n")"
}

foreach ($moduleName in $expectedModules) {
    Write-Host "${moduleName}: 100% line / 100% branch"
}

Write-Host "Coverage verification passed for all $($expectedModules.Count) production modules."
