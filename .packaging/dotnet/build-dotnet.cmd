@echo off
REM Builds the example projects against PDFjet.dll and runs them.

for /L %%i in (1,1,9) do dotnet run --project examples\Example_0%%i -c release
for /L %%i in (10,1,51) do dotnet run --project examples\Example_%%i -c release
