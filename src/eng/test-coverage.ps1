param(
    [string]$Configuration = 'Release',
    [string]$ResultsDirectory = (Join-Path $PSScriptRoot '..\TestResults'),
    [string]$DotNetHost = (Get-Command dotnet -ErrorAction Stop).Source
)

$ErrorActionPreference = 'Stop'
$sourceDirectory = Split-Path -Parent $PSScriptRoot
$resolvedResultsDirectory = [System.IO.Path]::GetFullPath($ResultsDirectory)

# An explicit runtime host avoids the SDK-local host masking installed .NET runtimes.
# The repository SDK is still selected by global.json for MSBuild evaluation.
$testProjects = @(Get-ChildItem -LiteralPath $sourceDirectory -Directory -Filter '*.Tests' |
    ForEach-Object { Join-Path $_.FullName "$($_.Name).csproj" } |
    Where-Object { Test-Path -LiteralPath $_ })
if ($testProjects.Count -eq 0) {
    throw "No test projects found under '$sourceDirectory'."
}

foreach ($testProject in $testProjects) {
    $projectConfiguration = Join-Path (Split-Path -Parent $testProject) 'testconfig.json'
    $configurationFile = if (Test-Path -LiteralPath $projectConfiguration) {
        $projectConfiguration
    }
    else {
        Join-Path $sourceDirectory 'testconfig.json'
    }
    $frameworkJson = & $DotNetHost msbuild $testProject -nologo -getProperty:TargetFrameworks,TargetFramework
    if ($LASTEXITCODE -ne 0) {
        throw "Could not evaluate target frameworks for '$testProject'."
    }

    $properties = ($frameworkJson | Out-String | ConvertFrom-Json).Properties
    $frameworks = if ([string]::IsNullOrEmpty($properties.TargetFrameworks)) {
        @($properties.TargetFramework)
    }
    else {
        $properties.TargetFrameworks -split ';'
    }

    foreach ($framework in $frameworks) {
        if ([string]::IsNullOrWhiteSpace($framework)) {
            throw "An empty target framework was evaluated for '$testProject'."
        }

        $targetPath = & $DotNetHost msbuild $testProject -nologo -getProperty:TargetPath `
            "-p:TargetFramework=$framework" "-p:Configuration=$Configuration"
        if ($LASTEXITCODE -ne 0 -or -not (Test-Path -LiteralPath $targetPath)) {
            throw "Build '$testProject' for '$framework' ($Configuration) before running coverage."
        }

        $projectName = [System.IO.Path]::GetFileNameWithoutExtension($testProject)
        $moduleResults = Join-Path $resolvedResultsDirectory "$projectName/$framework"
        if (Test-Path -LiteralPath $moduleResults) {
            throw "Results already exist in '$moduleResults'. Use a fresh results directory to avoid stale coverage."
        }

        Write-Host "Running $projectName ($framework)"
        & $DotNetHost exec $targetPath --config-file $configurationFile `
            --coverage --coverage-output-format cobertura --results-directory $moduleResults `
            --output Minimal --progress off
        if ($LASTEXITCODE -ne 0) {
            throw "$projectName ($framework) failed with exit code $LASTEXITCODE."
        }
    }
}

& (Join-Path $PSScriptRoot 'verify-coverage.ps1') -ResultsDirectory $resolvedResultsDirectory
