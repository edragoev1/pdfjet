#!/bin/sh
mcs -warn:2 -debug -sdk:4 net/pdfjet/*.cs -reference:System.Drawing.dll -target:library -out:PDFjet.dll
mcs -debug -sdk:4 examples/Example_$1.cs -reference:PDFjet.dll
mv examples/Example_$1.exe .
chmod 777 Example_$1.exe
mono --debug Example_$1.exe

# evince Example_$1.pdf
mupdf Example_$1.pdf
