param(
    [Parameter()]
    [string]$ResultsDirectory = (Join-Path $PSScriptRoot '..\TestResults')
)

$ErrorActionPreference = 'Stop'

$expectedModules = @(
    'MQTTnet.Rx.ABPlc',
    'MQTTnet.Rx.ABPlc.Reactive',
    'MQTTnet.Rx.AspNetCore',
    'MQTTnet.Rx.AspNetCore.Reactive',
    'MQTTnet.Rx.Client',
    'MQTTnet.Rx.Client.Reactive',
    'MQTTnet.Rx.Mitsubishi',
    'MQTTnet.Rx.Mitsubishi.Reactive',
    'MQTTnet.Rx.Modbus',
    'MQTTnet.Rx.Modbus.Reactive',
    'MQTTnet.Rx.OmronPlc',
    'MQTTnet.Rx.OmronPlc.Reactive',
    'MQTTnet.Rx.S7Plc',
    'MQTTnet.Rx.S7Plc.Reactive',
    'MQTTnet.Rx.SerialPort',
    'MQTTnet.Rx.SerialPort.Reactive',
    'MQTTnet.Rx.Server',
    'MQTTnet.Rx.Server.Reactive',
    'MQTTnet.Rx.TwinCAT',
    'MQTTnet.Rx.TwinCAT.Reactive'
)

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
        if ($observation.LineRate -lt 1 -or $observation.BranchRate -lt 1) {
            $failures.Add(
                "$moduleName is below 100% in '$($observation.File)': " +
                "line=$($observation.LineRate), branch=$($observation.BranchRate)")
        }
    }
}

if ($failures.Count -gt 0) {
    $failures | ForEach-Object { Write-Error $_ }
    throw "Coverage verification failed with $($failures.Count) error(s)."
}

foreach ($moduleName in $expectedModules) {
    Write-Host "${moduleName}: 100% line / 100% branch"
}

Write-Host "Coverage verification passed for all $($expectedModules.Count) production modules."
