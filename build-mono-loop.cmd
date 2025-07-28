@echo off

mcs -warn:2 -debug -sdk:4.0 net/pdfjet/*.cs -reference:System.Drawing.dll -target:library -out:PDFjet.dll

for /L %%i in (1,1,50) do (
    if %%i lss 10 (
        mcs -warn:2 -optimize examples\Example_0%%i.cs -reference:PDFjet.dll
    ) else (
        mcs -warn:2 -optimize examples\Example_%%i.cs -reference:PDFjet.dll
    )
)

move examples\Example_??.exe .

for /L %%i in (1,1,50) do (
    if %%i lss 10 (
        mono --debug Example_0%%i.exe
    ) else (
        mono --debug Example_%%i.exe
    )
)
