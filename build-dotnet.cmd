@echo off

:: Remove bin directory
rd /s /q bin

:: Loop from 1 to 50
for /L %%i in (1,1,50) do (
    :: Check if %%i is less than 10
    if %%i lss 10 (
        rd /s /q obj
        dotnet build PDFjet.csproj /p:StartupObject=Example_0%%i
        dotnet run
        echo.
    ) else (
        rd /s /q obj
        dotnet build PDFjet.csproj /p:StartupObject=Example_%%i
        dotnet run
        echo.
    )
)
