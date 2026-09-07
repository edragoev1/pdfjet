@echo off
REM Run this script as an admin!

rmdir /s /q .build 2>nul

REM The Swift port has no Example_30 - it demonstrates encryption, which the
REM Swift port does not support. See the "Port differences" section in README.md.
for /L %%i in (1,1,50) do (
    if %%i neq 30 (
        if %%i lss 10 (
            swift run --configuration release Example_0%%i
        ) else (
            swift run --configuration release Example_%%i
        )
    )
)

pause
