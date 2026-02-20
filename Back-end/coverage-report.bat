@ECHO OFF
setlocal enabledelayedexpansion

echo ========================================
echo Minerva - Coverage Report Generator
echo ========================================
echo.

REM Clean TestResults inside tests folders and root so every run starts clean
echo [0/6] Cleaning previous TestResults...
if exist "TestResults" (
    rd /s /q "TestResults"
    echo   Removed root TestResults.
)
for /d %%p in (tests\*) do (
    if exist "%%p\TestResults" (
        echo   Removing %%p\TestResults
        rd /s /q "%%p\TestResults"
    )
)
echo   Done.
echo.

REM Check if tools are installed, install if not
echo [1/6] Checking/Installing coverage tools...
dotnet tool list --global | findstr /C:"coverlet.console" >nul
if errorlevel 1 (
    echo Installing coverlet.console...
    dotnet tool install --global coverlet.console --version 6.0.0
) else (
    echo coverlet.console already installed.
)

dotnet tool list --global | findstr /C:"dotnet-reportgenerator-globaltool" >nul
if errorlevel 1 (
    echo Installing dotnet-reportgenerator-globaltool...
    dotnet tool install --global dotnet-reportgenerator-globaltool --version 5.2.0
) else (
    echo dotnet-reportgenerator-globaltool already installed.
)

echo.
echo [2/6] Restoring packages...
dotnet restore Minerva.GestaoPedidos.slnx
if errorlevel 1 (
    echo ERROR: Failed to restore packages.
    pause
    exit /b 1
)

echo.
echo [3/6] Building solution (Release)...
dotnet build Minerva.GestaoPedidos.slnx --configuration Release --no-restore
if errorlevel 1 (
    echo ERROR: Build failed.
    pause
    exit /b 1
)

echo.
echo [4/6] Running tests with coverage...
if not exist "TestResults" mkdir TestResults

REM Run tests with coverage using runsettings file
dotnet test Minerva.GestaoPedidos.slnx --no-restore --verbosity normal --collect:"XPlat Code Coverage" --settings:coverlet.runsettings

if errorlevel 1 (
    echo WARNING: Some tests failed, but continuing with coverage report generation...
)

echo.
echo [5/6] Generating HTML coverage report...

REM Find coverage files created by XPlat Code Coverage
set "COVERAGE_FILES="
for /r tests %%f in (*.coverage.cobertura.xml) do (
    if exist "%%f" (
        set "COVERAGE_FILES=!COVERAGE_FILES!%%f;"
    )
)

REM Also check for regular cobertura files
for /r tests %%f in (coverage.cobertura.xml) do (
    if exist "%%f" (
        set "COVERAGE_FILES=!COVERAGE_FILES!%%f;"
    )
)

if "!COVERAGE_FILES!"=="" (
    echo ERROR: No coverage files found.
    echo Searching for coverage files in TestResults folders...
    dir /s /b TestResults\*.cobertura.xml 2>nul
    echo.
    pause
    exit /b 1
)

REM Remove trailing semicolon
set "COVERAGE_FILES=!COVERAGE_FILES:~0,-1!"

echo Found coverage files: !COVERAGE_FILES!
echo.

REM Generate report
reportgenerator ^
-reports:"!COVERAGE_FILES!" ^
-targetdir:"./TestResults/CoverageReport" ^
-reporttypes:Html ^
-assemblyfilters:"+Minerva.GestaoPedidos.Domain*;+Minerva.GestaoPedidos.Application*;+Minerva.GestaoPedidos.Infrastructure*;-*Tests*;-*Test*"

if errorlevel 1 (
    echo ERROR: Failed to generate coverage report.
    pause
    exit /b 1
)

echo.
echo [6/6] Cleaning TestResults inside tests (keeping only root TestResults)...
for /d %%p in (tests\*) do (
    if exist "%%p\TestResults" (
        echo   Removing %%p\TestResults
        rd /s /q "%%p\TestResults"
    )
)
echo   Done.
echo.
echo ========================================
echo Coverage report generated successfully!
echo ========================================
echo.
echo Report location: TestResults\CoverageReport\index.html
echo.
if exist "TestResults\CoverageReport\index.html" (
    echo Opening report in browser...
    start TestResults\CoverageReport\index.html
) else (
    echo WARNING: Report file not found.
)
echo.
pause
