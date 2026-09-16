@echo off
REM Compiles the examples against PDFjet.jar and runs them.

if exist out rmdir /s /q out
mkdir out
javac -encoding utf-8 -cp PDFjet.jar examples\*.java -d out || exit /b 1

for /L %%i in (1,1,9) do java -cp PDFjet.jar;out examples.Example_0%%i
for /L %%i in (10,1,51) do java -cp PDFjet.jar;out examples.Example_%%i
