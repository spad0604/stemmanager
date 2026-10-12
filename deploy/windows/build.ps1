$ErrorActionPreference = 'Stop'
$repository = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
Set-Location $repository
dotnet restore STEMManager.sln
if ($LASTEXITCODE -ne 0) { throw 'Restore failed' }
dotnet test STEMManager.sln -c Release --logger trx
if ($LASTEXITCODE -ne 0) { throw 'Tests failed' }
dotnet run --project App/STEMLabManager.Tools -c Release -- prepare-docs
if ($LASTEXITCODE -ne 0) { throw 'Documentation generation failed' }
dotnet publish App/STEMLabManager.Desktop -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -o artifacts/windows/app
if ($LASTEXITCODE -ne 0) { throw 'Publish failed' }
$crtDirectory = $env:STEM_VC_CRT_DIR
if (-not $crtDirectory) {
    $vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
    if (Test-Path $vswhere) {
        $vsInstallation = & $vswhere -latest -products '*' -property installationPath
        if ($vsInstallation) {
            $redistRoot = Join-Path $vsInstallation 'VC\Redist\MSVC'
            $crtDirectory = Get-ChildItem $redistRoot -Filter 'Microsoft.VC*.CRT' -Directory -Recurse |
                Where-Object { $_.FullName -match '\\x64\\' } |
                Sort-Object FullName -Descending | Select-Object -First 1 -ExpandProperty FullName
        }
    }
}
if (-not $crtDirectory -or -not (Test-Path (Join-Path $crtDirectory 'msvcp140.dll'))) {
    throw 'ONNX needs Visual C++ runtime. Set STEM_VC_CRT_DIR to the licensed x64 Microsoft.VC*.CRT redist folder, or install Visual Studio C++ build tools on the build machine.'
}
Copy-Item (Join-Path $crtDirectory '*.dll') artifacts/windows/app -Force
$compiler = Get-Command ISCC.exe -ErrorAction SilentlyContinue
$compilerPath = if ($compiler) { $compiler.Source } else { 'C:\Program Files (x86)\Inno Setup 6\ISCC.exe' }
if (-not (Test-Path $compilerPath)) { throw 'Install Inno Setup 6 on the build machine. End users do not need it.' }
& $compilerPath deploy/windows/STEMManager.iss
if ($LASTEXITCODE -ne 0) { throw 'Installer compilation failed' }
Compress-Archive -Path artifacts/windows/app/* -DestinationPath artifacts/windows/STEMManager-Portable-x64.zip -Force
