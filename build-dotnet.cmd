@echo off
REM Very important!!
rmdir /s /q bin 2>nul
rmdir /s /q obj 2>nul

REM TreatWarningsAsErrors fails the build on any warning and then writes no DLL.
REM The -warnaserror switch would still write it, so the example would run.

REM Build the PDFjet library project
dotnet build PDFjet.csproj -c release -p:TreatWarningsAsErrors=true

REM Build Example_01 to Example_50
for /L %%i in (1,1,50) do (
    REM Check if the example number is less than 10
    if %%i lss 10 (
        dotnet build examples\Example_0%%i\Example_0%%i.csproj -c release -p:TreatWarningsAsErrors=true
    ) else (
        dotnet build examples\Example_%%i\Example_%%i.csproj -c release -p:TreatWarningsAsErrors=true
    )
)

REM Run Example_01 to Example_50
for /L %%i in (1,1,50) do (
    REM Check if the example number is less than 10
    if %%i lss 10 (
        dotnet examples\Example_0%%i\bin\release\net8.0\Example_0%%i.dll
    ) else (
        dotnet examples\Example_%%i\bin\release\net8.0\Example_%%i.dll
    )
)
