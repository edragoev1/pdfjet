@echo off
REM Check if an argument is provided (example number)
if "%1"=="" (
    echo Usage: %0 ^<example_number^>
    exit /b 1
)

REM Very important!!
rmdir /s /q bin 2>nul
rmdir /s /q obj 2>nul

REM Build the PDFjet library
dotnet build PDFjet.csproj -c release

REM Build and run the specified Example project
dotnet build examples\Example_%1\Example_%1.csproj -c release
dotnet examples\Example_%1\bin\release\net8.0\Example_%1.dll

REM Open the generated PDF for the specified Example
start Example_%1.pdf
