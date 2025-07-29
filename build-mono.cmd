@echo off

del PDFjet.dll
del PDFjet.dll.mdb

call dmcs /o /warn:2 /out:PDFjet.dll /target:library net/pdfjet/*.cs

for /L %%i in (1,1,50) do (
    if %%i lss 10 (
        call dmcs /o /warn:2 examples\Example_0%%i.cs -reference:PDFjet.dll
    ) else (
        call dmcs /o /warn:2 examples\Example_%%i.cs -reference:PDFjet.dll
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
