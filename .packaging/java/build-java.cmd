@echo off
REM Compiles the 51 examples against PDFjet.jar and runs them.
REM Each example writes its PDF to this directory.

if exist out rmdir /s /q out
mkdir out

REM --release 8 builds Java 8 class files, as PDFjet.jar is. Java 8's javac has
REM no --release option and builds Java 8 class files anyway.
set RELEASE=--release 8
javac --release 8 -version >nul 2>&1 || set RELEASE=

javac -encoding utf-8 %RELEASE% -Xlint:-options -cp PDFjet.jar examples\Example_*.java -d out || exit /b 1

for /L %%i in (1,1,51) do (
    if %%i lss 10 (
        java -cp PDFjet.jar;out examples.Example_0%%i
    ) else (
        java -cp PDFjet.jar;out examples.Example_%%i
    )
)
