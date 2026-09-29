@echo off
call "%~dp0get-fonts-and-data.cmd" || exit /b 1
REM The folder of this script, which has fonts and data, from wherever it is run.
cd /d "%~dp0"
REM Windows batch script for building and running Swift examples

REM Check if argument is provided
if "%1"=="" (
    echo Please provide an example number:
    echo run-swift.cmd 33
    exit /b 1
)

REM Very important!!
call clean.cmd

REM swift run --configuration release Example_%1
swift run --configuration debug Example_%1

REM Open the resulting PDF using the default PDF viewer
start Example_%1.pdf
