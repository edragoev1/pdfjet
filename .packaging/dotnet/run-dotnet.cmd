@echo off
REM Builds one example project against PDFjet.dll and runs it.
REM
REM   run-dotnet.cmd 33

if "%1"=="" (
    echo Usage: run-dotnet.cmd 33
    exit /b 1
)

dotnet run --project examples\Example_%1 -c release
