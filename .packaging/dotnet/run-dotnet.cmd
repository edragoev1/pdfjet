@echo off
REM Builds one example project against PDFjet.dll, runs it and opens its PDF.
REM
REM   run-dotnet.cmd 33

if "%1"=="" (
    echo Please provide an example number:
    echo run-dotnet.cmd 33
    exit /b 1
)

dotnet build examples\Example_%1\Example_%1.csproj -c release || exit /b 1
dotnet examples\Example_%1\bin\release\net8.0\Example_%1.dll || exit /b 1

start Example_%1.pdf
