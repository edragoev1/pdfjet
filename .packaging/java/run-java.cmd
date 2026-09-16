@echo off
REM Compiles one example against PDFjet.jar and runs it.
REM
REM   run-java.cmd 33

if "%1"=="" (
    echo Usage: run-java.cmd 33
    exit /b 1
)

if not exist out mkdir out
javac -encoding utf-8 -cp PDFjet.jar examples\Example_%1.java -d out || exit /b 1
java -cp PDFjet.jar;out examples.Example_%1
