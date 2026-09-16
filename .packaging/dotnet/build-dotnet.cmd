@echo off
REM Builds the 51 example projects against PDFjet.dll and runs them.
REM Each example writes its PDF to this directory.

for /L %%i in (1,1,51) do (
    if %%i lss 10 (
        dotnet build examples\Example_0%%i\Example_0%%i.csproj -c release
    ) else (
        dotnet build examples\Example_%%i\Example_%%i.csproj -c release
    )
)

for /L %%i in (1,1,51) do (
    if %%i lss 10 (
        dotnet examples\Example_0%%i\bin\release\net8.0\Example_0%%i.dll
    ) else (
        dotnet examples\Example_%%i\bin\release\net8.0\Example_%%i.dll
    )
)
