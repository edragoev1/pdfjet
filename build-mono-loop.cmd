@echo off

# mcs -warn:2 -debug -sdk:4.0 net/pdfjet/*.cs -reference:System.Drawing.dll -target:library -out:PDFjet.dll
call dmcs /o /warn:2 /out:PDFjet.dll /target:library net/pdfjet/*.cs

for /L %%i in (1,1,50) do (
    if %%i lss 10 (
        call dmcs -warn:2 -optimize examples\Example_0%%i.cs -reference:PDFjet.dll
    ) else (
        call dmcs -warn:2 -optimize examples\Example_%%i.cs -reference:PDFjet.dll
    )
)

move examples\Example_??.exe .

for /L %%i in (1,1,50) do (
    if %%i lss 10 (
        call mono --debug Example_0%%i.exe
    ) else (
        call mono --debug Example_%%i.exe
    )
)
