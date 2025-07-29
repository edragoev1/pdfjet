rem Run this script as an admin!

@echo off

for /l %%i in (1,1,50) do (
    if %%i lss 10 (
        swift run --configuration release Example_0%%i
    ) else (
        swift run --configuration release Example_%%i
    )
)

pause
