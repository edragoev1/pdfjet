@echo off
call "%~dp0get-fonts-and-data.cmd" || exit /b 1
REM The folder of this script, which has fonts and data, from wherever it is run.
cd /d "%~dp0"
REM Windows batch script equivalent

REM Check if argument is provided
if "%1"=="" (
    echo Please provide an example number:
    echo run-go.cmd 33
    exit /b 1
)

REM Very important!!
call clean.cmd

cd src
go build -o ..\Example_%1.exe examples\example%1\main.go
cd ..

Example_%1.exe

REM Open the resulting PDF using the default PDF viewer
start Example_%1.pdf
