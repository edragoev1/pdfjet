@echo off
REM Run this script as an admin!

rmdir /s /q .build 2>nul

REM Builds the library and all the examples at once, then runs the examples.
REM -warnings-as-errors fails the build on any warning, and the script stops.
swift build --configuration release -Xswiftc -warnings-as-errors
if errorlevel 1 exit /b 1
for /f "delims=" %%b in ('swift build --configuration release --show-bin-path') do set BIN=%%b

REM The Swift port has no Example_30 - it demonstrates encryption, which the
REM Swift port does not support. See the "Port differences" section in README.md.
for /L %%i in (1,1,50) do (
    if %%i neq 30 (
        if %%i lss 10 (
            "%BIN%\Example_0%%i.exe"
        ) else (
            "%BIN%\Example_%%i.exe"
        )
    )
)

pause
