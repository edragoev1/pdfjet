@echo off
REM Compiles one example against PDFjet.jar, runs it and opens its PDF.
REM
REM   run-java.cmd 33

if "%1"=="" (
    echo Please provide an example number:
    echo run-java.cmd 33
    exit /b 1
)

if not exist out mkdir out

set RELEASE=--release 8
javac --release 8 -version >nul 2>&1 || set RELEASE=

javac -encoding utf-8 %RELEASE% -Xlint:-options -cp PDFjet.jar examples\Example_%1.java -d out || exit /b 1
java -cp PDFjet.jar;out examples.Example_%1 || exit /b 1

start Example_%1.pdf
