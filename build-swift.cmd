@echo off
REM Run this script as an admin!

rmdir /s /q .build 2>nul

REM The Swift port has no Example_30.
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
