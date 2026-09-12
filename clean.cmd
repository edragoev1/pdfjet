@echo off
REM Windows batch script equivalent of clean.sh

rmdir /s /q out\production 2>nul
del /f /q net\pdfjet\*.exe.mdb 2>nul
del /f /q examples\*.exe.mdb 2>nul
del /f /q tests\*.exe.mdb 2>nul
del /f /q util\*.class 2>nul
del /f /q *.jar 2>nul
del /f /q *.exe 2>nul
del /f /q *.mdb 2>nul
del /f /q *.dll 2>nul
rmdir /s /q bin 2>nul
rmdir /s /q obj 2>nul
rmdir /s /q .build 2>nul
del /f /q *.pdf 2>nul

for /L %%i in (1,1,50) do (
    if %%i lss 10 (
        rmdir /s /q examples\Example_0%%i\bin 2>nul
        rmdir /s /q examples\Example_0%%i\obj 2>nul
    ) else (
        rmdir /s /q examples\Example_%%i\bin 2>nul
        rmdir /s /q examples\Example_%%i\obj 2>nul
    )
)
