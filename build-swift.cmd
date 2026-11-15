@echo off
REM The folder of this script, which has the files the examples read, from wherever it is run.
cd /d "%~dp0"
REM Run this script as an admin!

rmdir /s /q .build 2>nul

REM Builds the library and all the examples at once, then runs the examples.
REM -warnings-as-errors fails the build on any warning, and the script stops.
swift build --configuration release -Xswiftc -warnings-as-errors
if errorlevel 1 exit /b 1
for /f "delims=" %%b in ('swift build --configuration release --show-bin-path') do set BIN=%%b

for /L %%i in (1,1,51) do (
    if %%i lss 10 (
        "%BIN%\Example_0%%i.exe"
    ) else (
        "%BIN%\Example_%%i.exe"
    )
)
REM Example_52 shapes with Core Text, Apple's, which Windows does not have.
for /L %%i in (53,1,85) do (
    "%BIN%\Example_%%i.exe"
)

pause
